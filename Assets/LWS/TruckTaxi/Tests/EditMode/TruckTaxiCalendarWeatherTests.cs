using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiMegaPass")]
    public sealed class TruckTaxiCalendarWeatherTests
    {
        private TruckTaxiEnvironmentSettings settings;
        private LwsGameClockService clock;
        private TruckTaxiCalendarWeatherService calendar;
        private readonly List<string> applied = new List<string>();

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            settings.startYear = 2026; settings.startMonth = 1; settings.startDay = 31;
            clock = new LwsGameClockService();
            clock.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            clock.SetDateTime(new LwsGameDateTime(2026, 1, 31, 12, 0, 0));
            calendar = new TruckTaxiCalendarWeatherService();
            Assert.IsTrue(calendar.Initialize(clock, settings, id => { applied.Add(id); return true; }));
        }

        [TearDown]
        public void TearDown()
        {
            calendar.Dispose();
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void CalendarUsesClockSnapshotAndAdvancesAcrossMonth()
        {
            Assert.AreEqual(new DateTime(2026, 1, 31, 12, 0, 0), calendar.CurrentDateTime);
            Assert.AreEqual(0, calendar.DayIndex);
            Assert.AreEqual(DayOfWeek.Saturday, calendar.DayOfWeek);
            clock.AddHours(14);
            Assert.AreEqual(new DateTime(2026, 2, 1, 2, 0, 0), calendar.CurrentDateTime);
            Assert.AreEqual(1, calendar.DayIndex);
            Assert.AreEqual(DayOfWeek.Sunday, calendar.DayOfWeek);
            calendar.SetDate(2028, 2, 29);
            calendar.SetTime(9);
            Assert.AreEqual(new DateTime(2028, 2, 29, 9, 0, 0), calendar.CurrentDateTime);
            calendar.AdvanceDay();
            Assert.AreEqual(new DateTime(2028, 3, 1, 9, 0, 0), calendar.CurrentDateTime);
        }

        [Test]
        public void ForecastCoversSevenDaysAndRollsWithoutGaps()
        {
            AssertWindow(new DateTime(2026, 1, 31));
            calendar.AdvanceDay();
            AssertWindow(new DateTime(2026, 2, 1));
        }

        [Test]
        public void ScheduledConditionIsTheAppliedWeatherAtEveryBoundary()
        {
            for (int i = 0; i < 6; i++)
            {
                var current = calendar.Current;
                Assert.AreEqual(current.PresetId, applied.Last());
                Assert.IsTrue(calendar.NextChange == null || calendar.NextChange >= current.End);
                clock.SetDateTime(LwsGameDateTime.FromDateTime(current.End.AddSeconds(1)));
                Assert.AreEqual(calendar.Current.PresetId, applied.Last());
            }
        }

        [Test]
        public void WinterSnowAndSummerStormWeightsAreConfigurable()
        {
            settings.weatherWeights = new[]
            {
                new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.LightSnowId, 1),
                new TruckTaxiWeatherWeight(LwsWeatherPresetCatalog.ThunderstormId, 1)
            };
            settings.winterSnowWeightMultiplier = 1000000;
            settings.summerStormWeightMultiplier = 1000000;
            calendar.Dispose();
            calendar = new TruckTaxiCalendarWeatherService();
            calendar.Initialize(clock, settings, id => { applied.Add(id); return true; });
            Assert.IsTrue(calendar.Forecast.Where(e => e.Start >= new DateTime(2026, 2, 1))
                .All(e => e.Condition == TruckTaxiWeatherCondition.Snow));
            calendar.SetDate(2026, 7, 1);
            Assert.IsTrue(calendar.Forecast.All(e => e.Condition == TruckTaxiWeatherCondition.Thunderstorm));
        }

        [Test]
        public void SevereOverrideChangesForecastAndActualThenExpires()
        {
            var now = calendar.CurrentDateTime;
            Assert.IsTrue(calendar.ForceCurrent(TruckTaxiWeatherCondition.Blizzard, 3, "Road closure"));
            Assert.AreEqual(TruckTaxiWeatherCondition.Blizzard, calendar.CurrentCondition);
            Assert.AreEqual(TruckTaxiWeatherSeverity.Emergency, calendar.CurrentSeverity);
            Assert.AreEqual("Road closure", calendar.CurrentWarning);
            Assert.AreEqual(TruckTaxiSnow.BlizzardId, applied.Last());
            Assert.AreEqual(1.5f, calendar.WeatherFareMultiplier);
            Assert.AreEqual(.65f, calendar.DemandMultiplier);
            Assert.IsTrue(calendar.Forecast.Where(e => e.Start < now.AddHours(3) && e.End > now)
                .All(e => e.Condition == TruckTaxiWeatherCondition.Blizzard));
            clock.SetDateTime(LwsGameDateTime.FromDateTime(now.AddHours(3).AddSeconds(1)));
            Assert.AreEqual(calendar.Current.PresetId, applied.Last());
        }

        [Test]
        public void RewindDiscardsDebugOverrideAndRestoresDeterministicSchedule()
        {
            var now = calendar.CurrentDateTime;
            // Initialization deliberately applies the authored starting weather once.
            clock.SetDateTime(LwsGameDateTime.FromDateTime(now.AddHours(-1)));
            clock.SetDateTime(LwsGameDateTime.FromDateTime(now));
            var original = calendar.Forecast.Select(e => (e.Start, e.End, e.Condition)).ToArray();
            calendar.ForceCurrent(TruckTaxiWeatherCondition.Blizzard, 4, "Debug storm");
            clock.SetDateTime(LwsGameDateTime.FromDateTime(now.AddHours(-1)));
            clock.SetDateTime(LwsGameDateTime.FromDateTime(now));
            CollectionAssert.AreEqual(original, calendar.Forecast.Select(e => (e.Start, e.End, e.Condition)).ToArray());
            Assert.AreEqual(calendar.Current.PresetId, applied.Last());
        }

        [Test] public void AdvancingWindowPreservesAlreadyPublishedTomorrow()
        {
            var tomorrow=calendar.CurrentDateTime.Date.AddDays(1);
            var published=calendar.Forecast.Where(e=>e.Start.Date==tomorrow).Select(e=>(e.Start,e.End,e.Condition)).ToArray();
            clock.SetDateTime(LwsGameDateTime.FromDateTime(tomorrow.AddHours(8)));
            CollectionAssert.AreEqual(published,calendar.Forecast.Where(e=>e.Start.Date==tomorrow).Select(e=>(e.Start,e.End,e.Condition)).ToArray());
        }

        [Test]
        public void ManualModeKeepsForecastTruthful()
        {
            calendar.SetAutomaticWeather(false);
            calendar.ForceCurrent(TruckTaxiWeatherCondition.Blizzard, 2, "Storm", TruckTaxiWeatherSeverity.Emergency);
            Assert.IsTrue(calendar.ForceForecast(TruckTaxiWeatherCondition.Fog));
            Assert.IsTrue(calendar.Forecast.All(e => e.Condition == TruckTaxiWeatherCondition.Fog));
            calendar.AdvanceDay();
            Assert.AreEqual(LwsWeatherPresetCatalog.FogId, applied.Last());
            Assert.IsTrue(calendar.Forecast.All(e => e.Condition == TruckTaxiWeatherCondition.Fog));
        }

        [Test]
        public void InvalidDebugDateDoesNotMutateClockOrSchedule()
        {
            var original = calendar.CurrentDateTime;
            Assert.IsFalse(calendar.SetDate(2027, 2, 29));
            Assert.AreEqual(original, calendar.CurrentDateTime);
            Assert.AreEqual(original.Date, calendar.Forecast[0].Start);
        }

        private void AssertWindow(DateTime start)
        {
            var forecast = calendar.Forecast;
            Assert.IsNotEmpty(forecast);
            Assert.AreEqual(start, forecast[0].Start);
            Assert.AreEqual(start.AddDays(7), forecast[forecast.Count - 1].End);
            for (int i = 1; i < forecast.Count; i++) Assert.AreEqual(forecast[i - 1].End, forecast[i].Start);
            Assert.AreEqual(7, forecast.Select(e => e.Start.Date).Distinct().Count());
        }
    }
}
