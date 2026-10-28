# Truck Sports: Party! Alpha 0.7.0 — release evidence

The Impossible Games adds21 integrated sports to the existing collection. The Windows development build includes81scenes, with74selection cards,80sport choices and10menu pages. Every new sport has original logo/icon assets and an actual Windows gameplay preview. Existing sport artwork and unrelated project work are preserved.

## Implemented and repaired

- Original per-mode rules, AI, physical interactions, setup/HUD/results and saved scenes for all21games below; native evidence is listed separately from Windows and human verification.
- Repaired Piñata/Ballet/Nest shared-camera obstruction, Tornado entry framing, overlapping contextual HUD rows, Pet owner cards, Yoga contrast and Orchestra setup controls. Nest retains its six original animated rigs/materials and now fits their actual animated geometry.
- Archaeology resets transient future schedules after restoring an earlier checkpoint and publishes validated saved visible poses before rebuilding the original vehicle assemblies. Real nonzero deliveries and unique per-owner score preservation were observed through disk reset/reload/fresh-scene restore.
- Bake-Off's physical oven/serving sequence and Nest's finite-material construction/hatching complete naturally in the recorded tests; no fabricated score or outcome injection is used.
- Camping/Pet share the original wilderness: four terrains,2297tree placements, physical navigation and water layout preserved. Improved canopy, water UV/material presentation and grounded rock-bank/cave appearance use existing assets. Snow accumulation/clear transitions and actual GPU track pixels pass the final Windows matrix.
- Tea lawn uses corrected UVs/tangents with original geometry/furniture/collision intact.
- Archaeology and Snowman pause Save/Restore collided with Change Driving View. A failing native reproduction captured both. The correction changes only three anchored positions in each scene; every other serialized byte and callback stays exact. Both scenes pass720/1080 layout checks and final Windows image review.

## Actual verification

Final content SHA256: `f863429826854b0197a2175d29fc301b826271ad7701ae60e5d71a222dc0217f`. Managed SHA256: `ecaa03cb21ecf4a7a3066cba8a69d73115f5d89fe5035515998c310175c69eae`.

| Actual Windows6 run | Checks | Errors | Seconds |
|---|---:|---:|---:|
| openings | 421 | 0 | 214.06 |
| archaeology-disk | 62 | 0 | 33.42 |
| wilderness-snow | 127 | 0 | 102.34 |
| menu | 575 | 0 | 108.1 |

These are1185check executions, not1185unique test cases. Earlier Windows3 retains two complete Bake/Nest matches (203/0 at disclosed4x) and genuine earned-score Archaeology disk persistence (198/0 at1x). The original failed receipts remain failures; later passing builds do not relabel them. Final gameplay differences are recorded in the frozen source/asset manifests.

Targeted native regressions: seven exact focused repair cases; two restore precision/rejection controls;14preview/saved-menu tests;40existing shared Dance rules/timing tests; the two-scene workshop pause case; and six Python validation-helper tests. All21sport core receipts below are native evidence, not human-playtest claims.

