# Truck Taxi dual-world checkpoint

## Final state
Actual Windows build DELIVERED; automated core acceptance PASSED. Human full-route/service/exhaustive visual acceptance and isolated performance remain explicitly unverified/blocked, not passed.
Executable: F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckTaxiDualWorld/TruckTaxi.exe.
Unity6000.4.10f1 Windows64 Mono/D3D11 development; actual BuildReport PASS2026-10-15T01:45:06.8834840Z. Binary source9bbac8e9. Full report: Delivery.md; raw evidence/screenshots: Evidence/.
Authoritative request: C:/Users/Jeremy/.codex/attachments/f3d55121-e14a-487b-a5d8-990f8bc2d3a7/Pasted text.txt.
Scenes: Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity and TruckTaxi_Edenborough.unity, with existing main menu/chunks/venues. Existing maps NOT regenerated.

## One compact requirement / acceptance checklist
| Requirement | Status | Evidence / limit |
|---|---|---|
| Existing maps/saves/vendors/unrelated work preserved | Verified complete | Two connector additions only; baseline Temp/TruckTaxiDualWorld/Baseline; all30,924 original Eden IDs retained; no real save touched |
| New-career choice of either map | Implemented but human UI unverified | Main Menu->New Game->Vibin'->Demo/Eden buttons+descriptions; not a claim of observed human menu navigation |
| Continue correct map / legacy compatibility | Verified complete automated core | Final actual separate Eden and Demo processes exit0; actual pre-feature save final Continue exit0 |
| Pause menu travel / cancel / keyboard/gamepad | Verified complete automated core | Actual Heat submit and virtual gamepad South/East, Escape cancellation; mouse/hardware acceptance not run |
| Physical exits / connected roads / signs / GPS POIs | Verified complete automated core | Real NWH input/contact both directions on final binary; narrow EasyRoads spurs, own atlases, local planner route; full-route/sign reading human test not run |
| Async loading / safe world-origin-ground-player order / rollback | Verified complete automated core | Genuine scene progress; safe headings and30m arrival exclusion; deliberate missing-arrival source rollback/native drive passed |
| Shared career/truck/paint/upgrades/needs/inventory/damage/fuel/calendar | Verified complete automated core | Same profile/name/wallet/9snacks/progression/2fares/bluepaint/brakeupgrade/.87body/fuel and nondecreasing calendar across every arrival and fresh processes |
| Bounded distinct regional pose/weather/snow/events/work area | Verified complete automated weather/snow core | Max2 visits; own forecast restored, manual rain catchup, snowfall/melt elapsed consistently; supported events captured, not all individual props |
| Unsafe fare/race/companion/private/recovery/towing/modal/nozzle restrictions | Implemented, partially verified | Existing gates retained, safe on-foot departure/rollback passed; exhaustive live unsafe fixtures not run |
| Trailer safety | Implemented, runtime unverified | Existing attached trailer guard BLOCKS with detach explanation; no unsupported transport/loss claimed |
| Current graph/atlas/POIs/dispatch/traffic/pedestrian lifecycle | Verified route/ownership core | One scene loaded, current GPS after each transfer, bounded local population ownership; every junction/service/dispatch not human-tested |
| Outdoor terrain/ground/foliage/roof/prop snow | Implemented, representative visuals verified | Actual clear/heavy terrain/branch/roof screenshots; original wind/cutout/lighting preserved, unknown shaders not silently rewritten; exhaustive material review not run |
| Glass/interior/player/HUD exclusion | Verified source/test core | Reviewed shader/material/hierarchy exclusions; exhaustive human interior review not run |
| Gradual accumulation / warm-rain melt | Verified complete automated core | Both final map depths increase then decrease gradually, no instantaneous clear |
| Actual-wheel curved nonroad tracks / bounded history / no dry stamps | Verified core and representative Windows visual | Final native long curves, four contacts each, actual GPU indents and visibly paired grooves on BOTH maps; GPU retention8s/callback checks; no night acceptance claim |
| A Demo->Eden physical +drive | PASS automated core FINAL binary | Actual tractor contact, grounded arrival, native throttle/brake/steering, current route |
| B Eden->Demo physical/no retrigger | PASS automated core FINAL binary | Actual tractor contact; safe return pose, no immediate re-entry |
| C menu both ways/gamepad/cancel | PASS automated core FINAL binary | Actual Heat submit, virtual pad and cancel assertions |
| D manual/legacy/save-quit-relaunch both maps | PASS automated core | Final seed+Eden Continue+Demo Continue+legacy each exit0. Prior306cfecb manual/rollback exit0 has identical persistence code; only later ground shading/probe length changed |
| E regional climate/full snow/curved tracks | PASS core/representative visual FINAL binary | Both maps clear/light/heavy/live melt, long realwheel paths/GPU, own weather through process restart |
| F >=10 transfers/input/GPS/objects/save | PASS automated core FINAL binary | Ten transfers plus11th checkpoint; each drive/career/localroute/oneowner check passes; finite samples not zero-leak proof |
| Isolated before/after and dense-urban/vegetation driving performance | Blocked / not run | User TruckSports PID28400/33688 remained active on same GPU; do NOT stop them. Non-isolated stationary metrics retained |
| Actual final executable/screenshots/test evidence/local commits | Verified complete | Build exists and run; no required own player/test session left running; source committed, handoff/evidence local commit only; NO PUSH |

