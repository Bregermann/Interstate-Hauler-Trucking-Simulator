# Truck Taxi Time, Weather, and Driver Needs

## Status and Ownership

Implemented in the existing Truck Taxi runtime, not a new environment, input, save, or UI framework. The completed validation results and their limits are recorded below. Sunset/night and storm-rain visuals were confirmed in Windows; audio and light-rain visibility remain unverified, and the fog transition correction is in progress.

- `ILwsGameClockService` remains the sole elapsed-game-time authority. No wall-clock time and no second clock tick loop are introduced.
- Existing `ILwsWeatherService` and `LwsWeatherMakerAdapter` own semantic weather requests and the vendor connection. Weather Maker owns atmospheric presentation.
- `TruckTaxiEnvironmentCoordinator` supplies Taxi policy, scheduling, and reactions only.
- `TruckTaxiDriverNeedsState` owns session pressure, jug state, and consequences. Its coordinator connects clock events, the player, existing dialogue, and GPS.
- `TruckTaxiEnvironmentNeedsPanel` uses the existing `TruckTaxiHud` Canvas, Heat buttons/sliders, TMP, and UI Input System action map. It creates no Canvas.
- Steering, NWH physics, Weatherade accumulation/grip, traffic, and commercial-game persistence are not replaced here.

## Setup and Authoring

Use `Truck Taxi/Update Time Weather And Driver Needs`, implemented by `LWS.TruckTaxi.Editor.TruckTaxiEnvironmentSetup.UpdateScene()`. It updates `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity` through Unity Editor APIs without rebuilding the city. `ConfigureScene(TruckTaxiBootstrap)` supports an already-open scene and does not save/open it itself.

The setup reuses or creates:

- `Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_EnvironmentSettings.asset`
- One `TruckTaxiEnvironmentCoordinator` and one `TruckTaxiDriverNeedsCoordinator` on the existing host.
- One configured, initially disabled `LwsWeatherMakerAdapter` using `Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_WeatherMaker.prefab`.
- Two restroom components on existing authored ride locations, plus project-owned restroom signs.
- An optional sunset bonus on the first existing scenic stop.
- Taxi-only URP assets described below.

Change normal defaults in the settings asset Inspector. Existing settings values are retained by setup. Runtime debug changes are not a saved career preference; set the asset outside Play Mode for enduring defaults. Individual restroom bounds/capabilities and scenic bonus conditions are authored on their scene components.

Bootstrap initializes environment after the player/HUD exist, then initializes needs with that environment and initializes the HUD subpanel. Public hooks:

```csharp
environment.Initialize(host);
driverNeeds.Initialize(host, environment);
panel.Initialize(host, host.hud, environment, driverNeeds);
environment.SetSessionPaused(paused);
environment.WeatherAudioRootAvailable += audio.RouteWorldTree;
```

The integrated host exposes `Environment` and `DriverNeeds`; its HUD exposes `EnvironmentNeeds`. Initialization is guarded against duplicate setup. Bathroom discovery is scene-scoped and cached once. Proximity updates inspect only that cache at 5 Hz.

## Clock Defaults

| Setting | Default / behavior |
| --- | --- |
| Starting time | 16:00 |
| Time scale | 30 game seconds per real second |
| Full day | 48 real minutes, excluding pause/freeze |
| Time progression | Enabled |
| Pause | Existing Taxi pause and explicit freeze stop the authoritative clock |
| Set time | Changes displayed time of day; does not manufacture elapsed bladder time |
| Advance one hour | Adds real semantic elapsed game time and therefore advances needs |

Periods are semantic labels, not hard lighting switches: Late Night 00:00-05:00, Dawn 05:00-07:00, Morning 07:00-11:00, Day 11:00-14:00, Afternoon 14:00-18:00, Sunset 18:00-20:00, Night 20:00-24:00. Weather Maker follows continuous LWS time. Its independent clock speed is set to zero while slaved.

The existing adapter ticks the clock only when no `LwsGameClockCoordinator.ActiveInstance` exists. The Taxi coordinator never adds another ticking authority. The small HUD band shows time/period and bladder pressure.

## Weather Defaults and Rendering Scope

