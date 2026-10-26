#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FloorAppearanceFramingTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object target, string name) => target.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(target) : target.GetType().GetProperty(name, Any).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Any).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] != null ? p.ParameterType.IsInstanceOfType(args[i]) : !p.ParameterType.IsValueType || Nullable.GetUnderlyingType(p.ParameterType) != null).All(compatible => compatible)).Invoke(target, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    static bool Visible(Renderer r) => r != null && (r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled && r.gameObject.activeInHierarchy && r.transform.lossyScale.sqrMagnitude > 1e-8f && r.bounds.size.sqrMagnitude > 1e-8f;
    static string FullPath(Transform t) => t.parent == null ? t.name : FullPath(t.parent) + "/" + t.name;
    static Vector3[] Corners(Renderer r) => Enumerable.Range(0, 8).Select(i => r.localToWorldMatrix.MultiplyPoint3x4(r.localBounds.center + Vector3.Scale(r.localBounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)))).ToArray();
    [Serializable] sealed class RendererInfo { public string path; public Vector3 min, max, scale; public bool visible, underOriginalVisuals, contained; }
    [Serializable] sealed class SlotInfo { public int slot; public string body, originalVisuals, appearanceRoot; public Vector3 envelopeMin, envelopeMax, composedMin, composedMax; public RendererInfo[] composed, exterior; public int outsideComposed, outsideVisibleExterior, invisibleExterior; }
    [Serializable] sealed class ProjectionInfo { public int slot, camera; public bool owner, primary; public Vector3 cameraPosition, cameraEuler, actualViewportMin, actualViewportMax; public Rect cameraRect, framingProjected; public float fov, aspect, near; public int cornersInsideFrustum, totalCorners; }
    [Serializable] sealed class Report { public string label; public int frame, primary; public float clock; public bool ownsShot; public SlotInfo[] slots; public ProjectionInfo[] projections; }
    static RendererInfo Inspect(Renderer r, Bounds envelope, Transform original) => new RendererInfo { path = FullPath(r.transform), min = r.bounds.min, max = r.bounds.max, scale = r.transform.lossyScale, visible = Visible(r), underOriginalVisuals = r.transform.IsChildOf(original), contained = envelope.Contains(r.bounds.min) && envelope.Contains(r.bounds.max) };
    static Report Measure(Component match, object framing, Component[] trucks, string label)
    {
        var report = new Report { label = label, frame = Time.frameCount, clock = (float)Get(Get(match, "rules"), "Clock"), primary = (int)Get(framing, "Primary"), ownsShot = (bool)Get(framing, "OwnsShot"), slots = new SlotInfo[8] };
        var cameras = ((Camera[])Get(match, "cameras")).Where(c => c != null && c.enabled && c.gameObject.activeInHierarchy).ToArray();
        var views = Get(match, "drivingViews"); var rally = Get(views, "rally"); int[] owners = (int[])Get(rally, "owners");
        var projections = new System.Collections.Generic.List<ProjectionInfo>();
        for (int i = 0; i < trucks.Length; i++)
        {
            var host = Get(trucks[i], "appearanceHost"); var root = (Transform)Get(host, "AppearanceRoot"); var original = (Transform)Get(trucks[i], "visuals");
            var visible = root.GetComponentsInChildren<Renderer>(true).Where(Visible).ToArray(); Assert.That(visible, Is.Not.Empty);
            var envelope = (Bounds)Call(framing, "TruckBounds", i); envelope.Expand(.1f);
            var composed = visible[0].bounds; foreach (var r in visible.Skip(1)) composed.Encapsulate(r.bounds);
            var body = visible.Select(r => Inspect(r, envelope, original)).ToArray(); var exterior = ((Renderer[])Get(host, "ExteriorRenderers")).Where(r => r != null).Select(r => Inspect(r, envelope, original)).ToArray();
            report.slots[i] = new SlotInfo { slot = i, body = (string)Get(host, "EffectiveBodyId"), originalVisuals = FullPath(original), appearanceRoot = FullPath(root), envelopeMin = envelope.min, envelopeMax = envelope.max, composedMin = composed.min, composedMax = composed.max, composed = body, exterior = exterior, outsideComposed = body.Count(r => !r.contained), outsideVisibleExterior = exterior.Count(r => r.visible && !r.contained), invisibleExterior = exterior.Count(r => !r.visible) };
            var corners = visible.SelectMany(Corners).ToArray();
            for (int v = 0; v < cameras.Length; v++)
            {
                var camera = cameras[v]; var points = corners.Select(camera.WorldToViewportPoint).ToArray(); var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                int inside = corners.Count(point => planes.All(plane => plane.GetDistanceToPoint(point) >= 0));
                projections.Add(new ProjectionInfo { slot = i, camera = v, owner = !(bool)Get(views, "NeedsArena") && v < owners.Length && owners[v] == i, primary = report.primary == i, cameraPosition = camera.transform.position, cameraEuler = camera.transform.eulerAngles, actualViewportMin = new Vector3(points.Min(p => p.x), points.Min(p => p.y), points.Min(p => p.z)), actualViewportMax = new Vector3(points.Max(p => p.x), points.Max(p => p.y), points.Max(p => p.z)), cameraRect = camera.rect, framingProjected = (Rect)Call(framing, "ProjectedTruckRect", camera, i), fov = camera.fieldOfView, aspect = camera.aspect, near = camera.nearClipPlane, cornersInsideFrustum = inside, totalCorners = corners.Length });
            }
        }
        report.projections = projections.ToArray(); return report;
    }
    static void Save(Report report, Component match, string folder)
    {
        Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, report.label + ".json"), JsonUtility.ToJson(report, true));
        var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>(); var cameras = ((Camera[])Get(match, "cameras")).Where(c => c != null && c.enabled).ToArray();
        var capture = Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 6);
        foreach (int width in new[] { 1280, 1920 }) capture.Invoke(null, new object[] { canvas, cameras, width, width == 1280 ? 720 : 1080, Path.Combine(folder, report.label + "-" + width + "-controlled.png"), null });
        foreach (var slot in report.slots) TestContext.WriteLine("FLOOR_FRAMING slot=" + slot.slot + " body=" + slot.body + " envelope=" + slot.envelopeMin.ToString("R") + ".." + slot.envelopeMax.ToString("R") + " composed=" + slot.composedMin.ToString("R") + ".." + slot.composedMax.ToString("R") + " outsideComposed=" + slot.outsideComposed + " outsideVisibleExterior=" + slot.outsideVisibleExterior + " invisibleExterior=" + slot.invisibleExterior);
        foreach (var projection in report.projections.Where(p => p.primary || p.owner)) TestContext.WriteLine("FLOOR_CAMERA slot=" + projection.slot + " view=" + projection.camera + " primary=" + projection.primary + " owner=" + projection.owner + " actual=" + projection.actualViewportMin.ToString("R") + ".." + projection.actualViewportMax.ToString("R") + " framing=" + projection.framingProjected + " inside=" + projection.cornersInsideFrustum + "/" + projection.totalCorners);
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator NativeEightActualBodiesAreIncludedByOriginalFramingInSharedAndSplitViews()
    {
        using (var prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "FloorAppearanceFraming" + Guid.NewGuid().ToString("N") }))
        {
            Registry("ForgetCache"); var isolation = new GameObject("Floor framing original AI input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); Component match = null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckFloorIsLava.LavaMatch, Assembly-CSharp", true));
                Set(Get(match, "setup"), "count", 8); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                string[] bodies = { "truck.monster", "novelty.icecream", "truck.lifted", "truck.fire", "novelty.hotdog", "semi.lowrider", "semi.standard", "truck.monster" }; var ids = new string[8];
                for (int i = 0; i < 8; i++) { ids[i] = (string)Get(Registry("Create", "Same Named Lava Driver"), "id"); Assert.That((bool)Registry("UseInSeat", i, ids[i]), Is.True); var draft = Registry("GetAppearance", ids[i]); Set(draft, "bodyId", bodies[i]); Registry("TrySetAppearance", ids[i], draft); }
                Call(match, "RefreshParticipants", false); Call(match, "AllBots"); var trucks = (Component[])((Array)Get(match, "trucks")).Cast<Component>().ToArray();
                var framing = UnityEngine.Object.FindObjectsByType(Sports("SportsBroadcastFraming"), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().Single(f => (UnityEngine.Object)Get(f, "session") == match);
                string folder = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/FloorAppearanceFramingNative"); var reports = new System.Collections.Generic.List<Report>();
                foreach (bool split in new[] { false, true })
                {
                    Call(match, "SetupScreen"); Sports("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { split }); Call(Get(match, "drivingViews"), "Select", split ? 1 : 0);
                    Assert.That((bool)Call(match, "StartMatch"), Is.True); float deadline = Time.realtimeSinceStartup + 15;
                    while ((float)Get(Get(match, "rules"), "Clock") < .6f && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That((float)Get(Get(match, "rules"), "Clock"), Is.GreaterThanOrEqualTo(.6f)); Call(match, "Pause", 0, false); Call(Get(match, "menus"), "Show", Enum.Parse(Sports("SportsEventScreen"), "Play")); yield return null;
                    var report = Measure(match, framing, trucks, split ? "split-eight-original-ai" : "shared-eight-original-ai"); Save(report, match, folder); reports.Add(report);
                }
                // Persist BOTH original camera modes and all exact numerical
                // evidence before asserting; old-source failure is diagnostic.
                foreach (var report in reports) foreach (var slot in report.slots) Assert.That(slot.outsideComposed, Is.Zero, report.label + " actual nonzero body geometry is outside the existing framing envelope: slot=" + slot.slot + " body=" + slot.body);
            }
            finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); Registry("ForgetCache"); }
        }
    }
}
#endif
