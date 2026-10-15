# Prompt 021 - Trailer Assignment / Pickup

## Authority

Prompt 021 adds project-owned trailer gameplay semantics on top of the existing vendor/runtime stack.

- Physical tractor and trailer behavior: NWH Vehicle Physics 2.
- Physical coupling: existing NWH trailer hitch through `LwsNwhTrailerCouplingAdapter` and `ILwsTrailerCoupling`.
- Gameplay trailer identity, type, assignment, pickup validation, and job transition: LWS.
- Persistence: existing Pixel Crushers semantic save path through `LwsActiveJobSaveParticipant`.
- Navigation presentation: existing LWS navigation service and Compass Navigator Pro presentation adapter.

No vendor source is modified and no second physical trailer system is introduced.

## Existing Trailer Architecture Found

The validation dry van is `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab`, a project prefab/variant using NWH trailer components. Development scenes use `LwsPlayerTruckSpawner` to spawn the player tractor and validation trailer. The spawner now adds LWS trailer gameplay identity to the existing NWH trailer instead of replacing it.

NWH remains the coupling authority. `LwsNwhTrailerCouplingAdapter` reads NWH `TrailerHitchModuleWrapper` attach/detach events and publishes `LwsTrailerAttachmentState` with the attached trailer stable ID resolved from `LwsVehicleIdentity`.

## Implementation Classes

- `LwsTrailerAssignment`: serializable active-job trailer assignment payload.
- `LwsTrailerPickupSnapshot`: lightweight diagnostics/UI snapshot.
- `LwsTrailerGameplayIdentity`: scene/runtime component that gives a physical trailer a stable LWS trailer ID, trailer type, origin depot, and assignment status.
- `LwsTrailerRegistryService`: one runtime registry for resolving trailer identities and finding assignable trailers.
- `LwsTrailerSpawnPoint`: reusable semantic spawn point for deterministic trailer respawn/recovery.
- `LwsTrailerPickupService`: event-driven coordinator for assignment, pickup objective, coupling validation, haul transition, destination objective, and autosave request.

These classes do not own storage, serialization frameworks, NWH physics, Compass internals, or vendor save slots.

## Trailer Identity

Each gameplay trailer exposes:

- stable trailer instance ID
- trailer type ID, currently `trailer.dry-van` for validation content
- display/debug name
- origin depot ID
- assignment state
- assigned active-job ID

`LwsVehicleIdentity` remains the stable vehicle identity source used by vehicle/coupling systems. `LwsTrailerGameplayIdentity` syncs it so the NWH coupling adapter can resolve the attached trailer ID correctly.

## Assignment Lifecycle

When an active job is accepted with `Status = AwaitingTrailerPickup`, `LwsTrailerPickupService` resolves the required trailer type from `LwsActiveJob.requiredTrailerTypeId`.

Assignment flow:

1. If the active job already has `trailerAssignment`, resolve that exact stable trailer ID.
2. If the assigned trailer is missing, respawn that same stable trailer ID through an eligible `LwsTrailerSpawnPoint` when possible.
3. If no assignment exists, choose an available compatible trailer from `LwsTrailerRegistryService`, preferring the active job origin depot.
4. If no compatible trailer exists, spawn one through an eligible `LwsTrailerSpawnPoint`.
5. Store the resulting `LwsTrailerAssignment` on the existing `LwsActiveJob`.

The service does not silently replace a saved assignment with a different available trailer.

## Coupling Validation

Pickup completion requires a real physical coupling event/state from the existing NWH/LWS coupling path. Distance, trigger overlap, and pressing the attach key are not sufficient.

Validation rule for Prompt 021:

```text
attached trailer stable ID == active job assigned trailer stable ID
```

If the wrong trailer is attached, the active job remains `AwaitingTrailerPickup`, the assignment is preserved, and diagnostics report the required trailer ID. Future interchangeable-trailer policy can extend this seam later.

## Job And Gameplay State Transition

Successful correct-trailer coupling updates the existing active job in place:

```text
AwaitingTrailerPickup -> HaulActive
```

The same `StableActiveJobId`, source job definition, origin depot, destination, cargo, reward, and trailer requirement remain intact. The Prompt 019 gameplay state service derives/transitions to `HaulActive`; no second state machine is created.

## Navigation Transition

Before pickup, the service requests an LWS navigation route to the assigned physical trailer position using a semantic pickup destination ID such as `pickup.<trailerId>`.

After correct coupling, the pickup objective is replaced by a destination objective using the active job's existing `DestinationId` and the destination semantic global position from the job catalog.

Compass Navigator Pro remains presentation-only. Prompt 021 does not manipulate Compass vendor internals.

## Save / Load Behavior

`LwsActiveJob` now contains `LwsTrailerAssignment`, so existing active-job semantic persistence stores the assignment through Pixel Crushers. No new save participant or storage path is created.

Save before pickup restores:

- active job
- `AwaitingTrailerPickup`
- assigned trailer stable ID/type
- pickup objective when the trailer can be resolved or deterministically respawned

Save after pickup restores:

- active job
- `HaulActive`
- assigned trailer identity
- destination objective activation after load

If a saved assignment cannot resolve or respawn the same trailer ID, Prompt 021 emits an actionable diagnostic instead of randomly assigning a different trailer.

## Development UI

The existing development UI shows:

- active job status
- required trailer type
- assigned trailer ID/resolution
- attached trailer ID
- pickup validity
- pickup distance
- last pickup-system message

Development buttons call the same runtime service methods:

- `RESOLVE TRAILER`
- `PICKUP GPS`
- `DEST GPS`

No new production HUD canvas is added.

## Tests

EditMode tests cover trailer type matching, stable assignment, duplicate prevention, wrong-trailer rejection, haul transition, registry filtering, identity sync, serialization shape, bootstrap registration, and source authority checks.

Manual Unity driving/physics validation is still required for real NWH coupling, attached-trailer physical stability, and player-facing visual pickup flow.

## Limitations

Prompt 021 does not implement delivery completion, parking scoring, payout, economy, XP, reputation, cargo damage, job failure penalties, physical trailer pickup target art, or production UI polish.

## Prompt 022 Handoff

Prompt 022 receives an active job in `HaulActive`, the assigned trailer stable ID, the active destination ID, the physically attached trailer through NWH/LWS coupling, and destination navigation already activated.