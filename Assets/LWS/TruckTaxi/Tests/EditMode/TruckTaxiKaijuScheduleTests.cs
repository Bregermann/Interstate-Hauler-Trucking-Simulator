using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiKaijuScheduleTests
    {
        private TruckTaxiKaijuConfig config;
        private static readonly DateTime StartTime = new DateTime(2026, 9, 28, 12, 0, 0);

        [SetUp] public void SetUp() => config = ScriptableObject.CreateInstance<TruckTaxiKaijuConfig>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(config);

        [Test]
        public void NaturalSightingFallsWithinOneToThreeGameDaysAndAttackWithinThreeToSeven()
        {
            var schedule = new TruckTaxiKaijuSchedule(config, 42);
            schedule.Start(StartTime);
            Assert.GreaterOrEqual(schedule.NextSighting, StartTime.AddDays(1));
            Assert.LessOrEqual(schedule.NextSighting, StartTime.AddDays(3));
            Assert.GreaterOrEqual(schedule.NextAttack, StartTime.AddDays(3));
            Assert.LessOrEqual(schedule.NextAttack, StartTime.AddDays(7));
            Assert.AreEqual(TruckTaxiKaijuKind.None, schedule.Advance(schedule.NextSighting.AddTicks(-1)));
            Assert.AreEqual(TruckTaxiKaijuKind.Sighting, schedule.Advance(schedule.NextSighting));
            Assert.Greater(schedule.NextSighting, schedule.LastSighting);
        }

        [Test]
        public void FixedSeedIsDeterministicAndNoFrameRateDependentRollsOccur()
        {
            var first = new TruckTaxiKaijuSchedule(config, 819);
            var second = new TruckTaxiKaijuSchedule(config, 819);
            first.Start(StartTime); second.Start(StartTime);
            for (int hour = 0; hour <= 48; hour++)
            {
                var time = StartTime.AddHours(hour);
                Assert.AreEqual(first.Advance(time), second.Advance(time));
                first.Advance(time);
            }
            Assert.AreEqual(first.LastSighting, second.LastSighting);
            Assert.AreEqual(first.NextSighting, second.NextSighting);
            Assert.AreEqual(first.NextAttack, second.NextAttack);
        }

        [Test]
        public void DryDeadlineAndClockRewindAreBounded()
        {
            config.sightingMinDays = 2;
            config.sightingMaxDays = 3;
            config.maximumSightingDryDays = 1.5f;
            var schedule = new TruckTaxiKaijuSchedule(config, 7);
            schedule.Start(StartTime);
            Assert.AreEqual(StartTime.AddDays(1.5), schedule.NextSighting);
            Assert.AreEqual(TruckTaxiKaijuKind.Sighting, schedule.Advance(StartTime.AddDays(1.5)));
            Assert.LessOrEqual(schedule.NextSighting, StartTime.AddDays(3));
            Assert.AreEqual(TruckTaxiKaijuKind.None, schedule.Advance(StartTime.AddDays(-1)));
            Assert.Greater(schedule.NextSighting, StartTime.AddDays(-1));
        }

        [Test]
        public void AttackResetsSightingDryStreakWithoutBacklog()
        {
            var schedule = new TruckTaxiKaijuSchedule(config, 5);
            schedule.Start(StartTime);
            var jump = StartTime.AddDays(40);
            Assert.AreEqual(TruckTaxiKaijuKind.Attack, schedule.Advance(jump));
            Assert.AreEqual(jump, schedule.LastSighting);
            Assert.AreEqual(TruckTaxiKaijuKind.None, schedule.Advance(jump));
            Assert.Greater(schedule.NextAttack, jump);
        }
    }
}
