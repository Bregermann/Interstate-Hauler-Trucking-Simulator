using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiFuelTests
    {
        [Test] public void RescueCostsDoubleNormalFullTank()
        { Assert.AreEqual(24000,TruckTaxiFuelController.RescueCost(80,150)); }
        [Test] public void RefuelRequiresStationarySameLevelInsideBay()
        {
            var go=new GameObject("Fuel test");
            try
            {
                var station=go.AddComponent<TruckTaxiGasStationPoint>(); station.stableId="test.gas";
                Assert.IsTrue(station.CanRefuel(Vector3.zero,.4f));
                Assert.IsFalse(station.CanRefuel(Vector3.zero,1));
                Assert.IsFalse(station.CanRefuel(Vector3.up*20,0));
                Assert.IsFalse(station.CanRefuel(Vector3.right*20,0));
                Assert.IsFalse(station.CanRefuel(Vector3.zero,float.NaN));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void NamedPatienceLevelsPreserveNormalAndOrdering()
        {
            var passenger=ScriptableObject.CreateInstance<PassengerProfile>();
            try
            {
                Assert.AreEqual(1,passenger.EffectivePickupPatience);
                passenger.pickupPatience=TruckTaxiPickupPatience.VeryImpatient;
                Assert.Less(passenger.EffectivePickupPatience,1);
                passenger.pickupPatience=TruckTaxiPickupPatience.VeryPatient;
                Assert.Greater(passenger.EffectivePickupPatience,1);
            }
            finally { Object.DestroyImmediate(passenger); }
        }
    }
}
