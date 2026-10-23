# Survivor, tournament, finish and team results — October 23, 2026

Q22-SKY-RESULT-CONTEST-01 integrates saved animated result boards into the remaining eight expansion sports. The complete eighteen-sport expansion now has the new typed results integration across its saved scenes. Other registered sports, richer round/winner celebrations and the parent Q22-SKY requirements remain open. Alpha 0.6.0 and the queued Alpha 0.7.0 are not declared complete.

## Actual result semantics

| Sport | Preserved result and actual statistics |
|---|---|
| Hot Potato | Original last survivor wins, even if an eliminated truck has more points. Points, passes and survived cycles are copied. Defeated trucks show OUT; no new loser-ranking rule is invented. |
| Balloon Battle | Original survivor wins; mutual final pops retain a draw with no invented gold winner. Points, credited pops, eliminations and remaining balloons are read from the rules. |
| Tetherball | Original win-count placements, series winner, round wins and winding direction. |
| Jousting | Original point placements and shared tournament ties; actual lance hits, central hits and knockdowns. |
| Frogger | Original course/tiebreak placing and declared winner; actual wins, successful/total recorded attempts and best successful crossing. Failed crossings have no invented time. |
| Red Light Green Light | Original legal winner and finisher placements, actual crossing times and finish points; incomplete/eliminated attempts have no numeric finish time. No legal finisher means no invented winner. |
| Volleyball | Original team winner and team totals, explicitly marked TEAM PTS; actual team aerial hits, saves and three-touch returns. Participant identities remain visible. |
| Freeze Tag | Original team round wins and shared outcomes, explicitly marked TEAM WINS; each participant's actual individual tags and rescues. |

SportsContestResults supplies immutable read-only snapshots to the existing Heat/DOTween result board. The three match-level result APIs expose or reuse existing Tether/Joust/Red Light calculations. Original standings and scoring remain authoritative. Team totals are presentation labels; they never become individual score awards. Existing player names, stable IDs, colors and AI/human labels are copied independently of duplicate names.

DOTween animates card reveal, movement into the supplied ranked order and displayed totals. No truck physics or score arrays are animated. Existing result data, focus/navigation, highlight/replay buttons, rematch and menu actions remain. Previous mountain/course and scored-mode boards are preserved. Poker, Bowling and Kickball specialized sequences are unchanged.

Each of the eight scenes adds 421 UI documents and changes exactly one original ResultsPage RectTransform child reference. Original scene objects, world poses, physics and source GUIDs are retained. Sixteen original scene/meta files and exact document diffs remain recoverable. The 925 unrelated pending files are rehashed unchanged.

## Executed tests

135 targeted native EditMode tests passed in 1.0106 seconds: shared result/finish contracts and the eight original rule suites.

Final native PlayMode run: six passed, zero failed, 72.7617 seconds. The new fixture exercises ten explicitly synthetic saved-scene result scenarios: all eight modes at maximum supported rosters, plus a mutual-pop draw and an all-eliminated Red Light match. Teams are explicitly configured four versus four. Assertions check survivor authority over displayed points, shared winners, original placings, nullable DNF measurements, individual/team-stat ownership, stable duplicate-name identities, actual text, bounds/TMP overflow at controlled 720p/1080p, unchanged rule arrays and rematch cleanup. The other five tests retain the prior eight scored-scene bindings, three arcade/queue cases and an actual team contact regression.

The first passing six-test run (69.7796 seconds) covered nine scenarios but Volleyball normalized to its default two versus two. The next passing run (72.7993 seconds) added the no-legal-finisher case and consistent synthetic balloon state. Final coverage explicitly asserts the maximum roster and four-versus-four team setup. These are coverage refinements, not fabricated failed gameplay reproductions. Initial passing XML is retained separately. Synthetic states are not physical-match or human acceptance.

All ten final controlled 720p scenarios and selected 1080p boards were inspected. Separate original Windows probes run complete physical matches or the original course/series workload, then inspect the new board before replay. They preserve their original software-input/contact fixtures and rematches. Each displayed result is compared with the actual completed game's identities, rules, declared winners and final measurements. Fresh actual Windows backbuffers are captured at 1280×720 and 1920×1080.

| Sport | Checks / errors | Duration |
|---|---|---|
| HotPotato | 50 / 0 | 56.87 s |
| Balloon | 52 / 0 | 37.36 s |
| Tether | 37 / 0 | 60.25 s |
| Joust | 39 / 0 | 34.43 s |
| Frog | 73 / 0 | 423.09 s |
| RedLight | 54 / 0 | 57.03 s |
| Volley | 53 / 0 | 119.08 s |
| Freeze | 56 / 0 | 123.59 s |

Total: 414 exact-payload Windows checks, zero reported errors. All sixteen fresh result frames were inspected. The original Volleyball autonomous match uses two versus two; four-versus-four result layout is checked separately in the controlled native fixture. Frogger's existing probe drives each of the five courses with one autonomous truck and separately checks software multiplayer/cameras; its new completed-board capture follows the first course. Its eight-participant result layout is synthetic. This batch does not claim full physical eight-player Frogger or four-versus-four Volleyball matches, every input/aspect combination, physical controller/wheel acceptance, human playtesting, subjective listening, isolated profiling or an extended soak.

## Build and preservation

Combined executable: `Builds/TruckSports/TruckSports.exe`, with all companion files and 60 scenes.

Full payload SHA-256: `2d02fc1fcaef6c6e9ef82631e0efb5360468747cf4934f5b04eea8da30c23eb1`.

Managed assembly SHA-256: `c2cf14400992a01064dfdac126b97669cfcfaf3a6a585ce83ccd2d52ce92dd62`.

Source manifests, exact compiled archive, scene diffs, prior build recovery and evidence remain retained. Public evidence: `Documentation/TruckSports/Evidence/2026-10-23-Contest-Results/`. Scoped delivery preserves unrelated Truck Taxi work and vendor files. Remaining registered-sport integration and environmental acceptance continue under existing parent IDs, followed by Carnival 0.6.0 and Impossible Games 0.7.0.

## VENDOR ASSETS AUDITED

Existing Heat results/page/button architecture, shared portraits/theme, DOTween public APIs and match/replay ownership.

## VENDOR ASSETS USED

Heat navigation/buttons/theme; DOTween UI reveal, ranking and value animation; existing truck portraits and match-end feedback. NWH/PhysX gameplay remains authoritative and unchanged.

## RELEVANT ASSETS NOT USED

Heat does not supply these sport-specific winners or statistics. LWS adapters supply semantics without replacing the menu, vehicle physics or replay framework.

## CUSTOM SYSTEMS CREATED

Eight read-only contest result adapters, three read-only result APIs reusing existing calculations, targeted synthetic fixtures and completed-board assertions in original development probes. No new gameplay authority.

## VENDOR SOURCE MODIFIED

No.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

No.
