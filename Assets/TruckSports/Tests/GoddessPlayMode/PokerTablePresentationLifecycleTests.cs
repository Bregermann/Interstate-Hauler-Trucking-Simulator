using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PokerTablePresentationLifecycleTests
{
    static object Get(object t, string n) => t.GetType().GetField(n)?.GetValue(t) ?? t.GetType().GetProperty(n)?.GetValue(t);
    static object Call(object t, string n, params object[] a) => t.GetType().GetMethod(n, BindingFlags.Instance | BindingFlags.Public).Invoke(t, a);
    [UnityTest] public IEnumerator AiPresenceMovesOnlyVisualPivotsAndPausesAndResets()
    {
        var prefsType = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true); var scope = (IDisposable)prefsType.GetMethod("IsolateValidation").Invoke(null, new object[] { "PokerTablePresenceNative" });
        try
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckPoker/Scenes/TruckPoker.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("TruckPoker");
#endif
            yield return null;
            var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckPoker.PokerMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var setup = Get(m, "setup"); setup.GetType().GetField("count").SetValue(setup, 8); Call(m, "AllBots"); Assert.That(Call(m, "StartMatch"), Is.True);
            var avatars = (Transform[])Get(m, "avatars"); var positions = avatars.Select(t => t.position).ToArray(); var rotations = avatars.Select(t => t.rotation).ToArray(); var p = Get(m, "presentation"); var presence = Get(p, "presence"); var pivots = (Transform[])Get(presence, "pivots"); var before = pivots.Select(t => t.localRotation).ToArray();
            yield return new WaitForSeconds(.9f); Assert.That(pivots.Where((t, i) => Quaternion.Angle(before[i], t.localRotation) > .1f).Count(), Is.GreaterThan(3));
            var hud = Get(p, "tableHud");
            foreach (var seat in (Array)Get(hud, "broadcastSeats"))
            {
                var label = (Component)Get(seat, "stack"); var mesh = (Mesh)Get(label, "mesh");
                Assert.That(mesh.vertexCount, Is.GreaterThan(0), "A public chip total must have visible rendered glyph geometry, not merely a text string.");
            }
            for (int i = 0; i < 8; i++) { Assert.That(avatars[i].position, Is.EqualTo(positions[i])); Assert.That(avatars[i].rotation, Is.EqualTo(rotations[i])); }
            Call(m, "Pause", 0, false); var paused = pivots.Select(t => t.localRotation).ToArray(); yield return new WaitForSecondsRealtime(.2f); for (int i = 0; i < 8; i++) Assert.That(pivots[i].localRotation, Is.EqualTo(paused[i]));
            Call(m, "Resume"); Call(m, "SetupScreen"); yield return null; foreach (var pivot in pivots) { Assert.That(pivot.localPosition, Is.EqualTo(Vector3.zero)); Assert.That(Quaternion.Angle(pivot.localRotation, Quaternion.identity), Is.LessThan(.001f)); }
            Assert.That(Call(m, "StartMatch"), Is.True); Assert.That((int)Get(Get(m, "Table"), "HandNumber"), Is.EqualTo(1)); Call(m, "SetupScreen");
        }
        finally { Time.timeScale = 1; scope.Dispose(); }
    }
}
