using System;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckBalloonBattle
{
    public sealed class BalloonMatch : SportsEventSession
    {
 public override bool BroadcastParticipantActive(int slot)=>rules.Alive(slot);
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public BalloonTarget[] targets;
        public BalloonSpike[] spikes;
        public Transform[] balloonGroups;
        public ParticleSystem[] confetti;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public LWS.TruckFieldGoal.FieldGoalCrowd crowd;
        public SportsRecordedReview review;
        public BalloonRules rules = new BalloonRules();
        readonly int[] scoreBeforeStep = new int[8];
        public float Countdown { get; private set; }
        public int PhysicalPops { get; private set; }
        readonly float[] stuck = new float[8], reverseUntil = new float[8], recoverAfter = new float[8];
        readonly StringBuilder text = new StringBuilder(700);
        float aftermath;
        public override string Title => "TRUCK BALLOON BATTLE";
        public override int MinimumPlayers => 2;
        public override int MaximumPlayers => 8;
        public override int OptionCount => 1;
        public override int WeatherCount => 1;
        public override string OptionLabel => "THREE BALLOONS · LAST TRUCK STANDING";
        public override int CompactScoreColumns => 4;
        public override bool SupportsSplitAIViews => true;
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Live && Countdown > 0 ? Countdown : -1;
        public override float RaceStartSeconds => Countdown <= 0 ? rules.Clock : -1;
        public override bool ReplayControls => review != null && review.Playing;
        public override bool MultiViewHUD => !ReplayControls && drivingViews != null && !drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible => !ReplayControls;
        public override bool ParticipantLocked(int slot) => !rules.Alive(slot);
        public override string Header => Title + (ReplayControls ? " · RECORDED REPLAY" : " · " + AliveCount() + " TRUCKS · " + rules.Pops + " POPS");
        public override string Status => Countdown > 0 ? "PROTECT YOUR THREE BALLOONS" : "EXTEND YOUR SPIKE · REAL CONTACT POPS · +1 POP / +3 ELIMINATION / +5 VICTORY";
        public override string ControlCaption(int slot) => base.ControlCaption(slot).Replace(" ACTION", " SPIKE");
        public override string Help => "Every truck has three huge tethered balloons. Drive, dodge and aim your roof-mounted retractable spike at an opponent's balloons. ACTION arms a yellow quarter-second warning, then a red 0.6-second attack. Cyan means ready; purple means the 2.5-second cooldown is still running. Actual spike-to-balloon contact is required. One attack can pop at most one balloon on each opponent. Ordinary body bumps, proximity and AI decisions never award points. +1 per pop, +3 for eliminating an opponent, +5 for sole survival. Lose all three and a confetti burst sends you to a spectator camera. Simultaneous mutual final pops are a draw. Difficulty changes actual AI driving/attack decisions, never free damage. Shared arena, independent chase/cab views, free look, optional AI views, crew names/colors, selectable weather and the universal horn use the collection controls. Slow or overturned trucks automatically recover at a separated safe point; this never restores balloons. Match-end recorded replay cannot change the result.";
        public int AliveCount() { int n = 0; for (int i = 0; i < setup.count; i++) if (rules.Alive(i)) n++; return n; }
        public override bool ValidateSources(out string reason)
        {
            if (!base.ValidateSources(out reason)) return false;
            int humans = 0; for (int i = 0; i < setup.count; i++) if (setup.players[i].source != BowlingSource.AI) humans++;
            if (humans <= 4) return true; reason = "Up to four local humans; assign other competitors to AI."; return false;
        }
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = rules.scores[slot] + "P"; detail = !rules.Alive(slot) ? "ELIMINATED · SPECTATING" : rules.Remaining(slot) + " BALLOONS · " + (rules.Extended(slot) ? "SPIKE OUT!" : rules.Cooldown(slot) > 0 ? "READY IN " + rules.Cooldown(slot).ToString("F1") : "SPIKE READY"); return slot < setup.count;
        }
        public override string ViewText(int view) { int slot = drivingViews.rally.owners[view]; TryReadScoreCard(slot, out var score, out var detail); return setup.players[slot].name + " · " + score + "\n" + detail; }
        public override string Standings { get { text.Clear(); for (int i = 0; i < setup.count; i++) text.Append(setup.players[i].name).Append(" · ").Append(rules.scores[i]).Append("PTS · ").Append(rules.creditedPops[i]).Append(" POPS · ").Append(rules.eliminations[i]).Append(" ELIMINATIONS\n"); return text.ToString(); } }
        public override string Results => (rules.Winner < 0 ? "MUTUAL POP · DRAW!" : setup.players[rules.Winner].name + " WINS!") + "\n\n" + Standings;
        // At32m, diagonal tractors straddled the20m ramp ends. Keep the full
        // compound tractor footprint on the flat outer lane during settling.
        public Vector3 Grid(int slot) { float a = slot * Mathf.PI * 2 / Mathf.Max(2, setup.count); return new Vector3(Mathf.Sin(a) * 42, .65f, Mathf.Cos(a) * 42); }
        protected override void BeginGame()
        {
            if (review == null) { review = gameObject.AddComponent<SportsRecordedReview>(); review.match = this; review.trucks = trucks; review.fragments = fragments; review.drivingViews = drivingViews; review.broadcast = broadcast; review.framing = framing; review.atmosphere = atmosphere; review.watchAction = "watchballoon"; review.extras = balloonGroups; }
            review.ResetRecord(); rules.Reset(setup.count); Countdown = 3; PhysicalPops = 0; aftermath = 3; crowd?.ResetAttempt();
            for (int i = 0; i < trucks.Length; i++)
            {
                confetti[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); fragments[i].ResetBody(); trucks[i].gameObject.SetActive(i < setup.count); balloonGroups[i].gameObject.SetActive(i < setup.count);
                if (i >= setup.count) continue;
                trucks[i].tractorMotor.Configure(); trucks[i].Prepare(Grid(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(Grid(i), Vector3.up))); Hold(trucks[i]);
                stuck[i] = reverseUntil[i] = recoverAfter[i] = 0;
                for (int b = 0; b < 3; b++) targets[i * 3 + b].Restore(); spikes[i].Pose();
            }
            drivingViews.Layout(); atmosphere.Configure(Environment.TickCount, 0, 1); Physics.SyncTransforms();
        }
        static void Hold(SportsEventTruck truck) { var input = truck.tractorMotor.vehicle.input; input.states.throttle = input.states.throttleRaw = input.states.brakes = input.states.brakesRaw = 0; input.states.handbrake = input.states.handbrakeRaw = 1; }
        protected override void Simulate(float dt)
        {
            atmosphere.Tick(rules.Clock);
            if (Countdown > 0) { Countdown = Mathf.Max(0, Countdown - dt); for (int i = 0; i < setup.count; i++) { Hold(trucks[i]); for (int b = 0; b < 3; b++) targets[i * 3 + b].Follow(dt); } return; }
            if (rules.Complete) { aftermath -= dt; review.Capture(rules.Clock + 6 - aftermath); if (aftermath <= 0) { review.focusSlot = rules.Winner; EndMatch(); } return; }
            Array.Copy(rules.scores, scoreBeforeStep, setup.count);
            rules.Step(dt);
            if (rules.Complete && rules.Winner >= 0)
                ConfirmScore("last-truck-standing", rules.Winner, rules.scores[rules.Winner] - scoreBeforeStep[rules.Winner], rules.scores[rules.Winner], "LAST TRUCK STANDING", SportsScoreTier.RoundWinner, roundEnded: true, matchEnded: true);
            for (int i = 0; i < setup.count; i++)
            {
                if (rules.Alive(i) && !rules.Complete)
                {
                    var truck = trucks[i]; truck.tractorMotor.gripScale = atmosphere.Grip;
                    truck.TickDrive(setup.players[i].source == BowlingSource.AI ? Bot(i, dt) : commands[i]);
                    if (setup.players[i].source != BowlingSource.AI && actions[i]) Attack(i);
                    if (truck.body.position.y < -5 || Vector3.Dot(truck.transform.up, Vector3.up) < .25f || stuck[i] > 8) Recover(i);
                    for (int b = 0; b < 3; b++) if (rules.balloons[i, b]) targets[i * 3 + b].Follow(dt);
                }
                else Hold(trucks[i]);
                spikes[i].Pose();
            }
            review.Capture(rules.Clock + 3);
        }
        public bool Attack(int slot)
        {
            if (!Live || Countdown > 0 || !rules.Attack(slot)) return false;
            feedback.PlayAt(2, trucks[slot].body.position, 0, true); Pulse(slot, .2f, .1f); return true;
        }
        // Only BalloonTarget's actual PhysX trigger invokes this gameplay path.
        internal void Contact(int attacker, int victim, int balloon)
        {
            if (!Live || Countdown > 0 || attacker < 0 || attacker >= setup.count) return;
            int previousScore = rules.scores[attacker];
            if (!rules.Pop(attacker, victim, balloon, rules.AttackSequence(attacker))) return;
            ConfirmScore("balloon:" + victim + ":" + balloon, attacker, rules.scores[attacker] - previousScore, rules.scores[attacker], rules.Alive(victim) ? "BALLOON POP" : "BALLOON ELIMINATION", rules.Alive(victim) ? SportsScoreTier.Small : SportsScoreTier.Major);
            var target = targets[victim * 3 + balloon]; var position = target.body.position; target.Hide(); PhysicalPops++;
            confetti[victim].transform.position = position; confetti[victim].Emit(rules.Alive(victim) ? 22 : 150);
            feedback.PlayAt(5, position, 0, true); Pulse(attacker, .6f, .18f); Pulse(victim, .4f, .15f);
            if (!rules.Alive(victim)) { Hold(trucks[victim]); spikes[victim].contact.enabled = false; crowd?.React(true, "CONFETTI ELIMINATION", position); }
        }
        Vector2 Bot(int slot, float dt)
        {
            var truck = trucks[slot]; var position = truck.body.position; int opponent = -1, balloon = 0; float best = float.MaxValue;
            for (int i = 0; i < setup.count; i++) if (i != slot && rules.Alive(i))
                for (int b = 0; b < 3; b++) if (rules.balloons[i, b]) { float distance = (targets[i * 3 + b].body.position - position).sqrMagnitude * (1 + rules.Remaining(i) * .1f); if (distance < best) { best = distance; opponent = i; balloon = b; } }
            if (opponent < 0) return Vector2.zero;
            float skill = (int)setup.players[slot].difficulty;
            Vector3 goal = targets[opponent * 3 + balloon].body.position + trucks[opponent].body.linearVelocity * (.1f + skill * .1f); goal.y = position.y;
            float distanceTo = Vector3.Distance(position, goal);
            float aim = Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up), goal - position, Vector3.up);
            if (distanceTo < 15 - (2 - skill) * 1.5f && Mathf.Abs(aim) < 32 - (2 - skill) * 4) Attack(slot);
            if (rules.Cooldown(slot) > 1.5f && !rules.Extended(slot)) goal = position + Vector3.ProjectOnPlane(position - trucks[opponent].body.position, Vector3.up).normalized * 18 + Vector3.Cross(Vector3.up, truck.transform.forward) * 9;
            if (Vector3.ProjectOnPlane(position, Vector3.up).magnitude > 47) goal = Vector3.zero;
            stuck[slot] = truck.body.linearVelocity.magnitude < .7f ? stuck[slot] + dt : 0;
            if (stuck[slot] > 1.5f && reverseUntil[slot] < rules.Clock) { reverseUntil[slot] = rules.Clock + 1.8f; }
            bool reverse = rules.Clock < reverseUntil[slot];
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(reverse ? -truck.transform.forward : truck.transform.forward, Vector3.up), goal - position, Vector3.up);
            float desiredSpeed = Mathf.Abs(angle) > 70 ? 4 : Mathf.Abs(angle) > 35 ? 7 : 11 + skill * 2;
            return new Vector2(Mathf.Clamp(angle / 32, -1, 1) * (reverse ? -1 : 1), reverse ? -.8f : truck.body.linearVelocity.magnitude < desiredSpeed ? .85f : 0);
        }
        void Recover(int slot)
        {
            if (rules.Clock < recoverAfter[slot]) return;
            for (int k = 0; k < 8; k++)
            {
                var p = Grid((slot + k) % setup.count); bool free = true;
                for (int i = 0; i < setup.count; i++) if (i != slot && Vector3.Distance(trucks[i].body.position, p) < 12) free = false;
                if (!free) continue;
                trucks[slot].Prepare(p, Quaternion.LookRotation(-Vector3.ProjectOnPlane(p, Vector3.up))); for (int b = 0; b < 3; b++) if (rules.balloons[slot, b]) targets[slot * 3 + b].Restore();
                recoverAfter[slot] = rules.Clock + 6; stuck[slot] = 0; return;
            }
        }
        protected override void Present(float dt)
        {
            if (drivingViews == null) return; if (ReplayControls) { review.Tick(); return; }
            drivingViews.Tick(dt);
            if (drivingViews.NeedsArena)
            {
                Bounds area = new Bounds(new Vector3(0, 4, 0), new Vector3(65, 16, 65));
                for (int i = 0; i < setup.count; i++) if (rules.Alive(i)) area.Encapsulate(trucks[i].body.position);
                area.Expand(new Vector3(12, 8, 12)); if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true; broadcast.Pose(area.center, new Vector3(0, 75, -110), 60);
                if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0, -75, 110), Vector3.up); framing.arenaBounds = area; framing.Apply();
            }
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); else review?.Action(key); }
        protected override void SetPaused(bool pause) => atmosphere?.Pause(pause);
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) truck.StopSound(); }
        protected override void StopGame()
        {
            review?.End(false); StopSounds(); if (trucks == null) return;
            for (int i = 0; i < trucks.Length; i++) { trucks[i].Power(false); trucks[i].body.isKinematic = true; fragments[i].Freeze(); spikes[i].contact.enabled = false; }
        }
    }
}
