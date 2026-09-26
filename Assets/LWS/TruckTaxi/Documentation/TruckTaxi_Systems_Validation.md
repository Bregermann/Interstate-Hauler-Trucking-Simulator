# Truck Taxi Systems Validation

## Scope

This pass extends the existing Compass presentation, UTS population, NWH steering presentation, Heat UI, LWS clock/weather semantics, Weather Maker and Pixel Crushers passenger dialogue. Passenger voice-reference assignment and generation remain deferred. No source voice WAVs were modified.

## Validation Conditions

- Unity 6000.4.10f1, Windows development player, 1920x1080.
- Standalone probes start only with explicit `-truck-taxi-*-smoke` arguments. They run isolated test shifts, not player career saves.
- Hidden standalone launches have no reliable swap-chain screenshot. Captures render the actual gameplay camera, existing map cameras and existing canvases to a temporary RenderTexture. They are player-rendered evidence, not manual driving or a screen recording.
- Keyboard and gamepad probes inject real Input System device state. No physical gamepad/wheel hardware was used.
- Evidence is in `Builds/TruckTaxiDemo/Validation`. Compile, automated assertions, visual inspection and audible listening are reported separately.

## Implemented Features

- Typed Compass icons shared by cab GPS, HUD GPS and offer preview, discovered-shortcut state, active/known/completed/optional styles. No replacement map or route authority.
- Once-per-offer notification claim; Heat notification clip; top-origin clockwise-depleting stroke ring and number driven by the same session offer deadline. Keyboard/gamepad accept/decline preserved.
- Taxi-only 12x pedestrian / 5x traffic profile: measured starting population 12/12, requested and reached 144/60. Existing UTS placement, AI and ragdoll lifecycle retained.
- One smoothed cockpit steering-wheel writer using NWH resolved steering, 900 degrees lock-to-lock, 0.16s steer response, 0.23s return, 900 degrees/s cap. No steering-physics retune.
- Existing semantic game clock, 30 game seconds per real second, 16:00 start, tunable automatic weather and manual environment controls.
- Clock-driven bladder progression over 4-8 game hours; real restroom eligibility, optional jug QTE, filled-jug disposal, cleanup and nonfatal crisis consequences. Passenger reactions use existing dialogue categories/fallback text.
- Audio routing/settings implementation prepared for Voices, Vehicle, World and UI under Master. Actual mixer setup remains pending below.

## Evidence So Far

| Validation | Result |
| --- | --- |
| C# compilation / Windows build | Passed; final fog-corrected Windows build succeeded with 0 errors, 451,893,850 bytes. Taxi resource container excludes unused vendor water shaders; vendor files unchanged |
| Final EditMode suite | 188 passed, 0 failed, 2 intentionally skipped external voice tests; 190 total, 15.17s |
| Shared weather EditMode regressions | 9 passed, 0 failed after the fog transition correction; `FogCorrectionEditMode.json` |
| PlayMode integration | All 7 passed in one 171.35s run: rides, low-speed NWH U-turn, no-mouse objectives/stops, actual pedestrian collision/ragdoll replenishment, dense population, steering ownership, GPS and weather lifecycle |
| GPS PlayMode follow-up | Passed after correcting route/icon widths for the actual map RectTransform units; visible cab and HUD routes reviewed |
| Weather lifecycle PlayMode | One weather instance across scene reload/re-enable passed; final cache/sky follow-up also passed 1/1 in 5.96s |
| Windows offer | Full/reset/expiry ring state, single notification claim, keyboard/gamepad accept/decline passed; notification listening not claimed |
| Windows map | Corrected Compass UV-space fixture and real pickup/destination/scenic/illicit/shortcut/traffic cases passed, including offscreen clipping/re-entry and all-type cab/HUD/offer sprite checks. Screenshots reviewed; dense showcase is crowded on the small physical screen |
| Windows environment/needs first run | Clock progression/freeze/pause, semantic weather transitions, restroom relief, keyboard/gamepad jug success, spill, disposal, cleanup, nonfatal crisis and passenger reactions passed |
| Weather visuals | Final Windows captures confirm orange sunset, dark night sky with visible roads, storm rain, and fog haze over distant roads/buildings. Light-rain requests pass semantically, but the captures do not establish useful light-rain particle visibility |
| Windows steering | Passed bounded signed motion, left/right, release, rapid/slow taps, exclusive writer and unchanged handling at 30/60/120 simulated frame steps. Timed cockpit PNG sequence captured and inspected |
| Windows dense ragdolls | Actual two tractor impacts, finite bone impulses, once-only scoring/reactions, lifetime cleanup and same-UTS replenishment passed; process exit 0, `WindowsPedestrianDenseFinal.log` |

