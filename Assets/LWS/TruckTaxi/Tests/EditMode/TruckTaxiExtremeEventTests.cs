using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated"), Category("TaxiMegaPass")]
    public sealed class TruckTaxiExtremeEventTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }

        private TruckTaxiHazardZone Zone(TruckTaxiExtremeKind kind)
        {
            root = new GameObject("extreme-test");
            var zone = root.AddComponent<TruckTaxiHazardZone>();
            zone.zoneId = "mountain-pass-slide";
            zone.regionId = "taxi.mountainpass";
            zone.kind = kind;
            zone.exposure = kind == TruckTaxiExtremeKind.Avalanche
                ? TruckTaxiHazardExposure.MountainSnow : TruckTaxiHazardExposure.RainSoakedSlope;
            zone.path = new[] { new Vector3(0, 20, 0), new Vector3(10, 12, 0), new Vector3(20, 0, 0) };
            return zone;
        }

        [Test]
        public void SlideRequiresAuthoredSlopeAndCorrectExposure()
        {
            var zone = Zone(TruckTaxiExtremeKind.Avalanche);
            Assert.IsTrue(zone.IsValid(out _));
            zone.exposure = TruckTaxiHazardExposure.BuiltArea;
            Assert.IsFalse(zone.IsValid(out _));
            zone.exposure = TruckTaxiHazardExposure.MountainSnow;
            zone.path[2] = new Vector3(20, 19, 0);
            Assert.IsFalse(zone.IsValid(out _));
            zone.path[2] = new Vector3(20, 0, 0);
            zone.kind = TruckTaxiExtremeKind.Mudslide;
            Assert.IsFalse(zone.IsValid(out _));
        }

        [Test]
        public void TornadoRequiresMovingPathAndFiniteRadius()
        {
            var zone = Zone(TruckTaxiExtremeKind.Tornado);
            zone.exposure = TruckTaxiHazardExposure.OpenTerrain;
            zone.path = new[] { Vector3.zero };
            Assert.IsFalse(zone.IsValid(out _));
            zone.path = new[] { Vector3.zero, new Vector3(20, 0, 0) };
            Assert.IsTrue(zone.IsValid(out _));
            Assert.AreEqual(new Vector3(10, 0, 0), zone.PositionAt(.5f));
            zone.radiusMeters = float.PositiveInfinity;
            Assert.IsFalse(zone.IsValid(out _));
        }

        [Test]
        public void DurationsCleanupAndForcesAreBounded()
        {
            Assert.AreEqual(TimeSpan.FromHours(6), TruckTaxiExtremeRules.Duration(TruckTaxiExtremeKind.Hurricane));
            Assert.AreEqual(TimeSpan.FromMinutes(2), TruckTaxiExtremeRules.Duration(TruckTaxiExtremeKind.Earthquake));
            Assert.AreEqual(TimeSpan.FromMinutes(30), TruckTaxiExtremeRules.Cleanup(TruckTaxiExtremeKind.Avalanche));
            Assert.AreEqual(TimeSpan.Zero, TruckTaxiExtremeRules.Cleanup(TruckTaxiExtremeKind.Tornado));
            Assert.AreEqual(5000, TruckTaxiExtremeRules.CappedForce(100000, 100000));
            Assert.AreEqual(0, TruckTaxiExtremeRules.CappedForce(float.NaN, 3500));
        }

        [Test]
        public void NaturalEligibilityIsRareAndEarthquakeIsNotWeatherPredicted()
        {
            foreach (TruckTaxiExtremeKind kind in Enum.GetValues(typeof(TruckTaxiExtremeKind)))
                Assert.Less(TruckTaxiExtremeRules.DailyChance(kind), .01f);
            Assert.IsFalse(TruckTaxiExtremeRules.NaturalEligible(TruckTaxiExtremeKind.Mudslide, true, false, 1.9f));
            Assert.IsTrue(TruckTaxiExtremeRules.NaturalEligible(TruckTaxiExtremeKind.Mudslide, true, false, 2));
            Assert.IsFalse(TruckTaxiExtremeRules.NaturalEligible(TruckTaxiExtremeKind.Avalanche, true, false, 4));
            Assert.IsTrue(TruckTaxiExtremeRules.NaturalEligible(TruckTaxiExtremeKind.Earthquake, false, false, 0));
        }

        [Test]
        public void StartExpiresAfterCleanupAndClearRemovesWarnings()
        {
            var zone = Zone(TruckTaxiExtremeKind.Avalanche);
            var host = root.AddComponent<TruckTaxiBootstrap>();
            var director = root.AddComponent<TruckTaxiExtremeEventDirector>();
            DateTime now = new DateTime(2026, 9, 27, 12, 0, 0);
            Assert.IsTrue(director.Initialize(host, () => now));
            Assert.IsTrue(director.RegisterZone(zone));
            Assert.IsTrue(director.StartEvent(TruckTaxiExtremeKind.Avalanche, zone.zoneId));
            Assert.IsFalse(director.StartEvent(TruckTaxiExtremeKind.Avalanche, zone.zoneId));
            Assert.AreEqual(1, director.ActiveSnapshots.Count);
            Assert.AreEqual(1, director.Warnings.Count);
            Assert.IsFalse(director.RoutingClosureSupported);
            now = now.AddMinutes(13);
            director.Tick();
            Assert.AreEqual(TruckTaxiExtremeStage.Cleanup, director.ActiveSnapshots[0].Stage);
            now = now.AddMinutes(30);
            director.Tick();
            Assert.AreEqual(0, director.ActiveSnapshots.Count);
            Assert.AreEqual(0, director.Warnings.Count);
            Assert.AreEqual(0, director.LiveVisualActorCount);
            Assert.AreEqual(0, director.LivePhysicsActorCount);
            director.Clear();
        }

        [Test]
        public void NaturalAttemptIsAtMostOncePerZonePerGameDay()
        {
            var zone = Zone(TruckTaxiExtremeKind.Mudslide);
            var host = root.AddComponent<TruckTaxiBootstrap>();
            var director = root.AddComponent<TruckTaxiExtremeEventDirector>();
            DateTime now = new DateTime(2026, 9, 27, 12, 0, 0);
            Assert.IsTrue(director.Initialize(host, () => now));
            Assert.IsTrue(director.RegisterZone(zone));
            Assert.IsFalse(director.TryStartNatural(zone.kind, zone.zoneId, false, false, 2, .99f));
            Assert.IsFalse(director.TryStartNatural(zone.kind, zone.zoneId, false, false, 2, 0));
            now = now.AddDays(1);
            Assert.IsTrue(director.TryStartNatural(zone.kind, zone.zoneId, false, false, 2, 0));
            director.Clear();
        }

        [Test]
        public void HurricanePublishesDurationAndClearReleasesWeatherAuthority()
        {
            var zone = Zone(TruckTaxiExtremeKind.Hurricane);
            zone.exposure = TruckTaxiHazardExposure.Coastal;
            zone.path = new[] { Vector3.zero };
            var host = root.AddComponent<TruckTaxiBootstrap>();
            var director = root.AddComponent<TruckTaxiExtremeEventDirector>();
            TruckTaxiExtremeWeatherProfile? observed = null;
            TimeSpan duration = TimeSpan.Zero;
            int callbacks = 0;
            Assert.IsTrue(director.Initialize(host, () => new DateTime(2026, 9, 27),
                (profile, requestedDuration) =>
                { observed = profile; duration = requestedDuration; callbacks++; }));
            Assert.IsTrue(director.RegisterZone(zone));
            Assert.IsTrue(director.StartEvent(TruckTaxiExtremeKind.Hurricane, zone.zoneId));
            Assert.IsTrue(observed.HasValue);
            Assert.AreEqual("thunderstorm", observed.Value.WeatherPresetId);
            Assert.AreEqual(28, observed.Value.WindMetersPerSecond);
            Assert.AreEqual(TimeSpan.FromHours(6), duration);
            director.Clear();
            Assert.IsFalse(observed.HasValue);
            Assert.AreEqual(TimeSpan.Zero, duration);
            Assert.AreEqual(2, callbacks);
        }

        [Test]
        public void ScheduledStormWarnsInLeadWindowButQuakeCannotBeForecast()
        {
            var zone = Zone(TruckTaxiExtremeKind.Hurricane);
            zone.exposure = TruckTaxiHazardExposure.Coastal;
            zone.path = new[] { Vector3.zero };
            var host = root.AddComponent<TruckTaxiBootstrap>();
            var director = root.AddComponent<TruckTaxiExtremeEventDirector>();
            DateTime now = new DateTime(2026, 9, 27, 12, 0, 0);
            Assert.IsTrue(director.Initialize(host, () => now));
            Assert.IsTrue(director.RegisterZone(zone));
            Assert.IsTrue(director.Schedule(zone.kind, zone.zoneId, now.AddHours(4)));
            Assert.AreEqual(0, director.Warnings.Count);
            now = now.AddHours(1).AddMinutes(1);
            director.Tick();
            Assert.AreEqual(1, director.Warnings.Count);
            zone.kind = TruckTaxiExtremeKind.Earthquake;
            Assert.IsFalse(director.Schedule(TruckTaxiExtremeKind.Earthquake, zone.zoneId, now.AddHours(2)));
            director.Clear();
            Assert.AreEqual(0, director.Warnings.Count);
        }

        [Test]
        public void CalendarRewindReleasesHurricaneOverrideAndTransientState()
        {
            var zone = Zone(TruckTaxiExtremeKind.Hurricane);
            zone.exposure = TruckTaxiHazardExposure.Coastal;
            zone.path = new[] { Vector3.zero };
            var host = root.AddComponent<TruckTaxiBootstrap>();
            var director = root.AddComponent<TruckTaxiExtremeEventDirector>();
            DateTime now = new DateTime(2026, 9, 27, 12, 0, 0);
            int restoreCalls = 0;
            Assert.IsTrue(director.Initialize(host, () => now, (profile, _) =>
            { if (!profile.HasValue) restoreCalls++; }));
            Assert.IsTrue(director.RegisterZone(zone));
            Assert.IsTrue(director.StartEvent(zone.kind, zone.zoneId));
            now = now.AddHours(-1);
            director.Tick();
            Assert.AreEqual(1, restoreCalls);
            Assert.IsFalse(director.ActiveSevereProfile.HasValue);
            Assert.AreEqual(0, director.ActiveSnapshots.Count);
            Assert.AreEqual(0, director.Warnings.Count);
        }
    }
}
