using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckWaterSports {
public sealed class WaterVessel:MonoBehaviour {
 public event System.Action<WaterVessel,Collision> HullImpact;
 public Rigidbody body;public SportsFloatingBody flotation;public WaterCourse course;public Transform[] paddles;public Renderer[] firstPaint=System.Array.Empty<Renderer>(),secondPaint=System.Array.Empty<Renderer>();public int firstSlot,secondSlot=-1;
 public float strokeImpulse=2.5f,turnAuthority=1;public int Capsizes{get;private set;}public int Synchronizations{get;private set;}public int Strokes{get;private set;}public int Collisions{get;private set;}public int OpponentBumps{get;private set;}public int CreditedBumps{get;private set;}public int CausedCapsizes{get;private set;}public int Landings{get;private set;}public int Rolls{get;private set;}public float TrickPoints{get;private set;}public float AirborneRotation{get;private set;}public float LastLandingQuality{get;private set;}public float AirborneSeconds=>airTime;
 public bool Capsized=>Vector3.Dot(transform.up,Vector3.up)<.1f;
 readonly float[] nextStroke=new float[2],lastStroke={-10,-10};readonly int[] stationStrokes=new int[2];[SerializeField]Vector3 start;[SerializeField]Quaternion startRotation=Quaternion.identity;float inverted,airTime,nextRecovery,nextCollision,rollAngle;bool wasWet=true;float[] paddleAngles=new float[2];
 readonly System.Collections.Generic.Dictionary<WaterVessel,float> contacts=new System.Collections.Generic.Dictionary<WaterVessel,float>(4);Vector3 previousVelocity;WaterVessel lastAttacker;float lastAttackedAt;
 public int StrokesAtStation(int side)=>stationStrokes[Mathf.Clamp(side,0,1)];
 bool boundedWater;Bounds lakeFootprint;
 // Freestyle alone opts into actual plan-position river elevation so gravity
 // can carry a kayak off a waterfall lip. Other modes retain their accepted path.
 [System.NonSerialized]public bool useHorizontalSurface;
 // Only the individual kayak/barge modes opt in. The accepted canoe remains unchanged.
 public void BoundWaterSupport(Bounds footprint){boundedWater=true;lakeFootprint=footprint;}
 public bool WaterSupported{
  get{
   if(!boundedWater)return true;
   Vector3 p=body.position;
   if(course.lake)return p.x>=lakeFootprint.min.x&&p.x<=lakeFootprint.max.x&&p.z>=lakeFootprint.min.z&&p.z<=lakeFootprint.max.z;
   float nearest=float.MaxValue;
   for(int i=0;i<course.stations.Length-1;i++){
    Vector3 a=course.Point(i),segment=Vector3.ProjectOnPlane(course.Point(i+1)-a,Vector3.up),offset=Vector3.ProjectOnPlane(p-a,Vector3.up);
    float fraction=segment.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(offset,segment)/segment.sqrMagnitude):0;
    nearest=Mathf.Min(nearest,(offset-segment*fraction).sqrMagnitude);
   }
   return nearest<=course.width*course.width*.25f;
  }
 }
 public void SteerWatercraft(float input){
  if(body.isKinematic||!WaterSupported||body.position.y>flotation.waterHeight+1.5f)return;
  // Continuous paddle/rudder authority must not be a one-frame torque hidden
  // behind the .42-second stroke cooldown. Rate feedback arrests overshoot.
  float yawRate=Vector3.Dot(body.angularVelocity,Vector3.up);
  body.AddTorque(Vector3.up*(Mathf.Clamp(input,-1,1)*1.5f-yawRate)*4,ForceMode.Acceleration);
 }
 public void Capture(){start=transform.position;startRotation=transform.rotation;}
 public void SetColors(Color first,Color second){var block=new MaterialPropertyBlock();for(int seat=0;seat<2;seat++){block.SetColor("_BaseColor",seat==0?first:second);block.SetColor("_Color",seat==0?first:second);foreach(var renderer in seat==0?firstPaint:secondPaint)if(renderer!=null)renderer.SetPropertyBlock(block);}}
 public void ResetVessel(){body.isKinematic=false;body.position=start;body.rotation=startRotation;body.linearVelocity=body.angularVelocity=Vector3.zero;Capsizes=Synchronizations=Strokes=Collisions=OpponentBumps=CreditedBumps=CausedCapsizes=Landings=Rolls=0;System.Array.Clear(stationStrokes,0,stationStrokes.Length);contacts.Clear();previousVelocity=Vector3.zero;lastAttacker=null;lastAttackedAt=-10;TrickPoints=AirborneRotation=LastLandingQuality=airTime=inverted=nextRecovery=nextCollision=rollAngle=0;lastStroke[0]=lastStroke[1]=-10;nextStroke[0]=nextStroke[1]=0;flotation.enabled=true;wasWet=true;}
 public void Tick(float dt,float clock){if(body.isKinematic)return;previousVelocity=body.linearVelocity;flotation.Clock=clock;flotation.waterHeight=(useHorizontalSurface?course.SurfacePointXZ(body.position):course.SurfacePoint(body.position)).y;bool supported=WaterSupported;if(boundedWater)flotation.enabled=supported;var flow=course.Current(body.position,clock);if(supported&&body.position.y<flotation.waterHeight+1.5f)body.AddForce((flow-body.linearVelocity)*.6f,ForceMode.Acceleration);if(Capsized){inverted+=dt;if(inverted>1&&inverted-dt<=1){Capsizes++;if(lastAttacker!=null&&clock-lastAttackedAt<5){lastAttacker.CausedCapsizes++;lastAttacker=null;}}}else inverted=0;
 bool wet=supported&&body.position.y<flotation.waterHeight+1;if(!wet){airTime+=dt;AirborneRotation+=body.angularVelocity.magnitude*dt;}else if(!wasWet){if(airTime>.3f){LastLandingQuality=Mathf.Clamp01(Vector3.Dot(transform.up,Vector3.up))*(1+AirborneRotation);TrickPoints+=LastLandingQuality*100;Landings++;}airTime=AirborneRotation=0;}wasWet=wet;rollAngle+=Vector3.Dot(body.angularVelocity,transform.forward)*dt;if(Mathf.Abs(rollAngle)>=Mathf.PI*1.8f&&!Capsized){Rolls++;TrickPoints+=40;rollAngle=0;}
 for(int i=0;i<paddles.Length;i++){paddleAngles[i]=Mathf.MoveTowards(paddleAngles[i],0,dt*220);paddles[i].localRotation=Quaternion.Euler(paddleAngles[i],0,i==0?20:-20);}}
 public bool Stroke(int side,float strength,float clock,float steer=0){side=Mathf.Clamp(side,0,1);if(clock<nextStroke[side]||Mathf.Abs(strength)<.05f)return false;nextStroke[side]=clock+.42f;lastStroke[side]=clock;Strokes++;stationStrokes[side]++;if(Mathf.Abs(lastStroke[1-side]-clock)<.16f)Synchronizations++;Vector3 point=body.worldCenterOfMass+transform.right*(side==0?-1:1)*.75f;body.AddForceAtPosition(transform.forward*Mathf.Clamp(strength,-1,1)*strokeImpulse*body.mass,point,ForceMode.Impulse);body.AddTorque(Vector3.up*steer*turnAuthority,ForceMode.Acceleration);paddleAngles[side]=105;return true;}
 public void Brace(float direction,float dt){body.AddTorque(transform.forward*-direction*2,ForceMode.Acceleration);}
 public void Recover(){if(Capsized&&flotation.Clock>=nextRecovery){nextRecovery=flotation.Clock+1.2f;body.AddTorque(transform.forward*(body.rotation.eulerAngles.z<180?-5:5),ForceMode.VelocityChange);}}
 public void Roll(){if(flotation.Clock>=nextRecovery){nextRecovery=flotation.Clock+1.2f;body.AddTorque(transform.forward*5,ForceMode.VelocityChange);}}
 public void AirBrace(float strength){if(airTime>.02f)body.AddTorque(Vector3.Cross(transform.up,Vector3.up)*strength-body.angularVelocity*2,ForceMode.Acceleration);}
 public bool Touching(WaterVessel vessel)=>contacts.TryGetValue(vessel,out float when)&&Time.fixedTime-when<.12f;
 WaterVessel ObserveContact(Collision collision){var other=collision.rigidbody!=null?collision.rigidbody.GetComponent<WaterVessel>():null;if(other!=null&&other!=this&&other.flotation!=null&&flotation!=null&&other.flotation.session==flotation.session)contacts[other]=Time.fixedTime;return other;}
 void OnCollisionStay(Collision collision)=>ObserveContact(collision);
 void OnCollisionEnter(Collision collision){if(body!=null&&!body.isKinematic)HullImpact?.Invoke(this,collision);var other=ObserveContact(collision);if(flotation==null||body.isKinematic||collision.relativeVelocity.magnitude<2||flotation.Clock<nextCollision)return;nextCollision=flotation.Clock+.8f;Collisions++;if(other==null)return;OpponentBumps++;var normal=collision.contactCount>0?collision.GetContact(0).normal:(body.position-other.body.position).normalized;float ownClosing=Mathf.Max(0,Vector3.Dot(previousVelocity,-normal)),otherClosing=Mathf.Max(0,Vector3.Dot(other.previousVelocity,normal));if(ownClosing>otherClosing+.75f){CreditedBumps++;other.lastAttacker=this;other.lastAttackedAt=flotation.Clock;}}
 public void Freeze(){flotation.enabled=false;body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;}
}
}
