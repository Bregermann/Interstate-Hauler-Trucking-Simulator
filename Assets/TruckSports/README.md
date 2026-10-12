# Truck Sports

Windows x64 development collection inside Interstate: Hauler. Startup scene: Assets/TruckSports/Scenes/TruckSports.unity. Executable: Builds/TruckSports/TruckSports.exe. Keep all generated accompanying files together.

## Front end
TRUCK SPORTS > Play > choose Billiards Match, Billiards Practice or Hockey. Billiards opens its retained setup and all existing series/difficulty/Practice/local control features. Hockey opens its own setup. How to Play, Settings and Quit are available from the title.
Mouse, arrows/stick/D-pad, Enter/South submit, Tab/Shift+Tab, East/Esc back. Controller players can retain the default names. Returning from a sport loads the front end in Single scene mode; services are scene-owned, with one active EventSystem and AudioListener. Active navigation links are configured through Heat public fields and uGUI together, including Start becoming enabled after joining.

## Hockey setup and controls
Player vs AI (keyboard or one assigned controller); Shared Keyboard; Separate Controllers; Keyboard vs Controller (keyboard P1, assigned pad P2). No hockey hot seat. Billiards retains hot seat.
Choose names, distinct colors, Easy/Normal/Hard AI, and 2/3/5 minutes (default 3). North/Y/Triangle joins the first empty controller slot. One pad cannot occupy both slots. Solo assigned-pad and two-pad matches do not require a keyboard. Use Reassign for explicit replacement. A rematch waiting for a missing device retains its setup and swaps ends once the replacement joins and Start is submitted.

| Control | P1 keyboard | P2 shared keyboard | Assigned controller |
|---|---|---|---|
| Drive | WASD | Arrows | Left stick steer, RT gas, LT reverse |
| Brake | Space | Right Shift | South/A/Cross |
| Recover own truck | R | Enter | North/Y/Triangle |
| Pause | Escape | Escape | Start/Menu |

Both trucks can hit the puck continuously and physically bump each other. There is no shooting button, possession, automatic dribbling or special ability. Recovery has a two-second cooldown and selects a separated safe position in the requesting truck's own half; puck, score and clock are unchanged. Truck teleports clear that truck's contact journal so an old pin cannot cause a false emergency faceoff.
Disconnecting an assigned pad pauses and identifies its slot. Reconnect the same device, or Reassign and join a replacement, then Resume. Score/time/puck and prior live/countdown phase are retained. Release controls before a fresh movement/recover input.

## Hockey rules and flow
Pregame 3–2–1–GO, live regulation, goal celebration, fair faceoff/reset/countdown, pause, sudden-death overtime, result. The clock advances only on completed live regulation simulation steps. A goal requires the entire flat puck to cross the line through the open mouth. Outside/side-net contact does not score. Net ownership determines the scoring team, including own goals; each goal is awarded once.
A crossing at or before the regulation deadline within the physics step counts. A later crossing is discarded before regulation result/overtime reset. This is determined from the swept simulated puck segment and crossing fraction, never collision callback order.
Goals stop interaction immediately, display the scoring team/score and reset separated trucks/puck with cleared velocities. Pauses, celebrations and countdowns use no regulation time. A regulation leader wins; a tie starts a fresh sudden-death faceoff. The first overtime goal ends immediately.
Result: Rematch (preserves setup, swaps ends), Change Setup, Sport Selection. HUD goal ownership, spawn orientation, team crease colors and AI orientation swap together. Pause: Resume, Controls, Settings, Restart Match(confirm), Sport Selection(confirm). Restart retains current ends/setup and clears score/time.
No offsides, icing, penalties or goalies. Rounded boards contain the puck; truck-only barriers keep trucks out of the goal nets without blocking scoring. A stationary accessible puck is left alone. Out-of-bounds/embedded puck or a prolonged two-truck contact deadlock announces an emergency fair faceoff, preserving score and remaining regulation time.

