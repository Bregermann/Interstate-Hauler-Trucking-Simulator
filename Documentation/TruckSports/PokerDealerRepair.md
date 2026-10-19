# Poker — travelling dealer

Saved scene: `Assets/TruckPoker/Scenes/TruckPoker.unity`.

The existing recognizable truck dealer now shuffles, drives a continuous route around participating seats, stops, turns, reaches and hands each truck two face-down cards, then returns home. Its actual wheel meshes roll and steer around their authored centers. Sixteen pooled transit-card hierarchies reuse the existing card presentation; they never receive private card values. DOTween owns choreography.

The existing `PokerTable`, evaluator, deck, betting, side pots and chip awards remain authoritative and unchanged. Betting/peeking waits until delivery completes. Cinematic/Quick pacing persists, and a fresh action or visible Skip Dealing button completes presentation exactly once without changing the cards. Pause stops the sequence; setup, rematch and exit cancel callbacks and retire all transit objects.

## Observed native results

`Logs/TruckSports/EssentialRelease/Poker/Dealer/result.txt`: **22 checks, zero errors**.

- Eight-seat cinematic deal:16 face-down handoffs,189.7m of continuous travel, maximum frame movement0.424m.
- Actual wheel rotation observed; maximum wheel-center drift0.00000151m.
- Pause preserves travel and callbacks; private transit/pane display data remains hidden.
- Every authoritative hand, actor, starting chip value and action count is unchanged by the cinematic deal. Repeated Skip is idempotent.
- Complete four-seat Easy, Normal and Hard AI tournaments finish with exact800-chip conservation and one champion each. Normal6 hands/60 actions; Hard3 hands/27 actions. The receipt contains all actual Easy metrics.
- Rematch, setup cancellation and menu/audio cleanup pass.
- Actual720p and1080p views captured; the first wide720p view was inspected.

A closer spectator camera passed a separate13-check native view run, and its720p deal image was inspected. The adapted focused input/privacy regression completed with zero errors across five aspect ratios, keyboard/whole-pad/shared-stick injected devices, hot-seat handoff and rematch. The saved-scene skip/privacy regression also passes in the24-test Goddess PlayMode suite. A modeled casino room upgrade is prepared in source but is not yet saved or visually accepted. Updated Windows, physical-device and human checks remain unperformed.

Sources: new `PokerMobileDealer`, `PokerMobileDealerAuthoring`, `PokerDealerProbe`; targeted changes in `PokerMatch`, `PokerPresentation` and authoring defaults. No vendor or original Truck Taxi source changed.
