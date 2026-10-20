using System;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckFareFight;
using LWS.InterstateHauler;
namespace LWS.TruckFloorIsLava
{
    public sealed class LavaMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public ParticleSystem[] blasts,splashes;
        public LavaPlatform[] platforms;
        public Transform platformGroup,lava;
        public Collider lavaVolume;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public SportsRecordedReview review;
        public LwsRoadGraph roads=new LwsRoadGraph();
        public int[] edgeFrom,edgeTo;
        public LavaRules rules=new LavaRules();
        [Min(1)]public float jumpHeight=5;
        public float jumpCooldown=1.7f;
        public float Countdown {get;private set;}
        public int LavaImpacts {get;private set;}
        public int ActualJumps {get;private set;}
        public int ActualBodyContacts {get;private set;}
        public float LastBodyContactAt {get;private set;}
        public float LastBodyNormalSpeed {get;private set;}
        public override string Title=>"TRUCK FLOOR IS LAVA";
        public override int MinimumPlayers=>1;
        public override int MaximumPlayers=>8;
        public override int OptionCount=>3;
        public override int WeatherCount=>1;
        public override int CompactScoreColumns=>8;
        public override bool SupportsSplitAIViews=>true;
        public override bool SupportsRaceCountdown=>true;
        public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;
        public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;
        public override string OptionLabel=>new[]{"LONGER WARNINGS","STANDARD COLLAPSE","FAST COLLAPSE"}[option];
        public override bool ReplayControls=>review!=null&&review.Playing;
        public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible=>!ReplayControls;
        public override bool ParticipantLocked(int slot)=>!rules.alive[slot];
        public override string Header=>Title+(ReplayControls?" · RECORDED REPLAY":" · "+AliveCount()+" SURVIVORS · "+rules.Available()+" PLATFORMS LEFT");
        public override string Status=>Countdown>0?"FIND YOUR ESCAPE ROUTE":rules.Complete?(rules.Winner>=0?setup.players[rules.Winner].name+" IS LAST TRUCK STANDING!":"SIMULTANEOUS FINAL LAVA IMPACTS · SHARED PLACING"):"TOUCH STARTS THE WARNING · JUMP THE GAPS · RAM RIVALS OFF";
        public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace(" ACTION"," JUMP");
        public override string Help=>"Eight trucks scramble across a finite network of platforms above molten lava. Touching a platform starts its warning: orange becomes red, cracks appear, then the whole platform physically falls. Escape before it drops. Use JUMP to clear gaps; it only works from a supported upright truck and needs a short recharge. Narrow, moving, tilting, short-warning and temporary-safe platforms offer different risks. Ramming is allowed. Survival decides placement; +1 per 15 seconds, +1 for a recent credited knockoff and +5 for the last survivor are bonus statistics. Simultaneous final lava impacts share placing. Solo continues until your actual lava impact. Choose longer, standard or fast warnings. Shared/chase/full cab, camera switch, free look, horn, Your Crew, selected weather and optional AI views use the collection controls. Rematch restores the entire arena. Watch the final escape sequence again from Results.";
        readonly int[] current=new int[8],target=new int[8];
        readonly float[] jumpAt=new float[8],previousBottom=new float[8];
        readonly Vector3[] incoming=new Vector3[8];
        readonly bool[] wasAlive=new bool[8];
        readonly FareFightRoute[] drivers=new FareFightRoute[8];
        readonly StringBuilder text=new StringBuilder(650);
        int pendingLava;float aftermath;
        protected override void Awake(){base.Awake();Array.Fill(current,-1);Array.Fill(target,-1);}
        int AliveCount(){int count=0;for(int i=0;i<setup.count;i++)if(rules.alive[i])count++;return count;}
        public override bool ValidateSources(out string reason){if(!base.ValidateSources(out reason))return false;int humans=0;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)humans++;if(humans<=4)return true;reason="Up to four local humans; assign the remaining trucks to AI.";return false;}
        public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.scores[slot]+"P";detail=rules.survived[slot].ToString("F0")+"s · "+(rules.alive[slot]?"LIVE":"LAVA")+" · K"+rules.knockoffs[slot];return slot<setup.count;}
        public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var value,out var detail);string floor=Countdown>0?"SETTLING FOR START":current[slot]>=0?"FLOOR: "+rules.Remaining(current[slot]).ToString("F1")+"s":"AIRBORNE";return setup.players[slot].name+" · "+value+"\n"+detail+"\n"+floor+" · "+(rules.Clock<jumpAt[slot]?"JUMP "+(jumpAt[slot]-rules.Clock).ToString("F1")+"s":"JUMP READY");}
        public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.survived[i].ToString("F1")).Append("s · ").Append(rules.scores[i]).Append("P · ").Append(rules.knockoffs[i]).Append(" KNOCKOFFS\n");return text.ToString();}}
        public override string Results=>(rules.Winner>=0?setup.players[rules.Winner].name+" SURVIVES!":"SURVIVAL RESULTS")+"\n\n"+Standings;
        public Vector3 Grid(int slot)=>platforms[12].transform.position+new Vector3((slot%4-1.5f)*9,.65f,slot<4?-8:8);
        protected override void BeginGame()
        {
            if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchlava";review.extras=new[]{platformGroup};}
            review.ResetRecord();rules.Reset(setup.count,platforms.Select(p=>p.warningSeconds*new[]{1.25f,1f,.75f}[option]).ToArray());Countdown=3;LavaImpacts=ActualJumps=ActualBodyContacts=pendingLava=0;LastBodyContactAt=LastBodyNormalSpeed=0;aftermath=0;
            for(int i=0;i<platforms.Length;i++)platforms[i].ResetPlatform(this,rules,i);
            for(int i=0;i<trucks.Length;i++)
            {
                drivers[i]??=new FareFightRoute(new LwsNavigationTuning{routeConnectorToleranceMeters=.5f});drivers[i].Reset();current[i]=target[i]=-1;jumpAt[i]=0;
                fragments[i].ResetBody();blasts[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);splashes[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);trucks[i].gameObject.SetActive(i<setup.count);if(i>=setup.count)continue;
                trucks[i].tractorMotor.Configure();trucks[i].Prepare(Grid(i),Quaternion.Euler(0,slotHeading(i),0));previousBottom[i]=trucks[i].shape.bounds.min.y;Hold(trucks[i]);
            }
            Physics.SyncTransforms();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
        }
        static float slotHeading(int slot)=>slot<4?0:180;
        static void Hold(SportsEventTruck truck){var input=truck.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
        protected override void Simulate(float dt)
        {
            atmosphere.Tick(rules.Clock);
            if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);for(int i=0;i<setup.count;i++)Hold(trucks[i]);if(Countdown==0)rules.Begin();return;}
            review.Capture(rules.Clock+3+aftermath);
            if(rules.Complete){aftermath+=dt;for(int i=0;i<setup.count;i++)if(rules.alive[i])Hold(trucks[i]);if(aftermath>=2){int focus=rules.Winner>=0?rules.Winner:0;for(int i=0;i<setup.count;i++)if(rules.survived[i]>rules.survived[focus])focus=i;review.focusSlot=focus;EndMatch();}return;}
            int losses=pendingLava;pendingLava=0;
            for(int i=0;i<setup.count;i++)
            {
                wasAlive[i]=rules.alive[i];if(!wasAlive[i])continue;var truck=trucks[i];float bottom=truck.shape.bounds.min.y;
                if(previousBottom[i]>lava.position.y&&bottom<=lava.position.y&&InsideLava(truck.body.worldCenterOfMass))losses|=1<<i;
                previousBottom[i]=bottom;incoming[i]=truck.body.linearVelocity;current[i]=-1;
                foreach(var wheel in truck.wheels)if(wheel.enabled&&wheel.IsGrounded&&wheel.HitCollider!=null)
                {var floor=wheel.HitCollider.GetComponentInParent<LavaPlatform>();if(floor==null)continue;int slot=Array.IndexOf(platforms,floor);if(slot<0)continue;current[i]=slot;floor.TouchFromTruck(truck);break;}
            }
            rules.Step(dt,losses);
            for(int i=0;i<platforms.Length;i++)platforms[i].Tick(lava.position.y);
            for(int i=0;i<setup.count;i++)
            {
                if(wasAlive[i]&&!rules.alive[i]){DestroyInLava(i);continue;}if(!rules.alive[i]||rules.Complete)continue;
                var truck=trucks[i];truck.tractorMotor.gripScale=atmosphere.Grip;truck.TickDrive(setup.players[i].source==BowlingSource.AI?Bot(i,dt):commands[i]);if(actions[i])Jump(i);
            }
        }
        bool InsideLava(Vector3 point){var b=lavaVolume.bounds;return point.x>=b.min.x&&point.x<=b.max.x&&point.z>=b.min.z&&point.z<=b.max.z;}
        public void MarkLavaContact(SportsEventTruck truck){if(Live&&rules.Started&&!rules.Complete&&truck!=null&&truck.session==this&&truck.slot<setup.count&&rules.alive[truck.slot]&&InsideLava(truck.body.worldCenterOfMass))pendingLava|=1<<truck.slot;}
        void DestroyInLava(int slot){var point=trucks[slot].body.worldCenterOfMass;point.y=lava.position.y;trucks[slot].Power(false);fragments[slot].Burst();blasts[slot].transform.position=splashes[slot].transform.position=point;blasts[slot].Play();splashes[slot].Play();LavaImpacts++;feedback.PlayAt(3,point,10,true);Pulse(slot,1,.4f);}
        public bool Jump(int slot)
        {
            var truck=trucks[slot];if(!Live||!rules.Started||rules.Complete||!rules.alive[slot]||rules.Clock<jumpAt[slot]||!truck.tractorMotor.Grounded||Vector3.Dot(truck.transform.up,Vector3.up)<.5f)return false;
            float impulse=Mathf.Clamp(Mathf.Sqrt(2*Physics.gravity.magnitude*jumpHeight)-truck.body.linearVelocity.y,0,16);truck.tractorMotor.AddAssemblyVelocity(Vector3.up*impulse);jumpAt[slot]=rules.Clock+jumpCooldown;ActualJumps++;feedback.PlayAt(2,truck.body.position,0,false);return true;
        }
        Vector2 Bot(int slot,float dt)
        {
            var truck=trucks[slot];int home=current[slot];
            if(target[slot]<0||rules.platforms[target[slot]]==LavaPlatformState.Falling||home==target[slot])
            {
                int best=-1;float bestScore=float.NegativeInfinity;
                for(int i=0;i<platforms.Length;i++)if(i!=home&&rules.platforms[i]!=LavaPlatformState.Falling)
                {bool adjacent=home<0;for(int e=0;e<edgeFrom.Length;e++)if(edgeFrom[e]==home&&edgeTo[e]==i||edgeTo[e]==home&&edgeFrom[e]==i)adjacent=true;if(!adjacent)continue;float distance=Vector3.Distance(truck.body.position,platforms[i].body.position);float safety=rules.platforms[i]==LavaPlatformState.Untouched?platforms[i].warningSeconds:rules.Remaining(i)-distance/9;float score=safety-distance*.16f+(i+slot)%3*.4f;if(safety>2&&score>bestScore){best=i;bestScore=score;}}
                target[slot]=best;
            }
            int destination=target[slot];if(destination<0)return Vector2.zero;var goal=platforms[destination].body.position;
            if(home>=0&&rules.Remaining(home)>16)
            {
                for(int other=0;other<setup.count;other++)if(other!=slot&&rules.alive[other]&&current[other]==home&&Vector3.Distance(truck.body.position,trucks[other].body.position)<12)
                {
                    var outward=Vector3.ProjectOnPlane(trucks[other].body.position-platforms[home].body.position,Vector3.up).normalized;
                    if(outward.sqrMagnitude<.1f)outward=truck.transform.forward;drivers[slot].DirectGoal(truck.body.position,trucks[other].body.position+outward*8);return drivers[slot].Drive(truck.body,truck.transform,(int)setup.players[slot].difficulty,dt,atmosphere.Grip,trucks[other].body);
                }
            }
            // The selected destination is a live, authored adjacent jump edge.
            // Drive that local edge directly rather than routing every truck
            // back through the same platform centre before it can escape.
            drivers[slot].DirectGoal(truck.body.position,goal);
            if(home>=0&&drivers[slot].points.Count>0)
            {
                var floor=platforms[home];var direction=Vector3.ProjectOnPlane(drivers[slot].points[Mathf.Min(drivers[slot].Cursor,drivers[slot].points.Count-1)]-truck.body.position,Vector3.up);
                var local=floor.transform.InverseTransformPoint(truck.body.worldCenterOfMass);var heading=floor.transform.InverseTransformDirection(truck.transform.forward);float edge=float.PositiveInfinity;
                if(Mathf.Abs(heading.x)>.01f)edge=Mathf.Min(edge,(Mathf.Sign(heading.x)*floor.halfSize.x-local.x)/heading.x);
                if(Mathf.Abs(heading.z)>.01f)edge=Mathf.Min(edge,(Mathf.Sign(heading.z)*floor.halfSize.y-local.z)/heading.z);
                if(edge<Mathf.Max(4.5f,truck.body.linearVelocity.magnitude*.45f)&&Vector3.Angle(truck.transform.forward,direction)<45)Jump(slot);
            }
            return drivers[slot].Drive(truck.body,truck.transform,(int)setup.players[slot].difficulty,dt,atmosphere.Grip);
        }
        public void BodyContact(int a,int b,Vector3 normal){if(!Live||!rules.Started||rules.Complete)return;float speed=Mathf.Abs(Vector3.Dot(incoming[a]-incoming[b],normal));ActualBodyContacts++;LastBodyContactAt=rules.Clock;LastBodyNormalSpeed=speed;float first=Vector3.Dot(incoming[a],-normal),second=Vector3.Dot(incoming[b],normal);if(first>=second-.5f&&first>3)rules.RecordPush(a,b,speed);if(second>=first-.5f&&second>3)rules.RecordPush(b,a,speed);}
        protected override void Present(float dt)
        {
            if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;
            int focus=0;for(int i=0;i<setup.count;i++)if(rules.alive[i]){focus=i;break;}var area=new Bounds(trucks[focus].body.position,new Vector3(36,6,36));for(int i=0;i<setup.count;i++)if(rules.alive[i])area.Encapsulate(trucks[i].body.position);area.Expand(new Vector3(12,8,12));broadcast.view.orthographic=true;broadcast.Pose(area.center,new Vector3(0,110,-90),60);broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0,-110,90),Vector3.up);framing.arenaBounds=area;framing.Apply();
        }
        public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
        protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);
        protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)truck.StopSound();}
        protected override void StopGame(){review?.End(false);StopSounds();if(platforms!=null)foreach(var floor in platforms)floor.Freeze();if(trucks==null)return;for(int i=0;i<trucks.Length;i++){trucks[i].Power(false);trucks[i].body.isKinematic=true;fragments[i].Freeze();}}
    }
}
