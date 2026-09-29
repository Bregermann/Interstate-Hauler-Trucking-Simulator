# Truck Taxi Addendum Validation

Date: 2026-09-29. Unity: 6000.4.10f1. Platform: Windows x64 development player,
Direct3D11, NVIDIA GeForce GTX 1660 Ti. Starting task commit: `2395c17d`.

## Acceptance Status

Implementation and focused automation are available. **Human Windows SEE / HEAR /
DRIVE acceptance remains REQUIRED.** Do not interpret this report as release approval.
Existing unrelated working-tree changes were preserved. Radio mixer routing is
explicitly deferred by the user. Voice generation/reference assignment was not run.

## Data and Automated Tests

71 focused EditMode cases passed across these fixtures (results in `Logs/<name>.json`):

| Fixture | Passed |
| --- | ---: |
| TruckTaxiTrafficAddendumTests | 4 |
| TruckTaxiRaceTests | 4 |
| TruckTaxiRaceSafetyTests | 6 |
| TruckTaxiFastTravelTests | 10 |
| TruckTaxiExtremeDynamicTests | 10 |
| TruckTaxiKaijuScheduleTests | 4 |
| TruckTaxiKaijuCaptureTests | 4 |
| TruckTaxiRoadPepTests | 4 |
| TruckTaxiCabOccupantRigTests | 8 |
| EdenboroughTrafficDataTests | 4 |
| TruckTaxiAddendumContractTests | 7 |
| TruckTaxiKaijuTrafficTests | 6 |

The Python import suite passed 6/6:
`C:/Python313/python.exe -m unittest discover -s Tools/GeoImport/Edenborough -p test_*.py -v`.
No runtime test depends on an external OSM download.

## Unity Runtime Tests

The following ten distinct PlayMode scenarios have focused coverage. Results are
aggregated across focused runs, not represented as a single all-green full-suite run:

- Natural calendar scheduling of tornado, earthquake, avalanche and mudslide.
- Critical/secondary HUD alert separation and positive control dimensions.
- Rejected travel preserves an accepted pickup, fare, statistics and player pose.
- All five forced extreme events materialize near the loaded player and clean up.
- Practice race cleanup releases only its own offer suppression and permits travel.
- Six radio stations cycle; off/on and selected station behavior remain coherent.
- A calendar-due kaiju sighting starts without the force-event entry point.
- Authored kaiju props can be grabbed/thrown and their original state restored.
- Kaiju near/far presentation does not create an invisible blocking wall.
- The deployed NWH Taxi tractor moves/steers on generated Edenborough EasyRoads.

Evidence: `Logs/TruckTaxiAddendumPlayModeTests.json`,
`TruckTaxiFastTravelRejectedPlayMode.json`, `TruckTaxiNaturalKaijuPlayMode.json`,
`TruckTaxiKaijuPlayModeTests.json`, `EdenboroughNwhMovementPlayModeTests.json`,
and `Logs/TruckTaxiHudFinalPlayMode.json` (1/1 passed after the final layout repair).
Earlier failed fixture outputs remain available.
The first combined run had a fast-travel fixture failure because accepting a ride
correctly resumed physics; the corrected fixture pauses before measuring preservation.
Earlier test-runner scene cleanup and inactive/path-named UI fixture errors were also
corrected rather than suppressing gameplay errors.

Natural event evidence uses accelerated semantic game time, not a multi-day human drive:
earthquake on June 3, tornado June 4, avalanche June 5, mudslide June 6; all near-player
fallbacks were within 300 m. Natural kaiju occurred June 4 at 09:00 after 65 advanced
game hours in Town01.

## Edenborough Evidence and Boundary

OSM normalization retains source hash, ODbL attribution, metric coordinates, stable
way/lane/turn IDs and left-hand semantics. The extract contains 273 selected ways,
2,259 lanes and 2,868 turns across four planned regions. Only R_0_0 was authored:
58 EasyRoads objects, 552 lanes, 705 turns. Five unsupported grade-separated ways
were excluded rather than flattened into false connections.

The PlayMode test reloads the generated scene and drives
`Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab` using public NWH input:
3.01 m forward, 1.93 m/s peak speed, 2.74 degrees wheel steering and EasyRoads wheel
contact in 147/147 observed physics samples. No transform/velocity driving shortcut.
The older `IH_PlayerTruck_NWH.prefab` produced a PrefabInstance/PPtr import error and
was not repaired or used for the passing test.

This is not a complete route, junction, trailer or hardware-input test. The district
is not enabled in the Truck Taxi Windows build; live traffic/streaming activation,
terrain/junction refinement, visible attribution and district performance remain work
for world authoring. See `Tools/GeoImport/Edenborough/README.md`.

## Collider Review

`Logs/TruckTaxiSolidWorldColliderReview.md`: 502 solid colliders reviewed in 12 Taxi
regional scenes, zero flagged after the targeted migration. Known invisible town
perimeter walls had disabled renderers but live colliders; only those known prototype
blockers were disabled. Triggers/disabled objects/legitimate visible terrain are excluded.
This heuristic is not proof that every accessible perimeter is drivable.

## Windows Build and Probe

Build method: `LWS.TruckTaxi.Editor.TruckTaxiDemoBuilder.BuildWindows()`.
Artifact: `Builds/TruckTaxiDemo/TruckTaxi.exe`.
Probe argument: `-truck-taxi-addendum-probe`, without batchmode/nographics.
The development-only probe owns its temporary offer suppression and restores radio,
ride-request and shortcut preferences plus display mode before unloading/quitting.
It measures stationary gameplay, not human steering or an entire race.

