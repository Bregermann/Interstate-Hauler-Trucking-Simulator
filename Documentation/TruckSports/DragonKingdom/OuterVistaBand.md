# Outer Vista scenery candidate

V184 passes actual Unity generation, seam measurements and source preservation. Final coastal rendering and attachment acceptance remain open. Existing accepted physical terrain remains the authority; the outer band does not extend the playable area.

`DragonKingdomOuterVistaAuthoring.CreateCandidate()` reads:

- `-tsDragonOuterSource RoundedIntegrated14`
- `-tsDragonOuterCandidate OuterCoast01` (must be a fresh, short tag)
- `-tsDragonOutput <native evidence folder>`

The explicit two-string overload has the same behavior. No normal Play/build hook invokes it. The Editor must be stopped and isolated, without active terrain or a Vista task. The native fixture saves the ordinary blank test-runner scene before isolation and restores the prior scene setup.

Actual source assets:

- `Assets/TruckDragonKingdom/VisualCandidates/RoundedIntegrated14/Shared Dragon Kingdom.prefab`: four accepted 1600 × 600 × 1600 m inner TerrainData assets, at x−1600/0 and z−800/800, base y−20, resolution513.
- `Assets/TruckDragonKingdom/ReviewCandidates/RoundedVista01/Vista Regional Foundation.prefab`: existing configured Vista manager, rounded regional biome graphs, source seeds/masks/order/blends.
- `Assets/PinwheelStudio/Vista/Personal/Runtime/Scripts/Core/VistaManager.cs`: public `Generate(IEnumerable<ITile>)`, height-populated callbacks and cancellation.
- `Assets/PinwheelStudio/Vista/Personal/Runtime/Scripts/Core/LocalProceduralBiome.cs`: public graph/mask references, anchors, falloff and blend configuration.
- Installed Vista `ConstantHeightNode`, `HeightOutputNode`, `BiomeMaskGraph`, `InputNode`, `CurveNode` and `OutputNode` provide the coastal bed and smooth falloff.

The temporary configured source instance is unpacked in a preview scene. Its four original terrain GameObjects are removed before generation; the original TerrainData assets are never passed to Vista. Existing graph/mask configurations are cloned into the new ignored private candidate folder. Only the full-coverage base biome polygon is extended. Four additional vendor ConstantHeight biomes lower the outermost boundary to y−8 through 600 m smooth falloff. ConstantHeight receives12 m above the actual y−20 terrain base; no custom noise or replacement terrain generator is used.

The exact 4 × 4 grid uses x origins−2400/−1600/0/1600, z origins−1600/−800/800/2400 and row/column sizes800/1600/1600/800, excluding the inner four cells. Vista generates the remaining12 real TerrainData assets through an explicit owned tile list. Every height callback must identify a distinct one of those12 tiles.

LWS then coordinates only new heights: the new160 m inner collar approaches the directly measured existing inner boundary, and shared new edge vertices use a canonical height. All new non-edge samples outside that collar must remain bit-exact to Vista's actual output. No inner heights, holes, splats, meshes, colliders, roads, poses or source assets are modified. All normalized heights must remain finite and inside the actual600 m range. Measured inner/new seams and the y−8 outer boundary must be within0.04 m; no fallback geometry or threshold relaxation is allowed.

The world owner's `DragonKingdomWorldVisualMigration.RepairOuterTerrainAppearance(data, exactTag)` reuses the existing normalized four-layer terrain appearance on the new exact candidate Terrain folder. It verifies height samples remain unchanged. The saved outer-only prefab retains12 actual Terrain/TerrainCollider pairs, no Vista manager/biomes/TerrainTile components. Outer neighbors are linked; attachment and neighbor linking to the accepted inner instance remain a later explicit world-owned step.

For the later16-terrain attachment plan, the new `ValidateCoreAgainstOuter(Terrain[] exactFourInner, GameObject outer)` overload reuses all existing seam/bed/outer checks with an explicit set of four distinct actual core terrains. It verifies the same original origins, dimensions and513resolution. The factory's `ReadInner` and existing prefab overload still require exactly four inner terrains. This API addition and its reflection assertion compile with0diagnostics after V184; it does not regenerate the passed candidate and has not separately run natively yet.

Outputs:

