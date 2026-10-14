using UnityEngine;
namespace LWS.TruckSports {
// Shared flat-surface drive adapter. PhysX remains the only contact impulse authority.
public static class SportsPlanarDrive {
 public static bool Step(Rigidbody body,float gas,float steer,bool brake,float acceleration,float speedLimit,float reverseLimit,float turn,float grip,float coast,float braking,float impactLimit,ref bool opposing){
  float dt=Time.fixedDeltaTime;var velocity=body.linearVelocity;var forward=body.rotation*Vector3.forward;float speed=Vector3.Dot(velocity,forward);
  bool requested=Mathf.Abs(gas)>.08f&&speed*gas<0;if(!requested||Mathf.Abs(speed)<.12f)opposing=false;else if(Mathf.Abs(speed)>.3f)opposing=true;
  float direction=speed<-.15f?-1:1;body.MoveRotation(body.rotation*Quaternion.Euler(0,steer*turn*Mathf.Clamp01(Mathf.Abs(speed)/1.6f)*direction*dt,0));
  if(!opposing&&!brake&&speed*(gas<0?-1:1)<(gas<0?reverseLimit:speedLimit))body.AddForce(forward*gas*acceleration,ForceMode.Acceleration);
  var right=body.rotation*Vector3.right;body.AddForce(-right*Vector3.Dot(velocity,right)*grip-velocity*(opposing||brake?braking:coast),ForceMode.Acceleration);
  body.linearVelocity=Vector3.ClampMagnitude(velocity,impactLimit);return opposing;
 }
}
}
