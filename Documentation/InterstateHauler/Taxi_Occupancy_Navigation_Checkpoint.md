# Occupancy and Navigation Source Checkpoint

Source changes only. No Unity compilation, tests, Editor operation, build, or Windows proof performed by this task.

## Integrated EditMode Correction

EditMode2 follow-up (`Logs/TaxiCorrection-EditMode2.xml`): the native-allocation
exception is gone. Three remaining failures were exact HDR Color struct equality
(printed channels matched but native float round-trip differed), and two fixtures
assuming ordinary MonoBehaviour OnDisable callbacks execute automatically outside
PlayMode. The tests now compare all four color channels with 1e-5 tolerance and
explicitly invoke the actual OnDisable callbacks twice after disabling. Assertions
still require zero visible beams, disabled renderers, and released race ownership.
They additionally require pool reuse after beam re-enable and no race resurrection
after GPS re-enable. No GPS production cleanup was bypassed or changed.

This review also found and fixed a genuine adjacent bug: public beam Refresh did
not gate on its own isActiveAndEnabled state, so a manual refresh while disabled
could report visible beams. It now keeps the entire pool hidden and VisibleCount
zero while disabled, covered separately from the callback assertions. Changes for
this follow-up are limited to ObjectiveBeams, the focused fixture, and this doc.
No tests were rerun by this task; Main owns the next validation pass.

Main's `Logs/TaxiCorrection-EditMode1.xml` reports 12 passed and 2 failed cases in
`TruckTaxiOccupancyNavigationRegressionTests`. Both failures are a production
lifecycle defect, not missing fixture data: constructing `MaterialPropertyBlock`
in the MonoBehaviour field initializer throws Unity's `CreateImpl is not allowed`
exception at construction. The subsequent `Refresh()` call then dereferenced
the uninitialized block at line 68 in that snapshot.

Correction: the field is now managed-null until explicit `Initialize(source)`
allocates the native property block on Unity's main thread. Allocation occurs
before the idempotent asset-load early return. No vendor material is cloned,
no errors are suppressed, and beam geometry/route behavior is unchanged.
`Refresh()` before initialization remains a safe no-op because it has no source
or assets. A new test covers inactive-object initialization before Awake,
repeated initialization, a single reused renderer, applied tint properties,
and absence of unexpected Unity logs. The two original failing tests remain
unchanged. The fixture now contains 15 cases. This correction has NOT been rerun.

Only three files changed for this correction: `TruckTaxiObjectiveBeams.cs`,
`TruckTaxiOccupancyNavigationRegressionTests.cs`, and this handoff.

## Root Causes

- Starter authoring supplied one companion-only secondary anchor. A capacity constant of eight did not create seven compatible passenger places.
- Rival theft rejected every busy companion before consulting compatible seats.
- Placement-only capacity omitted the legacy player visual, permitting eight non-driver actors in synthetic rigs.
- Released child visuals could continue blocking geometric clearance. Exit lifecycle did not prune a placement until the transform detached.
- Companion pickup skipped its destination whenever a fare or GPS target existed. Tick then discarded the destination on fare presence or another foreground target.
- Map presentation highlighted only the top navigation intent and supplied no retained world-beam presentation.

## Main Integration

For the explicitly selected Interstate starter only, before creating driver/passenger visuals:

`TruckTaxiEightOccupantLayout.TryApplyToEmptyStarter(rig, out reason)`

This is an opt-in composition hook, not automatically applied by this patch. Gate on `Player.DefinitionId == TruckTaxiEightOccupantLayout.StarterDefinitionId` (`ih.truck.nwh.euro.semi.starter`) immediately after Spawn and before passenger/driver initialization. Do not apply to another truck. The helper now MERGES existing anchor objects, preserving driver/front/private poses, masks, stable IDs, radii, restrictions and authored sleeper capability, including on disabled rigs. It adds only missing default positions whose authored radii do not overlap existing positions. Already eight valid authored slots returns success without mutation, even with live occupants. Incomplete live, invalid, or unmergeable layouts fail without changes. No sleeper/private-event capability is invented. The six proposed rear positions and clearance with actual shipped models still require rendered cabin-fit validation; radius checks do not prove seats fit inside truck geometry. Existing prefab/scene assets were not modified.

