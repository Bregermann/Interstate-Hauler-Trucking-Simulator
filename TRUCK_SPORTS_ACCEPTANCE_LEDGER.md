# Truck Sports: Party! — permanent requirements and acceptance ledger

Authoritative active-work record from October 20, 2026. Current authorization: completed presentation sprint and Git reconciliation, followed by the18new sports and their complete UI/logo addendum. Historical work is deferred unless the user reactivates it or it directly affects these requirements.

Statuses: MISSING; PARTIAL; IMPLEMENTED / UNVERIFIED; VERIFIED COMPLETE; BLOCKED. October20 expansion standardizes the two equivalent prior labels without changing their evidence or human-verification meaning. Automated completion and human verification are separate. Human verification is never inferred from software input, scripts or screenshots. IDs remain permanent; superseded/cancelled entries retain their record.

## Active work

Current priority (October21): Global broadcast-camera action/readability repair first, then the attached master human-playtest repairs in P0–P4 order. The protected Bowling/Poker/Cheese/Terrier/Fare Fight mechanics and deferred Tug of War decision take precedence over historical requests. Source directives: Requests/2026-10-21-GlobalBroadcastCamera.md and Requests/2026-10-21-HumanPlaytestRepair.md. The completed eighteen-sport delivery below remains the baseline.

Git reconciliation is verified (LD-004); public main is synchronized and local/private work is recoverable. All18requested expansion sports are implemented and integrated in the60-scene Windows build. The individual entries retain automated evidence separately from outstanding human, physical-device, listening and extended-soak acceptance. Source requirements and UI addendum are preserved in Documentation/TruckSports/Requests/. Earlier focused-sprint implementation/evidence remains recorded; its human/device/listening limits have not disappeared.

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
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Verify actual hiding, engine shutdown, restart delay, radar clues, seeker navigation, physical tagging, correct spectating, and timed victory. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


#### EX01-AI

| Field | Record |
|---|---|
| Requirement ID | EX01-AI |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 1 (TRUCK HIDE & SEEK), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


#### EX01-ENV

| Field | Record |
|---|---|
| Requirement ID | EX01-ENV |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 1 (TRUCK HIDE & SEEK), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


#### EX01-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX01-LOGO |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review; PNG and actual gameplay preview are installed and rendered in Windows. |


#### EX01-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX01-PREVIEW |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review; PNG and actual gameplay preview are installed and rendered in Windows. |


#### EX01-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX01-SETUP |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


#### EX01-HUD

| Field | Record |
|---|---|
| Requirement ID | EX01-HUD |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


#### EX01-FX

| Field | Record |
|---|---|
| Requirement ID | EX01-FX |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


#### EX01-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX01-RESULTS |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


#### EX01-QA

| Field | Record |
|---|---|
| Requirement ID | EX01-QA |
| Sport/System | TRUCK HIDE & SEEK |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity |
| Relevant Scripts | Assets/TruckHideAndSeek/Scripts/ and Editor/; shared opt-in NWH audio, view grouping, listener focus and road-snow contact adapters. |
| Prefabs or Assets | Existing Snake Vista/EasyRoads world cloned without its game systems; Hivemind modular warehouse, Leartes shelves/crates/cabinets, ShipContainerPack, full NWH tractors/cabs, stock CNP, original grungy PNG. |
| Implementation Evidence | Saved scene, menu/build registration and Evidence/2026-10-20-Expansion/Fifteen/. |
| Implementation Notes | Eight real trucks, original seeker only,30s hide/180s seek, engine shutdown/headlights/delayed restart, genuine LOS and spatial engine audio/coarse CNP clues, physical one-time tags, found spectators, role rotation,35 truck-clear route-connected hiding places across measured2.53square-mile terrain, genuine open hangars, moonlit supported fixtures, results/review/rematch. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Native253/253rules; actual native Hide47/0 and prior-mode adapter42/0. Final Windows Hide47/0, adapter42/0, accumulating snow/real tracks48/0 and menu365/0; exit0, same managed SHA256 c8bcb67b8063fe09371702f1b40786064b5eb698cb21856cfdb08cf9c866ae6e. |
| Runtime Evidence | Complete eight-AI round, isolated real AI tag, body-contact/no-duplicate tag, wall occlusion, delayed engine restart, one–four software-human inputs/cabs, coarse clue viewport isolation, eight-role transition fixture, replay and rematch. Normal snow and exposed-road tracks tested. Hardware/listening/human acceptance not performed. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Fifteen/ actual Windows composed-frame720p/1080p gameplay/cabs/replay/snow. |
| Human Verification | No human playtest of this new sport. |
| Known Issues | No physical-device, subjective listening, human balance or isolated CPU/GPU profiling acceptance. Full normal-duration eight-role rotation is not claimed; all role transitions used an explicitly short fixture. Local split-screen remains an honor-system game. Severe-weather replay readability/long-run performance need follow-up. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest, listening, physical devices, difficulty balance and long-run profiling. Preserve focused automated acceptance; no claim of human-certified release readiness. |


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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX02-ENV

| Field | Record |
|---|---|
| Requirement ID | EX02-ENV |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 2 (TRUCK RED LIGHT, GREEN LIGHT), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | User approved the grungy revision3artwork: 'that one looks good, continue on'. This is artwork approval, not gameplay or hardware verification. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX02-FX

| Field | Record |
|---|---|
| Requirement ID | EX02-FX |
| Sport/System | TRUCK RED LIGHT, GREEN LIGHT |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity |
| Relevant Scripts | Assets/TruckRedLightGreenLight/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckRedLightGreenLight.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/RedLight/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX03-AI

| Field | Record |
|---|---|
| Requirement ID | EX03-AI |
| Sport/System | TRUCK HOT POTATO |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 3 (TRUCK HOT POTATO), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHotPotato/Scenes/TruckHotPotato.unity |
| Relevant Scripts | Assets/TruckHotPotato/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHotPotato.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Keep one game-rule authority; reuse input, identity, weather, cameras and UI. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 11/11; actual Windows NWH/PhysX acceptance 32checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/HotPotato/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | All five challenges must be independently selectable and completable. One life per attempt. Fastest successful run receives the point. Five-challenge championship correctly determines the winner. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX04-AI

| Field | Record |
|---|---|
| Requirement ID | EX04-AI |
| Sport/System | TRUCK FROGGER |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 4 (TRUCK FROGGER), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX04-ENV

| Field | Record |
|---|---|
| Requirement ID | EX04-ENV |
| Sport/System | TRUCK FROGGER |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 4 (TRUCK FROGGER), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX04-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX04-LOGO |
| Sport/System | TRUCK FROGGER |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Optional human aesthetic review. Original imported PNG and actual gameplay preview were rendered and checked in the built menu. |


#### EX04-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX04-PREVIEW |
| Sport/System | TRUCK FROGGER |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Optional human aesthetic review. Original imported PNG and actual gameplay preview were rendered and checked in the built menu. |


#### EX04-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX04-SETUP |
| Sport/System | TRUCK FROGGER |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX04-HUD

| Field | Record |
|---|---|
| Requirement ID | EX04-HUD |
| Sport/System | TRUCK FROGGER |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX04-FX

| Field | Record |
|---|---|
| Requirement ID | EX04-FX |
| Sport/System | TRUCK FROGGER |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX04-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX04-RESULTS |
| Sport/System | TRUCK FROGGER |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX04-QA

| Field | Record |
|---|---|
| Requirement ID | EX04-QA |
| Sport/System | TRUCK FROGGER |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFrogger/Scenes/TruckFrogger.unity |
| Relevant Scripts | Assets/TruckFrogger/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch. Root causes: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsFrog48/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Frog-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


### EX05 — TRUCK NINJA WARRIOR


#### EX05-CORE

| Field | Record |
|---|---|
| Requirement ID | EX05-CORE |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 5 (TRUCK NINJA WARRIOR), including all confirmed design decisions. |
| Acceptance Criteria | The complete course must be physically traversable. Test every obstacle using player input. Verify jump/compression combinations, checkpoint penalties, timing, AI runs, and the warped-wall finish. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | The complete course must be physically traversable. Test every obstacle using player input. Verify jump/compression combinations, checkpoint penalties, timing, AI runs, and the warped-wall finish. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX05-AI

| Field | Record |
|---|---|
| Requirement ID | EX05-AI |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 5 (TRUCK NINJA WARRIOR), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX05-ENV

| Field | Record |
|---|---|
| Requirement ID | EX05-ENV |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 5 (TRUCK NINJA WARRIOR), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX05-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX05-LOGO |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Optional human aesthetic review. Original imported PNG and actual gameplay preview were rendered and checked in the built menu. |


#### EX05-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX05-PREVIEW |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Optional human aesthetic review. Original imported PNG and actual gameplay preview were rendered and checked in the built menu. |


#### EX05-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX05-SETUP |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX05-HUD

| Field | Record |
|---|---|
| Requirement ID | EX05-HUD |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX05-FX

| Field | Record |
|---|---|
| Requirement ID | EX05-FX |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX05-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX05-RESULTS |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX05-QA

| Field | Record |
|---|---|
| Requirement ID | EX05-QA |
| Sport/System | TRUCK NINJA WARRIOR |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity |
| Relevant Scripts | Assets/TruckNinjaWarrior/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch. Root causes: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from12m/s to0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsNinja36/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Ninja-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


### EX06 — TRUCK FLOOR IS LAVA


#### EX06-CORE

| Field | Record |
|---|---|
| Requirement ID | EX06-CORE |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 6 (TRUCK FLOOR IS LAVA), including all confirmed design decisions. |
| Acceptance Criteria | Verify collapsing platforms, real weight/collision interaction, ramming, AI decisions, lava elimination, and full matches. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Verify collapsing platforms, real weight/collision interaction, ramming, AI decisions, lava elimination, and full matches. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX06-AI

| Field | Record |
|---|---|
| Requirement ID | EX06-AI |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 6 (TRUCK FLOOR IS LAVA), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX06-ENV

| Field | Record |
|---|---|
| Requirement ID | EX06-ENV |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 6 (TRUCK FLOOR IS LAVA), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX06-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX06-LOGO |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX06-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX06-PREVIEW |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX06-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX06-SETUP |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX06-HUD

| Field | Record |
|---|---|
| Requirement ID | EX06-HUD |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX06-FX

| Field | Record |
|---|---|
| Requirement ID | EX06-FX |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX06-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX06-RESULTS |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX06-QA

| Field | Record |
|---|---|
| Requirement ID | EX06-QA |
| Sport/System | TRUCK FLOOR IS LAVA |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity |
| Relevant Scripts | Assets/TruckFloorIsLava/Scripts/ and Editor/; existing NWH moving-ground physics and FareFightRoute local steering. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFloorIsLava.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Twenty-five finite contact-activated basalt slabs; wide/narrow/moving/tilting/short-warning/temporary-safe surfaces, actual bounded jumps and ramming, warning/cracks/whole-body falls, genuine lava contact/debris, survival ranking and limited knockoff bonuses, locked replay/results/rematch. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 18/18; actual Windows NWH/PhysX acceptance 37checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Lava/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and isolated performance remain unverified. Moving-slab historical snow-track anchoring and severe-weather replay require visual follow-up; normal snow/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX07 — TRUCK AVALANCHE ESCAPE


#### EX07-CORE

