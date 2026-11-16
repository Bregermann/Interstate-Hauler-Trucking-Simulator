# Native EasyRoads relief coordination

Status: implemented / unverified. V306 generation failed the existing urban-bank
dry-land guard before full world acceptance. Four new bank samples remained below
sea level, and the connected original village/harbor dry-land stage had been
omitted with the old broad coordinator. The next candidate restores that exact
bounded semantic stage before bank refinement, without loosening any dry-land,
water, road, source, seam or terrain-quality acceptance limit. Full native
generation and the eighteen regional views remain required.

V308 confirmed the restored district stage passes the existing 172 additional
dry-land samples. It then stopped at the old merchant-berth input-delta limit:
the actual native bed needed a 6.72m cut, exceeding the legacy 4.04m allowance.
The next explicit native-only policy uses the existing actual 32m source hull
and its 60% draft to verify 25 terrain/keel contacts with at least 3m clearance
and 53 conservative ferry sweep boxes before accepting the bounded berth.
Deeper water is valid; no original outside/shore/ferry/seam/lowering-only mask
check is relaxed. The legacy default retains its 4.04m limit. The installed
ship retains its independent actual Physics overlap sweeps and waterline checks.
V309 confirmed this berth gate passes with 5.111427m minimum keel clearance.
The world then failed two actual forest-junction support footings. Those
physical failures were reproduced in V310 at the same two sides of the forest
junction. Both the exact-SHA persisted32 height diagnosis and fresh33 real
Collider rays measured ground at Y24.0029297m under actual road top Y24m.
The original prefilter discarded those contacts because it only considered
ground at least2cm below the pavement.

