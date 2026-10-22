#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BilliardsHudLifecycleTests
{
    static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
    static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
    [UnityTest]
    public IEnumerator ExistingSessionOwnsHudVisibilityAcrossPauseAndFreshSeries()
    {
        using ((IDisposable)Type("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "BilliardsHudNative" }))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBilliards/Scenes/TruckBilliards.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Type("LWS.TruckBilliards.BilliardsMatch"), FindObjectsSortMode.None).Single();
            var session = Field(match, "session"); Call(session, "SelectMode", Enum.Parse(Type("LWS.TruckBilliards.BilliardsMode"), "AIVsAI")); Assert.That((bool)Call(session, "StartMatch"), Is.True);
            var hud = Field(session, "hud"); var ui = Field(hud, "presentation"); var content = (GameObject)Field(ui, "content"); Call(hud, "Refresh"); Assert.That(content.activeSelf, Is.True);
            var rules = Field(match, "rules"); var phase = rules.GetType().GetProperty("Phase").GetValue(rules); var shot = rules.GetType().GetProperty("ShotId").GetValue(rules);
            for (int i = 0; i < 100; i++) Call(hud, "Refresh"); Assert.That(rules.GetType().GetProperty("Phase").GetValue(rules), Is.EqualTo(phase)); Assert.That(rules.GetType().GetProperty("ShotId").GetValue(rules), Is.EqualTo(shot));
            Call(session, "Pause"); Call(hud, "Refresh"); Assert.That(content.activeSelf, Is.False); Call(session, "Resume"); Call(hud, "Refresh"); Assert.That(content.activeSelf, Is.True);
            Call(session, "RestartSeries"); Call(hud, "Refresh"); Assert.That(content.activeSelf, Is.True); Assert.That(((int[])Field(Field(session, "series"), "Wins")).Sum(), Is.Zero);
            Time.timeScale = 1;
        }
    }
}
#endif