| Field | Record |
|---|---|
| Requirement ID | EX07-CORE |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 7 (TRUCK AVALANCHE ESCAPE), including all confirmed design decisions. |
| Acceptance Criteria | Test a complete mountain race, genuinely approaching avalanche, physical snow contact, brief escapes, burial, branching routes, and AI navigation. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Test a complete mountain race, genuinely approaching avalanche, physical snow contact, brief escapes, burial, branching routes, and AI navigation. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX07-AI

| Field | Record |
|---|---|
| Requirement ID | EX07-AI |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 7 (TRUCK AVALANCHE ESCAPE), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX07-ENV

| Field | Record |
|---|---|
| Requirement ID | EX07-ENV |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 7 (TRUCK AVALANCHE ESCAPE), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX07-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX07-LOGO |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Optional human aesthetic review. Original imported PNG and actual gameplay preview were rendered and checked in the built menu. |


#### EX07-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX07-PREVIEW |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Optional human aesthetic review. Original imported PNG and actual gameplay preview were rendered and checked in the built menu. |


#### EX07-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX07-SETUP |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX07-HUD

| Field | Record |
|---|---|
| Requirement ID | EX07-HUD |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX07-FX

| Field | Record |
|---|---|
| Requirement ID | EX07-FX |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX07-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX07-RESULTS |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


#### EX07-QA

| Field | Record |
|---|---|
| Requirement ID | EX07-QA |
| Sport/System | TRUCK AVALANCHE ESCAPE |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity |
| Relevant Scripts | Assets/TruckAvalancheEscape/Scripts/, Editor/ and Tests/Editor/. |
| Prefabs or Assets | Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators. |
| Implementation Evidence | Saved scene, imported logo/preview, registered menu and 60-scene Windows build; Evidence/2026-10-20-Expansion/Eighteen/. |
| Implementation Notes | Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch. Root causes: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned15m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Final native EditMode331/331passed. Actual final WindowsAvalanche41/0; same DLLbab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36; shared adapter, all 18 snow and menu checks recorded in windows-processes.json. |
| Runtime Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-result.txt; genuine NWH physics and software-controlled1–4human input fixtures; replay/rematch assertions. |
| Screenshot/Video Evidence | Evidence/2026-10-20-Expansion/Eighteen/Avalanche-*.png; actual Windows composed-frame720p/1080p captures. |
| Human Verification | Not human-playtested or physically controlled in this acceptance pass. |
| Known Issues | Physical controllers/wheels, subjective audio/balance and extended soak acceptance remain unverified. Performance.json contains measured development-build telemetry, not a hardware certification. Easy/Normal complete races and long multi-round alternating tournaments were not exhaustively rerun. |
| Last Verification Date | 2026-10-21T06:20:50.619280+00:00 |
| Remaining Work | Human playtest and physical-device/listening acceptance; review measured performance and extended severe-weather/multi-round balance. No unimplemented core gameplay item is being substituted with a menu entry. |


### EX08 — TRUCK FREEZE TAG


#### EX08-CORE

| Field | Record |
|---|---|
| Requirement ID | EX08-CORE |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 8 (TRUCK FREEZE TAG), including all confirmed design decisions. |
| Acceptance Criteria | Verify freezing, persistent ice states, physical rescues, asymmetric roles, timers, scoring, and role reversal. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Verify freezing, persistent ice states, physical rescues, asymmetric roles, timers, scoring, and role reversal. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX08-AI

| Field | Record |
|---|---|
| Requirement ID | EX08-AI |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 8 (TRUCK FREEZE TAG), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX08-ENV

| Field | Record |
|---|---|
| Requirement ID | EX08-ENV |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 8 (TRUCK FREEZE TAG), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX08-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX08-LOGO |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX08-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX08-PREVIEW |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX08-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX08-SETUP |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX08-HUD

| Field | Record |
|---|---|
| Requirement ID | EX08-HUD |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX08-FX

| Field | Record |
|---|---|
| Requirement ID | EX08-FX |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX08-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX08-RESULTS |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX08-QA

| Field | Record |
|---|---|
| Requirement ID | EX08-QA |
| Sport/System | TRUCK FREEZE TAG |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity |
| Relevant Scripts | Assets/TruckFreezeTag/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckFreezeTag.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Asymmetric four-round freezer/runner teams, real first-solid spray casts and cooldown, three-hit freezing, no automatic thaw, real teammate-impact rescue, visible frozen/shield states, alternating roles and timed/all-frozen wins, physical AI pursuit/evasion/rescue, actual collision-matched stacked container cover, pooled Weatherade sample spray, recorded review/reset. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 17/17; actual Windows NWH/PhysX acceptance 36checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Freeze/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balance, physical devices/listening and isolated profiling remain unverified; automated full matches usually favored runners and shared2–2results. Pure rules verify all-frozen wins. Original sample destroy-after-stop behavior and mismatched cover dimensions were fixed only in owned instances/scene. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX09 — TRUCK SEESAW


#### EX09-CORE

| Field | Record |
|---|---|
| Requirement ID | EX09-CORE |
| Sport/System | TRUCK SEESAW |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 9 (TRUCK SEESAW), including all confirmed design decisions. |
| Acceptance Criteria | Verify real multidirectional platform response, recoverable tilts, legitimate falls, AI balancing, and proper quarry impacts. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Verify real multidirectional platform response, recoverable tilts, legitimate falls, AI balancing, and proper quarry impacts. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX09-AI

| Field | Record |
|---|---|
| Requirement ID | EX09-AI |
| Sport/System | TRUCK SEESAW |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 9 (TRUCK SEESAW), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX09-ENV

| Field | Record |
|---|---|
| Requirement ID | EX09-ENV |
| Sport/System | TRUCK SEESAW |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 9 (TRUCK SEESAW), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX09-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX09-LOGO |
| Sport/System | TRUCK SEESAW |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX09-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX09-PREVIEW |
| Sport/System | TRUCK SEESAW |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX09-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX09-SETUP |
| Sport/System | TRUCK SEESAW |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX09-HUD

| Field | Record |
|---|---|
| Requirement ID | EX09-HUD |
| Sport/System | TRUCK SEESAW |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX09-FX

| Field | Record |
|---|---|
| Requirement ID | EX09-FX |
| Sport/System | TRUCK SEESAW |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX09-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX09-RESULTS |
| Sport/System | TRUCK SEESAW |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX09-QA

| Field | Record |
|---|---|
| Requirement ID | EX09-QA |
| Sport/System | TRUCK SEESAW |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSeesaw/Scenes/TruckSeesaw.unity |
| Relevant Scripts | Assets/TruckSeesaw/Scripts/ and Editor/; existing NWH wheel reaction forces/ConfigurableJoint and FareFightRoute direct steering adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSeesaw.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical two-axis suspended quarry board, genuine NWH wheel loads and movement, actual AI balancing/ramming, three-heat scoring, measured uphill recovery, attributed knockoffs, real falls/quarry-impact fragments, round reset, replay and shared/chase/cab presentation. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 39checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Seesaw/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Physical devices, listening, human pacing and isolated performance remain unverified. Moving-deck snow-track anchoring and severe-weather replays need visual follow-up; actual normal snow coverage/contact stamps are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX10 — TRUCK VOLLEYBALL


#### EX10-CORE

| Field | Record |
|---|---|
| Requirement ID | EX10-CORE |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 10 (TRUCK VOLLEYBALL), including all confirmed design decisions. |
| Acceptance Criteria | Verify serve, rally, touch limits, out-of-bounds calls, real ball contact, net behavior, scoring, and complete matches. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Verify serve, rally, touch limits, out-of-bounds calls, real ball contact, net behavior, scoring, and complete matches. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX10-AI

| Field | Record |
|---|---|
| Requirement ID | EX10-AI |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 10 (TRUCK VOLLEYBALL), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX10-ENV

| Field | Record |
|---|---|
| Requirement ID | EX10-ENV |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 10 (TRUCK VOLLEYBALL), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX10-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX10-LOGO |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX10-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX10-PREVIEW |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX10-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX10-SETUP |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX10-HUD

| Field | Record |
|---|---|
| Requirement ID | EX10-HUD |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX10-FX

| Field | Record |
|---|---|
| Requirement ID | EX10-FX |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX10-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX10-RESULTS |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX10-QA

| Field | Record |
|---|---|
| Requirement ID | EX10-QA |
| Sport/System | TRUCK VOLLEYBALL |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckVolleyball/Scenes/TruckVolleyball.unity |
| Relevant Scripts | Assets/TruckVolleyball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckVolleyball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Physical oversized beach ball and solid net, NWH suspension jumps, actual contact-normal returns, three-touch/fourth-touch/double-touch rules, first15winby2cap21, 1–4perteam, predictive physical AI, real service/rallies, court fault resolution, locked outcomes, recorded review, reset and cab/shared views. Sand/palms, posed spectators and original grungy PNG are integrated. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding twelve-sport build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Volley/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical controls, subjective listening and isolated long-run profiling remain unverified. Initial floor fixture overlapped a real opposing truck; moving only the fixture isolated true court contact, without relaxing scoring. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 13/13; actual Windows NWH/PhysX acceptance 34checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Balloon/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
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
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Verify real attraction/repulsion, correct polarity, force limits, scrap movement, contested scoring, AI strategies, and physics stability. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX12-AI

| Field | Record |
|---|---|
| Requirement ID | EX12-AI |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 12 (TRUCK MAGNET MAYHEM), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX12-ENV

| Field | Record |
|---|---|
| Requirement ID | EX12-ENV |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 12 (TRUCK MAGNET MAYHEM), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX12-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX12-LOGO |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX12-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX12-PREVIEW |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX12-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX12-SETUP |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX12-HUD

| Field | Record |
|---|---|
| Requirement ID | EX12-HUD |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX12-FX

| Field | Record |
|---|---|
| Requirement ID | EX12-FX |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX12-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX12-RESULTS |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX12-QA

| Field | Record |
|---|---|
| Requirement ID | EX12-QA |
| Sport/System | TRUCK MAGNET MAYHEM |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity |
| Relevant Scripts | Assets/TruckMagnetMayhem/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckMagnetMayhem.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Eight full NWH magnet tractors,40finite owned canister/container/car scrap objects, OFF/positive/negative modes, bounded mass-aware occluded equal/opposite PhysX forces, actual whole-object1.5s settled-bay scoring, physical reverse-hauling AI, timed shared placings, rotating machinery, complete reset/replay/cab views, original PNG and recorded preview. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 21/21; actual Windows NWH/PhysX acceptance 38checks/0errors on preceding ten-sport gameplay build; original gameplay sources/scenes preserved exactly, Assembly-CSharp SHA256 d4ed62b2586c3a0b729a4be55b9bac4b21be8497d23b070a52e8deb30e911bbe; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Magnet/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human playtest, physical controls, listening and isolated CPU/GPU performance remain unverified. First Windows fixture interruption was preserved as failed; bounded/diagnostic harness and fresh complete rerun are recorded separately. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX13 — TRUCK TETHERBALL


#### EX13-CORE

| Field | Record |
|---|---|
| Requirement ID | EX13-CORE |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 13 (TRUCK TETHERBALL), including all confirmed design decisions. |
| Acceptance Criteria | Verify ball swinging, rope wrapping, direction changes, scoring, truck collisions, AI play, and resets. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Verify ball swinging, rope wrapping, direction changes, scoring, truck collisions, AI play, and resets. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX13-AI

