using System.Text;
using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Opt-in on the Truck Taxi player only. NWH retains collision/physics authority.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiSignificantCollisionDiagnostic : MonoBehaviour
    {
        [Min(0)] public float minimumRelativeSpeed = .75f;
        [Min(0)] public float minimumImpulse = 1500f;
        [Min(.1f)] public float globalCooldown = 1f;
        [Min(.1f)] public float colliderCooldown = 5f;
        private VehicleController vehicle;
        private readonly TruckTaxiImpactLogThrottle throttle = new TruckTaxiImpactLogThrottle();
        private float nextSnapshot;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Attach(GameObject player)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (player == null) return;
            var controller = player.GetComponent<VehicleController>();
            if (controller == null)
            {
                Debug.LogWarning("TT COLLISION DIAGNOSTIC: player has no NWH VehicleController; not attached.", player);
                return;
            }
            var diagnostic = player.GetComponent<TruckTaxiSignificantCollisionDiagnostic>() ??
                player.AddComponent<TruckTaxiSignificantCollisionDiagnostic>();
            diagnostic.Initialize(controller);
#endif
        }

        public void Initialize(VehicleController controller)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (vehicle == controller)
            {
                if (isActiveAndEnabled) Subscribe();
                return;
            }
            Unsubscribe();
            vehicle = controller;
            throttle.Clear();
            if (isActiveAndEnabled) Subscribe();
#endif
        }

        private void OnEnable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Subscribe();
#endif
        }
        private void OnDisable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Unsubscribe();
#endif
        }

        // Parent-owned staging probes can call this before/after teleport and while immobile.
        // Collision-enter logging alone can miss an already resting or newly embedded obstruction.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void CaptureLocalOverlaps(string stage)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!isActiveAndEnabled || Time.unscaledTime < nextSnapshot) return;
            nextSnapshot = Time.unscaledTime + 1f;
            var body = GetComponent<Rigidbody>();
            if (body == null) return;
            var own = GetComponentsInChildren<Collider>(false);
            bool found = false;
            Bounds envelope = default;
            foreach (var collider in own)
            {
                if (!TruckTaxiWorldColliderReview.IsSolid(collider) || collider.attachedRigidbody != body) continue;
                if (!found) { envelope = collider.bounds; found = true; }
                else envelope.Encapsulate(collider.bounds);
            }
            if (!found) return;
            var candidates = new Collider[128];
            int count = Physics.OverlapBoxNonAlloc(envelope.center, envelope.extents + Vector3.one * .5f,
                candidates, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            var text = new StringBuilder($"TT LOCAL COLLISION SNAPSHOT stage={stage}; time={Time.unscaledTime:0.000}; " +
                $"player={TruckTaxiWorldColliderReview.Identity(transform)}; position={transform.position}; " +
                $"velocity={body.linearVelocity}; candidates={count}; saturated={count == candidates.Length}\n");
            text.AppendLine("Broadphase candidates include nearby ground; penetration is not a contact/visibility verdict. No geometry changed.");
            for (int i = 0; i < count; i++)
            {
                var other = candidates[i];
                if (!TruckTaxiWorldColliderReview.IsSolid(other) || other.attachedRigidbody == body || other.transform.IsChildOf(transform)) continue;
                text.AppendLine(DescribeCollider(other));
                foreach (var collider in own)
                {
                    if (!TruckTaxiWorldColliderReview.IsSolid(collider) || collider.attachedRigidbody != body ||
                        !collider.bounds.Intersects(other.bounds)) continue;
                    bool ignored = Physics.GetIgnoreLayerCollision(collider.gameObject.layer, other.gameObject.layer) ||
                        Physics.GetIgnoreCollision(collider, other);
                    text.Append($"pair truckCollider={TruckTaxiWorldColliderReview.Identity(collider.transform)}; ignored={ignored}; " +
                        $"layerOverrides={collider.includeLayers.value}/{collider.excludeLayers.value}/{other.includeLayers.value}/{other.excludeLayers.value}; ");
                    if (!SupportsPenetration(collider) && !SupportsPenetration(other))
                    { text.AppendLine("penetration=UNSUPPORTED shape pair; bounds-only candidate"); continue; }
                    bool penetrates = Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                        other, other.transform.position, other.transform.rotation, out var direction, out float depth);
                    text.AppendLine($"penetrates={penetrates}; depth={depth:0.0000}m; separateDirection={direction}");
                }
            }
            Debug.Log(text.ToString(), this);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool SupportsPenetration(Collider collider) => collider is BoxCollider || collider is SphereCollider ||
            collider is CapsuleCollider || collider is MeshCollider mesh && mesh.convex;
        private void Subscribe()
        {
            if (vehicle == null) return;
            vehicle.onCollision.RemoveListener(RecordImpact);
            vehicle.onCollision.AddListener(RecordImpact);
        }
        private void Unsubscribe()
        {
            if (vehicle != null) vehicle.onCollision.RemoveListener(RecordImpact);
        }
        private void RecordImpact(Collision collision)
        {
            if (!isActiveAndEnabled || collision == null || collision.collider == null) return;
            float speed = collision.relativeVelocity.magnitude;
            float impulse = collision.impulse.magnitude;
            if (!IsSignificant(speed, impulse, minimumRelativeSpeed, minimumImpulse) ||
                !throttle.TryAcquire(collision.collider.GetInstanceID(), Time.unscaledTime,
                    globalCooldown, colliderCooldown)) return;

            var text = new StringBuilder("TT SIGNIFICANT COLLISION (observation, not a defect verdict)\n");
            text.AppendLine($"unscaledTime={Time.unscaledTime:0.000}; frame={Time.frameCount}; " +
                $"relativeSpeed={speed:0.000}m/s; impulse={impulse:0.0}Ns; relativeVelocity={collision.relativeVelocity.ToString("F3")}");
            text.AppendLine("player=" + TruckTaxiWorldColliderReview.Identity(transform));
            text.AppendLine(DescribeCollider(collision.collider));
            int contacts = Mathf.Min(4, collision.contactCount);
            for (int i = 0; i < contacts; i++)
            {
                var contact = collision.GetContact(i);
                text.AppendLine($"contact[{i}] point={contact.point.ToString("F3")}; normal={contact.normal.ToString("F3")}; " +
                    $"separation={contact.separation:0.0000}; truckCollider={TruckTaxiWorldColliderReview.Identity(contact.thisCollider?.transform)}; " +
                    $"otherCollider={TruckTaxiWorldColliderReview.Identity(contact.otherCollider?.transform)}");
            }
            Debug.Log(text.ToString(), collision.collider);
        }
