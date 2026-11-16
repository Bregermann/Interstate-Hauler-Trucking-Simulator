using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonEscape
{
    public sealed class DragonEscapeMatch : DragonKingdomGroundMatch
    {
        public DragonRouteHazard[] routeHazards = Array.Empty<DragonRouteHazard>();
        [Min(60)] public float timeLimitSeconds = 480;
        public DragonEscapeScenarioSnapshot Scenario { get; private set; }
        public float[] ExtractionTimes { get; } = new float[] { -1, -1, -1, -1 };
        public int[] Placements { get; } = new int[4];
        public int[] Scores { get; } = new int[4];
        public IReadOnlyList<string> BlockedEdges => blocked;
        public int ActualExtractions { get; private set; }
        public int CommittedClosures { get; private set; }
        public bool Cooperative => option == 1;
        protected override bool UseInPlaceRecovery => true;
        readonly List<string> blocked = new List<string>();
        readonly float[] secureSeconds = new float[4];
        readonly bool[] extractedThisStep = new bool[4];
        readonly bool[] enteredApproach = new bool[4];
        readonly bool[] farParking = new bool[4], farParkingClaimed = new bool[2];
        DragonExtractionSite site;
        DragonRouteHazard scheduled;
        DragonCharacter collapseDragon;
        float closureAt, finishAge;
        bool closureAttempted, collapseContact;
        public override string Title => "TRUCK DRAGON ESCAPE";
        public override string Help => "ESCAPE THE WYRM\nDrive the same seeded kingdom escape as your rivals. Follow your route to the announced refuge: fully inside the fortress, inside the mountain chamber, or onto the evacuation boat. Stop upright within the inner zone to secure your truck. For the boat, drive up the ramp, settle your wheels on its deck, and wait for the announced departure.\n\nDragons follow visible or audible evidence. Horns and fast driving carry farther; take cover and change routes. Warned structures can collapse into real debris. Your GPS preserves a usable detour for every surviving crew. If overturned or stuck, stop and press ACTION for a physical self-righting attempt. Two attempts are available; each adds six seconds to your recorded escape time. Damage remains. Wrecked or unrecoverable crews enter spectator view.\n\nCompetitive crews receive placements for genuine extraction; same-step ties share a place. Cooperative Convoy wins when every crew extracts. The deadline closes the rescue window. Choose chase, cab or shared broadcast; optional AI views change camera space.";
        public override int OptionCount => 2;
        public override string OptionLabel => Cooperative ? "COOPERATIVE CONVOY" : "COMPETITIVE EXTRACTION";
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Intro > 0 ? Intro : -1;
        public override float RaceStartSeconds => Intro <= 0 ? Clock : -1;
        public override string Header => "DRAGON ESCAPE · " + (Scenario == null ? "CREW READY" : Scenario.Extraction.Kind.ToString().ToUpperInvariant()) + " · " + Mathf.CeilToInt(Mathf.Max(0, timeLimitSeconds - Clock)) + "s";
        public override string Status => site?.vessel != null && site.vessel.DepartureRequested && !site.vessel.Sailing
            ? "EVACUATION DEPARTS IN " + Mathf.CeilToInt(site.vessel.SecondsUntilDeparture) + "s · BOARD NOW"
            : Announcement;
        public override string Standings => string.Join("\n", Enumerable.Range(0, setup.count).Select(i => setup.players[i].name + " · " + Detail(i)));
        public override string Results => (Cooperative ? ActualExtractions == setup.count ? "CONVOY RESCUED" : "CONVOY RESCUE FAILED" : ActualExtractions > 0 ? "EXTRACTION RESULTS" : "NO CREW EXTRACTED") + "\n" + Standings;
        string Detail(int slot) => ExtractionTimes[slot] >= 0 ? "#" + Placements[slot] + " · " + ExtractionTimes[slot].ToString("0.0") + "s" : Retired[slot] ? "LOST IN THE KINGDOM" : "SEEKING REFUGE";
        public override bool TryReadScoreCard(int card, out string value, out string detail)
        {
            value = detail = null; if (card < 0 || card >= setup.count) return false;
            value = ExtractionTimes[card] >= 0 ? "#" + Placements[card] : (conditions[card].Health * 100).ToString("0") + "%";
            detail = Detail(card); return true;
        }
        public override float ParticipantProgress(int slot)
        {
            if (slot < 0 || slot >= setup.count || Scenario == null) return -1;
            return ExtractionTimes[slot] >= 0 ? 1 : Mathf.Clamp01(1 - Vector3.Distance(trucks[slot].body.position, Scenario.Extraction.Position)
                / Mathf.Max(1, Scenario.InitialRoutes[0].DistanceMeters));
        }

        protected override void PrepareObjectives()
        {
            if (routeHazards.Length < world.definition.closurePlans.Length)
                throw new InvalidOperationException("Every configured closure plan requires a real resettable CoreCollapse hazard.");
            if (!DragonEscapeScenario.TryCreate(world.definition.graph, MatchSeed, world.definition.escapeStarts, world.definition.extractions,
                world.definition.closurePlans, Scenario, out var scenario, out string reason)) throw new InvalidOperationException(reason);
            Scenario = scenario; startingRegionId = scenario.StartRegionId; site = world.Extraction(scenario.Extraction.Id);
            blocked.Clear(); SharedRoadRetracesCancelled = RepeatedRouteBuilds = 0; ActualExtractions = CommittedClosures = 0; finishAge = boatSecureAge = 0; closureAttempted = collapseContact = false;
            Array.Clear(secureSeconds, 0, 4); Array.Clear(Placements, 0, 4); Array.Clear(Scores, 0, 4);
            Array.Clear(enteredApproach, 0, enteredApproach.Length);
            Array.Clear(farParking, 0, farParking.Length); Array.Clear(farParkingClaimed, 0, farParkingClaimed.Length);
            for (int i = 0; i < 4; i++) ExtractionTimes[i] = -1;
            scheduled = routeHazards.FirstOrDefault(h => h != null && h.id == scenario.ClosurePlanId);
            if (scheduled == null || !scenario.PlannedClosures.SequenceEqual(scheduled.blockedEdges.OrderBy(x => x, StringComparer.Ordinal)))
                throw new InvalidOperationException("Physical collapse edges must exactly match the selected seeded scenario.");
            foreach (var hazard in routeHazards) hazard.ResetDragonMatch(MatchSeed);
            closureAt = 35 + (uint)MatchSeed % 20;
            Announcement = "ESCAPE TO " + scenario.Extraction.Kind.ToString().ToUpperInvariant() + " · SAME KINGDOM, SAME THREAT";
        }
        protected override void BeginDragonIntents()
        {
            var region = world.Region(startingRegionId);
            for (int i = 0; i < dragons.Length; i++)
            {
                var dragon = dragons[i];
                // Scenario staging happens before GO. During competition the
                // actor uses only its real perception and vendor locomotion.
                var point = region.root.position + new Vector3(i % 2 == 0 ? -100 : 100, 100 + i * 30, 35);
                dragon.motor.body.position = point;
                dragon.motor.body.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(region.root.position - point, Vector3.up));
                dragon.SetIntent(DragonCharacterIntent.Pursuit, region.root.position);
            }
            if (collapseDragon != null) collapseDragon.motor.ActionContact -= CollapseActionContact;
            collapseDragon = dragons.Length > 0 ? dragons[0] : null;
            if (collapseDragon != null) collapseDragon.motor.ActionContact += CollapseActionContact;
        }
        public int SharedRoadRetracesCancelled { get; private set; }
        public int RepeatedRouteBuilds { get; private set; }
        readonly int[] plannedSearchesBeforeDriver = new int[4];
        protected override bool CanFollowRoute(int slot, float dt)
        {
            var truck = trucks[slot];
            // The base repeats this same cached planning call, then invokes the
            // original driver exactly once. No steering/recovery state is stepped here.
            if (routes[slot].Update(liveGraph, truck.body.position, DrivingGoal(slot), RouteRevision, Time.time, false, truck.transform.forward) && routes[slot].Cursor == 0)
            {
                var plans = CommittedClosures == 0 ? Scenario.InitialRoutes : Scenario.Routes;
                if (plans.Count > 0) SharedRoadRetracesCancelled += CancelSharedRoadRetraces(routes[slot].points, liveGraph, plans[Math.Min(slot, plans.Count - 1)].EdgeIds);
            }
            plannedSearchesBeforeDriver[slot] = routes[slot].Searches;
            return true;
        }
        public static int CancelSharedRoadRetraces(List<Vector3> points, LwsRoadGraph graph, IReadOnlyList<string> edgeIds)
        {
            if (points == null || graph == null || graph.nodes == null || graph.edges == null || edgeIds == null || points.Count < 4) return 0;
            var node = graph.nodes.Find(n => n.nodeId == "caverns"); if (node == null) return 0;
            int cancelled = 0;
            for (int pair = 0; pair + 1 < edgeIds.Count; pair++)
            {
                string a = edgeIds[pair], b = edgeIds[pair + 1];
                bool Known(string id) => id == "castle-caverns" || id == "harbor-caverns";
                if (!(a == "caverns-volcano" && Known(b) || b == "caverns-volcano" && Known(a))) continue;
                var incoming = graph.edges.Find(e => e.edgeId == a); var outgoing = graph.edges.Find(e => e.edgeId == b);
                // This correction is only for the consecutive, bidirectional
                // authored cavern legs. The planner still owns every source edge.
                if (incoming == null || outgoing == null || incoming.oneWay || outgoing.oneWay || incoming.edgeId == outgoing.edgeId) continue;
                if (incoming.restrictions != null && incoming.restrictions.Any(r => r != null && !r.truckAllowed) || outgoing.restrictions != null && outgoing.restrictions.Any(r => r != null && !r.truckAllowed)) continue;
                if (!SharedStraightEnd(incoming, node.nodeId, node.position, out var cut) || !SharedStraightEnd(outgoing, node.nodeId, node.position, out var other) || !cut.Equals(other)) continue;
                int joint = points.FindIndex(p => p.Equals(node.position)); if (joint <= 0 || joint >= points.Count - 1) continue;
                int left = -1, right = -1;
                for (int i = joint - 1; i >= 0; i--) if (points[i].Equals(cut)) { left = i; break; }
                for (int i = joint + 1; i < points.Count; i++) if (points[i].Equals(cut)) { right = i; break; }
                if (left < 0 || right <= joint || right >= points.Count - 1) continue;
                var axis = cut - node.position; float length = axis.magnitude; if (length <= 0) continue; axis /= length;
                bool onStub = true; float last = length;
                for (int i = left; i <= right; i++)
                {
                    var delta = points[i] - node.position; float along = Vector3.Dot(delta, axis);
                    if (Vector3.Cross(delta, axis).sqrMagnitude > .00000001f || along < 0 || along > length || (i <= joint ? along > last : along < last)) { onStub = false; break; }
                    last = along;
                }
                if (!onStub) continue;
                // Both cutpoints are the identical original Vector3. Removing
                // only the excursion creates no new point or physical segment;
                // start, goal and every remaining waypoint retain their bytes/order.
                points.RemoveRange(left + 1, right - left); cancelled++;
            }
            return cancelled;
        }
        static bool SharedStraightEnd(LwsRoadEdge edge, string node, Vector3 origin, out Vector3 end)
        {
            end = origin; bool from = edge.fromNodeId == node; if (!from && edge.toNodeId != node || edge.samples == null || edge.samples.Count < 2) return false;
            int first = from ? 0 : edge.samples.Count - 1, step = from ? 1 : -1;
            if (!edge.samples[first].position.Equals(origin)) return false;
            Vector3 axis = Vector3.zero; float previous = 0;
            for (int i = first + step; i >= 0 && i < edge.samples.Count; i += step)
            {
                var delta = edge.samples[i].position - origin;
                if (delta.sqrMagnitude == 0) continue;
                if (axis.sqrMagnitude == 0) axis = delta.normalized;
                float along = Vector3.Dot(delta, axis);
                if (Vector3.Cross(delta, axis).sqrMagnitude > .00000001f || along <= previous) break;
                previous = along; end = edge.samples[i].position;
            }
            return !end.Equals(origin);
        }

        protected override Vector3 DrivingGoal(int slot)
        {
            if (site == null) return trucks[slot].body.position;
            if (!enteredApproach[slot]) return ApproachGoal(slot);
            return ParkingGoal(slot);
        }
        Vector3 ApproachGoal(int slot) => site.approach.position + site.secureZone.transform.right * (slot % 2 == 0 ? -5 : 5);
        Vector3 ParkingGoal(int slot)
        {
            if (site.vessel != null)
            {
                // Distinct parking targets let four real trucks board without
                // every AI attempting to occupy the same point on the deck.
                var zone = site.vessel.parkingZone;
                // First arrivals use the far row so later arrivals can remain
                // nearer the loading ramp without driving through parked rigs.
                return zone.transform.TransformPoint(zone.center + new Vector3((slot % 2 == 0 ? -1 : 1) * 5, 0, !enteredApproach[slot] || farParking[slot] ? -10 : 8));
            }
            return site.secureZone.bounds.center + new Vector3((slot % 2 == 0 ? -1 : 1) * 9, 0, slot < 2 ? 8 : -8);
        }
        protected override string ObjectivePrompt(int slot) => site == null ? "PREPARE" : site.vessel != null && site.vessel.IsSecured(trucks[slot])
            ? "SECURED ABOARD · WAIT FOR DEPARTURE" : "REACH " + Scenario.Extraction.Kind.ToString().ToUpperInvariant() + " · STOP INSIDE TO SECURE";
        protected override Vector2 FilterDriveCommand(int slot, Vector2 drive, float dt)
        {
            if (routes[slot].Searches != plannedSearchesBeforeDriver[slot]) RepeatedRouteBuilds++;
            if (site?.vessel != null && site.vessel.IsSecured(trucks[slot])) return Vector2.zero;
            return drive;
        }
        protected override bool ShouldHoldPosition(int slot)
        {
            if (site == null) return false;
            if (site.vessel != null && site.vessel.IsSecured(trucks[slot])) return true;
            if (!enteredApproach[slot] || Vector3.ProjectOnPlane(trucks[slot].body.position - ParkingGoal(slot), Vector3.up).magnitude >= 3.5f) return false;
            return site.vessel != null ? site.vessel.HasSupportedParkingFootprint(trucks[slot]) : SupportedInnerFootprint(slot);
        }
        protected override void TickObjectives(float dt)
        {
            Array.Clear(extractedThisStep, 0, 4);
            if (!closureAttempted && Clock >= closureAt - 6) { scheduled.SetWarning(true); Announcement = "DRAGON ATTACK · ROAD COLLAPSE WARNING · FOLLOW THE DETOUR"; }
            if (!closureAttempted && Clock >= closureAt && collapseDragon != null)
            {
                collapseDragon.SetIntent(DragonCharacterIntent.Patrol, scheduled.PhysicalBounds.center + Vector3.up * 8);
                if (Vector3.Distance(collapseDragon.mouth.position, scheduled.PhysicalBounds.ClosestPoint(collapseDragon.mouth.position)) < collapseDragon.breathRange * .9f)
                    collapseDragon.RequestAttack(DragonAnimationAction.Breath);
                // An actual animated contact queues the graph/destruction
                // transaction for this simulation step. A schedule is no hit.
                if (collapseContact)
                {
                    closureAttempted = true; CommitClosure(scheduled); scheduled.SetWarning(false);
                    collapseDragon.SetIntent(DragonCharacterIntent.Pursuit, scheduled.PhysicalBounds.center);
                }
                else if (Clock > closureAt + 90)
                {
                    closureAttempted = true; scheduled.SetWarning(false); Announcement = "DRAGON ATTACK DIVERTED · ROAD REMAINS OPEN";
                    collapseDragon.SetIntent(DragonCharacterIntent.Pursuit, scheduled.PhysicalBounds.center);
                }
            }
            for (int i = 0; i < setup.count; i++)
            {
                if (actions[i] && !Retired[i] && recoverAt[i] <= 0 && trucks[i].body.linearVelocity.magnitude < 1.5f)
                    BeginRecovery(i, "REQUESTED RECOVERY");
                if (Retired[i] || recoverAt[i] > 0 || conditions[i].Destroyed) { secureSeconds[i] = 0; continue; }
                if (!enteredApproach[i])
                {
                    var delta = Vector3.ProjectOnPlane(ApproachGoal(i) - trucks[i].body.position, Vector3.up);
                    var heading = Vector3.ProjectOnPlane(ParkingGoal(i) - ApproachGoal(i), Vector3.up);
                    // The normal road driver must actually reach and line up
                    // at the authored entrance. No truck pose is corrected here.
                    if (trucks[i].tractorMotor.Grounded && (delta.magnitude < 6 && Vector3.Angle(trucks[i].transform.forward, heading) < 35
                        || site.vessel != null && site.vessel.HasSupportedParkingFootprint(trucks[i]) || site.vessel == null && SupportedInnerFootprint(i)))
                    {
                        int lane=i%2;farParking[i]=!farParkingClaimed[lane];farParkingClaimed[lane]=true;enteredApproach[i] = true;
                    }
                }
                if (site.vessel != null)
                {
                    site.vessel.ObserveBoarding(trucks[i], dt, Scenario.Extraction.SecureSeconds, Scenario.Extraction.MaximumRelativeSpeed);
                    extractedThisStep[i] = site.vessel.HasExtracted(trucks[i], Scenario.Extraction.MinimumBoatDepartureDistance);
                }
                else
                {
                    secureSeconds[i] = InInnerRefuge(i) ? secureSeconds[i] + dt : 0;
                    extractedThisStep[i] = secureSeconds[i] >= Scenario.Extraction.SecureSeconds;
                }
            }
            int sharedRank = ActualExtractions + 1;
            for (int i = 0; i < setup.count; i++) if (extractedThisStep[i])
            {
                Retired[i] = true; ExtractionTimes[i] = Clock + Recoveries[i] * 6; Placements[i] = sharedRank;
                Scores[i] = setup.count + 1 - sharedRank; ActualExtractions++;
                ConfirmScore("extraction:" + i, i, Scores[i], Scores[i], "PHYSICALLY EXTRACTED · " + Scenario.Extraction.Kind, SportsScoreTier.Major, "PLACE PTS", roundEnded:true);
                Announcement = setup.players[i].name + " · EXTRACTED #" + sharedRank; feedback.Play(5, 0, true);
            }
            if (site.vessel != null && !site.vessel.DepartureRequested && site.vessel.SecuredCount > 0)
            {
                int waiting = 0; for (int i = 0; i < setup.count; i++) if (!Retired[i] && !site.vessel.IsSecured(trucks[i])) waiting++;
                // Co-op does not abandon an active partner. Competitive boarding
                // has a visible40second opportunity before its final8second call.
                if (waiting == 0 || !Cooperative && BoatFirstSecureAge(dt) >= 40) site.vessel.RequestDeparture();
            }
            if (Clock >= timeLimitSeconds)
                for (int i = 0; i < setup.count; i++) if (!Retired[i]) { Retired[i] = true; Announcement = "EXTRACTION WINDOW CLOSED"; }
            bool allDone = true; for (int i = 0; i < setup.count; i++) allDone &= Retired[i];
            finishAge = allDone ? finishAge + dt : 0;
            if (finishAge > 2) EndMatch();
        }
        float boatSecureAge;
        float BoatFirstSecureAge(float dt) { boatSecureAge += dt; return boatSecureAge; }
        void CollapseActionContact(DragonAnimationAction action)
        {
            if (!Live || closureAttempted || collapseDragon == null || action != DragonAnimationAction.Breath || scheduled == null) return;
            var origin = collapseDragon.mouth.position; var point = scheduled.PhysicalBounds.ClosestPoint(origin); var delta = point - origin;
            if (delta.magnitude > collapseDragon.breathRange || Vector3.Angle(collapseDragon.mouth.forward, delta) > collapseDragon.breathHalfAngle) return;
            // Actual physical obstruction takes precedence over a picturesque
            // background structure merely falling inside a sphere query.
            if (Physics.Raycast(origin, delta.normalized, out var hit, delta.magnitude + .3f, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(scheduled.transform)) return;
            collapseContact = true;
        }
        bool InInnerRefuge(int slot)
        {
            var truck = trucks[slot];
            return truck.body.linearVelocity.magnitude <= Scenario.Extraction.MaximumRelativeSpeed
                && truck.body.angularVelocity.magnitude <= .35f && SupportedInnerFootprint(slot);
        }
        bool SupportedInnerFootprint(int slot)
        {
            var truck = trucks[slot];
            if (!truck.tractorMotor.Grounded || Vector3.Dot(truck.transform.up, Vector3.up) < .9f) return false;
            var bounds = truck.shape.bounds;
            for (int corner = 0; corner < 4; corner++)
            {
                var p = bounds.center + new Vector3((corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x, 0, (corner & 2) == 0 ? -bounds.extents.z : bounds.extents.z);
                if ((site.secureZone.ClosestPoint(p) - p).sqrMagnitude > .0001f) return false;
            }
            return true;
        }
        bool CommitClosure(DragonRouteHazard hazard)
        {
            var candidate = new List<string>(blocked); candidate.AddRange(hazard.blockedEdges);
            var origins = new List<string>();
            for (int i = 0; i < setup.count; i++) if (!Retired[i])
            {
                string nearest = null; float distance = float.PositiveInfinity;
                foreach (var node in liveGraph.nodes)
                { float d = (node.position - trucks[i].body.position).sqrMagnitude; if (d < distance) { distance = d; nearest = node.nodeId; } }
                if (nearest != Scenario.Extraction.NodeId) origins.Add(nearest);
            }
            if (origins.Count > 0 && !DragonEscapeScenario.TryReplan(Scenario, origins, candidate, out _, out _))
            { Announcement = "REFUGE ROUTE PROTECTED · DRAGON ATTACK DIVERTED"; return false; }
            if (!hazard.Trigger()) { Announcement = "ROAD REMAINS OPEN"; return false; }
            blocked.AddRange(hazard.blockedEdges); liveGraph.edges.RemoveAll(e => blocked.Contains(e.edgeId));
            RouteRevision++; CommittedClosures++; Announcement = "ROAD DESTROYED · GPS DETOUR ACTIVE"; return true;
        }
        protected override void ParticipantRecovered(int slot) { secureSeconds[slot] = 0; }
        protected override void ParticipantEliminated(int slot) { secureSeconds[slot] = 0; }
        protected override void ParticipantRecoveryBeginning(int slot) { site?.vessel?.ReleaseTruck(trucks[slot]); secureSeconds[slot] = 0; }
        protected override bool DragonMayTarget(int slot) => base.DragonMayTarget(slot) && (site?.vessel == null || !site.vessel.IsSecured(trucks[slot]));
        protected override void StopGame()
        {
            if (collapseDragon != null) collapseDragon.motor.ActionContact -= CollapseActionContact;
            foreach (var hazard in routeHazards) if (hazard != null) hazard.SetWarning(false);
            base.StopGame();
        }
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing)
        {
            int active = -1; for (int i = 0; i < setup.count; i++) if (!Retired[i]) { active = i; break; }
            action.primary = active; action.preferHuman = true; action.reason = "DRAGON PURSUIT / REAL EXTRACTION";
            if (Scenario != null) action.Objective(site?.vessel != null ? site.vessel.hull.position : Scenario.Extraction.Position, 15);
        }
    }
}
