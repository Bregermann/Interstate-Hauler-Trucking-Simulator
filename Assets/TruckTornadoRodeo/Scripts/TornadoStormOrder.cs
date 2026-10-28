using System.Collections.Generic;
using DigitalRuby.WeatherMaker;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    // LateUpdate runs only after StartMatch has synchronously completed the
    // existing Session.Configure call. The original provider remains the sole
    // weather owner; this adapter supplies one mode-local forecast decision.
    [DefaultExecutionOrder(950)]
    public sealed class TornadoStormOrder : MonoBehaviour
    {
        public TornadoMatch match;
        readonly TornadoStormPolicy policy = new TornadoStormPolicy();
        public string Status { get; private set; } = "PENDING: WEATHER BINDING";
        public string HandledMatchId { get; private set; }
        public string AppliedPresetId { get; private set; }
        public int ConfigureCalls { get; private set; }
        void LateUpdate()
        {
            if (match == null) { Status = "PENDING: MATCH BINDING"; return; }
            var weather = match.presentationWeather; var catalog = weather != null ? weather.presets : null;
            bool bound = weather != null && weather.session == match && catalog != null && weather.vendor != null && weather.vendor.ResourceContainer != null && weather.precipitation != null && weather.wind != null;
            string selection = catalog != null ? catalog.Selection : null;
            // An explicit selection is retained only after the existing provider
            // has actually configured it, rather than consuming an early frame.
            bool configured = weather != null && weather.PresetDriven && (selection == SportsWeatherPresets.RandomId || weather.PresetId == selection);
            var decision = policy.Decide(match.MatchId, match.Running, match.Finished, match.Paused, bound, configured, selection, SportsWeatherPresets.RandomId);
            if (decision == TornadoForecastDecision.AlreadyHandled || decision == TornadoForecastDecision.WaitForMatch) return;
            if (decision == TornadoForecastDecision.PendingBindings) { Status = "PENDING: WEATHER BINDING"; return; }
            if (decision == TornadoForecastDecision.PendingBaseForecast) { Status = "PENDING: SESSION FORECAST"; return; }
            if (decision == TornadoForecastDecision.PendingResume) { Status = "PENDING: RESUME BEFORE FORECAST"; return; }
            if (decision == TornadoForecastDecision.KeepExplicit)
            {
                policy.Confirm(match.MatchId); HandledMatchId = match.MatchId; AppliedPresetId = weather.PresetId; Status = "EXPLICIT FORECAST RETAINED"; return;
            }
            var eligible = new List<SportsWeatherPresets.Entry>(5); var ids = new List<string>(5);
            foreach (var entry in catalog.entries ?? System.Array.Empty<SportsWeatherPresets.Entry>())
            {
                if (entry == null || entry.profile == null || string.IsNullOrEmpty(entry.id)) continue;
                var profile = entry.profile; var rain = profile.PrecipitationProfile; var wind = profile.WindProfile;
                if (!TornadoStormPolicy.Suitable(profile.name, profile.Disabled, rain != null && rain.PrecipitationType == WeatherMakerPrecipitationType.Rain, rain != null && rain.PrecipitationType == WeatherMakerPrecipitationType.Snow, profile.LightningProfile != null, entry.legacyKind == SportsWeatherKind.Hurricane, wind != null ? wind.WindSpeedRange.Maximum : 0)) continue;
                eligible.Add(entry); ids.Add(entry.id);
            }
            string chosen = TornadoStormPolicy.Choose(match.MatchId, ids.ToArray());
            if (chosen == null)
            {
                // No fake storm/label/fallback and no frame-by-frame reroll.
                policy.Confirm(match.MatchId); HandledMatchId = match.MatchId; AppliedPresetId = null; Status = "PENDING: NO SUITABLE SEVERE PRESET"; return;
            }
            var selected = eligible.Find(entry => entry.id == chosen);
            weather.ConfigurePreset(selected, TornadoStormPolicy.Seed(match.MatchId));
            ConfigureCalls++; AppliedPresetId = selected.id; HandledMatchId = match.MatchId; policy.Confirm(match.MatchId); Status = "RANDOM SEVERE: " + selected.displayName;
        }
    }
}
