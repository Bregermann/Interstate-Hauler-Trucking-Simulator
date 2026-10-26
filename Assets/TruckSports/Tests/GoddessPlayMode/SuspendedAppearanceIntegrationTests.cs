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

// Saved typed mode bindings; explicit synthetic deployment/recovery/visual-wheel
// samples. Full flight/demolition matches and visual approval remain separate.
public sealed class SuspendedAppearanceIntegrationTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    Component match; IDisposable lease; GameObject isolation;
    IEnumerator Load(bool bungee)
    {
        lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SuspendedAppearance" + Guid.NewGuid().ToString("N") }); Registry("ForgetCache");
        isolation = new GameObject("Explicit suspended appearance fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        string mode = bungee ? "TruckBungeeDemolition" : "TruckBaseJumping";
        EditorSceneManager.LoadSceneInPlayMode("Assets/" + mode + "/Scenes/" + mode + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        var type = Type.GetType(bungee ? "LWS.TruckBungeeDemolition.BungeeMatch, Assembly-CSharp" : "LWS.TruckBaseJumping.BaseJumpMatch, Assembly-CSharp", true);
        match = (Component)UnityEngine.Object.FindAnyObjectByType(type); Assert.That(match, Is.Not.Null); Call(match, "AllBots"); Set(Get(match, "setup"), "count", 4); Call(match, "NormalizeSetup");
    }
    [TearDown] public void Cleanup()
    {
        if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); Registry("ForgetCache"); lease?.Dispose(); lease = null;
    }
    static void Save(string id, string body)
    {
        var draft = Registry("GetAppearance", id); bool identical = (string)Get(draft, "bodyId") == body && ((IList)Get(draft, "selections")).Count == 0; Set(draft, "bodyId", body); ((IList)Get(draft, "selections")).Clear();
        Assert.That(Registry("TrySetAppearance", id, draft), Is.EqualTo(!identical), "The registry writes a change and rejects an identical detached draft.");
        Registry("ForgetCache"); var reloaded = Registry("GetAppearance", id); Assert.That(Get(reloaded, "bodyId"), Is.EqualTo(body)); Assert.That(((IList)Get(reloaded, "selections")).Count, Is.Zero, "Verify the actual persisted draft after a registry reload.");
    }
    static string Physics(Component truck)
    {
        var body = (Rigidbody)Get(truck, "body"); string value = body.GetInstanceID() + ":" + body.mass + ":" + body.centerOfMass.ToString("R") + ":" + truck.transform.localScale.ToString("R");
        value += "/RB=" + string.Join(",", truck.GetComponentsInChildren<Rigidbody>(true).Select(part => part.GetInstanceID()));
        foreach (var collider in truck.GetComponentsInChildren<Collider>(true))
        {
            value += "/" + collider.GetInstanceID() + ":" + collider.GetType().Name + ":" + collider.isTrigger;
            if (collider is BoxCollider box) value += ":" + box.center.ToString("R") + ":" + box.size.ToString("R");
            if (collider is MeshCollider mesh) value += ":" + mesh.convex + ":" + (mesh.sharedMesh != null ? mesh.sharedMesh.GetInstanceID() : 0);
        }
        foreach (var wheel in (IEnumerable)Get(truck, "wheels"))
        {
            var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring"); value += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "width") + ":" + Get(data, "mass") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce");
        }
        return value;
    }
    static string Gear(object host)
    {
        string value = "";
        foreach (var root in (Transform[])Get(host, "protectedRoots"))
        {
            Assert.That(root != null, Is.True); value += "/" + root.GetInstanceID() + ":" + (root.parent != null ? root.parent.GetInstanceID() : 0) + ":" + root.localPosition.ToString("R") + ":" + root.localRotation.ToString("R") + ":" + root.localScale.ToString("R");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) value += "/R" + renderer.GetInstanceID() + ":" + renderer.enabled + ":" + string.Join(",", renderer.sharedMaterials.Select(material => material != null ? material.GetInstanceID() : 0));
            foreach (var shape in root.GetComponentsInChildren<Collider>(true)) value += "/C" + shape.GetInstanceID() + ":" + shape.enabled + ":" + shape.isTrigger;
        }
        return value;
    }
    static string Spring(SpringJoint spring) => spring.GetInstanceID() + ":" + spring.anchor.ToString("R") + ":" + spring.connectedAnchor.ToString("R") + ":" + (spring.connectedBody != null ? spring.connectedBody.GetInstanceID() : 0) + ":" + spring.minDistance + ":" + spring.maxDistance + ":" + spring.spring + ":" + spring.damper + ":" + spring.tolerance + ":" + spring.massScale + ":" + spring.connectedMassScale + ":" + spring.enableCollision;
    static void AssertModel(object host)
    {
        var model = (GameObject)Get(host, "model"); Assert.That(model, Is.Not.Null);
        Assert.That(model.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(model.GetComponentsInChildren<Joint>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Camera>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<AudioListener>(true), Is.Empty);
        Assert.That(((Renderer[])Get(host, "originalBodyRenderers")).All(renderer => !renderer.enabled), Is.True);
        Assert.That(((Renderer[])Get(host, "originalWheelRenderers")).All(renderer => !renderer.enabled), Is.True, "Do not render the old semi tires underneath body-default tire followers.");
    }
    static void SampleVisualWheelRotation(object host)
    {
        var followers = ((IEnumerable)Get(host, "wheelVisuals")).Cast<object>().ToArray(); Assert.That(followers.Length, Is.EqualTo(4), "Each body retains recognizable body-default tire geometry on the four existing NWH visual sources.");
        foreach (var follower in followers)
        {
            var source = (Transform)Get(follower, "source"); var visual = (Transform)Get(follower, "visual"); var prior = source.rotation;
            try
            {
                source.rotation = prior * Quaternion.Euler(43, 0, 0); Call(host, "LateUpdate");
                Assert.That(Quaternion.Angle(visual.rotation, source.rotation * (Quaternion)Get(follower, "alignment")), Is.LessThan(.01f));
                Assert.That(Vector3.Distance(visual.position, source.position + ((Component)host).transform.up * (float)Get(follower, "verticalOffset")), Is.LessThan(.001f));
            }
            finally { source.rotation = prior; Call(host, "LateUpdate"); }
        }
    }
    IEnumerator Exercise(bool bungee)
    {
        yield return Load(bungee);
        var actors = ((Array)Get(match, bungee ? "bays" : "jumpers")).Cast<Component>().ToArray(); Assert.That(actors.Length, Is.EqualTo(4));
        var trucks = actors.Select(actor => (Component)Get(actor, "truck")).ToArray(); var hosts = trucks.Select(truck => Get(truck, "appearanceHost")).ToArray(); Assert.That(hosts.All(host => host != null), Is.True, "Invoke BindMissingBaseAndBungeeHosts first.");
        var ids = new string[4]; var visuals = trucks.Select(truck => (Transform)Get(truck, "visuals")).ToArray(); var wheels = trucks.Select(truck => ((IEnumerable)Get(truck, "wheels")).Cast<Component>().ToArray()).ToArray();
        for (int i = 0; i < 4; i++)
        {
            ids[i] = (string)Get(Registry("Create", "Same Suspended Driver"), "id"); Registry("UseInSeat", i, ids[i]); Save(ids[i], i == 0 ? "truck.monster" : i == 1 ? "semi.lowrider" : i == 2 ? "truck.fire" : "novelty.icecream");
            Assert.That(Get(trucks[i], "session"), Is.SameAs(match)); Assert.That(Get(actors[i], "slot"), Is.EqualTo(i)); Assert.That(Get(hosts[i], "participantSlot"), Is.EqualTo(i));
            Assert.That(Get(hosts[i], "presentationParent"), Is.SameAs(visuals[i])); Assert.That(visuals[i], Is.Not.SameAs(trucks[i].transform));
            Assert.That(visuals[i], Is.SameAs(((Renderer)Get(trucks[i], "paint")).transform.parent));
            if (!bungee)
            {
                CollectionAssert.AreEquivalent(new[] { "Whl HD FL", "Whl HD FR", "Whl HD RL", "Whl HD RR" }, ((Renderer[])Get(hosts[i], "originalWheelRenderers")).Select(renderer => renderer.name));
                var original = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab"); Assert.That(original, Is.Not.Null);
                foreach (var renderer in (Renderer[])Get(hosts[i], "originalWheelRenderers")) Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(original.GetComponentsInChildren<MeshFilter>(true).Single(mesh => mesh.name == renderer.name).sharedMesh), "Bind the installed original wheel mesh identity, not a loose full-root name match.");
                foreach (var wheel in wheels[i]) Assert.That(((Transform)Get(Get(wheel, "wheel"), "rotatingContainer")).GetComponentsInChildren<Renderer>(true), Is.Empty, "Original Base Jumping containers stay empty; only the owned render adapter follows them.");
            }
        }
        Assert.That(ids.Distinct().Count(), Is.EqualTo(4)); Call(match, "RefreshParticipants", false); var physical = trucks.Select(Physics).ToArray();
        Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; yield return null; yield return null;
        // These two modes intentionally keep wheel propulsion disabled. No
        // vendor wheel initialization or new physical authority is required.
        CollectionAssert.AreEqual(physical, trucks.Select(Physics));
        var springRefs = bungee ? actors.Select(actor => (SpringJoint)Get(actor, "spring")).ToArray() : null;
        var canopyRefs = bungee ? null : actors.Select(actor => (Transform)Get(actor, "canopy")).ToArray();
        var lines = bungee ? null : actors.Select(actor => (LineRenderer[])Get(actor, "lines")).ToArray();
        var transport = bungee ? null : Get(match, "transport"); var transportModel = bungee ? null : (GameObject)Get(transport, "model");
        string[] bodies = { "truck.monster", "truck.lifted", "semi.lowrider", "truck.fire", "novelty.hotdog", "novelty.icecream", "semi.standard" };
        for (int n = 0; n < bodies.Length; n++)
        {
            Save(ids[0], bodies[n]); Call(match, "RefreshParticipants", false);
            var gear = hosts.Select(Gear).ToArray(); var springState = springRefs?.Select(Spring).ToArray();
            var scores = actors.Select(actor => bungee ? (int)Get(actor, "Score") : (bool)Get(actor, "Landed") ? 1 : 0).ToArray(); int journal = (int)Get(Get(match, "ScoreJournal"), "Count");
            var modules = bungee ? actors.Select(actor => ((Array)Get(actor, "modules")).Cast<Component>().ToArray()).ToArray() : null;
            Call(match, "PrepareAppearances"); CollectionAssert.AreEqual(gear, hosts.Select(Gear), "Appearance composition must leave actual gear pose, parent, renderer, collider and material references intact.");
            if (bungee) CollectionAssert.AreEqual(springState, springRefs.Select(Spring));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(Get(hosts[i], "ProfileId"), Is.EqualTo(ids[i])); Assert.That(Get(hosts[i], "EffectiveBodyId"), Is.EqualTo(i == 0 ? bodies[n] : i == 1 ? "semi.lowrider" : i == 2 ? "truck.fire" : "novelty.icecream"));
                Assert.That(Get(trucks[i], "visuals"), Is.SameAs(visuals[i])); CollectionAssert.AreEqual(wheels[i], ((IEnumerable)Get(trucks[i], "wheels")).Cast<Component>()); Assert.That(Physics(trucks[i]), Is.EqualTo(physical[i])); AssertModel(hosts[i]);
                if (bungee)
                {
                    Assert.That(Get(actors[i], "spring"), Is.SameAs(springRefs[i])); CollectionAssert.AreEqual(modules[i], ((Array)Get(actors[i], "modules")).Cast<Component>());
                    var rope = (LineRenderer)Get(actors[i], "rope"); Call(actors[i], "LateUpdate");
                    Assert.That(rope.positionCount, Is.EqualTo(32)); Assert.That(Vector3.Distance(rope.GetPosition(0), ((Transform)Get(actors[i], "craneAnchor")).position), Is.LessThan(.001f));
                    var body = (Rigidbody)Get(trucks[i], "body"); Assert.That(Vector3.Distance(rope.GetPosition(31), body.position + body.rotation * new Vector3(0, .5f, -.5f)), Is.LessThan(.001f));
                }
                else
                {
                    Assert.That(Get(actors[i], "canopy"), Is.SameAs(canopyRefs[i])); CollectionAssert.AreEqual(lines[i], (LineRenderer[])Get(actors[i], "lines")); Assert.That(Get(Get(match, "transport"), "model"), Is.SameAs(transportModel));
                }
            }
            SampleVisualWheelRotation(hosts[0]); Assert.That((int)Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(journal)); CollectionAssert.AreEqual(scores, actors.Select(actor => bungee ? (int)Get(actor, "Score") : (bool)Get(actor, "Landed") ? 1 : 0));
            yield return null;
        }
        int revision = (int)Get(hosts[0], "AppearanceRevision");
        if (bungee)
        {
            var before = Spring(springRefs[0]); int score = (int)Get(actors[0], "Score"); Call(actors[0], "Recover");
            Assert.That(Get(actors[0], "Recoveries"), Is.EqualTo(1)); Assert.That(Get(actors[0], "RecoveryTime"), Is.EqualTo(3f)); Assert.That(Spring(springRefs[0]), Is.EqualTo(before)); Assert.That(Get(actors[0], "Score"), Is.EqualTo(score));
        }
        else
        {
            var jumper = actors[0]; Call(jumper, "Release"); Call(jumper, "Step", Vector2.zero, true, false, .1f, Vector3.zero); Call(jumper, "Draw", .1f);
            Assert.That(Get(jumper, "Deployed"), Is.EqualTo(true)); Assert.That(canopyRefs[0].gameObject.activeSelf, Is.True);
            Vector3[] harness = { new Vector3(-.5f, .45f, -.9f), new Vector3(.5f, .45f, -.9f), new Vector3(-.5f, .45f, .9f), new Vector3(.5f, .45f, .9f) };
            for (int i = 0; i < 4; i++) { Assert.That(lines[0][i].enabled, Is.True); Assert.That(Vector3.Distance(lines[0][i].GetPosition(0), trucks[0].transform.TransformPoint(harness[i])), Is.LessThan(.001f)); Assert.That(Vector3.Distance(lines[0][i].GetPosition(1), canopyRefs[0].TransformPoint(new Vector3(i % 2 == 0 ? -3.2f : 3.2f, 0, i < 2 ? -2.5f : 2.5f))), Is.LessThan(.001f)); }
            Call(jumper, "Prepare", (Vector3)Call(transport, "Stage", 0)); Assert.That(canopyRefs[0].gameObject.activeSelf, Is.False); Assert.That(lines[0].All(line => !line.enabled), Is.True);
        }
        Assert.That(Get(hosts[0], "AppearanceRevision"), Is.EqualTo(revision));
        Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); Assert.That(Get(hosts[0], "AppearanceRevision"), Is.EqualTo(revision));
        Save(ids[0], "truck.monster"); Registry("Rename", ids[0], "Renamed Suspended Driver", Color.magenta, true); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True);
        Assert.That(Get(hosts[0], "EffectiveBodyId"), Is.EqualTo("truck.monster")); Assert.That((int)Get(hosts[0], "AppearanceRevision"), Is.GreaterThan(revision)); Assert.That(Get(hosts[0], "ProfileId"), Is.EqualTo(ids[0]));
        Save(ids[0], "missing.saved.suspended.body"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); Assert.That(Get(hosts[0], "EffectiveBodyId"), Is.EqualTo("semi.standard")); Assert.That(Get(Registry("GetAppearance", ids[0]), "bodyId"), Is.EqualTo("missing.saved.suspended.body"));
        CollectionAssert.AreEqual(physical, trucks.Select(Physics));
        TestContext.WriteLine("SAVED_SUSPENDED_APPEARANCE mode=" + (bungee ? "BUNGEE" : "BASEJUMP") + " four fixed stableIDs/all7body composition/unchanged physics and actual equipment, body-default NWH visual follower sample, real Prepare/" + (bungee ? "Recover spring+rope" : "Release+Step+Draw canopy/harness") + "/rematch/missingID retention. Synthetic deployment/recovery/rotation, not a complete physical match or visual acceptance.");
    }
    [UnityTest, Timeout(180000)] public IEnumerator BaseJumpAllSevenBodiesPreserveActualHelicopterCanopyHarnessAndFixedOwners() { yield return Exercise(false); }
    [UnityTest, Timeout(180000)] public IEnumerator BungeeAllSevenBodiesPreserveActualCraneSpringRopeTowerAndRecoverOwnership() { yield return Exercise(true); }
}
#endif
