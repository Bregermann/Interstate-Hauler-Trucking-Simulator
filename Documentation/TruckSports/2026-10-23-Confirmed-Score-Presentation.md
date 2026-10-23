# Confirmed scoring presentation — first integration, October 23, 2026

Q22-SKY remains **PARTIAL**. This delivers the shared score-receipt/Heat presentation foundation and its actual Sumo integration. Other sports' authoritative award hooks, full collection celebrations and results/statistics remain open. No broad historical backlog is reopened; Carnival remains queued after Q22-SKY.

## Implementation

SportsScoreJournal accepts immutable receipts only after the gameplay owner has awarded points. Receipts contain the real scene sport ID, existing match ID, event ID, stable participant ID, display name/color, AI source, optional team, awarded delta, actual updated total, reason/unit/tier and round/match flags. Match-local event IDs deduplicate notifications. Closed matches reject late receipts; rematches use a fresh journal. The receipt model does not calculate or write gameplay points and does not parse UI text.

Sumo publishes after its actual Crossed and FinishStep awards. Ring-outs remain +1, sole survival +3, and the match still ends at five heat wins. Existing attribution, physics, replay, expressions, scorecards and results remain authoritative. Same display names retain distinct identities. Replay cannot generate another receipt.

The explicit saved-scene integration reuses the installed Heat Notification prefab/animation and existing broadcast portrait, fonts and plate styling. DOTween emphasizes only the UI portrait. Participant color/name, reason, signed award and actual total appear in a non-blocking shared banner. Rapid ordinary awards combine only for the same stable participant/reason/unit/tier; round/champion announcements retain distinct events. No gameplay input or vehicle transform is driven by this presentation. Pause pacing follows the vendor notification clock. Scene builds do not regenerate the UI.

## Actual verification

Three native EditMode tests passed: immutable identity/name snapshots and duplicate IDs; negative totals, closed matches and fresh-match deduplication; invalid identities/non-finite numbers. The final two-test PlayMode run passed real software-keyboard/NWH bumper contact, actual survival award, Heat receipt consumption, duplicate-name attribution, replay/rematch cleanup and the representative five-scene shared lifecycle/time regression. A separate focused physical pass also passed.

Final exact Windows payload: physical scoring **35 checks/0 errors**, plus a complete autonomous four-AI first-to-five match **12 checks/0 errors**. The AI run verifies physical motion, shoves, contacts, replay, score invariants, completion, rematch, pause and scene/audio cleanup through the existing probe. It is accelerated test playback, not human playtest or sustained performance acceptance.

Actual 720p/1080p score-banner captures are retained. The first logical Windows pass also passed 35/0, but its second screenshot occurred after the naturally short toast had ended. The final development-only capture waits for the real Heat In animation and holds that committed frame solely for both backbuffers, then restores the original game clock. It does not inject an award, replay a notification or change points. This controlled evidence is explicitly labelled in the player log.

Failed native fixtures are preserved. Initial source readiness failed because batch-mode focus disabled the test keyboard; subsequent held events were routed away from the unfocused Game View. The fixture now uses the installed Input System's documented AllDeviceInputAlwaysGoesToGameView plus IgnoreFocus settings on a temporary clone, enables its own synthetic device and restores the original settings. Production input policy was not changed. Errors were not suppressed or converted into successful gameplay assertions.

No physical-controller/wheel, listening, human-playtest or isolated CPU/GPU acceptance is claimed. Remaining human/AI mixtures and other score producers need their own integration evidence.

## Delivery and preservation

Build: Builds/TruckSports/TruckSports.exe,60 scenes / 547 files including the executable and companions. Full payload SHA256 `7403c234e96970d746b60c377629b8ae3aad82dba30c18ad75987dbd85afe9c7`; managed SHA256 `1c4f99db4a99fc5f7b8a81303548ed614d684ce5ef0ddd17558f229df7d6e5d3`. Exact 298 owned C# inputs and the prior accepted build are retained with immutable manifests/promotion backup.

The Sumo scene review preserves 3,292 original documents, original colliders/rigidbodies and world poses. It changes one original canvas child list and adds 46 UI documents. All 925 unrelated pending files remain byte-identical. No original Truck Taxi, vendor source or unrelated scene was changed. Shared event-session changes were checked against the representative existing lifecycle/time suite.

Changed scripts: SportsScoreEvent, SportsScorePresentation, SportsEventSession, SumoMatch, SportsScorePresentationAuthoring, SumoRepairProbe and targeted receipt/physical tests. The existing validation wrappers gained explicit scoped authoring and exact legacy-probe footer support. The only gameplay scene changed is Assets/TruckSumo/Scenes/TruckSumo.unity. No new sport or replacement scoring framework was created.

## Remaining work

Integrate real awards across the remaining registered sports, including continuous-score milestones and team events, while preserving specialized Poker/Bowling/Kickball sequences. Finish actual shared results, tied placements, per-sport statistics, champion presentation and acceptance. Complete remaining weather/time/outdoor artistic work under its existing IDs. This first integration does not mark the global scoring/results directive complete.

VENDOR ASSETS AUDITED: Installed Heat NotificationManager public API/prefab, notification animations, UI styles and audio references; existing DOTween/TMP/broadcast assets; NWH Sumo lifecycle and physical attribution; installed Input System editor-focus routing documentation/source.

VENDOR ASSETS USED: Heat notification transitions, DOTween portrait emphasis, existing broadcast portrait/fonts/plate, native NWH/PhysX gameplay and existing Input System controls. Referenced Heat vendor assets were already tracked; no new vendor source payload was added.

RELEVANT ASSETS NOT USED: No new UI Toolkit, vehicle, weather or custom animation framework is needed for this uGUI score adapter. Existing specialized sport presentations remain preserved for their later integrations.

CUSTOM SYSTEMS CREATED: Match-local immutable score receipts/deduplication, narrow authoritative Sumo publication hooks, an opt-in Heat presentation adapter, explicit additive scene authoring and targeted evidence/tests.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None. Gameplay scoring remains in its original owners; Heat and DOTween remain the presentation mechanisms.
