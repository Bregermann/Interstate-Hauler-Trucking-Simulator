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
 enum Stage{Wait,Staging,Align,Approach,Recover}
 Stage stage;Vector3 staging,direction;float timer,power;int tries;float wait;
 public void ResetPlan(){stage=Stage.Wait;timer=wait=0;tries=0;truck.deliberate=false;truck.Stop();}
 void FixedUpdate(){
  if((match.session!=null&&(!match.session.GameplayEnabled||!match.session.IsAI(1)))||match.rules.Phase!=MatchPhase.Aiming||match.rules.Turn!=1||!match.AreSettled){truck.deliberate=false;truck.throttle=truck.steer=0;truck.brake=true;stage=Stage.Wait;wait=0;return;}
  truck.brake=false;timer+=Time.fixedDeltaTime;
  if(stage==Stage.Wait){wait+=Time.fixedDeltaTime;if(wait<1)return;tries=0;Plan();return;}
  if(timer>approachTimeout){FailApproach("approach timed out");return;}
  if(stage==Stage.Recover){truck.throttle=-.4f;truck.steer=.7f;if(timer>1.2f)Plan();return;}
  if(stage==Stage.Staging){
   var delta=staging-truck.transform.position;delta.y=0;
   if(delta.magnitude<.3f){truck.Stop();stage=Stage.Align;timer=0;return;}
   DriveToward(staging,Mathf.Clamp(delta.magnitude*.5f,.15f,.55f));return;
  }
  if(stage==Stage.Align){
   // Reposition with steering while moving, never rotate/teleport a truck into impact.
   float angle=Vector3.SignedAngle(truck.transform.forward,direction,Vector3.up);
   if(Mathf.Abs(angle)<4){truck.Stop();stage=Stage.Approach;timer=0;truck.deliberate=true;return;}
   truck.steer=Mathf.Clamp(angle/25,-1,1);truck.throttle=.16f;
   if(Vector3.Distance(truck.transform.position,staging)>.85f){stage=Stage.Staging;staging-=direction*.6f;}
   return;
  }
  if(stage==Stage.Approach){
   var cue=match.balls[0].body.position;cue.y=0;
   var d=cue-truck.transform.position;d.y=0;
   float angle=Vector3.SignedAngle(truck.transform.forward,d.normalized,Vector3.up);
   truck.steer=Mathf.Clamp(angle/20,-.4f,.4f);truck.throttle=power;truck.deliberate=true;
   if(Vector3.Dot(d,direction)<-.2f)FailApproach("bumper missed the cue ball");
  }
 }
 void DriveToward(Vector3 p,float speed){
  var d=p-truck.transform.position;d.y=0;float angle=Vector3.SignedAngle(truck.transform.forward,d,Vector3.up);
  // Reverse when the staging point is behind; tighter low-speed turns remain bounded.
  bool reverse=Mathf.Abs(angle)>100;if(reverse)angle=Mathf.DeltaAngle(0,angle-180);
  truck.steer=Mathf.Clamp(angle/25,-1,1)*(reverse?-1:1);
  truck.throttle=(reverse?-1:1)*speed;
 }
 bool Clear(Vector3 a,Vector3 b,int ignore){
  var d=b-a;d.y=0;if(d.sqrMagnitude<.001f)return true;
  foreach(var ball in match.balls){if(ball.Pocketed||ball.number==0||ball.number==ignore)continue;
   var v=ball.body.position-a;v.y=0;float t=Vector3.Dot(v,d)/d.sqrMagnitude;
   if(t>.05f&&t<.96f&&(v-d*t).magnitude<1.03f)return false;
  }return true;
 }
 public ShotPlan EvaluateLayout(BilliardsDifficulty level,int seed){
  var tuning=Tuning(level);var cue=match.balls[0].body.position;cue.y=0;
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
  truck.deliberate=false;timer=0;
  LastPlan=EvaluateLayout(match.session!=null?match.session.difficulty:BilliardsDifficulty.Normal,Random.Range(0,int.MaxValue));Plans++;
  if(!LastPlan.valid){FailApproach("no reachable legal approach");return;}
  direction=LastPlan.direction;staging=LastPlan.staging;power=LastPlan.power;
  stage=Stage.Staging;Debug.Log("TB_AI_PLAN difficulty="+(match.session!=null?match.session.difficulty:BilliardsDifficulty.Normal)+" target="+LastPlan.target+" fallback="+LastPlan.fallback+" error="+LastPlan.aimError+" staging="+staging+" power="+power);
 }
 void FailApproach(string reason){
  tries++;truck.deliberate=false;
  if(tries>maxRecoveries){int previous=match.rules.Turn;match.rules.Forfeit(reason+" (recovery limit)");if(match.session!=null)match.session.ShotResolved(previous);Debug.Log("TB_AI_FORFEIT "+reason);ResetPlan();return;}
  stage=Stage.Recover;timer=0;
 }
 bool Inside(Vector3 p)=>Mathf.Abs(p.x)<match.halfWidth-.75f&&Mathf.Abs(p.z)<match.halfLength-.75f;
}}