- `Assets/TruckDragonKingdom/ReviewCandidates/OuterCoast01/Outer Vista Band.prefab`
- `Assets/TruckDragonKingdom/ReviewCandidates/OuterCoast01/Terrain/Outer <x> <z>.asset` (12)
- `Assets/TruckDragonKingdom/ReviewCandidates/OuterCoast01/Outer Vista Receipt.json`
- `Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVista/OuterCoast01/` (cloned and configured vendor graphs)
- Native evidence `outer-vista-OuterCoast01.json` and `outer-vista-native-verification.txt`.

Receipt preservation lists retain exact original dependency/meta SHA256 values. Existing candidates are never overwritten: a repeated call rejects before generation, and the native fixture verifies original/output bytes remain exact. Windows long-path reads use the existing extended-path convention.

The native receipt additionally records the real Unity dependency closure and hashes for the saved outer prefab and private graphs. This includes binary TerrainData references through `AssetDatabase.GetDependencies`; the retention tool does not infer those from text. Retain only after the exact native test passes:

`python Tools/TruckSports/PreserveDragonKingdomAuthoring.py <actual-run-tag> --scope outer --candidate OuterCoast01 --receipt-name retained-outer`

The new narrow scope selects only the receipt's exact candidate folder and matching private DerivedVista graph folder, including their own/subfolder metadata. It requires identical asset/native receipts, twelve distinct callbacks, every measured seam/bed gate, current original source hashes and exact Unity output dependency hashes. The prefab must contain twelve real Terrain/TerrainCollider pairs, each referencing its matching new TerrainData GUID twice, and no generation behaviours. Every external output reference must already belong to the preserved source closure; unresolved YAML references, duplicate GUIDs, extra candidate files and path escapes are rejected. Other candidates and parent folders are not swept. Before the first write, the complete batch rejects concurrent ROOT changes and conflicting backups; existing before bytes are retained once under the chosen receipt name. Fourteen focused retention cases and the complete81-case tooling suite pass (2.519 s). These are Python guard tests, not Unity generation evidence.

The first actual retention attempt correctly rejected a logical `Packages/` dependency because it has no direct OS file beneath CLEAN/Packages. The read-only resolver now locates the actual embedded package or `Library/PackageCache/<package-id>@*`, verifies its `package.json` identity, enforces path containment and selects only a file matching the exact native SHA256. Package files never enter the owned copy set. The exact Unity default Terrain lightmap reference (`m_LightmapParameters`, fileID15203, builtin GUID0000000000000000f000000000000000, type0) is recognized without inventing a project metadata file; the same GUID in another field or with another fileID/type remains rejected. A real read-only V184 preflight selects87 owned files and zero package/cache files. Nineteen outer retention cases and the complete96-case tooling suite pass in3.997 s. No native output was regenerated for this resolver repair.

Exact Editor-native filter:

`DragonOuterVistaNativeTests.TwelveRealVistaSceneryTilesPreserveInnerWorldAndMeetEveryMeasuredSeam`

The fixture invokes real Vista generation, verifies12 distinct actual height callbacks, all measured seam/bed samples, source dependencies/meta bytes,12 real colliders, four appearance layers, no runtime authoring authority, cleanup and safe repeat rejection. This is a geometry/provenance gate. It does not claim final landscape quality, no-visible-cut-edge acceptance, terrain-to-water composition, normal-speed performance, physics/gameplay on outer scenery, Windows results or human verification. Parent owns serialized Unity execution and later actual rendered review.

Source and native reflection-test isolated Roslyn compiles both pass with0 diagnostics. Parent executed V184 and reported all three batch cases passed; the retained test XML independently proves this exact outer test passed. This agent performed no Unity launch, CLEAN write, scene/world save, vendor source change, Git or ledger operation.

VENDOR ASSETS AUDITED: installed Vista manager, TerrainTile, LocalProceduralBiome and actual rounded foundation graphs; accepted four-tile inner world and existing terrain appearance adapter.

VENDOR ASSETS USED: Vista graph/biome generation, ConstantHeight and smooth mask nodes, Unity Terrain/TerrainCollider, existing owned normalized terrain appearance.

RELEVANT ASSETS NOT USED: EasyRoads/Digger and runtime streaming are outside this scenery-only task; current accepted roads, caves and playable physics remain untouched.

CUSTOM SYSTEMS CREATED: candidate-only vendor coordination, measured new-collar/seam reconciliation, source preservation receipts and native regression.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none; no custom terrain/noise generator.
