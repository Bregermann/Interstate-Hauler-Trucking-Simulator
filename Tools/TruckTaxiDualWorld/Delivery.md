# Truck Taxi dual-world Windows delivery

## Actual build
- Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckTaxiDualWorld/TruckTaxi.exe`.
- Build SUCCESS: 2026-10-15 01:45:06 UTC (October 14, 9:45 PM Eastern). Unity6000.4.10f1, Windows x64, development Mono/D3D11. Binary source commit `9bbac8e9`; generated manifest in `Evidence/Windows-build-manifest.json`.
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
| EditMode | 243 distinct PASS: world173, calendar12, snow22, occupancy/navigation24, MainMenu2, garage10. Subsets not double-counted |
| Graphics PlayMode | Demo and elevated Eden GPU cases PASS; real offroad replay, vendor reset, stale elevated anchor, actual GPU readback, availability callback and8s retention. Not a physical-driving claim |
| Windows A/B | PASS automated core: actual native NWH drive/contact activates physical exits both ways; grounded non-retrigger arrivals, local routes and driving after arrival |
| Windows C | PASS automated core: actual Heat submit, virtual gamepad confirm/cancel and Escape cancel; both directions |
| Windows D | PASS automated core: same career values, paint/real purchased/equipped upgrade/nonzero damage/fuel/inventory, save/quit/relaunch/Continue in snowy Eden then rainy Demo, immediate native drive. Manual1 load/drive/rollback also PASS on prior candidate with identical persistence code; final legacy compatibility PASS |
| Windows E | PASS core/representative visuals: clear/light/heavy states, live gradual accumulation/rain melt, real long curved offroad wheel tracks in BOTH maps, distinct regional climate through restart; representative terrain/branch/roof images reviewed |
| Windows F | PASS automated core on FINAL binary: ten transfers, then an eleventh to the snowy Eden checkpoint; every arrival checks career, native input/physics, local GPS, one player/host/HUD and retired loading transaction |
| Final process exits | Fresh seed0, Eden Continue0, Demo Continue0, actual old-save Continue0; no required own player left running |

Final integrated disposable root: `b59ee4f0674944458235d7389e170b11`. Raw files: `Evidence/Windows-final-seed.log`, `Windows-final-Continue-Eden.log`, `Windows-final-Continue-Demo.log`, `Windows-legacy-final-build.log`. Earlier failures are retained and explicitly named; they are not counted as passing tests. Actual real saves were never redirected, overwritten or deleted.

Final native offroad paths: Demo15.96m, four contacts/742 stamps, GPU1809 marked/1684 indented pixels. Eden four contacts/649 stamps, GPU1362/1293. Screenshots: `Windows-final-DemoCity-curved-grooves.png`, `Windows-final-Edenborough-curved-grooves.png`. These are actual rendered game frames, not generated images. Representative roof/branch images come from the preceding same-renderer candidate, not a claim of exhaustive scenery acceptance.

## Performance and honest remaining limits
The two user TruckSports games (PID28400/33688) remained running on the GTX1660Ti; neither was interrupted. Therefore isolated before/after regression analysis and dense urban/vegetation driving benchmarks remain BLOCKED/NOT RUN, not passed.

Final stationary regional-exit observations,1920x1080, live NPCs, clock frozen only for controlled snow. Milliseconds median/P95/P99:
| Map | Clear | Light | Heavy |
|---|---|---|---|
| Demo |12.503/18.766/25.002|18.745/18.841/25.030|18.827/25.107/31.226|
| Eden |12.507/18.807/18.883|18.758/25.055/25.145|24.993/25.128/31.375|

These are NON-ISOLATED observations, not a locked60FPS result or a dense-city driving benchmark. Final ten-transfer object counts: Demo8544 stable; Eden46464->46459. Allocated bytes Demo369435901->370593498; Eden530928602->533247923. Warm-cache finite samples are not proof of zero leaks; the eleventh snow-loaded Eden visit is recorded separately.
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
Local implementation commits: `8b23f96f`, `7cb9baf9`, `ed6b39e4`, `6c19ae1a`, `89a431ec`, `9aa4de5d`, `c908cf7b`, `cad0f2eb`, `cd0d9faa`, `efc622fa`, `fd321e0b`, `60ab13bc`, `404d95e9`, `e3e85a0f`, `660f72bb`, `306cfecb`, `9bbac8e9`. Final handoff/evidence commit is recorded in git. PUSHED: NO.
