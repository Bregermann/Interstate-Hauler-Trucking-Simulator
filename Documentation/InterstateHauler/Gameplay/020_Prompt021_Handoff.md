# Prompt 021 Handoff - Trailer Pickup

Prompt 020 ends after an authored job is accepted.

## Prompt 021 Receives

- `ILwsActiveJobService`
- `LwsActiveJob`
- `StableActiveJobId`
- `SourceJobDefinitionId`
- `OriginDepotId`
- `DestinationId`
- `RequiredTrailerTypeId`
- `Status = AwaitingTrailerPickup`
- Current depot presence from `ILwsDepotService`
- Macro gameplay state `TRAILER_PICKUP`
- Active Job persistence through `LwsActiveJobSaveParticipant`
- Existing Prompt 017 autosave request seam

## Prompt 021 Must Not Reinvent

- Job definitions
- Job catalog
- Authored job offer provider
- Job board acceptance
- Active job storage
- Reward/cargo/destination identity

## Prompt 021 Expected Scope

Prompt 021 should assign or resolve the physical trailer for the active job, create pickup target semantics, guide the player to pickup, validate correct coupling, and transition the active job/status toward haul gameplay. It should consume `RequiredTrailerTypeId` from the active job instead of creating a new job model.