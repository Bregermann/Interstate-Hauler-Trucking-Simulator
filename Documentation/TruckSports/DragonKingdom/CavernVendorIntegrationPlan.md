# Bounded cavern vendor integration

The rejected roof consists of four90m rock clusters rotated sideways. The V102 terrain surface at the cavern center is approximately105.42m, equal to the road/floor elevation. That surface cannot provide an underground roof. A real massif must precede excavation; adding another arbitrary roof-rock row would repeat the original defect.

`DragonKingdomCavernFoundationReview.CreateReviewCandidate()` is source-ready and compiles with zero errors/warnings. It clones the actual V102 setup and its four TerrainData, changes only the existing cavern graph's terminal Add by100/600, and runs real Vista generation. The source maps contain78.83..131.72m over the planned180×120m chamber footprint; a full-strength offset predicts178.83..231.72m, but only the native result is evidence. The helper requires117actual roof samples at least160m, four real height callbacks, unchanged original bytes and outside-biome delta at most.04m for saved-height quantization. Its output is `ReviewCandidates/CavernMassif01`, with one separate graph under ignored `PrivateLicensedEnvironment/DerivedVista/CavernMassif01`. It does not call this a cave or adopt a world.

Installed Digger provides the remaining foundation:

- `Digger.Modules.Core.Editor.DiggerSystemEditor.Init(digger,false)` configures the actual terrain material, voxel/chunk representation and persistence. Before calling it, require the installed `Digger/Terrain/URP/Lit` and `Digger/Mesh/URP/Lit-Pass0` shaders to exist, be supported and compile. Avoid `SetupTerrains()` and force-refresh initialization because those paths can import packages over installed vendor shader files.
- `DiggerMaster.SceneDataFolder` selects a fresh isolated native-generated folder under `Assets/DiggerData/Scenes`. No existing Digger data may be cleared or moved. Include the exact new folder in preservation/retention; vendor documentation warns against moving persisted data.
- `BasicOperation` with real `ModificationParameters` supplies Dig, sphere and rounded-cube brushes. `ModifyWithoutMeshes` followed by `BuildPendingMeshesAsync` permits bounded repeated brush work followed by one real mesh/collider build. `PrepareModification`, `PersistAndRecordUndo`, `SaveMeshesAsAssets` retain the actual authored result. No LWS voxel, CSG or terrain-hole engine is needed.
- The existing Digger spline walker can distribute vendor operations along actual road alignments. Its public walker invokes operations at spline positions and builds pending meshes; LWS supplies route semantics and intended clearances.

The next excavation candidate must retain the original105m road mesh/collider/graph and all starts/extractions. Only the intended underground corridor is exempt from surface-height restoration; flattening a surface corridor after excavation would erase the genuine roof. Two adjacent terrain tiles meet at z800 inside the planned cave, so both must receive the actual vendor cut and their seam must be checked.

Measure the configured dragon's full animated skin, including wings, plus the original full NWH assembly at its calibrated semantic saddle. Use those actual dimensions to choose the chamber/tunnel aperture and sweep it continuously. Do not substitute the motor's small collision radius or the truck center for the full carried combination. The new regional landform also requires this audit along the complete existing aerial course before any Riders adoption.

Digger's public basic Dig operation is incremental: its job adds `Opacity × max(.5, abs(original terrain distance)×.5) × brush distance` to each voxel field value. One brush invocation is not a Boolean subtraction. The source plan therefore requires measured repeated-operation convergence and real mesh support/clearance, with a bounded failure result. Brush timing or an assumed20pass count cannot establish a clear floor. Preserve the105m drivable floor physically, reground interior props against the actual upward cave-floor contact, retain a genuine roof, and inspect both exterior and interior Camera.Render images.

After native cavity proof, remove only the four rejected sideways roof clusters from the new review candidate, retain/recompose complete owned wall/crystal art, then repeat all8,022road contacts,36starts,444extraction sweeps and continuous full-mount course checks. Mode habitat/navigation rebakes and actual NWH traversal remain separate required integration work. No canonical world or saved game scene is modified by the preparation above.

Current continuation: the retained World29 Digger chamber passes technical
contacts but its 188 × 55 × 136m rectangular cut remains visually rejected.
Those 187 prototype grid rays are historical evidence, not a requirement to
preserve a rectangular final room. A following candidate must instead protect
actual source guardian actions, original NWH/cargo envelopes, meaningful driving
routes, the original 105m floor and the connected entrance.

