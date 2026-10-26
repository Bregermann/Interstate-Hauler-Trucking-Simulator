using System;using System.Text;using UnityEngine;using LWS.TruckSports;using LWS.TruckFareFight;using LWS.TruckBowling;
namespace LWS.TruckFreezeTag
{
 public sealed class FreezeMatch:SportsEventSession
 {
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public Transform[] muzzles;public GameObject[] cannons,shells;public LineRenderer[] sprays;public ParticleSystem[] shatter;
  public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;
  public FreezeRules rules=new FreezeRules();[Range(20,180)]public float roundSeconds=90;[Range(.4f,3)]public float shotCooldown=.8f;[Range(10,60)]public float sprayRange=35;
  public float Countdown{get;private set;}public int ActualSprayHits{get;private set;}public int ActualRescues{get;private set;}public int ActualFreezes{get;private set;}
  public override string Title=>"TRUCK FREEZE TAG";public override bool TeamSport=>true;public override int MinimumPlayers=>2;public override int MaximumPlayers=>8;public override int WeatherCount=>1;public override int OptionCount=>3;public override int CompactScoreColumns=>8;public override bool SupportsSplitAIViews=>true;
  public override string OptionLabel=>new[]{"TWO ROUNDS","FOUR ROUNDS","SIX ROUNDS"}[option];public override bool SupportsRaceCountdown=>true;public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;
  public override bool ReplayControls=>review!=null&&review.Playing;public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;public override bool BroadcastStandingsVisible=>!ReplayControls;
  public override string Header=>Title+" · ROUND "+(rules.Round+1)+" / "+rules.Rounds+" · "+Mathf.Max(0,roundSeconds-rules.Clock).ToString("F0")+"s · BLUE "+rules.wins[0]+" : "+rules.wins[1]+" ORANGE";
  public override string Status=>Countdown>0?"":elapsed<announcementUntil?announcement:"";
  public override bool CompactBroadcastStatus=>true;
        public override bool BroadcastStatusVisible=>!ReplayControls&&!string.IsNullOrEmpty(Status);
        public override bool CompactViewportHud=>true;
        public override string ViewportTelemetry(int view)=>"ROUND "+(rules.Round+1)+" · "+Mathf.Max(0,roundSeconds-rules.Clock).ToString("F0")+"s";
        public override bool TryReadViewportCard(int view,out int participant,out string score,out string detail)=>TryReadDrivingViewportCard(drivingViews,view,out participant,out score,out detail);
        public override string Help=>"Two asymmetric teams, default four trucks each. One side has visible ice cannons; the other runs and rescues. ACTION sprays a bounded physical sphere-cast ice burst straight along the cannon: aim by steering. Solid cover blocks it. Three full hits build100%ice; no shots through walls and no friendly freezing. Fully frozen trucks remain physically present inside visible ice shells and CANNOT drive. There is NO automatic thawing. An unfrozen teammate must hit the ice with a meaningful impact to shatter it and restore driving; resting overlap and enemies cannot rescue. Rescued trucks receive two seconds of protection. Freeze every runner before time expires to win the round; any surviving runner at time-up wins for the runners. Roles switch EVERY round. Choose2/4/6rounds; each side gets equal turns in each role. Most round wins takes the match, equal wins share first place. AI uses the same line-of-sight spray and real driving/contact rescue rules. Selected weather stays for the whole match. Full cab/chase/shared, independent input, horn, Your Crew, optional AI views, recorded highlights and rematches use collection controls.";
  public override string ControlCaption(int slot){var label=base.ControlCaption(slot);if((slot<teamA?0:1)==rules.Freezers)return label.Replace(" ACTION"," ICE CANNON");int at=label.LastIndexOf(" · ",StringComparison.Ordinal);return (at<0?label:label.Substring(0,at))+" · RAM TEAMMATE ICE TO RESCUE";}
  public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.Team(slot)==rules.Freezers?(elapsed>=shotAt[slot]?"READY":(shotAt[slot]-elapsed).ToString("F1")+"s"):rules.frozen[slot]?"FROZEN":rules.Clock<rules.protectedUntil[slot]?"SHIELD":Mathf.RoundToInt(rules.buildup[slot])+"%";detail=rules.Team(slot)==rules.Freezers?"FREEZER · "+rules.tags[slot]+" TAGS":"RUNNER · "+rules.rescues[slot]+" SAVES";return slot<setup.count;}
  public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var value,out var detail);return setup.players[slot].name+" · "+value+"\n"+detail;}
  public override string Standings{get{text.Clear();text.Append("BLUE ").Append(rules.wins[0]).Append(" ROUND WINS · ORANGE ").Append(rules.wins[1]).Append(" ROUND WINS\n");for(int i=0;i<setup.count;i++)text.Append(setup.players[i].name).Append(" · ").Append(rules.tags[i]).Append(" TAGS · ").Append(rules.rescues[i]).Append(" RESCUES\n");return text.ToString();}}
  public override string Results=>(rules.Winner<0?"SHARED FIRST PLACE":(rules.Winner==0?"BLUE":"ORANGE")+" WINS FREEZE TAG!")+"\n\n"+Standings;
  readonly StringBuilder text=new StringBuilder(900);readonly FareFightRoute[] drivers=new FareFightRoute[8];readonly float[] shotAt=new float[8],flashUntil=new float[8],stalled=new float[8];readonly bool[] appliedIce=new bool[8];readonly int[] targets=new int[8];readonly RaycastHit[] castHits=new RaycastHit[64];float elapsed,aftermath,announcementUntil;string announcement="";
  public override bool ValidateSources(out string reason){if(!base.ValidateSources(out reason))return false;int humans=0;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)humans++;if(humans<=4)return true;reason="Up to four local humans; assign remaining competitors to AI.";return false;}
  protected override void Awake(){base.Awake();rules.RoundAwarded+=ConfirmRound;}
  protected override void OnDestroy(){if(rules!=null)rules.RoundAwarded-=ConfirmRound;base.OnDestroy();}
  void ConfirmRound(int team)=>ConfirmTeamScore("round:"+rules.Round,team==0?"blue":"orange",team==0?"BLUE TEAM":"ORANGE TEAM",team==0?new Color(.03f,.3f,.9f):SportsBroadcastCanvas.Orange,1,rules.wins[team],team==rules.Freezers?"ALL RUNNERS FROZEN":"SURVIVED THE CLOCK",rules.Complete&&rules.Winner==team?SportsScoreTier.MatchChampion:SportsScoreTier.RoundWinner,"ROUNDS",true,rules.Complete);
  protected override void BeginGame()
  {
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchfreeze";review.extras=Array.ConvertAll(shells,s=>s.transform);}
   review.ResetRecord();rules.Reset(teamA,teamB,new[]{2,4,6}[option],roundSeconds);elapsed=0;announcementUntil=0;ActualSprayHits=ActualRescues=ActualFreezes=0;ResetRound();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
  }
  static void Hold(SportsEventTruck t){var input=t.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
  void ResetRound()
  {
   Countdown=3;aftermath=0;for(int i=0;i<8;i++){trucks[i].gameObject.SetActive(i<setup.count);shells[i].SetActive(false);sprays[i].enabled=false;shatter[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);appliedIce[i]=false;shotAt[i]=flashUntil[i]=stalled[i]=0;targets[i]=-1;if(i>=setup.count)continue;int team=rules.Team(i),local=team==0?i:i-teamA,count=team==0?teamA:teamB;var p=new Vector3((local-(count-1)*.5f)*17,.65f,team==0?-42:42);fragments[i].ResetBody();trucks[i].tractorMotor.Configure();trucks[i].Prepare(p,Quaternion.Euler(0,team==0?0:180,0));trucks[i].tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;trucks[i].tractorMotor.vehicle.powertrain.engine.StartEngine();cannons[i].SetActive(team==rules.Freezers);drivers[i]??=new FareFightRoute();drivers[i].Reset();Hold(trucks[i]);}Physics.SyncTransforms();
  }
  protected override void Simulate(float dt)
  {
   elapsed+=dt;atmosphere.Tick(elapsed);if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(Countdown==0)rules.Begin();return;}review.Capture(elapsed+3);
   for(int i=0;i<setup.count;i++)if(flashUntil[i]<elapsed)sprays[i].enabled=false;
   if(!rules.Live){if(aftermath==0){announcement=(rules.RoundWinner==0?"BLUE":"ORANGE")+(rules.Complete?" WINS THE FINAL ROUND":" WINS ROUND · ROLES SWITCH NEXT");announcementUntil=elapsed+3;feedback.Play(5,0,true);}aftermath+=dt;for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(aftermath>=3){if(rules.Complete){review.focusSlot=rules.Winner==1?teamA:0;EndMatch();}else{rules.NextRound();ResetRound();}}return;}
   rules.Tick(dt);if(!rules.Live)return;
   for(int i=0;i<setup.count;i++)
   {
    SyncIce(i);if(rules.frozen[i])continue;var truck=trucks[i];truck.tractorMotor.gripScale=atmosphere.Grip;truck.TickDrive(setup.players[i].source==BowlingSource.AI?Bot(i,dt):commands[i]);if(actions[i])Fire(i);stalled[i]=Vector3.Dot(truck.transform.up,Vector3.up)<.25f&&truck.body.linearVelocity.magnitude<1?stalled[i]+dt:0;if(stalled[i]>4){var p=new Vector3((i%4-1.5f)*17,.65f,rules.Team(i)==0?-58:58);truck.Prepare(p,Quaternion.Euler(0,rules.Team(i)==0?0:180,0));stalled[i]=0;}
   }
  }
  void SyncIce(int slot)
  {
   bool frozen=rules.frozen[slot];if(appliedIce[slot]==frozen)return;appliedIce[slot]=frozen;shells[slot].SetActive(frozen);var truck=trucks[slot];
   if(frozen){truck.Power(false);truck.body.linearVelocity=truck.body.angularVelocity=Vector3.zero;truck.body.isKinematic=true;ActualFreezes++;announcement=setup.players[slot].name+" IS FROZEN · RAM TO RESCUE";}
   else{truck.body.isKinematic=false;truck.tractorMotor.ResetAttachedBodies();truck.Power(true);truck.tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;truck.tractorMotor.vehicle.powertrain.engine.StartEngine();shatter[slot].Play();feedback.PlayAt(3,truck.body.position,4,true);announcement=setup.players[slot].name+" RESCUED · 2s ICE SHIELD";}
   announcementUntil=elapsed+2;
  }
  Vector2 Bot(int slot,float dt)
  {
   var t=trucks[slot];bool freezer=rules.Team(slot)==rules.Freezers;int target=-1;float nearest=float.PositiveInfinity;
   for(int i=0;i<setup.count;i++)if(i!=slot&&(freezer?rules.Team(i)!=rules.Freezers&&!rules.frozen[i]:rules.Team(i)==rules.Team(slot)&&rules.frozen[i])){float d=Vector3.Distance(t.body.position,trucks[i].body.position);float cost=d-(freezer?rules.buildup[i]*.06f:0);for(int other=0;other<setup.count;other++)if(other!=slot&&rules.Team(other)==rules.Team(slot)&&!rules.frozen[other]&&targets[other]==i)cost+=18;if(cost<nearest){nearest=cost;target=i;}}
   if(!freezer&&target>=0)for(int other=0;other<setup.count;other++)if(other!=slot&&rules.Team(other)==rules.Team(slot)&&!rules.frozen[other]&&targets[other]==target&&Vector3.Distance(trucks[other].body.position,trucks[target].body.position)+3<Vector3.Distance(t.body.position,trucks[target].body.position)){target=-1;break;}
   targets[slot]=target;Vector3 goal;
   if(target>=0){goal=trucks[target].body.position;float distance=Vector3.Distance(t.body.position,goal);float aim=(int)setup.players[slot].difficulty==0?12:(int)setup.players[slot].difficulty==1?8:5;if(freezer&&distance<sprayRange&&Vector3.Angle(Vector3.ProjectOnPlane(t.transform.forward,Vector3.up),Vector3.ProjectOnPlane(goal-t.body.position,Vector3.up))<aim)Fire(slot);}
   else{nearest=float.PositiveInfinity;int threat=-1;for(int i=0;i<setup.count;i++)if(rules.Team(i)==rules.Freezers){float d=Vector3.Distance(t.body.position,trucks[i].body.position);if(d<nearest){nearest=d;threat=i;}}var away=threat<0?new Vector3(Mathf.Sin(elapsed*.1f+slot),0,Mathf.Cos(elapsed*.1f+slot)):Vector3.ProjectOnPlane(t.body.position-trucks[threat].body.position,Vector3.up).normalized;goal=t.body.position+away*22+Vector3.Cross(away,Vector3.up)*(slot%2==0?14:-14);goal.x=Mathf.Clamp(goal.x,-70,70);goal.z=Mathf.Clamp(goal.z,-65,65);}
   drivers[slot].DirectGoal(t.body.position,goal);return drivers[slot].Drive(t.body,t.transform,(int)setup.players[slot].difficulty,dt,atmosphere.Grip,target>=0?trucks[target].body:null);
  }
  public bool Fire(int slot)
  {
   if(!Live||Countdown>0||!rules.Live||rules.Team(slot)!=rules.Freezers||elapsed<shotAt[slot])return false;shotAt[slot]=elapsed+shotCooldown;var origin=muzzles[slot].position;var direction=(Vector3.ProjectOnPlane(trucks[slot].transform.forward,Vector3.up)-Vector3.up*.035f).normalized;
   int count=Physics.SphereCastNonAlloc(origin,.85f,direction,castHits,sprayRange,~0,QueryTriggerInteraction.Ignore);RaycastHit closest=default;float distance=sprayRange;bool found=false;for(int i=0;i<count;i++)if(castHits[i].distance<distance&&!castHits[i].collider.transform.IsChildOf(trucks[slot].transform)){closest=castHits[i];distance=closest.distance;found=true;}
   var line=sprays[slot];line.enabled=true;line.positionCount=2;line.SetPosition(0,origin);line.SetPosition(1,origin+direction*distance);flashUntil[slot]=elapsed+.14f;feedback.PlayAt(0,origin,0,false);
   if(!found)return true;var victim=closest.collider.GetComponentInParent<SportsEventTruck>();if(victim!=null&&victim.session==this&&rules.Hit(slot,victim.slot)){ActualSprayHits++;SyncIce(victim.slot);feedback.PlayAt(1,closest.point,0,false);}return true;
  }
  public bool Contact(int a,int b,float normalSpeed){if(!Live||Countdown>0)return false;if(rules.Rescue(a,b,normalSpeed)){SyncIce(b);ActualRescues++;Pulse(a,.5f,.15f);return true;}if(rules.Rescue(b,a,normalSpeed)){SyncIce(a);ActualRescues++;Pulse(b,.5f,.15f);return true;}return false;}
  protected override void Present(float dt){if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;var bounds=new Bounds(new Vector3(0,5,0),new Vector3(170,18,165));if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true;broadcast.Pose(bounds.center,new Vector3(0,130,-110),60);if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-130,110),Vector3.up);framing.arenaBounds=bounds;framing.Apply();}
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
  protected override void SetPaused(bool pause)=>atmosphere?.Pause(pause);protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)truck.StopSound();}
  protected override void StopGame(){review?.End(false);StopSounds();if(trucks!=null)foreach(var t in trucks){t.Power(false);t.body.isKinematic=true;}if(sprays!=null)foreach(var line in sprays)line.enabled=false;if(shatter!=null)foreach(var effect in shatter)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 }
}
