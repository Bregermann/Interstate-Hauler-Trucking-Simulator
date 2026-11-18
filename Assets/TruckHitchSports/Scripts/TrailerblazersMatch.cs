using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using UnityEngine.InputSystem;

namespace LWS.TruckHitchSports
{
    public sealed class TrailerblazersMatch : HitchCompetitionSession
    {
        public Transform[] landingPads;
        public ParticleSystem[] exhaust;
        public RocketFlightPresentation[] flightPresentation = System.Array.Empty<RocketFlightPresentation>();
        public float fuelSeconds = 12, thrustToWeight = 2.4f;
        public float aiStageAltitude = 75, attitudeGain = 5, attitudeDamping = 3.2f;
        readonly float[] fuel = new float[4], flight = new float[4], peak = new float[4], quiet = new float[4];
        readonly bool[] ignited = new bool[4], separated = new bool[4];
        readonly bool[] controlledLanding = new bool[4];
        readonly int[] stageReleases = new int[4];
        readonly float[] distanceTravelled = new float[4];
        readonly float[] airborneSeconds = new float[4];
        readonly float[] controlledAirSeconds = new float[4], guidedDescentSeconds = new float[4], landingError = new float[4];
        readonly Rigidbody[][] launchBodies = new Rigidbody[4][];
        readonly Rigidbody[][] tractorBodies = new Rigidbody[4][];
        readonly Transform[][] boosterNozzles = new Transform[4][];
        readonly float[] assemblyMassObserved = new float[4], peakStageUp = new float[4], minimumPoweredUp = new float[4], maximumThrustMoment = new float[4];
        readonly bool[] unexpectedStageLoss = new bool[4];
        readonly float[] appliedBrakeNewtons = new float[4], brakeSeconds = new float[4], maximumImpact = new float[4];
        readonly Key[] rollLeft = new Key[4], rollRight = new Key[4];
        int rollBindingSignature = int.MinValue;
        static readonly Key[] RollCandidates = { Key.Z, Key.X, Key.Delete, Key.PageDown, Key.Semicolon, Key.Quote, Key.LeftBracket, Key.RightBracket, Key.Numpad1, Key.Numpad3, Key.Numpad4, Key.Numpad6, Key.Numpad7, Key.Numpad9, Key.Backslash, Key.Backquote, Key.Numpad2, Key.Numpad5, Key.Numpad8, Key.Numpad0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Insert, Key.End, Key.PageUp, Key.U, Key.O, Key.P, Key.B, Key.N, Key.M, Key.R, Key.T, Key.F, Key.G, Key.H, Key.J, Key.K, Key.L };
        public override string Title => "TRAILERBLAZERS: ROCKET LAUNCH";
        public float PeakAltitude(int slot) => peak[slot];
        public float AirborneSeconds(int slot) => airborneSeconds[slot];
        public int StageReleaseCount(int slot) => stageReleases[slot];
        public bool LandedControlled(int slot) => controlledLanding[slot];
        public float ControlledFlightSeconds(int slot) => controlledAirSeconds[slot];
        public float GuidedDescentSeconds(int slot) => guidedDescentSeconds[slot];
        public float LandingErrorMetres(int slot) => landingError[slot];
        public float AppliedAirBrakeNewtons(int slot) => appliedBrakeNewtons[slot];
        public bool HasIgnited(int slot) => ignited[slot];
        public float MaximumImpactSpeed(int slot) => maximumImpact[slot];
        public void ObserveRocketImpact(int slot, float normalSpeed)
        { if (slot >= 0 && slot < setup.count && Live && ignited[slot] && airborneSeconds[slot] > .25f) maximumImpact[slot] = Mathf.Max(maximumImpact[slot], normalSpeed); }
        public override string ControlCaption(int slot) => base.ControlCaption(slot) + " · ROLL: " + RollCaption(slot);
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            if (slot < 0 || slot >= setup.count) { value = detail = null; return false; }
            value = Completed[slot] ? Scores[slot].ToString("F0") : peak[slot].ToString("F0") + "m";
            detail = Completed[slot] ? (controlledLanding[slot] ? "CONTROLLED LANDING" : "FLIGHT ENDED") : "FUEL " + fuel[slot].ToString("F1") + "s · ROLL " + RollCaption(slot);
            return true;
        }
        public string FlightDiagnostic(int slot) => "slot=" + slot + " elapsed=" + Elapsed.ToString("F2") + " peak=" + peak[slot].ToString("F2") + " air=" + airborneSeconds[slot].ToString("F2") + " fuelUsed=" + (fuelSeconds - fuel[slot]).ToString("F2") + " releases=" + stageReleases[slot] + " assemblyKg=" + assemblyMassObserved[slot].ToString("F0") + " stageUpAtPeak=" + peakStageUp[slot].ToString("F3") + " minPoweredUp=" + minimumPoweredUp[slot].ToString("F3") + " maxThrustMomentNm=" + maximumThrustMoment[slot].ToString("F0") + " unexpectedStageLoss=" + unexpectedStageLoss[slot] + " controlledLanding=" + controlledLanding[slot];
        public override string ResultDetail(int slot) => option % 4 == 1 ? controlledAirSeconds[slot].ToString("F1") + "s CONTROLLED AIRTIME · " + (controlledLanding[slot] ? "CONTROLLED LANDING" : "FAILED LANDING") : option % 4 == 2 ? landingError[slot].ToString("F1") + "m PAD ERROR · " + guidedDescentSeconds[slot].ToString("F1") + "s GUIDED DESCENT" : peak[slot].ToString("F1") + "m ALTITUDE · " + flight[slot].ToString("F1") + "s FLIGHT · " + (fuelSeconds - fuel[slot]).ToString("F1") + "s FUEL";
        public override string OptionLabel => new[] { "HIGHEST ALTITUDE", "CONTROLLED FLIGHT", "PRECISION LANDING", "ROCKET DISTANCE" }[option % 4];
        public override string Help => "Ignite a physically coupled booster trailer with ACTION. Steer pitch/yaw with your movement controls and use the shown independent ROLL bindings; hold ACTION to burn fuel, release then press again after three seconds to separate the booster. After separation, hold ACTION for the air brake/parachute. Land on your illuminated pad for a controlled-landing bonus. Fuel, altitude, distance and impact are measured from the real rig. Detached boosters remain physical objects. Each participant has an independent launch lane. Precision Landing adds steerable descent: after staging, hold ACTION and use movement controls to guide the physical air brake toward your pad; centered controls damp sideways drift. Controlled Flight scores only actual airborne time with a stable upright rig, followed by a controlled landing. Whole pads roll with LB/RB; shared left-stick players use D-pad left/right and shared right-stick players use west/east face buttons. Keyboard roll keys are shown per participant and avoid assigned driving/action/horn keys. AI difficulty changes real attitude control, reaction, staging height and guidance; Normal retains its established flight law.";
        protected override void BeginCompetition()
        {
            EnsureRollBindings();
            for (int i = 0; i < 4; i++)
            {
                fuel[i] = fuelSeconds; flight[i] = peak[i] = quiet[i] = distanceTravelled[i] = airborneSeconds[i] = maximumThrustMoment[i] = controlledAirSeconds[i] = guidedDescentSeconds[i] = landingError[i] = 0;
                stageReleases[i] = 0; minimumPoweredUp[i] = 1; peakStageUp[i] = 1; appliedBrakeNewtons[i] = brakeSeconds[i] = maximumImpact[i] = 0;
                controlledLanding[i] = ignited[i] = separated[i] = unexpectedStageLoss[i] = false;
                tractorBodies[i] = trucks[i].GetComponentsInChildren<Rigidbody>(true);
                var bodies = new System.Collections.Generic.List<Rigidbody>(tractorBodies[i]);
                foreach (var part in trailers[i].GetComponentsInChildren<Rigidbody>(true)) if (!bodies.Contains(part)) bodies.Add(part);
                launchBodies[i] = bodies.ToArray();
                var nozzles = new System.Collections.Generic.List<Transform>();
                foreach (var part in trailers[i].GetComponentsInChildren<Transform>(true)) if (part.name == "Booster thrust nozzle") nozzles.Add(part);
                if (nozzles.Count != 4) throw new System.InvalidOperationException("Rocket stage requires its four authored physical booster nozzles.");
                boosterNozzles[i] = nozzles.ToArray();
                if (flightPresentation != null && i < flightPresentation.Length && flightPresentation[i] != null) flightPresentation[i].ResetForRound(setup.players[i].color);
                if (exhaust != null && i < exhaust.Length && exhaust[i] != null) exhaust[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            RecordMetric(slot, "rocket_peak_altitude_metres", peak[slot]);
            RecordMetric(slot, "rocket_flight_seconds", airborneSeconds[slot]);
            RecordMetric(slot, "rocket_ignition_elapsed_seconds", flight[slot]);
            RecordMetric(slot, "rocket_fuel_seconds_consumed", Mathf.Max(0, fuelSeconds - fuel[slot]));
            RecordMetric(slot, "rocket_stage_releases", stageReleases[slot]);
            RecordMetric(slot, "rocket_air_brake_seconds", brakeSeconds[slot]); RecordMetric(slot, "rocket_maximum_normal_impact_mps", maximumImpact[slot]);
            RecordMetric(slot, "rocket_controlled_landing", controlledLanding[slot] ? 1 : 0);
            RecordMetric(slot, "rocket_distance_metres", distanceTravelled[slot]);
            if (option % 4 == 1) RecordMetric(slot, "rocket_controlled_airborne_seconds", controlledAirSeconds[slot]);
            if (option % 4 == 2)
            {
                RecordMetric(slot, "rocket_guided_descent_seconds", guidedDescentSeconds[slot]);
                if (landingPads != null && slot < landingPads.Length && landingPads[slot] != null) RecordMetric(slot, "rocket_landing_error_metres", landingError[slot]);
            }
        }
        protected override void TickCompetition(float dt)
        {
            Notice = option % 4 == 2 ? "ACTION IGNITE / STAGE · HOLD ACTION + MOVE TO GUIDE DESCENT" : "ACTION IGNITE / STAGE · HOLD FOR THRUST OR DESCENT BRAKE";
            for (int i = 0; i < setup.count; i++)
            {
                appliedBrakeNewtons[i] = 0;
                if (Completed[i]) continue;
                var body = trucks[i].body;
                bool ai = IsAI(i);
                bool aiStage = flight[i] > 3 && peak[i] >= DifficultyValue(i, aiStageAltitude * .8f, aiStageAltitude, aiStageAltitude * 1.1f) && Vector3.Dot(trailers[i].transform.up, Vector3.up) > .8f;
                bool pressed = ai ? !ignited[i] && Elapsed >= DifficultyValue(i, 3.75f, 3, 3) || aiStage && !separated[i] : actions[i];
                if (!ignited[i] && pressed && couplers[i].Attached != null)
                { ignited[i] = true; trucks[i].TickDrive(Vector2.zero); trailers[i].SetBrakes(0); }
                if (!ignited[i]) { trucks[i].TickDrive(Vector2.zero); continue; }
                // Keep native wheel/suspension contact available for the real
                // landing; flight controls do not supply road throttle.
                trucks[i].TickDrive(Vector2.zero);
                flight[i] += dt;
                if (body.position.y - starts[i].y > peak[i]) { peak[i] = body.position.y - starts[i].y; peakStageUp[i] = Vector3.Dot(trailers[i].transform.up, Vector3.up); }
                if (!separated[i] && !couplers[i].Hitch.module.attached) unexpectedStageLoss[i] = true;
                distanceTravelled[i] = Vector3.ProjectOnPlane(body.position - starts[i], Vector3.up).magnitude;
                if (ai)
                {
                    Vector3 desiredUp = AILaunchUp(i);
                    StabilizeAttitude(body, desiredUp, i);
                    if (!separated[i]) StabilizeAttitude(trailers[i].Body, desiredUp, i);
                }
                else
                {
                    Vector2 control = commands[i];
                    Vector3 localRates = body.transform.InverseTransformDirection(body.angularVelocity);
                    body.AddRelativeTorque(new Vector3(control.y, control.x, ReadRollInput(i) - localRates.z * .15f) * body.mass * 1.4f);
                }
                bool burn = ai || ActionHeld(i);
                if (!separated[i] && fuel[i] > 0 && burn)
                {
                    fuel[i] = Mathf.Max(0, fuel[i] - dt);
                    ApplyBoosterThrust(i);
                    if (exhaust != null && i < exhaust.Length && exhaust[i] != null && !exhaust[i].isPlaying) exhaust[i].Play();
                }
                if (!separated[i] && flight[i] > 3 && (pressed || fuel[i] <= 0))
                {
                    bool wasAttached = couplers[i].Hitch.module.attached && couplers[i].Attached != null;
                    if (couplers[i].Release() != null && wasAttached) stageReleases[i]++;
                    separated[i] = true;
                    if (exhaust != null && i < exhaust.Length && exhaust[i] != null) exhaust[i].Stop();
                }
                if (separated[i] && body.linearVelocity.y < 0 && burn)
                {
                    float height = Mathf.Max(0, body.position.y - starts[i].y);
                    // Precision guidance estimates its remaining glide using a
                    // 6 m/s canopy descent. The other modes' fast high-altitude
                    // descent provided less than half that steering time and
                    // caused a genuine ~72 m undershoot of the authored pad.
                    // Reuse the same capped physical lift; never change velocity.
                    float descentSpeed = option % 4 == 2 ? 6 : Mathf.Lerp(6, 35, Mathf.Clamp01((height - 20) / 180));
                    float upwardAcceleration = Mathf.Clamp(Physics.gravity.magnitude + (-descentSpeed - body.linearVelocity.y) * 1.4f, 0, 30);
                    float tractorMass = 0; foreach (var part in tractorBodies[i]) tractorMass += part.mass;
                    body.AddForce(Vector3.up * tractorMass * upwardAcceleration);
                    appliedBrakeNewtons[i] = tractorMass * upwardAcceleration; if (upwardAcceleration > 0) brakeSeconds[i] += dt;
                    if (option % 4 == 2 && height > 3) GuidePrecisionDescent(i, tractorMass, upwardAcceleration, ai, dt);
                    if (!ai) body.AddTorque(Vector3.Cross(body.transform.up, Vector3.up) * body.mass * 2 - body.angularVelocity * body.mass * .4f);
                }
                bool supported = Physics.Raycast(body.position + Vector3.up, Vector3.down, 2.6f, 1, QueryTriggerInteraction.Ignore);
                if (!supported)
                {
                    airborneSeconds[i] += dt;
                    if (option % 4 == 1 && Vector3.Dot(body.transform.up, Vector3.up) > .65f && body.angularVelocity.magnitude < 1.2f) controlledAirSeconds[i] += dt;
                }
                quiet[i] = supported && body.linearVelocity.magnitude < 2 ? quiet[i] + dt : 0;
                if (flight[i] > 5 && (quiet[i] > 2 || flight[i] > 65 || body.position.y < -40))
                {
                    float distance = Vector3.ProjectOnPlane(body.position - starts[i], Vector3.up).magnitude;
                    float accuracy = landingPads != null && i < landingPads.Length ? Mathf.Max(0, 1000 - Vector3.Distance(body.position, landingPads[i].position) * 8) : 0;
                    float controlled = quiet[i] > 2 && Vector3.Dot(body.transform.up, Vector3.up) > .5f ? 1 : 0;
                    controlledLanding[i] = controlled > 0;
                    if (landingPads != null && i < landingPads.Length && landingPads[i] != null) landingError[i] = Vector3.ProjectOnPlane(body.position - landingPads[i].position, Vector3.up).magnitude;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log("HH10_ROCKET_FLIGHT " + FlightDiagnostic(i));
#endif
                    double score = option % 4 == 0 ? peak[i] * 10 : option % 4 == 1 ? controlledAirSeconds[i] * controlled * 20 : option % 4 == 2 ? accuracy * controlled : distance * 10;
                    AddScore(i, score + controlled * 100, "rocket-flight"); Completed[i] = true;
                }
            }
        }
        void GuidePrecisionDescent(int slot, float mass, float liftAcceleration, bool ai, float dt)
        {
            var body = trucks[slot].body;
            Vector3 flatVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 wind = presentationWeather != null ? Vector3.ClampMagnitude(Vector3.ProjectOnPlane(presentationWeather.Flow, Vector3.up), 20) : Vector3.zero;
            Vector3 desiredVelocity;
            if (ai && landingPads != null && slot < landingPads.Length && landingPads[slot] != null)
            {
                float height = Mathf.Max(0, body.position.y - landingPads[slot].position.y);
                float remaining = Mathf.Max(2, height / Mathf.Max(6, -body.linearVelocity.y));
                desiredVelocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(landingPads[slot].position - body.position, Vector3.up) / remaining, DifficultyValue(slot, 6, 8, 9));
            }
            else
            {
                Vector3 forward = Vector3.ProjectOnPlane(body.transform.forward, Vector3.up);
                if (forward.sqrMagnitude < .01f) forward = Vector3.ProjectOnPlane(rotations[slot] * Vector3.forward, Vector3.up);
                forward.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, forward);
                desiredVelocity = Vector3.ClampMagnitude(right * commands[slot].x + forward * commands[slot].y, 1) * 8;
            }
            // Deflect only the existing deployed air brake's actual lift. This
            // follows the owned BaseJumper's force-based steering/wind pattern,
            // keeping the rocket's existing match and measured physics authority.
            Vector3 acceleration = (desiredVelocity - flatVelocity) * .65f + (wind - flatVelocity) * .12f;
            acceleration = Vector3.ClampMagnitude(acceleration, Mathf.Min(2, liftAcceleration * .2f));
            body.AddForce(acceleration * mass, ForceMode.Force);
            if (!ai) StabilizeAttitude(body, Vector3.up);
            guidedDescentSeconds[slot] += dt;
        }
        Vector3 AILaunchUp(int slot)
        {
            if (separated[slot] || flight[slot] < 1) return Vector3.up;
            var body = trucks[slot].body;
            if (landingPads == null || slot >= landingPads.Length || landingPads[slot] == null) return Vector3.up;
            float gravity = Mathf.Max(.1f, Physics.gravity.magnitude);
            float rising = Mathf.Max(0, body.linearVelocity.y);
            float height = Mathf.Max(0, body.position.y - starts[slot].y);
            float predictedApex = height + rising * rising / (2 * gravity);
            float travelSeconds = Mathf.Max(5, rising / gravity + predictedApex / 12);
            Vector3 offset = Vector3.ProjectOnPlane(landingPads[slot].position - body.position, Vector3.up);
            Vector3 desiredVelocity = Vector3.ClampMagnitude(offset / travelSeconds, DifficultyValue(slot, 8, 10, 12));
            Vector3 horizontalAcceleration = Vector3.ClampMagnitude((desiredVelocity - Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up)) * .8f, DifficultyValue(slot, 1.5f, 2, 2.3f));
            return (Vector3.up + horizontalAcceleration / gravity).normalized;
        }
        void StabilizeAttitude(Rigidbody body, Vector3 desiredUp, int aiSlot = -1)
        {
            Quaternion.FromToRotation(body.transform.up, desiredUp).ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180) angle -= 360;
            float gain = aiSlot < 0 ? attitudeGain : DifficultyValue(aiSlot, attitudeGain * .75f, attitudeGain, attitudeGain * 1.2f);
            float damping = aiSlot < 0 ? attitudeDamping : DifficultyValue(aiSlot, attitudeDamping * .85f, attitudeDamping, attitudeDamping * 1.15f);
            Vector3 acceleration = axis * angle * Mathf.Deg2Rad * gain - body.angularVelocity * damping;
            acceleration = Vector3.ClampMagnitude(acceleration, 4);
            Quaternion principalAxes = body.rotation * body.inertiaTensorRotation;
            Vector3 principalAcceleration = Quaternion.Inverse(principalAxes) * acceleration;
            Vector3 torque = principalAxes * Vector3.Scale(body.inertiaTensor, principalAcceleration);
            body.AddTorque(torque, ForceMode.Force);
        }
        float DifficultyValue(int slot, float easy, float normal, float hard)
            => setup.players[slot].difficulty == BowlingDifficulty.Easy ? easy : setup.players[slot].difficulty == BowlingDifficulty.Hard ? hard : normal;
        void EnsureRollBindings()
        {
            var horn = GetComponent<SportsUniversalHorn>();
            int signature = setup.count;
            unchecked { for (int i = 0; i < setup.count; i++) { var p = setup.players[i]; signature = signature * 31 + (int)p.source; signature = signature * 31 + (int)p.forward; signature = signature * 31 + (int)p.back; signature = signature * 31 + (int)p.left; signature = signature * 31 + (int)p.right; signature = signature * 31 + (int)actionKeys[i]; signature = signature * 31 + (int)(horn != null ? horn.HornKey(i) : Key.None); } }
            if (signature == rollBindingSignature) return;
            rollBindingSignature = signature;
            var reserved = new System.Collections.Generic.HashSet<Key> { Key.None, Key.Escape, Key.Tab, Key.C, Key.Q, Key.E };
            for (int i = 0; i < setup.count; i++)
            {
                var p = setup.players[i]; if (p.source != BowlingSource.Keyboard) continue;
                reserved.Add(p.forward); reserved.Add(p.back); reserved.Add(p.left); reserved.Add(p.right); reserved.Add(actionKeys[i]);
                if (horn != null) reserved.Add(horn.HornKey(i));
            }
            int next = 0;
            for (int i = 0; i < setup.count; i++)
            {
                rollLeft[i] = rollRight[i] = Key.None;
                if (setup.players[i].source != BowlingSource.Keyboard) continue;
                while (next < RollCandidates.Length && reserved.Contains(RollCandidates[next])) next++;
                if (next < RollCandidates.Length) { rollLeft[i] = RollCandidates[next++]; reserved.Add(rollLeft[i]); }
                while (next < RollCandidates.Length && reserved.Contains(RollCandidates[next])) next++;
                if (next < RollCandidates.Length) { rollRight[i] = RollCandidates[next++]; reserved.Add(rollRight[i]); }
            }
        }
        public string RollCaption(int slot)
        {
            EnsureRollBindings();
            var source = setup.players[slot].source;
            if (source == BowlingSource.AI) return "AI ATTITUDE";
            if (source == BowlingSource.WholeController) return "LB / RB";
            if (source == BowlingSource.LeftStick) return "D-PAD LEFT / RIGHT";
            if (source == BowlingSource.RightStick) return "WEST / EAST";
            return rollLeft[slot] + " / " + rollRight[slot];
        }
        public Key RollKey(int slot, bool positive) { EnsureRollBindings(); return positive ? rollRight[slot] : rollLeft[slot]; }
        public float ReadRollInput(int slot)
        {
            if (slot < 0 || slot >= setup.count || !Live || SportsControllerKeyboard.InputBlocked || !Ready(slot) || !setup.players[slot].released) return 0;
            var p = setup.players[slot];
            if (p.source == BowlingSource.Keyboard)
            {
                EnsureRollBindings();
                return (rollRight[slot] != Key.None && SportsDeviceInput.KeyHeld(rollRight[slot]) ? 1 : 0) - (rollLeft[slot] != Key.None && SportsDeviceInput.KeyHeld(rollLeft[slot]) ? 1 : 0);
            }
            if (p.source == BowlingSource.WholeController) return (p.pad.rightShoulder.isPressed ? 1 : 0) - (p.pad.leftShoulder.isPressed ? 1 : 0);
            if (p.source == BowlingSource.LeftStick) return (p.pad.dpad.right.isPressed ? 1 : 0) - (p.pad.dpad.left.isPressed ? 1 : 0);
            if (p.source == BowlingSource.RightStick) return (p.pad.buttonEast.isPressed ? 1 : 0) - (p.pad.buttonWest.isPressed ? 1 : 0);
            return 0;
        }
        void ApplyBoosterThrust(int slot)
        {
            float mass = 0; Vector3 center = Vector3.zero;
            foreach (var part in launchBodies[slot]) { mass += part.mass; center += part.worldCenterOfMass * part.mass; }
            center /= Mathf.Max(.001f, mass); assemblyMassObserved[slot] = mass;
            var stage = trailers[slot].Body; Vector3 forward = stage.transform.forward, right = stage.transform.right;
            float front = float.NegativeInfinity, back = float.PositiveInfinity, left = float.PositiveInfinity, rightmost = float.NegativeInfinity;
            foreach (var nozzle in boosterNozzles[slot])
            {
                float z = Vector3.Dot(nozzle.position, forward), x = Vector3.Dot(nozzle.position, right);
                front = Mathf.Max(front, z); back = Mathf.Min(back, z); left = Mathf.Min(left, x); rightmost = Mathf.Max(rightmost, x);
            }
            float frontShare = Mathf.InverseLerp(back, front, Vector3.Dot(center, forward));
            float rightShare = Mathf.InverseLerp(left, rightmost, Vector3.Dot(center, right));
            Vector3 force = stage.transform.up * mass * Physics.gravity.magnitude * thrustToWeight, moment = Vector3.zero;
            // Physical front/rear booster throttles place their resultant through
            // the measured coupled COM. Tail-only thrust otherwise pitches a
            // nineteen-tonne road rig even with centered flight controls.
            foreach (var nozzle in boosterNozzles[slot])
            {
                bool isFront = Vector3.Dot(nozzle.position, forward) > (front + back) * .5f;
                bool isRight = Vector3.Dot(nozzle.position, right) > (left + rightmost) * .5f;
                float weight = (isFront ? frontShare : 1 - frontShare) * (isRight ? rightShare : 1 - rightShare);
                Vector3 component = force * weight;
                stage.AddForceAtPosition(component, nozzle.position, ForceMode.Force);
                moment += Vector3.Cross(nozzle.position - center, component);
            }
            maximumThrustMoment[slot] = Mathf.Max(maximumThrustMoment[slot], moment.magnitude);
            minimumPoweredUp[slot] = Mathf.Min(minimumPoweredUp[slot], Vector3.Dot(stage.transform.up, Vector3.up));
        }
    }
}
