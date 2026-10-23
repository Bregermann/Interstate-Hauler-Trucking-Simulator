using System;
using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckFareFight;
using LWS.TruckSports;
namespace LWS.TruckHideAndSeek
{
 public sealed class HideMatch:SportsEventSession
 {
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public HideEngine[] engines;public HideWorld world;public HidePerception perception;public HideHints hints;
  public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;
  public HideRules rules=new HideRules();public float hidingSeconds=30,seekingSeconds=180;public int ActualTags{get;private set;}public float Age{get;private set;}
  public float Visibility=>atmosphere.Kind==SportsWeatherKind.Blizzard?.42f:atmosphere.Kind==SportsWeatherKind.Hurricane?.55f:.85f;
  public override string Title=>"TRUCK HIDE & SEEK";public override int MinimumPlayers=>2;public override int MaximumPlayers=>8;public override int OptionCount=>3;public override int WeatherCount=>1;public override int CompactScoreColumns=>8;public override bool SupportsSplitAIViews=>true;
  public override string OptionLabel=>new[]{"ONE SEEKER ROUND","FOUR SEEKER ROUNDS","FULL SEEKER ROTATION"}[option];
  public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&drivingViews.rally.Humans>0;public override bool BroadcastStandingsVisible=>!ReplayControls&&drivingViews!=null&&drivingViews.rally.Humans==0;public override bool ReplayControls=>review!=null&&review.Playing;
  public override string Header=>Title+" · ROUND "+(rules.Round+1)+" / "+rules.Rounds+" · "+(rules.Phase==HidePhase.Hiding?"HIDE "+Mathf.CeilToInt(hidingSeconds-rules.Clock):rules.Phase==HidePhase.Seeking?"SEEK "+Mathf.CeilToInt(seekingSeconds-rules.Clock):"ROUND RESULTS");
  public override string Status=>Age<announcementUntil?announcement:rules.Phase==HidePhase.Hiding?"HIDERS DRIVE · SEEKER COUNTS IN THE DARK":rules.Phase==HidePhase.RoundEnd?"ROUND COMPLETE · NEXT SEEKER PREPARING":"ORIGINAL SEEKER ONLY · PHYSICAL BODY CONTACT TAGS · FOUND TRUCKS SPECTATE";
  public override string Help=>"One seeker, up to seven hiders, an enormous industrial road world. Default thirty seconds to hide, three minutes to seek. The seeker is blind and held still during hiding. Hiders can drive into open warehouses, underpasses, container yards and forest cover. Tap ACTION to toggle headlights; hold ACTION for 0.75s to stop/restart your engine. Restart takes 1.5s; pressing throttle cannot silently bypass it. Engines produce distance-sensitive sound; moving and lit trucks are easier to see. Solid walls block sight. Seeker hints are broad compass sectors, never permanent exact markers. AI searches real places and pursues only recent legitimate observations. Physical truck-body contact tags once: +1 to seeker, +3 for finding everyone; surviving hiders +2, last survivor +1. Found trucks cease hiding and their human cameras watch the original seeker. Seeker rotates between rounds. Two to eight competitors, up to four human controllers, difficulty, weather, cab/chase/elevated views, horn, pause, rematch and recorded highlights. Split-screen is a local honor-system game: another human can see an adjacent viewport. No AI hider viewport is added to a human match, to avoid revealing hiding places.";
  public override bool TryReadScoreCard(int i,out string value,out string detail){value=rules.points[i].ToString();detail=i==rules.Seeker?"SEEKER":rules.found[i]?"FOUND · SPECTATING":"HIDING";return i<setup.count;}
  public override string ViewText(int v){int i=v<drivingViews.rally.Humans?drivingViews.rally.owners[v]:rules.Seeker;return (rules.Phase==HidePhase.Hiding?"HIDE "+Mathf.CeilToInt(hidingSeconds-rules.Clock):"SEEK "+Mathf.CeilToInt(seekingSeconds-rules.Clock))+"s · "+setup.players[i].name+" · "+(i==rules.Seeker?"SEEKER · "+rules.Remaining+" LEFT":rules.found[i]?"FOUND · WATCHING SEEKER":"HIDER")+"\n"+(engines[i].Restarting?"ENGINE RESTARTING…":engines[i].Running?"ENGINE ON":"ENGINE OFF")+" · LIGHTS "+(engines[i].Lights?"ON":"OFF")+"\n"+(i==rules.Seeker&&Age-perception.knowledge.SoundAt<6?"CLUE: "+perception.knowledge.SoundBand+" / SECTOR "+(perception.knowledge.SoundSector+1):"TAP ACTION LIGHTS · HOLD ACTION ENGINE")+"\n"+ControlCaption(i);}
  readonly StringBuilder text=new StringBuilder(900);public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append(" PTS · ").Append(rules.tags[i]).Append(" FOUND · ").Append(rules.survived[i]).Append(" SURVIVED\n");return text.ToString();}}public override string Results=>"HIDE & SEEK FINAL\n\n"+Standings;
  readonly FareFightRoute[] drivers=new FareFightRoute[8];readonly int[] spots=new int[8];readonly bool[] idle=new bool[8];readonly float[] nextReposition=new float[8];readonly int[] masks=new int[4];int preparedRound=-1,searchCursor;float announcementUntil;string announcement="";
  protected override void Awake(){base.Awake();rules.Awarded+=ConfirmAward;}
  protected override void OnDestroy(){if(rules!=null)rules.Awarded-=ConfirmAward;base.OnDestroy();}
  void ConfirmAward(HideAwardKind kind,string id,int slot,int delta,int total){bool ended=kind!=HideAwardKind.Found;ConfirmScore(id,slot,delta,total,kind==HideAwardKind.Found?"HIDER FOUND":kind==HideAwardKind.Sweep?"EVERY HIDER FOUND":"HIDING SURVIVAL",ended?SportsScoreTier.RoundWinner:SportsScoreTier.Small,roundEnded:ended,matchEnded:ended&&rules.Round+1==rules.Rounds);}
  protected override void BeginGame()
  {
   rules.Reset(setup.count,option==0?1:option==1?Mathf.Min(4,setup.count):setup.count,hidingSeconds,seekingSeconds);Age=0;ActualTags=0;preparedRound=-1;rules.Begin();
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchhide";}review.ResetRecord();atmosphere.Configure(Environment.TickCount,0,1);PrepareRound();
  }
  void PrepareRound()
  {
   RestoreMasks();preparedRound=rules.Round;searchCursor=(rules.Round*7)%world.searchSpots.Length;perception.ResetKnowledge();
   int firstHuman=-1;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI){firstHuman=i;break;}
   var groups=new int[setup.count];for(int i=0;i<setup.count;i++)groups[i]=firstHuman>=0&&setup.players[i].source==BowlingSource.AI?firstHuman:i;
   drivingViews.rally.presentationGroups=groups;
   for(int i=0;i<trucks.Length;i++)
   {
    trucks[i].gameObject.SetActive(i<setup.count);if(i>=setup.count)continue;fragments[i].ResetBody();trucks[i].tractorMotor.forceSpatialAudio=true;trucks[i].tractorMotor.Configure();trucks[i].Prepare(world.starts[i],world.rotations[i]);var damage=trucks[i].GetComponent<NWH.VehiclePhysics2.Damage.DamageHandler>();if(damage!=null)damage.Repair();else{trucks[i].tractorMotor.vehicle.powertrain.Repair();foreach(var w in trucks[i].wheels)w.Damage=0;}engines[i].ResetStealth(i==rules.Seeker);drivers[i]??=new FareFightRoute();drivers[i].Reset();spots[i]=(i*5+rules.Round*3)%world.hidingSpots.Length;nextReposition[i]=0;Hold(i,true);
   }
   Physics.SyncTransforms();drivingViews.Layout();for(int i=0;i<4;i++)masks[i]=drivingViews.rally.views[i].view.cullingMask;var audio=GetComponent<SportsMatchAudioAnchor>();if(audio!=null)audio.explicitFocus=trucks[rules.Seeker].transform;Announce(setup.players[rules.Seeker].name+" IS SEEKER · FIND COVER!");
  }
  void Hold(int i,bool brake){trucks[i].TickDrive(Vector2.zero);var input=trucks[i].tractorMotor.vehicle.input;input.states.handbrake=input.states.handbrakeRaw=brake?1:0;}
  protected override void Simulate(float dt)
  {
   Age+=dt;atmosphere.Tick(Age);var old=rules.Phase;rules.Tick(dt);if(rules.Complete){EndMatch();return;}if(preparedRound!=rules.Round)PrepareRound();if(old==HidePhase.Hiding&&rules.Phase==HidePhase.Seeking)Announce("READY OR NOT · HERE COMES "+setup.players[rules.Seeker].name);
   perception.Step(Age);review.Capture(Age);
   for(int i=0;i<setup.count;i++)
   {
    bool ai=setup.players[i].source==BowlingSource.AI;bool allowed=rules.Phase==HidePhase.Hiding?i!=rules.Seeker:rules.Phase==HidePhase.Seeking&&!rules.found[i];
    engines[i].Step(!ai&&ActionHeld(i),dt,Age,allowed&&setup.players[i].released);trucks[i].tractorMotor.gripScale=atmosphere.Grip;
    if(!allowed){Hold(i,!rules.found[i]);if(rules.found[i])engines[i].RequestEngine(false,Age);continue;}
    trucks[i].TickDrive(ai?Bot(i,dt):commands[i]);if(ai&&idle[i])Hold(i,true);if(!world.Contains(trucks[i].body.position)&&trucks[i].body.position.y<world.Height(trucks[i].body.position)-8){trucks[i].Prepare(world.starts[i],world.rotations[i]);engines[i].ResetStealth(i==rules.Seeker);drivers[i].Reset();Announce(setup.players[i].name+" RECOVERED AT THE YARD");}
   }
  }
  Vector2 Bot(int i,float dt)
  {
   idle[i]=false;var t=trucks[i];Vector3 goal;SportsEventTruck visibleTarget=null;
   if(i==rules.Seeker)
   {
    engines[i].RequestEngine(true,Age);int recent=-1;float best=float.PositiveInfinity;for(int j=0;j<setup.count;j++)if(j!=i&&!rules.found[j]&&perception.knowledge.Recent(j,Age)){float d=(perception.knowledge.lastSeen[j]-t.body.position).sqrMagnitude;if(d<best){best=d;recent=j;}}
    if(recent>=0){goal=perception.knowledge.lastSeen[recent];if(perception.Visible(i,recent))visibleTarget=trucks[recent];}
    else if(Age-perception.knowledge.SoundAt<6)goal=perception.knowledge.SectorGoal(t.body.position);
    else{goal=world.searchSpots[searchCursor];if(Vector3.Distance(t.body.position,goal)<16)searchCursor=(searchCursor+1)%world.searchSpots.Length;}
   }
   else
   {
    float seekerDistance=Vector3.Distance(t.body.position,trucks[rules.Seeker].body.position);bool threat=rules.Phase==HidePhase.Seeking&&(perception.Visible(i,rules.Seeker)||seekerDistance<HideSignalRules.AudibleRange(engines[rules.Seeker].Running,trucks[rules.Seeker].body.linearVelocity.magnitude,perception.Occluded(t,trucks[rules.Seeker])));
    if(threat&&Age>=nextReposition[i]){float best=-1;for(int j=0;j<world.hidingSpots.Length;j++){float cost=Vector3.Distance(world.hidingSpots[j],trucks[rules.Seeker].body.position)-Vector3.Distance(t.body.position,world.hidingSpots[j])*.35f;if(cost>best){best=cost;spots[i]=j;}}nextReposition[i]=Age+10;drivers[i].Reset();}
    goal=world.hidingSpots[spots[i]];bool arrived=Vector3.Distance(t.body.position,goal)<8;if(arrived&&!threat){engines[i].SetLights(false);if(t.body.linearVelocity.magnitude<.6f)engines[i].RequestEngine(false,Age);idle[i]=true;return Vector2.zero;}engines[i].RequestEngine(true,Age);
   }
   var driver=drivers[i];if(Vector3.Distance(t.body.position,goal)<35)driver.DirectGoal(t.body.position,goal);else driver.Update(world.graph,t.body.position,goal,spots[i]+rules.Round*100,Age,false,t.transform.forward);
   return driver.Drive(t.body,t.transform,(int)setup.players[i].difficulty,dt,atmosphere.Grip,visibleTarget==null?null:visibleTarget.body);
  }
  public void Contact(int a,int b)
  {
   if(!Live||a<0||b<0||a>=setup.count||b>=setup.count)return;int target=a==rules.Seeker?b:b==rules.Seeker?a:-1;if(target<0||!rules.Tag(rules.Seeker,target))return;ActualTags++;engines[target].RequestEngine(false,Age);Announce("FOUND! "+setup.players[target].name+" · +1 "+setup.players[rules.Seeker].name);feedback.PlayAt(5,trucks[target].body.position,0,true);Pulse(target,.4f,.15f);
  }
  public void Announce(string value){announcement=value;announcementUntil=Age+3;}
  void RestoreMasks(){if(drivingViews?.rally?.views==null)return;for(int v=0;v<4;v++)if(masks[v]!=0)drivingViews.rally.views[v].view.cullingMask=masks[v];}
  protected override void Present(float dt)
  {
   if(drivingViews==null||world==null)return;if(ReplayControls){RestoreMasks();review.Tick();return;}drivingViews.Tick(dt);RestoreMasks();
   for(int v=0;v<drivingViews.rally.Humans;v++){int slot=drivingViews.rally.owners[v];var camera=drivingViews.rally.views[v];if(rules.Phase==HidePhase.Hiding&&slot==rules.Seeker){camera.view.clearFlags=CameraClearFlags.SolidColor;camera.view.backgroundColor=new Color(.005f,.008f,.015f);camera.view.cullingMask=0;drivingViews.cabs[v].gameObject.SetActive(false);}else{camera.view.clearFlags=CameraClearFlags.Skybox;if(rules.found[slot]){var seeker=trucks[rules.Seeker];camera.Pose(seeker.body.position+seeker.transform.forward*8,new Vector3(0,12,0)-seeker.transform.forward*24,70);drivingViews.cabs[v].gameObject.SetActive(false);}}}
   if(drivingViews.rally.Humans==0){broadcast.view.orthographic=false;var seeker=trucks[rules.Seeker];if(rules.Phase==HidePhase.Hiding){broadcast.Pose(seeker.body.position+Vector3.up*2,-seeker.transform.forward*10+Vector3.up*4,55);}else broadcast.Pose(seeker.body.position+seeker.transform.forward*12,Vector3.up*13-seeker.transform.forward*25,70);}hints?.Refresh();
  }
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
  protected override void SetPaused(bool paused){atmosphere?.Pause(paused);}protected override void StopSounds(){if(trucks!=null)foreach(var t in trucks)t.StopSound();}
  protected override void StopGame(){RestoreMasks();review?.End(false);StopSounds();if(trucks!=null)foreach(var t in trucks){t.Power(false);if(!t.body.isKinematic)t.body.linearVelocity=t.body.angularVelocity=Vector3.zero;t.body.isKinematic=true;}}
 }
}
