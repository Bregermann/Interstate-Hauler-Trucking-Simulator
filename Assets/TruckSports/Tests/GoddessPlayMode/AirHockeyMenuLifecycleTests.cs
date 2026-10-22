#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class AirHockeyMenuLifecycleTests
{
    static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    [UnityTest]
    public IEnumerator ScrapHeapOpensOnAGameAndReturnsToTheActiveCatalogue()
    {
        using ((IDisposable)Type("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ScrapMenuNative" }))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            var front = UnityEngine.Object.FindObjectsByType(Type("LWS.TruckSports.SportsFrontEnd"), FindObjectsSortMode.None).Single(); Call(front, "Action", "scrapheap"); Assert.That((int)Property(front, "Page"), Is.EqualTo(4));
            var systemType = System.Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI", true); var system = systemType.GetProperty("current").GetValue(null); var selected = (GameObject)Property(system, "currentSelectedGameObject"); Assert.That(selected, Is.Not.Null);
            var action = selected.GetComponent(Type("LWS.TruckSports.SportsMenuAction")); Assert.That(new[] { "wallball", "logrolling", "dodgeball" }, Does.Contain((string)Field(action, "action")));
            Call(front, "Action", "scrapback"); Assert.That((int)Property(front, "Page"), Is.EqualTo(1));
        }
    }
    [UnityTest]
    public IEnumerator PresentationClockStopsOnPauseAndResetsWithTheOriginalAlternatingServe()
    {
        using ((IDisposable)Type("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "AirHockeyMenuNative" }))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckAirHockey/Scenes/TruckAirHockey.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Type("LWS.TruckAirHockey.AirHockeyMatch"), FindObjectsSortMode.None).Single(); Call(match, "Configure", Enum.Parse(Type("LWS.TruckAirHockey.AirHockeyMode"), "AIVsAI")); Assert.That((bool)Call(match, "StartMatch", false), Is.True); Time.timeScale = 4;
                float until = Time.realtimeSinceStartup + 5; while ((float)Property(match, "MatchSeconds") < .2f && Time.realtimeSinceStartup < until) yield return null;
                Assert.That((float)Property(match, "MatchSeconds"), Is.GreaterThan(0)); Call(match, "Pause", false); float clock = (float)Property(match, "MatchSeconds"); yield return new WaitForSecondsRealtime(.2f); Assert.That((float)Property(match, "MatchSeconds"), Is.EqualTo(clock));
                var menus = Field(match, "menus"); var hud = (GameObject)Field(menus, "hud"); Assert.That(hud.activeSelf, Is.False); Call(match, "Resume"); Assert.That(hud.activeSelf, Is.True);
                Assert.That((bool)Call(match, "StartMatch", true), Is.True); Assert.That((float)Property(match, "MatchSeconds"), Is.Zero); Assert.That((int)Property(Field(match, "rules"), "Server"), Is.EqualTo(1));
                var ui = Field(menus, "presentation"); Call(ui, "Refresh"); Assert.That(((Array)Field(ui, "points")).Cast<object>().All(t => (string)Property(t, "text") == "0"), Is.True);
            }
            finally { Time.timeScale = 1; }
        }
    }
}
#endif
