# Frame capture and integration follow-up (source only)

## Authorized diagnostics follow-up

The later diagnostics-only authorization supersedes the original no-production-edit restriction below. Changed `TruckTaxiFrameSpikeDiagnostics.cs`, `TruckTaxiSnowSurface.cs`, and `TruckTaxiFrameSpikeDiagnosticsTests.cs`; added `TruckTaxiSnowCacheDiagnosticsTests.cs` and its `.meta`. No snow optimization, vendor edit, filesystem writer, new manager, Editor/build run or test execution was performed.

- `Capacity = 128`; `Ingest(Sample)` rejects invalid/subthreshold frame durations, counts inclusive warning/severe thresholds, and overwrites only the oldest sample. `CopyChronological(Sample[], int destinationIndex = 0)` copies oldest-retained first into caller-owned storage, bounded by available capacity; a short buffer receives the oldest retained prefix. It returns the copied count, allocates nothing, and never exposes the backing array. Use a Capacity-sized buffer for the complete ring. Both APIs are main-thread APIs.
- `ReadCounter(ProfilerRecorder)` returns -1 for invalid OR empty recorders. Clear now releases retained sample references, resets the GC generation-zero baseline and log cooldown as well as counters. GC baseline advances even across skipped/unfocused frames.
- Samples include realtime timestamp, separate SaveService.IsSaving versus IsBusy/IsLoading state, availability flags, weather ID, region, navigation target/ready/suppressed flags, HUD preference/full-map flag, pending streaming operations, and snow-cache counts. HUD ENABLED means the display preference, not proof of on-screen rendering. Context comes from cached references/getters; no scene scans/reflection were added to frame sampling. Persistence is resolved through Bootstrap's existing property because it is created after diagnostics initialization. Load preparation can be sampled even before host.Ready.
- State remains an observation at capture time, not a profiler marker or proof of causation; synchronous saves may finish before observation and recorder timing can refer to a preceding frame. Summary now says this explicitly. There is no durable report writer and no claim that transient operation correlation or Windows profiling is solved.
- Snow `CacheTargetCount` counts groundTargets entries, including repeated discoveries. `UniqueCacheTargetCount` counts unique ground renderer IDs discovered since the last streamed-surface clear; neither is a live renderer count. `CacheRefreshCount` counts lifetime actual CacheTargets discovery passes. A cached HashSet tracks IDs during existing discovery only; telemetry never deduplicates the production list or changes cadence/materials/rendering. Ownership notice to Averroes was given in the task before editing; no separate Averroes messaging endpoint was available.
- Tests now exercise real ingest/producer wrapping, chronological bounded independent copies, invalid times, inclusive/configured thresholds, Clear/GC baseline, invalid and valid-empty recorders, actual save/load observation through existing SaveService state, and snow duplicate visibility without changing discovery behavior. The older reflection-seeded-only test limitation below no longer applies. Live recorder/frame alignment, logging IO, and Windows impact remain unverified.

Main's focused rerun filter: `TruckTaxiFrameSpikeDiagnosticsTests;TruckTaxiSnowCacheDiagnosticsTests`. Add these to the earlier dispatch fixture rerun. No Bootstrap, MegaDebug, HUD, Session, or shared save/navigation/streaming type was edited in this follow-up.

## Original review

No Editor, build, test runner, player, profiler, or commit was run by this worker. No production performance or vendor files were edited. These are source-backed suspects, not measured Windows causes or performance improvements.

## Integration fixtures corrected

- `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiPolishRideBehaviorTests.cs`: the old completed-local cooldown assertion contradicts offer-based intercity mix. The replacement verifies a configured 100% intercity target remains eligible after a completed intercity fare and two declines, while legacy completed-fare counters stay unchanged. The local-only guard fixture now has a valid passenger ID so it actually reaches destination eligibility.
- `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiDispatchCoverageTests.cs`: the Speedway variant marked every synthetic stop Speedway but gave none a public gate ID and provided no outside-complex destination. It now supplies a public pickup and ordinary external dropoff per town. All 1,000 offers must still pass public/pair eligibility; venue offers must leave the complex. Roster coverage and streak assertions remain.
- `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiObjectiveTests.cs`: the scenic stop at the origin could coincide with final destination, correctly invoking optional-stop skipping. The dwell fixture now sits 100 m away from final arrival and explicitly remains active before testing the speed reset. Existing skipped-diversion/final-fare tests remain unchanged.
- The reported `IntegratedEditMode1.xml` was not found under the repo, its Builds directory, F:/Builds, or the task output folder. Failure details above were supplied by Main and traced against source; the original XML still requires its full path.

## Top three Windows measurement candidates