| Field | Record |
|---|---|
| Requirement ID | EX13-AI |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 13 (TRUCK TETHERBALL), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX13-ENV

| Field | Record |
|---|---|
| Requirement ID | EX13-ENV |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 13 (TRUCK TETHERBALL), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX13-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX13-LOGO |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX13-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX13-PREVIEW |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX13-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX13-SETUP |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX13-HUD

| Field | Record |
|---|---|
| Requirement ID | EX13-HUD |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX13-FX

| Field | Record |
|---|---|
| Requirement ID | EX13-FX |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX13-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX13-RESULTS |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX13-QA

| Field | Record |
|---|---|
| Requirement ID | EX13-QA |
| Sport/System | TRUCK TETHERBALL |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckTetherball/Scenes/TruckTetherball.unity |
| Relevant Scripts | Assets/TruckTetherball/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckTetherball.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two full NWH trucks physically strike a giant ball on one bounded ConfigurableJoint cable; actual angular travel winds/unwinds720degrees for a round, best3/5, AI intercepts, no contact-based free scores, reset/replay/cab/shared views. Original PNG and recorded gameplay preview installed. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 14/14; actual Windows NWH/PhysX acceptance 19checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Tether/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX14 — TRUCK JOUSTING


#### EX14-CORE

| Field | Record |
|---|---|
| Requirement ID | EX14-CORE |
| Sport/System | TRUCK JOUSTING |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 14 (TRUCK JOUSTING), including all confirmed design decisions. |
| Acceptance Criteria | Verify lance adjustment, real contact, hit zones, breakage, proper score awards, repeated passes, and competition results. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Verify lance adjustment, real contact, hit zones, breakage, proper score awards, repeated passes, and competition results. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX14-AI

| Field | Record |
|---|---|
| Requirement ID | EX14-AI |
| Sport/System | TRUCK JOUSTING |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 14 (TRUCK JOUSTING), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX14-ENV

| Field | Record |
|---|---|
| Requirement ID | EX14-ENV |
| Sport/System | TRUCK JOUSTING |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 14 (TRUCK JOUSTING), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX14-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX14-LOGO |
| Sport/System | TRUCK JOUSTING |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX14-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX14-PREVIEW |
| Sport/System | TRUCK JOUSTING |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX14-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX14-SETUP |
| Sport/System | TRUCK JOUSTING |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX14-HUD

| Field | Record |
|---|---|
| Requirement ID | EX14-HUD |
| Sport/System | TRUCK JOUSTING |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX14-FX

| Field | Record |
|---|---|
| Requirement ID | EX14-FX |
| Sport/System | TRUCK JOUSTING |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX14-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX14-RESULTS |
| Sport/System | TRUCK JOUSTING |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX14-QA

| Field | Record |
|---|---|
| Requirement ID | EX14-QA |
| Sport/System | TRUCK JOUSTING |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckJousting/Scenes/TruckJousting.unity |
| Relevant Scripts | Assets/TruckJousting/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckJousting.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Two NWH tractors charge in separate lanes with physical adjustable side lances and armor targets; real normal-speed shield contacts earn1/2points, observed knockdown upgrades to3, one hit per pass, Core Collapse pooled splinters,3/5passes, real AI aim, actual cab input, review/rematch. Prestarting NWH engine during held countdown fixes acceleration delay after aim mode. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 15/15; actual Windows NWH/PhysX acceptance 21checks/0errors on preceding nine-sport build; original gameplay source/scene preserved exactly, Assembly-CSharp SHA256 cfc08af13339f2c91a7443ffeb1804a35ff8a4f4569b2bd2fc63c17c537d1c9c; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–two software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Joust/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing, physical controls, listening and long-run performance remain unverified. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX15 — HUNGRY HUNGRY HAULERS


#### EX15-CORE

| Field | Record |
|---|---|
| Requirement ID | EX15-CORE |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 15 (HUNGRY HUNGRY HAULERS), including all confirmed design decisions. |
| Acceptance Criteria | Verify physical collection, scoop animation, cargo capacity, deposits, scoring, golden balls, explosive penalties, AI, and rematches. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Verify physical collection, scoop animation, cargo capacity, deposits, scoring, golden balls, explosive penalties, AI, and rematches. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX15-AI

| Field | Record |
|---|---|
| Requirement ID | EX15-AI |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 15 (HUNGRY HUNGRY HAULERS), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX15-ENV

| Field | Record |
|---|---|
| Requirement ID | EX15-ENV |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 15 (HUNGRY HUNGRY HAULERS), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX15-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX15-LOGO |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX15-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX15-PREVIEW |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX15-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX15-SETUP |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX15-HUD

| Field | Record |
|---|---|
| Requirement ID | EX15-HUD |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX15-FX

| Field | Record |
|---|---|
| Requirement ID | EX15-FX |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX15-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX15-RESULTS |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX15-QA

| Field | Record |
|---|---|
| Requirement ID | EX15-QA |
| Sport/System | HUNGRY HUNGRY HAULERS |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity |
| Relevant Scripts | Assets/TruckHungryHaulers/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckHungryHaulers.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Four full NWH tractors and256finite physical rubber/gold/bomb balls; actual DOTween telescoping scoop/lip contacts, six visible hopper slots, no collection points, one-time owned-bin deposits, genuine rival-ram cargo spill, bounded explosive balls, physical AI approach/banking, timed standings, recorded review/reset/cabs. HaulerMouth now has its own real MonoScript asset; first Windows serialized-level crash is preserved and the dedicated binding regression prevents embedded script instances. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 20/20; actual Windows NWH/PhysX acceptance 45checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hauler/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human pacing/balance, physical devices, listening and isolated long-run profiling remain unverified. The first Windows level54 was corrupted by an embedded intake MonoScript; dedicated asset bindings and current Windows45/0 prove the repaired build. Full native45/0 also verifies actual cargo/ram contacts and NWH damage repair on rematch. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Automated Verification | Unity6000.4.10f1 native EditMode 12/12; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
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
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Verify contested-zone logic, correct timing, physical occupancy, moving zones, AI navigation, and victory conditions. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX17-AI

| Field | Record |
|---|---|
| Requirement ID | EX17-AI |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 17 (TRUCK KING OF THE HILL), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX17-ENV

| Field | Record |
|---|---|
| Requirement ID | EX17-ENV |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 17 (TRUCK KING OF THE HILL), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX17-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX17-LOGO |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX17-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX17-PREVIEW |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX17-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX17-SETUP |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX17-HUD

| Field | Record |
|---|---|
| Requirement ID | EX17-HUD |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX17-FX

| Field | Record |
|---|---|
| Requirement ID | EX17-FX |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX17-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX17-RESULTS |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX17-QA

| Field | Record |
|---|---|
| Requirement ID | EX17-QA |
| Sport/System | TRUCK KING OF THE HILL |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity |
| Relevant Scripts | Assets/TruckKingOfTheHill/Scripts/ and Editor/; existing FareFightRoute graph overload/direct local-goal adapter. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckKingOfTheHill.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | Five genuine collision-matched mountaintop plateaus, eight EasyRoads bridges, existing LWS connected navigation graph, real sole-occupant grounded control, contested ring, announced relocations, physical AI combat/cliff checks, safe recovery, locked results/rematch/review. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 22/22; actual Windows NWH/PhysX acceptance 33checks/0errors on preceding eight-sport build; original gameplay source/scene preserved exactly in final build, Assembly-CSharp SHA256 9dbdb0a2c5b7ec3cc3921debdfbe8526df8686c7647753107b675b6412a1aaf3; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Hill/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Severe-weather replay presentation, subjective audio mix, physical controls and human gameplay remain unverified. Focused shared snow accumulation and actual NWH tire-contact tracks passed; exact checks in the current receipt; see its receipt. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


### EX18 — TRUCK SHEEP HERDING


#### EX18-CORE

| Field | Record |
|---|---|
| Requirement ID | EX18-CORE |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Complete playable rules and genuine physical gameplay. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 18 (TRUCK SHEEP HERDING), including all confirmed design decisions. |
| Acceptance Criteria | Verify animated sheep movement, horn reactions, truck herding pressure, pen detection, scoring, explosive sheep, AI behavior, and full matches. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Verify animated sheep movement, horn reactions, truck herding pressure, pen detection, scoring, explosive sheep, AI behavior, and full matches. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX18-AI

| Field | Record |
|---|---|
| Requirement ID | EX18-AI |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Actual difficulty-aware sport-specific AI under the same rules;1–4local humans and AI-only viewing where practical. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 18 (TRUCK SHEEP HERDING), including all confirmed design decisions. |
| Acceptance Criteria | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Observe genuine objective actions, fair physical interactions, appropriate difficulty and complete autonomous matches; no invented scores. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX18-ENV

| Field | Record |
|---|---|
| Requirement ID | EX18-ENV |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Detailed sport-specific modeled environment using audited owned assets. Documentation/TruckSports/Requests/2026-10-20-Eighteen-Sports.md, GAME 18 (TRUCK SHEEP HERDING), including all confirmed design decisions. |
| Acceptance Criteria | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Connected reachable play space meets the requested setting/scale; no final grayboxes, invisible blockers or empty floating arenas; actual render and performance inspection. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX18-LOGO

| Field | Record |
|---|---|
| Requirement ID | EX18-LOGO |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Unique original PNG logo/icon with cohesive PS1/PS2-era Truck Sports art direction. |
| Acceptance Criteria | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Real imported artwork assigned in menu/setup and suitable venue presentation; inspect readability, alpha, aspect and distinctness at720p/1080p. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX18-PREVIEW

| Field | Record |
|---|---|
| Requirement ID | EX18-PREVIEW |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Actual representative gameplay screenshot in the menu selection card. |
| Acceptance Criteria | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Status | VERIFIED COMPLETE |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Capture current actual scene gameplay; assign imported image; inspect normal menu rendering and source provenance. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human aesthetic review of final Windows placement; original41logos preserved. No missing image integration in targeted menu probe. |


#### EX18-SETUP

| Field | Record |
|---|---|
| Requirement ID | EX18-SETUP |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Polished match setup: relevant options, AI levels, names, weather, cameras and controller ownership. |
| Acceptance Criteria | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Existing Heat/shared components, no debug setup; options apply, identities persist, real input prompts and normal menu launch work. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX18-HUD

| Field | Record |
|---|---|
| Requirement ID | EX18-HUD |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Complete polished viewport-safe gameplay HUD and sport-appropriate countdown/start. |
| Acceptance Criteria | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Attractive identity/portrait panels, scores/timer/progress and mode information;1–4human layouts, AI camera policy and readable720p/1080p output. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX18-FX

| Field | Record |
|---|---|
| Requirement ID | EX18-FX |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Animated scoring, eliminations, bonuses and notable-event feedback. |
| Acceptance Criteria | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Actual authoritative events trigger bounded coherent DOTween/audio/visual feedback once, correctly attributed; pause/reset clean up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX18-RESULTS

| Field | Record |
|---|---|
| Requirement ID | EX18-RESULTS |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Polished victory, defeat, placements/statistics, recorded replay/rematch and menu return. |
| Acceptance Criteria | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Complete match resolves valid outcome, displays actual identities/statistics, replays preserve results, rematch resets objects/scores and exit cleans up. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


#### EX18-QA

