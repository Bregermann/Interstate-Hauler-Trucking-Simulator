using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiWeatherCondition
    {
        Clear, PartlyCloudy, Overcast, Rain, HeavyRain, Thunderstorm, Fog, Snow, HeavySnow, Blizzard
    }

    public enum TruckTaxiWeatherSeverity { None, Advisory, Warning, Emergency }

    public sealed class TruckTaxiForecastEntry
    {
        public DateTime Start { get; }
        public DateTime End { get; }
        public TruckTaxiWeatherCondition Condition { get; }
        public string PresetId => TruckTaxiCalendarWeatherService.PresetFor(Condition);
        public float HighCelsius { get; }
        public float LowCelsius { get; }
        public float PrecipitationChance01 { get; }
        public float WindMetersPerSecond { get; }
        public float VisibilityMeters { get; }
        public float SevereRisk01 { get; }
        public TruckTaxiWeatherSeverity Severity { get; }
        public string Warning { get; }

        internal TruckTaxiForecastEntry(DateTime start, DateTime end, TruckTaxiWeatherCondition condition,
            float high, float low, float precipitation, float wind, float visibility, float risk,
            TruckTaxiWeatherSeverity severity, string warning)
        {
            Start = start; End = end; Condition = condition;
            HighCelsius = high; LowCelsius = low; PrecipitationChance01 = precipitation;
            WindMetersPerSecond = wind; VisibilityMeters = visibility; SevereRisk01 = risk;
            Severity = severity; Warning = warning ?? string.Empty;
        }

        internal TruckTaxiForecastEntry Slice(DateTime start, DateTime end) =>
            new TruckTaxiForecastEntry(start, end, Condition, HighCelsius, LowCelsius, PrecipitationChance01,
                WindMetersPerSecond, VisibilityMeters, SevereRisk01, Severity, Warning);
    }

    // The LWS clock is the only clock; this service owns the forecast and its actual-weather requests.
    public sealed class TruckTaxiCalendarWeatherService : IDisposable
    {
        private static readonly TruckTaxiWeatherCondition[] Conditions =
        {
            TruckTaxiWeatherCondition.Clear, TruckTaxiWeatherCondition.PartlyCloudy,
            TruckTaxiWeatherCondition.Overcast, TruckTaxiWeatherCondition.Rain,
            TruckTaxiWeatherCondition.HeavyRain, TruckTaxiWeatherCondition.Thunderstorm,
            TruckTaxiWeatherCondition.Fog, TruckTaxiWeatherCondition.Snow,
            TruckTaxiWeatherCondition.HeavySnow, TruckTaxiWeatherCondition.Blizzard
        };

        private ILwsGameClockService clock;
        private TruckTaxiEnvironmentSettings settings;
        private Func<string, bool> applyWeather;
        private Func<TruckTaxiExtremeWeatherProfile,bool> applySevereWeather;
        private TruckTaxiExtremeWeatherProfile? severeProfile;
        private readonly List<TruckTaxiForecastEntry> entries = new List<TruckTaxiForecastEntry>();
        private IReadOnlyList<TruckTaxiForecastEntry> forecast;
        private DateTime windowStart;
        private DateTime overrideStart, overrideEnd;
        private TruckTaxiWeatherCondition overrideCondition, manualCondition;
        private TruckTaxiWeatherSeverity overrideSeverity;
        private string overrideWarning;
        private bool hasOverride, automatic = true, initialized;
        private string lastAppliedPreset;
        private DateTime lastSeen;
        private TruckTaxiForecastEntry current;

        public DateTime CurrentDateTime => clock == null ? default : SnapshotDateTime(clock.CurrentSnapshot);
        public DateTime StartDate { get; private set; }
        public int DayIndex => clock == null ? 0 : (CurrentDateTime.Date - StartDate.Date).Days;
        public DayOfWeek DayOfWeek => CurrentDateTime.DayOfWeek;
        public IReadOnlyList<TruckTaxiForecastEntry> Forecast => forecast ?? (forecast = entries.AsReadOnly());
        public TruckTaxiForecastEntry Current => current;
        public TruckTaxiWeatherCondition CurrentCondition => current == null ? TruckTaxiWeatherCondition.Clear : current.Condition;
        public TruckTaxiWeatherSeverity CurrentSeverity => current == null ? TruckTaxiWeatherSeverity.None : current.Severity;
        public string CurrentWarning => current == null ? string.Empty : current.Warning;
        public DateTime? NextChange
        {
            get
            {
                if (current == null) return null;
                foreach (var entry in entries)
                    if (entry.Start >= current.End && entry.Condition != current.Condition) return entry.Start;
                return null;
            }
        }
        public float WeatherFareMultiplier => current == null ? 1f : FareFactor(current.Condition);
        public float DemandMultiplier => current == null ? 1f : DemandFactor(current.Condition);
        public bool AutomaticWeather => automatic;
        public bool IsInitialized => initialized;
        public string LastDiagnostic { get; private set; } = "Not initialized";
        public event Action ForecastChanged;

        public bool Initialize(TruckTaxiEnvironmentCoordinator environment, TruckTaxiEnvironmentSettings configuration = null)
        {
            if (environment == null || environment.Clock == null) return false;
            applySevereWeather=environment.ApplySevereWeather;
            return Initialize(environment.Clock, configuration ?? environment.settings, environment.ApplyScheduledWeather);
        }

        // Injectable weather request allows focused schedule tests without a scene or vendor runtime.
        public bool Initialize(ILwsGameClockService gameClock, TruckTaxiEnvironmentSettings configuration, Func<string, bool> weatherRequest)
        {
            if (initialized) return true;
            if (gameClock == null || configuration == null || weatherRequest == null) return false;
            clock = gameClock; settings = configuration; applyWeather = weatherRequest;
            StartDate = SafeStartDate(settings);
            lastSeen = CurrentDateTime;
            automatic = settings.automaticWeather;
            manualCondition = ConditionFor(settings.startingWeather);
            forecast = entries.AsReadOnly();
            initialized = true;
            clock.ClockChanged += OnClockChanged;
            RebuildWindow();
            if (automatic) ForceForecast(manualCondition);
            else ApplyCurrent();
            return true;
        }

        public static DateTime SafeStartDate(TruckTaxiEnvironmentSettings value)
        {
            int year = Mathf.Clamp(value.startYear, 1, 9999);
            int month = Mathf.Clamp(value.startMonth, 1, 12);
            return new DateTime(year, month, Mathf.Clamp(value.startDay, 1, DateTime.DaysInMonth(year, month)));
        }

        public static DateTime SnapshotDateTime(LwsGameClockSnapshot snapshot) =>
            new LwsGameDateTime(snapshot.year, snapshot.month, snapshot.day, snapshot.hour, snapshot.minute, snapshot.second).ToDateTime();

        public static string PresetFor(TruckTaxiWeatherCondition condition)
        {
            switch (condition)
            {
                case TruckTaxiWeatherCondition.PartlyCloudy: return LwsWeatherPresetCatalog.PartlyCloudyId;
                case TruckTaxiWeatherCondition.Overcast: return LwsWeatherPresetCatalog.OvercastId;
                case TruckTaxiWeatherCondition.Rain: return LwsWeatherPresetCatalog.LightRainId;
                case TruckTaxiWeatherCondition.HeavyRain: return LwsWeatherPresetCatalog.HeavyRainId;
                case TruckTaxiWeatherCondition.Thunderstorm: return LwsWeatherPresetCatalog.ThunderstormId;
                case TruckTaxiWeatherCondition.Fog: return LwsWeatherPresetCatalog.FogId;
                case TruckTaxiWeatherCondition.Snow: return LwsWeatherPresetCatalog.LightSnowId;
                case TruckTaxiWeatherCondition.HeavySnow: return LwsWeatherPresetCatalog.HeavySnowId;
                case TruckTaxiWeatherCondition.Blizzard: return TruckTaxiSnow.BlizzardId;
                default: return LwsWeatherPresetCatalog.ClearId;
            }
        }

        public static TruckTaxiWeatherCondition ConditionFor(string preset)
        {
            if (preset == LwsWeatherPresetCatalog.CloudyId) return TruckTaxiWeatherCondition.Overcast;
            foreach (var condition in Conditions) if (PresetFor(condition) == preset) return condition;
            return TruckTaxiWeatherCondition.Clear;
        }

        public bool ForceForecast(TruckTaxiWeatherCondition condition)
        {
            severeProfile=null;
            if (!initialized || !Enum.IsDefined(typeof(TruckTaxiWeatherCondition), condition)) return false;
            if (!automatic)
            {
                manualCondition = condition;
                hasOverride = false;
                RebuildWindow();
                return ApplyCurrent();
            }
            DateTime now = CurrentDateTime;
            DateTime end = current != null && current.End > now ? current.End : now.AddHours(1);
            return ForceCurrent(condition, (float)(end - now).TotalHours);
        }

        public bool ForceCurrent(TruckTaxiWeatherCondition condition, float durationGameHours, string warning = null,
            TruckTaxiWeatherSeverity severity = TruckTaxiWeatherSeverity.None)
        {
            return ForceCurrentInternal(condition,durationGameHours,warning,severity,null);
        }
        private bool ForceCurrentInternal(TruckTaxiWeatherCondition condition,float durationGameHours,string warning,
            TruckTaxiWeatherSeverity severity,TruckTaxiExtremeWeatherProfile? profile)
        {
            if (!initialized || !Enum.IsDefined(typeof(TruckTaxiWeatherCondition), condition) ||
                !Enum.IsDefined(typeof(TruckTaxiWeatherSeverity), severity) ||
                !float.IsFinite(durationGameHours) || durationGameHours <= 0) return false;
            DateTime now = CurrentDateTime;
            DateTime end = now.AddHours(Mathf.Max(.01f, durationGameHours));
            if (!(profile.HasValue && applySevereWeather!=null ? applySevereWeather(profile.Value) : applyWeather(PresetFor(condition))))
            {
                LastDiagnostic = "Weather request was rejected; forecast was not overridden.";
                return false;
            }
            severeProfile=profile;
            hasOverride = true; overrideStart = now; overrideEnd = end; overrideCondition = condition;
            overrideWarning = warning;
            overrideSeverity = severity != TruckTaxiWeatherSeverity.None ? severity :
                condition == TruckTaxiWeatherCondition.Blizzard ? TruckTaxiWeatherSeverity.Emergency :
                condition == TruckTaxiWeatherCondition.Thunderstorm || condition == TruckTaxiWeatherCondition.HeavySnow
                    ? TruckTaxiWeatherSeverity.Warning :
                condition == TruckTaxiWeatherCondition.HeavyRain || condition == TruckTaxiWeatherCondition.Fog ||
                !string.IsNullOrEmpty(warning) ? TruckTaxiWeatherSeverity.Advisory : TruckTaxiWeatherSeverity.None;
            lastAppliedPreset = PresetFor(condition)+(severeProfile.HasValue ? ":severe" : "");
            RebuildWindow();
            LastDiagnostic = "Current forecast override applied.";
            return true;
        }

        public void SetAutomaticWeather(bool enabled)
        {
            if (!initialized || automatic == enabled) return;
            manualCondition = current != null ? current.Condition : TruckTaxiWeatherCondition.Clear;
            automatic = enabled; hasOverride = false; severeProfile=null;
            RebuildWindow(); ApplyCurrent();
        }

        public bool SetDate(int year, int month, int day)
        {
            if (clock == null || year < 1 || year > 9999 || month < 1 || month > 12 ||
                day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
            var date = new DateTime(year, month, day);
            clock.SetDateTime(LwsGameDateTime.FromDateTime(date.Add(CurrentDateTime.TimeOfDay)));
            return true;
        }

        public void SetTime(float hours) { clock?.SetTimeOfDayHours(hours); }
        public void AdvanceDay()
        {
            if (clock != null && CurrentDateTime < DateTime.MaxValue.AddDays(-1))
                clock.SetDateTime(LwsGameDateTime.FromDateTime(CurrentDateTime.AddDays(1)));
        }

        public bool ForceSevereStorm(float hours,TruckTaxiExtremeWeatherProfile profile)
        {
            return ForceCurrentInternal(TruckTaxiWeatherCondition.Thunderstorm,hours,"Hurricane warning",TruckTaxiWeatherSeverity.Emergency,profile);
        }
        public void ClearSevereOverride()
        {
            if(!severeProfile.HasValue) return;
            severeProfile=null; hasOverride=false; lastAppliedPreset=null;
            RebuildWindow(); ApplyCurrent();
        }

        private void OnClockChanged(LwsGameClockSnapshot snapshot)
        {
            DateTime now = SnapshotDateTime(snapshot);
            bool rewound = now < lastSeen;
            if (rewound) { hasOverride = false; severeProfile=null; }
            lastSeen = now;
            if (rewound || windowStart != now.Date || current == null || now < current.Start || now >= current.End)
            {
                bool rebuild = rewound || windowStart != now.Date;
                if (rebuild) RebuildWindow();
                else SelectCurrent();
                ApplyCurrent();
                if (!rebuild) ForecastChanged?.Invoke();
            }
        }

        private void RebuildWindow()
        {
            DateTime today = CurrentDateTime.Date;
            windowStart = today;
            entries.Clear();
            for (int day = 0; day < 7; day++)
            {
                DateTime date;
                try { date = today.AddDays(day); }
                catch (ArgumentOutOfRangeException) { break; }
                GenerateDay(date);
            }
            if (hasOverride && overrideEnd > today && overrideStart < entries[entries.Count - 1].End)
                OverlayOverride();
            SelectCurrent();
            ForecastChanged?.Invoke();
        }

        private void GenerateDay(DateTime date)
        {
            var random = new System.Random(unchecked(settings.forecastSeed * 397 ^ date.Year * 10000 ^ date.Month * 100 ^ date.Day));
            DateTime cursor = date, end = date == DateTime.MaxValue.Date ? DateTime.MaxValue : date.AddDays(1);
            float low = SeasonalBaseTemperature(date.Month) - 5f + (float)random.NextDouble() * 4f;
            float high = low + 7f + (float)random.NextDouble() * 5f;
            // Each date is independently seeded so tomorrow cannot change when the seven-day window rolls.
            TruckTaxiWeatherCondition previous = TruckTaxiWeatherCondition.Clear;
            while (cursor < end)
            {
                float min = Mathf.Max(.25f, settings.weatherDurationGameHours.x);
                float max = Mathf.Max(min, settings.weatherDurationGameHours.y);
                double hours = min + (max - min) * random.NextDouble();
                DateTime next = (end - cursor).TotalHours <= hours ? end : cursor.AddHours(hours);
                var condition = automatic ? ChooseCondition(date.Month, previous, random) : manualCondition;
                entries.Add(CreateEntry(cursor, next, condition, high, low, null, TruckTaxiWeatherSeverity.None));
                previous = condition; cursor = next;
            }
        }

        private TruckTaxiWeatherCondition ChooseCondition(int month, TruckTaxiWeatherCondition previous, System.Random random)
        {
            double total = 0;
            var weights = new double[Conditions.Length];
            for (int i = 0; i < Conditions.Length; i++)
            {
                string id = PresetFor(Conditions[i]);
                float baseWeight = 0;
                if (settings.weatherWeights != null)
                    foreach (var item in settings.weatherWeights)
                        if (item != null && item.presetId == id && float.IsFinite(item.weight)) baseWeight += Mathf.Max(0, item.weight);
                double factor = 1;
                bool snow = Conditions[i] == TruckTaxiWeatherCondition.Snow ||
                    Conditions[i] == TruckTaxiWeatherCondition.HeavySnow || Conditions[i] == TruckTaxiWeatherCondition.Blizzard;
                if (snow) factor = month == 12 || month <= 2 ? Mathf.Max(0, settings.winterSnowWeightMultiplier) :
                    month >= 6 && month <= 8 ? 0 : .25;
                if (Conditions[i] == TruckTaxiWeatherCondition.Thunderstorm && month >= 6 && month <= 8)
                    factor = Mathf.Max(0, settings.summerStormWeightMultiplier);
                if (Conditions[i] == previous) factor *= .35;
                weights[i] = baseWeight * factor; total += weights[i];
            }
            if (total <= 0) return TruckTaxiWeatherCondition.Clear;
            double roll = random.NextDouble() * total;
            for (int i = 0; i < weights.Length; i++) if ((roll -= weights[i]) < 0) return Conditions[i];
            return Conditions[Conditions.Length - 1];
        }

        private void OverlayOverride()
        {
            var replacement = new List<TruckTaxiForecastEntry>(entries.Count + 2);
            foreach (var entry in entries)
            {
                if (entry.End <= overrideStart || entry.Start >= overrideEnd) { replacement.Add(entry); continue; }
                DateTime start = entry.Start > overrideStart ? entry.Start : overrideStart;
                DateTime end = entry.End < overrideEnd ? entry.End : overrideEnd;
                if (entry.Start < start) replacement.Add(entry.Slice(entry.Start, start));
                var overridden=CreateEntry(start, end, overrideCondition, entry.HighCelsius, entry.LowCelsius,overrideWarning, overrideSeverity);
                if(severeProfile.HasValue)
                    overridden=new TruckTaxiForecastEntry(start,end,overrideCondition,entry.HighCelsius,entry.LowCelsius,
                        severeProfile.Value.RainIntensity01,severeProfile.Value.WindMetersPerSecond,severeProfile.Value.VisibilityMeters,1,overrideSeverity,overrideWarning);
                replacement.Add(overridden);
                if (end < entry.End) replacement.Add(entry.Slice(end, entry.End));
            }
            entries.Clear(); entries.AddRange(replacement);
        }

        private void SelectCurrent()
        {
            DateTime now = CurrentDateTime;
            current = null;
            foreach (var entry in entries) if (entry.Start <= now && now < entry.End) { current = entry; break; }
        }

        private bool ApplyCurrent()
        {
            if (current == null) return false;
            bool severe=severeProfile.HasValue && hasOverride && CurrentDateTime>=overrideStart && CurrentDateTime<overrideEnd;
            string key=current.PresetId+(severe ? ":severe" : "");
            if (lastAppliedPreset == key) return true;
            if (!(severe && applySevereWeather!=null ? applySevereWeather(severeProfile.Value) : applyWeather(current.PresetId)))
            {
                LastDiagnostic = "Scheduled weather request failed: " + current.PresetId;
                return false;
            }
            lastAppliedPreset = key;
            LastDiagnostic = "Scheduled weather: " + current.PresetId;
            return true;
        }

        private static TruckTaxiForecastEntry CreateEntry(DateTime start, DateTime end, TruckTaxiWeatherCondition condition,
            float high, float low, string warning, TruckTaxiWeatherSeverity requestedSeverity)
        {
            string preset = PresetFor(condition);
            LwsWeatherPresetCatalog.TryGetBuiltInPreset(preset, out var data);
            if (condition == TruckTaxiWeatherCondition.Blizzard) data = TruckTaxiEnvironmentCoordinator.CreateBlizzardPreset();
            float precip = condition == TruckTaxiWeatherCondition.Thunderstorm ? .95f :
                condition == TruckTaxiWeatherCondition.HeavyRain || condition == TruckTaxiWeatherCondition.HeavySnow ||
                condition == TruckTaxiWeatherCondition.Blizzard ? .85f :
                condition == TruckTaxiWeatherCondition.Rain || condition == TruckTaxiWeatherCondition.Snow ? .55f : .05f;
            float risk = condition == TruckTaxiWeatherCondition.Blizzard ? 1f :
                condition == TruckTaxiWeatherCondition.Thunderstorm ? .8f :
                condition == TruckTaxiWeatherCondition.HeavySnow ? .6f :
                condition == TruckTaxiWeatherCondition.HeavyRain ? .4f : 0f;
            var severity = requestedSeverity != TruckTaxiWeatherSeverity.None ? requestedSeverity :
                risk >= .8f ? TruckTaxiWeatherSeverity.Warning : risk >= .4f ? TruckTaxiWeatherSeverity.Advisory : TruckTaxiWeatherSeverity.None;
            string alert = warning ?? (condition == TruckTaxiWeatherCondition.Blizzard ? "Blizzard warning" :
                condition == TruckTaxiWeatherCondition.Thunderstorm ? "Thunderstorm warning" :
                severity == TruckTaxiWeatherSeverity.Advisory ? "Hazardous conditions" : string.Empty);
            return new TruckTaxiForecastEntry(start, end, condition, high, low, precip,
                data.windSpeedMetersPerSecond, data.visibilityMeters, risk, severity, alert);
        }

        private static float SeasonalBaseTemperature(int month) => month == 12 || month <= 2 ? 1f :
            month >= 6 && month <= 8 ? 28f : month >= 3 && month <= 5 ? 17f : 14f;
        private static float FareFactor(TruckTaxiWeatherCondition value) => value == TruckTaxiWeatherCondition.Blizzard ? 1.5f :
            value == TruckTaxiWeatherCondition.Thunderstorm || value == TruckTaxiWeatherCondition.HeavySnow ? 1.3f :
            value == TruckTaxiWeatherCondition.HeavyRain || value == TruckTaxiWeatherCondition.Snow ? 1.2f :
            value == TruckTaxiWeatherCondition.Rain || value == TruckTaxiWeatherCondition.Fog ? 1.1f : 1f;
        private static float DemandFactor(TruckTaxiWeatherCondition value) => value == TruckTaxiWeatherCondition.Blizzard ? .65f :
            value == TruckTaxiWeatherCondition.Thunderstorm ? .85f :
            value == TruckTaxiWeatherCondition.Rain || value == TruckTaxiWeatherCondition.HeavyRain ? 1.2f : 1f;

        public void Dispose()
        {
            if (clock != null) clock.ClockChanged -= OnClockChanged;
            initialized = false; clock = null; applyWeather = null;
        }
    }
}
