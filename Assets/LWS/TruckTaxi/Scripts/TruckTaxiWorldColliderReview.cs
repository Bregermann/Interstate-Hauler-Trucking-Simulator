using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Conservative review only: a flagged collider is never automatically deleted.
    public static class TruckTaxiWorldColliderReview
    {
        public static bool IsSolid(Collider collider) => collider != null && collider.enabled &&
            collider.gameObject.activeInHierarchy && !collider.isTrigger && !(collider is WheelCollider) &&
            (collider.attachedRigidbody == null || collider.attachedRigidbody.detectCollisions);

        public static bool IsVisible(Renderer renderer) => renderer != null && renderer.enabled &&
            !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy &&
            (renderer is MeshRenderer || renderer is SkinnedMeshRenderer);

        public static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            var result = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                result.Encapsulate(matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return result;
        }

        // Bounds coverage is a review heuristic, not proof of physical contact with a mesh.
        public static List<string> VehicleProblems(Transform vehicle, IReadOnlyList<Collider> playerSolids)
        {
            var problems = new List<string>();
            if (vehicle == null) { problems.Add("Vehicle reference is missing."); return problems; }
            var solids = new List<Collider>();
            foreach (var collider in vehicle.GetComponentsInChildren<Collider>(true))
                if (IsSolid(collider)) solids.Add(collider);
            if (solids.Count == 0) problems.Add("No enabled non-trigger body collider (wheel colliders do not count).");
            int playerCount = 0;
            if (playerSolids != null) foreach (var player in playerSolids)
                if (player != null && player.enabled && !player.isTrigger && !(player is WheelCollider)) playerCount++;
            if (playerCount == 0)
                problems.Add("Player collision is UNVERIFIED: supply canonical player body colliders.");
            foreach (var solid in solids)
            {
                if (solid.includeLayers.value != 0 || solid.excludeLayers.value != 0)
                    problems.Add(Hierarchy(solid.transform) + ": per-collider layer overrides require contact verification.");
                if (playerSolids == null) continue;
                foreach (var player in playerSolids)
                {
                    if (player == null || !player.enabled || player.isTrigger || player is WheelCollider) continue;
                    if (player.attachedRigidbody != null && !player.attachedRigidbody.detectCollisions)
                        problems.Add("Player Rigidbody.detectCollisions is disabled.");
                    if (Physics.GetIgnoreLayerCollision(solid.gameObject.layer, player.gameObject.layer) ||
                        (player.gameObject.scene.IsValid() && Physics.GetIgnoreCollision(solid, player)))
                        problems.Add(Hierarchy(solid.transform) + ": collision ignored against player " + Hierarchy(player.transform));
                    if (player.includeLayers.value != 0 || player.excludeLayers.value != 0)
                        problems.Add("Player layer overrides require contact verification: " + Hierarchy(player.transform));
                }
            }
            foreach (var renderer in vehicle.GetComponentsInChildren<Renderer>(false))
            {
                if (!IsVisible(renderer)) continue;
                var visible = renderer.bounds;
                if (visible.size.x * visible.size.y * visible.size.z < 1 || visible.size.sqrMagnitude < 4) continue;
                bool found = false;
                Bounds coverage = default;
                foreach (var solid in solids)
                {
                    if (!solid.bounds.Intersects(visible)) continue;
                    if (!found) { coverage = solid.bounds; found = true; } else coverage.Encapsulate(solid.bounds);
                }
                coverage.Expand(.3f);
                if (!found || !coverage.Contains(visible.center) || coverage.max.y < visible.max.y - .4f ||
                    coverage.size.x < visible.size.x * .6f || coverage.size.z < visible.size.z * .6f)
                    problems.Add(Hierarchy(renderer.transform) + ": large visible vehicle body lacks meaningful cab/chassis coverage (bounds heuristic).");
            }
            return problems;
        }

        public static string Suspicion(Collider collider)
        {
            if(collider==null || collider.isTrigger || !collider.enabled || !collider.gameObject.activeInHierarchy) return null;
            if(collider is TerrainCollider)
            {
                var terrain=collider.GetComponent<Terrain>();
                return terrain!=null && terrain.enabled && terrain.terrainData!=null ? null : "Terrain collision without visible terrain";
            }
            Transform owner=collider.attachedRigidbody!=null ? collider.attachedRigidbody.transform : collider.transform;
            bool found=false; Bounds visible=default;
            foreach(var renderer in owner.GetComponentsInChildren<Renderer>(false))
            {
                if(!renderer.enabled || renderer.forceRenderingOff || renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
                bool transparent=true;
                foreach(var material in renderer.sharedMaterials)
                {
                    if(material==null) continue;
                    bool blends=material.HasProperty("_Surface") && material.GetFloat("_Surface")>.5f;
                    float alpha=material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor").a : material.HasProperty("_Color") ? material.color.a : 1;
                    if(!blends || alpha>.01f) { transparent=false; break; }
                }
                if(transparent) continue;
                if(!found) { visible=renderer.bounds; found=true; } else visible.Encapsulate(renderer.bounds);
            }
            if(!found) return "Solid collider has no enabled corresponding geometry";
            visible.Expand(new Vector3(.6f,.6f,.6f)+visible.size*.2f);
            var physical=collider.bounds;
            if(!visible.Contains(physical.min) || !visible.Contains(physical.max)) return "Solid bounds exceed visible geometry by more than tolerance";
            return null;
        }
        public static string Hierarchy(Transform value)
        {
            if (value == null) return "<missing>";
            string path=value.name;
            while(value.parent!=null) { value=value.parent; path=value.name+"/"+path; }
            return path;
        }

        public static string Identity(Transform value)
        {
            if (value == null) return "<missing>";
            string indices = value.GetSiblingIndex().ToString();
            for (var parent = value.parent; parent != null; parent = parent.parent)
                indices = parent.GetSiblingIndex() + "/" + indices;
            return value.gameObject.scene.path + " [scene=" + value.gameObject.scene.handle + "] :: " + Hierarchy(value) + " [" + indices + "]";
        }
    }
}
