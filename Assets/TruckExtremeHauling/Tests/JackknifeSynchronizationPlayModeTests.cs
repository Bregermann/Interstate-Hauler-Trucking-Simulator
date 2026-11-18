#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class JackknifeSynchronizationPlayModeTests
    {
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);
        static IDisposable Isolate() => (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10SynchronizedIce" });
        static void Load() => EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/JackknifeOnIce.unity", new LoadSceneParameters(LoadSceneMode.Single));
        static object Match() => UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckExtremeHauling.JackknifeIceMatch"), FindObjectsSortMode.None).Single();

        [UnityTest]
        public IEnumerator OneToFourHumanSeatsRetainOwnersAndGainAIPartners()
        {
            using (Isolate())
            {
                Load(); yield return null; yield return null;
                var match = Match();
                match.GetType().GetField("option").SetValue(match, 2);
                var setup = Field(match, "setup");
                var players = (Array)Field(setup, "players");
                for (int humans = 1; humans <= 4; humans++)
                {
                    setup.GetType().GetField("count").SetValue(setup, humans);
                    for (int slot = 0; slot < 4; slot++)
                    {
                        var player = players.GetValue(slot);
                        var source = player.GetType().GetField("source");
                        source.SetValue(player, Enum.Parse(source.FieldType, slot < humans ? "Keyboard" : "AI"));
                    }
                    Call(match, "NormalizeSetup");
                    Assert.That((int)Field(setup, "count"), Is.EqualTo(4), "Two physical pairs must remain active.");
                    for (int slot = 0; slot < 4; slot++)
                    {
                        Assert.That(Field(players.GetValue(slot), "source").ToString(), Is.EqualTo(slot < humans ? "Keyboard" : "AI"), "Normalization must retain joined-human ownership.");
                        int partner = (int)Call(match, "SynchronizationPartner", slot);
                        Assert.That(partner, Is.EqualTo(slot < 2 ? slot + 2 : slot - 2));
                        Assert.That((int)Call(match, "SynchronizationPartner", partner), Is.EqualTo(slot));
                    }
                }
                match.GetType().GetField("option").SetValue(match, 0);
                setup.GetType().GetField("count").SetValue(setup, 1);
                Call(match, "NormalizeSetup");
                Assert.That((int)Field(setup, "count"), Is.EqualTo(1), "Default drift retains solo participation.");
            }
        }

        [UnityTest]
        public IEnumerator NaturalAIPairScoresOnlyRealSynchronizedDriftAndResets()
        {
            using (Isolate())
            {
                Load(); yield return null; yield return null;
                var match = Match(); var setup = Field(match, "setup");
                match.GetType().GetField("option").SetValue(match, 2);
                setup.GetType().GetField("count").SetValue(setup, 1);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                try
                {
                    Time.timeScale = 3;
                    float deadline = Time.realtimeSinceStartup + 70;
                    float nextSample = 20; int samples = 0;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        float clock = (float)match.GetType().BaseType.GetField("age", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(match);
                        if (clock >= nextSample && samples < 7)
                        {
                            nextSample += 20; samples++;
                            Debug.Log("HH10_SYNCHRONIZED_LIVE clock=" + clock.ToString("F1") + " " + string.Join(" | ", Enumerable.Range(0, 4).Select(slot => "P" + (slot + 1) + " " + Call(match, "SynchronizationDiagnostic", slot))));
                        }
                        yield return null;
                    }
                    string diagnostic = string.Join(" | ", Enumerable.Range(0, 4).Select(slot => "P" + (slot + 1) + " " + Call(match, "SynchronizationDiagnostic", slot)));
                    Assert.That((bool)Property(match, "Finished"), Is.True, diagnostic);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.GreaterThan(0), "A real matched physical drift is required; timeout alone cannot pass. " + diagnostic);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 2), Is.EqualTo(Call(match, "ObjectivesCompleted", 0)), "Both physical teammates share each real synchronization window.");
                    Assert.That((double)Call(match, "ResultScore", 2), Is.EqualTo(Call(match, "ResultScore", 0)).Within(.001));
                    Call(match, "SetupScreen");
                    Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 0), Is.Zero);
                    Assert.That((int)Call(match, "ObjectivesCompleted", 2), Is.Zero);
                    Time.timeScale = 3; yield return new WaitForSeconds(3.5f);
                    Assert.That((bool)Property(match, "Running"), Is.True);
                    Call(match, "SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
    }
}
#endif
