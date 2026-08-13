# Prompt 011 - GPS Routing and Voice Guidance

## Summary

Prompt 011 adds the first functional GPS/navigation baseline for Interstate: Hauler. LWS is the route authority. Compass Navigator Pro remains presentation middleware only.

The primary GPS presentation is physical in the starter truck cab through:

`Assets/LWS/InterstateHauler/Navigation/LwsCabGpsController.cs`

The selected NWH cab exposes `Cab` and `DashInstruments`, but no safe, named GPS display mesh/material was found in the prefab hierarchy. Prompt 011 therefore creates an LWS-owned world-space physical GPS screen child named `IH Physical Cab GPS Screen` under `Cab` at runtime.

## Routing Architecture

Runtime flow:

`LwsRoadGraph` -> `LwsRoutePlanner` -> `LwsNavigationService` -> `LwsCabGpsController` -> physical cab GPS display

Important files:

- `Assets/LWS/InterstateHauler/Roads/LwsRoadGraph.cs`
- `Assets/LWS/InterstateHauler/Navigation/LwsNavigationTypes.cs`
- `Assets/LWS/InterstateHauler/Navigation/LwsRoutePlanner.cs`
- `Assets/LWS/InterstateHauler/Navigation/LwsNavigation.cs`
- `Assets/LWS/InterstateHauler/Navigation/LwsCabGpsController.cs`
- `Assets/LWS/InterstateHauler/Navigation/LwsGpsMapGraphic.cs`
- `Assets/LWS/InterstateHauler/Navigation/LwsNavigationDebugPanel.cs`

`ILwsNavigationService` exposes `RequestRoute`, `SetDestination`, `RecalculateRoute`, `ClearRoute`, `UpdateVehiclePose`, and presenter binding. Route state is exposed through `LwsNavigationRuntimeState`.

## Graph Authority

The route planner consumes only the LWS road graph:

- nodes
- directed edges
- centerline samples
- road class
- direction
- speed limit
- distance/travel cost

It does not use UTS paths, EasyRoads concrete objects, Compass route solving, or NWH vehicle physics as route authority.

## Route Planner

`LwsRoutePlanner` uses a Dijkstra-style graph search over outgoing route segments. Current cost is distance/travel cost. The planner supports explicit node endpoints and nearest-node resolution from world positions. A connector tolerance supports the current validation corridor where ramp/crossover samples meet nearby mainline samples without turning UTS or EasyRoads paths into route authority.

Future cost modifiers can add travel time, road class, tolls, truck restrictions, weather, closures, traffic, and player preferences.

## Route Progress

`LwsNavigationService.UpdateVehiclePose` map-matches the truck against active route edges using `LwsRoadGraphQuery.TryFindNearestRoad`. The match favors route context and vehicle heading so divided-road carriageways are less likely to jump.

Runtime state includes:

- route active/status
- route ID/destination ID
- current road/edge
- next road
- current step/total steps
- next maneuver
- next instruction text
- distance to next maneuver
- remaining route distance
- ETA
- off-route/recalculating/arrival flags

## Maneuver Generation

`LwsNavigationManeuverType` supports 34 maneuver/event values, including straight/slight/turn/sharp, keep/merge/ramp/exit/fork, U-turn, roundabout exits, arrival, off-route, recalculating, and recalculated.

Classification lives in `LwsRoutePlanner.ClassifyManeuver`. Angle bands come from `LwsNavigationTuning`; ramp semantics prefer `LwsRoadClass.Ramp` before pure angle.

## Display Names

Road IDs remain stable machine identifiers. Player-facing GPS text uses `LwsRoadDisplayNames`.

Current validation names:

- `IH_TEST_I000_NB` -> Interstate Test Northbound
- `IH_TEST_I000_SB` -> Interstate Test Southbound
- `IH_TEST_I000_RAMP` -> Entry Ramp
- `IH_TEST_I000_TURN` -> Turnaround Crossover

## Off-Route and Rerouting

The first off-route implementation uses:

- off-route tolerance: 28 meters
- reroute cooldown: 4 seconds
- arrival distance: 35 meters

When the player leaves the active route, state becomes `OffRoute`, voice can announce the off-route/recalculating events, and `RecalculateRoute` requests a new route from the current world position to the prior destination.

## Physical GPS Rendering

`LwsCabGpsController` creates a world-space Canvas under `Cab`.

The screen shows:

