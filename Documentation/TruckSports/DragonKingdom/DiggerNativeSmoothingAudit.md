# Digger native cave polish audit

Read-only source/configuration review after the three actual V352 cave03 images.
The authored surface and source art are present. Dense grid-following bands and
abrupt lobe/corridor silhouettes remain visually rejected. No smoothing or source
geometry modification was performed by this audit.

Later lead-owned experiments: V356's normal-amplitude pair passes in 31.133983 s,
with exact restoration, but the no-normal-map image remains ribbed. V357's three
native upper-vault smoothing operations on fresh cave04 pass in 155.117934 s with
all 11,946 columns, 140 colliders and roof changes of roughly 3.4–3.8 cm. V358's
three saved renders pass preservation in 30.780287 s but reveal a new green roof
patch and remain visually rejected. Kernel smoothing alone is not accepted.
The subsequent native-paint audit is recorded in `DiggerNativePaintAudit.md`;
it changes no source helper or world and does not assert a repair succeeded.

## First diagnostic: directional shadows only

The saved cave has one direct directional light: intensity 1.4, soft shadows,
bias 0.05 and normal bias 0.4. Its twelve source lanterns have explicit saved
`m_Shadows.m_Type = 0` overrides. The active High URP configuration supports
2048 main-light shadows, four cascades and an 80 m shadow distance. Digger chunks
cast and receive shadows (`ChunkObject.cs:64`); static UV generation can set
two-sided casting (`ChunkObject.cs:125`).

These settings make directional shadow contribution plausible, but do not prove
it caused the ribbing. The smallest useful paired image preserves the actual
entry camera, all geometry, material bindings, light direction/intensity and
physics. Capture the original, set only the actual directional light's
`shadows = None`, capture, restore its exact original shadow type and capture
again. Full Light JSON/flags/pose, camera pose, original dependencies, voxel files/
membership, physics and heights must be restored. No scene is saved.

The explicit `-tsDragonCavernShadowDiagnostic` option now adds two separate
`shadowFrames` to the existing saved-cave capture; its original three baseline
frames remain unchanged. V355 executes this native diagnostic successfully in
32.485878 s; both cases in that batch pass in 35.9030955 s. Light JSON/flags/pose,
camera pose, dependency/voxel bytes/membership and physics/heights restore exactly.

The restored entry PNG is byte-identical to the baseline, SHA256
`f76d0c8d736f63dc5da0f169e3e4208a238f86727f90e5911bbedb2676e29bb7`.
The no-shadow image has SHA256
`94a5526a6e69822d213455c9227259373646ba813ee1365f9bb19c8467c3963b`.
Across the 1280×720 images, 260,781 pixels change, with mean absolute channel
difference 1.45034/255. In the fixed upper-left wall ROI `(24,32)-(500,400)`,
the mean difference is 1.28660/255. The heavy ribbing remains in both images.
The lead's direct review and this agent's inspection reject directional shadows
alone as its cause. Shadows do affect contrast; the result does not prove that
all lighting contributes nothing. Exact pixel evidence is retained in
`Logs/TruckSports/DragonKingdom/DiggerNativeSmoothingAuditV355/paired-shadow-pixels.json`.
This remains a cause-isolation result, not a shadow fix or visual acceptance.

## Geometry and normal evidence

The saved Digger settings are resolution multiplier 2 and anisotropic voxel size
`(1.5625, 0.5, 1.5625)` with automatic voxel height disabled. The installed
Marching Cubes code computes normals from forward SDF differences
(`MarchingCubesJob.cs:196`) and normally interpolates/normalizes those values;
its separate low-poly path uses triangle cross-product normals. The default
polygonizer constructor uses smooth style, not forced flat normals.

The installed URP mesh shader also deliberately blends interpolated normals
toward a derivative-derived triangle normal for triplanar sampling according to
triangle barycentrics (`MeshLitPasses.hlsl:129`). Thus voxel/SDF gradients,
triangle-dependent texture sampling and shadows can all contribute. Source and
images alone cannot quantify which dominates. V355 now establishes that shadow
cascades are insufficient explanation; it still does not distinguish SDF geometry
from normal/texture shading.

## Supported resolution and normal controls

The native editor exposes resolution multipliers 1, 2, 4 and 8, plus public
`VoxelHeight` and `AutoVoxelHeight`. `DiggerSystem.Init` derives horizontal spacing
from terrain heightmap spacing divided by the multiplier. Automatic voxel height
uses that same horizontal spacing, producing cubic voxels. The editor explicitly
requires clearing all existing modifications when either resolution or voxel
height changes. Do not change the saved cave03 master, invoke its destructive
clear operation or reinterpret existing voxel files at another grid size.

| Fresh candidate setting | Voxel XYZ, metres | Approximate volume-cell density vs current |
|---|---|---:|
| Current: ×2, manual Y=0.5 | 1.5625 / 0.5 / 1.5625 | 1× |
| ×2, automatic Y | 1.5625 / 1.5625 / 1.5625 | 0.32×; coarser vertically |
| ×4, automatic Y | 0.78125 / 0.78125 / 0.78125 | 2.56×; finer horizontally, coarser vertically |
| ×4, manual Y=0.5 | 0.78125 / 0.5 / 0.78125 | 4×; still anisotropic |
| ×8, automatic Y | 0.390625 / 0.390625 / 0.390625 | 20.48× |

