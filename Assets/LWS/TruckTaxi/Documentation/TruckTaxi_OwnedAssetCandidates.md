# Owned Asset Candidates

Unity CLI / unity-package-management skills were checked first. The installed CLI
exposes Editor/UPM tooling but no authenticated My Assets inventory endpoint. Its
OAuth session reports stale. No account tokens were read, no purchases/downloads
were attempted. **FULL MY ASSETS LIBRARY NOT PROGRAMMATICALLY ACCESSIBLE.**

No pre-existing inventory was found in project tooling/docs or the supplied Codex
projects. The configured/default downloaded cache is
`%APPDATA%/Unity/Asset Store-5.x`; `Asset Store/Cache/AssetStoreCache.json` contains
version IDs, not a purchase catalog. `Tools/UnityAssetCatalog/owned_assets.json`
records 53 local packages with discovery provenance, not verified ownership.
The read-only repeatable audit inspects archive path metadata, never mass-imports.

| Candidate / publisher | Source / content | Compatibility / scope / concerns | Decision |
| --- | --- | --- | --- |
| Compass Navigator Pro 4 / Kronnect | Downloaded and imported; POIBeacon, route/marker textures | Existing URP integration; existing GPS retained, no import | USE for map/navigation; pickup eligibility stays LWS-owned |
| Particle Ingredient Pack / GAPH | Downloaded, not imported; ring, energy-field, magic-circle particles | Pipeline compatibility unverified; broad particle/material dependency set, independent effect scaling | SKIP for this precise gameplay radius; lightweight mesh ring is simpler |
| Sci Fi Hologram Shader / Knife Entertainment | Downloaded, not imported; hologram shaders/demo/terrain/materials | Compatibility unverified, extra shader variants; unnecessary to mark a taxi bay | SKIP |
| Character Customizer / Jordbugg | Downloaded, not imported; modular heads/hair/apparel and customization scripts | Full customizer not installed; importing its demo/UI is unnecessary for this pass | MAYBE for final human art; prefab/manual-model adapter accepts authored vendor output |
| UTS Full Pack / AGLOBEX | Downloaded/imported; adult human prefabs, humanoid idle/walk/run/sit/talk clips | Existing URP materials; strip pedestrian movement components on passenger instances | USE existing human models/shared clips |
| Heat / Michsky | Downloaded/imported; buttons, UI patterns | Existing HUD integration | USE; no extra Canvas |
| Pixel Crushers Dialogue / Pixel Crushers | Downloaded/imported; BarkController, IBarkUI, CSV utility | Assembly-CSharp vendor bridge; no vendor asmdef/source changes | USE runtime bark lifecycle and editor CSV import |
| DOTween / Demigiant | Downloaded; project presentation utility | Not needed for fixed-radius procedural ring color/boarding progress | SKIP for this small effect |

Asset Store IDs/versions are left unknown where archive metadata has not proved
them. Current installed vendor versions, not cache filenames, govern integration.

## Selection

Pickup ring/inner area/marker use a small LWS mesh/LineRenderer presentation driven
by the existing RideLocation radius. A generic animated particle pack cannot own
boarding eligibility or guarantee its effect bounds. No replacement navigation,
VFX framework, vendor source changes, or imported-package churn is required.
UTS human models are reusable art; creatures use explicitly reported stylized
fallbacks pending final supplied/generated models. Casting is authored separately.
