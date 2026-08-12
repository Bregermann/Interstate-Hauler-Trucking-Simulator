# Interstate Hauler - Prompt 004 Handoff

Expected Prompt 004: Do not begin from Prompt 003. Use this only as handoff context.

## 1. Current Render Foundation

Prompt 003 activated URP and established the project-owned rendering foundation:

- Active Graphics Settings render pipeline: `Assets/Settings/PC_RPAsset.asset`
- Active renderer: `Assets/Settings/PC_Renderer.asset`
- Color space: Linear
- URP package: 17.4.0
- Quality tiers: Ultra, High, Medium, Low, Steam Deck
- Project-owned rendering settings: `Assets/LWS/InterstateHauler/Rendering/Data/IH_RenderingSettings.asset`
- Project-owned clean default volume profile: `Assets/LWS/InterstateHauler/Rendering/Volume/IH_DefaultVolumeProfile.asset`
- Render validation scene: `Assets/LWS/InterstateHauler/Rendering/Validation/RenderValidation.unity`

## 2. Prompt 002 Gate

The Prompt 002 readiness gate is PASSED by user manual verification in the normal Unity Editor.

The earlier batchmode compiler result is documented as a false-negative/incompatible validation path. Do not attempt to repair vendor NWH, Vista, River Modeler, Unity UI, EventSystems, InputSystem, Physics, IMGUI, Terrain, Audio, or other batchmode-only errors unless the same issue is reproduced in the normal Unity Editor.

## 3. Validation To Complete In Normal Editor

Prompt 004 should start only after, or alongside, normal Editor verification of:

- EditMode tests
- PlayMode tests
- `Interstate Hauler / Validate Project`
- `Assets/LWS/InterstateHauler/Bootstrap/Bootstrap.unity` Play Mode
- `Assets/LWS/InterstateHauler/Rendering/Validation/RenderValidation.unity`

Batchmode validation is not authoritative for vendor compatibility in this project state.

## 4. Known Render Risks

Carry these forward:

- Weather Maker URP support exists and `UNITY_URP` is defined, but the active renderer still needs normal Editor verification for Weather Maker render-feature sync.
- Weatherade URP support packages exist but were not imported. `USING_URP` was intentionally not enabled.
- EasyRoads has URP 17.2 support packages but no exact URP 17.4 support package was found.
- Compass, Heat, Pixel Crushers UI, River Modeler, and VFX Graph need representative visual validation under the activated URP setup.
- Steam Deck profile is configured for 1280x800 / 30 FPS intent but is not performance-proven.
- Aspect-ratio checks are defined for 16:9, 16:10, 21:9, and 32:9, but final HUD/cockpit/mirror UI is not present yet.

## 5. Ownership Boundaries

Do not modify vendor source, vendor asmdefs, package manifests, or vendor dependencies to satisfy command-line batchmode errors.

Continue adding project-owned code under:

`Assets/LWS/InterstateHauler/`

Keep vendor integrations behind project-owned adapters/facades described in the Prompt 002 architecture docs.
