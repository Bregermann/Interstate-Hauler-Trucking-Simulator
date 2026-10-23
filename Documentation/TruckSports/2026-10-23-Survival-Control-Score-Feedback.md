# Survival and control score feedback — October 23, 2026

Q22-SKY-SCORE-01 remains **PARTIAL**. Hot Potato, Seesaw and King of the Hill now publish actual score changes into the existing saved Heat/DOTween presentation. This milestone extends the delivered Sumo and five-sport arcade receipts. Remaining publishers, results redesign and other sky/environment requirements remain open.

## Implemented

| Sport | Actual score authority and presentation |
|---|---|
| Hot Potato | The existing rules emit typed receipts immediately after committing +10 once-per-cycle pass, +20 cycle survival and +100 sole-survivor awards. Semantic cycle/slot IDs prevent duplicates. Existing contact, fuse, cooldown, elimination and score values are preserved. |
| Seesaw | Existing rules emit +1 survival milestones, +3 credited knockoffs, +2 physical balancing recoveries and +5 sole heat wins after committing them. IDs include heat, participant and actual milestone/victim/recovery. No ID construction or receipt emission on ordinary zero-award frames. |
| King of the Hill | Existing physical occupancy rules retain every fraction of control time. Presentation publishes completed ten-second milestones, with the actual fractional total and SEC unit; a winning remainder is flushed. No extra points, time rounding, per-frame notification or score multiplier. |

Each saved scene receives one sport-owned Heat notification using the existing rounded broadcast panel, truck portrait, participant color and DOTween emphasis. The existing journal snapshots stable match/participant identity, delta, total, reason and tier. Match callbacks unsubscribe on destruction; fresh matches clear control-notification milestones and journal state. Existing replay/results flows remain authoritative.

Explicit additive authoring retains all original scene documents: Hot Potato 10,122, Seesaw 5,229, Hill 5,058. Each adds 46 UI documents. The five changed original documents are the Canvas child list and four existing shared null/default fields serialized by Unity. All original world poses and physics documents remain preserved. The 925 unrelated pending files were rehashed unchanged. Original source prefabs and vendor code are untouched.

## Actual validation

42 native EditMode tests passed (0.7844 s): existing Hot Potato/Seesaw/Hill rules, receipt invariants and new award-stream accounting. The new tests reconstruct every actual score, check contact/recovery deduplication, simultaneous bonuses, end locks, heat continuation and reset.

One native PlayMode test passed (8.6619 s): two actual neutral-input NWH trucks placed on real Hill terrain. Actual supported uncontested occupation creates no receipt below ten seconds, then exactly one at ten and another at twenty, with fractional totals preserved. Pause freezes scoring; rematch changes match identity and clears milestones/journal. The placement fixture does not assign scores, rule clocks, eligibility or grounding.

All three complete Windows probes passed on the exact delivered payload:

| Sport | Checks / errors | Process duration | First full-match receipt count |
|---|---|---|---|
| HotPotato | 39 / 0 | 56.53 s | 35 |
| Seesaw | 49 / 0 | 97.54 s | 40 |
| Hill | 40 / 0 | 142.46 s | 17 |

Hot Potato runs a complete eight-truck match through seven real bomb eliminations and sole survival, physical contact passes, replay and rematch. Seesaw runs the complete three-heat four-truck physical match, actual joint tilt, mass-distribution fixtures, a software-input fall/quarry impact, replay and rematch. Hill runs the full default eight-AI/60-control-second match with physical navigation/contesting, relocation/warnings, replay, actual occupancy/airborne/compound-collider fixtures and rematch. Each compares recorded deltas with authoritative totals or completed control milestones. Software-input chase/cab ownership checks exercise one through four humans, optional AI views and AI-only spectators; these are not physical-device or human playtests.

Six actual Windows framebuffer images (720p/1080p per sport) were visually inspected. The real first committed award is captured after Heat's entrance animation; only the capture interval holds simulation, restoring the original speed afterward. Each panel is legible, opaque and screen-safe. No substitute camera or invented award is used.

### Retained camera-test failure and correction

The initial e5a3eb20... candidate passed Hot Potato 39/0 and Seesaw's full scoring/three-heat gameplay, but Seesaw recorded 46 checks/1 failure: an older assertion demanded all eight deck corners remain visible. The October21 global broadcast directive explicitly supersedes whole-arena fitting in favor of readable active action. Production cameras were not changed to satisfy the test.

The legacy-camera branch retains its all-corners check. The current action director is checked using established SportsBroadcastReadabilityProbe criteria: valid live primary, actual renderer height at least 90% of the configured minimum and below half the viewport, truck center within view margins, finite camera position and bounded lens. The original failed result/process receipt is retained. Of 318 exact compiled source inputs, 317 are identical between candidates; the sole changed file is the development-only SeesawProbe. All three full probes were rerun on the final candidate; earlier failures are not counted as passes.

No human gameplay, physical controller/wheel, subjective listening, sustained soak, isolated CPU/GPU or memory-leak acceptance is claimed. Existing package-cache test-DLL startup warnings remain unsuppressed.

## Delivery and remaining work

Windows x64: `Builds/TruckSports/TruckSports.exe` with all companion files. Full payload SHA-256 `dfc4d41b3a94a4e5b414c93db88cc0d651ec6b81040ec1b8d78f77f5525714df`; managed SHA-256 `a74a12f4a8ec0bbd715a196855890aae5cc56f991826fe2f33c380db38ee098e`. 549 files, 60 saved scenes, 318 exact owned compiled source files archived locally. Previous accepted build remains in Recovery. Evidence: `Documentation/TruckSports/Evidence/2026-10-23-Survival-Control-Scores/`.

Remaining registered-sport publishers and team-score semantics, full-tier/split-screen presentation coverage and performance/hardware/human acceptance remain open. The global results redesign is still MISSING. Artistic sky, lighting, snow/tracks/reflections and queued Carnival work retain their existing ledger statuses. Specialized Poker/Bowling/Kickball presentation is preserved.

## VENDOR ASSETS AUDITED

Installed Heat notification/controller/API and existing saved UI, DOTween, NWH tractors/grounding/contact, original three sports' rules, PhysX pivot, shared journal and Cinemachine action framing.

## VENDOR ASSETS USED

Heat notification transitions, DOTween emphasis, shared broadcast portrait/plates, NWH driving and physical support, original PhysX pivot/collisions and existing Cinemachine director.

## RELEVANT ASSETS NOT USED

No replacement vehicle, UI-animation, physics, camera or scoring framework was required. Existing audio and replay systems remain authoritative.

## CUSTOM SYSTEMS CREATED

Extended existing LWS rule-commit receipt adapters, milestone pacing, explicit additive authoring and acceptance helpers. Added award-accounting and actual grounded-occupation tests.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
