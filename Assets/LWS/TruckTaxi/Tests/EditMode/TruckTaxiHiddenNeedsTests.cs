using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiHiddenNeedsTests
    {
        private TruckTaxiEnvironmentSettings settings;
        [SetUp] public void SetUp() => settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(settings);
        private TruckTaxiDriverNeedsState State() => new TruckTaxiDriverNeedsState(settings, 42);

        [TestCase(.2f, .05f, true)]
        [TestCase(.05f, .05f, false)]
        [TestCase(.12f, .12f, false)]
        [TestCase(.200001f, .200001f, false)]
        [TestCase(0f, 0f, false)]
        public void AuthoringMigratesOnlyExactLegacyThirstRate(float before, float expected, bool migrated)
        {
            settings.thirstGainPerGameHour = before;
            Assert.AreEqual(migrated, LWS.TruckTaxi.Editor.TruckTaxiNeedsPresentationAuthoring.MigrateLegacyThirstRate(settings));
            Assert.AreEqual(expected, settings.thirstGainPerGameHour);
            Assert.IsFalse(LWS.TruckTaxi.Editor.TruckTaxiNeedsPresentationAuthoring.MigrateLegacyThirstRate(settings));
        }

        [TestCase(TruckTaxiNeedsItem.WaterBottle, .015f)]
        [TestCase(TruckTaxiNeedsItem.Soda, .04f)]
        [TestCase(TruckTaxiNeedsItem.EnergyDrink, .08f)]
        [TestCase(TruckTaxiNeedsItem.Snack, .08f)]
        [TestCase(TruckTaxiNeedsItem.Burger, .18f)]
        [TestCase(TruckTaxiNeedsItem.Sandwich, .12f)]
        [TestCase(TruckTaxiNeedsItem.Meal, .24f)]
        public void SuccessfulConsumptionAddsMetadataLoadExactlyOnce(TruckTaxiNeedsItem item, float expected)
        {
            var state = State();
            Assert.IsTrue(state.AddItem(item)); Assert.IsTrue(state.Consume(item));
            Assert.AreEqual(expected, TruckTaxiNeedsItems.Find(item).BowelLoad, .00001f);
            Assert.AreEqual(expected, state.BowelPressure, .00001f);
            Assert.IsFalse(state.Consume(item)); Assert.AreEqual(expected, state.BowelPressure, .00001f);
        }
        [Test] public void EveryRegistryFoodAndDrinkHasHiddenMetadata()
        {
            foreach (var item in TruckTaxiNeedsItems.All)
                if (item.IsDrink || item.HungerRelief > 0) Assert.That(item.BowelLoad, Is.InRange(.001f, 1f), item.StableId);
        }
        [Test] public void ServiceMealUsesItsConfiguredMetadataOnce()
        {
            var state = State(); var meal = TruckTaxiNeedsItems.Find(TruckTaxiNeedsItem.Meal);
            state.EatMeal(meal.HungerRelief, meal.BowelLoad);
            Assert.AreEqual(meal.BowelLoad, state.BowelPressure);
        }
        [Test] public void CrossingCriticalDoesNotChargePreCriticalTimeAgainstGrace()
        {
            settings.bowelGainPerGameHour = 1; settings.bowelGraceGameMinutes = 5;
            var state = State(); state.DebugSetBowelPercent(50);
            state.AdvanceGameSeconds(1800 + 120);
            Assert.AreEqual(1, state.BowelPressure);
            Assert.AreEqual(120, state.CapturePersistentState().BowelCrisisGameSeconds, .001f);
            Assert.AreEqual(0, state.BowelAccidents);
            state.AdvanceGameSeconds(179); Assert.AreEqual(0, state.BowelAccidents);
            state.AdvanceGameSeconds(1); Assert.AreEqual(1, state.BowelAccidents);
            Assert.AreEqual(.15f, state.BowelPressure); Assert.IsTrue(state.CabMess);
            state.AdvanceGameSeconds(1); Assert.AreEqual(1, state.BowelAccidents);
        }
        [Test] public void ToiletRelievesBothNeedsAndDiscomfortButNotTheAccidentHistory()
        {
            var state = State(); state.DebugForceBowelFailure(); state.DebugSetBowelPercent(99); state.SetPressure(.9f);
            Assert.IsFalse(state.UseBathroom(false)); Assert.AreEqual(.99f, state.BowelPressure, .00001f);
            Assert.IsTrue(state.UseBathroom(true));
            Assert.AreEqual(0, state.BowelPressure); Assert.AreEqual(0, state.Pressure);
            Assert.AreEqual(0, state.BowelDiscomfortGameSeconds); Assert.AreEqual(1, state.BowelAccidents);
            Assert.AreEqual(0, state.CapturePersistentState().BowelCrisisGameSeconds);
        }
        [Test] public void ForceFailureUsesTheSameEventAndAdditiveSnapshotRoundTrips()
        {
            var state = State(); int events = 0;
            state.Event += kind => { if (kind == TruckTaxiDriverNeedEvent.BowelAccident) events++; };
            state.DebugForceBowelFailure(); Assert.AreEqual(1, events);
            var restored = State(); var payload = state.CapturePersistentState();
            Assert.AreEqual(1, payload.Version);
            Assert.IsTrue(restored.RestorePersistentState(payload, out var error), error);
            Assert.AreEqual(1, restored.BowelAccidents); Assert.AreEqual(1800, restored.BowelDiscomfortGameSeconds);
            restored.AdvanceGameSeconds(1800); Assert.AreEqual(0, restored.BowelDiscomfortGameSeconds);
            payload.BowelAccidents = 0; payload.BowelDiscomfortGameSeconds = 0;
            Assert.IsTrue(restored.RestorePersistentState(payload, out error), error);
            Assert.AreEqual(0, restored.BowelAccidents);
        }
        [Test] public void InvalidAdditiveSnapshotDoesNotPartiallyOverwriteState()
        {
            var state = State(); state.DebugSetBowelPercent(75);
            var payload = state.CapturePersistentState(); payload.BowelPressure = .1f; payload.BowelDiscomfortGameSeconds = float.NaN;
            Assert.IsFalse(state.RestorePersistentState(payload, out _)); Assert.AreEqual(.75f, state.BowelPressure);
        }
        [TestCase(0)] [TestCase(25)] [TestCase(50)] [TestCase(75)] [TestCase(90)] [TestCase(99)]
        public void DebugPercentIsAnAbsoluteValueAndResetsGrace(float percent)
        {
            var state = State(); state.DebugSetBowelPercent(percent);
            Assert.AreEqual(percent / 100, state.BowelPressure);
            state.DebugSetBowelPercent(float.NaN); Assert.AreEqual(percent / 100, state.BowelPressure);
            Assert.AreEqual(0, state.CapturePersistentState().BowelCrisisGameSeconds);
        }
        [TestCase(2)] [TestCase(4)] [TestCase(8)]
        public void SleepAndElapsedTimeUseTheSameSingleCoreNeedsRate(int hours)
        {
            var sleep = State(); var awake = State();
            Assert.IsTrue(sleep.ApplySleep(hours)); awake.AdvanceGameSeconds(hours * 3600);
            Assert.AreEqual(hours * .05f, sleep.Thirst, .00001f);
            Assert.AreEqual(awake.Thirst, sleep.Thirst, .00001f);
            Assert.AreEqual(awake.BowelPressure, sleep.BowelPressure, .00001f);
            float before = awake.Thirst; awake.TickArcadeEffects(3600);
            Assert.AreEqual(before, awake.Thirst, "Real-time presentation/effect ticks must not also advance core needs.");
        }
        [Test] public void PresentationCapacityAndThresholdAreBounded()
        {
            Assert.AreEqual(0, TruckTaxiNeedsPresentation.Severity(.89f, .9f));
            Assert.AreEqual(1, TruckTaxiNeedsPresentation.Severity(1, .9f));
            Assert.AreEqual(0, TruckTaxiNeedsPresentation.ConcurrentImages(float.NaN, 3));
            Assert.AreEqual(0, TruckTaxiNeedsPresentation.ConcurrentImages(0, 3));
            Assert.AreEqual(1, TruckTaxiNeedsPresentation.ConcurrentImages(.1f, 3));
            Assert.AreEqual(3, TruckTaxiNeedsPresentation.ConcurrentImages(1, 3));
            Assert.AreEqual(4, TruckTaxiNeedsPresentation.ConcurrentImages(1, 999));
        }
        [Test] public void ConfiguredPoolsDiscardNullsAndDuplicatesWithoutMixingNeeds()
        {
            var food = new Texture2D(2, 2); var drink = new Texture2D(2, 2);
            try
            {
                CollectionAssert.AreEqual(new[] { food }, TruckTaxiNeedsPresentation.ResolvePool(new[] { food, null, food }, null, "Hunger", "Pizza"));
                CollectionAssert.AreEqual(new[] { drink }, TruckTaxiNeedsPresentation.ResolvePool(new[] { drink }, null, "Thirst", "Soda"));
            }
            finally { Object.DestroyImmediate(food); Object.DestroyImmediate(drink); }
        }
        [Test] public void PausedFailureSurvivesPresentationHideButNotLoadReset()
        {
            var go = new GameObject("Needs presentation queue fixture");
            try
            {
                var presentation = go.AddComponent<TruckTaxiNeedsPresentation>();
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(TruckTaxiNeedsPresentation).GetMethod("OnNeedEvent", flags).Invoke(presentation,
                    new object[] { TruckTaxiDriverNeedEvent.BowelAccident });
                var pending = typeof(TruckTaxiNeedsPresentation).GetField("pendingFailure", flags);
                presentation.ClearPresentation(); Assert.IsTrue((bool)pending.GetValue(presentation));
                presentation.ClearPresentation(true); Assert.IsFalse((bool)pending.GetValue(presentation));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
