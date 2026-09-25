# Truck Taxi Prompt 002 Completion Preflight

Completion pass, 2026-09-24. Preserve baseline `ff0ee0f6` and current working
changes. Do not refresh the city, replace the tractor, or include unrelated
Dead Air, freight, vendor-import, or project-setting changes in these commits.

| System | Classification | Current authority / remaining work |
| --- | --- | --- |
| Demo scene | WORKING | `Scenes/TruckTaxi_DemoCity.unity`; authored city retained |
| Bootstrap/spawn | WORKING | `TruckTaxiBootstrap`, `LwsPlayerTruckSpawner`, `TruckTaxi_InterstateTractor.prefab`; unhitched sandbox |
| Tractor physics/input | CORE INTERSTATE SYSTEM | NWH VehicleController, LWS input, transmission, stability/recovery, wheel integration; unchanged |
| Steering | TRUCK TAXI OVERRIDE | reversible `TruckTaxiVehicleHandlingOverride`, NWH public steering settings |
| Navigation | CORE INTERSTATE SYSTEM | LWS graph/planner/navigation and Compass Navigator; no second map |
| GPS/offer map | TRUCK TAXI OVERRIDE | `TruckTaxiGPSAdapter` aligns live cab Canvas; `TruckTaxiOfferMap` reuses Compass texture |
| Traffic/pedestrians | WORKING | `TruckTaxiTrafficAdapter`, `TruckTaxiPedestrianPopulation`, UTS paths/prefabs |
| Ride offers/distances | WORKING | immutable `TruckTaxiRideOffer`, `TruckTaxiRouteDistanceService`, LWS route geometry |
| Rides/requests/fares | WORKING | `TruckTaxiSession`; local taxi semantics, not freight jobs/persistence |
| Pickup/dropoff | PARTIALLY IMPLEMENTED | `TruckTaxiRideLocation.detectionRadius`, stationary boarding; VFX/representations remain |
| HUD/debug | WORKING | `TruckTaxiHud` (Heat/TMP/uGUI), `TruckTaxiDebugPanel`; bottom telemetry and offer map |
| Passenger Factory | PARTIALLY IMPLEMENTED | UITK window; voice/dialogue works, models/rig/prefabs/roster/batch remain |
| Chatterbox | WORKING | external WorkHere engine; editor queue and `Tools/TruckTaxiPassengerFactory/Chatterbox/generate_dialogue.py` |
| Generated audio | WORKING | `Passengers/GeneratedAudio/<ID>/`; generated clips only, references external |
| Runtime dialogue | MISSING | generated clips/subtitles through existing Pixel Crushers bark lifecycle |
| Passenger runtime/ejection | MISSING | representations, seats, pairs, reversible modifiers, ejection lifecycle |
| Models/animation | MISSING | reuse available assets; explicit fallback/manifests for unavailable final content |
| Windows pipeline | WORKING | `TruckTaxiDemoBuilder.BuildWindows`, Windows64 development player, `TruckTaxiPlayerSmokeTest` |
| Tests | WORKING | Python adapter, Taxi EditMode and PlayMode; extend for remaining features |

## Checkpoint And Commit Boundaries

Previous checks: Python 11/11, EditMode 36/36 including external cached WAV
generation/import, PlayMode 3/3. Controlled keyboard/NWH U-turn: 9.54m,
176 degrees, 7.2 MPH peak, upright dot 1.00. Automated tests are not a claim of
human highway handling validation. Captures cover 1080p, 1440p, ultrawide offers
and the actual rectangular cockpit Compass display.

First commit: voice adapter, editor voice UI/queue/import, semantic voice/dialogue
assets, tests, metadata and voice docs only. Exclude temporary integration WAV,
Jobs/Logs, machine-local settings and unrelated work. Second commit: remaining
taxi-only gameplay/factory/content/tests/docs. No push. Build and launch Windows
player after source validation.

## Vendor First

Reuse NWH, Compass, LWS navigation, UTS, Heat/TMP, Pixel Crushers Dialogue and
external Chatterbox. Inspect owned Asset Store access/cache before pickup VFX;
do not confuse imports with full ownership. Audit human models/animations before
creating fallbacks. Custom code is taxi semantics, authoring and adapters.

## Limits At Start

Factory/roster/runtime passenger/ejection acceptance is outstanding. Character
voice profiles have no automatic casting to the user's recording. The integration
WAV is a fixture, not proof all passengers have voices. Final art and specific
references may remain missing with working fallbacks and explicit reports.
The existing Windows EXE predates this pass and must be rebuilt/tested.
