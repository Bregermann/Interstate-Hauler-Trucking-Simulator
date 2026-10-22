# Air Hockey presentation and Scrap Heap curation

This pass addresses HPR-14 and HPR-15. It preserves Air Hockey's first-to-seven rules, serving order, AI, puck/truck physics and arena. Log Rolling and Trampoline Dodgeball remain complete scene/assets in the build; their existing menu cards move into the existing Scrap Heap page alongside Wallball.

## Air Hockey

The saved presentation now has two driver setup cards using the existing name/input/difficulty/color controls, the original sport logo, a compact graphical score/serve display, player portraits/colors, visible actual-input prompts, paused scores and paired result cards. Existing buttons, events, controller ownership, reconnect and menu routes remain intact. DOTween supplies bounded score feedback.

`MatchSeconds` is a read-only display statistic. It accumulates only during existing live-play physics steps, stops during pause/goal/serve intervals and resets in the existing `StartMatch` path. It never changes the score, physics, AI or match duration. The HUD labels it **PLAY**; the match remains first to seven.

Windows visual review rejected the first candidate despite 55 passing assertions. The new setup had duplicated the existing saved branding image; the old Controls text remained inactive after being moved; and the legacy `SportsArenaOverlay.LateUpdate` revived empty projected plates. The refinement reuses the original branding node, removes only the identified unshipped duplicate, enables the existing controls and disables the obsolete layout component for this scene. AirHockeyMenus remains the sole UI refresh/lifecycle authority. The rejected candidate and receipt remain in local history.

Explicit authoring: **Truck Sports → Air Hockey → Upgrade saved menus and HUD**. Repeated authoring preserves designer layout and scene bytes. Normal builds do not rebuild the menus.

## Scrap Heap

The original Wallball, Log Rolling and Trampoline Dodgeball cards retain their launch actions and use their existing custom PNG logos and actual gameplay screenshots. The main catalogue excludes the three Scrap cards. The Scrap page has explicit left/right game navigation, down-to-Back navigation and initial focus on a game. Back returns to the active catalogue.

The explicit menu regeneration tools reuse the original card identities and reapply the category, preventing duplicate cards or accidental return to the main lineup. Artwork paths come from the existing scene registry. The first authoring attempt rejected a wrong Trampoline Dodgeball media filename before saving; the corrected tool preflights every original image before changing the menu.

Explicit authoring: **Truck Sports → Move preserved games into Scrap Heap**. Normal builds and gameplay retain the saved curation. No game scene, gameplay script or art asset is deleted.

## Validation

- Final native EditMode: **2 passed, 0 failed**, including saved player references, visible controls, one original logo, disabled competing layout and Scrap membership/actions.
- Final native PlayMode: **2 passed, 0 failed**, including live-play clock pause/reset, alternating rematch serve and Scrap focus/back flow.
- Existing Air Hockey rule fixtures: **26 assertions passed**.
- First Windows candidate: **55 checks passed**, but rejected after visual inspection. Refined Windows acceptance: **63 passed, 0 failed**, 71.42 real seconds. Includes six software-input configurations, four resolutions, a complete ordinary physical AI first-to-seven match (3–7), actual goal feedback, pause, results, alternating-serve rematch, both moved game launches and collection return.

A native test initially named the wrong EventSystem assembly; the fixture was corrected to the installed `UnityEngine.UI` assembly and rerun. Its failure is retained. Human playtesting, physical controllers and listening checks have not been performed.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** existing Heat/TMP controls and fonts, DOTween, shared broadcast theme/portrait/rounded plates, saved sport-branding widgets, catalogue/selection infrastructure, Air Hockey rules/AI/physics and existing menu/input handlers.
- **VENDOR ASSETS USED:** existing Heat buttons/input fields, Heat-derived TMP font, shared truck portrait and rounded plate, DOTween feedback, existing original sport PNG logos and gameplay screenshots. Original physics and scene-loading systems remain authoritative.
- **RELEVANT ASSETS NOT USED:** no replacement menu, input, camera, physics or artwork-generation framework is required. The existing logos are preserved.
- **CUSTOM SYSTEMS CREATED:** read-only Air Hockey presentation adapter, display-only live-play statistic, explicit saved-UI/category authoring, Scrap navigation and targeted verification.
- **VENDOR SOURCE MODIFIED:** NO.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO.

## Delivery and preservation

The exact 545-file Windows payload `040a0a1d92260de2238607e053a90adc0a70d365a8c00e872c8710f2953f5d48` (managed `12818bc6e77f7790a40fd35ce74ef3fe62d3acc9203c54f98e7f75c99cafb5c7`) is promoted to `Builds/TruckSports/TruckSports.exe`. The prior accepted Bowling build remains recoverable. The exact 201 compiled source inputs are archived locally. All 1,792 original Air Hockey and 3,904 original collection-menu scene documents are retained; modifications affect UI/default-null serialization, with no removed original objects or physical scene changes. All 925 unrelated pending files remain byte-identical. Labelled visual and acceptance evidence is in `Evidence/2026-10-22-Air-Hockey-Scrap`. Other active HPR/GBC requirements remain open.
