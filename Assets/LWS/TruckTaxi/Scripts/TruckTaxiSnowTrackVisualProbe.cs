#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // Opt-in rendered-player fixture; never created during normal gameplay.
    public sealed class TruckTaxiSnowTrackVisualProbe : MonoBehaviour
    {
        private Keyboard keyboard;
        private InputSettings originalInput;
        private InputSettings temporaryInput;
        private int failures;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Application.isEditor || !Array.Exists(Environment.GetCommandLineArgs(),
                arg => arg == "-truck-taxi-snow-track-smoke")) return;
            new GameObject("Taxi snow track visual probe").AddComponent<TruckTaxiSnowTrackVisualProbe>();
        }

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            if (SceneManager.GetActiveScene().name == TruckTaxiMainMenu.SceneName)
                yield return SceneManager.LoadSceneAsync(TruckTaxiMainMenu.FreePlaySceneName);
            float deadline = Time.realtimeSinceStartup + 60;
            while (TruckTaxiBootstrap.Instance?.Ready != true && Time.realtimeSinceStartup < deadline) yield return null;
            TruckTaxiBootstrap host = TruckTaxiBootstrap.Instance;
            if (!Check(host?.Ready == true, "Free Play ready")) { Finish(); yield break; }

            originalInput = InputSystem.settings;
            temporaryInput = Instantiate(originalInput);
            temporaryInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = temporaryInput;
            keyboard = InputSystem.AddDevice<Keyboard>("Taxi snow track keyboard");
            host.StartShift();
            host.SetPaused(false);
            if (!Check(!host.Paused && Time.timeScale > 0, "Truck can drive")) { Finish(); yield break; }
            TruckTaxiSnow snow = host.Environment.Snow;
            TruckTaxiSnowSurface surface = snow.GetComponent<TruckTaxiSnowSurface>();
            if (!Check(surface != null, "Weatherade snow surface exists")) { Finish(); yield break; }

            host.Environment.SetAutomaticWeather(false);
            snow.ClearSnow();
            Check(surface.ActiveWheelTraceSourceCount == 0, "Dry start has no active wheel stamps");
            Check(host.Environment.ForceWeather(TruckTaxiSnow.BlizzardId), "Blizzard selected");
            deadline = Time.realtimeSinceStartup + 45;
            while (snow.PlayerSnowDepth < .3f && Time.realtimeSinceStartup < deadline) yield return null;
            if (!Check(snow.PlayerSnowDepth >= .3f && surface.TraceMaskReady,
                "Accumulated local snow and Weatherade trace mask ready")) { Finish(); yield break; }

            Rigidbody body = host.Player.GetComponent<Rigidbody>();
            Vector3 start = body.position;
            yield return Drive(new KeyboardState(Key.W), 6);
            Vector3 straightEnd = body.position;
            Check(Vector3.Distance(start, straightEnd) > 10, "Straight drive used the actual truck");
            yield return ReadTracePixels(surface);
            Check(surface.WheelTraceSourceCount >= 2 && surface.TraceIndentPixelCount > 0,
                "Wheel contacts produce indented Weatherade tracks");
            Debug.Log($"SNOW TRACK STRAIGHT: active={surface.ActiveWheelTraceSourceCount}, sources={surface.WheelTraceSourceCount}, coverage={surface.Coverage01:0.00}, depth={snow.PlayerSnowDepth:0.00}; {surface.Diagnostic}; {surface.VisualDiagnostic}");
            yield return Capture("Straight");
            yield return CaptureTopDown("Straight", host.Player.transform);

            Vector3 heading = body.transform.forward;
            int straightStamps = surface.WheelTraceStampCount;
            yield return Drive(new KeyboardState(Key.W, Key.D), 4);
            Check(Vector3.Angle(heading, body.transform.forward) > 8, "Steering curved the truck path");
            Check(surface.WheelTraceStampCount > straightStamps, "Curved driving adds fresh wheel-contact tracks");
            Debug.Log($"SNOW TRACK CURVE: active={surface.ActiveWheelTraceSourceCount}, sources={surface.WheelTraceSourceCount}, coverage={surface.Coverage01:0.00}, depth={snow.PlayerSnowDepth:0.00}; {surface.Diagnostic}; {surface.VisualDiagnostic}");
            yield return Drive(new KeyboardState(Key.S), 3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            yield return new WaitForSecondsRealtime(8);
            yield return ReadTracePixels(surface);
            Check(surface.WheelTraceSourceCount >= 2 && surface.TraceActivePixelCount > 0 &&
                surface.TraceIndentPixelCount > 0, "Indented wheel tracks persist after stopping for eight seconds");
            Debug.Log("SNOW TRACK TRACE: " + surface.TracePixelDiagnostic);
            yield return Capture("Stopped");
            yield return CaptureTopDown("Stopped", host.Player.transform);

            host.Environment.SetTime(22f);
            yield return new WaitForSecondsRealtime(1);
            yield return ReadTracePixels(surface);
            Check(surface.TraceIndentPixelCount > 0, "Snow grooves remain available at night");
            yield return Capture("Night");
            yield return CaptureTopDown("Night", straightEnd, heading);

            host.Environment.ForceWeather("clear");
            snow.ClearSnow();
            yield return Drive(new KeyboardState(Key.W), 2);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(.5f);
            Check(surface.Coverage01 <= .001f && surface.ActiveWheelTraceSourceCount == 0,
                "Bare road adds no wheel stamps");
            yield return Capture("Dry");
            Finish();
        }

        private IEnumerator Drive(KeyboardState state, float seconds)
        {
            InputSystem.QueueStateEvent(keyboard, state);
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        private static IEnumerator ReadTracePixels(TruckTaxiSnowSurface surface)
        {
            surface.RequestTracePixelDiagnostic();
            float deadline = Time.realtimeSinceStartup + 3;
            while (surface.TracePixelDiagnostic == "Trace pixel readback pending." && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation"));
            Directory.CreateDirectory(directory);
            TruckTaxiPlayerSmokeTest.Capture(Path.Combine(directory, "SnowTracks_" + name + ".png"));
        }

        private IEnumerator CaptureTopDown(string name, Transform player)
        {
            if (player == null) yield break;
            yield return CaptureTopDown(name, player.position, player.forward);
        }

        private IEnumerator CaptureTopDown(string name, Vector3 target, Vector3 forward)
        {
            yield return new WaitForEndOfFrame();
            Camera camera = Camera.main;
            if (camera == null) yield break;
            Transform view = camera.transform;
            Vector3 position = view.position;
            Quaternion rotation = view.rotation;
            view.position = target + Vector3.up * 23f - forward * 9f;
            view.rotation = Quaternion.LookRotation(target - view.position, Vector3.up);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation"));
            Directory.CreateDirectory(directory);
            TruckTaxiPlayerSmokeTest.Capture(Path.Combine(directory, "SnowTracks_" + name + "_TopDown.png"));
            view.SetPositionAndRotation(position, rotation);
        }

        private bool Check(bool passed, string name)
        {
            if (passed) Debug.Log("SNOW TRACK PASS: " + name);
            else { failures++; Debug.LogError("SNOW TRACK FAIL: " + name); }
            return passed;
        }

        private void Finish()
        {
            Debug.Log($"SNOW TRACK COMPLETE failures={failures}");
            Application.Quit(failures == 0 ? 0 : 2);
        }

        private void OnDestroy()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (originalInput != null) InputSystem.settings = originalInput;
            if (temporaryInput != null) Destroy(temporaryInput);
        }
    }
}
#endif
