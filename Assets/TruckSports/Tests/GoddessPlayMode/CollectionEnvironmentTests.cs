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

public sealed class CollectionEnvironmentTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);

    [UnityTest, Timeout(240000)]
    public IEnumerator CompatibleOutdoorVenuesKeepIndependentTimeAndAccessibleMaximumRosters() => VerifyVenues(false);
    [UnityTest, Timeout(150000)]
    public IEnumerator NewlyIntegratedOutdoorVenuesRetainRenderersAndMatchLifecycle() => VerifyVenues(true);
    [UnityTest, Timeout(150000)]
    public IEnumerator RemainingSharedVenuesAndGroundedFixturesRetainMatchLifecycle() => VerifyVenues(true, RemainingIntegration);
    static readonly string[] NewlyIntegrated = { "TruckDockDiving", "TruckBuzkashi", "TruckBungeeDemolition", "TruckFishing", "TruckSumo", "TruckLogRolling", "TruckTrampolineDodgeball", "TruckDemolitionDerby" };
    static readonly string[] RemainingIntegration = { "TruckCurling", "TruckSkeeBall", "TruckBungeeDemolition", "TruckFishing" };
    static bool PresentationOwned(string name) => NewlyIntegrated.Contains(name) || RemainingIntegration.Contains(name);
    IEnumerator VerifyVenues(bool newlyIntegratedOnly, string[] selection = null)
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CollectionEnvironment" });
        var isolation = new GameObject("Explicit outdoor environment regression");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; int tested = 0; var layoutErrors = new System.Collections.Generic.List<string>();
        try
        {
            string bindingGuid = AssetDatabase.AssetPathToGUID("Assets/TruckSports/Scripts/SportsMatchTime.cs");
            Assert.That(bindingGuid, Is.Not.Empty);
            // Focused builds restore the host's remembered build settings. Audit
            // the actual saved sport scenes instead of treating that menu-only
            // Editor list as the collection registry.
            var paths = Directory.GetDirectories("Assets", "Truck*").SelectMany(directory => Directory.GetFiles(directory, "*.unity", SearchOption.AllDirectories)).Select(path => path.Replace('\\', '/')).Where(path => File.ReadAllText(path).Contains("guid: " + bindingGuid)).OrderBy(path => path).ToArray();
            foreach (string path in paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.StartsWith("TruckPedestrianPlowing", StringComparison.Ordinal)) continue;
                if (newlyIntegratedOnly && !(selection ?? NewlyIntegrated).Contains(name)) continue;
                EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().SingleOrDefault();
                if (match == null) continue;
                var weather = match.GetComponent(Runtime("SportsChaosWeather"));
                if (weather == null) { match = null; continue; }
                var binding = Get(weather, "matchTime"); Assert.That(binding, Is.Not.Null, name);
                if (PresentationOwned(name))
                {
                    Assert.That(Get(match, "presentationWeather"), Is.SameAs(weather), name + " single opt-in lifecycle owner");
                    Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("SportsChaosWeather"), FindObjectsSortMode.None).Length, Is.EqualTo(1), name + " no duplicate weather");
                    Assert.That(UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBaseJumping.BaseJumpRendering, Assembly-CSharp", true), FindObjectsSortMode.None).Length, Is.EqualTo(1), name + " single render pipeline scope");
                    Assert.That(((Light[])Get(binding, "nightLights")).Length, Is.GreaterThan(0), name + " authored night venue fixtures");
                    if (name == "TruckDockDiving" || name == "TruckLogRolling") AssertRetainedWaterRenderer(name);
                    if (name == "TruckBungeeDemolition" || name == "TruckFishing") AssertGroundedFixtures(name, (Component)binding);
                    if (name == "TruckSkeeBall")
                    {
                        var floors = match.gameObject.scene.GetRootGameObjects().Where(root => root.name.StartsWith("Equivalent giant arcade Skee course ", StringComparison.Ordinal)).Select(root => root.transform.Find("Course miss floor").GetComponent<MeshRenderer>().bounds).ToArray();
                        Assert.That(floors.Select(floor => floor.max.y).Distinct().Count(), Is.EqualTo(1), "Equivalent lowered return floors");
                        foreach (Transform fixture in ((Component)binding).transform.Find("Authored night venue lamps"))
                        {
                            Assert.That(fixture.position.y, Is.EqualTo(floors[0].max.y).Within(.01f), "Skee fixture rests on the actual lowered floor, not the assembly origin");
                            Assert.That(floors.Any(floor => fixture.position.x >= floor.min.x && fixture.position.x <= floor.max.x && fixture.position.z >= floor.min.z && fixture.position.z <= floor.max.z), Is.True, "Skee fixture has a floor beneath it");
                        }
                    }
                    if (name == "TruckCurling")
                    {
                        var trucks = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventTruck"), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().ToArray();
                        Assert.That(trucks.Length, Is.EqualTo(16), "All authored circular stones retained");
                        foreach (var truck in trucks)
                        {
                            Assert.That(truck.GetComponent<BoxCollider>(), Is.Null, "Lighting must not replace Curling's circular collision footprint");
                            Assert.That(truck.GetComponent<MeshCollider>(), Is.Not.Null);
                        }
                        Assert.That(((Light[])Get(binding, "nightLights")).Length, Is.EqualTo(6), "Full sheet side lighting");
                    }
                }
                Set(Get(match, "setup"), "count", (int)Get(match, "MaximumPlayers"));
                if ((bool)Get(match, "TeamSport")) { Set(match, "teamA", 4); Set(match, "teamB", 4); }
                Call(match, "AllBots"); Call(match, "SetupScreen");
                var menus = (Component)Get(match, "menus"); Call(menus, "Dirty"); yield return null;
                var pickers = menus.GetComponentsInChildren(Runtime("SportsWeatherPicker"), true).Cast<Component>().ToArray();
                var clock = pickers.Single(picker => (bool)Get(picker, "timeOfDay"));
                var forecast = pickers.Single(picker => !(bool)Get(picker, "timeOfDay"));
                var topActions = clock.transform.parent.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>().Where(button => button.gameObject.activeInHierarchy && button.transform.parent == clock.transform.parent).ToArray();
                var heading = clock.transform.parent.Find("Title");
                var textType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true);
                var headingBox = RenderedTextRect(heading.GetComponent(textType));
                var artwork = clock.transform.parent.Find("Original sport title artwork") as RectTransform;
                if (artwork != null && headingBox.Overlaps(WorldRect(artwork))) layoutErrors.Add(name + " heading overlaps sport artwork");
                for (int i = 0; i < topActions.Length; i++)
                {
                    var box = WorldRect(topActions[i].GetComponent<RectTransform>());
                    if (headingBox.Overlaps(box)) layoutErrors.Add(name + " heading overlaps " + topActions[i].name);
                    if (PresentationOwned(name) && box.Overlaps(RenderedTextRect((Component)Get(menus, "setupInfo")))) layoutErrors.Add(name + " control-help text overlaps " + topActions[i].name);
                    var caption = (Component)Get(Get(topActions[i], "button"), "normalTextObj");
                    if (caption != null)
                    {
                        var label = RenderedTextRect(caption);
                        if (label.xMin < box.xMin - 2 || label.xMax > box.xMax + 2 || label.yMin < box.yMin - 2 || label.yMax > box.yMax + 2) layoutErrors.Add(name + " caption outside its button: " + topActions[i].name);
                    }
                    for (int j = i + 1; j < topActions.Length; j++)
                        if (box.Overlaps(WorldRect(topActions[j].GetComponent<RectTransform>()))) layoutErrors.Add(name + " top-level buttons overlap: " + topActions[i].name + " / " + topActions[j].name);
                }
                foreach (var picker in new[] { clock, forecast })
                {
                    Assert.That(picker.gameObject.activeInHierarchy, Is.True, name + " visible setup selector");
                    var box = WorldRect(picker.GetComponent<RectTransform>());
                    foreach (var button in picker.transform.parent.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>().Where(button => button.gameObject.activeInHierarchy))
                        if (box.Overlaps(WorldRect(button.GetComponent<RectTransform>()))) layoutErrors.Add(name + " selector overlaps " + button.name);
                    foreach (var row in ((GameObject[])Get(menus, "rows")).Where(row => row.activeInHierarchy))
                        if (box.Overlaps(WorldRect(row.GetComponent<RectTransform>()))) layoutErrors.Add(name + " selector overlaps " + row.name);
                }
                var catalog = Get(forecast, "catalog"); var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray();
                int clear = Array.FindIndex(entries, entry => (string)Get(entry, "displayName") == "Clear"); Assert.That(clear, Is.GreaterThanOrEqualTo(0)); Call(forecast, "Choose", clear + 1);
                foreach (string choice in new[] { "Day", "Night" })
                {
                    Call(match, "SetupScreen"); Call(clock, "Choose", (int)Enum.Parse(Runtime("SportsTimeChoice"), choice));
                    Assert.That((bool)Call(match, "StartMatch"), Is.True, name); yield return new WaitForSeconds(.3f); yield return null;
                    Assert.That(Get(binding, "Applied").ToString(), Is.EqualTo(choice), name);
                    var cloudGradient = (Gradient)Get(Get(weather, "clouds"), "GlobalAmbientMultiplierGradient");
                    Assert.That(cloudGradient.Evaluate(1).a, Is.EqualTo(choice == "Night" ? (float)Get(binding, "nightCloudAmbientMultiplier") : 1).Within(.001f), name + " cloud ambient follows the selected match time without mutating source weather profiles");
                    Assert.That(Get(weather, "PresetId"), Is.EqualTo(Get(entries[clear], "id")), name);
                    if (PresentationOwned(name))
                    {
                        int revision = (int)Get(binding, "Revision"); int seed = (int)Get(weather, "Seed");
                        Call(match, "Pause", 0, false); yield return null;
                        Assert.That((bool)Get(match, "Paused"), Is.True, name + " pauses without replacing the weather owner");
                        Call(match, "Resume"); yield return null;
                        Assert.That(Get(weather, "Seed"), Is.EqualTo(seed), name + " resume retains selected weather instance");
                        Assert.That(Get(binding, "Revision"), Is.EqualTo(revision), name + " resume keeps frozen time");
                    }
                    var headlights = match.GetComponent(Runtime("SportsNightHeadlights"));
                    if (name == "TruckHideAndSeek") Assert.That(headlights, Is.Null, "Tactical manual headlights retain their existing owner.");
                    else
                    {
                        Assert.That(headlights, Is.Not.Null, name);
                        var trucks = ((Array)Get(headlights, "trucks")).Cast<Component>().ToArray();
                        var beams = (Light[])Get(headlights, "wheelOnlyBeams");
                        for (int i = 0; i < trucks.Length; i++)
                        {
                            bool expected = choice == "Night" && trucks[i].gameObject.activeInHierarchy && ((Rigidbody)Get(trucks[i], "body")).detectCollisions;
                            var motor = trucks[i].GetType().GetField("tractorMotor").GetValue(trucks[i]);
                            if (motor != null)
                            {
                                var manager = Get(Get(Get(motor, "vehicle"), "effectsManager"), "lightsManager");
                                Assert.That((bool)Get(Get(manager, "lowBeamLights"), "On"), Is.EqualTo(expected), name + " NWH headlight " + i);
                            }
                            else if (beams[i] != null) Assert.That(beams[i].enabled, Is.EqualTo(expected), name + " wheel-only authored beam " + i);
                        }
                    }
                    var vendor = (Component)Get(weather, "vendor");
                    var sky = vendor.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().Name == "WeatherMakerSkySphereScript");
                    var profile = Get(sky, "SkySphereProfile");
                    Assert.That(Get(profile, "NightTexture"), Is.Not.Null, name + " authored vendor star texture");
                    Assert.That((float)Get(profile, "NightIntensity"), Is.GreaterThan(.1f), name);
                    var camera = vendor.GetType().GetField("AllowCameras").GetValue(vendor) as System.Collections.IEnumerable;
                    if (PresentationOwned(name))
                    {
                        var allowed = camera.Cast<Camera>().ToArray();
                        foreach (var rig in UnityEngine.Object.FindObjectsByType(Runtime("SportsEventCamera"), FindObjectsSortMode.None).Cast<Component>())
                            Assert.That(allowed, Does.Contain((Camera)Get(rig, "view")), name + " includes authored spectator, replay and driving camera " + rig.name);
                    }
                    var view = camera.Cast<Camera>().First(item => item != null && item.enabled);
                    var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGBHalf); var prior = view.targetTexture;
                    try { view.targetTexture = target; view.Render(); }
                    finally { view.targetTexture = prior; RenderTexture.ReleaseTemporary(target); }
                    Assert.That(Shader.GetGlobalFloat("_NightIntensity"), Is.EqualTo((float)Get(profile, "NightIntensity")).Within(.001f), name);
                }
                tested++; TestContext.WriteLine("SAVED_OUTDOOR_TIME " + name + " maxRoster=" + Get(Get(match, "setup"), "count"));
                Call(match, "SetupScreen"); match = null;
            }
            Assert.That(tested, Is.EqualTo(newlyIntegratedOnly ? (selection ?? NewlyIntegrated).Length : 45), "Actual compatible saved outdoor sessions; legacy/indoor/Plowing exceptions are accounted separately.");
            Assert.That(layoutErrors, Is.Empty, string.Join("\n", layoutErrors));
        }
        finally { if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }
    static void AssertGroundedFixtures(string name, Component binding)
    {
        string prefix = name == "TruckBungeeDemolition" ? "Independent demolition bay" : "Equivalent independent river";
        float ground = binding.gameObject.scene.GetRootGameObjects().Where(root => root.name.StartsWith(prefix, StringComparison.Ordinal)).Select(root => root.transform.position.y).Distinct().Single();
        var fixtures = binding.transform.Find("Authored night venue lamps");
        Assert.That(fixtures, Is.Not.Null);
        foreach (Transform fixture in fixtures)
            Assert.That(fixture.position.y, Is.EqualTo(ground).Within(.01f), name + " fixture base rests at authored ground, not tree/tower bounds");
    }
    static void AssertRetainedWaterRenderer(string name)
    {
        string sourcePath = name == "TruckDockDiving" ? "Assets/TruckSports/Rendering/Dock local water pipeline.asset" : "Assets/TruckBogSnorkelling/Rendering/Bog pipeline.asset";
        var original = AssetDatabase.LoadMainAssetAtPath(sourcePath);
        var scope = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBaseJumping.BaseJumpRendering, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
        var current = (UnityEngine.Object)Get(scope, "pipeline");
        string[] Features(UnityEngine.Object pipeline)
        {
            var serialized = new SerializedObject(pipeline); int index = serialized.FindProperty("m_DefaultRendererIndex").intValue;
            var data = serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(index).objectReferenceValue;
            return ((System.Collections.IEnumerable)Get(data, "rendererFeatures")).Cast<UnityEngine.Object>().Select(feature => feature.GetType().FullName).ToArray();
        }
        Assert.That(AssetDatabase.GetAssetPath(current), Is.EqualTo("Assets/" + name + "/Rendering/Weather pipeline.asset"), name + " owns cloned renderer configuration");
        var before = Features(original); var after = Features(current);
        Assert.That(before, Is.Not.Empty, name + " original water feature baseline");
        foreach (string feature in before) Assert.That(after, Does.Contain(feature), name + " retained " + feature);
        Assert.That(after, Does.Contain("DigitalRuby.WeatherMaker.WeatherMakerURPRenderFeatureScript"), name + " vendor weather added beside water");
    }
    static Rect WorldRect(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners.Min(corner => corner.x), corners.Min(corner => corner.y), corners.Max(corner => corner.x), corners.Max(corner => corner.y));
    }
    static Rect RenderedTextRect(Component text)
    {
        text.GetType().GetMethod("ForceMeshUpdate", new[] { typeof(bool), typeof(bool) }).Invoke(text, new object[] { true, true });
        var bounds = (Bounds)Get(text, "textBounds");
        var corners = new[] { bounds.min, bounds.max, new Vector3(bounds.min.x, bounds.max.y), new Vector3(bounds.max.x, bounds.min.y) }.Select(point => text.transform.TransformPoint(point)).ToArray();
        return Rect.MinMaxRect(corners.Min(point => point.x), corners.Min(point => point.y), corners.Max(point => point.x), corners.Max(point => point.y));
    }
}
#endif
