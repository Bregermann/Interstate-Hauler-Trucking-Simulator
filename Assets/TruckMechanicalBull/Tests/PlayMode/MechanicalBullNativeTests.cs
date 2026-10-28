#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using LWS.TruckMechanicalBull;

public sealed class MechanicalBullNativeTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x)).Invoke(owner, args);
    static object Static(string type, string name, params object[] args) => Sports(type).GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    [Serializable] sealed class LifecycleSample
    {
        public string checkpoint, menu, phase, source, keyboard, backgroundInput, editorInput;
        public int frame; public float real, scaled, scale, delta, fixedDelta, phaseSeconds;
        public bool focused, running, live, paused, finished, ready, ownerReady, enabled, active;
    }
    static string progress;
    static string Trace(Component match, string checkpoint)
    {
        if (match == null) return checkpoint + " no match";
        var players = (Array)Get(Get(match, "setup"), "players");
        var sample = new LifecycleSample { checkpoint = checkpoint, frame = Time.frameCount, real = Time.realtimeSinceStartup, scaled = Time.time, scale = Time.timeScale, delta = Time.deltaTime, fixedDelta = Time.fixedDeltaTime, focused = Application.isFocused, running = (bool)Get(match, "Running"), live = (bool)Get(match, "Live"), paused = (bool)Get(match, "Paused"), finished = (bool)Get(match, "Finished"), ready = (bool)Get(match, "DevicesReady"), ownerReady = (bool)Call(match, "Ready", 0), menu = Get(Get(match, "menus"), "Screen").ToString(), phase = Get(match, "Phase").ToString(), phaseSeconds = (float)Get(match, "PhaseSeconds"), source = Get(players.GetValue(0), "source").ToString(), keyboard = Keyboard.current == null ? "NONE" : Keyboard.current.name + ":" + Keyboard.current.enabled + ":" + Keyboard.current.added + ":native=" + Keyboard.current.native, enabled = ((Behaviour)match).enabled, active = match.gameObject.activeInHierarchy };
        sample.backgroundInput = InputSystem.settings.backgroundBehavior.ToString(); sample.editorInput = InputSystem.settings.editorInputBehaviorInPlayMode.ToString();
        string json = JsonUtility.ToJson(sample); Directory.CreateDirectory(Path.GetDirectoryName(progress)); File.AppendAllText(progress, json + "\n"); TestContext.WriteLine("BULL_LIFECYCLE_V3 " + json); return json;
    }
    static void Capture(Component match, string label, int width, int height)
    {
        var method = Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5);
        method.Invoke(null, new object[] { match, width, height, Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ImpossibleMechanicalBull/" + label + ".png"), null });
    }
    static string Geometry(Component truck)
    {
        var shape = (BoxCollider)Get(truck, "shape"); var wheels = ((IEnumerable)Get(truck, "wheels")).Cast<Component>();
        return shape.GetInstanceID() + ":" + shape.center.ToString("R") + ":" + shape.size.ToString("R") + string.Join("/", wheels.Select(w => w.GetInstanceID() + ":" + Get(Get(w, "wheel"), "radius") + ":" + Get(Get(w, "spring"), "maxLength")));
    }
    [UnityTest]
    public IEnumerator PhysicalSaddleContactsCounterleanFallsPauseAndFourFairTurnsComplete()
    {
        var lease = (IDisposable)Static("SportsPrefs", "IsolateValidation", "ImpossibleBull" + Guid.NewGuid().ToString("N"));
        GameObject isolation = null; Component match = null; Keyboard keyboard = null;
        var random = UnityEngine.Random.state;
        var originalInputSettings = InputSystem.settings; InputSettings fixtureInputSettings = null;
        progress = Path.GetFullPath("Logs/TruckSports/Impossible07/BullLifecycleV3/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".jsonl");
        try
        {
            // Same scoped background-input fixture used by established Base,
            // Billiards and DeadAir tests. Batch has no focused Game View; do
            // not mutate the authored project settings or runtime focus policy.
            fixtureInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
            fixtureInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            fixtureInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            fixtureInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = fixtureInputSettings;
            Static("SportsParticipantRegistry", "ForgetCache"); isolation = new GameObject("Bull native owned input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
            keyboard = InputSystem.AddDevice<Keyboard>();
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMechanicalBull/Scenes/TruckMechanicalBull.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckMechanicalBull.MechanicalBullMatch, Assembly-CSharp", true));
            Trace(match, "scene-loaded-two-frames");
            Assert.That(match, Is.Not.Null); var riders = ((IEnumerable)Get(match, "riders")).Cast<Component>().ToArray(); Assert.That(riders.Length, Is.EqualTo(4));
            var trucks = riders.Select(r => (Component)Get(r, "truck")).ToArray(); var ids = trucks.Select(t => t.GetInstanceID()).ToArray(); var geometry = trucks.Select(Geometry).ToArray();
            Assert.That(trucks.All(t => Get(t, "tractorMotor") != null && Get(t, "appearanceHost") != null), Is.True);
            var setup = Get(match, "setup"); Set(setup, "count", 1); Call(match, "AllBots");
            var players = (Array)Get(setup, "players"); var player = players.GetValue(0); Set(player, "source", Enum.ToObject(Get(player, "source").GetType(), 1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            Time.timeScale = 3;
            Trace(match, "human-started");
            var rules = (MechanicalBullRules)Get(match, "rules"); var machine = (Component)Get(match, "machine"); var physical = (Rigidbody)Get(machine, "body");
            bool contacted = false; float deadline = Time.realtimeSinceStartup + 12, nextTrace = 0;
            while ((int)Get(match, "Phase") == 0 && Time.realtimeSinceStartup < deadline) { contacted |= (bool)Get(riders[0], "DeckContact"); if (Time.realtimeSinceStartup >= nextTrace) { Trace(match, "mounting-wait"); nextTrace = Time.realtimeSinceStartup + 1; } yield return null; }
            Assert.That((int)Get(match, "Phase"), Is.EqualTo(1), "Original mounting countdown must release the physical ride. " + Trace(match, "mounting-boundary"));
            var start = physical.position; var angle = physical.rotation; var riderBody = (Rigidbody)Get(trucks[0], "body"); Assert.That(riderBody.isKinematic, Is.False);
            float sampleUntil = Time.realtimeSinceStartup + 1.2f, peakDistance = 0, peakAngle = 0;
            int sampledSteps = (int)Get(machine, "Steps");
            // A periodic buck can return near its starting point. Measure actual
            // excursion throughout the unchanged sample, rather than its endpoint.
            while (Time.realtimeSinceStartup < sampleUntil)
            {
                contacted |= (bool)Get(riders[0], "DeckContact");
                peakDistance = Mathf.Max(peakDistance, Vector3.Distance(start, physical.position));
                peakAngle = Mathf.Max(peakAngle, Quaternion.Angle(angle, physical.rotation));
                yield return null;
            }
            TestContext.WriteLine($"BULL_ACTUAL_BUCK_SAMPLE peakMetres={peakDistance:R} endpointMetres={Vector3.Distance(start, physical.position):R} peakDegrees={peakAngle:R} endpointDegrees={Quaternion.Angle(angle, physical.rotation):R} physicsSteps={(int)Get(machine, "Steps") - sampledSteps}");
            Assert.That(contacted, Is.True, "Actual NWH HitCollider must contact the explicit moving saddle, not a timer-only ride.");
            Assert.That((int)Get(machine, "Steps"), Is.GreaterThan(sampledSteps));
            Assert.That(peakDistance, Is.GreaterThan(.015f)); Assert.That(peakAngle, Is.GreaterThan(2));
            Capture(match, "actual-riding-720-controlled", 1280, 720);
            Call(match, "Pause", 0, false); yield return null; yield return null;
            Trace(match, "explicit-pause-settled");
            var pausedPosition = riderBody.position; float survived = rules.Survival;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Vector3.Distance(pausedPosition, riderBody.position), Is.LessThan(.00001f)); Assert.That(rules.Survival, Is.EqualTo(survived));
            Call(match, "Resume"); yield return null; yield return null;
            Trace(match, "explicit-resume-neutral");
            // Synthetic owned keyboard input, not pose/force/outcome injection.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D)); InputSystem.Update();
            deadline = Time.realtimeSinceStartup + 25;
            while (!rules.Resolved && Time.realtimeSinceStartup < deadline) { if (Time.realtimeSinceStartup >= nextTrace) { Trace(match, "incorrect-keyboard-lean"); nextTrace = Time.realtimeSinceStartup + 1; } yield return null; }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            Assert.That(rules.Resolved, Is.True); Assert.That(rules.Fell, Is.True, "Sustained incorrect counterlean must cause a genuine physical fall.");
            Assert.That(rules.Survival, Is.GreaterThan(0)); Assert.That(rules.totals[0], Is.GreaterThan(0));
            Time.timeScale = 1; Capture(match, "actual-fall-1080-controlled", 1920, 1080);
            deadline = Time.realtimeSinceStartup + 7; while (!(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)Get(match, "Finished"), Is.True);
            Call(match, "SetupScreen"); Set(setup, "count", 4); Call(match, "AllBots"); Set(match, "option", 0); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var profileIds = Enumerable.Range(0, 4).Select(i => (string)Get(players.GetValue(i), "participantId")).ToArray(); Assert.That(profileIds.Distinct().Count(), Is.EqualTo(4));
            var masses = new Dictionary<int, float>(); Time.timeScale = 4; deadline = Time.realtimeSinceStartup + 105;
            Trace(match, "four-ai-rematch-started");
            while (!(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline)
            {
                if (Time.realtimeSinceStartup >= nextTrace) { Trace(match, "four-ai-rides"); nextTrace = Time.realtimeSinceStartup + 2; }
                for (int i = 0; i < 4; i++)
                {
                    var motor = Get(trucks[i], "tractorMotor");
                    if (!(bool)Get(Get(motor, "vehicle"), "IsInitialized")) continue;
                    var body = (Rigidbody)Get(trucks[i], "body");
                    if (masses.TryGetValue(i, out var mass)) Assert.That(body.mass, Is.EqualTo(mass)); else masses.Add(i, body.mass);
                }
                yield return null;
            }
            Assert.That((bool)Get(match, "Finished"), Is.True, "All four ordinary AI rides must finish through original physical simulation.");
            Assert.That(rules.Turn, Is.EqualTo(4)); Assert.That(rules.totals.All(p => p > 0), Is.True); Assert.That(masses.Count, Is.EqualTo(4));
            CollectionAssert.AreEqual(ids, trucks.Select(t => t.GetInstanceID()).ToArray()); CollectionAssert.AreEqual(geometry, trucks.Select(Geometry).ToArray());
            CollectionAssert.AreEqual(profileIds, Enumerable.Range(0, 4).Select(i => (string)Get(players.GetValue(i), "participantId")).ToArray());
            Assert.That(((Animator[])Get(Get(match, "crowd"), "nearFans")).Length, Is.GreaterThanOrEqualTo(48));
            Assert.That((bool)Call(match, "StartMatch"), Is.True); Assert.That(rules.Turn, Is.Zero); Assert.That(rules.totals.All(p => p == 0), Is.True);
            TestContext.WriteLine("MECHANICAL_BULL_NATIVE actual NWH saddle contact, physical bucking/counterlean/fall, pause/resume, four fair AI rides, stable owners/original wheel geometry/masses and rematch. Synthetic keyboard only; no hardware, listening or actual Windows claim.");
        }
        finally
        {
            if (match != null) Trace(match, "finally-before-cleanup"); Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); UnityEngine.Random.state = random;
            InputSystem.settings = originalInputSettings; if (fixtureInputSettings != null) UnityEngine.Object.DestroyImmediate(fixtureInputSettings);
            Static("SportsParticipantRegistry", "ForgetCache"); lease.Dispose(); Static("SportsParticipantRegistry", "ForgetCache");
        }
    }
}
#endif
