using UnityEngine;
namespace LWS.TruckNinjaWarrior
{
 public sealed class NinjaRotator:MonoBehaviour
 {
  public Rigidbody body;public Vector3 localAxis=Vector3.up;public float degreesPerSecond=22;Quaternion rest;bool cached;
  public void ResetRotation(){if(!cached){rest=body.rotation;cached=true;}body.rotation=rest;transform.rotation=rest;}
  public void Step(float age){if(!cached)ResetRotation();body.MoveRotation(rest*Quaternion.AngleAxis(age*degreesPerSecond,localAxis));}
 }
}
