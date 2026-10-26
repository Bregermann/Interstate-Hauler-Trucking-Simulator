# Urban road snow — Q22-SKY-SNOW-SURFACE-01

Focused Q22 snow-surface integration for Fare Fight and Truck Snake. This does not finish the entire Q22/Carnival0.6/Impossible0.7 queue. Human playtesting, physical devices, listening, whole-world snow coverage, all split-screen combinations and isolated performance acceptance are separate.

## Cause and implementation

The saved scenes left `SportsSnowSurface.traceExposedRoads` off and supplied ordinary Lit materials for their existing road renderers. The installed Weatherade bridge reported snow coverage around.79 but zero compatible road materials and zero road depth sources. The previous Windows rendering visibly showed bare roads; Fare also retained green surroundings. Passing forecast/time checks did not establish snow-surface acceptance.

`SportsSnowAuthoring.InstallUrbanRoadSnow` explicitly binds the existing roads using the established Hide/Frogger integration and `LwsWeatheradeMaterialFactory`. It creates three shared sport-owned materials—two for Fare, one for Snake—retaining source color, texture, tiling and offsets. The original runtime adapter then owns shared runtime copies, accumulation, clear-weather shader restoration, actual NWH wheel contact, Weatherade depth sources, GPU trace masks and reset. No new runtime system or vendor replacement was written.

Only106 existing renderer material arrays and two original road-trace flags change in the scenes. All30,267 original serialized records remain. There are no geometry, collider, transform, navigation, lamp, vehicle, input, scoring or gameplay changes. Original Taxi/source material files remain untouched. Builds and entering Play Mode never invoke this authoring method. Existing customized snow materials are retained on repeated explicit migration.

Unity's native scene save also emitted unrelated null/default fields on six existing components in each scene. Strict review rejected that export. The raw saved scenes are retained; a reviewed extraction transfers only material arrays and the two trace flags into the original scene documents. The final strict comparison passes without those unrelated additions.

## Actual validation

- Baseline diagnostic: Editor42144 exited0/60.45s, one diagnostic execution passed/20.4820605s. This was a state/material audit, not acceptance. Both original720p images inspected and retained.
- Explicit additive authoring: Editor1708 exited0/40.9s.46Fare road renderers share2new materials;60Snake road renderers share1. No scene rebuilding.
- Production native tests: Editor38836 exited0/166.59s;2PlayMode tests passed/107.8177046s. Each saved scene runs Clear→Snow→Clear→Snow with original autonomous AI physics. Source material files remain byte-identical after the rounds.
- Both scenes observed16physical road-wheel contacts. GPU readback showed Fare431/495active trace pixels and365/414indented pixels; Snake439/433active and361/366indented pixels. These are real vendor texture pixels, not inferred component state or injected scores.
- Frozen-pose comparison disabling only runtime road coverage: Fare34792/28266brighter pixels and Snake27544/29252out of57600. Both actual snow rounds affect the rendered road image. Vendor depth sources46/60 and runtime shared-material counts2/1 remain bounded.
- All16native720p/1080p captures individually inspected. Snow-covered roads and visible track grooves persist across rematches; clear rounds restore roads. Fare's captured exposed ground is snow-colored after the original depth/trace integration is active. Existing exaggerated arcade track shading remains; this is visible-area native evidence rather than whole-world acceptance.
- Existing independent weather/time preference contracts: Editor40212 exited0/27.08s;3EditMode tests passed/.1309124s. No runtime source changed in this slice.
- Windows x64 build41812 exited0/334.32s; Unity summary290.8856124s,0errors/46warnings,60scenes,548files,433frozen owned C# inputs. Fullcontent2514695426c7071284d7dcead029f7d9bc5a60c1e0b6031ecc4d12be53bbf2fe, managede76acf99ddce5357a5b90668fd0b45d8cc59f711262ee9e357281e98f996913f.
- Windows35360 exited0/98.58s; original nine-case weather/time matrix passed157checks/0errors. All36 actual weather/time720p/1080p captures individually inspected;4setup frames excluded. Snow/Blizzard roads visibly accumulate and retain exposure detail; all other exercised forecasts restore the non-snow road appearance. Fare Rain1080 contains a visible traffic collision/overturned truck; this focused rendering pass does not establish traffic/gameplay acceptance. Whole-world and split-layout snow remain open.

## Delivery and limits

Saved scenes: `Assets/TruckFareFight/Scenes/TruckFareFight.unity`, `Assets/TruckSnake/Scenes/TruckSnake.unity`. Shared owned editor tooling: `Assets/TruckSports/Editor/SportsSnowAuthoring.cs`. New material folders: each sport's `Materials/Urban Road Snow/`. Focused tests: `UrbanSnowSurfaceDiagnosticsTests.cs`, `UrbanRoadSnowIntegrationTests.cs` and their metas. Exact asset/scene hashes and retained-record checks are in the accompanying evidence.

Candidate is `Builds/TruckSportsCamera/TruckSports.exe` with all companion files. Previous full1f91e822 winter-palette candidate is retained before this build. Accepted canonical `Builds/TruckSports/TruckSports.exe` remains60d4716f… until promotion requirements pass. No public push or private integration-HEAD publication is part of this milestone. The separate current-Editor Dock native shutdown blocker remains open.

## VENDOR ASSETS AUDITED

Installed Weatherade SnowCoverage, CoverageBase, snow shader mask/trace controls and existing LWS Weatherade material factory/adapter. Existing Taxi snow bridge and Hide/Frogger integrations were inspected before adding code. Original NWH wheel-contact interfaces retained.

## VENDOR ASSETS USED

Weatherade snow shaders, depth renderer, accumulation and trace generator; existing LWS material factory and scene-local bridge; NWH wheel contacts. Existing Weather Maker forecast and winter palette remain authoritative.

## RELEVANT ASSETS NOT USED

No decal/terrain/snow replacement: the installed coverage and trace systems already provide the capability. Automatic per-renderer material rebinding was not enabled; the established three-shared-material path avoids an unnecessary material per road segment.

## CUSTOM SYSTEMS CREATED

No new runtime system. Explicit additive material authoring extends existing tooling; focused read-only audit and native integration tests added.

## VENDOR SOURCE MODIFIED

None. Original Truck Taxi source and authored material assets were also not modified.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None. Existing Weatherade and adapter responsibilities retained.
