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

public sealed class NinjaCarnivalEnvironmentTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    Component match, environment, truck;
    Rigidbody body;
    IDisposable lease;
    GameObject isolation;
    IEnumerator Load()
    {
        lease = (IDisposable)Type("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "NinjaEnvironment" + Guid.NewGuid().ToString("N") });
        isolation = new GameObject("Explicit Ninja environment fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Type("LWS.TruckSports.SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(Type("LWS.TruckNinjaWarrior.NinjaMatch")); environment = (Component)UnityEngine.Object.FindAnyObjectByType(Type("LWS.TruckNinjaWarrior.NinjaCarnivalEnvironment"));
        Assert.That(environment, Is.Not.Null, "Invoke the explicit missing-environment authoring entry first.");
        Set(Get(match, "setup"), "count", 1); Call(match, "AllBots"); truck = (Component)((Array)Get(match, "trucks")).GetValue(0); body = (Rigidbody)Get(truck, "body");
    }
    IEnumerator Running()
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 6;
        while (Get(Get(match, "rules"), "Phase").ToString() != "Running" && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That(Get(Get(match, "rules"), "Phase").ToString(), Is.EqualTo("Running"));
    }
    [TearDown] public void Cleanup()
    {
        if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); lease?.Dispose(); lease = null;
    }
    void Capture(string name)
    {
        var method = Type("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(item => item.Name == "Save" && item.GetParameters().Length == 6);
        var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>();
        method.Invoke(null, new object[] { canvas, (Camera[])Get(match, "cameras"), 1280, 720, "Logs/TruckSports/Carnival/NinjaEnvironment/CONTROLLED-" + name + ".png", null });
    }
    bool ObserveFraming(string label)
    {
        var cameras = ((Camera[])Get(match, "cameras")).Where(camera => camera != null && camera.enabled).ToArray(); Assert.That(cameras, Is.Not.Empty);
        var camera = cameras[0]; var broadcast = (Component)Get(match, "broadcast"); var framing = Get(broadcast, "broadcastFraming");
        var rect = framing != null && camera == (Camera)Get(broadcast, "view") ? (Rect)Call(framing, "ProjectedTruckRect", camera, 0) : new Rect(.5f, .5f, .1f, .1f);
        Vector3 center = camera.WorldToViewportPoint(body.worldCenterOfMass);
        var logos = match.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).Where(renderer => renderer.name == "Original supported Ninja gameshow logo").ToArray();
        bool blocked = false;
        foreach (var logo in logos)
        {
            var b = logo.bounds; float minX = float.PositiveInfinity, minY = float.PositiveInfinity, maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            float depth = camera.WorldToViewportPoint(b.center).z;
            for (int i = 0; i < 8; i++) { var p = camera.WorldToViewportPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))); if (p.z <= 0) continue; minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y); }
            if (depth > 0 && depth < center.z && Rect.MinMaxRect(minX, minY, maxX, maxY).Contains(new Vector2(center.x, center.y))) blocked = true;
        }
        TestContext.WriteLine("NINJA_CAMERA_SEQUENCE " + label + " frame=" + Time.frameCount + " position=" + camera.transform.position + " angle=" + camera.transform.eulerAngles + " truck=" + center + " projected=" + rect + " foregroundLogo=" + blocked);
        return !blocked && center.z > 0 && center.x > .03f && center.x < .97f && center.y > .03f && center.y < .97f && rect.height > .06f;
    }
    [UnityTest, Timeout(120000)]
    public IEnumerator ActualGravityFallCapturesBeforeResetAndSplashesOnlyAtVisualWaterIntersection()
    {
        yield return Load(); float level = (float)Get(Get(match, "course"), "waterLevel"); var start = (Vector3)Get(Get(match, "course"), "start");
        Assert.That(Call(match, "StartMatch"), Is.True); yield return Running();
        var driving = Get(match, "drivingViews"); int originalMode = (int)Get(driving, "Mode");
        var originalCameras = ((Camera[])Get(match, "cameras")).ToArray(); var originalMasks = originalCameras.Select(camera => camera.cullingMask).ToArray();
        var cabs = (Transform[])Get(driving, "cabs"); var originalCabState = cabs.Select(cab => cab.gameObject.activeSelf).ToArray();
        var fit = (Behaviour)Get(match, "framing"); bool fitEnabled = fit.enabled, fitArena = (bool)Get(fit, "arenaFit");
        // Controlled setup, then genuine original PhysX gravity and fall logic.
        // No velocity/score is fabricated and real source reset stays immediate.
        Call(truck, "Prepare", new Vector3(45, 8, 170), Quaternion.identity, true); Physics.SyncTransforms();
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while ((int)Get(environment, "CapturedFalls") == 0 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That((int)Get(environment, "CapturedFalls"), Is.EqualTo(1));
        Assert.That(((Vector3)Get(environment, "CapturedPosition")).y, Is.LessThan(level + 1)); Assert.That(((Vector3)Get(environment, "CapturedVelocity")).y, Is.LessThan(-1));
        Assert.That(Vector3.Distance((Vector3)Get(environment, "CapturedPosition"), start), Is.GreaterThan(20)); Assert.That(Vector3.Distance(body.position, start), Is.LessThan(2));
        var rules = Get(match, "rules"); Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("Countdown")); Assert.That((float)Get(rules, "Penalty"), Is.Zero); Assert.That((int)Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
        foreach (var ghost in (IEnumerable)Get(environment, "ghosts")) if (ghost != null)
        {
            var visual = (GameObject)Get(ghost, "visual"); Assert.That(visual.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(visual.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
        }
        Capture("fall-reset-immediate"); ObserveFraming("fall-reset-immediate");
        deadline = Time.realtimeSinceStartupAsDouble + 3;
        bool visibleSplash = false;
        while ((int)Get(environment, "WaterSplashes") == 0 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That((int)Get(environment, "WaterSplashes"), Is.EqualTo(1)); Assert.That((float)Get(environment, "LastVisualBottom"), Is.LessThanOrEqualTo((float)Get(environment, "WaterHeight") + .005f));
        yield return null; // Observe an actual rendered Present/LateUpdate boundary.
        Assert.That((bool)Get(environment, "FallShotBorrowed"), Is.True);
        Assert.That((int)Get(Get(match, "broadcast"), "FrameEvaluations"), Is.EqualTo(1), "Only Ninja.Present evaluates the existing Cinemachine camera during the fall shot.");
        Vector3 splash = (Vector3)Get(environment, "LastSplashPoint");
        foreach (var camera in (Camera[])Get(match, "cameras")) if (camera != null && camera.enabled) { var screen = camera.WorldToViewportPoint(splash); visibleSplash |= screen.z > 0 && screen.x >= 0 && screen.x <= 1 && screen.y >= 0 && screen.y <= 1; }
        Capture("surface-intersection-splash"); TestContext.WriteLine("NINJA_VISUAL_FALL captured=" + Get(environment, "CapturedPosition") + " incoming=" + Get(environment, "CapturedVelocity") + " initialRenderBottom=" + Get(environment, "CapturedVisualBottom") + " contactRenderBottom=" + Get(environment, "LastVisualBottom") + " splash=" + splash + " primaryCameraVisible=" + visibleSplash + "; render-only continuation, not recorded physical motion.");
        Assert.That(visibleSplash, Is.True, "A pooled world splash must be inside an actual active viewport; inspect the retained image if presentation framing needs a mode-only correction.");
        float shotAge = (float)Get(environment, "FallShotAge"), countdownAge = (float)Get(rules, "StateClock");
        Call(match, "Pause", 0, false); yield return null; yield return null;
        Assert.That((bool)Get(environment, "FallShotActive"), Is.True); Assert.That((float)Get(environment, "FallShotAge"), Is.EqualTo(shotAge));
        Assert.That((float)Get(rules, "StateClock"), Is.EqualTo(countdownAge)); Call(match, "Resume");
        deadline = Time.realtimeSinceStartupAsDouble + 3;
        while ((bool)Get(environment, "FallShotActive") && Time.realtimeSinceStartupAsDouble < deadline)
        {
            Assert.That((int)Get(Get(match, "broadcast"), "FrameEvaluations"), Is.LessThanOrEqualTo(1)); yield return null;
        }
        Assert.That((bool)Get(environment, "FallShotActive"), Is.False); Assert.That((float)Get(environment, "FallShotAge"), Is.LessThanOrEqualTo(1.25f));
        Assert.That(Get(rules, "Phase").ToString(), Is.EqualTo("Countdown"), "The camera must return before the unchanged countdown releases GO.");
        Assert.That((float)Get(rules, "Clock"), Is.Zero); Assert.That((float)Get(rules, "Penalty"), Is.Zero); Assert.That((int)Get(Get(match, "ScoreJournal"), "Count"), Is.Zero);
        Assert.That((int)Get(driving, "Mode"), Is.EqualTo(originalMode)); CollectionAssert.AreEqual(originalCameras, (Camera[])Get(match, "cameras"));
        CollectionAssert.AreEqual(originalMasks, originalCameras.Select(camera => camera.cullingMask)); CollectionAssert.AreEqual(originalCabState, cabs.Select(cab => cab.gameObject.activeSelf));
        Assert.That(fit.enabled, Is.EqualTo(fitEnabled)); Assert.That((bool)Get(fit, "arenaFit"), Is.EqualTo(fitArena));
        bool settled = false;
        for (int frame = 1; frame <= 18; frame++) { yield return null; if (frame == 2 || frame == 6 || frame == 18) { Capture("fall-reset-after-" + frame + "-frames"); settled = ObserveFraming("fall-reset-after-" + frame + "-frames"); } }
        Assert.That(settled, Is.True, "Repeated rendered restart framing must acquire the start truck without a foreground logo; inspect sequence before changing any camera.");
        Assert.That((float)Get(Get(match, "course"), "waterLevel"), Is.EqualTo(level));
        // Exercise the original outside-course fall rule but require no visual
        // splash beyond the actual water plane's bounds.
        yield return Running(); var bounds = (Bounds)Get(environment, "waterBounds"); int before = (int)Get(environment, "WaterSplashes");
        int captured = (int)Get(environment, "CapturedFalls");
        Call(truck, "Prepare", new Vector3(bounds.max.x + 30, 8, 170), Quaternion.identity, true); Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartupAsDouble + 2;
        while ((int)Get(environment, "CapturedFalls") == captured && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That((int)Get(environment, "CapturedFalls"), Is.EqualTo(captured + 1));
        deadline = Time.realtimeSinceStartupAsDouble + 3;
        while ((int)Get(environment, "ActiveGhosts") > 0 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That((int)Get(environment, "WaterSplashes"), Is.EqualTo(before), "Outside-pool falls must not invent water contact.");
        Call(match, "SetupScreen"); yield return null; Assert.That((int)Get(environment, "ActiveGhosts"), Is.Zero); Assert.That(((ParticleSystem[])Get(environment, "splash")).All(system => !system.IsAlive(true)), Is.True);
        Assert.That(Call(match, "StartMatch"), Is.True); yield return null; yield return null; Assert.That((int)Get(environment, "CapturedFalls"), Is.Zero);
    }
    [UnityTest, Timeout(120000)]
    public IEnumerator FallShotReturnsExistingCabAndCameraMasksAndClearsImmediatelyOnSetupAndRematch()
    {
        yield return Load();
        Type("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { true });
        var driving = Get(match, "drivingViews"); Call(driving, "Select", 2);
        Assert.That(Call(match, "StartMatch"), Is.True); yield return Running();
        var original = ((Camera[])Get(match, "cameras")).ToArray(); Assert.That(original, Is.Not.Empty);
        var masks = original.Select(camera => camera.cullingMask).ToArray(); var cabs = (Transform[])Get(driving, "cabs");
        var cabState = cabs.Select(cab => cab.gameObject.activeSelf).ToArray(); Assert.That(cabState.Any(active => active), Is.True);
        var fit = (Behaviour)Get(match, "framing"); bool enabled = fit.enabled, arena = (bool)Get(fit, "arenaFit");
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Call(truck, "Prepare", new Vector3(45, 8, 170), Quaternion.identity, true); Physics.SyncTransforms();
            double deadline = Time.realtimeSinceStartupAsDouble + 8;
            while (!(bool)Get(environment, "FallShotBorrowed") && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That((bool)Get(environment, "FallShotBorrowed"), Is.True); Assert.That(cabs.All(cab => !cab.gameObject.activeSelf), Is.True);
            Assert.That(((Camera[])Get(match, "cameras")).Single(), Is.SameAs((Camera)Get(Get(match, "broadcast"), "view")));
            Assert.That((int)Get(driving, "Mode"), Is.EqualTo(2));
            if (attempt == 0)
            {
                deadline = Time.realtimeSinceStartupAsDouble + 3;
                while ((bool)Get(environment, "FallShotActive") && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Assert.That((bool)Get(environment, "FallShotActive"), Is.False);
                CollectionAssert.AreEqual(original, (Camera[])Get(match, "cameras")); CollectionAssert.AreEqual(masks, original.Select(camera => camera.cullingMask));
                CollectionAssert.AreEqual(cabState, cabs.Select(cab => cab.gameObject.activeSelf)); Assert.That(fit.enabled, Is.EqualTo(enabled)); Assert.That((bool)Get(fit, "arenaFit"), Is.EqualTo(arena));
                yield return Running();
            }
            else
            {
                Call(match, "SetupScreen"); Assert.That((bool)Get(environment, "FallShotActive"), Is.False); Assert.That((bool)Get(environment, "FallShotBorrowed"), Is.False);
                CollectionAssert.AreEqual(original, (Camera[])Get(match, "cameras")); CollectionAssert.AreEqual(masks, original.Select(camera => camera.cullingMask));
                Assert.That(Call(match, "StartMatch"), Is.True); yield return null; yield return null;
                Assert.That((bool)Get(environment, "FallShotActive"), Is.False); Assert.That((int)Get(environment, "ActiveGhosts"), Is.Zero); Assert.That((int)Get(environment, "CapturedFalls"), Is.Zero);
                Assert.That((int)Get(driving, "Mode"), Is.EqualTo(2));
            }
        }
        TestContext.WriteLine("NINJA_FALL_SHOT_CAB existing selected mode/camera array/masks/interior visibility restored; setup/rematch clear owned presentation immediately. No new camera or rule delay.");
    }
    [UnityTest, Timeout(90000)]
    public IEnumerator SavedNinjaRetainsAnimatedWaterAndCountdownWhileAppendingRealCrowdsAndTwoFirePillars()
    {
        yield return Load();
        var water = (Transform)Get(environment, "waterSurface"); Assert.That(water.position.y, Is.Zero.Within(.001f)); Assert.That(water.GetComponent(Type("LWS.TruckFishing.FishingWaterFlow")), Is.Not.Null);
        Assert.That(environment.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(environment.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(environment.GetComponentsInChildren<Light>(true), Is.Empty);
        var crowd = Get(match, "crowd"); Assert.That((int)Get(crowd, "Spectators"), Is.GreaterThanOrEqualTo(2184));
        Assert.That(((Array)Get(environment, "addedPosedFans")).Length, Is.EqualTo(810)); Assert.That(((Animator[])Get(environment, "newFans")).Length, Is.EqualTo(24));
        Assert.That(((Animator[])Get(environment, "newFans")).All(animator => animator.runtimeAnimatorController != null && !animator.applyRootMotion), Is.True);
        var pillars = environment.GetComponentsInChildren<MeshRenderer>(true).Where(renderer => renderer.name == "Giant flaming entrance pillar").ToArray(); Assert.That(pillars.Length, Is.EqualTo(2)); Assert.That(pillars.All(pillar => pillar.bounds.size.y > 25), Is.True);
        Assert.That(environment.GetComponentsInChildren<MeshRenderer>(true).Count(renderer => renderer.name == "Proper checkered Ninja start line"), Is.EqualTo(1));
        Assert.That(((ParticleSystem[])Get(environment, "entranceFire")).Length, Is.GreaterThanOrEqualTo(4)); Assert.That(((ParticleSystem[])Get(environment, "entranceFire")).All(system => system.main.maxParticles <= 64), Is.True);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Assert.That(Call(match, "StartMatch"), Is.True); Assert.That(Get(Get(match, "rules"), "Phase").ToString(), Is.EqualTo("Countdown"), "Reuse the existing countdown at the original start boundary."); ObserveFraming("initial-attempt" + attempt + "-immediate"); Capture("initial-attempt" + attempt + "-immediate");
            for (int frame = 0; frame < 18; frame++) yield return null;
            Assert.That(ObserveFraming("initial-attempt" + attempt + "-settled"), Is.True); Capture("initial-attempt" + attempt + "-settled");
            Assert.That((bool)Get(match, "SupportsRaceCountdown"), Is.True, "The existing shared countdown remains the presentation authority.");
        }
        TestContext.WriteLine("SAVED_NINJA_ENVIRONMENT same animated Y0 water and existing countdown;810 additional FieldGoal batch fans +24 animated fans, two genuine masked fire pillars and checkered start. No new physics, lighting or camera source changes; controlled frame sequence requires inspection.");
    }
}
#endif
