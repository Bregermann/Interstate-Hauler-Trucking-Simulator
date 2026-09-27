#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LWS.InterstateHauler;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // Command-line-only sampling; no Update loop or competing population authority.
    public sealed class TruckTaxiMegaRuntimeProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Sample
        {
            public string phase;
            public int frames, liveCars, logicalCars, livePedestrians, logicalPedestrians;
            public int liveRivals, logicalRivals, venueCrowds, buses, extremePhysics, extremeVisuals, pausedFrames;
            public float fps, meanFrameMs, p95FrameMs, meanMainMs, meanPhysicsMs;
            public bool mainRecorderValid, physicsRecorderValid;
        }

        [Serializable]
        private sealed class Report
        {
            public string scene, zoneId, extremeKind, error;
            public Sample spawnBaseline, normal, extreme;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            bool requested = false;
            foreach (var argument in Environment.GetCommandLineArgs())
                if (string.Equals(argument, "-truck-taxi-mega-probe", StringComparison.OrdinalIgnoreCase))
                    requested = true;
            if (!requested) return;
            var runner = new GameObject("Truck Taxi mega runtime probe");
            DontDestroyOnLoad(runner);
            var probe = runner.AddComponent<TruckTaxiMegaRuntimeProbe>();
            probe.StartCoroutine(probe.Run());
        }

        private IEnumerator Run()
        {
            var report = new Report();
            Application.runInBackground = true;
            if (SceneManager.GetActiveScene().name == TruckTaxiMainMenu.SceneName)
                yield return SceneManager.LoadSceneAsync(TruckTaxiMainMenu.FreePlaySceneName);
            TruckTaxiBootstrap host = null;
            float deadline = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < deadline)
            {
                host = TruckTaxiBootstrap.Instance;
                if (host != null && host.Ready && host.WorldCoordinator != null &&
                    host.Extremes != null && host.Rivals != null) break;
                yield return null;
            }
            if (host == null || !host.Ready || host.WorldCoordinator == null ||
                host.Extremes == null || host.Rivals == null)
            {
                report.error = "DemoCity mega runtime dependencies did not become ready within 90 seconds";
                Write(report);
                Application.Quit(2);
                yield break;
            }
            report.scene = host.gameObject.scene.name;
            bool startedShift = host.Session.State == TruckTaxiState.Inactive;
            bool wasPaused = host.Paused;
            bool requests = host.Session.RideRequestsEnabled;
            bool hadPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            int preference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);
            var input = host.Player.GetComponent<LwsKeyboardGamepadTruckInputSource>();
            bool inputSuppressed = input != null && input.DrivingInputSuppressed;
            TruckTaxiHazardZone zone = null;
            float nearestZone = float.PositiveInfinity;
            foreach (var candidate in host.WorldCoordinator.HazardZones)
                if (candidate != null && candidate.kind == TruckTaxiExtremeKind.Earthquake &&
                    candidate.IsValid(out _))
                {
                    float distance = Vector3.Distance(candidate.PositionAt(0), host.Player.transform.position);
                    if (distance < nearestZone) { nearestZone = distance; zone = candidate; }
                }
            TruckTaxiRideLocation destination = null;
            float nearestStop = float.PositiveInfinity;
            if (zone != null)
                foreach (var location in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
                {
                    float distance = Vector3.Distance(location.StopPosition, zone.PositionAt(0));
                    if (distance < nearestStop) { nearestStop = distance; destination = location; }
                }
            try
            {
                if (input == null || zone == null || destination == null ||
                    nearestStop > zone.radiusMeters * .75f)
                    report.error = "Input gate or authored ride stop near Town01 earthquake is missing";
                else
                {
                    host.Session.AcquireOfferSuppression(this);
                    host.Session.SetRideRequestsEnabled(false);
                    if (startedShift) host.StartShift();
                    else host.SetPaused(false);
                    input.SetDrivingInputSuppressed(true);
                    yield return new WaitForSecondsRealtime(5f);
                    report.spawnBaseline = new Sample { phase = "stationary Town01 spawn" };
                    yield return Measure(host, report.spawnBaseline, 8f);
                    host.TeleportNear(destination);
                    var world = host.GetComponent<TruckTaxiRegionalWorld>();
                    float streamingDeadline = Time.realtimeSinceStartup + 20f;
                    while (world != null && !world.IsPositionAvailable(host.Player.transform.position) &&
                           Time.realtimeSinceStartup < streamingDeadline) yield return null;
                    if (world != null && !world.IsPositionAvailable(host.Player.transform.position))
                        report.error = "Town01 earthquake sample location did not stream within 20 seconds";
                    else if (Vector3.Distance(host.Player.transform.position, zone.PositionAt(0)) > zone.radiusMeters)
                        report.error = "Authored ride stop is outside earthquake exposure";
                    else
                    {
                        yield return new WaitForSecondsRealtime(5f);
                        report.normal = new Sample { phase = "normal" };
                        yield return Measure(host, report.normal, 8f);
                        report.zoneId = zone.zoneId;
                        report.extremeKind = zone.kind.ToString();
                        if (!host.Extremes.StartEvent(zone.kind, zone.zoneId))
                            report.error = "Town01 earthquake could not start";
                        else
                        {
                            yield return new WaitForSecondsRealtime(.5f);
                            report.extreme = new Sample { phase = "extreme" };
                            yield return Measure(host, report.extreme, 8f);
                            if (report.extreme.extremePhysics == 0)
                                report.error = "Earthquake sample had zero live physics actors";
                        }
                        if (report.spawnBaseline.pausedFrames > 0 || report.normal.pausedFrames > 0 ||
                            report.extreme != null && report.extreme.pausedFrames > 0)
                            report.error = "A modal pause interrupted the benchmark";
                    }
                }
            }
            finally
            {
                if (zone != null) host.Extremes.Clear(zone.kind, zone.zoneId);
                if (input != null) input.SetDrivingInputSuppressed(inputSuppressed);
                host.Session.ReleaseOfferSuppression(this);
                host.Session.SetRideRequestsEnabled(requests);
                if (hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, preference);
                else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
                PlayerPrefs.Save();
                if (startedShift) host.Session.EndShift();
                host.SetPaused(wasPaused);
            }
            Write(report);
            Application.Quit(string.IsNullOrEmpty(report.error) ? 0 : 2);
        }

        private static IEnumerator Measure(TruckTaxiBootstrap host, Sample sample, float seconds)
        {
            var frames = new List<float>(1024);
            double main = 0, physics = 0;
            int mainSamples = 0, physicsSamples = 0;
            using (var mainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
            using (var physicsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 1))
            {
                sample.mainRecorderValid = mainRecorder.Valid;
                sample.physicsRecorderValid = physicsRecorder.Valid;
                float deadline = Time.realtimeSinceStartup + seconds;
                while (Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    if (host.Paused) sample.pausedFrames++;
                    sample.extremePhysics = Math.Max(sample.extremePhysics, host.Extremes.LivePhysicsActorCount);
                    sample.extremeVisuals = Math.Max(sample.extremeVisuals, host.Extremes.LiveVisualActorCount);
                    frames.Add(Time.unscaledDeltaTime * 1000f);
                    if (mainRecorder.Valid) { main += mainRecorder.LastValue * 1e-6; mainSamples++; }
                    if (physicsRecorder.Valid) { physics += physicsRecorder.LastValue * 1e-6; physicsSamples++; }
                }
            }
            sample.frames = frames.Count;
            float sum = 0;
            foreach (float value in frames) sum += value;
            sample.meanFrameMs = sum / Mathf.Max(1, sample.frames);
            sample.fps = sample.meanFrameMs > 0 ? 1000f / sample.meanFrameMs : 0;
            frames.Sort();
            if (frames.Count > 0) sample.p95FrameMs = frames[Mathf.Clamp(
                Mathf.CeilToInt(frames.Count * .95f) - 1, 0, frames.Count - 1)];
            sample.meanMainMs = (float)(main / Math.Max(1, mainSamples));
            sample.meanPhysicsMs = (float)(physics / Math.Max(1, physicsSamples));
            sample.liveCars = host.traffic.ActiveCount;
            sample.logicalCars = host.traffic.LogicalCount;
            sample.livePedestrians = host.pedestrians.ActiveCount;
            sample.logicalPedestrians = host.pedestrians.LogicalCount;
            sample.liveRivals = host.Rivals.LiveCount;
            sample.logicalRivals = host.Rivals.LogicalCount;
            sample.venueCrowds = host.WorldCoordinator.VenueCrowdActors;
            sample.buses = host.WorldCoordinator.BusActors;
        }

        private static void Write(Report report)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "TruckTaxiMegaRuntimeProbe.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("TAXI MEGA AUTOMATED PROBE " + path + " " + JsonUtility.ToJson(report));
        }
    }
}
#endif
