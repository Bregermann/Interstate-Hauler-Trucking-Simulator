using System;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsStreamedChunkSceneRoot : MonoBehaviour, ILwsStreamedChunkParticipant, ILwsFloatingOriginParticipant
    {
        private const string NeighboringScenesTypeName = "PixelCrushers.SceneStreamer.NeighboringScenes";

        [SerializeField] private string chunkId = "IH_TEST_CHUNK_UNASSIGNED";
        [SerializeField] private string sceneName = "IH_Chunk_Unassigned";
        [SerializeField] private string[] neighborSceneNames = Array.Empty<string>();
        [SerializeField] private bool synchronizeSceneStreamerNeighbors = true;

        private ILwsWorldOriginService _originService;
        private bool _originRegistered;

        public string ChunkId => chunkId;
        public string SceneName => sceneName;
        public string[] NeighborSceneNames => neighborSceneNames;
        public string ParticipantId => $"chunk.{chunkId}";
        public LwsFloatingOriginParticipantKind ParticipantKind => LwsFloatingOriginParticipantKind.LoadedChunkRoot;
        public Transform ParticipantTransform => transform;
        public bool AlignToCurrentOriginOnRegistration => true;

        private void OnEnable()
        {
            RegisterFloatingOriginParticipant();
        }

        private void Awake()
        {
            if (synchronizeSceneStreamerNeighbors)
            {
                EnsureSceneStreamerNeighborMetadata();
            }
        }

        private void Start()
        {
            RegisterFloatingOriginParticipant();
        }

        private void OnDisable()
        {
            if (!_originRegistered)
            {
                return;
            }

            _originService?.UnregisterParticipant(this);
            _originRegistered = false;
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

        public bool ApplyOriginShift(LwsOriginShiftEvent shiftEvent, out string message)
        {
            transform.position += shiftEvent.LocalTranslationDelta;
            message = $"{ChunkId} chunk root shifted by {shiftEvent.LocalTranslationDelta}.";
            return true;
        }

        private void RegisterFloatingOriginParticipant()
        {
            if (_originRegistered)
            {
                return;
            }

            if (_originService == null &&
                LwsApplicationBootstrap.Instance != null &&
                LwsApplicationBootstrap.Instance.Registry != null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
            }

            if (_originService == null)
            {
                return;
            }

            _originService.RegisterParticipant(this);
            _originRegistered = true;
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
