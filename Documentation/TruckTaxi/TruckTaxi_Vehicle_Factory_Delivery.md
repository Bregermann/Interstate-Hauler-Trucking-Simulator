# Truck Taxi Six-Truck Vehicle Factory Delivery

## Build And Status

Windows x64 development player: `Builds/TruckTaxiVehicleFactory/TruckTaxi.exe`.
Unity: 6000.4.10f1. Final GPS-corrected build: 2026-10-15 15:17:21 UTC.
Build through the existing `TruckTaxiEdenboroughAuthoring.BuildVehicleFactoryWindows` workflow.
Both Demo City and Edenborough, existing travel and weather/snow systems remain included.

Implementation is integrated, not Blender-only. Runtime and asset validation are recorded below.
The full human-driving acceptance checklist is NOT claimed complete. Outstanding checks are explicit.
The current project contains unrelated pre-existing dirty work; those files were not reverted or included in the feature commits.

## Original Source And Reproduction

- Original starter: `Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/GR3D 010416TRUK/Mesh/010416TRUK_HD.fbx`.
- Source GUID: `9817eabadfb74f04abda4bd5329b9138`; existing importer scale 0.8.
- Blender executable: `C:/Program Files/Blender Foundation/Blender 3.5/blender.exe`, version 3.5.1.
- Untouched project-local review reference: `ArtSource/TruckTaxi/SourceReference/Starter_Untouched_Reference.blend`.
- Reproduction: `ArtSource/TruckTaxi/ModelTrucks.py`, `ValidateExports.py`, `TrailerFit.py`, and `SourceReference/PrefabLayout.json`.
- Local vendor reference stays inside this licensed project; no vendor mesh was included in the five exported bodies.

For each name `HighwayKing`, `Nightrunner`, `Outlaw`, `Thunderhaul`, `RoadPalace`:

| Artifact | Path |
|---|---|
| Editable Blender source | `ArtSource/TruckTaxi/Models/<name>.blend` |
| Authored model metadata | `ArtSource/TruckTaxi/Models/<name>.json` |
| Body and two distance LOD exports | `Assets/LWS/TruckTaxi/Art/Trucks/Models/<name>.fbx`, `<name>_LOD1.fbx`, `<name>_LOD2.fbx` |
| Real NWH player prefab | `Assets/LWS/TruckTaxi/Art/Trucks/Prefabs/TT_<name>_NWH.prefab` |
| Existing-format truck definition | `Assets/LWS/TruckTaxi/Art/Trucks/Definitions/TT_<name>.asset` |
| Dashboard configuration | `Assets/LWS/TruckTaxi/Art/Trucks/Definitions/TT_<name>_Dashboard.asset` |

Starter prefab remains `Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab`; only roster metadata was added, not replacement geometry.
All five new bodies were modeled, exported, imported, integrated and exercised in Unity and Windows. Art review is not a claim of AAA quality or human approval.

## Roster And Economy

| Model | Stable ID | Price | Tractor mass | City/highway steering lock | Launch at rest | Sleeper |
|---|---|---:|---:|---:|---:|---|
| Interstate Starter | ih.truck.nwh.euro.semi | Free / owned | Existing | Existing | Existing | Existing baseline |
| Highway King | taxi.truck.highwayking | $600 | 8400 kg | 36 / 7.5 deg | 3.5 | Furnished |
| Nightrunner | taxi.truck.nightrunner | $900 | 7600 kg | 40 / 8 deg | 3.6 | Furnished |
| Outlaw | taxi.truck.outlaw | $1,250 | 7400 kg | 43 / 8.5 deg | 4.1 | No |
| Thunderhaul | taxi.truck.thunderhaul | $1,700 | 6900 kg | 45 / 9 deg | 4.5 | No |
| Road Palace | taxi.truck.roadpalace | $2,400 | 9300 kg | 34 / 7 deg | 3.4 | Furnished / refrigerator |

Existing separate cab Rigidbody remains 1500 kg. The new models use 70-deg low-speed lock and the existing speed-sensitive handling implementation. These are editable model defaults, not demonstrated comparative driving-feel conclusions.

Existing fare configuration is 500 cents base, 1.5 cents/metre, 2 cents/second before modifiers/tips. A nominal 1 km / 180 s fare is 2360 cents, giving approximately 25/38/53/72/102 such fares for the five prices. This is an authored economy rationale, NOT measured ordinary-fare income or final balancing acceptance. No new currency or wallet was added.

## Runtime Architecture

