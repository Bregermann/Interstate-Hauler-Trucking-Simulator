# Priority 28: vendor dressing checkpoint

Source only: no Unity/build/tests, assets/scenes generated, YAML edits or commits. Parent owns compilation/import/meta files and execution.

## Corrected vendor discovery

Installed package: Packages/xyz.staggart-creations.spline-spawner. package.json declares 1.2.0; changelog also lists 1.2.1 fixes. Runtime assembly sc.splines.spawning.runtime. The earlier Assets-only search missed this embedded package; no installation is needed.

## Parent commands after compile

Existing TruckTaxiWorldGeometryAuthoring now has menu commands under Truck Taxi/World Dressing:

1. Create Missing Context Profiles (public CreateMissingDressingProfiles): creates missing assets under Assets/LWS/TruckTaxi/Authoring/Dressing for Forest, Suburb, Urban, Industrial, Highway, MotelDistrict, TruckStop, Construction. Existing assets retained. Empty pools intentionally fail validation; no guessed content.
2. Bind Selected Existing Vendor Spawner: select an existing vendor spawner in an editable Taxi scene. Adds/reuses binding, assigns stable ID once, disables automatic generation. No spawning or scene save.
3. Inspector: assign actual profile/prefab assets, existing spline, graph, all parking bays/entrance approaches and protected BoxCollider volumes. Confirm exclusionScopeReviewed. Output root must be the same unscaled GameObject with only this vendor's owned containers. Duplicated binding IDs fail loaded-scope validation.
4. Validate Selected Binding: no changes/generation. Designer remains responsible for complete exclusion references, including unloaded neighboring content.
5. Generate Selected Binding Explicitly: confirmation required; calls vendor Respawn for the selected dedicated output only. Scene remains unsaved. Review before explicitly saving or discarding. Full vendor-generated-object Undo is NOT verified; do not rely on it as sole recovery.

No commands run automatically on import. Vendor startup and automatic triggers remain disabled; spawnObjects is true only inside explicit generation and resets in finally. Existing vendor modifiers and masks are retained. DistributionSettings is profile-owned and replaced from authored profile values.

## Authority and exclusions

Vendor alone distributes, modifies and instantiates candidates and owns containers. LWS provides context/weighted prefab references/spacing and a deterministic integer-hashed profile-seed + stable-binding-ID seed, never Unity Random or per-launch GUIDs.

The public onAfterModifiers callback marks overlapping final SpawnPoints invalid before instantiation. It never moves, retries, rerandomizes or creates candidates. Footprints include renderers and supported Box/Sphere/Capsule/Mesh solid colliders in final position/rotation/scale. Existing invalid candidates stay invalid. Unsupported solid collider types and invalid inputs fail closed.

Road segment AABBs derive from graph samples, lane widths/counts, shoulders/median and vertical clearance. They are conservative, not a drive test. Bays use 4x4.5x12m envelopes; entrances use 2x2.5x3m envelopes. Supply larger protected volumes for long vehicles, turning paths, doorway sweeps and special geometry. Missing anchors/graph, foreign output children, malformed geometry or absent pools fails. Clearance completeness cannot be inferred from loaded objects.

## Content and validation blockers

Final checkpoint guards also reject empty road graphs and vendor early returns that never execute the final exclusion callback. Disabled bindings do not resubscribe from OnValidate. These guards remain uncompiled/unexecuted here.

Narrow existing Taxi authoring references contained vehicles, passengers, UI and weather, not an approved static roadside pool. None were misused. Assign approved scenery to all eight profiles; vendor demo assets are examples, not automatically final Taxi content.

Run TruckTaxiDressingTests (seed repeatability/RNG isolation, eight contexts, empty-pool refusal, footprint/padding, post-modifier pose, invalid geometry, exclusion ordering). Then generate twice on explicitly authorized dedicated content and compare prefab identities + poses/scales, not instance IDs. Vendor version, spline/pool ordering, modifiers, seeds and exclusions must be unchanged. Actual vendor output determinism, masks, Undo behavior and scene-reload persistence remain untested here.

Parent-reported EmptyRendererDoesNotCountAsReferencedArt failure was fixed using Unity filter != null before sharedMesh. Other appearance/world tests were parent-reported passing; this new dressing code has not been compiled/run here.

## Files

- Dressing/LWS.TruckTaxi.Dressing.asmdef, TruckTaxiDressingProfile.cs, TruckTaxiDressingBinding.cs: isolated vendor adapter assembly. Core runtime asmdef unchanged.
- Editor/TruckTaxiWorldGeometryAuthoring.cs (partial declaration) and TruckTaxiWorldGeometryAuthoring.Dressing.cs: existing authoring extension.
- Editor/LWS.TruckTaxi.Editor.asmdef and Tests/EditMode/LWS.TruckTaxi.Tests.EditMode.asmdef: adapter references.
- Tests/EditMode/TruckTaxiDressingTests.cs and this handoff.
- Editor/TruckTaxiServiceMapValidator.cs: Unity fake-null fix.

## Vendor report

- VENDOR ASSETS AUDITED: embedded Spline Spawner API, README, changelog, demo configuration; existing Taxi prefab references.
- VENDOR ASSETS USED: SplineSpawner, SpawnableObject, DistributionSettings.SetSeed, onAfterModifiers, SpawnPoint.isValid, SplineInstanceContainer and Respawn.
- RELEVANT ASSETS NOT USED: no guessed prefab pools; no EasyRoads/Vista/world regeneration.
- CUSTOM SYSTEMS CREATED: authored profile/binding, safety exclusion filter and editor entry extensions only.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
