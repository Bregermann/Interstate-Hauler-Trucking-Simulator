using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsWorldStreamingEditModeTests
    {
        [Test]
        public void StreamingManifestValidatesStableChunkTopology()
        {
            LwsWorldStreamingManifest manifest = CreateManifest();

            LwsWorldStreamingValidationResult result = manifest.ValidateManifest();

            Assert.IsTrue(result.IsValid, result.Summary);
        }

        [Test]
        public void DuplicateChunkIdsAreRejected()
        {
            LwsWorldStreamingManifest manifest = CreateManifest();
            manifest.chunks.Add(CreateChunk("chunk-a", "Chunk_Duplicate", Vector3.forward * 1000f));

            LwsWorldStreamingValidationResult result = manifest.ValidateManifest();

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("Duplicate chunk ID", result.Summary);
        }

        [Test]
        public void MissingNeighborIdsAreRejected()
        {
            LwsWorldStreamingManifest manifest = CreateManifest();
            manifest.chunks[0].neighborChunkIds.Add("missing-neighbor");

            LwsWorldStreamingValidationResult result = manifest.ValidateManifest();

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("missing neighbor", result.Summary);
        }

        [Test]
        public void PolicyDecisionKeepsCurrentAheadAndTrailerChunks()
        {
            LwsWorldStreamingManifest manifest = CreateManifest();
            LwsWorldStreamingPolicy policy = manifest.policy;
            policy.loadAheadDistanceMeters = 700f;
            policy.keepBehindDistanceMeters = 250f;
            policy.preloadMarginMeters = 0f;
            policy.trailerSafetyMarginMeters = 120f;

            var anchor = new LwsWorldStreamingAnchorState(
                new Vector3(0f, 0f, 20f),
                Vector3.forward,
                24f,
                true,
                new Vector3(0f, 0f, -500f));

            LwsWorldStreamingPolicyDecision decision = LwsWorldStreamingService.BuildPolicyDecision(manifest, policy, anchor);

            Assert.AreEqual("chunk-a", decision.CurrentChunkId);
            CollectionAssert.Contains(decision.DesiredChunkIds, "chunk-a");
            CollectionAssert.Contains(decision.DesiredChunkIds, "chunk-b");
            CollectionAssert.Contains(decision.DesiredChunkIds, "chunk-behind");
            CollectionAssert.Contains(decision.ProtectedChunkIds, "chunk-behind");
        }

        [Test]
        public void StreamingHighwayGraphBootstrapCreatesValidGlobalGraph()
        {
            LwsRoadGraph graph = LwsStreamingHighwayGraphBootstrap.CreateDefaultGraph();

            LwsRoadGraphValidationResult result = graph.Validate();

            Assert.IsTrue(result.IsValid, result.Summary);
            Assert.AreEqual("IH_STREAMING_VALIDATION_GLOBAL_GRAPH", graph.graphId);
            Assert.AreEqual(4, graph.edges.Count);
        }

        [Test]
        public void SceneStreamerConcreteDependencyStaysInAdapterBoundary()
        {
            string types = File.ReadAllText("Assets/LWS/InterstateHauler/World/Streaming/LwsWorldStreamingTypes.cs");
            string service = File.ReadAllText("Assets/LWS/InterstateHauler/World/Streaming/LwsWorldStreamingService.cs");
            string adapter = File.ReadAllText("Assets/LWS/InterstateHauler/World/Streaming/LwsSceneStreamerAdapter.cs");

            StringAssert.DoesNotContain("PixelCrushers", types);
            StringAssert.DoesNotContain("PixelCrushers", service);
            StringAssert.Contains("PixelCrushers.SceneStreamer.SceneStreamer", adapter);
        }

        private static LwsWorldStreamingManifest CreateManifest()
        {
            LwsWorldStreamingPolicy policy = ScriptableObject.CreateInstance<LwsWorldStreamingPolicy>();
            policy.loadAheadDistanceMeters = 700f;
            policy.keepBehindDistanceMeters = 300f;
            policy.unloadDistanceMeters = 1200f;

            LwsWorldStreamingManifest manifest = ScriptableObject.CreateInstance<LwsWorldStreamingManifest>();
            manifest.policy = policy;
            manifest.worldId = "streaming-test-world";
            manifest.chunks = new List<LwsWorldChunkDefinition>
            {
                CreateChunk("chunk-behind", "Chunk_Behind", new Vector3(0f, 0f, -500f), "chunk-a"),
                CreateChunk("chunk-a", "Chunk_A", new Vector3(0f, 0f, 0f), "chunk-behind", "chunk-b"),
                CreateChunk("chunk-b", "Chunk_B", new Vector3(0f, 0f, 550f), "chunk-a", "chunk-c"),
                CreateChunk("chunk-c", "Chunk_C", new Vector3(0f, 0f, 1300f), "chunk-b")
            };

            return manifest;
        }

        private static LwsWorldChunkDefinition CreateChunk(string chunkId, string sceneName, Vector3 center, params string[] neighbors)
        {
            return new LwsWorldChunkDefinition
            {
                chunkId = chunkId,
                displayName = chunkId,
                sceneName = sceneName,
                scenePath = $"Assets/Test/{sceneName}.unity",
                boundsCenter = center,
                boundsSize = new Vector3(200f, 100f, 360f),
                neighborChunkIds = new List<string>(neighbors),
                roadIds = new List<string> { "IH_TEST_I000_NB" },
                containsRoadGeometry = true,
                containsTrafficPresentation = true,
                containsWeatheradePresentation = true,
                containsGlobalServices = false,
                containsPlayerContent = false
            };
        }
    }
}
