# Prompt 017 Handoff - Mid-Route Save / Resume

Prompt 017 is now implemented on top of the Prompt 016 Pixel Crushers foundation. This file remains as the historical handoff into Prompt 017 and as a quick reference for the active production save authority.

## Current Authority

- Save framework authority: Pixel Crushers Save System.
- PC backend: `PixelCrushers.DiskSavedGameDataStorer`.
- Serializer: `PixelCrushers.JsonDataSerializer`.
- LWS facade: `ILwsSaveService` / `LwsSaveService`.
- Vendor adapter: `LwsPixelCrushersSaveAdapter`.
- Pixel bridge: `LwsPixelCrushersSemanticSaver`.

No production `ILwsSaveStorage`, `LwsPcSaveStorage`, `LwsInMemorySaveStorage`, proof slot, or custom LWS serializer should exist.

## Prompt 017 Additions

- `LwsSaveLoadCoordinator` for pre-read, safe world preparation, origin setup, vendor load application, and presentation rebinding.
- `LwsWorldResumeSaveParticipant` with payload ID `lws.world.resume-context`.
- Autosave slot and hidden backup slot reservations inside each profile namespace.
- Vendor-backed backup copying through `SavedGameDataStorer`, not direct filesystem copying.
- Recovery offer UI in `LwsPersistencePauseMenu`.
- Busy/save/load/pending autosave state through `ILwsSaveService`.

## Manual Validation Still Required

The real acceptance test remains: save while mid-route, quit, restart, select the same profile, load, and confirm the truck resumes at the meaningful world location with exactly one truck/trailer and stationary physics.

Dialogue System variables should use Pixel Crushers `DialogueSystemSaver`; LWS should not duplicate Dialogue System persistence.