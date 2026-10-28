using System;
using System.Linq;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckTruckGoose
{
    public sealed class GooseMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks=Array.Empty<SportsEventTruck>();
        public SportsDrivingViews drivingViews; public SportsEventCamera broadcast;
        public Transform[] bayMarkers=Array.Empty<Transform>();
        public Transform center; public float radius=52,bayRadius=43,roadHalfWidth=13;
        public GooseRules Rules { get; private set; }
        public float RoundTime { get; private set; }
        public int RecoveryCount { get; private set; }
        float resultTime,pickTime; readonly float[] stuck=new float[12],safeAngle=new float[12]; readonly Vector3[] last=new Vector3[12];
        public override string Title=>"TRUCK TRUCK GOOSE";
        public override string Help=>"The picker drives around the outside and presses ACTION near a parked truck: GOOSE! Both trucks race opposite ways around the marked loop to its vacant bay. Complete the loop, arrive upright and slow below 4 m/s to claim +1. The loser picks next. Highest score after the chosen rounds wins. Inner and outer lines permit physical passing; the central playground cannot bypass loop progress. A stranded racer returns to its last course angle after 8 seconds, losing only 12 degrees of progress. Drive with your assigned controls; ACTION chooses the goose. Pause, view and rematch use the standard menus.";
        public override int MinimumPlayers=>8; public override int MaximumPlayers=>12;
        public override bool SupportsSplitAIViews=>true;
        public override string OptionLabel=>SelectedRounds; public string SelectedRounds=>new[]{"3 ROUNDS","5 ROUNDS","8 ROUNDS"}[option];
        public override bool CompactBroadcastStatus=>true; public override bool CompactViewportHud=>true;
        public override int CompactScoreColumns=>12;
        public override bool MultiViewHUD=>drivingViews!=null&&!drivingViews.NeedsArena;
        public override bool TryReadViewportCard(int view,out int participant,out string score,out string detail)=>TryReadDrivingViewportCard(drivingViews,view,out participant,out score,out detail);
        public override string Header=>Rules==null?Title:$"ROUND {Rules.Round}/{Rules.Rounds} · {Rules.Phase}";
        public override string Status=>Rules==null?"":Rules.Phase==GooseRules.Stage.Picking?Name(Rules.Picker)+" · PICK A GOOSE":Rules.Phase==GooseRules.Stage.RoundResult?Name(Rules.Winner)+" CLAIMS +1":$"{Name(Rules.Picker)} CW | {Name(Rules.Goose)} CCW | {RoundTime:0.0}s";
        public override string Standings=>Rules==null?"":string.Join("\n",Enumerable.Range(0,setup.count).OrderByDescending(i=>Rules.Scores[i]).Select(i=>Name(i)+"  "+Rules.Scores[i]));
        public override string Results=>Rules==null?"":("TOP CLAIMS · "+string.Join(" / ",Enumerable.Range(0,setup.count).Where(i=>Rules.Scores[i]==Rules.Scores.Max()).Select(Name))+"\n"+Standings);
        string Name(int i)=>i>=0&&i<setup.count?setup.players[i].name:"";
        public override bool TryReadScoreCard(int card,out string value,out string detail){if(card<0||card>=setup.count){value=detail=null;return false;}value=Rules==null?"0":Rules.Scores[card].ToString();detail=Rules==null?"READY":card==Rules.Picker?"PICKER":card==Rules.Goose?"GOOSE":"PARKED";return card>=0&&card<setup.count;}
        public override string ViewportContextPrompt(int view)=>Status;
        public override string ViewportTelemetry(int view){if(Rules==null)return "";int owner=drivingViews!=null&&view>=0&&view<drivingViews.rally.owners.Length?drivingViews.rally.owners[view]:-1;return owner>=0&&owner<setup.count?$"LOOP {Mathf.Clamp01((float)Rules.Progress(owner)/350)*100:0}% | {RoundTime:0.0}s":"";}
        public override bool BroadcastParticipantActive(int slot)=>Rules==null?slot>=0&&slot<setup.count:Rules.Phase==GooseRules.Stage.Picking?slot==Rules.Picker:slot==Rules.Picker||slot==Rules.Goose;
        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){if(Rules==null)return;action.primary=Rules.Picker;action.preferHuman=false;action.reason=Rules.Phase==GooseRules.Stage.Picking?"PICKER SELECTING":"OPPOSITE GOOSE RACE";if(Rules.Goose>=0)action.Objective(Bay(Rules.Goose),5);}
        public override void ExtraAction(string key){if(key=="drivingview"&&drivingViews!=null)drivingViews.Cycle();}
        public override bool ValidateSources(out string reason){if(!base.ValidateSources(out reason))return false;if(setup.players.Take(setup.count).Count(p=>p.source!=BowlingSource.AI)>4){reason="At most four local drivers; the remaining trucks use AI.";return false;}return true;}
        public Vector3 Origin=>center!=null?center.position:Vector3.zero;
        public float Angle(Vector3 p)=>Mathf.Atan2(p.x-Origin.x,p.z-Origin.z)*Mathf.Rad2Deg;
        public Vector3 CoursePoint(float degrees,float r){float a=degrees*Mathf.Deg2Rad;return Origin+new Vector3(Mathf.Sin(a)*r,.7f,Mathf.Cos(a)*r);}
        public Quaternion Heading(float angle,int direction)=>Quaternion.Euler(0,angle+(direction>0?90:-90),0);
        public Vector3 Bay(int slot)=>CoursePoint(slot*360f/setup.count,bayRadius);
        protected override void BeginGame(){if(trucks.Length<setup.count)throw new InvalidOperationException("Goose requires every full NWH actor.");foreach(var truck in trucks)if(truck.tractorMotor!=null)truck.tractorMotor.Configure();Rules=new GooseRules(setup.count,new[]{3,5,8}[option]);RecoveryCount=0;SetupRound();if(drivingViews!=null)drivingViews.Layout();}
        void SetupRound(){RoundTime=pickTime=resultTime=0;for(int marker=0;marker<bayMarkers.Length;marker++){bayMarkers[marker].gameObject.SetActive(marker<setup.count);if(marker<setup.count)bayMarkers[marker].SetPositionAndRotation(Bay(marker)-Vector3.up*.65f,Heading(marker*360f/setup.count,1));}for(int i=0;i<trucks.Length;i++){if(i>=setup.count){trucks[i].gameObject.SetActive(false);continue;}bool picker=i==Rules.Picker;float angle=i*360f/setup.count;trucks[i].Prepare(picker?CoursePoint(angle,radius):Bay(i),Heading(angle,1),picker);if(!picker)trucks[i].body.isKinematic=true;trucks[i].body.detectCollisions=true;stuck[i]=0;safeAngle[i]=angle;last[i]=trucks[i].body.position;}ConsumeActions();}
        public bool Choose(int slot){if(Rules==null||Rules.Phase!=GooseRules.Stage.Picking)return false;var p=trucks[Rules.Picker];bool nearby=slot!=Rules.Picker&&slot>=0&&slot<setup.count&&Vector3.Distance(p.body.position,trucks[slot].body.position)<15;if(!nearby||!Rules.Select(slot,Angle(p.body.position),Angle(trucks[slot].body.position),nearby))return false;trucks[slot].body.isKinematic=false;trucks[slot].Power(true);RoundTime=0;feedback.Play(2,0,true);return true;}
        protected override void Simulate(float dt)
        {
            if(Rules.Phase==GooseRules.Stage.RoundResult){resultTime+=dt;if(resultTime>2.2f){if(!Rules.NextRound()){EndMatch();return;}SetupRound();}return;}
            RoundTime+=dt;pickTime+=dt;
            for(int i=0;i<setup.count;i++){
                bool active=i==Rules.Picker||Rules.Phase==GooseRules.Stage.Racing&&i==Rules.Goose;if(!active)continue;
                var t=trucks[i];Vector2 drive=commands[i];bool ai=setup.players[i].source==BowlingSource.AI;float a=Angle(t.body.position);
                if(ai){int dir=Rules.Phase==GooseRules.Stage.Picking?1:Rules.Direction(i);Vector3 target=CoursePoint(a+dir*14,radius+(dir<0?-3:3));if(Rules.Phase==GooseRules.Stage.Racing&&Rules.Progress(i)>=350)target=Bay(Rules.Goose);drive=Steer(t,target);}
                t.TickDrive(drive);
                if(Rules.Phase==GooseRules.Stage.Picking){if(i==Rules.Picker&&(actions[i]||ai&&pickTime>2.5f)){int nearest=-1;float best=15;for(int s=0;s<setup.count;s++)if(s!=i){float d=Vector3.Distance(t.body.position,Bay(s));if(d<best){best=d;nearest=s;}}if(nearest>=0)Choose(nearest);}continue;}
                float radial=Vector3.ProjectOnPlane(t.body.position-Origin,Vector3.up).magnitude;bool legal=radial>=radius-roadHalfWidth&&radial<=radius+roadHalfWidth;Rules.Observe(i,a,legal);if(legal&&Vector3.Dot(t.transform.up,Vector3.up)>.45f)safeAngle[i]=a;
                if(Rules.Claim(i,Vector3.Distance(t.body.position,Bay(Rules.Goose))<5,t.body.linearVelocity.magnitude,Vector3.Dot(t.transform.up,Vector3.up)>.65f)){ConfirmScore("goose.claim."+Rules.Round,i,1,Rules.Scores[i],"LEGAL CLAIM",SportsScoreTier.RoundWinner,roundEnded:true,matchEnded:Rules.Round==Rules.Rounds);foreach(var truck in trucks)truck.TickDrive(Vector2.zero);feedback.Play(4,0,true);break;}
                if(Vector3.Distance(t.body.position,last[i])<.025f||Vector3.Dot(t.transform.up,Vector3.up)<.2f||radial<radius-roadHalfWidth||radial>radius+roadHalfWidth)stuck[i]+=dt;else stuck[i]=0;last[i]=t.body.position;
                if(stuck[i]>8){int dir=Rules.Direction(i);float recover=safeAngle[i];var pose=CoursePoint(recover,radius+dir*3);t.Prepare(pose,Heading(recover,dir));Rules.Recover(i,recover);stuck[i]=0;RecoveryCount++;}
            }
        }
        static Vector2 Steer(SportsEventTruck truck,Vector3 target){var local=truck.transform.InverseTransformPoint(target);float error=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;float speed=truck.body.linearVelocity.magnitude;float distance=Vector3.ProjectOnPlane(target-truck.body.position,Vector3.up).magnitude;float desired=distance<7?Mathf.Clamp(distance*.6f,0,4):Mathf.Abs(error)>65?3:11;float throttle=Mathf.Clamp((desired-speed)*.8f,-.7f,1);if(distance<4&&speed>2)throttle=-.5f;return new Vector2(Mathf.Clamp(error/30,-1,1),throttle);}
        protected override void Present(float dt){if(drivingViews!=null)drivingViews.Tick(dt);if(broadcast!=null&&(drivingViews==null||drivingViews.NeedsArena))broadcast.Pose(Origin,new Vector3(0,100,-100),60);}
        protected override void StopGame(){foreach(var t in trucks)if(t!=null)t.Power(false);}
        protected override void StopSounds(){foreach(var t in trucks)if(t!=null)t.StopSound();}
    }
}
