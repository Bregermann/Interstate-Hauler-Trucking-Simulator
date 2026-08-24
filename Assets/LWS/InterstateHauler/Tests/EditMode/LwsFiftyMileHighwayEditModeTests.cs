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
        public void ChunkBoundarySeamsAreExactAcrossTheFullFiftyMiles()
        {
            IReadOnlyList<LwsWorldChunkDefinition> chunks = LwsFiftyMileHighwayModel.CreateChunkDefinitions();

            for (int i = 0; i < chunks.Count - 1; i++)
            {
                double currentEnd = chunks[i].WorldBounds.max.z;
                double nextStart = chunks[i + 1].WorldBounds.min.z;

                Assert.That(currentEnd, Is.EqualTo(nextStart).Within(0.002d), $"Chunk seam {i:000}->{i + 1:000}");
                Assert.That(chunks[i].neighborChunkIds, Contains.Item(chunks[i + 1].chunkId), $"Forward neighbor missing at {i:000}");
                Assert.That(chunks[i + 1].neighborChunkIds, Contains.Item(chunks[i].chunkId), $"Backward neighbor missing at {i + 1:000}");
            }
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
        public void CertificationTeleportCheckpointsResolveCorrectChunkWeatherAndDistance()
        {
            var expected = new (double Mile, string ChunkId, string Weather, double RemainingMiles)[]
            {
                (0d, "IH_50MI_CHUNK_000", LwsWeatherPresetCatalog.ClearId, 50d),
                (10d, "IH_50MI_CHUNK_005", LwsWeatherPresetCatalog.OvercastId, 40d),
                (25d, "IH_50MI_CHUNK_012", LwsWeatherPresetCatalog.ThunderstormId, 25d),
                (40d, "IH_50MI_CHUNK_020", LwsWeatherPresetCatalog.HeavySnowId, 10d),
                (49d, "IH_50MI_CHUNK_024", LwsWeatherPresetCatalog.ClearId, 1d)
            };

            foreach ((double mile, string chunkId, string weather, double remainingMiles) in expected)
            {
                double meters = LwsFiftyMileHighwayModel.MileToMeters(mile);

                Assert.AreEqual(chunkId, LwsFiftyMileHighwayModel.GetChunkId(LwsFiftyMileHighwayModel.GetChunkIndexForMeters(meters)), $"Chunk at Mile {mile}");
                Assert.AreEqual(weather, LwsFiftyMileHighwayModel.GetWeatherPresetForMile(mile), $"Weather at Mile {mile}");
                Assert.That((LwsFiftyMileHighwayModel.TotalLengthMeters - meters) / LwsFiftyMileHighwayModel.MetersPerMile, Is.EqualTo(remainingMiles).Within(0.001d), $"Remaining at Mile {mile}");
            }
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

        [Test]
        public void CertificationReportAndDebugSourceExposeRequiredMetrics()
        {
            var owner = new GameObject("50-mile-certification-report-test");
            try
            {
                var controller = owner.AddComponent<LwsFiftyMileHighwayValidationController>();
                string report = controller.BuildCertificationReportForValidation();

                StringAssert.Contains("50-MILE CERTIFICATION COMPLETE", report);
                StringAssert.Contains("Distance:", report);
                StringAssert.Contains("Origin Shifts:", report);
                StringAssert.Contains("Chunk Loads:", report);
                StringAssert.Contains("Chunk Unloads:", report);
                StringAssert.Contains("Traffic Spawned:", report);
                StringAssert.Contains("Weather Transitions:", report);
                StringAssert.Contains("GPS Status:", report);
                StringAssert.Contains("Maximum Local Distance:", report);
                StringAssert.Contains("Largest Shift Duration:", report);
                StringAssert.Contains("Meters Road Ahead:", report);
                StringAssert.Contains("Streaming Failures:", report);
                StringAssert.Contains("Runtime Errors:", report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }

            string debugPanel = System.IO.File.ReadAllText("Assets/LWS/InterstateHauler/World/Origin/LwsFiftyMileHighwayDebugPanel.cs");
            StringAssert.Contains("Meters Road Ahead", debugPanel);
            StringAssert.Contains("Streaming Failures", debugPanel);
        }
    }
}
