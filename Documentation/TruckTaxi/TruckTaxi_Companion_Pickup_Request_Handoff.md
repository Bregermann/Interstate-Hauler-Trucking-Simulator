# Ordinary Companion Pickup Request

Scope: the nonsexual optional pickup diversion from section 7. No private event,
inventory action, identity-based inference, authored content, or new dialogue/UI
framework is introduced. A small panel reuses the existing HUD factories/modal bridge.
Bootstrap, HUD core, GPS/navigation, and Session core are not
edited by this workstream.

## Parent Wiring

1. Create one `TruckTaxiCompanionPickupRequest` component for the session, after
   `host.Companions`, `host.GPS`, `host.DriverNeeds`, and `host.hud` are initialized. Call
   `Initialize(host)` once; an optional seed supports reproducible selection.
2. Call `Tick()` from the existing gameplay update. It does not have its own
   Update method. Offer searches are limited to twice per game second; direct
   `TryOffer()` is available for explicit refresh/testing. Pause does not roll.
3. Subscribe to `Changed`; read `HasOffer`, `IsActive`, `IsBoarding`, `State`,
   `Prompt`, `Feedback`, `RideId`, `Target`, and `RouteLeaseToken`. For patience,
   use `IsLegitimateDiversion`: accepted AND owning the active route, never a
   covered/suspended diversion merely because it remains accepted.
4. Initialize creates/reuses `TruckTaxiCompanionPickupRequestPanel` on the existing
   HUD. Its Heat-factory buttons call `Accept()` and `Decline()`. These
   return whether the action succeeded. `Cancel()` closes only this request;
   it does not eject an occupant, replace the fare, or start another event.
5. Author `PassengerProfile.companionPickupEligible = true` only for intended
   passengers. The default is false; `companionPickupChance` defaults to .15.
   No gender, age, appearance, personality text, or special-trait inference occurs.
6. The existing horn binding still calls `Companions.TryHornPickup()`. Do not
   call `Interact()` or any private-event API on request acceptance/completion.

The coordinator does not change Session's patience rules.
Keep one coordinator for a Session: its once-per-fare ledger is retained through
disable/re-enable and repeated Initialize with the same host/session. A new
Session gets a new ledger. No persistence schema is introduced; Session load
already ends the active fare rather than restoring an in-progress request.

### Modal Ownership

The panel uses `TruckTaxiHudModalBridge` for pause, focus, cancel and interruption.
It creates no Canvas, EventSystem, input actions, or independent input polling.
`CanOfferPresentation` must explicitly return true before a chance roll; the panel
assigns its `CanOffer` check, including shared modal availability and the fare route.
The acquisition path checks again before claiming the lease. Bathroom, service,
private-event, and existing HUD modals therefore cannot be bypassed by the offer.
If no presentation capability is available, no request is rolled or shown.

The bridge pauses while the offer is open. `Accept()` permits that paused state
only when `HudPanel.OwnsOfferHold` proves this exact panel owns an open shared lease.
The panel never manually unpauses: accept, decline, cancel, and interruption dispose
only its owned lease, leaving the bridge and Bootstrap.CanResume guards authoritative.
Headless routing/boarding tests inject an explicit presentation capability; they do
not claim visual, focus, or live Heat validation.

## Loop Contract

- `PickupTarget` is an immutable handle containing `Id`, actual `Actor` Transform,
  and `Variant`. IDs include the materialized actor incarnation, not just its
  logical population slot. Read position only while the actor remains available.
- `TryFindPickupTarget(radius, out target)` scans existing materialized actors,
  never spawns one. It is bounded by the loop's materialization radius (request
  default: 120m), checks region availability/cooldown/world lifecycle, and requires
  an actual compatible safe secondary seat for that actor's dimensions.
- `IsPickupTargetAvailable(target, radius)` revalidates the same actor instance.
- `TryReservePickupTarget(owner, target, canBoard)` restricts nearby horn selection
  to this target. The coordinator's predicate requires the same fare and the
  current owned pickup route. Covering service/emergency routes suspend pickup.
- `ReleasePickupTarget(owner)` cannot release another owner's reservation.
- `IsPickupTargetBoarding` and `IsPickupTargetOnboard` report the actual lifecycle.
- `CompanionBoarded` fires after `TrySeatSecondary` succeeds and `CompleteSeating`
  finishes. Horn presses, proximity, reservation, and arrival do not complete it.

## Request and Route Rules

