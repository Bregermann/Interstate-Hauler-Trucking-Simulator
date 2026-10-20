# SOL 6.1 HIGH — INTERSTATE: HAULER
# TRUCK SPORTS: PARTY!
# 18 NEW SPORTS — MASTER IMPLEMENTATION DIRECTIVE

## FULL GAMEPLAY IMPLEMENTATION, AI, PHYSICS, ENVIRONMENTS, MULTIPLAYER & PERMANENT ACCEPTANCE LEDGER

---

# SECTION 0 — OVERALL MISSION

You are working directly in the existing Unity project for Interstate: Hauler, specifically its Truck Sports: Party! collection.

Your assignment is to implement **18 new, complete, playable Truck Sports modes**.

These are additions to the existing collection, not replacements.

## Games to Implement

### Group A — Previously Designed, Now Finalized

1. Truck Hide & Seek
2. Truck Red Light, Green Light
3. Truck Hot Potato
4. Truck Frogger
5. Truck Ninja Warrior
6. Truck Floor Is Lava
7. Truck Avalanche Escape
8. Truck Freeze Tag
9. Truck Seesaw

### Group B — New Additions

10. Truck Volleyball
11. Truck Balloon Battle
12. Truck Magnet Mayhem
13. Truck Tetherball
14. Truck Jousting
15. Hungry Hungry Haulers
16. Truck Whack-a-Mole
17. Truck King of the Hill
18. Truck Sheep Herding

**Implement the actual games. Do not substitute design documents, isolated prototype scripts, or implementation plans for working gameplay.**

Every mode requires functional rules, human controls, AI behavior, match progression, cameras, scoring, environments, menus, and rematches.

Where design choices have already been explicitly made, preserve them.

Where reasonable defaults are needed, choose sensible arcade-game behavior and expose balancing variables in Unity.

Do not interrupt implementation with unnecessary design questions.

---

# SECTION 1 — GLOBAL DEVELOPMENT REQUIREMENTS

## 1.1 Inspect Existing Architecture First

Before making changes, investigate the actual project structure.

Locate:

- Truck Sports mode registry.
- Main selection menus.
- Match setup architecture.
- Shared gameplay managers.
- Local multiplayer configuration.
- Human and AI participant assignments.
- Player identity and name persistence.
- NWH Vehicle Physics 2 integration.
- Input System configuration.
- Camera managers.
- Split-screen layouts.
- Weather selection.
- Scoreboards.
- Explosions and destructible systems.
- Pedestrian and animal systems.
- Object pooling.
- Existing gameplay tests.
- Build automation.
- Existing imported assets and packages.

Reuse functioning components wherever possible.

Do not introduce 18 incompatible gameplay frameworks.

Do not rewrite Truck Taxi or the main trucking experience.

## 1.2 Preserve Existing Sports

Existing Truck Sports modes must remain operational.

Do not change their gameplay rules, controllers, camera behavior, scoring, or environments unless a required shared-system integration genuinely demands it.

If shared modifications are necessary, test affected existing games for regressions.

Do not discard existing player settings or name data.

## 1.3 Human and AI Competitors

All 18 games must support the broader Truck Sports multiplayer framework.

Requirements:

- One through four local human participants.
- AI opponents filling the remaining positions.
- Selectable AI difficulty.
- Correct controller ownership.
- Correct player names and colors.
- AI-only spectator matches where practical.
- Appropriate multiplayer match setup.
- Proper match completion.
- Replay/rematch functionality.

Not every sport needs simultaneous four-way action.

Use alternating turns for sports where simultaneous control would undermine the design.

The same game rules must apply to humans and AI.

AI should perform genuine gameplay actions, not merely receive randomly generated scores.

## 1.4 Shared Player Identity

Use the established global Truck Sports participant registry.

Participant names should persist across sports and application restarts.

Use stable participant IDs internally.

Do not attach long-term records solely to editable names.

Ensure scoreboards and victory announcements use the current participant names.

Prepare game results for future per-participant career statistics.

## 1.5 Split-Screen and AI Camera Visibility

Respect the shared:

**Show AI Cameras: ON / OFF**

setting.

When OFF, only human players receive dedicated gameplay viewports.

When ON, additional AI views can appear where supported.

AI competitors still play normally when their cameras are not being shown.

In AI-only matches, provide a functional spectator/broadcast camera.

## 1.6 Controller and Keyboard Support

Use established Truck Sports input conventions.

Preserve:

- Throttle.
- Steering.
- Braking.
- Camera switching.
- Camera look.
- Universal horn.
- Pause.
- Restart/rematch.

Add game-specific controls using context-sensitive input actions.

Avoid binding conflicts.

Provide readable control hints.

## 1.7 Weather

Use the existing shared weather selector.

Support appropriate registered conditions, including:

- Clear.
- Rain.
- Heavy rain.
- Thunderstorms.
- Snow.
- Heavy snow.
- Blizzard.
- Hurricane.
- Random.

Apply the selected weather to the match.

Weather should meaningfully affect vehicle traction and relevant gameplay physics.

Maintain appropriate snow accumulation.

Do not reset weather because a player is eliminated or finishes early.

## 1.8 Use Existing Asset Collection Extensively

The project already contains a substantial library of Unity environment, vehicle, terrain, character, and effects assets.

Inspect it before creating scenery.

Prioritize appropriate existing assets for:

- Stadiums.
- Industrial environments.
- Cities.
- Roads.
- Mountains.
- Snow.
- Forests.
- Water.
- Lava.
- Quarry environments.
- Vehicles.
- Pedestrians.
- Animals.
- Spectators.
- Particle effects.
- Destruction.
- Lighting.
- Props.

Avoid empty placeholder scenes.

Do not leave arenas floating in blank space.

Use original art and appropriately licensed project assets.

## 1.9 Presentation

Maintain the playful, colorful, slightly ridiculous visual style of Truck Sports: Party!

Each game needs:

- Recognizable environment.
- Game-specific HUD.
- Player-colored identity.
- Correct scoring display.
- Win/loss feedback.
- Appropriate crowd reactions.
- Useful cameras.
- Clear instructions.
- Distinct atmosphere.

Avoid giant black debug rectangles, repeated status messages, and overlapping labels.

---

# GROUP A — NINE FINALIZED SPORTS

# GAME 1 — TRUCK HIDE & SEEK

## Concept

Eight enormous semi-trucks play hide-and-seek inside a sprawling industrial city.

One truck is the seeker.

Seven trucks are hiders.

The seeker must physically locate and tag the others before time expires.

## Environment

Create approximately one square mile or more of connected, genuinely usable industrial/urban terrain.

Include:

- Warehouses with accessible interiors.
- Shipping-container yards.
- Parking garages.
- Service roads.
- Dark alleys.
- Construction sites.
- Bridges and underpasses.
- Tunnels.
- Open loading areas.
- Forested outskirts.
- Hidden courtyards.
- Abandoned factories.

Design real truck-sized hiding places.

Avoid an oversized empty city with nothing meaningful to hide behind.