- `TruckTaxiTruckModel`: metadata on real NWH-compatible prefabs; mass, body/LODs, explicit paint materials, wheelbase/hitch offsets, cockpit, seats and sleeper capability.
- `TruckTaxiTruckRig`: applies those authored visuals and bindings to the ONE existing canonical NWH actor.
- `TruckTaxiTruckFactory`: validates grounded/stationary/clear-space ownership transactions; preserves input, cameras, GPS, transmission, recovery and other canonical components. Rollback restores the old actor; unrecoverable rollback fails closed rather than resuming a damaged rig.
- `TruckTaxiDealership` / `TruckTaxiDealershipFacility`: shared HUD modal, Heat controls, pure-visual model preview and regional facility binding. No preview NWH actors, physics, listeners or duplicate player controllers.
- `TruckTaxiSession.TruckIdentityMigration`: explicit known legacy starter-ID migration through existing Career data. No fuzzy model matching.
- Existing Career catalogs: `Assets/LWS/TruckTaxi/Edenborough/CareerCatalog.asset` and `Assets/LWS/TruckTaxi/Resources/TruckTaxi/Career/Catalog.asset`.

Vehicle switching refuses active fares, races, occupied companion/passenger cabs, attached trailers, sleep/private interactions, travel, recovery, unstable or moving vehicles. Purchases are guarded by existing wallet/ownership transactions. A repeat purchase cannot charge again. Observer failures after a successful Career commit do not roll back only the physical actor.

Pixel Crushers remains the save authority. The existing Taxi semantic payload already persists ownership, equipped truck and per-truck paint/upgrades. Loading applies the selected physical model before component damage, fuel and final vehicle-clearance restoration. No new serializer, save files, slots or storage backend.

## Dealership And Paint

Demo City uses the reusable `Assets/LWS/TruckTaxi/Resources/TruckTaxi/Trucks/DealershipFacility.prefab`, positioned relative to the existing player spawn (36 m right, 20 m behind, ground-resolved). Edenborough reuses the existing `TruckTaxiEdenboroughSceneBinding.garage` and `garageVolume`.

The existing garage interaction opens customization; choose **DEALERSHIP** to browse all six models. A routeable **Truck Dealership & Customization** marker is registered per region. Physical drive-in approach and controller-only human walkthrough remain unverified.

Existing GarageDraft owns preview/apply/cancel/default. The garage now previews actual selected models and equips through the validated factory, not a decorative mesh menu. HSV paint affects explicit primary-body slots, not glass, chrome or tires. Existing compatible upgrades and per-truck paints remain Career-owned.

## Cockpit, GPS, Lighting And Sleepers

NWH caches its camera local pose, so new cab eye placement uses a removable parent mount around the SAME camera. Removing that mount first detaches the camera safely. Native camera input and starter pose are preserved.

New cab eye: `(-0.56, 2.20, 0.62)`; GPS mount: `(-0.08, 2.18, 1.35)` in tractor coordinates. Change these in the model metadata / roster authoring, not vendor camera code.

Actual Windows review found the Taxi GPS adapter had moved Compass into the starter's mesh-specific interior. `SetVehicleDashboardMount` now reparents that SAME Compass display to each new cab mount, level to the truck, at 0.00042 pixel scale; null restores the exact original starter parent/pose/scale. No new minimap, routing engine or vendor changes. Existing map settings remain available. The GPS route test requests the existing regional exit through existing LWS navigation and confirms vendor route presentation.

New lamp meshes bind to native NWH low/high, turn, brake and marker groups, with four beam Lights. Damage bindings refresh on model changes and respect native on/off state. Windows checks exercise headlight/horn commands; audible horn and night beam illumination are not claimed observed.

Highway King, Nightrunner and Road Palace include real modeled berth, mattress, trim/lining, storage and curtains; Road Palace also includes extra cabinets/refrigerator. Existing WorldLife, CabRest and DriverNeeds supply sleep, clock/fatigue and controls. The test exercises all three 2-hour rests, advances at least 7200 game seconds, lowers fatigue and restores driving mode. Passenger/companion seating clearance still needs visual gameplay acceptance.

## Validation Evidence

| Check | Actual result |
|---|---|
| Blender exports round-trip | 15 / 15 PASS; metadata/normals/material checks in `Tools/TruckTaxiVehicleFactory/ExportValidation.json` |
| Static trailer geometry fit | 5 / 5 PASS in `TrailerFit.json`; NOT substituted for physical coupling |
| Career EditMode | 82 / 82 PASS in `Evidence/EditMode-Final-82.json` |
| Integrated PlayMode | 3 / 3 PASS in 84.54 s in `Evidence/PlayMode-ActualGps-3.json` |
| Last GPS orientation/starter restoration and six-model transfer | 1 / 1 PASS in 34.40 s in `Evidence/PlayMode-FinalGpsRestore.json` |
| Native NWH trailer physics | All six: real ConfigurableJoint/connected body, adapter attached/detached, 3 s bounded velocities, switching blocked when coupled |
| Actual furnished sleepers | All three: view/rest/clock/fatigue/control return PASS |
| Selected model/paint/ownership transfer Demo -> Eden | PASS in the actual integrated PlayMode test |
| Earlier Windows core restart runs | 12 / 12 process sessions PASS; retained separately as historical evidence, not the final GPS build |
| Final Windows build | PASS, 2026-10-15 15:17:21 UTC |
| Final Windows six-truck runs | 12 / 12 PASS: six seed/quit/fresh-process Continue pairs; `Evidence/WindowsFinalSessions.json` |

