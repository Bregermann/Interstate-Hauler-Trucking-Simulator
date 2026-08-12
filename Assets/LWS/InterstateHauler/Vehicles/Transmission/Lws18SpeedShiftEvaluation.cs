using UnityEngine;

namespace LWS.InterstateHauler
{
    public static class Lws18SpeedShiftEvaluation
    {
        public static bool IsClutchDepressed(float clutchInput, float depressedThreshold)
        {
            return Mathf.Clamp01(clutchInput) >= Mathf.Clamp01(depressedThreshold);
        }

        public static float PredictRpmFromRatioChange(
            float currentEngineRpm,
            float currentTotalRatio,
            float targetTotalRatio,
            int currentNwhGear)
        {
            if (currentNwhGear == 0 ||
                currentEngineRpm <= 1f ||
                Mathf.Abs(currentTotalRatio) <= 0.0001f ||
                Mathf.Abs(targetTotalRatio) <= 0.0001f)
            {
                return Mathf.Max(0f, currentEngineRpm);
            }

            return Mathf.Abs(currentEngineRpm * targetTotalRatio / currentTotalRatio);
        }

        public static bool IsRpmSynchronized(float currentRpm, float predictedTargetRpm, float tolerance)
        {
            return Mathf.Abs(predictedTargetRpm - currentRpm) <= Mathf.Max(0f, tolerance);
        }

        public static LwsTransmissionAbuseSeverity ClassifyOverspeed(
            float predictedRpm,
            float revLimiterRpm,
            float severeMultiplier,
            float catastrophicMultiplier)
        {
            if (predictedRpm <= 0f || revLimiterRpm <= 0f)
            {
                return LwsTransmissionAbuseSeverity.None;
            }

            if (predictedRpm >= revLimiterRpm * catastrophicMultiplier)
            {
                return LwsTransmissionAbuseSeverity.CatastrophicRisk;
            }

            if (predictedRpm >= revLimiterRpm * severeMultiplier)
            {
                return LwsTransmissionAbuseSeverity.Severe;
            }

            return LwsTransmissionAbuseSeverity.None;
        }
    }
}
