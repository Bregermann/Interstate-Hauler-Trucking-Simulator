using UnityEngine;
using NWH.VehiclePhysics2.Modules.Trailer;

namespace LWS.TruckHitchSports
{
    [DisallowMultipleComponent]
    public sealed class HitchCoupler : MonoBehaviour
    {
        public TrailerHitchModuleWrapper Hitch;
        public Rigidbody Body;
        public int Participant;
        [Tooltip("Strict physical capture envelope. NWH owns the joint; no approach teleporting.")]
        public float CaptureDistance = .025f, MaximumAngle = 8, MaximumRelativeSpeed = .8f;
        public TrailerBody Attached { get; private set; }
        public Transform Anchor => Hitch != null ? Hitch.module.attachmentPoint : null;
        public int SuccessfulHitches { get; private set; }
        public string LastReason { get; private set; }
        public string DiagnosticState(TrailerBody trailer)
        {
            var vc = Hitch != null ? Hitch.module.vehicleController : null;
            var tv = trailer != null ? trailer.Vehicle : null;
            float distance = Anchor != null && trailer != null && trailer.Kingpin != null ? Vector3.Distance(Anchor.position, trailer.Kingpin.position) : -1;
            float angle = trailer != null ? Quaternion.Angle(transform.rotation, trailer.transform.rotation) : -1;
            return "reason=" + LastReason + " distance=" + distance.ToString("F5") + " rotation=" + angle.ToString("F2") +
                (Anchor!=null&&trailer!=null?" anchor="+Anchor.position.ToString("F4")+" pin="+trailer.Kingpin.position.ToString("F4")+" delta="+(trailer.Kingpin.position-Anchor.position).ToString("F4"):"") +
                " tractorReady=" + (vc != null && vc.IsInitialized) + " trailerReady=" + (tv != null && tv.IsInitialized) +
                " nativeAttached=" + (Hitch != null && Hitch.module.attached) + " hitches=" + SuccessfulHitches +
                " hitchModuleReady=" + (Hitch != null && Hitch.module.IsActive) + " joints=" + GetComponents<ConfigurableJoint>().Length;
        }
        struct WheelContact { public Collider wheel,load; }
        readonly System.Collections.Generic.List<WheelContact> guidedWheelContacts=new System.Collections.Generic.List<WheelContact>(64);
        NWH.WheelController3D.WheelController[] roadWheels;
        TrailerBody guidedTrailer;
        void ClearWheelGuide()
        {
            foreach(var pair in guidedWheelContacts)
                if(pair.wheel!=null&&pair.load!=null&&pair.wheel.gameObject.activeInHierarchy&&pair.load.gameObject.activeInHierarchy)
                    Physics.IgnoreCollision(pair.wheel,pair.load,false);
            guidedWheelContacts.Clear();guidedTrailer=null;
        }
        void PrepareWheelGuide(TrailerBody trailer)
        {
            bool aligned=trailer!=null&&Anchor!=null&&Vector3.Distance(Anchor.position,trailer.Kingpin.position)<2
                &&Quaternion.Angle(transform.rotation,trailer.transform.rotation)<MaximumAngle;
            if(!aligned){ClearWheelGuide();return;}
            if(guidedTrailer==trailer)return;
            ClearWheelGuide();guidedTrailer=trailer;
            var shapes=trailer.GetComponentsInChildren<Collider>(true);
            foreach(var wheel in roadWheels)
            {
                var tyre=wheel.wheel.meshCollider;if(tyre==null||!tyre.enabled)continue;
                foreach(var shape in shapes)
                    if(shape.enabled&&!shape.isTrigger&&shape.attachedRigidbody==trailer.Body&&!Physics.GetIgnoreCollision(tyre,shape))
                    {Physics.IgnoreCollision(tyre,shape,true);guidedWheelContacts.Add(new WheelContact{wheel=tyre,load=shape});}
            }
            // The native convex tyre housings overlap this prefab's underside
            // at a correctly aligned fifth wheel. Only these two mating rigs'
            // tyre/hull pairs are filtered during the final aligned approach.
            // Wheel ray/suspension support, cab/chassis/obstacle collisions,
            // the strict capture envelope and the native joint remain intact.
        }
        ConfigurableJoint capturedJoint;
        public bool PhysicalConstraintPresent => Attached!=null && capturedJoint!=null && capturedJoint.connectedBody==Attached.Body;
        float nextAttach;
        public void ResetMatchRecords() { SuccessfulHitches = 0; LastReason = ""; }

