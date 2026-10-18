using UnityEngine;
namespace LWS.TruckPedestrianPlowing {
 public sealed class PlowProp:MonoBehaviour {
  public Rigidbody body;public PlowBreakable breakable;Vector3 position;Quaternion rotation;
  void Awake(){position=transform.position;rotation=transform.rotation;}
  public void ResetProp(){body.position=position;body.rotation=rotation;body.linearVelocity=body.angularVelocity=Vector3.zero;body.Sleep();breakable?.ResetPiece();}
 }
}