Starting weather is `clear`. Automatic progression defaults ON. A condition holds 2-5 in-game hours; transitions request 25 real simulation seconds through the existing weather service/adapter. The scheduler uses elapsed game time, excludes the current preset, and skips invalid/nonpositive weighted entries. Manual Force Weather disables automatic scheduling until re-enabled; Next Weather reevaluates eligible weights. Freezing game time stops scheduling, not necessarily an already-running visual blend; normal pause also stops scaled presentation progress.

| Preset | Default weight |
| --- | ---: |
| `clear` | 4 |
| `partly_cloudy` | 3 |
| `overcast` | 2 |
| `light_rain` | 2 |
| `heavy_rain` | 1 |
| `thunderstorm` | 0.5 |
| `fog` | 1 |

The existing `cloudy` preset is also accepted when explicitly authored. Snow is not exposed: vendor snow assets exist, but this Taxi world has no validated snow surface/accumulation authoring. There is no new slippery-road force or tire tuning.

Setup copies the existing PC renderer and medium Interstate pipeline into:

- `Assets/LWS/TruckTaxi/Rendering/TruckTaxi_Weather_Renderer.asset`
- `Assets/LWS/TruckTaxi/Rendering/TruckTaxi_Weather_RPAsset.asset`

It adds the installed public Weather Maker URP render feature to the Taxi renderer and points only the copied pipeline's first renderer at it. Other copied renderer entries, including the existing depth renderer, remain intact. It does not edit shared Interstate pipelines, the original PC renderer, Graphics Settings, Quality Settings assets, or vendor source.

While Taxi runs, its coordinator captures `QualitySettings.renderPipeline`, applies the Taxi pipeline at runtime before enabling its adapter, and restores the captured value on destruction if Taxi still owns that assignment. The Weather Maker prefab copy has `IsPermanent = false`; it is scene-scoped. The original vendor prefab retains its own authored lifetime. There must be one active Weather Maker runtime, not one per consumer.

The shared adapter's lifecycle fix preserves the just-instantiated component while Unity activates its scene, accepts valid runtime scenes before `scene.isLoaded` turns true, and respects the prefab's lifetime instead of forcing permanence. This prevents its immediate quality request from instantiating a second vendor singleton. `WeatherMakerRuntimeRoot` exposes the cached actual root for audio, without a repeating name search.

Weather Maker remains responsible for sun/moon, sky, clouds, fog, precipitation, lightning and weather audio blending. See `TruckTaxi_TimeWeatherAssetAudit.md` for source assets and rendering caveats. A successful semantic request does not prove the pixels or sound are correct.

## Night Lighting and Windshield Limitations

`nightLights` accepts explicit authored `Light` references and enables them during Dawn, Night, and Late Night; original enabled states are restored on exit. Do not assign Weather Maker sun/moon, traffic signals, or truck lights to this collection.

The audited Taxi builder creates a basic sun/ambient setup but no usable authored street/building night-light rig. The current night-light list is therefore not a claim of a new illuminated-city rig. Existing traffic lights and vehicle controls retain their own owners. Live PlayMode and final Windows captures confirmed the corrected sunset/night sky; the night view retained visible streets. This confirms those captured views, not every route or weather combination. No fake full-screen darkening is added.

Existing LWS truck controls contain wiper semantics, but no reliable physical wiper animation or current-truck rain-on-glass presentation was found in this focused source/prefab inspection. This pass does not invent or claim a working windshield system. Physical wiper/glass work remains a separate cab task.

## Weather Audio

The adapter's cached runtime root is exposed as `WeatherAudioRoot` and through `WeatherAudioRootAvailable`. The audio integration calls `TruckTaxiAudioController.Instance.RouteWorldTree(root)`; that controller owns routing of existing and subsequently created child AudioSources to the World category. The environment coordinator also performs a single idempotent registration when the controller becomes available. No independent ambient audio bus or per-frame scene-wide source search is introduced.

A configured mixer/group assignment is required. The development probe reports routing as UNVERIFIED when the audio controller is not ready; it must not label missing mixer setup as audible success. Audio validation remains PENDING: crossfade quality, rain/thunder audibility, and player-volume response have not been confirmed by a listening test. Passing automated checks is not an audible-volume pass.

