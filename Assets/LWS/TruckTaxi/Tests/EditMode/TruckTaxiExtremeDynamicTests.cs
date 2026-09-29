using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated"), Category("TaxiMegaPass")]
    public sealed class TruckTaxiExtremeDynamicTests
    {
        [Test]
        public void TornadoCandidateIsReadableDistanceAndNeverFollowsPlayer()
        {
            var settings = new TruckTaxiExtremeFootprintSettings();
            Assert.IsTrue(TruckTaxiExtremeFootprint.TryCreate(TruckTaxiExtremeKind.Tornado,
                "dynamic:tornado:1", "taxi.town01", Vector3.zero, Vector3.forward, settings,
                _ => true, p => p, out var footprint, out _));
            Assert.GreaterOrEqual(Vector3.Distance(Vector3.zero, footprint.Path[0]), 300);
            Assert.LessOrEqual(Vector3.Distance(Vector3.zero, footprint.Path[0]), 800);
            Vector3 first = footprint.PositionAt(.5f);
            Vector3 movedPlayer = new Vector3(2000, 0, 2000);
            Assert.AreEqual(first, footprint.PositionAt(.5f));
            Assert.Greater(Vector3.Distance(movedPlayer, footprint.PositionAt(.5f)), 1000);
            Assert.IsTrue(footprint.IsProcedural);
        }

        [Test]
        public void SlideUsesSlopeWhenAvailableAndMovingFlatFallbackOtherwise()
        {
            var settings = new TruckTaxiExtremeFootprintSettings();
            Func<Vector3, bool> available = _ => true;
            Assert.IsTrue(TruckTaxiExtremeFootprint.TryCreate(TruckTaxiExtremeKind.Avalanche,
                "snow", "taxi.highway", Vector3.zero, Vector3.forward, settings,
                available, p => new Vector3(p.x, p.z * .1f, p.z), out var slope, out _));
            Assert.IsFalse(slope.IsFlatFallback);
            Assert.Greater(slope.Path[0].y - slope.Path[2].y, 3);
            Assert.IsTrue(TruckTaxiExtremeFootprint.TryCreate(TruckTaxiExtremeKind.Mudslide,
                "mud", "taxi.town01", Vector3.zero, Vector3.forward, settings,
                available, p => p, out var flat, out _));
            Assert.IsTrue(flat.IsFlatFallback);
            Assert.AreEqual(3, flat.Path.Length);
            Assert.AreNotEqual(flat.Path[0], flat.Path[2]);
            Assert.AreEqual(2, flat.DebrisPositions.Length);
            Assert.GreaterOrEqual(Vector3.Distance(Vector3.zero, flat.Path[0]), 150);
            Assert.LessOrEqual(Vector3.Distance(Vector3.zero, flat.Path[0]), 500);
        }

        [Test]
        public void SlideSearchesPastFlatCandidateForRealDownhillSlope()
        {
            Assert.IsTrue(TruckTaxiExtremeFootprint.TryCreate(TruckTaxiExtremeKind.Mudslide,
                "slope", "taxi.town01", Vector3.zero, Vector3.forward,
                new TruckTaxiExtremeFootprintSettings(), _ => true,
                p => new Vector3(p.x, p.x * .1f, p.z), out var footprint, out _));
            Assert.IsFalse(footprint.IsFlatFallback);
            Assert.Greater(footprint.Source.x, 0);
            Assert.Greater(footprint.Path[0].y - footprint.Path[2].y, 3);
        }

        [Test]
        public void QuakeAndHurricaneNeedNoAuthoredZoneOrTerrainSlope()
        {
            var player = new Vector3(25, 2, 40);
            foreach (var kind in new[] { TruckTaxiExtremeKind.Earthquake,
                TruckTaxiExtremeKind.Hurricane })
            {
                Assert.IsTrue(TruckTaxiExtremeFootprint.TryCreate(kind, "local", "taxi.town02",
                    player, Vector3.forward, new TruckTaxiExtremeFootprintSettings(),
                    _ => true, null, out var footprint, out _));
                Assert.AreEqual(player, footprint.Source);
                Assert.IsTrue(footprint.ContainsDanger(player, 0));
                Assert.IsTrue(footprint.IsProcedural);
            }
        }

        [Test]
        public void UnloadedRegionRejectsWithoutTeleportingFootprint()
        {
            bool created = TruckTaxiExtremeFootprint.TryCreate(TruckTaxiExtremeKind.Tornado,
                "x", "taxi.town01", Vector3.zero, Vector3.forward,
                new TruckTaxiExtremeFootprintSettings(), p => p.sqrMagnitude < 100,
                p => p, out _, out string reason);
            Assert.IsFalse(created);
            Assert.AreEqual("NO_PROCEDURAL_CANDIDATE", reason);
        }

        [TestCase(TruckTaxiExtremeKind.Tornado, 3)]
        [TestCase(TruckTaxiExtremeKind.Earthquake, 5)]
        [TestCase(TruckTaxiExtremeKind.Avalanche, 4)]
        [TestCase(TruckTaxiExtremeKind.Mudslide, 4)]
        public void AcceleratedEligibleCalendarGuaranteesOccurrenceByBound(
            TruckTaxiExtremeKind kind, int maximumDays)
        {
            var schedule = new TruckTaxiExtremeDaySchedule(new TruckTaxiExtremeScheduleSettings());
            DateTime start = new DateTime(2026, 9, 1);
            int occurred = 0;
            for (int day = 0; day < maximumDays; day++)
            {
                DateTime date = start.AddDays(day);
                if (!schedule.TryDay(kind, date, true, .999f, out _)) continue;
                occurred = day + 1;
                schedule.RecordStart(kind, date);
                break;
            }
            Assert.AreEqual(maximumDays, occurred);
            Assert.AreEqual(start.AddDays(occurred - 1), schedule.LastOccurrence(kind));
            Assert.AreEqual(0, schedule.DryDays(kind));
        }

        [Test]
        public void IneligibleDaysDoNotConsumeDryStreakAndEachDayIsAttemptedOnce()
        {
            var schedule = new TruckTaxiExtremeDaySchedule(new TruckTaxiExtremeScheduleSettings());
            DateTime day = new DateTime(2026, 9, 1);
            Assert.IsFalse(schedule.TryDay(TruckTaxiExtremeKind.Avalanche, day, false, 0,
                out string reason));
            Assert.AreEqual("WRONG_WEATHER", reason);
            Assert.AreEqual(0, schedule.DryDays(TruckTaxiExtremeKind.Avalanche));
            Assert.IsTrue(schedule.TryDay(TruckTaxiExtremeKind.Avalanche, day, true, 0,
                out reason));
            Assert.IsFalse(schedule.TryDay(TruckTaxiExtremeKind.Avalanche, day, true, 0,
                out reason));
            Assert.AreEqual("ALREADY_ATTEMPTED", reason);
            Assert.IsFalse(schedule.TryDay(TruckTaxiExtremeKind.Avalanche, day.AddDays(1), true,
                .999f, out reason));
            Assert.AreEqual("ROLL_FAILED", reason);
            Assert.AreEqual(1, schedule.DryDays(TruckTaxiExtremeKind.Avalanche));
        }
    }
}
