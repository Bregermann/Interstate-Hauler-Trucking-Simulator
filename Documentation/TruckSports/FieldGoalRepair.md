# Truck Field Goal repair and combined player acceptance — October18,2026

Current canonical binary is the later [Sumo combined milestone](SumoRepair.md), which retains this implementation and original wordmark. Tests below remain versioned to their recorded binary; the later build reruns the relevant Field Goal/title regressions.


Windows x64 Development;35 explicit Sports-only scenes; TruckSports startup;0 errors /20 warnings. Built 2026-10-18T17:53:15.697166+00:00. Managed SHA256 `6d67807a233ee2a2163b44b902c096e86ca0958b3b1b92ac2032c0405c2c6446`;438-file data manifest `d97805acd6b7cbc758219bd00ce3a7d643cc601adc0af59a0e0fa9df7d464633`.

Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep its entire directory, including TruckSports_Data, UnityPlayer.dll and UnityCrashHandler64.exe. The original painted Party wordmark is now included in the executable, title and field. This accepts the Field Goal/brand milestone, not the entire queued release.

## Corrections and integration

| Feature | Inspected cause / resulting behavior |
|---|---|
| Bumper-only kicking | Saved option1 selected the obsolete detached-foot/truck-projectile variant. Competition/practice now both use the truck bumper to contact an upright real football. Versioned migration maps old competition0/1 to new0 and old practice2/3 to new1; names, stable identities and bumper statistics survive. The old boot/mechanism is inactive. |
| Upright ball and holder | Saved capsule direction2 and presentation were horizontal at0.75m. Capsule direction1, upright prolate/laced visual and1.23m tee placement now agree. A noncolliding holder reuses the existing truck model with a padded holding arm. |
| Contact and flight | Incoming speed/direction and contact offset/height determine bounded line, rise, power and spin. One velocity-difference response consumes the kick; no second impulse, repeated kicking, homing or football aftertouch. PhysX owns gravity and post impacts. |
| AI execution | Near-ball recentering cancelled the planned entry angle; initial Hard validation became over-perfect and produced prolonged tied extras. Look-ahead follows the planned approach line and keeps difficulty-dependent heading/speed/aim error. All approaches use real NWH controls and contact. |
| Weather lifecycle | PrepareAttempt forced Clear on every shot. A match-owned forecast now persists through attempts, pause and extras. Explicit preference survives reload; Random advances a persisted sequence for new matches/rematches. |
| Weather catalog and selection | Opt-in shared asset has32 actual installed profiles plus a configured Hurricane profile based on installed storm/wind assets. Heat dropdown supports persisted Random/explicit choice and actual submit, scrolling and cancel. Weather Maker performs weighted selection and all weather visuals. User-authored catalog entries are not repopulated by builds. |
| Actual weather rendering | Previous scoped URP renderer lacked Weather Maker's feature. The owned pipeline installs the vendor feature and preserves the snow/depth renderer. Enabling the ordinary ZTestAlways sky mesh hid foreground geometry; it stays off while the vendor script renders through its command buffers. |
| Wind and snow | Actual vendor wind, with explicit unit conversion, drives continuous bounded relative-air drag/spin, the striped windsock and MPH/relative-camera arrow. Humans and AI receive the same preset grip. Existing Taxi/Weatherade snow coverage and NWH wheel-contact tracks are reused. |
| Venue and feedback | Ten-yard end zone, posts at its back, transparent original Party turf wordmark, compact forecast HUD and refreshed help. Open windsock cloth has correct outward mesh faces and separate two-sided red/ivory materials; the initial broken-ring view was rejected and corrected. |
| Saved integration | Additive saved-scene update preserves every original scene block/object; no venue regeneration. Defaults invoke the same installer for explicit fresh authoring. Normal build only reads saved scenes. Menu preview is a verified initialized real bumper-contact render; automatic preview authoring preserves it. |

## Changed source and assets

