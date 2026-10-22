using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Assets.CoreCollapse.Scripts.Structural;

namespace LWS.TruckBungeeDemolition.Editor
{
    // Explicit inspection/migration tooling only. Never a build or Play Mode hook.
    public static class BungeeBuildingRepairAuthoring
    {
        const string Folder = "Assets/TruckBungeeDemolition/";
        const string OriginalAnnex = Folder + "Prefabs/Ten Additional Reachable Buildings.prefab";
        const string OriginalTower = Folder + "Prefabs/Ten Storey Destructible Tower.prefab";
        public const string Annex = Folder + "Prefabs/Owned city demolition annex v2.prefab";
        public const string Tower = Folder + "Prefabs/Owned hotel demolition tower v2.prefab";
        const string Fractures = Folder + "Prefabs/Owned city fractures v2/";
        static readonly string[] Models = { "SM_Buildings_House_06_V1", "SM_Buildings_Hotel_01", "SM_Buildings_House_04_V1" };
        static Material cap;
        static MethodInfo slice;
        static string meshPath;
        static int meshIndex;

        public static void Upgrade()
        {
            EditorSceneManager.OpenScene(BungeeAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<BungeeMatch>();
            if (match.bays.All(b => AssetDatabase.GetAssetPath(b.expansionPrefab) == Annex && AssetDatabase.GetAssetPath(b.towerPrefab) == Tower))
            { Debug.Log("BUNGEE_CITY_PRESERVED existing authored variants"); return; }
            if (match.bays.Any(b => (AssetDatabase.GetAssetPath(b.expansionPrefab) != OriginalAnnex && AssetDatabase.GetAssetPath(b.expansionPrefab) != Folder+"Prefabs/Owned city demolition annex.prefab") || (AssetDatabase.GetAssetPath(b.towerPrefab) != OriginalTower && AssetDatabase.GetAssetPath(b.towerPrefab) != Folder+"Prefabs/Owned hotel demolition tower.prefab")))
                throw new InvalidOperationException("Preserve customized Bungee building assignments; explicit stock migration cannot replace them.");
            var slicer = Type.GetType("Assets.CoreCollapse.EzySlice.SlicerExtensions, Assembly-CSharp", true);
            slice = slicer.GetMethod("Slice", new[] { typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(Material) });
            cap = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Materials/Pale reinforced concrete.mat");
            Directory.CreateDirectory(Fractures);AssetDatabase.Refresh();
            var annex = AssetDatabase.LoadAssetAtPath<GameObject>(Annex) ?? CreateAnnex();
            var tower = AssetDatabase.LoadAssetAtPath<GameObject>(Tower) ?? CreateTower();
            foreach (var bay in match.bays) { bay.expansionPrefab = annex; bay.towerPrefab = tower; EditorUtility.SetDirty(bay); }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("BUNGEE_CITY_UPGRADED ten owned city buildings and hotel tower; original support graphs, budgets, collision footprints, swing tuning and cameras retained");
        }

        static GameObject CreateAnnex()
        {
            var root = PrefabUtility.LoadPrefabContents(OriginalAnnex);
            try
            {
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    var building = root.transform.GetChild(i);var modules = building.GetComponentsInChildren<BungeeModule>().OrderBy(m => m.level).ToArray();
                    var shape = modules[0].GetComponent<BoxCollider>();var size = new Vector3(shape.size.x, modules.Length * shape.size.y, shape.size.z);
                    BeginMeshes("City building " + i);
                    var model = Normalize(Models[i % Models.Length], size);
                    try
                    {
                        foreach (var module in modules)
                        {
                            var parts = Band(model, module.level * shape.size.y, (module.level + 1) * shape.size.y);
                            foreach (Transform child in module.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                            InstallBand(module, parts, "Annex " + i + " level " + module.level, null);
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(model); }
                    building.name = "Owned city demolition " + (i + 1) + " " + Models[i % Models.Length];
                }
                return PrefabUtility.SaveAsPrefabAsset(root, Annex);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static GameObject CreateTower()
        {
            var root = PrefabUtility.LoadPrefabContents(OriginalTower);BeginMeshes("Hotel tower");
            var model = Normalize("SM_Buildings_Hotel_01", new Vector3(11,48,11));
            try
            {
                foreach (var module in root.GetComponentsInChildren<BungeeModule>().Where(m => m.name.StartsWith("Floor diaphragm")))
                {
                    var original = new SerializedObject(module.core).FindProperty("collapsedPrefab").objectReferenceValue as GameObject;
                    foreach (Transform child in module.transform.Cast<Transform>().Where(t => t.name == "Inset window glazing" || t.name == "Facade sill beam").ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                    InstallBand(module, Band(model, module.level * 4.8f, (module.level + 1) * 4.8f), "Hotel level " + module.level, original);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, Tower);
            }
            finally { UnityEngine.Object.DestroyImmediate(model);PrefabUtility.UnloadPrefabContents(root); }
        }

        static void BeginMeshes(string name) { meshPath = Folder + "Models/Precut v2 " + name + ".asset";meshIndex = 0; }
        static Mesh Persist(Mesh mesh)
        {
            var copy = UnityEngine.Object.Instantiate(mesh);copy.name = "Owned city band fracture " + meshIndex++;
            if (!File.Exists(meshPath)) AssetDatabase.CreateAsset(copy, meshPath);else AssetDatabase.AddObjectToAsset(copy, meshPath);
            return copy;
        }
        static GameObject Normalize(string name, Vector3 size)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JC_LP_MegaCity/Prefabs/Buildings/" + name + ".prefab");
            if (asset == null) throw new InvalidOperationException("Owned Bungee source missing: " + name);
            var root = UnityEngine.Object.Instantiate(asset);var renderers = root.GetComponentsInChildren<MeshRenderer>();var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            // Extend the model with repeated vendor-authored middle storeys.
            // Preserve a natural window height rather than stretching a small
            // townhouse into a forty-metre shaft. Entry and roof stay unique.
            var scale = new Vector3(size.x / bounds.size.x, 1, size.z / bounds.size.z);scale.y=Mathf.Sqrt(scale.x*scale.z);
            root.transform.localScale = scale;root.transform.position = -Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
            int originalStories=name.Contains("06")?6:5;float storyHeight=bounds.size.y*scale.y/originalStories;int stories=Mathf.Max(originalStories,Mathf.RoundToInt(size.y/storyHeight));float fit=size.y/(stories*storyHeight);
            var extended=new GameObject("Owned authored facade with repeated middle storeys");
            for(int story=0;story<stories;story++)
            {
                int sourceStory=story<2?story:story>=stories-2?originalStories-(stories-story):2;
                foreach(var part in Band(root,sourceStory*storyHeight,(sourceStory+1)*storyHeight))
                {
                    var position=part.transform.position;position.y=(position.y+(story-sourceStory)*storyHeight)*fit;part.transform.position=position;
                    var partScale=part.transform.localScale;partScale.y*=fit;part.transform.localScale=partScale;part.transform.SetParent(extended.transform,true);
                }
            }
            UnityEngine.Object.DestroyImmediate(root);return extended;
        }
        static GameObject CopyMesh(MeshFilter source)
        {
            var part = new GameObject(source.name);part.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);part.transform.localScale = source.transform.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;part.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;return part;
        }
        static GameObject Cut(GameObject part, Vector3 point, Vector3 normal, bool upper)
        {
            var hull = slice.Invoke(null,new object[] { part,point,normal,cap });
            if (hull == null)
            {
                bool inside = Vector3.Dot(part.GetComponent<Renderer>().bounds.center-point,normal) >= 0;
                if (inside == upper) return part;UnityEngine.Object.DestroyImmediate(part);return null;
            }
            var result = (GameObject)hull.GetType().GetMethod(upper ? "CreateUpperHull" : "CreateLowerHull",new[] { typeof(GameObject),typeof(Material) }).Invoke(hull,new object[] { part,cap });
            UnityEngine.Object.DestroyImmediate(part);return result;
        }
        static List<GameObject> Band(GameObject model, float low, float high)
        {
            var parts = new List<GameObject>();
            foreach (var source in model.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh != null && m.GetComponent<MeshRenderer>() != null))
            {
                var part = Cut(CopyMesh(source), Vector3.up * low, Vector3.up, true);
                if (part == null) continue;part = Cut(part, Vector3.up * high, Vector3.up, false);
                if (part == null) continue;parts.Add(part);
            }
            if (parts.Count == 0) throw new InvalidOperationException("Vendor slicing left an empty Bungee storey " + low + " to " + high);
            return parts;
        }
        static void LocalPlace(Transform part, Transform parent, Vector3 origin)
        {
            var p = part.position;var r = part.rotation;var s = part.lossyScale;part.SetParent(parent,false);part.localPosition = p-origin;part.localRotation = r;part.localScale = s;
        }
        static void InstallBand(BungeeModule module, List<GameObject> parts, string label, GameObject baseFracture)
        {
            var fragments = new GameObject(label + " actual model debris");fragments.AddComponent<BungeeDebris>();
            var halves = new Transform[2];
            for (int side=0;side<2;side++)
            {
                halves[side] = new GameObject("Physical owned model cluster " + side).transform;halves[side].SetParent(fragments.transform,false);
                if (baseFracture != null)
                    foreach (var source in baseFracture.transform.GetChild(side).GetComponentsInChildren<MeshFilter>().Where(m => !m.name.StartsWith("Falling facade")))
                        LocalPlace(CopyMesh(source).transform,halves[side],Vector3.zero);
            }
            foreach (var part in parts)
            {
                for(int side=0;side<2;side++)
                {
                    var piece = Cut(CopyMesh(part.GetComponent<MeshFilter>()),Vector3.zero,Vector3.right,side==0);
                    if(piece==null)continue;piece.GetComponent<MeshFilter>().sharedMesh=Persist(piece.GetComponent<MeshFilter>().sharedMesh);LocalPlace(piece.transform,halves[side],module.transform.localPosition);
                }
                part.GetComponent<MeshFilter>().sharedMesh=Persist(part.GetComponent<MeshFilter>().sharedMesh);part.name="Owned city model band";LocalPlace(part.transform,module.transform,module.transform.localPosition);
            }
            foreach(var half in halves)
            {
                var renderers=half.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)throw new InvalidOperationException("Empty real-model Bungee fracture: "+label);
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                var body=half.gameObject.AddComponent<Rigidbody>();body.mass=3;body.solverIterations=8;
                var shape=half.gameObject.AddComponent<BoxCollider>();shape.center=half.InverseTransformPoint(bounds.center);shape.size=bounds.size;shape.sharedMaterial=module.GetComponent<Collider>().sharedMaterial;
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(fragments,Fractures+label+".prefab");UnityEngine.Object.DestroyImmediate(fragments);
            var settings=new SerializedObject(module.core);settings.FindProperty("collapsedPrefab").objectReferenceValue=prefab;settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(module.core);
        }

        public static void Preview()
        {
            EditorSceneManager.OpenScene(BungeeAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<BungeeMatch>();var bay=match.bays[0];
            var tower=UnityEngine.Object.Instantiate(bay.towerPrefab,bay.towerMount.position,bay.towerMount.rotation);var annex=UnityEngine.Object.Instantiate(bay.expansionPrefab);
            var camera=match.broadcast.view;var oldTarget=camera.targetTexture;var oldRect=camera.rect;var active=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);var target=new RenderTexture(1920,1080,24);bool asyncShaders=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try
            {
                camera.orthographic=false;camera.farClipPlane=160;match.broadcast.PoseRaw(new Vector3(3,24,5),new Vector3(-48,18,-69),64,true);
                target.Create();camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllBytes("Logs/TruckSports/PlaytestRepair/bungee-owned-city-editor-preview.png",image.EncodeToPNG());
            }
            finally{ShaderUtil.allowAsyncCompilation=asyncShaders;camera.targetTexture=oldTarget;camera.rect=oldRect;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(tower);UnityEngine.Object.DestroyImmediate(annex);}
        }

        public static void Audit()
        {
            EditorSceneManager.OpenScene(BungeeAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<BungeeMatch>();
            var report = new StringBuilder();
            foreach (var bay in match.bays)
                report.AppendLine("BAY " + bay.slot + " pump=" + bay.pumpAcceleration +
                    " restLength=" + bay.restLength + " tower=" + AssetDatabase.GetAssetPath(bay.towerPrefab) +
                    " annex=" + AssetDatabase.GetAssetPath(bay.expansionPrefab));
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/JC_LP_MegaCity/Prefabs/Buildings" })
                         .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = UnityEngine.Object.Instantiate(source);
                var meshes = instance.GetComponentsInChildren<MeshFilter>(true)
                    .Where(m => m.sharedMesh != null && m.GetComponent<MeshRenderer>() != null).ToArray();
                var bounds = meshes[0].GetComponent<Renderer>().bounds;
                foreach (var mesh in meshes) bounds.Encapsulate(mesh.GetComponent<Renderer>().bounds);
                report.AppendLine("MODEL " + path + " bounds=" + bounds + " meshes=" + meshes.Length +
                    " vertices=" + meshes.Sum(m => m.sharedMesh.vertexCount) +
                    " materials=" + string.Join(",", meshes.SelectMany(m => m.GetComponent<Renderer>().sharedMaterials)
                        .Where(m => m != null).Select(AssetDatabase.GetAssetPath).Distinct()));
                UnityEngine.Object.DestroyImmediate(instance);
            }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            File.WriteAllText("Logs/TruckSports/PlaytestRepair/bungee-building-asset-audit.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
