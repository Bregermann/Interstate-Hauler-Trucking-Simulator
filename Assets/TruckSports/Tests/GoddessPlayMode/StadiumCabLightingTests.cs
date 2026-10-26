#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class StadiumCabLightingTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(120000)] public IEnumerator KickballUsesSavedStadiumLightsAndTheOriginalClock() => Verify("TruckKickball", 6);
    [UnityTest, Timeout(120000)] public IEnumerator KeepawayCabReusesTheReviewedCompanionVenueFixtures() => Verify("TruckKeepawayFirstPerson", 4);

    IEnumerator Verify(string name, int expected)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "VenueLamps" + name });
        var isolation = new GameObject("Isolated saved venue-light acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            string folder = name == "TruckKickball" ? name : "TruckKeepaway";
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + folder + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
            var clock = UnityEngine.Object.FindObjectsByType(Runtime("SportsMatchTime"), FindObjectsSortMode.None).Cast<Component>().Single();
            var lamps = (Light[])Get(clock, "nightLights"); Assert.That(lamps.Length, Is.EqualTo(expected));
            var root = clock.transform.Find("Authored night venue lamps"); Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Joint>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Assert.That(lamps.All(light => light.transform.IsChildOf(root) && light.type == LightType.Spot && light.shadows == LightShadows.None), Is.True);
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(expected), "Each lamp retains an actual purchased mesh.");
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
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(1);
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); Render(camera); var lit = Render(camera);
            foreach (var light in lamps) light.enabled = false;
            Color[] dark; try { dark = Render(camera); } finally { foreach (var light in lamps) light.enabled = true; }
            int brighter = lit.Zip(dark, (a, b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value);
            Assert.That(brighter, Is.GreaterThan(1000), name + " original gameplay camera must actually see the saved venue illumination.");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/StadiumCabLighting/" + name + "-night-" + size.y + ".png", null });
            TestContext.WriteLine(name + " brighterPixels=" + brighter + "/57600; saved purchased fixtures, zero added physics, original clock, explicit time changes, pause and rematch identities passed. Native controlled rendering and AI-only match starts, not human cab or full-match acceptance.");
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
