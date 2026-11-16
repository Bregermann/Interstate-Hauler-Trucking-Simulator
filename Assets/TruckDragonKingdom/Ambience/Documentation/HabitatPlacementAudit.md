# Actual-world habitat search repair

Native baseline: `Logs/TruckSports/DragonKingdom/NativeFourScenesV76/rendered-evidence/AmbienceClearance/20261029T005841971-castle-ground-2.json`. The composer rejected the third of eight ground inhabitants before any saved mode scene. All96 old castle centers were attempted, but only first24 failures were retained:20 road/objective exclusions and4 unsuitable support probes, zero body overlaps. Therefore the baseline does **not** prove the remaining72 reasons.

Source confirms the incorrect search assumption: regional root `(0,60,360)` is the six-road castle junction. Actual `Fortified castle sanctuary` is an authored child200m north, and actual300m-wide graded castle apron is centered220m north. Root-centered radii42/65/88/112 mostly search converging roads and terrain drop-offs instead of the courtyard. The native witnesses show these real roads/starts and steep or low offroad terrain at the captured probes.

The batch repair searches actual regional environment child pivots and renderer footprints, then the physical region AABB. Bare semantic flight/start/objective anchors are not used as ground evidence. At most16 distributed environment seeds,512 nearby source candidates and a bounded terrain-region grid share the1024candidate maximum. The same8point9×7m ground loop remains. No actor is omitted, world geometry is untouched, and all8ground searches finish before any population/NavMesh creation; failures aggregate with separate per-habitat witnesses.

Support is still established by actual static-collider raycasts, normal.y>0.85, an unobstructed original body capsule, the original30m support band,12m road-edge clearance,28m objective distance,45m extraction clearance,30m same-region population separation, and every original0.7m leg sample with<0.22m height discontinuity. Subsequent original native NavMesh complete-path checks remain. No threshold was lowered. The support-band reference is actual local `Terrain.SampleHeight` instead of a semantic road-junction altitude: forest root24 can stand44m above actual offroad terrain−20. This corrects the source's assumed support location; it does not create a flat surface, change terrain height or waive a real collider test.

New native receipts contain every rejection-kind total, up to24representative witnesses spread over later candidates/kinds, and the accepted route when one exists. Support witness rays start at each actual candidate altitude+130, matching the230m-depth physical search rather than missing low ground by starting from a high junction. Scene and native Editor execution remain parent-owned. `Habitat.SourceCheck.json` records zero-diagnostic standalone Roslyn compilation; native eight-population success is **unverified** here.

VENDOR ASSETS AUDITED: actual accepted world hierarchy, terrain/road support witnesses, original ambient route authoring, Unity NavMesh builder/public terrain APIs and existing owned living art.

VENDOR ASSETS USED: existing real terrain/colliders/EasyRoads graph, original NavMesh bake/path checks, Camping fauna/Taxi humanoid population composition unchanged.

RELEVANT ASSETS NOT USED: semantic aerial anchors as assumed walking ground; no new level/terrain/road authoring framework.

CUSTOM SYSTEMS CREATED: bounded actual-environment habitat search and complete batch diagnostics, replacing the mistaken junction-centered search.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; Unity physics establishes support and Unity NavMesh owns navigation.