Use existing urban and industrial assets.

## Match Rules

Default:
- 8 trucks.
- 1 seeker.
- 7 hiders.
- 30-second hiding phase.
- 3-minute seeking phase.

During hiding:
- The seeker cannot see the hiders or their locations.
- Hiders can drive freely.
- The seeker remains confined or visually isolated.

During seeking:
- The seeker drives around searching.
- Hiders attempt to remain undiscovered.
- Physical truck-body contact counts as a tag when the seeker has legitimately reached a hider.

## Confirmed Design Decision: Caught Hiders Spectate

When a hider is tagged:

- Mark them FOUND.
- Remove them from active hiding competition.
- Transition human players to spectator mode.
- AI hiders cease active participation.
- Do not convert caught hiders into additional seekers.
- Keep the original seeker responsible for finding everyone.

The seeker wins by finding all remaining hiders before time expires.

Surviving hiders win if time expires.

## Engine and Headlight Stealth

Hiders must be able to:

- Switch their engines off.
- Switch headlights off.
- Reduce detectable engine noise.
- Restart the engine when necessary.

Engine restart must have a short delay so shutting down carries a tactical cost.

A hidden truck with its engine off should be harder to locate.

## Detection

Implement:
- Directional radar hints.
- Distance-sensitive engine sounds.
- Visibility affected by headlights.
- Visual line-of-sight detection.
- Movement-based discoverability.
- Occasional broad seeker clues.

Do not reveal exact hider positions permanently.

Do not allow nameplates to expose hiders through buildings.

## AI

Seeker AI:
- Searches likely hiding areas.
- Uses clues.
- Follows accessible routes.
- Checks buildings.
- Pursues detected vehicles.
- Physically tags hiders.

Hider AI:
- Chooses reasonable hiding spots.
- Can stop its engine.
- Uses cover.
- Repositions when threatened.
- Attempts escape if discovered.

AI seekers must not be omniscient.

## Scoring

- Valid tag: +1 seeker point.
- Finding all hiders: seeker victory bonus.
- Surviving to timeout: hider survival reward.
- Last hider surviving: optional bonus.

Rotate the seeker role across rounds.

## Acceptance

Verify actual hiding, engine shutdown, restart delay, radar clues, seeker navigation, physical tagging, correct spectating, and timed victory.

---

# GAME 2 — TRUCK RED LIGHT, GREEN LIGHT

## Concept

Twelve trucks race toward a distant finish line.

A gigantic, creepy, ridiculous game master stands at the far end of the course.

The master controls the red and green light phases.

If a truck moves when it should be stopped:

**IT EXPLODES IN AN ABSOLUTELY ENORMOUS FIREBALL.**

## Environment

Build a large stadium racecourse with:

- Twelve starting positions.
- Long driving field.
- Clearly marked finish line.
- Giant traffic signals.
- Massive animated game-master figure.
- Stadium stands.
- Spectators.
- Digital scoreboards.
- Large speakers.
- Dramatic lighting.
- Event decorations.

## Confirmed Design Decision: NO YELLOW LIGHT

Do not implement a yellow light phase.

Instead:

1. GREEN LIGHT begins.
2. Trucks accelerate toward the finish.
3. The giant master suddenly LAUGHS.
4. Exactly approximately one second after the laugh cue begins, the signal switches to RED.
5. Vehicles must be stopped by the red transition.

The master's laugh is the warning.

Do not add another yellow warning stage.

The laughter should be distinctive, audible, and synchronized with the signal.

The master can have multiple laugh animations and audio variations.

## Red-Light Violation Detection

Evaluate genuine vehicle ground movement.

Do not punish:
- Small Rigidbody jitter.
- Suspension settling.
- Wheel animation without actual progress.
- Movement forced entirely by another truck striking a stationary vehicle.

Do punish:
- Continued deliberate rolling.
- Throttle-driven movement.
- Deliberate creeping.
- Failure to stop by the red signal.
- Driving backward or forward during red.

Use appropriate small tolerances for physics stability and input latency, without creating a second hidden driving grace period that negates the laugh warning.

## MASSIVE EXPLOSIONS

On violation:

- Enormous fireball.
- Powerful shockwave.
- Truck body destruction.
- Wheels flying.
- Debris.
- Sparks.
- Smoke.
- Heavy camera shake.
- Dramatic sound.
- Spectator reactions.
- Elimination announcement.

The explosion should be among the largest in the collection.

Use existing explosion/destruction systems where practical.

Prevent duplicate eliminations.

## Match Rules

- Twelve trucks by default.
- Survive light cycles.
- First legal finisher wins.
- Additional placement scoring.
- Violators eliminated.
- Continue until the winning conditions are resolved.

## AI

AI must genuinely drive, anticipate the laugh, brake, and react to the red light.

Lower difficulties occasionally fail to brake correctly.

Hard AI makes better decisions.

Do not arbitrarily explode AI trucks without valid detected movement.

## Acceptance

Verify the master laughs, the one-second warning, no yellow signal, fair stopping detection, legal finishes, and spectacular explosions.

---

# GAME 3 — TRUCK HOT POTATO

## Concept

Eight semi-trucks play an explosive version of Hot Potato.

One truck carries a gigantic ticking bomb.

The carrier tries to touch another truck to pass it.

When the timer expires, the bomb carrier explodes.

## Confirmed Design Decision: ANY TRUCK CONTACT TRANSFERS

Any legitimate truck-to-truck body contact should transfer the bomb.

Do not require a minimum high-speed ram.

A tiny deliberate bump should work.

However:
- Prevent transfer from mere proximity.
- Require genuine collider contact.
- Prevent multiple transfers during one continuous overlapping contact.
- Use a short cooldown and require appropriate separation before the same pair can transfer again.
- Never transfer to eliminated trucks.

## Carrier Speed Boost

The bomb carrier receives a modest temporary speed boost.

The boost should help the carrier chase opponents without making escape impossible.

Expose the value for balancing.

## Bomb

Use a gigantic visible bomb with:

- Flashing lights.
- Audible ticking.
- Increasing urgency.
- Smoke or sparks near expiration.
- Clear carrier identification.

Use an unpredictable but bounded timer.

## Arena

Create an exciting stadium arena with:

- Open driving lanes.
- A modest number of obstacles.
- Ramps.
- Destructible props.
- Spectator stands.
- Strong lighting.

Avoid excessive clutter.

## Rules

- One active bomb.
- One carrier at a time.
- Contact transfers ownership.
- Timer expiry eliminates carrier.
- New cycle begins among survivors.
- Last truck remaining wins.

## Scoring

Reward:
- Successful transfers.
- Survived explosion cycles.
- Last-survivor bonus.

Avoid infinite transfer-score farming.

## AI

Carrier AI aggressively seeks contact.

Noncarriers flee, evade, and use obstacles.

AI should react to the bomb timer and the carrier's speed advantage.

## Acceptance

Verify gentle contact transfers, cooldowns, carrier boost, explosions, eliminations, and final victory.

