# Truck Taxi Integrated Gameplay / UX Pass

## Scope and authorities

This is an additive Truck Taxi pass, not an Interstate controller, navigation,
persistence, weather, or traffic replacement. NWH still drives the player truck;
UTS still moves ambient vehicles/pedestrians on its paths; Compass presents the
existing LWS navigation intent; Weather Maker and Weatherade retain their visual
roles. The existing Heat/TMP/uGUI menus, Pixel Crushers dialogue bridge, and
project audio routing are reused. No vendor source was edited for this pass.
No voice references or audio generation jobs were run.

## Controls and camera

- Pause > Controls is scrollable with pointer, keyboard, and gamepad navigation.
  Hold F1 for the non-pausing compact overlay. Both use TruckTaxiControlsCatalog.
- Taxi InputAction bindings are queried live. Existing Interstate vehicle input
  reads devices directly rather than an InputAction asset; those rows are marked
  FIXED DIRECT READ, not falsely advertised as rebindable. Wheel rows read the
  current calibration profile. Existing conflicts and missing bindings are shown.
- F2 changes the same persisted Ride Requests setting used by the menus. It does
  not cancel the current fare or service route. F1/F2 have no generic gamepad
  default because the existing mapping occupies its buttons; they show UNBOUND.
- Cab look reuses NWH CameraChanger/CameraMouseDrag. Hold right mouse and move,
  or use right stick. Backquote centers the view. Wheel LookReset is honored.
  Cab settings expose mouse/stick sensitivity, invert Y, and smoothing.
- Limits: yaw -120/+120 degrees, pitch -45/+60. Vendor camera permissions restore
  on camera change, disable, and destruction. No separate camera is created.

## Vehicle and weather tuning

TruckTaxiVehicleHandlingOverride applies an NWH engine power modifier only while
Taxi owns the vehicle: 3.4x at rest, 2.2x at 15 MPH, smoothly 1x at 20 MPH.
TruckTaxiSnowTraction keeps engine power and instead interpolates deep-snow grip
to longitudinal 0.56 / lateral 0.36. Deep-snow rolling resistance is 1.18x;
plowing reduces local depth and restores grip. Existing Weatherade coverage is
unchanged. These are defaults for driving review, not measured handling claims.

## Ambient population and traffic

- Saved Free Play profile: 720 pedestrians / 300 traffic, 16 spawns per maintenance
  pass. Counts were not lowered to obtain a benchmark.
- UTS pedestrians receive bounded, reachable sidewalk routes. A registered vendor
  crosswalk joins opposite sidewalk components; entries wait for the pedestrian
  signal, finish an occupied crossing, then return to sidewalk routing.
- Curb connectors allow half a sample cell of inward offset, but still reject
  road shortcuts, unsupported ground, and solid obstacles. Duplicate aliases of
  the same crosswalk do not generate duplicate journeys.
- Route policy is staggered at near/mid/far intervals. Shadows, skinning and
  authored LOD transitions retain the existing presentation optimization policy.
  Ambient Wobble geometry shares one three-material mesh per actor instead of
  eleven separate primitive renderers. UTS skeleton/ragdoll authority remains.
- Taxi's four authored boulevard loops expose eight parallel legal lanes, with
  same-direction adjacency. Initial assignment distributes cars across them.
- Stable lifetime driver personalities: Cautious 20%, Normal 45%, Impatient 19%,
  Aggressive 12%, Reckless 4%, with individual desired speeds and headway.
- Nonalloc swept obstacle checks cover player, other vehicles, pedestrians, and
  walls. Braking accounts for relative speed and stopping distance. Signal/occupied
  crossing stop lines precede the crossing; perception is staggered by distance.
- Passing/avoidance requests must pass same-direction, straight-road,
  intersection-clearance, front/rear-gap and reservation checks before UTS changes
  path. No transform teleport is used. Horns use the installed NWH horn clip and
  World mixer, with personality cooldowns. Existing collision road rage applies
  to AI/player and AI/AI contacts, bounded to two active incidents.