## Bladder and Consequences

Pressure starts at 0 and samples a 4-8 in-game-hour empty-to-full cycle after each relief. At 30x this is roughly 8-16 real driving minutes. It grows only during Available, DrivingToPickup, or DrivingToDestination, while gameplay is unpaused. Menus, inactive shifts, paused/frozen time, and display-only time changes do not age it.

| State | Pressure |
| --- | --- |
| Fine | below 25% |
| Building | 25% to below 65% |
| NeedToGo | 65% to below 85% |
| Urgent | 85% to below 100% |
| Crisis | 100% |

Urgency emits once per relief cycle. At full pressure, a 20-game-minute grace period precedes a nonfatal accident: cab mess, a crisis counter/event, pressure reduced to 15%, feedback, and an optional passenger reaction. No game over, teleport, loss of driving control, or graphic bodily-fluid presentation is added.

Jug spills leave pressure at 20% and set cab mess. Both spills and crisis accidents call existing `ApplyMechanicReward(5, -spillCleanupCostCents)`, default 500 cents. That existing API applies only while a passenger is onboard and clamps its own ride adjustments. This is a ride fare/chaos consequence, not a new wallet or guaranteed cash debit on an empty shift. Cleaning the cab clears the mess and does not charge the same incident again.

## Restroom Locations

These are actual current scene-authored stopping areas, not globally available relief buttons:

| Location | Stable restroom ID | Current stop position (Unity world metres) |
| --- | --- | --- |
| Wrong Turn Diner Restroom | `taxi.stop.01.restroom` | `(-307, 0.16, -100)` |
| Corner Market Restroom | `taxi.stop.07.restroom` | `(-147, 0.16, 220)` |

Both reuse their `TruckTaxiRideLocation.Truck Stop`, currently with zero local offset. `TruckTaxiBathroomPoint.Position` reads that actual stop transform, so moving the authored stop moves the service target; the coordinates above are not a runtime authority. Both currently allow jug disposal and cab cleanup, with radius 11 metres and vertical tolerance 3 metres. Maximum use speed defaults to 0.447 m/s (approximately 1 MPH).

ROUTE TO RESTROOM chooses the nearest enabled, configured cached point by distance and calls the existing GPS `SetServiceDestination(stableId, displayName, position)`. Compass/LWS navigation remain the map/route authorities. USE BATHROOM validates current position/speed, relieves pressure to zero, and restores the ride/optional-stop route. It does not silently empty a filled jug or clean the cab: those have explicit actions. RESUME RIDE ROUTE can cancel service guidance without claiming bathroom use.

Author more points with a unique `stableId`, display name, valid `TruckTaxiRideLocation`, grounded truck stop, and appropriate radius/tolerance. Loaded points are registered at needs initialization; dynamic streaming registration is not implemented here. No pedestrian restroom interior or walking system is required.

## Controls and Jug Rules

Open DRIVER NEEDS from the existing pause menu using keyboard/gamepad navigation, or click the small NEEDS HUD button. The modal uses the HUD's existing focus scope and pause owner. USE PISS JUG closes the modal and resumes gameplay before starting the interaction.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Start/hold jug | Hold Insert, or start from Driver Needs | Start from Driver Needs, then hold LB |
| Hit timed cue | Tap Delete while holding Insert | Tap RB while holding LB |
| Bathroom/disposal/cleanup/routing | Existing UI navigation + submit | Existing UI navigation + submit |

`DriverNeedsJugHold` and `DriverNeedsJugCue` live in the existing HUD Input System map. Gamepad shoulder taps do not start an idle jug. Needs caches the player's input sources once and acquires its own gamepad-indicator suppression lease on JugStarted. Success, spill, crisis, cancellation (including inactive shift), and destruction release only that lease. Existing configured suppression and other owners remain intact. The input source also discards leases belonging to destroyed Unity objects, covering objects destroyed before their runtime lifecycle began. It always consumes LB/RB edges before returning None while suppressed, so releasing the lease does not synthesize a new press. Keyboard indicators, steering, pedals, and other commands are untouched. Public hooks are `AcquireGamepadIndicatorSuppression(owner)`, `ReleaseGamepadIndicatorSuppression(owner)`, and the separate baseline `SetGamepadIndicatorsSuppressed(bool)`; default suppression is false. Physical-controller driving remains an integrated validation requirement.