---

# GAME 4 — TRUCK FROGGER

## Concept

Truck Frogger is a five-event obstacle competition involving roads, trains, rivers, and moving hazards.

## Confirmed Design Decision: FIVE INDEPENDENTLY PLAYABLE CHALLENGES

Do not build a single mandatory continuous five-section race.

Build five distinct selectable challenges, each with its own start, course, finish, and results.

### Challenge 1 — Traffic Nightmare

Cross multiple busy highways filled with realistic AI traffic.

### Challenge 2 — Rail Crossing

Navigate multiple freight-train crossings with carefully timed openings.

### Challenge 3 — River Crossing

Cross bridges, moving cargo platforms, barges, and river obstacles.

### Challenge 4 — Industrial Chaos

Navigate forklifts, shipping-container movers, construction machinery, and industrial intersections.

### Challenge 5 — Ultimate Crossing

Combine traffic, trains, waterways, and industrial hazards in one difficult finale.

## Confirmed Design Decision: ONE LIFE PER ROUND

Each player gets one attempt per challenge round.

If the truck is destroyed, falls into an invalid area, or fails under established challenge rules, the attempt ends.

No checkpoint respawns during a scored attempt.

Allow new rounds and rematches.

## Scoring

**The fastest successful completion earns one point.**

- One point to the fastest eligible finisher in each challenge.
- Slower finishers receive no challenge-win point.
- Failed attempts receive no point.
- After all five challenges, the competitor with the most points wins.

Use a defined tie policy:
1. Compare successful challenge completion times.
2. If still tied, run a selected tiebreaker challenge.

Do not arbitrarily select a winner.

## Multiplayer

Support human and AI competitors.

Alternating timed runs are acceptable and preferable if simultaneous trucks would make obstacle timing unfair.

All competitors must face equivalent course conditions for a given challenge round.

## AI

AI needs actual hazard-aware decision-making.

It must:
- Wait for safe traffic gaps.
- Navigate train crossings.
- Cross moving platforms.
- Avoid moving industrial equipment.
- Make mistakes appropriate to difficulty.

Do not assign fake completion times.

## Acceptance

All five challenges must be independently selectable and completable.

One life per attempt.

Fastest successful run receives the point.

Five-challenge championship correctly determines the winner.

---

# GAME 5 — TRUCK NINJA WARRIOR

## Concept

A ridiculous semi-truck obstacle course inspired by televised physical challenge competitions.

This mode should reward precision, timing, balance, jumping, and expressive vehicle control.

## Confirmed Design Direction

Create a **2–3-minute timed obstacle course** containing approximately 6–10 substantial, properly designed obstacles.

Use individual timed attempts rather than crowding narrow obstacles with eight simultaneous trucks.

## Truck Movement Abilities

Implement separate controls for:

**JUMP**
- Responsive arcade jump.

**SUSPENSION COMPRESSION**
- Compress the truck's suspension.
- Lower its body.
- Prepare for stronger jumps.

**COMBINED SUPER-JUMP**
- Compress, then release while jumping to produce a stronger launch.

Also allow:
- Controlled leaning.
- Two-wheel balancing.
- Limited midair corrections.
- Suspension-based body movement.
- Appropriate recovery.

Make these abilities visually expressive and slightly ridiculous.

Do not break the ordinary NWH vehicle setup.

## Obstacle Course

Build at least the following six major obstacles, with additional ones where practical.

### 1. Tilting Steps

Jump across giant platforms that tilt under truck weight.

### 2. Rotating Logs

Cross enormous rotating cylinders while maintaining traction.

### 3. Wrecking Ball Gauntlet

Time movement between huge swinging wrecking balls.

### 4. Giant Seesaw Bridge

Use the truck's weight to lower a massive mechanical bridge.

### 5. Spinning Discs

Jump between independently rotating platforms.

### 6. Warped Wall Finale

Build speed up a giant curved ramp to reach an elevated finish.

Strike an enormous victory bell with the truck bumper.

Additional suitable obstacles:
- Collapsing bridge.
- Precision ramp jump.
- Narrow balancing path.
- Moving platform gauntlet.

## Checkpoints

Use checkpoints between major obstacles.

Falling into water or otherwise failing an obstacle results in:

**+5 seconds to total time.**

Recover at the latest checkpoint.

Do not make the player replay the entire course after every mistake.

## Competition

- Individual timed runs.
- Fastest adjusted time wins.
- AI competitors perform real runs.
- Live leaderboard.
- Best-time records.
- Spectator/replay support.

## Environment

Build a massive game-show stadium featuring:

- Elevated obstacle structures.
- Large water pits.
- Spectator stands.
- Flashing lights.
- Announcers.
- Giant finish structure.
- Truck Sports branding.

## Acceptance

The complete course must be physically traversable.

Test every obstacle using player input.

Verify jump/compression combinations, checkpoint penalties, timing, AI runs, and the warped-wall finish.

---

# GAME 6 — TRUCK FLOOR IS LAVA

## Concept

Eight trucks compete across collapsing platforms suspended above molten lava.

Survive longer than everyone else.

## Confirmed Design Decision: ENCOURAGE RAMMING

Truck collisions are a central gameplay mechanic.

Players should be able to shove opponents off platforms.

Do not prohibit deliberate ramming.

## Arena

Build a massive volcanic environment with:

- Glowing lava lake.
- Cliff walls.
- Steam vents.
- Volcano.
- Rock formations.
- Suspended platforms.
- Bridges.
- Jumping sections.
- Dynamic hazards.

## Platform Behavior

When touched:

1. Platform activates.
2. Warning begins.
3. Platform shakes and cracks.
4. Collapse timer expires.
5. Platform falls or breaks apart.
6. Remaining trucks must escape.

Use visible and physical collapse.

## Platform Variety

Include:
- Wide introductory platforms.
- Narrow platforms.
- Tilting sections.
- Moving platforms.
- Jump gaps.
- Short-duration platforms.
- Temporary safe platforms.

Difficulty should increase as safe space disappears.

Maintain viable routes.

## Ramming and Scoring

Award survival points.

Optionally award small bonuses for correctly attributed opponent knockoffs.

Use actual collision attribution, not mere proximity.

Last truck standing earns a victory bonus.

## AI

AI must understand:
- Platform stability.
- Remaining safe routes.
- Jump timing.
- Opponent positions.
- Ramming opportunities.
- Risk of falling.

## Lava

Falling trucks hit lava and explode spectacularly.

Include lava splashes, shockwaves, flames, and debris.

## Acceptance

Verify collapsing platforms, real weight/collision interaction, ramming, AI decisions, lava elimination, and full matches.

---

# GAME 7 — TRUCK AVALANCHE ESCAPE

## Concept

Twelve trucks race downhill through an enormous Arctic mountain environment while a massive avalanche pursues them.

## Environment

Create a scenic mountainous course with:

- Snow-covered peaks.
- Pine forests.
- Mountain roads.
- Bridges.
- Cabins.
- Ski lodges.
- Cliffs.
- Tunnels.
- Frozen rivers.
- Scenic overlooks.
- Deep valleys.

