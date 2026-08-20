using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsFiftyMileHighwayEditModeTests
    {
        [Test]
        public void ExactTotalRoadLengthIsFiftyMiles()
        {
            Assert.That(LwsFiftyMileHighwayModel.MetersPerMile, Is.EqualTo(1609.344d).Within(0.0001d));
            Assert.That(LwsFiftyMileHighwayModel.TotalMiles, Is.EqualTo(50d).Within(0.0001d));
            Assert.That(LwsFiftyMileHighwayModel.TotalLengthMeters, Is.EqualTo(80467.2d).Within(0.001d));
            Assert.That(LwsFiftyMileHighwayModel.MileToMeters(50d), Is.EqualTo(80467.2d).Within(0.001d));
        }

        [Test]
        public void ChunkDefinitionsAreExactContiguousAndUnique()
        {
            IReadOnlyList<LwsWorldChunkDefinition> chunks = LwsFiftyMileHighwayModel.CreateChunkDefinitions();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            Assert.AreEqual(25, chunks.Count);
            for (int i = 0; i < chunks.Count; i++)
            {
                LwsWorldChunkDefinition chunk = chunks[i];
                double expectedStart = i * LwsFiftyMileHighwayModel.ChunkLengthMeters;
                double expectedEnd = expectedStart + LwsFiftyMileHighwayModel.ChunkLengthMeters;

                Assert.IsTrue(ids.Add(chunk.chunkId), $"Duplicate chunk ID {chunk.chunkId}");
                Assert.AreEqual($"IH_50MI_CHUNK_{i:000}", chunk.chunkId);
                Assert.That(chunk.WorldBounds.min.z, Is.EqualTo(expectedStart).Within(0.002d));
                Assert.That(chunk.WorldBounds.max.z, Is.EqualTo(expectedEnd).Within(0.002d));
                Assert.That(chunk.boundsSize.z, Is.EqualTo(LwsFiftyMileHighwayModel.ChunkLengthMeters).Within(0.002d));
                Assert.IsFalse(chunk.containsGlobalServices);
                Assert.IsFalse(chunk.containsPlayerContent);
            }

            Assert.That(chunks[0].neighborChunkIds, Is.EquivalentTo(new[] { "IH_50MI_CHUNK_001" }));
            Assert.That(chunks[24].neighborChunkIds, Is.EquivalentTo(new[] { "IH_50MI_CHUNK_023" }));
        }

        [Test]
        public void WholeMileMarkersCoverZeroThroughFifty()
        {
            List<int> markers = Enumerable
                .Range(0, LwsFiftyMileHighwayModel.ChunkCount)
                .SelectMany(LwsFiftyMileHighwayModel.EnumerateWholeMileMarkersForChunk)
                .OrderBy(m => m)
                .ToList();

            Assert.AreEqual(51, markers.Count);
            Assert.That(markers, Is.EqualTo(Enumerable.Range(0, 51)));
        }

        [Test]
        public void WeatherZonesUseGlobalMileage()
        {
            Assert.AreEqual(LwsWeatherPresetCatalog.ClearId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(0d));
            Assert.AreEqual(LwsWeatherPresetCatalog.PartlyCloudyId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(5d));
            Assert.AreEqual(LwsWeatherPresetCatalog.OvercastId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(10d));
            Assert.AreEqual(LwsWeatherPresetCatalog.LightRainId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(15d));
            Assert.AreEqual(LwsWeatherPresetCatalog.HeavyRainId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(20d));
            Assert.AreEqual(LwsWeatherPresetCatalog.ThunderstormId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(25d));
            Assert.AreEqual(LwsWeatherPresetCatalog.FogId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(30d));
            Assert.AreEqual(LwsWeatherPresetCatalog.LightSnowId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(35d));
            Assert.AreEqual(LwsWeatherPresetCatalog.HeavySnowId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(40d));
            Assert.AreEqual(LwsWeatherPresetCatalog.ClearId, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(45d));
        }

        [Test]
        public void RoadGraphIsContinuousAndRouteDistanceIsFiftyMiles()
        {
            LwsRoadGraph graph = LwsFiftyMileHighwayModel.CreateRoadGraph();
            LwsRoadGraphValidationResult validation = graph.Validate();

            Assert.IsTrue(validation.IsValid, validation.Summary);
            Assert.AreEqual(4, graph.nodes.Count);
            Assert.AreEqual(2, graph.edges.Count);

            var planner = new LwsRoutePlanner(LwsNavigationTuning.Default());
            LwsRouteResult route = planner.PlanRoute(
                new LwsRouteRequest
                {
                    requestId = "test.50mile.route",
                    originNodeId = $"{LwsFiftyMileHighwayModel.EastboundSegmentId}_START",
                    destinationNodeId = $"{LwsFiftyMileHighwayModel.EastboundSegmentId}_END"
                },
                graph);

            Assert.IsTrue(route.succeeded, route.message);
            Assert.That(route.distanceMeters, Is.EqualTo((float)LwsFiftyMileHighwayModel.TotalLengthMeters).Within(0.01f));
            Assert.That(route.edgeIds, Is.EqualTo(new[] { LwsFiftyMileHighwayModel.EastboundEdgeId }));
        }

        [Test]
        public void ChunkPlacementAfterOriginOffsetUsesGlobalMinusOffset()
        {
            LwsWorldChunkDefinition chunk = LwsFiftyMileHighwayModel.CreateChunkDefinitions()[24];
            var originOffset = new LwsWorldPositionD(0d, 0d, 77000d);

            Vector3 localCenter = chunk.GetLocalBoundsCenter(originOffset);

            Assert.That(chunk.boundsCenter.z, Is.EqualTo(78857.856f).Within(0.01f));
            Assert.That(localCenter.z, Is.EqualTo(1857.856f).Within(0.01f));
        }
    }
}
