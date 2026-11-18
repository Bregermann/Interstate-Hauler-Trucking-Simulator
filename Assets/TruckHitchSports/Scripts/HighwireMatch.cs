using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class HighwireMatch : HitchCompetitionSession
    {
        public float crosswind = 2.5f, fallHeight = -20;
        public GameObject[] originalRoadway = System.Array.Empty<GameObject>();
        float standardMatchSeconds = -1;
        public HighwireMovingBarrier[] crossingBarriers = System.Array.Empty<HighwireMovingBarrier>();
        readonly bool[] aiHold = new bool[4];
        public HighwireCourseVariant[] courseVariants = System.Array.Empty<HighwireCourseVariant>();
        HighwireCourseVariant activeCourse;
        readonly float[] stopQuiet = new float[4];
        readonly int[] reverseGatesPassed = new int[4];
        readonly bool[] reverseRecovery = new bool[4], waitingAtBarrier = new bool[4];
        Vector3[] ActiveRoute => activeCourse != null ? activeCourse.Points : route;
        int GateCount => ActiveRoute.Length;
        bool ReverseNode(int node) => activeCourse != null && activeCourse.RequiresReverse(node);
        bool StopNode(int node) => activeCourse != null && activeCourse.RequiresStop(node);
        readonly int[] gate = new int[4];
        readonly float[] jackknife = new float[4];
        readonly float[] survival = new float[4];
        readonly bool[] backing = new bool[4], reverseEntered = new bool[4];
        readonly bool[] failed = new bool[4];
        readonly string[] hitchLossState = new string[4];
        readonly System.Collections.Generic.List<HighwireHitchEventProbe> hitchEventProbes = new System.Collections.Generic.List<HighwireHitchEventProbe>(12);
        readonly float[] survivedSeconds = new float[4], offCentre = new float[4], finishSeconds = new float[4];
        readonly int[] technicalGates = new int[4];
        public override bool ResultEligible(int slot) => slot >= 0 && slot < setup.count && Scores[slot] > 0 && (option % 4 == 2 ? survivedSeconds[slot] > 0 && (Completed[slot] || Elapsed >= matchSeconds) : !failed[slot] && gate[slot] >= GateCount);
        public override string ResultDetail(int slot) => gate[slot] + "/" + GateCount + " GATES · " + (trailers[slot].CargoCondition * 100).ToString("F0") + "% CARGO · " + jackknife[slot].ToString("F1") + "s JACKKNIFE" + (activeCourse != null && option % 4 == 3 ? " · " + reverseGatesPassed[slot] + " REVERSE GATES" : "");
        public override string Title => "JACKKNIFE HIGHWIRE";
        public override string OptionLabel => new[] { "FASTEST CROSSING", "PRECISION HAULING", "SURVIVAL HIGHWIRE", "TECHNICAL JACKKNIFE" }[option % 4];
        public override string Help => "Tow your physically articulated trailer across the suspended course. Movement controls steer/throttle/brake; hold ACTION for the trailer brake. Crosswind affects the actual load. Fastest Crossing ranks successful crossings by elapsed time. Precision Hauling rewards preserved cargo and staying near the roadway center, with measured deviation and jackknife penalties; speed is not its judging criterion. Survival Highwire ranks actual time alive, including when every competitor eventually falls. Technical Jackknife rewards controlled articulated gate passages and requires actual reverse travel in the marked elevated backing section. Precision and Technical variants use their marked alternate elevated roads. Stop signs require both real bodies to settle; the Technical backing sign requires actual reverse motion of tractor and trailer. Moving crossing gates are real obstacles: wait for GO and let the entire load clear. Pass gates in order. A fall or lost hitch ends your attempt; no invisible safety floor supports the trailer.";
        protected override void BeginCompetition() { System.Array.Clear(gate, 0, gate.Length); System.Array.Clear(jackknife, 0, jackknife.Length); System.Array.Clear(survival, 0, 4); System.Array.Clear(backing, 0, 4); System.Array.Clear(reverseEntered, 0, 4); System.Array.Clear(failed, 0, 4); System.Array.Clear(survivedSeconds, 0, 4); System.Array.Clear(offCentre, 0, 4); System.Array.Clear(finishSeconds, 0, 4); System.Array.Clear(technicalGates, 0, 4);
            System.Array.Clear(stopQuiet, 0, 4); System.Array.Clear(reverseGatesPassed, 0, 4); System.Array.Clear(reverseRecovery, 0, 4); System.Array.Clear(waitingAtBarrier, 0, 4);
            System.Array.Clear(hitchLossState, 0, hitchLossState.Length);
            hitchEventProbes.Clear();
            for (int slot = 0; slot < setup.count; slot++)
            {
                InstallHitchEventProbes(trucks[slot].gameObject, slot, "tractor");
                InstallHitchEventProbes(trailers[slot].gameObject, slot, "load");
            }
            activeCourse = null;
            foreach (var course in courseVariants) if (course != null) { bool selected = course.Mode == option % 4; course.gameObject.SetActive(selected); if (selected) activeCourse = course; }
            if (standardMatchSeconds < 0) standardMatchSeconds = matchSeconds;
            matchSeconds = activeCourse != null ? Mathf.Max(standardMatchSeconds, activeCourse.RecommendedSeconds) : standardMatchSeconds;
            foreach (var roadway in originalRoadway) if (roadway != null) roadway.SetActive(option % 4 == 0 || option % 4 == 2);
            foreach (var barrier in crossingBarriers) if (barrier != null) barrier.ResetCycle();
            if (activeCourse == null) foreach (var truck in trucks) truck.tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.Auto;
        }
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = option % 4 == 0 ? (finishSeconds[slot] > 0 ? finishSeconds[slot].ToString("F2") + "s" : "—") : option % 4 == 2 ? survivedSeconds[slot].ToString("F1") + "s" : Scores[slot].ToString("F0");
            detail = (failed[slot] ? "FELL · " : "") + gate[slot] + "/" + GateCount + " GATES";
            if (!Completed[slot] && waitingAtBarrier[slot]) detail += " · WAIT FOR GO";
            else if (!Completed[slot] && ReverseNode(gate[slot])) detail += " · REVERSE BOTH BODIES";
            else if (!Completed[slot] && StopNode(gate[slot])) detail += " · STOP AND BALANCE";
            return true;
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            RecordMetric(slot, "highwire_gates_passed", gate[slot]);
            RecordMetric(slot, "highwire_cargo_condition_percent", trailers[slot].CargoCondition * 100);
            RecordMetric(slot, "highwire_jackknife_seconds", jackknife[slot]);
            RecordMetric(slot, "highwire_survival_seconds", survivedSeconds[slot]);
            RecordMetric(slot, "highwire_off_center_metre_seconds", offCentre[slot]);
            RecordMetric(slot, "highwire_controlled_technical_gates", technicalGates[slot]);
            if (activeCourse != null) RecordMetric(slot, "highwire_actual_reverse_gates", reverseGatesPassed[slot]);
            if (finishSeconds[slot] > 0) RecordMetric(slot, "highwire_finish_seconds", finishSeconds[slot]);
        }
        protected override void TickCompetition(float dt)
        {
            Notice = "BRAKE BEFORE BENDS · ACTION TRAILER BRAKE · KEEP EVERY AXLE ON THE ROAD";
            for (int i = 0; i < setup.count; i++)
            {
                if (Completed[i]) continue;
                Vector3 target = ActiveRoute[Mathf.Min(gate[i], GateCount - 1)] + Vector3.right * (i * 24);
                aiHold[i] = false;
                waitingAtBarrier[i] = activeCourse != null ? activeCourse.BlockedAhead(trucks[i].body.position) : option % 4 == 2 && CrossingBlockedAhead(trucks[i].body.position);
                Vector2 aiDrive = activeCourse != null ? (IsAI(i) ? FollowVariantRoadway(i) : Vector2.zero) : FollowPhysicalRoadway(i);
                if (option % 4 == 2 && waitingAtBarrier[i]) { aiDrive = Vector2.zero; aiHold[i] = true; }
                bool technical = activeCourse == null && option % 4 == 3 && gate[i] == route.Length - 1;
                if (technical && !reverseEntered[i])
                {
                    aiDrive = DriveTo(i, target + Vector3.forward * 25, 3);
                    if (trucks[i].body.position.z > target.z + 22) backing[i] = true;
                    if (backing[i]) aiDrive = new Vector2(Mathf.Clamp((trucks[i].body.position.x - target.x) * .1f, -.5f, .5f), -.25f);
                    if (Vector3.Distance(trucks[i].body.position, target) < 10 && Vector3.Dot(trucks[i].body.linearVelocity, trucks[i].transform.forward) < -.2f) reverseEntered[i] = true;
                }
                if (option % 4 == 2 && gate[i] >= GateCount && Vector3.Distance(trucks[i].body.position, target) < 8) aiDrive = new Vector2(0, -1);
                var drive = IsAI(i) ? aiDrive : commands[i];
                trucks[i].TickDrive(drive);
                if (IsAI(i) && aiHold[i])
                { var input = trucks[i].tractorMotor.vehicle.input; input.states.handbrake = input.states.handbrakeRaw = 1; input.states.brakes = input.states.brakesRaw = 0; }
                trailers[i].SetBrakes(IsAI(i) && aiHold[i] || !IsAI(i) && ActionHeld(i) ? 1 : 0);
                trailers[i].Body.AddForce(Vector3.right * Mathf.Sin(Elapsed * .7f + i) * crosswind * trailers[i].Body.mass);
                float angle = Mathf.Abs(Vector3.SignedAngle(trucks[i].transform.forward, trailers[i].transform.forward, Vector3.up));
                bool reversePass = !ReverseNode(gate[i]) || Vector3.Dot(trucks[i].body.linearVelocity, trucks[i].transform.forward) < -.2f && Vector3.Dot(trailers[i].Body.linearVelocity, trailers[i].transform.forward) < -.1f && couplers[i].PhysicalConstraintPresent && trailers[i].WheelContact;
                bool atNode = Vector3.Distance(trucks[i].body.position, target) < 7 && Vector3.Distance(trailers[i].Body.position, target) < 18;
                if (StopNode(gate[i])) stopQuiet[i] = atNode && trucks[i].body.linearVelocity.magnitude < .8f && trailers[i].Body.linearVelocity.magnitude < .8f && angle < 35 && Vector3.Dot(trucks[i].transform.up, Vector3.up) > .85f && Vector3.Dot(trailers[i].transform.up, Vector3.up) > .85f ? stopQuiet[i] + dt : 0;
                if (angle > 65) jackknife[i] += dt;
                survivedSeconds[i] += dt;
                offCentre[i] += Mathf.Max(0, RouteDeviation(i) - .75f) * dt;
                if (gate[i] < GateCount && (!technical || reverseEntered[i]) && reversePass && (!StopNode(gate[i]) || stopQuiet[i] > .6f) && atNode)
                {
                    if (option % 4 == 1 || option % 4 == 3) AddScore(i, 100 * trailers[i].CargoCondition, "highwire-gate");
                    if (option % 4 == 3 && angle >= 12 && angle <= 55 && trailers[i].WheelContact && trailers[i].Body.angularVelocity.magnitude < 1 && trucks[i].body.linearVelocity.magnitude < 6)
                    { technicalGates[i]++; AddScore(i, angle * 4, "controlled-articulation-gate"); }
                    if (ReverseNode(gate[i])) reverseGatesPassed[i]++;
                    gate[i]++; stopQuiet[i] = 0;
                }
                if (option % 4 == 2)
                { survival[i] += dt; if (survival[i] > 5 || Elapsed >= matchSeconds) { AddScore(i, survival[i] * 1000, "highwire-survival"); survival[i] = 0; } }
                if (gate[i] >= GateCount && option % 4 != 2)
                {
                    finishSeconds[i] = Elapsed;
                    double outcome = option % 4 == 0 ? 1 + Mathf.Max(0, matchSeconds - Elapsed) * 1000 : Mathf.Max(0, 1200 * trailers[i].CargoCondition - offCentre[i] * 12 - jackknife[i] * 30);
                    AddScore(i, outcome, "safe-crossing"); Completed[i] = true;
                }
                if (trucks[i].body.position.y < fallHeight || trailers[i].Body.position.y < fallHeight || couplers[i].Attached == null)
                {
                    if (couplers[i].Attached == null && hitchLossState[i] == null)
                    {
                        hitchLossState[i] = CaptureHitchLoss(i);
                        Debug.Log("HH10_HIGHWIRE_ACTUAL_HITCH_LOSS " + hitchLossState[i]);
                    }
                    if (option % 4 == 2 && survival[i] > 0) { AddScore(i, survival[i] * 1000, "highwire-survival"); survival[i] = 0; } Completed[i] = failed[i] = true; Notice = setup.players[i].name + " LOST THE HIGHWIRE"; }
            }
        }
        Vector2 FollowVariantRoadway(int slot)
        {
            int segment = Mathf.Min(gate[slot], GateCount - 1); var truck = trucks[slot];
            Vector3 offset = Vector3.right * slot * 24;
            Vector3 a = segment == 0 ? starts[slot] : ActiveRoute[segment - 1] + offset;
            Vector3 b = ActiveRoute[segment] + offset;
            bool reverse = ReverseNode(segment);
            float articulation = Vector3.Angle(truck.transform.forward, trailers[slot].transform.forward);
            if (reverse && articulation > 50) reverseRecovery[slot] = true;
            if (reverseRecovery[slot] && articulation < 25) reverseRecovery[slot] = false;
            bool backingNow = reverse && !reverseRecovery[slot];
            var vehicle = truck.tractorMotor.vehicle; var transmission = vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            float signedSpeed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            if (backingNow ? transmission.Gear >= 0 : transmission.Gear <= 0)
            {
                aiHold[slot] = true;
                if (Mathf.Abs(signedSpeed) < .3f) transmission.ShiftInto(backingNow ? -1 : 1);
                return Vector2.zero;
            }
            Vector3 line = Vector3.ProjectOnPlane(b - a, Vector3.up);
            Vector3 flat = Vector3.ProjectOnPlane(truck.body.position - a, Vector3.up);
            float along = line.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(flat, line) / line.sqrMagnitude) : 1;
            float remaining = line.magnitude * (1 - along);
            Vector3 target = a + line * along + line.normalized * Mathf.Min(remaining, backingNow ? 5 : 7);
            if (reverseRecovery[slot]) target = a + Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up).normalized * 8;
            Vector3 local = truck.transform.InverseTransformPoint(target);
            float steer = backingNow ? Mathf.Atan2(local.x, Mathf.Max(.5f, -local.z)) * 1.8f + truck.body.angularVelocity.y * .3f : Mathf.Atan2(local.x, local.z) * 1.8f;
            float requested = backingNow ? -1.8f : reverseRecovery[slot] ? 2 : 4.3f;
            if (!backingNow && articulation > 35) requested = Mathf.Min(requested, 2.5f);
            waitingAtBarrier[slot] = activeCourse.BlockedAhead(truck.body.position);
            if (waitingAtBarrier[slot] || StopNode(segment) && Vector3.ProjectOnPlane(truck.body.position - b, Vector3.up).magnitude < 5)
            {
                aiHold[slot] = true;
                return Vector2.zero;
            }
            return new Vector2(Mathf.Clamp(steer, backingNow ? -.6f : -1, backingNow ? .6f : 1), Mathf.Clamp((requested - signedSpeed) * .4f, -.7f, .75f));
        }
        bool CrossingBlockedAhead(Vector3 point)
        { foreach (var barrier in crossingBarriers) if (barrier != null && barrier.gameObject.activeInHierarchy && barrier.ShouldWait(point)) return true; return false; }
        public string VariantDiagnostic(int slot) => "course=" + (activeCourse != null ? activeCourse.name : "original") + " gate=" + gate[slot] + "/" + GateCount + " reverse=" + ReverseNode(gate[slot]) + " reversePassed=" + reverseGatesPassed[slot] + " quiet=" + stopQuiet[slot].ToString("F2") + " barrier=" + waitingAtBarrier[slot] + " recovery=" + reverseRecovery[slot] + " tractor=" + trucks[slot].body.position.ToString("F1") + " trailer=" + trailers[slot].Body.position.ToString("F1") + " velocity=" + trucks[slot].body.linearVelocity.ToString("F1") + " gear=" + trucks[slot].tractorMotor.vehicle.powertrain.transmission.Gear + (hitchLossState[slot] != null ? " loss=[" + hitchLossState[slot] + "]" : "");
        public int ReversingGatesPassed(int slot) => reverseGatesPassed[slot];
        void InstallHitchEventProbes(GameObject rig, int slot, string role)
        {
            foreach (var body in rig.GetComponentsInChildren<Rigidbody>(true))
            {
                var probe = body.GetComponent<HighwireHitchEventProbe>();
                if (probe == null) probe = body.gameObject.AddComponent<HighwireHitchEventProbe>();
                probe.Configure(couplers[slot], slot, role + "/" + body.name);
                hitchEventProbes.Add(probe);
            }
        }
        string CaptureHitchLoss(int slot)
        {
            var text = new System.Text.StringBuilder("age=").Append(Elapsed.ToString("F3")).Append(" seat=").Append(slot).Append(" gate=").Append(gate[slot]).Append(" reason=").Append(couplers[slot].LastReason);
            foreach (var probe in hitchEventProbes) if (probe.Seat == slot) text.Append(" [").Append(probe.Describe()).Append(']');
            foreach (var barrier in crossingBarriers)
            {
                if (barrier == null || !barrier.gameObject.activeInHierarchy || Vector3.Distance(trucks[slot].body.position, barrier.ClosedPosition) > 40) continue;
                var shape = barrier.Body.GetComponent<Collider>();
                text.Append(" [gate body=").Append(barrier.Body.position.ToString("F3")).Append(" closed=").Append(barrier.ClosedPosition.ToString("F3")).Append(" opening=").Append(barrier.Opening.ToString("F3")).Append(" held=").Append(barrier.OccupancyHeld).Append(" hull=").Append(shape != null ? shape.bounds.ToString("F3") : "none").Append(']');
            }
            return text.ToString();
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
            if (activeCourse != null) return activeCourse.Deviation(trailers[slot].Body.position, starts[slot], slot * 24);
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
    // Bounded read-only event observation. Native NWH owns the actual input,
    // joint creation/break force and detachment. This records their sequence.
    [DefaultExecutionOrder(-81)]
    public sealed class HighwireHitchEventProbe : MonoBehaviour
    {
        public int Seat { get; private set; }
        HitchCoupler coupler;
        Rigidbody body;
        ConfigurableJoint joint;
        string role;
        bool inputBeforeNative, nativeDetach, observedDetachInput;
        string strongestContact = "none";
        float strongestContactSpeed, detachInputAt;
        float breakForce, brokenAt, detachedAt, peakForce, contactAt, contactSpeed;
        Vector3 jointForce, contactPoint, contactNormal;
        Collider otherShape, selfShape;
        public void Configure(HitchCoupler value, int seat, string label)
        {
            if (coupler != null && coupler.Hitch != null && body != null && body.gameObject == coupler.gameObject) coupler.Hitch.module.onTrailerDetach.RemoveListener(OnNativeDetach);
            coupler = value; Seat = seat; role = label; body = GetComponent<Rigidbody>(); joint = null;
            inputBeforeNative = nativeDetach = observedDetachInput = false;
            strongestContact = "none"; strongestContactSpeed = detachInputAt = 0;
            breakForce = brokenAt = detachedAt = peakForce = contactAt = contactSpeed = 0;
            jointForce = contactPoint = contactNormal = Vector3.zero; otherShape = selfShape = null;
            if (body.gameObject == coupler.gameObject) coupler.Hitch.module.onTrailerDetach.AddListener(OnNativeDetach);
        }
        void FixedUpdate()
        {
            if (coupler == null || body == null || body.gameObject != coupler.gameObject) return;
            inputBeforeNative = coupler.Hitch.module.vehicleController.input.TrailerAttachDetach;
            if (inputBeforeNative && coupler.Attached != null) { observedDetachInput = true; detachInputAt = Time.fixedTime; }
            if (joint == null && coupler.Attached != null)
                foreach (var candidate in body.GetComponents<ConfigurableJoint>()) if (candidate.connectedBody == coupler.Attached.Body) { joint = candidate; break; }
            if (joint != null) { jointForce = joint.currentForce; peakForce = Mathf.Max(peakForce, jointForce.magnitude); }
        }
        void OnJointBreak(float force) { breakForce = force; brokenAt = Time.fixedTime; }
        void OnNativeDetach() { nativeDetach = true; detachedAt = Time.fixedTime; }
        void OnCollisionEnter(Collision collision) => Observe(collision);
        void OnCollisionStay(Collision collision) => Observe(collision);
        void Observe(Collision collision)
        {
            otherShape = collision.collider; contactAt = Time.fixedTime; contactSpeed = collision.relativeVelocity.magnitude;
            if (collision.contactCount == 0) return;
            var contact = collision.GetContact(0); otherShape = contact.otherCollider; selfShape = contact.thisCollider;
            contactPoint = contact.point; contactNormal = contact.normal;
            if (contactSpeed > strongestContactSpeed)
            {
                strongestContactSpeed = contactSpeed;
                strongestContact = (otherShape != null ? otherShape.name : "none") + " self=" + (selfShape != null ? selfShape.name : "none") + " point=" + contactPoint.ToString("F3") + " normal=" + contactNormal.ToString("F3") + " at=" + Time.fixedTime.ToString("F3");
            }
        }
        public string Describe()
        {
            Bounds hull = default; bool first = true;
            foreach (var shape in body.GetComponentsInChildren<Collider>(true))
            {
                if (!shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy || shape.attachedRigidbody != body) continue;
                if (first) { hull = shape.bounds; first = false; } else hull.Encapsulate(shape.bounds);
            }
            return role + " inputBeforeNative=" + inputBeforeNative + " detachInputObserved=" + observedDetachInput + " inputAt=" + detachInputAt.ToString("F3") + " nativeDetached=" + nativeDetach + " detachedAt=" + detachedAt.ToString("F3") + " jointBreakForce=" + breakForce.ToString("F0") + " brokenAt=" + brokenAt.ToString("F3") + " lastJointForce=" + jointForce.ToString("F0") + " peakForce=" + peakForce.ToString("F0") + " physicalHull=" + hull.ToString("F3") + " contactOther=" + (otherShape != null ? otherShape.name : "none") + " contactSelf=" + (selfShape != null ? selfShape.name : "none") + " point=" + contactPoint.ToString("F3") + " normal=" + contactNormal.ToString("F3") + " impact=" + contactSpeed.ToString("F2") + " contactAge=" + (Time.fixedTime - contactAt).ToString("F3") + " strongestImpact=" + strongestContactSpeed.ToString("F2") + " strongestContact=[" + strongestContact + "]";
        }
        void OnDestroy()
        {
            if (coupler != null && coupler.Hitch != null && body != null && body.gameObject == coupler.gameObject) coupler.Hitch.module.onTrailerDetach.RemoveListener(OnNativeDetach);
        }
    }

}
