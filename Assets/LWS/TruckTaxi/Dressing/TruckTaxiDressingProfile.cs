using System;
using System.Collections.Generic;
using sc.splines.spawner.runtime;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiDressingContext { Forest, Suburb, Urban, Industrial, Highway, MotelDistrict, TruckStop, Construction }
    [CreateAssetMenu(menuName = "Truck Taxi/World/Dressing Profile")]
    public sealed class TruckTaxiDressingProfile : ScriptableObject
    {
        public TruckTaxiDressingContext context;
        [Min(0)] public int seed = 12345;
        public bool insideArea;
        [Min(.1f)] public float spacingMeters = 8;
        [Min(0)] public float clearanceMeters = 1;
        public List<SplineSpawner.SpawnableObject> prefabs = new List<SplineSpawner.SpawnableObject>();
        public bool Validate(out string reason)
        {
            reason = "Assign approved prefab assets and valid density/seed values.";
            if (!Enum.IsDefined(typeof(TruckTaxiDressingContext), context) || seed < 0 ||
                !float.IsFinite(spacingMeters) || spacingMeters < .1f || !float.IsFinite(clearanceMeters) || clearanceMeters < 0 ||
                prefabs == null || prefabs.Count == 0) return false;
            var seen = new HashSet<GameObject>();
            foreach (var entry in prefabs)
            {
                if (entry == null || entry.prefab == null || !seen.Add(entry.prefab) || entry.prefab.scene.IsValid() ||
                    !float.IsFinite(entry.probability) || entry.probability <= 0 || entry.probability > 100 ||
                    !Positive(entry.baseScale) || entry.prefab.GetComponentsInChildren<SplineSpawnerMask>(true).Length > 0 ||
                    entry.prefab.GetComponentsInChildren<SplineSpawner>(true).Length > 0) return false;
                foreach (var body in entry.prefab.GetComponentsInChildren<Rigidbody>(true))
                    if (!body.isKinematic) { reason = "Dressing pools cannot contain live dynamic vehicles/characters."; return false; }
                if (!TryPrefabBounds(entry.prefab, out _)) { reason = "Dressing prefab has no usable renderer bounds."; return false; }
            }
            reason = null; return true;
        }
        private static bool Positive(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && value.x > 0 && value.y > 0 && value.z > 0;
        public static int StableSeed(int seed, string bindingId)
        {
            if (seed < 0 || string.IsNullOrWhiteSpace(bindingId)) throw new ArgumentException("Stable seed and binding ID required.");
            unchecked { uint hash = 2166136261u ^ (uint)seed; foreach (char c in bindingId) hash = (hash ^ c) * 16777619u;
                return (int)(hash & 0x7fffffff); }
        }
        public static bool TryPrefabBounds(GameObject prefab, out Bounds bounds)
        {
            bounds = default; bool found = false;
            if (prefab == null) return false;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var local = TruckTaxiWorldColliderReview.TransformBounds(renderer.localBounds,
                    prefab.transform.worldToLocalMatrix * renderer.localToWorldMatrix);
                if (!float.IsFinite(local.center.sqrMagnitude) || !float.IsFinite(local.size.sqrMagnitude) || local.size.sqrMagnitude <= 0) return false;
                if (!found) { bounds = local; found = true; } else bounds.Encapsulate(local);
            }
            // Include collision protrusions, even when they extend beyond visible art.
            if (found) foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger) continue;
                Bounds local;
                if (collider is BoxCollider box) local = new Bounds(box.center, box.size);
                else if (collider is SphereCollider sphere) local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2);
                else if (collider is CapsuleCollider capsule)
                {
                    var size = Vector3.one * capsule.radius * 2; size[capsule.direction] = Mathf.Max(capsule.height, size[capsule.direction]);
                    local = new Bounds(capsule.center, size);
                }
                else if (collider is MeshCollider mesh && mesh.sharedMesh != null) local = mesh.sharedMesh.bounds;
                else return false;
                bounds.Encapsulate(TruckTaxiWorldColliderReview.TransformBounds(local,
                    prefab.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix));
            }
            return found && float.IsFinite(bounds.center.sqrMagnitude) && float.IsFinite(bounds.size.sqrMagnitude);
        }
    }
}
