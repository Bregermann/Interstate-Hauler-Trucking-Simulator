# Truck Taxi Lose Vehicle pursuit limit

## Decision

Lose Vehicle remains disabled. The installed UTS and LWS APIs inspected here do not
provide a clean way to route an existing UTS car through connected legal traffic
lanes toward a moving player. Increasing speed on its current loop, aiming its
steering at a graph waypoint, or swapping in raw graph centerline points would
not meet the objective's road-following pursuit requirement.

## Exact boundary

- `MovePath` exposes its current `WalkPath`, lane index, waypoint index, and
  `finishPos`, but no destination/repath or path-chain operation.
- `CarAIController.GetPath` advances only within that one `WalkPath`. At a
  non-looping path end it queues another spawn and destroys the car; a looping
  path wraps to its start. `CarWalkPath` supplies waypoint geometry, not a
  junction-turn routing contract.
- `TruckTaxiTrafficAdapter` creates independent UTS paths from authored
  `cityLanes` and spawns vehicles onto one path. Its stable IDs and AI access
  support identifying a pursuer, but do not link those paths into a routable
  lane network.
- `TruckTaxiRouteDistanceService` can return an LWS road-graph route. The route
  planner's waypoints are road-edge sample positions, not verified traffic-lane
  centers or turn connectors. The graph lane builder creates lanes per edge,
  not a continuous drivable path across selected edges; it also covers only
  interstate/ramp edges, not the Taxi city lanes.

The public fields could be mutated or a temporary `CarWalkPath` could be built,
but that would require a separate, verified graph-to-UTS lane/turn mapping and
safe live route replacement. Neither contract exists in the inspected setup.
That is more than a thin pursuit adapter, and an unverified bridge could send
the pursuer through opposing lanes, junction corners, or off the roadway.

## Revisit criteria

Author or expose a connected, directed traffic-lane route with legal junction
connectors for the Taxi road network, then provide a supported way to hand that
route to a live UTS actor without teleporting or destroying it on repath. Prove
continuous UTS movement through a turn and a player route change before enabling
`TruckTaxiObjectiveCapability.Pursuit` and adding escape-distance/hold-time tests.

No runtime code, vendor source, scene, or capability gate was changed in this
bounded investigation. No Unity runtime or build validation was run.
