# Prompt 022 - Delivery and Parking

## Authority

ILwsDeliveryService / LwsDeliveryService is the focused LWS authority for destination arrival, delivery context, assigned-trailer parking validation, and the semantic handoff to results.

It coordinates existing authorities rather than replacing them:

- ILwsActiveJobService owns the accepted job and persistent lifecycle.
- ILwsTrailerRegistryService resolves the physical trailer by stable LWS identity.
- ILwsTrailerCoupling and LwsNwhTrailerCouplingAdapter report actual NWH coupling.
- ILwsGameplayStateService owns macro gameplay state.
- ILwsNavigationService owns route intent.
- ILwsSaveService exposes the existing Prompt 017 autosave request seam.
- ILwsWorldOriginService remains global/local position authority.
- Pixel Crushers remains the sole production persistence framework.

The delivery service does not own rewards, economy, XP, save files, NWH physics, trailer assignment, route algorithms, or Compass presentation.

## Runtime Components

| Component | Responsibility |
| --- | --- |
| LwsDeliveryArea | Broad destination arrival trigger keyed by an authored stable destination ID. Tracks canonical tractor colliders without duplicate enter events. |
| LwsDeliveryParkingZone | Trailer-collider containment, horizontal heading, motion, hold-time validation, tunable parking bounds, and optional development visualization. |
| LwsDeliveryService | Resolves the destination and assigned trailer, enters delivery, evaluates parking, completes semantic delivery, updates navigation, and requests autosaves. |
| LwsDeliverySnapshot | Current UI/debug state for destination, trailer, containment, heading, speed, hold time, readiness, and diagnostic message. |
| LwsDeliveryCompletionSnapshot | Compact persistent completion handoff retained inside LwsActiveJob; it does not duplicate the job. |

Exactly one delivery service is registered by LwsApplicationBootstrap.

## Destination Runtime Semantics

LwsDestinationDefinition.StableDestinationId remains persistent destination identity. A runtime LwsDeliveryArea references that definition and its LwsDeliveryParkingZone. Scene names and trigger booleans are not saved as identity.

The validation corridor builder creates one delivery runtime for each enabled destination in the authored validation catalog. Each runtime root is placed from the destination's double-precision global position using ILwsWorldOriginService.GlobalToLocal, then registered as a floating-origin transform participant.

Delivery area and parking target are separate:

- The delivery area means the canonical tractor reached the matching facility.
- The parking target validates the assigned trailer's final physical placement.

Entering the broad area alone never completes a job.

## Delivery Flow

    LwsActiveJob.Status = HaulActive
            |
            | canonical tractor enters matching LwsDeliveryArea
            | exact assigned trailer is physically coupled and registered
            v
    LwsActiveJob.Status = Delivery
    LwsGameplayState = Delivery
            |
            | route objective clears/re-focuses to local parking target
            | validation runs only against assigned trailer
            v
    containment + heading + settled motion valid for 2 seconds
            |
            v
    LwsActiveJob.Status = Completed
    LwsGameplayState = DeliveryResults
            |
            v
    Prompt 017 autosave requested

Delivery allows driving. DeliveryResults blocks driving.

## Correct-Trailer Policy

Initial delivery requires the Prompt 021 assigned trailer to remain physically coupled while parking. The service compares the active job trailerAssignment.stableTrailerId with playerTruck.CouplingAdapter.CurrentTrailerId and resolves that same ID through ILwsTrailerRegistryService.

A tractor alone, detached assigned trailer, another dry van, or any unassigned trailer cannot enter or complete delivery.

No disconnect step is required. Correct parking auto-completes after the hold duration. This avoids brittle hitch manipulation and leaves post-delivery trailer cleanup to a later owning system.

## Parking Validation

LwsDeliveryParkingZone samples each enabled non-trigger trailer collider. Box colliders contribute eight corners plus center; other colliders contribute world-bounds corners plus center. Samples are converted into target-local space.

Current success conditions:

1. Active job status is Delivery.
2. Active area destination ID exactly matches active job destination ID.
3. Canonical tractor remains in the broad delivery area.
4. Assigned trailer resolves by stable ID.
5. NWH/LWS coupling reports a physically attached trailer.
6. Attached trailer ID exactly equals assigned trailer ID.
7. At least 80 percent of representative collider samples are within the parking volume, including tolerance.
8. Horizontal trailer heading error is no more than 12 degrees.
9. Trailer linear speed is no more than 0.35 m/s.
10. Trailer angular speed is no more than 5 degrees/s.
11. Every condition remains valid continuously for 2 seconds.

The hold resets whenever validity fails, the trailer attaches/detaches, the player leaves the area, a floating-origin shift starts, or development tooling explicitly resets it. There is no automatic job failure for leaving or trying again.

