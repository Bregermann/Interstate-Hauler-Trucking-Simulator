# Nonsexual Companion Population and Lifecycle

Scope: requested sections 5, 6, 28 and participant visibility. No new adult
choreography, content, consumption, session/profile changes, map implementation,
serialized asset changes, Unity launch/build, or commits in this workstream.

## Parent Integration

- `TruckTaxiRoadsideCompanionLoop.RegisterBlock(id, bounds)` creates at least one
  logical slot per authored developed block. Call for actual city-block metadata;
  discovered pedestrian areas are a fallback, not proof of complete block coverage.
- `RegisterService(id, position, exclusionRadius, largeSite)` creates three slots,
  or six for large sites. Radius is the service ring to keep clear. Registration
  is idempotent, updates positions, and does not remove existing logical slots.
- Automatic discovery covers enabled service capabilities, store/restaurant/hotel/
  gas ride locations without their own service component, and authored rival
  depots (six). It refreshes every three seconds across loaded scenes.
- Wire `SetRegionAvailability(regional.IsPositionAvailable)` after initialization.
  The loop never loads scenes. A missing ground surface also prevents spawning.
- `LogicalPopulationCount`, `MaterializedCount`, `MaterializationBudget`,
  `NearbyPopulationDeficit`, and `RejectedPlacementCount` distinguish targets from
  actual safe actors. Do not present logical counts as guaranteed visible actors.
- Default actor budget is 48, clamped to 8-128. Materialization is within 180m,
  with 60m retirement hysteresis, eight attempts per half second, and five-second
  retry backoff for blocked locations. Distant/blocked areas retain logical slots.
- Explicit secondary anchors must set `secondaryOccupantSeat = true`, permit
  `Companion`, specify a real mount, and have sufficient `occupantRadius` clearance.
  Driver/primary fare anchors cannot be secondary. Existing unregistered fare
  actors and the driver are also checked. No synthetic center/lap seat is created.
- `HasSecondarySeat(type, actor)` and `TrySeatSecondary(actor, type, worldScale)`
  work independently of sleeper-event capability. Missing capacity produces
  `NO ROOM FOR ANOTHER PASSENGER` through `Prompt`/`Feedback`.
- Fare-onboard pickup uses the auxiliary phase: no fare replacement, offer
  suppression, navigation claim, or primary seat reassignment. `H` is the horn
  label fallback; the parent owns the authoritative input/catalog binding.

## Lifecycle and Neutral Content

- `Interact()` and the completed sleeper transition both call
  `BeginEventPresentation()`. Profile selection uses `variantInteractionProfiles`
  (optional eight-entry override) then `host.Configuration.customCompanionInteraction`.
  All authored profile fields remain empty; only test-created assets assign content.
- `host.CustomContent.Begin(profile, driverPresentation.Occupant, active.actor.transform)`
  receives visual participant roots, never the truck root. `Beat()` is called at
  intervals of at least one event second. `FadeAlpha` follows the runner.
- `RequestsTruckRocking` drives the existing renderer-only motion. A pre-event
  set of truck renderers excludes occupants; runner proxies/prefabs are excluded
  from the synchronous motion snapshot. No rigidbody/collider state is changed by
  this presentation path.
- Successful `Finish(true)` calls `Complete()` once, enters `CompletingContent`,
  and keeps source occupants seated until `IsPlaying` becomes false. It does not
  immediately cancel the end phase. `Cancel()`, ejection, disable, and destruction
  cancel owned playback and restore presentation/seat/world state.
- Public read-only hooks: `ActiveCompanionVisual`, `PlayerTruckRoot`,
  `OccupantRig`, and `ActiveInteractionProfile`. The parent still owns Escape/HUD.
- No assigned custom profile retains the existing non-graphic presentation;
  the participant remains active and represented by the existing visual proxy.
- `TruckTaxiPassengerActor` exposes explicit lifecycle state, standing dimensions,
  measured foot offset, `PrepareForWorld`, `ShowInWorld`, `PrepareForSeat`, and
  `CompleteSeating`. World placement hides first, releases the rig, detaches,
  restores scale/kinematic state, validates clearance, then enables the fitted
  standing trigger and shows the actor. Ragdoll colliders stay disabled while standing.
- Sketchy presentation validates a truck-clear exit, separate contact/actor points,
  and each short walking segment. Blocked paths use concealed reboarding, not
  movement through the chassis or scenery. Actual body dimensions affect clearance.

## Population Safety

Eight stable variant IDs preserve the existing reaction bank and distribute the
existing clothed Wobble visual combinations. Large sites have six distinct variants.
Weather/night never reduce logical baselines. Placement rejects road surfaces,
known traffic lanes, service rings, sloped/unsupported ground, roofs, obstacles,
truck render/collider bounds, and neighboring companions. Actual renderer-derived
dimensions are checked after visual creation. Replenishment waits for cooldown
and distance/offscreen conditions; no persistent per-companion map markers are added.

