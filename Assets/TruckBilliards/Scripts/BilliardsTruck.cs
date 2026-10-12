using UnityEngine;
namespace LWS.TruckBilliards {
// Collider-free visual rig: chassis and wheels cannot obstruct balls or the other truck.
// Swept front-bumper contact transfers one capped shot after a fresh separation.
public sealed class BilliardsTruck:MonoBehaviour {
 public BilliardsMatch match;public int side;
 public float maxSpeed=5,acceleration=5,steering=155,minStrikeSpeed=.22f,powerScale=3.2f,maxShotSpeed=15;
 public float Speed{get;private set;} public float throttle,steer;public bool brake,deliberate;
 public bool Armed{get;private set;} bool clear;
 public Vector3 Bumper=>transform.position+transform.forward*.5f;
 public void ResetTruck(Vector3 p,Quaternion q){transform.SetPositionAndRotation(p,q);Speed=0;throttle=steer=0;brake=false;deliberate=false;clear=false;Armed=false;}
 public void Disarm(){clear=false;Armed=false;}
 public void Stop(){Speed=0;throttle=steer=0;Armed=false;}
 void FixedUpdate(){
  if(match==null||match.balls==null)return;
  bool enabled=(match.session==null||match.session.GameplayEnabled)&&match.rules.Phase!=MatchPhase.Menu&&match.rules.Phase!=MatchPhase.Placement&&match.rules.Phase!=MatchPhase.Ended;
  if(!enabled){Stop();return;}
  Vector3 old=Bumper;
  Speed=Mathf.MoveTowards(Speed,brake?0:throttle*maxSpeed,(brake?acceleration*3:acceleration)*Time.fixedDeltaTime);
  if(Mathf.Abs(throttle)<.01f)Speed=Mathf.MoveTowards(Speed,0,acceleration*.4f*Time.fixedDeltaTime);
  transform.Rotate(0,steer*steering*Mathf.Clamp01(Mathf.Abs(Speed)/.8f)*Mathf.Sign(Speed)*Time.fixedDeltaTime,0);
  var p=transform.position+transform.forward*Speed*Time.fixedDeltaTime;
  p.x=Mathf.Clamp(p.x,-match.halfWidth+.65f,match.halfWidth-.65f);p.z=Mathf.Clamp(p.z,-match.halfLength+.65f,match.halfLength-.65f);transform.position=p;
  var cue=match.balls[0];var delta=cue.body.position-Bumper;delta.y=0;
  bool allowed=match.rules.Phase==MatchPhase.Aiming&&match.rules.Turn==side&&match.AreSettled&&!cue.Pocketed&&(match.session==null?(side==0||deliberate):(!match.session.IsAI(side)||deliberate));
  if(!allowed){clear=false;Armed=false;return;}
  if(delta.magnitude>.78f)clear=true;Armed=clear;
  if(!Armed||Speed<minStrikeSpeed)return;
  var segment=Bumper-old;segment.y=0;var from=cue.body.position-old;from.y=0;
  float t=segment.sqrMagnitude>.00001f?Mathf.Clamp01(Vector3.Dot(from,segment)/segment.sqrMagnitude):0;
  var hit=cue.body.position-(old+segment*t);hit.y=0;
  if(hit.magnitude<=.57f&&Vector3.Dot(from.normalized,transform.forward)>.78f&&Vector3.Dot(segment,transform.forward)>0){
   clear=false;Armed=false;match.Strike(this,transform.forward*Mathf.Min(maxShotSpeed,Speed*powerScale));
  }
 }
}}

