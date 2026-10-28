using System.Text;
using LWS.TruckBowling;
using LWS.TruckFareFight;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckPinataPanic
{
    public sealed class PinataMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks;
        public PinataShell pinata;
        public PinataCandy[] candies;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public PinataFestivalPresentation festival;
        public PinataRules rules = new PinataRules();
        public bool CanStrike => Live && rules.Phase == PinataPhase.Striking;
        public bool CanCollect => Live && rules.Phase == PinataPhase.Candy;
        public int NativePickups { get; private set; }
        public int NativeExplosions { get; private set; }
        public int AIReplans { get; private set; }
        public int AIThreatAvoidances { get; private set; }
        public int AIStrikePasses { get; private set; }
        public int AIReturnRoutes { get; private set; }
        public int AIStrikeAimUpdates { get; private set; }
        public int LastImpactSlot { get; private set; } = -1;
        public int LastCollectorSlot { get; private set; } = -1;
        float lastImpactAt = -100, lastCollectionAt = -100;
        public override string Title => "TRUCK PIÑATA PANIC";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int WeatherCount => 1; // lead owns the common preset picker
        public override bool SupportsSplitAIViews => true;
        public override bool CompactBroadcastStatus => true;
        public override bool CompactViewportHud => true;
        public override bool BroadcastStatusVisible => !string.IsNullOrEmpty(Status);
        public override int CompactScoreColumns => 4;
        public override bool MultiViewHUD => drivingViews != null && !drivingViews.NeedsArena;
        public override string OptionLabel => new[] { "THREE PIÑATAS", "FIVE PIÑATAS", "SEVEN PIÑATAS" }[Mathf.Clamp(option, 0, 2)];
        public override string Header => Title + " · PIÑATA " + (rules.Round + 1) + " / " + rules.Rounds;
        public override string Status => rules.Phase == PinataPhase.Ready ? "READY · " + Mathf.CeilToInt(rules.PhaseSeconds) : rules.Phase == PinataPhase.Intermission ? "NEXT PIÑATA · " + Mathf.CeilToInt(rules.PhaseSeconds) : rules.Phase == PinataPhase.Striking ? "SHELL " + Mathf.CeilToInt(pinata.Health) + " / " + Mathf.CeilToInt(pinata.MaximumHealth) + " · " + Mathf.CeilToInt(rules.PhaseSeconds) + "s" : rules.Phase == PinataPhase.Candy ? rules.RemainingCandy + " SWEETS LEFT · " + Mathf.CeilToInt(rules.PhaseSeconds) + "s" : "";
        public override string Help => "Ram the giant hanging paper piñata with your real truck. A solid impact above 2.5 m/s swings it and breaks its CoreCollapse shell; rubbing or parking against it earns nothing. Damage earns small points; released candy earns the big rewards. Wrapped sweets are 10 points, golden stars 50, and black fused sweets explode on collection: lose up to 15 points and receive a physical shove. Drive into each physical sweet to claim it once. AI drives the same trucks, strikes the same shell, chases the same sweets and avoids explosives. Each piñata allows 55 seconds to break it and 24 seconds to collect; a timed-out intact shell earns no candy. Three, five or seven piñatas form a match. Ties share first place. ACTION is a cooldown-limited in-place hop/upright recovery, never a teleport. Collection drive, horn, shared/chase/cab, free-look, pause and crew controls are retained. Profiles, names and colors use the shared setup. Rematch resets health, candies, scores and trucks.";
        public override string ControlCaption(int slot) => base.ControlCaption(slot).Replace(" ACTION", " RECOVER");
        public override bool BroadcastParticipantActive(int slot) => slot >= 0 && slot < setup.count;
        public override float ParticipantProgress(int slot) => -1;
        public override bool TryReadScoreCard(int slot, out string score, out string detail)
        {
            score = slot >= 0 && slot < setup.count ? rules.points[slot] + " PTS" : "";
            detail = slot >= 0 && slot < setup.count ? rules.collected[slot] + " SWEETS · " + rules.damagePoints[slot] + " HIT PTS" : "";
            return slot >= 0 && slot < setup.count;
        }
        public override bool TryReadViewportCard(int view, out int participant, out string score, out string detail) => TryReadDrivingViewportCard(drivingViews, view, out participant, out score, out detail);
        public override string ViewportTelemetry(int view) => PhaseReadout;
        public override string ViewText(int view) { int slot = drivingViews.rally.owners[view]; TryReadScoreCard(slot, out string score, out string detail); return setup.players[slot].name + " · " + score + "\n" + detail + "\n" + PhaseReadout; }
        public string PhaseReadout => Status;
        readonly StringBuilder result = new StringBuilder(320);
        public override string Standings
        {
            get { result.Clear(); for (int place = 1; place <= setup.count; place++) for (int i = 0; i < setup.count; i++) if (rules.Place(i) == place) result.Append('#').Append(place).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append(" PTS · ").Append(rules.collected[i]).Append(" SWEETS\n"); return result.ToString(); }
        }
        public override string Results => (rules.TiedWinner ? "SHARED CANDY CHAMPIONS!" : rules.Winner >= 0 ? setup.players[rules.Winner].name + " WINS PIÑATA PANIC!" : "PIÑATA PANIC") + "\n\n" + Standings;
        readonly FareFightRoute[] drivers = new FareFightRoute[4];
        readonly Vector3[] goals = new Vector3[4];
        readonly float[] nextPlan = new float[4], nextRecovery = new float[4], stuck = new float[4];
        readonly int[] targets = { -1, -1, -1, -1 };
        readonly Vector3[] strikeOrigins = new Vector3[4], strikeDirections = new Vector3[4], strikeGoals = new Vector3[4];
        readonly bool[] strikeCommitted = new bool[4], returning = new bool[4], strikeAimLocked = new bool[4];
        readonly Vector3[] returnEndpoints = new Vector3[4];
        int preparedRound = -1;
        SportsBroadcastFraming festivalFraming;
        void ConfigureFestivalFraming()
        {
            var shared=broadcast.GetComponent<SportsBroadcastFraming>();if(shared==null||festivalFraming==shared)return;
            // The real suspension beam is retained. The outdoor steep shot put
            // that nearby beam across most of the image; look underneath it from
            // an oblique festival-side angle using the existing shared rig only.
            var local=JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(shared.Configuration));
            local.elevation=35;local.azimuth=35;local.minimumTruckHeight=.10f;local.preferredTruckHeight=.15f;
            local.maximumDistanceInTruckLengths=8;shared.settings=local;shared.useSharedDefaults=false;festivalFraming=shared;
        }
        protected override void BeginGame()
        {
            if (trucks == null || trucks.Length != 4 || candies == null || candies.Length != PinataRules.CandyCapacity || pinata == null || drivingViews == null || broadcast == null || framing == null || festival == null) throw new System.InvalidOperationException("Author the complete Pinata Panic venue before play.");
            ConfigureFestivalFraming();rules.Reset(setup.count, option == 0 ? 3 : option == 1 ? 5 : 7); preparedRound = -1;
            NativePickups = NativeExplosions = AIReplans = AIThreatAvoidances = AIStrikePasses = AIReturnRoutes = AIStrikeAimUpdates = 0;
            LastImpactSlot = LastCollectorSlot = -1; lastImpactAt = lastCollectionAt = -100;
            PrepareRound(); drivingViews.Layout(); festival?.ResetFestival();
        }
        void PrepareRound()
        {
            preparedRound = rules.Round; foreach (var candy in candies) candy.Retire();
            LastImpactSlot = LastCollectorSlot = -1; lastImpactAt = lastCollectionAt = -100;
            pinata.ResetShell(); pinata.Freeze();
            for (int i = 0; i < 4; i++)
            {
                var truck = trucks[i]; truck.gameObject.SetActive(i < setup.count);
                drivers[i] ??= new FareFightRoute(); drivers[i].Reset();
                // A real moving U-turn deliberately makes little progress toward
                // its final endpoint. Keep the existing stationary/obstacle
                // recovery instead of interpreting that maneuver as a stall.
                drivers[i].useGoalProgressRecovery = false;
                drivers[i].minimumReverseDistance = 0;
                strikeCommitted[i] = returning[i] = strikeAimLocked[i] = false;
                nextPlan[i] = nextRecovery[i] = stuck[i] = 0; targets[i] = -1;
                if (i >= setup.count) continue;
                truck.tractorMotor.Configure(); float angle = Mathf.PI * .25f + i * Mathf.PI * .5f;
                var position = new Vector3(Mathf.Cos(angle) * 35, .65f, Mathf.Sin(angle) * 35);
                truck.Prepare(position, Quaternion.LookRotation(Vector3.ProjectOnPlane(-position, Vector3.up)));
                Hold(truck);
            }
            Physics.SyncTransforms();
        }
        static void Hold(SportsEventTruck truck)
        {
            var input = truck.tractorMotor.vehicle.input;
            input.states.throttle = input.states.throttleRaw = input.states.brakes = input.states.brakesRaw = 0;
            input.states.handbrake = input.states.handbrakeRaw = 1;
        }
        protected override void Simulate(float dt)
        {
            var phase = rules.Phase; rules.Tick(dt);
            if (rules.Complete)
            {
                for (int i = 0; i < setup.count; i++) if (rules.Place(i) == 1) ConfirmOutcome("result:" + i, i, rules.points[i], rules.TiedWinner ? "SHARED CANDY CHAMPION" : "CANDY CHAMPION", "PTS", SportsScoreTier.MatchChampion, matchEnded: true);
                EndMatch(); return;
            }
            if (preparedRound != rules.Round) PrepareRound();
            if (phase == PinataPhase.Ready && rules.Phase == PinataPhase.Striking) pinata.body.isKinematic = false;
            if (rules.Phase == PinataPhase.Ready || rules.Phase == PinataPhase.Intermission) { for (int i = 0; i < setup.count; i++) Hold(trucks[i]); if (phase != rules.Phase && rules.Phase == PinataPhase.Intermission) { pinata.Freeze(); foreach (var candy in candies) candy.Retire(); } return; }
            for (int i = 0; i < setup.count; i++)
            {
                var truck = trucks[i]; stuck[i] = truck.body.linearVelocity.magnitude < .8f ? stuck[i] + dt : 0;
                bool ai = setup.players[i].source == BowlingSource.AI;
                truck.TickDrive(ai ? Bot(i, dt) : commands[i]);
                if (actions[i] || ai && stuck[i] > 5) Recover(i);
            }
        }
        Vector2 Bot(int slot, float dt)
        {
            var truck = trucks[slot]; int skill = (int)setup.players[slot].difficulty;
            if (rules.Clock >= nextPlan[slot])
            {
                nextPlan[slot] = rules.Clock + (skill == 0 ? .5f : skill == 1 ? .3f : .18f); AIReplans++;
                Vector3 goal = pinata.body.position; targets[slot] = -1;
                if (CanCollect)
                {
                    returning[slot] = false;
                    float best = float.MaxValue;
                    foreach (var candy in candies)
                    {
                        if (!candy.Released || candy.kind == PinataCandyKind.Explosive) continue;
                        float value = (candy.body.position - truck.body.position).sqrMagnitude / (candy.kind == PinataCandyKind.Golden ? 3 : 1);
                        if (value < best) { best = value; goal = candy.body.position; targets[slot] = candy.id; }
                    }
                    if (targets[slot] < 0) goal = new Vector3(Mathf.Cos(slot * 1.57f) * 20, 0, Mathf.Sin(slot * 1.57f) * 20);
                    foreach (var danger in candies)
                    {
                        if (!danger.Released || danger.kind != PinataCandyKind.Explosive) continue;
                        var to = Vector3.ProjectOnPlane(goal - truck.body.position, Vector3.up); var threat = Vector3.ProjectOnPlane(danger.body.position - truck.body.position, Vector3.up);
                        float along = Vector3.Dot(threat, to.normalized); float lateral = Vector3.Cross(to.normalized, threat).magnitude;
                        if (along > -2 && along < 13 && lateral < 5)
                        {
                            var right = Vector3.Cross(Vector3.up, to.normalized); float side = Vector3.Dot(threat, right) >= 0 ? -1 : 1;
                            goal = truck.body.position + to.normalized * 8 + right * side * 10; AIThreatAvoidances++; break;
                        }
                    }
                }
                else
                {
                    PlanStrikeRoute(slot, truck);
                    goal = returning[slot] ? returnEndpoints[slot] : strikeGoals[slot];
                }
                goal.y = truck.body.position.y; goals[slot] = goal;
            }
            // The existing route controller advances the authored physical arc
            // waypoints. DirectGoal here would erase that route every step.
            if (!returning[slot]) drivers[slot].DirectGoal(truck.body.position, goals[slot]);
            drivers[slot].speedScale = returning[slot] ? .65f : 1;
            return drivers[slot].Drive(truck.body, truck.transform, skill, dt, 1, CanStrike ? pinata.body : null);
        }
        void PlanStrikeRoute(int slot, SportsEventTruck truck)
        {
            if (returning[slot])
            {
                var toShell = Vector3.ProjectOnPlane(pinata.body.position - truck.body.position, Vector3.up);
                var toEnd = Vector3.ProjectOnPlane(returnEndpoints[slot] - truck.body.position, Vector3.up);
                if (toEnd.magnitude < 9 && Vector3.Dot(truck.transform.forward, toShell.normalized) > .45f)
                {
                    returning[slot] = false; strikeCommitted[slot] = false;
                }
                else return;
            }
            if (!strikeCommitted[slot])
            {
                var sampled = pinata.body.position + Vector3.ClampMagnitude(pinata.body.linearVelocity * .2f, 2);
                var direction = Vector3.ProjectOnPlane(sampled - truck.body.position, Vector3.up).normalized;
                if (direction.sqrMagnitude < .1f) direction = Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up).normalized;
                strikeOrigins[slot] = sampled; strikeDirections[slot] = direction;
                strikeGoals[slot] = sampled + direction * 18;
                strikeAimLocked[slot] = false;
                strikeCommitted[slot] = true; AIStrikePasses++;
            }
            if (!strikeAimLocked[slot])
            {
                // The real shell can cross several metres during a long run-up.
                // Follow its observed movement at the existing planning cadence;
                // commit only near contact, then complete the original pass.
                var sampled = pinata.body.position + Vector3.ClampMagnitude(pinata.body.linearVelocity * .2f, 2);
                var toShell = Vector3.ProjectOnPlane(sampled - truck.body.position, Vector3.up);
                if (toShell.sqrMagnitude > .01f)
                {
                    strikeOrigins[slot] = sampled; strikeDirections[slot] = toShell.normalized;
                    strikeGoals[slot] = sampled + strikeDirections[slot] * 18;
                    AIStrikeAimUpdates++;
                }
                if (Vector3.ProjectOnPlane(pinata.body.position - truck.body.position, Vector3.up).sqrMagnitude <= 64)
                    strikeAimLocked[slot] = true;
                return;
            }
            // Finish the physical pass even when the first contact swings the
            // shell away. No six-second expiration can flip this point behind
            // the tractor halfway through its first impact.
            if (Vector3.Dot(truck.body.position - strikeOrigins[slot], strikeDirections[slot]) <= 12) return;
            var start = truck.body.position;
            var forward = Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .5f) forward = strikeDirections[slot];
            var right = Vector3.Cross(Vector3.up, forward);
            const float radius = 9, straight = 32;
            var end = start - forward * straight;
            var driver = drivers[slot]; driver.Reset(); driver.DirectGoal(start, end);
            driver.points.Clear();
            // Two genuine semicircles and a parallel straight return put the
            // full tractor back on a forward-facing run-up. These are waypoints
            // for the original NWH driver, never body poses or steering forces.
            for (int step = 1; step <= 6; step++)
            {
                float a = step * Mathf.PI / 6;
                driver.points.Add(start + forward * (Mathf.Sin(a) * radius) + right * ((1 - Mathf.Cos(a)) * radius));
            }
            var second = start - forward * straight + right * (radius * 2);
            driver.points.Add(second);
            for (int step = 1; step <= 6; step++)
            {
                float a = step * Mathf.PI / 6;
                driver.points.Add(second - forward * (Mathf.Sin(a) * radius) - right * ((1 - Mathf.Cos(a)) * radius));
            }
            returnEndpoints[slot] = end; returning[slot] = true; AIReturnRoutes++;
        }
        public bool Recover(int slot)
        {
            if (!Live || slot < 0 || slot >= setup.count || rules.Clock < nextRecovery[slot]) return false;
            var truck = trucks[slot]; if (Vector3.Dot(truck.transform.up, Vector3.up) > .6f && (stuck[slot] < 3 || truck.body.linearVelocity.magnitude > 1.5f)) return false;
            nextRecovery[slot] = rules.Clock + 4; stuck[slot] = 0;
            truck.tractorMotor.AddAssemblyVelocity(Vector3.up * 2.8f);
            truck.body.AddTorque(Vector3.Cross(truck.transform.up, Vector3.up) * 4, ForceMode.VelocityChange);
            return true;
        }
        public void BankImpact(int slot, int serial, float actualHealthLost, Vector3 point, float relativeSpeed)
        {
            if (!Live) return; int delta = rules.BankDamage(rules.Round, serial, slot, actualHealthLost); if (delta == 0) return;
            LastImpactSlot = slot; lastImpactAt = rules.Clock;
            ConfirmScore("hit:" + rules.Round + ":" + serial, slot, delta, rules.points[slot], "PAPER SHELL DAMAGED", SportsScoreTier.Small);
            feedback.PlayAt(2, point, relativeSpeed); Pulse(slot, .3f, .12f); festival?.Hit(point, actualHealthLost);
        }
        public void ReleaseCandy(Vector3 origin)
        {
            if (!Live || !rules.Rupture()) return;
            var random = new System.Random(104729 + rules.Round * 7919);
            foreach (var candy in candies)
            {
                if (!rules.Release(rules.Round, candy.id, candy.kind)) continue;
                // Distinct separated ejection positions: finite preauthored pool,
                // no all-pieces overlap at the shell center or teleport to owners.
                float angle = candy.id * 2.39996323f; int ring = candy.id / 12;
                var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var position = origin + radial * (2.1f + ring * 1.05f) + Vector3.up * (1.1f + candy.id % 3 * .95f);
                var velocity = radial * (5 + (float)random.NextDouble() * 5) + Vector3.up * (4 + (float)random.NextDouble() * 4);
                candy.Release(rules.Round, position, Quaternion.Euler(candy.id * 37, candy.id * 61, candy.id * 19), velocity);
            }
            festival?.Rupture(origin); feedback.PlayAt(5, origin, 0, true);
        }
        public bool Collect(PinataCandy candy, SportsEventTruck truck)
        {
            if (!CanCollect || candy == null || truck == null || truck.session != this || !candy.Released || candy.id < 0 || candy.id >= candies.Length || candies[candy.id] != candy) return false;
            if (!rules.Collect(candy.Generation, candy.id, truck.slot, out int delta)) return false;
            NativePickups++;
            LastCollectorSlot = truck.slot; lastCollectionAt = rules.Clock;
            if (candy.kind == PinataCandyKind.Explosive)
            {
                NativeExplosions++; var away = Vector3.ProjectOnPlane(truck.body.worldCenterOfMass - candy.body.position, Vector3.up).normalized;
                if (away.sqrMagnitude < .1f) away = -truck.transform.forward;
                truck.tractorMotor.AddAssemblyVelocity(away * 3 + Vector3.up * 1.8f);
                festival?.Explosion(candy.body.position); feedback.PlayAt(4, candy.body.position, 0, true);
            }
            else { festival?.Pickup(candy.body.position, candy.kind == PinataCandyKind.Golden); feedback.PlayAt(3, candy.body.position, 0, true); }
            if (delta != 0) ConfirmScore("candy:" + candy.Generation + ":" + candy.id, truck.slot, delta, rules.points[truck.slot], candy.kind == PinataCandyKind.Golden ? "GOLDEN CANDY" : candy.kind == PinataCandyKind.Explosive ? "EXPLOSIVE SWEET" : "CANDY COLLECTED", candy.kind == PinataCandyKind.Golden ? SportsScoreTier.Major : SportsScoreTier.Small);
            else if (candy.kind == PinataCandyKind.Explosive) ConfirmOutcome("candy:" + candy.Generation + ":" + candy.id, truck.slot, 1, "EXPLOSIVE SWEET", "EXPLOSION", SportsScoreTier.Small);
            return true;
        }
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming source)
        {
            if (pinata == null || pinata.body == null) return;
            bool collecting = rules.Phase == PinataPhase.Candy;
            var point = pinata.body.position; bool candyObjective = false;
            int recent = collecting ? rules.Clock - lastCollectionAt < 3 ? LastCollectorSlot : -1 : rules.Clock - lastImpactAt < 3 ? LastImpactSlot : -1;
            if (collecting)
            {
                float nearest = float.MaxValue; var anchor = recent >= 0 ? trucks[recent].body.position : point;
                foreach (var candy in candies)
                {
                    if (!candy.Released || candy.kind == PinataCandyKind.Explosive) continue;
                    float cost = (candy.body.position - anchor).sqrMagnitude / (candy.kind == PinataCandyKind.Golden ? 2 : 1);
                    if (cost < nearest) { nearest = cost; point = candy.body.position; candyObjective = true; }
                }
                if (!candyObjective && LastCollectorSlot >= 0) point = trucks[LastCollectorSlot].body.position;
            }
            action.primary = recent >= 0 && recent < setup.count ? recent : source.Nearest(point, true);
            if (action.primary < 0) action.primary = source.Nearest(point);
            action.Objective(point, candyObjective ? 1 : 5);
            action.reason = collecting ? candyObjective ? "PHYSICAL CANDY / LAST COLLECTOR" : "ACTUAL CANDY COLLECTOR" : "HANGING PIÑATA / REAL IMPACT";
        }
        protected override void Present(float dt)
        {
            if (drivingViews == null || !Running) return;
            drivingViews.Tick(dt); if (!drivingViews.NeedsArena || broadcast == null || !broadcast.LegacyPoseAllowed) return;
            var bounds = new Bounds(new Vector3(0, 5, 0), new Vector3(65, 14, 65));
            for (int i = 0; i < setup.count; i++) bounds.Encapsulate(trucks[i].body.position);
            framing.arenaBounds = bounds; broadcast.Pose(bounds.center, new Vector3(0, 65, -72), 60); framing.Apply();
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); }
        protected override void SetPaused(bool paused) { festival?.Pause(paused); }
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) if (truck != null) truck.StopSound(); if (!Paused) festival?.StopSound(); }
        protected override void StopGame()
        {
            if (pinata != null) pinata.Freeze(); if (candies != null) foreach (var candy in candies) if (candy != null) candy.Retire();
            if (trucks != null) foreach (var truck in trucks) if (truck != null) { truck.Power(false); if (!truck.body.isKinematic) truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero; truck.body.isKinematic = true; }
            StopSounds(); festival?.StopPresentation();
        }
    }
}
