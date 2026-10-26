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

public sealed class TargetAppearanceIntegrationTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static Type Mode(string name) => Type.GetType("LWS.TruckTargetLaunch." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    sealed class Fixture : IDisposable
    {
        readonly IDisposable prefs; readonly GameObject isolation;
        public Behaviour match; public string[] ids = new string[4];
        public object[] Racers => ((Array)Get(match, "racers")).Cast<object>().ToArray();
        public Component Truck(int slot) => (Component)Get(Racers[slot], "truck");
        public Rigidbody Body(int slot) => (Rigidbody)Get(Truck(slot), "body");
        public object Host(int slot) => Get(Truck(slot), "appearanceHost");
        public object Volcano => Get(match, "volcano"); public object Pool => Get(Volcano, "wrecks"); public object Bridge => Get(Volcano, "appearanceWrecks"); public object Replay => Get(match, "replay");
        public Fixture() { prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "TargetAppearance" + Guid.NewGuid().ToString("N") }); Registry("ForgetCache"); isolation = new GameObject("Target selected-body original input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); }
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Behaviour)UnityEngine.Object.FindAnyObjectByType(Mode("TargetMatch")); Assert.That(Bridge, Is.Not.Null, "Validate private recipes, run explicit Target appearance author and save first.");
            for (int i = 0; i < 4; i++) { ids[i] = (string)Get(Registry("Create", "Same Target Driver"), "id"); Assert.That((bool)Registry("UseInSeat", i, ids[i]), Is.True); Save(i, i == 0 ? "truck.monster" : "novelty.icecream"); }
            Call(match, "RefreshParticipants", false); var presets = Get(Get(match, "atmosphere"), "presets"); if (presets != null) { var clear = ((Array)Get(presets, "entries")).Cast<object>().First(entry => (string)Get(entry, "displayName") == "Clear"); Call(presets, "Select", Get(clear, "id")); }
        }
        public void Save(int slot, string body)
        {
            var draft = Registry("GetAppearance", ids[slot]); Set(draft, "bodyId", body); var list = (IList)Get(draft, "selections"); list.Clear();
            foreach (var pair in new[] { new[] { "hat", "hat.crown" }, new[] { "mirrors", "mirrors.chrome" }, new[] { "wheels", "wheels.gold" }, new[] { "rear.ornament", "rear.chrome" } }) { var item = Activator.CreateInstance(Sports("SportsTruckCosmeticSelection")); Set(item, "slotId", pair[0]); Set(item, "itemId", pair[1]); list.Add(item); }
            Registry("TrySetAppearance", ids[slot], draft); object[] args = { draft, false }; var normalized = Sports("SportsTruckAppearanceData").GetMethod("Normalize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args); Assert.That((bool)Call(Registry("GetAppearance", ids[slot]), "SameAs", normalized), Is.True);
        }
        public void Start(int count = 4) { match.enabled = true; Call(match, "SetupScreen"); Set(Get(match, "setup"), "count", count); Call(match, "NormalizeSetup"); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True); }
        public void Dispose() { Time.timeScale = 1; if (match != null) { match.enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); Registry("ForgetCache"); prefs.Dispose(); }
    }
    static string PiecePhysics(object pool)
    {
        return string.Join("/", ((Array)Get(pool, "pieces")).Cast<object>().Select(piece => { var body = (Rigidbody)Get(piece, "body"); var shape = (BoxCollider)Get(piece, "shape"); return body.GetInstanceID() + ":" + body.mass + ":" + body.centerOfMass + ":" + body.maxLinearVelocity + ":" + body.maxAngularVelocity + ":" + body.linearDamping + ":" + body.angularDamping + ":" + shape.GetInstanceID() + ":" + shape.center + ":" + shape.size; }));
    }
    static void CheckGraphics(object bridge, int slot, bool emitted)
    {
        var owner = ((Array)Get(bridge, "owners")).GetValue(slot); var graphics = ((IEnumerable)Get(owner, "graphics")).Cast<object>().ToArray(); Assert.That(graphics.Length, Is.GreaterThan(12));
        foreach (var graphic in graphics)
        {
            var mesh = (Mesh)Get(graphic, "mesh"); var input = (Mesh)Get(graphic, "input"); var target = (Renderer)Get(graphic, "target"); var source = (Renderer)Get(graphic, "source");
            Assert.That(mesh, Is.Not.SameAs(input)); Assert.That(mesh.vertexCount, Is.EqualTo(input.vertexCount)); CollectionAssert.AreEqual(mesh.uv, input.uv); Assert.That(mesh.vertices.All(vertex => float.IsFinite(vertex.x) && float.IsFinite(vertex.y) && float.IsFinite(vertex.z)), Is.True);
            Assert.That(target.GetComponents<Collider>(), Is.Empty); Assert.That(target.GetComponents<Rigidbody>(), Is.Empty);
            int material = (int)Get(graphic, "material"); Assert.That(target.sharedMaterials[0], Is.SameAs(source.sharedMaterials[material < 0 ? 0 : material])); if (material >= 0 && mesh.subMeshCount > 1) Assert.That(target.sharedMaterials[1], Is.SameAs(Get(graphic, "cap")));
            if (emitted) for (int i = 0; i < input.vertexCount; i += Mathf.Max(1, input.vertexCount / 32))
                Assert.That(Vector3.Distance(target.transform.TransformPoint(mesh.vertices[i]), source.transform.TransformPoint(input.vertices[i])), Is.LessThan(.001f), "Actual selected-body/cosmetic surface must coincide at the unchanged physical Emit pose (render-only1mm bound).");
        }
    }
    [UnityTest, Timeout(180000)]
    public IEnumerator SevenSelectedBodiesUseSameFortyEightPiecesOriginalEmitAndPreservedNineReplayRoots()
    {
        using (var f = new Fixture())
        {
            yield return f.Load(); var pieces = ((Array)Get(f.Pool, "pieces")).Cast<object>().ToArray(); Assert.That(pieces.Length, Is.EqualTo(48)); string physics = PiecePhysics(f.Pool);
            var ghosts = ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray(); var pieceGhosts = pieces.Select(piece => ((Transform)Get(piece, "ghost")).GetInstanceID()).ToArray(); var pieceRenderers = pieces.Select(piece => ((Renderer)Get(piece, "ghostRenderer")).GetInstanceID()).ToArray(); var owned = new System.Collections.Generic.List<Mesh>();
            foreach (string body in new[] { "truck.monster", "truck.lifted", "semi.lowrider", "truck.fire", "novelty.hotdog", "novelty.icecream", "semi.standard" })
            {
                f.Save(0, body); Call(f.match, "RefreshParticipants", false); f.Start(); f.match.enabled = false; Time.timeScale = 0;
                Assert.That((string)Get(f.Bridge, "LastFallback"), Is.Empty); Assert.That(Get(f.Host(0), "EffectiveBodyId"), Is.EqualTo(body)); Assert.That(PiecePhysics(f.Pool), Is.EqualTo(physics));
                for (int slot = 0; slot < 4; slot++) { Assert.That(Get(f.Host(slot), "ProfileId"), Is.EqualTo(f.ids[slot])); foreach (var wing in ((Transform[])Get(f.Racers[slot], "wings"))) { Assert.That(wing.gameObject.activeSelf, Is.True); Assert.That(wing.GetComponentsInChildren<Renderer>(true).All(renderer => renderer.enabled), Is.True); } CheckGraphics(f.Bridge, slot, false); }
                var selected = pieces.Where(piece => (int)Get(piece, "owner") == 0).ToArray(); var bodies = selected.Select(piece => (Rigidbody)Get(piece, "body")).ToArray(); var before = bodies.Select(rb => rb.position).ToArray(); var velocities = bodies.Select(rb => rb.linearVelocity).ToArray();
                Assert.That((bool)Call(f.Bridge, "BeforeEmit", 0), Is.True); CollectionAssert.AreEqual(before, bodies.Select(rb => rb.position).ToArray()); CollectionAssert.AreEqual(velocities, bodies.Select(rb => rb.linearVelocity).ToArray());
                var expectedVelocity = selected.Select(piece => { var source = (Transform)Get(piece, "source"); var center = source.TransformPoint(source.GetComponent<MeshFilter>().sharedMesh.bounds.center); var radial = center - f.Body(0).worldCenterOfMass; if (radial.sqrMagnitude < .01f) radial = Vector3.up; radial.Normalize(); var value = f.Body(0).GetPointVelocity(center) * .65f + radial * 5 + Vector3.up * 6; value.y = Mathf.Max(value.y, (float)Get(f.Pool, "minimumUpwardVelocity")); return value; }).ToArray();
                // Controlled original public Emit comparison, separate from the
                // actual lava-contact case below. No rule/scoring callback.
                Call(f.Pool, "Emit", 0); var emittedPositions = bodies.Select(rb => rb.position).ToArray(); Call(f.Bridge, "AfterEmit", 0, true);
                CollectionAssert.AreEqual(emittedPositions, bodies.Select(rb => rb.position).ToArray()); for (int i = 0; i < 12; i++) Assert.That(Vector3.Distance(bodies[i].linearVelocity, expectedVelocity[i]), Is.LessThan(.00001f), "Render bridge preserves the exact original physical launch formula.");
                CheckGraphics(f.Bridge, 0, true); Assert.That((int)Get(f.Bridge, "ActualEmissions"), Is.EqualTo(1)); Assert.That(Get(f.Bridge, "LastEmittedBodyId"), Is.EqualTo(body)); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);
                CollectionAssert.AreEqual(ghosts, ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray()); CollectionAssert.AreEqual(pieceGhosts, pieces.Select(piece => ((Transform)Get(piece, "ghost")).GetInstanceID()).ToArray()); CollectionAssert.AreEqual(pieceRenderers, pieces.Select(piece => ((Renderer)Get(piece, "ghostRenderer")).GetInstanceID()).ToArray());
                var state = ((Array)Get(f.Bridge, "owners")).GetValue(0); owned.AddRange(((IEnumerable)Get(state, "graphics")).Cast<object>().Select(graphic => (Mesh)Get(graphic, "mesh"))); yield return null;
            }
            Call(f.match, "SetupScreen"); UnityEngine.Object.Destroy(f.match.gameObject); yield return null; yield return null; Assert.That(owned.All(mesh => mesh == null), Is.True, "Bridge-owned mesh clones must be released on scene/match destruction."); f.match = null;
        }
    }
    [UnityTest, Timeout(90000)]
    public IEnumerator ActualLavaTriggerEmitsSelectedBodyOnceAndRecordsOriginalDebrisWithoutChangingScoreAuthority()
    {
        using (var f = new Fixture())
        {
            yield return f.Load(); f.Start(1); float deadline = Time.realtimeSinceStartup + 8; while (!(bool)Get(f.match, "RoundLive") && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate(); Assert.That((bool)Get(f.match, "RoundLive"), Is.True);
            var lava = (Transform)Get(f.Volcano, "lava"); var position = lava.position + new Vector3(160, 5, 0);
            // Controlled initial physical fall onto the original lava trigger.
            // Neither LavaImpact/Explode/Emit/Lock nor score setters are invoked.
            Call(f.Racers[0], "ResetRacer", position); Call(f.Truck(0), "Power", false); f.Body(0).linearVelocity = Vector3.down * 2; UnityEngine.Physics.SyncTransforms();
            deadline = Time.realtimeSinceStartup + 8; while ((int)Get(f.Volcano, "Impacts") == 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That((int)Get(f.Volcano, "Impacts"), Is.EqualTo(1)); Assert.That((int)Get(f.Pool, "Emissions"), Is.EqualTo(1)); Assert.That((int)Get(f.Bridge, "ActualEmissions"), Is.EqualTo(1)); Assert.That(Get(f.Racers[0], "ResolutionReason"), Is.EqualTo("ACTUAL_LAVA_CONTACT")); Assert.That(((int[])Get(Get(f.match, "rules"), "last"))[0], Is.Zero);
            int receipts = (int)Get(Get(f.match, "ScoreJournal"), "Count"); yield return new WaitForSeconds(.25f); Assert.That((int)Get(f.Pool, "Emissions"), Is.EqualTo(1)); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.EqualTo(receipts));
            deadline = Time.realtimeSinceStartup + 8; while (!(bool)Get(f.Replay, "Playing") && Time.realtimeSinceStartup < deadline) yield return null; Assert.That((bool)Get(f.Replay, "Playing"), Is.True);
            Time.timeScale = 0; Call(f.Replay, "Seek", Mathf.Max(0, (float)Get(f.Replay, "Duration") - .1f)); var debris = ((Transform[])Get(f.Replay, "ghosts"))[5]; Assert.That(debris.gameObject.activeSelf, Is.True); Assert.That(debris.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.name.StartsWith("Target actual appearance surface")), Is.True); Assert.That(debris.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(debris.GetComponentsInChildren<Collider>(true), Is.Empty);
            var oldPositions = ((Array)Get(f.Pool, "pieces")).Cast<object>().Select(piece => ((Rigidbody)Get(piece, "body")).position).ToArray(); foreach (int angle in new[] { 0, 1, 2 }) { if (angle > 0) Call(f.match, "ExtraAction", "replayangle"); Call(f.match, "ExtraAction", "replayrewind"); yield return null; Assert.That((int)Get(f.Replay, "Angle"), Is.EqualTo(angle)); } CollectionAssert.AreEqual(oldPositions, ((Array)Get(f.Pool, "pieces")).Cast<object>().Select(piece => ((Rigidbody)Get(piece, "body")).position).ToArray());
            TestContext.WriteLine("TARGET_ACTUAL_LAVA selected body, original trigger/48-body Emit once, genuine zero outcome, actual recorded selected debris and3 replay angles. Initial fall pose/velocity controlled; no direct contact/outcome injection.");
            f.Start(1); Assert.That((int)Get(f.Bridge, "ActualEmissions"), Is.Zero); Assert.That((int)Get(f.Pool, "Emissions"), Is.Zero); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);
        }
    }
}
#endif
