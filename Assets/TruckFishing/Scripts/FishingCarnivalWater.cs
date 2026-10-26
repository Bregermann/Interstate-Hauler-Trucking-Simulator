using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckFishing
{
    // Presentation over the existing analytic fish trajectories. No fish/net
    // positions, catch samples, speed plan, QTE or scoring state are written.
    [DefaultExecutionOrder(200)]
    public sealed class FishingCarnivalWater : MonoBehaviour
    {
        public FishingStream stream;
        public FishingMatch match;
        public Transform surface;
        public Camera view;
        public float localWaterHeight = .27f;
        public ParticleSystem ripples, foam, droplets, separation;
        public int Emissions { get; private set; }
        public float LastSpeed { get; private set; }
        public float LastSurfaceWeight { get; private set; }
        public float WaterY => stream.transform.TransformPoint(new Vector3(0, localWaterHeight, 0)).y;
        readonly List<Sample> samples = new List<Sample>(FishingStream.Capacity);
        sealed class Sample { public Transform model; public Renderer body; public Vector3 previous; public float next; public bool wet; }
        FishingStream subscribed;
        float clock;
        bool stopped;
        void OnEnable() { if (stream != null) { subscribed = stream; subscribed.Spawned += Spawned; } }
        void Spawned(FishingStream owner, Transform model)
        {
            var sample = samples.Find(entry => entry.model == model);
            if (sample == null)
            {
                if (samples.Count >= FishingStream.Capacity) return;
                Renderer body = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (body == null) body = model.GetComponentInChildren<Renderer>(true);
                sample = new Sample { model = model, body = body }; samples.Add(sample);
            }
            sample.previous = model.position; sample.next = clock; sample.wet = false;
        }
        void LateUpdate()
        {
            if (match == null || stream == null || match.Paused || Time.deltaTime <= 0) return;
            if (!match.Running) { if (!stopped) { ClearParticles(); stopped = true; } return; }
            stopped = false; TickPresentation(Time.deltaTime);
        }
        public void TickPresentation(float dt)
        {
            if (!isActiveAndEnabled || !float.IsFinite(dt) || dt <= 0 || stream == null) return;
            clock += dt; int budget = 16;
            foreach (var sample in samples)
            {
                if (sample.model == null || !sample.model.gameObject.activeInHierarchy || sample.body == null) continue;
                Vector3 position = sample.model.position, velocity = (position - sample.previous) / dt; sample.previous = position;
                var bounds = sample.body.bounds;
                float weight = SurfaceWeight(bounds.min.y, bounds.max.y, WaterY);
                bool wet = weight > .01f;
                if (view != null)
                {
                    Vector3 screen = view.WorldToViewportPoint(position);
                    if (!view.enabled || screen.z <= 0 || screen.x < -.1f || screen.x > 1.1f || screen.y < -.1f || screen.y > 1.1f) { sample.wet = wet; continue; }
                }
                if (wet && clock >= sample.next && budget-- > 0)
                {
                    float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), .15f, 1.2f);
                    Disturb(position, velocity, weight, radius, !sample.wet);
                    sample.next = clock + Mathf.Lerp(.4f, .11f, Mathf.Clamp01(velocity.magnitude / 12));
                }
                sample.wet = wet;
            }
        }
        public static float SurfaceWeight(float bottom, float top, float waterHeight)
        {
            if (!float.IsFinite(bottom) || !float.IsFinite(top) || !float.IsFinite(waterHeight) || bottom > waterHeight + .04f) return 0;
            return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .65f, waterHeight - top));
        }
        // Wildlife uses the same surface response at its actual animated feet
        // or swimming body. This never contacts the catch/net authority.
        public void Disturb(Vector3 point, Vector3 velocity, float weight, float radius, bool entering)
        {
            if (!isActiveAndEnabled || weight <= .01f || !float.IsFinite(point.x) || !float.IsFinite(point.z)) return;
            weight = Mathf.Clamp01(weight); radius = Mathf.Clamp(radius, .1f, 1.5f);
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            LastSpeed = speed; LastSurfaceWeight = weight; Emissions++;
            point.y = WaterY + .012f;
            var horizontal = new Vector3(velocity.x, 0, velocity.z); var backward = horizontal.sqrMagnitude > .001f ? -horizontal.normalized : Vector3.back;
            Emit(ripples, point, backward * Mathf.Min(speed * .035f, .4f), radius * 1.2f, weight * .32f, 1, true);
            if (speed > .5f)
            {
                Emit(foam, point + backward * radius * .45f, backward * speed * .07f, radius * Mathf.Lerp(.6f, 1.8f, Mathf.Clamp01(speed / 12)), weight * .28f, Mathf.Clamp((int)(speed * .25f), 1, 4), true);
                Emit(separation, point, horizontal * .03f, radius * 1.6f, weight * .16f, 1, true);
            }
            if (entering || speed > 6)
                Emit(droplets, point, Vector3.up * Mathf.Clamp(speed * .12f, .3f, 1.8f), .045f + radius * .025f, weight * .55f, entering ? 5 : 2, false);
        }
        static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, float size, float alpha, int count, bool surface)
        {
            if (system == null || system.particleCount >= system.main.maxParticles) return;
            if (!system.isPlaying) system.Play(false);
            var emit = new ParticleSystem.EmitParams { position = position, velocity = velocity, startSize = size, startColor = new Color(.76f, .93f, 1, alpha), rotation3D = surface ? new Vector3(0, Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg, 0) : Vector3.zero };
            system.Emit(emit, Mathf.Min(count, system.main.maxParticles - system.particleCount));
        }
        void ClearParticles() { foreach (var system in new[] { ripples, foam, droplets, separation }) if (system != null) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        void OnDisable() { if (subscribed != null) subscribed.Spawned -= Spawned; subscribed = null; ClearParticles(); samples.Clear(); }
    }
}
