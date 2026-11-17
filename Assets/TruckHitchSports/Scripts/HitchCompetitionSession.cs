using System;
using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;

namespace LWS.TruckHitchSports
{
    public abstract class HitchCompetitionSession : SportsEventSession
    {
        public SportsEventTruck[] trucks;
        public HitchCoupler[] couplers;
        public TrailerBody[] trailers;
        public SportsEventCamera broadcast;
        public SportsRallyViews views;
        public Vector3[] route;
        public float matchSeconds = 180;
        public readonly double[] Scores = new double[12];
        public readonly bool[] Completed = new bool[12];
        public float Elapsed { get; private set; }
        public int[] Deliveries = new int[12];
        public virtual bool Cooperative => false;
        public string Notice { get; protected set; } = "";
        protected Vector3[] starts;
        protected Quaternion[] rotations;
        protected bool[] initialCoupling;
        readonly float[] stalled = new float[4], recovery = new float[4];
        string reportedMatchId;
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 4;
        public override bool CompactBroadcastStatus => true;
        public override string Header => Title + " · " + Mathf.CeilToInt(Mathf.Max(0, matchSeconds - Elapsed)) + "s";
        public override string Status => Elapsed < 3 ? "READY · " + Mathf.CeilToInt(3 - Elapsed) : Notice;
        public override string Standings
        {
            get { var text = new StringBuilder(); for (int i = 0; i < setup.count; i++) text.Append(setup.players[i].name).Append("  ").Append(Scores[i].ToString("F0")).Append(" PTS\n"); return text.ToString(); }
        }
        public override string Results => "FINAL RESULTS\n" + Standings;
        public int ResultPlace(int slot) { if (!ResultEligible(slot)) return 0; int place = 1; for (int i = 0; i < setup.count; i++) if (ResultEligible(i) && Scores[i] > Scores[slot] + .001) place++; return place; }
        public virtual bool ResultEligible(int slot) => slot >= 0 && slot < setup.count && Completed[slot] && Scores[slot] > 0;
        public bool ResultWinner(int slot)
        {
            if (!Finished || Cooperative || !ResultEligible(slot)) return false;
            for (int i = 0; i < setup.count; i++) if (ResultEligible(i) && Scores[i] > Scores[slot] + .001) return false;
            return true;
        }
        public virtual string ResultDetail(int slot) => Deliveries[slot] + " DELIVERIES · " + couplers[slot].SuccessfulHitches + " HITCHES";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        { value = Scores[slot].ToString("F0"); detail = Completed[slot] ? "FINISHED" : couplers[slot].Attached != null ? "HITCH LOCKED" : "UNHITCHED"; return true; }
        protected override void BeginGame()
        {
            if (starts == null)
            {
                starts = new Vector3[trucks.Length]; rotations = new Quaternion[trucks.Length];
                for (int i = 0; i < trucks.Length; i++) { starts[i] = trucks[i].body.position; rotations[i] = trucks[i].body.rotation; }
            }
            Elapsed = 0; Array.Clear(Scores, 0, Scores.Length); Array.Clear(Completed, 0, Completed.Length); Array.Clear(Deliveries, 0, Deliveries.Length);
            Array.Clear(stalled, 0, stalled.Length); Array.Clear(recovery, 0, recovery.Length);
            initialCoupling = new bool[trucks.Length];
            for (int i = 0; i < couplers.Length; i++) { couplers[i].Release(); couplers[i].ResetMatchRecords(); }
            foreach (var trailer in trailers) { trailer.gameObject.SetActive(true); trailer.ResetForRound(); foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false; }
            for (int i = 0; i < trucks.Length; i++)
            {
                bool active = i < setup.count; trucks[i].gameObject.SetActive(active);
                if (!active) continue;
                foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                trucks[i].tractorMotor.Configure(); trucks[i].Prepare(starts[i], rotations[i]); couplers[i].Participant = i;
                if (StartsCoupled && i < trailers.Length)
                {
                    foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                    foreach (var body in trailers[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                }
            }
            foreach (var obstacle in GetComponentsInChildren<HitchModeObstacle>(true)) obstacle.Apply(option);
            Physics.SyncTransforms();
            views?.Layout(); BeginCompetition(); Gate();
        }
        protected abstract void BeginCompetition();
        protected abstract void TickCompetition(float dt);
        protected virtual bool StartsCoupled => true;
        protected override void Simulate(float dt)
        {
            Elapsed += dt;
            if (Elapsed < 3)
            {
                for (int i = 0; i < setup.count; i++)
                {
                    trucks[i].TickDrive(new Vector2(0, -1));
                    if (StartsCoupled && !initialCoupling[i] && i < trailers.Length)
                    {
                        initialCoupling[i] = couplers[i].TryCouple(trailers[i]);
                        if (initialCoupling[i])
                        {
                            foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                            foreach (var body in trailers[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                        }
                    }
                }
                return;
            }
            if (presentationWeather != null)
            {
                var wind = Vector3.ClampMagnitude(presentationWeather.Flow, 20);
                foreach (var trailer in trailers) if (!trailer.Body.isKinematic) trailer.Body.AddForce(wind * trailer.Body.mass * .04f);
                for (int i = 0; i < setup.count; i++) trucks[i].tractorMotor.gripScale = presentationWeather.Grip;
            }
            TickCompetition(dt);
            bool all = true; for (int i = 0; i < setup.count; i++) all &= Completed[i];
            if (all || Elapsed >= matchSeconds)
            {
                for (int i = 0; i < setup.count; i++) ConfirmOutcome("final-" + i, i, Scores[i], "Measured competition result", "PTS", SportsScoreTier.Major, true, true);
                ReportCompletedMatch();
                EndMatch();
            }
        }
        protected bool IsAI(int i) => setup.players[i].source == BowlingSource.AI;
        protected Vector2 DriveTo(int i, Vector3 target, float desiredSpeed = 12)
        {
            var truck = trucks[i]; Vector3 local = truck.transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z); float speed = truck.body.linearVelocity.magnitude;
            if (local.magnitude > 10 && speed < .35f) stalled[i] += Time.fixedDeltaTime; else stalled[i] = 0;
            if (stalled[i] > 3) { recovery[i] = 1.25f; stalled[i] = 0; }
            if (recovery[i] > 0)
            { recovery[i] -= Time.fixedDeltaTime; return new Vector2(angle >= 0 ? -.65f : .65f, -.45f); }
            float throttle = Mathf.Clamp((desiredSpeed - speed) * .25f, -1, 1);
            return new Vector2(Mathf.Clamp(angle * 1.8f, -1, 1), throttle * Mathf.Lerp(.3f, 1, 1 - Mathf.Clamp01(Mathf.Abs(angle))));
        }
        protected bool ApproachHitch(int i, TrailerBody target, float dt)
        {
            var coupler = couplers[i];
            Vector3 desiredRoot = target.Kingpin.position - trucks[i].transform.TransformVector(trucks[i].transform.InverseTransformPoint(coupler.Anchor.position));
            Vector3 local = trucks[i].transform.InverseTransformPoint(desiredRoot);
            float error = Vector3.SignedAngle(trucks[i].transform.forward, target.transform.forward, Vector3.up);
            bool reverse = local.z < 0;
            float distance = Vector3.Distance(coupler.Anchor.position, target.Kingpin.position);
            Vector2 drive = new Vector2(Mathf.Clamp((local.x * .2f + error * .03f) * (reverse ? -1 : 1), -1, 1), distance > .4f ? (reverse ? -.18f : .18f) : -Mathf.Clamp01(trucks[i].body.linearVelocity.magnitude));
            trucks[i].TickDrive(drive);
            return coupler.GuideCoupling(target, dt);
        }
        protected void AddScore(int i, double amount, string reason)
        { if (amount <= 0) return; Scores[i] += amount; ConfirmScore(reason + "-" + i + "-" + Elapsed.ToString("F3"), i, amount, Scores[i], reason); }
        void ReportCompletedMatch()
        {
            // Only the normal rules resolution emits final measurements. Setup,
            // abandoned attempts, pause and StopGame cannot duplicate a report.
            if (string.IsNullOrEmpty(MatchId) || reportedMatchId == MatchId) return;
            reportedMatchId = MatchId;
            for (int slot = 0; slot < setup.count; slot++)
            {
                RecordMetric(slot, "match_elapsed_seconds", Elapsed);
                RecordMetric(slot, "attempt_resolved", Completed[slot] ? 1 : 0);
                RecordMetric(slot, "successful_hitches", couplers[slot].SuccessfulHitches);
                ReportFinalMeasurements(slot);
            }
        }
        protected virtual void ReportFinalMeasurements(int slot) { }
        protected void RecordMetric(int slot, string measurement, double value, string context = null)
        {
            if (slot < 0 || slot >= setup.count || double.IsNaN(value) || double.IsInfinity(value)) return;
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, measurement, setup.players[slot].participantId,
                "mode=" + OptionLabel + (string.IsNullOrEmpty(context) ? "" : ";" + context), value);
        }
        protected override void Present(float dt)
        {
            if (views != null) { views.Follow(dt); return; }
            if (broadcast == null || trucks == null || trucks.Length == 0) return;
            int focus = 0; for (int i = 0; i < setup.count; i++) if (!IsAI(i) && !Completed[i]) { focus = i; break; }
            var subject = couplers[focus].Attached != null ? Vector3.Lerp(trucks[focus].body.position, couplers[focus].Attached.Body.position, .45f) : trucks[focus].body.position;
            broadcast.Pose(subject + Vector3.up * 2, new Vector3(18, 18, -28), 64);
        }
        protected override void StopGame()
        {
            if (trucks != null) foreach (var truck in trucks) if (truck != null)
            { truck.Power(false); foreach (var body in truck.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true; }
            if (trailers != null) foreach (var trailer in trailers) if (trailer != null)
            { if (trailer.Vehicle != null) trailer.Vehicle.enabled = false; foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true; }
        }
    }
}
