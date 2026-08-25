# Prompt 017 - World Resume Context

`LwsWorldResumeSaveParticipant` writes the semantic resume payload `lws.world.resume-context` into the LWS semantic snapshot. This payload is only used to prepare the right world before Pixel Crushers applies saved gameplay state.

## Payload

`LwsWorldResumeContextPayload` records:

- `schemaVersion`
- `stableWorldId`
- `authoredSceneName`
- `sceneBuildIndex`
- `savedGlobalX`, `savedGlobalY`, `savedGlobalZ`
- local diagnostic position
- saved rotation
- saved heading
- origin version and shift count diagnostics

The stable world ID is resolved through `LwsWorldResumeCatalog`. A saved arbitrary scene string is not trusted as the sole runtime load key.

## Floating Origin

Resume uses global coordinates as the canonical location:

`local position = saved global position - current origin offset`

The coordinator first calls `ILwsWorldOriginService.SetOriginOffset`, then converts global-to-local once and applies the player truck pose. Development logging records saved global, origin offset, calculated local, reconstructed global, and error distance so double-offset regressions are visible.

## Authored Scene Policy

Current supported authored scenes are explicitly mapped in source. `InterstateCorridorValidation` is the primary current validation scene. The 50-mile floating-origin test remains a validation tool and is not required for production save/load acceptance.

## Persistence Boundary

The world resume participant does not own storage, write files, load scenes by itself, or serialize outside Pixel Crushers. It is an LWS semantic provider consumed by the load coordinator.