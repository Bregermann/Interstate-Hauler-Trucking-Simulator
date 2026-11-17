using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class TrebuchetArm : MonoBehaviour
    {
        public Rigidbody Arm;
        public HingeJoint Pivot;
        public BoxCollider LoadingCradle;
        public TrailerBody Load { get; private set; }
        public bool Swinging { get; private set; }
        public bool Released { get; private set; }
        ConfigurableJoint sling;
        public bool SlingIntact => sling != null && Load != null && sling.connectedBody == Load.Body;
        // A negative gap marks an absent constraint, never a measured distance.
        public float SlingAnchorGapMetres => SlingIntact ? Vector3.Distance(Arm.transform.TransformPoint(sling.anchor), Load.Body.transform.TransformPoint(sling.connectedAnchor)) : -1;
        public Vector3 SlingConstraintForce => SlingIntact ? sling.currentForce : Vector3.zero;
        public Vector3 SlingConstraintTorque => SlingIntact ? sling.currentTorque : Vector3.zero;
        public string SlingDiagnostic() => SlingIntact ? "sling=point constraint gap=" + SlingAnchorGapMetres.ToString("F4") + "m force=" + sling.currentForce.ToString("F1") + "N torque=" + sling.currentTorque.ToString("F1") + "Nm armAnchor=" + Arm.transform.TransformPoint(sling.anchor).ToString("F2") + " loadAnchor=" + Load.Body.transform.TransformPoint(sling.connectedAnchor).ToString("F2") + " angular=free" : "sling=absent released=" + Released;
        Vector3 start;
        Quaternion rotation;
        void Awake()
        {
            start = Arm.position; rotation = Arm.rotation;
            StopMechanism();
        }
        public bool CanLoad(TrailerBody trailer)
        {
            if (trailer == null || trailer.Body == null || LoadingCradle == null || Arm == null) return false;
            Vector3 point = trailer.Body.worldCenterOfMass;
            Bounds volume = LoadingCradle.bounds;
            if (!volume.Contains(point)) return false;
            Vector3 downstream = Vector3.ProjectOnPlane(Arm.transform.forward, Vector3.up);
            // First bounds entry is still behind the pivot and leaves the rear
            // of the trailer on the approach ramp. Haul it onto the actual
            // receiving center before closing the sling, so swing lifts it.
            return downstream.sqrMagnitude > .001f && Vector3.Dot(point - volume.center, downstream.normalized) >= -1;
        }
        public bool TryLoad(TrailerBody trailer)
        {
            if (Load != null || trailer == null || trailer.Module.module.attached || !CanLoad(trailer) || trailer.Body.linearVelocity.magnitude > 2) return false;
            // The loading cradle/sling supports this load; native parking legs
            // lowered by OnDetach must not spear that same physical platform.
            if(trailer.Module.module.trailerStand!=null)trailer.Module.module.trailerStand.SetActive(false);
            Load = trailer; sling = Arm.gameObject.AddComponent<ConfigurableJoint>();
            // A physical point sling retains translation at the actual load,
            // while allowing the trailer to rotate independently as its axles
            // leave the ramp. A six-axis weld also tried to rotate the grounded
            // trailer with the arm and could mechanically lock the launch.
            var point=trailer.Body.worldCenterOfMass;sling.autoConfigureConnectedAnchor=false;
            sling.anchor=Arm.transform.InverseTransformPoint(point);sling.connectedBody=trailer.Body;
            sling.connectedAnchor=trailer.Body.transform.InverseTransformPoint(point);
            sling.xMotion = sling.yMotion = sling.zMotion = ConfigurableJointMotion.Locked;
            sling.angularXMotion = sling.angularYMotion = sling.angularZMotion = ConfigurableJointMotion.Free;
            // Let the actual solver carry the load; no corrective projection
            // or motor on the sling may manufacture its trajectory.
            sling.projectionMode = JointProjectionMode.None;
            sling.breakForce=sling.breakTorque=float.PositiveInfinity;
            trailer.Body.solverIterations=32;trailer.Body.solverVelocityIterations=12;
            sling.enableCollision = false; sling.enablePreprocessing = false; trailer.SetBrakes(0); return true;
        }
        public void Aim(float offsetDegrees)
        {
            if (Swinging || !Arm.isKinematic) return;
            // A position-driven loading winch moves the actual locked cradle.
            // The attached dynamic load responds through the real sling joint.
            Arm.MoveRotation(rotation * Quaternion.AngleAxis(Mathf.Clamp(offsetDegrees, -15, 10), Vector3.right));
        }
        public void BeginSwing(float power)
        {
            if (Load == null || Swinging) return;
            Swinging = true; Pivot.useSpring = false;
            Arm.isKinematic = false; Arm.useGravity = true; Arm.WakeUp();
            var motor = Pivot.motor; motor.force = Arm.mass * Mathf.Lerp(50, 120, Mathf.Clamp01(power)); motor.targetVelocity = -65; motor.freeSpin = true;
            Pivot.motor = motor; Pivot.useMotor = true;
        }
        public bool Release()
        {
            if (!Swinging || Released || sling == null) return false;
            Object.Destroy(sling); sling = null; Released = true; return true;
        }
        public void StopMechanism()
        {
            Pivot.useMotor = Pivot.useSpring = Pivot.useLimits = false; Pivot.enablePreprocessing = false;
            if (!Arm.isKinematic) Arm.linearVelocity = Arm.angularVelocity = Vector3.zero;
            Arm.isKinematic = true;
        }
        public void ResetForRound()
        {
            if (sling != null) Object.Destroy(sling); sling = null; Load = null; Swinging = Released = false;
            StopMechanism();
            Arm.interpolation = RigidbodyInterpolation.None;
            Arm.position = start; Arm.rotation = rotation;
            Arm.transform.SetPositionAndRotation(start, rotation);
        }
    }
}
