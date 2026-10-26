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

public sealed class MountainCourseLightingTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(180000)] public IEnumerator AvalancheRouteLampsPreserveOriginalClockAndTerrain() => Verify("TruckAvalancheEscape", 0);
    [UnityTest, Timeout(180000)] public IEnumerator BogCourseActivationOwnsItsShoreLights() => Verify("TruckBogSnorkelling", 34);
    [UnityTest, Timeout(180000)] public IEnumerator MattressFootingsStayBelowRandomizedBankSurface() => Verify("TruckMattressRacing", 26);
    [UnityTest, Timeout(180000)] public IEnumerator MinefieldLightsReachTheOriginalExtendedRunout() => Verify("TruckMinefield", 186);
    [UnityTest, Timeout(180000)] public IEnumerator WifeCircuitHasSupportedContinuousVenueLights() => Verify("TruckWifeCarrying", 20);
    [UnityTest, Timeout(180000)] public IEnumerator CheeseSlopeHasSupportedVenueLights() => Verify("TruckCheeseRolling", 16);
    IEnumerator Verify(string name, int count)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "MountainLamps" + name });
        var isolation = new GameObject("Isolated course lighting acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; bool cheese = name == "TruckCheeseRolling";
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            var matchType = cheese ? Type.GetType("LWS.TruckCheeseRolling.CheeseMatch, Assembly-CSharp", true) : Runtime("SportsEventSession");
            match = UnityEngine.Object.FindObjectsByType(matchType).Cast<Component>().Single();
            if (name == "TruckAvalancheEscape")
            {
                var staging = GameObject.Find("Broad flat safe mountain staging platform");
                Assert.That(staging.GetComponent<MeshRenderer>().enabled, Is.False, "The coplanar staging skin must not fight the EasyRoads surface.");
                Assert.That(staging.GetComponent<Collider>().enabled, Is.True, "The original safe staging collider remains active.");
                foreach (float x in new[] { -28f, 0f, 28f })
                {
                    Assert.That(staging.GetComponent<Collider>().Raycast(new Ray(new Vector3(x, 450, -65), Vector3.down), out var hit, 30), Is.True);
                    Assert.That(hit.point.y, Is.EqualTo(430).Within(.001f));
                }
            }
            if (name == "TruckBogSnorkelling") match.GetType().GetField("option").SetValue(match, 0);
            Call(match, cheese ? "AllAI" : "AllBots");
            var clock = UnityEngine.Object.FindObjectsByType(Runtime("SportsMatchTime")).Cast<Component>().Single();
            var lamps = (Light[])Get(clock, "nightLights");
            if (count == 0) Assert.That(lamps.Length, Is.InRange(60, 220)); else Assert.That(lamps.Length, Is.EqualTo(count));
            var roots = match.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Where(item => item.name == "Authored mountain course night fixtures").ToArray();
            Assert.That(roots.Length, Is.EqualTo(name == "TruckBogSnorkelling" ? 3 : 1));
            foreach (var root in roots)
            {
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Joint>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true).All(component => component.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalAdditionalLightData"), Is.True, "Only URP's runtime-added light metadata is allowed; no new gameplay behaviour.");
            }
            Assert.That(roots.Sum(root => root.GetComponentsInChildren<MeshRenderer>(true).Length), Is.EqualTo(lamps.Length), "Every light retains the installed modeled fixture.");
            Assert.That(lamps.All(light => light.shadows == LightShadows.None && light.type == LightType.Spot && light.range <= 190), Is.True);
            var ids = lamps.Select(light => light.GetHashCode()).ToArray();
            var weather = UnityEngine.Object.FindObjectsByType(Runtime("SportsChaosWeather")).Cast<Component>().Single(); var catalog = Get(weather, "presets");
            Call(catalog, "Select", Get(((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"), "id"));
            SelectTime("Night"); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(5);
            Assert.That(lamps.All(light => light.enabled), Is.True); var nightPowers = lamps.Select(light => light.intensity).ToArray();
            if (name == "TruckMattressRacing")
            {
                var terrain = (Terrain)Get(Get(match, "course"), "terrain");
                foreach (var light in lamps)
                {
                    var mount = light.transform.parent.position; float height = terrain.SampleHeight(mount) + terrain.transform.position.y;
                    Assert.That(mount.y, Is.LessThanOrEqualTo(height + .1f), "No floating footing after the actual randomized course generation."); Assert.That(height - mount.y, Is.LessThan(6));
                }
            }
            if (name == "TruckMinefield") Assert.That(lamps.Max(light => light.transform.position.z), Is.GreaterThan(8200), "Retain illumination into the original long landing runout.");
            var cameras = ((Array)Get(match, "cameras")).Cast<object>().Select(item => item is Camera camera ? camera : ((Component)item).GetComponent<Camera>()).ToArray();
            var active = cameras.First(camera => camera.enabled); Time.timeScale = 0; Read(active); var lit = Read(active); foreach (var lamp in lamps) lamp.enabled = false; Color[] dark;
            try { dark = Read(active); } finally { foreach (var lamp in lamps) lamp.enabled = true; }
            int changed = lit.Zip(dark, (a,b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value);
            float mean = lit.Average(pixel => pixel.grayscale), clipped = lit.Count(pixel => pixel.r > .97f && pixel.g > .97f && pixel.b > .97f) / 57600f;
            TestContext.WriteLine(name + " fixtures=" + lamps.Length + " actualLitPixels=" + changed + "/57600 mean=" + mean + " whiteClippedFraction=" + clipped); Capture("night"); Assert.That(changed, Is.GreaterThan(1000), "The original gameplay camera must receive venue light beyond existing headlights."); Time.timeScale = 1;
            string pipelinePath = name == "TruckMinefield" || name == "TruckWifeCarrying" ? "Assets/" + name + "/Rendering/Weather pipeline.asset" : "Assets/TruckSports/Rendering/" + name + " snow pipeline.asset";
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; Assert.That(AssetDatabase.GetAssetPath(pipeline), Is.EqualTo(pipelinePath)); Assert.That(Get(pipeline, "maxAdditionalLightsCount"), Is.EqualTo(8));
            var renderers = new SerializedObject(pipeline).FindProperty("m_RendererDataList"); Assert.That(renderers.arraySize, Is.EqualTo(2));
            var renderer = renderers.GetArrayElementAtIndex(0).objectReferenceValue; Assert.That(Get(renderer, "renderingMode").ToString(), Is.EqualTo("ForwardPlus")); Assert.That(new SerializedObject(renderer).FindProperty("m_RendererFeatures").arraySize, Is.EqualTo(cheese || name == "TruckWifeCarrying" ? 2 : 1), "Retain Weather Maker plus the original Cheese mushroom or Wife water feature where present.");
            Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderers.GetArrayElementAtIndex(1).objectReferenceValue)), Is.EqualTo("2de0078876aa96b4289a9cd6ccfc10de"));
            Call(match, cheese ? "ChangeSetup" : "SetupScreen"); SelectTime("Day"); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(.5f); Assert.That(lamps.All(light => light.intensity == 0), Is.True, "The established clock turns daytime contribution off through intensity.");
            Call(match, cheese ? "ChangeSetup" : "SetupScreen"); SelectTime("Night"); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(.5f); CollectionAssert.AreEqual(ids, lamps.Select(light => light.GetHashCode()).ToArray()); CollectionAssert.AreEqual(nightPowers, lamps.Select(light => light.intensity).ToArray());
            Call(match, "Pause", 0, false); yield return null; Call(match, "Resume"); Assert.That(lamps.All(light => light.enabled), Is.True);
            if (name == "TruckBogSnorkelling")
            {
                Call(match, "SetupScreen"); match.GetType().GetField("option").SetValue(match, 1); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(.5f);
                var course = Get(match, "course"); Assert.That((bool)Get(course, "Hard"), Is.True); Assert.That(lamps.Count(light => light.gameObject.activeInHierarchy), Is.EqualTo(22), "Normal-course fixtures retire with the normal terrain.");
                yield return new WaitForSecondsRealtime(5); Capture("hard-night");
            }
            void Capture(string label)
            {
                var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>();
                foreach (int width in new[] { 1280, 1920 }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 6).Invoke(null, new object[] { canvas, cameras, width, width == 1280 ? 720 : 1080, "Logs/TruckSports/PlaytestRepair/MountainCourseLightingNative/" + name + "-" + label + "-" + width + ".png", null });
            }
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, cheese ? "ChangeSetup" : "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
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
