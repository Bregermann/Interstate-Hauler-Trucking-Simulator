# Interstate Hauler - Vendor Asset First Rule

This is a mandatory project-wide rule for Interstate: Hauler work. It applies to every major prompt and to future feature implementation unless the user explicitly overrides it.

## Core Rule

Before writing custom functionality, audit the installed commercial assets and use the appropriate vendor solution wherever it provides the requested capability.

The default question for every implementation is:

`Which installed asset already solves this?`

LWS code should primarily provide game-specific semantics, adapters, vendor coordination, state ownership, and functionality genuinely missing from the installed assets.

## Mandatory First Step

Before implementing a requested feature:

1. Audit the relevant installed assets.
2. Read their documentation, README files, demo scenes, sample prefabs, public APIs, inspectors, editor tooling, and runtime components where available.
3. Determine whether one or more installed assets already provide the complete feature, most of the feature, an appropriate foundation, or editor tooling that should be reused.
4. Use those assets aggressively where appropriate.
5. Only write custom LWS code for glue, adapters, semantic game logic, cross-vendor coordination, project-specific behavior, or functionality genuinely missing from the asset.

If a relevant installed asset is not used, explain exactly why before creating a custom replacement.

## Current Major Assets

| Asset | Primary Responsibility |
| --- | --- |
| NWH Vehicle Physics 2 | Player tractor physics, powertrain, wheels, suspension, drivetrain, vehicle behavior, trailer physics/coupling where applicable |
| NWH / wheel-controller integrations | Vehicle controls where applicable |
| Unity-DirectInput / imDanoush | Windows wheel, pedals, shifter input, force feedback backend |
| EasyRoads 3D | Production road authoring, road geometry, road editor tooling |
| UTS Full Pack | NPC traffic, traffic AI, traffic paths, traffic lights/intersections where supported, vehicle spawning/behavior |
| Compass Navigator Pro 4 | Navigation presentation, compass, POIs, route/navigation display, NavMesh route capability where appropriate |
| Weather Maker | Sky, clouds, sun/moon, time-of-day presentation, precipitation, fog, storms, atmospheric weather visuals |
| Weatherade | Wet/snow-covered surface presentation and accumulation visuals |
| Scene Streamer | Additive/async world scene streaming mechanism |
| Pixel Crushers Save System | Save-state framework, save/load orchestration, persistence framework |
| Pixel Crushers Dialogue System | Dialogue, narrative variables, conversation architecture, future cab-life hooks |
| Heat / installed UI framework/assets | Production-quality menus, HUD, and UI where appropriate |
| Vista and terrain/world tools | Terrain generation and world presentation where appropriate |
| Spline Spawner | Roadside object generation and spline-based world dressing where appropriate |
| DOTween | UI/presentation interpolation and animation where appropriate |

## Authority Rules

Using vendor assets does not mean letting multiple plugins compete for the same responsibility. Maintain one authority per responsibility.

| Responsibility | Authority |
| --- | --- |
| Player vehicle physics | NWH |
| NPC traffic behavior | UTS |
| Game world / road semantics | LWS |
| Road authoring | EasyRoads plus LWS world metadata |
| Navigation / route semantics | LWS road graph/navigation service |
| Navigation presentation | Compass Navigator Pro 4 where supported |
| Weather semantics | LWS |
| Weather visual presentation | Weather Maker |
| Road accumulation visuals | Weatherade |
| World streaming semantics | LWS |
| Scene streaming mechanism | Scene Streamer |
| Save-state framework | Pixel Crushers Save System |
| Game-specific save semantics | LWS providers/adapters |
| Dialogue framework | Pixel Crushers Dialogue System |

## Adapter Pattern

Vendor dependencies should generally be localized behind LWS adapters:

```text
Gameplay
    ->
LWS semantic service
    ->
LWS vendor adapter
    ->
Commercial asset
```

Examples:

- `ILwsNavigationService` / navigation state -> `LwsCompassNavigatorProAdapter` -> Compass Navigator Pro 4
- `ILwsSaveService` / save participants -> Pixel Crushers Save System adapter -> Pixel Crushers Save System
- `ILwsTrafficService` / traffic semantics -> UTS adapter -> UTS

## Vendor Source Rule

Do not modify vendor source by default.

Prefer:

- public APIs
- prefab variants
- wrappers
- adapters
- configuration
- subclassing where vendor supports it
- project-owned editor tooling

Only modify vendor source if the imported package itself has a reproducible compatibility defect, no supported project-side solution exists, the modification is minimal, and the modification is documented explicitly.

## Demo-First Rule

If an asset's functionality is unfamiliar, open and inspect the vendor demo before guessing. If the vendor has already built a working example of GPS, traffic lights, weather, saving, road generation, UI, terrain, or POIs, integrate the supported pattern instead of building a replacement.

## No Fake Substitute UI

If the project has a purchased asset specifically intended to display a feature, use its presentation where appropriate. Temporary diagnostic UI is acceptable only for development diagnostics and is not an acceptable substitute for an installed production asset.

## Manual Authoring Rule

For systems the user intends to author manually, build tooling around that workflow rather than procedurally inventing final content.

This is especially important for roads. The user intends to manually design important road layouts. Use EasyRoads plus LWS road metadata and bake/generator tooling to derive the physical roadway, road graph, GPS/navigation data, UTS lane/path data, streaming chunk metadata, and roadside placement.

Do not randomly invent final American highway geometry.

## World Generation Rule

When later world-generation work creates the America pipeline, use the installed world assets aggressively. The generator should coordinate EasyRoads, Vista/terrain tooling, Spline Spawner, UTS, Compass Navigator Pro/LWS navigation presentation, Weather Maker, Weatherade, and Scene Streamer through LWS world metadata and adapters.

## Implementation Decision Rule

If an installed asset provides approximately 70 percent or more of the required feature, use the asset and write the remaining project-specific functionality around it.

Do not discard the installed asset's majority solution and recreate the whole system in LWS.

## Before Creating A Custom System

Before introducing any substantial custom manager, renderer, physics system, traffic system, weather system, save framework, road generator, navigation presentation, terrain generator, or UI framework, Codex must state:

1. Which installed assets were audited.
2. Why those assets cannot provide the required capability.
3. What exact missing capability requires custom code.

If this cannot be justified, do not create the custom system.

## Final Report Requirement

Every major prompt must end with these sections:

- `VENDOR ASSETS AUDITED`
- `VENDOR ASSETS USED`
- `RELEVANT ASSETS NOT USED`
- `CUSTOM SYSTEMS CREATED`
- `VENDOR SOURCE MODIFIED`
- `DUPLICATE VENDOR FUNCTIONALITY CREATED`

For each used asset, report exact path/version if available, concrete classes, prefabs, components, and APIs used.

`VENDOR SOURCE MODIFIED` should be `NO` unless explicitly documented.

`DUPLICATE VENDOR FUNCTIONALITY CREATED` should be `NO`.

## Acceptance Rule

A feature is not complete if an inferior custom replacement quietly recreates functionality already provided by an installed production asset.

Use the assets that are in the project.
