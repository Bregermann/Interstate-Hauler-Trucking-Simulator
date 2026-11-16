# Alpha 0.9 Release Foundation — implementation checkpoint

Authority: `Requests/2026-11-16-Alpha09-ReleaseFoundation.md` and the root acceptance ledger. This is implementation evidence, not a release certification or replacement backlog.

## Implemented so far

- One persistent game-night owner, four independent seats, stable profile/device ownership, host selection, ready checks, optional AI and guest profiles. No played-sports checklist.
- Shared Heat/uGUI lobby, profile creation/rename/color/icon selection, deletion confirmation, explicit reconnect-seat selection, disconnected-host takeover and final session standings. Existing DOTween and broadcast theme reused.
- Owner-controller on-screen keyboard with detached edits, TMP validation, commit once, cancellation, disconnect rollback and release-gated focus restoration. The inactive validator uses protected TMP edit positions; no TMP source modifications.
- Existing profile preference registry extended compatibly to schema3 with avatar and date fields. Original save keys, migration backups and historical match receipts retained.
- Common `SportsEventSession` adapter carries selected identities and controllers into sports, gates starts and returns to a fresh ready check for rematch/restart. Interrupted matches cannot award points.
- Read-only typed final-result adapters cover additional existing rule owners. Jenga's collapse loser is excluded from winners; Goose uses series totals rather than the last-round winner; cooperative workshops have no individual champion. Three rules owners expose read-only final placing queries; gameplay scoring is unchanged.
- Exactly-once session winner points are modeled independently of raw sport scores; AI receives no human-profile award.

## Actual verification

- `Logs/TruckSports/Foundation09/core-rules-01.xml`: Unity EditMode10/10 passed. Includes38party-rule assertions and9text-buffer cases.
- Current standalone party scenario:38/38 passed. Restart scenario separately passed fresh readiness, interrupted-result rejection and subsequent single award.
- `controller-ui-01`: canceled before tests because restricted-process licensing reconnect stalled. Not a test pass.
- `controller-ui-02.xml`:0/6 passed. Two actual causes identified: detached TMP public caret setter required an absent rendered mesh, and Unity missing-component wrappers were mishandled with C# null coalescing. Both corrected in project code.
- `controller-ui-03.xml`:2/6 passed. Real saved front-end → two virtual controllers → profile creation (`ace`) → profile reload → host-only sport selection → Musical Parking controller/identity binding → lobby return passed. Commit-once/held-submit gating passed. Four assertions were affected by fixture counters not reset between NUnit cases; fixture corrected.
- `controller-match-04.xml`:5/5 passed in69.421s (Editor0,117.14s). All four corrected keyboard cases pass. Actual mixed-human/AI Dance-Off completes through the normal DSP/song judge; AI wins earn no human-profile session points; rematch returns to fresh setup/readiness, starts a new match ID, and interruption returns both assigned controllers to the lobby without award. No gameplay scores or finish states were injected.
- Latest source compiles in the actual Editor, including additional result bridges. Existing vendor warnings are retained; compilation is not gameplay acceptance.

Virtual Input System devices are software tests. Physical controllers, listening, rendered Windows UI, complete controller-only game-night acceptance and a new0.9Windows build remain unverified.

## Remaining active work

The first actual common-session match/results/rematch/menu loop passes. Next: supported permanent statistics and report finalization. Integrate the separate legacy session owners, finish catalog navigation coverage, inspect rendered layouts and deliver a bounded representative Windows game-night build. The0.8candidate remains unlaunched; the safe installed0.7build is retained. Future22/28sports and0.8scenery/QA are not active tasks.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** existing Heat button/menu implementation, TMP input validation APIs, Unity Input System UI/device APIs, DOTween presentation, existing preference/profile and sport lifecycle adapters.
- **VENDOR ASSETS USED:** Heat/uGUI, TMP, Input System, DOTween. NWH and individual sport rules/physics remain their existing authorities.
- **RELEVANT ASSETS NOT USED:** Pixel Crushers career-save orchestration is not substituted for the established independent Sports profile preference boundary; world/traffic/weather packages are unchanged by this flow.
- **CUSTOM SYSTEMS CREATED:** game-night semantics and vendor/UI/session coordination, typed result projections and focused regression tests.
- **VENDOR SOURCE MODIFIED:** none.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** none; no vehicle physics, UI animation engine, device backend or career-save framework replacement.