Use existing Arctic and mountain assets extensively.

## Avalanche

Build an actual advancing avalanche simulation/presentation.

Include:
- Huge moving snow front.
- Snow clouds.
- Rolling debris.
- Rocks and trees.
- Ground vibration.
- Deep rumbling audio.
- Reduced visibility.
- Physical interaction with caught vehicles.

## Confirmed Design Decision: PHYSICAL CATCHING AND BURYING

Do not instantly eliminate a truck the moment the avalanche touches it.

Instead:
- The avalanche initially pushes the vehicle.
- Snow and debris interfere with driving.
- Visibility deteriorates.
- The vehicle risks being buried.
- The player has a short opportunity to escape.

If buried beyond recovery or overtaken for too long, eliminate it.

Use a fair, configurable escape window.

## Confirmed Design Decision: BRANCHING SHORTCUTS

Provide alternative routes including:

- Dangerous cliffside shortcuts.
- Ravine jumps.
- Narrow mountain passes.
- Risky ice bridges.
- Steeper downhill routes.
- Longer but safer roads.

Each route should offer meaningful tradeoffs.

The player can take a shortcut to escape the avalanche but risk crashing.

## AI

AI must:
- Race intelligently.
- Choose routes.
- Avoid collisions.
- React to blocked roads.
- Attempt shortcuts based on difficulty.
- Escape initial avalanche contact when possible.

## Scoring

- Finishing placement.
- Completion time.
- Distance survived if eliminated.
- Optional shortcut bonuses.

## Acceptance

Test a complete mountain race, genuinely approaching avalanche, physical snow contact, brief escapes, burial, branching routes, and AI navigation.

---

# GAME 8 — TRUCK FREEZE TAG

## Concept

Two teams of four trucks compete in a winter stadium.

One team freezes.

The other attempts to survive and rescue teammates.

## Confirmed Design Decision: ASYMMETRIC TEAMS

One team has ice cannons.

The other team consists of runners/rescuers.

Switch roles after each round.

Do not give both teams identical freeze cannons by default.

## Freezer Team

Use:
- Visible ice cannons.
- Physics-aware projectiles or freezing spray.
- Valid aiming.
- Collision detection.
- Progressive freeze buildup.
- Cooldowns.

Do not allow shots through walls.

## Runner Team

Runners evade ice attacks.

They can use:
- Cover.
- Speed.
- Teamwork.
- Rescue tactics.

## Frozen Truck State

When completely frozen:

- Encapsulate the truck in a large ice shell.
- Disable driving.
- Preserve the visible truck.
- Display appropriate effects.
- Keep it physically present in the arena.

## Confirmed Design Decision: NO AUTOMATIC THAWING

Frozen trucks remain frozen **until another teammate physically rescues them**.

Do not implement automatic thawing after a timeout.

Rescue requires a qualifying truck impact against the ice-encased teammate.

Successful rescue:
- Shatters the ice.
- Restores vehicle control.
- Plays appropriate VFX.
- Applies brief protection against immediate refreezing.

If every runner is frozen simultaneously, the freezing team wins.

If at least one runner remains unfrozen when the timer expires, the runners win.

## AI

Freezers must:
- Pursue runners.
- Aim.
- Fire.
- Coordinate.
- Avoid wasting effort on fully frozen opponents.

Runners must:
- Flee.
- Use cover.
- Rescue teammates.
- Avoid exposed routes.

## Acceptance

Verify freezing, persistent ice states, physical rescues, asymmetric roles, timers, scoring, and role reversal.

---

# GAME 9 — TRUCK SEESAW

## Concept

Four trucks compete on an enormous suspended balancing platform.

Their position and weight cause the entire structure to tip.

## Confirmed Design Decision: MULTIDIRECTIONAL WOBBLE BOARD

Do not use a simple single-axis playground seesaw.

Create a giant platform that tilts forward, backward, left, and right.

Its motion must respond to actual vehicle weight and movement.

## Confirmed Environment: GIANT QUARRY

Place the wobble platform high above a massive industrial quarry.

Include:
- Enormous pit.
- Quarry cliffs.
- Rock walls.
- Industrial cranes.
- Construction equipment.
- Stadium-style spectator viewing areas.
- Floodlights.
- Rock formations.

Falling trucks should crash into the quarry below, producing spectacular impacts and explosions.

## Physics

The platform responds to:
- Truck mass.
- Position.
- Acceleration.
- Braking.
- Collisions.
- Inertia.
- Weight transfer.

Use stable physics constraints or a physically grounded torque model.

Do not arbitrarily tilt the board independent of vehicle positioning.

## Gameplay

Trucks attempt to:
- Maintain balance.
- Reposition to safe locations.
- Shift weight strategically.
- Force opponents toward dangerous edges.
- Survive sudden platform movement.

Ramming is allowed, but the core challenge should remain weight distribution and balance.

## Scoring

Reward:
- Survival.
- Successful major balance recoveries.
- Credited opponent knockoffs.
- Last truck standing.

## AI

AI must react to actual platform orientation and changing center of mass.

It should deliberately redistribute weight and recognize imminent falls.

## Acceptance

Verify real multidirectional platform response, recoverable tilts, legitimate falls, AI balancing, and proper quarry impacts.

---

# GROUP B — NINE ADDITIONAL SPORTS

# GAME 10 — TRUCK VOLLEYBALL

## Concept

Two teams of enormous trucks play volleyball using a gigantic inflatable beach ball.

The trucks use bumpers, body contact, controlled hops, and suspension-powered jumps to return the ball over an enormous net.

## Arena

Build a colorful beach or stadium volleyball court.

Include:
- Truck-sized playing surface.
- Giant center net.
- Clearly marked boundaries.
- Beach or resort scenery.
- Spectators.
- Scoreboard.
- Lighting.
- Crowd reactions.

Default to 2v2, configurable to 3v3 or 4v4 where practical.

## Ball Physics

The ball must:
- Bounce.
- Spin.
- Float.
- Respond to impacts.
- React to wind where appropriate.
- Remain large enough to be reasonably struck by trucks.

Tune mass, drag, collision response, and inflation-like bounce for arcade play.

Do not make it behave like a solid steel sphere.

## Truck Abilities

Players can:
- Drive.
- Jump.
- Use suspension-powered hops.
- Angle the truck for different hits.
- Perform stronger aerial attacks with correct timing.

Hitting angle and contact location determine the return.

## Rules

Use recognizable volleyball rules adapted to truck gameplay.

- Up to three team touches before returning over the net.
- Opponent receives a point if the ball lands legally in your court.
- Out-of-bounds shots award the point to the other team.
- Net collisions are physically handled.
- No unlimited self-juggling to exploit scoring.
- Ball returns to a valid service setup after each point.

Default first to 15 points, win by two, with a configurable practical cap.

## Special Mechanics

Reward:
- Clean spikes.
- Successful saves.
- Three-touch team plays.
- Well-placed returns.

