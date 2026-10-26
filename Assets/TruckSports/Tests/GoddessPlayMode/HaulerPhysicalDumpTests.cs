#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HaulerPhysicalDumpTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v)).Invoke(owner, args);
    static object At(object owner, string field, int index) => ((Array)Get(owner, field)).GetValue(index);
    static string State(object match, object ball) => ((Array)Get(Get(match, "rules"), "state")).GetValue((int)Get(ball, "id")).ToString();
    static IDisposable Preferences(string key)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { key }); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); return prefs;
    }
    static GameObject IsolateInputs()
    {
        var go = new GameObject("Explicit hungry hauler physical dump isolation"); UnityEngine.Object.DontDestroyOnLoad(go); go.AddComponent(Runtime("SportsProbeInputIsolation")); return go;
    }
    static IEnumerator Load()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
    }
    static Component Match() => (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckHungryHaulers.HaulerMatch, Assembly-CSharp", true));
    static void Start(Component match, int count)
    {
        Assert.That(Get(match, "ballSupport"), Is.Not.Null, "Lead must run the explicit saved HaulerPhysicalDumpAuthoring.Install upgrade before these tests.");
        Call(match, "AllBots"); Set(Get(match, "setup"), "count", count); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Call(match, "Simulate", 3f);
    }
    static void NonAi(Component match, int slot)
    {
        var player = At(Get(match, "setup"), "players", slot); Set(player, "source", Enum.Parse(Get(player, "source").GetType(), "Keyboard")); // Disabled session: controlled command fixture, no device/input claim.
    }
    static void Park(object truck, Vector3 position, Quaternion rotation)
    {
        Call(truck, "Prepare", position, rotation, false); ((Rigidbody)Get(truck, "body")).isKinematic = true; Physics.SyncTransforms();
    }
    static void SeedBall(object match, object ball, Vector3 position, Vector3 velocity)
    {
        var component = (Component)ball; component.gameObject.SetActive(true); var body = (Rigidbody)Get(ball, "body"); var shape = (SphereCollider)Get(ball, "shape"); body.isKinematic = false; body.position = position; body.rotation = Quaternion.identity; body.linearVelocity = velocity; body.angularVelocity = Vector3.zero; shape.enabled = true; Call(Get(match, "ballSupport"), "BindBall", ball); Physics.SyncTransforms();
    }
    static IEnumerator Steps(Component match, int count)
    {
        for (int i = 0; i < count; i++) { yield return new WaitForFixedUpdate(); Call(match, "Simulate", Time.fixedDeltaTime); }
    }
    static void CaptureHole(Component match, Vector3 center, string label)
    {
        var source = (Camera)Get(Get(match, "broadcast"), "view"); var go = new GameObject("Controlled authored hauler hole detail camera"); var camera = go.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled = true; camera.orthographic = false; camera.fieldOfView = 55; camera.rect = new Rect(0, 0, 1, 1); camera.transform.position = center + new Vector3(17, 17, -20); camera.transform.LookAt(center + Vector3.down);
        try
        {
            var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>(); var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 6);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) }) save.Invoke(null, new object[] { canvas, new[] { camera }, size.x, size.y, "Logs/TruckSports/PlaytestRepair/HaulerPhysicalDump/" + label + "-" + size.y + ".png", null });
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    static IEnumerator NativePickup(Component match, object ball)
    {
        var scoop = At(match, "scoops", 0); Call(scoop, "Deploy", true, true); var mouth = (BoxCollider)Get(scoop, "mouth"); float radius = ((SphereCollider)Get(ball, "shape")).radius;
        // Explicit initial approach to the original physical intake. No call to
        // Capture/Claim/Collect supplies ownership; the native callback must do so.
        SeedBall(match, ball, new Vector3(mouth.bounds.center.x, mouth.bounds.center.y, mouth.bounds.max.z + radius + .3f), Vector3.back * 5);
        for (int i = 0; i < 150 && State(match, ball) != "Carried"; i++) { yield return new WaitForFixedUpdate(); Call(match, "Simulate", Time.fixedDeltaTime); }
        Assert.That(State(match, ball), Is.EqualTo("Carried")); yield return Steps(match, 30);
    }

    [UnityTest, Timeout(120000)] public IEnumerator AlignedNormalCargoBanksOnlyAfterManualReleaseAndNativeHoleEntry() => DumpFixture(1, 0, false);
    [UnityTest, Timeout(120000)] public IEnumerator AlignedGoldenCargoKeepsFivePointIdentityAndSingleReceipt() => DumpFixture(5, 0, false);
    [UnityTest, Timeout(120000)] public IEnumerator MisalignedReleasedCargoMissesAndStaysPhysicalWithoutPoints() => DumpFixture(1, 9.3f, false);
    [UnityTest, Timeout(180000)] public IEnumerator ControlledAiApproachUsesIdenticalRequestReleaseAndNativeHoleBanking() => DumpFixture(1, 0, true);
    static IEnumerator DumpFixture(int value, float lateral, bool ai)
    {
        var prefs = Preferences("CarnivalHaulerPhysicalDump"); var isolation = IsolateInputs(); Component match = null;
        try
        {
            yield return Load(); match = Match(); Start(match, 1); NonAi(match, 0); Time.timeScale = 1;
            var rules = Get(match, "rules"); var truck = At(match, "trucks", 0); var balls = ((Array)Get(match, "balls")).Cast<object>().ToArray(); var chosen = balls.First(ball => (int)Get(ball, "value") == value); var cargo = ai ? balls.Where(ball => (int)Get(ball, "value") == 1).Take(3).ToArray() : new[] { chosen };
            foreach (var ball in balls) if (!cargo.Contains(ball)) ((Component)ball).gameObject.SetActive(false);
            Park(truck, new Vector3(0, .65f, 0), Quaternion.identity);
            foreach (var ball in cargo) { yield return NativePickup(match, ball); Assert.That(((int[])Get(rules, "points"))[0], Is.Zero); }
            Assert.That((int)Get(match, "ActualCollections"), Is.EqualTo(cargo.Length)); var bank = (Transform)At(match, "banks", 0); var hopper = At(match, "hoppers", 0); var truckBody = (Rigidbody)Get(truck, "body");
            Park(truck, bank.position + bank.forward * 3.7f + Vector3.up * .65f + bank.right * lateral, Quaternion.LookRotation(bank.forward));
            yield return Steps(match, 40); Assert.That(((int[])Get(rules, "points"))[0], Is.Zero); Assert.That((int)Get(match, "ActualReleases"), Is.Zero); Assert.That(Get(hopper, "Open"), Is.False, "Remaining still at the zone cannot automatically unload.");
            int before = (int)Get(Get(match, "ScoreJournal"), "Count");
            if (ai)
            {
                // Controlled final AI approach state, after real native pickups.
                // Ordinary route/whole-match behavior remains in HaulerProbe.
                var player = At(Get(match, "setup"), "players", 0); Set(player, "source", Enum.Parse(Get(player, "source").GetType(), "AI")); Set(player, "difficulty", Enum.Parse(Get(player, "difficulty").GetType(), "Easy")); ((bool[])Get(match, "bankEntering"))[0] = true;
                truckBody.isKinematic = false; truckBody.linearVelocity = Vector3.forward * 5; Assert.That(Call(match, "RequestDump", 0), Is.False, "AI uses the same physical speed gate."); truckBody.linearVelocity = Vector3.zero; truckBody.isKinematic = true; Call(match, "Simulate", .02f);
            }
            else { ((bool[])Get(match, "actions"))[0] = true; Call(match, "Simulate", .02f); ((bool[])Get(match, "actions"))[0] = false; }
            Assert.That(Get(hopper, "Open"), Is.True); Assert.That((int)Get(match, "ActualReleases"), Is.Zero); Assert.That(((int[])Get(rules, "points"))[0], Is.Zero);
            bool released = false; int priorReleases = 0; float highestStep = 0; float end = Time.realtimeSinceStartup + 12;
            while (Time.realtimeSinceStartup < end && (lateral > 0 ? (int)Get(match, "ActualReleases") == 0 || (bool)Get(hopper, "Open") : (int)Get(match, "ActualDeposits") < cargo.Length))
            {
                yield return new WaitForFixedUpdate(); var positions = cargo.ToDictionary(ball => ball, ball => ((Rigidbody)Get(ball, "body")).position); Call(match, "Simulate", Time.fixedDeltaTime); int count = (int)Get(match, "ActualReleases");
                if (count > priorReleases)
                {
                    Assert.That(count - priorReleases, Is.EqualTo(1), "One update cannot release all cargo as a bulk score.");
                    foreach (var ball in cargo) if (State(match, ball) == "Released" && ((Rigidbody)Get(ball, "body")).isKinematic == false) highestStep = Mathf.Max(highestStep, Vector3.Distance(positions[ball], ((Rigidbody)Get(ball, "body")).position));
                    Assert.That(highestStep, Is.LessThan(.001f), "Release may change authority/velocity but may not teleport balls to the bank."); if (!released) CaptureHole(match, bank.position, (ai ? "ai" : value == 5 ? "gold" : lateral > 0 ? "miss" : "normal") + "-actual-open-hopper-release"); released = true; priorReleases = count;
                }
            }
            Assert.That(released, Is.True); Assert.That((int)Get(match, "ActualReleases"), Is.EqualTo(cargo.Length)); Assert.That(((int[])Get(rules, "carried"))[0], Is.Zero);
            if (lateral > 0)
            {
                Assert.That(State(match, chosen), Is.EqualTo("Released")); Assert.That(((int[])Get(rules, "points"))[0], Is.Zero); Assert.That((int)Get(match, "ActualDeposits"), Is.Zero); Assert.That((int)Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(before)); Assert.That(((Rigidbody)Get(chosen, "body")).isKinematic, Is.False); Assert.That(((SphereCollider)Get(chosen, "shape")).enabled, Is.True); Assert.That(((Rigidbody)Get(chosen, "body")).position.y, Is.GreaterThan(.5f), "A miss stays on the authored ball floor rather than passing through the entire arena.");
            }
            else
            {
                Assert.That((int)Get(match, "ActualDeposits"), Is.EqualTo(cargo.Length)); Assert.That(((int[])Get(rules, "points"))[0], Is.EqualTo(cargo.Sum(ball => (int)Get(ball, "value")))); Assert.That((int)Get(Get(match, "ScoreJournal"), "Count") - before, Is.EqualTo(cargo.Length));
                foreach (var ball in cargo) { Assert.That(State(match, ball), Is.EqualTo("Deposited")); Assert.That(((Rigidbody)Get(ball, "body")).position.y, Is.LessThan(-.6f)); Assert.That(((Component)ball).gameObject.activeSelf, Is.False); }
                var journal = Get(match, "ScoreJournal"); int receipts = (int)Get(journal, "Count"); var latest = Get(journal, "Latest"); Assert.That(Get(latest, "Delta"), Is.EqualTo((double)(int)Get(cargo.Last(), "value"))); Assert.That(Get(latest, "ParticipantId"), Is.Not.Null.And.Not.Empty); yield return Steps(match, 50); Assert.That((int)Get(journal, "Count"), Is.EqualTo(receipts)); Assert.That((int)Get(match, "ActualDeposits"), Is.EqualTo(cargo.Length));
            }
            string id = (string)Get(match, "MatchId"); Call(match, "SetupScreen"); Call(match, "AllBots"); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
            Assert.That(Get(match, "MatchId"), Is.Not.EqualTo(id)); Assert.That(Get(match, "ActualReleases"), Is.Zero); Assert.That(Get(match, "ActualDeposits"), Is.Zero); Assert.That((int[])Get(rules, "points"), Is.All.Zero); Assert.That((int[])Get(rules, "carried"), Is.All.Zero); Assert.That(((Array)Get(rules, "state")).Cast<object>().All(state => state.ToString() == "Loose"), Is.True); Assert.That(balls.All(ball => ((Component)ball).gameObject.activeSelf && ((SphereCollider)Get(ball, "shape")).enabled), Is.True); Assert.That(Get(hopper, "Open"), Is.False);
            TestContext.WriteLine("CONTROLLED_HAULER_PHYSICAL_DUMP value=" + value + " lateral=" + lateral + " aiFinalApproach=" + ai + "; actual original mouth/lip pickups, supplied truck approach pose, contextual ACTION/AI same RequestDump, visible authored hopper/gate, staggered no-pose-write release, native hole entry or physical miss, original values/stable receipt ownership/idempotence, full population rematch. AI approach flag is a fixture; no whole route/full-match/human/visual/listening acceptance.");
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator EverySavedLayoutDropsBallsThroughHoleWhileOriginalNativeNwhTrucksRemainSupported()
    {
        var prefs = Preferences("CarnivalHaulerBallOnlyFloor"); var isolation = IsolateInputs(); Component match = null;
        try
        {
            yield return Load(); match = Match(); var support = Get(match, "ballSupport"); Assert.That(support, Is.Not.Null); var floors = (Collider[])Get(support, "originalFloors"); var floorJson = floors.Select(collider=>EditorJsonUtility.ToJson(collider)).ToArray(); var meshes = floors.OfType<MeshCollider>().Select(collider => collider.sharedMesh).ToArray();
            for (int players = 1; players <= 4; players++)
            {
                if (players > 1) Call(match, "SetupScreen"); Start(match, players); Time.timeScale = 1; var ball = ((Array)Get(match, "balls")).Cast<object>().First(candidate => (int)Get(candidate, "value") == 1);
                foreach (var other in ((Array)Get(match, "balls")).Cast<object>()) if (!ReferenceEquals(other, ball)) ((Component)other).gameObject.SetActive(false);
                for(int slot=0;slot<players;slot++)
                {
                var truck = At(match,"trucks",slot);var bank=(Transform)At(match,"banks",slot);var truckBody=(Rigidbody)Get(truck,"body");
                // Compare against this exact authored NWH assembly on the
                // unchanged flat court. Its body pivot can settle near y=0;
                // an arbitrary positive pivot height is not wheel support.
                Call(truck, "Prepare", bank.position-bank.forward*16+Vector3.up*.65f,Quaternion.LookRotation(bank.forward),true);
                float controlMinimum=truckBody.position.y,controlUntil=Time.time+2.5f;
                while(Time.time<controlUntil){yield return new WaitForFixedUpdate();controlMinimum=Mathf.Min(controlMinimum,truckBody.position.y);}
                Assert.That(Get(Get(truck,"tractorMotor"),"Grounded"),Is.True,"Original flat court must support the comparison assembly.");
                Call(truck, "Prepare", bank.position + Vector3.up * .65f, Quaternion.LookRotation(bank.forward), true); SeedBall(match, ball, bank.position + Vector3.up * 4 + bank.right * 4.3f, Vector3.zero);
                float minTruckY = truckBody.position.y, minBallY = 10, stop = Time.time + 2.5f;
                while (Time.time < stop) { yield return new WaitForFixedUpdate(); minTruckY = Mathf.Min(minTruckY, truckBody.position.y); minBallY = Mathf.Min(minBallY, ((Rigidbody)Get(ball, "body")).position.y); }
                TestContext.WriteLine("HAULER_SUPPORT_AB players="+players+" slot="+slot+" originalFlatCourtMin="+controlMinimum+" visibleHoleMin="+minTruckY+" holeBallMin="+minBallY);
                Assert.That(minTruckY, Is.GreaterThanOrEqualTo(controlMinimum-.05f),"The visible ball hole must not reduce real NWH support versus the unchanged original court."); Assert.That(Get(Get(truck, "tractorMotor"), "Grounded"), Is.True, "Original enabled NWH wheels must actually support the truck over a visible ball hole."); Assert.That(minBallY, Is.LessThan(-1), "The native ball must pass through the recessed opening despite the unchanged truck collider."); Assert.That(State(match, ball), Is.EqualTo("Loose")); Assert.That((int)Get(match, "ActualDeposits"), Is.Zero, "An unowned loose ball falling into a hole cannot bank.");
                Call(support, "BindGeneratedTruckShapes"); var layout = ((GameObject[])Get(support, "layouts"))[players - 1]; var contacts = layout.GetComponentsInChildren<Collider>().Concat(((Collider[])Get(support, "pitColliders")).Where(collider => collider.gameObject.activeInHierarchy));
                foreach (var shape in ((Component)truck).GetComponentsInChildren<Collider>().Where(shape=>shape.enabled)) foreach (var contact in contacts) Assert.That(Physics.GetIgnoreCollision(shape, contact), Is.True);
                foreach (var wheel in (Array)Get(truck, "wheels")) Assert.That(((LayerMask)Get(wheel, "layerMask")).value & (1 << 2), Is.Zero);
                Assert.That(Physics.GetIgnoreCollision((Collider)Get(ball, "shape"), floors[0]), Is.True); Assert.That(floors.Select(collider=>EditorJsonUtility.ToJson(collider)), Is.EqualTo(floorJson)); Assert.That(floors.OfType<MeshCollider>().Select(collider => collider.sharedMesh), Is.EqualTo(meshes));
                TestContext.WriteLine("CONTROLLED_HAULER_BALL_ONLY_LAYOUT players=" + players + " slot="+slot+" minTruckY=" + minTruckY + " minBallY=" + minBallY + "; real original NWH support, unchanged original collider serialization/mesh, ball floor/pit ignored by all generated truck colliders and wheel ground mask; loose ball entry awards nothing.");
                }
            }
        }
        finally
        {
            Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        }
    }
}
#endif
