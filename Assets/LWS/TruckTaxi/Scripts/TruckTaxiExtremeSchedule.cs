using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiExtremeScheduleSettings
    {
        [Range(0, 1)] public float tornadoDailyChance = .55f;
        [Range(0, 1)] public float earthquakeDailyChance = .29f;
        [Range(0, 1)] public float avalancheDailyChance = .40f;
        [Range(0, 1)] public float mudslideDailyChance = .40f;
        [Min(1)] public int tornadoMaximumEligibleDays = 3;
        [Min(1)] public int earthquakeMaximumDays = 5;
        [Min(1)] public int avalancheMaximumEligibleDays = 4;
        [Min(1)] public int mudslideMaximumEligibleDays = 4;

        public float Chance(TruckTaxiExtremeKind kind)
        {
            switch (kind)
            {
                case TruckTaxiExtremeKind.Tornado: return Mathf.Clamp01(tornadoDailyChance);
                case TruckTaxiExtremeKind.Earthquake: return Mathf.Clamp01(earthquakeDailyChance);
                case TruckTaxiExtremeKind.Avalanche: return Mathf.Clamp01(avalancheDailyChance);
                case TruckTaxiExtremeKind.Mudslide: return Mathf.Clamp01(mudslideDailyChance);
                default: return TruckTaxiExtremeRules.DailyChance(kind);
            }
        }

        public int MaximumDays(TruckTaxiExtremeKind kind)
        {
            switch (kind)
            {
                case TruckTaxiExtremeKind.Tornado: return Mathf.Max(1, tornadoMaximumEligibleDays);
                case TruckTaxiExtremeKind.Earthquake: return Mathf.Max(1, earthquakeMaximumDays);
                case TruckTaxiExtremeKind.Avalanche: return Mathf.Max(1, avalancheMaximumEligibleDays);
                case TruckTaxiExtremeKind.Mudslide: return Mathf.Max(1, mudslideMaximumEligibleDays);
                default: return int.MaxValue;
            }
        }
    }

    public sealed class TruckTaxiExtremeDaySchedule
    {
        private readonly TruckTaxiExtremeScheduleSettings settings;
        private readonly DateTime[] lastAttempt = new DateTime[5];
        private readonly int[] eligibleDryDays = new int[5];
        private readonly DateTime[] lastOccurrence = new DateTime[5];

        public TruckTaxiExtremeDaySchedule(TruckTaxiExtremeScheduleSettings settings)
        { this.settings = settings ?? new TruckTaxiExtremeScheduleSettings(); }

        public int DryDays(TruckTaxiExtremeKind kind) => eligibleDryDays[(int)kind];
        public DateTime LastOccurrence(TruckTaxiExtremeKind kind) => lastOccurrence[(int)kind];
        public float CurrentProbability(TruckTaxiExtremeKind kind, bool eligible)
        {
            if (!eligible) return 0;
            int index = (int)kind;
            if (eligibleDryDays[index] + 1 >= settings.MaximumDays(kind)) return 1;
            return Mathf.Clamp01(settings.Chance(kind) * (1 + .35f * eligibleDryDays[index]));
        }

        public bool TryDay(TruckTaxiExtremeKind kind, DateTime day, bool eligible, float roll,
            out string reason)
        {
            int index = (int)kind;
            day = day.Date;
            if (lastAttempt[index] == day) { reason = "ALREADY_ATTEMPTED"; return false; }
            if (lastAttempt[index] > day) { reason = "CLOCK_REWIND"; return false; }
            if (!eligible) { reason = "WRONG_WEATHER"; return false; }
            lastAttempt[index] = day;
            float probability = CurrentProbability(kind, true);
            if (!float.IsFinite(roll) || Mathf.Clamp01(roll) >= probability)
            {
                eligibleDryDays[index]++;
                reason = "ROLL_FAILED";
                return false;
            }
            reason = null;
            return true;
        }

        public void RecordStart(TruckTaxiExtremeKind kind, DateTime day)
        {
            lastOccurrence[(int)kind] = day;
            eligibleDryDays[(int)kind] = 0;
        }

        public void Reset()
        {
            Array.Clear(lastAttempt, 0, lastAttempt.Length);
            Array.Clear(lastOccurrence, 0, lastOccurrence.Length);
            Array.Clear(eligibleDryDays, 0, eligibleDryDays.Length);
        }

        public TruckTaxiExtremeDayState Capture()
        {
            var value = new TruckTaxiExtremeDayState();
            for (int i = 0; i < 5; i++)
            { value.attempts[i] = lastAttempt[i].Ticks; value.occurrences[i] = lastOccurrence[i].Ticks; value.dryDays[i] = eligibleDryDays[i]; }
            return value;
        }
        public bool Restore(TruckTaxiExtremeDayState value)
        {
            if (!TruckTaxiExtremeDayState.Valid(value)) return false;
            Reset();
            if (value != null) for (int i = 0; i < 5; i++)
            { lastAttempt[i] = new DateTime(value.attempts[i]); lastOccurrence[i] = new DateTime(value.occurrences[i]); eligibleDryDays[i] = value.dryDays[i]; }
            return true;
        }
    }
}
