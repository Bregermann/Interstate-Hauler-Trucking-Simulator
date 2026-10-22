# Poker table presentation and public AI reactions

Addresses HPR-16-1 through HPR-16-4. The original `PokerTable`, hand evaluator, pots, policy, card views, betting handlers and mobile dealer remain authoritative and unchanged. This pass adds a saved, read-only presentation adapter and render-only public-context movement.

## Reference and vendor audit

The inspected [PS2 World Series of Poker gameplay image](https://www.gamesdatabase.org/media/sony-playstation-2/artwork-in-game/2006/world-series-of-poker) organizes attention around the table, with active-player information, community cards, a local hand/stack and pot in compact lower panels. The [Tournament of Champions PS2 packaging](https://gamesdb.launchbox-app.com/games/images/112298-world-series-of-poker-tournament-of-champions) supplies supporting table/player presentation references. The packaging's small game images were viewed; the full Tournament of Champions manual and a complete readable gameplay HUD were not successfully retrieved. No third-party artwork is copied into the project.

The Truck Sports interpretation uses the existing ink/cream/orange palette, crew identity colors, original truck portrait, rounded plates and Heat-derived TMP font. Existing uGUI/TMP card ranks and vector suits, Heat betting controls, source routing and DOTween are retained. The established mobile dealer, restaurant and original logo are preserved.

## Implementation

- Public seat panels show each participant's chip stack, current public action, acting-player underline and dealer/blind badge. They do not inspect private cards or hand strength.
- Broadcast community cards, pot/award total, street and acting player are grouped into a compact table lower panel.
- Each human viewport retains its original private-card container, hold-to-peek lifecycle and seven betting routes. The new owned lower panel and public chip strip scale to that viewport. Inactive betting controls leave no large empty lower panel.
- Safe ambient parent pivots add small suspension movement, cab sway, public-actor/dealer gaze and acting-seat nervous movement for AI only. Original action and award tweens remain on their original child pivots. No collider, Rigidbody, camera or parked participant root moves. Reduced Motion disables the ambient movement. Pause freezes it and setup/rematch resets it.
- Existing generic header/standing overlays are disabled for this saved Poker presentation, leaving one HUD layout authority. The original fields remain available for fallback.

Explicit authoring: **Truck Sports → Poker → Upgrade saved table HUD and AI presence**. Existing designer layout is preserved on subsequent calls; normal builds do not regenerate the scene.

The original AI action path already called `React`, but that animation lasted only a brief action/award interval; there was no ambient public gaze or movement between actions or while waiting for the dealer. The new independent parent pivots fill that gap without competing with the original child tweens or human cab-look transforms.

## Validation

Saved-scene EditMode tests: **3 passed, 0 failed**, checking original private-card isolation, betting routes, dealer references and render-only pivot hierarchy.

The first new PlayMode fixture failed because it tried to load a scene using the player build-list API from a native Editor test. The fixture now uses `EditorSceneManager.LoadSceneAsyncInPlayMode`; the failed receipt is retained. This failure does not establish a game defect. The final native lifecycle test passes, including public numeric mesh geometry, actual AI movement, stationary truck roots, pause, setup and rematch reset. Both original dealer regressions pass (skip/card invariants and actual restaurant traversal). The unchanged original core passes 505 assertions, including exhaustive five-card category checks, legal betting, side pots, chip accounting and AI tournaments.

The first Windows candidate `08e29a92` passed 114 assertions and completed an actual eight-AI tournament in 14 hands / 129 actions with all 1,600 chips conserved. Visual inspection nevertheless rejected it: the large broadcast stack labels had insufficient vertical room for their minimum glyph line height, and the new title overlapped the existing original logo. The one-time versioned authoring refinement gives the labels adequate height, moves only the new title and shortens the spectator caption. A new native assertion confirms visible numeric mesh geometry, and the refined Windows probe checks it at every resolution. A subsequent 122-check Windows candidate proved the proportional header clears the actual projected venue logo across all four resolutions. The baseline also lacked a visible winning-rank caption; the final HUD adds it using only already-revealed world card faces and the original evaluator. A third EditMode test verifies fold wins stay private and unavailable public faces do not reveal rank information.

All 9,482 original scene documents remain. Added UI and safe visual-wrapper hierarchy retain original parking transform values; all original card/rule/policy/dealer source bytes are preserved. Repeat refined authoring preserves the saved scene byte for byte.

Human playtesting, physical controllers and listening acceptance have not been performed.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** existing Heat buttons/fonts, DOTween documentation/public API, shared broadcast theme/portrait/rounded plates, original Poker card renderers/owned panes, mobile dealer, restaurant and input lifecycle.
- **VENDOR ASSETS USED:** Heat/uGUI/TMP, original portrait/theme/plates, DOTween public gaze interpolation and unchanged existing dealer/action/award animations.
- **RELEVANT ASSETS NOT USED:** a replacement poker engine, card renderer, input framework or physical vehicle controller would duplicate or disturb the approved core; none is introduced.
- **CUSTOM SYSTEMS CREATED:** read-only table HUD adapter, public-context visual-only presence, explicit additive authoring and focused tests/probe.
- **VENDOR SOURCE MODIFIED:** NO.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO.

## Final Windows acceptance and delivery

Exact payload e4ab7c474f7b3332986195f23164979e0eebcbe5a21ab27adfbb956b41d60d72 (managed 6094046e31ff9fe0140b7223b2950345d0d4670d32c7321e43380e26c4b890de) passes 123 presentation/gameplay checks and 63 independent original privacy/input checks, with zero failures. The first run covers twenty zero-to-four-human layouts at 720p, 1080p, ultrawide and 4:3; visible authoritative chip glyphs; measured projected-logo clearance; actual private peeking; public-only awards; original eight-AI tournament with conserved chips; rematch and menu cleanup. The independent probe exercises keyboard, whole controller, opposite shared-stick ownership, unassigned-pad rejection, cab free look, focus/pause clearing, actual software disconnect/reassignment, hot-seat handoff and viewport-safe card geometry. These are software-injected device checks, not physical-controller or human acceptance.

Actual composed images are inspected and retained in Evidence/2026-10-22-Poker-Table. The 545-file combined executable is delivered at Builds/TruckSports/TruckSports.exe, with 210 exact source inputs archived locally and the preceding accepted Air Hockey/Scrap build retained in recovery. The six native tests and 505 original core assertions pass. One fixture initially used an unsupported NUnit assertion; it was corrected to the installed API and rerun. Failed and visually rejected intermediate receipts remain retained. All 925 unrelated pending files remain byte-identical. Other active current-directive requirements remain open.
