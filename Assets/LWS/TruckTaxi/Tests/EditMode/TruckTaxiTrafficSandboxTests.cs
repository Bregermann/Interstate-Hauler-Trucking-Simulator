using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiTrafficSandboxTests
    {
        [Test]
        public void PedestrianAreaRejectsRoadCenterlineAndOutsidePoint()
        {
            var root = new GameObject("Pedestrian area test");
            try
            {
                var area = root.AddComponent<TruckTaxiPedestrianArea>();
                area.Configure("TT_PEDAREA_Test", new Vector2(20, 16), 4);
                var lane = new LwsTrafficLaneDefinition { centerline = new[] {
                    new Vector3(-12, 0, 0), new Vector3(12, 0, 0) } };
                Assert.IsFalse(area.Allows(new Vector3(0, 0, 2), new[] { lane }));
                Assert.IsTrue(area.Allows(new Vector3(0, 0, 7), new[] { lane }));
                Assert.IsFalse(area.Allows(new Vector3(11, 0, 7), new[] { lane }));
                Assert.AreEqual("TT_PEDAREA_Test", area.areaId);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void IntersectionRequiresActualVendorReferences()
        {
            var root = new GameObject("Intersection test");
            try
            {
                var intersection = root.AddComponent<TruckTaxiIntersection>();
                intersection.Configure("TT_TRAFFICLIGHT_Test", null, null, null);
                Assert.IsFalse(intersection.IsConfigured);
                Assert.IsFalse(intersection.VehicleMayProceed);
                Assert.IsFalse(intersection.PedestrianMayCross);
                intersection.Configure("TT_TRAFFICLIGHT_Test", null, null, null);
                Assert.AreEqual("TT_TRAFFICLIGHT_Test", intersection.intersectionId);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
