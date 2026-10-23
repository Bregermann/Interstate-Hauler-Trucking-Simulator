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

public sealed class TournamentScoreLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);

    [UnityTest, Timeout(180000)]
    public IEnumerator PhysicalLanceContactPublishesRealAwardAndFreshRematchClearsIt()
    {
        yield return ActualAward("TruckJousting", 2, 0, "PTS");
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator ActualSuccessfulRoadCrossingPublishesOnlyItsResolvedWin()
    {
        yield return ActualAward("TruckFrogger", 1, 1, "WINS");
    }

    static IEnumerator ActualAward(string scene, int players, int option, string unit)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ActualTournamentScore" });
        var isolation = new GameObject("Actual physical tournament award fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup"); Set(setup, "count", players); Set(match, "option", option); Call(match, "AllBots");
            var crew = (Array)Get(setup, "players");
            for (int i = 0; i < players; i++) { var player = crew.GetValue(i); var field = player.GetType().GetField("difficulty"); field.SetValue(player, Enum.Parse(field.FieldType, "Hard")); }
            var catalog = Get(Get(match, "atmosphere"), "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>();
            Call(catalog, "Select", Get(entries.Single(entry => (string)Get(entry, "displayName") == "Clear"), "id"));
            Assert.That(Call(match, "StartMatch"), Is.True); Time.timeScale = 4; float until = Time.realtimeSinceStartup + 120; var journal = Get(match, "ScoreJournal");
            while ((int)Get(journal, "Count") == 0 && !(bool)Get(match, "Finished") && Time.realtimeSinceStartup < until) yield return new WaitForFixedUpdate();
            Assert.That(Get(journal, "Count"), Is.GreaterThan(0), "Real AI/NWH driving must reach the authoritative scoring transition.");
            var receipt = Get(journal, "Latest"); int slot = (int)Get(receipt, "Slot"); var points = (int[])Get(Get(match, "rules"), "points");
            Assert.That(Get(receipt, "Unit"), Is.EqualTo(unit)); Assert.That(Get(receipt, "Total"), Is.EqualTo((double)points[slot])); Assert.That(Get(receipt, "Delta"), Is.GreaterThan(0d));
            if (scene == "TruckJousting") Assert.That(Get(match, "ActualStrikes"), Is.GreaterThan(0));
            else Assert.That(Get(match, "CompletedCrossings"), Is.GreaterThan(0));
            ((Behaviour)match).enabled = false; Time.timeScale = 1; var before = points.ToArray();
            var presenter = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation")); Assert.That(presenter, Is.Not.Null); until = Time.realtimeSinceStartup + 8;
            while (Get(presenter, "LastPresented") == null && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(Get(presenter, "LastPresented"), Is.Not.Null); CollectionAssert.AreEqual(before, points);
            ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null;
            Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); Assert.That(points.All(value => value == 0), Is.True); Assert.That(Get(presenter, "Presented"), Is.Zero);
            TestContext.WriteLine("ACTUAL_TOURNAMENT_AWARD " + scene + " genuine AI/NWH scoring and Heat presentation; no score, placement or clock injection; rematch clears receipts.");
        }
        finally { if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } Time.timeScale = 1; UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