Avoid automatic scoring purely because a truck made contact.

## AI

AI must:
- Predict ball trajectory.
- Position for returns.
- Jump.
- Coordinate teammates.
- Avoid all chasing the same landing spot.
- Attempt spikes and saves.

## Acceptance

Verify serve, rally, touch limits, out-of-bounds calls, real ball contact, net behavior, scoring, and complete matches.

---

# GAME 11 — TRUCK BALLOON BATTLE

## Concept

Every truck has three gigantic colorful balloons tethered above it.

Players attempt to pop opponents' balloons while protecting their own.

Last truck with balloons remaining wins.

## Arena

Create a vibrant party-combat stadium featuring:

- Colorful platforms.
- Broad driving lanes.
- Ramps.
- Balloons.
- Confetti.
- Spectator stands.
- Festival decorations.

## Balloon System

Each truck starts with exactly three balloons.

Balloons should:
- Sway.
- Bounce.
- React to motion.
- Stretch their tethers.
- Respond to impacts.
- Remain visually attached until popped.

Use a stable simulated tether system without excessive physics jitter.

## Popping Mechanic

Players can pop opponents' balloons through:

- Actual contact with appropriately positioned truck components.
- A controlled retractable spike mechanism.
- Well-timed bumps or attacks.

Popping must be based on real contact.

Do not let an arbitrary vehicle-body collision automatically destroy all three balloons.

Provide clear telegraphs for spike extension and cooldowns.

## Elimination

After the third balloon pops:

- Truck becomes eliminated.
- Trigger a spectacular confetti explosion.
- Transition to spectator mode.

Use confetti as the main visual punchline rather than requiring a normal fiery truck explosion.

## Scoring

- Balloon popped: +1.
- Opponent eliminated: bonus.
- Last remaining truck: victory bonus.

Prevent repeated scoring on an already popped balloon.

## AI

AI must target opponents' balloons while protecting its own.

It should navigate, attack, retreat, and use difficulty-appropriate strategies.

## Acceptance

Verify three balloons per truck, tether motion, collision-based popping, correct scoring, elimination, AI competition, and rematches.

---

# GAME 12 — TRUCK MAGNET MAYHEM

## Concept

Semi-trucks fitted with enormous industrial electromagnets compete inside a chaotic scrapyard.

Players attract and repel metal objects and opposing trucks using controllable magnetic fields.

## Environment

Build a large industrial scrapyard with:

- Magnetic cranes.
- Scrap piles.
- Metal containers.
- Steel platforms.
- Conveyor machinery.
- Industrial hazards.
- Scoring bays.
- Spectator stands.
- Large environmental objects.

## Electromagnet System

Each truck has an obvious magnetic device.

Allow three settings:

**POSITIVE**

**NEGATIVE**

**OFF**

Use understandable polarity interactions:

- Opposite poles attract.
- Matching poles repel.
- OFF disables the player's active magnetic field.

Metal environmental objects should have appropriate magnetic properties.

Provide visual magnetic field feedback.

## Magnetic Physics

Implement forces based on:
- Distance.
- Magnetic mode.
- Object mass.
- Configurable maximum force.
- Appropriate force falloff.
- Physics stability.

Avoid instantaneous teleportation or unlimited attraction through walls.

Prevent forces from destabilizing the entire scene.

Use conservative maximum impulses and configurable limits.

## Core Competition

Build a scoring-based arena where players must transport magnetic scrap into their own team's or player's collection zone.

Allow competitors to interfere using attraction and repulsion.

Examples:
- Pull a container toward your goal.
- Repel an opponent away from valuable scrap.
- Dislodge another truck's captured object.
- Push a metal object through a target gate.
- Accidentally get launched by an opposing field.

## Scoring

Objects have different point values.

Score only when an eligible scrap object enters and settles inside a valid scoring zone.

Do not award continuous points for leaving an object inside a trigger.

Default timed matches of approximately three minutes.

## AI

AI should:
- Select valuable objects.
- Choose polarity.
- Move objects toward scoring zones.
- Interfere with opponents.
- Recover from failed magnetic grabs.

## Acceptance

Verify real attraction/repulsion, correct polarity, force limits, scrap movement, contested scoring, AI strategies, and physics stability.

---

# GAME 13 — TRUCK TETHERBALL

## Concept

Two semi-trucks compete around an enormous center pole with a huge ball attached by a long cable.

Players ram the ball to wind the tether around the pole in their assigned direction.

## Arena

Create an oversized outdoor schoolyard or festival-sports stadium.

Include:
- Giant center pole.
- Massive suspended ball.
- Long visible rope/cable.
- Two competing trucks.
- Spectators.
- Field markings.
- Clear directional indicators.

## Physics

The ball should:
- Swing.
- Carry momentum.
- Respond to impacts.
- Change direction.
- Orbit the center pole.
- Wrap the cable around it.

Use stable rope physics or a reliable hybrid tether model.

Do not let the cable stretch infinitely or explode during repeated wraps.

## Rules

Each competitor has an assigned wrapping direction.

A player wins by completing the required valid rope winding around the pole.

Actual cable winding and ball trajectory must determine progress.

Do not award progress based solely on the truck touching the ball.

Opponents can strike the ball to reverse its winding direction.

## Gameplay

Players maneuver around the pole and use their bumpers to hit the ball.

Power, timing, approach angle, and positioning matter.

The ball may swing back dangerously toward competitors.

## Scoring

Use a round-based structure:

- Valid completed wrap wins a round.
- Best of three or five rounds.
- Clear wrap-progress display.

## AI

AI should predict ball trajectory, intercept it, strike at useful angles, and defend against opposing wraps.

## Acceptance

Verify ball swinging, rope wrapping, direction changes, scoring, truck collisions, AI play, and resets.

---

# GAME 14 — TRUCK JOUSTING

## Concept

Two semi-trucks perform medieval-style jousting competitions with giant padded lances mounted to their vehicles.

They charge along parallel lanes and attempt to strike opposing target zones.

## Arena

Build a complete medieval tournament venue featuring:

- Castle walls.
- Grandstands.
- Colorful banners.
- Medieval festival props.
- Wooden barriers.
- Spectators.
- Dramatic announcer presentation.
- Truck Sports branding.

## Jousting Equipment

Each truck has a giant lance mounted appropriately to its side.

Allow:
- Aim adjustment.
- Lance height adjustment.
- Limited angle adjustment.
- Stable mounting.
- Breakable lance effects.

The physical contact point of the lance should matter.

## Gameplay

Both trucks accelerate along parallel lanes.

Players aim their lance at scoring zones on the opposing truck.

Aiming, speed, timing, and alignment determine the result.

A clean strike should feel meaningful.

A very strong hit may destabilize or overturn the opponent.

Avoid arbitrary automatic knockdowns based on hidden random rolls.

## Scoring

Suggested:
- Valid armor/target hit: 1 point.
- Clean central hit: 2 points.
- Knockdown: 3 points.
- Miss: 0.

