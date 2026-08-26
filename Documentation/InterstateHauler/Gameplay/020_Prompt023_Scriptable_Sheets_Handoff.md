# Prompt 023 Handoff - Scriptable Sheets Job + Destination Authoring Pipeline

Prompt 023 should build the high-volume authoring workflow for the Prompt 020 runtime data model. It must not replace authored jobs with procedural generation.

## Runtime Types To Author

- `LwsDepotDefinition`
- `LwsDestinationDefinition`
- `LwsJobDefinition`
- `LwsJobCatalog`

## Current Serialized Fields

`LwsJobDefinition` exposes enabled/display/origin/destination/cargo/flavor/trailer/weight/distance/reward fields directly. These should appear naturally as useful columns in Scriptable Sheets.

## Scriptable Sheets Installation

- Package: `com.lunawolfstudios.scriptablesheets`
- Display: `LWS Scriptable Sheets`
- Version: `1.11.0`
- Root: `Packages/com.lunawolfstudios.scriptablesheets`
- Open: `Window -> Scriptable Sheets`
- Relevant features from package metadata: ScriptableObject grid editing, batch create, object references, copy/paste, CSV/TSV import/export, JSON export, filtering, sorting, row height controls for multiline text.

## Prompt 023 Goals

- Configure useful Scriptable Sheets views for jobs, destinations, and depots.
- Make large authored job sets easy to create and edit.
- Preserve stable IDs.
- Improve catalog maintenance and validation.
- Expose flavor text, rewards, weights, trailer requirements, origin, and destination references.
- Add CSV/TSV workflows only through actual Scriptable Sheets support.

Prompt 023 must not create random/procedural jobs as the primary content source.