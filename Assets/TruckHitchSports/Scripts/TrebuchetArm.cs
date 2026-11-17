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
        void Awake() { start = Arm.position; rotation = Arm.rotation; }
        public bool TryLoad(TrailerBody trailer)
        {
            if (Load != null || trailer == null || trailer.Module.module.attached || !LoadingCradle.bounds.Contains(trailer.Body.worldCenterOfMass) || trailer.Body.linearVelocity.magnitude > 2) return false;
            Load = trailer; sling = Arm.gameObject.AddComponent<FixedJoint>(); sling.connectedBody = trailer.Body;
            sling.breakForce = float.PositiveInfinity; sling.enableCollision = false; trailer.SetBrakes(0); return true;
        }
        public void BeginSwing(float power)
        {
            if (Load == null || Swinging) return;
            Swinging = true; Pivot.useSpring = false;
            var motor = Pivot.motor; motor.force = Arm.mass * Mathf.Lerp(10, 30, Mathf.Clamp01(power)); motor.targetVelocity = -65; motor.freeSpin = true;
            Pivot.motor = motor; Pivot.useMotor = true;
        }
        public bool Release()
        {
            if (!Swinging || Released || sling == null) return false;
            Object.Destroy(sling); sling = null; Released = true; return true;
        }
        public void ResetForRound()
        {
            if (sling != null) Object.Destroy(sling); sling = null; Load = null; Swinging = Released = false;
            Arm.position = start; Arm.rotation = rotation; Arm.linearVelocity = Arm.angularVelocity = Vector3.zero;
            Pivot.useMotor = false; Pivot.useSpring = true;
        }
    }
}
