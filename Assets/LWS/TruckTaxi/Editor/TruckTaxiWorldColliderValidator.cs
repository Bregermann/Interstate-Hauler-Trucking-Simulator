using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Editor
{
    [Serializable]
    public sealed class TruckTaxiWorldAuditReport
    {
        public string scope;
        public string limitation = "Candidates, not a confirmed flicker cause. Bounds do not prove coplanar triangles. No changes or saves performed. Physical contact and runtime streaming require Unity acceptance.";
        public int renderersReviewed, surfacesCompared, solidsReviewed, parkedVehiclesReviewed;
        public List<string> scenes = new List<string>();
        public List<string> findings = new List<string>();
        public string ToJson() => JsonUtility.ToJson(this, true);
        public override string ToString() => $"# World geometry and collision audit\n\n{scope}\n\n{limitation}\n\n" +
            $"Renderers {renderersReviewed}; large surfaces {surfacesCompared}; solids {solidsReviewed}; parked vehicles {parkedVehiclesReviewed}.\n\n" +
            string.Join("\n", scenes.Select(s => "SCENE " + s)) + "\n\n" + string.Join("\n\n", findings);
    }

    public static class TruckTaxiWorldColliderValidator
    {
        private sealed class Surface
        {
            public string identity, mesh, materials, kind;
            public Bounds bounds;
            public Matrix4x4 matrix;
        }

        public static string[] AuthoredScenePaths() => new[] { TruckTaxiDemoBuilder.ScenePath }.Concat(
            AssetDatabase.FindAssets("t:Scene", new[] { "Assets/LWS/TruckTaxi/Regional/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToArray();

        public static Collider[] CanonicalPlayerBodyColliders()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab");
            return prefab == null ? Array.Empty<Collider>() : prefab.GetComponentsInChildren<Collider>(true)
                .Where(c => c.enabled && !c.isTrigger && !(c is WheelCollider) && AuthoredActive(c.transform)).ToArray();
        }

        private static bool AuthoredActive(Transform transform)
        {
            for (var current = transform; current != null; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }

        [MenuItem("Truck Taxi/Validation/Audit Authored Geometry And Parked Trucks")]
        public static void WriteAuthoredAudit() => WriteReport(AuditAuthoredWorld(), "Logs/TruckTaxiAuthoredWorldAudit");

        [MenuItem("Truck Taxi/Validation/Audit Loaded Geometry And Scene Residency")]
        public static void WriteLoadedAudit() => WriteReport(AuditLoadedWorld(), "Logs/TruckTaxiLoadedWorldAudit");

        public static void WriteReport(TruckTaxiWorldAuditReport report, string pathWithoutExtension)
        {
            var directory = Path.GetDirectoryName(pathWithoutExtension);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(pathWithoutExtension + ".json", report.ToJson());
            File.WriteAllText(pathWithoutExtension + ".md", report.ToString());
            Debug.Log($"World audit: {report.findings.Count} review findings. {pathWithoutExtension}.json/.md. No scene or prefab saved.");
        }

        public static TruckTaxiWorldAuditReport AuditAuthoredWorld()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Source audit requires Edit Mode.");
            var report = new TruckTaxiWorldAuditReport { scope = "Serialized source scenes in isolated previews, including persistent DemoCity. Unsaved loaded edits are not included. Cross-region candidates do not imply simultaneous loading." };
            var surfaces = new List<Surface>();
            var player = CanonicalPlayerBodyColliders();
            report.findings.Add("PLAYER REFERENCE: canonical NWH prefab body layers only; instance IgnoreCollision state must be checked with the live player.");
            foreach (var path in AuthoredScenePaths())
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                { report.findings.Add("MISSING SCENE: " + path); continue; }
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try { CaptureScene(scene, path, player, report, surfaces); }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            CompareSurfaces(surfaces, report);
            return report;
        }

        public static TruckTaxiWorldAuditReport AuditLoadedWorld(IReadOnlyList<Collider> playerSolids = null)
        {
            var report = new TruckTaxiWorldAuditReport { scope = "Loaded object observation including DontDestroyOnLoad renderers. Nothing repaired." };
            var surfaces = new List<Surface>();
            var scenes = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene) && scenes.Add(scene.handle))
                    CaptureScene(scene, scene.path, playerSolids, report, surfaces);
            }
            foreach (var renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                var scene = renderer.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded || EditorUtility.IsPersistent(renderer) || EditorSceneManager.IsPreviewScene(scene)) continue;
                if (scenes.Add(scene.handle)) CaptureScene(scene, scene.path, playerSolids, report, surfaces);
            }
            report.findings.Add(TruckTaxiColliderDiagnostics.WorldSceneSnapshot());
            AppendVendorRegistry(report);
            CompareSurfaces(surfaces, report);
            return report;
        }

        private static void AppendVendorRegistry(TruckTaxiWorldAuditReport report)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < SceneManager.sceneCount; i++) names.Add(SceneManager.GetSceneAt(i).name);
            foreach (var world in UnityEngine.Object.FindObjectsByType<TruckTaxiRegionalWorld>(FindObjectsSortMode.None))
                foreach (var region in world.regions ?? Array.Empty<TruckTaxiRegion>())
                    if (region != null && !string.IsNullOrEmpty(region.sceneName)) names.Add(region.sceneName);
            // Use an already-existing instance's public query. Static IsSceneLoaded creates a singleton.
            foreach (var vendor in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (vendor == null || vendor.GetType().FullName != "PixelCrushers.SceneStreamer.SceneStreamer" ||
                    EditorUtility.IsPersistent(vendor) || !vendor.gameObject.scene.IsValid() ||
                    EditorSceneManager.IsPreviewScene(vendor.gameObject.scene)) continue;
                var method = vendor.GetType().GetMethod("IsLoaded", new[] { typeof(string) });
                if (method == null) { report.findings.Add("VENDOR REGISTRY: public IsLoaded query unavailable."); continue; }
                foreach (string name in names.OrderBy(n => n, StringComparer.Ordinal))
                {
                    bool actual = SceneManager.GetSceneByName(name).isLoaded;
                    try
                    {
                        bool registered = (bool)method.Invoke(vendor, new object[] { name });
                        report.findings.Add($"VENDOR REGISTRY: {name}; registered={registered}; SceneManager={actual}" +
                            (registered != actual ? " MISMATCH (not proof of duplicate geometry)" : ""));
                    }
                    catch (Exception error) { report.findings.Add("VENDOR REGISTRY query failed: " + error.GetType().Name); }
                }
            }
        }

        private static void CaptureScene(Scene scene, string path, IReadOnlyList<Collider> player,
            TruckTaxiWorldAuditReport report, List<Surface> surfaces)
        {
            report.scenes.Add(path + " [handle " + scene.handle + "]");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(false))
                {
                    if (!TruckTaxiWorldColliderReview.IsVisible(renderer)) continue;
                    report.renderersReviewed++;
                    var bounds = TruckTaxiWorldGeometryAuthoring.WorldMeshBounds(renderer);
                    if (bounds.size.x * bounds.size.z < 25 || bounds.size.x < 2 || bounds.size.z < 2) continue;
                    var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    string key = mesh == null ? "<no static mesh>" : AssetKey(mesh);
                    surfaces.Add(new Surface { identity = Identity(renderer.transform, path), mesh = key,
                        materials = string.Join(", ", renderer.sharedMaterials.Select(AssetKey)),
                        bounds = bounds, matrix = renderer.localToWorldMatrix,
                        kind = renderer.GetType().Name + "; shadow=" + renderer.shadowCastingMode + "; layer=" + renderer.gameObject.layer });
                }
                foreach (var terrain in root.GetComponentsInChildren<Terrain>(false))
                {
                    if (!terrain.enabled || terrain.terrainData == null) continue;
                    surfaces.Add(new Surface { identity = Identity(terrain.transform, path), mesh = AssetKey(terrain.terrainData),
                        materials = AssetKey(terrain.materialTemplate), matrix = terrain.transform.localToWorldMatrix, kind = "Terrain (bounds-only)",
                        bounds = TruckTaxiWorldColliderReview.TransformBounds(terrain.terrainData.bounds, terrain.transform.localToWorldMatrix) });
                }
                foreach (var collider in root.GetComponentsInChildren<Collider>(false))
                {
                    if (!TruckTaxiWorldColliderReview.IsSolid(collider)) continue;
                    report.solidsReviewed++;
                    string suspicion = TruckTaxiWorldColliderReview.Suspicion(collider);
                    if (suspicion != null) report.findings.Add("SOLID REVIEW: " + Identity(collider.transform, path) + ": " + suspicion);
                }
                foreach (var depot in root.GetComponentsInChildren<TruckTaxiRivalDepot>(true))
                    foreach (Transform child in depot.transform)
                    {
                        if (!child.name.StartsWith("Parked", StringComparison.OrdinalIgnoreCase) && child.GetComponent<Rigidbody>() == null) continue;
                        if (!child.GetComponentsInChildren<Renderer>(false).Any(TruckTaxiWorldColliderReview.IsVisible)) continue;
                        report.parkedVehiclesReviewed++;
                        foreach (string problem in TruckTaxiWorldColliderReview.VehicleProblems(child, player))
                            report.findings.Add("PARKED VEHICLE: " + Identity(child, path) + ": " + problem);
                    }
                foreach (var regional in root.GetComponentsInChildren<TruckTaxiRegionalWorld>(true))
                {
                    foreach (var group in (regional.regions ?? Array.Empty<TruckTaxiRegion>()).Where(r => r != null).GroupBy(r => r.sceneName))
                        if (group.Count() > 1) report.findings.Add("DUPLICATE REGION SCENE REGISTRATION: " + group.Key);
                    if (regional.streamingManifest == null) report.findings.Add("STREAMING: missing manifest on " + Identity(regional.transform, path));
                    else
                    {
                        var validation = regional.streamingManifest.ValidateManifest();
                        report.findings.Add("STREAMING MANIFEST: " + AssetKey(regional.streamingManifest) + "; " + validation.Summary);
                        if (regional.streamingManifest.chunks != null)
                            foreach (var group in regional.streamingManifest.chunks.Where(c => c != null).GroupBy(c => c.sceneName))
                                if (group.Count() > 1) report.findings.Add("DUPLICATE MANIFEST SCENE: " + group.Key);
                    }
                }
            }
        }

        private static void CompareSurfaces(List<Surface> surfaces, TruckTaxiWorldAuditReport report)
        {
            report.surfacesCompared = surfaces.Count;
            surfaces.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
            for (int i = 0; i < surfaces.Count; i++)
                for (int j = i + 1; j < surfaces.Count && surfaces[j].bounds.min.x < surfaces[i].bounds.max.x; j++)
                {
                    var a = surfaces[i]; var b = surfaces[j];
                    bool sameTransform = true;
                    for (int k = 0; k < 16; k++) if (Mathf.Abs(a.matrix[k] - b.matrix[k]) > .00001f) sameTransform = false;
                    bool exact = a.mesh != "<no static mesh>" && a.mesh == b.mesh && a.materials == b.materials && sameTransform;
                    bool top = TruckTaxiWorldGeometryAuthoring.CoplanarTopOverlap(a.bounds, b.bounds, out float area);
                    // Tall objects with merely equal AABB tops are not useful coplanar candidates.
                    top &= a.bounds.size.y <= 2 && b.bounds.size.y <= 2;
                    if (!exact && !top) continue;
                    report.findings.Add((exact ? "SAME MESH/MATERIAL/TRANSFORM CANDIDATE" : "COPLANAR TOP-BOUNDS CANDIDATE") +
                        $" (overlap XZ={area:0.###}m2; confirm triangles, visibility and residency):\n" +
                        Describe(a) + "\n" + Describe(b));
                }
        }

        private static string Describe(Surface s) => $"{s.identity}; {s.kind}; mesh={s.mesh}; materials={s.materials}; bounds={s.bounds}";
        private static string AssetKey(UnityEngine.Object asset)
        {
            if (asset == null) return "<none>";
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long id);
            return AssetDatabase.GetAssetPath(asset) + " :: " + asset.name + " [" + guid + ":" + id +
                (string.IsNullOrEmpty(guid) ? ":instance=" + asset.GetInstanceID() : "") + "]";
        }
        private static string Identity(Transform value, string path) => path + " :: " + TruckTaxiWorldColliderReview.Identity(value);

        [MenuItem("Truck Taxi/Validation/Review Solid World Colliders")]
        public static void ReviewWorld()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use runtime collider gizmos during Play Mode.");
            var report=new StringBuilder("# Solid world collider review\n\nReport only. Triggers, disabled objects and legitimate visible terrain are excluded.\n\n");
            int reviewed=0, flagged=0;
            var paths=new List<string>();
            paths.AddRange(AuthoredScenePaths());
            paths.Sort(StringComparer.Ordinal);
            foreach(var path in paths)
            {
                var scene=EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    report.AppendLine("## "+Path.GetFileNameWithoutExtension(path));
                    int count=0;
                    foreach(var root in scene.GetRootGameObjects()) foreach(var collider in root.GetComponentsInChildren<Collider>(false))
                    {
                        if(collider.isTrigger || !collider.enabled) continue;
                        reviewed++;
                        string reason=TruckTaxiWorldColliderReview.Suspicion(collider);
                        if(reason==null) continue;
                        flagged++; count++;
                        report.AppendLine("- "+TruckTaxiWorldColliderReview.Hierarchy(collider.transform)+": "+reason+"; bounds "+collider.bounds);
                    }
                    if(count==0) report.AppendLine("No suspicious solids detected.");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            report.AppendLine($"\nReviewed {reviewed}; flagged {flagged}. This is not a human drive-around collision acceptance pass.");
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/TruckTaxiSolidWorldColliderReview.md",report.ToString());
            Debug.Log($"Solid world collider review: {reviewed} checked, {flagged} for manual review. Logs/TruckTaxiSolidWorldColliderReview.md. No geometry changed.");
        }
    }
}
