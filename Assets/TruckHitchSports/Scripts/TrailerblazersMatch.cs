using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckHitchSports
{
    public sealed class TrailerblazersMatch : HitchCompetitionSession
    {
        public Transform[] landingPads;
        public ParticleSystem[] exhaust;
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
        public override string Title => "TRAILERBLAZERS: ROCKET LAUNCH";
        public float PeakAltitude(int slot) => peak[slot];
        public float AirborneSeconds(int slot) => airborneSeconds[slot];
        public int StageReleaseCount(int slot) => stageReleases[slot];
        public bool LandedControlled(int slot) => controlledLanding[slot];
        public float ControlledFlightSeconds(int slot) => controlledAirSeconds[slot];
        public float GuidedDescentSeconds(int slot) => guidedDescentSeconds[slot];
        public float LandingErrorMetres(int slot) => landingError[slot];
        public string FlightDiagnostic(int slot) => "slot=" + slot + " elapsed=" + Elapsed.ToString("F2") + " peak=" + peak[slot].ToString("F2") + " air=" + airborneSeconds[slot].ToString("F2") + " fuelUsed=" + (fuelSeconds - fuel[slot]).ToString("F2") + " releases=" + stageReleases[slot] + " assemblyKg=" + assemblyMassObserved[slot].ToString("F0") + " stageUpAtPeak=" + peakStageUp[slot].ToString("F3") + " minPoweredUp=" + minimumPoweredUp[slot].ToString("F3") + " maxThrustMomentNm=" + maximumThrustMoment[slot].ToString("F0") + " unexpectedStageLoss=" + unexpectedStageLoss[slot] + " controlledLanding=" + controlledLanding[slot];
        public override string ResultDetail(int slot) => option % 4 == 1 ? controlledAirSeconds[slot].ToString("F1") + "s CONTROLLED AIRTIME · " + (controlledLanding[slot] ? "CONTROLLED LANDING" : "FAILED LANDING") : option % 4 == 2 ? landingError[slot].ToString("F1") + "m PAD ERROR · " + guidedDescentSeconds[slot].ToString("F1") + "s GUIDED DESCENT" : peak[slot].ToString("F1") + "m ALTITUDE · " + flight[slot].ToString("F1") + "s FLIGHT · " + (fuelSeconds - fuel[slot]).ToString("F1") + "s FUEL";
        public override string OptionLabel => new[] { "HIGHEST ALTITUDE", "CONTROLLED FLIGHT", "PRECISION LANDING", "ROCKET DISTANCE" }[option % 4];
        public override string Help => "Ignite a physically coupled booster trailer with ACTION. Steer pitch/yaw with your movement controls; hold ACTION to burn fuel, release then press again after three seconds to separate the booster. After separation, hold ACTION for the air brake/parachute. Land on your illuminated pad for a controlled-landing bonus. Fuel, altitude, distance and impact are measured from the real rig. Detached boosters remain physical objects. Each participant has an independent launch lane. Precision Landing adds steerable descent: after staging, hold ACTION and use movement controls to guide the physical air brake toward your pad; centered controls damp sideways drift. Controlled Flight scores only actual airborne time with a stable upright rig, followed by a controlled landing.";
        protected override void BeginCompetition()
        {
            for (int i = 0; i < 4; i++)
            {
                fuel[i] = fuelSeconds; flight[i] = peak[i] = quiet[i] = distanceTravelled[i] = airborneSeconds[i] = maximumThrustMoment[i] = controlledAirSeconds[i] = guidedDescentSeconds[i] = landingError[i] = 0;
                stageReleases[i] = 0; minimumPoweredUp[i] = 1; peakStageUp[i] = 1;
                controlledLanding[i] = ignited[i] = separated[i] = unexpectedStageLoss[i] = false;
                tractorBodies[i] = trucks[i].GetComponentsInChildren<Rigidbody>(true);
                var bodies = new System.Collections.Generic.List<Rigidbody>(tractorBodies[i]);
                foreach (var part in trailers[i].GetComponentsInChildren<Rigidbody>(true)) if (!bodies.Contains(part)) bodies.Add(part);
                launchBodies[i] = bodies.ToArray();
                var nozzles = new System.Collections.Generic.List<Transform>();
                foreach (var part in trailers[i].GetComponentsInChildren<Transform>(true)) if (part.name == "Booster thrust nozzle") nozzles.Add(part);
                if (nozzles.Count != 4) throw new System.InvalidOperationException("Rocket stage requires its four authored physical booster nozzles.");
                boosterNozzles[i] = nozzles.ToArray();
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
                if (Completed[i]) continue;
                var body = trucks[i].body;
                bool ai = IsAI(i);
                bool aiStage = flight[i] > 3 && peak[i] >= aiStageAltitude && Vector3.Dot(trailers[i].transform.up, Vector3.up) > .8f;
                bool pressed = ai ? !ignited[i] || aiStage && !separated[i] : actions[i];
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
                    StabilizeAttitude(body, desiredUp);
                    if (!separated[i]) StabilizeAttitude(trailers[i].Body, desiredUp);
                }
                else
                {
                    Vector2 control = commands[i];
                    Vector3 localRates = body.transform.InverseTransformDirection(body.angularVelocity);
                    body.AddRelativeTorque(new Vector3(control.y, control.x, -localRates.z * .15f) * body.mass * 1.4f);
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
                    float descentSpeed = Mathf.Lerp(6, 35, Mathf.Clamp01((height - 20) / 180));
                    float upwardAcceleration = Mathf.Clamp(Physics.gravity.magnitude + (-descentSpeed - body.linearVelocity.y) * 1.4f, 0, 30);
                    float tractorMass = 0; foreach (var part in tractorBodies[i]) tractorMass += part.mass;
                    body.AddForce(Vector3.up * tractorMass * upwardAcceleration);
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
                desiredVelocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(landingPads[slot].position - body.position, Vector3.up) / remaining, 8);
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
            Vector3 desiredVelocity = Vector3.ClampMagnitude(offset / travelSeconds, 10);
            Vector3 horizontalAcceleration = Vector3.ClampMagnitude((desiredVelocity - Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up)) * .8f, 2);
            return (Vector3.up + horizontalAcceleration / gravity).normalized;
        }
        void StabilizeAttitude(Rigidbody body, Vector3 desiredUp)
        {
            Quaternion.FromToRotation(body.transform.up, desiredUp).ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180) angle -= 360;
            Vector3 acceleration = axis * angle * Mathf.Deg2Rad * attitudeGain - body.angularVelocity * attitudeDamping;
            acceleration = Vector3.ClampMagnitude(acceleration, 4);
            Quaternion principalAxes = body.rotation * body.inertiaTensorRotation;
            Vector3 principalAcceleration = Quaternion.Inverse(principalAxes) * acceleration;
            Vector3 torque = principalAxes * Vector3.Scale(body.inertiaTensor, principalAcceleration);
            body.AddTorque(torque, ForceMode.Force);
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