| Field | Record |
|---|---|
| Requirement ID | EX18-QA |
| Sport/System | TRUCK SHEEP HERDING |
| Original Request | Targeted EditMode/PlayMode, actual matches, multiplayer/render/performance and regressions. |
| Acceptance Criteria | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Status | IMPLEMENTED / UNVERIFIED |
| Relevant Unity Scene | Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity |
| Relevant Scripts | Assets/TruckSheepHerding/Scripts/ and Editor/; shared SportsRecordedReview, SportsExpansionViewHud, SportsExpansionSpectators. |
| Prefabs or Assets | Full NWH sport tractor/cab, owned venue/posed crowd and original Assets/TruckSports/Art/Logos/TruckSheepHerding.png; see vendor audit. |
| Implementation Evidence | Saved scene and runtime integration; Documentation/TruckSports/EighteenSportsProgress.md; Evidence/2026-10-20-Expansion/. |
| Implementation Notes | 48genuine rigged SheepV5 animals, existing locomotion, Unity NavMesh flock/threat/avoidance, real truck/horn pressure, whole-body one-time pen contacts, completion-time tie policy, physical AI herding, marked explosive sheep and bounded loose-fence reactions, detailed rolling farm with original timber barn and reused Vista scenery, 3minute competition, recorded review/reset/cabs. |
| Test Procedure | Execute all original sport acceptance scenarios plus720p/1080p, full AI match, physical scenarios and repeated rematches; record measured outcomes, errors and exact hardware/human limitations. |
| Automated Verification | Unity6000.4.10f1 native EditMode 19/19; actual Windows NWH/PhysX acceptance 52checks/0errors on final fourteen-sport build, Assembly-CSharp SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700; final common menu359/0 and fourteen-sport native227/227. |
| Runtime Evidence | Full AI match, actual physical interaction, pause, recorded replay, rematch, one–four software-controller viewport/input allocation and AI-only camera checks executed. Windows processes exited0; no physical hardware or human certification. |
| Screenshot/Video Evidence | Logs/TruckSports/EighteenSports/Herd/; selected actual Windows frames copied to tracked Evidence/2026-10-20-Expansion/. |
| Human Verification | Not performed for this new sport. No subjective listening or physical-device acceptance claimed. |
| Known Issues | Human balancing/pacing, physical controls, listening and isolated profiling remain unverified. Native and Windows52/0 include actual contested captures and independent physical penning by all four AI slots. An earlier every-loser-must-score assumption was replaced with stronger isolated capability cases. Those cases exposed retained NWH damage across rematches; the vendor repair API now restores engines, transmissions and tyres, with deliberately damaged-start regressions. This is not human balance acceptance. |
| Last Verification Date | 2026-10-20 |
| Remaining Work | Human playtest; physical devices/listening; severe-weather/long-run performance and remaining original presentation details. Preserve passed focused acceptance evidence. |


## King of the Hill — bottom scorecard row

### EX17-HUD-ROW-001 — King of the Hill

| Field | Record |
|---|---|
| Requirement ID | EX17-HUD-ROW-001 |
| Sport/System | King of the Hill / broadcast HUD |
| Original Request | Fit all participant cards on one row at the bottom, just above the existing bottom information line (October20 screenshot). |
| Acceptance Criteria | Eight cards occupy one row immediately above the status plate; names, values, status and progress remain within their cards at720p/1080p; smaller rosters resize on rematch. |
| Status | VERIFIED COMPLETE |
| Implementation Evidence | HillMatch.CompactScoreColumns=8; SportsBroadcastScores single-row layout with score on second line, compact portrait/name and bounded progress width. Other sports keep existing layouts. |
| Automated Verification | 24/24 targeted native EditMode regressions; native actual-scene HUD10/0; exact Windows composed-frame HUD10/0 at1280x720 and1920x1080, with two/four/eight-truck rematches. All cards/text/progress remain bounded, same row, eight reference-pixels above the status plate. Windows build succeeded with0errors/57existing warnings. Evidence: Documentation/TruckSports/Evidence/2026-10-20-Expansion/HillHud/. |
| Human Verification | User screenshot confirms the prior two-row layout was too high; no acceptance of this correction yet. |
| Remaining Work | None for this layout request in exercised resolutions/rosters. Human aesthetic review remains unperformed; this entry does not certify the broader expansion candidate. |

## October 21 human playtest repair and broadcast-camera pass

Permanent IDs below preserve both supplied directives. Initial PARTIAL means a reported defect or requested change awaits current-scene investigation, not that a fix has been established. Human feedback is recorded separately. Camera pass precedes HPR work; Bowling cameras and fixed precision/strategic views are protected exceptions.

### GBC-01 — ESTABLISH MINIMUM VISIBLE TRUCK SIZE

| Field | Record |
|---|---|
| Requirement ID | GBC-01 |
| Sport/System | ESTABLISH MINIMUM VISIBLE TRUCK SIZE |
| Original Request | The main rule: Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section1. |
| Acceptance Criteria | All observable requirements and acceptance in source section1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-02 — ADD A MAXIMUM BROADCAST CAMERA DISTANCE / ZOOM LIMIT

| Field | Record |
|---|---|
| Requirement ID | GBC-02 |
| Sport/System | ADD A MAXIMUM BROADCAST CAMERA DISTANCE / ZOOM LIMIT |
| Original Request | Every shared camera needs an appropriate maximum zoom-out boundary. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section2. |
| Acceptance Criteria | All observable requirements and acceptance in source section2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-03 — STOP TRYING TO KEEP EVERY TRUCK VISIBLE AT ALL TIMES

| Field | Record |
|---|---|
| Requirement ID | GBC-03 |
| Sport/System | STOP TRYING TO KEEP EVERY TRUCK VISIBLE AT ALL TIMES |
| Original Request | This is likely a major cause of the problem. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section3. |
| Acceptance Criteria | All observable requirements and acceptance in source section3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-04 — IMPLEMENT INTELLIGENT DYNAMIC FRAMING

| Field | Record |
|---|---|
| Requirement ID | GBC-04 |
| Sport/System | IMPLEMENT INTELLIGENT DYNAMIC FRAMING |
| Original Request | The broadcast camera should adjust according to actual gameplay. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section4. |
| Acceptance Criteria | All observable requirements and acceptance in source section4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-05 — ADD OFFSCREEN COMPETITOR INDICATORS WHERE USEFUL

| Field | Record |
|---|---|
| Requirement ID | GBC-05 |
| Sport/System | ADD OFFSCREEN COMPETITOR INDICATORS WHERE USEFUL |
| Original Request | When important competitors leave the camera view, use subtle directional indicators instead of zooming out excessively. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section5. |
| Acceptance Criteria | All observable requirements and acceptance in source section5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-06 — TRUCK KICKBALL — SPECIFIC CAMERA REPAIR

| Field | Record |
|---|---|
| Requirement ID | GBC-06 |
| Sport/System | TRUCK KICKBALL — SPECIFIC CAMERA REPAIR |
| Original Request | Kickball is one of the worst offenders. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section6. |
| Acceptance Criteria | All observable requirements and acceptance in source section6; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-07 — TRUCK MAGNET MAYHEM — SPECIFIC CAMERA REPAIR

| Field | Record |
|---|---|
| Requirement ID | GBC-07 |
| Sport/System | TRUCK MAGNET MAYHEM — SPECIFIC CAMERA REPAIR |
| Original Request | Magnet Mayhem also suffers from trucks appearing extremely small. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section7. |
| Acceptance Criteria | All observable requirements and acceptance in source section7; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-08 — FIX ALL OTHER APPLICABLE SPORTS

| Field | Record |
|---|---|
| Requirement ID | GBC-08 |
| Sport/System | FIX ALL OTHER APPLICABLE SPORTS |
| Original Request | Audit every registered Truck Sports mode that uses: Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section8. |
| Acceptance Criteria | All observable requirements and acceptance in source section8; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### GBC-09 — SUPPORT HUMAN-ONLY AND AI CAMERA OPTIONS

| Field | Record |
|---|---|
| Requirement ID | GBC-09 |
| Sport/System | SUPPORT HUMAN-ONLY AND AI CAMERA OPTIONS |
| Original Request | Respect the existing Show AI Cameras setting. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section9. |
| Acceptance Criteria | All observable requirements and acceptance in source section9; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-10 — CAMERA SAFETY AND SMOOTHING

| Field | Record |
|---|---|
| Requirement ID | GBC-10 |
| Sport/System | CAMERA SAFETY AND SMOOTHING |
| Original Request | Preserve functional camera movement. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section10. |
| Acceptance Criteria | All observable requirements and acceptance in source section10; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-11 — CONFIGURABLE SHARED CAMERA SETTINGS

| Field | Record |
|---|---|
| Requirement ID | GBC-11 |
| Sport/System | CONFIGURABLE SHARED CAMERA SETTINGS |
| Original Request | Create or update a reusable camera-framing configuration with parameters such as: Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section11. |
| Acceptance Criteria | All observable requirements and acceptance in source section11; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-12 — MANDATORY VISUAL ACCEPTANCE TESTS

| Field | Record |
|---|---|
| Requirement ID | GBC-12 |
| Sport/System | MANDATORY VISUAL ACCEPTANCE TESTS |
| Original Request | This issue is fundamentally visual. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section12. |
| Acceptance Criteria | All observable requirements and acceptance in source section12; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### GBC-13 — BEFORE-AND-AFTER EVIDENCE

| Field | Record |
|---|---|
| Requirement ID | GBC-13 |
| Sport/System | BEFORE-AND-AFTER EVIDENCE |
| Original Request | For Kickball, Magnet Mayhem, and at least three other affected sports, provide: Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section13. |
| Acceptance Criteria | All observable requirements and acceptance in source section13; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SportsBroadcastFraming/Settings/Guidance/Compass; SportsBroadcastCameraAuthoring;33explicitly bound saved scenes; Cinemachine and CNP adapters. See PlaytestRepairProgress.md for mode-specific limits. |
| Automated Verification | Native broadcast9/9; exact Windows Full4 candidate b47e7720:693checks/0errors, actual720p/1080p captures, projected truck bounds, objective/zoom limits, all-human cue coverage, ownership and rematches. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Complete-match/action coverage beyond the targeted33-scene fixtures; human driving, visual-preference and physical-device acceptance remain unperformed. Protected precision/strategic views are not blanket-certified. |

### GBC-14 — UPDATE THE PERMANENT ACCEPTANCE LEDGER

| Field | Record |
|---|---|
| Requirement ID | GBC-14 |
| Sport/System | UPDATE THE PERMANENT ACCEPTANCE LEDGER |
| Original Request | Create or update the shared broadcast-camera requirements in the existing Truck Sports ledger. Full authoritative scope: Requests/2026-10-21-GlobalBroadcastCamera.md, section14. |
| Acceptance Criteria | All observable requirements and acceptance in source section14; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-1-1 — Fix the Broken Audio After Kicking

| Field | Record |
|---|---|
| Requirement ID | HPR-1-1 |
| Sport/System | Fix the Broken Audio After Kicking |
| Original Request | Human testing revealed an extremely unpleasant, broken sound after kicking the football. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section1.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section1.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-1-2 — Align the Holder Truck Correctly

| Field | Record |
|---|---|
| Requirement ID | HPR-1-2 |
| Sport/System | Align the Holder Truck Correctly |
| Original Request | The holder truck is currently positioned incorrectly relative to the football. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section1.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section1.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-1-3 — Holder Truck Victory Dance

