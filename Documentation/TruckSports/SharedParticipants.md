# Shared Truck Sports participants

Status: accepted shared-identity milestone. Native isolated fixtures pass **325** checks; all **12 current-build Windows acceptance suites** pass. This does not mark the remaining release queue complete.

Open **Your Crew** from the collection title screen. Four cards appear per page. Previous/Next 4 accesses the rest of the roster. Edit a name, change its preferred color or human/AI preference, or select **Choose Driver** to open the saved-identity grid. **New** creates another identity. Duplicate names are supported; the short ID distinguishes them. Choosing a driver already assigned elsewhere swaps the two global seats, preventing duplicate identities. Names and colors also remain editable through existing sport setup screens.

The crew is global. A seat is the crew's current presentation position, not its identity. Reassigning a driver to another seat preserves that driver's ID and history. Controller devices, shared-stick ownership, keyboard clusters, team placement and actual human/AI participation remain in each sport's setup. The stored human/AI preference is descriptive metadata; it does not silently replace a mode's working input configuration.

## Persistence and migration

`SportsParticipantRegistry` owns version 1 of `TruckSports.Participants.v1`, using the existing `SportsPrefs` adapter. Each participant has an independent GUID, display name, preferred color, human/AI preference, optional truck-preference key, and import provenance. The registry initially supports twelve seats and grows without deleting identities if a future consumer needs more. Every current session family binds this registry after loading its existing configuration. Serialized mode slots retain IDs for compatibility; the global crew order governs the next setup instead of allowing stale mode-specific copies to undo a crew selection.

On the first run, migration reads the seven current standalone setup keys, their known obsolete aliases, and every concrete `SportsEventSession` setup key. Current v2 standalone keys take precedence, then current shared/v1 keys in ordinal alphabetical order, then known obsolete Billiards/Hockey/Cheese aliases. The first non-default name for a seat becomes its active identity; conflicting custom names become additional selectable identities. Identical display names are allowed and do not merge different participants. Recognized historical defaults are Player/Player N/Racer N/Blue/Orange/You/AI. Raw source JSON, including unknown fields, is retained verbatim with its original key in the registry. Original setup keys are not deleted. No modification timestamp is invented.

A validated previous registry copy is retained under `.Backup`. If the primary copy is unreadable, its exact contents are preserved under `.Recovered.<UTC ticks>` before restoring the previous valid copy or migrating legacy settings. A newer schema is opened temporarily without overwriting it. Development fixtures and player probes use private validation preference scopes; they do not overwrite normal user preferences.

## Match-result contract

Existing authoritative match-completion boundaries submit one receipt containing a unique match ID, mode key, UTC completion time, participant IDs and displayed-name snapshots, plus the actual mode result summary. Repeated completion calls with the same match ID are ignored. Only the latest 128 receipts are retained. This is a lightweight integration contract, not fabricated lifetime statistics or a new career-save system. A rematch receives a new match ID; ordinary Billiards rack transitions within a series do not create additional completed-series receipts.

## Integration and authoring

Updated shared session, Bowling, Cornhole, Cheese, Billiards, Ice Hockey, Air Hockey and Wallball load/save/completion boundaries. Three existing participant DTOs expose an identity interface and retain their original input fields. Both Plowing scenes and all other concrete shared-session sports inherit the same integration.

The front-end upgrade adds two owned pages and one title-screen button. A native serialization-block comparison found 611 added objects, zero removed objects and only four existing blocks changed (the component, page references, and child lists). It does not regenerate sport scenes or replace existing menu pages. `Truck Sports > Upgrade Shared Participants Menu` is idempotent and refuses unsaved scenes. The explicit front-end authoring defaults call this upgrade. Normal `SportsBuilder.Build` validates saved scenes without reauthoring them.

## Validation and limits

Native fixtures: 325 checks covering migration precedence, nested and top-level roster formats, conflict preservation, raw JSON retention, unique IDs, duplicate names, capacities 1–16, independent input designation, rename/color propagation, cache reload, profile selection/swapping, completion deduplication, corrupt-copy recovery, newer-schema protection and production-preference isolation. Capacity fixtures do not claim sixteen-player gameplay or hardware support. The first fixture exposed a nested-roster reader defect; it was corrected before acceptance.

