using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class TrebuchetMatch : HitchCompetitionSession
    {
        public TrebuchetArm[] engines;
        Collider[][] tractorShapes;
        public Transform[] targets;
        readonly float[] quiet = new float[4], flightTime = new float[4];
        readonly Vector3[] release = new Vector3[4];
        readonly int[] attempts = new int[4];
        readonly float[] resetAt = new float[4];
        readonly float[] longestThrow = new float[4], totalThrowDistance = new float[4];
        readonly int[] targetsHit = new int[4];
        readonly float[] assistPower = new float[4], aimAngle = new float[4], aimAge = new float[4], swingAge = new float[4];
        readonly float[] bestProjectedRange = new float[4], observedProjectedRange = new float[4], observedElevation = new float[4];
        struct TrajectoryPeak
        {
            public Vector3 velocity, position, armAngularVelocity, armInertia;
            public float range, swingSeconds, motorForce, armPitch;
        }
        readonly TrajectoryPeak[] trajectoryPeak = new TrajectoryPeak[4];
        readonly bool[] peakReported = new bool[4];
        readonly float[] peakDeclineSeconds = new float[4];
        public override string Title => "TRAILER TREBUCHET";
        public override string OptionLabel => new[] { "LONGEST TRAILER TOSS", "TARGET ACCURACY", "CASTLE DEMOLITION", "MULTI-ROUND SIEGE" }[option % 4];
        public override string Help => "Tow into the loading cradle and stop. After locking the sling, your movement controls still drive the tractor: leave both loading roads completely. The tractor then parks while their hydraulic bridges retract. Only after retraction do movement UP/DOWN set assist and LEFT/RIGHT aim the locked arm; ACTION begins the real swing. Press ACTION in the receiving cradle to unhitch and lock the actual sling. After the safe aim stage, press ACTION to start the counterweight swing, then ACTION as the load rises over the pivot toward the target field to release the real trailer at its current velocity. Longest Toss scores supported throw distance. Accuracy scores final distance from the target. Castle Demolition scores genuine trailer/target collision chains. Multi-Round Siege gives three independently loaded physical throws with cumulative distance, accuracy and damage. Let the projectile land and settle; a timeout or fall earns no unproven resting-position bonus.";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = Scores[slot].ToString("F0");
            var engine = engines[slot];
            detail = Completed[slot] ? "THROW COMPLETE" : engine.Load == null ? "LOAD CRADLE · ACTION LOCK" :
                engine.PreparingToSwing ? "BRIDGES RETRACTING · WAIT" : !engine.Swinging && !engine.LoadingBridgesReady ? "DRIVE CLEAR OF LOADING ROADS" :
                !engine.Swinging ? "MOVE AIM/ASSIST · ACTION SWING" : !engine.Released ? "ACTION RELEASE" : "PROJECTILE FLIGHT / SETTLING";
            return true;
        }
        protected override void BeginCompetition()
        {
            tractorShapes = new Collider[trucks.Length][];
            for (int i = 0; i < trucks.Length; i++) tractorShapes[i] = trucks[i].GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < engines.Length; i++) { engines[i].Pivot.useLimits = false; engines[i].ResetForRound(); quiet[i] = flightTime[i] = resetAt[i] = longestThrow[i] = totalThrowDistance[i] = aimAngle[i] = aimAge[i] = swingAge[i] = 0; assistPower[i] = IsAI(i) && option % 4 == 0 ? 1 : .65f; trajectoryPeak[i] = default; peakReported[i] = false; peakDeclineSeconds[i] = 0; bestProjectedRange[i] = observedProjectedRange[i] = observedElevation[i] = 0; attempts[i] = targetsHit[i] = 0; release[i] = Vector3.zero; }
            Physics.SyncTransforms();
            foreach (var target in GetComponentsInChildren<SiegeTarget>()) target.ResetTarget();
        }
        public string TrebuchetLoadingDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            var engine = engines[slot]; var load = trailers[slot];
            return "angle=" + engine.Pivot.angle.ToString("F1") + " loadingLock=" + engine.Arm.isKinematic + " worldPitch=" + engine.Arm.rotation.eulerAngles.x.ToString("F1") + " limits=" + engine.Pivot.useLimits + " cradle=" + engine.LoadingCradle.bounds.ToString() + " cargoCOM=" + load.Body.worldCenterOfMass.ToString("F1") + " loaded=" + (engine.Load != null) + " swinging=" + engine.Swinging + " released=" + engine.Released + " sling=" + engine.SlingDiagnostic() + " " + engine.BridgeDiagnostic();
        }
        public void TargetHit(int slot)
        {
            if (slot < 0 || slot >= setup.count || !Live || !engines[slot].Released) return;
            targetsHit[slot]++;
            if (option % 4 == 2 || option % 4 == 3) AddScore(slot, 150, "castle-block-demolished");
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            ReportMeasuredPeak(slot);
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
                    bool inCradle = engine.CanLoad(trailer);
                    if (ai && inCradle) ParkAI(i);
                    if ((ai || actions[i]) && inCradle && trailer.Body.linearVelocity.magnitude < 2)
                    { couplers[i].Release(); engine.TryLoad(trailer); }
                    continue;
                }
                if (!engine.Swinging)
                {
                    bool tractorClear=Vector3.ProjectOnPlane(trucks[i].body.worldCenterOfMass-engine.LoadingCradle.bounds.center,Vector3.up).magnitude>18 && engine.TractorClearOfLoadingBridges(tractorShapes[i]);
                    if (!ai && !engine.LoadingBridgesReady)
                    {
                        if (!tractorClear && !engine.PreparingToSwing)
                        {
                            trucks[i].TickDrive(commands[i]);
                            Notice = "SLING LOCKED · DRIVE YOUR TRUCK CLEAR OF BOTH LOADING ROADS";
                        }
                        else
                        {
                            ParkAI(i);
                            engine.PrepareLoadingBridgesForAim();
                            Notice = "RETRACTING LOADING BRIDGES · WAIT BEFORE AIMING";
                        }
                        // Driving clear must not also rotate the captive arm
                        // into a fixed road or silently change launch power.
                        continue;
                    }
                    if(ai&&!tractorClear)DriveOffLoadingBridge(i,engine.LoadingCradle.bounds.center+Vector3.right*20+Vector3.forward*30);
                    else ParkAI(i);
                    aimAge[i] += dt;
                    if (!ai) { assistPower[i] = Mathf.Clamp01(assistPower[i] + commands[i].y * dt * .45f); aimAngle[i] = Mathf.Clamp(aimAngle[i] + commands[i].x * dt * 12, -15, 10); }
                    engine.Aim(aimAngle[i]);
                    if ((ai ? tractorClear : engine.LoadingBridgesReady) && (ai ? aimAge[i] > 1.5f : actions[i]))
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
                    if (engine.PreparingToSwing) { Notice = "RETRACTING LOADING BRIDGES · STAND CLEAR"; continue; }
                    Notice = "ASSIST " + (assistPower[i] * 100).ToString("F0") + "% · ARM " + aimAngle[i].ToString("F0") + "° · ACTION SWING";
                }
                else trucks[i].TickDrive(ai ? DriveTo(i, starts[i] + Vector3.right * 15, 3) : commands[i]);
                if (engine.Swinging) swingAge[i] += dt;
                if (engine.Swinging && !beganSwing && !engine.Released && (ai ? ShouldReleaseAI(i) : actions[i]))
                { if (engine.Release())
                    {
                        release[i] = trailer.Body.worldCenterOfMass;
                        ReportMeasuredPeak(i);
                        RecordMetric(i, "trebuchet_release_downrange_mps", Vector3.Dot(engine.ReleaseWorldVelocity, Vector3.ProjectOnPlane(targets[i].position - release[i], Vector3.up).normalized), "attempt=" + (attempts[i] + 1));
                        RecordMetric(i, "trebuchet_initial_release_penetrations", engine.InitiallyPenetratingPairs, "attempt=" + (attempts[i] + 1));
                    } }
                if (!engine.Released) continue;
                flightTime[i] += dt;
                quiet[i] = trailer.Body.linearVelocity.sqrMagnitude < .2f && trailer.Body.angularVelocity.sqrMagnitude < .2f ? quiet[i] + dt : 0;
                if (quiet[i] > 2 || flightTime[i] > 40 || trailer.Body.position.y < -25)
                {
                    float distance = Vector3.ProjectOnPlane(trailer.Body.worldCenterOfMass - release[i], Vector3.up).magnitude;
                    longestThrow[i] = Mathf.Max(longestThrow[i], distance); totalThrowDistance[i] += distance;
                    bool settled = quiet[i] > 2 && trailer.Body.position.y > -5;
                    float accuracy = settled ? Mathf.Max(0, 1500 - Vector3.Distance(trailer.Body.worldCenterOfMass, targets[i].position) * 10) : 0;
                    AddScore(i, option % 4 == 0 ? (settled ? distance * 10 : 0) : option % 4 == 1 ? accuracy : option % 4 == 2 ? 0 : (settled ? distance * 3 : 0) + accuracy, "physical-trailer-toss");
                    RecordMetric(i, "trebuchet_resolved_downrange_metres", Vector3.Dot(trailer.Body.worldCenterOfMass - release[i], Vector3.ProjectOnPlane(targets[i].position - release[i], Vector3.up).normalized), "attempt=" + (attempts[i] + 1));
                    RecordMetric(i, "trebuchet_clearance_timed_out", engine.ClearanceTimedOut ? 1 : 0, "attempt=" + (attempts[i] + 1));
                    if (settled) RecordMetric(i, "trebuchet_final_target_error_metres", Vector3.Distance(trailer.Body.worldCenterOfMass, targets[i].position), "attempt=" + (attempts[i] + 1));
                    attempts[i]++;
                    if (option % 4 == 3 && attempts[i] < 3)
                    {
                        couplers[i].Release(); engine.ResetForRound(); trailer.ResetForRound(); trucks[i].Prepare(starts[i], rotations[i]);
                        foreach (var part in trucks[i].GetComponentsInChildren<Rigidbody>(true)) part.isKinematic = true;
                        foreach (var part in trailer.GetComponentsInChildren<Rigidbody>(true)) part.isKinematic = true;
                        foreach (var target in GetComponentsInChildren<SiegeTarget>()) if (target.Lane == i) target.ResetTarget();
                        if (ai) assistPower[i] = Mathf.Clamp01(assistPower[i] + (Vector3.Distance(targets[i].position, release[i]) - distance) * .002f);
                        quiet[i] = flightTime[i] = aimAge[i] = swingAge[i] = bestProjectedRange[i] = observedProjectedRange[i] = observedElevation[i] = 0; trajectoryPeak[i] = default; peakReported[i] = false; peakDeclineSeconds[i] = 0; resetAt[i] = Elapsed + 3;
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
        void DriveOffLoadingBridge(int slot, Vector3 destination)
        {
            // Leave the open side of the receiving road before it retracts;
            // continuing straight would drive into the real siege target wall.
            var truck = trucks[slot]; var transmission = truck.tractorMotor.vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            if (transmission.Gear <= 0 && Mathf.Abs(speed) < .4f) transmission.ShiftInto(1);
            Vector3 local = truck.transform.InverseTransformPoint(new Vector3(destination.x, truck.body.position.y, destination.z));
            float steering = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f, -1, 1);
            truck.TickDrive(new Vector2(steering, Mathf.Clamp((2.5f - speed) * .55f, -.7f, .75f)));
        }
        bool ShouldReleaseAI(int slot)
        {
            var body = trailers[slot].Body; Vector3 velocity = body.linearVelocity;
            if (swingAge[slot] < .2f) return false;
            Vector3 direction = Vector3.ProjectOnPlane(targets[slot].position - body.worldCenterOfMass, Vector3.up).normalized;
            float forwardSpeed = Vector3.Dot(velocity, direction);
            float flatSpeed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
            float elevation = Mathf.Atan2(velocity.y, Mathf.Max(.01f, flatSpeed)) * Mathf.Rad2Deg;
            float gravity = Mathf.Max(.1f, Physics.gravity.magnitude), height = Mathf.Max(0, body.worldCenterOfMass.y - targets[slot].position.y);
            float flightSeconds = (velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2 * gravity * height)) / gravity;
            float range = Mathf.Max(0, forwardSpeed) * flightSeconds;
            float previousBest = bestProjectedRange[slot];
            observedProjectedRange[slot] = range; observedElevation[slot] = elevation;
            // AI observes the same physical rising/downrange launch a human
            // can release. Reject static/numerical noise, not a presumed sport
            // performance minimum that this apparatus may never achieve.
            bool risingThrow = forwardSpeed > .05f && velocity.y > .05f && velocity.sqrMagnitude > .25f && !float.IsNaN(range) && !float.IsInfinity(range);
            if (!risingThrow) { peakDeclineSeconds[slot] = 0; return false; }
            if (range > previousBest)
            {
                bestProjectedRange[slot] = range;
                var arm = engines[slot].Arm;
                trajectoryPeak[slot] = new TrajectoryPeak { velocity = velocity, position = body.worldCenterOfMass, range = range, swingSeconds = swingAge[slot], armAngularVelocity = arm.angularVelocity, armInertia = arm.inertiaTensor, motorForce = engines[slot].Pivot.motor.force, armPitch = arm.rotation.eulerAngles.x };
            }
            // The measured positive rising range has begun to decline: release
            // the available real momentum rather than holding the sling forever.
            // No absolute 6m/8m range or assumed launch energy is required here.
            bool declining = previousBest > 0 && range < previousBest * .98f;
            peakDeclineSeconds[slot] = declining ? peakDeclineSeconds[slot] + Time.fixedDeltaTime : 0;
            bool passedPeak = peakDeclineSeconds[slot] >= .06f;
            if (option % 4 == 0) return passedPeak;
            Vector3 predicted = body.worldCenterOfMass + velocity * flightSeconds;
            float miss = Vector3.ProjectOnPlane(predicted - targets[slot].position, Vector3.up).magnitude;
            float tolerance = Mathf.Lerp(35, 12, (int)setup.players[slot].difficulty / 2f);
            // Accuracy modes first seek their real target. If this apparatus
            // cannot reach it, release its best actual rising trajectory rather
            // than manufacture power or wait until the counterweight stops.
            return miss < tolerance || passedPeak;
        }
        public string ReleaseDecisionDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            return "swingSeconds=" + swingAge[slot].ToString("F2") + " velocity=" + trailers[slot].Body.linearVelocity.ToString("F2") + " elevation=" + observedElevation[slot].ToString("F1") + " projectedForwardRange=" + observedProjectedRange[slot].ToString("F2") + " bestProjectedRange=" + bestProjectedRange[slot].ToString("F2") + " peak=[" + MeasuredPeakDiagnostic(slot) + "]";
        }
        public string MeasuredPeakDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            var peak = trajectoryPeak[slot];
            return "range=" + peak.range.ToString("F2") + " velocity=" + peak.velocity.ToString("F2") + " COM=" + peak.position.ToString("F2") + " swingSeconds=" + peak.swingSeconds.ToString("F2") + " armW=" + peak.armAngularVelocity.ToString("F3") + " motorForce=" + peak.motorForce.ToString("F0") + " inertia=" + peak.armInertia.ToString("F0") + " pitch=" + peak.armPitch.ToString("F1") + " assist=" + assistPower[slot].ToString("F2");
        }
        void ReportMeasuredPeak(int slot)
        {
            if (peakReported[slot] || trajectoryPeak[slot].range <= 0) return;
            peakReported[slot] = true;
            string context = "attempt=" + (attempts[slot] + 1);
            Debug.Log("HH10_TREBUCHET_MEASURED_PEAK seat=" + slot + " " + context + " " + MeasuredPeakDiagnostic(slot));
            RecordMetric(slot, "trebuchet_measured_rising_peak_range_metres", trajectoryPeak[slot].range, context);
        }
        readonly Vector3[] cameraSubjects=new Vector3[4];
        readonly bool[] cameraAirborne=new bool[4];
        protected override void Present(float dt)
        {
            // Keep normal shared/split ownership and camera safety. Once the
            // loading bridges retract around the captured load, the actual projectile owns this sport's
            // action framing; the parked loading tractor must not lose the toss.
            for(int slot=0;slot<setup.count;slot++)
            {
                bool loaded=engines!=null&&slot<engines.Length&&engines[slot]!=null&&engines[slot].Load!=null&&(engines[slot].PreparingToSwing||engines[slot].Swinging||engines[slot].Released);
                cameraSubjects[slot]=loaded?trailers[slot].Body.worldCenterOfMass:trucks[slot].body.position;
                cameraAirborne[slot]=loaded&&engines[slot].Released&&!Completed[slot];
            }
            if(views!=null){views.Follow(dt,cameraAirborne,cameraSubjects);return;}
            if(broadcast!=null&&setup.count>0)broadcast.Pose(cameraSubjects[0]+Vector3.up*2,new Vector3(18,18,-28),64);
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
