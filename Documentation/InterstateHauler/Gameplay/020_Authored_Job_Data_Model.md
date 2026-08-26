# Prompt 020 - Authored Job Data Model

Interstate: Hauler uses developer-authored jobs as the primary job content model.

## ScriptableObject Types

- `LwsDepotDefinition`: stable depot identity, display name, world ID, job-board availability, enabled flag, optional semantic global position, notes.
- `LwsDestinationDefinition`: stable destination identity, display name, world ID, destination type, enabled flag, optional semantic global position, notes.
- `LwsJobDefinition`: source content for a possible haul.
- `LwsJobCatalog`: runtime-safe reference collection of depot, destination, and job assets.

## Job Definition Fields

`LwsJobDefinition` exposes flat serialized fields suitable for Unity Inspector and Scriptable Sheets:

- `StableJobDefinitionId`
- `DisplayName`
- `Enabled`
- `OriginDepot`
- `Destination`
- `CargoId`
- `CargoDisplayName`
- `FlavorText`
- `RequiredTrailerTypeId`
- `CargoWeightLbs`
- `EstimatedDistanceMiles`
- `QuotedGrossPayCents`
- `DeveloperNotes`

Currency is stored as integer cents. Distance and reward are authored values for Prompt 020; future tooling may calculate/populate them.

## Runtime Concepts

`LwsJobOffer` is derived from a job definition for a specific board listing. `LwsActiveJob` is a persistent player-specific snapshot created at acceptance time. The active job retains `SourceJobDefinitionId` for provenance but copies important authored values so saved careers are stable if source content changes later.

## Current Status Values

- `AwaitingTrailerPickup`: implemented by Prompt 020 and maps to `TRAILER_PICKUP` after acceptance/load.
- `HaulActive`, `Delivery`, `Completed`, `Failed`, `Abandoned`: reserved for future job lifecycle prompts.

Prompt 020 does not implement physical trailer assignment, delivery completion, economy, XP, reputation, procedural jobs, or random job generation.