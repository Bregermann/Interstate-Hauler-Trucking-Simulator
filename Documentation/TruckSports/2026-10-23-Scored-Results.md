# Eight scored-mode results boards — October 23, 2026

Q22-SKY-RESULT-SCORED-01 adds saved animated results cards to Whack-a-Mole, Magnet Mayhem, Sheep Herding, Seesaw, King of the Hill, Floor Is Lava, Hide and Seek and Hungry Haulers. The parent global results and Q22-SKY requirements remain partial. This is not completion of Alpha 0.6.0 or the queued 0.7.0 directive.

## Implementation

SportsScoredResults reads each mode's existing rules after the match finishes. Its immutable snapshot retains stable participant IDs, names, colors, AI/human ownership, original placings, actual final measurements and relevant statistics. No points, ranks, physics or AI decisions are calculated by presentation. Existing Heat navigation, results data, highlight buttons, rematch and return actions remain in place. DOTween reveals and sorts UI cards and counts displayed measurements; only display values animate.

| Sport | Actual displayed information | Ranking preserved |
|---|---|---|
| Whack-a-Mole | Points; total physical target hits in summary | Original scores and shared placings, including negative scores |
| Magnet Mayhem | Final scrap points | Original scores and shared placings |
| Sheep Herding | Sheep secured; last delivery time or no delivery | Original delivery-time tiebreak |
| Seesaw | Points, heat wins, knockoffs, recoveries; heat count | Original total points and ties |
| King of the Hill | Fractional control seconds; declared winner | Original control ranking and explicit winner |
| Floor Is Lava | Points and actual survival seconds | Original survival-first ranking and explicit survivor, not point-only ranking |
| Hide and Seek | Points, tags, survived rounds and sweeps | Original point ranking and ties |
| Hungry Haulers | Final banked delivery points | Original point ranking and ties |

Saved integration is additive: each scene gains 421 UI documents. Exactly one original ResultsPage RectTransform document per scene changes to reference its new child. Every original object, world pose and physics document remains. Before scene/meta backups, exact original-document diffs and the 925-file unrelated-work preservation guard are retained. Truck Taxi and vendor source are unchanged. Both previously delivered mountain/course boards remain integrated.

## Actual validation

147 targeted native EditMode tests passed in 1.4207 seconds. They exercise the shared result/finish contracts and all eight original rule suites.

Seven native PlayMode tests passed in 52.0515 seconds. The new test explicitly seeds synthetic values in all eight real saved scenes and tests rule-to-UI bindings, duplicate-name identities, ties, negative points, Herd's time tiebreak, Hill's fractional control and Lava's survival-first ordering. It checks controlled 720p/1080p card bounds, text overflow, unchanged rule arrays and rematch cleanup. The other six tests cover two compact HUD layouts, three arcade/queue cases and one actual team contact regression. Synthetic results are not physical-match or human acceptance. All eight controlled 720p boards and selected 1080p boards were inspected.

The eight original Windows probes run complete autonomous matches, inspect the new board after genuine completion, and then retain their original replay, software-input/contact fixtures and rematch checks. The board is compared with actual rules for every rank, winner, identity and final measurement. Real Windows backbuffers at 1280×720 and 1920×1080 are captured and checked for card, heading, button and text fit.

| Sport | Checks / errors | Duration |
|---|---|---|
| Whack | 54 / 0 | 46.72 s |
| Magnet | 56 / 0 | 69.14 s |
| Herd | 70 / 0 | 98.07 s |
| Seesaw | 60 / 0 | 85.98 s |
| Hill | 51 / 0 | 211.12 s |
| Lava | 59 / 0 | 45.83 s |
| Hide | 65 / 0 | 99.23 s |
| Hauler | 63 / 0 | 81.28 s |

Total: 478 Windows checks, zero reported errors on the exact payload below. All sixteen fresh Windows result frames were inspected. The probes' controlled physical/input fixtures remain explicitly distinct from their completed autonomous matches. No physical gamepad/wheel, human playtest, subjective listening, isolated profiler or extended soak acceptance is claimed. Full-count physical matches are exercised; this batch does not claim every player-count/input/aspect combination.

## Delivery

Windows executable: `Builds/TruckSports/TruckSports.exe`, with its complete companion payload.

Full payload SHA-256: `51fbaa270be74a6ca04a99f6e3324db94cd4c8f7b5aa26b6553a83c4aaefc65f`.

Managed assembly SHA-256: `dbff55420845011ef7e2a3be87b5c51ed7409cbc38e305ff49ea63324f8c61b9`.

Exact source inputs, receipts, scene diffs and images are retained under `Documentation/TruckSports/Evidence/2026-10-23-Scored-Results/`. The prior accepted build remains recoverable. Scoped source comparison documents every changed input. Remaining global result coverage, richer winner/round celebrations and sky/time/environment acceptance remain recorded under their existing permanent IDs. The next queued directives remain Carnival 0.6.0, then Impossible Games 0.7.0.

## VENDOR ASSETS AUDITED

Existing Heat results/page/button architecture, shared portrait/theme assets, DOTween APIs and established result/replay ownership.

## VENDOR ASSETS USED

Heat page/navigation/buttons and theme; DOTween UI reveal/ranking/value animation; existing truck portraits and match-end feedback. Original NWH driving and PhysX continue unchanged.

## RELEVANT ASSETS NOT USED

Heat does not supply these sports' authoritative ranks or statistics. LWS provides read-only adapters rather than a replacement menu, physics or replay framework.

## CUSTOM SYSTEMS CREATED

Eight read-only sport adapters using the existing immutable snapshot and saved result board; one targeted eight-scene lifecycle fixture; development assertions in the original Windows probes. No additional gameplay authority.

## VENDOR SOURCE MODIFIED

No.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

No.
