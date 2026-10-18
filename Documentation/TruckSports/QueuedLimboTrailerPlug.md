# SOL 6.1 HIGH — TRUCK SPORTS: PARTY!
# TWO NEW SPORTS: TRUCK LIMBO & TRAILER PLUG

Implement TWO complete new Truck Sports modes within the existing Interstate: Hauler Unity project.

These games should feature expressive vehicle animation, ridiculous arcade physics, actual player skill, strong environmental presentation, AI competitors, multiplayer compatibility, and entertaining scoring.

Reuse existing project assets and systems wherever possible.

Do not modify working mechanics in unrelated sports.

---

# GAME 1 — TRUCK LIMBO

## 1. Core Concept

Create a competitive limbo game where enormous semi-trucks attempt to squeeze beneath an increasingly low bar.

The truck itself should move and animate in exaggerated, adorable, almost character-like ways.

Think of an enormous truck trying to dance underneath a limbo bar.

Players must coordinate suspension compression, body tilting, approach speed, and timing to fit beneath the obstacle.

Support:
- 1–4 human players.
- AI competitors.
- Solo challenge.
- Competitive elimination rounds.
- Multiple selectable difficulty levels.

## 2. Expressive Truck Animations

This is the most important feature.

The trucks should look genuinely funny as they try to contort themselves.

Implement:

**Truck Squat**
- The entire truck compresses toward the ground.
- Its suspension lowers dramatically.
- The cab seems to crouch in anticipation.

**Shy Lean**
- The truck gently tilts to one side.
- The cab wobbles as though trying to squeeze beneath the bar.

**Belly Slide**
- The truck drops very low and carefully slides forward.

**Sideways Limbo**
- The truck leans heavily to one side, potentially balancing partially on two wheels.

**Happy Wiggle**
- After clearing the bar, the truck does a cute little celebratory wiggle.

**Bear-Up Celebration**
- Following a particularly impressive clearance, the truck rises onto its rear wheels like an excited bear.

**Oh-No Wobble**
- A failed attempt causes the truck to wobble, recoil, and react humorously.

Animations should use functioning vehicle suspension and controlled body movement.

**Important:** The physical collision geometry must agree with the animated vehicle's actual shape and position. Do not make the visual truck appear below a bar while invisible colliders hit it.

Preserve NWH compatibility and reliable return to normal vehicle configuration.

## 3. Limbo Controls

Give players accessible controls for:

- Forward movement.
- Suspension compression.
- Left/right body leaning.
- Controlled body positioning.
- Timed release of compression.

The gameplay should reward timing and experimentation.

A truck that approaches too quickly might hit the bar.

One that crouches too early could lose control.

One that leans properly may squeeze through an otherwise impossible gap.

Avoid making every successful clearance depend on a single button.

## 4. Progressively Lowering Bar

After every successful round, lower the bar.

Start at a relatively forgiving height.

Gradually introduce increasingly difficult heights.

Eventually, players should need ridiculous techniques to succeed.

Display the current bar height.

Provide proper physical collision with the bar.

A truck must pass its entire required body through the opening to count as successful.

If the bar is struck, knocked down, or displaced beyond the accepted threshold, the attempt fails.

## 5. Spectacular Limbo Venue

Build an absurdly festive competition arena.

Include:
- Tropical beach or festival atmosphere.
- Colorful lights.
- Palm trees.
- Cheerful spectators.
- Giant limbo bar.
- DJ booth.
- Dancing fans.
- Party decorations.
- Truck Sports: Party! branding.
- Appropriate upbeat music.

Use existing project assets where suitable.

## 6. Scoring and Competition

Reward:
- Successfully clearing the bar.
- Clearing lower heights.
- Clean attempts.
- Stylish controlled movement.
- Surviving difficult rounds.

Use standard elimination rules or configurable best-height competitions.

Provide proper victory celebrations and rematches.

## 7. AI

AI trucks should visibly attempt the same limbo techniques as human players.

They must:
- Approach the bar.
- Lower their suspension.
- Lean appropriately.
- Attempt difficult clearances.
- Occasionally fail based on difficulty.

Avoid scripted success with no visible attempt.

## 8. Testing

Verify:
- Truck compression is visible and physically meaningful.
- Leaning works.
- The bar reacts correctly to collisions.
- Clean clearances count.
- Failed attempts are correctly detected.
- Vehicles recover from expressive poses.
- AI can compete.
- Multiplayer works.
- Round progression and rematches work.

**FINAL GOAL:** A giant semi-truck awkwardly squatting and wiggling underneath a ridiculously low limbo bar while spectators cheer.

The animation should be adorable, expressive, and hilarious.

---

# GAME 2 — TRAILER PLUG
## BUNGEE TRAILER DROP & SINKHOLE CHAOS

## 1. Core Concept

Build a new competition in which players operate a semi-truck attached to an enormous bungee system.

The truck begins with a full-sized trailer attached.

Below the swinging truck is a gigantic bubbling sinkhole.

The hole is continuously releasing hordes of crazy, screaming, cartoonish pedestrians.

