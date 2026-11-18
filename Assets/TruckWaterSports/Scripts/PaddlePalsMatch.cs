using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckWaterSports {
public sealed class PaddlePalsMatch:WaterMatch {
 readonly float[] chaosDisengageUntil=new float[2],chaosProgressAt=new float[2];
 readonly int[] chaosProgressGate=new int[2];
 readonly RaycastHit[] chaosObstacles=new RaycastHit[24];
 readonly int[] chaosDecision=new int[2],chaosInterceptions=new int[2],chaosEvasions=new int[2];
 public int ChaosInterceptionAttempts(int slot)=>chaosInterceptions[slot/2];
 public int ChaosEvasionAttempts(int slot)=>chaosEvasions[slot/2];
 readonly int[] lastSynchronization=new int[2],qualifiedSynchronization=new int[2],lastBumps=new int[2],lastCapsizes=new int[2];
 protected override bool Canoe=>true;
 public override string Title=>"TRUCK CANOEING: PADDLE PALS";
 public override string OptionLabel=>new[]{"RIVER SPRINT","RAPID RALLY","CANOE CHAOS","TEAM PADDLE CHALLENGE"}[option];
 public override string ResultUnit=>option<2?"SEC":"PTS";
 public int MovingSynchronizedPairs(int slot)=>qualifiedSynchronization[slot/2];
 public override bool ResultHigherIsBetter=>option>=2;
 public override double ResultScore(int slot)=>option<2?FinishTimes[slot]>=0?FinishTimes[slot]+Vessel(slot).Capsizes*2+(option==1?Vessel(slot).Collisions*2+RoutePenaltySeconds(slot):0):0:Scores[slot];
 public override bool ResultHasOutcome(int slot)=>slot>=0&&slot<setup.count&&(option<2?FinishTimes[slot]>=0:option==3?Gates[slot]>1&&(qualifiedSynchronization[slot/2]>0||GateAccuracyPoints[slot]>0):Gates[slot]>1||Vessel(slot).CreditedBumps>0);
 public override string Help=>"Each truck controls a real paddle station. Drive for forward/reverse strokes; steer changes stroke angle; ACTION rights a capsize. AI partners follow a human's actual cadence. River Sprint: quickest full crossing. Rapid Rally: tighter gates and a physical fork. Choose the orange log shortcut or green shallow channel; missing both branch gates adds three seconds. Ordinary misses and collision/capsize penalties still apply. Canoe Chaos: watch for timber swinging on riverbed tethers. Only the canoe providing greater closing momentum earns a bump; capsizing an opponent adds a bonus. AI crews alternate interceptions with evasive gate lines, disengage after actual contact and avoid attacking across waterfalls. Steer aside and brace to spoil an incoming ram. Team Paddle Challenge: synchronization counts only while moving forward and upright; accurate gates also score. Partners share their canoe's result.";
 public override string Status=>option==2?"CLEAN BUMPS · FORCE A CAPSIZE · REACH THE FINISH":option==3?"PADDLE TOGETHER · KEEP MOVING · CENTER THE GATES":base.Status;
 protected override void BeginWater(){System.Array.Clear(lastSynchronization,0,2);System.Array.Clear(qualifiedSynchronization,0,2);System.Array.Clear(lastBumps,0,2);System.Array.Clear(lastCapsizes,0,2);System.Array.Clear(chaosDisengageUntil,0,2);System.Array.Clear(chaosDecision,0,2);System.Array.Clear(chaosInterceptions,0,2);System.Array.Clear(chaosEvasions,0,2);System.Array.Clear(chaosProgressAt,0,2);System.Array.Clear(chaosProgressGate,0,2);}
 protected override void TickBoat(WaterVessel vessel,int index,float dt){
  int first=index*2;if(FinishTimes[first]>=0)return;
  var left=Pilot(first,vessel,Gates[first]);var right=Pilot(first+1,vessel,Gates[first]);
  bool leftAI=setup.players[first].source==BowlingSource.AI;
  bool rightAI=first+1>=setup.count||setup.players[first+1].source==BowlingSource.AI;
  if(rightAI&&!leftAI||first+1>=setup.count)right=left;
  else if(leftAI&&!rightAI)left=right;
  if(option==2&&leftAI&&rightAI&&TryChaosCommand(first,vessel,index,out var contest))left=right=contest;
  vessel.Stroke(0,left.y,Clock,left.x*.9f);vessel.Stroke(1,right.y,Clock,right.x*.9f);vessel.Brace((left.x+right.x)*.25f,dt);
  if(Action(first)||Action(first+1)||vessel.Capsized&&leftAI)vessel.Recover();
  CheckGate(first,vessel,option==1?8:13);
  Scores[first]=Gates[first]*100-vessel.Capsizes*20-MissedGates[first]*50+(FinishTimes[first]>=0?10000-FinishTimes[first]:0);
  if(option==1)Scores[first]-=vessel.Collisions*10;
  if(option==2){
   Scores[first]=Gates[first]*50+vessel.CreditedBumps*75+vessel.CausedCapsizes*200-vessel.Capsizes*50-MissedGates[first]*25+(FinishTimes[first]>=0?Mathf.Max(0,500-FinishTimes[first]):0);
   if(vessel.CreditedBumps>lastBumps[index])ConfirmScore("chaos-bump:"+index+":"+vessel.CreditedBumps,first,(vessel.CreditedBumps-lastBumps[index])*75,Scores[first],"CLEAN CANOE BUMP");
   if(vessel.CausedCapsizes>lastCapsizes[index])ConfirmScore("chaos-capsize:"+index+":"+vessel.CausedCapsizes,first,(vessel.CausedCapsizes-lastCapsizes[index])*200,Scores[first],"OPPONENT CAPSIZED");
   lastBumps[index]=vessel.CreditedBumps;lastCapsizes[index]=vessel.CausedCapsizes;
  }
  if(option==3){
   int pairs=vessel.Synchronizations-lastSynchronization[index];lastSynchronization[index]=vessel.Synchronizations;
   var forward=(course.Point(Gates[first])-course.Point(Gates[first]-1)).normalized;
   if(pairs>0&&!vessel.Capsized&&Vector3.Dot(vessel.body.linearVelocity,forward)>1.5f)qualifiedSynchronization[index]+=pairs;
   Scores[first]=Gates[first]*50+GateAccuracyPoints[first]+qualifiedSynchronization[index]*10-vessel.Capsizes*30-MissedGates[first]*25+(FinishTimes[first]>=0?Mathf.Max(0,500-FinishTimes[first]):0);
  }
  if(first+1<setup.count){Gates[first+1]=Gates[first];MissedGates[first+1]=MissedGates[first];GateAccuracyPoints[first+1]=GateAccuracyPoints[first];FinishTimes[first+1]=FinishTimes[first];Scores[first+1]=Scores[first];}
 }
 bool TryChaosCommand(int first,WaterVessel vessel,int index,out Vector2 command){
  command=default;int decision=0;WaterVessel opponent=null;
  if(chaosProgressGate[index]!=Gates[first]){chaosProgressGate[index]=Gates[first];chaosProgressAt[index]=Clock;}
  for(int other=0;other<vessels.Length;other++)if(other!=index&&vessels[other].gameObject.activeSelf&&FinishTimes[other*2]<0){opponent=vessels[other];break;}
  if(opponent==null||vessel.Capsized||opponent.Capsized){chaosDecision[index]=0;return false;}
  // The13m hulls must clear the launch and both opening checkpoints before
  // any side contest. Run103 showed a launch-side pursuit could strand gate1.
  // A crew that stops making real gate progress or falls behind races first.
  if(Gates[first]<3||Gates[opponent.firstSlot]<3||Clock-chaosProgressAt[index]>6||
     Gates[first]+1<Gates[opponent.firstSlot]){chaosDecision[index]=0;return false;}
  if(vessel.Touching(opponent))chaosDisengageUntil[index]=Clock+1.2f;
  // Real contact triggers a brief return to the gate, so two crews do not
  // endlessly grind their hulls together instead of completing the river.
  if(Clock<chaosDisengageUntil[index]){chaosDecision[index]=0;return false;}
  Vector3 offset=opponent.body.position-vessel.body.position;
  if(offset.magnitude>32||Mathf.Abs(offset.y)>2.5f){chaosDecision[index]=0;return false;}
  int skill=(int)setup.players[first].difficulty;
  Vector3 courseForward=Vector3.ProjectOnPlane(course.Point(Gates[first])-course.Point(Gates[first]-1),Vector3.up).normalized;
  bool attacking=Mathf.Repeat(Clock+index*4.5f,9)<1.5f+skill*.75f;
  Vector3 target;
  if(attacking){
   target=opponent.body.position+opponent.body.linearVelocity*(.4f+skill*.15f);
   Vector3 intercept=Vector3.ProjectOnPlane(target-vessel.body.position,Vector3.up);
   if(Vector3.Dot(offset,courseForward)>22||Vector3.Angle(Vector3.ProjectOnPlane(vessel.transform.forward,Vector3.up),intercept)>55||
      Vector3.Dot(intercept,courseForward)<0||!SafeChaosLine(vessel,target,opponent.body)){chaosDecision[index]=0;return false;}
   decision=1;command=Navigate(first,vessel,target,opponent.body);
  }else{
   Vector3 towardSelf=Vector3.ProjectOnPlane(-offset,Vector3.up).normalized;
   float closing=Vector3.Dot(opponent.body.linearVelocity-vessel.body.linearVelocity,towardSelf);
   if(offset.magnitude>8+skill*2||closing<1){chaosDecision[index]=0;return false;}
   Vector3 right=Vector3.Cross(Vector3.up,courseForward);
   float side=Vector3.Dot(vessel.body.position-opponent.body.position,right)>=0?1:-1;
   target=course.Point(Gates[first])+right*side*(3.5f+skill*.5f);
   if(!SafeChaosLine(vessel,target,opponent.body)){chaosDecision[index]=0;return false;}
   decision=2;command=Navigate(first,vessel,target);
  }
  if(chaosDecision[index]!=decision){if(decision==1)chaosInterceptions[index]++;else chaosEvasions[index]++;}
  chaosDecision[index]=decision;return true;
 }
 bool SafeChaosLine(WaterVessel vessel,Vector3 end,Rigidbody opponent){
  Vector3 start=vessel.body.position;
  // No interception across waterfall levels or into the bank. All propulsion,
  // steering, bracing and impacts still use the ordinary two paddle stations.
  float startingHeight=course.SurfacePointXZ(start).y;
  for(int sample=1;sample<=4;sample++){
   Vector3 at=Vector3.Lerp(start,end,sample/4f),water=course.SurfacePointXZ(at);
   if(Mathf.Abs(water.y-startingHeight)>2.5f||Vector3.ProjectOnPlane(at-water,Vector3.up).magnitude>course.width*.5f-3.6f)return false;
  }
  Vector3 delta=Vector3.ProjectOnPlane(end-start,Vector3.up);
  if(delta.sqrMagnitude<.01f)return false;
  int hits=Physics.SphereCastNonAlloc(start+Vector3.up*.5f,1.8f,delta.normalized,chaosObstacles,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
  if(hits==chaosObstacles.Length)return false;
  for(int hit=0;hit<hits;hit++)if(chaosObstacles[hit].rigidbody!=vessel.body&&chaosObstacles[hit].rigidbody!=opponent)return false;
  return true;
 }
 protected override void RecordModeFinalMetrics(int slot){if(option==2){FinalMetric(slot,"chaos_interception_attempts",ChaosInterceptionAttempts(slot),"shared_canoe_"+(slot/2));FinalMetric(slot,"chaos_evasion_attempts",ChaosEvasionAttempts(slot),"shared_canoe_"+(slot/2));FinalMetric(slot,"credited_bumps",Vessel(slot).CreditedBumps,"shared_canoe_"+(slot/2));FinalMetric(slot,"opponents_capsized",Vessel(slot).CausedCapsizes,"shared_canoe_"+(slot/2));}if(option==3){FinalMetric(slot,"moving_synchronized_pairs",qualifiedSynchronization[slot/2],"shared_canoe_"+(slot/2));FinalMetric(slot,"gate_accuracy_points",GateAccuracyPoints[slot],"shared_canoe_"+(slot/2));}}
}
}
