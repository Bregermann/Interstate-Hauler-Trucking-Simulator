# Prompt 012 - Weather Test Matrix

| Test | Weather | Camera | Traffic | GPS | Mirrors | Performance | Status | Notes |
|---|---|---|---|---|---|---|---|---|
| Runtime compile | N/A | N/A | N/A | N/A | N/A | N/A | Automated | `dotnet build LWS.InterstateHauler.Runtime.csproj --no-restore` completed after code fix. |
| Preset validation | All | N/A | N/A | N/A | N/A | N/A | Automated | `LwsWeatherEditModeTests` validates preset IDs and profile paths. |
| Semantic snapshot clamps | All | N/A | N/A | N/A | N/A | N/A | Automated | Snapshot clamps intensity, cloud, fog, storm, visibility, and time. |
| Service transition events | Clear -> Rain | N/A | N/A | N/A | N/A | N/A | Automated | Coordinator exposes transition and precipitation events. |
| Duplicate weather service | N/A | N/A | N/A | N/A | N/A | N/A | Automated | Duplicate active coordinator initialization is rejected. |
| Adapter ownership | Rain/Snow | N/A | N/A | N/A | N/A | N/A | Automated | PlayMode fake adapter verifies one active adapter and safe detach behavior. |
| Adapter request bridge | Heavy Rain | N/A | N/A | N/A | N/A | N/A | Automated | PlayMode fake adapter verifies the requested LWS preset/profile matches the semantic snapshot. |
| Weather Maker runtime creation | Clear/default | Main gameplay camera | N/A | N/A | Mirror cameras excluded | N/A | Automated | PlayMode adapter test loads `WeatherMakerPrefab`, verifies exactly one runtime instance, day/night manager availability, camera allow-list binding, and no duplicate runtime after camera refresh. |
| Corridor weather wiring | Default | Validation observer/gameplay | Required | Required | Required | N/A | Automated | `InterstateCorridorValidation` asserts weather validation enabled, serialized Weather Maker prefab reference present, exactly one `LwsWeatherMakerAdapter`, debug panel present, active weather service attached, and no duplicate runtime on camera rebind. |
| Day/night snapshot | Noon/Midnight | N/A | N/A | GPS seam | N/A | N/A | Automated | Weather service updates `Daylight01`, `IsDay`, and `IsNight`. |
| Debug panel service lookup | N/A | N/A | N/A | N/A | N/A | N/A | Automated | PlayMode panel resolves bootstrap weather service. |
| Clear visual pass | Clear | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm sky, sun, visibility, dashboard/GPS readability. |
| Cloud transition | Partly/Cloudy/Overcast | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm smooth cloud/lighting transition. |
| Light rain | Light Rain | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm visible rain and no input/navigation regression. |
| Heavy rain | Heavy Rain | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm stronger rain and reduced visibility. |
| Thunderstorm | Thunderstorm | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm lightning/storm presentation, no console spam. |
| Light snow | Light Snow | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm falling snow only; no accumulation/traction effect. |
| Heavy snow | Heavy Snow | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm heavier snowfall and traffic visibility. |
| Fog | Fog | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm plausible visibility and road readability. |
| Sunset/night/sunrise | Clear/any | Cockpit/exterior | Required | Required | Required | Required | Manual required | Confirm sun/moon/lighting and GPS day/night theme. |
| Mirror low tier | Clear/Rain/Fog/Night | Mirror cameras | Required | N/A | Low | Required | Manual required | Confirm no inappropriate windshield overlay on mirror textures. |
| Mirror medium tier | Clear/Rain/Fog/Night | Mirror cameras | Required | N/A | Medium | Required | Manual required | Confirm no camera-stack duplication artifacts. |
| Mirror high tier | Clear/Rain/Fog/Night | Mirror cameras | Required | N/A | High | Required | Manual required | Confirm Weather Maker cost is acceptable. |
| Traffic regression | Rain/Storm/Snow/Fog/Night | Gameplay | Required | N/A | Optional | Required | Manual required | Traffic must continue spawning and driving. |
| GPS regression | Rain/Storm/Snow/Fog/Night | Cockpit | Optional | Required | Optional | Required | Manual required | Route, physical display, voice setting, and reroute must remain independent. |
| Road physics guard | Rain/Snow/Fog | Any | Optional | Optional | Optional | N/A | Automated/manual | LWS snapshot exposes input potential only; no wet/snow/ice traction added. |

Prompt 012A manual visual checks remain required in the normal Unity Editor. Automated tests verify runtime wiring and profile requests; they do not assert rendered pixels for clouds, precipitation, fog, lightning, or sun/moon presentation.

Manual validation scene:

`Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`

Use the development weather panel to cycle every weather and time state in normal Unity Editor Play Mode.
