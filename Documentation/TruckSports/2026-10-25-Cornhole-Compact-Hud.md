# Cornhole compact owned HUD

Carnival §12.6 source preparation, October 25, 2026. Explicit author/native/image/human validation remain pending. The agent ran no Unity/player/Git, wrote no CLEAN files and saved no scene. Frozen shared-garden gameplay/camera/replay/art files remain unchanged by this slice.

Entry: `LWS.TruckCornhole.Editor.CornholeHudAuthoring.InstallOpenScene()`. Root opens the original Cornhole scene, runs shared-garden author first and HUD author second, reviews and saves. The HUD author never opens/saves scenes or edits shared components/assets. It reuses existing Heat fonts, truck portrait, branded plate and original UI action components; no runtime HUD hierarchy generator exists.

`CornholeCompactHud` is a typed read-only adapter for legacy Cornhole, not another SportsEventSession or score authority. It shows original banked totals separately from provisional/final-round delta, actual owner identity/color/NWH portrait, round/delivery, and original phase elapsed or replay clock/rate. No invented match countdown or score is introduced. Name labels disable rich-text interpretation. Duplicate names retain stable participant IDs and P-slot/color identities.

One 318×94 maximum card sits at each enabled viewport's outer top corner. Single-camera spectator mode also has a compact four-owner roster using actual totals/provisional state. Permanent rule/debug rails are suppressed by disabling only their existing Graphics; underlying objects/source labels remain for original scripts/tests and exact enabled-state fallback. Original contextual target indicator remains. Disabling the adapter restores each original Graphic.enabled value and hides/kills its own card presentation/tweens.

Original CornholeWindDisplay remains enabled and solely owns wind arrow/MPH/vendor camera allow-list. Its existing bottom-right plate becomes143×59; no wind/weather/physics code or value changes. Existing replay buttons become128×38 and retain their original SportsMenuAction/input/ownership/timeline paths. Cards, wind and replay controls occupy separate areas, with clear space for the shared score notification. Global canvas, UI palette, shared compact viewport code, profile data and other sports are unchanged.

Native filter: `CornholeCompactHudPlayModeTests` (two cases):

- Actual natural original AI flight and phase time, original replay button actions/all four angles, genuine whole-round banked scores and unchanged journal, fresh rematch. No supplied positions/points/finish/receipts.
-1–4 enabled viewports plus single-camera four-owner roster, saved portraits/stable IDs/bounds,720p/1080p captures, existing20replay actions, pause and exact Graphic.enabled fallback. A separately labeled frozen render-only input checks the actual existing wind display reads10.0MPH from the original direct-headwind cap without changing source flow/scores/clock. This controlled rendering fixture is not weather/physics acceptance.

Static semantic compilation against534cached native references reports0errors for all Cornhole runtime sources, new HUD author/tests and the separately repaired bird diagnostic fixture. Native execution, visual inspection at both resolutions, physical controls and human acceptance are not inferred.

VENDOR ASSETS AUDITED: Heat broadcast fonts/UI, existing SportsBroadcastPlate/portrait, DOTween and original weather/wind presentation.

VENDOR ASSETS USED: existing Heat fonts, installed NWH portrait resource, broadcast plate and DOTween score pulse; existing Weather Maker/CornholeWindDisplay authority retained.

RELEVANT ASSETS NOT USED: SportsCompactViewportHud component binds SportsEventMenus/SportsEventSession; legacy Cornhole has independent types. A small mode-only typed adapter avoids changing shared authority or creating another backend.

CUSTOM SYSTEMS CREATED: Cornhole-only read-only HUD binding, explicit additive author and focused native tests.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
