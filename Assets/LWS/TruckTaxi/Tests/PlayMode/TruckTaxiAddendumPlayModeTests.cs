using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiAddendumIntegration")]
    public sealed class TruckTaxiAddendumPlayModeTests
    {
        private const string RadioOnKey = "LWS.TruckTaxi.Radio.On";
        private const string RadioVolumeKey = "LWS.TruckTaxi.Radio.Volume";
        private const string RadioStationKey = "LWS.TruckTaxi.Radio.Station";
        private readonly object otherOfferOwner = new object();
        private TruckTaxiBootstrap host;
        private PassengerProfile testPassenger;
        private bool hadRadioOn, hadVolume, hadStation, hadRequests;
        private int savedRadioOn, savedRequests;
        private float savedVolume, savedTimeScale;
        private string savedStation;
        private bool savedListenerPause, savedCursorVisible;
        private CursorLockMode savedCursorLock;
        private UnityEngine.Random.State savedRandom;

        [UnitySetUp]
        public IEnumerator LoadFreshDemo()
        {
            savedTimeScale = Time.timeScale;
            savedListenerPause = AudioListener.pause;
            savedCursorLock = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            savedRandom = UnityEngine.Random.state;
            hadRadioOn = PlayerPrefs.HasKey(RadioOnKey);
            savedRadioOn = PlayerPrefs.GetInt(RadioOnKey);
            hadVolume = PlayerPrefs.HasKey(RadioVolumeKey);
            savedVolume = PlayerPrefs.GetFloat(RadioVolumeKey);
            hadStation = PlayerPrefs.HasKey(RadioStationKey);
            savedStation = PlayerPrefs.GetString(RadioStationKey);
            hadRequests = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            savedRequests = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);

            yield return UnloadWorld();
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, 0);
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("TruckTaxi_DemoCity"));
            yield return WaitFor(() => TruckTaxiBootstrap.Instance != null && TruckTaxiBootstrap.Instance.Ready,
                60, "TruckTaxi_DemoCity bootstrap did not become ready.");
            host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host.Player);
            Assert.IsNotNull(host.Session);
            Assert.IsNotNull(host.CalendarWeather);
            Assert.IsTrue(host.CalendarWeather.IsInitialized, host.CalendarWeather.LastDiagnostic);
            host.SetPaused(true);
            host.Environment.SetTimeScale(0);
            host.Session.SetRideRequestsEnabled(false);
        }

        [UnityTearDown]
        public IEnumerator CleanDemoAndRestorePreferences()
        {
            try
            {
                if (host != null)
                {
                    // Let the real recovery coroutine release its lease before destroying its host.
                    float deadline = Time.realtimeSinceStartup + 65;
                    while (host.FastTravel != null && host.FastTravel.IsTravelling &&
                           Time.realtimeSinceStartup < deadline) yield return null;
                    host.Races?.LeaveRace();
                    host.Extremes?.Clear();
                    host.Session?.ReleaseOfferSuppression(otherOfferOwner);
                    host.SetPaused(false);
                }
                yield return UnloadWorld();
            }
            finally
            {
                if (testPassenger != null) Object.Destroy(testPassenger);
                testPassenger = null;
                RestoreInt(RadioOnKey, hadRadioOn, savedRadioOn);
                if (hadVolume) PlayerPrefs.SetFloat(RadioVolumeKey, savedVolume);
                else PlayerPrefs.DeleteKey(RadioVolumeKey);
                if (hadStation) PlayerPrefs.SetString(RadioStationKey, savedStation);
                else PlayerPrefs.DeleteKey(RadioStationKey);
                RestoreInt(TruckTaxiSession.RideRequestsPreferenceKey, hadRequests, savedRequests);
                // Radio.OnDestroy persists selection; restore only after scene destruction.
                PlayerPrefs.Save();
                Time.timeScale = savedTimeScale;
                AudioListener.pause = savedListenerPause;
                Cursor.lockState = savedCursorLock;
                Cursor.visible = savedCursorVisible;
                UnityEngine.Random.state = savedRandom;
                host = null;
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator RadioCatalogHasSixStationsAndPowerPreservesSelection()
        {
            var catalog = Resources.Load<TruckTaxiRadioCatalog>("TruckTaxi/TruckTaxiRadioCatalog");
            Assert.IsNotNull(catalog, "Demo radio catalog must be authored in Resources.");
            Assert.AreEqual(6, catalog.stations.Length);
            Assert.IsTrue(catalog.stations.All(s => s != null && !string.IsNullOrWhiteSpace(s.stableId)));
            Assert.AreEqual(6, catalog.stations.Select(s => s.stableId).Distinct().Count());
            var radio = host.Radio;
            Assert.IsNotNull(radio, "Bootstrap must initialize the radio.");
            Assert.IsNotNull(radio.Station);
            if (radio.IsOn) radio.Toggle();
            Assert.AreEqual("RADIO OFF", radio.Display);
            var visited = new HashSet<string>();
            string startingStation = radio.Station.stableId;
            for (int i = 0; i < 6; i++)
            {
                visited.Add(radio.Station.stableId);
                radio.ChangeStation(1);
                Assert.IsFalse(radio.IsOn, "Changing station while off must not switch power on.");
                Assert.AreEqual("RADIO OFF", radio.Display);
            }
            CollectionAssert.AreEquivalent(catalog.stations.Select(s => s.stableId), visited);
            Assert.AreEqual(startingStation, radio.Station.stableId, "Station selection must wrap.");
            radio.ChangeStation(-1);
            string selected = radio.Station.stableId;
            Assert.AreNotEqual(startingStation, selected);
            radio.Toggle();
            Assert.IsTrue(radio.IsOn);
            Assert.AreEqual(selected, radio.Station.stableId);
            StringAssert.Contains(radio.Station.displayName, radio.Display);
            yield return null;
            radio.Toggle();
            Assert.IsFalse(radio.IsOn);
            radio.Toggle();
            Assert.AreEqual(selected, radio.Station.stableId);
            Assert.IsTrue(radio.IsOn);
        }

        [UnityTest, Timeout(150000)]
        public IEnumerator CriticalAndSecondaryAlertsHavePositiveSeparateLanes()
        {
            var alerts = host.hud.Alerts;
            Assert.IsNotNull(alerts);
            yield return WaitFor(() => alerts.CurrentText.Length == 0 && alerts.PendingCount == 0,
                60, "Startup alerts did not drain before the layout test.");
            alerts.Publish("addendum-layout-critical", "ADDENDUM CRITICAL ALERT", TruckTaxiAlertPriority.Critical, 20);
            alerts.Publish("addendum-layout-secondary", "ADDENDUM SECONDARY ALERT", TruckTaxiAlertPriority.High, 20);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual("ADDENDUM CRITICAL ALERT", alerts.CurrentText);
            var layout = host.hud.Root.Find("Top HUD layout");
            Assert.IsNotNull(layout);
            var critical = RequireRect(layout, "Critical alert lane");
            var secondary = RequireRect(layout, "Secondary alert lane");
            var permanent = RequireRect(layout, "Time needs and commands");
            Rect criticalBounds = VisibleBounds(critical);
            Rect secondaryBounds = VisibleBounds(secondary);
            Rect permanentBounds = VisibleBounds(permanent);
            Assert.IsFalse(criticalBounds.Overlaps(secondaryBounds), "Critical and secondary alerts overlap.");
            Assert.Less(secondaryBounds.yMax, criticalBounds.yMin, "Alert lanes need a positive vertical gap.");
            Assert.Less(criticalBounds.yMax, permanentBounds.yMin, "Alerts must remain below permanent controls.");
            Assert.IsFalse(VisibleBounds(alerts.NeedsSlot).Overlaps(VisibleBounds(alerts.CommandsSlot)),
                "Needs and command controls overlap.");
            Assert.IsTrue(critical.GetComponentsInChildren<Graphic>().Any(g => g.enabled));
            Assert.IsTrue(secondary.GetComponentsInChildren<Graphic>().Any(g => g.enabled));
            var raceButton = RequireRect(host.hud.Root, "RACE REGISTRATION");
            foreach (string label in new[] { "RESET UPRIGHT", "PAUSE", "DEBUG", "GPS ON / OFF", "GPS SETTINGS", "SERVICES / TOW", "THROW FILLED CONTAINER" })
                Assert.IsFalse(PositiveBounds(raceButton).Overlaps(PositiveBounds(RequireRect(host.hud.Root, label))),
                    "Race registration overlaps " + label);
            var raceTiming = RequireRect(host.hud.Root, "Race timing");
            var radioCard = RequireRect(host.hud.Root, "Radio station card");
            Assert.IsFalse(PositiveBounds(raceTiming).Overlaps(PositiveBounds(radioCard)));
            Assert.IsFalse(PositiveBounds(raceTiming).Overlaps(secondaryBounds));
            Assert.IsFalse(PositiveBounds(radioCard).Overlaps(VisibleBounds(RequireRect(host.hud.Root, "Telemetry background"))));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator ForcedExtremesMaterializeNearLoadedPlayerAndClear()
        {
            var director = host.Extremes;
            var regional = host.GetComponent<TruckTaxiRegionalWorld>();
            Assert.IsNotNull(director);
            Assert.IsNotNull(regional);
            Assert.IsTrue(regional.IsPositionAvailable(host.Player.transform.position));
            director.Clear();
            int warnings = 0;
            Action<TruckTaxiExtremeWarning> onWarning = _ => warnings++;
            director.WarningRaised += onWarning;
            try
            {
                foreach (TruckTaxiExtremeKind kind in Enum.GetValues(typeof(TruckTaxiExtremeKind)))
                {
                    int before = warnings;
                    Assert.IsTrue(ForceHere(director, kind), "Force failed for " + kind + ": " + AttemptSummary(director, kind));
                    yield return WaitFor(() => director.ActiveSnapshots.Any(s => s.Kind == kind && s.LoadedNearby) &&
                        director.LiveVisualActorCount > 0, 3, kind + " never materialized near the loaded player.");
                    var snapshot = director.ActiveSnapshots.Single(s => s.Kind == kind);
                    Assert.AreEqual(before + 1, warnings, "Each start must publish one warning.");
                    Assert.IsTrue(regional.IsPositionAvailable(snapshot.Position));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.RegionId));
                    Assert.LessOrEqual(Vector3.Distance(host.Player.transform.position, snapshot.Position),
                        kind == TruckTaxiExtremeKind.Hurricane ? 1501 : 1001);
                    Assert.Greater(snapshot.RadiusMeters, 0);
                    var attempt = director.Attempts.Last(a => a.Started && a.ZoneId == snapshot.ZoneId);
                    Assert.IsTrue(attempt.VisualMaterialized);
                    Assert.IsTrue(attempt.AlertSent);
                    var visual = FindEventVisual(snapshot);
                    Assert.IsNotNull(visual, "No visual root was instantiated for " + kind);
                    var materialized = visual.GetComponentsInChildren<Renderer>(true);
                    Assert.IsTrue(materialized.Any(r => r.enabled && r.gameObject.activeInHierarchy),
                        "A started-event record alone is not rendered presentation.");
                    yield return WaitFor(() => FindHazardMarker(snapshot.ZoneId)?.state == TruckTaxiMapMarkerState.Active,
                        3, "Coordinator did not publish an active hazard marker for " + kind);
                    if (kind == TruckTaxiExtremeKind.Hurricane)
                    {
                        Assert.IsTrue(director.ActiveSevereProfile.HasValue);
                        Assert.AreEqual(28, director.ActiveSevereProfile.Value.WindMetersPerSecond);
                        Assert.AreEqual(TruckTaxiWeatherCondition.Thunderstorm, host.CalendarWeather.CurrentCondition);
                    }
                    director.Clear(kind, snapshot.ZoneId);
                    yield return null;
                    Assert.IsEmpty(director.ActiveSnapshots);
                    Assert.IsEmpty(director.Warnings);
                    Assert.AreEqual(0, director.LiveVisualActorCount);
                    Assert.AreEqual(0, director.LivePhysicsActorCount);
                    Assert.IsTrue(visual == null, "Clear must destroy the event visual root.");
                    Assert.IsTrue(materialized.All(r => r == null), "Clear must destroy materialized event renderers.");
                    Assert.IsNull(director.ActiveSevereProfile);
                    Assert.AreEqual("CLEARED", director.Attempts.Last(a => a.Started && a.ZoneId == snapshot.ZoneId).EndReason);
                    yield return WaitFor(() => FindHazardMarker(snapshot.ZoneId)?.state == TruckTaxiMapMarkerState.Hidden,
                        3, "Cleared hazard marker remained active for " + kind);
                }
            }
            finally { director.WarningRaised -= onWarning; director.Clear(); }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator PracticeRaceReleasesOnlyItsOwnOfferSuppressionLease()
        {
            Assert.IsNotNull(host.Races, "Bootstrap must initialize the race sidecar.");
            host.StartShift();
            host.SetPaused(true);
            yield return TravelTo(TruckTaxiSpeedwayLayout.Stops[1].Position, "Addendum practice setup");
            Assert.AreEqual(TruckTaxiState.Available, host.Session.State);
            Assert.IsFalse(host.Session.OffersSuppressed);
            host.Session.SetRideRequestsEnabled(true);
            long earnings = host.Session.ShiftEarnings;
            Assert.IsTrue(host.Races.BeginRace(TruckTaxiRaceMode.Practice), host.Races.Feedback);
            Assert.IsTrue(host.Races.IsActive);
            Assert.AreEqual(TruckTaxiRaceMode.Practice, host.Races.Mode);
            Assert.AreEqual(0, host.Races.OpponentCount);
            Assert.IsTrue(host.Session.OffersSuppressed);
            Assert.IsTrue(host.Session.RideRequestsEnabled);
            Assert.IsFalse(string.IsNullOrEmpty(host.Races.BlockTravelReason));
            Assert.IsFalse(host.Races.BeginRace(TruckTaxiRaceMode.Practice), "An active race must not acquire a second lease.");
            yield return null;
            Assert.IsTrue(host.Races.IsActive);
            host.Session.AcquireOfferSuppression(otherOfferOwner);
            host.Races.LeaveRace();
            Assert.IsFalse(host.Races.IsActive);
            Assert.AreEqual(0, host.Races.OpponentCount);
            Assert.IsTrue(host.Session.OffersSuppressed, "Race exit must preserve another owner's lease.");
            Assert.IsTrue(host.Session.RideRequestsEnabled);
            Assert.IsTrue(string.IsNullOrEmpty(host.Races.BlockTravelReason));
            host.Session.ReleaseOfferSuppression(otherOfferOwner);
            Assert.IsFalse(host.Session.OffersSuppressed, "Race lease leaked after exit.");
            Assert.AreEqual(earnings, host.Session.ShiftEarnings, "Practice exit must not award prize money.");

            host.Session.SetRideRequestsEnabled(false);
            Assert.IsTrue(host.Races.BeginRace(TruckTaxiRaceMode.Practice), host.Races.Feedback);
            host.Races.LeaveRace();
            Assert.IsFalse(host.Session.RideRequestsEnabled, "Race exit must preserve the user's disabled requests setting.");
            Assert.IsFalse(host.Session.OffersSuppressed);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator FastTravelRejectsAcceptedPickupWithoutChangingRideOrCredit()
        {
            host.StartShift();
            host.SetPaused(true);
            Assert.IsNotNull(host.FastTravel);
            Assert.AreEqual(TruckTaxiState.Available, host.Session.State);
            Assert.IsTrue(string.IsNullOrEmpty(host.FastTravel.BlockReason), host.FastTravel.BlockReason);
            testPassenger = Object.Instantiate(host.configuration.passengerDatabase.passengers.First(p => p != null));
            testPassenger.possibleRequests = Array.Empty<PassengerRequestDefinition>();
            host.Session.SetRideRequestsEnabled(true);
            Assert.IsTrue(host.Session.OfferRide(testPassenger));
            Assert.IsTrue(host.Session.AcceptRide());
            Assert.AreEqual(TruckTaxiState.DrivingToPickup, host.Session.State);
            // Acceptance resumes driving. Test the paused map caller, not normal suspension settling.
            host.SetPaused(true);
            int completed = host.Session.CompletedRides;
            int failed = host.Session.FailedRides;
            int stars = host.Session.TotalStarsEarned;
            long earnings = host.Session.ShiftEarnings;
            long wallet = host.Session.WalletBalanceCents;
            float distance = host.Session.DistanceDriven;
            var fare = host.Session.LastFare;
            var passenger = host.Session.Passenger;
            string rideId = host.Session.CurrentRideId;
            int travels = host.FastTravel.CompletedTravels;
            Vector3 origin = host.Player.transform.position;
            Quaternion rotation = host.Player.transform.rotation;
            string blocked = host.FastTravel.BlockReason;
            Assert.IsFalse(string.IsNullOrEmpty(blocked), "An accepted pickup must block fast travel.");
            Assert.IsFalse(host.FastTravel.TryTravel(TruckTaxiSpeedwayLayout.Stops[4].Position,
                "Addendum blocked pickup travel"), "Pre-pickup travel must reject, not cancel the ride.");
            Assert.AreEqual(blocked, host.FastTravel.Feedback);
            Assert.IsFalse(host.FastTravel.IsTravelling);
            yield return null;
            Assert.IsFalse(host.FastTravel.IsTravelling, "Rejected travel must not queue a recovery.");
            Assert.AreEqual(travels, host.FastTravel.CompletedTravels);
            Assert.Less(Vector3.Distance(origin, host.Player.transform.position), .01f,
                "Rejected travel must not relocate the truck.");
            Assert.Less(Quaternion.Angle(rotation, host.Player.transform.rotation), .01f);
            Assert.IsTrue(host.Paused, "Rejected travel must preserve the caller's pause state.");
            Assert.AreEqual(TruckTaxiState.DrivingToPickup, host.Session.State);
            Assert.AreSame(passenger, host.Session.Passenger);
            Assert.AreEqual(rideId, host.Session.CurrentRideId);
            Assert.AreEqual(failed, host.Session.FailedRides);
            Assert.AreEqual(completed, host.Session.CompletedRides);
            Assert.AreEqual(earnings, host.Session.ShiftEarnings);
            Assert.AreEqual(wallet, host.Session.WalletBalanceCents);
            Assert.AreEqual(stars, host.Session.TotalStarsEarned);
            Assert.AreSame(fare, host.Session.LastFare);
            Assert.That(host.Session.DistanceDriven, Is.EqualTo(distance).Within(.1f));
            Assert.IsFalse(host.Session.OffersSuppressed);
            Assert.IsTrue(host.Session.RideRequestsEnabled);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator AcceleratedCalendarNaturallyStartsEachKindWithinEligibleDayBound()
        {
            var director = host.Extremes;
            Assert.IsNotNull(director);
            var kaiju = host.GetComponent<TruckTaxiKaijuDirector>();
            if (kaiju != null) kaiju.enabled = false;
            host.Player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            host.StartShift();
            host.SetPaused(true);
            host.Session.SetRideRequestsEnabled(false);
            host.CalendarWeather.SetAutomaticWeather(false);
            host.CalendarWeather.SetTime(6);
            Assert.IsTrue(host.CalendarWeather.ForceForecast(TruckTaxiWeatherCondition.Clear),
                host.CalendarWeather.LastDiagnostic);
            yield return new WaitForSecondsRealtime(1.2f);
            // Observe the starting date before skipping forward, so each later step is one new day.
            DateTime startingDate = host.CalendarWeather.CurrentDateTime.Date;
            host.SetPaused(false);
            yield return WaitFor(() => director.Attempts.Any(a => a.Kind == TruckTaxiExtremeKind.Earthquake &&
                a.Time.Date == startingDate && (a.Started || a.Reason == "ROLL_FAILED")), 9,
                "Natural scheduler did not process the starting calendar date.");
            host.SetPaused(true);
            director.Clear();
            var kinds = new[] { TruckTaxiExtremeKind.Earthquake, TruckTaxiExtremeKind.Tornado,
                TruckTaxiExtremeKind.Avalanche, TruckTaxiExtremeKind.Mudslide };
            var weather = new[] { TruckTaxiWeatherCondition.Clear, TruckTaxiWeatherCondition.Thunderstorm,
                TruckTaxiWeatherCondition.HeavySnow, TruckTaxiWeatherCondition.HeavyRain };
            var maximumDays = new[] { 5, 3, 4, 4 };
            var starts = kinds.ToDictionary(k => k, _ => 0);
            Action<TruckTaxiExtremeWarning> countStart = warning =>
            { if (starts.ContainsKey(warning.Kind)) starts[warning.Kind]++; };
            director.WarningRaised += countStart;
            try
            {
                for (int index = 0; index < kinds.Length; index++)
                {
                    var kind = kinds[index];
                    director.Clear();
                    Assert.IsTrue(host.CalendarWeather.ForceForecast(weather[index]), host.CalendarWeather.LastDiagnostic);
                    // The coordinator samples the real weather service once per second.
                    yield return new WaitForSecondsRealtime(1.2f);
                    if (kind == TruckTaxiExtremeKind.Mudslide)
                    {
                        host.Environment.AdvanceHour();
                        host.Environment.AdvanceHour();
                        host.Environment.AdvanceHour();
                    }
                    yield return WaitFor(() => director.GetScheduleStatus(kind).Eligible, 3,
                        kind + " never became eligible from the actual calendar/weather service.");
                    int before = starts[kind];
                    int elapsedDays = 0;
                    for (int day = 1; day <= maximumDays[index] && starts[kind] == before; day++)
                    {
                        director.Clear();
                        host.CalendarWeather.AdvanceDay();
                        DateTime date = host.CalendarWeather.CurrentDateTime.Date;
                        host.SetPaused(false);
                        yield return WaitFor(() => director.Attempts.Any(a => a.Kind == kind && a.Time.Date == date &&
                            (a.Started || a.Reason == "ROLL_FAILED" || a.Reason == "NO_PROCEDURAL_CANDIDATE" ||
                             a.Reason == "PLAYER_UNAVAILABLE" || a.Reason == "REGION_UNLOADED")),
                            9, kind + " had no daily scheduler attempt on " + date.ToString("yyyy-MM-dd"));
                        host.SetPaused(true);
                        elapsedDays = day;
                        var attempt = director.Attempts.Last(a => a.Kind == kind && a.Time.Date == date);
                        Assert.IsTrue(attempt.Started || attempt.Reason == "ROLL_FAILED", AttemptSummary(director, kind));
                        if (starts[kind] == before) continue;
                        Assert.IsTrue(attempt.Started);
                        Assert.IsTrue(attempt.VisualMaterialized, "A natural start must materialize in the loaded region.");
                        Assert.IsTrue(attempt.AlertSent);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.RegionId));
                        Assert.AreEqual(date, director.GetScheduleStatus(kind).LastOccurrence.Date);
                        Assert.IsTrue(director.ActiveSnapshots.Any(s => s.Kind == kind && s.LoadedNearby));
                        Debug.Log($"ADDENDUM NATURAL {kind}: {day} eligible day(s), {date:yyyy-MM-dd}, " +
                            $"region {attempt.RegionId}, source {attempt.Source}, distance {attempt.PlayerDistanceMeters:F0}m");
                    }
                    Assert.AreEqual(before + 1, starts[kind], kind + " exceeded its eligible-day bound.");
                    Assert.That(elapsedDays, Is.InRange(1, maximumDays[index]));
                    int sameDayStarts = starts[kind];
                    director.Clear();
                    host.SetPaused(false);
                    yield return new WaitForSecondsRealtime(5.2f);
                    host.SetPaused(true);
                    Assert.AreEqual(sameDayStarts, starts[kind], "Clearing an event must not reroll it on the same eligible day.");
                }
                Assert.IsTrue(kinds.All(k => starts[k] > 0), "Every target kind needs an observed natural start.");
            }
            finally { director.WarningRaised -= countStart; director.Clear(); }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator CalendarDueKaijuMaterializesWithoutForceCommand()
        {
            host.Extremes.enabled = false;
            host.Extremes.Clear();
            host.Kaiju.EndKaiju();
            host.Player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            var due = host.Kaiju.NextNaturalSighting;
            Assert.Greater(due, host.CalendarWeather.CurrentDateTime);
            Assert.Less(due, host.Kaiju.NextNaturalAttack);
            int hours = 0;
            while (host.CalendarWeather.CurrentDateTime < due && hours++ < 73)
                host.Environment.AdvanceHour();
            Assert.GreaterOrEqual(host.CalendarWeather.CurrentDateTime, due);
            host.StartShift();
            yield return WaitFor(() => host.Kaiju.IsActive, 5, "Due calendar sighting never materialized.");
            Assert.AreEqual(TruckTaxiKaijuKind.Sighting, host.Kaiju.Kind);
            Assert.IsNotNull(host.Kaiju.GetComponentInChildren<TruckTaxiKaijuActor>());
            Assert.IsFalse(string.IsNullOrWhiteSpace(host.Kaiju.State.RegionId));
            Assert.Greater(host.Kaiju.NextNaturalSighting, host.CalendarWeather.CurrentDateTime);
            Debug.Log("ADDENDUM NATURAL KAIJU: " + host.CalendarWeather.CurrentDateTime.ToString("O") +
                ", region=" + host.Kaiju.State.RegionId + ", advancedHours=" + hours);
            host.Kaiju.EndKaiju();
            yield return null;
            Assert.IsFalse(host.Kaiju.IsActive);
        }

        private IEnumerator TravelTo(Vector3 position, string label)
        {
            Assert.IsNotNull(host.FastTravel);
            Assert.AreEqual(TruckTaxiState.Available, host.Session.State,
                "Successful travel requires an Available session without an active fare.");
            int completed = host.FastTravel.CompletedTravels;
            Assert.IsTrue(host.FastTravel.TryTravel(position, label), host.FastTravel.Feedback);
            yield return WaitFor(() => !host.FastTravel.IsTravelling, 70, "Fast travel did not finish: " + label);
            Assert.AreEqual(completed + 1, host.FastTravel.CompletedTravels, host.FastTravel.Feedback);
            Assert.AreEqual(TruckTaxiState.Available, host.Session.State);
            var regional = host.GetComponent<TruckTaxiRegionalWorld>();
            Assert.IsTrue(regional != null && regional.IsPositionAvailable(host.Player.transform.position));
            Assert.IsFalse(host.Player.GetComponent<Rigidbody>().isKinematic, "Fast travel left NWH kinematic.");
        }

        private TruckTaxiMapMarker FindHazardMarker(string zoneId) =>
            host.GetComponentsInChildren<TruckTaxiMapMarker>(true).FirstOrDefault(m => m.stableId == "hazard:" + zoneId);

        private Transform FindEventVisual(TruckTaxiExtremeSnapshot snapshot)
        {
            var zone = host.WorldCoordinator.HazardZones.FirstOrDefault(z => z != null && z.zoneId == snapshot.ZoneId);
            string visualName = zone != null && zone.nearbyVisualPrefab != null
                ? zone.nearbyVisualPrefab.name + "(Clone)" : "Taxi " + snapshot.Kind + " temporary VFX";
            return host.Extremes.transform.Cast<Transform>().FirstOrDefault(child => child.name == visualName);
        }

        private static bool ForceHere(TruckTaxiExtremeEventDirector director, TruckTaxiExtremeKind kind)
        {
            switch (kind)
            {
                case TruckTaxiExtremeKind.Tornado: return director.ForceTornadoHere();
                case TruckTaxiExtremeKind.Earthquake: return director.ForceEarthquakeHere();
                case TruckTaxiExtremeKind.Avalanche: return director.ForceAvalancheHere();
                case TruckTaxiExtremeKind.Mudslide: return director.ForceMudslideHere();
                default: return director.StartEventHere(kind);
            }
        }

        private static string AttemptSummary(TruckTaxiExtremeEventDirector director, TruckTaxiExtremeKind kind)
        {
            var attempt = director.Attempts.LastOrDefault(a => a.Kind == kind);
            return $"{kind}: {attempt.Reason}, source={attempt.Source}, region={attempt.RegionId}, started={attempt.Started}";
        }

        private static RectTransform RequireRect(Transform parent, string name)
        {
            // Button labels can contain '/', which Transform.Find interprets as a path.
            var result = parent.Cast<Transform>().FirstOrDefault(child => child.name == name) as RectTransform;
            Assert.IsNotNull(result, "Missing scene UI: " + name);
            return result;
        }

        private static Rect VisibleBounds(RectTransform rect)
        {
            Assert.IsNotNull(rect);
            Assert.IsTrue(rect.gameObject.activeInHierarchy, rect.name + " is hidden.");
            return PositiveBounds(rect);
        }

        private static Rect PositiveBounds(RectTransform rect)
        {
            Assert.IsNotNull(rect);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var bounds = Rect.MinMaxRect(corners.Min(p => p.x), corners.Min(p => p.y),
                corners.Max(p => p.x), corners.Max(p => p.y));
            Assert.Greater(bounds.width, 1, rect.name + " has no positive width.");
            Assert.Greater(bounds.height, 1, rect.name + " has no positive height.");
            return bounds;
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds, string message)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(condition(), message);
        }

        private static IEnumerator UnloadWorld()
        {
            Time.timeScale = 1;
            // Keep the runner-owned scene loaded. Unity selects it when the active demo unloads.
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(IsTaxiScene).ToArray();
            foreach (var scene in scenes)
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (LwsApplicationBootstrap.Instance != null) Object.Destroy(LwsApplicationBootstrap.Instance.gameObject);
            yield return null;
            // A streaming operation can finish while the original scene list is being unloaded.
            var remaining = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(IsTaxiScene).ToArray();
            foreach (var scene in remaining)
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static bool IsTaxiScene(Scene scene) => scene.isLoaded &&
            (scene.name.StartsWith("TruckTaxi_", StringComparison.Ordinal) ||
             scene.name.StartsWith("Taxi_", StringComparison.Ordinal));

        private static void RestoreInt(string key, bool existed, int value)
        { if (existed) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key); }
    }
}
