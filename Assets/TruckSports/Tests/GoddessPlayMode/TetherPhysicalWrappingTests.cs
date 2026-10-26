#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TetherPhysicalWrappingTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v)).Invoke(owner, args);
    static IDisposable Preferences(string key)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { key }); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); return scope;
    }
    static GameObject IsolateInputs()
    {
        var owner = new GameObject("Tether physical regression input isolation"); UnityEngine.Object.DontDestroyOnLoad(owner); owner.AddComponent(Runtime("SportsProbeInputIsolation")); return owner;
    }
    static Component Match() => (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckTetherball.TetherMatch, Assembly-CSharp", true));
    static IEnumerator Load()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTetherball/Scenes/TruckTetherball.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
    }
    static void Start(Component match)
    {
        Call(match, "AllBots"); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0; Call(match, "Simulate", 3f);
        // These are controlled manual-physics fixtures, not AI-match tests.
        // Disabling the NWH motors alone does not disable the original bot's
        // 14-second stuck recovery, which otherwise introduces real contacts
        // into the deliberately unstruck multi-orbit control.
        foreach (var player in (Array)Get(Get(match, "setup"), "players"))
        { var field = player.GetType().GetField("source"); field.SetValue(player, Enum.Parse(field.FieldType, "Keyboard")); }
        foreach (var truck in (Array)Get(match, "trucks")) { Call(truck, "Power", false); var body = (Rigidbody)Get(truck, "body"); body.isKinematic = true; }
        Physics.SyncTransforms();
    }
    static Vector3 Anchor(ConfigurableJoint joint) => joint.connectedBody == null ? joint.connectedAnchor : joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
    static void NativeContactFixture(Component match)
    {
        Call(Get(match, "rules"), "Reset", 3, 4); Call(match, "ResetRound"); Call(match, "Simulate", 3f); var body = (Rigidbody)Get(match, "ball"); var truck = ((Array)Get(match, "trucks")).GetValue(0); var truckBody = (Rigidbody)Get(truck, "body"); var shape = (BoxCollider)Get(truck, "shape");
        Call(truck, "Prepare", new Vector3(body.position.x, .65f, body.position.z - 15), Quaternion.identity, false); truckBody.isKinematic = true; Physics.SyncTransforms();
        float front = shape.bounds.max.z; var p = truckBody.position; p.z += body.position.z - 3.3f - front; Call(truck, "Prepare", p, Quaternion.identity, false); truckBody.isKinematic = true;
        var second = ((Array)Get(match, "trucks")).GetValue(1); Call(second, "Prepare", new Vector3(-70, .65f, -50), Quaternion.identity, false); ((Rigidbody)Get(second, "body")).isKinematic = true;
        body.linearVelocity = Vector3.back * 10; Physics.SyncTransforms();
    }
    static void CableGeometry(object cable, float degrees)
    {
        var body = (Rigidbody)Get(cable, "ball"); var joint = (ConfigurableJoint)Get(cable, "constraint"); var top = (Transform)Get(cable, "poleTop"); var segments = (Transform[])Get(cable, "segments");
        Call(cable, "Tick", degrees); float radius = (float)Get(cable, "CoilRadius"), pitch = (float)Get(cable, "wrapPitch");
        float arc = Mathf.Abs(degrees) * Mathf.Deg2Rad * Mathf.Sqrt(radius * radius + Mathf.Pow(pitch / (2 * Mathf.PI), 2));
        Assert.That((float)Get(cable, "WrappedLength"), Is.EqualTo(arc).Within(.0002f)); Assert.That(joint.linearLimit.limit, Is.EqualTo(Mathf.Max(2, (float)Get(cable, "InitialLength") - arc)).Within(.0002f));
        var anchor = (Vector3)Get(cable, "FreeAnchor"); Assert.That(Vector3.Distance(anchor, Anchor(joint)), Is.LessThan(.0001f));
        Assert.That(anchor.y, Is.EqualTo(top.position.y - Mathf.Abs(degrees) / 360 * pitch).Within(.0001f));
        int coil = Mathf.Min(48, segments.Length - 1); var end = segments[coil - 1]; var firstTail = segments[coil];
        if (Mathf.Abs(degrees) > .001f) Assert.That(Vector3.Distance(end.position + end.up * end.localScale.y, anchor), Is.LessThan(.001f), "The final visible coil must meet the physical joint anchor.");
        Assert.That(Vector3.Distance(firstTail.position - firstTail.up * firstTail.localScale.y, anchor), Is.LessThan(.001f));
        var radial = Vector3.ProjectOnPlane(anchor - top.position, Vector3.up); var tail = Vector3.ProjectOnPlane(body.position - anchor, Vector3.up);
        Assert.That(Vector3.Dot(radial, tail), Is.GreaterThanOrEqualTo(-.001f), "The free rope must leave outward or tangent, never pass back through the pole.");
        if (Mathf.Abs(degrees) >= (float)Get(cable, "capHandoffDegrees")) Assert.That(Mathf.Abs(Vector3.Dot(radial.normalized, tail.normalized)), Is.LessThan(.0002f), "Established coils must release at the actual tangent.");
        Assert.That(segments.Length, Is.EqualTo(64)); Assert.That(segments.All(segment => segment.GetComponent<Collider>() == null), Is.True);
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator SavedJointUsesHelicalLengthContinuousTangentAnchorAndNativePhysicalWindingUnwinding()
    {
        var prefs = Preferences("CarnivalTetherJoint"); var isolation = IsolateInputs(); Component match = null; var priorSimulation = Physics.simulationMode; GameObject connected = null;
        try
        {
            yield return Load(); match = Match(); Start(match); var cable = Get(match, "cable"); var body = (Rigidbody)Get(match, "ball"); var rules = Get(match, "rules"); var joint = body.GetComponent<ConfigurableJoint>(); var top = (Transform)Get(cable, "poleTop");
            Assert.That(Get(match, "requiredTurns"), Is.EqualTo(4), "The previously saved two-wrap field must upgrade to the doubled minimum."); Assert.That(Get(rules, "TargetTurns"), Is.EqualTo(4));
            Assert.That(joint.xMotion, Is.EqualTo(ConfigurableJointMotion.Limited)); Assert.That(joint.linearLimitSpring.spring, Is.GreaterThan(0)); Assert.That(joint.connectedBody, Is.Null);
            int parked = 0; foreach (var truck in (Array)Get(match, "trucks")) { Call(truck, "Prepare", new Vector3(parked++ == 0 ? -70 : 70, .65f, -50), Quaternion.identity, false); ((Rigidbody)Get(truck, "body")).isKinematic = true; } Physics.SyncTransforms();
            float initial = (float)Get(cable, "InitialLength");
            foreach (float degrees in new[] { 0f, 45, 360, 720, 1440, 720, 45, .1f, 0, -.1f, -45, -360, 0 }) CableGeometry(cable, degrees);
            Call(cable, "Tick", .01f); var positive = Anchor(joint); Call(cable, "Tick", -.01f); Assert.That(Vector3.Distance(positive, Anchor(joint)), Is.LessThan(.001f), "Opposite winding shares a continuous near-side cap exit at zero.");
            connected = new GameObject("Controlled transformed joint connected-body fixture"); connected.transform.SetPositionAndRotation(new Vector3(11, 3, -7), Quaternion.Euler(13, 42, 8)); var fixedBody = connected.AddComponent<Rigidbody>(); fixedBody.isKinematic = true;
            joint.connectedBody = fixedBody; CableGeometry(cable, 720); Assert.That(Vector3.Distance(joint.connectedAnchor, (Vector3)Get(cable, "FreeAnchor")), Is.GreaterThan(1), "Non-null connected bodies require local coordinates."); joint.connectedBody = null; Call(cable, "Tick", 0f);
            Physics.simulationMode = SimulationMode.Script;
            // Explicit initial native tangent velocity and bounded drive forces.
            // No ball positions or angles are written during the orbit loops.
            body.linearVelocity = Vector3.forward * 18; float net = 0, previous = Mathf.Atan2(body.position.z - top.position.z, body.position.x - top.position.x) * Mathf.Rad2Deg; float peakSpeed = 0, maxExtension = 0; int steps = 0;
            for (; steps < 5000 && net < 1440; steps++)
            {
                var radial = Vector3.ProjectOnPlane(body.position - top.position, Vector3.up).normalized; var tangent = new Vector3(-radial.z, 0, radial.x);
                body.AddForce(tangent * Mathf.Clamp((18 - Vector3.Dot(body.linearVelocity, tangent)) * 4, -30, 30), ForceMode.Acceleration); Physics.Simulate(.02f);
                float angle = Mathf.Atan2(body.position.z - top.position.z, body.position.x - top.position.x) * Mathf.Rad2Deg; net += Mathf.DeltaAngle(previous, angle); previous = angle; Call(cable, "Tick", net);
                peakSpeed = Mathf.Max(peakSpeed, body.linearVelocity.magnitude); maxExtension = Mathf.Max(maxExtension, Vector3.Distance(body.position, Anchor(joint)) - joint.linearLimit.limit);
                Assert.That(float.IsFinite(body.position.x) && float.IsFinite(body.position.y) && float.IsFinite(body.position.z), Is.True); if (steps % 300 == 0) yield return null;
            }
            Assert.That(net, Is.GreaterThanOrEqualTo(1440), "Controlled actual Rigidbody orbits must reach the doubled four-wrap geometry."); float shortest = joint.linearLimit.limit;
            for (steps = 0; steps < 6500 && net > -.5f; steps++)
            {
                var radial = Vector3.ProjectOnPlane(body.position - top.position, Vector3.up).normalized; var tangent = new Vector3(-radial.z, 0, radial.x);
                body.AddForce(tangent * Mathf.Clamp((-18 - Vector3.Dot(body.linearVelocity, tangent)) * 4, -30, 30), ForceMode.Acceleration); Physics.Simulate(.02f);
                float angle = Mathf.Atan2(body.position.z - top.position.z, body.position.x - top.position.x) * Mathf.Rad2Deg; net += Mathf.DeltaAngle(previous, angle); previous = angle; Call(cable, "Tick", net);
                peakSpeed = Mathf.Max(peakSpeed, body.linearVelocity.magnitude); maxExtension = Mathf.Max(maxExtension, Vector3.Distance(body.position, Anchor(joint)) - joint.linearLimit.limit); if (steps % 300 == 0) yield return null;
            }
            Assert.That(net, Is.LessThanOrEqualTo(-.5f)); Assert.That(joint.linearLimit.limit, Is.GreaterThan(shortest + 15)); Assert.That(joint.linearLimit.limit, Is.EqualTo(initial).Within(1)); Assert.That(peakSpeed, Is.LessThan(45)); Assert.That(maxExtension, Is.LessThan(2));
            Assert.That(Get(rules, "NetDegrees"), Is.Zero); Assert.That(Get(match, "ActualBallContacts"), Is.Zero); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero, "The controlled rope fixture is not a scoring operation.");
            Call(cable, "ResetCable", 8); CableGeometry(cable, 2880); Assert.That(joint.linearLimit.limit, Is.GreaterThan(2)); Call(cable, "ResetCable", 4); Assert.That(Get(cable, "InitialLength"), Is.EqualTo(initial));
            TestContext.WriteLine("CONTROLLED_TETHER_NATIVE_JOINT net=" + net + " peakSpeed=" + peakSpeed + " maxActualAnchorExtension=" + maxExtension + "; original saved PhysX ball/joint, explicit bounded tangent forces, real four-orbit winding and reverse unwind, helical length, tangent exit, zero continuity, local connected-body coordinates, fixed 64 render-only segments. No AI/full-match/human/visual/listening acceptance.");
        }
        finally
        {
            Physics.simulationMode = priorSimulation; Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } if (connected != null) UnityEngine.Object.DestroyImmediate(connected); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator NativePassiveMotionCannotAwardAndRealTruckContactQualifiesDespiteCueThrottleThenRematchResets()
    {
        var prefs = Preferences("CarnivalTetherContact"); var isolation = IsolateInputs(); Component match = null; var priorSimulation = Physics.simulationMode;
        try
        {
            yield return Load(); match = Match(); Start(match); Physics.simulationMode = SimulationMode.Script; var body = (Rigidbody)Get(match, "ball"); var cable = Get(match, "cable"); var rules = Get(match, "rules"); var top = (Transform)Get(cable, "poleTop"); var trucks = (Array)Get(match, "trucks");
            for (int i = 0; i < 2; i++) { Call(trucks.GetValue(i), "Prepare", new Vector3(i == 0 ? -70 : 70, .65f, -50), Quaternion.identity, false); ((Rigidbody)Get(trucks.GetValue(i), "body")).isKinematic = true; }
            Physics.SyncTransforms(); body.linearVelocity = Vector3.forward * 18; float previousAngle = Mathf.Atan2(body.position.z, body.position.x) * Mathf.Rad2Deg, unstruckDegrees = 0; int passiveSteps = 0;
            // Bounded native tangent forces supply reproducible unstruck motion;
            // they are deliberately not truck contacts or wind-only evidence.
            for (; passiveSteps < 6000 && unstruckDegrees < 1440; passiveSteps++)
            {
                var direction = Vector3.ProjectOnPlane(body.position - top.position, Vector3.up).normalized; var tangent = new Vector3(-direction.z, 0, direction.x);
                body.AddForce(tangent * Mathf.Clamp((18 - Vector3.Dot(body.linearVelocity, tangent)) * 4, -30, 30), ForceMode.Acceleration); Physics.Simulate(.02f); Call(match, "Simulate", .02f);
                float angle = Mathf.Atan2(body.position.z, body.position.x) * Mathf.Rad2Deg; unstruckDegrees += Mathf.DeltaAngle(previousAngle, angle); previousAngle = angle; if (passiveSteps % 300 == 0) yield return null;
            }
            Assert.That(unstruckDegrees, Is.GreaterThanOrEqualTo(1440), "Unstruck physical motion must span the whole doubled target to prove no passive award."); Assert.That((float)Get(rules, "Clock"), Is.EqualTo(passiveSteps * .02f).Within(.05f)); Assert.That(Get(rules, "NetDegrees"), Is.Zero); Assert.That(Get(match, "ActualBallContacts"), Is.Zero); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
            // Controlled actual contact initial conditions, not a direct call to
            // BallContact or rules.Sample. The original NWH truck collider and
            // original TetherBallContact callback must perform qualification.
            NativeContactFixture(match); ((float[])Get(match, "contactCue"))[0] = 100;
            for (int i = 0; i < 100 && (int)Get(match, "ActualBallContacts") == 0; i++) { Physics.Simulate(.02f); Call(match, "Simulate", .02f); }
            Assert.That(Get(match, "ActualBallContacts"), Is.GreaterThan(0)); Assert.That(Get(match, "roundHasNativeContact"), Is.True, "First real collision qualifies even while sound/bonus cue is throttled."); Assert.That(((float[])Get(match, "contactCue"))[0], Is.EqualTo(100));
            var orbitStart = (float)Get(rules, "NetDegrees"); var radial = Vector3.ProjectOnPlane(body.position - top.position, Vector3.up).normalized; body.AddForce(new Vector3(-radial.z, 0, radial.x) * 15, ForceMode.VelocityChange);
            for (int i = 0; i < 150; i++) { Physics.Simulate(.02f); Call(match, "Simulate", .02f); }
            Assert.That(Mathf.Abs((float)Get(rules, "NetDegrees") - orbitStart), Is.GreaterThan(5)); Assert.That(body.linearVelocity.magnitude, Is.LessThanOrEqualTo(33)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero, "A genuine partial orbit does not fabricate a round award.");
            var meshOwner = body.transform.Find("Modeled rubber tetherball panel seams"); Assert.That(meshOwner, Is.Not.Null); Assert.That(meshOwner.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(2304)); Assert.That(meshOwner.GetComponent<Collider>(), Is.Null);
            var rubber = body.GetComponentsInChildren<MeshRenderer>().First(renderer => renderer.transform != meshOwner).sharedMaterial; var seamMesh = meshOwner.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(rubber.name, Does.Contain("owned pebbled gold rubber")); Assert.That((float)Get(match, "ballImpactSpeedMultiplier"), Is.EqualTo(1.35f));
            Call(match, "ResetRound"); Assert.That(Get(match, "roundHasNativeContact"), Is.False); // Match ResetRound owns bodies; rules.NextRound owns winding.
            Call(match, "SetupScreen"); Call(match, "AllBots"); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
            Assert.That(Get(match, "ActualBallContacts"), Is.Zero); Assert.That(Get(match, "roundHasNativeContact"), Is.False); Assert.That(Get(rules, "NetDegrees"), Is.Zero); Assert.That(Get(rules, "Clock"), Is.Zero); Assert.That((int[])Get(rules, "wins"), Is.All.Zero);
            Assert.That(body.transform.Find("Modeled rubber tetherball panel seams").GetComponent<MeshFilter>().sharedMesh, Is.SameAs(seamMesh)); Assert.That(body.GetComponentsInChildren<MeshRenderer>().Count(renderer => renderer.name == "Modeled rubber tetherball panel seams"), Is.EqualTo(1));
            TestContext.WriteLine("CONTROLLED_TETHER_NATIVE_CONTACT unstruckDegrees=" + unstruckDegrees + "; actual PhysX full four unstruck orbits under explicit bounded tangent forces, no wraps/score; actual original NWH collider contact qualified independently of cue throttle; subsequent native partial orbit progressed; bounded ball speed; original owned pebbled material and collider-free modeled seams reused once across clean rematch. No direct contact callback/rules scoring injection, no wind-only/full AI/human/visual/listening acceptance.");
        }
        finally
        {
            Physics.simulationMode = priorSimulation; Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator NativeTruckCollisionReceivesBoundedBoostAndExistingPoleColliderStopsTheRubberBall()
    {
        var prefs = Preferences("CarnivalTetherNativeImpact"); var isolation = IsolateInputs(); Component match = null; var priorSimulation = Physics.simulationMode;
        try
        {
            yield return Load(); match = Match(); Start(match); Physics.simulationMode = SimulationMode.Script; var body = (Rigidbody)Get(match, "ball"); var cable = Get(match, "cable"); var feedback = Get(match, "feedback");
            var collisionVelocity = new Vector3[2];
            for (int pass = 0; pass < 2; pass++)
            {
                Set(match, "ballImpactSpeedMultiplier", pass == 0 ? 1f : 1.35f); NativeContactFixture(match); int before = (int)Get(match, "ActualBallContacts"), sounds = (int)Get(feedback, "SpatialPlayed");
                for (int step = 0; step < 100 && (int)Get(match, "ActualBallContacts") == before; step++) Physics.Simulate(.02f);
                Assert.That(Get(match, "ActualBallContacts"), Is.GreaterThan(before)); collisionVelocity[pass] = body.linearVelocity; Assert.That(collisionVelocity[pass].magnitude, Is.GreaterThan(.1f)); Assert.That(collisionVelocity[pass].magnitude, Is.LessThanOrEqualTo(32.01f));
                Assert.That((float[])Get(match, "contactCue"), Has.Some.GreaterThan(0f)); Assert.That((int)Get(feedback, "SpatialPlayed") - sounds, Is.InRange(0, 1), "Existing settings and pooled voice availability may suppress sound, but an impact cannot spam it.");
            }
            Assert.That(collisionVelocity[1].magnitude / collisionVelocity[0].magnitude, Is.InRange(1.12f, 1.6f), "The same controlled native impact must gain significant mode-local responsiveness."); Assert.That(Vector3.Dot(collisionVelocity[0].normalized, collisionVelocity[1].normalized), Is.GreaterThan(.9f), "The boost preserves the actual native response direction.");
            Set(match, "ballImpactSpeedMultiplier", 1.35f); NativeContactFixture(match);
            foreach (var truck in (Array)Get(match, "trucks")) { Call(truck, "Prepare", new Vector3(-70, .65f, -50), Quaternion.identity, false); ((Rigidbody)Get(truck, "body")).isKinematic = true; }
            // Explicit pole-contact initial condition. Subsequent motion and
            // separation come from the saved native ball/pole colliders.
            body.position = new Vector3(6, 4, 0); body.linearVelocity = Vector3.left * 8; body.angularVelocity = Vector3.zero; Call(cable, "Tick", 0f); Physics.SyncTransforms();
            var recorder = body.gameObject.AddComponent<TetherPhysicalContactRecorder>(); float minimumRadius = float.MaxValue;
            for (int step = 0; step < 120; step++) { Physics.Simulate(.02f); Call(cable, "Tick", 0f); minimumRadius = Mathf.Min(minimumRadius, Vector3.ProjectOnPlane(body.position - ((Transform)Get(cable, "poleTop")).position, Vector3.up).magnitude); }
            Assert.That(recorder.PoleContacts, Is.GreaterThan(0)); Assert.That(minimumRadius, Is.GreaterThan(3.5f), "The three-metre ball must not tunnel through the existing .8-metre mast."); Assert.That(body.linearVelocity.magnitude, Is.LessThan(20)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
            TestContext.WriteLine("CONTROLLED_TETHER_NATIVE_IMPACT baseline=" + collisionVelocity[0] + " boosted=" + collisionVelocity[1] + " poleContacts=" + recorder.PoleContacts + " minimumPoleRadius=" + minimumRadius + "; controlled same-pose original NWH collision, existing callback mode-local boost/direction and cue bound, actual original pole collision. Explicit initial poses are fixtures; no full-match, human, listening or visual acceptance."); UnityEngine.Object.DestroyImmediate(recorder);
        }
        finally
        {
            Physics.simulationMode = priorSimulation; Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }
}

public sealed class TetherPhysicalContactRecorder : MonoBehaviour
{
    public int PoleContacts { get; private set; }
    void OnCollisionEnter(Collision collision) { if (collision.collider.name == "Giant physical schoolyard tether pole") PoleContacts++; }
}
#endif
