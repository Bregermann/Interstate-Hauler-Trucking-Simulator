using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiCorrectionTests
    {
        [Test] public void StarterInventoryIsCompleteOncePerFreePlaySession()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 3);
                state.GrantFreePlayTestInventory(); state.GrantFreePlayTestInventory();
                foreach (var entry in TruckTaxiNeedsItems.Store) Assert.AreEqual(1, state.Count(entry.Item), entry.Name);
                state.Consume(TruckTaxiNeedsItem.WaterBottle); state.GrantFreePlayTestInventory();
                Assert.AreEqual(0, state.Count(TruckTaxiNeedsItem.WaterBottle));
                state.AdvanceGameSeconds(9 * 3600);
                Assert.AreEqual(1, state.Hunger); Assert.AreEqual(1, state.Thirst);
                Assert.AreEqual("VEHICLE OK", TruckTaxiRoadsideAssistance.Diagnose(1, true, true, 0, 0, 0, 0, 0, 1, 0));
            }
            finally { Object.DestroyImmediate(settings); }
        }
        [TestCase(0, true, true, 0, 0, 0, 0, 0, 1, 0, "OUT OF FUEL")]
        [TestCase(1, true, true, 1, 0, 0, 0, 0, 1, 0, "ENGINE DAMAGED")]
        [TestCase(1, true, true, 0, 1, 0, 0, 0, 1, 0, "TRANSMISSION DAMAGED")]
        [TestCase(1, false, false, 0, 0, 0, 0, 0, 1, 0, "ENGINE DISABLED")]
        [TestCase(1, true, true, 0, 0, 0, 1, 0, 1, 0, "PARKING BRAKE")]
        [TestCase(1, true, true, 0, 0, 0, 0, 0, 1, .6f, "DEEP SNOW")]
        public void ConditionUsesAuthoritativeState(float fuel, bool enabled, bool running, float engineDamage,
            float transDamage, float damage, float park, float brake, int gear, float snow, string reason)
        { StringAssert.StartsWith(reason, TruckTaxiRoadsideAssistance.Diagnose(fuel, enabled, running, engineDamage, transDamage, damage, park, brake, gear, snow)); }
        [Test] public void CapabilitiesAreIndependentAndNearestIsDeterministic()
        {
            var a = new GameObject("store"); var b = new GameObject("repair");
            try
            {
                var store = a.AddComponent<TruckTaxiServicePoint>(); store.stableId = "a";
                store.capabilities = TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Restroom | TruckTaxiServiceCapability.Food;
                var repair = b.AddComponent<TruckTaxiServicePoint>(); repair.stableId = "b";
                repair.capabilities = TruckTaxiServiceCapability.RepairGeneralDamage | TruckTaxiServiceCapability.RecoverVehicle;
                // Ordinary MonoBehaviours do not receive runtime OnEnable in EditMode.
                Lifecycle(store, "OnEnable"); Lifecycle(repair, "OnEnable");
                Assert.IsFalse(store.Supports(TruckTaxiServiceCapability.DisposeWaste));
                Assert.IsFalse(store.Supports(TruckTaxiServiceCapability.Drink));
                Assert.AreSame(store, TruckTaxiServicePoint.Nearest(Vector3.zero,
                    TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink, a.scene, true));
                Assert.AreSame(store, TruckTaxiServicePoint.Nearest(Vector3.zero, TruckTaxiServiceCapability.Restroom, a.scene));
                Assert.AreSame(repair, TruckTaxiServicePoint.Nearest(Vector3.zero, TruckTaxiServiceCapability.RecoverVehicle, a.scene));
                Assert.IsTrue(store.CanUse(Vector3.zero, 0, .447f));
                Assert.IsFalse(store.CanUse(Vector3.up * 10, 0, .447f));
                Assert.IsFalse(store.CanUse(Vector3.zero, 3, .447f));
            }
            finally { Lifecycle(a.GetComponent<TruckTaxiServicePoint>(), "OnDisable"); Lifecycle(b.GetComponent<TruckTaxiServicePoint>(), "OnDisable"); Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }
        private static void Lifecycle(TruckTaxiServicePoint point, string method) => typeof(TruckTaxiServicePoint)
            .GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(point, null);
    }
}
