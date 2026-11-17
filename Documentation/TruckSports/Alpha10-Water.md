# Alpha 0.10 water-family implementation checkpoint

Status: **PARTIAL — not gameplay verified**. This document does not certify the three sports as finished.

## Integration

`LWS.TruckWaterSports.Editor.WaterSportsAuthoring.CreateAll()` creates only absent scenes. Individual entry points are `CreatePaddlePals`, `CreateWhitewaterWipeout`, and `CreateBlackwater`.

| Sport | Runtime | Scene |
|---|---|---|
| Truck Canoeing: Paddle Pals | PaddlePalsMatch | Assets/TruckWaterSports/Scenes/PaddlePals.unity |
| Truck Kayaking: Whitewater Wipeout | WhitewaterWipeoutMatch | Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity |
| Blackwater: Something Below | BlackwaterMatch | Assets/TruckWaterSports/Scenes/Blackwater.unity |

All inherit the existing `SportsEventSession`. Root integrator owns catalog/build/result registration. The typed result adapter can read `WaterMatch.ResultPlace(int)`, `ResultWinner(int)`, `ResultHasOutcome(int)`, `ResultScore(int)`, `ResultUnit`, `ResultHigherIsBetter`, `Cooperative`, `Gates`, `MissedGates`, `FinishTimes`, and `Vessel(slot)`. Unfinished races have no eligible winner; race/delivery times are lower-is-better seconds, survival duration is higher-is-better seconds, measured tricks/challenges use points, and cooperative Blackwater reports deliveries. Blackwater additionally exposes real `Integrity` and `Deliveries`. Cooperative Blackwater is option3 and must not award an individual winner point.

## Implemented source

* Archimeds-backed `SportsFloatingBody` supplies displaced-hull buoyancy and drag; no second hydrostatic solver is introduced.
* A course-owned flow field applies current-relative forces and three-dimensional downstream flow. Existing shared weather wind applies a bounded force.
* Canoes have two independently controlled off-center paddle stations, measured stroke synchronization, partner AI that follows a human's stroke cadence, physical roll/capsizing, and action-operated recovery.
* Kayaks alternate independent paddle impulses, lean/carve with torque, brace/roll, track airborne rotation and controlled landing quality, and traverse descending checkpoint courses.
* Blackwater boats use propulsion forces and physical cargo joints. Supplies must touch the boat and return to the dock slowly. A moving underwater rigidbody approaches real boats and applies wake forces, lift, lateral impulses, roll torque and hull damage.
* Four human positions remain active. Multiple humans receive their own split-camera views; same-canoe occupants each retain an independent stroke station.
* Scores derive from checkpoint crossings, real time, synchronization, capsizes, measured landing quality, supply delivery and surviving hulls. There are no generated opponent scores.
* Rematches restore serialized starting vessel positions, orientation, velocity, counters, cargo and entity state.
* Builders reuse owned river rocks, Toon Gas Station trees, real truck meshes, River Modeler surface material, Weather Maker water audio, and common Heat/uGUI event presentation. Original vendor files are unchanged.

## Verification

Seven focused PlayMode tests were added in `Assets/TruckWaterSports/Tests/PlayMode/WaterPhysicsTests.cs`: real impulse from either canoe station; opposite station torque; stroke cooldown; synchronization measurement; descending river current; actual current force without paddling; and inverted-hull recognition. **They have not been run by this implementation agent.** Unity compilation, scene import, normal matches, split-camera rendering, Game Night result integration, Windows rendering and physical gamepads all remain unverified pending lead integration.

## Exact remaining feature work

* Course authoring currently creates a single descending river for each paddle sport. Branching routes, fallen logs, dedicated shallow hazards, Class III–V authored intensity, shortcuts, recovery pools, spectators and complete waterfall presentation require additional work.
* Rapid Rally now uses narrow gates and actual collision penalties; Canoe Chaos measures opponent contacts; Team Paddle Challenge rewards measured synchronization. Kayak slalom measures missed gates and penalties, freestyle accumulates actual airborne/controlled landing points and completed rolls, and survival physically reverses course rather than teleporting. Extended trick combos, dedicated scenario courses, long-form challenge balancing and presentation remain incomplete.
* AI applies actual strokes/propulsion, follows checkpoint/cargo objectives, changes steering accuracy with difficulty and uses physical obstacle lookahead. Fair rescue behavior, difficulty balancing and complete-match reliability require tuning and runtime evidence.
* The Blackwater entity physically affects vessels and drives a visible dynamic surface wake from the same wave sampled by vessel waterlines. Its current giant-shark presentation needs original supernatural art, differentiated attacks, rescue/extraction outcomes and narrative variation. The opening35seconds retain distance, with escalating cues and nearby strategic silence. Its four options remain incomplete variants.
* Blackwater supply attachment now requires a recent native collider contact; delivery checks both hull and attached crate at the dock, completes once, and broken joints release possession. Cargo durability, recovery scenarios and full reliability still require implementation/acceptance.
* Original logos, actual gameplay thumbnails, wet visual effects, impact splashes, fully owned spatial audio/cadence and accessibility presentation remain outstanding.
* Weather has a shared visual owner and bounded wind influence in river modes. Blackwater's intended designed storm/lightning sequence and all preset/time-of-day choices remain incomplete.
* Actual buoyancy/currents/capsizes, fair landing scoring, completed rounds, reset/rematch, lobby return, profile preservation, statistics and telemetry need focused acceptance runs. No human acceptance is claimed.

## Vendor accounting

**VENDOR ASSETS AUDITED:** existing Archimeds/Floating Entities adapter and Submarine integration; River Modeler APIs/materials/VFX; Weather Maker water/audio/weather integration; owned Fishing river assets; Toon Gas Station trees; common truck geometry and event UI.

**VENDOR ASSETS USED:** existing `SportsFloatingBody`/Archimeds solver, River Modeler material, Weather Maker audio/weather, owned Fishing rocks, Toon Gas Station trees, existing truck visuals, Heat/uGUI shared event presentation.

**RELEVANT ASSETS NOT USED:** River Modeler's spline-authoring component is not needed for the saved fixed course mesh; its owned shader is reused. No installed canoe/kayak model was found in the targeted asset search, so sport-owned hull geometry is authored. NWH wheel propulsion is not used to propel boats; truck assets ride the hull rather than simulating wheel traction on water.

**CUSTOM SYSTEMS CREATED:** course flow/checkpoint semantics, vessel paddle/capsize adapter, canoe synchronization, kayak landing measurement, Blackwater cargo/entity encounter semantics, three scene builders and focused physics tests.

**VENDOR SOURCE MODIFIED:** none.

**DUPLICATE VENDOR FUNCTIONALITY CREATED:** none intended; buoyancy/drag remain in the existing Archimeds adapter, weather in the shared owner, and lobby/input/persistence/results in the existing foundation.

## Lead integration checkpoint — November 17, 2026

Saved water venues are authored. Seven isolated physics tests passed in Physics01. Authored Paddle Pals AI completed its full race and rematch in NaturalObjectives07. Later mode-specific changes preserve race option 0 but remain unverified. Three original logos exist under Assets/TruckSports/Art/Logos. Weather/time catalogs are bound; Blackwater retains authored midnight. Full new default-mode tests are queued; no Windows or human acceptance is claimed. See the permanent acceptance ledger and Logs/TruckSports/Alpha10 for the exact scoped receipts. This supplements the implementation agent handoff; it does not certify the entire sport.
