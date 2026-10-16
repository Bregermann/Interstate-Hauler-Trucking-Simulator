# Actual Windows milestone — October16,2026

Included in the25-sport combined Windows build. Actual visible player SumoFocused passed0errors. See [collection validation](../TruckSports/VALIDATION.md) for exact build hashes, measured results, version boundaries and remaining human checks. Earlier statements below saying the executable contains19sports or Windows acceptance is pending are superseded by this section.

## Earlier evidence (historical)

# Sumo validation

Native rule fixtures passed: batched final draw/no point, one-survivor awards, duplicate-finalization rejection, first-to-five lock, timeout draw, inclusive body-center boundary and airborne-inside eligibility. The saved scene validator checks four equal-mass NWH competitors, actual ring MeshCollider, five recorded render sources, complete audio/outfit/brow/tail references and no cosmetic colliders.

Two complete native four-AI PhysX matches passed with zero errors. Initial: ten heats, four draws, wins0/5/0/1,168 qualifying contact observations. After allowing stationary AI shoves and finishing own-surge limits/presentation: sixteen heats, three draws, wins3/2/3/5,280 contact observations. Both actually drove, shoved, shrank, recorded decisive replays and reached five wins; no results were injected. Rematch, paused truck/clock and collection-return listener cleanup passed. These are physical automated matches, not human balance evidence.

Focused acceptance ERRORS0: software left/right stick/trigger ownership and independent keyboard clusters; whole-pad and duplicate-stick rejection; actual driven impact moves a stopped opponent; one trigger surge and held-contact deduplication; wheel overhang legal; airborne center inside legal; actual center crossing eliminates and disables competitive collisions; controlled real glancing contact transfers motion/rotation (observed angular speed.0603rad/s, incoming normal speed8.5m/s); brow pose/priority hold; fitted outfit render references with no colliders; real collider/visual/legal shrink synchronization; pause freezes body/clock/cooldown; virtual disconnect/replacement/neutral gate; fresh input owner; rematches and front-end cleanup.

Native camera-only arena captures at1280x720,1920x1080,1440x900,1280x960 and2560x1080, plus a1200x900 close view, were inspected. Four long-name score/cooldown rows have actual font/glyphs and fit their rectangles. Images show the modeled brow/headband/wrap and ring occupying the useful viewport. Screen-space full-window Windows UI remains a separate final acceptance.

A60real-second four-truck batch workload passed with206252frames, mean.292ms,p95.384ms,p99.770ms,GC0=0,managed+14262272bytes. Automatic renders=0 in batch: this is NOT player/GPU performance evidence. Final -tsSumoFocused in the development Windows build must include actual automatic arena renders. -tsSumo runs a full first-to-five autonomous match.

Evidence: Logs/TruckSports/TerrierUpdate/SumoEvidence, SumoFocusedEvidence and project-local native preview images. Final combined Windows acceptance still pending. Remaining human checks: shove feel and recovery, outfit/expression readability at the actual display size, audio mix, physical controller ergonomics and long-session fun. Injected devices are not real hardware validation.


Final full replay/cleanup acceptance:14heats,4draws,wins0/5/2/3,240contacts, ERRORS0. Actual angle/slow-speed/rewind/skip controls and the final winning brow pose in recorded child transforms passed. A prior25heat run exposed a destroyed tire AudioSource during scene teardown; the archived failure is retained under Logs/TruckSports/SevenSports/SumoCleanupFailure. Sumo now uses Unity destroyed-object checks rather than null-conditional calls and guards destroyed fighters. The final test deliberately destroyed one optional tire source before teardown, then completed the real menu return cleanly. No gameplay result was injected to bypass the rerun.

Native24-scene audit passed, including an actual solid upward-facing ring raycast after explicit edit-time Physics.SyncTransforms, missing-script/listener checks and exact build-failure host-setting restoration. The initial unsynchronized edit-time ray failed; synchronized native diagnostics returned top normals(0,1,0) at four sample positions, rather than changing working gameplay physics.
