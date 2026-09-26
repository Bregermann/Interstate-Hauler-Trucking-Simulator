using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi
{
    // Owns only the jug loop and its generated non-speech fallback clip.
    public sealed class TruckTaxiJugLiquidLoop : IDisposable
    {
        private const int SampleRate = 22050;
        private readonly AudioSource source;
        private AudioClip generatedClip;
        private bool started;
        private bool paused;

        public bool IsPlaying => started && source != null && source.isPlaying && !paused;
        public bool UsesGeneratedClip => generatedClip != null && source != null && source.clip == generatedClip;

        public TruckTaxiJugLiquidLoop(AudioSource source, AudioClip clip)
        {
            this.source = source != null ? source : throw new ArgumentNullException(nameof(source));
            source.Stop();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = .5f;
            source.maxDistance = 15;
            source.volume = .8f;
            source.clip = clip;
        }

        public void Configure(AudioClip clip)
        {
            bool resume = started;
            bool wasPaused = paused;
            Stop();
            source.clip = null;
            ReleaseGeneratedClip();
            source.clip = clip;
            if (resume) { Play(); SetPaused(wasPaused); }
        }

        public void Play()
        {
            if (source == null) return;
            if (source.clip == null)
            {
                generatedClip = AudioClip.Create("TT Jug Liquid Stream (generated)", SampleRate * 3, 1, SampleRate, false);
                generatedClip.SetData(BuildSamples(SampleRate, 3), 0);
                source.clip = generatedClip;
            }
            paused = false;
            source.Play();
            started = true;
        }

        public void SetPaused(bool value)
        {
            if (source == null || !started || paused == value) return;
            paused = value;
            if (value) source.Pause();
            else source.UnPause();
        }

        public void Stop()
        {
            if (source != null) source.Stop();
            paused = false; started = false;
        }

        public void Dispose()
        {
            Stop();
            if (source != null) source.clip = null;
            ReleaseGeneratedClip();
        }

        private void ReleaseGeneratedClip()
        {
            if (generatedClip == null) return;
            if (Application.isPlaying) Object.Destroy(generatedClip);
            else Object.DestroyImmediate(generatedClip);
            generatedClip = null;
        }

        // A low-pass flowing bed plus irregular, decaying water drops. Tapered edges loop cleanly.
        public static float[] BuildSamples(int sampleRate, int seconds)
        {
            if (sampleRate < 8000 || seconds < 1 || seconds > 10)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            int length = checked(sampleRate * seconds);
            var samples = new float[length];
            var random = new System.Random(90210);
            float flow = 0, ripple = 0;
            float nextDrop = .13f, dropAge = 1, dropFrequency = 580;
            float edgeSamples = sampleRate * .025f;
            for (int i = 0; i < length; i++)
            {
                float time = (float)i / sampleRate;
                float noise = (float)random.NextDouble() * 2 - 1;
                flow += (noise - flow) * .08f;
                ripple += (noise - ripple) * .28f;
                if (time >= nextDrop)
                {
                    nextDrop += .12f + (float)random.NextDouble() * .23f;
                    dropAge = 0;
                    dropFrequency = 430 + (float)random.NextDouble() * 420;
                }
                float drop = dropAge < .045f
                    ? Mathf.Sin(2 * Mathf.PI * dropFrequency * dropAge) * Mathf.Exp(-dropAge * 88) * .19f : 0;
                dropAge += 1f / sampleRate;
                float edge = Mathf.Clamp01(Mathf.Min(i, length - 1 - i) / edgeSamples);
                samples[i] = Mathf.Clamp((flow * .18f + ripple * .08f + noise * .025f + drop) * 1.7f * edge, -.7f, .7f);
            }
            return samples;
        }
    }
}
