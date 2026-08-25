# Weatherade API Matrix

| Feature | Exact Vendor Class | Property/Method/Event | Vendor Prefab/Profile | Required Scene Components | How Interstate Uses It |
|---|---|---|---|---|---|
| Coverage base | NOT_Lonely.Weatherade.CoverageBase | areaSize, areaDepth, depthLayerMask, useFollowTarget, followTarget, UpdateCoverageMaterials() | BasicSetupRain/Snow sample scenes | One active CoverageBase-derived instance | LwsWeatheradeAdapter configures coverage area and follows the player truck/camera. |
| Rain wetness | NOT_Lonely.Weatherade.RainCoverage | wetnessAmount | SRS_RainCoverageInstance sample | RainCoverage active | LWS wetness01 drives visible wetness. |
| Rain puddles | NOT_Lonely.Weatherade.RainCoverage | puddlesAmount | SRS_RainCoverageInstance sample | RainCoverage active and compatible material | LWS standingWater01 drives puddle amount. |
| Rain ripples | NOT_Lonely.Weatherade.RainCoverage | ripplesAmount, ripplesIntensity | SRS_RainCoverageInstance sample | RainCoverage active and compatible material | LWS standingWater01 drives ripple count/intensity hints. |
| Rain spots/drips | NOT_Lonely.Weatherade.RainCoverage | spotsIntensity, dripsIntensity | SRS_RainCoverageInstance sample | RainCoverage active and compatible material | LWS wetness01 drives visible rain surface effects. |
| Snow accumulation | NOT_Lonely.Weatherade.SnowCoverage | coverageAmount | SRS_SnowCoverageInstance sample | SnowCoverage active and compatible material | LWS snowDepth01/packedSnow01/ice01 drive snow coverage amount. |
| Rain mesh shader | Unity shader | NOT_Lonely/Weatherade/Rain Coverage | Assets/NOT_Lonely/Weatherade SRS/Shaders/RainCoverage/MeshRender/RainCoverage.shader | MeshRenderer road materials | LwsWeatheradeMaterialFactory creates rain-compatible LWS runtime road materials. |
| Snow mesh shader | Unity shader | NOT_Lonely/Weatherade/Snow Coverage | Assets/NOT_Lonely/Weatherade SRS/Shaders/SnowCoverage/MeshRender/SnowCoverage.shader | MeshRenderer road materials | Adapter swaps LwsRoadSurface renderers to snow-compatible runtime materials during snow conditions. |
| Depth setup | NOT_Lonely.Weatherade.CoverageBase | internal depth camera/material workflow | BasicSetupRain.unity, BasicSetupSnow.unity | Coverage area and layer mask | Adapter configures area/depth/layer mask and relies on Weatherade's vendor depth workflow. |
| Runtime refresh | NOT_Lonely.Weatherade.CoverageBase | UpdateCoverageMaterials() | RainCoverage/SnowCoverage components | Compatible materials in loaded scenes | Called whenever semantic road condition changes or origin shifts. |