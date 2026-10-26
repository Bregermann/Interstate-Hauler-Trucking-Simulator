using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckSports.Editor
{
    public static class SportsTruckAppearanceDiagnostics
    {
        [MenuItem("Truck Sports/Customizer/Audit Saved Mesh Geometry")]
        public static void AuditGeneratedGeometry()
        {
            var catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            if (catalog == null) throw new InvalidOperationException("Author the saved catalog first.");
            if (!catalog.Validate(out var issue)) throw new InvalidOperationException(issue);
            var prefabs = catalog.bodies.Select(body => body.visualPrefab).Concat(catalog.cosmetics.Select(item => item.visualPrefab)).Distinct(); int count = 0;
            foreach (var prefab in prefabs)
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount == 0 || mesh.vertices.Any(p => !float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z)) || !float.IsFinite(mesh.bounds.size.sqrMagnitude) || !float.IsFinite(mesh.bounds.center.sqrMagnitude))
                    throw new InvalidOperationException("Inspect saved geometry: " + prefab.name + "/" + filter.name);
                count++;
            }
            Debug.Log("APPEARANCE_SAVED_GEOMETRY_VALID " + count + " meshes; no asset or scene was changed.");
        }
    }
}
