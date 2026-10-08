# Truck Taxi performance baseline (Priority 0)

## Measurement rules

- Reference machine: Windows, NVIDIA GeForce GTX 1660 Ti, 1920x1080, High quality.
- `TruckTaxiDrivingPerformanceProbe` is development-build-only and opt-in with `-truck-taxi-driving-perf`. It drives through the existing Input System and NWH controls, not Rigidbody teleportation during timed windows.
- `-truck-taxi-driving-perf-continuous-map` restores Compass Pro's original continuous camera capture for a same-binary A/B control. Without it, Truck Taxi uses the timed-snapshot policy.
- A sample is not accepted when the player is unfocused, the truck barely moves, highway speed is too low, or a boundary drive does not change region. Warm-up, world preparation, and staging are outside timed windows.
- GPU durations are accepted only when finite, positive and below 1000ms; prior sentinel-like values remain invalid. The latest run returned plausible GPU durations. Render-thread/draw-call and direct Compass recorders remain unavailable; do not substitute main-thread time or zero. The existing regional `ActiveBodies` count is enabled Rigidbody components, not awake PhysX bodies.

## Human-play problem reproduced

The actual user-played Build 13 (GTX 1660 Ti) was connected to the Unity Profiler without deep profiling. Across 201 sampled player frames from a 924-frame capture, main-thread frame time was **30.78 ms mean, 29.37 ms median, 42.68 ms P95, 54.15 ms P99**. One 23.82 ms frame spent 6.96 ms in two `CompassPro.LateUpdate` calls, including 6.56 ms in their two `Camera.CustomRender` calls. Across the sampled capture, Compass averaged **8.24 ms/frame**, environment update **1.49 ms**, main-camera finish/render **6.36 ms**, and fixed physics update **5.26 ms**. The contemporaneous gameplay telemetry reported approximately 20-32 live cars and 180 live pedestrians. The Profiler GPU frame-time field was zero, so GPU limitation was not established.

The shared Interstate truck Compass setup had explicitly selected `miniMapCameraSnapshotFrequency = Continuous` for both the cab and HUD displays. The installed vendor offers a supported `TimeInterval` policy that retains real map capture and route/icon updates. In clear weather, `TruckTaxiSnow.Tick` was also walking up to 4096 road snow cells and calling `DepthAt` for each grounded wheel every frame despite zero accumulated snow. These were source-confirmed contributors, not reasons to reduce population or remove weather.

## Exploratory pre-change standalone drive

The pre-change Windows build was run alone at 1920x1080 High with opt-in synthetic keyboard driving. It was hidden, and speed/focus were not yet measured; treat these numbers as **exploratory only**, not a valid before/after performance claim. The raw JSON is retained in `Logs/TruckTaxiDrivingBefore.json` (ignored local evidence). FPS below is `1000 / mean frame ms`, not the mean of per-frame FPS.

| Workload | FPS | Mean ms | Median ms | P95 ms | P99 ms | Cars / peds | Net movement |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| A dense town | 69.3 | 14.4 | 13.9 | 19.2 | 27.0 | 39 / 180 | 230 m |
| G GPS / HUD | 97.0 | 10.3 | 8.7 | 16.4 | 20.0 | 35 / 180 | 18 m |
| C ride to pickup | 64.4 | 15.5 | 15.1 | 19.9 | 27.6 | 44 / 32 | 126 m |
| C passenger onboard | 56.4 | 17.7 | 16.4 | 23.2 | 34.2 | 37 / 160 | 315 m |
| D heavy rain | 72.2 | 13.8 | 14.1 | 18.8 | 23.9 | 30 / 180 | 116 m |
| E earthquake | 93.8 | 10.7 | 9.7 | 15.9 | 19.2 | 30 / 180 | 21 m |
| B highway | 66.7 | 15.0 | 14.9 | 23.6 | 30.1 | 25 / 158 | 196 m |
| H region crossing | 118.8 | 8.4 | 7.7 | 11.7 | 15.1 | 8 / 0 | 745 m |
| F Thunder Bowl practice | 133.8 | 7.5 | 7.0 | 10.4 | 12.7 | 0 / 0 | 117 m |

The exploratory run's main-thread recorder tracked frame time closely. Its GPU/render recorders were invalid. Physics.Simulate ranged from 0.05 to 0.80 ms mean by workload. GC allocated-in-frame ranged from about 3 KB in the sparse race to 84 KB with a passenger onboard; this needs attribution before declaring the GC gate passed. Managed heap at the sample ends ranged from 84 to 106 MB. High's existing shadows and its Ultra/Medium/Low alternatives were left unchanged.

