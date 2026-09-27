using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class TruckTaxiFilledContainerProjectile : MonoBehaviour
    {
        public enum HitPhase { Direct, Blast }

        public readonly struct HitReport
        {
            public readonly HitPhase Phase;
            public readonly TaxiImpactKind? Kind;
            public readonly string TargetId;
            public readonly Vector3 Position;
            public readonly float Strength;
            public readonly bool PlayerThrown;

            public HitReport(HitPhase phase, TaxiImpactKind? kind, string targetId,
                Vector3 position, float strength, bool playerThrown)
            {
                Phase = phase; Kind = kind; TargetId = targetId;
                Position = position; Strength = strength; PlayerThrown = playerThrown;
            }
        }

        private readonly struct BlastContact
        {
            public readonly Collider Collider;
            public readonly Vector3 Point;
            public readonly float Strength;
            public BlastContact(Collider collider, Vector3 point, float strength)
            { Collider = collider; Point = point; Strength = strength; }
        }

        [Header("Arcade blast")]
        [Min(15)] public float maximumLifetime = 20;
        public bool detonateOnImpact;
        [Range(.05f, 2)] public float postBounceFuse = .65f;
        [Range(1, 20)] public float blastRadius = 6;
        [Range(0, 600)] public float blastImpulse = 180;
        [Range(0, 6000)] public float vehicleBlastImpulse = 4500;
        [Range(0, 100)] public float upwardImpulse = 35;
        [Range(1, 30)] public float directImpactSpeed = 8;
        public AudioClip detonationClip;

        public event Action<HitReport> HitReported;
        public bool Detonated => detonated;

        private readonly HashSet<int> reportedActors = new HashSet<int>();
        private AudioClip impactClip;
        private PhysicsMaterial bounceMaterial;
        private Rigidbody body;
        private Collider projectileCollider;
        private TruckTaxiBootstrap host;
        private Rigidbody launcher;
        private bool impacted;
        private bool detonated;
        private bool fuseStarted;
        private bool launcherIgnoredAfterRelease;

        // The original caller uses this signature. An owner-aware overload is available
        // when the throw coordinator can pass its already-known host and tractor body.
        public void Configure(AudioClip clip) => Configure(clip, TruckTaxiBootstrap.Instance,
            TruckTaxiBootstrap.Instance?.Player != null ?
                TruckTaxiBootstrap.Instance.Player.GetComponent<Rigidbody>() : null);

        public void Configure(AudioClip clip, TruckTaxiBootstrap owner, Rigidbody launchingBody)
        {
            impactClip = clip;
            host = owner;
            launcher = launchingBody;
            body = GetComponent<Rigidbody>();
            projectileCollider = GetComponent<Collider>();
            bounceMaterial = new PhysicsMaterial("Taxi container bounce") {
                bounciness = .32f, bounceCombine = PhysicsMaterialCombine.Maximum
            };
            projectileCollider.material = bounceMaterial;
            IgnoreLauncherCollisions();
            Destroy(gameObject, Mathf.Clamp(maximumLifetime, 15, 30));
        }

        private void FixedUpdate()
        {
            if (launcherIgnoredAfterRelease || body == null || body.isKinematic ||
                projectileCollider == null || !projectileCollider.enabled) return;
            IgnoreLauncherCollisions();
            launcherIgnoredAfterRelease = true;
        }

        private void IgnoreLauncherCollisions()
        {
            if (launcher == null || projectileCollider == null) return;
            foreach (var collider in launcher.GetComponentsInChildren<Collider>())
                if (collider != null && collider != projectileCollider)
                    Physics.IgnoreCollision(projectileCollider, collider, true);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (detonated || body == null || body.isKinematic || collision.collider == null ||
                IsLauncher(collision.collider)) return;

            Vector3 contact = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            float speed = collision.relativeVelocity.magnitude;
            if (!impacted)
            {
                impacted = true;
                PlayWorldSound(impactClip, contact);
            }
            if (speed >= 2) Affect(collision.collider, contact, speed, 1, HitPhase.Direct);
            if (detonateOnImpact) Detonate(contact);
            else if (!fuseStarted)
            {
                fuseStarted = true;
                Invoke(nameof(DetonateAfterBounce), Mathf.Clamp(postBounceFuse, .05f, 2));
            }
        }

        private void DetonateAfterBounce() { Detonate(transform.position); }

        public void Detonate(Vector3 position)
        {
            if (detonated) return;
            detonated = true;
            CancelInvoke();
            float radius = Mathf.Clamp(blastRadius, 1, 20);
            var nearestActors = new Dictionary<int, BlastContact>();
            foreach (var collider in Physics.OverlapSphere(position, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (collider == null || collider == projectileCollider || IsLauncher(collider)) continue;
                Vector3 nearest = collider.ClosestPoint(position);
                float strength = BlastFalloff(Vector3.Distance(position, nearest), radius);
                if (strength < .1f) continue;
                int key = ActorKey(collider);
                if (key != 0 && (!nearestActors.TryGetValue(key, out var previous) ||
                    strength > previous.Strength))
                    nearestActors[key] = new BlastContact(collider, nearest, strength);
            }
            foreach (var hit in nearestActors.Values)
                Affect(hit.Collider, hit.Point, Mathf.Max(directImpactSpeed * hit.Strength, 2),
                    hit.Strength, HitPhase.Blast);
            var fuel = host != null ? host.GetComponent<TruckTaxiFuelController>() : null;
            PlayWorldSound(detonationClip != null ? detonationClip : fuel != null ? fuel.explosionClip : impactClip, position);
            SpawnBurst(position, radius);
            SafeDisable();
        }

        public static float BlastFalloff(float distance, float radius)
        {
            if (!float.IsFinite(distance) || !float.IsFinite(radius) || radius <= 0) return 0;
            float normalized = Mathf.Clamp01(1 - Mathf.Max(0, distance) / radius);
            return normalized * normalized;
        }

        public static int ActorKey(Collider collider)
        {
            if (collider == null) return 0;
            var target = collider.GetComponentInParent<TruckTaxiImpactTarget>();
            if (target != null) return target.GetInstanceID();
            var pedestrian = collider.GetComponentInParent<TruckTaxiPedestrian>();
            if (pedestrian != null) return pedestrian.GetInstanceID();
            return collider.attachedRigidbody != null ? collider.attachedRigidbody.GetInstanceID() :
                collider.transform.root.GetInstanceID();
        }

        private bool IsLauncher(Collider collider) => launcher != null &&
            (collider.attachedRigidbody == launcher || collider.transform.IsChildOf(launcher.transform));

        private void Affect(Collider collider, Vector3 contact, float speed, float strength, HitPhase phase)
        {
            int key = ActorKey(collider);
            if (key == 0 || !reportedActors.Add(key)) return;
            var target = collider.GetComponentInParent<TruckTaxiImpactTarget>();
            var pedestrian = collider.GetComponentInParent<TruckTaxiPedestrian>();
            if (pedestrian != null && !pedestrian.IsRagdoll)
            {
                Vector3 away = (pedestrian.transform.position - transform.position).normalized;
                if (away.sqrMagnitude < .01f) away = transform.forward;
                Vector3 strike = away * Mathf.Max(speed, pedestrian.settings.minimumRagdollImpactSpeed) +
                    Vector3.up * Mathf.Min(3, strength * 2);
                if (pedestrian.TryStrike(strike, contact))
                {
                    if (!pedestrian.HitEventSent && host?.Session != null)
                    {
                        host.Session.RecordPedestrianHit(pedestrian.PedestrianId, pedestrian.LastImpactSpeed,
                            pedestrian.LastImpulse, contact);
                        pedestrian.MarkHitEventSent();
                    }
                }
            }
            else if (target != null && target.kind != TaxiImpactKind.Pedestrian)
            {
                bool valid = speed >= (host?.Configuration != null ? host.Configuration.minimumImpactSpeed : 3) &&
                    target.Hit();
                if (valid && host?.Session != null)
                {
                    if (target.kind == TaxiImpactKind.Traffic)
                    {
                        var objectives = host.GetComponent<TruckTaxiVehicleObjectiveCoordinator>();
                        if (objectives == null || !objectives.RecordExternalQualifiedHit(target, speed, GetInstanceID()))
                            host.Session.RecordEvent(TaxiEventType.TrafficRam, target.targetId, speed);
                    }
                    else host.Session.RecordEvent(TaxiEventType.PropDamage, target.targetId, speed);
                }
            }

            var rigidbody = collider.attachedRigidbody;
            if (pedestrian == null && rigidbody != null && rigidbody != body && !rigidbody.isKinematic)
            {
                Vector3 direction = (rigidbody.worldCenterOfMass - contact).normalized;
                if (direction.sqrMagnitude < .01f) direction = Vector3.up;
                float impulse = target != null && target.kind == TaxiImpactKind.Traffic ?
                    Mathf.Clamp(vehicleBlastImpulse, 0, 6000) : Mathf.Clamp(blastImpulse, 0, 600);
                rigidbody.AddForce((direction * impulse +
                    Vector3.up * Mathf.Clamp(upwardImpulse, 0, 100)) * strength, ForceMode.Impulse);
            }
            HitReported?.Invoke(new HitReport(phase, target != null ? target.kind : (TaxiImpactKind?)null,
                target != null ? target.targetId : null, contact, strength, launcher != null));
        }

        private void PlayWorldSound(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            var emitter = new GameObject("Taxi arcade blast audio");
            emitter.transform.position = position;
            var source = emitter.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1;
            source.maxDistance = 28;
            TruckTaxiAudioController.Instance?.Route(source, TruckTaxiAudioCategory.World);
            source.PlayOneShot(clip);
            Destroy(emitter, clip.length + .1f);
        }

        private static void SpawnBurst(Vector3 position, float radius)
        {
            var visual = new GameObject("Taxi cartoon container burst");
            visual.transform.position = position;
            var particles = visual.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = .7f;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 56;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * .25f, radius * .8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.12f, .55f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.9f, 1f, .25f, .9f),
                new Color(.2f, .8f, .25f, .65f));
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)56) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .35f;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var material = new Material(shader);
                visual.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                Destroy(material, 2);
            }
            particles.Play();
            Destroy(visual, 2);
        }

        public void SafeDisable()
        {
            detonated = true;
            CancelInvoke();
            if (projectileCollider != null) projectileCollider.enabled = false;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            Destroy(gameObject);
        }

        private void OnDestroy() { if (bounceMaterial != null) Destroy(bounceMaterial); }
    }
}
