using System;

namespace LWS.TruckTaxi
{
    // Optional extension of the existing Pixel Crushers-backed Vibin participant, not a second save format.
    [Serializable]
    public sealed class TruckTaxiEnvironmentSnapshot
    {
        public int version = 1;
        public bool frozen;
        public bool hasSnowState;
        public TruckTaxiForecastSnapshot forecast;
        public TruckTaxiSnowSnapshot snow;

        public static bool Valid(TruckTaxiEnvironmentSnapshot value) => value == null ||
            value.version == 1 && TruckTaxiForecastSnapshot.Valid(value.forecast) &&
            value.forecast != null && (!value.hasSnowState || value.snow != null) && TruckTaxiSnowSnapshot.Valid(value.snow);
    }

    [Serializable]
    public sealed class TruckTaxiForecastSnapshot
    {
        public bool automatic;
        public TruckTaxiWeatherCondition manualCondition;
        public bool hasOverride;
        public long overrideStartTicks, overrideEndTicks;
        public TruckTaxiWeatherCondition overrideCondition;
        public TruckTaxiWeatherSeverity severity;
        public string warning;
        public bool severe;
        public float wind, visibility, rain;

        public static bool Valid(TruckTaxiForecastSnapshot value)
        {
            if (value == null) return true;
            if (!Enum.IsDefined(typeof(TruckTaxiWeatherCondition), value.manualCondition) ||
                !Enum.IsDefined(typeof(TruckTaxiWeatherCondition), value.overrideCondition) ||
                !Enum.IsDefined(typeof(TruckTaxiWeatherSeverity), value.severity)) return false;
            if (value.hasOverride && (value.overrideStartTicks < DateTime.MinValue.Ticks ||
                value.overrideEndTicks > DateTime.MaxValue.Ticks || value.overrideEndTicks <= value.overrideStartTicks)) return false;
            return !value.severe || value.hasOverride && float.IsFinite(value.wind) && value.wind >= 0 && value.wind <= 200 &&
                float.IsFinite(value.visibility) && value.visibility > 0 && value.visibility <= 100000 &&
                float.IsFinite(value.rain) && value.rain >= 0 && value.rain <= 1;
        }
    }

    [Serializable]
    public sealed class TruckTaxiSnowSnapshot
    {
        public int topology;
        public float maximumDepth, backgroundDepth;
        public float[] depths;
        public static bool Valid(TruckTaxiSnowSnapshot value)
        {
            if (value == null) return true;
            if (!float.IsFinite(value.maximumDepth) || value.maximumDepth <= 0 ||
                !float.IsFinite(value.backgroundDepth) || value.backgroundDepth < 0 || value.backgroundDepth > value.maximumDepth ||
                value.depths == null || value.depths.Length > 200000) return false;
            foreach (float depth in value.depths)
                if (!float.IsFinite(depth) || depth < 0 || depth > value.maximumDepth) return false;
            return true;
        }
    }
}
