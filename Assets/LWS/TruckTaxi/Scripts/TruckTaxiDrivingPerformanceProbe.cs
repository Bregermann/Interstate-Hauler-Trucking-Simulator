#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LWS.InterstateHauler;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace LWS.TruckTaxi
{
    // Opt-in Windows-player measurement. Setup and one-time world queries are outside timed windows.
    public sealed class TruckTaxiDrivingPerformanceProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Sample
        {
            public string workload, error, startRegion, endRegion, compassMarker;
            public int frames, movingFrames, focusedFrames, liveCars, livePedestrians, fullCars, fullPedestrians;
            public int loadedRegionsStart, loadedRegionsEnd, activeRigidbodies, drawCalls;
            public float seconds, distanceMeters, pathMeters, meanSpeedMps, peakSpeedMps, meanMs, medianMs, p95Ms, p99Ms;
            public float meanMainMs, meanRenderMs, meanPhysicsMs, meanGpuMs, meanGcBytes, meanCompassMs;
            public long managedHeapBytes;
            public bool mainValid, renderValid, physicsValid, gpuValid, gcValid, drawCallsValid, compassValid;
        }

        [Serializable]
        private sealed class Report
        {
            public string scene, device, quality, error, mapCaptureMode;
            public int width, height, vSync, targetFrameRate;
            public Sample[] workloads;
        }

        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld world;
        private Rigidbody truck;
        private Keyboard keyboard;
        private bool continuousMapBaseline;
        private readonly List<Sample> results = new List<Sample>(10);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Application.isEditor) return;
            bool requested = false;
            foreach (string arg in Environment.GetCommandLineArgs())
                if (string.Equals(arg, "-truck-taxi-driving-perf", StringComparison.OrdinalIgnoreCase)) requested = true;
            if (!requested) return;
            var runner = new GameObject("Truck Taxi driving performance probe").AddComponent<TruckTaxiDrivingPerformanceProbe>();
            foreach (string arg in Environment.GetCommandLineArgs())
                if (string.Equals(arg, "-truck-taxi-driving-perf-continuous-map", StringComparison.OrdinalIgnoreCase))
                    runner.continuousMapBaseline = true;
            DontDestroyOnLoad(runner.gameObject);
            runner.StartCoroutine(runner.Run());
        }

        private IEnumerator Run()
        {
            var report = new Report();
            report.mapCaptureMode = continuousMapBaseline ? "Compass continuous control" : "Truck Taxi timed snapshots";
            Application.runInBackground = true;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            if (SceneManager.GetActiveScene().name == TruckTaxiMainMenu.SceneName)
                yield return SceneManager.LoadSceneAsync(TruckTaxiMainMenu.FreePlaySceneName);
            float deadline = Time.realtimeSinceStartup + 120f;
            while (Time.realtimeSinceStartup < deadline)
            {
                host = TruckTaxiBootstrap.Instance;
                if (host != null && host.Ready && host.Player != null && host.RouteDistances != null) break;
                yield return null;
            }
            if (host == null || !host.Ready || host.Player == null || host.RouteDistances == null)
            {
                report.error = "Free Play did not become ready within 120 seconds";
                Write(report);
                Application.Quit(2);
                yield break;
            }

            report.scene = SceneManager.GetActiveScene().name;
            report.device = SystemInfo.graphicsDeviceName;
            report.quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            report.width = Screen.width; report.height = Screen.height;
            report.vSync = QualitySettings.vSyncCount; report.targetFrameRate = Application.targetFrameRate;
            world = host.GetComponent<TruckTaxiRegionalWorld>();
            truck = host.Player.GetComponent<Rigidbody>();
            if (world == null || truck == null)
            {
                report.error = "Regional world or NWH player body is missing";
                Write(report); Application.Quit(2); yield break;
            }
            var settings = InputSystem.settings;
            var temporary = Instantiate(settings);
            temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = temporary;
            keyboard = InputSystem.AddDevice<Keyboard>("Truck Taxi driving performance keyboard");
            TruckTaxiRideLocation townOrigin = null;
            foreach (var stop in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
                if (stop.locationId == "taxi.stop.00") { townOrigin = stop; break; }
            if (townOrigin != null) { host.TeleportNear(townOrigin); yield return new WaitForSecondsRealtime(2); }
            Vector3 townStart = truck.position;
            Vector3 townTarget = SelectTownStop(townStart);
            bool originalRideRequests = host.Session.RideRequestsEnabled;
            try
            {
                if (host.Session.State == TruckTaxiState.Inactive) host.StartShift();
                host.SetPaused(false);
                yield return new WaitForSecondsRealtime(5);
                yield return Measure("A dense town drive", townTarget, 30);
                if (townOrigin != null) { host.TeleportNear(townOrigin); yield return new WaitForSecondsRealtime(2); }
                yield return Measure("G GPS HUD (radio unchanged)", townTarget, 20);

                if (host.Session.State != TruckTaxiState.Available)
                { host.Session.EndShift(); host.StartShift(); }
                host.Session.SetRideRequestsEnabled(true);
                var roster = host.Configuration.passengerDatabase?.passengers;
                var passenger = roster == null ? null : Array.Find(roster, p => p != null && p.passengerId == "bald-chaos-warrior");
                if (passenger != null && host.Session.OfferRide(passenger) && host.Session.AcceptRide())
                {
                    host.Session.SetRideRequestsEnabled(false);
                    yield return Measure("C ride to pickup", host.Session.Pickup.StopPosition, 25);
                    if (!host.Session.HasPassenger)
                    {
                        host.TeleportNear(host.Session.Pickup);
                        deadline = Time.realtimeSinceStartup + 12;
                        while (!host.Session.HasPassenger && Time.realtimeSinceStartup < deadline) yield return null;
                    }
                    if (host.Session.HasPassenger)
                        yield return Measure("C passenger onboard", host.Session.Destination.StopPosition, 30);
                    else results.Add(new Sample { workload = "C passenger onboard", error = "Passenger did not board" });
                }
                else results.Add(new Sample { workload = "C ride", error = "No authored ride could be accepted" });
                host.Session.SetRideRequestsEnabled(false);
                host.Session.EndShift(); host.StartShift();

                if (host.CalendarWeather != null &&
                    host.CalendarWeather.ForceCurrent(TruckTaxiWeatherCondition.HeavyRain, 4))
                {
                    if (townOrigin != null)
                    {
                        host.TeleportNear(townOrigin);
                        yield return new WaitForSecondsRealtime(2);
                        truck.GetComponent<TruckTaxiSignificantCollisionDiagnostic>()?.CaptureLocalOverlaps("rain post-teleport");
                    }
                    yield return new WaitForSecondsRealtime(4);
                    yield return Measure("D heavy rain drive", townTarget, 25);
                    host.CalendarWeather.ForceCurrent(TruckTaxiWeatherCondition.Clear, 4);
                }
                else results.Add(new Sample { workload = "D heavy rain drive", error = "Weather request rejected" });

                if (host.Extremes != null && host.Extremes.ForceEarthquakeHere())
                {
                    yield return new WaitForSecondsRealtime(2);
                    yield return Measure("E earthquake drive", townStart, 25);
                    host.Extremes.Clear();
                }
                else results.Add(new Sample { workload = "E earthquake drive", error = "No current earthquake zone" });

                Vector3 highwayStart = HighwayPoint(1080, false), highwayEnd = HighwayPoint(3000, false);
                if (highwayStart != Vector3.zero && highwayEnd != Vector3.zero)
                {
                    yield return StageHighway(highwayStart, Vector3.right);
                    if (world.IsPositionAvailable(highwayStart)) yield return Measure("B highway drive", highwayEnd, 45);
                    else results.Add(new Sample { workload = "B highway drive", error = "Highway staging region unavailable" });
                    Vector3 eastStart = HighwayPoint(900, false), eastEnd = HighwayPoint(1700, false);
                    Vector3 westStart = HighwayPoint(1700, true), westEnd = HighwayPoint(900, true);
                    yield return StageHighway(eastStart, Vector3.right);
                    if (world.IsPositionAvailable(eastStart)) yield return Measure("H boundary eastbound", eastEnd, 35);
                    else results.Add(new Sample { workload = "H boundary eastbound", error = "Eastbound staging region unavailable" });
                    yield return StageHighway(westStart, Vector3.left);
                    if (world.IsPositionAvailable(westStart)) yield return Measure("H boundary westbound", westEnd, 35);
                    else results.Add(new Sample { workload = "H boundary westbound", error = "Westbound staging region unavailable" });
                }
                else results.Add(new Sample { workload = "B/H highway and boundary", error = "Authored regional traffic lane unavailable" });

                var pit = new GameObject("Performance probe race staging").AddComponent<TruckTaxiRideLocation>();
                pit.transform.position = TruckTaxiSpeedwayLayout.Stops[1].Position;
                try
                {
                    yield return world.PrepareInitialWorld(pit.StopPosition);
                    if (world.IsPositionAvailable(pit.StopPosition))
                    {
                        host.TeleportNear(pit);
                        host.Session.EndShift(); host.StartShift();
                        if (host.Races != null && host.Races.BeginRace(TruckTaxiRaceMode.Practice))
                        {
                            yield return new WaitForSecondsRealtime(7);
                            yield return Measure("F Thunder Bowl race", TruckTaxiSpeedwayLayout.TrackCenterline(), 30);
                        }
                        else results.Add(new Sample { workload = "F Thunder Bowl race", error = host.Races?.Feedback ?? "Race unavailable" });
                    }
                    else results.Add(new Sample { workload = "F Thunder Bowl race", error = "Speedway region did not load" });
                }
                finally { Destroy(pit.gameObject); }
            }
            finally
            {
                host.Session.SetRideRequestsEnabled(originalRideRequests);
                if (keyboard != null)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.RemoveDevice(keyboard);
                }
                InputSystem.settings = settings;
                Destroy(temporary);
                report.workloads = results.ToArray();
                Write(report);
                Application.Quit(0);
            }
        }

        private Vector3 SelectTownStop(Vector3 start)
        {
            Vector3 best = start;
            float longest = 0;
            string town = world.ResolveRideTown(start);
            foreach (var stop in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
            {
                if (stop == null || world.ResolveRideTown(stop.StopPosition) != town) continue;
                float distance = Vector3.Distance(start, stop.StopPosition);
                if (distance <= longest || distance < 150 || distance > 1600) continue;
                var route = host.RouteDistances.Measure(start, stop.StopPosition);
                if (route?.Navigable != true || route.Points.Count < 2) continue;
                best = stop.StopPosition; longest = distance;
            }
            return best;
        }

        private Vector3 HighwayPoint(float x, bool westbound)
        {
            foreach (var lane in host.traffic.cityLanes)
            {
                if (lane == null || lane.laneId != "taxi.regional.lane.0" || lane.centerline == null) continue;
                Vector3 best = Vector3.zero;
                float score = float.PositiveInfinity;
                float z = westbound ? 3.5f : -3.5f;
                foreach (var point in lane.centerline)
                {
                    float candidate = Mathf.Abs(point.x - x) + Mathf.Abs(point.z - z) * 10;
                    if (candidate >= score) continue;
                    score = candidate; best = point;
                }
                return score < 30 ? best : Vector3.zero;
            }
            return Vector3.zero;
        }

        private IEnumerator StageHighway(Vector3 position, Vector3 heading)
        {
            if (position == Vector3.zero) yield break;
            yield return world.PrepareInitialWorld(position);
            if (!world.IsPositionAvailable(position)) yield break;
            var marker = new GameObject("Performance probe highway staging").AddComponent<TruckTaxiRideLocation>();
            marker.transform.SetPositionAndRotation(position, Quaternion.LookRotation(heading, Vector3.up));
            marker.truckStopPoint = marker.transform;
            host.TeleportNear(marker);
            Destroy(marker.gameObject);
            host.SetPaused(false);
            yield return new WaitForSecondsRealtime(2);
        }

        private IEnumerator Measure(string name, Vector3 target, float seconds)
        {
            var route = host.RouteDistances.Measure(truck.position, target);
            if (route?.Navigable != true || route.Points.Count < 2)
            {
                results.Add(new Sample { workload = name, error = "No navigable route" });
                yield break;
            }
            yield return Measure(name, route.Points, seconds);
        }

        private IEnumerator Measure(string name, IReadOnlyList<Vector3> points, float seconds)
        {
            var sample = new Sample { workload = name, startRegion = world.CurrentRegion,
                loadedRegionsStart = world.LoadedCount };
            results.Add(sample);
            if (points == null || points.Count < 2) { sample.error = "Route has fewer than two points"; yield break; }
            // Development-only A/B control: the same player binary can measure Compass's
            // original continuous mode against the project-owned timed snapshot policy.
            foreach (var compass in new[] { host.GPS?.CabCompass, host.GPS?.HudCompass })
            {
                var property = compass?.GetType().GetProperty("miniMapCameraSnapshotFrequency");
                if (property != null)
                    property.SetValue(compass, Enum.Parse(property.PropertyType,
                        continuousMapBaseline ? "Continuous" : "TimeInterval"));
            }
            yield return new WaitForSecondsRealtime(.5f);
            var times = new List<float>(8192);
            var gpuTiming = new FrameTiming[1];
            double main = 0, render = 0, physics = 0, gpu = 0, gc = 0, speedTotal = 0, drawTotal = 0, compassCpu = 0;
            int mainCount = 0, renderCount = 0, physicsCount = 0, gpuCount = 0, gcCount = 0, drawCount = 0, compassCount = 0;
            Vector3 start = truck.position;
            Vector3 previous = start;
            int waypoint = 1;
            bool triedEngine = false;
            var vehicle = truck.GetComponent<NWH.VehiclePhysics2.VehicleController>();
            float began = Time.realtimeSinceStartup;
            using (var mainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
            using (var renderRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 1))
            using (var physicsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 1))
            using (var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1))
            using (var drawRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1))
            using (var compassRecorder = StartCompassRecorder(out sample.compassMarker))
            {
                sample.mainValid = mainRecorder.Valid; sample.renderValid = renderRecorder.Valid;
                sample.physicsValid = physicsRecorder.Valid; sample.gcValid = gcRecorder.Valid;
                while (Time.realtimeSinceStartup - began < seconds)
                {
                    Vector3 aim = points[waypoint] - truck.position; aim.y = 0;
                    while (waypoint < points.Count - 1 && aim.sqrMagnitude < 18 * 18)
                    { waypoint++; aim = points[waypoint] - truck.position; aim.y = 0; }
                    float angle = Vector3.SignedAngle(truck.rotation * Vector3.forward, aim, Vector3.up);
                    float speed = truck.linearVelocity.magnitude;
                    float desired = Mathf.Abs(angle) > 45 ? 7 : 25;
                    Key pedal = speed > desired ? Key.S : Key.W;
                    Key steering = angle > 4 ? Key.D : angle < -4 ? Key.A : Key.None;
                    InputSystem.QueueStateEvent(keyboard, steering == Key.None ?
                        new KeyboardState(pedal) : new KeyboardState(pedal, steering));
                    FrameTimingManager.CaptureFrameTimings();
                    yield return null;
                    times.Add(Time.unscaledDeltaTime * 1000);
                    float currentSpeed = truck.linearVelocity.magnitude;
                    speedTotal += currentSpeed;
                    sample.peakSpeedMps = Mathf.Max(sample.peakSpeedMps, currentSpeed);
                    Vector3 current = truck.position;
                    sample.pathMeters += Vector3.Distance(previous, current);
                    previous = current;
                    if (currentSpeed > 1) sample.movingFrames++;
                    if (Application.isFocused) sample.focusedFrames++;
                    if (mainRecorder.Valid && mainRecorder.Count > 0) { main += mainRecorder.LastValue * 1e-6; mainCount++; }
                    if (renderRecorder.Valid && renderRecorder.Count > 0) { render += renderRecorder.LastValue * 1e-6; renderCount++; }
                    if (physicsRecorder.Valid && physicsRecorder.Count > 0) { physics += physicsRecorder.LastValue * 1e-6; physicsCount++; }
                    if (gcRecorder.Valid && gcRecorder.Count > 0) { gc += gcRecorder.LastValue; gcCount++; }
                    if (compassRecorder.Valid && compassRecorder.Count > 0) { compassCpu += compassRecorder.LastValue * 1e-6; compassCount++; }
                    if (drawRecorder.Valid && drawRecorder.Count > 0) { drawTotal += drawRecorder.LastValue; drawCount++; }
                    // Some Windows drivers return a sentinel/invalid duration despite a successful query.
                    if (FrameTimingManager.GetLatestTimings(1, gpuTiming) > 0 &&
                        IsValidGpuDuration(gpuTiming[0].gpuFrameTime))
                    { gpu += gpuTiming[0].gpuFrameTime; gpuCount++; }
                    if (!triedEngine && Time.realtimeSinceStartup - began > 8 &&
                        Vector3.Distance(start, truck.position) < 2)
                    {
                        if (vehicle?.powertrain?.engine != null && !vehicle.powertrain.engine.IsRunning)
                        {
                            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                            yield return null;
                        }
                        triedEngine = true;
                    }
                }
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            sample.frames = times.Count;
            sample.seconds = Time.realtimeSinceStartup - began;
            sample.distanceMeters = Vector3.Distance(start, truck.position);
            sample.meanSpeedMps = (float)(speedTotal / Math.Max(1, sample.frames));
            sample.drawCalls = (int)(drawTotal / Math.Max(1, drawCount));
            sample.drawCallsValid = drawCount > 0;
            times.Sort();
            if (times.Count > 0)
            {
                double total = 0; foreach (float ms in times) total += ms;
                sample.meanMs = (float)(total / times.Count);
                sample.medianMs = At(times, .5f); sample.p95Ms = At(times, .95f); sample.p99Ms = At(times, .99f);
            }
            sample.meanMainMs = (float)(main / Math.Max(1, mainCount));
            sample.meanRenderMs = (float)(render / Math.Max(1, renderCount));
            sample.meanPhysicsMs = (float)(physics / Math.Max(1, physicsCount));
            sample.meanGpuMs = (float)(gpu / Math.Max(1, gpuCount));
            sample.meanGcBytes = (float)(gc / Math.Max(1, gcCount));
            sample.gpuValid = gpuCount > 0;
            sample.compassValid = compassCount > 0;
            sample.meanCompassMs = (float)(compassCpu / Math.Max(1, compassCount));
            sample.managedHeapBytes = GC.GetTotalMemory(false);
            sample.liveCars = host.traffic.ActiveCount; sample.livePedestrians = host.pedestrians.ActiveCount;
            sample.fullCars = host.traffic.FullPhysicsCount; sample.fullPedestrians = host.pedestrians.FullPhysicsCount;
            sample.loadedRegionsEnd = world.LoadedCount; sample.endRegion = world.CurrentRegion;
            sample.activeRigidbodies = world.ActiveBodies;
            if (sample.focusedFrames < sample.frames * .9f)
                sample.error = "Player was not focused; graphics may have been throttled";
            else if (sample.pathMeters < seconds * 3 || sample.movingFrames < sample.frames / 4)
            {
                sample.error = "Truck did not move enough for a representative driving sample";
                truck.GetComponent<TruckTaxiSignificantCollisionDiagnostic>()?.CaptureLocalOverlaps(name + " insufficient movement");
            }
            else if (name.StartsWith("B highway", StringComparison.Ordinal) && sample.meanSpeedMps < 12)
                sample.error = "Highway sample did not reach representative highway speed";
            else if (name.StartsWith("H boundary", StringComparison.Ordinal) && sample.startRegion == sample.endRegion)
                sample.error = "Truck did not cross a regional streaming boundary";
            Debug.Log("TAXI DRIVING PERF " + JsonUtility.ToJson(sample));
        }

        private static float At(List<float> sorted, float percentile) => sorted[Mathf.Clamp(
            Mathf.CeilToInt(sorted.Count * percentile) - 1, 0, sorted.Count - 1)];

        public static bool IsValidGpuDuration(double milliseconds) =>
            double.IsFinite(milliseconds) && milliseconds > 0 && milliseconds < 1000;

        private static ProfilerRecorder StartCompassRecorder(out string marker)
        {
            // Enumerate once outside each measured window; unavailable timing is not a zero-cost claim.
            marker = "Unavailable";
            var available = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(available);
            foreach (var handle in available)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.Name.IndexOf("CompassPro.LateUpdate", StringComparison.Ordinal) < 0) continue;
                marker = description.Name;
                return ProfilerRecorder.StartNew(description.Category, description.Name, 1);
            }
            return default;
        }

        private static void Write(Report report)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "TruckTaxiDrivingPerformanceProbe.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("TAXI DRIVING PERF REPORT " + path);
        }
    }
}
#endif
