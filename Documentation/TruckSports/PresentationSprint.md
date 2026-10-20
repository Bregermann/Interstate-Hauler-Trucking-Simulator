# October 20 presentation sprint

Active requirements are in [the permanent acceptance ledger](../../TRUCK_SPORTS_ACCEPTANCE_LEDGER.md). The old queue is archived; this assignment covers Dock Diving, Poker, original sport logos and menus.

## Implemented

- Forty-one original transparent PNG sport logos, revised toward 1997–2002 console mascot/party-game art: irregular chunky lettering, broad painted bevels, thick dark outlines, cream keylines and faceted cartoon trucks. Prompts and selected-image provenance are retained in `Tools/TruckSports/ArtworkSources`.
- Existing sport selection now has a two-column paginated logo grid and a larger selected-sport showcase with gameplay preview, description and actual participant capacity. Heat actions, mode choices and device configuration are preserved. DOTween fades the showcase. Spatial focus and a Your Crew shortcut improve browsing; Back returns Crew to its originating page.
- Logos are saved into all 41 sport scenes' setup/info headers and suitable existing venue signs. Additive authoring upgrades preserve existing courses. Ordinary builds consume saved assets and never regenerate artwork or scenes.
- Dock bubbles show the airborne participant's name/color, preserve their launch token and timing across pause, stay within the camera's viewport, and reset explicitly on rematch. Presentation randomness is independent of gameplay randomness.
- Dock launch/splash sounds use a bounded positional pool with the existing shared clips/settings. A single delayed audience reaction replaces competing immediate reactions. Reset/pause cancels pending reactions. No new spoken Canadian recordings were created.
- Dock's park perimeter uses installed trees, rock gardens, walkway lights and a clubhouse, with a tiled lawn material. Dressing stays outside the established launch/pool envelope; imported decorative colliders are removed.
- Poker's recognizable pink truck dealer now drives continuously on the tabletop, with rolling/steering wheels. At each active seat she turns toward the participant, gives a short shimmy and positioned giggle, and hands over two face-down cards. Normal/quick pacing, pause, idempotent skip and reset remain safe. Authoritative Hold'em hands, bets, pots and chips remain owned by the existing table, separate from choreography.
- Four restaurant dining tables, eight animated UTS guests, meals and service ovens dress the existing room outside the table area. Three original nonverbal giggle clips use the installed local Chatterbox Turbo engine, cached pinned weights and default conditioning, with no reference voice/cloning. Prompts, seeds and model hashes are retained in `Assets/TruckPoker/Audio/Dealer giggle provenance.json` and `Tools/TruckSports/GeneratePokerGiggle.py`. Subjective listening or approval of the desired sultry tone is not claimed.

## Poker black/white rendering: actual cause and fix

The user's actual Windows report was reproduced. Controlled Editor captures had failed to expose it. The generic fitting step stretched almost-planar architectural modules along an effectively zero-thickness axis: the saved floor had local scale245518.3 on that axis. Those extreme transforms, static-batched structural rendering and noisy industrial normal maps produced the broken presentation.

The sport-owned correction bounds extreme thickness scales while preserving world centers, removes structural static batching, clears inappropriate normal maps and uses warm two-sided interior material variants. Original intended lighting and grade remain; experimental unlit/postprocessing-disabled overrides were not retained. No vendor mesh, shader or source was changed. Final actual Windows1080p restaurant and four-player cab frames render in full color. No post-fix human confirmation has yet been supplied.

Relevant Poker files: `Assets/TruckPoker/Scenes/TruckPoker.unity`, `Scripts/PokerMobileDealer.cs`, `PokerMatch.cs`, `PokerDealerProbe.cs`, `PokerFocusedProbe.cs`, `Editor/PokerRestaurantDressing.cs`, `PokerRestaurantSurfaceAuthoring.cs`, `PokerTabletopDealerAuthoring.cs`, `PokerMobileDealerAuthoring.cs`, and owned Materials/Audio assets. Existing poker rules were preserved.

Relevant Dock files: `Assets/TruckDockDiving/Scenes/TruckDockDiving.unity`, `Scripts/DockSlang.cs`, `DockMatch.cs`, `DockSlangProbe.cs`, `Editor/DockSlangAuthoring.cs`, `DockVenueDressing.cs` and shared `SportsFeedback.cs`.

Relevant menu files: `Assets/TruckSports/Scenes/TruckSports.unity`, `SportsCatalogueHero.cs`, `SportsCatalogueChoice.cs`, `SportsFrontEnd.cs`, `SportsMenuView.cs`, `SportsCardMotion.cs`, `SportsParticipantsMenu.cs`, `SportsMenuPresentationProbe.cs`, `SportsWindowCapture.cs`, `SportsRetroMenuAuthoring.cs`, `SportsSportLogoAuthoring.cs`, `SportsMenuArtAuthoring.cs` and additive branding in all41sport scenes. Initial scene delta audit found no removed objects or changes to existing native physics components.

## Actual final verification

Native tests and actual Windows-player tests are distinguished below. These are software checks and agent-inspected renders, not human approval:

