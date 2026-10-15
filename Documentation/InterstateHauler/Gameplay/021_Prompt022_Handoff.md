# Prompt 021 -> Prompt 022 Handoff

Prompt 022 owns delivery and parking. It should not recreate job definitions, job board acceptance, active job storage, trailer assignment, or trailer pickup validation.

## Runtime State Prompt 022 Receives

- Active job service: `ILwsActiveJobService`
- Active job model: `LwsActiveJob`
- Stable active job ID: `LwsActiveJob.stableActiveJobId`
- Source authored job ID: `LwsActiveJob.sourceJobDefinitionId`
- Origin depot ID: `LwsActiveJob.originDepotId`
- Destination ID: `LwsActiveJob.destinationId`
- Required trailer type ID: `LwsActiveJob.requiredTrailerTypeId`
- Assigned trailer: `LwsActiveJob.trailerAssignment.stableTrailerId`
- Job status after correct pickup: `LwsJobStatus.HaulActive`
- Macro gameplay state after correct pickup: `LwsGameplayState.HaulActive`
- Attached trailer state: existing `ILwsTrailerCoupling` / `LwsNwhTrailerCouplingAdapter`
- Navigation objective: active job destination route requested through `ILwsNavigationService`
- Autosave seam: existing `ILwsSaveService.RequestAutosave(...)`

## What Prompt 022 Should Reuse

Prompt 022 should consume the existing active job and assigned trailer identity. It should validate delivery against the active job destination and the physically attached assigned trailer.

Expected future flow:

```text
HaulActive
    |
    | player reaches destination/delivery context
    v
Delivery
    |
    | parking/delivery validation succeeds
    v
DeliveryResults
```

## Do Not Recreate

Prompt 022 must not recreate:

- job definitions
- job catalog
- job board
- active job service
- trailer gameplay identity
- trailer assignment service
- NWH coupling adapter
- Pixel Crushers save framework
- autosave manager
- Compass/navigation presentation stack

## Prompt 022 Scope

Prompt 022 should implement destination arrival, delivery/parking validation, delivery state transition, and delivery handoff to future rewards/economy prompts. It should not pay money, award XP, implement reputation, or create procedural jobs unless a later prompt explicitly owns those systems.