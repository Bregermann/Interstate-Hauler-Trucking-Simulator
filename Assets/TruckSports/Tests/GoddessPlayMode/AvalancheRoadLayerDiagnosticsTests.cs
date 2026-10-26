#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Explicit unsaved diagnosis. Renderer toggles never change road/terrain collision.
public sealed class AvalancheRoadLayerDiagnosticsTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [Explicit("Read-only road-layer diagnosis, not gameplay or visual acceptance.")]
    [UnityTest, Timeout(180000)] public IEnumerator CompareOriginalStagingRoadAndTerrainVisibility()
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "AvalancheRoadLayers" });
        var isolation = new GameObject("Isolated road-layer diagnosis"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession")).Cast<Component>().Single(); Call(match, "AllBots");
            var catalog = Get(match.GetComponent(Runtime("SportsChaosWeather")), "presets"); Call(catalog, "Select", Get(((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"), "id"));
            Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Night") }); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(5); Time.timeScale = 0;
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled);
            var staging = GameObject.Find("Broad flat safe mountain staging platform").GetComponent<MeshRenderer>();
            var roads = GameObject.Find("Existing EasyRoads authored evacuation network").GetComponentsInChildren<MeshRenderer>();
            var shortcuts = roads.Where(renderer => renderer.GetComponents<MonoBehaviour>().Any(component => component != null && component.GetType().FullName == "LWS.InterstateHauler.LwsRoadSurface" && ((string)Get(component, "RoadId")).IndexOf("shortcut", StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            var terrain = (Terrain)Get(Get(match, "world"), "terrain"); var report = new StringBuilder(); Assert.That(shortcuts, Is.Not.Empty);
            report.AppendLine("Staging " + staging.bounds + "; roads=" + roads.Length + "; shortcut renderers=" + shortcuts.Length);
            foreach (float z in new[] { -95f, -78f, -58f, -38f, -22f }) foreach (float x in new[] { -28f, -8f, 0f, 8f, 28f })
            {
                var ray = new Ray(new Vector3(x, 500, z), Vector3.down); var samples = new System.Collections.Generic.List<string>();
                var stagingShape = staging.GetComponent<Collider>(); if (stagingShape.Raycast(ray, out var top, 200)) samples.Add("staging=" + top.point.y.ToString("F5"));
                foreach (var renderer in roads) if (renderer.GetComponent<Collider>().Raycast(ray, out var hit, 200)) samples.Add(renderer.GetComponents<MonoBehaviour>().Where(component => component != null && component.GetType().FullName == "LWS.InterstateHauler.LwsRoadSurface").Select(component => (string)Get(component, "RoadId")).Single() + "=" + hit.point.y.ToString("F5"));
                report.AppendLine("SURFACES x=" + x + " z=" + z + " " + string.Join("; ", samples));
            }
            var original = Read(camera); Capture("original");
            bool stagingVisible = staging.enabled; try { staging.enabled = false; Compare("staging-renderer-off"); } finally { staging.enabled = stagingVisible; }
            var enabled = shortcuts.Select(renderer => renderer.enabled).ToArray(); try { foreach (var renderer in shortcuts) renderer.enabled = false; Compare("shortcut-renderers-off"); } finally { for (int i = 0; i < shortcuts.Length; i++) shortcuts[i].enabled = enabled[i]; }
            bool heightmapVisible = terrain.drawHeightmap; try { terrain.drawHeightmap = false; Compare("terrain-renderer-off"); } finally { terrain.drawHeightmap = heightmapVisible; }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair/AvalancheRoadLayerAudit"); File.WriteAllText("Logs/TruckSports/PlaytestRepair/AvalancheRoadLayerAudit/observations.txt", report.ToString()); TestContext.WriteLine(report.ToString());
            void Compare(string label) { var pixels = Read(camera); report.AppendLine(label + " changedPixels=" + original.Zip(pixels, (a,b) => Mathf.Abs(a.grayscale - b.grayscale) > .02f).Count(value => value)); Capture(label); }
            void Capture(string label) => Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, 1280, 720, "Logs/TruckSports/PlaytestRepair/AvalancheRoadLayerAudit/" + label + ".png", null });
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    static Color[] Read(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); var prior = camera.targetTexture; var active = RenderTexture.active; Texture2D image = null;
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
