using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckVolleyball
{
 // Volleyball owns the temporary tuning and ability; NWH still owns propulsion,
 // steering, wheel contact and the simulated vehicle assembly.
 public sealed class VolleyTruckAbility
 {
  readonly SportsEventTruck truck;
  readonly RaycastHit[] supportHits=new RaycastHit[16];
  float speedLimit,cityLock,highLock,response,smoothing,righting,originalGripScale;float[] longitudinalGrip,lateralGrip;
  bool handlingWasApplied;
  public bool Applied{get;private set;}
  public bool Recovering=>righting>0;
  public float MovementSpeedLimit{get;set;}=40;
  public float GripMultiplier=>Applied?1.35f:1;
  public VolleyTruckAbility(SportsEventTruck owner){truck=owner;}
  float Power()=>1.5f;
  public void Apply()
  {
   CancelRecovery();if(Applied)return;
   var motor=truck.tractorMotor;var handling=motor.handling;
   // The shared NWH adapter remains the sole wheel-grip writer while driving.
   // Cache the exact exposed values for exit, rather than compounding a tyre
   // multiplier or replacing its original weather-relative friction inputs.
   originalGripScale=motor.gripScale;longitudinalGrip=new float[truck.wheels.Length];lateralGrip=new float[truck.wheels.Length];for(int i=0;i<truck.wheels.Length;i++){longitudinalGrip[i]=truck.wheels[i].LongitudinalFrictionGrip;lateralGrip[i]=truck.wheels[i].LateralFrictionGrip;}
   // Unity's runtime default can be effectively unbounded even though the old
   // scene author assigned32. Preserve that exact baseline only for restoration;
   // it must not win a Max comparison against this mode's bounded public cap.
   speedLimit=truck.body.maxLinearVelocity;truck.body.maxLinearVelocity=Mathf.Clamp(MovementSpeedLimit,1,40);
   if(handling!=null){handlingWasApplied=handling.Applied;cityLock=handling.citySteeringLock;highLock=handling.highSpeedSteeringLock;response=handling.steeringResponse;smoothing=handling.lowSpeedSmoothing;handling.Restore();handling.citySteeringLock=Mathf.Max(cityLock,Mathf.Min(60,cityLock*1.25f));handling.highSpeedSteeringLock=Mathf.Max(highLock,Mathf.Min(15,highLock*1.5f));handling.steeringResponse=Mathf.Max(response,Mathf.Min(500,response*1.25f));handling.lowSpeedSmoothing=Mathf.Max(.01f,smoothing*.72f);handling.Apply();}
   motor.vehicle.powertrain.engine.powerModifiers.Add(Power);Applied=true;
  }
  public void Restore()
  {
   CancelRecovery();if(!Applied)return;Applied=false;if(truck==null)return;
   var motor=truck.tractorMotor;if(motor!=null){motor.gripScale=originalGripScale;if(motor.vehicle!=null)motor.vehicle.powertrain.engine.powerModifiers.Remove(Power);}if(truck.body!=null)truck.body.maxLinearVelocity=speedLimit;
   if(longitudinalGrip!=null&&truck.wheels!=null)for(int i=0;i<Mathf.Min(truck.wheels.Length,longitudinalGrip.Length);i++)if(truck.wheels[i]!=null){truck.wheels[i].LongitudinalFrictionGrip=longitudinalGrip[i];truck.wheels[i].LateralFrictionGrip=lateralGrip[i];}
   var handling=motor!=null?motor.handling:null;if(handling!=null){handling.Restore();handling.citySteeringLock=cityLock;handling.highSpeedSteeringLock=highLock;handling.steeringResponse=response;handling.lowSpeedSmoothing=smoothing;if(handlingWasApplied)handling.Apply();}
  }
  public void CancelRecovery()=>righting=0;
  bool Supported()
  {
   if(truck.tractorMotor.Grounded)return true;
   var bounds=truck.shape.bounds;
   int count=Physics.SphereCastNonAlloc(bounds.center+Vector3.up*.1f,.25f,Vector3.down,supportHits,bounds.extents.y+.65f,~0,QueryTriggerInteraction.Ignore);
   for(int i=0;i<count;i++){var hit=supportHits[i];if(hit.collider!=null&&!hit.collider.transform.IsChildOf(truck.transform)&&hit.normal.y>.35f)return true;}
   return false;
  }
  Vector3 RightingAxis()
  {
   var axis=Vector3.Cross(truck.transform.up,Vector3.up);
   // Cross(up, up) is zero for a fully inverted truck. Its forward axis gives
   // a stable roll direction without selecting a new heading or setting a pose.
   return axis.sqrMagnitude>.0001f?axis.normalized:truck.transform.forward;
  }
  public bool TryJump(float height)
  {
   if(truck.body.isKinematic)return false;
   bool tipped=Vector3.Dot(truck.transform.up,Vector3.up)<.5f;
   if(tipped?!Supported():!truck.tractorMotor.Grounded)return false;
   float speed=tipped?6.5f:Mathf.Sqrt(2*Physics.gravity.magnitude*height);
   truck.tractorMotor.AddAssemblyVelocity(Vector3.up*Mathf.Clamp(speed-truck.body.linearVelocity.y,0,16));
   if(tipped){righting=.9f;truck.body.AddTorque(RightingAxis()*2.5f,ForceMode.VelocityChange);}
   return true;
  }
  public void Step(float dt)
  {
   if(righting<=0||truck.body.isKinematic)return;
   righting=Mathf.Max(0,righting-dt);
   float angle=Vector3.Angle(truck.transform.up,Vector3.up)*Mathf.Deg2Rad;
   var angular=Vector3.ProjectOnPlane(truck.body.angularVelocity,Vector3.up);
   truck.body.AddTorque(Vector3.ClampMagnitude(RightingAxis()*angle*10-angular*3,8),ForceMode.Acceleration);
   if(angle<.08f&&angular.magnitude<.3f)righting=0;
  }
 }
}
