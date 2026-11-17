#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    // Saved-scene software/AI physics acceptance, not a listening, visual-quality
    // or physical-controller certification. No poses, scores or gates are injected.
    public sealed class WaterEffectsPlayModeTests
    {
        static Type Runtime(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new InvalidOperationException("Missing runtime type: " + name);
        }

        static object Field(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] arguments) => target.GetType().GetMethod(name).Invoke(target, arguments);

        static void SelectClearWeather()
        {
            var catalog = AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset", Runtime("LWS.TruckSports.SportsWeatherPresets"));
            Assert.That(catalog, Is.Not.Null);
            foreach (var entry in (Array)Field(catalog, "entries"))
                if ((string)Field(entry, "displayName") == "Clear") { Call(catalog, "Select", (string)Field(entry, "id")); return; }
            Assert.Fail("The installed Clear preset is required for this focused effects check.");
        }

        [UnityTest]
        public IEnumerator AuthoredWhitewaterEmitsCourseHeightEffectsAndResetsOnRematch()
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation")
                .Invoke(null, new object[] { "WaterEffectsSavedScene" });
            float originalScale = Time.timeScale, originalStep = Time.fixedDeltaTime;
            Component match = null;
            try
            {
                Time.timeScale = 1;
                SelectClearWeather();
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                var matches = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.WhitewaterWipeoutMatch"), FindObjectsSortMode.None);
                Assert.That(matches.Length, Is.EqualTo(1));
                match = (Component)matches[0];
                match.GetType().GetField("option").SetValue(match, 0);
                var setup = Field(match, "setup");
                setup.GetType().GetField("count").SetValue(setup, 1);
                Call(match, "NormalizeSetup");
                Call(match, "AllBots");

                var effects = match.GetComponent(Runtime("LWS.TruckSports.SportsWaterInteraction"));
                Assert.That(effects, Is.Not.Null, "The saved scene must contain authored effects; the test does not install them.");
                var course = Field(match, "course");
                Assert.That(Field(effects, "session"), Is.SameAs(match));
                Assert.That(Field(effects, "course"), Is.SameAs(course));
                var vessels = (Array)Field(match, "vessels");
                var bodies = (Rigidbody[])Field(effects, "bodies");
                Assert.That(bodies.Length, Is.EqualTo(vessels.Length));
                for (int i = 0; i < bodies.Length; i++)
                    Assert.That(bodies[i], Is.SameAs(Field(vessels.GetValue(i), "body")), "Effects must observe real saved vessel bodies.");
                Assert.That(((ParticleSystem[])Field(effects, "spray")).Length, Is.EqualTo(8));
                Assert.That(((LineRenderer[])Field(effects, "ripples")).Length, Is.EqualTo(48));

                var stations = (Vector3[])Field(course, "stations");
                Vector3 downstream = stations[stations.Length - 3];
                float sampled = (float)Call(effects, "HeightAt", downstream);
                float expected = ((Vector3)Call(course, "SurfacePoint", downstream)).y;
                Assert.That(sampled, Is.EqualTo(expected).Within(.01f));
                Assert.That(Mathf.Abs(sampled - (float)Property(effects, "WaterHeight")), Is.GreaterThan(1),
                    "A descending river must sample its real downstream height, not the surface object's static origin.");

                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                string firstMatch = (string)Property(match, "MatchId");
                Vector3 spawn = bodies[0].position;
                Time.timeScale = 2;
                float deadline = Time.realtimeSinceStartup + 20;
                while ((float)Property(match, "Clock") < 8 && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Time.timeScale = 1;
                Assert.That((float)Property(match, "Clock"), Is.GreaterThanOrEqualTo(8), "Run eight seconds of normal AI/physics.");
                Assert.That(Vector3.Distance(bodies[0].position, spawn), Is.GreaterThan(1));
                int firstEntries = (int)Property(effects, "Entries"), firstWakes = (int)Property(effects, "Wakes");
                Assert.That(firstEntries, Is.GreaterThan(0), "The actual hull must enter the water.");
                Assert.That(firstWakes, Is.GreaterThan(2), "The moving actual body must emit wakes through the shared pools.");
                Assert.That(Field(effects, "activeMatch"), Is.EqualTo(firstMatch));

                Call(match, "SetupScreen");
                yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                string nextMatch = (string)Property(match, "MatchId");
                Assert.That(nextMatch, Is.Not.EqualTo(firstMatch));
                // Each LateUpdate emits at most once per active body. Allow two
                // rendered frames of genuine new entries, not old-match counters.
                yield return null;
                yield return null;
                Assert.That(Field(effects, "activeMatch"), Is.EqualTo(nextMatch));
                Assert.That((int)Property(effects, "Entries"), Is.InRange(0, 2));
                Assert.That((int)Property(effects, "Wakes"), Is.InRange(0, 2));
                Assert.That((int)Property(effects, "Wakes"), Is.LessThan(firstWakes));
                Assert.That((int)Field(effects, "ring"), Is.InRange(0, 2), "Old foam-ring ownership must reset with the new match.");
                Assert.That((int)Field(effects, "count"), Is.Zero, "This non-replay water scene must not retain old replay events.");
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(originalStep));
                TestContext.WriteLine("WATER_EFFECTS_SAVED_SCENE software_ai_only=true simulated_seconds=8 entries=" + firstEntries +
                    " wakes=" + firstWakes + " downstream_height=" + sampled + " rematch_id_changed=true pools_reset=true");
            }
            finally
            {
                if (match != null) Call(match, "SetupScreen");
                Time.timeScale = originalScale;
                lease.Dispose();
            }
        }
    }
}
#endif
