#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class LaunchFourAppearanceIntegrationTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object target, string name) => target.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(target) : target.GetType().GetProperty(name, Any).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Any).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(target, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    static void Save(string id, string body)
    {
        var draft = Registry("GetAppearance", id); Set(draft, "bodyId", body);
        var list = (IList)Get(draft, "selections"); list.Clear();
        foreach (var pair in new[] { new[] { "hat", "hat.crown" }, new[] { "face.eyes", "eyes.friendly" }, new[] { "mirrors", "mirrors.chrome" }, new[] { "wheels", "wheels.gold" }, new[] { "rear.ornament", "rear.chrome" }, new[] { "dashboard", "dashboard.cat" } })
        { var item = Activator.CreateInstance(Sports("SportsTruckCosmeticSelection")); Set(item, "slotId", pair[0]); Set(item, "itemId", pair[1]); list.Add(item); }
        bool changed = (bool)Registry("TrySetAppearance", id, draft); var normalizeArgs = new object[] { draft, false }; var normalized = Sports("SportsTruckAppearanceData").GetMethod("Normalize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, normalizeArgs); Assert.That((bool)Call(Registry("GetAppearance", id), "SameAs", normalized), Is.True, "Saved semantic appearance must exactly match the registry-normalized draft; changed=" + changed);
    }
    sealed class Fixture : IDisposable
    {
        readonly IDisposable prefs; readonly GameObject isolation;
        public Behaviour match; public Component[] trucks; public string[] ids; public string mode;
        public Fixture() { prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "LaunchFourAppearance" + Guid.NewGuid().ToString("N") }); Registry("ForgetCache"); isolation = new GameObject("Launch appearance original AI input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); }
        public IEnumerator Load(string scene)
        {
            mode = scene; EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Sports("SportsEventSession"), FindObjectsSortMode.None).Cast<Behaviour>().Single();
            Set(Get(match, "setup"), "count", scene == "TruckSkyTow" ? 4 : 2); if (scene == "TruckBeerPong") { Set(match, "option", 0); Set(match, "teamA", 1); Set(match, "teamB", 1); }
            Call(match, "NormalizeSetup"); Call(match, "AllBots");
            trucks = scene == "TruckSkyTow" ? ((Array)Get(match, "racers")).Cast<object>().Select(racer => (Component)Get(racer, "truck")).ToArray() : new[] { (Component)Get(match, "truck") };
            Assert.That(trucks.All(truck => Get(truck, "appearanceHost") != null), Is.True, "Run InstallOpenLaunchFourAppearance on this original venue, then save explicitly.");
            int count = (int)Get(Get(match, "setup"), "count"); ids = new string[count];
            for (int i = 0; i < count; i++) { ids[i] = (string)Get(Registry("Create", "Same Named Driver"), "id"); Assert.That((bool)Registry("UseInSeat", i, ids[i]), Is.True); Save(ids[i], i == 0 ? "truck.monster" : "novelty.icecream"); }
            Assert.That(ids.Distinct().Count(), Is.EqualTo(count), "Duplicate display names must retain distinct persistent owners.");
            Call(match, "RefreshParticipants", false); Call(match, "AllBots");
        }
        public object Host(int i = 0) => Get(trucks[i], "appearanceHost");
        public object Snow => UnityEngine.Object.FindObjectsByType(Sports("SportsSnowSurface"), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().Single(component => component.gameObject.scene == match.gameObject.scene);
        public void SelectWeather(bool snow)
        {
            var weather = UnityEngine.Object.FindObjectsByType(Sports("SportsChaosWeather"), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().FirstOrDefault(component => component.gameObject.scene == match.gameObject.scene);
            if (weather == null) { Assert.That(mode == "TruckBeerPong" && !snow, Is.True, "Only the original fixed-clear Pong venue may omit a weather component."); return; }
            var presets = Get(weather, "presets");
            if (presets == null && match.GetType().GetField(mode == "TruckSkyTow" ? "presets" : "weatherPresets", Any) is FieldInfo field) presets = field.GetValue(match);
            Assert.That(presets, Is.Not.Null);
            var entry = ((Array)Get(presets, "entries")).Cast<object>().First(item => snow ? (bool)Get(item, "Snow") : (string)Get(item, "displayName") == "Clear"); Call(presets, "Select", Get(entry, "id"));
        }
        public object Replay => mode == "TruckSkyTow" ? Get(Get(match, "review"), "motion") : Get(match, "replay");
        public int Current => mode == "TruckFieldGoal" ? (int)Get(Get(match, "rules"), "Current") : mode == "TruckGiantSwing" ? (int)Get(match, "Current") : (int)Get(match, "Current");
        public void Start() { Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True); }
        public void Dispose() { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); Registry("ForgetCache"); prefs.Dispose(); }
    }
    static string Physics(Component truck)
    {
        var body = (Rigidbody)Get(truck, "body"); string result = body.GetInstanceID() + ":" + body.mass.ToString("R", CultureInfo.InvariantCulture) + ":" + body.centerOfMass + ":" + body.linearDamping + ":" + body.angularDamping + ":" + truck.transform.localScale;
        foreach (var collider in truck.GetComponentsInChildren<Collider>(true)) { result += "/" + collider.GetInstanceID() + ":" + collider.isTrigger; if (collider is BoxCollider box) result += ":" + box.center + ":" + box.size; if (collider is MeshCollider mesh) result += ":" + (mesh.sharedMesh != null ? mesh.sharedMesh.GetInstanceID() : 0) + ":" + mesh.convex; }
        foreach (var wheel in (IEnumerable)Get(truck, "wheels")) { var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring"); result += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "width") + ":" + Get(data, "mass") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce"); }
        foreach (var joint in truck.GetComponents<Joint>()) result += "/joint:" + joint.GetInstanceID() + ":" + joint.anchor + ":" + joint.connectedAnchor;
        return result;
    }
    static string Equipment(object host) => string.Join("/", ((Transform[])Get(host, "protectedRoots")).Select(root => root.GetInstanceID() + ":" + root.localPosition + ":" + root.localRotation + ":" + root.localScale + ":" + root.gameObject.activeSelf + ":" + string.Join(",", root.GetComponentsInChildren<Renderer>(true).Select(renderer => renderer.GetInstanceID() + ":" + renderer.enabled))));
    static void Applied(object host, string id, string body)
    {
        Assert.That(Get(host, "ProfileId"), Is.EqualTo(id)); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo(body));
        var model = (Transform)Get(host, "AppearanceRoot"); Assert.That(model, Is.Not.Null);
        Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<Camera>(true), Is.Empty); Assert.That(model.GetComponentsInChildren<AudioListener>(true), Is.Empty);
        Assert.That(((Renderer[])Get(host, "originalBodyRenderers")).All(renderer => !renderer.enabled), Is.True);
        Assert.That((string)Get(host, "CompatibilityNotice"), Does.Contain("no dashboard view"));
        Assert.That(model.GetComponentsInChildren<Transform>(true).Count(part => part.name == "hat mount"), Is.EqualTo(1));
    }
    static void SameReplayAppearance(object replay, int index)
    {
        var source = ((Transform[])Get(replay, "sources"))[index]; var ghost = ((Transform[])Get(replay, "ghosts"))[index];
        var live = source.GetComponentsInChildren<Transform>(true); var copy = ghost.GetComponentsInChildren<Transform>(true);
        Assert.That(copy.Length, Is.EqualTo(live.Length), "Actual current body/accessory topology must be represented exactly.");
        for (int i = 1; i < live.Length; i++)
        {
            Assert.That(copy[i].name, Is.EqualTo(live[i].name)); var a = live[i].GetComponent<Renderer>(); var b = copy[i].GetComponent<Renderer>();
            if (a == null) continue; Assert.That(b, Is.Not.Null); Assert.That(b.enabled, Is.EqualTo(a.enabled)); CollectionAssert.AreEqual(a.sharedMaterials, b.sharedMaterials);
            var pa = new MaterialPropertyBlock(); var pb = new MaterialPropertyBlock(); a.GetPropertyBlock(pa); b.GetPropertyBlock(pb); Assert.That(pb.GetColor("_BaseColor"), Is.EqualTo(pa.GetColor("_BaseColor")));
        }
        Assert.That(ghost.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(ghost.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator SevenBodiesAcrossFourOriginalLaunchVenuesKeepPhysicsEquipmentProfilesAndGhostRoots()
    {
        foreach (string mode in new[] { "TruckSkyTow", "TruckGiantSwing", "TruckFieldGoal", "TruckBeerPong" }) using (var f = new Fixture())
        {
            yield return f.Load(mode); f.SelectWeather(false); string[] bodies = { "truck.monster", "truck.lifted", "semi.lowrider", "truck.fire", "novelty.hotdog", "novelty.icecream", "semi.standard" };
            int[] roots = ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray();
            foreach (string body in bodies)
            {
                Save(f.ids[0], body); Call(f.match, "RefreshParticipants", false);
                var beforeStartPhysics = f.trucks.Select(Physics).ToArray();
                var beforeEquipment = f.trucks.Select((truck, i) => Equipment(f.Host(i))).ToArray();
                var cameraIds = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(camera => camera.GetInstanceID()).OrderBy(id => id).ToArray();
                var listenerIds = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(listener => listener.GetInstanceID()).OrderBy(id => id).ToArray();
                // Isolate actual different-body composition from original Power /
                // NWH activation and pendulum joint creation at match start.
                for (int i = 0; i < f.trucks.Length; i++) Call(f.Host(i), "Prepare", f.ids[i], (Color)Get(((Array)Get(Get(f.match, "setup"), "players")).GetValue(i), "color"));
                CollectionAssert.AreEqual(beforeStartPhysics, f.trucks.Select(Physics).ToArray(), "Changing actual body/cosmetics retains original physical configuration.");
                CollectionAssert.AreEqual(beforeEquipment, f.trucks.Select((truck, i) => Equipment(f.Host(i))).ToArray());
                CollectionAssert.AreEqual(cameraIds, UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(camera => camera.GetInstanceID()).OrderBy(id => id).ToArray());
                CollectionAssert.AreEqual(listenerIds, UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(listener => listener.GetInstanceID()).OrderBy(id => id).ToArray());
                f.Start(); f.match.enabled = false; Time.timeScale = 0;
                if (mode == "TruckSkyTow")
                {
                    // Controlled appearance case: original countdown stays held,
                    // while real weather batches settle through their normal clock.
                    Time.timeScale = 1; yield return null; yield return null;
                    float readyBy = Time.realtimeSinceStartup + 30;
                    while (!(bool)Get(Get(f.Snow, "surface"), "EnvironmentBindingsReady") && Time.realtimeSinceStartup < readyBy) yield return null;
                    Assert.That((bool)Get(Get(f.Snow, "surface"), "EnvironmentBindingsReady"), Is.True);
                    Call(Get(f.match, "review"), "Record", (float)Get(f.match, "Age")); Time.timeScale = 0;
                }
                var physics = f.trucks.Select(Physics).ToArray(); var equipment = f.trucks.Select((truck, i) => Equipment(f.Host(i))).ToArray();
                int receipts = (int)Get(Get(f.match, "ScoreJournal"), "Count");
                if (mode == "TruckGiantSwing") Call(f.match, "LoadComplete");
                for (int i = 0; i < f.trucks.Length; i++)
                {
                    Applied(f.Host(i), f.ids[i], i == 0 ? body : "novelty.icecream");
                    // Isolate the rendering operation from the original Swing's
                    // LoadComplete joint/COM lifecycle before checking invariance.
                    physics[i] = Physics(f.trucks[i]); equipment[i] = Equipment(f.Host(i));
                    Call(f.Host(i), "Prepare", f.ids[i], (Color)Get(((Array)Get(Get(f.match, "setup"), "players")).GetValue(i), "color"));
                    Assert.That(Physics(f.trucks[i]), Is.EqualTo(physics[i])); Assert.That(Equipment(f.Host(i)), Is.EqualTo(equipment[i]));
                    SameReplayAppearance(f.Replay, mode == "TruckSkyTow" ? i * 2 : 0);
                }
                CollectionAssert.AreEqual(roots, ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray());
                Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.EqualTo(receipts));
                yield return null; Call(f.match, "SetupScreen"); f.match.enabled = true; Time.timeScale = 1;
            }
            Assert.That((bool)Registry("Rename", f.ids[0], "Renamed Driver", Color.cyan, true), Is.True); Registry("ForgetCache");
            Assert.That(Get(Registry("GetAppearance", f.ids[0]), "bodyId"), Is.EqualTo("semi.standard")); Call(f.match, "RefreshParticipants", false); f.Start();
            Assert.That(Get(f.Host(), "ProfileId"), Is.EqualTo(f.ids[0]));
            TestContext.WriteLine("LAUNCH_FOUR_APPEARANCE " + mode + " all7 bodies, original physical actors=" + f.trucks.Length + ", retained roots/owner IDs/profile persistence/equipment/render-only checks. Controlled appearance boundaries, not complete-match acceptance.");
        }
    }

    [UnityTest, Timeout(360000)]
    public IEnumerator RealAiAttemptsReplayAllAnglesAndPrepareNextActualTurnOwner()
    {
        foreach (string mode in new[] { "TruckGiantSwing", "TruckFieldGoal", "TruckBeerPong" }) using (var f = new Fixture())
        {
            yield return f.Load(mode); f.Start(); bool sawReplay = false; int[] ghostIds = ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray();
            float deadline = Time.realtimeSinceStartup + 110;
            while (f.Current == 0 && Time.realtimeSinceStartup < deadline)
            {
                if ((bool)Get(f.Replay, "Playing") && !sawReplay)
                {
                    sawReplay = true; SameReplayAppearance(f.Replay, 0);
                    foreach (int angle in new[] { 0, 1, 2 })
                    {
                        if (angle > 0) Call(f.match, "ExtraAction", "replayangle"); Call(f.match, "ExtraAction", "replayrewind");
                        yield return null; Assert.That((int)Get(f.Replay, "Angle"), Is.EqualTo(angle));
                        Assert.That(Get(f.Host(), "ProfileId"), Is.EqualTo(f.ids[0]));
                        var ghost = ((Transform[])Get(f.Replay, "ghosts"))[0]; Assert.That(ghost.gameObject.activeSelf, Is.True);
                        Assert.That(ghost.GetComponentsInChildren<Transform>(true).Count(part => part.name == "Sports appearance — truck.monster"), Is.EqualTo(1));
                        Assert.That(ghost.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy), Is.True);
                    }
                }
                yield return null;
            }
            Assert.That(sawReplay, Is.True, "Original AI attempt must genuinely reach its original replay."); Assert.That(f.Current, Is.EqualTo(1), "Original rules advance to the actual next owner.");
            Applied(f.Host(), f.ids[1], "novelty.icecream");
            if (mode == "TruckGiantSwing") { deadline = Time.realtimeSinceStartup + 4; while (Get(f.match, "Phase").ToString() == "Loading" && Time.realtimeSinceStartup < deadline) yield return null; }
            SameReplayAppearance(f.Replay, 0); CollectionAssert.AreEqual(ghostIds, ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray());
            TestContext.WriteLine("LAUNCH_REAL_AI_OWNER " + mode + " owner0=" + f.ids[0] + " owner1=" + f.ids[1] + " original replay observed, all3 existing angles sought; no pose/force/score/outcome injection.");
        }
    }

    [UnityTest, Timeout(390000)]
    public IEnumerator RealAiSameBodyDifferentOwnersReplaceTransformsAndRecordTheirOwnCurrentHierarchy()
    {
        foreach (string mode in new[] { "TruckGiantSwing", "TruckFieldGoal", "TruckBeerPong" }) using (var f = new Fixture())
        {
            yield return f.Load(mode); Save(f.ids[0], "novelty.icecream"); f.SelectWeather(false);
            string[] saved = f.ids.Select(id => JsonUtility.ToJson(Registry("GetAppearance", id))).ToArray();
            f.Start(); var replay = f.Replay; var source = ((Transform[])Get(replay, "sources"))[0];
            var ghostRoots = ((Transform[])Get(replay, "ghosts")).ToArray();
            var originalModel = (Transform)Get(f.Host(), "AppearanceRoot"); int originalModelId = originalModel.GetInstanceID();
            var initialTopology = source.GetComponentsInChildren<Transform>(true).Skip(1).Select(part => part.name).ToArray();
            bool sawFirstReplay = false; float deadline = Time.realtimeSinceStartup + 110;
            while (f.Current == 0 && Time.realtimeSinceStartup < deadline)
            {
                if ((bool)Get(replay, "Playing") && !sawFirstReplay)
                {
                    sawFirstReplay = true; Applied(f.Host(), f.ids[0], "novelty.icecream"); SameReplayAppearance(replay, 0);
                    foreach (int angle in new[] { 0, 1, 2 }) { if (angle > 0) Call(f.match, "ExtraAction", "replayangle"); Call(f.match, "ExtraAction", "replayrewind"); yield return null; Assert.That((int)Get(replay, "Angle"), Is.EqualTo(angle)); }
                }
                yield return null;
            }
            Assert.That(sawFirstReplay, Is.True, "The original first AI attempt must genuinely reach its replay."); Assert.That(f.Current, Is.EqualTo(1), "Original rules must naturally advance to the distinct second owner.");
            deadline = Time.realtimeSinceStartup + 8;
            while ((Get(replay, "liveParts") == null || (int)Get(replay, "count") < 4) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(f.Current, Is.EqualTo(1)); Assert.That((int)Get(replay, "count"), Is.GreaterThanOrEqualTo(4), "Second owner's native simulation must actually write several new samples.");
            Applied(f.Host(), f.ids[1], "novelty.icecream"); var currentModel = (Transform)Get(f.Host(), "AppearanceRoot");
            Assert.That(currentModel.GetInstanceID(), Is.Not.EqualTo(originalModelId)); Assert.That(originalModel == null, Is.True, "The prior profile's composed model is genuinely disposed after handoff.");
            var current = source.GetComponentsInChildren<Transform>(true);
            CollectionAssert.AreEqual(initialTopology, current.Skip(1).Select(part => part.name), "This regression must exercise equal topology, rather than relying on the changed-body rebuild branch.");
            var cached = (Transform[][])Get(replay, "liveParts");
            for (int i = 0; i < cached.Length; i++)
            {
                Assert.That(cached[i].All(part => part != null), Is.True); CollectionAssert.AreEqual(((Transform[])Get(replay, "sources"))[i].GetComponentsInChildren<Transform>(true), cached[i], "Every cached instance must belong to the fresh second-owner source.");
            }
            SameReplayAppearance(replay, 0); CollectionAssert.AreEqual(ghostRoots, (Transform[])Get(replay, "ghosts"));
            Call(f.match, "Pause", 0, false); string physical = Physics(f.trucks[0]); int receipts = (int)Get(Get(f.match, "ScoreJournal"), "Count");
            var poseBuffer = Get(replay, "roots"); var timeBuffer = Get(replay, "times"); int frames = (int)Get(replay, "count");
            // Controlled recorded-view exercise over the second owner's genuine
            // samples; this does not claim a completed second attempt or score.
            Call(replay, "Begin", 0f, float.PositiveInfinity); Assert.That((bool)Get(replay, "Playing"), Is.True);
            Call(replay, "Seek", (float)Get(replay, "Duration")); SameReplayAppearance(replay, 0); Call(replay, "End");
            Assert.That(Physics(f.trucks[0]), Is.EqualTo(physical)); Assert.That(Get(Get(f.match, "ScoreJournal"), "Count"), Is.EqualTo(receipts));
            Assert.That(Get(replay, "roots"), Is.SameAs(poseBuffer)); Assert.That(Get(replay, "times"), Is.SameAs(timeBuffer)); Assert.That((int)Get(replay, "count"), Is.EqualTo(frames));
            CollectionAssert.AreEqual(saved, f.ids.Select(id => JsonUtility.ToJson(Registry("GetAppearance", id))));
            TestContext.WriteLine("LAUNCH_SAME_BODY_NATIVE " + mode + " distinct IDs=" + string.Join(",", f.ids) + " model IDs=" + originalModelId + "->" + currentModel.GetInstanceID() + " equal-topology transforms freshly cached, genuine second-owner frames=" + frames + "; original AI advancement/first replay/physics/roots/drafts preserved.");
        }
    }

    [UnityTest, Timeout(150000)]
    public IEnumerator SkyFourOriginalOwnersRefreshRecordedModelsWithoutChangingTowOrRecordedBoatRoots()
    {
        using (var f = new Fixture())
        {
            yield return f.Load("TruckSkyTow");
            int[] ghostIds = ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray();
            foreach (bool snow in new[] { false, true })
            {
                f.SelectWeather(snow); f.Start(); var review = Get(f.match, "review"); var motion = f.Replay;
                Assert.That((int)Get(review, "WeatherMetadataRefreshes"), Is.Zero);
                var poseBuffer = Get(motion, "roots"); var partBuffer = Get(motion, "parts"); var timeBuffer = Get(motion, "times");
                float deadline = Time.realtimeSinceStartup + 35;
                while (((float)Get(f.match, "Age") < 2 || (int)Get(review, "WeatherMetadataRefreshes") != 1) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((float)Get(f.match, "Age"), Is.GreaterThanOrEqualTo(2)); Assert.That((int)Get(review, "WeatherMetadataRefreshes"), Is.EqualTo(1));
                Assert.That((bool)Get(Get(f.Snow, "surface"), "EnvironmentBindingsReady"), Is.True);
                Assert.That(Get(motion, "roots"), Is.SameAs(poseBuffer)); Assert.That(Get(motion, "parts"), Is.SameAs(partBuffer)); Assert.That(Get(motion, "times"), Is.SameAs(timeBuffer));
                for (int index = 0; index < 8; index++) SameReplayAppearance(motion, index);
                // Independently exercise the same metadata-only adapter against
                // an already populated genuine record: no pose/time invalidation.
                int count = (int)Get(motion, "count"); float duration = (float)Get(motion, "Duration");
                var times = (float[])((float[])timeBuffer).Clone(); var recorded = ((Array)poseBuffer).Cast<object>().Take(count * 8).ToArray();
                Assert.That(count, Is.GreaterThan(2)); Assert.That((bool)Call(motion, "RefreshAppearanceSources", Enumerable.Range(0, 8).ToArray(), false), Is.True);
                Assert.That(Get(motion, "roots"), Is.SameAs(poseBuffer)); Assert.That(Get(motion, "parts"), Is.SameAs(partBuffer)); Assert.That(Get(motion, "times"), Is.SameAs(timeBuffer));
                Assert.That((int)Get(motion, "count"), Is.EqualTo(count)); Assert.That((float)Get(motion, "Duration"), Is.EqualTo(duration)); CollectionAssert.AreEqual(times, (float[])Get(motion, "times")); CollectionAssert.AreEqual(recorded, ((Array)Get(motion, "roots")).Cast<object>().Take(count * 8).ToArray());
                long payload = ((Array)partBuffer).LongLength * 40 + ((Array)poseBuffer).LongLength * 40;
                TestContext.WriteLine("SKY_WEATHER_METADATA snow=" + snow + " actualRecordFrames=" + count + " duration=" + duration + " automaticRefreshes=" + Get(review, "WeatherMetadataRefreshes") + " originalPoseBytes=" + payload + "; all8 truck/boat material identities equal, existing buffers/times/poses retained.");
                // Controlled match-end presentation gate, not a ring result.
                Call(f.match, "EndMatch"); Assert.That((bool)Call(review, "Play"), Is.True);
                for (int i = 0; i < 4; i++)
                {
                    var ghost = ((Transform[])Get(motion, "ghosts"))[i * 2];
                    Assert.That(ghost.GetComponentsInChildren<Transform>(true).Count(part => part.name == "Sports appearance — " + (i == 0 ? "truck.monster" : "novelty.icecream")), Is.EqualTo(1));
                    Assert.That(((Transform[])Get(motion, "ghosts"))[i * 2 + 1].gameObject.activeSelf, Is.True);
                }
                foreach (int angle in new[] { 0, 1, 2 }) { if (angle > 0) Call(review, "Action", "replayangle"); Call(review, "Action", "replayrewind"); yield return null; Assert.That((int)Get(motion, "Angle"), Is.EqualTo(angle)); }
                Assert.That((int)Get(review, "WeatherMetadataRefreshes"), Is.EqualTo(1)); Call(review, "Stop", false);
                CollectionAssert.AreEqual(ghostIds, ((Transform[])Get(motion, "ghosts")).Select(root => root.GetInstanceID()).ToArray());
            }
            f.Start(); Assert.That((int)Get(Get(f.match, "review"), "WeatherMetadataRefreshes"), Is.Zero);
            for (int i = 0; i < 4; i++) Applied(f.Host(i), f.ids[i], i == 0 ? "truck.monster" : "novelty.icecream");
            CollectionAssert.AreEqual(ghostIds, ((Transform[])Get(f.Replay, "ghosts")).Select(root => root.GetInstanceID()).ToArray()); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);
        }
    }
}
#endif
