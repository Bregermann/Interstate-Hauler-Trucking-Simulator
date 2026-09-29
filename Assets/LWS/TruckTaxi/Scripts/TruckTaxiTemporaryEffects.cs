using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiTemporaryEffectKind { MysteryMushroom, HighOctaneBoost, EnergyDrink, RoadPep, RoadPepComedown }
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
        public readonly float BladderRateMultiplier;
        public readonly float Saturation;
        public readonly float Contrast;
        public readonly float BloomIntensity;
        public readonly float ChromaticAberration;
        public readonly int ChaosBonus;
        public readonly string DialogueHook;
        public readonly TruckTaxiEffectStacking Stacking;

        public TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind kind, string label, float durationSeconds,
            float fadeInSeconds, float fadeOutSeconds, float vehiclePowerMultiplier = 1,
            float hungerRateMultiplier = 1, float thirstRateMultiplier = 1, float bladderRateMultiplier = 1,
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
            BladderRateMultiplier = Mathf.Max(0, bladderRateMultiplier);
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
                360, 2, 4, saturation: 100, contrast: 32, bloomIntensity: 1.2f,
                chromaticAberration: .2f, chaosBonus: 2, dialogueHook: "MysteryMushroom"),
            new TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind.HighOctaneBoost, "High-octane boost",
                300, .35f, 1.5f, vehiclePowerMultiplier: 1.7f,
                chaosBonus: 2, dialogueHook: "HighOctaneBoost"),
            new TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind.EnergyDrink, "Energy drink",
                420, .5f, 2, vehiclePowerMultiplier: 1.15f, hungerRateMultiplier: .9f,
                dialogueHook: "EnergyDrink"),
            new TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind.RoadPep, "Road Pep",
                600, 1, 3, hungerRateMultiplier: .35f, thirstRateMultiplier: 1.2f,
                bladderRateMultiplier: 1.15f, dialogueHook: "RoadPep"),
            new TruckTaxiTemporaryEffect(TruckTaxiTemporaryEffectKind.RoadPepComedown, "Road Pep comedown",
                120, 2, 4, hungerRateMultiplier: 1.35f, thirstRateMultiplier: 1.15f,
                dialogueHook: "RoadPepComedown")
        };
        private readonly float[] remaining = new float[profiles.Length];
        private readonly TruckTaxiTemporaryEffect[] activeProfiles = new TruckTaxiTemporaryEffect[profiles.Length];
        public int RoadPepOveruse { get; private set; }
        private float roadPepComedownQuality = 1;
        public bool ShortDebugDurations { get; private set; }
        public void SetShortDebugDurations(bool enabled)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ShortDebugDurations = enabled;
