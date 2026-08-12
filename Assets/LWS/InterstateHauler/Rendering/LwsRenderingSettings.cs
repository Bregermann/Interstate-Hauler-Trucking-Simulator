using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.InterstateHauler
{
    public enum LwsRenderQualityTier
    {
        Ultra,
        High,
        Medium,
        Low,
        SteamDeck
    }

    public enum LwsMirrorQuality
    {
        Off,
        Low,
        Medium,
        High,
        Ultra
    }

    public enum LwsWeatherVisualQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum LwsVegetationQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum LwsShadowQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum LwsPlatformClass
    {
        DesktopPC,
        SteamDeck,
        NintendoSwitch,
        XboxSeries,
        PlayStation5
    }

    [Serializable]
    public sealed class LwsRenderingQualityProfile
    {
        public LwsRenderQualityTier tier;
        public string unityQualityLevelName;
        public RenderPipelineAsset renderPipelineAsset;
        public LwsMirrorQuality mirrorQuality = LwsMirrorQuality.Medium;
        public LwsWeatherVisualQuality weatherQuality = LwsWeatherVisualQuality.Medium;
        public LwsVegetationQuality vegetationQuality = LwsVegetationQuality.Medium;
        public LwsShadowQuality shadowQuality = LwsShadowQuality.Medium;
        public float renderScale = 1f;
        public int targetFrameRate = -1;
        public int mirrorResolutionPixels = 1024;
        public int mirrorUpdateIntervalFrames = 1;
        public float vegetationDistanceMultiplier = 1f;
        public string notes;
    }

    [Serializable]
    public sealed class LwsPlatformQualityDefault
    {
        public LwsPlatformClass platformClass;
        public LwsRenderQualityTier defaultTier;
        public string notes;
    }

    [CreateAssetMenu(menuName = "Interstate Hauler/Rendering Settings", fileName = "IH_RenderingSettings")]
    public sealed class LwsRenderingSettings : ScriptableObject
    {
        public LwsRenderQualityTier defaultTier = LwsRenderQualityTier.High;
        public LwsRenderQualityTier steamDeckTier = LwsRenderQualityTier.SteamDeck;
        public List<LwsRenderingQualityProfile> qualityProfiles = new List<LwsRenderingQualityProfile>();
        public List<LwsPlatformQualityDefault> platformDefaults = new List<LwsPlatformQualityDefault>();

        public bool TryGetProfile(LwsRenderQualityTier tier, out LwsRenderingQualityProfile profile)
        {
            for (int i = 0; i < qualityProfiles.Count; i++)
            {
                if (qualityProfiles[i] != null && qualityProfiles[i].tier == tier)
                {
                    profile = qualityProfiles[i];
                    return true;
                }
            }

            profile = null;
            return false;
        }

        public bool Validate(out string message)
        {
            var seen = new HashSet<LwsRenderQualityTier>();
            foreach (LwsRenderingQualityProfile profile in qualityProfiles)
            {
                if (profile == null)
                {
                    message = "Rendering settings contain a null quality profile.";
                    return false;
                }

                if (!seen.Add(profile.tier))
                {
                    message = $"Duplicate rendering quality tier: {profile.tier}";
                    return false;
                }

                if (profile.renderPipelineAsset == null)
                {
                    message = $"Quality tier {profile.tier} has no render pipeline asset.";
                    return false;
                }
            }

            foreach (LwsRenderQualityTier requiredTier in Enum.GetValues(typeof(LwsRenderQualityTier)))
            {
                if (!seen.Contains(requiredTier))
                {
                    message = $"Missing rendering quality tier: {requiredTier}";
                    return false;
                }
            }

            message = "Rendering settings are valid.";
            return true;
        }
    }

    public interface ILwsRenderingService : ILwsService
    {
        LwsRenderingSettings Settings { get; }
        LwsRenderQualityTier ActiveTier { get; }
        void SetSettings(LwsRenderingSettings settings);
        void SetActiveTier(LwsRenderQualityTier tier);
        bool TryGetActiveProfile(out LwsRenderingQualityProfile profile);
    }

    public sealed class LwsRenderingService : ILwsRenderingService
    {
        public string ServiceId => "lws.rendering";
        public LwsRenderingSettings Settings { get; private set; }
        public LwsRenderQualityTier ActiveTier { get; private set; } = LwsRenderQualityTier.High;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            ActiveTier = Settings != null ? Settings.defaultTier : LwsRenderQualityTier.High;
            return LwsServiceResult.Success("LWS rendering service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            return LwsServiceResult.Success("LWS rendering service shut down.");
        }

        public void SetSettings(LwsRenderingSettings settings)
        {
            Settings = settings;
            if (settings != null)
            {
                ActiveTier = settings.defaultTier;
            }
        }

        public void SetActiveTier(LwsRenderQualityTier tier)
        {
            ActiveTier = tier;
        }

        public bool TryGetActiveProfile(out LwsRenderingQualityProfile profile)
        {
            if (Settings == null)
            {
                profile = null;
                return false;
            }

            return Settings.TryGetProfile(ActiveTier, out profile);
        }
    }
}
