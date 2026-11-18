using UnityEngine;
using System.Linq;

namespace LWS.TruckExtremeHauling
{
    public sealed class TrailerPlinkoMatch : ExtremeHaulingMatch
    {
        public Transform[] boardOrigins;
        [Min(1)] public float boardScale = 2.5f;
        public PlinkoLandingTray[] landingTrays;
        public float[] binPoints = { 100, 300, 1000, 300, 100 };
        [Range(1, 5)] public int dropsPerPlayer = 3;
        [Range(4, 30)] public float preparationSeconds = 12;
        [Range(15, 60)] public float dropTimeoutSeconds = 32;
        [Range(.5f, 4)] public float betweenDropSeconds = 1.2f;
        Rigidbody[] movingPegs;
        Vector3[] pegPositions;
        int targetBin, competitionDrops;
        readonly bool[] dropped = new bool[4], resolved = new bool[4], actionArmed = new bool[4];
        readonly float[] quiet = new float[4], spin = new float[4], roll = new float[4], tilt = new float[4], preparedAt = new float[4], releasedAt = new float[4], nextDropAt = new float[4];
        readonly int[] completedDrops = new int[4], releasedDrops = new int[4], misses = new int[4], lastBin = new int[4];
        readonly string[] lastOutcome = new string[4];
        readonly Quaternion[] originalRotation = new Quaternion[4];
        readonly Vector3[] preparedPosition = new Vector3[4];
        readonly float[,] releaseSpin = new float[4, 5], releaseRoll = new float[4, 5], releaseTilt = new float[4, 5], releaseX = new float[4, 5], dropPoints = new float[4, 5];
        readonly int[,] settledBins = new int[4, 5];
        int BinCount => Mathf.Clamp(binPoints != null ? binPoints.Length : 0, 1, 5);
        int TargetBin(int slot) => (targetBin + completedDrops[slot]) % BinCount;

