# Truck Sports: Party! — permanent requirements and acceptance ledger

Authoritative active-work record from October 20, 2026. Current authorization: Dock Diving, Poker, original logos for every registered sport, and the main menu overhaul. Historical work is deferred unless the user reactivates it or it directly affects these requirements.

Statuses: MISSING; PARTIAL; IMPLEMENTED / UNVERIFIED; VERIFIED COMPLETE; BLOCKED. Automated completion and human verification are separate. Human verification is never inferred from software input, scripts or screenshots. IDs remain permanent; superseded/cancelled entries retain their record.

## Active work

Implementation is delivered for the four authorized priorities. No known blocking gameplay/rendering failure remains in the focused final automated runs. Pending verification is kept explicit:

- Audio/listening approval: DD-002, PK-006.
- Physical human/controller/reconnect acceptance: DD-004, PK-005, MN-004.
- Fresh individual current-gameplay comparison of the unchanged39 previews: MN-002.
- Build and both approved public branch pushes: LD-003 verified; publication receipt retained with the implementation hash.

Completed objective work remains recorded below, separate from human acceptance. This sprint does not certify or reopen the archived collection backlog.

Historical baseline: [dated backlog archive](Documentation/TruckSports/Archive/2026-10-20-LegacyBacklog/). Existing per-mode implementation and test evidence remains in Documentation/TruckSports. This sprint does not re-import the old reminder list.

## Dock Diving

### DD-001 — Dock Diving

| Field | Record |
|---|---|
| Requirement ID | DD-001 |
| Sport/System | Dock Diving |
| Original Request | Complete airborne comic speech bubbles with correct participant ownership. |
| Acceptance Criteria | Visible timed bubble follows the actual airborne truck, uses participant identity/color, stays readable and screen-safe in supported views, and clears on landing/reset. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | DockSlang.cs / DockSlangAuthoring.cs; saved TruckDockDiving scene. Participant colors/names, viewport-local placement, pause-preserved launch token and explicit reset. |
| Automated Verification | Native viewport EditMode and Dock PlayMode pass; exact Windows Dock 16/0: real flight/ownership, pause/idempotence, complete 24-attempt match and rematch. Actual 720p/1080p/4:3 frames inspected. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | None in the exercised supported broadcast flow; subjective/hardware acceptance is unperformed. |

### DD-002 — Dock Diving

| Field | Record |
|---|---|
| Requirement ID | DD-002 |
| Sport/System | Dock Diving |
| Original Request | Complete launch/landing/reaction audio and natural cadence. |
| Acceptance Criteria | Correct event and source position; no rapid duplicate or overlapping reactions; shared volume/mute honored; no audio survives reset or exit. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsFeedback.PlayAt bounded positional source pool; DockMatch launch/splash and one delayed crowd response; shared settings and StopAll reused. |
| Automated Verification | Exact Windows Dock 16/0 includes positional source playback, shared SFX mute and setup cleanup. No new spoken Canadian voice recordings were created. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Subjective listening, mix/cadence and physical audio-output acceptance remain unperformed. |

### DD-003 — Dock Diving

| Field | Record |
|---|---|
| Requirement ID | DD-003 |
| Sport/System | Dock Diving |
| Original Request | Verify complete launches, trajectories, splitting, splashes, scoring and result transitions. |
| Acceptance Criteria | Complete real matches score each attempt once with useful cameras, accurate names/colors and preserved established rules. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | TruckDockDiving.unity; existing DockMatch/Replay physics and scoring retained. |
| Automated Verification | Exact Windows Dock 16/0: eight AI competitors, three physical attempts each, measured water contact, one lock per launch, complete match and results/rematch. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | No observed failure in the automated complete match; human driving acceptance remains unperformed. |

### DD-004 — Dock Diving

| Field | Record |
|---|---|
| Requirement ID | DD-004 |
| Sport/System | Dock Diving |
| Original Request | Reliable multiplayer ownership, consecutive rounds, pause and rematches. |
| Acceptance Criteria | All supported participant configurations rotate correctly; independent inputs and results survive pause/reconnect/rematch without stale presentation. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Existing DockMatch turn-taking participant/input authority; DockSlang participant ownership and immediate reset. |
| Automated Verification | Native actual-flight pause/rematch 1/1; Windows eight-participant rotation, scoring ownership, pause, rematch and menu cleanup pass. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | All physical human/controller/reconnect combinations have not been exercised. This is the established turn-taking broadcast sport, not a new simultaneous split-screen mode. |

