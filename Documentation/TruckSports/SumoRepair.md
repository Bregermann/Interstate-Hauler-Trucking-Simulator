# Truck Sumo scoring repair — October 18, 2026

Windows x64 Development; 35 explicit Sports-only scenes; TruckSports startup; 0 errors /31 warnings. Built 2026-10-18T19:53:10.004882+00:00. Managed SHA256 `90dc08698caa0e7f9a2335644d3b985930f0a00d4b1c6c5bce90d7ac81d933d4`; 438-file data manifest `217c43b660c82864c4880352931a332823969968160fb72ecaca62b3b548161f`.

Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the entire folder. AMD64, companion files and complete data hashes are verified. The original retro Party wordmark and the preceding Field Goal repair remain included. This is a Sumo scoring milestone; the complete queued release remains unfinished.

## Implemented and preserved

Sumo now awards **+1 per causally credited opponent ring-out** and **+3 to the sole surviving truck**. Three credited eliminations plus survival yield six points. Points and ring-outs accumulate across heats and reset on rematch. Same-step mutual eliminations can credit each legitimate attacker; no survivor means no three-point award. Duplicate crossings and repeated finalization cannot add points.

The existing match format is preserved: first to five heat wins becomes champion. Activity points, ring-outs and heat wins are displayed separately. No unrequested change to the victory condition, ring dimensions, shrinking, chassis-center boundary, NWH handling, mass, grip, speed or shove strength.

The existing rounded Heat/uGUI player cards now read typed values per participant, including duplicate names. They show points, ring-outs, heat-win progress and eliminated/ready/shove-cooldown state. Existing DOTween score pulses respond to authoritative score changes. Brief +1 ring-out and +3 survival feedback is separate from scoring. Compact fallback standings fit long names; full result breakdown remains explicit. The saved How to Play text is updated additively. AI target choice moderately considers vulnerable edge opponents and score leaders while retaining the existing controls and edge safety.

## Causes and contact evidence

The inspected old rules tracked heat wins only. They had no point arrays, no ring-out attribution and no retained attacker history. Impact callbacks provided expression/audio feedback without scoring responsibility.

The new small fixed history records actual receiving contact normals, impulse magnitude, incoming movement and pre-physics positions. It never applies a force. Strong recent impacts or active physical pressure can receive credit for a continuing outward departure; stale contact expires after four seconds, settled or inward recovery clears it, and stationary shrink-only/unrelated self-driven falls give no point. Shared-impact ties use deterministic priority rotating each heat. An eliminated aggressor can retain credit for its genuine recent delayed/mutual impact, independent of callback ordering.

Actual isolated dynamic-body calibration disproved an unsafe assumption: Collision.impulse had the same world direction in both callbacks. Receiving normals correctly opposed. The adapter uses normal-oriented impulse magnitude, not raw impulse sign.

A targeted one-step rim fixture verifies pre-step evidence: actual truck center moves from 11.96m to 12.13m and awards once. Taking the contact origin after the step would miss an immediate crossing.

A stationary, already-contacting assigned-input shove initially displaced the victim but earned zero. The actual pressure callback reported 24.2006 N·s and only 0.0186m/s relative speed. Incoming motion had been snapshotted before the queued shove, and the low-relative-speed contact was rejected without throttle. Bookkeeping now includes the already queued velocity change and recognizes that one shove step as active pressure. Retest uses the **same impulse**, awards exactly one, and adds no second force or stronger shove.

The first Windows candidate exposed repeated blocked-AI standoffs. The inspected code gave every truck an identical two-second recovery delay, permitting synchronized reverse retries that restored the same obstruction. Staggering that timing was tested as a narrow planning correction; it does not establish that every draw has the same cause. Its Hard test reached 19 heats with only three resolved wins, and that candidate was rejected. Recovery now rotates a 1.6/1.83/2.06/2.29-second delay by seat and heat, keeping the average near the original two seconds. Reverse duration, truck controls, grip, collision forces and edge safety are unchanged. A complete native Hard experiment finished 24 heats with 9 draws and a five-win champion; the final Windows difficulty results below are the acceptance evidence, not that earlier experiment. No score injection or shortened artificial heat result was used.

The old focused UI assertion read stale geometry from a disabled legacy text object. ForceMeshUpdate(true) now validates the fallback deliberately, and separate checks verify all 12 visible name/value/detail texts. Failed and non-started HTTP-eval attempts are not acceptance evidence. The existing Editor job dispatcher now invokes scoped Sumo acceptance after import/compilation rather than relying on long scene operations inside a five-second eval request.

## Source, scenes and assets

Changed SumoRules, SumoFighter, SumoMatch, existing focused/full-game probes and SumoFixtures. Added SumoAttribution, explicit development-only SumoRepairProbe and additive SumoRepairAuthoring. The existing PlowEditorJobs transport has one Sumo dispatch branch; Pedestrian Plowing runtime, roster and gameplay are unchanged.

