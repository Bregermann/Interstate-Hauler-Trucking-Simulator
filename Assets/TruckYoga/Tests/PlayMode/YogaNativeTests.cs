#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LWS.TruckYoga;

public sealed class YogaNativeTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x)).Invoke(owner, args);
    static object Static(string type, string name, params object[] args) => Sports(type).GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    [Serializable] sealed class LifecycleSample
    {
        public string checkpoint, menu, source, keyboard, backgroundInput, editorInput; public int frame, bank, poseSteps;
        public float real, scaled, scale, delta, fixedDelta, arrival, poseSeconds;
        public bool focused, running, live, paused, finished, ready, ownerReady, enabled, active, actionHeld, pendingAction, released;
    }
    static string progress;
    static string Trace(Component match, string checkpoint)
    {
        if (match == null) return checkpoint + " no match";
        var players = (Array)Get(Get(match, "setup"), "players"); var rules = (YogaRules)Get(match, "rules");
        var sample = new LifecycleSample { checkpoint = checkpoint, frame = Time.frameCount, real = Time.realtimeSinceStartup, scaled = Time.time, scale = Time.timeScale, delta = Time.deltaTime, fixedDelta = Time.fixedDeltaTime, focused = Application.isFocused, running = (bool)Get(match, "Running"), live = (bool)Get(match, "Live"), paused = (bool)Get(match, "Paused"), finished = (bool)Get(match, "Finished"), ready = (bool)Get(match, "DevicesReady"), ownerReady = (bool)Call(match, "Ready", 0), menu = Get(Get(match, "menus"), "Screen").ToString(), arrival = (float)Get(match, "ArrivalSeconds"), poseSeconds = rules.PoseSeconds, source = Get(players.GetValue(0), "source").ToString(), keyboard = Keyboard.current == null ? "NONE" : Keyboard.current.name + ":" + Keyboard.current.enabled + ":" + Keyboard.current.added + ":native=" + Keyboard.current.native, enabled = ((Behaviour)match).enabled, active = match.gameObject.activeInHierarchy };
        sample.backgroundInput = InputSystem.settings.backgroundBehavior.ToString(); sample.editorInput = InputSystem.settings.editorInputBehaviorInPlayMode.ToString();
        var rig = ((Array)Get(match, "rigs")).GetValue(0);
        sample.bank = (int)Get(rig, "ControlBank"); sample.poseSteps = (int)Get(rig, "PoseSteps");
        sample.actionHeld = (bool)Call(match, "ActionHeld", 0); sample.pendingAction = ((bool[])Get(match, "actions"))[0]; sample.released = (bool)Get(players.GetValue(0), "released");
        string json = JsonUtility.ToJson(sample); Directory.CreateDirectory(Path.GetDirectoryName(progress)); File.AppendAllText(progress, json + "\n"); TestContext.WriteLine("YOGA_LIFECYCLE_V3 " + json); return json;
    }
    static IEnumerator AdvanceSimulation(Component match, float seconds, float realLimit, string checkpoint)
    {
        float start = Time.time, deadline = Time.realtimeSinceStartup + realLimit, next = 0; Trace(match, checkpoint + "-begin");
        while (Time.time - start < seconds && Time.realtimeSinceStartup < deadline) { if (Time.realtimeSinceStartup >= next) { Trace(match, checkpoint + "-wait"); next = Time.realtimeSinceStartup + 1; } yield return null; }
        Assert.That(Time.time - start, Is.GreaterThanOrEqualTo(seconds), "Scaled simulation did not advance at the original lifecycle boundary: " + Trace(match, checkpoint + "-end"));
    }
    static void Capture(Component match, string label, int width = 1280, int height = 720)
    {
        Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5).Invoke(null, new object[] { match, width, height, Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ImpossibleYoga/" + label + ".png"), null });
    }
    static string OriginalConfiguration(Component truck)
    {
        var shape = (BoxCollider)Get(truck, "shape");
        return shape.GetInstanceID() + ":" + shape.center.ToString("R") + ":" + shape.size.ToString("R") + string.Join("/", truck.GetComponentsInChildren<Rigidbody>(true).Select(b => b.GetInstanceID() + ":" + b.mass)) + string.Join("/", truck.GetComponentsInChildren<ConfigurableJoint>(true).Select(j => j.GetInstanceID() + ":" + j.connectedBody.GetInstanceID() + ":" + j.anchor.ToString("R") + ":" + j.connectedAnchor.ToString("R")));
    }
    [UnityTest]
    public IEnumerator ActualSevenPoseBodiesHumanBanksInstructorAIAndNoScoreFreeClass()
    {
        var lease = (IDisposable)Static("SportsPrefs", "IsolateValidation", "ImpossibleYoga" + Guid.NewGuid().ToString("N"));
        Component match = null; GameObject isolation = null; Keyboard keyboard = null;
        var originalInputSettings = InputSystem.settings; InputSettings fixtureInputSettings = null;
        progress = Path.GetFullPath("Logs/TruckSports/Impossible07/YogaLifecycleV3/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".jsonl");
        try
        {
            fixtureInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
            fixtureInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            fixtureInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            fixtureInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = fixtureInputSettings;
            Static("SportsParticipantRegistry", "ForgetCache"); isolation = new GameObject("Yoga native owned input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); keyboard = InputSystem.AddDevice<Keyboard>();
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckYoga/Scenes/TruckYoga.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckYoga.YogaMatch, Assembly-CSharp", true)); Assert.That(match, Is.Not.Null);
            Trace(match, "scene-loaded-two-frames");
            var rigs = ((IEnumerable)Get(match, "rigs")).Cast<Component>().ToArray(); Assert.That(rigs.Length, Is.EqualTo(4));
            var trucks = rigs.Select(r => (Component)Get(r, "truck")).ToArray(); var ids = trucks.Select(t => t.GetInstanceID()).ToArray(); var configuration = trucks.Select(OriginalConfiguration).ToArray();
            var setup = Get(match, "setup"); Set(setup, "count", 1); Call(match, "AllBots"); Set(match, "option", 1);
            var players = (Array)Get(setup, "players"); var player = players.GetValue(0); Set(player, "source", Enum.ToObject(Get(player, "source").GetType(), 1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); Assert.That((bool)Call(match, "StartMatch"), Is.True); Time.timeScale = 3;
            Trace(match, "human-free-class-started");
            var rules = (YogaRules)Get(match, "rules");
            float deadline = Time.realtimeSinceStartup + 8, nextTrace = 0; while ((float)Get(match, "ArrivalSeconds") > 0 && Time.realtimeSinceStartup < deadline) { if (Time.realtimeSinceStartup >= nextTrace) { Trace(match, "arrival-wait"); nextTrace = Time.realtimeSinceStartup + 1; } yield return null; }
            Assert.That((float)Get(match, "ArrivalSeconds"), Is.LessThanOrEqualTo(0), "The original class arrival must advance. " + Trace(match, "arrival-boundary"));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update(); yield return AdvanceSimulation(match, .8f, 5, "owned-pitch-input");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null; yield return null;
            var first = (Vector4)Get(rigs[0], "Achieved"); Assert.That(first.x, Is.GreaterThan(8), "Owned keyboard pitch must visibly move the actual body.");
            // Two rendered frames can be shorter than one fixed step in an
            // unthrottled batch. Keep one ordinary press held until consumed;
            // never invoke ChangeBank or write the input latch in the fixture.
            Trace(match, "owned-bank-press-begin"); int beforeBankSteps = (int)Get(rigs[0], "PoseSteps");
            var actionKey = ((Key[])Get(match, "actionKeys"))[0];
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(actionKey)); InputSystem.Update();
            deadline = Time.realtimeSinceStartup + 2;
            while ((int)Get(rigs[0], "ControlBank") == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((int)Get(rigs[0], "ControlBank"), Is.EqualTo(1), "The genuine ACTION press must reach fixed-step pose control. " + Trace(match, "owned-bank-press-consumed"));
            Assert.That((int)Get(rigs[0], "PoseSteps"), Is.GreaterThan(beforeBankSteps));
            yield return AdvanceSimulation(match, .12f, 2, "owned-bank-held");
            Assert.That((int)Get(rigs[0], "ControlBank"), Is.EqualTo(1), "One held press must toggle only once.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return AdvanceSimulation(match, .12f, 2, "owned-bank-release");
            Assert.That((int)Get(rigs[0], "ControlBank"), Is.EqualTo(1), "Releasing ACTION must preserve the selected bank.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D)); InputSystem.Update(); yield return AdvanceSimulation(match, .7f, 5, "owned-lift-twist-input");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return AdvanceSimulation(match, .25f, 5, "owned-input-release");
            var controlled = (Vector4)Get(rigs[0], "Achieved"); Assert.That(controlled.z, Is.GreaterThan(.2f)); Assert.That(controlled.w, Is.GreaterThan(8), "Actual whole-assembly yaw must move selected body AND original wheels together; hidden-only art cannot satisfy this.");
            Assert.That((int)Get(rigs[0], "BoundAppearanceRevision"), Is.EqualTo((int)Get(Get(trucks[0], "appearanceHost"), "AppearanceRevision")));
            Assert.That(rules.score.All(v => v == 0), Is.True); Assert.That(rules.Finished, Is.False);
            Capture(match, "owned-keyboard-free-yoga-720-controlled");
            Call(match, "Pause", 0, false); yield return null; yield return null;
            Trace(match, "explicit-pause-settled");
            var body = (Rigidbody)Get(trucks[0], "body"); var paused = body.position; float seconds = rules.PoseSeconds; yield return new WaitForSecondsRealtime(.25f);
            Assert.That(body.position, Is.EqualTo(paused)); Assert.That(rules.PoseSeconds, Is.EqualTo(seconds));
            Call(match, "ExtraAction", "yoga-finish"); Assert.That((bool)Get(match, "Finished"), Is.True); Assert.That((string)Get(match, "Results"), Does.Contain("NO WINNER OR POINTS"));
            Call(match, "SetupScreen"); Set(setup, "count", 4); Set(match, "option", 0); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True); Time.timeScale = 4;
            Trace(match, "four-ai-class-started");
            var seen = new HashSet<YogaPose>(); var instructor = (Component)Get(match, "instructor"); var bones = (Transform[])Get(instructor, "bones"); var rest = bones.Select(b => b.localRotation).ToArray(); var instructorOrigin = instructor.transform.position;
            var profileIds = Enumerable.Range(0, 4).Select(i => (string)Get(players.GetValue(i), "participantId")).ToArray(); Assert.That(profileIds.Distinct().Count(), Is.EqualTo(4));
            deadline = Time.realtimeSinceStartup + 90; bool movedBones = false;
            while (!(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline)
            {
                if (Time.realtimeSinceStartup >= nextTrace) { Trace(match, "four-ai-class"); nextTrace = Time.realtimeSinceStartup + 2; }
                for (int i = 0; i < bones.Length; i++) movedBones |= Quaternion.Angle(rest[i], bones[i].localRotation) > 12;
                if (rules.accuracy[0] >= .86f && rules.hold[0] > .5f && seen.Add(rules.Pose)) Capture(match, "actual-ai-" + rules.Pose + "-720-controlled");
                Assert.That(instructor.transform.position, Is.EqualTo(instructorOrigin), "Instructor articulates actual bones and never slides a frozen mesh.");
                yield return null;
            }
            Assert.That((bool)Get(match, "Finished"), Is.True); Assert.That(seen.Count, Is.EqualTo(7));
            Assert.That(rules.completed.All(v => v == 7), Is.True, "All four AI must actually align and hold all seven poses, not just wait for timers.");
            Assert.That(rules.score.All(v => v > 350), Is.True); Assert.That(movedBones, Is.True); Assert.That((int)Get(instructor, "AnimatedFrames"), Is.GreaterThan(100));
            Assert.That(trucks.All(t => ((Rigidbody)Get(t, "body")).isKinematic), Is.True);
            CollectionAssert.AreEqual(configuration, trucks.Select(OriginalConfiguration).ToArray()); CollectionAssert.AreEqual(ids, trucks.Select(t => t.GetInstanceID()).ToArray());
            CollectionAssert.AreEqual(profileIds, Enumerable.Range(0, 4).Select(i => (string)Get(players.GetValue(i), "participantId")).ToArray());
            Capture(match, "actual-four-ai-results-1080-controlled", 1920, 1080);
            Assert.That((bool)Call(match, "StartMatch"), Is.True); Assert.That(rules.Pose, Is.EqualTo(YogaPose.DownwardFacingTruck)); Assert.That(rules.score.All(v => v == 0), Is.True);
            foreach (var rig in rigs) Assert.That(((IEnumerable)Get(rig, "attachedBodies")).Cast<Rigidbody>().All(b => b.isKinematic), Is.True);
            TestContext.WriteLine("YOGA_NATIVE actual owned keyboard banks, original kinematic full-tractor assembly, selected composed twist, measured seven AI pose/hold results, real instructor bone animation, free class zero points/winner, pause,1to4 rematch and stable physical/profile IDs. Controlled native captures, no Windows/hardware/listening claim.");
        }
        finally
        {
            if (match != null) Trace(match, "finally-before-cleanup"); Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
            InputSystem.settings = originalInputSettings; if (fixtureInputSettings != null) UnityEngine.Object.DestroyImmediate(fixtureInputSettings);
            Static("SportsParticipantRegistry", "ForgetCache"); lease.Dispose(); Static("SportsParticipantRegistry", "ForgetCache");
        }
    }
}
#endif
