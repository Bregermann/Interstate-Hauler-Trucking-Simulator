using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiNeedsSleepTests
    {
        private TruckTaxiEnvironmentSettings settings;
        [SetUp] public void Setup() { settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>(); settings.startingBladder = 0; }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(settings);
        private TruckTaxiDriverNeedsState State() => new TruckTaxiDriverNeedsState(settings, 29);

        [Test] public void RegistryHasStableUniqueIdsAndAllShopsShareEveryBuyableItem()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            Assert.AreEqual(Enum.GetValues(typeof(TruckTaxiNeedsItem)).Length, TruckTaxiNeedsItems.All.Length);
            var go = new GameObject("Ordinary store");
            try
            {
                var store = go.AddComponent<TruckTaxiStorePoint>(); store.sellsRoadPep = false;
                foreach (var entry in TruckTaxiNeedsItems.All)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry.StableId)); Assert.IsTrue(ids.Add(entry.StableId));
                    Assert.AreSame(entry, TruckTaxiNeedsItems.FindByStableId(entry.StableId));
                    Assert.AreEqual(entry.PlayerOwnable && entry.Purchasable, store.Sells(entry.Item));
                }
                Assert.IsTrue(store.Sells(TruckTaxiNeedsItem.RoadPep));
                Assert.IsFalse(store.Sells(TruckTaxiNeedsItem.FilledJug));
                Assert.AreEqual(15, TruckTaxiNeedsItems.Store.Length);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [TestCase(TruckTaxiItemFlags.NonInventory)] [TestCase(TruckTaxiItemFlags.DebugOnly)] [TestCase(TruckTaxiItemFlags.NotPlayerOwnable)]
        public void ExplicitExclusionFlagsPreventOwnership(TruckTaxiItemFlags flag)
        { Assert.IsFalse(new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Snack, "Excluded", 0, flags: flag).PlayerOwnable); }

        [Test] public void NewInventoryIsExactTenIdempotentAndRestoredStateNeverGrants()
        {
            var state = State(); Assert.IsFalse(state.StarterInventoryGranted);
            Assert.IsTrue(state.CreateNewVibinInventory()); Assert.IsFalse(state.CreateNewVibinInventory());
            foreach (var entry in TruckTaxiNeedsItems.All) Assert.AreEqual(10, state.Count(entry.Item));
            Assert.IsTrue(state.TryConsumeItem("water-bottle", 4));
            var snapshot = state.CapturePersistentState();
            var restored = State(); Assert.IsTrue(restored.RestorePersistentState(snapshot, out var error), error);
            restored.GrantFreePlayTestInventory(); Assert.AreEqual(6, restored.Count(TruckTaxiNeedsItem.WaterBottle));
            snapshot.StarterInventoryGranted = false;
            Assert.IsTrue(restored.RestorePersistentState(snapshot, out error), error);
            Assert.IsFalse(restored.CreateNewVibinInventory()); Assert.IsFalse(restored.StarterInventoryGranted);
            Assert.AreEqual(6, restored.Count(TruckTaxiNeedsItem.WaterBottle));
        }
        [Test] public void GenericDebitIsAtomicAndZeroOnlyValidates()
        {
            var state = State();
            Assert.IsTrue(state.TryConsumeItem("water-bottle", 0));
            Assert.IsFalse(state.TryConsumeItem("unknown", 0)); Assert.IsFalse(state.TryConsumeItem("water-bottle", -1));
            state.AddItem(TruckTaxiNeedsItem.WaterBottle, 4);
            Assert.IsFalse(state.TryConsumeItem("water-bottle", 5)); Assert.AreEqual(4, state.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.IsTrue(state.TryConsumeItem("water-bottle", 3)); Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(0, state.Count(TruckTaxiNeedsItem.EmptyBottle));
            state.SetPressure(.9f); Assert.IsTrue(state.StartJug());
            Assert.IsFalse(state.TryConsumeItem("water-bottle", 0)); Assert.IsFalse(state.TryConsumeItem("water-bottle", 1));
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.WaterBottle));
        }
        [TestCase(TruckTaxiNeedsItem.WaterBottle, .95f, .90f)]
        [TestCase(TruckTaxiNeedsItem.Soda, .52f, .47f)]
        [TestCase(TruckTaxiNeedsItem.EnergyDrink, .03f, 0f)]
        public void EveryDrinkRemovesExactlyFivePoints(TruckTaxiNeedsItem drink, float before, float after)
        {
            var state = State(); var snapshot = state.CapturePersistentState(); snapshot.Thirst = before;
            Assert.IsTrue(state.RestorePersistentState(snapshot, out _)); state.AddItem(drink);
            Assert.IsTrue(state.Consume(drink)); Assert.AreEqual(after, state.Thirst, .00001f);
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.EmptyBottle));
            if (drink == TruckTaxiNeedsItem.EnergyDrink) Assert.Greater(state.Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.EnergyDrink).RemainingSeconds, 0);
        }
        [Test] public void SnapshotRoundTripsExactValuesAndOrderIndependentStableItems()
        {
            var source = State(); source.CreateNewVibinInventory(); source.TryConsumeItem("snack", 7);
            var data = source.CapturePersistentState();
            data.Pressure = .37f; data.Hunger = .41f; data.Thirst = .93f; data.Fatigue = .876f; data.CabMess = true;
            Array.Reverse(data.Inventory);
            // JSON here is only an in-memory DTO compatibility test, not a new save backend.
            data = JsonUtility.FromJson<TruckTaxiDriverNeedsSnapshot>(JsonUtility.ToJson(data));
            var target = State(); Assert.IsTrue(target.RestorePersistentState(data, out var error), error);
            Assert.AreEqual(data.Pressure, target.Pressure); Assert.AreEqual(data.Hunger, target.Hunger);
            Assert.AreEqual(data.Thirst, target.Thirst); Assert.AreEqual(data.Fatigue, target.Fatigue);
            Assert.IsTrue(target.CabMess); Assert.IsTrue(target.StarterInventoryGranted); Assert.AreEqual(3, target.Count(TruckTaxiNeedsItem.Snack));
            Assert.AreEqual(source.CycleGameHours, target.CycleGameHours);
            data.Inventory[0].Count = 0;
            Assert.AreEqual(10, target.Count(TruckTaxiNeedsItems.Find(data.Inventory[0].ItemId).Item));
        }
        [TestCase("duplicate")] [TestCase("unknown")] [TestCase("negative")] [TestCase("overflow")] [TestCase("nan")] [TestCase("version")]
        public void InvalidSnapshotDoesNotPartiallyMutateState(string corruption)
        {
            var state = State(); state.CreateNewVibinInventory(); var data = state.CapturePersistentState();
            data.Pressure = .7f;
            if (corruption == "duplicate") data.Inventory[1].ItemId = data.Inventory[0].ItemId;
            if (corruption == "unknown") data.Inventory[0].ItemId = "removed-item";
            if (corruption == "negative") data.Inventory[0].Count = -1;
            if (corruption == "overflow") data.Inventory[0].Count = 100;
            if (corruption == "nan") data.Fatigue = float.NaN;
            if (corruption == "version") data.Version = 999;
            Assert.IsFalse(state.RestorePersistentState(data, out var error)); Assert.IsNotEmpty(error);
            Assert.AreEqual(0, state.Pressure); Assert.AreEqual(0, state.Fatigue); Assert.AreEqual(10, state.Count(TruckTaxiNeedsItem.WaterBottle));
        }
        [Test] public void CaptureDoesNotPersistAnActiveJugAndRetainsReservedContainer()
        {
            var state = State(); state.SetPressure(.9f); state.StartJug();
            var target = State(); Assert.IsTrue(target.RestorePersistentState(state.CapturePersistentState(), out _));
            Assert.IsFalse(target.JugActive); Assert.AreEqual(1, target.Count(TruckTaxiNeedsItem.EmptyPissJug));
            Assert.IsTrue(state.JugActive);
        }
        [TestCase(TruckTaxiFatigueActivity.Parked, 19f, 20.1f)]
        [TestCase(TruckTaxiFatigueActivity.Driving, 17f, 19f)]
        [TestCase(TruckTaxiFatigueActivity.Stressful, 16f, 17f)]
        public void FullCircadianShiftReachesEightyPercentOnTarget(TruckTaxiFatigueActivity activity, float min, float max)
        {
            var state = State(); float elapsedHours = 0;
            while (state.Fatigue < .8f && elapsedHours < 24)
            { state.AdvanceGameSeconds(60, activity, (8 + elapsedHours) % 24); elapsedHours += 1f / 60; }
            Assert.That(elapsedHours, Is.InRange(min, max));
        }
        [TestCase(TruckTaxiFatigueActivity.Parked, .04f)] [TestCase(TruckTaxiFatigueActivity.Driving, .044f)]
        [TestCase(TruckTaxiFatigueActivity.Stressful, .0485f)]
        public void DayAndNightRatesStayModest(TruckTaxiFatigueActivity activity, float hourly)
        {
            Assert.AreEqual(hourly, TruckTaxiFatigueRules.Gain(3600, activity, 12), .00001f);
            Assert.AreEqual(hourly * 1.1f, TruckTaxiFatigueRules.Gain(3600, activity, 1), .00001f);
            float split = TruckTaxiFatigueRules.Gain(3600, activity, 23) + TruckTaxiFatigueRules.Gain(3600, activity, 0);
            Assert.AreEqual(split, TruckTaxiFatigueRules.Gain(7200, activity, 23), .00001f);
        }
        [Test] public void InvalidElapsedTimeNeverChangesFatigueAndLongTimeClamps()
        {
            var state = State(); state.AdvanceGameSeconds(double.NaN); state.AdvanceGameSeconds(double.PositiveInfinity); state.AdvanceGameSeconds(-1);
            Assert.AreEqual(0, state.Fatigue); state.AdvanceGameSeconds(100 * 3600); Assert.AreEqual(1, state.Fatigue);
        }
        [Test] public void StimulantsSlowGainWithoutErasingExhaustionAndOveruseStillNeedsSleep()
        {
            var state = State(); var data = state.CapturePersistentState(); data.Fatigue = .9f; state.RestorePersistentState(data, out _);
            state.Effects.Activate(TruckTaxiTemporaryEffectKind.EnergyDrink); float energy = state.FatigueGainMultiplier;
            Assert.That(energy, Is.InRange(.75f, .9f)); state.Effects.Clear(); state.CompleteRoadPep(100);
            Assert.Less(state.FatigueGainMultiplier, energy); Assert.AreEqual(.9f, state.Fatigue);
            for (int i = 0; i < 4; i++) state.CompleteRoadPep(100);
            Assert.GreaterOrEqual(state.FatigueGainMultiplier, .8f);
            state.TickArcadeEffects(600); Assert.Greater(state.FatigueGainMultiplier, 1);
            state.TickArcadeEffects(211); Assert.AreEqual(1, state.FatigueGainMultiplier);
        }
        [TestCase(2, .75f)] [TestCase(4, .45f)] [TestCase(8, 0f)]
        public void SleepRecoversFatigueAndProgressesOtherNeeds(int hours, float expected)
        {
            var state = State(); var data = state.CapturePersistentState(); data.Fatigue = 1; state.RestorePersistentState(data, out _);
            state.CompleteRoadPep(100); Assert.IsTrue(state.ApplySleep(hours));
            Assert.AreEqual(expected, state.Fatigue, .0001f); Assert.Greater(state.Hunger, 0); Assert.Greater(state.Thirst, 0);
            Assert.AreEqual(0, state.Effects.ActiveCount);
        }
        [Test] public void SleepRejectsUnsupportedDurationsAndFareStates()
        {
            var state = State(); Assert.IsFalse(state.ApplySleep(3)); Assert.AreEqual(0, state.Hunger);
            foreach (TruckTaxiState phase in Enum.GetValues(typeof(TruckTaxiState)))
                Assert.AreEqual(phase == TruckTaxiState.Available, TruckTaxiFatigueRules.CanSleepInSession(phase), phase.ToString());
            state.SetPressure(.9f); state.StartJug(); Assert.IsFalse(state.ApplySleep(2));
        }
        [Test] public void SleepUsesExistingClockAcrossMidnightEvenWhenMenuPaused()
        {
            var clock = new LwsGameClockService(); clock.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            clock.SetDateTime(new LwsGameDateTime(2026, 9, 30, 23, 0, 0)); clock.SetPaused(true);
            double before = clock.CurrentSnapshot.totalGameSeconds; clock.AddHours(8);
            Assert.AreEqual(1, clock.CurrentSnapshot.day); Assert.AreEqual(10, clock.CurrentSnapshot.month);
            Assert.AreEqual(7, clock.CurrentSnapshot.hour); Assert.IsTrue(clock.CurrentSnapshot.paused);
            Assert.AreEqual(8 * 3600, clock.CurrentSnapshot.totalGameSeconds - before);
        }
        [Test] public void SleepAuthoringIsIdempotentPreservesOtherCapabilitiesAndUsesRealAnchor()
        {
            var go = new GameObject("Hotel");
            try
            {
                var location = go.AddComponent<TruckTaxiRideLocation>(); location.locationType = TaxiLocationType.Hotel; location.locationId = "test.hotel";
                Assert.Throws<ArgumentException>(() => TruckTaxiSleepAuthoring.ConfigureSleepLocation(location));
                var anchor = new GameObject("Parking anchor"); anchor.transform.SetParent(go.transform); anchor.transform.position = new Vector3(20, 0, 10);
                location.truckStopPoint = anchor.transform;
                var point = go.AddComponent<TruckTaxiServicePoint>(); point.stableId = "existing.hotel"; point.capabilities = TruckTaxiServiceCapability.Restroom;
                TruckTaxiSleepAuthoring.ConfigureSleepLocation(location, 500); TruckTaxiSleepAuthoring.ConfigureSleepLocation(location, 500);
                Assert.AreEqual(1, go.GetComponents<TruckTaxiServicePoint>().Length); Assert.AreEqual("existing.hotel", point.stableId);
                Assert.IsTrue(point.Supports(TruckTaxiServiceCapability.Sleep | TruckTaxiServiceCapability.Restroom));
                Assert.AreEqual(anchor.transform.position, point.Position); Assert.AreEqual(4000, point.SleepCostCents(8));
                Assert.IsTrue(point.CanUse(point.Position, 0, .44704f)); Assert.IsFalse(point.CanUse(point.Position, 3, .44704f));
                Assert.IsFalse(point.CanUse(point.Position + Vector3.up * 8, 0, .44704f));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [TestCase("taxi.stop.07", TaxiLocationType.Hotel, false)]
        [TestCase("taxi.stop.19", TaxiLocationType.Hotel, false)]
        [TestCase("taxi.town02.stop.07", TaxiLocationType.Hotel, false)]
        [TestCase("taxi.town02.stop.19", TaxiLocationType.Hotel, false)]
        [TestCase("taxi.stop.08", TaxiLocationType.Hotel, true)]
        [TestCase("taxi.stop.15", TaxiLocationType.Hotel, true)]
        [TestCase("taxi.town02.stop.08", TaxiLocationType.Hotel, true)]
        [TestCase("taxi.town02.stop.15", TaxiLocationType.Hotel, true)]
        [TestCase("designer.hotel", TaxiLocationType.Hotel, true)]
        public void HotelAuthoringDoesNotTreatLegacyCycledTypesAsLodging(string id, TaxiLocationType type, bool expected)
        {
            var go = new GameObject("Authoring fixture");
            try
            {
                var location = go.AddComponent<TruckTaxiRideLocation>();
                location.locationId = id; location.locationType = type;
                Assert.AreEqual(expected, TruckTaxiSleepAuthoring.IsAuthoredHotel(location));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void VisualCurveEscalatesWithoutFastStrobing()
        {
            Assert.AreEqual(0, TruckTaxiFatigueRules.VisualSeverity(.79f));
            Assert.Less(TruckTaxiFatigueRules.VisualSeverity(.9f), TruckTaxiFatigueRules.VisualSeverity(.95f));
            Assert.Less(TruckTaxiFatigueRules.MinimumEffectInterval(1), TruckTaxiFatigueRules.MinimumEffectInterval(.8f));
            Assert.GreaterOrEqual(TruckTaxiFatigueRules.MinimumEffectInterval(1), 8);
        }
    }
}
