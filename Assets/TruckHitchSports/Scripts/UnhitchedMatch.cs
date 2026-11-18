using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class UnhitchedMatch : HitchCompetitionSession
    {
        public Collider[] deckSurfaces;
        public UnhitchedRecoveryShoulder[] recoveryShoulders = System.Array.Empty<UnhitchedRecoveryShoulder>();
        public float towImpulse = 10, minimumRideSeconds = 3;
        public float recoveryWindowSeconds = 12, groundRemountJumpVelocity = 8, maximumRecoveryDistance = 30;
        readonly bool[] recovering = new bool[4], rampRiderAboard = new bool[4];
        readonly float[] recoveryUntil = new float[4], remountQuiet = new float[4], offDeckGround = new float[4], signedAirYaw = new float[4], supportedDistance = new float[4], rampLandingQuiet = new float[4];
        readonly Vector3[] previousDeckPosition = new Vector3[4];
        readonly int[] falls = new int[4], recoveries = new int[4], recoveryHops = new int[4], shoulderHops = new int[4], rampCombo = new int[4], bestRampCombo = new int[4];
        readonly UnhitchedSurfaceContact[][] physicalSupport = new UnhitchedSurfaceContact[4][];
        readonly float[] riding = new float[4], airtime = new float[4], combo = new float[4];
        readonly float[] rotation = new float[4], lastHeading = new float[4];
        readonly bool[] airborne = new bool[4];
        readonly float[] lastFlightSeconds = new float[4], lastFlightSignedYaw = new float[4], lastFlightUp = new float[4];
        readonly string[] lastFlightEnd = new string[4];
        readonly float[] bank = new float[4];
        readonly float[] landedRotation = new float[4], landedAirtime = new float[4];
        readonly float[] supportedRideSeconds = new float[4];
        readonly float[] lastJump = new float[4], rampAir = new float[4], rampPeak = new float[4];
        readonly Vector3[] rampStart = new Vector3[4];
        readonly bool[] rampFlying = new bool[4];
        const float TrickHopSpeed = 7.6f;
        readonly int[] rampLandings = new int[4], quarterTurns = new int[4], cleanLandings = new int[4], bestCombo = new int[4];
        readonly float[] rampLandingUntil = new float[4], rampDistance = new float[4];
        readonly Vector3[] trailerStart = new Vector3[4];
        readonly NWH.WheelController3D.WheelController[][] platformWheels = new NWH.WheelController3D.WheelController[4][];
        protected override bool StartsCoupled => false;
        public override bool ResultEligible(int slot) => slot >= 0 && slot < setup.count && Scores[slot] > 0 && supportedRideSeconds[slot] >= minimumRideSeconds && (Completed[slot] || Elapsed >= matchSeconds);
        public override string ResultDetail(int slot) => riding[slot].ToString("F1") + "s RIDE · " + cleanLandings[slot] + " CLEAN LANDINGS · " + recoveries[slot] + " RECOVERIES";
        public override string Title => "UNHITCHED!";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = option % 4 == 1 ? (supportedRideSeconds[slot] + landedAirtime[slot]).ToString("F1") + "s" : Scores[slot].ToString("F0");
            if (recovering[slot]) { detail = "REMOUNT · " + Mathf.Max(0, recoveryUntil[slot] - riding[slot]).ToString("F1") + "s · ACTION HOP"; return true; }
            detail = option % 4 == 2 ? rampLandings[slot] + " RAMP LANDINGS" : option % 4 == 3 ? quarterTurns[slot] + " QUARTER TURNS" : cleanLandings[slot] + " CLEAN LANDINGS";
            return true;
        }
        public override string OptionLabel => new[] { "TRAILER FREESTYLE", "LONGEST RIDE", "RAMP ATTACK", "TRAILER TRICK CHALLENGE" }[option % 4];
        public override string Help => "Drive on a rolling physical flatbed; your truck's weight and wheel forces affect it. Movement controls steer/balance, ACTION hops when your actual wheels contact the deck, with a short recovery between hops. Freestyle banks clean airborne landings, rotations, balance and ride distance. Freestyle: on the flat deck beside a recovery lane, ACTION + strong steering toward that lane performs a physical sideways hop. Release steering in the air to avoid spinning. Without that modifier, ACTION is the ordinary vertical hop. Longest Ride scores only supported riding time plus airborne time that ends back on the deck. Ramp Attack scores the trailer's genuine ramp takeoffs and landings while you remain aboard, not ordinary truck hops. Trick Challenge banks landed quarter-turns of at least 90 degrees, with clean-landing combos. Longest Ride ends on a fall. Other modes give a brief physical recovery window. The lower side recovery lane is a real supported route: match your original deck's forward speed, steer inward and ACTION hop from grounded wheels to remount. Braking on the moving deck holds the tyres against it; it does not reliably steer you sideways off it. A fall breaks the combo and banks no imaginary airtime; only a real stable wheel landing restores the ride. The trailer gets one starting impulse; slopes, collisions and your weight determine the rest. No invisible joint attaches the truck to the deck.";
        protected override void BeginCompetition()
        {
            for (int i = 0; i < 4; i++)
            {
                recovering[i] = rampRiderAboard[i] = false; recoveryUntil[i] = remountQuiet[i] = offDeckGround[i] = signedAirYaw[i] = supportedDistance[i] = rampLandingQuiet[i] = 0; falls[i] = recoveries[i] = recoveryHops[i] = shoulderHops[i] = rampCombo[i] = bestRampCombo[i] = 0;
                previousDeckPosition[i] = trailers[i].Body.position;
                lastFlightSeconds[i] = lastFlightSignedYaw[i] = lastFlightUp[i] = 0; lastFlightEnd[i] = "none";
                var contacts = new System.Collections.Generic.List<UnhitchedSurfaceContact>();
                foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true))
                {
                    var contact = part.GetComponent<UnhitchedSurfaceContact>();
                    if (contact == null) contact = part.gameObject.AddComponent<UnhitchedSurfaceContact>();
                    contact.Configure(trucks[i].transform); contacts.Add(contact);
                }
                physicalSupport[i] = contacts.ToArray();
                riding[i] = airtime[i] = rotation[i] = combo[i] = bank[i] = landedRotation[i] = landedAirtime[i] = supportedRideSeconds[i] = rampAir[i] = rampPeak[i] = rampLandingUntil[i] = rampDistance[i] = 0; lastJump[i] = -10; rampLandings[i] = quarterTurns[i] = cleanLandings[i] = bestCombo[i] = 0; lastHeading[i] = trucks[i].transform.eulerAngles.y; airborne[i] = rampFlying[i] = false; trailerStart[i] = trailers[i].Body.position;
                // Detached surfing is a moving-platform state. Native detach
                // normally lowers the parking stand; it must be stowed here,
                // while all native axles, suspension and body mass remain live.
                var module = trailers[i].Module;
                if (module != null && module.module.trailerStand != null) module.module.trailerStand.SetActive(false);
                trailers[i].SetBrakes(0);
                platformWheels[i] = trailers[i].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            }
        }
        public string PlatformDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            var trailer = trailers[slot]; var body = trailer.Body; var vehicle = trailer.Vehicle;
            var stand = trailer.Module != null ? trailer.Module.module.trailerStand : null;
            int grounded = 0, count = 0, frontGrounded = 0, frontCount = 0; float brakeTorque = 0, rollingTorque = 0;
            if (platformWheels[slot] != null) foreach (var wheel in platformWheels[slot]) if (wheel != null)
            {
                count++; if (wheel.IsGrounded) grounded++;
                if (wheel.name.StartsWith("Surf front rolling support", System.StringComparison.Ordinal)) { frontCount++; if (wheel.IsGrounded) frontGrounded++; }
                brakeTorque = Mathf.Max(brakeTorque, wheel.BrakeTorque); rollingTorque += wheel.rollingResistanceTorque;
            }
            return "platformPos=" + body.position.ToString("F2") + " COM=" + body.worldCenterOfMass.ToString("F2") + " localCOM=" + body.centerOfMass.ToString("F2") + " mass=" + body.mass.ToString("F0") + " velocity=" + body.linearVelocity.ToString("F2") + " up=" + Vector3.Dot(trailer.transform.up, Vector3.up).ToString("F3") + " pitch=" + body.rotation.eulerAngles.x.ToString("F1") + " travel=" + Vector3.ProjectOnPlane(body.position - trailerStart[slot], Vector3.up).magnitude.ToString("F2") + " standLowered=" + (stand != null && stand.activeInHierarchy) + " trailerVC=" + (vehicle != null && vehicle.enabled) + " initialized=" + (vehicle != null && vehicle.IsInitialized) + " brakeInput=" + (vehicle != null ? vehicle.input.states.brakes.ToString("F2") : "n/a") + " handbrakeInput=" + (vehicle != null ? vehicle.input.states.handbrake.ToString("F2") : "n/a") + " groundedWheels=" + grounded + "/" + count + " frontSupportGrounded=" + frontGrounded + "/" + frontCount + " maxWheelBrakeNm=" + brakeTorque.ToString("F1") + " rollingTorqueNm=" + rollingTorque.ToString("F1") + " supportedRide=" + supportedRideSeconds[slot].ToString("F1");
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            RecordMetric(slot, "unhitched_ride_seconds", supportedRideSeconds[slot]);
            RecordMetric(slot, "unhitched_attempt_seconds", riding[slot]);
            RecordMetric(slot, "unhitched_clean_landings", cleanLandings[slot]);
            RecordMetric(slot, "unhitched_best_clean_combo", bestCombo[slot]);
            RecordMetric(slot, "unhitched_landed_trick_rotation_degrees", landedRotation[slot]);
            RecordMetric(slot, "unhitched_landed_trick_airtime_seconds", landedAirtime[slot]);
            RecordMetric(slot, "unhitched_ramp_landings", rampLandings[slot]);
            RecordMetric(slot, "unhitched_landed_quarter_turns", quarterTurns[slot]);
            RecordMetric(slot, "unhitched_actual_falls", falls[slot]);
            RecordMetric(slot, "unhitched_physical_remounts", recoveries[slot]);
            RecordMetric(slot, "unhitched_ground_recovery_hops", recoveryHops[slot]);
            RecordMetric(slot, "unhitched_directional_shoulder_hops", shoulderHops[slot]);
            RecordMetric(slot, "unhitched_supported_platform_metres", supportedDistance[slot]);
            RecordMetric(slot, "unhitched_best_ramp_combo", bestRampCombo[slot]);
        }
        protected override void TickCompetition(float dt)
        {
            Notice = option % 4 == 0 ? "ACTION HOP · ACTION + STRONG STEER TOWARD SIDE LANE: SIDE HOP" : "BALANCE ON THE REAL DECK · ACTION HOP · LAND CLEANLY TO BANK TRICKS";
            for (int i = 0; i < setup.count; i++)
            {
                if (Completed[i]) continue; var body = trucks[i].body; var deck = trailers[i].Body;
                if (riding[i] == 0)
                {
                    // The launch starts a loaded moving platform. Give both
                    // actual assemblies the same one-time physical impulse;
                    // kicking only the deck instantly strips it from the rider.
                    Vector3 launch = trailers[i].transform.forward * towImpulse;
                    deck.AddForce(launch, ForceMode.VelocityChange);
                    foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true)) if (!part.isKinematic) part.AddForce(launch, ForceMode.VelocityChange);
                }
                riding[i] += dt;
                Vector3 deckCenter = deckSurfaces != null && i < deckSurfaces.Length && deckSurfaces[i] != null ? trailers[i].transform.InverseTransformPoint(deckSurfaces[i].bounds.center) : Vector3.zero;
                Vector3 relative = trailers[i].transform.InverseTransformPoint(body.position) - deckCenter;
                Vector3 relativeVelocity = trailers[i].transform.InverseTransformDirection(body.linearVelocity - deck.GetPointVelocity(body.worldCenterOfMass));
                float headingError = Vector3.SignedAngle(trailers[i].transform.forward, trucks[i].transform.forward, Vector3.up) * Mathf.Deg2Rad;
                Vector2 drive = IsAI(i) ? new Vector2(Mathf.Clamp(-relative.x * .3f - relativeVelocity.x * .35f - headingError * .8f, -.6f, .6f), Mathf.Clamp(-relative.z * .15f - relativeVelocity.z * .5f, -.65f, .65f)) : commands[i];
                if (IsAI(i) && recovering[i] && !TryDeckSupport(i, out _))
                    drive = trucks[i].Grounded || HasPhysicalSupport(i)
                        ? DriveTo(i, RecoveryAimPoint(i), Mathf.Min(16, deck.linearVelocity.magnitude + 4))
                        : Vector2.zero;
                else if (IsAI(i) && option % 4 == 3 && airborne[i] && !recovering[i])
                {
                    // Use the same physical torque limit as human air steering.
                    // Aim past a genuine quarter turn, then brake the measured
                    // yaw rate instead of spinning until the truck leaves the deck.
                    float remainingYaw = (120 - signedAirYaw[i]) * Mathf.Deg2Rad;
                    drive.x = Mathf.Clamp(remainingYaw * 2.2f - Vector3.Dot(body.angularVelocity, Vector3.up) * .7f, -.8f, .8f);
                }
                trucks[i].TickDrive(drive);
                bool supported = TryDeckSupport(i, out var hit) && trucks[i].Grounded;
                bool physicallySupported = trucks[i].Grounded || HasPhysicalSupport(i);
                offDeckGround[i] = !supported && physicallySupported ? offDeckGround[i] + dt : 0;
                if (supported) supportedDistance[i] += Vector3.ProjectOnPlane(deck.position - previousDeckPosition[i], Vector3.up).magnitude;
                previousDeckPosition[i] = deck.position;
                bool jump = IsAI(i) ? recovering[i] ? RecoveryHopPrediction(i) : option % 4 == 3 ? FlatTrickTakeoff(i, supported) : option % 4 != 2 && !airborne[i] && riding[i] > 4 && ((int)riding[i] % 8 == 0) : actions[i];
                if (jump && recovering[i] && !supported && trucks[i].Grounded && Vector3.ProjectOnPlane(body.position - deck.position, Vector3.up).magnitude < maximumRecoveryDistance && riding[i] - lastJump[i] >= 1.25f)
                {
                    // A genuine ground takeoff. Only vertical impulses are
                    // applied; existing velocity/steering must carry the truck
                    // back onto the independently moving physical platform.
                    foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true)) if (!part.isKinematic) part.AddForce(Vector3.up * groundRemountJumpVelocity, ForceMode.VelocityChange);
                    lastJump[i] = riding[i]; airborne[i] = true; recoveryHops[i]++;
                }
                else if (jump && supported && !airborne[i] && riding[i] - lastJump[i] >= 1.25f)
                {
                    if (option % 4 == 3)
                    {
                        // A trick takeoff launches the native jointed assembly,
                        // just like its existing platform launch/recovery hop.
                        // Leaving the separate 1500kg cab behind drags/pitches the
                        // chassis through its joint and consumes the intended hop.
                        float launchedMass = 0;
                        foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true)) if (!part.isKinematic)
                        { part.AddForce(Vector3.up * TrickHopSpeed, ForceMode.VelocityChange); launchedMass += part.mass; }
                        deck.AddForceAtPosition(Vector3.down * launchedMass * TrickHopSpeed, hit.point, ForceMode.Impulse);
                    }
                    else if (TryShoulderHopDelta(i, drive.x, out var shoulderDelta))
                    {
                        float launchedMass = 0;
                        foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true)) if (!part.isKinematic)
                        { part.AddForce(shoulderDelta, ForceMode.VelocityChange); launchedMass += part.mass; }
                        deck.AddForceAtPosition(-shoulderDelta * launchedMass, hit.point, ForceMode.Impulse);
                        shoulderHops[i]++;
                    }
                    else { body.AddForce(Vector3.up * 6, ForceMode.VelocityChange); deck.AddForceAtPosition(Vector3.down * body.mass * 6, hit.point, ForceMode.Impulse); }
                    airborne[i] = true; supported = false; lastJump[i] = riding[i];
                }
                float signedTurn = Mathf.DeltaAngle(lastHeading[i], body.rotation.eulerAngles.y); float turn = Mathf.Abs(signedTurn); lastHeading[i] = body.rotation.eulerAngles.y;
                if (!supported)
                {
                    if (!physicallySupported)
                    {
                        airborne[i] = true; airtime[i] += dt; rotation[i] += turn; signedAirYaw[i] += signedTurn;
                        body.AddRelativeTorque(new Vector3(drive.y * .7f, drive.x * 4, -drive.x * .3f), ForceMode.Acceleration);
                    }
                    else
                    {
                        if (airtime[i] > .05f) RememberFlightEnd(i, "outside wheel/hull support");
                        if (airtime[i] > .3f) combo[i] = 0;
                        airtime[i] = rotation[i] = signedAirYaw[i] = 0; airborne[i] = false;
                    }
                }
                else
                {
                    supportedRideSeconds[i] += dt;
                    if (airtime[i] > .05f) RememberFlightEnd(i, "own deck wheel support");
                    if (airtime[i] > .3f && Vector3.Dot(body.transform.up, Vector3.up) > .65f)
                    {
                        int turns = Mathf.FloorToInt(Mathf.Abs(signedAirYaw[i]) / 90);
                        if (option % 4 != 3 || turns > 0) combo[i]++; else combo[i] = 0;
                        cleanLandings[i]++; bestCombo[i] = Mathf.Max(bestCombo[i], Mathf.RoundToInt(combo[i])); landedRotation[i] += rotation[i]; landedAirtime[i] += airtime[i];
                        quarterTurns[i] += turns;
                        if (option % 4 == 0) AddScore(i, (airtime[i] * 40 + rotation[i] * .5f) * Mathf.Min(combo[i], 5), "deck-trick-landed");
                        else if (option % 4 == 1) AddScore(i, airtime[i] * 1000, "supported-ride-airtime-banked");
                        else if (option % 4 == 3 && turns > 0) AddScore(i, turns * 150 * Mathf.Min(combo[i], 5), "quarter-turn-trick-landed");
                    }
                    else if (airtime[i] > .3f) combo[i] = 0;
                    airtime[i] = rotation[i] = signedAirYaw[i] = 0; airborne[i] = false;
                    bank[i] += dt;
                    if (bank[i] >= 1) BankSupportedRide(i);
                }
                if (recovering[i])
                {
                    remountQuiet[i] = supported && Vector3.Dot(body.rotation * Vector3.up, Vector3.up) > .65f && (body.linearVelocity - deck.GetPointVelocity(body.worldCenterOfMass)).magnitude < 3 ? remountQuiet[i] + dt : 0;
                    if (remountQuiet[i] >= .25f) { recovering[i] = false; recoveries[i]++; offDeckGround[i] = 0; }
                    else if (riding[i] >= recoveryUntil[i] || Vector3.ProjectOnPlane(body.position - deck.position, Vector3.up).magnitude > maximumRecoveryDistance || body.position.y < -20) FinishRide(i);
                }
                else if (riding[i] > minimumRideSeconds && (offDeckGround[i] > .35f || body.position.y < deck.position.y - 2 || Mathf.Abs(relative.x) > 10 || Mathf.Abs(relative.z) > 22))
                {
                    falls[i]++; combo[i] = 0;
                    if (option % 4 == 1 || body.position.y < -20) FinishRide(i);
                    else
                    {
                        recovering[i] = true; recoveryUntil[i] = riding[i] + recoveryWindowSeconds; remountQuiet[i] = 0;
                        if (airtime[i] > .05f) RememberFlightEnd(i, "outside deck envelope before landing");
                        airtime[i] = rotation[i] = signedAirYaw[i] = 0; rampRiderAboard[i] = false; rampLandingUntil[i] = 0; rampCombo[i] = 0;
                        BankSupportedRide(i);
                    }
                }
                bool touchedOtherSupport = false;
                if (!supported)
                    foreach (var wheel in trucks[i].wheels)
                        if (wheel != null && wheel.IsGrounded && wheel.HitCollider != null &&
                            wheel.HitCollider.attachedRigidbody != deck)
                            touchedOtherSupport = true;
                if (physicalSupport[i] != null) foreach (var contact in physicalSupport[i]) touchedOtherSupport |= contact != null && contact.SupportedOutside(deck);
                if (touchedOtherSupport) rampRiderAboard[i] = false;
                ObserveRamp(i, dt, supported);
                if (Elapsed >= matchSeconds) BankSupportedRide(i);
            }
        }
        void RememberFlightEnd(int slot, string reason)
        {
            lastFlightSeconds[slot] = airtime[slot]; lastFlightSignedYaw[slot] = signedAirYaw[slot];
            lastFlightUp[slot] = Vector3.Dot(trucks[slot].body.rotation * Vector3.up, Vector3.up); lastFlightEnd[slot] = reason;
        }
        public string TrickDiagnostic(int slot) => "currentAir=" + airtime[slot].ToString("F2") + " yaw=" + signedAirYaw[slot].ToString("F1") + " bodyOmega=" + trucks[slot].body.angularVelocity.ToString("F2") + " lastFlight=" + lastFlightSeconds[slot].ToString("F2") + "s/" + lastFlightSignedYaw[slot].ToString("F1") + "deg up=" + lastFlightUp[slot].ToString("F2") + " end=" + lastFlightEnd[slot] + " hopCount=" + recoveryHops[slot] + " landedTurns=" + quarterTurns[slot];
        bool HasPhysicalSupport(int slot)
        {
            if (physicalSupport[slot] != null) foreach (var contact in physicalSupport[slot]) if (contact != null && contact.Supported) return true;
            return false;
        }
        bool FlatTrickTakeoff(int slot, bool supported)
        {
            var deck = trailers[slot].Body;
            // Run83 showed a genuine flat stretch at ride1–3s/Z23–43,
            // followed by rising/tilting support and only0.96s of late flight.
            // Select the earlier actual supported stretch; the launch impulse,
            // air torque and90-degree landing requirement are unchanged.
            if (!supported || airborne[slot] || supportedRideSeconds[slot] < 1.5f || riding[slot] < 2 || riding[slot] - lastJump[slot] < 6) return false;
            if (Vector3.Dot(deck.rotation * Vector3.up, Vector3.up) < .985f || Mathf.Abs(deck.linearVelocity.y) > .3f || deck.angularVelocity.magnitude > .25f) return false;
            float projectedLandingZ = deck.position.z + Mathf.Max(0, deck.linearVelocity.z) * (2 * TrickHopSpeed / Mathf.Max(.1f, Physics.gravity.magnitude));
            return projectedLandingZ < 52; // The original physical first ramp begins nearZ53.
        }
        bool RecoveryTopPoint(int slot, out Vector3 point, out Vector3 insetHalf)
        {
            point = Vector3.zero; insetHalf = Vector3.zero;
            if (!(deckSurfaces[slot] is BoxCollider box)) return false;
            var truck = trucks[slot];
            Vector3 half = box.size * .5f, rider = Vector3.zero;
            if (truck.shape != null)
            {
                // Project the original native tractor bounds into this exact
                // oriented deck frame, rather than using its world AABB height.
                Matrix4x4 frame = box.transform.worldToLocalMatrix * truck.shape.transform.localToWorldMatrix;
                Vector3 size = truck.shape.size * .5f;
                rider.x = Mathf.Abs(frame.m00) * size.x + Mathf.Abs(frame.m01) * size.y + Mathf.Abs(frame.m02) * size.z;
                rider.z = Mathf.Abs(frame.m20) * size.x + Mathf.Abs(frame.m21) * size.y + Mathf.Abs(frame.m22) * size.z;
            }
            insetHalf = new Vector3(half.x - rider.x, half.y, half.z - rider.z);
            if (insetHalf.x <= 0 || insetHalf.z <= 0) return false;
            Vector3 local = box.transform.InverseTransformPoint(truck.body.worldCenterOfMass) - box.center;
            point = box.transform.TransformPoint(box.center + new Vector3(Mathf.Clamp(local.x, -insetHalf.x, insetHalf.x), half.y, Mathf.Clamp(local.z, -insetHalf.z, insetHalf.z)));
            return true;
        }
        public Vector3 RecoveryAimPoint(int slot)
        {
            var surface = deckSurfaces[slot];
            Vector3 point = RecoveryTopPoint(slot, out var top, out _) ? top : surface.bounds.center;
            return point + trailers[slot].Body.GetPointVelocity(point) * .7f;
        }
        public bool TryRecoveryLaunchVelocity(int slot, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            if (slot < 0 || slot >= setup.count || !recovering[slot] || !RecoveryTopPoint(slot, out var top, out _)) return false;
            var truck = trucks[slot]; var deck = trailers[slot].Body;
            float gravity = Mathf.Max(.1f, Physics.gravity.magnitude), height = top.y - truck.body.position.y;
            float discriminant = groundRemountJumpVelocity * groundRemountJumpVelocity - 2 * gravity * height;
            if (discriminant <= 0) return false;
            float flight = (groundRemountJumpVelocity + Mathf.Sqrt(discriminant)) / gravity;
            Vector3 target = top + deck.GetPointVelocity(top) * flight;
            // This is an observation for ordinary steering/throttle decisions,
            // never an assigned body velocity or automatic remount outcome.
            velocity = Vector3.ProjectOnPlane(target - truck.body.worldCenterOfMass, Vector3.up) / Mathf.Max(.1f, flight);
            return true;
        }
        public bool RecoveryHopPrediction(int slot)
        {
            if (slot < 0 || slot >= setup.count || !recovering[slot] || !trucks[slot].Grounded || TryDeckSupport(slot, out _)) return false;
            var truck = trucks[slot]; var deck = trailers[slot].Body; var surface = deckSurfaces[slot];
            if (!RecoveryTopPoint(slot, out var top, out var insetHalf)) return false;
            float gravity = Mathf.Max(.1f, Physics.gravity.magnitude);
            float height = top.y - truck.body.position.y;
            float discriminant = groundRemountJumpVelocity * groundRemountJumpVelocity - 2 * gravity * height;
            if (discriminant <= 0) return false;
            float flight = (groundRemountJumpVelocity + Mathf.Sqrt(discriminant)) / gravity;
            Vector3 target = top + deck.GetPointVelocity(top) * flight;
            Vector3 predicted = truck.body.worldCenterOfMass + truck.body.linearVelocity * flight;
            Vector3 local = surface.transform.InverseTransformVector(Vector3.ProjectOnPlane(predicted - target, Vector3.up));
            // Prediction chooses a real input time. Only actual native wheel
            // contact with the top deck can certify the eventual remount.
            return Mathf.Abs(local.x) < insetHalf.x && Mathf.Abs(local.z) < insetHalf.z && Vector3.Dot(truck.transform.up, Vector3.up) > .8f;
        }
        public string RecoveryDiagnostic(int slot) => "recovering=" + recovering[slot] + " remaining=" + Mathf.Max(0, recoveryUntil[slot] - riding[slot]).ToString("F2") + " falls=" + falls[slot] + " actualRemounts=" + recoveries[slot] + " wheelsGrounded=" + trucks[slot].Grounded + " bodySupported=" + HasPhysicalSupport(slot) + " hopPrediction=" + RecoveryHopPrediction(slot) + " body=" + trucks[slot].body.position.ToString("F2") + " velocity=" + trucks[slot].body.linearVelocity.ToString("F2") + " up=" + Vector3.Dot(trucks[slot].body.rotation * Vector3.up,Vector3.up).ToString("F2") + " deckTop=" + deckSurfaces[slot].bounds.max.y.ToString("F2") + " recoveryAim=" + RecoveryAimPoint(slot).ToString("F2") + " nativeSupport=[" + WheelSupportDiagnostic(slot) + "]" + " maxGroundRise=" + (groundRemountJumpVelocity * groundRemountJumpVelocity / (2 * Mathf.Max(.1f,Physics.gravity.magnitude))).ToString("F2");
        void FinishRide(int slot)
        {
            if (Completed[slot]) return;
            if (option % 4 == 0 && supportedRideSeconds[slot] >= minimumRideSeconds) AddScore(slot, supportedDistance[slot] * 2, "ride-distance");
            BankSupportedRide(slot); combo[slot] = 0; recovering[slot] = false; Completed[slot] = true;
            trucks[slot].HoldForInteraction();
        }
        bool TryDeckSupport(int slot, out RaycastHit support)
        {
            support = default;
            var surface = deckSurfaces != null && slot < deckSurfaces.Length ? deckSurfaces[slot] : null;
            if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy) return false;
            // The COM support ray alone can see a trailer axle/chassis while
            // native tyres actually rest on the arena underneath. Require both
            // the designated load-bearing top face and its real wheel identity.
            if (!surface.Raycast(new Ray(trucks[slot].body.worldCenterOfMass + Vector3.up, Vector3.down), out support, 4)
                || Vector3.Dot(support.normal, surface.transform.up) < .8f) return false;
            foreach (var wheel in trucks[slot].wheels)
                if (wheel != null && wheel.enabled && wheel.IsGrounded && wheel.HitCollider == surface && Vector3.Dot(wheel.HitNormal, surface.transform.up) > .8f) return true;
            return false;
        }
        public bool OwnDeckWheelSupport(int slot) => TryDeckSupport(slot, out _);
        public int ShoulderHopCount(int slot) => shoulderHops[slot];
        bool TryShoulderHopDelta(int slot, float steer, out Vector3 delta)
        {
            delta = Vector3.zero;
            if (option % 4 != 0 || IsAI(slot) || Mathf.Abs(steer) < .65f || recoveryShoulders == null || slot >= recoveryShoulders.Length) return false;
            var lane = recoveryShoulders[slot]; var truck = trucks[slot]; var deck = trailers[slot].Body;
            if (lane == null || !lane.isActiveAndEnabled || lane.DrivingSurfaces == null || Vector3.Dot(deck.transform.up, Vector3.up) < .97f || Vector3.Dot(truck.transform.up, Vector3.up) < .8f) return false;
            Vector3 destination = lane.PointAtForwardPosition(truck.body.position.z + Mathf.Max(0, truck.body.linearVelocity.z) * 1.35f, true);
            bool hasRoad = false;
            foreach (var surface in lane.DrivingSurfaces)
                if (surface != null && surface.enabled && surface.gameObject.activeInHierarchy && !surface.isTrigger && surface.Raycast(new Ray(destination + Vector3.up * 6, Vector3.down), out var roadHit, 12) && Vector3.Dot(roadHit.normal, Vector3.up) > .8f)
                { destination = roadHit.point; hasRoad = true; break; }
            if (!hasRoad) return false;
            Vector3 side = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(destination - truck.body.worldCenterOfMass, deck.transform.forward), Vector3.up);
            if (side.magnitude < 2 || side.magnitude > 8 || Vector3.Dot(truck.transform.right * Mathf.Sign(steer), side.normalized) < .75f) return false;
            // A real nearby lower road is required, not an arbitrary midair
            // side boost. The original true deck support gate remains outside.
            float drop = deckSurfaces[slot].bounds.max.y - destination.y;
            if (drop < 0 || drop > 2.5f) return false;
            side.Normalize();
            float relativeSideSpeed = Vector3.Dot(truck.body.linearVelocity - deck.GetPointVelocity(truck.body.worldCenterOfMass), side);
            delta = Vector3.up * 6 + side * Mathf.Clamp(4 - relativeSideSpeed, -4, 4);
            return true;
        }
        string WheelSupportDiagnostic(int slot)
        {
            var text = new System.Text.StringBuilder();
            foreach (var wheel in trucks[slot].wheels) if (wheel != null && wheel.IsGrounded)
                text.Append(wheel.name).Append("->").Append(wheel.HitCollider != null ? wheel.HitCollider.name : "none").Append('@').Append(wheel.HitPoint.ToString("F1")).Append(" n=").Append(wheel.HitNormal.ToString("F1")).Append(';');
            return text.ToString();
        }
        void BankSupportedRide(int slot)
        {
            if (option % 4 == 0 || option % 4 == 1) AddScore(slot, bank[slot] * (option % 4 == 1 ? 1000 : 2), "balanced-ride");
            bank[slot] = 0;
        }
        void ObserveRamp(int slot, float dt, bool riderSupported)
        {
            var trailer = trailers[slot]; bool grounded = trailer.WheelContact;
            if (rampLandingUntil[slot] > 0)
            {
                bool controlled = rampRiderAboard[slot] && !recovering[slot] && riderSupported && Vector3.Dot(trucks[slot].transform.up, Vector3.up) > .65f && Vector3.Dot(trailer.transform.up, Vector3.up) > .65f && Mathf.Abs((trucks[slot].body.linearVelocity - trailer.Body.GetPointVelocity(trucks[slot].body.worldCenterOfMass)).y) < 2 && trucks[slot].body.angularVelocity.sqrMagnitude < 4;
                rampLandingQuiet[slot] = controlled ? rampLandingQuiet[slot] + dt : 0;
                if (rampLandingQuiet[slot] >= .2f)
                {
                    rampLandings[slot]++; rampCombo[slot]++; bestRampCombo[slot] = Mathf.Max(bestRampCombo[slot], rampCombo[slot]);
                    if (option % 4 == 2) AddScore(slot, (100 + rampPeak[slot] * 70 + rampDistance[slot] * 8) * Mathf.Min(rampCombo[slot], 5), "physical-ramp-landing");
                    rampLandingUntil[slot] = 0;
                }
                else if (riding[slot] > rampLandingUntil[slot]) { rampLandingUntil[slot] = 0; rampCombo[slot] = 0; }
            }
            if (!rampFlying[slot] && !grounded && trailer.Body.linearVelocity.y > 1.2f && riding[slot] > 3)
            { rampFlying[slot] = true; rampRiderAboard[slot] = riderSupported && !recovering[slot]; rampAir[slot] = rampPeak[slot] = rampLandingQuiet[slot] = 0; rampStart[slot] = trailer.Body.position; }
            if (!rampFlying[slot]) return;
            if (recovering[slot]) rampRiderAboard[slot] = false;
            rampAir[slot] += dt; rampPeak[slot] = Mathf.Max(rampPeak[slot], trailer.Body.position.y - rampStart[slot].y);
            if (!grounded) return;
            if (rampAir[slot] >= .25f)
            { rampDistance[slot] = Vector3.ProjectOnPlane(trailer.Body.position - rampStart[slot], Vector3.up).magnitude; rampLandingUntil[slot] = riding[slot] + 1.5f; }
            rampFlying[slot] = false;
        }
    }
}
