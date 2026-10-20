using UnityEngine;
namespace LWS.TruckMagnetMayhem
{
 public sealed class MagnetSortingArm:MonoBehaviour
 {
  public MagnetMatch match;public Rigidbody body;public float degreesPerSecond=12;
  Quaternion start;
  void Awake(){start=body.rotation;}
  void FixedUpdate(){if(!match.Live||match.Countdown>0)return;body.MoveRotation(body.rotation*Quaternion.AngleAxis(degreesPerSecond*Time.fixedDeltaTime,Vector3.up));}
  public void ResetArm(){body.rotation=start;body.transform.rotation=start;}
 }
}