The QTE lasts 8 seconds with three cues at 25%, 50%, and 75% progress. Hold continuously and tap on each cue. At default duration, stopped cue half-width is about 0.46 seconds; moving half-width is about 0.22 seconds (settings are fractions of a quarter-cycle). A mistimed/missed cue, release longer than 0.8 seconds, or sustained high lateral acceleration causes a spill. The default acceleration threshold is 4.5 m/s squared, with an accumulated instability threshold of 0.6 seconds. Steering/throttle/braking remain with existing controls.

Success empties pressure, records `SuccessfulJugs`, sets FILLED JUG, and emits `JugSucceeded` with TRUCKER'S BOTTLE feedback. It does not claim a platform achievement unlock. A filled jug blocks another attempt until explicit disposal at a valid stationary service point. Spill/crisis outcomes are implied/comedic only. Bathroom actions are rejected while a jug is active.

## Reactions and Scenic Authoring

Appended `TruckTaxiDialogueCategory` entries preserve earlier enum values: RainReaction, StormReaction, FogReaction, NightReaction, SunsetReaction, WeatherChanged, BladderUrgent, JugStarted, JugSucceeded, JugSpilled, BathroomStop.

Author passenger-specific lines/audio in the existing dialogue assets using those categories. Existing authored selection takes precedence over `fallbackReactions` in environment settings. Fallback text distinguishes positive `chaosAffinity` from calmer passengers. No reference voice assignment, Chatterbox generation, or new dialogue/audio playback authority is part of this pass. `WeatherChanged` is an event/category hook without a generic fallback line unless authored.

Weather transition completion emits the weather categories; entering sunset/night emits their period categories. Needs events go through existing `Session.React` and passenger `Dialogue.Speak` only when applicable. Dialogue prioritization may select one reaction when events coincide; it is not a requirement to play every line simultaneously.

`TruckTaxiScenicEnvironmentBonus` defaults to a preferred Sunset period, optional weather filter, 150-cent bonus, and an authored reaction. It checks an already-successful existing scenic request, pays once per ride/stop, and never blocks the objective when conditions do not match. It does not create another stop system.

## Development Controls

In Editor/development builds, Driver Needs exposes ENVIRONMENT / DEBUG: dawn/morning/noon/afternoon/sunset/night presets, +1 hour, freeze/run, automatic weather, time/time-scale sliders, force weather/next weather, pressure 0/50/90/crisis, start jug, force success/spill, clear cab mess. Force-result buttons are diagnostic shortcuts, not evidence that normal controls work. The time-scale UI ranges 0-180; the coordinator API accepts finite values clamped 0-600.

## Session and Save Boundary

Pressure, sampled cycle, filled jug, cab mess, and counters live in the current Taxi runtime and survive individual ride changes. Ending a shift cancels temporary QTE/scenic award tracking; scene destruction creates a fresh needs model next time. No current feature claims cross-launch persistence of driver state, runtime debug time/weather choices, or QTE progress.

No autosave request, new slot, serializer, file I/O, PlayerPrefs driver save, or Pixel Crushers participant is introduced. Existing full-game save/autosave authority is unchanged. Audio preferences belong to the separate existing audio-settings integration. ScriptableObject defaults are authored content, not player progress.

## Validation and Known Limits

Final established validation baseline, before the subsequent fog transition correction:

| Validation | Executed result |
| --- | --- |
| Truck Taxi EditMode suite | 188 passed, 2 skipped, 0 failed |
| Shared weather regression tests | 8 passed, 0 failed |
| Truck Taxi PlayMode suite | 7 passed, 0 failed |
| Isolated environment reload/re-enable regression | 1 passed, 0 failed |
| Windows systems validation | 226 checks, 0 failures |
| Windows build | Succeeded, 0 errors |

These are suite-wide results, not 226 independent visual weather checks. The skipped tests are not passes. The isolated reload result is listed separately rather than added to the seven-test PlayMode suite.

