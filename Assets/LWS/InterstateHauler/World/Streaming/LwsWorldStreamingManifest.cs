using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [CreateAssetMenu(
        fileName = "IH_WorldStreamingManifest",
        menuName = "Interstate Hauler/World Streaming/World Manifest")]
    public sealed class LwsWorldStreamingManifest : ScriptableObject
    {
        public string worldId = "IH_STREAMING_VALIDATION_WORLD";
        public LwsWorldStreamingPolicy policy;
        public List<LwsWorldChunkDefinition> chunks = new List<LwsWorldChunkDefinition>();

        public IReadOnlyList<LwsWorldChunkDefinition> Chunks => chunks;

        public LwsWorldStreamingValidationResult ValidateManifest()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(worldId))
            {
                errors.Add("World ID is required.");
            }

            if (policy == null)
            {
                errors.Add("Streaming policy asset is required.");
            }
            else if (!policy.Validate(out string policyMessage))
            {
                errors.Add(policyMessage);
            }

            if (chunks == null || chunks.Count == 0)
            {
                errors.Add("At least one world chunk is required.");
                return new LwsWorldStreamingValidationResult(false, errors);
            }

            var knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sceneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < chunks.Count; i++)
            {
                LwsWorldChunkDefinition chunk = chunks[i];
                if (chunk == null || string.IsNullOrWhiteSpace(chunk.chunkId))
                {
                    errors.Add($"Chunk at index {i} has no ID.");
                    continue;
                }

                if (!knownIds.Add(chunk.chunkId))
                {
                    errors.Add($"Duplicate chunk ID: {chunk.chunkId}");
                }

                if (!string.IsNullOrWhiteSpace(chunk.sceneName) && !sceneNames.Add(chunk.sceneName))
                {
                    errors.Add($"Duplicate chunk scene name: {chunk.sceneName}");
                }
            }

            for (int i = 0; i < chunks.Count; i++)
            {
                LwsWorldChunkDefinition chunk = chunks[i];
                if (chunk == null)
                {
                    continue;
                }

                if (!chunk.Validate(knownIds, out string chunkMessage))
                {
                    errors.Add(chunkMessage);
                }
            }

            return new LwsWorldStreamingValidationResult(errors.Count == 0, errors);
        }

        public bool TryGetChunk(string chunkId, out LwsWorldChunkDefinition chunk)
        {
            chunk = null;
            if (chunks == null || string.IsNullOrWhiteSpace(chunkId))
            {
                return false;
            }

            chunk = chunks.FirstOrDefault(c => c != null && string.Equals(c.chunkId, chunkId, StringComparison.OrdinalIgnoreCase));
            return chunk != null;
        }

        public bool TryGetChunkBySceneName(string sceneName, out LwsWorldChunkDefinition chunk)
        {
            chunk = null;
            if (chunks == null || string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            chunk = chunks.FirstOrDefault(c => c != null && string.Equals(c.sceneName, sceneName, StringComparison.OrdinalIgnoreCase));
            return chunk != null;
        }

        public LwsWorldChunkDefinition FindContainingChunk(Vector3 worldPosition)
        {
            if (chunks == null)
            {
                return null;
            }

            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i] != null && chunks[i].Contains(worldPosition))
                {
                    return chunks[i];
                }
            }

            return chunks
                .Where(c => c != null)
                .OrderBy(c => c.DistanceTo(worldPosition))
                .FirstOrDefault();
        }

        public LwsWorldChunkDefinition FindContainingChunk(LwsWorldPositionD globalPosition)
        {
            return FindContainingChunk(globalPosition.ToVector3());
        }
    }

    public readonly struct LwsWorldStreamingValidationResult
    {
        public LwsWorldStreamingValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? Array.Empty<string>();
        }

        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        public string Summary => IsValid ? "World streaming manifest is valid." : string.Join("; ", Errors);
    }
}
