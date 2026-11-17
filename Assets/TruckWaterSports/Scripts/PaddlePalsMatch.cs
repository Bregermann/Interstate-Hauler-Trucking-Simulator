using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckWaterSports {
public sealed class PaddlePalsMatch:WaterMatch {
 readonly int[] lastSynchronization=new int[2],qualifiedSynchronization=new int[2],lastBumps=new int[2],lastCapsizes=new int[2];
 protected override bool Canoe=>true;
 public override string Title=>"TRUCK CANOEING: PADDLE PALS";
 public override string OptionLabel=>new[]{"RIVER SPRINT","RAPID RALLY","CANOE CHAOS","TEAM PADDLE CHALLENGE"}[option];
 public override string ResultUnit=>option<2?"SEC":"PTS";
 public override bool ResultHigherIsBetter=>option>=2;
 public override double ResultScore(int slot)=>option<2?FinishTimes[slot]>=0?FinishTimes[slot]+Vessel(slot).Capsizes*2+(option==1?Vessel(slot).Collisions*2:0):0:Scores[slot];
 public override bool ResultHasOutcome(int slot)=>slot>=0&&slot<setup.count&&(option<2?FinishTimes[slot]>=0:option==3?Gates[slot]>1&&(qualifiedSynchronization[slot/2]>0||GateAccuracyPoints[slot]>0):Gates[slot]>1||Vessel(slot).CreditedBumps>0);
 public override string Help=>"Each truck controls a real paddle station. Drive for forward/reverse strokes; steer changes stroke angle; ACTION rights a capsize. AI partners follow a human's actual cadence. River Sprint: quickest full crossing. Rapid Rally: tighter gates, five-second misses, collision/capsize time penalties. Canoe Chaos: only the canoe providing greater closing momentum earns a bump; capsizing an opponent adds a bonus. Team Paddle Challenge: synchronization counts only while moving forward and upright; accurate gates also score. Partners share their canoe's result.";
 public override string Status=>option==2?"CLEAN BUMPS · FORCE A CAPSIZE · REACH THE FINISH":option==3?"PADDLE TOGETHER · KEEP MOVING · CENTER THE GATES":base.Status;
 protected override void BeginWater(){System.Array.Clear(lastSynchronization,0,2);System.Array.Clear(qualifiedSynchronization,0,2);System.Array.Clear(lastBumps,0,2);System.Array.Clear(lastCapsizes,0,2);}
 protected override void TickBoat(WaterVessel vessel,int index,float dt){
  int first=index*2;if(FinishTimes[first]>=0)return;
  var left=Pilot(first,vessel,Gates[first]);var right=Pilot(first+1,vessel,Gates[first]);
  bool leftAI=setup.players[first].source==BowlingSource.AI;
  bool rightAI=first+1>=setup.count||setup.players[first+1].source==BowlingSource.AI;
  if(rightAI&&!leftAI||first+1>=setup.count)right=left;
  else if(leftAI&&!rightAI)left=right;
  if(option==2&&leftAI&&rightAI&&Mathf.Repeat(Clock+index*4.5f,9)<1.5f+(int)setup.players[first].difficulty*.75f)for(int other=0;other<vessels.Length;other++){
   var rival=vessels[other];if(other==index||!rival.gameObject.activeSelf||FinishTimes[other*2]>=0)continue;
   if(Vector3.Distance(vessel.body.position,rival.body.position)<32){left=right=Navigate(first,vessel,rival.body.position+rival.body.linearVelocity*.6f,rival.body);break;}
  }
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
 protected override void RecordModeFinalMetrics(int slot){if(option==2){FinalMetric(slot,"credited_bumps",Vessel(slot).CreditedBumps,"shared_canoe_"+(slot/2));FinalMetric(slot,"opponents_capsized",Vessel(slot).CausedCapsizes,"shared_canoe_"+(slot/2));}if(option==3){FinalMetric(slot,"moving_synchronized_pairs",qualifiedSynchronization[slot/2],"shared_canoe_"+(slot/2));FinalMetric(slot,"gate_accuracy_points",GateAccuracyPoints[slot],"shared_canoe_"+(slot/2));}}
}
}
