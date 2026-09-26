#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // Explicit focused integration fixture. Never created during ordinary gameplay.
    public sealed class TruckTaxiCorrectionProbe : MonoBehaviour
    {
        public bool Complete { get; private set; }
        public int Checks { get; private set; }
        public int Failures { get; private set; }
        private Keyboard keyboard;
        private InputSettings originalInput;
        private TruckTaxiBootstrap host;
        private bool previousRequests;
        private int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (!Application.isEditor && Array.Exists(Environment.GetCommandLineArgs(), a => a == "-truck-taxi-correction-smoke"))
                new GameObject("Taxi correction validation").AddComponent<TruckTaxiCorrectionProbe>();
        }
        private void Check(bool pass, string name)
        { Checks++; if (pass) Debug.Log("CORRECTION PASS: " + name); else { Failures++; Debug.LogError("CORRECTION FAIL: " + name); } }
        private void LogError(string message, string stack, LogType type)
        { if ((type == LogType.Error || type == LogType.Exception) && !message.StartsWith("CORRECTION FAIL:")) errors++; }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Application.runInBackground = true;
            Application.logMessageReceived += LogError;
            originalInput = InputSystem.settings; InputSystem.settings = Instantiate(originalInput);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("Taxi correction keyboard");
            if (SceneManager.GetActiveScene().name == TruckTaxiMainMenu.SceneName) SceneManager.LoadScene(TruckTaxiMainMenu.FreePlaySceneName);
            float deadline = Time.realtimeSinceStartup + 60;
            while (TruckTaxiBootstrap.Instance?.Ready != true && Time.realtimeSinceStartup < deadline) yield return null;
            host = TruckTaxiBootstrap.Instance;
            Check(host?.Ready == true, "Taxi initialized");
            if (host?.Ready != true) { Finish(); yield break; }
            previousRequests = host.Session.RideRequestsEnabled;
            host.Session.SetRideRequestsEnabled(false); host.StartShift(); host.Environment.SetAutomaticWeather(false);
            host.Environment.ForceWeather("clear"); host.Environment.Snow.ClearSnow();
            var body = host.Player.GetComponent<Rigidbody>(); var vehicle = host.Player.NwhAdapter.VehicleController;
            var needs = host.DriverNeeds;
            Check(TruckTaxiNeedsItems.Store.All(e => needs.State.Count(e.Item) >= 1), "Free Play starter categories stocked");
            Check(!host.Session.OfferRide(), "Ride Requests OFF blocks dispatch");
            Check(needs.RouteToService(TruckTaxiServiceCapability.Restroom), "Restroom capability lookup");
            string serviceId = host.GPS.TargetId;
            host.Session.SetRideRequestsEnabled(true);
            Check(host.Session.OfferRide(), "Offer generated for route ownership check");
            host.Session.DeclineRide();
            Check(host.GPS.IsServiceDestination && host.GPS.TargetId == serviceId, "Decline preserves exact service target");
            host.Session.SetRideRequestsEnabled(false); host.GPS.ClearDestination(); host.SetPaused(false);
            // Full needs do not write engine or input state. Measure the real dry launch using keyboard input.
            needs.State.AdvanceGameSeconds(9 * 3600);
            Check(needs.State.Hunger == 1 && needs.State.Thirst == 1, "Hunger and thirst at 100 percent");
            float peak = 0, began = Time.time, sample = 0;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            while (Time.time - began < 8)
            {
                peak = Mathf.Max(peak, vehicle.Speed);
                if (Time.time >= sample)
                {
                    sample = Time.time + 1;
                    Debug.Log($"TAXI DRY TRACE t={Time.time-began:0.0}s mph={vehicle.Speed*2.236936f:0.0} throttle={vehicle.input.Throttle:0.00} brake={vehicle.input.Brakes:0.00} park={vehicle.input.Handbrake:0.00} rpm={vehicle.powertrain.engine.OutputRPM:0} gear={vehicle.powertrain.transmission.GearName} damage={vehicle.powertrain.engine.Damage:0.00} rolling={vehicle.powertrain.wheels[0].wheelUAPI.RollingResistanceTorque:0.0}");
                }
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            Check(peak > 5, $"Dry launch with full needs remains driveable (peak {peak*2.236936f:0.0} MPH in 8s)");
            needs.State.AddItem(TruckTaxiNeedsItem.FilledBottle);
            Check(needs.ThrowFilledContainer(), "Filled bottle thrown away from service bay while driving");
            yield return new WaitForSeconds(.2f);
            Check(FindObjectsByType<TruckTaxiFilledContainerProjectile>(FindObjectsSortMode.None).Length > 0, "Physical throw projectile exists");
            vehicle.powertrain.engine.Damage = 1;
            host.Roadside.RefreshCondition();
            Check(host.Roadside.Condition.Contains("ENGINE DAMAGED"), "Authoritative engine failure displayed");
            Check(host.Roadside.PrepareQuote(), "Tow quote resolves authored repair point");
            var destination = host.Roadside.QuotedDestination;
            long before = host.Session.ServiceChargesCents, cost = host.Roadside.QuotedCostCents;
            Check(host.Roadside.ConfirmTow(), "Confirmed tow starts");
            deadline = Time.realtimeSinceStartup + 10;
            while (host.Roadside.IsRecovering && Time.realtimeSinceStartup < deadline) yield return null;
            Check(!host.Roadside.IsRecovering && vehicle.powertrain.engine.Damage == 0 && vehicle.powertrain.engine.IsRunning, "Tow repairs and restarts NWH engine");
            Check(host.Session.ServiceChargesCents == before + cost, "Tow charges exact quote once");
            Check(destination != null && Vector3.Distance(body.position, destination.RecoveryPosition) < 8 && Vector3.Dot(body.transform.up, Vector3.up) > .95f, "Tow returns existing truck upright to service");
            needs.RefreshProximity();
            Check(needs.UseBathroom(), "Gas/repair bay provides existing restroom behavior");
            Check(needs.RouteToStore() && needs.RouteToService(TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink) && needs.RouteToService(TruckTaxiServiceCapability.Refuel), "Store food drink and gas navigation");
            host.hud.EnvironmentNeeds.OpenServices();
            yield return new WaitForSecondsRealtime(.4f);
            Capture("Services"); host.hud.EnvironmentNeeds.Close(); host.SetPaused(false);
            var snow = host.Environment.Snow;
            foreach (string id in new[] { "light_snow", "heavy_snow", "blizzard" }) Check(host.Environment.ForceWeather(id), "Explicit weather " + id);
            deadline = Time.time + 27;
            while (Time.time < deadline) yield return null;
            Check(snow.Region.Cells.Any(c => c.depth >= .58f), "Blizzard reaches severe gameplay depth in 27 seconds");
            var surface = snow.GetComponent<TruckTaxiSnowSurface>();
            Check(surface != null && surface.DepthCollider == null && surface.RenderedCells == 0, "No visible snow slabs or raised collider");
            Check(surface != null && surface.TraceMaskReady && surface.RoadDepthSourceCount > 0, "Weatherade accumulation and trace cameras ready");
            Check(surface != null && surface.SnowTextureReady && surface.BoundDepthCameraCount == 3,
                "Snow textures and all three vendor depth cameras use configured SRS renderer");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(4);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            surface.RequestTracePixelDiagnostic();
            yield return new WaitForSecondsRealtime(.3f);
            Check(surface.WheelTraceSourceCount >= 2, "Actual grounded wheel contacts supplied left and right trace inputs");
            Check(surface.TraceActivePixelCount > 0 && surface.TraceIndentPixelCount > 0,
                "Weatherade GPU trace mask contains contact and indentation pixels");
            Debug.Log("CORRECTION TRACE: " + surface.TracePixelDiagnostic);
            Debug.Log("CORRECTION SNOW: " + surface?.Diagnostic);
            Capture("Snow");
            Check(host.pedestrians.GraphNodeCount > 100 && host.pedestrians.GraphWalkerCount > 0, "Broad pedestrian graph is active");
            host.Session.SetRideRequestsEnabled(previousRequests);
            Finish();
        }
        private void Capture(string name)
        {
            string directory = Application.isEditor ? Path.GetFullPath("Builds/TruckTaxiDemo/Validation") : Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation"));
            Directory.CreateDirectory(directory); TruckTaxiPlayerSmokeTest.Capture(Path.Combine(directory, "Correction_" + name + ".png"));
        }
        private void Finish()
        {
            Check(errors == 0, "No unexpected runtime errors (" + errors + ")");
            Complete = true; Debug.Log($"CORRECTION COMPLETE checks={Checks} failures={Failures}");
            if (!Application.isEditor) Application.Quit(Failures == 0 ? 0 : 2);
        }
        private void OnDestroy()
        {
            Application.logMessageReceived -= LogError;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (originalInput != null) { var temporary = InputSystem.settings; InputSystem.settings = originalInput; Destroy(temporary); }
            if (host?.Session != null) host.Session.SetRideRequestsEnabled(previousRequests);
        }
    }
}
#endif
