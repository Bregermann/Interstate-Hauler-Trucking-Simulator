using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
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
}