### DD-005 — Dock Diving

| Field | Record |
|---|---|
| Requirement ID | DD-005 |
| Sport/System | Dock Diving |
| Original Request | Inspect and finish the Dock Diving environment and presentation. |
| Acceptance Criteria | Finished installed-model venue, readable HUD/intro/logo, no missing materials, clipped action or placeholder artifacts in the player. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | DockVenueDressing.cs and saved Dock scene; owned Toon Gas Station trees/rocks/lamps, JC clubhouse, tiled NWH lawn material, original logo. |
| Automated Verification | Actual Windows flight frames at720p/1080p/4:3 inspected; imported dressing stays outside run-up/pool envelope. No existing native scene physics components altered. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Subjective venue approval remains for the user; no known blocking rendering defect. |

## Poker

### PK-001 — Poker

| Field | Record |
|---|---|
| Requirement ID | PK-001 |
| Sport/System | Poker |
| Original Request | Finish the mobile dealer in the actual restaurant. |
| Acceptance Criteria | Dealer continuously travels a clear route, visibly operates vehicle and hands two face-down cards to each active participant, with correct wait/pause/skip/reset states. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | PokerMobileDealer.cs; PokerMobileDealerAuthoring / PokerTabletopDealerAuthoring; saved TruckPoker scene. Existing authoritative PokerTable retained. |
| Automated Verification | Native tabletop regression1/1; exact Windows Poker25/0: continuous travel, wheels rotate about attached centers, eight seats/two handoffs each, pause/skip/reset and full tournaments. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | No observed choreography/state failure; human gameplay acceptance remains unperformed. |

### PK-002 — Poker

| Field | Record |
|---|---|
| Requirement ID | PK-002 |
| Sport/System | Poker |
| Original Request | Finish coherent restaurant architecture, furniture, lighting and population. |
| Acceptance Criteria | Owned models and appropriate dressing support table visibility and dealer clearance; no obsolete placeholder surfaces or missing materials. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | PokerRestaurantDressing.cs / PokerRestaurantSurfaceAuthoring.cs; sport-owned materials and saved scene. Nearly planar module thickness scaling bounded, structural static batching removed, noisy normal maps cleared; original intended grade retained. Four tables/eight animated diners/service furniture added. |
| Automated Verification | User-reported black/white failure reproduced in actual Windows. Same-camera material/geometry isolation found extreme thin-axis scales (floor245518.3). Corrected Windows1080p full-color restaurant frame inspected; final Poker25/0 and privacy suite pass. |
| Human Verification | User reported black/white Windows rendering and rejected that as the intended presentation. That defect was reproduced and corrected; no post-fix human confirmation yet. |
| Remaining Work | No known reproduction of the reported threshold rendering in final build; subjective restaurant-art approval remains unperformed. |

### PK-003 — Poker

| Field | Record |
|---|---|
| Requirement ID | PK-003 |
| Sport/System | Poker |
| Original Request | Keep private cards private in human, shared and spectator views. |
| Acceptance Criteria | Only the correct owner can peek; moving cards and AI/spectator panes never reveal private values; public showdown follows established rules. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Existing PokerTable/PokerPresentation/PokerCardView ownership preserved; travelling cards are backs only. PokerFocusedProbe and PokerDealerProbe verify actual data/display isolation. |
| Automated Verification | Windows Poker25/0 plus focused privacy0errors: four injected humans with fourAI, owner-only peek, shared left/right sticks, hot-seat handoff, reconnect and viewport geometry. Actual four-pane owner-peek image inspected. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Physical multiplayer screens/devices and human privacy playtest remain unperformed. |

### PK-004 — Poker

| Field | Record |
|---|---|
| Requirement ID | PK-004 |
| Sport/System | Poker |
| Original Request | Preserve complete existing Hold’em rules and chip accounting. |
| Acceptance Criteria | Legal actions, side pots, ties, eliminations and full tournaments resolve without duplicate deals, lost chips or softlocks. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Existing authoritative PokerTable Hold’em rules unchanged; mobile dealer never reads or writes deck values, bets, pots or awards. |
| Automated Verification | Exact Windows25/0 includes three complete four-AI tournaments at all difficulties, legal match progression and chip checks throughout; each ends with800chips and one champion. Animated/skip dealing preserves exact cards, actor and stacks. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | No rule defect observed in these tournaments. This does not claim exhaustive poker-state proof or new human verification. |

