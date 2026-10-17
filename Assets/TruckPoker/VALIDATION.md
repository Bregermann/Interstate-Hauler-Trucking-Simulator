# Current combined Windows delivery — October16,2026

Complete offline virtual-chip Hold’em is included in the final31-sport Windows build. Full eight-seat tournaments and four-view hold-to-peek/privacy/input/reconnect checks passed. Exhaustive rules evidence below is unchanged; actual hardware and human betting/privacy ergonomics remain manual. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Poker rules validation

2026-10-16:505checks passed in both an isolated .NET10 harness and the actual Unity6000.4.10f1 C# runtime. Native marker: Logs/TruckSports/SevenSports/poker-core-native.txt. Harness: Logs/TruckSports/SevenSports/PokerRulesHarness/result.txt. These505 checks cover the pure rules/policy. Separate saved-scene acceptance below covers the playable runtime. Neither is a physical-controller or human playtest.

Coverage: all2,598,960 distinct five-card combinations match the nine category totals; best-seven/wheel/board-equivalent ties and duplicate rejection. Main/side pots, folded ineligibility, split/odd chips and uncalled returns. Heads-up pre/postflop order/button alternation, blind option, minimum raise/rejected underraise, single and cumulative short all-ins, prior checks versus incomplete opening, short heads-up big blind with no phantom call, multiway full bring-in, runout and showdown exposure. Chip conservation is checked after every action.

Eighty-four complete random legal-action tournaments cover2–8seats and12seeds per size. Nine complete actual own-snapshot AI-policy tournaments cover2/5/8seats ×Easy/Normal/Hard. Observed AI hands/actions:2seats9/73,2/19,8/48;5seats14/186,21/223,25/234;8seats17/187,25/333,21/239. These demonstrate legal completion, not relative human competitiveness. Policy-boundary checks reject engine/deck/RNG authority fields and prove snapshot mutations cannot change stacks.

Unity Editor PokerFixtures.Run() runs focused rules; Run(true) adds exhaustive enumeration and tournaments. A draft phantom-call defect was corrected before importing the core; the same tests then passed in Unity. Harness CLI home, package/cache and build artifacts remain under ignored project Logs.

Saved-scene Unity acceptance (2026-10-16): three complete eight-seat AI tournaments passed with0runtime errors and exact4000-chip conservation: Easy25hands/235actions, Normal13/176, Hard22/284. Each produced exactly one winner. These are completion observations, not a difficulty-ranking claim. Native marker poker-integrated.txt confirms saved references, one listener, collection entry, authoring defaults and28explicit scenes.

PokerFocusedEvidence passed with0errors: four independent human panes, whole-controller and opposite-stick ownership, hidden-by-default/release clearing including actual TMP geometry/character metadata, no private world faces, peeking cannot bet, unassigned pad rejection, independent whole-pad/mouse cab look with stable interior, fixed shared-stick views, pause preserving exact hand/actor/pot, fresh reveal after resume, actual software device removal and explicit replacement, hot-seat held-confirmation/peek suppression, rematch reset, simultaneous duplicate rejection. Five controlled camera/composite UI renders cover1280×720,1920×1080,1440×900,1280×960 and2560×1080 with visible cards bounded to their own view.720p private ranks/suits/canonical cockpits visually inspected. These are manual Camera.Render/composite fixtures, not normal player-window capture or GPU performance evidence.

Initial test failures are retained under Logs/TruckSports/SevenSports/PokerFocusedInitial. The first three-frame waits did not guarantee an input update; the unfocused Editor also routed keyboard/mouse away from the Game view. Probe-only timing and temporary Editor input routing were corrected and restored afterward. No production privacy gate was relaxed. A real presentation defect (inactive rank geometry) was fixed by activating before TMP mesh generation; hidden state now clears glyph metadata as well as text and mesh. A shared HUD defect repeatedly reactivated legacy standings under participant cards; menu visibility now respects the cached card presenter.

Remaining: final combined Windows build/player acceptance and normal full-window layouts, sustained rendered performance, real hardware, human gameplay/art/audio acceptance. The sole current executable remains the earlier25-sport milestone; no Windows Poker delivery claim yet.