### 1. Snow surface rescans and accumulating duplicate ground targets

`TruckTaxiSnow.cs:231` calls surface refresh every 0.2 seconds. With snow coverage, `TruckTaxiSnowSurface.cs:163` reapplies road registration every three unscaled seconds and invalidates its surface cache. `CacheTargets` at line 461 allocates scene-wide FindObjectsByType arrays plus child renderer arrays. It unconditionally appends each ground renderer at line 478, without the duplicate guard used for roadTargets. Only `RefreshStreamedSurfaces` clears groundTargets. Consequently the same scene's ground list grows on repeated snow scans and `BindGround` traverses those duplicates every visual refresh. Material bindings prevent repeated material creation, but not list growth or traversal.

Measure a stationary and a moving snowy scene for 60 seconds without region changes; compare dry conditions at identical resolution/population. Inspect `TruckTaxiSnowSurface.Refresh`, `CacheTargets`, `BindGround`, reflection/vendor material updates, GC.Alloc call stacks, and the number of groundTargets entries versus unique renderers in the debugger. Look for three-second bursts and increasing traversal time. Keep Weatherade active; no vendor edit or blanket disabling is proposed.

### 2. Two Compass renders and per-frame reflection allocations

`TruckTaxiGPSAdapter.cs:177` configures vendor TimeInterval capture at 0.10 seconds for HUD and 0.15 for cab; full-map/preview switches the HUD to Continuous at line 311. Vendor `Assets/Plugins/Kronnect/CompassNavigatorPro/Scripts/Private/CompassProPrivate.MiniMap.cs:716` schedules snapshots and lines 839/842 call Camera.Render. Both maps can render on the same frame. LateUpdate also calls `CompassPresentation.BecameVisible` for both; the reflected bool returned by Visible.GetValue is boxed each visible-instance check. Reflection is separate from rendering cost.

Use the existing timed-versus-continuous probe flag with the same binary. In an attached Windows CPU Timeline inspect each `CompassPro.LateUpdate`, Camera.CustomRender/render submission, and GC.Alloc under `TruckTaxiGPSAdapter.LateUpdate`. Also inspect full-map open/closed and ordinary gameplay with both displays retained. Interval renders are expected; their actual contribution to >33/>50 ms frames needs measurement. Historical PerformanceBaseline captures do not establish current cost.

### 3. Repeated presentation formatting and fare allocation

`TruckTaxiHud.cs:262` refreshes at 0.15 seconds plus session-change events. Its line 479 formats ride strings and calls `session.EstimateFare()`, which allocates `new TaxiFare` at `TruckTaxiSession.cs:796`. Binding hint strings and other UI fields are rebuilt too. Race countdown `TruckTaxiRaceRuntime.cs:194` formats/assigns every countdown frame; running timing text at line 266 is already bounded to 0.1 seconds, not every frame. Do not remove those existing throttles.

Measure pickup/onboard driving against the same route when available; inspect GC.Alloc stacks under Hud.Refresh, EstimateFare and binding display formatting, plus Canvas/TMP rebuild timing. Repeat race countdown/start/finish and a 20-rival flagship event. Report bytes/call, calls/second, GC collection frames and p95/p99/>33/>50 ms counts. String construction is certain from source; material impact on spikes is unmeasured.

## Capture/probe limitations for Main

- New capture stores only 128 threshold-exceeding frames and public Summary describes only the newest. No timestamp, ordered sample export, weather/navigation/map/debris/stream-operation state or subsystem markers currently exists, although the attachment requests them. A future read-only chronological copy/export API and deterministic sample-ingestion helper would enable threshold/wrap tests without a live host. Not implemented here because Main owns production performance.
- `Sample.saving` uses `Persistence.IsBusy`, which tracks load preparation, not `SaveService.IsSaving`. The label SAVE/LOAD therefore cannot establish autosave correlation. Readiness gating can also omit load-blocked frames. Snapshot state is observed in Update and may not correspond to the preceding frame's recorder value; synchronous saves can finish before capture observes them. Use profiler/save lifecycle markers for attribution.
- Recorder reads check Valid, not Count. An initialized but empty recorder can appear as zero instead of unavailable. GC collections use generation zero only. Clear resets visible counters/cursor but not the GC baseline or logging cooldown. None of these production behaviors was changed.
- The driving probe has useful motion/focus/path guards and reports mean/p95/p99 and GC/physics/main counters, but its race workload is Practice, not the required 20-rival flagship race. It has rain but no snow workload and no explicit save/load workload. Add those manually during Windows validation.
- Probe keyboard injection uses `new KeyboardState(pedal[, steering])` every measured frame; the installed Input System constructor is `params Key[]`, producing a small harness-only array. The sample list starts at 8,192 floats and can grow if a window exceeds that many frames. Soak windows are 60 seconds each. Capture an ordinary manual-driving session too, rather than attributing all probe allocation to gameplay.
- RegionalWorld's Rigidbody array scan is already development-only and once per five seconds, not every frame. It counts found bodies, not awake bodies. Treat it as diagnostic overhead when correlating that cadence. Spike log formatting/IO is opt-in with a two-second throttle; keep it off for the primary profiler comparison.

