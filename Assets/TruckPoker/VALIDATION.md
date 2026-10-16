# Poker rules validation

2026-10-16:505checks passed in both an isolated .NET10 harness and the actual Unity6000.4.10f1 C# runtime. Native marker: Logs/TruckSports/SevenSports/poker-core-native.txt. Harness: Logs/TruckSports/SevenSports/PokerRulesHarness/result.txt. This is rules and policy validation, not a playable Unity match, rendering benchmark, controller test or human playtest.

Coverage: all2,598,960 distinct five-card combinations match the nine category totals; best-seven/wheel/board-equivalent ties and duplicate rejection. Main/side pots, folded ineligibility, split/odd chips and uncalled returns. Heads-up pre/postflop order/button alternation, blind option, minimum raise/rejected underraise, single and cumulative short all-ins, prior checks versus incomplete opening, short heads-up big blind with no phantom call, multiway full bring-in, runout and showdown exposure. Chip conservation is checked after every action.

Eighty-four complete random legal-action tournaments cover2–8seats and12seeds per size. Nine complete actual own-snapshot AI-policy tournaments cover2/5/8seats ×Easy/Normal/Hard. Observed AI hands/actions:2seats9/73,2/19,8/48;5seats14/186,21/223,25/234;8seats17/187,25/333,21/239. These demonstrate legal completion, not relative human competitiveness. Policy-boundary checks reject engine/deck/RNG authority fields and prove snapshot mutations cannot change stacks.

Unity Editor PokerFixtures.Run() runs focused rules; Run(true) adds exhaustive enumeration and tournaments. A draft phantom-call defect was corrected before importing the core; the same tests then passed in Unity. Harness CLI home, package/cache and build artifacts remain under ignored project Logs.

Remaining: playable saved scene and UI, real source ownership/handoff/hold-to-peek leakage tests, camera/render privacy, AI-only Unity matches/rematches, dealer/public reactions, full-window720/1080/aspect checks, final combined Windows build/player acceptance, sustained performance and human/hardware testing. No build completion claim for this rules stage.
