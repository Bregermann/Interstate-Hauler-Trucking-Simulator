#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Saved-venue integration plus explicitly synthetic ownership/visibility and
// pose checks. Does not claim a complete match, art approval or physical input.
public sealed class TruckAppearanceGameplayIntegrationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static Type Bowling(string name) => Type.GetType("LWS.TruckBowling." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string member) => owner.GetType().GetField(member, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(member, Any).GetValue(owner);
    static void Set(object owner, string member, object value) => owner.GetType().GetField(member, Any).SetValue(owner, value);
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethods(Any).Single(candidate => candidate.Name == method && candidate.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string method, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(method).Invoke(null, args);
    Component match;
    GameObject isolation;
    IDisposable lease;
    IEnumerator Load(bool parking)
    {
        lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "AppearanceGameplay" + Guid.NewGuid().ToString("N") });
        Registry("ForgetCache"); isolation = new GameObject("Explicit appearance integration fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode(parking ? "Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity" : "Assets/TruckBowling/Scenes/TruckBowling.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(parking ? Sports("SportsEventSession") : Bowling("BowlingMatch"));
        Assert.That(match, Is.Not.Null);
    }
    [TearDown] public void Cleanup()
    {
        if (match != null)
        {
            if (match.GetType().GetMethod("SetupScreen") != null) Call(match, "SetupScreen");
            else { foreach (var lane in (IEnumerable)Get(match, "lanes")) Call(Get(lane, "truck"), "Clear"); ((Behaviour)match).enabled = false; }
        }
        Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
        Registry("ForgetCache"); lease?.Dispose(); lease = null;
    }
    string Profile(int seat, string body, params string[] selections)
    {
        string id = (string)Get(Registry("Create", "Same Driver"), "id"); Registry("UseInSeat", seat, id);
        Save(id, body, selections); return id;
    }
    static void Save(string id, string body, params string[] selections)
    {
        var draft = Registry("GetAppearance", id); Set(draft, "bodyId", body); ((IList)Get(draft, "selections")).Clear();
        for (int i = 0; i < selections.Length; i += 2)
        {
            var selection = Activator.CreateInstance(Sports("SportsTruckCosmeticSelection")); Set(selection, "slotId", selections[i]); Set(selection, "itemId", selections[i + 1]); ((IList)Get(draft, "selections")).Add(selection);
        }
        Assert.That(Registry("TrySetAppearance", id, draft), Is.True);
    }
    static string Physics(Component truck)
    {
        var body = truck.GetComponent<Rigidbody>(); var text = body.mass.ToString("R", CultureInfo.InvariantCulture) + "/" + body.centerOfMass + "/" + body.linearDamping + "/" + body.angularDamping;
        foreach (var collider in truck.GetComponentsInChildren<Collider>(true))
        {
            text += "/" + collider.GetInstanceID() + ":" + collider.GetType().Name;
            if (collider is BoxCollider box) text += box.center + ":" + box.size;
        }
        foreach (var wheel in (IEnumerable)Get(truck, "wheels"))
        { var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring"); text += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "width") + ":" + Get(data, "mass") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce"); }
        return text;
    }
    static GameObject Model(object host)
    {
        var result = (GameObject)Get(host, "model"); Assert.That(result, Is.Not.Null);
        Assert.That(result.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(result.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(result.GetComponentsInChildren<AudioListener>(true), Is.Empty); Assert.That(result.GetComponentsInChildren<Camera>(true), Is.Empty);
        return result;
    }
    static string[] Topology(Transform root) => root.GetComponentsInChildren<Transform>(true).Select(part => part == root ? "ROOT" : part.name).ToArray();

    [UnityTest, Timeout(120000)]
    public IEnumerator SavedParkingUsesProfileIdsRealBodyWheelsRematchAndCameraIsolationWithoutPhysicsChanges()
    {
        yield return Load(true); Call(match, "AllBots");
        string first = Profile(0, "truck.monster", "hat", "hat.crown", "dashboard", "dashboard.cat");
        string second = Profile(1, "semi.lowrider", "wheels", "wheels.gold");
        var trucks = ((Array)Get(match, "trucks")).Cast<Component>().ToArray();
        var host = Get(trucks[0], "appearanceHost"); Assert.That(host, Is.Not.Null, "Run explicit BindMissingHosts after content authoring.");
        string physics = Physics(trucks[0]); Call(match, "RefreshParticipants", false);
        Sports("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { true });
        Assert.That(Call(match, "StartMatch"), Is.True);
        Assert.That(Get(host, "ProfileId"), Is.EqualTo(first)); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo("truck.monster"));
        Assert.That(Get(Get(trucks[1], "appearanceHost"), "ProfileId"), Is.EqualTo(second)); Assert.That(Physics(trucks[0]), Is.EqualTo(physics));
        var model = Model(host); var defaults = model.GetComponentsInChildren<Transform>(true).Where(part => part.name.StartsWith("NWH body wheel ", StringComparison.Ordinal)).ToArray();
        Assert.That(defaults.Length, Is.EqualTo(4), "The Monster keeps its own authored tire/rim geometry as NWH followers.");
        var catalogBody = Call(Get(host, "catalog"), "FindBody", "truck.monster");
        var meshes = ((GameObject)Get(catalogBody, "visualPrefab")).GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray();
        Assert.That(defaults.SelectMany(part => part.GetComponentsInChildren<MeshFilter>()).All(filter => meshes.Contains(filter.sharedMesh)), Is.True);
        var views = trucks[0].gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).Single(component => component != null && component.GetType() == Sports("SportsDrivingViews"));
        Call(views, "Select", 2); yield return null; yield return null;
        var rally = Get(views, "rally"); var cameras = (Array)Get(rally, "views"); var camera = (Camera)Get(cameras.GetValue(0), "view");
        var own = ((Renderer[])Get(host, "ExteriorRenderers")).Where(renderer => renderer.enabled).ToArray();
        Assert.That(own, Is.Not.Empty); Call(views, "BeginCamera", default(ScriptableRenderContext), camera);
        Assert.That(own.All(renderer => renderer.forceRenderingOff), Is.True);
        Assert.That(((Renderer[])Get(Get(trucks[1], "appearanceHost"), "ExteriorRenderers")).Where(renderer => renderer.enabled).All(renderer => !renderer.forceRenderingOff), Is.True);
        Call(views, "RefreshAppearanceExteriors"); Assert.That(own.All(renderer => !renderer.forceRenderingOff), Is.True, "Rebinding restores temporary own-cab hiding.");
        var cabs = ((Transform[])Get(views, "cabs"));
        var figure = cabs[0].GetComponentsInChildren<Transform>(true).Single(part => part.name == "Profile dashboard — dashboard.cat");
        Assert.That(figure.gameObject.layer, Is.EqualTo(cabs[0].gameObject.layer));
        Assert.That((camera.cullingMask & (1 << figure.gameObject.layer)) != 0, Is.True);
        var otherCamera = (Camera)Get(cameras.GetValue(1), "view"); Assert.That((otherCamera.cullingMask & (1 << figure.gameObject.layer)), Is.Zero);
        int revision = (int)Get(host, "AppearanceRevision"); Call(match, "StartMatch"); Assert.That(Get(host, "AppearanceRevision"), Is.EqualTo(revision), "Unchanged rematch must reuse rendering.");
        Save(first, "novelty.icecream", "hat", "missing.saved.hat"); Registry("Rename", first, "Renamed Driver", Color.magenta, true);
        Assert.That(Call(match, "StartMatch"), Is.True); Assert.That(Get(host, "ProfileId"), Is.EqualTo(first)); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo("novelty.icecream"));
        Assert.That(Get(host, "AppearanceRevision"), Is.EqualTo(revision + 1)); Assert.That(Physics(trucks[0]), Is.EqualTo(physics)); Model(host);
        Assert.That(((IList)Get(Registry("GetAppearance", first), "selections")).Count, Is.EqualTo(1));
        Save(first, "missing.saved.body", "wheels", "missing.saved.wheel"); Assert.That(Call(match, "StartMatch"), Is.True);
        Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo("semi.standard")); Assert.That(Get(Registry("GetAppearance", first), "bodyId"), Is.EqualTo("missing.saved.body"));
        TestContext.WriteLine("SAVED_PARKING_APPEARANCE stable IDs, real render-only bodies/default wheel meshes, rematch changes, missing-ID retention, unchanged physics and synthetic own-cab isolation checked. No complete-match/art/device claim.");
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator SavedBowlingHotSeatChangesActualLaneAppearanceAndRebuildsReplayKeepingRootAndOriginalReferences()
    {
        yield return Load(false);
        string first = Profile(0, "truck.monster", "dashboard", "dashboard.wolf"); string second = Profile(1, "semi.lowrider", "wheels", "wheels.ruby");
        Call(match, "RefreshParticipants", false); Call(match, "PresetAI", 2, Enum.Parse(Bowling("BowlingMode"), "HotSeat"));
        var lane = ((Array)Get(match, "lanes")).GetValue(0); var truck = (Component)Get(lane, "truck"); var host = Get(truck, "appearanceHost");
        Assert.That(host, Is.Not.Null, "Run explicit BindMissingHosts after content authoring.");
        var replay = Get(Get(lane, "highlights"), "replay"); var ghost = ((Transform[])Get(replay, "ghosts"))[0]; var originalChild = ghost.GetChild(0);
        string physics = Physics(truck); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false;
        Assert.That(Get(host, "ProfileId"), Is.EqualTo(first)); Assert.That(Physics(truck), Is.EqualTo(physics)); Model(host);
        CollectionAssert.AreEqual(Topology(truck.transform), Topology(ghost)); Assert.That(((Transform[])Get(replay, "ghosts"))[0], Is.SameAs(ghost)); Assert.That(ghost.GetChild(0), Is.SameAs(originalChild));
        Call(replay, "Record", .1f, 2047); Call(replay, "Record", .2f, 2047); Call(replay, "Begin", 0f, float.PositiveInfinity);
        Assert.That(Get(replay, "Playing"), Is.True); Assert.That(Call(replay, "RebuildGhostHierarchy", 0), Is.False); Assert.That(Call(replay, "InvalidateFreshRecord"), Is.False);
        Call(replay, "End"); int revision = (int)Get(host, "AppearanceRevision");
        Call(lane, "StartLane", 1, true); Assert.That(Get(host, "ProfileId"), Is.EqualTo(second)); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo("semi.lowrider"));
        Assert.That(Get(host, "AppearanceRevision"), Is.EqualTo(revision + 1)); Assert.That(Physics(truck), Is.EqualTo(physics));
        CollectionAssert.AreEqual(Topology(truck.transform), Topology(ghost)); Assert.That(((Transform[])Get(replay, "ghosts"))[0], Is.SameAs(ghost)); Assert.That(ghost.GetChild(0), Is.SameAs(originalChild));
        Assert.That(((Renderer[])Get(Get(lane, "cab"), "exterior")).Contains(Model(host).GetComponentsInChildren<Renderer>().First(renderer => renderer.enabled)), Is.True);
        var wheelGraphic = Model(host).GetComponentsInChildren<Transform>().Single(part => part.name == "NWH appearance wheel 0");
        var wheel = ((Array)Get(truck, "wheels")).GetValue(0); var source = (Transform)Get(Get(wheel, "wheel"), "rotatingContainer");
        var offset = Quaternion.Inverse(source.rotation) * wheelGraphic.rotation;
        // Explicit synthetic controller-pose update proves the adapter consumes
        // existing visual motion. It does not pretend to be physical driving.
        source.rotation *= Quaternion.Euler(39, 0, 0); Call(host, "LateUpdate");
        Assert.That(Quaternion.Angle(wheelGraphic.rotation, source.rotation * offset), Is.LessThan(.01f)); Assert.That(Vector3.Distance(wheelGraphic.position, source.position), Is.LessThan(.001f));
        Call(replay, "Record", .1f, 2047); Call(replay, "Record", .2f, 2047); Call(replay, "Begin", 0f, float.PositiveInfinity); Assert.That(Get(replay, "Playing"), Is.True); Call(replay, "End");
        revision = (int)Get(host, "AppearanceRevision"); Call(lane, "Continue"); Assert.That(Get(host, "AppearanceRevision"), Is.EqualTo(revision));
        Assert.That(Get(Registry("GetAppearance", first), "bodyId"), Is.EqualTo("truck.monster"));
        TestContext.WriteLine("SAVED_BOWLING_APPEARANCE hot-seat owner binding, actual source/ghost topology, preserved root/original-child references, playing rejection, camera exterior cache and synthetic NWH visual-pose following checked; no pin score fabricated.");
    }

    [Test]
    public void AuditedPaintSlotsExcludeGlassChromeInteriorAndCombinedAtlases()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); Assert.That(shader, Is.Not.Null);
        var material = new Material(shader);
        try
        {
            var select = Sports("SportsTruckAppearanceCompatibility").GetMethod("IsPaintMaterial");
            material.name = "010416TRUK-bodycolor.003"; Assert.That(select.Invoke(null, new object[] { "semi.standard", material }), Is.True);
            foreach (string name in new[] { "010416TRUK-windows", "010416TRUK-trims", "010416TRUK-interior", "010416TRUK-wheelrims", "Fire_truck_1_Body_D", "MiniBus_Commercial" })
            { material.name = name; Assert.That(select.Invoke(null, new object[] { "truck.fire", material }), Is.False, name); }
            material.name = "Body"; Assert.That(select.Invoke(null, new object[] { "truck.monster", material }), Is.True); Assert.That(select.Invoke(null, new object[] { "semi.standard", material }), Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(material); }
    }
}
#endif
