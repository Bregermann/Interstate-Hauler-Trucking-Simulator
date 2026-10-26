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

public sealed class UrbanVenueLightingTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(180000)] public IEnumerator FareRoadsHaveSupportedNightFixtures() => Verify("TruckFareFight");
    [UnityTest, Timeout(180000)] public IEnumerator SnakeRoadsHaveSupportedNightFixtures() => Verify("TruckSnake");
    [UnityTest, Timeout(180000)] public IEnumerator HideRetainsExistingLoadingLightsAndTimeOwnership() => Verify("TruckHideAndSeek");

    internal IEnumerator Verify(string name, bool diagnostic = false)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "UrbanVenue" + name });
        var isolation = new GameObject("Isolated urban lighting acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
            var clock = UnityEngine.Object.FindObjectsByType(Runtime("SportsMatchTime"), FindObjectsSortMode.None).Cast<Component>().Single();
            var lamps = (Light[])Get(clock, "nightLights"); var powers = lamps.Select(item => item.intensity).ToArray(); var ids = lamps.Select(item => item.GetInstanceID()).ToArray();
            bool hide = name == "TruckHideAndSeek";
            if (hide)
            {
                Assert.That(lamps.Length, Is.EqualTo(30)); Assert.That(powers.Count(power => power == 75), Is.EqualTo(18)); Assert.That(powers.Count(power => power == 110), Is.EqualTo(12));
                Assert.That(lamps.All(item => item.name == "Supported warm night-shift floodlight" && item.transform.parent.name == "Grounded night-shift lighting and sign supports"), Is.True);
            }
            else
            {
                Assert.That(lamps.Length, Is.GreaterThanOrEqualTo(20));
                var root = clock.transform.Find("Authored urban roadside night fixtures"); Assert.That(root, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Joint>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(lamps.Length));
                Assert.That(lamps.All(item => item.transform.IsChildOf(root) && item.type == LightType.Spot && item.shadows == LightShadows.None), Is.True);
                Physics.SyncTransforms();
                foreach (var light in lamps)
                {
                    var point = light.transform.parent.position;
                    Assert.That(Physics.RaycastAll(point + Vector3.up * .1f, Vector3.down, .2f, ~0, QueryTriggerInteraction.Ignore).Any(hit => hit.rigidbody == null && Mathf.Abs(hit.point.y - point.y) < .02f), Is.True, "Mount must contact original static geometry.");
                }
            }
            var catalog = Get(match.GetComponent(Runtime("SportsChaosWeather")), "presets"); var clear = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"); Call(catalog, "Select", Get(clear, "id"));
            foreach (string choice in new[] { "Night", "Day", "Evening", "Morning", "Night" })
            {
                Call(match, "SetupScreen"); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), choice) }); Assert.That(Call(match, "StartMatch"), Is.True);
                yield return new WaitForSecondsRealtime(.5f); float gain = choice == "Night" || choice == "Evening" ? 1 : choice == "Morning" && !hide ? .65f : 0;
                for (int i = 0; i < lamps.Length; i++) Assert.That(lamps[i].intensity, Is.EqualTo(powers[i] * gain).Within(.01f), "Preserve original night powers and explicit shared clock gains.");
                int revision = (int)Get(clock, "Revision"); Call(clock, "Configure", -123, Get(match, "MatchId")); Assert.That(Get(clock, "Revision"), Is.EqualTo(revision));
                Call(match, "Pause", 0, false); yield return null; Call(match, "Resume"); CollectionAssert.AreEqual(ids, lamps.Select(item => item.GetInstanceID()).ToArray());
                if (choice == "Morning" && !hide) Capture("clear-morning");
            }
            yield return new WaitForSecondsRealtime(4); Time.timeScale = 0; yield return new WaitForSecondsRealtime(.4f);
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); Render(camera); var lit = Render(camera);
            foreach (var light in lamps) light.enabled = false;
            Color[] dark; try { dark = Render(camera); } finally { foreach (var light in lamps) light.enabled = true; }
            int brighter = lit.Zip(dark, (a, b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value);
            TestContext.WriteLine(name + " fixtures=" + lamps.Length + " brighterPixels=" + brighter + "/57600");
            if (diagnostic)
            {
                var original = QualitySettings.renderPipeline;
                var current = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                foreach (bool clustered in new[] { false, true })
                {
                    var clone = UnityEngine.Object.Instantiate(current); clone.hideFlags = HideFlags.DontSave; ScriptableObject rendererClone = null;
                    try
                    {
                        clone.GetType().GetProperty("maxAdditionalLightsCount").SetValue(clone, 8);
                        if (clustered)
                        {
                            var settings = new SerializedObject(clone); var first = settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
                            rendererClone = UnityEngine.Object.Instantiate((ScriptableObject)first.objectReferenceValue); rendererClone.hideFlags = HideFlags.DontSave;
                            var mode = rendererClone.GetType().GetProperty("renderingMode"); mode.SetValue(rendererClone, Enum.Parse(mode.PropertyType, "ForwardPlus")); first.objectReferenceValue = rendererClone; settings.ApplyModifiedPropertiesWithoutUndo();
                        }
                        QualitySettings.renderPipeline = clone; yield return null; yield return null; Render(camera); var enabled = Render(camera);
                        foreach (var light in lamps) light.enabled = false;
                        Color[] disabled; try { disabled = Render(camera); } finally { foreach (var light in lamps) light.enabled = true; }
                        int changed = enabled.Zip(disabled, (a, b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value);
                        TestContext.WriteLine("UNSAVED DIAGNOSTIC " + name + " " + (clustered ? "ForwardPlus" : "ForwardCap8") + " brighterPixels=" + changed);
                        Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, 1280, 720, "Logs/TruckSports/PlaytestRepair/UrbanLightBudgetDiagnostics/" + name + (clustered ? "-clustered" : "-cap8") + "-720.png", null });
                    }
                    finally { QualitySettings.renderPipeline = original; }
                    yield return null; yield return null; UnityEngine.Object.DestroyImmediate(clone); if (rendererClone != null) UnityEngine.Object.DestroyImmediate(rendererClone);
                }
                yield break;
            }
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/UrbanVenueLighting/" + name + "-night-" + size.y + ".png", null });
            Assert.That(brighter, Is.GreaterThan(hide ? 100 : 10000), "Original gameplay view must receive actual illumination; do not accept isolated headlight pixels as a lit road.");
            if (!hide)
            {
                var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                Assert.That(pipeline.GetType().GetProperty("maxAdditionalLightsCount").GetValue(pipeline), Is.EqualTo(8));
                var settings = new SerializedObject(pipeline); var renderers = settings.FindProperty("m_RendererDataList"); Assert.That(renderers.arraySize, Is.EqualTo(2));
                var renderer = renderers.GetArrayElementAtIndex(0).objectReferenceValue;
                Assert.That(renderer.GetType().GetProperty("renderingMode").GetValue(renderer).ToString(), Is.EqualTo("ForwardPlus"));
                Assert.That(new SerializedObject(renderer).FindProperty("m_RendererFeatures").arraySize, Is.EqualTo(1), "Retain the original Weather Maker renderer feature.");
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderers.GetArrayElementAtIndex(1).objectReferenceValue)), Is.EqualTo("2de0078876aa96b4289a9cd6ccfc10de"), "Retain the original Weatherade depth/track renderer.");
                Call(match, "SetupScreen"); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Morning") });
                var snowChoice = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Snow"); Call(catalog, "Select", Get(snowChoice, "id"));
                Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(5); Time.timeScale = 0;
                var snow = match.GetComponent(Runtime("SportsSnowSurface")); Assert.That(Get(clock, "lampSnowSurface"), Is.SameAs(snow)); Assert.That((float)Get(snow, "Coverage"), Is.GreaterThan(.5f));
                for (int i = 0; i < lamps.Length; i++) Assert.That(lamps[i].intensity, Is.EqualTo(powers[i] * .65f * .3f).Within(.1f), "Original snow coverage controls the configured covered-surface gain.");
                Capture("snow-morning");
            }
            void Capture(string label)
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/UrbanVenueDawn/" + name + "-" + label + "-" + size.y + ".png", null });
            }
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    static Color[] Render(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); var prior = camera.targetTexture; var active = RenderTexture.active; Texture2D image = null;
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
public sealed class UrbanLightBudgetDiagnosticsTests
{
    [UnityTest, Timeout(180000)] public IEnumerator CompareFareLightSelection() => new UrbanVenueLightingTests().Verify("TruckFareFight", true);
    [UnityTest, Timeout(180000)] public IEnumerator CompareSnakeLightSelection() => new UrbanVenueLightingTests().Verify("TruckSnake", true);
}
#endif