## Focused fixes

1. `TruckTaxiGPSAdapter` selects the vendor's timed camera snapshots: 0.10 s for the HUD and 0.15 s for the small physical cab display. Interactive full-map and ride-offer preview temporarily keep continuous capture, then return to timed capture for normal driving. Compass Pro still owns both actual map layers and routes. No vendor files were edited for this change.
2. `TruckTaxiSnow` skips dry-weather snow-cell melt/depth scans only when its accumulated depth is zero. Plow logic, tire modifiers from other sources, snow accumulation, melting, and actual snowy traction remain active.
3. The opt-in Windows benchmark now reports focus, path length, speed, population, physics, GC, and available draw calls, and stages highway/boundary drives from authored regional lanes. It rejects nonrepresentative runs rather than presenting them as a pass.

## Validation and after measurement

- Unity C# compilation: pass. `TruckTaxiPresentationPlayModeTests.CockpitPresentation`: 1/1 pass, including the physical GPS route pixel check. `TruckTaxiSnowTests`: 7/7 pass. The existing Windows demo build reported `Result: Success`; its builder also printed `1 errors` without a C# compiler error in the log, so that inconsistency remains to inspect.
- The first post-fix hidden run is **invalid and excluded**. An unrelated GitHub Desktop `lifeline-run` push consumed roughly 49-61 GB during the run, one workload failed movement validation, and later frame times fell to implausible 1-2 ms as the hidden window ceased to be a credible rendered-gameplay test.
- The first **visible, focused** same-binary continuous-map versus timed-map drive is recorded below. The full acceptance gate remains open; the original hidden-run figures are not used as proof of improvement.

## Focused Windows comparison, 2026-10-08

Same Windows development binary, High, 1920x1080, visible window, 100% focused
frames in every timed workload. Raw evidence: `Logs/TaxiFoundation-Before.json`,
`Logs/TaxiFoundation-Timed.json` and corresponding `*Player.log`. This binary
includes the retained Weatherade snow-track materials but predates the current
Phase 0 GPS recovery/secondary fare/source fixes. Population remains enabled;
these are stochastic traffic runs, not identical replay trajectories.

| Moving workload | Continuous mean / P95 ms | Timed mean / median / P95 / P99 ms | Timed cars / peds | Timed path m |
| --- | --- | --- | --- | ---: |
| Dense town | 18.56 / 24.40 | 12.91 / 12.51 / 17.87 / 23.32 | 47 / 134 | 435 |
| GPS / HUD | 17.22 / 21.15 | 12.89 / 12.54 / 18.07 / 22.87 | 31 / 180 | 194 |
| Highway | 9.95 / 12.59 | 8.13 / 7.15 / 11.35 / 15.29 | 0 / 0 | 1009 |
| Boundary east | 10.05 / 12.57 | 7.93 / 7.12 / 10.48 / 13.17 | 4 / 0 | 755 |
| Boundary west | 10.29 / 12.83 | 8.03 / 7.26 / 10.62 / 13.39 | 9 / 0 | 763 |
| Thunder Bowl practice | 9.24 / 12.03 | 6.44 / 6.06 / 8.99 / 10.82 | 0 / 0 | 121 |

Town mean improved from 53.9 to 77.5 FPS; GPS/HUD from 58.1 to 77.6 FPS.
Timed main-thread means respectively: 12.89, 12.87, 8.12, 7.92, 8.02, 6.42 ms.
Physics.Simulate means: 0.574, 0.579, 0.092, 0.083, 0.099, 0.038 ms.
Enabled body counts: 2233, 2220, 7, 11, 17, 7; these are NOT awake-body counts.
GC allocated/frame: 51.9, 47.2, 24.9, 22.5, 22.6, 15.4 KB. Managed heap ranged
90.4-111.8 MB across those timed samples. Allocation attribution and long-soak
growth remain pending. The runtime probe does not attribute Compass CPU cost;
the earlier real Profiler capture above remains the direct attribution evidence.

Rejected/incomplete workloads:
- Passenger: neither run accepted a fare; the probe had left Ride Requests
  disabled. The fixture now enables requests for that stage and restores the
  original preference on exit. Re-execution is required.
