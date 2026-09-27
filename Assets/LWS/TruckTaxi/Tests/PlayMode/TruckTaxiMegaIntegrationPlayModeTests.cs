using System;
using System.Collections;
using System.Linq;
using NWH.Common.Cameras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiMegaAutomatedIntegration")]
    public sealed class TruckTaxiMegaIntegrationPlayModeTests
    {
        private static IEnumerator LoadCity(Action<TruckTaxiBootstrap> ready)
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline = Time.realtimeSinceStartup + 60f;
            while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.Ready) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(TruckTaxiBootstrap.Instance != null && TruckTaxiBootstrap.Instance.Ready,
                "DemoCity bootstrap did not become ready within 60 seconds");
            ready(TruckTaxiBootstrap.Instance);
        }

        [UnityTest]
        public IEnumerator WaitingPassengerContactUsesPhysicalTruckAndViableThreshold()
        {
            var isolation = SceneManager.CreateScene("Taxi mega contact isolation");
            Assert.IsTrue(SceneManager.SetActiveScene(isolation));
            var prior = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).Where(scene => scene != isolation && scene.isLoaded).ToArray();
            foreach (var scene in prior) yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
            var truck = new GameObject("Mega contact truck");
            var person = new GameObject("Mega waiting passenger");
            try
            {
                truck.transform.position = Vector3.left * 3f;
                var body = truck.AddComponent<Rigidbody>();
                body.useGravity = false;
                truck.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
                var actor = person.AddComponent<TruckTaxiPassengerActor>();
                actor.Bind(null);
                actor.ArmWaitingTruckHit(body);
                var sensor = person.GetComponent<CapsuleCollider>();
                Assert.IsTrue(sensor.enabled && sensor.isTrigger);
                Assert.Greater(sensor.height, 1f);
                Assert.That(actor.MinimumWaitingImpactMetersPerSecond, Is.InRange(1f, 2f));
                int hits = 0;
                actor.WaitingTruckHit += (_, velocity) =>
                {
                    if (velocity.magnitude >= actor.MinimumWaitingImpactMetersPerSecond) hits++;
                };
                body.linearVelocity = Vector3.right * .5f;
                yield return new WaitForSeconds(1f);
                Assert.AreEqual(0, hits, "Sub-threshold contact must not count");
                body.linearVelocity = Vector3.right * 4f;
                float deadline = Time.realtimeSinceStartup + 2f;
                while (hits == 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
                Assert.Greater(hits, 0, "A moving physical truck collider must trigger the waiting-hit path");
            }
            finally
            {
                UnityEngine.Object.Destroy(person);
                UnityEngine.Object.Destroy(truck);
            }
        }

        [UnityTest]
        public IEnumerator CityStartupDriverMapBusAndPausedCountdown()
        {
            TruckTaxiBootstrap host = null;
            yield return LoadCity(value => host = value);
            var session = host.Session;
            var originalArea = session.CaptureWorkArea();
            bool originalPaused = host.Paused;
            bool originalRequests = session.RideRequestsEnabled;
            bool originalKinematic = host.Player.GetComponent<Rigidbody>().isKinematic;
            PassengerProfile forcedProfile = null;
            try
            {
                Assert.IsNotNull(host.WorldCoordinator);
                Assert.IsTrue(host.CalendarWeather.IsInitialized, host.CalendarWeather.LastDiagnostic);
                var now = host.CalendarWeather.CurrentDateTime;
                Assert.Greater(now.Year, 2000);
                Assert.AreEqual(now.DayOfWeek, host.CalendarWeather.DayOfWeek);
                var forecast = host.CalendarWeather.Forecast;
                Assert.IsNotEmpty(forecast);
                Assert.LessOrEqual(forecast[0].Start, now);
                Assert.GreaterOrEqual(forecast[forecast.Count - 1].End, now.Date.AddDays(7));
                for (int i = 0; i < forecast.Count; i++)
                {
                    Assert.Greater(forecast[i].End, forecast[i].Start);
                    if (i > 0) Assert.AreEqual(forecast[i - 1].End, forecast[i].Start);
                }

                var driver = host.Player.GetComponent<TruckTaxiDriverPresentation>();
                Assert.IsTrue(driver != null && driver.Ready);
                var visible = host.Player.transform.Find("Cab/Truck Taxi visible driver (Wobble)") ??
                              host.Player.transform.Find("Truck Taxi visible driver (Wobble)");
                Assert.IsNotNull(visible);
                var parts = visible.GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(parts);
                Assert.IsTrue(parts.All(r => r.gameObject.layer == 29));
                var inside = host.Player.GetComponentsInChildren<CameraInsideVehicle>(true)
                    .Where(c => c.isInsideVehicle)
                    .Select(c => c.GetComponent<Camera>() ?? c.GetComponentInChildren<Camera>(true))
                    .Where(c => c != null).ToArray();
                Assert.IsNotEmpty(inside);
                Assert.IsTrue(inside.All(c => (c.cullingMask & (1 << 29)) == 0));
                Assert.IsTrue(host.Player.GetComponentsInChildren<Camera>(true).Except(inside)
                    .Any(c => (c.cullingMask & (1 << 29)) != 0),
                    "An outside camera must render the complete driver layer");

                if (session.State == TruckTaxiState.Inactive) host.StartShift();
                session.SetRideRequestsEnabled(false);
                Assert.IsTrue(host.hud.FullMap.Open());
                var panel = host.hud.FullMap.WorkArea;
                Assert.IsNotNull(panel);
                panel.Toggle();
                Assert.IsTrue(panel.IsOpen);
                var area = new TruckTaxiRideWorkArea { mode = TruckTaxiWorkAreaMode.Custom,
                    center = host.Player.transform.position, radius = 250f, label = "MEGA TEST" };
                Assert.IsTrue(session.SetWorkArea(area));
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.IsNotNull(host.GPS.PreviewCamera, "Compass map camera must be initialized");
                Assert.IsNotNull(host.GPS.MapScreenRect);
                var graphic = host.GPS.MapScreenRect.GetComponentsInChildren<TruckTaxiWorkAreaGraphic>(true)
                    .FirstOrDefault(g => g.name == "Work Area boundary");
                Assert.IsNotNull(graphic);
                Assert.IsTrue(graphic.gameObject.activeInHierarchy);
                Assert.Greater(graphic.rectTransform.rect.width, 0f);
                var mesh = graphic.canvasRenderer.GetMesh();
                Assert.IsNotNull(mesh, "Restricted work area must produce a mesh");
                Assert.Greater(mesh.vertexCount, 0, "Restricted work area must draw positive geometry");
                Assert.IsTrue(session.WorkArea.Contains(area.center));
                Assert.IsFalse(session.WorkArea.Contains(area.center + Vector3.right * 500f));
                panel.Close();
                host.hud.FullMap.Close();

                var buses = UnityEngine.Object.FindObjectsByType<TruckTaxiBusService>(FindObjectsSortMode.None);
                Assert.IsNotEmpty(buses);
                Assert.Greater(buses.Sum(b => b.RouteCount), 0);
                float busDeadline = Time.realtimeSinceStartup + 15f;
                GameObject liveBus = null;
                while (liveBus == null && Time.realtimeSinceStartup < busDeadline)
                {
                    foreach (var bus in buses)
                        for (int i = 0; i < bus.RouteCount; i++) liveBus = liveBus ?? bus.GetLiveBus(i);
                    yield return null;
                }
                Assert.IsNotNull(liveBus, "An authored city bus must materialize within 15 seconds");
                var busCollider = liveBus.GetComponent<BoxCollider>();
                Assert.IsNotNull(busCollider);
                Assert.IsFalse(busCollider.isTrigger);
                Assert.Greater(busCollider.bounds.size.x, 1f);
                Assert.Greater(busCollider.bounds.size.y, 1f);
                Assert.Greater(Mathf.Max(busCollider.bounds.size.x,busCollider.bounds.size.z), 5f,
                    "Coach length is independent of its world heading");

                forcedProfile = UnityEngine.Object.Instantiate(
                    host.configuration.passengerDatabase.passengers.First(p => p != null));
                forcedProfile.possibleRequests = Array.Empty<PassengerRequestDefinition>();
                forcedProfile.basePatience = 10000f;
                session.SetRideRequestsEnabled(true);
                Assert.IsTrue(session.OfferRide(forcedProfile));
                Assert.AreSame(forcedProfile, session.Passenger);
                Assert.IsFalse(TruckTaxiRivalPopulation.IsIdentityEligible(session.Passenger,
                    session.Passenger, null), "A waiting player's identity cannot become a rival rider");
                float before = session.OfferRemaining;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.Less(session.OfferRemaining, before, "Live offer countdown advances");
                host.SetPaused(true);
                float paused = session.OfferRemaining;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.Less(session.OfferRemaining, paused,
                    "Offer modals retain the existing real-time expiry while driving is suppressed");
                host.SetPaused(false);
                Assert.IsTrue(session.AcceptRide());
                host.TeleportNear(session.Pickup);
                host.Player.GetComponent<Rigidbody>().isKinematic = true;
                float boardingDeadline = Time.realtimeSinceStartup + 12f;
                while (!session.HasPassenger && Time.realtimeSinceStartup < boardingDeadline) yield return null;
                Assert.IsTrue(session.HasPassenger, "Teleport-assisted pickup may stream for several frames");
                var goal = host.configuration.requests.First(r => r.requestType == TaxiRequestType.FastDelivery);
                Assert.IsTrue(session.GenerateRequest(goal));
                var progress = session.Requests.First(r => r.Definition == goal);
                float goalBefore = progress.Remaining;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.Less(progress.Remaining, goalBefore, "Active ride goal countdown advances");
                host.SetPaused(true);
                float goalPaused = progress.Remaining;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(progress.Remaining, Is.EqualTo(goalPaused).Within(.02f),
                    "Pause freezes the active ride goal countdown");
            }
            finally
            {
                if (host.hud.FullMap.IsOpen) host.hud.FullMap.Close();
                host.SetPaused(false);
                if (session.State == TruckTaxiState.RideOffered) session.DeclineRide();
                session.RestoreWorkArea(originalArea);
                session.SetRideRequestsEnabled(originalRequests);
                host.Player.GetComponent<Rigidbody>().isKinematic = originalKinematic;
                if (forcedProfile != null) UnityEngine.Object.Destroy(forcedProfile);
                host.SetPaused(originalPaused);
            }
        }

        [UnityTest]
        public IEnumerator AuthoredVenuesRivalsAndFiveExtremesStartAndCleanUp()
        {
            TruckTaxiBootstrap host = null;
            yield return LoadCity(value => host = value);
            var venues = host.Venues;
            var extremes = host.Extremes;
            Assert.IsNotNull(venues);
            Assert.IsNotNull(extremes);
            Assert.GreaterOrEqual(venues.Venues.Count, 3);
            Assert.IsNotEmpty(venues.GetNextSevenDays());
            Assert.GreaterOrEqual(host.WorldCoordinator.HazardZones.Length, 5);
            var authoredEvent = venues.EventsSnapshots[0];
            try
            {
                Assert.IsTrue(venues.ForceStart(authoredEvent.EventId));
                var active = venues.EventsSnapshots.First(e => e.EventId == authoredEvent.EventId &&
                    e.OccurrenceDate == host.CalendarWeather.CurrentDateTime.Date);
                Assert.AreEqual(TruckTaxiVenueEventStatus.InProgress, active.Status);
                Assert.IsTrue(venues.ForceEnd(authoredEvent.EventId));
                var departure = venues.EventsSnapshots.First(e => e.EventId == authoredEvent.EventId &&
                    e.OccurrenceDate == host.CalendarWeather.CurrentDateTime.Date);
                Assert.AreEqual(TruckTaxiVenueEventStatus.DepartureSurge, departure.Status);
                Assert.Greater(venues.GetWeightForPickup(departure.Position), 1f);
                Assert.Greater(venues.GetFareQuote(departure.Position,
                    departure.Position + Vector3.right * 1000f).EventMultiplier, 1f);

                Assert.IsNotNull(host.Rivals);
                Assert.AreEqual(12, host.Rivals.LogicalCount);
                yield return new WaitForSecondsRealtime(1f);
                Assert.LessOrEqual(host.Rivals.LiveCount, 3);
                foreach (TruckTaxiExtremeKind kind in Enum.GetValues(typeof(TruckTaxiExtremeKind)))
                {
                    var zone = host.WorldCoordinator.HazardZones.FirstOrDefault(z => z != null && z.kind == kind);
                    Assert.IsNotNull(zone, "Missing authored hazard zone for " + kind);
                    Assert.IsTrue(zone.IsValid(out string reason), reason);
                    Assert.IsTrue(extremes.StartEvent(kind, zone.zoneId), "Start " + kind);
                    yield return null;
                    Assert.IsTrue(extremes.ActiveSnapshots.Any(s => s.Kind == kind));
                    Assert.LessOrEqual(extremes.LiveVisualActorCount, 3);
                    Assert.LessOrEqual(extremes.LivePhysicsActorCount, 2);
                    extremes.Clear(kind, zone.zoneId);
                    yield return null;
                    Assert.IsFalse(extremes.ActiveSnapshots.Any(s => s.Kind == kind));
                    Assert.AreEqual(0, extremes.LivePhysicsActorCount);
                }
            }
            finally
            {
                extremes.Clear();
                venues.ForceEnd(authoredEvent.EventId);
            }
            yield return null;
            Assert.AreEqual(0, extremes.ActiveSnapshots.Count);
            Assert.AreEqual(0, extremes.LiveVisualActorCount);
            Assert.IsNull(extremes.ActiveSevereProfile);
        }

        [UnityTest]
        public IEnumerator ScenicRendererAndPrivateStopEjectionRestoreState()
        {
            TruckTaxiBootstrap host = null;
            yield return LoadCity(value => host = value);
            var motion = host.GetComponent<TruckTaxiPrivateEventVehicleMotion>();
            Assert.IsTrue(motion != null && motion.Ready);
            var body = host.Player.GetComponent<Rigidbody>();
            var position = body.position;
            var rotation = body.rotation;
            var renderers = host.Player.GetComponentsInChildren<Renderer>(true);
            var original = renderers.Select(r => r.enabled).ToArray();
            Assert.IsTrue(motion.BeginScenic());
            try
            {
                motion.Step(.5f);
                Assert.IsTrue(motion.IsActive);
                Assert.IsTrue(renderers.Where((r, i) => original[i]).Any(r => !r.enabled));
            }
            finally { motion.StopMotion(); }
            Assert.IsFalse(motion.IsActive);
            for (int i = 0; i < renderers.Length; i++) Assert.AreEqual(original[i], renderers[i].enabled);
            Assert.AreEqual(position, body.position);
            Assert.AreEqual(rotation, body.rotation);

            var companions = host.Companions;
            Assert.IsTrue(companions != null && companions.Ready);
            bool wasKinematic = body.isKinematic;
            bool requests = host.Session.RideRequestsEnabled;
            Keyboard keyboard = null;
            InputSettings previousInput = null;
            try
            {
                host.StartShift();
                host.Session.SetRideRequestsEnabled(false);
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                Assert.IsTrue(companions.DebugSpawnNearPlayer());
                yield return new WaitForSecondsRealtime(.3f);
                Assert.IsTrue(companions.CanInteract);
                previousInput = InputSystem.settings;
                InputSystem.settings = UnityEngine.Object.Instantiate(previousInput);
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                keyboard = InputSystem.AddDevice<Keyboard>("Mega private-stop test keyboard");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.H));
                float deadline = Time.realtimeSinceStartup + 4f;
                while (!companions.HasOnboardCompanion && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(companions.HasOnboardCompanion);
                var stop = companions.ActivePrivateStop;
                Assert.IsNotNull(stop);
                deadline = Time.realtimeSinceStartup + 5f;
                while (!host.GPS.RouteReady && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(stop.stableId, host.GPS.TargetId);
                var circle = host.GetComponentsInChildren<LineRenderer>(true)
                    .FirstOrDefault(l => l.name == "Private stop parking radius");
                Assert.IsNotNull(circle);
                Assert.IsTrue(circle.gameObject.activeInHierarchy);
                Assert.That(Vector3.ProjectOnPlane(circle.GetPosition(0) - stop.Position, Vector3.up).magnitude,
                    Is.EqualTo(stop.radius).Within(.3f));
                var actor = host.Player.GetComponentInChildren<TruckTaxiPassengerActor>();
                Assert.IsNotNull(actor);
                Assert.IsTrue(companions.Eject());
                yield return null;
                Assert.IsFalse(companions.HasOnboardCompanion);
                Assert.IsFalse(actor.transform.IsChildOf(host.Player.transform));
                Assert.IsFalse(actor.GetComponent<Rigidbody>().isKinematic);
                Assert.IsTrue(string.IsNullOrEmpty(host.GPS.TargetId));
                Assert.IsFalse(circle.gameObject.activeInHierarchy);
                Assert.IsFalse(host.Session.OffersSuppressed);
            }
            finally
            {
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                if (previousInput != null)
                {
                    var temporary = InputSystem.settings;
                    InputSystem.settings = previousInput;
                    UnityEngine.Object.Destroy(temporary);
                }
                companions.Cancel();
                host.Session.SetRideRequestsEnabled(requests);
                body.isKinematic = wasKinematic;
            }
        }
    }
}
