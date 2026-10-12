# Truck Billiards — Truck Sports update
Independent local arcade 8-ball game inside Interstate: Hauler.
Scene: Assets/TruckBilliards/Scenes/TruckBilliards.unity
Current combined Windows executable: Builds/TruckSports/TruckSports.exe (the prior Billiards-only artifact is historical)
Keep every generated accompanying file with the executable.

## Menus and setup
Main menu: Play, Practice, How to Play, Settings, Quit.
Setup cycles modes, opening breaker (Player 1 / Player 2 / Random), and distinct truck colors. Both slots have editable display names, including AI. Human/AI sources and per-slot AI difficulties are independent; human/human, human/AI and AI/AI are supported. TruckBilliards.Setup.v2 persists setup across rematches and menu returns.
Menus support mouse, arrow/stick/D-pad navigation and Enter/South face-button submit. Tab/Shift+Tab moves through fields and buttons; East/B/Circle backs out. Escape backs out or opens Pause during a match.
Separate-controller join: press North/Y/Triangle on each device. Each device is locked to one slot. Two distinct controllers are required. Hot seat with a controller requires one joined shared device.
A lost assigned controller pauses the match and identifies the slot. Reconnect or explicitly reassign, then Resume. The match is preserved.

## Exact controls
| Mode / player | Drive | Brake | Reset | Pause |
|---|---|---|---|---|
| Single player | WASD or arrows | Space | R | Escape |
| Shared keyboard, Player 1 | WASD | Space | R | Escape |
| Shared keyboard, Player 2 | Arrows | Right Shift | Enter | Escape |
| Separate controllers, each assigned pad | Left stick steer; RT accelerate; LT reverse | South/A/Cross | North/Y/Triangle | Start/Menu |
| Hot seat, keyboard | WASD, current player's truck | Space | R | Escape |
| Hot seat, controller | Shared pad, current player's truck; stick/RT/LT | South/A/Cross | North/Y/Triangle | Start/Menu |

Both shared-keyboard or separately controlled humans can drive off-turn. Only the current player can strike the cue ball. No truck collider can push or block any ball.
Hot seat keeps the shooter's device while balls settle. A retained turn continues. When ownership changes, PASS TO [NAME] freezes both trucks until a fresh readiness confirmation. Handoff precedes scratch placement.
Reset affects only the controlled truck. Starting/resuming/placing/handoff consumes held driving and confirmation input.

## Cue placement
In competitive play, only a scratch grants placement to the incoming side.
- Player 1 keyboard / single player / keyboard hot seat: WASD or mouse over felt; F or left click confirms.
- Shared-keyboard Player 2: arrows position; Right Ctrl confirms. Player 1's mouse/keys cannot alter this placement.
- Assigned-controller player: left stick positions; that device's South/A/Cross confirms.
- Controller hot seat: the shared pad belongs to the incoming player after readiness.
Green means valid, red invalid. Ball overlaps, rails, pockets and outside positions are rejected. Confirmation restores the cue at rest and requires fresh driving input. AI places automatically after settling.

## Pause, settings and rematches
Escape or assigned Start/Menu opens Pause: Resume, Controls, Settings, Restart Rack, Restart Series and Main Menu. The restart and abandonment actions require confirmation. Practice replaces competitive restart actions with Reset Rack and Reposition Cue Ball.
Physics and AI freeze while menus, pause or handoff are active. Placement, handoff and group-choice state survive pause and reconnect. Ordinary turn changes and settling preserve held human driving and momentum; only striking eligibility changes.
Settings persist under TruckBilliards.Settings.* keys: master volume, SFX volume, supported display resolution, fullscreen/windowed, controller steering sensitivity and Vibration On/Off. AI difficulty persists separately. Audio previews immediately; Save/Apply stores and applies settings. All new effects use Master/SFX. There is no music track/category in this mode.
At rack end, the winner, reason and running score are shown. Next Rack preserves player setup and alternates the breaker. The deciding rack opens Series Won with Rematch Series. Restart Rack preserves completed scores/current breaker; Restart Series clears scores and restores the original series opener. Rematch Series alternates that original opener.

