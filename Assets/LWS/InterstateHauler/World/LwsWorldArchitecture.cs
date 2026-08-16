using System;
using System.Collections.Generic;
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

}