The player's objective is to swing into position and **detach the trailer at precisely the right moment**.

The detached trailer flies through the air under actual physics.

Players score for:
1. How effectively the trailer plugs the hole.
2. How many hostile/crazed pedestrians the trailer physically knocks down or crushes.
3. Particularly accurate or impressive releases.

The most satisfying possible outcome is landing the trailer cleanly into the sinkhole, sealing it shut and stopping the pedestrian outbreak.

## 2. Reuse Existing Bungee Mechanics

**The existing Bungee Destruction swing controls have already been confirmed to feel good. Preserve them.**

Reuse the established bungee movement system where possible.

Do not unnecessarily retune it.

For Trailer Plug:

- The player controls a bungee-suspended truck.
- A cargo trailer is attached to its rear hitch.
- The trailer swings along with the truck.
- The added trailer mass affects momentum and handling.
- The player can position the entire truck/trailer combination.
- A dedicated input disconnects the trailer.

Use a real, properly configured hitch/connection system.

The trailer must visibly follow the vehicle before release.

Avoid physics jitter, clipping, exploding joints, or unstable attachments.

## 3. Manual Trailer Detachment

Give the player a clearly labeled:

**RELEASE TRAILER**

control.

When activated:
- The hitch disconnects immediately.
- The trailer preserves its actual linear velocity and angular momentum.
- The trailer becomes an independent Rigidbody.
- Gravity acts naturally.
- The trailer tumbles, flies, and falls toward the arena.
- The player's bungee truck remains attached to its bungee.

There should be no fake teleporting or scripted landing.

Timing and swing direction must determine the result.

Players should be able to learn the physics and improve their accuracy over repeated attempts.

## 4. The Giant Pedestrian Sinkhole

Build a massive, dramatic sinkhole beneath the swinging truck.

It should look ominous, ridiculous, and alive.

Include:
- A huge circular opening.
- Bubbling liquid or slime.
- Cracking ground.
- Steam and smoke.
- Disturbing underground lighting.
- Debris around the rim.
- Dramatic environmental audio.
- Cartoonishly frantic pedestrian activity.

### Pedestrian Spawning

Crazy pedestrians should continuously emerge from the hole.

- They climb, scramble, or run out.
- Some stumble.
- Some flee across the arena.
- Some gather around the rim.
- Others emerge in chaotic groups.
- They react to falling trailers and impacts.

Build rising tension by increasing their number as the player waits.

Use existing project pedestrian assets, ragdolls, animations, and pooling systems.

Keep spawning performance-conscious.

The pedestrians should feel like a ridiculous arcade swarm rather than static decorations.

## 5. Hole Plugging Mechanic

The primary objective is to block the opening with the detached trailer.

Evaluate:
- How much of the opening is covered.
- Trailer alignment.
- Trailer orientation.
- Depth of landing.
- Stability after contact.
- Whether the trailer remains in place.

A trailer that only brushes the rim should not count as a successful plug.

A trailer lying partially over the opening may earn partial points.

A well-centered trailer that covers the functional opening should seal it.

**Do not award a perfect plug until the trailer has settled.**

Use an appropriate stability check similar to the final resting-position evaluation in Truck Target Launch.

### Successful Plug

When the trailer seals the opening:
- Pedestrian spawning stops.
- Bubbling effects subside.
- The ground shakes.
- Steam erupts around the trailer.
- The crowd cheers.
- A giant **HOLE PLUGGED!** announcement appears.
- Award a major score bonus.

Make the successful landing feel like a spectacular victory.

Pedestrians that already escaped can remain active until the round cleanup.

## 6. Pedestrian Impact Scoring

The falling trailer should also be capable of striking the crowds.

Use actual trailer collision detection.

- Pedestrians react with exaggerated ragdoll physics.
- Strong impacts can launch them through the air.
- Count distinct pedestrians physically struck with sufficient force.
- Prevent repeated contact with the same pedestrian from generating infinite points.
- Allow legitimate secondary impacts where appropriate.
- Keep effects exaggerated and cartoonish rather than graphically gory.

The player should have a meaningful choice:

**Aim directly at the hole for a large sealing bonus, or drop the trailer into a crowd for a potentially impressive impact score.**

An excellent attempt may accomplish both.

## 7. Suggested Scoring

Use configurable initial values:

| Event | Points |
|---|---:|
| Pedestrian struck | +100 |
| Partial hole coverage | +500 to +2,000 |
| Successful complete seal | +5,000 |
| Perfectly centered seal | +2,000 bonus |
| Multiple pedestrian impacts | Combo bonus |
| Particularly accurate release | Precision bonus |

Ensure the sealing bonus remains valuable enough that players are encouraged to attempt the primary objective.

Do not let unlimited pedestrian spawning create an infinite scoring exploit.

## 8. Build Tension During the Swing

As time passes:

- More pedestrians emerge.
- Bubbling becomes more intense.
- The hole produces more smoke.
- The audience becomes louder.
- Pedestrian crowds spread around the arena.
- The opportunity for high-impact drops becomes more dramatic.

However, waiting forever should not be optimal.

