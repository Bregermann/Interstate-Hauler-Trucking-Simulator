# Egg & Spoon Race source handoff

Source implementation only; this agent did not launch Unity, author the scene, execute tests or run a player. Native and actual Windows acceptance remain pending.

New-scene author: `LWS.TruckEggSpoonRace.Editor.EggSpoonAuthoring.Create`.
Destination: `Assets/TruckEggSpoonRace/Scenes/TruckEggSpoonRace.unity`.
The explicit author refuses an existing scene or dirty current scenes. It preserves existing material/mesh assets and refuses an existing terrain asset rather than destroying a designer's work after an interrupted author.

## Physical game

Four fixed-owner full NWH tractors race along separate countryside lanes. Every giant egg is an independent dynamic CapsuleCollider/Rigidbody under gravity. It has no joint, carrier attachment, position-follow script, artificial stabilizing force or AI immunity. A real concave MeshCollider on a kinematic spoon supplies contact. The spoon has one physics pose writer following the actual chassis, with bounded pitch/roll at a visible gimbal. Its handle, mast and egg roots are outside the tractor hierarchy, so the existing NWH attached-body/vehicle-collider caches contain only the original physical tractor/cab.

Normal controls drive through the existing SportsNwhTractorMotor/NWH input authority. HOLD ACTION switches the same axes to spoon trim and engages NWH's handbrake input. This deliberately avoids negative-pedal automatic-DNR reversal during stationary service. Acceleration, steering, braking, hill/bridge motion and impacts affect the actual egg through contact. AI uses the same drive/trim and station service commands, reacting to measured egg offset/chassis tilt and steering physically along the course.

Spills or real impact breakage add eight seconds once per egg. Shell chips and golden yolk particles originate at actual collision contact. Replacement requires physically returning to the last secured checkpoint (or START), stopping upright and holding ACTION for three seconds. The spoon is reset while empty and a new physical egg is placed at that station only. Trucks are never teleported to recover an egg; AI has no exception. A stranded/capsized vehicle can DNF, and the whole race is bounded at180 seconds.

All three gates must be crossed forward in the correct lane while the intact egg is geometrically carried in the actual bowl; a lost egg cannot advance/finish. Competition formats rank adjusted time, prioritize a clean finish, or prioritize fewer spills. Clean times are retained in this run and written in the existing named-participant match receipt, without a new save backend. Results, shared profiles, rematch, pause, help, controller ownership and return use SportsEventSession/Heat.

## Vendor/asset foundation

- Existing full Fare Fight tractor prefab is instantiated in place; NWH remains the only vehicle/suspension/tire/powertrain authority. Original Cab Rigidbody/joint, wheel geometry and physical configuration remain intact.
- EasyRoads3D Pro owns the actual dirt-road mesh/collision bake through the existing `SportsEasyRoadsAuthoring.Bake` public adapter. The explicit small sports course does not recreate road-authoring tools or change Taxi/transport roads.
- Vista sample grass terrain, installed Mega City trees and Toon Gas Station rocks form the woodland/country setting. Existing UTS Generic biped characters and compatible source dance/cheer clips provide animated finish spectators through SportsAudienceAuthoring.
- Existing Heat/uGUI, SportsDrivingViews/Cinemachine and the lead's new-scene-only appearance binder supply UI,1–4 views and all-seven-body customization. Gear is explicitly protected; no whole physical root is hidden/scaled.
- No installed egg, spoon or barn model was located in the actual asset-name audit. Original tapered eggs, double-skinned concave stainless cutlery, gimbal/mast, red siding barns and timber bridge deck fill those missing props. These are original authored geometry, not commercial model extracts. Shared immutable Bull author-time geometry/tractor helpers are referenced, not modified.
- Unity ParticleSystem supplies bounded shell/yolk pools. Existing Hockey feedback/NWH vehicle sounds are reused. No new audio or physics framework.

## Validation contract

`EggSpoonRulesTests` has five expected EditMode cases: ordered intact-egg gates, one-time spill penalties, station-only service, format ranking, DNF/rematch, actual concave mesh and wrong-lane/reverse crossing rejection.

`EggSpoonNativeTests` has one expected PlayMode fixture. It requires real egg/scoop contact, owned virtual-keyboard trim, pause, a clearly labelled CONTROLLED physical drop followed by actual PhysX impact/break/VFX and real station servicing. The controlled drop is not claimed as natural racing. A separate full four-AI race uses varied saved fire/hotdog/monster/lifted bodies and requires all four actually to deliver intact eggs through real gates, preserving original geometry/identities/draft appearance and rematch. It does not directly inject a break/score/gate/finish result. Controlled native images are not actual Windows evidence.

Lead still owns registration/card/logo art, actual gameplay screenshot, shared weather/camera integration and native/Windows/rendered/human acceptance. Source compilation alone does not certify balance tuning, AI completion, visual attachment, performance or listening.

VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
