using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsInterstateCorridorEditModeTests
    {
        [Test]
        public void InterstateRoadMetadataValidates()
        {
            LwsRoadGraph graph = CreateRoadGraph();

            LwsRoadGraphValidationResult result = graph.Validate();

            Assert.IsTrue(result.IsValid, result.Summary);
        }

        [Test]
        public void DuplicateRoadIdsAreRejected()
        {
            LwsRoadGraph graph = CreateRoadGraph();
            graph.edges.Add(CloneEdge(graph.edges[0], "edge-duplicate", "segment-duplicate"));

            LwsRoadGraphValidationResult result = graph.Validate();

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Duplicate road ID", result.Summary);
        }

        [Test]
        public void InvalidLaneCountIsRejected()
        {
            LwsRoadGraph graph = CreateRoadGraph();
            graph.edges[0].laneCount = 0;

            LwsRoadGraphValidationResult result = graph.Validate();

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("invalid lane count", result.Summary);
        }

        [Test]
        public void NearestRoadLookupReturnsSegmentMetadata()
        {
            LwsRoadGraph graph = CreateRoadGraph();

            bool found = LwsRoadGraphQuery.TryFindNearestRoad(graph, new Vector3(2f, 0f, 40f), 25f, out LwsRoadLookupResult result);

            Assert.IsTrue(found);
            Assert.AreEqual("IH_TEST_I000_NB", result.RoadId);
            Assert.AreEqual("IH_TEST_I000_NB_MAIN", result.SegmentId);
            Assert.AreEqual(LwsRoadClass.Interstate, result.RoadClass);
            Assert.AreEqual(65f, result.SpeedLimitMph);
            Assert.AreEqual(2, result.LaneCount);
        }

        [Test]
        public void RoadGraphServiceRegistersAndQueriesActiveGraph()
        {
            var service = new LwsRoadGraphService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            service.SetActiveGraph(CreateRoadGraph());

            bool found = service.TryFindNearestRoad(new Vector3(0f, 0f, 75f), 25f, out LwsRoadLookupResult result);

            Assert.IsTrue(found);
            Assert.AreEqual("IH_TEST_I000_NB_MAIN", result.SegmentId);
            service.Shutdown(new LwsServiceContext(new LwsServiceRegistry()));
        }

        [Test]
        public void EasyRoadsExportBoundaryReflectsCenterlineIntoRoadGraph()
        {
            var network = new FakeEasyRoadsNetwork();
            var boundary = new LwsEasyRoadsExportBoundary();

            LwsEasyRoadsExportReport report = boundary.CanExport(network);
            LwsRoadGraph graph = boundary.ExportRoadGraph(network, new LwsEasyRoadsExportOptions
            {
                graphId = "fake-export",
                defaultRoadIdPrefix = "IH_TEST",
                sampleSpacingMeters = 25f,
                defaultLaneCount = 2,
                defaultLaneWidthMeters = 3.7f
            });

            Assert.IsTrue(report.succeeded, report.message);
            Assert.AreEqual("fake-export", graph.graphId);
            Assert.AreEqual(1, graph.edges.Count);
            Assert.GreaterOrEqual(graph.edges[0].samples.Count, 4);
            Assert.IsTrue(graph.Validate().IsValid, graph.Validate().Summary);
        }

        private static LwsRoadGraph CreateRoadGraph()
        {
            var graph = new LwsRoadGraph
            {
                graphId = "IH_TEST_INTERSTATE_CORRIDOR_009",
                nodes = new List<LwsRoadNode>
                {
                    new LwsRoadNode { nodeId = "IH_TEST_I000_NB_MAIN_START", position = Vector3.zero },
                    new LwsRoadNode { nodeId = "IH_TEST_I000_NB_MAIN_END", position = new Vector3(0f, 0f, 100f) }
                }
            };

            var edge = new LwsRoadEdge
            {
                roadId = "IH_TEST_I000_NB",
                segmentId = "IH_TEST_I000_NB_MAIN",
                edgeId = "IH_TEST_I000_NB_MAIN_EDGE",
                fromNodeId = "IH_TEST_I000_NB_MAIN_START",
                toNodeId = "IH_TEST_I000_NB_MAIN_END",
                roadClass = LwsRoadClass.Interstate,
                direction = LwsRoadDirection.Northbound,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = true,
                distanceMeters = 100f,
                travelCost = 100f,
                speedLimitMph = 65f,
                laneCount = 2,
                laneWidthMeters = 3.7f,
                rightShoulderWidthMeters = 3.0f,
                leftShoulderWidthMeters = 1.2f,
                medianWidthMeters = 14f,
                laneCenterOffsetsMeters = new List<float> { -1.85f, 1.85f },
                samples = new List<LwsRoadSample>
                {
                    CreateSample(0f),
                    CreateSample(50f),
                    CreateSample(100f)
                }
            };

            graph.edges.Add(edge);
            return graph;
        }

        private static LwsRoadSample CreateSample(float distance)
        {
            return new LwsRoadSample
            {
                roadId = "IH_TEST_I000_NB",
                segmentId = "IH_TEST_I000_NB_MAIN",
                distanceFromStartMeters = distance,
                position = new Vector3(0f, 0f, distance),
                forward = Vector3.forward,
                up = Vector3.up,
                direction = LwsRoadDirection.Northbound,
                roadWidthMeters = 11.6f,
                laneWidthMeters = 3.7f,
                laneCount = 2,
                speedLimitMph = 65f
            };
        }

        private static LwsRoadEdge CloneEdge(LwsRoadEdge source, string edgeId, string segmentId)
        {
            return new LwsRoadEdge
            {
                roadId = source.roadId,
                segmentId = segmentId,
                edgeId = edgeId,
                fromNodeId = source.fromNodeId,
                toNodeId = source.toNodeId,
                roadClass = source.roadClass,
                direction = source.direction,
                surfaceType = source.surfaceType,
                oneWay = source.oneWay,
                distanceMeters = source.distanceMeters,
                speedLimitMph = source.speedLimitMph,
                laneCount = source.laneCount,
                laneWidthMeters = source.laneWidthMeters,
                laneCenterOffsetsMeters = new List<float>(source.laneCenterOffsetsMeters),
                samples = new List<LwsRoadSample>(source.samples)
            };
        }

        private sealed class FakeEasyRoadsNetwork
        {
            public FakeEasyRoad[] GetRoads()
            {
                return new[] { new FakeEasyRoad() };
            }
        }

        private sealed class FakeEasyRoad
        {
            public string roadName = "Fake Main";

            public Vector3[] GetMarkerPositions()
            {
                return new[]
                {
                    Vector3.zero,
                    new Vector3(0f, 0f, 50f),
                    new Vector3(5f, 0f, 100f)
                };
            }
        }
    }
}
