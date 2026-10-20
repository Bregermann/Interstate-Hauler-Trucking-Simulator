# Truck Sports: Party! — permanent requirements and acceptance ledger

Authoritative active-work record from October 20, 2026. Current authorization: completed presentation sprint and Git reconciliation, followed by the18new sports and their complete UI/logo addendum. Historical work is deferred unless the user reactivates it or it directly affects these requirements.

Statuses: MISSING; PARTIAL; IMPLEMENTED / UNVERIFIED; VERIFIED COMPLETE; BLOCKED. October20 expansion standardizes the two equivalent prior labels without changing their evidence or human-verification meaning. Automated completion and human verification are separate. Human verification is never inferred from software input, scripts or screenshots. IDs remain permanent; superseded/cancelled entries retain their record.

## Active work

Git reconciliation is verified (LD-004); public main is synchronized and local/private work is recoverable. The new18-sport expansion is now active, in the original request's recommended vertical-slice order. Source requirements and UI addendum are preserved in Documentation/TruckSports/Requests/. Earlier focused-sprint implementation/evidence remains recorded; its human/device/listening limits have not disappeared.

Expansion order: Red Light Green Light, Hot Potato, Balloon Battle, Whack-a-Mole, King of the Hill; Seesaw, Floor Is Lava, Tetherball, Jousting, Magnet Mayhem; Volleyball, Freeze Tag, Hungry Hungry Haulers, Sheep Herding; Hide and Seek, Frogger, Avalanche Escape, Ninja Warrior. Do not create18empty scenes or count registry entries as gameplay.

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

## Eighteen-sport expansion — authorized October20

Source SHA256: `5a8abd9b6d2856c2252570fe186bfbe0fee38eff355cc382ba26447b08a8e56d`. Original wording: [master request](Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md), [UI addendum](Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports-UI-Addendum.md). The new status spelling is equivalent to prior evidence-qualified statuses; existing IDs are unchanged.


### EX01 — TRUCK HIDE & SEEK


#### EX01-CORE

| Field | Record |
|---|---|
| Requirement ID | EX01-CORE |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 1 (TRUCK HIDE & SEEK), including all confirmed design decisions. |
| Acceptance Criteria | Verify actual hiding, engine shutdown, restart delay, radar clues, seeker navigation, physical tagging, correct spectating, and timed victory. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify actual hiding, engine shutdown, restart delay, radar clues, seeker navigation, physical tagging, correct spectating, and timed victory. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-AI

| Field | Record |
|---|---|
| Requirement ID | EX01-AI |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 1 (TRUCK HIDE & SEEK), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-ENV

| Field | Record |
|---|---|
| Requirement ID | EX01-ENV |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 1 (TRUCK HIDE & SEEK), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX01-LOGO |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX01-PREVIEW |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX01-SETUP |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-HUD

| Field | Record |
|---|---|
| Requirement ID | EX01-HUD |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-FX

| Field | Record |
|---|---|
| Requirement ID | EX01-FX |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX01-RESULTS |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX01-QA

| Field | Record |
|---|---|
| Requirement ID | EX01-QA |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHideSeek/Scenes/TruckHideSeek.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX02 — TRUCK RED LIGHT, GREEN LIGHT


#### EX02-CORE

| Field | Record |
|---|---|
| Requirement ID | EX02-CORE |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 2 (TRUCK RED LIGHT, GREEN LIGHT), including all confirmed design decisions. |
| Acceptance Criteria | Verify the master laughs, the one-second warning, no yellow signal, fair stopping detection, legal finishes, and spectacular explosions. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify the master laughs, the one-second warning, no yellow signal, fair stopping detection, legal finishes, and spectacular explosions. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX02-AI

| Field | Record |
|---|---|
| Requirement ID | EX02-AI |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 2 (TRUCK RED LIGHT, GREEN LIGHT), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX02-ENV

