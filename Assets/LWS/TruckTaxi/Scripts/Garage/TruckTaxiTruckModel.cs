using System;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Authored configuration on a REAL NWH player prefab, not another vehicle
    // controller, ownership catalog, save participant or runtime identity.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiTruckModel : MonoBehaviour
    {
        [Header("Original Blender body / proven NWH chassis")]
        public bool retainsOriginalBody;
        public GameObject bodyVisualPrefab;
        public GameObject[] distanceLodPrefabs = Array.Empty<GameObject>();
        public Material[] primaryPaintMaterials = Array.Empty<Material>();
        [Tooltip("Exact original mesh identities replaced by this body. Never hide GPS, occupants or arbitrary renderers.")]
        public Mesh[] replacedSourceMeshes = Array.Empty<Mesh>();

        [Header("Model-specific cab authoring (metres, tractor local space)")]
        public bool hasSleeper;
        public Vector3 cabOffset;
        [Tooltip("Authored rear axle shift. Sleeper chassis extend the wheelbase to maintain actual trailer corner clearance. Requires per-prefab physical validation.")]
        public float rearAxleOffset;
        public float fifthWheelOffset;
        public Vector3 driverEye = new Vector3(-.56f, 2.20f, .62f);
        public Vector3 gpsMount = new Vector3(-.08f, 2.18f, 1.35f);
        public Vector3 driverSeat = new Vector3(-.56f, 1.22f, .65f);
        public Vector3 frontPassengerSeat = new Vector3(.56f, 1.22f, .65f);
        public Vector3 companionSeat = new Vector3(.65f, 1.76f, -.40f);
        public Vector3 sleeperDriver = new Vector3(-.65f, 1.76f, -.40f);
        public Vector3 sleeperLamp = new Vector3(0, 3.6f, -.25f);
        public Vector3 headlampOffset = new Vector3(.99f,1.05f,3.24f);
        [Tooltip("Cab/sleeper shell clearance, excluding the preserved NWH running gear.")]
        public Bounds cabinCollision = new Bounds(new Vector3(0, 2.35f, .30f), new Vector3(2.30f, 2.30f, 2.40f));
        public Bounds hoodCollision = new Bounds(new Vector3(0, 1.40f, 2.20f), new Vector3(1.75f, 1.05f, 1.65f));

        [Header("Real NWH tuning; baseline retained until driving validation")]
        [Min(1000)] public float tractorMassKg = 7400;
        [Range(1,5)] public float launchPowerAtRest = 3.4f;
        [Range(1,4)] public float launchPowerAt15Mph = 2.2f;
        [Range(35,70)] public float lowSpeedSteeringLock = 70;
        [Range(15,60)] public float citySteeringLock = 40;
        [Range(3,15)] public float highwaySteeringLock = 8;
        [Range(90,500)] public float steeringResponse = 400;

        [Header("Dealership presentation, not simulated performance scores")]
        [TextArea] public string engineDescription;
        [TextArea] public string handlingDescription;
        public Sprite[] previewAngles = Array.Empty<Sprite>();

        public bool Validate(out string reason)
        {
            if(retainsOriginalBody)
            {
                reason="Original starter requires explicit body-paint materials.";
                if(primaryPaintMaterials==null || primaryPaintMaterials.Length==0 || Array.Exists(primaryPaintMaterials,m=>m==null)) return false;
                reason=null; return true;
            }
            reason = "Model requires a real Blender body, exact paint channels and two distance LODs.";
            if (bodyVisualPrefab == null || bodyVisualPrefab.GetComponentsInChildren<MeshFilter>(true).Length == 0 ||
                primaryPaintMaterials == null || primaryPaintMaterials.Length == 0 ||
                Array.Exists(primaryPaintMaterials, m => m == null) ||
                distanceLodPrefabs == null || distanceLodPrefabs.Length != 2 ||
                Array.Exists(distanceLodPrefabs, p => p == null)) return false;
            reason = "Body preview must contain only presentation, never another NWH player or Rigidbody.";
            if (bodyVisualPrefab.GetComponentInChildren<VehicleController>(true) != null ||
                bodyVisualPrefab.GetComponentInChildren<Rigidbody>(true) != null ||
                bodyVisualPrefab.GetComponentInChildren<AudioListener>(true) != null ||
                bodyVisualPrefab.GetComponentInChildren<Camera>(true) != null) return false;
            reason = "Invalid non-finite model pose, collision or handling configuration.";
            if (!Finite(cabOffset) || !InRange(rearAxleOffset,-2,0) || !InRange(fifthWheelOffset,-2,0) || !Finite(driverEye) || !Finite(gpsMount) || !Finite(driverSeat) || !Finite(frontPassengerSeat) ||
                !Finite(companionSeat) || !Finite(sleeperDriver) || !Finite(sleeperLamp) ||
                !ValidBounds(cabinCollision) || !ValidBounds(hoodCollision) ||
                !float.IsFinite(tractorMassKg) || tractorMassKg < 1000 ||
                !InRange(launchPowerAtRest,1,5) || !InRange(launchPowerAt15Mph,1,launchPowerAtRest) ||
                !InRange(lowSpeedSteeringLock,35,70) || !InRange(citySteeringLock,15,lowSpeedSteeringLock) ||
                !InRange(highwaySteeringLock,3,15) || !InRange(steeringResponse,90,500)) return false;
            reason = null;
            return true;
        }

        public static bool ValidatePlayerPrefab(LwsTruckDefinition definition, out string reason)
        {
            reason = "Truck definition or actual player prefab is missing.";
            if (definition == null || !definition.Validate(out reason)) return false;
            var prefab = definition.PlayerPrefab;
            var vehicle = prefab.GetComponent<VehicleController>();
            var body = prefab.GetComponent<Rigidbody>();
            reason = "Purchasable truck requires one root NWH controller and a non-kinematic tractor Rigidbody.";
            if (vehicle == null || body == null || body.isKinematic || body.mass <= 0 ||
                prefab.GetComponentsInChildren<VehicleController>(true).Length != 1) return false;
            reason = "The compatible tractor requires two actual paired NWH axles and four wheel references.";
            if (vehicle.powertrain?.wheelGroups == null || vehicle.powertrain.wheelGroups.Count != 2 ||
                vehicle.powertrain.wheels == null || vehicle.powertrain.wheels.Count != 4) return false;
            var counts = new int[2];
            foreach (var wheel in vehicle.powertrain.wheels)
            {
                if (wheel?.wheelUAPI == null || wheel.wheelGroupSelector == null ||
                    wheel.wheelGroupSelector.index < 0 || wheel.wheelGroupSelector.index > 1) return false;
                counts[wheel.wheelGroupSelector.index]++;
            }
            // WheelGroup.Wheels is initialized by NWH Awake, so validate its
            // serialized membership instead of requiring a live physics run.
            if (counts[0] != 2 || counts[1] != 2) return false;
            reason = "The authored NWH cab, cockpit and exterior cameras must remain present.";
            if (prefab.transform.Find("Cab") == null || prefab.GetComponentsInChildren<Camera>(true).Length < 2) return false;
            var model = prefab.GetComponent<TruckTaxiTruckModel>();
            if (model == null)
            {
                reason = "No authored Truck Taxi model configuration.";
                return false;
            }
            return model.Validate(out reason);
        }

        private static bool InRange(float value, float low, float high) => float.IsFinite(value) && value >= low && value <= high;
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        private static bool ValidBounds(Bounds value) => Finite(value.center) && Finite(value.size) &&
            value.size.x > .01f && value.size.y > .01f && value.size.z > .01f;
    }
}
