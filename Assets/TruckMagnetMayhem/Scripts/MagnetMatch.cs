using System;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckFareFight;
using LWS.TruckBowling;
namespace LWS.TruckMagnetMayhem
{
 public sealed class MagnetMatch:SportsEventSession
 {
  public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){int slot=framing.Nearest(framing.RequestedFocus,true);if(slot<0){slot=0;for(int i=1;i<setup.count;i++)if(carrying[i]&&!carrying[slot])slot=i;}action.primary=slot;int item=target[slot];if(item<0||rules.owners[item]>=0){float nearest=float.PositiveInfinity;item=-1;for(int i=0;i<scrap.Length;i++)if(rules.owners[i]<0){float d=(scrap[i].body.position-trucks[slot].body.position).sqrMagnitude;if(d<nearest){nearest=d;item=i;}}}if(item>=0)action.Objective(scrap[item].body.position,2);action.reason=carrying[slot]?"MAGNETIC HAUL / SCORING BAY":"LOCAL MAGNET CONTEST";}
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public MagnetMetal[] truckMetal,scrap;public MagnetBay[] bays;public MagnetFields fields;public Transform scrapGroup;
  public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;
  public MagnetSortingArm[] sortingArms;
  public MagnetRules rules=new MagnetRules();public float Countdown{get;private set;}public int Recoveries{get;private set;}public int ActualDeposits{get;private set;}
  public override string Title=>"TRUCK MAGNET MAYHEM";public override int MinimumPlayers=>1;public override int MaximumPlayers=>8;public override int WeatherCount=>1;public override int OptionCount=>3;public override int CompactScoreColumns=>8;
  public override string OptionLabel=>new[]{"TWO MINUTES","THREE MINUTES","FIVE MINUTES"}[option];public override bool SupportsSplitAIViews=>true;
  public override bool SupportsRaceCountdown=>true;public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;
  public override bool ReplayControls=>review!=null&&review.Playing;public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;public override bool BroadcastStandingsVisible=>!ReplayControls;
  public override string Header=>Title+(ReplayControls?" · RECORDED SCRAP HEIST":" · "+Mathf.Max(0,rules.Duration-rules.Clock).ToString("F0")+"s · "+rules.Remaining+" SCRAP LEFT");
  public override string Status=>Countdown>0?"FIELDS DISARMED · PREPARE TO COLLECT":elapsed<announceUntil?announcement:"OPPOSITES ATTRACT · MATCHING POLES REPEL · SETTLE METAL IN YOUR COLOR BAY";
  public override string Help=>"Eight industrial electromagnet trucks compete for a finite scrapyard of real physical metal. ACTION cycles OFF, POSITIVE (red +), NEGATIVE (blue -). Opposite poles attract; matching poles repel; neutral steel is attracted by either active pole. OFF stops your own field, but other active magnets can still affect your truck. Forces fall with distance, respect solid obstructions and have bounded force/acceleration; no teleporting magnetic grabs. Pull, push or ram scrap into your own colored bay. For controlled hauling, keep the scrap ahead of the magnet and reverse toward your bay. The WHOLE object must settle there for1.5seconds before its displayed value scores, once. Stored metal is secured and stops responding to fields. Barrels1, crates3 and vehicle scrap5points. Highest points at time-up wins; ties share placing. Clearing all finite scrap ends the match early. AI uses the same fields, real contact, driving and settling rules. Turning sorting arms are physical hazards; anchored yard buildings are not loose scrap. Shared/chase/cab, free look, Your Crew, weather, horn and optional AI views use collection controls. Rematch restores every object. Results offer recorded highlights.";
  public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace(" ACTION"," CYCLE MAGNET")+" · "+Pole(truckMetal[slot].polarity);
  public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.points[slot]+"P";detail=(truckMetal[slot].polarity>0?"POS +":truckMetal[slot].polarity<0?"NEG -":"OFF")+" · "+rules.deliveries[slot]+" STORED";return slot<setup.count;}
  public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var score,out var detail);return setup.players[slot].name+" · "+score+"\n"+detail+"\n"+ControlCaption(slot);}
  public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append("P · ").Append(rules.deliveries[i]).Append(" METAL DELIVERIES\n");return text.ToString();}}
  public override string Results=>"SCRAPYARD FINAL PLACINGS\n\n"+Standings;
  readonly StringBuilder text=new StringBuilder(900);readonly FareFightRoute[] drivers=new FareFightRoute[8];readonly int[] target=new int[8];readonly bool[] carrying=new bool[8];readonly float[] poleAt=new float[8],stalled=new float[8];
  Vector3[] spawn;Quaternion[] spawnRot;MagnetMetal[] registry;float elapsed,announceUntil;string announcement="";
  protected override void Awake(){base.Awake();spawn=Array.ConvertAll(scrap,a=>a.body.position);spawnRot=Array.ConvertAll(scrap,a=>a.body.rotation);registry=new MagnetMetal[truckMetal.Length+scrap.Length];Array.Copy(truckMetal,registry,truckMetal.Length);Array.Copy(scrap,0,registry,truckMetal.Length,scrap.Length);}
  public override bool ValidateSources(out string reason){if(!base.ValidateSources(out reason))return false;int count=0;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)count++;if(count<=4)return true;reason="Up to four local humans; assign the remaining trucks to AI.";return false;}
  protected override void BeginGame()
  {
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchmagnet";review.extras=new[]{scrapGroup};}
   review.ResetRecord();rules.Reset(setup.count,Array.ConvertAll(scrap,a=>a.points),new[]{120f,180f,300f}[option],true);elapsed=0;announceUntil=0;Countdown=3;Recoveries=ActualDeposits=0;
   for(int i=0;i<bays.Length;i++)
   {
    bays[i].gameObject.SetActive(i<setup.count);trucks[i].gameObject.SetActive(i<setup.count);if(i>=setup.count)continue;
    float angle=i*2*Mathf.PI/setup.count;var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));bays[i].transform.SetPositionAndRotation(radial*65,Quaternion.LookRotation(radial));bays[i].Colorize(setup.players[i].color);
    fragments[i].ResetBody();trucks[i].tractorMotor.Configure();trucks[i].Prepare(radial*77+Vector3.up*.65f,Quaternion.LookRotation(-radial));trucks[i].tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;trucks[i].tractorMotor.vehicle.powertrain.engine.StartEngine();truckMetal[i].polarity=0;drivers[i]??=new FareFightRoute();drivers[i].Reset();target[i]=-1;carrying[i]=false;poleAt[i]=stalled[i]=0;Hold(trucks[i]);
   }
   for(int i=0;i<scrap.Length;i++){var body=scrap[i].body;var interpolation=body.interpolation;body.interpolation=RigidbodyInterpolation.None;body.isKinematic=false;body.linearVelocity=body.angularVelocity=Vector3.zero;body.position=spawn[i];body.rotation=spawnRot[i];body.transform.SetPositionAndRotation(spawn[i],spawnRot[i]);body.isKinematic=true;body.interpolation=interpolation;scrap[i].gameObject.SetActive(true);scrap[i].shape.enabled=true;scrap[i].Colorize(scrap[i].points>=5?new Color(.88f,.66f,.18f):scrap[i].points>=3?new Color(.32f,.52f,.62f):new Color(.58f,.61f,.6f));}
   foreach(var arm in sortingArms)arm.ResetArm();fields.ResetFields(registry,setup.count,truckMetal.Length);Physics.SyncTransforms();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
  }
  static void Hold(SportsEventTruck truck){var input=truck.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
  protected override void Simulate(float dt)
  {
   elapsed+=dt;atmosphere.Tick(elapsed);
   if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);foreach(var truck in trucks)if(truck.gameObject.activeSelf)Hold(truck);if(Countdown==0){foreach(var item in scrap)item.body.isKinematic=false;rules.Begin();}return;}
   if(rules.Complete){EndMatch();return;}rules.Tick(dt);if(rules.Complete){EndMatch();return;}review.Capture(elapsed+3);
   for(int i=0;i<setup.count;i++)
   {
    var truck=trucks[i];truck.tractorMotor.gripScale=atmosphere.Grip;
    if(setup.players[i].source==BowlingSource.AI)Bot(i,dt);else{if(actions[i])SetPole(i,truckMetal[i].polarity==0?1:truckMetal[i].polarity>0?-1:0);truck.TickDrive(commands[i]);}
    stalled[i]=truck.body.linearVelocity.magnitude<1.2f&&(Vector3.Dot(truck.transform.up,Vector3.up)<.25f||truck.body.position.y< -5)?stalled[i]+dt:0;
    if(stalled[i]>4)Recover(i);
   }
   fields.Tick();
   for(int item=0;item<scrap.Length;item++)
   {
    var actor=scrap[item];if(rules.owners[item]>=0)continue;int zone=-1;for(int bay=0;bay<setup.count;bay++)if(bays[bay].Contains(actor.shape)){zone=bay;break;}
    if(!rules.Observe(item,zone,zone>=0,actor.body.linearVelocity.magnitude,actor.body.angularVelocity.magnitude,dt))continue;
    actor.body.linearVelocity=actor.body.angularVelocity=Vector3.zero;actor.body.isKinematic=true;actor.Colorize(setup.players[zone].color);ActualDeposits++;announcement=setup.players[zone].name+" SECURED +"+actor.points+"!";announceUntil=elapsed+2;feedback.PlayAt(5,actor.body.position,0,true);Pulse(zone,.5f,.15f);
   }
   if(rules.Complete){review.focusSlot=0;for(int i=1;i<setup.count;i++)if(rules.points[i]>rules.points[review.focusSlot])review.focusSlot=i;EndMatch();}
  }
  public bool SetPole(int slot,int pole)
  {
   if(!Live||Countdown>0||pole< -1||pole>1||rules.Clock<poleAt[slot]||truckMetal[slot].polarity==pole)return false;
   truckMetal[slot].polarity=pole;poleAt[slot]=rules.Clock+.25f;feedback.Play(0,0,true);return true;
  }
  static string Pole(int value)=>value>0?"POSITIVE +":value<0?"NEGATIVE -":"MAGNET OFF";
  Vector3 Slot(int player){int stored=rules.deliveries[player]%16;return bays[player].transform.TransformPoint(new Vector3((stored%4-1.5f)*4,0,(stored/4-1.5f)*5));}
  void Bot(int slot,float dt)
  {
   var truck=trucks[slot];int choice=target[slot];
   if(choice<0||rules.owners[choice]>=0)
   {
    float best=float.PositiveInfinity;choice=-1;for(int i=0;i<scrap.Length;i++)if(rules.owners[i]<0){float cost=Vector3.Distance(truck.body.position,scrap[i].body.position)-scrap[i].points*4;for(int other=0;other<setup.count;other++)if(other!=slot&&target[other]==i)cost+=18;if(cost<best){best=cost;choice=i;}}
    target[slot]=choice;carrying[slot]=false;drivers[slot].Reset();
   }
   if(choice<0){Hold(truck);SetPole(slot,0);return;}
   var actor=scrap[choice];float distance=Vector3.Distance(truckMetal[slot].Point,actor.Point);if(distance<10)carrying[slot]=true;if(distance>fields.range*.95f)carrying[slot]=false;
   if(bays[slot].Contains(actor.shape)){SetPole(slot,0);Hold(truck);return;}
   int wanted=actor.polarity==0?1:-actor.polarity;
   if((int)setup.players[slot].difficulty==2&&carrying[slot])for(int other=0;other<setup.count;other++)if(other!=slot&&truckMetal[other].polarity!=0&&Vector3.Distance(truck.body.position,trucks[other].body.position)<9&&(actor.polarity==0||truckMetal[other].polarity==-actor.polarity)){wanted=truckMetal[other].polarity;break;}
   SetPole(slot,wanted);
   var radial=bays[slot].transform.forward;var goal=carrying[slot]?Slot(slot)+radial*8:actor.body.position+(bays[slot].transform.position-actor.body.position).normalized*6;
   drivers[slot].DirectGoal(truck.body.position,goal);var drive=drivers[slot].Drive(truck.body,truck.transform,(int)setup.players[slot].difficulty,dt,atmosphere.Grip,actor.body);
   if(carrying[slot])
   {
    // Pull by backing toward the bay: the existing NWH reverse input keeps the
    // scrap ahead of the roof magnet instead of attempting a U-turn through it.
    var desiredNose=Vector3.ProjectOnPlane(truck.body.position-goal,Vector3.up);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),desiredNose,Vector3.up);
    float reverseSpeed=-Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);drive=new Vector2(reverseSpeed<-.5f?0:-Mathf.Clamp(angle/35,-1,1),reverseSpeed<4.5f?-.55f:0);
   }
   truck.TickDrive(drive);
  }
  void Recover(int slot)
  {
   var bay=bays[slot].transform;var p=bay.position+bay.forward*12+Vector3.up*.65f;for(int i=0;i<setup.count;i++)if(i!=slot&&Vector3.Distance(trucks[i].body.position,p)<13)return;
   foreach(var actor in scrap)if(Vector3.Distance(actor.body.position,p)<7)return;
   trucks[slot].Prepare(p,Quaternion.LookRotation(-bay.forward));drivers[slot].Reset();truckMetal[slot].polarity=0;stalled[slot]=0;carrying[slot]=false;Recoveries++;announcement=setup.players[slot].name+" RECOVERED · SCRAP STAYS IN PLAY";announceUntil=elapsed+2;
  }
  protected override void Present(float dt)
  {
   if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;
   var bounds=new Bounds(new Vector3(0,5,0),new Vector3(180,18,180));if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true;broadcast.Pose(bounds.center,new Vector3(0,135,-105),60);if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-135,105),Vector3.up);framing.arenaBounds=bounds;framing.Apply();
  }
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
  protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);
  protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)truck.StopSound();}
  protected override void StopGame(){review?.End(false);StopSounds();if(trucks!=null)foreach(var truck in trucks){truck.Power(false);truck.body.isKinematic=true;}if(scrap!=null)foreach(var actor in scrap){if(!actor.body.isKinematic)actor.body.linearVelocity=actor.body.angularVelocity=Vector3.zero;actor.body.isKinematic=true;}}
 }
}
