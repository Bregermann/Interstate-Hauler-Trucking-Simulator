using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckMechanicalBull.Editor
{
    // Small author-time mesh recipes for the missing original rodeo prop/mats.
    // Vendor trucks, characters, scenery and clips remain referenced in place.
    public static class ImpossibleBalanceArt
    {
        public static void GuardNew(string scene)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || File.Exists(scene))
                throw new InvalidOperationException("Stopped, compiled Editor and a NEW scene required. Existing designer scenes are never overwritten: " + scene);
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save current scene edits before explicit new-scene authoring.");
        }
        public static SportsEventTruck Tractor(SportsEventSession match, int slot, Vector3 position)
        {
            const string path = "Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("Existing full NWH Fare tractor is required; this author never rebuilds it.", path);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            root.name = match.Title + " · owner " + (slot + 1); root.transform.SetPositionAndRotation(position, Quaternion.identity);
            var truck = root.GetComponent<SportsEventTruck>();
            if (truck == null || truck.tractorMotor == null || truck.shape == null || truck.wheels.Length != 4)
                throw new InvalidOperationException("Complete existing four-wheel NWH tractor required.");
            truck.session = match; truck.slot = slot; truck.tractorMotor.settings = match.settings;
            truck.Power(false); truck.body.isKinematic = true; truck.body.detectCollisions = false;
            return truck;
        }
        public static Material Material(string folder, string name, Color color, float smoothness = .3f, float metallic = 0)
        {
            string directory = "Assets/" + folder + "/Materials"; Directory.CreateDirectory(directory);
            string path = directory + "/" + name + ".mat";
            var prior = AssetDatabase.LoadAssetAtPath<Material>(path); if (prior != null) return prior;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color, enableInstancing = true };
            material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Metallic", metallic); AssetDatabase.CreateAsset(material, path); return material;
        }
        public static Mesh Mesh(string folder, string name, Func<Mesh> build)
        {
            string directory = "Assets/" + folder + "/Models";
            if (!AssetDatabase.IsValidFolder(directory)) { Directory.CreateDirectory(directory); AssetDatabase.Refresh(); }
            string path = directory + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            mesh = build(); mesh.name = name; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        public static GameObject Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Quaternion rotation = default)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            root.transform.localScale = scale; root.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
            root.AddComponent<MeshFilter>().sharedMesh = mesh; root.AddComponent<MeshRenderer>().sharedMaterial = material; return root;
        }
        public static Mesh RoundedBlock()
        {
            // Bevelled octagonal cross-section with inset end rings, not a cube.
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            Vector2[] outline = { new Vector2(-.42f,-.5f),new Vector2(.42f,-.5f),new Vector2(.5f,-.42f),new Vector2(.5f,.42f),new Vector2(.42f,.5f),new Vector2(-.42f,.5f),new Vector2(-.5f,.42f),new Vector2(-.5f,-.42f) };
            for (int ring = 0; ring < 4; ring++) for (int i = 0; i < 8; i++)
            {
                float y = ring == 0 ? -.5f : ring == 1 ? -.44f : ring == 2 ? .44f : .5f;
                float inset = ring == 0 || ring == 3 ? .92f : 1;
                vertices.Add(new Vector3(outline[i].x * inset, y, outline[i].y * inset)); uv.Add(new Vector2(i / 7f, ring / 3f));
            }
            for (int ring = 0; ring < 3; ring++) for (int i = 0; i < 8; i++) { int a = ring * 8 + i, b = ring * 8 + (i + 1) % 8; indices.AddRange(new[] { a, b + 8, b, a, a + 8, b + 8 }); }
            vertices.Add(Vector3.down * .5f); uv.Add(Vector2.zero); vertices.Add(Vector3.up * .5f); uv.Add(Vector2.one);
            for (int i = 0; i < 8; i++) { indices.AddRange(new[] { 32, i, (i + 1) % 8 }); indices.AddRange(new[] { 33, 24 + (i + 1) % 8, 24 + i }); }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); return mesh;
        }
        public static Mesh Lathe(Vector2[] profile, int sides = 40)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int row = 0; row < profile.Length; row++) for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides; vertices.Add(new Vector3(Mathf.Sin(a) * profile[row].x, profile[row].y, Mathf.Cos(a) * profile[row].x)); uv.Add(new Vector2(i / (float)sides, row / (float)(profile.Length - 1)));
            }
            for (int row = 0; row < profile.Length - 1; row++) for (int i = 0; i < sides; i++) { int a = row * (sides + 1) + i, b = a + sides + 1; triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); return mesh;
        }
        public static Mesh Ovoid()
        {
            var profile = new Vector2[21]; for (int i = 0; i < profile.Length; i++) { float angle = i * Mathf.PI / 20; profile[i] = new Vector2(Mathf.Sin(angle) * .5f, -Mathf.Cos(angle) * .5f); } return Lathe(profile);
        }
        public static Mesh Horn(int side)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); const int rings = 18, sides = 12;
            for (int row = 0; row <= rings; row++)
            {
                float u = row / (float)rings; var center = new Vector3(side * (u * 4.5f), Mathf.Sin(u * Mathf.PI * .65f) * 2.2f, -u * u * 1.3f);
                float radius = Mathf.Lerp(.55f, .015f, u);
                for (int i = 0; i <= sides; i++) { float a = i * Mathf.PI * 2 / sides; v.Add(center + new Vector3(0, Mathf.Sin(a), Mathf.Cos(a)) * radius); uv.Add(new Vector2(u, i / (float)sides)); }
            }
            for (int row = 0; row < rings; row++) for (int i = 0; i < sides; i++) { int a = row * (sides + 1) + i, b = a + sides + 1; if (side > 0) t.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 }); else t.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 }); }
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); return mesh;
        }
        public static void UI(SportsEventSession match)
        {
            SportsEventAuthoring.UI(match);
            var menu = match.menus; var rail = menu.header.transform.parent.GetComponent<RectTransform>(); rail.sizeDelta = new Vector2(1880, 48); rail.anchoredPosition = new Vector2(0, 508);
            menu.header.rectTransform.sizeDelta = new Vector2(1810, 42); menu.header.fontSizeMax = 24;
            menu.status.rectTransform.anchoredPosition = new Vector2(0, -486); menu.status.rectTransform.sizeDelta = new Vector2(1730, 52); menu.status.fontSizeMax = 24;
            menu.standings.rectTransform.anchoredPosition = new Vector2(-725, 325); menu.standings.rectTransform.sizeDelta = new Vector2(435, 360); menu.standings.fontSizeMax = 21;
        }
        public static void Finish(SportsEventSession match, string folder, SportsEventTruck[] trucks)
        {
            SportsEventAuthoring.Save(folder);
            SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match, trucks);
            EditorSceneManager.MarkSceneDirty(match.gameObject.scene); EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
        }
    }
}
