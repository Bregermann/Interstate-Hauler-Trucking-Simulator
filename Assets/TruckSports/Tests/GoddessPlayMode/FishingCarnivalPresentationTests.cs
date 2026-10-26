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

// Real saved Fishing source and vendor clips; synthetic animation sampling is
// labeled separately from real AI movement/catches and controlled captures.
public sealed class FishingCarnivalPresentationTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Fishing(string name) => Type.GetType("LWS.TruckFishing." + name + ", Assembly-CSharp", true);
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string member) => owner.GetType().GetField(member, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(member, Any).GetValue(owner);
    static void Set(object owner, string member, object value) => owner.GetType().GetField(member, Any).SetValue(owner, value);
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethods(Any).Single(candidate => candidate.Name == method && candidate.GetParameters().Length == args.Length).Invoke(owner, args);
    IDisposable lease;
    GameObject isolation;
    Component match;
    IEnumerator Load()
    {
        lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "FishingCarnival" + Guid.NewGuid().ToString("N") });
        isolation = new GameObject("Explicit Fishing presentation validation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFishing/Scenes/TruckFishing.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(Fishing("FishingMatch")); Assert.That(match, Is.Not.Null);
    }
    [TearDown] public void Cleanup()
    {
        if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); lease?.Dispose(); lease = null;
    }
    static string Settings(object match)
    {
        var names = new[] { "originalInitialSpeed", "initialSpeedMultiplier", "multiplierPerThirtySeconds", "maximumSpeedMultiplier", "fishHalfWidth", "fishAcceleration", "challengeSeconds", "poisonMaximum", "burstDuration", "breakDuration", "density", "seed" };
        return string.Join("|", names.Select(name => name + ":" + Get(match, name)));
    }
    [UnityTest, Timeout(120000)]
    public IEnumerator ActualAiFishSwimUnderRaisedWaterAndGenerateBoundedSpeedDepthResponsesWithoutTrajectoryChanges()
    {
        yield return Load(); var streams = ((Array)Get(match, "streams")).Cast<Component>().ToArray(); var waters = streams.Select(stream => stream.GetComponent(Fishing("FishingCarnivalWater"))).ToArray();
        Assert.That(waters.All(water => water != null), Is.True, "Run the explicit Fishing Carnival authoring entry.");
        string settings = Settings(match); Set(Get(match, "setup"), "count", 2); Call(match, "AllBots"); Assert.That(Call(match, "StartMatch"), Is.True);
        double deadline = Time.realtimeSinceStartupAsDouble + 40;
        while (waters.Sum(water => (int)Get(water, "Emissions")) == 0 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That(waters.Sum(water => (int)Get(water, "Emissions")), Is.GreaterThan(0), "Actual moving fish must disturb their visible water surface.");
        Assert.That(Settings(match), Is.EqualTo(settings)); int visible = 0;
        double clock = (double)Get(match, "Clock");
        for (int slot = 0; slot < 2; slot++)
        {
            var stream = streams[slot]; var water = waters[slot]; var surface = (Transform)Get(water, "surface");
            var mesh = surface.GetComponent<MeshFilter>().sharedMesh;
            float renderedY = surface.TransformPoint(mesh.bounds.center).y;
            Assert.That(renderedY, Is.EqualTo((float)Get(water, "WaterY")).Within(.001f));
            Assert.That((float)Get(water, "localWaterHeight"), Is.GreaterThan(.13f));
            var models = (Transform[])Get(stream, "models"); var active = (bool[])Get(stream, "active"); var plans = (Array)Get(stream, "plans");
            for (int i = 0; i < active.Length; i++)
            {
                if (!active[i]) continue; var plan = plans.GetValue(i);
                float progress = (float)Fishing("FishingStream").GetMethod("Progress").Invoke(null, new object[] { plan, clock });
                var expected = (Vector3)Fishing("FishingStream").GetMethod("Position").Invoke(null, new object[] { plan, progress });
                Assert.That(Vector3.Distance(models[i].localPosition, expected), Is.LessThan(.0001f), "Presentation may not move a catchable fish.");
                var body = models[i].GetComponentInChildren<SkinnedMeshRenderer>();
                if (body != null) { Assert.That(body.bounds.center.y, Is.LessThan(renderedY)); visible++; }
            }
            var net = (Transform)Get(stream, "net"); Assert.That(net.localPosition.y, Is.EqualTo(.12f).Within(.0001f));
            Assert.That(net.localPosition.x, Is.EqualTo((float)Get(stream, "NetX")).Within(.0001f));
            foreach (string effect in new[] { "ripples", "foam", "droplets", "separation" })
            { var system = (ParticleSystem)Get(water, effect); Assert.That(system.particleCount, Is.LessThanOrEqualTo(system.main.maxParticles)); Assert.That(system.GetComponentsInChildren<Collider>(true), Is.Empty); }
        }
        Assert.That(visible, Is.GreaterThan(0));
        var capture = Sports("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 6);
        var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>();
        foreach (int width in new[] { 1280, 1920 }) capture.Invoke(null, new object[] { canvas, (Camera[])Get(match, "cameras"), width, width * 9 / 16, "Logs/TruckSports/Carnival/Fishing/CONTROLLED-water-fish-" + width + ".png", null });
        TestContext.WriteLine("ACTUAL_FISHING_WATER real AI fish motion generated bounded wakes/foam/ripples with raised water above unchanged fish centers; net and analytic crossing positions/settings unchanged. Controlled images require visual inspection; no human/device claim.");
    }
    [UnityTest, Timeout(120000)]
    public IEnumerator GenuineWolfUsesChangingVendorBonePosesAndWalkSwimRoutesWithoutCatchOrPhysicsAuthority()
    {
        yield return Load(); var actor = (Component)UnityEngine.Object.FindAnyObjectByType(Fishing("FishingCarnivalWildlife"));
        Assert.That(actor, Is.Not.Null, "Run private owned clip intake then explicit presentation authoring; no wildlife placeholder is accepted.");
        Assert.That(Get(actor, "species"), Is.EqualTo("Wolf"), "Additional wolf wildlife must never be reported as the missing bear.");
        var visual = (GameObject)Get(actor, "visual"); Assert.That(visual.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
        var animator = (Animator)Get(actor, "animator"); Assert.That(animator.applyRootMotion, Is.False); Assert.That(animator.fireEvents, Is.False);
        Assert.That(animator.avatar != null && animator.avatar.isValid, Is.True); Assert.That(animator.runtimeAnimatorController.animationClips.Length, Is.GreaterThanOrEqualTo(6));
        string rules = JsonUtility.ToJson(Get(match, "rules")), settings = Settings(match);
        var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>(true); Assert.That(skin.bones.Length, Is.GreaterThan(10));
        visual.SetActive(true); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Play("Base Layer.Walk", 0, 0); animator.Update(0);
        var pose = skin.bones.Select(bone => bone.localRotation).ToArray(); Vector3 rootPosition = actor.transform.position;
        animator.Update(.35f); int moving = skin.bones.Where((bone, index) => Quaternion.Angle(pose[index], bone.localRotation) > .5f).Count();
        Assert.That(moving, Is.GreaterThan(3), "Real vendor skeletal locomotion must change multiple bones; translating an unanimated model is insufficient.");
        Assert.That(actor.transform.position, Is.EqualTo(rootPosition));
        bool walked = false, swam = false;
        for (int i = 0; i < 1800; i++)
        {
            Call(actor, "TickPresentation", .1f); animator.Update(.1f);
            string state = (string)Get(actor, "AnimationState"); walked |= state == "Walk" || state == "Left" || state == "Right"; swam |= state == "Swim";
            if (walked && swam && (int)Get(actor, "WaterSteps") > 0) break;
        }
        Assert.That(walked, Is.True); Assert.That(swam, Is.True); Assert.That((int)Get(actor, "WaterSteps"), Is.GreaterThan(0));
        Assert.That(JsonUtility.ToJson(Get(match, "rules")), Is.EqualTo(rules)); Assert.That(Settings(match), Is.EqualTo(settings));
        TestContext.WriteLine("SYNTHETIC_VENDOR_ANIMATION changingBones=" + moving + "; real WolfLite clips/rig sampled through walk/turn/swim and water response. Additional wolf only; bear model/animation requirement remains MISSING.");
    }
    [TestCase(.1f, .4f, 1f)][TestCase(.5f, .7f, 0f)][TestCase(-1f, -.5f, 0f)]
    public void SurfaceResponseDistinguishesIntersectingAirborneAndDeepBodies(float bottom, float top, float expected)
    {
        float weight = (float)Fishing("FishingCarnivalWater").GetMethod("SurfaceWeight").Invoke(null, new object[] { bottom, top, .27f }); Assert.That(weight, Is.EqualTo(expected).Within(.001f));
    }
}
#endif
