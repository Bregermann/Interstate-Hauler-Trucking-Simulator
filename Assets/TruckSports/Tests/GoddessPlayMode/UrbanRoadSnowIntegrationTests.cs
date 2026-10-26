#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class UrbanRoadSnowIntegrationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(180000)] public IEnumerator FareRoadsAccumulateAndTraceWithoutChangingSourceMaterials() => Verify("TruckFareFight", 2, 46);
    [UnityTest, Timeout(180000)] public IEnumerator SnakeRoadsAccumulateAndTraceWithoutChangingSourceMaterials() => Verify("TruckSnake", 1, 60);
    IEnumerator Verify(string name, int materialCount, int rendererCount)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "UrbanRoadSnow" + name });
        var isolation = new GameObject("Isolated native urban road snow acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        var paths = Directory.GetFiles("Assets/" + name + "/Materials/Urban Road Snow", "*.mat"); var originalFiles = paths.Select(File.ReadAllBytes).ToArray(); Assert.That(paths.Length, Is.EqualTo(materialCount));
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
            var weather = match.GetComponent(Runtime("SportsChaosWeather")); var catalog = Get(weather, "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray(); var bridge = match.GetComponent(Runtime("SportsSnowSurface")); var surface = Get(bridge, "surface");
            Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Morning") });
            Camera camera = null; int peakContacts = 0;
            foreach (string forecast in new[] { "Clear", "Snow", "Clear", "Snow" })
            {
                Call(match, "SetupScreen"); Call(catalog, "Select", Get(entries.Single(item => (string)Get(item, "displayName") == forecast), "id")); Assert.That(Call(match, "StartMatch"), Is.True); yield return null;
                camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); float until = Time.realtimeSinceStartup + 7;
                while (Time.realtimeSinceStartup < until)
                {
                    Read(camera); peakContacts = Math.Max(peakContacts, (int)Get(bridge, "RoadContacts")); yield return new WaitForSecondsRealtime(.15f);
                }
                Assert.That((int)Get(bridge, "RuntimeRoadMaterialCount"), Is.EqualTo(materialCount), "Reuse one runtime material per saved shared source, not one per road segment.");
                if (forecast == "Clear")
                {
                    Assert.That((float)Get(bridge, "Coverage"), Is.Zero); Assert.That((bool)Get(bridge, "RoadsInClearMode"), Is.True); Save("clear-" + (peakContacts == 0 ? "initial" : "restored")); continue;
                }
                Assert.That((float)Get(bridge, "Coverage"), Is.GreaterThan(.5f)); Assert.That((float)Get(bridge, "MaximumRoadCoverage"), Is.GreaterThan(.5f)); Assert.That((int)Get(surface, "RoadDepthSourceCount"), Is.EqualTo(rendererCount));
                Assert.That((int)Get(surface, "BoundSnowMaterialCount"), Is.EqualTo(materialCount)); Assert.That((bool)Get(surface, "TraceMaskReady"), Is.True); Assert.That((int)Get(bridge, "depthRenderer"), Is.EqualTo(1));
                Call(surface, "RequestTracePixelDiagnostic"); float readbackUntil = Time.realtimeSinceStartup + 3;
                while (((string)Get(surface, "TracePixelDiagnostic")).Contains("pending") && Time.realtimeSinceStartup < readbackUntil) yield return null;
                TestContext.WriteLine(name + " coverage=" + Get(bridge, "Coverage") + " roadCoverage=" + Get(bridge, "MaximumRoadCoverage") + " sharedRoadMaterials=" + Get(bridge, "RuntimeRoadMaterialCount") + " roadSources=" + Get(surface, "RoadDepthSourceCount") + " peakPhysicalWheelContacts=" + peakContacts + " " + Get(surface, "TracePixelDiagnostic"));
                Assert.That(peakContacts, Is.GreaterThan(0), "Original AI trucks must produce actual grounded road-wheel contacts.");
                Assert.That((int)Get(surface, "TraceActivePixelCount"), Is.GreaterThan(0), "Read the actual vendor GPU trace texture; a ready component alone is insufficient.");
                Time.timeScale = 0; var applied = Read(camera); var roadMaterials = ((IDictionary)Get(bridge, "runtimeRoads")).Values.Cast<Material>().ToArray(); var coverage = roadMaterials.Select(material => material.GetFloat("_CoverageAmount")).ToArray(); Color[] bare;
                try { foreach (var material in roadMaterials) material.SetFloat("_CoverageAmount", 0); bare = Read(camera); }
                finally { for (int i = 0; i < roadMaterials.Length; i++) roadMaterials[i].SetFloat("_CoverageAmount", coverage[i]); }
                int changed = applied.Zip(bare, (a, b) => a.grayscale - b.grayscale > .04f).Count(value => value); TestContext.WriteLine(name + " actual vendor road snow brighterPixels=" + changed + "/57600");
                Assert.That(changed, Is.GreaterThan(1000), "The actual road snow shader must visibly affect the rendered image."); Save("snow-" + (peakContacts > 0 ? Get(match, "MatchId") : "first")); Time.timeScale = 1;
            }
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(originalFiles[i], File.ReadAllBytes(paths[i]), "Play Mode/rematches must not save runtime coverage into source materials.");
            void Save(string label)
            {
                foreach (int width in new[] { 1280, 1920 }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, "Logs/TruckSports/PlaytestRepair/UrbanRoadSnowNative/" + name + "-" + label + "-" + width + ".png", null });
            }
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
