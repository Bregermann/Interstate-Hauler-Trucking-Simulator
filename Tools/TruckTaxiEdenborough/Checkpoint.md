# Edenborough Windows delivery checkpoint

Updated 2026-10-13. This is the current checklist, replacing the historical preparation statuses. Scope: finish the dedicated Edenborough world and Windows player. No push; real player saves and unrelated working-tree edits preserved.

## Actual artifacts

- Scene: Assets/LWS/TruckTaxi/Scenes/TruckTaxi_Edenborough.unity (saved, approximately 43 MB).
- Windows: Builds/TruckTaxiEdenborough/TruckTaxi.exe, with its adjacent TruckTaxi_Data and runtime folders (approximately 2.25 GB).
- Edenborough is the Truck Taxi primary new-game world. Original demo and other game modes remain in the workflow; existing old-world Continue is preserved.
- Nine fictional districts: Central, Gogar, Zoo, Leith, Musselburgh, Dalkeith, Bonnyrigg, Loanhead, Hillend. Approximately 12 x 10 km inspired regional layout, NOT a full geographic/OSM reconstruction.

## One acceptance checklist

| Requirement | Status | Evidence / remaining coverage |
| --- | --- | --- |
| Saved playable scene and new-game selection | Verified complete | Actual saved scene, Windows spawn and native NWH driving; original demo retained |
| Six terrain tiles and real elevation | Verified complete | Native terrain; sampled 6.0026..203.3291 m; Windows scene renders |
| Connected EasyRoads and road grading | Implemented, partly verified | 155 definitions, 859/859 baked-road surface probes, service/garage clearance checks PASS; full-world driving coverage remains |
| Narrow truck-scale lanes | Implemented, driving validation incomplete | 3.05/3.25/3.45/3.10 m lanes vs 2.196244 m tractor collider; no arbitrary widening |
| Turns, opposing traffic, ramps, lots, trailer swept paths | Implemented but unverified | Automated geometry is not manual maneuver proof; full driving pass not run |
| Owned packages actually imported and placed | Verified complete for 17 packages | 18 exact selection manifests, Unity-resolved roots and nonzero actual scene mesh usage; Winter withheld with broken material GUIDs |
| Buildings, nature, signs, clutter, furniture | Verified complete for existing placements | 21,794 renderers / 6,571 colliders; actual source meshes in saved world; screenshots. Sparse areas and mixed art styles are not final urban polish |
| Logical frontage/density and complete sightline/LOD review | Implemented but unverified | Existing deterministic dressing/exclusions; full visual placement review still required |
| UTS right-hand traffic across map | Implemented, partly verified | 24 initialized circuits; seven moving native UTS actors and three full-physics actors observed; not every junction/circuit driven |
| Citizens pedestrians in nine areas | Implemented, partly verified | Native walking graph with 7,367 nodes, 80 live actors; 51 enabled native Citizens skinned models, 12 moving pedestrians observed in final player. All nine areas have not been individually visited |
| Reachable destinations/services | Implemented, partly verified | 27 destinations; actual gas route computes; pump and shop reach/sight gates and transactions PASS. Fixture placement is NOT driving/walking arrival proof |
| Fuel interaction | Verified complete in automated Windows fixture | Normal truck exit, imported pump/nozzle, actual filler reach, fuel increase and wallet charge, cleanup |
| Shop/diner/restroom interaction | Shop verified; others unverified | Actual shelf/basket/register purchase changes inventory and wallet. Diner/restroom/motel playthrough not run |
| GPS/map | Implemented, partly verified | 14 scene map tiles; actual truck-legal route to Gogar gas station calculated; existing Compass shown in native screenshots |
| Near-spawn garage and HSV UI | Verified in automated Windows fixture | Owned garage, bay eligibility, real modal, HSV preview, Cancel, Default, Apply, lock release; physical drive-in/out and mouse/gamepad human operation unverified |
| Garage paint save/quit/relaunch/load | Verified complete | Real Pixel Crushers isolated roots; selected blue paint restored after actual process exit and fresh Continue/manual load |
| Genuine truck switching | Limited | One valid owned NWH starter; no decorative vehicle advertised as driveable |
| Post-load controls and save integrity | Verified in final Windows build | W-first native movement (10-11 m), brake/steering/engine/automatic transmission, dynamic body, one retained native input provider, no stale locks; multiple slots/regions/menu-return Continue |
| Performance | Measured, incomplete comparison | GTX 1660 Ti, 1920x1080, vSync 1/target 60: stationary median 18.7565 ms (53.3 FPS), p95 25.0115 ms with native Citizens. No matched before/after or sustained driving benchmark |
| Windows build and screenshots | Verified complete | Final fresh build PASS, updated Runtime DLL, actual seed/Continue/manual player processes PASS and quit. Native screenshots preserved in Evidence |
| Import workbook and usage evidence | Verified complete | Plan updated through artifact tool; all five tabs preserved; zero formula errors; exact roots/counts in Imports and Evidence/import-usage.json |
| Local commits, no push, preserve unrelated changes | Verified complete | f5c01224 authoring/grading; 3ea9d344 owned art; b2d8a9c1 world; 268d85cd runtime/Windows fixes; separate Record Edenborough Windows acceptance and owned-art usage commit contains this final evidence/workbook. No push; unrelated dirty files left untouched |

