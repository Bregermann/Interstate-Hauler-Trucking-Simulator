# Truck Taxi Regional Polish: Focused Validation

Baseline: `d1089e28` (regional expansion).
Status: IMPLEMENTED; focused automated/runtime checks passed; manual acceptance
items below remain unverified. This is not a claim of complete driving validation.
No voice generation; no changes to the Ride Requests default/preference.

## Executed Evidence

- Unity 6000.4.10f1 editor compilation: PASS, zero compiler errors.
- `TaxiPolish` EditMode category: 30/30 PASS before the build correction.
- Targeted final `TruckTaxiMapPolishTests`: 4/4 PASS, including one additional
  serialized shader-dependency regression test. Total distinct tests: 31.
- `TaxiPolish` PlayMode category: 2/2 PASS. Companion positioning is a deliberate
  kinematic/teleport fixture, with real Input System H/F and live ejected actors.
- Windows development build: PASS, zero errors, 550347351 bytes reported.
  Output: `Builds/TruckTaxiDemo/TruckTaxi.exe`.
- Final standalone `-truck-taxi-polish-smoke`: 40 checks PASS, zero failed checks.
- The requested single-build plan needed one corrective rebuild. The first
  standalone run found the map tiles missing because the runtime-only shader
  lookup had no authored build dependency. An explicit material reference in the
  tile catalog fixed it; the final screenshot shows unloaded regional geography.
- No full historic test campaign was run. No subjective audio pass was claimed.

Local evidence (ignored logs/build outputs, not source-controlled screenshots):
`Logs/TaxiPolishEditMode.json`, `Logs/TaxiPolishPlayMode.json`,
`Logs/TaxiPolishMapFixEditMode.json`, `Logs/TaxiPolishMapFixEditor.log`,
`Logs/TaxiPolishPlayerFinal.log`. The failed first player evidence remains in
`Logs/TaxiPolishPlayer.log`.

Viewed final captures: `Builds/TruckTaxiDemo/Validation/Windows_Polish_FullMap.png`
and `Windows_Polish_PrivateStop_0.png`. Full-map geography, the world circle,
parking-brake instruction and eject control are visible at 1920x1080. These do
not prove a cab-camera route, every UI resolution, or collision-free manual driving.

## Companion Acceptance Checks

- [x] Private-stop GPS objective/route and world circle share one target: standalone PASS.
  World circle visually observed. Cab and full-map private-route pixels still need review.
- [x] Private-stop rendered radius matches the authoritative gameplay radius: 9 m fixture PASS.
- [x] Inside-circle parking-brake prompt uses the current binding: visible `P`, PASS.
- [x] Normal Eject Passenger HUD button appears and its hold handler ejects: PASS.
- [x] Existing `F` hold-to-eject input ejects the companion: PASS. Gamepad not exercised.
- [x] Companion becomes a detached, active, non-kinematic physics actor: PASS.
  Existing velocity cap/collision-ignore/12-second cleanup reused; full trajectory and
  end-of-lifetime disappearance were not visually timed.
- [x] Private route, active marker and circle clear after eject: PASS.
  Stop/Restore Navigation also preserves the encounter. Legal pickup blocks an
  existing service target, so the tested flow has no prior service route to restore.
- [x] Offer suppression releases with Ride Requests OFF and ON preserved: PASS.
- [x] Thirst stays unresolved after eject: PASS.
- [x] Normal `H` horn pickup and boarding: PASS in the positioning fixture.
- [ ] Normal successful private-event completion: manual regression review remains.

## Integrated Checks