Current integration audit after EditMode1: no production caller of
`TryApplyToEmptyStarter` was found under `Assets/LWS/TruckTaxi/Scripts`.
Therefore the source layout passing its synthetic fixture is NOT evidence that
the live starter has eight seats. Main still owns this composition hook.
The HUD already reads companion destination label and US-formatted distance.
No central debug consumer of `CopyOccupancySnapshot`/`UsedCapacity` was found in
that source scan. Normal-fare identity binding is now connected inside PassengerRuntime:
both primary/pair actors use existing PassengerProfile.passengerId after seating,
and the common Seat path rebinds after returning from a stop. Authored travel-only
rigs use atomic pair validation/placement without requiring sleeper capability.
Companion and stolen-fare identity binding remains connected.

Prefab evidence: `Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab`
lines 12009-12023 in the inspected snapshot have `hasSleeperCapability: 0`, one
SleeperRight anchor at `(0.55, 0.83, -0.95)`, scale `(0.6, 0.6, 0.6)`, Companion-only
mask, radius 0.35, and no private-event eligibility. Merge preserves this anchor
unchanged and skips the overlapping proposed rear position. Thus this retained
slot remains Companion-only; capacity is still role/type-sensitive. Proposed new
seats accept Standard/Companion/Tiny/Robot profiles, not every shipped body type.

Debug HUD APIs:

- `rig.UsedCapacity`: player plus placed, reserved, and legacy seated/boarding actors.
- `rig.AuthoredCapacity`: valid unique mounted authored positions, not an unconditional eight.
- `rig.CopyOccupancySnapshot(List<TruckTaxiOccupantSlotSnapshot>)`: slot ID, role, occupant stable ID, free/reserved/occupied.
- `rig.SetOccupantIdentity(actorTransform, existingStableId)`: already called for normal fares via `TruckTaxiPassengerRuntime.BindFareIdentity`, and by companion/stolen-fare owners. Missing authored IDs remain explicitly unbound, never fabricated.
- `rig.TryReserveSecondary(actor, seatType, kind, stableId)` then existing `TrySeatSecondary`; cancellation calls `Release(actor)`. Companion boarding uses this seam.

GPS/HUD APIs:

- `CompanionDestinationId`, `CompanionDestinationLabel`, `CompanionDistanceMeters`: use existing US-unit formatting.
- `CopyNavigationObjectives(List<TruckTaxiNavigationObjective>)`: read-only foreground and suspended lease projection, including owner, target, position, type, token.
- Existing `NavigationStack` remains the ownership diagnostic.
- `TruckTaxiObjectiveBeams.VisibleCount` and `Diagnostic`: renderer/source availability, not rendered proof.

Beams are already wired inside `TruckTaxiGPSAdapter.ConfigureDemoPresentation()`, immediately after map-marker initialization. Do not add a second Bootstrap initializer. Marker refresh automatically applies point pulse and a direction glyph to the existing Compass mini-map icon; no new route manager or map camera exists. Hazard/closure/work-area types are excluded from point clamping/beam treatment. Hazard footprint renderers are untouched.

## Scope and Validation

Race addendum: GPS LateUpdate reads the existing RaceRuntime HasCheckpointTarget and LOCAL ActiveCheckpointPosition, guarded by checkpoint bounds and enabled state. The dedicated RaceCheckpoint layer sits below Emergency and above ordinary diversions, preserving underlying owners. Checkpoint changes update one stable transient point with a 1-based NextCheckpoint/CheckpointCount label; race end, countdown/no target, invalid target, disabled runtime, or GPS disable releases only its tracked lease and hides the transient marker. The existing objective projection supplies its minimap pulse/arrow and true-world beam. Main needs no additional race GPS/beam initializer. Off-road checkpoints remain visible even when the existing road graph cannot route to them; no circuit route is fabricated. Three added tests cover local coordinates, update/cleanup, Stop Navigation preservation, emergency priority, and disabled-GPS beam cleanup. Runtime enum values are not persisted. The beam pool now accommodates eight intent layers.

The existing one-stolen-fare Session/runtime workflow is unchanged. This patch fixes its empty-compatible-seat rejection and enables multiple secondary placements at the rig level; it does not implement a multiple-stolen-fare queue or multi-companion interaction loop.

No new persistence fields or storage. Accepted fare/navigation/companion restoration remains the existing owners' responsibility. No attached-trailer respawn support or save-gate change. No input rebinding, engine, pause, transmission, or save-load coordinator edits.

