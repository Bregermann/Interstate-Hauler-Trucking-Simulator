using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Input/Wheel Calibration Template", fileName = "IH_WheelCalibrationTemplate")]
    public sealed class LwsWheelCalibrationAsset : ScriptableObject
    {
        [SerializeField] private LwsWheelCalibrationProfile profile = LwsWheelCalibrationProfile.CreateDefaultLogitechG29();

        public LwsWheelCalibrationProfile Profile => profile;

        public bool Validate(out string message)
        {
            if (profile == null)
            {
                message = $"{name} has no wheel calibration profile.";
                return false;
            }

            return profile.ValidateRanges(out message);
        }
    }
}