| Field | Record |
|---|---|
| Requirement ID | EX02-ENV |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 2 (TRUCK RED LIGHT, GREEN LIGHT), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | PARTIAL |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Detailed digital stadium-stat boards/speaker dressing and dedicated camera-shake/smoke/sparks presentation are not all complete; preserve working venue, actual destruction and camera tests. |


#### EX02-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX02-LOGO |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | User approved the grungy revision3artwork: 'that one looks good, continue on'. This is artwork approval, not gameplay or hardware verification. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX02-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX02-PREVIEW |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX02-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX02-SETUP |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX02-HUD

| Field | Record |
|---|---|
| Requirement ID | EX02-HUD |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX02-FX

| Field | Record |
|---|---|
| Requirement ID | EX02-FX |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | PARTIAL |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Detailed digital stadium-stat boards/speaker dressing and dedicated camera-shake/smoke/sparks presentation are not all complete; preserve working venue, actual destruction and camera tests. |


#### EX02-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX02-RESULTS |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX02-QA

| Field | Record |
|---|---|
| Requirement ID | EX02-QA |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX03 — TRUCK HOT POTATO


#### EX03-CORE

| Field | Record |
|---|---|
| Requirement ID | EX03-CORE |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 3 (TRUCK HOT POTATO), including all confirmed design decisions. |
| Acceptance Criteria | Verify gentle contact transfers, cooldowns, carrier boost, explosions, eliminations, and final victory. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify gentle contact transfers, cooldowns, carrier boost, explosions, eliminations, and final victory. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX03-AI

| Field | Record |
|---|---|
| Requirement ID | EX03-AI |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 3 (TRUCK HOT POTATO), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | PARTIAL |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Add explicit fuse-urgency pursuit/evasion tuning. Existing actual contact-based carrier chase, evasion and recovery passed full Windows AI matches. |


#### EX03-ENV

| Field | Record |
|---|---|
| Requirement ID | EX03-ENV |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 3 (TRUCK HOT POTATO), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX03-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX03-LOGO |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX03-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX03-PREVIEW |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX03-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX03-SETUP |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX03-HUD

| Field | Record |
|---|---|
| Requirement ID | EX03-HUD |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX03-FX

| Field | Record |
|---|---|
| Requirement ID | EX03-FX |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX03-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX03-RESULTS |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX03-QA

| Field | Record |
|---|---|
| Requirement ID | EX03-QA |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX04 — TRUCK FROGGER


#### EX04-CORE

| Field | Record |
|---|---|
| Requirement ID | EX04-CORE |
| Sport/System | TRUCK FROGGER |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 4 (TRUCK FROGGER), including all confirmed design decisions. |
| Acceptance Criteria | All five challenges must be independently selectable and completable. One life per attempt. Fastest successful run receives the point. Five-challenge championship correctly determines the winner. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | All five challenges must be independently selectable and completable. One life per attempt. Fastest successful run receives the point. Five-challenge championship correctly determines the winner. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-AI

| Field | Record |
|---|---|
| Requirement ID | EX04-AI |
| Sport/System | TRUCK FROGGER |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 4 (TRUCK FROGGER), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-ENV

| Field | Record |
|---|---|
| Requirement ID | EX04-ENV |
| Sport/System | TRUCK FROGGER |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 4 (TRUCK FROGGER), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX04-LOGO |
| Sport/System | TRUCK FROGGER |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX04-PREVIEW |
| Sport/System | TRUCK FROGGER |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX04-SETUP |
| Sport/System | TRUCK FROGGER |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-HUD

| Field | Record |
|---|---|
| Requirement ID | EX04-HUD |
| Sport/System | TRUCK FROGGER |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-FX

| Field | Record |
|---|---|
| Requirement ID | EX04-FX |
| Sport/System | TRUCK FROGGER |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX04-RESULTS |
| Sport/System | TRUCK FROGGER |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX04-QA

| Field | Record |
|---|---|
| Requirement ID | EX04-QA |
| Sport/System | TRUCK FROGGER |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX05 — TRUCK NINJA WARRIOR


#### EX05-CORE