Expose balancing values.

Support multiple passes and best-of-match competition.

Do not award duplicate points from one sustained contact.

## AI

AI must adjust aim, accelerate, and attempt properly timed strikes.

Difficulty affects precision.

## Cameras

Provide side-on tournament broadcast views and close-up replays.

## Acceptance

Verify lance adjustment, real contact, hit zones, breakage, proper score awards, repeated passes, and competition results.

---

# GAME 15 — HUNGRY HUNGRY HAULERS

## Concept

Four semi-trucks compete inside a gigantic arena filled with hundreds of colorful bouncing balls.

Each truck has a giant extendable front scoop that collects balls.

Collected balls must be delivered into the truck's cargo storage system or scoring container.

## Arena

Create a colorful, toy-like arcade stadium.

Include:
- Large circular arena.
- Hundreds of physics balls.
- Player-colored collection zones.
- Bright lighting.
- Scoreboards.
- Giant toy-like decorations.
- Spectators.

Use a visual style inspired by classic tabletop arcade toys, but use original assets and designs.

## Scoop System

Every truck has a large front scoop.

Allow the player to:
- Extend it.
- Retract it.
- Collect balls through physical interaction.
- Carry a limited capacity.
- Deposit balls into its scoring system.

Scoop movement must be visibly animated.

Balls should physically react to the scoop rather than being collected from a distant invisible radius.

## Ball Types

**Normal Colored Ball**
- Standard points.

**Golden Ball**
- Higher value.
- Rare.

**Explosive Ball**
- Triggers a comedic explosion.
- May scatter stored or carried balls.
- Temporarily disrupts the affected truck.

Make hazardous balls visually identifiable.

## Scoring

Points are awarded after eligible balls are successfully deposited, not merely touched.

Prevent duplicate scoring.

Players may attempt to intercept or knock loose opponents' cargo.

## Trailer Integration

Use the existing trailer/cargo architecture where practical.

The attached storage system should visually fill with collected balls.

Avoid requiring full heavyweight Rigidbody simulation of every stored ball once safely deposited.

Preserve convincing ball collection and release visuals.

## AI

AI must:
- Pursue valuable balls.
- Operate scoops.
- Deposit cargo.
- Avoid explosive balls.
- Interfere with competitors.

## Acceptance

Verify physical collection, scoop animation, cargo capacity, deposits, scoring, golden balls, explosive penalties, AI, and rematches.

---

# GAME 16 — TRUCK WHACK-A-MOLE

## Concept

A gigantic dirt arena contains numerous holes.

Enormous cartoon moles randomly emerge.

Semi-trucks race around trying to hit them with their bumpers before they disappear underground.

## Environment

Build a giant whimsical fairground arena with:

- Dozens of mole holes.
- Dirt mounds.
- Wooden fences.
- Carnival decorations.
- Spectators.
- Colorful scoreboards.
- Farm/countryside scenery.

## Mole Behavior

Moles should:
- Pop out.
- Animate.
- Look around.
- Remain visible briefly.
- Retreat underground.
- Respond to valid bumper contact.

Use actual animated prefabs.

Do not simply teleport static models above the surface without animation.

## Mole Types

**Normal Mole**
- Standard points.

**Golden Mole**
- High-value rare target.

**Explosive Decoy**
- Causes a ridiculous explosion when struck.
- Launches or disorients the truck.
- Applies appropriate scoring penalty.

Make decoys visually distinguishable with attentive observation.

## Gameplay

Players drive toward active holes.

Valid physical bumper contact while a mole is exposed awards points.

The mole disappears or ragdolls comedically according to existing suitable effects.

Avoid graphic content.

Do not reward hitting an inactive hole.

## Difficulty

Increase the number of active moles and shorten emergence durations gradually.

Keep the arena navigable.

Avoid spawning every high-value mole directly next to one competitor.

## AI

AI must identify emerged targets, drive toward them, avoid decoys when possible, and compete for valuable moles.

## Acceptance

Verify emergence animation, timed windows, collision-based scoring, golden targets, decoys, AI pursuit, and match completion.

---

# GAME 17 — TRUCK KING OF THE HILL

## Concept

Eight trucks compete to control a moving scoring zone in a gigantic mountaintop arena.

This is a territory-control game, not a ball-possession game.

## Environment

Build a spectacular mountain-top competition area with:

- Broad roads.
- Rocky plateaus.
- Bridges.
- Cliffside paths.
- Elevated platforms.
- Narrow ridges.
- Open competition zones.
- Spectator overlooks.
- Dynamic weather.

## Scoring Zone

A large illuminated control zone appears at an accessible location.

Players earn points by occupying it under valid control conditions.

Periodically, the zone relocates.

Make movement clearly telegraphed.

Do not instantaneously put the zone inside inaccessible terrain.

## Control Rules

- A single uncontested truck controls the zone.
- Multiple opposing trucks in the zone contest control.
- Points accrue only under valid control conditions.
- Remaining stationary outside the zone earns nothing.
- The zone periodically moves to encourage active competition.

Use actual physical truck presence.

Prevent scoring through overlapping colliders from a distant position.

## Physical Combat

Players may shove opponents out of the zone.

Allow meaningful ramming.

The mode should reward:
- Holding territory.
- Positioning.
- Defensive driving.
- Displacing opponents.
- Reacting quickly to zone relocation.

## Scoring

Default victory target: 60 seconds of cumulative uncontested control.

Expose selectable win targets where useful.

Show live control progress.

## AI

AI must:
- Navigate to the active hill.
- Contest occupied zones.
- Defend control.
- Ram opponents strategically.
- React when the zone moves.
- Avoid dangerous cliffs.

## Acceptance

Verify contested-zone logic, correct timing, physical occupancy, moving zones, AI navigation, and victory conditions.

---

# GAME 18 — TRUCK SHEEP HERDING

## Concept

Semi-trucks compete to guide wandering sheep into designated fenced pens.

This should be a hilarious but surprisingly skillful herding competition.

## Environment

Build a large countryside/farm landscape with:

- Rolling green fields.
- Fences.
- Sheep pens.
- Barns.
- Farmhouses.
- Trees.
- Dirt paths.
- Rolling hills.
- Rural obstacles.
- Spectators near the competition area.

Reuse existing sheep and farming assets from the project.

## Sheep AI

Sheep must:
- Walk.
- Run.
- Turn.
- React to nearby trucks.
- Flee from threatening motion.
- Move as small flocks.
- Navigate fences and openings.
- Avoid getting permanently stuck.

Fix or reuse existing sheep grounding and animation systems as needed.

Do not introduce floating, unanimated sheep.

## Herding Mechanics

Players guide sheep using:
- Truck position.
- Controlled movement.
- Horn sounds.
- Herding pressure.
- Blocking escape routes.
- Careful navigation.

Sheep should avoid loud, rapidly approaching trucks.

Gentle positioning should be more effective than reckless high-speed driving.

## Pens

Each competitor or team has an assigned goal pen.

Sheep count only after physically entering a valid pen.

