using UnityEngine;
namespace LWS.TruckFrogger
{
 // Deterministic challenge machinery paths, not replacement vehicle physics.
 public sealed class FrogMover:MonoBehaviour
 {
  public Rigidbody body;public Vector3 origin;public float travel=145,period=15,phase,halfWidth=9;public bool platform,loop;float seededPhase;
  public Vector3 At(float age){float t=(age/period+seededPhase)%1;float x=platform?(t<.45f?0:Mathf.Sin((t-.45f)/.55f*Mathf.PI*2)*travel):loop?(t*2-1)*travel:Mathf.Sin(t*Mathf.PI*2)*travel;return origin+Vector3.right*x;}
  // Speculative CCD produced remote contacts when a loop/reset relocated a
  // kinematic freight hierarchy. Normal movement is <0.5m per fixed step.
  public void ResetMotion(int seed){body.collisionDetectionMode=CollisionDetectionMode.Discrete;seededPhase=Mathf.Repeat(phase+(uint)seed%997/997f,1);body.position=At(0);body.rotation=Quaternion.identity;transform.SetPositionAndRotation(body.position,body.rotation);}
  public void Step(float age){body.MovePosition(At(age));}
 }
}
