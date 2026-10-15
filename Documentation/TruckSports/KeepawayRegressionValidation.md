# Keepaway regression — evidence and current status

User report: attachment992e306e-69ce-46e0-bcfa-29e3d8de23e3,2026-10-15. AI inactivity in both views, inert cab wheel, enormous reversed opponent labels. Source fix and Editor acceptance complete; final current-revision combined Windows acceptance remains pending the outstanding expansion. No hardware or human-playtest claim.

## Reproduction and root causes

Native baseline six cases: Arena/First Person,0/1/2 explicit humans with remaining slots AI. Actual AI intent reached NWH motors, bodies were dynamic and grounded, brakes were not permanently applied, and AI never required a human device. Nevertheless the trucks milled near the perimeter: paths roughly6–14m for many AI in12s, zero pickups or steals in every baseline case. The camera did not gate the shared simulation.

The regression range includes7ad137dc, which introduced connected-grid routing and modeled escape ramps. Ramp MeshColliders were nonconvex while avoidance and clear-ball checks called Collider.ClosestPoint, an unsupported combination that emitted repeated PhysX warnings. Isolating a convex wedge-collider correction substantially restored movement, but still produced no pickup in the12s isolated case. The remaining follower aimed at only the next2m grid cell, inside the NWH truck's turning circle; obstacle repulsion could overwhelm that short target, causing repeated reverse/edge loops. The grid also allowed row-wrap neighbor links, used stale transformed obstacle bounds when generating connectivity, and planner state was not reset consistently across rematch/respawn/ownership changes.

An additional concrete lifecycle defect left the visual child hidden after a wreck when rematching: the root and physics were reactivated while the child mesh stayed inactive. That is fixed explicitly in Keepaway BeginGame.

## Fix

Keepaway-only convex wedge colliders preserve the actual closed ramp geometry and support exact closest-point queries. A safe bounds fallback prevents unsupported queries if older nonconvex data is encountered. Native authoring and saved-scene validation enforce the corrected data. Connectivity synchronizes transforms, uses valid four-neighbor adjacency, includes ramp clearance, and keeps authored obstacle/hazard features active. The follower string-pulls clear segments of the valid path instead of targeting every adjacent cell. Local repulsion is bounded relative to the route. It uses the same NWH controls, torque, braking and speed limits; no AI teleport, direct translation, hidden force or collision disable was added.

A physical progress window distinguishes motor intent from intentional braking. Once bounded reverse starts, it executes before further stall/replan checks, so the timer cannot repeatedly restart. Spawn/rematch reset planner state; possession changes immediately invalidate stale targeting. Existing out-of-bounds recovery remains authoritative. Possession, protection, stealing and scoring rules remain unchanged.

The copied Taxi cockpit originally contained render meshes without Taxi's animation behavior. The existing SportsCabSteering adapter reuses Taxi's signed-angle/rate-limited motion, binds each cab to its actual owning NWH truck, preserves the neutral transform and rotates only the real steering-wheel mesh. Earlier source repair was not yet in the existing executable. Both saved Keepaway scenes now validate these bindings. No competing Animator was added.

Shared world-size TextMesh labels were unsuitable near the windshield and across competing camera rotations. World text rendering is disabled. Sixteen pooled uGUI/TMP plates project independent labels into each camera viewport, with fixed bounded pixel size, name/color, distance/near limits, obstacle occlusion, overlap spacing, HUD exclusion and local-cab self-label hiding. No shared billboard rotation is required. Captions refresh on a bounded cadence; UI objects are reused. Single-line/ellipsis behavior handles exceptionally wide names.

## Actual validation

Permanent development flag -tsKeepawayRegression (Editor SessionState LWS.KeepawayRegression). Latest completed native suite107 passing assertions,0errors, before adding the already separately validated blocked-recovery fixture to permanent coverage and final single-line label authoring. Six mixed roster/view cases demonstrate meaningful displacement, intent and delivered motor torque for each AI; pause/resume and rematch preserve state and restore visible dynamic trucks. Normal Lively hazards, six solid props, sixteen sheep slots and all four ground themes remain present.

Arena AI-only normal60s win: winner0; possession60/36.04063/24.72038/58.46115;19pickups,34steals,87blasts. First-Person variant AI-only normal60s win: winner1; possession21.3603/60/55.94109/25.34039;13pickups,24steals,57blasts. These matches ran actual NWH/PhysX with mixed Easy/Normal/Hard agents and rain/snow. They required no assigned human devices. AI-only cab mode intentionally uses its existing broadcast spectator camera.

Controlled initial-pose physical reproductions verify each AI slot independently acquires a loose ball and every slot steals with a genuine incoming truck impact. Contact debounce, explosion-before-respawn lifecycle, one loose-ball hazard drop and exact cumulative win conditions pass. No score was injected. A separate real U-shaped solid-wall fixture verifies uninterrupted bounded reverse1.119995s and2.302649m displacement; test-only temporary protection isolates sheep blasts. That fixture is now included in permanent regression coverage.

Four actual cab wheels follow NWH steering at approximately-450/-148.5/225/450 degrees; pause freeze and neutral return pass. Every viewport actually displays opponent plates, hides the local plate, fits bounded rectangles and has no TMP overflow for the test names. Actual saved-menu return and autonomous rematch pass. Camera-only720/1080 four-cab captures inspected: these omit screen-space overlays, so projected-label evidence is the explicit live viewport/TMP checks until Windows full-window capture. Hardware feel, human playtesting and subjective audio remain manual.

Evidence (ignored logs): Logs/TruckSports/SevenSports/Keepaway-{baseline,convex-only,route-fixed}-result.txt; Logs/TruckSports/TerrierUpdate/KeepawayRegressionEvidence/result.txt and four-cameras-1280/1920.png; Logs/TruckSports/SevenSports/Keepaway-recovery-result.txt. Initial acceptance was interrupted by a confirmed Windows reboot at18:47:42EDT; its all-zero progress file is not a pass. Recovery audited1298owned text files and regenerated only one corrupt untracked test .meta. Subsequent completed acceptance above is valid. Git saved commits passed connectivity checks.

## Delivery and vendor audit

Final executable remains F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe with all companions. Existing executable is older. Final build uses SportsBuilder.Build and explicit sports scenes with host-setting restoration after the remaining four new sports complete. No intermediate output folders. Current completed expansion milestones are pushed to codex/truck-sports-seven-events; final reviewed main delivery is pending. Never push private Taxi3651e3ee ancestry.

Vendor assets audited/used: NWH wheels/vehicle control APIs, actual GR3D/Taxi cab and steering presentation, Unity PhysX, Heat/uGUI/TMP, Weather Maker, installed ramp/prop/sheep art. No vendor source modified. Custom scope is Keepaway navigation/lifecycle coordination, projected identity presentation and acceptance coverage. No duplicate vehicle physics, weather, input backend or UI framework was created.
