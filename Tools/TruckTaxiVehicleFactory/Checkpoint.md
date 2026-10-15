# Truck Taxi Vehicle Factory Checkpoint

Original request: attachment `3c3484b5-e44b-43d6-817d-30e3ac3e2b8d/Pasted text.txt`.
Project: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator`.
Scope unchanged: six real NWH trucks, five Blender designs, three functional sleepers, dealership in both maps, existing Career/paint/persistence, actual Windows build and acceptance. LOCAL commits only. DO NOT PUSH.

## Current Action
FINAL BUILD DELIVERED: Builds/TruckTaxiVehicleFactory/TruckTaxi.exe, PASS2026-10-15T15:17:21Z. ALL12 fresh Windows process sessions PASS (six seed/quit/relaunch/Continue pairs) against this GPS-corrected binary. Existing real GPS route, native throttle/brake/steering, dealer UI, paint and ownership verified. Actual cockpit/dealer captures reviewed. Integrated PlayMode3/3 PASS84.54s; last screen orientation/exact starter restoration/model-region transfer rerun1/1 PASS34.40s. Career EditMode82/82 PASS. Art commit38d8848f; integration78fab8de; final evidence/report committed as "Record six-truck Windows restart validation renders and delivery limits". No pending human approval. Full ordinary-fare/manual/highway/night/seating/snow-appearance acceptance remains unverified, not falsely marked complete.

Own root Editor PID28376 gracefully CLOSED after verifying no dirty scenes, no Play Mode/compilation. No own Windows test processes remain. Unity6000.4.10f1; former Pipeline7800. Log: local Research/TruckTaxiVehicleFactory/Unity-Editor-Teardown.log. No duplicate Editor launch needed.

Sports previously FULL RELEASED the root for this integration. Taxi is returning FULL RELEASE immediately after this final checkpoint commit: own Editor closed, tests exited, no more root source/asset/build/index writes scheduled. Sports uses separate C: truck-bowling-delivery worktree; DO NOT touch it. All helpers closed; no concurrent writers. Failed startup32968 was closed under human approval. No approval currently blocks this task.

## One Acceptance Checklist
| Original requirement | Current status | Evidence / remaining check |
|---|---|---|
| Recover baseline, vendor source/license, actual proportions | Verified complete | GR3D010416TRUK_HD.fbx, importer0.8, Blender3.5.1, untouched local reference retained. |
| Starter preserved and free/owned | Verified complete | Existing mesh/controller retained; canonical ID ih.truck.nwh.euro.semi; explicit known legacy alias migration tested. |
| Highway King classic sleeper | Verified core runtime and visual | .blend/body+2LOD/real NWH prefab; final Windows purchase/drive/GPS/restart PASS; actual cockpit/dealer reviewed. |
| Nightrunner aero sleeper | Implemented; core runtime verified | Same evidence; distinct curved raised roof/body. |
| Outlaw custom daycab | Implemented; core runtime verified | Same evidence; tall stacks/bullbar/rally lamps. |
| Thunderhaul performance daycab | Implemented; core runtime verified | Same evidence; lowered roof/hood/vents/aero treatment. |
| Road Palace luxury sleeper | Implemented; core runtime verified | Same evidence; larger berth/cabinets/fridge/light; actual longer wheelbase/hitch. |
| Actual NWH wheels/axles/colliders/stability/transmission | Core verified; handling comparison unverified | All6 Windows native throttle/brake/steering; one canonical actor/provider. No custom physics controller. Full back-to-back highway feel still requires observed driving. |
| Actual trailer coupling/uncoupling | Verified automated physics | All6 native ConfigurableJoint connected bodies, real adapter states,3s velocity bounds, detach and switching guard PASS. Not a human-driven approach/coupling claim. |
| Headlights/brake/horn/radio/fuel/damage/repair/GPS | Implemented; core runtime verified | Final Windows native headlight/horn semantic commands and actual regional road-graph/Compass route PASS for6models. No audible-horn/night-beam visual claim. Existing fuel/damage/radio retained, not fully regression-tested. |
| Cameras, dashboard, mirror and eye/accessory placement | Implemented; cockpit/GPS verified | Actual captures exposed native cached camera reset and legacy GPS reparent. Removable parent camera mount and SetVehicleDashboardMount fixed these. Eye(-.56,2.20,.62), GPS(-.08,2.18,1.35). Actual Compass parent, viewport, level and exact starter restore tested; physical map visible in Windows. Mirror scene observation limited. |
| Real sleeper interiors/rest/time/fatigue/access | Verified PlayMode | Three modeled interiors observed; existing WorldLife/CabRest/DriverNeeds advances7200s and reduces fatigue. Return-to-truck stale flag fixed. Normal passenger+companion visual occupancy still unverified. |
| Paint HSV/presets/preview/apply/cancel/default | Implemented; core verified | Exact declared paint materials, existing GarageDraft. Per-model paint Windows restart PASS. Final UI/input visual pass pending. |
| Real dealership both maps, catalog and gamepad focus | Implemented; partially verified | Demo facility runtime, Eden existing garage binding, one HUD modal/Heat UI, pure-visual preview. Demo screenshots reviewed, HUD bleed/caption gap fixed. Eden facility exists in passing transfer. Complete physical access/gamepad walkthrough unverified. |
| Prices based on existing fare economy and exact-once wallet | Partly verified | Prices600/900/1250/1700/2400 dollars; actual fare config500c+1.5c/m+2c/s. Purchase affordability/idempotency/charge tests and Windows PASS. Normal-fare income balance and each-model ordinary ride acceptance unverified. |
| Safe switching and ownership/compatible upgrades | Core verified | Existing atomic Career transaction, rollback/failclosed tests, active-fare/race/interaction/companion/trailer guards. No duplicate player/input/GPS. |
| Pixel Crushers selected/paint/career save/restart | Verified final Windows |12 final real process sessions PASS, model and paint restored then immediate native driving. Earlier12 retained separately. Real saves untouched. |
| Cross-region selected model/ownership/wallet/paint | Verified final PlayMode | RoadPalace with6 owned entries transfers Demo->Eden, same wallet/selected physical model, one truck, paint bound. No new Windows cross-region claim. |
| Mesh/material/UV/normals/LOD/collider organization | Verified assets |15/15 FBX round trips PASS, no vendor ref exported; distant interiors omitted. Geometry5/5 trailer-fit calculations not substituted for actual coupling. |
| All exterior/cockpit/sleeper render review | Blender and selected gameplay captures reviewed |27 Blender views retained in Tools/TruckTaxiVehicleFactory/ReviewRenders;24 final Windows images plus3 Unity sleeper captures retained. No AAA-art or human approval claim. |
| Dual worlds/travel/weather/full snow preserved | Implemented; regression limited | No world regeneration or weather/snow replacement. Existing wheel traction/trace identity retained; MarkDirty does not clear traces. New body roof/hood snow appearance not visually validated. |
| Performance bounded/no preview physics/controllers | Implementation and bounded samples verified | LOD0~62-86k triangles, two distant LODs. Pure mesh preview, one explicit-render camera/RT, no listeners/controllers/physics. Final shared-GPU stationary median~18.73ms, p95~25ms exceptPalace31.25ms. NOT isolated driving benchmarks. |
| Full each-model Windows acceptance13 steps | Partially verified; manual checks not run | Final12 seed/continue sessions and expanded dealer/cockpit/GPS/headlight/horn semantics passed. Every-model ordinary driven fare, final gamepad use, companion seating, highway comparison and night/audio observations remain NOT RUN. |
| Fresh final build/local commits/report | Delivered | Actual final GPS-corrected binary PASS15:17:21Z and12/12 Windows sessions. Art38d8848f, integration78fab8de, final evidence/report commit at HEAD. Delivery report documents exact artifacts/evidence/limitations. NO PUSH. |

## Evidence And Debugging
- Current binary: Builds/TruckTaxiVehicleFactory/TruckTaxi.exe, final GPS-corrected build15:17:21Z. SHA256 of actual runtime assembly recorded in Evidence/FinalBuildArtifact.json.
- Evidence: Tools/TruckTaxiVehicleFactory/Evidence/WindowsRosterSessions.json +12 extracts; actual logs in local Research. Six GUID-isolated Pixel Crushers directories only.
- Final EditMode82/82: Evidence/EditMode-Final-82.json. Integrated3/3: Evidence/PlayMode-ActualGps-3.json. Last orientation/starter-restore/model-transfer1/1: Evidence/PlayMode-FinalGpsRestore.json. Final Windows12/12: Evidence/WindowsFinalSessions.json; exact frame samples in WindowsFinalPerformance.json.
- Native coupling1/1 across6 in33.52s: Evidence/PlayMode-NativeCoupling.json. Preserve failed attempts, do not label them PASS.
- Physical fixture initially wrongly snapped an unsynchronized trailer10m during joint creation. Pre-align uncoupled kingpin + Physics.SyncTransforms; NWH still owns actual joint. No breakforce or stability weakening.
- Blank unbaked source ReflectionProbe on the disposable trailer caused URP null probe/culling errors during teardown. Physics fixture excludes ONLY baked probes with no texture, deactivates before Destroy. Production/vendor probes unchanged; no log suppression. Evidence/PlayMode-RenderGraphFailure.json retained. Restart alone did not solve it.
- Native CameraMouseDrag caches local position. Move a removable TT_Authored_CockpitMount parent, preserve original camera/local pose; detach camera before removing generated mounts. Starter appearance/pose restored.
- No vendor source changed by this task. Pre-existing dirty vendor/user/dependency changes retained; do not stage them.

## Working Rules / Exact Next Step
Read actual files, not stale PreparedRuntime copies. Factory/Rig include former helper lifecycle/rollback/material/LOD fixes. All current source is in the root; local PreparedRuntime is obsolete.

Use native codex.exe --codex-run-as-apply-patch with locally prepared .patch files for F: manual edits; ordinary sandbox apply_patch cannot write F: reliably. Use Unity APIs for serialized assets. CLI main-thread5s timeout is NOT authoring/build completion: inspect durable Tools/TruckTaxiVehicleFactory/Authoring-result.txt and Build-result.txt.

Next step is NOT rebuilding/reimplementing. Deliver the actual Windows player and report/evidence. Full manual fare/handling/seating/night/snow-appearance acceptance is recorded as NOT RUN. Local integration is committed. After evidence/report commit, send Sports explicit root FULL RELEASE (own Editor closed and index work finished). No push. Unrelated existing dirty files remain preserved and unstaged.
