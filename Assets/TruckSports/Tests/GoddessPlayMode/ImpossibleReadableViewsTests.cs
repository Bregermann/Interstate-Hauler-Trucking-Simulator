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

public sealed class ImpossibleReadableViewsTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(owner, args);

    [UnityTest, Timeout(120000)]
    public IEnumerator YogaFourMatsAndBullActualPrimaryRemainReadableInCloserSavedViews()
    {
        var lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ReadableImpossible" + Guid.NewGuid().ToString("N") });
        var isolation = new GameObject("Readability native physical input isolation");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            foreach (string scene in new[] { "TruckYoga", "TruckMechanicalBull" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindAnyObjectByType(Sports("SportsEventSession"));
                Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                float until = Time.realtimeSinceStartup + 5;
                while (Time.realtimeSinceStartup < until) yield return null;
                string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/ReadableViewsV2"); Directory.CreateDirectory(folder);
                foreach (int width in new[] { 1280, 1920 })
                    Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5)
                        .Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, Path.Combine(folder, scene + "-" + width + "-controlled.png"), null });
                foreach (Camera camera in ((Camera[])Get(match, "cameras")).Where(c => c.enabled))
                {
                    var framing = camera.GetComponent(Sports("SportsBroadcastFraming")); Assert.That(framing, Is.Not.Null);
                    Assert.That((bool)Get(framing, "useSharedDefaults"), Is.False, "Only the sport-owned configuration changes.");
                    Assert.That((float)Get(framing, "ProjectedTruckHeight"), Is.GreaterThanOrEqualTo(scene == "TruckYoga" ? .117f : .137f), scene + " actual primary projection");
                    int primary = (int)Get(framing, "Primary");
                    int count = scene == "TruckYoga" ? 4 : 1;
                    for (int i = 0; i < count; i++)
                    {
                        int slot = count == 1 ? primary : i;
                        var rect = (Rect)Call(framing, "ProjectedTruckRect", camera, slot);
                        Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(.015f), scene + " slot" + slot + " left");
                        Assert.That(rect.xMax, Is.LessThanOrEqualTo(.985f), scene + " slot" + slot + " right");
                        Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(.20f), scene + " slot" + slot + " above score row");
                        Assert.That(rect.yMax, Is.LessThanOrEqualTo(.90f), scene + " slot" + slot + " below title");
                        TestContext.WriteLine("IMPOSSIBLE_READABLE " + scene + " slot=" + slot + " rect=" + rect.ToString("R"));
                    }
                }
                Call(match, "SetupScreen"); match = null;
            }
            TestContext.WriteLine("Controlled native presentation assertions and four images; Windows, physical-controller and human verification remain unperformed.");
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); }
    }
}
#endif
