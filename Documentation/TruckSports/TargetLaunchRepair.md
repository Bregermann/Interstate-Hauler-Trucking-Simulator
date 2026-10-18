# Target Launch repair — October 18

Status: accepted implementation/native/Windows milestone, with manual-device and human-playtest limitations below. The larger queued release is not complete. Git delivery is recorded separately in the generated build GIT-DELIVERY.txt and ignored Logs/TruckSports/TargetRepair/commit.json.

Windows x64 Development;35 explicit Sports-only scenes; TruckSports startup;0 errors /47 warnings. Built 2026-10-18T15:01:32.476217+00:00. Managed SHA256 `d9c7c7d3a10cb729e9ad23e064b910b3534ff3f79bd305ee78d1aecb1fde1dcb`;438-file data manifest `58f5c5c4a96e42a0672c13b60d5a10f32e35ec45a5893e2450d1ae5875e72ff1`.

## Cause and behavior

TargetRacer previously called the authoritative score lock at the first descending top contact. Its sweep fallback also converted a contact into a banked result. That made later slides, tumbles and falls irrelevant. The old quarry miss floor did not provide the requested volcanic environment.

The first target contact now ends propulsion and aftertouch while retaining the real dynamic body. A truck must be supported by the actual target top, with linear speed at most0.12m/s and angular speed at most0.15rad/s, continuously for1.8seconds. Renewed translation/rotation or loss of contact resets that interval. Sleeping PhysX contact remains valid. All unresolved simultaneous participants must be stable before their final chassis-center snapshots lock together. Existing ring sizes/values, launch course, NWH handling, dive/lift, horizontal aftertouch, five launches and tie rules remain unchanged. Upside-down support is valid. Sixty simulated seconds of genuine persistent post-contact motion resolves zero rather than banking the first ring. Separate approach/flight/world-escape recovery remains bounded.

The existing finite1.6m/s aftertouch budget also gates modest wing pitch/yaw/bank torque. Held launch acceleration does not spend range/pitch correction until released. Commanded angular correction is bounded; collision angular velocity is never erased. The truck remains ballistic, with the existing downward gravity and existing dive behavior. Target contact permanently ends flight correction.

## Environment and presentation

The saved scene reuses the existing Terrier volcanic terrain, molten lake, vents, smoke and ember presentation through a sport-owned prefab. The terrain data is copied into Target ownership, with identical height samples and matching TerrainCollider reference; only its visual layers change. Installed NWH cliff maps and River Modeler ash layers provide the owned surface materials. Installed Vista rock models replace repeated flat quarry clusters on the distant crater. Original vendor assets and Terrier scene remain unchanged.

Actual lava contact or swept downward lava-plane crossing emits one splash, a Cinemachine impulse and twelve pooled real truck mesh fragments. Crossing the target boundary merely continues the fall. Fragments collide cosmetically with the course and cannot decide scores. The existing TerrierWreckPool accepts explicit source trucks, external replay ownership and an optional cosmetic minimum upward ejection. Target uses9m/s so a fast downward miss produces visible airborne debris rather than immediately sinking all pieces below opaque lava; the disabled default preserves Terrier behavior. The hidden destroyed authority retires after the real lava strike while its fragments remain dynamic. Owned Cinemachine impact groups frame each human miss and the completed broadcast/replay impact instead of tracking the invisible falling authority. Target replay records four fragment groups alongside the original four trucks and target, with render-only ghosts and existing camera/speed/rewind/skip controls.

A scene-owned URP HDR profile uses ACES, restrained bloom and contrast. Final awarded rings pulse briefly through DOTween. Compact per-slot cards use authoritative values, distinguish SETTLING/FINAL/MISS and show the stability progress. The saved scene and explicit authoring defaults agree. Normal SportsBuilder.Build validates saved scenes and does not regenerate them.

## Validation so far

- Native saved/reloaded scene audit passes: owned terrain/collider references,1089 unchanged height samples,28 installed Vista outcrops, obsolete flat clusters disabled, nine replay sources and48 real fragments.
- Fourteen existing deterministic rule assertions pass for zone boundaries, ownership, once-only awards, rounds, ties and solo progression.
- Final physical Play Mode acceptance passed71 checks: real landings, inward/outward ring slides, upside-down rest, stability reset after impulse, simultaneous finalization, late fall/lava/real debris, persistent-motion zero, rematch, limited wing controls, pause and cleanup. The final-source Windows rerun also passes71 checks.
- Measured physical slide examples:10→25,50→100,100→50 and25→25. Wing input produced about29degrees of combined attitude correction over2.4simulated seconds, with gravity and forward flight preserved.
- Rejected legacy runs are retained in Logs/TruckSports/TargetRepair/Native. The old1500m/s test expected an immediate bullseye; actual PhysX contact rebounds upward at about224m/s. It now checks real CCD contact and no premature score/freeze; normal-speed fixtures separately require final10/25/50/100 results. No fake supported score or collision retuning was introduced.

No human playtest, physical controllers, wheel hardware, haptics, perceived audio mix or release readiness is claimed. Exact-build runtime and sustained rendered samples are recorded below.

## Changed integration

Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity; TargetRacer,TargetMatch,TargetVolcano,TargetLavaSurface,TargetScoreFeedback; additive TargetRepairAuthoring and regeneration defaults; owned environment/material/terrain assets; development acceptance probes. Shared changes are limited to TerrierWreckPool's optional source/replay adapter and configurable defaults in the LWS lava shader. Their default behavior must pass regressions in the new build.

## Vendor accounting