        void Awake()
        {
            roadWheels=GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            if (Body == null) Body = GetComponent<Rigidbody>();
            if (Hitch == null) Hitch = GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            if (Hitch != null) { Hitch.module.attachOnEnable = false; Hitch.module.detachable = true; }
        }
        public bool CanCouple(TrailerBody trailer, out string reason)
        {
            reason = "";
            if (Hitch == null || Hitch.module == null || Anchor == null || trailer == null || trailer.Module == null || trailer.Kingpin == null || trailer.Body == null || Body == null)
                reason = "Missing native hitch or kingpin";
            else if (Attached != null || Hitch.module.attached || trailer.Module.module.attached || Time.time < nextAttach)
                reason = "Hitch occupied or release still retiring";
            else if (Hitch.module.vehicleController == null || trailer.Vehicle == null || !Hitch.module.vehicleController.IsInitialized || !trailer.Vehicle.IsInitialized)
                reason = "Native suspension is initializing";
            else if (Vector3.Distance(Anchor.position, trailer.Kingpin.position) > CaptureDistance)
                reason = "Align fifth wheel directly below kingpin";
            else if (Quaternion.Angle(transform.rotation, trailer.transform.rotation) > MaximumAngle)
                reason = "Align tractor and trailer heading, pitch and roll";
            else if ((Body.GetPointVelocity(Anchor.position) - trailer.Body.GetPointVelocity(trailer.Kingpin.position)).magnitude > MaximumRelativeSpeed)
                reason = "Slow down and match trailer velocity";
            return reason.Length == 0;
        }
        public bool TryCouple(TrailerBody trailer)
        {
            if (!CanCouple(trailer, out string reason)) { LastReason = reason; return false; }
            Hitch.module.AttachTrailer(trailer.Module);
            if (!Hitch.module.attached) { LastReason = "Native coupling rejected"; return false; }
            Attached = trailer; trailer.Owner = Participant; trailer.SetBrakes(0);
            capturedJoint=null;
            foreach (var joint in GetComponents<ConfigurableJoint>())
                if (joint.connectedBody == trailer.Body)
                { capturedJoint=joint; joint.enableCollision = false; joint.projectionMode = JointProjectionMode.PositionAndRotation; joint.projectionDistance = .05f; joint.projectionAngle = 3; }
            if(capturedJoint==null){Release();LastReason="Native physical hitch joint missing";return false;}
            SuccessfulHitches++;LastReason = "Fifth wheel locked"; return true;
        }
        public bool GuideCoupling(TrailerBody trailer, float dt, bool velocityMatchedApproach = false)
        {
            if (trailer == null || Anchor == null || trailer.Kingpin == null || Attached != null || trailer.Module.module.attached) return false;
            PrepareWheelGuide(trailer);
            Vector3 delta = trailer.Kingpin.position - Anchor.position;
            Vector3 relative = trailer.Body.GetPointVelocity(trailer.Kingpin.position) - Body.GetPointVelocity(Anchor.position);
            if (delta.magnitude < .65f && Quaternion.Angle(transform.rotation, trailer.transform.rotation) < MaximumAngle && (velocityMatchedApproach ? relative.magnitude : Body.linearVelocity.magnitude) < 1.2f)
            {
                Body.AddForceAtPosition(Vector3.ClampMagnitude(delta * 18 + relative * 6, 5) * Body.mass, Anchor.position);
            }
            return TryCouple(trailer);
        }
        public TrailerBody Release()
        {
            ClearWheelGuide();
            var released = Attached;
            if (Hitch != null && Hitch.module.attached) Hitch.module.DetachTrailer(Hitch.module.vehicleController);
            Attached = null; capturedJoint=null; if (released != null) released.Owner = -1;
            nextAttach = Time.time + .1f; LastReason = "Trailer released"; return released;
        }
        void LateUpdate()
        {
            if(guidedTrailer!=null&&(Attached!=null||guidedTrailer.Module.module.attached||Vector3.Distance(Anchor.position,guidedTrailer.Kingpin.position)>2.5f))ClearWheelGuide();
            if (Attached != null && (Hitch == null || !Hitch.module.attached || !PhysicalConstraintPresent))
            {
                var lost=Attached;
                if(Hitch!=null && Hitch.module.attached)Hitch.module.DetachTrailer(Hitch.module.vehicleController);
                lost.Owner=-1;Attached=null;capturedJoint=null;nextAttach=Time.time+.1f;LastReason="Physical hitch constraint lost";
            }
        }
        void OnDestroy() { ClearWheelGuide();if(Hitch!=null&&Hitch.module.attached)Release(); }
    }
}
