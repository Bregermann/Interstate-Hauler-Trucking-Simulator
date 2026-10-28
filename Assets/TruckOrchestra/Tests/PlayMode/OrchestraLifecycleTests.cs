#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using LWS.TruckDanceOff;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckOrchestra.Tests
{
    public sealed class OrchestraLifecycleTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Get(object item, string name) => item.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(item) : item.GetType().GetProperty(name, Any).GetValue(item);
        static object Call(object item, string name, params object[] args) => item.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(item, args);
        static void Set(object item, string name, object value) => item.GetType().GetField(name, Any).SetValue(item, value);
        [UnityTest, Timeout(140000)]
        public IEnumerator OneThroughFourRealKeyboardOwnersOnlySoundTheirAssignedInstrumentAndChart()
        {
            var prefs = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "OrchestraOwners" + Guid.NewGuid().ToString("N") });
            var original = InputSystem.settings; var scoped = UnityEngine.Object.Instantiate(original); scoped.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; scoped.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = scoped;
            var isolation = new GameObject("Orchestra owned keyboard isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            Component match = null; Keyboard keyboard = null;
            Key[][] keys = { new[] { Key.A, Key.S, Key.W, Key.D }, new[] { Key.J, Key.K, Key.I, Key.L }, new[] { Key.F, Key.G, Key.T, Key.H }, new[] { Key.LeftArrow, Key.DownArrow, Key.UpArrow, Key.RightArrow } };
            try
            {
                for (int count = 1; count <= 4; count++)
                {
                    EditorSceneManager.LoadSceneInPlayMode("Assets/TruckOrchestra/Scenes/TruckOrchestra.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                    match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckOrchestra.OrchestraMatch"));
                    if (keyboard == null) keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                    var setup = Get(match, "setup"); Set(setup, "count", count); var players = (Array)Get(setup, "players");
                    for (int i = 0; i < count; i++)
                    {
                        var player = players.GetValue(i); var source = player.GetType().GetField("source"); source.SetValue(player, Enum.Parse(source.FieldType, "Keyboard"));
                        for (int lane = 0; lane < 4; lane++) Set(player, new[] { "left", "back", "forward", "right" }[lane], keys[i][lane]); players.SetValue(player, i);
                    }
                    Assert.That((bool)Call(match, "StartMatch"), Is.True); var rules = (DanceRules[])Get(match, "Rules");
                    var pressed = new bool[count]; var released = new bool[count]; var held = new bool[count]; float deadline = Time.realtimeSinceStartup + 16;
                    while (!released.All(value => value) && Time.realtimeSinceStartup < deadline)
                    {
                        double now = (double)Get(match, "SongTime"); bool changed = false;
                        for (int i = 0; i < count; i++)
                        {
                            var note = rules[i].Note(0);
                            if (!pressed[i] && now >= note.time - .015) { pressed[i] = held[i] = true; changed = true; }
                            if (held[i] && now >= note.time + Math.Max(.06, note.duration) + .03) { held[i] = false; released[i] = true; changed = true; }
                        }
                        if (changed)
                        {
                            var down = new System.Collections.Generic.List<Key>();
                            for (int i = 0; i < count; i++) if (held[i]) for (int lane = 0; lane < 4; lane++) if ((rules[i].Note(0).mask & 1 << lane) != 0) down.Add(keys[i][lane]);
                            InputSystem.QueueStateEvent(keyboard, new KeyboardState(down.ToArray())); InputSystem.Update();
                        }
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(.3f); Assert.That(released.All(value => value), Is.True);
                    var instruments = (Array)Get(match, "instruments");
                    for (int i = 0; i < 4; i++)
                    {
                        Assert.That((int)Get(instruments.GetValue(i), "RealNoteHeads"), Is.EqualTo(i < count ? 1 : 0), "Actual InputAction ownership for instrumental station" + i);
                        if (i < count) { Assert.That(rules[i].Score, Is.GreaterThan(0)); Assert.That(rules[i].Judgment(0), Is.Not.EqualTo(DanceGrade.Miss)); }
                    }
                    string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/OrchestraNativeV1"); Directory.CreateDirectory(folder);
                    foreach (int width in new[] { 1280, 1920 }) Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5)
                        .Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, Path.Combine(folder, count + "-keyboard-owners-" + width + "-controlled.png"), null });
                    Call(match, "SetupScreen"); match = null;
                }
                TestContext.WriteLine("ORCHESTRA_OWNERS real InputSystem timestamped keyboard events,1–4 separately assigned parts, actual accepted note heads and completed hold tails; no direct score/Receive injection. Controlled captures only, no hardware acceptance.");
            }
            finally
            {
                Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                UnityEngine.Object.DestroyImmediate(isolation); InputSystem.settings = original; UnityEngine.Object.DestroyImmediate(scoped); prefs.Dispose();
            }
        }
        [UnityTest, Timeout(140000)]
        public IEnumerator FourActualAiPartsSoundAnimateConductFinishPauseAndRematchWithoutRhythmFork()
        {
            var prefs = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "OrchestraNative" + Guid.NewGuid().ToString("N") });
            var isolation = new GameObject("Orchestra native physical input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            Component match = null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckOrchestra/Scenes/TruckOrchestra.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckOrchestra.OrchestraMatch")); Assert.That(match, Is.Not.Null);
                Assert.That(match.GetType().BaseType.FullName, Is.EqualTo("LWS.TruckDanceOff.DanceMatch"));
                Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                var actors = (Array)Get(match, "dancers"); var instruments = (Array)Get(match, "instruments"); var ids = actors.Cast<Component>().Select(a => a.GetInstanceID()).ToArray();
                var conductor = (Component)Get(match, "conductor"); var arm = (Transform)Get(conductor, "rightArm"); Quaternion armStart = arm.localRotation;
                var rules = (DanceRules[])Get(match, "Rules"); Assert.That(rules.Length, Is.EqualTo(4));
                Assert.That(rules.Select(r => r.Note(0).time).Distinct().Count(), Is.EqualTo(4), "Actual participants own distinct instrumental parts.");
                bool paused = false, captured = false, moved = false; float deadline = Time.realtimeSinceStartup + 85;
                while (!(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    moved |= Quaternion.Angle(armStart, arm.localRotation) > 2;
                    if (!paused && (int)Get(match, "AudibleNotes") > 8)
                    {
                        Call(match, "Pause", 0, false); double before = (double)Get(match, "SongTime"); int heads = (int)Get(match, "AudibleNotes");
                        yield return new WaitForSecondsRealtime(.25f); Assert.That((double)Get(match, "SongTime"), Is.EqualTo(before)); Assert.That((int)Get(match, "AudibleNotes"), Is.EqualTo(heads));
                        Call(match, "Resume"); paused = true;
                    }
                    if (!captured && (int)Get(match, "AudibleNotes") > 20)
                    {
                        string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/OrchestraNativeV1"); Directory.CreateDirectory(folder);
                        foreach (int width in new[] { 1280, 1920 })
                            Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5)
                                .Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, Path.Combine(folder, "concert-" + width + "-controlled.png"), null });
                        captured = true;
                    }
                    yield return null;
                }
                Assert.That((bool)Get(match, "Finished"), Is.True, "Actual DSP concert must finish without forced outcomes."); Assert.That(paused && captured && moved, Is.True);
                Assert.That((int)Get(match, "AudibleNotes"), Is.GreaterThan(40)); Assert.That((int)Get(conductor, "ConductedBeats"), Is.GreaterThan(20));
                for (int i = 0; i < 4; i++)
                {
                    Assert.That(rules[i].Resolved, Is.EqualTo(rules[i].Count)); Assert.That(rules[i].Score, Is.GreaterThan(0));
                    Assert.That((int)Get(instruments.GetValue(i), "RealNoteHeads"), Is.GreaterThan(0)); Assert.That((int)Get(instruments.GetValue(i), "VisibleStrikes"), Is.GreaterThan(0));
                }
                Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
                Assert.That(actors.Cast<Component>().Select(a => a.GetInstanceID()).ToArray(), Is.EqualTo(ids)); Assert.That((int)Get(match, "AudibleNotes"), Is.Zero);
                TestContext.WriteLine("ORCHESTRA_NATIVE actual four independent original Dance timing parts, accepted heads/held tails, instrument strikes and conductor bones, complete DSP performance, pause/resume and retained-actor rematch. Controlled captures only; listening/human/device/Windows acceptance remains pending.");
            }
            finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
        }
    }
}
#endif
