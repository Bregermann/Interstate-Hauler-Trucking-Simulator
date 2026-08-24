using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsGpsVoiceGuidanceService : ILwsService
    {
        LwsGpsVoicePack VoicePack { get; }
        string LastInstruction { get; }
        string LastClipName { get; }
        bool VoicePlaying { get; }
        void Configure(LwsGpsVoicePack voicePack, AudioSource audioSource);
        bool Announce(LwsNavigationManeuverType maneuver, int stepIndex, bool force);
        void Stop();
    }

    public sealed class LwsGpsVoiceGuidanceService : ILwsGpsVoiceGuidanceService
    {
        private ILwsPlayerSettingsService _settingsService;
        private AudioSource _audioSource;
        private int _lastStepIndex = int.MinValue;
        private LwsNavigationManeuverType _lastManeuver;

        public string ServiceId => "lws.navigation.voice";
        public LwsGpsVoicePack VoicePack { get; private set; }
        public string LastInstruction { get; private set; } = string.Empty;
        public string LastClipName { get; private set; } = string.Empty;
        public bool VoicePlaying => _audioSource != null && _audioSource.isPlaying;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _settingsService);
            if (_settingsService != null)
            {
                _settingsService.GpsVoiceGuidanceChanged += HandleVoiceSettingChanged;
            }

            return LwsServiceResult.Success("LWS GPS voice guidance service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            if (_settingsService != null)
            {
                _settingsService.GpsVoiceGuidanceChanged -= HandleVoiceSettingChanged;
            }

            Stop();
            _audioSource = null;
            VoicePack = null;
            return LwsServiceResult.Success("LWS GPS voice guidance service shut down.");
        }

        public void Configure(LwsGpsVoicePack voicePack, AudioSource audioSource)
        {
            VoicePack = voicePack;
            if (VoicePack != null)
            {
                VoicePack.EnsureAllSlots();
            }

            _audioSource = audioSource;
            if (_audioSource != null)
            {
                _audioSource.spatialBlend = 0f;
                _audioSource.dopplerLevel = 0f;
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
            }
        }

        public bool Announce(LwsNavigationManeuverType maneuver, int stepIndex, bool force)
        {
            LastInstruction = LwsNavigationManeuverCatalog.GetDisplayName(maneuver);
            LastClipName = string.Empty;

            if (_settingsService != null && !_settingsService.GpsVoiceGuidanceEnabled)
            {
                Stop();
                return false;
            }

            if (!force && stepIndex == _lastStepIndex && maneuver == _lastManeuver)
            {
                return false;
            }

            _lastStepIndex = stepIndex;
            _lastManeuver = maneuver;

            if (_audioSource == null || VoicePack == null || !VoicePack.TryGetClip(maneuver, out AudioClip clip))
            {
                return false;
            }

            _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.Play();
            LastClipName = clip.name;
            return true;
        }

        public void Stop()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        private void HandleVoiceSettingChanged(bool enabled)
        {
            if (!enabled)
            {
                Stop();
            }
        }
    }
}