| Field | Record |
|---|---|
| Requirement ID | EX05-CORE |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 5 (TRUCK NINJA WARRIOR), including all confirmed design decisions. |
| Acceptance Criteria | The complete course must be physically traversable. Test every obstacle using player input. Verify jump/compression combinations, checkpoint penalties, timing, AI runs, and the warped-wall finish. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | The complete course must be physically traversable. Test every obstacle using player input. Verify jump/compression combinations, checkpoint penalties, timing, AI runs, and the warped-wall finish. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-AI

| Field | Record |
|---|---|
| Requirement ID | EX05-AI |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 5 (TRUCK NINJA WARRIOR), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-ENV

| Field | Record |
|---|---|
| Requirement ID | EX05-ENV |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 5 (TRUCK NINJA WARRIOR), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX05-LOGO |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX05-PREVIEW |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX05-SETUP |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-HUD

| Field | Record |
|---|---|
| Requirement ID | EX05-HUD |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-FX

| Field | Record |
|---|---|
| Requirement ID | EX05-FX |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX05-RESULTS |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX05-QA

| Field | Record |
|---|---|
| Requirement ID | EX05-QA |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX06 — TRUCK FLOOR IS LAVA


#### EX06-CORE

| Field | Record |
|---|---|
| Requirement ID | EX06-CORE |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 6 (TRUCK FLOOR IS LAVA), including all confirmed design decisions. |
| Acceptance Criteria | Verify collapsing platforms, real weight/collision interaction, ramming, AI decisions, lava elimination, and full matches. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify collapsing platforms, real weight/collision interaction, ramming, AI decisions, lava elimination, and full matches. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-AI

| Field | Record |
|---|---|
| Requirement ID | EX06-AI |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 6 (TRUCK FLOOR IS LAVA), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-ENV

| Field | Record |
|---|---|
| Requirement ID | EX06-ENV |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 6 (TRUCK FLOOR IS LAVA), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX06-LOGO |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX06-PREVIEW |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX06-SETUP |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-HUD

| Field | Record |
|---|---|
| Requirement ID | EX06-HUD |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-FX

| Field | Record |
|---|---|
| Requirement ID | EX06-FX |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX06-RESULTS |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX06-QA

| Field | Record |
|---|---|
| Requirement ID | EX06-QA |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX07 — TRUCK AVALANCHE ESCAPE


#### EX07-CORE

| Field | Record |
|---|---|
| Requirement ID | EX07-CORE |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 7 (TRUCK AVALANCHE ESCAPE), including all confirmed design decisions. |
| Acceptance Criteria | Test a complete mountain race, genuinely approaching avalanche, physical snow contact, brief escapes, burial, branching routes, and AI navigation. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Test a complete mountain race, genuinely approaching avalanche, physical snow contact, brief escapes, burial, branching routes, and AI navigation. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-AI

| Field | Record |
|---|---|
| Requirement ID | EX07-AI |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 7 (TRUCK AVALANCHE ESCAPE), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-ENV

| Field | Record |
|---|---|
| Requirement ID | EX07-ENV |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 7 (TRUCK AVALANCHE ESCAPE), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX07-LOGO |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX07-PREVIEW |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX07-SETUP |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-HUD

| Field | Record |
|---|---|
| Requirement ID | EX07-HUD |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-FX

| Field | Record |
|---|---|
| Requirement ID | EX07-FX |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX07-RESULTS |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX07-QA

| Field | Record |
|---|---|
| Requirement ID | EX07-QA |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX08 — TRUCK FREEZE TAG


#### EX08-CORE

| Field | Record |
|---|---|
| Requirement ID | EX08-CORE |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 8 (TRUCK FREEZE TAG), including all confirmed design decisions. |
| Acceptance Criteria | Verify freezing, persistent ice states, physical rescues, asymmetric roles, timers, scoring, and role reversal. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify freezing, persistent ice states, physical rescues, asymmetric roles, timers, scoring, and role reversal. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-AI

| Field | Record |
|---|---|
| Requirement ID | EX08-AI |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 8 (TRUCK FREEZE TAG), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-ENV

