# Prompt 019 - Gameplay State Machine

## Authority

`ILwsGameplayStateService` / `LwsGameplayStateService` is the single LWS macro gameplay flow authority. It answers which broad gameplay phase the player is currently in. It does not implement jobs, saving, traffic, weather, truck physics, GPS, economy, or UI screens.

## Implementation Classes

- Runtime source: `Assets/LWS/InterstateHauler/Gameplay/LwsGameplayState.cs`
- Interface: `ILwsGameplayStateService`
- Implementation: `LwsGameplayStateService`
- Enum: `LwsGameplayState`
- Event: `LwsGameplayStateChangedEvent`
- Transition result: `LwsGameplayStateTransitionResult`
- Static state policy: `LwsGameplayStateRules`

The service is registered once in `LwsApplicationBootstrap.CreateDefaultRegistry()` as `lws.gameplay.state`.

## Current States

| State | Driving | Description |
|---|---|---|
| Initializing | No | Runtime bootstrapping and required service readiness. |
| LoadingWorld | No | Broad state for Prompt 017 save/load/world-restore phases. |
| FreeDrive | Yes | Normal free-roam driving with no required active job. |
| Paused | No | Escape pause menu is active. |
| Transitioning | No | Short non-load gameplay transition. |
| RecoveryError | No | Critical load/transition failure requiring retry/recovery/cancel. |
| AtDepot | Yes | Canonical tractor is in an eligible depot context. |
| JobSelection | No | Authored job board is open. |
| TrailerPickup | Yes | Active job is awaiting its assigned trailer. |
| HaulActive | Yes | Assigned trailer is coupled and freight is in transit. |
| Delivery | Yes | Matching destination reached; trailer parking is in progress. |
| DeliveryResults | No | Parking completed; retained active job awaits a later results owner. |
## Current Transition Graph

    INITIALIZING
         |
         v
    FREE_DRIVE <----------> PAUSED

    TRAILER_PICKUP
         |
         | assigned trailer physically coupled
         v
    HAUL_ACTIVE
         |
         | matching destination entered with assigned trailer coupled
         v
    DELIVERY
         |
         | assigned trailer parked and settled
         v
    DELIVERY_RESULTS

    any eligible gameplay state
         |
         v
    LOADING_WORLD
         |
         +---- success ----> state derived from active-job lifecycle
         |
         +---- failure ----> RECOVERY_ERROR

Paused remembers the appropriate return state rather than permanently assuming resume means FreeDrive.
## Active Job Flow States

Prompt 020 activated AtDepot, JobSelection, and TrailerPickup. Prompt 021 activated HaulActive. Prompt 022 activates Delivery and DeliveryResults.

    FREE_DRIVE
        |
        v
    AT_DEPOT
        |
        v
    JOB_SELECTION
        |
        v
    TRAILER_PICKUP
        |
        v
    HAUL_ACTIVE
        |
        v
    DELIVERY
        |
        v
    DELIVERY_RESULTS

All currently declared enum entries now have runtime meaning. Future prompts should add another macro state only when a real gameplay system requires it.
## Input Relationship

AllowsDrivingInput is a broad gameplay gate:

- Driving allowed: FreeDrive, AtDepot, TrailerPickup, HaulActive, Delivery.
- Driving blocked: Initializing, LoadingWorld, Paused, Transitioning, RecoveryError, JobSelection, DeliveryResults.

This does not replace ILwsVehicleInputService, wheel calibration, NWH input adapters, or Prompt 006 transmission authority.
## Prompt 017 Relationship

`LwsSaveLoadCoordinator` remains the detailed load ordering coordinator. Prompt 019 only maps broad load phases to macro gameplay state:

- Load/pre-read/application phases -> `LoadingWorld`
- `Complete` -> `FreeDrive`
- `Failed` -> `RecoveryError`

Hidden backup validation can pre-read vendor data without changing macro gameplay state. Pixel Crushers remains the save framework authority.

## Pause Relationship

