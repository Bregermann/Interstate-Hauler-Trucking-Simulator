# Prompt 015B - UI Test Matrix

| Area | Automated Coverage | Manual Editor Validation |
| --- | --- | --- |
| Development UI service | Default registry includes one `ILwsDevelopmentUiService` | Confirm one runtime UI instance in validation scenes |
| Canvas | PlayMode verifies Canvas, CanvasScaler, ScrollRect exist | Confirm Screen Space Overlay scales at 16:9, 16:10, ultrawide |
| Tabs | EditMode validates 13 unique tab IDs | Confirm each tab scrolls and text does not overlap |
| Legacy panels | EditMode verifies project-owned legacy IMGUI panels default hidden | Confirm old panels do not overlap the new overlay by default |
| F1 control center | PlayMode verifies service show/hide/open-tab behavior | Press F1 in Truck, corridor, streaming, and 50-mile scenes |
| Full map | PlayMode verifies big map visibility and pause restore | Press M, use MAP button, close with Escape |
| Semantic map | EditMode/PlayMode verifies road graph and route presentation are cached | Confirm minimap shows roads, route, player, and destination |
| Origin safety | PlayMode simulates an origin offset and keeps route presentation active | Drive/teleport in 50-mile scene across chunk/origin changes |
| GPS route | PlayMode seeds a 50-mile route into the map | Use START TEST ROUTE and ROUTE TO MILE 50 |
| Weather controls | Validator checks use of `ILwsWeatherService.RequestWeather` | Press Clear/Rain/Snow/Fog and observe Weather Maker state |
| Road conditions | Validator checks use of `ILwsRoadConditionService` | Toggle Auto/Dry/Wet/Snow/Ice and observe road condition panel/state |
| Transmission | Validator checks `TrySetDevelopmentAutomaticTestMode` | Switch Automatic/manual and verify Prompt 006 authority remains intact |
| Truck controls | Validator checks `ApplyCommandFrame` semantic routing | Use truck buttons and verify no direct NWH transmission bypass |
| Streaming | UI exposes freeze/load/unload/reload actions | Use in StreamingHighwayValidation |
| 50-mile test | UI exposes route/teleport/weather-cycle actions | Use in IH_50MileFloatingOriginValidation |

## Current Validation Status

Automated tests cover the non-hardware logic and semantic data flow. Final visual polish and actual driving-map interaction should be verified in the normal Unity Editor because the previous command-line batchmode path is known unreliable for this project.
