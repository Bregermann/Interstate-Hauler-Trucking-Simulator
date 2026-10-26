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

public sealed class PartyVenueLightingTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(120000)] public IEnumerator BalloonBattleUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckBalloonBattle", 6);
    [UnityTest, Timeout(120000)] public IEnumerator HotPotatoUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckHotPotato", 6);
    [UnityTest, Timeout(120000)] public IEnumerator WhackAMoleUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckWhackAMole", 6);
    [UnityTest, Timeout(120000)] public IEnumerator KingOfTheHillUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckKingOfTheHill", 16);
    [UnityTest, Timeout(120000)] public IEnumerator TetherballUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckTetherball", 6);
    [UnityTest, Timeout(120000)] public IEnumerator MagnetMayhemUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckMagnetMayhem", 6);
    [UnityTest, Timeout(120000)] public IEnumerator VolleyballUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckVolleyball", 4);
    [UnityTest, Timeout(120000)] public IEnumerator SeesawUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckSeesaw", 6);

    IEnumerator Verify(string name, int expected)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "VenueLamps" + name });
        var isolation = new GameObject("Isolated saved venue-light acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            string folder = name;
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + folder + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
            var clock = UnityEngine.Object.FindObjectsByType(Runtime("SportsMatchTime"), FindObjectsSortMode.None).Cast<Component>().Single();
            var lamps = (Light[])Get(clock, "nightLights"); Assert.That(lamps.Length, Is.EqualTo(expected));
            var root = clock.transform.Find("Authored party venue night fixtures"); Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Joint>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Assert.That(lamps.All(light => light.transform.IsChildOf(root) && light.type == LightType.Spot && light.shadows == LightShadows.None), Is.True);
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(name == "TruckSeesaw" ? expected * 2 : expected), "Each lamp retains actual purchased geometry; Seesaw also has the shared original LED panel.");
            var venueMeshes = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<MeshRenderer>(true)).Where(item => !item.transform.IsChildOf(root)).ToArray();
            foreach (Transform fixture in root)
            {
                if (name == "TruckSeesaw")
                {
                    var housing = fixture.GetComponentsInChildren<MeshRenderer>().Single(item => item.name != "Original nine-LED emitter");
                    var gantry = venueMeshes.Single(item => item.name == "Suspension gantry crossbeam");
                    Assert.That(Mathf.Abs(housing.bounds.max.y - gantry.bounds.min.y), Is.LessThan(.015f), "The light housing touches the real fixed gantry underside.");
                    StringAssert.Contains("Pixel Crushers", AssetDatabase.GetAssetPath(housing.GetComponent<MeshFilter>().sharedMesh));
                    Assert.That(fixture.GetComponentsInChildren<MeshRenderer>().Single(item => item.name == "Original nine-LED emitter").sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"));
                }
                else if (name == "TruckKingOfTheHill")
                {
                    var rails = venueMeshes.Where(item => item.name == "Visible continuous bridge edge rail");
                    Assert.That(rails.Min(rail => Vector3.Distance(fixture.position, rail.transform.TransformPoint(new Vector3(rail.GetComponent<MeshFilter>().sharedMesh.bounds.center.x, rail.GetComponent<MeshFilter>().sharedMesh.bounds.max.y, rail.GetComponent<MeshFilter>().sharedMesh.bounds.center.z)))), Is.LessThan(.015f), "Elevated poles mount on actual bridge rails, never on the valley below.");
                }
                else if (name != "TruckVolleyball" && name != "TruckMagnetMayhem")
                {
                    var seating = venueMeshes.Where(item => item.name == "Union Field Grandstand tier").OrderBy(item => Vector3.ProjectOnPlane(item.bounds.center-fixture.position,Vector3.up).sqrMagnitude).First();
                    var local = seating.transform.InverseTransformPoint(fixture.position); var mesh = seating.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Assert.That(Mathf.Abs(local.y-mesh.max.y), Is.LessThan(.001f), "Fixture follows the sloped seating top plane rather than its AABB.");
                }
            }
            var catalog = Get(match.GetComponent(Runtime("SportsChaosWeather")), "presets"); var clear = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"); Call(catalog, "Select", Get(clear, "id"));
            var ids = lamps.Select(light => light.GetInstanceID()).ToArray();
            foreach (string choice in new[] { "Night", "Day", "Evening", "Morning", "Night" })
            {
                Call(match, "SetupScreen"); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), choice) }); Assert.That(Call(match, "StartMatch"), Is.True);
                yield return new WaitForSecondsRealtime(.4f); bool on = choice == "Night" || choice == "Evening";
                Assert.That(Get(clock, "Applied").ToString(), Is.EqualTo(choice)); Assert.That(lamps.All(light => on ? light.intensity > 0 : light.intensity == 0), Is.True);
                int revision = (int)Get(clock, "Revision"); Call(clock, "Configure", -812, Get(match, "MatchId")); Assert.That(Get(clock, "Revision"), Is.EqualTo(revision));
                Call(match, "Pause", 0, false); yield return null; Assert.That(lamps.All(light => on ? light.intensity > 0 : light.intensity == 0), Is.True); Call(match, "Resume");
                CollectionAssert.AreEqual(ids, lamps.Select(light => light.GetInstanceID()).ToArray());
            }
            // Let the ordinary countdown release before inspecting gameplay;
            // retain its original timing rather than overriding sport state.
            yield return new WaitForSecondsRealtime(4);
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(1);
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); Render(camera); var lit = Render(camera);
            foreach (var light in lamps) light.enabled = false;
            Color[] dark; try { dark = Render(camera); } finally { foreach (var light in lamps) light.enabled = true; }
            int brighter = lit.Zip(dark, (a, b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value);
            Assert.That(brighter, Is.GreaterThan(1000), name + " original gameplay camera must actually see the saved venue illumination.");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/PartyVenueLighting/" + name + "-night-" + size.y + ".png", null });
            TestContext.WriteLine(name + " brighterPixels=" + brighter + "/57600; saved purchased fixtures, zero added physics, original clock, explicit time changes, pause and rematch identities passed. Native controlled rendering and AI-only match starts, not physical-device or full-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    static Color[] Render(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); var prior = camera.targetTexture; var active = RenderTexture.active; var image = new Texture2D(320, 180, TextureFormat.RGB24, false);
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
