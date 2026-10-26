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

public sealed class SwingVenueLightingTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static void Select(string name) => Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), name) });

    [UnityTest, Timeout(120000)]
    public IEnumerator SavedSwingFixturesLightThePlayingAreaWithoutChangingPhysicsOrTimeOwnership()
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SwingVenueLighting" });
        var isolation = new GameObject("Isolated Swing venue-light acceptance");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckGiantSwing/Scenes/TruckGiantSwing.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single();
            Call(match, "AllBots");
            var catalog = Get(Get(match, "weatherField"), "presets");
            var clear = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear");
            Call(catalog, "Select", Get(clear, "id"));
            var clock = UnityEngine.Object.FindObjectsByType(Runtime("SportsMatchTime"), FindObjectsSortMode.None).Cast<Component>().Single();
            var lamps = (Light[])Get(clock, "nightLights");
            Assert.That(lamps.Length, Is.EqualTo(6), "Swing's saved scene needs actual purchased venue fixtures; a moving truck beam cannot light its gantry and targets.");
            var root = clock.transform.Find("Authored Swing night venue lamps"); Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<Joint>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty, "Saved fixtures need no runtime regeneration or second clock.");
            Assert.That(lamps.All(lamp => lamp.transform.IsChildOf(root) && lamp.shadows == LightShadows.None && lamp.type == LightType.Spot), Is.True);
            Assert.That(lamps.All(lamp => Mathf.Abs(lamp.transform.position.x) >= 35), Is.True, "Fixtures sit outside the active gantry, barriers and target lanes.");
            var ids = lamps.Select(lamp => lamp.GetInstanceID()).ToArray();
            foreach (string choice in new[] { "Night", "Day", "Evening", "Morning", "Night" })
            {
                Call(match, "SetupScreen"); Select(choice); Assert.That(Call(match, "StartMatch"), Is.True);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.That(Get(clock, "Applied").ToString(), Is.EqualTo(choice));
                bool on = choice == "Night" || choice == "Evening";
                Assert.That(lamps.All(lamp => on ? lamp.intensity > 0 : lamp.intensity == 0), Is.True);
                int revision = (int)Get(clock, "Revision");
                Call(clock, "Configure", -91, Get(match, "MatchId"));
                Assert.That(Get(clock, "Revision"), Is.EqualTo(revision));
                Call(match, "Pause", 0, false); yield return null;
                Assert.That(lamps.All(lamp => on ? lamp.intensity > 0 : lamp.intensity == 0), Is.True);
                Call(match, "Resume");
                CollectionAssert.AreEqual(ids, lamps.Select(lamp => lamp.GetInstanceID()).ToArray());
            }
            // Same frozen rendered scene, with only these six lights toggled.
            // This measures a real GPU lighting difference, not just assignments.
            Time.timeScale = 0; yield return null;
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled);
            Render(camera);
            var lit = Render(camera);
            foreach (var lamp in lamps) lamp.enabled = false;
            var dark = Render(camera);
            foreach (var lamp in lamps) lamp.enabled = true;
            int brighter = lit.Zip(dark, (a, b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value);
            TestContext.WriteLine("Swing actual rendered pixels brightened by saved fixtures: " + brighter + "/57600");
            Assert.That(brighter, Is.GreaterThan(1000), "The existing gameplay camera must actually see light on venue surfaces.");
            Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, 1280, 720, "Logs/TruckSports/PlaytestRepair/SwingVenueLighting/gantry-night-720.png", null });
            Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, 1920, 1080, "Logs/TruckSports/PlaytestRepair/SwingVenueLighting/gantry-night-1080.png", null });
            TestContext.WriteLine("Saved fixtures, no added physics, Night/Evening on, Day/Morning off, pause/rematch identities and original clock authority passed. Controlled rendering only; no human or Windows acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    static Color[] Render(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32);
        var previous = camera.targetTexture; var active = RenderTexture.active; var image = new Texture2D(320, 180, TextureFormat.RGB24, false);
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = previous; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
