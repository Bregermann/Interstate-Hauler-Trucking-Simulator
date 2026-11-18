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

public sealed class HitchRunContestPlayModeTests
{
    static object Read(object value, string name)
    {
        for (var type = value.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(value);
        }
        throw new MissingFieldException(value.GetType().FullName, name);
    }
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.HitchRunMatch, Assembly-CSharp", true);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HitchRunContest" });

    [UnityTest]
    public IEnumerator NaturalTrailerThiefContestsRecouplesAndDeliversActualOpponentCargo()
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/HitchAndRun.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
            var setup = Read(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4);
            match.GetType().GetField("option").SetValue(match, 0); Call(match, "NormalizeSetup"); Call(match, "AllBots");
            // Ordinary asymmetric difficulty configuration creates an actual
            // tactical ambush opportunity; it never grants a steal or score.
            var players = (Array)Read(setup, "players");
            for (int slot = 0; slot < 4; slot++)
            {
                var player = players.GetValue(slot); var difficulty = player.GetType().GetField("difficulty");
                difficulty.SetValue(player, Enum.Parse(difficulty.FieldType, slot == 0 ? "Hard" : "Normal")); players.SetValue(player, slot);
            }
            var weather = Read(match, "presentationWeather");
            if (weather != null && Read(weather, "presets") != null) Call(Read(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var trailers = (Array)Read(match, "trailers"); var couplers = (Array)Read(match, "couplers");
            var observedContest = new bool[trailers.Length]; var observedPhysicalRecapture = new bool[trailers.Length];
            var originalOwners = Enumerable.Repeat(-1, trailers.Length).ToArray();
            var deliveredStolen = new bool[4];
            int samples = 0; float nextSample = 30; string lastLive = "not sampled"; bool contestObservedLogged = false;
            bool counterTheftLogged = false;
            float firstContestAt = -1, firstRecaptureAt = -1, firstStolenDeliveryAt = -1;
            try
            {
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 75;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    var priorOwners = (int[])Read(match, "contestedOwner"); var stealers = (int[])Read(match, "cargoStealer");
                    for (int t = 0; t < trailers.Length; t++)
                    {
                        var load = trailers.GetValue(t); int owner = (int)Property(load, "Owner");
                        if (!counterTheftLogged && observedPhysicalRecapture[t] && priorOwners[t] >= 0 && owner < 0)
                        {
                            counterTheftLogged = true;
                            Debug.Log("HH10_HITCH_RUN_COUNTER_THEFT age=" + Property(match, "Elapsed") + " releasedOwner=" + priorOwners[t] + " " + Call(match, "ContestDiagnostic", priorOwners[t]));
                        }
                        if (priorOwners[t] >= 0 && owner < 0) { observedContest[t] = true; originalOwners[t] = priorOwners[t]; }
                        if (observedContest[t] && owner >= 0 && owner != originalOwners[t] && ReferenceEquals(Property(couplers.GetValue(owner), "Attached"), load) && (bool)Property(couplers.GetValue(owner), "PhysicalConstraintPresent"))
                        {
                            observedPhysicalRecapture[t] = true;
                            if (firstRecaptureAt < 0) { firstRecaptureAt = (float)Property(match, "Elapsed"); Debug.Log("HH10_HITCH_RUN_FIRST_NATIVE_RECAPTURE age=" + firstRecaptureAt + " owner=" + owner + " " + Call(match, "HitchRunApproachDiagnostic", owner)); }
                        }
                        if (observedPhysicalRecapture[t] && stealers[t] >= 0 && (int)Property(load, "Deliveries") > 0)
                        {
                            deliveredStolen[stealers[t]] = true;
                            if (firstStolenDeliveryAt < 0) { firstStolenDeliveryAt = (float)Property(match, "Elapsed"); Debug.Log("HH10_HITCH_RUN_FIRST_STOLEN_DELIVERY age=" + firstStolenDeliveryAt + " owner=" + stealers[t]); }
                        }
                    }
                    if (!contestObservedLogged && observedContest.Any(value => value))
                    {
                        contestObservedLogged = true;
                        firstContestAt = (float)Property(match, "Elapsed");
                        Debug.Log("HH10_HITCH_RUN_FIRST_PHYSICAL_CONTEST age=" + Property(match, "Elapsed") + " " + Call(match, "ContestDiagnostic", 0));
                    }
                    if ((float)Property(match, "Elapsed") >= nextSample && samples < 6)
                    {
                        lastLive = string.Join(" | ", Enumerable.Range(0, 4).Select(slot => "P" + (slot + 1) + " " + Call(match, "ContestDiagnostic", slot) + " " + Call(match, "HitchRunApproachDiagnostic", slot)));
                        Debug.Log("HH10_HITCH_RUN_NATURAL_CONTEST age=" + Property(match, "Elapsed") + " " + lastLive);
                        nextSample += 30; samples++;
                    }
                    yield return null;
                }
                string state = "age=" + Property(match, "Elapsed") + " firstContest=" + firstContestAt + " firstNativeRecapture=" + firstRecaptureAt + " firstStolenDelivery=" + firstStolenDeliveryAt + " contests=" + string.Join(",", (int[])Read(match, "contestsWon")) + " steals=" + string.Join(",", (int[])Read(match, "successfulSteals")) + " deliveries=" + string.Join(",", ((int[])Read(match, "Deliveries")).Take(4)) + " lastLive=[" + lastLive + "]";
                Assert.That((bool)Property(match, "Finished"), Is.True, state);
                Assert.That(observedContest.Any(value => value), Is.True, "A normal stopped three-second challenge must release actual opponent cargo. " + state);
                Assert.That(observedPhysicalRecapture.Any(value => value), Is.True, "Strict native physical coupling must occur after the opponent releases. " + state);
                Assert.That(deliveredStolen.Any(value => value), Is.True, "Stealing alone cannot certify a delivered cargo result. " + state);
                for (int slot = 0; slot < 4; slot++) if (deliveredStolen[slot])
                {
                    Assert.That(((double[])Read(match, "Scores"))[slot], Is.GreaterThan(0), state);
                    Assert.That((bool)Call(match, "ResultEligible", slot), Is.True, state);
                }
                Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That(((int[])Read(match, "contestsWon")).All(value => value == 0), Is.True);
                Assert.That(((int[])Read(match, "successfulSteals")).All(value => value == 0), Is.True);
                Assert.That(((Array)Read(match, "recoveryCargo")).Cast<object>().All(value => value == null), Is.True, "Rematch must forget every unfinished physical cargo recovery.");
                Assert.That(((double[])Read(match, "Scores")).All(value => value == 0), Is.True);
            }
            finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); }
        }
    }
}
#endif