Prevent duplicate scoring.

Avoid allowing competitors to claim the same animal repeatedly.

## Explosive Sheep

Rare explosive sheep may occasionally appear.

They should:
- Be visually recognizable.
- React to vehicles.
- Detonate under established triggers.
- Scatter nearby sheep.
- Knock down appropriate lightweight fences.
- Create additional herding chaos.

Use existing explosive sheep effects where possible.

Ordinary sheep should not reward violent impacts.

## Competition

Default timed event of approximately three minutes.

Player with the most correctly herded sheep wins.

Tie policy: compare valid completion times for the tied count or run a short tiebreaker.

## AI

AI must:
- Approach sheep appropriately.
- Influence flock direction.
- Avoid over-scattering animals.
- Guide them toward pens.
- Recover escaped sheep.
- React to explosive sheep.

Do not simply teleport sheep into AI scoring zones.

## Acceptance

Verify animated sheep movement, horn reactions, truck herding pressure, pen detection, scoring, explosive sheep, AI behavior, and full matches.

---

# SECTION 2 — SHARED MENU INTEGRATION

All 18 sports must appear in the existing Truck Sports: Party! game-selection menu.

For every mode, create:

- Original custom PNG icon.
- Representative actual gameplay screenshot.
- Game title.
- Short description.
- Relevant setup options.
- Controls overview.

Use the existing menu style and interaction framework.

Do not replace the current game registry with a parallel hardcoded menu.

Do not remove existing games.

Every new game must be reachable through normal player navigation, not only by manually loading its Unity scene.

---

# SECTION 3 — SHARED MATCH LIFECYCLE

Every new sport must correctly support:

1. Entering setup.
2. Assigning humans and AI.
3. Applying names and colors.
4. Loading the arena.
5. Initializing cameras.
6. Starting the match.
7. Running game-specific rules.
8. Updating scores.
9. Determining winners.
10. Showing results.
11. Rematching.
12. Returning to the game menu.
13. Reloading the mode without stale state.

Avoid duplicate event subscriptions, leftover physics objects, duplicated HUDs, and incorrect participant assignments.

Ensure global player names remain consistent.

---

# SECTION 4 — GAME-SPECIFIC AI REQUIREMENTS

Ordinary traffic navigation alone is not sufficient.

Implement or reuse suitable behavior for each sport.

| Sport | Essential AI capability |
|---|---|
| Hide & Seek | Search, hide, detect, escape |
| Red Light, Green Light | Anticipate stopping, brake |
| Hot Potato | Transfer bomb, evade |
| Frogger | Time moving hazards |
| Ninja Warrior | Jump, balance, recover |
| Floor Is Lava | Find safe platforms, ram |
| Avalanche Escape | Race, choose shortcuts, escape snow |
| Freeze Tag | Freeze, evade, rescue |
| Seesaw | Shift weight, balance |
| Volleyball | Predict ball trajectories |
| Balloon Battle | Protect and attack balloons |
| Magnet Mayhem | Control magnetic fields and scrap |
| Tetherball | Predict swing and winding |
| Jousting | Aim lance and time passes |
| Hungry Hungry Haulers | Scoop, carry, deposit |
| Whack-a-Mole | Pursue temporary targets |
| King of the Hill | Capture, defend, contest |
| Sheep Herding | Influence and guide sheep |

All AI must be observed performing the actual behavior in gameplay before being marked verified.

Do not use invisible AI-only scoring shortcuts.

---

# SECTION 5 — PHYSICS & PERFORMANCE

## Physics Requirements

Preserve the project's existing vehicle physics.

For special-purpose mechanics, use stable, mode-local arcade assistance where necessary.

Investigate and prevent:

- Invisible blockers.
- Terrain penetration.
- Incorrect collision layers.
- Spawn overlaps.
- Repeated explosion events.
- Accidental duplicate scoring.
- Unstable large joint chains.
- Broken Rigidbody state transitions.
- Incorrect velocity calculations.
- Uncontrolled physics jitter.
- Objects failing to reset.
- AI stuck against environmental geometry.

The physics should be exaggerated but mechanically understandable.

## Performance

Optimize particularly demanding modes.

- Hide & Seek: Large environment and search AI.
- Red Light, Green Light: Multiple explosions.
- Hot Potato: Eight-truck collisions.
- Ninja Warrior: Moving obstacles.
- Floor Is Lava: Collapsing geometry.
- Avalanche Escape: Large snow effects.
- Freeze Tag: Projectiles and ice.
- Seesaw: Dynamic platform with truck physics.
- Volleyball: Continuous ball prediction.
- Magnet Mayhem: Many physics interactions.
- Hungry Hungry Haulers: Hundreds of balls.
- Sheep Herding: Multiple animated animals.

Use pooling, LODs, instancing, culling, collision filtering, and reasonable simulation budgets.

Do not claim good performance without measuring actual runtime behavior.

---

# SECTION 6 — PERMANENT, EVIDENCE-BASED ACCEPTANCE LEDGER

**THIS SECTION IS MANDATORY AND OVERRIDES ANY PREVIOUS HABIT OF TREATING COMPILED CODE AS COMPLETED WORK.**

Previous development sessions have incorrectly reported incomplete or already-implemented features because task summaries and backlogs were not reliably synchronized with actual Unity scene behavior.

Do not repeat that problem.

## 6.1 Create a Permanent Project Ledger

Create or update a durable, version-controlled acceptance ledger inside the project.

Use the project's existing ledger location if one exists.

Otherwise establish a clearly named project-side location such as:

`Docs/TruckSports/Acceptance/`

Do not create duplicate competing ledgers.

Every one of the 18 sports must have its own requirements record.

The ledger must persist across execution sessions.

## 6.2 Required Status Values

Every requirement must have exactly one of the following statuses:

**MISSING**
The feature does not exist or has not been implemented.

**PARTIAL**
Some implementation exists, but requirements are not fully satisfied.

**IMPLEMENTED BUT UNVERIFIED**
Implementation exists, but appropriate testing has not established correct gameplay.

**VERIFIED**
The requirement has passed the necessary implementation checks and actual relevant testing.

**BLOCKED**
A specific blocker prevents further progress.

Do not use vague statuses like "probably done."

## 6.3 Required Record Fields

Every acceptance item must record:

- Stable requirement ID.
- Sport name.
- Original requirement.
- Current status.
- Relevant Unity scene.
- Relevant scripts.
- Prefabs or assets.
- Implementation notes.
- Test procedure.
- Automated test evidence.
- Runtime evidence.
- Screenshot/video evidence where relevant.
- Human-playtest findings.
- Known issues.
- Last verification date.
- Explicit remaining work.

Do not fabricate evidence.

## 6.4 Never Equate Implementation With Verification

A script existing does not prove gameplay works.

A Unity compilation passing does not prove gameplay works.

A unit test passing does not prove the visual presentation works.

A scene loading does not prove the mode is playable.

A game manager containing the correct methods does not prove AI performs the required behavior.