These are calculated grid-density ratios, not measured timing, memory, triangle
or collider counts. Rounded brush dimensions/padding change exact array counts.
The supported cubic ×4 option is the smallest finer-horizontal cubic experiment,
but its coarser Y may fail existing floor tolerances. Cubic ×8 is substantially
more expensive and is not justified as an immediate full-world attempt.

There is no discovered public mesh-normal smoothing material slider. The default
Marching Cubes polygonizer already uses its smooth-normal path. Digger copies
`TerrainLayer.normalScale` to `_NormalScale0` through `_NormalScale3`; these control
texture normal-map amplitude, not SDF shape or vertex-normal smoothing.
`_BaryContrast` is declared/set by the installed code but not consumed by the
current URP mesh HLSL, so changing it is not a supported remedy.

Before any grid experiment, the cheaper remaining rendering discriminator is
the explicit `-tsDragonCavernNormalDiagnostic` capture. It now temporarily zeroes
supported `_NormalScale0..3` only on disposable copies of the actual Digger mesh
materials. It requires a real nonzero authored normal map, preserves every other
shader/color/vector/float/int property, keyword, texture and UV binding, then
restores original renderer references and destroys the copies in `finally`.
Exact original material JSON, camera/light state, mesh references and the
existing physical/source/voxel guards remain required. Its two `normalFrames`
are separate from the original three baseline views. Compilation is recorded in
`Logs/TruckSports/DragonKingdom/CavernNormalDiagnosticReady01/status.json`;
native execution is pending. This could isolate normal-map contribution but
does not eliminate triangle-dependent triplanar treatment or establish a mesh
fix. No voxel-resolution change or smoothing has been performed.

For a later fresh-grid experiment, first run the existing native input-voxelizer
on the same original closed source brushes using the proposed actual grid scale.
Measure finite signs, near-wall/roof/floor gradients and seam adjacency, then
time the native operation and record peak memory, mesh/collider counts and saved
dependency size. Configure only the new candidate master before `Init(false)`.
Retain every original 11,946 column, roof-mass, floor, corridor, 3 m guardian,
source/material/light/critical and render gate; a new grid does not relax them.

The installed `MeshToVoxelsJob` chooses a signed nearest-triangle distance with
an orthogonality tie-break, and custom-brush application samples its integer
input grid. Those paths and forward SDF normals are plausible contributors to
banding. No native counterexample has established a sign/triangle-distance defect,
so this audit does not diagnose one or propose modifying vendor source.

## Supported smoothing operation, only if later justified

The installed public editor uses `KernelOperation` with
`ActionType.Smooth`, `BrushType.Sphere`, positive opacity and uniform `Size`.
`DiggerSystem.ModifyWithoutMeshes(operation)` followed by
`BuildPendingMeshesAsync(false)` is the existing public batching path. The
kernel averages a 3×3×3 neighborhood and linearly blends the SDF. It is a physical
volume edit: walls may move inward or outward. It is not normal-only polish.

The actual radius is `Params.Size.x`; use equal XYZ sizes because area selection
uses their maximum. There is no ellipsoid mask or radial-opacity falloff here.
The job also runs alteration/hole bookkeeping over affected chunks; do not assume
that an unchanged density outside the sphere proves all metadata/hole maps exact.
Neighbor loading is scoped to one DiggerSystem and absent neighbors return a
default voxel, so the z800 terrain-system seam needs particular protection.

If a later diagnostic warrants a native experiment, start with one actual upper
roof/wall hit near `(585, measuredRoofY, 830)`, radius 6 m, opacity 0.15, one pass,
on a fresh candidate before final persistence/art placement. The location is a
planning hint, not a fabricated contact: require a real candidate Digger hit and
adequate floor, roof-mass and field-edge separation including voxel support.
Keep at least radius plus two largest voxel spacings from system edges. Do not
smooth the flat floor or both sides of a seam speculatively.

Re-run all 11,946 required source/live/roof columns, roof mass, critical poses,
roads/starts, original height/alpha/outside-hole and source guards. Require actual
changed-voxel/chunk bounds, paired identical-camera images and final source-art
support. Any failure rejects the candidate. One local pass is not promised to
repair large structural joins or coarse sampling. No custom mesh, renderer,
shader replacement, vendor edit or broad smoothing pass is authorized here.

VENDOR ASSETS AUDITED: installed Digger KernelOperation, kernel job, public system
batching, editor smoothing operation, marching cubes/normal and URP mesh shader;
actual cave03 saved light/voxel settings and High URP shadow configuration.

VENDOR ASSETS USED: existing Digger mesh/shader and Unity light/render APIs for
the explicit read-only paired diagnostic; no new terrain technology.

RELEVANT ASSETS NOT USED: native smoothing remains unexecuted pending diagnosis
and strict seam/clearance qualification.

CUSTOM SYSTEMS CREATED: one opt-in paired directional-shadow capture in the
existing saved-cave diagnostic, with exact restoration checks.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
