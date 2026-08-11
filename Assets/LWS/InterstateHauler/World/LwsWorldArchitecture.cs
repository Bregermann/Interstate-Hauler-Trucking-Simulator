using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsWorldGenerationStep
    {
        WorldChunkDefinition,
        VistaTerrainAndBiomes,
        EasyRoadsGeometry,
        RoadGraphExport,
        RoadsideSpawning,
        Vegetation,
        Navigation,
        Traffic,
        WeatherRegions,
        SceneStreamingValidation
    }

    [Serializable]
    public sealed class LwsWorldGenerationPlan
    {
        public string planId = "default";
        public List<LwsWorldGenerationStep> orderedSteps = new List<LwsWorldGenerationStep>
        {
            LwsWorldGenerationStep.WorldChunkDefinition,
            LwsWorldGenerationStep.VistaTerrainAndBiomes,
            LwsWorldGenerationStep.EasyRoadsGeometry,
            LwsWorldGenerationStep.RoadGraphExport,
            LwsWorldGenerationStep.RoadsideSpawning,
            LwsWorldGenerationStep.Vegetation,
            LwsWorldGenerationStep.Navigation,
            LwsWorldGenerationStep.Traffic,
            LwsWorldGenerationStep.WeatherRegions,
            LwsWorldGenerationStep.SceneStreamingValidation
        };
    }

    public interface ILwsWorldGenerationCoordinator : ILwsService
    {
        LwsWorldGenerationPlan CurrentPlan { get; }
    }

    public sealed class LwsWorldGenerationCoordinator : ILwsWorldGenerationCoordinator
    {
        public string ServiceId => "lws.world.generation";
        public LwsWorldGenerationPlan CurrentPlan { get; private set; }

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            CurrentPlan = new LwsWorldGenerationPlan();
            return LwsServiceResult.Success("LWS world generation coordinator initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            CurrentPlan = null;
            return LwsServiceResult.Success("LWS world generation coordinator shut down.");
        }
    }

    public interface ILwsWorldStreamingService : ILwsService
    {
        string ActiveWorldChunkId { get; }
        void SetActiveWorldChunk(string chunkId);
    }

    public sealed class LwsWorldStreamingService : ILwsWorldStreamingService
    {
        public string ServiceId => "lws.world.streaming";
        public string ActiveWorldChunkId { get; private set; }

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            ActiveWorldChunkId = string.Empty;
            return LwsServiceResult.Success("LWS world streaming service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ActiveWorldChunkId = string.Empty;
            return LwsServiceResult.Success("LWS world streaming service shut down.");
        }

        public void SetActiveWorldChunk(string chunkId)
        {
            ActiveWorldChunkId = chunkId ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsSceneStreamerAdapter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour sceneStreamerBehaviour;

        public bool LoadScene(string sceneName)
        {
            return InvokeSceneStreamer("LoadScene", sceneName);
        }

        public bool UnloadScene(string sceneName)
        {
            return InvokeSceneStreamer("UnloadScene", sceneName);
        }

        public bool ClearAll()
        {
            Type type = ResolveSceneStreamerType();
            MethodInfo method = type?.GetMethod("UnloadAll", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                return false;
            }

            method.Invoke(null, null);
            return true;
        }

        private bool InvokeSceneStreamer(string methodName, string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            object instance = sceneStreamerBehaviour;
            MethodInfo instanceMethod = instance?.GetType().GetMethod(methodName.Replace("Scene", string.Empty), BindingFlags.Instance | BindingFlags.Public);
            if (instanceMethod != null)
            {
                instanceMethod.Invoke(instance, new object[] { sceneName });
                return true;
            }

            Type type = ResolveSceneStreamerType();
            MethodInfo staticMethod = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (staticMethod == null)
            {
                return false;
            }

            staticMethod.Invoke(null, new object[] { sceneName });
            return true;
        }

        private static Type ResolveSceneStreamerType()
        {
            return Type.GetType("PixelCrushers.SceneStreamer, Assembly-CSharp-firstpass")
                ?? Type.GetType("PixelCrushers.SceneStreamer, Assembly-CSharp");
        }
    }
}
