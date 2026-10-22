#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class CameraLayoutDiagnosisTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length && method.GetParameters().Select((parameter, index) => values[index] == null || parameter.ParameterType.IsInstanceOfType(values[index])).All(ok => ok)).Invoke(owner, values);
    static Component Find(string name) => UnityEngine.Object.FindObjectsByType(Runtime(name), FindObjectsSortMode.None).Cast<Component>().Single();

    [UnityTest, Timeout(60000)]
    public IEnumerator TargetOwnedCardsFollowRematchLayoutsAfterRenderedLateUpdate()
    {
        var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "TargetOwnedCards" });
        var original = InputSystem.settings; InputSettings temporary = null; Keyboard keyboard = null; Component match = null;
        var isolation = new GameObject("Explicit camera ownership regression"); UnityEngine.Object.DontDestroyOnLoad(isolation);
        isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        try
        {
            temporary = UnityEngine.Object.Instantiate(original); temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = temporary; keyboard = InputSystem.AddDevice<Keyboard>();
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; match = Find("LWS.TruckTargetLaunch.TargetMatch");
            var setup = Get(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); var players = (Array)Get(setup, "players");
            foreach (int humans in new[] { 0, 1, 2, 3, 4 }) foreach (bool ai in new[] { false, true })
            {
                Call(match, "SetupScreen"); Call(match, "AllBots");
                for (int slot = 4 - humans; slot < 4; slot++)
                {
                    var player = players.GetValue(slot); var source = player.GetType().GetField("source"); source.SetValue(player, Enum.Parse(source.FieldType, "Keyboard"));
                }
                Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { ai });
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(.06f);
                Call(match, "Pause", 0, false); var menus = Get(match, "menus"); Call(menus, "Show", Enum.Parse(Runtime("LWS.TruckSports.SportsEventScreen"), "Play"));
                yield return null;
                var panels = (Array)Get(menus, "viewPanels"); int visible = (int)Get(match, "HumanViews"); var owners = (int[])Get(match, "Owners");
                string Snapshot(int view)
                {
                    var root = ((Transform)panels.GetValue(view)).Find("Compact owned viewport score");
                    var name = root?.Find("Participant name")?.GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true));
                    return "view=" + view + " owner=" + owners[view] + " rootActive=" + (root != null && root.gameObject.activeInHierarchy) + " name=" + (name != null ? Get(name, "text") : "missing");
                }
                TestContext.WriteLine("BEFORE RENDER humans=" + humans + " ai=" + ai + " " + string.Join(";", Enumerable.Range(0, visible).Select(Snapshot)));
                // Batchmode cannot yield WaitForEndOfFrame. Advancing two
                // normal frames lets the preceding LateUpdate publish cards.
                yield return null; yield return null;
                for (int view = 0; view < visible; view++)
                {
                    var panel = (Transform)panels.GetValue(view); var root = panel.Find("Compact owned viewport score");
                    Assert.That(root != null && root.gameObject.activeInHierarchy, Is.True, Snapshot(view));
                    var name = root.Find("Participant name").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true));
                    Assert.That((string)Get(name, "text"), Is.EqualTo((string)Call(match, "ViewportCardLabel", view, owners[view])), Snapshot(view));
                }
                TestContext.WriteLine("AFTER RENDER humans=" + humans + " ai=" + ai + " " + string.Join(";", Enumerable.Range(0, visible).Select(Snapshot)));
            }
        }
        finally
        {
            if (match != null) Call(match, "SetupScreen");
            InputSystem.settings = original; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1;
        }
    }

    [UnityTest, Timeout(60000)]
    public IEnumerator MinefieldSavedSkyboxSurvivesWeatherAndRematches()
    {
        var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "MinefieldSavedSkybox" });
        var original = InputSystem.settings; InputSettings temporary = null; Keyboard keyboard = null; Component match = null;
        var isolation = new GameObject("Explicit saved sky coverage regression"); UnityEngine.Object.DontDestroyOnLoad(isolation);
        isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        try
        {
            temporary = UnityEngine.Object.Instantiate(original); temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = temporary; keyboard = InputSystem.AddDevice<Keyboard>();
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMinefield/Scenes/TruckMinefield.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; match = Find("LWS.TruckMinefield.MinefieldMatch");
            var sky = RenderSettings.skybox; Assert.That(sky, Is.Not.Null); Assert.That(sky.shader.name, Is.EqualTo("LWS/TruckSports/Weather Maker Viewport Skybox"));
            Assert.That(UnityEditor.ShaderUtil.GetShaderMessages(sky.shader).Any(message => message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error), Is.False);
            var weather = Find("LWS.TruckSports.SportsChaosWeather"); var clouds = Get(weather, "clouds"); var catalog = Get(weather, "presets");
            var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray(); var setup = Get(match, "setup"); var players = (Array)Get(setup, "players");
            foreach (string label in new[] { "Clear", "Heavy Rain", "Snow", "Blizzard", "Hurricane" })
            {
                Call(match, "SetupScreen"); Call(match, "AllBots"); var entry = entries.Single(item => (string)Get(item, "displayName") == label); Call(catalog, "Select", Get(entry, "id"));
                for (int slot = 4; slot < 8; slot++) { var player = players.GetValue(slot); var source = player.GetType().GetField("source"); source.SetValue(player, Enum.Parse(source.FieldType, "Keyboard")); }
                Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { false });
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(.35f);
                Assert.That((string)Get(weather, "PresetId"), Is.EqualTo((string)Get(entry, "id")), label);
                Assert.That((CameraEvent)Get(clouds, "RenderQueue"), Is.EqualTo(CameraEvent.AfterSkybox));
                var cameras = (Camera[])Get(match, "cameras"); Assert.That(cameras.Length, Is.EqualTo(4));
                Assert.That(cameras.All(camera => camera.clearFlags == CameraClearFlags.Skybox), Is.True, label);
                Assert.That(RenderSettings.skybox, Is.SameAs(sky)); Call(match, "Pause", 0, false); yield return null;
                TestContext.WriteLine("SAVED SKY REMATCH: " + label + ", four human cameras, vendor AfterSkybox, stable material and authoritative selected preset.");
            }
            Call(match, "SetupScreen"); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
            var spectator = (Camera[])Get(match, "cameras"); Assert.That(spectator.Length, Is.EqualTo(1)); Assert.That(spectator[0].clearFlags, Is.EqualTo(CameraClearFlags.Skybox));
            Runtime("LWS.TruckSports.SportsNavigation").GetMethod("FrontEnd").Invoke(null, new object[] { false }); yield return null; yield return null;
            Assert.That(RenderSettings.skybox, Is.Not.SameAs(sky), "Loading the menu restores its own scene sky, without runtime global overrides."); match = null;
        }
        finally { if (match != null) Call(match, "SetupScreen"); InputSystem.settings = original; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }

    [UnityTest, Timeout(60000)]
    public IEnumerator CornholeSavedSkyboxPreservesFourCoursesAndWeatherRematches()
    {
        var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CornholeSavedSkybox" });
        var isolation = new GameObject("Explicit Cornhole saved sky regression"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = Find("LWS.TruckCornhole.CornholeMatch"); var setup = Get(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(match, "AllAI");
            Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { true });
            var sky = RenderSettings.skybox; Assert.That(sky, Is.Not.Null); Assert.That(sky.shader.name, Is.EqualTo("LWS/TruckSports/Weather Maker Viewport Skybox"));
            var weather = Get(match, "atmosphere"); var catalog = Get(weather, "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray();
            foreach (string label in new[] { "Clear", "Hurricane", "Clear" })
            {
                Call(match, "ChangeSetup"); var entry = entries.Single(item => (string)Get(item, "displayName") == label); Call(catalog, "Select", Get(entry, "id"));
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(.35f);
                Assert.That((string)Get(weather, "PresetId"), Is.EqualTo((string)Get(entry, "id"))); Assert.That(RenderSettings.skybox, Is.SameAs(sky));
                Assert.That((CameraEvent)Get(Get(weather, "clouds"), "RenderQueue"), Is.EqualTo(CameraEvent.AfterSkybox));
                var courses = (Array)Get(match, "courses"); Assert.That(courses.Length, Is.EqualTo(4));
                foreach (var course in courses) { var camera = (Camera)Get(Get(course, "chase"), "view"); Assert.That(camera.enabled, Is.True); Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.Skybox)); }
                TestContext.WriteLine("CORNHOLE SAVED SKY REMATCH: " + label + ", all four original independent course cameras remain active.");
            }
        }
        finally { if (match != null) Call(match, "ChangeSetup"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }

    [UnityTest, Timeout(60000)]
    public IEnumerator MinefieldFourViewHDRSeparatesCloudsPostAndSkySphere()
    {
        var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "MinefieldSkyDiagnosis" });
        var isolation = new GameObject("Explicit four-view sky isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation);
        isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation")); Component match = null;
        var original = InputSystem.settings; InputSettings temporary = null; Keyboard keyboard = null;
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../..", "Logs/TruckSports/PlaytestRepair/MinefieldNativeSkyDiagnosis")); Directory.CreateDirectory(folder);
        try
        {
            temporary = UnityEngine.Object.Instantiate(original); temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = temporary; keyboard = InputSystem.AddDevice<Keyboard>();
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMinefield/Scenes/TruckMinefield.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; match = Find("LWS.TruckMinefield.MinefieldMatch"); Call(match, "AllBots");
            var catalog = Get(Find("LWS.TruckSports.SportsChaosWeather"), "presets");
            foreach (var entry in (Array)Get(catalog, "entries")) if ((string)Get(entry, "displayName") == "Clear") Call(catalog, "Select", Get(entry, "id"));
            var setup = Get(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 8); var players = (Array)Get(setup, "players");
            for (int slot = 4; slot < 8; slot++) { var player = players.GetValue(slot); var source = player.GetType().GetField("source"); source.SetValue(player, Enum.Parse(source.FieldType, "Keyboard")); }
            Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { false }); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            yield return new WaitForSeconds(.5f); Call(match, "Pause", 0, false); yield return null;
            var cameras = (Camera[])Get(match, "cameras"); Assert.That(cameras.Length, Is.EqualTo(4));
            Capture(cameras, folder + "/01-baseline.png");
            Capture(cameras, folder + "/01b-hdr-msaa2.png", 2);
            var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/TruckSports/Resources/Viewport Weather Maker Skybox.shader"); Assert.That(shader, Is.Not.Null);
            Assert.That(UnityEditor.ShaderUtil.GetShaderMessages(shader).Any(message => message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error), Is.False);
            var oldSky = RenderSettings.skybox; var bridge = new Material(shader); var oldClears = cameras.Select(camera => camera.clearFlags).ToArray();
            var clouds = Find("LWS.TruckSports.SportsChaosWeather"); var cloudComponent = (Component)Get(clouds, "clouds"); var queue = cloudComponent.GetType().GetField("RenderQueue"); var oldQueue = queue.GetValue(cloudComponent);
            try
            {
                RenderSettings.skybox = bridge; queue.SetValue(cloudComponent, CameraEvent.AfterSkybox); foreach (var camera in cameras) camera.clearFlags = CameraClearFlags.Skybox; Call(cloudComponent, "ForceReload");
                yield return null; Capture(cameras, folder + "/01c-vendor-skybox-path.png");
            }
            finally { RenderSettings.skybox = oldSky; queue.SetValue(cloudComponent, oldQueue); for (int i = 0; i < cameras.Length; i++) cameras[i].clearFlags = oldClears[i]; Call(cloudComponent, "ForceReload"); UnityEngine.Object.DestroyImmediate(bridge); }
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) if (component.GetType().Name == "WeatherMakerFullScreenCloudsScript") component.enabled = false;
            yield return null; Capture(cameras, folder + "/02-no-cloud-component.png");
            var data = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime", true);
            foreach (var camera in cameras) { var extra = camera.GetComponent(data); if (extra != null) data.GetProperty("renderPostProcessing").SetValue(extra, false); }
            yield return null; Capture(cameras, folder + "/03-no-cloud-or-post.png");
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component.GetType().Name == "WeatherMakerSkySphereScript") { TestContext.WriteLine("SKY SPHERE " + component.name); foreach (var renderer in component.GetComponentsInChildren<Renderer>()) renderer.enabled = false; }
            yield return null; Capture(cameras, folder + "/04-no-cloud-post-or-sphere.png");
            TestContext.WriteLine("Frozen four-view HDR visual isolation only; no corrected rendering claim.");
        }
        finally { if (match != null) Call(match, "SetupScreen"); InputSystem.settings = original; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }
    static void Capture(Camera[] cameras, string path, int samples = 1)
    {
        var previous = RenderTexture.active; var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, samples); var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        try
        {
            foreach (var camera in cameras) { var saved = camera.targetTexture; try { camera.targetTexture = target; camera.Render(); } finally { camera.targetTexture = saved; } }
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
