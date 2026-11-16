# Read-only four-mode final results source

`LWS.TruckDragonKingdom.DragonFinalResultsSource` derives from the existing SportsFinishResultsSource. Its only product change is a new mode-owned adapter file and metadata. Parent owns the existing base's virtual extension, explicit scene opt-in, Heat/DOTween board installation and validation. Compilation against the actual extended base and retained rules assemblies passes with zero diagnostics; native UI/results verification is pending.

The inherited session, MatchId, ResultReady and OriginalResult remain the existing board's lifecycle interface. TryCapture returns false before the session finishes, for unsupported modes, or without a matching prepared roster/rules owner. It never starts/finishes a match, saves anything, invokes an event, modifies a score, or parses Results text. Rows receive the actual participant objects, retaining stable IDs, display names, colors, bot ownership and duplicate-name separation. The existing immutable snapshot performs only its normal place/slot presentation ordering.

| Mode | Authoritative data and display |
|---|---|
| Egg Delivery | Exact Rules.Winners and cargo Score in PTS. Delivered rows use existing SportsResultPlacings over those scores; undelivered rows have no placing and explicit DNF detail. Condition, RoughImpacts, DeliveredBaby and DeliverySeconds are copied from each record. No delivery time is fabricated for DNF cargo. |
| Hoard Heist | Existing shared-maximum winner convention and SportsResultPlacings over actual Scores in VALUE, including the owner's all-zero shared top. DeliveredCount and exact total inventory minus received count describe global unbanked treasure. StopGame clears rack owners, so the adapter never invents a per-crew unbanked load/value. |
| Dragon Escape | Exact Placements and ExtractionTimes. Adjusted extraction seconds already include recovery penalties and are never recomputed or used to replace same-step owner placements. DNF has no invented time/place. Cooperative success/failure uses ActualExtractions versus crew count; it produces no fabricated individual winners or first places. |
| Dragon Riders | Exact ResultPlace, Finished, FinishTime, Penalty, Passed and Retired. FinishTime already includes penalties. Only a genuinely finished first-place crew is a winner; no-finisher progress placings remain visible without a fake winner. Incomplete rows use RETIRED/DNF instead of a fictitious finish time. |

Existing SportsResultsPresentation supplies the actual Heat page, original participant rows, readable board and DOTween transitions. This adapter creates no UI or results framework. Parent's explicit migration/tests must establish saved-scene identity preservation, ties, duplicate names, zero/no-finish cases, real final captures and replay/rematch behavior before presentation acceptance. Software/native results are separate from human legibility review.

- VENDOR ASSETS AUDITED: installed Heat/DOTween integration and existing SportsFinishResultsSource, SportsResultsSnapshot/Row, SportsResultPlacings and SportsResultsPresentation; all four authoritative mode owners/rules.
- VENDOR ASSETS USED: existing Heat/DOTween board and immutable Sports results/participant APIs.
- RELEVANT ASSETS NOT USED: legacy Results-string parsing, a new scoring authority, or any new UI framework.
- CUSTOM SYSTEMS CREATED: one read-only four-mode results adapter.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
