using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckSports
{
    // Sports-owned visual content. NWH retains all vehicle/collision authority.
    // Requested IDs remain in profile data when content is unavailable.
    [CreateAssetMenu(menuName = "Truck Sports/Truck Appearance Catalog")]
    public sealed class SportsTruckAppearanceCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Mount
        {
            public string slotId;
            public Vector3 position, euler;
            public Vector3 scale = Vector3.one;
        }
        [Serializable]
        public sealed class Body
        {
            public string id, label, category;
            public GameObject visualPrefab;
            public Sprite thumbnail;
            public Bounds visualEnvelope;
            public Mount[] mounts = Array.Empty<Mount>();
            public Mount[] wheelMounts = Array.Empty<Mount>();
            public string[] wheelRendererNames = Array.Empty<string>();
            [Tooltip("Render-only geometry with its nose facing local +Z. Existing sport colliders and wheel simulation remain unchanged.")]
            public Vector3 presentationEuler;
        }
        [Serializable]
        public sealed class Cosmetic
        {
            public string id, label, slotId;
            public GameObject visualPrefab;
            public Sprite thumbnail;
            public Vector3 position, euler;
            public Vector3 scale = Vector3.one;
            public bool rotatesWithWheel;
            public string[] compatibleBodyIds = Array.Empty<string>();
            public bool CompatibleWith(string bodyId)
            {
                if (compatibleBodyIds == null || compatibleBodyIds.Length == 0) return true;
                return Array.IndexOf(compatibleBodyIds, bodyId) >= 0;
            }
        }
        public string defaultBodyId = "semi.standard";
        public Body[] bodies = Array.Empty<Body>();
        public Cosmetic[] cosmetics = Array.Empty<Cosmetic>();

        public Body FindBody(string id)
        {
            if (bodies != null) foreach (var body in bodies)
                if (body != null && string.Equals(body.id, id, StringComparison.Ordinal) && body.visualPrefab != null) return body;
            return null;
        }
        public Body ResolveBody(string requestedId) => FindBody(requestedId) ?? FindBody(defaultBodyId);
        public Cosmetic FindCosmetic(string id, string slotId, string bodyId)
        {
            if (cosmetics != null) foreach (var item in cosmetics)
                if (item != null && string.Equals(item.id, id, StringComparison.Ordinal) &&
                    string.Equals(item.slotId, slotId, StringComparison.Ordinal) && item.visualPrefab != null && item.CompatibleWith(bodyId)) return item;
            return null;
        }
        public bool Validate(out string error)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (bodies == null || bodies.Length == 0) { error = "The appearance catalog needs a standard truck body."; return false; }
            foreach (var body in bodies)
            {
                if (body == null || string.IsNullOrWhiteSpace(body.id) || !ids.Add(body.id)) { error = "Every truck body needs a unique stable ID."; return false; }
                if (!RenderOnly(body.visualPrefab, out error)) { error = body.id + ": " + error; return false; }
                if (!Finite(body.visualEnvelope.center) || !Positive(body.visualEnvelope.size) || body.visualEnvelope.size.sqrMagnitude < .001f || !Finite(body.presentationEuler))
                { error = body.id + ": authored visual bounds are missing or invalid."; return false; }
                var slots = new HashSet<string>(StringComparer.Ordinal);
                foreach (var mount in body.mounts ?? Array.Empty<Mount>())
                    if (mount == null || string.IsNullOrWhiteSpace(mount.slotId) || !slots.Add(mount.slotId) || !Finite(mount.position) || !Finite(mount.euler) || !Positive(mount.scale))
                    { error = body.id + ": mounts need unique slot IDs and finite, positive poses."; return false; }
                slots.Clear();
                foreach (var mount in body.wheelMounts ?? Array.Empty<Mount>())
                    if (mount == null || string.IsNullOrWhiteSpace(mount.slotId) || !slots.Add(mount.slotId) || !Finite(mount.position) || !Finite(mount.euler) || !Positive(mount.scale))
                    { error = body.id + ": wheel mounts need unique IDs and finite, positive poses."; return false; }
            }
            if (FindBody(defaultBodyId) == null) { error = "Default body ID must identify an available catalog body."; return false; }
            ids.Clear();
            foreach (var item in cosmetics ?? Array.Empty<Cosmetic>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || string.IsNullOrWhiteSpace(item.slotId) || !ids.Add(item.id))
                { error = "Every cosmetic needs a unique stable ID and a slot."; return false; }
                if (!RenderOnly(item.visualPrefab, out error)) { error = item.id + ": " + error; return false; }
                if (!Finite(item.position) || !Finite(item.euler) || !Positive(item.scale)) { error = item.id + ": invalid cosmetic pose."; return false; }
            }
            error = ""; return true;
        }
        static bool RenderOnly(GameObject prefab, out string error)
        {
            if (prefab == null) { error = "Missing render-only prefab."; return false; }
            if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0) { error = "The prefab has no visible renderer."; return false; }
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null) { error = "The prefab contains a missing script."; return false; }
                if (component is Transform || component is MeshFilter || component is Renderer || component is LODGroup) continue;
                error = "Presentation prefab contains a gameplay component: " + component.GetType().Name + ". Use a Sports render-only variant."; return false;
            }
            error = ""; return true;
        }
        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        static bool Positive(Vector3 value) => Finite(value) && value.x > 0 && value.y > 0 && value.z > 0;
    }
}
