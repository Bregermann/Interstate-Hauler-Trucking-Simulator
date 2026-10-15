# Hardcore Truck Cheese Rolling validation — 2026-10-15

The completed seven-sport collection was validated in a visible Windows x64 Mono Development player. All results below are native completed runs, with exit0 and CHEESE_SMOKE_FINISHED code=0. No physical-controller or human acceptance testing is claimed.

## Automated rule and scene checks

300 distinct editor assertions: Hockey20 / participant ownership16 / Billiards50+72 / Bowling32 / Air Hockey26 / Wallball30 / Cornhole16 / Cheese38. These are project-native assertion fixtures invoked by the builder, not a claim of 300 Unity Test Runner test cases. Saved scenes validate references, listeners, event systems and missing scripts. The new mode contains16 trucks, four cameras,36 genuine sheep,14 physical trees,18 rocks and8 cat/wolf props.

The five new start assertions cover early held input, a fresh press during2, continued hold during1, release cancellation and late rejection. Unique contact, background exclusion and same-step tie rules are deterministic rule fixtures; the full games below use genuine body contacts, not injected scoring.

## Focused physical/input checks

[Guards13:46 assertions](../../Documentation/TruckSports/CheeseEvidence/guards-13.txt) pass. Actual software keyboard edges confirm early/timed/late eligibility and actual upward GO momentum. An isolated physical truck strike breaks tree roots and leaves the trunk fully fallen, with absolute up-dot below0.35. One controlled sheep detonation uproots the same tree exactly once and also makes it fully fall. The next round restores the pooled roots/body pose. These tree fixtures deliberately reposition test bodies and isolate hazards; they are not presented as natural match catches.

Other guards cover one explosion per multi-collider truck body, momentum through disablement, safe paused recovery behind progress and cheese, cooldown/separation, independent WASD/arrows, mushroom full/low/off and exact scene-exit pipeline restoration. A strengthened tree reset updates transform and Rigidbody together so interpolation cannot pull a new anchor against an old pose. Blast impulses wait until the destroyed root joint is actually gone; otherwise the joint absorbed the force. Pending events clear on reset.

## Visual and layout checks

[Views15:27 assertions](../../Documentation/TruckSports/CheeseEvidence/views-15.txt) pass. Native captures were inspected for one through four views at1280×720 and1920×1080. The compact Heat radial countdown, mint charge accent, condensed typography and score/status cards replace the rejected giant accelerator-instruction text. Gameplay displays no accelerator instruction; Help explains timing. Help pages, imported sheep, actual orange cat/wolf/tree/rock meshes and split HUDs were inspected.

Grass uses native instanced Terrain details with explicit InstanceCountMode. The previous nonzero map used sparse coverage semantics and did not produce visible tall patches. Actual engine instance transforms now verify scattered clumps (sample patch18 instances, scaleY2.411636, source mesh height≈0.64m); visible clumps appear in runtime views. Thirty seeded patches and three clear routes remain. The extreme close-up sample partly intersects a tree canopy, so it is not used as a polished presentation image.

## Complete real physical games and lifecycle

[Native physical run15:94 assertions](../../Documentation/TruckSports/CheeseEvidence/physical-match-15.txt) passes. A normal-speed sixteen-truck/four-camera autonomous match ends4–2–5–4 after15 genuine catches/rounds in531.9966 simulated seconds, zero neutral replays,218 natural truck-triggered sheep explosions and762 truck damage applications. Every participant scores; all weather presets and outside starter slots are exercised. No scores are injected, racers are not repositioned to manufacture catches, and the cheese is not steered toward a winner. Twelve background trucks physically participate without consuming scores.

The run then verifies rematch/progression, physics/weather/damage/clock pause, software shared-stick ownership, whole-controller conflict rejection, software disconnect, explicit replacement and neutral/fresh input policy. The solo challenge earns five genuine catches. All six older sports load/exit twice; collection returns and restoration pass without runtime errors/exceptions. Existing gameplay/scenes are unchanged; Cornhole's editor-only front upgrade preserves the seventh entry.

## Performance and preservation

The completed real-time run samples71,980 live frames across the four-player match, rematch/lifecycle segment and solo challenge: mean10.0065ms, p9518.7643ms, p9925.0083ms, maximum368.8503ms onGTX1660Ti. It includes2,975 frames with four cameras, hurricane weather and at least two simultaneous effects; maximum8 effects, gust5.141807. This is an aggregate mixed-view sample, not an isolated four-view percentile benchmark. Native screenshots and validation logging add overhead. The worst hitch is not isolated; uniformly smooth performance is not claimed. The environment snapshot shows only the owned validation player among Unity/TruckSports/OBS processes. No user testing builds were closed.

All1,536 baseline ProjectSettings/Packages hashes match after validation. Success and deliberate-failure title/build-list restoration pass. New source uses owned weather/rendering/profile assets; Taxi and vendor source are untouched. Build15 succeeds with0 errors/40 warnings (vendor/host deprecations and shader warnings); no Cheese source compile errors. Initial unused Weather Maker tessellated-water errors were fixed by excluding water references from the owned resource copy, without vendor edits. The builder now rejects any reported errors even if the overall result saysSucceeded.

Rejected/incomplete evidence is retained in local Logs/TruckSports/Cheese: pre-crash07 lacks completion; early hidden probes are physics-only; guards08/09 exposed fixture isolation issues; guards11/12 exposed reset/joint timing defects fixed before guards13. None are counted as final passes.

## Final normal build

Final16 succeeds with native exit0,0 errors/19 warnings,447606496 bytes, eight explicit scenes and TruckSports startup. All176 DLL/eight scene hashes match validation15 and all1536 settings/package hashes remain exact. Normal launch without probe flags shows Truck Sports and no startup errors; left available for human testing. See BUILD-INFO.md.

## Remaining human checks

Human handling/balance and timing feel; keyboard rollover; actual controllers, shared sticks, disconnect/reconnect and rumble; audio mix; comfort/readability of the mushroom look and extreme weather; representative slower hardware and the unisolated performance hitch. Full/low/off presentation options are available in Pause. No claimed hardware or human playtest replaces these checks.
