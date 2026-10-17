# Current combined Windows delivery — October16,2026

Production update: seeded viable terrain variation, physical knockdown trees, bounded exploding sheep, loose props/fences and Taxi ragdoll pedestrians. Actual scenery/destruction is the ninth replay source and resets per seed. Shared contact-based Weatherade snow adds broad mattress trails. Full eight-AI races, world/replay/reset checks and a sustained four-view Windows sample passed; original handling and checkpoint policy remain. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Truck Mattress Racing

Eight real truck-and-mattress rigid bodies race down a banked650m meadow. Up to four humans use independent keyboard clusters, whole controllers or separate sticks; AI fills the field. A single root listener serves all views.

Movement shifts weight and carves fabric contact; hold the displayed action to brace, trading speed for less bounce/rotation. Gravity supplies down-course propulsion. NWH wheel controllers stay disabled for the entire sport: no hidden tire motor. Real contact, mud/grass/smoother surface differences, weather and bounded lateral carving determine the slide. Five-second recovery returns to the last passed checkpoint after sustained flipping, genuine stalling or leaving the course; three recoveries, then DNF. Eight ordered swept gates prevent bypasses. Three races award8..1 points, DNF0; equal totals compare best places, then tied leaders race again.

Actual installed HQ_ResidentialHouse Bed.FBX supplies the rounded mattress and quilt meshes. Owned tartan/fabric material, DOTween contact compression, bounded dust and mud trails add presentation. Shared actual-pose replay records root/child rotation and scale; render-only ghosts have no colliders or physics. Three angles, speed, rewind and skip are available. Cameras maintain a stable horizon. Pause, disconnect, replacement, rematch and collection return use existing Sports policies.

Authoring: LWS.TruckMattressRacing.Editor.MattressAuthoring.Create. Saved scene: Scenes/TruckMattressRacing.unity. Final combined build uses SportsBuilder.Build, never a host scene list. See VALIDATION.md and BUILD-INFO.md.
