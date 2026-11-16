using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using LWS.TruckBowling;
using LWS.TruckFareFight;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // Common lifecycle/adapter wiring for the three ground-based kingdom games.
    // Each concrete mode owns its objectives, awards and terminal conditions.
    public abstract class DragonKingdomGroundMatch : SportsEventSession
    {
        public DragonKingdomWorld world;
        public DragonSpeciesCatalog species;
        public SportsEventTruck[] trucks = Array.Empty<SportsEventTruck>();
        public DragonTruckCondition[] conditions = Array.Empty<DragonTruckCondition>();
        public DragonCharacter[] dragons = Array.Empty<DragonCharacter>();
        public string[] dragonSpeciesIds = Array.Empty<string>();
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        [Tooltip("Zero rolls a new reproducible seed per match; set a nonzero seed for repeatable testing.")]
        public int fixedSeed;
        public int MatchSeed { get; private set; }
        public float Clock { get; private set; }
        public float Intro { get; private set; }
        public string Announcement { get; protected set; }
        public int RouteRevision { get; protected set; }
        public bool[] Retired { get; } = new bool[4];
        public int[] Recoveries { get; } = new int[4];
        protected string startingRegionId;
        protected LwsRoadGraph liveGraph;
        protected readonly FareFightRoute[] routes = { new FareFightRoute(), new FareFightRoute(), new FareFightRoute(), new FareFightRoute() };
        protected readonly float[] recoverAt = new float[4];
        readonly Vector3[] safeRoad = new Vector3[4];
        readonly Quaternion[] safeHeading = new Quaternion[4];
        readonly float[] lift = new float[4], badPoseSeconds = new float[4], nextSafeSample = new float[4], recoveryDeadline = new float[4];
        readonly bool[] recoveryImpulse = new bool[4];
        readonly Rigidbody[][] truckBodies = new Rigidbody[4][];
        readonly RaycastHit[] groundHits = new RaycastHit[32];
        readonly Collider[] overlaps = new Collider[64];
        readonly Dictionary<DragonCharacter, Pose> dragonStarts = new Dictionary<DragonCharacter, Pose>();
        SportsUniversalHorn horn;
        float nextNoise;
        bool started;
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override bool SupportsSplitAIViews => true;
        public override bool CompactBroadcastStatus => true;
        public override bool CompactViewportHud => true;
        public override int CompactScoreColumns => 4;
        public override bool MultiViewHUD => drivingViews != null && !drivingViews.NeedsArena;
        public override bool TryReadViewportCard(int view, out int participant, out string score, out string detail)
            => TryReadDrivingViewportCard(drivingViews, view, out participant, out score, out detail);
        public override string ViewportContextPrompt(int view)
        {
            int slot = ViewOwner(view);
            return slot < 0 ? null : Retired[slot] ? "SPECTATING REMAINING CREWS" : recoverAt[slot] > 0 ? "RECOVERY · " + Mathf.CeilToInt(Mathf.Max(0, recoverAt[slot] - Time.time)) + "s" : ObjectivePrompt(slot);
        }
        public override string ViewportTelemetry(int view)
        {
            int slot = ViewOwner(view);
            return slot < 0 ? null : (trucks[slot].body.linearVelocity.magnitude * 2.236936f).ToString("0") + " MPH · TRUCK " + (conditions[slot].Health * 100).ToString("0") + "%"
                + (Retired[slot] ? "" : " · " + FareFightMatch.DistanceText(NavigationDistance(slot)));
        }
        int ViewOwner(int view) => drivingViews != null && view >= 0 && view < drivingViews.rally.Humans ? drivingViews.rally.owners[view] : -1;
        public override bool BroadcastParticipantActive(int slot) => slot >= 0 && slot < setup.count && !Retired[slot];
        public override bool ParticipantLocked(int slot) => slot >= 0 && slot < 4 && (Retired[slot] || recoverAt[slot] > 0);
        public bool IsDriving(int slot) => Live && Intro <= 0 && slot >= 0 && slot < setup.count && !Retired[slot] && recoverAt[slot] <= 0;
        protected abstract void PrepareObjectives();
        protected abstract void TickObjectives(float dt);
        protected abstract Vector3 DrivingGoal(int slot);
        protected abstract string ObjectivePrompt(int slot);
        public Vector3 NavigationGoal(int slot) => DrivingGoal(slot);
        public FareFightRoute NavigationRoute(int slot) => routes[slot];
        public string NavigationCaption(int slot) => ObjectivePrompt(slot);
        public float NavigationDistance(int slot)
        {
            // Read the route already planned for this participant. Presentation
            // never performs another graph search or displays metres.
            var route = routes[slot]; var position = trucks[slot].body.position;
            if (route.points.Count == 0) return Vector3.Distance(position, DrivingGoal(slot));
            int cursor = Mathf.Clamp(route.Cursor, 0, route.points.Count - 1);
            float distance = Vector3.Distance(position, route.points[cursor]);
            for (int i = cursor + 1; i < route.points.Count; i++) distance += Vector3.Distance(route.points[i - 1], route.points[i]);
            return distance;
        }
        protected virtual void BeginDragonIntents() { }
        protected virtual Vector2 FilterDriveCommand(int slot, Vector2 drive, float dt) => drive;
        protected virtual bool CanFollowRoute(int slot, float dt) => true;
        protected virtual bool ShouldHoldPosition(int slot) => false;
        protected virtual bool UseInPlaceRecovery => false;
        protected virtual void TruckNoiseObserved(int slot, Vector3 position, float loudness) { }
        protected virtual void ParticipantRecoveryBeginning(int slot) { }
        protected virtual bool DragonMayTarget(int slot) => slot >= 0 && slot < setup.count && !Retired[slot] && recoverAt[slot] <= 0;
        protected virtual void ParticipantRecovered(int slot) { }
        protected virtual void ParticipantEliminated(int slot) { }

        protected override void Awake()
        {
            base.Awake();
            // Capture authored poses before any setup-screen physics can move
            // a guardian. Activating/pausing it belongs to the match lifecycle.
            foreach (var dragon in dragons)
                if (dragon != null && dragon.motor != null && dragon.motor.body != null)
                {
                    dragonStarts[dragon] = new Pose(dragon.motor.body.position, dragon.motor.body.rotation);
                    dragon.gameObject.SetActive(false);
                }
        }

        protected override void BeginGame()
        {
            if (world == null || species == null || trucks.Length != 4 || conditions.Length != 4 || dragons.Length != dragonSpeciesIds.Length)
                throw new InvalidOperationException("Four complete NWH crews, shared kingdom/species and explicitly configured dragon actors are required.");
            MatchSeed = fixedSeed != 0 ? fixedSeed : Guid.NewGuid().GetHashCode(); world.ResetWorld(MatchSeed);
            liveGraph = LwsRoadGraph.FromJson(world.definition.graph.ToJson());
            Clock = nextNoise = 0; Intro = 3; started = false; RouteRevision = 0; Announcement = "PREPARE YOUR CREW";
            Array.Clear(Retired, 0, 4); Array.Clear(Recoveries, 0, 4); Array.Clear(recoverAt, 0, 4); Array.Clear(badPoseSeconds, 0, 4);
            PrepareObjectives();
            var region = world.Region(startingRegionId);
            if (region.truckStarts.Length < 4) throw new InvalidOperationException("Four comparable authored starts are required in " + startingRegionId);
            int rotation = (int)((uint)MatchSeed % 4);
            // Stage the whole old grid before placing its rotated replacement.
            // A later, unplaced crew must not occupy an earlier crew's new bay
            // during the actual physical clearance query. Already placed crews
            // stay active so overlaps within the new grid are still rejected.
            foreach (var truck in trucks) truck.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++)
            {
                var truck = trucks[i]; truck.gameObject.SetActive(i < setup.count);
                routes[i].Reset(); routes[i].useRoadConnectors = routes[i].useGoalProgressRecovery = true;
                routes[i].stallSeconds = 3; routes[i].reverseSeconds = 2; routes[i].minimumReverseDistance = 3;
                if (i >= setup.count) continue;
                if (truck.tractorMotor == null || conditions[i] == null || conditions[i].truck != truck)
                    throw new InvalidOperationException("Kingdom crews must use the actual configured NWH tractor and its condition adapter.");
                truck.tractorMotor.Configure();
                // These arcade rounds begin ready to drive. Use NWH's public
                // instant-idle start rather than leaving its 1.2s starter to
                // run after GO, or after a stationary crew stalls in a rematch.
                // NWH still owns the engine, clutch and every driving force.
                truck.tractorMotor.vehicle.powertrain.engine.flyingStartEnabled = true;
                conditions[i].RepairForNewAttempt();
                truckBodies[i] = truck.GetComponentsInChildren<Rigidbody>(true);
                lift[i] = .12f;
                foreach (var wheel in truck.wheels)
                    lift[i] = Mathf.Max(lift[i], .12f - (truck.transform.InverseTransformPoint(wheel.transform.position).y - wheel.wheel.radius - wheel.spring.maxLength));
                var anchor = region.truckStarts[(i + rotation) % 4];
                var road = anchor.position;
                if (!TryGroundedPose(i, road, anchor.rotation, out var pose)) throw new InvalidOperationException("The authored start lacks a clear physical wheel-supporting road: " + anchor.name);
                truck.Prepare(pose.position, pose.rotation); safeRoad[i] = pose.position - Vector3.up * lift[i]; safeHeading[i] = pose.rotation; nextSafeSample[i] = .5f;
            }
            for (int i = 0; i < dragons.Length; i++)
            {
                var dragon = dragons[i];
                if (dragon == null || !species.TryGet(dragonSpeciesIds[i], out var definition)) throw new InvalidOperationException("Every actor must bind an actual catalog species.");
                if (!dragonStarts.TryGetValue(dragon, out var pose)) { pose = new Pose(dragon.motor.body.position, dragon.motor.body.rotation); dragonStarts.Add(dragon, pose); }
                dragon.motor.body.position = pose.position; dragon.motor.body.rotation = pose.rotation;
                dragon.gameObject.SetActive(true); dragon.BindAudio(settings); dragon.Prepare(definition, trucks, setup.count, MatchSeed + i * 7919);
                dragon.TruckHit -= DragonHit; dragon.TruckHit += DragonHit;
                dragon.TargetEligible = DragonMayTarget;
            }
            BeginDragonIntents(); foreach (var dragon in dragons) dragon.Pause(true);
            GetComponent<DragonKingdomNavigation>()?.ResetRoutes();
            drivingViews.Layout(); ConsumeActions();
        }

        protected override void Simulate(float dt)
        {
            if (Intro > 0)
            {
                bool grounded = true;
                for (int i = 0; i < setup.count; i++)
                {
                    HoldWithNwh(trucks[i]);
                    grounded &= trucks[i].tractorMotor.Grounded && Vector3.Dot(trucks[i].transform.up, Vector3.up) > .9f;
                }
                if (grounded) Intro = Mathf.Max(0, Intro - dt);
                return;
            }
            if (!started) { started = true; foreach (var dragon in dragons) dragon.Pause(false); feedback.Play(5, 0, true); }
            Clock += dt;
            TickObjectives(dt);
            if (!Live) return;
            for (int i = 0; i < setup.count; i++)
            {
                if (Retired[i]) { HoldWithNwh(trucks[i]); continue; }
                if (recoverAt[i] > 0) { if (Time.time >= recoverAt[i]) TryRecover(i); continue; }
                var truck = trucks[i];
                bool overturned = Vector3.Dot(truck.transform.up, Vector3.up) < .2f && truck.body.linearVelocity.magnitude < 2;
                badPoseSeconds[i] = overturned ? badPoseSeconds[i] + dt : 0;
                if (conditions[i].Destroyed || badPoseSeconds[i] > 5 || truck.body.position.y < -15)
                { BeginRecovery(i, conditions[i].Destroyed ? "TRUCK WRECKED" : "RECOVERY NEEDED"); continue; }
                if (!CanFollowRoute(i, dt)) { HoldWithNwh(truck); continue; }
                var goal = DrivingGoal(i);
                routes[i].Update(liveGraph, truck.body.position, goal, RouteRevision, Time.time, false, truck.transform.forward);
                float grip = (presentationWeather != null ? presentationWeather.Grip : 1) * conditions[i].GripMultiplier;
                truck.tractorMotor.gripScale = grip;
                Vector2 drive = setup.players[i].source == BowlingSource.AI
                    ? routes[i].Drive(truck.body, truck.transform, (int)setup.players[i].difficulty, dt, grip)
                    : commands[i];
                var filtered = FilterDriveCommand(i, drive, dt);
                if (conditions[i].Stunned || ShouldHoldPosition(i)) HoldWithNwh(truck); else truck.TickDrive(filtered);
                if (Clock >= nextSafeSample[i])
                {
                    nextSafeSample[i] = Clock + .5f;
                    if (truck.tractorMotor.Grounded && Vector3.Dot(truck.transform.up, Vector3.up) > .8f
                        && LwsRoadGraphQuery.TryFindNearestRoad(liveGraph, truck.body.position, 15, out var road))
                    { safeRoad[i] = road.NearestPosition; safeHeading[i] = Quaternion.LookRotation(Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up).normalized); }
                }
            }
            if (Clock >= nextNoise)
            {
                nextNoise = Clock + .25f;
                if (horn == null) horn = GetComponent<SportsUniversalHorn>();
                for (int i = 0; i < setup.count; i++)
                    if (!Retired[i] && recoverAt[i] <= 0)
                    {
                        float noise = Mathf.Clamp01(Mathf.Abs(trucks[i].command.y)) * .25f + Mathf.Clamp01(trucks[i].body.linearVelocity.magnitude / 30) * .5f;
                        if (horn != null && horn.Voice(i) != null && horn.Voice(i).isPlaying) noise += 3;
                        TruckNoiseObserved(i, trucks[i].body.position, noise);
                        foreach (var dragon in dragons) dragon.ReportNoise(i, trucks[i].body.position, noise);
                    }
            }
            foreach (var dragon in dragons) dragon.Tick(dt);
        }

        void DragonHit(int slot, DragonElement element, float damagePercent, Vector3 point)
        {
            if (!IsDriving(slot) || !DragonMayTarget(slot)) return;
            if (conditions[slot].ApplyDragonHit(element, damagePercent * .01f, point))
            { Announcement = setup.players[slot].name + " · " + element.ToString().ToUpperInvariant() + " HIT"; Pulse(slot, .3f, .2f); feedback.Play(4, 0, true); }
        }
        protected void BeginRecovery(int slot, string reason)
        {
            if (Retired[slot] || recoverAt[slot] > 0) return;
            ParticipantRecoveryBeginning(slot);
            if (UseInPlaceRecovery && (conditions[slot].Destroyed || trucks[slot].body.position.y < -15))
            { Retired[slot] = true; trucks[slot].Power(false); ParticipantEliminated(slot); Announcement = setup.players[slot].name + " · ELIMINATED"; return; }
            if (Recoveries[slot] >= 2)
            { Retired[slot] = true; trucks[slot].Power(false); ParticipantEliminated(slot); Announcement = setup.players[slot].name + " · ELIMINATED"; return; }
            Recoveries[slot]++; recoverAt[slot] = Time.time + 4; recoveryDeadline[slot] = Time.time + 16;
            recoveryImpulse[slot] = false;
            trucks[slot].Power(false); Announcement = setup.players[slot].name + " · " + reason + (UseInPlaceRecovery ? " · SELF-RIGHTING" : " · ROAD RECOVERY");
        }
        void TryRecover(int slot)
        {
            if (UseInPlaceRecovery) { StepInPlaceRecovery(slot); return; }
            if (!TryGroundedPose(slot, safeRoad[slot], safeHeading[slot], out var pose))
            {
                recoverAt[slot] = Time.time + 1;
                if (Time.time >= recoveryDeadline[slot]) { Retired[slot] = true; recoverAt[slot] = 0; ParticipantEliminated(slot); Announcement = setup.players[slot].name + " · NO SAFE RECOVERY · ELIMINATED"; }
                return;
            }
            conditions[slot].RepairForNewAttempt(); trucks[slot].Prepare(pose.position, pose.rotation); routes[slot].Reset();
            recoverAt[slot] = badPoseSeconds[slot] = 0; RouteRevision++; ParticipantRecovered(slot);
        }
        void StepInPlaceRecovery(int slot)
        {
            var truck = trucks[slot]; var body = truck.body;
            if (conditions[slot].Destroyed || Time.time >= recoveryDeadline[slot] || body.position.y < -15)
            { Retired[slot] = true; recoverAt[slot] = 0; truck.Power(false); ParticipantEliminated(slot); Announcement = setup.players[slot].name + " · UNRECOVERABLE · ELIMINATED"; return; }
            if (Vector3.Dot(truck.transform.up, Vector3.up) > .88f && body.linearVelocity.magnitude < 4)
            {
                if (!truck.Powered) conditions[slot].ClearInterruptedRevLimiter();
                truck.Power(true); HoldWithNwh(truck);
                if (truck.tractorMotor.Grounded)
                { recoverAt[slot] = badPoseSeconds[slot] = 0; routes[slot].Reset(); RouteRevision++; ParticipantRecovered(slot); }
                return;
            }
            var correction = Quaternion.FromToRotation(truck.transform.up, Vector3.up);
            correction.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180) angle -= 360;
            var torque = Vector3.ClampMagnitude(axis.normalized * angle * Mathf.Deg2Rad * 3 - body.angularVelocity * 1.8f, 6);
            foreach (var part in truckBodies[slot])
                if (part != null && !part.isKinematic)
                {
                    if (!recoveryImpulse[slot]) part.AddForce(Vector3.up * 1.5f, ForceMode.VelocityChange);
                    part.AddTorque(torque, ForceMode.Acceleration);
                }
            recoveryImpulse[slot] = true;
        }
        protected bool TryGroundedPose(int slot, Vector3 road, Quaternion rotation, out Pose pose, bool ignoreOtherTrucks = false, Collider[] attachedLoads = null)
        {
            pose = default;
            if (!TryFindSupportedSurface(road, out var surface, out var normal, out var support)) return false;
            rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.forward, normal).normalized, normal);
            var truck = trucks[slot]; var p = surface + normal * lift[slot];
            var shape = truck.shape;
            var center = p + rotation * Vector3.Scale(shape.center, truck.transform.lossyScale);
            var half = Vector3.Scale(shape.size * .5f, truck.transform.lossyScale) + new Vector3(.2f, .05f, .3f);
            int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, rotation, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (overlaps[i] != support && !overlaps[i].transform.IsChildOf(truck.transform))
                {
                    // Optional static-corridor planning only. Existing starts,
                    // recoveries and all default callers retain dynamic blockers.
                    // The opting-in mode must check live bodies every drive tick;
                    // no collision/renderer/vehicle state is disabled here.
                    if (ignoreOtherTrucks && overlaps[i].GetComponentInParent<SportsEventTruck>() != null) continue;
                    // A mode can identify its bounded set of exact currently
                    // restrained truck loads, treated with their vehicles only
                    // for static planning. Loose loads/live collisions remain.
                    if (ignoreOtherTrucks && attachedLoads != null && Array.IndexOf(attachedLoads, overlaps[i]) >= 0) continue;
                    return false;
                }
            pose = new Pose(p, rotation); return true;
        }
        // The original support query remains one authority. Planning can ask
        // about real footing without interpreting a nearby movable obstacle as
        // a missing road; full spawn/recovery clearance still runs above.
        protected bool TryFindSupportedSurface(Vector3 road, out Vector3 surface, out Vector3 normal, out Collider support)
        {
            float best = float.PositiveInfinity; surface = default; normal = Vector3.up; support = null; bool found = false;
            int count = Physics.RaycastNonAlloc(road + Vector3.up * 6, Vector3.down, groundHits, 30, ~0, QueryTriggerInteraction.Ignore);
            if (count == groundHits.Length) return false;
            // NonAlloc buffers retain stale entries beyond count; inspect only
            // contacts returned by this actual query.
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.normal.y < .65f || hit.collider.GetComponentInParent<SportsEventTruck>() != null) continue;
                float error = Mathf.Abs(hit.point.y - road.y);
                if (error < best && error < 4) { best = error; surface = hit.point; normal = hit.normal; support = hit.collider; found = true; }
            }
            return found;
        }
        protected override void Present(float dt)
        {
            if (drivingViews != null && Running) drivingViews.Tick(dt);
        }
        protected static void HoldWithNwh(SportsEventTruck truck)
        {
            // Negative throttle uses NWH's automatic brake-to-reverse flow.
            // Hold a stopped truck with the existing handbrake instead.
            truck.TickDrive(Vector2.zero);
            var state = truck.tractorMotor.vehicle.input.states;
            state.handbrake = state.handbrakeRaw = 1;
            truck.tractorMotor.vehicle.input.states = state;
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); }
        protected override void SetPaused(bool paused)
        {
            foreach (var dragon in dragons) if (dragon != null) dragon.Pause(paused || Intro > 0);
            if (world != null) foreach (var site in world.extractionSites) if (site?.vessel != null) site.vessel.SetPaused(paused);
        }
        protected override void StopGame()
        {
            foreach (var dragon in dragons) if (dragon != null) { dragon.TruckHit -= DragonHit; dragon.ResetCharacter(); dragon.Pause(true); }
            if (world != null && world.extractionSites != null) foreach (var site in world.extractionSites) if (site?.vessel != null) site.vessel.SetPaused(true);
            foreach (var truck in trucks) if (truck != null) { truck.command = Vector2.zero; truck.Power(false); }
        }
    }
}
