#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using NWH.Common.Cameras;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Opt-in observations of the production world. Driving uses only the existing input/NWH path.
    public static class TruckTaxiRegionalRuntimeProbe
    {
        private sealed class DriveResult { public bool Reached; public string Detail = "not attempted"; }

        public static IEnumerator Run(TruckTaxiBootstrap host, Action<bool, string> check, Action<string> capture, bool companionOnly=false, bool skipIntercity=false)
        {
            if (check == null) yield break;
            var world = host != null ? host.GetComponent<TruckTaxiRegionalWorld>() : null;
            var body = host?.Player != null ? host.Player.GetComponent<Rigidbody>() : null;
            if (host?.Ready != true || world == null || body == null || host.hud?.FullMap == null ||
                host.traffic == null || host.pedestrians == null || host.RouteDistances == null ||
                host.DriverNeeds?.State == null || host.Companions == null || host.GPS == null)
            { check(false, "Regional probe dependencies ready"); yield break; }

            bool paused = host.Paused, startedShift = host.Session.State == TruckTaxiState.Inactive;
            bool requests = host.Session.RideRequestsEnabled;
            bool hadRequestPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            int requestPreference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);
            bool effectOwned = false, companionOwned = false;
            int width = Screen.width, height = Screen.height;
            var settings = InputSystem.settings;
            var temporary = UnityEngine.Object.Instantiate(settings);
            Keyboard keyboard = null;
            var changer = host.Player.GetComponentInChildren<CameraChanger>(true);
            int priorCamera = changer != null ? changer.currentCameraIndex : -1;
            try
            {
                InputSystem.settings = temporary;
                temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                keyboard = InputSystem.AddDevice<Keyboard>("Taxi regional probe keyboard");
                if (startedShift) host.StartShift(); else host.SetPaused(false);
                host.Session.SetRideRequestsEnabled(false);
                check(host.Session.State == TruckTaxiState.Available, "Free Play available");
                if (host.Session.State != TruckTaxiState.Available) yield break;

                yield return new WaitForSecondsRealtime(2);
                if(!companionOnly)
                {
                int cars = host.traffic.LogicalCount, people = host.pedestrians.LogicalCount;
                check(cars == 150 && people == 720, $"Logical cars={cars}/150 pedestrians={people}/720");
                int liveCars = 0, fullCars = 0, livePeople = 0, fullPeople = 0;
                var frame = new List<float>(); var main = new List<float>(); var physics = new List<float>();
                bool mainValid, physicsValid;
                using (var m = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
                using (var p = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 1))
                {
                    mainValid = m.Valid; physicsValid = p.Valid;
                    float end = Time.realtimeSinceStartup + 15;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        frame.Add(Time.unscaledDeltaTime * 1000);
                        if (m.Valid) main.Add(m.LastValue * 1e-6f);
                        if (p.Valid) physics.Add(p.LastValue * 1e-6f);
                        liveCars = Mathf.Max(liveCars, host.traffic.ActiveCount);
                        fullCars = Mathf.Max(fullCars, host.traffic.FullPhysicsCount);
                        livePeople = Mathf.Max(livePeople, host.pedestrians.ActiveCount);
                        fullPeople = Mathf.Max(fullPeople, host.pedestrians.FullPhysicsCount);
                    }
                }
                Debug.Log($"TAXI REGIONAL PERF: frames={frame.Count} seconds=15 fps={(Mean(frame) > 0 ? 1000 / Mean(frame) : 0):F1} mainMs={Mean(main):F2} physicsMs={Mean(physics):F2} bodies={world.ActiveBodies} " +
                    $"cars logical={cars} live={host.traffic.ActiveCount} full={host.traffic.FullPhysicsCount} pooled={host.traffic.PooledCount} peakLive={liveCars} peakFull={fullCars}; " +
                    $"ped logical={people} live={host.pedestrians.ActiveCount} full={host.pedestrians.FullPhysicsCount} pooled={host.pedestrians.PooledCount} peakLive={livePeople} peakFull={fullPeople}");
                check(frame.Count > 0 && mainValid && physicsValid && main.Count > 0 && physics.Count > 0,
                    "15-second FPS/Main Thread/Physics.Simulate measured");
                check(liveCars < cars && fullCars <= host.traffic.maxFullTraffic &&
                    liveCars <= host.traffic.maxFullTraffic + host.traffic.maxVisibleTraffic + host.traffic.maximumDedicatedVehicles &&
                    livePeople < people && livePeople <= host.pedestrians.maxVisiblePedestrians &&
                    fullPeople <= host.pedestrians.maxFullPedestrians,
                    $"Bounded live/full simulation: cars={liveCars}/{fullCars}, pedestrians={livePeople}/{fullPeople}");
                check(host.traffic.PooledCount + host.pedestrians.PooledCount > 0,
                    $"Pooled actors observed: cars={host.traffic.PooledCount} pedestrians={host.pedestrians.PooledCount}");

                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3440, 1440) })
                {
                    Screen.SetResolution(size.x, size.y, false);
                    yield return new WaitForSecondsRealtime(1);
                    var debug = host.hud.GetComponent<TruckTaxiDebugPanel>();
                    debug?.Toggle();
                    yield return null;
                    var band = host.hud.Root.Find("Taxi time and driver needs")?.GetComponent<CanvasGroup>();
                    var bars = host.hud.Root.Find("Vehicle status bars");
                    check(Screen.width == size.x && Screen.height == size.y && host.hud.DebugOverlayOpen &&
                        host.hud.NormalHudSuppressed && band != null && band.alpha == 0 &&
                        bars != null && !bars.gameObject.activeSelf,
                        $"Debug owns screen; normal needs/status hidden at {size.x}x{size.y} (actual {Screen.width}x{Screen.height})");
                    capture?.Invoke($"Regional_Debug_{size.x}x{size.y}");
                    if (debug?.IsOpen == true) debug.Toggle();
                    yield return null;
                    check(NeedLabels(host.hud.Root, "BLADDER") == 1 && NeedLabels(host.hud.Root, "HUNGER") == 1 &&
                        NeedLabels(host.hud.Root, "THIRST") == 1 && !host.hud.NormalHudSuppressed,
                        $"One normal-HUD label per need at {size.x}x{size.y}");
                }

                TruckTaxiRegion unloaded = null;
                foreach (var region in world.regions)
                    if (!world.Loaded(region) && (unloaded == null || region.id.Contains("town02"))) unloaded = region;
                bool metadata = false;
                if (unloaded != null && host.GPS.MapMarkers != null)
                    foreach (var point in host.GPS.MapMarkers.RegionalPoints)
                        if (point.Id == unloaded.id) { metadata = true; break; }
                check(metadata, "Unloaded region has persistent map metadata");
                int loadedBefore = world.LoadedCount;
                Press(keyboard, Key.F3); yield return null; Release(keyboard); yield return null;
                check(host.hud.FullMap.IsOpen && host.GPS.FullMapOpen && host.Paused, "F3 opens shared full-map state");
                yield return new WaitForSecondsRealtime(.5f);
                capture?.Invoke("Regional_FullMap");
                if (host.hud.FullMap.IsOpen) host.hud.FullMap.Close();
                yield return null;
                var expand = host.GPS.HudCompass?.transform.Find("MiniMap Root/Expand regional map")?.GetComponent<Button>();
                if (expand != null && EventSystem.current != null)
                    ExecuteEvents.Execute<IPointerClickHandler>(expand.gameObject,
                        new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                yield return null;
                check(expand != null && host.hud.FullMap.IsOpen && host.GPS.FullMapOpen, "Minimap expand button uses shared full-map state");
                yield return new WaitForSecondsRealtime(1);
                check(metadata && world.LoadedCount == loadedBefore && unloaded != null && !world.Loaded(unloaded),
                    "Map opening does not load unloaded world");
                if (host.hud.FullMap.IsOpen) host.hud.FullMap.Close();
                yield return null;

                var effects = host.DriverNeeds.State.Effects;
                var presentation = host.GetComponent<TruckTaxiEffectPresentation>();
                if (changer == null || presentation == null || effects.ActiveCount != 0)
                    check(false, "Gameplay cameras available and mushroom baseline idle");
                else
                    foreach (string view in new[] { "Driver", "Cinemachine" })
                    {
                        var camera = SelectCamera(changer, view);
                        if (camera == null) { check(false, view + " gameplay camera available"); continue; }
                        yield return null;
                        float before = Saturation(camera);
                        capture?.Invoke("Regional_Mushroom" + view + "Before");
                        bool activated = effects.DebugMushroomPeak();
                        effectOwned |= activated;
                        yield return null; yield return null;
                        float peak = Saturation(camera);
                        capture?.Invoke("Regional_Mushroom" + view + "Peak");
                        Debug.Log($"TAXI REGIONAL MUSHROOM: camera={camera.name} before={before:F4} peak={peak:F4} intensity={presentation.Intensity:F3}");
                        check(activated && presentation.Intensity > .9f && peak > before + .01f,
                            view + " actual gameplay camera has visibly increased pixel saturation");
                        effects.Clear(); yield return null;
                    }

                }
                host.DriverNeeds.State.DebugSetThirst(1f);
                bool spawned = !host.Companions.IsBusy && host.Companions.DebugSpawnNearPlayer();
                yield return new WaitForSecondsRealtime(.4f);
                check(host.DriverNeeds.State.Thirst >= .99f && spawned && host.Companions.CanInteract,
                    "Thirst full and companion interactable beside stopped normal truck");
                bool canBoard = spawned && host.Companions.CanInteract;
                if (canBoard) { Press(keyboard, Key.Enter); yield return null; Release(keyboard); yield return null; }
                bool boarded = canBoard && host.Companions.IsBusy && host.GPS.IsServiceDestination;
                companionOwned = boarded;
                check(boarded && host.Companions.IsBusy && host.GPS.IsServiceDestination,
                    "Enter interaction boards companion and sets service GPS");
                if (boarded)
                {
                    bool forced = host.Companions.DebugForceNearbyPrivateStop();
                    check(forced, "Nearby private stop chosen");
                    var stop = FindStop(host.GPS.TargetId);
                    var drive = new DriveResult();
                    if (stop != null) yield return Drive(host, body, keyboard, stop.Position, 100, drive);
                    bool arrived = stop != null && drive.Reached && host.Companions.CanInteract;
                    check(arrived, "Input-driven NWH arrival at valid private stop: " + drive.Detail);
                    if (arrived)
                    {
                        // Park through the normal control before releasing the brake; idle creep cancels the event.
                        var controls = host.Player.GetComponent<LWS.InterstateHauler.LwsTruckControlController>();
                        bool parkingWasOn = controls != null && controls.CurrentState.parkingBrakeOn;
                        if (!parkingWasOn) { Press(keyboard, Key.P); yield return null; Release(keyboard); yield return null; }
                        check(controls != null && controls.CurrentState.parkingBrakeOn, "Normal parking brake holds private stop");
                        Press(keyboard, Key.Enter); yield return null; Release(keyboard); yield return null;
                        var motion = host.GetComponent<TruckTaxiPrivateEventVehicleMotion>();
                        check(motion != null && motion.IsActive, "Enter at stopped truck begins private event");
                        capture?.Invoke("Regional_CompanionEvent");
                        float maxBounce = 0, maxRock = 0;
                        float minimumThirst = 1;
                        bool completionObserved = false;
                        float eventEnd = Time.realtimeSinceStartup + 8;
                        while (Time.realtimeSinceStartup < eventEnd)
                        {
                            yield return null;
                            minimumThirst = Mathf.Min(minimumThirst,host.DriverNeeds.State.Thirst);
                            completionObserved |= !host.Companions.IsBusy && host.Companions.Feedback=="THIRST SATISFIED";
                            var proxy = host.Player.transform.Find("Truck Taxi temporary body presentation");
                            if (proxy == null) continue;
                            maxBounce = Mathf.Max(maxBounce, Mathf.Abs(proxy.localPosition.y));
                            maxRock = Mathf.Max(maxRock, Quaternion.Angle(Quaternion.identity, proxy.localRotation));
                        }
                        check(maxBounce > .25f && maxRock > 10,
                            $"Visible hydraulic event motion: bounce={maxBounce:F2}m rock={maxRock:F1}deg");
                        // Thirst resumes accumulating at the semantic clock rate after completion.
                        check(completionObserved && !host.Companions.IsBusy && minimumThirst <= .001f &&
                            !host.GPS.IsServiceDestination && !host.Paused,
                            $"Companion exits; thirst reset; navigation/control restored: completion={completionObserved} minimumThirst={minimumThirst:F6} currentThirst={host.DriverNeeds.State.Thirst:F6} serviceGps={host.GPS.IsServiceDestination} paused={host.Paused} feedback={host.Companions.Feedback}");
                        if (!parkingWasOn) { Press(keyboard, Key.P); yield return null; Release(keyboard); yield return null; }
                    }
                }

                if(companionOnly || skipIntercity) yield break;
                bool startedTown01 = world.CurrentRegion.Contains("town01");
                var intercity = new DriveResult();
                if (startedTown01)
                    yield return Drive(host, body, keyboard, new Vector3(3680, .15f, 0), 260, intercity);
                check(startedTown01 && intercity.Reached && world.CurrentRegion.Contains("town02") &&
                    world.IsPositionAvailable(body.position),
                    "Continuous NWH input drive Town 01 to streamed Town 02: " + intercity.Detail);
                Debug.Log("TAXI REGIONAL INTERCITY RIDE: UNVERIFIED; physical traversal is not a passenger ride.");
                capture?.Invoke("Regional_DriveEnd");
            }
            finally
            {
                if (keyboard != null) { Release(keyboard); InputSystem.RemoveDevice(keyboard); }
                var debug = host.hud.GetComponent<TruckTaxiDebugPanel>();
                if (debug?.IsOpen == true) debug.Toggle();
                if (host.hud.FullMap?.IsOpen == true) host.hud.FullMap.Close();
                if (effectOwned) host.DriverNeeds.State.Effects.Clear();
                if (changer != null && priorCamera >= 0)
                    for (int i = 0; i < changer.cameras.Count && changer.currentCameraIndex != priorCamera; i++) changer.NextCamera();
                if (companionOwned && host.Companions.IsBusy) host.Companions.Cancel();
                host.Session.SetRideRequestsEnabled(requests);
                if (hadRequestPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, requestPreference);
                else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
                PlayerPrefs.Save();
                if (startedShift) host.Session.EndShift();
                host.SetPaused(paused);
                Screen.SetResolution(width, height, false);
                InputSystem.settings = settings;
                UnityEngine.Object.Destroy(temporary);
            }
        }

        private static float Mean(List<float> values)
        { double sum = 0; foreach (float value in values) sum += value; return values.Count == 0 ? 0 : (float)(sum / values.Count); }
        private static int NeedLabels(Transform root, string label)
        { int n = 0; foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (text.gameObject.activeInHierarchy && text.text.StartsWith(label, StringComparison.Ordinal)) n++; return n; }
        private static void Press(Keyboard keyboard, Key key) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        private static void Release(Keyboard keyboard) => InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        private static TruckTaxiStopObjectivePoint FindStop(string id)
        { foreach (var stop in UnityEngine.Object.FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None))
            if (stop.stableId == id) return stop; return null; }
        private static Camera SelectCamera(CameraChanger changer, string name)
        {
            for (int i = 0; i < changer.cameras.Count; i++)
            {
                var current = changer.cameras[changer.currentCameraIndex];
                if (current != null && current.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return current.GetComponent<Camera>();
                changer.NextCamera();
            }
            return null;
        }
        // Render the selected NWH gameplay camera, including its own URP post-processing.
        private static float Saturation(Camera camera)
        {
            var target = new RenderTexture(640, 360, 24);
            var pixels = new Texture2D(640, 360, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); pixels.Apply();
                double sum = 0; int count = 0;
                for (int y = 20; y < 340; y += 5) for (int x = 20; x < 620; x += 5)
                {
                    Color c = pixels.GetPixel(x, y);
                    sum += Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    count++;
                }
                return count > 0 ? (float)(sum / count) : 0;
            }
            finally
            {
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(pixels);
            }
        }
        // No Rigidbody position/velocity writes: only keyboard events enter the existing truck controls.
        private static IEnumerator Drive(TruckTaxiBootstrap host, Rigidbody body, Keyboard keyboard,
            Vector3 destination, float seconds, DriveResult result)
        {
            var route = host.RouteDistances.Measure(body.position, destination);
            if (route?.Navigable != true || route.Points.Count < 2)
            { result.Detail = "no navigable LWS route"; yield break; }
            float startTime = Time.realtimeSinceStartup, deadline = startTime + seconds;
            Vector3 start = body.position; int waypoint = 1; bool triedEngine = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                Vector3 remaining = destination - body.position; remaining.y = 0;
                float speed = body.linearVelocity.magnitude;
                if (remaining.magnitude < 7 && speed < .4f) { result.Reached = true; break; }
                while (waypoint < route.Points.Count - 1 &&
                    Vector2.Distance(new Vector2(body.position.x, body.position.z),
                        new Vector2(route.Points[waypoint].x, route.Points[waypoint].z)) < 18) waypoint++;
                Vector3 aim = route.Points[waypoint] - body.position; aim.y = 0;
                float angle = Vector3.SignedAngle(body.rotation * Vector3.forward, aim, Vector3.up);
                // Continue creeping after braking; a fixed 10m stop radius can never reach a 7m target.
                float desiredSpeed = Mathf.Min(Mathf.Abs(angle)>45 ? 7 : 25,
                    Mathf.Sqrt(Mathf.Max(0,remaining.magnitude-5)*2));
                bool brake = remaining.magnitude < 6 || speed > desiredSpeed;
                Key steering = angle > 4 ? Key.D : angle < -4 ? Key.A : Key.None;
                Key pedal = brake ? Key.S : Key.W;
                InputSystem.QueueStateEvent(keyboard, steering == Key.None ?
                    new KeyboardState(pedal) : new KeyboardState(pedal, steering));
                yield return null;
                if (!triedEngine && Time.realtimeSinceStartup - startTime > 8 &&
                    Vector3.Distance(start, body.position) < 2)
                {
                    Press(keyboard, Key.E); yield return null; Release(keyboard); yield return null;
                    triedEngine = true;
                }
            }
            Press(keyboard, Key.S);
            float stopDeadline = Time.realtimeSinceStartup + 5;
            while (body.linearVelocity.magnitude > .4f && Time.realtimeSinceStartup < stopDeadline) yield return null;
            Release(keyboard);
            result.Detail = $"reached={result.Reached}, moved={Vector3.Distance(start, body.position):F0}m, remaining={Vector3.Distance(body.position, destination):F0}m, speed={body.linearVelocity.magnitude:F1}m/s, source={route.Source}";
        }
    }
}
#endif
