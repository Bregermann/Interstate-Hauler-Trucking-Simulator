using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiNeedsItemsTests
    {
        private TruckTaxiEnvironmentSettings settings;
        [SetUp] public void Setup() { settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>(); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(settings); }
        private TruckTaxiDriverNeedsState NewState(float pressure = .9f)
        { settings.startingBladder = pressure; return new TruckTaxiDriverNeedsState(settings, 42); }

        [Test] public void ThirstRisesFasterThanHungerAndInvalidTimeDoesNothing()
        {
            var state = NewState(0);
            state.AdvanceGameSeconds(3600);
            Assert.Greater(state.Thirst, state.Hunger);
            float hunger = state.Hunger, thirst = state.Thirst;
            state.AdvanceGameSeconds(double.NaN); state.AdvanceGameSeconds(-1);
            Assert.AreEqual(hunger, state.Hunger); Assert.AreEqual(thirst, state.Thirst);
        }
        [Test] public void BottledDrinkProducesExactlyOneEmptyBottle()
        {
            var state = NewState(0);
            Assert.IsTrue(state.AddItem(TruckTaxiNeedsItem.WaterBottle));
            Assert.IsTrue(state.Consume(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(0, state.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.EmptyBottle));
            Assert.IsFalse(state.Consume(TruckTaxiNeedsItem.WaterBottle));
        }
        [Test] public void JugReservesOneContainerAndCancelReturnsIt()
        {
            var state = NewState();
            Assert.IsTrue(state.StartJug());
            Assert.AreEqual(0, state.Count(TruckTaxiNeedsItem.EmptyPissJug));
            Assert.IsFalse(state.StartJug());
            state.CancelJug(); state.CancelJug();
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.EmptyPissJug));
            Assert.IsFalse(state.FilledJug);
        }
        [Test] public void BottleQteSuccessThrowAndDisposalDoNotDuplicateContainers()
        {
            var state = NewState();
            Assert.IsTrue(state.StartJug());
            state.FinishJug(true);
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.FilledJug));
            Assert.IsTrue(state.DisposeJug(true));
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.EmptyPissJug));
            state.SetPressure(.9f);
            Assert.IsTrue(state.StartJug()); state.FinishJug(false);
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.EmptyPissJug));
            Assert.AreEqual(0, state.Count(TruckTaxiNeedsItem.FilledJug));
            Assert.IsTrue(state.AddItem(TruckTaxiNeedsItem.EmptyBottle));
            Assert.IsTrue(state.StartJug()); state.FinishJug(true);
            state.SetPressure(.9f);
            Assert.IsTrue(state.StartJug()); state.FinishJug(true);
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.FilledJug));
            Assert.AreEqual(1, state.Count(TruckTaxiNeedsItem.FilledBottle));
            Assert.IsTrue(state.ThrowFilled(TruckTaxiNeedsItem.FilledBottle));
            Assert.IsFalse(state.ThrowFilled(TruckTaxiNeedsItem.FilledBottle));
            Assert.AreEqual(1, state.BottlesThrown);
        }
        [Test] public void StoreRequiresStationaryTruckInsideBay()
        {
            var go = new GameObject("Store");
            try
            {
                var point = go.AddComponent<TruckTaxiStorePoint>();
                point.stableId = "TT_STORE_test";
                Assert.IsTrue(point.CanUse(Vector3.zero, 0, .447f));
                Assert.IsFalse(point.CanUse(Vector3.zero, 2, .447f));
                Assert.IsFalse(point.CanUse(Vector3.right * 30, 0, .447f));
                Assert.IsFalse(point.CanUse(Vector3.up * 5, 0, .447f));
                Assert.IsFalse(point.CanUse(Vector3.zero, float.NaN, .447f));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