#endif
        }
        private static TruckTaxiTemporaryEffect DebugProfile(TruckTaxiTemporaryEffect profile)
        {
            float duration = profile.Kind == TruckTaxiTemporaryEffectKind.MysteryMushroom ? 18 :
                profile.Kind == TruckTaxiTemporaryEffectKind.HighOctaneBoost ? 12 :
                profile.Kind == TruckTaxiTemporaryEffectKind.RoadPep ? 20 :
                profile.Kind == TruckTaxiTemporaryEffectKind.RoadPepComedown ? 8 : 14;
            return new TruckTaxiTemporaryEffect(profile.Kind, profile.Label, duration,
                profile.FadeInSeconds, profile.FadeOutSeconds, profile.VehiclePowerMultiplier,
                profile.HungerRateMultiplier, profile.ThirstRateMultiplier, profile.BladderRateMultiplier, profile.Saturation,
                profile.Contrast, profile.BloomIntensity, profile.ChromaticAberration,
                profile.ChaosBonus, profile.DialogueHook);
        }
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
                        value = Mathf.Max(value, Mathf.Lerp(1, (activeProfiles[i] ?? profiles[i]).VehiclePowerMultiplier, GetSnapshot(profiles[i].Kind).Intensity01));
                return value;
            }
        }
        public float HungerRateMultiplier
        {
            get
            {
                float value = 1;
                for (int i = 0; i < remaining.Length; i++)
                    if (remaining[i] > 0) value *= (activeProfiles[i] ?? profiles[i]).HungerRateMultiplier;
                return value;
            }
        }
        public float ThirstRateMultiplier
        {
            get
            {
                float value = 1;
                for (int i = 0; i < remaining.Length; i++)
                    if (remaining[i] > 0) value *= (activeProfiles[i] ?? profiles[i]).ThirstRateMultiplier;
                return value;
            }
        }
        public float BladderRateMultiplier
        {
            get
            {
                float value = 1;
                for (int i = 0; i < remaining.Length; i++)
                    if (remaining[i] > 0) value *= (activeProfiles[i] ?? profiles[i]).BladderRateMultiplier;
                return value;
            }
        }
        public static TruckTaxiTemporaryEffect Profile(TruckTaxiTemporaryEffectKind kind) =>
            (int)kind >= 0 && (int)kind < profiles.Length ? profiles[(int)kind] : null;
        public TruckTaxiTemporaryEffectSnapshot GetSnapshot(TruckTaxiTemporaryEffectKind kind)
        {
            var profile = Profile(kind);
            return profile == null ? default : new TruckTaxiTemporaryEffectSnapshot(
                activeProfiles[(int)kind] ?? profile, remaining[(int)kind]);
        }
        public bool Activate(TruckTaxiTemporaryEffectKind kind)
        {
            if (kind == TruckTaxiTemporaryEffectKind.RoadPep || kind == TruckTaxiTemporaryEffectKind.RoadPepComedown) return false;
            var profile = Profile(kind);
            if (profile == null) return false;
            bool wasActive = remaining[(int)kind] > 0;
            activeProfiles[(int)kind] = ShortDebugDurations ? DebugProfile(profile) : profile;
            remaining[(int)kind] = activeProfiles[(int)kind].DurationSeconds;
            if (!wasActive) Started?.Invoke(kind);
            return true;
        }
        public static float RoadPepQuality(int score) => score >= 90 ? 1 : score >= 70 ? .9f : score >= 40 ? .7f : .45f;
        public void ActivateRoadPep(int score)
        {
            score = Mathf.Clamp(score, 0, 100);
            int index = (int)TruckTaxiTemporaryEffectKind.RoadPep;
            bool repeated = remaining[index] > 0 || remaining[(int)TruckTaxiTemporaryEffectKind.RoadPepComedown] > 0;
            RoadPepOveruse = repeated ? Mathf.Min(3, RoadPepOveruse + 1) : 0;
            float quality = RoadPepQuality(score);
            roadPepComedownQuality = quality;
            float benefit = Mathf.Lerp(.45f, 1, quality);
            var baseProfile = profiles[index];
            float duration = (ShortDebugDurations ? 20 : baseProfile.DurationSeconds) * quality;
            activeProfiles[index] = new TruckTaxiTemporaryEffect(baseProfile.Kind, baseProfile.Label,
                duration, baseProfile.FadeInSeconds, baseProfile.FadeOutSeconds,
                hungerRateMultiplier: Mathf.Lerp(1, baseProfile.HungerRateMultiplier, benefit),
                thirstRateMultiplier: baseProfile.ThirstRateMultiplier + RoadPepOveruse * .1f,
                bladderRateMultiplier: baseProfile.BladderRateMultiplier + RoadPepOveruse * .05f,
                dialogueHook: baseProfile.DialogueHook);
            bool wasActive = remaining[index] > 0;
            remaining[index] = duration;
            int comedown = (int)TruckTaxiTemporaryEffectKind.RoadPepComedown;
            if (remaining[comedown] > 0) { remaining[comedown] = 0; Expired?.Invoke(TruckTaxiTemporaryEffectKind.RoadPepComedown); }
            if (!wasActive) Started?.Invoke(TruckTaxiTemporaryEffectKind.RoadPep);
        }
        public bool DebugMushroomPeak()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kind = TruckTaxiTemporaryEffectKind.MysteryMushroom;
            bool wasActive = remaining[(int)kind] > 0;
            activeProfiles[(int)kind] = ShortDebugDurations ? DebugProfile(profiles[(int)kind]) : profiles[(int)kind];
            remaining[(int)kind] = activeProfiles[(int)kind].DurationSeconds - activeProfiles[(int)kind].FadeInSeconds;
            if (!wasActive) Started?.Invoke(kind);
            return true;
#else
            return false;
#endif
        }
        public void Tick(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0) return;
            bool comedownCreated = false;
            float comedownSeconds = 0;
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] <= 0) continue;
                float elapsed = comedownCreated && i == (int)TruckTaxiTemporaryEffectKind.RoadPepComedown ? comedownSeconds : seconds;
                float before = remaining[i];
                remaining[i] = Mathf.Max(0, remaining[i] - elapsed);
                if (remaining[i] == 0)
                {
                    Expired?.Invoke(profiles[i].Kind);
                    if (profiles[i].Kind == TruckTaxiTemporaryEffectKind.RoadPep)
                    { comedownCreated = true; comedownSeconds = Mathf.Max(0, seconds - before); StartRoadPepComedown(); }
                    else if (profiles[i].Kind == TruckTaxiTemporaryEffectKind.RoadPepComedown) RoadPepOveruse = 0;
                }
            }
        }
        public void ClearRoadPep()
        {
            for (int i = (int)TruckTaxiTemporaryEffectKind.RoadPep; i <= (int)TruckTaxiTemporaryEffectKind.RoadPepComedown; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] = 0;
                Expired?.Invoke(profiles[i].Kind);
            }
            RoadPepOveruse = 0;
        }
        private void StartRoadPepComedown()
        {
            int index = (int)TruckTaxiTemporaryEffectKind.RoadPepComedown;
            var baseProfile = profiles[index];
            float severity = 1 + RoadPepOveruse * .2f + (1 - roadPepComedownQuality) * .5f;
            float duration = ShortDebugDurations ? 8 : Mathf.Min(210, baseProfile.DurationSeconds * severity);
            activeProfiles[index] = new TruckTaxiTemporaryEffect(baseProfile.Kind, baseProfile.Label,
                duration, baseProfile.FadeInSeconds, baseProfile.FadeOutSeconds,
                hungerRateMultiplier: 1 + (baseProfile.HungerRateMultiplier - 1) * severity,
                thirstRateMultiplier: 1 + (baseProfile.ThirstRateMultiplier - 1) * severity,
                dialogueHook: baseProfile.DialogueHook);
            remaining[index] = duration;
            Started?.Invoke(TruckTaxiTemporaryEffectKind.RoadPepComedown);
        }
        public void Clear()
        {
            RoadPepOveruse = 0;
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] = 0;
                Expired?.Invoke(profiles[i].Kind);
            }
        }
    }
}
