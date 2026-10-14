using UnityEngine;
namespace LWS.TruckAirHockey {
public sealed class AirHockeyPuck:MonoBehaviour {
 public AirHockeyMatch match;public Rigidbody body;public float drag=.012f,maxSpeed=22;public int RailContacts{get;private set;}public int TruckContacts{get;private set;}readonly float[] contacts={-100,-100};float nextAudio;
 public int ActiveContacts=>(Time.time-contacts[0]<.08f?1:0)+(Time.time-contacts[1]<.08f?1:0);public void ClearContacts(){contacts[0]=contacts[1]=-100;}
 void FixedUpdate(){if(!match.rules.Live||match.MenuOpen)return;body.linearVelocity=Vector3.ClampMagnitude(body.linearVelocity/(1+drag*Time.fixedDeltaTime),maxSpeed);}
 void OnCollisionStay(Collision c){for(int i=0;i<2;i++)if(c.rigidbody==match.trucks[i].body)contacts[i]=Time.time;}void OnCollisionExit(Collision c){for(int i=0;i<2;i++)if(c.rigidbody==match.trucks[i].body)contacts[i]=-100;}
 void OnCollisionEnter(Collision c){if(!match.rules.Live||match.MenuOpen)return;if(c.rigidbody==null)RailContacts++;else{TruckContacts++;for(int i=0;i<2;i++)if(c.rigidbody==match.trucks[i].body)match.LastTouch=i;}if(Time.time>=nextAudio){nextAudio=Time.time+.055f;match.feedback.Play(c.rigidbody==null?2:1,c.relativeVelocity.magnitude);}}
}
}
