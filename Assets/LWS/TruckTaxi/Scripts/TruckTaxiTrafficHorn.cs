using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiHonkPattern { Short, Double, Long, Angry }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiTrafficHorn : MonoBehaviour
    {
        private static readonly List<TruckTaxiTrafficHorn> Active = new List<TruckTaxiTrafficHorn>(8);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Active.Clear();

        private AudioSource source;
        private AudioClip ordinary, longClip, angryClip;
        private float nextAllowed, nextPart;
        private int partsRemaining, priority;
        private bool playing;
        public bool IsPlaying => playing;
        public float NextAllowed => nextAllowed;

        public void Configure(AudioClip shortClip, AudioClip sustainedClip, AudioClip rageClip)
        {
            if (source == null)
            {
                var child = new GameObject("Taxi Traffic Horn");
                child.transform.SetParent(transform, false);
                source = child.AddComponent<AudioSource>();
            }
            ordinary = shortClip;
            longClip = sustainedClip;
            angryClip = rageClip;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 6;
            source.maxDistance = 65;
            source.volume = .35f;
            TruckTaxiAudioController.Instance?.Route(source, TruckTaxiAudioCategory.World);
        }

        public bool TryHonk(TruckTaxiHonkPattern pattern, TruckTaxiDriverPersonality personality, bool highPriority = false)
        {
            if (source == null || ordinary == null || playing || Time.time < nextAllowed) return false;
            AudioClip clip = pattern == TruckTaxiHonkPattern.Angry ? angryClip ?? longClip ?? ordinary :
                pattern == TruckTaxiHonkPattern.Long ? longClip ?? ordinary : ordinary;
            if (clip == null || !Reserve(highPriority ? 2 : 0)) return false;
            priority = highPriority ? 2 : 0;
            partsRemaining = pattern == TruckTaxiHonkPattern.Double ? 2 : 1;
            playing = true;
            source.clip = clip;
            source.pitch = 1;
            source.loop = false;
            PlayPart();
            float low = personality >= TruckTaxiDriverPersonality.Aggressive ? 2 : 3;
            float high = personality >= TruckTaxiDriverPersonality.Aggressive ? 5 : 8;
            nextAllowed = Time.time + Random.Range(low, high);
            return true;
        }

        private bool Reserve(int requestedPriority)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] == null || !Active[i].playing) Active.RemoveAt(i);
            var camera = Camera.main;
            if (camera != null && (transform.position - camera.transform.position).sqrMagnitude > 65 * 65)
                return false;
            if (Active.Count < 5) { Active.Add(this); return true; }
            if (requestedPriority == 0) return false;
            Vector3 listener = camera != null ? camera.transform.position : transform.position;
            float distance = (transform.position - listener).sqrMagnitude;
            TruckTaxiTrafficHorn replacement = null;
            float farthest = distance;
            foreach (var candidate in Active)
            {
                if (candidate.priority >= requestedPriority) continue;
                float candidateDistance = (candidate.transform.position - listener).sqrMagnitude;
                if (candidateDistance > farthest)
                { replacement = candidate; farthest = candidateDistance; }
            }
            if (replacement == null) return false;
            replacement.StopPlayback(false);
            Active.Add(this);
            return true;
        }

        private void PlayPart()
        {
            partsRemaining--;
            source.Play();
            nextPart = Time.time + source.clip.length / Mathf.Max(.01f, source.pitch) + .12f;
        }

        private void Update()
        {
            if (!playing || Time.time < nextPart || source.isPlaying) return;
            if (partsRemaining > 0) PlayPart();
            else { playing = false; Active.Remove(this); }
        }

        public void ResetForPool()
        {
            StopPlayback(true);
        }

        private void StopPlayback(bool resetCooldown)
        {
            playing = false;
            partsRemaining = priority = 0;
            if (resetCooldown) nextAllowed = 0;
            nextPart = 0;
            Active.Remove(this);
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.pitch = 1;
            source.loop = false;
        }

        private void OnDisable() => ResetForPool();
    }
}
