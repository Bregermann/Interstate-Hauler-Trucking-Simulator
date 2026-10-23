# Hockey / Wallball shared weather and time — October 23, 2026

Q22-SKY remains **PARTIAL**. Hockey and Wallball now use the existing shared Weather Maker presentation, saved Heat weather/time controls and bounded night lighting. Coverage is 45 shared-session scenes plus five legacy time integrations, including the earlier Cornhole/Cheese/Base milestones. This does not finish the remaining artistic lighting, legacy forecast expansion, snow/tracks, global score publishers or results presentation.

## Implementation

The courts had separate match controllers and fixed lighting. A read-only `ISportsWeatherLifecycle` contract exposes their existing match GUID, active/pause/audio state and SFX setting to `SportsChaosWeather`. That existing adapter configures once per actual new match, retains the clock/profile through pause, and uses its established vendor profile cloning, camera binding and cleanup. No replacement match controller is introduced. Hockey/Wallball gameplay methods, force calculations, scoring, AI and input ownership are unchanged; their source changes add only lifecycle properties/interface declarations.

Both saved scenes contain the existing preset catalog, independent Random/Morning/Day/Evening/Night Heat selector and vendor sky/sun/moon/cloud setup. The original active renderer is cloned before adding the installed Weather Maker URP feature; existing renderer features remain present. Four sport-owned venue fixtures per court and render-only headlight beams use the existing light coordinator. Neither adds collision geometry or shadow-casting spots. Added visual objects stay outside authoritative truck hierarchies.

Explicit additive authoring preserves every original scene document, original Rigidbody/collider document and original world pose. The exact scene review and asset list are in the evidence folder. 925 unrelated pending files are rehashed and preserved. Normal Play Mode/builds do not invoke authoring or regenerate designer settings.

The first Windows candidate passed97checks but was rejected on visual review: court floods were too strong and Wallball fixture models obstructed the view. The explicit correction scales the new models to35%, positions them at the platform perimeter, places lights at their actual model tops and reduces venue/headlight intensities to50/8. The old candidate's evidence remains in local `LegacyCourtsWindows`. Its Hockey setup capture also had stale labels after the probe directly configured AI; the development harness now refreshes that existing menu and explicitly checks the actual roster. Production input/menu behavior was not changed for that capture correction.

## Actual validation

| Check | Observed result |
|---|---|
| legacy-courts-accepted-native | 1 PlayMode tests passed; 6.79s test time |
| legacy-court-shared-physics-regression-native | 3 PlayMode tests passed; 147.37s test time |
| LegacyCourtsFinalWindows | 99 checks / 0 errors; 33.86s; PID 21416 |
| LegacyCourtsHockeySeriesWindows | 34 checks / 0 errors; 445.36s; PID 74528 |
| LegacyCourtsWallMatchWindows | 11 checks / 0 errors; 41.22s; PID 21456 |

The focused native test exercises six Hockey/eight Wallball AI participants and four real forecast/time starts per sport: Clear/Day, Rain/Evening, Snow/Morning and Blizzard/Night. It checks actual AI ownership, one weather/render owner, saved Heat controls without button overlap, night fixtures/headlights, frozen vendor clocks, fresh match identities and pause preservation. The regression contains the45-scene shared test,18 actual Hockey puck crossings through the center/near both legal mouth edges at three speeds, and Sumo's real keyboard/NWH bumper scoring, Heat presentation, replay and rematch test. The shared/physics regression preceded fixture-only refinement; the final focused native test follows it and uses the built runtime sources.

The exact Windows candidate passed 144 checks across three recorded processes. The environment run captures20 actual backbuffers at1280×720 and1920×1080; both setups and all eight weather/time views were inspected. Hockey's existing series completes four actual six-AI three-minute regulation matches across Easy/Normal/Hard, including overtime, duplicate-name-safe identity retention, pauses, rematches, recovery/query fixtures and exit cleanup. Wallball's existing production probe verifies fresh software-keyboard/NWH opening movement, a complete eight-truck win-by-two match, real returns/volleys, pause, rematch and its existing Experimental menu placement. Its additional multi-aspect UI captures are controlled renders, distinct from the environment run's actual backbuffers.

