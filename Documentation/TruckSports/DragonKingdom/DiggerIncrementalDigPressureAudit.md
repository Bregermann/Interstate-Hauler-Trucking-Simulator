# Installed incremental Dig pressure audit

Read-only source/formula audit. No vendor, shader, provider, canonical or production setting was changed. The coordinator owns the explicit analytic-only four-pass comparison; ordinary20-pass behavior stays unchanged.

V374 actual native analytic cave passed its full excavation fixture(121.558024s) and saved three-view rendering fixture(30.215334s). Shared finishing contains12crystals,12single-light lanterns and8rocks, with32parts/288actual native floor contacts/zero placement failures, source and original anchor/graph preservation true. The analytic5/6/7 anchors each passed trial0. Directly reviewed all three images: heavy regular ribs/grid shading and abrupt vault/corridor joins remain. Placement success is not visual or gameplay acceptance.

## Source-derived field

VoxelGenerationJob initializes v0=worldY-originalTerrainHeight. Below untouched terrain, write v0=-D for D>0. VoxelModificationJob computes w=max(0.5,D/2), independently of the current modified voxel value. ApplyDigAdd with positive opacity I is:

`v <- v + I*w*max(brushDistance,0)`

For N equal passes over the same brushes:

`vN = -D + N*I*w*sum_j(max(d_j,0))`

For D>=1, the continuous zero condition is `sum_j(max(d_j,0))=2/(N*I)`. At I=1 this is0.1for20passes and0.5for4passes. Overlapping brush contributions ADD, rather than forming a max-union or direct Boolean SDF. Utils.AdjustAlteration changes flags, not this density value. OpacityIsTarget is used by painting, not ApplyDigAdd, so it does not turn Dig into direct SDF assignment.

A single isolated native65x30vertical ellipsoid would have a continuous inward shift of0.4native distance units when moving20to4passes: approximately0.4m at its horizontal side and0.1846m at its top pole. This is only a local continuous-field estimate, not guaranteed marched geometry. Side/body/roof clearance can shrink, so original physical gates must remain unchanged. Floor Reset independently restores the original solid below105m, but does not guarantee every lateral or overhead boundary.

## Why grid locking is plausible

Outside a brush (d<0), Dig leaves the original negative density unchanged. Inside, increasing positive density steepens the transition. MarchingCubes interpolates zero using the two actual sampled values, not the analytic distance directly. Large pressure can push the interpolated crossing close to the first untouched grid endpoint.

Illustrative source arithmetic, not an executed native geometry test: assume locally constant terrain depth20m and a1.5625m horizontal edge whose brush distances are1and-0.5625. With20passes, endpoint values are180and-20; interpolation crosses90% toward the untouched endpoint, where the analytic distance is approximately-0.40625(outside the intended boundary). Four passes give20and-20, a50%crossing at analytic distance0.21875. Thus the continuous0.1/0.5threshold alone does not predict the physical surface.

Reducing pressure is a reasonable supported experiment, but it does not remove clipped-distance gradient kinks or overlap effects. Native scalar fields, all11946floor/roof/body/road/source checks, source art and the actual saved camera images remain authoritative. Do not compensate dimensions, change normals/materials or relax tests in the same experiment.

Source hashes, assumptions and exact arithmetic are preserved in `Logs/TruckSports/DragonKingdom/IncrementalDigPressureAuditV374/source-arithmetic.json`. No native performance, physical or visual success is inferred from these arithmetic examples. V375 actual four-pass comparison completed:3/3NUnit cases passed148.9038096s, including native cave101.571050s and three-view rendering30.711815s. It retained all11946columns,140colliders, zero roof failures, exact heights/outside holes/source and all32art/anchors. Direct review still REJECTS the appearance: regular left-vault ribs, right-wall grid and abrupt corridor joins remain. Entry PNG SHAf852ee5f66da1a9323abd5763c2e45342dae38c6aaa6f31b010870ad4c3fd58a; hoard3e851cc31e027a21602efb3879d63791644c92b5d5f97aaa5b469df1795928c8; flightf7677a65ec6c1e3450c72ecbfcd46e7b8b7c81c1697c79a4ceb296c827623b7d. Neither four-pass production behavior nor a complete cave repair is accepted.

VENDOR ASSETS AUDITED: VoxelGenerationJob, VoxelModificationJob, Utils.AdjustAlteration, BasicOperation, native Sphere distance and MarchingCubes interpolation.

VENDOR ASSETS USED: existing public native Dig/Reset/polygonization in the coordinator's bounded comparison; this contribution is read-only audit.

RELEVANT ASSETS NOT USED: shader replacements, custom normal reconstruction, direct scalar-field writes and vendor patches would confound the experiment.

CUSTOM SYSTEMS CREATED: source-derived arithmetic evidence only.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
## More decisive conditional source bound

For an isolated analytic lobe on the original1.5625/0.5/1.5625grid, native ellipsoid distance can change at most1.5625along one X/Z neighbor and1.3along one Y neighbor (largest65/25vertical ratio). A one-pass continuous threshold d=2 would force both endpoints of an isolated sign-crossing edge into the positive-distance branch, whereas four-pass d=0.5does not. This could separate clipped-boundary contribution more decisively than arbitrary intermediate pass tuning.

This is not native verification or an implementation request: overlap sums and sampled custom corridor brushes invalidate the isolated bound, and lowering20to1passes can shrink a single cap by roughly0.877m vertically or1.9m horizontally. Every original physical/source/art criterion must still pass. The coordinator alone owns any new candidate trial; ordinary defaults remain untouched.