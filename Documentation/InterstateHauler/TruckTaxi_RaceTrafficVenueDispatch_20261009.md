# Race / Traffic / Venue / Dispatch Integration Handoff

Status: IMPLEMENTED IN SOURCE; NOT COMPILED OR EXECUTED. No Editor, build,
commit, save migration, or Windows acceptance run was performed. All changes
were made through apply_patch in the shared working tree. Unrelated dirty
files were preserved. Baseline requested by Main: 05871832.

## Root Causes And Changes

- Race countdown disabled NWH autoSetInput, reset all input states (including
  steering), then restored a cached input mode/state over any newer owner.
  It now leaves provider selection, autoSetInput, steering, and existing
  pause/load gates alone. Its temporary callback holds propulsion/brakes;
  disposal removes only that callback and releases the existing body lock.
  This addresses the ownership hazard; the reported Windows failure has not
  been reproduced or verified here.
- GO text was immediately overwritten by ordinary timing. GO now persists
  for 1.25 seconds. Race status includes position, checkpoint, progress,
  distance in miles, timing, road segment, checkpoint confirmation and a
  debounced wrong-way warning. Existing wallet rewards/debug no-reward rules,
  refund paths and 20-rival flagship defaults remain intact.
- Traffic only reversed for impatient-or-worse drivers, always straight,
  without a bounded failure budget or route reacquisition. All personalities
  now use the existing UTS drive API for short, clearance-checked reverse
  turns, then reacquire their current lane. Three failed attempts lead to
  existing pool/rival cleanup outside 25 m of the player. Mission actors,
  legitimate signal/pedestrian waits, active road-rage and presentation holds
  are protected. No player physics or vendor traffic implementation changed.
- Speedway authoring gave ordinary fare eligibility to the infield and pit,
  and dispatch had no same-complex exclusion. Eligibility now uses public
  Speedway IDs and optional existing tag metadata: complex:<id>, race-surface,
  infield, restricted-venue. This is enforced before route queries, including
  pickup diagnostics. The shared RideLocation schema is unchanged.
- Intercity selection fairness used completed rides. Displayed opportunities
  now drive a bounded deficit/fairness policy at the configured .25 default,
  with a seven-local-offer starvation limit when a valid intercity candidate
  is found. An unavailable intercity route falls back to a valid local fare;
  demand timing and manual-decline cooldown are unchanged. Completed-fare
  statistics/save fields remain unchanged. Offer fairness is session-local,
  not persisted. Intercity classification still uses the existing regions.
- Final fare arrival explicitly required ActiveStop == null. The existing
  RideBehavior partial now resolves skipped optional stops at valid final
  arrival, before that guard, and recognizes progress toward either the
  accepted diversion or final destination for patience. No skipped-stop
  reward is granted. No edits were made to Session.cs or RivalTheft.
- The old ground, parking, apron and at-grade central road would occlude an
  underpass. The authoring upgrade splits only those surfaces around a
  14 m two-way corridor, with 6 m headroom, floor at -6 m, structural roof and
  side walls, ramps below 8%, and two-way road graph links. Bowl rails and
  grandstands remain. Pit/infield authored endpoints move to provide ramp
  runout; the old direct pit/infield graph edge is retired.

## Integration Hooks

Main's TruckTaxiHud.cs (NOT EDITED):

- Existing RaceRuntime.Initialize(host) remains the initializer; it builds
  its own status panel using the existing HUD/Heat presentation helpers.
- Pause menu commands: OpenRegistration(), ResetToTrack(), LeaveRace().
  RegistrationAvailable reports nearby eligible registration. The old
  persistent registration and in-race reset/leave buttons are hidden/removed.
- PresentationRoot exposes the race-owned RectTransform for layout. It hides
  when paused or NormalHudSuppressed. PresentationText, CountdownText,
  WrongWay and Progress01 expose read-only content.
- Race FocusRoot/MenuRoot and CloseRegistration() remain the modal hooks.
- Central debug can retain existing DebugStartFlagship, DebugRestartFlagship,
  DebugResetRace, DebugAdvanceCheckpoint, DebugBackCheckpoint,
  DebugSetRivalCount, DebugForceFinish and RivalCheckpointDiagnostics.

GPS/beam owner (NOT EDITED):

- While HasCheckpointTarget is true, ActiveCheckpointPosition is a LOCAL
  world position. Use NextCheckpoint/CheckpointCount for the label. Release
  the race navigation intent/beam when false; never replace underlying fare
  intent ownership. This hookup is still required for a 3D beam/minimap route.
- PresentationCue emits countdown numbers, GO, checkpoint confirmations,
  WRONG WAY and finish text. Subscribe/unsubscribe with the presentation
  owner's lifetime. Optional countdownClip, goClip, checkpointClip,
  wrongWayClip and finishClip fields play through a race-owned AudioSource
  routed to the existing World mixer category. Clips were NOT authored or
  assigned; no voice was generated.

Venue content owner:

- Run TruckTaxiSpeedwayAuthoring.UpgradeTwoWayAccess (menu: Truck Taxi /
  Regional / Upgrade Thunder Bowl Two-Way Access) only after saving scenes
  and accounting for shared content edits. This opens/saves the persistent
  and Speedway scenes and bakes project-owned EasyRoads meshes.