## Competitive rack series
Setup offers Single Rack (default), Best of 3 (first to 2 wins), and Best of 5 (first to 3). All competitive control/participant configurations support every format. AI-only series automatically advance between racks after a 2.5-second result display, while final results stay visible.
The HUD shows actual names, wins, rack number, target wins and single-player AI difficulty. BilliardsRules resolves each rack winner; BilliardsSession awards it once through BilliardsSeries. Early/illegal 8 and scratch-on-8 award the opponent normally.
Next Rack clears balls/groups/shot/placement/trucks/AI, retains identity/devices/difficulty/score, and alternates the opener. Hot seat requests fresh readiness when the next breaker differs from the current device owner. Reconnecting from a result screen restores that screen.
Restart Rack affects only the unfinished rack and preserves earlier wins/current breaker. Restart Series resets from the series' original opener. Rematch Series clears score and reverses that original opener. Deciding racks stop immediately.

## AI difficulty
Single-player setup has Easy / Normal / Hard; Normal is the default. Selection persists under TruckBilliards.Settings.Difficulty.
Every difficulty uses the same truck handling, physics, legal target checks and 15-unit normal shot-speed cap. Opening contact uses the same configurable 1.65 boost and 24-unit break cap for humans and AI. Navigation remains competent; aiming/power error is sampled once per attempt. No teleportation, ball manipulation or injected outcomes are used by AI.
| Profile | Aim error bound | Power error bound | Pot selection |
|---|---:|---:|---|
| Easy | ±5.5° | ±28% | Minimum alignment .8, object-to-pocket distance ≤8, first straightforward target; otherwise reachable legal contact |
| Normal | ±1.7° | ±12% | Original balanced scoring; minimum alignment .25 |
| Hard | ±.55° | ±4.5% | Minimum alignment .32; checks approach corridor/ghost position, scores rail space and approach distance, adjusts useful power for cut angle |
Serialized tuning is in BilliardsAI.easy/normal/hard: aimErrorDegrees, powerErrorFraction, minimumPotAlignment, maxPotDistance, simpleSelection, checkApproach and fallbackThrottle. A maneuver timeout bounds each approach. After failed progress, AI reverses/repositions and eventually relaxes the contact direction. Navigation exhaustion never passes the turn; a physical bumper strike is still attempted, even for a poor shot. Hard shot-quality tuning is unchanged. Placement and retained turns discard stale plans. Off-turn AI cruises without ball interference; modal screens stop it.
A reproducible three-layout/12-seed comparison is recorded in VALIDATION and FeatureSmokeTest/ai-comparison.csv. These are observed plans and real strike checks, not human win-rate estimates.

## Practice
Choose Practice from Main, select a name/color and keyboard/mouse or one controller, then start. Controller join uses North/Y/Triangle.
Only one truck is active; there is no opponent AI, group assignment, turn passing or win/loss. Pocketed balls, including the 8, remain down. One bumper strike starts a shot; driving continues while balls move, with striking disabled until settled. Scratches use the same valid placement flow for that player.
Pause with Escape or assigned Start/Menu:
- Reset Rack (confirmation) restores every ball, cue and opening truck; works mid-shot and clears pending motion/pocket/placement state.
- Reposition Cue Ball opens normal placement only while settled. It is unavailable during motion and is a Practice-only exception.
- Main Menu (confirmation).
Truck reset (R or North) remains separate from Reset Rack. Placement uses WASD/mouse + F/click or assigned stick + South. Brake and pause bindings are unchanged.