A source-ready native-only correction recognizes direct terrain support solely
when the actual R16_UNorm/600m TerrainData encoding bounds the height discrepancy
within half a quantization step, the real pavement top exists at the same XZ,
and the original upward-normal requirement passes. It reads Unity’s public
normalized height scale rather than adding a generic gap tolerance. The
encoding scale is exposed in [Unity6000.4 PaintContext](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.4/Modules/Terrain/Public/PaintContext.cs#L145). Legacy
filter behavior is unchanged. Six targeted guard cases cover the measured
contact and rejected high ground, missing top, nonterrain, bad normal and an
overlarge bound. V311 passed all six controls and the real native34 bearing stage, including
eight genuine pavement-coincident ground supports and zero support failures.
The world then failed unchanged terrain quality:21,366 steep65 cells against
2,000,4,863 steep80 against500 and945 conflicting ground cores; tile seams
remain exact.15,908 steep cells join untouched native-vendor samples, so this
requires vendor-stage boundary attribution rather than more erosion of the
previous artificial fans or relaxed limits. The complete native world has not
passed.

The rejected World29 landscape is preserved. Its previous coordination path
reconstructs broad 600m cut/fill envelopes from the raw Vista foundation after
EasyRoads has already graded the candidate. Those envelopes produce the large
planar fans visible in the rejected regional images.

The explicit `VendorGradedRelief01` mode starts with the **actual EasyRoads
heightfields**, retaining their relief outside the real road/staging cores and
bounded local transitions and the explicitly declared connected village/harbor
dry districts. These semantic parcels retain the existing hydrology-aware 40m
creek transition; they are not inferred from whole-ensemble renderer bounds.
Receipts record their exact bounds/footings and descriptor hash, per-tile applied
counts, maximum raise, and authorized/district mask hashes. The outside guard
compares all samples outside every declared physical and semantic area against
the actual native heightfield. It records the vendor and coordinated height hashes,
untouched native samples, actual protected start/extraction samples and any
bridge or underground handling. It does not infer a town-wide or grove-wide pad
from scenery renderer bounds.

The actual underground traversal retains its native graded roof. Underground
road/floor/start coordinates do not lower that roof to pavement height; the
existing Digger excavation remains responsible for its physical floor. The
receipt distinguishes preserving the native roof from equality to the original
ungraded Vista foundation rather than asserting those are interchangeable.

Use a fresh candidate with the existing rounded-world authoring command and:

```text
-tsDragonTerrainCoordination VendorGradedRelief01
-tsDragonFinishArt
-tsDragonWorldCandidate RoundedIntegrated36
```

The default remains `LegacyContinuousEnvelope`. Normal Play Mode and builds do
not regenerate this terrain. Existing candidate directories are rejected instead
of overwritten. A typo in the mode name is an error.

The focused native case is:

`DragonKingdomNativeRoadReliefTests.ActualEasyRoadsReliefCandidatePreservesNativeSamplesAndPassesOriginalPhysicalWorldGates`

It runs the real asynchronous complete authoring path and retains the original
road, start, extraction, grounding, terrain-quality, Digger and source checks.
Passing this case does not establish finished art, actual driving, Windows
rendering or human acceptance.

Substantial architecture is a following composition stage using the actual
native geometry measurements. Its individual ground-bearing parcels require
source/layout/instance/geometry provenance. This first terrain-mode invocation
records an explicitly empty new-architecture-footing list; it does not invent
measured building parcels or claim that the four new ensembles are installed.

VENDOR ASSETS AUDITED: installed EasyRoads public grading adapter and its shipped
documentation; Vista foundation/terrain workflow; Digger brush operations.

VENDOR ASSETS USED: actual EasyRoads grading output, Vista TerrainData and existing
Digger floor/roof authoring.

RELEVANT ASSETS NOT USED: further erosion of World29's reconstructed fans; the
V296 comparison was visually insufficient and V299's stronger treatment failed
the unchanged slope guard.

CUSTOM SYSTEMS CREATED: one opt-in coordination branch and its native regression
case, plus structured evidence of its limited physical corrections.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none. The adapter retains and coordinates
vendor terrain output; it does not replace the terrain or road generators.

The optional `-tsDragonObserveVendorGrade` diagnostic records real setter
returns, source-assigned and actual public type metadata separately, actual
markers, deformation spans, native serialized settings, focused NE/junction
height patches and full tile height hashes before/after the single vendor build
and network destruction/restoration. V314 exposed a null public road type for
runtime-created EasyRoads types before the vendor build. The observer now records
that unresolved public type honestly and retains assigned labels separately;
V315 completed the single real vendor build and proved the NE discontinuity
appears during that build, not creation or destruction/restoration. Marker
setters returned 28/110, while actual road-global indent/surrounding remained
5.859375/5 and the dynamic type defaults were zero. These are distinct settings.
A following native-only adapter registers stable owned type IDs through public
`ERRoadType.Update()`, looks up that exact owned binding, and configures its real
defaults before road creation. V327 proved actual stable registration, distinct
bindings, preserved existing entries, duplicate rejection and exact marker/type
defaults. Its tiny fixture still failed because the created road's independent
global indent/surrounding remained5.859375/5. A following shared adapter selects
only the created road holding the exact registered type object and configures
its public global fields before Refresh. The updated tiny fixture also retains
a differently typed road to check that its globals/type are untouched. V329
exposed that created native type data are not reference-equal to the registry
binding. The corrected adapter uses the returned `ERRoad.roadScript` and
`gameObject` public identities, validates exact owned network/type semantics,
and records reference sharing separately. V330's actual tiny fixture passed
in3.183547s with exact road globals, defaults, markers, other-road state, source
bytes and incoming context. Defaults/globals-only World38 still failed its
unchanged quality gates (steep65=21,341, steep80=4,876, ground conflicts947,
seam0). Correct configuration therefore does not establish a terrain remedy.
Legacy grading and all original quality limits remain unchanged.
The observer itself changes no grading parameter.

A separate crossing provider derives proposed native
marker/classification intervals from actual lower road width, the existing
wheel/body swept corridor, crossing angle and the original34m creek core. Its
input is the actual authored road graph geometry; it does not divide by the
visual stone arch's aperture ratio. It has independent physical-envelope and
ground-core conflict controls; all9 actual native cases passed in V329. The new
explicit `-tsDragonPhysicalCrossings` flag now feeds this same provider to vendor
marker exclusions and native terrain classification. It also rejects genuinely
incompatible lower ground/upper clearance constraints instead of silently taking
their minimum. The defaults-only behavior remains unchanged without this flag.
V335's actual failure witness identified a same-road endpoint cap beyond the
declared stream-bridge interval: at X−1053.125/Z350 the grounded road required
59.793884m, while a projection1.113998 clamped back to the prior bridge endpoint
and incorrectly demanded57.700733m. The narrowly bounded projection correction
retains the interval's existing8m body margin and does not change inside-span
or lateral clearance. V337 passed all16 crossing controls and the full native
candidate reached zero core conflicts/seam0. Its unchanged quality gates still
failed (steep65=21,557, steep80=6,128), so the full world remains rejected.

The independent `-tsDragonObserveNativeGradeFootprints` option records the actual
public vendor-generated contours plus four field hashes at every creation and
update. V335 retained68 creation/68 update events and one vendor build; only
that build changed the fields. At the NE failure, the actual main-road indent
point was approximately(1255.346,198.748,1167.063), while its surrounding edge
was(1372.396,452.964,1225.599). The terrain nevertheless jumped roughly199→302→423m
over6.25m at that indent edge. Both per-segment surrounding sides were enabled.

The new independent `-tsDragonVendorSmoothNE` causal trial uses the installed
public `ERRoadNetwork.SmoothenIndentFalloff` and `SmoothenSurroundingFalloff`
post-level tools on only those two measured actual returned NE roads. Its
explicit110m distance reuses the existing surrounding policy. It records each
operation's four before/after height hashes, change masks, real contour/tool
footprints, costs, owned setting restoration and original source-byte checks.
No custom smoothing kernel or all-road treatment is introduced. This is
source-ready; full quality/physical acceptance has not been established. In
V340/V343 the first real vendor operation changed6,513 samples in about0.023s,
with other fields/source/owned settings exact, but the continuous footprint
guard rejected169 samples. The retained actual before/after float32 hashes
confirm all169 lie on just two boundary rows Z1068.75/Z1331.25:0.25m outside the
continuous bounds but exactly inside the actual3.125m terrain-cell coverage.
No sample escapes the precise outward floor/ceil grid footprint. The corrected
guard records both continuous and raster masks separately and has portable
boundary/negative-grid controls plus an optional test against all actual native
float32 samples. It changes no SDK parameter, height tolerance or roof guard.
The complete vendor operation sequence and subsequent original world gates
still require an actual native pass.

The separate `-tsDragonFinishedArchitecture` flag additionally requires finished
art and an explicit `-tsDragonArchitectureGeometry` witness. Its source-ready
main-builder integration preserves the complete25 retained-building inventory,
adds all678 measured source instances and525 individual foundation parcels,
uses the prepared spatial footing index, protects the four exact complete-frame
Transforms during loose scenery re-grounding, and schedules real waterfront
support after dressing. It requires final independent9-point/native TerrainCollider
contacts for every new parcel before claiming terrain support. This integration
does not run in the crossing-only experiment. Saved-reader/schema4, native full
world, actual driving and all18-view acceptance remain separate requirements.
