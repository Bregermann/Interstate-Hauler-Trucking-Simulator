#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FinalCourseLightingTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(180000)] public IEnumerator LavaPlatformsKeepSupportedPerimeterLights() => Verify("TruckFloorIsLava", 10);
    [UnityTest, Timeout(180000)] public IEnumerator TugLedgesKeepNightFixturesAndLavaArt() => Verify("TruckTugOfWar", 4);
    [UnityTest, Timeout(180000)] public IEnumerator CornholeWallsSupportIndependentCourseLights() => Verify("TruckCornhole", 32);
    [UnityTest, Timeout(180000)] public IEnumerator TargetGantryAndPlatformFixturesIlluminateTheOriginalView() => Verify("TruckTargetLaunch", 5);
    [UnityTest, Timeout(180000)] public IEnumerator TerrierDeckFixturesSurviveRandomizedRematch() => Verify("TruckTerrierRacing", 8);
    [UnityTest, Timeout(180000)] public IEnumerator BaseCargoAndLandingLightsFollowOneClock() => Verify("TruckBaseJumping", 8);
    [UnityTest, Timeout(180000)] public IEnumerator SkyTowShoreFixturesHaveRealDryTerrainSupport() => Verify("TruckSkyTow", 0);
    IEnumerator Verify(string name, int expected)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "FinalCourseLights" + name });
        var isolation = new GameObject("Isolated final course light acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        bool corn = name == "TruckCornhole";
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(corn ? Type.GetType("LWS.TruckCornhole.CornholeMatch, Assembly-CSharp", true) : Runtime("SportsEventSession")).Cast<Component>().Single(); Call(match, corn ? "AllAI" : "AllBots");
            var clock = UnityEngine.Object.FindObjectsByType(Runtime("SportsMatchTime")).Cast<Component>().Single(); var lamps = (Light[])Get(clock, "nightLights");
            if (expected == 0) Assert.That(lamps.Length, Is.InRange(20, 62)); else Assert.That(lamps.Length, Is.EqualTo(expected));
            var roots = match.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Where(item => item.name == "Authored final course night fixtures").ToArray();
            Assert.That(roots.Length, Is.EqualTo(name == "TruckTerrierRacing" ? 2 : 1));
            foreach (var root in roots)
            {
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Joint>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true).All(component => component.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalAdditionalLightData"), Is.True);
            }
            Assert.That(lamps.All(light => light.shadows == LightShadows.None && light.type == LightType.Spot && light.range <= 220), Is.True);
            var ids = lamps.Select(light => light.GetInstanceID()).ToArray();
            var weather = UnityEngine.Object.FindObjectsByType(Runtime("SportsChaosWeather")).Cast<Component>().Single(); var catalog = Get(weather, "presets");
            Call(catalog, "Select", Get(((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"), "id"));
            SelectTime("Night"); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(name == "TruckBaseJumping" ? 9 : 3);
            Assert.That(lamps.All(light => light.enabled && light.intensity > 0), Is.True);
            if (name == "TruckSkyTow" || name == "TruckFloorIsLava")
            {
                var terrain = UnityEngine.Object.FindObjectsByType<Terrain>().Single();
                foreach (var lamp in lamps) { var mount = lamp.transform.parent.position; Assert.That(mount.y, Is.EqualTo(terrain.SampleHeight(mount) + terrain.transform.position.y).Within(.03f)); }
                if (name == "TruckSkyTow") { var course = Get(match, "course"); float water = ((Transform)Get(course, "water")).position.y; Assert.That(lamps.All(light => light.transform.parent.position.y > water + 2), Is.True); }
            }
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; Assert.That(Get(pipeline, "maxAdditionalLightsCount"), Is.EqualTo(8));
            var renderer = new SerializedObject(pipeline).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue; Assert.That(Get(renderer, "renderingMode").ToString(), Is.EqualTo("ForwardPlus"));
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>().Where(camera => camera.enabled && camera.gameObject.scene == match.gameObject.scene && camera.targetTexture == null).ToArray(); Assert.That(cameras, Is.Not.Empty);
            Time.timeScale = 0; Read(cameras[0]); var lit = Read(cameras[0]); foreach (var lamp in lamps) lamp.enabled = false; Color[] dark;
            try { dark = Read(cameras[0]); } finally { foreach (var lamp in lamps) lamp.enabled = true; }
            int changed = lit.Zip(dark, (a,b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value); TestContext.WriteLine(name + " lamps=" + lamps.Length + " actualLitPixels=" + changed + "/57600 mean=" + lit.Average(pixel => pixel.grayscale));
            var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>();
            foreach (int width in new[] { 1280, 1920 }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 6).Invoke(null, new object[] { canvas, cameras, width, width == 1280 ? 720 : 1080, "Logs/TruckSports/PlaytestRepair/FinalCourseLightingNative/" + name + "-night-" + width + ".png", null });
            Assert.That(changed, Is.GreaterThan(500), "The actual gameplay view must receive venue light beyond existing headlights."); Time.timeScale = 1;
            Call(match, corn ? "ChangeSetup" : "SetupScreen"); SelectTime("Day"); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(.5f); Assert.That(lamps.All(light => light.intensity == 0), Is.True);
            Call(match, corn ? "ChangeSetup" : "SetupScreen"); SelectTime("Night"); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(.5f); CollectionAssert.AreEqual(ids, lamps.Select(light => light.GetInstanceID()).ToArray()); Assert.That(lamps.All(light => light.intensity > 0), Is.True);
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, corn ? "ChangeSetup" : "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    static void SelectTime(string name) => Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), name) });
    static Color[] Read(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); var prior = camera.targetTexture; var active = RenderTexture.active; Texture2D image = null;
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