Saved scene: Assets/TruckSumo/Scenes/TruckSumo.unity. Only help text and normal empty participantId schema serialization changed; original scene IDs, geometry, truck references and authoring layout are preserved. Explicit fresh authoring already reads match.Help. No scene regeneration, vendor source changes or new visual asset framework. Required Unity meta files are included.

## Actual acceptance

Native Editor compilation and deterministic scoring/contact-history fixtures pass. Explicit Play Mode checks cover receiving normals, actual keyboard-driven push-out, uncredited self-drive, one-step rim impact, stationary resting-contact shove, sole-survivor award, duplicate names, pause, replay, rematch, injected shared-controller ownership/reconnect and cleanup. Development native runs completed four-AI Easy/Normal/Hard games; Hard was repeated after the recovery-timing change. The final-source Easy/Normal/Hard acceptance is the Windows suite below. These are custom native fixtures/probes, not a claimed NUnit Test Runner run or human playtest.

All 14 final-version Windows suites pass: contact calibration; physical scoring; resting-contact shove; focused input/reconnect/replay/five-aspect UI and 60-second live render sample; three complete AI difficulty matches; title brand lifecycle; FieldGoalFocused; TargetRepair; KeepawayPresentation; TerrierFinish; shared participant matrix and shared camera matrix. Fresh results, expected process exits, PID checks where supported and sealed build hashes prevent stale acceptance. No detected runtime exception, shader-error or crash markers.

Actual final player match observations (one full game per difficulty, not a statistical balance claim):

```text
MATCH difficulty=0 points=8,8,15,7 ringouts=8,2,0,4 contacts=1834 heats=9 draws=1 wins=0,2,5,1 winner=2 impacts=140 shrinkObserved=True
MATCH difficulty=1 points=22,9,20,17 ringouts=7,3,8,5 contacts=8687 heats=18 draws=3 wins=5,2,4,4 winner=0 impacts=444 shrinkObserved=True
MATCH difficulty=2 points=22,20,20,13 ringouts=7,8,8,7 contacts=15743 heats=27 draws=12 wins=5,4,4,2 winner=0 impacts=814 shrinkObserved=True
```

Actual live-workload observation:

```text
PERF seconds=60.00328 resolution=1920x1080 gpu=NVIDIA GeForce GTX 1660 Ti liveFrames=9418 focusedFrames=9418 rematches=0 frames=9418 renders=9418 meanMS=6.37082395312588 p95MS=6.296939 p99MS=12.47837 GC0=1 managedDelta=1826816 editorBatch=False
```

This is the mode's single broadcast camera with four active AI trucks/replays, at the resolution recorded above. Automatic rematches keep the sample live if a match completes. Other applications were not closed or controlled. Timings are an observed development-player sample, not an isolated comparative GPU benchmark. The earlier Editor measurements are not player performance acceptance.

Actual player gameplay/HUD images at 1280x720,1920x1080,1440x900,1280x960 and 2560x1080 are retained and inspected, alongside credited ring-out and original title screenshots. Earlier camera-only images are not described as full HUD evidence.

[Acceptance manifest](SumoEvidence/Acceptance.json) and [suite receipts](SumoEvidence/WindowsSuites.json) bind all results to this executable. Warning inventory is retained in SumoEvidence/build-warnings.txt; zero errors is not a warning-free build. Host settings restoration and unrelated file hashes are verified.

Hard still produced draw heats in the final full-match run; the exact count above is retained. Staggering recovery improves progress but is not a claim that every crowded standoff is eliminated. Longer human and AI balance sessions remain appropriate.

Manual checks remain: human feel/competitive balance, physical controllers/shared-pad ergonomics/wheels/keyboard rollover, perceived audio/haptics and longer sessions on other hardware. Attribution is a bounded physical heuristic; pathological multi-truck piles deserve human review. The later Goddess front-costume correction and remaining other-mode work are not claimed complete.

## Vendor accounting

VENDOR ASSETS AUDITED: Assets/NWH/Vehicle Physics 2 and WheelController foundations, existing PhysX contact model, Heat/uGUI SportsBroadcastScores, DOTween feedback, Cinemachine camera/replay and installed ring/truck/environment presentation.

VENDOR ASSETS USED: Existing NWH through SportsEventTruck; Rigidbody/PhysX contacts and the unchanged bounded shove; Heat/uGUI typed score cards and DOTween score pulses; existing Cinemachine, replay, audio/vibration/input adapters and owned venue/truck art.

RELEVANT ASSETS NOT USED: Traffic, navigation and general weather systems do not provide Sumo-specific causal ring-out ownership or point rules. No new vehicle/collision/UI/replay framework is needed.

CUSTOM SYSTEMS CREATED: Small Sumo point/contact-history semantics, pre-step/queued-shove observation, typed score feedback, additive help integration and explicit acceptance fixtures. Existing Editor transport is extended.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
