#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BowlingHudLifecycleTests
{
    static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
    static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
    [UnityTest]
    public IEnumerator LaneHudUsesCurrentOwnersAndFollowsExistingPauseAndRematchLifecycle()
    {
        using ((IDisposable)Type("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "BowlingHudNative" }))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBowling/Scenes/TruckBowling.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Type("LWS.TruckBowling.BowlingMatch"), FindObjectsSortMode.None).Single();
                Call(match, "PresetAI", 4, Enum.Parse(Type("LWS.TruckBowling.BowlingMode"), "Simultaneous")); Type("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { true }); Assert.That((bool)Call(match, "StartMatch"), Is.True); Time.timeScale = 0;
                var menus = Field(match, "menus"); var ui = Field(menus, "presentation"); var players = (Array)Field(Field(match, "setup"), "players");
                for (int i = 0; i < 4; i++)
                {
                    Call(ui, "RefreshLane", i); var score = Field(players.GetValue(i), "score"); Assert.That((int)score.GetType().GetProperty("Deliveries").GetValue(score), Is.Zero);
                    var header = ((Array)Field(menus, "headers")).GetValue(i); Assert.That(header.GetType().GetProperty("text").GetValue(header), Is.EqualTo(Field(players.GetValue(i), "name")));
                }
                Call(match, "Pause", 0, false); Assert.That(((Array)Field(menus, "viewRoots")).Cast<RectTransform>().All(r => !r.gameObject.activeSelf), Is.True);
                Call(match, "Resume"); Assert.That(((Array)Field(menus, "viewRoots")).Cast<RectTransform>().All(r => r.gameObject.activeSelf), Is.True);
                Call(match, "ChangeSetup"); Assert.That((bool)Call(match, "StartMatch"), Is.True); for (int i = 0; i < 4; i++) Call(ui, "RefreshLane", i);
                Assert.That(((Array)Field(ui, "totals")).Cast<object>().All(t => (string)t.GetType().GetProperty("text").GetValue(t) == "0"), Is.True);
            }
            finally { Time.timeScale = 1; }
        }
    }
}
#endif
