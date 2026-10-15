# Truck Sports expansion validation — 2026-10-15

The eleven-sport collection is implemented, saved, compiled and built for Windows x64. Normal-company build: `C:/Users/Jeremy/.codex/worktrees/truck-bowling-delivery/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Startup TruckSports; twelve explicit Sports scenes; Mono Development; Unity6000.4.10f1. Native build succeeded with0errors30warnings,430344831bytes,51.3696071seconds. All required player data, UnityPlayer, crash handler and Mono runtime files exist; PE machine8664 confirms x64. Exact host PlayerSettings/EditorBuildSettings hashes match before and after the scoped build, including injected-failure restoration.

## Changes and causes

Wallball: independent1–4 team sizes, eight participants, team eligibility/serve rotation, primary-interceptor/support AI. Its expanded ball-only oriented envelope requires fresh separation, closing motion and wall clearance; native truck-ball collisions are excluded so there is one response. Complete1v1/2v2/4v4/1v4 matches produced routine volleys without emergency replays.

Bowling: the sweeper was authored at lane height and only tweened in Z. It now parks overhead, waits for score lock, lifts survivors, lowers at the front, physically sweeps back, returns low, raises, replaces the rack/survivors, then starts the next delivery. Existing scoring, tenth-frame logic and momentum/AI tuning are preserved. Four complete simultaneous games scored71/93/91/124 with19/20/20/20 sweeps and every phase exercised; the live-bar-clear invariant held throughout.

Ice Hockey: queued symmetric responses use actual cached incoming velocities and contact normals. Strong responses replace the ordinary planar solver outcome once per fresh pair, with pileup averaging/caps and brief protection. Moving targets receive a strong launch; actual stopped defenders rebound the attacker. Initial physical measurements:11m/s attacker into2m/s target yielded target20.28m/s and8.57m travel in.55s; a stopped defender remained still while the attacker rebounded14.47m/s. Articulated puck-only kinematic blades use lag/preparation/follow-through, predicted board/shaft clearance, tuck/switch hysteresis and clear-distance rearming. A disabled-collider ClosestPoint query had returned the input point and prevented rearming; analytic box distance fixes it. Goal/board/corner, reverse/drift, gentle/equal/glancing/pileup checks and complete six-AI games pass.

Dock Diving:1–8 rotating competitors, three attempts plus tied-leader extra jumps. Actual wheel-driven entry speed/timing produces the leap; bounded aftertouch, single-body cab/chassis separation, first-water whole-inch measurement and persistent local solo records. Every attempt records mesh-only replay. Manual camera choice no longer gets overwritten by automatic cuts; human camera/rate ownership can hold playback until skip. Replay UI consumes pending gameplay action edges. UTS spectators, NWH water-plane geometry and Weather Maker normals/audio are reused.

Keepaway:2–4 free-for-all cumulative possession,30/60/90seconds. Fresh meaningful pre-impact challenges atomically transfer one ball, then wreck the former carrier with pooled Cheese effects/debris. Thief/blast/respawn protection prevents recursive steals; safe respawn and reachable-ball recovery preserve progress. Full four-AI matches complete at every target.

Curling:1–4 per team, sequential hot seat or separate sources,16 physical retained truck-stones per end,2/4/8ends, hammer/blank/extra ends. Identical circular footprints, house-distance scoring, side/back/hog rules and first-five opposing free-guard restoration. Actual draw and takeout contacts are physical; the guard-removal reproduction deliberately seeds a guard and delivery between turns, then uses real PhysX. No sweepers/no-tick/official clocks/burned-stone procedure; those arcade omissions are stated in Help.

Base Jumping: four equal-altitude/time/range releases, three rounds plus exact-tie extra rounds, freefall/progressive canopy/flare, one shared wind field and three Weather Maker presets. First ground contact locks millimetre error, millisecond time and pre-contact descent/attitude; no competitor collision or post-landing improvement. All three full four-AI weather matches had valid safe landings. Four software-source views and actual open canopies were inspected at720/1080. Compact colored HUD avoids the canopy center; scoped bloom is bounded after HDR truck lamps had obscured the view. Owned helicopter/parachute geometry is a first-pass presentation.

A shared mesh authoring helper had normalized children against a nonzero parent world position, leaving visuals offset from their authoritative bodies. Geometry is normalized before parenting; legacy Hockey visuals were centered from actual mesh bounds. Weather Maker’s default permanent setting left managers behind after Cheese/Base exits; owned scene instances are now nonpersistent with exit cleanup. The final normal-build22-transition suite has no errors and explicitly checks weather removal. No vendor source was edited.

## Evidence and limits

See [evidence index](ExpansionEvidence/README.md), result files and selected genuine screenshots. Runtime assertions: integration119; Dock68; Keepaway21; Curling14; Base44; extended Ice60; normal pointer/keyboard10; normal four-view13. Editor rule/source fixtures are separate. Existing-sport22 runtime assertions additionally cover full four-lane Bowling and Wallball team-size matches. These counts are suite assertions, not independent human playtests.

Observed development-player frame intervals on GTX1660Ti: a complete real-time six-AI Hockey match recorded8740 live frames, mean6.880345ms,p9512.48257,p9912.54043,max18.69337;3goals,69strong hits,0emergency faceoffs. A90second four-view snow sample recorded7918frames, mean11.36738ms,p9512.5454,p9918.76859,max106.2722. The Base sample used the pre-final HUD position, the same physics/meshes/weather, software controls and round breaks; final compact HUD rendering was checked separately. These are observed frame intervals, not GPU timings or a controlled hardware comparison, and do not imply hitch-free play.

No physical-controller, wheel, rumble, audio-listening or human gameplay acceptance was performed. Remaining checks: human handling/balance and readability/comfort; real whole/shared controller combinations and hardware reconnects; hot-seat ergonomics; sound/vibration levels; additional machines/display shapes. Prior sports retain their established tuning; old-sport navigation was rerun, while the new physical fixes/new events received the focused gameplay suites above.

## Git/dependency scope

Implementation commits e7e07e652d8263ca13884d33e5cc4d4ca9b224e1 and a53cc7ea8cbf47aa846d6934537fa9deab9258a9 are pushed to codex/truck-cheese-rolling. Prior completed Sports through40ec3fc was already pushed. Main F: Taxi checkout/source/index/Editor and user testing builds were preserved.

Dependency publication is complete following explicit human approval: commit `12f0653faae7290f0e6895097e9ff68ce06447b3` is pushed and directly verified. It includes exactly the approved 45 files. All 13 scenery hashes and original Git blobs were rechecked; active references resolve through tracked assets and declared URP 17.4.0 package content. The two documented unused legacy GUID fields remain unchanged. EXE and managed gameplay DLL hashes remain identical to the validated build. A fresh remote clone/import/build was not rerun. See [scope and evidence](ExpansionDependencyRepair.md).

The final runtime payload was copied to the main F: project and all 414 source payload files were SHA256-verified before promotion. Test captures were moved to ignored Logs. Current Sports source was safely synchronized there; no divergent root Sports edits were found. Temporary validation builds were removed; the open C: testing game was preserved.

## Vendor report

VENDOR ASSETS AUDITED: NWH Vehicle Physics2/WheelController/GR3D prefab and public APIs; Heat/TMP; Cinemachine; DOTween; Weather Maker; UTS people/animations; installed scenery and vehicle/art catalogs; project vendor-first inventory.

VENDOR ASSETS USED: NWH wheels/truck meshes/water plane and engine audio; Heat/uGUI/TMP; Cinemachine; DOTween pinsetter motion; Weather Maker weather/audio/normal/cloud texture; UTS spectator meshes/applause; existing Cheese effects/debris and original JC/Toon scenery dependencies.

RELEVANT ASSETS NOT USED: full road powertrain/traffic/GPS/streaming/career-save systems do not own these isolated arcade sport rules. Existing collection settings persistence is reused. No suitable installed helicopter/parachute model was found; owned geometry supplies that missing presentation. No new wheel-input backend, terrain generator, world weather engine or save framework was created.

CUSTOM SYSTEMS CREATED: sport rule/progression/AI owners, shared local-event adapters, bounded launch/flight/aftertouch, record/replay coordination, physical stick/impact semantics, source/modal lifecycle and native validation.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; vendor APIs/assets remain behind owned adapters.
