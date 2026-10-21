using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckNinjaWarrior
{
 // NWH retains suspension/vehicle authority; these are bounded game-show abilities.
 public sealed class NinjaAthlete:MonoBehaviour
 {
  public SportsEventTruck truck;public float jumpHeight=4.2f,superJumpHeight=7.5f,jumpCooldown=.9f;
  public float Compression{get;private set;}public int Jumps{get;private set;}public int SuperJumps{get;private set;}public bool Grounded{get;private set;}
  float[] originalTravel;Vector3 originalCOM;float nextJump,releaseAt=-100,charge;bool previouslyCompressed;
  void Cache(){if(originalTravel!=null)return;originalTravel=new float[truck.wheels.Length];for(int i=0;i<originalTravel.Length;i++)originalTravel[i]=truck.wheels[i].spring.maxLength;originalCOM=truck.body.centerOfMass;}
  public void ResetAthlete(){Cache();Compression=charge=0;releaseAt=-100;nextJump=0;previouslyCompressed=false;Jumps=SuperJumps=0;Restore();}
  public void Restore(){Cache();for(int i=0;i<originalTravel.Length;i++)truck.wheels[i].spring.maxLength=originalTravel[i];truck.body.centerOfMass=originalCOM;}
  public void Recover(float now){Restore();Compression=charge=0;releaseAt=-100;previouslyCompressed=false;nextJump=now+.2f;}
  public static float TakeoffDelta(float currentUp,float height,float gravity)=>Mathf.Clamp(Mathf.Sqrt(2*Mathf.Abs(gravity)*height)-currentUp,0,24);
  public void Step(Vector2 drive,bool compress,bool jump,float dt,float now)
  {
   Cache();Grounded=false;foreach(var wheel in truck.wheels)Grounded|=wheel.IsGrounded;
   if(previouslyCompressed&&!compress){releaseAt=now;charge=Compression;}if(compress)charge=Mathf.Max(charge,Compression);previouslyCompressed=compress;Compression=Mathf.MoveTowards(Compression,compress?1:0,dt*3.5f);
   for(int i=0;i<originalTravel.Length;i++)truck.wheels[i].spring.maxLength=Mathf.Lerp(originalTravel[i],Mathf.Max(.08f,originalTravel[i]*.28f),Compression);truck.body.centerOfMass=originalCOM+new Vector3(drive.x*.22f,-Compression*.15f,0);truck.TickDrive(drive);
   if(jump&&Grounded&&now>=nextJump)
   {
    bool super=charge>.55f&&!compress&&now-releaseAt<=.3f;float height=super?superJumpHeight:jumpHeight;float up=Vector3.Dot(truck.body.linearVelocity,Vector3.up);truck.tractorMotor.AddAssemblyVelocity(Vector3.up*TakeoffDelta(up,height,Physics.gravity.y));Debug.Log("NINJA_TAKEOFF time="+now+" position="+truck.body.position+" velocity="+truck.body.linearVelocity+" targetHeight="+height+" super="+super);nextJump=now+jumpCooldown;Jumps++;if(super)SuperJumps++;charge=0;
   }
   if(!Grounded){var upright=Vector3.Cross(truck.transform.up,Vector3.up);var torque=upright*1.7f-truck.body.angularVelocity*.65f+truck.transform.right*(-drive.y*.45f)+Vector3.up*(drive.x*.35f)+truck.transform.forward*(-drive.x*.35f);truck.body.AddTorque(Vector3.ClampMagnitude(torque,2),ForceMode.Acceleration);}
   else{float roll=Vector3.SignedAngle(Vector3.up,truck.transform.up,truck.transform.forward);float desired=-drive.x*(Compression>.5f?17:8);float torque=Mathf.Clamp(Mathf.DeltaAngle(roll,desired)*.045f-Vector3.Dot(truck.body.angularVelocity,truck.transform.forward)*.6f,-1.8f,1.8f);truck.body.AddTorque(truck.transform.forward*torque,ForceMode.Acceleration);}
  }
  void OnDisable(){if(truck!=null)Restore();}
 }
}