**Mark VERIFIED only when appropriate evidence supports the actual requirement.**

For a visual requirement, inspect rendered output.

For an AI requirement, observe the behavior.

For a physics requirement, execute the collision or movement scenario.

For scoring, verify that actual gameplay events produce correct results.

For multiplayer, test relevant participant combinations.

## 6.5 No Duplicate Backlog Work

Before implementing a requirement:

1. Inspect the existing code and scene.
2. Determine whether the feature already exists.
3. Verify whether it actually functions.
4. If already working, record supporting evidence.
5. If partial, implement only missing behavior.
6. If broken, repair the existing implementation when appropriate.

Do not rebuild functioning features simply because they appear in a task list.

Do not assume a feature is absent solely because a previous summary omitted it.

## 6.6 Never Lose Open Tasks

Unfinished requirements must remain in the ledger.

Do not silently remove, rename away, or mark incomplete requirements as finished.

If a work session ends, preserve:

- Completed changes.
- Current statuses.
- Evidence.
- Remaining requirements.
- Known blockers.
- Exact continuation instructions.

A future development session should be able to resume from the ledger without reconstructing the entire conversation.

## 6.7 Human Testing Has Priority

If the developer reports that a supposedly completed feature is broken, reopen that requirement.

Record the human-playtest observation.

Investigate the discrepancy.

Do not argue that the system is working merely because automated tests passed.

---

# SECTION 7 — IMPLEMENTATION ORDER

Proceed pragmatically, completing functioning vertical slices instead of creating 18 half-finished scenes.

Suggested order:

## Phase 1 — Shared Preparation

- Audit architecture.
- Audit reusable assets.
- Establish acceptance ledger.
- Identify shared controls, AI infrastructure, weather, scoring, menus, and cameras.

## Phase 2 — Fast, High-Impact Party Games

1. Red Light, Green Light
2. Hot Potato
3. Balloon Battle
4. Whack-a-Mole
5. King of the Hill

These should produce relatively fast playable results while validating shared multiplayer systems.

## Phase 3 — Physics-Based Arena Games

6. Seesaw
7. Floor Is Lava
8. Tetherball
9. Jousting
10. Magnet Mayhem

These require carefully tuned physics interactions.

## Phase 4 — Team and Coordination Games

11. Volleyball
12. Freeze Tag
13. Hungry Hungry Haulers
14. Sheep Herding

These require stronger coordination AI and reliable objective ownership.

## Phase 5 — Large-Environment & Complex Challenge Games

15. Hide & Seek
16. Frogger
17. Avalanche Escape
18. Ninja Warrior

These require more sophisticated environments, special movement, navigation, and testing.

This order is a recommendation, not permission to omit later games.

If existing project architecture makes another order more efficient, adjust it while preserving the complete ledger.

---

# SECTION 8 — MANDATORY RUNTIME VERIFICATION

For every new game:

1. Launch from the actual Truck Sports menu.
2. Verify match configuration works.
3. Verify correct player spawning.
4. Verify human controls.
5. Verify correct camera assignments.
6. Verify AI performs meaningful gameplay.
7. Verify physics and collisions.
8. Verify scoring.
9. Verify win/loss resolution.
10. Verify results presentation.
11. Verify spectators where appropriate.
12. Verify rematches.
13. Verify returning to the menu.
14. Verify global names.
15. Verify weather selection.
16. Verify relevant controller layouts.
17. Test at 1280×720 and 1920×1080.
18. Check console errors.
19. Observe actual performance.
20. Record results in the acceptance ledger.

Add automated EditMode and PlayMode tests for deterministic logic.

Use controlled scenarios to test boundary conditions.

For example:

- Red Light, Green Light: intentional driving vs physics jitter.
- Hot Potato: repeated contact and ownership cooldown.
- Frogger: tied challenge results and failed one-life attempts.
- Ninja Warrior: checkpoint penalties.
- Floor Is Lava: competing trucks on a collapsing platform.
- Avalanche Escape: burial and successful recovery.
- Freeze Tag: frozen truck remains frozen until rescued.
- Seesaw: platform reacts to distributed truck weight.
- Volleyball: three-touch limit and net crossing.
- Balloon Battle: each balloon pops exactly once.
- Magnet Mayhem: polarity and force limits.
- Tetherball: correct directional wrap detection.
- Jousting: lance hit attribution.
- Hungry Hungry Haulers: only deposited balls score.
- Whack-a-Mole: late hits after mole retreat do not score.
- King of the Hill: contested zones stop accumulation.
- Sheep Herding: sheep physically enter pens before scoring.

When actual runtime testing is unavailable, keep the relevant requirements at IMPLEMENTED BUT UNVERIFIED.

Never invent test results.

---

# SECTION 9 — COMPLETION REPORT

At the end of implementation, provide a report covering each of the 18 modes.

For each mode include:

**Implementation**
- Gameplay features completed.
- Scripts and scenes created.
- Prefabs and assets used.
- Main technical design decisions.

**Verification**
- Compilation result.
- Automated tests.
- Runtime testing.
- Visual checks.
- Multiplayer checks.
- AI checks.
- Actual observed behavior.

**Ledger**
- Number of requirements verified.
- Number partial.
- Number implemented but unverified.
- Number missing.
- Number blocked.

**Outstanding Issues**
- Exact remaining work.
- Known bugs.
- Performance concerns.
- Further human testing required.

Also report shared-system regression results.

If implementation cannot finish within the current session, preserve the work and exact continuation state. Do not falsely declare the entire expansion complete.

---

# FINAL DEVELOPMENT DIRECTIVE

These 18 sports should transform Truck Sports: Party! into a gigantic collection of absurd, physics-driven multiplayer party games.

Each must have a distinct identity.

Hide & Seek is about stealth.

Red Light, Green Light is about braking and spectacular punishment.

Hot Potato is frantic contact-based chaos.

Frogger is about timing moving hazards.

Ninja Warrior is about expressive truck movement and obstacle mastery.

Floor Is Lava is about survival and ramming.

Avalanche Escape is about fleeing environmental destruction.

Freeze Tag is about teamwork and rescue.

Seesaw is about physics and weight.

Volleyball is about jumping, teamwork, and ball control.

Balloon Battle is about protecting vulnerable balloons.

Magnet Mayhem is about manipulating metal through force.

Tetherball is about momentum and winding.

Jousting is about precision and speed.

Hungry Hungry Haulers is about collecting and delivering enormous numbers of balls.

Whack-a-Mole is about timing and target chasing.

King of the Hill is about moving territory control.

Sheep Herding is about controlling unpredictable animal behavior.

**Use the existing assets. Preserve the established vehicle systems. Build complete environments. Implement functioning AI. Support human multiplayer. Make the games genuinely funny and playable.**

Most importantly:

**Do not confuse progress with completion.**

A feature is complete only when the relevant gameplay actually works and the permanent acceptance ledger contains the evidence.

Build all eighteen sports, integrate them into Truck Sports: Party!, and leave an accurate, persistent record of exactly what has and has not been verified.