using System;
using UnityEngine;

namespace LWS.TruckFieldGoal {
// Sport-specific contact response and bounded aerodynamics, shared by every shooter.
// NWH still owns driving, PhysX still owns contacts, gravity and post rebounds.
[Serializable]
public sealed class FieldGoalFlightModel {
 [Range(1,2)]public float contactPower=1.65f;
 [Range(0,.02f)]public float airDrag=.0025f;
 [Range(0,.02f)]public float spinLift=.003f;
 [Min(1)]public float maximumDragAcceleration=12;
 [Min(0)]public float maximumSpinAcceleration=2.5f;
 public void Kick(Vector3 incoming,Vector3 contactOffset,float radius,out Vector3 velocity,out Vector3 spin){
  incoming=Vector3.ProjectOnPlane(incoming,Vector3.up);float speed=incoming.magnitude;
  var forward=speed>.001f?incoming/speed:Vector3.forward;var right=Vector3.Cross(Vector3.up,forward);
  float lateral=Mathf.Clamp(Vector3.Dot(contactOffset,right)/Mathf.Max(.1f,radius),-1,1);
  float height=Mathf.Clamp(contactOffset.y/Mathf.Max(.1f,radius),-1,1);
  float rise=Mathf.Clamp(3+speed*.7f-height*speed*.3f,3,17);
  velocity=forward*Mathf.Clamp(speed*contactPower,3,32)-right*(lateral*speed*.18f)+Vector3.up*rise;
  spin=Vector3.ClampMagnitude(Vector3.Cross(contactOffset,incoming)*2+Vector3.Cross(Vector3.up,forward)*(speed*.3f),20);
 }
 public Vector3 AirAcceleration(Vector3 velocity,Vector3 angularVelocity,Vector3 wind){
  var relative=wind-velocity;
  var drag=Vector3.ClampMagnitude(relative*relative.magnitude*airDrag,maximumDragAcceleration);
  var lift=Vector3.ClampMagnitude(Vector3.Cross(angularVelocity,-relative)*spinLift,maximumSpinAcceleration);
  return drag+lift;
 }
}
}