| Field | Record |
|---|---|
| Requirement ID | EX08-ENV |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 8 (TRUCK FREEZE TAG), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX08-LOGO |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX08-PREVIEW |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX08-SETUP |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-HUD

| Field | Record |
|---|---|
| Requirement ID | EX08-HUD |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-FX

| Field | Record |
|---|---|
| Requirement ID | EX08-FX |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX08-RESULTS |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX08-QA

| Field | Record |
|---|---|
| Requirement ID | EX08-QA |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX09 — TRUCK SEESAW


#### EX09-CORE

| Field | Record |
|---|---|
| Requirement ID | EX09-CORE |
| Sport/System | TRUCK SEESAW |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 9 (TRUCK SEESAW), including all confirmed design decisions. |
| Acceptance Criteria | Verify real multidirectional platform response, recoverable tilts, legitimate falls, AI balancing, and proper quarry impacts. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify real multidirectional platform response, recoverable tilts, legitimate falls, AI balancing, and proper quarry impacts. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-AI

| Field | Record |
|---|---|
| Requirement ID | EX09-AI |
| Sport/System | TRUCK SEESAW |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 9 (TRUCK SEESAW), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-ENV

| Field | Record |
|---|---|
| Requirement ID | EX09-ENV |
| Sport/System | TRUCK SEESAW |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 9 (TRUCK SEESAW), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX09-LOGO |
| Sport/System | TRUCK SEESAW |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX09-PREVIEW |
| Sport/System | TRUCK SEESAW |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX09-SETUP |
| Sport/System | TRUCK SEESAW |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-HUD

| Field | Record |
|---|---|
| Requirement ID | EX09-HUD |
| Sport/System | TRUCK SEESAW |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-FX

| Field | Record |
|---|---|
| Requirement ID | EX09-FX |
| Sport/System | TRUCK SEESAW |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX09-RESULTS |
| Sport/System | TRUCK SEESAW |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX09-QA

| Field | Record |
|---|---|
| Requirement ID | EX09-QA |
| Sport/System | TRUCK SEESAW |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX10 — TRUCK VOLLEYBALL


#### EX10-CORE

| Field | Record |
|---|---|
| Requirement ID | EX10-CORE |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 10 (TRUCK VOLLEYBALL), including all confirmed design decisions. |
| Acceptance Criteria | Verify serve, rally, touch limits, out-of-bounds calls, real ball contact, net behavior, scoring, and complete matches. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify serve, rally, touch limits, out-of-bounds calls, real ball contact, net behavior, scoring, and complete matches. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-AI

| Field | Record |
|---|---|
| Requirement ID | EX10-AI |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 10 (TRUCK VOLLEYBALL), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-ENV

| Field | Record |
|---|---|
| Requirement ID | EX10-ENV |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 10 (TRUCK VOLLEYBALL), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX10-LOGO |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX10-PREVIEW |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX10-SETUP |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-HUD

| Field | Record |
|---|---|
| Requirement ID | EX10-HUD |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-FX

| Field | Record |
|---|---|
| Requirement ID | EX10-FX |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX10-RESULTS |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX10-QA

| Field | Record |
|---|---|
| Requirement ID | EX10-QA |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX11 — TRUCK BALLOON BATTLE


#### EX11-CORE

| Field | Record |
|---|---|
| Requirement ID | EX11-CORE |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 11 (TRUCK BALLOON BATTLE), including all confirmed design decisions. |
| Acceptance Criteria | Verify three balloons per truck, tether motion, collision-based popping, correct scoring, elimination, AI competition, and rematches. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify three balloons per truck, tether motion, collision-based popping, correct scoring, elimination, AI competition, and rematches. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX11-AI

| Field | Record |
|---|---|
| Requirement ID | EX11-AI |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 11 (TRUCK BALLOON BATTLE), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX11-ENV

