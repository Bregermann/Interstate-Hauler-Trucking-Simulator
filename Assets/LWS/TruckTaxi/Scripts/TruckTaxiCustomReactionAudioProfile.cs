using System;
using UnityEngine;
using UnityEngine.Audio;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiCustomPhase { Started, Beat, Completed, Cancelled }

    [CreateAssetMenu(menuName = "Truck Taxi/Custom Content/Reaction Audio")]
    public sealed class TruckTaxiCustomReactionAudioProfile : ScriptableObject
    {
        public string stableId;
        public AudioClip[] startClips = Array.Empty<AudioClip>();
        public AudioClip[] loopClips = Array.Empty<AudioClip>();
        public AudioClip[] accentClips = Array.Empty<AudioClip>();
        public AudioClip[] completionClips = Array.Empty<AudioClip>();
        [Range(0, 1)] public float volume = .7f;
        public Vector2 pitchRange = new Vector2(.98f, 1.02f);
        [Min(0)] public float minimumInterval = 1.5f;
        [Min(0)] public float maximumInterval = 4;
        public bool allowOverlap;
        public bool shuffleWithoutImmediateRepeat = true;
        [Tooltip("Optional override. Null routes through the existing Voices bus.")]
        public AudioMixerGroup mixerGroup;
        [Range(0, 1)] public float insideCabGain = 1;
        [Range(0, 1)] public float exteriorGain = .35f;
        public AudioClip[] Clips(TruckTaxiCustomPhase phase, bool accent = false) =>
            phase == TruckTaxiCustomPhase.Started ? startClips :
            phase == TruckTaxiCustomPhase.Completed ? completionClips : accent ? accentClips : loopClips;
    }
}
