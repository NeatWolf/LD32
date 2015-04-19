﻿using UnityEngine;
using System.Collections;

public class Monster : MonoBehaviour {
	public float avoidForce=1000;
	public float crumbRange=5f;
	bool active=false;
	public LayerMask playerLayer;
	public LayerMask avoidLayers;
	public float healthBonus=20f;
	// Use this for initialization
	void Start () {
		// HAXXXX
		Vector3 temp = transform.position;
		temp.y=0.55f;
		transform.position = temp;

		StartCoroutine(AILoop());
		if (jumpy){
			StartCoroutine(Moving(aiRate));
		}
		StartCoroutine(OOS(aiRate));
		SphereCollider trig=null;
		foreach (SphereCollider c in GetComponentsInChildren<SphereCollider>(true)){
			if (c.isTrigger){
				trig = c;
				break;
			}
		}
		foreach (Collider c in Physics.OverlapSphere(transform.position,trig.radius,playerLayer.value)){
			if (PlayerCharacter.CheckCollider(c)){
				OnTriggerEnter(c);
				break;
			}
		}
	}

	void OnTriggerEnter(Collider col){
		// Col is player
		active = true;
	}
	
	private Crumb crumb;
	private Vector3 rRate = new Vector3();
	[SerializeField]
	private float rotRate=1.5f;
	[SerializeField]
	private float speed=0.5f;
	[SerializeField]
	private bool jumpy=false;
	[SerializeField]
	private float pauseMin=0.5f;
	[SerializeField]
	private float pauseMax=2f;
	[SerializeField]
	private float moveMin=2f;
	[SerializeField]
	private float moveMax=4f;
	bool paused=false;
	private float aiRate=0.05f;

	private bool tracking=false;
	private bool crumbing=false;
	private bool pvis=false;
	IEnumerator AILoop(){
		while (true){
			if (active){
				RaycastHit hit;
				if (Physics.Linecast(transform.position,PlayerCharacter.pos,out hit)){
					if (PlayerCharacter.CheckCollider(hit.collider)){
						pvis=true;
						tracking = true;
						// No longer using player's breadcrumbs - using own
						SetCrumb();
						//Debug.DrawLine(transform.position,PlayerCharacter.pos,Color.green,aiRate);
					} else {
						pvis=false;
						//Debug.DrawLine(transform.position,PlayerCharacter.pos,Color.red,aiRate);
					}
				} else {
					pvis=false;
				}
			}
			yield return new WaitForSeconds(aiRate);
		}
	}
	IEnumerator Pause(float dT){
		paused=true;
		float t = Random.Range(pauseMin,pauseMax);
		while (t>0){
			if (active && tracking){
				t-=dT;
			}
			yield return new WaitForSeconds(dT);
		}
		StartCoroutine(Moving(dT));
	}
	IEnumerator Moving(float dT){
		paused = false;
		float t = Random.Range(moveMin,moveMax);
		while (t>0){
			if (active && tracking){
				t-=dT;
			}
			yield return new WaitForSeconds(dT);
		}
		StartCoroutine(Pause(dT));
	}
	void FixedUpdate(){
		if (active){
			if (tracking){
				// Track crumb
				Vector3 dir = crumb.pos;
				dir.y = transform.position.y;
				dir = dir-transform.position;
				float angle = Vector3.Angle(transform.forward,dir);
				// Rate is per 180 degrees
				transform.forward = Vector3.SmoothDamp(transform.forward,dir.normalized,ref rRate, rotRate*angle/180);

				if (!paused){
					// Check approach distance
					if (pvis){
						if (dir.magnitude > 1.2f){
							transform.position += transform.forward*speed*Time.fixedDeltaTime;
						} else {
							// Bounce!
							transform.position -= transform.forward*speed*Time.fixedDeltaTime;
						}
					} else {
//						Debug.Log("Player invisible");
						if (dir.magnitude > crumbRange){
//							Debug.Log(dir.ToString());
							Debug.DrawLine(transform.position,transform.position+dir,Color.white,Time.fixedDeltaTime);
							transform.position += transform.forward*speed*Time.fixedDeltaTime;
						} else {
							Debug.Log("Hit crumb");
							// Get next crumb
							if (crumbing){
								Debug.Log("Getting next player crumb");
								crumb = PlayerCharacter.NextCrum(crumb);
							} else {
								// Get nearest player crumb
								float minSM = 999999999;
								foreach (Crumb c in PlayerCharacter.crumbs){
									Vector3 dp = c.pos - transform.position;
									// Check if nearer than current
									float dps = dp.sqrMagnitude;
									if (dps < minSM){
										crumb = c;
										minSM = dps;
									}
								}
								if (crumb != null){
									// Check LoS
									// Start at index of current crumb and work backwards
									for (int i=PlayerCharacter.crumbs.IndexOf(crumb); i>=0; --i){										RaycastHit hit;
										if (Physics.Linecast(transform.position,
										                     PlayerCharacter.crumbs[i].pos,
										                     out hit)){
											// Obstructed
											Debug.DrawLine(transform.position,hit.point,Color.red,Time.fixedDeltaTime);
											if (PlayerCharacter.CheckCollider(hit.collider)){
												// By player, so set new crumb
												SetCrumb();
												Debug.Log("PLAYER");
											}
										} else {
											// No obstruction
											Debug.Log("CRUMB");
											crumb = PlayerCharacter.crumbs[i];
											crumbing=true;
											break;
										}
									}
									crumbing = true;
								}
							}
						}
					}
				}
			} else {
				// Rotate towards player so AI doesn't run in wrong dir when visible
				float angle = Vector3.Angle(transform.forward,(PlayerCharacter.pos-transform.position));
				Vector3 target = (PlayerCharacter.pos-transform.position).normalized;
				// Rate is per 180 degrees
				transform.forward = Vector3.SmoothDamp(transform.forward,target,ref rRate, rotRate*angle/180);
			}

			// Evasion
			if (tracking){
				RaycastHit[] temp = Physics.SphereCastAll(transform.position,1.2f,transform.forward,0,avoidLayers.value);
				if (temp.Length > 0){
					Vector3 dir = new Vector3();
					foreach (RaycastHit h in temp){
						dir += (h.point - transform.position).normalized;
						Debug.DrawLine (transform.position,h.point,Color.cyan,Time.fixedDeltaTime);
					}
					Debug.DrawLine(transform.position,transform.position-dir,Color.blue,Time.fixedDeltaTime);
					dir /= temp.Length;
					GetComponent<Rigidbody>().AddForce(avoidForce*dir.normalized*Time.fixedDeltaTime);
				}
			}
		}
	}
	private void SetCrumb(){
		crumbing=false;
		pvis = true;
		crumb = PlayerCharacter.GetCrumb();
	}

	[SerializeField]
	private float memory=8f;
	private float timeToDie;
	private bool evis=true;
	IEnumerator OOS(float dT){
		timeToDie = memory;
		while (timeToDie>0){
			// Only decrement when unseen
			if (!evis){
//				Debug.Log("Forget in "+timeToDie);
				timeToDie -= dT;
			} else {
				timeToDie=memory;
			}
			yield return new WaitForSeconds(dT);
		}
		Destroy(gameObject);
	}

	public Coroutine coos;
	public void OutOfSight(){
//		Debug.Log("FORGETTING");
		evis=false;
	}
	public void InSight(){
//		Debug.Log("OMGWTF");
		evis=true;
	}
	void OnDestroy(){
		PlayerCharacter.AddHealth(healthBonus);
	}
}
