# GPS Stabilization Handoff

Scope: Priority 1/14 only. Shared tree; no Unity, builds, commits, vendor edits, or serialized asset edits performed.

## Implemented

- `Scripts/TruckTaxiNavigationIntents.cs`: deterministic priority is Emergency > RivalPassenger > PlayerService > RidePickup > PassengerDiversion > CompanionTask > RideFinal. Companion tasks no longer replace service leases. Tokens remain the authority for late completion rejection. Enum ordinals are runtime ordering, not persistence IDs; reconstruct semantic owners on load.
- `Scripts/TruckTaxiGPSAdapter.cs`: existing private-stop entry points now own CompanionTask. Accepted fare pickup covers it; boarding removes pickup and restores the companion before the underlying fare. Existing IsServiceDestination presentation semantics include companion stops for caller compatibility.
- Route reconciliation uses the existing LWS navigation service every 0.5 unscaled seconds. Missing/wrong routes are requested again, arrived/ownerless routes are cleared, and explicit Stop Navigation remains suppressed. RouteReady checks destination identity. No new router or save store.
- Both Compass capture events now use the existing baked, unlit regional tiles. Only the instrument camera is temporarily classified Preview (Weather Maker's ShouldIgnoreCamera explicitly rejects it), and its URP post-processing is temporarily disabled. Camera state is restored after capture, disable, and destruction. World cameras, weather profiles, accumulation, and global fog are untouched. Existing .10s HUD/.15s cab timed snapshots and continuous modal maps are preserved.
- `Scripts/TruckTaxiWorkAreaMapPanel.cs`: hazard/work-area projection runs after Compass LateUpdate; wholly offscreen ellipses are culled, partial ellipses keep their true center and use the existing RectMask2D. No screen-edge translation.

## Evidence And Limits

Static causes: companion and manual service shared PlayerService; accepted pickup occupied lower RideFinal; presentation repair only re-presented an existing route; baked capture covered only the full map. Hazard projection previously ran before Compass's camera update and lacked explicit offscreen rejection. No explicit hazard center clamp was found, so the reported hurricane relocation is not runtime-confirmed fixed.

RivalTheft already calls ReleaseNavigationIntent with layer/owner/target/token. The GPS release seam clears or restores the route and is now covered by focused tests. The exact runtime cause of the human-observed stale stolen route remains unconfirmed; do not claim theft lifecycle acceptance from these tests.

## Parent Hooks

1. `Scripts/TruckTaxiHud.cs`, companion request panel branch: retain authored companion name/task; show `GPS.CompanionDestinationLabel` and `TruckTaxiUsUnits.Distance(GPS.CompanionDistanceMeters)`. These stay available while a pickup/service covers the task. General active-route UI can use TargetLabel/TargetDistanceMeters. Do not hide the accepted pickup panel merely because a companion exists.
2. Persistence coordinator: after world/player/graph readiness, clear old intents once via GPS.ClearDestination(), then reconstruct supported semantic ride/companion/service owners through existing entry points. Call GPS.ReconcileNavigation() after reconstruction. Do not persist lease tokens or enum ordinals. The adapter does not invent absent saved intent data.
3. `Scripts/TruckTaxiRivalTheft.cs`, ReleasePresentation: existing captured oldFare/oldToken release occurs AFTER actor teardown. Move the exact existing ReleaseNavigationIntent(RivalPassenger, "rival-theft." + oldFare.Id, oldFare.DestinationId, oldToken) before actor operations, or into a finally block, so visual teardown failure cannot strand the lease. Preserve identity capture before clearing fare/token. Validate delivery/cancel/reset and suspended-under-emergency paths.
4. `Scripts/TruckTaxiDriverNeedsCoordinator.cs`, RestoreRideRoute: when a private stop exists, the current early-return path never clears a covering PlayerService lease. Release the owned service first (prefer captured target/token), then restore the already-existing companion intent; do not replace it to force priority. This file was outside ownership.
5. Parent companion owner must continue pairing SetPrivateStopDestination with ClearPrivateStopDestination on completion/cancel/ejection. Current callers were preserved. No Bootstrap, Hud, Session*, RivalTheft*, Companion* files were edited by this workstream.

## Tests And Validation

Added to `Tests/EditMode/TruckTaxiNavigationIntentTests.cs`:
- CompanionSurvivesNewPickupServiceAndStolenPassenger
- LostOrWrongRouteRestoresButExplicitStopRemainsSuppressed
- StolenLeaseReleaseWithoutUnderlyingIntentClearsRoute

Added to `Tests/EditMode/TruckTaxiGPSDisplayTests.cs`:
- InstrumentCaptureRestoresCameraWithoutChangingWorldWeather
- HazardFootprintNeverMovesToViewportEdgeDuringPanOrZoom

Static diff/whitespace review only; tests are added, NOT executed. Parent runs focused tests and compilation. Required visual checks: HUD/cab/full map through blizzard, pause, night, GPS toggle, Planning/full-map close, sleep and load; world blizzard/snow unchanged; hurricane wholly/partially offscreen and pan-back; stolen delivery restores companion/fare or leaves no route. Check that baked catalog covers each sandbox location and verify no black capture from renderer features or stale weather command buffers. Capture tests verify scoped camera state, not rendered pixels. Do not claim feature freeze.

## Vendor Report

- VENDOR ASSETS AUDITED: Compass Navigator Pro under `Assets/Plugins/Kronnect/CompassNavigatorPro` (public capture events, timed snapshots, UpdateMiniMapContents, sample capture wiring); Weather Maker under `Assets/WeatherMaker` (ShouldIgnoreCamera, command-buffer camera filter, URP feature). Existing LWS Compass adapter and baked tile material/baker inspected.
- VENDOR ASSETS USED: Compass Pro public OnMiniMapBeforeCapture/OnMiniMapAfterCapture, UpdateMiniMapContents and existing route presentation; Weather Maker's Preview-camera exclusion contract; existing Unity URP unlit tile material and per-camera post-processing API.
- RELEVANT ASSETS NOT USED: Weatherade untouched; world snow/road accumulation remains its responsibility, while the instrument reads baked tiles. Pixel Crushers persistence remains parent-owned.
- CUSTOM SYSTEMS CREATED: None; existing LWS intents, GPS adapter, tile capture and masked area graphic extended.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