Accepted combined Windows x64 Development: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`, built 2026-10-18 11:51:09 UTC, zero errors / 31 warnings, 35 explicit Sports-only scenes with TruckSports startup. Keep the entire accompanying folder. Managed SHA256 `6f6e3da1478ce9a9760bc7308a200c80de391aabc3fb5ec1023171177d272e72`; full 437-file data manifest `8963d253cb96ad577a7b6eeea1971d52f429c5782758a64133c13f6435717647`. AMD64 executable and required Unity/Mono companions were verified. Host settings match the pre-build baseline; unrelated tracked changes remain excluded.

Windows write/restart-read and the all-34-sport-scene tour pass, including **769** tour/UI checks. Actual Heat entry/profile-selection callbacks work. Crew captures at 1280×720, 1920×1080, 1024×768 and 1920×800, plus the selector at 720p/1080p, were inspected; no text overflow was detected. The 20-mode AI-camera ownership/layout suite, Hockey input/contact/lifecycle regression and Billiards multiplayer regression also pass. These use software-injected devices or direct UI callbacks, not physical controllers.

Three complete eight-AI Plowing games pass. Four simultaneous Bowling AI players completed ten frames with scores **90 / 117 / 109 / 107**, genuine pin contacts and 80 pinsetter sweeps, followed by results and rematch checks; elapsed 406.1 seconds at normal simulation speed. Cheese repeated grounded starts/full AI completion, Cornhole's complete 80-throw match/rematch (343.1 seconds at normal speed), and focused Poker checks pass. This is regression evidence, not a claim that the later queued Bowling/Goddess polish is finished.

The initial result-recording integration incorrectly read inactive Bowling slots, whose unfinished `FinalTotal` throws “Bonuses are pending.” That interrupted the rejected candidate's results screen after its real competitors finished. The correction uses active-only summaries in Bowling, Cornhole and Cheese. Native real-BowlingScore fixtures reproduce the inactive-score condition, and the accepted full Windows Bowling run verifies results/rematch. Actual persisted completion receipts from Plowing, Hockey, Billiards, Bowling, Cheese and Cornhole verify stable participant IDs, distinct match IDs and correctly bounded summaries. Rejected runs remain in ignored diagnostic evidence and are not reported as passes.

The final 60-second Plowing sample used four rendered AI views, eight physical competitors and 240 targets: **23.11 ms mean / 37.49 ms p95 / 43.77 ms p99**, 2,598 live focused frames, 132 physical hits and 27 peak active ragdolls. The Editor remained idle during sampling. No sustained 384-target or human/hardware benchmark is claimed.

[Acceptance receipt and source fingerprints](SharedParticipantEvidence/acceptance.json) · [Persisted physical-match receipts](SharedParticipantEvidence/physical-receipts.json) · [720p crew](SharedParticipantEvidence/crew-1280x720.png) · [720p selector](SharedParticipantEvidence/choose-driver-1280x720.png). Remaining checks: human UX/playtesting, actual USB controllers/wheels/keyboard rollover and perceived haptics/audio. The rest of Party/Fare Fight/Goddess/Sky Tow/Limbo/Trailer Plug remains queued. Branch/commit/push verification is reported in the delivery handoff and canonical build's GIT-DELIVERY.txt.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** existing Pixel Crushers semantic saver/profile metadata; Heat ButtonManager/InputFieldManager/Dropdown; DOTween; existing Sports preference and menu adapters.
- **VENDOR ASSETS USED:** Heat buttons and name inputs, existing uGUI/Sports UI authoring and DOTween page transitions. SportsPrefs remains the preference backend.
- **RELEVANT ASSETS NOT USED:** Pixel Crushers world/career SaveSystem orchestration is not attached to standalone Sports menu preferences; its host save profiles represent world saves, not simultaneous party participants. Heat Dropdown was inspected; a larger button grid provides room for names and distinguishing IDs across splits and small displays.
- **CUSTOM SYSTEMS CREATED:** LWS participant semantics, lossless legacy migration, bindings, small completion-receipt contract, additive crew pages and isolated acceptance fixtures/probe.
- **VENDOR SOURCE MODIFIED:** none.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** none; no replacement vehicle, input, UI animation or world-save framework.
