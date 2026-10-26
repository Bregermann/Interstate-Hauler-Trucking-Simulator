#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BaseJumpInputDiagnosticsTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [Explicit("Records the source-validator reason without changing production input rules.")]
    [UnityTest, Timeout(120000)] public IEnumerator ExplainTwoKeyboardSourceValidation()
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "EnvironmentWindows" });
        var isolation = new GameObject("Isolated Base Jump source diagnosis"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var originalSettings = InputSystem.settings; var probeSettings = UnityEngine.Object.Instantiate(originalSettings); probeSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = probeSettings;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession")).Cast<Component>().Single(); Call(match, "AllBots");
            var setup = Get(match, "setup"); var players = (Array)Get(setup, "players");
            foreach (int i in new[] { 0, 1 }) { var player = players.GetValue(i); var source = player.GetType().GetField("source"); source.SetValue(player, Enum.Parse(source.FieldType, "Keyboard")); }
            var report = new StringBuilder(); var args = new object[] { null }; var validate = match.GetType().GetMethod("ValidateSources", Any);
            bool valid = (bool)validate.Invoke(match, args); report.AppendLine("Original source-only fixture valid=" + valid + " reason=" + args[0]);
            report.AppendLine("keyboard current=" + Keyboard.current?.deviceId + " enabled=" + Keyboard.current?.enabled + " software=" + keyboard.deviceId);
            foreach (int i in new[] { 0, 1 }) { var player = players.GetValue(i); report.AppendLine("slot=" + i + " forward=" + Get(player, "forward") + " left=" + Get(player, "left") + " back=" + Get(player, "back") + " right=" + Get(player, "right") + " action=" + ((Array)Get(match, "actionKeys")).GetValue(i)); }
            var defaults = (Array)Type.GetType("LWS.TruckBowling.BowlingSetup, Assembly-CSharp", true).GetMethod("Defaults").Invoke(null, null);
            for (int i = 0; i < 2; i++) foreach (string field in new[] { "forward", "left", "back", "right" }) players.GetValue(i).GetType().GetField(field).SetValue(players.GetValue(i), Get(defaults.GetValue(i), field));
            var actions = (Array)Get(match, "actionKeys"); actions.SetValue(Enum.Parse(actions.GetType().GetElementType(), "Space"), 0); actions.SetValue(Enum.Parse(actions.GetType().GetElementType(), "RightShift"), 1);
            args[0] = null; bool corrected = (bool)validate.Invoke(match, args); report.AppendLine("Explicit independent fixture valid=" + corrected + " reason=" + args[0]); Assert.That(corrected, Is.True, args[0] as string);
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair/BaseJumpInputAudit"); File.WriteAllText("Logs/TruckSports/PlaytestRepair/BaseJumpInputAudit/observations.txt", report.ToString()); TestContext.WriteLine(report.ToString());
            Assert.That(Call(match, "StartMatch"), Is.True); yield return null;
            Assert.That((int)Get(match, "ViewCount"), Is.EqualTo(2)); report.AppendLine("Explicit independent fixture starts with two human views.");
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair/BaseJumpInputAudit"); File.WriteAllText("Logs/TruckSports/PlaytestRepair/BaseJumpInputAudit/observations.txt", report.ToString()); TestContext.WriteLine(report.ToString());
        }
        finally { if (match != null) Call(match, "SetupScreen"); if (keyboard.added) InputSystem.RemoveDevice(keyboard); InputSystem.settings = originalSettings; UnityEngine.Object.DestroyImmediate(probeSettings); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
}
#endif
