using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiAnalyticsTests
    {
        private readonly List<Object> allocated = new List<Object>();
        private readonly List<TruckTaxiAnalyticsStorage> stores = new List<TruckTaxiAnalyticsStorage>();
        private string directory;
        private bool requestsPreferenceExisted;
        private int requestsPreference;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "TruckTaxiAnalyticsTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            requestsPreferenceExisted = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            requestsPreference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
        }
        [TearDown] public async Task Cleanup()
        {
            foreach (var store in stores) { store.Dispose(); await Wait(() => store.Completion.IsCompleted); }
            stores.Clear();
            foreach (var item in allocated) if (item != null) Object.DestroyImmediate(item);
            allocated.Clear();
            if (requestsPreferenceExisted) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, requestsPreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
            PlayerPrefs.Save();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        private static TruckTaxiAnalyticsEvent Event(TruckTaxiAnalyticsKind kind = TruckTaxiAnalyticsKind.SessionSnapshot) =>
            new TruckTaxiAnalyticsEvent { kind = kind, installId = Guid.NewGuid().ToString("N"), sessionId = Guid.NewGuid().ToString("N"),
                eventId = Guid.NewGuid().ToString("N"), gameVersion = "1.2.3", buildId = "editor" };
        private TruckTaxiAnalyticsStorage Store(Func<string, CancellationToken, Task<bool>> delivery = null)
        { var store = new TruckTaxiAnalyticsStorage(directory, delivery); stores.Add(store); return store; }
        private static async Task Wait(Func<bool> predicate)
        {
            for (int i = 0; i < 200 && !predicate(); i++) await Task.Delay(25);
            Assert.IsTrue(predicate(), "Bounded worker did not reach expected state within five seconds.");
        }

        [TestCase("C:\\Users\\someone\\private.txt")]
        [TestCase("person@example.com")]
        [TestCase("raw player dialogue")]
        [TestCase("../secret")]
        [TestCase("=HYPERLINK(x)")]
        public void PrivateTextIsRejectedRatherThanTruncatedIntoAnIdentifier(string input)
        { Assert.AreEqual("", TruckTaxiAnalyticsPrivacy.Token(input)); }

        [Test] public void SchemaRejectsUnknownVersionsAndBoundsAllNumericInputs()
        {
            var e = Event(); e.passengerId = "private@email.com"; e.regionId = "C:/private/location";
            e.fps = double.NaN; e.distanceMeters = double.PositiveInfinity; e.rating = 100; e.patience = -100;
            e.fareCents = long.MaxValue; e.liveCars = int.MaxValue;
            var safe = e.Sanitize();
            Assert.AreEqual("", safe.passengerId); Assert.AreEqual("", safe.regionId);
            Assert.AreEqual(-1, safe.fps); Assert.AreEqual(0, safe.distanceMeters);
            Assert.AreEqual(5, safe.rating); Assert.AreEqual(-1, safe.patience);
            Assert.AreEqual(1000000000000L, safe.fareCents); Assert.AreEqual(1000000, safe.liveCars);
            e.schemaVersion = 2; Assert.IsNull(e.Sanitize());
            e.schemaVersion = 1; e.kind = (TruckTaxiAnalyticsKind)999; Assert.IsNull(e.Sanitize());
        }
        [Test] public void UnscaledPauseAndDrivingMetricsIgnoreInactiveAndTeleportCoordinates()
        {
            var m = new TruckTaxiAnalyticsMetrics();
            m.Tick(1, .5, 10, true, false);
            m.Tick(2, 0, 100, true, true);
            m.Tick(1, 1, 0, true, false);
            m.Tick(100, 100, 100, false, false);
            Assert.AreEqual(4, m.RealSeconds); Assert.AreEqual(2, m.PlaySeconds); Assert.AreEqual(2, m.PauseSeconds);
            Assert.AreEqual(1, m.DrivingSeconds); Assert.AreEqual(1, m.StoppedSeconds);
            Assert.AreEqual(5, m.DistanceMeters); Assert.AreEqual(10, m.MaxSpeedMps); Assert.AreEqual(5, m.AverageSpeedMps);
        }
        [TestCase("http://example.invalid/events", false)]
        [TestCase("https://user:secret@example.invalid/events", false)]
        [TestCase("https://example.invalid/events#private", false)]
        [TestCase("https://example.invalid/events", true)]
        public void UploadRequiresExplicitHttpsWithoutCredentialsOrFragment(string endpoint, bool valid)
        {
            var config = new TruckTaxiAnalyticsLocalConfig { remoteEnabled = true, endpoint = endpoint, performanceSeconds = 1 };
            Assert.AreEqual(valid, config.HasValidEndpoint); Assert.AreEqual(10, config.PerformanceSeconds);
            config.performanceSeconds = 1000; Assert.AreEqual(30, config.PerformanceSeconds);
            config.remoteEnabled = false; Assert.IsFalse(config.HasValidEndpoint);
        }
        [Test] public void RetryIsExponentialAndBounded()
        {
            Assert.AreEqual(5, TruckTaxiAnalyticsStorage.RetryDelay(1));
            Assert.AreEqual(10, TruckTaxiAnalyticsStorage.RetryDelay(2));
            Assert.AreEqual(300, TruckTaxiAnalyticsStorage.RetryDelay(10000));
        }
        [Test] public void CustomContentAliasesAreOpaqueBoundedAndSessionLocal()
        {
            var ids = new TruckTaxiAnalyticsContentIds();
            string first = ids.Alias("custom.authored.example");
            Assert.AreEqual(first, TruckTaxiAnalyticsPrivacy.RandomId(first));
            Assert.AreEqual(first, ids.Alias("custom.authored.example"));
            Assert.AreNotEqual(first, new TruckTaxiAnalyticsContentIds().Alias("custom.authored.example"));
            Assert.AreEqual("", ids.Alias("C:/private/presentation"));
            for (int i = 1; i < 256; i++) Assert.IsNotEmpty(ids.Alias("content." + i));
            Assert.AreEqual("", ids.Alias("overflow"));
            var e = Event(); e.customContentId = "raw-content-name"; e.customItemId = first;
            Assert.AreEqual("", e.Sanitize().customContentId); Assert.AreEqual(first, e.Sanitize().customItemId);
        }
        [Test] public void GenericCustomItemSignalRecordsOnlyNeutralAliasesAndUnsubscribes()
        {
            var session = Session(out var passenger, out _); var events = new List<TruckTaxiAnalyticsEvent>();
            using (var observer = new TruckTaxiAnalyticsSessionObserver(session, new[] { passenger }, events.Add))
            {
                session.StartShift(); session.OfferRide(); session.AcceptRide();
                session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true); session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
                Assert.IsTrue(session.RaiseCustomItemEvent("custom.used", "custom.item", "occurrence-1", "custom.interaction"));
                var e = events.Single(value => value.kind == TruckTaxiAnalyticsKind.CustomItemInteraction);
                Assert.AreEqual(e.customEventId, TruckTaxiAnalyticsPrivacy.RandomId(e.customEventId));
                Assert.AreEqual(e.customItemId, TruckTaxiAnalyticsPrivacy.RandomId(e.customItemId));
                Assert.AreEqual(e.customContentId, TruckTaxiAnalyticsPrivacy.RandomId(e.customContentId));
                StringAssert.DoesNotContain("custom.used", JsonUtility.ToJson(e));
                StringAssert.DoesNotContain("custom.item", JsonUtility.ToJson(e));
            }
            int count = events.Count; session.RaiseCustomItemEvent("custom.used", "custom.item", "occurrence-2"); Assert.AreEqual(count, events.Count);
        }
        [Test] public async Task LocalOnlyWriterPersistsRandomInstallIdentityAndSnapshotsInputs()
        {
            var store = Store(); var e = Event(); e.passengerId = "passenger.test";
            string originalInstall = e.installId;
            Assert.IsTrue(store.TryRecord(e)); e.passengerId = "changed";
            Assert.AreEqual(originalInstall, e.installId, "Writer must not mutate the caller DTO.");
            store.Dispose(); await Wait(() => store.Completion.IsCompleted);
            string root = Path.Combine(directory, "Analytics"), id = File.ReadAllText(Path.Combine(root, "install.id"));
            Assert.AreEqual(id, TruckTaxiAnalyticsPrivacy.RandomId(id));
            var records = Directory.GetFiles(root, "events-*.jsonl").SelectMany(TruckTaxiAnalyticsStorage.ReadEvents).ToArray();
            Assert.AreEqual(1, records.Length); Assert.AreEqual("passenger.test", records[0].passengerId);
            Assert.AreEqual(id, records[0].installId); Assert.AreEqual(TruckTaxiAnalyticsUploadResult.NotConfigured, store.LastUploadResult);
            Assert.IsEmpty(Directory.GetFiles(root, "pending-*.jsonl"));
            var second = Store(); Assert.IsTrue(second.TryRecord(Event())); second.Dispose(); await Wait(() => second.Completion.IsCompleted);
            Assert.AreEqual(id, File.ReadAllText(Path.Combine(root, "install.id")));
        }
        [Test] public async Task FailedDeliverySurvivesRestartThenAcknowledgesOnlyAfterSuccess()
        {
            File.WriteAllText(Path.Combine(directory, "analytics.local.json"), JsonUtility.ToJson(new TruckTaxiAnalyticsLocalConfig
                { remoteEnabled = true, endpoint = "https://example.invalid/telemetry" }));
            int calls = 0;
            var failed = Store((body, token) => { Interlocked.Increment(ref calls); return Task.FromResult(false); });
            failed.TryRecord(Event()); failed.RequestBatch();
            await Wait(() => failed.LastUploadResult == TruckTaxiAnalyticsUploadResult.RetryScheduled);
            Assert.Greater(failed.PendingUploadEvents, 0); failed.Dispose(); await Wait(() => failed.Completion.IsCompleted);
            Assert.AreEqual(1, calls);
            string uploaded = null;
            var resumed = Store((body, token) => { uploaded = body; return Task.FromResult(true); }); resumed.RequestBatch();
            await Wait(() => resumed.LastUploadResult == TruckTaxiAnalyticsUploadResult.Delivered);
            Assert.AreEqual(0, resumed.PendingUploadEvents); StringAssert.Contains("schemaVersion", uploaded);
            Assert.IsNotEmpty(Directory.GetFiles(Path.Combine(directory, "Analytics"), "events-*.jsonl"));
        }
        [Test] public async Task OfflineQueueAndJournalHaveHardFileCaps()
        {
            string root = Path.Combine(directory, "Analytics"); Directory.CreateDirectory(root);
            string line = JsonUtility.ToJson(Event()) + "\n";
            for (int i = 0; i < 70; i++)
            {
                File.WriteAllText(Path.Combine(root, "events-" + i.ToString("D3") + ".jsonl"), line);
                File.WriteAllText(Path.Combine(root, "pending-" + i.ToString("D3") + ".jsonl"), line);
            }
            var store = Store(); await Wait(() => store.LastLocalResult == TruckTaxiAnalyticsLocalResult.Ready);
            Assert.LessOrEqual(Directory.GetFiles(root, "events-*.jsonl").Length, TruckTaxiAnalyticsStorage.JournalFileLimit);
            Assert.LessOrEqual(Directory.GetFiles(root, "pending-*.jsonl").Length, TruckTaxiAnalyticsStorage.PendingFileLimit);
            Assert.AreEqual(64, store.PendingUploadEvents); Assert.GreaterOrEqual(store.DroppedEvents, 6);
        }
        [Test] public void CorruptLinesAndUnknownFieldsCannotEscapeCsvOrQueueReadback()
        {
            string root = Path.Combine(directory, "Analytics"); Directory.CreateDirectory(root);
            var e = Event(TruckTaxiAnalyticsKind.Performance); e.fps = 42;
            string json = JsonUtility.ToJson(e);
            json = json.Substring(0, json.Length - 1) + ",\"privateData\":\"do-not-export\"}";
            File.WriteAllText(Path.Combine(root, "events-test.jsonl"), json + "\n{truncated\n");
            var read = TruckTaxiAnalyticsStorage.ReadEvents(Path.Combine(root, "events-test.jsonl"));
            Assert.AreEqual(1, read.Count); StringAssert.DoesNotContain("do-not-export", JsonUtility.ToJson(read[0]));
            TruckTaxiAnalyticsCsv.Export(root);
            Assert.AreEqual(6, Directory.GetFiles(Path.Combine(root, "Exports"), "*.csv").Length);
            Assert.AreEqual(2, File.ReadAllLines(Path.Combine(root, "Exports", "Performance.csv")).Length);
            foreach (string path in Directory.GetFiles(Path.Combine(root, "Exports"), "*.csv")) StringAssert.DoesNotContain("do-not-export", File.ReadAllText(path));
            StringAssert.StartsWith("\"'=", TruckTaxiAnalyticsPrivacy.Csv("=formula"));
        }
        private T Asset<T>() where T : ScriptableObject
        { var value = ScriptableObject.CreateInstance<T>(); allocated.Add(value); return value; }
        private TruckTaxiSession Session(out PassengerProfile passenger, out TruckTaxiConfiguration config)
        {
            passenger = Asset<PassengerProfile>(); passenger.passengerId = "passenger.test";
            passenger.basePatience = 10000; passenger.requestFrequency = 10000;
            config = Asset<TruckTaxiConfiguration>(); config.minimumTripDistance = 10; config.maximumTripDistance = 1000;
            config.passengers = new[] { passenger }; config.boardingSeconds = 1;
            var stops = new List<TruckTaxiRideLocation>();
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Analytics fixture"); allocated.Add(go);
                var stop = go.AddComponent<TruckTaxiRideLocation>(); stop.locationId = "location." + i;
                go.transform.position = Vector3.right * i * 100; stops.Add(stop);
            }
            var session = new TruckTaxiSession(config, stops, 31); session.SetRideRequestsEnabled(true); return session;
        }
        [Test] public void ExistingSessionEventsAreObservedOnceAndDetachCleanly()
        {
            var session = Session(out var passenger, out _); var events = new List<TruckTaxiAnalyticsEvent>();
            using (var observer = new TruckTaxiAnalyticsSessionObserver(session, new[] { passenger }, events.Add))
            {
                session.StartShift(); Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
                session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true); session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
                session.DebugComplete(); session.DebugComplete();
                Assert.AreEqual(1, events.Count(e => e.kind == TruckTaxiAnalyticsKind.OfferGenerated));
                Assert.AreEqual(1, events.Count(e => e.kind == TruckTaxiAnalyticsKind.OfferAccepted));
                Assert.AreEqual(1, events.Count(e => e.kind == TruckTaxiAnalyticsKind.PickupCompleted));
                Assert.AreEqual(1, events.Count(e => e.kind == TruckTaxiAnalyticsKind.RideFinished));
                Assert.AreEqual(100, observer.CoveragePercent); Assert.AreEqual("", observer.ApprovedPassenger("private-id"));
            }
            int count = events.Count; session.ContinueShift(); session.OfferRide(); Assert.AreEqual(count, events.Count);
        }
        [Test] public void SharedDeclineExpiryPathIsReportedAsUnknownNotInvented()
        {
            var session = Session(out var passenger, out var config); var events = new List<TruckTaxiAnalyticsEvent>();
            using (var observer = new TruckTaxiAnalyticsSessionObserver(session, new[] { passenger }, events.Add))
            {
                session.StartShift(); session.OfferRide(); session.DeclineRide();
                session.OfferRide(); session.Tick(config.offerDuration + 1, Vector3.zero, 0, 0, true);
                Assert.AreEqual(2, events.Count(e => e.kind == TruckTaxiAnalyticsKind.OfferClosedUnknown));
                Assert.IsFalse(events.Any(e => e.kind == TruckTaxiAnalyticsKind.OfferDeclined || e.kind == TruckTaxiAnalyticsKind.OfferExpired));
            }
        }
    }
}
