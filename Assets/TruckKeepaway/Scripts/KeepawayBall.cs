using UnityEngine;
namespace LWS.TruckKeepaway {
public sealed class KeepawayBall:MonoBehaviour {
 public KeepawayMatch match;
 void OnCollisionEnter(Collision c){Contact(c);}void OnCollisionStay(Collision c){Contact(c);}void Contact(Collision c){var truck=c.rigidbody!=null?c.rigidbody.GetComponent<KeepawayContact>():null;if(truck!=null)match.Pickup(truck.slot);}
}
}
