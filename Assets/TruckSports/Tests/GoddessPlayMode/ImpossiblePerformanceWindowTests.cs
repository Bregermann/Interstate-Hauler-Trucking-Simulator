#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ImpossiblePerformanceWindowTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static readonly BindingFlags Access = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object Read(object o, string name) => o.GetType().GetField(name, Access) != null ? o.GetType().GetField(name, Access).GetValue(o) : o.GetType().GetProperty(name, Access).GetValue(o);
    [UnityTest, Timeout(60000)] public IEnumerator ExplicitNormalSpeedNonRaceWindowCollectsActualUnitySamplesWithoutOwningGameplay()
    {
        IDisposable scope = null; Component match = null; GameObject isolation = null, observerRoot = null;
        try
        {
            scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "NonRaceProfiler" + Guid.NewGuid().ToString("N") });
            isolation = new GameObject("Explicit profiler validation device isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTruckGoose/Scenes/TruckTruckGoose.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); Assert.That(match, Is.Not.Null);
            match.GetType().GetMethod("AllBots").Invoke(match, null); Assert.That((bool)match.GetType().GetMethod("StartMatch").Invoke(match, null), Is.True);
            Assert.That((float)Read(match, "RaceStartSeconds"), Is.LessThan(0), "The original racing-only profiler gate would reject this actual non-race session.");
            float originalStep = Time.fixedDeltaTime; string originalId = (string)Read(match, "MatchId");
            string folder = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ImpossiblePerformanceNative/" + Guid.NewGuid().ToString("N"));
            observerRoot = new GameObject("Existing Unity profiler wrapper explicit native window"); var observer = observerRoot.AddComponent(Runtime("SportsExpansionPerformance"));
            observer.GetType().GetMethod("BeginWindow").Invoke(observer, new object[] { match, folder });
            yield return new WaitForSecondsRealtime(6.5f);
            object receipt = observer.GetType().GetMethod("EndWindow").Invoke(observer, null);
            Assert.That((int)Read(receipt, "samples"), Is.GreaterThan(0)); Assert.That(File.Exists(Path.Combine(folder, "performance.json")), Is.True);
            Assert.That((double)Read(receipt, "frameMeanMs"), Is.GreaterThan(0)); Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(Time.fixedDeltaTime, Is.EqualTo(originalStep));
            Assert.That((string)Read(match, "MatchId"), Is.EqualTo(originalId)); Assert.That((bool)Read(match, "Live"), Is.True);
            Assert.That(observer.GetType().GetMethod("EndWindow").Invoke(observer, null), Is.SameAs(receipt), "Closing twice never creates a second receipt or starts another game.");
            TestContext.WriteLine("Actual native non-racing1x profiler samples=" + Read(receipt, "samples") + " CPU=" + Read(receipt, "cpuSamples") + " physics=" + Read(receipt, "physicsSamples") + " GPU=" + Read(receipt, "gpuSamples") + "; no Windows/hardware performance acceptance inferred.");
        }
        finally
        {
            if (observerRoot != null) UnityEngine.Object.DestroyImmediate(observerRoot);
            if (match != null) match.GetType().GetMethod("SetupScreen").Invoke(match, null);
            if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
            scope?.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); Time.timeScale = 1;
        }
    }
}
#endif