## Feedback
Original authored WAVs under Assets/TruckBilliards/Audio provide distinct ball clack, cushion, pocket, cue strike, turn, foul/scratch, rack and series cues. Heat Click.wav remains the menu click. No audio was downloaded.
Ball pairs report one clack via the lower-numbered ball only; floor/resting contacts are silent below a .35 relative-speed threshold. Contact strength scales volume with a square-root curve, with cue impacts attenuated to .6 and cushions to .8. Inspector impactGain=.25 and cueGain=.5 reserve headroom for overlapping effects. Subtle impact pitch range is .96–1.04. Six reusable sources bound simultaneous voices; overflow impacts drop instead of growing the break mix. All sources obey Master/SFX.
Vibration setting persists immediately. The assigned device gets a .13/.10s strike pulse, .16/.14s legal-score pulse and .23–.38/.30s win pulse. Hot seat uses the shared device. Pause, disconnect, handoff, Main, disable, lost focus and quit stop requests; an unscaled expiry also stops them. Unsupported devices never affect gameplay.
Winner-colored UI confetti uses DOTween: 12 pieces for a rack and 24 with a stronger title/sting for a series. Buttons remain immediately usable; next rack/menu clears the effect. Practice never triggers competitive celebrations.
## Pool rules
A legal open-table shot potting one group assigns it automatically. If it pots both groups, the shooter chooses after settling, with all pots retained. F/G chooses for WASD, Right Ctrl/Right Shift for arrows; the owning controller submits with South or the human clicks. Wrong-player controllers cannot confirm. Scratches/illegal shots keep the table open without a choice. This also applies to the simplified opening break. Legal own-ball pots retain the turn; opponent pots stay pocketed.
Wrong/missing first contact passes the turn with the cue where it stopped. Only cue-ball pocketing enables placement.
Your group must be clear at shot start to win with the 8. Early/illegal 8 or 8 plus scratch loses. Break 8 is safely respotted after settling.
No called pocket, rail-after-contact or tournament legal-break rule.
One capped forward bumper strike per shot; genuine contact and separation are required. Ball-settled checks use a sustained below-threshold period.

## Architecture and tuning
BilliardsRules remains the single scoring authority. BilliardsMatch owns the registry, settling and physical outcomes.
BilliardsSession owns player slots, identity, control source, menu/pause/handoff state and rematch setup. Slot groups derive from the rules.
BilliardsInputRouter reads each assigned device directly; it never routes both trucks through Gamepad.current. AI is enabled only for the AI slot.
Heat ButtonManager, SliderManager and InputFieldManager provide menu controls/navigation; TextMeshPro renders labels. Existing NWH/GR3D mesh geometry remains the truck foundation. Truck paint is applied to local runtime material instances.
Tuning: BilliardsMatch openingBreakBoost/maxBreakShotSpeed and rollingResistance/settledSpeed/settledDuration/pocketRadius; BilliardsTruck speed/acceleration/steering/minStrikeSpeed/powerScale/maxShotSpeed; BilliardsAI error/recovery limits; mode physics materials restitution.

## Build and focused tests
Unity menu: Truck Billiards / Build Windows x64 Development.
Menu upgrade: Truck Billiards / Upgrade Local Multiplayer Menus. It upgrades the existing saved scene and preserves table/gameplay objects.
The build uses an explicit one-scene list and does not replace host/Taxi build settings or profiles.
New-feature smoke: TruckBilliards.exe -tbFeatureSmoke -screen-fullscreen 0 -screen-width 1280 -screen-height 720
Existing multiplayer regression: TruckBilliards.exe -tbLocalSmoke -screen-fullscreen 0 -screen-width 1280 -screen-height 720
Focused reconnect/handoff probe: -tbHandoffProbe
The opt-in harness uses injected Input System devices and cloned in-memory input settings to avoid desktop-focus interference. It restores settings/prefs afterward. This is software evidence, not physical-controller or native OS-input validation.
Physical Practice pocket tests and existing full-match fixtures reposition balls, then require real motion/contact/capture. Series flow fixtures explicitly supply authoritative rule facts; they establish score/state flow, not physical full-series play.
Results/screenshots: FeatureSmokeTest/ and LocalSmokeTest/ alongside the executable; detailed evidence and remaining checks: VALIDATION.md.

