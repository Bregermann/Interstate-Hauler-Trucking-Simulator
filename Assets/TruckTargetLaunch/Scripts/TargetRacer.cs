using UnityEngine;
using DG.Tweening;
using LWS.TruckSports;
namespace LWS.TruckTargetLaunch {
public enum TargetFlight{Approach,Airborne,Passive,Resolved}
public sealed class TargetRacer:MonoBehaviour {
 public TargetMatch match;public SportsEventTruck truck;public Transform[] wings;public ParticleSystem crash;public AudioSource wind;public int slot;
 [Min(.5f)]public float stabilityDuration=1.8f;
 [Min(.01f)]public float stableLinearSpeed=.12f,stableAngularSpeed=.15f;
 [Min(10)]public float maximumSettleTime=60;
 [Min(0)]public float wingTorque=.16f,maximumCorrectionSpeed=.6f;
 public TargetFlight Phase{get;private set;}
 public bool Dived{get;private set;}public bool Destroyed{get;private set;}
 public float FlightAge{get;private set;}public float Age{get;private set;}
 public Vector3 LaunchVelocity{get;private set;}public Vector3 LandingPoint{get;private set;}
 public int TopContacts{get;private set;}public int AirContacts{get;private set;}
 public float StableTime{get;private set;}public bool Supported{get;private set;}
 public bool ReadyToScore=>Phase==TargetFlight.Passive&&Supported&&StableTime>=stabilityDuration;
 public string ResolutionReason{get;private set;}
 public readonly SportsLimitedAftertouch touch=new SportsLimitedAftertouch();
 readonly Tween[] folds=new Tween[2];readonly RaycastHit[] swept=new RaycastHit[24];readonly ContactPoint[] supports=new ContactPoint[32];
 Vector3 previous,motionPrevious;Quaternion previousRotation,motionRotation;float contactAge;bool touchedTarget,targetSupporting;
 public void ResetRacer(Vector3 position){
  foreach(var tween in folds)tween?.Kill();Dived=Destroyed=touchedTarget=Supported=targetSupporting=false;ResolutionReason="";
  LaunchVelocity=LandingPoint=Vector3.zero;touch.Reset(1.6f,Vector2.zero);Age=FlightAge=StableTime=contactAge=0;TopContacts=AirContacts=0;Phase=TargetFlight.Approach;
  for(int i=0;i<wings.Length;i++)wings[i].localRotation=Quaternion.Euler(0,0,i==0?-80:80);
  truck.visuals.gameObject.SetActive(true);truck.Prepare(position,Quaternion.Euler(13,0,0));
  previous=motionPrevious=truck.body.position;previousRotation=motionRotation=truck.body.rotation;
  crash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
 }
 void Wings(bool open){for(int i=0;i<wings.Length;i++){folds[i]?.Kill();folds[i]=wings[i].DOLocalRotate(new Vector3(0,0,open?0:i==0?-80:80),.35f).SetEase(Ease.OutCubic);}}
 public void Tick(Vector2 drive,bool dive,float dt){
  Age+=dt;
  if(Phase==TargetFlight.Approach){
   truck.TickDrive(drive);
   if(truck.body.position.z>72&&!truck.Grounded&&truck.body.linearVelocity.z>1){Phase=TargetFlight.Airborne;FlightAge=0;LaunchVelocity=truck.body.linearVelocity;touch.Reset(1.6f,drive);truck.Power(false);Wings(true);match.Launched(slot,LaunchVelocity);}
   if(Age>35)ResolveMiss("APPROACH_TIMEOUT",false);
  }else if(Phase==TargetFlight.Airborne){
   float before=touch.Spent;touch.Tick(truck.body,drive,dt);
   float authority=dt>0?Mathf.Clamp01((touch.Spent-before)/(.65f*dt)):0;
   var torque=new Vector3(touch.RangeArmed?-drive.y:0,drive.x*.35f,-drive.x*.65f)*wingTorque*authority;
   // Limit only additional commanded rotation; never erase collision-induced angular motion.
   if(torque.sqrMagnitude>0&&Vector3.Dot(truck.body.angularVelocity,torque.normalized)<maximumCorrectionSpeed)truck.body.AddTorque(torque,ForceMode.Acceleration);
   if(dive&&!Dived){Dived=true;Wings(false);match.Dived(slot);}
   float lift=Dived?-.18f:.24f;truck.body.AddForce(Vector3.up*(Physics.gravity.magnitude*lift),ForceMode.Acceleration);
   var horizontal=Vector3.ProjectOnPlane(truck.body.linearVelocity,Vector3.up);truck.body.AddForce((match.CurrentWind-horizontal)*.018f,ForceMode.Acceleration);
  }
  if(Phase==TargetFlight.Airborne||Phase==TargetFlight.Passive){
   FlightAge+=dt;
   if(touchedTarget){contactAge+=dt;ObserveRest(dt);if(!ReadyToScore&&contactAge>maximumSettleTime)ResolveMiss("PERSISTENT_MOTION_TIMEOUT",false);}
   else if(FlightAge>30)ResolveMiss("FLIGHT_TIMEOUT",false);
   var p=truck.body.position;
   if(Mathf.Abs(p.x)>1100||Mathf.Abs(p.z-130)>1100||p.y< -250)ResolveMiss("TRUE_WORLD_ESCAPE",false);
  }
  motionPrevious=truck.body.position;motionRotation=truck.body.rotation;
 }
 void ObserveRest(float dt){
  Supported=targetSupporting&&match.targetCollider.enabled&&match.targetCollider.gameObject.activeInHierarchy;
  bool quiet=Supported&&truck.body.linearVelocity.sqrMagnitude<=stableLinearSpeed*stableLinearSpeed&&truck.body.angularVelocity.sqrMagnitude<=stableAngularSpeed*stableAngularSpeed;
  quiet&=Vector3.Distance(truck.body.position,motionPrevious)<.025f&&Quaternion.Angle(truck.body.rotation,motionRotation)<.6f;
  StableTime=quiet?StableTime+dt:0;
 }
 void TouchTarget(){if(Phase==TargetFlight.Resolved||Phase==TargetFlight.Approach)return;if(touchedTarget)return;touchedTarget=true;contactAge=0;Phase=TargetFlight.Passive;truck.Power(false);Wings(false);}
 public void FinalizeRest(){
  if(!ReadyToScore||match.rules.resolved[slot])return;
  LandingPoint=truck.body.worldCenterOfMass;ResolutionReason="FINAL_REST";Phase=TargetFlight.Resolved;
  match.Lock(slot,match.ScoreAt(LandingPoint),LandingPoint);
 }
 public void LavaImpact(Vector3 point){if(Phase==TargetFlight.Resolved)return;LandingPoint=point;Destroyed=true;match.volcano?.Explode(this,point);ResolveMiss("ACTUAL_LAVA_CONTACT",false);truck.visuals.gameObject.SetActive(false);truck.body.detectCollisions=false;truck.body.isKinematic=true;}
 void ResolveMiss(string reason,bool burst){
  if(Phase==TargetFlight.Resolved)return;Phase=TargetFlight.Resolved;ResolutionReason=reason;StableTime=0;truck.Power(false);Wings(false);LandingPoint=truck.body.position;
  if(burst){crash.transform.position=truck.body.position;crash.Play();}
  Debug.Log("TARGET_RESOLVE slot="+slot+" reason="+reason+" position="+LandingPoint+" velocity="+truck.body.linearVelocity+" angular="+truck.body.angularVelocity);
  match.Lock(slot,0,LandingPoint);
 }
 public void ObservePastStep(){
  if(Phase==TargetFlight.Airborne||Phase==TargetFlight.Passive){match.volcano?.ObserveSweep(this,previous,previousRotation);if(Phase!=TargetFlight.Resolved)SweptLanding();}
  previous=truck.body.position;previousRotation=truck.body.rotation;
 }
 void SweptLanding(){
  var delta=truck.body.position-previous;if(delta.y>=-.001f||delta.sqrMagnitude<.0001f)return;var shape=truck.shape;Vector3 origin=previous+previousRotation*shape.center;
  int n=Physics.BoxCastNonAlloc(origin,shape.size*.5f,delta.normalized,swept,previousRotation,delta.magnitude,1,QueryTriggerInteraction.Ignore);
  if(n==swept.Length)throw new System.InvalidOperationException("Target landing sweep buffer saturated");
  for(int i=0;i<n;i++)if(swept[i].collider==match.targetCollider&&swept[i].normal.y>.6f){TouchTarget();break;}
  // A swept top crossing disables aftertouch; it never teleports, changes momentum
  // or awards points. Only an actually supported, continuously stable body scores.
 }
 void SupportContacts(Collision collision){
  if(collision.contactCount>supports.Length)throw new System.InvalidOperationException("Target support contact buffer saturated");
  int count=collision.GetContacts(supports);targetSupporting=false;
  for(int i=0;i<count;i++){var c=supports[i];var normal=c.otherCollider==match.targetCollider?c.normal:-c.normal;if(normal.y>.6f&&c.point.y>=match.target.position.y-.15f){targetSupporting=true;break;}}
 }
 void OnCollisionStay(Collision collision){if(collision.collider==match.targetCollider)SupportContacts(collision);}
 void OnCollisionExit(Collision collision){if(collision.collider==match.targetCollider){targetSupporting=false;Supported=false;StableTime=0;}}
 void OnCollisionEnter(Collision collision){
  if(!match.Live||!match.RoundLive)return;var other=collision.rigidbody!=null?collision.rigidbody.GetComponent<TargetRacer>():null;
  if(other!=null&&(Phase==TargetFlight.Airborne||other.Phase==TargetFlight.Airborne))AirContacts++;
  if(collision.collider==match.targetCollider){TouchTarget();SupportContacts(collision);for(int i=0;i<collision.contactCount;i++)if(collision.GetContact(i).normal.y>.6f){TopContacts++;break;}match.Impact(slot,collision.relativeVelocity.magnitude);}
  else if(collision.collider==match.missGround&&match.volcano==null&&Phase!=TargetFlight.Resolved)ResolveMiss("ACTUAL_MISS_GROUND",true);
 }
 void Update(){if(wind==null)return;float volume=match.Live&&Phase==TargetFlight.Airborne&&Application.isFocused?match.settings.sfx*.05f:0;wind.volume=volume;wind.pitch=.7f+truck.body.linearVelocity.magnitude*.015f;if(volume>.001f){if(!wind.isPlaying)wind.Play();}else wind.Stop();}
 public void Freeze(){foreach(var tween in folds)tween?.Pause();truck.Power(false);truck.body.isKinematic=true;if(wind!=null)wind.Stop();crash.Pause();}
 void OnDisable(){foreach(var tween in folds)tween?.Kill();if(wind!=null)wind.Stop();}
}
}
