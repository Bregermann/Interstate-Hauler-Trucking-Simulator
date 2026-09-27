using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiVehicleHandlingOverride : MonoBehaviour
    {
        [Header("Truck Taxi only / NWH steering")]
        [Tooltip("Wheel steering lock below 10 MPH. Does not change Interstate prefabs.")]
        [Range(35, 70)] public float lowSpeedSteeringLock = 65;
        [Tooltip("Maximum wheel angle at 20 MPH; tapers further at highway speeds.")]
        [Range(15, 60)] public float citySteeringLock = 40;
        [Tooltip("Maximum wheel angle at 70 MPH and above.")]
        [Range(3, 15)] public float highSpeedSteeringLock = 8;
        [Tooltip("NWH wheel angle change limit, degrees/second. Higher responds faster.")]
        [Range(90, 500)] public float steeringResponse = 300;
        [Tooltip("Seconds of input smoothing at low speed. Highway smoothing is 0.15 seconds.")]
        [Range(.01f, .2f)] public float lowSpeedSmoothing = .045f;
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
            return launch * (effects?.VehiclePowerMultiplier ?? 1);
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
            steering.speedSensitiveSteeringCurve = new AnimationCurve(
                new Keyframe(0, 1), new Keyframe(10 / 111.8468f, 1),
                new Keyframe(20 / 111.8468f, citySteeringLock / lowSpeedSteeringLock),
                new Keyframe(35 / 111.8468f, 20 / lowSpeedSteeringLock),
                new Keyframe(70 / 111.8468f, highSpeedSteeringLock / lowSpeedSteeringLock),
                new Keyframe(1, highSpeedSteeringLock / lowSpeedSteeringLock));
            steering.speedSensitiveSmoothingCurve = AnimationCurve.Linear(0, lowSpeedSmoothing, 1, .15f);
            Applied = true;
            if (!vehicle.powertrain.engine.powerModifiers.Contains(PowerModifier))
                vehicle.powertrain.engine.powerModifiers.Add(PowerModifier);
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
            Restore(); lowSpeedSteeringLock = 65; citySteeringLock = 40; highSpeedSteeringLock = 8;
            steeringResponse = 300; lowSpeedSmoothing = .045f;
            launchPowerAtRest = 3.4f; launchPowerAt15Mph = 2.2f; Apply();
        }
        private void OnEnable() { if (vehicle != null) Apply(); }
        private void OnDisable() => Restore();
    }
}
