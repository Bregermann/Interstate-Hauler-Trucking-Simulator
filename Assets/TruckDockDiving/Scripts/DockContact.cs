using UnityEngine;
namespace LWS.TruckDockDiving {
public sealed class DockContact:MonoBehaviour {
 public DockMatch match;
 void OnTriggerEnter(Collider c){if(c==match.waterVolume)match.TouchWater();}
 void OnCollisionEnter(Collision c){match.SolidLanding(c.collider,c.relativeVelocity.magnitude);}
}
}
