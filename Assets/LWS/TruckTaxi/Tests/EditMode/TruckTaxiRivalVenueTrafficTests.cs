using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiRegional"), Category("TaxiMegaPass")]
    public sealed class TruckTaxiRivalVenueTrafficTests
    {
        [Test]
        public void VenuePressureReordersCandidatesWithoutChangingPopulationCaps()
        {
            Assert.AreEqual(0, TruckTaxiTrafficAdapter.VenuePressureFromMultiplier(1));
            Assert.AreEqual(.5f, TruckTaxiTrafficAdapter.VenuePressureFromMultiplier(1.5f));
            Assert.AreEqual(1, TruckTaxiTrafficAdapter.VenuePressureFromMultiplier(3));
            Assert.AreEqual(0, TruckTaxiTrafficAdapter.VenuePressureFromMultiplier(float.NaN));
            Assert.AreEqual(120, TruckTaxiTrafficAdapter.VenuePriorityScore(120, 0));
            Assert.AreEqual(75, TruckTaxiTrafficAdapter.VenuePriorityScore(120, .5f));
            Assert.AreEqual(30, TruckTaxiTrafficAdapter.VenuePriorityScore(120, 1));
            Assert.AreEqual(30, TruckTaxiTrafficAdapter.VenuePriorityScore(120, 2));
            Assert.AreEqual(-80, TruckTaxiTrafficAdapter.VenuePriorityScore(10, 1));

            var root = new GameObject("traffic demand test");
            try
            {
                var traffic = root.AddComponent<TruckTaxiTrafficAdapter>();
                traffic.useDensityOverride = false;
                traffic.maximumVehicles = 150;
                traffic.maxFullTraffic = 40;
                traffic.SetDemandSampler(_ => 1);
                Assert.AreEqual(150, traffic.TargetCount);
                Assert.AreEqual(40, traffic.maxFullTraffic);
                Assert.AreEqual(0, traffic.ActiveCount);
                traffic.SetDemandSampler(null);
                Assert.AreEqual(150, traffic.TargetCount);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