- Some impatient blocked drivers can briefly reverse after checking rear space.
  The adapter releases only its own UTS low-speed brake hold, avoiding sticky
  wheel brake torque after an obstacle clears.
- LIMIT: the authored network still lacks connected directed intersection-turn
  and merge connectors. This pass does not claim general merging or rerouting
  pursuit. Lose Vehicle remains disabled; faster loop-following is not pursuit.

## Goals, effects, projectiles, and roadside companions

- Cancelled unfinished goals are failures, with a reason in history, never success.
  The passenger destination ring follows the actual session destination rather
  than temporary service GPS intent.
- Goal bars fill on success, rate-limit confirmation sounds, hold 1.2 seconds,
  fade over 0.7 seconds, and collapse their layout. History is not deleted.
  Needs and timed effects have compact bars.
- TruckTaxiTemporaryEffects refreshes timers, never multiplies repeated stacks.
  Mushroom: 18 seconds, 2-second fade in / 4 out; isolated URP profile with vivid
  color, bloom and restrained chromatic aberration. High Octane: 12 seconds, 1.7x
  NWH power; Energy Drink: 14 seconds, 1.15x and slower hunger gain. Exact baseline
  power/profile/camera settings restore when effects end. Water is not libido relief.
- Filled-container projectiles inherit vehicle motion, last at most 20 seconds,
  and detonate on impact or a 0.65-second bounce fuse. Six-metre bounded falloff,
  non-gory ragdoll/prop/car responses, and per-actor hit deduplication reuse the
  existing impact and exact-assigned-mission-target accounting.
- THIRST means libido, not hydration. Eight clothed, explicitly consenting 21+
  roadside companion variants use valid authored off-road stops. Pickup is not a
  fare. It leases offer suppression without changing the player's preference,
  routes to a nearby private stop, and restores the lease/GPS on completion/cancel.
- The seven-second implied event hides no physics: temporary renderer-only proxies
  rock the stationary truck (0.24 m bounce, 14-degree roll, 9-degree pitch, 0.45 m
  final bounce). NWH, joints, colliders and Rigidbody transforms are not animated.
  Completion satisfies Thirst, restores renderers and lets the companion exit.
  Moving/recovery/another fare cancels safely. No explicit act is depicted.
- Scene changes are limited to one root containing six nearby private stops and
  six companion pickup points. Existing authored stops were not moved.

## Authored roster coverage

- 114/114 passenger prefabs have intentional RosterVisual-v1 Wobble designs.
  Editor tools preserve non-factory authored models instead of replacing them.
- 2,736 new character-specific text lines were imported additively: three each
  for greeting, chatter, arrival and scenic view; one each for twelve additional
  boarding/cancellation/repeat/stop/failure/ejection/jug/weather events.
- Source: Tools/TruckTaxiPassengerFactory/Content/IntegratedDialogue*.tsv.
  Apply/validate through Tools > Truck Taxi > Dialogue. Existing IDs with changed
  content are rejected for manual resolution; assigned clips are preserved.
- INCOMPLETE: the full requested dialogue category matrix is not finished.
  The roster validator reports 1,082 missing passenger/category pairs and all
  114 passengers remain thin in over half its common categories. It finds two
  pre-existing normalized long-line duplicates, and zero identical scenic lines.
  Remaining categories include driving/traffic/pedestrian reactions, most jug
  states, request outcomes, tow/fuel/item hooks, and additional medium-event variants.
  These must receive actual authored copy, not generated adjective substitutions.

## Validation and known limits

- Clean Unity compilation; focused TaxiIntegrated EditMode suite: 38/38 passed.
  No repeated run of the entire previous suite.
- Live Editor integration: 720 pedestrians, 300 cars, eight companions, complete
  crosswalk routes and observed legal crossing occupancy. Vehicles resumed after
  stopped-brake release. Counts do not prove collision avoidance quality.
