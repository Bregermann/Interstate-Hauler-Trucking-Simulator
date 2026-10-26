using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceContentAuthoring
    {
        static bool repairInitialLiveryWinding;
        public static void RepairInitialLiveryWinding()
        {
            repairInitialLiveryWinding = true;
            try { AddMissingAtlasLivery(); }
            finally { repairInitialLiveryWinding = false; }
        }
        // Explicit owned-art upgrade, never a load/build callback. The source
        // atlas, windows, chrome and authored vendor mesh stay byte-identical.
        [MenuItem("Truck Sports/Customizer/Add Missing Owned Atlas Livery")]
        public static void AddMissingAtlasLivery()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Author the saved body catalog first.");
            var paint = Material("Sports participant livery", new Color(.09f, .65f, .96f), .15f, .4f);
            foreach (var body in catalog.bodies.Where(item => item.id == "truck.fire" || item.id == "novelty.icecream"))
            {
                string path = AssetDatabase.GetAssetPath(body.visualPrefab);
                if (!path.StartsWith(Folder + "/Bodies/", StringComparison.Ordinal)) throw new InvalidOperationException("Preserve external designer body prefabs: " + path);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var existingPart = root.transform.Find("Owned surface-following participant livery");
                    if (existingPart != null && !repairInitialLiveryWinding) continue;
                    var envelope = BoundsOf(root); var vertices = new List<Vector3>(); var triangles = new List<int>();
                    var inv = root.transform.worldToLocalMatrix;
                    foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        string name = filter.name.ToUpperInvariant();
                        if (name.Contains("WHEEL") || name.Contains("TIRE") || name.Contains("GLASS") || name.Contains("WINDOW")) continue;
                        var mesh = filter.sharedMesh; if (mesh == null) continue;
                        var points = mesh.vertices; var indices = mesh.triangles; var matrix = inv * filter.transform.localToWorldMatrix;
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            var a = matrix.MultiplyPoint3x4(points[indices[i]]); var b = matrix.MultiplyPoint3x4(points[indices[i + 1]]); var c = matrix.MultiplyPoint3x4(points[indices[i + 2]]);
                            bool mirrored = matrix.determinant < 0;
                            var normal = Vector3.Cross(b - a, c - a).normalized * (mirrored ? -1 : 1);
                            if (Mathf.Abs(normal.x) < .65f && normal.z < .65f) continue;
                            var polygon = new List<Vector3> { a, b, c };
                            // A narrow wrap below the glazing, clipped to the
                            // actual body surface rather than floating panels.
                            polygon = Clip(polygon, 1, envelope.min.y + envelope.size.y * .31f, true);
                            polygon = Clip(polygon, 1, envelope.min.y + envelope.size.y * .45f, false);
                            polygon = Clip(polygon, 2, envelope.center.z - envelope.size.z * .28f, true);
                            if (polygon.Count < 3) continue;
                            int at = vertices.Count; foreach (var point in polygon) vertices.Add(point + normal * .009f);
                            for (int j = 1; j + 1 < polygon.Count; j++) triangles.AddRange(mirrored ? new[] { at, at + j + 1, at + j } : new[] { at, at + j, at + j + 1 });
                        }
                    }
                    if (triangles.Count < 6) throw new InvalidOperationException("No valid opaque livery surface: " + body.id);
                    string meshPath = Folder + "/Meshes/" + body.id + " owned participant livery.asset";
                    var priorMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (priorMesh != null && !repairInitialLiveryWinding) throw new InvalidOperationException("Inspect preserved unbound livery mesh before reuse: " + meshPath);
                    if (existingPart != null && existingPart.GetComponent<MeshFilter>().sharedMesh != priorMesh) throw new InvalidOperationException("Preserve designer-replaced livery geometry: " + path);
                    var livery = new Mesh { name = body.id + " surface-following livery", vertices = vertices.ToArray(), triangles = triangles.ToArray() };
                    livery.RecalculateNormals(); livery.RecalculateBounds();
                    if (priorMesh != null) { EditorUtility.CopySerialized(livery, priorMesh); UnityEngine.Object.DestroyImmediate(livery); livery = priorMesh; EditorUtility.SetDirty(livery); }
                    else AssetDatabase.CreateAsset(livery, meshPath);
                    if (existingPart == null)
                    {
                        var part = new GameObject("Owned surface-following participant livery"); part.transform.SetParent(root.transform, false);
                        part.AddComponent<MeshFilter>().sharedMesh = livery; part.AddComponent<MeshRenderer>().sharedMaterial = paint;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path); rebuiltPreviews.Add(body.id);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                body.thumbnail = Thumbnail(body.id, body.visualPrefab);
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Debug.Log("OWNED_ATLAS_LIVERY_SAVED clipped surface-following opaque paint bands on Fire/Icecream; original atlas/window/chrome/material references and physics untouched. Rebind owned compatibility paint slots explicitly before native acceptance.");
        }
        static List<Vector3> Clip(List<Vector3> input, int axis, float boundary, bool above)
        {
            var output = new List<Vector3>(); if (input.Count == 0) return output;
            var before = input[input.Count - 1]; bool prior = above ? before[axis] >= boundary : before[axis] <= boundary;
            foreach (var current in input)
            {
                bool inside = above ? current[axis] >= boundary : current[axis] <= boundary;
                if (inside != prior) output.Add(Vector3.LerpUnclamped(before, current, (boundary - before[axis]) / (current[axis] - before[axis])));
                if (inside) output.Add(current); before = current; prior = inside;
            }
            return output;
        }
    }
}
