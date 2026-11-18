using UnityEngine;
using LWS.TruckHitchSports;
using Dnr = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType;

namespace LWS.TruckExtremeHauling
{
    // One rescue load, one operator. Existing Skyhook native bodies/joints lift
    // the actual load; NWH alone accepts and carries the fifth-wheel connection.
    public sealed class ThinIceRecoveryHoist : MonoBehaviour
    {
        public ThinIceRescueObjectives rescue;
        public PhysicalCableCar carrier;
        public SuspendedFreightHook hook;
        public Transform liftingEye;
        public ConfigurableJoint translationRail;
        public bool RailPresented { get; private set; }
        public float SuspendedTravel { get; private set; }
        public float roofHeight = 3.4f;
        public int Owner { get; private set; } = -1;
        public bool Lifted { get; private set; }
        public bool NativeCoupled { get; private set; }
        public enum RecoveryPhase { Idle, AlignHook, Lift, AlignTractor, Translate, ReverseHitch, Hauling }
        public RecoveryPhase Phase { get; private set; }
        const float PresentedAiReverseLaneHalfWidth = 2;
        Dnr[] originalDnr;
        float initialPinHeight;
        Vector3 capturedLoadPosition;
        bool capturedBefore;
        bool active;
        TrailerBody Cargo => rescue.strandedCargo;
        HitchCoupler Coupler(int slot) => rescue.match.trucks[slot].GetComponent<HitchCoupler>();
        public bool AttachedTo(int slot) => Coupler(slot).Attached == Cargo && Coupler(slot).PhysicalConstraintPresent;
        public bool Owns(int slot) => active && Owner == slot && Phase != RecoveryPhase.Idle;
        public bool HoldsForLift => active && Owner >= 0 && Phase != RecoveryPhase.Hauling;
        public bool ParkOperator => Phase == RecoveryPhase.AlignHook || Phase == RecoveryPhase.Lift || Phase == RecoveryPhase.Translate;
        Vector3 EyeLocal => new Vector3(Cargo.Body.centerOfMass.x, Mathf.Max(roofHeight, Cargo.Body.centerOfMass.y + .8f), Cargo.Body.centerOfMass.z);
        public Vector3 Eye => Cargo.transform.TransformPoint(EyeLocal);

        public void BeginRound()
        {
            active = true; Owner = -1; Phase = RecoveryPhase.Idle; Lifted = NativeCoupled = RailPresented = false; SuspendedTravel = 0; capturedBefore = false;
            originalDnr = new Dnr[rescue.match.trucks.Length];
            for (int i = 0; i < originalDnr.Length; i++) originalDnr[i] = rescue.match.trucks[i].tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType;
            carrier.trolley.isKinematic = false; carrier.ResetCable(); carrier.trolley.isKinematic = true; hook.ResetHook();
        }
        void FixedUpdate()
        {
            // A manual release leaves a real trolley with inertia. Apply the
            // existing rail brake while unowned rather than freezing its pose.
            if(active&&Owner<0&&translationRail!=null&&!carrier.trolley.isKinematic)carrier.DriveAlongRail(0,1);
        }
        public void StopRound()
        {
            active = false; ReleaseHook(); hook.body.isKinematic = true; carrier.trolley.isKinematic = true;
            if (originalDnr != null) for (int i = 0; i < originalDnr.Length; i++)
                rescue.match.trucks[i].tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType = originalDnr[i];
            Owner = -1; Phase = RecoveryPhase.Idle;
        }
        public bool TryClaim(int slot)
        {
            if (!active || Owner >= 0 || Cargo.Module.module.attached || Cargo.Body.isKinematic || !Cargo.WheelContact ||
                Cargo.Body.position.z < rescue.incidentIce.transform.position.z + 6 || Vector3.Dot(Cargo.transform.up, Vector3.up) < .8f ||
                Mathf.Abs(Eye.x-carrier.trolley.position.x)>3||Mathf.Abs(Eye.z-carrier.trolley.position.z)>3) return false;
            Owner = slot; Phase = RecoveryPhase.AlignHook; initialPinHeight = Cargo.Kingpin.position.y; return true;
        }
        Vector3 CouplingPosition(int slot)
        {
            var tractor = rescue.match.trucks[slot];
            Vector3 localAnchor = tractor.transform.InverseTransformPoint(Coupler(slot).Anchor.position);
            return Cargo.Kingpin.position - Cargo.transform.rotation * localAnchor;
        }

