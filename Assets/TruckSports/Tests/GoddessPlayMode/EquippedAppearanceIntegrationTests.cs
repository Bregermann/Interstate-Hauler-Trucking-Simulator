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

// Actual saved typed actors/equipment with explicit synthetic expression and
// recorded-pose samples. Does not claim a complete race/heat or art acceptance.
public sealed class EquippedAppearanceIntegrationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    Component match; IDisposable lease; GameObject isolation;
    IEnumerator Load(bool sumo)
    {
        lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "EquippedAppearance" + Guid.NewGuid().ToString("N") }); Registry("ForgetCache");
        isolation = new GameObject("Explicit equipped appearance fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        string mode = sumo ? "TruckSumo" : "TruckBogSnorkelling";
        EditorSceneManager.LoadSceneInPlayMode("Assets/" + mode + "/Scenes/" + mode + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        var type = Type.GetType(sumo ? "LWS.TruckSumo.SumoMatch, Assembly-CSharp" : "LWS.TruckBogSnorkelling.BogMatch, Assembly-CSharp", true);
        match = (Component)UnityEngine.Object.FindAnyObjectByType(type); Assert.That(match, Is.Not.Null); Call(match, "AllBots"); Set(Get(match, "setup"), "count", 4); Call(match, "NormalizeSetup");
    }
    [TearDown] public void Cleanup()
    {
        if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); Registry("ForgetCache"); lease?.Dispose(); lease = null;
    }
    static void Save(string id, string body, string slot, string item)
    {
        var draft = Registry("GetAppearance", id); Set(draft, "bodyId", body); ((IList)Get(draft, "selections")).Clear();
        var selection = Activator.CreateInstance(Sports("SportsTruckCosmeticSelection")); Set(selection, "slotId", slot); Set(selection, "itemId", item); ((IList)Get(draft, "selections")).Add(selection);
        Assert.That(Registry("TrySetAppearance", id, draft), Is.True);
    }
    static string Physics(Component truck, Collider[] originalColliders = null)
    {
        var body = (Rigidbody)Get(truck, "body"); string value = body.GetInstanceID() + ":" + body.mass + ":" + body.centerOfMass.ToString("R") + ":" + truck.transform.localScale.ToString("R");
        foreach (var collider in originalColliders ?? truck.GetComponentsInChildren<Collider>(true))
        {
            Assert.That(collider != null, Is.True, "An original physical component must survive.");
            value += "/" + collider.GetInstanceID() + ":" + collider.GetType().Name + ":" + collider.isTrigger;
            if (collider is BoxCollider box) value += ":" + box.center.ToString("R") + ":" + box.size.ToString("R");
            if (collider is MeshCollider mesh) value += ":" + (mesh.sharedMesh != null ? mesh.sharedMesh.GetInstanceID() : 0) + ":" + mesh.convex;
        }
        foreach (var wheel in (IEnumerable)Get(truck, "wheels")) { var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring"); value += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "width") + ":" + Get(data, "mass") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce"); }
        return value;
    }
    static string Gear(object host)
    {
        string value = "";
        foreach (var part in (Transform[])Get(host, "protectedRoots"))
        {
            Assert.That(part, Is.Not.Null); value += part.GetInstanceID() + ":" + part.parent.GetInstanceID() + ":" + part.localPosition.ToString("R") + ":" + part.localRotation.ToString("R") + ":" + part.localScale.ToString("R");
            foreach (var renderer in part.GetComponentsInChildren<Renderer>(true)) value += "/R" + renderer.GetInstanceID() + ":" + renderer.enabled + ":" + string.Join(",", renderer.sharedMaterials.Select(material => material != null ? material.GetInstanceID() : 0));
            foreach (var light in part.GetComponentsInChildren<Light>(true)) value += "/L" + light.GetInstanceID() + ":" + light.enabled + ":" + light.intensity + ":" + light.range + ":" + light.cullingMask;
        }
        return value;
    }
    static GameObject Model(object host)
    {
        var model = (GameObject)Get(host, "model"); Assert.That(model, Is.Not.Null);
        Assert.That(model.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(model.GetComponentsInChildren<Light>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Camera>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<AudioListener>(true), Is.Empty); return model;
    }
    static string[] Topology(Transform root) => root.GetComponentsInChildren<Transform>(true).Select(part => part == root ? "ROOT" : part.name).ToArray();
    IEnumerator Exercise(bool sumo)
    {
        yield return Load(sumo); var actors = ((Array)Get(match, sumo ? "fighters" : "racers")).Cast<Component>().ToArray(); var trucks = actors.Select(actor => (Component)Get(actor, "truck")).ToArray();
        Assert.That(actors.Length, Is.EqualTo(4)); var hosts = trucks.Select(truck => Get(truck, "appearanceHost")).ToArray(); Assert.That(hosts.All(host => host != null), Is.True, "Invoke explicit BindMissingBogAndSumoHosts first.");
        var ids = new string[4];
        for (int i = 0; i < 4; i++)
        {
            ids[i] = (string)Get(Registry("Create", "Same Equipped Driver"), "id"); Registry("UseInSeat", i, ids[i]);
            Assert.That(Get(trucks[i], "session"), Is.SameAs(match)); Assert.That(Get(hosts[i], "participantSlot"), Is.EqualTo(i));
            var inner = (Transform)Get(hosts[i], "presentationParent"); Assert.That(inner, Is.SameAs(((Renderer)Get(trucks[i], "paint")).transform.parent)); Assert.That(inner, Is.Not.SameAs(trucks[i].transform));
            if (sumo) { Assert.That(inner.parent, Is.SameAs((Transform)Get(trucks[i], "visuals"))); Assert.That(((Transform[])Get(hosts[i], "protectedRoots")).Length, Is.EqualTo(8)); }
            else Assert.That(inner, Is.SameAs((Transform)Get(trucks[i], "visuals")));
        }
        Assert.That(ids.Distinct().Count(), Is.EqualTo(4)); Call(match, "RefreshParticipants", false);
        var originalColliders = trucks.Select(truck => truck.GetComponentsInChildren<Collider>(true)).ToArray();
        var originalPhysical = trucks.Select((truck, i) => Physics(truck, originalColliders[i])).ToArray(); string[] physical = null;
        var intakes = sumo ? null : actors.Select(actor => (Transform)Get(actor, "intake")).ToArray(); var intakeLocal = intakes?.Select(intake => intake.localPosition).ToArray();
        object replay = sumo ? Get(match, "replay") : null; var ghosts = replay != null ? ((Transform[])Get(replay, "ghosts")).ToArray() : null; var firstChildren = ghosts?.Select(ghost => ghost.GetChild(0)).ToArray();
        string[] bodies = { "truck.monster", "truck.lifted", "semi.lowrider", "truck.fire", "novelty.hotdog", "novelty.icecream", "semi.standard" };
        for (int n = 0; n < bodies.Length; n++)
        {
            Save(ids[0], bodies[n], sumo ? "face.brows" : "hat", sumo ? "brows.angry" : "hat.crown");
            if (n == 0) for (int i = 1; i < 4; i++) Save(ids[i], i == 1 ? "semi.lowrider" : i == 2 ? "truck.fire" : "novelty.icecream", "wheels", "wheels.gold");
            Call(match, "RefreshParticipants", false); var before = hosts.Select(Gear).ToArray();
            var airBefore = sumo ? null : actors.Select(actor => (float)Get(actor, "Air")).ToArray(); var intakeBefore = sumo ? null : actors.Select(actor => (Vector3)Get(actor, "IntakePosition")).ToArray();
            Call(match, "PrepareAppearances"); CollectionAssert.AreEqual(before, hosts.Select(Gear), "Appearance preparation must not move/scale/hide/reparent actual sport equipment or change its shared art.");
            if (!sumo) { CollectionAssert.AreEqual(airBefore, actors.Select(actor => (float)Get(actor, "Air"))); CollectionAssert.AreEqual(intakeBefore, actors.Select(actor => (Vector3)Get(actor, "IntakePosition"))); }
            Assert.That(Call(match, "StartMatch"), Is.True);
            // Bog disables wheels throughout its real countdown. Observe the
            // original countdown/release once before freezing controlled trials.
            if (!sumo && n == 0)
            {
                ((Behaviour)match).enabled = true; double releaseDeadline = Time.realtimeSinceStartupAsDouble + 8;
                while (Get(match, "Phase").ToString() == "Countdown" && Time.realtimeSinceStartupAsDouble < releaseDeadline) yield return null;
                TestContext.WriteLine("BOG_ACTUAL_STARTUP phase=" + Get(match, "Phase") + " age=" + Get(match, "Age") + " clock=" + Get(match, "Clock") + " live=" + Get(match, "Live") + " paused=" + Get(match, "Paused") + " timeScale=" + Time.timeScale);
                Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("Racing"), "The real Bog countdown must reach its original Release within8real seconds before controlled body trials.");
            }
            ((Behaviour)match).enabled = false;
            // Let the original NWH Start initialize its owned wheel colliders.
            // No appearance-created or unrelated physical component is permitted.
            yield return null; yield return null;
            for (int i = 0; i < trucks.Length; i++)
            {
                Assert.That(Physics(trucks[i], originalColliders[i]), Is.EqualTo(originalPhysical[i]));
                var wheelColliders = ((IEnumerable)Get(trucks[i], "wheels")).Cast<object>().Select(wheel => (Collider)Get(Get(wheel, "wheel"), "meshCollider")).ToArray();
                Assert.That(wheelColliders.Length, Is.EqualTo(4)); Assert.That(wheelColliders.All(collider => collider != null && collider is MeshCollider && collider.name == "Collider"), Is.True);
                CollectionAssert.AreEquivalent(originalColliders[i].Concat(wheelColliders).Distinct(), trucks[i].GetComponentsInChildren<Collider>(true), "Only exact existing NWH wheel collider bindings may be added.");
                if (n == 0) TestContext.WriteLine("EQUIPPED_INITIALIZED_PHYSICS mode=" + (sumo ? "SUMO" : "BOG") + " slot=" + i + " original=" + originalPhysical[i] + " wheelColliders=" + string.Join(",", wheelColliders.Select(collider => collider.GetInstanceID() + ":" + collider.transform.parent.name)));
            }
            if (n == 0) physical = trucks.Select(truck => Physics(truck)).ToArray();
            for (int i = 0; i < 4; i++)
            {
                Assert.That(Get(hosts[i], "ProfileId"), Is.EqualTo(ids[i])); Assert.That(Get(hosts[i], "EffectiveBodyId"), Is.EqualTo(i == 0 ? bodies[n] : i == 1 ? "semi.lowrider" : i == 2 ? "truck.fire" : "novelty.icecream"));
                Assert.That(Physics(trucks[i]), Is.EqualTo(physical[i])); var model = Model(hosts[i]);
                var protectedRoots = (Transform[])Get(hosts[i], "protectedRoots"); Assert.That(((Renderer[])Get(hosts[i], "originalBodyRenderers")).All(renderer => protectedRoots.All(root => !renderer.transform.IsChildOf(root))), Is.True);
                if (sumo)
                {
                    Assert.That(model.GetComponentsInChildren<Transform>(true).Any(part => part.name == "face.brows mount"), Is.False, "Required live Sumo brows suppress duplicate saved brows only in this mode.");
                    Assert.That(((Transform)Get(trucks[i], "visuals")).gameObject.activeSelf, Is.True);
                    Assert.That(Topology(ghosts[i]), Is.EqualTo(Topology((Transform)Get(trucks[i], "visuals")))); Assert.That(((Transform[])Get(replay, "ghosts"))[i], Is.SameAs(ghosts[i])); Assert.That(ghosts[i].GetChild(0), Is.SameAs(firstChildren[i]));
                }
                else
                {
                    Assert.That(intakes[i], Is.SameAs((Transform)Get(actors[i], "intake"))); Assert.That(intakes[i].parent, Is.SameAs(trucks[i].transform)); Assert.That(intakes[i].localPosition, Is.EqualTo(intakeLocal[i]));
                    var body = (Rigidbody)Get(trucks[i], "body"); var expected = body.position + body.rotation * Vector3.Scale(intakeLocal[i], trucks[i].transform.lossyScale); Assert.That(Vector3.Distance((Vector3)Get(actors[i], "IntakePosition"), expected), Is.LessThan(.001f));
                }
            }
            if (sumo) { Assert.That(((Transform[])Get(replay, "ghosts"))[4], Is.SameAs(ghosts[4])); Assert.That(ghosts[4].GetChild(0), Is.SameAs(firstChildren[4])); }
            yield return null; yield return null;
        }
        if (sumo)
        {
            var face = Type.GetType("LWS.TruckSumo.SumoFace, Assembly-CSharp", true); var brows = (Transform[])Get(actors[0], "brows"); var before = brows.Select(brow => brow.localRotation).ToArray();
            Call(actors[0], "Expression", Enum.Parse(face, "Shove"), 9, 1f); Call(actors[0], "SnapFace"); Assert.That(brows.Where((brow, i) => Quaternion.Angle(brow.localRotation, before[i]) > 5).Count(), Is.EqualTo(2), "Original real brow pivots remain animated on the selected body.");
            Assert.That(((IList)Get(Registry("GetAppearance", ids[0]), "selections")).Count, Is.EqualTo(1)); Assert.That((string)Get(hosts[0], "CompatibilityNotice"), Does.Contain("animated eyebrows"));
            Call(replay, "Record", .1f, 31); Call(replay, "Record", .2f, 31); Call(replay, "Begin", 0f, float.PositiveInfinity);
            Assert.That((bool)Get(replay, "Playing"), Is.True); Assert.That(Call(replay, "RebuildGhostHierarchy", 0), Is.False); Assert.That(Call(replay, "InvalidateFreshRecord"), Is.False); Call(replay, "End");
        }
        int revision = (int)Get(hosts[0], "AppearanceRevision"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); Assert.That(Get(hosts[0], "AppearanceRevision"), Is.EqualTo(revision));
        Registry("Rename", ids[0], "Renamed Equipped Driver", Color.magenta, true); Save(ids[0], "missing.saved.body", "hat", "missing.saved.hat"); Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True);
        Assert.That(Get(hosts[0], "ProfileId"), Is.EqualTo(ids[0])); Assert.That(Get(hosts[0], "EffectiveBodyId"), Is.EqualTo("semi.standard")); Assert.That(Get(Registry("GetAppearance", ids[0]), "bodyId"), Is.EqualTo("missing.saved.body")); Assert.That(Physics(trucks[0]), Is.EqualTo(physical[0]));
        TestContext.WriteLine("SAVED_EQUIPPED_APPEARANCE " + (sumo ? "SUMO" : "BOG") + " four fixed stableIDs/all7bodies, exactgear/physicalbindings/rematch/missingID retention; " + (sumo ? "actual brow expression and in-place five-source replay refs/playing guard" : "unchanged actual intake/air/headlamp/snorkel bindings") + ". Synthetic expression/pose samples; no complete race/heat/art acceptance claim.");
    }
    [UnityTest, Timeout(180000)] public IEnumerator BogAllSevenBodiesRetainActualIntakeSnorkelHeadlampsAndPhysicalVehicle() { yield return Exercise(false); }
    [UnityTest, Timeout(180000)] public IEnumerator SumoAllSevenBodiesRetainCostumeLiveBrowsAndOriginalReplayRoots() { yield return Exercise(true); }
}
#endif
