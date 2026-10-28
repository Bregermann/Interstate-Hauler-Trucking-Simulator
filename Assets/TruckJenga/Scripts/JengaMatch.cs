using System;
using System.Linq;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckJenga
{
    public sealed class JengaMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks=Array.Empty<SportsEventTruck>();public Rigidbody[] containers=Array.Empty<Rigidbody>();public BoxCollider[] shapes=Array.Empty<BoxCollider>();
        public Vector3[] initialPositions=Array.Empty<Vector3>();public Quaternion[] initialRotations=Array.Empty<Quaternion>();
        public GameObject[] selectionControls=Array.Empty<GameObject>();public GameObject attachControl,releaseControl;
        public JengaWinch winch;public JengaCrane crane;public SportsEventCamera broadcast;public SportsDrivingViews drivingViews;
        public Vector3 pieceSize=new Vector3(3.2f,2.4f,9.6f);public Transform selectionMarker;public Animator[] spectators=Array.Empty<Animator>();
        public JengaRules Rules { get; private set; }
        public int Selected { get; private set; }
        public int PullEnd { get; private set; }=1;
        public float Clock { get; private set; }
        public string Fault { get; private set; }="";
        public int PhysicalExtractions { get; private set; }
        public int PhysicalRestacks { get; private set; }
        public int CollapsedPiece { get; private set; }=-1;
        Vector3[] originalPositions;Quaternion[] originalRotations;float age,stable,nav;int foundationTraceMask,operationSamples;float nextOperationTrace;JengaRules.Stage tracePhase;JengaCrane.MotorStage traceCrane;bool reelArmed;Vector3 pullDirection,target;Quaternion targetRotation;
        public override string Title=>"TRUCK JENGA";
        public override string Help=>"CONTAINER CATASTROPHE\nSelect any container below the highest completed layer. LEFT/RIGHT cycles legal pieces; UP/DOWN chooses the pulling end. ACTION stages your full tractor in the marked winch bay and attaches the real cable. Then your assigned driving controls operate NWH normally. Hold ACTION to reel; watch actual rope tension and ease off before the cable breaks. Press ACTION while braking, or RELEASE CABLE in the HUD, to disconnect and reposition; then ACTION reconnects within range. Pull the entire physical container clear without dropping or tipping another tower piece. The overhead crane physically picks up, lifts, rotates and lowers it onto the next top slot. Only a released, actually resting container counts as a completed stack. Turns rotate after the tower settles. Whoever causes a real tower collapse loses; other players survive. Delayed collapse during crane/settling belongs to the last puller. No forced collapse, timer winner or simulated AI extraction. One human may play against AI; up to four assigned humans. Pause/reconnect/rematch retain the standard Sports lifecycle.";
        public override int MinimumPlayers=>2;public override int MaximumPlayers=>4;public override int OptionCount=>1;
        public override string OptionLabel=>"PHYSICAL TOWER · 18 CONTAINERS";
        public override bool SupportsSplitAIViews=>true;public override bool CompactBroadcastStatus=>true;public override bool CompactViewportHud=>true;public override int CompactScoreColumns=>4;
        public override bool MultiViewHUD=>drivingViews!=null&&!drivingViews.NeedsArena;
        public override string Header=>Rules==null?Title:$"TURN {Rules.Turn} · {setup.players[Rules.Owner].name} · {Rules.Phase}";
        public override string Status=>Fault.Length>0?Fault:Rules==null?"":Rules.Phase==JengaRules.Stage.Selecting?$"CONTAINER {Selected+1} · LAYER {Rules.Row(Selected)+1} · END {(PullEnd>0?"A":"B")}":Rules.Phase==JengaRules.Stage.Pulling?$"WINCH {winch.Tension/1000:0.0} kN · {winch.Length:0.0} m · {(winch.Attached?"CONNECTED":"RECONNECT IN RANGE")}":Rules.Phase==JengaRules.Stage.Crane?"CRANE · "+crane.Stage:Rules.Phase==JengaRules.Stage.Complete?"TOWER COLLAPSED":"WAITING FOR REAL PHYSICAL REST";
        public override string Standings=>Rules==null?"":string.Join("\n",Enumerable.Range(0,setup.count).Select(i=>setup.players[i].name+" · "+Rules.Stacks[i]+" STACKS"));
        public override string Results=>Rules==null||Rules.Loser<0?Standings:"TOWER COLLAPSED\n"+setup.players[Rules.Loser].name+" CAUSED THE COLLAPSE\nSURVIVORS: "+string.Join(" / ",Enumerable.Range(0,setup.count).Where(i=>i!=Rules.Loser).Select(i=>setup.players[i].name))+"\n"+Standings;
        public override bool TryReadScoreCard(int card,out string value,out string detail){if(card<0||card>=setup.count){value=detail=null;return false;}value=Rules==null?"0":Rules.Stacks[card].ToString();detail=Rules!=null&&card==Rules.Owner?"OPERATING":"WAITING";return true;}
        public override bool TryReadViewportCard(int view,out int participant,out string score,out string detail)=>TryReadDrivingViewportCard(drivingViews,view,out participant,out score,out detail);
        int ViewportOwner(int view)=>drivingViews!=null&&view>=0&&view<drivingViews.rally.Humans?drivingViews.rally.owners[view]:-1;
        public override string ViewportContextPrompt(int view)=>Rules!=null&&ViewportOwner(view)==Rules.Owner?Status:null;
        public override string ViewportTelemetry(int view)=>Rules!=null&&ViewportOwner(view)==Rules.Owner&&Rules.Phase==JengaRules.Stage.Pulling?$"TENSION {winch.Tension/1000:0.0} kN · {Clock:0.0}s":null;
        public override bool BroadcastParticipantActive(int slot)=>Rules==null?slot>=0&&slot<setup.count:slot==Rules.Owner;
        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){if(Rules==null)return;action.primary=Rules.Owner;action.preferHuman=false;action.reason="PHYSICAL CONTAINER EXTRACTION";if(Selected>=0&&Selected<containers.Length){action.hasObjective=true;action.objective=shapes[Selected].bounds;}}
        public Vector3 Slot(int row,int column){Vector3 p=new Vector3((column-1)*pieceSize.x,(row+.5f)*pieceSize.y,0);return row%2==0?p:new Vector3(0,p.y,p.x);}
        public Quaternion Rotation(int row)=>Quaternion.Euler(0,row%2==0?0:90,0);
        public float TowerTop=>(Rules.Highest+1)*pieceSize.y;
        protected override void BeginGame()
        {
            if(containers.Length!=18||shapes.Length!=18||trucks.Length!=4||crane==null||winch==null)throw new InvalidOperationException("Complete authored Jenga physics and crane required.");
            if(initialPositions.Length!=18||initialRotations.Length!=18)throw new InvalidOperationException("Authored original container reset poses are required.");for(int i=0;i<18;i++)if((initialPositions[i]-Slot(i/3,i%3)).magnitude>.00001f||Quaternion.Angle(initialRotations[i],Rotation(i/3))>.001f)throw new InvalidOperationException("Jenga reset metadata does not match authored tower slot "+i+"; run the explicit guarded editor repair, never pile containers at origin.");if(originalPositions==null){originalPositions=initialPositions.ToArray();originalRotations=initialRotations.ToArray();}
            winch.ResetWinch();crane.ResetCrane();Rules=new JengaRules(setup.count,6);PhysicalExtractions=PhysicalRestacks=0;CollapsedPiece=-1;Clock=age=stable=nav=0;Fault="";
            for(int i=0;i<containers.Length;i++){var b=containers[i];b.isKinematic=false;b.position=originalPositions[i];b.rotation=originalRotations[i];b.linearVelocity=b.angularVelocity=Vector3.zero;b.WakeUp();}
            for(int i=0;i<trucks.Length;i++){trucks[i].gameObject.SetActive(i<setup.count);if(i>=setup.count)continue;trucks[i].tractorMotor.Configure();trucks[i].Prepare(new Vector3(-25+i*7,.7f,-27),Quaternion.identity,false);trucks[i].body.isKinematic=true;trucks[i].body.detectCollisions=true;}
            Selected=0;PullEnd=1;foundationTraceMask=operationSamples=0;nextOperationTrace=0;tracePhase=(JengaRules.Stage)(-1);traceCrane=(JengaCrane.MotorStage)(-1);TraceFoundation("reset");if(drivingViews!=null)drivingViews.Layout();
        }
        public bool SelectPiece(int piece,int end){if(!Live||Rules.Phase!=JengaRules.Stage.Selecting||!Rules.Legal(piece))return false;Selected=piece;PullEnd=end<0?-1:1;return true;}
        public bool BeginExtraction()
        {
            if(!Live||!Rules.BeginPull(Rules.Owner,Selected))return false;pullDirection=containers[Selected].rotation*Vector3.forward*PullEnd;pullDirection=Vector3.ProjectOnPlane(pullDirection,Vector3.up).normalized;
            var truck=trucks[Rules.Owner];truck.Prepare(Vector3.ProjectOnPlane(containers[Selected].position,Vector3.up)+pullDirection*15+Vector3.up*.7f,Quaternion.LookRotation(pullDirection));reelArmed=false;age=stable=0;bool attached=winch.Attach(containers[Selected],truck,new Vector3(0,0,PullEnd*pieceSize.z*.5f));if(attached)feedback.Play(2,0,true);if(!attached)Fault="CABLE OUT OF RANGE · DRIVE CLOSER, THEN ACTION";ConsumeActions();return true;
        }
        void Cycle(int direction){int[] legal=Rules.LegalPieces();if(legal.Length==0)return;int at=Array.IndexOf(legal,Selected);Selected=legal[(Mathf.Max(0,at)+direction+legal.Length)%legal.Length];}
        public override void ExtraAction(string key){if(key=="drivingview"&&drivingViews!=null){drivingViews.Cycle();return;}if(!Live||Rules==null||setup.players[Rules.Owner].source==BowlingSource.AI)return;if(Rules.Phase==JengaRules.Stage.Selecting){if(key=="jenga.previous")Cycle(-1);else if(key=="jenga.next")Cycle(1);else if(key=="jenga.end")PullEnd=-PullEnd;else if(key=="jenga.attach")BeginExtraction();}else if(Rules.Phase==JengaRules.Stage.Pulling){if(key=="jenga.release")winch.Disconnect();else if(key=="jenga.attach"&&!winch.Attached){if(winch.Attach(containers[Selected],trucks[Rules.Owner],new Vector3(0,0,PullEnd*pieceSize.z*.5f)))Fault="";}}}
        protected override void Simulate(float dt)
        {
            Clock+=dt;age+=dt;nav-=dt;ObserveOperationTrace();
            if(Rules.Phase==JengaRules.Stage.Preparing){if(age>=.5f&&(foundationTraceMask&1)==0){foundationTraceMask|=1;TraceFoundation("settle-0.5");}if(age>=1.5f&&(foundationTraceMask&2)==0){foundationTraceMask|=2;TraceFoundation("settle-1.5");}if(age>=3&&(foundationTraceMask&4)==0){foundationTraceMask|=4;TraceFoundation("settle-3");}if(PhysicalCollapse(out int fallen)){if((foundationTraceMask&8)==0){foundationTraceMask|=8;TraceFoundation("first-fault-piece-"+fallen);}Fault="FOUNDATION UNSTABLE · RESTART REQUIRED";return;}stable=TowerStill()?stable+dt:0;if(stable>1.5f){TraceFoundation("foundation-ready");Rules.FoundationReady();Selected=JengaSelection.Careful(Rules);age=stable=0;}return;}
            if(Rules.Phase==JengaRules.Stage.Complete){if(age>2.8f)EndMatch();return;}
            if(PhysicalCollapse(out int piece)&&Rules.Collapse(true)){TraceFoundation("actual-collapse-piece-"+piece);TraceOperation("collapse");CollapsedPiece=piece;winch.Disconnect();foreach(var truck in trucks)truck.Power(false);ConfirmOutcome("jenga.collapse",Rules.Loser,0,"PHYSICAL TOWER COLLAPSE","LOSS",SportsScoreTier.MatchChampion,matchEnded:true);foreach(var fan in spectators)if(fan!=null)fan.CrossFade("Shout",.15f);age=0;return;}
            int owner=Rules.Owner;bool ai=setup.players[owner].source==BowlingSource.AI;Vector2 input=commands[owner];
            if(Rules.Phase==JengaRules.Stage.Selecting){if(ai){if(age>1.4f){Selected=JengaSelection.Careful(Rules);if(Selected>=0){PullEnd=(Rules.Turn%2)==0?-1:1;BeginExtraction();}}}else{if(nav<=0&&Mathf.Abs(input.x)>.5f){Cycle(input.x>0?1:-1);nav=.23f;}if(nav<=0&&Mathf.Abs(input.y)>.5f){PullEnd=input.y>0?1:-1;nav=.23f;}if(actions[owner])BeginExtraction();}return;}
            if(Rules.Phase==JengaRules.Stage.Pulling){var truck=trucks[owner];if(ai){float error=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),pullDirection,Vector3.up);float speed=Vector3.Dot(truck.body.linearVelocity,pullDirection);float desired=!winch.Attached||winch.Tension>19000?0:1.15f;input=new Vector2(Mathf.Clamp(error/25,-1,1),Mathf.Clamp((desired-speed)*.65f,-.8f,.4f));}
                truck.TickDrive(input);bool released=!ai&&actions[owner]&&input.y<-.6f&&winch.Attached;if(released){winch.Disconnect();reelArmed=false;}bool held=ai?age>1&&winch.Tension<13000:ActionHeld(owner);if(!held)reelArmed=true;if(ai)reelArmed=true;if(!released&&!winch.Attached&&(ai||actions[owner])){if(winch.Attach(containers[Selected],truck,new Vector3(0,0,PullEnd*pieceSize.z*.5f)))Fault="";}winch.Step(held&&reelArmed,dt);
                if(EntirePieceClear(Selected)&&Rules.Extracted(Selected,true)){TraceLiftFootprints();TraceOperation("physical-clearance");PhysicalExtractions++;winch.Disconnect();HoldOperatingTruck(truck);Rules.NextSlot(out int row,out int column);target=Slot(row,column);targetRotation=Rotation(row);if(!crane.Begin(containers[Selected],target,targetRotation,pieceSize.y))throw new InvalidOperationException("Crane acquisition must start once for the real extracted dynamic load.");age=stable=0;TraceOperation("crane-begin");}return;}
            if(Rules.Phase==JengaRules.Stage.Crane){HoldOperatingTruck(trucks[owner]);crane.Step(dt,TowerTop);stable=crane.AtRest(target,pieceSize.y)?stable+dt:0;if(stable>.7f&&Rules.Restacked(Selected,true)){PhysicalRestacks++;ConfirmScore("jenga.stack."+Rules.Turn,owner,1,Rules.Stacks[owner],"PHYSICAL RESTACK",SportsScoreTier.Major,"STACKS",roundEnded:true);crane.Complete();foreach(var fan in spectators)if(fan!=null)fan.CrossFade("Cheer",.15f);feedback.Play(4,0,true);age=stable=0;}return;}
            if(Rules.Phase==JengaRules.Stage.Settling){stable=TowerStill()?stable+dt:0;if(stable>1.5f&&Rules.NextTurn(true)){trucks[owner].Power(false);trucks[owner].body.isKinematic=true;Selected=JengaSelection.Careful(Rules);age=stable=0;ConsumeActions();foreach(var fan in spectators)if(fan!=null)fan.CrossFade("Idle",.2f);}}
        }
        public bool EntirePieceClear(int id){Bounds b=shapes[id].bounds;float extent=Mathf.Abs(pullDirection.x)*b.extents.x+Mathf.Abs(pullDirection.z)*b.extents.z;if(Vector3.Dot(b.center,pullDirection)-extent<pieceSize.z*.5f+.25f)return false;for(int i=0;i<shapes.Length;i++)if(i!=id){Bounds other=shapes[i].bounds;other.Expand(.08f);if(ColumnFootprintsOverlap(b,other))return false;}return true;}
        // A floor-level extracted load must clear the actual tower's entire vertical
        // column before rising. Full 3D separation alone can hide a real overhang.
        public static bool ColumnFootprintsOverlap(Bounds a,Bounds b)=>a.min.x<b.max.x&&a.max.x>b.min.x&&a.min.z<b.max.z&&a.max.z>b.min.z;
        public bool PhysicalCollapse(out int piece){piece=-1;if(Rules==null)return false;for(int i=0;i<containers.Length;i++){if(i==Rules.Selected&&(Rules.Phase==JengaRules.Stage.Pulling||Rules.Phase==JengaRules.Stage.Crane))continue;var b=containers[i];float expected=Slot(Rules.Row(i),Rules.Column(i)).y;if((Vector3.Dot(b.transform.up,Vector3.up)<.82f&&b.linearVelocity.y<-.5f)||b.position.y<expected-pieceSize.y*.38f){piece=i;return true;}}return false;}
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void TraceLiftFootprints()
        {
            Bounds load=shapes[Selected].bounds;
            for(int i=0;i<shapes.Length;i++)if(i!=Selected){Bounds other=shapes[i].bounds;other.Expand(.08f);Debug.Log($"JENGA_LIFT_CLEARANCE clock={Clock:R} load={Selected} other={i} loadMin={load.min.ToString("F6")} loadMax={load.max.ToString("F6")} otherMin={other.min.ToString("F6")} otherMax={other.max.ToString("F6")} projectedOverlap={ColumnFootprintsOverlap(load,other)} full3DOverlap={load.Intersects(other)}");}
        }
        static void HoldOperatingTruck(SportsEventTruck truck)
        {
            // Auto-DNR intentionally turns sustained service-brake input into reverse
            // propulsion. A stationary crane operator uses NWH's real parking brake.
            truck.TickDrive(Vector2.zero);
            var states=truck.tractorMotor.vehicle.input.states;states.handbrake=states.handbrakeRaw=1;
            truck.tractorMotor.vehicle.input.states=states;
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void ObserveOperationTrace()
        {
            if(operationSamples>=240)return;
            if(tracePhase==Rules.Phase&&traceCrane==crane.Stage&&Clock<nextOperationTrace)return;
            tracePhase=Rules.Phase;traceCrane=crane.Stage;nextOperationTrace=Clock+(Clock<35?.5f:2);operationSamples++;TraceOperation("sample");
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void TraceOperation(string reason)
        {
            var selected=Selected>=0&&Selected<containers.Length?containers[Selected]:null;var load=crane.Load;
            string p=selected!=null?selected.position.ToString("F6"):"none",v=selected!=null?selected.linearVelocity.ToString("F6"):"none";
            string loadPosition=load!=null?load.position.ToString("F6"):"none",loadVelocity=load!=null?load.linearVelocity.ToString("F6"):"none";
            float anchorGap=load!=null?Vector3.Distance(load.transform.TransformPoint(Vector3.up*(pieceSize.y*.5f)),crane.spreader.position):-1;
            Debug.Log($"JENGA_OPERATION why={reason} clock={Clock:R} frame={Time.frameCount} phase={Rules.Phase} turn={Rules.Turn} owner={Rules.Owner} selected={Selected} end={PullEnd} selectionNav={nav:R} p={p} v={v} up={(selected!=null?Vector3.Dot(selected.transform.up,Vector3.up):-1):R} truck={trucks[Rules.Owner].body.position.ToString("F6")} truckV={trucks[Rules.Owner].body.linearVelocity.ToString("F6")} truckCmd={trucks[Rules.Owner].command} rope={winch.Attached} length={winch.Length:R} force={winch.Tension:R} crane={crane.Stage} spreader={crane.spreader.position.ToString("F6")} load={loadPosition} loadV={loadVelocity} loadDynamic={(load!=null&&!load.isKinematic)} grip={(load!=null&&load.GetComponent<FixedJoint>()!=null)} anchorGap={anchorGap:R} pickups={crane.Pickups} releases={crane.Releases} extracted={PhysicalExtractions} restacked={PhysicalRestacks}");
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void TraceFoundation(string phase)
        {
            Debug.Log($"JENGA_FOUNDATION phase={phase} clock={Clock:R} stable={stable:R} frame={Time.frameCount}");
            for(int i=0;i<containers.Length;i++)
            {
                var b=containers[i];var box=shapes[i];int overlaps=0;float deepest=0;
                for(int j=0;j<i;j++)if(Physics.ComputePenetration(box,b.position,b.rotation,shapes[j],containers[j].position,containers[j].rotation,out _,out float depth)&&depth>.0001f){overlaps++;deepest=Mathf.Max(deepest,depth);}
                Bounds bounds=box.bounds;var supports=Physics.RaycastAll(bounds.center,Vector3.down,bounds.extents.y+.15f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.attachedRigidbody!=b).Select(h=>h.collider.name).Take(3);
                Debug.Log($"JENGA_FOUNDATION_BODY phase={phase} piece={i} body={b.GetInstanceID()} p={b.position.ToString("F6")} transform={b.transform.position.ToString("F6")} q={b.rotation.ToString("F6")} v={b.linearVelocity.ToString("F6")} w={b.angularVelocity.ToString("F6")} expectedY={Slot(Rules.Row(i),Rules.Column(i)).y:R} up={Vector3.Dot(b.transform.up,Vector3.up):R} bounds={bounds} overlapCount={overlaps} deepest={deepest:R} support={string.Join("|",supports)} kinematic={b.isKinematic}");
            }
        }
        bool TowerStill(){for(int i=0;i<containers.Length;i++)if(containers[i].linearVelocity.magnitude>.25f||containers[i].angularVelocity.magnitude>.18f)return false;return true;}
        protected override void Present(float dt){bool selecting=Live&&Rules!=null&&Rules.Phase==JengaRules.Stage.Selecting,pulling=Live&&Rules!=null&&Rules.Phase==JengaRules.Stage.Pulling;foreach(var control in selectionControls)if(control!=null)control.SetActive(selecting);if(attachControl!=null)attachControl.SetActive(selecting||pulling&&!winch.Attached);if(releaseControl!=null)releaseControl.SetActive(pulling&&winch.Attached);if(drivingViews!=null)drivingViews.Tick(dt);if(winch!=null)winch.Draw();if(crane!=null)crane.Present();if(selectionMarker!=null&&Rules!=null){selectionMarker.gameObject.SetActive(!Finished&&Rules.Phase==JengaRules.Stage.Selecting&&Selected>=0);if(Selected>=0)selectionMarker.SetPositionAndRotation(containers[Selected].position,containers[Selected].rotation);}if(broadcast!=null&&(drivingViews==null||drivingViews.NeedsArena)){Vector3 focus=Rules==null?Vector3.up*6:containers[Mathf.Max(0,Selected)].position;broadcast.Pose(focus,new Vector3(24,20,-30),58);}foreach(var fan in spectators)if(fan!=null)fan.speed=Live?1:0;}
        protected override void StopGame(){if(winch!=null)winch.Disconnect();if(crane!=null)crane.StopCrane();foreach(var t in trucks)if(t!=null)t.Power(false);}
        protected override void StopSounds(){foreach(var t in trucks)if(t!=null)t.StopSound();}
    }
}
