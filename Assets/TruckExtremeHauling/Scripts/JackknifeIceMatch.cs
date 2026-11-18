using UnityEngine;
using LWS.TruckBowling;

namespace LWS.TruckExtremeHauling
{
    public sealed class JackknifeIceMatch : ExtremeHaulingMatch
    {
        readonly float[] intervals = new float[4], quality = new float[4], gateDrift = new float[4], lastYaw = new float[4], spin = new float[4], scrubDistance = new float[4], lostHitch = new float[4], aiBrake = new float[4];
        readonly int[] gates = new int[4], pendingTurns = new int[4], combos = new int[4];
        readonly bool[] armed = new bool[4];
        readonly float[] syncTime = new float[2], syncQuality = new float[2], matchedSeconds = new float[2], bestSyncWindow = new float[2];
        readonly int[] syncWindows = new int[2];
        readonly float[] synchronizedSteer = new float[4], formationHold = new float[2];
        readonly bool[] pairRecovery = new bool[2];
        readonly float[] recoveryQuiet = new float[2], liveRequested = new float[4], liveSpeed = new float[4], liveTrailerSpeed = new float[4], liveEffort = new float[4], liveSlip = new float[4];
        readonly int[] liveGear = new int[4];
        readonly bool[] liveEngine = new bool[4], liveStalled = new bool[4];
        readonly Vector3[] livePosition = new Vector3[4];
        readonly int[] driftPhase = new int[4];
        readonly float[] phaseSeconds = new float[4], noSlipSeconds = new float[4], validSeconds = new float[4], longestWindow = new float[4], peakLateral = new float[4], peakNearLateral = new float[4];
        readonly float[,] guardSeconds = new float[4, 8];
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"ice_controlled_drift_seconds",validSeconds[i]);RecordMetric(i,"ice_longest_controlled_drift_seconds",longestWindow[i]);RecordMetric(i,"ice_peak_lateral_mps",peakLateral[i]);RecordMetric(i,"ice_gates_completed",gates[i]);RecordMetric(i,"ice_recovered_combo",combos[i]); if (option == 2) { int team = i % 2; RecordMetric(i,"ice_pair_matched_seconds",matchedSeconds[team]); RecordMetric(i,"ice_pair_best_window_seconds",bestSyncWindow[team]); RecordMetric(i,"ice_pair_scoring_windows",syncWindows[team]); } }
        public override string Title => "JACKKNIFE ON ICE";
        public override int MinimumPlayers => option == 2 ? 4 : 1;
        protected override string[] Modes => new[] { "ICE DRIFT CHAMPIONSHIP", "PRECISION TRAILER SKATING", "SYNCHRONIZED JACKKNIFE", "FROZEN FREESTYLE" };
        protected override string ActionHint => "TRAILER BRAKE";
        public override string Help => "Steer/throttle/brake the real low-traction articulated rig; hold ACTION for its independent trailer brake. Drift Championship banks controlled lateral travel near the marked gates, then advances to the next gate. Precision Skating requires four genuine slow, aligned passages through gate centers; parking or circling inside a gate cannot score twice. Synchronized Jackknife pairs seats 1+3 against 2+4, with AI filling unused seats: both trailers must actually drift with matched heading, articulation and speed. Frozen Freestyle banks sideways distance and completed half-turns only after recovering a controllable rig. Stopped jackknifes, unsupported trailers and lost hitches do not score.";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            if (slot < 0 || slot >= setup.count) { value = detail = null; return false; }
            value = scores[slot].ToString("F0");
            if (option == 2)
            {
                int partner = SynchronizationPartner(slot);
                string partnerName = partner >= 0 ? setup.players[partner].name ?? ("P" + (partner + 1)) : "NO PARTNER";
                if (partnerName.Length > 12) partnerName = partnerName.Substring(0, 11) + "…";
                detail = partner < 0 ? "WAITING FOR TEAM" : "P" + (partner + 1) + " " + partnerName + " · " + SyncGuidance(slot);
            }
            else detail = option == 3 ? pendingTurns[slot] + " HALF-TURNS TO RECOVER" : gates[slot] + (option == 1 ? "/4 PRECISION GATES" : " DRIFT GATES");
            return true;
        }
        public int SynchronizationPartner(int slot)
        {
            if (option != 2 || slot < 0 || slot >= setup.count) return -1;
            int partner = slot < 2 ? slot + 2 : slot - 2;
            return partner < setup.count ? partner : -1;
        }
        string SyncGuidance(int slot)
        {
            int partner = SynchronizationPartner(slot);
            if (partner < 0 || hitches == null) return "READY UP WITH YOUR TEAM";
            var own = trailers[slot]; var other = trailers[partner];
            if (hitches[slot].Attached != own || hitches[partner].Attached != other) return "PAIR HITCH LOST";
            if (retired[partner]) return "PARTNER OUT";
            if (Vector3.Distance(own.Body.position, other.Body.position) >= 40) return "CLOSE THE GAP";
            if (Vector3.Angle(own.transform.forward, other.transform.forward) >= 15) return "MATCH HEADING";
            float ownSpeed = Vector3.ProjectOnPlane(own.Body.linearVelocity, Vector3.up).magnitude;
            float partnerSpeed = Vector3.ProjectOnPlane(other.Body.linearVelocity, Vector3.up).magnitude;
            if (ownSpeed <= 3 || partnerSpeed <= 3 || Mathf.Abs(ownSpeed - partnerSpeed) >= 3) return "MATCH SPEED ABOVE 7 MPH";
            float ownArticulation = Vector3.SignedAngle(trucks[slot].transform.forward, own.transform.forward, Vector3.up);
            float partnerArticulation = Vector3.SignedAngle(trucks[partner].transform.forward, other.transform.forward, Vector3.up);
            if (Mathf.Abs(Mathf.DeltaAngle(ownArticulation, partnerArticulation)) >= 12) return "MATCH TRAILER LEAN";
            if (syncTime[slot % 2] > 0) return "IN SYNC " + syncTime[slot % 2].ToString("F1") + "s";
            return "SLIDE TOGETHER";
        }
        protected override void BeginMechanic()
        {
            System.Array.Clear(intervals, 0, 4); System.Array.Clear(quality, 0, 4); System.Array.Clear(gateDrift, 0, 4); System.Array.Clear(gates, 0, 4); System.Array.Clear(spin, 0, 4); System.Array.Clear(scrubDistance, 0, 4); System.Array.Clear(pendingTurns, 0, 4); System.Array.Clear(combos, 0, 4); System.Array.Clear(lostHitch, 0, 4); System.Array.Clear(syncTime, 0, 2); System.Array.Clear(syncQuality, 0, 2); System.Array.Clear(matchedSeconds, 0, 2); System.Array.Clear(bestSyncWindow, 0, 2); System.Array.Clear(syncWindows, 0, 2); System.Array.Clear(synchronizedSteer, 0, 4); System.Array.Clear(formationHold, 0, 2); System.Array.Clear(pairRecovery, 0, 2); System.Array.Clear(recoveryQuiet, 0, 2);
            System.Array.Clear(liveRequested, 0, 4); System.Array.Clear(liveSpeed, 0, 4); System.Array.Clear(liveTrailerSpeed, 0, 4); System.Array.Clear(liveEffort, 0, 4); System.Array.Clear(liveSlip, 0, 4); System.Array.Clear(liveGear, 0, 4); System.Array.Clear(liveEngine, 0, 4); System.Array.Clear(liveStalled, 0, 4); System.Array.Clear(livePosition, 0, 4);
            System.Array.Clear(driftPhase, 0, 4); System.Array.Clear(phaseSeconds, 0, 4); System.Array.Clear(noSlipSeconds, 0, 4); System.Array.Clear(validSeconds, 0, 4); System.Array.Clear(longestWindow, 0, 4); System.Array.Clear(peakLateral, 0, 4); System.Array.Clear(peakNearLateral, 0, 4); System.Array.Clear(guardSeconds, 0, guardSeconds.Length);
            for (int i = 0; i < setup.count; i++) { trucks[i].tractorMotor.gripScale = .12f; trailers[i].SetBrakes(0); lastYaw[i] = trailers[i].transform.eulerAngles.y; armed[i] = true; }
        }
        Vector3 Gate(int slot) => objectives[(gates[slot] + slot) % objectives.Length].position;
        protected override Vector3 Destination(int i) => Gate(i);
        protected override Vector2 AICommand(int i, float dt)
        {
            if (option == 0) return DriftCommand(i, dt);
            if (option == 2) return SynchronizedCommand(i, dt);
            var truck = trucks[i]; var trailer = trailers[i]; Vector3 target;
            float requestedSpeed = 6 + (int)setup.players[i].difficulty * 1.3f;
            if (option == 1)
            {
                target = Gate(i) + (truck.body.position - trailer.Body.worldCenterOfMass);
                requestedSpeed = Mathf.Min(5, Mathf.Max(2, Vector3.Distance(trailer.Body.position, Gate(i)) * .25f));
            }
            else
            { float phase = age * (option == 3 ? .24f : .15f) + i * 1.2f; target = Gate(i) + new Vector3(Mathf.Sin(phase) * 22, 0, Mathf.Cos(phase) * 22); }
            Vector3 local = truck.transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z);
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            float articulation = Vector3.Angle(truck.transform.forward, trailer.transform.forward);
            aiBrake[i] = articulation > 65 ? .6f : 0;
            return new Vector2(Mathf.Clamp(angle * 1.5f, -1, 1), Mathf.Clamp((requestedSpeed - speed) * .25f, -.7f, .8f));
        }
        Vector2 SynchronizedCommand(int i, float dt)
        {
            int partner = SynchronizationPartner(i);
            if (partner < 0 || retired[partner]) { aiBrake[i] = 1; synchronizedSteer[i] = 0; return Vector2.zero; }
            var truck = trucks[i]; var trailer = trailers[i]; var otherTruck = trucks[partner]; var other = trailers[partner];
            bool followsPartner = setup.players[partner].source != BowlingSource.AI || i >= 2;
            float gap = Vector3.Distance(trailer.Body.position, other.Body.position);
            float articulation = Vector3.Angle(truck.transform.forward, trailer.transform.forward);
            float headingGap = Vector3.Angle(trailer.transform.forward, other.transform.forward);
            float speedGap = Mathf.Abs(Vector3.ProjectOnPlane(trailer.Body.linearVelocity, Vector3.up).magnitude - Vector3.ProjectOnPlane(other.Body.linearVelocity, Vector3.up).magnitude);
            float ownArt = Vector3.SignedAngle(truck.transform.forward, trailer.transform.forward, Vector3.up);
            float otherArt = Vector3.SignedAngle(otherTruck.transform.forward, other.transform.forward, Vector3.up);
            bool paired = gap < 26 && headingGap < 10 && speedGap < 1.8f && Mathf.Abs(Mathf.DeltaAngle(ownArt, otherArt)) < 8 && trailer.WheelContact && other.WheelContact;
            float steer, requested;
            aiBrake[i] = 0;
            if (followsPartner)
            {
                // A rotating formation has different inner/outer velocities.
                // v + omega x offset is the actual leader's measured motion,
                // not a target pose or forced follower velocity.
                float side = i < 2 ? -1 : 1;
                Vector3 offset = otherTruck.transform.right * side * 13;
                // A full trailer at42 degrees sweeps almost9m laterally.
                // Keep its partner outside that real swept envelope. With the
                // current42m orbit,13m spacing needs only~2.3m/s speed difference,
                // unlike the old26m-radius formation that violated the guard.
                Vector3 formation = otherTruck.body.position + offset;
                Vector3 error = formation - truck.body.position;
                Vector3 desiredVelocity = otherTruck.body.linearVelocity + Vector3.Cross(otherTruck.body.angularVelocity, offset);
                float forwardError = Vector3.Dot(error, otherTruck.transform.forward);
                float crossError = Vector3.Dot(error, otherTruck.transform.right);
                float headingError = Vector3.SignedAngle(truck.transform.forward, otherTruck.transform.forward, Vector3.up);
                bool close = gap < 30 && Mathf.Abs(headingError) < 35;
                if (close)
                {
                    float leaderSteer = setup.players[partner].source == BowlingSource.AI ? synchronizedSteer[partner] : commands[partner].x;
                    float yawError = otherTruck.body.angularVelocity.y - truck.body.angularVelocity.y;
                    float competence = .8f + (int)setup.players[i].difficulty * .2f;
                    float trailerHeadingError = Vector3.SignedAngle(trailer.transform.forward, other.transform.forward, Vector3.up);
                    // Match the measured trailer frame as well as its tractor;
                    // equal tractor headings alone can hide a50-degree trailer
                    // phase error behind opposite articulation angles.
                    steer = Mathf.Clamp(leaderSteer + headingError * .035f * competence + crossError * .018f + yawError * .2f + trailerHeadingError * .008f, -1, 1);
                    requested = Mathf.Clamp(Vector3.Dot(desiredVelocity, truck.transform.forward) + forwardError * .25f, 0, 11);
                    // Share the real initiating input, before lateral slip has
                    // appeared. Waiting for slip missed the short brake pulse.
                    aiBrake[i] = setup.players[partner].source == BowlingSource.AI ? aiBrake[partner] : ActionHeld(partner) ? .65f : 0;
                }
                else
                {
                    Vector3 target = formation + desiredVelocity * .8f + otherTruck.transform.forward * 8;
                    Vector3 local = truck.transform.InverseTransformPoint(target);
                    steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.5f, -1, 1);
                    requested = Mathf.Clamp(otherTruck.body.linearVelocity.magnitude + 2, 5, 10);
                }
            }
            else
            {
                int team = i % 2;
                Vector3 center = (objectives[team].position + objectives[team + 2].position) * .5f + Vector3.back * 65;
                Vector3 radial = Vector3.ProjectOnPlane(truck.body.position - center, Vector3.up);
                if (radial.sqrMagnitude < 1) radial = -truck.transform.forward;
                bool approaching = radial.magnitude > 54;
                Vector3 target = center + (approaching ? radial.normalized : Quaternion.AngleAxis(35, Vector3.up) * radial.normalized) * 42;
                Vector3 local = truck.transform.InverseTransformPoint(target);
                steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.5f, -1, 1);
                // Preserve a physically coordinated maneuver through small
                // feedback delays instead of alternating target speed every
                // time readiness crosses its stricter setup threshold.
                formationHold[team] = paired ? 3 : Mathf.Max(0, formationHold[team] - dt);
                requested = formationHold[team] > 0 ? 6.8f + (int)setup.players[i].difficulty * .5f : 4;
                if (gap > 32) requested = 3;
                float articulationGap = Mathf.Abs(Mathf.DeltaAngle(ownArt, otherArt));
                if (gap < 35 && driftPhase[i] == 0 && (headingGap > 15 || articulationGap > 12)) pairRecovery[team] = true;
                if (pairRecovery[team])
                {
                    recoveryQuiet[team] = headingGap < 6 && articulationGap < 5 && speedGap < 1.5f ? recoveryQuiet[team] + dt : 0;
                    if (recoveryQuiet[team] > .8f) { pairRecovery[team] = false; recoveryQuiet[team] = 0; }
                    else
                    {
                        // Haul both real articulated rigs straight in the
                        // average current trailer direction before attempting
                        // another coordinated turn. No countersteer oscillation
                        // is initiated from mismatched trailer frames.
                        Vector3 straight = Vector3.ProjectOnPlane(trailer.transform.forward + other.transform.forward, Vector3.up);
                        if (straight.sqrMagnitude < .01f) straight = Vector3.ProjectOnPlane(trailer.transform.forward, Vector3.up);
                        Vector3 recoveryLocal = truck.transform.InverseTransformDirection(straight.normalized);
                        steer = Mathf.Clamp(Mathf.Atan2(recoveryLocal.x, recoveryLocal.z) * 1.3f - truck.body.angularVelocity.y * .2f, -.35f, .35f);
                        requested = 5.2f; aiBrake[i] = 0; noSlipSeconds[i] = 0;
                    }
                }
                float lateral = Mathf.Abs(Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.right));
                noSlipSeconds[i] = !pairRecovery[team] && !approaching && paired && lateral < .8f && trailer.Body.linearVelocity.magnitude > 3.5f ? noSlipSeconds[i] + dt : 0;
                if (noSlipSeconds[i] > 1.25f && driftPhase[i] == 0) { driftPhase[i] = 1; phaseSeconds[i] = 0; noSlipSeconds[i] = 0; }
                if (driftPhase[i] != 0)
                {
                    phaseSeconds[i] += dt;
                    if (driftPhase[i] == 1)
                    { steer = -.45f; if (phaseSeconds[i] > .3f) { driftPhase[i] = 2; phaseSeconds[i] = 0; } }
                    else
                    { steer = .6f; aiBrake[i] = phaseSeconds[i] < .25f ? .4f : 0; if (phaseSeconds[i] > 1.1f) { driftPhase[i] = 0; phaseSeconds[i] = 0; } }
                }
            }
            if (articulation > 60 || !trailer.WheelContact)
            { requested = 3; aiBrake[i] = .6f; driftPhase[i] = 0; }
            synchronizedSteer[i] = steer;
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            float effort = Mathf.Clamp((requested - speed) * .3f, -.7f, .8f);
            // Preserve last-live evidence: EndMatch deliberately freezes bodies
            // and disables native motors before terminal test diagnostics run.
            liveRequested[i] = requested; liveSpeed[i] = speed; liveEffort[i] = effort;
            liveTrailerSpeed[i] = trailer.Body.linearVelocity.magnitude; liveSlip[i] = Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.right); livePosition[i] = truck.body.position;
            var native = truck.tractorMotor.vehicle; liveGear[i] = native.powertrain.transmission.Gear; liveEngine[i] = native.powertrain.engine.IsRunning; liveStalled[i] = native.powertrain.engine.IsStalled;
            return new Vector2(steer, effort);
        }
        public string SynchronizationDiagnostic(int slot)
        {
            int partner = SynchronizationPartner(slot);
            if (partner < 0) return "missing partner";
            return "partner=" + partner + " guidance=" + SyncGuidance(slot) + " gap=" + Vector3.Distance(trailers[slot].Body.position, trailers[partner].Body.position).ToString("F1") + " window=" + syncTime[slot % 2].ToString("F2") + " matched=" + matchedSeconds[slot % 2].ToString("F2") + " scoringWindows=" + syncWindows[slot % 2] + " brake=" + aiBrake[slot].ToString("F2") + " steer=" + synchronizedSteer[slot].ToString("F2") + " headingGap=" + Vector3.Angle(trailers[slot].transform.forward, trailers[partner].transform.forward).ToString("F1") + " speeds=" + trailers[slot].Body.linearVelocity.magnitude.ToString("F2") + "/" + trailers[partner].Body.linearVelocity.magnitude.ToString("F2") + " signedSlip=" + Vector3.Dot(trailers[slot].Body.linearVelocity, trailers[slot].transform.right).ToString("F2") + "/" + Vector3.Dot(trailers[partner].Body.linearVelocity, trailers[partner].transform.right).ToString("F2") + " lastLive[requested=" + liveRequested[slot].ToString("F2") + ",tractorSpeed=" + liveSpeed[slot].ToString("F2") + ",trailerSpeed=" + liveTrailerSpeed[slot].ToString("F2") + ",slip=" + liveSlip[slot].ToString("F2") + ",effort=" + liveEffort[slot].ToString("F2") + ",gear=" + liveGear[slot] + ",engine=" + liveEngine[slot] + ",stalled=" + liveStalled[slot] + ",position=" + livePosition[slot].ToString("F1") + ",recovery=" + pairRecovery[slot % 2] + "] articulation=" + Vector3.SignedAngle(trucks[slot].transform.forward, trailers[slot].transform.forward, Vector3.up).ToString("F1") + "/" + Vector3.SignedAngle(trucks[partner].transform.forward, trailers[partner].transform.forward, Vector3.up).ToString("F1");
        }
        Vector2 DriftCommand(int i, float dt)
        {
            var truck = trucks[i]; var trailer = trailers[i];
            Vector3 radial = Vector3.ProjectOnPlane(truck.body.position - Gate(i), Vector3.up);
            float distance = Vector3.ProjectOnPlane(trailer.Body.position - Gate(i), Vector3.up).magnitude;
            float signedSpeed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            float speed = Vector3.ProjectOnPlane(trailer.Body.linearVelocity, Vector3.up).magnitude;
            float lateral = Mathf.Abs(Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.right));
            float articulation = Vector3.Angle(truck.transform.forward, trailer.transform.forward);
            int direction = (i & 1) == 0 ? 1 : -1;
            if (radial.sqrMagnitude < 1) radial = -truck.transform.forward;
            // Follow a geometric orbit using the actual rig position. The old
            // clock-driven target kept advancing while an understeering rig
            // was still approaching, producing loops outside the scoring zone.
            bool approaching = distance > 30;
            Vector3 target = Gate(i) + (approaching ? radial.normalized : Quaternion.AngleAxis(direction * 55, Vector3.up) * radial.normalized) * 18;
            Vector3 local = truck.transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z);
            float steer = Mathf.Clamp(angle * 1.5f, -1, 1);
            float requested = 8.5f + (int)setup.players[i].difficulty * .4f;
            aiBrake[i] = 0;
            if (approaching || articulation > 60 || !trailer.WheelContact)
            {
                driftPhase[i] = 0; phaseSeconds[i] = noSlipSeconds[i] = 0;
                if (articulation > 60) { aiBrake[i] = .6f; requested = 3.5f; }
            }
            else
            {
                noSlipSeconds[i] = speed > 3 && lateral < 1 ? noSlipSeconds[i] + dt : 0;
                if (driftPhase[i] == 0 && noSlipSeconds[i] > 1.25f)
                { driftPhase[i] = 1; phaseSeconds[i] = noSlipSeconds[i] = 0; }
                phaseSeconds[i] += dt;
                float turn = Mathf.Abs(steer) > .1f ? Mathf.Sign(steer) : -direction;
                if (driftPhase[i] == 1)
                {
                    // A short counter-steer then a real locked-trailer-brake
                    // turn initiates slip; no forces/velocities are synthesized.
                    steer = -turn * .8f;
                    if (phaseSeconds[i] >= .25f) { driftPhase[i] = 2; phaseSeconds[i] = 0; }
                }
                else if (driftPhase[i] == 2)
                {
                    steer = turn;
                    aiBrake[i] = phaseSeconds[i] < .18f ? .4f : 0;
                    if (phaseSeconds[i] >= 1) { driftPhase[i] = 0; phaseSeconds[i] = 0; }
                }
            }
            return new Vector2(steer, Mathf.Clamp((requested - signedSpeed) * .3f, -.7f, .8f));
        }
        // Read-only and bounded: the caller chooses when to sample. Guard
        // durations may overlap because more than one condition can fail.
        public string DriftDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            var trailer = trailers[slot];
            float speed = Vector3.ProjectOnPlane(trailer.Body.linearVelocity, Vector3.up).magnitude;
            float lateral = Mathf.Abs(Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.right));
            float angle = Vector3.Angle(trucks[slot].transform.forward, trailer.transform.forward);
            float distance = Vector3.ProjectOnPlane(trailer.Body.position - Gate(slot), Vector3.up).magnitude;
            return "age=" + age.ToString("F1") + " hitch=" + (hitches[slot].Attached == trailer) + " wheel=" + trailer.WheelContact + " up=" + Vector3.Dot(trailer.transform.up, Vector3.up).ToString("F2") + " articulation=" + angle.ToString("F1") + " speed=" + speed.ToString("F2") + " lateral=" + lateral.ToString("F2") + " range=" + distance.ToString("F1") + " yaw=" + Mathf.Abs(trailer.Body.angularVelocity.y).ToString("F2") + " window=" + intervals[slot].ToString("F2") + " validSeconds=" + validSeconds[slot].ToString("F2") + " longestWindow=" + longestWindow[slot].ToString("F2") + " peakLateral=" + peakLateral[slot].ToString("F2") + " peakNearLateral=" + peakNearLateral[slot].ToString("F2") + " rejectedSeconds[hitch=" + guardSeconds[slot, 0].ToString("F1") + ",wheel=" + guardSeconds[slot, 1].ToString("F1") + ",articulation=" + guardSeconds[slot, 2].ToString("F1") + ",tilt=" + guardSeconds[slot, 3].ToString("F1") + ",speed=" + guardSeconds[slot, 4].ToString("F1") + ",lateral=" + guardSeconds[slot, 5].ToString("F1") + ",range=" + guardSeconds[slot, 6].ToString("F1") + ",yaw=" + guardSeconds[slot, 7].ToString("F1") + "] aiPhase=" + driftPhase[slot] + " trailerBrake=" + aiBrake[slot].ToString("F2");
        }
        void ObserveDriftGuards(int i, float angle, float speed, float lateral, float distance, float dt)
        {
            var trailer = trailers[i];
            peakLateral[i] = Mathf.Max(peakLateral[i], lateral);
            if (distance <= 35) peakNearLateral[i] = Mathf.Max(peakNearLateral[i], lateral);
            int rejected = (hitches[i].Attached != trailer ? 1 : 0) | (!trailer.WheelContact ? 2 : 0) | (angle >= 80 ? 4 : 0) | (Vector3.Dot(trailer.transform.up, Vector3.up) <= .65f ? 8 : 0) | (speed < 3 ? 16 : 0) | (lateral < 1 ? 32 : 0) | (distance > 35 ? 64 : 0) | (Mathf.Abs(trailer.Body.angularVelocity.y) > 1.2f ? 128 : 0);
            for (int guard = 0; guard < 8; guard++) if ((rejected & (1 << guard)) != 0) guardSeconds[i, guard] += dt;
            if (rejected == 0) { validSeconds[i] += dt; longestWindow[i] = Mathf.Max(longestWindow[i], intervals[i] + dt); }
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            var trailer = trailers[i]; trailer.SetBrakes(ai ? aiBrake[i] : ActionHeld(i) ? .65f : 0);
            if (hitches[i].Attached != trailer)
            { if (option == 0) guardSeconds[i, 0] += dt; lostHitch[i] += dt; intervals[i] = quality[i] = 0; pendingTurns[i] = 0; if (lostHitch[i] > 2) retired[i] = true; return; }
            lostHitch[i] = 0;
            float angle = Vector3.Angle(trucks[i].transform.forward, trailer.transform.forward);
            float lateral = Mathf.Abs(Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.right));
            float speed = Vector3.ProjectOnPlane(trailer.Body.linearVelocity, Vector3.up).magnitude;
            bool controlled = angle < 80 && trailer.WheelContact && Vector3.Dot(trailer.transform.up, Vector3.up) > .65f;
            float distance = Vector3.ProjectOnPlane(trailer.Body.position - Gate(i), Vector3.up).magnitude;
            if (option == 0) ObserveDriftGuards(i, angle, speed, lateral, distance, dt);
            if (option == 1)
            {
                if (distance > 14) armed[i] = true;
                if (armed[i] && controlled && distance < 8 && speed >= 1.5f && speed <= 7 && angle < 25)
                { Award(i, 100 + (8 - distance) * 60 + trailer.CargoCondition * 250 - angle * 3, "precision_ice_gate"); gates[i]++; armed[i] = false; if (gates[i] >= 4) retired[i] = true; }
                return;
            }
            if (option == 2)
            {
                if (i >= 2) return;
                int partner = SynchronizationPartner(i);
                if (partner < 0) { syncTime[i] = syncQuality[i] = 0; return; }
                var other = trailers[partner];
                float otherAngle = Vector3.Angle(trucks[partner].transform.forward, other.transform.forward);
                float otherSpeed = Vector3.ProjectOnPlane(other.Body.linearVelocity, Vector3.up).magnitude;
                float signedSlip = Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.right);
                float otherSlip = Vector3.Dot(other.Body.linearVelocity, other.transform.right);
                float ownArticulation = Vector3.SignedAngle(trucks[i].transform.forward, trailer.transform.forward, Vector3.up);
                float otherArticulation = Vector3.SignedAngle(trucks[partner].transform.forward, other.transform.forward, Vector3.up);
                float articulationError = Mathf.Abs(Mathf.DeltaAngle(ownArticulation, otherArticulation));
                bool matched = controlled && !retired[partner] && hitches[i].PhysicalConstraintPresent && hitches[partner].Attached == other && hitches[partner].PhysicalConstraintPresent && other.WheelContact && otherAngle < 80 && Vector3.Dot(other.transform.up, Vector3.up) > .65f && speed > 3 && otherSpeed > 3 && lateral > .8f && Mathf.Abs(otherSlip) > .8f && signedSlip * otherSlip > 0 && articulationError < 12 && Vector3.Angle(trailer.transform.forward, other.transform.forward) < 15 && Mathf.Abs(speed - otherSpeed) < 3 && Vector3.Distance(trailer.Body.position, other.Body.position) < 40 && Mathf.Abs(trailer.Body.angularVelocity.y) < 1.2f && Mathf.Abs(other.Body.angularVelocity.y) < 1.2f;
                if (!matched) { syncTime[i] = syncQuality[i] = 0; return; }
                syncTime[i] += dt; matchedSeconds[i] += dt; bestSyncWindow[i] = Mathf.Max(bestSyncWindow[i], syncTime[i]);
                syncQuality[i] += dt * Mathf.Min(speed, otherSpeed) * (1 - articulationError / 20);
                if (syncTime[i] >= 1)
                { Award(i, syncQuality[i] * 12, "synchronized_physical_drift"); Award(partner, syncQuality[i] * 12, "synchronized_physical_drift"); syncWindows[i]++; syncTime[i] = syncQuality[i] = 0; }
                return;
            }
            if (option == 3)
            {
                float turn = Mathf.DeltaAngle(lastYaw[i], trailer.transform.eulerAngles.y); lastYaw[i] = trailer.transform.eulerAngles.y;
                if (!controlled) { spin[i] = 0; pendingTurns[i] = combos[i] = 0; return; }
                if (speed > 2)
                {
                    if (spin[i] * turn < 0) spin[i] = 0;
                    spin[i] += turn;
                    while (Mathf.Abs(spin[i]) >= 180) { pendingTurns[i]++; spin[i] -= Mathf.Sign(spin[i]) * 180; }
                    if (lateral > 1) scrubDistance[i] += lateral * dt;
                    if (scrubDistance[i] >= 10) { Award(i, scrubDistance[i] * 5, "freestyle_sideways_metres"); scrubDistance[i] = 0; }
                }
                if (pendingTurns[i] > 0 && angle < 30 && Mathf.Abs(trailer.Body.angularVelocity.y) < .35f && speed > 1)
                { combos[i]++; Award(i, pendingTurns[i] * 150 * Mathf.Min(combos[i], 4), "recovered_physical_half_turns"); pendingTurns[i] = 0; }
                return;
            }
            if (!controlled || speed < 3 || lateral < 1 || distance > 35 || Mathf.Abs(trailer.Body.angularVelocity.y) > 1.2f)
            { intervals[i] = quality[i] = 0; return; }
            intervals[i] += dt; gateDrift[i] += dt; quality[i] += lateral * speed * (1 - angle / 100) * dt;
            if (intervals[i] >= 1) { Award(i, quality[i], "controlled_ice_drift"); intervals[i] = quality[i] = 0; }
            if (gateDrift[i] >= 3) { gates[i]++; gateDrift[i] = 0; }
        }
    }
}
