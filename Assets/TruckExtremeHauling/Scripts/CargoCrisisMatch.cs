using System;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckHitchSports;

namespace LWS.TruckExtremeHauling
{
    // Rescue decisions add no transform correction: the cable, native wheels and
    // native fifth wheel do all recovery, approach and transport work.
    public sealed class CargoCrisisMatch : ExtremeHaulingMatch
    {
        public SpringJoint[] winches;
        public Transform[] recoveryEyes;
        public float cableReach = 22, reelSpeed = .85f, uprightThreshold = .97f;
        public Material cableMaterial;
        public CargoEmergencyScenarios emergencySites;
        TrailerBody[] defaultTrailers;
        Transform[] defaultObjectives;
        Vector3[] defaultStarts;
        Quaternion[] defaultRotations;
        enum RescueStage { Approach, Winching, ClearSide, AlignLane, Straighten, ReverseHitch, Tow, Delivered, Failed }
        readonly RescueStage[] stage = new RescueStage[4];
        readonly int[] assigned = new int[4];
        readonly int[] recoveryAttempts = new int[4];
        readonly float[] stable = new float[4], stageTime = new float[4];
        readonly BoxCollider[] floodDryLane = new BoxCollider[4];
        BoxCollider floodNorthernRoad;
        readonly int[] floodTowHitchLosses = new int[4];
        readonly bool[] floodAlignBacking = new bool[4], floodHold = new bool[4];
        readonly LineRenderer[] cables = new LineRenderer[4];
        bool[] deliveredTargets;
        Material ownedCableMaterial;
        public int SharedRescues { get; private set; }
        public string RecoveryPhase(int slot) => stage[slot].ToString();
        public bool CableConnected(int slot) => winches != null && winches[slot] != null && winches[slot].connectedBody != null;
        public bool CooperativeSuccess => Cooperative && deliveredTargets != null && SharedRescues == Mathf.Min(setup.count, deliveredTargets.Length);
        static readonly string[] ScenarioNames =
        {
            "EMERGENCY RESPONSE", "RESCUE RACE", "COOPERATIVE CARGO CRISIS",
            "HIGH-RISK RECOVERY", "INDUSTRIAL FIRE RECOVERY", "FLOODED LOADING RESCUE", "HIGHWAY PILEUP RECOVERY"
        };
        protected override void ReportFinalMeasurements(int slot)
        {
            if (option == 5) RecordMetric(slot, "cargo_flood_actual_tow_hitch_losses", floodTowHitchLosses[slot]);
            RecordMetric(slot, "cargo_recovery_attempts", recoveryAttempts[slot]);
            RecordMetric(slot, "cargo_shared_rescues", SharedRescues);
            var target = Target(slot);
            if (target != null) RecordMetric(slot, "rescued_cargo_condition", target.CargoCondition);
            if (emergencySites?.Active == null) return;
            if (emergencySites.PileupActive)
            {
                RecordMetric(slot, "cargo_pileup_wreck_motion_metres", emergencySites.PileupWreckMetres[slot]);
                return;
            }
            RecordMetric(slot, "cargo_fire_exposure_seconds", emergencySites.HeatSeconds[slot]);
            RecordMetric(slot, "cargo_water_immersion_seconds", emergencySites.ImmersedSeconds[slot]);
            RecordMetric(slot, "cargo_physical_cable_tow_metres", emergencySites.CableTowMetres[slot]);
            RecordMetric(slot, "cargo_flood_extracted", emergencySites.Extracted[slot] ? 1 : 0);
        }
        public override string Title => "CARGO CRISIS";
        public override int OptionCount => emergencySites?.highwayPileup?.environment != null ? ScenarioNames.Length : ScenarioNames.Length - 1;
        protected override string[] Modes => ScenarioNames;
        public override bool Cooperative => option == 2;
        protected override string ActionHint => "HOOK / RELEASE CABLE · HOLD REEL / ALIGN HITCH";
        public override string Status => emergencySites!=null&&emergencySites.Active!=null ? emergencySites.Status : Cooperative ? "TEAM RECOVERY · " + SharedRescues + "/" + Mathf.Min(setup.count, trailers.Length) + " LOADS SAFE · " + ActionHint : "PHYSICAL RECOVERY · " + ActionHint;
        public override string Results => (Cooperative ? (CooperativeSuccess ? "ALL CARGO RECOVERED — TEAM SUCCESS" : "TEAM RECOVERY INCOMPLETE") + "\n" : "") + base.Results;
        public override string Help => "Drive alongside the high recovery eye of a tipped trailer and press ACTION to hook your physical winch cable. Hold ACTION to reel while braking or driving to pull the load upright; press ACTION again to release. A cable transmits force at the trailer roof eye, so truck position matters. Once upright, clear the cable, position ahead of the trailer, straighten and reverse the fifth wheel beneath its kingpin. Hold ACTION near the aligned kingpin to lock the native hitch. Tow to your lit depot and stop the upright load for two seconds. A touch or cable-only arrival never scores. In cooperative play the team must recover every active load; teammates may assist other unclaimed loads, and cooperative success awards no individual winner points. Industrial Fire: visible burning freight damages exposed cargo; extract it quickly and preserve its condition. Flooded Loading: remain on the dry right bank, use the longer recovery cable and physically reel the floating load up the extraction slope. Native wheel support on the dry bank is required before hitching; buoyant or cable-only arrivals cannot qualify. Highway Pileup: work around the disabled vehicles, pull the stranded trailer upright and haul it to your depot. Real wreck movement is tracked separately from cargo delivery.";

