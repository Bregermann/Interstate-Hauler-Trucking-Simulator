using UnityEngine;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;

namespace LWS.TruckHitchSports
{
    // The native NWH vehicle, axles and suspension remain the physical authority.
    [DisallowMultipleComponent]
    public sealed class TrailerBody : MonoBehaviour
    {
        public Rigidbody Body;
        public TrailerModuleWrapper Module;
        public VehicleController Vehicle;
        public Transform Kingpin => Module != null ? Module.module.attachmentPoint : null;
        public float InitialMass { get; private set; }
        public float CargoCondition { get; private set; } = 1;
        public int Owner { get; internal set; } = -1;
        public int Deliveries { get; internal set; }
        public float LastImpact { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public Quaternion StartRotation { get; private set; }
        Vector3 originalCenter;
        NWH.WheelController3D.WheelController[] wheels;
        float[] originalLongitudinalGrip, originalLateralGrip;
        public void SetGripMultiplier(float multiplier)
        {
            if(wheels==null||Vehicle==null||!Vehicle.IsInitialized)return;
            if(originalLongitudinalGrip==null)
            {
                originalLongitudinalGrip=new float[wheels.Length];originalLateralGrip=new float[wheels.Length];
                for(int i=0;i<wheels.Length;i++){originalLongitudinalGrip[i]=wheels[i].LongitudinalFrictionGrip;originalLateralGrip[i]=wheels[i].LateralFrictionGrip;}
            }
            for(int i=0;i<wheels.Length;i++){wheels[i].LongitudinalFrictionGrip=originalLongitudinalGrip[i]*Mathf.Max(.01f,multiplier);wheels[i].LateralFrictionGrip=originalLateralGrip[i]*Mathf.Max(.01f,multiplier);}
        }
        Rigidbody[] parts;
        Vector3[] partPositions;
        Quaternion[] partRotations;

        void Awake()
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            if (Module == null) Module = GetComponentInChildren<TrailerModuleWrapper>(true);
            if (Vehicle == null) Vehicle = GetComponent<VehicleController>();
            InitialMass = Body.mass; originalCenter = Body.centerOfMass;
            StartPosition = Body.position; StartRotation = Body.rotation;
            wheels = GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            parts = GetComponentsInChildren<Rigidbody>(true); partPositions = new Vector3[parts.Length]; partRotations = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            { partPositions[i] = transform.InverseTransformPoint(parts[i].position); partRotations[i] = Quaternion.Inverse(transform.rotation) * parts[i].rotation; }
        }
        public void ResetForRound()
        {
            if (Module != null && Module.module.attached && Module.module.trailerHitch != null)
                Module.module.trailerHitch.DetachTrailer(Module.module.trailerHitch.vehicleController);
            // Reset is the round boundary, never a coupling/delivery shortcut.
            // NWH attaches from Transform anchors; a Rigidbody-only teleport
            // leaves those anchors in the previous interpolation pose on rematch.
            foreach (var part in parts) part.interpolation = RigidbodyInterpolation.None;
            Body.isKinematic = false; Body.mass = InitialMass; Body.centerOfMass = originalCenter;
            Body.position = StartPosition; Body.rotation = StartRotation;
            transform.SetPositionAndRotation(StartPosition, StartRotation);
            Body.linearVelocity = Body.angularVelocity = Vector3.zero;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == Body) continue;
                parts[i].position = transform.TransformPoint(partPositions[i]); parts[i].rotation = transform.rotation * partRotations[i];
                parts[i].transform.SetPositionAndRotation(parts[i].position, parts[i].rotation);
                if (!parts[i].isKinematic) parts[i].linearVelocity = parts[i].angularVelocity = Vector3.zero;
            }
            CargoCondition = 1; Owner = -1; Deliveries = 0; LastImpact = 0;
            Physics.SyncTransforms();
            foreach (var part in parts) part.interpolation = RigidbodyInterpolation.Interpolate;
            Body.WakeUp(); SetBrakes(1);
        }
        public void SetBrakes(float amount)
        {
            if (Vehicle == null) return;
            Vehicle.enabled = true; Vehicle.input.autoSetInput = false;
            Vehicle.input.states.brakes = Vehicle.input.states.brakesRaw = Mathf.Clamp01(amount);
            Vehicle.input.states.handbrake = Vehicle.input.states.handbrakeRaw = Mathf.Clamp01(amount);
        }
        public void ShiftCargo(Vector3 offset) => Body.centerOfMass = originalCenter + offset;
        public void AddCargo(float kilograms) => Body.mass = Mathf.Max(InitialMass, InitialMass + kilograms);
        public bool WheelContact
        {
            get { if (wheels != null) foreach (var wheel in wheels) if (wheel != null && wheel.IsGrounded) return true; return false; }
        }
        void OnCollisionEnter(Collision collision)
        {
            LastImpact = collision.relativeVelocity.magnitude;
            if (LastImpact > 3) CargoCondition = Mathf.Max(0, CargoCondition - (LastImpact - 3) * .012f);
        }
    }
}
