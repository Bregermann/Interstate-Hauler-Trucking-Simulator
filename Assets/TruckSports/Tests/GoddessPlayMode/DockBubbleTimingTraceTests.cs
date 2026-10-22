using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[DefaultExecutionOrder(1000)] public sealed class DockBubbleTimingRecorder : MonoBehaviour
{
    public Component slang; public object match; public RectTransform bubble, canvas; public CanvasGroup group; public Rigidbody body; public Camera camera;
    public readonly List<Vector4> samples = new List<Vector4>();
    public bool complete; int launch = -1;
    void LateUpdate()
    {
        if (slang == null || complete) return;
        if (group.alpha < .9f || !(bool)Read(match, "Live") || (int)Convert.ChangeType(Read(match, "Phase"), typeof(int)) != 1) { if (samples.Count > 20) complete = true; return; }
        int current = (int)Read(match, "Launches"); if (launch < 0) launch = current; if (current != launch) { complete = true; return; }
        var p = camera.WorldToViewportPoint(body.position + Vector3.up * 2.1f); if (p.z <= 0) return;
        var want = (Vector2)slang.GetType().GetMethod("PositionInViewport").Invoke(null, new object[] { canvas.rect, camera.rect, bubble.rect.size, new Vector2(p.x, p.y) });
        samples.Add(new Vector4(bubble.anchoredPosition.x, bubble.anchoredPosition.y, want.x, want.y));
    }
    public static object Read(object t, string n) => t.GetType().GetField(n)?.GetValue(t) ?? t.GetType().GetProperty(n)?.GetValue(t);
}
public sealed class DockBubbleTimingTraceTests
{
    static object Get(object t, string n) => DockBubbleTimingRecorder.Read(t, n);
    [UnityTest] public IEnumerator TraceActualFlightBubbleAfterFinalBroadcastCameraWithoutChangingGameRules()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true); var scope = (IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "DockBubbleTimingNative" }); GameObject recorder = null;
        try
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckDockDiving/Scenes/TruckDockDiving.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("TruckDockDiving");
#endif
            yield return null; var match = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckDockDiving.DockMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var slang = ((Component)match).GetComponent(Type.GetType("LWS.TruckDockDiving.DockSlang, Assembly-CSharp", true));
            var setup = Get(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); match.GetType().GetMethod("AllBots").Invoke(match, null); Assert.That(match.GetType().GetMethod("StartMatch").Invoke(match, null), Is.True);
            recorder = new GameObject("Explicit post-camera Dock bubble timing trace"); var trace = recorder.AddComponent<DockBubbleTimingRecorder>(); trace.match = match; trace.slang = slang; trace.bubble = (RectTransform)Get(slang, "bubble"); trace.canvas = (RectTransform)trace.bubble.GetComponentInParent<Canvas>().transform; trace.group = (CanvasGroup)Get(slang, "group"); trace.body = (Rigidbody)Get(Get(match, "truck"), "body"); trace.camera = (Camera)Get(Get(match, "broadcast"), "view");
            float until = Time.realtimeSinceStartup + 40; while (!trace.complete && trace.samples.Count < 120 && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(trace.samples.Count, Is.GreaterThan(20), "Observe a real airborne bubble rather than forcing a flight or score.");
            var errors = trace.samples.Select(v => Vector2.Distance(new Vector2(v.x, v.y), new Vector2(v.z, v.w))).ToArray();
            float jerk = 0, rawJerk = 0; for (int i = 2; i < trace.samples.Count; i++) { var v = trace.samples[i] - 2 * trace.samples[i - 1] + trace.samples[i - 2]; jerk += v.x * v.x + v.y * v.y; rawJerk += v.z * v.z + v.w * v.w; }
            Assert.That(jerk, Is.LessThan(rawJerk * .65f), "Bubble tracking must attenuate the measured raw projection jitter instead of passing it straight into the caption.");
            string directory = "Logs/TruckSports/PlaytestRepair"; Directory.CreateDirectory(directory); File.WriteAllText(directory + "/dock-bubble-timing-trace.txt", "samples=" + errors.Length + " afterCameraMeanError=" + errors.Average().ToString("F3") + " afterCameraMaxError=" + errors.Max().ToString("F3") + " actualJerkRms=" + Mathf.Sqrt(jerk / (errors.Length - 2)).ToString("F3") + " postCameraTargetJerkRms=" + Mathf.Sqrt(rawJerk / (errors.Length - 2)).ToString("F3") + "\nActual native flight; no score, camera or physics modification.\n");
            Debug.Log(File.ReadAllText(directory + "/dock-bubble-timing-trace.txt")); match.GetType().GetMethod("SetupScreen").Invoke(match, null);
        }
        finally { if (recorder != null) UnityEngine.Object.Destroy(recorder); Time.timeScale = 1; scope.Dispose(); }
    }
}
