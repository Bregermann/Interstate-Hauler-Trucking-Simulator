#if UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LWS.InterstateHauler;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace LWS.TruckTaxi
{
    // Explicit build-only fixture. Gameplay, streaming, recovery and UTS retain their own authorities.
    public sealed class TruckTaxiAddendumRuntimeProbe : MonoBehaviour
    {
        private const string Argument = "-truck-taxi-addendum-probe";
        private const double WorkDeadline = 144, QuitDeadline = 150;
        private const float SampleSeconds = 8;

        [Serializable] private sealed class Sample
        {
            public string phase, status = "not-run", detail, region;
            public int frames, pausedFrames, activeFrames, width, height;
            public int liveCars, logicalCars, livePedestrians, logicalPedestrians, liveRivals, logicalRivals;
            public int venueCrowds, buses, extremePhysics, extremeVisuals, kaijuActors, raceOpponents;
            public int utsDrivers, movingUtsDrivers;
            public float seconds, frameMeanMs, frameP95Ms, fps, meanMainMs, meanPhysicsMs, maxCameraShake;
            public bool mainRecorderValid, physicsRecorderValid;
            public List<string> failures = new List<string>();
        }

        [Serializable] private sealed class Shot
        {
            public string name, file, error, camera, effect, effectEvidence;
            public int requestedWidth, requestedHeight, actualWidth, actualHeight, forcedAlertsPublished;
            public int effectRenderersInFrustum = -1, sampledColorRange;
            public long bytes;
            public bool success, humanReviewed;
            public List<string> visibleAlertTexts = new List<string>();
        }

        [Serializable] private sealed class Report
        {
            public string startedUtc, unityVersion, device, graphics, outputDirectory, initialScene, scene;
            public string status = "running", stage = "startup";
            public string evidence = "Automated runtime measurements only. No human visual observation or FPS acceptance threshold. Counts are sample peaks. World screenshot frustum checks do not prove unoccluded visibility.";
            public string preferenceScope = "Ride requests, radio, shortcut discovery in loaded scenes; other gameplay preferences are not changed. Original screen resolution/mode is restored.";
            public double elapsedSeconds;
            public int exitCode, runtimeErrorCount, preferenceKeysPreserved;
            public bool mainMenuLoaded, freePlayLoaded, preferencesRestored, displayRestored;
            public List<Sample> samples = new List<Sample>();
            public List<Shot> screenshots = new List<Shot>();
            public List<string> failures = new List<string>();
            public List<string> runtimeErrors = new List<string>();
            public List<string> limitations = new List<string>();
        }

        private sealed class Preference
        {
            public string Key, StringValue;
            public int Kind, IntValue;
            public float FloatValue;
            public bool Existed;
        }

        private static bool launched;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        private readonly Dictionary<string, Preference> preferences = new Dictionary<string, Preference>();
        private Report report;
        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld regional;
        private LwsKeyboardGamepadTruckInputSource drivingInput;
        private bool inputWasSuppressed, offerSuppression, startedShift, finishing, quit, travelled;
        private bool originalRunInBackground;
        private int originalWidth, originalHeight;
        private FullScreenMode originalScreenMode;
        private string forcedEventId;
        private int alertsPublished;
        private string ReportPath => Path.Combine(report.outputDirectory, "TruckTaxiAddendumRuntimeProbe.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartIfRequested()
        {
            if (launched || !Array.Exists(Environment.GetCommandLineArgs(),
                arg => string.Equals(arg, Argument, StringComparison.OrdinalIgnoreCase))) return;
            launched = true;
            var runner = new GameObject(nameof(TruckTaxiAddendumRuntimeProbe));
            DontDestroyOnLoad(runner);
            runner.AddComponent<TruckTaxiAddendumRuntimeProbe>().Begin();
        }

        private void Begin()
        {
            clock.Start();
            report = new Report
            {
                startedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                device = SystemInfo.graphicsDeviceName, graphics = SystemInfo.graphicsDeviceType.ToString(),
                outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation",
                    "Addendum_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")))
            };
            foreach (string phase in new[] { "normal-town", "venue-event", "uts-truck-race", "earthquake", "kaiju" })
                report.samples.Add(new Sample { phase = phase });
            report.limitations.Add("150s in-process deadline requires a responsive Unity main thread; use an external process timeout for native hangs.");
            report.limitations.Add("No fullscreen/layout quality approval; unsupported window sizes fail with actual dimensions. Run without -batchmode/-nographics.");
            originalRunInBackground = Application.runInBackground;
            originalWidth = Screen.width; originalHeight = Screen.height; originalScreenMode = Screen.fullScreenMode;
            Application.runInBackground = true;
            SavePreference(TruckTaxiSession.RideRequestsPreferenceKey);
            // Radio persists from OnDestroy, so cleanup unloads it before restoring these keys.
            SavePreference("LWS.TruckTaxi.Radio.On");
            SavePreference("LWS.TruckTaxi.Radio.Volume", 1);
            SavePreference("LWS.TruckTaxi.Radio.Station", 2);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.logMessageReceived += OnLog;
            routines.Push(Run());
            StartCoroutine(Drive());
        }

        // Flatten nested iterators so exceptions cannot silently abandon reporting/cleanup.
        private IEnumerator Drive()
        {
            while (routines.Count > 0 && !finishing)
            {
                object next = null;
                Exception failure = null;
                try
                {
                    var current = routines.Peek();
                    if (!current.MoveNext())
                    { routines.Pop(); (current as IDisposable)?.Dispose(); continue; }
                    next = current.Current;
                    if (next is IEnumerator nested) { routines.Push(nested); continue; }
                }
                catch (Exception error) { failure = error; }
                if (failure != null) { Fail(report.stage + ": " + failure); break; }
                yield return next;
            }
            BeginFinish();
        }

        private void Update()
        {
            if (report == null || quit) return;
            if (!finishing && clock.Elapsed.TotalSeconds >= WorkDeadline)
            { Fail("144s work timeout during " + report.stage); BeginFinish(); }
            else if (finishing && clock.Elapsed.TotalSeconds >= QuitDeadline)
            { Fail("150s deadline reached during cleanup; remaining restoration may be incomplete."); QuitNow(); }
        }

        private IEnumerator Run()
        {
            yield return null;
            Directory.CreateDirectory(report.outputDirectory);
            Debug.Log("TruckTaxi addendum probe artifacts: " + report.outputDirectory);
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith("-truck-taxi-", StringComparison.OrdinalIgnoreCase) &&
                    arg.EndsWith("-probe", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(arg, Argument, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Do not combine runtime probes: " + arg);
            report.initialScene = SceneManager.GetActiveScene().name;
            yield return LoadScene(TruckTaxiMainMenu.SceneName, 12);
            report.mainMenuLoaded = SceneManager.GetActiveScene().name == TruckTaxiMainMenu.SceneName &&
                FindFirstObjectByType<TruckTaxiMainMenu>() != null;
            if (!report.mainMenuLoaded) throw new InvalidOperationException("MainMenu component is missing.");
            yield return null;
            yield return LoadScene(TruckTaxiMainMenu.FreePlaySceneName, 30);
            report.freePlayLoaded = SceneManager.GetActiveScene().name == TruckTaxiMainMenu.FreePlaySceneName;
            report.stage = "bootstrap";
            double readyUntil = Math.Min(clock.Elapsed.TotalSeconds + 40, 55);
            while (!Ready() && clock.Elapsed.TotalSeconds < readyUntil) yield return null;
            if (!Ready()) throw new TimeoutException("Bootstrap readiness timeout: " + MissingDependencies());
            report.scene = host.gameObject.scene.name;
            regional = host.GetComponent<TruckTaxiRegionalWorld>();
            drivingInput = host.Player.GetComponent<LwsKeyboardGamepadTruckInputSource>();
            if (regional == null || drivingInput == null)
                throw new InvalidOperationException("Regional streaming or existing driving input adapter missing.");
            inputWasSuppressed = drivingInput.DrivingInputSuppressed;
            drivingInput.SetDrivingInputSuppressed(true);
            host.Session.AcquireOfferSuppression(this); offerSuppression = true;
            startedShift = host.Session.State == TruckTaxiState.Inactive;
            host.StartShift();
            if (host.Session.State != TruckTaxiState.Available || host.Paused)
                throw new InvalidOperationException("StartShift did not produce unpaused Available: " + host.Session.State);
            host.hud.Alerts.Published += OnAlert;
            // A hidden Windows launch may retain an unpresented initial swap chain until resized.
            yield return Resolution(1280, 720);
            yield return Resolution(1920, 1080);
            if (Screen.width != 1920 || Screen.height != 1080)
                Fail("Performance baseline resolution unavailable; each sample records its actual dimensions.");
            yield return Delay(1);
            host.Extremes.Clear();
            host.Kaiju.EndKaiju();
            yield return null;
            yield return Measure(report.samples[0], () => host.Extremes.ActiveSnapshots.Count == 0 && !host.Kaiju.IsActive && !LocalVenueActive());
            yield return VenuePhase(report.samples[1]);
            yield return RacePhase(report.samples[2]);
            yield return EarthquakePhase(report.samples[3]);
            yield return KaijuPhase(report.samples[4]);
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3440, 1440) })
            {
                yield return Resolution(size.x, size.y);
                yield return Capture("HUD_" + size.x + "x" + size.y, size.x, size.y, "none", null);
            }
        }

        private bool Ready()
        {
            host = TruckTaxiBootstrap.Instance;
            return host != null && host.Ready && host.Player != null && host.Session != null &&
                host.hud != null && host.hud.Root != null && host.hud.Alerts != null &&
                host.WorldCoordinator != null && host.Venues != null && host.Venues.catalog != null &&
                host.Races != null && host.Kaiju != null && host.Kaiju.Configuration != null &&
                host.Extremes != null && host.Rivals != null && host.FastTravel != null &&
                host.traffic != null && host.pedestrians != null;
        }

        private string MissingDependencies() => host == null ? "TruckTaxiBootstrap.Instance=null" :
            "Ready=" + host.Ready + ", Player=" + (host.Player != null) + ", HUD=" + (host.hud?.Alerts != null) +
            ", WorldCoordinator=" + (host.WorldCoordinator != null) + ", Venues=" + (host.Venues?.catalog != null) +
            ", Races=" + (host.Races != null) + ", Kaiju=" + (host.Kaiju?.Configuration != null) +
            ", Extremes=" + (host.Extremes != null) + ", FastTravel=" + (host.FastTravel != null) +
            ", Rivals=" + (host.Rivals != null) + ", traffic=" + (host.traffic != null) + ", pedestrians=" + (host.pedestrians != null);

        private IEnumerator LoadScene(string name, float timeout)
        {
            report.stage = "load " + name;
            if (SceneManager.GetActiveScene().name == name) yield break;
            if (!Application.CanStreamedLevelBeLoaded(name)) throw new InvalidOperationException("Build scene missing: " + name);
            var operation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
            if (operation == null) throw new InvalidOperationException("LoadSceneAsync returned null: " + name);
            double until = clock.Elapsed.TotalSeconds + timeout;
            while (!operation.isDone && clock.Elapsed.TotalSeconds < until) yield return null;
            if (!operation.isDone) throw new TimeoutException(name + " load exceeded " + timeout + "s.");
        }
        private IEnumerator VenuePhase(Sample sample)
        {
            report.stage = sample.phase;
            TruckTaxiVenueDefinition selected = null;
            string eventId = null;
            float nearest = float.PositiveInfinity;
            foreach (var venue in host.Venues.Venues)
            {
                if (venue == null || (venue.type != TruckTaxiVenueType.Stadium && venue.type != TruckTaxiVenueType.ConcertArena)) continue;
                foreach (var item in host.Venues.catalog.events)
                {
                    if (item == null || item.venueId != venue.id) continue;
                    float distance = (venue.worldPosition - host.Player.transform.position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance; selected = venue; eventId = item.id;
                }
            }
            if (selected == null) { Reject(sample, "No authored stadium/concert event in venue catalog."); yield break; }
            sample.detail = selected.id + ":" + eventId;
            Vector3 destination = selected.taxiPickupZones != null && selected.taxiPickupZones.Length > 0
                ? selected.taxiPickupZones[0] : selected.worldPosition;
            yield return Travel(destination, selected.displayName, sample);
            if (!travelled) yield break;
            if (!host.Venues.ForceStart(eventId)) { Reject(sample, "Venue ForceStart rejected " + eventId); yield break; }
            forcedEventId = eventId;
            yield return Delay(2);
            yield return Measure(sample, () => EventInProgress(eventId) &&
                Vector3.Distance(host.Player.transform.position, selected.worldPosition) <= selected.influenceRadiusMeters);
            if (sample.venueCrowds == 0) Reject(sample, "No live venue crowd actors during the local event sample.");
            yield return Capture("Venue_event", 1920, 1080, "venue-event", null);
            host.Venues.ForceEnd(eventId); forcedEventId = null;
        }

        private bool EventInProgress(string id)
        {
            foreach (var item in host.Venues.EventsSnapshots)
                if (item.EventId == id && item.Status == TruckTaxiVenueEventStatus.InProgress) return true;
            return false;
        }

        private bool LocalVenueActive()
        {
            foreach (var item in host.Venues.EventsSnapshots)
                if ((item.Status == TruckTaxiVenueEventStatus.InProgress || item.Status == TruckTaxiVenueEventStatus.ArrivalSurge ||
                    item.Status == TruckTaxiVenueEventStatus.DepartureSurge) &&
                    Vector3.Distance(item.Position, host.Player.transform.position) < 350) return true;
            return false;
        }

        private IEnumerator Travel(Vector3 point, string label, Sample sample)
        {
            report.stage = sample.phase + " travel"; travelled = false;
            int before = host.FastTravel.CompletedTravels;
            if (!host.FastTravel.TryTravel(point, label))
            { Reject(sample, "FastTravel rejected: " + host.FastTravel.Feedback + " BlockReason=" + host.FastTravel.BlockReason); yield break; }
            double until = clock.Elapsed.TotalSeconds + 16;
            while (host.FastTravel.IsTravelling && clock.Elapsed.TotalSeconds < until) yield return null;
            if (host.FastTravel.IsTravelling)
            { host.FastTravel.CancelTravel(); Reject(sample, "FastTravel exceeded 16s: " + host.FastTravel.Feedback); yield break; }
            travelled = host.FastTravel.CompletedTravels == before + 1 && !host.Paused &&
                regional.IsPositionAvailable(host.Player.transform.position) && Vector3.Distance(host.Player.transform.position, point) < 250;
            if (!travelled) Reject(sample, "Travel did not commit into the requested loaded area: " + host.FastTravel.Feedback);
        }

        private IEnumerator RacePhase(Sample sample)
        {
            yield return Travel(TruckTaxiSpeedwayLayout.Stops[1].Position, "Thunder Bowl pit office", sample);
            if (!travelled) yield break;
            report.stage = sample.phase + " track streaming";
            double until = clock.Elapsed.TotalSeconds + 5;
            Vector3 track = TruckTaxiSpeedwayLayout.LapCheckpoints()[0];
            while (!regional.IsPositionAvailable(track) && clock.Elapsed.TotalSeconds < until) yield return null;
            if (!regional.IsPositionAvailable(track)) { Reject(sample, "Speedway track streaming exceeded 5s after travel."); yield break; }
            // This fresh, disposable shift has no earnings. Exercise the live AI path with a waived
            // fixture entry fee; economic entry/refund behavior is covered by focused session tests.
            long entryFee = host.Races.settings.entryFeeCents;
            bool entered;
            try
            {
                host.Races.settings.entryFeeCents = 0;
                entered = host.Races.BeginRace(TruckTaxiRaceMode.TruckRace);
            }
            finally { host.Races.settings.entryFeeCents = entryFee; }
            sample.detail = "Disposable probe session: entry fee waived; not payout/finish evidence.";
            if (!entered)
            { Reject(sample, "BeginRace(TruckRace) rejected: " + host.Races.Feedback); yield break; }
            yield return Delay(3.25f);
            var drivers = new List<Behaviour>();
            var starts = new List<Vector3>();
            // Race and traffic may share a host transform. Identify race-owned actors, not every UTS car.
            foreach (var target in host.Races.GetComponentsInChildren<TruckTaxiImpactTarget>())
            {
                if (target == null || target.targetId == null || !target.targetId.StartsWith("race:thunder-bowl:", StringComparison.Ordinal)) continue;
                foreach (var component in target.GetComponentsInChildren<MonoBehaviour>())
                    if (component != null && component.GetType().Name == "CarAIController")
                    { drivers.Add(component); starts.Add(component.transform.position); break; }
            }
            sample.utsDrivers = drivers.Count;
            yield return Measure(sample, () => host.Races.IsActive && host.Races.Mode == TruckTaxiRaceMode.TruckRace);
            for (int i = 0; i < drivers.Count; i++)
                if (drivers[i] != null && drivers[i].isActiveAndEnabled && Vector3.Distance(starts[i], drivers[i].transform.position) > 1)
                    sample.movingUtsDrivers++;
            if (sample.raceOpponents < 3 || sample.utsDrivers < 3 || sample.movingUtsDrivers == 0)
                Reject(sample, "UTS race expected >=3 opponents/drivers and movement; opponents=" + sample.raceOpponents +
                    ", drivers=" + sample.utsDrivers + ", moved>1m=" + sample.movingUtsDrivers);
            yield return Capture("UTS_TruckRace", 1920, 1080, "uts-truck-race", null);
            host.Races.CancelRace("Addendum runtime probe: sample complete.");
            yield return null;
            if (host.Races.IsActive || host.Races.OpponentCount != 0) Reject(sample, "CancelRace did not clean up opponents.");
        }

        private IEnumerator EarthquakePhase(Sample sample)
        {
            report.stage = sample.phase;
            if (!host.Extremes.ForceEarthquakeHere())
            {
                string reason = "none recorded";
                foreach (var attempt in host.Extremes.Attempts)
                    if (attempt.Kind == TruckTaxiExtremeKind.Earthquake) reason = attempt.Reason;
                Reject(sample, "ForceEarthquakeHere rejected: " + reason); yield break;
            }
            yield return Delay(.5f);
            yield return Measure(sample, HasEarthquake);
            if (sample.extremePhysics == 0 || sample.extremeVisuals == 0 || sample.maxCameraShake <= 0)
                Reject(sample, "Earthquake did not produce nearby physics, visuals and camera shake.");
            yield return Capture("Earthquake_world", 1920, 1080, "earthquake", null);
            host.Extremes.Clear(TruckTaxiExtremeKind.Earthquake, null);
        }

        private bool HasEarthquake()
        {
            foreach (var item in host.Extremes.ActiveSnapshots)
                if (item.Kind == TruckTaxiExtremeKind.Earthquake && item.LoadedNearby) return true;
            return false;
        }

        private IEnumerator KaijuPhase(Sample sample)
        {
            report.stage = sample.phase;
            if (!host.Kaiju.StartEvent(TruckTaxiKaijuKind.Attack, false))
            { Reject(sample, "Kaiju StartEvent(Attack) rejected; active=" + host.Kaiju.IsActive + ", enabled=" + host.Kaiju.isActiveAndEnabled); yield break; }
            yield return Delay(.5f);
            sample.detail = host.Kaiju.State.RegionId;
            yield return Measure(sample, () => host.Kaiju.IsActive && host.Kaiju.Kind == TruckTaxiKaijuKind.Attack);
            var actor = host.Kaiju.GetComponentInChildren<TruckTaxiKaijuActor>();
            if (sample.kaijuActors == 0 || actor == null) Reject(sample, "Kaiju director had no actual active actor.");
            yield return Capture("Kaiju_world", 1920, 1080, "kaiju", actor != null ? actor.transform : null);
            host.Kaiju.EndKaiju();
            yield return null;
            if (host.Kaiju.IsActive || host.Kaiju.IsCapturingPlayer) Reject(sample, "EndKaiju left an active actor/capture.");
        }

        private IEnumerator Measure(Sample sample, Func<bool> active)
        {
            report.stage = sample.phase; sample.status = "measuring";
            sample.width = Screen.width; sample.height = Screen.height;
            sample.region = regional.ResolveRegion(host.Player.transform.position);
            var frames = new List<float>(2048);
            double began = clock.Elapsed.TotalSeconds, previous = began, mainTotal = 0, physicsTotal = 0;
            using (var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
            using (var physics = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 1))
            {
                sample.mainRecorderValid = main.Valid; sample.physicsRecorderValid = physics.Valid;
                while (clock.Elapsed.TotalSeconds - began < SampleSeconds)
                {
                    yield return null;
                    double now = clock.Elapsed.TotalSeconds;
                    frames.Add((float)((now - previous) * 1000)); previous = now;
                    if (host.Paused || Time.timeScale <= 0) sample.pausedFrames++;
                    if (active()) sample.activeFrames++;
                    if (main.Valid) mainTotal += main.LastValue * .000001;
                    if (physics.Valid) physicsTotal += physics.LastValue * .000001;
                    sample.liveCars = Mathf.Max(sample.liveCars, host.traffic.ActiveCount);
                    sample.logicalCars = Mathf.Max(sample.logicalCars, host.traffic.LogicalCount);
                    sample.livePedestrians = Mathf.Max(sample.livePedestrians, host.pedestrians.ActiveCount);
                    sample.logicalPedestrians = Mathf.Max(sample.logicalPedestrians, host.pedestrians.LogicalCount);
                    sample.liveRivals = Mathf.Max(sample.liveRivals, host.Rivals.LiveCount);
                    sample.logicalRivals = Mathf.Max(sample.logicalRivals, host.Rivals.LogicalCount);
                    sample.venueCrowds = Mathf.Max(sample.venueCrowds, host.WorldCoordinator.VenueCrowdActors);
                    sample.buses = Mathf.Max(sample.buses, host.WorldCoordinator.BusActors);
                    sample.raceOpponents = Mathf.Max(sample.raceOpponents, host.Races.OpponentCount);
                    sample.extremePhysics = Mathf.Max(sample.extremePhysics, host.Extremes.LivePhysicsActorCount);
                    sample.extremeVisuals = Mathf.Max(sample.extremeVisuals, host.Extremes.LiveVisualActorCount);
                    sample.maxCameraShake = Mathf.Max(sample.maxCameraShake, host.Extremes.CameraShakeIntensity01);
                    if (host.Kaiju.IsActive) sample.kaijuActors = 1;
                }
            }
            sample.seconds = (float)(previous - began); sample.frames = frames.Count;
            if (frames.Count > 0)
            {
                sample.frameMeanMs = sample.seconds * 1000 / frames.Count;
                sample.fps = frames.Count / sample.seconds;
                frames.Sort(); sample.frameP95Ms = frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count * .95f) - 1, 0, frames.Count - 1)];
                sample.meanMainMs = (float)(mainTotal / frames.Count); sample.meanPhysicsMs = (float)(physicsTotal / frames.Count);
            }
            if (sample.frames < 2 || sample.seconds < SampleSeconds) Reject(sample, "Incomplete eight-second frame sample.");
            if (sample.pausedFrames != 0) Reject(sample, "World paused during " + sample.pausedFrames + " measured frames.");
            if (sample.activeFrames != sample.frames) Reject(sample, "Requested phase state present for " + sample.activeFrames + "/" + sample.frames + " frames.");
            if (sample.phase == "normal-town" && (string.IsNullOrEmpty(sample.region) || !sample.region.StartsWith("taxi.town", StringComparison.Ordinal)))
                Reject(sample, "Normal-town sample did not run in an authored town: " + sample.region);
            sample.status = sample.failures.Count == 0 ? "measured" : "failed";
            WriteReport();
        }

        private IEnumerator Delay(float seconds)
        {
            double until = clock.Elapsed.TotalSeconds + seconds;
            while (clock.Elapsed.TotalSeconds < until) yield return null;
        }
        private IEnumerator Resolution(int width, int height)
        {
            report.stage = "resolution " + width + "x" + height;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            yield return null;
            double until = clock.Elapsed.TotalSeconds + 3;
            while ((Screen.width != width || Screen.height != height) && clock.Elapsed.TotalSeconds < until) yield return null;
            yield return null;
        }

        private void OnAlert(string text, TruckTaxiAlertPriority priority)
        { if (text.StartsWith("PROBE FIXTURE:", StringComparison.Ordinal)) alertsPublished++; }

        private IEnumerator Capture(string name, int width, int height, string effect, Transform effectRoot)
        {
            report.stage = "screenshot " + name;
            var shot = new Shot { name = name, requestedWidth = width, requestedHeight = height, effect = effect };
            report.screenshots.Add(shot);
            int before = alertsPublished;
            foreach (var priority in new[] { TruckTaxiAlertPriority.Critical, TruckTaxiAlertPriority.High, TruckTaxiAlertPriority.Normal })
                host.hud.Alerts.Publish("addendum-probe:" + name + ":" + priority,
                    "PROBE FIXTURE: " + priority + " simultaneous HUD alert - needs, radio and GPS remain present.", priority, 20);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            shot.forcedAlertsPublished = alertsPublished - before;
            shot.actualWidth = Screen.width; shot.actualHeight = Screen.height;
            var camera = Camera.main; shot.camera = camera != null ? camera.name : "none";
            foreach (var text in host.hud.Root.GetComponentsInChildren<TextMeshProUGUI>())
                if (text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text) && text.transform.parent != null &&
                    (text.transform.parent.name == "Critical alert" || text.transform.parent.name == "Secondary alert"))
                    shot.visibleAlertTexts.Add(text.text);
            if (effectRoot != null && camera != null)
            {
                shot.effectRenderersInFrustum = 0;
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                foreach (var renderer in effectRoot.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                        shot.effectRenderersInFrustum++;
            }
            shot.effectEvidence = effect == "none" ? "Synthetic alert stress capture of the existing HUD." :
                "Real event state; existing gameplay camera. Frustum candidates (-1 = not checked) do not prove unoccluded appearance.";
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) throw new InvalidOperationException("ScreenCapture returned no texture.");
                shot.actualWidth = texture.width; shot.actualHeight = texture.height;
                var pixels = texture.GetPixels32();
                int low = 765, high = 0;
                for (int i = 0; i < pixels.Length; i += Math.Max(1, pixels.Length / 4096))
                { int sum = pixels[i].r + pixels[i].g + pixels[i].b; low = Math.Min(low, sum); high = Math.Max(high, sum); }
                shot.sampledColorRange = high - low;
                byte[] png = texture.EncodeToPNG();
                shot.file = Path.Combine(report.outputDirectory, name + ".png");
                File.WriteAllBytes(shot.file, png); shot.bytes = new FileInfo(shot.file).Length;
                if (shot.actualWidth != width || shot.actualHeight != height) shot.error = "Requested resolution not available; actual dimensions recorded.";
                else if (shot.visibleAlertTexts.Count < 2 || shot.forcedAlertsPublished < 2) shot.error = "Two simultaneous active alert lanes/publications not observed.";
                else if (camera == null || host.hud.NormalHudSuppressed) shot.error = "Gameplay camera or normal HUD unavailable.";
                else if (shot.bytes == 0 || shot.sampledColorRange < 8) shot.error = "Empty or nearly uniform capture.";
                shot.success = string.IsNullOrEmpty(shot.error);
            }
            catch (Exception error) { shot.error = error.ToString(); }
            finally { if (texture != null) Destroy(texture); }
            if (!shot.success) Fail(name + ": " + shot.error);
            if (effectRoot != null && shot.effectRenderersInFrustum == 0)
                report.limitations.Add(name + ": no effect renderer intersected the gameplay camera frustum; PNG is not visual proof of the effect.");
            WriteReport();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var shortcut in root.GetComponentsInChildren<ShortcutTrigger>(true))
                    if (!string.IsNullOrEmpty(shortcut.shortcutId)) SavePreference(TruckTaxiMapMarkers.ShortcutPreferenceKey(shortcut.shortcutId));
        }

        private void SavePreference(string key, int kind = 0)
        {
            if (preferences.ContainsKey(key)) return;
            preferences.Add(key, new Preference { Key = key, Kind = kind, Existed = PlayerPrefs.HasKey(key),
                IntValue = kind == 0 ? PlayerPrefs.GetInt(key) : 0, FloatValue = kind == 1 ? PlayerPrefs.GetFloat(key) : 0,
                StringValue = kind == 2 ? PlayerPrefs.GetString(key) : null });
        }

        private void RestorePreferences()
        {
            foreach (var saved in preferences.Values)
            {
                if (!saved.Existed) PlayerPrefs.DeleteKey(saved.Key);
                else if (saved.Kind == 0) PlayerPrefs.SetInt(saved.Key, saved.IntValue);
                else if (saved.Kind == 1) PlayerPrefs.SetFloat(saved.Key, saved.FloatValue);
                else PlayerPrefs.SetString(saved.Key, saved.StringValue);
            }
            PlayerPrefs.Save();
            report.preferencesRestored = true;
            report.preferenceKeysPreserved = 0;
            foreach (var saved in preferences.Values)
            {
                bool equal = PlayerPrefs.HasKey(saved.Key) == saved.Existed && (!saved.Existed ||
                    (saved.Kind == 0 ? PlayerPrefs.GetInt(saved.Key) == saved.IntValue :
                    saved.Kind == 1 ? PlayerPrefs.GetFloat(saved.Key).Equals(saved.FloatValue) : PlayerPrefs.GetString(saved.Key) == saved.StringValue));
                if (equal) report.preferenceKeysPreserved++;
                else { report.preferencesRestored = false; Fail("Preference restoration failed: " + saved.Key); }
            }
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            report.runtimeErrorCount++;
            if (report.runtimeErrors.Count < 24) report.runtimeErrors.Add(report.stage + ": " + message + "\n" + stack);
        }

        private void Reject(Sample sample, string reason)
        { sample.status = "failed"; sample.failures.Add(reason); Fail(sample.phase + ": " + reason); }
        private void Fail(string reason) { if (report.failures.Count < 64) report.failures.Add(reason); }
        private void Clean(Action action)
        { try { action(); } catch (Exception error) { Fail("Cleanup: " + error); } }

        private void BeginFinish()
        {
            if (finishing || quit) return;
            finishing = true; StopAllCoroutines();
            while (routines.Count > 0)
            { var routine = routines.Pop(); Clean(() => (routine as IDisposable)?.Dispose()); }
            StartCoroutine(CleanupAndQuit());
        }

        private IEnumerator CleanupAndQuit()
        {
            report.stage = "cleanup";
            if (host != null)
            {
                Clean(() => host.FastTravel?.CancelTravel());
                Clean(() => host.Races?.CancelRace("Addendum probe cleanup."));
                Clean(() => host.Extremes?.Clear(TruckTaxiExtremeKind.Earthquake, null));
                Clean(() => host.Kaiju?.EndKaiju());
                if (forcedEventId != null) Clean(() => host.Venues?.ForceEnd(forcedEventId));
                if (startedShift) Clean(() => host.Session?.EndShift());
                if (offerSuppression) Clean(() => host.Session?.ReleaseOfferSuppression(this));
                Clean(() => host.SetPaused(true));
                if (host.hud?.Alerts != null) host.hud.Alerts.Published -= OnAlert;
            }
            if (drivingInput != null) Clean(() => drivingInput.SetDrivingInputSuppressed(inputWasSuppressed));
            Clean(() => Screen.SetResolution(originalWidth, originalHeight, originalScreenMode));
            // Unload Radio (which saves on destruction) before restoring exact preference values.
            AsyncOperation unloading = null;
            Clean(() =>
            {
                if (SceneManager.GetActiveScene().name != TruckTaxiMainMenu.SceneName &&
                    Application.CanStreamedLevelBeLoaded(TruckTaxiMainMenu.SceneName))
                    unloading = SceneManager.LoadSceneAsync(TruckTaxiMainMenu.SceneName, LoadSceneMode.Single);
            });
            double until = Math.Min(clock.Elapsed.TotalSeconds + 4, QuitDeadline - 1);
            while (unloading != null && !unloading.isDone && clock.Elapsed.TotalSeconds < until) yield return null;
            if (unloading != null && !unloading.isDone) Fail("Cleanup scene unload exceeded its deadline.");
            // Cover an interrupted bootstrap or failed scene unload with ordinary deferred destruction.
            foreach (var radio in FindObjectsByType<TruckTaxiRadio>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(radio);
            yield return null;
            yield return null;
            QuitNow();
        }

        private void QuitNow()
        {
            if (quit) return;
            quit = true;
            Clean(RestorePreferences);
            if (FindObjectsByType<TruckTaxiRadio>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
            { report.preferencesRestored = false; Fail("Radio still present at exit: destruction may overwrite restored radio preferences."); }
            report.displayRestored = Screen.width == originalWidth && Screen.height == originalHeight && Screen.fullScreenMode == originalScreenMode;
            if (!report.displayRestored) Fail("Original screen resolution/mode was not restored before exit.");
            foreach (var sample in report.samples)
                if (sample.status != "measured" && sample.status != "failed") Reject(sample, "Phase incomplete at exit: " + sample.status);
            foreach (string name in new[] { "HUD_1920x1080", "HUD_2560x1440", "HUD_3440x1440" })
                if (!report.screenshots.Exists(shot => shot.name == name && shot.success)) Fail("Required HUD capture missing/failed: " + name);
            if (report.runtimeErrorCount > 0) Fail("Runtime logged " + report.runtimeErrorCount + " errors/exceptions/assertions; see runtimeErrors.");
            report.elapsedSeconds = clock.Elapsed.TotalSeconds;
            report.exitCode = report.failures.Count == 0 && report.preferencesRestored ? 0 : 2;
            report.status = report.exitCode == 0 ? "completed" : "failed";
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.logMessageReceived -= OnLog;
            Application.runInBackground = originalRunInBackground;
            WriteReport();
            Debug.Log("TruckTaxi addendum probe " + report.status + ": " + ReportPath);
            Application.Quit(report.exitCode);
        }

        private void WriteReport()
        {
            try
            {
                Directory.CreateDirectory(report.outputDirectory);
                report.elapsedSeconds = clock.Elapsed.TotalSeconds;
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            }
            catch (Exception error)
            { Fail("Report write failed: " + error.Message); report.exitCode = 2; report.status = "failed"; Debug.Log("Addendum report write failed: " + error); }
        }

        private void OnApplicationQuit()
        {
            if (quit || report == null) return;
            Fail("Application quit before probe completion.");
            // External termination cannot guarantee that other components have finished saving.
            report.status = "interrupted"; report.exitCode = 2;
            Clean(RestorePreferences); report.preferencesRestored = false;
            WriteReport();
        }
    }
}
#endif