World acceptance still requires authored block registration, explicitly certified
secondary seats, regional availability wiring, and an in-Editor population scan.
Blocked geometry or an exhausted actor budget is reported as a deficit, never
filled with an unsafe actor or claimed as a fulfilled visible minimum.

## Validation

- Roslyn syntax checks: all eight scoped runtime/test source files passed.
- Non-emitting semantic audit: 205 current runtime sources with 300 cached Unity
  metadata references, zero errors; both focused fixture sources also zero errors.
  This reads metadata only and produces no assembly/build output.
- Actual policy source executed in memory with minimal value-type stubs: 1,000
  blocks, three standard service slots, six large-site slots, distinct variants,
  100 idempotent refreshes, unique IDs, and capacity preservation passed.
- `git diff --check`: no whitespace errors in scoped tracked changes.
- The parent reported an earlier four-test PlayMode setup failure from automatic
  `Update` and missing GPS. The current fixture provides GPS, disables automatic
  loop updates, and drives real `Interact`/`Tick`/`Finish` manually. Production
  `Ready` now reflects initialization dependencies. No logs are suppressed.
- Fresh parent-owned Unity execution passed all seven existing lifecycle tests:
  `Logs/TruckTaxi-CustomContent-PlayMode.json` reports seven passed, zero failed.
  The eighth Timeline case is new and awaits the final rerun. No Unity tests
  or build were launched by this workstream.

Run category `TaxiCompanionLifecycle`. Concrete PlayMode hooks:

1. `RealCompanionEventPlaysNeutralProfileAndPreservesAsyncCompletion`: transient
   beep, generic wave clip, and cube through actual Interact/Beat/Complete; checks
   source hiding and exact renderer restoration, including an originally disabled part.
2. `CancelRestoresVisibleWorldActorAndStopsNeutralPlayback`: same real entry path,
   cancel, detached actor, world scale, and exact visibility restoration.
3. `NoAssignedProfileRetainsVisibleFallbackParticipant`: empty configuration fallback.
4. `CubeOnlyProfileKeepsParticipantVisibleAndHonorsRendererOnlyRocking`: participant
   visibility, fade consumption, motion cleanup, unchanged truck pose/body/collider.
5. `NearbyServiceMaterializesItsMinimumWithoutAllMapActors`: three nearby service
   actors; distant unavailable large site stays logical; actor spacing/ring clearance.
6. `FareOnboardCanPickUpSecondaryWithoutReplacingFareOrNavigation`: real horn pickup,
   secondary boarding, unchanged fare actor/profile/destination/GPS lease.
7. `MissingSecondaryCapabilityReportsNoRoom`: visible rejection with no boarding.
8. `PassengerTimelineRunsThroughCompanionEventAndCleansItsProxy`: a real transient
   `TimelineAsset`/`AnimationTrack` named Passenger, real director graph/binding and
   evaluation with measured wave rotation, unchanged root physics, asynchronous
   completion, and proxy cleanup.
   Parent adds `Unity.Timeline` to the PlayMode test assembly reference list.

Pickup replaces the source's available slot with a cooldown vacancy independently
of the onboard occupant. After a long ride the area can therefore replenish while
the companion remains aboard. Safely exited actors retire only after distance/
offscreen checks and are counted against the materialization budget until retired.

EditMode tests cover logical minima/idempotence, eight-variant coverage, no actor
creation by registration, service classification, explicit secondary capability,
hidden seat ownership, overlap rejection, wide-body exits, foot pivots, road/roof/
obstacle rejection, and blocked return segments.

## Vendor Report

- VENDOR ASSETS AUDITED: installed `Assets/UTS_FullPack/Scripts/People/Passersby.cs`
  and standing-people APIs; `Assets/NWH/Vehicle Physics 2/Scripts/VehicleController/Input/VehicleInputHandler.cs`;
  `Assets/NWH/Common/Scripts/Camera/CameraChanger.cs`; existing LWS pedestrian graph,
  Scene Streamer regional adapter, actor, seat, and Wobble authorities. Project is
  Unity 6000.4.10f1 / URP 17.4.0; vendor package versions not re-inferred from names.
- VENDOR ASSETS USED: existing NWH `VehicleController`, `VehicleInputStates`,
  `CameraChanger`/`CameraInsideVehicle` integration preserved; existing UTS-derived
  `LwsTrafficLaneDefinition` data is consumed for exclusion. Existing LWS Wobble
  visuals, actor physics, and regional-availability adapter are reused.
- RELEVANT ASSETS NOT USED: no second UTS locomotion manager; standing semantic
  companions and short validated transitions do not require general wandering.
  No new Spline Spawner road dressing, Compass map rendering, Weather Maker/
  Weatherade visuals, or Scene Streamer loading logic is needed for this scope.
- CUSTOM SYSTEMS CREATED: logical companion population policy and small shared
  actor-world-placement helper; extensions to the existing loop/rig/actor/runtime.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
