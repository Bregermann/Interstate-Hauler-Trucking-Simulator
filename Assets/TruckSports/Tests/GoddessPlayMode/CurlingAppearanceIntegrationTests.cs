#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CurlingAppearanceIntegrationTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static Type Mode(string name) => Type.GetType("LWS.TruckCurling." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    static object[] Stones(object match) => ((Array)Get(match, "stones")).Cast<object>().ToArray();
    static Component Truck(object stone) => (Component)Get(stone, "truck");
    static object Host(object stone) => Get(Truck(stone), "appearanceHost");
    static Rigidbody Body(object stone) => (Rigidbody)Get(Truck(stone), "body");
    static void Save(string id, string body)
    {
        var draft = Registry("GetAppearance", id); Set(draft, "bodyId", body); var list = (IList)Get(draft, "selections"); list.Clear();
        foreach (var pair in new[] { new[] { "hat", "hat.crown" }, new[] { "wheels", "wheels.gold" }, new[] { "dashboard", "dashboard.cat" } }) { var item = Activator.CreateInstance(Sports("SportsTruckCosmeticSelection")); Set(item, "slotId", pair[0]); Set(item, "itemId", pair[1]); list.Add(item); }
        bool changed = (bool)Registry("TrySetAppearance", id, draft); var normalizeArgs = new object[] { draft, false }; var normalized = Sports("SportsTruckAppearanceData").GetMethod("Normalize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, normalizeArgs); Assert.That((bool)Call(Registry("GetAppearance", id), "SameAs", normalized), Is.True, "Saved semantic appearance must exactly match the registry-normalized draft; changed=" + changed);
    }
    sealed class Fixture : IDisposable
    {
        readonly IDisposable prefs; readonly GameObject isolation; readonly UnityEngine.Random.State random;
        public Behaviour match; public string[] ids = new string[4];
        public Fixture() { random = UnityEngine.Random.state; prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CurlingAppearance" + Guid.NewGuid().ToString("N") }); Registry("ForgetCache"); isolation = new GameObject("Curling appearance original AI input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); }
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCurling/Scenes/TruckCurling.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Behaviour)UnityEngine.Object.FindAnyObjectByType(Mode("CurlingMatch")); Assert.That(Stones(match).All(stone => Host(stone) != null), Is.True, "Explicit Curling appearance author/save required.");
            Set(match, "teamA", 2); Set(match, "teamB", 2); Set(match, "option", 0); Call(match, "NormalizeSetup"); Call(match, "AllBots");
            for (int i = 0; i < 4; i++) { ids[i] = (string)Get(Registry("Create", "Same Curling Driver"), "id"); Assert.That((bool)Registry("UseInSeat", i, ids[i]), Is.True); Save(ids[i], i == 0 ? "truck.monster" : "novelty.icecream"); }
            Call(match, "RefreshParticipants", false); Call(match, "AllBots"); foreach (var player in ((Array)Get(Get(match, "setup"), "players")).Cast<object>()) Set(player, "difficulty", Enum.Parse(Get(player, "difficulty").GetType(), "Hard"));
        }
        public void Start() { Call(match, "SetupScreen"); UnityEngine.Random.InitState(2207); Assert.That((bool)Call(match, "StartMatch"), Is.True); }
        public void Dispose() { Time.timeScale = 1; if (match != null) { match.enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); UnityEngine.Random.state = random; Registry("ForgetCache"); prefs.Dispose(); }
    }
    static string Physics(object stone)
    {
        var truck = Truck(stone); var body = Body(stone); var shape = truck.GetComponent<MeshCollider>(); string value = body.GetInstanceID() + ":" + body.mass + ":" + body.centerOfMass + ":" + body.linearDamping + ":" + body.angularDamping + ":" + body.constraints + ":" + truck.transform.localScale + ":" + truck.gameObject.layer + ":" + shape.GetInstanceID() + ":" + shape.sharedMesh.GetInstanceID() + ":" + shape.convex;
        foreach (var wheel in (IEnumerable)Get(truck, "wheels")) { var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring"); value += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "mass") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce") + ":" + ((Transform)Get(data, "rotatingContainer")).GetInstanceID(); }
        return value;
    }
    static void Applied(object stone, string id, string body)
    {
        var host = Host(stone); Assert.That(Get(host, "ProfileId"), Is.EqualTo(id)); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo(body)); var root = (Transform)Get(host, "AppearanceRoot"); Assert.That(root, Is.Not.Null);
        Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(((Renderer[])Get(host, "originalBodyRenderers")).All(renderer => !renderer.enabled), Is.True); Assert.That(((Renderer[])Get(host, "originalWheelRenderers")).Length, Is.EqualTo(4));
        Assert.That(((Renderer)Get(stone, "footprint")).enabled, Is.True);
    }
    [UnityTest, Timeout(180000)]
    public IEnumerator SevenBodiesOnSixteenRetainedStonesKeepOriginalSixtyFourNwhControllersAndCircularPhysics()
    {
        using (var f = new Fixture())
        {
            yield return f.Load(); var stones = Stones(f.match); var physics = stones.Select(Physics).ToArray();
            foreach (string body in new[] { "truck.monster", "truck.lifted", "semi.lowrider", "truck.fire", "novelty.hotdog", "novelty.icecream", "semi.standard" })
            {
                Save(f.ids[0], body); Call(f.match, "RefreshParticipants", false); f.Start(); f.match.enabled = false; Time.timeScale = 0;
                // Explicit retained-owner presentation fixture, not natural
                // delivery/score evidence. Original Prepare/Restore paths only.
                for (int i = 0; i < 16; i++) { int owner = i % 4; Call(stones[i], "Prepare", i, owner / 2, owner); Applied(stones[i], f.ids[owner], owner == 0 ? body : "novelty.icecream"); }
                CollectionAssert.AreEqual(physics, stones.Select(Physics).ToArray());
                for (int i = 0; i < 16; i++) { string id = (string)Get(Host(stones[i]), "ProfileId"); var p = Body(stones[i]).position; var q = Body(stones[i]).rotation; Call(stones[i], "Restore", p, q, true, true); Assert.That(Get(Host(stones[i]), "ProfileId"), Is.EqualTo(id), "Original guard rollback restoration retains appearance ownership."); }
                // Restore actual current delivery/cab binding after the isolated
                // sixteen-owner fixture; retain original current-model layer22.
                int current = (int)Get(f.match, "ActiveSlot"); Call(stones[0], "Prepare", 0, (int)Get(f.match, "ActiveTeam"), current); yield return null;
                var cabBinding = (Component)Get(f.match, "appearanceCab"); Assert.That(Get(cabBinding, "owner"), Is.SameAs(Host(stones[0])));
                var dashboard = cabBinding.GetComponentsInChildren<Transform>(true).Single(part => part.name == "Profile dashboard — dashboard.cat" && part.gameObject.activeSelf);
                Assert.That(dashboard.GetComponentsInChildren<Renderer>(true).All(renderer => renderer.gameObject.layer == 26), Is.True);
                Assert.That(((Camera)Get(f.match, "houseCamera")).cullingMask & (1 << 26), Is.Zero);
                var cabCamera = (Camera)Get(Get(f.match, "cabCamera"), "view"); Assert.That(cabCamera.cullingMask & (1 << 22), Is.Zero);
                Assert.That(((Transform)Get(Host(stones[0]), "AppearanceRoot")).GetComponentsInChildren<Renderer>(true).All(renderer => renderer.gameObject.layer == 22), Is.True);
                Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero); f.match.enabled = true;
            }
            // Separate controlled real PhysX contact with selected bodies. No
            // synthetic OnCollisionEnter/contact/scoring method is invoked.
            f.Start(); f.match.enabled = false; for (int i = 2; i < 16; i++) Call(stones[i], "Remove");
            Call(stones[1], "Prepare", 1, 1, 2); Call(Truck(stones[0]), "Power", false); Call(Truck(stones[1]), "Power", false);
            Body(stones[0]).position = new Vector3(0,.38f,40); Body(stones[1]).position = new Vector3(0,.38f,43); Body(stones[0]).linearVelocity = Vector3.forward * 4; PhysicsSync(); Time.timeScale = 1;
            int contacts = (int)Get(f.match, "PhysicalContacts"); float until = Time.realtimeSinceStartup + 4; while ((int)Get(f.match, "PhysicalContacts") == contacts && Time.realtimeSinceStartup < until) yield return new WaitForFixedUpdate();
            Assert.That((int)Get(f.match, "PhysicalContacts"), Is.GreaterThan(contacts)); Assert.That((bool)Get(stones[0], "HitStone"), Is.True); Assert.That((bool)Get(stones[1], "HitStone"), Is.True);
            TestContext.WriteLine("CURLING_APPEARANCE controlled16 owner/restore + separately controlled genuine circular-PhysX contact; all7 bodies, original64 controllers/containers and physical settings retained. No scoring outcome injected.");
        }
    }
    static void PhysicsSync() => UnityEngine.Physics.SyncTransforms();
    [UnityTest, Timeout(240000)]
    public IEnumerator OriginalFourAiDeliveryOwnersKeepPriorBodiesAndActualCabOwnerAcrossTurns()
    {
        using (var f = new Fixture())
        {
            yield return f.Load(); f.Start(); var owners = new Dictionary<int, string>(); var stones = Stones(f.match); int retained = 0; float deadline = Time.realtimeSinceStartup + 210;
            while (((int)Get(f.match, "Delivery") < 4 || owners.Count < 4) && Time.realtimeSinceStartup < deadline)
            {
                int delivery = (int)Get(f.match, "Delivery");
                // Resolve advances Delivery immediately, then intentionally
                // leaves1.7s DeliveryResult before BeginDelivery prepares it.
                // Assert every genuine Approach, never infer preparation from
                // the already-advanced next index during the result interval.
                if (Get(f.match, "Phase").ToString() == "Approach" && delivery < 4)
                {
                    int slot = (int)Get(f.match, "ActiveSlot"); var current = stones[delivery]; owners[delivery] = f.ids[slot];
                    Assert.That((bool)Get(current, "InPlay"), Is.True); Assert.That((int)Get(current, "delivery"), Is.EqualTo(delivery)); Assert.That((int)Get(Truck(current), "slot"), Is.EqualTo(slot));
                    Assert.That(Get(Host(current), "ProfileId"), Is.EqualTo(f.ids[slot])); Assert.That(Get(Get(f.match, "appearanceCab"), "assignedOwner"), Is.SameAs(Host(current)));
                }
                foreach (var prior in owners.Where(pair => pair.Key < delivery)) if ((bool)Get(stones[prior.Key], "InPlay")) { retained++; Assert.That(Get(Host(stones[prior.Key]), "ProfileId"), Is.EqualTo(prior.Value)); }
                yield return new WaitForFixedUpdate();
            }
            Assert.That((int)Get(f.match, "Delivery"), Is.GreaterThanOrEqualTo(4)); Assert.That(owners.Count, Is.EqualTo(4)); Assert.That(owners.Values.Distinct().Count(), Is.EqualTo(4), "Original roster rotation must reach all four independently named owners."); Assert.That(retained, Is.GreaterThan(0));
            f.Start(); Assert.That((int)Get(f.match, "Delivery"), Is.Zero); Assert.That(Get(Host(Stones(f.match)[0]), "ProfileId"), Is.EqualTo(f.ids[(int)Get(f.match, "ActiveSlot")]));
            TestContext.WriteLine("CURLING_APPEARANCE_NATURAL original four AI delivery transitions, retained prior identities and cab assignments; no pose/velocity/rule/score injection in this case.");
        }
    }
}
#endif
