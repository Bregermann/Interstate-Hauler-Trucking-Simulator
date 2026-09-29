# Sleeper Occupant Sidecar (Addendum N/O)

No model swap or scene authoring is performed by this change. The existing day cab keeps its legacy seat placement and private-event path when no enabled, sleeper-capable rig is authored.

## Prefab Setup

Add `TruckTaxiCabOccupantRig` to the player tractor root, the same object passed to `TruckTaxiSeatProfile.PlaceActor` and `TruckTaxiDriverPresentation.Initialize`. Set `hasSleeperCapability` only on the future sleeper tractor. Author unique `Anchor.id` values with real cab-relative mount paths, position, rotation, and scale.

Supported IDs are DriverSeat, FrontPassengerSeat, FrontCenter, SleeperLeft, SleeperRight, SleeperRear, SleeperBed, and FloorPerch. Role flags and optional seat-type restrictions filter candidates; the requested ID wins, then priority selects a free compatible anchor. `PosePart` paths are relative to the occupant visual root and override local part transforms. Hidden-in-cab occupants use the existing interior-hidden layer 29; exterior cameras retain their normal visibility.

Provide DriverSeat for Driver, FrontPassengerSeat for Companion/Passenger, and enough rear anchors for the intended passenger count. Seat profiles can select primary/secondary IDs. Secondary occupants require a rear anchor by default. `TruckTaxiSeatProfile.TryPlaceActor(...)` returns false when authored capacity is insufficient; it does not stack riders into the windshield. The existing void `PlaceActor(...)` reports that authoring failure. Rig anchor scale replaces legacy front-seat shrink factors. Seated bodies are kinematic and colliders disabled; `TruckTaxiPassengerActor` remains responsible for exit/ejection physics.

For private events, author two distinct rear IDs with `privateEventEligible`, one allowing Driver and one allowing Companion. The rig reserves a compatible pair atomically. `privateTransitionSeconds` defaults to 0.9 seconds. Optional `privacyDivider` is an existing authored object, activated after the move and restored on cleanup.

## Runtime Integration

No Bootstrap, HUD, input-source, coordinator, or passenger-runtime edits are required. Existing `DriverPresentation.Initialize`, `SeatProfile.PlaceActor`, and `RoadsideCompanionLoop.Initialize` discover the rig on the tractor root.

`TruckTaxiDriverPresentation.State` exposes Driving, MovingToSleeper, SleeperOccupied, and ReturningToDriver. The loop exposes `SleeperSequenceActive` and `FrontSeatsVacated`. The enhanced path requires the existing private-stop/parking-brake checks, a visible driver, NWH VehicleController with automatic input ownership, enabled LWS truck controls, an available exterior camera in the existing CameraChanger list, and two compatible private rear anchors. Missing requirements reject the sleeper event without beginning rocking.

The loop moves both clothed consenting adults to rear anchors before calling the existing `TruckTaxiPrivateEventVehicleMotion.Begin`. It switches via NWH CameraChanger.NextCamera and updates the existing camera presentation monitor. During the sequence it temporarily uses NWH's documented `input.autoSetInput = false`, neutral throttle, full brake/handbrake and clutch; disables semantic truck commands and the LWS transmission controller; and holds the parked rigidbody constraints. Keyboard and wheel input providers are not replaced. Existing input ownership must be automatic before acquiring this temporary hold.

Finish, cancel, ejection, disable, and destruction stop render-only motion, restore the driver/front transforms and divider, release the companion seat before its existing exit/ejection flow, restore camera/controls/transmission/input mode and body constraints, and clear stale driving commands. The previous residual velocity or throttle is deliberately not replayed. The return pose is an immediate restore; a bespoke return animation can be authored later. Do not change truck input/camera ownership from another system while `SleeperSequenceActive` is true; cancel the companion sequence first.

For a future third rider, use `rig.TrySeat(...)` with its role, preferred rear anchor, seat type, and standing world scale. No third-passenger continuity or ride logic is added here.

## Parent Validation

This worker did not run Unity, compilation, Play Mode, or builds. Eight added EditMode tests cover day-cab compatibility, restrictions/capacity, pose/visibility, lightweight seating, two-seat atomicity, transition ordering, anchor scale independence, reservation restoration, and completed/interrupted rig restoration. These tests have not been executed by this worker.

Parent should compile/import, run `TruckTaxiCabOccupantRigTests` and existing occupant/companion tests, then verify an authored sleeper: front seats vacate before motion; camera remains exterior; keyboard and wheel cannot move the truck; finish/cancel/eject/disable during both transition and rocking restore the driver, camera and controls. Also check pause/resume, private-event start rejection when anchors/camera are missing, first-person culling, and day-cab visual equivalence. Physical hold/camera integration still requires this runtime validation.

## Vendor Audit

- VENDOR ASSETS AUDITED: NWH Vehicle Physics 2 public VehicleInputHandler/VehicleInputStates; NWH Common CameraChanger/CameraInsideVehicle; existing LWS control, transmission, camera monitor and passenger presentation adapters.
- VENDOR ASSETS USED: NWH VehicleController.input manual-input API, CameraChanger.NextCamera, existing TruckTaxiPrivateEventVehicleMotion render-only motion.
- RELEVANT ASSETS NOT USED: NavMesh/UTS interior movement and full seated ragdolls are unnecessary for attached cab presentation.
- CUSTOM SYSTEMS CREATED: Cab anchor/reservation/pose data and private-event occupant transition coordination.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