## Physics and AI
Hockey-owned constrained dynamic Rigidbody bodies use one coherent PhysX collision model. Hits receive no additional custom impulse. The puck and trucks are held upright on a plane; continuous collision detection, bounded depenetration and speed caps limit runaway contacts. Ice gives modest lateral slide while steering stays responsive.
Serialized tuning: HockeyTruck acceleration/maxSpeed/reverseSpeed/turnDegrees/lateralGrip/rollingDrag/brakeDrag/recoverCooldown; puck radius/drag/maxSpeed; mode contact physics materials; match goal opening/corners/deadlock timeout.
AI uses Defending, Intercepting, Approaching, Attacking and Recovering states. It predicts short travel with reflected board bounds, approaches from the attacking side, routes around the wrong side, defends its own net and backs away after blocked progress. It drives through exactly the same truck component/contact limits. No teleport, direct puck forces or hidden score changes.
Easy reaction .40s/prediction .18s/position error ±.65/steering error ±6 degrees. Normal .18s/.45s/±.25/±2 degrees. Hard .08s/.75s/±.08/±.7 degrees, with earlier threat defense. Error is sampled per decision, not frame noise. Hockey difficulty uses TruckHockey.Difficulty; Billiards keeps its own persisted difficulty.

## Shared settings and feedback
SportsSettings adapts the existing TruckBilliards.Settings.* keys for Master/SFX, steering, display and vibration, so collection scenes use the same preferences. Existing BilliardsSettings remains intact. Hockey audio reuses Heat Click and original Billiards impact assets; original Faceoff, GoalHorn and FinalWhistle WAVs live under Assets/TruckHockey/Audio. Six pooled voices, threshold/cooldown/strength scaling and Master/SFX bound collision feedback.
Assigned-pad strike/goal/win requests are restrained, expire in unscaled time, and stop on pause/disconnect/scene unload/disable/focus loss/quit. Unsupported device commands are optional and cannot fail gameplay. Short colored DOTween confetti differentiates goal and win; no wait is required for rematch.

## Build and validation
Truck Sports > Create Front End and Hockey Scenes authors and wires both new saved scenes. Truck Sports > Build Windows x64 Development validates the three scenes and builds using an explicit scene list. Product/window title is temporarily Truck Sports; a finally block restores the prior PlayerSettings property and exact ProjectSettings/EditorBuildSettings bytes, including failure paths. The existing Billiards-only build method remains usable.
Opt-in development probes: -tsSmoke; -tsBilliardsFeature -tbFeatureSmoke; -tsBilliardsRegression -tbLocalSmoke. Probes clone background input settings and restore preferences/devices. Normal launches run no probes.
Final build passed the retained Billiards feature (92 checks) and local regression (58 checks) suites. Read VALIDATION.md and CHECKPOINT.md for collection results, artifact hashes, observed performance and limitations. Injected device/haptic checks are software evidence. Physical goal tests reposition trucks/puck into reproducible layouts then require actual simulation/crossing; they never inject scoring events.

## Human playtest
1. Play both sports through repeated returns to selection; check Billiards series/Practice and Hockey end swaps.
2. Play native keyboard and two-person shared keyboard hockey, including goals, overtime, recovery and restart.
3. Join real solo/two/mixed pads, unplug during live/countdown/pause, reassign, and check fresh input plus appropriate actual rumble.
4. Listen to puck/board/truck contacts, horn/whistle/countdown, both mutes; check 720p/1080p, other aspect ratios and display settings.
5. Try all AI difficulties for full matches, corners/banks/defensive blocks and repeated rematches.

## Vendor report
VENDOR ASSETS AUDITED: NWH Vehicle Physics 2 readme, VehicleController and ArcadeModule; NWH/GR3D SemiTruck prefab; UTS_FullPack CarAIController; Heat ButtonManager/SliderManager/InputFieldManager and audio; existing Billiards audio/input/settings; DOTween readme/APIs; installed Input System 1.19 and UI action asset.
VENDOR ASSETS USED: Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab render geometry; Assets/Heat - Complete Modern UI controls and Audio/Click.wav; DOTween Assets/Plugins/Demigiant/DOTween/DOTween.dll (DOTween.To, DOScale, SetUpdate, Kill); Input System default UI bindings copied into Assets/TruckSports/Input/SportsUI.inputactions; uGUI/TMP/URP and retained Billiards control/visual/setting conventions.
RELEVANT ASSETS NOT USED: NWH powertrain/wheel ArcadeModule needs grounded road-wheel dynamics, not the constrained planar two-body rink/puck model; UTS relies on road MovePath/CarMove and does not supply hockey interception/goal-side positioning. Wheel/DirectInput and world authoring systems do not supply these sports mechanics. Audited environment audio has no hockey horn/whistle set; three original procedural cues were authored.
CUSTOM SYSTEMS CREATED: Hockey rules/simulation timing, planar contact/control adapter, sport-specific AI, recovery and device coordination; shared front end/settings/input/feedback adapters, scene authoring and focused validation. Existing Billiards rules are not reused by Hockey.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
