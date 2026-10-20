using System;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckFareFight;
using LWS.InterstateHauler;
namespace LWS.TruckKingOfTheHill
{
    public sealed class HillMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public SportsRecordedReview review;
        public Vector3[] sites;
        public string[] siteNames;
        public LwsRoadGraph roads = new LwsRoadGraph();
        public Transform ring, nextRing, beacon;
        public Renderer ringRenderer, nextRenderer;
        public float radius = 11;
        public float relocationSeconds = 40;
        public HillRules rules = new HillRules();
        public float Countdown { get; private set; }
        public int Recoveries { get; private set; }
        public override string Title => "TRUCK KING OF THE HILL";
        public override int MinimumPlayers => 2;
        public override int MaximumPlayers => 8;
        public override int OptionCount => 3;
        public override int WeatherCount => 1;
        public override string OptionLabel => new[] { "30 SECONDS CONTROL", "60 SECONDS CONTROL", "90 SECONDS CONTROL" }[option];
        public override bool SupportsSplitAIViews => true;
        public override int CompactScoreColumns => 4;
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Live && Countdown > 0 ? Countdown : -1;
        public override float RaceStartSeconds => Countdown <= 0 ? rules.Clock : -1;
        public override bool ReplayControls => review != null && review.Playing;
        public override bool MultiViewHUD => !ReplayControls && drivingViews != null && !drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible => !ReplayControls;
        public override float ParticipantProgress(int slot) => rules.Target > 0 ? rules.control[slot] / rules.Target : 0;
        public override string Header => Title + (ReplayControls ? " · RECORDED REPLAY" : " · " + (sites != null && sites.Length > rules.Zone ? siteNames[rules.Zone] : "CROWN PLATEAU"));
        public override string Status => Countdown > 0 ? "REACH THE ILLUMINATED CONTROL RING" : rules.Warning ? "HILL MOVING IN " + rules.UntilMove.ToString("F1") + "s · NEXT: " + siteNames[rules.NextZone] : rules.Occupants > 1 ? "CONTESTED · NO CONTROL POINTS" : rules.Controller >= 0 ? setup.players[rules.Controller].name + " CONTROLS THE HILL · " + rules.control[rules.Controller].ToString("F1") + " / " + rules.Target + "s" : "HILL OPEN · DRIVE INTO THE RING";
        public override string ControlCaption(int slot) => base.ControlCaption(slot).Replace(" ACTION", " RECOVER");
        public override string Help => "Eight trucks fight for a moving illuminated mountaintop control zone. One physically present, upright and grounded truck earns control seconds. Two or more opponents contest it and nobody scores; an overturned truck still physically contests while it remains in the ring. Distant compound colliders and airborne passes cannot award control. Default victory requires 60 cumulative uncontested seconds; choose 30 or 90 in setup. Every 40 seconds by default the hill relocates to an accessible connected plateau, with a three-second preview ring at the announced next site. Use the broad authored roads, bridges and ridges; shove rivals out using actual NWH collisions. Falling from a cliff costs recovery time. ACTION only recovers a genuinely stuck/overturned truck at a separated nearby safe plateau; it cannot warp a moving truck to the next hill. AI uses the existing LWS road planner and Fare Fight driving adapter, navigates the same roads, contests and physically rams; no hidden points. Shared arena/chase/full cab, free look, optional AI views, crew colors/names, shared weather and horn use collection controls. Match-end replay records actual movement and never changes the winner.";
        readonly bool[] present = new bool[8], eligible = new bool[8];
        readonly float[] stuck = new float[8], nextPlan = new float[8], fallenAt = new float[8];
        readonly int[] revisions = new int[8];
        readonly FareFightRoute[] drivers = new FareFightRoute[8];
        readonly int[] combatTargets = new int[8];
        readonly bool[] committed = new bool[8];
        readonly float[] commitUntil = new float[8], reconsiderAt = new float[8];
        readonly Vector3[] commitDirection = new Vector3[8];
        readonly RaycastHit[] supportHits = new RaycastHit[32];
        MaterialPropertyBlock tint;
        readonly StringBuilder text = new StringBuilder(700);
        public override bool ValidateSources(out string reason)
        {
            if (!base.ValidateSources(out reason)) return false;
            int humans = 0; for (int i = 0; i < setup.count; i++) if (setup.players[i].source != BowlingSource.AI) humans++;
            if (humans <= 4) return true; reason = "Up to four local humans; assign additional competitors to AI."; return false;
        }
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        { value = rules.control[slot].ToString("F1") + "s"; detail = fallenAt[slot] >= 0 ? "RECOVERING" : present[slot] ? rules.Occupants > 1 ? "CONTESTING" : eligible[slot] ? "CONTROLLING" : "GET UPRIGHT" : "REACH THE HILL"; return slot < setup.count; }
        public override string ViewText(int view) { int slot = drivingViews.rally.owners[view]; TryReadScoreCard(slot, out var score, out var detail); return setup.players[slot].name + " · " + score + " / " + rules.Target + "s\n" + detail + "\n" + Status; }
        public override string Standings { get { text.Clear(); for (int i = 0; i < setup.count; i++) text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.control[i].ToString("F1")).Append(" / ").Append(rules.Target).Append("s CONTROL\n"); return text.ToString(); } }
        public override string Results => (rules.Winner >= 0 ? setup.players[rules.Winner].name + " IS KING!" : "CONTROL RESULTS") + "\n\n" + Standings;
        public Vector3 Grid(int slot) { float a = slot * Mathf.PI * 2 / Mathf.Max(2, setup.count); return sites[0] + new Vector3(Mathf.Sin(a) * 32, .65f, Mathf.Cos(a) * 32); }
        protected override void BeginGame()
        {
            if (review == null) { review = gameObject.AddComponent<SportsRecordedReview>(); review.match = this; review.trucks = trucks; review.fragments = fragments; review.drivingViews = drivingViews; review.broadcast = broadcast; review.framing = framing; review.atmosphere = atmosphere; review.watchAction = "watchhill"; review.extras = new[] { ring, nextRing, beacon }; }
            review.ResetRecord(); rules.Reset(setup.count, sites.Length, Environment.TickCount, new[] { 30f, 60f, 90f }[option], Mathf.Max(10, relocationSeconds)); Countdown = 3; Recoveries = 0;
            tint ??= new MaterialPropertyBlock();
            for (int i = 0; i < trucks.Length; i++)
            {
                drivers[i] ??= new FareFightRoute(new LwsNavigationTuning { routeConnectorToleranceMeters = .5f }); drivers[i].Reset(); revisions[i] = combatTargets[i] = -1; committed[i] = false; commitUntil[i] = reconsiderAt[i] = 0; stuck[i] = nextPlan[i] = 0; fallenAt[i] = -1; present[i] = eligible[i] = false;
                fragments[i].ResetBody(); trucks[i].gameObject.SetActive(i < setup.count); if (i >= setup.count) continue;
                trucks[i].tractorMotor.Configure(); trucks[i].Prepare(Grid(i), Quaternion.LookRotation(Vector3.ProjectOnPlane(sites[0] - Grid(i), Vector3.up))); Hold(trucks[i]);
            }
            drivingViews.Layout(); atmosphere.Configure(Environment.TickCount, 0, 1);
        }
        static void Hold(SportsEventTruck truck) { var input = truck.tractorMotor.vehicle.input; input.states.throttle = input.states.throttleRaw = input.states.brakes = input.states.brakesRaw = 0; input.states.handbrake = input.states.handbrakeRaw = 1; }
        protected override void Simulate(float dt)
        {
            atmosphere.Tick(rules.Clock);
            if (Countdown > 0) { Countdown = Mathf.Max(0, Countdown - dt); for (int i = 0; i < setup.count; i++) Hold(trucks[i]); return; }
            var center = sites[rules.Zone];
            for (int i = 0; i < setup.count; i++)
            {
                var truck = trucks[i]; var point = truck.body.worldCenterOfMass;
                present[i] = fallenAt[i] < 0 && truck.body.detectCollisions && Mathf.Abs(point.y - center.y) < 4.5f && Vector3.ProjectOnPlane(point - center, Vector3.up).sqrMagnitude <= radius * radius;
                eligible[i] = present[i] && truck.tractorMotor.Grounded && Vector3.Dot(truck.transform.up, Vector3.up) > .55f;
            }
            int oldZone = rules.Zone; rules.Step(dt, present, eligible); review.Capture(rules.Clock + 3);
            if (oldZone != rules.Zone) { feedback.PlayAt(5, sites[rules.Zone], 0, true); Array.Clear(present, 0, 8); Array.Clear(eligible, 0, 8); }
            if (rules.Complete) { review.focusSlot = rules.Winner; EndMatch(); return; }
            for (int i = 0; i < setup.count; i++)
            {
                var truck = trucks[i]; truck.tractorMotor.gripScale = atmosphere.Grip;
                stuck[i] = truck.body.linearVelocity.magnitude < .5f && !present[i] ? stuck[i] + dt : 0;
                bool fallen = truck.body.position.y < 70 || Mathf.Abs(truck.body.position.x) > 170 || Mathf.Abs(truck.body.position.z) > 160;
                if (fallen && fallenAt[i] < 0) fallenAt[i] = rules.Clock;
                if (fallenAt[i] >= 0) { Hold(truck); if (rules.Clock - fallenAt[i] > 2) Recover(i, true); continue; }
                if (setup.players[i].source == BowlingSource.AI)
                {
                    if (rules.Controller == i) Hold(truck); else truck.TickDrive(Bot(i, dt));
                    if (Vector3.Dot(truck.transform.up, Vector3.up) < .25f || stuck[i] > 22) Recover(i, false);
                }
                else { truck.TickDrive(commands[i]); if (actions[i]) Recover(i, false); }
            }
        }
        Vector2 Bot(int slot, float dt)
        {
            var truck = trucks[slot]; var p = truck.body.position; var center = sites[rules.Zone]; var driver = drivers[slot]; Rigidbody opponent = null;
            if (Vector3.ProjectOnPlane(p - center, Vector3.up).magnitude < 24 && Mathf.Abs(p.y - center.y) < 5)
            {
                Vector3 goal = center; int target = combatTargets[slot];
                if (target < 0 || target >= setup.count || !present[target] || !committed[slot] && rules.Clock >= reconsiderAt[slot])
                {
                    target = -1; float nearest = float.MaxValue;
                    for (int i = 0; i < setup.count; i++) if (i != slot && present[i]) { float d = (trucks[i].body.position - p).sqrMagnitude; if (d < nearest) { nearest = d; target = i; } }
                    combatTargets[slot] = target; committed[slot] = false; reconsiderAt[slot] = rules.Clock + 5;
                }
                if (target >= 0)
                {
                    opponent = trucks[target].body;
                    var outward = Vector3.ProjectOnPlane(opponent.position - center, Vector3.up);
                    if (outward.sqrMagnitude < .5f) outward = Quaternion.Euler(0, slot * 47, 0) * Vector3.forward;
                    outward.Normalize(); var behind = opponent.position - outward * 9;
                    if (committed[slot] && rules.Clock >= commitUntil[slot]) committed[slot] = false;
                    if (!committed[slot] && Vector3.Dot(p - opponent.position, outward) < -2 && Vector3.Distance(p, behind) < 10 && Vector3.Angle(Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up), outward) < 50)
                    { committed[slot] = true; commitUntil[slot] = rules.Clock + 3; commitDirection[slot] = outward; }
                    // Keep one opponent and shove direction long enough to
                    // complete a physical ram instead of oscillating between
                    // nearest neighbors at every physics sample.
                    goal = committed[slot] ? opponent.position + commitDirection[slot] * (radius + 12) : behind;
                }
                driver.DirectGoal(p, goal); revisions[slot] = -1;
            }
            else if (revisions[slot] != rules.Moves || rules.Clock >= nextPlan[slot] || driver.points.Count == 0)
            {
                // Use the nearest physical road node. The city driver's optional
                // heading shortcut can pick a distant node across empty air
                // between these plateaus, bypassing the actual bridge approach.
                driver.Update(roads, p, center, rules.Moves, rules.Clock, revisions[slot] < 0);
                revisions[slot] = rules.Moves; nextPlan[slot] = rules.Clock + .5f; combatTargets[slot] = -1; committed[slot] = false;
            }
            var command = driver.Drive(truck.body, truck.transform, (int)setup.players[slot].difficulty, dt, atmosphere.Grip, opponent);
            // The route planner supplies the path; a local physical support
            // check brakes before an exposed cliff. It never adds forces or
            // moves the truck. Other vehicles do not count as safe road.
            float speed = Mathf.Max(0, Vector3.Dot(truck.body.linearVelocity, truck.transform.forward));
            var ahead = p + truck.transform.forward * Mathf.Max(7, speed * .8f);
            int hits = Physics.RaycastNonAlloc(ahead + Vector3.up * 5, Vector3.down, supportHits, 13, ~0, QueryTriggerInteraction.Ignore); bool supported = false;
            for (int i = 0; i < hits; i++) if (supportHits[i].rigidbody == null && supportHits[i].normal.y > .45f && Mathf.Abs(supportHits[i].point.y - p.y) < 7) supported = true;
            if (!supported && command.y > 0) command.y = -.8f;
            return command;
        }
        public bool Recover(int slot, bool fallen)
        {
            var truck = trucks[slot]; if (!fallen && Vector3.Dot(truck.transform.up, Vector3.up) > .5f && (truck.body.linearVelocity.magnitude > .8f || stuck[slot] < 3)) return false;
            int site = 0; float nearest = float.MaxValue; for (int i = 0; i < sites.Length; i++) { float d = Vector3.ProjectOnPlane(truck.body.position - sites[i], Vector3.up).sqrMagnitude; if (d < nearest) { nearest = d; site = i; } }
            for (int k = 0; k < 16; k++)
            {
                float a = (slot + k) * Mathf.PI / 4; var point = sites[site] + new Vector3(Mathf.Sin(a) * 23, .65f, Mathf.Cos(a) * 23); bool clear = true;
                for (int i = 0; i < setup.count; i++) if (i != slot && Vector3.Distance(trucks[i].body.position, point) < 11) clear = false;
                if (!clear) continue;
                truck.Prepare(point, Quaternion.LookRotation(Vector3.ProjectOnPlane(sites[site] - point, Vector3.up))); fallenAt[slot] = -1; nextPlan[slot] = stuck[slot] = 0; drivers[slot].Reset(); combatTargets[slot] = -1; committed[slot] = false; Recoveries++; return true;
            }
            return false;
        }
        protected override void Present(float dt)
        {
            if (drivingViews == null) return; if (ReplayControls) { review.Tick(); return; } drivingViews.Tick(dt);
            if (sites == null || sites.Length == 0) return;
            ring.position = sites[rules.Zone] + Vector3.up * .12f; beacon.position = ring.position + Vector3.up * 8;
            nextRing.position = sites[rules.NextZone] + Vector3.up * .14f; nextRing.localScale = rules.Warning ? Vector3.one * (1 + Mathf.Sin(Time.time * 7) * .025f) : Vector3.zero;
            var color = rules.Occupants > 1 ? new Color(1, .24f, .04f) : rules.Controller >= 0 ? setup.players[rules.Controller].color : new Color(1, .72f, .05f);
            tint ??= new MaterialPropertyBlock(); tint.SetColor("_BaseColor", color); tint.SetColor("_EmissionColor", color * 2); ringRenderer.SetPropertyBlock(tint);
            if (drivingViews.NeedsArena)
            {
                var area = new Bounds(sites[rules.Zone] + Vector3.up * 5, new Vector3(76, 28, 76));
                for (int i = 0; i < setup.count; i++) if (setup.players[i].source != BowlingSource.AI && fallenAt[i] < 0) area.Encapsulate(trucks[i].body.position);
                if (rules.Warning) area.Encapsulate(sites[rules.NextZone] + Vector3.up * 8);
                area.Expand(new Vector3(12, 10, 12)); broadcast.view.orthographic = true; broadcast.Pose(area.center, new Vector3(0, 105, -155), 60); broadcast.transform.rotation = Quaternion.LookRotation(new Vector3(0, -105, 155), Vector3.up); framing.arenaBounds = area; framing.Apply();
            }
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); else review?.Action(key); }
        protected override void SetPaused(bool pause) => atmosphere?.Pause(pause);
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) truck.StopSound(); }
        protected override void StopGame() { review?.End(false); StopSounds(); if (trucks == null) return; for (int i = 0; i < trucks.Length; i++) { trucks[i].Power(false); trucks[i].body.isKinematic = true; fragments[i].Freeze(); } }
    }
}
