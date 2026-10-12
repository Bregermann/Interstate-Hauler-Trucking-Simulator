using UnityEngine;
namespace LWS.TruckHockey {
public sealed class HockeyPuck:MonoBehaviour {
 public HockeyMatch match;public Rigidbody body;public float radius=.55f,drag=.28f,maxSpeed=16;float nextAudio;readonly float[] contactTime={-100,-100};
 public bool ContactBothTrucks=>Time.time-contactTime[0]<.08f&&Time.time-contactTime[1]<.08f;public void ClearContacts(){contactTime[0]=contactTime[1]=-100;}public void ForgetTruckContact(int side){contactTime[side]=-100;}
 void OnCollisionStay(Collision c){for(int i=0;i<2;i++)if(c.rigidbody==match.trucks[i].body)contactTime[i]=Time.time;}
 void OnCollisionExit(Collision c){for(int i=0;i<2;i++)if(c.rigidbody==match.trucks[i].body)contactTime[i]=-100;}
 public int BoardContacts{get;private set;}public int TruckContacts{get;private set;}
 void FixedUpdate(){if(!match.rules.Live||match.MenuOpen)return;body.linearVelocity=Vector3.ClampMagnitude(body.linearVelocity/(1+drag*Time.fixedDeltaTime),maxSpeed);}
 void OnCollisionEnter(Collision c){if(!match.rules.Live||match.MenuOpen)return;body.linearVelocity=Vector3.ClampMagnitude(body.linearVelocity,maxSpeed);if(c.rigidbody==null)BoardContacts++;else TruckContacts++;if(Time.time>=nextAudio){nextAudio=Time.time+.075f;match.feedback.Play(c.rigidbody==null?2:1,c.relativeVelocity.magnitude);}}
}
}