- Companion lifecycle was exercised in a controlled kinematic-position fixture:
  pickup, nearby target, park, timed event, satisfied Thirst and cleanup. This is
  not a manual driving or suspension test. Renderer-only invariants also have tests.
- Controls page was rendered and inspected in Play Mode. Human gamepad/wheel,
  free-look, launch/snow feel, dense traffic safety, full private-stop approach and
  projectile impact feel still require manual playtesting.
- Editor dense sample: roughly 350 ms/frame, 335 ms main thread, 93 ms physics
  simulation, 15 ms BehaviourUpdate. The Editor measurement is not a standalone
  performance claim. Native UTS vehicle/pedestrian simulation remains active;
  staggered policy/presentation LOD does not eliminate that cost.
- Single Windows Development build: succeeded, zero errors, 468,018,372 bytes.
  Output: Builds/TruckTaxiDemo/TruckTaxi.exe.
- Single focused standalone pass: 24 checks passed, exit code 0. F2 preference,
  F1 hold/release, page focus, populated counts, renderer-only motion/restoration,
  effect fade/expiry and live NWH boost baseline restoration passed. The boost
  measured 3.4x -> 5.78x -> 3.4x at rest. No manual driving claim is implied.
- Standalone density measurement (15 seconds, 58 frames): mean 258.974 ms,
  median 257.664 ms, p95 300.047 ms; main-thread mean 253.790 ms; recorded
  Physics.Simulate mean 70.441 ms. Hardware: i9-9900K / GTX 1660 Ti. This is about
  3.9 FPS and is NOT performance-ready. Full native vehicle/wheel simulation is
  a measured contributor, not the entire 254 ms main-thread cost; a deeper
  native simulation/render profile is still required before claiming a remedy.
- Standalone traffic: 300 behavior instances; 247 exceeded 1 m/s during the
  sample, 167 moving at the final observation, mean speed 3.375 m/s; two lane
  changes, eight lanes occupied, all five personalities. Horn playback was
  recorded, not listened to. This does not prove safe passing/braking in every case.
- Legal crossing occupancy was observed in the Editor, but was zero at the
  single standalone traffic snapshot. Do not describe that snapshot as a failed
  or successful full crossing cycle, or claim a vehicle yield was manually seen.
- Captured and inspected full/quick controls and mushroom peak images at 1080p.
  The quick panel is readable but still oversized for a compact overlay. Mushroom
  timing/profile activation passed; its desired extreme visual strength still
  needs a normal Game view review. Gamepad ergonomics and physical stop clearance
  remain manual checks. The project is not presented as completing every item.
- Local evidence: Builds/TruckTaxiDemo/IntegratedPlayer.log and
  Builds/TruckTaxiDemo/Validation/Windows_Integrated_*.png. They are intentionally
  not committed as a large evidence dump. Voice smoke fixtures remain untouched.

## Vendor-first report

- VENDOR ASSETS AUDITED: NWH, UTS, Compass, Weather Maker, Weatherade, Heat,
  Pixel Crushers; existing EasyRoads/LWS authored road and navigation contracts.
- VENDOR ASSETS USED: those existing runtime integrations, Unity Input System,
  TextMeshPro/uGUI and URP; NWH horn audio and existing mixer routing.
- RELEVANT ASSETS NOT USED: no new Scene Streamer, alternative AI/navigation,
  replacement UI, or external voice-generation pass.
- CUSTOM SYSTEMS CREATED: Taxi control catalogue/presenters and cab-look adapter;
  goal/status presenters; timed-effect policy/presentation; UTS behavior/lane
  policy; companion/render-only motion; additive roster authoring and focused probe.
- VENDOR SOURCE MODIFIED: NO (pre-existing vendor working-tree edits excluded).
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO; project code supplies Taxi policy.
