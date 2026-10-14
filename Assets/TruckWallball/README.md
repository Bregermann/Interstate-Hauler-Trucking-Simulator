# Truck Wallball

Offline 1v1 in the five-sport Truck Sports collection. Saved scene: `Assets/TruckWallball/Scenes/TruckWallball.unity`. Combined Windows x64 development executable: `Builds/TruckSports/TruckSports.exe`; keep its accompanying files together. Enter through Play → Wallball.

Both trucks drive and jump throughout rallies and pass through each other. Only the named eligible participant can contact the ball; body and NWH wheel contacts are disabled for everyone else. A newly eligible truck must separate from an overlapping ball before contact is armed. This changes collision ownership without stopping momentum or gating held driving inputs.

Physically hit the highlighted front wall before the floor. A valid wall contact enables the opponent: volley, or return after one floor bounce. SECOND BOUNCE faults the receiver. MISSED WALL and OUT fault the responsible hitter, including an unreturned out ball after a legal wall contact. Every rally scores. First to 11, win by two; the point winner serves next. These are simplified arcade rules.

Serve from the marked box into the reachable ball within 25 active seconds. The receiver starts behind the serving lane. Countdown, pause and disconnect do not consume the timeout. Rematches preserve names, colors, sources and independent difficulty, and alternate the opening server. Genuine missing/invalid simulation state announces a neutral replay with unchanged scores; ordinary misses award points.

| Source | Drive | Jump |
| --- | --- | --- |
| First keyboard | W/A/S/D | Space |
| Second keyboard | Arrow keys | Right Shift |
| Whole controller | Left stick steering, RT forward, LT brake/reverse | South / A / Cross |
| Shared controller, first owner | Left stick X/Y | LT |
| Shared controller, second owner | Right stick X/Y | RT |

Opposite throttle brakes first, then reverses. Jumps require grounding and a cooldown; midair repeats are rejected. North / Y / Triangle joins an input source; Esc / Start pauses. Whole controllers have one owner, or complementary sticks/triggers have separate owners. Source assignment rejects overlaps. Assigned disconnect pauses both shared owners, preserves score and physical rally state, and permits reconnection or explicit replacement. Modal confirmations require neutral controls and fresh input; confirmation cannot leak into a jump. AI-only games require no connected controllers or confirmations during normal play.

The shared camera faces a single front wall. Side/rear lines are out, without rebound walls. Ball color, truck marker, participant names, score, server, eligibility, first bounce and point reason identify the current play. The shadow and first-landing ring forecast wall rebound and floor landing; they never award points. Shared Master/SFX, vibration and display settings remain in use. Sound and assigned-device feedback stop on pause, disconnect, focus loss and unload.

## Ownership and vendor foundation

NWH WheelController 3D alone owns tire friction, suspension, wheel forces and grounding, following the existing Bowling adapter pattern. Four tiny wheels use the vendor DefaultFrictionPreset; casts include Default-layer court geometry and exclude trucks/ball on built-in Ignore Raycast. Per-collider pairs make trucks ghost each other and disable ineligible ball contacts. Arming uses explicit current Rigidbody poses, avoiding interpolated render-pose queries. Project layers and collision settings are unchanged.

LWS adds input ownership, upright physical jumps/local gravity, speed bounds, once-per-contact bounded lift/strength, trajectory forecasting, rally rules and match coordination. Contact angle and existing PhysX response determine direction; assistance never aims at the wall. Jump speed 9 with local gravity 24 gives a visible physical hop. Ball gravity 12, radius .45, speed cap 15, floor rebound .88 and wall rebound .9. Contact lift is bounded to 6.2–8.5, keeping low returns and airborne volleys practical. The Taxi companion bounce was inspected: it uses render proxies; Wallball moves the physical body so jumping contact is real. Taxi source is unchanged.

AI forecasts wall/floor rebounds, selects reachable interception space behind the ball, drives/jumps through the same controller, and reverses after getting stuck. Off-turn serve positioning waits behind the ball, and outgoing forecasts select the returning trajectory after the wall. Difficulty changes reaction, prediction/position error and jump availability. No AI speed/grip advantage, direct ball force, live-return teleport or injected score.

Heat uGUI and the shared sports menu/input/settings/audio adapters provide setup, pause, rules, settings, disconnect and results. DOTween animates results. UI references and participant strings refresh on state changes; HUD refresh is throttled. Each scene owns its subscriptions/materials/audio and retains one EventSystem/AudioListener.

## Authoring and validation

Use `LWS.TruckSports.Editor.SportsBuilder.Build` for final builds from saved scenes. Its six-scene array starts with TruckSports, then Billiards, Hockey, Bowling, Air Hockey and Wallball; it restores host title/build settings on success and deliberate failure. Do not use `CreateValidateBuild` as validation: it reauthors existing scenes. Wallball's `CreateBuild` authors only its own scene plus the fifth card; `UpgradeBuild` updates its setup labels and front end. Legacy front-end upgrades retain the fifth entry.

`WallballFixtures.Run` checks pure rally rules. Development flags `-tsWallball` and `-tsWallballAI` run software-input/physical probes and an autonomous match. Their native device isolation is confined to that player process. See VALIDATION.md for actual evidence and remaining hardware/human checks.

VENDOR ASSETS AUDITED: NWH WheelController/ArcadeModule, GR3D truck, Heat, DOTween, existing sports and Taxi bounce adapters.

VENDOR ASSETS USED: NWH wheels/DefaultFrictionPreset/Truck4 engine audio, GR3D geometry, Heat UI, DOTween, shared sports input/settings/audio.

RELEVANT ASSETS NOT USED: NWH road VehicleController/ArcadeModule lacks rally/jump semantics; world traffic/road/weather/streaming and online systems are outside this offline court.

CUSTOM SYSTEMS CREATED: Wallball rules, eligibility, physical jump/contact coordination, forecast/AI, court authoring and validation probes.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO; NWH retains tire/suspension authority.