### PK-005 — Poker

| Field | Record |
|---|---|
| Requirement ID | PK-005 |
| Sport/System | Poker |
| Original Request | Finish Poker cameras, readable cards/actions/results and controller-compatible UI. |
| Acceptance Criteria | Supported human layouts have useful views and clear actions; dealer/showdown transitions preserve privacy; rematch/exit work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved Poker scene; existing private-seat/broadcast cameras, PokerFocusedProbe, new logo/setup headers and room presentation. |
| Automated Verification | Focused exact Windows suite0errors: injected four-human layouts720p/1080p/16:10/4:3/ultrawide, pad/mouse free look, ownership, pause/reconnect/hot seat and rematch/exit. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Physical keyboard/gamepads/wheels, shared-display usability and subjective camera readability need human testing. |

### PK-006 — Poker

| Field | Record |
|---|---|
| Requirement ID | PK-006 |
| Sport/System | Poker |
| Original Request | Dealer drives around on the table and performs a little shake and sultry giggle in front of each player while dealing their card. |
| Acceptance Criteria | Continuous tabletop route clears chips/cards and table edges; dealer visibly pauses and gives a playful shake with a brief positioned giggle at each served seat. Pause, skip and reset cancel presentation safely. Authoritative hands, privacy and chip accounting remain unchanged. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | PokerMobileDealer / PokerTabletopDealerAuthoring; saved tabletop route, seat-facing four-part shake, three original nonverbal giggle clips in Assets/TruckPoker/Audio with provenance. Existing DOTween and local Chatterbox generation engine reused. |
| Automated Verification | Native tabletop1/1 and exact WindowsPoker25/0: all8seats get one shake/giggle;16handoffs, bounded tabletop path, pause/skip/reset, no private-data or chip changes. Windows travelling-dealer image inspected. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Subjective giggle/desired sultry tone has not been listened to or approved by a human; no such approval claimed. |

## Original sport logos

Each registered scene has an individual art/integration requirement. Variants receive distinct related artwork rather than silently sharing another scene’s identity. Original PNG artwork must be inspected at actual menu size; generated output alone is not completion.

### LG-001 — TRUCK SNAKE (TruckSnake)

| Field | Record |
|---|---|
| Requirement ID | LG-001 |
| Sport/System | TRUCK SNAKE (TruckSnake) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckSnake.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckSnake.json; saved TruckSnake setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-002 — TRUCK FARE FIGHT (TruckFareFight)

| Field | Record |
|---|---|
| Requirement ID | LG-002 |
| Sport/System | TRUCK FARE FIGHT (TruckFareFight) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckFareFight.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckFareFight.json; saved TruckFareFight setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-003 — TRUCK TRAILER PLUG (TruckTrailerPlug)

| Field | Record |
|---|---|
| Requirement ID | LG-003 |
| Sport/System | TRUCK TRAILER PLUG (TruckTrailerPlug) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckTrailerPlug.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckTrailerPlug.json; saved TruckTrailerPlug setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-004 — TRUCK SKY TOW RINGS (TruckSkyTow)

| Field | Record |
|---|---|
| Requirement ID | LG-004 |
| Sport/System | TRUCK SKY TOW RINGS (TruckSkyTow) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckSkyTow.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckSkyTow.json; saved TruckSkyTow setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-005 — TRUCK DANCE OFF (TruckDanceOff)

| Field | Record |
|---|---|
| Requirement ID | LG-005 |
| Sport/System | TRUCK DANCE OFF (TruckDanceOff) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckDanceOff.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckDanceOff.json; saved TruckDanceOff setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-006 — TRUCK LIMBO (TruckLimbo)

| Field | Record |
|---|---|
| Requirement ID | LG-006 |
| Sport/System | TRUCK LIMBO (TruckLimbo) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckLimbo.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckLimbo.json; saved TruckLimbo setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-007 — TRUCK KICKBALL (TruckKickball)

| Field | Record |
|---|---|
| Requirement ID | LG-007 |
| Sport/System | TRUCK KICKBALL (TruckKickball) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckKickball.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckKickball.json; saved TruckKickball setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-008 — PEDESTRIAN PLOWING (TruckPedestrianPlowing)