- map panel
- route polyline
- player marker
- next instruction
- distance to next maneuver
- current road and remaining distance

`LwsGpsMapGraphic` draws route/player geometry directly with `VertexHelper`. Route geometry changes only when the route changes. Player marker/text refresh is throttled to a configurable interval, currently 0.15 seconds.

## Development GPS UI

`LwsNavigationDebugPanel` is added in `InterstateCorridorValidation` by `LwsInterstateCorridorRuntimeBuilder` when `createNavigationValidation` is enabled.

Debug controls:

- START TEST ROUTE
- CLEAR ROUTE
- RECALCULATE ROUTE
- GPS voice ON/OFF

The debug voice button changes the real player setting, not a parallel debug-only flag.

## Voice Guidance

Voice guidance is implemented by:

- `ILwsGpsVoiceGuidanceService`
- `LwsGpsVoiceGuidanceService`
- `LwsGpsVoicePack`

Default pack:

`Assets/LWS/InterstateHauler/Navigation/Data/IH_GpsVoicePack_Default.asset`

The default pack intentionally contains empty clips, but it exposes one assignable AudioClip slot for every supported maneuver and distance/context slot.

Voice prompts are event/step driven:

- route started
- approaching a step threshold
- off route/recalculating/recalculated
- arrival

The service tracks announced step IDs and will not repeat the same maneuver every frame.

## Voice Setting

Player setting:

`GpsVoiceGuidanceEnabled`

Default:

ON

Storage:

Unity `PlayerPrefs` key `ih.settings.gpsVoiceGuidance`

Turning OFF:

- stops any current voice clip
- suppresses future voice clips
- leaves visual GPS active
- does not clear the route

Turning ON:

- allows future prompts
- does not replay already-completed route steps unnecessarily

## Audio Routing

The cab GPS controller creates or reuses a dedicated `AudioSource` on the player truck:

- 2D audio
- no Doppler
- no loop
- no play-on-awake

Prompt 044 can later bind this to a production mixer and navigation voice volume.

## Compass Audit

Installed package:

- Compass Navigator Pro
- Root: `Assets/Plugins/Kronnect/CompassNavigatorPro`
- Namespace: `CompassNavigatorPro`
- Version evidence: README history includes Version 6.0

Useful APIs:

- `CompassPro.follow`
- `CompassPro.showMiniMap`
- `CompassPro.SetRoute(IList<Vector3> worldPoints)`
- `CompassPro.SetRouteToPOI(CompassProPOI poi)`
- `CompassPro.SetRouteToDestination(Vector3 worldPos)`
- `CompassPro.ClearRoute()`
- `CompassPro.hasRoute`

Prompt 011 includes `LwsCompassRoutePresenter`, which can reflectively pass LWS route waypoints into Compass if a Compass instance is available. Compass is not used for route solving.

## Traffic Independence

GPS does not depend on UTS vehicle positions, UTS paths, `CarAIController`, `CarWalkPath`, or `WalkPath`. Future traffic-aware rerouting should consume LWS traffic services/events, not UTS concrete scripts.

## Performance

Prompt 011 avoids Prompt 009A-style hot-path issues:

- no route rebuild every frame
- no scene-wide search every frame
- no UTS path scan for routing
- map text/marker refresh is throttled
- route geometry is reused until route changes
- debug UI reads service state and cached fields

## Validation

Automated coverage added:

- route planner finds a route through a graph
- one-way edges are respected
- maneuver classification covers straight/slight/turn/sharp and ramp
- default voice pack exposes every maneuver slot
- GPS Voice Guidance defaults ON and suppresses prompts when OFF
- off-route state is detected
- route planner has no UTS source dependency
- navigation services initialize and start a route
- physical GPS presenter binds a world-space screen
- disabling voice leaves the route active

Normal Unity Editor manual validation is still required for cockpit readability, live route driving, route recalc during driving, traffic regression, physical G29 regression, mirrors/dashboard regression, and real voice clips.

## Limitations

- The physical GPS display is a cab overlay because no safe named GPS display mesh/material was found.
- The map graphic is development baseline quality.
- Empty voice clips are allowed and expected until recordings are provided.
- Route planner costs are distance-first only.
- No production destination/map browsing UI exists yet.
- No traffic-aware routing exists yet.

## Prompt 012 Recommendations

Prompt 012 should treat Weather Maker as weather/rendering authority and consume only LWS navigation state if weather UI or debug context needs current road/route information. Weather gameplay should not alter route authority directly.
