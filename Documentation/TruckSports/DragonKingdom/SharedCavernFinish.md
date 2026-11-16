# Shared source-backed cavern finishing

V342's cave-only geometry render was visually rejected. That isolated probe had
deliberately omitted the existing full builder's terrain palette and finishing.
Geometry success did not establish finished cave presentation.

`InstallSharedCavernFinish(world, candidateFolder, sourceOccupancyPath,
liveOccupancyPath)` adds the purchased art and existing light presentation to an
actual stopped native Digger candidate. The lead must install the existing authored
terrain palette **before** Digger `Init(false)`, then call this entry point after
the native floor/roof and collision are ready. This helper changes no Digger,
terrain palette, excavation, road graph, source prefab or gameplay system.

## Vendor audit and reuse

The actual source prefabs and their model/material/texture dependencies are reused:

| Source | Use |
|---|---|
| TARBO Tower Defense `Crystals/TB_Str_Crystal_A`, `_C`, `_F`, `_H` | The original twelve outcrops, varied authored shapes and materials |
| Polyart Handpainted Environment Vol1 `Cliffs/RockFormation_01` through `_04` | Eight smaller, grounded rock variations in the isolated cave |
| Fantastic Village `props/P_PROP_lantern_04` | Twelve recognizable real lantern models |
| Fantastic Village `light/Light_lantern` | The one original nested light per lantern; no second light copy |

`CaptureCavernFinishSourceAudit()` inspects the ten actual native prefab assets,
primary meshes, vertex counts, materials and embedded-light ownership without
creating assets. `CavernFinishEvidence.sourceDependencies` records the complete
actual dependency GUIDs, source hashes and metadata hashes before finishing and
verifies them afterwards. Unsupported legacy art materials use the existing
`ConfigureFinishMaterials` conversion to owned candidate copies.

No licensed chest/coin/treasure prefab was found in the installed private
environment closure or project-wide prefab/model search. The existing Heist coin
hoard and ingot are game-owned meshes and are left intact. No extra heap, primitive
substitute, collectible-looking decoration or duplicate pickup is added without
proven guardian, anchor and actual gameplay clearance.

## Placement and ownership

The existing twelve-crystal recipe has been extracted into one shared helper.
Its full Main invocation uses the same source sequence, hints, scale targets,
yaws, solid collision, underground support mode and bounded placement trials.
No additional guardian guard is applied to the default full-world call. The
Water helper accepts an optional region filter; null retains the exact original
`caverns, village, harbor, refuge` order and original 28-lantern behavior.

The isolated entry point supplies the additional measured clearance guard to
those existing placement systems. It requires the exact native source-action
and recorded-live occupancy inputs already used by the rounded-cave experiment:
31 original action envelopes and 2,913 live guardian-skin samples. It protects
their conservative combined AABB with a further 3 m margin. This live recording
is explicitly partial visited evidence, not proof of all future guardian motion.
All current protected original guardian Assets hashes must remain exact.

Every new crystal, lamp and rock also passes the existing road, truck-start,
nest/cache/perch and extraction guards. Nine footprint stations must hit the
actual candidate's Digger collider floor, not TerrainCollider, another world's
geometry, a decoration or a fabricated floor height. Failed positions remain in
the original bounded placement process; a missing required placement fails the
finishing stage rather than silently dropping art.

The isolated result requires exactly 12 crystals, 12 lanterns and 8 rocks. The
lamps retain their original single vendor light and the existing cave settings
(Point/Realtime, intensity 60, range 34, no shadows). Existing source materials,
UVs and texture art are reused through the established conversion, not a global
shader replacement. The helper verifies all original transforms, active states,
critical anchors and graph remain unchanged. It does not move gold anchors,
replace treasure objects or alter pickup availability.

The report is written to the active run's `shared-cavern-finish.json`, including
source provenance, actual accepted geometry, light counts and Digger contacts.
On failure, only newly created cave roots are removed; failed placement/evidence
is retained. Candidate material artifacts remain reviewable and are not adopted.

## Validation limits

New targeted tests inspect actual owned source geometry/material/light ownership
and reject any attempt to finish the persistent canonical prefab. These are
separate from the lead's actual new native cave placement and render test.
The additional saved-native trial opens the actual V339 probe additively and
runs finishing against its already-generated Digger collision. It never calls
excavation or `Init`. Candidate material copies live only in a fresh temporary
`CFn-<8 hex digits>` folder, then the scene changes are discarded and that
exact folder is deleted. Creation and cleanup both verify the resolved absolute
path and exact leaf are directly inside this project's
`Application.dataPath/TruckDragonKingdom/VisualCandidates`; existing folders or
metadata are never reused or deleted. The short tag reduces the representative
absolute material path from 258 to 215 characters, avoiding a separate metadata
path-length risk. The actual V345 NUnit output identifies the immediate failure:
`Parent directory must exist before creating asset`. The cave-only helper had
omitted `Materials/Finish`, normally created by the full builder. The shared
helper now creates and imports that exact owned-candidate subfolder before art
conversion. V345 failed before placement; no placement acceptance is inferred.
Original probe dependencies and all native voxel bytes/
folder membership must remain exact. Supply the existing probe through
`-tsDragonCavernFinishSavedProbe CavernRoundedProbe01` plus its original
`-tsDragonCavernOccupancyEvidence` and `-tsDragonCavernLiveEvidence` paths. This
cheap native placement test precedes another full Digger run; it does not grant
new-palette rendering or finished-art acceptance.
Compilation does not establish attractive lighting, materials, guardian play,
camera comfort or finished art. Three fresh cave images and actual native
clearance results remain required before presentation acceptance.

VENDOR ASSETS AUDITED: TARBO crystal prefabs and source models/materials,
Polyart authored rock variants/textures, Fantastic Village lantern and its
documented single nested light, existing source scenes/import provenance,
existing Heist gold assets, Digger floor/collision and current finish systems.

VENDOR ASSETS USED: those original crystals, rocks, lantern models/materials and
light, Digger native collision, existing owned material conversion and bounded
source-art placement authority.

RELEVANT ASSETS NOT USED: no suitable licensed gold/chest prefab exists in the
current intake; no additional game-owned hoard is placed without proven clearance.
The existing terrain palette remains the lead probe's prerequisite, not duplicated.

CUSTOM SYSTEMS CREATED: a narrow reusable cavern-finishing entry point, source
audit and additional original-guardian/native-floor placement guard.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