- Timed rain: only 2.49m/25s; earthquake: 1.08m/25s. Both INVALID for driving
  performance. Continuous controls moved 299m and 337m, mean 18.75/17.75ms.
  The timed truck became stuck after Clockout staging, cause not established.
  A bounded overlap/contact diagnostic now records that situation; no scenery
  or population has been removed to manufacture a passing sample.
- GPU API returned impossible sentinel-like durations despite success status.
  Those old JSON `gpuValid` flags must be treated as INVALID. The updated probe
  rejects nonfinite/out-of-range durations. Render-thread evidence unavailable.

Ordinary town averages exceeded 60 FPS in this run, but P95 exceeds 16.7ms.
This is improvement evidence, NOT full performance acceptance or human playtest.

## Integrated Phase 0 Windows Checkpoint, 2026-10-08

`Logs/TaxiFoundation-Phase0-WindowsBuild2.log`: Windows development build succeeded,
593024853 bytes, zero reported errors. The earlier builder error-count inconsistency
did not recur. Command-line opt-in: `-truck-taxi-driving-perf`, 1920x1080 windowed.
Raw report: `Logs/TaxiFoundation-Phase0-Focused.json`; full runtime evidence:
`Logs/TaxiFoundation-Phase0-FocusedPlayer.log`. Player exited normally, code 0.
Same GTX 1660 Ti / High reference; all ten workloads passed the probe's motion and
focus checks, 100% focused frames. This checkpoint is not a new same-binary A/B pair.

| Moving workload | Mean / median / P95 / P99 ms | Main / physics / GPU ms | Cars / peds / enabled bodies | Path m | GC KB/frame |
| --- | --- | --- | --- | ---: | ---: |
| Dense town | 12.27 / 11.44 / 16.17 / 21.22 | 12.25 / 0.471 / 8.05 | 44 / 180 / 2231 | 373 | 57.1 |
| GPS / HUD | 11.17 / 10.99 / 15.34 / 18.32 | 11.16 / 0.435 / 8.19 | 36 / 180 / 2226 | 209 | 48.2 |
| Ride to pickup | 11.90 / 11.93 / 16.52 / 19.42 | 11.88 / 0.496 / 7.58 | 28 / 180 / 2223 | 285 | 52.3 |
| Passenger onboard | 11.28 / 11.19 / 15.78 / 19.75 | 11.26 / 0.424 / 7.84 | 20 / 180 / 2213 | 191 | 48.8 |
| Heavy rain | 14.60 / 13.27 / 17.97 / 21.74 | 14.58 / 0.628 / 13.45 | 49 / 180 / 2234 | 315 | 62.7 |
| Earthquake | 12.76 / 12.41 / 17.45 / 22.59 | 12.75 / 0.602 / 11.89 | 35 / 180 / 2227 | 346 | 50.1 |
| Highway | 7.43 / 6.44 / 9.67 / 12.26 | 7.42 / 0.066 / 6.53 | 6 / 0 / 13 | 969 | 25.0 |
| Boundary east | 7.73 / 6.61 / 9.83 / 12.51 | 7.71 / 0.070 / 6.69 | 7 / 0 / 39 | 723 | 25.7 |
| Boundary west | 7.33 / 6.60 / 9.80 / 12.32 | 7.32 / 0.068 / 6.76 | 7 / 0 / 13 | 746 | 22.0 |
| Thunder Bowl | 6.38 / 5.93 / 8.66 / 10.38 | 6.29 / 0.036 / 6.10 | 0 / 0 / 7 | 172 | 16.9 |

Managed heap at sample ends: 82.3-115.7 MiB. This short run is not a leak/soak test.
Traffic, pedestrians, weather and events were retained. The passenger fixture now
uses the registered passenger database, enables requests only for fare acceptance,
and then disables new unsolicited dispatch until measurement ends. It does not
remove population. Engine recovery only presses the existing engine command when
the engine is stopped, avoiding the previous unconditional toggle. Staging and
any pickup teleport remain outside measurement windows; this tests moving cost,
not human completion of a pickup/ride.

Direct `CompassPro.LateUpdate` marker discovery returned unavailable in this
standalone run (`compassValid=false`), not zero CPU cost. Render-thread and draw
calls were also unavailable. The old attached Profiler capture remains the direct
Compass attribution; a new attached profile is still needed for current cost/GC
attribution. Rain/earthquake P95 still exceeds 16.7ms; no locked-60 claim is made.

The current prompt's Phase 0 gate remains incomplete. Follow-on feature completion
and the final feature-freeze declaration must wait for its remaining bug/restart
acceptance, detailed in `SandboxFeatureFreeze.md`.
