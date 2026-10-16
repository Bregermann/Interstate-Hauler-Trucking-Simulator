# Target Launch validation — October16,2026

## Native Unity evidence

Unity6000.4.10f1 compiled the implementation without new compilation errors.14 deterministic rule assertions passed: all zone edges, moving center, top-only rejection, once-only award, equal round completion, five launches, tied-leader extras, result lock and solo total.

Three complete four-AI physical matches passed0errors after the course/AI corrections:

| Difficulty | Rounds | Totals by slot | Air contacts | Dive observed |
|---|---:|---|---:|---|
| Easy |5|175 /85 /225 /125|3|Yes|
| Normal |5|275 /175 /200 /150|4|Yes|
| Hard |5|325 /175 /200 /225|6|Yes|

Actual NWH approaches produced real ramp departures, high-value/bullseye landings, airborne contacts, misses, dives, actual shared replay and complete results. These samples establish functional difficulty variation, not a statistically established score distribution.

Root-cause work: native math reproduced a false13° SignedAngle heading from ramp pitch despite zero yaw; projecting the forward vector onto the horizontal plane removed it. A suspension hop atz67 was also incorrectly disabling power halfway up the lip; final flight now waits past the real72m ramp end. Six actual software-keyboard straight-drive/no-aftertouch trials measured natural range and an actual mid-flight pause; this exposed the original142m target center as beyond most controlled approaches. Default target center is now130m. AI aims from its existing lane toward the distant chosen location instead of merging all trucks early. No added launch velocity was used.

The first captured chase view cropped the target and excluded the truck: Cinemachine's initial eye was inside its target group. Providing a conservative outside-group initial offset restored complete group composition. Native captures were inspected. Final saved scenery uses installed3D rock clusters and modeled gantries; target numbers are persistent baked TMP glyph meshes, including in replay.

Focused native acceptance passed0errors: actual software-keyboard ramp flight; held-throttle range gate; fresh finite aftertouch; one dive and heavier descent; actual mid-flight pause; no post-contact propulsion/rescore; physical dropped-body10/25/50/100 contact fixtures; persistent markers; actual post-score rebounds;1500m/s downward contact fixture; render-only recorded replay/three angle and playback controls/rewind/skip/paint/glyphs; shared sticks/triggers with duplicate and whole-pad conflict rejection; keyboard independence; disconnect during delivery/replacement/neutral gate; rematches; three actual saved-scene menu roundtrips and one listener. The first focused run found stale aftertouch usage after rematch. ResetRacer now resets that budget immediately; the corrected reproduction passes.

Native camera-only renders cover one/two/four-human panes at1280×720 and1920×1080, plus four panes at1440×900,1280×960 and2560×1080. Long-name TMP component glyph/overflow checks passed. Camera-only renders exclude overlay UI and are not proof of full-window UI readability; final Windows checks remain required.

The saved-scene audit passed23sports /25distinct explicit scenes, no missing scripts, one listener per scene, correct startup, actual EasyRoads ramp rays, persistent glyph/marker/replay references, preview/audio, and exact restoration after a simulated build failure.

Raw evidence stays under project Logs/TruckSports/TerrierUpdate/TargetLaunchEvidence, TargetRangeEvidence and TargetFocusedEvidence. Earlier comparisons/failures are archived under Logs/TruckSports/SevenSports/TargetAIBaseline, TargetRangeBefore, TargetRangeAfterTakeoff and TargetResetFailure. Logs and generated images are excluded from Git.

## Still unverified

The current Windows executable remains the earlier19-sport milestone. Target Windows/full-window UI and sustained rendered four-view performance are pending the combined expansion build. No human playtest, physical-controller, real haptic, speaker/audio mix or GPU benchmark acceptance is claimed. Rule fixtures, software devices, injected physical contact fixtures, actual runway simulation and manual camera rendering are distinct evidence.
