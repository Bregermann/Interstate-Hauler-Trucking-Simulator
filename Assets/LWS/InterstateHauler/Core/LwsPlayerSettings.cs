using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsPlayerSettingsService : ILwsService
    {
        bool GpsVoiceGuidanceEnabled { get; }
        event Action<bool> GpsVoiceGuidanceChanged;
        void SetGpsVoiceGuidanceEnabled(bool enabled);
    }

    public sealed class LwsPlayerSettingsService : ILwsPlayerSettingsService
    {
        private const string GpsVoiceGuidanceKey = "ih.settings.gpsVoiceGuidance";

        public string ServiceId => "lws.player.settings";
        public bool GpsVoiceGuidanceEnabled { get; private set; } = true;
        public event Action<bool> GpsVoiceGuidanceChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            GpsVoiceGuidanceEnabled = PlayerPrefs.GetInt(GpsVoiceGuidanceKey, 1) != 0;
            return LwsServiceResult.Success("LWS player settings service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            PlayerPrefs.Save();
            return LwsServiceResult.Success("LWS player settings service shut down.");
        }

        public void SetGpsVoiceGuidanceEnabled(bool enabled)
        {
            if (GpsVoiceGuidanceEnabled == enabled)
            {
                return;
            }

            GpsVoiceGuidanceEnabled = enabled;
            PlayerPrefs.SetInt(GpsVoiceGuidanceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            GpsVoiceGuidanceChanged?.Invoke(enabled);
        }
    }
}
