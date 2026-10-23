using System;
using System.Text;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckHotPotato
{
    public sealed class HotPotatoMatch : SportsEventSession
    {
        public override bool BroadcastParticipantActive(int slot)=>rules.alive[slot];
        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){action.primary=rules.Carrier;if(rules.Carrier>=0)action.Objective(trucks[rules.Carrier].body.position,1);action.reason="BOMB CARRIER / NEARBY CHALLENGERS";}
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public ParticleSystem[] blasts;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public SportsRecordedReview review;
        public Transform bomb;
        public Renderer bombGlow;
        public TMP_Text bombClock;
        public ParticleSystem fuseSparks;
        public float arenaRadius = 64, carrierPowerBoost = 1.15f;
        [Min(1)] public float urgentFuseSeconds = 6;
        public HotPotatoRules rules = new HotPotatoRules();
        public LWS.TruckFieldGoal.FieldGoalCrowd crowd;
        public LWS.TruckPedestrianPlowing.PlowBreakable[] breakables;
        Vector3[] propPositions; Quaternion[] propRotations;
        public float Countdown { get; private set; }
        public int Explosions { get; private set; }
        public override string Title => "TRUCK HOT POTATO";
        public override int MinimumPlayers => 2;
        public override int MaximumPlayers => 8;
        public override bool ValidateSources(out string reason)
        {
            if (!base.ValidateSources(out reason)) return false;
            int humans = 0; for (int i = 0; i < setup.count; i++) if (setup.players[i].source != BowlingSource.AI) humans++;
            if (humans <= 4) return true;
            reason = "Up to four local human drivers. Set additional competitors to AI."; return false;
        }
        public override int OptionCount => 1;
        public override int WeatherCount => 1;
        public override string OptionLabel => "CONTACT PASS · LAST TRUCK STANDING";
        public override bool SupportsSplitAIViews => true;
        public override int CompactScoreColumns => 4;
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Live && Countdown > 0 ? Countdown : -1;
        public override float RaceStartSeconds => Countdown <= 0 ? rules.Clock : -1;
        public override bool ReplayControls => review != null && review.Playing;
        public override bool MultiViewHUD => !ReplayControls && drivingViews != null && !drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible => !ReplayControls;
        public override string Header => ReplayControls ? Title + " · RECORDED REPLAY" : Title + " · " + rules.AliveCount() + " SURVIVORS · CYCLE " + rules.Cycle;
        public override string Status => Countdown > 0 ? "GET READY" : rules.Carrier >= 0 ? setup.players[rules.Carrier].name + " HAS THE BOMB · " + rules.Fuse.ToString("F1") + "s" + (rules.Cooldown > 0 ? " · TRANSFER COOLING " + rules.Cooldown.ToString("F1") + "s" : " · BUMP TO PASS!") : "LAST TRUCK STANDING";
        public override string Help => "Eight trucks. One giant ticking bomb. Any real truck-body bump passes it: no speed threshold, no proximity transfer. Resting contact cannot repeatedly swap it; the same pair must separate and a visible 0.7-second transfer cooldown must expire. The carrier gets a modest 15% engine-power boost. An unpredictable 12–20-second fuse eliminates the carrier with a physical truck-shattering blast. Blast damage never passes the bomb. A new cycle selects one survivor; last truck standing wins. +10 for your first successful pass each cycle, +20 for surviving a cycle and +100 for sole survival. Pass counts remain visible, but repeated passes in one cycle cannot farm points. Human and named AI trucks obey the same contacts and timer. AI carriers chase; others evade. Use normal throttle/brake/steering. ACTION recovers an overturned or genuinely stuck truck at its current safe location; it never pauses the bomb. Shared, chase and full-cab cameras, free look, optional AI views and the universal horn are available. Weather, crew names/colors and inputs use the shared collection settings. Match-end replays are recorded transforms and never change results.";
        public override string ControlCaption(int slot) => base.ControlCaption(slot).Replace(" ACTION", " RECOVER");
        readonly StringBuilder text = new StringBuilder(600);
        readonly float[] basePower = new float[8], stuck = new float[8], reverseUntil = new float[8], recoveryAt = new float[8];
        readonly RaycastHit[] recoveryGround = new RaycastHit[32];
        int priorCarrier = -1; float nextTick, aftermath;
        MaterialPropertyBlock glow;

        public override string Standings { get { text.Clear(); for (int i = 0; i < setup.count; i++) text.Append(setup.players[i].name).Append(" · ").Append(rules.scores[i]).Append("PTS · ").Append(rules.passes[i]).Append(" PASSES · ").Append(rules.survived[i]).Append(" SURVIVED\n"); return text.ToString(); } }
        public override string Results => (rules.Winner >= 0 ? setup.players[rules.Winner].name + " WINS!" : "MATCH ENDED") + "\n\n" + Standings;
        public override bool ParticipantLocked(int slot) => !rules.alive[slot];
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        { value = rules.scores[slot] + "P"; detail = !rules.alive[slot] ? "ELIMINATED" : rules.Carrier == slot ? "BOMB · " + rules.Fuse.ToString("F1") + "s" : rules.survived[slot] + " SURVIVED · " + rules.passes[slot] + " PASSES"; return slot < setup.count; }
        public override string ViewText(int view) { int slot = drivingViews.rally.owners[view]; TryReadScoreCard(slot, out string score, out string detail); return setup.players[slot].name + " · " + score + "\n" + detail + "\n" + (ParticipantLocked(slot) ? "SPECTATOR · " : "") + Status; }
        protected override void Awake()
        {
            base.Awake(); glow = new MaterialPropertyBlock(); rules.Awarded += ConfirmAward;
            propPositions = new Vector3[breakables.Length]; propRotations = new Quaternion[breakables.Length];
            for (int i = 0; i < breakables.Length; i++) { propPositions[i] = breakables[i].transform.position; propRotations[i] = breakables[i].transform.rotation; }
        }
        void ConfirmAward(HotPotatoAwardKind kind, string id, int slot, int delta, int total)
        {
            bool winner = kind == HotPotatoAwardKind.LastStanding;
            ConfirmScore(id, slot, delta, total, kind == HotPotatoAwardKind.Pass ? "BOMB PASSED" : winner ? "LAST TRUCK STANDING" : "BOMB CYCLE SURVIVED", winner ? SportsScoreTier.RoundWinner : SportsScoreTier.Small, roundEnded: kind != HotPotatoAwardKind.Pass, matchEnded: winner);
        }
        protected override void OnDestroy() { if (rules != null) rules.Awarded -= ConfirmAward; base.OnDestroy(); }
        public Vector3 Grid(int slot) { float angle = slot * Mathf.PI * 2 / Mathf.Max(2, setup.count); return new Vector3(Mathf.Sin(angle) * 42, .65f, Mathf.Cos(angle) * 42); }

        protected override void BeginGame()
        {
            if (review == null) { review = gameObject.AddComponent<SportsRecordedReview>(); review.match = this; review.trucks = trucks; review.fragments = fragments; review.drivingViews = drivingViews; review.broadcast = broadcast; review.framing = framing; review.atmosphere = atmosphere; review.watchAction = "watchpotato"; review.extras = new[] { bomb }; }
            review.ResetRecord(); rules.Reset(setup.count, Environment.TickCount); Countdown = 3; Explosions = 0; priorCarrier = -1; nextTick = 0; aftermath = 2;
            crowd?.ResetAttempt();
            for (int i = 0; i < breakables.Length; i++)
            { var prop = breakables[i]; prop.ResetPiece(); prop.intactBody.isKinematic = false; prop.intactBody.position = propPositions[i]; prop.intactBody.rotation = propRotations[i]; prop.intactBody.linearVelocity = prop.intactBody.angularVelocity = Vector3.zero; }
            for (int i = 0; i < trucks.Length; i++)
            {
                blasts[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); fragments[i].ResetBody(); trucks[i].gameObject.SetActive(i < setup.count);
                if (i >= setup.count) continue;
                var truck = trucks[i]; truck.tractorMotor.Configure();
                if (basePower[i] == 0) basePower[i] = truck.tractorMotor.vehicle.powertrain.engine.maxPower;
                truck.Prepare(Grid(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(Grid(i), Vector3.up))); Hold(truck);
                stuck[i] = reverseUntil[i] = recoveryAt[i] = 0;
            }
            bomb.gameObject.SetActive(true); drivingViews.Layout(); atmosphere.Configure(Environment.TickCount, 0, 1);
        }
        static void Hold(SportsEventTruck truck)
        {
            var input = truck.tractorMotor.vehicle.input; input.states.throttle = input.states.throttleRaw = input.states.brakes = input.states.brakesRaw = 0; input.states.handbrake = input.states.handbrakeRaw = 1;
        }
        protected override void Simulate(float dt)
        {
            atmosphere.Tick(rules.Clock);
            if (Countdown > 0) { Countdown = Mathf.Max(0, Countdown - dt); foreach (var truck in trucks) if (truck.gameObject.activeSelf) Hold(truck); return; }
            if (rules.Complete)
            {
                aftermath -= dt; review.Capture(rules.Clock + 5 - aftermath);
                for (int i = 0; i < setup.count; i++) if (rules.alive[i]) Hold(trucks[i]);
                if (aftermath <= 0) { review.focusSlot = rules.Winner; EndMatch(); }
                return;
            }
            int eliminated = rules.Step(dt);
            if (eliminated >= 0)
            {
                trucks[eliminated].Power(false); fragments[eliminated].Burst(); blasts[eliminated].transform.position = trucks[eliminated].body.worldCenterOfMass; blasts[eliminated].Play(); Explosions++;
                feedback.PlayAt(3, trucks[eliminated].body.position, 12, true); Pulse(eliminated, 1, .5f);
                crowd?.React(true, "BOMB EXPLOSION", trucks[eliminated].body.position);
                for (int i = 0; i < setup.count; i++) if (rules.alive[i])
                { var away = trucks[i].body.position - trucks[eliminated].body.position; if (away.magnitude < 18 && away.magnitude > .01f) trucks[i].tractorMotor.AddAssemblyVelocity((away.normalized + Vector3.up * .3f) * 6 * (1 - away.magnitude / 18)); }
            }
            if (priorCarrier != rules.Carrier)
            { if (rules.Carrier >= 0) { feedback.PlayAt(5, trucks[rules.Carrier].body.position, 0, true); Pulse(rules.Carrier, .5f, .15f); } priorCarrier = rules.Carrier; }
            review.Capture(rules.Clock + 3);
            if (rules.Complete) return;
            if (rules.Clock >= nextTick) { feedback.PlayAt(2, trucks[rules.Carrier].body.position, 0, true); nextTick = rules.Clock + Mathf.Lerp(.12f, .9f, Mathf.Clamp01(rules.Fuse / 10)); }
            for (int i = 0; i < setup.count; i++) if (rules.alive[i])
            {
                var truck = trucks[i]; truck.tractorMotor.gripScale = atmosphere.Grip;
                truck.tractorMotor.vehicle.powertrain.engine.maxPower = basePower[i] * (rules.Carrier == i ? carrierPowerBoost : 1);
                truck.TickDrive(setup.players[i].source == BowlingSource.AI ? Bot(i, dt) : commands[i]);
                if (setup.players[i].source != BowlingSource.AI && actions[i]) Recover(i);
                if (Mathf.Abs(truck.body.position.x) > 75 || Mathf.Abs(truck.body.position.z) > 75 || truck.body.position.y < -8) Recover(i);
            }
        }
        Vector2 Bot(int slot, float dt)
        {
            var t = trucks[slot]; var position = t.body.position; var carrier = trucks[rules.Carrier]; float skill = (int)setup.players[slot].difficulty;
            float urgency = 1 - Mathf.Clamp01(rules.Fuse / Mathf.Max(1, urgentFuseSeconds));
            Vector3 goal;
            if (slot == rules.Carrier)
            {
                int target = -1; float best = float.MaxValue;
                for (int i = 0; i < setup.count; i++) if (i != slot && rules.alive[i])
                { float distance = (trucks[i].body.position - position).sqrMagnitude; if (distance < best) { target = i; best = distance; } }
                // As the fuse expires, commit to a near-term physical bump
                // rather than continuing a long speculative intercept.
                goal = trucks[target].body.position + trucks[target].body.linearVelocity * Mathf.Lerp(.15f + skill * .12f, .08f, urgency);
            }
            else
            {
                Vector3 away = Vector3.ProjectOnPlane(position - carrier.body.position, Vector3.up).normalized;
                goal = position + away * Mathf.Lerp(22, 34, urgency) + Vector3.Cross(Vector3.up, away) * (slot % 2 == 0 ? 12 : -12);
            }
            goal.y = position.y;
            if (Vector3.ProjectOnPlane(position, Vector3.up).magnitude > arenaRadius - 9) goal = -Vector3.ProjectOnPlane(position, Vector3.up) * .5f;
            stuck[slot] = t.body.linearVelocity.magnitude < .6f ? stuck[slot] + dt : 0;
            if (stuck[slot] > 1.7f && reverseUntil[slot] <= rules.Clock) { reverseUntil[slot] = rules.Clock + 2; stuck[slot] = 0; }
            bool reverse = rules.Clock < reverseUntil[slot]; Vector3 heading = reverse ? -t.transform.forward : t.transform.forward;
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(heading, Vector3.up), goal - position, Vector3.up);
            float steer = Mathf.Clamp(angle / (40 - skill * 5 - urgency * 5), -1, 1) * (reverse ? -1 : 1);
            float throttle = reverse ? -.85f : Mathf.Abs(angle) > 75 ? .48f : Mathf.Clamp01(.82f + skill * .09f + urgency * .16f);
            if (Vector3.Dot(t.transform.up, Vector3.up) < .25f) { stuck[slot] += dt; if (stuck[slot] > 2.5f) Recover(slot); }
            return new Vector2(steer, throttle);
        }
        void Recover(int slot)
        {
            if (rules.Clock < recoveryAt[slot]) return;
            var truck = trucks[slot]; var point = truck.body.position;
            point.x = Mathf.Clamp(point.x, -50, 50); point.z = Mathf.Clamp(point.z, -50, 50); point.y = .65f;
            int groundCount = Physics.RaycastNonAlloc(new Vector3(point.x, 100, point.z), Vector3.down, recoveryGround, 150, 1, QueryTriggerInteraction.Ignore);
            float floor = 0;
            for (int i = 0; i < groundCount; i++)
                if (recoveryGround[i].rigidbody == null && recoveryGround[i].normal.y > .65f)
                    floor = Mathf.Max(floor, recoveryGround[i].point.y);
            point.y = floor + .65f;
            for (int i = 0; i < setup.count; i++) if (i != slot && rules.alive[i] && Vector3.Distance(trucks[i].body.position, point) < 11) return;
            truck.Prepare(point, Quaternion.Euler(0, truck.transform.eulerAngles.y, 0)); recoveryAt[slot] = rules.Clock + 6; stuck[slot] = 0;
        }
        protected override void Present(float dt)
        {
            if (drivingViews == null) return;
            if (ReplayControls) { review.Tick(); return; }
            drivingViews.Tick(dt);
            if (drivingViews.NeedsArena)
            {
                Bounds active = new Bounds(Vector3.zero, new Vector3(100, 8, 100));
                for (int i = 0; i < setup.count; i++) if (rules.alive[i]) active.Encapsulate(trucks[i].body.position);
                active.Expand(new Vector3(14, 10, 14));
                if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true; broadcast.Pose(active.center, new Vector3(0, 90, -135), 60);
                // Fit uses this camera-space orientation; snap prevents the
                // startup camera damping from leaving an unintended top-down shot.
                if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0, -90, 135), Vector3.up);
                framing.arenaBounds = active; framing.Apply();
            }
            bool visible = Running && !Finished && rules.Carrier >= 0; bomb.gameObject.SetActive(visible);
            if (!visible) return;
            bomb.position = trucks[rules.Carrier].body.position + Vector3.up * 5;
            if (fuseSparks != null)
            {
                if (rules.Fuse < 5 && !fuseSparks.isPlaying) fuseSparks.Play();
                else if (rules.Fuse >= 5 && fuseSparks.isPlaying) fuseSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            bomb.rotation = Quaternion.Euler(0, rules.Clock * 22, 0);
            float urgency = 1 - Mathf.Clamp01(rules.Fuse / 12); float pulse = .5f + .5f * Mathf.Sin(rules.Clock * Mathf.Lerp(5, 22, urgency));
            glow.SetColor("_BaseColor", Color.Lerp(new Color(.2f, .02f, .01f), Color.red, pulse)); glow.SetColor("_EmissionColor", Color.red * (1 + pulse * 4)); bombGlow.SetPropertyBlock(glow);
            bombClock.text = rules.Fuse.ToString("F1"); bombClock.transform.rotation = broadcast.view.transform.rotation;
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); else review?.Action(key); }
        protected override void SetPaused(bool pause) { atmosphere?.Pause(pause); }
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) truck.StopSound(); }
        protected override void StopGame()
        {
            review?.End(false); StopSounds(); if (bomb != null) bomb.gameObject.SetActive(false);
            if (breakables != null) foreach (var prop in breakables) { prop.intactBody.isKinematic = true; foreach (var piece in prop.fragments) piece.isKinematic = true; }
            if (trucks != null) for (int i = 0; i < trucks.Length; i++) { trucks[i].Power(false); trucks[i].body.isKinematic = true; fragments[i].Freeze(); if (basePower[i] > 0) trucks[i].tractorMotor.vehicle.powertrain.engine.maxPower = basePower[i]; }
        }
    }
}
