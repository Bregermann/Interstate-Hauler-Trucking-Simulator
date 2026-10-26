#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Explicit native diagnostic. Camera.Render images are controlled renders, never
// Windows backbuffers. Natural starts and recorded-target camera A/B are separate.
public sealed class MinefieldStartupCameraDiagnostics
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static string V(Vector3 value) => value.ToString("F5");

    sealed class Sample
    {
        public Vector3 position, euler, aim, target;
        public Quaternion rotation;
        public int missing, behindStart, belowTerrain, focus;
        public float age, fov, aspect;
        public string phase, solids, rays;
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator StartupCornerRaysSeparateTerrainEdgesFromCameraIntersection()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "MinefieldStartupCameraDiagnostic" });
        var isolation = new GameObject("Minefield camera diagnostic input isolation");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Behaviour match = null; Camera diagnosticCamera = null; float originalAspect = 0;
        string folder = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/MinefieldStartupCameraNativeTargetSnap");
        Directory.CreateDirectory(folder);
        var report = new StringBuilder("DIAGNOSTIC ONLY: natural AI starts with an explicit 16:9 native diagnostic lens; separately labeled recorded-worst-target camera A/B; controlled renders, no Windows or human acceptance. Natural worst/settled captures replay the recorded camera pose against the unchanged terrain at the end of that observation, without rewinding trucks or physics. A/B snaps the existing rig to the exact recorded target, offset and FOV; the current live focus slot is separately recorded and is not rewritten.\n");
        var rows = new StringBuilder("case\tage\tphase\tfocus\tposition\teuler\taim\ttarget\tfov\taspect\tmissingLowerTerrainRays\tbehindTerrainStart\tcameraBelowTerrain\tsolids\n");
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMinefield/Scenes/TruckMinefield.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckMinefield.MinefieldMatch, Assembly-CSharp", true)).Cast<Behaviour>().Single();
            var rally = Get(match, "views"); var broadcast = Get(rally, "broadcast"); var camera = (Camera)Get(broadcast, "view");
            diagnosticCamera = camera; originalAspect = camera.aspect; camera.aspect = 16f / 9;
            var terrains = (Terrain[])Get(match, "grounds");
            Assert.That(terrains.Length, Is.EqualTo(4));
            Assert.That(Get(broadcast, "broadcastFraming"), Is.Null, "This diagnostic targets the original RallyViews camera path.");
            float startZ = terrains.Min(terrain => terrain.transform.position.z);
            report.AppendLine("TERRAIN_START_Z=" + startZ.ToString("F5", CultureInfo.InvariantCulture));
            foreach (var terrain in terrains) report.AppendLine("TERRAIN " + terrain.name + " origin=" + V(terrain.transform.position) + " size=" + V(terrain.terrainData.size));
            var catalog = Get(Get(match, "weatherField"), "presets");
            var clear = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear");
            foreach (string time in new[] { "Day", "Night" })
            {
                Call(match, "SetupScreen"); Call(match, "AllBots"); Call(catalog, "Select", Get(clear, "id"));
                Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { false });
                Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), time) });
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                float began = Time.time, deadline = Time.realtimeSinceStartup + 20; Sample worst = null, settled = null; int samples = 0, focusChanges = 0, priorFocus = -1;
                // Ordinary frames allow the existing Update camera path to run.
                // No WaitForEndOfFrame, resolution switches, or camera overrides.
                while (Time.time - began < 5.2f && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    var sample = Inspect(match, broadcast, camera, terrains, startZ, Time.time - began);
                    AppendRow(rows, "natural-" + time, sample); samples++;
                    if (priorFocus >= 0 && sample.focus != priorFocus) focusChanges++;
                    priorFocus = sample.focus;
                    if (sample.phase == "Driving" && (worst == null || sample.missing > worst.missing)) worst = sample;
                    settled = sample;
                }
                Assert.That(samples, Is.GreaterThan(2)); Assert.That(settled.age, Is.GreaterThanOrEqualTo(5), "The diagnostic must reach the actual countdown-to-driving transition.");
                Assert.That(worst, Is.Not.Null, "The real match must enter Driving.");
                report.AppendLine("NATURAL " + time + " samples=" + samples + " focusChanges=" + focusChanges + " worstMissing=" + worst.missing + " worstBehindStart=" + worst.behindStart + " below=" + worst.belowTerrain);
                report.AppendLine("WORST " + time + " " + worst.rays);
                CaptureFixed(match, camera, worst, folder, "natural-" + time + "-worst-fixed");
                CaptureFixed(match, camera, settled, folder, "natural-" + time + "-settled-fixed");
                // Reconstruct the actual recorded target, rather than inventing
                // a different handoff which may not reproduce the measured lag.
                // Synchronous renders do not advance the match or move any body.
                var target = (Transform)Get(broadcast, "target"); var aim = (Transform)Get(broadcast, "aim");
                var priorTarget = target.position; var priorAim = aim.position;
                var priorPosition = camera.transform.position; var priorRotation = camera.transform.rotation;
                try
                {
                    Call(broadcast, "Pose", worst.target, new Vector3(0, 16, -24), worst.fov, true);
                    var snapped = Inspect(match, broadcast, camera, terrains, startZ, worst.age);
                    AppendRow(rows, "recorded-worst-target-snap-" + time, snapped);
                    report.AppendLine("RECORDED_TARGET_AB " + time + " referenceFocus=" + worst.focus + " liveFocus=" + snapped.focus + " target=" + V(worst.target) + " recordedPosition=" + V(worst.position) + " recordedEuler=" + V(worst.euler) + " snappedPosition=" + V(snapped.position) + " snappedEuler=" + V(snapped.euler) + " recordedMissing=" + worst.missing + " recordedBehindStart=" + worst.behindStart + " snappedMissing=" + snapped.missing + " snappedBehindStart=" + snapped.behindStart + " snappedBelow=" + snapped.belowTerrain + " snappedSolids=" + snapped.solids);
                    report.AppendLine("RECORDED_TARGET_SNAPPED_RAYS " + time + " " + snapped.rays);
                    CaptureFixed(match, camera, snapped, folder, "recorded-worst-target-snap-" + time + "-fixed");
                    Assert.That(Vector3.Distance(snapped.target, worst.target), Is.LessThan(.0001f), "Compare exactly the recorded natural target.");
                    Assert.That(snapped.belowTerrain, Is.Zero);
                    if (worst.missing > 0) Assert.That(snapped.missing, Is.Zero, "The candidate camera snap must clear the reproduced terrain-edge rays before a production fix is considered.");
                }
                finally { target.position = priorTarget; aim.position = priorAim; camera.transform.SetPositionAndRotation(priorPosition, priorRotation); }
            }
            report.AppendLine("DIAGNOSTIC_FINISHED. A passing test confirms evidence collection only; inspect rays and images before choosing a production fix.");
            TestContext.WriteLine(report.ToString()); TestContext.WriteLine("Evidence folder: " + folder);
        }
        finally
        {
            File.WriteAllText(Path.Combine(folder, "samples.tsv"), rows.ToString()); File.WriteAllText(Path.Combine(folder, "diagnosis.txt"), report.ToString());
            if (diagnosticCamera != null) diagnosticCamera.aspect = originalAspect;
            Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose();
        }
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator OriginalPresentationKeepsGroundVisibleAcrossStartsRematchesAndReplayReturn()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "MinefieldStartupCameraRegression" });
        var isolation = new GameObject("Minefield camera regression input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Behaviour match = null; Camera camera = null; float originalAspect = 0;
        string folder = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/MinefieldStartupCameraRegression"); Directory.CreateDirectory(folder);
        var report = new StringBuilder("REGRESSION: original AI starts/rematches; original recorded transforms plus explicitly invoked LockHeat(false) for a bounded replay-return lifecycle fixture. No scores or finishing outcomes injected. Native controlled camera renders, no Windows/human/device acceptance.\n");
        var rows = new StringBuilder("case\tage\tphase\tfocus\tposition\teuler\taim\ttarget\tfov\taspect\tmissingLowerTerrainRays\tbehindTerrainStart\tcameraBelowTerrain\tsolids\n");
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMinefield/Scenes/TruckMinefield.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckMinefield.MinefieldMatch, Assembly-CSharp", true)).Cast<Behaviour>().Single();
            var rally = Get(match, "views"); var broadcast = Get(rally, "broadcast"); camera = (Camera)Get(broadcast, "view");
            originalAspect = camera.aspect; camera.aspect = 16f / 9;
            var terrains = (Terrain[])Get(match, "grounds"); float startZ = terrains.Min(terrain => terrain.transform.position.z);
            var catalog = Get(Get(match, "weatherField"), "presets"); var clear = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear");
            var follow = Get(broadcast, "follow");
            Assert.That((float)Get(follow, "m_XDamping"), Is.EqualTo(.25f)); Assert.That((float)Get(follow, "m_YDamping"), Is.EqualTo(.2f)); Assert.That((float)Get(follow, "m_ZDamping"), Is.EqualTo(.25f));
            foreach (string time in new[] { "Day", "Night" }) for (int rematch = 0; rematch < 2; rematch++)
            {
                Call(match, "SetupScreen"); Call(match, "AllBots"); Call(catalog, "Select", Get(clear, "id"));
                Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null, new object[] { false }); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), time) });
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                string label = time + "-rematch" + rematch; float began = Time.time, deadline = Time.realtimeSinceStartup + 25; int samples = 0, handoffs = 0, priorFocus = -1; bool goCaptured = false;
                while (Time.time - began < 4.3f && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    var sample = Inspect(match, broadcast, camera, terrains, startZ, Time.time - began); AppendRow(rows, label, sample); samples++;
                    if (priorFocus >= 0 && priorFocus != sample.focus) handoffs++; priorFocus = sample.focus;
                    RequireGround(sample, label);
                    if (!goCaptured && sample.phase == "Driving") { goCaptured = true; CaptureFixed(match, camera, sample, folder, label + "-go-fixed"); }
                }
                Assert.That(goCaptured, Is.True, "Observe actual GO, not just the held countdown."); Assert.That(Time.time - began, Is.GreaterThanOrEqualTo(4));
                Assert.That((int)Get(rally, "Humans"), Is.Zero); Assert.That(((Camera[])Get(match, "cameras")).Single(), Is.SameAs(camera));
                report.AppendLine(label + " samples=" + samples + " naturalSubjectChanges=" + handoffs + " all15lowerRaysHitTerrain=true; original vendor damping .25/.20/.25 retained.");
                if (rematch != 1) continue;
                // Exercise the existing replay owner using real recorded poses.
                // The explicit false argument ends no scoring heat and awards no points.
                int oldHeat = (int)Get(match, "Heat"); var points = (int[])Get(match, "points"); var savedPoints = (int[])points.Clone();
                Call(match, "LockHeat", false); Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("Replay")); Assert.That((bool)Get(Get(match, "replay"), "Playing"), Is.True);
                bool sawReplay = false; deadline = Time.realtimeSinceStartup + 20;
                while (Get(match, "Phase").ToString() == "Replay" && Time.realtimeSinceStartup < deadline) { yield return null; sawReplay = true; }
                Assert.That(sawReplay, Is.True); Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("HeatResult"));
                var returned = Inspect(match, broadcast, camera, terrains, startZ, 0); AppendRow(rows, label + "-replay-return", returned); RequireGround(returned, label + " replay return"); CaptureFixed(match, camera, returned, folder, label + "-replay-return-fixed");
                while ((int)Get(match, "Heat") == oldHeat && Time.realtimeSinceStartup < deadline)
                {
                    yield return null; var sample = Inspect(match, broadcast, camera, terrains, startZ, 0); AppendRow(rows, label + "-post-replay", sample); RequireGround(sample, label + " post replay");
                }
                Assert.That((int)Get(match, "Heat"), Is.EqualTo(oldHeat + 1)); Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("Countdown"));
                CollectionAssert.AreEqual(savedPoints, points, "This replay camera fixture does not invent heat points.");
                report.AppendLine(label + " original replay return and next-heat acquisition retained all15lowerTerrainRays; point totals unchanged.");
            }
            TestContext.WriteLine(report.ToString()); TestContext.WriteLine("Evidence folder: " + folder);

            void RequireGround(Sample sample, string label)
            {
                if (sample.missing != 0 || sample.belowTerrain != 0) { report.AppendLine("FAILED " + label + " missing=" + sample.missing + " below=" + sample.belowTerrain + " " + sample.rays); CaptureFixed(match, camera, sample, folder, label.Replace(' ', '-') + "-failure-fixed"); }
                Assert.That(sample.missing, Is.Zero, label + " lower viewport must show authored terrain, not the rear sky gap. " + sample.rays); Assert.That(sample.belowTerrain, Is.Zero, label);
            }
        }
        finally
        {
            File.WriteAllText(Path.Combine(folder, "samples.tsv"), rows.ToString()); File.WriteAllText(Path.Combine(folder, "regression.txt"), report.ToString());
            if (camera != null) camera.aspect = originalAspect; Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose();
        }
    }

    static Sample Inspect(Component match, object broadcast, Camera camera, Terrain[] terrains, float startZ, float age)
    {
        var sample = new Sample { age = age, phase = Get(match, "Phase").ToString(), focus = (int)Get(match, "focus"), position = camera.transform.position, rotation = camera.transform.rotation, euler = camera.transform.eulerAngles, aim = ((Transform)Get(broadcast, "aim")).position, target = ((Transform)Get(broadcast, "target")).position, fov = camera.fieldOfView, aspect = camera.aspect };
        foreach (var terrain in terrains)
        {
            var min = terrain.transform.position; var max = min + terrain.terrainData.size;
            if (sample.position.x >= min.x && sample.position.x <= max.x && sample.position.z >= min.z && sample.position.z <= max.z && sample.position.y < terrain.SampleHeight(sample.position) + min.y + .02f) sample.belowTerrain++;
        }
        sample.solids = string.Join("|", Physics.OverlapSphere(sample.position, .35f, 1, QueryTriggerInteraction.Ignore).Select(collider => collider.name).Distinct());
        var detail = new StringBuilder();
        foreach (float y in new[] { .02f, .08f, .16f }) foreach (float x in new[] { .02f, .15f, .5f, .85f, .98f })
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(x, y, 0)); bool hit = false; float distance = float.PositiveInfinity; string name = "NONE";
            foreach (var terrain in terrains) if (terrain.GetComponent<TerrainCollider>().Raycast(ray, out var contact, 10000) && contact.distance < distance) { hit = true; distance = contact.distance; name = terrain.name; }
            bool planeHit = new Plane(Vector3.up, new Vector3(0, terrains[0].transform.position.y, 0)).Raycast(ray, out float enter);
            var plane = planeHit ? ray.GetPoint(enter) : Vector3.positiveInfinity;
            if (!hit) sample.missing++; if (!hit && planeHit && plane.z < startZ) sample.behindStart++;
            detail.Append("uv=").Append(x.ToString("F2", CultureInfo.InvariantCulture)).Append(',').Append(y.ToString("F2", CultureInfo.InvariantCulture)).Append(" terrain=").Append(name).Append(" plane=").Append(V(plane)).Append("; ");
        }
        sample.rays = detail.ToString(); return sample;
    }

    static void AppendRow(StringBuilder rows, string label, Sample sample)
    {
        rows.Append(label).Append('\t').Append(sample.age.ToString("F5", CultureInfo.InvariantCulture)).Append('\t').Append(sample.phase).Append('\t').Append(sample.focus).Append('\t').Append(V(sample.position)).Append('\t').Append(V(sample.euler)).Append('\t').Append(V(sample.aim)).Append('\t').Append(V(sample.target)).Append('\t').Append(sample.fov.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(sample.aspect.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(sample.missing).Append('\t').Append(sample.behindStart).Append('\t').Append(sample.belowTerrain).Append('\t').Append(sample.solids).AppendLine();
    }

    static void CaptureFixed(Component match, Camera camera, Sample sample, string folder, string label)
    {
        var position = camera.transform.position; var rotation = camera.transform.rotation; float aspect = camera.aspect, fov = camera.fieldOfView;
        var canvas = ((Component)Get(match, "menus")).GetComponent<Canvas>();
        var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 6);
        try
        {
            foreach (int width in new[] { 1280, 1920 })
            {
                camera.transform.SetPositionAndRotation(sample.position, sample.rotation); camera.fieldOfView = sample.fov; camera.aspect = 16f / 9;
                save.Invoke(null, new object[] { canvas, new[] { camera }, width, width == 1280 ? 720 : 1080, Path.Combine(folder, label + "-" + width + "-controlled.png"), null });
            }
        }
        finally { camera.transform.SetPositionAndRotation(position, rotation); camera.aspect = aspect; camera.fieldOfView = fov; }
    }
}
#endif
