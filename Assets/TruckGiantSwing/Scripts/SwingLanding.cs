using UnityEngine;
namespace LWS.TruckGiantSwing {
public sealed class SwingLanding:MonoBehaviour {public SwingMatch match;void OnCollisionEnter(Collision c){if(c.collider.GetComponentInParent<SwingGround>()!=null)match.GroundContact(c);}}
}
