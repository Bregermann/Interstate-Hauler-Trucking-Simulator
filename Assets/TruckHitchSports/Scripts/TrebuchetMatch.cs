using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class TrebuchetMatch : HitchCompetitionSession
    {
        public TrebuchetArm[] engines;
        public Transform[] targets;
        readonly float[] quiet = new float[4], flightTime = new float[4];
        readonly Vector3[] release = new Vector3[4];
        readonly int[] attempts = new int[4];
        readonly float[] resetAt = new float[4];
        readonly float[] longestThrow = new float[4], totalThrowDistance = new float[4];
        readonly int[] targetsHit = new int[4];
        readonly float[] assistPower = new float[4], aimAngle = new float[4], aimAge = new float[4], swingAge = new float[4];
        public override string Title => "TRAILER TREBUCHET";
        public override string OptionLabel => new[] { "LONGEST TRAILER TOSS", "TARGET ACCURACY", "CASTLE DEMOLITION", "MULTI-ROUND SIEGE" }[option % 4];
        public override string Help => "Tow into the loading cradle and stop. After locking the sling, drive clear of the loading platform before beginning the swing. Press ACTION to unhitch and lock the actual sling. While loaded, movement UP/DOWN adjusts winch assist and LEFT/RIGHT changes the mechanically locked arm's physical starting angle. Press ACTION to start the counterweight swing, then press ACTION during the swing to release the real trailer at its current velocity. Longest Toss scores supported throw distance. Accuracy scores final distance from the target. Castle Demolition scores genuine trailer/target collision chains. Multi-Round Siege gives three independently loaded physical throws with cumulative distance, accuracy and damage. Let the projectile land and settle; a timeout or fall earns no unproven resting-position bonus.";
        protected override void BeginCompetition()
        {
            for (int i = 0; i < engines.Length; i++) { engines[i].Pivot.useLimits = false; engines[i].ResetForRound(); quiet[i] = flightTime[i] = resetAt[i] = longestThrow[i] = totalThrowDistance[i] = aimAngle[i] = aimAge[i] = swingAge[i] = 0; assistPower[i] = .65f; attempts[i] = targetsHit[i] = 0; release[i] = Vector3.zero; }
            Physics.SyncTransforms();
            foreach (var target in GetComponentsInChildren<SiegeTarget>()) target.ResetTarget();
        }
        public string TrebuchetLoadingDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            var engine = engines[slot]; var load = trailers[slot];
            return "angle=" + engine.Pivot.angle.ToString("F1") + " loadingLock=" + engine.Arm.isKinematic + " worldPitch=" + engine.Arm.rotation.eulerAngles.x.ToString("F1") + " limits=" + engine.Pivot.useLimits + " cradle=" + engine.LoadingCradle.bounds.ToString() + " cargoCOM=" + load.Body.worldCenterOfMass.ToString("F1") + " loaded=" + (engine.Load != null) + " swinging=" + engine.Swinging + " released=" + engine.Released;
        }
        public void TargetHit(int slot)
        {
            if (slot < 0 || slot >= setup.count || !Live || !engines[slot].Released) return;
            targetsHit[slot]++;
            if (option % 4 == 2 || option % 4 == 3) AddScore(slot, 150, "castle-block-demolished");
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            RecordMetric(slot, "trebuchet_longest_throw_metres", longestThrow[slot]);
            RecordMetric(slot, "trebuchet_total_throw_metres", totalThrowDistance[slot]);
            RecordMetric(slot, "trebuchet_targets_struck", targetsHit[slot]);
            RecordMetric(slot, "trebuchet_attempts_resolved", attempts[slot]);
        }
        protected override void TickCompetition(float dt)
        {
            Notice = "LOAD CRADLE → ACTION LOCK → ACTION SWING → ACTION RELEASE";
            for (int i = 0; i < setup.count; i++)
            {
                if (Completed[i]) continue; var engine = engines[i]; var trailer = trailers[i]; bool ai = IsAI(i), beganSwing = false;
                if (resetAt[i] > 0)
                {
                    if (couplers[i].Attached == null) couplers[i].TryCouple(trailer);
                    if (Elapsed < resetAt[i] || couplers[i].Attached == null) continue;
                    foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true)) part.isKinematic = false;
                    foreach (var part in trailer.GetComponentsInChildren<Rigidbody>(true)) part.isKinematic = false;
                    resetAt[i] = 0; continue;
                }
                if (engine.Load == null)
                {
                    Vector3 target = engine.LoadingCradle.bounds.center;
                    if (ai) DriveLoadingLane(i,target); else trucks[i].TickDrive(commands[i]);
                    bool inCradle = engine.LoadingCradle.bounds.Contains(trailer.Body.worldCenterOfMass);
                    if (ai && inCradle) ParkAI(i);
                    if ((ai || actions[i]) && inCradle && trailer.Body.linearVelocity.magnitude < 2)
                    { couplers[i].Release(); engine.TryLoad(trailer); }
                    continue;
                }
                if (!engine.Swinging)
                {
                    bool tractorClear=Vector3.ProjectOnPlane(trucks[i].body.worldCenterOfMass-engine.LoadingCradle.bounds.center,Vector3.up).magnitude>18;
                    if(ai&&!tractorClear)DriveLoadingLane(i,engine.LoadingCradle.bounds.center+Vector3.forward*34);
                    else ParkAI(i);
                    aimAge[i] += dt;
                    if (!ai) { assistPower[i] = Mathf.Clamp01(assistPower[i] + commands[i].y * dt * .45f); aimAngle[i] = Mathf.Clamp(aimAngle[i] + commands[i].x * dt * 12, -15, 10); }
                    engine.Aim(aimAngle[i]);
                    if (tractorClear && (ai ? aimAge[i] > 1.5f : actions[i]))
                    {
                        engine.BeginSwing(assistPower[i]);
                        // Physical winch assist, expressed as joint torque.
                        // The normalized full-size counterweight/load inertia
                        // needs siege-scale torque rather than a truck gearbox.
                        var motor = engine.Pivot.motor;
                        motor.force = engine.Arm.mass * Mathf.Lerp(50, 120, assistPower[i]);
                        engine.Pivot.motor = motor;
                        beganSwing = true;
                    }
                    Notice = "ASSIST " + (assistPower[i] * 100).ToString("F0") + "% · ARM " + aimAngle[i].ToString("F0") + "° · ACTION SWING";
                }
                else trucks[i].TickDrive(ai ? DriveTo(i, starts[i] + Vector3.right * 15, 3) : commands[i]);
                if (engine.Swinging) swingAge[i] += dt;
                if (engine.Swinging && !beganSwing && !engine.Released && (ai ? ShouldReleaseAI(i) : actions[i]))
                { if (engine.Release()) release[i] = trailer.Body.worldCenterOfMass; }
                if (!engine.Released) continue;
                flightTime[i] += dt;
                quiet[i] = trailer.Body.linearVelocity.sqrMagnitude < .2f && trailer.Body.angularVelocity.sqrMagnitude < .2f ? quiet[i] + dt : 0;
                if (quiet[i] > 2 || flightTime[i] > 40 || trailer.Body.position.y < -25)
                {
                    float distance = Vector3.ProjectOnPlane(trailer.Body.position - release[i], Vector3.up).magnitude;
                    longestThrow[i] = Mathf.Max(longestThrow[i], distance); totalThrowDistance[i] += distance;
                    bool settled = quiet[i] > 2 && trailer.Body.position.y > -5;
                    float accuracy = settled ? Mathf.Max(0, 1500 - Vector3.Distance(trailer.Body.position, targets[i].position) * 10) : 0;
                    AddScore(i, option % 4 == 0 ? (settled ? distance * 10 : 0) : option % 4 == 1 ? accuracy : option % 4 == 2 ? 0 : (settled ? distance * 3 : 0) + accuracy, "physical-trailer-toss");
                    attempts[i]++;
                    if (option % 4 == 3 && attempts[i] < 3)
                    {
                        couplers[i].Release(); engine.ResetForRound(); trailer.ResetForRound(); trucks[i].Prepare(starts[i], rotations[i]);
                        foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true)) part.isKinematic = true;
                        foreach (var part in trailer.GetComponentsInChildren<Rigidbody>(true)) part.isKinematic = true;
                        foreach (var target in GetComponentsInChildren<SiegeTarget>()) if (target.Lane == i) target.ResetTarget();
                        if (ai) assistPower[i] = Mathf.Clamp01(assistPower[i] + (Vector3.Distance(targets[i].position, release[i]) - distance) * .002f);
                        quiet[i] = flightTime[i] = aimAge[i] = swingAge[i] = 0; resetAt[i] = Elapsed + 3;
                    }
                    else Completed[i] = true;
                }
            }
        }
        void DriveLoadingLane(int slot,Vector3 cradle)
        {
            var truck=trucks[slot];var transmission=truck.tractorMotor.vehicle.powertrain.transmission;
            // A loading approach is a forward haul, not generic unsticking.
            // Speed magnitude would mistake an accidental reverse for forward
            // travel and feed more reverse throttle through native Auto DNR.
            transmission.automaticTransmissionDNRShiftType=NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            float forward=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);
            if(transmission.Gear<=0&&Mathf.Abs(forward)<.4f)transmission.ShiftInto(1);
            Vector3 laneTarget=new Vector3(cradle.x,truck.body.position.y,Mathf.Min(cradle.z+14,truck.body.position.z+12));
            Vector3 local=truck.transform.InverseTransformPoint(laneTarget);
            float steering=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*2.4f,-1,1);
            float desired=Mathf.Abs(steering)>.6f?1.2f:2;
            float demand=transmission.Gear<=0?1:Mathf.Clamp((desired-forward)*.65f,-1,1);
            truck.TickDrive(new Vector2(steering,demand));
        }
        bool ShouldReleaseAI(int slot)
        {
            var body = trailers[slot].Body; Vector3 velocity = body.linearVelocity;
            float forwardSpeed = Vector3.Dot(velocity, Vector3.forward);
            float elevation = Mathf.Atan2(velocity.y, Mathf.Max(.01f, Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude)) * Mathf.Rad2Deg;
            if (swingAge[slot] < .2f) return false;
            if (option % 4 == 0 && forwardSpeed > 5 && elevation >= 35 && elevation <= 60) return true;
            float gravity = Mathf.Max(.1f, Physics.gravity.magnitude), height = Mathf.Max(0, body.position.y - targets[slot].position.y);
            float time = (velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2 * gravity * height)) / gravity;
            Vector3 predicted = body.position + velocity * time;
            float miss = Vector3.ProjectOnPlane(predicted - targets[slot].position, Vector3.up).magnitude;
            float tolerance = Mathf.Lerp(35, 12, (int)setup.players[slot].difficulty / 2f);
            return forwardSpeed > 5 && miss < tolerance || swingAge[slot] > 7 && forwardSpeed > 2 && velocity.y > 0 || swingAge[slot] > 12;
        }
        protected override void StopGame()
        {
            base.StopGame();
            if (engines != null) foreach (var engine in engines) if (engine != null) engine.StopMechanism();
        }
        void ParkAI(int slot)
        {
            trucks[slot].TickDrive(Vector2.zero); var input = trucks[slot].tractorMotor.vehicle.input;
            input.states.handbrake = input.states.handbrakeRaw = 1; input.states.brakes = input.states.brakesRaw = 0;
        }
    }
}
