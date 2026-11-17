# Alpha 0.10 horror family — implementation checkpoint

This is implementation evidence, not a release certification. The authoritative requirements remain in `TRUCK_SPORTS_ACCEPTANCE_LEDGER.md` and the November 17 master prompt.

## Integration

`LWS.TruckHorrorSports.Editor.HorrorAuthoring.CreateAll()` creates only missing new venues. Existing authored scenes are preserved. It exposes `Scene(HorrorSport)` for catalog/build registration.

| Sport | Scene | Implemented runtime loop |
|---|---|---|
| Graveyard Shift: The Last Stop | `Assets/TruckHorrorSports/GraveyardShift/Scenes/GraveyardShift.unity` | Fuel-court driving, supply/pump tasks, owner-controlled exit/re-entry into an existing first-person rig, walking to breaker/CB tasks inside a physical modular utility room, power failures, an approaching physical driverless truck, extraction/night outcomes. |
| Truck Harvest: Scarecrow Season | `Assets/TruckHorrorSports/ScarecrowSeason/Scenes/ScarecrowSeason.unity` | Reachable corn corridors, prerequisite ritual wards, clear-only secondary gate changes, false lights, a physical animated humanoid stalker, light-based counterplay, pursuit/lunge/repel states, escape/survival. |
| Dead End: The Last Headlight | `Assets/TruckHorrorSports/DeadEnd/Scenes/DeadEnd.unity` | Continuous physical highway circuit, actual sequential route-gate passages, changing contradictory roadside signs, road-following driverless semi, final extraction and survival variants. |
| Cursed Cargo | `Assets/TruckHorrorSports/CursedCargo/Scenes/CursedCargo.unity` | Native NWH tractor/trailer coupling, actual brake manipulation, force/torque surges and cargo center-of-mass shifts, moving original trailer doors and interior lighting, stopped tractor-and-trailer containment interactions, physical final delivery. |

`HorrorMatch` derives from the established `SportsEventSession`. Existing setup, controller ownership, names/colors, pause/reconnect, countdown/input gate, UI, rematch and split-screen views are reused. Result adapter entry points are `Cooperative`, `ResultPlace(slot)`, `Score(slot)`, `Objectives(slot)`, `Escaped(slot)` and `SurvivalSeconds(slot)`. Cooperative sessions return place zero. Objective and escape telemetry uses the existing bounded `SportsSessionTelemetry.GameplayEvent` service.

The family never injects AI scores: AI drives, brakes, walks to interior tasks, holds interactions and traverses route gates. NavMesh paths are replanned at bounded intervals. Physics contact drives antagonist damage. Score receipts are issued only after actual interaction duration/proximity or physical passage. Native fifth-wheel coupling holds the saved aligned starting pose while NWH initializes, then releases both real assemblies to physics after successful coupling.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** NWH trailer modules/public API and original tractor/trailer prefabs; Toon Gas Station; JC_LP_MegaCity; Leartes Garage Warehouse; NOT_Lonely first-person controller; UTS adult humanoid/ragdoll rig; existing humanoid locomotion FBXs; Weather Maker ambient audio; Pixel Crushers DialogueManager; existing Heat/TMP sports UI.
- **VENDOR ASSETS USED:** NWH vehicles/wheels/coupling through the Hitch family adapter; owned fuel pumps/trees/station/buildings/fences/crates/modular utility furniture; NOT_Lonely CharacterController rig; UTS adult humanoid and existing idle/run clips; Weather Maker scene-owned sky/cloud/weather rendering, an authored fixed-midnight profile, wind/traction influence and wind/crickets/hum/owl/crow audio; existing sports UI/camera/input/telemetry services.
- **RELEVANT ASSETS NOT USED:** Pixel Crushers conversations are not replaced with another dialogue engine; this checkpoint has short environmental captions and existing match-status text, no authored conversation database. Vendor SimpleFPController's global keyboard/mouse reader is disabled because Game Night must route independent controls to each local participant. Its CharacterController rig is retained. Dead Air is not imported or changed. No installed corn/scarecrow model was found in the targeted asset audit; owned sport-specific maize and cloth meshes are generated as editable assets.
- **CUSTOM SYSTEMS CREATED:** Horror objective receipts, per-sport encounter/objective rules, owner-bound on-foot input adapter, physical stalker semantics, authored anomaly triggers, haunted trailer force adapter, new-venue authoring and focused tests.
- **VENDOR SOURCE MODIFIED:** None.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** None intended. NWH owns vehicle and hitch physics; Unity NavMesh owns path queries; existing Game Night owns participants/input/results/persistence. No second profile, lobby, weather or analytics service was created.

## Verification and remaining work

At this checkpoint the agent has **not run Unity, generated scenes, inspected rendering, run the tests or built Windows**. Root owns the single Unity integration process. Tests supplied: twelve EditMode objective cases; a four-venue actual-AI-motion/native-coupling/rematch PlayMode check; one natural Graveyard AI-round check. Their presence is not passing evidence.

All four sports remain **PARTIAL** pending integration and actual acceptance. Required follow-through includes:

- Generate the scenes, compile and exercise real physics/AI/task completion; repair observed errors.
- Full Game Night typed-results/catalog/menu/profile/statistics integration and Windows match/rematch/lobby checks by root.
- Original logos, actual gameplay screenshots and intended-size visual review by the presentation owner.
- Verify Weather Maker sky/cloud rendering in all active viewports. Narrative horror holds a cloned vendor clock at midnight; explicit shared weather selections are honored, while Random defaults to a clouded narrative night. No global time/weather selection is overwritten.
- Graveyard's full requested truck-stop presentation (store/restroom/diner/security-camera interactions and additional narrative encounter variety) exceeds the current fuel-court and utility-room scenario.
- Scarecrow's richer key/puzzle/clue and ritual variations, field dressing and authored encounter animation/audio require gameplay and visual review.
- Dead End currently changes signs and repeats a real road circuit; more elaborate designed landmark/exit/visibility anomalies and ending variants remain to be finished.
- Cursed Cargo has actual physics/door/light hauntings; richer apparitions, cargo escape/neutralization interactions and mode-specific variation remain to be finished.
- Physical gamepad, listening and human-playtest acceptance remain unverified. The inherited native Windows shutdown blocker is owned by the main ledger and is not altered here.

No existing sport, source prefab, hand-edited scene, installed build or vendor code is modified by this family.

## Lead integration checkpoint — November 17, 2026

All four scenes are saved. The previous focused run passed real AI motion/native coupling/rematch checks and a full natural Graveyard default night. Midnight weather ownership and shared participant HUDs are bound. Twenty current rules cases and three new natural default outcomes are queued. Four original PNG logos exist. Detailed venue/story/audio gaps above remain open; no Windows, listening or human horror acceptance is claimed. See the permanent acceptance ledger and Logs/TruckSports/Alpha10 for the exact scoped receipts. This supplements the implementation agent handoff; it does not certify the entire sport.
