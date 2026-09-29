using UnityEngine;

namespace LWS.TruckTaxi
{
    // Copy geometry, never vendor scripts, joints, wheel colliders, or pool identity.
    public static class TruckTaxiKaijuVisual
    {
        public static GameObject Snapshot(Transform source, bool includeInactiveRoot = false)
        {
            if (source == null) return null;
            var root = new GameObject("Kaiju visual - " + source.name);
            root.transform.SetPositionAndRotation(source.position, source.rotation);
            var block = new MaterialPropertyBlock();
            int count = 0;
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled || (!includeInactiveRoot && !renderer.gameObject.activeInHierarchy)) continue;
                bool active = true;
                for (var parent = renderer.transform; parent != null && parent != source; parent = parent.parent)
                    if (!parent.gameObject.activeSelf) { active = false; break; }
                if (!active) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var part = new GameObject(renderer.name);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = root.transform.InverseTransformPoint(renderer.transform.position);
                part.transform.localRotation = Quaternion.Inverse(root.transform.rotation) * renderer.transform.rotation;
                part.transform.localScale = renderer.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var copy = part.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;
                renderer.GetPropertyBlock(block);
                copy.SetPropertyBlock(block);
                for (int i = 0; i < copy.sharedMaterials.Length; i++)
                {
                    renderer.GetPropertyBlock(block, i);
                    copy.SetPropertyBlock(block, i);
                }
                if (++count >= 128) break;
            }
            if (count > 0) return root;
            Dispose(root);
            return null;
        }

        public static Bounds LocalBounds(GameObject root)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool found = false;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 local = root.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!found) { bounds = new Bounds(local, Vector3.zero); found = true; }
                    else bounds.Encapsulate(local);
                }
            }
            return bounds;
        }

        public static void Throw(GameObject visual, Vector3 velocity)
        {
            if (visual == null) return;
            visual.transform.SetParent(null, true);
            var bounds = LocalBounds(visual);
            var collider = visual.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = Vector3.Max(Vector3.one * .1f, bounds.size);
            var body = visual.AddComponent<Rigidbody>();
            body.mass = 150;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = Vector3.ClampMagnitude(velocity, 35);
        }

        public static void Dispose(GameObject visual)
        {
            if (visual == null) return;
            if (Application.isPlaying) Object.Destroy(visual);
            else Object.DestroyImmediate(visual);
        }
    }
}