| Field | Record |
|---|---|
| Requirement ID | LG-008 |
| Sport/System | PEDESTRIAN PLOWING (TruckPedestrianPlowing) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckPedestrianPlowing.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckPedestrianPlowing.json; saved TruckPedestrianPlowing setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-009 — PEDESTRIAN PLOWING CAB CHAOS (TruckPedestrianPlowingFirstPerson)

| Field | Record |
|---|---|
| Requirement ID | LG-009 |
| Sport/System | PEDESTRIAN PLOWING CAB CHAOS (TruckPedestrianPlowingFirstPerson) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckPedestrianPlowingFirstPerson.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckPedestrianPlowingFirstPerson.json; saved TruckPedestrianPlowingFirstPerson setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-010 — TRUCK BILLIARDS (TruckBilliards)

| Field | Record |
|---|---|
| Requirement ID | LG-010 |
| Sport/System | TRUCK BILLIARDS (TruckBilliards) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckBilliards.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckBilliards.json; saved TruckBilliards setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-011 — TRUCK ICE HOCKEY (TruckHockey)

| Field | Record |
|---|---|
| Requirement ID | LG-011 |
| Sport/System | TRUCK ICE HOCKEY (TruckHockey) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckHockey.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckHockey.json; saved TruckHockey setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-012 — TRUCK BOWLING (TruckBowling)

| Field | Record |
|---|---|
| Requirement ID | LG-012 |
| Sport/System | TRUCK BOWLING (TruckBowling) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckBowling.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckBowling.json; saved TruckBowling setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-013 — TRUCK AIR HOCKEY (TruckAirHockey)

| Field | Record |
|---|---|
| Requirement ID | LG-013 |
| Sport/System | TRUCK AIR HOCKEY (TruckAirHockey) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckAirHockey.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckAirHockey.json; saved TruckAirHockey setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-014 — TRUCK WALLBALL (TruckWallball)

| Field | Record |
|---|---|
| Requirement ID | LG-014 |
| Sport/System | TRUCK WALLBALL (TruckWallball) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckWallball.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckWallball.json; saved TruckWallball setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-015 — TRUCK CORNHOLE (TruckCornhole)

| Field | Record |
|---|---|
| Requirement ID | LG-015 |
| Sport/System | TRUCK CORNHOLE (TruckCornhole) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckCornhole.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckCornhole.json; saved TruckCornhole setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-016 — HARDCORE TRUCK CHEESE ROLLING (TruckCheeseRolling)

| Field | Record |
|---|---|
| Requirement ID | LG-016 |
| Sport/System | HARDCORE TRUCK CHEESE ROLLING (TruckCheeseRolling) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckCheeseRolling.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckCheeseRolling.json; saved TruckCheeseRolling setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-017 — TRUCK DOCK DIVING (TruckDockDiving)

| Field | Record |
|---|---|
| Requirement ID | LG-017 |
| Sport/System | TRUCK DOCK DIVING (TruckDockDiving) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckDockDiving.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckDockDiving.json; saved TruckDockDiving setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-018 — TRUCK KEEPAWAY (TruckKeepaway)

| Field | Record |
|---|---|
| Requirement ID | LG-018 |
| Sport/System | TRUCK KEEPAWAY (TruckKeepaway) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckKeepaway.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckKeepaway.json; saved TruckKeepaway setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-019 — TRUCK CURLING (TruckCurling)

| Field | Record |
|---|---|
| Requirement ID | LG-019 |
| Sport/System | TRUCK CURLING (TruckCurling) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckCurling.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckCurling.json; saved TruckCurling setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-020 — TRUCK BASE JUMPING (TruckBaseJumping)

| Field | Record |
|---|---|
| Requirement ID | LG-020 |
| Sport/System | TRUCK BASE JUMPING (TruckBaseJumping) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckBaseJumping.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckBaseJumping.json; saved TruckBaseJumping setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-021 — TRUCK KEEPAWAY CAB CHAOS (TruckKeepawayFirstPerson)

| Field | Record |
|---|---|
| Requirement ID | LG-021 |
| Sport/System | TRUCK KEEPAWAY CAB CHAOS (TruckKeepawayFirstPerson) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckKeepawayFirstPerson.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckKeepawayFirstPerson.json; saved TruckKeepawayFirstPerson setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-022 — TRUCK TERRIER RACING (TruckTerrierRacing)

