using UnityEngine;
namespace LWS.TruckCheeseRolling {
public sealed class CheeseWheel:MonoBehaviour {
 public CheeseMatch match;public Rigidbody body;public int Bounces{get;private set;}
 float soundAt;
 void OnCollisionEnter(Collision c){Contact(c);if(c.relativeVelocity.magnitude>2&&Time.time>soundAt){soundAt=Time.time+.2f;Bounces++;match.feedback.Play(1,c.relativeVelocity.magnitude);}}
 void OnCollisionStay(Collision c){Contact(c);}
 void Contact(Collision c){var truck=c.rigidbody!=null?c.rigidbody.GetComponent<CheeseTruck>():null;if(truck!=null)match.Catch(truck);}
 void FixedUpdate(){if(!match.Live)return;var v=body.linearVelocity;if(body.position.z>170){body.AddForce(-new Vector3(v.x,0,v.z)*.8f,ForceMode.Acceleration);body.angularDamping=.8f;}else body.angularDamping=.16f;body.AddForce(match.weather.Gust*.55f,ForceMode.Acceleration);}
}
}
