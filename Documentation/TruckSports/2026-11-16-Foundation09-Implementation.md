# Alpha 0.9 Release Foundation — implementation checkpoint

Authority: `Requests/2026-11-16-Alpha09-ReleaseFoundation.md` and the root acceptance ledger. This is implementation evidence, not a release certification or replacement backlog.

**Current delivery status: BLOCKED Windows release acceptance.** The85-scene Alpha0.9 development candidate builds successfully and is available at `Builds/TruckSportsCamera/TruckSports.exe` with its companion files. A two-software-controller Windows run completes Dance-Off and Whack-a-Mole, statistics and report export. Both profiles retain their exact saved statistics after executable relaunch. The lobby title-canvas overlap is fixed and visually confirmed at720p. Ordinary menu quit still exits with native code `0xC0000005`, including a comparison without input isolation, synthetic controllers or an active match. A visible full-match run separately stalls in Dance-Off until its480-second deadline. No clean Windows release pass is claimed. `Builds/TruckSports/TruckSports.exe` remains the previous working installation; no unsafe replacement was performed.

Local implementation checkpoints: `7acd4789`, `caf5a1fe`, `218b5ea3`, `e7163a98`, plus the final evidence checkpoint recorded in Git. The user approved the exact53-file public checkpoint `6d588978d98d0f8351b81f62ea7bcf9d1f78c573`; it was pushed normally and its remote tip verified on `develop/truck-sports-alpha-0.9.0-foundation`. Later statistics/catalog/delivery changes remain locally preserved pending their separate exact public scope. No private ancestry, unapproved payload or vendor source was pushed.

**Actual remaining issues:** native shutdown access violation (module/cause not established; no fresh player dump or installed native debugger was found in the bounded check), visible Dance-Off interruption (cause unconfirmed), physical-controller families/reconnection and listening, complete specialized-menu/controller coverage across85scenes, and1080p/ultrawide visual acceptance. No Editor upgrade, vendor patch, new world work or unrelated validation cycle was attempted. This is an honest alpha checkpoint, not completion of every acceptance criterion.

**Latest evidence:** `Evidence/Foundation09/README.md` links inspected lobby, keyboard, saved-statistics and retained before-fix/initial-gameplay captures. Windows02's actual report contains two unique complete matches, real AI winners, zero human point awards, two human losses each and a telemetry end event. Windows04's short visual retake and the separate relaunch comparison pass their own assertions; all process-exit failures remain recorded. The latest scripts-only build exits0; existing saved scene/content data is reused. See the root ledger for permanent IDs.

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

The first actual common-session match/results/rematch/menu loop passes. Permanent statistics and report finalization are implemented; their targeted evidence is recorded below. Integrate the separate legacy session owners, finish catalog navigation coverage, inspect rendered layouts and deliver a bounded representative Windows game-night build. The0.8candidate remains unlaunched; the safe installed0.7build is retained. Future22/28sports and0.8scenery/QA are not active tasks.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** existing Heat button/menu implementation, TMP input validation APIs, Unity Input System UI/device APIs, DOTween presentation, existing preference/profile and sport lifecycle adapters.
- **VENDOR ASSETS USED:** Heat/uGUI, TMP, Input System, DOTween. NWH and individual sport rules/physics remain their existing authorities.
- **RELEVANT ASSETS NOT USED:** Pixel Crushers career-save orchestration is not substituted for the established independent Sports profile preference boundary; world/traffic/weather packages are unchanged by this flow.
- **CUSTOM SYSTEMS CREATED:** game-night semantics and vendor/UI/session coordination, typed result projections and focused regression tests.
- **VENDOR SOURCE MODIFIED:** none.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** none; no vehicle physics, UI animation engine, device backend or career-save framework replacement.

## Statistics and reporting continuation

Schema4 statistics now use the existing registry/backups, stable IDs and a persistent completed-match deduplication list. Competitive outcomes, practice/cooperative participation, visits, placements, explicit comparable records and dates are separated. Guests/AI receive no permanent human-profile awards. Controller profile pages expose lifetime/per-sport data and all record pages.

The existing telemetry observer now tags game nights and supplies structured match reports, settings/participants/results/awards/interruption records, bounded events/performance summaries and local JSON/text export with atomic replacements/backups. The final screen supports X export. See `GameNightProfilesAndReports.md`.

