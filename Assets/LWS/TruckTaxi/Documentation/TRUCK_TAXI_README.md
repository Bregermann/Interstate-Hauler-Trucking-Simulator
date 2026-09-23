# Truck Taxi

An isolated, fictional arcade taxi demo using the Interstate tractor, NWH physics,
LWS navigation/Compass, EasyRoads, UTS traffic/pedestrians and Heat UI.

## Launch
Open `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity` and press Play.
Choose START SHIFT. The Windows build starts directly in this scene.
No trailer, freight job, career profile or autosave is required or created.

Windows executable: `Builds/TruckTaxiDemo/TruckTaxi.exe`.
Keep the executable, its Data folder and adjacent Unity DLLs together.

## Build
Unity menu: **Truck Taxi > Build Windows Demo**.
This uses the existing Windows player pipeline and an explicit one-scene build list.
The demo is a Development build with the requested debug panel. Normal launches
remain interactive. The opt-in `-truck-taxi-smoke` command-line switch runs a
teleport-assisted validation ride, captures actual player frames, and exits.
It does not reorder the main game's scenes or replace the main product settings.
The scene is also appended to Build Settings.

**Truck Taxi > Create Or Refresh Demo City** rebuilds only the taxi scene.
Save any designer changes first. It preserves existing passenger/request assets but
regenerates city placement. Do not use refresh to preserve manual scene edits.

## Play
Accept or decline the dispatch offer. Follow the existing Compass map to the taxi
bay and stop below 0.5 m/s for 1.5 seconds. The passenger boards and GPS switches to
the destination. Optional passenger requests have their own timers and rewards.
Stop in the destination bay for one second to finish. NEXT FARE resumes the shift.

The demo intentionally permits fictional slapstick collisions. Passengers differ:
calm passengers penalize impacts; chaos passengers reward them. Fares are integer
cents, separate from chaos and request score. No real-world claims are implied.

## Controls
Existing Interstate controls remain in charge:
- W / Up: forward throttle; S / Down: brake, then reverse in automatic mode.
- A / D or Left / Right: steer.
- E: engine start; Shift+E: engine stop; I: ignition; P: parking brake.
- Tab: cycle existing truck cameras. U: existing Reset Truck Upright.
- Gamepad: existing steering/triggers and truck commands remain unchanged.
- Mouse: taxi buttons. Enter: start/accept/continue when no UI control is selected.
- Backspace: decline an offer (also retains the existing cruise-cancel binding).
- Escape / gamepad Start: taxi pause. F8 or DEBUG: taxi debug panel.
- Heat/uGUI buttons support submit/navigation through the Input System event module.

Gamepad south can also sound the existing horn while driving; taxi UI does not
remap or remove core truck commands.

The demo-local Compass HUD uses the lower-left corner with a 650-metre capture
area. Cab GPS, road graph routing and the underlying Compass renderer are reused.

## Debug Panel
Start/End Shift, Force Ride Offer, Auto Accept, Teleport Near Pickup,
Force Passenger Boarding, Teleport Near Destination, Complete/Fail Ride,
Generate Request, Complete/Fail Current Request, Add Chaos Score,
Spawn Traffic, Spawn Pedestrian and Reset Demo City.
Debug actions are explicitly labeled. They exercise guarded session methods;
they do not claim that driving or real contacts occurred.

RESET CITY restores player position, existing UTS vehicles, pedestrian hit state,
movable props and ride state without reloading the scene. Shift statistics remain
session-only. Population caps prevent debug spawn storms.

## Content
Twenty taxi locations form at least 50 valid origin/destination pairs.
The demo has ten passenger profiles:
Pat Pending (commuter), Max Volume (adrenaline), Joy Ride (chaos tourist),
Bill Rush (executive), Dot Matrix (conspiracy), Nora Brake (nervous),
Al Lee (shortcuts), Ray Rage (road rage), Cam Era (tourist), Robin Roundabout (local).

Eleven request definitions:
Fast Delivery, Shortcut, Ram Traffic, Hit Pedestrian, Property Damage, Offroad,
Smooth Ride, No Collisions, Maximum Chaos, Scenic Route and Near Miss.

## Authoring
- Passenger: Create > Truck Taxi > Passenger Profile. Edit personality, reactions,
  patience, tip chance, chaos affinity and allowed request references. Add the asset
  to the demo configuration's Passengers array. Portrait/audio are optional.
- Location: add TruckTaxiRideLocation. Give it a unique Location ID/name, allowed
  pickup/dropoff flags, stop/passenger points and radius. Locations are discovered
  once at startup. Place stops on usable ground near the existing road graph.
- Shortcut: add ShortcutTrigger and a trigger BoxCollider. Set ID, name, score,
  optional direction and minimum speed. Scenic Point changes its semantic event.
  Optional Passenger Dialogue overrides the current passenger's shortcut reaction.
  No object-name matching is used for gameplay.
- Request: Create > Truck Taxi > Passenger Request. Set type, description, timer,
  target/count, optional target ID, rewards, rating and cooldown. Add references to
  passenger profiles. Adding a genuinely new type requires extending TaxiRequestType
  and the local session evaluator, not changing GPS, NWH, UTS or the freight game.
- Tuning: TruckTaxi_DemoConfiguration.asset contains fares, scoring, stop/boarding,
  offer/trip limits, collision thresholds, near-miss settings and optional audio.

## Ownership
TruckTaxiSession is a local ride transaction/state machine, not a second global
gameplay manager. LwsGameplayStateService still gates driving/pause. Taxi GPS adapter
requests existing LWS routes; Compass owns rendering. Traffic adapter uses
LwsUtsTrafficApi.CreatePath/SpawnVehicle. PeopleWalkPath/Passersby own pedestrian
walking and animation; TruckTaxiPedestrian adds only strike/respawn semantics.
The only vendor source change is a two-directive Windows compile fix in Weather
Maker's WeatherMakerCloudProbeScript: Editor-only texture cleanup now has the same
UNITY_EDITOR guard as its declaration. No weather behavior was changed.

The only core code change is LwsApplicationBootstrap's opt-in drivingSandbox flag.
It excludes career persistence/jobs before initialization. Existing scenes default
to the full registry. Taxi statistics deliberately do not touch career storage.

`Prefabs/TruckTaxi_InterstateTractor.prefab` is a Unity-reserialized copy of the
existing Interstate tractor, retaining its vendor components. This avoids malformed
PrefabInstance references in the source variant without editing that original.
The scene generator refreshes this copy through Unity's prefab API.

## Scope And Limitations
- Simple compact validation city, not final city art or a traffic-signal simulation.
- Imported truck/building/adult pedestrian/car art reused; simple bays/props/parks.
- Traffic follows four bounded closed UTS loops; no new citywide traffic AI.
- Session-only earnings; restarting the application clears them.
- Pickup/exit use a short stopped dwell and visibility change, not walking into the cab.
- Request completion is event-based; offroad/smooth/timer checks use bounded telemetry.
- Near misses use a small nonalloc local overlap query, never a scene-wide scan.
- Real controller/wheel hardware and manual driving remain separate validation cases.
- Notification sounds and portraits are optional and unassigned in the sample content.
- Weather is fixed clear daylight; dynamic Weather Maker presentation is not added.
- The generator uses EasyRoads editor APIs and bakes mesh assets; runtime does not
  rebuild the road network or invoke AssetDatabase.

See TRUCK_TAXI_VALIDATION.md for actual test/build evidence.
