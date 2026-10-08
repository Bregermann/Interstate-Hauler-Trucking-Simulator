# Truck Taxi Foundation Acceptance Ledger

Updated: 2026-10-08. Scope: Truck Taxi inside the Interstate: Hauler host project.

## Decision

**NOT FEATURE-FROZEN. TASK INCOMPLETE.**

The current request requires Phase 0 human-bug stabilization before follow-on
feature completion. The implementation checkpoint below is useful and builds,
but it does not satisfy that gate. Do not begin production-scale world building
or treat the Windows checkpoint as the completed foundation release.

## Phase 0

| Item | Current evidence | Remaining acceptance |
| --- | --- | --- |
| Performance | Ten visible/focused moving workloads completed; town mean 12.27ms/P95 16.17ms; rain 14.60ms/P95 17.97ms. Timed Compass improves the earlier same-binary comparison. | Current direct Compass CPU attribution, GC allocation attribution, long-session growth and human playtest. |
| Save/load inactive softlock | Startup completion is observable; failed startup exposes Retry, Save/Load and Main Menu instead of a dead Start Shift. Retry reuses current persistence. Optional fuel amount now roundtrips with bounds validation. | REQUIRED: two actual save/quit/relaunch/load cycles, restored needs/inventory/controls/GPS/UI. Not executed. Safety review blocked a restart probe that could write normal profile slots; approval for a named disposable profile is pending. No replacement save architecture/probe was installed. |
| GPS route recovery | Coalesced unscaled one-shot map/route refresh, unchanged-intent repair, vendor auto-clear disabled; actual cab route-pixel test passed. Offer preview drag clamp fixed. | Original sleep/time-skip/temporary service/companion/restart sequence in Windows. |
| Regional map | Exclusive atlas capture through Compass events, camera settings restored; ten rendered open/close cycles passed. | Human sequence including sleep, route change, GPS toggle and fresh-process restore. |
| Invisible wall | Exact NWH impact-path diagnostic and authoring bounds/visibility checks; generated-facility teardown fixed. | Reported location/collider not identified. Need place name/screenshot, actual reverse impact and targeted reproduction. No blanket collider removal. |
| Flashing road/ground | Validator flags duplicate/coplanar candidates; stale generated-art cleanup fixed. | Exact location/pair not reproduced; toggle candidates individually and prove culprit before changing authoritative geometry. No depth hack applied. |
| Duplicate hotel | Explicit ride/service identity link canonicalized for presentation only; stable save/fare IDs preserved; regression tests passed. | Human repeated streaming/map check. |
| Fare plus companion | Primary passenger role separated from certified secondary seat; no invented center perch; busy/capacity guards; regression coverage. | Human fare+companion interaction and occupant placement in Windows. |
| Rival passenger stealing | Session-owned secondary fare, exact rival generation, strong-impact adapter, seat reservation, route lease, one-time payout, angry feedback. Tests cover invalid capacity, reset and reentrant callbacks. | Actual collision, boarding presentation, delivery, payout, route restoration and companion coexistence in Windows. |
| On-foot access | G exit prompt/block reason, controls entries, full-map chord guard and modal focus repair. Actual G exit/enter PlayMode test passed. | Windows park/exit/walk/look/sprint/interact/re-enter/drive and gamepad. Input test holds tractor kinematic; it is not driving physics proof. |

## Validation Checkpoint

- Unity 6000.4.10f1 / Windows development build: successful, zero errors.
  `Builds/TruckTaxiDemo/TruckTaxi.exe`; `Logs/TaxiFoundation-Phase0-WindowsBuild2.log`.
- Integrated EditMode attempt: 922 total, 917 passed, three failures, two skipped.
  `Logs/TaxiFoundation-Phase0-EditMode.xml` preserves the evidence.
- Two failures were addressed: ordinary MonoBehaviour teardown moved to PlayMode;
  obsolete fabricated center-seat expectation changed to the certified-seat rule.
- The remaining explicit themed-roster fixture fails on the pre-existing dirty
  `analytical-robot` visual asset. That asset/profile/test was not changed by this
  task and the failure was not suppressed. Do not report the full suite green.
- Focused rerun: 83/83 passed, zero skipped
  (`Logs/TaxiFoundation-Phase0-EditMode2.xml`). GPS recovery, occupant polish,
  facility diagnostics, rival theft and persistence fixtures.
- Rendered PlayMode: 5/5 passed
  (`Logs/TaxiFoundation-Phase0-PlayMode4.xml`): cab/map presentation, ten atlas
  captures, G exit/enter, exact-instance facility teardown, existing breakaway test.
- Performance: `Logs/TaxiFoundation-Phase0-Focused.json` and `*FocusedPlayer.log`.
  All ten moving samples focused; player exited 0. The probe stages between
  workloads, so it is not the uninterrupted sandbox acceptance or a soak test.
- No human acceptance, fresh-process persistence acceptance or final foundation
  build is claimed. The build is a Phase 0 checkpoint.

## Remaining Work After The Gate

