#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CornholeAppearanceIntegrationTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static Type Mode(string name) => Type.GetType("LWS.TruckCornhole." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    static object[] Courses(object match) => ((Array)Get(match, "courses")).Cast<object>().ToArray();
    static Component[] Trucks(object match) => Courses(match).SelectMany(course => ((Array)Get(course, "trucks")).Cast<Component>()).ToArray();
    static object Host(Component truck) => Get(truck, "appearanceHost");
    static void Save(string id, string body)
    {
        var draft = Registry("GetAppearance", id); Set(draft, "bodyId", body); var list = (IList)Get(draft, "selections"); list.Clear();
        foreach (var pair in new[] { new[] { "hat", "hat.crown" }, new[] { "wheels", "wheels.gold" }, new[] { "dashboard", "dashboard.cat" } })
        { var item = Activator.CreateInstance(Sports("SportsTruckCosmeticSelection")); Set(item, "slotId", pair[0]); Set(item, "itemId", pair[1]); list.Add(item); }
        bool changed = (bool)Registry("TrySetAppearance", id, draft); var normalizeArgs = new object[] { draft, false }; var normalized = Sports("SportsTruckAppearanceData").GetMethod("Normalize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, normalizeArgs); Assert.That((bool)Call(Registry("GetAppearance", id), "SameAs", normalized), Is.True, "Saved semantic appearance must exactly match the registry-normalized draft; changed=" + changed);
    }
    sealed class Fixture : IDisposable
    {
        readonly IDisposable prefs; readonly GameObject isolation; readonly UnityEngine.Random.State random;
        public Component match; public string[] ids = new string[4];
        public Fixture() { random = UnityEngine.Random.state; prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CornAppearance" + Guid.NewGuid().ToString("N") }); Registry("ForgetCache"); isolation = new GameObject("Cornhole appearance original AI input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); }
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Mode("CornholeMatch"));
            Assert.That(Trucks(match).All(truck => Host(truck) != null), Is.True, "Run explicit Cornhole appearance author and save original scene first.");
            for (int i = 0; i < 4; i++) { ids[i] = (string)Get(Registry("Create", "Same Mini Driver"), "id"); Assert.That((bool)Registry("UseInSeat", i, ids[i]), Is.True); Save(ids[i], i == 0 ? "truck.monster" : "novelty.icecream"); }
            Assert.That(ids.Distinct().Count(), Is.EqualTo(4)); Call(match, "RefreshParticipants", false);
        }
        public void Start(int count = 4)
        {
            Call(match, "ChangeSetup"); Set(Get(match, "setup"), "count", count); Call(match, "AllAI");
            foreach (var player in ((Array)Get(Get(match, "setup"), "players")).Cast<object>()) Set(player, "difficulty", Enum.Parse(Get(player, "difficulty").GetType(), "Hard"));
            var presets = Get(match, "weatherPresets"); var clear = ((Array)Get(presets, "entries")).Cast<object>().First(entry => (string)Get(entry, "displayName") == "Clear"); Call(presets, "Select", Get(clear, "id"));
            Sports("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { true }); UnityEngine.Random.InitState(174207);
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
        }
        public void Dispose() { Time.timeScale = 1; if (match != null) Call(match, "ChangeSetup"); UnityEngine.Object.DestroyImmediate(isolation); UnityEngine.Random.state = random; Registry("ForgetCache"); prefs.Dispose(); }
    }
    static string Physics(Component truck)
    {
        var body = (Rigidbody)Get(truck, "body"); var shape = (BoxCollider)Get(truck, "shape"); string text = body.GetInstanceID() + ":" + body.mass + ":" + body.centerOfMass + ":" + body.linearDamping + ":" + body.angularDamping + ":" + shape.GetInstanceID() + ":" + shape.center + ":" + shape.size + ":" + shape.gameObject.layer + ":" + truck.transform.localScale;
        foreach (var wheel in (IEnumerable)Get(truck, "wheels")) { var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring"); text += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "mass") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce") + ":" + ((Transform)Get(data, "rotatingContainer")).GetInstanceID(); }
        return text;
    }
    static void CheckSnapshots(object replay, int owner)
    {
        var roots = (Transform[])Get(replay, "ghosts"); var original = (Renderer[][])Get(replay, "snapshotSources"); var copy = (Renderer[][])Get(replay, "renderers");
        Assert.That(roots.Count(root => root != null), Is.EqualTo(16));
        for (int i = 0; i < 16; i++)
        {
            Assert.That(roots[i].GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(roots[i].GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(copy[i].Length, Is.EqualTo(original[i].Length)); Assert.That(copy[i].Length, Is.GreaterThan(4));
            for (int part = 0; part < copy[i].Length; part++)
            {
                Assert.That(copy[i][part].enabled, Is.True, "Only actual selected visible body surfaces belong to a snapshot."); Assert.That(copy[i][part].gameObject.layer, Is.EqualTo(24 + owner)); CollectionAssert.AreEqual(copy[i][part].sharedMaterials, original[i][part].sharedMaterials);
                var a = new MaterialPropertyBlock(); var b = new MaterialPropertyBlock(); original[i][part].GetPropertyBlock(a); copy[i][part].GetPropertyBlock(b); Assert.That(b.GetColor("_BaseColor"), Is.EqualTo(a.GetColor("_BaseColor")));
                Assert.That(copy[i][part].GetComponent<MeshFilter>().sharedMesh, Is.SameAs(original[i][part].GetComponent<MeshFilter>().sharedMesh));
            }
        }
        Assert.That((int)Mode("CornholeReplay").GetField("MaximumSamplePayloadBytes").GetRawConstantValue(), Is.EqualTo(722400));
        Assert.That(((Array)Get(replay, "samples")).GetLength(0), Is.EqualTo(1400)); Assert.That(((Array)Get(replay, "samples")).GetLength(1), Is.EqualTo(16));
    }
    [UnityTest, Timeout(180000)]
    public IEnumerator SevenBodiesKeepSixteenPhysicalOwnersPrivateCabsAndExistingPoseBudgetAcrossRematches()
    {
        using (var f = new Fixture())
        {
            yield return f.Load(); var trucks = Trucks(f.match); var courses = Courses(f.match); var physics = trucks.Select(Physics).ToArray();
            // Warm the original exterior cache before the first composition.
            foreach (var truck in trucks) Assert.That(((Renderer[])Get(truck, "Visuals")).Length, Is.GreaterThan(0));
            int[][] roots = null;
            foreach (string body in new[] { "truck.monster", "truck.lifted", "semi.lowrider", "truck.fire", "novelty.hotdog", "novelty.icecream", "semi.standard" })
            {
                Save(f.ids[0], body); Call(f.match, "RefreshParticipants", false); f.Start(); Time.timeScale = 0; yield return null;
                CollectionAssert.AreEqual(physics, trucks.Select(Physics).ToArray());
                for (int i = 0; i < 16; i++)
                {
                    var host = Host(trucks[i]); Assert.That(Get(host, "ProfileId"), Is.EqualTo(f.ids[i / 4])); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo(i < 4 ? body : "novelty.icecream")); Assert.That(Get(host, "participantSlot"), Is.EqualTo(i / 4));
                    var model = (Transform)Get(host, "AppearanceRoot"); Assert.That(model, Is.Not.Null); Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                    Assert.That(((Renderer[])Get(host, "originalBodyRenderers")).All(renderer => !renderer.enabled), Is.True); Assert.That(((Renderer[])Get(host, "originalWheelRenderers")).Length, Is.EqualTo(8));
                    var cached = (Renderer[])Get(trucks[i], "Visuals"); Assert.That(model.GetComponentsInChildren<Renderer>(true).All(renderer => cached.Contains(renderer)), Is.True);
                    Assert.That(model.GetComponentsInChildren<Renderer>(true).All(renderer => renderer.gameObject.layer == 29), Is.True);
                }
                for (int owner = 0; owner < 4; owner++)
                {
                    var chase = Get(courses[owner], "chase"); var binding = (Component)Get(chase, "appearanceCab"); Assert.That(Get(binding, "owner"), Is.SameAs(Host(trucks[owner * 4])));
                    var dashboard = binding.GetComponentsInChildren<Transform>(true).Single(part => part.name == "Profile dashboard — dashboard.cat" && part.gameObject.activeSelf);
                    Assert.That(dashboard.GetComponentsInChildren<Renderer>(true).All(renderer => renderer.gameObject.layer == 24 + owner), Is.True);
                    var replay = Get(courses[owner], "replay"); Call(replay, "Ensure"); CheckSnapshots(replay, owner);
                    var camera = (Camera)Get(chase, "view"); Call(chase, "Begin", default(ScriptableRenderContext), camera);
                    Assert.That(((Renderer[])Get(trucks[owner * 4], "Visuals")).All(renderer => renderer.forceRenderingOff), Is.True);
                    Assert.That(((Renderer[])Get(trucks[((owner + 1) % 4) * 4], "Visuals")).All(renderer => !renderer.forceRenderingOff), Is.True);
                    Call(chase, "End", default(ScriptableRenderContext), camera); Assert.That(trucks.SelectMany(truck => (Renderer[])Get(truck, "Visuals")).All(renderer => !renderer.forceRenderingOff), Is.True);
                }
                var currentRoots = courses.Select(course => ((Transform[])Get(Get(course, "replay"), "ghosts")).Select(root => root.GetInstanceID()).ToArray()).ToArray();
                if (roots == null) roots = currentRoots; else for (int owner = 0; owner < 4; owner++) CollectionAssert.AreEqual(roots[owner], currentRoots[owner]);
                Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);
            }
            Registry("ForgetCache"); Assert.That(Get(Registry("GetAppearance", f.ids[0]), "bodyId"), Is.EqualTo("semi.standard"));
        }
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator NaturalFourAiDeliveriesRetainSelectedBodiesMakeRealPileupsAndReplayAllOwners()
    {
        using (var f = new Fixture())
        {
            yield return f.Load(); f.Start(); var courses = Courses(f.match); var trucks = Trucks(f.match); var replay = Get(courses[0], "replay"); bool reviewed = false;
            float deadline = Time.realtimeSinceStartup + 180;
            while (!(bool)Get(f.match, "RoundBreak") && Time.realtimeSinceStartup < deadline)
            {
                if (!reviewed && (bool)Get(replay, "Available") && (bool)Call(replay, "Play"))
                {
                    reviewed = true; Time.timeScale = 0; CheckSnapshots(replay, 0); var positions = trucks.Select(truck => ((Rigidbody)Get(truck, "body")).position).ToArray();
                    for (int angle = 0; angle < 4; angle++) { if (angle > 0) Call(replay, "NextAngle"); Call(replay, "Rewind"); var chase = Get(courses[0], "chase"); var camera = (Camera)Get(chase, "view"); Call(replay, "PositionCamera", chase); Call(chase, "Begin", default(ScriptableRenderContext), camera); Assert.That(trucks.SelectMany(truck => (Renderer[])Get(truck, "Visuals")).All(renderer => renderer.forceRenderingOff), Is.True); Call(chase, "End", default(ScriptableRenderContext), camera); CollectionAssert.AreEqual(positions, trucks.Select(truck => ((Rigidbody)Get(truck, "body")).position).ToArray()); }
                    Call(replay, "Stop"); Time.timeScale = 1;
                }
                yield return new WaitForFixedUpdate();
            }
            Assert.That(reviewed, Is.True); Assert.That((bool)Get(f.match, "RoundBreak"), Is.True); Assert.That(trucks.Sum(truck => (int)Get(truck, "CrossParticipantImpacts")), Is.GreaterThan(0));
            Assert.That(courses.All(course => (int)Get(course, "Attempts") == 4), Is.True); Assert.That(((int[])Get(f.match, "totals")).Sum(), Is.GreaterThan(0), "Same established Hard/Clear/174207 natural seed must actually bank points.");
            Assert.That(trucks.All(truck => (string)Get(Host(truck), "ProfileId") == f.ids[(int)Get(Get(truck, "course"), "index")]), Is.True);
            TestContext.WriteLine("CORN_APPEARANCE_NATURAL seed174207 Hard Clear; original four AI approaches/real contacts,16 retained owner bindings, original score bank and private replay. No pose/force/score/outcome injection.");
            f.Start(1); Assert.That(trucks.Skip(4).All(truck => !(bool)Get(Host(truck), "Applied")), Is.True); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);
        }
    }
}
#endif
