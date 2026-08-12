# Interstate Hauler - Render Compatibility Matrix

Prompt: 003 - URP / Quality / Platform Foundation  
Date: 2026-08-11  
Validation authority: normal Unity Editor

## 1. Status Legend

| Status | Meaning |
| --- | --- |
| PASS | Static evidence and normal Editor verification show the path is ready. |
| PASS WITH RISK | Static evidence is acceptable, but a vendor visual/runtime check remains. |
| BLOCKED | The vendor path requires a deliberate setup/import/decision before it can be treated as compatible. |
| NOT TESTABLE | No meaningful Prompt 003 validation scene or runtime path exists yet. |

## 2. Compatibility Matrix

| Vendor/System | Status | Evidence | Remaining Work |
| --- | --- | --- | --- |
| NWH Vehicle Physics 2 | PASS WITH RISK | Prompt 002 was manually verified by the user in the normal Editor. Prompt 003 made no NWH source, asmdef, dependency, or package changes. | Verify vehicle paint/glass/lights/mirrors in a real truck scene after URP activation. |
| EasyRoads3D | PASS WITH RISK | EasyRoads SRP support packages exist through URP 17.2.0. No exact URP 17.4 package was found. | Visual road material validation in normal Editor; do not import older support packages blindly. |
| Vista | PASS WITH RISK | Vista wizard code identifies URP and notes Vista is render-pipeline independent, with sample scenes made for URP. Project URP is now active. | Verify generated terrain materials, compute outputs, and biome shading in a Vista-backed scene. |
| Weather Maker | PASS WITH RISK | Weather Maker 8.0.9 includes URP 17+ support, URP profile, URP forward/deferred renderer assets, and URP render-feature source. `UNITY_URP` is defined. | Run/verify Weather Maker's normal Editor URP setup so the active renderer has the Weather Maker render feature where required. |
| Weatherade | BLOCKED | Weatherade URP support packages exist, including the URP 17.1+ package, but they were not imported. `USING_URP` was intentionally not enabled. | Decide in the normal Editor whether to import Weatherade URP support, then enable/verify the correct define and scene materials. |
| Compass Navigator Pro | PASS WITH RISK | Compass includes URP helper scripts and URP-tagged demo shaders. No Compass vendor files were changed. | Verify compass, minimap, scan/fog route rendering in a URP game scene. |
| Heat UI | NOT TESTABLE | Prompt 003 did not create final HUD/menu screens and no Heat-specific render scene was available in the LWS-owned validation scene. | Validate Heat UGUI materials and scaling when the project UI shell exists. |
| Pixel Crushers | NOT TESTABLE | Pixel Crushers UI/dialogue runtime was not instantiated by Prompt 003. Scene Streamer and Dialogue System packages were not modified. | Validate dialogue UI and Scene Streamer transition visuals when those systems are integrated. |
| River Modeler | PASS WITH RISK | River Modeler includes URP VFX assets, including `VFX (URP)/Water Surface Splashes.vfx`. No River Modeler vendor files were changed. | Open a representative river/water scene in normal Editor and verify shaders/VFX under URP 17.4. |
| VFX Graph | PASS WITH RISK | `com.unity.visualeffectgraph` 17.4.0 is present in `Packages/packages-lock.json`. | Validate actual VFX playback with River Modeler and Weather Maker effects in the normal Editor. |

## 3. Batchmode Validation Note

The earlier batchmode compiler result is a known false-negative/incompatible validation result for this project state. It referenced vendor-side missing modules and dependencies that the user reports are not reproducible in the normal Unity Editor.

Do not use batchmode validation as a hard blocker for these vendor packages until the discrepancy is investigated separately.

## 4. Vendor Modification Check

Prompt 003 made no source or asmdef changes under known vendor roots:

- `Assets/NWH`
- `Assets/EasyRoads3D`
- `Assets/UTS_FullPack`
- `Assets/WeatherMaker`
- `Assets/NOT_Lonely`
- `Assets/Plugins/Kronnect`
- `Assets/Plugins/Pixel Crushers`
- `Assets/Plugins/Demigiant`
- `Assets/PinwheelStudio`
- `Assets/RiverModeler`
- `Packages/xyz.staggart-creations.spline-spawner`

Optional vendor setup steps remain normal-Editor validation tasks, not source patches.
