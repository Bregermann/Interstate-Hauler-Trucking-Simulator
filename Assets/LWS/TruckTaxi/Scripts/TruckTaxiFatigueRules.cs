using System;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiFatigueActivity { Parked, Driving, Stressful }
    public enum TruckTaxiFatigueVisualMode { Full, Reduced }

    public static class TruckTaxiFatigueRules
    {
        public const float MicrosleepThreshold = .8f;
        public static float HourlyRate(TruckTaxiFatigueActivity activity) =>
            activity == TruckTaxiFatigueActivity.Parked ? .04f : activity == TruckTaxiFatigueActivity.Stressful ? .0485f : .044f;

        // Integrate midnight-5AM overlap so a large clock step has the same result as small steps.
        public static float Gain(double seconds, TruckTaxiFatigueActivity activity, float startHour)
        {
            if (!double.IsFinite(seconds) || seconds <= 0 || !float.IsFinite(startHour)) return 0;
            double start = ((startHour % 24) + 24) % 24;
            double end = start + seconds / 3600;
            double night = NightHours(end) - NightHours(start);
            return (float)Math.Min(float.MaxValue, HourlyRate(activity) * (seconds / 3600 + night * .1));
        }
        private static double NightHours(double hour) => Math.Floor(hour / 24) * 5 + Math.Min(5, hour % 24);
        public static bool IsSleepDuration(int hours) => hours == 2 || hours == 4 || hours == 8;
        public static float SleepRecovery(int hours) => hours == 2 ? .25f : hours == 4 ? .55f : hours == 8 ? 1 : 0;
        public static bool CanSleepInSession(TruckTaxiState state) => state == TruckTaxiState.Available;
        public static float VisualSeverity(float fatigue) => float.IsFinite(fatigue) && fatigue >= MicrosleepThreshold ?
            UnityEngine.Mathf.Clamp01((fatigue - MicrosleepThreshold) / .2f) : 0;
        public static float MinimumEffectInterval(float fatigue) => UnityEngine.Mathf.Lerp(50, 8, VisualSeverity(fatigue));
    }
}
