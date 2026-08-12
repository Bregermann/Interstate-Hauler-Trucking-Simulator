using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsMirrorQualityPreset
    {
        public LwsMirrorQuality quality;
        public bool enabled;
        public int resolutionPixels;
        public int updateIntervalFrames;
        public bool shadows;
        public bool postProcessing;
        public float nearClip;
        public float farClip;
        public float fieldOfView;
        public string notes;

        public bool Validate(out string message)
        {
            if (!enabled)
            {
                message = $"{quality} disables mirrors.";
                return true;
            }

            if (resolutionPixels < 128)
            {
                message = $"{quality} mirror resolution is too small.";
                return false;
            }

            if (updateIntervalFrames < 1)
            {
                message = $"{quality} mirror update interval must be at least one frame.";
                return false;
            }

            if (farClip <= nearClip)
            {
                message = $"{quality} mirror far clip must be greater than near clip.";
                return false;
            }

            message = $"{quality} mirror preset is valid.";
            return true;
        }
    }

    [CreateAssetMenu(menuName = "Interstate Hauler/Vehicles/Truck Dashboard Definition", fileName = "IH_DashboardDefinition")]
    public sealed class LwsTruckDashboardDefinition : ScriptableObject
    {
        [SerializeField] private string truckDefinitionId = "ih.truck.nwh.euro.semi.starter";
        [SerializeField] private LwsDashboardSpeedUnit speedUnit = LwsDashboardSpeedUnit.MilesPerHour;
        [SerializeField] private LwsDashboardGaugeMapping speedometer = new LwsDashboardGaugeMapping
        {
            minimumValue = 0f,
            maximumValue = 100f,
            startAngle = 574f,
            endAngle = 330f,
            smoothing = 0.35f
        };
        [SerializeField] private LwsDashboardGaugeMapping tachometer = new LwsDashboardGaugeMapping
        {
            minimumValue = 0f,
            maximumValue = 3000f,
            startAngle = 574f,
            endAngle = 330f,
            smoothing = 0.3f
        };
        [SerializeField, Range(0f, 1f)] private float dashboardBrightness = 0.8f;
        [SerializeField] private LwsMirrorQuality defaultMirrorQuality = LwsMirrorQuality.High;
        [SerializeField] private List<LwsMirrorQualityPreset> mirrorQualityPresets = new List<LwsMirrorQualityPreset>
        {
            new LwsMirrorQualityPreset { quality = LwsMirrorQuality.Off, enabled = false, resolutionPixels = 0, updateIntervalFrames = 0, nearClip = 0.05f, farClip = 0f, fieldOfView = 55f, notes = "Mirror rendering disabled." },
            new LwsMirrorQualityPreset { quality = LwsMirrorQuality.Low, enabled = true, resolutionPixels = 768, updateIntervalFrames = 3, shadows = false, postProcessing = false, nearClip = 0.05f, farClip = 120f, fieldOfView = 58f, notes = "Steam Deck and low-end fallback." },
            new LwsMirrorQualityPreset { quality = LwsMirrorQuality.Medium, enabled = true, resolutionPixels = 1024, updateIntervalFrames = 2, shadows = false, postProcessing = false, nearClip = 0.05f, farClip = 160f, fieldOfView = 56f, notes = "Balanced validation setting." },
            new LwsMirrorQualityPreset { quality = LwsMirrorQuality.High, enabled = true, resolutionPixels = 1536, updateIntervalFrames = 1, shadows = false, postProcessing = false, nearClip = 0.05f, farClip = 220f, fieldOfView = 55f, notes = "Main PC default." },
            new LwsMirrorQualityPreset { quality = LwsMirrorQuality.Ultra, enabled = true, resolutionPixels = 2048, updateIntervalFrames = 1, shadows = true, postProcessing = false, nearClip = 0.05f, farClip = 260f, fieldOfView = 55f, notes = "High-end PC ceiling; not full-screen mirror resolution." }
        };
        [SerializeField] private LwsTruckDashboardCapabilities capabilities = LwsTruckDashboardCapabilities.NwhSemiDevelopmentDefault();
        [SerializeField] private string notes = "Prompt 008 NWH semi dashboard/mirror/cab-life baseline.";

        public string TruckDefinitionId => truckDefinitionId;
        public LwsDashboardSpeedUnit SpeedUnit => speedUnit;
        public LwsDashboardGaugeMapping Speedometer => speedometer;
        public LwsDashboardGaugeMapping Tachometer => tachometer;
        public float DashboardBrightness => dashboardBrightness;
        public LwsMirrorQuality DefaultMirrorQuality => defaultMirrorQuality;
        public IReadOnlyList<LwsMirrorQualityPreset> MirrorQualityPresets => mirrorQualityPresets;
        public LwsTruckDashboardCapabilities Capabilities => capabilities;
        public string Notes => notes;

        public float MetersPerSecondToDisplaySpeed(float metersPerSecond)
        {
            float absoluteSpeed = Mathf.Abs(metersPerSecond);
            return speedUnit == LwsDashboardSpeedUnit.MilesPerHour
                ? absoluteSpeed * 2.23693629f
                : absoluteSpeed * 3.6f;
        }

        public bool TryGetMirrorPreset(LwsMirrorQuality quality, out LwsMirrorQualityPreset preset)
        {
            for (int i = 0; i < mirrorQualityPresets.Count; i++)
            {
                if (mirrorQualityPresets[i].quality == quality)
                {
                    preset = mirrorQualityPresets[i];
                    return true;
                }
            }

            preset = default;
            return false;
        }

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(truckDefinitionId))
            {
                message = "Dashboard definition has no truck definition ID.";
                return false;
            }

            if (!speedometer.Validate(out message))
            {
                message = $"Speedometer mapping invalid: {message}";
                return false;
            }

            if (!tachometer.Validate(out message))
            {
                message = $"Tachometer mapping invalid: {message}";
                return false;
            }

            var seen = new HashSet<LwsMirrorQuality>();
            foreach (LwsMirrorQualityPreset preset in mirrorQualityPresets)
            {
                if (!seen.Add(preset.quality))
                {
                    message = $"Duplicate mirror quality preset: {preset.quality}";
                    return false;
                }

                if (!preset.Validate(out message))
                {
                    return false;
                }
            }

            foreach (LwsMirrorQuality required in Enum.GetValues(typeof(LwsMirrorQuality)))
            {
                if (!seen.Contains(required))
                {
                    message = $"Missing mirror quality preset: {required}";
                    return false;
                }
            }

            message = $"{truckDefinitionId} dashboard definition is valid.";
            return true;
        }
    }
}
