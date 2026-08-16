using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsWorldChunkStreamingState
    {
        Unloaded,
        Loading,
        LoadedInactive,
        Active,
        Unloading,
        Failed
    }

    [Serializable]
    public sealed class LwsWorldChunkDefinition
    {
        public string chunkId;
        public string displayName;
        public string sceneName;
        public string scenePath;
        public Vector3 boundsCenter;
        public Vector3 boundsSize = new Vector3(260f, 160f, 900f);
        public List<string> neighborChunkIds = new List<string>();
        public List<string> roadIds = new List<string>();
        public string trafficContent = "Validation traffic lane presentation";
        public string weatheradeContent = "Weatherade-compatible road/environment presentation";
        public int preloadPriority;
        public bool containsRoadGeometry = true;
        public bool containsTrafficPresentation = true;
        public bool containsWeatheradePresentation = true;
        public bool containsGlobalServices;
        public bool containsPlayerContent;

        public Bounds WorldBounds
        {
            get
            {
                Vector3 size = new Vector3(
                    Mathf.Max(1f, boundsSize.x),
                    Mathf.Max(1f, boundsSize.y),
                    Mathf.Max(1f, boundsSize.z));
                return new Bounds(boundsCenter, size);
            }
        }

        public bool Contains(Vector3 worldPosition)
        {
            return WorldBounds.Contains(worldPosition);
        }

        public float DistanceTo(Vector3 worldPosition)
        {
            return Mathf.Sqrt(WorldBounds.SqrDistance(worldPosition));
        }

        public float SignedDistanceAhead(Vector3 worldPosition, Vector3 heading)
        {
            Vector3 safeHeading = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector3.forward;
            return Vector3.Dot(boundsCenter - worldPosition, safeHeading);
        }

        public bool Validate(ISet<string> knownChunkIds, out string message)
        {
            if (string.IsNullOrWhiteSpace(chunkId))
            {
                message = "Chunk ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(scenePath))
            {
                message = $"Chunk {chunkId} requires a scene name and scene path.";
                return false;
            }

            if (boundsSize.x <= 0f || boundsSize.y <= 0f || boundsSize.z <= 0f)
            {
                message = $"Chunk {chunkId} has invalid bounds.";
                return false;
            }

            if (containsGlobalServices)
            {
                message = $"Chunk {chunkId} is marked as containing global services.";
                return false;
            }

            if (containsPlayerContent)
            {
                message = $"Chunk {chunkId} is marked as containing player-owned content.";
                return false;
            }

            if (neighborChunkIds != null)
            {
                for (int i = 0; i < neighborChunkIds.Count; i++)
                {
                    string neighborId = neighborChunkIds[i];
                    if (string.Equals(neighborId, chunkId, StringComparison.OrdinalIgnoreCase))
                    {
                        message = $"Chunk {chunkId} cannot list itself as a neighbor.";
                        return false;
                    }

                    if (knownChunkIds != null && !knownChunkIds.Contains(neighborId))
                    {
                        message = $"Chunk {chunkId} references missing neighbor {neighborId}.";
                        return false;
                    }
                }
            }

            message = $"Chunk {chunkId} is valid.";
            return true;
        }
    }

    [Serializable]
    public sealed class LwsWorldChunkRuntimeState
    {
        public string chunkId;
        public string sceneName;
        public LwsWorldChunkStreamingState state;
        public float distanceMeters;
        public float signedAheadDistanceMeters;
        public bool requiredByTractor;
        public bool requiredByTrailer;
        public bool desiredByPolicy;
        public bool registeredRoadPresentation;
        public bool registeredTrafficPresentation;
        public bool registeredWeatheradePresentation;
        public double requestedAtSeconds;
        public double loadedAtSeconds;
        public double activatedAtSeconds;
        public double unloadedAtSeconds;
        public float lastOperationDurationSeconds;
        public string lastError;

        public bool IsLoaded => state == LwsWorldChunkStreamingState.LoadedInactive ||
                                state == LwsWorldChunkStreamingState.Active ||
                                state == LwsWorldChunkStreamingState.Unloading;

        public LwsWorldChunkRuntimeState Clone()
        {
            return (LwsWorldChunkRuntimeState)MemberwiseClone();
        }
    }

    public readonly struct LwsWorldChunkEvent
    {
        public LwsWorldChunkEvent(LwsWorldChunkRuntimeState state, string message)
        {
            State = state?.Clone();
            Message = message ?? string.Empty;
        }

        public LwsWorldChunkRuntimeState State { get; }
        public string Message { get; }
    }

    public readonly struct LwsWorldStreamingAnchorState
    {
        public LwsWorldStreamingAnchorState(
            Vector3 tractorPosition,
            Vector3 heading,
            float speedMetersPerSecond,
            bool hasTrailer,
            Vector3 trailerPosition)
        {
            TractorPosition = tractorPosition;
            Heading = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector3.forward;
            SpeedMetersPerSecond = Mathf.Max(0f, speedMetersPerSecond);
            HasTrailer = hasTrailer;
            TrailerPosition = trailerPosition;
        }

        public Vector3 TractorPosition { get; }
        public Vector3 Heading { get; }
        public float SpeedMetersPerSecond { get; }
        public bool HasTrailer { get; }
        public Vector3 TrailerPosition { get; }

        public static LwsWorldStreamingAnchorState Default => new LwsWorldStreamingAnchorState(
            Vector3.zero,
            Vector3.forward,
            0f,
            false,
            Vector3.zero);
    }

    public readonly struct LwsWorldStreamingPolicyDecision
    {
        public LwsWorldStreamingPolicyDecision(
            string currentChunkId,
            HashSet<string> desiredChunkIds,
            HashSet<string> protectedChunkIds)
        {
            CurrentChunkId = currentChunkId ?? string.Empty;
            DesiredChunkIds = desiredChunkIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ProtectedChunkIds = protectedChunkIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public string CurrentChunkId { get; }
        public HashSet<string> DesiredChunkIds { get; }
        public HashSet<string> ProtectedChunkIds { get; }
    }

    public interface ILwsStreamedChunkParticipant
    {
        string ChunkId { get; }
        void OnChunkLoaded(LwsWorldChunkRuntimeState state);
        void OnChunkActivated(LwsWorldChunkRuntimeState state);
        void OnChunkDeactivating(LwsWorldChunkRuntimeState state);
        void OnChunkUnloaded(LwsWorldChunkRuntimeState state);
    }
}