New focused fixture: `TruckTaxiOccupancyNavigationRegressionTests` (category `TaxiOccupancyNavigation`). Covers player-plus-seven mixed roles, reservation/commit/cancel, concealed versus exited actors, suspended companion lease projection, stale-token rejection, world-positioned reusable beams, point-only edge policy, and pulse/direction bounds. Existing `TruckTaxiOccupantFoundationTests` capacity case now explicitly includes the driver. Tests added, not run.

Required parent checks: compile; focused occupancy/navigation suites; normal fare and pair boarding; companion with fare/pickup/service/theft push/pop; empty starter theft; seven non-driver models plus player and rejected ninth person; cancel/eject/private-event cleanup; walking driver conceal/return; distant day/night/weather beam visibility and no hazard-area clamping; live save/load driving regression.

## Exact Owned Files

Modified runtime source under `Assets/LWS/TruckTaxi/Scripts/`:

- `TruckTaxiCabOccupantRig.cs`: actual occupancy, player reservation, concrete boarding reservations, role/identity diagnostics, exit cleanup, shared-primary opt-in, travel/private capability separation.
- `TruckTaxiSeatProfile.cs`: authored primary travel seat support without inventing sleeper capability; inactive/exiting legacy visual clearance.
- `TruckTaxiPassengerRuntime.cs`: normal-fare profile identity binding after single/pair seating and reseating; atomic pair path for authored travel-only rigs.
- `TruckTaxiRoadsideCompanionLoop.cs`: destination lease retention, token cleanup, physical boarding reservation and identity binding.
- `TruckTaxiRivalTheft.cs`: removes companion-busy capacity veto; binds stolen-fare identity.
- `TruckTaxiGPSAdapter.cs`: objective projection, companion identity/token API, automatic beam setup and race checkpoint lease synchronization/cleanup.
- `TruckTaxiNavigationIntents.cs`: dedicated nonpersisted RaceCheckpoint layer below Emergency.
- `TruckTaxiMapMarkers.cs`: foreground/suspended point highlighting, objective presentation adapter wiring, transient race marker cleanup.

New runtime source in that same directory:

- `TruckTaxiEightOccupantLayout.cs`: explicit opt-in starter layout, seven non-driver slots plus player; supported seat types are Standard, Companion, Tiny and Robot. Other types require authored compatibility, not a global full flag.
- `TruckTaxiOccupantSlotSnapshot.cs`: immutable slot diagnostics.
- `TruckTaxiNavigationObjective.cs`: immutable lease presentation projection and point-only policy.
- `TruckTaxiObjectiveBeams.cs`: retained vendor-art renderer pool and safe explicit native-resource initialization.
- `TruckTaxiObjectivePointPresentation.cs`: pulse and direction glyph on existing per-compass point icons, not hazard footprints.

Tests under `Assets/LWS/TruckTaxi/Tests/EditMode/`:

- Modified `TruckTaxiOccupantFoundationTests.cs`: eight-person capacity includes driver.
- New `TruckTaxiOccupancyNavigationRegressionTests.cs`: 22 cases including the actual scene-configured fallback in both companion boarding orders, exclusive/shared reservation preservation and an unrelated-fallback rejection; latest additions not run.

New C# metadata exists in the shared tree. The Build2 correction edits only the
occupancy startup hook in Bootstrap. This task did not edit vendor source,
HUD, MainMenu, RaceRuntime, RivalPopulation, Dispatch, persistent
state, scenes, prefabs, or live-driving/load input code. Parent integrations
and preexisting dirty changes in those files remain owned by their authors.

## Remaining Acceptance

- Main reruns the focused fixture and existing OccupantFoundation, CabOccupantRig,
  CompanionLifecycle and NavigationIntent suites, then integrated validation.
- Starter layout is wired immediately after spawn, before passenger initialization; normal fare stable-ID binding is complete in PassengerRuntime.
- Rendered validation must prove actual cabin fit, geometry clearance with shipped
  actors, legible arrows at minimap edges, and long-range beams under day/night/weather.
- Race checkpoint beam follows the true supplied point; an unavailable road-graph
  route is not replaced by a fabricated race course or a straight-line driving route.
