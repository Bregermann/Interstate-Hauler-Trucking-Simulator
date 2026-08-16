# Prompt 012 - Weather Maker API Matrix

| Feature | Weather Maker Class | Method/Property | Runtime/Editor | LWS Adapter | Authority | Risk | Notes |
|---|---|---|---|---|---|---|---|
| Runtime manager | `DigitalRuby.WeatherMaker.WeatherMakerScript` | `Instance` | Runtime | `LwsWeatherMakerAdapter.ResolveWeatherMakerRuntime` | Weather Maker | Medium | Adapter can instantiate `WeatherMakerPrefab.prefab` in Editor validation if missing. |
| Runtime prefab | prefab | `Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab` | Editor/runtime asset | `InstantiateWeatherMakerPrefab` | Weather Maker | Low | Vendor prefab is referenced, not modified. |
| Profile transition | `WeatherMakerScript` | `RaiseWeatherProfileChanged(oldProfile, newProfile, transitionDuration, holdDuration, forceTransition, connectionIds)` | Runtime | `ApplyWeatherPreset` | Weather Maker | Medium | Reflection isolates DigitalRuby types from gameplay interfaces. |
| Profile loading | `WeatherMakerScript` | `LoadResource<T>(string name)` | Runtime | `LoadWeatherProfile` | Weather Maker | Low | AssetDatabase fallback is Editor-only. |
| Weather profile | `WeatherMakerProfileScript` | profile asset reference | Runtime asset | `LwsWeatherPreset.weatherMakerProfileName/path` | Weather Maker | Low | LWS presets wrap vendor profiles without editing them. |
| Cloud profiles | `WeatherMakerProfileScript` | Cloud profile fields inside asset | Runtime asset | preset mapping | Weather Maker | Medium | Visual cloud density/motion requires Editor validation. |
| Rain profiles | `WeatherMakerPrecipitationProfileScript` | precipitation profile inside weather profile | Runtime asset | preset mapping | Weather Maker | Medium | Camera rain/drop behavior must be visually checked. |
| Snow profiles | `WeatherMakerPrecipitationProfileScript` | snow profile inside weather profile | Runtime asset | preset mapping | Weather Maker | Medium | Prompt 012 uses falling snow only, not accumulation. |
| Fog profiles | `WeatherMakerProfileScript` | fog profile fields inside asset | Runtime asset | preset mapping | Weather Maker | Medium | Distance readability must be checked in corridor. |
| Wind state | `WeatherMakerWindScript` | `WindIntensity`, `CurrentWindVelocity` | Runtime | semantic snapshot values | Weather Maker/LWS | Low | Prompt 012 exposes semantic wind; it does not push vehicles. |
| Lightning | `WeatherMakerProfileScript` / lightning manager components | lightning profile fields | Runtime | thunderstorm preset semantic state | Weather Maker | Medium | Visual flash/thunder timing must be checked in Editor. |
| Performance profile | `WeatherMakerScript` | `PerformanceProfile` | Runtime | `ApplyProjectQualityTier` | Weather Maker | Medium | Missing profile reports adapter status instead of guessing. |
| Camera allow list | `WeatherMakerScript` | `AllowCameras` | Runtime | `BindActiveCamera` | Weather Maker/LWS | High | Mirror/render texture cameras are deliberately skipped. |
| Day/night manager | `WeatherMakerDayNightCycleManagerScript` | `Instance` | Runtime | `ResolveDayNightManager` | Weather Maker | Medium | Requires runtime Weather Maker manager. |
| Time of day | `WeatherMakerDayNightCycleManagerScript` | `TimeOfDay` seconds | Runtime | `ApplyTimeOfDay` | Weather Maker | Low | LWS uses hours; adapter converts to seconds. |
| Time speed | `WeatherMakerDayNightCycleManagerScript` | `Speed`, `NightSpeed` | Runtime | `ApplyTimeScale` | Weather Maker | Low | Development controls set paused/1x/10x/60x. |
| Weather zones | `WeatherMakerWeatherZoneScript` | `SingleProfile`, `ProfileGroup`, `TransitionDuration` | Runtime | not used directly | Weather Maker | Low | Prompt 012 uses global world weather, not per-zone weather. |
| URP support | Weather Maker URP scripts/docs | installed package docs/source | Editor/runtime | validator audit | Weather Maker | Medium | Normal Editor verification still required with URP 17.4. |
| Audio | Weather Maker audio components | profile sound fields | Runtime | deferred mixer seam | Weather Maker | Medium | Prompt 044 owns production audio routing. |
