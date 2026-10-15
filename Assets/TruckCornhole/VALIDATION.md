# Truck Cornhole validation — 2026-10-15

Implementation and Windows delivery are complete; publication is recorded in CHECKPOINT.md and Builds/TruckSports/GIT-DELIVERY.txt. No human playtesting or physical-controller acceptance is claimed.

## Final evidence

| Evidence | Result | What it establishes |
| --- | --- | --- |
| Native Editor fixtures |262 assertions passed|Existing sport rules/source ownership plus16 Cornhole rules/setup/view-coverage fixtures; saved scenes have valid references, actual open hole, no missing scripts, one listener/EventSystem.|
| views15 |146 checks, native0 and completion0|Visible native1–4 participant layouts at1280×720 and1920×1080; long names; default cab eyes; horizon; two non-truncated help pages; four replay angles/five rates; software replay controls.|
| suite23 |216 checks, native0 and completion0|Actual trampoline contacts, seeded PhysX scoring/contact cases, software source ownership/sensitivity, pause/replacement gates, full accelerated match, rematch and repeated menu/old-sport loads. No runtime errors.|
| guards21 |9 checks, native0 and completion0|Injected lifecycle failure states cannot gain late hole credit or advance another truck's active flight. Production guards are unchanged in23/24.|
| AI23 |25 checks, native0 and completion0|Visible real-time four-course, five-round match at timeScale1;80 actual launches, independent progress, correct attempt/bank counts, results/review/rematch.|
| Final native24 |Succeeded,0 errors,13 warnings|Windows x64 Mono Development, seven explicit scenes, TruckSports startup, full companion payload;49.6710765s build,314,604,880 bytes.|
| Normal executable startup |Running, title Truck Sports|Exact delivered executable launched as PID43868 with original company/preferences; no runtime exceptions in startup log. Left running for human testing.|

All176 managed DLLs and all seven level0–6 hashes match isolated validation23. All1,536 ProjectSettings/Packages baseline hashes match after success and deliberate-failure restoration. Earlier sports' gameplay/scenes remain unchanged; Wallball changes only its editor front-end preservation hook. Native regeneration was explicitly checked to retain six menu entries.

## Physical and rule coverage

Real NWH approach reaches actual trampoline contact once. Seeded entry speeds/angles produce different flight velocities; these are contact fixtures, not AI successes. Natural final-suite arc:6.139925s to board contact, peak48.49538m, range58.47199m. This replaces the earlier fast prototype's lower/flatter launch; no target trajectory snapping was added. Held forward cannot immediately consume range aftertouch; lateral/range changes remain bounded, provide no lift/reverse flight, and stop after board or already-landed-truck contact. Horizon remains stable under an injected tumble.

Seeded real PhysX cases prove stable top support1, full downward hole3 once, partial passage not3, miss0, later physical impact displacing a prior truck into the hole (1→3) and off the board (1→0). Pure rules additionally reject board-plus-ground, hovering, moving support and excluded failures. A sleeping body lifted off the board loses support; stable unsupported landings resolve zero without a failure timeout. These are marked fixtures with body repositioning/forced sleep, not unassisted human shots.

Fixes found during validation: NWH-generated zero-friction wheel colliders had remained active after disabling the wheel behaviours, preventing reliable board rest. The adapter now disables those colliders in flight and restores them on spawn. GR3D geometry must be built at the origin before course positioning. Board support now clears on real contact exit; a sleeping body's historical contact cannot score. Irretrievable lower-ground misses collect for zero after1.2s, while on-board bodies stay dynamic. Settled stacks/side hangs resolve normally; previous-body landing events cannot advance the active truck, and excluded failures cannot gain late hole credit. Camera teardown now checks destroyed truck references. All final repeated-return checks pass.

Private suite23 includes an accelerated physical match (timeScale3); it is not a GPU benchmark. AI23 is normal speed with no Editor or other game at startup; OBS was left running. Both have80 launches and zero resolution timeouts:

