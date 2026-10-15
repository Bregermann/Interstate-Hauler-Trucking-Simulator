# Phase 0.8 / 0.9 Integration Handoff

Status: scoped implementation and focused EditMode tests authored. Static diff
review only; no Unity, builds, test execution or commits. Parent owns integrated
compilation and runtime validation. The chat connector cannot message the native
parent; this shared handoff and the workstream final response carry the hooks.

## Pending Parent Hooks

- Bootstrap: create one `TruckTaxiRivalTheft` and call
  `Initialize(this, rivalPopulation)` after Session, Player, GPS and the existing
  `TruckTaxiRivalPopulation` are initialized. Retain it as `RivalTheft`.
- Call `RivalTheft.Tick(Time.deltaTime)` before `Session.Tick` during gameplay.
- Include `RivalTheft.IsLegitimateDiversion` in the existing legitimate
  traffic/service/diversion predicate; do not reset primary patience.
- Session core: guard ordinary primary destination arrival with
  `(!HasStolenPassenger || SafeDropRequested)`. The safe-drop exception is
  important: an Emergency lease must not deadlock beneath a stolen-passenger
  guard. Primary fare, passenger, destination and requests remain
  unchanged. A stolen fare never becomes `Session.Passenger`.
- Session core: call `ResetRivalPassengerTheft()` on EndShift. Persistence: call
  it only after a load has passed validation, at the accepted transient reset.
- HUD: keep the primary fare; display `Session.StolenPassengerFare` as the extra
  stop and `RivalTheft.Feedback` for transfer/delivery messages. The fare exposes
  Id, RivalId, PassengerId, Passenger, DestinationId, DestinationName,
  DestinationPosition, DestinationRadius and PayoutCents. No HUD payout logic.
- Save: explicitly reject manual save AND autosave while
  `Session.HasStolenPassenger`. Reason: `Deliver the stolen passenger before
  saving.` Never cancel/drop the stolen fare as a side effect of saving. Existing
  wallet capture already includes its completed payout in ShiftEarnings.
- Recovery/teleport or world teardown: call `RivalTheft.Cancel()` as part of the
  existing temporary-action cancellation path. It abandons this extra fare
  without payment, releases only its exact route token and preserves the primary.

## Authority And Route Contract

`TruckTaxiSession.RivalTheft.cs` is a partial extension of the existing fare and
wallet authority, not another session. Source fares use a distinct immutable ID
for every pickup. Rival ID, fare ID and destination ID comparisons are exact.
Source releases cannot release a transferred fare. Completion removes the fare
before publishing events and pays ShiftEarnings once, separately from LastFare
and MechanicFareAdjustment. In-flight stolen fare persistence is intentionally
not added; saves must be explicitly blocked until delivery or intentional cancel.

`TruckTaxiNavigationIntents.cs` adds RivalPassenger above PlayerService and below
Emergency. Its route owner is `rival-theft.<fareId>` and target is the authored
destination ID. Cleanup uses layer, owner, target AND token. Existing fare,
passenger diversion and player service leases stay underneath unchanged. Higher
emergencies suspend delivery; stale callbacks never release replacements.

## Ownership

This workstream does not edit TruckTaxiSession.cs, .Persistence.cs, Bootstrap,
HUD, WorldLife, GPS, scenes, prefabs or vendor source. Main/other dirty changes
are preserved. Main owns the integration hooks above.

## Root Causes And Changes

- Secondary clearance used the full standing radius of an already scaled/seated
  fare, rejecting genuine extra capacity. It now uses actual seated world scale,
  including hidden occupants, without changing either transform.
- Legacy secondary placement fabricated a center perch; primary seat kind was
  inferred from profile type. Certified secondary anchors are now required,
  primary fare role is explicit, and primary anchors cannot fall back to a
  secondary reservation. Boarding readiness checks capacity before acceptance.
- Companion loop capacity did not explicitly reject its own busy occupant. It
  now rejects a second companion while preserving the existing auxiliary ride,
  horn, boarding, private-event, ejection and driving behavior.
- Rival riders had only a timed hidden visual. They now have immutable Session
  fare/destination identity, a visible cab occupant and occupied/for-hire sign.
  Strong player contact requires normal impact speed >= 6 m/s and positive
  player approach >= 3 m/s, plus exact pooled generation and occupied fare.
  Rejected capacity or route acquisition leaves the rival fare untouched.
- Theft gives 12 seconds of visible anger, an angry horn and the existing bounded
  RoadRage.TryTrigger response. UTS steering, wild speed/personality, lane changes,
  physics and existing rage safety/cooldown limits are unchanged.

## Exact Changed Paths

All paths below are relative to `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator`.

- Assets/LWS/TruckTaxi/Scripts/TruckTaxiCabOccupantRig.cs
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiSeatProfile.cs
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiPassengerRuntime.cs
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiRoadsideCompanionLoop.cs
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiNavigationIntents.cs
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiRivalPopulation.cs
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiRivalVehicleVisual.cs
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiSession.RivalTheft.cs (new, plus .meta)
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiRivalTheft.cs (new, plus .meta)
- Assets/LWS/TruckTaxi/Scripts/TruckTaxiRivalCollisionRelay.cs (new, plus .meta)
- Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiRivalTheftTests.cs (new, plus .meta)
- Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiCompanionLifecycleTests.cs
- Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiCabOccupantRigTests.cs
- Documentation/TruckTaxi/TruckTaxi_Phase08_09_Handoff.md

## Validation And Remaining Acceptance

Scoped tracked `git diff --check` passed (line-ending warnings only). New runtime
API names were checked against existing source. Test categories TaxiRivalTheft,
TaxiCompanionLifecycle and TaxiSleeper cover role coexistence in both orders,
missing/occupied seats, concealed/scaled clearance, preserved primary state and
patience, exact IDs, stale/reset/cancel guards, impact policy, reentrant pay-once,
and preserved/replaced/covered route leases. Tests are authored, NOT executed.

Parent still must compile the integrated batch and verify an actual collision,
full seat rejection, primary-plus-stolen visuals, anger, independent delivery,
underlying route restoration, save/autosave rejection while active and successful
save after delivery. No new PlayMode fixture or visual acceptance is claimed.

## Vendor Report

- VENDOR ASSETS AUDITED: UTS public CarAIController controls at
  Assets/UTS_FullPack/Scripts/Car/CarAIController.cs and CarMove.Move at
  Assets/UTS_FullPack/Scripts/Car/CarMove.cs; NWH VehicleController at
  Assets/NWH/Vehicle Physics 2/Scripts/VehicleController/VehicleController.cs and
  NWH Common CameraInsideVehicle; existing Compass GPS adapter and current
  vendor-backed companion/seat integration handoffs. Installed versions not inferred.
- VENDOR ASSETS USED: unchanged UTS driving through TruckTaxiTrafficBehaviour and
  TruckTaxiRoadRage.TryTrigger; NWH player physics/cab camera ownership retained;
  Compass presentation via existing GPS named intent/release APIs and LWS routing.
- RELEVANT ASSETS NOT USED: no new Heat UI or Pixel Crushers save provider is
  needed here; parent uses existing HUD/save guard. Vendors do not provide the
  Truck Taxi-specific passenger transfer, capacity role or pay-once semantics.
- CUSTOM SYSTEMS CREATED: Session rival-fare partial and small collision/temporary
  stop coordination extensions only. No alternate wallet, fare, routing or AI authority.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