This is a continuation list from the single orientation, not permission to replace
working systems. Extend existing owners only; recheck the relevant source as work
resumes.

| Requested area | Existing foundation / genuinely remaining work |
| --- | --- |
| Sandbox and continuous truck-life loop (1-3,7,40-41) | Existing on-foot/services/needs/runtime world. Prove one compact reachable example per loop, the uninterrupted 30-step test and one-hour soak. No final world expansion. |
| Stores/inventory (4-5) | Existing wallet, cart and needs inventory. Register-proximity checkout, useful scrollable item categories/icons/details and separate select/use interaction remain. |
| Fuel/truck stops (6-7) | Existing NWH FuelModule/manual fueling; optional fuel save state added. Prove combined-stop reachability and restart consumption/restore. |
| Diners/motels/truck sleep (8-10) | Existing services and choices; modal focus fixed. Separate diner drink selection and actual sleep/time/GPS/restart acceptance remain. |
| Restrooms/bowel/toilet collection (11-13) | Existing needs/used-ID state. Remove completion dependence on generated ID suffixes, cover authored fixtures and all-used collection event. |
| Ambient encounters/effects (14-15) | Existing world-life/temporary-effect owners; runtime sequential acceptance still required. No new competing effect/inventory authority. |
| Damage/tires/inspection/repair (16-20) | Existing centralized component damage. Explicit flat/failing presentation, looked-at part inspection, component-specific service capabilities and focused handling consequences remain. |
| Destruction (21-22) | Existing static-to-dynamic breakaway props. Classify porta-potty pieces, repeated-hit durability, global active-body/debris budget and safe retirement. Preserve structural collision. |
| Weather/snow (23-24) | Weather Maker/Weatherade kept. Snow-track Windows asset retention committed; all-weather/snow human straight/curve/persistence/dry-surface checks remain. |
| Rivals/companions (25-26) | Phase 0 changes above; complete actual pickup/delivery/eject/cleanup/suppression and collision acceptance. |
| Generic location/input/units (27-29) | Existing composable service/ride identities, input/modal and US units. Multiple bays, hours, interiors/ambient/fixture references and full player-facing audit remain. |
| Central diagnostics (30) | Existing developer surfaces; consolidate sections and development-only access without adding another manager. |
| Persistence (31) | Pixel Crushers remains authority. Attached trailer saves are STILL explicitly guarded; deterministic fresh-process trailer identity/placement/recoupling must precede removing that guard. Required restart tests pending. |
| Minimal Career (32-37) | NOT implemented in this pass. Reuse existing profile ID, wallet, stats, dialogue/guide, truck definition, NWH tuning and accessory ownership. Add only missing persistent progression/purchases/equipment and Career UI through existing save payload. |
| Authoring tools (38) | Existing world builder presets; add missing combined-location composition and expose current specialized tools, not a second builder. |
| World validator (39) | Existing service validator extended with stable IDs, hidden/oversized solids, duplicate/coplanar candidates and conservative bay clearance. Floating/buried/entrance/destructible classification and aggregation of existing route/seam checks remain. |
| Feature freeze (42) | BLOCKED by earlier rows. No systems declared frozen by this report. |

## Human Acceptance To Retain

- Two isolated-profile Windows save/quit/relaunch/load cycles with needs, inventory,
  fuel, input, UI, GPS and session continuation; do not corrupt real player slots.
- Exact invisible-wall reverse contact and road flicker camera/location reproduction.
- Route after sleep/time skip, pause, temporary routes, GPS/Planning/full-map toggle.
- Companion private-stop GPS and world circle; visual radius equals gameplay radius;
  parking-brake prompt; Eject Passenger HUD/input/physics; route/marker/circle cleanup;
  Ride Request suppression released after eject.
- Correct primary/secondary seats, rival actual-hit stealing and exactly-once reward.
- Windows on-foot keyboard/gamepad loop and all remaining sequential sandbox systems.
- Meaningful snow tracks straight/curved, retained after stopping, absent on dry roads.

## Ownership And Vendor Report

- VENDOR ASSETS AUDITED: installed Compass Pro, NWH physics/coupling/fuel/collision,
  Pixel Crushers persistence, Weatherade snow tracks, Weather Maker, UTS and existing
  project adapters/authoring tools. No second broad vendor audit is required.
- VENDOR ASSETS USED: the same authorities, public Compass capture/route APIs,
  NWH collision/fuel interfaces, existing Input System and registered save service.
- RELEVANT ASSETS NOT USED: vendor replacement routing/save/vehicle frameworks;
  Chatterbox/voice generation, proprietary console APIs and final world generation.
- CUSTOM SYSTEMS CREATED: Taxi-specific secondary rival fare coordination/collision
  relay; bounded development collision/performance diagnostics; focused regression
  tests. Existing authorities, GPS/UI/persistence/validator extended.
- VENDOR SOURCE MODIFIED: NO by this task. Pre-existing vendor/package edits remain
  in the working tree and are excluded from these local checkpoints.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
- PUSHED: NO. Unrelated Dead Air/shared-host/package/art changes remain untouched.
