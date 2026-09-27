# Truck Taxi: Regional World and Population Pass

## Scope and Ownership

The existing Truck Taxi session, passenger continuity, ride objectives, NWH truck,
Compass presentation, needs, services, and authored dialogue remain in place.
No voices were generated. No vendor source was edited by this pass.

| Responsibility | Authority |
| --- | --- |
| Full vehicle physics and nearby traffic AI | NWH player; UTS traffic/buses |
| Logical population and presentation budgets | Existing Taxi adapters plus small logical records/pools |
| Road geometry / route computation | EasyRoads baked meshes / existing LWS road graph |
| Additive scene operations | Installed Pixel Crushers Scene Streamer |
| Load selection and hysteresis | Existing ILwsWorldStreamingService and manifest/policy |
| Minimap/full map and route rendering | Existing Compass instance/camera, with Taxi controls |
| River / atmosphere / road accumulation | Weather Maker / existing Weather Maker / Weatherade |
| Rail path evaluation | Installed Unity Splines 2.9.0 |
| HUD and controls | Existing Heat/uGUI Taxi factories and Input System |

`TruckTaxiRegionalWorld` binds the Taxi player and metadata to the existing streaming
service. It is not a second streaming scheduler. The persistent DemoCity holds the
truck/session, weather/time, UI, routing, stop/service metadata, population records,
transit controllers, and active passenger/objective state.

## Authored Regions

`Assets/LWS/TruckTaxi/Regional/Scenes/` contains:

- `Taxi_Town01`: original town scenery and colliders.
- `Taxi_Town01Outskirts`: western highway approach/interchange.
- `Taxi_Highway`: multi-lane connector.
- `Taxi_RiverBridge`: water, banks, complete 440m elevated highway crossing and guardrails.
- `Taxi_ServiceArea`: accessible service apron/building.
- `Taxi_Town02Outskirts`: eastern interchange/approach.
- `Taxi_Town02`: offset second-town validation scenery, authored stops and services.
- `Taxi_RailCorridor`: station/track/rail-bridge scenery.

Town 02 is deliberately prototype art, derived from the existing town, not final
production geography. Its roads, traffic/pedestrian regions, 20 unique ride stops,
gas/repair, stores and restrooms use existing runtime systems. There are 56 persistent
ride locations including transit stops. Eight scenes are added to Build Settings;
the builder explicitly includes them in the Windows build.

Open DemoCity plus the desired chunk additively to edit scenery. Keep semantic
metadata in DemoCity. `Truck Taxi/Regional/Convert Existing City To Streamed Region`
refuses to regenerate a scene already converted. Transit authoring is likewise
idempotent. EasyRoads construction roads are removed after project-owned meshes
are baked; the regional scene dependencies do not point at temporary vendor meshes.

`Regional/Taxi_StreamingManifest.asset` and `Taxi_StreamingPolicy.asset` are normal
LWS assets. Default preload is 450m plus 14 seconds of velocity (extra capped at
1200m). Unload starts at 850m, raised above the current preload; hysteresis is 200m,
two concurrent loads, evaluation every 0.5 seconds. Tow prepares destination ground
before moving the player and protects their old region until the move completes.
Population materialization is forbidden in unloaded regions. The bridge is one
chunk so its deck is not independently unloaded halfway across.

### Installed Scene Streamer Compatibility

Installed 1.26.3's public `Load(string)` passes its current scene name instead of
the argument to the internal loader. `SetCurrentScene` also starts its own neighbor
unload policy, conflicting with the existing LWS policy. The adapter therefore
uses a cached, narrowly checked `Load(string, InternalLoadedHandler, int)` overload
on the vendor singleton, with null callback and depth zero. The vendor still owns
its coroutine, scene operation and registry. Unload uses public `UnloadScene`.
This private-API compatibility dependency is explicit, not a copied loader.

`World/Streaming/link.xml` preserves the exact reflected signatures. The package
assembly is preferred over the simultaneously imported legacy Assets copy. Upgrade
Scene Streamer deliberately: rerun the signature test and additive load/unload check,
then remove this compatibility bridge when the public explicit-load API is fixed.

The new river instance sets Weather Maker `ReflectionMask = 0` and disables the
optional planar reflection component. Its installed reflection renderer otherwise
repeatedly submits destination-less URP RenderRequests. Water remains vendor-rendered;
global weather and vendor prefabs/materials are not changed.

