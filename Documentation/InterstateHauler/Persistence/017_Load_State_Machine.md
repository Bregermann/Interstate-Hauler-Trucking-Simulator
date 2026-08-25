# Prompt 017 - Load State Machine

`LwsSaveLoadCoordinator` is a transient ordering coordinator. It does not serialize, store files, own vendor slots, or replace Pixel Crushers. It exists so LWS can prepare the world before Pixel Crushers applies saved state.

## Phases

| Phase | Purpose |
|---|---|
| `Idle` | No active load. |
| `ReadingVendorSlot` | Pre-read selected Pixel Crushers slot through `SavedGameDataStorer`. |
| `ValidatingSave` | Validate schema, profile ownership, resume context, and world binding. |
| `PreparingGameplaySuppression` | Neutralize vehicle input so load cannot leave held throttle/steering. |
| `ResolvingTargetWorld` | Resolve stable world ID through `LwsWorldResumeCatalog`. |
| `LoadingTargetWorld` | Load the allowed target authored scene when needed. |
| `EstablishingOrigin` | Set floating-origin offset for the saved global location. |
| `PreparingWorld` | Give current world/streaming services a chance to prepare the area. |
| `WaitingForWorldReady` | Sync transforms and mark the immediate authored-scene world ready. |
| `ApplyingVendorSave` | Invoke Pixel Crushers load application for the selected slot. |
| `RestoringPlayerVehicle` | Finalize exactly one canonical player truck and stationary pose. |
| `RestoringTrailer` | Finalize existing trailer pose/attachment state where supported. |
| `RestoringWorldSemantics` | Sync restored LWS semantic state after vendor application. |
| `RestoringNavigation` | Re-present restored navigation destination/route. |
| `RegeneratingAmbientWorld` | Leave UTS ambient traffic to repopulate; individual NPC cars are not persisted. |
| `FinalizingPhysics` | Run physics transform sync after pose/trailer restoration. |
| `RebindingPresentation` | Rebind camera/navigation/weather presentation to the canonical player. |
| `Complete` | Load completed. |
| `Failed` | Load failed and stays safe for retry/recovery/cancel. |

## Pre-Read Rule

Pre-read may inspect Pixel Crushers `SavedGameData` and deserialize the LWS semantic snapshot for validation. It must not apply truck, weather, navigation, transmission, trailer, or world state. Gameplay application remains Pixel Crushers-owned and occurs only in `ApplyingVendorSave` after world preparation.

## Application Order

`LwsPixelCrushersSemanticSaver.ApplyData` continues to receive a Pixel Crushers serialized snapshot. LWS now applies known semantic participant categories in a deterministic order so world/global context is handled before vehicle, trailer, clock, weather, road condition, and navigation semantics.

## Failure Behavior

A failed load sets `LastFailure`, clears unsafe transient context, keeps input suppressed while appropriate, and can surface a recovery offer if a hidden backup slot validates. Backups load through the same coordinator; there is no separate backup loader.