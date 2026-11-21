# Perfect Culling — confirmed Luna Wolf Studios ownership, November 21, 2026

**Ownership:** explicitly confirmed by the owner in conversation on 2026-11-21.
**Name:** Perfect Culling - Occlusion Culling System
**Unity Asset Store ID:** 193611
**Publisher:** KK Koenigz, Inhb. Patrick König
**Store URL:** https://assetstore.unity.com/packages/tools/utilities/perfect-culling-occlusion-culling-system-193611
**Store details inspected 2026-11-21:** version 1.3.0.4, released 2026-02-11, 13.4 MB; official store labels Built-in/URP/HDRP compatibility dating to original Unity 2019.4.41. Exact **Unity 6.x URP** integration in each production project remains unverified.
**Status:** CONFIRMED OWNED. Download, imported state, licensing seats, current project integration, platform compatibility and performance changes: **NOT VERIFIED**.

## What the asset does, and does not do

- Editor-baked visibility/occlusion system: renderer visibility is sampled ahead of time; the culling data is used at runtime to avoid submitting hidden renderers. Publisher advertises low runtime cost, LOD/transparency support, multiple cameras, prebaked spawned prefabs, multi-scene setups and mobile/VR support.
- **No runtime baking**: dynamically authored/unbaked world geometry cannot just be converted into new occlusion data on the fly. **No 2D sprite support**.
- Separate from **Super Level Optimizer 2 (319098)**: SLO2 can combine meshes/materials and perform other content optimization; Perfect Culling decides which pre-baked renderers should be culled. Using both indiscriminately may undermine culling granularity.
- Compare against **native Unity URP GPU occlusion / GPU Resident Drawer** and Unity's older baked occlusion. Unity announced deprecation of **native baked occlusion in Unity 6.7**; the third-party Perfect Culling system is separate and is **not automatically a supported drop-in replacement**. Verify their mutual behavior before activating multiple culling paths.
- Do not promise an FPS win without matched before/after testing. Open horizons, long highways and exposed fields often have limited occlusion benefit; closed interiors, corners, city blocks, bunkers and tunnels are stronger candidates.

## Project-fit priority

- **Interstate: Hauler / Truck Taxi:** HIGH for interior spaces, populated town blocks, truck stops, warehouses, underpasses/tunnels, hotels and diners. Lower utility in open Virginia highways. Preserve NWH vehicles, traffic, weather/seasonal shader variants, EasyRoads and existing streaming/culling architecture.
- **Truck Sports: Party!** (same Interstate repo): HIGH selectively for solid-walled venues, stadium geometry and dense enclosed tracks; preserve controller-first and Release Foundation sprint priorities.
- **Rowan of Bramblebrook:** HIGH for Bramblebrook buildings, dungeon interiors/Hollow Bell Crypts, forest ruins with substantial occluders; lower for sweeping open forest. Preserve original PS1/PSX aesthetic, existing approved scene geometry and camera.
- **Moving Violation:** Potentially HIGH for urban environments, dense building complexes and occluded interiors; no connected repo identified in this account, so include this in owned-asset-first planning rather than claiming project modification.
- **Horror games:** VERY HIGH for modular Undergrounds 7-in-1 bunkers/shelters, subway tunnels and indoor spaces.
- **Lifeline Run:** Useful in city/industrial missions and tunnels, less applicable to open roads.
- **Jersey Devil/PS1 Universal Importer:** Optional ONLY for independently authored Unity scenes, not archival source geometry or native reconstruction checkpoints; preserve the project's storage and preservation instructions.

## Required Codex evaluation before installing

1. Audit current project Unity/URP versions, rendering path, GPU occlusion settings, already installed culling systems, streaming, camera configuration and relevant vendor optimizers.
2. Build representative **duplicate** scenes (interior/urban/underground and outdoor control) and profile CPU/GPU frame timings, draw calls/rendered meshes, memory/build size and editor bake time **before** changes.
3. Import the vendor asset in isolated Unity 6 URP sandbox; verify shaders, LODs, multi-camera visibility, moving doors/props, prebaked spawned prefabs, streaming transitions, weather changes and save/load.
4. Compare (A) existing project setup, (B) Unity-native occlusion when supported, (C) Perfect Culling, and (D) combined with Super Level Optimizer 2 ONLY if compatible. Keep the fastest **stable** configuration based on actual representative game builds and correctness, not the advertised feature list.
5. Never merge an entire world into one occlusion chunk, treat all dynamic objects as static, assume procedural runtime generation is baked, or touch unrelated project backlog. Maintain exact existing project-specific rules and milestone gates.

**Library source:** `/Luna Wolf Studios/LunaWolf_ConfirmedOwned_2026-11-16_ADDENDUM.xlsx` (updated 2026-11-21; 73 supplement asset rows).
**Explicit exclusions:** The Ultimate Cyber November Humble Bundle remains **NOT PURCHASED**. Sonity, Undergrounds parent + 7 packs, Timeflow and free Cosmos Demonic Village remain independently confirmed owned.

## Public references

- Unity Asset Store: https://assetstore.unity.com/packages/tools/utilities/perfect-culling-occlusion-culling-system-193611
- Unity 6.7 baked occlusion deprecation: https://docs.unity.com/en-us/engine/6000.7/manual/cameras/occlusion-culling
- Unity's November 2026 culling direction: https://discussions.unity.com/t/culling-update-now-and-next/1741459

Ownership record only. Does **not** authorize importing or rewriting game systems immediately.
