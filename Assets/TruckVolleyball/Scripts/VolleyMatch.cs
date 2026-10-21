using System;using System.Text;using UnityEngine;
using LWS.TruckSports;using LWS.TruckFareFight;using LWS.TruckBowling;
namespace LWS.TruckVolleyball
{
 public sealed class VolleyMatch:SportsEventSession
 {
  public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){action.Objective(ball.position,ballRadius);action.primary=framing.Nearest(ball.position);action.reason="BALL / ACTIVE RALLY";}
  public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;public Rigidbody ball;public Transform net;public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;public LWS.TruckFieldGoal.FieldGoalCrowd crowd;
  public VolleyRules rules=new VolleyRules();public float Countdown{get;private set;}public int ActualContacts{get;private set;}public int ActualJumps{get;private set;}public int NetContacts{get;private set;}
  public float ballRadius=3,jumpHeight=8,jumpCooldown=1.8f,halfWidth=32,halfLength=50,netHeight=7;
  public override string Title=>"TRUCK VOLLEYBALL";public override bool TeamSport=>true;public override int MinimumPlayers=>2;public override int MaximumPlayers=>8;public override int WeatherCount=>1;public override int OptionCount=>3;public override bool SupportsSplitAIViews=>true;public override int CompactScoreColumns=>8;
  public override string OptionLabel=>new[]{"FIRST TO 7","FIRST TO 15","FIRST TO 21"}[option];public override bool SupportsRaceCountdown=>true;public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;public override float RaceStartSeconds=>Countdown<=0?elapsed:-1;
  public override bool ReplayControls=>review!=null&&review.Playing;public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;public override bool BroadcastStandingsVisible=>!ReplayControls;
  public override string Header=>Title+" · BLUE "+rules.points[0]+" : "+rules.points[1]+" ORANGE · TO "+rules.Target+" WIN BY TWO";
  public override string Status=>Countdown>0?(rules.Server==0?"BLUE":"ORANGE")+" SERVICE · PREPARE":rules.Call+" · "+(rules.LastTeam==0?"BLUE":"ORANGE")+" TOUCHES "+rules.Touches+" / 3";
  public override string Help=>"Two to four trucks per team on a real beach volleyball court; one-on-one is also supported. Drive and ACTION to suspension-jump. Your actual bumper/body contact determines the return: approach facing over the net, hit under the ball for a loft, time an aerial strike for a stronger spike. You cannot directly steer or teleport the ball. Three touches per team; a fourth or consecutive double touch loses the rally. Crossing the net resets the touch count. A ball touching your court gives the other team a point; the last touching team loses an out-of-bounds shot. The net and posts are physical. The winning team serves the next rally after a grounded countdown. Default15points, win by two, cap21;7/21options. AI predicts actual flight and assigns a primary receiver while teammates cover. Selected weather and wind affect the shared physical ball. Shared/chase/full-cab, free look, horn, Your Crew and optional AI cameras use standard collection controls. Results include contact/spike/save statistics and recorded highlights. Rematch restores all participants and score.";
  public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace(" ACTION"," JUMP");
  public int Team(int slot)=>slot<teamA?0:1;
  public override bool TryReadScoreCard(int slot,out string value,out string detail){int t=Team(slot);value=rules.points[t]+"P";detail=(t==0?"BLUE":"ORANGE")+" · "+hits[slot]+" HITS";return slot<setup.count;}
  public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var score,out var detail);return setup.players[slot].name+" · "+score+"\n"+detail+"\n"+ControlCaption(slot);}
  public override string Standings=>"BLUE "+rules.points[0]+" · "+rules.spikes[0]+" AERIAL HITS · "+rules.saves[0]+" SAVES · "+rules.teamPlays[0]+" THREE-TOUCH RETURNS\nORANGE "+rules.points[1]+" · "+rules.spikes[1]+" AERIAL HITS · "+rules.saves[1]+" SAVES · "+rules.teamPlays[1]+" THREE-TOUCH RETURNS";
  public override string Results=>(rules.Winner<0?"VOLLEYBALL RESULTS":(rules.Winner==0?"BLUE":"ORANGE")+" WINS!")+"\n\n"+Standings;
  readonly FareFightRoute[] drivers=new FareFightRoute[8];readonly float[] jumpAt=new float[8],stalled=new float[8];readonly int[] hits=new int[8];float elapsed,after;Vector3 previous;bool pointPending;
  public override bool ValidateSources(out string reason){if(!base.ValidateSources(out reason))return false;int humans=0;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)humans++;if(humans<=4)return true;reason="Up to four local humans; remaining positions use AI.";return false;}
  protected override void BeginGame()
  {
   if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchvolley";review.extras=new[]{ball.transform};}
   review.ResetRecord();rules.Reset(new[]{7,15,21}[option],new[]{13,21,29}[option]);elapsed=ActualContacts=ActualJumps=NetContacts=0;Array.Clear(hits,0,8);ResetServe();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
  }
  static void Hold(SportsEventTruck t){var input=t.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
  void ResetServe()
  {
   Countdown=3;after=0;pointPending=false;ball.interpolation=RigidbodyInterpolation.None;ball.isKinematic=false;ball.linearVelocity=ball.angularVelocity=Vector3.zero;ball.position=new Vector3(0,ballRadius+.15f,rules.Server==0?-39:39);ball.rotation=Quaternion.identity;ball.transform.SetPositionAndRotation(ball.position,ball.rotation);ball.isKinematic=true;ball.interpolation=RigidbodyInterpolation.Interpolate;previous=ball.position;
   for(int i=0;i<trucks.Length;i++){trucks[i].gameObject.SetActive(i<setup.count);if(i>=setup.count)continue;int team=Team(i),local=team==0?i:i-teamA,count=team==0?teamA:teamB;float x=(local-(count-1)*.5f)*13;var p=new Vector3(x,.65f,team==0?-30:30);fragments[i].ResetBody();trucks[i].tractorMotor.Configure();trucks[i].Prepare(p,Quaternion.Euler(0,team==0?0:180,0));trucks[i].tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;trucks[i].tractorMotor.vehicle.powertrain.engine.StartEngine();Hold(trucks[i]);drivers[i]??=new FareFightRoute();drivers[i].Reset();jumpAt[i]=stalled[i]=0;}Physics.SyncTransforms();
  }
  protected override void Simulate(float dt)
  {
   elapsed+=dt;atmosphere.Tick(elapsed);if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(Countdown==0){rules.Serve();ball.isKinematic=false;ball.linearVelocity=new Vector3((rules.Rally%3-1)*2,13,rules.Server==0?13:-13);previous=ball.position;}return;}
   review.Capture(elapsed);if(!rules.Live){if(!pointPending){pointPending=true;feedback.PlayAt(5,ball.position,0,true);crowd?.React(true,rules.Call,ball.position);}after+=dt;for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(after>=2.5f){if(rules.Complete){review.focusSlot=rules.Winner==0?0:teamA;EndMatch();}else ResetServe();}return;}
   // Wind/float forces stay physical and bounded; no target-following flight.
   ball.AddForce(Vector3.up*(ball.mass*Physics.gravity.magnitude*.45f)+Vector3.ClampMagnitude((atmosphere.Flow-ball.linearVelocity)*5,800),ForceMode.Force);
   if(previous.z*ball.position.z<0&&ball.position.y-ballRadius>=netHeight-.1f)rules.Cross(previous.z<0?0:1,ball.position.z<0?0:1);previous=ball.position;
   if(Mathf.Abs(ball.position.x)>halfWidth+ballRadius||Mathf.Abs(ball.position.z)>halfLength+ballRadius||ball.position.y< -5)rules.Out();
   for(int i=0;i<setup.count;i++){var truck=trucks[i];truck.tractorMotor.gripScale=atmosphere.Grip;truck.TickDrive(setup.players[i].source==BowlingSource.AI?Bot(i,dt):commands[i]);if(actions[i])Jump(i);stalled[i]=Vector3.Dot(truck.transform.up,Vector3.up)<.3f&&truck.body.linearVelocity.magnitude<1?stalled[i]+dt:0;if(stalled[i]>4){float z=Team(i)==0?-40:40;truck.Prepare(new Vector3((i%3-1)*13,.65f,z),Quaternion.Euler(0,Team(i)==0?0:180,0));stalled[i]=0;}}
  }
  Vector2 Bot(int slot,float dt)
  {
   int team=Team(slot),start=team==0?0:teamA,count=team==0?teamA:teamB,local=slot-start;var truck=trucks[slot];float gravity=Physics.gravity.magnitude*.55f;float time=(ball.linearVelocity.y+Mathf.Sqrt(ball.linearVelocity.y*ball.linearVelocity.y+2*gravity*Mathf.Max(0,ball.position.y-6)))/gravity;time=Mathf.Clamp(time,0,3);var prediction=ball.position+ball.linearVelocity*time;prediction.y=truck.body.position.y;prediction.x=Mathf.Clamp(prediction.x,-halfWidth+5,halfWidth-5);prediction.z=team==0?Mathf.Clamp(prediction.z,-halfLength+5,-8):Mathf.Clamp(prediction.z,8,halfLength-5);
   int receiver=start;float nearest=float.PositiveInfinity;for(int i=start;i<start+count;i++){float d=Vector3.Distance(trucks[i].body.position,prediction);if(d<nearest){nearest=d;receiver=i;}}
   var goal=receiver==slot?prediction:new Vector3((local-(count-1)*.5f)*14,truck.body.position.y,team==0?-34:34);
   if(receiver==slot&&Vector3.Distance(Vector3.ProjectOnPlane(ball.position-truck.body.position,Vector3.up),Vector3.zero)<10&&ball.position.y<12&&ball.linearVelocity.y<2)Jump(slot);
   // Face the other court at contact instead of ramming away from the net.
   float side=team==0?1:-1;if(receiver==slot&&Mathf.Abs(ball.position.z-truck.body.position.z)<10)goal=ball.position-new Vector3(0,0,side*4);goal.y=truck.body.position.y;
   drivers[slot].DirectGoal(truck.body.position,goal);return drivers[slot].Drive(truck.body,truck.transform,(int)setup.players[slot].difficulty,dt,atmosphere.Grip,ball);
  }
  public bool Jump(int slot){var t=trucks[slot];if(!Live||!rules.Live||Countdown>0||elapsed<jumpAt[slot]||!t.tractorMotor.Grounded||Vector3.Dot(t.transform.up,Vector3.up)<.5f)return false;t.tractorMotor.AddAssemblyVelocity(Vector3.up*Mathf.Clamp(Mathf.Sqrt(2*Physics.gravity.magnitude*jumpHeight)-t.body.linearVelocity.y,0,16));jumpAt[slot]=elapsed+jumpCooldown;ActualJumps++;feedback.PlayAt(2,t.body.position,0,false);return true;}
  public void Strike(SportsEventTruck truck,Collision contact)
  {
   if(!Live||Countdown>0||!rules.Live||contact.relativeVelocity.magnitude<.5f)return;int team=Team(truck.slot);bool air=!truck.tractorMotor.Grounded,save=ball.position.y<5.5f;if(!rules.Touch(team,truck.slot,elapsed,air,save))return;ActualContacts++;hits[truck.slot]++;
   var point=contact.GetContact(0).point;var outward=(ball.worldCenterOfMass-point).normalized;float incoming=Mathf.Clamp(contact.relativeVelocity.magnitude,1,22);
   // Contact normal and actual point velocity determine direction. No team-directed homing.
   var strike=outward*(incoming*(air?.55f:.35f))+Vector3.ProjectOnPlane(truck.body.GetPointVelocity(point),Vector3.up)*.25f;strike.y+=air?3:8;ball.AddForce(Vector3.ClampMagnitude(strike,22),ForceMode.VelocityChange);feedback.PlayAt(1,point,incoming);Pulse(truck.slot,.4f,.12f);
  }
  public void MarkNet(){NetContacts++;}
  public void Landing(){if(!Live||!rules.Live||Countdown>0)return;if(Mathf.Abs(ball.position.x)<=halfWidth&&Mathf.Abs(ball.position.z)<=halfLength)rules.Land(ball.position.z<0?0:1);else rules.Out();}
  protected override void Present(float dt){if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;var bounds=new Bounds(new Vector3(0,5,0),new Vector3(50,12,70));bounds.Encapsulate(ball.position);for(int i=0;i<setup.count;i++)bounds.Encapsulate(trucks[i].body.position+Vector3.up*2);bounds.Expand(new Vector3(10,8,10));if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true;broadcast.Pose(bounds.center,new Vector3(0,100,-85),60);if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-100,85),Vector3.up);framing.arenaBounds=bounds;framing.Apply();}
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
  protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)truck.StopSound();}
  protected override void StopGame(){review?.End(false);StopSounds();if(ball!=null){if(!ball.isKinematic)ball.linearVelocity=ball.angularVelocity=Vector3.zero;ball.isKinematic=true;}if(trucks!=null)foreach(var truck in trucks){truck.Power(false);truck.body.isKinematic=true;}}
 }
}
