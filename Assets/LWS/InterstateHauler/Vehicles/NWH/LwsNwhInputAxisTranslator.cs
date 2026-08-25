using UnityEngine;

namespace LWS.InterstateHauler
{
    public static class LwsNwhInputAxisTranslator
    {
        public static LwsVehicleContinuousInput TranslateSemanticInputForNwh(
            LwsVehicleContinuousInput semanticInput,
            bool swapThrottleBrakeInReverse,
            bool reverseDriveState)
        {
            LwsVehicleContinuousInput nwhInput = semanticInput;
            nwhInput.steering = Mathf.Clamp(semanticInput.steering, -1f, 1f);
            nwhInput.throttle = Mathf.Clamp01(semanticInput.throttle);
            nwhInput.brake = Mathf.Clamp01(semanticInput.brake);
            nwhInput.clutch = Mathf.Clamp01(semanticInput.clutch);
            nwhInput.parkingBrake = Mathf.Clamp01(semanticInput.parkingBrake);

            if (swapThrottleBrakeInReverse && reverseDriveState)
            {
                nwhInput.throttle = Mathf.Clamp01(semanticInput.brake);
                nwhInput.brake = Mathf.Clamp01(semanticInput.throttle);
            }

            return nwhInput;
        }
    }
}