| Field | Record |
|---|---|
| Requirement ID | HPR-1-3 |
| Sport/System | Holder Truck Victory Dance |
| Original Request | When a field goal succeeds, the holder truck should perform a cute celebration. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section1.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section1.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-1-4 — Preserve Other Football Features

| Field | Record |
|---|---|
| Requirement ID | HPR-1-4 |
| Sport/System | Preserve Other Football Features |
| Original Request | Keep the current: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section1.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section1.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-2-1 — Restore Actual Fish Catching

| Field | Record |
|---|---|
| Requirement ID | HPR-2-1 |
| Sport/System | Restore Actual Fish Catching |
| Original Request | Investigate: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section2.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section2.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FishingStream.Progress/Advance aligns visible movement and analytic moving-net crossing; original1.1m hoop radius and score authority retained. |
| Automated Verification | FishingCrossingRegression12/12; Windows FishingRiverAndTiming343/0; complete Easy/Normal rounds plus FishingHardCompletion350/0 (620.12match seconds, fourAI,396/389/376/372catches, no overflow); real rematch reset and720p/1080p river screenshots inspected. Hardware/human/listening acceptance unperformed. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Rerun corrected Windows ten-catch fixture and complete survival/rematches after the speed/river update. |

### HPR-2-2 — Correct Fish Movement Speed

| Field | Record |
|---|---|
| Requirement ID | HPR-2-2 |
| Sport/System | Correct Fish Movement Speed |
| Original Request | The previous requests for much faster fish have not produced the desired result. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section2.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section2.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FishingDirector baseline2.0x,+0.5per30seconds, configurable6xcap, deterministic per-fish tempo variation; Match passes actual arrival-clock offset. |
| Automated Verification | FishingCrossingRegression12/12; Windows FishingRiverAndTiming343/0; complete Easy/Normal rounds plus FishingHardCompletion350/0 (620.12match seconds, fourAI,396/389/376/372catches, no overflow); real rematch reset and720p/1080p river screenshots inspected. Hardware/human/listening acceptance unperformed. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Windows measured motion/speed, full human/AI rounds and difficulty balance remain to verify. |

### HPR-2-3 — Redesign the Waterway

| Field | Record |
|---|---|
| Requirement ID | HPR-2-3 |
| Sport/System | Redesign the Waterway |
| Original Request | Reduce the horizontal fish-travel area to approximately half its current width. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section2.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section2.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FishingRepairAuthoring.Upgrade version1; saved Fishing scene with half-width fish/net area, winding240m owned water mesh/Terrain, installed JC trees and existing grass/boulders, clear pier apron. |
| Automated Verification | FishingCrossingRegression12/12; Windows FishingRiverAndTiming343/0; complete Easy/Normal rounds plus FishingHardCompletion350/0 (620.12match seconds, fourAI,396/389/376/372catches, no overflow); real rematch reset and720p/1080p river screenshots inspected. Hardware/human/listening acceptance unperformed. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect rendered banks/bend at720p/1080p and offscreen births, validate all streams/rematches. |

### HPR-2-4 — Increase Fish Size by 30%

| Field | Record |
|---|---|
| Requirement ID | HPR-2-4 |
| Sport/System | Increase Fish Size by 30% |
| Original Request | Increase the actual fish models to approximately 130% of their current size. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section2.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section2.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Three sport-owned fish prefab roots set to130%scale by versioned scene migration; original Layer Lab source models/materials/rigs unchanged. |
| Automated Verification | FishingCrossingRegression12/12; Windows FishingRiverAndTiming343/0; complete Easy/Normal rounds plus FishingHardCompletion350/0 (620.12match seconds, fourAI,396/389/376/372catches, no overflow); real rematch reset and720p/1080p river screenshots inspected. Hardware/human/listening acceptance unperformed. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Validate rendered scale/clearances and catch boundaries in the new player. |

### HPR-2-5 — Preserve Other Fishing Features

| Field | Record |
|---|---|
| Requirement ID | HPR-2-5 |
| Sport/System | Preserve Other Fishing Features |
| Original Request | Retain: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section2.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section2.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-3-1 — Reduce Negative Targets

| Field | Record |
|---|---|
| Requirement ID | HPR-3-1 |
| Sport/System | Reduce Negative Targets |
| Original Request | There are too many -100 targets. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section3.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section3.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-3-2 — Make Negative Targets Obvious

| Field | Record |
|---|---|
| Requirement ID | HPR-3-2 |
| Sport/System | Make Negative Targets Obvious |
| Original Request | Add a highly visible radiant aura to -100 targets. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section3.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section3.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-3-3 — Restore Truck Taxi Pedestrian Physics

| Field | Record |
|---|---|
| Requirement ID | HPR-3-3 |
| Sport/System | Restore Truck Taxi Pedestrian Physics |
| Original Request | Pedestrians currently slow down or overturn the player's truck far too much. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section3.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section3.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-3-4 — Simplify the HUD

| Field | Record |
|---|---|
| Requirement ID | HPR-3-4 |
| Sport/System | Simplify the HUD |
| Original Request | Replace the crowded current interface with a compact, polished point scoreboard. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section3.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section3.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-04 — CAB CHAOS

| Field | Record |
|---|---|
| Requirement ID | HPR-04 |
| Sport/System | CAB CHAOS |
| Original Request |  Clarify and Verify the Actual Game Identity Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section4. |
| Acceptance Criteria | All observable requirements and acceptance in source section4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-5-1 — Broadcast Camera Position

| Field | Record |
|---|---|
| Requirement ID | HPR-5-1 |
| Sport/System | Broadcast Camera Position |
| Original Request | Maintain the shared-camera distance standard established by Truck Keepaway. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section5.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section5.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-5-2 — Fix Home-Run Resolution

| Field | Record |
|---|---|
| Requirement ID | HPR-5-2 |
| Sport/System | Fix Home-Run Resolution |
| Original Request | Current bug: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section5.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section5.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | KickballRules.HomeRun atomically credits pending/active eligible runs, clears live runners and rejects repeat/third-out credit. Match parks ball and suspends interactions during3.4second sealed celebration before existing replay/next-plate restoration. |
| Automated Verification | New before-fix native2/2fail with zero awarded runs. kickball-after.xml33/33pass, including grand slam, immediate single, late tags, duplicate denial and previous rules. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Run actual Windows fence-crossing fixture, complete match/rematch and targeted PlayMode gameplay tests. |

### HPR-5-3 — Add Home-Run Celebration

| Field | Record |
|---|---|
| Requirement ID | HPR-5-3 |
| Sport/System | Add Home-Run Celebration |
| Original Request | Use: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section5.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section5.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved Kickball HUD banner, DOTween entrance/cab flourish, existing stadium crowd/audio reaction; celebration uses sealed score and existing action director. |
| Automated Verification | KickballRepairAuthoring bound banner in the real scene. Windows rendering, animation/audio subjective acceptance pending. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Verify banner at720p/1080p, camera, cleanup, next-play control ownership and human/listening acceptance. |

### HPR-5-4 — Waiting-Line Camera Freedom

| Field | Record |
|---|---|
| Requirement ID | HPR-5-4 |
| Sport/System | Waiting-Line Camera Freedom |
| Original Request | When a human player is waiting to bat: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section5.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section5.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-5-5 — Fast-Forward to Next At-Bat

| Field | Record |
|---|---|
| Requirement ID | HPR-5-5 |
| Sport/System | Fast-Forward to Next At-Bat |
| Original Request | Add an option to fast-forward AI-controlled play until the user's next batting opportunity. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section5.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section5.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-6-1 — Repair Camera Jitter

| Field | Record |
|---|---|
| Requirement ID | HPR-6-1 |
| Sport/System | Repair Camera Jitter |
| Original Request | The camera is extremely jerky. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section6.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section6.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SkyTowMatch.Present removed a second competing camera evaluation; existing spline camera is sole pose owner. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalSkyGateNormals:10/0; complete four-tow physical course, all finish, ordinary gates25-26/30, no duplicate camera poses/outside centers;720p/1080p images visually inspected, invalid-normal glare and black triangles gone. Native Sky26/26. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human smoothness, difficulty, visual and listening acceptance; physical controller checks unperformed. |

### HPR-6-2 — Fix Ring Difficulty

| Field | Record |
|---|---|
| Requirement ID | HPR-6-2 |
| Sport/System | Fix Ring Difficulty |
| Original Request | Current ring alignment is excessively difficult. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section6.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section6.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SkyTowCourse delayed spline nominal/ring alignment; ordinary8m vs gold4.8m, matching whole-truck swept clearance; real cable/forces/course preserved. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalSkyGateNormals:10/0; complete four-tow physical course, all finish, ordinary gates25-26/30, no duplicate camera poses/outside centers;720p/1080p images visually inspected, invalid-normal glare and black triangles gone. Native Sky26/26. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human smoothness, difficulty, visual and listening acceptance; physical controller checks unperformed. |

### HPR-6-3 — Replace Ring Assets

| Field | Record |
|---|---|
| Requirement ID | HPR-6-3 |
| Sport/System | Replace Ring Assets |
| Original Request | Find high-quality existing ring models and suitable effects from the project asset collection. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section6.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section6.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Owned segmented/inset gate meshes, chevrons with valid separate front/back/side normals; existing Arctic materials; four pooled Weatherade-based snow-chip effects. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalSkyGateNormals:10/0; complete four-tow physical course, all finish, ordinary gates25-26/30, no duplicate camera poses/outside centers;720p/1080p images visually inspected, invalid-normal glare and black triangles gone. Native Sky26/26. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human smoothness, difficulty, visual and listening acceptance; physical controller checks unperformed. |

### HPR-6-4 — Fix Boat Turns and Ring Alignment

| Field | Record |
|---|---|
| Requirement ID | HPR-6-4 |
| Sport/System | Fix Boat Turns and Ring Alignment |
| Original Request | The towing boat currently makes turns that throw the airborne truck far outside the intended ring path. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section6.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section6.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | SkyTowCourse delayed nominal spline frame follows actual tow lag through bends; SkyTowMatch is sole camera pose owner; existing real boat/cable/forces retained. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalSkyGateNormals:10/0; complete four-tow physical course, all finish, ordinary gates25-26/30, no duplicate camera poses/outside centers;720p/1080p images visually inspected, invalid-normal glare and black triangles gone. Native Sky26/26. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human smoothness, difficulty, visual and listening acceptance; physical controller checks unperformed. |

### HPR-7-1 — Correct Vehicle and Trailer Scale

| Field | Record |
|---|---|
| Requirement ID | HPR-7-1 |
| Sport/System | Correct Vehicle and Trailer Scale |
| Original Request | The truck and trailer dimensions currently look inconsistent. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section7.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section7.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-7-2 — Use Proper Hitch Attachment

| Field | Record |
|---|---|
| Requirement ID | HPR-7-2 |
| Sport/System | Use Proper Hitch Attachment |
| Original Request | Reuse the existing trailer hookup and detachment infrastructure from Interstate: Hauler. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section7.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section7.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-7-3 — Enlarge the Hole

| Field | Record |
|---|---|
| Requirement ID | HPR-7-3 |
| Sport/System | Enlarge the Hole |
| Original Request | The sinkhole is currently too small to receive or be sealed by the trailer. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section7.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section7.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-7-4 — Preserve Working Mechanics

