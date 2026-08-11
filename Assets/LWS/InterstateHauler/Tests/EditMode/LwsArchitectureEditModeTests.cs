using System;
using System.Collections.Generic;
using LWS.InterstateHauler.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsArchitectureEditModeTests
    {
        [Test]
        public void ServiceRegistryRejectsDuplicateRegistrations()
        {
            var registry = new LwsServiceRegistry();
            registry.Register(new RecordingServiceA("a", null));
            Assert.Throws<InvalidOperationException>(() => registry.Register(new RecordingServiceA("a2", null)));
        }

        [Test]
        public void ServiceInitializationOrderIsDeterministic()
        {
            var order = new List<string>();
            var registry = new LwsServiceRegistry();
            registry.Register(new RecordingServiceA("first", order));
            registry.Register(new RecordingServiceB("second", order), typeof(RecordingServiceA));

            LwsServiceResult result = registry.InitializeAll();

            Assert.IsTrue(result.Succeeded, result.Message);
            CollectionAssert.AreEqual(new[] { "init:first", "init:second" }, order);
        }

        [Test]
        public void ServiceShutdownOrderIsReverseInitializationOrder()
        {
            var order = new List<string>();
            var registry = new LwsServiceRegistry();
            registry.Register(new RecordingServiceA("first", order));
            registry.Register(new RecordingServiceB("second", order), typeof(RecordingServiceA));

            registry.InitializeAll();
            LwsServiceResult result = registry.ShutdownAll();

            Assert.IsTrue(result.Succeeded, result.Message);
            CollectionAssert.AreEqual(
                new[] { "init:first", "init:second", "shutdown:second", "shutdown:first" },
                order);
        }

        [Test]
        public void SaveParticipantIdsMustBeUnique()
        {
            var saveService = new LwsSaveService();
            saveService.Initialize(new LwsServiceContext(new LwsServiceRegistry()));

            LwsSaveOperationResult duplicate = saveService.RegisterParticipant(
                new LwsPlaceholderSaveParticipant("vehicle.truck", 1));

            Assert.IsFalse(duplicate.Succeeded);
        }

        [Test]
        public void RoadGraphSerializesAndDeserializes()
        {
            LwsRoadGraph graph = CreateTinyRoadGraph();

            string json = graph.ToJson();
            LwsRoadGraph copy = LwsRoadGraph.FromJson(json);

            Assert.IsNotNull(copy);
            Assert.AreEqual("test-graph", copy.graphId);
            Assert.AreEqual(2, copy.nodes.Count);
            Assert.AreEqual(1, copy.edges.Count);
        }

        [Test]
        public void RoadNodeAndEdgeIdsValidate()
        {
            LwsRoadGraph graph = CreateTinyRoadGraph();

            LwsRoadGraphValidationResult result = graph.Validate();

            Assert.IsTrue(result.IsValid, result.Summary);
        }

        [Test]
        public void TransmissionStateIsSerializationSafe()
        {
            var wrapper = new TransmissionStateWrapper
            {
                state = new LwsTransmissionState
                {
                    mode = LwsTransmissionMode.RangeSplitter,
                    logicalGear = 7,
                    physicalGate = LwsTruckShifterGate.Gate4,
                    range = LwsTruckRange.High,
                    splitter = LwsTruckSplitter.Low,
                    clutchInput = 0.25f,
                    neutral = false,
                    reverse = false,
                    engineStalled = false
                }
            };

            string json = JsonUtility.ToJson(wrapper);
            TransmissionStateWrapper copy = JsonUtility.FromJson<TransmissionStateWrapper>(json);

            Assert.AreEqual(7, copy.state.logicalGear);
            Assert.AreEqual(LwsTruckRange.High, copy.state.range);
        }

        [Test]
        public void ProjectValidatorReportsExpectedPrompt001Issues()
        {
            LwsProjectValidationReport report = LwsProjectValidator.RunValidation();

            Assert.IsTrue(report.Items.Count > 0);
            Assert.IsTrue(report.Items.Exists(i => i.Title == "Build Scenes"));
        }

        private static LwsRoadGraph CreateTinyRoadGraph()
        {
            return new LwsRoadGraph
            {
                graphId = "test-graph",
                nodes = new List<LwsRoadNode>
                {
                    new LwsRoadNode { nodeId = "node-a", position = Vector3.zero },
                    new LwsRoadNode { nodeId = "node-b", position = Vector3.forward }
                },
                edges = new List<LwsRoadEdge>
                {
                    new LwsRoadEdge
                    {
                        edgeId = "edge-a-b",
                        fromNodeId = "node-a",
                        toNodeId = "node-b",
                        roadClass = LwsRoadClass.Interstate,
                        distanceMeters = 100f,
                        speedLimitMph = 65f
                    }
                }
            };
        }

        [Serializable]
        private sealed class TransmissionStateWrapper
        {
            public LwsTransmissionState state;
        }

        private sealed class RecordingServiceA : ILwsService
        {
            private readonly List<string> _order;

            public RecordingServiceA(string id, List<string> order)
            {
                ServiceId = id;
                _order = order;
            }

            public string ServiceId { get; }

            public LwsServiceResult Initialize(LwsServiceContext context)
            {
                _order?.Add($"init:{ServiceId}");
                return LwsServiceResult.Success();
            }

            public LwsServiceResult Shutdown(LwsServiceContext context)
            {
                _order?.Add($"shutdown:{ServiceId}");
                return LwsServiceResult.Success();
            }
        }

        private sealed class RecordingServiceB : ILwsService
        {
            private readonly List<string> _order;

            public RecordingServiceB(string id, List<string> order)
            {
                ServiceId = id;
                _order = order;
            }

            public string ServiceId { get; }

            public LwsServiceResult Initialize(LwsServiceContext context)
            {
                _order?.Add($"init:{ServiceId}");
                return LwsServiceResult.Success();
            }

            public LwsServiceResult Shutdown(LwsServiceContext context)
            {
                _order?.Add($"shutdown:{ServiceId}");
                return LwsServiceResult.Success();
            }
        }
    }

    internal static class ValidationItemExtensions
    {
        public static bool Exists(this IReadOnlyList<LwsValidationItem> items, Predicate<LwsValidationItem> predicate)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (predicate(items[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
