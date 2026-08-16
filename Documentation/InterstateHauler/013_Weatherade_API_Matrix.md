# Prompt 013 - Weatherade API Matrix

| Area | Weatherade Class | Member | LWS Usage | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| Namespace | `NOT_Lonely.Weatherade` | runtime namespace | Resolved by reflection in `LwsWeatheradeAdapter` | Available | No LWS runtime hard reference. |
| Package version | n/a | `CurrentVersion.txt` | Audit reference | Available | Installed version is `1.1.8`. |
| Base coverage | `CoverageBase` | `instance` | Detect existing coverage singleton | Available | Existing non-owned singleton is preserved. |
| Base coverage | `CoverageBase` | `areaSize` | Visual quality scaling | Available | Adjusted by `ApplyQualityTier`. |
| Base coverage | `CoverageBase` | `areaDepth` | Visual quality scaling | Available | Adjusted by `ApplyQualityTier`. |
| Base coverage | `CoverageBase` | `useFollowTarget` | Player-following coverage | Available | Set true by adapter. |
| Base coverage | `CoverageBase` | `followTarget` | Follow active player truck/camera | Available | Player truck preferred. |
| Base coverage | `CoverageBase` | `depthLayerMask` | Coverage depth filtering | Available | Serialized on adapter. |
| Base coverage | `CoverageBase` | `UpdateCoverageMaterials()` | Push coverage values to materials | Available | Called after value changes. |
| Rain visuals | `RainCoverage` | `wetnessAmount` | Map LWS wetness | Available | `wetness01 -> wetnessAmount`. |
| Rain visuals | `RainCoverage` | `puddlesAmount` | Map standing water | Available | `standingWater01 -> puddlesAmount`. |
| Rain visuals | `RainCoverage` | `ripplesAmount` | Standing-water ripple count | Available | Integer value derived from standing water. |
| Rain visuals | `RainCoverage` | `ripplesIntensity` | Standing-water ripple intensity | Available | Derived from standing water. |
| Rain visuals | `RainCoverage` | `spotsIntensity` | Wet-road spots | Available | Derived from wetness. |
| Rain visuals | `RainCoverage` | `dripsIntensity` | Wet-road drips | Available | Derived from wetness. |
| Snow visuals | `SnowCoverage` | `coverageAmount` | Map snow/packed snow/ice visual coverage | Available | Uses max of snow, packed snow, and partial ice. |
| URP support | package file | `WeatheradeSRS_URP_17_1.unitypackage` | Manual vendor setup option | Present locally | Not imported/enabled by Prompt 013. |
| URP define | scripting define | `USING_URP` | Weatherade URP compilation path | Not enabled | Must be validated in normal Unity Editor if imported. |
| Render feature | resource asset | `SRS_DepthRenderer.asset` | Depth rendering support | Not found | Expected only after URP support setup. |

## Integration Notes

`LwsWeatheradeAdapter` deliberately uses reflection to avoid making Weatherade a compile-time dependency of the LWS road condition service. The core road condition model does not contain `NOT_Lonely`, Weather Maker, or NWH symbols.
