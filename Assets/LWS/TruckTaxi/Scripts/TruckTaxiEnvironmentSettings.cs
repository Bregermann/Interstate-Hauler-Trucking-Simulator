using System;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiDayPeriod { Dawn, Morning, Day, Afternoon, Sunset, Night, LateNight }

    [Serializable]
    public sealed class TruckTaxiWeatherWeight
    {
        public string presetId = LwsWeatherPresetCatalog.ClearId;
        [Min(0)] public float weight = 1;
        public TruckTaxiWeatherWeight(string id, float chance) { presetId = id; weight = chance; }
    }

    [Serializable]
    public sealed class TruckTaxiWorldReaction
    {
        public TruckTaxiDialogueCategory category;
        [TextArea] public string calmPassenger;
        [TextArea] public string chaosPassenger;
        public TruckTaxiWorldReaction(TruckTaxiDialogueCategory category, string calm, string chaos)
        { this.category = category; calmPassenger = calm; chaosPassenger = chaos; }
    }

    [CreateAssetMenu(menuName = "Truck Taxi/Environment and Driver Needs")]
    public sealed class TruckTaxiEnvironmentSettings : ScriptableObject
    {
        [Header("LWS game clock (not Unity simulation speed)")]
        [Range(0, 23.99f)] public float startingTime = 16;
        [Header("In-game calendar")]
        [Range(1, 9999)] public int startYear = 2026;
        [Range(1, 12)] public int startMonth = 6;
        [Range(1, 31)] public int startDay = 1;
        public int forecastSeed = 71923;
        [Tooltip("30 game seconds per real second: a day takes 48 minutes; 6 game hours take 12 minutes.")]
        [Min(0)] public float timeScale = 30;
        public bool timeProgressionEnabled = true;
        [Header("Weather Maker through the existing LWS weather service")]
        public bool automaticWeather = true;
        [Tooltip("Taxi-scoped copy of the existing URP renderer with Weather Maker's installed render feature. Previous pipeline is restored on exit.")]
        public UnityEngine.Rendering.RenderPipelineAsset weatherRenderPipeline;
        public string startingWeather = LwsWeatherPresetCatalog.ClearId;
        [Min(5)] public float weatherTransitionSeconds = 25;
        [Tooltip("Weather holds for this range of IN-GAME hours, excluding pause/frozen time.")]
        public Vector2 weatherDurationGameHours = new Vector2(2, 5);
        [Min(0)] public float winterSnowWeightMultiplier = 4;
        [Min(0)] public float summerStormWeightMultiplier = 2;
        public TruckTaxiWeatherWeight[] weatherWeights =
        {
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.ClearId, 4),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.PartlyCloudyId, 3),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.OvercastId, 2),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.LightRainId, 2),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.HeavyRainId, 1),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.ThunderstormId, .5f),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.FogId, 1),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.LightSnowId, 1),
            new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.HeavySnowId, .5f),
            new TruckTaxiWeatherWeight(TruckTaxiSnow.BlizzardId, .2f)
        };
        [Header("Taxi snow (metres, real seconds)")]
        [Range(0.1f, 1f)] public float maximumSnowDepthMeters = .6f;
        [Min(0)] public float lightSnowMetersPerSecond = .0066667f;
        [Min(0)] public float heavySnowMetersPerSecond = .0171429f;
        [Min(0)] public float blizzardMetersPerSecond = .024f;
        [Min(0)] public float snowMeltMetersPerSecond = .01f;
        [Range(0, .6f)] public float plowResidualDepthMeters = .04f;
        [Min(0)] public float tireCompressionMetersPerMeter = .002f;
        [Range(128, 8192)] public int maximumSnowCells = 4096;
        [Header("Driver bladder")]
        [Tooltip("Time from empty to full in IN-GAME hours. Sampled once after each relief.")]
        public Vector2 bladderCycleGameHours = new Vector2(4, 8);
        [Range(0, 1)] public float startingBladder;
        [Min(1)] public float crisisGraceGameMinutes = 20;
        [Min(0)] public int spillCleanupCostCents = 500;
        [Header("Jug interaction: hold steady and hit three timed cues")]
        [Min(3)] public float jugDurationSeconds = 8;
        [Range(.08f, .45f)] public float stoppedCueHalfWindow = .23f;
        [Range(.03f, .3f)] public float drivingCueHalfWindow = .11f;
        [Min(.1f)] public float jugReleaseGraceSeconds = .8f;
        [Min(.5f)] public float lateralAccelerationSpillThreshold = 4.5f;
        [Min(0)] public float bathroomMaximumSpeed = .447f;
        [Header("Fallback reactions (passenger-authored dialogue always wins)")]
        public TruckTaxiWorldReaction[] fallbackReactions =
        {
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.RainReaction, "Looks like rain. Take your time.", "Rain makes the city look like a movie."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.StormReaction, "That thunder is a good reason to drive carefully.", "The sky brought its own sound effects."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.FogReaction, "I can barely see the next block.", "Mysterious. I approve of the atmosphere."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.NightReaction, "The city gets quieter after dark.", "Night shift. Now the interesting people come out."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.SunsetReaction, "Look at that sunset.", "Even this city knows how to make an entrance."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.BladderUrgent, "We can stop at a restroom. Really.", "This ride has developed an unexpected side quest."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.JugStarted, "Please tell me we are stopping first.", "That is certainly one way to manage a schedule."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.JugSucceeded, "I would still prefer a restroom next time.", "Flawless technique. A very unusual talent."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.JugSpilled, "We need a cleanup stop. Immediately.", "Well. That story is going in the review."),
            new TruckTaxiWorldReaction(TruckTaxiDialogueCategory.BathroomStop, "Much better. Ready when you are.", "Pit stop complete. Back to the adventure.")
        };
        public string ReactionFor(TruckTaxiDialogueCategory category, PassengerProfile passenger)
        {
            if (passenger == null || fallbackReactions == null) return null;
            foreach (var reaction in fallbackReactions) if (reaction != null && reaction.category == category)
                return passenger.chaosAffinity > 0 ? reaction.chaosPassenger : reaction.calmPassenger;
            return null;
        }

        public static TruckTaxiDayPeriod PeriodAt(float hours)
        {
            hours = LwsGameClockUtility.NormalizeHours(hours);
            if (hours < 5) return TruckTaxiDayPeriod.LateNight;
            if (hours < 7) return TruckTaxiDayPeriod.Dawn;
            if (hours < 11) return TruckTaxiDayPeriod.Morning;
            if (hours < 14) return TruckTaxiDayPeriod.Day;
            if (hours < 18) return TruckTaxiDayPeriod.Afternoon;
            return hours < 20 ? TruckTaxiDayPeriod.Sunset : TruckTaxiDayPeriod.Night;
        }
    }
}
