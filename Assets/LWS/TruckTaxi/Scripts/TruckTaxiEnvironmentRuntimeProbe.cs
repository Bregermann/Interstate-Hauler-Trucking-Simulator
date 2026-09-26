#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckTaxi
{
    // Opt-in fixture. Screenshots are review artifacts, not assertions that weather looks correct.
    public static class TruckTaxiEnvironmentRuntimeProbe
    {
        public static IEnumerator Run(TruckTaxiBootstrap host, Action<bool, string> check, Action<string> screenshot)
        {
            var environment = host.Environment;
            var needs = host.DriverNeeds;
            check(environment?.IsInitialized == true && needs?.State != null, "Environment and driver-needs hooks initialized");
            if (environment?.IsInitialized != true || needs?.State == null) yield break;
            var body = host.Player.GetComponent<Rigidbody>();
            var position = body.position; var rotation = body.rotation; bool wasKinematic = body.isKinematic;
            var clock = environment.Clock.CurrentSnapshot; bool autoWeather = environment.AutomaticWeather, frozen = environment.Frozen;
            float transitionDuration = environment.settings.weatherTransitionSeconds;
            float rideFrequency = host.configuration.rideFrequency;
            string previousWeather = environment.Weather.CurrentSnapshot.weatherPresetId;
            PassengerProfile passengerFixture = null;
            var previousInputSettings = InputSystem.settings;
            var inputSettings = UnityEngine.Object.Instantiate(previousInputSettings);
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = inputSettings;
            var keyboard = InputSystem.AddDevice<Keyboard>("Taxi driver needs test keyboard");
            var gamepad = InputSystem.AddDevice<Gamepad>("Taxi driver needs test gamepad");
            int environmentEvents = 0, driverEvents = 0;
            Action<TruckTaxiDialogueCategory> onEnvironment = _ => environmentEvents++;
            Action<TruckTaxiDriverNeedEvent> onDriver = _ => driverEvents++;
            environment.EnvironmentEvent += onEnvironment; needs.DriverNeedsEvent += onDriver;
            try
            {
                host.Session.EndShift(); host.StartShift(); body.isKinematic = true;
                host.configuration.rideFrequency = 100000;
                environment.SetAutomaticWeather(false); environment.SetFrozen(false); environment.SetTimeScale(30);
                environment.settings.weatherTransitionSeconds = 5;
                yield return new WaitForSecondsRealtime(.5f);
                check(UnityEngine.Object.FindObjectsByType<TruckTaxiEnvironmentCoordinator>(FindObjectsSortMode.None).Length == 1,
                    "Exactly one Taxi environment policy coordinator");
                double start = environment.Clock.CurrentSnapshot.totalGameSeconds;
                yield return new WaitForSecondsRealtime(1);
                check(environment.Clock.CurrentSnapshot.totalGameSeconds > start + 20, "Authoritative in-game time advances at configured scale");
                environment.SetFrozen(true); double stopped = environment.Clock.CurrentSnapshot.totalGameSeconds;
                yield return new WaitForSecondsRealtime(.4f);
                check(Math.Abs(stopped - environment.Clock.CurrentSnapshot.totalGameSeconds) < .001, "Freeze stops the existing game clock");
                environment.SetFrozen(false); host.SetPaused(true); stopped = environment.Clock.CurrentSnapshot.totalGameSeconds;
                yield return new WaitForSecondsRealtime(.4f);
                check(Math.Abs(stopped - environment.Clock.CurrentSnapshot.totalGameSeconds) < 2, "Taxi pause suppresses elapsed game time");
                host.SetPaused(false);
                environment.SetTime(19); yield return new WaitForSecondsRealtime(1);
                check(environment.Period == TruckTaxiDayPeriod.Sunset, "Set sunset maps to the semantic sunset period"); screenshot?.Invoke("Environment_Sunset");
                environment.SetTime(22); yield return new WaitForSecondsRealtime(1);
                check(environment.Period == TruckTaxiDayPeriod.Night && environment.weatherAdapter.GameClockSlaved, "Night uses the slaved Weather Maker clock");
                check(Mathf.Abs(environment.weatherAdapter.WeatherMakerTimeOfDayHours - environment.Clock.CurrentSnapshot.timeOfDayHours) < .2f,
                    "Weather Maker time follows LWS time"); screenshot?.Invoke("Environment_Night");
                check(environmentEvents >= 2, "Sunset and night emit authored passenger reaction events");
                environment.SetTime(12);
                foreach (string weather in new[] { "light_rain", "fog", "thunderstorm" })
                {
                    bool accepted = environment.ForceWeather(weather);
                    check(accepted, "Existing weather adapter accepts " + weather);
                    yield return Until(() => !environment.Weather.CurrentSnapshot.transitioning, 10);
                    check(environment.Weather.CurrentSnapshot.weatherPresetId == weather && !environment.Weather.CurrentSnapshot.transitioning,
                        "Semantic weather transition completes: " + weather);
                    check(environment.weatherAdapter.LastWeatherMakerApplySucceeded, "Weather Maker public adapter reports apply success: " + weather);
                    yield return new WaitForSecondsRealtime(1);
                    if (weather == "fog")
                    {
                        var fog = environment.weatherAdapter.WeatherMakerRuntimeRoot.GetComponentsInChildren<MonoBehaviour>(true)
                            .FirstOrDefault(component => component.GetType().FullName == "DigitalRuby.WeatherMaker.WeatherMakerFullScreenFogScript");
                        var profile = fog?.GetType().GetField("FogProfile")?.GetValue(fog);
                        float density = profile == null ? -1 : (float)profile.GetType().GetField("FogDensity").GetValue(profile);
                        float expected = Mathf.Lerp(.0006f, .018f, environment.Weather.CurrentSnapshot.fogIntensity01);
                        check(Mathf.Abs(density - expected) < .00001f,
                            $"Vendor fog transition retains semantic density after completion: {density:0.00000} expected {expected:0.00000}");
                    }
                    screenshot?.Invoke("Environment_" + weather);
                }
                if (TruckTaxiAudioController.Instance?.Ready == true && environment.WeatherAudioRoot != null)
                {
                    TruckTaxiAudioController.Instance.RouteWorldTree(environment.WeatherAudioRoot);
                    var sources = environment.WeatherAudioRoot.GetComponentsInChildren<AudioSource>(true);
                    check(sources.Length > 0 && sources.All(s => s.outputAudioMixerGroup != null), "Weather source outputs are assigned to mixer routing");
                }
                else Debug.Log("TAXI ENVIRONMENT PROBE: weather mixer routing UNVERIFIED; configured audio mixer not ready. No audible-volume PASS claimed.");
                needs.State.SetPressure(.2f); float pressure = needs.State.Pressure; environment.AdvanceHour();
                check(needs.State.Pressure > pressure, "Bladder grows from elapsed authoritative game hours");
                check(needs.Bathrooms.Count >= 2, "Authored restroom locations are registered");
                if (needs.Bathrooms.Count == 0) yield break;
                var bathroom = needs.Bathrooms[0];
                check(needs.RouteToBathroom() && host.GPS.TargetId == needs.RoutedBathroom.stableId, "Restroom route uses existing GPS service target");
                Place(host, bathroom.Position + Vector3.right * 40); needs.RefreshProximity();
                check(!needs.UseBathroom(), "Bathroom relief rejected away from the actual bay");
                Place(host, bathroom.Position); needs.RefreshProximity();
                check(needs.UseBathroom() && needs.State.Pressure == 0, "Stationary relief works in an authored restroom bay");
                needs.State.SetPressure(.9f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Insert));
                yield return Until(() => needs.JugActive, 1);
                check(needs.JugActive, "Keyboard Insert starts the jug interaction without a pointer");
                yield return CompleteJug(needs, keyboard, null);
                check(needs.State.FilledJug && !needs.State.CabMess, "Three real Input System keyboard cues complete the jug without a spill");
                screenshot?.Invoke("DriverNeeds_FilledJug");
                check(needs.DisposeJug() && !needs.State.FilledJug, "Filled jug disposal at the actual restroom enables reuse");
                needs.State.SetPressure(.9f); needs.StartJug();
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.LeftShoulder));
                yield return CompleteJug(needs, null, gamepad);
                check(needs.State.FilledJug && needs.State.SuccessfulJugs >= 2, "Controller LB hold and RB timed cues complete the same jug implementation");
                needs.DisposeJug(); needs.State.SetPressure(.9f); needs.StartJug();
                yield return new WaitForSecondsRealtime(1.1f);
                check(needs.State.Spills > 0 && needs.State.CabMess, "Releasing the jug input produces a spill and cab-mess state");
                check(needs.CleanCab() && !needs.State.CabMess, "Cab cleanup at a service location clears the mess");
                needs.State.SetPressure(1); int accidents = needs.State.CrisisAccidents;
                environment.Clock.AddHours(environment.settings.crisisGraceGameMinutes / 60 + .01f);
                check(needs.State.CrisisAccidents == accidents + 1 && host.Session.State == TruckTaxiState.Available,
                    "Ignored bladder crisis records a nonfatal consequence without ending the shift");
                check(driverEvents >= 8, "Driver events emitted for authored passenger reaction categories");
                var panel = host.hud.EnvironmentNeeds;
                if (panel != null)
                {
                    panel.Open(); yield return null;
                    check(panel.IsOpen && panel.FocusRoot != null, "Driver-needs Heat panel exposes a keyboard/gamepad focus scope");
                    screenshot?.Invoke("DriverNeeds_Panel"); panel.Close();
                }
                var source = host.configuration.passengerDatabase != null ? host.configuration.passengerDatabase.passengers.FirstOrDefault(p => p != null) :
                    host.configuration.passengers.FirstOrDefault(p => p != null);
                if (source != null)
                {
                    passengerFixture = UnityEngine.Object.Instantiate(source);
                    passengerFixture.uniqueMechanics = Array.Empty<TruckTaxiMechanic>(); passengerFixture.possibleRequests = Array.Empty<PassengerRequestDefinition>();
                    passengerFixture.pairPassenger = passengerFixture.companion = null; passengerFixture.basePatience = passengerFixture.requestFrequency = 10000;
                    passengerFixture.specialAppreciationEligible = false;
                    check(host.Session.OfferRide(passengerFixture) && host.Session.AcceptRide(), "Reaction fixture uses existing offer/accept flow");
                    host.TeleportNear(host.Session.Pickup); yield return Until(() => host.Session.HasPassenger, 15);
                    check(host.Session.HasPassenger, "Reaction fixture passenger actually boards");
                    if (host.Session.HasPassenger)
                    {
                        host.Passengers.Dialogue.Stop(); int spoken = host.Passengers.Dialogue.SpokenLines;
                        needs.State.SetPressure(.86f); environment.Clock.AddHours(.001f); yield return null;
                        check(host.Passengers.Dialogue.SpokenLines > spoken, "BladderUrgent reaches existing passenger dialogue/subtitle output");
                    }
                }
                Debug.Log("TAXI ENVIRONMENT PROBE: screenshots captured for visual review. Actual sky readability, rain/fog pixels, and audible volume require review; semantic checks are not visual proof.");
            }
            finally
            {
                needs.State.CancelJug(); environment.EnvironmentEvent -= onEnvironment; needs.DriverNeedsEvent -= onDriver;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.QueueStateEvent(gamepad, new GamepadState());
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad); InputSystem.settings = previousInputSettings;
                UnityEngine.Object.Destroy(inputSettings);
                host.configuration.rideFrequency = rideFrequency;
                body.position = position; body.rotation = rotation; host.Player.transform.SetPositionAndRotation(position, rotation);
                body.isKinematic = wasKinematic; if (!wasKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                Physics.SyncTransforms(); host.Session.DiscardTeleportDistance();
                environment.settings.weatherTransitionSeconds = transitionDuration;
                environment.Weather.RequestWeather(previousWeather, 0, true);
                environment.SetTime(clock.timeOfDayHours); environment.SetTimeScale(clock.timeScale);
                environment.SetAutomaticWeather(autoWeather); environment.SetFrozen(frozen); host.Session.EndShift();
                if (passengerFixture != null) UnityEngine.Object.Destroy(passengerFixture);
            }
        }
        public static IEnumerator CompleteJug(TruckTaxiDriverNeedsCoordinator needs, Keyboard keyboard, Gamepad gamepad)
        {
            for (int cue = 0; cue < 3; cue++)
            {
                float target = (cue + 1) * .25f;
                yield return Until(() => !needs.JugActive || needs.State.JugProgress >= target - .015f, 4);
                if (!needs.JugActive) break;
                if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Insert, Key.Delete));
                else InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.RightShoulder));
                yield return null;
                if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Insert));
                else InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.LeftShoulder));
                yield return null;
            }
            yield return Until(() => !needs.JugActive, 4);
            if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            else InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
        private static IEnumerator Until(Func<bool> predicate, float timeout)
        { float until = Time.realtimeSinceStartup + timeout; while (!predicate() && Time.realtimeSinceStartup < until) yield return null; }
        private static void Place(TruckTaxiBootstrap host, Vector3 position)
        {
            var body = host.Player.GetComponent<Rigidbody>(); body.position = position + Vector3.up * 1.6f;
            host.Player.transform.position = body.position; Physics.SyncTransforms(); host.Session.DiscardTeleportDistance();
        }
    }
}
#endif