| Field | Record |
|---|---|
| Requirement ID | LG-022 |
| Sport/System | TRUCK TERRIER RACING (TruckTerrierRacing) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckTerrierRacing.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckTerrierRacing.json; saved TruckTerrierRacing setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-023 — TRUCK MINEFIELD (TruckMinefield)

| Field | Record |
|---|---|
| Requirement ID | LG-023 |
| Sport/System | TRUCK MINEFIELD (TruckMinefield) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckMinefield.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckMinefield.json; saved TruckMinefield setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-024 — TRUCK MATTRESS RACING (TruckMattressRacing)

| Field | Record |
|---|---|
| Requirement ID | LG-024 |
| Sport/System | TRUCK MATTRESS RACING (TruckMattressRacing) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckMattressRacing.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckMattressRacing.json; saved TruckMattressRacing setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-025 — TRUCK WIFE CARRYING (TruckWifeCarrying)

| Field | Record |
|---|---|
| Requirement ID | LG-025 |
| Sport/System | TRUCK WIFE CARRYING (TruckWifeCarrying) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckWifeCarrying.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckWifeCarrying.json; saved TruckWifeCarrying setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-026 — TRUCK MUSICAL PARKING (TruckMusicalParking)

| Field | Record |
|---|---|
| Requirement ID | LG-026 |
| Sport/System | TRUCK MUSICAL PARKING (TruckMusicalParking) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckMusicalParking.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckMusicalParking.json; saved TruckMusicalParking setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-027 — TRUCK GIANT SWING (TruckGiantSwing)

| Field | Record |
|---|---|
| Requirement ID | LG-027 |
| Sport/System | TRUCK GIANT SWING (TruckGiantSwing) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckGiantSwing.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckGiantSwing.json; saved TruckGiantSwing setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-028 — TRUCK BUZKASHI (TruckBuzkashi)

| Field | Record |
|---|---|
| Requirement ID | LG-028 |
| Sport/System | TRUCK BUZKASHI (TruckBuzkashi) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckBuzkashi.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckBuzkashi.json; saved TruckBuzkashi setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-029 — TRUCK BOG SNORKELLING (TruckBogSnorkelling)

| Field | Record |
|---|---|
| Requirement ID | LG-029 |
| Sport/System | TRUCK BOG SNORKELLING (TruckBogSnorkelling) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckBogSnorkelling.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckBogSnorkelling.json; saved TruckBogSnorkelling setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-030 — TRUCK BEER PONG (TruckBeerPong)

| Field | Record |
|---|---|
| Requirement ID | LG-030 |
| Sport/System | TRUCK BEER PONG (TruckBeerPong) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckBeerPong.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckBeerPong.json; saved TruckBeerPong setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-031 — TRUCK FIELD GOAL (TruckFieldGoal)

| Field | Record |
|---|---|
| Requirement ID | LG-031 |
| Sport/System | TRUCK FIELD GOAL (TruckFieldGoal) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckFieldGoal.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckFieldGoal.json; saved TruckFieldGoal setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-032 — TRUCK TARGET LAUNCH (TruckTargetLaunch)

| Field | Record |
|---|---|
| Requirement ID | LG-032 |
| Sport/System | TRUCK TARGET LAUNCH (TruckTargetLaunch) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckTargetLaunch.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckTargetLaunch.json; saved TruckTargetLaunch setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-033 — TRUCK SUMO (TruckSumo)

| Field | Record |
|---|---|
| Requirement ID | LG-033 |
| Sport/System | TRUCK SUMO (TruckSumo) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckSumo.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckSumo.json; saved TruckSumo setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-034 — TRUCK FISHING (TruckFishing)

| Field | Record |
|---|---|
| Requirement ID | LG-034 |
| Sport/System | TRUCK FISHING (TruckFishing) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckFishing.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckFishing.json; saved TruckFishing setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-035 — TRUCK TUG OF WAR (TruckTugOfWar)

| Field | Record |
|---|---|
| Requirement ID | LG-035 |
| Sport/System | TRUCK TUG OF WAR (TruckTugOfWar) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckTugOfWar.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckTugOfWar.json; saved TruckTugOfWar setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-036 — TRUCK SKEE-BALL (TruckSkeeBall)

