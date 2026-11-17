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
        enum RescueStage { Approach, Winching, ClearSide, AlignLane, Straighten, ReverseHitch, Tow, Delivered }
        readonly RescueStage[] stage = new RescueStage[4];
        readonly int[] assigned = new int[4];
        readonly int[] recoveryAttempts = new int[4];
        readonly float[] stable = new float[4], stageTime = new float[4];
        readonly LineRenderer[] cables = new LineRenderer[4];
        bool[] deliveredTargets;
        Material ownedCableMaterial;
        public int SharedRescues { get; private set; }
        public string RecoveryPhase(int slot) => stage[slot].ToString();
        public bool CableConnected(int slot) => winches != null && winches[slot] != null && winches[slot].connectedBody != null;
        public bool CooperativeSuccess => Cooperative && deliveredTargets != null && SharedRescues == Mathf.Min(setup.count, deliveredTargets.Length);
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"cargo_recovery_attempts",recoveryAttempts[i]);RecordMetric(i,"cargo_shared_rescues",SharedRescues);var target=Target(i);if(target!=null)RecordMetric(i,"rescued_cargo_condition",target.CargoCondition); }
        public override string Title => "CARGO CRISIS";
        protected override string[] Modes => new[] { "EMERGENCY RESPONSE", "RESCUE RACE", "COOPERATIVE CARGO CRISIS", "HIGH-RISK RECOVERY" };
        public override bool Cooperative => option == 2;
        protected override string ActionHint => "HOOK / RELEASE CABLE · HOLD REEL / ALIGN HITCH";
        public override string Status => Cooperative ? "TEAM RECOVERY · " + SharedRescues + "/" + Mathf.Min(setup.count, trailers.Length) + " LOADS SAFE · " + ActionHint : "PHYSICAL RECOVERY · " + ActionHint;
        public override string Results => (Cooperative ? (CooperativeSuccess ? "ALL CARGO RECOVERED — TEAM SUCCESS" : "TEAM RECOVERY INCOMPLETE") + "\n" : "") + base.Results;
        public override string Help => "Drive alongside the high recovery eye of a tipped trailer and press ACTION to hook your physical winch cable. Hold ACTION to reel while braking or driving to pull the load upright; press ACTION again to release. A cable transmits force at the trailer roof eye, so truck position matters. Once upright, clear the cable, position ahead of the trailer, straighten and reverse the fifth wheel beneath its kingpin. Hold ACTION near the aligned kingpin to lock the native hitch. Tow to your lit depot and stop the upright load for two seconds. A touch or cable-only arrival never scores. In cooperative play the team must recover every active load; teammates may assist other unclaimed loads, and cooperative success awards no individual winner points.";

        public override bool TryReadScoreCard(int card, out string value, out string detail)
        {
            value=detail=null;if(card<0||card>=setup.count)return false;
            value=scores[card].ToString("0");detail=(Cooperative?"TEAM "+SharedRescues+" SAFE · ":"")+stage[card].ToString().ToUpperInvariant();return true;
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
        }
        TrailerBody Target(int i) => assigned[i] >= 0 && assigned[i] < trailers.Length ? trailers[assigned[i]] : null;
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
                case RescueStage.Approach: return target.Body.position + RecoverySide(target) * (recoveryAttempts[i] == 0 ? 12 : 17) - forward * 2;
                case RescueStage.ClearSide: return aligned + RecoverySide(target) * 18 + forward * 12;
                case RescueStage.AlignLane: return aligned + forward * 22;
                case RescueStage.Straighten: return aligned + forward * 50;
                case RescueStage.ReverseHitch: return aligned;
                default: return trucks[i].body.position;
            }
        }
        protected override Vector2 AICommand(int i, float dt)
        {
            var target=Target(i);if(target==null||stage[i]==RescueStage.Delivered)return Vector2.zero;
            var transmission=trucks[i].tractorMotor.vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType=stage[i]==RescueStage.ReverseHitch
                ? NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput
                : NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.Auto;
            if(stage[i]==RescueStage.Winching)return Vector2.zero;
            if (hitches[i].Attached != null)
            {
                Vector3 destination = base.Destination(i);
                float distance = Vector3.ProjectOnPlane(hitches[i].Attached.Body.worldCenterOfMass - objectives[i].position, Vector3.up).magnitude;
                if (distance < 6) return new Vector2(0, -Mathf.Clamp01(trucks[i].body.linearVelocity.magnitude));
                return DriveToward(i, destination, 8, false);
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
            if (hitches[i].Attached != null && hitches[i].Attached != target)
            { assigned[i] = Array.IndexOf(trailers, hitches[i].Attached); target = Target(i); }
            float eyeDistance = Vector3.Distance(trucks[i].transform.TransformPoint(winches[i].anchor), Eye(target));
            bool tipped = !Upright(target);
            if (ai && stage[i] == RescueStage.Approach)
            {
                if (!tipped) SetStage(i, RescueStage.ClearSide);
                else if (Vector3.ProjectOnPlane(trucks[i].body.position - Destination(i), Vector3.up).magnitude < 3 && trucks[i].body.linearVelocity.magnitude < 1.5f)
                { HookCable(i, target); SetStage(i, RescueStage.Winching); }
            }
            if (!ai && actions[i] && hitches[i].Attached == null)
            {
                if (winches[i].connectedBody != null) DetachCable(i);
                else if (!tipped && Vector3.Distance(hitches[i].Anchor.position, target.Kingpin.position) < .65f) hitches[i].GuideCoupling(target, dt);
                else if (eyeDistance < cableReach) HookCable(i, target);
            }
            if (winches[i].connectedBody != null)
            {
                if (ai || ActionHeld(i))
                    winches[i].maxDistance = Mathf.Max(2, winches[i].maxDistance - reelSpeed * dt * (option == 3 ? .8f : 1));
                if (ai) Park(i);
                stable[i] = !tipped && target.Body.angularVelocity.magnitude < .65f ? stable[i] + dt : 0;
                if (ai && stable[i] > 1) { DetachCable(i); target.SetBrakes(1); SetStage(i, RescueStage.ClearSide); }
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
            if (hitches[i].Attached == null && !tipped && (ai && stage[i] == RescueStage.ReverseHitch || !ai && ActionHeld(i)))
            {
                if (hitches[i].Hitch.module.vehicleController != null && target.Vehicle.IsInitialized && hitches[i].GuideCoupling(target, dt))
                { DetachCable(i); SetStage(i, RescueStage.Tow); }
            }
            if (ai && stage[i] == RescueStage.ReverseHitch && stageTime[i] > 35) SetStage(i, RescueStage.ClearSide);
            if (hitches[i].Attached != null)
            {
                DetachCable(i); stage[i] = RescueStage.Tow;
                bool inDepot = AtRestIn(i, target.Body, objectives[i]) && Upright(target) && trucks[i].body.linearVelocity.magnitude < 1;
                if (ai && Vector3.ProjectOnPlane(target.Body.worldCenterOfMass - objectives[i].position, Vector3.up).magnitude < 6) Park(i);
                stable[i] = inDepot ? stable[i] + dt : 0;
                if (stable[i] >= 2 && !deliveredTargets[assigned[i]])
                {
                    deliveredTargets[assigned[i]] = true; SharedRescues++;
                    Award(i, option==1 ? 500 + Mathf.Max(0,matchSeconds-age)*15 : (option==3?2000:1000) * target.CargoCondition + Mathf.Max(0, matchSeconds - age) * 3, "cargo_rescued");
                    hitches[i].Release(); target.SetBrakes(1); SetStage(i, RescueStage.Delivered);
                    if (!Cooperative) retired[i] = true;
                    else if (CooperativeSuccess) for (int slot = 0; slot < setup.count; slot++) retired[slot] = true;
                    else { ChooseTarget(i); SetStage(i, RescueStage.Approach); }
                }
            }
            UpdateCable(i, target);
        }
        bool Near(int i, Vector3 target, float radius) => Vector3.ProjectOnPlane(trucks[i].body.position - target, Vector3.up).magnitude < radius;
        void Park(int i)
        {
            trucks[i].TickDrive(Vector2.zero);
            var input = trucks[i].tractorMotor.vehicle.input;
            input.states.handbrake = input.states.handbrakeRaw = 1;
            input.states.brakes = input.states.brakesRaw = 1;
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
            Vector3 eye = Eye(target), anchor = trucks[i].transform.TransformPoint(winch.anchor);
            if (target.Module.module.attached || Vector3.Distance(eye, anchor) > cableReach) return;
            winch.autoConfigureConnectedAnchor = false; winch.connectedBody = target.Body;
            winch.connectedAnchor = target.transform.InverseTransformPoint(eye);
            float mass = Mathf.Min(target.Body.mass, trucks[i].body.mass);
            winch.spring = mass * 10; winch.damper = Mathf.Sqrt(winch.spring * mass) * 1.4f;
            winch.minDistance = 0; winch.maxDistance = Mathf.Max(2, Vector3.Distance(anchor, eye) - .1f);
            winch.breakForce = mass * 80; winch.enableCollision = true; winch.tolerance = .05f;
            target.SetBrakes(0); stable[i] = 0;
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
        { if (winches != null) for (int i = 0; i < winches.Length; i++) if (winches[i] != null) DetachCable(i); base.StopGame(); }
        protected override void OnDestroy()
        { if (ownedCableMaterial != null) Destroy(ownedCableMaterial); base.OnDestroy(); }
    }
}