| Field | Record |
|---|---|
| Requirement ID | HPR-7-4 |
| Sport/System | Preserve Working Mechanics |
| Original Request | Keep: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section7.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section7.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-8-1 — Objective Direction Arrow

| Field | Record |
|---|---|
| Requirement ID | HPR-8-1 |
| Sport/System | Objective Direction Arrow |
| Original Request | Add a large, clearly readable directional arrow showing the player's current objective. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section8.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section8.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FareFightNavigation and FareFightRepairAuthoring: four owned CNP markers, valid1-4vendor groups,48pxicons and goal/state updates; existing GPS route/minimap preserved. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalFareCabViews:30/0; four software-human input sources and isolated cab/chase/CNP views, actual original jointed Taxi actor bumper impact8.1786m/s with no delivery points, reconnect/rematch/menu;720p/1080p inspected, fourth cab clear and duplicate metric POI distance removed. Native fare-sky-mine PlayMode5/5. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human driving/impact feel/listening acceptance, physical controllers and extended dense-crowd soak unperformed. |

### HPR-8-2 — Restore Truck Taxi Pedestrian Physics

| Field | Record |
|---|---|
| Requirement ID | HPR-8-2 |
| Sport/System | Restore Truck Taxi Pedestrian Physics |
| Original Request | Pedestrians currently behave like indestructible obstacles. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section8.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section8.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FareFightContact delegates actual pre-solver bumper collisions to existing Taxi TryStrike/MarkHitEventSent; existing positional sound pool; no passenger points. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalFareCabViews:30/0; four software-human input sources and isolated cab/chase/CNP views, actual original jointed Taxi actor bumper impact8.1786m/s with no delivery points, reconnect/rematch/menu;720p/1080p inspected, fourth cab clear and duplicate metric POI distance removed. Native fare-sky-mine PlayMode5/5. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human driving/impact feel/listening acceptance, physical controllers and extended dense-crowd soak unperformed. |

### HPR-8-3 — Fix Dashboard Clipping

| Field | Record |
|---|---|
| Requirement ID | HPR-8-3 |
| Sport/System | Fix Dashboard Clipping |
| Original Request | Part of the dashboard visually escapes or glitches outside the truck during high-speed driving. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section8.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section8.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FareFightDriver mode-owned hidden-avatar layer adapter; original Taxi avatar29 collided with Sports cockpit4layer29. Only hidden avatar moved to25and excluded from these cameras. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalFareCabViews:30/0; four software-human input sources and isolated cab/chase/CNP views, actual original jointed Taxi actor bumper impact8.1786m/s with no delivery points, reconnect/rematch/menu;720p/1080p inspected, fourth cab clear and duplicate metric POI distance removed. Native fare-sky-mine PlayMode5/5. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human driving/impact feel/listening acceptance, physical controllers and extended dense-crowd soak unperformed. |

### HPR-8-4 — Completely Repair AI Objective Pursuit

| Field | Record |
|---|---|
| Requirement ID | HPR-8-4 |
| Sport/System | Completely Repair AI Objective Pursuit |
| Original Request | AI currently drives aimlessly and collides with walls instead of pursuing the current goal. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section8.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section8.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FareFightRoadAccess, FareFightRoute progress-based recovery and match objective states; sampled road join plus authored opposing-edge handling, original shared planner retained. |
| Automated Verification | FareFight22/22native; Windows FareFightFairRoutesFull2 candidate5869909c15/0 complete first-to-three real AI match, actual pickups/thefts/deliveries, rematch and menu. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Default first-to-five full match currently running; severe weather, physical devices and human driving acceptance remain unperformed. FinalFareCabViews30/0 establishes four software-human isolation and contact/arrow integration. |

### HPR-8-5 — Fair Initial Passenger Distances

| Field | Record |
|---|---|
| Requirement ID | HPR-8-5 |
| Sport/System | Fair Initial Passenger Distances |
| Original Request | NEW REQUIREMENT: All competing trucks must begin at approximately equal driving distance from the first passenger. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section8.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section8.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | FareFightStartingGrid; actual initial footprint clearance; comparable sampled road distances with rotated slot assignment; traffic/population and hazards avoid countdown truck poses. |
| Automated Verification | Native every20authored pickup supports four separated fair starts; actual Windows full match starts at250.01m road distance for all four competitors, initialized truck bounds do not overlap. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Default first-to-five full match currently running; severe weather, physical devices and human driving acceptance remain unperformed. FinalFareCabViews30/0 establishes four software-human isolation and contact/arrow integration. |

### HPR-8-6 — Preserve Existing Gameplay

| Field | Record |
|---|---|
| Requirement ID | HPR-8-6 |
| Sport/System | Preserve Existing Gameplay |
| Original Request | Passenger pickup, delivery, and stealing already work and must remain intact. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section8.6. |
| Acceptance Criteria | All observable requirements and acceptance in source section8.6; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-9-1 — Complete HUD Modernization

| Field | Record |
|---|---|
| Requirement ID | HPR-9-1 |
| Sport/System | Complete HUD Modernization |
| Original Request | Replace the existing poor interface with polished player information. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section9.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section9.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-9-2 — Fix Eliminated AI Cameras

| Field | Record |
|---|---|
| Requirement ID | HPR-9-2 |
| Sport/System | Fix Eliminated AI Cameras |
| Original Request | AI cameras behave wildly after their trucks are eliminated. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section9.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section9.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-9-3 — Faster Acceleration and Better Turning

| Field | Record |
|---|---|
| Requirement ID | HPR-9-3 |
| Sport/System | Faster Acceleration and Better Turning |
| Original Request | Apply mode-specific improvements to: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section9.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section9.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-9-4 — Populate the Town

| Field | Record |
|---|---|
| Requirement ID | HPR-9-4 |
| Sport/System | Populate the Town |
| Original Request | Add more: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section9.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section9.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-9-5 — Respawning Trailer Pickups

| Field | Record |
|---|---|
| Requirement ID | HPR-9-5 |
| Sport/System | Respawning Trailer Pickups |
| Original Request | Keep new pickups spawning throughout the round. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section9.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section9.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-9-6 — Nearest-Pickup Arrow

| Field | Record |
|---|---|
| Requirement ID | HPR-9-6 |
| Sport/System | Nearest-Pickup Arrow |
| Original Request | Add a clear directional indicator toward the nearest reachable trailer pickup. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section9.6. |
| Acceptance Criteria | All observable requirements and acceptance in source section9.6; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-10-1 — Minimal Polished HUD

| Field | Record |
|---|---|
| Requirement ID | HPR-10-1 |
| Sport/System | Minimal Polished HUD |
| Original Request | Normal gameplay should show only: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section10.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section10.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | MinefieldMatch live typed score cards/minimal view text; existing SportsExpansionViewHud gains optional Rally roster/minimal portrait mode, assigned by targeted MinefieldPlaytestRepairAuthoring. Empty Mine details no longer turn into READY. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalMineProgressHUD:52/0; actual grounded pre-blast progress, once-only nonzero last-valid-ground outside finalization, supported rest/backward slide/secondary blasts, rematch/menu.720p/1080p compact portrait+distance HUD inspected. Native final-camera-mine-gate EditMode11/11. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human boundary/gameplay acceptance and repeated full normal matches; mountain/environment requirement remains separate and open. |

### HPR-10-2 — Correct Cumulative Distance

| Field | Record |
|---|---|
| Requirement ID | HPR-10-2 |
| Sport/System | Correct Cumulative Distance |
| Original Request | NEW CRITICAL CORRECTION: Distance currently appears to begin counting only after an explosion. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section10.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section10.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | MinefieldRacer.CurrentDistance and MinefieldMatch use live starting-line progression before explosions; authoritative final-rest Distance remains separate. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalMineProgressHUD:52/0; actual grounded pre-blast progress, once-only nonzero last-valid-ground outside finalization, supported rest/backward slide/secondary blasts, rematch/menu.720p/1080p compact portrait+distance HUD inspected. Native final-camera-mine-gate EditMode11/11. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human boundary/gameplay acceptance and repeated full normal matches; mountain/environment requirement remains separate and open. |

### HPR-10-3 — Out-of-Bounds Distance

| Field | Record |
|---|---|
| Requirement ID | HPR-10-3 |
| Sport/System | Out-of-Bounds Distance |
| Original Request | If the truck falls beyond the map, finalize the score at the last valid grounded course position. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section10.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section10.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | MinefieldRacer records actual NWH-grounded/static-supported valid position; falling beyond terrain finalizes once at that position. No last-blast teleport, arbitrary falling distance or zeroing. Normal backward rest scoring preserved. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalMineProgressHUD:52/0; actual grounded pre-blast progress, once-only nonzero last-valid-ground outside finalization, supported rest/backward slide/secondary blasts, rematch/menu.720p/1080p compact portrait+distance HUD inspected. Native final-camera-mine-gate EditMode11/11. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Human boundary/gameplay acceptance and repeated full normal matches; mountain/environment requirement remains separate and open. |

### HPR-10-4 — Map Width and Side Mountains

| Field | Record |
|---|---|
| Requirement ID | HPR-10-4 |
| Sport/System | Map Width and Side Mountains |
| Original Request | Ensure the earlier physical map improvements exist: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section10.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section10.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-10-5 — Preserve Gameplay

| Field | Record |
|---|---|
| Requirement ID | HPR-10-5 |
| Sport/System | Preserve Gameplay |
| Original Request | Minefield is otherwise satisfactory. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section10.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section10.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-11 — TRUCK BOWLING

| Field | Record |
|---|---|
| Requirement ID | HPR-11 |
| Sport/System | TRUCK BOWLING |
| Original Request |  LATEST INSTRUCTION OVERRIDES EARLIER BOWLING REQUESTS Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section11. |
| Acceptance Criteria | All observable requirements and acceptance in source section11; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-12 — TRUCK BILLIARDS

| Field | Record |
|---|---|
| Requirement ID | HPR-12 |
| Sport/System | TRUCK BILLIARDS |
| Original Request | Perform a full polished HUD pass. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section12. |
| Acceptance Criteria | All observable requirements and acceptance in source section12; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-13-1 — CRITICAL: FIX BROKEN GOAL DETECTION FIRST

| Field | Record |
|---|---|
| Requirement ID | HPR-13-1 |
| Sport/System | CRITICAL: FIX BROKEN GOAL DETECTION FIRST |
| Original Request | NEW HIGH-PRIORITY REGRESSION: The puck can enter the goal without registering a goal. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section13.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section13.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-13-2 — Complete UI Modernization

| Field | Record |
|---|---|
| Requirement ID | HPR-13-2 |
| Sport/System | Complete UI Modernization |
| Original Request | After goal detection is repaired, overhaul: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section13.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section13.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-14 — TRUCK AIR HOCKEY

| Field | Record |
|---|---|
| Requirement ID | HPR-14 |
| Sport/System | TRUCK AIR HOCKEY |
| Original Request | Perform a substantial release-quality menu and HUD redesign. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section14. |
| Acceptance Criteria | All observable requirements and acceptance in source section14; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-15 — SCRAP HEAP

| Field | Record |
|---|---|
| Requirement ID | HPR-15 |
| Sport/System | SCRAP HEAP |
| Original Request | Move these two sports into the project's existing Scrap Heap category: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section15. |
| Acceptance Criteria | All observable requirements and acceptance in source section15; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-16-1 — Preserve the Excellent Core Game