The policy permits one selection roll per explicitly eligible fare, only when a
real target and secondary seat are available. Failed chance rolls, decline,
cancel, and success cannot be retried on that fare. Temporary lack of capacity
does not consume the roll. No other pending/active passenger diversion or higher
navigation layer is replaced when offering or accepting this request.

Acceptance reserves the exact actor, validates a truck route with the existing
GPS adapter, and claims only `PassengerDiversion` using owner
`companion-pickup.<rideId>` and the target ID. The original `RideFinal` lease is
left intact. Route failure releases the reservation and grants no reward.

After actual seating, the coordinator proves that its captured owner/target/token
can be released. It then awards `ApplyMechanicReward(0, rewardFareCents)` exactly
once. The default 400-cent fare bonus follows existing request tuning and is
clamped to the Session mechanic-reward authority's limits. There is no inventory,
chaos, private-event, or automatic consumption reward. Covering service/emergency
leases survive both success and cancellation. Revealing the underlying fare
destination is the GPS stack's responsibility, not a hard-coded saved coordinate.

Target loss, respawn, seat loss, movement away from the routed point, fare change,
disable, or route replacement cancels the request. It never silently substitutes
another actor or generates a coordinate. Movement before acceptance is resolved
from the still-valid actor's actual position. Movement during the real boarding
coroutine is expected and does not invalidate the pickup.

## Validation

- Category `TaxiCompanionPickupRequest` contains pure EditMode policy tests for
  explicit eligibility/capacity, 15% selection, no rerolls, decline/cancel, exact
  fare/actor matching, one completion, and nonfinite authoring values.
- The new PlayMode fixture calls real GPS APIs, horn pickup, and the boarding
  coroutine. It covers accepted reward/resume, decline, flag/seat/actor gating,
  loss under a service overlay, replacement diversion during boarding, moved or
  respawned targets, next-fare protection, disable/re-enable, unrouteable targets,
  horn suspension/resume, and completion beneath a covering service lease.
- The actual plain C# policy was executed in memory without Unity: 150 offers
  across 1,000 eligible midpoint samples, no rerolls, explicit eligibility and
  capacity gates, exact fare/actor matching, and duplicate-completion rejection
  passed. This is not an execution of the Unity EditMode/PlayMode fixtures.
- PlayMode GPS tests place an actual target inside the pickup radius and use the
  adapter's existing arrived-target branch. They do not claim to validate a full
  authored road graph or Compass rendering. The no-graph case must fail safely.
- New Unity tests are authored but not executed by this workstream. Parent owns
  Editor compilation, discovery, execution, and scene/HUD wiring. No Unity launch,
  build, serialized YAML edits, or commits were performed.

### Population Expectation Review

The four reported failures in the three owned population test files reflected
superseded defaults, not a new reduction in population code. Commit `866a6315`
raised multipliers from 12/5 to 60/25; regional commit `25c8b4a4` retained the
720-pedestrian target while halving traffic to multiplier 12.5 and cap 150.
`TruckTaxi_Regional_Expansion.md` and subsequent validation reports preserve
720/150 logical targets. Thus observed baseline 12 now means 720 pedestrians and
150 cars, independently of presentation optimization. Assertions now reflect that.
The measured-baseline test raises its local cap to 2000 before contrasting observed
24 with authored baseline 12, so the production cap cannot mask a broken baseline
selection. Baseline-validation counts, manual spawn caps, and explicit ScaleTarget
test vectors are unchanged. No runtime population defaults were edited.

## Vendor Report

- VENDOR ASSETS AUDITED: existing Heat ButtonManager/HUD factories and shared modal
  bridge; Compass Navigator Pro route API at
  `Assets/Plugins/Kronnect/CompassNavigatorPro/Scripts/CompassProRoute.cs`, existing
  `LwsCompassNavigatorProAdapter`, UTS Passersby, Pixel Crushers Dialogue trigger
  APIs, and the existing NWH/companion seat integration.
- VENDOR ASSETS USED: existing Heat/HudRoot factories and shared input/modal bridge;
  GPS/LWS navigation stack and Compass route presentation path; existing NWH
  control/physics and companion secondary seating.
- RELEVANT ASSETS NOT USED: no new UTS general locomotion or Pixel Crushers dialogue
  graph is authored. Existing boarding is reused; the parent owns bootstrap and
  HUD dispatcher wiring. These assets do not supply the per-fare eligibility ledger.
- CUSTOM SYSTEMS CREATED: the small pickup-request policy/coordinator and target,
  reservation, and verified-boarding hooks in the existing companion loop.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
