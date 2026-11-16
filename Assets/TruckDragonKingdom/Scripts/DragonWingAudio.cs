using System;
using System.Collections.Generic;
using LWS.TruckSports;
using MalbersAnimations.Utilities;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // Sound playback/randomization/spatialization belongs to Malbers. This
    // adapter observes actual rendered wing strokes and binds match lifecycle.
    [DefaultExecutionOrder(30)]
    public sealed class DragonWingAudio : MonoBehaviour
    {
        public const string EventName = "Owned dragon wing downstroke";
        public const int MaximumConcurrentVoices = 3;
        public DragonAnimalMotor motor;
        public Transform leftHinge, leftTip, rightHinge, rightTip;
        public AnimatorEventSounds vendorSounds;
        public AudioSource source;
        public AnimationClip[] flappingClips = Array.Empty<AnimationClip>();
        [Range(0, 1)] public float gain = .45f;
        [Range(.1f, 1)] public float minimumClipWeight = .4f;
        [Min(.02f)] public float minimumRise = .10f;
        [Min(.005f)] public float crestDeparture = .025f;
        [Min(.1f)] public float cooldown = .4f;
        public int EmittedStrokes { get; private set; }
        public int ObservedStrokes { get; private set; }
        public AnimationClip ObservedClip { get; private set; }
        public float ObservedWeight { get; private set; }
        public float LastStrokeExcursion { get; private set; }
        public AnimationClip LastEmissionClip { get; private set; }
        public float LastEmissionWeight { get; private set; }
        public bool Bound => settings != null;
        static readonly List<DragonWingAudio> voices = new List<DragonWingAudio>(MaximumConcurrentVoices);
        readonly List<AnimatorClipInfo> clips = new List<AnimatorClipInfo>(8);
        SportsSettings settings;
        DragonCharacter character;
        EventSound wingEvent;
        float lowLeft, lowRight, peakLeft, peakRight, readyAt, activeWeight;
        bool tracking, ascending, heldAudio, wasPrepared;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStaticVoices() => voices.Clear();

        // One explicit caller beside the original actor BindAudio invocation.
        // Settings own SFX; AudioListener retains master ownership exactly once.
        public static void BindAll(DragonCharacter[] actors, SportsSettings audioSettings)
        {
            if (actors == null) return;
            foreach (var actor in actors)
                if (actor != null)
                    foreach (var wings in actor.GetComponentsInChildren<DragonWingAudio>(true)) wings.BindAudio(audioSettings);
        }
        public void BindAudio(SportsSettings audioSettings)
        {
            StopAndReset(); settings = audioSettings;
            character = GetComponentInParent<DragonCharacter>();
            ResolveEvent(); ApplyGain();
        }
        public void ValidateBindings()
        {
            if (motor == null || motor.animator == null || leftHinge == null || leftTip == null || rightHinge == null || rightTip == null ||
                vendorSounds == null || source == null || vendorSounds._audioSource != source || flappingClips == null || flappingClips.Length == 0 ||
                leftHinge == rightHinge || leftTip == rightTip || !leftTip.IsChildOf(leftHinge) || !rightTip.IsChildOf(rightHinge))
                throw new InvalidOperationException("Actual paired wing bones, configured animator, mapped flap clips and one explicit Malbers sound source are required.");
            foreach (var clip in flappingClips) if (clip == null) throw new InvalidOperationException("Missing actual supported flapping animation clip.");
            ResolveEvent();
            if (wingEvent == null || wingEvent.source != source || wingEvent.Clips == null || wingEvent.Clips.Length == 0 || source.loop || source.playOnAwake)
                throw new InvalidOperationException("Explicit non-looping owned wing sound event/source required.");
            foreach (var clip in wingEvent.Clips) if (clip == null) throw new InvalidOperationException("Missing owned wing audio clip.");
        }
        void Awake() { character = GetComponentInParent<DragonCharacter>(); ResolveEvent(); }
        void ResolveEvent() => wingEvent = vendorSounds != null && vendorSounds.m_EventSound != null ? vendorSounds.m_EventSound.Find(item => item != null && item.name == EventName) : null;

        void LateUpdate()
        {
            bool prepared = motor != null && motor.Prepared && (character == null || character.Prepared);
            if (!prepared || settings == null || source == null || wingEvent == null)
            {
                if (wasPrepared || tracking || heldAudio || source != null && source.isPlaying) StopAndReset();
                wasPrepared = false; return;
            }
            if (!wasPrepared) { EmittedStrokes = ObservedStrokes = 0; readyAt = 0; tracking = false; wasPrepared = true; }
            if (motor.Paused || character != null && character.Paused)
            {
                if (!heldAudio && source.isPlaying) { source.Pause(); heldAudio = true; }
                tracking = false; return;
            }
            ApplyGain();
            if (heldAudio) { source.UnPause(); heldAudio = false; }
            PruneVoices();
            if (!TryWeightedClip(out var clip, out float weight)) { tracking = false; ObservedClip = null; ObservedWeight = 0; return; }
            Vector3 up = motor.animator.transform.up;
            float left = Height(leftHinge, leftTip, up), right = Height(rightHinge, rightTip, up);
            if (!float.IsFinite(left) || !float.IsFinite(right)) { tracking = false; return; }
            if (!tracking || ObservedClip != clip)
            {
                lowLeft = peakLeft = left; lowRight = peakRight = right; ascending = false; tracking = true;
                ObservedClip = clip; ObservedWeight = weight; return;
            }
            ObservedWeight = weight;
            if (!ascending)
            {
                lowLeft = Mathf.Min(lowLeft, left); lowRight = Mathf.Min(lowRight, right);
                if (left - lowLeft >= minimumRise && right - lowRight >= minimumRise)
                { ascending = true; peakLeft = left; peakRight = right; }
                return;
            }
            peakLeft = Mathf.Max(peakLeft, left); peakRight = Mathf.Max(peakRight, right);
            // Both real wing tips must rise, then depart their actual crest.
            // Body travel/roll is removed by hinge-relative animator-up sampling.
            if (peakLeft - left < crestDeparture || peakRight - right < crestDeparture) return;
            LastStrokeExcursion = Mathf.Min(peakLeft - lowLeft, peakRight - lowRight);
            ObservedStrokes++; ascending = false; lowLeft = left; lowRight = right;
            if (Time.time < readyAt || source.isPlaying || settings.sfx <= 0 || gain <= 0 || !ReserveVoice()) return;
            activeWeight = weight; ApplyGain();
            vendorSounds.PlaySound(EventName); // unchanged vendor randomization + playback
            if (source.isPlaying) { EmittedStrokes++; LastEmissionClip = clip; LastEmissionWeight = weight; readyAt = Time.time + Mathf.Max(.1f, cooldown); }
            else voices.Remove(this);
        }
        static float Height(Transform hinge, Transform tip, Vector3 up)
        {
            if (hinge == null || tip == null) return float.NaN;
            Vector3 span = tip.position - hinge.position;
            return span.sqrMagnitude > .000001f ? Vector3.Dot(span.normalized, up) : float.NaN;
        }
        bool TryWeightedClip(out AnimationClip actual, out float weight)
        {
            actual = null; weight = 0;
            var animator = motor.animator;
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null || animator.speed <= 0) return false;
            // Flight is base-layer locomotion. Action layers must not spoof
            // another state clock. Each reference must actually be weighted.
            float incomingWeight = animator.IsInTransition(0) ? Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime) : 0;
            clips.Clear(); animator.GetCurrentAnimatorClipInfo(0, clips);
            Select(clips, 1 - incomingWeight, ref actual, ref weight);
            if (animator.IsInTransition(0)) { clips.Clear(); animator.GetNextAnimatorClipInfo(0, clips); Select(clips, incomingWeight, ref actual, ref weight); }
            return actual != null && weight >= minimumClipWeight;
        }
        void Select(List<AnimatorClipInfo> values, float stateWeight, ref AnimationClip selected, ref float weight)
        {
            foreach (var item in values)
                if (item.clip != null && Array.IndexOf(flappingClips, item.clip) >= 0 && item.weight * stateWeight > weight)
                { selected = item.clip; weight = item.weight * stateWeight; }
        }
        void ApplyGain()
        {
            float value = Mathf.Clamp01(gain) * (settings != null ? Mathf.Clamp01(settings.sfx) : 0) * Mathf.Clamp01(activeWeight);
            if (wingEvent != null) wingEvent.volume = value;
            if (source != null) source.volume = value;
        }
        static void PruneVoices()
        {
            for (int index = voices.Count - 1; index >= 0; index--)
            {
                var item = voices[index];
                if (item == null || !item.isActiveAndEnabled || item.source == null || !item.source.isPlaying && !item.heldAudio) voices.RemoveAt(index);
            }
        }
        bool ReserveVoice()
        {
            PruneVoices(); if (voices.Contains(this)) return true;
            if (voices.Count >= MaximumConcurrentVoices) return false;
            voices.Add(this); return true;
        }
        public void StopAndReset()
        {
            if (source != null) { source.Stop(); source.clip = null; }
            voices.Remove(this); tracking = ascending = heldAudio = wasPrepared = false;
            ObservedClip = null; ObservedWeight = LastStrokeExcursion = readyAt = activeWeight = 0;
            LastEmissionClip = null; LastEmissionWeight = 0;
            EmittedStrokes = ObservedStrokes = 0;
        }
        void OnDisable() => StopAndReset();
        void OnDestroy() => voices.Remove(this);
    }
}
