using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class UnhitchedMatch : HitchCompetitionSession
    {
        public Collider[] deckSurfaces;
        public float towImpulse = 10, minimumRideSeconds = 3;
        readonly float[] riding = new float[4], airtime = new float[4], combo = new float[4];
        readonly float[] rotation = new float[4], lastHeading = new float[4];
        readonly bool[] airborne = new bool[4];
        readonly float[] bank = new float[4];
        readonly float[] landedRotation = new float[4], landedAirtime = new float[4];
        readonly float[] supportedRideSeconds = new float[4];
        readonly float[] lastJump = new float[4], rampAir = new float[4], rampPeak = new float[4];
        readonly Vector3[] rampStart = new Vector3[4];
        readonly bool[] rampFlying = new bool[4];
        readonly int[] rampLandings = new int[4], quarterTurns = new int[4], cleanLandings = new int[4], bestCombo = new int[4];
        readonly float[] rampLandingUntil = new float[4], rampDistance = new float[4];
        readonly Vector3[] trailerStart = new Vector3[4];
        readonly RaycastHit[] deckHits = new RaycastHit[24];
        readonly NWH.WheelController3D.WheelController[][] platformWheels = new NWH.WheelController3D.WheelController[4][];
        protected override bool StartsCoupled => false;
        public override bool ResultEligible(int slot) => slot >= 0 && slot < setup.count && Scores[slot] > 0 && supportedRideSeconds[slot] >= minimumRideSeconds && (Completed[slot] || Elapsed >= matchSeconds);
        public override string ResultDetail(int slot) => riding[slot].ToString("F1") + "s RIDE · " + cleanLandings[slot] + " CLEAN LANDINGS";
        public override string Title => "UNHITCHED!";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = option % 4 == 1 ? (supportedRideSeconds[slot] + landedAirtime[slot]).ToString("F1") + "s" : Scores[slot].ToString("F0");
            detail = option % 4 == 2 ? rampLandings[slot] + " RAMP LANDINGS" : option % 4 == 3 ? quarterTurns[slot] + " QUARTER TURNS" : cleanLandings[slot] + " CLEAN LANDINGS";
            return true;
        }
        public override string OptionLabel => new[] { "TRAILER FREESTYLE", "LONGEST RIDE", "RAMP ATTACK", "TRAILER TRICK CHALLENGE" }[option % 4];
        public override string Help => "Drive on a rolling physical flatbed; your truck's weight and wheel forces affect it. Movement controls steer/balance, ACTION hops when your actual wheels contact the deck, with a short recovery between hops. Freestyle banks clean airborne landings, rotations, balance and ride distance. Longest Ride scores only supported riding time plus airborne time that ends back on the deck. Ramp Attack scores the trailer's genuine ramp takeoffs and landings while you remain aboard, not ordinary truck hops. Trick Challenge banks landed quarter-turns of at least 90 degrees, with clean-landing combos. Falling off ends your ride. The trailer gets one starting impulse; slopes, collisions and your weight determine the rest. No invisible joint attaches the truck to the deck.";
        protected override void BeginCompetition()
        {
            for (int i = 0; i < 4; i++)
            {
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
        }
        protected override void TickCompetition(float dt)
        {
            Notice = "BALANCE ON THE REAL DECK · ACTION HOP · LAND CLEANLY TO BANK TRICKS";
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
                if (IsAI(i) && option % 4 == 3 && airborne[i]) drive.x = .8f;
                trucks[i].TickDrive(drive);
                bool supported = TryDeckSupport(i, out var hit) && trucks[i].Grounded;
                bool jump = IsAI(i) ? option % 4 != 2 && !airborne[i] && riding[i] > 4 && ((int)riding[i] % 8 == 0) : actions[i];
                if (jump && supported && !airborne[i] && riding[i] - lastJump[i] >= 1.25f)
                { body.AddForce(Vector3.up * 6, ForceMode.VelocityChange); deck.AddForceAtPosition(Vector3.down * body.mass * 6, hit.point, ForceMode.Impulse); airborne[i] = true; supported = false; lastJump[i] = riding[i]; }
                float turn = Mathf.Abs(Mathf.DeltaAngle(lastHeading[i], body.rotation.eulerAngles.y)); lastHeading[i] = body.rotation.eulerAngles.y;
                if (!supported)
                { airtime[i] += dt; rotation[i] += turn; body.AddRelativeTorque(new Vector3(drive.y * .7f, drive.x * 4, -drive.x * .3f), ForceMode.Acceleration); }
                else
                {
                    supportedRideSeconds[i] += dt;
                    if (airtime[i] > .3f && Vector3.Dot(body.transform.up, Vector3.up) > .65f)
                    {
                        combo[i]++; cleanLandings[i]++; bestCombo[i] = Mathf.Max(bestCombo[i], Mathf.RoundToInt(combo[i])); landedRotation[i] += rotation[i]; landedAirtime[i] += airtime[i];
                        int turns = Mathf.FloorToInt(rotation[i] / 90); quarterTurns[i] += turns;
                        if (option % 4 == 0) AddScore(i, (airtime[i] * 40 + rotation[i] * .5f) * Mathf.Min(combo[i], 5), "deck-trick-landed");
                        else if (option % 4 == 1) AddScore(i, airtime[i] * 1000, "supported-ride-airtime-banked");
                        else if (option % 4 == 3 && turns > 0) AddScore(i, turns * 150 * Mathf.Min(combo[i], 5), "quarter-turn-trick-landed");
                    }
                    else if (airtime[i] > .3f) combo[i] = 0;
                    airtime[i] = rotation[i] = 0; airborne[i] = false;
                    bank[i] += dt;
                    if (bank[i] >= 1) BankSupportedRide(i);
                }
                ObserveRamp(i, dt, supported);
                if (riding[i] > minimumRideSeconds && (body.position.y < deck.position.y - 2 || Mathf.Abs(relative.x) > 10 || Mathf.Abs(relative.z) > 22))
                { if (option % 4 == 0 && supportedRideSeconds[i] >= minimumRideSeconds) AddScore(i, Vector3.ProjectOnPlane(deck.position - trailerStart[i], Vector3.up).magnitude * 2, "ride-distance"); BankSupportedRide(i); combo[i] = 0; Completed[i] = true; }
                else if (Elapsed >= matchSeconds) BankSupportedRide(i);
            }
        }
        bool TryDeckSupport(int slot, out RaycastHit support)
        {
            support = default; float nearest = float.PositiveInfinity;
            int count = Physics.RaycastNonAlloc(trucks[slot].body.worldCenterOfMass + Vector3.up, Vector3.down, deckHits, 4, ~0, QueryTriggerInteraction.Ignore);
            if (count == deckHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = deckHits[i];
                if (hit.collider.transform.IsChildOf(trucks[slot].transform) || hit.distance >= nearest) continue;
                nearest = hit.distance; support = hit;
            }
            return nearest < float.PositiveInfinity && support.rigidbody == trailers[slot].Body;
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
                if (riderSupported && Vector3.Dot(trucks[slot].transform.up, Vector3.up) > .65f)
                {
                    rampLandings[slot]++;
                    if (option % 4 == 2) AddScore(slot, 100 + rampPeak[slot] * 70 + rampDistance[slot] * 8, "physical-ramp-landing");
                    rampLandingUntil[slot] = 0;
                }
                else if (riding[slot] > rampLandingUntil[slot]) rampLandingUntil[slot] = 0;
            }
            if (!rampFlying[slot] && !grounded && trailer.Body.linearVelocity.y > 1.2f && riding[slot] > 3)
            { rampFlying[slot] = true; rampAir[slot] = rampPeak[slot] = 0; rampStart[slot] = trailer.Body.position; }
            if (!rampFlying[slot]) return;
            rampAir[slot] += dt; rampPeak[slot] = Mathf.Max(rampPeak[slot], trailer.Body.position.y - rampStart[slot].y);
            if (!grounded) return;
            if (rampAir[slot] >= .25f)
            { rampDistance[slot] = Vector3.ProjectOnPlane(trailer.Body.position - rampStart[slot], Vector3.up).magnitude; rampLandingUntil[slot] = riding[slot] + 1.5f; }
            rampFlying[slot] = false;
        }
    }
}