| Field | Record |
|---|---|
| Requirement ID | EX11-ENV |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 11 (TRUCK BALLOON BATTLE), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX11-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX11-LOGO |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX11-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX11-PREVIEW |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX11-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX11-SETUP |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX11-HUD

| Field | Record |
|---|---|
| Requirement ID | EX11-HUD |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX11-FX

| Field | Record |
|---|---|
| Requirement ID | EX11-FX |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX11-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX11-RESULTS |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX11-QA

| Field | Record |
|---|---|
| Requirement ID | EX11-QA |
| Sport/System | TRUCK BALLOON BATTLE |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity |
| Relevant Scripts | Assets/TruckBalloonBattle/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckBalloonBattle.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | All severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. See progress note for presentation/replay limitations. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX12 — TRUCK MAGNET MAYHEM


#### EX12-CORE

| Field | Record |
|---|---|
| Requirement ID | EX12-CORE |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 12 (TRUCK MAGNET MAYHEM), including all confirmed design decisions. |
| Acceptance Criteria | Verify real attraction/repulsion, correct polarity, force limits, scrap movement, contested scoring, AI strategies, and physics stability. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify real attraction/repulsion, correct polarity, force limits, scrap movement, contested scoring, AI strategies, and physics stability. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-AI

| Field | Record |
|---|---|
| Requirement ID | EX12-AI |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 12 (TRUCK MAGNET MAYHEM), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-ENV

| Field | Record |
|---|---|
| Requirement ID | EX12-ENV |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 12 (TRUCK MAGNET MAYHEM), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX12-LOGO |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX12-PREVIEW |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX12-SETUP |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-HUD

| Field | Record |
|---|---|
| Requirement ID | EX12-HUD |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-FX

| Field | Record |
|---|---|
| Requirement ID | EX12-FX |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX12-RESULTS |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX12-QA

| Field | Record |
|---|---|
| Requirement ID | EX12-QA |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX13 — TRUCK TETHERBALL


#### EX13-CORE

| Field | Record |
|---|---|
| Requirement ID | EX13-CORE |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 13 (TRUCK TETHERBALL), including all confirmed design decisions. |
| Acceptance Criteria | Verify ball swinging, rope wrapping, direction changes, scoring, truck collisions, AI play, and resets. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify ball swinging, rope wrapping, direction changes, scoring, truck collisions, AI play, and resets. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-AI

| Field | Record |
|---|---|
| Requirement ID | EX13-AI |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 13 (TRUCK TETHERBALL), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-ENV

| Field | Record |
|---|---|
| Requirement ID | EX13-ENV |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 13 (TRUCK TETHERBALL), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX13-LOGO |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX13-PREVIEW |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX13-SETUP |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-HUD

| Field | Record |
|---|---|
| Requirement ID | EX13-HUD |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-FX

| Field | Record |
|---|---|
| Requirement ID | EX13-FX |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX13-RESULTS |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX13-QA

| Field | Record |
|---|---|
| Requirement ID | EX13-QA |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX14 — TRUCK JOUSTING


#### EX14-CORE

| Field | Record |
|---|---|
| Requirement ID | EX14-CORE |
| Sport/System | TRUCK JOUSTING |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 14 (TRUCK JOUSTING), including all confirmed design decisions. |
| Acceptance Criteria | Verify lance adjustment, real contact, hit zones, breakage, proper score awards, repeated passes, and competition results. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify lance adjustment, real contact, hit zones, breakage, proper score awards, repeated passes, and competition results. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-AI

| Field | Record |
|---|---|
| Requirement ID | EX14-AI |
| Sport/System | TRUCK JOUSTING |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 14 (TRUCK JOUSTING), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-ENV

| Field | Record |
|---|---|
| Requirement ID | EX14-ENV |
| Sport/System | TRUCK JOUSTING |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 14 (TRUCK JOUSTING), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX14-LOGO |
| Sport/System | TRUCK JOUSTING |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX14-PREVIEW |
| Sport/System | TRUCK JOUSTING |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX14-SETUP |
| Sport/System | TRUCK JOUSTING |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-HUD

