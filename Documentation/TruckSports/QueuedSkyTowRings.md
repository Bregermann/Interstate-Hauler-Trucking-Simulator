# TRUCK SPORTS: PARTY! — SKY TOW RINGS
## ARCTIC ENVIRONMENT, SPLINE COURSE & ASSET LIBRARY EXPANSION

Expand the previously requested Truck Sky Tow Rings implementation with the following mandatory requirements.

### 1. Two-Minute Spline-Driven Course

Build a spectacular continuous aerial route that takes approximately **120 seconds to complete**, with a reasonable target range of 110–130 seconds.

Use Unity Splines or the project's existing spline infrastructure.

- Create a carefully authored, winding spline path through an enormous Arctic environment.
- The towing vehicle must follow the spline smoothly, with appropriate turning, acceleration, and deceleration.
- Use varied elevations, banking, sweeping turns, tight canyon passages, and dramatic changes in scenery.
- The player's truck remains tethered and suspended, with limited control for aiming through rings.
- Do not teleport the towing vehicle between spline points.
- Avoid abrupt rotations, unnatural stops, cable instability, or geometry clipping.
- Time the speed profile so a complete successful run lasts around two minutes.

The route should feel meticulously designed, not procedurally scattered across an empty map.

### 2. Arctic Speedboat Towing

Favor the original concept of a powerful speedboat pulling the airborne truck through the course.

- The boat travels along navigable Arctic waterways.
- The truck is suspended above and behind it using a visible towing cable and an appropriate aerodynamic support mechanism.
- The boat follows the water-level towing spline.
- The truck can swing, climb, descend, and adjust alignment within believable arcade limits.
- Design rings around the truck's actual reachable aerial envelope.
- The boat must never pass through mountains, terrain, or solid ice.
- Where the course requires dramatic elevation changes, use water-level changes, tow dynamics, and carefully constructed navigable transitions rather than allowing the boat to fly.

Make the speedboat look powerful enough to tow a massive semi-truck.

### 3. Spectacular Arctic Route

Build a continuous scenic journey featuring several distinct sections:

**Section 1 — Frozen Harbor (0–20 seconds)**

Start near an Arctic fishing village or research station, with docks, boats, snow-covered buildings, and enormous mountains in the distance.

**Section 2 — Iceberg Fjord (20–40 seconds)**

Tow through a winding fjord filled with floating icebergs, towering cliffs, and suspended rings.

**Section 3 — Glacier Passage (40–60 seconds)**

Navigate between enormous blue glaciers, through natural ice arches and alongside impressive frozen waterfalls.

**Section 4 — Arctic Canyon (60–80 seconds)**

Introduce tighter turns, dramatic cliff walls, rock formations, snow-covered forests, and more demanding ring alignment.

**Section 5 — Frozen Ruins (80–100 seconds)**

Pass an abandoned Arctic industrial installation, shipwreck, research outpost, or other memorable landmark.

**Section 6 — The Grand Finale (100–120 seconds)**

End with enormous glacier formations, a spectacular sequence of rings, a dramatic snowy mountain panorama, and a memorable finish.

Make transitions between sections seamless.

### 4. USE MY EXISTING MASSIVE ASSET COLLECTION

**This is critically important.**

Before creating placeholder geometry, inspect the Unity project's available assets and packages.

Find appropriate existing assets for:

- Arctic terrain and mountains.
- Snow and ice materials.
- Glaciers.
- Icebergs.
- Water and ocean surfaces.
- Rocks and cliffs.
- Pine trees.
- Cabins and buildings.
- Boats and ships.
- Industrial structures.
- Particle effects.
- Snow and weather.
- Lighting and atmospheric effects.
- Environmental props.

Prioritize reuse of high-quality assets already available in the project.

Do not default to primitive cubes, generic white terrain, or unfinished blockout scenery when suitable production assets already exist.

Respect existing package dependencies and asset organization.

### 5. Strong Visual Composition

The two-minute course must remain visually interesting throughout.

- Use landmark-based level design.
- Frame stunning mountain views.
- Include substantial environmental depth.
- Use appropriate fog and atmospheric perspective.
- Create convincing snow-covered landscapes.
- Use detailed water and ice surfaces.
- Incorporate weather changes where supported.
- Consider optional aurora borealis during night conditions.
- Ensure the camera makes the world feel enormous.

Avoid empty stretches where nothing interesting is visible.

### 6. Ring Placement Along the Spline

Place rings in carefully designed sequences that complement the route.

- Easy introductory rings.
- Offset left/right rings.
- Height-adjustment challenges.
- Curved sequences.
- Tight but fair precision sections.
- Bonus rings.
- A spectacular final sequence.

Position rings based on the towing spline and the suspended truck's actual movement envelope.

Do not put rings where they are physically impossible to reach.

### 7. Performance

This will be an enormous environment.

Use:

- LODs.
- Occlusion and frustum culling.
- Appropriate terrain streaming where needed.
- Efficient instancing.
- Sensible physics colliders.
- Optimized weather effects.

Do not solve performance problems by stripping out all the scenic detail.

### 8. Mandatory Acceptance

Test a complete uninterrupted run.

Confirm:

1. The full route takes approximately two minutes.
2. The towing vehicle follows the spline smoothly.
3. The truck remains attached and airborne.
4. All ring sequences are physically reachable.
5. Player air control provides meaningful corrections.
6. The Arctic environment is detailed and scenic throughout.
7. Existing project assets are reused extensively.
8. No terrain or water gaps appear.
9. Cameras properly communicate scale and speed.
10. AI competitors can complete the route.
11. Scoring and rematches function.
12. The mode is accessible through the Truck Sports menu.

**Final goal:** A beautiful, ridiculous two-minute Arctic aerial adventure where a semi-truck is towed above icy fjords, steering through floating rings against the backdrop of enormous glaciers and snowy mountains.

Make this one of the most visually impressive games in Truck Sports: Party!