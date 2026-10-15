# Navigation / Map / Service Rings: Focused Evidence

Date: 2026-09-30. Scope: supplied consolidated prompt sections 23-27 and 32.
Status: source implementation and non-Unity checks complete; integrated live acceptance remains parent-owned.

## Implemented

- One four-layer lease authority: RideFinal < PassengerDiversion < PlayerService < Emergency.
- Replacing a service replaces only that layer. Scoped completion restores the highest surviving intent. Owner/target/token checks reject stale completions.
- Actual service arrival does not imply successful service use. Completion is explicit; manual place navigation can finish when stopped inside its bay.
- Existing ride, optional-stop, private-stop, stop-guidance, and restore APIs use the layered authority. Development NAV PUSH/SUSPEND/POP/RESTORE traces and a Show Navigation Stack context menu expose ownership.
- Places-list submit and Compass POI click immediately route. Focus does not route. Reopening selects the active stable ID and page; active rows and Compass markers retain explicit active styling. Closing the map does not clear GPS.
- Both existing Compass displays bind to the canonical truck Rigidbody transform before Compass LateUpdate. Steering-wheel/camera transforms are excluded from heading ownership.
- Actual ServicePoint capabilities determine icons and combined details. Metadata-only FoodStop/service claims become neutral Place entries. Regional ride destinations, river, bridge, and regions no longer masquerade as food/scenic services.
- Nearby loaded service, optional-stop, and real ride bays render persistent rings at actual bay centers and interaction radii, independently of selection. One service point has one combined-capability ring.
- A shared configurable palette supports ambient, nearby, selected, inside-too-fast, valid, interacting, and unavailable states. Existing passenger/private presentation and lap checkpoints retain their hooks; overlapping bay rings are suppressed.
- Validator flags false service icons, missing real service markers/rings, duplicate IDs, incorrect map/ring centers, actual vertex/radius mismatches, missing Store/refuel interaction components, and refuel radius disagreement.
- Focused Undo-supported authoring aligns selected explicit Store/Refuel service interactions. No service is inferred from a decorative name, building appearance, or ride-location category.

## Owned Files

Modified runtime: TruckTaxiGPSAdapter, TruckTaxiFullMapPresenter, TruckTaxiMapMarkers, TruckTaxiMapMarker, TruckTaxiMapIconRegistry, TruckTaxiOptionalStops, TruckTaxiPickupZoneVisualizer, and TruckTaxiRegionalWorld.RegisterMapMetadata.

Added runtime: TruckTaxiNavigationIntents, TruckTaxiServicePresentation, TruckTaxiStopRingPalette, TruckTaxiServiceRings, and TruckTaxiServiceMapValidation.

Editor: new TruckTaxiServiceMapValidator; four-line extension to existing TruckTaxiMapIconSetup so its normal setup handles appended Drink/Sleep/Place enum values. No existing enum values changed.

Tests: new TruckTaxiNavigationIntentTests and TruckTaxiServiceMapValidationTests (19 parameterized cases); updated two legacy private-field fixtures to use real lease APIs.

Tools: Tools/TruckTaxiValidation/Test-NavigationIntents.ps1 and Test-SourceDiagnostics.ps1.

## Executed Evidence

1. Production C# ownership helper executed in memory: 9 focused assertions passed, including exact Final -> Scenic -> Restroom -> Scenic -> Final restore, wrong-owner/stale-token rejection, service replacement under emergency, and ride release retaining service.
2. 10,000 deterministic randomized production-C# lease mutations matched an independent four-slot reference model.
3. Roslyn semantic diagnostics using cached Unity response-file references and current in-memory runtime/editor source references:
   - LWS.TruckTaxi.Runtime: 203 source files, 0 errors.
   - LWS.TruckTaxi.Editor: 48 source files, 0 errors.
   - LWS.TruckTaxi.Tests.EditMode: 80 source files, 0 errors.
4. Owned tracked diffs passed git diff --check. Line-ending conversion notices only.