| Field | Record |
|---|---|
| Requirement ID | LG-036 |
| Sport/System | TRUCK SKEE-BALL (TruckSkeeBall) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckSkeeBall.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckSkeeBall.json; saved TruckSkeeBall setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-037 — TRUCK LOG ROLLING (TruckLogRolling)

| Field | Record |
|---|---|
| Requirement ID | LG-037 |
| Sport/System | TRUCK LOG ROLLING (TruckLogRolling) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckLogRolling.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckLogRolling.json; saved TruckLogRolling setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-038 — TRUCK TRAMPOLINE DODGEBALL (TruckTrampolineDodgeball)

| Field | Record |
|---|---|
| Requirement ID | LG-038 |
| Sport/System | TRUCK TRAMPOLINE DODGEBALL (TruckTrampolineDodgeball) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckTrampolineDodgeball.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckTrampolineDodgeball.json; saved TruckTrampolineDodgeball setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-039 — TRUCK DEMOLITION DERBY (TruckDemolitionDerby)

| Field | Record |
|---|---|
| Requirement ID | LG-039 |
| Sport/System | TRUCK DEMOLITION DERBY (TruckDemolitionDerby) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckDemolitionDerby.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckDemolitionDerby.json; saved TruckDemolitionDerby setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-040 — TRUCK POKER (TruckPoker)

| Field | Record |
|---|---|
| Requirement ID | LG-040 |
| Sport/System | TRUCK POKER (TruckPoker) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckPoker.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckPoker.json; saved TruckPoker setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

### LG-041 — TRUCK BUNGEE DESTRUCTION (TruckBungeeDemolition)

| Field | Record |
|---|---|
| Requirement ID | LG-041 |
| Sport/System | TRUCK BUNGEE DESTRUCTION (TruckBungeeDemolition) |
| Original Request | Create a distinctive original retro arcade logo with custom lettering, palette and integrated sport imagery, using the collection branding as foundation. |
| Acceptance Criteria | A finished high-definition image asset appears correctly in sport selection, configuration, title/loading presentation and suitable venue signage; readable, uncropped and undistorted in Windows. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckBungeeDemolition.png and .meta; editable prompt/provenance Tools/TruckSports/ArtworkSources/TruckBungeeDemolition.json; saved TruckBungeeDemolition setup/info header, collection card/showcase and suitable existing venue branding. |
| Automated Verification | Native EditMode distinct sprite/import checks pass for all41. Final Windows menu273/0 verifies this scene logo/card/setup and actual launch/return. Full-size generated art and allfive menu pages inspected at intended display sizes. |
| Human Verification | User liked the earlier images and requested a stronger PS1/PS2 era look; revised chunky/faceted artwork delivered. No human build approval of this revision yet. |
| Remaining Work | No missing art or integration defect observed; subjective final art approval is not claimed. Venue placement is limited to suitable existing signs, avoiding new gameplay obstructions. |

## Main menu and shared presentation

### MN-001 — Main Menu

| Field | Record |
|---|---|
| Requirement ID | MN-001 |
| Sport/System | Main Menu |
| Original Request | Overhaul title and sport browsing with prominent selected sport, custom logos, screenshots and concise information. |
| Acceptance Criteria | All registered games remain discoverable and launchable; selected sport has a clear focus state, representative art and responsive polished layout. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | SportsCatalogueHero/Choice, SportsRetroMenuAuthoring, saved TruckSports scene. Eight-card pages, large logo/gameplay/title/details, mouse selection and DOTween fades. |
| Automated Verification | Exact Windows menu273/0: all41real scene launch/returns and own-logo setup headers. Final five pages inspected; page focus now selects an active sport and refreshes its matching showcase. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | No known launch/page-focus failure in final run; subjective menu preference remains for the user. |

### MN-002 — Main Menu

| Field | Record |
|---|---|
| Requirement ID | MN-002 |
| Sport/System | Main Menu |
| Original Request | Use current representative actual gameplay screenshots. |
| Acceptance Criteria | Every registered sport has an inspected genuine gameplay capture; changed Dock/Poker/Kickball environments and cameras are represented accurately. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Assets/TruckSports/Art/Previews: current Dock/Poker runtime captures refreshed with existing GUIDs; existing39 scene previews retained, including prior repaired Kickball. Source labels: Tools/TruckSports/ArtworkSources/GameplayPreviews.json. |
| Automated Verification | Windows menu273/0 verifies all41non-null gameplay sprites and loads each saved scene. Changed Dock/Poker images and representative menu captures visually inspected. All image dimensions valid. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Unchanged39 previews were not all freshly recaptured or individually compared against current full gameplay in this focused sprint. They remain the existing genuine runtime-capture baseline, not newly claimed current-gameplay acceptance. |

