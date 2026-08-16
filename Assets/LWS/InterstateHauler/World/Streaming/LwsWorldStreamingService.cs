using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.InterstateHauler
{
    public interface ILwsWorldStreamingService : ILwsService
    {
        string ActiveWorldChunkId { get; }
        string WorldId { get; }
        bool IsFrozen { get; }
        LwsWorldStreamingManifest ActiveManifest { get; }
        IReadOnlyList<LwsWorldChunkRuntimeState> ChunkStates { get; }
        LwsWorldStreamingAnchorState AnchorState { get; }
        float LastLoadDurationSeconds { get; }
        float LastUnloadDurationSeconds { get; }
        string LastError { get; }

        event Action<LwsWorldChunkEvent> ChunkLoadRequested;
        event Action<LwsWorldChunkEvent> ChunkLoaded;
        event Action<LwsWorldChunkEvent> ChunkActivated;
        event Action<LwsWorldChunkEvent> ChunkUnloadRequested;
        event Action<LwsWorldChunkEvent> ChunkUnloaded;
        event Action<LwsWorldChunkEvent> ChunkFailed;
        event Action<LwsWorldStreamingAnchorState> StreamingAnchorChanged;

        LwsWorldStreamingValidationResult Configure(LwsWorldStreamingManifest manifest, ILwsSceneStreamerAdapter adapter);
        void UpdateStreamingAnchor(LwsWorldStreamingAnchorState anchorState);
        void Tick(float deltaTimeSeconds);
        void SetActiveWorldChunk(string chunkId);
        bool TryGetChunkState(string chunkId, out LwsWorldChunkRuntimeState state);
        bool TryGetChunkDefinition(string chunkId, out LwsWorldChunkDefinition definition);
        void SetFrozen(bool frozen);
        void LoadAllChunks();
        void UnloadDistantChunks();
        void ReloadCurrentNeighborhood();
    }

    public sealed class LwsWorldStreamingService : ILwsWorldStreamingService
    {
        private readonly Dictionary<string, LwsWorldChunkRuntimeState> _states =
            new Dictionary<string, LwsWorldChunkRuntimeState>(StringComparer.OrdinalIgnoreCase);

        private readonly Queue<string> _loadQueue = new Queue<string>();
        private readonly Queue<string> _unloadQueue = new Queue<string>();
        private readonly HashSet<string> _requestedLoads = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _requestedUnloads = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private ILwsSceneStreamerAdapter _adapter;
        private float _evaluationTimer;

        public string ServiceId => "lws.world.streaming";
        public string ActiveWorldChunkId { get; private set; } = string.Empty;
        public string WorldId => ActiveManifest != null ? ActiveManifest.worldId : string.Empty;
        public bool IsFrozen { get; private set; }
        public LwsWorldStreamingManifest ActiveManifest { get; private set; }
        public IReadOnlyList<LwsWorldChunkRuntimeState> ChunkStates => _states.Values.Select(s => s.Clone()).ToList();
        public LwsWorldStreamingAnchorState AnchorState { get; private set; } = LwsWorldStreamingAnchorState.Default;
        public float LastLoadDurationSeconds { get; private set; }
        public float LastUnloadDurationSeconds { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public event Action<LwsWorldChunkEvent> ChunkLoadRequested;
        public event Action<LwsWorldChunkEvent> ChunkLoaded;
        public event Action<LwsWorldChunkEvent> ChunkActivated;
        public event Action<LwsWorldChunkEvent> ChunkUnloadRequested;
        public event Action<LwsWorldChunkEvent> ChunkUnloaded;
        public event Action<LwsWorldChunkEvent> ChunkFailed;
        public event Action<LwsWorldStreamingAnchorState> StreamingAnchorChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            return LwsServiceResult.Success("LWS world streaming service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            DetachAdapter();
            _states.Clear();
            _loadQueue.Clear();
            _unloadQueue.Clear();
            _requestedLoads.Clear();
            _requestedUnloads.Clear();
            ActiveManifest = null;
            ActiveWorldChunkId = string.Empty;
            AnchorState = LwsWorldStreamingAnchorState.Default;
            return LwsServiceResult.Success("LWS world streaming service shut down.");
        }

        public LwsWorldStreamingValidationResult Configure(LwsWorldStreamingManifest manifest, ILwsSceneStreamerAdapter adapter)
        {
            DetachAdapter();
            ActiveManifest = manifest;
            _adapter = adapter;
            _states.Clear();
            _loadQueue.Clear();
            _unloadQueue.Clear();
            _requestedLoads.Clear();
            _requestedUnloads.Clear();

            LwsWorldStreamingValidationResult validation = manifest != null
                ? manifest.ValidateManifest()
                : new LwsWorldStreamingValidationResult(false, new[] { "Streaming manifest is not assigned." });
            if (!validation.IsValid)
            {
                LastError = validation.Summary;
                return validation;
            }

            for (int i = 0; i < manifest.chunks.Count; i++)
            {
                LwsWorldChunkDefinition chunk = manifest.chunks[i];
                _states[chunk.chunkId] = new LwsWorldChunkRuntimeState
                {
                    chunkId = chunk.chunkId,
                    sceneName = chunk.sceneName,
                    state = LwsWorldChunkStreamingState.Unloaded
                };
            }

            AttachAdapter();
            LastError = string.Empty;
            return validation;
        }

        public void UpdateStreamingAnchor(LwsWorldStreamingAnchorState anchorState)
        {
            AnchorState = anchorState;
            StreamingAnchorChanged?.Invoke(anchorState);
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (ActiveManifest == null || _adapter == null || IsFrozen)
            {
                return;
            }

            LwsWorldStreamingPolicy policy = ActiveManifest.policy;
            if (policy == null)
            {
                return;
            }

            _evaluationTimer += Mathf.Max(0f, deltaTimeSeconds);
            if (_evaluationTimer < policy.evaluationIntervalSeconds)
            {
                PumpQueues();
                return;
            }

            _evaluationTimer = 0f;
            LwsWorldStreamingPolicyDecision decision = BuildPolicyDecision(ActiveManifest, policy, AnchorState);
            if (!string.IsNullOrWhiteSpace(decision.CurrentChunkId))
            {
                SetActiveWorldChunk(decision.CurrentChunkId);
            }

            UpdateDistancesAndPolicyFlags(decision);
            QueuePolicyOperations(decision);
            PumpQueues();
        }

        public void SetActiveWorldChunk(string chunkId)
        {
            if (string.IsNullOrWhiteSpace(chunkId) || ActiveManifest == null || !ActiveManifest.TryGetChunk(chunkId, out LwsWorldChunkDefinition chunk))
            {
                return;
            }

            if (string.Equals(ActiveWorldChunkId, chunkId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string previous = ActiveWorldChunkId;
            ActiveWorldChunkId = chunkId;
            if (!string.IsNullOrWhiteSpace(previous) && _states.TryGetValue(previous, out LwsWorldChunkRuntimeState previousState) &&
                previousState.state == LwsWorldChunkStreamingState.Active)
            {
                if (ActiveManifest.TryGetChunk(previous, out LwsWorldChunkDefinition previousChunk))
                {
                    NotifyChunkParticipants(previousChunk.sceneName, previousState, (participant, runtimeState) => participant.OnChunkDeactivating(runtimeState));
                }

                previousState.state = LwsWorldChunkStreamingState.LoadedInactive;
            }

            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            if (state.state == LwsWorldChunkStreamingState.LoadedInactive || state.state == LwsWorldChunkStreamingState.Loading)
            {
                state.state = LwsWorldChunkStreamingState.Active;
                state.activatedAtSeconds = Time.realtimeSinceStartupAsDouble;
                NotifyChunkParticipants(chunk.sceneName, state, (participant, runtimeState) => participant.OnChunkActivated(runtimeState));
                ChunkActivated?.Invoke(new LwsWorldChunkEvent(state, $"Chunk {chunkId} activated."));
            }

            // The installed Scene Streamer LoadScene API ignores its sceneName argument.
            // Keep LWS policy loads explicit and avoid asking Scene Streamer to re-load scenes
            // it did not load through its own private tracking list.
        }

        public bool TryGetChunkState(string chunkId, out LwsWorldChunkRuntimeState state)
        {
            if (_states.TryGetValue(chunkId ?? string.Empty, out LwsWorldChunkRuntimeState stored))
            {
                state = stored.Clone();
                return true;
            }

            state = null;
            return false;
        }

        public bool TryGetChunkDefinition(string chunkId, out LwsWorldChunkDefinition definition)
        {
            definition = null;
            return ActiveManifest != null && ActiveManifest.TryGetChunk(chunkId, out definition);
        }

        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;
        }

        public void LoadAllChunks()
        {
            if (ActiveManifest == null)
            {
                return;
            }

            for (int i = 0; i < ActiveManifest.chunks.Count; i++)
            {
                QueueLoad(ActiveManifest.chunks[i]);
            }

            PumpQueues();
        }

        public void UnloadDistantChunks()
        {
            if (ActiveManifest == null || ActiveManifest.policy == null)
            {
                return;
            }

            LwsWorldStreamingPolicyDecision decision = BuildPolicyDecision(ActiveManifest, ActiveManifest.policy, AnchorState);
            UpdateDistancesAndPolicyFlags(decision);
            QueuePolicyOperations(decision);
            PumpQueues();
        }

        public void ReloadCurrentNeighborhood()
        {
            if (ActiveManifest == null || ActiveManifest.policy == null)
            {
                return;
            }

            LwsWorldStreamingPolicyDecision decision = BuildPolicyDecision(ActiveManifest, ActiveManifest.policy, AnchorState);
            UpdateDistancesAndPolicyFlags(decision);
            foreach (string chunkId in decision.DesiredChunkIds)
            {
                if (ActiveManifest.TryGetChunk(chunkId, out LwsWorldChunkDefinition chunk))
                {
                    QueueLoad(chunk);
                }
            }

            PumpQueues();
        }

        public static LwsWorldStreamingPolicyDecision BuildPolicyDecision(
            LwsWorldStreamingManifest manifest,
            LwsWorldStreamingPolicy policy,
            LwsWorldStreamingAnchorState anchorState)
        {
            var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var protectedChunks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (manifest == null || policy == null || manifest.chunks == null || manifest.chunks.Count == 0)
            {
                return new LwsWorldStreamingPolicyDecision(string.Empty, desired, protectedChunks);
            }

            LwsWorldChunkDefinition current = manifest.FindContainingChunk(anchorState.TractorPosition);
            if (current != null)
            {
                desired.Add(current.chunkId);
                protectedChunks.Add(current.chunkId);
                AddNeighbors(manifest, current, policy.minimumLoadedNeighborCount, desired);
            }

            for (int i = 0; i < manifest.chunks.Count; i++)
            {
                LwsWorldChunkDefinition chunk = manifest.chunks[i];
                if (chunk == null)
                {
                    continue;
                }

                float distance = chunk.DistanceTo(anchorState.TractorPosition);
                float ahead = chunk.SignedDistanceAhead(anchorState.TractorPosition, anchorState.Heading);
                bool aheadCandidate = ahead >= -policy.keepBehindDistanceMeters &&
                                      ahead <= policy.loadAheadDistanceMeters + policy.preloadMarginMeters &&
                                      distance <= policy.loadAheadDistanceMeters + policy.preloadMarginMeters;
                bool behindCandidate = ahead < 0f && Mathf.Abs(ahead) <= policy.keepBehindDistanceMeters && distance <= policy.keepBehindDistanceMeters + policy.preloadMarginMeters;
                bool tractorProtected = chunk.WorldBounds.ExpandCopy(policy.tractorSafetyMarginMeters).Contains(anchorState.TractorPosition);
                bool trailerProtected = anchorState.HasTrailer &&
                                        chunk.WorldBounds.ExpandCopy(policy.trailerSafetyMarginMeters).Contains(anchorState.TrailerPosition);

                if (aheadCandidate || behindCandidate || tractorProtected || trailerProtected)
                {
                    desired.Add(chunk.chunkId);
                }

                if (tractorProtected || trailerProtected)
                {
                    protectedChunks.Add(chunk.chunkId);
                }
            }

            return new LwsWorldStreamingPolicyDecision(current != null ? current.chunkId : string.Empty, desired, protectedChunks);
        }

        private static void AddNeighbors(
            LwsWorldStreamingManifest manifest,
            LwsWorldChunkDefinition seed,
            int depth,
            HashSet<string> desired)
        {
            if (manifest == null || seed == null || depth <= 0 || seed.neighborChunkIds == null)
            {
                return;
            }

            var frontier = new Queue<(string chunkId, int distance)>();
            for (int i = 0; i < seed.neighborChunkIds.Count; i++)
            {
                frontier.Enqueue((seed.neighborChunkIds[i], 1));
            }

            while (frontier.Count > 0)
            {
                (string chunkId, int distance) = frontier.Dequeue();
                if (!desired.Add(chunkId) || distance >= depth || !manifest.TryGetChunk(chunkId, out LwsWorldChunkDefinition chunk) || chunk.neighborChunkIds == null)
                {
                    continue;
                }

                for (int i = 0; i < chunk.neighborChunkIds.Count; i++)
                {
                    frontier.Enqueue((chunk.neighborChunkIds[i], distance + 1));
                }
            }
        }

        private void UpdateDistancesAndPolicyFlags(LwsWorldStreamingPolicyDecision decision)
        {
            foreach (LwsWorldChunkDefinition chunk in ActiveManifest.chunks)
            {
                if (chunk == null || !_states.TryGetValue(chunk.chunkId, out LwsWorldChunkRuntimeState state))
                {
                    continue;
                }

                state.distanceMeters = chunk.DistanceTo(AnchorState.TractorPosition);
                state.signedAheadDistanceMeters = chunk.SignedDistanceAhead(AnchorState.TractorPosition, AnchorState.Heading);
                state.desiredByPolicy = decision.DesiredChunkIds.Contains(chunk.chunkId);
                state.requiredByTractor = chunk.WorldBounds.ExpandCopy(ActiveManifest.policy.tractorSafetyMarginMeters).Contains(AnchorState.TractorPosition);
                state.requiredByTrailer = AnchorState.HasTrailer &&
                                          chunk.WorldBounds.ExpandCopy(ActiveManifest.policy.trailerSafetyMarginMeters).Contains(AnchorState.TrailerPosition);
            }
        }

        private void QueuePolicyOperations(LwsWorldStreamingPolicyDecision decision)
        {
            foreach (string chunkId in decision.DesiredChunkIds)
            {
                if (ActiveManifest.TryGetChunk(chunkId, out LwsWorldChunkDefinition desiredChunk))
                {
                    QueueLoad(desiredChunk);
                }
            }

            foreach (LwsWorldChunkDefinition chunk in ActiveManifest.chunks)
            {
                if (chunk == null || !_states.TryGetValue(chunk.chunkId, out LwsWorldChunkRuntimeState state))
                {
                    continue;
                }

                if (!state.IsLoaded || decision.DesiredChunkIds.Contains(chunk.chunkId) || decision.ProtectedChunkIds.Contains(chunk.chunkId))
                {
                    continue;
                }

                float unloadThreshold = ActiveManifest.policy.unloadDistanceMeters + ActiveManifest.policy.unloadHysteresisMeters;
                if (state.distanceMeters >= unloadThreshold)
                {
                    QueueUnload(chunk);
                }
            }
        }

        private void QueueLoad(LwsWorldChunkDefinition chunk)
        {
            if (chunk == null || _requestedLoads.Contains(chunk.chunkId))
            {
                return;
            }

            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            if (state.state == LwsWorldChunkStreamingState.Loading || state.IsLoaded)
            {
                return;
            }

            _requestedLoads.Add(chunk.chunkId);
            _loadQueue.Enqueue(chunk.chunkId);
        }

        private void QueueUnload(LwsWorldChunkDefinition chunk)
        {
            if (chunk == null || _requestedUnloads.Contains(chunk.chunkId))
            {
                return;
            }

            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            if (state.state == LwsWorldChunkStreamingState.Unloaded || state.state == LwsWorldChunkStreamingState.Unloading)
            {
                return;
            }

            _requestedUnloads.Add(chunk.chunkId);
            _unloadQueue.Enqueue(chunk.chunkId);
        }

        private void PumpQueues()
        {
            if (_adapter == null || ActiveManifest == null || ActiveManifest.policy == null)
            {
                return;
            }

            int budget = Mathf.Max(1, ActiveManifest.policy.maximumConcurrentLoads) - _adapter.PendingOperationCount;
            while (budget > 0 && _loadQueue.Count > 0)
            {
                string chunkId = _loadQueue.Dequeue();
                _requestedLoads.Remove(chunkId);
                if (ActiveManifest.TryGetChunk(chunkId, out LwsWorldChunkDefinition chunk))
                {
                    RequestLoad(chunk);
                    budget--;
                }
            }

            while (budget > 0 && _unloadQueue.Count > 0)
            {
                string chunkId = _unloadQueue.Dequeue();
                _requestedUnloads.Remove(chunkId);
                if (ActiveManifest.TryGetChunk(chunkId, out LwsWorldChunkDefinition chunk))
                {
                    RequestUnload(chunk);
                    budget--;
                }
            }
        }

        private void RequestLoad(LwsWorldChunkDefinition chunk)
        {
            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            if (state.IsLoaded || state.state == LwsWorldChunkStreamingState.Loading)
            {
                return;
            }

            state.state = LwsWorldChunkStreamingState.Loading;
            state.requestedAtSeconds = Time.realtimeSinceStartupAsDouble;
            ChunkLoadRequested?.Invoke(new LwsWorldChunkEvent(state, $"Chunk {chunk.chunkId} load requested."));
            if (!_adapter.RequestLoadScene(chunk.sceneName))
            {
                MarkFailed(chunk.sceneName, $"Chunk {chunk.chunkId} failed to request load.");
            }
        }

        private void RequestUnload(LwsWorldChunkDefinition chunk)
        {
            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            if (!state.IsLoaded || state.state == LwsWorldChunkStreamingState.Unloading)
            {
                return;
            }

            state.state = LwsWorldChunkStreamingState.Unloading;
            state.requestedAtSeconds = Time.realtimeSinceStartupAsDouble;
            NotifyChunkParticipants(chunk.sceneName, state, (participant, runtimeState) => participant.OnChunkDeactivating(runtimeState));
            ChunkUnloadRequested?.Invoke(new LwsWorldChunkEvent(state, $"Chunk {chunk.chunkId} unload requested."));
            if (!_adapter.RequestUnloadScene(chunk.sceneName))
            {
                MarkFailed(chunk.sceneName, $"Chunk {chunk.chunkId} failed to request unload.");
            }
        }

        private void AttachAdapter()
        {
            if (_adapter == null)
            {
                return;
            }

            _adapter.SceneLoaded += HandleSceneLoaded;
            _adapter.SceneUnloaded += HandleSceneUnloaded;
            _adapter.SceneOperationFailed += MarkFailed;
        }

        private void DetachAdapter()
        {
            if (_adapter == null)
            {
                return;
            }

            _adapter.SceneLoaded -= HandleSceneLoaded;
            _adapter.SceneUnloaded -= HandleSceneUnloaded;
            _adapter.SceneOperationFailed -= MarkFailed;
            _adapter = null;
        }

        private void HandleSceneLoaded(string sceneName)
        {
            if (ActiveManifest == null || !ActiveManifest.TryGetChunkBySceneName(sceneName, out LwsWorldChunkDefinition chunk))
            {
                return;
            }

            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            state.state = string.Equals(chunk.chunkId, ActiveWorldChunkId, StringComparison.OrdinalIgnoreCase)
                ? LwsWorldChunkStreamingState.Active
                : LwsWorldChunkStreamingState.LoadedInactive;
            state.loadedAtSeconds = Time.realtimeSinceStartupAsDouble;
            if (state.activatedAtSeconds <= 0d && state.state == LwsWorldChunkStreamingState.Active)
            {
                state.activatedAtSeconds = state.loadedAtSeconds;
            }

            state.lastOperationDurationSeconds = state.requestedAtSeconds > 0d
                ? Mathf.Max(0f, (float)(state.loadedAtSeconds - state.requestedAtSeconds))
                : 0f;
            LastLoadDurationSeconds = state.lastOperationDurationSeconds;
            RegisterChunkPresentation(state, chunk);
            NotifyChunkParticipants(sceneName, state, (participant, runtimeState) => participant.OnChunkLoaded(runtimeState));
            ChunkLoaded?.Invoke(new LwsWorldChunkEvent(state, $"Chunk {chunk.chunkId} loaded."));
            if (state.state == LwsWorldChunkStreamingState.Active)
            {
                NotifyChunkParticipants(sceneName, state, (participant, runtimeState) => participant.OnChunkActivated(runtimeState));
                ChunkActivated?.Invoke(new LwsWorldChunkEvent(state, $"Chunk {chunk.chunkId} activated."));
            }
        }

        private void HandleSceneUnloaded(string sceneName)
        {
            if (ActiveManifest == null || !ActiveManifest.TryGetChunkBySceneName(sceneName, out LwsWorldChunkDefinition chunk))
            {
                return;
            }

            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            state.state = LwsWorldChunkStreamingState.Unloaded;
            state.unloadedAtSeconds = Time.realtimeSinceStartupAsDouble;
            state.lastOperationDurationSeconds = state.requestedAtSeconds > 0d
                ? Mathf.Max(0f, (float)(state.unloadedAtSeconds - state.requestedAtSeconds))
                : 0f;
            LastUnloadDurationSeconds = state.lastOperationDurationSeconds;
            state.registeredRoadPresentation = false;
            state.registeredTrafficPresentation = false;
            state.registeredWeatheradePresentation = false;
            ChunkUnloaded?.Invoke(new LwsWorldChunkEvent(state, $"Chunk {chunk.chunkId} unloaded."));
        }

        private void MarkFailed(string sceneName, string message)
        {
            if (ActiveManifest == null || !ActiveManifest.TryGetChunkBySceneName(sceneName, out LwsWorldChunkDefinition chunk))
            {
                LastError = message ?? string.Empty;
                return;
            }

            LwsWorldChunkRuntimeState state = GetOrCreateState(chunk);
            state.state = LwsWorldChunkStreamingState.Failed;
            state.lastError = message ?? string.Empty;
            LastError = state.lastError;
            ChunkFailed?.Invoke(new LwsWorldChunkEvent(state, state.lastError));
        }

        private static void RegisterChunkPresentation(LwsWorldChunkRuntimeState state, LwsWorldChunkDefinition chunk)
        {
            state.registeredRoadPresentation = chunk.containsRoadGeometry;
            state.registeredTrafficPresentation = chunk.containsTrafficPresentation;
            state.registeredWeatheradePresentation = chunk.containsWeatheradePresentation;
        }

        private static void NotifyChunkParticipants(
            string sceneName,
            LwsWorldChunkRuntimeState state,
            Action<ILwsStreamedChunkParticipant, LwsWorldChunkRuntimeState> callback)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || state == null || callback == null)
            {
                return;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                MonoBehaviour[] behaviours = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (int b = 0; b < behaviours.Length; b++)
                {
                    if (behaviours[b] is ILwsStreamedChunkParticipant participant &&
                        string.Equals(participant.ChunkId, state.chunkId, StringComparison.OrdinalIgnoreCase))
                    {
                        callback(participant, state.Clone());
                    }
                }
            }
        }

        private LwsWorldChunkRuntimeState GetOrCreateState(LwsWorldChunkDefinition chunk)
        {
            if (_states.TryGetValue(chunk.chunkId, out LwsWorldChunkRuntimeState state))
            {
                return state;
            }

            state = new LwsWorldChunkRuntimeState
            {
                chunkId = chunk.chunkId,
                sceneName = chunk.sceneName,
                state = LwsWorldChunkStreamingState.Unloaded
            };
            _states[chunk.chunkId] = state;
            return state;
        }
    }

    internal static class LwsBoundsExtensions
    {
        public static Bounds ExpandCopy(this Bounds bounds, float amount)
        {
            bounds.Expand(Mathf.Max(0f, amount) * 2f);
            return bounds;
        }
    }
}
