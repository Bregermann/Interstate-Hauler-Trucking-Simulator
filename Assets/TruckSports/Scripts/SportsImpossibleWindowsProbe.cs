#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LWS.TruckSports
{
    // A short actual-player integration tour, separate from full native match
    // acceptance. Uses the existing preference, navigation and capture adapters.
    public sealed class SportsImpossibleWindowsProbe : MonoBehaviour
    {
        const string Flag = "-tsImpossibleOpenings", Output = "-tsImpossibleOutput";
        readonly StringBuilder report = new StringBuilder();
        IDisposable preferences;
        string folder, current;
        int checks, errors, visited, captures;
        float began;
        bool finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) < 0) return;
            if (Application.isEditor || Application.platform != RuntimePlatform.WindowsPlayer)
            { Debug.LogError("Impossible openings require the actual Windows development player."); return; }
            var root = new GameObject("Explicit Impossible Windows saved-scene tour");
            DontDestroyOnLoad(root);
            if (!SportsProbeInputIsolation.Active) root.AddComponent<SportsProbeInputIsolation>();
            root.AddComponent<SportsImpossibleWindowsProbe>();
        }

        void Check(bool pass, string message)
        {
            checks++; if (!pass) errors++;
            report.AppendLine((pass ? "PASS " : "FAIL ") + current + " " + message);
            File.WriteAllText(Path.Combine(folder, "progress.txt"), report.ToString());
        }
        void Error(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            errors++; report.AppendLine("ERROR " + current + " " + message + "\n" + stack);
            if (folder != null) File.WriteAllText(Path.Combine(folder, "progress.txt"), report.ToString());
        }
        IEnumerator Start()
        {
            began = Time.realtimeSinceStartup;
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, Output);
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Provide an explicit -tsImpossibleOutput.");
            folder = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(folder);
            if (File.Exists(Path.Combine(folder, "result.txt"))) throw new IOException("Preserve the previous Windows receipt; use a new output folder.");
            File.WriteAllText(Path.Combine(folder, "result.txt"), "RUNNING " + DateTime.UtcNow.ToString("o"));
            preferences = SportsPrefs.IsolateValidation("ImpossibleWindowsOpenings");
            SportsParticipantRegistry.ForgetCache(); SportsCameraOptions.Set(false);
            Application.logMessageReceived += Error;
            SceneManager.sceneLoaded += FreshWorkshopStorageBeforeRepositoryStart;
            foreach (var entry in SportsImpossibleGames.All)
            {
                current = entry.scene;
                Check(Application.CanStreamedLevelBeLoaded(entry.scene), "actual scene is included in this executable");
                if (!Application.CanStreamedLevelBeLoaded(entry.scene)) continue;
                SportsNavigation.LoadSport(entry.scene);
                yield return null; yield return null; yield return new WaitForSecondsRealtime(.5f);
                var match = FindAnyObjectByType<SportsEventSession>();
                Check(match != null && SceneManager.GetActiveScene().name == entry.scene, "existing navigation opens correct saved authority");
                if (match == null) continue;
                visited++;
                var artwork = match.menus.pages[0].GetComponentsInChildren<Image>(true)
                    .Where(item => item.name == "Original sport title artwork").ToArray();
                Check(artwork.Length == 1 && artwork[0].sprite != null && artwork[0].sprite.name == entry.scene,
                    "setup has the actual original sport logo");
                yield return Capture("setup");
                bool started = false;
                try
                {
                    match.setup.count = Mathf.Clamp(4, match.MinimumPlayers, match.MaximumPlayers);
                    match.NormalizeSetup(); match.AllBots(); started = match.StartMatch();
                }
                catch (Exception failure) { Error(failure.Message, failure.StackTrace, LogType.Exception); }
                Check(started, "ordinary AI setup starts the actual match");
                if (started)
                {
                    string matchId = match.MatchId;
                    yield return new WaitForSecondsRealtime(7);
                    Check(match.Running || match.Finished, "actual match remains in a valid gameplay/results state");
                    Check(match.cameras != null && match.cameras.Any(camera => camera != null && camera.isActiveAndEnabled && camera.targetTexture == null && camera.rect.width > 0 && camera.rect.height > 0), "actual active camera renders into the player backbuffer");
                    Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Count(item => item.isActiveAndEnabled) == 1,
                        "one active listener after real scene transition");
                    var rigs = FindObjectsByType<SportsEventTruck>(FindObjectsInactive.Include)
                        .Where(truck => truck.session == match && truck.slot >= 0 && truck.slot < match.setup.count).ToArray();
                    if (rigs.Length > 0)
                        Check(rigs.Length >= match.setup.count && rigs.All(truck => truck.appearanceHost != null && truck.appearanceHost.ProfileId == match.setup.players[truck.slot].participantId),
                            "original participating rigs retain stable appearance owners");
                    else
                        report.AppendLine("SCOPE " + current + " board presentation has no driving-rig acceptance claim.");
                    report.AppendLine("OPENING_STATE " + current + " " + match.Header.Replace('\n', ' ') + " | " + match.Status.Replace('\n', ' '));
                    yield return Capture("natural-gameplay");
                    if (!match.Finished)
                    {
                        match.Pause(0, false); yield return new WaitForSecondsRealtime(.2f);
                        Check(match.Paused && !match.Live, "original pause gates this live match");
                        yield return Capture("pause");
                        match.Resume(); yield return null;
                        Check(!match.Paused, "original resume restores gameplay");
                    }
                    match.SetupScreen();
                    bool rematched = false;
                    try { rematched = match.StartMatch(); }
                    catch (Exception failure) { Error(failure.Message, failure.StackTrace, LogType.Exception); }
                    Check(rematched && match.MatchId != matchId, "ordinary rematch starts a distinct match");
                    yield return new WaitForSecondsRealtime(.3f); match.SetupScreen();
                    Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Count(item => item.isActiveAndEnabled) == 1,
                        "one active listener after ordinary rematch");
                }
                var oldScene = match.gameObject.scene;
                SportsNavigation.FrontEnd(); yield return null; yield return null; yield return new WaitForSecondsRealtime(.2f);
                Check(FindAnyObjectByType<SportsFrontEnd>() != null && !oldScene.isLoaded, "returns to original collection and unloads prior sport");
                Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Count(item => item.isActiveAndEnabled) == 1,
                    "one active listener after collection return");
            }
            current = "collection"; yield return Capture("return"); Finish(false);
        }
        IEnumerator Capture(string state)
        {
            foreach (int width in new[] { 1280, 1920 })
            {
                int height = width == 1280 ? 720 : 1080;
                string path = Path.Combine(folder, current + "-" + state + "-" + width + ".png");
                yield return SportsWindowCapture.Save(width, height, path);
                bool valid = Screen.width == width && Screen.height == height && ActualPngSize(path, width, height);
                Check(valid, state + " has actual " + width + "x" + height + " backbuffer and PNG dimensions");
                if (valid) captures++;
                RecordActualCameraWorld(state + "-" + width);
            }
        }
        [Serializable] sealed class CameraWorldFact
        {
            public string scene, state, cameraName, reason, drivingMode;
            public int frame, primary, cameraId;
            public bool framingOwnsShot;
            public float aspect, fov, projectedHeight;
            public Vector3 position, rotation, subject, objectiveViewport;
            public Rect viewport, projectedSubject;
            public string[] interveningOpaqueMeshBounds;
        }
        // Read-only actual post-backbuffer camera telemetry. A correct rectangle
        // alone is NOT visibility proof; native GPU body witnesses and direct
        // Windows images still decide whether render-only scenery hides it.
        void RecordActualCameraWorld(string state)
        {
            var match = FindAnyObjectByType<SportsEventSession>(); if (match == null || match.gameObject.scene.name != current) return;
            var driving = match.GetComponent<SportsDrivingViews>();
            foreach (var camera in match.cameras.Where(c => c != null && c.isActiveAndEnabled && c.targetTexture == null))
            {
                var framing = camera.GetComponent<SportsBroadcastFraming>(); bool valid = framing != null && framing.Valid(framing.Primary);
                var fact = new CameraWorldFact { scene = current, state = state, frame = Time.frameCount, cameraName = camera.name,
                    cameraId = camera.GetInstanceID(), position = camera.transform.position, rotation = camera.transform.eulerAngles,
                    viewport = camera.rect, aspect = camera.aspect, fov = camera.fieldOfView, primary = valid ? framing.Primary : -1,
                    framingOwnsShot = framing != null && framing.OwnsShot, reason = framing == null ? "NO SHARED ACTOR FRAMING" : framing.ShotReason,
                    drivingMode = driving == null ? "MODE-OWNED CAMERA" : "MODE=" + driving.Mode + " NEEDS_ARENA=" + driving.NeedsArena + " LOCAL_VIEWS=" + driving.rally.Humans };
                if (valid)
                {
                    var bounds = framing.TruckBounds(framing.Primary); fact.subject = bounds.center;
                    fact.projectedSubject = framing.ProjectedTruckRect(camera, framing.Primary); fact.projectedHeight = fact.projectedSubject.height;
                    if (framing.action.hasObjective) fact.objectiveViewport = camera.WorldToViewportPoint(framing.action.objective.center);
                    var ray = new Ray(camera.transform.position, (bounds.center - camera.transform.position).normalized);
                    float distance = Vector3.Distance(camera.transform.position, bounds.center); var actor = framing.Body(framing.Primary).transform;
                    fact.interveningOpaqueMeshBounds = match.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true))
                        .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy && !renderer.forceRenderingOff &&
                            !renderer.transform.IsChildOf(actor) && renderer.GetComponent<TMPro.TMP_Text>() == null &&
                            renderer.sharedMaterials.Any(material => material != null && material.renderQueue < 2500) &&
                            renderer.bounds.IntersectRay(ray, out float near) && near < distance)
                        .OrderBy(renderer => Vector3.Distance(camera.transform.position, renderer.bounds.center)).Take(12)
                        .Select(renderer => renderer.name + " center=" + renderer.bounds.center.ToString("R") + " size=" + renderer.bounds.size.ToString("R") + " cameraInside=" + renderer.bounds.Contains(camera.transform.position)).ToArray();
                }
                File.AppendAllText(Path.Combine(folder, "actual-camera-world.jsonl"), JsonUtility.ToJson(fact) + "\n", Encoding.UTF8);
            }
        }
        static bool ActualPngSize(string path, int width, int height)
        {
            if (!File.Exists(path)) return false;
            var header = new byte[24];
            using (var file = File.OpenRead(path)) if (file.Read(header, 0, header.Length) != header.Length) return false;
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            for (int i = 0; i < signature.Length; i++) if (header[i] != signature[i]) return false;
            if (header[12] != 'I' || header[13] != 'H' || header[14] != 'D' || header[15] != 'R') return false;
            int x = header[16] << 24 | header[17] << 16 | header[18] << 8 | header[19];
            int y = header[20] << 24 | header[21] << 16 | header[22] << 8 | header[23];
            return x == width && y == height;
        }
        void FreshWorkshopStorageBeforeRepositoryStart(Scene scene, LoadSceneMode mode)
        {
            // Match the existing native wilderness fixture: Awake resolves the
            // vendor path without I/O; sceneLoaded precedes repository Start.
            // PlayerPrefs isolation alone does not isolate Pet autosaves.
            foreach (var workshop in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsWorkshopPersistence>(true)))
            {
                if (workshop.storage == null) throw new InvalidOperationException("Actual configured workshop storer required for isolated Windows acceptance.");
                string id, path;
                do
                {
                    id = "ImpossibleWindows_" + Guid.NewGuid().ToString("N");
                    path = Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops", id);
                } while (Directory.Exists(path));
                workshop.modeId = id; workshop.storage.customPath = path;
                report.AppendLine("ISOLATED_WORKSHOP_STORAGE " + scene.name + " " + path);
            }
        }
        void Update()
        {
            if (!finished && folder != null && Time.realtimeSinceStartup - began > 1100)
            { Check(false, "bounded Windows tour exceeded its deadline"); Finish(true); }
        }
        void Finish(bool interrupted)
        {
            if (finished) return; finished = true;
            Application.logMessageReceived -= Error;
            SceneManager.sceneLoaded -= FreshWorkshopStorageBeforeRepositoryStart;
            preferences?.Dispose(); preferences = null; SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1;
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + " SCENES=" + visited + " SCREENSHOTS=" + captures + " INTERRUPTED=" + interrupted);
            report.AppendLine("Actual Windows composed frames, ordinary saved setup/AI opening, pause/resume/rematch/exit only. Full matches have separate native receipts. No physical-controller, listening, human or performance acceptance inferred.");
            File.WriteAllText(Path.Combine(folder, "result.txt"), report.ToString());
            Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Error; preferences?.Dispose(); SportsParticipantRegistry.ForgetCache();
            SceneManager.sceneLoaded -= FreshWorkshopStorageBeforeRepositoryStart;
        }
    }
}
#endif
