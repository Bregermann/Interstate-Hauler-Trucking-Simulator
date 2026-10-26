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

public sealed class SnowLightingIntegrationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly string[] Fields = { "SunTintColorGradient", "DayAmbientColor", "DayAmbientColorSky", "DayAmbientColorGround", "DayAmbientColorEquator", "DawnDuskAmbientColor", "DawnDuskAmbientColorSky", "DawnDuskAmbientColorGround", "DawnDuskAmbientColorEquator" };
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(180000)] public IEnumerator FareForecastsRestoreAuthoredLightingAndRematch() => Check("TruckFareFight");
    [UnityTest, Timeout(180000)] public IEnumerator SnakeForecastsRestoreAuthoredLightingAndRematch() => Check("TruckSnake");
    IEnumerator Check(string name)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SnowLighting" + name });
        var isolation = new GameObject("Isolated winter lighting acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
            var weather = match.GetComponent(Runtime("SportsChaosWeather")); var clock = Get(weather, "matchTime"); var cycle = Get(clock, "cycle");
            var source = (ScriptableObject)Get(cycle, "DayNightProfile"); var sourceBefore = EditorJsonUtility.ToJson(source);
            var catalog = Get(weather, "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray();
            object Entry(string title) => entries.Single(item => (string)Get(item, "displayName") == title);
            Call(catalog, "Select", Get(Entry("Clear"), "id")); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Morning") });
            Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(3);
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); Read(camera);
            var profile = (ScriptableObject)Get(cycle, "DayNightProfile"); Assert.That(profile, Is.Not.SameAs(source)); Assert.That(AssetDatabase.Contains(profile), Is.False);
            int revision = (int)Get(clock, "Revision"); float seconds = (float)Get(cycle, "TimeOfDay");
            string originalIntensity = GradientJson((Gradient)Get(profile, "SunIntensityGradient")); string night = GradientJson((Gradient)Get(profile, "NightAmbientColor"));
            Save("clear-morning");
            foreach (string forecast in new[] { "Snow", "Heavy Snow", "Blizzard", "Rain", "Snow", "Clear" })
            {
                var selected = Entry(forecast); Call(weather, "ConfigurePreset", selected, 45671); yield return new WaitForSeconds(1);
                Assert.That(Get(clock, "Revision"), Is.EqualTo(revision), "Changing forecast must not reroll the match clock."); Assert.That((float)Get(cycle, "TimeOfDay"), Is.EqualTo(seconds));
                Assert.That(GradientJson((Gradient)Get(profile, "SunIntensityGradient")), Is.EqualTo(originalIntensity)); Assert.That(GradientJson((Gradient)Get(profile, "NightAmbientColor")), Is.EqualTo(night));
                bool snowy = (bool)Get(selected, "Snow");
                foreach (string field in Fields)
                {
                    var gradient = (Gradient)Get(profile, field); var authored = (Gradient)Get(source, field);
                    CollectionAssert.AreEqual(authored.alphaKeys.Select(key => key.time), gradient.alphaKeys.Select(key => key.time)); CollectionAssert.AreEqual(authored.alphaKeys.Select(key => key.alpha), gradient.alphaKeys.Select(key => key.alpha));
                    CollectionAssert.AreEqual(authored.colorKeys.Select(key => key.time), gradient.colorKeys.Select(key => key.time)); Assert.That(gradient.mode, Is.EqualTo(authored.mode));
                    if (!snowy) Assert.That(GradientJson(gradient), Is.EqualTo(GradientJson(authored)), "Non-snow restores authored " + field);
                }
                var sun = (Light)Get(Get(clock, "perspectiveSun"), "Light");
                if (snowy) { Assert.That(sun.color.b, Is.GreaterThan(sun.color.r)); Assert.That(sun.color.g, Is.GreaterThan(sun.color.r)); }
                else Assert.That(sun.color.r, Is.GreaterThan(sun.color.b), "Authored warm clear dawn must return.");
                TestContext.WriteLine(name + " " + forecast + " actualSun=" + sun.color + " revision=" + revision + " time=" + seconds);
                if (forecast == "Clear") Save("restored-clear-morning");
            }
            Call(weather, "ConfigurePreset", Entry("Snow"), 45671); yield return new WaitForSeconds(3);
            Time.timeScale = 0; var applied = Read(camera); Save("snow-morning");
            var snowFields = Fields.Select(field => GradientJson((Gradient)Get(profile, field))).ToArray();
            Call(clock, "ApplySnowPalette", true); CollectionAssert.AreEqual(snowFields, Fields.Select(field => GradientJson((Gradient)Get(profile, field))).ToArray(), "Repeated configuration cannot compound tint.");
            Call(clock, "ApplySnowPalette", false); Refresh(profile); var warm = Read(camera);
            Call(clock, "ApplySnowPalette", true); Refresh(profile); Read(camera);
            int before = warm.Count(Warm), after = applied.Count(Warm); TestContext.WriteLine(name + " actual snowy surface warmPixels=" + before + "->" + after + "/57600");
            Assert.That(before, Is.GreaterThan(100)); Assert.That(after, Is.LessThan(before * .3f), "Actual snowy rendering must substantially reduce the diagnosed orange cast.");
            // The same adapter's deliberate indoor exception must keep all nine
            // authored gradients, without changing the selected forecast or clock.
            Call(clock, "ApplySnowPalette", false); clock.GetType().GetField("preserveInteriorAmbient").SetValue(clock, true); Call(clock, "ApplySnowPalette", true);
            foreach (string field in Fields) Assert.That(GradientJson((Gradient)Get(profile, field)), Is.EqualTo(GradientJson((Gradient)Get(source, field))));
            clock.GetType().GetField("preserveInteriorAmbient").SetValue(clock, false); Call(clock, "ApplySnowPalette", true);
            Time.timeScale = 1; Call(match, "SetupScreen"); Call(catalog, "Select", Get(Entry("Snow"), "id")); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(3);
            camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); Save("snow-rematch");
            CollectionAssert.AreEqual(snowFields, Fields.Select(field => GradientJson((Gradient)Get(profile, field))).ToArray(), "Rematch retains a single cool palette, with no accumulation.");
            Assert.That(EditorJsonUtility.ToJson(source), Is.EqualTo(sourceBefore), "Source day/night profile must never be mutated.");
            void Save(string label)
            {
                foreach (int width in new[] { 1280, 1920 }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, "Logs/TruckSports/PlaytestRepair/SnowLightingNative/" + name + "-" + label + "-" + width + ".png", null });
            }
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    [Serializable] sealed class GradientValue { public Gradient gradient; }
    static string GradientJson(Gradient value) => JsonUtility.ToJson(new GradientValue { gradient = value });
    static bool Warm(Color color) => color.r > .12f && color.r > color.b * 1.35f && color.r > color.g * 1.15f;
    static void Refresh(ScriptableObject profile)
    {
        var field = profile.GetType().GetField("UpdateInterval"); var interval = field.GetValue(profile);
        try { field.SetValue(profile, -.001f); Call(profile, "UpdateFromProfile", false); } finally { field.SetValue(profile, interval); }
    }
    static Color[] Read(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); var prior = camera.targetTexture; var active = RenderTexture.active; Texture2D image = null;
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