Full match observations are retained verbatim in `complete-match-observations.txt`:

```text
MATCH game=0 difficulty=Easy score=2,4 winner=1 goals=6 overtime=False recoveries=0 resets=6 strongContacts=171 counters=9 stickSwitches=80 peakTruck=24.66 peakPuck=21.30 realSeconds=70.35
MATCH game=1 difficulty=Normal score=5,2 winner=0 goals=7 overtime=False recoveries=0 resets=7 strongContacts=357 counters=21 stickSwitches=177 peakTruck=25.46 peakPuck=27.63 realSeconds=71.85
MATCH game=2 difficulty=Hard score=3,2 winner=0 goals=5 overtime=True recoveries=0 resets=5 strongContacts=554 counters=32 stickSwitches=271 peakTruck=24.92 peakPuck=24.79 realSeconds=71.93
MATCH game=3 difficulty=Hard score=3,5 winner=1 goals=8 overtime=False recoveries=0 resets=8 strongContacts=728 counters=49 stickSwitches=357 peakTruck=24.90 peakPuck=27.94 realSeconds=221.31
MATCH shots=33 physicalReturns=20 legalReturns=12 maxVolley=4 score=11:2 neutralReplays=0
```

The normal-speed Hockey observation is:

```text
OBSERVATION six-AI normal-time activeFrames=27857 activeSeconds=181.79 meanFrameMs=6.53 mainSamples=27857 meanMainMs=6.51 mainCounterAvailable=True GC0=10 managedDelta=19292160 GPU=NVIDIA GeForce GTX 1660 Ti context=Windows player · uncontrolled host load, no isolated GPU benchmark
```

The frame and main-thread marker values include frame pacing/host load; they are not isolated CPU or GPU timings. Managed-memory delta alone does not establish a leak. Separately, eight short-start blocks record480 whole-player intervals; structured values are in `short-frame-samples.json`. No human driving, physical controller/wheel, listening or collection-wide performance acceptance is inferred.

## Build

`Builds/TruckSports/TruckSports.exe` plus all companion files. Full payload SHA-256 `4f69fbc0b17443b65fdcfa52d6bb9cde5b743dfd93553da8ff925b491dc90d0c`; managed SHA-256 `f73d5592037ba3391eab39aeb1fef73116e15f5e4d4906f26653536e83718675`; 549 payload files and60 saved build scenes including the menu. The exact301 owned compiled sources are archived with local evidence. The preceding accepted build remains in Recovery.

## Remaining work

Cheese/Base still have their three original forecasts and require the full registry integration. Indoor/show and protected-mood exceptions, remaining venue lighting/reflections and recorded-replay headlights remain separate acceptance work. Snowy morning colors remain too warm in these captures, and full exposed-surface snow accumulation/tyre tracks were not authored for these courts. Hockey's existing close action framing is preserved; these checks do not replace the recorded human goal/framing feedback or establish all human/device combinations. Shared score publishers beyond Sumo and the new global results presentation remain unfinished. Carnival is still queued after Q22-SKY.

## VENDOR ASSETS AUDITED

Installed Weather Maker prefab/profile/day-night/URP APIs, existing court match/physics/camera owners and renderer configuration, NWH/WheelController integrations, Heat dropdowns, Toon Gas Station fixture model and existing Weatherade boundaries.

## VENDOR ASSETS USED

Weather Maker registry/sky/clouds/precipitation/wind/lightning/sun/moon; Heat selectors; existing NWH Wallball wheels and Sumo test vehicles; original Hockey physics; Toon Gas Station model. No new vendor source dependencies are published.

## RELEVANT ASSETS NOT USED

Weatherade surface masks/accumulation and tracks were not newly authored. Existing vehicle/input/camera/rule systems were retained. No substitute weather or vehicle physics was created.

## CUSTOM SYSTEMS CREATED

Read-only LWS lifecycle bridge for two existing match authorities; existing vendor adapter, light coordinator, additive tooling and development validation were extended.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
