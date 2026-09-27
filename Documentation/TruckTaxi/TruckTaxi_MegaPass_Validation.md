# Truck Taxi Mega Pass: Validation and Handoff

## Status

Implementation is integrated. Human Windows driving acceptance remains REQUIRED.
This report separates data correctness, automated runtime integration, and actual
player observation. Synthetic contact and layer-mask checks are not proof that
the real passenger collision or cab visibility feels correct while driving.

Baseline: Regional Polish commit `b9448576`. Unity 6000.4.10f1, Windows/D3D11.
No passenger voices or authored dialogue were regenerated. No vendor source was
edited for this pass. Existing unrelated working-tree changes are excluded.

## Implemented Changes

- Driver: all local Wobble driver renderers use the existing driver layer;
  every actual NWH interior camera excludes it, exterior cameras retain it.
- Waiting occupants: renderer-fitted capsule, canonical-truck contact and
  nonallocating overlap detection, 1.34 m/s impact threshold, light tumble,
  collision/pool restoration, same identity, one revenge throw per encounter,
  pickup-patience suspension. Pair members and companions use the same actor seam.
- Buses: physical box fitted to body bounds at initialization, excluding tiny
  accessories; shared traffic-vehicle impact attribution includes buses and rivals.
  Town 02 circle route is unchanged.
- Goals: authored common/medium/chaotic count bands and route opportunity,
  distinct targets, count-scaled reward, explicit deadline text, untimed
  ride-scoped objectives, failed/completed row fade and history retention.
- Scenic: unchanged offer frequency, 25 percentage-point Thirst relief,
  three-second render-only bounce and one-time completion. Sketchy eligibility
  diagnostics added without increasing global offer frequency.
- Work Area: pickup-only circle, four presets, custom center/radius, existing
  Compass full-map selection and controller/keyboard map-center workflow.
  Area snapshots are session semantics, not a new career save framework.
- Dispatch: real-second stochastic delay bands 5-20 / 10-35 / 25-75 /
  60-150 / 150-300 by demand. Intercity baseline 10%, minimum three completed
  locals after an intercity, next eligible intercity guaranteed after five
  completed locals. Declines/unroutable attempts do not consume completion state.
- Rivals: twelve logical, three live maximum from existing UTS pooling; roof
  signs, Wobble drivers, isolated visual rider pickup/dropoff. Player continuity
  and fare history remain separate.
- World: Union Field Sports Stadium, Neon Yard Concert Venue and Pinecrest
  Mountain Pass streamed scenes; existing Thunder Bowl integrated. Persistent
  venue/graph/stop metadata, two optional shuttles, crowd paths and fallback tiles.
- Calendar: sole LWS clock, fixed start 2026-06-01 16:00, weekly authored events,
  arrival/departure demand, occurrence-scoped weather delay/cancel, queued notices.
  Existing traffic/pedestrian budgets remain 150/720 logical.
- Fare quotes: captured event/weather/time/intercity modifiers, additive explicit
  bonus lines rather than hidden multiplicative stacking.
- Forecast: seven-day deterministic per-date weather schedule, seasonal weights,
  ten conditions, actual requests through the existing Weather Maker/LWS adapter;
  Weatherade remains the road-accumulation presenter. Tomorrow's published weather
  remains stable when the forecast window rolls forward.
- Extremes: authored tornado, hurricane, earthquake, avalanche and mudslide zones;
  nearby bounded VFX/player forces, quake camera/prop motion, visible slide debris,
  cleanup and rewind handling. NWH dust material is reused by fallback particles.
  Three nearby visual actors / two debris piles maximum; no distant physics scan.
- Planning Today/Events/Calendar/Weather/Surge UI, typed venue/surge/hazard map
  markers and circles, existing legend, development clock/weather/event/collider
  controls. No second minimap or population authority.

## Automated Evidence

- Editor compilation: PASS before final player build.
- `TaxiMegaPass`: **52/52 EditMode PASS**, final run 0.12 s.
  Raw result: `Logs/TaxiMegaEditModeFinal.json`.
- `TaxiMegaAutomatedIntegration`: **5/5 PlayMode PASS**, final run 26.60 s.
  Raw result: `Logs/TaxiMegaPlayMode.json`.
- After the final player-only callback-name correction: **9/9 extreme-event
  EditMode PASS**, 1.92 s. The five-test PlayMode run preceded this API rename;
  the final Windows probe exercised the corrected event API afterward.
