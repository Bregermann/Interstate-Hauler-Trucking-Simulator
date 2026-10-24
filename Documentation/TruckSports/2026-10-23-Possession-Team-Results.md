# Possession and team results — October 23, 2026

Delivered implementation and automated acceptance for Q22-SKY-RESULT-POSSESSION-01 and Q22-SKY-RESULT-NAME-01. The parent Q22-SKY, Alpha 0.6.0 and Alpha 0.7.0 scopes remain incomplete; human and physical-device acceptance is separate.

Five saved scenes use the existing graphical Heat results presentation, truck portraits, participant colors and DOTween animation. Read-only result adapters retain the original matches as the scoring and winner authority.

| Scene | Displayed facts |
|---|---|
| Buzkashi | Original winning team, shared team deliveries, cargo deliveries and legal rope breaks. |
| Keepaway Arena | Individual cumulative possession seconds, original winner, pickups, steals and comic explosions. |
| Keepaway First Person | Same match authority and result semantics as Arena; existing first-person setup retained. |
| Kickball | All twelve participant identities, original winning team and shared team runs, physical kicks, catches and base touches. |
| Trampoline Dodgeball | Original first-three winning team, shared team round wins, drawn rounds and actual final-round survival. |

Shared statistics are explicitly labelled TEAM values. Results do not grant each teammate a separate score, invent loser finish times or change tie/possession rules.

Each scene adds 426 UI documents and changes only the original ResultsPage child-reference document. Original scene objects, transforms, world geometry and physics serialization are preserved. Seven original Match/Rules files are byte-identical to the accepted public baseline. The original physical probe simulation and assertions remain intact; the added result check runs only after genuine match completion. The 925 unrelated pending files remain protected by exact hashes.

## Actual bug and correction

The new forty-wide-character Buzkashi fixture reproduced player-name glyphs crossing the TEAM DELIVERIES score even though TextMeshPro reported no text overflow. With NoWrap and Overflow, TMP can render outside horizontal bounds once auto-sizing reaches its minimum.

The shared results name label now uses TMP's existing Ellipsis mode. Full stored names and result identities are preserved. The minimum name size remains 14 points; numeric values and their units are unchanged. No vendor source was edited. The original failing XML, log and screenshot are retained alongside the fixed captures.

## Executed verification

- Native EditMode: 45 passed, zero failed.
- Focused rendered PlayMode reproduction: one expected failure, preserved as evidence.
- Focused fixed PlayMode: one passed, zero failed, covering ten saved-scene configurations.
- Final rendered regression: 15 passed, zero failed; 237.2893953 seconds of test execution, Editor exit 0. This includes the earlier results families affected by the shared name policy.
- All twenty fixed controlled images at 1280×720 and 1920×1080 were individually inspected. Cases include maximum rosters, asymmetric teams, duplicate display names and forty-wide-character names. Glyph assertions check actual name/score separation.

The new board fixtures deliberately inject synthetic final statistics to validate binding and layout. They do not establish complete physical-match or Windows-backbuffer acceptance. Four original physical Windows workloads passed on the exact candidate: Buzkashi, both Keepaway variants, twelve-truck Kickball and all three Dodgeball difficulty configurations.

| Windows workload | Checks / errors | Seconds |
|---|---|---|
| Buzkashi | 18 / 0 | 91.54 |
| Keepaway | 149 / 0 | 163.52 |
| Kickball | 32 / 0 | 264.1 |
| Dodgeball | 50 / 0 | 482.3 |

Total: 249 checks, zero errors. All fourteen completed-match backbuffers at 720p and 1080p were individually inspected. These runs preserve original competition, physics, replay and rematch assertions. An explicitly coordinated independent headless build overlapped part of validation, so elapsed times are not isolated performance measurements. Retained historical images in legacy raw folders are not treated as current evidence.

No human gameplay, physical controller/wheel, subjective listening, isolated performance or extended-soak acceptance is claimed.

Canonical Windows delivery: `Builds/TruckSports/TruckSports.exe`, containing 549 build files and 60 scenes. Full payload SHA256: `ceab67c93f12e3f08742b3a2b9c4ad99a23db5fb058eacf2caab7d595a0ed277`. Managed SHA256: `e8205ecc064338d2d626bc8e8f568ec9847fa4ec5ea153e16f172a64d3593a0c`.

The native build exited 0 after 368 seconds. BuildReport records zero errors and 90 warnings; the existing Unity.Collections.Tests Unsafe.dll import issue remains recorded. All 386 source inputs match ROOT/CLEAN at acceptance, and 925 unrelated files remain unchanged. The previous accepted d23 payload is preserved by the promotion receipt. Public delivery uses only the reviewed Sports patch from `393e513f`, preserving private integration history; the scoped publication receipt records the exact commit and remote verification.

## VENDOR ASSETS AUDITED

Existing Heat page/navigation controls, shared results typography/portraits/theme, DOTween animation, installed TextMeshPro NoWrap/overflow implementation and the original match/replay lifecycles.

## VENDOR ASSETS USED

Heat navigation and controls, existing shared graphical results components, DOTween reveal animation, TMP Ellipsis, and original NWH/PhysX competition.

## RELEVANT ASSETS NOT USED

Presentation assets do not determine the sport-specific winner or team/individual statistic semantics. Those facts are read from existing LWS match authority. No replacement vehicle, navigation, weather or replay framework is needed.

## CUSTOM SYSTEMS CREATED

One read-only four-sport results adapter, an additive five-scene authoring entry point, a targeted saved-scene lifecycle fixture and post-completion assertions on the original physical workloads. No new scoring authority.

## VENDOR SOURCE MODIFIED

None.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None.


Evidence: [retained test XML, source/scene proofs, build receipt, physical workload transcripts, twenty controlled captures, fourteen actual Windows captures and the pre-fix failure](Evidence/2026-10-23-Possession-Team-Results/).

Three additional fresh Kickball action frames retain the startup field, replay-transition framing and 1920×1080 rematch. Their capture dates were checked and pixels inspected; the historical October 20 sky-only image in the raw folder is excluded.

Observed existing gameplay limit: Normal 4v4 Dodgeball remains draw-heavy (24 rounds, 19 draws in this run; the retained prior baseline had 21 rounds and 16 draws). This results integration preserves the original AI and 105-second draw rule.

The Git check found sixteen trailing spaces in two failure stack traces. Readable XML copies trim only those diagnostic spaces; `original-native-test-xml.zip` retains all five original native XML files byte-for-byte, with hashes and a method record. Test outcomes, attributes and assertions are unchanged.