`WindowsSystemsFogCorrection.log`: **227 checks passed, 0 failed**, process exit 0. This repeats the offer, map, environment and needs probes and adds a settled vendor-fog-density assertion. The vendor transition now retains 0.01591 instead of overwriting it with the profile's 0.00175. The adapter passes the target through public `ShowFogAnimated`, not a competing per-frame override. `Windows_Environment_fog.png` visibly confirms distant haze compared with the light-rain capture. Earlier `WindowsSystemsFinal.log` retains the 226-check pre-correction result. These results do not turn pending audio listening or unconfirmed light-rain particles into passes.

Steering simulated frame steps are not achieved hardware FPS. These tests do not prove a human-driven corner, hardware wheel feel or comparison against an unavailable original video. The keyboard fixture can move the vehicle under existing automatic reverse semantics; it does not freeze or rewrite its Rigidbody.

## First Windows Population Measurements

Both runs use the same development build and 20-second samples after population settles. No Editor or second player ran during samples. Full requested counts were retained.

| Case | Pedestrians | Traffic | Mean frame | P95 frame | FPS | Main thread | Physics.Simulate |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Baseline | 12 | 12 | 54.78ms | 67.59ms | 18.25 | 54.13ms | 0.57ms |
| Dense first run | 144 | 60 | 115.86ms | 142.84ms | 8.63 | 113.98ms | 6.57ms |

The first result was not acceptable final performance. A live diagnostic sample located most of the time in script Update (80.05ms of a 94.92ms frame), not animation (0.44ms) or skinning (0.07ms). The weather adapter was scanning every project Component for diagnostics three times each frame. It now caches known runtime managers, uses type-specific discovery only when necessary, and refreshes diagnostic strings at 1Hz. Runtime root changes/destroyed references invalidate the cache. Native population LOD/offscreen-animation/distant-shadow settings also reduce presentation cost without removing actors or changing UTS AI.

Final rebuilt-player samples, same machine and methodology:

| Case | Pedestrians | Traffic | Mean frame | P95 frame | FPS | Main thread | Physics.Simulate | Traffic moving >2m |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Baseline final | 12 | 12 | 3.98ms | 5.04ms | 251.53 | 3.93ms | 0.046ms | 12/12 |
| Dense final | 144 | 60 | 8.51ms | 11.61ms | 117.51 | 8.42ms | 0.456ms | 60/60 |

Both player processes exited 0. These are uncapped development-player measurements on this machine, not a promise for all hardware. Physics figures are averaged per rendered frame, not per fixed simulation step. Original JSON samples are retained as `Population_*_BeforeOptimization.json`; final samples are `Population_Baseline.json` / `Population_Dense.json`.

No silent multiplier reduction. Spawn rejection count was 0 pedestrian / 16 traffic attempts in the final dense run; bounded retries still reached 60 cars. There is no existing reusable vendor pool to report as exhausted. Ragdoll population slots replenish and observed fallen bodies are capped.

## Audio Limitation

`Assets/LWS/TruckTaxi/Audio/TruckTaxi.mixer` still needs authoring in Unity's Audio Mixer window. Supported automation, including the added Unity Essentials workflow, does not expose mixer group creation or gain-parameter exposure. No private mixer API or handwritten mixer YAML was used.

Required groups: Master with Voices, Vehicle, World and UI children. Exposed Volume names: `MasterVolume`, `VoicesVolume`, `VehicleVolume`, `WorldVolume`, `UIVolume`, initially 0dB. Run `Truck Taxi/Configure Presentation Assets` afterward.

Until then, the existing Audio Settings panel honestly reports pending setup, without inert sliders. Master/category gain, audible isolation, silent voices with continuing subtitles, audio preference restart and no-mouse slider validation are NOT passed. The notification clip and weather audio still play through their existing paths; complete player volume control is not delivered yet.

See `TruckTaxi_AudioAudit.md`, `TruckTaxi_MapMarkerLegend.md`, `TruckTaxi_OfferCountdown.md`, `TruckTaxi_Population_and_Steering.md`, `TruckTaxi_TimeWeatherAssetAudit.md` and `TruckTaxi_TimeWeather_DriverNeeds.md` for file locations, configuration, control bindings and system boundaries.

## Vendor Integrity

- VENDOR ASSETS AUDITED: Compass Navigator Pro 4, UTS, NWH Vehicle Physics 2, Heat, Weather Maker, Pixel Crushers, existing LWS clock/weather/input adapters.
- VENDOR ASSETS USED: Those existing presentation, simulation, UI, weather and dialogue authorities.
- RELEVANT ASSETS NOT USED: No new sky package, spawner, minimap, input backend, career save framework or audio engine. No usable Taxi windshield wiper setup/current streetlight rig was found; no speculative replacement was built.
- CUSTOM SYSTEMS CREATED: Taxi-specific semantic icon mapping, environment/needs coordination, density configuration, visual steering smoothing, view-only offer timer and audio settings/routing adapters; scoped setup and opt-in tests.
- VENDOR SOURCE MODIFIED: NO by this pass. Unrelated pre-existing working-tree edits are preserved.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