- Real authored DemoCity repeatedly loaded; bootstrap/world ready, driver camera
  masks, Work Area mesh, live bus, offer expiry, assisted boarding and paused
  active-goal countdown checked. Existing offer modals intentionally retain
  real-time expiry while driving is suppressed; active goals freeze on pause.
- Authored venue start/end/fare demand, twelve logical rivals, all five extreme
  lifecycle starts/cleanup, dedicated UTS rival materialization/pickup request
  and player-state isolation checked.
- Primitive Rigidbody truck/capsule contact crosses the impact threshold. This
  is a physical fixture, NOT a real NWH drive into a visible waiting passenger.
- Scenic renderer restoration leaves the physical pose unchanged. Companion
  horn pickup, private-stop GPS, circle radius, dynamic ejection, route/circle
  cleanup and offer-suppression release checked in authored-scene Play Mode.
- 1,000 completed-selection POLICY simulation: 826 local, 30 random intercity,
  144 guaranteed intercity; maximum local streak 5. Includes 91 declined and
  59 invalid-path attempts. Three-local cooldown and actual small-graph session
  eligibility/Work Area/continuity cases are separate tests. This is not 1,000
  manually driven regional fares or a natural Sketchy observation.
- Planning Today off-screen 1920x1080 render inspected:
  `Logs/TaxiMegaPlanning.png`. Today/Weather inspected TMP content had no
  overflow. The batch Editor has no Game-window render target; temporary
  screen-space-camera capture is layout evidence, NOT normal player acceptance.

## Failures Found and Fixed During Integration

- Single scene reload left Scene Streamer's remembered regions loaded after
  Unity had unloaded them. LWS now trusts actual Unity loaded scenes and resets
  a wholly stale vendor registry through its public state APIs before reloading.
  No vendor file was edited and no replacement loader was added.
- Work Area/custom map graphics required an explicit CanvasRenderer. The shared
  graphic now requires it; overlays hide when the full map closes.
- Rival paint MaterialPropertyBlock could not be created in a MonoBehaviour
  field initializer. It is now allocated during Apply on the main thread.
- The first Windows probe exposed Unity treating `Start(kind, zoneId)` as an
  invalid MonoBehaviour lifecycle callback. Renamed it to `StartEvent` and
  updated callers, reran the focused tests, then rebuilt and reran the player.
  The final player log contains no script errors or exceptions.
- Test corrections: bus world heading is not always +Z; select the actual Work
  Area graphic, not an unused pooled hazard circle; close map in cleanup; preserve
  the existing offer-modal expiry policy. Initial failed runs are not counted as
  successful acceptance.

## Final Windows Build and Performance

- Development Windows x64 build: `Builds/TruckTaxiDemo/TruckTaxi.exe`.
- Final build: Succeeded, zero errors, 560,360,904 bytes reported by Unity.
  One corrective rebuild was necessary for the player-only callback warning.
- Final standalone log: `Logs/TaxiMegaPlayerFinal.log`.
- Raw measurements: `Builds/TruckTaxiDemo/Validation/TruckTaxiMegaRuntimeProbe.json`.
- D3D11, 1920x1080, eight-second samples after five-second warmup, no paused
  frames; valid main-thread and physics recorders. The opt-in automated probe
  completed with an empty error field and exited. This is not human driving.

| Sample | FPS | Main ms | Physics ms | Cars live/logical | Pedestrians live/logical | Rivals live/logical | Venue crowds | Buses | Extreme physics/visual |
| --- | ---: | ---: | ---: | --- | --- | --- | ---: | ---: | --- |
| Town 01 original spawn | 78.28 | 12.744 | 0.525 | 18/150 | 180/720 | 0/12 | 0 | 2 | 0/0 |
| Town 01 near quake zone, normal | 86.02 | 11.595 | 0.420 | 46/150 | 177/720 | 3/12 | 0 | 2 | 0/0 |
| Same location, earthquake active | 71.61 | 13.930 | 0.727 | 44/150 | 180/720 | 3/12 | 0 | 2 | 1/1 |

