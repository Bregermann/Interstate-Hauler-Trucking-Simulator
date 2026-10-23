# Jousting and Frogger score feedback — October 23, 2026

Q22-SKY-SCORE-01 remains **PARTIAL**. Two more existing sports now feed their actual committed awards into the shared Heat/DOTween presentation. This adds feedback; it does not redesign their rules, driving, courses or contacts.

## Implemented

**Jousting:** Actual qualifying armor/central-shield contacts publish the original 1/2 points. A credited knockdown publishes only the actual upgrade delta to the original 3-point pass total. Pass-specific hit/knockdown IDs prevent duplicated presentation. A custom zero-delta upgrade publishes no invented points. Both original lance contacts, speed thresholds, splinter physics, tournament lengths and shared tied placings remain intact.

**Frogger:** A course awards its existing one win point only after the actual fastest successful driver is resolved. An individual successful crossing does not prematurely award a point while other drivers still have attempts. Failed attempts and tied courses publish nothing; the original additional physical tiebreak decides the winner. The final championship tiebreak that only resolves placing remains point-free. The presentation unit is WINS, with the actual updated total. All five original challenges, one-life collision logic, timing, AI and participant ownership remain intact.

Both adapters subscribe once in Awake and unsubscribe on destruction. Explicit additive authoring installs the existing shared Heat notification with participant portrait/color, opaque readable backing and DOTween emphasis. Original objects retained: Joust 10,086; Frog 17,366. Each adds 46 UI documents. Five original documents per scene change only through the Canvas child list and preexisting shared null/default serialization; exact diffs are retained. All original world poses and physics documents are preserved; 925 unrelated pending files were rehashed unchanged. New tests preserve their authored GUIDs.

## Results-heading overlap found and corrected

The initial 945c1d68... candidate passed all 90 Windows gameplay checks (Joust 28/Frog 62), but actual Frog results captures revealed the final award notification clipping the results heading. Those original process receipts and both before screenshots are retained. This visual failure prevented promotion.

An unconstrained Editor Game View geometry attempt passed and did not reproduce the window layout. A controlled 1280x720 render using the existing SportsControlledCapture helper then failed the same title-overlap assertion after an actual mole contact and explicitly early results transition. Both native outcomes and exact fixture sources are retained; the first attempt is not misreported as a failure.

The shared results notification now occupies the unused upper screen margin (reference position y=-8) above the standard results card. Explicit additive migration updates only that presentation setting in all 16 saved opt-in sports. Sumo additionally serializes the existing result-size default and shared null references. A strict document diff allows only those exact fields: no objects, transforms, physics or other gameplay configuration change. Original gameplay placement y=-112 still restores on rematch. The corrected native fixture checks headings at controlled 720p/1080p as well as standings/buttons and rematch cleanup. This is controlled Editor rendering, distinguished from the final actual Windows captures.

## Actual validation

46 native EditMode tests passed in 2.1947 s: original Joust/Frog rules, saved Frog scene references, shared receipt invariants and three new exact-accounting tests. These cover all three joust passes, incremental knockdown values, duplicate/end locks, rematches, all five championship wins and ties/failures without invented points.

Five targeted PlayMode tests passed in 39.0325 s. Three exercise actual AI/NWH scoring: a qualifying lance contact, the real road crossing, and an actual mole contact followed by an explicitly early results transition. Actual awards reach Heat, presentation leaves authoritative points unchanged and rematch clears receipts/panels. The other two are explicitly synthetic receipt-order fixtures that leave game points unchanged. No physical scoring, clock or placement is injected in the three actual-scoring fixtures; the mole fixture deliberately calls EndMatch after its real contact to examine the final-award lifecycle.

Both original complete Windows probes passed on the delivered payload:

| Sport | Checks / errors | Process duration |
|---|---|---|
| Joust | 28 / 0 | 32.82 s |
| Frog | 62 / 0 | 360.82 s |

The Joust probe preserves the entire three-pass tournament, actual lance/shield/splinter contacts, replay, assigned-controller aiming/driving and rematches. The Frog probe preserves all five independently driven actual crossing challenges plus the original train, drowning, bypass, input, replay and rematch checks. The new accounting reconstructs actual score totals from each award stream. Four actual 720p/1080p award images were inspected at their rendered dimensions. These are composed Windows backbuffers; no screenshot substitutes for a physical match.

Native controlled input is software injection. No physical-controller/wheel, human playtest, subjective listening, prolonged soak or isolated CPU/GPU/memory acceptance is claimed. Existing startup/package-cache warnings are not suppressed. Other score publishers, time/placement outcome presentation, global results, sky/lighting/snow/reflection work and broader acceptance retain their previous statuses.

## Delivery

Windows x64: `Builds/TruckSports/TruckSports.exe`, with all companion files. Full payload SHA-256 `fc57b431f11308ba6ebe4bf702b24bc7da54bd5cd182e83cf89eee393c12d84d`; managed SHA-256 `f628427b4288f24cb75a24597a9534da0f498e7dd4834f416ef7355e87554030`. Exact sources and the previous accepted build are preserved. Evidence: `Documentation/TruckSports/Evidence/2026-10-23-Tournament-Score-Feedback/`.

Carnival 0.6.0 follows the complete current sky/time/scoring/results scope. Q23-IMPOSSIBLE remains queued after complete 0.6.0. Existing specialized Poker, Bowling and Kickball presentation is preserved.

## VENDOR ASSETS AUDITED

Installed Heat notification prefab/API, DOTween, existing original NWH/PhysX lance contacts and route/crossing systems, original shared camera/replay/menu architecture.

## VENDOR ASSETS USED

Heat transitions, DOTween emphasis, existing portrait/plate/theme, original NWH vehicle/PhysX contacts, original crossing navigation and replay.

## RELEVANT ASSETS NOT USED

No replacement vehicle, pathfinding, physics, UI animation or replay framework was needed. LWS owns award semantics and coordination.

## CUSTOM SYSTEMS CREATED

Typed Joust award callback, Frog course-win callback, existing shared presenter adapters, explicit additive scene installation and targeted accounting/physical acceptance tests.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
