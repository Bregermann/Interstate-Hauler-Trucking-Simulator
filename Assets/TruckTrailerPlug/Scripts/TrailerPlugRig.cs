using System.Collections;
using System.Linq;
using UnityEngine;
using NWH.VehiclePhysics2.Modules.Trailer;
using LWS.TruckSports;
namespace LWS.TruckTrailerPlug
{
    public sealed class TrailerPlugRig : MonoBehaviour
    {
        public TrailerPlugMatch match;
        public SportsEventTruck truck;
        public Rigidbody trailer;
        public Transform anchor;
        public SpringJoint spring;
        public LineRenderer cable;
        public TrailerHitchModuleWrapper tractorHitch;
        public TrailerModuleWrapper trailerModule;
        public Bounds trailerLocalBounds;
        public Vector3 TrailerCenter => trailer.transform.TransformPoint(trailerLocalBounds.center);
        public Vector3 truckStart = new Vector3(-7, 61, -22);
        public Vector3 trailerOffset = new Vector3(0, -.6f, -9);
        public Vector3 trailerHitch = new Vector3(0, 1, 7.5f);
        public Vector3 truckHitch = new Vector3(0, .4f, -1.5f);
        public float restLength = 48;
        public bool Released { get; private set; }
        public bool Supported
        {
            get
            {
                if (Time.fixedTime - lastSupport < .12f || trailer.IsSleeping() && lastSupport > 0) return true;
                if (trailerWheels != null) foreach (var wheel in trailerWheels)
                    if (wheel.enabled && wheel.IsGrounded && wheel.HitCollider != null && wheel.HitNormal.y > .45f && wheel.HitPoint.y > -.3f) return true;
                return false;
            }
        }
        public Vector3 IncomingVelocity { get; private set; }
        public Vector3 IncomingAngular { get; private set; }
        public Vector3 IncomingCenter { get; private set; }
        public Vector3 ReleasedVelocity { get; private set; }
        public Vector3 ReleasedAngular { get; private set; }
        ConfigurableJoint hitch;
        Coroutine pendingAttachment;
        Rigidbody[] tractorParts;
        NWH.WheelController3D.WheelController[] trailerWheels;
        public bool Attached => tractorHitch != null && tractorHitch.module.attached && hitch != null;
        public float HitchSeparation => tractorHitch == null || trailerModule == null ? float.PositiveInfinity :
            Vector3.Distance(tractorHitch.module.attachmentPoint.position, trailerModule.module.attachmentPoint.position);
        float lastSupport = -100;
        public void Paint(Color color)
        {
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            truck.paint.SetPropertyBlock(block);
        }
        public void Prepare()
        {
            if (tractorHitch == null || trailerModule == null)
                throw new System.InvalidOperationException("Trailer Plug requires the matched NWH tractor/trailer repair in its saved scene.");
            if (pendingAttachment != null) { StopCoroutine(pendingAttachment); pendingAttachment = null; }
            ClearHitch(); Released = false; lastSupport = -100;
            TrailerWheels(false);
            tractorParts ??= truck.GetComponentsInChildren<Rigidbody>(true).Where(b => b != truck.body).ToArray();
            // Retire the previous drop's render interpolation history while
            // teleporting the stationary countdown assembly, not during flight.
            truck.body.interpolation = trailer.interpolation = RigidbodyInterpolation.None;
            foreach (var part in tractorParts) part.interpolation = RigidbodyInterpolation.None;
            truck.Prepare(truckStart, Quaternion.identity, false);
            truck.body.isKinematic = true; truck.Power(false);
            foreach (var part in tractorParts) part.isKinematic = true;
            trailer.gameObject.SetActive(true); trailer.isKinematic = true;
            trailer.position = truckStart + trailerOffset; trailer.rotation = Quaternion.identity;
            trailer.transform.SetPositionAndRotation(trailer.position, trailer.rotation);
            Physics.SyncTransforms();
            trailer.detectCollisions = true;
            spring.maxDistance = restLength;
            // NWH destroys its previous joint at end of frame. Reattaching in the
            // same callback can reuse that doomed joint. Countdown safely owns
            // both stationary bodies while the vendor gets a clean next frame.
            pendingAttachment = StartCoroutine(AttachAfterOldJointRetires());
        }
        IEnumerator AttachAfterOldJointRetires()
        {
            yield return null;
            tractorHitch.module.AttachTrailer(trailerModule);
            // The vendor aligns Transform anchors; synchronize the kinematic
            // Rigidbody pose before the countdown can hand it to interpolation.
            trailer.position = trailer.transform.position; trailer.rotation = trailer.transform.rotation;
            Physics.SyncTransforms();
            // Suspended competition has no powered road wheels. NWH still owns
            // the hitch, attachment points and compound physical vehicle bodies.
            trailerModule.module.vehicleController.enabled = false;
            hitch = truck.body.GetComponents<ConfigurableJoint>().FirstOrDefault(j => j.connectedBody == trailer);
            if (hitch == null) throw new System.InvalidOperationException("NWH did not create Trailer Plug's actual hitch.");
            hitch.enableCollision = false; hitch.projectionMode = JointProjectionMode.PositionAndRotation;
            hitch.projectionDistance = .05f; hitch.projectionAngle = 3;
            foreach (var a in trailer.GetComponentsInChildren<Collider>())
                foreach (var b in truck.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a, b, true);
            pendingAttachment = null;
        }
        public void BeginSwing()
        {
            if (!Attached) throw new System.InvalidOperationException("Countdown cannot release an unattached Trailer Plug rig.");
            truck.body.interpolation = trailer.interpolation = RigidbodyInterpolation.Interpolate;
            foreach (var part in tractorParts) part.interpolation = RigidbodyInterpolation.Interpolate;
            truck.body.isKinematic = trailer.isKinematic = false;
            truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero;
            trailer.linearVelocity = trailer.angularVelocity = Vector3.zero;
            foreach (var part in tractorParts) { part.isKinematic = false; part.linearVelocity = part.angularVelocity = Vector3.zero; part.WakeUp(); }
            truck.body.WakeUp(); trailer.WakeUp();
            TrailerWheels(true);
        }
        public void Pump(Vector2 input)
        {
            if (!Released) LWS.TruckBungeeDemolition.BungeeSwingForces.Apply(truck.body, input, 5.5f);
        }
        public bool Release()
        {
            if (Released || !Attached) return false;
            Released = true; ReleasedVelocity = trailer.linearVelocity; ReleasedAngular = trailer.angularVelocity;
            ClearHitch(); return true;
        }
        void ClearHitch()
        {
            if (tractorHitch != null && tractorHitch.module.attached)
                tractorHitch.module.DetachTrailer(tractorHitch.module.vehicleController);
            hitch = null;
        }
        public void Hold()
        {
            if (pendingAttachment != null) { StopCoroutine(pendingAttachment); pendingAttachment = null; }
            truck.Power(false); truck.body.isKinematic = true; trailer.isKinematic = true;
            TrailerWheels(false);
            if (tractorParts != null) foreach (var part in tractorParts) if (part != null) part.isKinematic = true;
        }
        void FixedUpdate()
        {
            IncomingVelocity = trailer.linearVelocity; IncomingAngular = trailer.angularVelocity;
            IncomingCenter = trailer.worldCenterOfMass;
        }
        void TrailerWheels(bool enabled)
        {
            trailerWheels ??= trailer.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            foreach (var wheel in trailerWheels)
            {
                wheel.MotorTorque = wheel.BrakeTorque = 0; wheel.SteerAngle = 0; wheel.enabled = enabled;
                if (wheel.wheel.meshCollider != null) wheel.wheel.meshCollider.enabled = enabled;
            }
        }
        public void GroundContact(Collision collision)
        {
            if (!Released || collision.collider.isTrigger || collision.rigidbody != null) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y > .45f && collision.GetContact(i).point.y > -.3f)
                { lastSupport = Time.fixedTime; break; }
        }
        void LateUpdate()
        {
            if (cable == null) return;
            const int count = 32; cable.positionCount = count;
            var tip = truck.transform.TransformPoint(spring.anchor);
            float slack = Mathf.Max(0, restLength - Vector3.Distance(anchor.position, tip));
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                cable.SetPosition(i, Vector3.Lerp(anchor.position, tip, t) + Vector3.down *
                    (Mathf.Sin(t * Mathf.PI) * Mathf.Min(5, slack * .15f)));
            }
        }
        void OnDestroy() { if (pendingAttachment != null) StopCoroutine(pendingAttachment); ClearHitch(); }
    }
}