The new read-only `DragonKingdomCavernOccupancyTests` native fixture is source
ready. V309 passed its native read-only fixture in 38.489665s: 31 original
configured effective actions, 2,893 samples and 3,224 source skin vertices.
Original source bytes, incoming scene context and preview cleanup all passed.
The actual combined sampled skin reaches Y130.95572m at the original Y107m
guardian home. This is a source-action measurement, not live flight acceptance. It opens the actual saved Heist in an
isolated preview scene, instantiates the original configured guardian beneath
an inactive disposable parent, resolves effective action clips through the
existing `DragonAnimalMotor` override authority, and samples each declared
frame plus scalar key times. It records the original mesh membership, source
hashes, four complete NWH assemblies and chassis boxes, four racks, sixteen
cargo bodies, semantic anchors and clipped source roads. Incoming scenes,
original asset bytes and preview cleanup must remain exact. This is deliberately
not a claim about live Mecanim blends, continuous motion, final excavation or
human acceptance.

The saved guardian is `dragon-inferno`, supported-role mask123, including
Flying as well as HoardGuardian. Pursuit requests vendor flight and a 2–14m
height offset. It must not be designed as a ground-only actor. Four original
complete NWH models measure approximately8.725m tall, but their saved transforms
are still at the village spawn; those world positions are not cavern occupancy.
Use their measured dimensions along actual cavern starts and driving routes.

The installed Digger Sphere is round in XZ: `Size.x` is its radius and `Size.y`
controls vertical radius; `Size.z` is ignored by the actual sphere distance job.
An anisotropic XZ ellipsoid would therefore be a false assumption. A bounded
rounded candidate can combine genuine vendor sphere vaults with low physical
floor/vehicle cores and measured route corridors, followed by the existing real
Reset operation below the 105m floor. Its exact dimensions must follow actual
occupancy results. The rectangular 55m core must not survive merely to satisfy
its own old probe grid. Material review must use the installed Digger URP
TerrainLayer bindings (the current rock layer tiles at 3m) rather than adding a
second excavation or terrain shader framework.

The optional `-tsDragonRecordLiveCavern` extension of the existing two-match
four-AI Heist fixture ran in V320. It retained 2,913 actual sampled world skins,
including 2,622 vendor-flight observations, with source bytes exact and no
truncation. The existing full-match test FAILED its second-round positive-score
gate; this is partial motion evidence, not a passing match. Sampled guardian
skin spans approximately X466.44–563.23, Y106.02–147.13, Z746.30–827.77.
Original pose fields describe the stationary outer character wrapper; they
must not be used as moving motor poses. A source-ready follow-up explicitly
records the authoritative vendor Rigidbody pose and labels both authorities.
The observer samples real skin and complete NWH/cargo bounds at 0.1 simulation-
second intervals, records vendor flight and Animator transitions, and uses the
existing public match setup/rematch/cleanup. It never injects guardian movement
or a flight result. Detailed telemetry is capped at12,000samples; truncation is
an explicit failed/incomplete observation. Source scene/world/catalog/avatar/
controller/skin bytes are checked after cleanup. This finite live sample set,
when available, must be combined with the complete original source-action
measurements and all allowed/reachable routes; unvisited space is not permission
to shrink a valid escape path. No new cavern geometry has been accepted.

The actual pursuit implementation has no fixed home-radius leash: its target
includes remembered velocity prediction for up to1.1seconds and the configured
2–14m flight-height offset. Therefore the chamber cannot be sized only around
the finite home-flight samples. Its original connected driving/extraction
corridors must remain valid for the actual flight skin and NWH/cargo bodies.

`DragonKingdomRoundedCavernBrushProfile` is an unintegrated authoring-input
builder for the installed `Digger.CustomBrush`. It supplies a centered, closed
flat-floor/vertical-wall/arched-vault mesh rather than a rendered box. The
proposed65m-radius lobe preserves the105m floor,130m shoulder and160m crown;
paired X490/550 lobes match the separately retained ceiling-only screening.
Geometry tests cover indexed manifold edges, outward winding, centered bounds,
ushort-safe indices and the genuine flat floor at several resolutions. Source
compilation passed and V329's three actual native geometry cases passed. Actual
Digger voxelization, roof-mass/floor/portal/continuous-flight gates and visual
inspection have not passed. This input does not replace Digger's
terrain, collision, material, LOD or persistence implementation.

VENDOR ASSETS AUDITED: installed Vista graph/biome APIs; Digger setup, persistence, basic operations, voxel modification job, spline walker and URP17 shaders; existing real cavern art and NWH carry/saddle contracts.

VENDOR ASSETS USED: Vista generates the review massif; the excavation plan uses Digger's existing implementation and owned cave art.

RELEVANT ASSETS NOT USED: Digger runtime live editing is unnecessary for a fixed authored cavern; the four rotated roof rocks remain rejected presentation evidence.

CUSTOM SYSTEMS CREATED: measured review orchestration, source preservation and planned game-specific corridor/clearance coordination.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