#endif

        public static bool IsSignificant(float speed, float impulse, float speedThreshold, float impulseThreshold) =>
            float.IsFinite(speed) && float.IsFinite(impulse) && speed >= 0 && impulse >= 0 &&
            (speed >= Mathf.Max(.01f, speedThreshold) || impulse >= Mathf.Max(.01f, impulseThreshold));

        // Reused by the read-only authoring validator; never performs a global object search.
        public static string DescribeCollider(Collider collider)
        {
            if (collider == null) return "collider=<missing>";
            var text = new StringBuilder();
            text.AppendLine("collider=" + TruckTaxiWorldColliderReview.Identity(collider.transform));
            text.AppendLine($"object={collider.name}; instance={collider.GetInstanceID()}; type={collider.GetType().FullName}; " +
                $"layer={collider.gameObject.layer} ({LayerMask.LayerToName(collider.gameObject.layer)}); " +
                $"enabled={collider.enabled}; active={collider.gameObject.activeInHierarchy}; trigger={collider.isTrigger}");
            var bounds = collider.bounds;
            text.AppendLine($"boundsCenter={bounds.center.ToString("F3")}; boundsSize={bounds.size.ToString("F3")}; " +
                $"min={bounds.min.ToString("F3")}; max={bounds.max.ToString("F3")}");
            text.AppendLine("parent=" + TruckTaxiWorldColliderReview.Identity(collider.transform.parent));
            var body = collider.attachedRigidbody;
            text.AppendLine(body == null ? "rigidbody=none (static)" :
                $"rigidbody={(body.isKinematic ? "kinematic" : "dynamic")}; owner={TruckTaxiWorldColliderReview.Identity(body.transform)}; " +
                $"mass={body.mass:0.0}; detectCollisions={body.detectCollisions}; velocity={body.linearVelocity}");
            if (collider is BoxCollider box)
                text.AppendLine($"localBoxCenter={box.center.ToString("F3")}; localBoxSize={box.size.ToString("F3")}");
            int solids = 0;
            foreach (var sibling in collider.GetComponents<Collider>())
                if (TruckTaxiWorldColliderReview.IsSolid(sibling)) solids++;
            text.AppendLine($"sameObjectSolids={solids}; duplicateStatus=UNVERIFIED (use service/world authoring audit)");
            var renderers = collider.GetComponents<Renderer>();
            if (renderers.Length == 0) text.AppendLine("rendererOnCollider=none (split visual/collision hierarchies require review)");
            foreach (var renderer in renderers)
                text.AppendLine($"renderer={renderer.GetType().Name}; enabled={renderer.enabled}; " +
                    $"forceRenderingOff={renderer.forceRenderingOff}; active={renderer.gameObject.activeInHierarchy}; " +
                    $"bounds={renderer.bounds}; materials={renderer.sharedMaterials.Length}");
            var terrain = collider.GetComponent<Terrain>();
            if (terrain != null) text.AppendLine($"terrainEnabled={terrain.enabled}; drawHeightmap={terrain.drawHeightmap}");
            string suspicion = TruckTaxiWorldColliderReview.Suspicion(collider);
            text.Append("geometryReview=" + (suspicion ?? "no bounds/visibility suspicion; camera visibility unverified"));
            return text.ToString();
        }
    }

    // Fixed storage bounds memory in long sessions. Check the global budget before formatting any log.
    public sealed class TruckTaxiImpactLogThrottle
    {
        private const int Capacity = 64;
        private readonly int[] ids = new int[Capacity];
        private readonly float[] times = new float[Capacity];
        private int count, next;
        private float lastGlobal = float.NegativeInfinity;
        public int TrackedCount => count;
        public bool TryAcquire(int colliderId, float now, float globalSeconds, float perColliderSeconds)
        {
            if (!float.IsFinite(now) || now - lastGlobal < Mathf.Max(.1f, globalSeconds)) return false;
            int slot = -1;
            for (int i = 0; i < count; i++)
                if (ids[i] == colliderId) { slot = i; break; }
            if (slot >= 0 && now - times[slot] < Mathf.Max(.1f, perColliderSeconds)) return false;
            if (slot < 0)
            {
                slot = next;
                next = (next + 1) % Capacity;
                count = Mathf.Min(Capacity, count + 1);
            }
            ids[slot] = colliderId; times[slot] = now; lastGlobal = now;
            return true;
        }
        public void Clear() { count = next = 0; lastGlobal = float.NegativeInfinity; }
    }
}
