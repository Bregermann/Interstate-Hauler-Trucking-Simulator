using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class TruckTaxiSteeringWheelVisual : MonoBehaviour
    {
        [Header("Truck Taxi presentation only - never modifies vehicle steering")]
        [Range(360, 1440), Tooltip("Total visual travel. 900 means 450 degrees each side of the authored neutral pose.")]
        public float lockToLockDegrees = 900;
        [Min(.02f), Tooltip("Smaller is more responsive. Damps the already-resolved NWH wheel angle, not keyboard input.")]
        public float steerVisualResponse = .16f;
        [Min(.02f), Tooltip("Seconds of response while approaching centre. Larger returns more gradually.")]
        public float returnToCenterResponse = .23f;
        [Min(1), Tooltip("Maximum visual rotation in degrees per second. Does not limit road-wheel steering.")]
        public float visualAngularVelocityLimit = 900;
        [Range(1, 2), Tooltip("1 is critically damped. Larger is overdamped and slower; values below 1 are intentionally disallowed.")]
        public float visualDamping = 1;
        [Tooltip("The existing truck wheel rotates around negative local Z. Change only for different interior art.")]
        public Vector3 localRotationAxis = Vector3.back;

        private VehicleController vehicle;
        private LwsTruckDashboardController dashboard;
        private Transform wheel, originalNwhWheel;
        private bool originalDashboardAnimation;
        private Quaternion neutralRotation;
        private TruckTaxiSteeringVisualMotion motion;
        public bool Applied { get; private set; }
        public Transform Wheel => wheel;
        public float RawSteeringInput => vehicle != null ? vehicle.input.Steering : 0;
        public float ResolvedSteeringAngle => vehicle != null ? vehicle.steering.angle + vehicle.steering.externallyAddedAngle : 0;
        public float TargetVisualAngle { get; private set; }
        public float CurrentVisualAngle => motion.Angle;
        public float VisualAngularVelocity => motion.Velocity;
        public string DiagnosticSummary => $"raw {RawSteeringInput:0.00} | resolved {ResolvedSteeringAngle:0.0} deg | target {TargetVisualAngle:0.0} | visual {CurrentVisualAngle:0.0} | velocity {VisualAngularVelocity:0.0} deg/s";

        public bool Initialize(VehicleController controller)
        {
            if (Applied && vehicle == controller) return true;
            Restore(); vehicle = controller;
            return Acquire();
        }
        private void OnEnable() { if (vehicle != null) Acquire(); }
        private bool Acquire()
        {
            if (Applied) return true;
            if (vehicle == null || vehicle.steering == null) return false;
            dashboard = vehicle.GetComponent<LwsTruckDashboardController>();
            wheel = dashboard != null ? dashboard.SteeringWheelVisual : null;
            if (wheel == null) wheel = vehicle.steering.steeringWheel;
            if (wheel == null)
            {
                Debug.LogWarning("TAXI STEERING VISUAL: no existing dashboard/NWH steering wheel is bound. No substitute geometry created.", this);
                return false;
            }
            originalNwhWheel = vehicle.steering.steeringWheel;
            originalDashboardAnimation = dashboard != null && dashboard.SteeringWheelAnimationEnabled;
            if (dashboard != null && dashboard.SteeringWheelVisual == wheel)
                neutralRotation = dashboard.SteeringWheelNeutralRotation;
            else
                neutralRotation = wheel.localRotation * Quaternion.Inverse(Quaternion.AngleAxis(vehicle.steering.angle * vehicle.steering.steeringWheelTurnRatio, Vector3.forward));
            // Release both existing visual writers. NWH's steering calculations and every other dashboard binding stay intact.
            if (dashboard != null) dashboard.SteeringWheelAnimationEnabled = false;
            vehicle.steering.steeringWheel = null;
            TargetVisualAngle = ResolveTargetAngle(ResolvedSteeringAngle, vehicle.steering.maximumSteerAngle, lockToLockDegrees);
            motion = new TruckTaxiSteeringVisualMotion(TargetVisualAngle);
            Applied = true; ApplyRotation();
            return true;
        }
        private void LateUpdate()
        {
            if (!Applied || vehicle == null || wheel == null || Time.deltaTime <= 0) return;
            TargetVisualAngle = ResolveTargetAngle(ResolvedSteeringAngle, vehicle.steering.maximumSteerAngle, lockToLockDegrees);
            bool returning = Mathf.Abs(TargetVisualAngle) < Mathf.Abs(motion.Angle) &&
                (Mathf.Sign(TargetVisualAngle) == Mathf.Sign(motion.Angle) || Mathf.Abs(TargetVisualAngle) < .01f);
            motion.Step(TargetVisualAngle, Time.deltaTime, returning ? returnToCenterResponse : steerVisualResponse,
                visualDamping, visualAngularVelocityLimit);
            ApplyRotation();
        }
        private void ApplyRotation()
        {
            Vector3 axis = localRotationAxis.sqrMagnitude > .001f ? localRotationAxis.normalized : Vector3.back;
            wheel.localRotation = neutralRotation * Quaternion.AngleAxis(motion.Angle, axis);
        }
        public void Restore()
        {
            if (!Applied) return;
            if (vehicle != null) vehicle.steering.steeringWheel = originalNwhWheel;
            if (dashboard != null) dashboard.SteeringWheelAnimationEnabled = originalDashboardAnimation;
            Applied = false;
        }
        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();

        public static float ResolveTargetAngle(float resolvedRoadAngle, float fullRoadLock, float visualLockToLock)
        {
            if (!Finite(resolvedRoadAngle) || !Finite(fullRoadLock) || !Finite(visualLockToLock) || fullRoadLock <= .001f) return 0;
            return Mathf.Clamp(resolvedRoadAngle / fullRoadLock, -1, 1) * Mathf.Max(0, visualLockToLock) * .5f;
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Continuous signed angles deliberately avoid LerpAngle/Quaternion interpolation across the 180/360-degree wrap.
    public struct TruckTaxiSteeringVisualMotion
    {
        public float Angle { get; private set; }
        public float Velocity { get; private set; }
        public TruckTaxiSteeringVisualMotion(float initialAngle) { Angle = initialAngle; Velocity = 0; }
        public void Step(float target, float deltaTime, float response, float damping, float velocityLimit)
        {
            if (!TruckTaxiSteeringWheelVisual.Finite(target) || !TruckTaxiSteeringWheelVisual.Finite(deltaTime) || deltaTime <= 0) return;
            if (!TruckTaxiSteeringWheelVisual.Finite(response) || !TruckTaxiSteeringWheelVisual.Finite(damping) || !TruckTaxiSteeringWheelVisual.Finite(velocityLimit)) return;
            float omega = 2 / Mathf.Max(.02f, response);
            damping = Mathf.Clamp(damping, 1, 2); velocityLimit = Mathf.Max(1, velocityLimit);
            // Bounded tiny integration steps keep the speed cap consistent at 30, 60 and 120 FPS and limit hitch jumps.
            deltaTime = Mathf.Min(deltaTime, .1f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime * 240 - .0001f));
            float dt = deltaTime / steps;
            for (int i = 0; i < steps; i++)
            {
                float offset = Angle - target, next, velocity;
                if (damping <= 1.0001f)
                {
                    float temp = Velocity + omega * offset, decay = Mathf.Exp(-omega * dt);
                    next = target + (offset + temp * dt) * decay;
                    velocity = (Velocity - omega * temp * dt) * decay;
                }
                else
                {
                    float root = Mathf.Sqrt(damping * damping - 1);
                    float r1 = -omega * (damping - root), r2 = -omega * (damping + root);
                    float a = (Velocity - r2 * offset) / (r1 - r2), b = offset - a;
                    float e1 = Mathf.Exp(r1 * dt), e2 = Mathf.Exp(r2 * dt);
                    next = target + a * e1 + b * e2;
                    velocity = r1 * a * e1 + r2 * b * e2;
                }
                Angle = Mathf.MoveTowards(Angle, next, velocityLimit * dt);
                Velocity = Mathf.Clamp(velocity, -velocityLimit, velocityLimit);
            }
        }
    }
}
