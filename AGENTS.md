# Interstate Hauler Codex Rules

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
