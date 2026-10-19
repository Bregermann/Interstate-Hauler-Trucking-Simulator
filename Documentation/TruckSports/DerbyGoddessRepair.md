# Demolition Derby — starts, contact damage and bank-edge repair

Updated October19,2026. Source and saved scene are updated. The canonical Windows executable has not yet been rebuilt for this pass.

The central obstruction was reproduced using the actual NWH truck with the saved props retained. Longitudinal routes passed, but all four transverse approaches stranded the chassis at the exposed side of an elevated EasyRoads road ribbon; nearby barrels could wedge into the same edge. The original ribbon had no surrounding terrain in this stadium. This was a real visible-but-poorly-shaped obstacle, not evidence of a hidden collider.

The repaired banks retain their EasyRoads meshes. Four seven-metre shoulders sample those exact saved road heights and ease smoothly to the stadium floor. Each shoulder uses the same visible mesh and physical collider. Barrel reset bases are lifted onto the actual new surface, preserving their X/Z placement and physical motion. The dirt treatment makes the raised course distinct. This missing shoulder geometry is project-owned glue around the installed EasyRoads result; no road or vehicle framework was replaced.

Actual repeated NWH traversal: **22 checks, zero errors**, including the same four previously failing side approaches. Failed evidence remains in `Logs/TruckSports/EssentialRelease/Rejected/Derby-Raised-Bank-Side-Edges`; accepted evidence is in `Derby/Traversal/shoulders`. Match AI and timers were deliberately disabled only after the normal countdown for these controlled geometry fixtures. These are not full-match or human tests.

Trucks now start stationary with their engines facing outward and protected rear ends toward the center. AI reverses into nearby opponents during the opening. A brief opening hint replaces the permanent repeated warning; actual stopped-truck warnings remain.

`DerbyMatch.derbyDamageMultiplier` defaults to2. It scales qualifying collision health damage once, preserving existing zone protection, wheel/engine attribution and the0.65 severe-contact ceiling. It does not multiply physics impulses, explosions or ongoing engine fire. NWH remains the sole powertrain, suspension, tyre and visual-deformation authority. Identical-contact fixtures compare every zone and wheel at1× and2×.

A complete eight-truck Hard series passed with zero runtime errors: **six heats,873 qualifying impacts,42 explosions,zero draws**, and a legitimate first-to-three winner. Pause and clean rematch passed. This match preceded the final shoulder repair; repeat full-match and Windows checks remain open. These figures do not establish a causal performance improvement over a different previous match.

Changed scene: `Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity`. Runtime: `DerbyMatch`, `DerbyFighter`, `DerbyDamageState`, `DerbyFixtures`. Authoring: `DerbyGoddessAuthoring` and regeneration hook in `DerbyAuthoring`. Acceptance: `DerbyProbe`, `DerbyTraversalProbe`.

No vendor or original Truck Taxi source was modified. Real-controller, human gameplay, listening, sustained performance and updated Windows acceptance remain unperformed.
