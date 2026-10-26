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

// Explicit investigation: modifies only the existing unsaved match-owned profile,
// restores every field, and never saves a scene, profile, material or vendor asset.
[Explicit("Controlled historical palette investigation; use SnowLightingIntegrationTests for production acceptance.")]
public sealed class SnowDawnPaletteDiagnosticsTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(180000)] public IEnumerator InspectFareSnowMorningPalette() => Compare("TruckFareFight");
    [UnityTest, Timeout(180000)] public IEnumerator InspectSnakeSnowMorningPalette() => Compare("TruckSnake");
    IEnumerator Compare(string name)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SnowDawnPalette" + name });
        var isolation = new GameObject("Isolated snow palette investigation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; ScriptableObject profile = null; FieldInfo[] fields = null; Gradient[] original = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
            var weather = match.GetComponent(Runtime("SportsChaosWeather")); var catalog = Get(weather, "presets"); var entry = ((Array)Get(catalog, "entries")).Cast<object>().Single(item => (string)Get(item, "displayName") == "Snow"); Call(catalog, "Select", Get(entry, "id"));
            Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Morning") }); Assert.That(Call(match, "StartMatch"), Is.True);
            yield return new WaitForSecondsRealtime(6); Time.timeScale = 0;
            var clock = match.GetComponent(Runtime("SportsMatchTime")); profile = (ScriptableObject)Get(Get(clock, "cycle"), "DayNightProfile");
            Call(clock, "ApplySnowPalette", false); Refresh(profile);
            Assert.That(AssetDatabase.Contains(profile), Is.False, "Only the existing runtime-owned clone may change during this investigation."); Assert.That((profile.hideFlags & HideFlags.DontSave) != 0, Is.True);
            var sun = (Component)Get(clock, "perspectiveSun"); var light = (Light)Get(sun, "Light");
            TestContext.WriteLine(name + " original vendor sun color=" + light.color + " power=" + light.intensity + " lookup=" + Call(sun, "GetGradientLookup") + " profile=" + profile.name);
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); var before = Read(camera); Save("original");
            string[] names = { "SunTintColorGradient", "DayAmbientColor", "DayAmbientColorSky", "DayAmbientColorGround", "DayAmbientColorEquator", "DawnDuskAmbientColor", "DawnDuskAmbientColorSky", "DawnDuskAmbientColorGround", "DawnDuskAmbientColorEquator" };
            fields = names.Select(field => profile.GetType().GetField(field)).ToArray(); Assert.That(fields.All(field => field != null), Is.True); original = fields.Select(field => (Gradient)field.GetValue(profile)).ToArray();
            for (int i = 0; i < fields.Length; i++)
            {
                var gradient = original[i]; TestContext.WriteLine(names[i] + " horizon=" + gradient.Evaluate(.5f));
                var replacement = new Gradient { mode = gradient.mode }; replacement.SetKeys(gradient.colorKeys.Select(key => new GradientColorKey(new Color(.82f, .91f, 1, 1) * key.color.maxColorComponent, key.time)).ToArray(), gradient.alphaKeys); fields[i].SetValue(profile, replacement);
            }
            Refresh(profile); yield return null; yield return null; Read(camera); var after = Read(camera); Save("temporary-cool");
            int warmBefore = before.Count(Warm), warmAfter = after.Count(Warm); TestContext.WriteLine(name + " UNSAVED profile-only warmPixels=" + warmBefore + "->" + warmAfter + "/57600; sun=" + light.color + "; no production palette acceptance.");
            Assert.That(warmBefore, Is.GreaterThan(100), "Reproduce the actual warm snow surface before interpreting the comparison.");
            Assert.That(light.color.b, Is.GreaterThan(light.color.r), "The temporary public profile fields must actually reach the vendor sun before interpreting pixels.");
            void Save(string label) => Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, 1280, 720, "Logs/TruckSports/PlaytestRepair/SnowDawnPaletteDiagnostics/" + name + "-" + label + "-720.png", null });
        }
        finally
        {
            if (profile != null && fields != null && original != null) { for (int i = 0; i < fields.Length; i++) fields[i].SetValue(profile, original[i]); Refresh(profile); }
            Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose();
        }
    }
    static void Refresh(ScriptableObject profile)
    {
        // The vendor uses scaled deltaTime for its interval accumulator. Keep
        // truck poses frozen and explicitly invoke its public update on this
        // unsaved profile only; restore the authored interval immediately.
        var interval = profile.GetType().GetField("UpdateInterval"); var original = interval.GetValue(profile);
        try { interval.SetValue(profile, -.001f); Call(profile, "UpdateFromProfile", false); }
        finally { interval.SetValue(profile, original); }
    }
    static bool Warm(Color color) => color.r > .12f && color.r > color.b * 1.35f && color.r > color.g * 1.15f;
    static Color[] Read(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); var prior = camera.targetTexture; var active = RenderTexture.active; Texture2D image = null;
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