- `TruckTaxiEnvironmentNeedsTests` covers clock/period semantics, weights, pressure/crisis, jug outcomes/windows, bathroom checks, scenic conditions, and fresh-session behavior.
- `TruckTaxiJugInputSuppressionTests` covers prior/concurrent leases, actual Input System shoulder-edge continuity, unaffected driving commands/pedals, and needs cleanup on success/spill/cancel/crisis/destruction. The earlier destroyed-owner cleanup failure is resolved in the passing EditMode baseline.
- `TruckTaxiWeatherAdapterCacheTests` covers known-root cache reuse, replacement/destruction, disable invalidation, and bounded diagnostic refresh. The adapter no longer scans all project components every frame. Final player profiling reported baseline population 12/12 at 251.53 FPS / 3.98 ms and dense population 144/60 at 117.51 FPS / 8.51 ms; all 60 dense-test cars moved more than two metres over 20 seconds. These are measurements of the integrated build, not an isolated attribution of every gain to this adapter.
- `TruckTaxiEnvironmentPlayModeTests.SceneReloadAndAdapterReenableReuseOneWeatherRuntime` checks one scene-scoped Weather Maker runtime across reload/re-enable and rejects Unity's default procedural sky after readiness. Its finally block restores pause/timeScale so subsequent scaled waits cannot remain frozen. The isolated regression passed; the earlier recorded run is `SavedSystemsWeatherReloadPlayMode.json` (1/1 in 7.57 seconds).
- `TruckTaxiEnvironmentRuntimeProbe.Run(host, check, screenshot)` is compiled only for Editor/development builds and invoked by the smoke-test integration. It checks time/freeze/pause, weather request completion, mixer assignment where configured, real authored bathroom eligibility, synthetic keyboard/gamepad jug inputs, spill/crisis/cleanup, and existing passenger dialogue output.
- The probe temporarily uses a kinematic/teleport-assisted tractor and a passenger fixture. It is not proof of moving-vehicle QTE difficulty, NWH driving, physical hardware ergonomics, visual readability, rain/fog pixels, or audible volume/crossfades. Captured screenshots require actual review.
- Initial PlayMode failures from duplicate Weather Maker creation and the subsequent fixture pause-cleanup issue were corrected before the passing baseline above.
- Final Windows sunset/night captures visually confirm the sky repair; storm rain is visibly present. Light-rain particle visibility remains UNVERIFIED. The subsequent fog correction passes its density through the vendor's public `ShowFogAnimated` transition, preventing a competing profile write from being overwritten. The rebuilt player retained the expected density 0.01591 and its capture visibly shows distant haze. Follow-up evidence: 9/9 shared-weather EditMode tests, 227/227 Windows systems checks with exit 0, and a zero-error build (`FogCorrectionEditMode.json`, `WindowsSystemsFogCorrection.log`, `FogCorrectionEditor.log`). The seven-test PlayMode suite above predates this narrow adapter correction; the affected flow was re-exercised in the Windows player.
- Weather audio remains PENDING / UNVERIFIED. No listening or hardware-controller pass is inferred from automated results.
- No complete windshield simulation, city streetlight rig, snow world, dynamic restroom streaming, persistent needs save, achievement service, or autonomous bathroom stop is implemented.

## Vendor Report

VENDOR ASSETS AUDITED: Weather Maker 8.0.9, installed URP integration, Heat UI, NWH/LWS wiper semantics, existing Compass/LWS navigation and passenger dialogue seams, alternate UTS/NWH skies.

VENDOR ASSETS USED: Weather Maker through the existing LWS adapter; Heat controls/TMP through the existing HUD; existing Compass map/route presentation and existing passenger dialogue playback.

RELEVANT ASSETS NOT USED: alternate static UTS/NWH skies, snow profiles, new Weatherade traction tuning, physical wipers where no usable authored implementation was found.

CUSTOM SYSTEMS CREATED: Taxi scheduling/policy, session needs semantics/QTE, bathroom/scenic metadata, HUD subpanel, Editor setup, focused tests/probe. These do not replace vendor clock/weather rendering, input, navigation, audio mixing, or persistence.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
