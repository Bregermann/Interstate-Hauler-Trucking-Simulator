# Native rock repaint after bounded vault smoothing

The lead's V357 experiment executes three native 6 m / 0.15 smoothing operations
on fresh cave04 and passes all 11,946 required columns. V358's three renders pass
source/render preservation but reveal a new green roof patch and remain visually
rejected. Smoothing can expose voxels whose original texture properties were not
the underground rock layer. That explanation is consistent with the installed
kernel retaining each voxel's texture properties while changing its SDF/alteration;
the affected actual vertex/voxel weights still need measurement.

No root smoothing helper, Main, source mesh, material or vendor code was edited
by this audit. The following is a proposed native recipe, not executed acceptance.

## Supported recipe and important target-opacity behavior

The existing authored palette order is GrassGreen, GrassYellow, Dirt, Rock, so
the intended underground rock index is 3. Verify that actual candidate layer 3
still points to the owned Rock palette before painting.

Use the same three recorded centers, owners and uniform 6 m sphere bounds as
the smoothing operations. After smoothing, run one public native paint operation
per recorded area, before final vendor mesh persistence:

```csharp
var paint = new BasicOperation
{
    Params = new ModificationParameters
    {
        Action = ActionType.Paint,
        Brush = BrushType.Sphere,
        Position = (float3)sample.center,
        Size = new float3(6f),
        TextureIndex = 3,
        Opacity = 1f,
        OpacityIsTarget = false
    }
};
await actualOwner.ModifyWithoutMeshes(paint);
// After the three owned operations:
await actualOwner.BuildPendingMeshesAsync(false);
```

Batch/build through the actual two existing candidate Digger systems as the
current authoring already does. Do not find an unrelated global Digger instance,
paint a material object or change the terrain alpha layers.

`OpacityIsTarget=false` is deliberate. The installed non-MicroSplat editor uses
that mode, and the public paint job then calls `Voxel.AddTexture(index, opacity)`.
At opacity 1 it saturates index 3 correctly whether that index is first, second
or newly introduced. The installed target-mode `Voxel.SetTexture` assigns its
lerp directly even when the requested index is **first**, while the shader gives
the first index weight `1 - lerp`. Consequently target mode with opacity 1 can
remove rock instead of saturating it:

| Current indices | Target=true, opacity=1 | Target=false, opacity=1 |
|---|---|---|
| First=3, Second=0 | Lerp=1: grass gets full weight | Lerp=0: rock gets full weight |
| First=0, Second=3 | Lerp=1: rock gets full weight | Lerp=1: rock gets full weight |
| Neither index is 3 | Introduces rock at an endpoint | Introduces rock at an endpoint |

There is no single universal target-opacity endpoint for both first and second
index orderings. Use the existing additive saturation path rather than modifying
vendor code or creating a custom per-voxel paint framework. This source behavior
is identified in `Voxel.cs:166`/`:183` and `MarchingCubesJob.GetControlFor`.

## Geometry and scope qualification

`ApplyPaint` changes texture properties and does not assign `Voxel.Value`.
However, the shared job subsequently runs alteration classification and hole
bookkeeping, including across the selected native chunks. Therefore geometry
preservation must be established by the actual experiment, not assumed from the
action name.

1. Keep cave03 and rejected cave04 immutable. Configure a fresh candidate and
   retain the actual post-kernel, pre-paint SDF/mesh/physics baseline.
2. Snapshot exact float bits of every affected loaded voxel Value, alteration,
   nontexture properties and actual chunk coordinates. After painting require
   identical SDF values, alteration, wetness/puddle/stream/lava properties, and
   exact outside-sphere voxel bytes. Only intended texture indices/weights within
   the declared spheres may differ. Audit all touched chunks, not merely centers.
3. Require TerrainData heights, alpha maps and holes unchanged by **painting**;
   this is stricter than excavation's outside-hole guard. Keep original assets,
   palette/shader/texture/UV bindings, master/grid, critical poses and source art
   untouched.
4. Rebuild using the native mesh system. Compare mesh and collider geometry with
   exact canonical triangle/normal signatures and counts, accounting only for
   native parallel triangle ordering and double-buffered mesh objects. Whole
   serialized mesh hashes are unsuitable because splat-weight channels change
   intentionally. Do not introduce position tolerances or custom replacement
   collision. Collider type, cooking, enabled state and physical triangle surfaces
   must remain equivalent to the post-kernel baseline.
5. Measure the actual rock weights on altered voxels and rebuilt visible roof
   vertices in the declared regions. Unity 6's first four splat weights occupy
   TEXCOORD2 (`SplatControl1`), with index 3 in its alpha component; TEXCOORD1 is
   reserved by the installed layout. Verify original authored Rock remains bound.
6. Repeat all 11,946 source/live/floor/roof columns, roof mass, 3 m guardian and
   original road/start/critical/source checks, plus final 32 art-root supports.
   Keep the actual collider count and full negative/positive results recorded.
7. Render the same three saved views and inspect the new roof patch directly.
   Texture restoration does not establish that ribbing or abrupt joins are fixed.

If a thin color edge remains, inspect the exact contributing marching-cube
vertices and their voxel weights. Surface interpolation can involve a voxel just
outside the smoothing sphere, and unaltered/on-surface voxels use terrain control
weights. Do not silently enlarge the paint area or relabel a green result as
accepted. Any additional halo needs an explicit measured bound and the same
guards. Existing floor/source/global terrain painting remains out of scope.

VENDOR ASSETS AUDITED: installed BasicOperation, native PaintOperationEditor,
VoxelModificationJob ApplyPaint, Voxel AddTexture/SetTexture, Marching Cubes
texture selection/Unity 6 layout, existing palette and actual smoothing helper.

VENDOR ASSETS USED: proposed recipe uses the existing public native paint action
and original authored Rock layer; execution/qualification remain pending.

RELEVANT ASSETS NOT USED: no material replacement, custom mesh, renderer overlay,
new terrain framework or vendor patch is needed for this texture repair trial.

CUSTOM SYSTEMS CREATED: none; audit and validation recipe only.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
