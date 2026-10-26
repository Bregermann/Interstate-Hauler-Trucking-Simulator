#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Saved-scene appearance integration. Controller-pose and camera callback
// checks are explicitly synthetic; these are not complete match/art/device tests.
public sealed class CleanModelAppearanceIntegrationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string member) => owner.GetType().GetField(member, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(member, Any).GetValue(owner);
    static void Set(object owner, string member, object value) => owner.GetType().GetField(member, Any).SetValue(owner, value);
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethods(Any).Single(candidate => candidate.Name == method && candidate.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string method, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(method).Invoke(null, args);
    Component match;
    GameObject isolation;
    IDisposable lease;

    IEnumerator Load(string mode)
    {
        lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CleanAppearance" + Guid.NewGuid().ToString("N") });
        Registry("ForgetCache"); isolation = new GameObject("Explicit clean-model appearance fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/" + mode + "/Scenes/" + mode + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(Sports("SportsEventSession")); Assert.That(match, Is.Not.Null);
        Call(match, "AllBots"); Set(Get(match, "setup"), "count", mode == "TruckBuzkashi" ? 6 : 4); Call(match, "NormalizeSetup");
    }
    [TearDown] public void Cleanup()
    {
        if (match != null) Call(match, "SetupScreen");
        Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
        Registry("ForgetCache"); lease?.Dispose(); lease = null;
    }
    static Component[] Trucks(Component session)
    {
        var field = session.GetType().GetField("trucks");
        if (field != null) return ((Array)field.GetValue(session)).Cast<Component>().ToArray();
        var actors = (Array)Get(session, session.GetType().Name == "FishingMatch" ? "streams" : "dancers");
        return actors.Cast<object>().Select(actor => (Component)Get(actor, "truck")).ToArray();
    }
    static string[] AssignProfiles(Component session, int count)
    {
        var ids = new string[count];
        for (int i = 0; i < count; i++)
        {
            ids[i] = (string)Get(Registry("Create", i < 2 ? "Same Driver" : "Named AI " + i), "id");
            Assert.That(Registry("UseInSeat", i, ids[i]), Is.True);
        }
        Call(session, "RefreshParticipants", false); return ids;
    }
    static void Save(string id, string body, params string[] selections)
    {
        var draft = Registry("GetAppearance", id); Set(draft, "bodyId", body); ((IList)Get(draft, "selections")).Clear();
        for (int i = 0; i < selections.Length; i += 2)
        {
            var item = Activator.CreateInstance(Sports("SportsTruckCosmeticSelection")); Set(item, "slotId", selections[i]); Set(item, "itemId", selections[i + 1]); ((IList)Get(draft, "selections")).Add(item);
        }
        Assert.That(Registry("TrySetAppearance", id, draft), Is.True, "Each test save changes the detached draft.");
    }
    static string Physics(Component truck)
    {
        var body = (Rigidbody)Get(truck, "body");
        string value = body.GetInstanceID() + ":" + body.mass.ToString("R", CultureInfo.InvariantCulture) + ":" + body.centerOfMass + ":" + body.linearDamping + ":" + body.angularDamping + ":" + truck.transform.localScale;
        foreach (var collider in truck.GetComponentsInChildren<Collider>(true))
        {
            value += "/" + collider.GetInstanceID() + ":" + collider.GetType().Name + ":" + collider.isTrigger;
            if (collider is BoxCollider box) value += ":" + box.center + ":" + box.size;
            if (collider is MeshCollider mesh) value += ":" + (mesh.sharedMesh != null ? mesh.sharedMesh.GetInstanceID() : 0) + ":" + mesh.convex;
        }
        foreach (var wheel in (IEnumerable)Get(truck, "wheels"))
        {
            var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring");
            value += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "width") + ":" + Get(data, "mass") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce");
        }
        return value;
    }
    static GameObject Model(object host)
    {
        var model = (GameObject)Get(host, "model"); Assert.That(model, Is.Not.Null);
        Assert.That(model.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(model.GetComponentsInChildren<Camera>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<AudioListener>(true), Is.Empty);
        return model;
    }
    static string Equipment(object host)
    {
        string value = "";
        foreach (var root in (Transform[])Get(host, "protectedRoots"))
        {
            Assert.That(root, Is.Not.Null);
            value += root.GetInstanceID() + ":" + (root.parent != null ? root.parent.GetInstanceID() : 0) + ":" + root.position + ":" + root.rotation + ":" + root.localScale + ":" + root.gameObject.activeSelf + "/";
        }
        return value;
    }
    IEnumerator Exercise(string mode)
    {
        yield return Load(mode);
        var trucks = Trucks(match); var hosts = trucks.Select(truck => Get(truck, "appearanceHost")).ToArray();
        Assert.That(hosts.All(host => host != null), Is.True, "Run BindMissingCleanModelHosts after catalog authoring.");
        Assert.That(((Array)Get(match, "appearanceHosts")).Length, Is.EqualTo(trucks.Length));
        var ids = AssignProfiles(match, trucks.Length); Assert.That(ids[0], Is.Not.EqualTo(ids[1]));
        for (int i = 0; i < hosts.Length; i++)
        {
            Assert.That(Get(hosts[i], "participantSlot"), Is.EqualTo(i));
            var presentation = (Transform)Get(hosts[i], "presentationParent");
            Assert.That(presentation, Is.Not.SameAs(trucks[i].transform)); Assert.That(presentation.IsChildOf(trucks[i].transform), Is.True);
        }
        var physics = trucks.Select(Physics).ToArray();
        var framings = ((Array)Get(match, "appearanceFraming")).Cast<Component>().ToArray();
        if (mode == "TruckBuzkashi") Assert.That(framings, Is.Not.Empty, "Existing shared camera is an external scene root and needs its explicit appearance cache binding.");
        foreach (var framing in framings) Call(framing, "TruckBounds", 0); // Warm the original-body cache before replacement.
        string[] bodies = { "truck.monster", "truck.lifted", "semi.lowrider", "truck.fire", "novelty.hotdog", "novelty.icecream", "semi.standard" };
        for (int n = 0; n < bodies.Length; n++)
        {
            Save(ids[0], bodies[n], "hat", "hat.crown");
            if (n == 0) for (int i = 1; i < ids.Length; i++) Save(ids[i], "semi.lowrider", "wheels", "wheels.gold");
            var protectedBefore = hosts.Select(Equipment).ToArray(); Call(match, "RefreshParticipants", false); Call(match, "PrepareAppearances");
            CollectionAssert.AreEqual(protectedBefore, hosts.Select(Equipment).ToArray(), "Preparing appearance may not move, hide or rescale sport equipment.");
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false;
            for (int i = 0; i < hosts.Length; i++)
            {
                Assert.That(Get(hosts[i], "ProfileId"), Is.EqualTo(ids[i]));
                Assert.That(Get(hosts[i], "EffectiveBodyId"), Is.EqualTo(i == 0 ? bodies[n] : "semi.lowrider"));
                Model(hosts[i]); Assert.That(Physics(trucks[i]), Is.EqualTo(physics[i]), "NWH/collider configuration belongs to the original sport.");
                Assert.That(((Renderer[])Get(hosts[i], "originalBodyRenderers")).All(renderer => !renderer.enabled), Is.True);
            }
            foreach (var framing in framings)
            {
                Call(framing, "TruckBounds", 0);
                var cache = (Renderer[][])Get(framing, "renderers");
                var live = Model(hosts[0]).GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
                Assert.That(live.All(renderer => cache[0].Contains(renderer)), Is.True, "Warm broadcast cache must include current body and accessories after rematch.");
            }
            if (n == 0)
            {
                var graphics = Model(hosts[0]).GetComponentsInChildren<Transform>(true).Where(part => part.name.StartsWith("NWH body wheel ", StringComparison.Ordinal)).ToArray();
                Assert.That(graphics.Length, Is.EqualTo(4), "Monster default tires must be the catalog's recognizable body wheels, not the old semi wheels.");
                var source = (Transform)Get(Get(((Array)Get(trucks[0], "wheels")).GetValue(0), "wheel"), "rotatingContainer");
                var graphic = graphics.Single(part => part.name == "NWH body wheel 0"); var original = source.rotation;
                var offset = Quaternion.Inverse(source.rotation) * graphic.rotation;
                source.rotation *= Quaternion.Euler(47, 0, 0); Call(hosts[0], "LateUpdate");
                Assert.That(Quaternion.Angle(graphic.rotation, source.rotation * offset), Is.LessThan(.01f), "Synthetic pose confirms the existing NWH visual container supplies steering/spin.");
                source.rotation = original; Call(hosts[0], "LateUpdate");
            }
            yield return null; yield return null;
        }
        int revision = (int)Get(hosts[0], "AppearanceRevision"); Assert.That(Call(match, "StartMatch"), Is.True);
        Assert.That(Get(hosts[0], "AppearanceRevision"), Is.EqualTo(revision), "An unchanged rematch reuses composition.");
        Registry("Rename", ids[0], "Renamed Driver", Color.magenta, true);
        Save(ids[0], "missing.saved.body", "hat", "missing.saved.hat"); Assert.That(Call(match, "StartMatch"), Is.True);
        Assert.That(Get(hosts[0], "ProfileId"), Is.EqualTo(ids[0])); Assert.That(Get(hosts[0], "EffectiveBodyId"), Is.EqualTo("semi.standard"));
        Assert.That(Get(Registry("GetAppearance", ids[0]), "bodyId"), Is.EqualTo("missing.saved.body"));
        Assert.That(Get(Registry("GetAppearance", ids[1]), "bodyId"), Is.EqualTo("semi.lowrider"));
        Assert.That((string)Get(hosts[0], "CompatibilityNotice"), Is.Not.Empty);
        if (mode == "TruckDanceOff")
        {
            var rig = ((Array)Get(match, "dancers")).GetValue(0); var grade = Type.GetType("LWS.TruckDanceOff.DanceGrade, LWS.TruckDanceOff.Rules", true);
            var body = (Rigidbody)Get(trucks[0], "body"); var before = body.position;
            Call(rig, "Hit", Enum.Parse(grade, "Perfect"), 24, 1, 0d, .5d, true);
            yield return new WaitForSeconds(.2f); Call(rig, "Pose", .25d); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(Vector3.Distance(body.position, before), Is.GreaterThan(.05f), "Original Dance choreography still moves its authoritative kinematic body.");
            Assert.That(Model(hosts[0]).transform.IsChildOf(trucks[0].transform), Is.True); Assert.That(Physics(trucks[0]), Is.EqualTo(physics[0]));
        }
        TestContext.WriteLine("SAVED_CLEAN_APPEARANCE " + mode + " actors=" + trucks.Length + " all7 bodies, same-name profiles/AI, missing IDs, unchanged NWH/colliders/equipment, rematch cache checked; no complete-match/art acceptance claimed.");
    }
    [UnityTest, Timeout(180000)] public IEnumerator BuzkashiRealModelsPreserveCargoAndWarmFramingAcrossRematches() { yield return Exercise("TruckBuzkashi"); }
    [UnityTest, Timeout(180000)] public IEnumerator FishingRealModelsPreserveNetsStreamsAndWaterAcrossRematches() { yield return Exercise("TruckFishing"); }
    [UnityTest, Timeout(180000)] public IEnumerator DanceRealModelsPreserveStageAndKinematicChoreographyAcrossRematches() { yield return Exercise("TruckDanceOff"); }

    [UnityTest, Timeout(120000)]
    public IEnumerator BuzkashiCabOwnerChangeRefreshesDashboardAndRestoresSyntheticRenderHiding()
    {
        yield return Load("TruckBuzkashi"); var trucks = Trucks(match); var ids = AssignProfiles(match, 6);
        Save(ids[2], "truck.monster", "dashboard", "dashboard.cat"); Save(ids[5], "semi.lowrider", "dashboard", "dashboard.wolf");
        Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false;
        var players = (Array)Get(Get(match, "setup"), "players"); var sourceType = Get(players.GetValue(2), "source").GetType();
        Set(players.GetValue(2), "source", Enum.Parse(sourceType, "Keyboard")); Set(players.GetValue(5), "source", Enum.Parse(sourceType, "Keyboard"));
        Sports("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { false }); Set(match, "option", 1);
        var cab = Get(match, "cab"); Call(cab, "Layout"); yield return null;
        CollectionAssert.AreEqual(new[] { 2, 5 }, ((int[])Get(cab, "owners")).Take(2));
        var interiors = (Transform[])Get(cab, "interiors"); var views = (Array)Get(cab, "views");
        var firstFigure = interiors[0].GetComponentsInChildren<Transform>(true).Single(part => part.name == "Profile dashboard — dashboard.cat");
        var secondFigure = interiors[1].GetComponentsInChildren<Transform>(true).Single(part => part.name == "Profile dashboard — dashboard.wolf");
        Assert.That(firstFigure.gameObject.layer, Is.EqualTo(interiors[0].gameObject.layer));
        var camera = (Camera)Get(views.GetValue(0), "view"); var other = (Camera)Get(views.GetValue(1), "view");
        Assert.That(camera.cullingMask & (1 << firstFigure.gameObject.layer), Is.Not.Zero); Assert.That(other.cullingMask & (1 << firstFigure.gameObject.layer), Is.Zero);
        var visibility = ((Component)views.GetValue(0)).GetComponent(Type.GetType("LWS.TruckBowling.BowlingCabCamera, Assembly-CSharp", true));
        var exterior = (Renderer[])Get(visibility, "exterior"); var live = Model(Get(trucks[2], "appearanceHost")).GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
        Assert.That(live.All(renderer => exterior.Contains(renderer)), Is.True);
        Assert.That(((Renderer[])Get(visibility, "interior")).Contains(firstFigure.GetComponentInChildren<Renderer>()), Is.True, "The existing cab mask must know about the new dashboard.");
        Call(visibility, "Begin", default(ScriptableRenderContext), camera);
        Assert.That(live.All(renderer => renderer.forceRenderingOff), Is.True);
        Assert.That(firstFigure.GetComponentsInChildren<Renderer>().All(renderer => !renderer.forceRenderingOff), Is.True);
        Set(players.GetValue(2), "source", Enum.Parse(sourceType, "AI")); Call(cab, "Layout");
        Assert.That(live.All(renderer => !renderer.forceRenderingOff), Is.True, "Changing owner restores all prior-camera temporary hiding before replacing references.");
        Assert.That(((int[])Get(cab, "owners"))[0], Is.EqualTo(5));
        var replacement = interiors[0].GetComponentsInChildren<Transform>(true).Single(part => part.name == "Profile dashboard — dashboard.wolf" && part.gameObject.activeSelf);
        Assert.That(replacement.gameObject.layer, Is.EqualTo(interiors[0].gameObject.layer)); Assert.That(firstFigure.gameObject.activeSelf, Is.False);
        var ownModel = Model(Get(trucks[5], "appearanceHost"));
        Assert.That(((Renderer[])Get(visibility, "exterior")).Contains(ownModel.GetComponentsInChildren<Renderer>().First(renderer => renderer.enabled)), Is.True);
        Assert.That(secondFigure.gameObject.activeSelf, Is.False, "Retired second view clears its previous dashboard immediately.");
        TestContext.WriteLine("SYNTHETIC_BUZ_CAB owners2/5→5, existing layer/mask isolation, dashboard cache and old-exterior forceRenderingOff restoration checked. Actual cab pose/input authority retained.");
    }
}
#endif
