using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiRoadPepTests
    {
        [Test]
        public void PerfectTimingScoresFullAndStrongCollisionBotsChallenge()
        {
            var perfect = new TruckTaxiRoadPepChallenge();
            for (int i = 0; i < 24; i++) perfect.Tick(.1f, 0, 0, false);
            perfect.Tick(.744f, 0, 0, true);
            Assert.IsTrue(perfect.Finished);
            Assert.AreEqual(TruckTaxiRoadPepRating.Perfect, perfect.Rating);
            Assert.That(perfect.Score, Is.InRange(90, 100));

            var collision = new TruckTaxiRoadPepChallenge();
            for (int i = 0; i < 24; i++) collision.Tick(.1f, 0, 0, false);
            collision.ReportCollision(6.5f);
            collision.Tick(.744f, 0, 0, true);
            Assert.AreEqual(TruckTaxiRoadPepRating.Botched, collision.Rating);
            Assert.LessOrEqual(collision.Score, 25);
        }

        [Test]
        public void ActualVehicleDisturbanceMovesTargetWithoutMovingTruck()
        {
            var challenge = new TruckTaxiRoadPepChallenge();
            challenge.Tick(.1f, 0, 1, false);
            Assert.Greater(challenge.Target01, .5f);
            Assert.AreEqual(.5f, challenge.Cursor01);
        }

        [Test]
        public void ReuseRefreshesWithinCapAndComedownEnds()
        {
            var effects = new TruckTaxiTemporaryEffects();
            for (int i = 0; i < 8; i++) effects.ActivateRoadPep(100);
            Assert.AreEqual(600, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.RoadPep).RemainingSeconds);
            Assert.AreEqual(3, effects.RoadPepOveruse);
            Assert.AreEqual(1, effects.VehiclePowerMultiplier);
            Assert.GreaterOrEqual(effects.HungerRateMultiplier, .35f);
            effects.Tick(600);
            Assert.AreEqual(0, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.RoadPep).RemainingSeconds);
            Assert.Greater(effects.GetSnapshot(TruckTaxiTemporaryEffectKind.RoadPepComedown).RemainingSeconds, 0);
            Assert.Greater(effects.HungerRateMultiplier, 1);
            effects.Tick(211);
            Assert.AreEqual(0, effects.RoadPepOveruse);
            Assert.AreEqual(1, effects.HungerRateMultiplier);
        }

        [Test]
        public void ItemMustPassChallengeAndClearDoesNotEraseMushroom()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 42);
                Assert.IsTrue(state.AddItem(TruckTaxiNeedsItem.RoadPep));
                Assert.IsFalse(state.Consume(TruckTaxiNeedsItem.RoadPep));
                Assert.IsTrue(state.BeginRoadPep());
                Assert.IsFalse(state.BeginRoadPep());
                state.CompleteRoadPep(40);
                Assert.AreEqual(0, state.Count(TruckTaxiNeedsItem.RoadPep));
                Assert.That(state.Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.RoadPep).RemainingSeconds,
                    Is.InRange(240, 480));
                state.Effects.Activate(TruckTaxiTemporaryEffectKind.MysteryMushroom);
                state.Effects.ClearRoadPep();
                Assert.Greater(state.Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).RemainingSeconds, 0);
            }
            finally { Object.DestroyImmediate(settings); }
        }
    }
}
