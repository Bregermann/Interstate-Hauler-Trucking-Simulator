using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiEnvironmentNeedsTests
    {
        private TruckTaxiEnvironmentSettings settings;
        private readonly List<Object> allocated = new List<Object>();
        [SetUp] public void Setup() { settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>(); allocated.Add(settings); }
        [TearDown] public void Cleanup() { foreach (var value in allocated) Object.DestroyImmediate(value); allocated.Clear(); }
        private TruckTaxiDriverNeedsState State(float pressure = 0)
        { settings.bladderCycleGameHours = new Vector2(6, 6); settings.startingBladder = pressure; return new TruckTaxiDriverNeedsState(settings, 11); }

        [TestCase(5, TruckTaxiDayPeriod.Dawn)] [TestCase(7, TruckTaxiDayPeriod.Morning)] [TestCase(11, TruckTaxiDayPeriod.Day)]
        [TestCase(14, TruckTaxiDayPeriod.Afternoon)] [TestCase(18, TruckTaxiDayPeriod.Sunset)] [TestCase(20, TruckTaxiDayPeriod.Night)]
        [TestCase(0, TruckTaxiDayPeriod.LateNight)] [TestCase(24, TruckTaxiDayPeriod.LateNight)] [TestCase(-1, TruckTaxiDayPeriod.Night)]
        public void DayPeriodsUseGameHours(float hours, TruckTaxiDayPeriod expected) => Assert.AreEqual(expected, TruckTaxiEnvironmentSettings.PeriodAt(hours));
        [Test] public void DefaultsGiveFortyEightMinuteDayAndFourToEightGameHourNeedCycle()
        {
            Assert.AreEqual(30, settings.timeScale); Assert.AreEqual(new Vector2(4, 8), settings.bladderCycleGameHours);
            for (int i = 0; i < 100; i++) Assert.That(new TruckTaxiDriverNeedsState(settings, i).CycleGameHours, Is.InRange(4f, 8f));
        }
        [Test] public void BladderUsesElapsedGameSecondsAndIgnoresReverseAndInvalidTime()
        {
            var state = State(); state.AdvanceGameSeconds(3 * 3600); Assert.AreEqual(.5f, state.Pressure, .0001);
            state.AdvanceGameSeconds(-3600); state.AdvanceGameSeconds(double.NaN); state.AdvanceGameSeconds(double.PositiveInfinity);
            Assert.AreEqual(.5f, state.Pressure, .0001);
        }
        [Test] public void ExistingClockPauseAndTimeSetCannotArtificiallyAdvanceNeed()
        {
            var clock = new LwsGameClockService(); clock.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            var state = State(); double last = clock.CurrentSnapshot.totalGameSeconds;
            clock.ClockChanged += snapshot => { state.AdvanceGameSeconds(snapshot.totalGameSeconds - last); last = snapshot.totalGameSeconds; };
            clock.SetTimeScale(30); clock.SetPaused(true); clock.Tick(600); clock.SetTimeOfDayHours(23);
            Assert.AreEqual(0, state.Pressure);
            clock.SetPaused(false); clock.Tick(360); Assert.AreEqual(.5f, state.Pressure, .0001);
        }
        [Test] public void UrgencyEmitsOncePerCycleAndCrisisIsNonfatalWithOneConsequence()
        {
            var state = State(); var events = new List<TruckTaxiDriverNeedEvent>(); state.Event += events.Add;
            state.AdvanceGameSeconds(5.2 * 3600); state.AdvanceGameSeconds(10);
            Assert.AreEqual(1, events.FindAll(e => e == TruckTaxiDriverNeedEvent.BladderUrgent).Count);
            state.AdvanceGameSeconds(3600 + settings.crisisGraceGameMinutes * 60);
            Assert.AreEqual(1, state.CrisisAccidents); Assert.IsTrue(state.CabMess); Assert.Less(state.Pressure, .25f);
            state.AdvanceGameSeconds(1); Assert.AreEqual(1, state.CrisisAccidents);
        }
        [Test] public void JugRequiresNeedAndEmptyJugAndThreeTimedCues()
        {
            var state = State(); Assert.IsFalse(state.StartJug()); state.SetPressure(.9f); Assert.IsTrue(state.StartJug()); Assert.IsFalse(state.StartJug());
            for (int i = 0; i < 3; i++) state.TickJug(settings.jugDurationSeconds / 4, true, true, false, 0);
            state.TickJug(settings.jugDurationSeconds / 4, true, false, false, 0);
            Assert.IsFalse(state.JugActive); Assert.IsTrue(state.FilledJug); Assert.AreEqual(0, state.Pressure); Assert.AreEqual(1, state.SuccessfulJugs);
            state.SetPressure(.9f); Assert.IsFalse(state.StartJug());
            Assert.IsFalse(state.DisposeJug(false)); Assert.IsTrue(state.DisposeJug(true)); Assert.IsTrue(state.StartJug());
        }
        [Test] public void EarlyCueAndMissedCueSpillWithoutCreatingFilledJug()
        {
            var state = State(.9f); state.StartJug(); state.TickJug(.1f, true, true, false, 0);
            Assert.IsTrue(state.CabMess); Assert.AreEqual(1, state.Spills); Assert.IsFalse(state.FilledJug); Assert.IsFalse(state.JugActive);
            state.SetPressure(.9f); state.StartJug(); state.TickJug(3, true, false, false, 0);
            Assert.AreEqual(2, state.Spills);
        }
        [Test] public void ReleaseAndSustainedLateralAccelerationCauseSpill()
        {
            var state = State(.9f); state.StartJug(); state.TickJug(settings.jugReleaseGraceSeconds + .1f, false, false, false, 0);
            Assert.AreEqual(1, state.Spills);
            state.SetPressure(.9f); state.StartJug(); state.TickJug(.7f, true, false, true, settings.lateralAccelerationSpillThreshold + 1);
            Assert.AreEqual(2, state.Spills);
        }
        [Test] public void DrivingCueWindowIsTighterWithoutRemovingVehicleControls()
        {
            var state = State(.9f); Assert.Less(state.CueHalfWindow(true), state.CueHalfWindow(false));
            Assert.AreEqual(.11f, state.CueHalfWindow(true)); Assert.AreEqual(.23f, state.CueHalfWindow(false));
        }
        [Test] public void BathroomAndDisposalRequireActualStoppedTargetAndCannotInterruptQte()
        {
            var state = State(.9f); Assert.IsFalse(state.UseBathroom(false)); Assert.AreEqual(.9f, state.Pressure);
            state.StartJug(); Assert.IsFalse(state.UseBathroom(true)); state.CancelJug(); Assert.IsTrue(state.UseBathroom(true));
            Assert.AreEqual(0, state.Pressure); Assert.IsFalse(state.CleanCab(true));
        }
        [Test] public void BathroomRejectsSpeedWrongElevationAndOutsideBay()
        {
            var go = new GameObject("Restroom fixture"); allocated.Add(go);
            var location = go.AddComponent<TruckTaxiRideLocation>(); var point = go.AddComponent<TruckTaxiBathroomPoint>();
            point.location = location; point.stableId = "restroom.test";
            Assert.IsTrue(point.CanUse(Vector3.zero, 0, .447f));
            Assert.IsFalse(point.CanUse(Vector3.zero, 2, .447f)); Assert.IsFalse(point.CanUse(Vector3.up * 5, 0, .447f));
            Assert.IsFalse(point.CanUse(Vector3.right * 30, 0, .447f)); Assert.IsFalse(point.CanUse(Vector3.zero, float.NaN, .447f));
            point.stableId = ""; Assert.IsFalse(point.CanUse(Vector3.zero, 0, .447f));
        }
        [Test] public void WeatherSelectionHonorsWeightsExcludesCurrentAndRejectsFakeSnow()
        {
            var weights = new[] { new TruckTaxiWeatherWeight("clear", 100), new TruckTaxiWeatherWeight("fog", 1), new TruckTaxiWeatherWeight("snow", 100), new TruckTaxiWeatherWeight("heavy_rain", 0) };
            Assert.AreEqual("fog", TruckTaxiEnvironmentCoordinator.PickNextWeather(weights, "clear", .75f));
            Assert.IsFalse(TruckTaxiEnvironmentCoordinator.IsSupportedWeather("snow"));
            Assert.AreEqual("clear", TruckTaxiEnvironmentCoordinator.PickNextWeather(Array.Empty<TruckTaxiWeatherWeight>(), "clear", 1));
        }
        [Test] public void ScenicConditionOnlyChangesBonusNotObjectiveEligibility()
        {
            var go = new GameObject("Scenic fixture"); allocated.Add(go); var bonus = go.AddComponent<TruckTaxiScenicEnvironmentBonus>();
            Assert.IsTrue(bonus.Matches(TruckTaxiDayPeriod.Sunset, LwsWeatherSnapshot.Clear));
            Assert.IsFalse(bonus.Matches(TruckTaxiDayPeriod.Morning, LwsWeatherSnapshot.Clear));
            bonus.preferredWeatherId = "fog"; Assert.IsFalse(bonus.Matches(TruckTaxiDayPeriod.Sunset, LwsWeatherSnapshot.Clear));
            var fog = LwsWeatherSnapshot.Clear; fog.weatherPresetId = "fog";
            Assert.IsTrue(bonus.Matches(TruckTaxiDayPeriod.Sunset, fog));
            fog.transitioning = true; Assert.IsFalse(bonus.Matches(TruckTaxiDayPeriod.Sunset, fog));
            Assert.AreEqual(TruckTaxiStopCategory.Scenic, go.GetComponent<TruckTaxiStopObjectivePoint>().category);
        }
        [Test] public void RecreatedSessionDoesNotResumeAnInProgressJug()
        {
            var first = State(.9f); first.StartJug(); Assert.IsTrue(first.JugActive);
            var next = State(); Assert.IsFalse(next.JugActive); Assert.AreEqual(0, next.JugElapsed); Assert.IsFalse(next.FilledJug);
        }
    }
}
