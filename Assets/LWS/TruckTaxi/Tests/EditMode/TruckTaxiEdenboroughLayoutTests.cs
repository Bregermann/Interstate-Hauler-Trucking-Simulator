using System.Collections.Generic;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiEdenboroughLayoutTests
    {
        [Test] public void ResumeValidationAcceptsOnlyTheExactEdenboroughWorldScenePair()
        {
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(TruckTaxiEdenboroughLayout.WorldId, TruckTaxiEdenboroughLayout.SceneName));
            Assert.IsFalse(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld("world.unknown", TruckTaxiEdenboroughLayout.SceneName));
            Assert.IsFalse(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(TruckTaxiEdenboroughLayout.WorldId, TruckTaxiMainMenu.VibinSceneName));
            Assert.IsFalse(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(TruckTaxiEdenboroughLayout.WorldId, "../TruckTaxi_Edenborough"));
            Assert.IsTrue(TruckTaxiVibinSaveParticipant.IsTaxiResumeWorld(LWS.InterstateHauler.LwsWorldResumeCatalog.TruckTaxiWorldId, TruckTaxiMainMenu.VibinSceneName));
        }
        [Test] public void AllDistrictsArePresentAndInsideTheAuthoredWorld()
        {
            Assert.That(TruckTaxiEdenboroughLayout.Districts.Length, Is.EqualTo(9));
            Assert.That(TruckTaxiEdenboroughLayout.Districts.Select(d => d.Id).Distinct().Count(), Is.EqualTo(9));
            foreach (var district in TruckTaxiEdenboroughLayout.Districts)
                Assert.That(TruckTaxiEdenboroughLayout.WorldBounds.Contains(district.Center), Is.True, district.Id);
        }
        [Test] public void SpawnGarageAndAllStreetNodesShareOneConnectedGraph()
        {
            var graph = TruckTaxiEdenboroughRoadAuthoring.CreateGraph();
            Assert.That(graph.Validate().IsValid, Is.True);
            var visited = new HashSet<string> { "spawn" }; var pending = new Queue<string>(); pending.Enqueue("spawn");
            while (pending.Count > 0)
            {
                string node = pending.Dequeue();
                foreach (var edge in graph.edges.Where(e => e.fromNodeId == node))
                    if (visited.Add(edge.toNodeId)) pending.Enqueue(edge.toNodeId);
            }
            Assert.That(visited.Count, Is.EqualTo(graph.nodes.Count));
            Assert.That(visited, Does.Contain("garage"));
        }
        [Test] public void RoadWidthsRemainTightAndHierarchyHasAllFourClasses()
        {
            var roads = TruckTaxiEdenboroughLayout.Roads;
            Assert.That(roads.Select(r => r.Kind).Distinct(), Is.EquivalentTo(new[] { "Highway", "Avenue", "CityStreet", "ServiceRoad" }));
            foreach (var road in roads)
            {
                Assert.That(road.LaneWidth, Is.InRange(3.05f, 3.45f), road.Id);
                Assert.That(road.Width, Is.LessThan(16), road.Id);
                Assert.That(road.SpeedMph, Is.InRange(15, 60));
            }
        }
        [Test] public void RoadGradesAreSmoothTruckDrivableAndFinite()
        {
            foreach (var road in TruckTaxiEdenboroughLayout.Roads)
                for (int i = 1; i < road.Points.Length; i++)
                {
                    Vector3 delta = road.Points[i] - road.Points[i - 1];
                    float distance = new Vector2(delta.x, delta.z).magnitude;
                    Assert.That(distance, Is.GreaterThan(.1f), road.Id);
                    Assert.That(Mathf.Abs(delta.y) / distance, Is.LessThan(.12f), road.Id);
                    Assert.That(TruckTaxiEdenboroughLayout.WorldBounds.Contains(road.Points[i]), Is.True, road.Id);
                }
        }
        [Test] public void HillendHasMeaningfulHeightDifferenceFromTheWaterfront()
        {
            Assert.That(TruckTaxiEdenboroughLayout.Height(-3000,-3550) - TruckTaxiEdenboroughLayout.Height(1300,2700), Is.GreaterThan(100));
        }
        [Test] public void SaveSceneRoutingOnlyAllowsExistingTaxiWorlds()
        {
            Assert.That(TruckTaxiMainMenu.IsSupportedWorld(TruckTaxiMainMenu.VibinSceneName), Is.True);
            Assert.That(TruckTaxiMainMenu.IsSupportedWorld(TruckTaxiEdenboroughLayout.SceneName), Is.True);
            Assert.That(TruckTaxiMainMenu.IsSupportedWorld("../../not-a-world"), Is.False);
            Assert.That(TruckTaxiMainMenu.IsSupportedWorld("TruckSports"), Is.False);
        }
    }
}
