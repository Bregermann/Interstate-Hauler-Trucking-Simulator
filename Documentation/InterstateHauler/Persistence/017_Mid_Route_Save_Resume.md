# Prompt 017 - Mid-Route Save Resume

Prompt 017 keeps Pixel Crushers Save System as the single production save framework authority. LWS now coordinates load ordering so a mid-route save is pre-read, validated, prepared, and then applied through Pixel Crushers instead of calling `SaveSystem.LoadFromSlot` against an unprepared world.

## Authority

- Save framework authority: Pixel Crushers Save System.
- Current PC storage backend: `PixelCrushers.DiskSavedGameDataStorer`.
- Current serializer: `PixelCrushers.JsonDataSerializer`.
- Project-facing facade: `ILwsSaveService` / `LwsSaveService`.
- Vendor adapter: `LwsPixelCrushersSaveAdapter`.
- Semantic saver bridge: `LwsPixelCrushersSemanticSaver`.

LWS does not own save files, a slot database, an independent serializer, or physical storage. LWS owns semantic payloads, slot/profile labels, world-resume ordering, UI busy state, and validation.

## Resume Flow

1. Player requests load through `ILwsSaveService`.
2. LWS resolves the profile and player-facing slot to a Pixel Crushers vendor slot.
3. `LwsSaveLoadCoordinator.PreReadVendorSlot` asks `LwsPixelCrushersSaveAdapter` to retrieve `SavedGameData` through `SavedGameDataStorer.RetrieveSavedGameData`.
4. The adapter reads the LWS semantic snapshot record without applying gameplay state.
5. The coordinator validates schema, profile ownership, stable world ID, target scene, and saved global position.
6. Driving input is neutralized.
7. The target authored scene is resolved from `LwsWorldResumeCatalog`, not arbitrary saved text.
8. The target scene is loaded if required.
9. `ILwsWorldOriginService.SetOriginOffset` establishes the floating-origin offset before player placement.
10. Current world preparation runs for authored scenes and defers streaming-specific work to the existing world streaming service when a manifest is active.
11. Only after the world is prepared does LWS invoke Pixel Crushers load application through `SaveSystem.LoadFromSlot`.
12. LWS finalizes the canonical truck, trailer, navigation presentation, physics sync, and camera/presentation rebinding.

## Current World Support

Current stable world bindings are:

| Stable World ID | Scene | Scene Path |
|---|---|---|
| `world.interstate-corridor-validation` | `InterstateCorridorValidation` | `Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity` |
| `world.truck-validation` | `TruckValidation` | `Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity` |
| `world.streaming-highway-validation` | `StreamingHighwayValidation` | `Assets/LWS/InterstateHauler/World/Streaming/Validation/StreamingHighwayValidation.unity` |
| `world.fifty-mile-floating-origin-validation` | `IH_50MileFloatingOriginValidation` | `Assets/LWS/InterstateHauler/World/Origin/Validation/IH_50MileFloatingOriginValidation.unity` |

`InterstateCorridorValidation` is the primary current validation scene. The experimental 50-mile highway was not made mandatory for production persistence.

## Truck Resume

The existing `LwsPlayerTruckSavePayload` remains the player truck payload. Prompt 017 extends it with double-precision global position fields and restores the truck by converting saved global position through the current origin service. Saved linear/angular velocity remains diagnostic data only; runtime resume zeros Rigidbody linear and angular velocity so the truck loads stationary.

The coordinator and truck participant both guard against duplicate canonical player trucks. NWH remains the vehicle physics authority, and the Prompt 006 transmission participant remains the transmission authority.

## Trailer Resume

Prompt 017 uses existing trailer data inside `LwsPlayerTruckSavePayload` and the existing `LwsNwhTrailerCouplingAdapter` state. It does not create a second trailer persistence model. On restore, the intended existing trailer identity is found, its pose is restored if available, unsafe velocities are zeroed, and physics is synced.

## Semantic Providers

Prompt 017 continues to use Prompt 016 providers:

- `lws.world.global-position`
- `lws.vehicle.player-truck`
- `vehicle.transmission.player`
- `lws.game-clock`
- `lws.weather.semantic`
- `lws.road-condition.semantic`
- `lws.navigation.destination-intent`
- `lws.semantic-state`
- `lws.profile-directory`

Prompt 017 adds `lws.world.resume-context`, a semantic payload used only to determine which world and origin must be prepared before Pixel Crushers applies the save.

## Manual Validation Required

Source and assembly validation can prove architecture and compilation. The actual acceptance test remains manual/standalone: save mid-route, quit, restart, select profile, load, and confirm the truck resumes at the correct meaningful world position without duplicates or unsafe velocity.