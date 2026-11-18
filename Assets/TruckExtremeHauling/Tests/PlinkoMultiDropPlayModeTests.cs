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
    public sealed class PlinkoMultiDropPlayModeTests
    {
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);

        [UnityTest]
        public IEnumerator NaturalThreeDropsPreservePegOutcomesSumOnceAndRematch()
            => RunThreeDrops(0);

        [UnityTest]
        public IEnumerator ChaosPlinkoReboundsFromMovingPhysicalPegsAndScoresSettledBins()
            => RunThreeDrops(2);

        static IEnumerator RunThreeDrops(int option)
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10PlinkoMultiDrop" }))
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/TrailerPlinko.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var type = Type.GetType("LWS.TruckExtremeHauling.TrailerPlinkoMatch, Assembly-CSharp", true);
                object match = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single();
                object setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                type.GetField("option").SetValue(match, option);
                type.GetField("dropsPerPlayer").SetValue(match, 3);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                try
                {
                    Time.timeScale = 3;
                    float deadline = Time.realtimeSinceStartup + 70;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) yield return null;
                    string diagnostic = (string)Call(match, "PlinkoDiagnostic", 0);
                    Assert.That((bool)Property(match, "Finished"), Is.True, diagnostic);
                    Assert.That((int)Call(match, "DropsCompleted", 0), Is.EqualTo(3), diagnostic);
                    Assert.That((int)Call(match, "ReleasedDrops", 0), Is.EqualTo(3), diagnostic);
                    int scored = (int)Call(match, "ObjectivesCompleted", 0), missed = (int)Call(match, "MissedDrops", 0);
                    Assert.That(scored, Is.GreaterThan(0), "At least one real supported settled-bin outcome is required. " + diagnostic);
                    Assert.That(scored + missed, Is.EqualTo(3), "Each real throw resolves exactly once.");
                    double total = 0; int supported = 0, contacts = 0, movingContacts = 0;
                    var points = (float[])Field(match, "binPoints");
                    for (int drop = 0; drop < 3; drop++)
                    {
                        int bin = (int)Call(match, "SettledBinForDrop", 0, drop);
                        float measuredPoints = (float)Call(match, "MeasuredDropPoints", 0, drop);
                        contacts += (int)Call(match, "PegContactsForDrop", 0, drop);
                        movingContacts += (int)Call(match, "MovingPegContactsForDrop", 0, drop);
                        if (bin >= 0) { supported++; Assert.That(measuredPoints, Is.EqualTo(points[bin]), "Points come from the actual supported physical bin."); }
                        else Assert.That(measuredPoints, Is.Zero, "A failed physical drop awards no points.");
                        total += measuredPoints;
                    }
                    Assert.That(supported, Is.EqualTo(scored));
                    Assert.That(contacts, Is.GreaterThan(0), "The drop must hit physical pegs.");
                    if (option == 2) Assert.That(movingContacts, Is.GreaterThan(0), "Chaos must hit moving kinematic pegs.");
                    Assert.That((double)Call(match, "ResultScore", 0), Is.EqualTo(total).Within(.001), "No duplicate or overwritten drop awards.");
                    Assert.That(Mathf.Abs((float)Call(match, "ReleaseRollDegrees", 0, 1)), Is.GreaterThan(1), "AI must choose a real second release orientation.");
                    yield return new WaitForSeconds(1);
                    Assert.That((double)Call(match, "ResultScore", 0), Is.EqualTo(total).Within(.001));
                    string previous = (string)Property(match, "MatchId");
                    Call(match, "SetupScreen");
                    Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(previous));
                    Assert.That((int)Call(match, "DropsCompleted", 0), Is.Zero);
                    Assert.That((int)Call(match, "ReleasedDrops", 0), Is.Zero);
                    Assert.That((int)Call(match, "PegContactsForDrop", 0, 0), Is.Zero);
                    Assert.That((int)Call(match, "MovingPegContactsForDrop", 0, 0), Is.Zero);
                    Assert.That((double)Call(match, "ResultScore", 0), Is.Zero);
                    yield return new WaitForSeconds(.5f);
                    var trailer = (Component)((Array)Field(match, "trailers")).GetValue(0);
                    Assert.That(((Rigidbody)Field(trailer, "Body")).isKinematic, Is.True, "A rematch starts in ordinary kinematic aiming preparation.");
                    TestContext.WriteLine("PLINKO_REAL_CONTACTS mode=" + option + " pegContacts=" + contacts + " movingPegContacts=" + movingContacts + " settled=" + scored + " misses=" + missed + " points=" + total + " rematch=fresh");
                    Call(match, "SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
    }
}
#endif
