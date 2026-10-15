# Navigation / Map / Service Ring Integration Contract

Workstream scope: supplied late-September prompt sections 23-27 and 32.
No bootstrap/HUD, vendor, scene, prefab, raw asset YAML, Unity launch, build, or commit changes.

## Parent / Needs Integration

- `TruckTaxiServiceCapability.Sleep` is expected from the Needs agent in ServicePoint.cs.
- Navigation is one layered authority in GPSAdapter: RideFinal < PassengerDiversion < PlayerService < Emergency.
- Existing SetRideDestination/SetPickupDestination write only RideFinal. SetStopDestination writes PassengerDiversion.
- `GPS.CompleteServiceDestination(stableId, optionalLeaseToken)` or `GPS.ClearPlayerServiceDestination()` pops PlayerService and restores the suspended top. Complete only after the real interaction succeeds. Real service routes do not auto-pop on arrival.
- Replace NeedsCoordinator.RestoreRideRoute's whole-stack `ClearDestination()` with scoped player-service release. Do not reconstruct/overwrite scenic/ride targets there.
- Fuel completion should call CompleteServiceDestination for the station ID. A token obtained via `GPS.ActiveLeaseToken` protects against a delayed completion of a replaced route.
- `TrySetNavigationIntent(layer, ownerId, stableId, label, position, markerType, radius, requiresCompletion)` and `ReleaseNavigationIntent(layer, ownerId, stableId, token)` support passenger-request and emergency owners.
- Existing companion private-stop methods remain service-scoped; no excluded presentation behavior added.
- `ClearDestination()` is now expressly whole-session reset/load only. Session lifecycle must use ClearRideDestination.
- `TruckTaxiServiceRings.SetInteracting(stableId, bool)` is the optional interaction-state hook. Rings initialize from existing PickupZoneVisualizer.Initialize; no new bootstrap dependency.
- Marker registration and rings use ServicePoint.Position / InteractionRadius, never a building or POI center. ServicePoint and interaction implementations must agree on the real bay/radius.
- UI/debug may read `GPS.NavigationStack` and call `GPS.ShowNavigationStack()`; development NAV PUSH/SUSPEND/POP/RESTORE logs contain owner and target IDs.
- No GPS persistence added. Parent load/reset should clear all intents, restore session semantics, then optionally restore a persisted player-service intent if implemented later.

## Authoring And Validation

New Drink, Sleep, and neutral Place marker enum values are appended, preserving serialized values.
The focused editor tool will provision these registry entries using project-owned glyphs and report service/marker/ring mismatches. Run it once after import in the parent's integrated Editor pass, without regenerating the world.

Live Unity/manual verification remains parent-owned; static and pure ownership tests are provided here.