- VENDOR ASSETS AUDITED: NWH WheelController/VehiclePhysics2 AirSteerModule, EasyRoads3D, Cinemachine2.10.7 impulse/group/replay integration, Weather Maker, URP17.4, Vista sample terrain/rock prefab, River Modeler terrain layers, DOTween1.2.825 and Heat/uGUI.
- VENDOR ASSETS USED: existing NWH wheels and EasyRoads ramp unchanged; CinemachineImpulseSource/Listener and existing group cameras; Weather Maker through SportsChaosWeather; VistaSample_DesertRock.prefab; NWH cliff textures; owned copies of River Modeler layers; URP VolumeProfile/ACES/Bloom; DOTween wing/ring animation; existing Heat menus/input/settings and existing Terrier physical fragment pool.
- RELEVANT ASSETS NOT USED: NWH AirSteerModule assumes a full aircraft-style VehicleController authority. This sport retains its existing wheel adapter and finite correction budget; the small bounded torque is sport semantics. Scene Streamer/Pixel Crushers are unrelated to this self-contained match repair. No substitute terrain/road/weather/UI framework was added.
- CUSTOM SYSTEMS CREATED: sport-specific supported-rest progression and acceptance, an actual-lava bridge to existing effects/pooling, final-ring feedback, additive scene installer and owned visual variants.
- VENDOR SOURCE MODIFIED: NO. The configurable lava shader is existing project-owned LWS code; previous material defaults remain0.19/0.075.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

First Windows candidate47e8/5d0fe passed 68 supported-rest/wing assertions,52 focused checks, three complete four-AI five-launch matches, six real-input speed trials and Terrier finish fixtures. Render inspection rejected its impact presentation; final acceptance requires the corrected camera/debris candidate. Its raw evidence is retained separately and is not claimed as the final build. Initial AI totals: Easy35/135/160/80, Normal155/145/85/110, Hard80/120/200/70; these are small functional samples, not difficulty-distribution claims.




## Exact-build Windows acceptance

All13 sequential Windows suites pass, with fresh result files, verified process exit, matching assembly/data-manifest receipts and no detected runtime exceptions or shader errors. These are automated physical simulation and injected input checks, not human playtesting.

- Target supported rest/wing/lava/debris/camera acceptance:71 checks; actual12 visible physical fragments above lava.
- TargetFocused:52 input, NWH runway, physical zone, replay, shared-stick/reconnect and lifecycle checks.
- Three complete four-AI five-launch matches, one per difficulty, plus six genuine keyboard speed trials. All six final-build trials physically departed the ramp; undershoots/overshoots were legitimate lava misses. Middle approaches produced final50/50/10 point outcomes. No score or launch-velocity injection in these trials.
- Terrier finish chronology/edge scrape/real wall slam/high-speed/locked-result fixtures pass. Complete12-truck first-to-five physical match:33 heats, winner slot11, wins1/4/4/0/1/4/2/4/3/1/2/5. Shared debris defaults remain intact.
- TugFocused passes; existing LWS lava material defaults remain unchanged.
- Keepaway117 presentation checks/five aspect ratios and actual full-match/recovery/rematch regression pass.
- Shared cameras:2467 checks/20 modes. Participants:769 checks/34 sport scenes. No missing or duplicate scene owners/listeners.

Actual Target full-match samples:

```
MATCH difficulty=0 rounds=5 totals=35,175,160,110 attempts=5,5,5,5 winner=1 diveObserved=1 airContacts=4
MATCH difficulty=1 rounds=5 totals=105,145,80,110 attempts=5,5,5,5 winner=1 diveObserved=1 airContacts=4
MATCH difficulty=2 rounds=5 totals=10,85,110,185 attempts=5,5,5,5 winner=3 diveObserved=1 airContacts=4
```

These are three functional samples with different seeds, not a statistically established difficulty score distribution.

## Rendered performance and visual inspection

Two repeated60-real-second four-AI-view samples ran at1920×1080 after their screenshot phase. The first process launched at720p but the dual-resolution screenshot phase ended at1080p; its launch label is not a720p benchmark claim. Both samples include ordinary shared replay transitions; the logs report actual four-view/live/focus frame counts. Screenshot pauses are outside the measured interval. Host GPU: NVIDIA GeForce GTX1660Ti. Other host applications were not controlled or closed.

```
PERFORMANCE seconds=60 realtime frames=7628 meanMs=7.86581 p95Ms=12.51307 p99Ms=12.53357 fourViewFrames=5078 liveFrames=7628 focusedFrames=7628 rematches=0 resolution=1920x1080
PERFORMANCE seconds=60 realtime frames=7611 meanMs=7.8842 p95Ms=12.51417 p99Ms=12.53437 fourViewFrames=5085 liveFrames=7611 focusedFrames=7611 rematches=0 resolution=1920x1080
```

Actual player screenshots at1280×720 and1920×1080 were inspected for final scores, flight, impact fragments and four-view HUDs. The five-aspect focused captures are camera-only renders and component overflow checks, not full-window UI acceptance at every aspect. No hardware/controller/wheel/haptic/audio-listening or human-playtest claim. Remaining human checks: flight correction feel, cab/controller comfort where applicable, perceived mix and final art preference. This milestone does not implement the remaining Party/Fare Fight/Goddess/new-sport queue.

[Version-bound acceptance](TargetLaunchEvidence/Acceptance.json), [all suite receipts](TargetLaunchEvidence/WindowsSuites.json), [actual final score](TargetLaunchEvidence/final-rest-1280.png), [actual impact fragments](TargetLaunchEvidence/lava-and-fragments-1920.png), [four views](TargetLaunchEvidence/four-view-natural-flight-1920.png).