## Exact final Windows evidence
Fresh disposable root b59ee4f0674944458235d7389e170b11:
- Final seed PID38668 exit0: both physical exits, actual menu/gamepad/cancel, TEN transfers, native drive after each, then11th transfer to snowy Eden save/quit. Evidence/Windows-final-seed.log.
- Fresh Eden Continue PID37088 exit0: own heavy snow/career restored, native drive4.106m, menu return Demo, own Rain forecast completes automatically and elapsed melt validated, save/quit. Evidence/Windows-final-Continue-Eden.log.
- Fresh Demo Continue PID11692 exit0: own Rain/melt/career, native drive3.868m, no debug intervention. Evidence/Windows-final-Continue-Demo.log.
- Legacy root6506dd9db338435896dfaa87e2727dff seeded with actual old DLL2026-10-12; FINAL binary Continue PID26312 exit0: exact profile/pose/needs/fuel/damage/weather/eight-seat/control-restoration assertions. Evidence/Windows-legacy-final-build.log. Do not claim native drive if old fixture engineoff.
- Prior306cfecb root57e manual exit0: real Manual1 load/native drive, safe on-foot rejected destination rollback/native drive, both actual offroad curves and snowy Eden saves. Evidence/Windows-57e-manual-final.log. Real saves never used.
- Final native Demo offroad15.958m,4 contacts/742 stamps; GPU1809marked/1684indented. Eden4 contacts/649stamps; GPU1362/1293. Actual representative groove images visually reviewed in both maps.
- No exceptions/errors found in final seed+Continue logs; no shader errors in successful final build. Deliberate earlier failures are retained with explicit failure names, not counted as passing.

## Final tests / measurements
243 unique EditMode PASS: world173/calendar12/snow22/occupancy24/MainMenu2/garage10. Subsets not counted twice; summary Evidence/Test-summary.json. GPU Demo final40.23s PASS; Eden elevated42.45s PASS, including actual raycast replay, vendor reset, stale elevated anchor, callback and8s retention. GPU test alone is not physical driving proof.
Final stationary regional-exit1920x1080 GTX1660Ti, live NPCs, frozen clock only for controlled snow; NON-ISOLATED median/P95/P99 ms:
Demo clear12.503/18.766/25.002, light18.745/18.841/25.030, heavy18.827/25.107/31.226.
Eden clear12.507/18.807/18.883, light18.758/25.055/25.145, heavy24.993/25.128/31.375.
No locked60FPS or isolated before/after or dense-city driving claim. Final ten-transfer object counts Demo8544stable; Eden46464->46459. Allocated bytes Demo369435901->370593498; Eden530928602->533247923 (warm caches/finite samples, NOT zero-leak proof).

## Fixes completed / boundaries
Existing Pixel Crushers authority and generic coordinator retained. Same existing Taxi prefab/canonical ID/reviewed paint material; exact eight-seat cab contract preserved. Local graph-spur subdivision fixes planner aliasing. Weather Maker initialized before Weatherade depth cameras. SRS renderer/RTHandle rebinding; three-texel contact-depth support; availability refresh preserves history; thresholded public follow repair after vendor UpdateDepth stops its coroutine. Saved unfinished weather blends resume persisted forecast intent; manual rain melt ignores stale FROM preset. Project flat-ground shader uses Weatherade RG groove normals/A compression, no second trace system.
No independent serializer/storage/slot database; no vendor source changes BY THIS TASK. Other pre-existing vendor dirty changes are untouched. Attached trailer transfer remains explicitly blocked.

## Repository and recovery notes
Branch codex/truck-sports-3v3. Preserve ALL unrelated dirty files, especially Demo updateQueue344874416->67430938 remains UNSTAGED. Baseline in Temp/TruckTaxiDualWorld/Baseline. NO PUSH.
Local implementation commits:8b23f96f,7cb9baf9,ed6b39e4,6c19ae1a,89a431ec,9aa4de5d,c908cf7b,cad0f2eb,cd0d9faa,efc622fa,fd321e0b,60ab13bc,404d95e9,e3e85a0f,660f72bb,306cfecb,9bbac8e9. Final handoff/evidence commit can be read from git log.
No further source edits required by current evidence. Remaining work is human acceptance and isolated performance when other games are closed, NOT another implementation restart.
Unity CLI C:/Users/Jeremy/AppData/Local/Unity/bin/unity.exe; EditorPID5804/Pipeline7800. Use require_escalated for CLI/fresh logs; sandbox can show stale views. Explicit recompile then recompile_status. run_tests single substring filter; OR selected0 and is not a pass. Build existing Research/TruckTaxiDualWorld/BuildWindows.cs via eval_file --file;5s CLI timeout does not stop build, durable Temp/TruckTaxiDualWorld/build-result.txt controls. Never queue delayCall builds or build over a running player. No raw Unity YAML edits.
RunWindows.ps1 -Mode seed/continue/manual -Run32hexGUID; fresh seed needs fresh root; preserve Continue log before second process overwrites same filename. Test roots only under AppData/Local/Temp/Jeremy Bregermann/Interstate Hauler/TruckTaxiRestartTests. No real saves.
Only helper Curie01a13c6b-4899-7100-8373-2cc7d751ffd1 is idle/done; no nested delegation. All required own Windows exec sessions collected.
