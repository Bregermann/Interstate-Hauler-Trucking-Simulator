using System.Collections.Generic;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class LwsTruckStabilityController : MonoBehaviour
    {
        public const float DefaultFrontAntiRollStrength = 14000f;
        public const float DefaultRearAntiRollStrength = 18000f;
        public const float DefaultLowSpeedMaxSteeringAngle = 35f;
        public const float DefaultHighSpeedMaxSteeringAngle = 8f;
        public const float DefaultSpeedAtMaximumReductionMph = 70f;
        public static readonly Vector3 DefaultCenterOfMassOffset = new Vector3(0f, -0.45f, 0f);
        public static readonly Vector3 DefaultInertiaTensorScale = new Vector3(1.15f, 1.05f, 1.55f);

        [Header("References")]
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private Rigidbody tractorRigidbody;

        [Header("Center Of Mass")]
        [Tooltip("Applied in tractor local space after the original Rigidbody center of mass is captured. Lower Y makes the truck more planted; extreme values make the truck feel artificial.")]
        [SerializeField] private Vector3 centerOfMassOffset = DefaultCenterOfMassOffset;
        [Tooltip("When enabled, centerOfMassOffset is added to the Rigidbody's original center of mass on startup.")]
        [SerializeField] private bool applyCenterOfMassOffset = true;

        [Header("Inertia")]
        [Tooltip("Scales the original Rigidbody inertia tensor. Higher Z resists roll around the truck's long axis without freezing rollover.")]
        [SerializeField] private Vector3 inertiaTensorScale = DefaultInertiaTensorScale;
        [Tooltip("When enabled, scales the Rigidbody inertia tensor once during stability setup.")]
        [SerializeField] private bool applyInertiaTensorScale = true;

        [Header("NWH Anti-Roll")]
        [Tooltip("NWH wheel-group anti-roll force applied to the front axle. Higher values resist cab/chassis lean; excessive values can cause jitter.")]
        [SerializeField] private float frontAntiRollStrength = DefaultFrontAntiRollStrength;
        [Tooltip("NWH wheel-group anti-roll force applied to rear axles. Higher values resist rollover; excessive values can cause jitter.")]
        [SerializeField] private float rearAntiRollStrength = DefaultRearAntiRollStrength;
        [Tooltip("NWH WheelController force application height as a percentage of spring length. Higher values reduce rollover torque from tire forces.")]
        [SerializeField] private float forceApplicationPointDistance = 0.9f;
        [Tooltip("Optional multiplier for lateral tire grip. Values below 1 encourage sliding before rollover. Default keeps vendor tire grip unchanged.")]
        [SerializeField] private float lateralGripMultiplier = 1f;

        [Header("Speed-Sensitive Steering")]
        [Tooltip("Configures the existing NWH steering curve from the values below.")]
        [SerializeField] private bool configureNwhSteering = true;
        [Tooltip("Maximum NWH wheel steering angle at low speed.")]
        [SerializeField] private float lowSpeedMaxSteeringAngle = DefaultLowSpeedMaxSteeringAngle;
        [Tooltip("Maximum effective steering angle once speedAtMaximumReductionMph is reached.")]
        [SerializeField] private float highSpeedMaxSteeringAngle = DefaultHighSpeedMaxSteeringAngle;
        [Tooltip("Vehicle speed where the high-speed steering limit is fully applied.")]
        [SerializeField] private float speedAtMaximumReductionMph = DefaultSpeedAtMaximumReductionMph;

        [Header("Diagnostics")]
        [Tooltip("Logs the resolved mass, COM, inertia, anti-roll, and steering settings once when the truck is configured.")]
        [SerializeField] private bool logStabilityConfiguration = false;

        private readonly Dictionary<WheelUAPI, float> _baseLateralGripByWheel = new Dictionary<WheelUAPI, float>();
        private Vector3 _baseCenterOfMass;
        private Vector3 _baseInertiaTensor;
        private bool _baseCaptured;

        public Vector3 CenterOfMassOffset => centerOfMassOffset;
        public float FrontAntiRollStrength => frontAntiRollStrength;
        public float RearAntiRollStrength => rearAntiRollStrength;
        public float ForceApplicationPointDistance => forceApplicationPointDistance;
        public float LateralGripMultiplier => lateralGripMultiplier;
        public float LowSpeedMaxSteeringAngle => lowSpeedMaxSteeringAngle;
        public float HighSpeedMaxSteeringAngle => highSpeedMaxSteeringAngle;
        public float SpeedAtMaximumReductionMph => speedAtMaximumReductionMph;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureBasePhysics();
        }

        private void Start()
        {
            ApplyStabilityTuning();
        }

        public void ApplyStabilityTuning()
        {
            ResolveReferences();
            CaptureBasePhysics();
            ApplyRigidbodyTuning();
            ApplyNwhWheelTuning();
            ApplyNwhSteeringTuning();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (logStabilityConfiguration)
            {
                Debug.Log(
                    $"[IH Truck Stability] {name}: mass {tractorRigidbody?.mass ?? 0f:0} kg, base COM {_baseCenterOfMass}, final COM {tractorRigidbody?.centerOfMass ?? Vector3.zero}, inertia {tractorRigidbody?.inertiaTensor ?? Vector3.zero}, front ARB {frontAntiRollStrength:0}, rear ARB {rearAntiRollStrength:0}, steering {lowSpeedMaxSteeringAngle:0.#}->{highSpeedMaxSteeringAngle:0.#} deg by {speedAtMaximumReductionMph:0} mph.",
                    this);
            }
#endif
        }

        public float EvaluateEffectiveMaxSteeringAngle(float speedMetersPerSecond)
        {
            float high = Mathf.Clamp(highSpeedMaxSteeringAngle, 0f, lowSpeedMaxSteeringAngle);
            float low = Mathf.Max(0.01f, lowSpeedMaxSteeringAngle);
            float reductionSpeed = Mathf.Max(1f, speedAtMaximumReductionMph) * 0.44704f;
            float t = Mathf.Clamp01(Mathf.Abs(speedMetersPerSecond) / reductionSpeed);
            return Mathf.Lerp(low, high, SmoothReduction(t));
        }

        private void ResolveReferences()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            if (tractorRigidbody == null)
            {
                tractorRigidbody = vehicleController != null && vehicleController.vehicleRigidbody != null
                    ? vehicleController.vehicleRigidbody
                    : GetComponent<Rigidbody>();
            }
        }

        private void CaptureBasePhysics()
        {
            if (_baseCaptured || tractorRigidbody == null)
            {
                return;
            }

            _baseCenterOfMass = tractorRigidbody.centerOfMass;
            _baseInertiaTensor = tractorRigidbody.inertiaTensor;
            _baseCaptured = true;
        }

        private void ApplyRigidbodyTuning()
        {
            if (tractorRigidbody == null)
            {
                return;
            }

            if (applyCenterOfMassOffset)
            {
                tractorRigidbody.centerOfMass = _baseCenterOfMass + centerOfMassOffset;
            }

            if (applyInertiaTensorScale && _baseInertiaTensor.x > 0f && _baseInertiaTensor.y > 0f && _baseInertiaTensor.z > 0f)
            {
                tractorRigidbody.inertiaTensor = new Vector3(
                    _baseInertiaTensor.x * Mathf.Max(0.01f, inertiaTensorScale.x),
                    _baseInertiaTensor.y * Mathf.Max(0.01f, inertiaTensorScale.y),
                    _baseInertiaTensor.z * Mathf.Max(0.01f, inertiaTensorScale.z));
            }
        }

        private void ApplyNwhWheelTuning()
        {
            if (vehicleController == null || vehicleController.powertrain == null || vehicleController.powertrain.wheelGroups == null)
            {
                return;
            }

            List<WheelGroup> pairedGroups = new List<WheelGroup>();
            foreach (WheelGroup group in vehicleController.powertrain.wheelGroups)
            {
                if (group != null && group.Wheels != null && group.Wheels.Count == 2)
                {
                    pairedGroups.Add(group);
                }
            }

            for (int i = 0; i < pairedGroups.Count; i++)
            {
                WheelGroup group = pairedGroups[i];
                bool frontGroup = i == 0;
                group.antiRollBarForce = frontGroup ? frontAntiRollStrength : rearAntiRollStrength;
            }

            if (vehicleController.powertrain.wheels == null)
            {
                return;
            }

            foreach (NWH.VehiclePhysics2.Powertrain.WheelComponent wheel in vehicleController.powertrain.wheels)
            {
                WheelUAPI wheelUapi = wheel?.wheelUAPI;
                if (wheelUapi == null)
                {
                    continue;
                }

                if (!_baseLateralGripByWheel.ContainsKey(wheelUapi))
                {
                    _baseLateralGripByWheel.Add(wheelUapi, wheelUapi.LateralFrictionGrip);
                }

                wheelUapi.ForceApplicationPointDistance = Mathf.Max(0f, forceApplicationPointDistance);
                wheelUapi.LateralFrictionGrip = Mathf.Max(0.1f, _baseLateralGripByWheel[wheelUapi] * lateralGripMultiplier);
            }
        }

        private void ApplyNwhSteeringTuning()
        {
            if (!configureNwhSteering || vehicleController == null || vehicleController.steering == null)
            {
                return;
            }

            float low = Mathf.Max(1f, lowSpeedMaxSteeringAngle);
            float high = Mathf.Clamp(highSpeedMaxSteeringAngle, 0.5f, low);
            float highNormalized = high / low;
            float maxReductionNormalizedSpeed = Mathf.Clamp01((Mathf.Max(1f, speedAtMaximumReductionMph) * 0.44704f) / 50f);
            float midSpeed = Mathf.Clamp01(maxReductionNormalizedSpeed * 0.5f);
            float midValue = Mathf.Lerp(1f, highNormalized, 0.55f);

            vehicleController.steering.maximumSteerAngle = low;
            vehicleController.steering.speedSensitiveSteeringCurve = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(midSpeed, midValue, -0.5f, -0.5f),
                new Keyframe(maxReductionNormalizedSpeed, highNormalized, -0.2f, -0.05f),
                new Keyframe(1f, highNormalized, 0f, 0f));
        }

        private static float SmoothReduction(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private void OnValidate()
        {
            frontAntiRollStrength = Mathf.Max(0f, frontAntiRollStrength);
            rearAntiRollStrength = Mathf.Max(0f, rearAntiRollStrength);
            forceApplicationPointDistance = Mathf.Max(0f, forceApplicationPointDistance);
            lateralGripMultiplier = Mathf.Max(0.1f, lateralGripMultiplier);
            lowSpeedMaxSteeringAngle = Mathf.Max(1f, lowSpeedMaxSteeringAngle);
            highSpeedMaxSteeringAngle = Mathf.Clamp(highSpeedMaxSteeringAngle, 0.5f, lowSpeedMaxSteeringAngle);
            speedAtMaximumReductionMph = Mathf.Max(1f, speedAtMaximumReductionMph);
        }

        [ContextMenu("Apply Stability Tuning")]
        private void ApplyStabilityTuningFromInspector()
        {
            ApplyStabilityTuning();
        }
    }
}
