using System;
using UnityEngine;

namespace LWS.TruckSports
{
    // Rendering and pooled reactions only. Existing sport contacts remain the
    // sole authority for scoring, wrecks, elimination and physical debris.
    public sealed class SportsCarnivalLavaSurface : MonoBehaviour
    {
        public Renderer surface;
        public Material moltenMaterial;
        public Material[] originalMaterials;
        public ParticleSystem[] existingImpacts = Array.Empty<ParticleSystem>();
        public ParticleSystem[] reactionPool = Array.Empty<ParticleSystem>();
        public ParticleSystem[] ambientEffects = Array.Empty<ParticleSystem>();
        public float surfaceHeight;
        public Bounds worldEnvelope;
        public float PresentationTime { get; private set; }
        public int Reactions { get; private set; }
        MaterialPropertyBlock prior, block;
        bool[] playing;
        float[] particleTimes;
        readonly Vector4[] impacts = new Vector4[4];
        readonly int[] bodies = new int[32];
        readonly float[] bodyTimes = new float[32];
        int nextImpact, nextBody;
        Vector3 lastPosition;
        float lastReaction = -100;
        static readonly int ClockId = Shader.PropertyToID("_LavaTime");
        static readonly int[] ImpactIds = { Shader.PropertyToID("_Impact0"), Shader.PropertyToID("_Impact1"), Shader.PropertyToID("_Impact2"), Shader.PropertyToID("_Impact3") };
        void OnEnable()
        {
            if (surface == null || moltenMaterial == null) return;
            prior = new MaterialPropertyBlock(); surface.GetPropertyBlock(prior);
            block = new MaterialPropertyBlock(); surface.GetPropertyBlock(block);
            if (originalMaterials == null || originalMaterials.Length == 0) originalMaterials = surface.sharedMaterials;
            surface.sharedMaterial = moltenMaterial;
            playing = new bool[existingImpacts.Length]; particleTimes = new float[existingImpacts.Length];
            for (int i = 0; i < impacts.Length; i++) impacts[i] = new Vector4(0, 0, -100, 0);
            foreach (var effect in ambientEffects ?? Array.Empty<ParticleSystem>()) if (effect != null) effect.Play(true);
            WriteProperties();
        }
        void Update()
        {
            Advance(Time.deltaTime);
            for (int i = 0; playing != null && i < existingImpacts.Length; i++)
            {
                var particle = existingImpacts[i]; if (particle == null) continue;
                bool now = particle.isPlaying;
                if (now && (!playing[i] || particle.time + .001f < particleTimes[i])) ObserveImpact(particle.transform.position, 0, false);
                playing[i] = now; particleTimes[i] = particle.time;
            }
        }
        public void Advance(float seconds)
        {
            if (!isActiveAndEnabled || !float.IsFinite(seconds) || seconds < 0 || surface == null || block == null) return;
            PresentationTime += seconds; WriteProperties();
        }
        // Called only by an observer on the existing lava contact volume, or
        // by the existing sport's actual impact particle pool starting to play.
        public bool ObserveImpact(Vector3 point, int bodyId, bool emitSplash)
        {
            if (!isActiveAndEnabled || surface == null || block == null || !float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z)) return false;
            if (point.x < worldEnvelope.min.x || point.x > worldEnvelope.max.x || point.z < worldEnvelope.min.z || point.z > worldEnvelope.max.z) return false;
            if (bodyId != 0)
            {
                for (int i = 0; i < bodies.Length; i++) if (bodies[i] == bodyId && PresentationTime - bodyTimes[i] < .75f) return false;
                bodies[nextBody] = bodyId; bodyTimes[nextBody] = PresentationTime; nextBody = (nextBody + 1) % bodies.Length;
            }
            point.y = surfaceHeight;
            if (PresentationTime - lastReaction < .18f && Vector3.SqrMagnitude(point - lastPosition) < 16) return false;
            impacts[nextImpact] = new Vector4(point.x, point.z, PresentationTime, 1); nextImpact = (nextImpact + 1) % impacts.Length;
            lastReaction = PresentationTime; lastPosition = point; Reactions++;
            if (emitSplash && reactionPool.Length > 0)
            {
                var effect = reactionPool[(Reactions - 1) % reactionPool.Length];
                if (effect != null) { effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); effect.transform.position = point + Vector3.up * .15f; effect.Play(true); }
            }
            WriteProperties(); return true;
        }
        void WriteProperties()
        {
            if (surface == null || block == null) return;
            block.SetFloat(ClockId, PresentationTime);
            for (int i = 0; i < impacts.Length; i++) block.SetVector(ImpactIds[i], impacts[i]);
            surface.SetPropertyBlock(block);
        }
        void OnDisable()
        {
            foreach (var effect in ambientEffects ?? Array.Empty<ParticleSystem>()) if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var effect in reactionPool ?? Array.Empty<ParticleSystem>()) if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (surface == null) return;
            if (originalMaterials != null && originalMaterials.Length > 0) surface.sharedMaterials = originalMaterials;
            if (prior != null) surface.SetPropertyBlock(prior);
        }
    }
}
