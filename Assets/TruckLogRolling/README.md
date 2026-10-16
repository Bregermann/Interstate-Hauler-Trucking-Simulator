# Truck Log Rolling

Two to four trucks balance on one genuinely buoyant rotating timber log. Physically drive, reposition weight and bump opponents into the water. First to five round wins; last survivor takes the round. Same-step final falls draw. Starts rotate along the safe centerline. From35seconds waves grow gradually; unresolved120second rounds explicitly draw and replay.

Use the collection's assigned acceleration/steering/brake-before-reverse controls. Fresh ACTION braces for0.85seconds, lowers the physical center of mass, and applies real wheel braking;2.6second cooldown. Minor wheel overhang does not eliminate. Genuine chassis water entry does. One human can play against AI; named/difficulty AI and fully autonomous matches work. Mixed keyboard, whole controllers and opposite shared sticks retain explicit source ownership. Pause/reconnect preserves log, trucks, scores and current round. Rematch keeps setup. Recorded motion replays offer angles, slow/fast, rewind and skip. Offline only.

## Physical foundation

Owned Floating Entities ArchimedsLab.dll handles hull splitting and all buoyancy/drag forces through SportsFloatingBody. The closed80triangle hull is baked in metres, radius3m/length22m, volume611.85m³, mass300000kg. Trucks12000kg; NWH wheel mass/spring/damping/torque are scaled coherently, with tire MaxLoad58860N (twice static per-wheel load). One soft bounded position tether keeps the log in the pond; it does not constrain roll or hold trucks up. No hidden balancing platform, homing, collision impulse or direct-velocity propulsion.

The unchanged legacy DLL has a reproduced triangle-area unit bug: Vector3.Angle(degrees) is passed to Mathf.Sin(radians). The LWS adapter corrects only exposed tri.area metadata before invoking the vendor force solver; it does not replace the solver or modify its source/binary. Static force/equilibrium evidence documents the before/after result.

## Owned presentation

Old Sea Port timber model/endgrain, ANGRY MESH bark/normal native-transcoded1024PNG, NWH truck/wheels and pond mesh, existing River Modeler normal map/Fishing orthographic URPLit+DOTween flow adapter, installed woodland/rocks, Heat/TMP/Cinemachine/shared broadcast and actual-motion replay. Native model-transform baking makes the visible6x6x22metre timber match the physical hull, avoiding the imported rotated-pivot fit error. Original vendor assets remain untouched. One scene listener and pooled splash effects.

Saved scene: Assets/TruckLogRolling/Scenes/TruckLogRolling.unity. Regenerate through LWS.TruckLogRolling.Editor.LogAuthoring.Create. Combined sports-only build uses SportsBuilder.Build; do not overwrite host scene configuration.

## Acceptance

Final native full-match reruns passed0errors: Easy2AI seven heats5–2,28truck contact callbacks; Normal4AI nine heats0/2/0/5 with2draws,86contact callbacks; Hard4AI17heats5/3/3/4 with2draws,172contact callbacks. Real log roll reached71–74degrees. Corrected five-aspect presentation captured;1280x720 composite inspected. Focused actual support/bump/input/pause/reconnect/rematch/menu checks passed after tire/grid fixes. Final combined Windows, sustained rendered performance and real hardware/human playtesting remain required. This is not a completed Windows build handoff.
