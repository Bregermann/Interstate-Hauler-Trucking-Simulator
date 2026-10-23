# Confirmed team score feedback — October 23, 2026

Q22-SKY-SCORE-01 remains **PARTIAL**. Volleyball and Freeze Tag now present real team scores through the existing Heat/DOTween UI. The current global sky/time/scoring/results pass and queued Carnival remain unfinished.

## Implementation and source of the missing capability

The existing receipt API required a participant ID and nonnegative player slot. It could not represent a genuine team recipient. The existing journal now accepts explicit team receipts with stable team identity, team name/color and `IsTeam`; `ParticipantId` is null and `Slot` is−1. No captain, individual or fabricated crew member is credited. Existing participant receipts/API retain their behavior. Recipient keys distinguish `team:blue` from `participant:blue`, so duplicate names, opposite teams and player/team IDs do not combine accidentally. The presenter uses that stable recipient key for aggregation.

Volleyball emits one receipt at the existing rally-point commit for a legal landing, out or touch fault. It retains the original server, win-by-two and cap rules. The final real point uses the champion tier without adding a second point. Freeze Tag emits one team round-win receipt after the existing timer-survival or all-runners-frozen resolution; ties and final match winners still come from the original rules. Tags/rescues do not become invented match points. Both adapters unsubscribe on destruction.

Explicit additive authoring installs the existing Heat notification, shared truck portrait, rounded backing, team accent and DOTween emphasis in the two saved scenes. Normal Play/build operations do not regenerate UI. Volleyball's 5,467 and Freeze Tag's 5,983 original scene documents are retained; each adds 46 UI documents. The five changed original documents are a Canvas child list and four pre-existing shared null/default-field serialization updates, inspected individually. Original world poses and physics are preserved, and 925 unrelated pending files rehashed unchanged.

## Actual validation

39 native EditMode tests passed in 0.2292 s: existing Volleyball/Freeze Tag rules, old participant receipts, new team/player identity separation, invalid/late/duplicate receipts, fresh-match behavior and prior survival award accounting.

Four native PlayMode tests passed in 22.7851 s. The new team test places the actual ball above the real court and lets physical contact award a real point; it never assigns a score, rule clock or winner. One blue-team receipt is produced despite all four crew members sharing the same display name. Resting contact does not rescore. An explicitly labelled early-results fixture after that real contact retains the team identity in Heat, freezes late reports and resets correctly on rematch. This fixture is not a complete-match claim. Existing actual Sumo, Whack-a-Mole and grounded-Hill physical/lifecycle regressions also passed.

The exact Windows payload passed 100 checks/0 errors:

| Sport | Coverage | Checks / errors | Process duration |
|---|---|---|---|
| Volley | Complete default2v2 match | 42 / 0 | 113.18 s |
| Freeze | Complete default4v4/four-round match | 45 / 0 | 121.73 s |
| HotPotato | Focused first-award/rematch regression | 13 / 0 | 13.45 s |

The full Volleyball match produced 24 actual rally-point receipts. The full Freeze Tag match produced 4 actual round-win receipts. Each receipt was checked for one semantic ID, genuine team attribution, no player slot/ID and exact delta accounting against both authoritative team totals. Full probes retain physical rallies/jumps, net/floor duplicate fixtures, line-of-sight ice casts, actual rescue, pause, replay, software-input ownership, optional AI cameras and repeated rematches. The Hot Potato regression is explicitly focused on its first real award and fresh rematch; prior full-match evidence remains separate.

Six actual Windows framebuffer images at 720p/1080p were visually inspected: real team labels/colors in Volleyball and Freeze Tag, and the correct individual player in Hot Potato. Panels are legible, opaque, nonblocking and screen-safe. Capture holds the genuine scored frame after Heat's entrance and restores simulation speed afterward. No award is manufactured for an image.

The exact 323 owned source inputs are archived locally; 310 previous inputs remain unchanged. The relevant changes are shared receipt/presentation semantics, the two event-only rule hooks/adapters, explicit authoring and focused acceptance sources. No vendor source, gameplay scoring arithmetic, physics, AI routes or camera authority is changed.

No human gameplay, physical-controller/wheel, subjective listening, extended soak, isolated CPU/GPU or memory-leak acceptance is claimed. Pre-existing package-cache startup warnings remain unsuppressed.

## Delivery and remaining work

Windows x64: `Builds/TruckSports/TruckSports.exe` with all companion files. Full payload SHA-256 `b6174b622e8471ae7c2fd789d728183ed49e72a3c1aaaeda166c1e15940a4829`; managed SHA-256 `b8a5ff5d9d597fb627a76c898806bff3d3ca79082c9172ec7d6867949022692f`. 549 files, 60 saved scenes, 323 exact owned source inputs retained. The previous accepted build remains in Recovery. Evidence: `Documentation/TruckSports/Evidence/2026-10-23-Team-Score-Feedback/`.

Remaining sports' score publishers, wider presentation/performance/hardware acceptance, global results redesign and unfinished sky/lighting/snow/reflection work remain open. Specialized Poker/Bowling/Kickball presentation is preserved. Q23-IMPOSSIBLE records the newly supplied 21-mode proposed 0.7.0 directive as queued only, with exact source bytes/checksum preserved; activation follows complete 0.6.0/Carnival.

## VENDOR ASSETS AUDITED

Installed Heat notification/API, DOTween, existing NWH contacts/ball and freeze physics, Cinemachine director, original two rule authorities and existing score journal.

## VENDOR ASSETS USED

Heat transitions, DOTween emphasis, shared broadcast portrait/plates, original NWH/PhysX sport interactions and existing Cinemachine camera authority.

## RELEVANT ASSETS NOT USED

No replacement vehicle, physics, camera, replay or UI-animation framework was needed. Team attribution is LWS match semantics over the existing vendor presentation.

## CUSTOM SYSTEMS CREATED

Extended existing LWS receipts/session adapter with explicit team recipients, two real rule-commit publishers, additive authoring and targeted acceptance tests/helpers.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
