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

namespace LWS.TruckPinataPanic.Tests
{
    public sealed class PinataPhysicalContactTelemetry : MonoBehaviour
    {
        public int Events { get; private set; }
        public string Last { get; private set; } = "none";
        void OnCollisionEnter(Collision collision)
        {
            Events++; var point = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Last = collision.collider.name + " rb=" + (collision.rigidbody != null ? collision.rigidbody.name : "static") + " relative=" + collision.relativeVelocity.ToString("F4") + " normal=" + point.normal.ToString("F4") + " closing=" + Mathf.Abs(Vector3.Dot(collision.relativeVelocity, point.normal)).ToString("R") + " impulse=" + collision.impulse.ToString("F4");
        }
    }
    public sealed class PinataLifecycleTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string CaptureSession = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
        static object Get(object value, string member) => value.GetType().GetField(member, Any) is FieldInfo field ? field.GetValue(value) : value.GetType().GetProperty(member, Any).GetValue(value);
        static void Set(object value, string member, object next) => value.GetType().GetField(member, Any).SetValue(value, next);
        static object Call(object value, string method, params object[] args) => value.GetType().GetMethods(Any).Single(m => m.Name == method && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(ok => ok)).Invoke(value, args);
        static object Registry(string method, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(method).Invoke(null, args);
        static Rigidbody Body(object component) => (Rigidbody)Get(component, "body");
        sealed class Fixture : IDisposable
        {
            readonly IDisposable prefs; readonly GameObject isolation;
            readonly InputSettings originalSettings, focusedSettings;
            readonly Gamepad priorCurrentPad;
            readonly System.Collections.Generic.List<Gamepad> syntheticPads = new System.Collections.Generic.List<Gamepad>(4);
            public Behaviour match; public object rules, shell; public object[] trucks, candies;
            PinataPhysicalContactTelemetry contactTelemetry;
            float nextTelemetry; string previousPhase; string telemetryPath;
            readonly System.Text.StringBuilder telemetry = new System.Text.StringBuilder(32768);
            public void ObservePhysicalState(bool force = false)
            {
                if (match == null) return;
                string phase = Get(rules, "Phase").ToString(); float clock = (float)Get(rules, "Clock");
                if (!force && phase == previousPhase && clock < nextTelemetry) return;
                previousPhase = phase; nextTelemetry = clock + 2;
                var body = Body(shell); var joint = (ConfigurableJoint)Get(shell, "suspension");
                var shapes = ((Collider[])Get(shell, "shellColliders")).Where(c => c.enabled && c.gameObject.activeInHierarchy).ToArray();
                var bounds = new Bounds(body.position, Vector3.zero); foreach (var shape in shapes) bounds.Encapsulate(shape.bounds);
                var drivers = (Array)Get(match, "drivers"); var returning = (bool[])Get(match, "returning");
                string line = "PINATA_PHYSICAL clock=" + clock.ToString("R") + " phase=" + phase + " round=" + Get(rules, "Round") + " hp=" + Get(shell, "Health") + "/" + Get(shell, "MaximumHealth") + " accepted=" + Get(shell, "AcceptedContacts") + " ruptures=" + Get(rules, "Ruptures") + " shellPos=" + body.position.ToString("F4") + " velocity=" + body.linearVelocity.ToString("F4") + " rotation=" + body.rotation.ToString("F4") + " kinematic=" + body.isKinematic + " constraints=" + body.constraints + " mass=" + body.mass + " bounds=" + bounds.ToString("F4") + " jointAnchor=" + joint.connectedAnchor.ToString("F4") + " limit=" + joint.linearLimit.limit + " collisionEvents=" + contactTelemetry.Events + " lastContact=" + contactTelemetry.Last;
                for (int i = 0; i < (int)Get(Get(match, "setup"), "count"); i++)
                {
                    var truck = trucks[i]; var rb = Body(truck); var driver = drivers.GetValue(i);
                    line += " | truck" + i + " pos=" + rb.position.ToString("F3") + " vel=" + rb.linearVelocity.ToString("F3") + " cmd=" + ((Vector2)Get(truck, "command")).ToString("F3") + " powered=" + Get(truck, "Powered") + " kinematic=" + rb.isKinematic + " grounded=" + Get(truck, "Grounded") + " goal=" + (driver != null ? Get(driver, "Goal") : "not initialized") + " desired=" + (driver != null ? Get(driver, "DesiredSpeed") : "none") + " blocking=" + (driver != null ? Get(driver, "BlockingObstacle") : "none") + " reversing=" + (driver != null ? Get(driver, "Reversing") : "none") + " progressStall=" + (driver != null ? Get(driver, "SecondsWithoutProgress") : "none") + " recoveries=" + (driver != null ? Get(driver, "RecoveryStarts") : "none") + " aimLocked="+((bool[])Get(match,"strikeAimLocked"))[i]+" aimUpdates="+Get(match,"AIStrikeAimUpdates")+" passOrigin="+((Vector3[])Get(match,"strikeOrigins"))[i].ToString("F3")+" returnRoute=" + returning[i] + " routeCursor=" + (driver != null ? Get(driver, "Cursor") : "none") + " routePoints=" + (driver != null ? ((System.Collections.ICollection)Get(driver, "points")).Count.ToString() : "none");
                }
                TestContext.WriteLine(line); telemetry.AppendLine(line); File.WriteAllText(telemetryPath, telemetry.ToString());
            }
            public Fixture()
            {
                prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "PinataNative" + Guid.NewGuid().ToString("N") }); Registry("ForgetCache");
                originalSettings = InputSystem.settings; priorCurrentPad = Gamepad.current; focusedSettings = UnityEngine.Object.Instantiate(originalSettings);
                focusedSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                focusedSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                focusedSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = focusedSettings;
                isolation = new GameObject("Pinata native fixture physical input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
            }
            public Gamepad AddPad()
            {
                var pad = InputSystem.AddDevice<Gamepad>(); syntheticPads.Add(pad); InputSystem.EnableDevice(pad); pad.MakeCurrent();
                Assert.That(pad.added && pad.enabled && Gamepad.current == pad, Is.True, "Each explicit software pad is enabled/current under the scoped GameView focus route."); return pad;
            }
            public IEnumerator Load()
            {
                Assert.That(File.Exists("Assets/TruckPinataPanic/Scenes/TruckPinataPanic.unity"), Is.True, "Lead must run PinataAuthoring.Create once on the explicit NEW venue.");
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckPinataPanic/Scenes/TruckPinataPanic.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckPinataPanic.PinataMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Cast<Behaviour>().Single();
                rules = Get(match, "rules"); shell = Get(match, "pinata"); contactTelemetry = ((Component)shell).gameObject.AddComponent<PinataPhysicalContactTelemetry>(); string telemetryDir = "Logs/TruckSports/PlaytestRepair/PinataPanicControlledNative/" + CaptureSession; Directory.CreateDirectory(telemetryDir); telemetryPath = Path.Combine(telemetryDir, "physical-" + Guid.NewGuid().ToString("N") + ".txt"); trucks = ((Array)Get(match, "trucks")).Cast<object>().ToArray(); candies = ((Array)Get(match, "candies")).Cast<object>().ToArray();
                var suspension = (ConfigurableJoint)Get(shell, "suspension");
                Assert.That(Vector3.Distance(suspension.anchor, new Vector3(0, 5.1f, 0)), Is.LessThan(.0001f), "Explicit owned author repair must suspend the real modeled top, not the center of mass.");
                Assert.That(suspension.linearLimit.limit, Is.EqualTo(4.9f).Within(.0001f)); Assert.That(suspension.connectedBody, Is.Null); Assert.That(suspension.angularXMotion, Is.EqualTo(ConfigurableJointMotion.Free)); Assert.That(suspension.angularYMotion, Is.EqualTo(ConfigurableJointMotion.Free)); Assert.That(suspension.angularZMotion, Is.EqualTo(ConfigurableJointMotion.Free));
                Assert.That(trucks.Length, Is.EqualTo(4)); Assert.That(candies.Length, Is.EqualTo(48)); Assert.That(trucks.All(t => Get(t, "tractorMotor") != null), Is.True, "Full existing NWH tractor authority is mandatory.");
                Assert.That(trucks.All(t => Get(t, "appearanceHost") != null), Is.True, "Lead-owned shared appearance author must bind each actual NWH actor.");
            }
            public void Start(int count = 4)
            {
                Call(match, "SetupScreen"); Set(Get(match, "setup"), "count", count); Set(match, "option", 0); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var players = (Array)Get(Get(match, "setup"), "players"); for (int i = 0; i < count; i++) { var p = players.GetValue(i); var difficulty = p.GetType().GetField("difficulty"); difficulty.SetValue(p, Enum.ToObject(difficulty.FieldType, 2)); }
                Assert.That((bool)Call(match, "StartMatch"), Is.True); nextTelemetry = 0; previousPhase = null;
            }
            public void Dispose()
            {
                Time.timeScale = 1;
                try { if (match != null) { ObservePhysicalState(true); Call(match, "SetupScreen"); } }
                finally
                {
                    foreach (var pad in syntheticPads) if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
                    UnityEngine.Object.DestroyImmediate(isolation); Registry("ForgetCache"); prefs.Dispose();
                    InputSystem.settings = originalSettings; UnityEngine.Object.DestroyImmediate(focusedSettings);
                    if (priorCurrentPad != null && priorCurrentPad.added && priorCurrentPad.enabled) priorCurrentPad.MakeCurrent();
                }
            }
        }
        static IEnumerator Until(Func<bool> predicate, float seconds, string message)
        {
            float end = Time.realtimeSinceStartup + seconds; while (!predicate() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(predicate(), Is.True, message);
        }
        static IEnumerator Capture(Behaviour match, string name, int width, int height)
        {
            yield return null; yield return null;
            string dir = "Logs/TruckSports/PlaytestRepair/PinataPanicControlledNative/" + CaptureSession; Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "controlled-" + name + ".png");
            var save = Sports("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5 && method.GetParameters()[0].ParameterType.IsInstanceOfType(match));
            save.Invoke(null, new object[] { match, width, height, path, null });
            Assert.That(File.Exists(path), Is.True, "Controlled native Camera.Render/UI composite must exist; actual Windows backbuffer capture remains a separate pending gate.");
            yield return null; var image = new Texture2D(2, 2); try { Assert.That(image.LoadImage(File.ReadAllBytes(path)), Is.True); Assert.That(new Vector2Int(image.width, image.height), Is.EqualTo(new Vector2Int(width, height))); } finally { UnityEngine.Object.DestroyImmediate(image); }
            TestContext.WriteLine("PINATA_CONTROLLED_NATIVE_CAPTURE " + width + "x" + height + " " + path + "; not an actual GameView/Windows/human capture.");
        }
        [UnityTest, Timeout(180000)] public IEnumerator FourRealAIDriveStrikeRuptureCollectFinishAndRematch()
        {
            using (var f = new Fixture())
            {
                yield return f.Load(); f.Start(); string matchId = (string)Get(f.match, "MatchId");
                yield return new WaitForFixedUpdate(); Assert.That((int[])Get(f.rules, "points"), Is.All.Zero, "Countdown and ambient physics cannot invent strike or candy points.");
                var shellBody = Body(f.shell); var start = shellBody.position; float maxSwing = 0; int maxNativeContacts = 0; bool paused = false, captured = false, strikeCaptured = false;
                float end = Time.realtimeSinceStartup + 135; Time.timeScale = 3;
                while (!(bool)Get(f.match, "Finished") && Time.realtimeSinceStartup < end)
                {
                    f.ObservePhysicalState(); maxSwing = Mathf.Max(maxSwing, Vector3.ProjectOnPlane(shellBody.position - start, Vector3.up).magnitude); maxNativeContacts = Mathf.Max(maxNativeContacts, (int)Get(f.shell, "AcceptedContacts"));
                    if (!strikeCaptured && Get(f.rules, "Phase").ToString() == "Striking" && (float)Get(f.rules, "Clock") >= 12)
                    {
                        strikeCaptured = true; Time.timeScale = 1; yield return Capture(f.match, "natural-AI-striking-diagnostic-1280", 1280, 720); Time.timeScale = 3;
                    }
                    if (!captured && (int)Get(f.rules, "Ruptures") > 0 && (int)Get(f.rules, "RemainingCandy") > 0)
                    {
                        captured = true; Assert.That(f.candies.Any(c => (bool)Get(c, "Released") && !Body(c).isKinematic && Body(c).linearVelocity.magnitude > .1f), Is.True, "Real burst must release moving physical candies, not score tokens.");
                        Time.timeScale = 1; yield return Capture(f.match, "natural-four-AI-candy-1280", 1280, 720); yield return Capture(f.match, "natural-four-AI-candy-1920", 1920, 1080); Time.timeScale = 3;
                    }
                    if (!paused && (int)Get(f.match, "NativePickups") > 0)
                    {
                        paused = true; Call(f.match, "Pause", 0, false); float clock = (float)Get(f.rules, "Clock"), health = (float)Get(f.shell, "Health"); int[] points = ((int[])Get(f.rules, "points")).ToArray(); var positions = f.trucks.Select(Body).Select(b => b.position).ToArray();
                        float hold = Time.realtimeSinceStartup + .25f; while (Time.realtimeSinceStartup < hold) yield return null;
                        Assert.That((float)Get(f.rules, "Clock"), Is.EqualTo(clock)); Assert.That((float)Get(f.shell, "Health"), Is.EqualTo(health)); Assert.That((int[])Get(f.rules, "points"), Is.EqualTo(points));
                        for (int i = 0; i < positions.Length; i++) Assert.That(Vector3.Distance(positions[i], Body(f.trucks[i]).position), Is.LessThan(.001f)); Call(f.match, "Resume"); Time.timeScale = 3;
                    }
                    yield return null;
                }
                Assert.That((bool)Get(f.match, "Finished"), Is.True, "Finite natural three-pinata match must reach its real result page.");
                f.ObservePhysicalState(true); Assert.That(maxNativeContacts, Is.GreaterThan(0), "At least one native truck impact must pass the real closing-speed damage gate."); Assert.That(maxSwing, Is.GreaterThan(.15f), "The real suspended shell must swing horizontally after native truck impact."); Assert.That((int)Get(f.rules, "Ruptures"), Is.GreaterThan(0), "Real CoreCollapse health must reach one atomic rupture."); Assert.That((int)Get(f.match, "NativePickups"), Is.GreaterThan(0), "AI must collect actual released physical candy."); Assert.That((int)Get(f.match, "AIReplans"), Is.GreaterThan(0)); Assert.That(paused && captured, Is.True);
                Assert.That(((int[])Get(f.rules, "collected")).Sum(), Is.GreaterThan(0)); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.GreaterThan(0));
                TestContext.WriteLine("PINATA_NATURAL swing=" + maxSwing + " contacts=" + maxNativeContacts + " ruptures=" + Get(f.rules, "Ruptures") + " pickups=" + Get(f.match, "NativePickups") + " threatAvoidances=" + Get(f.match, "AIThreatAvoidances"));
                f.Start(); Assert.That((string)Get(f.match, "MatchId"), Is.Not.EqualTo(matchId)); Assert.That((int[])Get(f.rules, "points"), Is.All.Zero); Assert.That((int)Get(f.rules, "Ruptures"), Is.Zero); Assert.That(f.candies.All(c => !(bool)Get(c, "Released")), Is.True); Assert.That((float)Get(f.shell, "Health"), Is.EqualTo((float)Get(f.shell, "MaximumHealth"))); var originalJoint = (ConfigurableJoint)Get(f.shell, "suspension"); Assert.That(originalJoint.anchor, Is.EqualTo(new Vector3(0, 5.1f, 0))); Assert.That(originalJoint.linearLimit.limit, Is.EqualTo(4.9f).Within(.0001f), "Rematch preserves authored physical suspension; no runtime authoring overwrite.");
            }
        }
        [UnityTest, Timeout(130000)] public IEnumerator SoloAiRebuildsRealRunupsAcrossConsecutiveStartsWithOriginalHealth()
        {
            using (var f = new Fixture())
            {
                yield return f.Load(); string previous = null;
                for (int run = 0; run < 2; run++)
                {
                    f.Start(1); string id = (string)Get(f.match, "MatchId"); Assert.That(id, Is.Not.EqualTo(previous)); previous = id;
                    Assert.That((float)Get(f.shell, "MaximumHealth"), Is.EqualTo((float)Get(f.shell, "maximumHealth") / 4)); Time.timeScale = 3;
                    yield return Until(() => { f.ObservePhysicalState(); return (int)Get(f.rules, "Ruptures") > 0; }, 50, "The genuine solo AI must rebuild actual NWH run-ups and rupture the unchanged600HP shell.");
                    Assert.That((int)Get(f.shell, "AcceptedContacts"), Is.GreaterThan(0)); Assert.That((int)Get(f.match, "AIStrikePasses"), Is.GreaterThan(0)); Assert.That((int)Get(f.match,"AIStrikeAimUpdates"),Is.GreaterThan((int)Get(f.match,"AIStrikePasses")),"Actual approach tracks the moving shell before close pass commitment."); if ((int)Get(f.match, "AIReturnRoutes") > 0) Assert.That((int)Get(f.match, "AIStrikePasses"), Is.GreaterThan(1), "A completed physical return route must establish a fresh forward pass before solo rupture.");
                    Assert.That((float)Get(f.shell, "Health"), Is.Zero); Assert.That((int[])Get(f.rules, "points"), Has.Some.GreaterThan(0));
                }
            }
        }
        [UnityTest, Timeout(90000)] public IEnumerator NativeNormalGoldenExplosiveContactBanksOnlyActualOwnerOnce()
        {
            using (var f = new Fixture())
            {
                yield return f.Load(); f.Start(1); Time.timeScale = 3;
                Assert.That((int)Get(Get(f.match, "setup"), "count"), Is.EqualTo(1));
                Assert.That((float)Get(f.shell, "MaximumHealth"), Is.EqualTo((float)Get(f.shell, "maximumHealth") / 4), "Use the real solo roster health policy; never directly damage or collapse the shell for this fixture.");
                yield return Until(() => { f.ObservePhysicalState(); return (int)Get(f.rules, "Ruptures") > 0; }, 50, "Real solo AI truck contacts must rupture the real CoreCollapse shell before the owner0 candy collision fixture is allowed.");
                Time.timeScale = 1; f.match.enabled = false; // controlled collision fixture only; native PhysX and original NWH remain active
                for (int i = 1; i < f.trucks.Length; i++) { Call(f.trucks[i], "Power", false); Body(f.trucks[i]).isKinematic = true; }
                var truck = f.trucks[0]; Call(truck, "TickDrive", Vector2.zero); var body = Body(truck);
                foreach (string kind in new[] { "Normal", "Golden", "Explosive" })
                {
                    var candy = f.candies.First(c => (bool)Get(c, "Released") && Get(c, "kind").ToString() == kind);
                    var cb = Body(candy);
                    // The original NWH facade has a separate1500kg Cab body.
                    // Prepare resets its actual attached-body poses; one common
                    // assembly velocity prevents the suspension pulling a lone
                    // teleported root back to the old rupture location.
                    Call(truck, "Prepare", new Vector3(40, .65f, 35), Quaternion.identity, true);
                    Call(Get(truck, "tractorMotor"), "AddAssemblyVelocity", Vector3.forward * 7);
                    cb.position = new Vector3(40, 1.2f, 41); cb.transform.SetPositionAndRotation(cb.position, cb.rotation); cb.linearVelocity = cb.angularVelocity = Vector3.zero;
                    Assert.That((bool)Get(f.match, "CanCollect") && (bool)Get(candy, "Released") && (bool)Get(truck, "Powered") && !body.isKinematic && !cb.isKinematic && cb.detectCollisions, Is.True);
                    var observer = ((Component)candy).GetComponent<PinataPhysicalContactTelemetry>(); if (observer == null) observer = ((Component)candy).gameObject.AddComponent<PinataPhysicalContactTelemetry>();
                    var physical = ((Component)truck).GetComponentsInChildren<Collider>(true).Where(c => c.enabled && !c.isTrigger).ToArray(); Assert.That(physical.Length, Is.GreaterThan(0));
                    int before = ((int[])Get(f.rules, "points"))[0], pickups = (int)Get(f.match, "NativePickups"), explosions = (int)Get(f.match, "NativeExplosions"); Physics.SyncTransforms();
                    float nextDiagnostic = 0;
                    yield return Until(() => { if (Time.realtimeSinceStartup >= nextDiagnostic) { nextDiagnostic = Time.realtimeSinceStartup + 1; TestContext.WriteLine("PINATA_CONTROLLED_CONTACT kind=" + kind + " collectible=" + Get(f.match, "CanCollect") + " released=" + Get(candy, "Released") + " candyPos=" + cb.position.ToString("F3") + " rootPos=" + body.position.ToString("F3") + " rootVelocity=" + body.linearVelocity.ToString("F3") + " events=" + observer.Events + " last=" + observer.Last + " assembly=" + string.Join(";", ((Component)truck).GetComponentsInChildren<Rigidbody>(true).Select(b => b.name + ":" + b.position.ToString("F3") + ":" + b.linearVelocity.ToString("F3")))); } return !(bool)Get(candy, "Released"); }, 4, "The physical original NWH assembly must collide with the selected " + kind + " candy.");
                    int value = kind == "Golden" ? 50 : kind == "Normal" ? 10 : -15;
                    Assert.That(((int[])Get(f.rules, "points"))[0], Is.EqualTo(Math.Max(0, before + value))); Assert.That((int)Get(candy, "CollectedBy"), Is.Zero);
                    Assert.That((int)Get(f.match, "NativePickups"), Is.EqualTo(pickups + 1)); if (kind == "Explosive") { Assert.That((int)Get(f.match, "NativeExplosions"), Is.EqualTo(explosions + 1)); Assert.That(body.linearVelocity.y, Is.GreaterThan(.5f), "The actual collector receives the explosive shove."); }
                    int score = ((int[])Get(f.rules, "points"))[0]; yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); Assert.That(((int[])Get(f.rules, "points"))[0], Is.EqualTo(score), "Multiple compound-collider contacts cannot collect a retired candy twice.");
                }
                f.match.enabled = true;
            }
        }
        [UnityTest, Timeout(60000)] public IEnumerator OneThroughFourAIViewsKeepOriginalOwnersAndTypedCards()
        {
            using (var f = new Fixture())
            {
                yield return f.Load(); Sports("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { true });
                var views = Get(f.match, "drivingViews"); Call(views, "Select", 1);
                for (int count = 1; count <= 4; count++)
                {
                    f.Start(count); yield return null; yield return null; var rally = Get(views, "rally"); Assert.That((int)Get(rally, "Humans"), Is.EqualTo(count)); var owners = ((int[])Get(rally, "owners")).Take(count).ToArray(); Assert.That(owners, Is.EqualTo(Enumerable.Range(0, count)));
                    var cameras = (Camera[])Get(f.match, "cameras"); Assert.That(cameras.Length, Is.EqualTo(count)); float area = cameras.Sum(c => c.rect.width * c.rect.height); Assert.That(area, Is.EqualTo(1).Within(.001f));
                    for (int i = 0; i < count; i++) { object[] args = { i, -1, null, null }; Assert.That((bool)f.match.GetType().GetMethod("TryReadViewportCard").Invoke(f.match, args), Is.True); Assert.That((int)args[1], Is.EqualTo(i)); Assert.That((string)args[2], Is.EqualTo("0 PTS")); }
                    Assert.That((string)Get(f.match, "Help"), Does.Contain("2.5 m/s").And.Contain("50").And.Contain("never a teleport"));
                }
                Call(views, "Select", 0); yield return null; Assert.That((bool)Get(views, "NeedsArena"), Is.True); Assert.That(((Camera[])Get(f.match, "cameras")).Length, Is.EqualTo(1));
            }
        }
        [UnityTest, Timeout(45000)] public IEnumerator FourSyntheticLocalPadsDriveTheirOwnOriginalNwhTractors()
        {
            // Software input proves routing, not physical controller acceptance.
            var pads = new Gamepad[4];
            using (var f = new Fixture())
            {
                    yield return f.Load(); Call(f.match, "SetupScreen"); Set(Get(f.match, "setup"), "count", 4); Call(f.match, "NormalizeSetup"); var players = (Array)Get(Get(f.match, "setup"), "players");
                    for (int i = 0; i < 4; i++) { pads[i] = f.AddPad(); var p = players.GetValue(i); Set(p, "source", Enum.Parse(p.GetType().GetField("source").FieldType, "WholeController")); Set(p, "pad", pads[i]); InputSystem.QueueStateEvent(pads[i], new GamepadState()); }
                    Assert.That((bool)Call(f.match, "StartMatch"), Is.True); yield return null; yield return null;
                    yield return Until(() => Get(f.rules, "Phase").ToString() == "Striking", 8, "Original countdown must release the four actual NWH tractors.");
                    var starts = f.trucks.Select(Body).Select(body => body.position).ToArray();
                    for (int i = 0; i < 4; i++) InputSystem.QueueStateEvent(pads[i], new GamepadState { rightTrigger = .8f });
                    float end = Time.realtimeSinceStartup + 2; while (Time.realtimeSinceStartup < end) yield return null;
                    var commands = (Vector2[])Get(f.match, "commands");
                    for (int i = 0; i < 4; i++) { Assert.That(commands[i].y, Is.GreaterThan(.7f)); Assert.That(Vector3.Distance(starts[i], Body(f.trucks[i]).position), Is.GreaterThan(.3f), "Only original NWH input/physics moved this local player's truck."); var host = Get(f.trucks[i], "appearanceHost"); Assert.That((string)Get(host, "ProfileId"), Is.EqualTo((string)Get(players.GetValue(i), "participantId"))); }
                    Call(f.match, "Pause", 2, false); Assert.That((bool)Get(f.match, "Paused"), Is.True); foreach (var pad in pads) InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null; Call(f.match, "Resume"); yield return null;
                    Assert.That((bool)Get(f.match, "Live"), Is.True); Assert.That(f.trucks.All(t => (bool)Get(t, "Powered")), Is.True);
            }
        }
    }
}
#endif
