#if UNITY_EDITOR
using System;
using System.Collections;
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

namespace LWS.TruckTornadoRodeo.Tests
{
    public sealed class TornadoLifecycleTests
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly string CaptureRun = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
        static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
        static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
        static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(ok => ok)).Invoke(owner, args);
        static Rigidbody Body(object owner) => (Rigidbody)Get(owner, "body");
        sealed class Fixture : IDisposable
        {
            readonly IDisposable prefs; readonly GameObject isolation;
            public Behaviour match; public object rules, field; public object[] trucks, riders, debris, rings;
            public Fixture()
            {
                prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "TornadoNative" + Guid.NewGuid().ToString("N") }); Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
                isolation = new GameObject("Explicit native tornado input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
            }
            public IEnumerator Load()
            {
                Assert.That(File.Exists("Assets/TruckTornadoRodeo/Scenes/TruckTornadoRodeo.unity"), Is.True, "Lead must run explicit TornadoAuthoring.Create before native testing.");
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTornadoRodeo/Scenes/TruckTornadoRodeo.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckTornadoRodeo.TornadoMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Cast<Behaviour>().Single();
                rules = Get(match, "rules"); field = Get(match, "field"); trucks = ((Array)Get(match, "trucks")).Cast<object>().ToArray(); riders = ((Array)Get(match, "riders")).Cast<object>().ToArray(); debris = ((Array)Get(match, "debris")).Cast<object>().ToArray(); rings = ((Array)Get(match, "rings")).Cast<object>().ToArray();
                Assert.That(trucks.Length, Is.EqualTo(4)); Assert.That(trucks.All(t => Get(t, "tractorMotor") != null), Is.True); Assert.That(rings.Length, Is.EqualTo(6)); Assert.That(debris.Count(d => (bool)Get(d, "animal")), Is.EqualTo(4));
            }
            public void Start(int count = 4) { Call(match, "SetupScreen"); Set(Get(match, "setup"), "count", count); Set(match, "option", 0); Call(match, "NormalizeSetup"); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True); }
            public void Dispose() { Time.timeScale = 1; if (match != null) { match.enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); prefs.Dispose(); }
        }
        static IEnumerator Until(Func<bool> condition, float seconds, string why) { float end = Time.realtimeSinceStartup + seconds; while (!condition() && Time.realtimeSinceStartup < end) yield return null; Assert.That(condition(), Is.True, why); }
        static IEnumerator Capture(Behaviour match, string name, int width, int height)
        {
            yield return null; yield return null;
            string folder = "Logs/TruckSports/PlaytestRepair/TornadoRodeoControlledNative/" + CaptureRun; Directory.CreateDirectory(folder); string file = Path.Combine(folder, "controlled-" + name + ".png");
            var save = Sports("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5 && method.GetParameters()[0].ParameterType.IsInstanceOfType(match));
            save.Invoke(null, new object[] { match, width, height, file, null });
            Assert.That(File.Exists(file), Is.True, "Controlled native Camera.Render/UI composite must be written; actual Windows backbuffer acceptance is separate.");
            var image = new Texture2D(2, 2); try { Assert.That(image.LoadImage(File.ReadAllBytes(file)), Is.True); Assert.That(new Vector2Int(image.width, image.height), Is.EqualTo(new Vector2Int(width, height))); } finally { UnityEngine.Object.DestroyImmediate(image); }
            TestContext.WriteLine("TORNADO_CONTROLLED_NATIVE " + width + "x" + height + " " + file + "; not an actual GameView/Windows/human capture.");
        }
        [UnityTest, Timeout(180000)] public IEnumerator RealAIEnterLiftStuntFlareLandFinishPauseAndRematch()
        {
            using (var f = new Fixture())
            {
                yield return f.Load(); var gravity = Physics.gravity; var simulation = Physics.simulationMode; f.Start(); string id = (string)Get(f.match, "MatchId"); Time.timeScale = 3;
                bool pause = false, capture = false; float height = 0, airborne = 0; int maxFlares = 0; float end = Time.realtimeSinceStartup + 130;
                var sheep = f.debris.Where(d => (bool)Get(d, "animal")).Cast<Component>().ToArray(); var animators = sheep.Select(s => s.GetComponentInChildren<Animator>(true)).ToArray(); Assert.That(animators.All(a => a != null && a.runtimeAnimatorController != null), Is.True);
                var bones = sheep.Select(s => s.GetComponentInChildren<SkinnedMeshRenderer>(true).bones).ToArray(); var baseline = bones.Select(rig => rig.Select(b => b.localRotation).ToArray()).ToArray(); var moved = new bool[sheep.Length]; var maxBoneAngle = new float[sheep.Length]; bool bonesMoved = false;
                for (int animal = 0; animal < sheep.Length; animal++) Assert.That(bones[animal].All(b => b != null && b.IsChildOf(animators[animal].transform)), Is.True, "Actual skin bones must be descendants of their Animator, not siblings of a mesh-node Animator.");
                while (!(bool)Get(f.match, "Finished") && Time.realtimeSinceStartup < end)
                {
                    height = Mathf.Max(height, ((float[])Get(f.rules, "bestHeight")).Max()); airborne = Mathf.Max(airborne, f.riders.Max(r => (float)Get(r, "AirborneSeconds"))); maxFlares = Mathf.Max(maxFlares, (int)Get(f.match, "AIFlares"));
                    for (int animal = 0; animal < bones.Length; animal++) for (int i = 0; i < bones[animal].Length; i++) { float angle = Quaternion.Angle(baseline[animal][i], bones[animal][i].localRotation); maxBoneAngle[animal] = Mathf.Max(maxBoneAngle[animal], angle); if (angle > .5f) moved[animal] = true; } bonesMoved = moved.All(value => value);
                    if (!capture && height > 15 && (float)Get(f.rules, "Clock") > 10)
                    {
                        capture = true; Time.timeScale = 1; yield return Capture(f.match, "natural-airborne-four-AI-720", 1280, 720); yield return Capture(f.match, "natural-airborne-four-AI-1080", 1920, 1080); Time.timeScale = 3;
                    }
                    if (!pause && airborne > 2)
                    {
                        pause = true; Call(f.match, "Pause", 0, false); float clock = (float)Get(f.rules, "Clock"); var points = ((int[])Get(f.rules, "points")).ToArray(); var positions = f.trucks.Select(Body).Select(b => b.position).Concat(f.debris.Select(Body).Select(b => b.position)).ToArray(); float hold = Time.realtimeSinceStartup + .25f; while (Time.realtimeSinceStartup < hold) yield return null;
                        Assert.That((float)Get(f.rules, "Clock"), Is.EqualTo(clock)); Assert.That((int[])Get(f.rules, "points"), Is.EqualTo(points)); var after = f.trucks.Select(Body).Select(b => b.position).Concat(f.debris.Select(Body).Select(b => b.position)).ToArray(); for (int i = 0; i < positions.Length; i++) Assert.That(Vector3.Distance(positions[i], after[i]), Is.LessThan(.001f)); Call(f.match, "Resume"); Time.timeScale = 3;
                    }
                    yield return null;
                }
                Assert.That((bool)Get(f.match, "Finished"), Is.True); Assert.That(height, Is.GreaterThan(15)); Assert.That(airborne, Is.GreaterThan(2)); Assert.That(maxFlares, Is.GreaterThan(0)); Assert.That(((int[])Get(f.rules, "landings")).Sum(), Is.GreaterThan(0), "AI must prepare and complete an actually supported controlled landing, not just time out aloft."); Assert.That(((int[])Get(f.rules, "points")).Sum(), Is.GreaterThan(0)); TestContext.WriteLine("TORNADO_OBSERVATIONS capture=" + capture + " pause=" + pause + " allAnimalBonesMoved=" + bonesMoved + " perAnimalMaxDegrees=" + string.Join(",", maxBoneAngle.Select(v => v.ToString("R"))) + " animatorRoots=" + string.Join(";", animators.Select(a => a.transform.parent.name + "/" + a.name))); Assert.That(capture, Is.True, "Natural airborne controlled native capture must occur."); Assert.That(pause, Is.True, "Actual airborne pause must occur."); Assert.That(bonesMoved, Is.True, "All four genuine sheep rigs must move their actual bound skeleton by more than0.5degrees.");
                Assert.That(f.debris.Max(d => (float)Get(d, "MaximumHeight")), Is.GreaterThan(8)); Assert.That(Physics.gravity, Is.EqualTo(gravity)); Assert.That(Physics.simulationMode, Is.EqualTo(simulation));
                Assert.That(f.riders.Max(r => (float)Get(r, "MaximumAppliedAcceleration")), Is.LessThanOrEqualTo(48.001f), "Combined field and flare acceleration must remain bounded."); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.GreaterThan(0));
                TestContext.WriteLine("TORNADO_NATURAL height=" + height + " airborne=" + airborne + " flares=" + maxFlares + " landings=" + ((int[])Get(f.rules, "landings")).Sum() + " animalBonesMoved=" + bonesMoved);
                f.Start(); Assert.That((string)Get(f.match, "MatchId"), Is.Not.EqualTo(id)); Assert.That((int[])Get(f.rules, "points"), Is.All.Zero); Assert.That((int[])Get(f.rules, "flights"), Is.All.Zero); Assert.That((bool[])Get(f.rules, "airborne"), Is.All.False); Assert.That(f.debris.All(d => Body(d).isKinematic), Is.True);
            }
        }
        [UnityTest, Timeout(45000)] public IEnumerator NativeRigidTractorCrossesActualApertureAndGrazingDoesNotPass()
        {
            using (var f = new Fixture())
            {
                yield return f.Load(); f.Start(1); f.match.enabled = false;
                var ring = (Component)f.rings[0]; var truck = f.trucks[0]; var body = Body(truck); var shape = (BoxCollider)Get(truck, "shape");
                foreach (float offset in new[] { 0f, 11f })
                {
                    var rotation = ring.transform.rotation; var position = ring.transform.position - ring.transform.forward * 12 + ring.transform.right * offset - rotation * shape.center;
                    Call(truck, "Prepare", position, rotation, true); body.linearVelocity = ring.transform.forward * 20; Physics.SyncTransforms(); bool passed = false; Vector3 before = body.position; Quaternion q = body.rotation; float end = Time.realtimeSinceStartup + 1.5f;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return new WaitForFixedUpdate(); var after = body.position; var next = body.rotation;
                        passed |= (bool)Call(ring, "WholeTruckPassed", truck, before, q, after, next); before = after; q = next;
                    }
                    Assert.That(passed, Is.EqualTo(offset == 0), "Controlled native full-NWH trajectory: whole envelope must fit, not just a center/trigger touch. offset=" + offset);
                }
                Assert.That((int[])Get(f.rules, "points"), Is.All.Zero, "Read-only gate geometry cannot itself fabricate gameplay awards in this controlled non-running fixture.");
            }
        }
        [UnityTest, Timeout(60000)] public IEnumerator OneThroughFourViewsUseTypedActualOwnersAndRealPoints()
        {
            using (var f = new Fixture())
            {
                yield return f.Load(); Sports("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { true }); var views = Get(f.match, "drivingViews"); Call(views, "Select", 1);
                for (int count = 1; count <= 4; count++)
                {
                    f.Start(count); yield return null; yield return null; var rally = Get(views, "rally"); Assert.That((int)Get(rally, "Humans"), Is.EqualTo(count)); Assert.That(((int[])Get(rally, "owners")).Take(count), Is.EqualTo(Enumerable.Range(0, count))); Assert.That(((Camera[])Get(f.match, "cameras")).Sum(c => c.rect.width * c.rect.height), Is.EqualTo(1).Within(.001));
                    for (int i = 0; i < count; i++) { object[] args = { i, -1, null, null }; Assert.That((bool)f.match.GetType().GetMethod("TryReadViewportCard").Invoke(f.match, args), Is.True); Assert.That((int)args[1], Is.EqualTo(i)); Assert.That((string)args[2], Is.EqualTo("0 PTS")); }
                    Assert.That((string)Get(f.match, "Help"), Does.Contain("seven-second flare").And.Contain("no active-run teleports"));
                }
                Call(views, "Select", 0); yield return null; Assert.That(((Camera[])Get(f.match, "cameras")).Length, Is.EqualTo(1));
            }
        }
        [UnityTest, Timeout(45000)] public IEnumerator FourSoftwarePadsDriveFourOriginalNwhActorsBeforeActualEntry()
        {
            var previous = InputSystem.settings; var settings = UnityEngine.Object.Instantiate(previous); var pads = new Gamepad[4]; settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = settings;
            try
            {
                using (var f = new Fixture())
                {
                    yield return f.Load(); Call(f.match, "SetupScreen"); Set(Get(f.match, "setup"), "count", 4); Call(f.match, "NormalizeSetup"); var players = (Array)Get(Get(f.match, "setup"), "players");
                    for (int i = 0; i < 4; i++) { pads[i] = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(pads[i]); var p = players.GetValue(i); Set(p, "source", Enum.Parse(p.GetType().GetField("source").FieldType, "WholeController")); Set(p, "pad", pads[i]); InputSystem.QueueStateEvent(pads[i], new GamepadState()); }
                    Assert.That((bool)Call(f.match, "StartMatch"), Is.True); yield return null; yield return null; yield return Until(() => (bool)Get(f.rules, "Live"), 8, "Original countdown completed.");
                    var starts = f.trucks.Select(Body).Select(b => b.position).ToArray(); foreach (var pad in pads) InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = .8f }); float end = Time.realtimeSinceStartup + 2; while (Time.realtimeSinceStartup < end) yield return null;
                    var commands = (Vector2[])Get(f.match, "commands"); for (int i = 0; i < 4; i++) { Assert.That(commands[i].y, Is.GreaterThan(.7f)); Assert.That(Vector3.Distance(starts[i], Body(f.trucks[i]).position), Is.GreaterThan(.3f)); Assert.That((string)Get(Get(f.trucks[i], "appearanceHost"), "ProfileId"), Is.EqualTo((string)Get(players.GetValue(i), "participantId"))); }
                }
            }
            finally { foreach (var pad in pads) if (pad != null && pad.added) InputSystem.RemoveDevice(pad); InputSystem.settings = previous; UnityEngine.Object.DestroyImmediate(settings); Time.timeScale = 1; }
        }
    }
}
#endif
