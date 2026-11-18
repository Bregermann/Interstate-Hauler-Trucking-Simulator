using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class HandoffMatch : HitchCompetitionSession
    {
        public Transform[] transferZones, finishes;
        [Min(180)] public float extendedMatchSeconds=300;
        float ordinaryMatchSeconds=-1;
        readonly int[] leg = new int[2];
        readonly int[] extendedStaging = new int[4];
        readonly float[] transfer = new float[2];
        readonly bool[] released = new bool[2], reverseEntered = new bool[2], backing = new bool[2];
        readonly bool[] finishedTeam = new bool[2];
        readonly double[] handoffPenalty = new double[2];
        readonly float[] finishSeconds = new float[2];
        readonly float[] legStartedAt = new float[2], previousStationOffset = new float[2], lastCollisionAt = new float[2], penaltySeconds = new float[2];
        readonly float[,] legSplits = new float[2, 4], handoffSeconds = new float[2, 3];
        readonly int[] collisions = new int[2], missedTransfers = new int[2];
        readonly bool[] missedCurrentStation = new bool[2];
        public override bool ResultEligible(int slot) => slot >= 0 && slot < setup.count && finishedTeam[slot % 2] && Scores[slot] > 0;
        public override string Title => "HITCH HANDOFF";
        // Four competitor seats; any vacant human seats are AI partners/opponents.
        public override int MinimumPlayers => 4;
        public override string OptionLabel => new[] { "STANDARD RELAY", "OBSTACLE RELAY", "REVERSE-HITCH RELAY", "EXTENDED TEAM HAUL" }[option % 4];
        public override string Help => "Teams are seats 1+3 against 2+4; unused human seats have actual AI partners/opponents. The trailer is the baton. Tow to the transfer zone, stop and press ACTION to unhitch. Your teammate backs under that same kingpin and holds ACTION to couple. Clear the old tractor before the next latch. Standard Relay has two physical legs. Obstacle Relay adds the offset barrier/chicane. Reverse-Hitch Relay requires an actual backward entry into the transfer zone before releasing. Extended Team Haul alternates both teammates through four real legs, with an editable five-minute default for three physical handoffs; the ordinary two-leg clock is restored in other modes. Each leg split includes its physical handoff; the next leg starts only when the next native hitch locks. Slow handoffs reduce the handoff award. A hard impact adds two seconds; carrying the load past a missed transfer adds five seconds once per station. Reverse-Hitch's required overshoot is exempt. Final cargo condition and penalty-adjusted finish time determine the team result. Both teammates share it. There is no trailer teleport or automatic transfer.";
        protected override void BeginCompetition()
        { if(ordinaryMatchSeconds<0)ordinaryMatchSeconds=matchSeconds;
            matchSeconds=option%4==3?Mathf.Max(ordinaryMatchSeconds,extendedMatchSeconds):ordinaryMatchSeconds;
            System.Array.Clear(leg, 0, 2); System.Array.Clear(transfer, 0, 2); System.Array.Clear(released, 0, 2); System.Array.Clear(reverseEntered, 0, 2); System.Array.Clear(backing, 0, 2); System.Array.Clear(finishedTeam, 0, 2); System.Array.Clear(handoffPenalty, 0, 2); System.Array.Clear(finishSeconds, 0, 2);
            System.Array.Clear(extendedStaging, 0, extendedStaging.Length);
            System.Array.Clear(legSplits, 0, legSplits.Length); System.Array.Clear(handoffSeconds, 0, handoffSeconds.Length);
            System.Array.Clear(collisions, 0, 2); System.Array.Clear(missedTransfers, 0, 2); System.Array.Clear(penaltySeconds, 0, 2); System.Array.Clear(missedCurrentStation, 0, 2);
            for (int team = 0; team < 2; team++) { legStartedAt[team] = 3; previousStationOffset[team] = float.NegativeInfinity; lastCollisionAt[team] = -10; }
            for (int slot = 0; slot < setup.count; slot++) InstallCollisionProbes(trucks[slot].gameObject, slot % 2);
            for (int team = 0; team < 2; team++) InstallCollisionProbes(trailers[team].gameObject, team);
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            int team = slot % 2; string context = "team=" + team;
            RecordMetric(slot, "relay_team_legs_completed", leg[team] + (finishedTeam[team] ? 1 : 0), context);
            RecordMetric(slot, "relay_team_handoffs", leg[team], context);
            RecordMetric(slot, "relay_handoff_penalty_points", handoffPenalty[team], context);
            RecordMetric(slot, "relay_team_finished", finishedTeam[team] ? 1 : 0, context);
            RecordMetric(slot, "relay_collision_count", collisions[team], context);
            RecordMetric(slot, "relay_missed_transfers", missedTransfers[team], context);
            RecordMetric(slot, "relay_penalty_seconds", penaltySeconds[team], context);
            for (int n = 0; n < 4; n++) if (legSplits[team, n] > 0) RecordMetric(slot, "relay_leg_split_seconds", legSplits[team, n], context + ";leg=" + (n + 1));
            for (int n = 0; n < 3; n++) if (handoffSeconds[team, n] > 0) RecordMetric(slot, "relay_handoff_seconds", handoffSeconds[team, n], context + ";leg=" + (n + 1));
            if (finishedTeam[team]) { RecordMetric(slot, "relay_finish_seconds", finishSeconds[team], context); RecordMetric(slot, "relay_race_seconds", Mathf.Max(0, finishSeconds[team] - 3), context); }
        }
        protected override void TickCompetition(float dt)
        {
            Notice = "1 + 3 vs 2 + 4 · STOP, UNHITCH, CLEAR THE KINGPIN, RECOUPLE";
            // Towing/clearing retain ordinary native Auto DNR. Only a receiver's
            // deliberate physical close approach selects a specific direction.
            for (int slot = 0; slot < setup.count; slot++) if (IsAI(slot)) trucks[slot].tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.Auto;
            int legs = option % 4 == 3 ? 4 : 2;
            for (int team = 0; team < 2; team++)
            {
                if (Completed[team]) continue;
                int active = team + (leg[team] % 2) * 2;
                int waiting = team + ((leg[team] + 1) % 2) * 2;
                var trailer = trailers[team];
                Vector3 station = transferZones[team].position + Vector3.forward * leg[team] * 80;
                bool final = leg[team] == legs - 1;
                Vector3 destination = final ? finishes[team].position + Vector3.forward * (legs - 2) * 80 : station;
                if (!released[team])
                {
                    bool inZone = Vector3.Distance(trailer.Body.position, destination) < 10;
                    float stationOffset = Vector3.Dot(trailer.Body.position - destination, Vector3.forward);
                    // Actual swept passage past the receiving station, not a
                    // timer or an AI decision, establishes a missed transfer.
                    bool reverseOvershoot = option % 4 == 2 && !reverseEntered[team];
                    if (!final && !reverseOvershoot && !missedCurrentStation[team] && couplers[active].Attached == trailer && !float.IsNegativeInfinity(previousStationOffset[team]) && previousStationOffset[team] <= 12 && stationOffset > 12)
                    { missedCurrentStation[team] = true; missedTransfers[team]++; penaltySeconds[team] += 5; }
                    previousStationOffset[team] = stationOffset;
                    Vector3 target = destination + Vector3.forward * 12;
                    if (option % 4 == 1 && leg[team] == 0 && trucks[active].body.position.z < 75)
                        target = new Vector3(destination.x + 17, destination.y, 85);
                    Vector2 aiDrive = inZone ? new Vector2(0, -1) : DriveTo(active, target, option % 4 == 1 ? 6 : 9);
                    if (!final && option % 4 == 2 && !reverseEntered[team])
                    {
                        if (trailer.Body.position.z < destination.z + 12) aiDrive = DriveTo(active, destination + Vector3.forward * 28, 3);
                        else backing[team] = true;
                        if (backing[team]) aiDrive = new Vector2(Mathf.Clamp((trucks[active].body.position.x - target.x) * .1f, -.5f, .5f), -.3f);
                        if (inZone && Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.forward) < -.2f) reverseEntered[team] = true;
                    }
                    trucks[active].TickDrive(IsAI(active) ? aiDrive : commands[active]);
                    if (IsAI(active) && inZone && (final || option % 4 != 2 || reverseEntered[team])) ParkAI(active);
                    if (option % 4 == 3 && leg[team] >= 1 && IsAI(waiting))
                    {
                        if (final)
                        {
                            Vector3 clear = station + new Vector3(18, 0, -52);
                            if (Vector3.Distance(trucks[waiting].body.position, clear) < 5) ParkAI(waiting);
                            else trucks[waiting].TickDrive(DriveTo(waiting, clear, 4));
                        }
                        else StageExtendedReceiver(waiting, station, Vector3.forward);
                    }
                    else
                    {
                        Vector3 staging = station + Vector3.forward * 30;
                        trucks[waiting].TickDrive(IsAI(waiting) ? (Vector3.Distance(trucks[waiting].body.position, staging) < 5 ? new Vector2(0, -1) : DriveTo(waiting, staging, 5)) : commands[waiting]);
                        if (IsAI(waiting) && Vector3.Distance(trucks[waiting].body.position, staging) < 5) ParkAI(waiting);
                    }
                    if (inZone && trailer.Body.linearVelocity.magnitude < .9f)
                    {
                        if (final)
                        {
                            double value = Mathf.Max(0, 2400 - (Elapsed + penaltySeconds[team]) * 6) * trailer.CargoCondition;
                            AddScore(team, value, "relay-finish"); AddScore(team + 2, value, "relay-finish"); finishedTeam[team] = true; finishSeconds[team] = Elapsed; legSplits[team, leg[team]] = Mathf.Max(0, Elapsed - legStartedAt[team]); Completed[team] = Completed[team + 2] = true;
                        }
                        else if ((option % 4 != 2 || reverseEntered[team]) && (IsAI(active) || actions[active]))
                        { couplers[active].Release(); trailer.SetBrakes(1); released[team] = true; transfer[team] = Elapsed; }
                    }
                }
                else
                {
                    trucks[active].TickDrive(IsAI(active) ? DriveTo(active, station + new Vector3(18, 0, 28), 4) : commands[active]);
                    if (IsAI(waiting))
                    {
                        bool ready = true;
                        if (option % 4 == 3 && leg[team] >= 1)
                        {
                            Vector3 forward = Vector3.ProjectOnPlane(trailer.transform.forward, Vector3.up).normalized;
                            Vector3 localAnchor = trucks[waiting].transform.InverseTransformPoint(couplers[waiting].Anchor.position);
                            Vector3 receivingRoot = trailer.Kingpin.position - Quaternion.LookRotation(forward, Vector3.up) * localAnchor;
                            ready = StageExtendedReceiver(waiting, receivingRoot, forward);
                        }
                        if (ready) ApproachRelayHitch(waiting, trailer, dt);
                    }
                    else { trucks[waiting].TickDrive(commands[waiting]); if (ActionHeld(waiting)) couplers[waiting].GuideCoupling(trailer, dt); }
                    if (couplers[waiting].Attached == trailer)
                    {
                        int completedLeg = leg[team];
                        legSplits[team, completedLeg] = Mathf.Max(0, Elapsed - legStartedAt[team]);
                        handoffSeconds[team, completedLeg] = Mathf.Max(0, Elapsed - transfer[team]);
                        legStartedAt[team] = Elapsed; previousStationOffset[team] = float.NegativeInfinity; missedCurrentStation[team] = false;
                        leg[team]++; released[team] = reverseEntered[team] = backing[team] = false;
                        extendedStaging[team] = extendedStaging[team + 2] = 0;
                        double points = Mathf.Max(20, 200 - (Elapsed - transfer[team]) * 2);
                        handoffPenalty[team] += 200 - points;
                        AddScore(active, points, "relay-handoff"); AddScore(waiting, points, "relay-handoff");
                    }
                }
                if (trailer.Body.position.y < -20) Completed[team] = Completed[team + 2] = true;
            }
        }
        public float LegSplitSeconds(int team, int completedLeg) => team >= 0 && team < 2 && completedLeg >= 0 && completedLeg < 4 ? legSplits[team, completedLeg] : 0;
        public float HandoffWaitSeconds(int team, int completedLeg) => team >= 0 && team < 2 && completedLeg >= 0 && completedLeg < 3 ? handoffSeconds[team, completedLeg] : 0;
        public int CollisionCount(int team) => team >= 0 && team < 2 ? collisions[team] : 0;
        public int MissedTransferCount(int team) => team >= 0 && team < 2 ? missedTransfers[team] : 0;
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            int team = slot % 2;
            value = Scores[slot].ToString("F0");
            detail = (finishedTeam[team] ? "FINISHED" : Completed[team] ? "LOST LOAD" : "LEG " + (leg[team] + 1) + "/" + (option % 4 == 3 ? 4 : 2)) + " · +" + penaltySeconds[team].ToString("F0") + "s PEN";
            if (!Completed[team]) detail += " · " + Mathf.Max(0, Elapsed - legStartedAt[team]).ToString("F1") + "s";
            else if (leg[team] > 0) detail += " · LAST " + legSplits[team, leg[team] - 1].ToString("F1") + "s";
            return true;
        }
        public override string ResultDetail(int slot)
        {
            int team = slot % 2;
            return (finishedTeam[team] ? "TOTAL " + Mathf.Max(0, finishSeconds[team] - 3).ToString("F1") + "s · " : "UNFINISHED · ") + SplitSummary(team) + " · " + collisions[team] + " HITS · " + missedTransfers[team] + " MISSES · +" + penaltySeconds[team].ToString("F0") + "s";
        }
        public override string Results => base.Results + "\nP1 + P3: " + ResultDetail(0) + "\nP2 + P4: " + ResultDetail(1);
        string SplitSummary(int team)
        {
            var text = new System.Text.StringBuilder("SPLITS");
            for (int n = 0; n < 4; n++) if (legSplits[team, n] > 0) text.Append(" L").Append(n + 1).Append(' ').Append(legSplits[team, n].ToString("F1")).Append('s');
            return text.ToString();
        }
        void InstallCollisionProbes(GameObject rig, int team)
        {
            foreach (var body in rig.GetComponentsInChildren<Rigidbody>(true))
            {
                var probe = body.GetComponent<HandoffCollisionProbe>();
                if (probe == null) probe = body.gameObject.AddComponent<HandoffCollisionProbe>();
                probe.Configure(this, team);
            }
        }
        public void ReportPhysicalCollision(int team, Rigidbody source, Collision collision)
        {
            if (!Live || Elapsed < 3 || team < 0 || team > 1 || Completed[team] || Elapsed - lastCollisionAt[team] < .75f) return;
            var ownCoupler = source.GetComponentInParent<HitchCoupler>();
            var sourceTrailer = source.GetComponentInParent<TrailerBody>();
            var otherTrailer = collision.collider.GetComponentInParent<TrailerBody>();
            var otherCoupler = collision.collider.GetComponentInParent<HitchCoupler>();
            if ((ownCoupler != null && ownCoupler.Attached == otherTrailer && otherTrailer != null) || (sourceTrailer != null && otherCoupler != null && sourceTrailer.Owner == otherCoupler.Participant)) return;
            float normalSpeed = 0;
            for (int n = 0; n < collision.contactCount; n++) normalSpeed = Mathf.Max(normalSpeed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(n).normal)));
            if (normalSpeed < 3.5f) return;
            // Cab/chassis/axle callbacks from one real impact share one bounded
            // team cooldown. Road contact at speed is not itself a collision.
            lastCollisionAt[team] = Elapsed; collisions[team]++; penaltySeconds[team] += 2;
        }
        bool StageExtendedReceiver(int slot, Vector3 receivingRoot, Vector3 forward)
        {
            var truck = trucks[slot];
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 offset = Vector3.ProjectOnPlane(truck.body.position - receivingRoot, Vector3.up);
            float ahead = Vector3.Dot(offset, forward), lateral = Vector3.Dot(offset, right);
            float heading = Vector3.Angle(Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up), forward);
            if (extendedStaging[slot] >= 3)
            {
                // Keep the proven close backing controller once actually
                // staged. Only a genuine loss of the lane restarts the route.
                if (heading < 35 && Mathf.Abs(lateral) < 6 && ahead > -2) return true;
                extendedStaging[slot] = 0;
            }
            Vector3 target;
            if (extendedStaging[slot] == 0)
            {
                // Travel around the trailer's rear and flank; a direct line to
                // its pin was driving the returning driver into its cargo.
                target = receivingRoot + right * 18 + forward * (ahead > 8 ? Mathf.Max(18, ahead + 5) : -18);
                if (Mathf.Abs(lateral - 18) < 2 && ahead >= -22)
                    extendedStaging[slot] = 1;
            }
            else if (extendedStaging[slot] == 1)
            {
                target = receivingRoot + right * 18 + forward * Mathf.Max(18, ahead + 8);
                if (Mathf.Abs(lateral - 18) < 4 && ahead >= 14)
                    extendedStaging[slot] = 2;
            }
            else
            {
                // Geometric forward lane alignment, then reverse from ahead.
                // This uses real steering/gear/velocity, never a pose snap.
                target = receivingRoot + forward * (ahead + 10);
                if (heading < 8 && Mathf.Abs(lateral) < 1.25f && ahead > 8)
                { extendedStaging[slot] = 3; ParkAI(slot); return false; }
            }
            var vehicle = truck.tractorMotor.vehicle; var transmission = vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            if (transmission.Gear <= 0)
            {
                ParkAI(slot); if (Mathf.Abs(speed) < .3f) transmission.ShiftInto(1); return false;
            }
            Vector3 local = truck.transform.InverseTransformPoint(new Vector3(target.x, truck.body.position.y, target.z));
            float steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f - truck.body.angularVelocity.y * .3f, -1, 1);
            float requestedSpeed = extendedStaging[slot] == 2 ? 2.5f : 5;
            float effort = Mathf.Clamp((requestedSpeed - speed) * .5f, -.7f, .7f);
            if (!vehicle.powertrain.engine.IsRunning) effort = Mathf.Max(.35f, effort);
            truck.TickDrive(new Vector2(steer, effort));
            return false;
        }
        bool ApproachRelayHitch(int slot, TrailerBody trailer, float dt)
        {
            var truck = trucks[slot]; var coupler = couplers[slot];
            var vehicle = truck.tractorMotor.vehicle; var transmission = vehicle.powertrain.transmission;
            Vector3 forward = Vector3.ProjectOnPlane(trailer.transform.forward, Vector3.up).normalized;
            Vector3 localAnchor = truck.transform.InverseTransformPoint(coupler.Anchor.position);
            Vector3 root = trailer.Kingpin.position - Quaternion.LookRotation(forward, Vector3.up) * localAnchor;
            Vector3 error = root - truck.body.position;
            bool reverse = Vector3.Dot(error, truck.transform.forward) < 0;
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            if (reverse ? transmission.Gear >= 0 : transmission.Gear <= 0)
            {
                ParkAI(slot);
                if (Mathf.Abs(speed) < .3f) transmission.ShiftInto(reverse ? -1 : 1);
                return false;
            }
            float distance = Vector3.Distance(coupler.Anchor.position, trailer.Kingpin.position);
            Vector3 local = truck.transform.InverseTransformPoint(root);
            float steer;
            if (reverse)
            {
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float lateral = Vector3.Dot(truck.body.position - root, right);
                float lookahead = Mathf.Clamp(Mathf.Abs(Vector3.Dot(error, forward)), 4, 12);
                Vector3 desiredHeading = Quaternion.AngleAxis(Mathf.Atan2(lateral, lookahead) * Mathf.Rad2Deg, Vector3.up) * forward;
                float headingError = Vector3.SignedAngle(truck.transform.forward, desiredHeading, Vector3.up);
                steer = Mathf.Clamp(-headingError * .07f + truck.body.angularVelocity.y * .6f, -.65f, .65f);
            }
            else steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f, -.65f, .65f);
            float requestedSpeed = Mathf.Clamp(distance * .18f, .12f, 2) * (reverse ? -1 : 1);
            float effort = Mathf.Clamp((requestedSpeed - speed) * .7f, -.65f, .65f);
            // NWH's actual autostarter requires swapped throttle >0.2. The
            // shared legacy +/-0.18 approach cannot start a waiting receiver's
            // engine; brake=1 while parked used to mask this missing demand.
            if (!vehicle.powertrain.engine.IsRunning && distance > .65f) effort = reverse ? -.35f : .35f;
            truck.TickDrive(new Vector2(steer, distance < .6f ? 0 : effort));
            return coupler.GuideCoupling(trailer, dt);
        }
        public string HandoffApproachDiagnostic(int slot)
        {
            var vehicle = trucks[slot].tractorMotor.vehicle;
            return "extendedStage=" + extendedStaging[slot] + " engineRunning=" + vehicle.powertrain.engine.IsRunning + " gear=" + vehicle.powertrain.transmission.Gear + " forwardSpeed=" + Vector3.Dot(trucks[slot].body.linearVelocity, trucks[slot].transform.forward).ToString("F2") + " swappedThrottle=" + vehicle.input.InputSwappedThrottle.ToString("F2") + " swappedBrakes=" + vehicle.input.InputSwappedBrakes.ToString("F2") + " rawBrake=" + vehicle.input.states.brakesRaw.ToString("F2") + " handbrake=" + vehicle.input.states.handbrakeRaw.ToString("F2") + " " + couplers[slot].DiagnosticState(trailers[slot % 2]);
        }
        void ParkAI(int slot)
        {
            trucks[slot].TickDrive(Vector2.zero); var input = trucks[slot].tractorMotor.vehicle.input;
            input.states.handbrake = input.states.handbrakeRaw = 1; input.states.brakes = input.states.brakesRaw = 0;
        }
    }
}
