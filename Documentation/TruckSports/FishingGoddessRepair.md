# Fishing — offscreen entrances and unwanted catches

Saved scene: `Assets/TruckFishing/Scenes/TruckFishing.unity`.

## Implemented

Fish no longer appear at the visible upstream edge. Each generated plan receives eight seconds of upstream approach, continuing its original velocity into the existing 20-metre active stream. The established downstream acceleration, species and pound scoring remain. The shared seeded director remains unchanged; an occasional scheduled non-poison arrival becomes a discarded tyre, canister or river stone. Water and terrain continue beyond the spawn area.

Each stream has 144 inactive pooled fish and 12 pooled debris models. A caught unwanted object awards no pounds, consumes its event ID once, and snags that participant's net. Four fresh alternating horizontal inputs clear it; a held direction cannot consume multiple stages. Six seconds provides automatic recovery. Fish continue past a blocked net without missed-weight penalties; other players and the survival clock continue. Pause, rematch and scene exit preserve or clear the correct local state.

Pixel Crushers `StandardUIQTEControls` handles indicator visibility, DOTween animates the existing Sports uGUI card, and assigned keyboard/shared-stick input remains under `SportsEventSession`. No global Dialogue Manager or new input framework was added. Layer Lab's existing fish/animation assets, RoadPropsPack models and River Modeler textures are reused without modifying vendor files.

## Actual validation

- Native saved-scene acceptance: **20 checks, zero errors**, October19.
- Sixteen real pooled-model/frustum checks across720p,1080p,4:3 and ultrawide.
- Moving debris catch, four independent viewports, shared-stick isolation, fresh-input alternation, pause, automatic recovery, rematch and spectator/menu cleanup.
- Complete four-Normal-AI survival: **280.22 simulated seconds,125 scheduled arrivals**, authoritative winner2; no pool overflow.
- EditMode: **2 tests pass**, including the existing10,000 seeded schedule/rule fixtures and duplicate unwanted-object event handling.
- Cross-mode PlayMode: **18/18 pass** following this integration.
- Actual720p QTE screenshot inspected: `Logs/TruckSports/EssentialRelease/Fishing/Goddess/qte-four-720.png`;1080p also captured.

These are native Editor simulation and software-injected input checks. Updated Windows, physical-controller, human and GPU acceptance remain pending. The latest compact backing-height refinement passed PlayMode but still needs a final rendered capture.

## Source

`FishingMatch`, `FishingStream`, `FishingRules`, new `FishingNetClear`, `FishingGoddessAuthoring`, `FishingGoddessProbe`, and dedicated EditMode tests. Three new editable sport-owned debris prefabs and the upstream riverbank asset are saved. Ordinary builds do not regenerate the scene.
