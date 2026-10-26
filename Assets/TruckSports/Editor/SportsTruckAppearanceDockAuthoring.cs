using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckDockDiving;
using LWS.TruckDockDiving.Editor;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        public const string DockRecipeFolder = "Assets/TruckDockDiving/Customization/PrivateLicensedSplitBodies";
        public const string DockPlanePath = DockRecipeFolder + "/Dock split planes.asset";
        static DockMatch OpenDock()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || SceneManager.GetActiveScene().path != "Assets/TruckDockDiving/Scenes/TruckDockDiving.unity") throw new InvalidOperationException("Open the original stopped compiled Dock scene explicitly; caller owns scene save.");
            return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DockMatch>(true)).Single();
        }
        [MenuItem("Truck Sports/Customizer/Dock/Bind Open Dock Owners And Split Rendering")]
        public static void InstallOpenDockAppearance()
        {
            var match = OpenDock(); var scene = SceneManager.GetActiveScene();
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            if (catalog == null || !catalog.Validate(out _) || original == null || match.staged.Length != 8 || match.split == null || match.replay == null || match.replay.ghostSplit == null) throw new InvalidOperationException("Original nine Dock actors, split and recorder required.");
            var recipes = catalog.bodies.Select(body => AssetDatabase.LoadAssetAtPath<DockAppearanceSplitRecipe>(DockRecipeFolder + "/" + body.id + ".asset")).ToArray();
            for (int i = 0; i < recipes.Length; i++) if (recipes[i] == null || !recipes[i].ValidFor(catalog.bodies[i]) || recipes[i].sourceDependencyHash != AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(catalog.bodies[i].visualPrefab)).ToString()) throw new InvalidOperationException("Current persistent private Y recipes required for every selected body.");
            var trucks = new[] { match.truck }.Concat(match.staged).ToArray(); var rolling = new Renderer[trucks.Length][];
            var expected = original.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.GetComponent<MeshRenderer>() != null).ToArray();
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i]; CheckVehicle(truck.transform, truck.visuals, truck.wheels); rolling[i] = BaseOriginalRollingRenderers(truck);
                var meshes = truck.visuals.GetComponentsInChildren<MeshFilter>(true);
                if (truck.session != match || meshes.Length != 33 || meshes.Any(mesh => mesh.GetComponent<MeshRenderer>() == null || expected.Count(source => source.name == mesh.name && source.sharedMesh == mesh.sharedMesh) != 1)) throw new InvalidOperationException("Exact original33 NWH mesh identities required for each Dock actor.");
            }
            if (trucks.Any(truck => truck.appearanceHost != null || truck.GetComponent<SportsTruckAppearanceHost>() != null))
            {
                if (match.appearanceSplit != null && trucks.All(truck => truck.appearanceHost != null && match.appearanceHosts.Contains(truck.appearanceHost))) return;
                throw new InvalidOperationException("Existing partial/custom Dock bindings preserved for explicit review.");
            }
            var hosts = match.appearanceHosts.ToList();
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i]; var host = Create(truck.transform, truck.visuals, truck.wheels, rolling[i], catalog); host.participantSlot = i == 0 ? -1 : i - 1;
                host.modeOwnedSlots = new[] { SportsTruckAppearanceSlots.Dashboard }; host.protectedRoots = new[] { match.dock.transform, match.waterVolume.transform, match.bestMarker, match.replay.ghost };
                foreach (var fit in host.compatibleBodies) fit.notice += " Dock keeps its original physical envelope and upper/lower visual separation. No dashboard view is authored here.";
                truck.appearanceHost = host; hosts.Add(host); EditorUtility.SetDirty(host); EditorUtility.SetDirty(truck);
            }
            var presenter = match.truck.gameObject.AddComponent<DockSelectedSplitPresentation>(); presenter.match = match; presenter.host = match.truck.appearanceHost; presenter.recipes = recipes; match.appearanceSplit = presenter;
            match.appearanceHosts = hosts.ToArray(); match.appearanceFraming = match.appearanceFraming.Concat(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(frame => frame.session == match)).Distinct().ToArray();
            EditorUtility.SetDirty(presenter); EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("DOCK_APPEARANCE_BOUND eight fixed staged owners and one actual turn owner; original single body/1000-frame recorder retained. Caller saves; native proof pending.");
        }
        [MenuItem("Truck Sports/Customizer/Dock/Create Missing Plane Settings")]
        public static void CreateDockSplitPlaneSettings()
        {
            var match = OpenDock(); if (AssetDatabase.LoadAssetAtPath<DockAppearanceSplitSettings>(DockPlanePath) != null) return;
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            if (catalog == null || !catalog.Validate(out _)) throw new InvalidOperationException("Validated existing catalog required.");
            var truck = match.truck; CheckVehicle(truck.transform, truck.visuals, truck.wheels); var rolling = BaseOriginalRollingRenderers(truck); var bounds = LocalBounds(truck.transform, truck.visuals.GetComponentsInChildren<Renderer>(true));
            Directory.CreateDirectory(DockRecipeFolder); AssetDatabase.Refresh();
            var settings = ScriptableObject.CreateInstance<DockAppearanceSplitSettings>(); settings.legacyVisualYScale = truck.visuals.localScale.y;
            settings.planes = catalog.bodies.Select(body => { var fit = Fit(body, bounds, truck.transform, truck.wheels); return new DockAppearanceSplitSettings.Plane { bodyId = body.id, vehiclePlaneY = .65f, canonicalToVehicle = Matrix4x4.TRS(fit.position, Quaternion.Euler(fit.euler), fit.scale) * Matrix4x4.Rotate(Quaternion.Euler(body.presentationEuler)) }; }).ToArray();
            AssetDatabase.CreateAsset(settings, DockPlanePath); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets(); Selection.activeObject = settings;
        }
        [MenuItem("Truck Sports/Customizer/Dock/Create Missing Private Y Split Recipes")]
        public static void CreateMissingDockSplitRecipes()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var settings = AssetDatabase.LoadAssetAtPath<DockAppearanceSplitSettings>(DockPlanePath); var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var cap = AssetDatabase.LoadAssetAtPath<Material>(SportsTruckFragmentRecipeAuthoring.Folder + "/Owned cut metal.mat");
            if (settings == null || catalog == null || !catalog.Validate(out _) || cap == null) throw new InvalidOperationException("Create/inspect explicit Dock plane settings and validate existing private cap/catalog first.");
            foreach (var body in catalog.bodies)
            {
                var plane = settings.planes.Single(item => item.bodyId == body.id); string path = DockRecipeFolder + "/" + body.id + ".asset"; string hash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(body.visualPrefab)).ToString();
                var existing = AssetDatabase.LoadAssetAtPath<DockAppearanceSplitRecipe>(path);
                if (existing != null) { if (existing.sourceDependencyHash != hash || existing.vehiclePlaneY != plane.vehiclePlaneY || existing.canonicalToVehicle != plane.canonicalToVehicle || !existing.ValidFor(body)) throw new InvalidOperationException("Existing Dock recipe changed/outdated; preserve it for explicit artist revision: " + body.id); continue; }
                var recipe = ScriptableObject.CreateInstance<DockAppearanceSplitRecipe>(); var parts = new List<DockAppearanceSplitRecipe.Part>(); var meshes = new List<Mesh>();
                recipe.bodyId = body.id; recipe.sourcePrefab = body.visualPrefab; recipe.sourceDependencyHash = hash; recipe.vehiclePlaneY = plane.vehiclePlaneY; recipe.canonicalToVehicle = plane.canonicalToVehicle; recipe.capMaterial = cap;
                bool persistent = false;
                try
                {
                    var renderers = body.visualPrefab.GetComponentsInChildren<Renderer>(true); var tires = new HashSet<string>(body.wheelRendererNames ?? Array.Empty<string>());
                    for (int index = 0; index < renderers.Length; index++)
                    {
                        var renderer = renderers[index]; if (!renderer.enabled || tires.Contains(renderer.name)) continue; var filter = renderer.GetComponent<MeshFilter>();
                        if (!(renderer is MeshRenderer) || filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable || filter.sharedMesh.subMeshCount != renderer.sharedMaterials.Length) throw new InvalidOperationException("Readable original material/mesh surfaces required: " + body.id + "/" + renderer.name);
                        var source = filter.sharedMesh; Matrix4x4 toBody = body.visualPrefab.transform.worldToLocalMatrix * renderer.localToWorldMatrix;
                        var canonicalPoint = plane.canonicalToVehicle.inverse.MultiplyPoint3x4(new Vector3(0, plane.vehiclePlaneY, 0)); var canonicalNormal = plane.canonicalToVehicle.transpose.MultiplyVector(Vector3.up).normalized;
                        for (int material = 0; material < source.subMeshCount; material++) for (int half = 0; half < 2; half++)
                        {
                            var slice = SliceDockSurfaceHalf(source, material, toBody, canonicalPoint, half == 1 ? canonicalNormal : -canonicalNormal, half == 1);
                            if (slice == null) continue; slice.name = body.id + " renderer " + index + " material " + material + (half == 1 ? " upper" : " lower"); meshes.Add(slice); parts.Add(new DockAppearanceSplitRecipe.Part { rendererIndex = index, materialIndex = material, sourceMaterialCount = renderer.sharedMaterials.Length, sourceMesh = source, mesh = slice, upper = half == 1 });
                        }
                    }
                    recipe.parts = parts.ToArray(); if (!recipe.ValidFor(body)) throw new InvalidOperationException("Dock split needs two nonempty validated body halves: " + body.id);
                    // Persist to a private staging asset first. An interrupted
                    // author never publishes a final recipe with null subassets.
                    string staging = DockRecipeFolder + "/" + body.id + ".staging-" + Guid.NewGuid().ToString("N") + ".asset"; AssetDatabase.CreateAsset(recipe, staging); persistent = true;
                    foreach (var mesh in meshes) { AssetDatabase.AddObjectToAsset(mesh, recipe); EditorUtility.SetDirty(mesh); }
                    EditorUtility.SetDirty(recipe); AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(staging, ImportAssetOptions.ForceUpdate);
                    if (!recipe.ValidFor(body) || meshes.Any(mesh => !AssetDatabase.Contains(mesh))) throw new InvalidOperationException("Persistent Dock slice subassets did not validate; staging retained for inspection.");
                    string error = AssetDatabase.MoveAsset(staging, path); if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error); AssetDatabase.SaveAssets();
                    Debug.Log("DOCK_PRIVATE_Y_RECIPE " + body.id + " planeVehicleY=" + plane.vehiclePlaneY + " parts=" + parts.Count + "; original materials and actual extra channels retained; explicit preview/native acceptance pending.");
                }
                catch { if (!persistent) { foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(recipe); } throw; }
            }
        }
        public static Mesh SliceDockSurface(Mesh source, int material, Matrix4x4 toBody, Vector3 point, Vector3 normal)
        {
            return SliceDockSurfaceHalf(source, material, toBody, point, normal, true);
        }
        public static Mesh SliceDockSurfaceHalf(Mesh source, int material, Matrix4x4 toBody, Vector3 point, Vector3 normal, bool originalUpper)
        {
            if (source == null || !source.isReadable) throw new InvalidOperationException("Readable source required.");
            if (source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0) && source.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord0) != 2) throw new InvalidOperationException("Vendor UV0 interpolation requires two-component source UV0.");
            Mesh working = DockSplitSurfaceAttributes.OwnMaterialSurface(source, material);
            try
            {
                DockSplitCoplanarOwnership.FilterOtherHalf(working, toBody, point, normal, originalUpper);
                working = SportsTruckFragmentRecipeAuthoring.ClipOwnedWorkingMesh(working, toBody, point, normal, 1, "Dock " + source.name + " material=" + material);
                if (working == null) return null;
                if (working.vertexCount < 3 || working.subMeshCount == 0 || working.GetTriangles(0).Length == 0) return null;
                // Uncut clones keep all channels exactly; only new vendor cut
                // topology needs additional source-surface interpolation.
                bool missing = source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color) && !working.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color);
                for (int channel = 1; channel < 8; channel++) { var attribute = (UnityEngine.Rendering.VertexAttribute)((int)UnityEngine.Rendering.VertexAttribute.TexCoord0 + channel); missing |= source.HasVertexAttribute(attribute) && !working.HasVertexAttribute(attribute); }
                if (missing) DockSplitSurfaceAttributes.Restore(working, source, material, toBody);
                var result = working; working = null; return result;
            }
            finally { if (working != null) UnityEngine.Object.DestroyImmediate(working); }
        }
    }
}
