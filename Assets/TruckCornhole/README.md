# Truck Cornhole

Sixth offline Truck Sports game. Choose **Truck Sports > Play > Cornhole**, or open `Assets/TruckCornhole/Scenes/TruckCornhole.unity`. The truck IS the bag. No separate projectile, launch button, hot seat, networking or powerups.

## Play

One to four participants play simultaneously on isolated courses. Configure names, identifying colors, human/AI sources and independent Easy/Normal/Hard difficulty. Rematches retain setup. `TruckCornhole.Setup.v1` reuses Bowling's participant schema and exclusive-source validation without changing Bowling preferences or handling.

Drive down the slope onto the trampoline. Forward accelerates; opposite input brakes before reversing. Left/right steers. Whole controller: left stick, RT forward, LT reverse. Shared left/right sticks: XY movement, independently owned. Keyboard clusters: WASD, arrows, TFGH and IJKL. The shared controller-steering sensitivity setting scales each owned stick independently after Unity input processing, with bounded steering. Whole-controller ownership conflicts with either shared stick; duplicate sources and overlapping keyboard clusters are rejected. Assign connected pads explicitly in Setup/Devices; North/Y joins the next unassigned source.

Every view defaults to the **in-cab driver seat**. The horizon remains stable through spins and tumbles. A physical contact with the trampoline launches once, using entry speed, lateral momentum and heading. Learn the approach stripes; roughly 13–14 m/s at the bed is a starting reference, not a guaranteed scoring shot. Slow, fast and angled entries can miss. The revised high arc takes about six seconds to the board, under ordinary gravity. The distant board stays readable through the windscreen; an indicator marks an off-screen target.

In flight, left/right provides a small lateral nudge and bank. Release forward/back after launch, then press again for a limited range/pitch nudge. Held approach acceleration cannot consume the range correction immediately. The shared correction budget cannot provide lift, hovering or reverse flight. First board contact permanently disables propulsion/aftertouch for that body. The landing, bounce, skid and collisions then belong to PhysX.

Esc / assigned Start pauses without clearing bodies, flight velocity, attempt counts or scores. Reconnect or explicitly replace a missing controller, release controls and resume. Modal confirmation uses neutral/fresh-input gates. Configuration stays separate from existing sports.

## Scoring

This is **arcade free-for-all on separate boards, without cancellation scoring**. Five rounds, four fresh truck bodies per participant per round. Earlier trucks remain inert physical obstacles: later deliveries can knock them off or push them through the hole. Only the current truck receives input.

- **1 provisional point:** stable direct support on the board's top, low motion for 1.2 seconds, and no surrounding-ground contact. Hovering, side-face contact or a moving body does not qualify.
- **3 points:** the complete collider passes downward through the actual opening, counted once and collected with live collisions disabled. Partial overlap or a hang does not count as holed.
- **0:** off board, ground contact (including board plus ground), or a still-moving body excluded at the generous simulation safeguard.

One-point landings are never permanently banked per attempt. Everyone's fourth delivery and relevant settling complete before the round total is banked. Round transitions clear the pooled bodies; highest cumulative score after five rounds wins, with ties retained. Courses progress independently and AI-only matches need no confirmation.

## Replays

After a throw settles, use its view's **REPLAY** button. **ANGLE** cycles chase, board-side, overhead and cab. **SPEED** cycles 0.25×, 0.5×, 1×, 2× and 4×. **RESTART** rewinds; **LIVE CAB** returns to play. Final standings include **REVIEW LAST THROWS**.

Replay records all four bodies on that course at 20 Hz, interpolating collider-free visual meshes. It does not resimulate physics, apply forces, rewind scores or alter the global time scale. Other courses continue. The reviewing course waits before its next delivery; return to live or the 45-second viewing limit releases it. A round reset clears recordings. Pause preserves the replay clock. Controls are on-screen and mouse accessible. A whole-pad owner can use West/X to open or change angle, South/A for speed, North/Y to rewind and East/B to return. Shared-stick viewers use the on-screen controls to avoid ambiguous shared buttons. Returning requires neutral movement before control resumes. Real controller navigation remains a human acceptance check.

## Implementation and tuning

NWH WheelController owns approach tire/suspension forces, using the same vendor foundation as Bowling. Its generated wheel mesh colliders are explicitly disabled after launch because disabling only WheelController leaves zero-friction wheel colliders active; those prevented reliable board rest. The chassis remains dynamic and owns landing contacts. GR3D truck geometry is constructed at the origin before positioning each course's trucks.

Each course has a 13.5-degree approach, visible compression-only trampoline animation, a 14×24 inclined board with a real radius-1.65 opening, surrounding catch ground, and four reusable bodies. Render isolation uses scene-local camera masks; collider layers, TagManager and host physics settings are preserved. Physics mesh, material, light, camera, cab, UI and audio authoring are saved in the scene.

Launch parameters on `CornholeCourse`: upward base 30 m/s plus 0.06× entry speed; forward 0.65× entry speed +1 m/s, bounded 3–15; ordinary gravity unchanged. Lateral entry velocity and heading are retained with bounds. Aftertouch acceleration 0.65 m/s², total delta-velocity budget 1.6 m/s, no vertical force. Same controls and limits apply to humans and AI. AI uses approach alignment, speed selection, ballistic estimates, reaction/error differences and opportunities to push an earlier board truck. It aims the airborne landing before the hole, leaving room for the physical skid instead of spending every correction pitching toward the hole. It never directly awards points or homes a body onto a scoring path.

Board support is cleared on actual contact exit, including sleeping bodies; a lifted or indirectly supported truck cannot retain a stale one-point credit. Stable settling is observed for 1.6 seconds across delivered bodies. A truck resting on another truck or hanging with no qualifying top support resolves normally for zero; it does not wait for a simulation-failure timer. Contact with an already-landed truck ends aftertouch too. Only the current body can advance the delivery phase.

Irretrievable misses below the board on the lower catch ground are collected after 1.2 seconds of ground contact and stay zero. This removes dead misses, not live on-board movement. Already-holed trucks retain their credit; an excluded simulation failure cannot gain late hole credit. Genuine failure guards: approach 60 seconds, flight 15 seconds, landing/settling 35 seconds; a moving body at the limit is excluded from board points without freezing or teleporting it. The final round wait has a 12-second safeguard, also excluding moving board points. A hole already fully passed stays scored.

Heat/uGUI/TMP provide setup, HUD, settings and modals. DOTween animates trampoline compression. Vendor engine/skid and project-owned launch/wind/result sounds share Sports audio settings. One AudioListener serves split screen. Haptics route only to a whole-pad owner; shared-stick slots omit ambiguous whole-device rumble. Pause, disconnect, focus loss and scene exit stop vibration.

## Authoring and build

`LWS.TruckCornhole.Editor.CornholeAuthoring.Create` regenerates only Cornhole and upgrades the six-sport front end. It validates references, an unobstructed real hole, four truck pools, cab/replay references, no missing scripts, and one listener/EventSystem. Existing creation/upgrade hooks retain Cornhole.

For the final combined Windows x64 Development build use **`LWS.TruckSports.Editor.SportsBuilder.Build`** against saved scenes. Seven explicit scenes start at TruckSports and include all six sports. Exact host settings restore on both success and deliberate failure. Avoid `CreateValidateBuild` for routine builds because it regenerates other sports.

Output: `Builds/TruckSports/TruckSports.exe` plus the complete accompanying payload. Generated builds, caches and logs are ignored, not Git deliverables. See `VALIDATION.md` and the collection checkpoint for actual evidence and remaining human checks.
