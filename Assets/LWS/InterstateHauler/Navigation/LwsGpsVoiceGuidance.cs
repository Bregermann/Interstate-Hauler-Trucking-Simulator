using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsGpsDistanceVoicePrompt
    {
        InOneMile,
        InHalfMile,
        InQuarterMile,
        InOneThousandFeet,
        InFiveHundredFeet,
        Now,
        Then
    }

    [Serializable]
    public sealed class LwsGpsManeuverVoiceSlot
    {
        public LwsNavigationManeuverType maneuver;
        public AudioClip clip;
    }

    [Serializable]
    public sealed class LwsGpsDistanceVoiceSlot
    {
        public LwsGpsDistanceVoicePrompt prompt;
        public AudioClip clip;
    }

    [CreateAssetMenu(menuName = "Interstate Hauler/Navigation/GPS Voice Pack", fileName = "IH_GpsVoicePack")]
    public sealed class LwsGpsVoicePack : ScriptableObject
    {
        [SerializeField] private string voicePackId = "ih.gps.voice.default";
        [SerializeField] private string displayName = "Default GPS Voice";
        [SerializeField] private List<LwsGpsManeuverVoiceSlot> maneuverSlots = new List<LwsGpsManeuverVoiceSlot>();
        [SerializeField] private List<LwsGpsDistanceVoiceSlot> distanceSlots = new List<LwsGpsDistanceVoiceSlot>();

        public string VoicePackId => voicePackId;
        public string DisplayName => displayName;
        public IReadOnlyList<LwsGpsManeuverVoiceSlot> ManeuverSlots => maneuverSlots;
        public IReadOnlyList<LwsGpsDistanceVoiceSlot> DistanceSlots => distanceSlots;

        private void OnValidate()
        {
            EnsureAllSlots();
        }

        public void EnsureAllSlots()
        {
            foreach (LwsNavigationManeuverType maneuver in LwsNavigationManeuverCatalog.All)
            {
                if (maneuverSlots.Exists(s => s != null && s.maneuver == maneuver))
                {
                    continue;
                }

                maneuverSlots.Add(new LwsGpsManeuverVoiceSlot { maneuver = maneuver });
            }

            foreach (LwsGpsDistanceVoicePrompt prompt in Enum.GetValues(typeof(LwsGpsDistanceVoicePrompt)))
            {
                if (distanceSlots.Exists(s => s != null && s.prompt == prompt))
                {
                    continue;
                }

                distanceSlots.Add(new LwsGpsDistanceVoiceSlot { prompt = prompt });
            }
        }

        public bool TryGetClip(LwsNavigationManeuverType maneuver, out AudioClip clip)
        {
            EnsureAllSlots();
            for (int i = 0; i < maneuverSlots.Count; i++)
            {
                LwsGpsManeuverVoiceSlot slot = maneuverSlots[i];
                if (slot != null && slot.maneuver == maneuver)
                {
                    clip = slot.clip;
                    return clip != null;
                }
            }

            clip = null;
            return false;
        }

        public bool ValidateSlots(out string message)
        {
            EnsureAllSlots();
            var missing = new List<string>();
            foreach (LwsNavigationManeuverType maneuver in LwsNavigationManeuverCatalog.All)
            {
                int count = 0;
                for (int i = 0; i < maneuverSlots.Count; i++)
                {
                    if (maneuverSlots[i] != null && maneuverSlots[i].maneuver == maneuver)
                    {
                        count++;
                    }
                }

                if (count != 1)
                {
                    missing.Add($"{maneuver} slots={count}");
                }
            }

            message = missing.Count == 0
                ? "GPS voice pack exposes one assignable slot for every maneuver."
                : "GPS voice pack slot issues: " + string.Join(", ", missing);
            return missing.Count == 0;
        }
    }

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
