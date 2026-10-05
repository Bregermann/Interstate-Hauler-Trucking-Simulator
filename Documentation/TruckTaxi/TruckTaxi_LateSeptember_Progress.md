# Late-September Systems Pass

Source request: attachment `557bc09b-0a32-4499-b33f-b78e7a172da9`.
Content-hook addendum: attachment `c1a738b9-0080-4cff-8081-d266bf3c74b2`.
Live event integration clarification: attachment `080c080f-7c42-4274-ac1a-7ca88b4d0aa5`.

## Implementation State

- Passenger dispatch now tracks stable-ID exposure and recent archetypes, supports authored eligibility and decline cadence, and persists its selection/intercity policy state. The four static 1,000-offer simulations in `DispatchAudit/dispatch-simulation.json` cover all 114 eligible profiles; Captain Applause is present without a special forced weight. Live route availability remains a separate condition.
- Explicit gender metadata is **not complete**: the roster audit reports 37 male, 29 female and 48 unresolved profiles. The 48 have no sufficiently explicit authored casting value. They are listed in `DispatchAudit/dispatch-simulation.json`; they require designer assignment, not name-based guesses. The runtime appreciation gate requires an explicitly adult, opted-in female and uses the central 33% chance; males and unresolved profiles are ineligible.
- Stores use the current catalog and scrollable UI, and the fresh VIBIN' run grants the authored starter stock. Needs include fatigue, timed sleep and drink relief. Normal driving fatigue gain is 0.044 per game hour, so 80% takes about 18.2 game hours before modifiers.
- The GPS/map retains Compass presentation and LWS navigation ownership. Service markers and world rings use actual service capability, map selection pushes a navigation intent, and temporary intent ownership preserves the underlying ride/diversion destination.
- The roadside companion path uses a scene-independent secondary seat on the existing tractor prefab, with the same seat authored by `TruckTaxiDemoBuilder` for refresh. The temporary cab mount must be visually revalidated when the final sleeper tractor replaces this model. Companion pickup and passenger bathroom diversions, private-stop circle/GPS, and jug-goal policy use the current ride/session authorities.
- Existing world geometry/collider diagnostics and targeted authoring repair are used instead of broad regeneration. Analytics writes privacy-sanitized local JSONL asynchronously and exposes an optional HTTPS endpoint; no endpoint is configured by default.
- VIBIN' profiles use the registered `ILwsSaveService` and Pixel Crushers storer. The load coordinator now applies prepared vendor data synchronously after world readiness, avoiding the installed `LoadFromSlot` path's deferred-frame completion race. Save, autosave, backup and recovery remain vendor-backed.
- Content-neutral reaction/presentation and item/custom-goal authoring hooks are wired to the existing private companion event. Empty production fields retain the safe fallback. The focused fixtures passed 84 EditMode and 17 PlayMode cases; see `TruckTaxi_Custom_Content.md`.

## Validation Ledger

- Unity 6000.4.10f1 script recompile after the latest fixes: completed without errors.
- Final Truck Taxi EditMode: 860 total, 858 passed, 2 skipped, 0 failed. Report: `Logs/TruckTaxi-October-EditMode-Final.json`.
- Final integrated Truck Taxi PlayMode: 59/59 passed. Report: `Logs/TruckTaxi-October-PlayMode-Final.json`. The three-ride fixture uses authored subtitles without assuming deferred voice WAV assignment; the pedestrian impact fixture explicitly waits for two live UTS walkers and fixes its authored count at two.
- LWS/Pixel Crushers focused: 14/14 passed; cab seating focused: 9/9 passed.
- `Truck Taxi/Build Windows Demo`: succeeded, 592596049 bytes, 0 errors. `Builds/TruckTaxiDemo/TruckTaxi_Data/Managed/LWS.TruckTaxi.Runtime.dll` was refreshed on October 5, 2026.
- Windows player quit/restart/load, manual cockpit/full-route driving, and visual presentation acceptance remain unverified. Automated render and PlayMode checks are not substitutes for those manual proofs.

## Required Manual Acceptance

- Drive the full ride loop, including real vehicle input, HUD readability, store purchase, a sleep point, mapped service stops, passenger diversion priority and the previously affected world-flicker/depot-collision locations.
- Verify private-stop GPS and world circle, exact circle/gameplay radius alignment, parking-brake prompt inside the circle, companion Eject Passenger HUD button and input, ejection physics, private route/marker/circle cleanup, and Ride Request suppression release after ejection.
- Verify authored passenger coverage and 33% long-run offer behavior with the final roster. Assign the 48 unresolved genders from designer-owned metadata before claiming complete roster coverage.
- Exercise real VIBIN' save, quit, process restart, Continue/Load, correct region/truck/needs restoration, and recovery confirmation in a Windows player. Do not count in-process EditMode or PlayMode save tests as this proof.

## Boundaries

No vendor source edits, whole-world regeneration, alternate navigation framework or alternate career save storage.
Existing unrelated dirty files are preserved. Radio mixer routing remains explicitly pending.
New pickup ring art remains deferred. Current gameplay rings retain future replacement hooks.
Erotic vocalizations, sexual simulations and fetish interactions are excluded. General non-sexual content slots and ordinary item exchanges remain in scope.

## Baseline

The previous addendum validation is historical evidence only, not validation of this pass.
The current VIBIN' implementation uses the existing Pixel Crushers-backed save facade with profile isolation; a real process restart/load still requires manual Windows-player validation.