## Focused coverage added

`Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiFrameSpikeDiagnosticsTests.cs` and its `.meta` add eight NUnit cases: default thresholds and fixed 128-slot storage, newest-summary slot before/at cursor wrap (three cases), idempotent Clear preserving configuration/storage, unbound Update, unavailable recorder sentinel, and destruction before Initialize.

Private storage is seeded through reflection only to test public Summary/Clear. These tests do not prove live threshold admission, actual producer wrap, focused/ready host capture, recorder frame alignment, logging throttle, or development/release integration. No live Initialize is invoked and no profiler recorder is started by these tests.

## Commands for Main (not executed)

Run tests only when the shared Editor is closed, or select these classes in its existing test runner. Set `$unity` to the installed 6000.4.10f1 Unity.exe, `$repo` to this checkout, and `$out` to an existing validation output directory:

```powershell
& $unity -batchmode -nographics -projectPath $repo -runTests -testPlatform EditMode -testFilter 'TruckTaxiPolishRideBehaviorTests;TruckTaxiDispatchCoverageTests;TruckTaxiObjectiveTests;TruckTaxiFrameSpikeDiagnosticsTests' -testResults "$out/DispatchFrameFollowup.xml" -logFile "$out/DispatchFrameFollowup.log"
```

After Main independently produces a current Development Windows player, use the actual player path, foreground focus and a disposable test profile. The existing executable at `Builds/TruckTaxiDemo/TruckTaxi.exe` is not evidence that current source was built. The harness owns synthetic input, changes video settings and exits the process. Keep other player instances closed. Do not pass batchmode/nographics to graphical driving validation.

```powershell
& $player -truck-taxi-driving-perf -logFile "$out/timed-player.log"
Copy-Item -LiteralPath "$playerDir/Validation/TruckTaxiDrivingPerformanceProbe.json" -Destination "$out/timed.json"
& $player -truck-taxi-driving-perf -truck-taxi-driving-perf-continuous-map -logFile "$out/continuous-player.log"
Copy-Item -LiteralPath "$playerDir/Validation/TruckTaxiDrivingPerformanceProbe.json" -Destination "$out/continuous.json"
& $player -logFile "$out/manual-player.log"
```

Wait for each player to exit and verify a newly written report before copying; the report filename is fixed and overwritten by the next run. For manual driving attach the Unity Profiler, enable CPU Timeline and GC.Alloc call stacks, leave Deep Profile off initially, record matched weather/map/population states, and save the raw capture. Keep normal maps, traffic, snow and all 20 flagship rivals functioning. Check each workload's error, motion and focus fields before accepting any timings. Commands are handoff instructions, not executed evidence.

## Vendor rule

- VENDOR ASSETS AUDITED: installed Compass Navigator Pro render path, UTS control path, Weatherade-facing LWS surface adapter, Input System keyboard API, existing NWH-driving probe, Scene Streamer-facing regional service, Pixel Crushers-facing save state.
- VENDOR ASSETS USED: existing integrations only; Unity ProfilerRecorder and existing NUnit/Unity Test Framework contracts in new tests.
- RELEVANT ASSETS NOT USED: no alternate profiler, renderer, traffic, GPS, save, snow, or UI system introduced; Heat presentation unchanged.
- CUSTOM SYSTEMS CREATED: none; focused tests and this source-review report only.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.

## Current status correction (post-integration)

The earlier "no snow optimization" statements describe the original review, not the current working tree: the later authorized fix now prevents duplicate ground-target accumulation in `TruckTaxiSnowSurface.CacheTargets`. Main's `Logs/TaxiCorrection-IntegratedFinal.xml` records all three snow-cache tests passing. With three fixed renderer inputs over 20 discovery passes, the actual production fixture retains three entries and performs 60 candidate visits, versus the legacy append-only control's 60 entries and 630 candidate visits; unique count stays three. This proves bounded algorithmic work for that fixture, not a Windows FPS or hitch improvement, which remains unmeasured. See [Main's final acceptance ledger](../../Assets/LWS/TruckTaxi/Documentation/PlayerCorrectionAcceptance_20261009.md) for current integration and validation status.
