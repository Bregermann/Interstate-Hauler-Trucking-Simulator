using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    // Explicit source-scene operations only. No world rebuild, asset save, or vendor mutation.
    public static partial class TruckTaxiWorldGeometryAuthoring
    {
        public const string UtsTruckPath = "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab";
        public const string CabColliderName = "LWS Parked Cab Collision";
        private const string UndoLabel = "Repair scoped Truck Taxi world geometry";

        [MenuItem("Truck Taxi/Validation/Repair Selected Parked UTS Truck")]
        public static void RepairSelectedParkedTruck()
        {
            int changes = RepairParkedUtsTruck(Selection.activeGameObject, TruckTaxiWorldColliderValidator.CanonicalPlayerBodyColliders());
            Debug.Log($"Parked truck repair: {changes} changes. Review cab/chassis gizmos and save the source scene explicitly. No asset saved.");
        }

        public static void RequireEditableSceneObject(GameObject target)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || target == null || EditorUtility.IsPersistent(target) ||
                !target.scene.IsValid() || !target.scene.isLoaded || EditorSceneManager.IsPreviewScene(target.scene))
                throw new InvalidOperationException("Repair requires a loaded source scene in Edit Mode, not a prefab asset or preview.");
            if (!string.IsNullOrEmpty(target.scene.path) && !target.scene.path.StartsWith("Assets/LWS/TruckTaxi/", StringComparison.Ordinal))
                throw new InvalidOperationException("Only project-owned Truck Taxi scenes may be repaired.");
        }

        public static bool ExactDuplicate(MeshRenderer authority, MeshRenderer obsolete)
        {
            if (authority == null || obsolete == null || authority == obsolete) return false;
            var a = authority.GetComponent<MeshFilter>();
            var b = obsolete.GetComponent<MeshFilter>();
            if (a == null || b == null || a.sharedMesh == null || a.sharedMesh != b.sharedMesh) return false;
            if (!authority.sharedMaterials.SequenceEqual(obsolete.sharedMaterials) || authority.HasPropertyBlock() || obsolete.HasPropertyBlock()) return false;
            if (authority.gameObject.layer != obsolete.gameObject.layer || authority.shadowCastingMode != obsolete.shadowCastingMode ||
                authority.receiveShadows != obsolete.receiveShadows || authority.renderingLayerMask != obsolete.renderingLayerMask ||
                authority.lightmapIndex != obsolete.lightmapIndex || authority.lightmapScaleOffset != obsolete.lightmapScaleOffset) return false;
            var left = authority.localToWorldMatrix;
            var right = obsolete.localToWorldMatrix;
            for (int i = 0; i < 16; i++) if (Mathf.Abs(left[i] - right[i]) > .00001f) return false;
            return true;
        }

        // Caller must establish co-residency; matching geometry in mutually exclusive chunks is not redundant.
        public static int RetireExactDuplicateRenderer(MeshRenderer authority, MeshRenderer obsolete, bool coResidencyConfirmed)
        {
            if (obsolete == null) return 0;
            RequireEditableSceneObject(authority != null ? authority.gameObject : null);
            RequireEditableSceneObject(obsolete.gameObject);
            if (!coResidencyConfirmed || !TruckTaxiWorldColliderReview.IsVisible(authority) || !ExactDuplicate(authority, obsolete))
                throw new InvalidOperationException("Select an exact mesh/material/transform duplicate and confirm simultaneous residency first.");
            if (authority.GetComponentInParent<LODGroup>() != null || obsolete.GetComponentInParent<LODGroup>() != null ||
                authority.GetComponentInParent<Rigidbody>() != null || obsolete.GetComponentInParent<Rigidbody>() != null ||
                PrefabUtility.IsPartOfPrefabInstance(obsolete))
                throw new InvalidOperationException("LOD, moving, or prefab geometry requires an owner-reviewed repair.");
            var scene = obsolete.gameObject.scene;
            Undo.DestroyObjectImmediate(obsolete);
            EditorSceneManager.MarkSceneDirty(scene);
            // MeshFilter, GameObject, colliders and surface metadata remain intact.
            return 1;
        }

        public static bool CoplanarTopOverlap(Bounds a, Bounds b, out float area, float tolerance = .002f)
        {
            float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            float z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
            area = Mathf.Max(0, x) * Mathf.Max(0, z);
            return area >= 1 && Mathf.Abs(a.max.y - b.max.y) <= tolerance;
        }

        // Only the audited primitive regional ground slabs. Roads/meshes/terrain never enter this path.
        public static int TrimRegionalGroundOverlap(MeshRenderer authority, MeshRenderer trim, bool coResidencyConfirmed)
        {
            RequireEditableSceneObject(authority != null ? authority.gameObject : null);
            RequireEditableSceneObject(trim != null ? trim.gameObject : null);
            if (authority == trim || !coResidencyConfirmed) throw new InvalidOperationException("Distinct co-resident ground slabs are required.");
            var keepBox = RequireGroundSlab(authority);
            var trimBox = RequireGroundSlab(trim);
            var keepSurface = authority.GetComponentInParent<TruckTaxiSurface>();
            var trimSurface = trim.GetComponentInParent<TruckTaxiSurface>();
            if (keepBox.gameObject.layer != trimBox.gameObject.layer || keepBox.sharedMaterial != trimBox.sharedMaterial ||
                !authority.sharedMaterials.SequenceEqual(trim.sharedMaterials) ||
                (keepSurface == null) != (trimSurface == null) ||
                (keepSurface != null && keepSurface.isRoad != trimSurface.isRoad))
                throw new InvalidOperationException("Ground layers, physics/visual materials and surface semantics must match.");
            var a = WorldMeshBounds(authority);
            var b = WorldMeshBounds(trim);
            if (Mathf.Abs(a.min.y - b.min.y) > .001f || Mathf.Abs(a.max.y - b.max.y) > .001f ||
                Mathf.Abs(a.min.z - b.min.z) > .001f || Mathf.Abs(a.max.z - b.max.z) > .001f)
                throw new InvalidOperationException("Only equal-height, equal-depth slabs with an X-edge overlap are supported.");
            float overlap = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            if (overlap <= .0001f) return 0;
            float min = b.min.x, max = b.max.x;
            if (a.min.x <= min && a.max.x < max) min = a.max.x;
            else if (a.max.x >= max && a.min.x > min) max = a.min.x;
            else throw new InvalidOperationException("Containment/interior overlap needs explicit source geometry review.");
            if (max - min < 1) throw new InvalidOperationException("Repair would leave an implausibly narrow slab.");
            var transform = trim.transform;
            Undo.RecordObject(transform, UndoLabel);
            var position = transform.position;
            position.x = (min + max) * .5f;
            transform.position = position;
            var scale = transform.localScale;
            scale.x *= (max - min) / b.size.x;
            transform.localScale = scale;
            Changed(transform);
            Physics.SyncTransforms();
            return 1;
        }

        private static BoxCollider RequireGroundSlab(MeshRenderer renderer)
        {
            string name = renderer.name;
            var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            var box = renderer.GetComponent<BoxCollider>();
            if ((name != "Regional ground" && name != "West river bank" && name != "East river bank") ||
                mesh == null || mesh.name != "Cube" || AssetDatabase.GetAssetPath(mesh) != "Library/unity default resources" ||
                mesh.vertexCount != 24 || mesh.bounds.center.sqrMagnitude > .000001f ||
                (mesh.bounds.size - Vector3.one).sqrMagnitude > .000001f || box == null ||
                !TruckTaxiWorldColliderReview.IsSolid(box) || !TruckTaxiWorldColliderReview.IsVisible(renderer) ||
                box.center.sqrMagnitude > .000001f || (box.size - Vector3.one).sqrMagnitude > .000001f ||
                renderer.transform.childCount != 0 || renderer.GetComponentInParent<Rigidbody>() != null ||
                PrefabUtility.IsPartOfPrefabInstance(renderer) || box.includeLayers.value != 0 || box.excludeLayers.value != 0)
                throw new InvalidOperationException("Expected an unmodified, visible regional cube slab with its unit solid BoxCollider.");
            if (renderer.GetComponents<Component>().Any(c => c == null || !(c is Transform || c is MeshFilter ||
                c is MeshRenderer || c is BoxCollider || c is TruckTaxiSurface)))
                throw new InvalidOperationException("Ground contains additional gameplay components; ask its owner before resizing.");
            var matrix = renderer.localToWorldMatrix;
            if (matrix.m00 <= 0 || matrix.m11 <= 0 || matrix.m22 <= 0 ||
                Mathf.Abs(matrix.m01) + Mathf.Abs(matrix.m02) + Mathf.Abs(matrix.m10) + Mathf.Abs(matrix.m12) +
                Mathf.Abs(matrix.m20) + Mathf.Abs(matrix.m21) > .00001f)
                throw new InvalidOperationException("Rotated, mirrored or sheared slabs are not supported.");
            return box;
        }

        public static Bounds WorldMeshBounds(Renderer renderer) => TruckTaxiWorldColliderReview.TransformBounds(
            renderer.localBounds, renderer.localToWorldMatrix);

        public static Bounds FitUtsCab(Transform parked, MeshFilter body, BoxCollider chassis)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Imported body vertex fitting is Edit Mode only.");
            if (parked == null || body == null || body.sharedMesh == null || chassis == null)
                throw new InvalidOperationException("UTS body mesh and chassis box are required.");
            var chassisBounds = TruckTaxiWorldColliderReview.TransformBounds(new Bounds(chassis.center, chassis.size),
                parked.worldToLocalMatrix * chassis.transform.localToWorldMatrix);
            var matrix = parked.worldToLocalMatrix * body.transform.localToWorldMatrix;
            var points = new List<Vector3>();
            body.sharedMesh.GetVertices(points); // Editor-only read; importer Read/Write remains unchanged.
            var bodyBounds = TruckTaxiWorldColliderReview.TransformBounds(body.sharedMesh.bounds, matrix);
            var allowed = bodyBounds;
            allowed.Expand(.4f);
            if (!allowed.Contains(chassisBounds.min) || !allowed.Contains(chassisBounds.max))
                throw new InvalidOperationException("Existing chassis bounds exceed the visible body; inspect its overrides before enabling collision.");
            float threshold = Mathf.Max(chassisBounds.max.y + .2f, bodyBounds.min.y + bodyBounds.size.y * .35f);
            bool found = false;
            Bounds cab = default;
            foreach (var point in points)
            {
                var local = matrix.MultiplyPoint3x4(point);
                if (local.y < threshold) continue;
                if (!found) { cab = new Bounds(local, Vector3.zero); found = true; } else cab.Encapsulate(local);
            }
            var min = cab.min;
            min.y = Mathf.Max(bodyBounds.min.y, chassisBounds.max.y - .1f);
            cab.SetMinMax(min, cab.max);
            if (!found || cab.size.x < .5f || cab.size.y < .5f || cab.size.z < .5f ||
                cab.size.x > chassisBounds.size.x * 1.4f || cab.size.z > chassisBounds.size.z * .8f || cab.size.y > 6)
                throw new InvalidOperationException("Cab fit failed conservative size guards. Inspect the body mesh; no fallback whole-vehicle box is created.");
            return cab;
        }

        public static int RepairParkedUtsTruck(GameObject parked, IReadOnlyList<Collider> playerSolids, int collisionLayer = 0)
        {
            RequireEditableSceneObject(parked);
            if (!parked.activeInHierarchy || !parked.name.StartsWith("Parked UTS taxi truck ", StringComparison.Ordinal) ||
                parked.GetComponentInParent<TruckTaxiRivalDepot>() == null ||
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(parked)) != UtsTruckPath)
                throw new InvalidOperationException("Only the authored parked Truck_1 instances inside a TruckTaxiRivalDepot are supported.");
            if (collisionLayer < 0 || collisionLayer > 31 || playerSolids == null || playerSolids.Count == 0)
                throw new InvalidOperationException("Supply canonical player body colliders and a valid collision layer.");
            var chassis = parked.transform.Find("Box_Collider")?.GetComponent<BoxCollider>();
            var body = parked.transform.Find("Truck_1_LOD0")?.GetComponent<MeshFilter>();
            var rigidbody = parked.GetComponent<Rigidbody>();
            if (chassis == null || body == null || rigidbody == null || chassis.isTrigger || !chassis.gameObject.activeInHierarchy ||
                parked.GetComponentsInChildren<Rigidbody>(true).Length != 1 ||
                parked.GetComponentsInChildren<MonoBehaviour>(true).Any(b => b != null && b.enabled))
                throw new InvalidOperationException("Unexpected parked prefab structure or enabled behavior; refusing to alter a live vehicle.");
            if (chassis.includeLayers.value != 0 || chassis.excludeLayers.value != 0)
                throw new InvalidOperationException("Chassis layer overrides require manual review.");
            int checkedPlayers = 0;
            foreach (var player in playerSolids)
            {
                if (player == null || !player.enabled || player.isTrigger || player is WheelCollider) continue;
                checkedPlayers++;
                if ((player.attachedRigidbody != null && !player.attachedRigidbody.detectCollisions) ||
                    player.includeLayers.value != 0 || player.excludeLayers.value != 0 ||
                    Physics.GetIgnoreLayerCollision(collisionLayer, player.gameObject.layer) ||
                    (player.gameObject.scene.IsValid() && Physics.GetIgnoreCollision(chassis, player)))
                    throw new InvalidOperationException("Requested layer/pair cannot be verified against every player body collider. Global physics settings are never changed.");
            }
            if (checkedPlayers == 0 || Physics.GetIgnoreLayerCollision(collisionLayer, 9))
                throw new InvalidOperationException("A player body collider and collision with UTS Car layer 9 are required.");
            var fit = FitUtsCab(parked.transform, body, chassis);
            var cabTransform = parked.transform.Find(CabColliderName);
            var cab = cabTransform != null ? cabTransform.GetComponent<BoxCollider>() : null;
            if (cabTransform != null && (cab == null || cabTransform.childCount != 0 ||
                cabTransform.GetComponents<Component>().Length != 2 || cabTransform.localPosition != Vector3.zero ||
                cabTransform.localRotation != Quaternion.identity || cabTransform.localScale != Vector3.one ||
                !cabTransform.gameObject.activeSelf || cab.includeLayers.value != 0 || cab.excludeLayers.value != 0))
                throw new InvalidOperationException("Reserved cab collision child has been customized; review it manually.");
            if (cab != null && playerSolids.Any(p => p != null && p.gameObject.scene.IsValid() && Physics.GetIgnoreCollision(cab, p)))
                throw new InvalidOperationException("Cab has a runtime ignored player pair; investigate its owner.");

            int changes = 0;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                if (!rigidbody.isKinematic || rigidbody.useGravity || !rigidbody.detectCollisions)
                {
                    Undo.RecordObject(rigidbody, UndoLabel);
                    rigidbody.isKinematic = true; rigidbody.useGravity = false; rigidbody.detectCollisions = true;
                    Changed(rigidbody); changes++;
                }
                if (!chassis.enabled) { Undo.RecordObject(chassis, UndoLabel); chassis.enabled = true; Changed(chassis); changes++; }
                if (cab == null)
                {
                    var go = new GameObject(CabColliderName);
                    go.transform.SetParent(parked.transform, false);
                    Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                    cab = Undo.AddComponent<BoxCollider>(go);
                    changes++;
                }
                if (cab.center != fit.center || cab.size != fit.size || !cab.enabled || cab.isTrigger)
                {
                    Undo.RecordObject(cab, UndoLabel);
                    cab.center = fit.center; cab.size = fit.size; cab.enabled = true; cab.isTrigger = false;
                    Changed(cab); changes++;
                }
                foreach (var solid in new[] { chassis, cab })
                    if (solid.gameObject.layer != collisionLayer)
                    {
                        Undo.RecordObject(solid.gameObject, UndoLabel);
                        solid.gameObject.layer = collisionLayer;
                        Changed(solid.gameObject); changes++;
                    }
                if (changes > 0) EditorSceneManager.MarkSceneDirty(parked.scene);
                Physics.SyncTransforms();
                Undo.CollapseUndoOperations(group);
                return changes;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static void Changed(Object value)
        {
            EditorUtility.SetDirty(value);
            PrefabUtility.RecordPrefabInstancePropertyModifications(value);
            var component = value as Component;
            var go = value as GameObject;
            EditorSceneManager.MarkSceneDirty(component != null ? component.gameObject.scene : go.scene);
        }
    }
}
