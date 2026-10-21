using System;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckFareFight;
namespace LWS.TruckSeesaw
{
    public sealed class SeesawMatch : SportsEventSession
    {
 public override bool BroadcastParticipantActive(int slot)=>rules.alive[slot];
 public override void DescribeBroadcast(SportsBroadcastAction a,SportsBroadcastFraming f){a.Objective(platform.position,2);a.reason="WEIGHT BALANCE / LOCAL EDGE CONTEST";}
        public Rigidbody platform;
        public ConfigurableJoint pivot;
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public ParticleSystem[] impacts;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public SportsRecordedReview review;
        public Vector3 platformOrigin = new Vector3(0,85,0);
        public float halfWidth = 41, halfThickness = .9f;
        public SeesawRules rules = new SeesawRules();
        public float Countdown { get; private set; }
        public float Tilt => Vector3.Angle(platform.transform.up,Vector3.up);
        public int QuarryImpacts { get; private set; }
        public override string Title => "TRUCK SEESAW";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 3;
        public override int WeatherCount => 1;
        public override string OptionLabel => new[]{"ONE HEAT","THREE HEATS","FIVE HEATS"}[option];
        public override bool SupportsSplitAIViews => true;
        public override int CompactScoreColumns => 4;
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Live && Countdown > 0 ? Countdown : -1;
        public override float RaceStartSeconds => Countdown <= 0 ? rules.Clock : -1;
        public override bool ReplayControls => review != null && review.Playing;
        public override bool MultiViewHUD => !ReplayControls && drivingViews != null && !drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible => !ReplayControls;
        public override bool ParticipantLocked(int slot) => !rules.alive[slot];
        public override string Header => Title + (ReplayControls ? " · RECORDED REPLAY" : " · HEAT " + (rules.Heat+1) + " / " + rules.HeatCount + " · " + Tilt.ToString("F0") + "° TILT");
        public override string Status => Countdown > 0 ? "BALANCE THE LOAD · GO IN " + Mathf.CeilToInt(Countdown) : rules.HeatComplete ? (rules.HeatWinner < 0 ? "SHARED SURVIVAL HEAT" : setup.players[rules.HeatWinner].name + " IS LAST TRUCK STANDING!") : Tilt > 17 ? "BOARD TIPPING · SHIFT WEIGHT UPHILL!" : "STAY ON THE BOARD · " + Mathf.Max(0,rules.HeatSeconds-rules.Clock).ToString("F0") + "s";
        public override string ControlCaption(int slot) => base.ControlCaption(slot).Replace(" ACTION"," RECOVER");
        public override string Help => "Balance a giant suspended deck above the quarry. Drive uphill to counter a tilt, spread out, and ram rivals toward the edges. Earn +1 for every 15 seconds survived, +2 when you help level a heavily tilted board by moving uphill, +3 for a credited knockoff, and +5 for sole survival. Heats last up to 90 seconds. If several trucks survive the timer, they share the heat without a sole-survivor bonus. Choose one, three or five heats; highest total wins and equal totals share placing. Solo is a 90-second balance challenge. Falling trucks crash into the quarry. RECOVER works only when you are stuck or overturned on the board. Choose a shared arena, chase or full-cab view; camera switch, free look, horn and actual device bindings are shown in Pause. Your Crew names/colors, selected weather and optional AI cameras carry through the match. Watch the final moments again from Results.";
        readonly FareFightRoute[] drivers = new FareFightRoute[4];
        readonly bool[] recoverySample = new bool[4], armed = new bool[4], crashed = new bool[4];
        readonly Vector3[] armedAt = new Vector3[4], uphillAt = new Vector3[4], incomingVelocity = new Vector3[4];
        readonly float[] recoveryCooldown = new float[4], stopped = new float[4];
        readonly StringBuilder text = new StringBuilder(500);
        float aftermath;
        public override bool TryReadScoreCard(int slot,out string value,out string detail)
        { value=rules.scores[slot]+"P"; detail=!rules.alive[slot]?"QUARRY DROP":armed[slot]?"SHIFT UPHILL FOR +2":rules.knockoffs[slot]+" KNOCKOFFS · "+rules.recoveries[slot]+" SAVES"; return slot<setup.count; }
        public override string ViewText(int view) {int slot=drivingViews.rally.owners[view]; TryReadScoreCard(slot,out var score,out var detail);return setup.players[slot].name+" · "+score+"\n"+detail+"\n"+Status;}
        public override string Standings {get {text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.scores[i]).Append("PTS · ").Append(rules.heatWins[i]).Append(" HEATS · ").Append(rules.knockoffs[i]).Append(" KNOCKOFFS · ").Append(rules.recoveries[i]).Append(" SAVES\n");return text.ToString();}}
        public override string Results => "QUARRY WOBBLE RESULTS\n\n"+Standings;
        public Vector3 Grid(int slot) => platformOrigin+new Vector3(slot%2==0?-16:16,halfThickness+.65f,slot<2?-16:16);
        protected override void BeginGame()
        {
            if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchseesaw";review.extras=new[]{platform.transform};}
            review.ResetRecord();rules.Reset(setup.count,new[]{1,3,5}[option]);QuarryImpacts=0;ResetPlatform();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
        }
        void ResetPlatform()
        {
            var interpolation=platform.interpolation;platform.interpolation=RigidbodyInterpolation.None;platform.isKinematic=false;platform.linearVelocity=platform.angularVelocity=Vector3.zero;platform.position=platformOrigin;platform.rotation=Quaternion.identity;platform.transform.SetPositionAndRotation(platformOrigin,Quaternion.identity);platform.isKinematic=true;platform.interpolation=interpolation;Countdown=3;aftermath=0;
            for(int i=0;i<trucks.Length;i++)
            {
                drivers[i]??=new FareFightRoute();drivers[i].Reset();recoverySample[i]=armed[i]=crashed[i]=false;stopped[i]=recoveryCooldown[i]=0;
                impacts[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);fragments[i].ResetBody();trucks[i].gameObject.SetActive(i<setup.count);if(i>=setup.count)continue;
                trucks[i].tractorMotor.Configure();trucks[i].Prepare(Grid(i),Quaternion.LookRotation(Vector3.ProjectOnPlane(platformOrigin-Grid(i),Vector3.up)));Hold(trucks[i]);
            }
            Physics.SyncTransforms();
        }
        static void Hold(SportsEventTruck truck){var input=truck.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
        public bool OnBoard(int slot)
        { foreach(var wheel in trucks[slot].wheels)if(wheel.enabled&&wheel.IsGrounded&&wheel.HitCollider!=null&&wheel.HitCollider.attachedRigidbody==platform)return true;return false; }
        protected override void Simulate(float dt)
        {
            atmosphere.Tick(rules.Clock);
            if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(Countdown==0)platform.isKinematic=false;return;}
            review.Capture((rules.Heat*100)+rules.Clock+aftermath+3);
            if(rules.HeatComplete)
            {
                aftermath+=dt;for(int i=0;i<setup.count;i++)if(rules.alive[i])Hold(trucks[i]);
                if(aftermath>=7){if(rules.Complete){int leader=0;for(int i=1;i<setup.count;i++)if(rules.scores[i]>rules.scores[leader])leader=i;review.focusSlot=leader;EndMatch();}else{rules.NextHeat();ResetPlatform();}}return;
            }
            int falls=0;float tilt=Tilt;var gravity=platform.transform.InverseTransformDirection(Vector3.down);var uphill=-new Vector3(gravity.x,0,gravity.z).normalized;
            for(int i=0;i<setup.count;i++)
            {
                recoverySample[i]=false;if(!rules.alive[i])continue;var truck=trucks[i];incomingVelocity[i]=truck.body.linearVelocity;var local=platform.transform.InverseTransformPoint(truck.body.worldCenterOfMass);bool supported=OnBoard(i);
                bool fallen=!supported&&(truck.body.position.y<platformOrigin.y-25||((Mathf.Abs(local.x)>halfWidth+3||Mathf.Abs(local.z)>halfWidth+3)&&local.y< -3));
                if(fallen){falls|=1<<i;armed[i]=false;truck.Power(false);continue;}
                if(tilt>=17&&supported&&!armed[i]&&rules.Clock>=recoveryCooldown[i]){armed[i]=true;armedAt[i]=local;uphillAt[i]=uphill;}
                if(armed[i]&&tilt<8&&supported)
                {var moved=Vector3.ProjectOnPlane(local-armedAt[i],Vector3.up);recoverySample[i]=moved.magnitude>=5&&Vector3.Dot(moved,uphillAt[i])>3;armed[i]=false;recoveryCooldown[i]=rules.Clock+10;}
                stopped[i]=truck.body.linearVelocity.magnitude<.5f?stopped[i]+dt:0;truck.tractorMotor.gripScale=atmosphere.Grip;
                truck.TickDrive(setup.players[i].source==BowlingSource.AI?Bot(i,dt,tilt,uphill):commands[i]);if(actions[i])Recover(i);
            }
            rules.Step(dt,falls,recoverySample);if(rules.HeatComplete)feedback.PlayAt(5,platform.position,0,true);
        }
        Vector2 Bot(int slot,float dt,float tilt,Vector3 uphill)
        {
            var truck=trucks[slot];var local=platform.transform.InverseTransformPoint(truck.body.position);float skill=(int)setup.players[slot].difficulty;
            Vector3 weight=Vector3.zero;float mass=0;for(int i=0;i<setup.count;i++)if(rules.alive[i]&&OnBoard(i)){weight+=platform.transform.InverseTransformPoint(trucks[i].body.worldCenterOfMass)*trucks[i].body.mass;mass+=trucks[i].body.mass;}
            if(mass>0)weight/=mass;float angle=slot*Mathf.PI*.5f;var goal=new Vector3(Mathf.Sin(angle)*8,halfThickness,Mathf.Cos(angle)*8)-Vector3.ProjectOnPlane(weight,Vector3.up)*.65f;
            Rigidbody opponent=null;
            if(tilt>9-skill)goal=uphill*Mathf.Lerp(15,25,Mathf.InverseLerp(9,25,tilt))+new Vector3(Mathf.Sin(angle)*3,halfThickness,Mathf.Cos(angle)*3);
            else
            {
                float nearest=float.MaxValue;for(int i=0;i<setup.count;i++)if(i!=slot&&rules.alive[i]){var p=platform.transform.InverseTransformPoint(trucks[i].body.position);float d=(p-local).sqrMagnitude;if(d<nearest){nearest=d;opponent=trucks[i].body;}}
                if(opponent!=null&&nearest<400){var p=platform.transform.InverseTransformPoint(opponent.position);var outward=Vector3.ProjectOnPlane(p,Vector3.up).normalized;if(outward.sqrMagnitude<.1f)outward=Vector3.right;goal=Vector3.Dot(local-p,outward)<-2?p+outward*12:p-outward*8;goal.y=halfThickness;}
            }
            goal.x=Mathf.Clamp(goal.x,-halfWidth+12,halfWidth-12);goal.z=Mathf.Clamp(goal.z,-halfWidth+12,halfWidth-12);drivers[slot].DirectGoal(truck.body.position,platform.transform.TransformPoint(goal));
            var command=drivers[slot].Drive(truck.body,truck.transform,(int)skill,dt,atmosphere.Grip,opponent);
            // No extra force or orientation correction: NWH tires and the
            // actual moving ground remain the only driving authority.
            if(Mathf.Max(Mathf.Abs(local.x),Mathf.Abs(local.z))>halfWidth-7&&Vector3.Dot(platform.transform.InverseTransformDirection(truck.transform.forward),new Vector3(local.x,0,local.z))>0)command.y=-.7f;
            if(stopped[slot]>18||Vector3.Dot(truck.transform.up,platform.transform.up)<.25f&&stopped[slot]>4)Recover(slot);
            return command;
        }
        public bool Recover(int slot)
        {
            var truck=trucks[slot];var local=platform.transform.InverseTransformPoint(truck.body.position);
            if(!rules.alive[slot]||local.y< -2||Mathf.Abs(local.x)>halfWidth||Mathf.Abs(local.z)>halfWidth||Vector3.Dot(truck.transform.up,platform.transform.up)>.5f&&(stopped[slot]<3||truck.body.linearVelocity.magnitude>.8f))return false;
            for(int k=0;k<8;k++){float a=(slot+k)*Mathf.PI/4;var point=platform.transform.TransformPoint(new Vector3(Mathf.Sin(a)*15,halfThickness+.65f,Mathf.Cos(a)*15));bool clear=true;for(int j=0;j<setup.count;j++)if(j!=slot&&rules.alive[j]&&Vector3.Distance(trucks[j].body.position,point)<10)clear=false;if(!clear)continue;truck.Prepare(point,Quaternion.LookRotation(Vector3.ProjectOnPlane(platform.position-point,platform.transform.up),platform.transform.up));drivers[slot].Reset();armed[slot]=false;stopped[slot]=0;return true;}return false;
        }
        public void BodyContact(int a,int b,Vector3 normal)
        {
            if(!Live||Countdown>0||rules.HeatComplete)return;float incoming=Mathf.Abs(Vector3.Dot(incomingVelocity[a]-incomingVelocity[b],normal));float first=Vector3.Dot(incomingVelocity[a],-normal),second=Vector3.Dot(incomingVelocity[b],normal);
            if(first>=second-.5f&&first>2)rules.RecordPush(a,b,incoming);if(second>=first-.5f&&second>2)rules.RecordPush(b,a,incoming);
        }
        public void QuarryContact(int slot,Collision collision)
        {
            if(!Running||rules.alive[slot]||crashed[slot]||collision.rigidbody!=null||trucks[slot].body.position.y>platformOrigin.y-18)return;
            if(collision.relativeVelocity.magnitude<5)return;crashed[slot]=true;QuarryImpacts++;var point=collision.GetContact(0).point;fragments[slot].Burst();impacts[slot].transform.position=point;impacts[slot].Play();feedback.PlayAt(3,point,12,true);Pulse(slot,1,.4f);
        }
        protected override void Present(float dt)
        {
            if(drivingViews==null)return;if(ReplayControls)
            {
                review.Tick();if(review.Playing)
                {
                    var point=review.record.Position(review.focusSlot);var offset=review.record.Angle==0?new Vector3(22,52,-30):review.record.Angle==1?new Vector3(-26,48,24):new Vector3(0,65,-8);broadcast.Pose(point+Vector3.up*2,offset,60,true);
                    var board=review.record.ghosts[trucks.Length*2];var local=board.InverseTransformPoint(broadcast.transform.position);
                    if(Mathf.Abs(local.x)<halfWidth+3&&Mathf.Abs(local.z)<halfWidth+3&&local.y<8){broadcast.transform.position+=board.up*(8-local.y);if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(point+Vector3.up*2-broadcast.transform.position,Vector3.up);}
                }
                return;
            }drivingViews.Tick(dt);
            if(drivingViews.NeedsArena){var area=new Bounds(platformOrigin,Vector3.zero);for(int corner=0;corner<8;corner++)area.Encapsulate(platform.transform.TransformPoint(new Vector3((corner&1)==0?-halfWidth:halfWidth,(corner&2)==0?-halfThickness:halfThickness,(corner&4)==0?-halfWidth:halfWidth)));area.Expand(new Vector3(20,20,20));for(int i=0;i<setup.count;i++)if(!crashed[i])area.Encapsulate(trucks[i].body.position);if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true;broadcast.Pose(area.center,new Vector3(0,170,-85),60);if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-170,85),Vector3.up);framing.arenaBounds=area;framing.Apply();}
        }
        public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
        protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);
        protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)truck.StopSound();}
        protected override void StopGame(){review?.End(false);StopSounds();if(platform!=null)platform.isKinematic=true;if(trucks==null)return;for(int i=0;i<trucks.Length;i++){trucks[i].Power(false);trucks[i].body.isKinematic=true;fragments[i].Freeze();}}
    }
}
