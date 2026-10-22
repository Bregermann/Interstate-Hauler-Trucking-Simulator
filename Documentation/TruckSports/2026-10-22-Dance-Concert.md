# Dance-Off — party timing and concert presentation

HPR-28-1 through28-5 now use the existing dedicated Dance-Off PNG on the saved stage sign, front-facing truck bases, configurable more forgiving timing and musical camera variation through the original Cinemachine rig. The original22 choreography routines, seven song recordings/charts, input maps, calibration and rhythm HUD remain authoritative.

## Cause and changes

The baseline actual Windows image showed the collection logo and rear-facing truck bases. One-time `DancePlaytestRepairAuthoring.Upgrade` changes only the existing sign's material/pose and four dancer base rotations, adds the saved camera adapter and sets two serialized timing fields. Repeating the tool preserves saved designer edits; ordinary builds do not regenerate the scene or art.

The legacy rules exposed fixed90ms GREAT/160ms GOOD windows. New match settings default to120ms GREAT/210ms GOOD while PERFECT stays45ms. Rules validate finite ordered tolerances; the original constructor retains the strict defaults for existing callers. Early/late/wrong-lane judgments, chart resolution, scoring math, combos, DSP scheduling and calibration remain intact. All competitors use the same match tolerance.

`DanceConcertCamera` selects four bounded, slow musical shot styles through the existing `SportsEventCamera`/Cinemachine rig. A16-beat period with7-second minimum controls shot changes;1.2-second smoothing plus original camera damping provide sweeps, lower wide views and performer emphasis. Multiple human players retain wide gameplay views; solo/AI-only shots can emphasize one performer. Performer focus locks within each shot to prevent rapidly changing AI leaders jerking the camera. Existing spin routines receive extra headroom. Pause/rematch/menu lifecycle resets presentation independently of scoring.

## Actual verification

- Native EditMode40/0:28 original rule cases,11 new timing cases, one saved reference/idempotence check.
- Native PlayMode1/0: explicit camera-clock fixture exercises all four musical shots, original one-rig ownership, stable mid-shot focus despite fixture leader changes and setup reset. No fixture scores are presented as real song outcomes.
- Exact Windows payload39/0:21 composed concert checks over one complete actual four-AI60-second original song, plus9 mixed-source and9 shared-input/reconnect regressions. Actual DSP timestamps generate scores and original physical dance routines; every note resolves, results/rematch/menu cleanup pass. Keyboard/whole-controller/two-stick events are software-injected, not hardware tests.
- Agent reviewed actual composed720p/1080p captures across all four musical shots, including performer emphasis and complete results. Timing prompts remain readable; the correct artwork renders, trucks face forward before expressive routines, and motion stays bounded. Source music/charts and choreography were not replaced.
- All15474 original scene documents retained, one adapter added. Five changed transforms are precisely four base rotations plus the existing sign. Original colliders, particles and camera-rig configuration are preserved; Unity serialized one previously absent null broadcast-framing reference. Original input, HUD, choreography, venue and song library/audio/chart source hashes remain unchanged. All925 unrelated pending files retain their baseline hashes.

No developer playtest, physical controllers, listening, display-latency calibration acceptance or isolated CPU/GPU benchmark was performed. These are automated/agent visual results. Other active requirements remain open, including the newly reported Dock platform/water/crowd follow-up.

## Delivery and vendor accounting

Combined60-scene Windows build: `Builds/TruckSports/TruckSports.exe`; full payload `b3813fe215a01dc3f59d3ef4ef676169bb61f877b7be2cde32111c0d964169e4`, managed `c2521a4ef179e66e1285de05a0f8c4e4ad269c84a529ddbd5b7581095424577c`,546 verified companion files and248 exact archived scoped C# inputs. Prior accepted Derby build remains recoverable.

VENDOR ASSETS AUDITED: Existing Cinemachine virtual camera/Transposer/Composer damping, SportsEventCamera and broadcast ownership, DOTween/Heat/TMP rhythm presentation, original song/chart and truck choreography assets.

VENDOR ASSETS USED: Original Cinemachine rig/public adapter, existing dedicated PNG and URP material foundation; unchanged Heat/TMP/DOTween HUD and original animated truck rigs.

RELEVANT ASSETS NOT USED: No new camera framework, post-processing style, rhythm engine, music package or artwork generator was needed. Dance uses its existing dedicated concert camera and yields if shared broadcast framing later owns the view.

CUSTOM SYSTEMS CREATED: Small musical-shot coordinator, validated per-match timing parameters, idempotent scene authoring and targeted diagnostics/tests. No duplicate input, animation, camera or UI framework.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
