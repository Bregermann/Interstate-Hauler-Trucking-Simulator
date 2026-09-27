#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckTaxi
{
    // One opt-in integration pass over the production scene; no density or world-state fixtures.
    public static class TruckTaxiIntegratedRuntimeProbe
    {
        public static IEnumerator Run(TruckTaxiBootstrap host, Action<bool, string> check, Action<string> capture)
        {
            if (host == null || !host.Ready || host.hud?.Controls == null || host.hud.UIInput == null ||
                host.DriverNeeds?.State == null || host.Handling == null || host.Session == null ||
                host.pedestrians == null || host.traffic == null)
            {
                check(false, "Integrated runtime dependencies are ready");
                yield break;
            }

            var session = host.Session;
            var controls = host.hud.Controls;
            var input = host.hud.UIInput;
            var effects = host.DriverNeeds.State.Effects;
            var presentation = host.GetComponent<TruckTaxiEffectPresentation>();
            bool wasPaused = host.Paused;
            bool startedShift = session.State == TruckTaxiState.Inactive;
            bool originalRequests = session.RideRequestsEnabled;
            bool hadPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            int originalPreference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);
            Keyboard keyboard = null;
            bool activatedEffects = false;
            try
            {
                if (startedShift) host.StartShift();
                else host.SetPaused(false);
                check(session.State == TruckTaxiState.Available, "Integrated probe has an available shift");
                if (session.State != TruckTaxiState.Available) yield break;

                // A dedicated input device makes the F1/F2 checks deterministic in a headless player.
                keyboard = InputSystem.AddDevice<Keyboard>("Truck Taxi integrated probe keyboard");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F2));
                yield return null;
                bool toggled = session.RideRequestsEnabled != originalRequests;
                check(toggled && PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey, -1) ==
                    (session.RideRequestsEnabled ? 1 : 0), "F2 toggles ride requests and persists the preference");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                if (session.RideRequestsEnabled) session.SetRideRequestsEnabled(false);

                check(!host.pedestrians.IsBaselineValidation && !host.traffic.IsBaselineValidation &&
                    host.pedestrians.TargetCount >= 720 && host.traffic.TargetCount >= 300,
                    "Production density targets remain at least 720 pedestrians / 300 traffic");
                float deadline = Time.realtimeSinceStartup + 120f;
                while ((host.pedestrians.ActiveCount < 720 || host.traffic.ActiveCount < 300) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                int pedestrians = host.pedestrians.ActiveCount;
                int traffic = host.traffic.ActiveCount;
                Debug.Log($"TAXI INTEGRATED POPULATION: pedestrians={pedestrians}/{host.pedestrians.TargetCount}, traffic={traffic}/{host.traffic.TargetCount}, waitLimitSeconds=120");
                check(pedestrians >= 720 && traffic >= 300, "Real population reached 720 pedestrians / 300 traffic");
                check(host.Companions != null && host.Companions.Ready && host.Companions.CompanionCount >= 6,
                    "Roadside companion loop initialized at least six authored companions");

                var frameMilliseconds = new List<float>();
                var mainMilliseconds = new List<float>();
                var physicsMilliseconds = new List<float>();
                var observedMoving = new HashSet<TruckTaxiTrafficBehaviour>();
                var monitoredTraffic = new List<TruckTaxiTrafficBehaviour>();
                foreach (var vehicle in host.traffic.Vehicles)
                    if (vehicle != null && vehicle.activeInHierarchy)
                    {
                        var behavior = vehicle.GetComponent<TruckTaxiTrafficBehaviour>();
                        if (behavior != null) monitoredTraffic.Add(behavior);
                    }
                bool mainValid, physicsValid;
                using (var mainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
                using (var physicsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 1))
                {
                    mainValid = mainRecorder.Valid;
                    physicsValid = physicsRecorder.Valid;
                    deadline = Time.realtimeSinceStartup + 15f;
                    while (Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                        frameMilliseconds.Add(Time.unscaledDeltaTime * 1000f);
                        if (mainRecorder.Valid) mainMilliseconds.Add(mainRecorder.LastValue * 1e-6f);
                        if (physicsRecorder.Valid) physicsMilliseconds.Add(physicsRecorder.LastValue * 1e-6f);
                        foreach (var behavior in monitoredTraffic)
                            if (behavior != null && behavior.isActiveAndEnabled && behavior.Speed > 1f)
                                observedMoving.Add(behavior);
                    }
                }
                frameMilliseconds.Sort();
                mainMilliseconds.Sort();
                physicsMilliseconds.Sort();
                int frames = frameMilliseconds.Count;
                float p95 = frames == 0 ? 0 : frameMilliseconds[Mathf.Clamp(Mathf.CeilToInt(frames * .95f) - 1, 0, frames - 1)];
                Debug.Log($"TAXI INTEGRATED FRAMES: samples={frames}, meanMs={Mean(frameMilliseconds):0.000}, medianMs={Median(frameMilliseconds):0.000}, p95Ms={p95:0.000}, windowSeconds=15");
                Debug.Log($"TAXI INTEGRATED PROFILER: mainThreadValid={mainValid}, mainSamples={mainMilliseconds.Count}, mainMeanMs={Mean(mainMilliseconds):0.000}, mainMedianMs={Median(mainMilliseconds):0.000}, physicsValid={physicsValid}, physicsSamples={physicsMilliseconds.Count}, physicsMeanMs={Mean(physicsMilliseconds):0.000}, physicsMedianMs={Median(physicsMilliseconds):0.000}");
                check(frames > 0, "15-second frame sample contains measured frames");
                ReportTraffic(host, observedMoving.Count, check);
                CheckRendererOnlyMotion(host, check);

                controls.Open();
                yield return null;
                bool populatedPage = false;
                if (controls.FocusRoot != null)
                    foreach (var text in controls.FocusRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
                        if (text.text.Contains("CURRENT DEVICE:")) populatedPage = true;
                check(controls.IsOpen && host.Paused && input.FocusRoot == controls.FocusRoot &&
                    populatedPage, "Full controls page opens with populated bindings and UI focus");
                capture?.Invoke("Integrated_ControlsFull");
                controls.Close();
                yield return null;
                check(!controls.IsOpen && !host.Paused, "Full controls page restores gameplay");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F1));
                yield return null;
                var quick = host.hud.Root.Find("Quick controls");
                var quickText = quick != null ? quick.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                check(quick != null && quick.gameObject.activeInHierarchy && quickText != null &&
                    !string.IsNullOrWhiteSpace(quickText.text), "Held F1 displays the populated quick controls overlay");
                capture?.Invoke("Integrated_ControlsQuick");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                check(quick != null && !quick.gameObject.activeSelf, "Releasing F1 hides quick controls");

                check(presentation != null && host.Handling.Applied && effects.ActiveCount == 0,
                    "Temporary-effect presentation and NWH handling are idle before activation");
                if (presentation == null || !host.Handling.Applied || effects.ActiveCount != 0) yield break;
                float baselinePower = host.Handling.CurrentPowerMultiplier;
                Debug.Log($"TAXI INTEGRATED ENGINE: initialBaseline={baselinePower:0.0000}, speedMph={host.Handling.SpeedMph:0.000}");
                check(Mathf.Abs(presentation.Intensity) < .0001f, "Mushroom presentation starts at zero intensity");
                activatedEffects = effects.Activate(TruckTaxiTemporaryEffectKind.MysteryMushroom);
                check(activatedEffects, "Mushroom effect activates");
                deadline = Time.realtimeSinceStartup + 15f;
                while (presentation.Intensity < .98f && Time.realtimeSinceStartup < deadline) yield return null;
                float peak = presentation.Intensity;
                check(peak > .95f && effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).RemainingSeconds > 0,
                    "Mushroom presentation fades in while effect is active");
                capture?.Invoke("Integrated_MushroomPeak");
                deadline = Time.realtimeSinceStartup + 60f;
                while (effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).RemainingSeconds > 2f &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                yield return null;
                float fading = presentation.Intensity;
                check(fading > 0 && fading < peak, "Mushroom presentation fades out before expiry");
                capture?.Invoke("Integrated_MushroomFade");
                deadline = Time.realtimeSinceStartup + 15f;
                while (effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).RemainingSeconds > 0 &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                yield return null;
                check(effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).RemainingSeconds == 0 &&
                    Mathf.Abs(presentation.Intensity) < .0001f, "Mushroom expires and presentation returns to zero");

                check(effects.Activate(TruckTaxiTemporaryEffectKind.HighOctaneBoost), "Engine boost activates");
                yield return new WaitForSecondsRealtime(1f);
                float boosted = host.Handling.CurrentPowerMultiplier;
                float launch = TruckTaxiVehicleHandlingOverride.LaunchMultiplier(host.Handling.SpeedMph,
                    host.Handling.launchPowerAtRest, host.Handling.launchPowerAt15Mph);
                check(boosted > launch && effects.VehiclePowerMultiplier > 1f,
                    "Active boost increases the live NWH power modifier");
                deadline = Time.realtimeSinceStartup + 45f;
                while (effects.GetSnapshot(TruckTaxiTemporaryEffectKind.HighOctaneBoost).RemainingSeconds > 0 &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                yield return null;
                float restoredLaunch = TruckTaxiVehicleHandlingOverride.LaunchMultiplier(host.Handling.SpeedMph,
                    host.Handling.launchPowerAtRest, host.Handling.launchPowerAt15Mph);
                Debug.Log($"TAXI INTEGRATED ENGINE: boosted={boosted:0.0000}, restored={host.Handling.CurrentPowerMultiplier:0.0000}, liveBaseline={restoredLaunch:0.0000}, speedMph={host.Handling.SpeedMph:0.000}");
                check(effects.GetSnapshot(TruckTaxiTemporaryEffectKind.HighOctaneBoost).RemainingSeconds == 0 &&
                    effects.VehiclePowerMultiplier == 1f &&
                    Mathf.Abs(host.Handling.CurrentPowerMultiplier - restoredLaunch) < .0001f,
                    "Expired boost restores the exact measured engine baseline");
                capture?.Invoke("Integrated_FinalGameplay");
            }
            finally
            {
                if (keyboard != null)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.RemoveDevice(keyboard);
                }
                if (controls != null && controls.IsOpen) controls.Close();
                if (activatedEffects) effects.Clear();
                if (session.RideRequestsEnabled != originalRequests) session.SetRideRequestsEnabled(originalRequests);
                if (hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, originalPreference);
                else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
                PlayerPrefs.Save();
                if (startedShift) session.EndShift();
                host.SetPaused(wasPaused);
            }
        }

        private static float Mean(List<float> sortedSamples)
        {
            if (sortedSamples.Count == 0) return 0;
            double total = 0;
            foreach (float sample in sortedSamples) total += sample;
            return (float)(total / sortedSamples.Count);
        }

        private static float Median(List<float> sortedSamples)
        {
            int count = sortedSamples.Count;
            return count == 0 ? 0 : (sortedSamples[(count - 1) / 2] + sortedSamples[count / 2]) * .5f;
        }

        private static void ReportTraffic(TruckTaxiBootstrap host, int observedMoving, Action<bool, string> check)
        {
            var traffic = host.traffic;
            var personalities = new int[Enum.GetValues(typeof(TruckTaxiDriverPersonality)).Length];
            var lanes = new SortedDictionary<int, int>();
            int observed = 0, horns = 0, laneChanges = 0, movingNow = 0, stoppedWithInfinityObstacle = 0;
            double speedSum = 0;
            foreach (var vehicle in traffic.Vehicles)
            {
                if (vehicle == null || !vehicle.activeInHierarchy) continue;
                var behavior = vehicle.GetComponent<TruckTaxiTrafficBehaviour>();
                if (behavior == null) continue;
                observed++;
                personalities[(int)behavior.Personality]++;
                horns += behavior.HornsPlayed;
                laneChanges += behavior.LaneChanges;
                float speed = behavior.Speed;
                speedSum += speed;
                if (speed > 1f) movingNow++;
                if (speed < .2f && float.IsPositiveInfinity(behavior.ObstacleDistance))
                    stoppedWithInfinityObstacle++;
                lanes.TryGetValue(behavior.LaneIndex, out int count);
                lanes[behavior.LaneIndex] = count + 1;
            }
            int activeCrosswalks = 0, legalCrossings = 0;
            foreach (var intersection in UnityEngine.Object.FindObjectsByType<TruckTaxiIntersection>(FindObjectsSortMode.None))
            {
                if (intersection == null || !intersection.isActiveAndEnabled ||
                    intersection.gameObject.scene != host.gameObject.scene || !intersection.IsConfigured) continue;
                int count = host.pedestrians.LegalCrossingCount(intersection);
                if (count > 0) activeCrosswalks++;
                legalCrossings += count;
            }
            var report = new StringBuilder($"TAXI INTEGRATED TRAFFIC: behaviorVehicles={observed}, movingNowOver1Mps={movingNow}, movedOver1MpsDuringSample={observedMoving}, meanSpeedMps={(observed > 0 ? speedSum / observed : 0):0.000}, stoppedWithInfinityObstacle={stoppedWithInfinityObstacle}, activeLegalCrosswalks={activeCrosswalks}, legalCrossingCountSum={legalCrossings}, hornsPlayed={horns}, laneChanges={laneChanges}, personalities=");
            foreach (TruckTaxiDriverPersonality personality in Enum.GetValues(typeof(TruckTaxiDriverPersonality)))
                report.Append(personality).Append(':').Append(personalities[(int)personality]).Append(' ');
            report.Append("lanes=");
            foreach (var lane in lanes) report.Append(lane.Key).Append(':').Append(lane.Value).Append(' ');
            Debug.Log(report.ToString());
            check(observedMoving > 0, "At least one UTS traffic vehicle exceeded 1 m/s during the 15-second sample (movement only)");
        }

        private static void CheckRendererOnlyMotion(TruckTaxiBootstrap host, Action<bool, string> check)
        {
            var motion = host.GetComponent<TruckTaxiPrivateEventVehicleMotion>();
            var root = host.Player != null ? host.Player.transform : null;
            var body = root != null ? root.GetComponent<Rigidbody>() : null;
            check(motion != null && motion.Ready && !motion.IsActive && body != null,
                "Bound private-event renderer motion is ready and idle");
            if (motion == null || !motion.Ready || motion.IsActive || body == null) return;

            Vector3 rootPosition = root.position, bodyPosition = body.position;
            Quaternion rootRotation = root.rotation, bodyRotation = body.rotation;
            Vector3 velocity = body.linearVelocity, angularVelocity = body.angularVelocity;
            bool wasKinematic = body.isKinematic;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var enabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) enabled[i] = renderers[i].enabled;
            try
            {
                bool began = motion.Begin();
                check(began && motion.IsActive, "Renderer-only private-event motion begins without a companion event");
                if (!began) return;
                motion.Step(.2f);
                var proxy = root.Find("Truck Taxi temporary body presentation");
                int hidden = 0;
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && enabled[i] && !renderers[i].enabled) hidden++;
                check(proxy != null && proxy.localPosition.sqrMagnitude > .000001f && hidden > 0,
                    "Private-event step moves only a visible renderer proxy");
            }
            finally
            {
                motion.StopMotion();
            }
            bool restored = !motion.IsActive;
            for (int i = 0; i < renderers.Length; i++)
                restored &= renderers[i] != null && renderers[i].enabled == enabled[i];
            bool physicalUnchanged = root.position == rootPosition && root.rotation == rootRotation &&
                body.position == bodyPosition && body.rotation == bodyRotation &&
                body.linearVelocity == velocity && body.angularVelocity == angularVelocity &&
                body.isKinematic == wasKinematic;
            check(restored && physicalUnchanged,
                "Stopping renderer-only motion restores original renderers without moving the physical truck");
        }
    }
}
#endif
