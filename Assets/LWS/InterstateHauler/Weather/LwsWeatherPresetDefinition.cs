using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(menuName = "Interstate Hauler/Weather Preset", fileName = "IH_WeatherPreset")]
    public sealed class LwsWeatherPresetDefinition : ScriptableObject
    {
        [SerializeField] private LwsWeatherPreset preset;

        public LwsWeatherPreset Preset => preset;
        public string PresetId => preset.presetId;
        public string DisplayName => preset.displayName;

        public void Configure(LwsWeatherPreset value)
        {
            preset = value;
        }

        public bool ValidateDefinition(out string message)
        {
            return preset.Validate(out message);
        }
    }
}
