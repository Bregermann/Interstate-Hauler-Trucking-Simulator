using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsWorldStreamingPlayModeTests
    {
        [UnityTest]
        public IEnumerator DefaultRegistryHasSingleWorldStreamingService()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            int count = registry.Registrations.Count(r => r.ServiceType == typeof(ILwsWorldStreamingService));

            Assert.AreEqual(1, count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StreamingServiceRequestsAndTracksLoadedChunks()
        {
            LwsWorldStreamingService service = new LwsWorldStreamingService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            FakeSceneStreamerAdapter adapter = new FakeSceneStreamerAdapter();
            LwsWorldStreamingManifest manifest = CreateManifest();

            LwsWorldStreamingValidationResult result = service.Configure(manifest, adapter);
            Assert.IsTrue(result.IsValid, result.Summary);

            service.UpdateStreamingAnchor(new LwsWorldStreamingAnchorState(Vector3.zero, Vector3.forward, 15f, false, Vector3.zero));
            service.Tick(float.MaxValue);
            yield return null;

            Assert.AreEqual("chunk-a", service.ActiveWorldChunkId);
            Assert.IsTrue(service.TryGetChunkState("chunk-a", out LwsWorldChunkRuntimeState current));
            Assert.AreEqual(LwsWorldChunkStreamingState.Active, current.state);
            Assert.IsTrue(service.TryGetChunkState("chunk-b", out LwsWorldChunkRuntimeState next));
            Assert.IsTrue(next.IsLoaded);
            CollectionAssert.Contains(adapter.LoadedScenes, "Chunk_A");
        }

        [UnityTest]
        public IEnumerator FreezingStreamingPreventsPolicyLoads()
        {
            LwsWorldStreamingService service = new LwsWorldStreamingService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            FakeSceneStreamerAdapter adapter = new FakeSceneStreamerAdapter();
            service.Configure(CreateManifest(), adapter);
            service.SetFrozen(true);

            service.UpdateStreamingAnchor(new LwsWorldStreamingAnchorState(Vector3.zero, Vector3.forward, 15f, false, Vector3.zero));
            service.Tick(float.MaxValue);
            yield return null;

            Assert.IsEmpty(adapter.LoadedScenes);
        }

        [UnityTest]
        public IEnumerator ExplicitLoadAllAndUnloadDistantAreSafeWithAdapter()
        {
            LwsWorldStreamingService service = new LwsWorldStreamingService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            FakeSceneStreamerAdapter adapter = new FakeSceneStreamerAdapter();
            service.Configure(CreateManifest(), adapter);

            service.LoadAllChunks();
            yield return null;

            Assert.AreEqual(2, adapter.LoadedScenes.Count);
            service.UpdateStreamingAnchor(new LwsWorldStreamingAnchorState(new Vector3(0f, 0f, 4000f), Vector3.forward, 10f, false, Vector3.zero));
            service.UnloadDistantChunks();
            yield return null;

            Assert.AreEqual(1, adapter.LoadedScenes.Count);
            CollectionAssert.Contains(adapter.LoadedScenes, "Chunk_B");
        }

        private static LwsWorldStreamingManifest CreateManifest()
        {
            LwsWorldStreamingPolicy policy = ScriptableObject.CreateInstance<LwsWorldStreamingPolicy>();
            policy.loadAheadDistanceMeters = 700f;
            policy.keepBehindDistanceMeters = 250f;
            policy.preloadMarginMeters = 0f;
            policy.unloadDistanceMeters = 900f;
            policy.maximumConcurrentLoads = 4;
            policy.minimumLoadedNeighborCount = 0;
            policy.evaluationIntervalSeconds = 0.01f;

            LwsWorldStreamingManifest manifest = ScriptableObject.CreateInstance<LwsWorldStreamingManifest>();
            manifest.worldId = "playmode-streaming-test";
            manifest.policy = policy;
            manifest.chunks = new List<LwsWorldChunkDefinition>
            {
                CreateChunk("chunk-a", "Chunk_A", Vector3.zero, "chunk-b"),
                CreateChunk("chunk-b", "Chunk_B", new Vector3(0f, 0f, 500f), "chunk-a")
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
                boundsSize = new Vector3(200f, 100f, 400f),
                neighborChunkIds = new List<string>(neighbors),
                roadIds = new List<string> { "IH_TEST_I000_NB" },
                containsRoadGeometry = true,
                containsTrafficPresentation = true,
                containsWeatheradePresentation = true
            };
        }

        private sealed class FakeSceneStreamerAdapter : ILwsSceneStreamerAdapter
        {
            private readonly HashSet<string> _loadedScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public bool IsAvailable => true;
            public string Status => "Fake adapter ready.";
            public int PendingOperationCount => 0;
            public IReadOnlyCollection<string> LoadedScenes => _loadedScenes;

            public event Action<string> SceneLoadRequested;
            public event Action<string> SceneLoaded;
            public event Action<string> SceneUnloadRequested;
            public event Action<string> SceneUnloaded;
            public event Action<string, string> SceneOperationFailed;

            public bool SetCurrentScene(string sceneName)
            {
                return !string.IsNullOrWhiteSpace(sceneName);
            }

            public bool RequestLoadScene(string sceneName)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    SceneOperationFailed?.Invoke(sceneName, "Scene name is empty.");
                    return false;
                }

                SceneLoadRequested?.Invoke(sceneName);
                _loadedScenes.Add(sceneName);
                SceneLoaded?.Invoke(sceneName);
                return true;
            }

            public bool RequestUnloadScene(string sceneName)
            {
                SceneUnloadRequested?.Invoke(sceneName);
                _loadedScenes.Remove(sceneName);
                SceneUnloaded?.Invoke(sceneName);
                return true;
            }

            public bool IsSceneLoaded(string sceneName)
            {
                return _loadedScenes.Contains(sceneName);
            }

            public bool ClearAll()
            {
                _loadedScenes.Clear();
                return true;
            }
        }
    }
}