| Area | Executed result | Remaining acceptance |
| --- | --- | --- |
| Offers | Cold/warm standalone offers: 7 route queries each, 5-6 frames, no scenery loads; EditMode bounded-job checks pass | Long-session statistical ratio |
| Intercity | Authored 10% default; three-local-offer cooldown and no local fallback to intercity tested | Statistical sampling while driving |
| Patience | Pickup countdown/suspension, 600 seconds of good-progress behavior, sustained wrong-way depletion pass | Real signal/congestion edge cases |
| Cancellation | Partial fare/completed-goal retention and no completed-fare increment pass | Drive to safe drop and review result UI |
| Diversions | Offer/accept/decline and authored rewards pass | Full optional-stop route/return loop |
| Sketchy stop | Completion blocked until controlled presentation is ready | Observe exit/exchange/dance/reboard and interrupted cleanup |
| Occupant scale | Lossy-scale compensation and distinct secondary seat pass | Paired cab fit across body types, posed driver and inside-camera head suppression |
| Waiting revenge | Sensor/fallback physical actor test and pickup-timer suspension pass | Real truck hit, recovery, return projectile and boarding |
| Map | Correct +/- convention, typed icon mapping, persistent tiles and Stop/Restore Navigation pass; geography capture viewed | Legend paging/controller review, cab-route pixels, dense overview icon readability |
| Services | Authored bay/radius and <=1 MPH checks, dine-in hunger/inventory behavior pass | Store error messages, restaurant popup/EAT HERE, damage-bar repair UX |
| Walls/snow | Five perimeter renderers disabled per town; registration refresh implemented | Drive perimeter; observe snowfall on freshly streamed/material-changed roads |
| Powerups | Production 360/300/420-second defaults, bounded refresh and separate short dev mode pass | Subjective duration/strength balancing |
| Rage/audio | Invalid/bounded NPC throws tested; spatial World mixer routing configured | Listen near/far; observe rare extreme event. Optional angry-bark clip unassigned |
| Transit | Logical route projection and UTS initialization continuity tests pass; three routes appended | Ride alongside real buses and observe Wobble stop exchange |
| Town 02 circle bus | Existing route retained by additive authoring | Full loop driving observation |
| Speedway | Layout/banking/ordered-checkpoint tests pass; scene included in build; map tile visible | Drive access road, banked lap, pits/curbs and streaming boundaries |
| Dialogue | 43 additive stable-ID authored lines across 25 profiles; authoring idempotence tests pass | All character/context coverage and subtitle timing |
| Pooling | Visibility/relevance grace rules tested; 150/720 logical budgets verified | Real moving-camera pop inspection through region transitions |

## Performance Sample

Final standalone stationary Town 01: **77.2 FPS**, **12.94 ms main**,
**0.47 ms physics**, 21/150 active/logical cars and 180/720 pedestrians.
Earlier regional baseline was 80.9 FPS / 12.35 ms / 0.45 ms. This sample is
about 4.6% lower FPS, not evidence of a performance improvement or a full-route
benchmark. It remains well above the 20 FPS focused smoke floor.

Offer timings, cold then warm: 90.6 / 69.9 / 67.9 ms wall time, spread across
frames. Largest indivisible step: 7.44 / 5.17 / 4.99 ms. The 3 ms scheduler
budget cannot interrupt an individual existing planner call; it yields between
calls. No one-second freeze or scenery load occurred in these three offers.

## Scope and Handoff

Attributable Taxi code, assets and documentation are committed locally in logical
groups. Unrelated vendor, Dead Air, Interstate, package/settings and prior voice
validation changes remain outside those commits. Only the Speedway entry is
staged from the already-dirty EditorBuildSettings file. No push.

- `4eb4ee0e` - Polish Truck Taxi rides companions services and regional presentation:
  runtime, tests, authoring tools, map tile resources and typed map icons.
- `d10f6236` - Author Thunder Bowl Speedway transit stops and regional ride content:
  scenes/streaming metadata, three bus routes, authored diversions and dialogue.
- The following documentation-only commit records this report and the architecture.

The remaining manual rows above are the next focused acceptance session, not a
request to rerun historic suites or generate voices. No feature is marked visually
passed merely because it compiled or had a data-model test.
