#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    public sealed class WaterRaceRematchTests
    {
        const string Scene = "Assets/TruckWaterSports/Scenes/PaddlePals.unity";
        const float PlaybackSpeed = 4;
        const float WallSecondsPerRound = 90;

        static Type Runtime(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new InvalidOperationException("Missing runtime type: " + name);
        }

        static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] arguments) => target.GetType().GetMethod(name).Invoke(target, arguments);

        static void SelectClearWeather()
        {
            var type = Runtime("LWS.TruckSports.SportsWeatherPresets");
            var catalog = AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset", type);
            Assert.That(catalog, Is.Not.Null, "Use the existing installed weather catalog.");
            foreach (var entry in (Array)Field(catalog, "entries"))
            {
                if ((string)Field(entry, "displayName") != "Clear") continue;
                Call(catalog, "Select", (string)Field(entry, "id"));
                return;
            }
            Assert.Fail("The existing Clear weather selection is missing.");
        }

        static string ObservedState(object match)
        {
            var text = new StringBuilder("clock=").Append(Property(match, "Clock"));
            var gates = (int[])Field(match, "Gates");
            var missed = (int[])Field(match, "MissedGates");
            var finish = (float[])Field(match, "FinishTimes");
            for (int slot = 0; slot < 4; slot++)
            {
                var vessel = Call(match, "Vessel", slot);
                var body = (Rigidbody)Field(vessel, "body");
                text.Append(" | slot=").Append(slot).Append(" gate=").Append(gates[slot])
                    .Append(" missed=").Append(missed[slot]).Append(" finish=").Append(finish[slot])
                    .Append(" strokes=").Append(Property(vessel, "Strokes"))
                    .Append(" capsizes=").Append(Property(vessel, "Capsizes"))
                    .Append(" position=").Append(body.position.ToString("F1"));
            }
            return text.ToString();
        }

        [UnityTest]
        public IEnumerator AuthoredPaddlePalsAICompletesRaceAndRematchUsingNativePhysics()
        {
            var prefs = Runtime("LWS.TruckSports.SportsPrefs");
            var lease = (IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "WaterRaceRematch" });
            var originalScale = Time.timeScale;
            var originalFixedStep = Time.fixedDeltaTime;
            Component match = null;
            try
            {
                Time.timeScale = 1;
                SelectClearWeather();
                EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                var matches = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.PaddlePalsMatch"), FindObjectsSortMode.None);
                Assert.That(matches.Length, Is.EqualTo(1));
                match = (Component)matches[0];

                // These are normal pre-game choices, not altered game rules.
                // Keep the complete authored course, duration, gates, currents,
                // buoyancy, paddle forces and AI implementation unchanged.
                match.GetType().GetField("option").SetValue(match, 0);
                Field(match, "setup").GetType().GetField("count").SetValue(Field(match, "setup"), 4);
                Call(match, "AllBots");
                var menus = Field(match, "menus");
                var firstVessel = Call(match, "Vessel", 0);
                var secondVessel = Call(match, "Vessel", 2);
                var bodies = new[] { (Rigidbody)Field(firstVessel, "body"), (Rigidbody)Field(secondVessel, "body") };
                var startingPositions = new[] { bodies[0].position, bodies[1].position };
                var course = Field(match, "course");
                int checkpointCount = ((Vector3[])Field(course, "stations")).Length - 1;
                Assert.That(checkpointCount, Is.GreaterThan(3), "Do not replace the authored river with a tiny test course.");
                string previousMatch = null;

                for (int round = 0; round < 2; round++)
                {
                    Time.timeScale = 1;
                    Call(menus, "Action", round == 0 ? "start" : "rematch");
                    Assert.That((bool)Property(match, "Live"), Is.True, "Normal start/rematch menu action must launch the race.");
                    Assert.That(Property(menus, "Screen").ToString(), Is.EqualTo("Play"));
                    var matchId = (string)Property(match, "MatchId");
                    Assert.That(matchId, Is.Not.Empty);
                    Assert.That(matchId, Is.Not.EqualTo(previousMatch), "Rematch requires a new authoritative match identity.");
                    previousMatch = matchId;
                    Assert.That((int[])Field(match, "Gates"), Is.All.EqualTo(1));
                    Assert.That((int[])Field(match, "MissedGates"), Is.All.EqualTo(0));
                    Assert.That((float[])Field(match, "FinishTimes"), Is.All.EqualTo(-1));
                    foreach (var vessel in new[] { firstVessel, secondVessel })
                    {
                        Assert.That((int)Property(vessel, "Strokes"), Is.Zero, "Rematch resets real stroke counters.");
                        Assert.That((int)Property(vessel, "Capsizes"), Is.Zero);
                    }
                    for (int i = 0; i < bodies.Length; i++)
                        Assert.That(Vector3.Distance(bodies[i].position, startingPositions[i]), Is.LessThan(.02f), "Rematch restores the authored vessel spawn.");

                    // Accelerate wall-clock playback only. Unity still executes
                    // ordinary FixedUpdate steps at the unchanged physics dt.
                    Time.timeScale = PlaybackSpeed;
                    float wallDeadline = Time.realtimeSinceStartup + WallSecondsPerRound;
                    float simulationDeadline = (float)Field(match, "matchDuration") + 5;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < wallDeadline
                           && (float)Property(match, "Clock") < simulationDeadline)
                        yield return null;
                    Time.timeScale = 1;
                    string observed = ObservedState(match);
                    Assert.That((bool)Property(match, "Finished"), Is.True, "Natural race did not resolve within its configured duration. " + observed);
                    Assert.That(Property(menus, "Screen").ToString(), Is.EqualTo("Result"), observed);

                    int finishers = 0, winners = 0;
                    var finishes = (float[])Field(match, "FinishTimes");
                    var misses = (int[])Field(match, "MissedGates");
                    for (int slot = 0; slot < 4; slot++)
                    {
                        if (finishes[slot] >= 0)
                        {
                            finishers++;
                            Assert.That(checkpointCount - misses[slot], Is.GreaterThanOrEqualTo(2), "A finisher must genuinely hit gates, not merely miss every gate. " + observed);
                            Assert.That((bool)Call(match, "ResultHasOutcome", slot), Is.True, observed);
                        }
                        if ((bool)Call(match, "ResultWinner", slot))
                        {
                            winners++;
                            Assert.That(finishes[slot], Is.GreaterThan(0), "A winner must have an actual measured finish. " + observed);
                        }
                    }
                    Assert.That(finishers, Is.GreaterThan(0), "A timed-out all-DNF race is not a successful completed-race check. " + observed);
                    Assert.That(winners, Is.GreaterThan(0), observed);
                    Assert.That(finishes[0], Is.EqualTo(finishes[1]), "Canoe partners share their physical finish.");
                    Assert.That(finishes[2], Is.EqualTo(finishes[3]));
                    foreach (var vessel in new[] { firstVessel, secondVessel })
                    {
                        Assert.That((int)Property(vessel, "Strokes"), Is.GreaterThan(0), observed);
                        Assert.That((int)Call(vessel, "StrokesAtStation", 0), Is.GreaterThan(0), "Left station must actually paddle.");
                        Assert.That((int)Call(vessel, "StrokesAtStation", 1), Is.GreaterThan(0), "Right station must actually paddle.");
                        Assert.That((int)Property(Field(vessel, "flotation"), "Steps"), Is.GreaterThan(0), "The existing Archimeds solver must actually execute.");
                    }
                    Assert.That(Time.fixedDeltaTime, Is.EqualTo(originalFixedStep), "Do not alter physics resolution to accelerate acceptance.");
                    TestContext.WriteLine("WATER_NATURAL_RACE round=" + round + " playback=" + PlaybackSpeed + "x; " + observed);
                }
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