| Field | Record |
|---|---|
| Requirement ID | HPR-16-1 |
| Sport/System | Preserve the Excellent Core Game |
| Original Request | The game itself is excellent. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section16.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section16.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-16-2 — PS2 World Series of Poker-Inspired UI

| Field | Record |
|---|---|
| Requirement ID | HPR-16-2 |
| Sport/System | PS2 World Series of Poker-Inspired UI |
| Original Request | Research visual references from the PlayStation 2-era World Series of Poker games, particularly World Series of Poker and World Series of Poker: Tournament of Champions. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section16.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section16.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-16-3 — Animate AI Trucks

| Field | Record |
|---|---|
| Requirement ID | HPR-16-3 |
| Sport/System | Animate AI Trucks |
| Original Request | The AI trucks are completely stiff during poker. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section16.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section16.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-16-4 — Preserve Previous Poker Presentation Features

| Field | Record |
|---|---|
| Requirement ID | HPR-16-4 |
| Sport/System | Preserve Previous Poker Presentation Features |
| Original Request | Maintain: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section16.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section16.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-17-1 — Replace Placeholder Explosion VFX

| Field | Record |
|---|---|
| Requirement ID | HPR-17-1 |
| Sport/System | Replace Placeholder Explosion VFX |
| Original Request | Search existing owned assets for suitable explosion effects. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section17.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section17.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-17-2 — Complete HUD Overhaul

| Field | Record |
|---|---|
| Requirement ID | HPR-17-2 |
| Sport/System | Complete HUD Overhaul |
| Original Request | Modernize: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section17.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section17.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-17-3 — Use Owned Animated Cat Models

| Field | Record |
|---|---|
| Requirement ID | HPR-17-3 |
| Sport/System | Use Owned Animated Cat Models |
| Original Request | NEW REQUIREMENT: The developer owns animated cat models. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section17.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section17.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-17-4 — Preserve Racing Gameplay

| Field | Record |
|---|---|
| Requirement ID | HPR-17-4 |
| Sport/System | Preserve Racing Gameplay |
| Original Request | The underlying Cheese Rolling gameplay is good. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section17.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section17.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-18-1 — Denser, More Varied Crowds

| Field | Record |
|---|---|
| Requirement ID | HPR-18-1 |
| Sport/System | Denser, More Varied Crowds |
| Original Request | Populate the stands substantially more densely. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section18.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section18.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-18-2 — Enclose and Complete the Pool

| Field | Record |
|---|---|
| Requirement ID | HPR-18-2 |
| Sport/System | Enclose and Complete the Pool |
| Original Request | The pool has visible unfinished areas, particularly toward the back. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section18.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section18.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-18-3 — Replace Placeholder Run-Up Art

| Field | Record |
|---|---|
| Requirement ID | HPR-18-3 |
| Sport/System | Replace Placeholder Run-Up Art |
| Original Request | The approach section should look like a finished sporting venue. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section18.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section18.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-18-4 — Animated Water and Splashes

| Field | Record |
|---|---|
| Requirement ID | HPR-18-4 |
| Sport/System | Animated Water and Splashes |
| Original Request | Find and reuse appropriate existing water assets. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section18.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section18.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-18-5 — Stabilize Airborne Speech Bubbles

| Field | Record |
|---|---|
| Requirement ID | HPR-18-5 |
| Sport/System | Stabilize Airborne Speech Bubbles |
| Original Request | NEW REQUIREMENT: The speech/text bubbles shake excessively inside their small display area. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section18.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section18.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-19-1 — Triple the Playable Arena Area

| Field | Record |
|---|---|
| Requirement ID | HPR-19-1 |
| Sport/System | Triple the Playable Arena Area |
| Original Request | Increase the actual physical playable surface area to approximately 3× the current baseline, not simply the camera zoom. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section19.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section19.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-19-2 — Reduce Obstacle Density to Approximately One-Third

| Field | Record |
|---|---|
| Requirement ID | HPR-19-2 |
| Sport/System | Reduce Obstacle Density to Approximately One-Third |
| Original Request | Three times less dense means aim for approximately one-third the prior obstacle density per unit of playable area. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section19.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section19.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-19-3 — MAKE ALL GAMEPLAY OBSTACLES DESTRUCTIBLE

| Field | Record |
|---|---|
| Requirement ID | HPR-19-3 |
| Sport/System | MAKE ALL GAMEPLAY OBSTACLES DESTRUCTIBLE |
| Original Request | NEW REQUIREMENT: Every obstacle placed within the active arena should be capable of reacting to and being destroyed by sufficient physical contact. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section19.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section19.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-19-4 — Preserve Existing Keepaway Mechanics

| Field | Record |
|---|---|
| Requirement ID | HPR-19-4 |
| Sport/System | Preserve Existing Keepaway Mechanics |
| Original Request | Maintain: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section19.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section19.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-20-1 — Reduce Wind Intensity by Approximately 30%

| Field | Record |
|---|---|
| Requirement ID | HPR-20-1 |
| Sport/System | Reduce Wind Intensity by Approximately 30% |
| Original Request | Apply a mode-specific weather wind-force multiplier of approximately: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section20.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section20.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-20-2 — Do Not Rework Racing Physics

| Field | Record |
|---|---|
| Requirement ID | HPR-20-2 |
| Sport/System | Do Not Rework Racing Physics |
| Original Request | Do not unnecessarily adjust: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section20.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section20.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-20-3 — Replace the HUD with BASE Jumping-Style Compact Score Presentation

| Field | Record |
|---|---|
| Requirement ID | HPR-20-3 |
| Sport/System | Replace the HUD with BASE Jumping-Style Compact Score Presentation |
| Original Request | Remove the oversized repeated scoreboard/status layout. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section20.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section20.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-21-1 — Add Smoke to Flares

| Field | Record |
|---|---|
| Requirement ID | HPR-21-1 |
| Sport/System | Add Smoke to Flares |
| Original Request | The existing flares need visible smoke. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section21.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section21.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-21-2 — REMOVE THE MASSIVE TOP HUD BAR

| Field | Record |
|---|---|
| Requirement ID | HPR-21-2 |
| Sport/System | REMOVE THE MASSIVE TOP HUD BAR |
| Original Request | The large top scoreboard/status bar must be removed completely. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section21.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section21.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-21-3 — ONE CENTRAL SPLIT-SCREEN SCOREBOARD

| Field | Record |
|---|---|
| Requirement ID | HPR-21-3 |
| Sport/System | ONE CENTRAL SPLIT-SCREEN SCOREBOARD |
| Original Request | For four-player split-screen: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section21.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section21.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-22-1 — NO AUTOMATIC FLIP RESET

| Field | Record |
|---|---|
| Requirement ID | HPR-22-1 |
| Sport/System | NO AUTOMATIC FLIP RESET |
| Original Request | NEW REQUIREMENT: When a player flips their truck, do not automatically reset it. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section22.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section22.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-22-2 — Use the BASE Jumping HUD Layout

| Field | Record |
|---|---|
| Requirement ID | HPR-22-2 |
| Sport/System | Use the BASE Jumping HUD Layout |
| Original Request | Apply the same compact central split-screen presentation. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section22.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section22.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-22-3 — Preserve Existing Mattress Systems

| Field | Record |
|---|---|
| Requirement ID | HPR-22-3 |
| Sport/System | Preserve Existing Mattress Systems |
| Original Request | Keep: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section22.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section22.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-23-1 — Apply the Latest Minimal Racing HUD

| Field | Record |
|---|---|
| Requirement ID | HPR-23-1 |
| Sport/System | Apply the Latest Minimal Racing HUD |
| Original Request | The mode has been referred to as Wife Carrying and Wife Racing in the notes. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section23.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section23.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-23-2 — BASE Jumping-Style Split-Screen Layout

| Field | Record |
|---|---|
| Requirement ID | HPR-23-2 |
| Sport/System | BASE Jumping-Style Split-Screen Layout |
| Original Request | Use the same compact central split-screen information arrangement as Mattress Racing. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section23.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section23.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-24-1 — Preserve Working Gameplay

| Field | Record |
|---|---|
| Requirement ID | HPR-24-1 |
| Sport/System | Preserve Working Gameplay |
| Original Request | The developer likes the current game mechanics. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section24.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section24.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-24-2 — Improve Parking Bay Presentation

| Field | Record |
|---|---|
| Requirement ID | HPR-24-2 |
| Sport/System | Improve Parking Bay Presentation |
| Original Request | Replace default-looking markers and plain text with attractive: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section24.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section24.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-24-3 — Remove the Obstructive Top Bar

| Field | Record |
|---|---|
| Requirement ID | HPR-24-3 |
| Sport/System | Remove the Obstructive Top Bar |
| Original Request | Remove the unnecessary shared-view top information bar. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section24.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section24.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-24-4 — Redesign the Bottom UI

| Field | Record |
|---|---|
| Requirement ID | HPR-24-4 |
| Sport/System | Redesign the Bottom UI |
| Original Request | The bottom strip should no longer consume the entire screen width. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section24.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section24.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-24-5 — NEW: Shared-View Leaderboard Must Exist in Split-Screen

| Field | Record |
|---|---|
| Requirement ID | HPR-24-5 |
| Sport/System | NEW: Shared-View Leaderboard Must Exist in Split-Screen |
| Original Request | The polished shared-view leaderboard currently disappears or is not properly represented in split-screen. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section24.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section24.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-25 — TRUCK TARGET LAUNCH

| Field | Record |
|---|---|
| Requirement ID | HPR-25 |
| Sport/System | TRUCK TARGET LAUNCH |
| Original Request |  Use the BASE Jumping Compact Split-Screen HUD Standard Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section25. |
| Acceptance Criteria | All observable requirements and acceptance in source section25; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-26 — TRUCK TUG OF WAR

| Field | Record |
|---|---|
| Requirement ID | HPR-26 |
| Sport/System | TRUCK TUG OF WAR |
| Original Request |  DO NOT IMPLEMENT GAMEPLAY CHANGES YET Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section26. |
| Acceptance Criteria | All observable requirements and acceptance in source section26; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-27-1 — Repair Clipped Damage Percentages

| Field | Record |
|---|---|
| Requirement ID | HPR-27-1 |
| Sport/System | Repair Clipped Damage Percentages |
| Original Request | The HUD currently clips the damage percentages. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section27.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section27.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-27-2 — Remove Redundant Side Text

| Field | Record |
|---|---|
| Requirement ID | HPR-27-2 |
| Sport/System | Remove Redundant Side Text |
| Original Request | Remove the large: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section27.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section27.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-27-3 — Add Prominent Arena Branding

| Field | Record |
|---|---|
| Requirement ID | HPR-27-3 |
| Sport/System | Add Prominent Arena Branding |
| Original Request | Place a large, polished Demolition Derby / Truck Sports: Party! logo inside the actual arena. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section27.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section27.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-27-4 — Preserve Existing Derby Features

| Field | Record |
|---|---|
| Requirement ID | HPR-27-4 |
| Sport/System | Preserve Existing Derby Features |
| Original Request | Do not undo: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section27.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section27.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-28-1 — Correct Sport-Specific Logo

| Field | Record |
|---|---|
| Requirement ID | HPR-28-1 |
| Sport/System | Correct Sport-Specific Logo |
| Original Request | Replace the incorrect or generic displayed logo with the dedicated Truck Dance-Off logo. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section28.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section28.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-28-2 — Make Rhythm Inputs More Forgiving