Actual new verification:

- Native statistics05: no tests executed; isolated source cohort lacked the existing telemetry script. The explicit dependency was copied.
- Native statistics06: no tests executed; compiler handoff timed out at600seconds. Failure retained. Strategy changed to portable PlayMode tests with two workers.
- Complete-cohort cached Main compile: exit0. This is compilation evidence only.
- Native statistics-match07:10/11passed,89.968s; Editor2/253.78s. All10statistics/migration/ranking/restart cases passed. The actual mixed human/AI match passed its completion, rematch, interruption, saved-statistics reload, AI exclusion, device-event, JSON/text export and controller-export assertions. Its final live JSONL read failed because the test reader did not share access with the active writer.
- Post-run retained-output inspection confirms the same session's completed/interrupted records, zero human AI-win awards, device events and `game_night_ended` stream record. The original failed XML is unchanged. The test reader now uses explicit `FileShare.ReadWrite`; that change awaits the next relevant integration run.

No new Windows build, physical-controller, listening or human-playtest acceptance is claimed. Separate legacy session adapters, controller navigation/rendering and final representative Windows delivery remain active. Three subsequent setup-focus/mouse-ready changes are source-only pending the catalog integration check.

## Catalog integration and Windows candidate

All seven legacy owners now use the shared party identity/ready/lifecycle adapter: Bowling, Cornhole, Cheese Rolling, Hockey, Air Hockey, Wallball and Billiards. Their existing rules, input readers, vehicle controllers and specialized result screens remain authoritative. Read-only final-result bridges use complete bowling bonuses, full Billiards series results and actual team winners. Cornhole/Wallball reuse their original result projections through static helpers.

Native legacy-match08: four-controller case passes all seven saved scene starts/returns, capacity/spectator mapping, unready-start rejection and stable identities; Hockey accepts a different replacement device without losing the driver. The changed common case fails on a real one-frame destroyed-session poll during scene unload. `Loaded` now clears the binding before delayed scene setup. Focused common-transition09 then passes1/1 in69.419s (Editor0/131.58s), including normal match/rematch/statistics/device/report export and the corrected live telemetry read. The original08failed XML remains unchanged; the seven-scene pass was reused.

Setup now skips managed read-only controls, permits host Y/F6 to switch crew/AI spectating, and prevents shared mouse readiness for controller-owned seats. Legacy preset controls cannot silently replace lobby assignments. A replacement controller's Start can claim a missing driver during a match; multiple missing drivers require an explicit choice.

Sports-only resource metadata is prepared as Alpha0.9.0 / Release Foundation. Host ProjectSettings are not permanently changed. The prior unlaunched0.8candidate is preserved in `.sports-validation/Recovery/Alpha08CandidateBeforeFoundation09-20261116-131913`; the installed working build is untouched. The established `SportsBuilder.BuildFocused` is building the replacement candidate without scene regeneration. An opt-in Windows probe will exercise software controllers, actual UI, two complete sports, statistics/report export and720p/1080p captures. Its source compiles; no player-build result is claimed yet.

## Windows delivery continuation

The full Windows development candidate succeeded: zero build errors,245warnings,3,502,139,747bytes; Editor exit0 after2404.10seconds. Build summary time is27:36.834, excluding Editor startup/import. Log: `Logs/TruckSports/PlaytestRepair/foundation09-windows-build.log`. Candidate output: `Builds/TruckSportsCamera/TruckSports.exe`. This is build success, not launch acceptance. The installed working build remains untouched.

Follow-ups add analog navigation to profile/confirmation pages, hide unsupported AI spectating in Billiards practice, and retain overflow counts in the existing telemetry summary. The existing builder now accepts explicit `-tsBuildScriptsOnly` only when a complete player already exists at the same output. Its default remains the full development build. Unity's installed API documentation was checked; the update log confirms content data is reused. No scenes, assets, vendor code or project version were changed for these follow-ups.

The opt-in Windows check is scoped to isolated validation preferences, two actual sports, rendered720p/1080p captures and a separate executable relaunch that compares saved profile/statistics data. Its telemetry component uses the persistent probe owner. No score, winner or completion-state injection is used. Actual results remain pending.