`LwsPersistencePauseMenu` remains the Escape pause UI owner. Opening the Escape pause menu requests `Paused`. Closing it requests `Resume`, which returns to the remembered gameplay state.

The F3 direct save/load menu does not enter `Paused` by itself. If a load begins through F3, the Prompt 017 load coordinator drives the macro state to `LoadingWorld`.

## Persistence Boundary

`LwsGameplayState` is transient runtime flow state. It is not saved as career authority and has no `ILwsSaveParticipant`. Future job/depot/delivery systems persist their own semantic data; startup/load derives the macro state from the active runtime flow.

## Development Diagnostics

The existing development control center overview shows:

- Gameplay state
- Previous state
- State age
- Last transition reason
- Driving allowed

No F2 weather panel, F3 save/load layout, cab GPS, or pause visual rebuild was performed.

## Future Extension Procedure

When a future prompt needs a new macro gameplay mode:

1. Read `Gameplay_Vocabulary_and_Authority_Glossary.md`.
2. Reuse an existing state if it already represents the concept.
3. Add a new state only when a real gameplay system needs it.
4. Add or update transition validation in `LwsGameplayStateService`.
5. Update this document's current/future transition graph.
6. Keep feature behavior in the owning system, not in the state machine.
## Prompt 020 Active State Update

Prompt 020 activates three previously reserved macro states:

- `AT_DEPOT`: player tractor is inside a registered enabled depot context and not inside the modal job board. Driving is allowed so the player can maneuver around the yard.
- `JOB_SELECTION`: player is browsing/selecting an authored job through the job board. Driving is blocked.
- `TRAILER_PICKUP`: an Active Job exists with `Status = AwaitingTrailerPickup`. Driving is allowed. Prompt 021 owns physical trailer assignment and pickup completion.

Current Prompt 020 transitions:

```text
FREE_DRIVE
    |
    | canonical tractor enters eligible depot
    v
AT_DEPOT
    |
    | interact with job board
    v
JOB_SELECTION
    |
    +-- close/cancel --> AT_DEPOT
    |
    +-- accept authored job --> TRAILER_PICKUP
```

Depot presence is separate from macro gameplay state. Entering a depot during `TRAILER_PICKUP`, `HAUL_ACTIVE`, `DELIVERY`, `LOADING_WORLD`, or `RECOVERY_ERROR` updates depot presence only and does not blindly force `AT_DEPOT`.

Prompt 020 adds no new global state manager and does not persist raw `LwsGameplayState` as career authority. Active job semantic status is persisted through the Pixel Crushers semantic save snapshot and derives the appropriate macro state after load.

## Prompt 021 Active Haul Update

Prompt 021 activates `HAUL_ACTIVE` as a current runtime state.

```text
TRAILER_PICKUP
    |
    | correct assigned trailer physically coupled through NWH/LWS coupling
    v
HAUL_ACTIVE
```

`HAUL_ACTIVE` allows driving input. It remains a macro gameplay state only: delivery validation, parking scoring, payout, economy, XP, and reputation belong to later prompts.

The state is derived from `LwsActiveJob.status = HaulActive`. Raw `LwsGameplayState` is still not persisted as career authority.
## Prompt 022 Delivery Update

Prompt 022 activates Delivery and DeliveryResults:

    HAUL_ACTIVE
        |
        | matching stable destination area entered
        | exact assigned trailer physically coupled
        v
    DELIVERY
        |
        | trailer collider containment, heading, and stopped state
        | remain valid for the configured hold
        v
    DELIVERY_RESULTS

Delivery allows driving so the player can maneuver. DeliveryResults blocks driving while a later results/economy owner consumes the retained completed active job.

LwsDeliveryService owns detailed destination/parking coordination. The gameplay state service remains macro-only. LwsActiveJob.status is the persistent semantic authority: Delivery derives Delivery, and Completed with a valid delivery completion snapshot derives DeliveryResults after load. Raw gameplay state remains transient and is not saved.