| Field | Record |
|---|---|
| Requirement ID | EX14-HUD |
| Sport/System | TRUCK JOUSTING |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-FX

| Field | Record |
|---|---|
| Requirement ID | EX14-FX |
| Sport/System | TRUCK JOUSTING |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX14-RESULTS |
| Sport/System | TRUCK JOUSTING |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX14-QA

| Field | Record |
|---|---|
| Requirement ID | EX14-QA |
| Sport/System | TRUCK JOUSTING |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX15 — HUNGRY HUNGRY HAULERS


#### EX15-CORE

| Field | Record |
|---|---|
| Requirement ID | EX15-CORE |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 15 (HUNGRY HUNGRY HAULERS), including all confirmed design decisions. |
| Acceptance Criteria | Verify physical collection, scoop animation, cargo capacity, deposits, scoring, golden balls, explosive penalties, AI, and rematches. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify physical collection, scoop animation, cargo capacity, deposits, scoring, golden balls, explosive penalties, AI, and rematches. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-AI

| Field | Record |
|---|---|
| Requirement ID | EX15-AI |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 15 (HUNGRY HUNGRY HAULERS), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-ENV

| Field | Record |
|---|---|
| Requirement ID | EX15-ENV |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 15 (HUNGRY HUNGRY HAULERS), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX15-LOGO |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX15-PREVIEW |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX15-SETUP |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-HUD

| Field | Record |
|---|---|
| Requirement ID | EX15-HUD |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-FX

| Field | Record |
|---|---|
| Requirement ID | EX15-FX |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX15-RESULTS |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX15-QA

| Field | Record |
|---|---|
| Requirement ID | EX15-QA |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX16 — TRUCK WHACK-A-MOLE


#### EX16-CORE

| Field | Record |
|---|---|
| Requirement ID | EX16-CORE |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 16 (TRUCK WHACK-A-MOLE), including all confirmed design decisions. |
| Acceptance Criteria | Verify emergence animation, timed windows, collision-based scoring, golden targets, decoys, AI pursuit, and match completion. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Verify emergence animation, timed windows, collision-based scoring, golden targets, decoys, AI pursuit, and match completion. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX16-AI

| Field | Record |
|---|---|
| Requirement ID | EX16-AI |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 16 (TRUCK WHACK-A-MOLE), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX16-ENV

| Field | Record |
|---|---|
| Requirement ID | EX16-ENV |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 16 (TRUCK WHACK-A-MOLE), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX16-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX16-LOGO |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX16-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX16-PREVIEW |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX16-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX16-SETUP |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX16-HUD

| Field | Record |
|---|---|
| Requirement ID | EX16-HUD |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX16-FX

| Field | Record |
|---|---|
| Requirement ID | EX16-FX |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX16-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX16-RESULTS |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX16-QA

| Field | Record |
|---|---|
| Requirement ID | EX16-QA |
| Sport/System | TRUCK WHACK-A-MOLE |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity |
| Relevant Scripts | Assets/TruckWhackAMole/Scripts/ and Editor/; shared SportsRecordedReview and SportsExpansionViewHud. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckWhackAMole.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Thirty-six pooled holes and three original animated mole prefabs; actual front-bumper contacts, one hit per emergence,10/50/−25points, timed difficulty growth, decoy launch, shared placings, complete timed AI match and rematch. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors; common menu298/0 on Assembly-CSharp SHA256 c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8. |
| Runtime Evidence | Full AI match, actual collision fixture, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Whack/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical controllers/wheels, subjective listening, human gameplay and isolated CPU/GPU performance remain unverified. No unimplemented Whack gameplay feature is inferred from those limits. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX17 — TRUCK KING OF THE HILL


#### EX17-CORE

