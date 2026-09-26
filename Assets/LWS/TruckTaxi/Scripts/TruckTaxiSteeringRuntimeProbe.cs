#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using NWH.Common.Cameras;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckTaxi
{
    // Opt-in development fixture, never auto-started. Uses the actual Input System and NWH resolved angle.
    public static class TruckTaxiSteeringRuntimeProbe
    {
        public static IEnumerator Run(TruckTaxiBootstrap host, Action<bool, string> check, Action<string> capture)
        {
            var visual = host != null && host.Player != null ? host.Player.GetComponent<TruckTaxiSteeringWheelVisual>() : null;
            check(visual != null && visual.Applied, "Taxi visual steering presenter is the active wheel owner");
            if (visual == null || !visual.Applied) yield break;
            var vehicle = host.Player.NwhAdapter.VehicleController;
            float lockBefore = vehicle.steering.maximumSteerAngle, rateBefore = vehicle.steering.degreesPerSecondLimit;
            var curveBefore = vehicle.steering.speedSensitiveSteeringCurve;
            var inputSettings = InputSystem.settings;
            var temporarySettings = UnityEngine.Object.Instantiate(inputSettings);
            InputSystem.settings = temporarySettings;
            temporarySettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            temporarySettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>("Taxi visual steering validation keyboard");
            float previousCaptureDelta = Time.captureDeltaTime;
            int previousRate = Application.targetFrameRate, previousSync = QualitySettings.vSyncCount;
            bool paused = host.Paused;
            var changer = host.Player.GetComponentInChildren<CameraChanger>(true);
            int previousCamera = changer != null ? changer.currentCameraIndex : -1;
            float frequency = host.configuration.rideFrequency;
            try
            {
                host.configuration.rideFrequency = 600;
                host.Session.EndShift(); host.StartShift();
                if (changer != null)
                    for (int i = 0; i < changer.cameras.Count && !changer.cameras[changer.currentCameraIndex].name.Contains("Driver"); i++) changer.NextCamera();
                QualitySettings.vSyncCount = 0;
                foreach (int fps in new[] { 30, 60, 120 })
                {
                    Application.targetFrameRate = fps; Time.captureDeltaTime = 1f / fps;
                    yield return null; // Flush the previous frame's delta before comparing per-frame limits at the new rate.
                    float peakLeft = 0, peakRight = 0;
                    foreach (string phase in new[] { "Left", "ReleaseLeft", "Right", "ReleaseRight", "RapidTaps", "SlowTaps" })
                    {
                        float before = visual.CurrentVisualAngle;
                        float maximumStep = 0;
                        bool bounded = true;
                        int frames = fps * 2;
                        for (int i = 0; i < frames; i++)
                        {
                            Key steer = phase == "Left" ? Key.A : phase == "Right" ? Key.D :
                                phase == "RapidTaps" ? ((i / Mathf.Max(1, fps / 10)) % 2 == 0 ? Key.A : Key.D) :
                                phase == "SlowTaps" ? ((i / Mathf.Max(1, fps / 2)) % 2 == 0 ? Key.A : Key.D) : Key.None;
                            // Exercise the existing brake/reverse input; automatic D/N/R may move the
                            // truck at low speed. Never freeze its Rigidbody or edit steering physics.
                            InputSystem.QueueStateEvent(keyboard, steer == Key.None ? new KeyboardState(Key.S) : new KeyboardState(Key.S, steer));
                            yield return null;
                            float step = Mathf.Abs(visual.CurrentVisualAngle - before);
                            maximumStep = Mathf.Max(step, maximumStep);
                            bounded &= step <= visual.visualAngularVelocityLimit * Mathf.Min(Time.deltaTime, .1f) + .2f;
                            before = visual.CurrentVisualAngle;
                            peakLeft = Mathf.Min(peakLeft, before); peakRight = Mathf.Max(peakRight, before);
                            if (i == 0 || i == fps / 4 || i == fps || i == frames - 1)
                            {
                                Debug.Log($"TAXI STEERING TRACE: simulatedHz={fps} phase={phase} frame={i} {visual.DiagnosticSummary}");
                                capture?.Invoke($"Steering_{fps}_{phase}_{i:D3}");
                            }
                        }
                        check(bounded, $"{fps} Hz simulated {phase}: continuous signed motion, max step {maximumStep:0.00} deg");
                        if (phase.StartsWith("Release")) check(Mathf.Abs(visual.CurrentVisualAngle) < 5, fps + " Hz simulated smoothly returns to centre");
                    }
                    check(peakLeft < -300 && peakRight > 300, fps + " Hz simulated reached both sides through real resolved NWH steering");
                }
                check(vehicle.steering.maximumSteerAngle == lockBefore && vehicle.steering.degreesPerSecondLimit == rateBefore &&
                    vehicle.steering.speedSensitiveSteeringCurve == curveBefore, "Visual probe/presenter preserved arcade handling settings");
                check(vehicle.steering.steeringWheel == null && !host.Player.DashboardController.SteeringWheelAnimationEnabled,
                    "Exactly one wheel transform writer throughout the test");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.settings = inputSettings;
                UnityEngine.Object.Destroy(temporarySettings);
                Time.captureDeltaTime = previousCaptureDelta; Application.targetFrameRate = previousRate; QualitySettings.vSyncCount = previousSync;
                host.configuration.rideFrequency = frequency;
                if (changer != null && previousCamera >= 0)
                    for (int i = 0; i < changer.cameras.Count && changer.currentCameraIndex != previousCamera; i++) changer.NextCamera();
                host.SetPaused(paused);
            }
        }
    }
}
#endif
