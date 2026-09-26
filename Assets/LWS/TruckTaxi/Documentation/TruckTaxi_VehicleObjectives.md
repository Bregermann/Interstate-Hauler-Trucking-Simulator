# Truck Taxi vehicle objectives

The existing Truck Taxi bootstrap adds/reuses the runtime coordinator on its own GameObject
and calls `Initialize(host)` during initialization, including scene entry from the main menu. It
does not edit a scene or take over UTS traffic physics. The coordinator selects
only vehicles present in `TruckTaxiTrafficAdapter.ActiveVehicles`, retains the
stable traffic ID for the request, and fails the objective if that vehicle or
its UTS AI disappears. An authored `targetId` must resolve to that exact
active vehicle when the offer is made. Blank IDs choose an unreserved vehicle.

Available evaluators:

- Follow: both vehicles move while the truck maintains the configured distance
  band for the request's target number of seconds.
- Ram: the canonical tractor collision observer reports a player-caused
  `TrafficRam` with the assigned stable ID and qualified impact speed.
- Block: a previously moving UTS target stops with the slow tractor ahead in
  its lane, confirmed as the first physical raycast obstruction, for a hold
  interval.
- Race: a read-only LWS adapter chooses an upcoming waypoint on the assigned
  car's actual UTS path. The player must have a navigable LWS road-graph route
  to the same point. Physical first arrival inside the goal radius wins; the
  UTS car arriving first fails the request. An authored `goalLocationId` is
  accepted only when that location is near the upcoming UTS waypoint.
- Destroy: only the assigned mission vehicle gets
  `TruckTaxiMissionTarget` durability. Repeated qualified tractor impacts
  advance the request and the last hit retires that car; ordinary traffic
  never gets durability.
- Collect: mission objects drop physically near the assigned car. Each has a
  rigidbody, solid collider, separate collection trigger, bounded lifetime,
  and a typed Compass collectible marker. Only canonical tractor contact
  collects it. An expired required drop fails the objective.

Lose Vehicle is intentionally unavailable. Installed UTS `MovePath` exposes
mutable waypoint fields, but `CarAIController` advances them against a
shared `WalkPath`. Repointing `finishPos` toward the player would not be
road-following pursuit. An actual directed-pursuit adapter is required before
that capability can be enabled. Its generated request asset remains disabled.

Run `Truck Taxi > Vehicle Objectives > Create Missing Request Assets` to add
only absent definitions. Existing authored assets are never overwritten.
Select a PassengerProfile asset and run `Add Functional Requests To Selected
Passenger` to opt that passenger into the six available request types. Run
`Validate Request Assets` to check authored definitions. The parent
integration pass owns Unity compilation, tests, and runtime probe execution.
For a focused Play Mode probe, board a passenger, select one of the generated
request assets, then run `Force Selected Request In Play Mode`. This uses
normal Session selection gates; it does not force a success. Verify target
marker/ID, reject impacts on other cars, hold Follow/Block physically, reach
the Race goal before or after UTS traffic, destroy only the mission car after
multiple rams, and collect real drops with the tractor. Confirm target loss
and expired drops fail rather than grant progress.

Focused parent walkthrough:

1. Run the asset-create and validation menu items, select a passenger profile,
   and explicitly add functional requests. Enter TruckTaxi_DemoCity, board a
   passenger, and inspect `Session.Capabilities`. TargetVehicles requires a
   live UTS car with AI and a target icon. Race additionally requires a
   reachable upcoming UTS waypoint and an objective icon. Pursuit is zero.
2. Force Follow and drive in/out of the distance band. Progress rises only
   while both truck and assigned car are moving in-band. Force Ram and hit a
   different car first (no progress), then the assigned stable-ID car with
   the canonical tractor (success). AI striking the tractor must not count.
3. Force Block. Merely parking near the car does not count. Stop ahead in its
   travel lane so the tractor is the first physical raycast obstruction;
   hold while the previously moving target has stopped.
4. Force Race. The objective marker is on an upcoming UTS waypoint. Drive
   the truck to it first for success; repeat and let that same UTS car reach
   it first for failure. Neither car is moved or teleported by the mission.
5. Force Destroy and check that intermediate qualified rams leave the car
   alive. The final required ram retires only the assigned car; other
   traffic remains under UTS. Force Collect, observe rigidbody drops and
   Compass collectible icons, then collect with the tractor. AI contact does
   not collect. Let an uncollected drop expire to verify failure.
6. Remove/despawn an assigned vehicle during an active request and verify
   target-lost failure. Force Lose and verify selection is rejected rather
   than awarding distance-based progress.

No mission overrides the ride GPS destination. The existing
`TruckTaxiMapMarkers` registry and Compass icons present active vehicle and
drop markers. The `Collectible` icon must exist for collection missions to
be selectable. `TruckTaxiVehicleObjectiveCoordinator.Status(request)`
returns the assigned stable ID and live progress for presentation. The
HUD uses that status without mutating a shared ScriptableObject. Fare Complete
snapshots each description, progress, success/failure/expiry and earned reward.

The integrated Play Mode probe exercised an actual NWH tractor collision with
the exact assigned UTS car, completed Ram through the collision observer,
verified the Compass target marker, then physically entered a dropped-object
trigger and verified collection and marker cleanup. Follow, Block, Race and
Destroy have logic coverage but have not had an end-to-end manual driving pass.
No false claim of a pursuing AI is made: the rejected speed-boost-only attempt
was removed and Lose remains unselectable.
