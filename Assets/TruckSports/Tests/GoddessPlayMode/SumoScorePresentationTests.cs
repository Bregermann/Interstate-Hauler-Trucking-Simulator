#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SumoScorePresentationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);
    [UnityTest, Timeout(90000)]
    public IEnumerator RealKeyboardBumperPushPublishesOneStableReceiptAndHeatSurvivalStaysSeparate()
    {
        var previousInput = InputSystem.settings; var fixtureInput = UnityEngine.Object.Instantiate(previousInput);
        fixtureInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        fixtureInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; fixtureInput.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = fixtureInput;
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SumoScoreNative" });
        var isolation = new GameObject("Explicit Sumo score receipt regression"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var keyboard = InputSystem.AddDevice<Keyboard>(); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSumo/Scenes/TruckSumo.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(component => component.GetType().Name == "SumoMatch");
            var presenter = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsScorePresentation")); Assert.That(presenter, Is.Not.Null, "Saved real Heat score presentation installed");
            var setup = Get(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(match, "AllBots");
            var players = (Array)Get(setup, "players");
            var defaults = (Array)Type.GetType("LWS.TruckBowling.BowlingSetup, Assembly-CSharp", true).GetMethod("Defaults").Invoke(null, null);
            for (int i = 0; i < 4; i++)
            {
                var player = players.GetValue(i); var basic = defaults.GetValue(i);
                player.GetType().GetField("source").SetValue(player, Enum.Parse(player.GetType().GetField("source").FieldType, "Keyboard"));
                foreach (string key in new[] { "forward", "back", "left", "right" }) player.GetType().GetField(key).SetValue(player, basic.GetType().GetField(key).GetValue(basic));
                if (i < 2) player.GetType().GetField("name").SetValue(player, "Same Name");
            }
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            var sourceReason = new object[] { null };
            Assert.That((bool)match.GetType().GetMethod("ValidateSources").Invoke(match, sourceReason), Is.True, "Fixture source configuration: " + sourceReason[0]);
            Assert.That((bool)Get(match, "DevicesReady"), Is.True, "Fixture keyboard=" + Keyboard.current?.deviceId + " enabled=" + Keyboard.current?.enabled + " added=" + keyboard.added);
            Assert.That(Call(match, "StartMatch"), Is.True); yield return Fight(match);
            var fighters = (Array)Get(match, "fighters"); Place(fighters, 0, new Vector3(0, .7f, 5.8f), 0); Place(fighters, 1, new Vector3(0, .7f, 9.6f), 0); Place(fighters, 2, new Vector3(-6, .7f, -5), 0); Place(fighters, 3, new Vector3(-6, .7f, 5), 0);
            var rules = Get(match, "rules"); var alive = (bool[])Get(rules, "alive"); var points = (int[])Get(rules, "points");
            SendState(keyboard, new KeyboardState((Key)Get(players.GetValue(0), "forward")));
            float until = Time.realtimeSinceStartup + 10; while (alive[1] && Time.realtimeSinceStartup < until) yield return null;
            var pushingBody = (Rigidbody)Get(Get(fighters.GetValue(0), "truck"), "body"); var victimBody = (Rigidbody)Get(Get(fighters.GetValue(1), "truck"), "body");
            string contactTrace = "ownedKeyboard=" + keyboard.deviceId + "/" + keyboard.enabled + " current=" + Keyboard.current?.deviceId + "/" + Keyboard.current?.enabled + " held=" + keyboard[(Key)Get(players.GetValue(0), "forward")].isPressed + " drive=" + Call(match, "ReadDrive", 0) + " command=" + ((Vector2[])Get(match, "commands"))[0] + " pusher=" + pushingBody.position + " victim=" + victimBody.position + " contacts=" + Get(match, "QualifyingContacts") + " paused=" + Get(match, "Paused") + " stage=" + Get(match, "Stage");
            TestContext.WriteLine(contactTrace);
            SendState(keyboard, new KeyboardState()); Assert.That(alive[1], Is.False, "Actual keyboard-driven NWH bumper push crosses the ring; " + contactTrace); Assert.That(points[0], Is.EqualTo(1));
            var journal = Get(match, "ScoreJournal"); var receipt = Get(journal, "Latest");
            Assert.That(Get(journal, "Count"), Is.EqualTo(1)); Assert.That(Get(receipt, "ParticipantId"), Is.EqualTo(Get(players.GetValue(0), "participantId"))); Assert.That(Get(receipt, "ParticipantId"), Is.Not.EqualTo(Get(players.GetValue(1), "participantId")));
            Assert.That(Get(receipt, "SportId"), Is.EqualTo("TruckSumo")); Assert.That(Get(receipt, "Delta"), Is.EqualTo(1d)); Assert.That(Get(receipt, "Total"), Is.EqualTo(1d));
            until = Time.realtimeSinceStartup + 4; while ((int)Get(presenter, "Presented") == 0 && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(Get(presenter, "Presented"), Is.EqualTo(1), "Existing Heat animation consumes the real award"); Assert.That(points[0], Is.EqualTo(1), "Animation never creates another point");
            Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return Fight(match);
            Place(fighters, 0, new Vector3(0, .7f, 0), 0); Place(fighters, 1, new Vector3(0, .7f, 10), 0); Place(fighters, 2, new Vector3(10, .7f, 0), 90); Place(fighters, 3, new Vector3(0, .7f, -10), 180);
            SendState(keyboard, new KeyboardState((Key)Get(players.GetValue(1), "forward"), (Key)Get(players.GetValue(2), "forward"), (Key)Get(players.GetValue(3), "forward")));
            until = Time.realtimeSinceStartup + 10; while (Get(match, "Stage").ToString() != "Replay" && Time.realtimeSinceStartup < until) yield return null;
            SendState(keyboard, new KeyboardState());
            Assert.That(points[0], Is.EqualTo(3)); journal = Get(match, "ScoreJournal"); receipt = Get(journal, "Latest");
            Assert.That(Get(journal, "Count"), Is.EqualTo(1)); Assert.That(Get(receipt, "Delta"), Is.EqualTo(3d)); Assert.That(Get(receipt, "RoundEnded"), Is.True); Assert.That(Get(receipt, "Tier").ToString(), Is.EqualTo("RoundWinner"));
            var sealedPoints = points.ToArray(); Call(Get(match, "replay"), "Seek", 0f); CollectionAssert.AreEqual(sealedPoints, points); Assert.That(Get(journal, "Count"), Is.EqualTo(1), "Replay does not republish awards");
            Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null;
            Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero); Assert.That(points.All(value => value == 0), Is.True); Assert.That(Get(presenter, "Presented"), Is.Zero, "Rematch clears presentation and receipt state");
        }
        finally { if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1; if (keyboard.added) InputSystem.RemoveDevice(keyboard); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); InputSystem.settings = previousInput; UnityEngine.Object.DestroyImmediate(fixtureInput); }
    }
    static void SendState(Keyboard keyboard, KeyboardState state)
    {
        keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, state);
    }
    static IEnumerator Fight(Component match)
    {
        float until = Time.realtimeSinceStartup + 7; while (Get(match, "Stage").ToString() != "Fight" && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(Get(match, "Stage").ToString(), Is.EqualTo("Fight")); yield return new WaitForSeconds(.2f);
    }
    static void Place(Array fighters, int slot, Vector3 point, float yaw) { Call(fighters.GetValue(slot), "ResetFighter", point, Quaternion.Euler(0, yaw, 0)); Physics.SyncTransforms(); }
}
#endif