| Run | Actual result |
|---|---|
| Presentation EditMode | 2/2 pass: registered logo references/import settings and viewport-local Dock bubble geometry |
| Dock PlayMode regression | 1/1 pass: actual launch ownership, pause idempotence and immediate rematch |
| Revised tabletop Poker PlayMode | 1/1 pass: eight animated diners, safe continuous tabletop circuit,16handoffs,8shake/giggle events and no furniture overlap |
| Final Windows Dock | 16 checks / 0 errors; eight competitors,24physical attempts, pause, bubbles, real water contacts, positional sound/mute/reset, rematch and menu cleanup |
| Final Windows Poker | 25 checks / 0 errors; eight-seat deal, pause/skip, hidden-card invariants and three complete four-AI tournaments across all difficulties; each conserves800chips |
| Final Windows software input/privacy | 0 errors; four injected human sources plus fourAI, private card geometry, shared-stick ownership, free look, reconnect, hot seat and viewport bounds |
| Final Windows catalogue | 273 checks / 0 errors;41scene launches/returns, artwork/previews, actual saved Crew reload, injected keyboard/pad focus, matching page showcase and display fixtures |

All four final Windows processes exited0 on the same managed hash. Actual composed-framebuffer images were inspected at720p/1080p/1440p/2560x1080 as appropriate, including four-player Poker. Full-size logo art and allfive menu pages were inspected at their intended menu sizes. Reference checks initially missed a paging defect: Back retained focus and the previous showcase remained. The final correction focuses the first visible sport and updates its matching hero; allfive page assertions now pass.

Untouched Windows screenshots, exact process receipts, result text and native XML: [PresentationEvidence](PresentationEvidence/README.md). Full logs and historical failing reproductions remain in `Logs/TruckSports/PresentationSprint` locally. The first revised Poker PlayMode attempt using `-nographics` failed a realtime ReflectionProbe render-texture allocation; the proper graphics/D3D11 rerun passed. That earlier failure is retained, not counted as success.

## Build and precise verification limits

Canonical output: `Builds/TruckSports/TruckSports.exe` and all companion files. Windows x64 Development/Mono/D3D11, Unity6000.4.10f1;42explicit Sports-only scenes, startupTruckSports. Built2026-10-20T07:11:54.067510Z;0errors/57warnings. Deliberate failure-restoration and final host-settings restoration passed. No extra numbered build is delivered.

Managed SHA256: `e98b590107681386b1f0fe0e87dc196719cbf44ef146674c019a656558bb307c`. Runtime manifest SHA256: `e67c5ab15980c4c1e66753b87760fd399086bbf2dfab3f7a02679d74ddf8d2ed` (504 executable companions; documentation/debug-only folders excluded). Actual publication receipts are recorded in the delivered GIT-DELIVERY.txt after push.

- Subjective listening, mix/cadence and desired giggle tone remain unverified (DD-002,PK-006).
- Physical controllers/wheels, human multiplayer/reconnect and usability remain unverified (DD-004,PK-005,MN-004).
- Dock/Poker menu previews were refreshed from the current saved scene at runtime; provenance correctly labels them controlled Editor captures, distinct from final Windows evidence. The other39genuine baseline previews, including repaired Kickball, remain. They were reference/load checked but not all freshly recaptured or individually compared against current full gameplay in this focused sprint (MN-002).
- Human approval of revised logos, restaurant/menu appearance and gameplay is separate from software success.

These are explicit acceptance limits, not invented passes. The historical full-collection backlog was archived under the newer user directive. Sixty permanent requirement IDs preserve the current sprint, including the user's tabletop dealer request PK-006 and reopened rendering defect PK-002.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** Heat/uGUI, DOTween, NWH Vehicle Physics 2, UTS character/animation assets, Modern Restaurant modules/materials, Toon Gas Station scenery, JC Low Poly Mega City scenery, Weather Maker water normal, existing Sports artwork/preview/import systems, installed local Chatterbox Turbo engine and cached weights.
- **VENDOR ASSETS USED:** Existing Heat controls; DOTween transitions/dealer animation; NWH trucks/physics and grass texture; UTS spectators/dining animation; Modern Restaurant tables/chairs/food/ovens and structural albedo; Toon Gas Station trees/rocks/lamps; JC clubhouse; existing water and Sports feedback systems; local Chatterbox nonverbal generation.
- **RELEVANT ASSETS NOT USED:** No new traffic, GPS, terrain-authoring or streaming system was needed for this focused presentation assignment. Existing weather remains under its established authority. No external stock branding substituted for original logos.
- **CUSTOM SYSTEMS CREATED:** Original bitmap logos and prompt records; Sports catalogue presentation components; additive logo/menu/restaurant/park authoring adapters; targeted probes and tests; permanent requirement ledger.
- **VENDOR SOURCE MODIFIED:** None.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** None. Match rules remain with existing authorities; animation remains DOTween, vehicle physics NWH and production controls Heat/uGUI.

## Publication

Implementation commit `c67ad99c1f4b0dc45eef3edf35124726ac31e980` was pushed normally and atomically to both approved public branches (`main`, `codex/truck-sports-seven-events`) and read back successfully. Local preservation: `dd3de2efd4ff312c63289886aed723f4b78d6e83`. The301-file scope excludes unrelated changes, adds no vendor source, and excludes private Truck Taxi ancestry. All912protected vendor/settings hashes remain unchanged. See [publication receipt](PresentationEvidence/publication.json) and the canonical GIT-DELIVERY.txt for the documentation-only follow-up commit.
