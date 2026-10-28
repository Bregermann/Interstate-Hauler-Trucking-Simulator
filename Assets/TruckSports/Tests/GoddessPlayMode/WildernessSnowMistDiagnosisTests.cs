#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class WildernessSnowMistDiagnosisTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object o, string name) => o.GetType().GetField(name, Any)?.GetValue(o) ?? o.GetType().GetProperty(name, Any).GetValue(o);
    static object Call(object o, string method, params object[] args) => o.GetType().GetMethods(Any).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(o, args);
    [Serializable] sealed class Pixels { public string stage; public int samples, white; public float luminance, deviation; public bool mistForcedOff; public int mistParticles; }
    [Serializable] sealed class Report { public string scope = "CONTROLLED_NATIVE_PAIRED_MIST_RENDER_DIAGNOSIS_NOT_WINDOWS_OR_PRODUCTION_ACCEPTANCE", scene, preset; public float intensity, mistGain, mistThreshold; public Pixels[] frames; }

    [Explicit("Paired native render diagnosis only; no production configuration or vendor assets are edited.")]
    [UnityTest, Timeout(120000)] public IEnumerator HeavySnowDayMistBeforeHiddenAndRestoredOnOriginalCampingScene()
    {
        IDisposable prefs = null; GameObject isolation = null; Component match = null; Renderer mist = null; bool originalOff = false;
        try
        {
            prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "WildernessMist" + Guid.NewGuid().ToString("N") });
            isolation = new GameObject("Explicit snow mist diagnostic input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCamping/Scenes/TruckCamping.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); Call(match, "AllBots");
            var weather = (Component)Get(match, "presentationWeather"); var catalog = Get(weather, "presets");
            var preset = ((Array)Get(catalog, "entries")).Cast<object>().Single(p => (string)Get(p, "displayName") == "Heavy Snow");
            Call(catalog, "Select", Get(preset, "id")); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Day") });
            Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(8);
            Call(match, "Pause", 0, false); yield return new WaitForSecondsRealtime(.3f); Assert.That(Get(match, "Paused"), Is.True);
            var snow = (Component)Get(Get(weather, "precipitation"), "SnowScript"); var particles = (ParticleSystem)Get(snow, "MistParticleSystem");
            Assert.That(particles, Is.Not.Null, "The actual installed snow emitter must own a real mist system."); mist = particles.GetComponent<Renderer>(); Assert.That(mist, Is.Not.Null);
            originalOff = mist.forceRenderingOff; Assert.That(originalOff, Is.False); Assert.That(particles.particleCount, Is.GreaterThan(0), "Reproduce actual live heavy-snow mist before interpreting any pixels.");
            var cameras = (Camera[])Get(match, "cameras"); var camera = cameras.First(c => c != null && c.isActiveAndEnabled && c.targetTexture == null);
            var bodies = match.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Rigidbody>(true)).Select(b => (body: b, p: b.position, q: b.rotation, v: b.linearVelocity, a: b.angularVelocity)).ToArray();
            string folder = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/WildernessSnowMistDiagnosis/" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            var frames = new System.Collections.Generic.List<Pixels>();
            foreach (string phase in new[] { "original-heavy-snow", "only-mist-render-hidden", "original-mist-restored" })
            {
                mist.forceRenderingOff = phase == "only-mist-render-hidden";
                frames.Add(Read(camera, phase, mist.forceRenderingOff, particles.particleCount));
                Runtime("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5).Invoke(null, new object[] { match, 1280, 720, Path.Combine(folder, phase + "-controlled-720.png"), null });
                Assert.That(mist.forceRenderingOff, Is.EqualTo(phase == "only-mist-render-hidden"), "Vendor visibility callbacks must not silently undo the paired render condition.");
                foreach (var body in bodies) { Assert.That(body.body.position, Is.EqualTo(body.p)); Assert.That(body.body.rotation, Is.EqualTo(body.q)); Assert.That(body.body.linearVelocity, Is.EqualTo(body.v)); Assert.That(body.body.angularVelocity, Is.EqualTo(body.a)); }
            }
            var report = new Report { scene = "TruckCamping", preset = (string)Get(weather, "PresetId"), intensity = (float)Get(snow, "Intensity"), mistGain = (float)Get(snow, "MistIntensityMultiplier"), mistThreshold = (float)Get(snow, "MistThreshold"), frames = frames.ToArray() };
            File.WriteAllText(Path.Combine(folder, "paired-render-observations.json"), JsonUtility.ToJson(report, true)); TestContext.WriteLine("NATIVE_DIAGNOSTIC_ORIGINALS " + folder + " " + JsonUtility.ToJson(report));
        }
        finally
        {
            if (mist != null) mist.forceRenderingOff = originalOff;
            if (match != null) Call(match, "SetupScreen");
            if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
            prefs?.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); Time.timeScale = 1;
        }
    }
    static Pixels Read(Camera camera, string stage, bool off, int count)
    {
        var prior = camera.targetTexture; var active = RenderTexture.active; var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); Texture2D image = null;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply();
            var values = image.GetPixels().Where((p, i) => i / 320 > 25 && i / 320 < 150).ToArray(); float mean = values.Average(p => p.grayscale);
            return new Pixels { stage = stage, samples = values.Length, white = values.Count(p => p.r > .9f && p.g > .9f && p.b > .9f), luminance = mean,
                deviation = Mathf.Sqrt(values.Average(p => (p.grayscale - mean) * (p.grayscale - mean))), mistForcedOff = off, mistParticles = count };
        }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); if (image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
