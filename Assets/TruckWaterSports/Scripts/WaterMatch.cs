using UnityEngine;using System.Text;using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckWaterSports {
public abstract class WaterMatch:SportsEventSession {
 public WaterVessel[] vessels;public WaterCourse course;public SportsEventCamera broadcast;public float matchDuration=180;public float Clock{get;protected set;}public int[] Gates=new int[4],MissedGates=new int[4],GateAccuracyPoints=new int[4];public float[] Scores=new float[4],FinishTimes={-1,-1,-1,-1};public virtual bool Cooperative=>false;
 protected readonly StringBuilder text=new StringBuilder(400);protected abstract bool Canoe{get;}
 public SportsEventCamera[] participantCameras;readonly int[] viewOwners=new int[4];int humanViews;bool finalMetricsRecorded;string metricsMatchId;
 readonly int[] soundedStrokes=new int[4];readonly float[] nextSplash=new float[4];
 public readonly bool[] Stranded=new bool[4];public readonly float[] StrandedAt=new float[4];readonly float[] outsideWater=new float[4];readonly RaycastHit[] navigationHits=new RaycastHit[24];
 public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;public override int OptionCount=>4;public override int CompactScoreColumns=>4;public override bool CompactBroadcastStatus=>true;
 public override string Header=>Title+" · "+Mathf.CeilToInt(Mathf.Max(0,matchDuration-Clock))+"s";
 public override string Status=>"PADDLE · LEAN · WATCH THE CURRENT";
 public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(ResultPlace(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(Scores[i].ToString("F0")).Append(" · ").Append(Stranded[i]?"STRANDED · DNF":FinishTimes[i]>=0?FinishTimes[i].ToString("F1")+"s":"GATE "+Gates[i]).Append('\n');return text.ToString();}}
 public override string Results=>"WATER EVENT COMPLETE\n"+Standings;
 public virtual string ResultUnit=>"PTS";public virtual bool ResultHigherIsBetter=>true;public virtual double ResultScore(int slot)=>Scores[slot];
 public virtual bool ResultHasOutcome(int slot)=>slot>=0&&slot<setup.count&&Gates[slot]>1;
 public int ResultPlace(int slot){if(!ResultHasOutcome(slot))return 0;int place=1;double score=ResultScore(slot);for(int i=0;i<setup.count;i+=Canoe?2:1)if(ResultHasOutcome(i)&&(ResultHigherIsBetter?ResultScore(i)>score+.01:ResultScore(i)<score-.01))place++;return place;}
 public bool ResultWinner(int slot)=>!Cooperative&&ResultHasOutcome(slot)&&ResultPlace(slot)==1;
 public override bool TryReadScoreCard(int slot,out string score,out string detail){score=Scores[slot].ToString("F0");var v=Vessel(slot);detail=Stranded[slot]?"STRANDED · LEFT THE WATER":"GATE "+Gates[slot]+" · "+v.Capsizes+" CAPSIZES";return slot<setup.count;}
 public WaterVessel Vessel(int slot)=>vessels[Canoe?slot/2:slot];
 protected override void BeginGame(){Clock=0;finalMetricsRecorded=false;metricsMatchId=MatchId;humanViews=0;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)viewOwners[humanViews++]=i;ConfigureViews();for(int i=0;i<4;i++){Gates[i]=1;MissedGates[i]=0;GateAccuracyPoints[i]=0;Scores[i]=0;FinishTimes[i]=-1;Stranded[i]=false;StrandedAt[i]=-1;outsideWater[i]=0;soundedStrokes[i]=0;nextSplash[i]=0;}Bounds footprint=default;if(!Canoe&&course.lake){foreach(var surface in UnityEngine.Object.FindObjectsByType<BlackwaterWake>(FindObjectsSortMode.None))if(surface.gameObject.scene==gameObject.scene){footprint=surface.GetComponent<Renderer>().bounds;break;}if(footprint.size.x<1||footprint.size.z<1)throw new System.InvalidOperationException("Blackwater requires its authored water-surface footprint.");}for(int i=0;i<vessels.Length;i++){bool active=Canoe?i*2<setup.count:i<setup.count;vessels[i].gameObject.SetActive(active);if(active){int first=Canoe?i*2:i;int second=Mathf.Min(first+1,setup.count-1);vessels[i].SetColors(setup.players[first].color,setup.players[second].color);vessels[i].ResetVessel();if(!Canoe)vessels[i].BoundWaterSupport(footprint);}}BeginWater();}
 protected virtual void BeginWater(){}
 protected override void Simulate(float dt){Clock+=dt;for(int i=0;i<vessels.Length;i++){var v=vessels[i];if(!v.gameObject.activeSelf)continue;v.Tick(dt,Clock);if(!Canoe&&FinishTimes[i]<0&&!Stranded[i]){outsideWater[i]=v.WaterSupported?0:outsideWater[i]+dt;if(outsideWater[i]>8||v.body.position.y<course.SurfacePoint(v.body.position).y-12){Stranded[i]=true;StrandedAt[i]=Clock;Scores[i]=-1000;ConfirmOutcome("water-stranded:"+i,i,0,"STRANDED OUTSIDE THE WATER","DNF");}}if(presentationWeather!=null&&!v.body.isKinematic)v.body.AddForce(presentationWeather.Flow*.06f,ForceMode.Acceleration);TickBoat(v,i,dt);if(v.Strokes!=soundedStrokes[i]&&Clock>=nextSplash[i]){soundedStrokes[i]=v.Strokes;nextSplash[i]=Clock+.7f;feedback.PlayAt(0,v.body.position,3);}}AfterBoats(dt);bool all=true;for(int i=0;i<setup.count;i++)all&=FinishTimes[i]>=0||Stranded[i];if(all||Clock>=matchDuration){for(int i=0;i<setup.count;i++)ConfirmFinalScore(i);EndMatch();}}
 // Scores include legitimate penalties and can be negative. The existing
 // score API accepts signed totals; the outcome API is for nonnegative measured
 // durations/distances. A zero delta records the actual final total without
 // awarding the entire accumulated score a second time.
 void ConfirmFinalScore(int slot){
  string reason=Stranded[slot]?"STRANDED OUTSIDE THE WATER · DNF":"WATER EVENT";
  ConfirmScore("water-result:"+slot,slot,0,Scores[slot],reason,SportsScoreTier.Major,"PTS",matchEnded:true);
 }
 protected virtual void AfterBoats(float dt){}
 protected abstract void TickBoat(WaterVessel vessel,int index,float dt);
 public Vector3 RouteAim(int slot,WaterVessel vessel,Vector3 ordinaryTarget){var fork=option==1?GetComponent<RiverForkChallenge>():null;return fork!=null?fork.Aim(vessel,ordinaryTarget,slot):ordinaryTarget;}
 public float RoutePenaltySeconds(int slot){var fork=option==1?GetComponent<RiverForkChallenge>():null;return fork!=null?fork.PenaltySeconds(Vessel(slot)):0;}
 protected Vector2 Pilot(int slot,WaterVessel v,int gate){if(slot>=setup.count||setup.players[slot].source==BowlingSource.AI)return Navigate(slot,v,RouteAim(slot,v,course.Point(gate)));return commands[slot];}
 protected Vector2 Navigate(int slot,WaterVessel v,Vector3 target,Rigidbody contactTarget=null){int skill=slot<setup.count?(int)setup.players[slot].difficulty:1;target+=Vector3.right*Mathf.Sin(Clock*.4f+slot)*(2-skill)*1.5f;var delta=Vector3.ProjectOnPlane(target-v.body.position,Vector3.up);float yaw=Vector3.SignedAngle(v.transform.forward,delta,Vector3.up);float steer=Mathf.Clamp(yaw/(45-skill*7),-1,1);if(Physics.SphereCast(v.body.position+Vector3.up*.4f,1,v.transform.forward,out var hit,10+skill*2,~0,QueryTriggerInteraction.Ignore)&&hit.rigidbody!=v.body&&(contactTarget==null||hit.rigidbody!=contactTarget))steer=Vector3.Dot(hit.normal,v.transform.right)>0?1:-1;return new Vector2(steer,1-Mathf.Abs(steer)*.32f);}
 // Kayaks and powered barges need closed-loop heading/speed control. Keep the
 // accepted two-station canoe path untouched. Multi-hit probes discard all own
 // hull/oar contacts instead of allowing the first self hit to hide a riverbank.
 protected Vector2 NavigateWatercraft(int slot,WaterVessel vessel,Vector3 target,float cruiseSpeed,Rigidbody contactTarget=null){
  Vector3 delta=Vector3.ProjectOnPlane(target-vessel.body.position,Vector3.up);
  if(delta.sqrMagnitude<.01f)return new Vector2(0,-Mathf.Clamp(Vector3.Dot(vessel.body.linearVelocity,vessel.transform.forward),-1,1));
  Vector3 goal=delta.normalized,bestDirection=goal;float bestScore=float.NegativeInfinity;
  float reach=Mathf.Clamp(vessel.body.linearVelocity.magnitude*1.2f+7,9,20);
  float radius=course.lake?2.1f:1.25f;
  for(int sample=-3;sample<=3;sample++){
   Vector3 direction=Quaternion.AngleAxis(sample*25,Vector3.up)*goal;float clear=reach;
   int count=Physics.SphereCastNonAlloc(vessel.body.position+Vector3.up*.45f,radius,direction,navigationHits,reach,~0,QueryTriggerInteraction.Ignore);
   for(int hit=0;hit<count;hit++)if(navigationHits[hit].rigidbody!=vessel.body&&(contactTarget==null||navigationHits[hit].rigidbody!=contactTarget))clear=Mathf.Min(clear,navigationHits[hit].distance);
   float score=Vector3.Dot(direction,goal)*3+clear/reach*4;
   if(score>bestScore){bestScore=score;bestDirection=direction;}
  }
  float yaw=Vector3.SignedAngle(Vector3.ProjectOnPlane(vessel.transform.forward,Vector3.up),bestDirection,Vector3.up);
  float steer=Mathf.Clamp(yaw/35,-1,1);
  float desired=cruiseSpeed*Mathf.Max(0,Mathf.Cos(yaw*Mathf.Deg2Rad));
  float speed=Vector3.Dot(vessel.body.linearVelocity,Vector3.ProjectOnPlane(vessel.transform.forward,Vector3.up).normalized);
  return new Vector2(steer,Mathf.Clamp((desired-speed)*.6f,-1,1));
 }
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
   else if(this is WhitewaterWipeoutMatch){FinalMetric(slot,"strokes",vessel.Strokes,"individual");FinalMetric(slot,"trick_points",vessel.TrickPoints,"individual");FinalMetric(slot,"completed_rolls",vessel.Rolls,"individual");FinalMetric(slot,"landings",vessel.Landings,"individual");if(option==3)FinalMetric(slot,"survival_seconds",Stranded[slot]?StrandedAt[slot]:FinishTimes[slot]>=0?FinishTimes[slot]:Clock,"individual");}
   RecordModeFinalMetrics(slot);
  }
 }
 protected void FinalMetric(int slot,string metric,double value,string scope){SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"water_final_"+metric,setup.players[slot].participantId,"match="+metricsMatchId+"; option="+option+"; scope="+scope+"; observed_at_match_stop",value);}
 protected virtual void RecordModeFinalMetrics(int slot){}
 protected override void StopGame(){RecordFinalMetrics();if(vessels!=null)foreach(var v in vessels)if(v!=null)v.Freeze();if(broadcast!=null)broadcast.view.enabled=true;if(participantCameras!=null)foreach(var camera in participantCameras)camera.view.enabled=false;}
}
}
