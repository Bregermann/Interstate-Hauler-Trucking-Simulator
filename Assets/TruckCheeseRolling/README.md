# Hardcore Truck Cheese Rolling

The seventh offline Truck Sports game. Open `Scenes/TruckCheeseRolling.unity`, or choose **HARDCORE TRUCK CHEESE ROLLING** in the collection. The combined Windows startup remains Truck Sports.

## Rules

One to four marked participants chase one physical cheese wheel with twelve additional background racers. Only a marked participant's genuine truck-body contact earns a point. First to five wins; a solo session is a timed five-catch challenge. Background racers never score or consume the cheese. There is no finish-line score.

Contact callbacks gather participant IDs for one physics step. The next fixed step awards a unique contact once; contacts from different participants in that step replay the round neutrally. Escaped cheese or genuinely stalled simulation also replays with unchanged scores. Normal slow mud play continues; there is no short delivery timer.

Each round announces seeded clear, blizzard or hurricane weather, runs a dramatic three-second 3 / 2 / 1 countdown, releases cheese during the final 0.9 seconds, then releases every racer exactly on GO. Begin holding acceleration while 2 is displayed and keep holding through GO to earn one jump boost. An early hold, late press or released throttle does not qualify. AI uses the same accelerator-edge rule with difficulty-dependent timing errors. Round results last three seconds. AI-only games need no confirmation until final results. Rematch retains participant names, colors, sources and difficulty.

## Controls and setup

Forward accelerates; opposite throttle brakes before reversing. Left/right steer relative to the truck. Whole controller: left stick steer, RT accelerate, LT reverse/brake, North/Y recover, Start pause. Shared controllers assign left/right sticks independently. Keyboard clusters start with WASD, arrows, TFGH and IJKL; setup displays each source. Recovery is available for every slot in Pause, including shared-stick/keyboard players. Esc pauses.

Whole-controller ownership conflicts with either shared stick. Duplicate sticks and overlapping simultaneous keyboard clusters are rejected. Disconnect pauses physics, damage, weather forces and clocks; assign a replacement explicitly, release controls, then resume. Confirmation inputs cannot leak into driving. Shared-pad haptics are suppressed to avoid vibrating the other stick owner; whole-pad impacts route to their assigned owner.

## Random hill and presentation

A roughly 50-degree sustained heightfield descent leads into a broad muddy runout. Sixteen starter positions cover the width in two rows. Normal matches choose a fresh seed; every round shuffles the complete grid, so participants can start outside or in the back row. Fixed seeds remain available for reproducible development checks.

Fourteen trees with breakable physical roots, eighteen rocks, four cats and four wolves are pooled and repositioned each round. Three winding clear corridors, spacing checks and clear starts avoid mandatory bottlenecks. Thirty taller-grass patches use native instanced Terrain details in explicit instance-count mode (six clumps per populated cell, with randomized scale); clear corridors remain readable. Truck impacts break tree anchors through PhysX contact; sheep blasts uproot nearby trees once per explosion. Fallen tree bodies, colliders and visuals remain together until the next round restores their roots. Scenery also participates in AI avoidance and safe-recovery checks. Animals are recognizable static model props; only red-collared sheep explode.

Cinemachine follows a stable objective-facing proxy, never the tumbling chassis rotation. Each view uses compact smoked-glass score cards and Heat condensed typography for participant, scores/catches, cheese direction/distance, weather, damage and recovery. The start is a small gold radial sweep with bold italic numerals; a mint glow marks a charged boost. Gameplay never displays an accelerator instruction. The world uses Taxi's exact mushroom wave renderer and its saturation/contrast/bloom/chromatic settings through a scene-owned pipeline, profile and animated sky copy. The screen-aligned HUD is outside these effects. Pause → MUSHROOM LOOK cycles full, low and off. The old pipeline and sky are restored on exit. No Taxi needs, item timers, power bonuses or other Taxi gameplay is instantiated.

## Physics, hazards and recovery

NWH WheelController owns tires, braking and suspension; PhysX owns truck and cheese contacts. There is no second truck-contact impulse. Snow reduces grip; hurricane gusts are shared world forces. Mud reduces traction and adds gradual damping, splashes and temporary tire tracks. Airborne correction has a small horizontal delta-velocity budget. Damage removes driving authority briefly while flight, sliding and spinning remain physical.

Thirty-six actual sheep use reproducible placements. Truck contact queues one detonation; nearby sheep chain. At most two queued blasts process per fixed step. Each registered truck/body receives one impulse and damage application per explosion, regardless of wheel/collider count. Eight particle pools, twelve debris bodies and bounded audio voices cap effects. Cheese is indestructible.

Recovery searches nearby positions at or behind the truck's valid progress and behind the cheese. It avoids trucks, hard scenery, sheep and queued/live blast zones, clears dangerous motion, has a five-second cooldown, and requires separation before scoring. A blocked recovery is deferred rather than forced into an unsafe location.

## Main tuning

- Course: 140 × 360 m heightfield, approximately 185 m descent, wide natural banks; mud begins near z=170.
- Trucks: mass 48, four NWH wheels; motor 24 / brake 65 / steer 30 degrees; world speed cap 48 m/s, no orientation-relative reverse clamp.
- Cheese: radius 1.8, width 1.1, mass 25, speed cap 29 m/s; physical roll, wobble and bounces.
- Start boost: one bounded world-up 5 m/s plus down-course 4 m/s velocity change at GO; no teleport or repeated held-input boosts.
- Trees: 90 kg rigidbodies, breakable world anchors (force 1400 / torque 300), physical trunk colliders, bounded blast response; pooled and reset each round.
- Air correction: acceleration 0.65, horizontal budget 1.2 per flight; no hover forces.
- Weather: intensity 0.7, gust acceleration up to roughly 5.14 m/s²; clear/snow/hurricane grip multipliers 1 / 0.62 / 0.82 before mud.
- Mud: truck grip multiplier 0.48 and linear damping 0.45; cheese horizontal drag 0.8. Bodies stay mobile.
- Blasts: 25 m outer radius, strong inner response around 4 m, truck delta-velocity 26→2 with falloff; bounded cheese/debris impulses and body speed limits.
- Automatic recovery: seven seconds of truly motionless requested driving or slow overturned motion, settled severe damage or escaped truck. Slow legitimate mud movement and damaged flight are not recovery triggers. Race safeguard: 110 seconds without closer reachable progress while cheese is still, or 600 seconds absolute; neutral replay, never an arbitrary point.

`Editor/CheeseAuthoring.Create` regenerates this scene and upgrades the seven-entry front end. It preserves other saved sports. Final builds use `LWS.TruckSports.Editor.SportsBuilder.Build`, whose explicit eight-scene list and failure-safe title/settings restoration preserve host build configuration. `CreateValidateBuild` reauthors older sports and is unnecessary for delivery.

See [asset sources](SOURCES.md), [validation](VALIDATION.md), [build information](BUILD-INFO.md), and [checkpoint](CHECKPOINT.md).
