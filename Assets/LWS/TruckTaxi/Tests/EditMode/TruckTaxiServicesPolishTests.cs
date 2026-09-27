using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPolish")]
    public sealed class TruckTaxiServicesPolishTests
    {
        [Test]
        public void StoreServiceUsesSameAuthoredBayForNavigationAndEligibility()
        {
            var go = new GameObject("Authored store bay");
            try
            {
                var location = go.AddComponent<TruckTaxiRideLocation>();
                var bay = new GameObject("Truck stop anchor").transform;
                bay.SetParent(go.transform, false);
                bay.localPosition = Vector3.right * 5;
                location.truckStopPoint = bay;
                var service = go.AddComponent<TruckTaxiServicePoint>();
                service.location = location;
                service.radius = 11;
                service.capabilities = TruckTaxiServiceCapability.Store;
                var store = go.AddComponent<TruckTaxiStorePoint>();
                store.stableId = "store.test";
                store.location = location;
                store.stopRadius = 11;
                var gas = go.AddComponent<TruckTaxiGasStationPoint>();
                Assert.AreEqual(location.StopPosition, service.Position);
                Assert.AreEqual(location.StopPosition, gas.Position);
                Assert.IsTrue(service.CanUse(bay.position + Vector3.right * 17, .44704f, 0));
                Assert.IsTrue(store.CanUse(bay.position + Vector3.right * 17, .44704f, 0));
                Assert.IsFalse(service.CanUse(bay.position + Vector3.right * 19, 0, 0));
                Assert.IsFalse(service.CanUse(bay.position, .5f, 0));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DineInRelievesHungerWithoutAddingOrConsumingInventory()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 42);
                state.AdvanceGameSeconds(8 * 3600);
                int before = state.Count(TruckTaxiNeedsItem.Meal);
                state.EatMeal(TruckTaxiNeedsItems.Find(TruckTaxiNeedsItem.Meal).HungerRelief);
                Assert.That(state.Hunger, Is.LessThan(.23f));
                Assert.AreEqual(before, state.Count(TruckTaxiNeedsItem.Meal));
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void ProductionEffectsLastFiveMinutesOrMoreAndRefreshWithoutStrengthStacking()
        {
            var effects = new TruckTaxiTemporaryEffects();
            Assert.AreEqual(360, TruckTaxiTemporaryEffects.Profile(TruckTaxiTemporaryEffectKind.MysteryMushroom).DurationSeconds);
            Assert.AreEqual(300, TruckTaxiTemporaryEffects.Profile(TruckTaxiTemporaryEffectKind.HighOctaneBoost).DurationSeconds);
            Assert.AreEqual(420, TruckTaxiTemporaryEffects.Profile(TruckTaxiTemporaryEffectKind.EnergyDrink).DurationSeconds);
            effects.Activate(TruckTaxiTemporaryEffectKind.HighOctaneBoost);
            effects.Tick(30);
            effects.Activate(TruckTaxiTemporaryEffectKind.HighOctaneBoost);
            Assert.AreEqual(300, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.HighOctaneBoost).RemainingSeconds);
            effects.Tick(1);
            Assert.That(effects.VehiclePowerMultiplier, Is.LessThanOrEqualTo(1.7f));
        }

        [Test]
        public void DevelopmentShortModeKeepsOriginalTestDuration()
        {
            var effects = new TruckTaxiTemporaryEffects();
            effects.SetShortDebugDurations(true);
            effects.Activate(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            Assert.AreEqual(18, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Profile.DurationSeconds);
        }
    }
}