## Default Values

| Setting | Default |
| --- | --- |
| Parking zone dimensions | 5.5 m wide x 5.5 m high x 18 m long |
| Containment tolerance | 0.35 m |
| Minimum containment ratio | 0.80 |
| Allowed heading error | 12 degrees |
| Opposite heading accepted | No |
| Maximum trailer linear speed | 0.35 m/s |
| Maximum trailer angular speed | 5 degrees/s |
| Minimum continuous hold | 2.0 seconds |
| Validation arrival trigger | 54 m x 8 m x 64 m |
| Development line width | 0.20 m |
| Completion mode | assigned-trailer-coupled-auto-hold |

## Manual Tuning

For authored/runtime components, tune the LwsDeliveryParkingZone Inspector:

- Parking Bounds / Zone Dimensions
- Parking Bounds / Containment Tolerance Meters
- Parking Bounds / Minimum Containment Ratio
- Heading / Allowed Heading Error Degrees
- Heading / Allow Opposite Heading
- Settling / Maximum Linear Speed Meters Per Second
- Settling / Maximum Angular Speed Degrees Per Second
- Settling / Minimum Valid Hold Seconds
- Development Visualization / Show Runtime Visualization
- Development Visualization / Line Width Meters and colors

Validation defaults are supplied in LwsInterstateCorridorRuntimeBuilder.BuildDeliveryValidation() through parkingZone.Configure(...). Production scenes can author destination-specific values.

## Navigation

Entering delivery clears the road destination and requests a local objective through the existing LWS navigation seam when a road graph is available. If a local road route cannot resolve, the route remains cleared and the parking target marker remains active. No Compass Navigator Pro internals were modified and no second minimap or route authority was created.

## Persistence and Load Derivation

Delivery data rides inside the existing LwsActiveJobSaveParticipant payload and Pixel Crushers snapshot.

- HaulActive restore derives LwsGameplayState.HaulActive; Prompt 021 destination guidance remains in charge.
- Delivery restore derives LwsGameplayState.Delivery. When the matching runtime area registers, the service restores the parking target/local guidance and starts with a zero hold timer.
- Completed plus a valid LwsDeliveryCompletionSnapshot derives LwsGameplayState.DeliveryResults. The active job remains present for the later results/economy owner.

Runtime collider occupancy, rapidly changing speed, and hold telemetry are not persisted. Completion data includes stable active-job, destination, and assigned-trailer IDs, UTC completion ticks, containment, heading error, lateral/longitudinal offsets, valid hold duration, delivery-context duration, and validation mode.

Autosave requests:

- delivery.entered
- delivery.completed

## Development UI

The existing development control center Truck tab now reports active/current destination, parking target, assigned/attached trailers, exact coupling, containment, heading, linear/angular motion, hold progress, readiness, and the latest delivery diagnostic.

Actions:

- DELIVERY TARGET
- RE-EVALUATE
- ENTER DELIVERY
- RESET HOLD

There is no payout or complete-and-pay cheat.

## Validation Content

LwsInterstateCorridorRuntimeBuilder adds a paved validation pad, broad arrival trigger, visible parking rectangle, and heading indicator for every enabled authored validation destination. This is development art and can be replaced without changing delivery semantics.

## Automated Validation

Executed with Unity 6000.4.10f1:

- Runtime and test assembly compile: passed.
- Prompt 022 EditMode tests: 8 passed, 0 failed.
- Prompt 022 PlayMode tests: 2 passed, 0 failed.

Coverage includes stable destination/trailer matching, wrong identity rejection, horizontal heading, containment/tolerance, stopped thresholds, hold reset, active-job transitions, load-state derivation, completion serialization, single delivery authority, parking component collider evaluation, and results handoff without clearing the active job.

## Manual Validation Required

A full NWH driving pass is still required in InterstateCorridorValidation for:

- accepting a real job and coupling the assigned trailer
- driving to the authored destination
- wrong-destination and wrong-trailer physical entry
- backing and containment feel
- moving/angled rejection
- continuous hold and automatic completion
- save/load during Delivery and after completion
- upright recovery during Delivery
- visible marker placement at each destination

These are not claimed as passed by source compilation or synthetic PlayMode coverage.

## Known Limitations

- Initial completion is automatic after the valid hold; a final confirmation UI is deferred.
- Runtime marker lines are development visualization, not final delivery art.
- The service records parking measurements but does not score or monetize them.
- UTC completion ticks are used because no completed-delivery game-clock contract currently exposes an acceptance-safe timestamp.
- Post-delivery trailer disposition and active-job cleanup belong to later systems.