| Sport / saved scene | Passing native receipt(s) | Final Windows coverage |
|---|---|---|
| TruckTruckGoose | impossible07-v2-goose-natural.xml: 2 passed (131.3832609s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckSubmarine | impossible07-submarine-v1.xml: 5 passed (115.9871265s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckMoonMadness | impossible07-v13-moon-original-full-eight-ai.xml: 1 passed (308.1349839s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckPinataPanic | impossible07-pinata-moving-shell-v9.xml: 5 passed (109.2272143s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckEggSpoonRace | impossible07-egg-v2.xml: 1 passed (28.6564485s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckTornadoRodeo | impossible07-tornado-v5.xml: 4 passed (46.7712663s); impossible07-v85-actual-transverse-entry-framing-and-original-nest-screen-effect-diagnostic.xml: 1 passed (56.1170281s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckMechanicalBull | impossible07-bull-v3.xml: 1 passed (53.8720661s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckGuessThat | impossible07-v2-guess.xml: 3 passed (53.7398906s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckJenga | impossible07-jenga-v6-natural.xml: 3 passed (111.0718498s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckCamping | impossible07-v52-actual-keyboard-loaded-deck-both-entries-and-ground-exits.xml: 1 passed (47.5234656s); impossible07-v52-original-keyboard-nap-four-ai-expedition-and-sixteen-render-ropes.xml: 3 passed (107.1520298s); impossible07-v58-installed-water-original-ramps-history-jc07-sharedworld-preservation.xml: 25 passed (26.2077213s); impossible07-v58-actual-installed-flow-pause-rematch-render-camp-pet-navigation-and-tree-collisions.xml: 5 passed (29.0600208s); impossible07-v80-installed-rock-actual-weather-shared-material-registry-diagnostic.xml: 1 passed (7.087871s); impossible07-v75-actual-closed-rock-roof-support-and-vertex-geometry-guards.xml: 3 passed (2.3476997s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckTeaParty | impossible07-v14-tea-original-natural-complete-hosting.xml: 1 passed (66.0506496s); impossible07-v63-whole-original-scene-source-and-geometry-preservation.xml: 1 passed (0.6434407s); impossible07-v69-tea-original-one-four-view-software-input-and-lawn-render.xml: 1 passed (8.7722098s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckDentist | impossible07-v18-dentist-original-natural-full-checkups.xml: 1 passed (93.4062533s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckBallet | impossible07-ballet-bank-v2.xml: 2 passed (38.7276982s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckBakeOff | impossible07-v53-actual-quickshow-all-four-oven-and-worktop-brake-arrivals.xml: 1 passed (73.3416985s); impossible07-v53-original-complete-baking-show.xml: 1 passed (75.5482583s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckFashionShow | impossible07-fashion-v1.xml: 5 passed (402.6250658s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckYoga | impossible07-yoga-v3.xml: 1 passed (16.0379571s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckNestBuilding | impossible07-v45-original-complete-protected-egg-and-hatched-family240.xml: 1 passed (112.5203917s); impossible07-v44-actual-finite-ai-brood-clearing-and-original-support-controls.xml: 4 passed (30.1595939s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckSnowmanBuilding | impossible07-v14-snowman-original-complete-workshop.xml: 1 passed (358.3012392s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckArchaeology | impossible07-v35-complete-untimed-four-crew-all12-physical-find-session.xml: 1 passed (1206.631857s); impossible07-v60-actual-backward-clock-and-fresh-original-nwh-assembly-diagnostic.xml: 2 passed (31.3682352s); impossible07-v71b-real-earned-checkpoint-independent-ledger-policy.xml: 4 passed (0.0820564s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckOrchestra | impossible07-v13-orchestra-strict-rendered-and-full-concert.xml: 4 passed (98.3765349s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |
| TruckPetPals | impossible07-v30b-actual-pet-adoption-and-natural-seven-trick-cues.xml: 3 passed (96.8102346s); impossible07-v58-installed-water-original-ramps-history-jc07-sharedworld-preservation.xml: 25 passed (26.2077213s); impossible07-v58-actual-installed-flow-pause-rematch-render-camp-pet-navigation-and-tree-collisions.xml: 5 passed (29.0600208s); impossible07-v80-installed-rock-actual-weather-shared-material-registry-diagnostic.xml: 1 passed (7.087871s); impossible07-v75-actual-closed-rock-roof-support-and-vertex-geometry-guards.xml: 3 passed (2.3476997s) | Final saved scene starts, pauses, rematches and returns to menu;720/1080 original captures retained. |

## Build, source and preservation

`Builds/TruckSports/TruckSports.exe` is the canonical destination; it requires its companion files. The accepted0.6 build and previous0.7 build are retained in private recovery folders. The final candidate is copied only after exact code/content acceptance.925unrelated protected file hashes remain intact. Public source is published through an alternate-index, public-parent commit to `main` and `codex/truck-sports-seven-events`; private integration history, raw commercial source and Digger-generated private geometry are excluded. Actual publication hashes are reported after the remote verification gate.

## Remaining verification limits

- No new human playtest, physical-controller/wheel/reconnect certification or listening acceptance.
- Full Windows matches were exercised for Bake-Off and Nest; other sports have per-mode native receipts plus real Windows opening/pause/rematch/menu checks, not a claim of full Windows coverage of every rules variant.
- Camping/Pet Heavy Snow Day is very bright and reduces contrast; subjective comfort remains unverified.
- No sustained GPU/CPU profiler or long-session memory-soak certification.
- Seven obsolete APV GUIDs remain unresolved in26 individually reviewed inactive renderer/pipeline fields; no runtime-resource resolution is claimed.

No claim is made that every historical request or excluded legacy row is complete. The permanent ledger retains their statuses. This delivery does not reopen the archived backlog or modify unrelated gameplay.

## Visual evidence

[Machine-readable acceptance and original image hashes](Evidence/Alpha07/acceptance-evidence.json). The same folder contains unaltered actual Windows images of the repaired pause controls, five world/gameplay examples, allfournew menu pages,1080p showcase, and Camping/Pet snow. All21actual gameplay previews are also imported under `Assets/TruckSports/Art/Previews/`.

# Alpha 0.7.0 vendor integration accounting

Implementation and acceptance are tracked separately in `TRUCK_SPORTS_ACCEPTANCE_LEDGER.md`. Native results do not establish Windows, human, physical-controller, listening or sustained-performance acceptance.

## VENDOR ASSETS AUDITED

NWH Vehicle Physics 2 and the existing Truck Sports tractor adapters; Heat and DOTween presentation; Pixel Crushers persistence; Weather Maker and Weatherade; Vista and EasyRoads wilderness authoring; the installed Digger excavation package; existing serving/gripper interactions; installed Vista, Mega City, IndieKit and SICS environment assets. The focused tree comparison also reviewed installed planter trees and cached SICS tree silhouettes. The landmark repair additionally audits the existing Toon Gas Station clusters and Toby Fredson RockBigRHEWP-S closed boulder, retaining its authored basis and original mesh/albedo/normal dependencies.

## VENDOR ASSETS USED

- NWH remains the tractor, wheel, suspension and driving authority. Recovery and AI commands use its existing input path.
- Heat/TMP/DOTween provide the established setup, cards, results and transitions. New sports extend shared Sports adapters.
- Pixel Crushers remains the workshop save framework; mode adapters own validated game-specific state.
- Weather Maker and Weatherade remain behind shared Sports weather and accumulation adapters.
- Camping and Pet Pals share one Vista/EasyRoads wilderness prefab. Existing terrain, road and all2,297 tree placements are preserved. The installed Mega City JC07 pine is reused through a sport-owned prefab variant with its original meshes, materials and two LODs; its two colliders remain disabled to preserve the original nonblocking forest physics. R54 verifies the four terrain payloads and actual disabled/enabled collision controls.
- Archaeology performs actual Digger excavation and preserves its generated scene/streaming dependencies.
- The installed landmark appearance profile reuses the genuine closed Toby Fredson boulder through an owned render-only prefab/material and17 explicitly placed masses. Five original renderers are hidden after validation while original colliders, shared-world bytes, water, terrain and navigation remain intact. Native visual selection and exact terrain/solid-contact checks pass. R79 installed rematch/registration proof passes against the actual existing shader contract: all17 registered bindings, one shared adapted material, active snow and matching global coverage. R77/R78 wrongly requested a nonexistent per-material property and remain retained failures. R80 actual native snow rendering passes with four inspected720/1080 captures; rebuilt Windows inspection remains pending.
- Tornado entry framing reuses the original Cinemachine GroupComposer and public extension callbacks for a mode-local MeshCollider ground floor; the existing obstacle authority and airborne/cab views remain intact. Nest preserves the original six Generic bird rigs, clips, routes and materials, with a mode-owned uniform visual fit measured from actual baked animated geometry.
- Bake-Off and Nest Building retain the existing finite-force serving/gripper and PhysX contact/joint foundations.
- Camping dressing reuses installed IndieKit crates and privately installed original SICS bench/pot assets. Licensed source bytes and GUIDs remain unchanged and excluded from the public payload.
- The shared lake/creek repair reuses the functioning Fishing URP material and DOTween flow adapter with River Modeler's WaveNormal. The separate waterfall material reuses its continuous WaterFoam texture, selected after actual720/1080 comparisons; it does not use a splash flipbook as a surface.

## RELEVANT ASSETS NOT USED

The sparse original Whitebark canopy and staged Vista pine were superseded after actual native comparisons. Mega City JC03 was also compared; JC07 provided the selected solid canopy. Integral-planter trees and similarly sparse cached SICS trees were rejected for wilderness use. The full river graph and Dock impact simulation would add responsibilities this static wilderness surface does not need; the working material/normal/flow foundation is reused instead. Open-bottom Toon Gas Station rock candidates were rejected after actual native images exposed floating rods and missing underside surfaces; their original licensed source is untouched. No second traffic, vehicle, save, weather, road or UI framework was introduced.

## CUSTOM SYSTEMS CREATED

Mode-specific rules, state, AI coordination and vendor adapters; guarded authoring and migration tools; original sport artwork and binding; input/viewport ownership; native and Windows acceptance probes. The focused repair pass adds physical oven/crew sequencing, construction surplus handling, safe saved-pose publication before joint recreation, and guarded hammock geometry/rope corrections. A separate water-only UV/tangent derivative retains every original triangle position, normal and winding; its author changes only19 mesh and19 material references and preserves the shared timber original. The Tea lawn repair reuses that same owned UV/tangent construction and changes a single saved mesh reference, with inverse whole-scene proof. A separate optional earned-score Windows probe observes genuine Digger deliveries and existing Pixel Crushers disk persistence; it never creates gameplay scores or replaces storage. These coordinate existing systems rather than replace their authorities.

## VENDOR SOURCE MODIFIED

No vendor gameplay C# was modified for these repairs. The private Digger intake separately records two optional assembly-configuration overlays for unavailable integrations; these are configuration changes, not replacement excavation code. The validation Editor also upgraded its private Digger reticle material serialization from URP7 to URP10; the original ROOT material remains untouched and is not an export candidate. Raw commercial assets remain private. Original SICS intake files are hash-checked against the owned package.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None intended. NWH, persistence, weather, road/terrain authoring, UI animation and excavation retain their existing authorities. Test-owned observers and isolated validation storage are acceptance infrastructure, not alternate production systems.

October28 exact dependency review preserves133 existing external assets:130 line-ending-only differences, one omitted default-false audio field, and two UTS URP Lit materials with identical active inputs but differing legacy aliases. Native mapping verifies47 installed package references; seven unresolved obsolete APV fields are retained only in26 individually verified inactive renderer/pipeline contexts. No vendor overwrite, runtime resource resolution, legacy getter or editor lightmapping equivalence is claimed by those classifications. The final menu appends21 cards and imports21 directly reviewed actual Windows opening captures using existing Heat/DOTween/Sports authoring. Development-only restore observers use a0.1mm physical/visible position bound after an actual0.019mm round-trip; existing angles, velocities, support and score checks remain intact.
