using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiTemporaryEffectKind { MysteryMushroom, HighOctaneBoost, EnergyDrink }
    public enum TruckTaxiEffectStacking { RefreshDuration }

    public sealed class TruckTaxiTemporaryEffect
    {
        public readonly TruckTaxiTemporaryEffectKind Kind;
        public readonly string Label;
        public readonly float DurationSeconds;
        public readonly float FadeInSeconds;
        public readonly float FadeOutSeconds;
        public readonly float VehiclePowerMultiplier;
        public readonly float HungerRateMultiplier;
        public readonly float ThirstRateMultiplier;
        public readonly float Saturation;
        public readonly float Contrast;
        public readonly float BloomIntensity;
        public readonly float ChromaticAberration;
        public readonly int ChaosBonus;
        public readonly string DialogueHook;
        public readonly TruckTaxiEffectStacking Stacking;

        public TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind kind, string label, float durationSeconds,
            float fadeInSeconds, float fadeOutSeconds, float vehiclePowerMultiplier = 1,
            float hungerRateMultiplier = 1, float thirstRateMultiplier = 1,
            float saturation = 0, float contrast = 0, float bloomIntensity = 0,
            float chromaticAberration = 0, int chaosBonus = 0, string dialogueHook = null)
        {
            Kind = kind; Label = label;
            DurationSeconds = Mathf.Max(.01f, durationSeconds);
            FadeInSeconds = Mathf.Clamp(fadeInSeconds, 0, DurationSeconds);
            FadeOutSeconds = Mathf.Clamp(fadeOutSeconds, 0, DurationSeconds - FadeInSeconds);
            VehiclePowerMultiplier = Mathf.Max(1, vehiclePowerMultiplier);
            HungerRateMultiplier = Mathf.Max(0, hungerRateMultiplier);
            ThirstRateMultiplier = Mathf.Max(0, thirstRateMultiplier);
            Saturation = saturation; Contrast = contrast;
            BloomIntensity = bloomIntensity; ChromaticAberration = chromaticAberration;
            ChaosBonus = chaosBonus; DialogueHook = dialogueHook;
            Stacking = TruckTaxiEffectStacking.RefreshDuration;
        }
    }

    public readonly struct TruckTaxiTemporaryEffectSnapshot
    {
        public readonly TruckTaxiTemporaryEffect Profile;
        public readonly float RemainingSeconds;
        public float Progress01 => Profile == null ? 0 : Mathf.Clamp01(RemainingSeconds / Profile.DurationSeconds);
        public float Intensity01
        {
            get
            {
                if (Profile == null || RemainingSeconds <= 0) return 0;
                float elapsed = Profile.DurationSeconds - RemainingSeconds;
                float fadeIn = Profile.FadeInSeconds <= 0 ? 1 : Mathf.Clamp01(elapsed / Profile.FadeInSeconds);
                float fadeOut = Profile.FadeOutSeconds <= 0 ? 1 : Mathf.Clamp01(RemainingSeconds / Profile.FadeOutSeconds);
                return Mathf.Min(fadeIn, fadeOut);
            }
        }
        public TruckTaxiTemporaryEffectSnapshot(TruckTaxiTemporaryEffect profile, float remainingSeconds)
        { Profile = profile; RemainingSeconds = remainingSeconds; }
    }

    // Taxi session authority for timed effects. Presentation and NWH only read these values.
    public sealed class TruckTaxiTemporaryEffects
    {
        private static readonly TruckTaxiTemporaryEffect[] profiles =
        {
            new TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind.MysteryMushroom, "Mystery mushroom",
                18, 2, 4, saturation: 100, contrast: 32, bloomIntensity: 1.2f,
                chromaticAberration: .2f, chaosBonus: 2, dialogueHook: "MysteryMushroom"),
            new TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind.HighOctaneBoost, "High-octane boost",
                12, .35f, 1.5f, vehiclePowerMultiplier: 1.7f,
                chaosBonus: 2, dialogueHook: "HighOctaneBoost"),
            new TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind.EnergyDrink, "Energy drink",
                14, .5f, 2, vehiclePowerMultiplier: 1.15f, hungerRateMultiplier: .9f,
                dialogueHook: "EnergyDrink")
        };
        private readonly float[] remaining = new float[profiles.Length];
        public static int ProfileCount => profiles.Length;
        public event Action<TruckTaxiTemporaryEffectKind> Started;
        public event Action<TruckTaxiTemporaryEffectKind> Expired;
        public int ActiveCount
        {
            get { int count = 0; for (int i = 0; i < remaining.Length; i++) if (remaining[i] > 0) count++; return count; }
        }
        public float VehiclePowerMultiplier
        {
            get
            {
                float value = 1;
                for (int i = 0; i < remaining.Length; i++)
                    if (remaining[i] > 0)
                        value = Mathf.Max(value, Mathf.Lerp(1, profiles[i].VehiclePowerMultiplier, GetSnapshot(profiles[i].Kind).Intensity01));
                return value;
            }
        }
        public float HungerRateMultiplier
        {
            get
            {
                float value = 1;
                for (int i = 0; i < remaining.Length; i++)
                    if (remaining[i] > 0) value = Mathf.Min(value, profiles[i].HungerRateMultiplier);
                return value;
            }
        }
        public float ThirstRateMultiplier
        {
            get
            {
                float value = 1;
                for (int i = 0; i < remaining.Length; i++)
                    if (remaining[i] > 0) value = Mathf.Min(value, profiles[i].ThirstRateMultiplier);
                return value;
            }
        }
        public static TruckTaxiTemporaryEffect Profile(TruckTaxiTemporaryEffectKind kind) =>
            (int)kind >= 0 && (int)kind < profiles.Length ? profiles[(int)kind] : null;
        public TruckTaxiTemporaryEffectSnapshot GetSnapshot(TruckTaxiTemporaryEffectKind kind)
        {
            var profile = Profile(kind);
            return profile == null ? default : new TruckTaxiTemporaryEffectSnapshot(profile, remaining[(int)kind]);
        }
        public bool Activate(TruckTaxiTemporaryEffectKind kind)
        {
            var profile = Profile(kind);
            if (profile == null) return false;
            bool wasActive = remaining[(int)kind] > 0;
            remaining[(int)kind] = profile.DurationSeconds;
            if (!wasActive) Started?.Invoke(kind);
            return true;
        }
        public bool DebugMushroomPeak()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kind = TruckTaxiTemporaryEffectKind.MysteryMushroom;
            bool wasActive = remaining[(int)kind] > 0;
            remaining[(int)kind] = profiles[(int)kind].DurationSeconds - profiles[(int)kind].FadeInSeconds;
            if (!wasActive) Started?.Invoke(kind);
            return true;
#else
            return false;
#endif
        }
        public void Tick(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0) return;
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] = Mathf.Max(0, remaining[i] - seconds);
                if (remaining[i] == 0) Expired?.Invoke(profiles[i].Kind);
            }
        }
        public void Clear()
        {
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] = 0;
                Expired?.Invoke(profiles[i].Kind);
            }
        }
    }
}