        public override string Title => "TRAILER PLINKO";
        protected override string[] Modes => new[] { "CLASSIC PLINKO", "PRECISION DROP", "CHAOS PLINKO", "HIGH-RISK JACKPOT" };
        protected override string ActionHint => "HOLD + STEER: ANGLE / RELEASE: DROP";
        public override string Status => option == 2 ? "MOVING PEGS / PHYSICAL DROPS / HOLD ACTION TO AIM ANGLE" : "STEER: POSITION / THROTTLE: SPIN / HOLD ACTION: ROLL + TILT / RELEASE: DROP";
        public override string Help => "Each player gets the configured 1-5 drops (default three); real settled-bin points add across the competition. Steering chooses release position and throttle adjusts initial spin. Hold ACTION and steer to roll the trailer across the board, or throttle/brake to tilt its release angle. Release ACTION to drop; a quick tap also drops. You have the displayed preparation time before automatic release. Real peg contacts, inertia and gravity determine the bin; the trailer must settle supported by the catch tray. Precision Drop marks a target bin for each drop; Chaos moves physical pegs; High-Risk Jackpot triples the center bin. Leaving the board or exceeding the flight timer is a zero-point miss. Only a scored or failed drop resets to the ordinary starting position for the next turn. Horn and pause controls are unchanged.";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            if (slot < 0 || slot >= setup.count) { value = detail = null; return false; }
            value = scores[slot].ToString("F0");
            string turn = "DROP " + Mathf.Min(completedDrops[slot] + 1, Mathf.Max(1, competitionDrops)) + "/" + Mathf.Max(1, competitionDrops);
            if (retired[slot]) detail = "FINAL / " + deliveries[slot] + " SCORED / " + misses[slot] + " MISSED";
            else if (resolved[slot]) detail = lastOutcome[slot] + " / NEXT DROP";
            else if (dropped[slot]) detail = turn + " / FALLING " + (age - releasedAt[slot]).ToString("F1") + "s";
            else detail = turn + " / " + Mathf.CeilToInt(Mathf.Max(0, preparationSeconds - (age - preparedAt[slot]))) + "s / R" + roll[slot].ToString("F0") + "° T" + tilt[slot].ToString("F0") + "° / " + spin[slot].ToString("F1") + "rad/s";
            if (option == 1 && !retired[slot] && !resolved[slot]) detail += " / TARGET " + (TargetBin(slot) + 1);
            return true;
        }
        public override string Results
        {
            get
            {
                var text = new System.Text.StringBuilder();
                for (int i = 0; i < setup.count; i++)
                {
                    text.Append(setup.players[i].name).Append(" · ").Append(scores[i].ToString("F0")).Append(" PTS · DROPS ");
                    for (int n = 0; n < completedDrops[i]; n++) { if (n > 0) text.Append(" / "); text.Append(settledBins[i, n] < 0 ? "MISS" : dropPoints[i, n].ToString("F0")); }
                    if (completedDrops[i] < competitionDrops) text.Append(" · INCOMPLETE");
                    text.AppendLine();
                }
                return text.ToString();
            }
        }
        protected override void BeginMechanic()
        {
            competitionDrops = Mathf.Clamp(dropsPerPlayer, 1, 5);
            // A configured event has room for all real prep/timeout rules.
            // The ordinary three-drop default still fits the existing 150s.
            matchSeconds = Mathf.Max(matchSeconds, competitionDrops * (preparationSeconds + dropTimeoutSeconds + betweenDropSeconds) + 3);
            targetBin = RoundSeed() % BinCount;
            if (movingPegs == null)
            {
                var pegs = gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true)).Where(c => c.name == "Physical rebound peg").Where((c, index) => index % 3 == 0).ToArray();
                movingPegs = new Rigidbody[pegs.Length]; pegPositions = new Vector3[pegs.Length];
                for (int p = 0; p < pegs.Length; p++)
                {
                    movingPegs[p] = pegs[p].GetComponent<Rigidbody>();
                    if (movingPegs[p] == null) movingPegs[p] = pegs[p].gameObject.AddComponent<Rigidbody>();
                    movingPegs[p].isKinematic = true; movingPegs[p].useGravity = false; pegPositions[p] = movingPegs[p].position;
                }
            }
            for (int p = 0; p < movingPegs.Length; p++) { movingPegs[p].position = pegPositions[p]; movingPegs[p].transform.position = pegPositions[p]; }
            System.Array.Clear(completedDrops, 0, 4); System.Array.Clear(releasedDrops, 0, 4); System.Array.Clear(misses, 0, 4);
            System.Array.Clear(releaseSpin, 0, releaseSpin.Length); System.Array.Clear(releaseRoll, 0, releaseRoll.Length); System.Array.Clear(releaseTilt, 0, releaseTilt.Length); System.Array.Clear(releaseX, 0, releaseX.Length); System.Array.Clear(dropPoints, 0, dropPoints.Length);
            for (int slot = 0; slot < 4; slot++) for (int n = 0; n < 5; n++) settledBins[slot, n] = -1;
            foreach (var tray in landingTrays) tray.ResetContacts();
            for (int i = 0; i < setup.count; i++)
            {
                originalRotation[i] = Quaternion.Inverse(boardOrigins[i].rotation) * trailers[i].StartRotation;
                lastBin[i] = -1; lastOutcome[i] = ""; PrepareDrop(i, false);
                trucks[i].Power(false); foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            }
        }
        int RoundSeed() => MatchId == null ? 0 : MatchId.GetHashCode() & int.MaxValue;
        protected override void Simulate(float dt)
        {
            if (option == 2 && movingPegs != null) for (int p = 0; p < movingPegs.Length; p++) movingPegs[p].MovePosition(pegPositions[p] + Vector3.right * (Mathf.Sin(age * 1.3f + p * .7f) * 2.4f));
            base.Simulate(dt);
        }
        protected override Vector2 AICommand(int i, float dt)
        {
            float error = Mathf.Sin(age * 1.5f + i) * (.4f - (int)setup.players[i].difficulty * .15f);
            float targetX = boardOrigins[i].position.x + (option == 1 ? (TargetBin(i) - 2) * 8 * boardScale : 0);
            return new Vector2(Mathf.Clamp((targetX - trailers[i].Body.position.x) * .3f + error, -1, 1), .2f);
        }
        void PrepareDrop(int slot, bool resetResolvedBody)
        {
            // Ordinary completed/failed round boundary, never a peg-path shortcut.
            if (resetResolvedBody) trailers[slot].ResetForRound();
            trailers[slot].Vehicle.enabled = false;
            foreach (var part in trailers[slot].GetComponentsInChildren<Rigidbody>(true)) part.isKinematic = true;
            dropped[slot] = resolved[slot] = actionArmed[slot] = false;
            quiet[slot] = spin[slot] = roll[slot] = tilt[slot] = 0;
            preparedAt[slot] = age; releasedAt[slot] = nextDropAt[slot] = 0;
            preparedPosition[slot] = trailers[slot].Body.position; landingTrays[slot].ResetContacts();
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            var body = trailers[i].Body; var origin = boardOrigins[i];
            if (resolved[i]) { if (age >= nextDropAt[i]) PrepareDrop(i, true); return; }
            if (!dropped[i])
            {
                bool holding = !ai && ActionHeld(i);
                if (!ai && (actions[i] || holding)) actionArmed[i] = true;
                if (holding)
                {
                    roll[i] = Mathf.Clamp(roll[i] + drive.x * dt * 45, -70, 70);
                    tilt[i] = Mathf.Clamp(tilt[i] + drive.y * dt * 25, -20, 20);
                }
                else
                {
                    Vector3 local = origin.InverseTransformPoint(preparedPosition[i]);
                    local.x = Mathf.Clamp(local.x + drive.x * dt * 6 * boardScale, -18 * boardScale, 18 * boardScale);
                    preparedPosition[i] = origin.TransformPoint(local); if (!ai) spin[i] = Mathf.Clamp(spin[i] + drive.y * dt, -2.5f, 2.5f);
                }
                if (ai)
                {
                    // These are real release choices, never predetermined bins.
                    int drop = completedDrops[i], difficulty = (int)setup.players[i].difficulty;
                    float desiredRoll = Mathf.Sin(drop * 1.7f + i * .9f) * (option == 2 ? 35 : 16 - difficulty * 3);
                    float desiredTilt = Mathf.Sin(drop * 1.1f + i) * (6 - difficulty);
                    roll[i] = Mathf.MoveTowards(roll[i], desiredRoll, dt * 30); tilt[i] = Mathf.MoveTowards(tilt[i], desiredTilt, dt * 15);
                    float desiredSpin = .6f + Mathf.Sin(drop * 1.3f + i * .7f) * (option == 2 ? 1.2f : .65f);
                    spin[i] = Mathf.MoveTowards(spin[i], desiredSpin, dt * 1.2f);
                }
                body.position = preparedPosition[i];
                body.rotation = origin.rotation * Quaternion.AngleAxis(roll[i], Vector3.forward) * Quaternion.AngleAxis(tilt[i], Vector3.right) * originalRotation[i];
                float preparationAge = age - preparedAt[i];
                if ((!ai && actionArmed[i] && !holding) || ai && preparationAge > 3 + i * .25f || preparationAge >= preparationSeconds) ReleaseDrop(i);
                return;
            }
            Vector3 at = origin.InverseTransformPoint(body.worldCenterOfMass);
            bool inTray = at.y < -48 * boardScale && at.y > -58 * boardScale && Mathf.Abs(at.x) < 25 * boardScale && Mathf.Abs(at.z) < 6 * boardScale;
            quiet[i] = inTray && landingTrays[i].Supports(body) && body.linearVelocity.magnitude < .6f && body.angularVelocity.magnitude < .2f ? quiet[i] + dt : 0;
            if (quiet[i] > 1.5f)
            {
                int bin = Mathf.Clamp(Mathf.FloorToInt((at.x + 25 * boardScale) / (10 * boardScale)), 0, BinCount - 1);
                float points = option == 1 ? Mathf.Max(25, 1500 - Mathf.Abs(bin - TargetBin(i)) * 500) : binPoints[bin] * (option == 3 && bin == 2 ? 3 : 1);
                FinishDrop(i, bin, points, "BIN " + (bin + 1) + " / +" + points.ToString("F0"));
            }
            else if (Mathf.Abs(at.x) > 32 * boardScale || Mathf.Abs(at.z) > 10 * boardScale || at.y < -65 * boardScale) FinishDrop(i, -1, 0, "MISS / OUT OF BOARD");
            else if (age - releasedAt[i] >= dropTimeoutSeconds) FinishDrop(i, -1, 0, "MISS / DID NOT SETTLE");
        }
        void ReleaseDrop(int slot)
        {
            int drop = completedDrops[slot]; var body = trailers[slot].Body;
            releaseRoll[slot, drop] = roll[slot]; releaseTilt[slot, drop] = tilt[slot]; releaseSpin[slot, drop] = spin[slot]; releaseX[slot, drop] = boardOrigins[slot].InverseTransformPoint(body.position).x;
            dropped[slot] = true; releasedDrops[slot]++; releasedAt[slot] = age;
            body.isKinematic = false; body.useGravity = true; body.linearVelocity = Vector3.zero; body.angularVelocity = boardOrigins[slot].forward * spin[slot]; body.WakeUp();
        }
        void FinishDrop(int slot, int bin, float points, string outcome)
        {
            if (resolved[slot]) return;
            int drop = completedDrops[slot]; settledBins[slot, drop] = bin; dropPoints[slot, drop] = points; lastBin[slot] = bin; lastOutcome[slot] = outcome;
            if (bin >= 0) Award(slot, points, "settled_plinko_bin_" + bin); else misses[slot]++;
            completedDrops[slot]++; resolved[slot] = true;
            foreach (var body in trailers[slot].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            if (completedDrops[slot] >= competitionDrops) retired[slot] = true; else nextDropAt[slot] = age + betweenDropSeconds;
        }
        protected override void ReportFinalMeasurements(int i)
        {
            RecordMetric(i, "plinko_released", releasedDrops[i] > 0 ? 1 : 0); RecordMetric(i, "plinko_releases", releasedDrops[i]); RecordMetric(i, "plinko_drops_completed", completedDrops[i]); RecordMetric(i, "plinko_misses", misses[i]);
            for (int n = 0; n < releasedDrops[i]; n++)
            {
                string suffix = "_drop_" + (n + 1);
                RecordMetric(i, "plinko_release_spin_radians" + suffix, releaseSpin[i, n]); RecordMetric(i, "plinko_release_roll_degrees" + suffix, releaseRoll[i, n]); RecordMetric(i, "plinko_release_tilt_degrees" + suffix, releaseTilt[i, n]); RecordMetric(i, "plinko_release_x_metres" + suffix, releaseX[i, n]);
                if (option == 1) RecordMetric(i, "plinko_target_bin" + suffix, (targetBin + n) % BinCount);
                if (n < completedDrops[i]) { RecordMetric(i, "plinko_drop_points" + suffix, dropPoints[i, n]); if (settledBins[i, n] >= 0) RecordMetric(i, "plinko_supported_landing_bin" + suffix, settledBins[i, n]); }
            }
        }
        public int DropsCompleted(int slot) => completedDrops[slot];
        public int ReleasedDrops(int slot) => releasedDrops[slot];
        public int MissedDrops(int slot) => misses[slot];
        public int LastSettledBin(int slot) => lastBin[slot];
        public float ReleaseRollDegrees(int slot, int drop) => releaseRoll[slot, drop];
        public float ReleaseTiltDegrees(int slot, int drop) => releaseTilt[slot, drop];
        public float ReleaseSpinRadians(int slot, int drop) => releaseSpin[slot, drop];
        public float MeasuredDropPoints(int slot, int drop) => dropPoints[slot, drop];
        public int SettledBinForDrop(int slot, int drop) => settledBins[slot, drop];
        public string PlinkoDiagnostic(int slot) => "drops=" + completedDrops[slot] + "/" + competitionDrops + " released=" + releasedDrops[slot] + " scored=" + deliveries[slot] + " misses=" + misses[slot] + " bin=" + lastBin[slot] + " roll=" + roll[slot].ToString("F1") + " tilt=" + tilt[slot].ToString("F1") + " flight=" + (dropped[slot] ? age - releasedAt[slot] : 0).ToString("F1") + " quiet=" + quiet[slot].ToString("F2") + " outcome=" + lastOutcome[slot];
        protected override Vector3 CameraFocus(int slot) => trailers[CameraParticipant(slot)].Body.worldCenterOfMass;
    }
}
