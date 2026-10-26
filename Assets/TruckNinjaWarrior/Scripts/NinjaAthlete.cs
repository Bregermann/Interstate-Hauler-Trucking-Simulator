using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckNinjaWarrior
{
 // NWH retains suspension/vehicle authority; these are bounded game-show abilities.
 public sealed class NinjaAthlete:MonoBehaviour
 {
  public SportsEventTruck truck;public float jumpHeight=4.2f,superJumpHeight=7.5f,jumpCooldown=.9f;
  [Min(.1f)]public float chargeSeconds=.75f;[Range(0,12)]public float chargedForwardSpeed=10;
  public float Compression{get;private set;}public int Jumps{get;private set;}public int SuperJumps{get;private set;}public bool Grounded{get;private set;}
  public float Charge{get;private set;}public bool Charging{get;private set;}
  float[] originalTravel;Vector3 originalCOM;float nextJump;bool previouslyHeld;
  void Cache(){if(originalTravel!=null)return;originalTravel=new float[truck.wheels.Length];for(int i=0;i<originalTravel.Length;i++)originalTravel[i]=truck.wheels[i].spring.maxLength;originalCOM=truck.body.centerOfMass;}
  public void ResetAthlete(){Cache();nextJump=0;Jumps=SuperJumps=0;CancelCharge();}
  public void Restore(){Cache();for(int i=0;i<originalTravel.Length;i++)truck.wheels[i].spring.maxLength=originalTravel[i];truck.body.centerOfMass=originalCOM;}
  public void CancelCharge(){Charge=Compression=0;Charging=previouslyHeld=false;Restore();}
  public void Recover(float now){CancelCharge();nextJump=now+.2f;}
  public static float TakeoffDelta(float currentUp,float height,float gravity)=>Mathf.Clamp(Mathf.Sqrt(2*Mathf.Abs(gravity)*height)-currentUp,0,24);
  public static float ChargedHeight(float shortHeight,float fullHeight,float charge)=>Mathf.Lerp(shortHeight,fullHeight,Mathf.Clamp01(charge));
  public void Step(Vector2 drive,bool jumpHeld,bool jumpPressed,float dt,float now)
  {
   Cache();Grounded=false;foreach(var wheel in truck.wheels)Grounded|=wheel.IsGrounded;
   bool released=previouslyHeld&&!jumpHeld;
   if(jumpHeld&&!previouslyHeld&&Grounded&&now>=nextJump){Charging=true;Charge=0;}
   if(Charging&&jumpHeld){if(Grounded)Charge=Mathf.Clamp01(Charge+dt/Mathf.Max(.1f,chargeSeconds));else{Charging=false;Charge=0;}}
   // A tap can begin and end between physics steps. The established pressed
   // latch preserves it; held charge still launches only on the actual release.
   bool shortTap=jumpPressed&&!jumpHeld&&!previouslyHeld;
   bool launch=Grounded&&now>=nextJump&&(Charging&&released||shortTap);
   float power=Charging?Charge:0;
   if(!jumpHeld){Charging=false;Charge=0;}
   previouslyHeld=jumpHeld;Compression=Charging?Charge:0;
   for(int i=0;i<originalTravel.Length;i++)truck.wheels[i].spring.maxLength=Mathf.Lerp(originalTravel[i],Mathf.Max(.08f,originalTravel[i]*.28f),Compression);truck.body.centerOfMass=originalCOM+new Vector3(drive.x*.22f,-Compression*.15f,0);truck.TickDrive(drive);
   if(launch)
   {
    float height=ChargedHeight(jumpHeight,superJumpHeight,power);float up=Vector3.Dot(truck.body.linearVelocity,Vector3.up);var forward=Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up).normalized;float along=Vector3.Dot(truck.body.linearVelocity,forward);
    float forwardDelta=power>0?Mathf.Clamp(chargedForwardSpeed*power-along,0,chargedForwardSpeed):0;
    truck.tractorMotor.AddAssemblyVelocity(Vector3.up*TakeoffDelta(up,height,Physics.gravity.y)+forward*forwardDelta);nextJump=now+jumpCooldown;Jumps++;if(power>=.65f)SuperJumps++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log("NINJA_TAKEOFF time="+now+" position="+truck.body.position+" velocity="+truck.body.linearVelocity+" targetHeight="+height+" charge="+power+" forwardDelta="+forwardDelta);
#endif
   }
   // Reuse MattressRacer's fore/aft pitch and left/right roll signs and gains;
   // retain this NWH sport's bounded yaw, angular damping and upright assistance.
   if(!Grounded){var upright=Vector3.Cross(truck.transform.up,Vector3.up);var torque=upright*1.7f-truck.body.angularVelocity*.65f+truck.transform.right*(drive.y*.75f)+Vector3.up*(drive.x*.35f)-truck.transform.forward*(drive.x*.3f);truck.body.AddTorque(Vector3.ClampMagnitude(torque,2),ForceMode.Acceleration);}
   else{float roll=Vector3.SignedAngle(Vector3.up,truck.transform.up,truck.transform.forward);float desired=-drive.x*(Compression>.5f?17:8);float torque=Mathf.Clamp(Mathf.DeltaAngle(roll,desired)*.045f-Vector3.Dot(truck.body.angularVelocity,truck.transform.forward)*.6f,-1.8f,1.8f);truck.body.AddTorque(truck.transform.forward*torque,ForceMode.Acceleration);}
  }
  void OnDisable(){if(truck!=null)CancelCharge();}
 }
}
