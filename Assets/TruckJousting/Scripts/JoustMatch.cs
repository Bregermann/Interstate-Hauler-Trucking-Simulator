using System;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckJousting
{
    public sealed class JoustMatch:SportsEventSession
    {
        public SportsEventTruck[] trucks;public SportsFragmentRig[] fragments;
        public JoustLance[] lances;public Transform[] mounts;public JoustShield[] shields;
        public Transform debrisGroup;public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;
        public SportsCameraFit framing;public SportsChaosWeather atmosphere;public SportsRecordedReview review;
        public JoustRules rules=new JoustRules();
        public float Countdown{get;private set;}public int ActualStrikes{get;private set;}
        [Min(15)]public float passSeconds=35;public float startDistance=115,finishDistance=145;
        readonly float[] height=new float[2],yaw=new float[2],cruise=new float[2],knockedFor=new float[2],hitAt=new float[2];
        readonly bool[] aiming=new bool[2],passed=new bool[2];float elapsed,intermission;string announcement="";float announceUntil;
        readonly StringBuilder text=new StringBuilder(350);
        public override string Title=>"TRUCK JOUSTING";public override int MinimumPlayers=>2;public override int MaximumPlayers=>2;
        public override int OptionCount=>2;public override int WeatherCount=>1;public override int CompactScoreColumns=>8;
        public override string OptionLabel=>option==0?"THREE PASSES":"FIVE PASSES";
        public override bool SupportsSplitAIViews=>true;public override bool SupportsRaceCountdown=>true;
        public override float RaceCountdownSeconds=>Live&&Countdown>0?Countdown:-1;public override float RaceStartSeconds=>Countdown<=0?rules.Clock:-1;
        public override bool ReplayControls=>review!=null&&review.Playing;
        public override bool MultiViewHUD=>!ReplayControls&&drivingViews!=null&&!drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible=>!ReplayControls;
        public override string Header=>Title+(ReplayControls?" · ACTUAL CONTACT REPLAY":" · PASS "+rules.Pass+" / "+rules.PassLimit);
        public override string Status=>Countdown>0?"READY YOUR LANCE":elapsed<announceUntil?announcement:rules.PassClosed?"PREPARING THE NEXT TOURNAMENT PASS":"ARMOR 1 · CENTRAL SHIELD 2 · CREDITED KNOCKDOWN 3 · "+Mathf.Max(0,passSeconds-rules.Clock).ToString("F0")+"s";
        public override string Help=>"Two full semi-trucks charge along opposite tournament lanes. Your giant side-mounted lance has a real physical tip: strike the opposing truck's shield. Armor earns1point; the central gold mark earns2. A truck genuinely overturned shortly after a credited hit upgrades that pass to3points. Only one lance hit scores per player per pass. Misses and wall hits earn nothing. Press ACTION to toggle lance aiming: directions then raise/lower and yaw the lance while your last accelerator command is held. Press ACTION again to drive. Aim is limited; steer and accelerate normally in driving mode. Lance tips and splinters react physically and reset each pass. Highest score after three or five passes wins; equal totals share placing. No invisible automatic hits, random knockdowns or AI-only points. Shared/chase/cab views, free look, horn, weather, Your Crew, optional AI cameras and a skippable recorded replay use the collection systems.";
        public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace(" ACTION"," TOGGLE LANCE AIM")+" · "+(aiming[slot]?"DIRECTIONS: HEIGHT / ANGLE":"DRIVE: STEER / ACCELERATE");
        public override bool TryReadScoreCard(int slot,out string value,out string detail){value=rules.points[slot]+"P";detail=lances[slot].Broken?"LANCE BROKEN · +"+rules.passPoints[slot]:"LANCE "+height[slot].ToString("F1")+"m · "+yaw[slot].ToString("F0")+"deg";return slot<2;}
        public override string ViewText(int view){int slot=drivingViews.rally.owners[view];TryReadScoreCard(slot,out var score,out var detail);return setup.players[slot].name+" · "+score+"\n"+detail+"\n"+ControlCaption(slot);}
        public int ResultPlace(int slot)=>rules.points[1-slot]>rules.points[slot]?2:1;
        public override string Standings{get{text.Clear();for(int i=0;i<2;i++)text.Append('#').Append(ResultPlace(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append("P · ").Append(rules.hits[i]).Append(" HITS / ").Append(rules.centralHits[i]).Append(" CENTRAL / ").Append(rules.knockdowns[i]).Append(" KNOCKDOWNS\n");return text.ToString();}}
        public override string Results=>(rules.Winner>=0?setup.players[rules.Winner].name+" WINS THE JOUST!":"SHARED TOURNAMENT PLACING")+"\n\n"+Standings;
        protected override void Awake(){base.Awake();rules.Awarded+=ConfirmAward;}
        protected override void OnDestroy(){if(rules!=null)rules.Awarded-=ConfirmAward;base.OnDestroy();}
        void ConfirmAward(JoustAwardKind kind,string id,int slot,int delta,int total){ConfirmScore(id,slot,delta,total,kind==JoustAwardKind.Knockdown?"CREDITED KNOCKDOWN":kind==JoustAwardKind.Central?"CENTRAL SHIELD":"ARMOR HIT",kind==JoustAwardKind.Armor?SportsScoreTier.Small:SportsScoreTier.Major);}
        protected override void BeginGame()
        {
            if(review==null){review=gameObject.AddComponent<SportsRecordedReview>();review.match=this;review.trucks=trucks;review.fragments=fragments;review.drivingViews=drivingViews;review.broadcast=broadcast;review.framing=framing;review.atmosphere=atmosphere;review.watchAction="watchjoust";review.extras=new[]{debrisGroup};}
            review.ResetRecord();rules.Reset(option==0?3:5);elapsed=0;ActualStrikes=0;ResetPass();drivingViews.Layout();atmosphere.Configure(Environment.TickCount,0,1);
        }
        void ResetPass()
        {
            Countdown=3;intermission=0;announcement="";announceUntil=0;
            for(int i=0;i<2;i++){fragments[i].ResetBody();trucks[i].tractorMotor.Configure();trucks[i].Prepare(new Vector3(i==0?-4:4,.65f,i==0?-startDistance:startDistance),Quaternion.Euler(0,i==0?0:180,0));trucks[i].tractorMotor.vehicle.powertrain.engine.flyingStartEnabled=true;trucks[i].tractorMotor.vehicle.powertrain.engine.StartEngine();height[i]=2.3f;yaw[i]=24;cruise[i]=knockedFor[i]=0;hitAt[i]=-100;aiming[i]=passed[i]=false;lances[i].ResetLance();PoseLance(i);Hold(trucks[i]);}
            Physics.SyncTransforms();
        }
        static void Hold(SportsEventTruck truck){var input=truck.tractorMotor.vehicle.input;input.states.throttle=input.states.throttleRaw=input.states.brakes=input.states.brakesRaw=0;input.states.handbrake=input.states.handbrakeRaw=1;}
        void PoseLance(int slot){mounts[slot].localPosition=new Vector3(1.6f,height[slot],3);mounts[slot].localRotation=Quaternion.Euler(0,yaw[slot],0);}
        protected override void Simulate(float dt)
        {
            elapsed+=dt;atmosphere.Tick(elapsed);
            if(Countdown>0){Countdown=Mathf.Max(0,Countdown-dt);foreach(var truck in trucks)Hold(truck);if(Countdown==0)rules.Begin();return;}
            review.Capture(elapsed+3);
            if(rules.PassClosed){foreach(var truck in trucks)Hold(truck);intermission+=dt;if(intermission>=3){if(rules.Complete){review.focusSlot=Mathf.Max(0,rules.Winner);EndMatch();}else{rules.NextPass();ResetPass();}}return;}
            rules.Tick(dt);bool beyond=true;
            for(int i=0;i<2;i++)
            {
                var truck=trucks[i];truck.tractorMotor.gripScale=atmosphere.Grip;
                Vector2 drive=commands[i];
                if(setup.players[i].source==BowlingSource.AI)drive=Bot(i,dt);
                else{
                    if(actions[i])aiming[i]=!aiming[i];
                    if(aiming[i]){yaw[i]=Mathf.Clamp(yaw[i]+drive.x*8*dt,15,32);height[i]=Mathf.Clamp(height[i]+drive.y*.9f*dt,1.2f,3.7f);drive=new Vector2(0,cruise[i]);}
                    else cruise[i]=drive.y;
                }
                passed[i] |= i==0?truck.body.position.z>=finishDistance:truck.body.position.z<=-finishDistance;
                PoseLance(i);if(passed[i])Hold(truck);else truck.TickDrive(drive);
                knockedFor[i]=Vector3.Dot(truck.transform.up,Vector3.up)<.35f?knockedFor[i]+dt:0;
                if(knockedFor[i]>.4f&&rules.Clock-hitAt[1-i]<4&&rules.Knockdown(i))Announce(setup.players[1-i].name+" · ACTUAL KNOCKDOWN!",5);
                beyond &= passed[i];
            }
            if(beyond||rules.Clock>=passSeconds||knockedFor[0]>3||knockedFor[1]>3)rules.EndPass();
        }
        Vector2 Bot(int slot,float dt)
        {
            int skill=(int)setup.players[slot].difficulty;var truck=trucks[slot];
            // Track the actual opposing shield. Bounded lower-skill oscillation is aiming error, never a score roll.
            var target=truck.transform.InverseTransformPoint(shields[1-slot].transform.position);
            float error=(skill==0?.35f:skill==1?.14f:.03f)*Mathf.Sin(rules.Clock*1.7f+slot+rules.Pass*2.3f);
            if(target.z>3&&!lances[slot].Broken){float desired=Mathf.Atan2(target.x-1.6f,12)*Mathf.Rad2Deg;yaw[slot]=Mathf.MoveTowards(yaw[slot],Mathf.Clamp(desired+error*10,15,32),12*dt);height[slot]=Mathf.MoveTowards(height[slot],Mathf.Clamp(target.y+error,1.2f,3.7f),1.2f*dt);}
            float lane=slot==0?-4:4;float forward=slot==0?1:-1;float lateral=(lane-truck.body.position.x)*forward;
            float heading=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),Vector3.forward*forward,Vector3.up);
            float steer=Mathf.Clamp(lateral*.35f+heading/30,-1,1);float speed=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);float desiredSpeed=skill==0?16:skill==1?19:22;
            return new Vector2(steer,speed<desiredSpeed-.5f?1:speed>desiredSpeed+1?-.5f:.25f);
        }
        public void Strike(int attacker,JoustShield shield,Vector3 point,float incomingNormalSpeed)
        {
            if(!Live||Countdown>0||shield==null||shield.match!=this||shield.owner==attacker||passed[attacker]||passed[shield.owner]||lances[attacker].Broken||!rules.Hit(attacker,shield.Central(point),incomingNormalSpeed))return;
            ActualStrikes++;hitAt[attacker]=rules.Clock;lances[attacker].Break();feedback.PlayAt(1,point,incomingNormalSpeed);Pulse(attacker,.8f,.25f);Pulse(1-attacker,.65f,.2f);Announce(setup.players[attacker].name+" · "+(rules.passPoints[attacker]==2?"CENTRAL SHIELD! +2":"ARMOR HIT! +1"),4);
        }
        void Announce(string message,int cue){announcement=message;announceUntil=elapsed+2;feedback.Play(cue,0,true);}
        protected override void Present(float dt)
        {
            if(drivingViews==null)return;if(ReplayControls){review.Tick();return;}drivingViews.Tick(dt);if(!drivingViews.NeedsArena)return;
            var area=new Bounds(new Vector3(0,3,0),new Vector3(22,8,30));foreach(var truck in trucks)area.Encapsulate(truck.body.position);area.Expand(new Vector3(12,4,20));
            // The shared director is the only pose writer while it owns this shot.
            // The old side-on rotation used to replace it between camera updates.
            broadcast.Pose(area.center,new Vector3(130,95,-20),60);
            if(broadcast.broadcastFraming==null||!broadcast.broadcastFraming.OwnsShot){broadcast.view.orthographic=true;broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(-130,-95,20),Vector3.up);}
            framing.arenaBounds=area;framing.Apply();
        }
        public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
        protected override void SetPaused(bool paused)=>atmosphere?.Pause(paused);
        protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)truck.StopSound();}
        protected override void StopGame(){review?.End(false);StopSounds();if(trucks!=null)for(int i=0;i<trucks.Length;i++){trucks[i].Power(false);trucks[i].body.isKinematic=true;lances[i].Freeze();}}
    }
}
