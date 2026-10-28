# Mechanical Bull source handoff

This is source implementation, not native or actual Windows acceptance. The scene has not been created by this agent. No Editor/player/Git command was run.

Explicit new-scene author: `LWS.TruckMechanicalBull.Editor.MechanicalBullAuthoring.Create`.
Destination: `Assets/TruckMechanicalBull/Scenes/TruckMechanicalBull.unity`.
The author refuses an existing destination or dirty open scenes; it does not migrate an older sport or replace hand edits.

The original Fare Fight full NWH tractor prefab supplies four fixed-owner physical actors. The saddle is one kinematic Rigidbody with a real leather-contact collider. Its one fixed-step writer uses smooth pitch/roll/yaw/reversal/vertical-buck harmonics. Each active tractor stays dynamic, with original NWH suspension/contact. Player counterlean uses bounded acceleration torque and centre-of-mass shifts, like the existing Mattress balance adapter. ACTION braces angular damping. AI reads actual orientation/rate and uses the same forces. The inactive original cab Rigidbody is parked safely and restored for its next actual ride; original mass, joints, shapes and wheel geometry are retained.

One to four named participants take alternating rides, with shared hot-seat controllers/keyboard supported. Options give one, three or five rides each. Real physical falls, survival time, genuine dangerous-tilt recoveries and completed ten-second patterns determine points. A fallen truck continues physical gravity during the outcome hold instead of freezing in the air. Sixty seconds is the maximum ride. Ties use best measured survival. Pause, results, persistence, rematch and return use SportsEventSession/Heat. DescribeBroadcast explicitly focuses the current rider and saddle, and excludes parked participants.

Original generated art: sculpted longhorn torso, muzzle, swept horns, stitched saddle, lathed hydraulic pedestal, cedar fencing/stands and floodlight fixtures. These are project-owned meshes, not copied commercial geometry. The installed UTS humanoid audience uses genuine source clips through an owned controller; crowd/audio reactions reuse FieldGoalCrowd.

## Vendor foundation

- NWH Vehicle Physics2 / WheelController: existing full tractor, public IsGrounded/HitCollider, original suspension and powertrain. No new vehicle controller or tire forces.
- Existing `Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab` is instantiated directly; its author and source prefab are never rewritten.
- Heat/uGUI through SportsEventAuthoring/SportsUIAuthoring; Cinemachine through SportsEventCamera and lead-installed SportsBroadcastFraming.
- `Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab` plus original `Girl no Heel` idle1_f/cheer_f/idle2_f clips; render-only audience through SportsAudienceAuthoring. No pedestrian gameplay, categories or impact behavior is imported.
- Existing Dock crowd clips and NWH engine loop provide feedback/machinery foundation. Listening is unverified.
- No installed bull prop or bucking controller was located; the original mechanical sculpture and rodeo semantics fill that actual gap.

Native filters: `MechanicalBullRulesTests`, `BalancePropMeshTests` (EditMode); `MechanicalBullNativeTests` (PlayMode).
Native fixture uses ordinary owned virtual-keyboard controls, actual NWH saddle contact/fall, pause, four full AI turns, identity/geometry/mass checks and rematch. It does not inject a pose, force, score or resolved outcome. Controlled native screenshots are labelled and cannot stand in for actual Windows captures.

Lead integration still owns menu registration/card art, actual gameplay screenshots, shared appearance/camera/weather integration and native/Windows/human acceptance. The author calls the new-scene-only lead appearance binder after its first save. All seven bodies and 1–4 layouts require rendered review, not only a compile.

VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