| Field | Record |
|---|---|
| Requirement ID | HPR-28-2 |
| Sport/System | Make Rhythm Inputs More Forgiving |
| Original Request | Current button timing is too strict. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section28.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section28.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-28-3 — Make Trucks Face the Screen

| Field | Record |
|---|---|
| Requirement ID | HPR-28-3 |
| Sport/System | Make Trucks Face the Screen |
| Original Request | The dancing trucks should be oriented toward the primary gameplay/broadcast camera. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section28.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section28.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-28-4 — Improve Camera Work

| Field | Record |
|---|---|
| Requirement ID | HPR-28-4 |
| Sport/System | Improve Camera Work |
| Original Request | Add more interesting cinematic presentation: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section28.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section28.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-28-5 — Preserve Cute Truck Animations

| Field | Record |
|---|---|
| Requirement ID | HPR-28-5 |
| Sport/System | Preserve Cute Truck Animations |
| Original Request | Keep: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section28.5. |
| Acceptance Criteria | All observable requirements and acceptance in source section28.5; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-29-1 — Four-Player Layout

| Field | Record |
|---|---|
| Requirement ID | HPR-29-1 |
| Sport/System | Four-Player Layout |
| Original Request | For a standard 2×2 split-screen: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section29.1. |
| Acceptance Criteria | All observable requirements and acceptance in source section29.1; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-29-2 — Vary Contents by Sport

| Field | Record |
|---|---|
| Requirement ID | HPR-29-2 |
| Sport/System | Vary Contents by Sport |
| Original Request | BASE Jumping - Name. - Truck icon. - Score. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section29.2. |
| Acceptance Criteria | All observable requirements and acceptance in source section29.2; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-29-3 — Other Player Counts

| Field | Record |
|---|---|
| Requirement ID | HPR-29-3 |
| Sport/System | Other Player Counts |
| Original Request | Adapt layouts for: Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section29.3. |
| Acceptance Criteria | All observable requirements and acceptance in source section29.3; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-29-4 — UI Visual Quality

| Field | Record |
|---|---|
| Requirement ID | HPR-29-4 |
| Sport/System | UI Visual Quality |
| Original Request | Match polished existing Truck Sports games. Full authoritative scope: Requests/2026-10-21-HumanPlaytestRepair.md, section29.4. |
| Acceptance Criteria | All observable requirements and acceptance in source section29.4; relevant complete match, rematch, input and rendered output at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Current scene/scripts to inspect; no new repair claimed. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | Supplied October21 playtest observations take precedence over earlier completion claims. |
| Remaining Work | Inspect, reproduce where applicable, implement missing behavior, run targeted tests and actual Windows visual/gameplay acceptance. |

### HPR-BUNGEE — Bungee Destruction

| Field | Record |
|---|---|
| Requirement ID | HPR-BUNGEE |
| Sport/System | Bungee Destruction |
| Original Request | Master directive P1 item14 and final directive: building replacements and responsiveness; actual building destruction. Preserve established swing controls. |
| Acceptance Criteria | Appropriate owned buildings physically break on valid impacts; responsive existing controls; full attempts, reset and rematch preserve them. |
| Status | PARTIAL |
| Implementation Evidence | Existing Bungee scene/assets to inspect. |
| Automated Verification | Not run for this directive yet. |
| Human Verification | New master directive requests this correction. |
| Remaining Work | Audit current destruction, reproduce and repair without rebuilding working swing controls. |

### GBC sport-by-sport camera acceptance

Each permanent scene ID below tracks the actual registered variant separately. Source directive sections1–14 apply, with protected/precision/stealth exceptions documented. Original issue: shared views can shrink important trucks below readable screen size. Acceptance requires actual renderer projection measurements, legible action/objective, target-loss stability and720p/1080p rendered evidence; no compilation-only verification.

#### GBC-SPORT-TruckBilliards

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBilliards |
| Sport/System | TruckBilliards |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckHockey

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckHockey |
| Sport/System | TruckHockey |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckBowling

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBowling |
| Sport/System | TruckBowling |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Protected by newer master directive: camera/gameplay unchanged; only UI/text cleanup authorized. |

#### GBC-SPORT-TruckAirHockey

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckAirHockey |
| Sport/System | TruckAirHockey |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckWallball

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckWallball |
| Sport/System | TruckWallball |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckCornhole

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckCornhole |
| Sport/System | TruckCornhole |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckCheeseRolling

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckCheeseRolling |
| Sport/System | TruckCheeseRolling |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckDockDiving

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckDockDiving |
| Sport/System | TruckDockDiving |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckKeepaway

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckKeepaway |
| Sport/System | TruckKeepaway |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckKeepawayFirstPerson

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckKeepawayFirstPerson |
| Sport/System | TruckKeepawayFirstPerson |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckCurling

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckCurling |
| Sport/System | TruckCurling |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckBaseJumping

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBaseJumping |
| Sport/System | TruckBaseJumping |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckTerrierRacing

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckTerrierRacing |
| Sport/System | TruckTerrierRacing |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckMinefield

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckMinefield |
| Sport/System | TruckMinefield |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckMattressRacing

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckMattressRacing |
| Sport/System | TruckMattressRacing |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckWifeCarrying

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckWifeCarrying |
| Sport/System | TruckWifeCarrying |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckMusicalParking

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckMusicalParking |
| Sport/System | TruckMusicalParking |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckGiantSwing

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckGiantSwing |
| Sport/System | TruckGiantSwing |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckBuzkashi

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBuzkashi |
| Sport/System | TruckBuzkashi |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckBogSnorkelling

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBogSnorkelling |
| Sport/System | TruckBogSnorkelling |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckBungeeDemolition

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBungeeDemolition |
| Sport/System | TruckBungeeDemolition |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckFishing

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckFishing |
| Sport/System | TruckFishing |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckSumo

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckSumo |
| Sport/System | TruckSumo |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckTargetLaunch

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckTargetLaunch |
| Sport/System | TruckTargetLaunch |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckFieldGoal

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckFieldGoal |
| Sport/System | TruckFieldGoal |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckBeerPong

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBeerPong |
| Sport/System | TruckBeerPong |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckPoker

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckPoker |
| Sport/System | TruckPoker |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckTugOfWar

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckTugOfWar |
| Sport/System | TruckTugOfWar |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Newer directive prohibits changes in this pass; retain future design discussion. |

#### GBC-SPORT-TruckSkeeBall

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckSkeeBall |
| Sport/System | TruckSkeeBall |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckLogRolling

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckLogRolling |
| Sport/System | TruckLogRolling |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckTrampolineDodgeball

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckTrampolineDodgeball |
| Sport/System | TruckTrampolineDodgeball |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckDemolitionDerby

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckDemolitionDerby |
| Sport/System | TruckDemolitionDerby |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckPedestrianPlowing

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckPedestrianPlowing |
| Sport/System | TruckPedestrianPlowing |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckPedestrianPlowingFirstPerson

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckPedestrianPlowingFirstPerson |
| Sport/System | TruckPedestrianPlowingFirstPerson |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckKickball

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckKickball |
| Sport/System | TruckKickball |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckDanceOff

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckDanceOff |
| Sport/System | TruckDanceOff |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckLimbo

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckLimbo |
| Sport/System | TruckLimbo |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckSkyTow

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckSkyTow |
| Sport/System | TruckSkyTow |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckTrailerPlug

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckTrailerPlug |
| Sport/System | TruckTrailerPlug |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Inspect actual scene rendering, apply shared framing if appropriate, measure, validate relevant controls and record evidence. |

#### GBC-SPORT-TruckFareFight

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckFareFight |
| Sport/System | TruckFareFight |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckSnake

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckSnake |
| Sport/System | TruckSnake |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckRedLightGreenLight

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckRedLightGreenLight |
| Sport/System | TruckRedLightGreenLight |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckHotPotato

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckHotPotato |
| Sport/System | TruckHotPotato |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckBalloonBattle

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckBalloonBattle |
| Sport/System | TruckBalloonBattle |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckWhackAMole

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckWhackAMole |
| Sport/System | TruckWhackAMole |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckKingOfTheHill

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckKingOfTheHill |
| Sport/System | TruckKingOfTheHill |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckSeesaw

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckSeesaw |
| Sport/System | TruckSeesaw |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckFloorIsLava

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckFloorIsLava |
| Sport/System | TruckFloorIsLava |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckTetherball

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckTetherball |
| Sport/System | TruckTetherball |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckJousting

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckJousting |
| Sport/System | TruckJousting |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckMagnetMayhem

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckMagnetMayhem |
| Sport/System | TruckMagnetMayhem |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckVolleyball

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckVolleyball |
| Sport/System | TruckVolleyball |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckFreezeTag

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckFreezeTag |
| Sport/System | TruckFreezeTag |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckHungryHaulers

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckHungryHaulers |
| Sport/System | TruckHungryHaulers |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckSheepHerding

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckSheepHerding |
| Sport/System | TruckSheepHerding |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckHideAndSeek

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckHideAndSeek |
| Sport/System | TruckHideAndSeek |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | PARTIAL |
| Implementation Evidence | Saved scene and existing camera path under investigation. Common runtime: SportsBroadcastFraming/SportsEventCamera/SportsCameraFit; explicit sport-owned selectors where applicable. |
| Automated Verification | New projection math6/6passed; not yet per-scene acceptance. First Windows Kickball/Magnet reproduction recorded under Logs/TruckSports/PlaytestRepair/Quick; overshoot found and repair in progress. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Stealth/privacy exception: never expose hidden players or add hidden-player POIs. Audit existing legal seeker/spectator view only. |

#### GBC-SPORT-TruckFrogger

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckFrogger |
| Sport/System | TruckFrogger |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckAvalancheEscape

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckAvalancheEscape |
| Sport/System | TruckAvalancheEscape |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |

#### GBC-SPORT-TruckNinjaWarrior

| Field | Record |
|---|---|
| Requirement ID | GBC-SPORT-TruckNinjaWarrior |
| Sport/System | TruckNinjaWarrior |
| Original Request | Audit and correct unreadable shared/broadcast framing without altering cab, precision or protected gameplay. |
| Acceptance Criteria | Relevant action readable; actual projected renderer size; objective visible or legitimate offscreen cue; bounded smooth shot; valid targets and viewport ownership at720p/1080p. |
| Status | IMPLEMENTED / UNVERIFIED |
| Implementation Evidence | Saved scene binding and shared SportsBroadcastFraming/Profile/Guidance; protected precision/cab views retain their own camera authority. |
| Automated Verification | Exact Windows build c8b0b9c1aed9. Logs/TruckSports/PlaytestRepair/FinalCamera33:693/0 across33bound scenes; actual720p/1080p projected vehicle size, objective/zoom bounds, all-human cues,1-4software-human ownership, AI-only rematch and spectator retirement. This is targeted camera/rendering acceptance, not complete human matches for all scenes. |
| Human Verification | User reports numerous shared views far too distant. No human approval of new framing yet. |
| Remaining Work | Human readability/smoothness acceptance and physical controllers unperformed; unrelated gameplay requirements remain separate. |