The diagnostics script emits NO assemblies and invokes no Unity/compiler executable or build pipeline.
EditMode tests were authored and semantically checked, NOT run in Unity. No live rendering, driving, screenshots, playtest, or Windows build is claimed.

## Parent Integration / Acceptance

- The Needs agent's Sleep capability is present. Its current RestoreRideRoute uses ClearPlayerServiceDestination instead of ClearDestination; sleep completion calls CompleteServiceDestination.
- Parent must wire successful fuel service completion to GPS.CompleteServiceDestination(station.stableId[, capturedToken]). Fuel currently has no completion call. Use the same explicit completion pattern for any other asynchronous service.
- Parent may drive TruckTaxiServiceRings.SetInteracting(id, bool) from service start/end/cancel events; the state hook exists but this workstream does not edit HUD/bootstrap/service execution.
- Run Truck Taxi > Map > Ensure Service Marker Icons (or the existing Ensure Typed Map Icons) once after import. The three appended icon entries are not serialized yet because Unity launch/raw asset YAML were prohibited.
- Let Unity generate metadata for new scripts during the integrated import, then run TruckTaxiNavigationIntentTests, TruckTaxiServiceMapValidationTests, and the existing map/service suites.
- Run Validate Services And World Rings in the loaded test region. Fix actual authored component/radius mismatches using Align Selected Service Interactions only on deliberately selected service objects.
- Manual acceptance: park and turn steering fully in both north-up/heading-up; verify goal stability on HUD and cab GPS; click/list-submit while a Scenic request is active; close/reopen; complete a restroom interaction then Scenic; verify each restored route; drive all advertised service bays and check ring center/radius and actual action.
- Verify ring labels/brightness/overlap in the integrated scene and monitor nearby rendering cost. No manual-quality or performance claim is inferred from source checks.
- No GPS persistence added. Whole-session/load reset remains the only intended use of ClearDestination.

## Vendor Accounting

VENDOR ASSETS AUDITED

- Assets/Plugins/Kronnect/CompassNavigatorPro: README identifies 6.0.4; inspected CompassPro follow/miniMapOrientation, CompassProPOI, CompassProEvents, mini-map LateUpdate projection, and demo LevelManager click/hover event usage.
- Assets/Heat - Complete Modern UI/Textures/Icons: existing HUD map arrow and Misc Shop icon mappings and installed icon inventory. No suitable distinct Drink/Sleep/neutral Place silhouettes were present in that inventory.
- Existing LWS Compass adapter, LWS navigation service/road graph, NWH-backed truck/fuel integration, and existing TMP/LineRenderer stop presentation were audited as integration boundaries.

VENDOR ASSETS USED

- Existing CompassPro HUD/cab displays, CompassProPOI public style/circle fields and stable IDs, follow transform and Follow orientation, and OnPOIMiniMapIconMouseClick. LWS navigation still calculates routes through ILwsNavigationService.RequestRoute.
- Existing Heat map registry mappings and TMP/uGUI presenter conventions remain intact. The truck Rigidbody remains NWH-owned.

RELEVANT ASSETS NOT USED

- Compass's NavMesh planner is not substituted for the existing LWS truck-road graph planner. The requested missing behavior is taxi-specific intent ownership, not a second planner.
- No new DOTween animation was necessary for static bay rings and vendor active-marker treatment.
- No new Scene Streamer calls, road authoring, terrain tooling, save framework, or world regeneration was required.

CUSTOM SYSTEMS CREATED

- Taxi-specific layered intent leases, service capability projection, nearby bay ring coordination/palette, focused validator/authoring hooks, and regression tooling.
- Three project-owned semantic glyphs extend the existing icon authoring path only.

VENDOR SOURCE MODIFIED

NO. Pre-existing dirty vendor files were left untouched.

DUPLICATE VENDOR FUNCTIONALITY CREATED

NO. No new minimap, map camera, pathfinder, physics, service execution, or world generator.

No bootstrap/HUD edits, Unity launch, builds, commits, raw asset YAML, or excluded erotic feature implementation were performed by this workstream.