### MN-003 — Main Menu

| Field | Record |
|---|---|
| Requirement ID | MN-003 |
| Sport/System | Main Menu |
| Original Request | Preserve and clarify sport setup, Your Crew, names/colors and participant options. |
| Acceptance Criteria | Existing human/AI configuration and identities persist; setup/start/results/return actions remain predictable and correct. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | SportsParticipantsMenu origin-page return; Your Crew shortcut; existing global IDs/names/colors/settings preserved. |
| Automated Verification | Windows menu273/0 includes open/edit/color/Back, actual preference save and registry cache reload preserving ID/name/color. All41setup actions/returns pass. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Physical multi-device selection and human setup usability remain unperformed. |

### MN-004 — Main Menu

| Field | Record |
|---|---|
| Requirement ID | MN-004 |
| Sport/System | Main Menu |
| Original Request | Complete keyboard/gamepad navigation, ownership and consistent Back/Confirm/Cancel behavior. |
| Acceptance Criteria | No lost/trapped focus; all registered entries/options are reachable; supported reconnect/shared ownership preserved; actual prompts are accurate. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsFrontEnd.Focus spatial Heat/uGUI navigation and active-page first-card selection; SportsMenuView default behavior preserved; existing input owners retained. |
| Automated Verification | Windows menu273/0: software keyboard/gamepad both move actual focus; five pages select visible matching cards; all41launch/return actions. Poker focused suite verifies software reconnect/shared stick ownership. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Physical controllers, multi-device menu operation and wheel navigation remain unperformed; software injection is not hardware verification. |

### MN-005 — Main Menu

| Field | Record |
|---|---|
| Requirement ID | MN-005 |
| Sport/System | Main Menu |
| Original Request | Polished efficient transitions and screen-compatible UI. |
| Acceptance Criteria | Logo/screenshot aspect preserved, readable focus/description/configuration at720p/1080p/higher/ultrawide and relevant splits; no overlap, broken return transition or unnecessary texture load cost. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | SportsCatalogueHero DOTween fades and preserved-aspect Image sprites; saved two-column grid/showcase; single-sprite1024max compressed logo imports without mipmaps. |
| Automated Verification | Windows menu273/0 and inspected720p/1080p/1440p/2560x1080 output: logo/preview/details screen bounds and text truncation checked; all5pages and return transitions pass. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | No subjective performance certification or physical-display acceptance; no known clipped UI in exercised fixtures. |

## Ledger and delivery

### LD-001 — Requirements ledger

| Field | Record |
|---|---|
| Requirement ID | LD-001 |
| Sport/System | Requirements ledger |
| Original Request | Archive the old backlog and make this permanent ledger the active authority without importing unrelated tasks. |
| Acceptance Criteria | Historical records preserved; stable IDs and all nine required fields exist for this sprint; future requests update original entries. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Permanent TRUCK_SPORTS_ACCEPTANCE_LEDGER.md, AGENTS.md workflow, dated7-document historical archive and5redirects; no historical requirement import. |
| Automated Verification | 61unique permanent requirement IDs, all9required fields; archived original document text preserved after line-ending normalization. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Maintain permanent IDs on future changes; no current implementation gap. |

### LD-002 — Targeted acceptance

| Field | Record |
|---|---|
| Requirement ID | LD-002 |
| Sport/System | Targeted acceptance |
| Original Request | Test relevant complete Dock/Poker matches, logo/menu art, navigation, multiplayer, rematches and actual Windows rendering. |
| Acceptance Criteria | Appropriate targeted EditMode/PlayMode and player checks have exact receipts and visual evidence; no invented listening/hardware/human claims. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Targeted native test XML, exact-player launch receipts, result files and untouched Windows-framebuffer PNGs in PresentationEvidence. |
| Automated Verification | Native EditMode2/2, DockPlayMode1/1, revised tabletopPokerPlayMode1/1 pass. FinalWindows Poker25/0, Dock16/0, privacy0errors, menu273/0; all4processesexit0 on the same final managed hash. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | Subjective listening, physical devices, human playtest and full collection gameplay acceptance are explicitly unperformed. |

### LD-003 — Build and Git delivery