First run retained at `Builds/TruckTaxiDemo/Validation/Addendum_20260929_203029_048`:
zero runtime errors, but FAILED fixture acceptance. A natural earthquake contaminated
the baseline, a fresh shift could not afford the race fee, and the initial hidden-window
swap chain produced blank 1080p captures. Later resolution captures rendered normally.
The corrected probe clears events before the baseline, waives the entry fee only in its
disposable race fixture and restores it immediately, and resizes before sampling.
This does not claim payout/finish acceptance. Screenshot review found and repaired the
new race-registration button overlapping existing recovery/GPS controls, and a narrow
Planning command. Failed evidence is not deleted.
The subsequent runtime run completed successfully, but screenshot inspection revealed
race timing and the radio card obstructing the default Compass area. These two new
panels were repositioned; Compass settings and GPS placement were not changed.

### Final Build / Runtime Evidence

Final build: **Succeeded, 592,124,664 bytes, zero errors**, recorded in
`Logs/TruckTaxiAddendumEditor3.log`. The final player run is
`Builds/TruckTaxiDemo/Validation/Addendum_20260929_204537_077/TruckTaxiAddendumRuntimeProbe.json`.
It completed in 55.57 seconds with exit code 0, no runtime errors, no fixture failures,
and confirmed preference/display restoration. `Logs/TruckTaxiAddendumPlayerVerified.log`
retains the player log. The normal gameplay entry point is unchanged; the probe runs
only with its explicit argument in a Windows development build.

Eight-second samples at actual 1920x1080, unpaused and with the requested phase present
on every sampled frame (different regions/populations; not comparable workloads):

| Phase | Frames | Mean ms | P95 ms | FPS | Main ms | Physics ms | Peak live actors |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Town01 | 437 | 18.34 | 22.93 | 54.5 | 18.29 | 0.61 | 18 cars, 180 pedestrians, 0 rivals |
| Union Field event | 640 | 12.51 | 14.89 | 79.9 | 12.48 | 0.27 | 9 cars, 105 pedestrians, 2 rivals, 48 crowd |
| Thunder Bowl truck race | 715 | 11.20 | 13.90 | 89.3 | 11.18 | 0.15 | 5 UTS opponents, all moved >1 m |
| Earthquake at speedway | 698 | 11.46 | 14.66 | 87.2 | 11.44 | 0.06 | 1 physics/visual event, no ordinary traffic |
| Kaiju at speedway | 743 | 10.77 | 13.43 | 92.8 | 10.75 | 0.05 | 1 actor, no ordinary traffic |

Logical background populations were 150 cars, 720 pedestrians and 12 rivals. These
short stationary samples are not worst-case destruction/traffic benchmarks or sustained
frame-rate acceptance. Other local Editor processes existed; no controlled pre-change
baseline was captured. Edenborough performance remains unmeasured.

Seven nonblank PNGs were captured: venue, race, earthquake, kaiju and HUD at 1920x1080,
2560x1440 and 3440x1440. Assistant screenshot review of the final race/1440p/ultrawide
frames confirms separated top alert lanes, readable Planning, radio below rather than
over the default GPS, and race timing to its right. The race frame is the 1080p check.
This is static screenshot review, **not human driving/listening acceptance**. The kaiju
frame/frustum count does not establish a readable creature silhouette or player threat;
the existing camera faces the track. Those presentation checks remain required.

Local implementation checkpoints: `33e03a78` (sleeper), `b1ff68cb` (Edenborough),
`b72f2ad2` (interdependent Taxi integration). Validation is committed separately.
No push was performed. Unrelated vendor, Interstate, Dead Air and passenger-pipeline
working-tree changes were not staged.

## Human Windows Checklist: NOT RUN

- Competent ordinary traffic in Town01/Town02/highway, deliberate passing, road rage,
  weather caution; rival truck bodies/drivers/pickup/dropoff/depot convergence.
- Listen to full horn clips, spatialization/cooldowns/concurrency; radio genre quality,
  interior/exterior sound and bulletin interruption/resume.
- Mushroom world wave on cab/exterior cameras, readable stable HUD/GPS, clean expiry.
- Travel to every major place and arbitrary road point, including an attached trailer,
  uneven ground and blocked destinations; no fare/shortcut exploits.
- Drive Neon Yard and other venue/service/mountain perimeters; no invisible collisions.
- Complete practice/time trial/truck/elimination/chaos/event races, real payouts and
  request restoration. A short AI movement probe is not race-completion evidence.
- Observe natural/forced extreme presentations, Hurricane preservation, fallback slides,
  earthquake threat and funnel readability while driving.
- Kaiju silhouette/LOD, warning, grab/eat/throw/fire/building/player interactions and
  recovery under actual driving. Assign original licensed/project audio to its sound slots.
- Road Pep parked/town/highway/race use, real motion/collision difficulty, quality and
  comedown; world remains unpaused. Current daycab unaffected by disabled sleeper capability.
- Private-stop GPS and world circle; circle radius equals gameplay radius; parking-brake
  prompt inside the circle; companion Eject Passenger HUD button and input; ejection
  physics; private route/marker/circle cleanup; Ride Request suppression released on eject.

## Remaining Content / Configuration

- Radio group/RadioVolume routing remains pending by user choice; Master fallback and
  source volume are used. Six original synth beds and tone/text bulletins are prototype
  programming, not finished genre shows or recorded announcers.
- Kaiju is an original bounded prototype, not a finished animated/audio creature asset.
- Sleeper anchors require an eventual sleeper model; no daycab geometry replacement.
- Edenborough is deliberately one prototype district, not a finished production city.
- No controlled pre-change performance baseline exists. Measurements cannot establish
  improvement or absence of regression; Edenborough performance is not measured.
