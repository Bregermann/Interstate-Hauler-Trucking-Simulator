# Real finish measurements — October 23, 2026

Q22-SKY-OUTCOME-01 adds explicit, non-score finish feedback to Red Light Green Light, Avalanche Escape and Ninja Warrior. The broader Q22-SKY scope remains partial. No new points, completion conditions, penalties or placings are introduced.

## Implementation

The existing immutable receipt/journal now distinguishes Points from Outcome. An Outcome carries the actual finite, nonnegative measurement and unit. The adapter shows recorded seconds to three decimal places, participant name/color and the actual finish reason; it never displays +0 points or a fictitious total. Outcomes remain separate from ordinary point aggregation. Existing points and team receipts retain their original values, formatting, identity and chronology. Event IDs are match-local and deduplicated; closing a match locks further events and rematch creates a fresh journal.

The original three rules publish only after committing their accepted finish: Red Light's interpolated crossing time, Avalanche's whole-truck escape time and Ninja's penalty-adjusted course time. The scene-bound session adapter subscribes once and unsubscribes on destruction. A finish is not prematurely announced as the champion while later competitors can affect placing.

Three saved scenes receive the existing Heat notification and DOTween portrait emphasis. Original scene documents retained: Red Light 8,728; Avalanche 9,298; Ninja 11,093. Each adds 46 UI documents. Five original documents per scene change only for the Canvas child and known null/default serialization fields. Exact original-document diffs are included. Original world poses, physics, courses, weather, cameras and replay objects remain unchanged. The 925 unrelated pending files are rehashed by the delivery guard.

## Executed validation

78 native EditMode tests passed in 3.1145 seconds: original rules/scene references, immutable outcome measurement/identity, invalid-value/unit guards, duplicates/end locks/rematches, each original rule callback, ties/penalties and ordinary point/team/tournament regression.

The final seven-test native PlayMode run passed in 123.7974 seconds. Two tests use actual AI/NWH routes and real finishes: solo Red Light and the standard twelve-truck Avalanche race at 3x game time. Each measurement matches its authoritative recorded time, Heat shows SEC without +0/PTS/TOTAL, and rematch clears the journal/panel. Three existing arcade tests cover a real mole contact and controlled results bounds plus two synthetic receipt-order fixtures. The existing physical court-contact regression also passes. The seventh test is explicitly synthetic UI-only input of point 1 / time 42.125 / point 2 for one identity/reason/tier/unit; it proves three separate ordered notices without modifying actual game points.

### Initial failed native scenario retained

The first suite passed 6/7 tests; its solo Avalanche entrant was buried before finishing. A repeat with sampled route/NWH state also failed that scenario: the actual standard option 0 truck drove to roughly 1,549 metres before burial at 158.137 race seconds. The accepted earlier Windows baseline already records truck 0 buried around 1,611 m while other competitors finished all three routes. There is no evidence that finish-feedback caused this individual loss. The initial test's assumption that this particular solo AI must always finish was invalid for an elimination race. Both failed XMLs and exact fixture sources are retained. The final fixture uses the established twelve-competitor race, requires at least one real finish and exactly one receipt per actual finisher, and does not weaken the genuine-finish/value assertions. The subsequent twelve-driver native run passed the actual Avalanche finish/UI/rematch test and five regressions, but its generalized test helper incorrectly queried a nonexistent finished field in Red Light, which uses an outcomes enum. That test-only null lookup was corrected to the original enum before the final full rerun. Both failed run and exact fixture remain in the evidence. Avalanche gameplay and difficulty are unchanged; no blanket solo-AI success claim is made.

All three original full Windows probes passed on this exact delivered payload:

| Sport | Checks / errors | Duration |
|---|---|---|
| RedLight | 43 / 0 | 50.66 s |
| Avalanche | 48 / 0 | 256.98 s |
| Ninja | 43 / 0 | 158.87 s |

Red Light preserves its complete legal-signal twelve-driver race and original illegal-movement/input/replay/rematch checks. Avalanche preserves the full twelve-truck standard race with at least one physical finisher on each of the broad road, cliff jump and frozen-river routes, plus original snow contact/burial and multiplayer/input/replay/rematch fixtures. Ninja preserves the actual full six-obstacle course, real bell contact and original platform/charge/input/replay/rematch checks; its explicitly injected Editor-only wall diagnostic is not full-course acceptance. The new accounting compares receipts against each actual recorded finish, including unfinished/eliminated drivers receiving none. Six actual Windows 720p/1080p measurement captures are inspected separately from controlled Editor rendering.

Software input is not physical-controller/wheel or human-playtest acceptance. Subjective listening, extended soak and isolated CPU/GPU/memory performance remain unperformed. The existing twelve-player bottom strip still crowds some value suffixes against lock icons in the inspected Avalanche frames; this remains open under the parent presentation requirement. Global results, remaining publishers and artistic environment acceptance retain their ledger statuses.

## Build

`Builds/TruckSports/TruckSports.exe` with all companion files. Full payload SHA-256 `521d56dd3e2ed5b521b27f12bd08dc532184aae567364167b90ed95aa873f7d0`; managed SHA-256 `b8877543d0920b9b52fe9afda1fa52806bee861aae4051575ba2eea982377e6c`. Exact compiled sources and the previous accepted build are preserved. Evidence resides in the matching `Evidence/2026-10-23-Finish-Outcome-Feedback` directory.

## VENDOR ASSETS AUDITED

Installed Heat notification prefab/API, DOTween, NWH public vehicle state and original race/course/replay systems.

## VENDOR ASSETS USED

Existing Heat UI, DOTween emphasis, NWH/PhysX driving, original navigation, cameras, portraits/theme and replay.

## RELEVANT ASSETS NOT USED

No replacement driving, navigation, UI tween or weather framework is needed. Measurement semantics belong to LWS.

## CUSTOM SYSTEMS CREATED

Explicit outcome kind in the existing receipt/journal, three rule-commit adapters, additive saved-panel installation and targeted accounting/runtime tests.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
