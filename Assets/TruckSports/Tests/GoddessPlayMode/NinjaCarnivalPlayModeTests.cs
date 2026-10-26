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

public sealed class NinjaCarnivalPlayModeTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    sealed class Fixture : IDisposable
    {
        readonly IDisposable prefs; readonly InputSettings original, temporary; readonly GameObject isolation;
        public readonly Gamepad pad; public Behaviour match;
        public Fixture(string suffix)
        {
            prefs = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "NinjaCarnival" + suffix });
            original = InputSystem.settings; temporary = UnityEngine.Object.Instantiate(original); temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = temporary;
            isolation = new GameObject("Ninja Carnival software-source isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(pad);
        }
        public object rules => Get(match, "rules");
        public object athlete => ((Array)Get(match, "athletes")).GetValue((int)Get(rules, "Active"));
        public object truck => ((Array)Get(match, "trucks")).GetValue((int)Get(rules, "Active"));
        public Rigidbody body => (Rigidbody)Get(truck, "body");
        public IEnumerator Load(int count, bool ai = false)
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckNinjaWarrior.NinjaMatch")).Cast<Behaviour>().Single(); Call(match, "AllBots"); Set(Get(match, "setup"), "count", count);
            var player = ((Array)Get(Get(match, "setup"), "players")).GetValue(0);
            if (ai) Set(player, "difficulty", Enum.Parse(player.GetType().GetField("difficulty").FieldType, "Hard"));
            else { Set(player, "source", Enum.Parse(player.GetType().GetField("source").FieldType, "WholeController")); Set(player, "pad", pad); }
            var catalog = Get(Get(match, "atmosphere"), "presets"); var clear = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"); Call(catalog, "Select", Get(clear, "id"));
            Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { false });
        }
        public IEnumerator Fresh()
        {
            Call(match, "SetupScreen"); InputSystem.QueueStateEvent(pad, new GamepadState()); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            float deadline = Time.realtimeSinceStartup + 15;
            while (Get(rules, "Phase").ToString() == "Countdown" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("Running"));
        }
        public void Dispose()
        {
            Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); InputSystem.settings = original;
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad); UnityEngine.Object.DestroyImmediate(temporary); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose();
        }
    }

    static float[] Travel(object athlete) => ((Array)Get(Get(athlete, "truck"), "wheels")).Cast<object>().Select(wheel => (float)Get(Get(wheel, "spring"), "maxLength")).ToArray();
    static void Capture(Fixture f, string label)
    {
        string folder = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/NinjaCarnivalNative"); Directory.CreateDirectory(folder);
        var canvas = ((Component)Get(f.match, "menus")).GetComponent<Canvas>(); var cameras = (Camera[])Get(f.match, "cameras");
        var method = Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(candidate => candidate.Name == "Save" && candidate.GetParameters().Length == 6);
        foreach (int width in new[] { 1280, 1920 }) method.Invoke(null, new object[] { canvas, cameras, width, width == 1280 ? 720 : 1080, Path.Combine(folder, label + "-" + width + "-controlled.png"), null });
    }
    static IEnumerator Hold(Fixture f, float minimumCharge)
    {
        float deadline = Time.realtimeSinceStartup + 3;
        while ((!(bool)Get(f.athlete, "Grounded") || Mathf.Abs(f.body.linearVelocity.y) > .5f) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That((bool)Get(f.athlete, "Grounded"), Is.True, "Begin the physical charge from actual wheel support.");
        InputSystem.QueueStateEvent(f.pad, new GamepadState().WithButton(GamepadButton.South)); deadline = Time.realtimeSinceStartup + 3;
        while ((float)Get(f.athlete, "Charge") < minimumCharge && Time.realtimeSinceStartup < deadline) { yield return null; Assert.That((int)Get(f.athlete, "Jumps"), Is.Zero, "Held JUMP must not launch before release."); }
        Assert.That((float)Get(f.athlete, "Charge"), Is.GreaterThanOrEqualTo(minimumCharge));
    }
    static IEnumerator Release(Fixture f)
    {
        InputSystem.QueueStateEvent(f.pad, new GamepadState()); float deadline = Time.realtimeSinceStartup + 3;
        while ((int)Get(f.athlete, "Jumps") == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That((int)Get(f.athlete, "Jumps"), Is.EqualTo(1)); Assert.That(f.body.linearVelocity.y, Is.GreaterThan(6));
    }
    static IEnumerator Peak(Fixture f, Action<float> found)
    {
        float origin = f.body.position.y, peak = origin, deadline = Time.realtimeSinceStartup + 4;
        while (f.body.linearVelocity.y > 0 && Time.realtimeSinceStartup < deadline) { yield return null; peak = Mathf.Max(peak, f.body.position.y); }
        found(peak - origin);
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator AssignedHoldReleaseProducesDistinctPhysicalFlightsAndRestoresPausedSuspension()
    {
        using (var f = new Fixture("Charge"))
        {
            yield return f.Load(1); yield return f.Fresh(); var original = Travel(f.athlete);
            yield return Hold(f, .02f); yield return Release(f); float shortPeak = 0; yield return Peak(f, value => shortPeak = value);
            Assert.That(shortPeak, Is.GreaterThan(2)); Capture(f, "short-jump-apex");
            yield return f.Fresh(); original = Travel(f.athlete); yield return Hold(f, .99f);
            var compressed = Travel(f.athlete); Assert.That(compressed.Zip(original, (a,b) => a < b * .5f).All(value => value), Is.True, "Owned public NWH spring travel must visibly compress.");
            Assert.That((float)Call(f.match, "ParticipantProgress", 0), Is.GreaterThan(.95f), "Use the existing compact Heat progress fill for charge."); Capture(f, "held-charge");
            Call(f.match, "Pause", 0, false);
            Assert.That((bool)Get(f.athlete, "Charging"), Is.False); Assert.That((float)Get(f.athlete, "Charge"), Is.Zero); CollectionAssert.AreEqual(original, Travel(f.athlete));
            InputSystem.QueueStateEvent(f.pad, new GamepadState()); yield return null; yield return null; Call(f.match, "Resume");
            float gate = Time.realtimeSinceStartup + 2; var player = ((Array)Get(Get(f.match, "setup"), "players")).GetValue(0);
            while (!(bool)Get(player, "released") && Time.realtimeSinceStartup < gate) yield return null;
            Assert.That((bool)Get(player, "released"), Is.True); Assert.That((int)Get(f.athlete, "Jumps"), Is.Zero, "Pause/reconnect neutral release cannot launch the canceled charge.");
            yield return Hold(f, .99f); yield return Release(f);
            Assert.That((int)Get(f.athlete, "SuperJumps"), Is.EqualTo(1)); Assert.That(Vector3.Dot(f.body.linearVelocity, f.body.transform.forward), Is.GreaterThan(8), "A stationary charged takeoff receives bounded forward help."); Assert.That(f.body.linearVelocity.y, Is.LessThan(18));
            float fullPeak = 0; yield return Peak(f, value => fullPeak = value); Assert.That(fullPeak, Is.GreaterThan(shortPeak + 1.2f)); Capture(f, "charged-jump-apex");
            InputSystem.QueueStateEvent(f.pad, new GamepadState { leftStick = Vector2.right, rightTrigger = 1 });
            // A synchronous apex capture can leave a catch-up physics batch
            // before the queued input's next Update. Start the response window
            // only after the original input router has delivered the command.
            float inputDeadline=Time.realtimeSinceStartup+2;
            while((((Vector2[])Get(f.match,"commands"))[0].x<.9f||((Vector2[])Get(f.match,"commands"))[0].y<.9f)&&Time.realtimeSinceStartup<inputDeadline)yield return null;
            var delivered=((Vector2[])Get(f.match,"commands"))[0];Assert.That(delivered.x,Is.GreaterThan(.9f));Assert.That(delivered.y,Is.GreaterThan(.9f));Assert.That((bool)Get(f.athlete,"Grounded"),Is.False);
            var initialAngular=f.body.angularVelocity;var pitchAxis=f.body.transform.right;var rollAxis=f.body.transform.forward;float fixedStart=Time.fixedTime;
            for(int step=0;step<10;step++){yield return new WaitForFixedUpdate();Assert.That((bool)Get(f.athlete,"Grounded"),Is.False,"Measure only real airborne control steps.");}
            var response=f.body.angularVelocity-initialAngular;
            TestContext.WriteLine("NINJA_AFTERTOUCH delivered="+delivered+" fixedSeconds="+(Time.fixedTime-fixedStart)+" angularBefore="+initialAngular+" angularAfter="+f.body.angularVelocity+" signedResponse="+new Vector3(Vector3.Dot(response,pitchAxis),Vector3.Dot(response,rollAxis),Vector3.Dot(response,Vector3.up))+" position="+f.body.position);
            Assert.That(Vector3.Dot(f.body.angularVelocity, f.body.transform.right), Is.GreaterThan(.005f), "Mattress-style forward input provides airborne pitch."); Assert.That(Vector3.Dot(f.body.angularVelocity, f.body.transform.forward), Is.LessThan(-.002f), "Mattress-style right input provides airborne roll."); Assert.That(Vector3.Dot(f.body.angularVelocity, Vector3.up), Is.GreaterThan(.002f), "The same assigned steering gives limited yaw."); Assert.That(Vector3.Dot(response,pitchAxis),Is.GreaterThan(.005f),"Pitch must physically respond to the delivered input.");Assert.That(Vector3.Dot(response,rollAxis),Is.LessThan(-.005f),"Right input must cause a physical negative roll response, not merely inherit prior rotation.");Assert.That(Vector3.Dot(response,Vector3.up),Is.GreaterThan(.005f),"Steering must cause a physical yaw response.");Assert.That(f.body.angularVelocity.magnitude, Is.LessThan(4), "Pitch, roll and yaw remain bounded.");
            Call(f.match, "SetupScreen"); CollectionAssert.AreEqual(original, Travel(f.athlete)); Assert.That((float)Get(f.athlete, "Charge"), Is.Zero);
            TestContext.WriteLine("Actual NWH/PhysX controller hold/release: short measured rise=" + shortPeak + ", full=" + fullPeak + "; no artificial truck position/flight velocity, compact charge fill, pause neutral rearm and suspension cleanup verified. Controlled captures are native Camera.Render, not Windows/hardware acceptance.");
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator WaterFailureResetsActiveAthleteClockCourseAndCountdownWithoutAward()
    {
        using (var f = new Fixture("Restart"))
        {
            yield return f.Load(2); var course = Get(f.match, "course"); var bodies = (Rigidbody[])Get(course, "movingBodies"); var poses = bodies.Select(body => body.position).ToArray(); var rotations = bodies.Select(body => body.rotation).ToArray();
            var inactive = ((Array)Get(f.match, "trucks")).GetValue(1); var inactiveBody = (Rigidbody)Get(inactive, "body"); var inactivePosition = inactiveBody.position;
            yield return f.Fresh(); yield return new WaitForSeconds(2);
            Assert.That(bodies.Select((body, index) => Quaternion.Angle(body.rotation, rotations[index]) > .5f).Any(value => value), Is.True, "The actual course has moved before failure.");
            // Ordered-progress fixture only, not a claim that two obstacles were
            // physically traversed. The fall itself uses the original water test.
            Call(f.rules, "Clear", 0, 1); Call(f.rules, "Clear", 0, 2); var start = (Vector3)Get(course, "start");
            Call(f.truck, "Prepare", new Vector3(0, (float)Get(course, "waterLevel") - 2, 250), Quaternion.identity, true); Physics.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 3; while ((int)Get(f.match, "ActualRecoveries") == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((int)Get(f.match, "ActualRecoveries"), Is.EqualTo(1)); Assert.That((int)Get(f.rules, "Active"), Is.Zero); Assert.That((float)Get(f.rules, "Clock"), Is.Zero); Assert.That((float)Get(f.rules, "Penalty"), Is.Zero); Assert.That((int)Get(f.rules, "Checkpoint"), Is.Zero); Assert.That(((int[])Get(f.rules, "cleared"))[0], Is.Zero);
            Assert.That(Get(f.rules, "Phase").ToString(), Is.EqualTo("Countdown")); Assert.That(Vector3.Distance(f.body.position, start), Is.LessThan(2));
            Assert.That((int)Get(f.match, "ActualBells"), Is.Zero); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero); Assert.That(((bool[])Get(f.rules, "finished"))[0], Is.False);
            Assert.That(Vector3.Distance(inactiveBody.position, inactivePosition), Is.LessThan(.001f), "Restart must not reposition an inactive athlete."); Assert.That(((int[])Get(f.rules, "falls"))[1], Is.Zero); Assert.That(float.IsPositiveInfinity(((float[])Get(f.rules, "adjustedTime"))[1]), Is.True);
            for (int index = 0; index < bodies.Length; index++) { Assert.That(bodies[index].isKinematic, Is.True); Assert.That(Vector3.Distance(bodies[index].position, poses[index]), Is.LessThan(.001f)); Assert.That(Quaternion.Angle(bodies[index].rotation, rotations[index]), Is.LessThan(.05f)); }
            Capture(f, "fall-restart-countdown"); yield return new WaitForSeconds(2.4f); Assert.That(Get(f.rules, "Phase").ToString(), Is.EqualTo("Countdown")); Assert.That((float)Get(f.rules, "Clock"), Is.Zero);
            deadline = Time.realtimeSinceStartup + 3; while (Get(f.rules, "Phase").ToString() == "Countdown" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get(f.rules, "Phase").ToString(), Is.EqualTo("Running")); Assert.That(bodies.Any(body => !body.isKinematic), Is.True);
            yield return f.Fresh(); Assert.That(((int[])Get(f.rules, "falls")).All(value => value == 0), Is.True); Assert.That((int)Get(f.match, "ActualRecoveries"), Is.Zero); Assert.That((float)Get(f.athlete, "Charge"), Is.Zero);
            TestContext.WriteLine("Controlled ordered-progress/water-entry fixture exercised original fall detection, same-athlete start teleport, actual obstacle reset/3-second countdown, no score, untouched inactive athlete and rematch. Not full-course traversal acceptance.");
        }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator OriginalAIDrivesAndHoldsThenReleasesTheSamePhysicalJumpController()
    {
        using (var f = new Fixture("AI"))
        {
            yield return f.Load(1, true); yield return f.Fresh(); float deadline = Time.realtimeSinceStartup + 22; bool charged = false; float furthest = f.body.position.z;
            while ((int)Get(f.athlete, "Jumps") == 0 && Time.realtimeSinceStartup < deadline) { yield return new WaitForFixedUpdate(); charged |= (bool)Get(f.athlete, "Charging") && (float)Get(f.athlete, "Charge") > 0; furthest = Mathf.Max(furthest, f.body.position.z); }
            Assert.That(charged, Is.True); Assert.That((int)Get(f.athlete, "Jumps"), Is.GreaterThan(0));Assert.That(f.body.linearVelocity.y,Is.GreaterThan(6),"Observe the actual upward takeoff at the first jump, not a later trajectory sample.");var takeoff=f.body.position;
            // Launchzone2 legitimately releases within3m, at Z just below0.
            // Retain the positive-course-progress assertion after actual flight.
            deadline=Time.realtimeSinceStartup+2;while(furthest<=.5f&&Time.realtimeSinceStartup<deadline){yield return new WaitForFixedUpdate();furthest=Mathf.Max(furthest,f.body.position.z);}
            Assert.That(furthest, Is.GreaterThan(.5f));Assert.That(f.body.position.z,Is.GreaterThan(takeoff.z+.5f)); Assert.That((float)Get(f.rules, "Penalty"), Is.Zero); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);TestContext.WriteLine("NINJA_AI_REAL_TAKEOFF position="+takeoff+" followedByPhysicalForwardZ="+furthest);
            TestContext.WriteLine("Unmodified route-driving foundation reached the first authored launch and used actual held/released NinjaAthlete input; this bounded test does not replace the retained strict six-obstacle/bell NinjaProbe full match.");
        }
    }
}
#endif