```
PRIVATE PHYSICS / ACCELERATED
PHYSICAL_MATCH launches=80 holed=7
AI difficulty=Easy total=26 timeouts=0
AI difficulty=Normal total=20 timeouts=0
AI difficulty=Hard total=15 timeouts=0
AI difficulty=Hard total=16 timeouts=0

VISIBLE REAL TIME
PHYSICAL_MATCH launches=80 holed=5
AI difficulty=Easy total=13 timeouts=0
AI difficulty=Normal total=19 timeouts=0
AI difficulty=Hard total=21 timeouts=0
AI difficulty=Hard total=13 timeouts=0
```

AI aligns on the ramp, chooses speed and aims an airborne landing before the hole to leave room for physical skid. Difficulty changes planning error, reaction and aftertouch judgment; no score assignment, privileged grip, homing or delivery teleport exists. These small samples establish plausible mistakes/progress, not guaranteed difficulty ranking. AI balance remains a human playtest task.

## Replay, inputs and lifecycle

Four bounded recordings contain all four course bodies. Replay meshes have no colliders, Rigidbody, input or audio handlers. Independent clocks/angles/rates never rewind scores or global physics. Tests verify0.25×/0.5×/1×/2×/4× timing against actual wall time, rewind, pause, stable horizon, unchanged settled body/score/attempts and continued second-course phase progress. The next delivery waits only for its own review, with45s limit. Final results can review all four last throws and return to standings even after playback stops.

Software keyboards/sticks prove independent routing, shared left/right ownership, duplicate/whole-versus-stick rejection, sensitivity scaling after Unity stick processing and bounded steering. Software removal/replacement during flight preserves exact velocity/position/counts/score and rejects held confirmation until neutral. West opens/changes angle, South changes speed, North rewinds, East returns; whole-pad ownership applies. Shared-stick viewers use on-screen replay controls to avoid shared-button ambiguity. Hardware rollover, real controllers/reconnect/rumble/audio are unperformed.

Both completed matches exercise rematch reset/configuration retention. Three menu round trips clean scene-owned camera callbacks, ghosts, input subscriptions and audio; all five existing sport scenes then load with no runtime errors. Source/scene preservation plus native rules are regression evidence; this is not a new full human playtest of every old sport.

## Performance and remaining checks

Real-time1080p four-course sample:

```
PERF duration=344.4204 frames=54183 meanMs=6.34200862789277 p95=6.271662 p99=12.47903 maxMs=737.6731 managedDelta=1622016 gen0=30 GPU=NVIDIA GeForce GTX 1660 Ti size=1920x1080 timescale=1
```

54,183 frames over344.4204s: mean6.342ms, p956.272ms, p9912.479ms. One worst737.673ms frame was recorded; its cause is unisolated. This is an observed hitch, not a claim of uniformly smooth frame time. Recheck cold launch/replay preparation and desktop contention in human profiling. OBS/shared-GPU activity was preserved; this is not an isolated lab benchmark. Managed delta1,622,016 bytes,30 Gen0 collections; recordings/bodies/effects are bounded and reused.

Still human checks: real keyboard/controller combinations, shared-pad comfort, haptic/audio mix, subjective launch/aftertouch/AI balance, cab comfort during tumbles, replay navigation and the recorded frame-time outlier. Wallball still needs its previously identified human acceptance. No claim of human playtesting, wheel hardware or physical controller validation is made.

Earlier failed/interrupted development probes are retained in ignored logs and excluded from final passes. Hidden captures are not visual proof. Representative native images and raw final result records are in Documentation/TruckSports/CornholeEvidence.

VENDOR ASSETS AUDITED: NWH WheelController/Vehicle Physics and GR3D geometry, Heat/TMP, DOTween, Cinemachine, Bowling/Sports input/setup/cab conventions; no installed replay system was found.

VENDOR ASSETS USED: NWH approach/suspension and GR3D meshes/engine/skid audio, Heat/uGUI/TMP, DOTween, shared Sports/Bowling adapters.

RELEVANT ASSETS NOT USED: Cinemachine (existing direct per-view cab pattern retained); world/road/weather/traffic/save/online systems are outside this isolated sport.

CUSTOM SYSTEMS CREATED: Cornhole course/rules/progression, physical launch/aftertouch/AI adapters, cab/pose-replay presentation, native authoring and focused probes.

VENDOR SOURCE MODIFIED: No by this update.

DUPLICATE VENDOR FUNCTIONALITY CREATED: No generic vehicle/input/UI/animation subsystem; game-specific scoring/replay semantics only.
