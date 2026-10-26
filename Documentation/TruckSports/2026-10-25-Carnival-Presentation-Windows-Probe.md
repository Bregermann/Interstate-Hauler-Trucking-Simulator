# Carnival presentation Windows probe

NEW source-only preparation. Existing probes, runtime HUDs/gameplay, scenes, build/runner, Git and CLEAN remain untouched. No Unity/player executed by this agent. Execution, screenshots and human review remain pending the lead's integrated candidate.

`SportsCarnivalPresentationWindowsProbe` activates only with `-tsCarnivalPresentation` in a DEVELOPMENT_BUILD WindowsPlayer; Editor execution is rejected. Output: `-tsCarnivalPresentationOutput`.

Named12venues: Cornhole, Target Launch, Sky Tow, Dock Diving, Giant Swing, Field Goal, Bungee Demolition, Whack-a-Mole, Floor Is Lava, Volleyball, Avalanche Escape, Red Light Green Light. Optional `-tsCarnivalPresentationNinja` adds one brief natural Ninja attempt/charge/jump/fall observation (13venues). No opaque60-scene sweep.

Default30cases/60PNGs: each venue has original all-AI shared/turn-based play; Corn/Target/Sky additionally use actual AI rosters1/2/3/4 for their real1–4view matrix; Bungee/Whack/Lava/Volley/Avalanche/Red additionally show the original first four AI views of the full AI roster. Dock/Swing/Field Goal retain original sequential single-view presentation. Optional Ninja adds one case/twoPNGs. Existing SportsCarnivalWindowsProbe separately covers full original Pong/Hauler/Hot/Skee workloads.

SportsCameraProbe.Bind was audited: it creates synthetic keyboard/controller owners and is deliberately not reused. This probe uses original SportsNavigation, SetupScreen/ChangeSetup, AllBots/AllAI, StartMatch, ShowAI, driving-view preferences and return-to-front cleanup. All sources remain AI; owner/camera arrays stay game-owned. No truck pose, rotation, velocity, command, force, score, outcome, timer or finish is assigned.

Natural AI reaches a brief current gameplay/flight state. Captures stay LIVE1x, without pause/freeze. Screen.SetResolution requests1280×720and1920×1080; ScreenCapture.CaptureScreenshot writes actual backbuffers. Decoded PNG dimensions are verified without rescaling/offscreen substitution. Checks cover real camera render callbacks, viewport coverage/actual owner bindings, subject/objective frustum intersections, and central world pixel variation/nonblack count. Request and inspection frame/state are recorded separately because gameplay continues during asynchronous PNG completion. PNG inspection is still required for actual visual/palette/readability approval.

Normal catalog/profile APIs choose installed bodies/a compatible hat in process-specific isolated validation preferences; original StartMatch applies them. Actual effective body IDs, exterior renderers/shaders and matching framing envelopes are observed. Modes without authored hosts are explicitly venue/HUD-only evidence. At least two effective composed body IDs must actually appear across the tour. Source prefabs and physical envelopes remain unchanged.

Existing typed racing finish gates/checkered meshes are checked only where applicable. Naturally observed locked finishes/victory bursts are recorded. Zero means mounted presentation only, never finish-celebration execution. No forced finish or full-race acceptance.

`live-diagnostics.tsv` samples unscaled wall-frame duration and Unity allocated/Mono memory in a separate three-second LIVE1x window per venue after resize stabilization, outside capture/freeze/replay. No forced GC/quality/VSync/frame-cap changes. An empty eligible window is unavailable. GPU timing is unavailable/not sampled; backend/device metadata is recorded. Mean/p95/max are raw diagnostics, not CPU/GPU/FPS acceptance or a soak benchmark.

Evidence: progress.txt, frames.tsv, live-diagnostics.tsv,60/62actualPNGs, player.log, result.txt. Output must be a new canonical child of Logs/TruckSports/PlaytestRepair. Junction/symlink redirects are rejected; prior results are never overwritten; CreateNew reserves owned evidence. Interrupted runs have no terminal acceptance footer. Rematches/scene unload must clear match ownership/old scene appearance hosts; profile/input leases restore on exit.

Use the existing runner unchanged after the lead builds into its expected candidate slot:

`python Tools/TruckSports/RunPlaytestWindowsValidation.py CarnivalPresentationCandidateUnique --flag=-tsCarnivalPresentation --output-flag=-tsCarnivalPresentationOutput --timeout 1200`

Optional Ninja: `--extra-arg=-tsCarnivalPresentationNinja`.

Standard CHECKS=N ERRORS=N footer matches the existing runner. RunPlaytestWindowsValidation.py/BuildIdentity.py retain managed/full-content hashes, owned process exit/watchdog, backend and completion evidence. This source never launches/controls a process.

Handoff contains only new C#/.meta and this document. Existing runtime catalog art is reused. No raw FBX/texture/package/archive/license-source HTML is copied or published. Separately reviewed Quaternius source/hash/CC0 records remain in their explicit existing intake/third-party paths; this probe packages none of them.

VENDOR ASSETS AUDITED: Heat/TMP, NWH/PhysX original flow, DOTween, appearance/framing, Weather Maker and existing camera/build-identity helpers.

VENDOR ASSETS USED: those existing runtime authorities plus Unity ScreenCapture/render callbacks/Profiler memory APIs.

RELEVANT ASSETS NOT USED: synthetic-owner binding from SportsCameraProbe; actual AI selection is required. Wall time never substitutes for GPU timing.

CUSTOM SYSTEMS CREATED: one opt-in bounded Windows evidence probe.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