- The command has NOT been run. Existing serialized scene/mesh assets still
  contain the old geometry. The original central road was verified as
  ThunderBowl_Access_2.asset, not the misleading Access_0 numbering.
- Inspect both portals, tractor/trailer swept clearances, banking/roof gap,
  ramp joins and two-way exit in Editor and Windows. Update map/navigation
  bakes as needed. No collider or Windows drive-through proof is claimed.

## Exact Files Changed

Under Assets/LWS/TruckTaxi/Scripts/:

- TruckTaxiRaceRules.cs
- TruckTaxiRaceRuntime.cs
- TruckTaxiRivalPopulation.cs
- TruckTaxiTrafficAdapter.cs
- TruckTaxiTrafficBehaviour.cs
- TruckTaxiSpeedwayLayout.cs
- TruckTaxiSession.Dispatch.cs
- TruckTaxiSession.Offers.cs
- TruckTaxiSession.RideBehavior.cs

Under Assets/LWS/TruckTaxi/Editor/:

- TruckTaxiSpeedwayAuthoring.cs

Under Assets/LWS/TruckTaxi/Tests/EditMode/:

- TruckTaxiRaceSafetyTests.cs: provider-state preservation, callback cleanup,
  body restoration and directional wrong-way thresholds.
- TruckTaxiSpeedwayTests.cs: dimensional ramp/headroom constraints and
  bidirectional tunnel graph without the old at-grade shortcut.
- TruckTaxiTrafficAddendumTests.cs: three-attempt budget, pool reset and
  unregistered-actor/missing-route rejection.
- TruckTaxiDispatchMegaTests.cs: same-complex/infield exclusion, 10,000 offer
  fairness draws, invalid-route local fallback and unchanged schedule delay.
- TruckTaxiDispatchCoverageTests.cs: 1,000 graph-gated offers at .25 target;
  replaces obsolete assertions of exact completed-fare spacing.
- TruckTaxiPolishRideBehaviorTests.cs: ignored pending/accepted stop normal
  arrival, no diversion reward, and patience while taking final-fare route.

This handoff: Documentation/InterstateHauler/TruckTaxi_RaceTrafficVenueDispatch_20261009.md.

## Validation And Remaining Work

- Scoped git diff --check: passed (line-ending conversion warnings only).
- Focused test source added/updated, not run. Main owns compilation and
  combined validation. Suggested fixture filter: the six fixtures above,
  plus existing TruckTaxiFlagshipRaceTests / TruckTaxiRaceTests / traffic pool
  and rival cleanup fixtures for integration regressions.
- Save/load authority and serialized state schema unchanged; live-driving
  save/load at 05871832 still requires Main's regression check.
- No shared HUD, Bootstrap, MainMenu, central debug, input/turning, companion,
  cab occupant, GPS or RivalTheft source was edited.
- Actual countdown/input transitions, reward payment, 20 live rivals,
  aggressive interactions, recovery under collision, route/beam hookup,
  tunnel regeneration and visual layout need runtime/Windows verification.

## VENDOR ASSETS AUDITED

- NWH Vehicle Physics 2: Assets/NWH/Vehicle Physics 2/Scripts/VehicleController/Input/VehicleInputHandler.cs;
  autoSetInput and inputModifyCallback polling lifecycle.
- UTS Full Pack: Assets/UTS_FullPack/Scripts/Car/CarMove.cs,
  CarAIController.cs and Scripts/Paths/MovePath.cs; existing LwsUtsTrafficApi,
  TruckTaxiTrafficPooledActor and UTS Truck_1/Truck_2 prefab integration.
- EasyRoads: existing TruckTaxiDemoBuilder.BakeRegionalRoads adapter and
  generated Speedway mesh bounds; ERRoadNetwork.CreateRoad/BuildRoadNetwork,
  SetMeshCollider, SetTerrainDeformation, SnapToTerrain and road.Destroy.
- Heat: existing TruckTaxiHud.Button integration resolves
  Michsky.UI.Heat.ButtonManager; existing Panel/Text helpers retained.
  Exact installed vendor versions were not re-established in this pass.

## VENDOR ASSETS USED

NWH input modifier callback; UTS CarMove.Move, MovePath.InitStartPosition,
public cruise/following controls and existing LWS pooling; EasyRoads through
the existing authoring adapter; Heat/HUD helpers for race UI.

## RELEVANT ASSETS NOT USED

Compass/GPS beam integration remains with Peirce; no parallel navigation
presentation was created. DOTween was unnecessary for timed text updates.
Pixel Crushers persistence was left unchanged. Vendor systems do not own
Truck Taxi offer fairness, race rewards or venue public-stop eligibility.

## CUSTOM SYSTEMS CREATED

No new managers/frameworks. Small offer-fairness and public-stop policy
helpers in the existing Dispatch partial; scoped race presentation hooks,
bounded UTS recovery coordination and Speedway authoring geometry updates.

## VENDOR SOURCE MODIFIED

NO. Pre-existing unrelated vendor dirt remains untouched.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
