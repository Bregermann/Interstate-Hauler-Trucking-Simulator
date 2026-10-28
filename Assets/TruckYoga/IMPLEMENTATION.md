# Yoga source handoff

This is source implementation, not native or actual Windows acceptance. The scene has not been created by this agent. No Editor/player/Git command was run.

Explicit new-scene author: `LWS.TruckYoga.Editor.YogaAuthoring.Create`.
Destination: `Assets/TruckYoga/Scenes/TruckYoga.unity`.
It refuses an existing destination/dirty open scenes and preserves original full-tractor Cab/Wheels hierarchy for the lead's new-scene appearance binder.

Four fixed-owner full NWH tractors support one to four simultaneous players. Yoga disables their powertrain and sets the original complete assembly kinematic; YogaTruckRig is the sole fixed-step pose owner, including the original separate physical cab body at its authored offset. Mass, shapes, joints, wheel controller/radius/spring configuration remain unchanged. No new physics/controller framework is introduced.

Player controls have two banks. PITCH/ROLL changes pitch and lean; ACTION changes to LIFT/TWIST, where the same axes change height and yaw. Achieved pose reads actual Rigidbody position/rotation, not desired input. All seven poses have distinct targets: Downward-Facing Truck, Warrior Wheelie, Two-Wheel Tree, Suspension Stretch, Cab Twist, Truck Bridge and Resting Truck. Accuracy, achieved motion stability and an actual continuous three-second hold determine score/progression. The competitive class permits22 seconds per pose; timeout records a failed/unheld attempt rather than awarding a pose. AI chooses the largest measured pose error and uses the same bounded controls.

**Explicit articulation limitation:** catalog cabs/chassis can be inseparable. CAB TWIST therefore turns the complete stationary assembly, with wheel/cab offsets staying attached. Literal cab-only bending/articulation is not claimed. The approved rigid assembly fallback is described in Help. `BoundAppearanceRevision` records the current composition revision, and every selected body inherits the same physical-root pose; no hidden-original-only animation or reparented physical rig is used.

Free yoga has no score, compulsory winner, forced class timeout or automatic ending. Players select NEXT POSE and FINISH CLASS; finish is also available in Pause. Shared lifecycle supplies real profiles/save, setup, pause, help, results, rematch and return. Custom pose cards show owned participant identity, achieved angles/height and hold gauge. Camera semantics explicitly describe the active class and instructor, leaving the existing camera authority to the lead.

## Living retreat and vendor foundation

- Original full Fare Fight / NWH tractor prefab referenced in place. DanceTruckRig's bounded kinematic pose architecture and Mattress control semantics were audited, not rewritten.
- Heat/uGUI and Cinemachine use existing SportsEvent/SportsUI authoring helpers.
- Actual UTS2.0 Girl_44 Generic biped avatar and twelve explicit named original bones form the instructor. The model/clips have importer animationType2; no Humanoid GetBoneTransform assumption or vendor reimport is used. YogaInstructor converts actor-space pose axes into each actual parent bone frame, disables the old Animator writer and interpolates seven original skeletal routines plus chest breathing/hip lowering. It never moves a frozen character along a route. Pose quality requires actual rendered/native review; no imported yoga clip was found or claimed.
- Existing genuine animated WolfLite presentation prefab from Fishing, with its existing compatible private clips/avatar/controller, supplies two walking/turning/idle wildlife routes. No duplicated rig/clip intake, fake animal or primitive substitute.
- Vista sample grass layers plus installed Mega City trees and Toon Gas Station rocks surround an original mountain-meadow terrain. River Modeler normals and the existing FishingWaterFlow/DOTween material adapter animate the custom waterfall sheet. The shader/material pipeline is unchanged globally.
- Existing Weather Maker morning-birds audio is an ambience source; shared weather/time integration remains lead-owned. No listening/hardware/performance claim.

Native filters: `YogaRulesTests` (EditMode); `YogaNativeTests` (PlayMode).
The native fixture requires actual virtual-keyboard bank control, measured assembly yaw, stable original physical/profile IDs,1-to-4 rematch, pause, free-class zero points/winner, genuine changing instructor bones and all four AI actually holding all seven achieved poses. Timed progression alone cannot pass the latter assertion. Controlled native captures are labelled and are not actual Windows evidence.

Lead integration still owns registration/card art, actual gameplay screenshots, shared appearance/camera/weather and native/Windows/human acceptance. No final visual quality or completed acceptance is claimed without those runs.

VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
