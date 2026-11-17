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
        FixedJoint sling;
        Vector3 start;
        Quaternion rotation;
        void Awake()
        {
            start = Arm.position; rotation = Arm.rotation;
            StopMechanism();
        }
        public bool TryLoad(TrailerBody trailer)
        {
            if (Load != null || trailer == null || trailer.Module.module.attached || !LoadingCradle.bounds.Contains(trailer.Body.worldCenterOfMass) || trailer.Body.linearVelocity.magnitude > 2) return false;
            // The loading cradle/sling supports this load; native parking legs
            // lowered by OnDetach must not spear that same physical platform.
            if(trailer.Module.module.trailerStand!=null)trailer.Module.module.trailerStand.SetActive(false);
            Load = trailer; sling = Arm.gameObject.AddComponent<FixedJoint>();
            // A commanded siege latch stays closed until the release input;
            // its break threshold is not an applied force. Match both anchors
            // at the actual suspended mass, avoiding an off-centre virtual lever.
            var point=trailer.Body.worldCenterOfMass;sling.autoConfigureConnectedAnchor=false;
            sling.anchor=Arm.transform.InverseTransformPoint(point);sling.connectedBody=trailer.Body;
            sling.connectedAnchor=trailer.Body.transform.InverseTransformPoint(point);
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
