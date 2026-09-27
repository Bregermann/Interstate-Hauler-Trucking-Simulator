using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiRegional")]
    public sealed class TruckTaxiTrafficPopulationTests
    {
        [Test]
        public void LogicalCarsKeepStableIdentityAndAuthoredBridgeHeight()
        {
            var lane = new LwsTrafficLaneDefinition {
                roadId = "bridge", laneId = "bridge.east.0", spawnEnabled = true, speedLimitMph = 45,
                centerline = new[] { new Vector3(0, 8, 0), new Vector3(40, 8, 0),
                    new Vector3(80, 8, 0), new Vector3(120, 8, 0), new Vector3(160, 8, 0) }
            };
            var population = new TruckTaxiTrafficPopulation(new[] { lane }, 150, 2, 12);
            Assert.AreEqual(150, population.Count);
            Assert.AreEqual("taxi.traffic.13", population.Cars[0].Id);
            Assert.AreEqual("taxi.traffic.162", population.Cars[149].Id);
            var car = population.Cars[0];
            population.Advance(car, 1);
            Assert.AreEqual(8, population.Position(car).y, .001f);
            Assert.AreEqual("bridge.east.0", car.LaneId);
            Assert.IsNull(car.Actor);
        }

        [Test]
        public void HysteresisAndLiveBudgetsAreDeterministic()
        {
            Assert.IsTrue(TruckTaxiTrafficPopulation.WantFull(175, false, 175, 35));
            Assert.IsFalse(TruckTaxiTrafficPopulation.WantFull(176, false, 175, 35));
            Assert.IsTrue(TruckTaxiTrafficPopulation.WantFull(205, true, 175, 35));
            Assert.IsFalse(TruckTaxiTrafficPopulation.WantVisible(386, true, 350, 35));
            Assert.IsFalse(TruckTaxiTrafficPopulation.CanMaterialize(80, 40, 40, 40, false));
            Assert.IsFalse(TruckTaxiTrafficPopulation.CanMaterialize(39, 40, 40, 40, true));
            Assert.IsTrue(TruckTaxiTrafficPopulation.CanMaterialize(39, 39, 40, 40, true));
        }

        [Test]
        public void PooledActorClearsMissionIdentityAndRestoresSimulationComponents()
        {
            var root = new GameObject("pooled traffic");
            try
            {
                var body = root.AddComponent<Rigidbody>();
                var collider = root.AddComponent<BoxCollider>();
                var rage = root.AddComponent<TruckTaxiRoadRage>();
                var impact = root.AddComponent<TruckTaxiImpactTarget>();
                var mission = root.AddComponent<TruckTaxiMissionTarget>();
                var actor = root.AddComponent<TruckTaxiTrafficPooledActor>();
                actor.Configure(0);
                impact.targetId = "old";
                mission.Assign("old", 3);
                actor.SetFullPhysics(false);
                Assert.IsTrue(body.isKinematic);
                Assert.IsFalse(collider.enabled);
                Assert.IsFalse(rage.enabled);
                actor.PutAway();
                Assert.IsFalse(root.activeSelf);
                Assert.IsNull(impact.targetId);
                Assert.IsNull(mission.StableId);
                actor.Place(null, 0, new Vector3(5, 8, 10), "new", false);
                Assert.AreEqual("new", impact.targetId);
                Assert.AreEqual(8, root.transform.position.y, .001f);
                actor.SetFullPhysics(true);
                Assert.IsFalse(body.isKinematic);
                Assert.IsTrue(collider.enabled);
                Assert.IsTrue(rage.enabled);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
