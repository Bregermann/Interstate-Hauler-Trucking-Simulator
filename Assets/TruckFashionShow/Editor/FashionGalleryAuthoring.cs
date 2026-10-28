using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;

namespace LWS.TruckFashionShow.Editor
{
    public static class FashionGalleryAuthoring
    {
        public const string KnownSceneSHA256 = "c52b1a9e62496d933b1904feedfc08521e27e7106cb612f5a540fcc43ddacddd";
        public const string ArtName = "Fashion gallery V2 / fixed boutique presentation";
        const string Restaurant = "Assets/Modern Resaturant - building and interior/Models/Prefabs/";
        static string Digest(string file) { using (var hash = SHA256.Create()) return string.Concat(hash.ComputeHash(File.ReadAllBytes(file)).Select(x => x.ToString("x2"))); }

        [MenuItem("Truck Sports/Alpha 0.7/Repair EXACT Saved Fashion Gallery Presentation")]
        public static void RepairSavedScene()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save current scene edits before explicit Fashion presentation repair.");
            if (!File.Exists(FashionAuthoring.Scene) || Digest(FashionAuthoring.Scene) != KnownSceneSHA256)
                throw new InvalidOperationException("Different/designer Fashion scene preserved; this repair accepts only the exact reviewed native V1 scene.");
            string backup = "Logs/TruckSports/Impossible07/FashionGalleryRepair/Before-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup); File.Copy(FashionAuthoring.Scene, backup + "/TruckFashionShow.unity", false); File.Copy(FashionAuthoring.Scene + ".meta", backup + "/TruckFashionShow.unity.meta", false);
            var scene = EditorSceneManager.OpenScene(FashionAuthoring.Scene, OpenSceneMode.Single);
            var match = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FashionMatch>(true)).Single();
            var physics = PhysicalState(match); InstallNewOrExactOriginal(match);
            if (!physics.SequenceEqual(PhysicalState(match))) throw new InvalidOperationException("Original Fashion physical/equipment/strut references changed; saved scene backup retained.");
            EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Fashion presentation scene save failed.");
            AssetDatabase.SaveAssets(); Debug.Log("FASHION_GALLERY_REPAIRED fixed signs, existing retail/restaurant art, warm practical lighting, four-owner context row; original physical state exact. Backup=" + backup);
        }

        static string[] PhysicalState(FashionMatch match)
        {
            return match.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).Where(c => c != null &&
                (c is Collider || c is Rigidbody || c is Joint || c is FashionStrut || c is FashionPeople || c is SportsEventTruck || (c.GetType().Namespace ?? "").StartsWith("NWH", StringComparison.Ordinal)))
                .OrderBy(c => c.GetInstanceID()).Select(c => c.GetInstanceID() + ":" + c.GetType().FullName + ":" + EditorJsonUtility.ToJson(c)).ToArray();
        }

        public static void InstallNewOrExactOriginal(FashionMatch match)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || match == null || match.gallery != null || match.GetComponent<FashionGalleryPresentation>() != null)
                throw new InvalidOperationException("Preserve existing/custom Fashion gallery bindings; explicit untouched original or new scene required.");
            var hub = match.transform.Find("Totally Ridiculous Runway fashion hub");
            if (hub == null || match.trucks == null || match.trucks.Length != 4 || match.walkers.Length != 4 || match.shops.Length != 4 || match.catalog == null || match.menus == null || hub.Find(ArtName) != null)
                throw new InvalidOperationException("Exact original four-owner Fashion hub and menu bindings required.");
            string[] allowed = { "HAT BOUTIQUE / FREE", "FACES / EXPRESSIONS / FREE", "WHEEL ATELIER / FREE", "MIRRORS / HITCH ACCESSORIES / FREE", "DRESSING ROOM / SAVE YOUR LOOK", "TOTALLY RIDICULOUS RUNWAY", "FREE FASHION / REAL TRUCK PERSONALITY" };
            var oldLabels = hub.GetComponentsInChildren<TextMesh>(true);
            var itemLabels = match.catalog.cosmetics.Where(c => c != null).Select(c => c.label + " / FREE").ToArray();
            if (allowed.Any(s => oldLabels.Count(t => t.text == s) != 1) || oldLabels.Any(t => !allowed.Contains(t.text) && !itemLabels.Contains(t.text)) || oldLabels.Any(t => !t.gameObject.activeSelf || t.GetComponent<Collider>() != null || t.GetComponent<Rigidbody>() != null))
                throw new InvalidOperationException("Unknown/edited original Fashion signage preserved.");
            var actions = match.menus.hud.GetComponentsInChildren<SportsMenuAction>(true);
            var buttons = Enumerable.Range(0, 4).Select(i => actions.Single(a => a.action == "fashion.ready." + i).button).ToArray();
            var floor = hub.GetComponentsInChildren<BoxCollider>(true).Single(c => c.name == "Indoor fashion hub actual floor");
            if (!floor.size.Equals(Vector3.one) || !floor.transform.localScale.Equals(new Vector3(124, .4f, 112))) throw new InvalidOperationException("Edited Fashion floor preserved.");
            var originalProperties = PhysicalState(match);
            var gallery = match.gameObject.AddComponent<FashionGalleryPresentation>(); gallery.match = match; gallery.readyButtons = buttons; match.gallery = gallery;
            gallery.supersededFloatingSigns = oldLabels.Select(t => t.gameObject).ToArray(); foreach (var label in gallery.supersededFloatingSigns) label.SetActive(false);
            var art = new GameObject(ArtName).transform; art.SetParent(hub, false); gallery.galleryArt = art;
            Material wood = Material("Gallery V2 warm cedar", new Color(.25f, .13f, .075f), .32f, 0), ivory = Material("Gallery V2 warm limestone", new Color(.66f, .6f, .49f), .45f, 0),
                ink = Material("Gallery V2 deep plum signage", new Color(.055f, .018f, .04f), .28f, 0), brass = Material("Gallery V2 satin brass", new Color(.72f, .43f, .12f), .58f, .65f),
                pink = Material("Gallery V2 rose display backing", new Color(.46f, .11f, .21f), .25f, 0), mint = Material("Gallery V2 jade display backing", new Color(.055f, .27f, .25f), .3f, 0);
            floor.GetComponent<Renderer>().sharedMaterial = Material("Gallery V2 warm terrazzo floor", new Color(.23f, .21f, .19f), .38f, .06f);
            var signs = new List<TextMesh>(); var lights = new List<Light>();
            string[] names = { "HAT BOUTIQUE", "FACE STUDIO", "WHEEL ATELIER", "MIRROR SALON" };
            for (int i = 0; i < 4; i++)
            {
                float x = match.shops[i].position.x < 0 ? -47 : 47, z = match.shops[i].position.z;
                var accent = i % 2 == 0 ? mint : pink;
                // Render-only storefronts leave the actual browse gates and all
                // original displays/counters/colliders intact.
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(art, names[i] + " fluted brass pilaster", new Vector3(x + side * 10.5f, 4, z + 12), new Vector3(.45f, 8, .5f), brass);
                    Part(art, names[i] + " ivory pier", new Vector3(x + side * 10.8f, 4, z + 12.3f), new Vector3(.8f, 8.4f, .6f), ivory);
                }
                Part(art, names[i] + " finished fascia", new Vector3(x, 7.4f, z + 11.8f), new Vector3(20, 2.6f, .4f), accent);
                Part(art, names[i] + " inset fixed sign panel", new Vector3(x, 7.4f, z + 11.54f), new Vector3(18.6f, 1.85f, .1f), ink);
                signs.Add(Sign(art, names[i], new Vector3(x, 7.4f, z + 11.43f), 17.6f, 1.3f, .22f));
                for (int row = 0; row < 4; row++) for (int column = 0; column < 4; column++)
                    FloorTile(art, new Vector3(x - 6 + column * 4, .035f, z - 6 + row * 4));
                Part(art, names[i] + " warm display border", new Vector3(x, .045f, z - 9.5f), new Vector3(20, .06f, .13f), brass);
                lights.Add(Practical(art, new Vector3(x, 10.3f, z), brass, 750, false));
            }
            // Bought reception furniture gives the original dressing room a real
            // human-scale reception point, without another shop or interaction.
            RenderProp(art, Restaurant + "Restaurant_Reception_Desk.prefab", new Vector3(-37, 1.2f, 45), new Vector3(7, 2.4f, 3));
            Part(art, "Dressing room upholstered fascia", new Vector3(-46, 8.2f, 46.5f), new Vector3(25, 2.7f, .35f), pink);
            signs.Add(Sign(art, "DRESSING ROOM", new Vector3(-46, 8.2f, 46.25f), 23, 1.5f, .25f));
            Part(art, "Grand gallery back-wall collection panel", new Vector3(0, 8.5f, 51), new Vector3(45, 3.7f, .25f), ink);
            signs.Add(Sign(art, "TOTALLY RIDICULOUS RUNWAY", new Vector3(0, 8.5f, 50.8f), 42, 2.2f, .34f));
            foreach (float x in new[] { -60.3f, 60.3f }) for (int i = 0; i < 12; i++)
                Part(art, "Gallery brass wall rhythm", new Vector3(x, 4, -52 + i * 9), new Vector3(.12f, 7.7f, .16f), brass);
            foreach (float z in new[] { -30f, -5f, 20f, 43f }) lights.Add(Practical(art, new Vector3(0, 10.8f, z), brass, 900, true));
            lights.Add(Practical(art, new Vector3(-35, 10.8f, 40), brass, 650, false));
            gallery.fixedSigns = signs.ToArray(); gallery.practicalLights = lights.ToArray(); gallery.ApplyLayout();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.42f, .37f, .34f);
            RenderSettings.fog = false;
            if (!originalProperties.SequenceEqual(PhysicalState(match))) throw new InvalidOperationException("Render-only Fashion gallery must not change original physical/strut/animation state.");
            EditorUtility.SetDirty(gallery); EditorUtility.SetDirty(match);
        }

        static Material Material(string name, Color color, float smooth, float metal)
        {
            string path = "Assets/TruckFashionShow/Materials/" + name + ".mat";
            var prior = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (prior != null)
            {
                if (prior.shader == null || prior.shader.name != "Universal Render Pipeline/Lit" || !prior.GetColor("_BaseColor").Equals(color) || prior.GetFloat("_Smoothness") != smooth || prior.GetFloat("_Metallic") != metal)
                    throw new InvalidOperationException("Custom/partial gallery material preserved: " + path);
                return prior;
            }
            var result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            result.SetColor("_BaseColor", color); result.SetFloat("_Smoothness", smooth); result.SetFloat("_Metallic", metal); AssetDatabase.CreateAsset(result, path); return result;
        }
        static GameObject Part(Transform parent, string name, Vector3 at, Vector3 size, Material material)
        {
            var mesh = ImpossibleBalanceArt.Mesh("TruckFashionShow", "Gallery V2 bevelled architectural trim", ImpossibleBalanceArt.RoundedBlock);
            return ImpossibleBalanceArt.Part(parent, name, mesh, material, at, size);
        }
        static Light Practical(Transform parent, Vector3 at, Material brass, float intensity, bool shadows)
        {
            Part(parent, "Finished brass warm-light canopy", at + Vector3.up * .13f, new Vector3(2.2f, .25f, 2.2f), brass);
            var root = new GameObject("Actual warm gallery downlight"); root.transform.SetParent(parent, false); root.transform.localPosition = at; root.transform.rotation = Quaternion.Euler(90, 0, 0);
            var light = root.AddComponent<Light>(); light.type = LightType.Spot; light.spotAngle = 105; light.innerSpotAngle = 70; light.range = 30;
            light.color = new Color(1, .85f, .68f); light.intensity = intensity; light.shadows = shadows ? LightShadows.Soft : LightShadows.None; light.shadowBias = .035f; light.shadowNormalBias = .25f; return light;
        }
        static TextMesh Sign(Transform root, string caption, Vector3 at, float width, float height, float character)
        {
            var label = SportsEventAuthoring.Label("Fixed gallery sign / " + caption, caption, at, character, new Color(1, .93f, .77f)); label.transform.SetParent(root, true); label.fontStyle = FontStyle.Bold;
            label.font.RequestCharactersInTexture(caption, label.fontSize, label.fontStyle);
            var bounds = label.GetComponent<Renderer>().bounds; float scale = Mathf.Min(1, width / Mathf.Max(.01f, bounds.size.x), height / Mathf.Max(.01f, bounds.size.y)); label.transform.localScale *= scale;
            return label;
        }
        static void FloorTile(Transform root, Vector3 at)
        {
            var go = RenderProp(root, Restaurant + "Floor_200x200.prefab", at, new Vector3(4, 4, 4)); var bounds = Bounds(go);
            go.transform.position += Vector3.up * (at.y - bounds.max.y);
        }
        static GameObject RenderProp(Transform root, string path, Vector3 at, Vector3 size)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (source == null) throw new InvalidOperationException("Installed original gallery prop required: " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source, root);
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var body in go.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
            if (go.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new InvalidOperationException("Gallery prop must have only static rendering: " + path);
            var bounds = Bounds(go); float scale = Mathf.Min(size.x / Mathf.Max(.001f, bounds.size.x), size.y / Mathf.Max(.001f, bounds.size.y), size.z / Mathf.Max(.001f, bounds.size.z));
            go.transform.localScale *= scale; bounds = Bounds(go); go.transform.position += at - bounds.center; return go;
        }
        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) throw new InvalidOperationException("Actual gallery renderer missing: " + go.name);
            var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
        }
    }
}
