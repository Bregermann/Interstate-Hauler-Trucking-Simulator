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
| `Initializing` | No | Runtime bootstrapping and required service readiness. |
| `LoadingWorld` | No | Broad state for Prompt 017 save/load/world-restore phases. |
| `FreeDrive` | Yes | Normal free-roam driving with no required active job. |
| `Paused` | No | Escape pause menu is active. |
| `Transitioning` | No | Short non-load gameplay transition. |
| `RecoveryError` | No | Critical load/transition failure requiring retry/recovery/cancel. |

## Current Transition Graph

```text
INITIALIZING
     |
     v
FREE_DRIVE <----------> PAUSED

FREE_DRIVE
     |
     v
LOADING_WORLD
     |
     +---- success ----> FREE_DRIVE
     |
     +---- failure ----> RECOVERY_ERROR
                            |
                            +--> retry -> LOADING_WORLD
                            |
                            +--> safe cancel -> previous safe state
```

`Paused` remembers a return state rather than permanently assuming resume means `FreeDrive`. That leaves room for future `HAUL_ACTIVE -> PAUSED -> HAUL_ACTIVE` behavior.

## Future Reserved States

Reserved enum entries exist to prevent prompt drift, but Prompt 019 does not implement their systems:

```text
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
    |
    +--> AT_DEPOT
    |
    +--> FREE_DRIVE
```

Future owners:

- `AT_DEPOT`: depot prompt.
- `JOB_SELECTION`: job board/job generation prompts.
- `TRAILER_PICKUP`: trailer assignment prompt.
- `HAUL_ACTIVE`: haul/job runtime prompt.
- `DELIVERY`: delivery/parking prompt.
- `DELIVERY_RESULTS`: economy/rewards/results prompt.

## Input Relationship

`AllowsDrivingInput` is a broad gameplay gate. Current behavior:

- `FreeDrive`: driving allowed.
- `Initializing`, `LoadingWorld`, `Paused`, `Transitioning`, `RecoveryError`: driving blocked.

This does not replace `ILwsVehicleInputService`, input ownership, G29/wheel calibration, NWH input adapters, or Prompt 006 transmission authority. Input systems may consume this state later, but continuous driving ownership remains explicit in the existing input layer.

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