using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckKeepaway {
public sealed class KeepawayContact:MonoBehaviour {
 public KeepawayMatch match;public int slot;
 void OnCollisionEnter(Collision c){var other=c.rigidbody!=null?c.rigidbody.GetComponent<KeepawayContact>():null;if(other!=null)match.Collision(slot,other.slot,c.GetContact(0).point);}
 void OnCollisionExit(Collision c){var other=c.rigidbody!=null?c.rigidbody.GetComponent<KeepawayContact>():null;if(other!=null)match.Separate(slot,other.slot);}
}
}