        public override bool TryReadScoreCard(int card, out string value, out string detail)
        {
            value = detail = null;
            if (card < 0 || card >= setup.count) return false;
            value = scores[card].ToString("0");
            string phase;
            if (stage[card] == RescueStage.Delivered) phase = "CARGO SAFE";
            else if (stage[card] == RescueStage.Failed) phase = "CARGO LOST";
            else if (CableConnected(card))
                phase = "WINCH · " + (winches[card].currentForce.magnitude / 1000).ToString("0") + " kN";
            else if (hitches != null && hitches[card]?.Attached != null) phase = "HITCH LOCKED";
            else phase = stage[card] switch
            {
                RescueStage.ClearSide or RescueStage.AlignLane or RescueStage.Straighten => "ALIGN HITCH",
                RescueStage.ReverseHitch => "REVERSE TO KINGPIN",
                _ => "APPROACH RECOVERY EYE"
            };
            detail = Cooperative ? "TEAM " + SharedRescues + " SAFE · " + phase : phase;
            if (emergencySites?.Active != null) detail += " · " + emergencySites.PlayerStatus(card);
            return true;
        }

        protected override void BeginGame()
        {
            if(defaultTrailers==null){defaultTrailers=trailers;defaultObjectives=objectives;defaultStarts=(Vector3[])starts.Clone();defaultRotations=(Quaternion[])rotations.Clone();}
            // A round boundary selects an already authored incident. Coupling and rescue
            // never use these setup poses; all active movement remains native physics.
            if(hitches!=null)foreach(var hitch in hitches)hitch?.Release();
            if(winches!=null)for(int i=0;i<winches.Length;i++)if(winches[i]!=null)DetachCable(i);
            if(option>=4&&emergencySites==null)throw new InvalidOperationException("Install the saved Cargo Crisis emergency sites before selecting this scenario.");
            emergencySites?.Select(option);
            var site=emergencySites!=null?emergencySites.Active:null;
            trailers=site!=null?site.loads:defaultTrailers;objectives=site!=null?site.depots:defaultObjectives;
            for(int i=0;i<trucks.Length;i++){starts[i]=site!=null?site.truckStarts[i].position:defaultStarts[i];rotations[i]=site!=null?site.truckStarts[i].rotation:defaultRotations[i];}
            foreach(var trailer in defaultTrailers)trailer.gameObject.SetActive(site==null);
            base.BeginGame();
        }
        protected override void BeginMechanic()
        {
            SharedRescues = 0; deliveredTargets = new bool[trailers.Length];
            if (winches == null || winches.Length != trucks.Length) winches = new SpringJoint[trucks.Length];
            for (int i = 0; i < trucks.Length; i++)
            {
                assigned[i] = Mathf.Min(i, trailers.Length - 1); stage[i] = RescueStage.Approach;
                recoveryAttempts[i] = 0;
                stable[i] = stageTime[i] = 0; DetachCable(i);
                if (i < setup.count) foreach (var body in trailers[assigned[i]].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                if (option == 3) trailers[assigned[i]].AddCargo(trailers[assigned[i]].InitialMass * .4f);
                EnsureCable(i); cables[i].enabled = false;
            }
            emergencySites?.BeginRound();
            floodNorthernRoad = null;
            if (emergencySites != null && emergencySites.FloodActive)
            {
                var road = emergencySites.Active.environment.transform.Find("Dry northern freight haul road");
                if (road != null && Quaternion.Angle(road.rotation, Quaternion.identity) < .1f) floodNorthernRoad = road.GetComponent<BoxCollider>();
            }
            Array.Clear(floodTowHitchLosses, 0, floodTowHitchLosses.Length);
            for (int slot = 0; slot < floodDryLane.Length; slot++)
            {
                floodDryLane[slot] = null; floodAlignBacking[slot] = floodHold[slot] = false;
                if (emergencySites == null || !emergencySites.FloodActive) continue;
                var plate = emergencySites.Active.environment.transform.Find("Dry right-bank recovery service lane " + slot);
                if (plate != null && Quaternion.Angle(plate.rotation, Quaternion.identity) < .1f) floodDryLane[slot] = plate.GetComponent<BoxCollider>();
            }
        }
        float EffectiveCableReach=>emergencySites!=null&&emergencySites.Active!=null?emergencySites.Active.cableReach:cableReach;
        bool HitchReady(int i,TrailerBody target)=>emergencySites==null||emergencySites.Active==null||emergencySites.HitchReady(i,target);
        TrailerBody Target(int i) => assigned[i] >= 0 && assigned[i] < trailers.Length ? trailers[assigned[i]] : null;

        protected override Vector3 CameraFocus(int slot)
        {
            int focus = CameraParticipant(slot);
            Vector3 tractorPosition = trucks[focus].body.position;
            var load = Target(focus);
            if (load == null || !load.gameObject.activeInHierarchy) return tractorPosition;
            // Frame both ends of the winch and hitch without losing the driver's
            // truck when the available cargo is across the recovery yard.
            Vector3 toLoad = load.Body.worldCenterOfMass - tractorPosition;
            return tractorPosition + Vector3.ClampMagnitude(toLoad, 18) * .42f;
        }
        bool Upright(TrailerBody trailer) => Vector3.Dot(trailer.transform.up, Vector3.up) > uprightThreshold;
        Vector3 Eye(TrailerBody trailer)
        {
            int index = Array.IndexOf(trailers, trailer);
            return recoveryEyes != null && index >= 0 && index < recoveryEyes.Length && recoveryEyes[index] != null
                ? recoveryEyes[index].position : trailer.transform.TransformPoint(trailer.Body.centerOfMass + Vector3.up * 2.1f + Vector3.right * (Vector3.Dot(trailer.transform.right,Vector3.up)>=0?.95f:-.95f));
        }
        Vector3 RecoverySide(TrailerBody trailer)
        {
            // Pull opposite the roof lean. Projected local-right flips at a
            // ninety-degree rollover; that would send a retry to the wrong side.
            Vector3 side=-Vector3.ProjectOnPlane(trailer.transform.up,Vector3.up);
            if(side.sqrMagnitude>.01f)return side.normalized;
            return Vector3.ProjectOnPlane(trailer.transform.right,Vector3.up).normalized;
        }
        Vector3 AlignedRoot(int i, TrailerBody trailer)
        {
            Vector3 localHitch = trucks[i].transform.InverseTransformPoint(hitches[i].Anchor.position);
            return trailer.Kingpin.position - Quaternion.LookRotation(Vector3.ProjectOnPlane(trailer.transform.forward, Vector3.up).normalized, Vector3.up) * localHitch;
        }
        protected override Vector3 Destination(int i)
        {
            var target = Target(i); if (target == null) return trucks[i].body.position;
            if (hitches[i].Attached != null) return base.Destination(i);
            Vector3 aligned = AlignedRoot(i, target), forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            switch (stage[i])
            {
                case RescueStage.Approach: return emergencySites!=null&&emergencySites.FloodActive?emergencySites.DryBankApproach(i,target):target.Body.position + RecoverySide(target) * (recoveryAttempts[i] == 0 ? 12 : 17) - forward * 2;
                case RescueStage.ClearSide: return aligned + (emergencySites!=null&&emergencySites.FloodActive?Vector3.right:RecoverySide(target)) * 18 + forward * 12;
                case RescueStage.AlignLane: return aligned + forward * 22;
                case RescueStage.Straighten: return emergencySites != null && emergencySites.FloodActive ? FloodAlignmentTarget(i, target) : aligned + forward * 50;
                case RescueStage.ReverseHitch: return aligned;
                default: return trucks[i].body.position;
            }
        }
        protected override Vector2 AICommand(int i, float dt)
        {
            floodHold[i] = false;
            var target=Target(i);if(target==null||stage[i]==RescueStage.Delivered)return Vector2.zero;
            RecoverUnexpectedFloodTowDetach(i, target);
            if (emergencySites != null && emergencySites.FloodActive && emergencySites.Extracted[i] && hitches[i].Attached == null && (stage[i] == RescueStage.ClearSide || stage[i] == RescueStage.AlignLane || stage[i] == RescueStage.Straighten))
                return FloodAlignmentCommand(i, target);
            var transmission=trucks[i].tractorMotor.vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType=stage[i]==RescueStage.ReverseHitch
                ? NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput
                : NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.Auto;
            if(stage[i]==RescueStage.Winching)return emergencySites!=null&&emergencySites.FrontRecoveryTow(i)&&!emergencySites.Extracted[i]
                ?DriveToward(i,emergencySites.FrontTowDestination(i),4,true):Vector2.zero;
            if (hitches[i].Attached != null)
            {
                Vector3 destination = base.Destination(i);
                bool floodExit = TryFloodTowExit(i, hitches[i].Attached, out var exit);
                if (floodExit) destination = exit;
                float distance = Vector3.ProjectOnPlane(hitches[i].Attached.Body.worldCenterOfMass - objectives[i].position, Vector3.up).magnitude;
                if (distance < 6) return new Vector2(0, -Mathf.Clamp01(trucks[i].body.linearVelocity.magnitude));
                return DriveToward(i, destination, floodExit ? 4 : 8, false);
            }
            if (stage[i] == RescueStage.ReverseHitch)
            {
                Vector3 local = trucks[i].transform.InverseTransformPoint(AlignedRoot(i, target));
                float angle = Mathf.Atan2(local.x, Mathf.Max(.2f, -local.z));
                float speed = Vector3.Dot(trucks[i].body.linearVelocity, trucks[i].transform.forward);
                if(Vector3.Angle(trucks[i].transform.forward,target.transform.forward)>12||Mathf.Abs(Vector3.Dot(trucks[i].body.position-AlignedRoot(i,target),target.transform.right))>1.5f||local.z>2)
                {SetStage(i,RescueStage.ClearSide);return Vector2.zero;}
                if(transmission.Gear>=0)
                {
                    if(Mathf.Abs(speed)<.35f)transmission.ShiftInto(-1);
                    return new Vector2(0,-1);
                }
                float gap=Vector3.Distance(hitches[i].Anchor.position,target.Kingpin.position);
                float requested=-Mathf.Clamp(gap*.25f,.12f,2.5f);
                return new Vector2(Mathf.Clamp(angle*1.8f,-1,1),gap<.6f?0:Mathf.Clamp((requested-speed)*.7f,-.75f,.7f));
            }
            return DriveToward(i, Destination(i), stage[i] == RescueStage.Approach ? 5 : 6, true);
        }
        void RecoverUnexpectedFloodTowDetach(int slot, TrailerBody target)
        {
            if (emergencySites == null || !emergencySites.FloodActive || stage[slot] != RescueStage.Tow || hitches[slot].Attached != null || deliveredTargets[assigned[slot]]) return;
            // Native OnDetach lowers the parking legs. A stale Tow phase with
            // no hitch otherwise targets its own cab position forever.
            floodTowHitchLosses[slot]++;
            Debug.Log("CARGO_FLOOD_ACTUAL_TOW_HITCH_LOSS " + FloodTowDiagnostic(slot));
            target.SetBrakes(1); SetStage(slot, RescueStage.ClearSide);
        }
        bool TryFloodTowExit(int slot, TrailerBody load, out Vector3 destination)
        {
            destination = default;
            if (emergencySites == null || !emergencySites.FloodActive || floodNorthernRoad == null || floodDryLane[slot] == null) return false;
            float roadStart = floodNorthernRoad.bounds.min.z;
            if (load.Body.worldCenterOfMass.z >= roadStart + 4) return false;
            // First haul up the clear inner bank corridor, to the left of the
            // existing crate pile at bankX+15/z-22. Turning toward a far depot
            // alone produced only a shallow avoidance angle at that corner.
            // This is a cab-clearance waypoint, not a scoring position: steer
            // the tractor itself through the open corridor. The articulated
            // load follows physically; final depot targeting remains COM-based.
            destination = new Vector3(emergencySites.Active.dryBankX[slot] + 7, objectives[slot].position.y, roadStart + 12);
            return true;
        }
        public string FloodTowDiagnostic(int slot)
        {
            var target = Target(slot); var vehicle = trucks[slot].tractorMotor.vehicle;
            return "slot=" + slot + " losses=" + floodTowHitchLosses[slot] + " phase=" + stage[slot] + " attached=" + (hitches[slot].Attached != null) + " physicalJoint=" + hitches[slot].PhysicalConstraintPresent + " nativeAttached=" + hitches[slot].Hitch.module.attached + " reason=" + hitches[slot].LastReason + " gear=" + vehicle.powertrain.transmission.Gear + " engine=" + vehicle.powertrain.engine.IsRunning + " swappedThrottle=" + vehicle.input.InputSwappedThrottle.ToString("F2") + " tractor=" + trucks[slot].body.position.ToString("F2") + " cargo=" + (target != null ? target.Body.position.ToString("F2") : "none");
        }
        bool FloodLaneInterval(int slot, TrailerBody target, out Bounds safe, out float low, out float high)
        {
            safe = default; low = 0; high = float.PositiveInfinity;
            var plate = floodDryLane[slot];
            if (plate == null || !plate.enabled || !plate.gameObject.activeInHierarchy) return false;
            safe = plate.bounds;
            // The bank joins the existing northern road edge-to-edge. Extend
            // only Z within this bank's unchanged X strip; never use the whole
            // northern road AABB across the neighboring flooded bays.
            if (floodNorthernRoad != null && floodNorthernRoad.enabled && floodNorthernRoad.gameObject.activeInHierarchy)
            {
                Bounds road = floodNorthernRoad.bounds;
                if (road.min.x <= safe.min.x && road.max.x >= safe.max.x && road.min.z <= safe.max.z + .02f && road.max.z > safe.max.z && Mathf.Abs(road.max.y - safe.max.y) < .02f)
                    safe.SetMinMax(safe.min, new Vector3(safe.max.x, safe.max.y, road.max.z));
            }
            float hullMargin = trucks[slot].shape != null ? Mathf.Max(4, trucks[slot].shape.bounds.extents.magnitude + 1) : 4;
            safe.Expand(new Vector3(-hullMargin * 2, 0, -hullMargin * 2));
            Vector3 root = AlignedRoot(slot, target), forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            if (!IntersectLaneAxis(root.x, forward.x, safe.min.x, safe.max.x, ref low, ref high) || !IntersectLaneAxis(root.z, forward.z, safe.min.z, safe.max.z, ref low, ref high)) return false;
            return high > Mathf.Max(low, 20) && high - low > 10;
        }
        static bool IntersectLaneAxis(float origin, float direction, float min, float max, ref float low, ref float high)
        {
            if (Mathf.Abs(direction) < .0001f) return origin >= min && origin <= max;
            float a = (min - origin) / direction, b = (max - origin) / direction;
            low = Mathf.Max(low, Mathf.Min(a, b)); high = Mathf.Min(high, Mathf.Max(a, b));
            return high > low;
        }
        Vector3 FloodAlignmentTarget(int slot, TrailerBody target)
        {
            if (!FloodLaneInterval(slot, target, out _, out float low, out float high)) return trucks[slot].body.position;
            Vector3 root = AlignedRoot(slot, target), forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            float ahead = Vector3.Dot(trucks[slot].body.position - root, forward);
            float minimum = Mathf.Max(8, low + 2), maximum = high - 2;
            return root + forward * (floodAlignBacking[slot] ? minimum : Mathf.Min(maximum, ahead + 8));
        }
        Vector2 FloodAlignmentCommand(int slot, TrailerBody target)
        {
            // The recovered load is already on real dry wheels. Its71degree
            // yaw leaves only~31m of usable ahead lane before the neighboring
            // flooded bay; the generic50m target sent the cab off its road.
            if (!FloodLaneInterval(slot, target, out var safe, out float low, out float high))
            { floodHold[slot] = true; return Vector2.zero; }
            var truck = trucks[slot]; var vehicle = truck.tractorMotor.vehicle;
            Vector3 root = AlignedRoot(slot, target), forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward), offset = truck.body.position - root;
            float ahead = Vector3.Dot(offset, forward), lateral = Vector3.Dot(offset, right);
            float minimum = Mathf.Max(8, low + 2), maximum = high - 2;
            if (stage[slot] != RescueStage.Straighten)
            {
                // Front towing already leaves the cab ahead of the kingpin.
                // Replace the outward U-turn with a genuine supported S-align.
                SetStage(slot, RescueStage.Straighten);
                floodAlignBacking[slot] = ahead > maximum - 4;
            }
            if (ahead > maximum - 1) floodAlignBacking[slot] = true;
            if (ahead < minimum + 1) floodAlignBacking[slot] = false;
            bool reverse = floodAlignBacking[slot];
            var transmission = vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            if (reverse ? transmission.Gear >= 0 : transmission.Gear <= 0)
            {
                if (Mathf.Abs(speed) < .3f) transmission.ShiftInto(reverse ? -1 : 1);
                floodHold[slot] = true; return Vector2.zero;
            }
            float steer;
            if (reverse)
            {
                float lookahead = Mathf.Clamp(Mathf.Abs(ahead - minimum), 4, 12);
                Vector3 desired = Quaternion.AngleAxis(Mathf.Atan2(lateral, lookahead) * Mathf.Rad2Deg, Vector3.up) * forward;
                steer = Mathf.Clamp(-Vector3.SignedAngle(truck.transform.forward, desired, Vector3.up) * .07f + truck.body.angularVelocity.y * .6f, -.65f, .65f);
            }
            else
            {
                Vector3 goal = root + forward * Mathf.Min(maximum, ahead + 8);
                // The reference line itself is clipped to the actual inset
                // support bounds. No point in a neighboring wet bay is pursued.
                goal.x = Mathf.Clamp(goal.x, safe.min.x, safe.max.x); goal.z = Mathf.Clamp(goal.z, safe.min.z, safe.max.z);
                Vector3 local = truck.transform.InverseTransformPoint(goal);
                steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f - truck.body.angularVelocity.y * .3f, -.65f, .65f);
            }
            float available = reverse ? ahead - minimum : maximum - ahead;
            float requested = Mathf.Min(2, Mathf.Sqrt(2 * 1.5f * Mathf.Max(0, available))) * (reverse ? -1 : 1);
            float effort = Mathf.Clamp((requested - speed) * .7f, -.7f, .7f);
            if (!vehicle.powertrain.engine.IsRunning && available > .5f) effort = reverse ? -.35f : .35f;
            return new Vector2(steer, effort);
        }
        public string FloodAlignmentDiagnostic(int slot)
        {
            var target = Target(slot);
            if (target == null || !FloodLaneInterval(slot, target, out var safe, out float low, out float high)) return "no recognized supported flood alignment lane";
            Vector3 root = AlignedRoot(slot, target), forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            return "safeBank=" + safe + " laneInterval=" + low.ToString("F2") + ".." + high.ToString("F2") + " ahead=" + Vector3.Dot(trucks[slot].body.position - root, forward).ToString("F2") + " lateral=" + Vector3.Dot(trucks[slot].body.position - root, Vector3.Cross(Vector3.up, forward)).ToString("F2") + " backing=" + floodAlignBacking[slot] + " target=" + FloodAlignmentTarget(slot, target).ToString("F2");
        }
        Vector2 DriveToward(int i, Vector3 destination, float speedLimit, bool slowArrival)
        {
            Vector3 local = trucks[i].transform.InverseTransformPoint(destination);
            float distance = Vector3.ProjectOnPlane(destination - trucks[i].body.position, Vector3.up).magnitude;
            float angle = Mathf.Atan2(local.x, local.z);
            float speed = Vector3.Dot(trucks[i].body.linearVelocity, trucks[i].transform.forward);
            float requested = speedLimit * Mathf.Lerp(.35f, 1, 1 - Mathf.Clamp01(Mathf.Abs(angle)));
            if (slowArrival) requested = Mathf.Min(requested, distance * .6f);
            return new Vector2(Mathf.Clamp(angle * 1.6f, -1, 1), Mathf.Clamp((requested - speed) * .35f, -.8f, .8f));
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            if (winches[i] == null) DetachCable(i);
            stageTime[i] += dt;
            if(ai && stage[i]==RescueStage.Winching && winches[i].connectedBody==null)
            { recoveryAttempts[i]++;SetStage(i,RescueStage.Approach); }
            if (Cooperative && hitches[i].Attached == null && winches[i].connectedBody == null)
            {
                if (assigned[i] < 0 || deliveredTargets[assigned[i]] || Target(i).Module.module.attached)
                { ChooseTarget(i); SetStage(i, RescueStage.Approach); }
                if (!ai && actions[i]) ChooseTarget(i);
            }
            var target = Target(i); if (target == null) { if (ai) Park(i); return; }
            RecoverUnexpectedFloodTowDetach(i, target);
            if (hitches[i].Attached != null && hitches[i].Attached != target)
            { assigned[i] = Array.IndexOf(trailers, hitches[i].Attached); target = Target(i); }
            if(emergencySites!=null&&emergencySites.Active!=null)
            {
                emergencySites.Step(i,target,winches[i],dt);
                if(target.CargoCondition<=0){DetachCable(i);hitches[i].Release();SetStage(i,RescueStage.Failed);retired[i]=true;return;}
            }
            float eyeDistance = Vector3.Distance(trucks[i].transform.TransformPoint(winches[i].anchor), Eye(target));
            bool tipped = !Upright(target);
            if (ai && stage[i] == RescueStage.Approach)
            {
                if (!tipped&&HitchReady(i,target)) SetStage(i, RescueStage.ClearSide);
                else if (Vector3.ProjectOnPlane(trucks[i].body.position - Destination(i), Vector3.up).magnitude < 3 && trucks[i].body.linearVelocity.magnitude < 1.5f)
                { HookCable(i, target); SetStage(i, RescueStage.Winching); }
            }
            if (!ai && actions[i] && hitches[i].Attached == null)
            {
                if (winches[i].connectedBody != null) { emergencySites?.ParkExtractedLandingGear(i,target);DetachCable(i); }
                else if (!tipped && HitchReady(i,target) && Vector3.Distance(hitches[i].Anchor.position, target.Kingpin.position) < .65f) hitches[i].GuideCoupling(target, dt);
                else if (eyeDistance < EffectiveCableReach) HookCable(i, target);
            }
            if (winches[i].connectedBody != null)
            {
                if(emergencySites!=null&&emergencySites.FloodActive&&!emergencySites.FrontRecoveryTow(i)&&!tipped&&(ai||ActionHeld(i)))
                {
                    // Stop pulling the roof as soon as it is actually upright.
                    // A roof cable kept until bank arrival simply rolls a floating
                    // trailer onto its OTHER side. Re-hook the existing joint at
                    // the real native front kingpin and use actual wheel towing.
                    Vector3 frontEye=target.Kingpin.position;
                    if(Vector3.Distance(trucks[i].transform.TransformPoint(winches[i].anchor),frontEye)<=EffectiveCableReach)
                    {ConfigureRecoveryCable(winches[i],trucks[i].body,target.Body,frontEye,EffectiveCableReach);emergencySites.SetFloodTowPhase(i,target,true);stable[i]=0;}
                }
                if (ai || ActionHeld(i))
                    winches[i].maxDistance = Mathf.Max(2, winches[i].maxDistance - reelSpeed * dt * (option == 3 ? .8f : 1));
                if (ai&&!(emergencySites!=null&&emergencySites.FrontRecoveryTow(i)&&!emergencySites.Extracted[i])) Park(i);
                stable[i] = !tipped && HitchReady(i,target) && target.Body.angularVelocity.magnitude < .65f ? stable[i] + dt : 0;
                if (ai && stable[i] > 1) { emergencySites?.ParkExtractedLandingGear(i,target);DetachCable(i); target.SetBrakes(1); SetStage(i, RescueStage.ClearSide); }
                else if (ai && stageTime[i] > 25 && tipped)
                { DetachCable(i); recoveryAttempts[i]++; SetStage(i, RescueStage.Approach); }
            }
            if (ai && stage[i] == RescueStage.ClearSide && Near(i, Destination(i), 5)) SetStage(i, RescueStage.AlignLane);
            else if (ai && stage[i] == RescueStage.AlignLane && Near(i, Destination(i), 6)) SetStage(i, RescueStage.Straighten);
            else if (ai && stage[i] == RescueStage.Straighten)
            {
                Vector3 aligned = AlignedRoot(i, target);
                float lateral = Mathf.Abs(Vector3.Dot(trucks[i].body.position - aligned, target.transform.right));
                float heading = Vector3.Angle(trucks[i].transform.forward, target.transform.forward);
                if (heading < 5 && lateral < .45f && Vector3.Dot(trucks[i].body.position - aligned, target.transform.forward) > 18)
                    SetStage(i, RescueStage.ReverseHitch);
            }
            if (hitches[i].Attached == null && !tipped && HitchReady(i,target) && (ai && stage[i] == RescueStage.ReverseHitch || !ai && ActionHeld(i)))
            {
                if (hitches[i].Hitch.module.vehicleController != null && target.Vehicle.IsInitialized && hitches[i].GuideCoupling(target, dt))
                { DetachCable(i); SetStage(i, RescueStage.Tow); }
            }
            if (ai && stage[i] == RescueStage.ReverseHitch && stageTime[i] > 35) SetStage(i, RescueStage.ClearSide);
            if (hitches[i].Attached != null)
            {
                DetachCable(i); stage[i] = RescueStage.Tow;
                bool inDepot = AtRestIn(i, target.Body, objectives[i]) && Upright(target) && trucks[i].body.linearVelocity.magnitude < 1 && (emergencySites==null||emergencySites.Active==null||emergencySites.DeliveryReady(i,target));
                if (ai && Vector3.ProjectOnPlane(target.Body.worldCenterOfMass - objectives[i].position, Vector3.up).magnitude < 6) Park(i);
                stable[i] = inDepot ? stable[i] + dt : 0;
                if (stable[i] >= 2 && !deliveredTargets[assigned[i]])
                {
                    deliveredTargets[assigned[i]] = true; SharedRescues++;
                    Award(i, option==1 ? 500 + Mathf.Max(0,matchSeconds-age)*15 : (option>=3?2000:1000) * target.CargoCondition + Mathf.Max(0, matchSeconds - age) * 3, "cargo_rescued");
                    hitches[i].Release(); target.SetBrakes(1); SetStage(i, RescueStage.Delivered);
                    if (!Cooperative) retired[i] = true;
                    else if (CooperativeSuccess) for (int slot = 0; slot < setup.count; slot++) retired[slot] = true;
                    else { ChooseTarget(i); SetStage(i, RescueStage.Approach); }
                }
            }
            UpdateCable(i, target);
            if (ai && floodHold[i]) Park(i);
        }
        bool Near(int i, Vector3 target, float radius) => Vector3.ProjectOnPlane(trucks[i].body.position - target, Vector3.up).magnitude < radius;
        void Park(int i)
        {
            trucks[i].TickDrive(Vector2.zero);
            var input = trucks[i].tractorMotor.vehicle.input;
            input.states.handbrake = input.states.handbrakeRaw = 1;
            if(emergencySites!=null&&emergencySites.Active!=null)
            {
                // Native AutoDNR interprets a held brake axis at standstill as
                // reverse. A recovery-site parked truck uses its actual handbrake.
                input.states.throttle=input.states.throttleRaw=0;
                input.states.brakes=input.states.brakesRaw=0;
            }
            else input.states.brakes = input.states.brakesRaw = 1;
        }
        void SetStage(int i, RescueStage next)
        {
            if (stage[i] != next) SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "cargo_recovery_stage", setup.players[i].participantId, next.ToString(), assigned[i]);
            stage[i] = next; stageTime[i] = stable[i] = 0;
        }
        void ChooseTarget(int i)
        {
            int nearest = -1; float distance = float.MaxValue;
            for (int t = 0; t < Mathf.Min(setup.count, trailers.Length); t++)
            {
                if (deliveredTargets[t] || trailers[t].Module.module.attached) continue;
                float candidate = (Eye(trailers[t]) - trucks[i].body.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; nearest = t; }
            }
            assigned[i] = nearest;
        }
        void HookCable(int i, TrailerBody target)
        {
            var winch = winches[i];
            bool frontTow=emergencySites!=null&&emergencySites.FloodActive&&Upright(target);
            Vector3 eye = frontTow?target.Kingpin.position:Eye(target), anchor = trucks[i].transform.TransformPoint(winch.anchor);
            if (target.Module.module.attached || Vector3.Distance(eye, anchor) > EffectiveCableReach) return;
            ConfigureRecoveryCable(winch,trucks[i].body,target.Body,eye,EffectiveCableReach);
            if(emergencySites!=null&&emergencySites.FloodActive)emergencySites.SetFloodTowPhase(i,target,frontTow);
            target.SetBrakes(0); stable[i] = 0;
        }
        // One cable tuning authority for Cargo Crisis and the thin-ice rescue adapter.
        // Callers own the meaningful hook action and distance/ownership checks.
        public static void ConfigureRecoveryCable(SpringJoint joint,Rigidbody tractor,Rigidbody load,Vector3 worldEye,float reach)
        {
            if(joint==null||tractor==null||load==null)throw new ArgumentNullException("A physical recovery joint, tractor and cargo body are required.");
            Vector3 anchor=tractor.transform.TransformPoint(joint.anchor);
            float distance=Vector3.Distance(anchor,worldEye);
            if(distance>reach)throw new InvalidOperationException("The cargo eye is outside the recovery cable reach.");
            joint.autoConfigureConnectedAnchor=false; joint.connectedBody=load;
            joint.connectedAnchor=load.transform.InverseTransformPoint(worldEye);
            float mass=Mathf.Min(load.mass,tractor.mass);
            joint.spring=mass*10; joint.damper=Mathf.Sqrt(joint.spring*mass)*1.4f;
            joint.minDistance=0; joint.maxDistance=Mathf.Max(2,distance-.1f);
            joint.breakForce=mass*80; joint.enableCollision=true; joint.tolerance=.05f;
        }
        void DetachCable(int i)
        {
            if (winches[i] == null)
            {
                winches[i] = trucks[i].gameObject.AddComponent<SpringJoint>();
                winches[i].anchor = trucks[i].transform.InverseTransformPoint(hitches[i].Anchor.position) + Vector3.up;
            }
            winches[i].spring = winches[i].damper = 0; winches[i].connectedBody = null;
            winches[i].autoConfigureConnectedAnchor = false; winches[i].connectedAnchor = trucks[i].transform.TransformPoint(winches[i].anchor);
            if (cables[i] != null) cables[i].enabled = false;
        }
        void EnsureCable(int i)
        {
            if (cables[i] != null) return;
            var visual = new GameObject("Physical recovery cable " + (i + 1)); visual.transform.SetParent(transform, false);
            var line = cables[i] = visual.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = 12; line.widthMultiplier = .055f;
            if (cableMaterial == null && ownedCableMaterial == null)
            { ownedCableMaterial = new Material(trucks[i].paint.sharedMaterial); ownedCableMaterial.color = new Color(.9f, .64f, .14f); }
            line.sharedMaterial = cableMaterial != null ? cableMaterial : ownedCableMaterial;
        }
        void UpdateCable(int i, TrailerBody target)
        {
            if (cables[i] == null) return; var winch = winches[i]; cables[i].enabled = winch.connectedBody != null;
            if (!cables[i].enabled) return;
            Vector3 a = trucks[i].transform.TransformPoint(winch.anchor), b = target.transform.TransformPoint(winch.connectedAnchor);
            float slack = Mathf.Max(0, winch.maxDistance - Vector3.Distance(a, b));
            for (int point = 0; point < 12; point++)
            { float t = point / 11f; cables[i].SetPosition(point, Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * Mathf.Min(.7f, slack * .35f)); }
        }
        protected override void StopGame()
        { if (winches != null) for (int i = 0; i < winches.Length; i++) if (winches[i] != null) DetachCable(i); base.StopGame();emergencySites?.StopRound(); }
        protected override void OnDestroy()
        { if (ownedCableMaterial != null) Destroy(ownedCableMaterial); base.OnDestroy(); }
    }
}
