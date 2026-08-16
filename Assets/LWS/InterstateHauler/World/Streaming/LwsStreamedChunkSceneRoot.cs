using System;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsStreamedChunkSceneRoot : MonoBehaviour, ILwsStreamedChunkParticipant
    {
        private const string NeighboringScenesTypeName = "PixelCrushers.SceneStreamer.NeighboringScenes";

        [SerializeField] private string chunkId = "IH_TEST_CHUNK_UNASSIGNED";
        [SerializeField] private string sceneName = "IH_Chunk_Unassigned";
        [SerializeField] private string[] neighborSceneNames = Array.Empty<string>();
        [SerializeField] private bool synchronizeSceneStreamerNeighbors = true;

        public string ChunkId => chunkId;
        public string SceneName => sceneName;
        public string[] NeighborSceneNames => neighborSceneNames;

        private void Awake()
        {
            if (synchronizeSceneStreamerNeighbors)
            {
                EnsureSceneStreamerNeighborMetadata();
            }
        }

        public void OnChunkLoaded(LwsWorldChunkRuntimeState state)
        {
        }

        public void OnChunkActivated(LwsWorldChunkRuntimeState state)
        {
        }

        public void OnChunkDeactivating(LwsWorldChunkRuntimeState state)
        {
        }

        public void OnChunkUnloaded(LwsWorldChunkRuntimeState state)
        {
        }

        private void EnsureSceneStreamerNeighborMetadata()
        {
            Type neighboringScenesType = ResolveType(NeighboringScenesTypeName);
            if (neighboringScenesType == null)
            {
                return;
            }

            Component component = GetComponent(neighboringScenesType);
            if (component == null)
            {
                component = gameObject.AddComponent(neighboringScenesType);
            }

            FieldInfo sceneNamesField = neighboringScenesType.GetField("sceneNames", BindingFlags.Instance | BindingFlags.Public);
            if (sceneNamesField != null)
            {
                sceneNamesField.SetValue(component, neighborSceneNames ?? Array.Empty<string>());
            }
        }

        private static Type ResolveType(string typeName)
        {
            Type type = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Assembly-CSharp");
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
