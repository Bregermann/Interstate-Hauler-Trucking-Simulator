# Interstate Hauler Codex Rules

## Truck Sports active requirements

For Truck Sports work, read `TRUCK_SPORTS_ACCEPTANCE_LEDGER.md` first. It is the permanent authority for the currently authorized sprint. Add new requests under permanent IDs, record implementation, automated evidence and human verification separately, and retain superseded requirements. Historical backlogs are archived in `Documentation/TruckSports/Archive/2026-10-20-LegacyBacklog`; do not automatically reopen them or import their reminders into active work.

## Vendor Asset First

Before writing custom functionality, audit relevant installed commercial assets and use the vendor solution wherever it provides the requested capability or a strong foundation.

Start from `Documentation/InterstateHauler/000_Vendor_Asset_First_Rule.md`.

Do not reinvent NWH vehicle physics, Unity-DirectInput/imDanoush wheel input, EasyRoads road authoring, UTS traffic, Compass Navigator Pro GPS presentation, Weather Maker weather visuals, Weatherade road accumulation visuals, Scene Streamer streaming, Pixel Crushers Save/Dialogue, Heat production UI, Vista terrain tooling, Spline Spawner roadside distribution, or DOTween presentation animation.

LWS owns Interstate-specific semantics, adapters, coordination, state, and genuinely missing behavior. Maintain one authority per responsibility and keep vendor dependencies behind LWS adapters.

Do not modify vendor source by default. Prefer public APIs, prefab variants, configuration, wrappers, adapters, and project-owned tooling.

Every major prompt final report must include:

- `VENDOR ASSETS AUDITED`
- `VENDOR ASSETS USED`
- `RELEVANT ASSETS NOT USED`
- `CUSTOM SYSTEMS CREATED`
- `VENDOR SOURCE MODIFIED`
- `DUPLICATE VENDOR FUNCTIONALITY CREATED`

## Luna Wolf Studios shared owned-asset intelligence (2026-11-19)

Before purchasing, planning, or importing vendor assets, consult `Docs/OwnedAssets/ConfirmedOwnership_2026-11-16.md` (confirmed additions and free Demonic Village) and `Docs/OwnedAssets/2026-11-19_Undergrounds_Acquisition_and_Sonity_Evaluation.md` (Undergrounds bundle purchase **in progress, not yet verified**; Sonity **recommended, not owned**). The previously discussed Ultimate Cyber November Humble Bundle was **not purchased**. These documents are inventory/planning references, not instructions to change project priority or import packages; actual installations and compatibility require validation. Retain existing project-specific requirements and authoritative vendor systems.
