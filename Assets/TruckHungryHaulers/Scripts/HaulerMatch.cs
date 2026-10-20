using System;using System.Text;using UnityEngine;using DG.Tweening;using LWS.TruckSports;using LWS.TruckFareFight;using LWS.TruckBowling;
namespace LWS.TruckHungryHaulers
{
 public sealed class HaulerMatch:SportsEventSession
 {
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public HaulerScoop[] scoops;public HaulerBall[] balls;public Transform[] banks;public ParticleSystem[] blasts;public Transform ballGroup;
  public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;
  public HaulerRules rules=new HaulerRules();public float Countdown{get;private set;}public int ActualCollections{get;private set;}public int ActualDeposits{get;private set;}public int ActualBombs{get;private set;}public int ActualRams{get;private set;}
  public override string Title=>"HUNGRY HUNGRY HAULERS";public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;public override int OptionCount=>3;public override int WeatherCount=>1;public override int CompactScoreColumns=>8;public override bool SupportsSplitAIViews=>true;public override string OptionLabel=>new[]{"TWO MINUTES","THREE MINUTES","FIVE MINUTES"}[option];
  public override bool SupportsRaceCountdown=>true;public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;public override bool ReplayControls=>review!=null&&review.Playing;public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;public override bool BroadcastStandingsVisible=>!ReplayControls;
  public override string Header=>Title+" · "+Mathf.Max(0,rules.Duration-rules.Clock).ToString("F0")+"s · "+rules.PositiveRemaining+" VALUABLE BALLS LEFT";
  public override string Status=>Countdown>0?"SCOOPS HELD · GET READY":elapsed<announcementUntil?announcement:"EXTEND TO SCOOP · RETURN CARGO TO YOUR COLOR BIN · GOLD5 · NORMAL1 · AVOID BOMBS";
  public override string Help=>"Four semi-trucks in a giant toy stadium,256real bouncing balls. ACTION extends/retracts your actual telescoping scoop. A ball must physically touch the open intake lip or enter its mouth; there is no distant pickup radius. Up to6balls fit in the visible rear cargo hopper. Collection earns NO points. Drive to your own colored collection bin and slow below4m/s to deposit. Only deposited normal/golden balls score1/5points, once. Black fuse balls explode, scatter your carried cargo and give a bounded physical knockback. A meaningful rival ram can also knock loose half your load; deposited balls are secured. AI uses the same real scoop, capacity, driving, hazard and deposit rules. The round ends when every valuable ball is banked or time expires. Highest score wins; equal scores share placing. Default three minutes,2/5minute options. Shared/chase/full cab, Your Crew, horn, weather, optional AI views, independent controls, recorded highlights and rematches use collection systems.";
  public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace(" ACTION",scoops[slot].Extended?" RETRACT SCOOP":" EXTEND SCOOP");
  public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.points[slot]+"P";detail=rules.carried[slot]+" / "+rules.Capacity+" CARGO · "+(scoops[slot].Extended?"OPEN":"CLOSED");return slot<setup.count;}
  public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var value,out var detail);return setup.players[slot].name+" · "+value+"\n"+detail+"\n"+ControlCaption(slot);}
  public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append("P · ").Append(rules.deposits[i]).Append(" BANKED · ").Append(rules.gold[i]).Append(" GOLD · ").Append(rules.bombs[i]).Append(" BOMBS\n");return text.ToString();}}
  public override string Results=>"HUNGRY HAULERS FINAL PLACINGS\n\n"+Standings;
  readonly StringBuilder text=new StringBuilder(900);readonly FareFightRoute[] drivers=new FareFightRoute[4];readonly float[] impactAt=new float[4],settle=new float[4],stalled=new float[4];readonly int[] release=new int[32],targets={-1,-1,-1,-1};Vector3[] spawn,claims;Quaternion[] rotations;float[] claimedAt;int[] cargoSlot;Tween[] unloads;readonly bool[] bankEntering=new bool[4],bankHold=new bool[4];float elapsed,announcementUntil,ending;string announcement="";
  protected override void Awake(){base.Awake();spawn=Array.ConvertAll(balls,b=>b.body.position);rotations=Array.ConvertAll(balls,b=>b.body.rotation);claims=new Vector3[balls.Length];claimedAt=new float[balls.Length];cargoSlot=new int[balls.Length];}
  protected override void BeginGame()
  {
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchhauler";review.extras=new[]{ballGroup};}
   if(unloads!=null)foreach(var tween in unloads)tween?.Kill();unloads=new Tween[balls.Length];ending=0;review.ResetRecord();rules.Reset(setup.count,Array.ConvertAll(balls,b=>b.value),new[]{120f,180f,300f}[option],6);Countdown=3;elapsed=announcementUntil=0;ActualCollections=ActualDeposits=ActualBombs=ActualRams=ActualLipCollections=0;
   for(int i=0;i<4;i++){trucks[i].gameObject.SetActive(i<setup.count);banks[i].gameObject.SetActive(i<setup.count);scoops[i].Deploy(false,true);impactAt[i]=settle[i]=stalled[i]=0;targets[i]=-1;bankEntering[i]=bankHold[i]=false;blasts[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(i>=setup.count)continue;float a=i*2*Mathf.PI/setup.count;var radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));banks[i].position=radial*74;banks[i].rotation=Quaternion.LookRotation(radial);fragments[i].ResetBody();trucks[i].tractorMotor.Configure();trucks[i].Prepare(radial*66+Vector3.up*.65f,Quaternion.LookRotation(-radial));var damage=trucks[i].GetComponent<NWH.VehiclePhysics2.Damage.DamageHandler>();if(damage!=null)damage.Repair();else{trucks[i].tractorMotor.vehicle.powertrain.Repair();foreach(var wheel in trucks[i].wheels)wheel.Damage=0;}trucks[i].tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;trucks[i].tractorMotor.vehicle.powertrain.engine.StartEngine();scoops[i].Colorize(setup.players[i].color);foreach(var panel in banks[i].GetComponentsInChildren<Renderer>()){var color=new MaterialPropertyBlock();color.SetColor("_BaseColor",setup.players[i].color);panel.SetPropertyBlock(color);}drivers[i]??=new FareFightRoute();drivers[i].Reset();Hold(trucks[i]);}
   for(int i=0;i<balls.Length;i++){var b=balls[i];b.gameObject.SetActive(true);b.body.interpolation=RigidbodyInterpolation.None;b.body.isKinematic=false;b.body.linearVelocity=b.body.angularVelocity=Vector3.zero;b.body.position=spawn[i];b.body.rotation=rotations[i];b.transform.SetPositionAndRotation(spawn[i],rotations[i]);b.body.isKinematic=true;b.body.interpolation=RigidbodyInterpolation.Interpolate;b.shape.enabled=true;cargoSlot[i]=-1;}Physics.SyncTransforms();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
  }
  static void Hold(SportsEventTruck t){var input=t.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
  protected override void Simulate(float dt)
  {
   elapsed+=dt;atmosphere.Tick(elapsed);if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(Countdown==0){rules.Begin();foreach(var b in balls)b.body.isKinematic=false;}return;}rules.Tick(dt);review.Capture(elapsed+3);
   if(rules.Complete){ending+=dt;for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(ending<.65f)return;review.focusSlot=0;for(int i=1;i<setup.count;i++)if(rules.points[i]>rules.points[review.focusSlot])review.focusSlot=i;EndMatch();return;}
   for(int i=0;i<setup.count;i++)
   {
    var t=trucks[i];t.tractorMotor.gripScale=atmosphere.Grip;bool ai=setup.players[i].source==BowlingSource.AI;t.TickDrive(ai?Bot(i,dt):commands[i]);if(ai&&bankHold[i])Hold(t);if(actions[i])scoops[i].Deploy(!scoops[i].Extended);var cargo=trucks[i].transform.TransformPoint(new Vector3(0,2.8f,-2.8f));bool inBin=Vector3.ProjectOnPlane(cargo-banks[i].position,Vector3.up).magnitude<8&&t.body.linearVelocity.magnitude<4;settle[i]=inBin?settle[i]+dt:0;if(settle[i]>=.3f&&rules.carried[i]>0)Deposit(i);
    stalled[i]=Vector3.Dot(t.transform.up,Vector3.up)<.3f&&t.body.linearVelocity.magnitude<1?stalled[i]+dt:0;if(stalled[i]>4){Scatter(i,32,4);t.Prepare(banks[i].position-banks[i].forward*12+Vector3.up*.65f,Quaternion.LookRotation(-banks[i].forward));stalled[i]=0;drivers[i].Reset();}
   }
   for(int i=0;i<balls.Length;i++)if(rules.state[i]==HaulerBallState.Carried)
   {
    int owner=rules.owners[i],order=cargoSlot[i];var target=trucks[owner].transform.TransformPoint(new Vector3((order%2-.5f)*2.1f,2.35f+balls[i].shape.radius,-5.8f+(order/2)*2.1f));float t=Mathf.Clamp01((rules.Clock-claimedAt[i])/.35f);var p=Vector3.Lerp(claims[i],target,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*2);balls[i].body.MovePosition(p);balls[i].body.MoveRotation(trucks[owner].body.rotation);
   }
  }
  Vector2 Bot(int slot,float dt)
  {
   var t=trucks[slot];int count=rules.carried[slot],threshold=(int)setup.players[slot].difficulty==0?3:rules.Capacity;bool bank=count>=threshold||count>0&&rules.Duration-rules.Clock<25;bankHold[slot]=false;if(count==0)bankEntering[slot]=false;Vector3 goal;
   if(!bank){int choice=targets[slot];if(choice<0||rules.state[choice]!=HaulerBallState.Loose){float best=float.PositiveInfinity;choice=-1;for(int i=0;i<balls.Length;i++)if(balls[i].value>0&&rules.state[i]==HaulerBallState.Loose){float cost=Vector3.Distance(t.body.position,balls[i].body.position)-balls[i].value*3;if(cost<best){best=cost;choice=i;}}targets[slot]=choice;}if(choice<0)bank=true;goal=choice>=0?balls[choice].body.position:banks[slot].position;}
   else goal=banks[slot].position;
   scoops[slot].Deploy(!bank);if(bank){var approach=banks[slot].position-banks[slot].forward*22;if(!bankEntering[slot]&&Vector3.ProjectOnPlane(t.body.position-approach,Vector3.up).magnitude<6)bankEntering[slot]=true;goal=bankEntering[slot]?banks[slot].position-banks[slot].forward*4:approach;var rear=t.transform.TransformPoint(new Vector3(0,2.8f,-2.8f));if(bankEntering[slot]&&Vector3.ProjectOnPlane(rear-banks[slot].position,Vector3.up).magnitude<7.5f){bankHold[slot]=true;return Vector2.zero;}}
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
   if(!Live||Countdown>0||!scoops[slot].Ready)return false;var take=rules.Collect(ball.id,slot);if(take==HaulerTake.Ignored)return false;
   if(take==HaulerTake.Explosive){ActualBombs++;ball.shape.enabled=false;ball.gameObject.SetActive(false);Scatter(slot,32,8);trucks[slot].tractorMotor.AddAssemblyVelocity(Vector3.up*6-trucks[slot].transform.forward*4);blasts[slot].transform.position=ball.body.position;blasts[slot].Play();feedback.PlayAt(3,trucks[slot].body.position,10,true);announcement=setup.players[slot].name+" SCOOPED A BOMB · CARGO SPILLED!";announcementUntil=elapsed+2;return true;}
   claims[ball.id]=ball.body.position;claimedAt[ball.id]=rules.Clock;int free=0;for(;free<rules.Capacity;free++){bool used=false;for(int i=0;i<balls.Length;i++)if(i!=ball.id&&rules.state[i]==HaulerBallState.Carried&&rules.owners[i]==slot&&cargoSlot[i]==free)used=true;if(!used)break;}cargoSlot[ball.id]=free;ball.body.linearVelocity=ball.body.angularVelocity=Vector3.zero;ball.body.isKinematic=true;ball.shape.enabled=false;ActualCollections++;feedback.PlayAt(0,ball.body.position,0,false);return true;
  }
  void Deposit(int slot)
  {
   int before=rules.deposits[slot],points=rules.points[slot];int count=rules.Deposit(slot);if(count==0)return;int order=before;
   for(int i=0;i<balls.Length;i++)if(rules.state[i]==HaulerBallState.Deposited&&rules.owners[i]==slot&&cargoSlot[i]>=0){var p=banks[slot].TransformPoint(new Vector3((order%8-3.5f)*1.35f,.9f+(order/64)*1.5f,(order/8%8-3.5f)*1.35f));balls[i].body.isKinematic=true;balls[i].shape.enabled=false;unloads[i]?.Kill();unloads[i]=balls[i].body.DOMove(p,.4f).SetEase(Ease.OutCubic).SetUpdate(UpdateType.Fixed);cargoSlot[i]=-1;order++;}ActualDeposits+=count;announcement=setup.players[slot].name+" BANKED +"+(rules.points[slot]-points)+"!";announcementUntil=elapsed+2;feedback.PlayAt(5,banks[slot].position,0,true);Pulse(slot,.5f,.15f);
  }
  void Scatter(int slot,int max,float strength)
  {
   int count=rules.Drop(slot,max,release);for(int i=0;i<count;i++){int id=release[i];var b=balls[id];var offset=trucks[slot].transform.TransformDirection(new Vector3((i%3-1)*1.6f,3+i/3*.2f,-4));var p=trucks[slot].body.position+offset;b.body.isKinematic=false;b.body.position=p;b.transform.position=p;b.shape.enabled=true;b.body.linearVelocity=trucks[slot].body.linearVelocity+Vector3.ClampMagnitude(offset.normalized*strength+Vector3.up*4,14);b.body.angularVelocity=Vector3.one*(i%2==0?4:-4);cargoSlot[id]=-1;}
  }
  public void Ram(int a,int b,float speed){if(!Live||!rules.Live||Countdown>0||elapsed<impactAt[a]||elapsed<impactAt[b])return;impactAt[a]=impactAt[b]=elapsed+.7f;Scatter(a,(rules.carried[a]+1)/2,5);Scatter(b,(rules.carried[b]+1)/2,5);ActualRams++;feedback.PlayAt(1,(trucks[a].body.position+trucks[b].body.position)*.5f,speed);}
  protected override void Present(float dt){if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;var bounds=new Bounds(new Vector3(0,5,0),new Vector3(180,18,180));broadcast.view.orthographic=true;broadcast.Pose(bounds.center,new Vector3(0,135,-110),60);broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-135,110),Vector3.up);framing.arenaBounds=bounds;framing.Apply();}
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);protected override void StopSounds(){if(trucks!=null)foreach(var t in trucks)t.StopSound();}
  protected override void OnDestroy(){if(unloads!=null)foreach(var tween in unloads)tween?.Kill();base.OnDestroy();}
  protected override void StopGame(){if(unloads!=null)foreach(var tween in unloads)tween?.Kill();review?.End(false);StopSounds();if(trucks!=null)foreach(var t in trucks){t.Power(false);t.body.isKinematic=true;}if(balls!=null)foreach(var b in balls){if(!b.body.isKinematic)b.body.linearVelocity=b.body.angularVelocity=Vector3.zero;b.body.isKinematic=true;}if(scoops!=null)foreach(var scoop in scoops)scoop.Deploy(scoop.Extended,true);}
 }
}
