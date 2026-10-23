# Ranked mountain and course results — October 23, 2026

Q22-SKY-RESULT-RACE-01 adds saved, animated result cards to Avalanche Escape and Ninja Warrior. The global results requirement remains partial; no collection-wide completion is claimed.

The rules provide every placement, winner and statistic. The immutable snapshot copies stable participant IDs, names, colors, AI/human labels, explicit placings, winner flags and actual measurements. Duplicate names do not merge participants. Tied leaders remain separate gold cards. Incomplete attempts have no invented finish time. Sorting uses the supplied placing and stable slot order; it never evaluates a new winner or changes points.

Avalanche shows actual whole-truck escape times and surviving distance for unsuccessful competitors, with the actual elimination reason. If nobody escapes, the heading explicitly celebrates furthest survivors rather than claiming a successful escape. Ninja shows actual penalty-adjusted course time, cleared obstacles, falls and the original five-second fall penalty. Incomplete courses display that state. If nobody finishes, the heading identifies furthest athletes.

DOTween reveals the participant cards, animates their move into ranked order and counts displayed measurements up to their real final values. Only UI is animated. The original Heat results page, input/focus, replay/rematch/setup/menu buttons, match-end sound and authoritative Results data remain intact. Legacy text is hidden only after a valid typed snapshot exists, restored when the board disables, and a new match clears the snapshot. Original Poker, Bowling and other specialized result sequences are untouched.

Both scenes add 421 UI documents and alter exactly one original document: a child reference on the ResultsPage RectTransform. Every original object, world pose and physics document is retained. Original scene/meta pairs and precise diffs are preserved; the 925 unrelated pending files are rehashed by the delivery guard. New metadata GUIDs were copied before first import.

## Actual tests and visual corrections

51 native EditMode tests passed in 0.7147 s, including eight new cases for immutable roster copies, explicit ties, stable duplicate-name identities, DNF states, invalid measurements, duplicate IDs/slots and winner-place validation, plus existing finish/point/team/race-rule regressions.

The first six-test native run passed 5/6. The twelve-card Avalanche board overlapped its existing highlight button at y=-245. Compact twelve-card rows now use 50-pixel height and 54-pixel spacing: the final row bottom is -200, above the original button top -221. Original navigation stays in place. The unchanged outer-card assertions then passed 6/6 in 29.5506 s.

Inspection of those actual controlled renders found a separate inner-text issue: the 42-pixel primary value rectangle crowded Ninja statistics. The before images and exact source are retained. Primary values now have their own 23/28-pixel line, separate from the detail line, with NoWrap/autosizing. Added assertions check inner-lane separation and actual TMP overflow. The next six-test run passed in 28.8456 s. Two are explicitly synthetic rule-to-saved-UI fixtures: twelve-driver tied Avalanche results and eight-athlete Ninja finishes/penalties/incomplete attempts. Two exercise real mole/court contacts, and two are explicitly synthetic receipt-order regressions. Controlled 720p/1080p frames were inspected. These UI fixtures are not physical-match or Windows-rendering acceptance.

The initial `90ee49fd...` payload passed 113 Windows checks for twelve-truck Avalanche and solo Ninja matches. Its earlier controlled eight-participant layout also passed, but a subsequent six-player configuration audit found the remaining one-column overlap with the original replay button. A dedicated native six-player reproduction failed in 2.7940 s. The two-column threshold now starts at six participants. The source/rule assertions are unchanged; coverage expands to every supported Avalanche 1–12 and Ninja 1–8 roster, plus the explicit six-player regression, at both controlled resolutions. Initial candidate receipts, four Windows frames, failed XML and exact source/fixture are retained. This candidate is not misreported as having passed every count.

The final native seven-test run passed in 80.1696 s and includes 21 explicit synthetic results configurations across the two sweeps and six-player regression, alongside the four ordinary score/team tests. No synthetic state injection is counted as physical-match acceptance. Both original full Windows probes were rerun on the final exact payload:

| Sport | Checks / errors | Duration |
|---|---|---|
| Avalanche | 59 / 0 | 255.55 s |
| Ninja | 54 / 0 | 134.53 s |

Their original physical race/course, route/obstacle, input, recovery, replay and rematch checks remain intact. Additional checks read the actual completed match and compare every displayed rank, tied winner, measurement, stable identity, name/color and AI flag with the original data. Actual 720p/1080p Windows backbuffers show the final cards; checks verify screen bounds, separate rows/headings/controls and no TMP text overflow. Four fresh Windows result frames are inspected independently of the six selected controlled Editor frames. No clocks, positions, finishes or results are injected in the original full Windows runs.

No human/physical-controller/wheel, subjective listening, prolonged-soak or isolated performance acceptance is claimed. Remaining result coverage, richer sport-specific victory presentation, other weather/time/artistic requirements and compact twelve-player gameplay-strip crowding remain open under their parent entries.

## Build and preservation

`Builds/TruckSports/TruckSports.exe` with all companion files. Full payload SHA-256 `dbaa003084138b3e442c88ecb84892e9822e6692c63891b63b70ea88a21744b7`; managed SHA-256 `fe3135d75362625bbee48f5ef4e0131ad0365b1e95bab03c68a015b7c5c72f71`. Exact compiled sources and the previous accepted build remain recoverable. Evidence is in `Documentation/TruckSports/Evidence/2026-10-23-Ranked-Race-Results/`.

## VENDOR ASSETS AUDITED

Installed Heat results/page/button and notification architecture, DOTween APIs, existing portrait/theme and result/replay ownership.

## VENDOR ASSETS USED

Existing Heat page/navigation/buttons and theme, DOTween UI fades/ranking/counting, existing truck portrait and match-end feedback. NWH driving and PhysX remain unchanged.

## RELEVANT ASSETS NOT USED

Heat does not own game-specific ranking/statistics; LWS supplies read-only result semantics. No replacement menu, vehicle, physics or replay framework is introduced.

## CUSTOM SYSTEMS CREATED

Immutable result snapshot/rows, two explicit read-only rule adapters, opt-in saved result view/authoring and targeted model/UI/Windows acceptance.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
