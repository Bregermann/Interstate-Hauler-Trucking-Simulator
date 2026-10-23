using System;
using UnityEngine;

namespace LWS.TruckSports
{
    public enum SportsTimeChoice { Random, Morning, Day, Evening, Night }

    // Match semantics only. Weather Maker computes the actual celestial/ambient lighting.
    public static class SportsTimeOfDay
    {
        public const string PreferenceKey = "TruckSports.Environment.TimeOfDay.v1";
        public static SportsTimeChoice Selection
        {
            get
            {
                int value = SportsPrefs.GetInt(PreferenceKey, (int)SportsTimeChoice.Day);
                return value >= 0 && value <= (int)SportsTimeChoice.Night ? (SportsTimeChoice)value : SportsTimeChoice.Day;
            }
        }
        public static void Select(SportsTimeChoice choice)
        {
            if (!Enum.IsDefined(typeof(SportsTimeChoice), choice)) throw new ArgumentOutOfRangeException(nameof(choice));
            SportsPrefs.SetInt(PreferenceKey, (int)choice); SportsPrefs.Save();
        }
        public static SportsTimeChoice Resolve(int matchSeed)
        {
            return Selection == SportsTimeChoice.Random
                ? (SportsTimeChoice)(1 + new System.Random(unchecked(matchSeed ^ 0x51D734B)).Next(4))
                : Selection;
        }
        public static float Seconds(SportsTimeChoice choice)
        {
            switch (choice)
            {
                case SportsTimeChoice.Morning: return 8.25f * 3600;
                case SportsTimeChoice.Day: return 13 * 3600;
                case SportsTimeChoice.Evening: return 19 * 3600;
                case SportsTimeChoice.Night: return 23 * 3600;
                default: throw new ArgumentException("Resolve Random before applying a time of day.", nameof(choice));
            }
        }
    }
}
