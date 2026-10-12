using UnityEngine;
namespace LWS.TruckBilliards {
public sealed class BilliardsAI:MonoBehaviour {
 public BilliardsMatch match;public BilliardsTruck truck;
 [System.Serializable] public sealed class DifficultyTuning {
  public float aimErrorDegrees=1.7f,powerErrorFraction=.12f,minimumPotAlignment=.25f,maxPotDistance=30;
  public bool simpleSelection,checkApproach;public float fallbackThrottle=.55f;
 }
 public DifficultyTuning easy=new DifficultyTuning{aimErrorDegrees=5.5f,powerErrorFraction=.28f,minimumPotAlignment=.8f,maxPotDistance=8,simpleSelection=true};
 public DifficultyTuning normal=new DifficultyTuning();
 public DifficultyTuning hard=new DifficultyTuning{aimErrorDegrees=.55f,powerErrorFraction=.045f,minimumPotAlignment=.32f,checkApproach=true};
 public float approachTimeout=12;
 public struct ShotPlan {public Vector3 direction,staging;public float power,aimError,powerError,score;public int target,pocket,candidates;public bool fallback,valid;}
 public ShotPlan LastPlan{get;private set;}public int Plans{get;private set;}
 public DifficultyTuning Tuning(BilliardsDifficulty level)=>level==BilliardsDifficulty.Easy?easy:level==BilliardsDifficulty.Hard?hard:normal;
 public int maxRecoveries=3;
 enum Stage{Wait,Staging,Align,Approach,Recover,Roam}
 static readonly Vector3[] roamPoints={new Vector3(-4.2f,0,-9.4f),new Vector3(4.2f,0,-9.4f),new Vector3(4.2f,0,9.4f),new Vector3(-4.2f,0,9.4f)};
 Stage stage;Vector3 staging,direction;float timer,power,wait,progressTimer,bestProgress=float.PositiveInfinity;int tries,planningShot=-1,roamPoint;bool ownWasActive;
 public string ExecutionState=>stage.ToString();public int RecoveryAttempts=>tries;public float ManeuverSeconds=>timer;public bool Roaming=>stage==Stage.Roam;
 public void ResetPlan(){stage=Stage.Wait;timer=wait=progressTimer=0;bestProgress=float.PositiveInfinity;tries=0;planningShot=-1;ownWasActive=false;truck.deliberate=false;truck.Stop();}
 void FixedUpdate(){
  var session=match.session;int side=truck.side;
  if(session==null||!session.GameplayEnabled||!session.IsAI(side)||!truck.gameObject.activeInHierarchy||(match.rules.Phase!=MatchPhase.Aiming&&match.rules.Phase!=MatchPhase.Resolving)){truck.deliberate=false;truck.throttle=truck.steer=0;truck.brake=true;ownWasActive=false;return;}
  bool own=match.rules.Turn==side&&match.rules.Phase==MatchPhase.Aiming&&match.AreSettled;
  if(!own){ownWasActive=false;Roam();return;}
  if(!ownWasActive||planningShot!=match.rules.ShotId){ResetPlan();ownWasActive=true;planningShot=match.rules.ShotId;}
  truck.brake=false;timer+=Time.fixedDeltaTime;
  if(stage==Stage.Wait){wait+=Time.fixedDeltaTime;if(wait>=.65f)Plan();return;}
  if(stage==Stage.Recover){truck.deliberate=false;truck.throttle=-.42f;truck.steer=tries%2==0?.55f:-.55f;if(timer>=1.1f)Plan();return;}
  if(timer>approachTimeout){FailApproach("maneuver timeout");return;}
  if(stage==Stage.Staging){var delta=staging-truck.transform.position;delta.y=0;if(!Progress(delta.magnitude))return;if(delta.magnitude<.22f){truck.Stop();stage=Stage.Align;ResetManeuver();return;}DriveToward(staging,Mathf.Clamp(delta.magnitude*.55f,.12f,.65f));return;}
  if(stage==Stage.Align){float angle=Vector3.SignedAngle(truck.transform.forward,direction,Vector3.up);if(!Progress(Mathf.Abs(angle)*.04f))return;if(Mathf.Abs(angle)<4){truck.Stop();stage=Stage.Approach;ResetManeuver();truck.deliberate=true;return;}truck.steer=Mathf.Clamp(angle/25,-1,1);truck.throttle=.16f;if(Vector3.Distance(truck.transform.position,staging)>.9f)FailApproach("alignment moved outside staging");return;}
  if(stage==Stage.Approach){var d=match.balls[0].body.position-truck.transform.position;d.y=0;if(!Progress(d.magnitude))return;float angle=Vector3.SignedAngle(truck.transform.forward,d.normalized,Vector3.up);truck.steer=Mathf.Clamp(angle/20,-.4f,.4f);truck.throttle=power;truck.deliberate=true;if(Vector3.Dot(d,direction)<-.2f)FailApproach("bumper passed cue");}
 }
 void ResetManeuver(){timer=progressTimer=0;bestProgress=float.PositiveInfinity;}
 bool Progress(float distance){if(distance<bestProgress-.05f){bestProgress=distance;progressTimer=0;}else progressTimer+=Time.fixedDeltaTime;if(progressTimer>3.5f){FailApproach("no maneuver progress");return false;}return true;}
 void Roam(){truck.deliberate=false;stage=Stage.Roam;var target=roamPoints[(roamPoint+truck.side*2)%4];if(Vector3.Distance(truck.transform.position,target)<.6f)roamPoint=(roamPoint+1)%4;truck.brake=false;DriveToward(target,match.rules.LastFoul?.2f:.3f);}
 void DriveToward(Vector3 target,float speed){var d=target-truck.transform.position;d.y=0;float angle=Vector3.SignedAngle(truck.transform.forward,d,Vector3.up);bool reverse=Mathf.Abs(angle)>105;if(reverse)angle=Mathf.DeltaAngle(0,angle-180);truck.steer=Mathf.Clamp(angle/25,-1,1)*(reverse?-1:1);truck.throttle=(reverse?-1:1)*speed;}
 bool Clear(Vector3 a,Vector3 b,int ignore){
  var d=b-a;d.y=0;if(d.sqrMagnitude<.001f)return true;
  foreach(var ball in match.balls){if(ball.Pocketed||ball.number==0||ball.number==ignore)continue;
   var v=ball.body.position-a;v.y=0;float t=Vector3.Dot(v,d)/d.sqrMagnitude;
   if(t>.05f&&t<.96f&&(v-d*t).magnitude<1.03f)return false;
  }return true;
 }
 public ShotPlan EvaluateLayout(BilliardsDifficulty level,int seed)=>EvaluateAt(level,seed,match.balls[0].body.position);
 ShotPlan EvaluateAt(BilliardsDifficulty level,int seed,Vector3 cue){
  var tuning=Tuning(level);cue.y=0;
  var plan=new ShotPlan{direction=Vector3.forward,power=tuning.fallbackThrottle,target=-1,pocket=-1,score=float.NegativeInfinity};
  foreach(var b in match.balls){
   if(b.Pocketed||!match.rules.LegalTarget(b.number))continue;
   var pos=b.body.position;pos.y=0;
   for(int i=0;i<match.pockets.Length;i++){
    var pocket=match.pockets[i];float objectDistance=Vector3.Distance(pos,pocket);if(objectDistance>tuning.maxPotDistance)continue;
    var pot=(pocket-pos).normalized;var ghost=pos-pot*1.02f;var dir=(ghost-cue).normalized;var start=cue-dir*2.7f;
    float alignment=Vector3.Dot(dir,pot);
    if(alignment<tuning.minimumPotAlignment||!Inside(start)||!Clear(cue,ghost,b.number)||!Clear(pos,pocket,b.number))continue;
    if(tuning.checkApproach&&(!Inside(ghost)||!Clear(start,cue,b.number)))continue;
    plan.candidates++;float distance=Vector3.Distance(cue,pos);
    float score=alignment*12-distance*.18f-objectDistance*.12f;
    if(tuning.checkApproach){float railSpace=Mathf.Min(match.halfWidth-Mathf.Abs(start.x),match.halfLength-Mathf.Abs(start.z));score+=Mathf.Min(railSpace,2)*.8f;score-=Vector3.Distance(start,truck.transform.position)*.06f;score-=Mathf.Pow(1-alignment,2)*5;}
    if(score>plan.score){plan.score=score;plan.direction=dir;plan.target=b.number;plan.pocket=i;plan.power=Mathf.Clamp((distance+objectDistance)*.045f/(tuning.checkApproach?Mathf.Max(.65f,alignment):1),.4f,.88f);}
   }
   if(tuning.simpleSelection&&plan.target>=0)break;
  }
  if(plan.target<0){
   plan.fallback=true;float nearest=float.PositiveInfinity;
   foreach(var b in match.balls){if(b.Pocketed||!match.rules.LegalTarget(b.number))continue;var pos=b.body.position;pos.y=0;var dir=(pos-cue).normalized;
    // Prefer legal reachable contact; shorter run-up makes near-rail contacts possible.
    if(!Inside(cue-dir*1.55f)||!Clear(cue,pos,b.number))continue;
    float distance=Vector3.Distance(cue,pos);if(distance<nearest){nearest=distance;plan.direction=dir;plan.target=b.number;plan.power=Mathf.Clamp(distance*.06f,.35f,.7f);}}
  }
  if(plan.target<0)return plan;
  var random=new System.Random(seed);plan.aimError=((float)random.NextDouble()*2-1)*tuning.aimErrorDegrees;plan.powerError=((float)random.NextDouble()*2-1)*tuning.powerErrorFraction;
  plan.direction=Quaternion.Euler(0,plan.aimError,0)*plan.direction;plan.power=Mathf.Clamp(plan.power*(1+plan.powerError),.25f,1);
  float runup=tuning.checkApproach?Mathf.Lerp(1.9f,2.9f,plan.power):2.7f;
  plan.staging=cue-plan.direction*runup;
  if(!Inside(plan.staging))plan.staging=cue-plan.direction*1.55f;
  plan.valid=Inside(plan.staging);return plan;
 }
 void Plan(){
  truck.deliberate=false;ResetManeuver();var level=match.session.DifficultyFor(truck.side);
  LastPlan=EvaluateLayout(level,Random.Range(0,int.MaxValue));
  if(tries>=maxRecoveries||!LastPlan.valid)LastPlan=ContactPlan(match.balls[0].body.position);
  Plans++;direction=LastPlan.direction;staging=LastPlan.staging;power=LastPlan.power;
  if(!LastPlan.valid){FailApproach("contact geometry needs separation");return;}
  stage=Stage.Staging;
  Debug.Log("TB_AI_PLAN side="+truck.side+" difficulty="+level+" target="+LastPlan.target+" fallback="+LastPlan.fallback+" attempts="+tries+" error="+LastPlan.aimError+" staging="+staging+" power="+power);
 }
 ShotPlan ContactPlan(Vector3 cue){cue.y=0;var best=new ShotPlan{target=-1,pocket=-1,fallback=true,score=float.NegativeInfinity};Vector3 from=cue-truck.transform.position;from.y=0;float baseAngle=Mathf.Atan2(from.x,from.z)*Mathf.Rad2Deg;for(int i=0;i<24;i++){var dir=Quaternion.Euler(0,baseAngle+i*15+tries*31,0)*Vector3.forward;var start=cue-dir*1.65f;if(!Inside(start))continue;float score=-Vector3.Distance(truck.transform.position,start)-Mathf.Abs(Vector3.SignedAngle(truck.transform.forward,dir,Vector3.up))*.005f;if(score<=best.score)continue;best.direction=dir;best.staging=start;best.power=.6f;best.score=score;best.valid=true;}return best;}
 public Vector3 FindCuePlacement(){Vector3 best=match.FindPlacement(0);float cost=float.PositiveInfinity;for(float z=-9;z<=9;z+=1.5f)for(float x=-4.5f;x<=4.5f;x+=1.5f){var p=new Vector3(x,.5f,z);if(!match.ValidPlacement(p,0))continue;var plan=EvaluateAt(match.session.DifficultyFor(truck.side),0,p);if(!plan.valid)plan=ContactPlan(p);if(!plan.valid)continue;float candidate=Vector3.Distance(truck.transform.position,plan.staging)+Mathf.Abs(x)*.2f;if(candidate<cost){cost=candidate;best=p;}}return best;}
 void FailApproach(string reason){tries++;truck.deliberate=false;stage=Stage.Recover;ResetManeuver();Debug.Log("TB_AI_RECOVERY side="+truck.side+" attempt="+tries+" reason="+reason+" cue="+match.balls[0].body.position+" truck="+truck.transform.position+" armed="+truck.Armed);}
 bool Inside(Vector3 p)=>Mathf.Abs(p.x)<match.halfWidth-.75f&&Mathf.Abs(p.z)<match.halfLength-.75f;
}}