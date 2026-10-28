using System;
using System.Collections.Generic;

namespace LWS.TruckTornadoRodeo
{
    public enum TornadoForecastDecision { WaitForMatch, AlreadyHandled, PendingBindings, PendingBaseForecast, PendingResume, KeepExplicit, ChooseSevere }

    // Pure, match-local ordering/selection policy. No preferences, profiles,
    // weather state or catalog entries are written here.
    public sealed class TornadoStormPolicy
    {
        string handled;
        public TornadoForecastDecision Decide(string matchId, bool running, bool finished, bool paused, bool bound, bool baseForecastConfigured, string selection, string randomId)
        {
            if (!running || finished || string.IsNullOrEmpty(matchId)) return TornadoForecastDecision.WaitForMatch;
            if (string.Equals(handled, matchId, StringComparison.Ordinal)) return TornadoForecastDecision.AlreadyHandled;
            if (!bound) return TornadoForecastDecision.PendingBindings;
            if (!baseForecastConfigured) return TornadoForecastDecision.PendingBaseForecast;
            if (paused) return TornadoForecastDecision.PendingResume;
            return string.Equals(selection, randomId, StringComparison.Ordinal) ? TornadoForecastDecision.ChooseSevere : TornadoForecastDecision.KeepExplicit;
        }
        public void Confirm(string matchId) { if (string.IsNullOrEmpty(matchId)) throw new ArgumentException("A real match token is required."); handled = matchId; }
        public static bool Suitable(string actualProfileName, bool disabled, bool rain, bool snow, bool lightning, bool hurricaneKind, float maximumWind)
        {
            if (disabled || string.IsNullOrEmpty(actualProfileName)) return false;
            // Actual installed profile descriptors, not a newly invented label
            // or a renamed Clear catalog row. Hurricane is the existing owned
            // high-wind semantic over the installed storm foundation.
            if (hurricaneKind && float.IsFinite(maximumWind) && maximumWind >= 65) return true;
            return actualProfileName == "WeatherMakerProfile_Storm" && rain && lightning ||
                actualProfileName == "WeatherMakerProfile_Blizzard" && snow ||
                actualProfileName == "WeatherMakerProfile_HeavyRain" && rain ||
                actualProfileName == "WeatherMakerProfile_HeavySnow" && snow;
        }
        public static int Seed(string matchId)
        {
            if (string.IsNullOrEmpty(matchId)) throw new ArgumentException("A real match token is required.");
            unchecked { uint hash = 2166136261; foreach (char c in matchId) { hash = (hash ^ c) * 16777619; } return (int)hash; }
        }
        public static string Choose(string matchId, string[] actualEligibleIds)
        {
            if (actualEligibleIds == null || actualEligibleIds.Length == 0) return null;
            var unique = new SortedSet<string>(StringComparer.Ordinal); foreach (var id in actualEligibleIds) if (!string.IsNullOrEmpty(id)) unique.Add(id);
            if (unique.Count == 0) return null;
            var ids = new string[unique.Count]; unique.CopyTo(ids);
            // Equal discrete choices over distinct actual eligible IDs. Sorting
            // keeps a stable token's result independent of catalog row order.
            int index = new Random(Seed(matchId)).Next(ids.Length); return ids[index];
        }
    }
}
