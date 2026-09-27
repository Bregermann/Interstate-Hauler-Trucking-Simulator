using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRegional")]
    public sealed class TruckTaxiRegionalMapTests
    {
        [Test]
        public void RegionalMetadataRegistersWithoutWorldObjectsOrStreaming()
        {
            var owner=new GameObject("Regional map test");
            try
            {
                var map=owner.AddComponent<TruckTaxiMapMarkers>();
                var point=new Vector3(1200,0,-400);
                Assert.IsTrue(map.RegisterRegionalPoint("town.02","Town 02","Town",point,TruckTaxiMapMarkerType.SpecialEvent,true));
                Assert.IsFalse(map.RegisterRegionalPoint("town.02","Duplicate","Town",point,TruckTaxiMapMarkerType.SpecialEvent));
                Assert.AreEqual(1,map.RegionalPoints.Count);
                var saved=map.RegionalPoints.Single();
                Assert.AreEqual("Town 02",saved.Label);
                Assert.AreEqual(point,saved.Position);
                Assert.IsTrue(saved.Routeable);
                Assert.IsTrue(map.UnregisterRegionalPoint("town.02"));
                Assert.IsFalse(map.UnregisterRegionalPoint("town.02"));
                Assert.IsEmpty(map.RegionalPoints);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void OnlyServiceLikeAuthoredMarkersOfferServiceRouting()
        {
            Assert.IsTrue(TruckTaxiFullMapPresenter.IsRouteable(TruckTaxiMapMarkerType.Bathroom));
            Assert.IsTrue(TruckTaxiFullMapPresenter.IsRouteable(TruckTaxiMapMarkerType.FoodStop));
            Assert.IsFalse(TruckTaxiFullMapPresenter.IsRouteable(TruckTaxiMapMarkerType.PassengerPickup));
            Assert.IsFalse(TruckTaxiFullMapPresenter.IsRouteable(TruckTaxiMapMarkerType.Destination));
            Assert.IsFalse(TruckTaxiFullMapPresenter.IsRouteable(TruckTaxiMapMarkerType.Debug));
        }

        [Test]
        public void UninitializedMapCannotClaimGpsDestination()
        {
            var owner=new GameObject("Regional GPS test");
            try
            {
                var gps=owner.AddComponent<TruckTaxiGPSAdapter>();
                Assert.IsFalse(gps.TrySetMapServiceDestination("town.02","Town 02",Vector3.one));
                Assert.IsNull(gps.TargetId);
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
