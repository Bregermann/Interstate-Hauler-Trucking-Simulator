# Interstate Hauler - Prompt 003 Handoff

Expected Prompt 003: URP / Quality / Platform Foundation

## Current State

- 2026-08-11 user verification update: Prompt 002 was manually verified by the user in the normal Unity Editor and the readiness gate is treated as PASSED.
- The earlier batchmode compiler result is recorded as a false-negative/incompatible validation path for this project state.
- Vendor packages are reported by the user to compile and function correctly in the normal Unity Editor.
- Batchmode validation should not be used as a hard blocker until the Editor-versus-batchmode discrepancy is separately investigated.
- Bootstrap scene exists: `Assets/LWS/InterstateHauler/Bootstrap/Bootstrap.unity`
- Bootstrap is build scene index 0.
- URP package is installed: 17.4.0.
- Intended PC URP asset exists: `Assets/Settings/PC_RPAsset.asset`.
- Graphics Settings still have no active custom render pipeline asset.
- Quality Settings still have no custom render pipeline assets assigned.
- Color space is still Gamma.
- `Assets/Settings/DefaultVolumeProfile.asset` still contains missing script references from Prompt 001.

## Prompt 003 Should Decide

- Whether to assign `PC_RPAsset.asset` as the Standalone render pipeline asset.
- Whether to assign URP assets per quality level.
- Whether to switch color space to Linear.
- Whether to replace or clean `DefaultVolumeProfile.asset`.
- Whether Weather Maker URP setup should be run through vendor menu/tools.
- Whether Weatherade URP support packages should be imported/enabled.
- Whether EasyRoads URP material support is compatible with URP 17.4 or needs manual validation.
- Which quality levels target PC high-end, PC low-end, and Steam Deck.

## Renderer Dependencies To Check

- Weather Maker 8.0.9 expects explicit URP setup for URP projects.
- Weatherade URP code paths need matching support package/defines.
- EasyRoads has URP 17.2.0 and beta 17.3 support packages; project URP is 17.4.0.
- Compass UI/minimap shaders should be checked after URP activation.
- Heat UI, Pixel Crushers UI, and UGUI materials should be visually checked.
- River Modeler and VFX Graph water/weather effects need URP validation.

## Do Not Do In Prompt 003

- Do not build gameplay.
- Do not alter NWH, UTS, Compass, Pixel Crushers, or EasyRoads source code.
- Do not import optional NWH steering-wheel packages.
- Do not implement routing, traffic, economy, jobs, cargo, or 18-speed shifting.

Prompt 003 readiness: READY by user manual verification in the normal Unity Editor. Batchmode compile output is not authoritative for Prompt 003 until investigated separately.
