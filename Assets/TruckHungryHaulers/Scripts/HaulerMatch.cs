using System;using System.Text;using UnityEngine;using LWS.TruckSports;using LWS.TruckFareFight;using LWS.TruckBowling;
namespace LWS.TruckHungryHaulers
{
 public sealed class HaulerMatch:SportsEventSession
 {
 public override void DescribeBroadcast(SportsBroadcastAction a,SportsBroadcastFraming f){int slot=f.Nearest(f.RequestedFocus,true);if(slot<0)slot=f.Nearest(f.RequestedFocus);a.primary=slot;if(slot<0)return;if(rules.carried[slot]>0)a.Objective(banks[slot].position,2);else{int best=-1;float distance=float.PositiveInfinity;for(int i=0;i<balls.Length;i++)if(Available(i)){float d=(balls[i].body.position-trucks[slot].body.position).sqrMagnitude;if(d<distance){distance=d;best=i;}}if(best>=0)a.Objective(balls[best].body.position,1);}a.reason="SCOOP / LOOSE BALL OR OWNED BANK";}
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public HaulerScoop[] scoops;public HaulerBall[] balls;public Transform[] banks;public ParticleSystem[] blasts;public Transform ballGroup;public HaulerHopper[] hoppers;public HaulerBankHole[] holes;public HaulerBallSupport ballSupport;public int ActualReleases{get;private set;}
  public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;
  public HaulerRules rules=new HaulerRules();public float Countdown{get;private set;}public int ActualCollections{get;private set;}public int ActualDeposits{get;private set;}public int ActualBombs{get;private set;}public int ActualRams{get;private set;}
  public override string Title=>"HUNGRY HUNGRY HAULERS";public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;public override int OptionCount=>3;public override int WeatherCount=>1;public override int CompactScoreColumns=>8;public override bool SupportsSplitAIViews=>true;public override string OptionLabel=>new[]{"TWO MINUTES","THREE MINUTES","FIVE MINUTES"}[option];
  public override bool SupportsRaceCountdown=>true;public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;public override bool ReplayControls=>review!=null&&review.Playing;public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;public override bool BroadcastStandingsVisible=>false;public override bool BroadcastHeaderVisible=>false;public override bool BroadcastStatusVisible=>false;
  public override string Header=>Title+" · "+Mathf.Max(0,rules.Duration-rules.Clock).ToString("F0")+"s · "+rules.PositiveRemaining+" VALUABLE BALLS LEFT";
  public override string Status=>Countdown>0?"SCOOPS HELD · GET READY":elapsed<announcementUntil?announcement:"ACTION TO SCOOP · ALIGN AT YOUR COLOR HOLE AND ACTION TO DUMP · GOLD5 · NORMAL1";
  public override string Help=>"Four semi-trucks in a giant toy stadium,256real bouncing balls. Away from your scoring zone, ACTION extends/retracts the actual telescoping scoop. Balls must physically touch the open intake lip or mouth. Up to6balls fit in the visible rear cargo hopper; collecting earns no points. Drive to your own colored recessed hole, align facing outward and slow below4m/s, then press ACTION to open the tailgate and tip the hopper. Balls release one at a time and fall/bounce physically. Only released balls actually entering your hole score their original1/5points once. A miss stays physical and can be scooped again; driving near the hole never automatically scores. Trucks retain the continuous driving surface across ball-only holes. Black fuse balls explode, spill cargo and give bounded physical knockback. A meaningful rival ram can knock loose half a load; banked balls are secured. AI uses exactly the same manual unload request, alignment, physical release and hole-entry rules. The round ends when all valuable balls are banked or time expires. Equal scores share placing. Default three minutes,2/5minute options. Shared/chase/full cab, Your Crew, horn, weather, optional AI views, independent controls, recorded highlights and rematches use collection systems.";
  public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace(" ACTION",NearBank(slot)?" DUMP AT OWN HOLE":scoops[slot].Extended?" RETRACT SCOOP":" EXTEND SCOOP");
  public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.points[slot]+"P";detail=rules.carried[slot]+" / "+rules.Capacity+" CARGO · "+(scoops[slot].Extended?"OPEN":"CLOSED");return slot<setup.count;}
  public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var value,out var detail);return setup.players[slot].name+" · "+value+"\n"+detail+"\n"+ControlCaption(slot);}
  public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append("P · ").Append(rules.deposits[i]).Append(" BANKED · ").Append(rules.gold[i]).Append(" GOLD · ").Append(rules.bombs[i]).Append(" BOMBS\n");return text.ToString();}}
  public override string Results=>"HUNGRY HAULERS FINAL PLACINGS\n\n"+Standings;
  readonly StringBuilder text=new StringBuilder(900);readonly FareFightRoute[] drivers=new FareFightRoute[4];readonly float[] impactAt=new float[4],releaseAt=new float[4],closeAt=new float[4],stalled=new float[4];readonly int[] release=new int[32],targets={-1,-1,-1,-1};Vector3[] spawn,claims;Quaternion[] rotations;float[] claimedAt;int[] cargoSlot;readonly bool[] bankEntering=new bool[4],bankHold=new bool[4];float elapsed,announcementUntil,ending;string announcement="";
  protected override void Awake(){base.Awake();spawn=Array.ConvertAll(balls,b=>b.body.position);rotations=Array.ConvertAll(balls,b=>b.body.rotation);claims=new Vector3[balls.Length];claimedAt=new float[balls.Length];cargoSlot=new int[balls.Length];}
  protected override void BeginGame()
  {
   if(hoppers==null||hoppers.Length!=4||holes==null||holes.Length!=4||ballSupport==null)throw new InvalidOperationException("Run the explicit HaulerPhysicalDumpAuthoring.Install upgrade before starting this saved scene.");
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchhauler";review.extras=new[]{ballGroup};}
   ending=0;review.ResetRecord();rules.Reset(setup.count,Array.ConvertAll(balls,b=>b.value),new[]{120f,180f,300f}[option],6);Countdown=3;elapsed=announcementUntil=0;ActualCollections=ActualDeposits=ActualBombs=ActualRams=ActualLipCollections=ActualReleases=0;
   for(int i=0;i<4;i++){trucks[i].gameObject.SetActive(i<setup.count);banks[i].gameObject.SetActive(i<setup.count);scoops[i].Deploy(false,true);impactAt[i]=releaseAt[i]=closeAt[i]=stalled[i]=0;hoppers[i].SetOpen(false,true);holes[i].Align();targets[i]=-1;bankEntering[i]=bankHold[i]=false;blasts[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(i>=setup.count)continue;float a=i*2*Mathf.PI/setup.count;var radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));banks[i].position=radial*74;banks[i].rotation=Quaternion.LookRotation(radial);holes[i].Align();fragments[i].ResetBody();trucks[i].tractorMotor.Configure();trucks[i].Prepare(radial*66+Vector3.up*.65f,Quaternion.LookRotation(-radial));var damage=trucks[i].GetComponent<NWH.VehiclePhysics2.Damage.DamageHandler>();if(damage!=null)damage.Repair();else{trucks[i].tractorMotor.vehicle.powertrain.Repair();foreach(var wheel in trucks[i].wheels)wheel.Damage=0;}trucks[i].tractorMotor.vehicle.powertrain.engine.revLimiterActive=false;trucks[i].tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;trucks[i].tractorMotor.vehicle.powertrain.engine.StartEngine();scoops[i].Colorize(setup.players[i].color);foreach(var panel in banks[i].GetComponentsInChildren<Renderer>()){var color=new MaterialPropertyBlock();color.SetColor("_BaseColor",setup.players[i].color);panel.SetPropertyBlock(color);}drivers[i]??=new FareFightRoute();drivers[i].Reset();Hold(trucks[i]);}
   for(int i=0;i<balls.Length;i++){var b=balls[i];b.gameObject.SetActive(true);b.body.interpolation=RigidbodyInterpolation.None;b.body.isKinematic=false;b.body.linearVelocity=b.body.angularVelocity=Vector3.zero;b.body.position=spawn[i];b.body.rotation=rotations[i];b.transform.SetPositionAndRotation(spawn[i],rotations[i]);b.body.isKinematic=true;b.body.interpolation=RigidbodyInterpolation.Interpolate;b.shape.enabled=true;cargoSlot[i]=-1;}Physics.SyncTransforms();ballSupport.Select(setup.count);drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
  }
  static void Hold(SportsEventTruck t){var input=t.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
  protected override void Simulate(float dt)
  {
   elapsed+=dt;atmosphere.Tick(elapsed);if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(Countdown==0){rules.Begin();foreach(var b in balls)b.body.isKinematic=false;}return;}rules.Tick(dt);review.Capture(elapsed+3);
   if(rules.Complete){ending+=dt;for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(ending<.65f)return;review.focusSlot=0;for(int i=1;i<setup.count;i++)if(rules.points[i]>rules.points[review.focusSlot])review.focusSlot=i;EndMatch();return;}
   for(int i=0;i<setup.count;i++)
   {
    var t=trucks[i];t.tractorMotor.gripScale=atmosphere.Grip;bool ai=setup.players[i].source==BowlingSource.AI;t.TickDrive(ai?Bot(i,dt):commands[i]);if(ai&&bankHold[i]||hoppers[i].Open)Hold(t);if(actions[i]){if(NearBank(i))RequestDump(i);else if(!hoppers[i].Open)scoops[i].Deploy(!scoops[i].Extended);}TickDump(i);
    stalled[i]=Vector3.Dot(t.transform.up,Vector3.up)<.3f&&t.body.linearVelocity.magnitude<1?stalled[i]+dt:0;if(stalled[i]>4){Scatter(i,32,4);t.Prepare(banks[i].position-banks[i].forward*12+Vector3.up*.65f,Quaternion.LookRotation(-banks[i].forward));stalled[i]=0;drivers[i].Reset();}
   }
   for(int i=0;i<balls.Length;i++)if(rules.state[i]==HaulerBallState.Carried)
   {
    int owner=rules.owners[i],order=cargoSlot[i];var target=hoppers[owner].StoragePoint(order,balls[i].shape.radius);float t=Mathf.Clamp01((rules.Clock-claimedAt[i])/.35f);var p=Vector3.Lerp(claims[i],target,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*2);balls[i].body.MovePosition(p);balls[i].body.MoveRotation(trucks[owner].body.rotation);
   }
  }
  Vector2 Bot(int slot,float dt)
  {
   var t=trucks[slot];if(hoppers[slot].Open){bankHold[slot]=true;return Vector2.zero;}int count=rules.carried[slot],threshold=(int)setup.players[slot].difficulty==0?3:rules.Capacity;bool bank=count>=threshold||count>0&&rules.Duration-rules.Clock<25;bankHold[slot]=false;if(count==0)bankEntering[slot]=false;Vector3 goal;
   if(!bank){int choice=targets[slot];if(choice<0||!Available(choice)){float best=float.PositiveInfinity;choice=-1;for(int i=0;i<balls.Length;i++)if(balls[i].value>0&&Available(i)){float cost=Vector3.Distance(t.body.position,balls[i].body.position)-balls[i].value*3;if(cost<best){best=cost;choice=i;}}targets[slot]=choice;}if(choice<0)bank=true;goal=choice>=0?balls[choice].body.position:banks[slot].position;}
   else goal=banks[slot].position;
   scoops[slot].Deploy(!bank);if(bank){var approach=banks[slot].position-banks[slot].forward*22;if(!bankEntering[slot]&&Vector3.ProjectOnPlane(t.body.position-approach,Vector3.up).magnitude<6)bankEntering[slot]=true;goal=bankEntering[slot]?banks[slot].position+banks[slot].forward*3.7f:approach;var rear=hoppers[slot].bed.position;if(bankEntering[slot]&&Vector3.ProjectOnPlane(rear-banks[slot].position,Vector3.up).magnitude<1.4f&&Vector3.Dot(t.transform.forward,banks[slot].forward)>.75f){bankHold[slot]=true;RequestDump(slot);return Vector2.zero;}}
   drivers[slot].DirectGoal(t.body.position,goal);return drivers[slot].Drive(t.body,t.transform,(int)setup.players[slot].difficulty,dt,atmosphere.Grip,!bank&&targets[slot]>=0?balls[targets[slot]].body:null);
  }
  public Vector3 AIGoal(int slot)=>drivers[slot]!=null?drivers[slot].Goal:Vector3.zero;
  public int ActualLipCollections{get;private set;}
  public bool Capture(int slot,HaulerBall ball)
  {
   if(ball==null||slot<0||slot>=setup.count||(scoops[slot].mouth.ClosestPoint(ball.body.worldCenterOfMass)-ball.body.worldCenterOfMass).sqrMagnitude>ball.shape.radius*ball.shape.radius)return false;
   return Claim(slot,ball);
  }
  public bool CaptureImpact(int slot,Collision collision)
  {
   if(slot<0||slot>=setup.count||collision.rigidbody==null)return false;var ball=collision.rigidbody.GetComponent<HaulerBall>();if(ball==null)return false;var scoop=scoops[slot];
   for(int i=0;i<collision.contactCount;i++)
   {
    var contact=collision.GetContact(i);if(contact.thisCollider==null||contact.thisCollider.GetComponentInParent<HaulerScoop>()!=scoop)continue;var point=scoop.moving.InverseTransformPoint(contact.point);
    // The physical teeth can rebound a rubber ball before a discrete trigger
    // overlaps. Only actual contacts on the visible open intake lip qualify.
    if(Mathf.Abs(point.x)<=3.7f&&point.z>=5.6f&&point.z<=6.65f&&point.y<2.9f&&Claim(slot,ball)){ActualLipCollections++;return true;}
   }
   return false;
  }
  bool Claim(int slot,HaulerBall ball)
  {
   if(!Live||Countdown>0||!scoops[slot].Ready||hoppers[slot].Open)return false;var take=rules.Collect(ball.id,slot);if(take==HaulerTake.Ignored)return false;
   if(take==HaulerTake.Explosive){ActualBombs++;ball.shape.enabled=false;ball.gameObject.SetActive(false);Scatter(slot,32,8);trucks[slot].tractorMotor.AddAssemblyVelocity(Vector3.up*6-trucks[slot].transform.forward*4);blasts[slot].transform.position=ball.body.position;blasts[slot].Play();feedback.PlayAt(3,trucks[slot].body.position,10,true);announcement=setup.players[slot].name+" SCOOPED A BOMB · CARGO SPILLED!";announcementUntil=elapsed+2;return true;}
   claims[ball.id]=ball.body.position;claimedAt[ball.id]=rules.Clock;int free=0;for(;free<rules.Capacity;free++){bool used=false;for(int i=0;i<balls.Length;i++)if(i!=ball.id&&rules.state[i]==HaulerBallState.Carried&&rules.owners[i]==slot&&cargoSlot[i]==free)used=true;if(!used)break;}cargoSlot[ball.id]=free;ball.body.linearVelocity=ball.body.angularVelocity=Vector3.zero;ball.body.isKinematic=true;ball.shape.enabled=false;ActualCollections++;feedback.PlayAt(0,ball.body.position,0,false);return true;
  }
  bool Available(int id)=>rules.state[id]==HaulerBallState.Loose||rules.state[id]==HaulerBallState.Released;
  bool NearBank(int slot)=>banks!=null&&hoppers!=null&&slot>=0&&slot<setup.count&&hoppers[slot]!=null&&Vector3.ProjectOnPlane(hoppers[slot].bed.position-banks[slot].position,Vector3.up).magnitude<10;
  public bool CanDump(int slot)=>Live&&rules.Live&&Countdown<=0&&slot>=0&&slot<setup.count&&NearBank(slot)&&!hoppers[slot].Open&&rules.carried[slot]>0&&trucks[slot].body.linearVelocity.magnitude<4&&Vector3.Dot(trucks[slot].transform.up,Vector3.up)>=.7f&&Vector3.Dot(trucks[slot].transform.forward,banks[slot].forward)>=.65f;
  public string DumpContext(int slot)
  {
   if(!Live||!rules.Live||Countdown>0||slot<0||slot>=setup.count||setup.players[slot].source==BowlingSource.AI)return null;
   if(hoppers[slot].Open)return "DUMPING";if(!NearBank(slot)||rules.carried[slot]==0)return null;if(trucks[slot].body.linearVelocity.magnitude>=4)return "SLOW TO DUMP";if(!CanDump(slot))return "ALIGN TO DUMP";
   var source=setup.players[slot].source;string key=source==BowlingSource.Keyboard?actionKeys[slot].ToString().ToUpperInvariant():source==BowlingSource.LeftStick?"LT":source==BowlingSource.RightStick?"RT":"SOUTH";return key+" · DUMP";
  }
  public bool RequestDump(int slot)
  {
   if(!CanDump(slot))return false;
   scoops[slot].Deploy(false);hoppers[slot].SetOpen(true);releaseAt[slot]=rules.Clock+.6f;closeAt[slot]=0;return true;
  }
  void TickDump(int slot)
  {
   var hopper=hoppers[slot];if(!hopper.Open)return;
   if(rules.carried[slot]>0)
   {
    if(!hopper.Ready||rules.Clock<releaseAt[slot])return;
    int selected=-1;for(int i=0;i<balls.Length;i++)if(rules.state[i]==HaulerBallState.Carried&&rules.owners[i]==slot&&(selected<0||cargoSlot[i]<cargoSlot[selected]))selected=i;
    if(selected>=0&&rules.Release(selected,slot))
    {
     var ball=balls[selected];ball.body.isKinematic=false;ball.shape.enabled=true;ballSupport.BindBall(ball);ball.body.linearVelocity=trucks[slot].body.GetPointVelocity(ball.body.worldCenterOfMass);ball.body.angularVelocity=trucks[slot].body.angularVelocity;ball.body.WakeUp();cargoSlot[selected]=-1;ActualReleases++;releaseAt[slot]=rules.Clock+.22f;
    }
   }
   else if(closeAt[slot]==0)closeAt[slot]=rules.Clock+3f;
   else if(rules.Clock>=closeAt[slot]){hopper.SetOpen(false);bankEntering[slot]=bankHold[slot]=false;}
  }
  public bool BankBall(HaulerBankHole hole,HaulerBall ball)
  {
   if(!Live||Countdown>0||hole==null||ball==null||ball.id<0||ball.id>=balls.Length||balls[ball.id]!=ball||hole.slot<0||hole.slot>=setup.count||holes[hole.slot]!=hole||ball.body.isKinematic||!hole.collection.bounds.Contains(ball.body.worldCenterOfMass)||ball.body.worldCenterOfMass.y>=hole.pit.position.y-.6f)return false;
   int slot=hole.slot,delta=rules.Bank(ball.id,slot);if(delta==0)return false;
   ConfirmScore("bank-ball:"+ball.id,slot,delta,rules.points[slot],ball.value==5?"GOLD BALL BANKED":"BALL BANKED",ball.value>=5?SportsScoreTier.Major:SportsScoreTier.Small,roundEnded:rules.Complete,matchEnded:rules.Complete);
   ActualDeposits++;ball.body.linearVelocity=ball.body.angularVelocity=Vector3.zero;ball.body.isKinematic=true;ball.shape.enabled=false;ball.gameObject.SetActive(false);announcement=setup.players[slot].name+" BANKED +"+delta+"!";announcementUntil=elapsed+2;feedback.PlayAt(5,hole.pit.position,0,true);Pulse(slot,.5f,.15f);return true;
  }
  void Scatter(int slot,int max,float strength)
  {
   int count=rules.Drop(slot,max,release);for(int i=0;i<count;i++){int id=release[i];var b=balls[id];var offset=trucks[slot].transform.TransformDirection(new Vector3((i%3-1)*1.6f,3+i/3*.2f,-4));var p=trucks[slot].body.position+offset;b.body.isKinematic=false;b.body.position=p;b.transform.position=p;b.shape.enabled=true;ballSupport.BindBall(b);b.body.linearVelocity=trucks[slot].body.linearVelocity+Vector3.ClampMagnitude(offset.normalized*strength+Vector3.up*4,14);b.body.angularVelocity=Vector3.one*(i%2==0?4:-4);cargoSlot[id]=-1;}
  }
  public void Ram(int a,int b,float speed){if(!Live||!rules.Live||Countdown>0||elapsed<impactAt[a]||elapsed<impactAt[b])return;impactAt[a]=impactAt[b]=elapsed+.7f;Scatter(a,(rules.carried[a]+1)/2,5);Scatter(b,(rules.carried[b]+1)/2,5);ActualRams++;feedback.PlayAt(1,(trucks[a].body.position+trucks[b].body.position)*.5f,speed);}
  protected override void Present(float dt){if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;var bounds=new Bounds(new Vector3(0,5,0),new Vector3(180,18,180));if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true;broadcast.Pose(bounds.center,new Vector3(0,135,-110),60);if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-135,110),Vector3.up);framing.arenaBounds=bounds;framing.Apply();}
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);protected override void StopSounds(){if(trucks!=null)foreach(var t in trucks)t.StopSound();}

  protected override void StopGame(){if(hoppers!=null)foreach(var hopper in hoppers)hopper.SetOpen(false,true);review?.End(false);StopSounds();if(trucks!=null)foreach(var t in trucks){t.Power(false);t.body.isKinematic=true;}if(balls!=null)foreach(var b in balls){if(!b.body.isKinematic)b.body.linearVelocity=b.body.angularVelocity=Vector3.zero;b.body.isKinematic=true;}if(scoops!=null)foreach(var scoop in scoops)scoop.Deploy(scoop.Extended,true);}
 }
}
