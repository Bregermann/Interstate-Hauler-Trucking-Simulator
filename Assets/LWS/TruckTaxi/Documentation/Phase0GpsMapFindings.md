# Phase 0 GPS / Regional Map

Scope: GPS adapter, full-map presenter, tile layer, map markers and focused tests. Existing timed-snapshot work was preserved. Integrated validation below supersedes the initial source-only workstream handoff. No vendor, scene or material asset was edited for these GPS fixes.

## Evidence And Fixes

- Route recovery: `Claim` previously returned success for an unchanged active destination without repairing guidance; `RestoreActiveRoute` rejected unsuppressed intents. Both now support repair without replacing the intent lease. Compass `UpdateRoute` draws a separate UI mesh, so refreshing the RenderTexture alone cannot repair all route failures. The Taxi adapter now coalesces transition requests, re-presents the current valid unsuppressed LWS route, invalidates that UI mesh via public route settings and requests `UpdateMiniMapContents(1)`. Visibility, resolved rect changes, route replacement, pause/resume, clock jumps/night transition and lost vendor route/texture trigger bounded recovery. Runtime confirmation of the original intermittent symptom is still required.
- Regional capture: vendor `SetupMiniMap` resets the camera mask. Previously Taxi ORed the tile layer into that camera separately, while tile visibility depended on SRP callbacks; live streamed geometry could also obscure tiles below ground. Tiles now use Compass `OnMiniMapBeforeCapture` / `OnMiniMapAfterCapture`, render exclusively during that camera capture with occlusion disabled and adequate far clipping, then restore the camera and hide all tile renderers. Uncovered atlas space uses the existing bake background color. This does not regenerate missing/bad source tiles; inspect actual map pixels in the player.
- Duplicate hotel: `TruckTaxi_DemoCity` has ride ID `taxi.stop.08` and service ID `TT_SLEEP_taxi.stop.08` on the same object, explicitly joined by `TruckTaxiServicePoint.location`. Regional metadata registered both; the old list only de-duplicated identical IDs. Presentation now canonicalizes this explicit relationship, retains fare/save IDs, keeps active fare icon treatment, and does not collapse unrelated same-name places. Stream-source cleanup removes orphan generated markers or restores their persistent metadata; repeated registrations stay idempotent.
- Intercity offer framing: rendered tests exposed the public Compass `miniMapFollowOffset` setter clamping the requested center offset to its 1000m `miniMapDragMaxDistance`. Destination endpoints could remain beyond the viewport despite a sufficient orthographic size. Taxi offer preview now includes the actual pickup/drop-off stops in its bounds and temporarily expands that public drag limit only as far as the preview needs. Closing preview restores the original limit and zero follow offset. This does not change driving zoom, routing or the physical cab mount.

## Parent Hook

After successful load has restored navigation intent and UI/display state, call:

```csharp
GPS?.RequestPresentationRefresh();
```

This queues one refresh on the next Taxi late update, before Compass's late-update capture. It works while paused, does not change snapshot frequency, and does not resume Stop Navigation or reconstruct missing intent. Restore the intended destination first if loading cleared it. Bootstrap/HUD remain parent-owned.

## Validation Handoff

- `TruckTaxiGpsRecoveryTests` covers paused/coalesced refresh, show/hide recovery, lost vendor route, suppression, unchanged-intent restoration, hotel identity/active marker, source unload/rebind, exclusive/repeated tile capture, camera restoration and intercity drag-limit framing.
- Run existing `TruckTaxiGPSDisplayTests`, `TruckTaxiNavigationIntentTests`, `TruckTaxiRegionalMapTests`, `TruckTaxiMapMarkerTests`, `TruckTaxiMapPolishTests`, and `TruckTaxiPresentationPlayModeTests.CockpitPresentation`; retain its actual cab-route pixel assertion.
- Visible focused Windows: hold a valid fare route through sleep/time jump/night, pause, temporary service/companion completion, GPS off/on, Planning and full-map open/close, then real save/quit/relaunch/load. Repeat regional open/close at least ten times including unloaded regions and verify actual road pixels across the atlas, not just icons or `BakedTilesVisible` assignment. Verify one hotel entry and one active endpoint. Capture before/after screenshots and observe sustained driving performance with the existing timed/continuous A/B probe.

## Integrated Evidence, 2026-10-08

- Focused EditMode: 83/83 passed across GPS recovery, occupant polish, facility diagnostics, rival theft and persistence (`Logs/TaxiFoundation-Phase0-EditMode2.xml`). This is not an all-project pass.
- Rendered PlayMode: 5/5 passed (`Logs/TaxiFoundation-Phase0-PlayMode4.xml`). `CockpitPresentation` retained its actual cyan route-pixel comparison with the route disabled/enabled. Intercity offer endpoints remained inside the preview viewport. `RegionalMapRepeatedCaptureRetainsRealRoadPixelsAndRestoresCamera` exercised ten full-map open/close captures, checked nonblank road-map pixels, camera restoration and return to timed capture.
- Reviewed `Builds/TruckTaxiDemo/Validation/RegionalMapRecovery9.png`: roads/atlas present, not a black map with icons only. Automated camera captures are not human cockpit/readability acceptance.
- Windows development checkpoint built with zero errors (`Logs/TaxiFoundation-Phase0-WindowsBuild2.log`). Visible focused moving workloads completed; see `PerformanceBaseline.md`.
- The original intermittent symptom through actual sleep, companion/service completion and a quit/relaunch/load cycle is NOT fully validated. Retain those manual checks; do not infer them from the pixel test or successful build.

## Vendor Report

- VENDOR ASSETS AUDITED: installed Compass Navigator Pro 4 at `Assets/Plugins/Kronnect/CompassNavigatorPro` (README version 6.0.4; shared adapter's diagnostic constant still says 6.0.2), public route/minimap/capture events and private implementation read-only; existing LWS navigation and map tile baker.
- VENDOR ASSETS USED: existing `CompassPro` / `CompassProPOI`, `UpdateMiniMapContents(1)`, capture UnityEvents, `showRoute`, `routeClearOnComplete`, existing registered LWS presenter. Existing URP Unlit tile material cloned through Unity APIs.
- RELEVANT ASSETS NOT USED: Compass NavMesh routing and save APIs; LWS graph and existing Pixel Crushers persistence remain authoritative. No additional vendor or framework needed.
- CUSTOM SYSTEMS CREATED: none; existing Taxi adapters/presentation repaired and focused regression tests added.
- VENDOR SOURCE MODIFIED: NO. Pre-existing vendor working-tree edits were left untouched.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