P95 frame times were 16.15 / 14.96 / 17.10 ms respectively. Regional Polish's
earlier spawn baseline was 77.2 FPS, 12.94 ms main, 0.47 ms physics, 21/150 cars
and 180/720 pedestrians. The new spawn sample shows no significant normal-state
regression in this short comparison; differing live populations prevent claiming
a controlled performance improvement. The quake sample adds about 2.34 ms main
and 0.307 ms physics relative to the same-location normal sample, with FPS down
about 16.7%. Actor timing varies, and this is not an all-disaster benchmark.

## Source Control

- `cd822bd9` - `truck-taxi: integrate ride demand rivals calendar weather and regression fixes`
- `73b8e669` - `truck-taxi: author regional venues mountain pass and hazard content`
- A separate documentation commit contains this report and the authoring guides.
- Local commits only; nothing pushed. Pre-existing vendor, Dead Air, Interstate,
  package/project settings and generated voice-validation changes were excluded.
  Only the three new streamed-scene entries were staged from the otherwise mixed
  `EditorBuildSettings.asset`; the other settings changes remain untouched.

## Known Limits

- Current road graph has no safe temporary edge-closure API. Hazards are visible,
  temporary and leave a navigable center. No invisible road wall, claimed GPS
  reroute, or false ROAD CLOSED marker was added.
- New venue/mountain geometry and fallback disaster particles are prototype
  presentation. Physical scale, colliders, crowd/bus exchanges and the actual
  human driving experience still need the checklist below.
- Optional angry/GPS/notification clips retain their existing assignments. This
  pass did not fabricate or generate voice audio.

## Human Windows Acceptance Checklist

All rows below are **NOT RUN in this automated session** unless marked separately
by the human tester. Data/runtime passes above do not mark these boxes complete.

- [ ] Cab clear ahead, left/right and every camera transition; full exterior driver.
- [ ] Real NWH hit on waiting passenger: contact, tumble, recovery, one jug, same
      identity and subsequent boarding; pair member and companion variants.
- [ ] Live bus contact front/rear/sides/corners, near visible body, generic ram goal.
- [ ] Multi-count 2+/3+ distinct targets; timer visible; expiration fade, reflow/history.
- [ ] Natural Scenic offer, Thirst relief, visible bounce with stationary physics,
      one reward; natural Sketchy offer and exchange/reboard flow.
- [ ] Actual cross-highway fare within guarantee; custom pickup area and outbound
      destination; controller/mouse selection and slider at both 1080p/1440p.
- [ ] Rival driving, visible pickup and dropoff without affecting player continuity.
- [ ] Stadium/concert/Thunder Bowl events, arrivals/departures, bus/ped crowd flows,
      quoted bonuses, announcement and Calendar state.
- [ ] Upcoming forecast actually matches Weather Maker; Weatherade roads remain
      correct on fresh streamed regions and material changes.
- [ ] Each extreme observed nearby: tornado, hurricane, earthquake, avalanche,
      mudslide; physical effects, visibility, cleanup and regional stream crossing.
- [ ] Private-stop GPS AND world circle visible.
- [ ] Private-stop circle radius matches gameplay radius.
- [ ] Parking-brake prompt appears inside private circle.
- [ ] Companion Eject Passenger HUD button works.
- [ ] Companion eject input works.
- [ ] Companion ejection physics visible and safe.
- [ ] Private route/marker/circle clean up after eject.
- [ ] Ride Request suppression correctly released after companion eject.
- [ ] Truck stability/reset, transmission, wheel/keyboard/gamepad, GPS, weather,
      road conditions, normal traffic, existing session/streaming still work.

## Vendor-First Report

- VENDOR ASSETS AUDITED: NWH, UTS, Compass Navigator Pro, EasyRoads,
  Pixel Crushers Scene Streamer/Save/Dialogue, Weather Maker, Weatherade, Heat.
- VENDOR ASSETS USED: existing authorities/adapters above; NWH particle material,
  UTS physical traffic pool, EasyRoads authored meshes, Compass map presentation,
  Scene Streamer chunks, Weather Maker/Weatherade presentation, Heat controls.
- RELEVANT ASSETS NOT USED: no new Scene Streamer loader, GPS, vehicle controller,
  save storage, spreadsheet or dialogue-generation framework.
- CUSTOM SYSTEMS CREATED: LWS work-area/dispatch policy, rival semantics,
  calendar/forecast, venue/demand, bounded extreme-event coordination and their
  existing-HUD/debug adapters; focused tests and opt-in Windows benchmark probe.
- VENDOR SOURCE MODIFIED: NO by this pass; pre-existing vendor dirt is excluded.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
