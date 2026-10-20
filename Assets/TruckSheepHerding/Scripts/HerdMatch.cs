using System;using System.Text;using UnityEngine;using LWS.TruckSports;using LWS.TruckFareFight;using LWS.TruckBowling;
namespace LWS.TruckSheepHerding
{
 public sealed class HerdMatch:SportsEventSession
 {
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public HerdSheep[] sheep;public HerdPen[] pens;public Rigidbody[] looseFences;public ParticleSystem[] blasts;public Transform flock;
  public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;public HerdRules rules=new HerdRules();
  public float Countdown{get;private set;}public int ActualSecured{get;private set;}public int ActualExplosions{get;private set;}public int HornReactions{get;private set;}public int TruckThreats{get;private set;}
  public override string Title=>"TRUCK SHEEP HERDING";public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;public override int OptionCount=>3;public override int WeatherCount=>1;public override int CompactScoreColumns=>8;public override bool SupportsSplitAIViews=>true;public override bool UsesAction=>false;
  public override string OptionLabel=>new[]{"TWO MINUTES","THREE MINUTES","FIVE MINUTES"}[option];public override bool SupportsRaceCountdown=>true;public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;public override bool ReplayControls=>review!=null&&review.Playing;public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;public override bool BroadcastStandingsVisible=>!ReplayControls;
  public override string Header=>Title+" · "+Mathf.Max(0,rules.Duration-rules.Clock).ToString("F0")+"s · "+rules.Remaining+" SHEEP TO HERD";
  public override string Status=>Countdown>0?"GENTLE DRIVING · YOUR COLOR PEN · GET READY":rules.Clock<announcementUntil?announcement:"GUIDE THE FLOCK · POSITION BEHIND THEM · HORN ADDS PRESSURE · AVOID WARNING SHEEP";
  public override string Help=>"Four farm trucks, a wandering animated flock, and four open timber pens. Guide ordinary sheep into YOUR colored pen using your truck's position, controlled low-speed movement and the dedicated HORN. Sheep flee approaching trucks, respond to honking and stay with nearby flock mates. Gentle pressure is predictable; fast reckless approaches scatter them. Every sheep must physically enter the pen completely, and scores exactly once. Hitting ordinary sheep awards nothing. Rare warning-marked explosive sheep detonate after genuine truck contact and a short warning, scatter nearby flock mates and knock lightweight field fences loose; they never award herd points. Most sheep after three minutes wins; equal counts use the earliest valid final capture time, with exact ties shared. Two/five-minute options. AI physically drives behind sheep and guides them through the same pen contacts, without teleporting animals or automatic points. Shared/chase/full cab, Your Crew, horn, weather, optional AI cameras, recorded review and rematches use the collection systems.";
  public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.points[slot].ToString();detail=rules.points[slot]>0?"SHEEP · LAST PEN "+rules.lastCompletion[slot].ToString("F1")+"s":"SHEEP · GUIDE TO YOUR PEN";return slot<setup.count;}
  public override string ViewText(int view){int i=drivingViews.rally.owners[view];TryReadScoreCard(i,out var value,out var detail);return setup.players[i].name+" · "+value+"\n"+detail+"\n"+ControlCaption(i);}
  readonly StringBuilder text=new StringBuilder(800);public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++){text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append(" SHEEP");if(rules.points[i]>0)text.Append(" · FINAL CAPTURE ").Append(rules.lastCompletion[i].ToString("F1")).Append('s');text.Append('\n');}return text.ToString();}}public override string Results=>"FARM HERDING FINAL\n\n"+Standings;
  Vector3[] spawns,fencePositions,panicDirection,wander;Quaternion[] fenceRotations;float[] panicUntil,explodeAt;readonly bool[] horns=new bool[4];readonly float[] stuck=new float[4],nextHorn=new float[4];readonly int[] target={-1,-1,-1,-1};readonly FareFightRoute[] drivers=new FareFightRoute[4];SportsUniversalHorn horn;float announcementUntil;string announcement="";
  protected override void Awake(){base.Awake();spawns=Array.ConvertAll(sheep,s=>s.body.position);fencePositions=Array.ConvertAll(looseFences,b=>b.position);fenceRotations=Array.ConvertAll(looseFences,b=>b.rotation);panicUntil=new float[sheep.Length];panicDirection=new Vector3[sheep.Length];wander=new Vector3[sheep.Length];explodeAt=new float[sheep.Length];}
  protected override void BeginGame()
  {
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchherd";review.extras=new[]{flock};}
   review.ResetRecord();rules.Reset(setup.count,Array.ConvertAll(sheep,s=>s.explosive),new[]{120f,180f,300f}[option]);Countdown=3;ActualSecured=ActualExplosions=HornReactions=TruckThreats=0;announcementUntil=0;horn=GetComponent<SportsUniversalHorn>();
   for(int i=0;i<4;i++){pens[i].Colorize(i<setup.count?setup.players[i].color:Color.gray);trucks[i].gameObject.SetActive(i<setup.count);horns[i]=false;target[i]=-1;stuck[i]=nextHorn[i]=0;if(i>=setup.count)continue;fragments[i].ResetBody();var inward=-Vector3.ProjectOnPlane(pens[i].transform.position,Vector3.up).normalized;trucks[i].tractorMotor.Configure();trucks[i].Prepare(pens[i].transform.position+inward*23+Vector3.up*.65f,Quaternion.LookRotation(inward));var damage=trucks[i].GetComponent<NWH.VehiclePhysics2.Damage.DamageHandler>();if(damage!=null)damage.Repair();else{trucks[i].tractorMotor.vehicle.powertrain.Repair();foreach(var wheel in trucks[i].wheels)wheel.Damage=0;}trucks[i].tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;trucks[i].tractorMotor.vehicle.powertrain.engine.StartEngine();drivers[i]??=new FareFightRoute();drivers[i].Reset();Hold(trucks[i]);}
   for(int i=0;i<sheep.Length;i++){explodeAt[i]=float.PositiveInfinity;panicUntil[i]=0;wander[i]=spawns[i];sheep[i].ResetSheep(spawns[i],Quaternion.Euler(0,(i*137)%360,0));}
   for(int i=0;i<looseFences.Length;i++){var b=looseFences[i];b.isKinematic=false;b.interpolation=RigidbodyInterpolation.None;b.position=fencePositions[i];b.rotation=fenceRotations[i];b.transform.SetPositionAndRotation(b.position,b.rotation);b.linearVelocity=b.angularVelocity=Vector3.zero;b.isKinematic=true;b.interpolation=RigidbodyInterpolation.Interpolate;}
   foreach(var fx in blasts)fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);Physics.SyncTransforms();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
  }
  static void Hold(SportsEventTruck t){var input=t.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;input.states.horn=false;}
  protected override void Simulate(float dt)
  {
   atmosphere.Tick(rules.Clock);if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(Countdown==0)rules.Begin();return;}rules.Tick(dt);review.Capture(rules.Clock+3);
   if(rules.Complete){review.focusSlot=0;for(int i=1;i<setup.count;i++)if(rules.Place(i)<rules.Place(review.focusSlot))review.focusSlot=i;EndMatch();return;}
   for(int i=0;i<setup.count;i++)
   {
    var truck=trucks[i];bool ai=setup.players[i].source==BowlingSource.AI;truck.tractorMotor.gripScale=atmosphere.Grip;truck.TickDrive(ai?Bot(i,dt):commands[i]);
    if(!ai&&horn!=null){var p=setup.players[i];horns[i]=p.released&&SportsUniversalHorn.Held(new SportsUniversalHorn.Driver{source=p.source,pad=p.pad},horn.HornKey(i));}truck.tractorMotor.vehicle.input.states.horn=ai&&horns[i];
    stuck[i]=Vector3.Dot(truck.transform.up,Vector3.up)<.3f&&truck.body.linearVelocity.magnitude<1?stuck[i]+dt:0;if(stuck[i]>4){var inward=-pens[i].transform.forward;var point=pens[i].transform.position+inward*23+Vector3.up*.65f;bool clear=true;for(int j=0;j<setup.count;j++)if(j!=i&&(trucks[j].body.position-point).sqrMagnitude<144)clear=false;if(clear){truck.Prepare(point,Quaternion.LookRotation(inward));drivers[i].Reset();stuck[i]=0;}}
   }
   for(int i=0;i<sheep.Length;i++){if(rules.Clock>=explodeAt[i])Explode(i);if(sheep[i].gameObject.activeSelf)sheep[i].Step(dt);}
  }
  public Vector3 ThreatGoal(HerdSheep animal,out float speed)
  {
   int id=animal.id;var position=animal.agent.nextPosition;if(rules.Clock<panicUntil[id]){speed=6;return position+panicDirection[id]*15;}
   Vector3 pressure=Vector3.zero;float severity=0;bool heard=false;
   for(int i=0;i<setup.count;i++)
   {
    var delta=Vector3.ProjectOnPlane(position-trucks[i].body.position,Vector3.up);float distance=delta.magnitude;float range=horns[i]?32:18;if(distance<.1f||distance>range)continue;float approach=Mathf.Max(0,Vector3.Dot(trucks[i].body.linearVelocity,delta/distance));float weight=(1-distance/range)*(1+Mathf.Min(approach,16)*.1f);pressure+=delta/distance*weight;severity+=weight;heard|=horns[i];
   }
   if(severity>.05f){TruckThreats++;if(heard)HornReactions++;speed=Mathf.Lerp(2.6f,5.8f,Mathf.Clamp01(severity));var direction=pressure.normalized;if(severity>1.5f)direction=(direction+Vector3.Cross(Vector3.up,direction)*Mathf.Sin(id*1.7f+rules.Clock*.7f)*.4f).normalized;return position+direction*14;}
   Vector3 group=Vector3.zero,separation=Vector3.zero;int neighbors=0;
   foreach(var other in sheep)if(other!=animal&&other.gameObject.activeSelf&&rules.owners[other.id]<0){var delta=Vector3.ProjectOnPlane(other.body.position-animal.body.position,Vector3.up);float d=delta.magnitude;if(d<9){group+=other.body.position;neighbors++;if(d<2&&d>.01f)separation-=delta.normalized*(2-d);}}
   if(neighbors>0){var center=group/neighbors;speed=1.3f;return position+Vector3.ClampMagnitude(center-position,5)+separation*2;}
   speed=.8f;float angle=id*2.39996f+Mathf.Floor(rules.Clock/7)*1.6f;return spawns[id]+new Vector3(Mathf.Sin(angle)*5,0,Mathf.Cos(angle)*5);
  }
  Vector2 Bot(int slot,float dt)
  {
   var truck=trucks[slot];int chosen=target[slot];if(chosen<0||rules.owners[chosen]>=0||!sheep[chosen].gameObject.activeSelf){chosen=-1;float best=float.PositiveInfinity;foreach(var s in sheep)if(!s.explosive&&rules.owners[s.id]<0&&s.gameObject.activeSelf){float cost=Vector3.Distance(truck.body.position,s.body.position)+Vector3.Distance(s.body.position,pens[slot].transform.position)*.25f;for(int j=0;j<setup.count;j++)if(j!=slot&&target[j]==s.id)cost+=65;if(cost<best){best=cost;chosen=s.id;}}target[slot]=chosen;}
   horns[slot]=false;if(chosen<0){Hold(truck);return Vector2.zero;}var animal=sheep[chosen];var outward=Vector3.ProjectOnPlane(animal.body.position-pens[slot].transform.position,Vector3.up).normalized;var behind=animal.body.position+outward*11;var toTruck=Vector3.ProjectOnPlane(truck.body.position-animal.body.position,Vector3.up);bool aligned=Vector3.Dot(toTruck.normalized,outward)>.85f;var goal=aligned?animal.body.position+outward*5:behind;
   drivers[slot].DirectGoal(truck.body.position,goal);var drive=drivers[slot].Drive(truck.body,truck.transform,(int)setup.players[slot].difficulty,dt,atmosphere.Grip,animal.body);if(aligned){float desired=setup.players[slot].difficulty==BowlingDifficulty.Easy?3.5f:5;float actual=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);if(actual>desired)drive.y=-.35f;else if(drive.y>0)drive.y*=.55f;if(toTruck.magnitude<20&&rules.Clock>=nextHorn[slot]){horns[slot]=true;if(rules.Clock>=nextHorn[slot]+.35f)nextHorn[slot]=rules.Clock+5;}}
   return drive;
  }
  public bool Secure(HerdSheep animal,int owner)
  {
   if(!Live||Countdown>0||animal==null||owner<0||owner>=setup.count||!pens[owner].ContainsWhole(animal)||!rules.Secure(animal.id,owner))return false;ActualSecured++;animal.Stop();announcement=setup.players[owner].name+" HERDED +1!";announcementUntil=rules.Clock+1.8f;feedback.PlayAt(5,animal.body.position,0,true);Pulse(owner,.35f,.12f);return true;
  }
  public void QueueExplosion(int id){if(!Live||Countdown>0||id<0||id>=sheep.Length||!sheep[id].explosive||!float.IsPositiveInfinity(explodeAt[id])||rules.detonated[id])return;explodeAt[id]=rules.Clock+.7f;announcement="WARNING SHEEP · CLEAR THE FLOCK!";announcementUntil=rules.Clock+1.5f;feedback.PlayAt(2,sheep[id].body.position,0,true);}
  void Explode(int id)
  {
   explodeAt[id]=float.PositiveInfinity;if(!rules.Detonate(id))return;var point=sheep[id].body.position;sheep[id].Stop();sheep[id].gameObject.SetActive(false);ActualExplosions++;var fx=blasts[ActualExplosions%blasts.Length];fx.transform.position=point;fx.Play();feedback.PlayAt(3,point,12,true);
   for(int i=0;i<setup.count;i++){var delta=trucks[i].body.position-point;float distance=delta.magnitude;if(distance<18)trucks[i].tractorMotor.AddAssemblyVelocity(Vector3.ClampMagnitude((delta.normalized*8+Vector3.up*4)*(1-distance/18),11));}
   foreach(var other in sheep)if(other.gameObject.activeSelf&&rules.owners[other.id]<0){var delta=Vector3.ProjectOnPlane(other.body.position-point,Vector3.up);if(delta.sqrMagnitude<900){panicUntil[other.id]=rules.Clock+3;panicDirection[other.id]=delta.sqrMagnitude>.01f?delta.normalized:Vector3.forward;}}
   foreach(var fence in looseFences)if((fence.position-point).sqrMagnitude<400){fence.isKinematic=false;fence.AddForce(Vector3.ClampMagnitude((fence.position-point).normalized*9+Vector3.up*5,11),ForceMode.VelocityChange);}
  }
  protected override void Present(float dt){if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;var bounds=new Bounds(new Vector3(0,5,0),new Vector3(225,24,225));broadcast.view.orthographic=true;broadcast.Pose(bounds.center,new Vector3(0,155,-135),60);broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-155,135),Vector3.up);framing.arenaBounds=bounds;framing.Apply();}
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}protected override void SetPaused(bool paused){atmosphere?.Pause(paused);if(sheep!=null)foreach(var s in sheep)if(s.agent.enabled&&s.agent.isOnNavMesh)s.agent.isStopped=paused;}
  protected override void StopSounds(){if(trucks!=null)foreach(var t in trucks)t.StopSound();}
  protected override void StopGame(){review?.End(false);StopSounds();if(sheep!=null)foreach(var s in sheep)s.Stop();if(trucks!=null)foreach(var t in trucks){t.Power(false);t.body.isKinematic=true;t.tractorMotor.vehicle.input.states.horn=false;}}
 }
}
