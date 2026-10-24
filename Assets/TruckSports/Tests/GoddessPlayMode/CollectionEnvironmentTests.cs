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
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);

    [UnityTest, Timeout(240000)]
    public IEnumerator CompatibleOutdoorVenuesKeepIndependentTimeAndAccessibleMaximumRosters() => VerifyVenues(false);
    [UnityTest, Timeout(150000)]
    public IEnumerator NewlyIntegratedOutdoorVenuesRetainRenderersAndMatchLifecycle() => VerifyVenues(true);
    [UnityTest, Timeout(150000)]
    public IEnumerator RemainingSharedVenuesAndGroundedFixturesRetainMatchLifecycle() => VerifyVenues(true, RemainingIntegration);
    [UnityTest, Timeout(90000)]
    public IEnumerator MusicalParkingOutdoorTimeKeepsMaximumRosterAndSavedSetup() => VerifyVenues(true, new[] { "TruckMusicalParking" });

    [UnityTest, Timeout(240000)]
    public IEnumerator MusicalParkingActualForecastSnowMusicAndEliminationRoundPersist()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ParkingEnvironment" });
        var isolation = new GameObject("Explicit actual Parking environment fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var frame = isolation.AddComponent<ParkingEnvironmentLateCapture>();
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); Set(Get(match, "setup"), "count", 8); Call(match, "AllBots");
            var weather = match.GetComponent(Runtime("SportsChaosWeather")); var binding = match.GetComponent(Runtime("SportsMatchTime")); var snow = match.GetComponent(Runtime("SportsSnowSurface"));
            Assert.That(weather, Is.Not.Null); Assert.That(binding, Is.Not.Null); Assert.That(snow, Is.Not.Null);
            Assert.That(Get(match, "presentationWeather"), Is.SameAs(weather)); Assert.That(Get(snow, "weather"), Is.SameAs(weather));
            Assert.That(((Light[])Get(binding, "nightLights")).Length, Is.EqualTo(6), "Retain the six original authored lamp pools before any visually justified tuning.");
            var menus = (Component)Get(match, "menus"); var pickers = menus.GetComponentsInChildren(Runtime("SportsWeatherPicker"), true).Cast<Component>().ToArray();
            var forecast = pickers.Single(p => !(bool)Get(p, "timeOfDay")); var clock = pickers.Single(p => (bool)Get(p, "timeOfDay"));
            var entries = ((Array)Get(Get(forecast, "catalog"), "entries")).Cast<object>().ToArray();
            var capture = Runtime("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5);
            foreach (string condition in new[] { "Clear|Day", "Rain|Evening", "Snow|Night", "Blizzard|Morning" })
            {
                string[] choice = condition.Split('|'); Call(match, "SetupScreen");
                int selected = Array.FindIndex(entries, entry => (string)Get(entry, "displayName") == choice[0]); Assert.That(selected, Is.GreaterThanOrEqualTo(0));
                SelectVisiblePicker(forecast, selected + 1); SelectVisiblePicker(clock, (int)Enum.Parse(Runtime("SportsTimeChoice"), choice[1]));
                yield return new WaitForSecondsRealtime(.35f); yield return null;
                Assert.That(Get(Get(Get(forecast, "dropdown"), "headerText"), "text"), Is.EqualTo(choice[0]));
                Assert.That(Get(Get(Get(clock, "dropdown"), "headerText"), "text"), Is.EqualTo(choice[1]));
                frame.capture = () => { foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) })
                    capture.Invoke(null, new object[] { match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/ParkingEnvironment/"+choice[0]+"-"+choice[1]+"-setup-"+size.y+".png",null }); };
                while (frame.capture != null) yield return null; Assert.That(frame.error, Is.Null);
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(.4f); yield return null;
                Assert.That(Get(weather, "PresetId"), Is.EqualTo(Get(entries[selected], "id"))); Assert.That(Get(binding, "Applied").ToString(), Is.EqualTo(choice[1]));
                Assert.That((bool)Get(snow, "Falling"), Is.EqualTo(choice[0] == "Snow" || choice[0] == "Blizzard"));
                if ((bool)Get(snow, "Falling")) Assert.That((float)Get(snow, "Coverage"), Is.GreaterThanOrEqualTo(.68f), "Snow begins covered, not bare.");
                bool lamps = choice[1] == "Night" || choice[1] == "Evening";
                Assert.That(((Light[])Get(binding, "nightLights")).All(light => (light.intensity > 0) == lamps), Is.True);
                float deadline = Time.realtimeSinceStartup + 7;
                while (Get(match, "Phase").ToString() == "Countdown" && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("Music")); yield return new WaitForSeconds(1);
                var music = (AudioSource)Get(match, "music"); Assert.That(music.isPlaying, Is.True, "Original music phase still plays; no listening claim.");
                var trucks = ((Array)Get(match, "trucks")).Cast<Component>().ToArray();
                Assert.That(trucks.Any(truck => ((Rigidbody)Get(truck, "body")).linearVelocity.magnitude > .5f), Is.True, "Actual NWH AI circulates without injecting a score or result.");
                if ((bool)Get(snow, "Falling")) Assert.That((int)Get(snow, "Contacts"), Is.GreaterThan(0), "Snow traces use actual supported NWH wheel contacts.");
                int revision = (int)Get(binding, "Revision"), seed = (int)Get(weather, "Seed");
                Call(match, "Pause",0,false); int sample = music.timeSamples; var pose = ((Rigidbody)Get(trucks[0], "body")).position; yield return new WaitForSecondsRealtime(.2f);
                Assert.That(((Rigidbody)Get(trucks[0], "body")).position, Is.EqualTo(pose)); Assert.That(music.timeSamples, Is.EqualTo(sample)); Call(match, "Resume"); yield return null;
                Assert.That(Get(binding, "Revision"), Is.EqualTo(revision)); Assert.That(Get(weather, "Seed"), Is.EqualTo(seed));
                if (choice[0] == "Snow")
                {
                    Time.timeScale = 4; deadline = Time.realtimeSinceStartup + 60;
                    while ((int)Get(match, "Round") < 2 && Time.realtimeSinceStartup < deadline) yield return null;
                    Time.timeScale = 1;
                    Assert.That((int)Get(match, "Round"), Is.GreaterThanOrEqualTo(2), "An actual AI parking round completes; no forced eliminations.");
                    Assert.That((int)Get(match, "TotalCaptures"), Is.GreaterThan(0));
                    Assert.That(Get(binding, "Revision"), Is.EqualTo(revision)); Assert.That(Get(weather, "Seed"), Is.EqualTo(seed), "Elimination does not reroll or stop weather.");
                    Assert.That(Get(weather, "PresetId"), Is.EqualTo(Get(entries[selected], "id")));
                }
                yield return new WaitForSecondsRealtime(.2f); yield return null;
                frame.capture = () => { foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) })
                    capture.Invoke(null, new object[] { match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/ParkingEnvironment/"+choice[0]+"-"+choice[1]+"-actual-play-"+size.y+".png",null }); };
                while (frame.capture != null) yield return null; Assert.That(frame.error, Is.Null);
                Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
                Assert.That((int)Get(match, "Round"), Is.EqualTo(1)); Assert.That((int)Get(match, "TotalCaptures"), Is.Zero);
                Assert.That(Get(weather, "PresetId"), Is.EqualTo(Get(entries[selected], "id"))); Assert.That(Get(binding, "Applied").ToString(), Is.EqualTo(choice[1]));
                TestContext.WriteLine("PARKING_ACTUAL_ENVIRONMENT "+condition+": real8AI start/music/NWH contacts, pause, explicit forecast/time, snow and rematch. Controlled captures, not Windows/human/device/listening/performance acceptance.");
            }
        }
        finally { Time.timeScale = 1; if (match != null) Call(match,"SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); }
    }

    static readonly string[] NewlyIntegrated = { "TruckDockDiving", "TruckBuzkashi", "TruckBungeeDemolition", "TruckFishing", "TruckSumo", "TruckLogRolling", "TruckTrampolineDodgeball", "TruckDemolitionDerby" };
    static readonly string[] RemainingIntegration = { "TruckCurling", "TruckSkeeBall", "TruckBungeeDemolition", "TruckFishing" };
    static void SelectVisiblePicker(Component picker, int index) { var dropdown=Get(picker,"dropdown"); Call(dropdown,"SetDropdownIndex",index); var changed=Get(dropdown,"onValueChanged"); changed.GetType().GetMethod("Invoke",new[] { typeof(int) }).Invoke(changed,new object[] { index }); }

    static bool PresentationOwned(string name) => NewlyIntegrated.Contains(name) || RemainingIntegration.Contains(name) || name == "TruckMusicalParking";
    [UnityTest, Timeout(240000)]
    public IEnumerator LegacyForecastCatalogUsesEveryInstalledPresetAndRetainsMatchChoice()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "LegacyForecastCatalog" });
        var isolation = new GameObject("Explicit legacy forecast catalog regression"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; bool modern = false;
        try
        {
            foreach (string name in new[] { "TruckCheeseRolling", "TruckBaseJumping" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                modern = name == "TruckBaseJumping";
                var type = Type.GetType(modern ? "LWS.TruckBaseJumping.BaseJumpMatch, Assembly-CSharp" : "LWS.TruckCheeseRolling.CheeseMatch, Assembly-CSharp", true);
                match = (Component)UnityEngine.Object.FindAnyObjectByType(type);
                var setup = Get(match, "setup"); Set(setup, "count", 4); Call(match, modern ? "AllBots" : "AllAI");
                var legacy = (Component)Get(match, modern ? "weatherField" : "weather"); var presentation = (Component)Get(legacy, "presentation");
                Assert.That(presentation, Is.Not.Null); Assert.That(Get(legacy, "vendor"), Is.SameAs(Get(presentation, "vendor")), "Reuse the original single vendor world");
                Assert.That(UnityEngine.Object.FindObjectsByType(((Component)Get(presentation, "vendor")).GetType(), FindObjectsSortMode.None).Length, Is.EqualTo(1));
                var snow = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsSnowSurface")); Assert.That(Get(snow, "weather"), Is.SameAs(presentation), "Accumulation follows the selected visual forecast rather than the old wind option");
                var pickers = ((Component)Get(match, "menus")).GetComponentsInChildren(Runtime("SportsWeatherPicker"), true).Cast<Component>().ToArray();
                Assert.That(pickers.Length, Is.EqualTo(2));
                var forecast = pickers.Single(picker => !(bool)Get(picker, "timeOfDay")); var clock = pickers.Single(picker => (bool)Get(picker, "timeOfDay"));
                Assert.That(forecast.gameObject.activeInHierarchy && clock.gameObject.activeInHierarchy, Is.True);
                Assert.That(WorldRect(forecast.GetComponent<RectTransform>()).Overlaps(WorldRect(clock.GetComponent<RectTransform>())), Is.False);
                foreach (var picker in pickers)
                foreach (var action in picker.transform.parent.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>().Where(action => action.gameObject.activeInHierarchy))
                    Assert.That(WorldRect(picker.GetComponent<RectTransform>()).Overlaps(WorldRect(action.GetComponent<RectTransform>())), Is.False, name + " selector covers " + action.name);
                var entries = ((Array)Get(Get(forecast, "catalog"), "entries")).Cast<object>().ToArray(); Assert.That(entries.Length, Is.GreaterThan(3));
                Call(clock, "Choose", (int)Enum.Parse(Runtime("SportsTimeChoice"), "Day"));
                for (int entry = 0; entry < entries.Length; entry++)
                {
                    Call(forecast, "Choose", entry + 1); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(.12f); yield return null;
                    Assert.That(Get(presentation, "PresetId"), Is.EqualTo(Get(entries[entry], "id")), name + " installed " + Get(entries[entry], "displayName"));
                    string kind = Get(presentation, "Kind").ToString(); Assert.That((bool)Get(snow, "Falling"), Is.EqualTo(kind == "Snow" || kind == "Blizzard"));
                    var binding = Get(presentation, "matchTime"); int revision = (int)Get(binding, "Revision"); int seed = (int)Get(presentation, "Seed");
                    var profile = Get(Get(presentation, "vendor"), "LastLocalProfile");
                    for (int refresh = 0; refresh < 3; refresh++)
                    {
                        if (modern) Call(legacy, "Select", refresh, seed + 997 * (refresh + 1)); else Call(legacy, "Select", seed + refresh + 1);
                        yield return null;
                        Assert.That(Get(presentation, "PresetId"), Is.EqualTo(Get(entries[entry], "id")));
                        Assert.That(Get(presentation, "Seed"), Is.EqualTo(seed), "Round refresh must not reroll the match forecast");
                        Assert.That(Get(Get(presentation, "vendor"), "LastLocalProfile"), Is.SameAs(profile)); Assert.That(Get(binding, "Revision"), Is.EqualTo(revision));
                    }
                    Call(match, modern ? "SetupScreen" : "ChangeSetup"); yield return null;
                }
                Call(forecast, "Choose", 0); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
                string previousMatch = (string)Get(match, "MatchId"); var previousProfile = Get(Get(presentation, "vendor"), "LastLocalProfile");
                Call(match, modern ? "SetupScreen" : "ChangeSetup"); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
                Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(previousMatch)); Assert.That(Get(Get(presentation, "vendor"), "LastLocalProfile"), Is.Not.SameAs(previousProfile), "Random resolves for the new match; identical choices remain legal");
                TestContext.WriteLine("LEGACY_FULL_FORECASTS " + name + " installed=" + entries.Length + " roundRefreshes=" + (entries.Length * 3));
                Call(match, modern ? "SetupScreen" : "ChangeSetup"); match = null;
            }
        }
        finally { if (match != null) Call(match, modern ? "SetupScreen" : "ChangeSetup"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }
    [UnityTest, Timeout(150000)]
    public IEnumerator LegacyCourtsKeepTheirRulesAndUseOneSavedWeatherClock()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "LegacyCourtEnvironment" });
        var isolation = new GameObject("Explicit legacy court environment regression"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            foreach (string name in new[] { "TruckHockey", "TruckWallball" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                bool hockey = name == "TruckHockey";
                var type = Type.GetType(hockey ? "LWS.TruckHockey.HockeyMatch, Assembly-CSharp" : "LWS.TruckWallball.WallballMatch, Assembly-CSharp", true);
                match = (Component)UnityEngine.Object.FindAnyObjectByType(type);
                if (!hockey) { Set(match, "blueSize", 4); Set(match, "orangeSize", 4); }
                var mode = Enum.Parse(match.GetType().GetField("mode").FieldType, "AIVsAI");
                if (hockey) { Set(match, "mode", mode); Call(match, "Configure"); } else Call(match, "Configure", mode);
                Assert.That((bool)Get(match, hockey ? "AIOnly" : "AllAI"), Is.True, name + " actual all-AI ownership");
                var weather = match.GetComponent(Runtime("SportsChaosWeather")); Assert.That(weather, Is.Not.Null, name);
                Assert.That(Get(weather, "alternateSession"), Is.SameAs(match)); Assert.That(Get(weather, "session"), Is.Null, "No replacement SportsEventSession");
                Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("SportsChaosWeather"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBaseJumping.BaseJumpRendering, Assembly-CSharp", true), FindObjectsSortMode.None).Length, Is.EqualTo(1));
                var binding = (Component)Get(weather, "matchTime"); var menus = (Component)Get(match, "menus");
                var pickers = menus.GetComponentsInChildren(Runtime("SportsWeatherPicker"), true).Cast<Component>().ToArray();
                var clock = pickers.Single(picker => (bool)Get(picker, "timeOfDay")); var forecast = pickers.Single(picker => !(bool)Get(picker, "timeOfDay"));
                var entries = ((Array)Get(Get(forecast, "catalog"), "entries")).Cast<object>().ToArray();
                foreach (var picker in pickers)
                {
                    Assert.That(picker.gameObject.activeInHierarchy, Is.True, name + " saved Heat setup selector");
                    var box = WorldRect(picker.GetComponent<RectTransform>());
                    foreach (var button in picker.transform.parent.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>().Where(button => button.gameObject.activeInHierarchy))
                        Assert.That(box.Overlaps(WorldRect(button.GetComponent<RectTransform>())), Is.False, name + " selector must not cover " + button.name);
                }
                string priorMatch = null;
                foreach (var condition in new[] { "Clear|Day", "Rain|Evening", "Snow|Morning", "Blizzard|Night" })
                {
                    string[] choice = condition.Split('|');
                    int entry = Array.FindIndex(entries, value => (string)Get(value, "displayName") == choice[0]); Assert.That(entry, Is.GreaterThanOrEqualTo(0));
                    Call(forecast, "Choose", entry + 1); Call(clock, "Choose", (int)Enum.Parse(Runtime("SportsTimeChoice"), choice[1]));
                    Assert.That((bool)Call(match, "StartMatch", false), Is.True); yield return new WaitForSeconds(.4f); yield return null;
                    string matchId = (string)Get(match, "WeatherMatchId"); Assert.That(matchId, Is.Not.Empty.And.Not.EqualTo(priorMatch)); priorMatch = matchId;
                    Assert.That(Get(binding, "Applied").ToString(), Is.EqualTo(choice[1]), name + " actual vendor time");
                    Assert.That(Get(weather, "PresetId"), Is.EqualTo(Get(entries[entry], "id")), name + " actual selected profile");
                    Assert.That(((Light)Get(binding, "daylightFill")).enabled, Is.False, "Single vendor outdoor sun/moon");
                    Assert.That(Convert.ToSingle(Get(Get(binding, "cycle"), "Speed")), Is.Zero); Assert.That(Convert.ToSingle(Get(Get(binding, "cycle"), "NightSpeed")), Is.Zero);
                    Assert.That(((Light[])Get(binding, "nightLights")).Length, Is.EqualTo(4));
                    bool night = choice[1] == "Night" || choice[1] == "Evening";
                    Assert.That(((Light[])Get(binding, "nightLights")).All(light => (light.intensity > 0) == night), Is.True);
                    var headlights = match.GetComponent(Runtime("SportsNightHeadlights")); Assert.That(Get(headlights, "alternateSession"), Is.SameAs(match));
                    var beams = ((Array)Get(headlights, "beams")).Cast<object>().ToArray(); Assert.That(beams.Length, Is.EqualTo(hockey ? 6 : 8));
                    Assert.That(beams.All(beam => ((Light)Get(beam, "light")).enabled == night), Is.True, name + " existing court truck lights");
                    int revision = (int)Get(binding, "Revision"); string preset = (string)Get(weather, "PresetId");
                    Call(match, "Pause", false); yield return null;
                    var points = ((int[])Get(Get(match, "rules"), "Score")).ToArray(); yield return new WaitForSecondsRealtime(.1f);
                    CollectionAssert.AreEqual(points, (int[])Get(Get(match, "rules"), "Score"), "Weather pause cannot change points");
                    Call(match, "Resume"); yield return null;
                    Assert.That(Get(binding, "Revision"), Is.EqualTo(revision)); Assert.That(Get(weather, "PresetId"), Is.EqualTo(preset));
                    Call(match, "ChangeSetup"); yield return null;
                }
                TestContext.WriteLine("LEGACY_COURT_TIME " + name + " four actual forecast/time starts with maximum AI roster"); match = null;
            }
        }
        finally { if (match != null) Call(match, "ChangeSetup"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }
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
            Assert.That(tested, Is.EqualTo(newlyIntegratedOnly ? (selection ?? NewlyIntegrated).Length : 47), "Actual compatible saved outdoor sessions; legacy/indoor/Plowing exceptions are accounted separately.");
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
[DefaultExecutionOrder(30000)]
public sealed class ParkingEnvironmentLateCapture:MonoBehaviour
{
    public Action capture;public string error;
    void LateUpdate(){var next=capture;capture=null;try{next?.Invoke();}catch(Exception failure){error=failure.ToString();}}
}
#endif