## Population Budgets

- Logical cars: **150**. Default full budget 40, additional reduced budget 40,
  175m full / 350m visible bubbles, hysteresis, forward speed preload and camera priority.
- Logical pedestrians: **720** across the two town regions. Default full budget 100,
  total visible budget 180; 85m full / 170m visible radii.
- Distant entries are records, not hundreds of running controllers. Inactive whole
  actors are pooled. Reduced traffic disables AI/wheels/colliders and uses logical
  movement; full promotion rebinds UTS path progress before enabling AI.
- Reuse clears transient rage, brakes, damage/mission identity and lane reservations.
  Pedestrian ragdolls settle/clean up and return to reusable walking actors; root
  walking colliders are cached independently of inactive attachedRigidbody lookup.
- Budgets are on `TruckTaxiTrafficAdapter` / `TruckTaxiPedestrianPopulation`.
  `ScriptableObjects/Population/TruckTaxiCityPopulation.asset` owns logical targets.
- Debug's regional page shows current/loaded regions, pending loads, speed, logical,
  live, full, pooled counts, churn, FPS/main/physics. Its Rigidbody count includes
  kinematic ragdoll bones and must not be mistaken for the full-physics actor count.

## UI, Effects and Map

Debug/full-map overlays suppress conflicting normal HUD bands and controls without
overwriting saved GPS visibility. The top needs strip is the only always-on
Bladder/Hunger/Thirst presentation. Bottom bars retain Fuel/Damage/Patience/effects.

Full map: **F3**, **gamepad left trigger + right-stick click**, or the minimap expand
icon. Same Compass camera/route, pause while open, mouse drag/wheel, WASD/arrows or
right-stick pan, PageUp/PageDown or shoulders zoom, Home/Center on Player. Closing
restores normal map/pause state. POI service navigation refuses to replace an active
ride target. Persistent regional POIs are available while scenery is unloaded;
opening the map does not request world loads. Unloaded scenery is not fabricated
as a second map renderer. Compass icon events must remain enabled on the HUD
instance so its CanvasGroup does not disable the expand button.

Mushrooms use an owned URP Volume, priority 200 on layer 31, bound to the actual NWH
camera list. Original camera post-processing/masks are restored afterward. Vivid
color, bloom, aberration, gentle animated hue/lens/vignette are weighted by the
existing temporary-effect envelope. Debug exposes use/peak/clear controls.

The existing adult companion stays clothed and uses the existing interaction loop.
Debug can spawn nearby, fill thirst and select a nearby private stop. Travel still
requires real driving. Hydraulic presentation uses renderer proxies only: default
bounce .65m, roll 29deg, pitch 19deg, final bounce 1.1m; NWH colliders/joints are not
rocked. Thirst resets on completion and then naturally accumulates with game time.
Use the normal parking brake (P) when parking for the event. Moving above the
existing 1 MPH safety threshold cancels it; the presentation does not freeze NWH.

## Rides and Transit

Configuration defaults: preferred routed pickup ETA 20-90 seconds, hard maximum
180 seconds, intercity chance 20%, maximum intercity trip 20km, premium 1.2x.
Eligibility uses the existing road-distance provider; no eligible nearby pickup
means retry later. Repeat passengers remain at their real recorded location.
Far destinations do not authorize far pickups. Existing fare rules still apply.

The regional road graph is persistent and routes through unloaded scenery.
Validation route from the original spawn to Town 02 measured about 4.60km.

Rail: one Unity spline route, locomotive plus three placeholder cars, 22m/s,
12-second station dwell, two stations, two crossings with warning/gates and cached
UTS vehicle/pedestrian stop coordination. Logical train progress persists while
its nearby presentation is pooled. The train is not stopped by a car collision.

Buses: existing UTS Big_Bus, one local route per town, four curb stops per route,
five-second dwell, a maximum two-actor pool, region/distance gates. Signals/traffic
remain UTS-owned. No regional bus, smaller local road bridge, or final transit art
was added; those were optional. Pedestrian boarding simulation is not added.

## Validation and Limits

