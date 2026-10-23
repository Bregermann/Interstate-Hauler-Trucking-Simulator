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

public sealed class LegacyMatchTimeTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object value, string name) => value.GetType().GetField(name, Any)?.GetValue(value) ?? value.GetType().GetProperty(name, Any).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(value, args);
    static void Select(string choice) => Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new[] { Enum.Parse(Runtime("SportsTimeChoice"), choice) });
    static void Setup(Component match, bool modern) => Call(match, modern ? "SetupScreen" : "ChangeSetup");

    [UnityTest, Timeout(180000)]
    public IEnumerator ExistingLegacyMatchesLatchVendorTimeAcrossAttemptsAndRematches()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "LegacyMatchTime" });
        var isolation = new GameObject("Explicit legacy clock regression"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; bool modern = false;
        try
        {
            foreach (string name in new[] { "TruckCornhole", "TruckCheeseRolling", "TruckBaseJumping" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                string className = name == "TruckCornhole" ? "CornholeMatch" : name == "TruckCheeseRolling" ? "CheeseMatch" : "BaseJumpMatch";
                match = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(component => component.GetType().Name == className);
                modern = name == "TruckBaseJumping";
                var binding = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsMatchTime")); Assert.That(binding, Is.Not.Null, name);
                var clock = ((Component)Get(match, "menus")).GetComponentsInChildren(Runtime("SportsWeatherPicker"), true).Cast<Component>().Single(picker => (bool)Get(picker, "timeOfDay"));
                Assert.That(clock.gameObject.activeInHierarchy, Is.True, name + " saved Heat time control");
                var labels = clock.transform.parent.GetComponentsInChildren(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true), true).Cast<Component>();
                Assert.That(labels.Any(label => label.gameObject.activeInHierarchy && (string)Get(label, "text") == "TIME OF DAY"), Is.True);
                var setup = Get(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(match, modern ? "AllBots" : "AllAI");
                var pipeline = new SerializedObject(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
                int rendererIndex = pipeline.FindProperty("m_DefaultRendererIndex").intValue;
                var renderer = pipeline.FindProperty("m_RendererDataList").GetArrayElementAtIndex(rendererIndex).objectReferenceValue;
                var features = ((IEnumerable)Get(renderer, "rendererFeatures")).Cast<UnityEngine.Object>().ToArray();
                Assert.That(features.Any(feature => feature.GetType().Name == "WeatherMakerURPRenderFeatureScript"), Is.True, name + " active pipeline contains the actual vendor weather pass");
                if (name == "TruckCheeseRolling") Assert.That(features.Any(feature => feature.GetType().Name == "TruckTaxiMushroomWaveFeature"), Is.True, "Cheese's existing mushroom look remains alongside weather");
                var cycle = Get(binding, "cycle"); var source = Get(cycle, "DayNightProfile"); var originalHour = Get(source, "TimeOfDay");
                var sphere = binding.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Single(component => component != null && component.GetType().Name == "WeatherMakerSkySphereScript");
                var horizon = (Gradient)Get(Get(sphere, "SkySphereProfile"), "SkyGroundGradient");
                Assert.That(horizon.Evaluate(0).a, Is.EqualTo(1), name + " vendor ground masks stars below the horizon at night");
                Assert.That(horizon.Evaluate(1).a, Is.EqualTo(1), name + " vendor ground is retained in daylight");
                string previousId = null;
                foreach (string choice in new[] { "Morning", "Day", "Evening", "Night", "Random" })
                {
                    Setup(match, modern); Select(choice); Assert.That((bool)Call(match, "StartMatch"), Is.True, name);
                    yield return null; yield return new WaitForSeconds(.25f);
                    string id = (string)Get(match, "MatchId"); Assert.That(id, Is.Not.Null.And.Not.Empty); Assert.That(id, Is.Not.EqualTo(previousId)); previousId = id;
                    var applied = Get(binding, "Applied"); Assert.That(applied.ToString(), Is.Not.EqualTo("Random"));
                    if (choice != "Random") Assert.That(applied.ToString(), Is.EqualTo(choice));
                    var actualProfile = Get(cycle, "DayNightProfile"); Assert.That(actualProfile, Is.Not.SameAs(source));
                    Assert.That((float)Get(actualProfile, "Speed"), Is.Zero); Assert.That((float)Get(actualProfile, "NightSpeed"), Is.Zero);
                    float hour = (float)Get(cycle, "TimeOfDay"); int revision = (int)Get(binding, "Revision");
                    var camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(view => view.enabled && view.cameraType == CameraType.Game);
                    Render(camera); yield return null;
                    var appliedGround = (Gradient)Get(Get(sphere, "SkySphereProfile"), "SkyGroundGradient");
                    Assert.That(appliedGround.Evaluate(0), Is.EqualTo(appliedGround.Evaluate(1)), name + " inactive sun lookup cannot substitute daylight ground tint");
                    if (choice == "Night") Assert.That(appliedGround.Evaluate(1).maxColorComponent, Is.LessThan(.03f), "Night lower hemisphere uses the selected dark ground color");
                    Assert.That(RenderSettings.skybox.shader.name, Is.EqualTo("LWS/TruckSports/Weather Maker Viewport Skybox"), name + " actual runtime sky owner");
                    Assert.That(((Light)Get(binding, "daylightFill")).enabled, Is.False, name + " fixed noon light disabled");
                    Assert.That(Get(source, "TimeOfDay"), Is.EqualTo(originalHour), name + " source profile unchanged");
                    // Invoke the actual existing per-round weather owner with a new seed.
                    // A heat or forecast refresh must never reroll this match's time.
                    if (name == "TruckCheeseRolling") Call(Get(match, "weather"), "Select", 31337);
                    else if (modern) Call(Get(match, "weatherField"), "Select", 1, 31337);
                    else
                    {
                        var weather = Get(match, "atmosphere"); var catalog = Get(match, "weatherPresets");
                        Call(weather, "ConfigurePreset", Call(catalog, "Resolve", 31337, false), 31337);
                    }
                    yield return null; Render(camera);
                    Assert.That(Get(binding, "Revision"), Is.EqualTo(revision)); Assert.That(Get(binding, "Applied"), Is.EqualTo(applied));
                    Assert.That((float)Get(cycle, "TimeOfDay"), Is.EqualTo(hour).Within(.01f));
                    Assert.That((string)Get(match, "MatchId"), Is.EqualTo(id));
                    TestContext.WriteLine(name + " " + choice + " => " + applied + " match=" + id + " seconds=" + hour);
                }
                Setup(match, modern); match = null;
            }
        }
        finally { if (match != null) Setup(match, modern); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }
    static void Render(Camera camera)
    {
        var old = camera.targetTexture; var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGBHalf);
        try { camera.targetTexture = target; camera.Render(); }
        finally { camera.targetTexture = old; RenderTexture.ReleaseTemporary(target); }
    }
}
#endif
