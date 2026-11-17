using UnityEngine;using System.Text;using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckWaterSports {
public abstract class WaterMatch:SportsEventSession {
 public WaterVessel[] vessels;public WaterCourse course;public SportsEventCamera broadcast;public float matchDuration=180;public float Clock{get;protected set;}public int[] Gates=new int[4],MissedGates=new int[4],GateAccuracyPoints=new int[4];public float[] Scores=new float[4],FinishTimes={-1,-1,-1,-1};public virtual bool Cooperative=>false;
 protected readonly StringBuilder text=new StringBuilder(400);protected abstract bool Canoe{get;}
 public SportsEventCamera[] participantCameras;readonly int[] viewOwners=new int[4];int humanViews;bool finalMetricsRecorded;string metricsMatchId;
 readonly int[] soundedStrokes=new int[4];readonly float[] nextSplash=new float[4];
 public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;public override int OptionCount=>4;public override int CompactScoreColumns=>4;public override bool CompactBroadcastStatus=>true;
 public override string Header=>Title+" · "+Mathf.CeilToInt(Mathf.Max(0,matchDuration-Clock))+"s";
 public override string Status=>"PADDLE · LEAN · WATCH THE CURRENT";
 public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(ResultPlace(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(Scores[i].ToString("F0")).Append(" · ").Append(FinishTimes[i]>=0?FinishTimes[i].ToString("F1")+"s":"GATE "+Gates[i]).Append('\n');return text.ToString();}}
 public override string Results=>"WATER EVENT COMPLETE\n"+Standings;
 public virtual string ResultUnit=>"PTS";public virtual bool ResultHigherIsBetter=>true;public virtual double ResultScore(int slot)=>Scores[slot];
 public virtual bool ResultHasOutcome(int slot)=>slot>=0&&slot<setup.count&&Gates[slot]>1;
 public int ResultPlace(int slot){if(!ResultHasOutcome(slot))return 0;int place=1;double score=ResultScore(slot);for(int i=0;i<setup.count;i++)if(ResultHasOutcome(i)&&(ResultHigherIsBetter?ResultScore(i)>score+.01:ResultScore(i)<score-.01))place++;return place;}
 public bool ResultWinner(int slot)=>!Cooperative&&ResultHasOutcome(slot)&&ResultPlace(slot)==1;
 public override bool TryReadScoreCard(int slot,out string score,out string detail){score=Scores[slot].ToString("F0");var v=Vessel(slot);detail="GATE "+Gates[slot]+" · "+v.Capsizes+" CAPSIZES";return slot<setup.count;}
 public WaterVessel Vessel(int slot)=>vessels[Canoe?slot/2:slot];
 protected override void BeginGame(){Clock=0;finalMetricsRecorded=false;metricsMatchId=MatchId;humanViews=0;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)viewOwners[humanViews++]=i;ConfigureViews();for(int i=0;i<4;i++){Gates[i]=1;MissedGates[i]=0;GateAccuracyPoints[i]=0;Scores[i]=0;FinishTimes[i]=-1;soundedStrokes[i]=0;nextSplash[i]=0;}for(int i=0;i<vessels.Length;i++){bool active=Canoe?i*2<setup.count:i<setup.count;vessels[i].gameObject.SetActive(active);if(active){int first=Canoe?i*2:i;int second=Mathf.Min(first+1,setup.count-1);vessels[i].SetColors(setup.players[first].color,setup.players[second].color);vessels[i].ResetVessel();}}BeginWater();}
 protected virtual void BeginWater(){}
 protected override void Simulate(float dt){Clock+=dt;for(int i=0;i<vessels.Length;i++){var v=vessels[i];if(!v.gameObject.activeSelf)continue;v.Tick(dt,Clock);if(presentationWeather!=null&&!v.body.isKinematic)v.body.AddForce(presentationWeather.Flow*.06f,ForceMode.Acceleration);TickBoat(v,i,dt);if(v.Strokes!=soundedStrokes[i]&&Clock>=nextSplash[i]){soundedStrokes[i]=v.Strokes;nextSplash[i]=Clock+.7f;feedback.PlayAt(0,v.body.position,3);}}AfterBoats(dt);bool all=true;for(int i=0;i<setup.count;i++)all&=FinishTimes[i]>=0;if(all||Clock>=matchDuration){for(int i=0;i<setup.count;i++)ConfirmOutcome("water-result:"+i,i,Scores[i],"WATER EVENT","PTS",matchEnded:true);EndMatch();}}
 protected virtual void AfterBoats(float dt){}
 protected abstract void TickBoat(WaterVessel vessel,int index,float dt);
 protected Vector2 Pilot(int slot,WaterVessel v,int gate){if(slot>=setup.count||setup.players[slot].source==BowlingSource.AI)return Navigate(slot,v,course.Point(gate));return commands[slot];}
 protected Vector2 Navigate(int slot,WaterVessel v,Vector3 target,Rigidbody contactTarget=null){int skill=slot<setup.count?(int)setup.players[slot].difficulty:1;target+=Vector3.right*Mathf.Sin(Clock*.4f+slot)*(2-skill)*1.5f;var delta=Vector3.ProjectOnPlane(target-v.body.position,Vector3.up);float yaw=Vector3.SignedAngle(v.transform.forward,delta,Vector3.up);float steer=Mathf.Clamp(yaw/(45-skill*7),-1,1);if(Physics.SphereCast(v.body.position+Vector3.up*.4f,1,v.transform.forward,out var hit,10+skill*2,~0,QueryTriggerInteraction.Ignore)&&hit.rigidbody!=v.body&&(contactTarget==null||hit.rigidbody!=contactTarget))steer=Vector3.Dot(hit.normal,v.transform.right)>0?1:-1;return new Vector2(steer,1-Mathf.Abs(steer)*.32f);}
 protected bool Action(int slot)=>slot>=setup.count||setup.players[slot].source==BowlingSource.AI?false:actions[slot];
 protected void CheckGate(int slot,WaterVessel v,float radius=13){if(FinishTimes[slot]>=0)return;int gate=Gates[slot];var delta=v.body.position-course.Point(gate);bool hit=delta.magnitude<radius;var forward=(course.Point(gate)-course.Point(gate-1)).normalized;bool missed=!hit&&Vector3.Dot(delta,forward)>radius*1.5f;if(!hit&&!missed)return;if(missed)MissedGates[slot]++;else GateAccuracyPoints[slot]+=Mathf.RoundToInt((1-Vector3.ProjectOnPlane(delta,forward).magnitude/radius)*100);Gates[slot]++;Scores[slot]=Gates[slot]*100-v.Capsizes*20-MissedGates[slot]*50;if(Gates[slot]>=course.stations.Length){Gates[slot]=course.stations.Length-1;FinishTimes[slot]=Clock+MissedGates[slot]*5;Scores[slot]+=10000-FinishTimes[slot];ConfirmOutcome("finish:"+slot,slot,FinishTimes[slot],"RIVER FINISH","SEC");}SportsSessionTelemetry.GameplayEvent(Title,missed?"water-gate-missed":"water-gate",setup.players[slot].participantId,missed?"five-second penalty":"checkpoint",gate);}
 void ConfigureViews(){if(participantCameras==null)return;broadcast.view.enabled=humanViews<=1;for(int i=0;i<participantCameras.Length;i++){var c=participantCameras[i].view;c.enabled=humanViews>1&&i<humanViews;c.rect=humanViews==2?new Rect(i*.5f,0,.5f,1):new Rect(i%2*.5f,1-(i/2+1)*.5f,.5f,.5f);}}
 protected override void Present(float dt){if(vessels==null||vessels.Length==0||broadcast==null)return;WaterVessel focus=vessels[0];for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI){focus=Vessel(i);break;}broadcast.Pose(focus.body.position+focus.transform.forward*8,new Vector3(32,27,-36),62);if(Live&&humanViews>1&&participantCameras!=null)for(int i=0;i<humanViews;i++){var v=Vessel(viewOwners[i]);participantCameras[i].Pose(v.body.position+v.transform.forward*5,v.transform.rotation*new Vector3(12,13,-21),65);}}
 // The existing telemetry queue owns persistence/buffering. Emit one bounded
 // final observation per participant/metric, including interrupted attempts.
 // Shared canoe values are explicitly labelled; individual strokes use the
 // actual station counter, rather than attributing the whole canoe to both seats.
 void RecordFinalMetrics(){
  if(finalMetricsRecorded||!Running||Clock<=0||string.IsNullOrEmpty(metricsMatchId)||metricsMatchId!=MatchId||vessels==null)return;
  finalMetricsRecorded=true;
  for(int slot=0;slot<setup.count;slot++){
   var vessel=Vessel(slot);if(vessel==null)continue;
   string scope=Canoe?"shared_canoe_"+(slot/2):"individual_vessel_"+slot;
   FinalMetric(slot,"capsizes",vessel.Capsizes,scope);
   FinalMetric(slot,"missed_gates",MissedGates[slot],scope);
   if(Canoe){FinalMetric(slot,"strokes",vessel.StrokesAtStation(slot%2),"individual_station_"+(slot%2));FinalMetric(slot,"synchronized_stroke_pairs",vessel.Synchronizations,scope);}
   else if(this is WhitewaterWipeoutMatch){FinalMetric(slot,"strokes",vessel.Strokes,"individual");FinalMetric(slot,"trick_points",vessel.TrickPoints,"individual");FinalMetric(slot,"completed_rolls",vessel.Rolls,"individual");FinalMetric(slot,"landings",vessel.Landings,"individual");if(option==3)FinalMetric(slot,"survival_seconds",FinishTimes[slot]>=0?FinishTimes[slot]:Clock,"individual");}
   RecordModeFinalMetrics(slot);
  }
 }
 protected void FinalMetric(int slot,string metric,double value,string scope){SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"water_final_"+metric,setup.players[slot].participantId,"match="+metricsMatchId+"; option="+option+"; scope="+scope+"; observed_at_match_stop",value);}
 protected virtual void RecordModeFinalMetrics(int slot){}
 protected override void StopGame(){RecordFinalMetrics();if(vessels!=null)foreach(var v in vessels)if(v!=null)v.Freeze();if(broadcast!=null)broadcast.view.enabled=true;if(participantCameras!=null)foreach(var camera in participantCameras)camera.view.enabled=false;}
}
}
