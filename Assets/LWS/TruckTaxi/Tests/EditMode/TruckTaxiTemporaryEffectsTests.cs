using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiTemporaryEffectsTests
    {
        [Test]
        public void LaunchIsStrongAtLowSpeedAndSmoothlyReturnsToBaseline()
        {
            float previous = float.MaxValue;
            for (int mph = 0; mph <= 20; mph++)
            {
                float value = TruckTaxiVehicleHandlingOverride.LaunchMultiplier(mph);
                Assert.That(value, Is.LessThanOrEqualTo(previous + .0001f), mph + " MPH");
                if (mph > 0) Assert.That(previous - value, Is.LessThan(.45f), mph + " MPH cliff");
                previous = value;
            }
            Assert.That(TruckTaxiVehicleHandlingOverride.LaunchMultiplier(0), Is.GreaterThan(3));
            Assert.That(TruckTaxiVehicleHandlingOverride.LaunchMultiplier(10), Is.GreaterThan(2));
            Assert.AreEqual(1, TruckTaxiVehicleHandlingOverride.LaunchMultiplier(20));
            Assert.AreEqual(1, TruckTaxiVehicleHandlingOverride.LaunchMultiplier(70));
            Assert.AreEqual(1, TruckTaxiVehicleHandlingOverride.LaunchMultiplier(float.NaN));
        }

        [Test]
        public void EffectsRefreshWithoutStackingAndExpireToExactNeutralPower()
        {
            var effects = new TruckTaxiTemporaryEffects();
            effects.SetShortDebugDurations(true);
            int started = 0, expired = 0;
            effects.Started += _ => started++;
            effects.Expired += _ => expired++;
            Assert.AreEqual(1, effects.VehiclePowerMultiplier);
            Assert.IsTrue(effects.Activate(TruckTaxiTemporaryEffectKind.HighOctaneBoost));
            effects.Tick(.5f);
            float boost = effects.VehiclePowerMultiplier;
            Assert.That(boost, Is.GreaterThan(1.5f));
            effects.Tick(5);
            effects.Activate(TruckTaxiTemporaryEffectKind.HighOctaneBoost);
            Assert.AreEqual(1, started, "Refreshing an active effect is not another stack.");
            Assert.AreEqual(12, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.HighOctaneBoost).RemainingSeconds);
            effects.Tick(.5f);
            Assert.AreEqual(boost, effects.VehiclePowerMultiplier, .001f);
            effects.Activate(TruckTaxiTemporaryEffectKind.EnergyDrink);
            effects.Tick(1);
            Assert.AreEqual(1.7f, effects.VehiclePowerMultiplier, .001f, "Distinct boosts use the strongest value.");
            effects.Tick(14);
            Assert.AreEqual(0, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.HighOctaneBoost).Progress01);
            Assert.AreEqual(1, effects.VehiclePowerMultiplier, "NWH callback has an exact neutral value after expiry.");
            Assert.AreEqual(0, effects.ActiveCount);
            Assert.AreEqual(2, expired);
        }

        [Test]
        public void MushroomEnvelopeFadesAndExposesUsableUrpProfile()
        {
            var effects = new TruckTaxiTemporaryEffects();
            effects.SetShortDebugDurations(true);
            effects.Activate(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            var start = effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            Assert.AreEqual(0, start.Intensity01);
            Assert.Greater(start.Profile.Saturation, 50);
            Assert.Greater(start.Profile.Contrast, 0);
            Assert.Greater(start.Profile.ChromaticAberration, .1f);
            effects.Tick(1);
            Assert.AreEqual(.5f, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Intensity01, .001f);
            effects.Tick(3);
            Assert.AreEqual(1, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Intensity01);
            effects.Tick(12);
            Assert.AreEqual(.5f, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Intensity01, .001f);
            effects.Tick(2);
            Assert.AreEqual(0, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Intensity01);
        }

        [Test]
        public void DrinksDoNotChangeLibidoAndAdultHookSatisfiesIt()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 11);
                state.AdvanceGameSeconds(3600);
                float thirst = state.Thirst;
                foreach (var drink in new[] { TruckTaxiNeedsItem.WaterBottle, TruckTaxiNeedsItem.Soda, TruckTaxiNeedsItem.EnergyDrink })
                {
                    Assert.IsTrue(state.AddItem(drink));
                    Assert.IsTrue(state.Consume(drink));
                    Assert.AreEqual(thirst, state.Thirst);
                }
                Assert.Greater(state.Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.EnergyDrink).RemainingSeconds, 0);
                state.SatisfyThirst();
                Assert.AreEqual(0, state.Thirst);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void DeepSnowReducesLateralGripMoreThanDriveGripWithoutLargeDrag()
        {
            float longitudinal = TruckTaxiSnowTraction.GripForDepth(.6f, .6f, .56f);
            float lateral = TruckTaxiSnowTraction.GripForDepth(.6f, .6f, .36f);
            float rolling = TruckTaxiSnowTraction.RollingForDepth(.6f, .6f);
            Assert.That(lateral, Is.LessThan(longitudinal));
            Assert.That(rolling, Is.InRange(1.1f, 1.25f));
            Assert.AreEqual(1, TruckTaxiSnowTraction.RollingForDepth(0, .6f));
            Assert.AreEqual(1, TruckTaxiSnowTraction.GripForDepth(0, .6f));
        }
    }
}
