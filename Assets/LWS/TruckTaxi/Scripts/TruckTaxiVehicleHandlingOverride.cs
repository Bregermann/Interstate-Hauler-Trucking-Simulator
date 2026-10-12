using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiVehicleHandlingOverride : MonoBehaviour
    {
        [Header("Truck Taxi only / NWH steering")]
        [Tooltip("Wheel steering lock below 10 MPH. Does not change Interstate prefabs.")]
        [Range(35, 70)] public float lowSpeedSteeringLock = 70;
        [Tooltip("Maximum wheel angle at 20 MPH; tapers further at highway speeds.")]
        [Range(15, 60)] public float citySteeringLock = 40;
        [Tooltip("Maximum wheel angle at 70 MPH and above.")]
        [Range(3, 15)] public float highSpeedSteeringLock = 8;
        [Tooltip("NWH wheel angle change limit, degrees/second. Higher responds faster.")]
        [Range(90, 500)] public float steeringResponse = 400;
        [Tooltip("Seconds of input smoothing at low speed. Highway smoothing is 0.15 seconds.")]
        [Range(.01f, .2f)] public float lowSpeedSmoothing = .025f;
        [Header("Truck Taxi only / NWH engine")]
        [Tooltip("Engine power multiplier at rest; fades to the normal drivetrain by 20 MPH.")]
        [Range(1, 5)] public float launchPowerAtRest = 3.4f;
        [Tooltip("Engine power multiplier at 15 MPH.")]
        [Range(1, 4)] public float launchPowerAt15Mph = 2.2f;
        public bool Applied { get; private set; }
        public float SteeringAngle => vehicle != null ? vehicle.steering.angle : 0;
        public float SteeringInput => vehicle != null ? vehicle.input.Steering : 0;
        public float SpeedMph => vehicle != null ? vehicle.Speed * 2.236936f : 0;
        public float YawAssist => 0; // NWH wheel geometry supplies the turn; no extra torque.
        private VehicleController vehicle;
        private float originalLock, originalRate;
        private bool originalRaw, originalReturn;
        private AnimationCurve originalCurve, originalSmoothing;
        private TruckTaxiTemporaryEffects effects;
        public System.Func<float> MechanicalPowerMultiplier { get; set; }
        public float CareerPowerMultiplier { get; set; } = 1;

        public void Initialize(VehicleController controller)
        {
            if (vehicle != controller) Restore();
            vehicle = controller;
            Apply();
        }
        public void BindEffects(TruckTaxiTemporaryEffects activeEffects) { effects = activeEffects; }
        public float CurrentPowerMultiplier => PowerModifier();
        public static float LaunchMultiplier(float speedMph, float atRest = 3.4f, float at15Mph = 2.2f)
        {
            float speed = float.IsFinite(speedMph) ? Mathf.Abs(speedMph) : 20;
            if (speed >= 20) return 1;
            float start = Mathf.Max(1, atRest), middle = Mathf.Clamp(at15Mph, 1, start);
            float t = speed < 15 ? speed / 15 : (speed - 15) / 5;
            t = t * t * (3 - 2 * t);
            return speed < 15 ? Mathf.Lerp(start, middle, t) : Mathf.Lerp(middle, 1, t);
        }
        private float PowerModifier()
        {
            if (!Applied || vehicle == null) return 1;
            float launch = LaunchMultiplier(SpeedMph, launchPowerAtRest, launchPowerAt15Mph);
            return launch * (effects?.VehiclePowerMultiplier ?? 1) * Mathf.Clamp(MechanicalPowerMultiplier?.Invoke() ?? 1f, .1f, 1f) * Mathf.Clamp(CareerPowerMultiplier,1,1.5f);
        }
        public void SetDamageSteeringMultiplier(float multiplier)
        {
            // Compatibility entry point: component condition never limits steering authority.
            if (Applied && vehicle != null) vehicle.steering.maximumSteerAngle = lowSpeedSteeringLock;
        }
        public void Apply()
        {
            if (vehicle == null || Applied || vehicle.powertrain?.engine == null) return;
            var steering = vehicle.steering;
            originalLock = steering.maximumSteerAngle; originalRate = steering.degreesPerSecondLimit;
            originalRaw = steering.useRawInput; originalReturn = steering.returnToCenter;
            originalCurve = steering.speedSensitiveSteeringCurve; originalSmoothing = steering.speedSensitiveSmoothingCurve;
            steering.maximumSteerAngle = lowSpeedSteeringLock;
            steering.degreesPerSecondLimit = steeringResponse;
            steering.useRawInput = false; steering.returnToCenter = true;
            // Installed NWH evaluates Speed / 50 m/s (not /100 as its older tooltip says).
            steering.speedSensitiveSteeringCurve = CreateSteeringCurve(lowSpeedSteeringLock, citySteeringLock, highSpeedSteeringLock);
            steering.speedSensitiveSmoothingCurve = CreateSmoothingCurve(lowSpeedSmoothing);
            Applied = true;
            if (!vehicle.powertrain.engine.powerModifiers.Contains(PowerModifier))
                vehicle.powertrain.engine.powerModifiers.Add(PowerModifier);
        }
        public static AnimationCurve CreateSteeringCurve(float low, float city, float high) => new AnimationCurve(
                new Keyframe(0, 1), new Keyframe(10 / 111.8468f, 1),
                new Keyframe(20 / 111.8468f, city / low),
                new Keyframe(35 / 111.8468f, 20 / low),
                new Keyframe(70 / 111.8468f, high / low), new Keyframe(1, high / low));
        public static AnimationCurve CreateSmoothingCurve(float low) => new AnimationCurve(
                new Keyframe(0, low), new Keyframe(10 / 111.8468f, low),
                new Keyframe(35 / 111.8468f, Mathf.Lerp(.045f, .15f, 35 / 111.8468f), .105f, .105f),
                new Keyframe(1, .15f, .105f, .105f));
        public static float ResponseForSpeed(float mph, float lowSpeedResponse) =>
            Mathf.Lerp(lowSpeedResponse, Mathf.Min(300, lowSpeedResponse), Mathf.InverseLerp(10, 35, Mathf.Abs(mph)));
        private void Update()
        {
            if (Applied && vehicle != null)
                vehicle.steering.degreesPerSecondLimit = ResponseForSpeed(SpeedMph, steeringResponse);
        }
        public void Restore()
        {
            if (!Applied || vehicle == null) return;
            var steering = vehicle.steering;
            steering.maximumSteerAngle = originalLock; steering.degreesPerSecondLimit = originalRate;
            steering.useRawInput = originalRaw; steering.returnToCenter = originalReturn;
            steering.speedSensitiveSteeringCurve = originalCurve; steering.speedSensitiveSmoothingCurve = originalSmoothing;
            vehicle.powertrain?.engine?.powerModifiers.Remove(PowerModifier);
            Applied = false;
        }
        public void Toggle() { if (Applied) Restore(); else Apply(); }
        public void ResetDefaults()
        {
            Restore(); lowSpeedSteeringLock = 70; citySteeringLock = 40; highSpeedSteeringLock = 8;
            steeringResponse = 400; lowSpeedSmoothing = .025f;
            launchPowerAtRest = 3.4f; launchPowerAt15Mph = 2.2f; Apply();
        }
        private void OnEnable() { if (vehicle != null) Apply(); }
        private void OnDisable() => Restore();
    }
}