Implement an appropriate configurable time limit or rising risk/reward system.

The player should feel pressured to choose a release moment.

## 9. Spectacular Environment

Create an enormous themed arena.

Suggested setting:

**An abandoned industrial quarry converted into a ridiculous live sporting event.**

Include:
- Giant pit.
- Rocky cliffs.
- Industrial structures.
- Cranes.
- Old warehouses.
- Spectator stands.
- Floodlights.
- Warning signs.
- Truck Sports: Party! banners.
- Hazard lights.
- Crowds.
- A huge visible bungee support structure.

Reuse the existing project's substantial environment and industrial asset libraries.

Do not build another mode floating in empty space.

## 10. Trailer Variety

If practical, allow selectable trailer types with different physical characteristics.

Examples:
- Standard enclosed cargo trailer.
- Heavy shipping container trailer.
- Flatbed trailer.
- Long box trailer.

Trailer dimensions and mass should influence flight and hole coverage.

Do not make one trailer automatically superior in every situation.

Balance trailer selection around different strategies.

## 11. Camera System

The camera must communicate both swinging motion and release accuracy.

### Before Release

Show:
- The truck.
- Attached trailer.
- Bungee rope.
- Hole below.
- Pedestrian swarm.
- Intended release trajectory where appropriate.

### After Release

Automatically transition to a cinematic trailer-following camera.

Track:
- Trailer flight.
- Rotation.
- Approach toward the hole.
- Pedestrian impacts.
- Final landing.
- Scoring result.

Allow a replay of spectacular drops.

Do not let the camera enter terrain, fall inside the sinkhole, or lose the trailer.

## 12. HUD

Display:
- Player name.
- Current swing state.
- Time remaining.
- Trailer attached/detached status.
- Pedestrians hit.
- Hole coverage.
- Precision result.
- Total score.

Use polished Truck Sports styling.

Do not clutter the screen with debug messages.

## 13. Multiplayer

Support:
- One to four human participants.
- AI competitors.
- Alternating turns.
- Best-of-three or best-of-five competitions.
- Global participant names.
- Proper score comparison.

Alternating turns are acceptable and preferable to implementing multiple simultaneous bungee rigs if that produces better performance and clearer gameplay.

## 14. AI

AI competitors should:
- Swing their trailers.
- Choose release timing.
- Aim toward the hole.
- Sometimes prioritize pedestrian clusters.
- Demonstrate difficulty-dependent accuracy.
- Occasionally make believable mistakes.

AI outcomes should be based on genuine release physics, not simply hardcoded scoring.

## 15. Physics and Performance

Preserve:
- Stable hitch connections.
- Stable bungee ropes.
- Proper trailer mass.
- Independent post-release physics.
- Real collision detection.
- Pedestrian ragdolls.
- Safe object cleanup.
- Consistent scoring.
- Appropriate performance.

Use object pooling and optimized crowd simulation as needed.

Do not weaken the trailer impact to hide collision or performance problems.

## 16. Required Tests

Verify:

1. Trailer attaches correctly to truck.
2. The combined vehicle swings properly.
3. Bungee controls remain responsive.
4. Release input disconnects the trailer.
5. Trailer retains momentum.
6. Trailer trajectory changes based on release timing.
7. Pedestrian spawning works.
8. Trailer impacts cause ragdoll reactions.
9. Distinct pedestrian hits award points.
10. Partial plugs receive correct scoring.
11. Complete plugs stop new pedestrians emerging.
12. An off-center trailer does not incorrectly seal the hole.
13. Final resting-position evaluation works.
14. Cameras capture the entire release and impact.
15. AI competitors can complete attempts.
16. Multiplayer scoring works.
17. Rematches restore the trailer, hole, and pedestrians.
18. Performance remains acceptable during large swarms.

## FINAL VISION

A massive semi-truck swings from an enormous bungee cable with a cargo trailer attached.

Far below, a horrifyingly ridiculous sinkhole bubbles and spits out hordes of screaming pedestrians.

The player carefully times the trailer release.

The trailer sails through the air, rotates, and crashes toward the ground.

It may smash into the mob, clip the rim, tumble into the pit, or land perfectly across the opening.

**TRAILER RELEASED!**

**23 PEDESTRIANS HIT!**

**HOLE PLUGGED!**

**PERFECT SEAL BONUS!**

Make it an absurd, satisfying, physics-driven precision sport.

---

# SHARED FINAL REQUIREMENTS

Both new sports must:

- Appear in the Truck Sports: Party! selection menu.
- Have custom game icons and representative screenshots.
- Use global player and AI identities.
- Support controller and keyboard input.
- Support the universal horn input where appropriate.
- Use existing weather selection where compatible.
- Have consistent HUD presentation.
- Support proper resets and rematches.
- Preserve working sports and vehicle physics.
- Use existing project assets wherever practical.
- Pass compilation and relevant automated tests.
- Be verified through actual gameplay whenever the environment permits.

Report actual implemented systems, changed files, tests executed, and remaining issues.

Do not claim completion based only on successful compilation.

**Build both sports as complete, entertaining additions to Truck Sports: Party!**