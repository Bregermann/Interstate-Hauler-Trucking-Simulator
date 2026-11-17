using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class HighwireMatch : HitchCompetitionSession
    {
        public float crosswind = 2.5f, fallHeight = -20;
        readonly int[] gate = new int[4];
        readonly float[] jackknife = new float[4];
        readonly float[] survival = new float[4];
        readonly bool[] backing = new bool[4], reverseEntered = new bool[4];
        readonly bool[] failed = new bool[4];
        readonly float[] survivedSeconds = new float[4], offCentre = new float[4], finishSeconds = new float[4];
        readonly int[] technicalGates = new int[4];
        public override bool ResultEligible(int slot) => slot >= 0 && slot < setup.count && Scores[slot] > 0 && (option % 4 == 2 ? survivedSeconds[slot] > 0 && (Completed[slot] || Elapsed >= matchSeconds) : !failed[slot] && gate[slot] >= route.Length);
        public override string ResultDetail(int slot) => gate[slot] + "/" + route.Length + " GATES · " + (trailers[slot].CargoCondition * 100).ToString("F0") + "% CARGO · " + jackknife[slot].ToString("F1") + "s JACKKNIFE";
        public override string Title => "JACKKNIFE HIGHWIRE";
        public override string OptionLabel => new[] { "FASTEST CROSSING", "PRECISION HAULING", "SURVIVAL HIGHWIRE", "TECHNICAL JACKKNIFE" }[option % 4];
        public override string Help => "Tow your physically articulated trailer across the suspended course. Movement controls steer/throttle/brake; hold ACTION for the trailer brake. Crosswind affects the actual load. Fastest Crossing ranks successful crossings by elapsed time. Precision Hauling rewards preserved cargo and staying near the roadway center, with measured deviation and jackknife penalties; speed is not its judging criterion. Survival Highwire ranks actual time alive, including when every competitor eventually falls. Technical Jackknife rewards controlled articulated gate passages and requires reversing into the final receiving platform. Pass gates in order. A fall or lost hitch ends your attempt; no invisible safety floor supports the trailer.";
        protected override void BeginCompetition() { System.Array.Clear(gate, 0, gate.Length); System.Array.Clear(jackknife, 0, jackknife.Length); System.Array.Clear(survival, 0, 4); System.Array.Clear(backing, 0, 4); System.Array.Clear(reverseEntered, 0, 4); System.Array.Clear(failed, 0, 4); System.Array.Clear(survivedSeconds, 0, 4); System.Array.Clear(offCentre, 0, 4); System.Array.Clear(finishSeconds, 0, 4); System.Array.Clear(technicalGates, 0, 4); }
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = option % 4 == 0 ? (finishSeconds[slot] > 0 ? finishSeconds[slot].ToString("F2") + "s" : "—") : option % 4 == 2 ? survivedSeconds[slot].ToString("F1") + "s" : Scores[slot].ToString("F0");
            detail = (failed[slot] ? "FELL · " : "") + gate[slot] + "/" + route.Length + " GATES"; return true;
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            RecordMetric(slot, "highwire_gates_passed", gate[slot]);
            RecordMetric(slot, "highwire_cargo_condition_percent", trailers[slot].CargoCondition * 100);
            RecordMetric(slot, "highwire_jackknife_seconds", jackknife[slot]);
            RecordMetric(slot, "highwire_survival_seconds", survivedSeconds[slot]);
            RecordMetric(slot, "highwire_off_center_metre_seconds", offCentre[slot]);
            RecordMetric(slot, "highwire_controlled_technical_gates", technicalGates[slot]);
            if (finishSeconds[slot] > 0) RecordMetric(slot, "highwire_finish_seconds", finishSeconds[slot]);
        }
        protected override void TickCompetition(float dt)
        {
            Notice = "BRAKE BEFORE BENDS · ACTION TRAILER BRAKE · KEEP EVERY AXLE ON THE ROAD";
            for (int i = 0; i < setup.count; i++)
            {
                if (Completed[i]) continue;
                Vector3 target = route[Mathf.Min(gate[i], route.Length - 1)] + Vector3.right * (i * 24);
                Vector2 aiDrive = FollowPhysicalRoadway(i);
                bool technical = option % 4 == 3 && gate[i] == route.Length - 1;
                if (technical && !reverseEntered[i])
                {
                    aiDrive = DriveTo(i, target + Vector3.forward * 25, 3);
                    if (trucks[i].body.position.z > target.z + 22) backing[i] = true;
                    if (backing[i]) aiDrive = new Vector2(Mathf.Clamp((trucks[i].body.position.x - target.x) * .1f, -.5f, .5f), -.25f);
                    if (Vector3.Distance(trucks[i].body.position, target) < 10 && Vector3.Dot(trucks[i].body.linearVelocity, trucks[i].transform.forward) < -.2f) reverseEntered[i] = true;
                }
                if (option % 4 == 2 && gate[i] >= route.Length && Vector3.Distance(trucks[i].body.position, target) < 8) aiDrive = new Vector2(0, -1);
                var drive = IsAI(i) ? aiDrive : commands[i];
                trucks[i].TickDrive(drive); trailers[i].SetBrakes(!IsAI(i) && ActionHeld(i) ? 1 : 0);
                trailers[i].Body.AddForce(Vector3.right * Mathf.Sin(Elapsed * .7f + i) * crosswind * trailers[i].Body.mass);
                float angle = Mathf.Abs(Vector3.SignedAngle(trucks[i].transform.forward, trailers[i].transform.forward, Vector3.up));
                if (angle > 65) jackknife[i] += dt;
                survivedSeconds[i] += dt;
                offCentre[i] += Mathf.Max(0, RouteDeviation(i) - .75f) * dt;
                if (gate[i] < route.Length && (!technical || reverseEntered[i]) && Vector3.Distance(trucks[i].body.position, target) < 7 && Vector3.Distance(trailers[i].Body.position, target) < 18)
                {
                    if (option % 4 == 1 || option % 4 == 3) AddScore(i, 100 * trailers[i].CargoCondition, "highwire-gate");
                    if (option % 4 == 3 && angle >= 12 && angle <= 55 && trailers[i].WheelContact && trailers[i].Body.angularVelocity.magnitude < 1 && trucks[i].body.linearVelocity.magnitude < 6)
                    { technicalGates[i]++; AddScore(i, angle * 4, "controlled-articulation-gate"); }
                    gate[i]++;
                }
                if (option % 4 == 2)
                { survival[i] += dt; if (survival[i] > 5 || Elapsed >= matchSeconds) { AddScore(i, survival[i] * 1000, "highwire-survival"); survival[i] = 0; } }
                if (gate[i] >= route.Length && option % 4 != 2)
                {
                    finishSeconds[i] = Elapsed;
                    double outcome = option % 4 == 0 ? 1 + Mathf.Max(0, matchSeconds - Elapsed) * 1000 : Mathf.Max(0, 1200 * trailers[i].CargoCondition - offCentre[i] * 12 - jackknife[i] * 30);
                    AddScore(i, outcome, "safe-crossing"); Completed[i] = true;
                }
                if (trucks[i].body.position.y < fallHeight || trailers[i].Body.position.y < fallHeight || couplers[i].Attached == null)
                { if (option % 4 == 2 && survival[i] > 0) { AddScore(i, survival[i] * 1000, "highwire-survival"); survival[i] = 0; } Completed[i] = failed[i] = true; Notice = setup.players[i].name + " LOST THE HIGHWIRE"; }
            }
        }
        Vector2 FollowPhysicalRoadway(int slot)
        {
            int segment = Mathf.Min(gate[slot], route.Length - 1);
            Vector3 offset = Vector3.right * slot * 24;
            Vector3 a = segment == 0 ? starts[slot] : route[segment - 1] + offset;
            Vector3 b = route[segment] + offset;
            Vector3 line = Vector3.ProjectOnPlane(b - a, Vector3.up);
            Vector3 position = trucks[slot].body.position;
            float along = line.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(position - a, Vector3.up), line) / line.sqrMagnitude) : 1;
            Vector3 closest = a + line * along;
            float speed = Vector3.ProjectOnPlane(trucks[slot].body.linearVelocity, Vector3.up).magnitude;
            float lookahead = 5 + Mathf.Min(speed, 5) * .4f;
            float remaining = line.magnitude * (1 - along);
            Vector3 target = closest + line.normalized * Mathf.Min(remaining, lookahead);
            if (remaining < lookahead && segment + 1 < route.Length)
            {
                Vector3 next = Vector3.ProjectOnPlane(route[segment + 1] + offset - b, Vector3.up);
                target = b + next.normalized * Mathf.Min(lookahead - remaining, next.magnitude);
            }
            float requested = 4.5f + (int)setup.players[slot].difficulty * .3f;
            if (remaining < 16 && segment + 1 < route.Length) requested = Mathf.Min(requested, 3.3f);
            float articulation = Vector3.Angle(trucks[slot].transform.forward, trailers[slot].transform.forward);
            if (articulation > 35) requested = Mathf.Min(requested, 2.5f);
            Vector3 local = trucks[slot].transform.InverseTransformPoint(target);
            float steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f, -1, 1);
            float forward = Vector3.Dot(trucks[slot].body.linearVelocity, trucks[slot].transform.forward);
            return new Vector2(steer, Mathf.Clamp((requested - forward) * .4f, -.7f, .75f));
        }
        float RouteDeviation(int slot)
        {
            Vector3 point = trailers[slot].Body.position, a = starts[slot]; float nearest = float.MaxValue;
            foreach (var center in route)
            {
                Vector3 b = center + Vector3.right * slot * 24, ab = Vector3.ProjectOnPlane(b - a, Vector3.up);
                float t = ab.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(point - a, Vector3.up), ab) / ab.sqrMagnitude) : 0;
                nearest = Mathf.Min(nearest, Vector3.ProjectOnPlane(point - (a + ab * t), Vector3.up).magnitude); a = b;
            }
            return nearest;
        }
    }
}
