# Prompt 020 - Job Content Authoring Architecture

## Content Policy

Primary job content is developer-authored. Runtime systems consume authored `ScriptableObject` assets and do not procedurally generate cargo, routes, flavor text, rewards, or destinations.

## Scriptable Sheets

Installed package: `com.lunawolfstudios.scriptablesheets`

Version: `1.11.0`

Root: `Packages/com.lunawolfstudios.scriptablesheets`

Scriptable Sheets is an Editor-only package opened through `Window -> Scriptable Sheets`. Package metadata documents ScriptableObject table editing, batch creation, copy/paste, CSV/TSV import/export, JSON export, object references, and searchable/filterable columns.

Prompt 020 shapes depot, destination, job, and catalog assets as normal serialized ScriptableObjects so Scriptable Sheets can become the high-volume authoring UI in Prompt 023. Prompt 020 does not build a custom spreadsheet editor.

## Current Asset Locations

- Depots: `Assets/LWS/InterstateHauler/Data/Depots/`
- Destinations: `Assets/LWS/InterstateHauler/Data/Destinations/`
- Jobs: `Assets/LWS/InterstateHauler/Data/Jobs/`
- Runtime validation catalog: `Assets/Resources/InterstateHauler/Gameplay/IH_JobCatalog_Validation.asset`

A job becomes a runtime offer when it is enabled, valid, present in the runtime catalog, and its `OriginDepot` matches the current depot.

## Vendor UI

Heat - Complete Modern UI was audited as the preferred UI foundation. Prompt 020 uses standard Unity UI fallback because no existing Heat job-board window/presenter asset was configured and wiring one would be broader UI work. Heat remains preferred for a later production presentation pass.