Saved scene: Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity. Changed FieldGoalMatch, FieldGoalFocusedProbe, FieldGoalProbe, FieldGoalAuthoring and FieldGoalPreviewAuthoring. Added FieldGoalFlightModel, FieldGoalPresentation, FieldGoalRepairProbe and FieldGoalRepairAuthoring. Shared SportsEventSession adds a no-op-by-default versioned option-migration hook; SportsChaosWeather adds an opt-in full-profile adapter. New SportsWeatherPresets/Picker and their editor authoring/rendering adapters remain opt-in to Field Goal in this milestone.

Owned assets: shared forecast catalog and Hurricane weather/wind, scoped Weather pipeline/renderer, ten persisted tapered windsock meshes, two-sided cloth materials, end-zone/wordmark materials and refreshed Menu Preview.png. Original Taxi/vendor prefabs and source are not modified. The new logo source was already committed/pushed in the preceding brand milestone.

## Actual validation

Native Editor: compilation, saved-scene validation and80 pure rule assertions;33 save/schema migration checks;100 all-profile integration checks;15 deterministic contact/isolated real-PhysX wind comparisons;18 setup/preference/submit/cancel/reload checks;50 focused actual driven-contact, scoring, replay, injected-device reconnect, practice and cleanup checks;14 actual HeavyRain/Blizzard/Hurricane gameplay checks; full four-AI matches on Easy/Normal/Hard. These are explicit custom Editor/Play Mode probes and fixtures, not a claimed NUnit Test Runner suite.

All15 final-version Windows suites pass with fresh results, expected process exits, sealed assembly/data hashes and no detected exception/shader-error/crash markers:

1. Title brand/action lifecycle at720p/1080p.
2. Versioned saved-setup migration and repeated reload.
3. Contact model and isolated actual Rigidbody head/tail/crosswind comparisons.
4. Forecast Heat setup, submit/scroll/cancel, persistence, match/turn/pause and menu cleanup.
5. All33 actual preset/precipitation/vendor-wind integrations.
6. Complete two-AI HeavyRain, Blizzard and Hurricane games, real kicks and substantial snow/grounded-wheel contacts.
7. FieldGoalFocused: real software-keyboard and assigned-pad kicks, once-only response, actual goal/post/ground chronology fixtures, pause/replacement neutral gate, actual recorded replay, practice/reset and five aspect-ratio windows.
8. Three complete four-AI difficulty games using real NWH approaches.
9. TargetRepair physical supported-rest/wing/lava regressions.
10. TargetFocused gameplay/replay/input regressions.
11. TerrierFinish centered/angled/scrape/slam/high-speed/order/lock regression.
12. KeepawayPresentation five-aspect/typed-score/cab-guidance regression.
13. Shared Show AI Cameras ownership matrix across20 applicable modes.
14. Shared participant/identity matrix across34 sport scenes.
15. Sustained60-real-second snowy Field Goal sample at1920x1080, timeScale1 and the mode's single broadcast camera with four sequential AI shooters.

Actual final player AI observations (one game per difficulty, not statistical balance evidence):

```text
MATCH variant=0 difficulty=0 goals=3,0,2,1 attempts=5,5,5,5 rounds=5 launches=17 winner=0
MATCH variant=0 difficulty=1 goals=0,4,1,3 attempts=5,5,5,5 rounds=5 launches=20 winner=1
MATCH variant=0 difficulty=2 goals=4,3,2,5 attempts=5,5,5,5 rounds=5 launches=20 winner=3
```

Actual final player weather games:

```text
ACTUAL_WEATHER_KICK Heavy Rain wind=(-5.34, 0.00, -1.07) grip=1.107 launch=(-1.20, 16.24, 22.44)
WEATHER_MATCH Heavy Rain goals=5,0,0,0 attempts=5,5,0,0 maximumRound=5 snowContact=False
ACTUAL_WEATHER_KICK Blizzard wind=(13.81, 0.00, -11.95) grip=0.9126 launch=(-2.18, 13.32, 17.47)
WEATHER_MATCH Blizzard goals=3,1,0,0 attempts=5,5,0,0 maximumRound=5 snowContact=True
ACTUAL_WEATHER_KICK Hurricane wind=(29.79, 0.00, 2.19) grip=0.999 launch=(0.00, 15.21, 20.80)
WEATHER_MATCH Hurricane goals=0,2,0,0 attempts=5,5,0,0 maximumRound=5 snowContact=False
```

