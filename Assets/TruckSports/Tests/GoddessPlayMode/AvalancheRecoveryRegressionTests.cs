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

public sealed class AvalancheRecoveryRegressionTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v)).Invoke(owner, args);
    static Component Match() => (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckAvalancheEscape.AvalancheMatch, Assembly-CSharp", true));
    static IDisposable Preferences(string key)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { key });
        Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); return scope;
    }
    static GameObject IsolateInputs()
    {
        var owner = new GameObject("Avalanche bounded recovery regression input isolation"); UnityEngine.Object.DontDestroyOnLoad(owner); owner.AddComponent(Runtime("SportsProbeInputIsolation")); return owner;
    }
    static IEnumerator Load()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
    }
    static void Start(Component match, int count)
    {
        Call(match, "AllBots"); Set(Get(match, "setup"), "count", count); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
    }
    static Vector3 RoadPoint(Component match)
    {
        var points = (Vector3[])Get(((Array)Get(Get(match, "world"), "routes")).GetValue(0), "points");
        return points.First(point => point.z > 450 && point.z < 1500);
    }
    static Rigidbody RolledFixture(object truck, Vector3 point, float roll)
    {
        var body = (Rigidbody)Get(truck, "body"); var shape = (BoxCollider)Get(truck, "shape"); var rotation = Quaternion.Euler(0, 0, roll);
        var hits = Physics.RaycastAll(point + Vector3.up * 30, Vector3.down, 70, ~0, QueryTriggerInteraction.Ignore).Where(hit => hit.rigidbody == null && hit.normal.y > .2f).ToArray();
        Assert.That(hits, Is.Not.Empty, "The explicit rolled fixture must be supported by the real existing road/terrain collider.");
        float ground = hits.Max(hit => hit.point.y); Call(truck, "Prepare", new Vector3(point.x, ground + 10, point.z), rotation, true); Physics.SyncTransforms();
        var supported = body.position; supported.y = ground + body.position.y - shape.bounds.min.y + .025f;
        Call(truck, "Prepare", supported, rotation, true); Physics.SyncTransforms(); return body;
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator ControlledRolledNwhBodiesRecoverThroughRealImpulsesWithoutPoseOrRaceStateReset()
    {
        var prefs = Preferences("CarnivalAvalanchePhysicalRecovery"); var isolation = IsolateInputs(); Component match = null;
        try
        {
            yield return Load(); match = Match(); Start(match, 2); var rules = Get(match, "rules"); var trucks = (Array)Get(match, "trucks");
            Assert.That(Call(match, "TryRecover", 0), Is.False, "Countdown cannot be bypassed with recovery."); Call(rules, "Tick", 3f); Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("Racing"));
            var road = RoadPoint(match); var truck = trucks.GetValue(0); var body = RolledFixture(truck, road, 150);
            Time.timeScale = 1; yield return new WaitForFixedUpdate(); yield return null; Time.timeScale = 0;
            // Explicit initial-condition fixture. Every subsequent recovery pose
            // change below must come from actual Rigidbody integration.
            body.linearVelocity = new Vector3(1.25f, 0, 1.75f); body.angularVelocity = Vector3.zero;
            Call(rules, "Progress", 0, (float)Call(Get(match, "world"), "Progress", body.position)); Call(rules, "SnowContact", 0, 1f, true);
            var distance = (float[])Get(rules, "distance"); var exposure = (float[])Get(rules, "caughtFor"); float banked = distance[0], caught = exposure[0];
            Vector3 before = body.position, velocity = body.linearVelocity; Quaternion rotation = body.rotation; float upBefore = (body.rotation * Vector3.up).y;
            Assert.That(Call(match, "TryRecover", 0), Is.True); Assert.That(Get(match, "PhysicalRecoveries"), Is.EqualTo(1));
            Assert.That(body.position, Is.EqualTo(before), "Recovery may not write a saved route/start position, including a vertical offset."); Assert.That(Quaternion.Angle(rotation, body.rotation), Is.LessThan(.001f), "Recovery torque must not directly assign an upright rotation.");
            Assert.That(body.linearVelocity.x, Is.EqualTo(velocity.x).Within(.0001f)); Assert.That(body.linearVelocity.z, Is.EqualTo(velocity.z).Within(.0001f)); Assert.That(body.linearVelocity.y - velocity.y, Is.InRange(1f, 6f));
            Assert.That(distance[0], Is.EqualTo(banked)); Assert.That(exposure[0], Is.EqualTo(caught)); Assert.That(((bool[])Get(rules, "alive"))[0], Is.True); Assert.That(((bool[])Get(rules, "finished"))[0], Is.False);
            var afterImpulse = body.linearVelocity; Assert.That(Call(match, "TryRecover", 0), Is.False); Assert.That(body.linearVelocity, Is.EqualTo(afterImpulse)); Assert.That(Get(match, "PhysicalRecoveries"), Is.EqualTo(1));
            float cooldownAt = ((float[])Get(match, "recoverAt"))[0]; Assert.That(cooldownAt - (float)Get(rules, "Clock"), Is.EqualTo(6).Within(.001f));
            Time.timeScale = 1; float stopAt = Time.time + .45f, highest = body.position.y;
            while (Time.time < stopAt) { yield return new WaitForFixedUpdate(); highest = Mathf.Max(highest, body.position.y); }
            Time.timeScale = 0; Assert.That(highest - before.y, Is.GreaterThan(.1f)); Assert.That(Quaternion.Angle(rotation, body.rotation), Is.GreaterThan(10)); Assert.That((body.rotation * Vector3.up).y, Is.GreaterThan(upBefore + .2f));
            Assert.That(Vector3.ProjectOnPlane(body.position - before, Vector3.up).magnitude, Is.LessThan(8), "A bounded hop must not move to a previous route node or start."); Assert.That(distance[0], Is.EqualTo(banked)); Assert.That(exposure[0], Is.EqualTo(caught));
            Assert.That(Call(match, "TryRecover", 0), Is.False, "Physical motion does not bypass the unchanged race-clock cooldown.");
            // Exercise the original AI dispatch, not a separate AI recovery API.
            var second = trucks.GetValue(1); var aiBody = RolledFixture(second, road + Vector3.right * 9, -150); aiBody.linearVelocity = aiBody.angularVelocity = Vector3.zero;
            ((float[])Get(match, "stuckFor"))[1] = 4.1f; Vector3 aiPosition = aiBody.position; Quaternion aiRotation = aiBody.rotation;
            Call(match, "Simulate", .02f); Assert.That(Get(match, "PhysicalRecoveries"), Is.EqualTo(2)); Assert.That(aiBody.position, Is.EqualTo(aiPosition)); Assert.That(Quaternion.Angle(aiBody.rotation, aiRotation), Is.LessThan(.001f)); Assert.That(aiBody.linearVelocity.y, Is.InRange(1f, 6f));
            Call(match, "Simulate", .02f); Assert.That(Get(match, "PhysicalRecoveries"), Is.EqualTo(2));
            Call(rules, "Tick", 6.1f); body = RolledFixture(truck, road, 150); body.linearVelocity = body.angularVelocity = Vector3.zero;
            Assert.That(Call(match, "TryRecover", 0), Is.True, "The same bounded physical recovery becomes eligible after the real cooldown boundary."); Assert.That(Get(match, "PhysicalRecoveries"), Is.EqualTo(3));
            Call(match, "Pause", 0, false); Assert.That(Call(match, "TryRecover", 1), Is.False); Call(match, "Resume");
            string prior = (string)Get(match, "MatchId"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); Time.timeScale = 0;
            Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(prior)); Assert.That(Get(match, "PhysicalRecoveries"), Is.Zero); Assert.That(((float[])Get(match, "recoverAt")).Take(2), Is.All.EqualTo(0f)); Assert.That(((float[])Get(rules, "caughtFor")).Take(2), Is.All.EqualTo(0f)); Assert.That(((float[])Get(rules, "distance")).Take(2), Is.All.EqualTo(0f));
            TestContext.WriteLine("CONTROLLED_AVALANCHE_RECOVERY actual saved NWH tractors on the original road, initial rolled/velocity/exposure fixtures explicitly supplied; zero immediate pose writes, real hop/upright integration, preserved horizontal momentum/progress/exposure, original AI dispatch, cooldown and rematch. Not a complete race, human/device or environment acceptance.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator AirborneKinematicAndResolvedTrucksCannotTurnRecoveryIntoExtraFlightOrRaceProgress()
    {
        var prefs = Preferences("CarnivalAvalancheRecoveryGates"); var isolation = IsolateInputs(); Component match = null;
        try
        {
            yield return Load(); match = Match(); Start(match, 2); var rules = Get(match, "rules"); Call(rules, "Tick", 3f);
            var truck = ((Array)Get(match, "trucks")).GetValue(0); var body = (Rigidbody)Get(truck, "body");
            Assert.That(Call(match, "TryRecover", 0), Is.False, "An upright unstuck truck receives no extra jump.");
            // Controlled airborne initial pose, followed by real native wheel
            // and Rigidbody updates so cached grounded state cannot fake support.
            Call(truck, "Prepare", RoadPoint(match) + Vector3.up * 35, Quaternion.Euler(0, 0, 180), true);
            Time.timeScale = 1; yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null; Time.timeScale = 0;
            Assert.That(Get(Get(truck, "tractorMotor"), "Grounded"), Is.False); var before = body.position; var rotation = body.rotation; var velocity = body.linearVelocity;
            Assert.That(Call(match, "TryRecover", 0), Is.False, "An unsupported airborne truck cannot use recovery as a flight impulse."); Assert.That(body.linearVelocity, Is.EqualTo(velocity));
            body.isKinematic = true; Assert.That(Call(match, "TryRecover", 0), Is.False); body.isKinematic = false; velocity = body.linearVelocity;
            var alive = (bool[])Get(rules, "alive"); var finished = (bool[])Get(rules, "finished");
            alive[0] = false; Assert.That(Call(match, "TryRecover", 0), Is.False); alive[0] = true;
            finished[0] = true; Assert.That(Call(match, "TryRecover", 0), Is.False); finished[0] = false;
            Assert.That(body.position, Is.EqualTo(before)); Assert.That(Quaternion.Angle(body.rotation, rotation), Is.LessThan(.001f)); Assert.That(body.linearVelocity, Is.EqualTo(velocity)); Assert.That(Get(match, "PhysicalRecoveries"), Is.Zero);
            Assert.That(((float[])Get(rules, "distance"))[0], Is.Zero); Assert.That(((float[])Get(rules, "caughtFor"))[0], Is.Zero); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
            Call(match, "SetupScreen"); Assert.That(Call(match, "TryRecover", 0), Is.False);
            TestContext.WriteLine("CONTROLLED_AVALANCHE_RECOVERY_GATES original saved NWH body/wheels: native airborne support rejection, upright/unstuck, kinematic, supplied eliminated/finished eligibility flags and setup; no impulse, pose/progress/exposure or score changes. Supplied flags are not real finish/elimination evidence.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator ActualFrontRigidbodyAndDebrisUseTwentyPercentTravelAcrossDifficultiesWithoutCompounding()
    {
        var prefs = Preferences("CarnivalAvalancheFrontSpeed"); var isolation = IsolateInputs(); Component match = null;
        try
        {
            yield return Load(); match = Match(); Assert.That(Get(match, "AvalancheSpeedMultiplier"), Is.EqualTo(1.2f)); var front = Get(match, "front"); float originalAcceleration = (float)Get(front, "acceleration");
            var frontBody = (Rigidbody)Get(front, "body"); var contact = (BoxCollider)Get(front, "contact"); var cloud = (ParticleSystem)Get(front, "cloud"); var rumble = (AudioSource)Get(front, "rumble"); var debris = (Rigidbody[])Get(front, "debris");
            Assert.That(contact.attachedRigidbody, Is.SameAs(frontBody)); Assert.That(contact.isTrigger, Is.True); Assert.That(cloud.transform.IsChildOf(frontBody.transform), Is.True);
            for (int difficulty = 0; difficulty < 3; difficulty++)
            {
                float baselineTravel = 0;
                foreach (float multiplier in new[] { 1f, 1.2f, 1.2f })
                {
                    Call(match, "SetupScreen"); Set(match, "option", difficulty); Set(match, "AvalancheSpeedMultiplier", multiplier); Start(match, 1); Call(Get(match, "rules"), "Tick", 3f);
                    Assert.That((float)Get(front, "speed"), Is.EqualTo(new[] { 12f, 14, 16 }[difficulty] * multiplier).Within(.0001f)); Assert.That((float)Get(front, "acceleration"), Is.EqualTo(originalAcceleration * multiplier).Within(.000001f));
                    float initial = (float)Get(front, "initialZ"), delay = (float)Get(front, "startDelay"); Call(front, "Step", delay, true); Assert.That(Get(front, "CurrentZ"), Is.EqualTo(initial));
                    const float elapsed = 20; Call(front, "Step", delay + elapsed, true); float current = (float)Get(front, "CurrentZ"), travel = current - initial;
                    float expected = (new[] { 12f, 14, 16 }[difficulty] * elapsed + .5f * originalAcceleration * elapsed * elapsed) * multiplier;
                    Assert.That(travel, Is.EqualTo(expected).Within(.001f)); Assert.That(cloud.isPlaying, Is.True); Assert.That(debris[0].isKinematic, Is.False); Assert.That(debris[0].linearVelocity.z, Is.EqualTo((float)Get(front, "speed")).Within(.001f));
                    float nearest = Mathf.Min(500, Mathf.Abs(((Rigidbody)Get(((Array)Get(match, "trucks")).GetValue(0), "body")).position.z - current));
                    Assert.That(rumble.volume, Is.EqualTo((float)Get(Get(match, "settings"), "sfx") * Mathf.Clamp01(1 - nearest / 260) * .6f).Within(.001f));
                    Time.timeScale = 1; yield return new WaitForFixedUpdate(); yield return null; Time.timeScale = 0;
                    Assert.That(frontBody.position.z, Is.EqualTo(current).Within(.05f), "The actual kinematic collision body must reach the sped-up front, not just its decorative particles.");
                    if (multiplier == 1) baselineTravel = frontBody.position.z - initial;
                    else Assert.That((frontBody.position.z - initial) / baselineTravel, Is.EqualTo(1.2f).Within(.001f));
                    Assert.That(Get(match, "ActualBurials"), Is.Zero); Assert.That(((bool[])Get(Get(match, "rules"), "finished"))[0], Is.False);
                    TestContext.WriteLine("AVALANCHE_ACTUAL_FRONT difficulty=" + difficulty + " multiplier=" + multiplier + " bodyZ=" + frontBody.position.z + " travel=" + travel + " speed=" + Get(front, "speed") + " acceleration=" + Get(front, "acceleration"));
                }
            }
            TestContext.WriteLine("Controlled source-owned front ages with native Rigidbody integration; actual travel, matching debris launch speed, attached cloud, distance-based rumble and noncompounding rematches. No full-race achievability, human, listening or performance acceptance claimed.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }
}
#endif
