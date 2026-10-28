using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckJenga;
namespace LWS.TruckArchaeology
{
    public sealed partial class ArchaeologyMatch:SportsEventSession,ISportsWorkshopState
    {
        public SportsEventTruck[] trucks;public ArchaeologyEquipment[] equipment;public ArchaeologyArtifact[] artifacts;
        public ArchaeologyTerrain earth;public Transform[] benches;public BoxCollider[] receptions;public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public Animator[] workers;public ArchaeologyStaff staff;
        public bool Gallery {get;private set;}
        public ArchaeologyRules Rules {get;private set;}public float Clock {get;private set;}public int ActualScans {get;private set;}public int ToolStrokes {get;private set;}
        public int PhysicalLoads {get;private set;}public int PhysicalUnloads {get;private set;}public int[] Targets {get;private set;}=Enumerable.Repeat(-1,4).ToArray();
        public ArchaeologyTool[] Tools {get;private set;}=new ArchaeologyTool[4];
        readonly int[] acceptedCuts=new int[4];public IReadOnlyList<int> AcceptedCuts=>acceptedCuts;
        public double LastCutMilliseconds{get;private set;}public double TotalCutMilliseconds{get;private set;}public double MaximumCutMilliseconds{get;private set;}
        // Actual successful supported-cut receipt: owner, artifact, tool, center,
        // radius, opacity, simulation clock, vendor edit ordinal. Read-only.
        public event Action<int,int,int,Vector3,float,float,float,int> CutAccepted;
        readonly float[] heldAge=new float[4],rest=new float[4];readonly bool[] previousHeld=new bool[4],unloading=new bool[4];
        float nextStroke,nextExposure,finishAge;int cursor;bool terrainResetPending;
        public override string Title=>"TRUCK ARCHAEOLOGY";public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;public override int OptionCount=>2;
        public override string OptionLabel=>option==0?"COOPERATIVE EXHIBIT":"PRESERVATION CUP";
        public override string Help=>"DIG SITE DISASTER\nScan a marked plot to locate buried fossils and artifacts. Their real models stay underground until actual Digger PRO terrain is removed. Drive within boom reach and stop before operating. Tap your assigned ACTION to cycle SCANNER, EXCAVATOR, DRILL, BLOWER, BRUSH and LIFT; hold ACTION to operate. The TOOL buttons provide the same choice for each assigned owner. Large tools remove soil faster but damage nearby artifacts. Use the scanner height and exposure readout to switch to blower/precision brush near a find. All surrounding terrain must physically clear the real artifact before the hoist accepts it. The real crane lifts and places it on your truck's cargo deck; after physical rest it is secured. Drive slowly into your numbered research reception. Actual reception scores the preserved value once, then the crane physically unloads it onto an exhibit bench. No score from scanning, digging or a loose dropped artifact. Each equal plot contains three finds; solo and up to four assigned people/AI use full original NWH driving. In Preservation Cup each crew owns its plot; Cooperative Exhibit shares active plots and total exhibit value. Rematch uses Digger's actual terrain reset for identical fresh conditions. Pause lets the already-issued cutter frame finish, then freezes terrain and the world. Workshop save/load uses the standard shared save menu; Digger disk persistence requires a player build and is deliberately unavailable in Editor.";
        public override bool SupportsSplitAIViews=>true;public override bool CompactBroadcastStatus=>true;public override bool CompactViewportHud=>true;public override int CompactScoreColumns=>4;
        public override bool MultiViewHUD=>drivingViews!=null&&!drivingViews.NeedsArena;
        public override string Header=>Gallery?"RECOVERED EXHIBIT GALLERY":"DIG SITE | "+Clock.ToString("0")+"s | "+(Rules==null?0:Rules.Delivered)+"/"+(setup.count*3)+" RECOVERED";
        public override string Status=>Rules==null?"RESEARCH CREW READY":"PRESERVED EXHIBIT VALUE "+Rules.Scores.Sum()+" | "+earth.ActualEdits+" ACTUAL TERRAIN EDITS";
        public override string Standings=>Rules==null?"":string.Join("\n",Enumerable.Range(0,setup.count).Select(i=>setup.players[i].name+" | "+Rules.Scores[i]+" VALUE"));
        public override string Results=>Rules==null?"":"EXHIBIT COMPLETE | "+Rules.Scores.Sum()+" PRESERVED VALUE\n"+Standings;
        public override bool TryReadScoreCard(int card,out string value,out string detail){if(card<0||card>=setup.count){value=detail=null;return false;}value=Rules==null?"0":Rules.Scores[card].ToString();detail=Rules==null?"READY":Tools[card].ToString().ToUpperInvariant();return true;}
        public override bool TryReadViewportCard(int view,out int participant,out string score,out string detail)=>TryReadDrivingViewportCard(drivingViews,view,out participant,out score,out detail);
        int ViewOwner(int view)=>drivingViews!=null&&view>=0&&view<drivingViews.rally.owners.Length?drivingViews.rally.owners[view]:-1;
        public override string ViewportContextPrompt(int view){int i=ViewOwner(view);return i>=0&&i<setup.count?"TOOL "+Tools[i].ToString().ToUpperInvariant():"";}
        public override string ViewportTelemetry(int view){int i=ViewOwner(view);if(i<0||i>=setup.count||Targets[i]<0||Rules==null)return "SCAN A MARKED PLOT";int a=Targets[i];return "EXPOSED "+(artifacts[a].Exposure*100).ToString("0")+"% | INTEGRITY "+(Rules.Preservation[a]*100).ToString("0")+"% | "+Rules.Stages[a];}
        public override bool BroadcastParticipantActive(int slot)=>slot>=0&&slot<setup.count;
        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){if(Rules==null)return;int owner=Enumerable.Range(0,setup.count).Where(i=>Targets[i]>=0).OrderByDescending(i=>Rules.Stages[Targets[i]]).DefaultIfEmpty(0).First();action.primary=owner;action.preferHuman=false;action.reason="REAL EXCAVATION / PRESERVATION";if(Targets[owner]>=0)action.Objective(artifacts[Targets[owner]].body.position,4);else action.Objective(Plot(owner),15);}
        public Vector3 Plot(int owner)=>new Vector3(-54+owner*36,0,5);
        public Vector3 Park(int owner)=>Plot(owner)+new Vector3(0,.7f,-23);
        public Vector3 BenchSlot(int owner,int artifact)=>benches[owner].position+new Vector3((artifact%3-1)*4,artifacts[artifact].shape.size.y*.5f+.25f,0);
        protected override void BeginGame()
        {
            if(trucks.Length!=4||equipment.Length!=4||artifacts.Length!=12||receptions.Length!=4||earth==null||!earth.Ready)throw new InvalidOperationException("Actual Digger PRO and complete four-plot physical venue required.");
            ResetAiWells();Array.Clear(acceptedCuts,0,acceptedCuts.Length);LastCutMilliseconds=TotalCutMilliseconds=MaximumCutMilliseconds=0;Rules=new ArchaeologyRules(setup.count,option==0);Gallery=false;RestoredSourceMatchId="";Clock=nextStroke=nextExposure=finishAge=0;ActualScans=ToolStrokes=PhysicalLoads=PhysicalUnloads=cursor=0;earth.SetPaused(false);terrainResetPending=!earth.Quiescent;if(!terrainResetPending)earth.Fresh(MatchId,pendingRestore==null);
            for(int i=0;i<4;i++){trucks[i].gameObject.SetActive(i<setup.count);heldAge[i]=rest[i]=0;previousHeld[i]=unloading[i]=false;Tools[i]=ArchaeologyTool.Scanner;Targets[i]=-1;if(i>=setup.count)continue;trucks[i].tractorMotor.Configure();trucks[i].Prepare(Park(i),Quaternion.identity);equipment[i].ResetEquipment();}
            foreach(var artifact in artifacts)artifact.ResetArtifact();if(drivingViews!=null)drivingViews.Layout();ConsumeActions();
            if(!terrainResetPending)ApplyPendingWorkshop();
        }
        void ApplyPendingWorkshop(){if(pendingRestore==null)return;string restore=pendingRestore;pendingRestore=null;applyingStart=true;try{if(!TryRestoreWorkshopJson(restore,out string reason))throw new InvalidOperationException(reason);}finally{applyingStart=false;}}
        public bool SelectTool(int owner,ArchaeologyTool tool){if(!Live||owner<0||owner>=setup.count||!Enum.IsDefined(typeof(ArchaeologyTool),tool)||equipment[owner].hoist.Stage!=JengaCrane.MotorStage.Idle)return false;Tools[owner]=tool;return true;}
        public override void ExtraAction(string key){if(key=="drivingview"&&drivingViews!=null){drivingViews.Cycle();return;}if(key.StartsWith("arch.tool.")&&int.TryParse(key.Substring(10),out int i)&&i>=0&&i<setup.count&&setup.players[i].source!=BowlingSource.AI)SelectTool(i,(ArchaeologyTool)(((int)Tools[i]+1)%6));}
        protected override void Simulate(float dt)
        {
            if(terrainResetPending)
            {
                foreach(var truck in trucks.Take(setup.count))ParkWithNwh(truck);
                if(earth.Quiescent){earth.Fresh(MatchId,pendingRestore==null);terrainResetPending=false;ApplyPendingWorkshop();}
                return;
            }
            if(Gallery)return;Clock+=dt;
            if(Clock>=nextExposure){nextExposure=Clock+.25f;foreach(var artifact in artifacts.Take(setup.count*3))if(Rules.Stages[artifact.index]>=ArtifactStage.Scanned&&Rules.Stages[artifact.index]<=ArtifactStage.Exposed)artifact.ObserveExposure();}
            bool[] operate=new bool[4];
            for(int i=0;i<setup.count;i++)
            {
                var truck=trucks[i];bool ai=setup.players[i].source==BowlingSource.AI;bool held=ActionHeld(i);Vector2 drive=commands[i];
                if(ai){drive=Ai(i,out bool work);operate[i]=work;}else{if(held)heldAge[i]+=dt;else if(previousHeld[i]){if(heldAge[i]<=.22f)SelectTool(i,(ArchaeologyTool)(((int)Tools[i]+1)%6));heldAge[i]=0;}if(actions[i]&&!held&&!previousHeld[i])SelectTool(i,(ArchaeologyTool)(((int)Tools[i]+1)%6));operate[i]=held&&heldAge[i]>.22f;previousHeld[i]=held;}
                var hoist=equipment[i].hoist;
                if(hoist.Stage!=JengaCrane.MotorStage.Idle)
                {
                    drive=new Vector2(0,-1);hoist.Step(dt,6);int a=Targets[i];
                    if(a>=0&&hoist.Released)
                    {
                        bool genuine=unloading[i]?hoist.AtRest(BenchSlot(i,a),artifacts[a].shape.size.y):artifacts[a].OnRack(i);
                        rest[i]=genuine?rest[i]+dt:0;
                        if(rest[i]>.6f)
                        {
                            if(unloading[i]){PhysicalUnloads++;unloading[i]=false;artifacts[a].body.isKinematic=true;Targets[i]=-1;}
                            else if(Rules.Load(a,i,true)){artifacts[a].SecureCargo(i);PhysicalLoads++;}
                            hoist.Complete();rest[i]=0;
                        }
                    }
                }
                else if(Targets[i]>=0&&Rules.Stages[Targets[i]]==ArtifactStage.Cargo)
                {
                    int a=Targets[i];var artifact=artifacts[a];
                    if(artifact.CargoJoint==null){Rules.DropCargo(a,i,true);Tools[i]=ArchaeologyTool.Lift;rest[i]=0;ParkWithNwh(truck);continue;}
                    bool received=receptions[i].bounds.Contains(artifact.body.position)&&Vector3.Distance(truck.body.position,receptions[i].bounds.center)<7&&truck.body.linearVelocity.magnitude<.8f&&artifact.CargoJoint!=null;
                    rest[i]=received?rest[i]+dt:0;
                    if(rest[i]>.5f)
                    {
                        int points=Rules.Deliver(a,i,true,artifact.value);
                        if(points>=0){if(Rules.Cooperative)ConfirmTeamScore("arch.delivery."+a,"research-crew","RESEARCH CREW",Color.cyan,points,Rules.Scores.Sum(),"PHYSICALLY RECEIVED / "+(Rules.Preservation[a]*100).ToString("0")+"% PRESERVED",SportsScoreTier.Major,"VALUE",roundEnded:true);else ConfirmScore("arch.delivery."+a,i,points,Rules.Scores[i],"PHYSICALLY RECEIVED ARTIFACT",SportsScoreTier.Major,"VALUE",roundEnded:true);artifact.ReleaseCargo();unloading[i]=true;Tools[i]=ArchaeologyTool.Lift;if(!hoist.Begin(artifact.body,BenchSlot(i,a),Quaternion.identity,artifact.shape.size.y))throw new InvalidOperationException("Actual received artifact must begin physical unloading.");rest[i]=0;if(staff!=null)staff.React(Clock);}
                    }
                }
                if(hoist.Stage!=JengaCrane.MotorStage.Idle||ai&&Mathf.Abs(drive.x)<.001f&&drive.y<=-.99f)ParkWithNwh(truck);else truck.TickDrive(drive);
                // Soil clearance first triggers a physical head withdrawal while the
                // original buried body is still held. Never sweep the solid head
                // diagonally through the newly released artifact.
                int preparing=Targets[i];
                if(hoist.Stage==JengaCrane.MotorStage.Idle&&preparing>=0&&artifacts[preparing].Clearance&&Rules.Stages[preparing]<=ArtifactStage.Exposed)
                    equipment[i].StepWithdrawal(artifacts[preparing],dt);
                else if(hoist.Stage==JengaCrane.MotorStage.Idle&&Tools[i]!=ArchaeologyTool.Lift)
                {
                    Vector3 point=Targets[i]>=0?(ai?AiDigPoint(i,artifacts[Targets[i]]):CutPoint(artifacts[Targets[i]])):Plot(i)+Vector3.up*7;if((Targets[i]<0||artifacts[Targets[i]].HasWorkPoint)&&float.IsFinite(point.x)&&float.IsFinite(point.y)&&float.IsFinite(point.z)&&(Targets[i]<0||!ai||ValidAiDigPoint(i,artifacts[Targets[i]],point)))equipment[i].Reach(point,dt);
                }
            }
            if(Clock>=nextStroke&&Clock>2)
            {
                nextStroke=Clock+.2f;int first=cursor;cursor=(first+1)%setup.count;
                // Each assigned crew retains the original SOLO .2s cooldown.
                // One bounded serial vendor authority, at most one call per
                // owner/pass and four total. Rotate who gets first access;
                // a busy/refused Cut does not become a stroke or receipt.
                for(int n=0;n<setup.count;n++){int i=(first+n)%setup.count;if(operate[i])Operate(i);}
            }
            bool done=Rules.Complete&&equipment.Take(setup.count).All(e=>e.hoist.Stage==JengaCrane.MotorStage.Idle);
            finishAge=done?finishAge+dt:0;if(finishAge>2){EndMatch();}
        }
        // Keep the original buried-body hold only until its operating heads have
        // physically left the load. Soil/collider clearance is still authoritative.
        public bool HeadClearForRelease(ArchaeologyArtifact item)
        {
            bool assigned=false;
            for(int i=0;i<setup.count;i++)
            {
                var eq=equipment[i];
                if(Targets[i]==item.index){assigned=true;if(!eq.WithdrawalReady(item))return false;}
                if(eq.HeadOverlaps(item.shape,out _,out _))return false;
            }
            return assigned;
        }
        // The actual dense terrain/collider work plan refreshes at the existing
        // .25s exposure cadence. AI and Reach read it without fresh ray arrays.
        Vector3 CutPoint(ArchaeologyArtifact artifact)=>artifact.WorkPoint;
        public bool Operate(int owner)
        {
            if(!Live||owner<0||owner>=setup.count||trucks[owner].body.linearVelocity.magnitude>.8f||equipment[owner].hoist.Stage!=JengaCrane.MotorStage.Idle)return false;
            if(Tools[owner]==ArchaeologyTool.Scanner)
            {
                bool found=false;foreach(var artifact in artifacts.Take(setup.count*3))if(Vector3.Distance(trucks[owner].body.position,artifact.seedPosition)<28&&Rules.Scan(artifact.index,owner,true)){found=true;ActualScans++;artifact.ObserveExposure();}
                if(found||Targets[owner]<0)Targets[owner]=Enumerable.Range(0,setup.count*3).Where(a=>Rules.CanWork(a,owner)&&Rules.Stages[a]>=ArtifactStage.Scanned&&Rules.Stages[a]<ArtifactStage.Lifting).OrderBy(a=>Vector3.SqrMagnitude(artifacts[a].body.position-trucks[owner].body.position)).DefaultIfEmpty(-1).First();return found;
            }
            int id=Targets[owner];if(id<0||!Rules.CanWork(id,owner)||Rules.Stages[id]>=ArtifactStage.Lifting)return false;var item=artifacts[id];
            if(Vector3.Distance(trucks[owner].body.position,item.body.position)>28)return false;
            if(Tools[owner]==ArchaeologyTool.Lift)
            {
                item.ObserveExposure();if(!item.Clearance||!equipment[owner].WithdrawalReady(item)||item.body.isKinematic||item.body.linearVelocity.magnitude>.7f||!Rules.Lift(id,owner,true))return false;
                Vector3 target=equipment[owner].rack.transform.TransformPoint(equipment[owner].rack.center)+Vector3.up*(equipment[owner].rack.bounds.extents.y+item.shape.size.y*.5f+.06f);
                return equipment[owner].hoist.Begin(item.body,target,Quaternion.identity,item.shape.size.y);
            }
            if(item.Clearance||!item.HasWorkPoint||!equipment[owner].AtWork)return false;
            float radius=Tools[owner]==ArchaeologyTool.Excavator?2.4f:Tools[owner]==ArchaeologyTool.Drill?1.25f:Tools[owner]==ArchaeologyTool.Blower?1.05f:.6f;
            float opacity=Tools[owner]==ArchaeologyTool.Excavator?.75f:Tools[owner]==ArchaeologyTool.Drill?.85f:Tools[owner]==ArchaeologyTool.Blower?.3f:.5f;
            Vector3 point=equipment[owner].head.position;Vector3 plot=Plot(id/3);if(Mathf.Abs(point.x-plot.x)>13||Mathf.Abs(point.z-plot.z)>13||point.y<-.5f||point.y>12)return false;
            long cutStarted=System.Diagnostics.Stopwatch.GetTimestamp();
            if(!earth.Cut(point,radius,opacity))return false;
            LastCutMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-cutStarted)*1000d/System.Diagnostics.Stopwatch.Frequency;TotalCutMilliseconds+=LastCutMilliseconds;MaximumCutMilliseconds=Math.Max(MaximumCutMilliseconds,LastCutMilliseconds);ToolStrokes++;acceptedCuts[owner]++;
            foreach(var artifact in artifacts.Take(setup.count*3))if(Vector3.Distance(artifact.shape.ClosestPoint(point),point)<radius+.05f)Rules.Damage(artifact.index,ArchaeologyRules.ToolLoss(Tools[owner])*opacity);
            if(equipment[owner].dust!=null){equipment[owner].dust.transform.position=point;equipment[owner].dust.Play(true);}
            CutAccepted?.Invoke(owner,id,(int)Tools[owner],point,radius,opacity,Clock,earth.ActualEdits);return true;
        }
        Vector2 Ai(int owner,out bool operate)
        {
            operate=false;int a=Targets[owner];var eq=equipment[owner];
            if(eq.hoist.Stage!=JengaCrane.MotorStage.Idle)return new Vector2(0,-1);
            if(a>=0&&Rules.Stages[a]==ArtifactStage.Cargo)return Drive(trucks[owner],receptions[owner].bounds.center,3);
            if(a<0||Rules.Stages[a]==ArtifactStage.Delivered)
            {
                Targets[owner]=Enumerable.Range(0,setup.count*3).Where(id=>Rules.CanWork(id,owner)&&Rules.Stages[id]>=ArtifactStage.Scanned&&Rules.Stages[id]<ArtifactStage.Delivered&&!Targets.Where((t,i)=>i!=owner).Contains(id)).OrderBy(id=>Mathf.Abs(id/3-owner)).DefaultIfEmpty(-1).First();a=Targets[owner];
            }
            if(a<0)
            {
                // Public marked plots guide the scanner; AI does not aim a digging tool at an undiscovered artifact.
                int plot=Enumerable.Range(0,setup.count).Where(p=>Rules.Cooperative||p==owner).Where(p=>Enumerable.Range(p*3,3).Any(id=>Rules.Stages[id]==ArtifactStage.Buried)).OrderBy(p=>Mathf.Abs(p-owner)).DefaultIfEmpty(-1).First();
                if(plot<0)return new Vector2(0,-1);Tools[owner]=ArchaeologyTool.Scanner;Vector3 marked=Park(plot);operate=Vector3.ProjectOnPlane(trucks[owner].body.position-marked,Vector3.up).magnitude<2.5f&&trucks[owner].body.linearVelocity.magnitude<.7f;return Drive(trucks[owner],marked,4);
            }
            Vector3 park=Park(a/3);Vector2 drive=Drive(trucks[owner],park,4);bool near=Vector3.ProjectOnPlane(trucks[owner].body.position-park,Vector3.up).magnitude<2.5f&&trucks[owner].body.linearVelocity.magnitude<.7f;
            if(!near)return drive;
            if(Rules.Stages[a]==ArtifactStage.Buried)Tools[owner]=ArchaeologyTool.Scanner;
            else if(Rules.Stages[a]==ArtifactStage.Exposed||artifacts[a].Clearance)Tools[owner]=ArchaeologyTool.Lift;
            else
            {
                if(!artifacts[a].HasWorkPoint)return new Vector2(0,-1);
                Vector3 point=AiDigPoint(owner,artifacts[a]);
                if(!ValidAiDigPoint(owner,artifacts[a],point))return new Vector2(0,-1);
                Tools[owner]=ArchaeologyAiDigPolicy.Choose(KnownDigClearance(owner,point));
            }
            operate=true;return new Vector2(0,-1);
        }
        static void ParkWithNwh(SportsEventTruck truck){truck.TickDrive(Vector2.zero);var states=truck.tractorMotor.vehicle.input.states;states.handbrake=states.handbrakeRaw=1;truck.tractorMotor.vehicle.input.states=states;}
        static Vector2 Drive(SportsEventTruck truck,Vector3 target,float speed)
        {
            Vector3 delta=Vector3.ProjectOnPlane(target-truck.body.position,Vector3.up);if(delta.magnitude<1.5f)return new Vector2(0,-1);
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),delta,Vector3.up);bool reverse=Mathf.Abs(angle)>105;float error=reverse?Mathf.DeltaAngle(180,angle):angle;
            float desired=Mathf.Min(speed,delta.magnitude*.6f),along=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward)*(reverse?-1:1);return new Vector2(Mathf.Clamp(error/30,-1,1)*(reverse?-1:1),Mathf.Clamp((desired-along)*.6f,-.8f,.8f)*(reverse?-1:1));
        }
        protected override void Present(float dt){if(drivingViews!=null)drivingViews.Tick(dt);if(equipment!=null)for(int i=0;i<setup.count;i++)equipment[i].Present(Tools[i],Live,Clock);if(broadcast!=null&&(drivingViews==null||drivingViews.NeedsArena))broadcast.Pose(new Vector3(0,1,-5),new Vector3(0,90,-110),60);}
        protected override void SetPaused(bool paused){if(earth!=null)earth.SetPaused(paused);}
        protected override void StopGame(){if(earth!=null)earth.SetPaused(false);if(equipment!=null)foreach(var eq in equipment)if(eq!=null)eq.StopEquipment();if(artifacts!=null)foreach(var artifact in artifacts)if(artifact!=null)artifact.ReleaseCargo();if(trucks!=null)foreach(var truck in trucks)if(truck!=null)truck.Power(false);}
        protected override void StopSounds(){if(trucks!=null)foreach(var truck in trucks)if(truck!=null)truck.StopSound();}
    }
}