| Field | Record |
|---|---|
| Requirement ID | LD-003 |
| Sport/System | Build and Git delivery |
| Original Request | Deliver one combined Windows build and scoped commits/push, preserving unrelated work. |
| Acceptance Criteria | Builds/TruckSports/TruckSports.exe plus companions includes finished sprint work; settings restored; reviewed source pushed normally; remaining sprint issues explicit. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Canonical Builds/TruckSports/TruckSports.exe and companions; SportsBuilder.Build explicit42Sports-only scenes; source preservation dd3de2efd4ff312c63289886aed723f4b78d6e83; public implementation c67ad99c1f4b0dc45eef3edf35124726ac31e980; exact receipt Documentation/TruckSports/PresentationEvidence/publication.json. |
| Automated Verification | Build0errors/57warnings; failure and final settings restoration pass;4exactWindowsprocessesexit0. Both approved public branches read back c67ad99c1f4b0dc45eef3edf35124726ac31e980 after atomic normal push, including129MBLFSobject. 301scopedfiles;912protectedvendor/settingsfilesunchanged; privateTaxiancestryexcluded. Documentation-only receipt follow-up does not require rebuilding runtime. |
| Human Verification | No new human playtest, physical-device or listening acceptance claimed. |
| Remaining Work | No publication blocker. Final documentation receipt commit is recorded in delivered GIT-DELIVERY.txt. Retain the full executable companion directory; human/hardware/listening limits remain in their individual entries. |

### LD-004 — Git divergence and preservation

| Field | Record |
|---|---|
| Requirement ID | LD-004 |
| Sport/System | Git delivery and local workspace |
| Original Request | Resolve the diverged Pull origin badge, get the completed work pushed and preserve everything properly. |
| Acceptance Criteria | Main tracks the public main head with no ahead/behind divergence; Sports delivery is pushed normally; all old local commits and uncommitted files remain recoverable and working files are unchanged. Private/unrelated work is not accidentally published. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | Documentation/TruckSports/GitPreservation.md; local recovery branch codex/local-preservation-2026-10-20 at f4675ce3d353b42157c942876412bb0731793409; exact local reconciliation receipt and raw-file archive retained in .sports-validation/Recovery/GitReconciliation-20261020T073002Z. |
| Automated Verification | Local main aligned to verified public739f3914;0ahead/0behind. All32512tracked/untracked file sizes/timestamps unchanged;1101candidate raw files archived, archive hashes checked and working bytes unchanged. All original main history is an ancestor of the recovery snapshot; nonignored uncommitted work included. No force push or working-tree reset. |
| Human Verification | User supplied the Pull origin ahead/behind screenshot and requested reconciliation. |
| Remaining Work | No reconciliation blocker. Private/unrelated work remains locally recoverable and deliberately uncommitted on public main; it was not published to the public repository. The following documentation-only normal push is recorded in GIT-DELIVERY.txt. |

## Requirement history

- 2026-10-20: User explicitly replaced the historical backlog process and prioritized this sprint. Existing completed features are not automatically reopened.
- Registry snapshot:41 unique sport scenes from current SportsNavigation; final scene-list/menu coverage must agree.

- 2026-10-20 human art-direction feedback: user likes the initial logos but requests a stronger PS1/PS2-era look. Preserve sport concepts; revise toward chunky asymmetric letterforms, simple painted/faceted shading, low-poly cartoon trucks and bold color planes with less modern gloss/detail. This supersedes the first art rendering direction for every LG requirement.

- 2026-10-20: PK-006 added for tabletop dealer shake/giggle. PK-002 reopened on the user's actual black/white Windows report, reproduced and repaired by bounded planar-module scale/static-batching/material corrections. MN-001 page-focus regression found by visual inspection after reference checks; corrected and final Windows menu273/0 passed.

- 2026-10-20 publication: reviewed301-file implementation c67ad99c1f4b0dc45eef3edf35124726ac31e980 pushed normally/atomically to main and codex/truck-sports-seven-events; both heads read back successfully. Private Taxi ancestry excluded; no vendor source files added or modified. Documentation-only receipt follow-up is separately identified in the canonical delivery file.

- 2026-10-20 LD-004: preserved the full original local main history and nonignored working changes on a local recovery branch, verified a1101-file raw archive and32512unchanged file timestamps/sizes, then aligned main without changing working files. Private Taxi work is preserved locally, not sent to the public repository.
