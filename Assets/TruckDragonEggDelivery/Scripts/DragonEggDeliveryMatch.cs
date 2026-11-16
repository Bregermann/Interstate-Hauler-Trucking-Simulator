using System;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonEggDelivery
{
    public sealed partial class DragonEggDeliveryMatch : DragonKingdomGroundMatch
    {
        public DragonEggCarrier[] carriers = Array.Empty<DragonEggCarrier>();
        public DragonEggNest[] nests = Array.Empty<DragonEggNest>();
        public Transform[] motherPerches = Array.Empty<Transform>();
        public DragonEggDeliveryRules Rules { get; private set; }
        public int PhysicalDeliveries { get; private set; }
        public int BabyDeliveries { get; private set; }
        public int LastDeliveredSlot { get; private set; } = -1;
        readonly float[] disturbedUntil = new float[4], releaseAge = new float[4], motherActionAt = new float[6];
        readonly int[] motherPatrol = new int[6];
        readonly int[] motherLandingBaseline = new int[6];
        readonly bool[] motherLandingPending = new bool[6];
        readonly bool[] bayParking=new bool[4],bayReverse=new bool[4],bayMoveBlocked=new bool[4],bayReverseObserved=new bool[4];
        readonly float[] bayNextCheck=new float[4];
        readonly Vector3[] bayCheckedGoal=new Vector3[4];
        readonly int[] bayChecks=new int[4];
        readonly string[] bayStatus=new string[4];
        readonly RaycastHit[] bayGroundHits=new RaycastHit[32],bayBodyHits=new RaycastHit[32];
        readonly Collider[] bayBodies=new Collider[64];
        readonly Collider[] bayAttachedLoads=new Collider[4];
        const float FinalBayRange=65, CorridorStep=2;
        public bool UsingBayParking(int slot)=>slot>=0&&slot<4&&bayParking[slot];
        public bool BayParkingReverseObserved(int slot)=>slot>=0&&slot<4&&bayReverseObserved[slot];
        public int BayCorridorChecks(int slot)=>slot>=0&&slot<4?bayChecks[slot]:0;
        public string BayParkingStatus(int slot)=>slot>=0&&slot<4?bayStatus[slot]:"INVALID SLOT";
        float resultsAt = -1;
        readonly StringBuilder text = new StringBuilder(512);
        public override string Title => "TRUCK DRAGON EGG DELIVERY";
        public override string OptionLabel => option == 0 ? "GENTLE BROOD" : option == 1 ? "RESTLESS BROOD" : "EARLY HATCHLINGS";
        public override string Help => "Carry your real dragon egg through the shared kingdom to its marked nest. ACTION held gently stabilizes the finite cradle restraints and comforts a hatched baby; it cannot erase impacts. Drive slowly over rough ground. Cracks lower condition; a hatching egg becomes the same physical cargo with an animated baby. If cargo comes loose, stop within26m and press ACTION to use the physical recovery hoist. At your marked nest loading bay, stop and press ACTION to unload by crane; AI uses the same equipment. Only the correct nest with the actual settled egg or baby scores. Delivery value combines condition, time and gentle handling; an absent cargo never scores. Protective mothers patrol and inspect; actual rough handling can provoke a temporary response. A full journey has15minutes. Customized trucks, shared inputs, weather, pause, results and rematches retain their original owners.";
        public override string Header => Title + " · " + PhysicalDeliveries + " / " + setup.count + " SAFE · " + (Rules == null ? "READY" : Math.Ceiling(Math.Max(0, Rules.TimeLimit - Rules.Clock)) + "s");
        public override string Status => Intro > 0 ? "BROOD CHECK · " + Mathf.CeilToInt(Intro) : Announcement;
        public override string Standings
        {
            get { text.Clear(); for (int i = 0; i < setup.count; i++) { TryReadScoreCard(i, out var value, out var detail); text.Append(setup.players[i].name).Append(" · ").Append(value).Append(" · ").Append(detail).AppendLine(); } return text.ToString(); }
        }
        public override string Results
        {
            get
            {
                if (Rules == null) return "DELIVER THE ACTUAL EGG OR HATCHED BABY TO ITS CORRECT NEST.";
                text.Clear(); var winners = Rules?.Winners() ?? Array.Empty<int>();
                text.Append(winners.Length == 0 ? "NO SAFE DELIVERIES" : winners.Length > 1 ? "SHARED BROOD HONORS" : setup.players[winners[0]].name + " · BROOD CHAMPION").AppendLine();
                for (int i = 0; i < Math.Min(setup.count, Rules.Count); i++)
                {
                    var record = Rules.Cargo[i]; text.Append(setup.players[i].name).Append(" · ").Append(record.Score).Append(" PTS · ")
                        .Append(record.Stage == DragonEggStage.Delivered ? record.DeliveredBaby ? "BABY SAFE" : "EGG SAFE" : "CARGO NOT DELIVERED")
                        .Append(" · ").Append(Math.Round(record.Condition * 100)).Append("% · ").Append(record.RoughImpacts).Append(" HARD IMPACTS");
                    if (record.Stage == DragonEggStage.Delivered) text.Append(" · ").Append(record.DeliverySeconds.ToString("F1")).Append("s"); text.AppendLine();
                }
                return text.ToString();
            }
        }
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = "0 PTS"; detail = "READY TO LOAD";
            if (slot < 0 || slot >= setup.count) return false;
            if (Rules == null || slot >= Rules.Count) return true;
            var cargo = Rules.Cargo[slot]; value = cargo.Score + " PTS";
            detail = cargo.Stage == DragonEggStage.Delivered ? cargo.DeliveredBaby ? "BABY DELIVERED" : "EGG DELIVERED" : cargo.Stage == DragonEggStage.Lost ? "CARGO LOST" :
                (cargo.Stage == DragonEggStage.Baby ? "BABY" : cargo.Stage.ToString().ToUpperInvariant()) + " · " + Math.Round(cargo.Condition * 100) + "%";
            return true;
        }
        public override float ParticipantProgress(int slot)
        {
            if (Rules == null || slot < 0 || slot >= Rules.Count) return -1;
            return Rules.Cargo[slot].Stage == DragonEggStage.Delivered ? 1 : Rules.Cargo[slot].Stage == DragonEggStage.Lost ? 0 : .25f + (float)Rules.Cargo[slot].Condition * .5f;
        }
        protected override void PrepareObjectives()
        {
            if (carriers.Length != 4 || nests.Length != 4 || motherPerches.Length < dragons.Length)
                throw new InvalidOperationException("Four real carriers/nests and an actual mother perch per actor are required.");
            if (!species.TryGet("dragon-spark", out var baby) || !baby.Supports(DragonSpeciesRole.Baby) || !baby.HasAnimation(DragonAnimationAction.Hatch))
                throw new InvalidOperationException("The real animation-compatible baby/Hatch source is required; an idle replacement is not accepted.");
            startingRegionId = "castle";
            var ids = new string[4]; for (int i = 0; i < 4; i++) { ids[i] = nests[i].id; carriers[i].Stop(); }
            Rules = new DragonEggDeliveryRules(setup.count, MatchSeed, option, ids);
            PhysicalDeliveries = BabyDeliveries = 0; LastDeliveredSlot = -1;
            resultsAt = -1;
            Array.Clear(disturbedUntil, 0, 4); Array.Clear(releaseAge, 0, 4); Array.Clear(motherActionAt, 0, 6); Array.Clear(motherPatrol, 0, 6);
            Array.Clear(motherLandingBaseline,0,6);Array.Clear(motherLandingPending,0,6);
            Array.Clear(bayParking,0,4);Array.Clear(bayReverse,0,4);Array.Clear(bayMoveBlocked,0,4);Array.Clear(bayReverseObserved,0,4);Array.Clear(bayNextCheck,0,4);Array.Clear(bayChecks,0,4);Array.Clear(bayCheckedGoal,0,4);Array.Clear(bayStatus,0,4);
            ResetBayYields();
            Announcement = "KEEP THE EGGS STEADY · FOLLOW YOUR MARKED NEST";
        }
        protected override void BeginGame()
        {
            base.BeginGame();
            // Base has now prepared the actual rotated NWH starting poses.
            // This is initial physical spawn, never a mid-journey cargo reset.
            for (int i = 0; i < 4; i++)
            { carriers[i].equipment.gameObject.SetActive(i < setup.count); carriers[i].ResetCarrier(); carriers[i].cargo.ResetCargo(carriers[i], i < setup.count); }
        }
        protected override void BeginDragonIntents()
        {
            for (int i = 0; i < dragons.Length; i++)
            {
                if (!species.TryGet(dragonSpeciesIds[i], out var definition) || !definition.Supports(DragonSpeciesRole.EggGuardian))
                    throw new InvalidOperationException("Mother actors require actual nest/inspection/ground/flight capability.");
                dragons[i].SetIntent(DragonCharacterIntent.Patrol, PatrolGoal(i));
            }
        }
        protected override void TickObjectives(float dt)
        {
            Rules.Advance(dt);
            for (int i = 0; i < setup.count; i++)
            {
                var carrier = carriers[i]; var cargo = carrier.cargo; var record = Rules.Cargo[i];
                if (record.Terminal)
                {
                    if (record.Stage == DragonEggStage.Lost)
                    {
                        cargo.Tick(dt, false);
                        if (!Retired[i]) { Retired[i] = true; carrier.Stop(); Announcement = setup.players[i].name + " · CARGO LOST"; }
                    }
                    continue;
                }
                bool bot = setup.players[i].source == BowlingSource.AI;
                bool care = bot ? cargo.HandlingAcceleration > 2 || record.Stage == DragonEggStage.Baby : ActionHeld(i);
                cargo.Tick(dt, care); carrier.Tick(dt);
                if (record.Stage == DragonEggStage.Lost) { Retired[i] = true; carrier.Stop(); Announcement = setup.players[i].name + " · CARGO LOST"; continue; }
                bool atNest = Vector3.Distance(trucks[i].body.position, nests[i].approach.position) < 11;
                bool settledTruck = trucks[i].body.linearVelocity.magnitude < .65f;
                if (!carrier.Busy && settledTruck && (bot || actions[i]))
                {
                    if (cargo.Restraint != null && atNest) carrier.BeginJob(true, nests[i]);
                    else if (cargo.Restraint == null) carrier.BeginJob(false, nests[i]);
                }
                if (carrier.Unloading && carrier.Released)
                {
                    releaseAge[i] += dt;
                    if (cargo.ObserveDelivery(nests[i], dt) && Rules.Deliver(i, nests[i].id, cargo.PhysicallyPresent, nests[i].ContainsAndSupports(cargo), cargo.Settled, out int award))
                    {
                        cargo.SecureDelivered(); carrier.FinishDelivery(); Retired[i] = true;
                        PhysicalDeliveries++; if (record.DeliveredBaby) BabyDeliveries++; LastDeliveredSlot = i;
                        ConfirmScore("nest-delivery:" + record.Id, i, award, record.Score, record.DeliveredBaby ? "BABY DRAGON DELIVERED SAFELY" : "DRAGON EGG DELIVERED", SportsScoreTier.Major);
                        ConfirmOutcome("nest-time:" + record.Id, i, record.DeliverySeconds, "ACTUAL NEST DELIVERY", "SEC");
                        Announcement = setup.players[i].name + (record.DeliveredBaby ? " · BABY SAFE AT THE NEST" : " · EGG SAFE AT THE NEST");
                        feedback.Play(6, 0, true); Pulse(i, .3f, .2f);
                    }
                    else if (releaseAge[i] > 25) { carrier.Stop(); releaseAge[i] = 0; Announcement = setup.players[i].name + " · CARGO NOT SEATED · RECOVER THE REAL LOAD"; }
                }
                else releaseAge[i] = 0;
            }
            TickMothers(); Rules.ExpireUnfinished();
            if (Rules.Complete)
            {
                if (resultsAt < 0) resultsAt = Clock + 2.5f;
                if (Clock >= resultsAt) EndMatch();
            }
        }
        protected override Vector3 DrivingGoal(int slot)
        {
            var carrier = carriers[slot];
            if (carrier.Busy || Rules.Cargo[slot].Terminal) return trucks[slot].body.position;
            if (carrier.cargo.Restraint != null) return nests[slot].approach.position;
            var delta = carrier.cargo.body.position - trucks[slot].body.position; delta.y = 0;
            var park = carrier.cargo.body.position - (delta.sqrMagnitude > 1 ? delta.normalized : trucks[slot].transform.forward) * 12;
            // Include the6m AI stopping tolerance in the real recovery reach;
            // otherwise projection can create an unreachable parked goal.
            if (LWS.InterstateHauler.LwsRoadGraphQuery.TryFindNearestRoad(liveGraph, park, 140, out var road)&&
                Vector3.Distance(road.NearestPosition,carrier.cargo.body.position)<DragonEggCarrier.MaximumHoistReach-7) park = road.NearestPosition;
            return park;
        }
        protected override string ObjectivePrompt(int slot)
        {
            if (Rules == null || slot < 0 || slot >= Rules.Count) return "LOAD THE PHYSICAL EGG";
            var carrier = carriers[slot]; var record = Rules.Cargo[slot];
            if (carrier.Busy) return carrier.Unloading ? "PHYSICAL NEST DELIVERY · WAIT FOR SUPPORT" : "RECOVERY HOIST · KEEP STILL";
            if (record.Terminal) return record.Stage == DragonEggStage.Delivered ? "SAFE DELIVERY · SPECTATING" : "CARGO LOST";
            if (carrier.cargo.Restraint == null) return "STOP NEAR YOUR REAL CARGO · ACTION TO RECOVER";
            float distance = Vector3.Distance(trucks[slot].body.position, nests[slot].approach.position);
            return distance < 12 ? "STOP · ACTION TO UNLOAD AT THE CORRECT NEST" : (record.Stage == DragonEggStage.Baby ? "HOLD ACTION TO COMFORT BABY · NEST " : "HOLD ACTION TO STEADY EGG · NEST ") + (distance > 1609 ? SportsMeasure.Miles(distance) : SportsMeasure.Feet(distance));
        }
        protected override bool ShouldHoldPosition(int slot) => carriers[slot].Busy || Rules.Cargo[slot].Terminal ||
            setup.players[slot].source==BowlingSource.AI&&bayParking[slot]&&bayMoveBlocked[slot]||
            setup.players[slot].source==BowlingSource.AI&&Vector3.ProjectOnPlane(DrivingGoal(slot)-trucks[slot].body.position,Vector3.up).magnitude<6&&
                CargoJobReach(slot)<DragonEggCarrier.MaximumHoistReach-.75f;
        protected override bool DragonMayTarget(int slot) => base.DragonMayTarget(slot) && (Rules == null || slot < Rules.Count && !Rules.Cargo[slot].Terminal);
        protected override Vector2 FilterDriveCommand(int slot, Vector2 drive, float dt)
        {
            if (setup.players[slot].source != BowlingSource.AI){bayParking[slot]=bayReverse[slot]=bayMoveBlocked[slot]=false;return drive;}
            if(TryFinalBayParking(slot,out var parking))return parking;
            float desired = Rules.Cargo[slot].Stage == DragonEggStage.Baby ? 7 : Rules.Cargo[slot].Condition < .6 ? 6 : 10;
            // Shared road following deliberately retains a3m/s final approach
            // for driving sports. Cargo jobs require a genuinely stopped truck;
            // brake for this physical bay before applying the existing NWH hold.
            float remaining=Vector3.ProjectOnPlane(DrivingGoal(slot)-trucks[slot].body.position,Vector3.up).magnitude;
            // A safe approach marker can be23m from the nest. Do not stop6m
            // short on its far side and then wait forever outside26m reach.
            if(CargoJobReach(slot)>=DragonEggCarrier.MaximumHoistReach-.75f)remaining=Mathf.Max(remaining,8);
            desired=Mathf.Min(desired,Mathf.Max(0,(remaining-4)*.6f));
            float speed = Vector3.Dot(trucks[slot].body.linearVelocity, trucks[slot].transform.forward);
            if (drive.y > 0 && speed > desired) drive.y = -Mathf.Clamp((speed - desired) * .2f, .1f, .65f);
            else if (drive.y > .6f) drive.y = .6f;
            return drive;
        }
        bool TryFinalBayParking(int slot,out Vector2 drive)
        {
            drive=default;var truck=trucks[slot];var carrier=carriers[slot];var goal=nests[slot].approach.position;Vector3 delta=Vector3.ProjectOnPlane(goal-truck.body.position,Vector3.up);
            if(carrier.Busy||Rules.Cargo[slot].Terminal||carrier.cargo.Restraint==null||delta.magnitude>FinalBayRange||Mathf.Abs(goal.y-truck.body.position.y)>4)
            {bayParking[slot]=bayReverse[slot]=bayMoveBlocked[slot]=false;bayNextCheck[slot]=0;yieldPhase[slot]=0;bayStatus[slot]="SHARED ROAD / CARGO JOB";return false;}
            if(yieldPhase[slot]!=0)return DriveBayYield(slot,out drive);
            // Only the actual final bay gets a direct physical corridor. Long
            // routes and human input keep the established road authority.
            if(Clock>=bayNextCheck[slot]||Vector3.Distance(goal,bayCheckedGoal[slot])>.5f)
            {
                bayNextCheck[slot]=Clock+1.25f;bayCheckedGoal[slot]=goal;bayChecks[slot]++;
                bayParking[slot]=CheckBayCorridor(slot,goal);if(!bayParking[slot])bayReverse[slot]=false;
            }
            if(!bayParking[slot])return false;
            // The shared GPS sees the same supported final goal rather than
            // the graph's nearby junction behind the actual truck.
            routes[slot].DirectGoal(truck.body.position,goal);
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),delta,Vector3.up);
            // Existing Musical Parking DriveTo's reverse hysteresis/steering;
            // this adapter supplies only Egg's finite final-bay desired speed.
            if(Mathf.Abs(angle)>128)bayReverse[slot]=true;else if(Mathf.Abs(angle)<82)bayReverse[slot]=false;
            bool reverse=bayReverse[slot];bayReverseObserved[slot]|=reverse;
            float desired=CargoJobReach(slot)<DragonEggCarrier.MaximumHoistReach-.75f?Mathf.Clamp((delta.magnitude-2.5f)*.6f,.45f,4.2f):Mathf.Clamp(delta.magnitude*.55f,.7f,4.2f);
            if(Mathf.Abs(angle)>45&&!reverse)desired*=.45f;
            float along=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);float steer=Mathf.Clamp((reverse?Mathf.DeltaAngle(angle,180):angle)/28,-1,1)*(reverse?-1:1);
            bayMoveBlocked[slot]=!BayMovementClear(slot,reverse);
            if(bayMoveBlocked[slot])
            {
                if(bayBlockedSince[slot]<=0)bayBlockedSince[slot]=Clock;
                if(Clock-bayBlockedSince[slot]>=.6f&&TryBeginBayYield(slot))return DriveBayYield(slot,out drive);
                drive=Vector2.zero;return true;
            } // Base uses NWH handbrake, never brake-to-reverse as a hold.
            bayBlockedSince[slot]=0;
            bayStatus[slot]=reverse?"SUPPORTED BAY · REVERSE PARKING":"SUPPORTED BAY · FORWARD PARKING";
            drive=new Vector2(steer,Mathf.Clamp(((reverse?-desired:desired)-along)*.65f,-1,1));return true;
        }
        bool CheckBayCorridor(int slot,Vector3 goal)
        {
            var from=trucks[slot].body.position;Vector3 forward=Vector3.ProjectOnPlane(goal-from,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=trucks[slot].transform.forward;
            // The established job parks inside6m, not on the approach transform.
            // V126's exact endpoint was proved only at its authored bay yaw;
            // validating that unused endpoint at travel yaw rejected slot0 in
            // V153. Prove the complete corridor to a real5m-short stop inside
            // the unchanged parking/reach gates, with every actual footprint.
            float length=Vector3.ProjectOnPlane(goal-from,Vector3.up).magnitude;
            Vector3 stop=length>5?goal-forward*5:from;
            if(Vector3.Distance(stop,nests[slot].center.position)>=DragonEggCarrier.MaximumHoistReach-.75f){bayStatus[slot]="NO STOP INSIDE EXISTING HOIST REACH";return false;}
            var rotation=Quaternion.LookRotation(forward);int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.ProjectOnPlane(stop-from,Vector3.up).magnitude/CorridorStep));
            if(steps>33){bayStatus[slot]="BAY CORRIDOR OUTSIDE BOUND";return false;}Pose previous=default;
            for(int i=0;i<=steps;i++)
            {
                if(!TryBayPose(slot,Vector3.Lerp(from,stop,i/(float)steps),rotation,out var pose)){bayStatus[slot]="BAY CORRIDOR "+i+"/"+steps+" · "+bayStatus[slot];return false;}
                if(i>0&&Mathf.Abs(pose.position.y-previous.position.y)>.65f){bayStatus[slot]="BAY CORRIDOR HEIGHT STEP";return false;}previous=pose;
            }
            return true;
        }
        bool TryBayPose(int slot,Vector3 point,Quaternion rotation,out Pose pose)
        {
            // V149/V153 identified the actual restrained capsules at sample0
            // and at the opposed crew's sample2. For static planning only, pair
            // each exact load with its ignored dynamic truck. The independent
            // live body probe below still blocks every other truck/load.
            for(int owner=0;owner<4;owner++)
            {
                var cargo=carriers[owner].cargo;
                bayAttachedLoads[owner]=cargo.PhysicallyPresent&&cargo.Restraint!=null&&cargo.Restraint.connectedBody==trucks[owner].body?cargo.ActiveShape:null;
            }
            if(!TryGroundedPose(slot,point,rotation,out pose,true,bayAttachedLoads)){bayStatus[slot]="CENTER SUPPORT / BODY CLEARANCE "+point.ToString("F2");return false;}
            var truck=trucks[slot];float lift=.12f;foreach(var wheel in truck.wheels)lift=Mathf.Max(lift,.12f-(truck.transform.InverseTransformPoint(wheel.transform.position).y-wheel.wheel.radius-wheel.spring.maxLength));
            Vector3 normal=pose.rotation*Vector3.up,surface=pose.position-normal*lift;
            foreach(var wheel in truck.wheels)
            {
                Vector3 local=truck.transform.InverseTransformPoint(wheel.transform.position),wheelPoint=pose.position+pose.rotation*Vector3.Scale(local,truck.transform.lossyScale);
                int count=Physics.RaycastNonAlloc(wheelPoint+Vector3.up*2,Vector3.down,bayGroundHits,5,~0,QueryTriggerInteraction.Ignore);if(count==bayGroundHits.Length){bayStatus[slot]="WHEEL QUERY SATURATED";return false;}bool supported=false;
                for(int i=0;i<count;i++)
                {
                    var hit=bayGroundHits[i];if(hit.collider==null||hit.normal.y<.7f||hit.collider.GetComponentInParent<SportsEventTruck>()!=null)continue;
                    float reach=Vector3.Dot(wheelPoint-hit.point,normal);
                    if(Mathf.Abs(Vector3.Dot(hit.point-surface,normal))<.45f&&reach>wheel.wheel.radius-.2f&&reach<wheel.wheel.radius+wheel.spring.maxLength+.35f){supported=true;break;}
                }
                if(!supported){bayStatus[slot]="WHEEL SUPPORT "+wheel.name+" "+wheelPoint.ToString("F2");return false;}
            }
            return true;
        }
        bool BayMovementClear(int slot,bool reverse,bool separatingYield=false)
        {
            bayBlockingCrew[slot]=-1;bool provedSeparation=false;
            var truck=trucks[slot];Vector3 direction=Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up).normalized*(reverse?-1:1);float distance=Mathf.Clamp(truck.body.linearVelocity.magnitude*.75f+1.5f,1.5f,7);
            if(!TryBayPose(slot,truck.body.position+direction*distance,truck.body.rotation,out _)){bayStatus[slot]="BAY HOLD · "+bayStatus[slot];return false;}
            Vector3 center=truck.transform.TransformPoint(truck.shape.center),half=Vector3.Scale(truck.shape.size*.5f,truck.transform.lossyScale)+new Vector3(.2f,.05f,.3f);
            int overlaps=Physics.OverlapBoxNonAlloc(center,half,bayBodies,truck.body.rotation,~0,QueryTriggerInteraction.Ignore);if(overlaps==bayBodies.Length){bayStatus[slot]="BAY HOLD · OVERLAP QUERY SATURATED";return false;}
            for(int i=0;i<overlaps;i++)if(!bayBodies[i].transform.IsChildOf(truck.transform)&&!bayBodies[i].transform.IsChildOf(carriers[slot].cargo.transform))
            {
                int owner=BayBodyOwner(bayBodies[i]);
                if(separatingYield&&owner==yieldTo[slot]&&SeparatingFromBody(slot,bayBodies[i],direction,distance)){provedSeparation=true;continue;}
                bayBlockingCrew[slot]=owner;bayStatus[slot]="BAY HOLD · OVERLAPPING ACTUAL BODY "+bayBodies[i].name;return false;
            }
            int count=Physics.BoxCastNonAlloc(center,half,direction,bayBodyHits,truck.body.rotation,distance,~0,QueryTriggerInteraction.Ignore);if(count==bayBodyHits.Length){bayStatus[slot]="BAY HOLD · BODY QUERY SATURATED";return false;}
            for(int i=0;i<count;i++)
            {
                var hit=bayBodyHits[i];if(hit.collider==null||hit.collider.transform.IsChildOf(truck.transform)||hit.collider.transform.IsChildOf(carriers[slot].cargo.transform))continue;
                if(hit.normal.y>.65f&&hit.collider.attachedRigidbody==null&&hit.collider.GetComponentInParent<SportsEventTruck>()==null)continue;
                int owner=BayBodyOwner(hit.collider);bool current=false;for(int j=0;j<overlaps;j++)current|=bayBodies[j]==hit.collider;
                if(separatingYield&&current&&owner==yieldTo[slot]&&SeparatingFromBody(slot,hit.collider,direction,distance)){provedSeparation=true;continue;}
                bayBlockingCrew[slot]=owner;bayStatus[slot]="BAY HOLD · ACTUAL BODY "+hit.collider.name;return false;
            }
            if(provedSeparation)yieldSeparatingSteps[slot]++;
            return true;
        }
        float CargoJobReach(int slot)=>Vector3.Distance(trucks[slot].body.position,
            carriers[slot].cargo.Restraint!=null?nests[slot].center.position:carriers[slot].cargo.body.position);
        public void CargoDisturbed(int slot)
        { if (slot >= 0 && slot < setup.count) { disturbedUntil[slot] = Clock + 12; Announcement = setup.players[slot].name + " · ROUGH CARGO IMPACT · MOTHER MAY NOTICE"; Pulse(slot, .2f, .12f); } }
        void TickMothers()
        {
            for (int index = 0; index < dragons.Length; index++)
            {
                var mother = dragons[index]; int disturbed = -1;
                for (int slot = 0; slot < setup.count; slot++)
                    if (DragonMayTarget(slot) && !Rules.Cargo[slot].Terminal && disturbedUntil[slot] > Clock && mother.CanObserveTarget(slot)) { disturbed = slot; break; }
                if (disturbed >= 0) { motherLandingPending[index]=false;mother.SetIntent(DragonCharacterIntent.ProtectNest, motherPerches[index].position, disturbed); continue; }
                if (mother.Intent == DragonCharacterIntent.ProtectNest) mother.SetIntent(DragonCharacterIntent.Patrol, PatrolGoal(index));
                int inspecting = -1;
                for (int slot = 0; slot < setup.count; slot++)
                    if (!Rules.Cargo[slot].Terminal && Vector3.Distance(trucks[slot].body.position, nests[slot].approach.position) < 35 && mother.CanObserveTarget(slot)) { inspecting = slot; break; }
                if(inspecting<0)motherLandingPending[index]=false;
                if (inspecting >= 0 && Clock >= motherActionAt[index])
                {
                    // Inspect the selected nest from its real shared-world
                    // perch, not this actor's unrelated home nest across the
                    // cliffs. Actual vendor landing/motion remains required.
                    var perches=world.Region("nests").perches;
                    var perch=inspecting<perches.Length?perches[inspecting]:motherPerches[index];
                    if (!mother.motor.IsGrounded || Vector3.Distance(mother.Position,perch.position)>16)
                    {
                        // Baseline before the real request. A previous landing
                        // at another perch cannot satisfy this new approach.
                        if(!motherLandingPending[index]||mother.motor.CompletedLandings>motherLandingBaseline[index])motherLandingBaseline[index]=mother.motor.CompletedLandings;
                        if(mother.RequestLanding(perch.position))motherLandingPending[index]=true;
                    }
                    else if(!mother.motor.LandingPlaying&&(!motherLandingPending[index]||mother.motor.CompletedLandings>motherLandingBaseline[index]))
                    {
                        motherLandingPending[index]=false;
                        if(!mother.motor.ActionPlaying&&mother.motor.TryAction(DragonAnimationAction.InspectEgg,8))motherActionAt[index]=Clock+14;
                    }
                }
                else if (mother.motor.IsGrounded && Clock >= motherActionAt[index])
                    mother.SetIntent(DragonCharacterIntent.Patrol, PatrolGoal(index));
                else if (mother.Intent == DragonCharacterIntent.Patrol && Vector3.Distance(mother.Position, mother.Goal) < 22)
                { motherPatrol[index]++; mother.SetIntent(DragonCharacterIntent.Patrol, PatrolGoal(index)); }
            }
        }
        Vector3 PatrolGoal(int index)
        {
            var region=world.Region(index%2==0?"forest":"nests");var anchors=region.ambientRoutes;
            Vector3 point=anchors.Length>0?anchors[(motherPatrol[index]+index)%anchors.Length].position:motherPerches[index].position+new Vector3(Mathf.Sin(motherPatrol[index]*1.9f)*90,45,Mathf.Cos(motherPatrol[index]*1.9f)*90);
            // Shared scenic flight anchors are160–180m high. Egg guardians
            // need real line-of-sight to ground crews with140m sight range.
            point.y=Mathf.Min(point.y,region.root.position.y+Mathf.Clamp(dragons[index].sightRange*.45f,35,80));return point;
        }
        protected override void ParticipantRecoveryBeginning(int slot) { carriers[slot].Stop(); carriers[slot].cargo.ReleaseRestraint(); }
        protected override void ParticipantEliminated(int slot) { Rules.Lose(slot); carriers[slot].Stop(); }
        protected override void StopGame()
        {
            foreach (var carrier in carriers) { carrier.Stop(); carrier.cargo.StopCargo(); }
            base.StopGame();
        }
    }
}
