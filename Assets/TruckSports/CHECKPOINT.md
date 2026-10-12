# Truck Sports checkpoint — 2026-10-12

Complete collection inside Interstate: Hauler: saved Truck Sports front end, retained Truck Billiards, and complete simultaneous 1v1 Truck Hockey. New owned content is under Assets/TruckSports and Assets/TruckHockey; existing Billiards has only narrow collection-entry/return hooks plus documentation. Its scene/rules/series/Practice/difficulty/physics/feedback and standalone builder are retained. Existing worktree changes were preserved.

- Title, sport cards, How to Play, settings and device-friendly navigation are wired in saved scenes.
- Hockey AI/shared-keyboard/two-pad/mixed modes include default names, distinct colors, difficulty, 2/3/5-minute regulation, strict join/disconnect/replacement and fresh-input gating. Solo/two-pad matches require no keyboard.
- Flat bounded puck and upright trucks use one PhysX contact model. Rounded rink/goals, truck-only net barriers and camera are complete.
- One Hockey rules authority handles full-mouth goals/own goals/deadline timing, fair resets, live-only clock, pause, overtime, results, preserved/swapped rematch and confirmations.
- Common-controller AI attacks/defends physically; recovery resets only the requesting truck. Actual pin/outside recovery is announced and preserves score/time; accessible stationary puck remains live. Truck teleports clear stale contact records.
- Shared preference adapters, restrained audio/haptic requests, DOTween celebrations and balanced scene lifecycle are complete. Heat navigation public fields prevent its deferred initialization from replacing active links.
- Final Windows x64 development build: Logs/TruckSports/build-11.log, zero errors / 25 existing vendor warnings. Explicit front/Billiards/Hockey scene list and scoped Truck Sports product title. Exact host settings restore on success and deliberate build exception. No vendor source, package, host physics/input setting or build-profile edits.
- Final executable: 161 collection checks at 1280x720 and 161 at 1920x1080, retained Billiards feature/local suites 92/58; all exit 0, no captured runtime errors. Saved-scene fixtures pass 20/44/72.

Executable: F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckSports\TruckSports.exe. Keep all generated Data/Mono/D3D12/DLL companions and development files together. Payload: 368 files, 293,224,436 bytes; build-manifest.csv records hashes. See VALIDATION.md for evidence, performance, fixture boundaries, repairs and limitations.

Continue from saved scenes/code without rebuilding Billiards authoring. Collection build command: LWS.TruckSports.Editor.SportsBuilder.Build. Author front/Hockey then build: CreateValidateBuild. Existing Billiards-only command: LWS.TruckBilliards.Editor.BilliardsBuilder.Build. Explicit scene arrays leave the host scene list intact.

Normal player is open as PID 7976, with exact executable path and Windows title Truck Sports verified via Computer Use. Native observation earlier timed out at app approval, then frame capture after fresh selection; no native gameplay input was sent. See Logs/TruckSports/native-smoke-11.txt and native-player-11.log.

Remaining human playtest: native keyboard/mouse/shared-keyboard full matches; physical solo/two/mixed controllers with unplug/reassign/rumble; listening/mutes; display settings/other aspect ratios; unassisted 2/3/5-minute matches and long-play AI balance. Software scoring tests require actual physical puck crossings, never score-event injection. No physical-controller or unassisted full-match pass is claimed. Join/Disconnect label rebuilding is a remaining avoidable menu allocation cost; sustained performance/memory soak is untested.

Vendor audit/use/reasons: README.md. VENDOR SOURCE MODIFIED: NO. DUPLICATE VENDOR FUNCTIONALITY CREATED: NO. No unresolved gameplay defect is known from the final successful software runs.