## Playtest before release
1. Try each AI difficulty, then play a best-of-3 series. Check Next Rack, both restart actions, final score and rematch opener.
2. Practice on keyboard and a real pad: pot the 8, scratch/place, reposition while settled and reset during motion.
3. Listen to a full break and gentle taps; check distinct clacks/cushions/pockets, both mute settings and appropriate device vibration.
4. Play a full single-player match using native WASD/mouse, including a break, AI turns, ordinary pots, a scratch, pause/resume, restart and rematch.
5. Play shared keyboard with both people moving simultaneously; test off-turn cue contact and Player 2 scratch placement (arrows / Right Ctrl).
6. Connect two physical gamepads. Join with North/Y/Triangle; verify independent steering, off-turn protection, disconnect/reconnect and replacement during both normal play and placement.
7. Play hot seat on one keyboard and one real gamepad. Check legal retention, turn-changing handoff, held-button rejection and handoff before scratch placement, including pause/settings/disconnect recovery.
8. Change and relaunch display/audio settings; check fullscreen, supported resolutions and steering sensitivity with real hardware.

The executable is a development build. The OS window/product title inherits Interstate Hauler because host PlayerSettings are preserved. Distribution branding and a production build are later release steps. No physical gamepads were connected for this pass; injected devices establish software behavior only. See VALIDATION.md for precise evidence and remaining release gates.
## Vendor report
VENDOR ASSETS AUDITED: NWH/GR3D, Heat button/slider/input-field APIs and audio, UTS traffic, project environment/vehicle audio, DOTween public API/readme, Input System/DirectInput.
VENDOR ASSETS USED: NWH SemiTruck render geometry; Heat ButtonManager onClick/SetText/navigation, SliderManager value formatting, InputFieldManager; Assets/Heat - Complete Modern UI/Audio/Click.wav; DOTween at Assets/Plugins/Demigiant/DOTween/DOTween.dll (DOTween.To, DOScale, SetUpdate, SetEase, Kill); existing URP, uGUI, TextMeshPro and Input System 1.19.
RELEVANT ASSETS NOT USED: Full road-vehicle NWH/UTS stacks and wheel/DirectInput backend do not fit the existing explicitly authorized miniature collision-isolated mode. No installed asset supplies pool series/Practice semantics or pool shot planning. Vehicle/environment clips do not provide pool feedback; original procedural effects supply it.
CUSTOM SYSTEMS CREATED: Mode session/player-slot coordination, direct assigned input routing, pool-specific menu/HUD bindings, settings persistence, scratch/handoff coordination and focused fixtures. Existing pool authority/physics/AI are extended; series bookkeeping, Practice policy, AI difficulty evaluation, feedback adapter and regression fixtures added.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.





# Truck Sports collection integration — 2026-10-12

Truck Sports now hosts Billiards and Hockey. Existing Billiards series, Practice, audio, haptics and ghost-object-ball collision design are preserved and extended by the current update. The updated owned setup/rules/input/AI preserve existing collection entry and sport-selection returns. Direct Billiards-only launch still uses its original title/menu and builder.

Collection startup: Assets/TruckSports/Scenes/TruckSports.unity. Collection artifact: Builds/TruckSports/TruckSports.exe. The dedicated builder supplies an explicit front-end/Billiards/Hockey scene list and temporarily scopes the product title. Host PlayerSettings and EditorBuildSettings are restored exactly, including deliberate build-failure validation.

For the current collection validation and remaining native/hardware checks, read ../TruckSports/VALIDATION.md and CHECKPOINT.md. Earlier Billiards-only artifact title statements above describe that earlier artifact; the collection title is Truck Sports.

Current build, regression evidence and Git delivery: ../TruckSports/UPDATE_3V3.md. Reported AI forfeits and held-input freezes were reproduced; the old historical defect-free handoff is superseded.