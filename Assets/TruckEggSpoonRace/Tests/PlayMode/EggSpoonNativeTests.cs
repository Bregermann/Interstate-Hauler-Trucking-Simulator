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
using LWS.TruckEggSpoonRace;

public sealed class EggSpoonNativeTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x)).Invoke(owner, args);
    static object Static(string type, string name, params object[] args) => Sports(type).GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    [Serializable] sealed class LifecycleSample
    {
        public string checkpoint, menu, keyboard, backgroundInput, editorInput;
        public int frame; public float real, scaled, scale, countdown, clock;
        public bool focused, running, live, paused, finished, ready, ownerReady;
    }
    static string progress;
    static string Trace(Component match, string checkpoint)
    {
        if (match == null) return checkpoint + " no match";
        var data = new LifecycleSample { checkpoint = checkpoint, frame = Time.frameCount, real = Time.realtimeSinceStartup, scaled = Time.time, scale = Time.timeScale, focused = Application.isFocused, running = (bool)Get(match, "Running"), live = (bool)Get(match, "Live"), paused = (bool)Get(match, "Paused"), finished = (bool)Get(match, "Finished"), ready = (bool)Get(match, "DevicesReady"), ownerReady = (bool)Call(match, "Ready", 0), countdown = (float)Get(match, "Countdown"), clock = (float)Get(match, "Clock"), menu = Get(Get(match, "menus"), "Screen").ToString(), keyboard = Keyboard.current == null ? "NONE" : Keyboard.current.name + ":" + Keyboard.current.enabled + ":" + Keyboard.current.added, backgroundInput = InputSystem.settings.backgroundBehavior.ToString(), editorInput = InputSystem.settings.editorInputBehaviorInPlayMode.ToString() };
        string json = JsonUtility.ToJson(data); Directory.CreateDirectory(Path.GetDirectoryName(progress)); File.AppendAllText(progress, json + "\n"); TestContext.WriteLine("EGG_LIFECYCLE_V2 " + json); return json;
    }
    static IEnumerator AdvanceSimulation(Component match, float seconds, float limit, string checkpoint)
    {
        float start = Time.time, until = Time.realtimeSinceStartup + limit, next = 0; Trace(match, checkpoint + "-begin");
        while (Time.time - start < seconds && Time.realtimeSinceStartup < until) { if (Time.realtimeSinceStartup >= next) { Trace(match, checkpoint + "-wait"); next = Time.realtimeSinceStartup + 1; } yield return null; }
        Assert.That(Time.time - start, Is.GreaterThanOrEqualTo(seconds), "Simulation must advance: " + Trace(match, checkpoint + "-end"));
    }
    static void Capture(Component match, string label, int width, int height) => Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5).Invoke(null, new object[] { match, width, height, Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ImpossibleEggSpoon/" + label + ".png"), null });
    static string Geometry(Component truck)
    {
        var shape = (BoxCollider)Get(truck, "shape");
        return shape.GetInstanceID() + ":" + shape.center.ToString("R") + ":" + shape.size.ToString("R") + string.Join("/", ((IEnumerable)Get(truck, "wheels")).Cast<Component>().Select(w => w.GetInstanceID() + ":" + Get(Get(w, "wheel"), "radius") + ":" + Get(Get(w, "spring"), "maxLength")));
    }
    [UnityTest]
    public IEnumerator ActualEggContactControlledImpactServiceAndFourAIPhysicalRace()
    {
        var lease = (IDisposable)Static("SportsPrefs", "IsolateValidation", "ImpossibleEgg" + Guid.NewGuid().ToString("N")); Component match = null; GameObject isolation = null; Keyboard keyboard = null;
        var originalInputSettings = InputSystem.settings; InputSettings fixtureInputSettings = null;
        progress = Path.GetFullPath("Logs/TruckSports/Impossible07/EggLifecycleV2/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".jsonl");
        try
        {
            fixtureInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
            fixtureInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            fixtureInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            fixtureInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = fixtureInputSettings;
            Static("SportsParticipantRegistry", "ForgetCache"); isolation = new GameObject("Egg spoon native input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); keyboard = InputSystem.AddDevice<Keyboard>();
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckEggSpoonRace/Scenes/TruckEggSpoonRace.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckEggSpoonRace.EggSpoonMatch, Assembly-CSharp", true)); Assert.That(match, Is.Not.Null);
            Trace(match, "scene-loaded-two-frames");
            var racers = ((IEnumerable)Get(match, "racers")).Cast<Component>().ToArray(); var trucks = racers.Select(r => (Component)Get(r, "truck")).ToArray(); Assert.That(racers.Length, Is.EqualTo(4));
            var ids = trucks.Select(t => t.GetInstanceID()).ToArray(); var geometry = trucks.Select(Geometry).ToArray();
            foreach (var racer in racers)
            {
                Assert.That(((Transform)Get(racer, "spoon")).IsChildOf(racer.transform), Is.False, "Own scoop physics must not pollute original NWH attached-body caches.");
                Assert.That(((Component)Get(racer, "egg")).transform.IsChildOf(racer.transform), Is.False);
                Assert.That(((MeshCollider)Get(racer, "bowlCollider")).convex, Is.False);
                Assert.That(((Component)Get(racer, "egg")).GetComponent<Joint>(), Is.Null, "Egg is not attached or AI-immune.");
            }
            var setup = Get(match, "setup"); Set(setup, "count", 1); Call(match, "AllBots"); var players = (Array)Get(setup, "players"); var player = players.GetValue(0); Set(player, "source", Enum.ToObject(Get(player, "source").GetType(), 1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); Assert.That((bool)Call(match, "StartMatch"), Is.True); Time.timeScale = 3;
            Trace(match, "owned-keyboard-started");
            float deadline = Time.realtimeSinceStartup + 8, nextTrace = 0; while (!(bool)Get(match, "Racing") && Time.realtimeSinceStartup < deadline) { if (Time.realtimeSinceStartup >= nextTrace) { Trace(match, "countdown-wait"); nextTrace = Time.realtimeSinceStartup + 1; } yield return null; }
            Assert.That((bool)Get(match, "Racing"), Is.True, "Real countdown must release physics: " + Trace(match, "countdown-boundary"));
            var egg = (Component)Get(racers[0], "egg"); var eggBody = (Rigidbody)Get(egg, "body");
            deadline = Time.realtimeSinceStartup + 4; while ((int)Get(egg, "PhysicalContacts") == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((int)Get(egg, "PhysicalContacts"), Is.GreaterThan(0)); Assert.That(eggBody.isKinematic, Is.False); Assert.That((bool)Get(egg, "Carried"), Is.True);
            Capture(match, "actual-physical-balance-720-controlled", 1280, 720);
            Call(match, "Pause", 0, false); yield return null; yield return null; var position = eggBody.position; float clock = (float)Get(match, "Clock"); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Vector3.Distance(position, eggBody.position), Is.LessThan(.00001f)); Assert.That((float)Get(match, "Clock"), Is.EqualTo(clock)); Call(match, "Resume"); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space, Key.D)); InputSystem.Update(); yield return AdvanceSimulation(match, .55f, 5, "owned-spoon-trim"); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            Assert.That((float)Get(racers[0], "TrimRoll"), Is.GreaterThan(4), "Owned input must adjust a real scoop, not just a gauge.");
            // Explicit CONTROLLED initial-condition drop, not a natural-race
            // claim or a direct break/score call. PhysX collision must break it.
            var bowlPosition = (Vector3)Get(racers[0], "BowlPosition"); Call(egg, "Place", bowlPosition + Vector3.up * 4.5f, Quaternion.identity, false);
            deadline = Time.realtimeSinceStartup + 8; while (!(bool)Get(egg, "Shattered") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)Get(egg, "Shattered"), Is.True); Assert.That((float)Get(egg, "GreatestImpact"), Is.GreaterThan(4.5f));
            Assert.That(((ParticleSystem)Get(egg, "chips")).isPlaying, Is.True); Assert.That(((ParticleSystem)Get(egg, "yolk")).isPlaying, Is.True);
            var rules = (EggSpoonRules)Get(match, "rules"); Assert.That(rules.spills[0], Is.EqualTo(1)); Assert.That(rules.carrying[0], Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); InputSystem.Update();
            deadline = Time.realtimeSinceStartup + 10; while (!rules.carrying[0] && Time.realtimeSinceStartup < deadline) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            Assert.That(rules.carrying[0], Is.True, "Actual stopped station service must replace the physically broken egg."); Assert.That(rules.replacements[0], Is.EqualTo(1)); Assert.That((bool)Get(egg, "Intact"), Is.True);
            Call(match, "SetupScreen"); Set(setup, "count", 4); Call(match, "AllBots");
            var bodyIds = new[] { "truck.fire", "novelty.hotdog", "truck.monster", "truck.lifted" };
            for (int i = 0; i < 4; i++) { string profile = (string)Get(players.GetValue(i), "participantId"); var draft = Static("SportsParticipantRegistry", "GetAppearance", profile); Set(draft, "bodyId", bodyIds[i]); Static("SportsParticipantRegistry", "TrySetAppearance", profile, draft); }
            Assert.That((bool)Call(match, "StartMatch"), Is.True); Time.timeScale = 4;
            Trace(match, "four-ai-race-started");
            var profileIds = Enumerable.Range(0, 4).Select(i => (string)Get(players.GetValue(i), "participantId")).ToArray(); Assert.That(profileIds.Distinct().Count(), Is.EqualTo(4));
            var appearance = profileIds.Select(id => JsonUtility.ToJson(Static("SportsParticipantRegistry", "GetAppearance", id))).ToArray();
            for (int i = 0; i < 4; i++) { var host = Get(trucks[i], "appearanceHost"); Assert.That(Get(host, "ProfileId"), Is.EqualTo(profileIds[i])); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo(bodyIds[i])); }
            var moved = new bool[4]; deadline = Time.realtimeSinceStartup + 75;
            while (!(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline)
            {
                if (Time.realtimeSinceStartup >= nextTrace) { Trace(match, "four-ai-race"); nextTrace = Time.realtimeSinceStartup + 2; }
                for (int i = 0; i < 4; i++) moved[i] |= ((Rigidbody)Get(trucks[i], "body")).position.z > 8;
                yield return null;
            }
            Assert.That((bool)Get(match, "Finished"), Is.True); Assert.That(moved.All(x => x), Is.True);
            Assert.That(rules.finished.All(x => x), Is.True, "All four AI must physically deliver intact eggs through real ordered gates, not merely time out.");
            Assert.That(rules.checkpoint.All(x => x == 3), Is.True); Assert.That(rules.time.All(t => float.IsFinite(t) && t > 20), Is.True);
            Capture(match, "four-actual-ai-results-1080-controlled", 1920, 1080);
            CollectionAssert.AreEqual(ids, trucks.Select(t => t.GetInstanceID()).ToArray()); CollectionAssert.AreEqual(geometry, trucks.Select(Geometry).ToArray());
            CollectionAssert.AreEqual(profileIds, Enumerable.Range(0, 4).Select(i => (string)Get(players.GetValue(i), "participantId")).ToArray());
            CollectionAssert.AreEqual(appearance, profileIds.Select(id => JsonUtility.ToJson(Static("SportsParticipantRegistry", "GetAppearance", id))).ToArray());
            Assert.That((bool)Call(match, "StartMatch"), Is.True); Assert.That(rules.spills.All(x => x == 0), Is.True); Assert.That(rules.checkpoint.All(x => x == 0), Is.True);
            TestContext.WriteLine("EGG_SPOON_NATIVE real free egg/concave scoop contact, owned trim controls, pause, CONTROLLED physical drop/impact break and original station replacement; separate four-AI actual full race, unchanged original geometry/stable profile IDs/rematch. No AI attachment/physics immunity/teleported checkpoint credit. No Windows/hardware/listening claim.");
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