Same initialized kick in isolated actual PhysX: still-air landing52.71m;15m/s headwind45.75m; tailwind57.46m; opposite crosswinds produce opposite2.94m landing shifts and0.96m shift at the30m scoring plane. These comparisons isolate aerodynamics; the driven matches separately prove actual integration.

Performance observation:

```text
PERFORMANCE realSeconds=60 editor=False resolution=1920x1080 frames=8896 meanMs=6.744651 p95Ms=12.4848 p99Ms=12.52407 focusedFrames=8896 liveFrames=8896 rematches=0 gpu=NVIDIA GeForce GTX 1660 Ti
```

This is a single-camera hot-seat sample, not four simultaneous viewports. Other user applications were left running; the environment is not an isolated benchmark. The raw sample records GPU, focus, actual resolution, live-frame ratio and timing. No new comparative cross-mode performance claim.

## Evidence, warnings and limits

[Acceptance manifest](FieldGoalEvidence/Acceptance.json) and [Windows suite receipts](FieldGoalEvidence/WindowsSuites.json) bind the evidence to this binary. FieldGoalEvidence includes actual title, kick and weather Windows images and explicitly labelled controlled Editor setup captures.720p/1080p,16:10,4:3 and ultrawide gameplay/HUD captures were inspected. Editor setup captures were inspected; Windows setup navigation/persistence was exercised automatically. Do not describe injected devices as physical controllers.

The build has20 warnings, retained in FieldGoalEvidence/build-warnings.txt: DirectInput excluded assemblies, disabled runtime Pipeline configuration, an existing large-triangle Transport Helicopter mesh warning and installed foliage/Weather Maker shader warnings. Zero build errors does not mean warning-free. Vendor source was preserved. First candidate/rejected render and AI diagnoses remain under ignored Logs/TruckSports/FieldGoalRepair/Rejected; they are not final passes.

Manual checks remain: human game feel/competitive balance, physical controllers/wheels/shared-pad ergonomics/keyboard rollover, perceived audio/haptics, weather visibility preference and longer multi-session hardware performance. The later Goddess crowd/reactions/rare field-runner requests and universal adoption of the weather selector across every sport remain queued. Do not claim the full Party/FareFight/Goddess/SkyTow/Limbo/TrailerPlug work is complete.

## Vendor accounting

VENDOR ASSETS AUDITED: NWH Vehicle Physics2 truck/wheels, Heat dropdown/UI Manager samples/public APIs, Weather Maker8.0.9 profiles/groups/wind/URP integration, Weatherade SRS and existing Taxi snow/track adapters, Cinemachine2.10.7/DOTween and stadium/truck assets.

VENDOR ASSETS USED: NWH through SportsEventTruck for every approach; Heat/uGUI and existing rounded presentation; WeatherMakerProfileGroupScript.PickWeightedProfile, profile transition/wind/precipitation/cloud/fog/lightning and WeatherMakerURPRenderFeatureScript; existing Weatherade/Taxi snow contacts, Cinemachine/replay, DOTween presentation and owned stadium/truck visuals.

RELEVANT ASSETS NOT USED: Inventory found no suitable installed windsock/football replacement. The owned tapered cloth and established prolate football supply those shapes. Traffic/general flight controllers do not supply contact/scoring semantics. The obsolete detached boot is disabled.

CUSTOM SYSTEMS CREATED: Field Goal contact/aerodynamic/AI semantics, stable forecast metadata and Heat binding, additive source/scene integration, read-only wind feedback and explicit acceptance probes. No replacement vehicle, weather-rendering, UI or snow framework.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