        public Vector3 TractorDestination(int slot)
        {
            Vector3 aligned = CouplingPosition(slot);
            if (Phase == RecoveryPhase.ReverseHitch) return aligned;
            // Keep the look-ahead on the trailer's centerline while the driver
            // straightens. A fixed parking point can stop a yawed cab in place.
            float ahead = Vector3.Dot(rescue.match.trucks[slot].body.position - aligned, Cargo.transform.forward);
            return aligned + Cargo.transform.forward * Mathf.Clamp(ahead + 12, 25, 48);
        }
        public Vector2 DriveAI(int slot)
        {
            var truck = rescue.match.trucks[slot]; var hitch = Coupler(slot);
            var transmission = truck.tractorMotor.vehicle.powertrain.transmission;
            if (ParkOperator) return Vector2.zero;
            bool reverse = Phase == RecoveryPhase.ReverseHitch;
            transmission.automaticTransmissionDNRShiftType = reverse ? Dnr.RequireShiftInput : originalDnr[slot];
            Vector3 target = TractorDestination(slot), local = truck.transform.InverseTransformPoint(target);
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            if (!reverse)
            {
                float yaw = Mathf.Atan2(local.x, local.z);
                float desired = Mathf.Min(4, Vector3.ProjectOnPlane(target - truck.body.position, Vector3.up).magnitude * .5f) * Mathf.Max(.15f, Mathf.Cos(yaw));
                return new Vector2(Mathf.Clamp(yaw * 1.3f, -1, 1), Mathf.Clamp((desired - speed) * .45f, -.8f, .8f));
            }
            if (transmission.Gear >= 0)
            {
                if (Mathf.Abs(speed) < .35f) transmission.ShiftInto(-1);
                return new Vector2(0, -1);
            }
            float gap = Vector3.Distance(hitch.Anchor.position, Cargo.Kingpin.position);
            float angle = Mathf.Atan2(local.x, Mathf.Max(.2f, -local.z));
            float requested = -Mathf.Clamp(gap * .25f, .12f, 1.8f);
            return new Vector2(Mathf.Clamp(angle * 1.8f, -1, 1), gap < .6f ? 0 : Mathf.Clamp((requested - speed) * .7f, -.75f, .7f));
        }
        public void Step(int slot, bool pressed, bool held, bool ai, float dt)
        {
            if (!Owns(slot)) return;
            var hitch = Coupler(slot);
            if (Phase == RecoveryPhase.Hauling)
            {
                if(pressed&&!ai){hitch.Release();rescue.ReleaseRecoveryCable(slot);Owner=-1;Phase=RecoveryPhase.Idle;return;}
                if (!AttachedTo(slot)) { Owner = -1; Phase = RecoveryPhase.Idle; }
                return;
            }
            if (pressed)
            {
                ReleaseHook(); rescue.ReleaseRecoveryCable(slot); Owner = -1; Phase = RecoveryPhase.Idle;
                rescue.match.trucks[slot].tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType = originalDnr[slot];
                return;
            }
            if (liftingEye != null) liftingEye.localPosition = EyeLocal;
            if (!ai && !held) { if (translationRail != null && !carrier.trolley.isKinematic) carrier.DriveAlongRail(0,1); return; }
            if (translationRail != null && !carrier.trolley.isKinematic)
            {
                float remaining = carrier.routeEnd.z - carrier.trolley.position.z;
                carrier.DriveAlongRail(Phase == RecoveryPhase.Translate || RailPresented ? Mathf.Clamp(remaining * .55f - carrier.trolley.linearVelocity.z * .8f, -.65f, .65f) : 0, 1);
            }
            if (hook.Holding) SuspendedTravel = Mathf.Max(SuspendedTravel, Cargo.Body.worldCenterOfMass.z - capturedLoadPosition.z);
            float targetY = hook.Holding ? Eye.y + hitch.Anchor.position.y - Cargo.Kingpin.position.y : Eye.y;
            float lift = Mathf.Clamp((targetY - hook.body.position.y) * .8f - hook.body.linearVelocity.y * .45f, -.65f, .65f);
            // Before latching, compensate the measured rope force on the400kg
            // hook. Pure position feedback otherwise hangs short of an off-plumb
            // roof eye. After latching, use bounded motor braking and let the
            // actual suspended load settle beneath the active carrier.
            float sway = Mathf.Clamp((hook.Holding?0:(Eye.x-hook.body.position.x)*1.5f-hook.rope.currentForce.x/(hook.body.mass*5))-hook.body.linearVelocity.x*.8f,-1,1);
            hook.Apply(sway, lift, dt);
            // The existing hook controls X sway and rope length. This fixed
            // recovery gantry also permits physical fore/aft hook alignment;
            // only this missing axis is added, with the same5m/s² sway limit.
            float foreAft=Mathf.Clamp((hook.Holding?0:(Eye.z-hook.body.position.z)*1.5f-hook.rope.currentForce.z/(hook.body.mass*5))-hook.body.linearVelocity.z*.8f,-1,1);
            hook.body.AddForce(Vector3.forward*foreAft*5,ForceMode.Acceleration);
            if (Phase == RecoveryPhase.AlignHook && Vector3.Distance(hook.body.position,Eye)<.01f&&
                (hook.body.GetPointVelocity(hook.body.position)-Cargo.Body.GetPointVelocity(Eye)).magnitude<.3f&&hook.TryCapture(Cargo.Body, EyeLocal))
            {
                // Only a real near/velocity-matched native hook capture retires
                // the tractor cable; another player's cable is never stolen.
                rescue.ReleaseRecoveryCable(slot);
                if(!capturedBefore){capturedLoadPosition=Cargo.Body.worldCenterOfMass;capturedBefore=true;}
                Phase = RecoveryPhase.Lift;
            }
            if (!hook.Holding && Phase != RecoveryPhase.AlignHook) { rescue.match.trucks[slot].tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType=originalDnr[slot];Owner = -1; Phase = RecoveryPhase.Idle; return; }
            if (hook.Holding && Cargo.Kingpin.position.y > initialPinHeight + .35f) Lifted = true;
            if (Phase == RecoveryPhase.Lift && Lifted && Mathf.Abs(Cargo.Kingpin.position.y - hitch.Anchor.position.y) < .15f && Cargo.Body.angularVelocity.magnitude < .3f)
                Phase = RecoveryPhase.AlignTractor;
            if (Phase == RecoveryPhase.AlignTractor)
            {
                var truck = rescue.match.trucks[slot]; Vector3 aligned = CouplingPosition(slot);
                float lateral = Mathf.Abs(Vector3.Dot(truck.body.position - aligned, Cargo.transform.right));
                // Once the actual load is presented over level ice, the existing
                // reverse steering can correct a modest cross-track offset. Run94
                // instead kept driving forward for a .45m staging line, then
                // spent the round backing that unnecessary extra distance.
                // This is only AI manoeuvre admission: native capture still
                // independently checks 25mm, velocity and full relative angle.
                float reverseLane = ai && RailPresented ? PresentedAiReverseLaneHalfWidth : .45f;
                if (Vector3.Angle(truck.transform.forward, Cargo.transform.forward) < 5 && lateral < reverseLane && Vector3.Dot(truck.body.position - aligned, Cargo.transform.forward) > (RailPresented ? 5 : 18))
                {
                    // Clear the native tractor ahead BEFORE carrying the suspended
                    // load north. The fixed legacy gantry keeps its original path.
                    if (translationRail != null && !RailPresented)
                    { carrier.trolley.isKinematic = false; Phase = RecoveryPhase.Translate; }
                    else Phase = RecoveryPhase.ReverseHitch;
                }
            }
            if (Phase == RecoveryPhase.Translate &&
                Mathf.Abs(carrier.routeEnd.z - carrier.trolley.position.z) < .25f &&
                SuspendedTravel > 8.5f && Cargo.Kingpin.position.z > rescue.incidentIce.surface.bounds.max.z + 1.5f &&
                carrier.trolley.linearVelocity.magnitude < .25f && Cargo.Body.linearVelocity.magnitude < .35f &&
                Cargo.Body.angularVelocity.magnitude < .25f && Vector3.Dot(Cargo.transform.up,Vector3.up) > .98f &&
                Mathf.Abs(Cargo.Kingpin.position.y - hitch.Anchor.position.y) < .15f)
            { RailPresented = true; Phase = RecoveryPhase.AlignTractor; }
            if ((Phase == RecoveryPhase.AlignTractor || Phase == RecoveryPhase.ReverseHitch) && hitch.GuideCoupling(Cargo, dt))
            {
                NativeCoupled = hitch.PhysicalConstraintPresent;
                if (NativeCoupled)
                {
                    ReleaseHook(); Phase = RecoveryPhase.Hauling;
                    rescue.match.trucks[slot].tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType = originalDnr[slot];
                }
            }
        }
        void ReleaseHook(){
            foreach(var latch in hook.GetComponents<ConfigurableJoint>())if(latch.connectedBody==Cargo.Body){
                latch.xMotion=latch.yMotion=latch.zMotion=ConfigurableJointMotion.Free;latch.connectedBody=null;
            }
            hook.Release();
        }
        public string DiagnosticState() => "hoist=" + Phase + " owner=" + Owner + " holding=" + hook.Holding + " lifted=" + Lifted +
            " nativeCoupled=" + NativeCoupled + " railPresented=" + RailPresented + " suspendedTravel=" + SuspendedTravel.ToString("F2") + " trolley=" + carrier.trolley.position.ToString("F2") + " hook=" + hook.body.position.ToString("F2") + " eye=" + Eye.ToString("F2") +
            (Owner >= 0 ? " " + Coupler(Owner).DiagnosticState(Cargo) : "");
    }
}