Test profile folders are GUID-isolated under the temporary cache. Real player saves were not modified. Seed tests use a clearly identified disposable affordability fixture; this is not a claim that normal play earned that money. Continue sessions use actual main-menu Continue and no engine/rebind/recovery/menu repair before native driving.

## Screenshots And Performance

Source art previews: `Assets/LWS/TruckTaxi/Art/Trucks/Previews/` (six front-three-quarter views). All 27 front/side/rear/cockpit/sleeper Blender reviews: `Tools/TruckTaxiVehicleFactory/ReviewRenders/`.
Actual Unity captures: `Builds/TruckTaxiDemo/Validation/{Roster,Cockpit,Dealer,Sleeper}_<stable-id>.png`.
Final Windows captures and raw 5-second frame samples are retained under each isolated run and copied into `Tools/TruckTaxiVehicleFactory/Evidence/FinalWindows/`.

LOD0 body triangle counts: HighwayKing 77,960; Nightrunner 72,482; Outlaw 66,042; Thunderhaul 62,596; RoadPalace 86,040. LOD1 approximately 25-32k, LOD2 approximately 9-11k; distant interiors/gauges/steering are omitted. Shared, binding-aware material batches are approximately 41-47 per model. Preview uses one explicitly rendered camera/RT, not six live vehicles.

Frame samples are stationary live Demo City with NPCs/weather enabled on a shared GPU. They are NOT isolated benchmarks or a back-to-back highway handling/performance comparison. No performance improvement claim is made.

Final GTX1660Ti frame samples (5 seconds each, model order Starter/HighwayKing/Nightrunner/Outlaw/Thunderhaul/RoadPalace): median approximately 18.74/18.73/18.73/18.73/18.73/18.74 ms; p95 approximately 25.03/24.97/25.02/25.03/25.01/31.25 ms. `Evidence/WindowsFinalPerformance.json` is the exact machine-readable source; rounded values here are descriptive only.

The final seed runs opened the actual dealer and native cockpit/exterior cameras, generated a regional road-graph route and confirmed Compass presentation, exercised headlight/horn semantics, moved at least 3 m through native throttle, verified braking and steering, applied paint and saved. Fresh-process Continue restored the selected physical model/paint and drove without engine/rebind/recovery intervention. This is a bounded live driving test, not the unexecuted ordinary-fare or highway-feel acceptance.

## Remaining Acceptance / Limitations

- Every-model ordinary human-driven taxi fare and real progression income balance: NOT RUN.
- Controller-only dealership/garage navigation and physical drive-in approach on both maps: NOT RUN end-to-end.
- Back-to-back highway feel, intentional rollover/recovery, wheel hardware, night beams, audible horn/radio: NOT RUN on this final roster.
- Passenger/companion visible seating and sleeper privacy interaction: integration retained; final visual clearance NOT VERIFIED.
- Existing environment snow/traction/track authority remains. Player hierarchy is excluded by the existing environment-snow adapter; new hood/roof snow appearance is NOT VERIFIED. MarkDirty does not clear existing track history.
- Paint applied through actual garage draft/Career semantics and process persistence; final mouse/controller HSV walkthrough not claimed.
- Regional travel tested in Unity; no new six-model cross-region Windows acceptance claim.
- Art is an original stylized implementation reviewed in Blender and gameplay captures, not a substitute for the user's art-quality acceptance.

These limitations prevent a claim that every original Windows acceptance step has passed. The delivered build remains available for those checks; no source-only completion claim.

## Vendor-First Report

VENDOR ASSETS AUDITED: NWH vehicle/wheel/camera/lighting/trailer APIs and existing GR3D truck, Compass Navigator Pro, Heat, Pixel Crushers; existing Weather Maker/Weatherade integration.

VENDOR ASSETS USED: NWH native physics/coupling/cameras/lights; Compass actual map/routes; Heat UI controls; Pixel Crushers existing persistence; existing owned garage art and weather/road systems.

RELEVANT ASSETS NOT USED: UTS player trailers (NWH owns player physics); vendor demo worlds/controller replacements (existing Taxi architecture retained); no extra modeling packages.

CUSTOM SYSTEMS CREATED: project-specific roster metadata, authored rig application, safe canonical model switch, dealer UI/facility binding, known-ID migration and focused validation harness extensions.

VENDOR SOURCE MODIFIED BY THIS TASK: NO. Pre-existing dirty vendor changes remain untouched.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO. No second physical truck, navigation, save, input, economy or sleep authority.

## Commits

- `38d8848f`: five original Blender bodies and validated LOD exports.
- `78fab8de`: six-truck NWH integration, real prefabs/catalogs, dealership, sleepers, exact starter GPS restoration and focused tests.
- Final validation/report commit follows that integration commit; its identifier is reported in the final handoff.
- PUSHED: NO.