## Findings repaired

- Native MaterialPropertyBlock construction moved from MonoBehaviour initializer to Awake/Configure; null-safe teardown. Garage suite 10/10 PASS.
- Standalone world lacked demo daylight setup: reused existing BuildLighting, saved only Edenborough sun/ambient setup.
- Garage preview framed accessory/route bounds and rendered world fog: uses actual body paint bounds, public Preview camera type, draw-scoped/restored layers. Actual blue-truck preview screenshot inspected.
- Existing Vibin save participant rejected Edenborough: added only the exact stable WorldId/SceneName pair; retained legacy demo behavior, rejected mismatches/traversal. Edenborough suite 38/38 PASS.
- Edenborough copied the demo Wobble visual override: generator and saved scene now use existing native Citizens models. Demo unchanged. Final Windows native-model assertion PASS; inspected actual player screenshot shows the original human models.

## Evidence and limitations

- Final build run: 45f22675fbaa41fcb2584c0878838f7f. seed-citizens-player.log, continue-citizens-player.log and manual-citizens-player.log all PASS. Disposable Pixel Crushers root under Application.temporaryCachePath/TruckTaxiRestartTests, never real profiles. All test players exited.
- Earlier actual failures are retained in Temp logs; not counted as successful tests. Compact final assertions, screenshots, SHA256 build metadata and validation coverage are in Evidence.
- Package inspector 8/8; Edenborough EditMode 38/38; Garage EditMode 10/10. No physics/manual-driving claim from EditMode.
- Native desktop capture repeatedly times out despite unlocked desktop. Unity player ScreenCapture works and actual images were inspected. No fabricated human input/acceptance claim.
- Native Pipeline returns a 5-second timeout during heavy builds while BuildPipeline continues. Confirm fresh result file AND Runtime DLL timestamp; never repeat blindly or reuse an old PASS.
- Unity URP adds light metadata after scene save. Build script saves only the generated Edenborough scene before the dirty-scene guard, not unrelated user scenes.

## Handoff / remaining acceptance

Executable and saved populated scene exist and were exercised; this is not a source-only checkpoint. Full manual traversal, all junction/swept-path checks, physical garage drive-in/out, diner/restroom/motel operation and matched driving performance remain unverified. Sparse areas, mixed art styles and Canada-source numeric speed-sign artwork are not final American city polish. Winter's broken source material references remain withheld, not silently counted as imported. One genuine NWH truck is available. Do not label the full request completely accepted until these remaining items are exercised/reviewed.

Preserved the nested Toon Gas Station package archives: one is the recorded import-manifest source, and uncertain cleanup was rejected by auto-review. Nothing was deleted. Source archives and unrelated work remain intact.

Vendor assets audited/used: NWH, EasyRoads, UTS/Citizens, Compass, Weather Maker/Weatherade, Pixel Crushers, Heat, Spline Spawner, installed owned art. Existing authorities preserved; no vendor source changed by this task, no duplicate gameplay/persistence framework created. Custom additions are scoped LWS authoring/assembly, garage semantics and opt-in disposable-player validation, not replacements. Vista/Scene Streamer were not introduced for this single-scene prepared terrain workflow. No purchases or dependency changes.
