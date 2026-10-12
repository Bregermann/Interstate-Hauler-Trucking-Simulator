using UnityEngine;
namespace LWS.TruckHockey {
public sealed class HockeyPuck:MonoBehaviour {
 public HockeyMatch match;public Rigidbody body;public float radius=.55f,drag=.28f,maxSpeed=16;float nextAudio;readonly float[] contactTime={-100,-100,-100,-100,-100,-100};
 public int LastTouch{get;private set;}=-1;public int ActiveContacts{get{int count=0;for(int i=0;i<6;i++)if(Time.time-contactTime[i]<.08f)count++;return count;}}
 public int PushingContacts{get{int count=0;for(int i=0;i<6;i++)if(Time.time-contactTime[i]<.08f&&Mathf.Abs(match.trucks[i].throttle)>.25f)count++;return count;}}
 public bool ContactBothTrucks=>ActiveContacts>=2;
 public void ClearContacts(){for(int i=0;i<6;i++)contactTime[i]=-100;LastTouch=-1;}public void ForgetTruckContact(int slot){contactTime[slot]=-100;}
 void OnCollisionStay(Collision c){for(int i=0;i<6;i++)if(c.rigidbody==match.trucks[i].body)contactTime[i]=Time.time;}
 void OnCollisionExit(Collision c){for(int i=0;i<6;i++)if(c.rigidbody==match.trucks[i].body)contactTime[i]=-100;}
 public int BoardContacts{get;private set;}public int TruckContacts{get;private set;}
 void FixedUpdate(){if(!match.rules.Live||match.MenuOpen)return;body.linearVelocity=Vector3.ClampMagnitude(body.linearVelocity/(1+drag*Time.fixedDeltaTime),maxSpeed);}
 void OnCollisionEnter(Collision c){if(!match.rules.Live||match.MenuOpen)return;if(c.rigidbody==null)BoardContacts++;else{TruckContacts++;for(int i=0;i<6;i++)if(c.rigidbody==match.trucks[i].body)LastTouch=i;}if(Time.time>=nextAudio){nextAudio=Time.time+.075f;match.feedback.Play(c.rigidbody==null?2:1,c.relativeVelocity.magnitude);}}
}
}