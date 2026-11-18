using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckWaterSports {
public sealed class WhitewaterWipeoutMatch:WaterMatch {
 protected override bool Canoe=>false;
 public override string ControlCaption(int slot){var player=setup.players[slot];if(player.source==BowlingSource.AI)return "AI · ALTERNATING PHYSICAL STROKES";if(player.source==BowlingSource.WholeController)return "LB LEFT PADDLE · RB RIGHT PADDLE · RT/LT POWER/DIRECTION · LS LEAN/CARVE · SOUTH ROLL";return Controls(slot)+" · LEFT/RIGHT CHOOSE PADDLE · UP/DOWN STROKE DIRECTION · CENTER + UP/DOWN BOTH";}

 public override string Title=>"TRUCK KAYAKING: WHITEWATER WIPEOUT";
 public override string OptionLabel=>new[]{"WHITEWATER RACE","KAYAK SLALOM","WATERFALL FREESTYLE","LONGEST SURVIVAL RUN"}[option];
 public override string ResultUnit=>option==2?"PTS":"SEC";
 public override bool ResultHigherIsBetter=>option>=2;
 public override double ResultScore(int slot)=>option<2?FinishTimes[slot]>=0?FinishTimes[slot]+Vessel(slot).Capsizes*2+(option==1?Vessel(slot).Collisions*2+RoutePenaltySeconds(slot):0):0:option==2?Scores[slot]:Stranded[slot]?StrandedAt[slot]:FinishTimes[slot]>=0?FinishTimes[slot]:Clock;
 public override bool ResultHasOutcome(int slot)=>slot>=0&&slot<setup.count&&(option<2?FinishTimes[slot]>=0:option==2?Vessel(slot).TrickPoints>0:Clock>=3);
 public override string Help=>"Humans control each real paddle independently. Whole controller: LB/RB paddle sides, RT/LT set forward/backstroke power and left stick carves/leans. Keyboard or split stick: left/right choose a paddle, up/down set direction, centered sides plus up/down stroke both. Steering with a single paddle turns away from that side. ACTION rolls or rights an inverted kayak. Whitewater Race follows the full river to the finish. Slalom adds a physical fork: orange log shortcut or green shallow channel. Missing both branch gates costs three seconds; ordinary gate and collision penalties also apply. Freestyle: tap ACTION in the air for a roll, then hold ACTION to brace toward a controlled landing; tricks score only after physical landing. Survival: three capsizes retire you. After the first descent, circle the lower rapids without attempting an impossible climb back up a waterfall. Currents/waves intensify over time. AI paddles, rolls, braces and recovers through the same forces.";
 public override string Status=>option==2?"TAP ACTION: AERIAL ROLL · HOLD: LANDING BRACE":option==3?"THREE CAPSIZES AND OUT · WAVES ARE BUILDING":base.Status;
 readonly int[] side=new int[4],survivalGate=new int[4],notifiedLandings=new int[4];
 readonly bool[] paddleArmed=new bool[4];
 readonly bool[] upstream=new bool[4];readonly Vector3[] lastPosition=new Vector3[4];readonly float[] distance=new float[4],originalWaves=new float[4],lastTrickPoints=new float[4];bool wavesCaptured;int survivalFloor;
 protected override void BeginWater(){
  survivalFloor=1;for(int gate=1;gate<course.stations.Length;gate++)if(course.Point(gate-1).y-course.Point(gate).y>2.5f)survivalFloor=Mathf.Min(gate+1,course.stations.Length-2);
  for(int i=0;i<4;i++){side[i]=0;paddleArmed[i]=false;survivalGate[i]=1;upstream[i]=false;distance[i]=0;lastPosition[i]=vessels[i].body.position;notifiedLandings[i]=0;lastTrickPoints[i]=0;if(!wavesCaptured)originalWaves[i]=vessels[i].flotation.waveAmplitude;vessels[i].flotation.waveAmplitude=originalWaves[i];}
  wavesCaptured=true;
 }
 protected override void TickBoat(WaterVessel vessel,int index,float dt){
  if(FinishTimes[index]>=0||Stranded[index])return;
  bool ai=setup.players[index].source==BowlingSource.AI;
  int target=option==3?survivalGate[index]:Gates[index];var drive=ai?NavigateWatercraft(index,vessel,RouteAim(index,vessel,course.Point(target)),11):commands[index];
  if(ai){
   // Preserve the natural AI/default/Slalom acceptance path.
   if(vessel.WaterSupported&&vessel.Stroke(side[index],drive.y,Clock))side[index]=1-side[index];
   vessel.SteerWatercraft(drive.x);vessel.Brace(drive.x*.65f,dt);
  }else{
   var strokes=KayakPaddleControls.Read(setup.players[index]);
   if(!setup.players[index].released||LWS.TruckSports.SportsControllerKeyboard.InputBlocked)paddleArmed[index]=false;
   else if(!paddleArmed[index]){if(strokes.Neutral)paddleArmed[index]=true;}
   else if(vessel.WaterSupported){vessel.Stroke(0,strokes.left,Clock);vessel.Stroke(1,strokes.right,Clock);}
   vessel.SteerWatercraft(strokes.carve);vessel.Brace(strokes.lean*.65f,dt);
  }
  if(Action(index)){if(vessel.Capsized)vessel.Recover();else vessel.Roll();}
  else if(vessel.Capsized&&ai)vessel.Recover();
  if(option==2){
   if(ai&&vessel.AirborneSeconds>.12f&&vessel.AirborneSeconds<.32f&&!vessel.Capsized)vessel.Roll();
   if(vessel.AirborneSeconds>.55f&&(ai||ActionHeld(index)))vessel.AirBrace(18);
   if(vessel.Landings>notifiedLandings[index]){
    float points=vessel.TrickPoints-lastTrickPoints[index];if(points>0&&vessel.LastLandingQuality>.25f)ConfirmScore("freestyle-landing:"+index+":"+vessel.Landings,index,points,vessel.TrickPoints,"CONTROLLED WATERFALL LANDING");
    notifiedLandings[index]=vessel.Landings;lastTrickPoints[index]=vessel.TrickPoints;feedback.PlayAt(0,vessel.body.position,15);
   }
  }
  float traveled=Vector3.Distance(lastPosition[index],vessel.body.position);if(traveled<20)distance[index]+=traveled;lastPosition[index]=vessel.body.position;
  if(option==3){
   float pressure=Mathf.Clamp01(Clock/Mathf.Max(1,matchDuration));vessel.flotation.waveAmplitude=originalWaves[index]+pressure*.25f;vessel.body.AddForce(Vector3.right*Mathf.Sin(Clock*1.3f+index)*pressure*.7f,ForceMode.Acceleration);
   if(Vector3.Distance(vessel.body.position,course.Point(target))<14){
    if(upstream[index]){if(target<=survivalFloor){upstream[index]=false;survivalGate[index]=survivalFloor+1;}else survivalGate[index]--;}
    else if(target>=course.stations.Length-1){upstream[index]=true;survivalGate[index]=course.stations.Length-2;}
    else survivalGate[index]++;
    Gates[index]++;
   }
   Scores[index]=Clock+distance[index]*.1f;if(vessel.Capsizes>=3){FinishTimes[index]=Clock;vessel.Freeze();ConfirmOutcome("survival-out:"+index,index,Clock,"THREE CAPSIZES","SEC");}return;
  }
  CheckGate(index,vessel,option==1?8:14);
  if(option==1)Scores[index]=Gates[index]*100-vessel.Capsizes*20-MissedGates[index]*50-vessel.Collisions*10+(FinishTimes[index]>=0?10000-FinishTimes[index]:0);
  if(option==2)Scores[index]=vessel.TrickPoints+Gates[index]*10-vessel.Capsizes*20-MissedGates[index]*25;
 }
 protected override void RecordModeFinalMetrics(int slot){if(option==3)FinalMetric(slot,"survival_distance_metres",distance[slot],"individual");}
}
}
