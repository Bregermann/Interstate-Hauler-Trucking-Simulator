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

public sealed class TeamScoreLifecycleTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);

    [UnityTest, Timeout(120000)]
    public IEnumerator ActualCourtContactCreditsTheTeamAndRetainsItsIdentityThroughResults()
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ActualTeamScore" });
        var isolation = new GameObject("Explicit team scoring contact fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckVolleyball/Scenes/TruckVolleyball.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); Set(match, "teamA", 2); Set(match, "teamB", 2); Call(match, "NormalizeSetup"); Call(match, "AllBots");
            var players = (Array)Get(Get(match, "setup"), "players");
            for (int i = 0; i < 4; i++) Set(players.GetValue(i), "name", "Same Driver Name");
            Assert.That(Call(match, "StartMatch"), Is.True); Time.timeScale = 4;
            float deadline = Time.realtimeSinceStartup + 10;
            while ((float)Get(match, "Countdown") > 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That((float)Get(match, "Countdown"), Is.Zero);
            var journal = Get(match, "ScoreJournal"); Assert.That(Get(journal, "Count"), Is.Zero);
            // Physical placement fixture: the real ball falls onto the real court.
            // No rule scoring method, score, winner or clock is assigned.
            var ball = (Rigidbody)Get(match, "ball"); ball.isKinematic = false; ball.position = new Vector3(25, 6, 38); ball.linearVelocity = Vector3.down * 2; ball.angularVelocity = Vector3.zero; Physics.SyncTransforms();
            deadline = Time.realtimeSinceStartup + 10;
            while ((int)Get(journal, "Count") == 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(Get(journal, "Count"), Is.EqualTo(1), "Real floor contact must commit exactly one rally point.");
            var receipt = Get(journal, "Latest"); Assert.That(Get(receipt, "IsTeam"), Is.True); Assert.That(Get(receipt, "RecipientId"), Is.EqualTo("team:blue"));
            Assert.That(Get(receipt, "DisplayName"), Is.EqualTo("BLUE TEAM")); Assert.That(Get(receipt, "ParticipantId"), Is.Null); Assert.That(Get(receipt, "Slot"), Is.EqualTo(-1));
            var points = (int[])Get(Get(match, "rules"), "points"); Assert.That(points[0], Is.EqualTo(1)); Assert.That(points[1], Is.Zero); Assert.That(Get(receipt, "Total"), Is.EqualTo(1d));
            Call(match, "Landing"); Assert.That(Get(journal, "Count"), Is.EqualTo(1), "Resting contact cannot publish a second team receipt.");
            // Explicit lifecycle fixture, not a complete-match claim: results
            // open immediately after the genuine scored physical contact.
            Call(match, "EndMatch"); Time.timeScale = 1;
            var presenter = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation")); Assert.That(presenter, Is.Not.Null);
            deadline = Time.realtimeSinceStartup + 8;
            while (Get(presenter, "LastPresented") == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get(presenter, "LastPresented"), Is.SameAs(receipt));
            string text = (string)Get(Get(presenter, "notification"), "notificationText");
            Assert.That(text, Does.Contain("BLUE TEAM")); Assert.That(text, Does.Not.Contain("Same Driver Name"));
            Assert.That(((CanvasGroup)Get(presenter, "visibility")).alpha, Is.EqualTo(1)); Assert.That(points, Is.EqualTo(new[] { 1, 0 }));
            Call(match, "Landing"); Assert.That(Get(journal, "Count"), Is.EqualTo(1), "Results freeze late physical score reports too.");
            string oldMatch = (string)Get(match, "MatchId"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null;
            Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(oldMatch)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); Assert.That(Get(presenter, "Presented"), Is.Zero);
            Assert.That(points, Is.EqualTo(new[] { 0, 0 }));
            TestContext.WriteLine("ACTUAL_TEAM_SCORE real court/ball contact; one blue team receipt without player credit; duplicate-name crew, results, late lock and fresh rematch.");
        }
        finally { if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1; UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
