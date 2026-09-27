using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LWS.InterstateHauler;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRegional")]
    public sealed class TruckTaxiPedestrianRegionalTests
    {
        [Test]
        public void LogicalPopulationPersistsAcrossUnavailableTown()
        {
            var population = new TruckTaxiPedestrianLogicalPopulation();
            population.Resize(720, Vector3.zero);
            population.RegisterRegion("Town01", new Bounds(Vector3.zero, new Vector3(640, 100, 640)));
            population.RegisterRegion("Town02", new Bounds(new Vector3(4000, 0, 0), new Vector3(640, 100, 640)));

            Assert.AreEqual(720, population.Count);
            Assert.AreEqual(360, population.Records.Count(record => record.RegionId == "Town02"));
            var town02 = population.Records.First(record => record.RegionId == "Town02");
            Assert.IsFalse(population.RegionAvailable(town02, position => position.x < 2000));
            Assert.IsFalse(population.RegionNear(town02, Vector3.zero, 170));
            Assert.IsTrue(population.RegionNear(town02, new Vector3(4000, 0, 0), 170));
            Assert.IsTrue(population.Contains(town02, new Vector3(4000, 0, 0)));
            Assert.IsFalse(population.Contains(town02, Vector3.zero));
        }

        [Test]
        public void WalkGraphSamplesOnlyCurrentTownDespiteRemotePersistentLanes()
        {
            var ground = new GameObject("Town02 ground");
            try
            {
                ground.transform.position = new Vector3(4000, -1, 0);
                ground.AddComponent<BoxCollider>().size = new Vector3(500, 2, 500);
                var lanes = new[]
                {
                    new LwsTrafficLaneDefinition { laneWidthMeters = 4,
                        centerline = new[] { new Vector3(-100, 0, 0), new Vector3(100, 0, 0) } },
                    new LwsTrafficLaneDefinition { laneWidthMeters = 4,
                        centerline = new[] { new Vector3(3900, 0, 0), new Vector3(4100, 0, 0) } }
                };
                Physics.SyncTransforms();
                var graph = new TruckTaxiPedestrianWalkGraph(lanes, null, null, null, null, null, null,
                    240, new Vector3(4000, 0, 0));
                Assert.Greater(graph.WalkableSpawnCount, 0);
                for (int i = 0; i < graph.Count; i++)
                    Assert.LessOrEqual(Mathf.Abs(graph[i].x - 4000), 240);
            }
            finally { Object.DestroyImmediate(ground); }
        }
    }
}
