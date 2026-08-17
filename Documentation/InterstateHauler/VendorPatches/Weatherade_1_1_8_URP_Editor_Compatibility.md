# Weatherade 1.1.8 URP Editor Compatibility

Date: 2026-08-17

## Summary

The Weatherade SRS URP 17.1 support package was imported into the project, but the project did not yet define Weatherade's own URP scripting symbol, `USING_URP`.

The imported URP editor inspectors call the URP signature of `NL_Utilities.BeginUICategory`:

`BeginUICategory(string headerText, GUIStyle backgroundStyle, GUIStyle foldoutStyle = null, SerializedProperty foldoutVal = null)`

The installed utility file already contains that signature in:

`Assets/NOT_Lonely/Weatherade SRS/Scripts/Extra/NL_Utilities.cs`

However, Weatherade gates that signature behind `#if USING_URP`. Without the define, Unity compiled the non-URP signature instead:

`BeginUICategory(string headerText, GUIStyle backgroundStyle, SerializedProperty foldoutVal = null)`

That produced CS1501 errors in the imported URP RainCoverage and SnowCoverage editors.

## Fix Applied

No Weatherade source compatibility shim was added.

The project now enables the vendor-intended Weatherade URP symbols for Standalone builds in:

`ProjectSettings/ProjectSettings.asset`

Added symbols:

- `WEATHERADE_INCLUDED`
- `USING_URP`

The imported Weatherade URP renderer data is also registered as the secondary renderer on the active PC and LWS quality URP assets while preserving the normal project renderer as index 0:

- `Assets/Settings/PC_RPAsset.asset`
- `Assets/LWS/InterstateHauler/Rendering/Quality/IH_Ultra_RPAsset.asset`
- `Assets/LWS/InterstateHauler/Rendering/Quality/IH_High_RPAsset.asset`
- `Assets/LWS/InterstateHauler/Rendering/Quality/IH_Medium_RPAsset.asset`
- `Assets/LWS/InterstateHauler/Rendering/Quality/IH_Low_RPAsset.asset`
- `Assets/LWS/InterstateHauler/Rendering/Quality/IH_SteamDeck_RPAsset.asset`

Renderer index map for those assets:

- `0`: `Assets/Settings/PC_Renderer.asset`
- `1`: `Assets/NOT_Lonely/Weatherade SRS/Resources/SRS_DepthRenderer.asset`

## Vendor Files Modified

No Weatherade source files were manually edited for this compatibility repair.

The commit does include the Weatherade URP support import files that were already present in the working tree as vendor import output. Those are imported vendor assets, not hand-authored LWS modifications.

## Validation Notes

The previous compile failure was:

- `RainCoverage_editor.cs`: CS1501, `BeginUICategory` four-argument calls
- `SnowCoverage_editor.cs`: CS1501, `BeginUICategory` four-argument calls

After this configuration change, those calls resolve through Weatherade's existing `USING_URP` utility signature.

If the Unity Editor was already open with the old define set cached, restart or allow a full domain reload so the updated Standalone scripting defines are applied.
