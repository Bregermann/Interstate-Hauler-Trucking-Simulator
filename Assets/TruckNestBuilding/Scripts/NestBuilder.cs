using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckNestBuilding
{
    public enum NestBuilderMode { Drive, Grasping, Placing, Settling }
    public sealed partial class NestBuilder : MonoBehaviour
    {
        public NestMatch match;
        public SportsEventTruck truck;
        public int slot;
        public Rigidbody hand;
        public Transform boom;
        public Rigidbody[] originalAttachedBodies;
        public Vector3 spawn, carryHome;
        public NestBuilderMode Mode { get; private set; }
        public NestMaterial Carried { get; private set; }
        public NestMaterial Selected { get; private set; }
        public NestBuildSite GoalSite { get; private set; }
        public int GoalSocket { get; private set; } = -1;
        public int Grasps { get; private set; }
        public int PhysicalDrops { get; private set; }
        public Vector3 HandTarget { get; private set; }
        public Quaternion CapturedGraspOffset { get; private set; } = Quaternion.identity;
        public bool GraspOffsetCaptured { get; private set; }
        public Quaternion RequestedHandRotation { get; private set; } = Quaternion.identity;
        void ClearGraspOrientation() { CapturedGraspOffset=Quaternion.identity;GraspOffsetCaptured=false; }
        public static Quaternion PlacementHandTarget(Quaternion desiredCargo,Quaternion capturedOffset) => desiredCargo*Quaternion.Inverse(capturedOffset);
        public int PlacementStage { get; private set; }
        Vector3 placementEntry;
        Vector3 localTarget;
        float yaw, pressedAge, settledAge, alignedAge;
        bool wasHeld;
        bool transit;
        bool stowing,waitingForSupport;
        public bool WaitingForPrerequisites => waitingForSupport;
        public bool WaitingForCarryStow => stowing;
        public static Vector3 CarryStowTarget(Vector3 current, Vector3 home)
        {
            // Lift the actual gripper vertically before sweeping a grounded load
            // sideways. This is a target for the existing bounded physical hand.
            return current.y < home.y ? new Vector3(current.x,home.y,current.z) : home;
        }
        bool HandAtCarryHome => Vector3.Distance(hand.position,truck.transform.TransformPoint(carryHome))<.2f && Vector3.Distance(localTarget,carryHome)<.2f;
        readonly RaycastHit[] pickupSupportHits = new RaycastHit[16];
        public int UnreachableSelectionsReleased { get; private set; }
        NestMaterial waiting;
        void Awake() { Stop(); }
        public void Prepare(bool active)
        {
            ReleaseAll(); truck.gameObject.SetActive(active); hand.gameObject.SetActive(active); boom.gameObject.SetActive(active); if (!active) return;
            foreach (var body in originalAttachedBodies) { body.isKinematic = false; body.detectCollisions = true; }
            truck.Prepare(spawn, Quaternion.identity, true); truck.tractorMotor.Configure(); localTarget = carryHome; yaw = 0;
            hand.position = truck.transform.TransformPoint(localTarget); hand.rotation = truck.body.rotation; Mode = NestBuilderMode.Drive; Grasps = PhysicalDrops = UnreachableSelectionsReleased = BroodClearancesCompleted = 0; wasHeld = false; pressedAge = 0;
        }
        public void ReleaseAll()
        {
            RestoreMaterialGroundExclusions();
            if (Carried != null) Carried.Drop(); if (Selected != null) Selected.CancelReservation(slot);
            foreach (var site in match.sites) site.ReleaseClaim(slot);
            ClearBroodClearance();ClearGraspOrientation();Carried = Selected = waiting = null; stowing = waitingForSupport = false; GoalSite = null; GoalSocket = -1;
        }
        public bool TrySelect(NestMaterial item)
        {
            if (item == null || truck.body.linearVelocity.magnitude > 1 || Vector3.Distance(item.body.position, truck.body.position) > 33 || !item.Reserve(slot)) return false;
            Selected = item; Mode = NestBuilderMode.Grasping; return true;
        }
        public void EnterPlacement() { if (Carried == null) return; Mode = NestBuilderMode.Placing; alignedAge = 0; PlacementStage = 0; placementEntry = Carried.body.position; }
        public static Vector3 PlacementTarget(int stage, Vector3 entry, Vector3 resting, float deckTop)
        {
            float clearance = Mathf.Max(deckTop + 4.5f, resting.y + 1.2f);
            return stage == 0 ? new Vector3(entry.x, clearance, entry.z) : stage == 1 ? new Vector3(resting.x, clearance, resting.z) : resting + Vector3.up * .07f;
        }
        public void DropMaterial()
        {
            if (Carried == null) return; waiting = Carried; Carried.Drop(); Carried = null; ClearGraspOrientation(); stowing = false; Mode = NestBuilderMode.Settling; settledAge = 0; PhysicalDrops++;
        }
        void Tap()
        {
            if (Mode == NestBuilderMode.Grasping) { Selected.CancelReservation(slot); Selected = null; Mode = NestBuilderMode.Drive; return; }
            if (Carried != null) { if (Mode == NestBuilderMode.Placing) DropMaterial(); else EnterPlacement(); return; }
            NestMaterial nearest = null; float best = 33 * 33;
            foreach (var piece in match.materials) if (piece.Available) { float distance = (piece.body.position - hand.position).sqrMagnitude; if (distance < best) { best = distance; nearest = piece; } }
            TrySelect(nearest);
        }
        public void Tick(Vector2 axes, bool action, bool bot, float dt)
        {
            if (Carried != null && Carried.Grip == null) { var lost = Carried; Carried = null; ClearBroodClearance(); ClearGraspOrientation(); stowing = false; lost.CancelReservation(slot); Mode = NestBuilderMode.Drive; if (GoalSite != null) GoalSite.ReleaseClaim(slot); GoalSocket = -1; }
            waitingForSupport=false;
            Vector2 drive = axes;
            if (bot) drive = Think(dt);
            else
            {
                if (action) pressedAge += dt;
                if (wasHeld && !action && pressedAge < .35f) Tap();
                if (!action) pressedAge = 0; wasHeld = action;
                if (Mode == NestBuilderMode.Placing)
                {
                    if (action && pressedAge >= .35f) Nudge(new Vector3(0, axes.y * 2, 0), axes.x * 55, dt);
                    else Nudge(new Vector3(axes.x * 2.5f, 0, axes.y * 2.5f), 0, dt);
                }
            }
            bool hold = Mode != NestBuilderMode.Drive || bot && (stowing||waitingForSupport||ClearingBrood && Carried != null);
            truck.TickDrive(hold ? Vector2.zero : new Vector2(drive.x, Mathf.Clamp(drive.y, -.5f, .5f)));
            if (hold) truck.tractorMotor.vehicle.input.states.handbrake = truck.tractorMotor.vehicle.input.states.handbrakeRaw = 1;
            bool graspingAtTick=Mode==NestBuilderMode.Grasping;
            if (graspingAtTick)
            {
                if (Selected == null || Selected.ReservedBy != slot || Selected.Site != null) { Selected = null; Mode = NestBuilderMode.Drive; }
                else
                {
                    var point = truck.transform.InverseTransformPoint(Selected.body.worldCenterOfMass);
                    if (!WithinHandWorkspace(point))
                    {
                        // A collision can send the already-selected body out
                        // of the original bounded hand workspace. Release the
                        // claim; never freeze/chase or teleport an escaped body.
                        Selected.CancelReservation(slot); Selected = null;
                        Mode = NestBuilderMode.Drive; UnreachableSelectionsReleased++;
                    }
                    else
                    {
                    localTarget = Vector3.MoveTowards(localTarget, point, dt * 3);
                    if (Vector3.Distance(hand.position, Selected.body.worldCenterOfMass) < .55f && truck.body.linearVelocity.magnitude < .8f)
                    { Selected.Grab(hand, slot); if (Selected.Grip != null) { Carried = Selected; Selected = null; Mode = NestBuilderMode.Drive; transit = true; Grasps++;
                        // Capture the real initialized physical grasp frame once.
                        // The unchanged FixedJoint retains this offset while it
                        // carries. A yaw-only hand cannot remove its pitch/roll.
                        CapturedGraspOffset=Quaternion.Inverse(hand.rotation)*Carried.body.rotation;GraspOffsetCaptured=true;
                        // The old grasp target is the material centre below the
                        // hand. Continuing down after fixing the joint presses
                        // that real grounded piece into its support. Start the
                        // AI lift from the achieved hand pose, without moving it.
                        if(bot){localTarget=truck.transform.InverseTransformPoint(hand.position);stowing=!ClearingBrood;if(ClearingBrood)broodCarryHeight=Carried.body.position.y;} } }
                    }
                }
            }
            if (Mode == NestBuilderMode.Drive && !(bot && ClearingBrood && Carried != null) && (!graspingAtTick || bot && stowing))
            {
                var target=bot&&stowing?CarryStowTarget(localTarget,carryHome):carryHome;
                localTarget=Vector3.MoveTowards(localTarget,target,dt*2);
                if(bot&&stowing&&HandAtCarryHome)stowing=false;
            }
            localTarget.x = Mathf.Clamp(localTarget.x, -24, 24); localTarget.y = Mathf.Clamp(localTarget.y, -2, 15); localTarget.z = Mathf.Clamp(localTarget.z, 2, 33);
            HandTarget = truck.transform.TransformPoint(localTarget); hand.MovePosition(Vector3.MoveTowards(hand.position, HandTarget, dt * 3));
            RequestedHandRotation=truck.body.rotation*Quaternion.Euler(0,yaw,0);
            if(Mode==NestBuilderMode.Placing&&Carried!=null&&Carried.Grip!=null&&GraspOffsetCaptured)
            {
                var desiredCargo=bot&&GoalSite!=null&&GoalSocket>=0?GoalSite.TargetRotation(GoalSocket):RequestedHandRotation;
                RequestedHandRotation=PlacementHandTarget(desiredCargo,CapturedGraspOffset);
            }
            // Sole original kinematic hand authority. Cargo remains dynamic;
            // actual finite-joint rotation, force and alignment gates decide.
            hand.MoveRotation(Quaternion.RotateTowards(hand.rotation, RequestedHandRotation, dt * 65));
            var from = truck.transform.TransformPoint(new Vector3(0, 1.5f, carryHome.z - 4)); var delta = hand.position - from;
            boom.position = from + delta * .5f; if (delta.sqrMagnitude > .01f) boom.rotation = Quaternion.LookRotation(delta); boom.localScale = new Vector3(.22f, .22f, Mathf.Max(.1f, delta.magnitude));
        }
        void Nudge(Vector3 rate, float yawRate, float dt) { localTarget += Vector3.ClampMagnitude(rate, 3) * dt; yaw = Mathf.DeltaAngle(0, yaw + Mathf.Clamp(yawRate, -65, 65) * dt); }
        Vector2 Think(float dt)
        {
            var site = match.sites[match.rules.Cooperative ? 0 : slot];
            if (match.rules.hatched[site.index]) return Vector2.zero;
            if (ThinkBroodClearance(site,dt,out var clearanceDrive)) return clearanceDrive;
            if (Mode == NestBuilderMode.Settling)
            {
                settledAge += dt;
                if (waiting != null && waiting.Site == site && site.Rooted(waiting)) { site.ReleaseClaim(slot); GoalSocket = -1; waiting = null; Mode = NestBuilderMode.Drive; }
                else if (settledAge > 5)
                {
                    var retry = waiting; waiting = null; Mode = NestBuilderMode.Drive;
                    if (retry != null && retry.Available) TrySelect(retry); else { site.ReleaseClaim(slot); GoalSocket = -1; }
                }
                return Vector2.zero;
            }
            if (Mode == NestBuilderMode.Grasping) return Vector2.zero;
            if (Carried == null)
            {
                if (GoalSocket < 0) { if (!match.ClaimAvailableFor(this)) return Vector2.zero; GoalSite = site; GoalSocket = site.ClaimNextForCollection(slot,match.builders,match.setup.count); }
                if (GoalSocket < 0) return Vector2.zero;
                NestMaterial nearest = null; float distance = float.PositiveInfinity;
                foreach (var piece in match.materials) if (piece.kind == NestBuildSite.Kind(GoalSocket) && CanApproachLooseMaterial(piece)) { float d = (piece.body.position - truck.body.position).sqrMagnitude; if (d < distance) { distance = d; nearest = piece; } }
                if (nearest == null) return Vector2.zero;
                // Materials can move after a real collision/grip break. A remembered depot stand
                // can stop behind them forever; park relative to the actual loose body
                // and present heading so the unchanged four-metre stop is within reach.
                Vector3 park = AcquisitionParkingTarget(nearest.body.position, truck.transform.forward, truck.body.position.y);
                var local = truck.transform.InverseTransformPoint(nearest.body.position);
                if (Mathf.Abs(local.x) < 23 && local.z > 2 && local.z < 30 && Vector3.Distance(nearest.body.position, truck.body.position) < 32 && truck.body.linearVelocity.magnitude < .7f) { TrySelect(nearest); return Vector2.zero; }
                return Navigate(park, Vector3.Dot(park - truck.body.position, truck.transform.forward) < -4);
            }
            if (GoalSocket < 0 || site.slots[GoalSocket] != null) { GoalSite = site; GoalSocket = site.ClaimNextForCollection(slot,match.builders,match.setup.count,Carried.kind); if (GoalSocket < 0) { DropMaterial(); return Vector2.zero; } }
            if(stowing)return Vector2.zero;
            // A genuine later structural loss can invalidate already-collected
            // future jobs. If every builder is waiting on missing support, one
            // worker physically stages its held piece and repairs the ancestor.
            // The cargo remains a real loose body; nothing is placed or scored.
            if(match.ShouldRecoverMissingCore(this,site))
            {
                Carried.Drop();Carried=null;ClearGraspOrientation();stowing=false;waiting=null;PhysicalDrops++;
                site.ReleaseClaim(slot);GoalSocket=-1;Mode=NestBuilderMode.Drive;return Vector2.zero;
            }
            Vector3 work = site.transform.position + new Vector3(match.rules.Cooperative ? (slot - 1.5f) * 6 : 0, -site.transform.position.y, -22);
            if (transit && truck.body.position.z < -12) return Navigate(new Vector3((slot - 1.5f) * 32, 0, -8));
            transit = false;
            if (Mode != NestBuilderMode.Placing)
            {
                if (Vector3.ProjectOnPlane(work - truck.body.position, Vector3.up).magnitude < 3.5f && truck.body.linearVelocity.magnitude < .7f) { if(site.Prerequisites(GoalSocket))EnterPlacement(); else {waitingForSupport=true;return Vector2.zero;} }
                else return Navigate(work, false, 3);
            }
            if (Mode == NestBuilderMode.Placing && site.Prerequisites(GoalSocket))
            {
                // The pickup is below the six-metre platform. A diagonal move
                // toward its socket hits the real deck lip/underside and breaks
                // the unchanged finite grip. Lift clear before crossing it,
                // then lower onto the actual rooted support using the same hand.
                Vector3 target = PlacementTarget(PlacementStage, placementEntry, site.Target(GoalSocket), site.deck.bounds.max.y);
                Vector3 error = target - Carried.body.position;
                var localError = Quaternion.Inverse(truck.body.rotation) * error;
                float angle = Vector3.SignedAngle(Carried.body.rotation * Vector3.forward, site.TargetRotation(GoalSocket) * Vector3.forward, Vector3.up);
                Nudge(new Vector3(Mathf.Clamp(localError.x * 2, -2.5f, 2.5f), Mathf.Clamp(localError.y * 2, -2, 2), Mathf.Clamp(localError.z * 2, -2.5f, 2.5f)), Mathf.Clamp(angle * 2, -65, 65), dt);
                alignedAge = error.magnitude < .18f && Mathf.Abs(angle) < 7 && Carried.body.linearVelocity.magnitude < .5f ? alignedAge + dt : 0;
                if (alignedAge > .6f) { if (PlacementStage < 2) { PlacementStage++; alignedAge = 0; } else DropMaterial(); }
            }
            return Vector2.zero;
        }
        public static bool WithinHandWorkspace(Vector3 localPoint)
        {
            return float.IsFinite(localPoint.x) && float.IsFinite(localPoint.y) && float.IsFinite(localPoint.z)
                && localPoint.x >= -24 && localPoint.x <= 24 && localPoint.y >= -2 && localPoint.y <= 15
                && localPoint.z >= 2 && localPoint.z <= 33;
        }
        bool CanApproachLooseMaterial(NestMaterial piece)
        {
            if (piece == null || !piece.Available || piece.body.linearVelocity.sqrMagnitude > 1) return false;
            // AI chooses a real resting/near-resting supply, not an item falling
            // beneath the finite terrain. This is a planning predicate only:
            // original pickup/force/support/rule gates remain authoritative.
            int hits = Physics.RaycastNonAlloc(piece.body.worldCenterOfMass + Vector3.up * .5f, Vector3.down,
                pickupSupportHits, 2.5f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
                if (pickupSupportHits[i].collider != null && pickupSupportHits[i].collider.attachedRigidbody != piece.body) return true;
            return false;
        }
        public static Vector3 AcquisitionParkingTarget(Vector3 actualLoosePiece, Vector3 actorForward, float groundHeight)
        {
            var heading = Vector3.ProjectOnPlane(actorForward, Vector3.up).normalized;
            var target = actualLoosePiece - heading * 14;
            target.y = groundHeight;
            return target;
        }
        public static float NavigationDesiredSpeed(float distance, float absoluteHeadingError, float stoppingDistance)
        {
            return distance < stoppingDistance ? 0 : absoluteHeadingError > 40 ? 1.4f : distance < 10 ? 1.7f : 3.1f;
        }
        Vector2 Navigate(Vector3 target, bool reverse = false, float stoppingDistance = 4)
        {
            var delta = target - truck.body.position; delta.y = 0; float distance = delta.magnitude;
            var forward = truck.transform.forward * (reverse ? -1 : 1); float angle = Vector3.SignedAngle(forward, delta, Vector3.up);
            float desired = NavigationDesiredSpeed(distance, Mathf.Abs(angle), stoppingDistance);
            float speed = Vector3.Dot(truck.body.linearVelocity, forward);
            return new Vector2(Mathf.Clamp(angle / 30, -1, 1) * (reverse ? -1 : 1), Mathf.Clamp((desired - speed) * .5f, -.3f, .5f) * (reverse ? -1 : 1));
        }
        public void Stop()
        {
            RestoreMaterialGroundExclusions();
            if (truck == null) return;
            if (Carried != null) Carried.Drop(); if (Selected != null) Selected.CancelReservation(slot);
            ClearBroodClearance();ClearGraspOrientation();Carried = Selected = null; stowing = waitingForSupport = false;
            truck.Power(false); truck.StopSound(); if (!truck.body.isKinematic) truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero; truck.body.isKinematic = true;
            if (originalAttachedBodies != null) foreach (var body in originalAttachedBodies) { if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true; }
            if (hand != null) hand.isKinematic = true;
        }
        public void ResumeWorkshop(bool active)
        {
            truck.gameObject.SetActive(active); hand.gameObject.SetActive(active); boom.gameObject.SetActive(active);
            if (!active) return;
            ClearBroodClearance();ClearGraspOrientation();Mode = NestBuilderMode.Drive; stowing = false; localTarget = carryHome; yaw = pressedAge = 0; wasHeld = false;
            hand.position = truck.transform.TransformPoint(carryHome); hand.rotation = truck.body.rotation;
        }
    }
}
