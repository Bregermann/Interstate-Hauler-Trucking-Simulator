# Interstate Hauler - Rendering, Quality, And Platform Foundation

Prompt: 003 - URP / Quality / Platform Foundation  
Date: 2026-08-11  
Unity: 6000.4.10f1  
URP: 17.4.0

## 1. Prompt 002 Verification Record

Prompt 002 is treated as PASSED for Prompt 003.

- The user manually verified Prompt 002 in the normal Unity Editor on 2026-08-11.
- The batchmode compiler run produced a false-negative/incompatible validation result.
- The user reports that the vendor packages compile and function correctly in the normal Unity Editor.
- Batchmode validation is not a hard blocker for this project until the Editor-versus-batchmode discrepancy is investigated separately.
- No vendor source, vendor asmdefs, vendor package manifests, or package versions were changed to satisfy the batchmode result.

The normal Unity Editor is the authoritative validation environment for this prompt.

## 2. Active Render Configuration

URP is now activated through:

- Graphics Settings: `ProjectSettings/GraphicsSettings.asset`
- Active pipeline asset: `Assets/Settings/PC_RPAsset.asset`
- Renderer asset: `Assets/Settings/PC_Renderer.asset`
- Color space: Linear
- Standalone scripting define added: `UNITY_URP`

`USING_URP` was not added for Weatherade because the Weatherade URP support packages were identified but not imported. Adding that define without the package import can expose missing Weatherade URP support types.

## 3. Created Project-Owned Rendering Assets

Prompt 003 created the project-owned rendering foundation under:

`Assets/LWS/InterstateHauler/Rendering/`

Important assets:

| Asset | Purpose |
| --- | --- |
| `Data/IH_RenderingSettings.asset` | Project-owned rendering quality and platform profile data. |
| `Volume/IH_DefaultVolumeProfile.asset` | Clean project-owned default/global volume profile. |
| `Quality/IH_Ultra_RPAsset.asset` | Ultra URP quality asset. |
| `Quality/IH_High_RPAsset.asset` | High URP quality asset. |
| `Quality/IH_Medium_RPAsset.asset` | Medium URP quality asset. |
| `Quality/IH_Low_RPAsset.asset` | Low URP quality asset. |
| `Quality/IH_SteamDeck_RPAsset.asset` | Steam Deck URP quality asset. |
| `Validation/RenderValidation.unity` | Project-owned render validation scene, intentionally excluded from Build Settings. |

Runtime support was added through `LwsRenderingSettings`, `ILwsRenderingService`, and `LwsRenderingService`.

The default LWS bootstrap now registers `ILwsRenderingService` after weather coordination and before streaming/navigation/traffic/world/vehicle services.

## 4. Default Volume Strategy

Prompt 001/002 identified missing script references in:

`Assets/Settings/DefaultVolumeProfile.asset`

The missing entries appear to be stale Unity render-pipeline test/template volume components, including Copy/Paste test components and generic volume support test components. That profile is no longer relied on by the active PC render pipeline.

Prompt 003 created:

`Assets/LWS/InterstateHauler/Rendering/Volume/IH_DefaultVolumeProfile.asset`

The clean LWS profile has no exact `m_Script: {fileID: 0}` references and is assigned to the active PC URP asset. The old profile remains in place for auditability and is treated as a warning if it stays unreferenced.

## 5. Validation Surface

The project-owned validator now checks:

- Active URP pipeline
- URP package lock version
- Linear color space
- Quality-tier pipeline assignment
- Clean LWS volume profile
- Rendering settings asset serialization
- Render validation scene existence
- Weather Maker URP setup markers
- Weatherade URP support package status
- EasyRoads URP support package status
- VFX Graph package status
- River Modeler URP VFX asset presence

New EditMode tests cover the same core rendering configuration data.

New PlayMode coverage verifies that the rendering service is registered and reaches Ready through the bootstrap.

## 6. Steam Deck Foundation

Steam Deck has a dedicated quality tier and rendering profile:

- Unity quality tier: `Steam Deck`
- URP asset: `Assets/LWS/InterstateHauler/Rendering/Quality/IH_SteamDeck_RPAsset.asset`
- Render scale: 0.85
- HDR: off
- MSAA: 1x
- Shadow distance: 40
- Shadow resolution target: 1024
- Target intent: 1280x800, 30 FPS, mirrors functional but reduced

This is a foundation profile only. It is not performance-proven until tested on Steam Deck or a representative 1280x800 handheld target.

## 7. Aspect Ratio Coverage

Prompt 003 established the validation targets:

| Aspect | Resolution | Status |
| --- | ---: | --- |
| 16:9 | 1920x1080 | Requires normal Editor visual pass. |
| 16:10 | 1280x800 | Requires normal Editor visual pass; Steam Deck target. |
| 21:9 | 3440x1440 | Requires normal Editor visual pass. |
| 32:9 | 5120x1440 | Requires normal Editor visual pass. |

The current scene does not include final HUD, mirrors, cockpit, compass, or menu composition, so aspect-ratio results are a foundation checklist rather than a final visual certification.

## 8. Normal Editor Validation To Run

Use the normal Unity Editor, not the known-faulty batchmode path, to run:

1. EditMode tests, including `LwsRenderingEditModeTests`.
2. PlayMode tests, including `LwsBootstrapPlayModeTests.RenderingCoordinatorInitializes`.
3. `Interstate Hauler / Validate Project`.
4. Open `Assets/LWS/InterstateHauler/Bootstrap/Bootstrap.unity` and enter Play Mode.
5. Open `Assets/LWS/InterstateHauler/Rendering/Validation/RenderValidation.unity` and check the aspect ratios listed above.

Warnings around Weather Maker render-feature sync, Weatherade URP import, and EasyRoads exact URP 17.4 support are expected until each vendor path is visually validated or explicitly configured in the normal Editor.