| Field | Record |
|---|---|
| Requirement ID | EX17-CORE |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 17 (TRUCK KING OF THE HILL), including all confirmed design decisions. |
| Acceptance Criteria | Verify contested-zone logic, correct timing, physical occupancy, moving zones, AI navigation, and victory conditions. |
| Status | PARTIAL |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/HillRules.cs; HillMatch.cs |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | Assets/TruckKingOfTheHill/Scripts/HillRules.cs; HillMatch.cs |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify contested-zone logic, correct timing, physical occupancy, moving zones, AI navigation, and victory conditions. |
| Automated Verification | Native Hill rules12/12passed; Hill gameplay and scene not yet tested. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Author mountaintop scene/roads and integrate actual physics occupancy. Twelve native rule tests passed; no playable Hill scene exists yet. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Author mountaintop scene/roads and integrate actual physics occupancy. Twelve native rule tests passed; no playable Hill scene exists yet. |


#### EX17-AI

| Field | Record |
|---|---|
| Requirement ID | EX17-AI |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 17 (TRUCK KING OF THE HILL), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | PARTIAL |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/HillMatch.cs |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | Assets/TruckKingOfTheHill/Scripts/HillMatch.cs |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Native Hill rules12/12passed; Hill gameplay and scene not yet tested. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Integrate the authored LWS road graph and test genuine NWH navigation/control/ramming. Runtime source compiled, but no AI match is claimed. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Integrate the authored LWS road graph and test genuine NWH navigation/control/ramming. Runtime source compiled, but no AI match is claimed. |


#### EX17-ENV

| Field | Record |
|---|---|
| Requirement ID | EX17-ENV |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 17 (TRUCK KING OF THE HILL), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX17-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX17-LOGO |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | PARTIAL |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; Tools/TruckSports/ArtworkSources/TruckKingOfTheHill.json |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; Tools/TruckSports/ArtworkSources/TruckKingOfTheHill.json |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Native Hill rules12/12passed; Hill gameplay and scene not yet tested. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Import/integrate actual artwork into the completed sport scene/menu. No empty menu entry exists. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Import/integrate actual artwork into the completed sport scene/menu. No empty menu entry exists. |


#### EX17-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX17-PREVIEW |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX17-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX17-SETUP |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX17-HUD

| Field | Record |
|---|---|
| Requirement ID | EX17-HUD |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX17-FX

| Field | Record |
|---|---|
| Requirement ID | EX17-FX |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX17-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX17-RESULTS |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX17-QA

| Field | Record |
|---|---|
| Requirement ID | EX17-QA |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


### EX18 — TRUCK SHEEP HERDING


#### EX18-CORE

| Field | Record |
|---|---|
| Requirement ID | EX18-CORE |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 18 (TRUCK SHEEP HERDING), including all confirmed design decisions. |
| Acceptance Criteria | Verify animated sheep movement, horn reactions, truck herding pressure, pen detection, scoring, explosive sheep, AI behavior, and full matches. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Verify animated sheep movement, horn reactions, truck herding pressure, pen detection, scoring, explosive sheep, AI behavior, and full matches. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-AI

| Field | Record |
|---|---|
| Requirement ID | EX18-AI |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 18 (TRUCK SHEEP HERDING), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-ENV

| Field | Record |
|---|---|
| Requirement ID | EX18-ENV |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 18 (TRUCK SHEEP HERDING), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX18-LOGO |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX18-PREVIEW |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX18-SETUP |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-HUD

| Field | Record |
|---|---|
| Requirement ID | EX18-HUD |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-FX

| Field | Record |
|---|---|
| Requirement ID | EX18-FX |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX18-RESULTS |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |


#### EX18-QA

| Field | Record |
|---|---|
| Requirement ID | EX18-QA |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | MISSING |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity (not yet created) |
| Relevant Scripts | Not yet implemented; inspect existing shared SportsEventSession/NWH/Heat adapters before coding. |
| Prefabs or Assets | Audit appropriate installed vendor assets; no placeholder delivery. |
| Implementation Evidence | None yet. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Not executed. |
| Runtime Evidence | Not executed. |
| Screenshot/Video Evidence | None yet. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | Requirement not implemented. |
| Last Verification Date | None |
| Remaining Work | Implement, integrate and execute this acceptance requirement. |
