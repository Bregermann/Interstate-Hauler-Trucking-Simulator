#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CornholeNaturalFlightFramingTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string n) => Type.GetType("LWS.TruckSports." + n + ", Assembly-CSharp", true);
    static Type Corn(string n) => Type.GetType("LWS.TruckCornhole." + n + ", Assembly-CSharp", true);
    static object Get(object o, string n) => o.GetType().GetField(n, Any) is FieldInfo f ? f.GetValue(o) : o.GetType().GetProperty(n, Any).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, Any).SetValue(o, v);
    static object Call(object o, string n, params object[] a) => o.GetType().GetMethods(Any).Single(m => m.Name == n && m.GetParameters().Length == a.Length && m.GetParameters().Select((p, i) => a[i] != null ? p.ParameterType.IsInstanceOfType(a[i]) : !p.ParameterType.IsValueType || Nullable.GetUnderlyingType(p.ParameterType) != null).All(v => v)).Invoke(o, a);
    static object Registry(string n, params object[] a) => Sports("SportsParticipantRegistry").GetMethod(n).Invoke(null, a);
    static bool Visible(Renderer r) => r != null && (r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled && r.gameObject.activeInHierarchy && r.transform.lossyScale.sqrMagnitude > 1e-8f && r.bounds.size.sqrMagnitude > 1e-8f;
    static bool Wheel(Renderer r) { var tag = r.GetComponentInParent(Sports("SportsTruckAppearanceMountTag"), true); return tag != null && (string)Get(tag, "slotId") == "wheels"; }
    static Vector3[] Corners(Renderer r) => Enumerable.Range(0, 8).Select(i => r.localToWorldMatrix.MultiplyPoint3x4(r.localBounds.center + Vector3.Scale(r.localBounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)))).ToArray();
    static string FullPath(Transform t) => t.parent == null ? t.name : FullPath(t.parent) + "/" + t.name;
    Renderer BoardMesh(object course, out MeshCollider collision)
    {
        var board = (Transform)Get(course, "board"); var shared = Get(course, "sharedTarget");
        Assert.That(shared, Is.Not.Null, "The saved shared target must explicitly own its gameplay board.");
        Assert.That((UnityEngine.Object)Get(shared, "match"), Is.EqualTo(match));
        var surface = (Component)Get(shared, "boardSurface"); Assert.That(surface, Is.Not.Null);
        Assert.That(surface.transform, Is.EqualTo(board)); Assert.That(Get(surface, "kind").ToString(), Is.EqualTo("Board"));
        Assert.That((bool)Call(shared, "Owns", surface), Is.True);
        collision = surface.GetComponent<MeshCollider>(); Assert.That(collision, Is.Not.Null); Assert.That(collision.sharedMesh, Is.Not.Null); Assert.That(collision.enabled && !collision.isTrigger, Is.True);
        // Original IsolateRendering moves ONLY the visible surface below the
        // collider root. Match exact gameplay mesh identity, not first-child,
        // edge stripes, hole markings, legs or decorative board text.
        var physicalMesh = collision.sharedMesh;
        var copies = board.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.sharedMesh == physicalMesh && filter.GetComponent<MeshRenderer>() != null).ToArray();
        Assert.That(copies.Length, Is.EqualTo(1), "One exact visible copy of the authoritative gameplay board mesh is required.");
        var filter = copies[0]; var renderer = filter.GetComponent<MeshRenderer>(); Assert.That(Visible(renderer), Is.True);
        for (int i = 0; i < 8; i++)
        {
            var point = physicalMesh.bounds.center + Vector3.Scale(physicalMesh.bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Assert.That(Vector3.Distance(collision.transform.TransformPoint(point), filter.transform.TransformPoint(point)), Is.LessThan(.0001f), "Visible board geometry must coincide with the actual scoring/contact mesh.");
        }
        return renderer;
    }
    [Serializable] sealed class BodySample { public int participant, delivery, launchCount, cornersInside, totalCorners; public float flightAge; public string phase, bodyId; public Vector3 bodyPosition, velocity, worldMin, worldMax, viewportMin, viewportMax, stockViewportMin, stockViewportMax, fullExteriorWorldMin, fullExteriorWorldMax; public int bodyMeshCount, stockBodyMeshCount, excludedWheelMeshCount; }
    [Serializable] sealed class FrameSample { public int frame, viewOwner; public float time, fov, aspect; public string boardRendererPath; public int boardColliderId, boardMeshId; public bool spectating, replay; public Rect cameraRect; public Vector3 cameraPosition, cameraEuler, boardViewportMin, boardViewportMax, boardCenterViewport; public BodySample[] bodies; }
    [Serializable] sealed class CaseReport { public string label; public string sampling = "Final native camera transform after LateUpdate32000; controlled projection/render, not URP render callbacks"; public int count; public bool split; public List<FrameSample> samples = new List<FrameSample>(); public bool captured; public int[] actualLaunches; }
    Component match; object[] courses; CaseReport active; readonly float[] nextSample = new float[4]; string folder; Exception sampleFailure;
    readonly List<CaseReport> reports = new List<CaseReport>();
    static Vector3 Min(IEnumerable<Vector3> p) => new Vector3(p.Min(v => v.x), p.Min(v => v.y), p.Min(v => v.z));
    static Vector3 Max(IEnumerable<Vector3> p) => new Vector3(p.Max(v => v.x), p.Max(v => v.y), p.Max(v => v.z));
    static bool Flying(object course) => Get(course, "Current") is Component t && t != null && (int)Get(t, "delivery") == 0 && (bool)Get(t, "Launched") && !(bool)Get(t, "Landed") && !(bool)Get(t, "Holed") && !(bool)Get(t, "Excluded") && Get(course, "Phase").ToString() == "Flight";
    void SampleFinalCameras()
    {
        if (active == null || match == null || (bool)Get(match, "Paused")) return;
        foreach (var course in courses.Take(active.count))
        {
            var camera = (Camera)Get(Get(course, "chase"), "view");
            if (camera.enabled) SampleCamera(camera);
        }
        if (!active.captured && courses.Take(active.count).Any(c => Flying(c) && (float)Get(c, "PhaseElapsed") >= 2.5f))
        {
            // Explicit controlled rerender after the ORIGINAL camera LateUpdate;
            // no GameView/URP callbacks are required in native batch mode.
            Call(match, "Pause", 0, false); Call(Get(match, "menus"), "Show", Enum.Parse(Corn("CornholeScreen"), "Play"));
            var originals = courses.Take(active.count).Select(c => (Rigidbody)Get(Get(c, "Current"), "body")).ToArray(); var positions = originals.Select(body => body.position).ToArray(); var velocities = originals.Select(body => body.linearVelocity).ToArray();
            Capture(match, courses, active, folder); CollectionAssert.AreEqual(positions, originals.Select(body => body.position)); CollectionAssert.AreEqual(velocities, originals.Select(body => body.linearVelocity)); active.captured = true; Call(match, "Resume");
        }
    }
    void SampleCamera(Camera camera)
    {
        if (active == null || match == null || (bool)Get(match, "Paused") || !camera.enabled) return;
        int owner = Array.FindIndex(courses, c => (Camera)Get(Get(c, "chase"), "view") == camera); if (owner < 0 || owner >= active.count) return;
        // Read the final ORIGINAL camera pose independently for every viewport.
        if (Time.time < nextSample[owner]) return; nextSample[owner] = Time.time + .3f;
        var chase = Get(courses[owner], "chase"); bool replay = (bool)Get(Get(courses[owner], "replay"), "Playing"); if (replay) return;
        var flying = courses.Take(active.count).Where(Flying).ToArray(); if (flying.Length == 0) return;
        if (flying.All(c => (float)Get(c, "PhaseElapsed") < .25f)) return;
        var boardMesh = BoardMesh(courses[owner], out var boardCollider);
        var boardPoints = Corners(boardMesh).Select(camera.WorldToViewportPoint).ToArray(); var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        var sample = new FrameSample { frame = Time.frameCount, time = Time.time, viewOwner = owner, spectating = (bool)Get(chase, "Spectating"), replay = replay, fov = camera.fieldOfView, aspect = camera.aspect, cameraRect = camera.rect, cameraPosition = camera.transform.position, cameraEuler = camera.transform.eulerAngles, boardViewportMin = Min(boardPoints), boardViewportMax = Max(boardPoints), boardCenterViewport = camera.WorldToViewportPoint(boardCollider.transform.TransformPoint(boardCollider.sharedMesh.bounds.center)), boardRendererPath = FullPath(boardMesh.transform), boardColliderId = boardCollider.GetInstanceID(), boardMeshId = boardCollider.sharedMesh.GetInstanceID(), bodies = new BodySample[flying.Length] };
        for (int i = 0; i < flying.Length; i++)
        {
            var course = flying[i]; var truck = (Component)Get(course, "Current"); var body = (Rigidbody)Get(truck, "body"); var host = Get(truck, "appearanceHost"); var root = (Transform)Get(host, "AppearanceRoot");
            var all = root.GetComponentsInChildren<Renderer>(true).Where(Visible).ToArray(); var bodyMeshes = all.Where(r => !Wheel(r)).ToArray(); Assert.That(bodyMeshes, Is.Not.Empty);
            var points = bodyMeshes.SelectMany(Corners).ToArray(); var full = all.SelectMany(Corners).ToArray(); var projected = points.Select(camera.WorldToViewportPoint).ToArray();
            // The original correctly miniaturized licensed chassis is the local
            // readability baseline. Ignore its enabled state (composition hides
            // it), and never include legacy Rubber tire primitives or wheels.
            var stock = ((Renderer[])Get(host, "originalBodyRenderers")).Where(r => r != null && r.name != "Rubber tire" && (r is MeshRenderer || r is SkinnedMeshRenderer) && r.transform.lossyScale.sqrMagnitude > 1e-8f && r.bounds.size.sqrMagnitude > 1e-8f).ToArray();
            Assert.That(stock, Is.Not.Empty); var stockProjected = stock.SelectMany(Corners).Select(camera.WorldToViewportPoint).ToArray();
            sample.bodies[i] = new BodySample { participant = (int)Get(course, "index"), delivery = (int)Get(truck, "delivery"), launchCount = (int)Get(truck, "LaunchCount"), flightAge = (float)Get(course, "PhaseElapsed"), phase = Get(course, "Phase").ToString(), bodyId = (string)Get(host, "EffectiveBodyId"), bodyPosition = body.position, velocity = body.linearVelocity, worldMin = Min(points), worldMax = Max(points), viewportMin = Min(projected), viewportMax = Max(projected), fullExteriorWorldMin = Min(full), fullExteriorWorldMax = Max(full), bodyMeshCount = bodyMeshes.Length, stockBodyMeshCount = stock.Length, stockViewportMin = Min(stockProjected), stockViewportMax = Max(stockProjected), excludedWheelMeshCount = all.Length - bodyMeshes.Length, totalCorners = points.Length, cornersInside = points.Count(p => planes.All(plane => plane.GetDistanceToPoint(p) >= 0)) };
        }
        active.samples.Add(sample);
    }
    static void Capture(Component match, object[] courses, CaseReport report, string folder)
    {
        var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>(); var cameras = courses.Select(c => (Camera)Get(Get(c, "chase"), "view")).Where(c => c.enabled).ToArray(); var save = Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 6);
        foreach (int width in new[] { 1280, 1920 }) save.Invoke(null, new object[] { canvas, cameras, width, width == 1280 ? 720 : 1080, Path.Combine(folder, report.label + "-" + width + "-controlled.png"), null });
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator NativeOneToFourOriginalAiFlightsKeepPrimaryAndBoardInOriginalViews()
    {
        var random = UnityEngine.Random.state;
        using (var prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CornFlightFraming" + Guid.NewGuid().ToString("N") }))
        {
            Registry("ForgetCache"); var isolation = new GameObject("Corn natural flight original AI isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
            folder = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/CornholeNaturalFlightFramingNativeV4"); Directory.CreateDirectory(folder);
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindAnyObjectByType(Corn("CornholeMatch")); courses = ((Array)Get(match, "courses")).Cast<object>().ToArray();
                string[] bodies = { "truck.monster", "novelty.icecream", "truck.lifted", "truck.fire" };
                for (int i = 0; i < 4; i++) { string id = (string)Get(Registry("Create", "Flight Driver " + (i + 1)), "id"); Assert.That((bool)Registry("UseInSeat", i, id), Is.True); var draft = Registry("GetAppearance", id); Set(draft, "bodyId", bodies[i]); Registry("TrySetAppearance", id, draft); }
                Call(match, "RefreshParticipants", false); var presets = Get(match, "weatherPresets"); var clear = ((Array)Get(presets, "entries")).Cast<object>().First(e => (string)Get(e, "displayName") == "Clear"); Call(presets, "Select", Get(clear, "id"));
                var sampler = isolation.AddComponent<CornholeNativeFinalCameraSampler>();
                sampler.AfterCamera = () => { try { SampleFinalCameras(); } catch (Exception failure) { sampleFailure = failure; sampler.enabled = false; } };
                foreach (bool split in new[] { false, true }) for (int count = 1; count <= 4; count++)
                {
                    Call(match, "ChangeSetup"); Set(Get(match, "setup"), "count", count); Call(match, "AllAI"); foreach (var player in ((Array)Get(Get(match, "setup"), "players")).Cast<object>()) Set(player, "difficulty", Enum.Parse(Get(player, "difficulty").GetType(), "Hard"));
                    Sports("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { split }); UnityEngine.Random.InitState(174207);
                    active = new CaseReport { label = (split ? "original-cab-split-" : "shared-external-") + count, count = count, split = split }; reports.Add(active); Array.Clear(nextSample, 0, nextSample.Length); sampleFailure = null; sampler.enabled = true;
                    Assert.That((bool)Call(match, "StartMatch"), Is.True); float deadline = Time.realtimeSinceStartup + 25; bool seenFlight = false;
                    while (Time.realtimeSinceStartup < deadline)
                    {
                        seenFlight |= courses.Take(count).Any(Flying);
                        if (sampleFailure != null) throw new InvalidOperationException("Final native camera diagnostic failed before assertions.", sampleFailure);
                        if (seenFlight && courses.Take(count).All(c => (int)Get(c, "Attempts") > 1 || Get(c, "Phase").ToString() != "Approach" && !Flying(c))) break;
                        yield return null;
                    }
                    if (sampleFailure != null) throw new InvalidOperationException("Final native camera diagnostic failed before assertions.", sampleFailure);
                    active.actualLaunches = courses.Take(count).Select(c => (int)Get(((Array)Get(c, "trucks")).GetValue(0), "LaunchCount")).ToArray();
                    File.WriteAllText(Path.Combine(folder, active.label + ".json"), JsonUtility.ToJson(active, true));
                    TestContext.WriteLine("CORN_NATURAL_FRAMING " + active.label + " launchCounts=" + string.Join(",", active.actualLaunches) + " finalCameraSamples=" + active.samples.Count + " captured=" + active.captured);
                    foreach (var sample in active.samples.Where(s => s.spectating)) foreach (var b in sample.bodies.Where(b => b.participant == sample.viewOwner && (b.viewportMax.y > 1 || b.viewportMin.y < 0))) TestContext.WriteLine("CORN_PRIMARY_CLIPPED_CONTROLLED_NATIVE_PROJECTION count=" + count + " frame=" + sample.frame + " slot=" + b.participant + " flightAge=" + b.flightAge + " body=" + b.bodyId + " bodyViewport=" + b.viewportMin.ToString("R") + ".." + b.viewportMax.ToString("R") + " camera=" + sample.cameraPosition.ToString("R") + " board=" + sample.boardCenterViewport.ToString("R") + " corners=" + b.cornersInside + "/" + b.totalCorners);
                    active = null;
                }
                // Persist every original 1-4 shared/split case before asserting.
                foreach (var report in reports)
                {
                    Assert.That(report.actualLaunches.All(n => n == 1), Is.True, report.label + " must use original natural trampoline launches."); Assert.That(report.samples, Is.Not.Empty); Assert.That(report.captured, Is.True);
                    if (report.split) { Assert.That(report.samples.Select(s => s.viewOwner).Distinct().Count(), Is.EqualTo(report.count), "Every original split owner must contribute actual render evidence."); Assert.That(report.samples.All(s => !s.spectating && !s.replay), Is.True, "Original first-person split behavior remains intentionally exempt from exterior-body visibility."); continue; }
                    Assert.That(report.samples.Any(frame => frame.bodies.Any(body => body.participant == frame.viewOwner && body.flightAge >= 2.5f)), Is.True, "The actual focused course0 projectile must be observed through natural flight.");
                    // Original all-AI spectator focuses its own course0 current.
                    // Other concurrent AI trucks remain diagnostic measurements;
                    // the original contract does not force every unfocused actor
                    // into the same shot or enlarge miniature art to six percent.
                    foreach (var frame in report.samples) foreach (var body in frame.bodies.Where(body => body.participant == frame.viewOwner))
                    {
                        Assert.That(body.viewportMin.z, Is.GreaterThan(0)); Assert.That(body.cornersInside, Is.EqualTo(body.totalCorners), report.label + " actual airborne body clipped slot=" + body.participant + " age=" + body.flightAge + " viewport=" + body.viewportMin + ".." + body.viewportMax);
                        float stockHeight = body.stockViewportMax.y - body.stockViewportMin.y;
                        // Global guidance suggests avoiding <4% when appropriate;
                        // Cornhole deliberately uses correct miniature stock art.
                        // Retain its actual original projected body height where
                        // that baseline is smaller, rather than inventing a zoom.
                        float minimumReadable = Mathf.Min(.04f, stockHeight);
                        Assert.That(body.viewportMax.y - body.viewportMin.y + .00001f, Is.GreaterThanOrEqualTo(minimumReadable), "The primary miniature must retain its original stock-camera readability; selected=" + (body.viewportMax.y - body.viewportMin.y) + " stock=" + stockHeight);
                        Assert.That(frame.boardCenterViewport.z > 0 && frame.boardCenterViewport.x >= 0 && frame.boardCenterViewport.x <= 1 && frame.boardCenterViewport.y >= 0 && frame.boardCenterViewport.y <= 1, Is.True, "The real target board must remain sensibly visible with live flight.");
                    }
                }
            }
            finally { active = null; Time.timeScale = 1; if (match != null) Call(match, "ChangeSetup"); UnityEngine.Object.DestroyImmediate(isolation); UnityEngine.Random.state = random; Registry("ForgetCache"); }
        }
    }
}

// Test-only final-pose reader; never writes a vehicle or camera transform.
[DefaultExecutionOrder(32000)]
public sealed class CornholeNativeFinalCameraSampler : MonoBehaviour
{
    public Action AfterCamera;
    void LateUpdate() => AfterCamera?.Invoke();
}
#endif