- Unity 6000.4.10f1 compilation and Windows development build passed, zero errors.
- Focused `TaxiRegional` EditMode category: 25 passing tests.
- Focused PlayMode pedestrian ragdoll/pool reuse test: 1 passed after fixing collider restoration.
- Real Play Mode loaded only Town01/outskirts/rail at startup, spawned the canonical
  truck on ground, and showed a live bus plus moving logical train.
- Windows 15-second stationary-town sample: **80.9 FPS, 12.35ms main, 0.45ms
  Physics.Simulate** versus reported prior 3.9 FPS / 253.79ms / 70.44ms.
  This is not a whole-route percentile benchmark or a guaranteed minimum FPS.
- Sample: cars 150 logical / 20 live (peak 21) / 5 full / 1 pooled;
  pedestrians 720 logical / 180 live / 65 full (peak 71); 2209 active Rigidbody
  components including kinematic bones. Pooling does more than hide renderers.
- Windows F3 and expand-button dispatch passed; unloaded-region POIs exist without
  loading the corresponding chunk. Cab/external camera mushroom pixel checks passed.
- Continuous real NWH keyboard drive to streamed Town 02 passed (3895m displacement,
  no teleport or kinematic truck). This was NOT an intercity passenger fare.
- Companion completion passed in the Windows player: debug setup placed a companion
  nearby/filled thirst, then normal Enter input boarded her, a real 126m NWH drive
  reached the private stop, P parked, and Enter began the event. No teleport,
  kinematic travel or direct completion call was used. Measured presentation reached
  1.46m bounce / 40 degrees rotation; completion feedback, companion exit, cleared
  service GPS and restored control were observed. Minimum sampled thirst was
  0.000018, followed by normal 30x-clock accumulation. The initial unparked run
  cancelled; the earlier delayed exact-zero assertion also ignored accumulation.
- Inspected final debug render captures at actual 1920x1080, 2560x1440 and
  3440x1440: normal HUD suppressed, no overlapping needs/GPS/status controls.
  One normal-HUD label per need was checked at each resolution. These are camera
  render captures, not a claim of hands-on mouse/gamepad ergonomics validation.
  The capture helper temporarily composites overlay canvases through the camera;
  its mushroom captures also distort UI, unlike the normal ScreenSpaceOverlay HUD.

Local evidence (ignored build/log artifacts): `Logs/TaxiRegionalFinalPlayer.log`
records successful continuous Town 01-to-02 physical traversal and the old thirst
assertion failure; `Logs/TaxiRegionalFocusedPlayer.log` records final-resolution
HUD/map/mushroom/performance observations; `Logs/TaxiCompanionCompletionPlayer.log`
records the passing parked companion recheck. `Logs/TaxiRegionalEditorCompletion.log`
records the final zero-error Windows build. Render captures are under
`Builds/TruckTaxiDemo/Validation/Windows_Regional_*.png`. Failed attempts are retained,
not relabelled as passing full-suite runs.

Remaining hands-on review: complete one actual intercity passenger ride; traffic
believability, crossing behavior with real approaching vehicles/pedestrians, bus
curb/signal behavior, final art/river appearance, and map/UI ergonomics. Do not label
these unexecuted manual checks PASS. Regional smoke is opt-in (`-truck-taxi-regional-smoke`);
`-skip-regional-drive` avoids repeating already observed traversal, and
`-companion-only` narrows a completion recheck. Normal launches do not create probes.

## Vendor-First Record

VENDOR ASSETS AUDITED: UTS, NWH, Compass Navigator Pro, Scene Streamer, EasyRoads,
Weather Maker, Weatherade integration, Heat, Unity Splines, existing Input System.

VENDOR ASSETS USED: those existing authorities; project-owned region assets and
compatibility/presentation adapters connect them.

RELEVANT ASSETS NOT USED: no new traffic, GPS, water, train, spreadsheet or voice package;
no voice generation, native plugin, or new save backend.

CUSTOM SYSTEMS CREATED: lightweight logical population/pool helpers, regional
metadata/binding and authoring, Compass full-map controls, rail schedule/crossing
coordination, UTS bus dwell/pooling, focused tests/probe. None replaces vendor physics,
navigation, world streaming operations, UI framework or persistence.

VENDOR SOURCE MODIFIED: NO (pre-existing dirty vendor work is excluded from these commits).

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
