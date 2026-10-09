using System.Collections.Generic;
using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Preserve serialized values; Structural was already authored as 3.
    public enum TruckTaxiPropPhysicsCategory { Lightweight = 0, Breakaway = 1, Destructible = 2, Structural = 3, HeavyPushable = 4, LargeVegetation = 5 }
    public enum TruckTaxiPropPartRole { Generic, PortableRestroomShell, PortableRestroomDoor, PortableRestroomFixture, LoadBearing }

    // Only scenery owned by this adapter consumes this budget. NWH/UTS bodies never do.
    public sealed class TruckTaxiPropPhysicsBudget
    {
        public static TruckTaxiPropPhysicsBudget Shared { get; } = new TruckTaxiPropPhysicsBudget(48, 24);
        public int MaximumBodies { get; }
        public int MaximumDebris { get; }
        private readonly HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();
        private readonly HashSet<Rigidbody> debris = new HashSet<Rigidbody>();
        public int ActiveBodies { get { Prune(); return bodies.Count; } }
        public int ActiveDebris { get { Prune(); return debris.Count; } }
        public TruckTaxiPropPhysicsBudget(int maximumBodies, int maximumDebris)
        {
            MaximumBodies = Mathf.Clamp(maximumBodies, 0, 128);
            MaximumDebris = Mathf.Clamp(maximumDebris, 0, MaximumBodies);
        }
        private void Prune() { bodies.RemoveWhere(value => value == null); debris.RemoveWhere(value => value == null); }
        public bool TryAcquire(Rigidbody value, bool isDebris)
        {
            Prune();
            if (value == null) return false;
            if (bodies.Contains(value)) return debris.Contains(value) == isDebris;
            if (bodies.Count >= MaximumBodies || (isDebris && debris.Count >= MaximumDebris)) return false;
            bodies.Add(value); if (isDebris) debris.Add(value);
            return true;
        }
        public void Release(Rigidbody value) { bodies.Remove(value); debris.Remove(value); }
    }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiBreakawayProp : MonoBehaviour
    {
        [Tooltip("Authored scenery ID. Required for portable-restroom pieces; never derive this from an instance ID.")]
        public string stableId;
        public TruckTaxiPropPartRole partRole;
        public TruckTaxiPropPhysicsCategory category = TruckTaxiPropPhysicsCategory.Breakaway;
        [Min(1)] public float activationSpeed = 2.5f;
        [Min(.1f)] public float massKg = 35f;
        [Min(.1f)] public float durability = 12f;
        [Min(.1f), Tooltip("Durability cost for legacy Hit() callers without measured impact context. No vehicle impulse is fabricated.")]
        public float damagePerHit = 4f;
        [Range(.1f, 2f)] public float hitCooldownSeconds = .5f;
        [Range(1f, 120f)] public float activeLifetimeSeconds = 30f;
        [Range(.5f, 10f)] public float settledRetirementSeconds = 2f;
        [Min(0)] public int debrisPieces = 2;
        [Min(1)] public float debrisLifetimeSeconds = 8f;
        public bool Activated { get; private set; }
        public bool Destroyed { get; private set; }
        public bool Retired { get; private set; }
        public float RemainingDurability { get; private set; }
        public int AcceptedHitCount { get; private set; }
        public int OwnedDebrisCount => chunks.Count;
        public string InitializationFailureReason { get; private set; }
        public TruckTaxiPropPhysicsCategory EffectiveCategory
        {
            get
            {
                if (partRole == TruckTaxiPropPartRole.LoadBearing) return TruckTaxiPropPhysicsCategory.Structural;
                if (category == TruckTaxiPropPhysicsCategory.Structural || category == TruckTaxiPropPhysicsCategory.HeavyPushable ||
                    category == TruckTaxiPropPhysicsCategory.LargeVegetation) return category;
                switch (partRole)
                {
                    case TruckTaxiPropPartRole.PortableRestroomShell: return TruckTaxiPropPhysicsCategory.Destructible;
                    case TruckTaxiPropPartRole.PortableRestroomDoor: return TruckTaxiPropPhysicsCategory.Breakaway;
                    case TruckTaxiPropPartRole.PortableRestroomFixture: return TruckTaxiPropPhysicsCategory.Lightweight;
                    default: return category;
                }
            }
        }
        public bool PreservesAuthoredPhysics => EffectiveCategory == TruckTaxiPropPhysicsCategory.Structural ||
            EffectiveCategory == TruckTaxiPropPhysicsCategory.HeavyPushable || EffectiveCategory == TruckTaxiPropPhysicsCategory.LargeVegetation;
        private const int MaximumColliders = 16, MaximumIgnoredPairs = 256;
        private Rigidbody body;
        private Collider[] colliders;
        private Renderer[] renderers;
        private bool[] colliderEnabled, colliderTrigger, rendererEnabled;
        private bool initialized, ownsBody, originallyKinematic, originalGravity, originalDetectCollisions;
        private float originalMass, elapsed, nextHitAt, activatedAt, settledTime, solidAt;
        private bool solid;
        private Vector3 home;
        private Quaternion homeRotation;
        private readonly List<CollisionPair> ignoredPairs = new List<CollisionPair>();
        private readonly List<Debris> chunks = new List<Debris>(4);
        private struct CollisionPair { public Collider Prop, Vehicle; public bool WasIgnored; }
        private sealed class Debris { public Rigidbody Body; public Collider Collider; public float ExpiresAt; }

        private static float Bounded(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
        private void Awake() => InitializePhysics();
        public void Configure(TruckTaxiPropPhysicsCategory nextCategory, float nextMassKg)
        {
            // Bootstrap may refine a newly added soft prop immediately after Awake.
            if (Activated || Destroyed || Retired) return;
            category = nextCategory; massKg = Bounded(nextMassKg, 35, .1f, 250);
            if (initialized && PreservesAuthoredPhysics) RestoreAuthoredPhysics();
            if (!PreservesAuthoredPhysics) InitializePhysics();
            if (body != null && initialized) body.mass = massKg;
        }
        public bool ValidateAuthoring(out string error)
        {
            error = null;
            if (PreservesAuthoredPhysics) return true;
            if (partRole != TruckTaxiPropPartRole.Generic && (string.IsNullOrWhiteSpace(stableId) || stableId.Length > 128))
                error = "Portable-restroom pieces require a stable authored ID (1-128 characters).";
            else if (GetComponentInParent<VehicleController>() != null ||
                GetComponentInParent<TruckTaxiImpactTarget>() is TruckTaxiImpactTarget target && target.kind != TaxiImpactKind.Property)
                error = "Breakaway scenery must not own vehicle or pedestrian physics.";
            else
            {
                var ownColliders = GetComponentsInChildren<Collider>(true);
                if (ownColliders.Length == 0 || ownColliders.Length > MaximumColliders)
                    error = "A breakaway piece requires 1-16 colliders; author smaller independent pieces.";
                foreach (var child in GetComponentsInChildren<TruckTaxiBreakawayProp>(true))
                    if (child != this) error = "Nested breakaway/structural pieces require separate sibling roots.";
                foreach (var child in GetComponentsInChildren<Rigidbody>(true))
                    if (child.gameObject != gameObject) error = "Nested rigidbodies must retain their separate physics owner.";
                if (transform.parent != null && transform.parent.GetComponentInParent<Rigidbody>() != null)
                    error = "Breakaway pieces must not be children of another rigidbody.";
                foreach (var collider in ownColliders)
                    if (collider is MeshCollider mesh && !mesh.convex)
                        error = "Dynamic breakaway pieces require primitive or authored convex colliders.";
            }
            return error == null;
        }
        public bool InitializePhysics()
        {
            if (initialized && PreservesAuthoredPhysics) { RestoreAuthoredPhysics(); return false; }
            if (initialized) return true;
            RemainingDurability = Bounded(durability, 12, .1f, 10000);
            if (PreservesAuthoredPhysics) return false;
            if (!ValidateAuthoring(out string error)) { InitializationFailureReason = error; return false; }
            InitializationFailureReason = null;
            home = transform.position; homeRotation = transform.rotation;
            colliders = GetComponentsInChildren<Collider>(true); renderers = GetComponentsInChildren<Renderer>(true);
            colliderEnabled = new bool[colliders.Length]; colliderTrigger = new bool[colliders.Length];
            rendererEnabled = new bool[renderers.Length];
            for (int i = 0; i < colliders.Length; i++)
            { colliderEnabled[i] = colliders[i].enabled; colliderTrigger[i] = colliders[i].isTrigger; colliders[i].isTrigger = true; }
            for (int i = 0; i < renderers.Length; i++) rendererEnabled[i] = renderers[i].enabled;
            body = GetComponent<Rigidbody>(); ownsBody = body == null;
            if (ownsBody) body = gameObject.AddComponent<Rigidbody>();
            originallyKinematic = body.isKinematic; originalGravity = body.useGravity;
            originalDetectCollisions = body.detectCollisions; originalMass = body.mass;
            body.mass = Bounded(massKg, 35, .1f, 250); body.isKinematic = true;
            initialized = true;
            return true;
        }
        private bool IsVehicle(Rigidbody source)
        {
            if (source == null || source == body) return false;
            if (source == TruckTaxiBootstrap.Instance?.Player?.GetComponent<Rigidbody>()) return true;
            var target = source.GetComponentInParent<TruckTaxiImpactTarget>();
            return target != null && target.kind == TaxiImpactKind.Traffic;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !IsVehicle(other.attachedRigidbody)) return;
            var source = other.attachedRigidbody;
            bool first = !Activated;
            float speed = (source.linearVelocity - (body != null ? body.linearVelocity : Vector3.zero)).magnitude;
            if (!TryReceiveImpact(source, other.ClosestPoint(transform.position), speed) || !first) return;
            var host = TruckTaxiBootstrap.Instance;
            if (source != host?.Player?.GetComponent<Rigidbody>()) return;
            var impact = GetComponent<TruckTaxiImpactTarget>();
            if (impact == null || impact.MarkPropertyDamage())
                host.Session?.RecordEvent(TaxiEventType.PropDamage, !string.IsNullOrWhiteSpace(stableId) ? stableId : impact != null ? impact.targetId : name, speed);
            if (speed > 8f) host.ComponentDamage?.ReceiveImpact(other.ClosestPoint(transform.position), speed, body.mass * speed);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsVehicle(collision.rigidbody)) return;
            TryReceiveImpact(collision.rigidbody, collision.contactCount > 0 ? collision.GetContact(0).point : transform.position,
                collision.relativeVelocity.magnitude);
        }
        // Existing collision/explosion adapters can supply measured impacts without owning this body's state.
        public bool TryReceiveImpact(Rigidbody source, Vector3 contact, float speed)
        {
            if (source == null || source == body || !float.IsFinite(speed) || speed < Bounded(activationSpeed, 2.5f, 1, 80) ||
                !float.IsFinite(contact.x) || !float.IsFinite(contact.y) || !float.IsFinite(contact.z)) return false;
            speed = Mathf.Min(speed, 80);
            return AcceptHit(speed * Bounded(source.mass / 2000f, 1, 1, 20), source, speed);
        }
        public bool TryReceiveHit() => AcceptHit(Bounded(damagePerHit, 4, .1f, 10000), null, 0);
        private bool AcceptHit(float damage, Rigidbody source, float speed)
        {
            if (PreservesAuthoredPhysics || Destroyed || Retired || !isActiveAndEnabled || elapsed < nextHitAt || !InitializePhysics()) return false;
            // InitializePhysics may discover an authored body after the caller's initial guard.
            if (source != null && source == body) return false;
            if (!Activated && !TruckTaxiPropPhysicsBudget.Shared.TryAcquire(body, false))
            { RetirePhysics(); return false; }
            if (source != null && !IgnoreVehicle(source)) { RetirePhysics(); return false; }
            nextHitAt = elapsed + Bounded(hitCooldownSeconds, .5f, .1f, 2);
            AcceptedHitCount++;
            if (!Activated) { activatedAt = elapsed; Activated = true; solidAt = elapsed + .35f; }
            settledTime = 0;
            body.isKinematic = false;
            // Trigger grace cannot fall through the road before the vehicle clears the prop.
            body.useGravity = solid && originalGravity;
            if (source != null)
            {
                Vector3 direction = Vector3.ProjectOnPlane(transform.position - source.worldCenterOfMass, Vector3.up).normalized;
                if (direction.sqrMagnitude < .01f) direction = source.transform.forward;
                body.AddForce((direction + Vector3.up * .22f) * Mathf.Min(18, speed * .8f), ForceMode.VelocityChange);
                body.AddTorque(Vector3.Cross(direction, Vector3.up) * Mathf.Min(9, speed * .4f), ForceMode.VelocityChange);
            }
            if (EffectiveCategory == TruckTaxiPropPhysicsCategory.Destructible)
            {
                RemainingDurability = Mathf.Max(0, RemainingDurability - damage);
                if (RemainingDurability <= 0) Shatter();
            }
            return true;
        }
        private bool IgnoreVehicle(Rigidbody source)
        {
            var vehicleColliders = source.GetComponentsInChildren<Collider>(true);
            if (vehicleColliders.Length * colliders.Length > MaximumIgnoredPairs) return false;
            foreach (var own in colliders)
                foreach (var other in vehicleColliders)
                {
                    if (!own.enabled || !other.enabled || !other.gameObject.activeInHierarchy) continue;
                    bool found = false;
                    foreach (var pair in ignoredPairs) if (pair.Prop == own && pair.Vehicle == other) { found = true; break; }
                    if (found) continue;
                    if (ignoredPairs.Count >= MaximumIgnoredPairs) return false;
                    ignoredPairs.Add(new CollisionPair { Prop = own, Vehicle = other, WasIgnored = Physics.GetIgnoreCollision(own, other) });
                    Physics.IgnoreCollision(own, other, true);
                }
            return true;
        }
        private void RestoreIgnoredPairs()
        {
            foreach (var pair in ignoredPairs)
                if (pair.Prop != null && pair.Vehicle != null) Physics.IgnoreCollision(pair.Prop, pair.Vehicle, pair.WasIgnored);
            ignoredPairs.Clear();
        }
        private void Update() => MaintainPhysics(Time.deltaTime);
        public void MaintainPhysics(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0) return;
            seconds = Mathf.Min(seconds, 1); elapsed += seconds;
            for (int i = chunks.Count - 1; i >= 0; i--)
                if (chunks[i].Body == null || elapsed >= chunks[i].ExpiresAt) RemoveChunk(i);
            if (initialized && PreservesAuthoredPhysics) { RestoreAuthoredPhysics(); return; }
            if (!initialized || !Activated || Retired || Destroyed || body == null) return;
            if (!solid && elapsed >= solidAt)
            {
                solid = true;
                for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].isTrigger = colliderTrigger[i];
                body.useGravity = originalGravity;
            }
            // Restore each pre-existing ignore state only after its bounds have separated.
            if (solid)
                for (int i = ignoredPairs.Count - 1; i >= 0; i--)
                {
                    var pair = ignoredPairs[i];
                    if (pair.Prop != null && pair.Vehicle != null && pair.Prop.bounds.Intersects(pair.Vehicle.bounds)) continue;
                    if (pair.Prop != null && pair.Vehicle != null) Physics.IgnoreCollision(pair.Prop, pair.Vehicle, pair.WasIgnored);
                    ignoredPairs.RemoveAt(i);
                }
            settledTime = body.IsSleeping() ? settledTime + seconds : 0;
            if (elapsed - activatedAt >= Bounded(activeLifetimeSeconds, 30, 1, 120) ||
                settledTime >= Bounded(settledRetirementSeconds, 2, .5f, 10)) RetirePhysics();
        }
        public void RetirePhysics()
        {
            if (!initialized || PreservesAuthoredPhysics) return;
            // Leave a nonblocking visual at its current pose, never an invisible solid or a home teleport.
            foreach (var collider in colliders) if (collider != null) collider.enabled = false;
            if (body != null)
            {
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = true; body.useGravity = originalGravity;
                TruckTaxiPropPhysicsBudget.Shared.Release(body);
            }
            RestoreIgnoredPairs(); Retired = true;
        }
        private void Shatter()
        {
            Destroyed = true;
            RetirePhysics();
            foreach (var renderer in renderers) if (renderer != null) renderer.enabled = false;
            for (int i = 0; i < Mathf.Clamp(debrisPieces, 0, 4); i++)
            {
                if (TruckTaxiPropPhysicsBudget.Shared.ActiveDebris >= TruckTaxiPropPhysicsBudget.Shared.MaximumDebris ||
                    TruckTaxiPropPhysicsBudget.Shared.ActiveBodies >= TruckTaxiPropPhysicsBudget.Shared.MaximumBodies) break;
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube); chunk.name = "Temporary prop debris";
                chunk.transform.position = transform.position + Vector3.up * .4f;
                chunk.transform.localScale = Vector3.one * .22f;
                chunk.transform.SetParent(transform, true);
                if (renderers.Length > 0 && renderers[0] != null) chunk.GetComponent<Renderer>().sharedMaterial = renderers[0].sharedMaterial;
                var piece = chunk.AddComponent<Rigidbody>(); piece.isKinematic = true;
                if (!TruckTaxiPropPhysicsBudget.Shared.TryAcquire(piece, true)) { DestroyOwned(chunk); break; }
                // Cosmetic fragments cannot catch a vehicle or become an unbudgeted static obstacle.
                var collider = chunk.GetComponent<Collider>(); collider.enabled = false;
                piece.mass = .2f; piece.isKinematic = false;
                piece.AddForce((Vector3.up + transform.right * (i % 2 == 0 ? 1 : -1)) * 2, ForceMode.VelocityChange);
                chunks.Add(new Debris { Body = piece, Collider = collider,
                    ExpiresAt = elapsed + Bounded(debrisLifetimeSeconds, 8, 1, 30) });
            }
        }
        private static void DestroyOwned(Object value)
        { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        private void RemoveChunk(int index)
        {
            var chunk = chunks[index]; chunks.RemoveAt(index);
            TruckTaxiPropPhysicsBudget.Shared.Release(chunk.Body);
            if (chunk.Body == null) return;
            if (chunk.Collider != null) chunk.Collider.enabled = false;
            chunk.Body.isKinematic = true; chunk.Body.gameObject.SetActive(false); DestroyOwned(chunk.Body.gameObject);
        }
        private void ClearChunks() { for (int i = chunks.Count - 1; i >= 0; i--) RemoveChunk(i); }
        public void ResetProp()
        {
            if (!initialized || PreservesAuthoredPhysics || body == null) return;
            RetirePhysics(); ClearChunks();
            transform.SetPositionAndRotation(home, homeRotation);
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null)
            { colliders[i].enabled = colliderEnabled[i]; colliders[i].isTrigger = true; }
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = rendererEnabled[i];
            RemainingDurability = Bounded(durability, 12, .1f, 10000);
            Destroyed = Activated = Retired = solid = false; AcceptedHitCount = 0; nextHitAt = elapsed; settledTime = 0;
        }
        private void RestoreAuthoredPhysics()
        {
            if (!initialized) return;
            ClearChunks(); RestoreIgnoredPairs(); TruckTaxiPropPhysicsBudget.Shared.Release(body);
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null)
            { colliders[i].enabled = colliderEnabled[i]; colliders[i].isTrigger = colliderTrigger[i]; }
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = rendererEnabled[i];
            if (body != null)
            {
                body.isKinematic = originallyKinematic; body.useGravity = originalGravity;
                body.detectCollisions = originalDetectCollisions; body.mass = originalMass;
                if (ownsBody) { body.isKinematic = true; DestroyOwned(body); }
            }
            initialized = false; body = null;
        }
        private void OnDisable()
        {
            ClearChunks();
            if (initialized && PreservesAuthoredPhysics) RestoreAuthoredPhysics(); else RetirePhysics();
        }
        private void OnDestroy() { ClearChunks(); TruckTaxiPropPhysicsBudget.Shared.Release(body); RestoreIgnoredPairs(); }
    }
}
