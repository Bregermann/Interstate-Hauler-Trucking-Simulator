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
        public bool detonateOnImpact = true;
        [Range(.05f, 2)] public float postBounceFuse = .65f;
        [Range(1, 32)] public float blastRadius = 24;
        [Range(.5f, 3)] public float blastFalloffExponent = 1;
        [Range(0, 6000)] public float blastImpulse = 1200;
        [Range(0, 15)] public float pedestrianBlastSpeed = 10;
        [Range(0, 30)] public float pedestrianUpwardSpeed = 28;
        [Range(0, 6000)] public float vehicleBlastImpulse = 4500;
        [Tooltip("Arcade traffic kick in meters/second before distance falloff. Applied as a bounded impulse, never a velocity override.")]
        [Range(0, 10)] public float vehicleBlastSpeed = 8;
        [Tooltip("Hard impulse ceiling for heavy vehicles; prevents mass scaling from becoming unbounded.")]
        [Range(6000, 100000)] public float maximumVehicleBlastImpulse = 80000;
        [Range(0, 6000)] public float upwardImpulse = 800;
        [Range(0, 1)] public float vehicleLiftFraction = .55f;
        [Range(0, 2)] public float vehicleSpinSpeed = 1.2f;
        [Range(1, 30)] public float directImpactSpeed = 8;
        public AudioClip detonationClip;
        public bool IsPlayerThrown { get; private set; } = true;
        public bool IsRoadRageGrenade { get; private set; }

        public event Action<HitReport> HitReported;
        public bool Detonated => detonated;

        private readonly HashSet<int> reportedActors = new HashSet<int>();
        private readonly HashSet<int> blastActors = new HashSet<int>();
        private readonly Collider[] blastContacts = new Collider[512];
        private Vector3 blastOrigin;
        public bool BlastContactBufferSaturated { get; private set; }
        public int BlastActorsAffected => blastActors.Count;
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

        public void ConfigureNpc(AudioClip clip, Rigidbody launchingBody, bool grenade)
        {
            Configure(clip, TruckTaxiBootstrap.Instance, launchingBody);
            IsPlayerThrown = false;
            IsRoadRageGrenade = grenade;
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
            ProcessImpact(collision.collider, contact, speed);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (detonated || body == null || body.isKinematic || other == null || IsLauncher(other) ||
                other.GetComponentInParent<TruckTaxiBreakawayProp>() == null) return;
            ProcessImpact(other, other.ClosestPoint(transform.position), body.linearVelocity.magnitude);
        }

        private void ProcessImpact(Collider collider, Vector3 contact, float speed)
        {
            if (!impacted)
            {
                impacted = true;
                PlayWorldSound(impactClip, contact);
            }
            if (speed >= 2) Affect(collider, contact, speed, 1, HitPhase.Direct);
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
            if (detonated || !TruckTaxiPedestrianImpactSettings.Finite(position)) return;
            detonated = true;
            CancelInvoke();
            blastOrigin = position;
            float radius = Bounded(blastRadius, 24, 1, 32);
            var nearestActors = new Dictionary<int, BlastContact>();
            int count = Physics.OverlapSphereNonAlloc(position, radius, blastContacts, ~0, QueryTriggerInteraction.Collide);
            BlastContactBufferSaturated = count == blastContacts.Length;
            for (int i = 0; i < count; i++)
            {
                var collider = blastContacts[i];
                blastContacts[i] = null;
                if (collider == null || collider == projectileCollider || IsLauncher(collider)) continue;
                if (collider.isTrigger && collider.GetComponentInParent<TruckTaxiBreakawayProp>() == null) continue;
                if (collider.attachedRigidbody == null && collider.GetComponentInParent<TruckTaxiPedestrian>() == null &&
                    collider.GetComponentInParent<TruckTaxiBreakawayProp>() == null) continue;
                Vector3 nearest = collider.ClosestPoint(position);
                float strength = BlastFalloff(Vector3.Distance(position, nearest), radius, blastFalloffExponent);
                if (strength <= 0) continue;
                int key = ActorKey(collider);
                if (key != 0 && (!nearestActors.TryGetValue(key, out var previous) ||
                    strength > previous.Strength))
                    nearestActors[key] = new BlastContact(collider, nearest, strength);
            }
            var ordered = new List<BlastContact>(nearestActors.Values);
            ordered.Sort((a, b) => b.Strength.CompareTo(a.Strength));
            // Retain the strongest 64 contacts. Process center actors last so the existing
            // oldest-ragdoll retirement policy does not discard them in favor of distant actors.
            for (int i = Mathf.Min(64, ordered.Count) - 1; i >= 0; i--)
            {
                var hit = ordered[i];
                Affect(hit.Collider, hit.Point, Mathf.Max(Bounded(directImpactSpeed, 8, 1, 30) * hit.Strength, 2),
                    hit.Strength, HitPhase.Blast);
            }
            var fuel = host != null ? host.GetComponent<TruckTaxiFuelController>() : null;
            PlayWorldSound(detonationClip != null ? detonationClip : fuel != null ? fuel.explosionClip : impactClip, position);
            SpawnBurst(position, radius);
            SafeDisable();
        }

        public static float BlastFalloff(float distance, float radius, float exponent = 2)
        {
            if (!float.IsFinite(distance) || !float.IsFinite(radius) || radius <= 0) return 0;
            float normalized = Mathf.Clamp01(1 - Mathf.Max(0, distance) / radius);
            return Mathf.Pow(normalized, Bounded(exponent, 2, .5f, 3));
        }

        public static float BoundedVehicleImpulse(float mass, float speed, float minimum, float maximum)
        {
            if (!float.IsFinite(mass) || !float.IsFinite(speed) || !float.IsFinite(minimum) || !float.IsFinite(maximum)) return 0;
            float limit = Mathf.Clamp(maximum, 0, 100000);
            return Mathf.Clamp(Mathf.Max(Mathf.Max(0, minimum), Mathf.Max(0, mass) * Mathf.Clamp(speed, 0, 10)), 0, limit);
        }
        private static float Bounded(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
        public static Vector3 BoundedVelocityChange(Vector3 current, Vector3 requested, float maximumSpeed)
        {
            if (!TruckTaxiPedestrianImpactSettings.Finite(current) || !TruckTaxiPedestrianImpactSettings.Finite(requested) ||
                !float.IsFinite(maximumSpeed) || maximumSpeed <= 0) return Vector3.zero;
            return Vector3.ClampMagnitude(current + requested, maximumSpeed) - current;
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
            if (collider == null || !collider.gameObject.activeInHierarchy) return;
            int key = ActorKey(collider);
            if (key == 0) return;
            bool firstHit = reportedActors.Add(key);
            bool radial = phase == HitPhase.Blast && blastActors.Add(key);
            if (!firstHit && !radial) return;
            var target = collider.GetComponentInParent<TruckTaxiImpactTarget>();
            var pedestrian = collider.GetComponentInParent<TruckTaxiPedestrian>();
            var prop = collider.GetComponentInParent<TruckTaxiBreakawayProp>();
            if (firstHit && pedestrian != null && !pedestrian.IsRagdoll)
            {
                Vector3 away = (pedestrian.transform.position - transform.position).normalized;
                if (away.sqrMagnitude < .01f) away = transform.forward;
                Vector3 strike = away * Mathf.Max(speed, pedestrian.settings.minimumRagdollImpactSpeed) +
                    Vector3.up * Mathf.Min(3, strength * 2);
                if (pedestrian.TryStrike(strike, contact))
                {
                    if (IsPlayerThrown && !pedestrian.HitEventSent && host?.Session != null)
                    {
                        host.Session.RecordPedestrianHit(pedestrian.PedestrianId, pedestrian.LastImpactSpeed,
                            pedestrian.LastImpulse, contact);
                        pedestrian.MarkHitEventSent();
                    }
                }
            }
            else if (firstHit && target != null && target.kind != TaxiImpactKind.Pedestrian)
            {
                bool mayDamage = target.kind != TaxiImpactKind.Property || prop != null ||
                    collider.attachedRigidbody != null && !collider.attachedRigidbody.isKinematic;
                bool valid = mayDamage && speed >= (host?.Configuration != null ? host.Configuration.minimumImpactSpeed : 3) &&
                    target.Hit(body, contact, speed);
                if (valid && IsPlayerThrown && host?.Session != null)
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

            if (radial) ApplyRadialForce(collider, pedestrian, target, prop, strength);
            if (firstHit) HitReported?.Invoke(new HitReport(phase, target != null ? target.kind : (TaxiImpactKind?)null,
                target != null ? target.targetId : null, contact, strength, IsPlayerThrown));
        }

        private void ApplyRadialForce(Collider collider, TruckTaxiPedestrian pedestrian, TruckTaxiImpactTarget target,
            TruckTaxiBreakawayProp prop, float strength)
        {
            if (pedestrian != null)
            {
                // TryStrike owns UTS activation; its population callback enforces the existing ragdoll budget.
                if (!pedestrian.IsRagdoll || !pedestrian.gameObject.activeInHierarchy || pedestrian.RagdollBodies == null) return;
                Vector3 direction = Vector3.ProjectOnPlane(pedestrian.transform.position - blastOrigin, Vector3.up).normalized;
                if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
                Vector3 kick = (direction * Bounded(pedestrianBlastSpeed, 10, 0, 15) + Vector3.up * Bounded(pedestrianUpwardSpeed, 28, 0, 30)) * strength;
                int count = 0;
                foreach (var limb in pedestrian.RagdollBodies)
                {
                    if (++count > 24) break;
                    if (limb != null && !limb.isKinematic && limb.gameObject.activeInHierarchy)
                        limb.AddForce(BoundedVelocityChange(limb.linearVelocity, kick, 30), ForceMode.VelocityChange);
                }
                return;
            }
            // Never activate authored structures or bypass a breakaway lease/retirement decision.
            if (prop != null && (prop.PreservesAuthoredPhysics || !prop.Activated || prop.Retired || prop.Destroyed)) return;
            var rigidbody = collider.attachedRigidbody;
            if (rigidbody == null || rigidbody == body || rigidbody.isKinematic || !rigidbody.gameObject.activeInHierarchy) return;
            Vector3 away = Vector3.ProjectOnPlane(rigidbody.worldCenterOfMass - blastOrigin, Vector3.up).normalized;
            if (away.sqrMagnitude < .01f) away = Vector3.forward;
            bool vehicle = target != null && target.kind == TaxiImpactKind.Traffic ||
                host?.Player != null && rigidbody == host.Player.GetComponent<Rigidbody>();
            float impulse = vehicle ? BoundedVehicleImpulse(rigidbody.mass, vehicleBlastSpeed, vehicleBlastImpulse, maximumVehicleBlastImpulse) : Bounded(blastImpulse, 1200, 0, 6000);
            float lift = vehicle ? impulse * Bounded(vehicleLiftFraction, .55f, 0, 1) : Bounded(upwardImpulse, 800, 0, 6000);
            rigidbody.AddForce((away * impulse + Vector3.up * lift) * strength, ForceMode.Impulse);
            if (vehicle) rigidbody.AddTorque(Vector3.Cross(Vector3.up, away) *
                Bounded(vehicleSpinSpeed, 1.2f, 0, 2) * strength, ForceMode.VelocityChange);
        }

        private void PlayWorldSound(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            var emitter = new GameObject("Taxi arcade blast audio");
            emitter.transform.position = position;
            var source = emitter.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3;
            source.maxDistance = 28;
            TruckTaxiAudioController.Instance?.Route(source, TruckTaxiAudioCategory.World);
            source.PlayOneShot(clip);
            Destroy(emitter, clip.length + .1f);
        }

        private void SpawnBurst(Vector3 position, float radius)
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
            main.startColor = IsRoadRageGrenade ?
                new ParticleSystem.MinMaxGradient(new Color(1f, .35f, .1f, .9f), new Color(1f, .8f, .2f, .65f)) :
                new ParticleSystem.MinMaxGradient(new Color(.9f, 1f, .25f, .9f), new Color(.2f, .8f, .25f, .65f));
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
