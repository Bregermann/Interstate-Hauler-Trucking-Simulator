using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CheeseHudLifecycleTests
{
    static object Get(object t, string n) => t.GetType().GetField(n)?.GetValue(t) ?? t.GetType().GetProperty(n)?.GetValue(t);
    static object Call(object t, string n, params object[] args) => t.GetType().GetMethod(n).Invoke(t, args);
    [UnityTest] public IEnumerator OriginalRaceOwnsScoresPosePauseAndRematchWhileHudOnlyReads()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true); var scope = (IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "CheeseHudNative" });
        try
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("TruckCheeseRolling");
#endif
            yield return null;
            var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckCheeseRolling.CheeseMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var setup = Get(m, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(m, "AllAI"); Assert.That(Call(m, "StartMatch"), Is.True);
            yield return null; var menus = Get(m, "menus"); var hud = Get(menus, "presentation"); var trucks = ((Array)Get(m, "trucks")).Cast<Component>().ToArray(); var poses = trucks.Select(t => t.transform.position).ToArray(); var totals = ((int[])Get(m, "totals")).ToArray();
            Call(hud, "Refresh"); CollectionAssert.AreEqual(totals, (int[])Get(m, "totals")); CollectionAssert.AreEqual(poses, trucks.Select(t => t.transform.position).ToArray());
            var view = ((Array)Get(hud, "views")).GetValue(0); Assert.That((string)Get(Get(view, "catches"), "text"), Is.EqualTo("0/5")); Assert.That(((UnityEngine.UI.Image)Get(view, "progress")).fillAmount, Is.Zero);
            Call(m, "Pause", 0, false); float elapsed = (float)Get(m, "Elapsed"); yield return new WaitForSecondsRealtime(.2f); Assert.That((float)Get(m, "Elapsed"), Is.EqualTo(elapsed)); Assert.That(Time.timeScale, Is.Zero); Call(m, "Resume"); Assert.That(Time.timeScale, Is.EqualTo(1));
            Call(m, "ChangeSetup"); Assert.That(Call(m, "StartMatch"), Is.True); yield return null; Assert.That(((int[])Get(m, "totals")).All(n => n == 0), Is.True); Assert.That((int)Get(m, "Round"), Is.EqualTo(1)); Assert.That((int)Get(m, "ExplosionCount"), Is.Zero); Call(m, "ChangeSetup");
        }
        finally { Time.timeScale = 1; scope.Dispose(); }
    }
}
