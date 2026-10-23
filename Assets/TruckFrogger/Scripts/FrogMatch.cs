using System;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckFareFight;
namespace LWS.TruckFrogger
{
 public sealed class FrogMatch:SportsEventSession
 {
 public override bool BroadcastParticipantActive(int slot)=>slot==rules.Active;
 public override void DescribeBroadcast(SportsBroadcastAction a,SportsBroadcastFraming f){a.primary=rules.Active;var c=world.courses[world.Current];a.Objective(gate<c.gates.Length?c.gates[gate]:c.finish,1.5f);a.reason="ACTIVE CROSSING / NEXT SAFE GATE";}
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public FrogWorld world;public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;
  public FrogRules rules=new FrogRules();public float attemptLimit=180;public int seed;public int CompletedCrossings{get;private set;}public int ActualImpacts{get;private set;}public int WaitDecisions{get;private set;}
  public override string Title=>"TRUCK FROGGER";public override int MinimumPlayers=>1;public override int MaximumPlayers=>8;public override int OptionCount=>6;public override int WeatherCount=>1;public override int CompactScoreColumns=>8;public override bool SupportsSplitAIViews=>true;
  public override bool AISplitViewsApplicable=>false;
  public override void NormalizeSetup(){base.NormalizeSetup();setup.mode=BowlingMode.HotSeat;setup.sharedHotSeat=true;}
  public override string OptionLabel=>option==0?"FIVE-EVENT CHAMPIONSHIP":world!=null&&world.courses!=null?world.courses[option-1].title.ToUpperInvariant():"SELECT CHALLENGE";
  public override bool SupportsRaceCountdown=>true;public override float RaceCountdownSeconds=>Live&&rules.Phase==FrogPhase.Countdown?3-rules.Clock:-1;public override float RaceStartSeconds=>rules.Phase==FrogPhase.Driving?rules.Clock:-1;
  public override bool ReplayControls=>review!=null&&review.Playing;public override bool BroadcastStandingsVisible=>!ReplayControls;
  public override string Header=>Title+" · "+world.courses[rules.Course].title.ToUpperInvariant()+" · "+setup.players[rules.Active].name;
  public override string Status=>rules.Phase==FrogPhase.Countdown?"ONE LIFE · IDENTICAL SEEDED OBSTACLES · GET READY":rules.Phase==FrogPhase.Driving?rules.Clock.ToString("F2")+"s · "+Mathf.Min(gate+1,world.courses[rules.Course].gates.Length)+" / "+world.courses[rules.Course].gates.Length+" CROSSINGS · HOLD ACTION TO RETIRE":rules.Announcement;
  public override string Help=>"Five independently selectable one-life crossing challenges: busy UTS traffic, freight railway, moving river barges, industrial machinery and the ultimate combination. Championship plays all five. One to eight alternating drivers, up to four local humans, named AI and difficulty. Everyone faces the same seeded hazard positions and weather for that challenge. There are no checkpoint respawns during an attempt. Drive, brake and wait for a genuine gap; meaningful solid crashes, drowning or leaving the course end your attempt. A whole-truck finish after every crossing records actual time. Fastest successful driver earns exactly one point, failures earn none. Ties use successful times then actual additional tiebreak runs; three unresolved retries produce an announced shared draw. AI physically waits, drives and crosses under the same rules. Hold ACTION for two seconds to retire a stuck attempt. Horn, chase, cab, elevated view, free look, pause and reconnect use established controls. All spectators watch the active alternating driver; optional AI cameras never create redundant views. Selected weather remains unchanged between turns. Recorded highlights, results and rematch preserve authoritative times.";
  public override string SourcePolicy=>"Alternating turns · shared keyboard or controller supported.\nOnly the active participant drives; everyone watches that attempt.";
  public override bool TryReadScoreCard(int i,out string value,out string detail){value=rules.points[i].ToString();detail=i==rules.Active?rules.Phase==FrogPhase.Driving?rules.Clock.ToString("F1")+"s":"NEXT / ACTIVE":"WAITING";return i<setup.count;}
  readonly StringBuilder text=new StringBuilder(800);public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++){text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append(" WINS · ");for(int c=0;c<5;c++)if(option==0||c==option-1)text.Append(float.IsFinite(rules.times[c,i])?rules.times[c,i].ToString("F2")+"s":"DNF").Append("  ");text.AppendLine();}return text.ToString();}}public override string Results=>"CROSSING CHAMPIONSHIP\n\n"+Standings+"\n"+rules.Announcement;
  int gate,preparedActor=-1,preparedSeed,preparedCourse;float elapsed,motionAge,retireHeld,impact=float.PositiveInfinity,stepClock;string impactReason;Vector3 previous;Bounds previousBounds;readonly FareFightRoute driver=new FareFightRoute();
  protected override void Awake(){base.Awake();rules.Awarded+=ConfirmAward;}
  protected override void OnDestroy(){if(rules!=null)rules.Awarded-=ConfirmAward;base.OnDestroy();}
  void ConfirmAward(string id,int slot,int delta,int total){ConfirmScore(id,slot,delta,total,"FASTEST SUCCESSFUL CROSSING",SportsScoreTier.RoundWinner,unit:"WINS",roundEnded:true);}
  protected override void BeginGame()
  {
   seed=Environment.TickCount;rules.Reset(setup.count,option,seed,attemptLimit);rules.Begin();CompletedCrossings=ActualImpacts=WaitDecisions=0;elapsed=0;preparedActor=-1;atmosphere.Configure(seed,0,1);
   foreach(var c in world.courses)c.traffic?.Ensure();
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchfrog";review.extras=world.courses.Select(c=>c.actors).ToArray();}review.ResetRecord();PrepareTurn();
  }
  void PrepareTurn()
  {
   preparedActor=rules.Active;preparedCourse=rules.Course;preparedSeed=rules.CourseSeed;world.Prepare(rules.Course,rules.CourseSeed);gate=0;motionAge=0;retireHeld=0;impact=float.PositiveInfinity;driver.Reset();var c=world.courses[rules.Course];
   for(int i=0;i<trucks.Length;i++){fragments[i].ResetBody();trucks[i].gameObject.SetActive(i==rules.Active);if(i!=rules.Active)continue;trucks[i].tractorMotor.Configure();var damage=trucks[i].GetComponent<NWH.VehiclePhysics2.Damage.DamageHandler>();if(damage!=null)damage.Repair();trucks[i].Power(false);trucks[i].Prepare(c.start,Quaternion.identity,false);Physics.SyncTransforms();foreach(var wheel in trucks[i].wheels){wheel.wheel.angularVelocity=0;wheel.spring.prevLength=wheel.spring.length;wheel.WakeFromSleep();}trucks[i].Power(true);Hold(trucks[i]);}
   previous=trucks[rules.Active].body.position;previousBounds=BodyBounds(trucks[rules.Active]);stepClock=0;var groups=new int[setup.count];Array.Fill(groups,rules.Active);drivingViews.rally.presentationGroups=groups;drivingViews.rally.Focus=rules.Active;drivingViews.Layout();Gate();Physics.SyncTransforms();
  }
  public static Bounds BodyBounds(SportsEventTruck t){bool any=false;var bounds=new Bounds(t.body.position,Vector3.zero);foreach(var shape in t.GetComponentsInChildren<Collider>())if(shape.enabled&&!shape.isTrigger&&shape.attachedRigidbody==t.body){if(!any){bounds=shape.bounds;any=true;}else bounds.Encapsulate(shape.bounds);}return bounds;}
  static void Hold(SportsEventTruck t){t.TickDrive(Vector2.zero);var input=t.tractorMotor.vehicle.input;input.states.handbrake=input.states.handbrakeRaw=1;}
  protected override void Simulate(float dt)
  {
   elapsed+=dt;atmosphere.Tick(elapsed);var t=trucks[rules.Active];var c=world.courses[rules.Course];var bounds=BodyBounds(t);
   if(rules.Phase==FrogPhase.Driving)
   {
    while(gate<c.gates.Length){float plane=world.CrossingEnd(gate);if(previousBounds.min.z>=plane||bounds.min.z<plane)break;float at=Mathf.Clamp01((plane-previousBounds.min.z)/Mathf.Max(.001f,bounds.min.z-previousBounds.min.z));float center=Mathf.Lerp(previousBounds.center.x,bounds.center.x,at);if(Mathf.Abs(center-c.start.x)+Mathf.Max(previousBounds.extents.x,bounds.extents.x)>c.halfWidth)break;gate++;CompletedCrossings++;}
    float crossing=float.PositiveInfinity;if(gate==c.gates.Length&&previousBounds.min.z<c.finish.z&&bounds.min.z>=c.finish.z&&bounds.min.x>=c.finish.x-c.halfWidth&&bounds.max.x<=c.finish.x+c.halfWidth)crossing=Mathf.Clamp01((c.finish.z-previousBounds.min.z)/Mathf.Max(.001f,bounds.min.z-previousBounds.min.z));
    if(FrogEventOrder.FinishWins(crossing,impact)){rules.Finish(rules.Active,stepClock+crossing*dt);feedback.PlayAt(6,t.body.position,0,true);}
    else if(float.IsFinite(impact)){rules.Fail(rules.Active,impactReason);ActualImpacts++;feedback.PlayAt(5,t.body.position,0,true);}
    else if(world.Water(t.body.position)||t.body.position.y< -12)rules.Fail(rules.Active,"SUNK · ONE LIFE");
    else if(bounds.min.x<c.start.x-c.halfWidth||bounds.max.x>c.start.x+c.halfWidth||t.body.position.z<c.start.z-35)rules.Fail(rules.Active,"LEFT THE MARKED COURSE");
   }
   impact=float.PositiveInfinity;var before=rules.Phase;rules.Tick(dt);if(rules.Complete){EndMatch();return;}if(preparedActor!=rules.Active||preparedCourse!=rules.Course||preparedSeed!=rules.CourseSeed||before==FrogPhase.TurnResult&&rules.Phase==FrogPhase.Countdown){PrepareTurn();t=trucks[rules.Active];c=world.courses[rules.Course];}
   if(rules.Phase==FrogPhase.Driving)motionAge=rules.Clock;world.Step(motionAge,rules.Phase==FrogPhase.Driving,atmosphere.Grip);review.Capture(elapsed);t.tractorMotor.gripScale=atmosphere.Grip;
   if(rules.Phase==FrogPhase.Driving){bool ai=setup.players[rules.Active].source==BowlingSource.AI;var command=ai?Bot(t,c,dt):commands[rules.Active];t.TickDrive(command);if(ai&&waiting)Hold(t);if(!ai){retireHeld=setup.players[rules.Active].released&&ActionHeld(rules.Active)?retireHeld+dt:0;if(retireHeld>=2)rules.Fail(rules.Active,"DRIVER RETIRED · ONE LIFE");}}
   else Hold(t);previous=t.body.position;previousBounds=BodyBounds(t);stepClock=rules.Clock;
  }
  public string Diagnostics(){var t=trucks[rules.Active];var v=t.tractorMotor.vehicle;var b=new StringBuilder("command="+t.command+" engine="+v.powertrain.engine.IsRunning+" engineDamage="+v.powertrain.engine.Damage+" transmissionDamage="+v.powertrain.transmission.Damage+" body="+BodyBounds(t));foreach(var w in t.wheels)b.Append(" wheel[").Append(w.name).Append(" grounded=").Append(w.IsGrounded).Append(" hit=").Append(w.HitCollider==null?"null":w.HitCollider.name).Append(" torque=").Append(w.MotorTorque).Append(" damage=").Append(w.Damage).Append(']');return b.ToString();}
  bool waiting;
  Vector2 Bot(SportsEventTruck t,FrogWorld.Course c,float dt)
  {
   waiting=false;var target=gate<c.gates.Length?new Vector3(c.start.x,0,world.CrossingEnd(gate)+12):c.finish+Vector3.forward*12;float distance=gate<c.gates.Length?c.gates[gate].z-t.body.position.z:1000;
   if(distance>15&&distance<48&&!world.SafeToCross(gate,rules.Clock,3.5f+(int)setup.players[rules.Active].difficulty*1.25f)){waiting=true;WaitDecisions++;return Vector2.zero;}
   driver.DirectGoal(t.body.position,target);return driver.Drive(t.body,t.transform,(int)setup.players[rules.Active].difficulty,dt,atmosphere.Grip);
  }
  public void SolidContact(int slot,Collision collision)
  {
   if(!Live||rules.Phase!=FrogPhase.Driving||slot!=rules.Active||collision.collider.isTrigger||collision.collider.attachedRigidbody==trucks[slot].body)return;float speed=0;Vector3 contact=trucks[slot].body.position;for(int i=0;i<collision.contactCount;i++){var p=collision.GetContact(i);if(Mathf.Abs(p.normal.y)>.65f)continue;float normal=Mathf.Abs(Vector3.Dot(collision.relativeVelocity,p.normal));if(normal>speed){speed=normal;contact=p.point;}}
   if(speed<6)return;var delta=trucks[slot].body.position-previous;float fraction=delta.sqrMagnitude<.001f?1:Mathf.Clamp01(Vector3.Dot(contact-previous,delta)/delta.sqrMagnitude);if(fraction<impact){impact=fraction;Debug.Log("FROG_CONTACT time="+rules.Clock+" position="+trucks[slot].body.position+" object="+collision.collider.name+" bounds="+collision.collider.bounds+" relative="+collision.relativeVelocity+" normalSpeed="+speed);impactReason="SOLID IMPACT · "+collision.collider.name+" · "+(speed*2.23694f).ToString("F0")+" MPH NORMAL";}
  }
  protected override void Present(float dt){if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(drivingViews.NeedsArena){var t=trucks[rules.Active];broadcast.Pose(t.body.position+Vector3.forward*15,new Vector3(25,35,-38),68);}}
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
  protected override void SetPaused(bool pause){atmosphere?.Pause(pause);}protected override void StopSounds(){if(trucks!=null)foreach(var t in trucks)t.StopSound();}
  protected override void StopGame(){review?.End(false);StopSounds();if(trucks!=null)foreach(var t in trucks){t.Power(false);if(!t.body.isKinematic)t.body.linearVelocity=t.body.angularVelocity=Vector3.zero;t.body.isKinematic=true;}}
 }
}
