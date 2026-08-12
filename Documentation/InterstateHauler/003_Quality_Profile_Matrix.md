# Interstate Hauler - Quality Profile Matrix

Prompt: 003 - URP / Quality / Platform Foundation  
Date: 2026-08-11

## 1. Unity Quality Tiers

Project quality tiers were reduced to the project-owned render profile set:

| Tier | URP Asset | Render Scale | HDR | MSAA | Shadow Distance | Main/Add Shadow Resolution | Cascades | Target Intent |
| --- | --- | ---: | --- | ---: | ---: | --- | ---: | --- |
| Ultra | `Assets/LWS/InterstateHauler/Rendering/Quality/IH_Ultra_RPAsset.asset` | 1.00 | On | 4x | 120 | 4096 / 4096 | 4 | High-end PC visual ceiling. |
| High | `Assets/LWS/InterstateHauler/Rendering/Quality/IH_High_RPAsset.asset` | 1.00 | On | 2x | 80 | 2048 / 2048 | 4 | Recommended desktop baseline. |
| Medium | `Assets/LWS/InterstateHauler/Rendering/Quality/IH_Medium_RPAsset.asset` | 0.90 | On | 1x | 55 | 2048 / 1024 | 2 | Main scalability tier. |
| Low | `Assets/LWS/InterstateHauler/Rendering/Quality/IH_Low_RPAsset.asset` | 0.80 | Off | 1x | 32 | 1024 / 512 | 2 | Lower-end fallback. |
| Steam Deck | `Assets/LWS/InterstateHauler/Rendering/Quality/IH_SteamDeck_RPAsset.asset` | 0.85 | Off | 1x | 40 | 1024 / 1024 | 2 | 1280x800 handheld target, 30 FPS intent. |

`ProjectSettings/QualitySettings.asset` sets `m_CurrentQuality` to High and Standalone default quality to High.

## 2. LWS Rendering Settings

Project-owned data asset:

`Assets/LWS/InterstateHauler/Rendering/Data/IH_RenderingSettings.asset`

| Tier | Mirror Quality | Mirror Resolution | Mirror Update | Weather Quality | Vegetation Quality | Vegetation Multiplier | Target FPS |
| --- | --- | ---: | ---: | --- | --- | ---: | ---: |
| Ultra | Ultra | 2048 | Every frame | Ultra | Ultra | 1.35 | Uncapped |
| High | High | 1536 | Every frame | High | High | 1.00 | 60 |
| Medium | Medium | 1024 | Every 2 frames | Medium | Medium | 0.75 | 60 |
| Low | Low | 768 | Every 3 frames | Low | Low | 0.50 | 30 |
| Steam Deck | Low | 768 | Every 3 frames | Low | Low | 0.55 | 30 |

## 3. Platform Defaults

| Platform Class | Default Tier | Notes |
| --- | --- | --- |
| Desktop PC | High | Windows and Steam desktop baseline. |
| Steam Deck | Steam Deck | Dedicated handheld profile. |
| Nintendo Switch | Low | Future target placeholder only. |
| Xbox Series | High | Future target placeholder only. |
| PlayStation 5 | High | Future target placeholder only. |

## 4. Quality Policy

Quality tiers reduce expensive visuals before changing gameplay correctness:

- Mirrors remain functional on all tiers.
- Weather visuals scale down before weather gameplay authority changes.
- Vegetation range/density scales down before terrain or road correctness changes.
- Lower tiers reduce HDR, MSAA, shadow resolution, shadow distance, additional light count, and render scale.
- Steam Deck is a dedicated profile, not just the lowest desktop profile.
