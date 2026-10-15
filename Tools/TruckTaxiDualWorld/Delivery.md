# Truck Taxi dual-world Windows delivery

## Actual build
- Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckTaxiDualWorld/TruckTaxi.exe`.
- Replacement build SUCCESS: 2026-10-15 04:29:05 UTC (12:29 AM Eastern). Unity6000.4.10f1, Windows x64, development Mono/D3D11. Binary source `f5749b7d`; actual shipped hashes in `Evidence/Arrival-guard-Windows-build.json`. Clean C# compilation; BuildReport succeeded but counted one Pipeline5s command-timeout log while the build continued. Do not describe it as zero logged build errors. The older manifest/evidence remains historical.
- Included main scenes: `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_MainMenu.unity`, `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity`, `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_Edenborough.unity`, plus existing regional chunks and venues.
- Main Menu -> NEW GAME -> VIBIN' -> DEMO CITY or EDENBOROUGH. Each has a description. CONTINUE uses the save's recorded world; it does not substitute the default new-game map.
- Pause -> TRAVEL TO ANOTHER MAP -> TRAVEL / CANCEL. Current and opposite region are shown. Existing Heat focus/submit and keyboard/gamepad cancellation are reused; no permanent travel HUD clutter.

## Physical connections and safe entries
Only two short EasyRoads spurs and local map supplements were added. Neither world was regenerated. Existing Edenborough nine districts/six terrain tiles, scenery, roads, services and garage remain intact.

| Saved scene | Outbound trigger center | Inbound authored position | Inbound yaw |
|---|---|---|---|
| Demo City | (1392.739,3.140,-230.291) | (1370.389,1.440,-182.111) | 331.3895 degrees |
| Edenborough | (-5239.001,68.408,-2544.899) | (-5187.246,66.708,-2532.975) | 73.30076 degrees |

These are read from the actual saved scene transforms, not guessed from the generator. Runtime grounds the tractor before applying it, so final Y accounts for the actual collider/vehicle dimensions.
- Demo connects existing east-highway `taxi.regional.1200` to marker `taxi.world-exit.edenborough`.
- Eden connects existing `bypass.west` to marker `taxi.world-exit.demo-city`.
- Each connector is 7.4m total, with two3.45m lanes, center dashes and a destination/regional-connection sign. The maps remain separate coordinate spaces; there is no giant cross-scene bridge.
- Canonical moving tractor only, validated entry direction, contact debounce and active-transaction suppression. NPCs cannot initiate transfer. Inbound placement faces back into the region, outside the outbound portal.
- Saved poses within30 horizontal meters of an outbound trigger use the safe inbound point. This fixes immediate re-entry when a menu return reuses a physical departure pose.
- Follow-up Windows testing found that an inbound tractor could still roll backwards down the slope into the return boundary. The existing boundary now requires outbound tractor HEADING as well as outbound velocity; seven new unit cases cover rollback/sideways/missing heading and legitimate pitched forward travel. The stationary development probe uses the existing parking brake and rejects moving/wrong-world samples; no physics or input controller replaced.
- Selectable local regional-exit POIs use the existing LWS road graph and Compass route. Each world's supplemental atlas contains only its own spur. Normal dispatch stays local.

## Travel, career and persistence
- Existing Pixel Crushers SaveSystem, JsonDataSerializer, DiskSavedGameDataStorer, ILwsSaveService and semantic Saver remain the ONE persistence path. No new backend, serializer, save database or slot namespace.
- The existing LwsSaveLoadCoordinator orders world/origin/ground preparation, semantic restoration, stationary vehicle placement, physics and presentation. Regional travel supplies only transient existing snapshots, not another persistence framework.
- A vendor source checkpoint precedes unloading. A successful arrival is vendor-autosaved. Genuine AsyncOperation progress is shown in a short-lived loading presentation; it retires after completion.
- Failed preparation restores the original scene/career and releases controls. If rollback itself cannot initialize, the source vendor checkpoint remains available from Main Menu. Actual rejected-arrival rollback was exercised.
- The same career/profile/name, truck ownership/model, paint, upgrades, inventory/needs, fuel, damage, fare statistics, progression and calendar are preserved. No starter-grant reset. Both scenes use the same existing canonical Taxi tractor and reviewed paint material; existing eight-seat cab behavior is preserved.
- Optional regional visits are bounded to TWO entries in the existing Vibin semantic payload: last global pose, absolute calendar time/rate, atmosphere, road/snow accumulation, forecast, supported extreme events and work area. No recursive career snapshot per visit.
- Live/catchup accumulation and temperature/rain melt share one formula. Snowy Edenborough and rainy Demo City remain distinct; rain can legitimately melt Demo snow to zero during elapsed game time.
- A save captured mid-weather-transition can contain the FROM preset. Taxi now resumes the persisted forecast target when that snapshot differs, without restarting matching weather. Manual rain intent also takes precedence over a stale FROM preset during catchup.
- Existing unsafe fare/race/companion/private interaction/recovery/towing/nozzle/modal gates remain. Safe on-foot travel returns the driver to the tractor.
- ATTACHED TRAILERS CURRENTLY BLOCK TRANSFER with a detach explanation. No silent detach, cargo destruction or unsupported trailer teleport. Physical trailer travel is not claimed.
- Legacy saves without regional visits retain their established Demo mapping. Actual pre-feature old-build save loading was tested against the final binary using disposable data.

## Full outdoor snow and real wheel tracks
- Weather Maker owns atmosphere; Weatherade owns coverage/depth/trace masks. NWH WheelUAPI contact points/width/path drive the existing mask volumes, not proximity, random decals or a second tire-track manager.
- Supported presentation: Unity terrain, outdoor grass/dirt/shoulder/parking/sidewalk meshes, URP Lit exterior roofs/props, NatureManufacture foliage, TTFE tree bark/foliage/billboards, and existing Weatherade roads.
- Project-owned URP adaptations preserve authored lighting/detail, cutout/transparency and foliage wind/auxiliary passes. Reviewed glass, interiors, player and HUD surfaces are excluded. Upward-facing and bounded depth exposure govern accumulation; unknown shader families are not silently replaced.
- Shared per-source materials,64-renderer binding batches and reused local depth/trace volumes bound work/memory. Deep snow suppresses terrain detail density, without changing TerrainData geometry.
- Off-road sampling support expands only to three actual surface-depth texels; the tire mask retains actual tire width. This fixes subpixel ground-depth proxies disappearing on short contacts.
- Revalidation repairs vendor camera renderer/RTHandle changes. Weatherade UpdateDepth stops its native follow coroutine; the adapter now uses the public thresholded UpdateSRSPosition/UV-scroll API rather than reinitializing or clearing history.
- Streamed-region discovery no longer wipes nearby tracks. Tracks curve with real wheels, retain indentation after stopping, and decay/scroll within the bounded mask. No dry/shallow stamping.
- Flat ground now also uses Weatherade's existing RG groove normals and A compression, so it is not just a faint flat tint. Terrain retains the installed displaced-snow path. Both final Windows groove screenshots were visually reviewed.
- Accumulation is saved; individual local GPU tire marks are transient across world unload/process restart. No unbounded serialized track history.

## Actual validation
| Check | Actual outcome |
|---|---|
| Unity compilation / Windows build | PASS, actual final executable built and run |
| EditMode | Current targeted world-travel104/104 PASS, including7 new direction cases. Earlier243 distinct PASS: world173, calendar12, snow22, occupancy/navigation24, MainMenu2, garage10; historical, not all rerun after the narrow guard/probe fix |
| Graphics PlayMode | Demo and elevated Eden GPU cases PASS; real offroad replay, vendor reset, stale elevated anchor, actual GPU readback, availability callback and8s retention. Not a physical-driving claim |
| Windows A/B | PASS automated core: actual native NWH drive/contact activates physical exits both ways; grounded non-retrigger arrivals, local routes and driving after arrival |
| Windows C | PASS automated core: actual Heat submit, virtual gamepad confirm/cancel and Escape cancel; both directions |
| Windows D | Current restoration/control core passed in Eden then Demo with shared career/climate; final Demo exact exit0. Seed/Eden shutdown errors recorded separately. Manual1 rollback and legacy compatibility are earlier-binary evidence with unchanged persistence code, not rerun after the exit-heading fix |
| Windows E | PASS core/representative visuals: clear/light/heavy states, live gradual accumulation/rain melt, real long curved offroad wheel tracks in BOTH maps, distinct regional climate through restart; representative terrain/branch/roof images reviewed |
| Windows F | PASS automated core on FINAL binary: ten transfers, then an eleventh to the snowy Eden checkpoint; every arrival checks career, native input/physics, local GPS, one player/host/HUD and retired loading transaction |
| Current process exits | Seed gameplay assertions completed then native0xc0000005; Eden Continue completed then native shutdown error. Final clean-client Demo Continue PASS/exit0. Earlier zero-exit runs below are historical, not substituted for these failures. No own player/helper remains |

Current integrated disposable root: `ab987faebd6842a0a2e78bd5ffc52e54`. Eleven transfers, all six valid parked snow measurements, both curved offroad paths, vendor checkpoints and fresh-process Eden/Demo career restoration were exercised. Current progress, metrics, exact Demo exit and screenshots are in `Evidence/Windows-recheck-*`; provenance/failures in `Windows-Shared-GPU-Recheck.md`. Older integrated root `b59ee4f0674944458235d7389e170b11` and its `Windows-final-*`/legacy files remain historical. Failed/interrupted runs are not relabelled as passes. Real saves were never used.

Current native offroad paths: Demo16.041m, four contacts/544stamps, GPU1818marked/1667indented; Eden15.759m, four contacts/568stamps, GPU1373/1315. Screenshots: `Windows-recheck-DemoCity-curved-grooves.png`, `Windows-recheck-Edenborough-curved-grooves.png`, both visually reviewed actual game frames, not generated images. Earlier `Windows-final-*` groove/roof/branch files remain historical same-renderer observations, not exhaustive scenery acceptance.

## Performance and honest remaining limits
The original two games were closed, but another task subsequently launched new Sports players/headless builds. The human explicitly approved shared-GPU testing. Foreground windows were coordinated separately to preserve strict native-input tests. Equivalent before/after and dense urban/vegetation driving benchmarks remain NOT RUN; no isolated-performance claim.

Final stationary regional-exit observations,1920x1080, live NPCs, clock frozen only for controlled snow. Milliseconds median/P95/P99:
| Map | Clear | Light | Heavy |
|---|---|---|---|
| Demo |18.712/25.015/37.507|18.763/31.270/50.017|24.998/37.498/43.789|
| Eden |18.758/25.036/31.269|25.002/37.523/50.008|25.010/43.758/50.007|

These are SHARED-GPU observations, not a locked60FPS result or dense-city driving benchmark. Quality/VSync/frame-cap values were not captured by this existing stationary probe. Current ten-transfer object counts: Demo10118->8544; Eden46464->46459. Allocated bytes Demo371143456->370340120; Eden512804505->533463430. Warm-cache finite samples are not zero-leak proof; the eleventh snow-loaded Eden visit is separate.
- A prior recheck's alleged Eden measurements are INVALID: an uncommanded return loaded Demo during sampling. That failure caused the guard fix, not a weather rewrite; wrong-world image retained.
- Native shutdown failures resolved to installed UnityPlayer symbols at ExternalGPUProfiler::GetGameViewWindowHandle+0x9, with UIAutomationCore on the exception stack. Resetting the automation client/no text-accessibility capture produced a fresh Demo Continue PASS and exact exit0. This supports an automation/native-accessibility interaction, not a claim all engine shutdown paths are fixed. Failed shutdowns remain recorded; no engine/vendor patch or forced process termination.
- Human full-route driving, every service/dispatch interaction, exhaustive imported-material review, night-track readability, physical gamepad hardware, mouse menu clicking and trailer-guard physics still need manual acceptance.
- The driving tests use controlled existing connector approaches followed by actual Input System->NWH driving, not a claim of manually driving the entire map. Career fixtures prove preservation, not manually earning money/shopping.
- NPC ownership/population counts and local-route reinitialization were checked, not human inspection of every traffic junction/pedestrian location.
- New-map selection is implemented with real buttons/descriptions; exhaustive human main-menu presentation has not been claimed. Regional sign TMP sizing was measured read-only; no unrelated visual redesign.

## Ownership and commits
VENDOR ASSETS AUDITED/USED: NWH, Pixel Crushers, Compass/LWS graph, EasyRoads, Weather Maker/Weatherade, UTS/Citizens, Heat, existing Spline Spawner/imported scenery.
RELEVANT ASSETS NOT USED: UTS is not used for player trailer physics; no new Scene Streamer authority, save backend or replacement navigation/weather system.
CUSTOM SYSTEMS CREATED: bounded world-visit semantics, transient regional ordering transaction, canonical directional exit, existing-HUD travel panel, narrow connection authoring, development-only probes and project-owned material/contact adaptations. None owns physical save storage or an independent serializer.
VENDOR SOURCE MODIFIED BY THIS TASK: NO. Pre-existing unrelated vendor changes remain untouched.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO. No new packages/purchases.
Local follow-up source commit: `f5749b7d` (three existing project-owned files, no new runtime system). Earlier implementation commits remain recorded above/in Git; prior handoff `86551781`. Current evidence/docs commit is recorded in Git. PUSHED BY THIS TASK: NO.