- Multiple-stolen-fare and multi-companion gameplay is not implemented by this patch.
  Companion private interaction retains its existing fare/boarding safety guards.
- No persistence fields were added. No claim of newly saved occupant world state,
  attached trailer restoration, or verified Windows save/load driving is made.

## Build2 Missing Slots Correction

Run `0b9efb8267a343d7a7b975992c225437` captured the occupancy screenshot but
did not validate its counts. The scene assigns no truck definition: the spawner
uses `TruckTaxi_InterstateTractor.prefab` and assigns `ih.truck.nwh.euro.semi`.
The previous Bootstrap hook gated on `ih.truck.nwh.euro.semi.starter`, so it
never ran. The prefab really contains only its original SleeperRight anchor.

`TryApplyToConfiguredStarter(LwsPlayerTruck, out string)` now accepts the
explicit starter or the definition-less fallback with the existing starter
vehicle ID AND matching authored Taxi companion anchor/Cab mount. An unrelated
generic-ID truck is rejected without mutation. Bootstrap calls this once after
spawn and logs failure instead of silently skipping. Save/career identity is
unchanged. The existing merge retains original anchors/capability, adds actual
travel anchors, checks radius conflicts and refuses live-occupant resets.

The scene-configured test opens a preview scene, resolves its real spawner's
prefab, copies that prefab's actual occupancy data and Cab transform into a
minimal fixture, and tests the same startup helper with the fallback identity.
It verifies eight real anchors, seven compatible placements plus player,
capacity rejection, preservation and idempotence. It does not execute NWH or
Bootstrap startup and is not proof of rendered physical cabin fit.

Files changed for this correction: `TruckTaxiEightOccupantLayout.cs`, only the
occupancy hook in `TruckTaxiBootstrap.cs`,
`TruckTaxiOccupancyNavigationRegressionTests.cs`, and this handoff.
Test filter: `LWS.TruckTaxi.Tests.TruckTaxiOccupancyNavigationRegressionTests`.
No Unity, build, test or Windows run performed for this correction. Rendered
clearance of the existing rear layout with shipped actor models remains an
explicit acceptance requirement; no eight-person physical-fit claim is made.

## Build3 Mixed-Role Seat Contract

The retained SleeperRight anchor is companion-only, with secondary travel enabled
and private-event eligibility disabled. The actual contract is one driver, six
shared travel seats and one companion-only travel seat, not seven arbitrary fares.
The harness's requirement that every non-driver seat accept both Passenger and
Companion is stronger than this authored mixed-role contract. Parent owns that
harness correction; mount, identity, geometry and actual placement checks remain
necessary and must not be replaced by a capacity-only assertion.

`SelectSecondary` now prefers a compatible, clear companion-exclusive travel
seat over a shared seat for the Companion role only. Previously the front seat's
higher priority could strand the exclusive seat after companion-first boarding.
Priority still breaks ties within each group; existing role/type, reservation,
occupancy and clearance checks precede selection. No authored fields are changed.

The actual-prefab test now covers companion-first and companion-last boarding
with six other mixed-role passengers, exclusive reservation/commit, incremental
counts through 8/8, ninth-person rejection and preservation of all original anchor
fields. Companion-last also checks that the final companion-only free position
does not accept an arbitrary fare. A separate test verifies shared-seat fallback
when the exclusive seat is reserved, and that releasing it cannot redirect an
already reserved shared-seat owner. No Unity/test/build run performed here.

## Vendor Audit

- VENDOR ASSETS AUDITED: Compass Pro POI public API, per-compass state, minimap clamping/rotation, beacon prefab/material and BeaconAnimator; existing NWH-backed cab presentation and actor adapters.
- VENDOR ASSETS USED: Compass point presentation and existing map-arrow sprite registry; Compass beacon mesh/material in an eight-renderer retained pool; existing NWH/LWS cab ownership.
- RELEVANT ASSETS NOT USED: BeaconAnimator is one-shot and destroys its object/material on disable, so it cannot own persistent pooled objective lifetime. No physics replacement, separate map camera, navigation solver, UI framework, or save framework is needed.
- CUSTOM SYSTEMS CREATED: project-specific occupancy snapshots/reservations, explicit starter layout, read-only intent projection, retained beacon and objective-point presentation adapters.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none; semantic lifetime and presentation adapters reuse vendor assets and the existing route authority.
