using System;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckFareFight;
using LWS.TruckBowling;
namespace LWS.TruckTetherball
{
    public sealed class TetherMatch : SportsEventSession
    {
 public override void DescribeBroadcast(SportsBroadcastAction a,SportsBroadcastFraming f){a.primary=f.Nearest(ball.position,true);if(a.primary<0)a.primary=f.Nearest(ball.position);a.Objective(ball.position,1.5f);a.reason="TETHERED BALL / CURRENT CONTEST";}
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public Rigidbody ball;
        public TetherCable cable;
        public Transform ropeGroup;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public SportsRecordedReview review;
        public TetherRules rules=new TetherRules();
        [Range(1,4)]public int requiredTurns=2;
        public float Countdown {get;private set;}
        public int ActualBallContacts {get;private set;}
        public override string Title=>"TRUCK TETHERBALL";
        public override int MinimumPlayers=>2;
        public override int MaximumPlayers=>2;
        public override int OptionCount=>2;
        public override int WeatherCount=>1;
        public override int CompactScoreColumns=>8;
        public override bool SupportsSplitAIViews=>true;
        public override bool SupportsRaceCountdown=>true;
        public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;
        public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;
        public override string OptionLabel=>option==0?"BEST OF THREE":"BEST OF FIVE";
        public override bool ReplayControls=>review!=null&&review.Playing;
        public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible=>!ReplayControls;
        public override float ParticipantProgress(int slot)=>rules.Progress(slot);
        public override string Header=>Title+(ReplayControls?" · RECORDED REPLAY":" · ROUND "+(rules.Round+1)+" · WIND "+requiredTurns+" TURNS");
        public override string Status=>Countdown>0?"LINE UP YOUR BUMPER":rules.RoundWinner>=0?setup.players[rules.RoundWinner].name+" WINDS THE ROPE!":setup.players[0].name+" COUNTERCLOCKWISE  ·  CLOCKWISE "+setup.players[1].name;
        public override string Help=>"Two trucks. One giant tetherball. Strike the ball with your actual bumper to wind the cable around the pole in your assigned direction. Player 1 winds counterclockwise; Player 2 winds clockwise. The nearer starting approach alternates each round. Reversing the ball unwinds your opponent's progress. Actual ball trajectory decides every turn: repeated touching a stationary ball earns nothing. Complete the required wraps to win a round. Choose best of three or five. Power, timing and a tangential approach matter; the ball can swing back into you. RECOVER only works when stuck or overturned. Shared/chase/full cab, free look, horn, Your Crew, selected weather and optional AI cameras use the collection controls. Weather lasts for the whole match. Results offer a skippable recorded review; rematch restores trucks, ball, rope and round wins.";
        public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace(" ACTION"," RECOVER");
        readonly FareFightRoute[] drivers=new FareFightRoute[2];
        readonly float[] stopped=new float[2],contactCue=new float[2],planAt=new float[2];
        readonly Vector3[] goals=new Vector3[2];readonly StringBuilder text=new StringBuilder(300);
        float elapsed,aftermath;
        public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.wins[slot]+" / "+(rules.BestOf/2+1);detail=(slot==0?"CCW · ":"CW · ")+(rules.Progress(slot)*100).ToString("F0")+"% WOUND";return slot<2;}
        public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var score,out var detail);return setup.players[slot].name+" · "+score+"\n"+detail+"\n"+ControlCaption(slot);}
        public int ResultPlace(int slot)=>rules.wins[1-slot]>rules.wins[slot]?2:1;
        public override string Standings{get{text.Clear();for(int i=0;i<2;i++)text.Append('#').Append(ResultPlace(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.wins[i]).Append(" ROUND WINS · ").Append(i==0?"COUNTERCLOCKWISE":"CLOCKWISE").Append('\n');return text.ToString();}}
        public override string Results=>(rules.Winner>=0?setup.players[rules.Winner].name+" WINS TETHERBALL!":"TETHERBALL RESULTS")+"\n\n"+Standings;
        protected override void BeginGame()
        {
            if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchtether";review.extras=new[]{ball.transform,ropeGroup};}
            review.ResetRecord();rules.Reset(option==0?3:5,requiredTurns);elapsed=0;ActualBallContacts=0;ResetRound();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
        }
        void ResetRound()
        {
            var interpolation=ball.interpolation;ball.interpolation=RigidbodyInterpolation.None;ball.isKinematic=false;ball.linearVelocity=ball.angularVelocity=Vector3.zero;cable.ResetCable(requiredTurns);ball.position=cable.StartPosition;ball.rotation=Quaternion.identity;ball.transform.SetPositionAndRotation(ball.position,ball.rotation);ball.isKinematic=true;ball.interpolation=interpolation;cable.Draw(0);Countdown=3;aftermath=0;
            for(int i=0;i<2;i++)
            {
                drivers[i]??=new FareFightRoute();drivers[i].Reset();stopped[i]=contactCue[i]=planAt[i]=0;fragments[i].ResetBody();trucks[i].tractorMotor.Configure();
                float gap=i==rules.Round%2?22:29;var p=cable.StartPosition+new Vector3(i==0?-3:3,.65f-cable.StartPosition.y,i==0?-gap:gap);trucks[i].Prepare(p,Quaternion.Euler(0,i==0?0:180,0));Hold(trucks[i]);
            }
            Physics.SyncTransforms();
        }
        static void Hold(SportsEventTruck truck){var input=truck.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
        protected override void Simulate(float dt)
        {
            elapsed+=dt;atmosphere.Tick(elapsed);
            if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);foreach(var truck in trucks)Hold(truck);if(Countdown==0){ball.isKinematic=false;rules.Begin();}return;}
            review.Capture(elapsed+3);
            if(rules.RoundWinner>=0)
            {
                foreach(var truck in trucks)Hold(truck);cable.Draw(rules.NetDegrees);aftermath+=dt;
                if(aftermath>=3){if(rules.Complete){review.focusSlot=rules.Winner;EndMatch();}else{rules.NextRound();ResetRound();}}return;
            }
            var radial=ball.position-cable.poleTop.position;rules.Sample(Mathf.Atan2(radial.z,radial.x)*Mathf.Rad2Deg,new Vector2(radial.x,radial.z).magnitude,dt);cable.Tick(rules.NetDegrees);if(rules.RoundWinner>=0){int winner=rules.RoundWinner;ConfirmScore("tether-round:"+rules.Round,winner,1,rules.wins[winner],"TETHER ROUND WON",SportsScoreTier.RoundWinner,"ROUNDS",roundEnded:true,matchEnded:rules.Complete);feedback.PlayAt(5,ball.position,0,true);}
            var relative=atmosphere.Flow-ball.linearVelocity;var drag=relative*Mathf.Min(relative.magnitude,50)*(1.225f*.47f*Mathf.PI*cable.ballRadius*cable.ballRadius*.5f);ball.AddForce(Vector3.ClampMagnitude(drag,15000),ForceMode.Force);
            for(int i=0;i<2;i++)
            {
                var truck=trucks[i];stopped[i]=truck.body.linearVelocity.magnitude<.5f?stopped[i]+dt:0;truck.tractorMotor.gripScale=atmosphere.Grip;truck.TickDrive(setup.players[i].source==BowlingSource.AI?Bot(i,dt):commands[i]);if(actions[i]||setup.players[i].source==BowlingSource.AI&&stopped[i]>14)Recover(i);
            }
        }
        Vector2 Bot(int slot,float dt)
        {
            var truck=trucks[slot];int skill=(int)setup.players[slot].difficulty;
            if(rules.Clock>=planAt[slot])
            {
                planAt[slot]=rules.Clock+(skill==0?.45f:skill==1?.3f:.18f);var prediction=ball.position+Vector3.ProjectOnPlane(ball.linearVelocity,Vector3.up)*(skill==0?.15f:.4f);
                var radial=Vector3.ProjectOnPlane(prediction-cable.poleTop.position,Vector3.up).normalized;var tangent=new Vector3(-radial.z,0,radial.x)*(slot==0?1:-1);var staging=prediction-tangent*13;staging.y=truck.body.position.y;
                bool aligned=Vector3.Angle(truck.transform.forward,tangent)<35&&Vector3.Distance(truck.body.position,prediction)<19;goals[slot]=aligned||Vector3.Distance(truck.body.position,staging)<6?prediction+tangent*9:staging;
            }
            drivers[slot].DirectGoal(truck.body.position,goals[slot]);return drivers[slot].Drive(truck.body,truck.transform,skill,dt,atmosphere.Grip,ball);
        }
        public void BallContact(SportsEventTruck truck,float relativeSpeed)
        {
            if(!Live||Countdown>0||rules.RoundWinner>=0||truck==null||truck.session!=this||relativeSpeed<.6f)return;ActualBallContacts++;
            if(rules.Clock<contactCue[truck.slot])return;contactCue[truck.slot]=rules.Clock+.3f;feedback.PlayAt(1,ball.position,relativeSpeed);Pulse(truck.slot,Mathf.Clamp01(relativeSpeed/15)*.5f,.15f);
        }
        public bool Recover(int slot)
        {
            var truck=trucks[slot];if(truck.body.position.y>-3&&Vector3.Dot(truck.transform.up,Vector3.up)>.5f&&(stopped[slot]<3||truck.body.linearVelocity.magnitude>.8f))return false;
            float a=slot*Mathf.PI+Mathf.PI*.25f;var p=new Vector3(Mathf.Cos(a)*40,.65f,Mathf.Sin(a)*40);if(Vector3.Distance(p,trucks[1-slot].body.position)<12)return false;
            truck.Prepare(p,Quaternion.LookRotation(Vector3.ProjectOnPlane(ball.position-p,Vector3.up)));drivers[slot].Reset();stopped[slot]=0;return true;
        }
        protected override void Present(float dt)
        {
            if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;
            var area=new Bounds(new Vector3(0,10,0),new Vector3(52,22,52));area.Encapsulate(ball.position);foreach(var truck in trucks)area.Encapsulate(truck.body.position);area.Expand(new Vector3(16,5,16));if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true;broadcast.Pose(area.center,new Vector3(0,105,-90),60);if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-105,90),Vector3.up);framing.arenaBounds=area;framing.Apply();
        }
        public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
        protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);
        protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)truck.StopSound();}
        protected override void StopGame(){review?.End(false);StopSounds();if(ball!=null)ball.isKinematic=true;if(trucks!=null)for(int i=0;i<trucks.Length;i++){trucks[i].Power(false);trucks[i].body.isKinematic=true;fragments[i].Freeze();}}
    }
}
