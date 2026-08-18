using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(
        fileName = "IH_FloatingOriginTuning",
        menuName = "Interstate Hauler/World/Floating Origin Tuning")]
    public sealed class LwsFloatingOriginTuning : ScriptableObject
    {
        public bool floatingOriginEnabled = true;
        public float shiftThresholdMeters = 750f;
        public float shiftGridMeters = 500f;
        public bool shiftXAxis = true;
        public bool shiftYAxis;
        public bool shiftZAxis = true;
        public float minimumSecondsBetweenShifts = 0.35f;
        public bool developmentLogging = true;
        public bool syncPhysicsTransforms = true;
        public bool addDebugPanel = true;

        public static LwsFloatingOriginTuning CreateRuntimeDefault()
        {
            LwsFloatingOriginTuning tuning = CreateInstance<LwsFloatingOriginTuning>();
            tuning.name = "IH Floating Origin Runtime Defaults";
            return tuning;
        }

        public bool Validate(out string message)
        {
            if (shiftThresholdMeters <= 0f)
            {
                message = "Floating-origin shift threshold must be greater than zero.";
                return false;
            }

            if (shiftGridMeters <= 0f)
            {
                message = "Floating-origin shift grid must be greater than zero.";
                return false;
            }

            if (!shiftXAxis && !shiftYAxis && !shiftZAxis)
            {
                message = "Floating-origin tuning must shift at least one axis.";
                return false;
            }

            if (shiftGridMeters > shiftThresholdMeters * 2f)
            {
                message = "Floating-origin shift grid is unusually large compared with the threshold.";
                return false;
            }

            message = "Floating-origin tuning is valid.";
            return true;
        }

        private void OnValidate()
        {
            shiftThresholdMeters = Mathf.Max(1f, shiftThresholdMeters);
            shiftGridMeters = Mathf.Max(1f, shiftGridMeters);
            minimumSecondsBetweenShifts = Mathf.Max(0f, minimumSecondsBetweenShifts);
        }
    }
}
