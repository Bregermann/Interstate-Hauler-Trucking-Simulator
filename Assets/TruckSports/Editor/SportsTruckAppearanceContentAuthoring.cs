using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Explicit content authoring only. Builds and normal sessions never replace
    // designer-edited catalogs or prefabs. Copies vendor rendering, not physics.
    public static partial class SportsTruckAppearanceContentAuthoring
    {
        const string Folder = "Assets/TruckSports/Customization";
        const string CatalogPath = "Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset";
        const string Semi = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab";
        const string Monster = "Assets/NWH/Vehicle Physics 2/Vehicles/MonsterTruck/MonsterTruck.prefab";
        const string Fire = "Assets/UTS_FullPack/Models/Cars/Models/Fire_Truck.FBX";
        const string Van = "Assets/UTS_FullPack/Models/Cars/Models/MiniBus_Commercial.FBX";
        static readonly HashSet<string> rebuiltPreviews = new HashSet<string>(StringComparer.Ordinal);

        [MenuItem("Truck Sports/Customizer/Create Missing Owned Body Catalog")]
        public static void CreateMissingContent()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring appearance content.");
            RepairInvalidGeneratedCaps();
            var existing = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>(CatalogPath);
            if (existing != null)
            {
                if (!existing.Validate(out var issue)) throw new InvalidOperationException(issue);
                Debug.Log("TRUCK_CUSTOMIZER_CATALOG_PRESERVED existing designer selections were not regenerated."); return;
            }
            Directory.CreateDirectory(Folder + "/Bodies"); Directory.CreateDirectory(Folder + "/Meshes"); Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh();
            var catalog = ScriptableObject.CreateInstance<SportsTruckAppearanceCatalog>();
            catalog.bodies = new[] {
                MakeBody("semi.standard", "Highway Hauler", "SEMI TRUCKS", Semi, Vector3.one),
                MakeBody("truck.monster", "Monster Hauler", "MONSTER TRUCKS", Monster, Vector3.one),
                MakeBody("semi.lowrider", "Chrome Lowrider", "LOWRIDERS", Semi, new Vector3(1, .74f, 1)),
                MakeBody("truck.lifted", "Backroad Bruiser", "LIFTED TRUCKS", Monster, new Vector3(1, 1.23f, 1)),
                MakeBody("truck.fire", "Party Fire Engine", "FIRETRUCKS", Fire, Vector3.one),
                MakeBody("novelty.hotdog", "Haul Dog", "NOVELTY TRUCKS", Semi, Vector3.one, HotDog),
                MakeBody("novelty.icecream", "Sundae Driver", "ICE CREAM TRUCKS", Van, Vector3.one, IceCream)
            };
            catalog.cosmetics = CreateCosmetics();
            foreach (var body in catalog.bodies) body.thumbnail = Thumbnail(body.id, body.visualPrefab);
            foreach (var item in catalog.cosmetics) item.thumbnail = Thumbnail(item.id, item.visualPrefab);
            if (!catalog.Validate(out var error)) { UnityEngine.Object.DestroyImmediate(catalog); throw new InvalidOperationException(error); }
            AssetDatabase.CreateAsset(catalog, CatalogPath); AssetDatabase.SaveAssets();
            foreach (var body in catalog.bodies) Debug.Log("TRUCK_CUSTOMIZER_BODY id=" + body.id + " asset=" + AssetDatabase.GetAssetPath(body.visualPrefab) + " bounds=" + body.visualEnvelope.size);
            Debug.Log("TRUCK_CUSTOMIZER_CATALOG_CREATED seven render-only body choices and " + catalog.cosmetics.Length + " accessories; no simulation, input, camera, traffic or Taxi prefab source copied/modified. Shop/runtime application acceptance pending.");
        }

        static SportsTruckAppearanceCatalog.Body MakeBody(string id, string label, string category, string sourcePath, Vector3 shape, Action<GameObject> decorate = null)
        {
            string path = Folder + "/Bodies/" + id + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || InvalidGeneratedGeometry(prefab))
            {
                rebuiltPreviews.Add(id);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null) throw new InvalidOperationException("Owned body source required: " + sourcePath);
                var root = new GameObject(label);
                try
                {
                    var highest = new HashSet<Renderer>();
                    foreach (var group in source.GetComponentsInChildren<LODGroup>(true))
                        foreach (var renderer in group.GetLODs()[0].renderers) if (renderer != null) highest.Add(renderer);
                    var inverse = source.transform.worldToLocalMatrix;
                    foreach (var mesh in source.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var renderer = mesh.GetComponent<MeshRenderer>(); if (mesh.sharedMesh == null || renderer == null || !renderer.enabled) continue;
                        if (highest.Count > 0 && !highest.Contains(renderer)) continue;
                        string name = mesh.name.ToUpperInvariant();
                        if (highest.Count == 0 && (name.Contains("LOD1") || name.Contains("LOD2") || name.Contains("LOD3") || name.Contains("LOD_1") || name.Contains("LOD_2"))) continue;
                        if (id == "novelty.hotdog" && (name.Contains("CAB") || name.Contains("INTERIOR") || name.Contains("STEERING") || name.Contains("GLASS"))) continue;
                        var part = new GameObject(mesh.name); part.transform.SetParent(root.transform, false);
                        part.transform.localPosition = inverse.MultiplyPoint3x4(mesh.transform.position);
                        part.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * mesh.transform.rotation;
                        part.transform.localScale = mesh.transform.lossyScale;
                        part.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                        part.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                    }
                    if (root.GetComponentsInChildren<Renderer>().Length == 0) throw new InvalidOperationException("No valid highest-detail body geometry in " + sourcePath);
                    // Normalize asset units before original art is mounted. Runtime
                    // adapters fit this authored envelope to each sport's vehicle.
                    Fit(root, new Vector3(2.8f, 3.5f, 7.5f));
                    decorate?.Invoke(root);
                    foreach (Transform part in root.transform) { var p = part.localPosition; part.localPosition = Vector3.Scale(p, shape); part.localScale = Vector3.Scale(part.localScale, shape); }
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var envelope = BoundsOf(prefab);
            var wheelParts = prefab.GetComponentsInChildren<MeshRenderer>(true).Where(part => {
                string name = part.name.ToUpperInvariant(); return !name.Contains("STEER") && (name.StartsWith("WHL HD ") || name.StartsWith("TIRE") || name.StartsWith("WHEELS_"));
            }).ToArray();
            var wheelMounts = new List<SportsTruckAppearanceCatalog.Mount>();
            foreach(var part in wheelParts)
            {
                if(wheelMounts.Any(mount=>Vector3.Distance(mount.position,part.bounds.center)<.15f))continue;
                var size=part.bounds.size;float diameter=Mathf.Max(size.x,size.y,size.z);
                wheelMounts.Add(new SportsTruckAppearanceCatalog.Mount{slotId="wheel."+wheelMounts.Count,position=part.bounds.center,euler=part.transform.localEulerAngles,scale=Vector3.one*diameter});
            }
            var front = new Vector3(envelope.center.x, envelope.min.y + envelope.size.y * .61f, envelope.max.z + .025f);
            return new SportsTruckAppearanceCatalog.Body {
                id = id, label = label, category = category, visualPrefab = prefab, visualEnvelope = envelope,
                wheelMounts=wheelMounts.ToArray(),wheelRendererNames=wheelParts.Select(part=>part.name).Concat(prefab.GetComponentsInChildren<MeshRenderer>(true).Where(part=>part.name.IndexOf("Rim",StringComparison.OrdinalIgnoreCase)>=0).Select(part=>part.name)).Distinct().ToArray(),
                mounts = new[] {
                    Mount("hat", new Vector3(envelope.center.x, envelope.max.y + .02f, envelope.center.z + envelope.size.z * .21f)),
                    Mount("face.eyes", front), Mount("face.brows", front), Mount("face.mouth", front), Mount("face.mustache", front), Mount("face.lashes", front),
                    Mount("mirrors", front), Mount("wheels", Vector3.zero),
                    Mount("rear.ornament", new Vector3(envelope.center.x, envelope.min.y + .55f, envelope.min.z + .1f)),
                    Mount("dashboard", new Vector3(envelope.center.x + .2f, envelope.min.y + envelope.size.y * .52f, envelope.center.z + envelope.size.z * .26f))
                }
            };
        }
        static SportsTruckAppearanceCatalog.Mount Mount(string id, Vector3 position) => new SportsTruckAppearanceCatalog.Mount { slotId = id, position = position };
        static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true); var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        static void Fit(GameObject root, Vector3 maximum)
        {
            var bounds = BoundsOf(root); float scale = Mathf.Min(maximum.x / bounds.size.x, maximum.y / bounds.size.y, maximum.z / bounds.size.z);
            foreach (Transform part in root.transform) { part.localPosition = (part.localPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)) * scale; part.localScale *= scale; }
        }
        static Material Material(string name, Color color, float metallic = 0, float smoothness = .4f)
        {
            string path = Folder + "/Materials/" + name + ".mat"; var value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value != null) return value;
            value = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color }; value.SetFloat("_Metallic", metallic); value.SetFloat("_Smoothness", smoothness); AssetDatabase.CreateAsset(value, path); return value;
        }
        static Sprite Thumbnail(string id, GameObject prefab)
        {
            string folder = Folder + "/Thumbnails"; Directory.CreateDirectory(folder); string path = folder + "/" + id + ".png";
            if (!File.Exists(path) || rebuiltPreviews.Contains(id) || EmptyGeneratedThumbnail(path))
            {
                var preview = new PreviewRenderUtility(); var model = UnityEngine.Object.Instantiate(prefab); Texture2D image = null;
                try
                {
                    preview.AddSingleGO(model); var bounds = BoundsOf(model); var camera = preview.camera;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .04f, .07f, 0);
                    camera.orthographic = true; camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.z) * 1.18f;
                    camera.nearClipPlane = .01f; camera.farClipPlane = 150;
                    camera.transform.position = bounds.center + new Vector3(1.1f, .7f, 1.3f).normalized * Mathf.Max(4, bounds.size.magnitude * 2);
                    camera.transform.LookAt(bounds.center);
                    // Fit the projected box, including very wide disconnected
                    // accessories such as paired mirrors, rather than assuming
                    // the model's longest axis is its forward extent.
                    float halfWidth = 0, halfHeight = 0; var inverseView = Quaternion.Inverse(camera.transform.rotation);
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        var local = inverseView * (corner - bounds.center); halfWidth = Mathf.Max(halfWidth, Mathf.Abs(local.x)); halfHeight = Mathf.Max(halfHeight, Mathf.Abs(local.y));
                    }
                    camera.orthographicSize = Mathf.Max(.05f, Mathf.Max(halfHeight, halfWidth / 1.5f) * 1.18f);
                    preview.lights[0].intensity = 1.5f; preview.lights[0].transform.rotation = Quaternion.Euler(40, 25, 0); preview.lights[1].intensity = 1;
                    preview.BeginStaticPreview(new Rect(0, 0, 384, 256)); preview.Render(true); image = preview.EndStaticPreview();
                    if (!VisibleThumbnail(image)) throw new InvalidOperationException("Empty generated preview for " + id + "; inspect the actual mesh bounds/materials before saving the catalog.");
                    File.WriteAllBytes(path, image.EncodeToPNG());
                }
                finally { preview.Cleanup(); if (model != null) UnityEngine.Object.DestroyImmediate(model); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 512; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Surface(GameObject root, string name, Vector3 center, Vector3 radii, Material material, int rings = 36, int sides = 24)
        {
            string path = Folder + "/Meshes/" + root.name + " " + name + ".asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create || InvalidMesh(mesh))
            {
                var points = new Vector3[(rings + 1) * (sides + 1)]; var uv = new Vector2[points.Length]; var triangles = new List<int>(rings * sides * 6);
                for (int z = 0; z <= rings; z++) for (int s = 0; s <= sides; s++)
                {
                    float t = z / (float)rings, angle = s * Mathf.PI * 2 / sides;
                    float round = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .38f);
                    int i = z * (sides + 1) + s; points[i] = new Vector3(Mathf.Cos(angle) * radii.x * round, Mathf.Sin(angle) * radii.y * round, (t - .5f) * radii.z * 2); uv[i] = new Vector2(s / (float)sides, t);
                    if (z < rings && s < sides) { int a = i, b = i + sides + 1; triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
                }
                if (create) mesh = new Mesh(); else mesh.Clear();
                mesh.name = name; mesh.vertices = points; mesh.uv = uv; mesh.triangles = triangles.ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                if (create) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            }
            var part = new GameObject(name); part.transform.SetParent(root.transform, false); part.transform.localPosition = center; part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        static bool InvalidMesh(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount == 0) return true;
            var bounds = mesh.bounds;
            return !float.IsFinite(bounds.center.sqrMagnitude) || !float.IsFinite(bounds.size.sqrMagnitude) || bounds.size.sqrMagnitude < .0000001f;
        }
        static bool InvalidGeneratedGeometry(GameObject prefab) => prefab.GetComponentsInChildren<MeshFilter>(true).Any(filter => InvalidMesh(filter.sharedMesh));
        static void RepairInvalidGeneratedCaps()
        {
            if (!AssetDatabase.IsValidFolder(Folder + "/Meshes")) return;
            int repaired = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { Folder + "/Meshes" }))
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid)); var points = mesh.vertices; bool changed = false;
                for (int i = 0; i < points.Length; i++)
                {
                    var p = points[i]; if (float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z)) continue;
                    // The old generated cap alone evaluated Pow(sin(pi),.38)
                    // with a tiny negative roundoff. Its authored z is intact;
                    // the intended terminal ring is exactly x=y=0.
                    if (!float.IsFinite(p.z) || float.IsFinite(p.x) || float.IsFinite(p.y))
                        throw new InvalidOperationException("Inspect unexpected invalid authored mesh data: " + mesh.name);
                    points[i] = new Vector3(0, 0, p.z); changed = true;
                }
                var bounds = mesh.bounds;
                bool finiteBounds = float.IsFinite(bounds.center.x) && float.IsFinite(bounds.center.y) && float.IsFinite(bounds.center.z) && float.IsFinite(bounds.size.x) && float.IsFinite(bounds.size.y) && float.IsFinite(bounds.size.z);
                if (!changed && finiteBounds) continue;
                mesh.vertices = points; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh); repaired++;
            }
            if (repaired > 0) { AssetDatabase.SaveAssets(); Debug.Log("TRUCK_CUSTOMIZER_INVALID_GENERATED_CAPS_REPAIRED " + repaired + "; only nonfinite cap coordinates changed, asset GUIDs retained."); }
        }
        static bool EmptyGeneratedThumbnail(string path)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try { return image.LoadImage(File.ReadAllBytes(path)) && !VisibleThumbnail(image); }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }
        static bool VisibleThumbnail(Texture2D image)
        {
            var pixels = image.GetPixels32(); if (pixels.Length == 0) return false;
            var first = pixels[0]; int visible = 0;
            foreach (var pixel in pixels) if (Math.Abs(pixel.r - first.r) + Math.Abs(pixel.g - first.g) + Math.Abs(pixel.b - first.b) > 20 && ++visible > 32) return true;
            return false;
        }
        static void HotDog(GameObject root)
        {
            Surface(root, "Haul Dog toasted left bun", new Vector3(-.65f, 1.45f, 0), new Vector3(.58f, .64f, 3.45f), Material("Toasted bun", new Color(.78f, .43f, .13f), 0, .22f));
            Surface(root, "Haul Dog toasted right bun", new Vector3(.65f, 1.45f, 0), new Vector3(.58f, .64f, 3.45f), Material("Toasted bun", new Color(.78f, .43f, .13f), 0, .22f));
            Surface(root, "Haul Dog sausage body", new Vector3(0, 1.78f, 0), new Vector3(.66f, .65f, 3.65f), Material("Smoked sausage red", new Color(.55f, .11f, .065f), 0, .48f));
            // A raised, individually shaped mustard zigzag is original mesh art.
            for (int i = 0; i < 17; i++)
            {
                float z = -2.8f + i * .35f; Surface(root, "Haul Dog mustard " + i, new Vector3(Mathf.Sin(i * 1.1f) * .28f, 2.41f - Mathf.Abs(z) * .025f, z), new Vector3(.09f, .07f, .25f), Material("Golden mustard", new Color(1, .74f, .04f), 0, .5f), 8, 10);
            }
        }
        static void IceCream(GameObject root)
        {
            var bounds = BoundsOf(root); float top = bounds.max.y;
            Surface(root, "Sundae Driver vanilla scoop", new Vector3(0, top + .48f, .1f), new Vector3(.62f, .65f, .65f), Material("Vanilla cream", new Color(1, .86f, .58f), 0, .2f));
            Surface(root, "Sundae Driver strawberry scoop", new Vector3(0, top + .94f, .1f), new Vector3(.47f, .49f, .49f), Material("Strawberry swirl", new Color(1, .28f, .48f), 0, .25f));
            Surface(root, "Sundae Driver cherry", new Vector3(.08f, top + 1.38f, .1f), new Vector3(.18f, .18f, .18f), Material("Cherry lacquer", new Color(.65f, .015f, .035f), 0, .72f), 16, 16);
        }
    }
}
