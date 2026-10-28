# SOL 6.1 HIGH — INTERSTATE: HAULER
# TRUCK SPORTS: PARTY!
# ALPHA 0.8.0 — WYRMS & WHEELS
## COMPLETE DRAGON KINGDOM MASTER IMPLEMENTATION PROMPT
### FOUR NEW GAMES + DRAGON MAYHEM VARIANTS FOR EXISTING SPORTS

**Proposed release:** Alpha 0.8.0 — Wyrms & Wheels

**Release theme:** Medieval fantasy, dragons, aerial racing, treasure theft, dangerous deliveries, destructive escape races, and chaotic dragon-powered variations of existing sports.

**Versioning instruction:** Inspect the project's authoritative version metadata. If Alpha 0.8.0 is already assigned or an existing version is newer, advance the version appropriately while retaining the Wyrms & Wheels title.

---

# 0. PRIMARY MISSION

Build one enormous, visually spectacular fantasy Dragon Kingdom inside the existing Interstate: Hauler / Truck Sports: Party! Unity project.

The developer owns multiple Unity asset collections containing dragon models. These models, their appropriate animations, and their related effects must be central to the implementation.

Build **FOUR complete new games**:

1. **Truck Dragon Riders** — Competitive dragon-mounted aerial racing.
2. **Truck Dragon Hoard Heist** — Physically steal treasure from a sleeping dragon without waking it.
3. **Truck Dragon Egg Delivery** — Transport fragile dragon eggs while protective mothers patrol, with eggs capable of hatching into baby dragons.
4. **Truck Dragon Escape** — Flee from aggressive dragons destroying the kingdom, reaching a different designated evacuation area through different routes each round.

Additionally, implement:

5. **Dragon Mayhem Variants** — Optional dragon-enhanced versions of selected EXISTING Truck Sports games, where dragons attack, drop collectible gold, grab sheep, and create environmental chaos.

Dragon Mayhem is a configurable collection of sport variants, NOT a replacement for the base games and NOT a fifth standalone game.

**All four new games and Dragon Mayhem must use shared dragon assets, animation, AI, and world systems where appropriate.**

## Fundamental design goal

The kingdom must feel alive.

Dragons should:

- Fly between mountains.
- Perch on towers.
- Walk on the ground.
- Roar.
- Breathe fire.
- Guard treasure.
- Sleep in caves.
- Protect eggs.
- Swoop toward trucks.
- Grab objects and animals.
- Cause significant environmental destruction.
- Pursue escaping players intelligently.

Most importantly, dragons must look and behave like enormous living creatures.

Do not build static dragon statues that glide around using transform movement.

Do not construct four unrelated fantasy levels when one shared kingdom can support all four.

**Implement real gameplay. Do not merely write an implementation plan.**

---

# 1. CRITICAL PROJECT PROTECTION

## 1.1 Protect the previous 21-game expansion

The existing Alpha 0.7.0 — The Impossible Games assignment contains twenty-one new games.

That work may be actively underway.

This expansion must not interfere with it.

Do not:

- Cancel previous development tasks.
- Reset or replace the previous backlog.
- Overwrite another agent's scenes.
- Delete prefabs or assets.
- Revert unrelated code.
- Rewrite active game-specific scripts unnecessarily.
- Replace profile/customization systems being developed.
- Corrupt existing saved data.
- Change previously approved Truck Sports gameplay.
- Break Truck Taxi.
- Modify the previous expansion's acceptance records.

## 1.2 Isolated development

Use a separate branch or worktree where supported.

Suggested branch:

`feature/truck-sports-dragon-kingdom`

Follow existing repository conventions if different.

Keep Dragon Kingdom code, assets, and configuration organized separately from the twenty-one-game expansion.

## 1.3 Shared systems

Reuse existing authoritative systems for:

- Game registration.
- Named profiles.
- Truck customization.
- Local multiplayer.
- Input.
- Weather.
- Skyboxes.
- Time of day.
- Scoreboards.
- Results.
- Camera managers.
- Physics.
- Saved settings.
- Match lifecycle.

If a shared system is being edited by another development session, coordinate integration through an authoritative owner or adapter layer.

Do not perform conflicting parallel edits.

## 1.4 Existing sports must remain unchanged by default

Dragon Mayhem is an **OPT-IN variant**.

The normal sport must play exactly as it did before when Dragon Mayhem is disabled.

Never silently introduce dragon hazards into a normal match.

Preserve standard:

- Rules.
- Scoring.
- Weather.
- Cameras.
- AI.
- Vehicle handling.
- Level presentation.
- Player settings.

Only explicitly enabled variant effects may change those behaviors.

---

# 2. MANDATORY OWNED DRAGON ASSET DISCOVERY

**DO THIS BEFORE CREATING NEW DRAGON MODELS OR WRITING MODEL-SPECIFIC ANIMATION CODE.**

The developer already owns numerous dragon assets.

Inspect all accessible sources:

- Existing Unity project folders.
- Imported Asset Store packages.
- Prefab collections.
- Model folders.
- Animation controllers.
- Animation clips.
- Materials.
- Shader packages.
- VFX packages.
- Audio packs.
- Authorized connected Unity asset-library sources.
- Available downloaded asset caches.

If a suitable owned pack is available through authorized library access but has not been imported, download and import the compatible package.

Do not pretend to have account access if it is unavailable.

Do not import unlicensed third-party assets.

## 2.1 Build a real dragon asset catalog

Document each suitable dragon asset:

- Asset/package name.
- File and prefab location.
- Rig type.
- Available clips.
- Material compatibility.
- Scale.
- Collider needs.
- Wing and tail capabilities.
- Jaw controls.
- Elemental VFX.
- Suitable gameplay roles.
- Known defects.
- Missing animations.

## 2.2 Mandatory dragon animations

Find and configure appropriate animations for:

- Ground idle.
- Looking around.
- Walking.
- Running.
- Turning.
- Sleeping.
- Awakening.
- Roaring.
- Takeoff.
- Flapping flight.
- Gliding.
- Banking.
- Diving.
- Landing.
- Perching.
- Fire/elemental breath.
- Claw attacks.
- Tail attacks.
- Grabbing objects.
- Carrying objects.
- Dropping objects.
- Eating.
- Inspecting eggs.
- Nest interactions.
- Damage reactions.
- Defeat or retreat when appropriate.

Not every asset needs every clip.

However, any action actually assigned to a dragon must be visually convincing.

Use compatible animation retargeting, blend trees, animation layers, IK, and procedural secondary animation where appropriate.

Do not use unanimated models to represent behaviors that require animation.

## 2.3 Dragon species

Create a configurable species catalog using actual discovered models.

Target suitable variations such as:

- Fire dragons.
- Ice dragons.
- Lightning dragons.
- Earth/forest dragons.
- Large ancient dragons.
- Small agile dragons.
- Baby dragons.

Only advertise species supported by real, usable assets.

Each species should have configurable:

- ID.
- Prefab.
- Size.
- Animation mappings.
- Movement speeds.
- Flight acceleration.
- Turning.
- Attack type.
- Element.
- Stamina.
- Temperament.
- AI profile.
- Effects.
- Sounds.
- Gameplay roles.

---

# 3. SHARED DRAGON FRAMEWORK

All four new games and Dragon Mayhem must use a common extensible dragon-character framework.

## 3.1 Dragon character state machine

Support appropriate states:

- Sleeping.
- Resting.
- Idling.
- Roaming.
- Walking.
- Running.
- Investigating.
- Alerted.
- Taking off.
- Flying.
- Gliding.
- Diving.
- Landing.
- Perching.
- Attacking.
- Breathing an element.
- Grabbing.
- Carrying.
- Eating.
- Dropping cargo.
- Chasing.
- Guarding.
- Returning home.
- Tending a nest.
- Celebrating.
- Retreating.

Prevent conflicting states and repeated animation transitions.

## 3.2 Ground movement

Dragons must navigate naturally on the ground.

Support:

- Correct walk/run animation.
- Terrain alignment.
- Sensible turning.
- Obstacle avoidance.
- Foot placement where supported.
- Smooth idle transitions.
- Appropriate collision.

No sliding frozen creatures.

## 3.3 Aerial movement

Implement real three-dimensional flight behavior.

Support:

- Takeoff.
- Acceleration.
- Climbing.
- Descending.
- Banking.
- Turning.
- Diving.
- Gliding.
- Flapping.
- Air braking.
- Landing.
- Perching.
- Following aerial routes.
- Approaching moving targets.

Flight animation must align with the dragon's movement.

Do not move an idle dragon through the air as if it were a rigid prop.

## 3.4 Flight AI

AI dragons must be able to:

- Patrol between points.
- Avoid cliffs.
- Avoid terrain.
- Avoid buildings where appropriate.
- Fly around towers.
- Follow trucks.
- Intercept trucks.
- Land on suitable surfaces.
- Return to nests.
- Recover from failed navigation.
- Change behavior based on weather or threats.

Use proper three-dimensional steering/pathing rather than relying exclusively on ground NavMesh navigation.

## 3.5 Elemental attacks

Create reusable elemental abilities.

Examples:

**Fire**
- Animated breath.
- Heat.
- Burning vegetation.
- Fire spread in designated destructible areas.
- Temporary truck damage.
- Environmental ignition.

**Ice**
- Frost effects.
- Slippery surfaces.
- Temporary handling changes.
- Ice buildup.

**Lightning**
- Electrical arcs.
- Sparks.
- Brief vehicle control disruption.
- Appropriate visual warnings.

**Earth**
- Dust.
- Ground shockwaves.
- Falling rocks.
- Physical knockback.

Use species-specific effects when supported by real assets.

Attacks require actual gameplay interactions.

Do not merely play particle effects without hit detection.

## 3.6 Dragon physical interactions

Support:

- Wing gusts.
- Tail sweeps.
- Body collisions.
- Claw strikes.
- Object pickup.
- Truck grabbing.
- Sheep grabbing.
- Egg inspection.
- Gold dropping.
- Large object dropping.

Use deliberate collision layers and safe interaction transitions.

Prevent physics explosions caused by dragon and carried-object colliders overlapping.

## 3.7 Dragon Mayhem Director

Create a reusable match-level Dragon Mayhem Director.

It should manage:

- Dragon spawn scheduling.
- Dragon behavior selection.
- Dragon attack cooldowns.
- Hazard budgets.
- Dragon gold drops.
- Sheep interactions.
- Safe-zone restrictions.
- AI awareness.
- Cinematic event triggers.
- Performance budgets.
- Match cleanup.

The Director must be configurable per sport.

It must not globally activate dragons in every game.

Use deterministic match seeds where helpful for reproducing bugs.

## 3.8 Performance

Optimize:

- Dragon LODs.
- Animation culling.
- Rig complexity.
- VFX pooling.
- Fire.
- Destruction.
- Physics debris.
- Audio.
- Distance-based AI updates.
- Multiple camera rendering.

Do not solve performance problems by removing the animation from nearby active dragons.

---

# 4. BUILD ONE SHARED DRAGON KINGDOM WORLD

The kingdom is a substantial shared fantasy environment.

It must support all four new games and provide reusable scenery for future fantasy activities.

## 4.1 Major world regions

### Royal Castle

- Huge medieval castle.
- Towers.
- Drawbridges.
- Defensive walls.
- Courtyards.
- Medieval roads.
- Dragon perches.
- Racing arches.
- Animated villagers.
- Spectator stands.
- Fortified refuge.

### Dragon Mountains

- High mountain roads.
- Cliffside paths.
- Tunnels.
- Bridges.
- Cave entrances.
- Mountain overlooks.
- Dragon nests.
- Snowy peaks.
- Aerial racing routes.

### Enchanted Forest

- Dense trees.
- Underbrush.
- Ferns.
- Giant mushrooms.
- Flowers.
- Ancient ruins.
- Rivers.
- Waterfalls.
- Wildlife.
- Narrow truck-accessible trails.

### Volcanic Region

- Active volcano.
- Lava rivers.
- Black stone.
- Smoke.
- Ash.
- Molten surfaces.
- Fire dragon lairs.
- Dangerous aerial passages.

### Dragon Caverns

- Ancient treasure caves.
- Huge gold hoards.
- Crystals.
- Underground rivers.
- Giant chambers.
- Narrow passages.
- Sleeping dragon areas.
- Destructible cave props.

### Dragon Nesting Grounds

- Cliffside nests.
- Forest nests.
- Mountain nests.
- Egg delivery destinations.
- Flying mother dragons.
- Animated baby dragons.

### Medieval Village

- Timber buildings.
- Stone houses.
- Marketplace.
- Windmill.
- Farm.
- Roads.
- Bridges.
- Animated villagers.
- Livestock.
- Escape routes.

### Lake and Harbor

**Important for Dragon Escape.**

Build a large fantasy lake or navigable water region with:

- Wooden docks.
- Boats.
- An evacuation vessel.
- Harbor buildings.
- Waterfront roads.
- Bridges.
- Shoreline paths.
- Dragon attack vantage points.

### Mountain Refuge

**Important for Dragon Escape.**

Build a large fortified mountain entrance or cave system with:

- Tunnel approach.
- Heavy gate.
- Defensive stone structures.
- Refuge chamber.
- Multiple approach routes.
- Valid escape trigger and shelter area.

## 4.2 Connected road network

Ensure meaningful road connections between regions.

Include:

- Main roads.
- Secondary roads.
- Dangerous shortcuts.
- Bridges.
- Mountain passes.
- Forest detours.
- Tunnel routes.
- Village streets.
- Waterfront access.

The road network must support actual truck navigation.

Do not build disconnected scenery that cannot be reached.

## 4.3 Beautiful skyboxes

Include suitable fantasy atmospheric presets:

- Golden morning.
- Bright fantasy daytime.
- Orange sunset.
- Moonlit castle.
- Star-filled night.
- Stormy skies.
- Heavy fog.
- Dramatic volcanic glow.
- Mountain snowstorms.

Use the established weather and time-of-day system.

## 4.4 Animated world population

Populate appropriate regions with:

- Dragons.
- Sheep.
- Villagers.
- Guards.
- Wildlife.
- Birds.
- Farm animals.
- Travelers.

Every moving animal and humanoid must have suitable animation.

## 4.5 Ambient world activity

Dragons should naturally:

- Fly overhead.
- Land on towers.
- Perch.
- Roar.
- Walk.
- Sleep.
- Circle mountains.
- Visit nests.
- Inspect territory.

The world must feel alive even when no dragon is actively attacking the player.

---

# 5. SHARED DESTRUCTION SYSTEM

**Dragon Escape and Dragon Mayhem require spectacular destruction.**

Build a reusable Dragon Destruction system using existing project destruction tools and VFX where appropriate.

## 5.1 Destructible objects

Support selected:

- Trees.
- Wooden fences.
- Market stalls.
- Hay bales.
- Crates.
- Small wooden structures.
- Bridge segments.
- Decorative towers.
- Roof sections.
- Watchtowers.
- Signs.
- Farm props.
- Rocks where appropriate.

Use suitable physics and breakable prefabs.

## 5.2 Dragon fire devastation

Dragon fire should be able to:

- Ignite vegetation.
- Set designated buildings on fire.
- Destroy fences.
- Burn wooden props.
- Produce smoke.
- Cause debris.
- Create temporary route hazards.

Fire must visually spread and react believably within configured limits.

## 5.3 Physical destruction

Dragons may:

- Strike structures.
- Sweep with tails.
- Knock down trees.
- Shatter wooden objects.
- Drop rocks.
- Damage bridges.
- Crash through designated scenery.

Avoid indestructible-looking scenery that has no reaction to a massive dragon impact when it is intended to be destructible.

## 5.4 Structural safety

Some world objects must remain stable.

Major terrain, essential buildings, and critical gameplay boundaries may require simplified collision or non-destructible protection.

Do not indiscriminately destroy the entire map.

**For Dragon Escape, destruction must never remove every legitimate route to the active extraction destination.**

## 5.5 Controlled reset

At the end of a match:

- Extinguish temporary fires.
- Despawn temporary debris.
- Restore affected structures.
- Restore bridge states.
- Clear hazards.
- Reset dragon behavior.
- Restore the arena for another match.

Use object pooling and resettable states where possible.

Do not require restarting Unity to restore the map.

---

# 6. GLOBAL CUSTOMIZATION, MULTIPLAYER, AND PRESENTATION

## 6.1 Persistent profiles

Use the existing stable participant/profile IDs.

Preserve:

- Truck body.
- Hat.
- Face.
- Wheels.
- Mirrors.
- Paint.
- Truck-ball accessories.
- Dashboard items.
- Player name.
- Preferred colors.

Do not create a separate incompatible customization database.

## 6.2 Special equipment compatibility

Dragon Riders needs a saddle/mount.

Hoard Heist needs cargo equipment.

Egg Delivery needs a protective carrier.

Dragon Escape uses the selected compatible truck and optional escape-specific visual equipment.

Use temporary mode-specific attachments.

Do not permanently overwrite saved customization.

## 6.3 Human and AI multiplayer

Support one to four local human players.

Use AI to fill suitable participant counts.

AI must genuinely play, not receive arbitrary scripted results.

## 6.4 Camera standards

Use established shared camera infrastructure.

Support appropriate:

- First-person.
- Third-person.
- Split-screen.
- Shared Arena View.
- Spectator cameras.
- Cinematic event cameras.

Keep active human players visible where a shared camera is used.

Avoid dramatic zoom-outs that make trucks unreadable.

## 6.5 UI standards

All modes need:

- Player identity.
- Truck icon.
- Scores or objective progress.
- Relevant timing.
- Attractive status feedback.
- Contextual objective indicators.
- Proper setup menus.
- Results.
- Rematches.

No giant permanent instructions or black debug panels.

## 6.6 Custom logos

Create four new original sport logos:

1. Truck Dragon Riders.
2. Truck Dragon Hoard Heist.
3. Truck Dragon Egg Delivery.
4. Truck Dragon Escape.

Also create a distinctive **Dragon Mayhem** visual modifier icon/badge for compatible existing sports.

Use actual gameplay screenshots on game-selection cards.

## 6.7 Units

Follow established Truck Sports player-facing unit conventions.

Use MPH for speed and miles/feet for applicable distances.

Do not display inconsistent metric measurements unless an explicit optional unit setting supports them.

---

# GAME 1 — TRUCK DRAGON RIDERS
## SKYFIRE GRAND PRIX

# 7. CORE GAMEPLAY

Trucks ride enormous dragons through aerial racecourses.

Players compete in spectacular flying races using altitude control, stamina, elemental attacks, and aerial shortcuts.

## 7.1 Dragon selection

Allow available dragon species to be selected.

Each should have distinctive:

- Appearance.
- Animation.
- Handling.
- Flight behavior.
- Elemental attack.

Balance competitive advantages.

Do not make one species objectively superior in every situation.

## 7.2 Truck saddle system

Mount the actual customized truck onto the dragon.

Requirements:

- Correct scale.
- Proper attachment.
- Convincing visual support.
- No floating above the mount.
- Appropriate suspension motion.
- Safe collision filtering.
- Compatible camera positions.

## 7.3 Flight controls

Implement:

- Acceleration.
- Deceleration.
- Climb.
- Descend.
- Turn.
- Bank.
- Controlled dive.
- Elemental attack.
- Limited air brake.

Use arcade-style responsive movement with meaningful inertia.

## 7.4 Stamina

Include a stamina system affecting aggressive climbing and sprinting.

Allow normal flight to restore stamina.

Make it a meaningful strategy rather than a constant punishment.

## 7.5 Racecourse

Build a large aerial loop through the kingdom.

Suggested route:

1. Castle starting platform.
2. Royal arch.
3. Enchanted forest.
4. Waterfall canyon.
5. Mountain cliffs.
6. Floating rings.
7. Volcanic region.
8. Dragon nesting cliffs.
9. Castle return.
10. Finish line.

## 7.6 Checkpoints

Use authoritative ordered aerial checkpoints.

Prevent skipping most of the route.

Track laps and finishing positions correctly.

## 7.7 Elemental attacks

Allow fire, ice, lightning, or other supported breath effects.

Attacks should cause controlled racing disruptions.

Use:

- Cooldowns.
- Clear visual warnings.
- Real hit detection.
- Sensible durations.
- Balanced reactions.

Prevent infinite stun or unavoidable attack loops.

## 7.8 Truck falls from dragon

Strong collisions may dislodge the truck.

The truck should:

- Physically detach.
- Begin falling.
- Allow emergency stabilization/parachute deployment.
- Attempt an animated dragon rescue.
- Reattach after successful capture.
- Continue with an appropriate time penalty.

## 7.9 Dragon rescue

The dragon must visibly circle or approach the falling truck.

Use a validated capture interaction.

Do not simply teleport the player back onto the saddle without visible recovery.

If rescue fails, apply legitimate checkpoint recovery.

## 7.10 AI racers

AI must:

- Follow aerial checkpoints.
- Manage altitude.
- Use stamina.
- Avoid terrain.
- Use elemental breath.
- Recover after collisions.
- Handle falls.
- Finish races.

## 7.11 HUD

Use the existing NASCAR-style horizontal leaderboard in single full-screen racing views.

Include:

- Race position.
- Player name.
- Truck icon.
- Dragon species.
- Lap.
- Stamina.
- Attack cooldown.
- Race time.

## 7.12 Cameras

Support:

- Third-person dragon chase.
- First-person mounted truck cockpit.
- Broadcast aerial camera.
- Rescue camera.
- Spectator view.

Avoid dragon wings obstructing the camera continuously.

## 7.13 Presentation

Include dramatic countdowns, medieval announcers, cheering crowds, fantasy victory celebrations, firework effects, dragon roars, and spectacular finishing shots.

## Acceptance

Verify actual animation, mounted-truck compatibility, flight, species handling, checkpoints, stamina, combat, falls, rescues, AI, multiplayer, scoring, results, and rematches.

---

# GAME 2 — TRUCK DRAGON HOARD HEIST
## DON'T WAKE THE DRAGON

# 8. CORE GAMEPLAY

Trucks sneak into an enormous treasure cave.

A colossal dragon sleeps on a mountain of gold.

Players attempt to steal and physically transport as much valuable treasure as possible without waking the monster.

## 8.1 Treasure cave

Build a gorgeous cavern featuring:

- Huge chambers.
- Narrow tunnels.
- Glittering gold.
- Ancient statues.
- Magical crystals.
- Underground streams.
- Stone columns.
- Stalactites.
- Valuable relics.
- Massive sleeping dragon.

## 8.2 Sleeping dragon

Use a real sleeping animation.

Include:

- Breathing.
- Chest movement.
- Wing motion.
- Tail movement.
- Sleep sounds.
- Occasional head movement.
- Stirring.
- Eye opening.
- Standing.
- Roaring.
- Pursuit.

The wake-up sequence should be animated rather than instantaneous.

## 8.3 Physical treasure

Players collect actual objects:

- Coins.
- Gold bars.
- Chests.
- Relics.
- Crates.
- Gemstones.
- Crowns.

Use physical cargo collection and capacity.

Heavy loads may affect handling and noise.

## 8.4 Noise system

Track meaningful noise events.

Sources include:

- Engine revving.
- Horns.
- Crashes.
- Dropped cargo.
- Breaking pottery.
- Hitting cave walls.
- Loose treasure.
- Colliding with competitors.

## 8.5 Dragon alertness

Noise raises the dragon's alertness.

The player sees a compact alert meter.

Dragon behavior transitions through:

- Sleeping.
- Restless.
- Suspicious.
- Investigating.
- Awake.
- Pursuing.

Use real proximity, hearing, and vision rules.

Do not give the dragon omniscient knowledge of every truck.

## 8.6 Stealth

Allow:

- Slow driving.
- Quiet idling.
- Using cover.
- Alternate routes.
- Waiting for suspicion to subside.
- Intentionally creating distractions.

## 8.7 Chase

When awakened, the dragon:

- Roars.
- Rises.
- Searches.
- Pursues.
- Attacks.
- Breathes fire.
- Knocks objects around.
- Causes cave debris to fall.

The player must escape with the treasure.

## 8.8 Scoring

Only treasure physically delivered beyond the valid extraction area counts.

Lost treasure does not score.

Different treasures have different values.

## 8.9 Multiplayer

Allow four human players and suitable AI thieves.

Players compete for treasure.

They may bump each other, knock cargo loose, and create distractions.

Prevent infinite point farming.

## 8.10 AI

AI must:

- Find treasure.
- Collect it.
- Manage noise.
- React to alerts.
- Avoid the dragon.
- Escape.
- Deliver real cargo.

## 8.11 HUD

Include:

- Player/truck identity.
- Cargo value.
- Cargo capacity.
- Dragon alertness.
- Extraction direction.
- Relevant escape status.

## 8.12 Cameras

Include cave driving, cockpit, dragon wake-up, chase, and spectator views.

Prevent camera clipping inside stone walls.

## Acceptance

Verify sleeping/waking animations, noise detection, stealth, treasure pickup, dragon pursuit, environmental reactions, extraction scoring, AI, multiplayer, and reset.

---

# GAME 3 — TRUCK DRAGON EGG DELIVERY
## THE MOTHER OF ALL DELIVERIES

# 9. CORE GAMEPLAY

Trucks transport enormous fragile dragon eggs through dangerous fantasy terrain.

Protective mother dragons patrol overhead.

Eggs may crack, shift, and sometimes hatch before delivery.

## 9.1 Egg types

Use species-themed eggs, where supported:

- Fire eggs.
- Ice eggs.
- Lightning eggs.
- Forest eggs.
- Other suitable dragon eggs.

Each should have appropriate materials and effects.

## 9.2 Physical carriers

Eggs must be visibly carried.

Use a protective truck-mounted carrier or trailer.

Egg movement should respond to:

- Acceleration.
- Braking.
- Steering.
- Bumps.
- Collisions.
- Jumps.
- Slopes.

## 9.3 Stability controls

Allow appropriate cargo stabilization.

The player must drive carefully.

Do not automatically prevent all egg movement.

## 9.4 Damage

Hard impacts can cause:

- Cracks.
- Condition loss.
- Special effects.
- Lower delivery score.
- Premature hatching.
- Failure after severe damage.

Use non-graphic presentation.

## 9.5 Delivery routes

Routes travel through:

- Forest.
- Castle roads.
- Mountains.
- Bridges.
- Volcano approaches.
- Nesting grounds.

Provide multiple route options with meaningful risk/reward.

## 9.6 Mother dragons

Mother dragons should:

- Patrol.
- Observe trucks.
- Inspect eggs.
- Follow suspicious transport.
- Land.
- Roar.
- React to rough driving.
- Guard nests.
- Attempt suitable protective actions.

They should not all be permanently hostile.

## 9.7 Premature hatching

Some eggs hatch before delivery.

Sequence:

1. Egg wiggles.
2. Shell cracks.
3. Light and sound effects play.
4. Shell breaks.
5. Small dragon emerges.
6. Baby dragon animates and moves.

## 9.8 Baby dragon behavior

Babies may:

- Wiggle.
- Crawl.
- Walk.
- Flap.
- Climb around cargo.
- Make noises.
- Breathe tiny fire bursts.
- Freeze nearby props.
- Generate harmless sparks.

Use actual baby dragon assets or appropriate animation-compatible alternatives.

## 9.9 Delivering a baby dragon

A hatched baby should not automatically count as failure.

Allow the player to transport it safely to the nest.

Create an appropriate retention/care interaction.

## 9.10 Scoring

Score:

- Correct nest.
- Successful delivery.
- Cargo condition.
- Travel time.
- Safe handling.
- Successful baby delivery.

Do not award delivery points if the relevant cargo is absent.

## 9.11 AI

AI must transport actual eggs, drive carefully, respond to mothers, handle hatching, and complete delivery.

## 9.12 HUD

Include:

- Egg type.
- Egg condition.
- Nest direction.
- Distance in miles/feet.
- Delivery score.
- Mother-dragon state when relevant.
- Hatching status.

## Acceptance

Verify physical egg transport, damage, mothers, animation, hatching, baby dragons, AI, correct deliveries, scoring, and rematches.

---

# GAME 4 — TRUCK DRAGON ESCAPE
## DRAGONFALL: ESCAPE THE KINGDOM

**THIS IS A MAJOR NEW GAME AND ONE OF THE CENTERPIECES OF THE EXPANSION.**

# 10. CORE CONCEPT

The player starts somewhere in the Dragon Kingdom.

Suddenly, enormous dragons begin attacking.

They breathe fire, destroy buildings, knock trees down, collapse structures, and aggressively pursue fleeing trucks.

The player must survive long enough to reach a designated safe evacuation destination.

**The extraction destination and valid route change between rounds.**

This must feel like a frantic survival race rather than a standard checkpoint racing game.

Think of a kingdom-wide disaster unfolding around the player.

---

# 11. SAME WORLD, DIFFERENT ESCAPE EVERY ROUND

Use the shared Dragon Kingdom as the reusable gameplay map.

Do not generate a brand-new scene for every attempt.

Instead, implement a proper escape scenario generator.

Each round selects:

- Starting region.
- Active extraction destination.
- Primary route.
- Alternate routes.
- Dragon species and behavior mix.
- Attack locations.
- Relevant environmental hazards.
- Specific destructible events.
- Escape difficulty.

## 11.1 Randomized scenario seed

Generate a reproducible scenario seed.

Use it to select meaningful variation.

Do not merely change a random number in a UI label while the route remains identical.

## 11.2 Route diversity

The same starting area may lead to different extraction targets.

Example:

**Round A**
Escape from village to a fortified castle refuge.

**Round B**
Escape from village through forest and into a mountain tunnel.

**Round C**
Escape from village toward the harbor and board an evacuation boat.

**Round D**
Start in the volcanic region and escape through mountain passes to a stone fortress.

**Round E**
Start near the castle and flee through a forest detour toward the lake evacuation point.

Use genuine navigable paths.

## 11.3 Avoid repetitive selections

Reduce the chance of repeating the same extraction destination and route configuration immediately across consecutive rounds.

Do not promise every possible combination if some combinations are not physically valid.

---

# 12. THREE CORE EVACUATION TYPES

All three must be implemented.

## 12.1 Fortified Safe Area

The player must reach a designated defensible refuge.

Examples:

- Castle courtyard.
- Reinforced stone fortress.
- Protected military-style medieval gate.
- Large walled sanctuary.

The refuge must be clearly marked.

The player must physically cross or park inside a valid extraction area.

Dragon attacks may continue outside.

Once the player is properly secured, extraction succeeds.

## 12.2 Mountain Shelter

The player must escape into a mountain.

Examples:

- Large cave opening.
- Reinforced tunnel.
- Underground refuge.
- Ancient mine passage.
- Natural mountain chamber.

A mountain extraction should feel distinct from the fortress.

The player must physically enter the shelter and reach the protected area.

Do not count success merely by touching the outside of the mountain.

Use appropriate cave lighting and transition effects.

## 12.3 Evacuation Boat

The player races toward the lake or harbor.

A large evacuation boat is waiting at the dock.

The truck must:

1. Reach the harbor.
2. Approach the loading ramp.
3. Physically drive onto the boat.
4. Enter a valid parking/loading space.
5. Remain aboard until secured.
6. Escape as the boat departs.

**The boat must actually move away from the dock.**

Use an animated/physically controlled departure sequence.

The truck should remain correctly attached to the moving vessel without unstable collisions.

Dragons may circle or attack around the harbor before departure.

The boat's departure creates a major cinematic escape moment.

---

# 13. INTENSE DRAGON PURSUIT

Dragons must actively chase and threaten players.

Not simply patrol far above them.

## 13.1 Aggressive behavior

Dragons should:

- Spot escaping trucks.
- Dive toward them.
- Follow along roads.
- Attempt interceptions.
- Breathe fire.
- Knock obstacles into their paths.
- Strike nearby structures.
- Cause wing gusts.
- Grab suitable objects.
- Create major visual destruction.

## 13.2 Distinct dragons

Different dragons should behave differently.

Examples:

**Fire Dragon**
Ignites trees, buildings, and road obstacles.

**Ice Dragon**
Freezes sections of road and reduces traction.

**Lightning Dragon**
Uses visible electrical attacks with brief control disturbances.

**Ancient Dragon**
Creates major physical destruction, powerful gusts, and larger attack zones.

Use the actual available species.

## 13.3 Pursuit escalation

The escape should become more intense over time.

Suggested progression:

1. Distant dragon sightings.
2. First attacks.
3. Direct pursuit.
4. Multiple dragon interceptions.
5. Destruction along important routes.
6. Final desperate run to extraction.

Use readable escalation rather than instantaneous overwhelming damage.

## 13.4 No unfair omniscience

Dragon AI may track visible trucks, investigate sounds, and predict likely routes.

However, it should not perfectly know hidden player positions through all terrain or mountains unless justified by a deliberately defined ability.

---

# 14. DRAGONS LAY WASTE TO THE MAP

**This is one of the most important requested features.**

The dragons should genuinely devastate the environment while hunting players.

## 14.1 Building destruction

Allow dragons to destroy designated:

- Wooden houses.
- Barns.
- Village structures.
- Market stalls.
- Roofs.
- Watchtowers.
- Fences.
- Bridges.
- Decorative defensive structures.

Use actual destructible objects, fracture systems, physical pieces, or convincing authored destruction sequences with proper collision updates.

## 14.2 Forest destruction

Fire dragons may:

- Ignite trees.
- Knock trees down.
- Burn vegetation.
- Create smoke clouds.
- Temporarily block roads.
- Spread limited fire in configured zones.

## 14.3 Road hazards

Dragons may create:

- Fallen trees.
- Burning debris.
- Collapsed carts.
- Rubble.
- Broken fences.
- Small rockslides.
- Fire patches.
- Ice patches.
- Damaged bridges.

These must have gameplay impact.

## 14.4 Cinematic destruction

Examples:

- A dragon tears the roof from a barn.
- A fire dragon burns a row of wooden buildings.
- A giant dragon lands on a watchtower and crushes it.
- A dragon knocks a huge tree across the road.
- A dragon flies beneath a bridge and damages its supports.
- A dragon destroys a gate as trucks attempt to escape.

Use animation, physical effects, audio, and environmental reactions together.

## 14.5 Keep extraction possible

Dynamic destruction must not create unfair impossible situations.

The scenario system must guarantee at least one viable path to the active escape point under its planned hazard events.

When a route becomes blocked:

- Update navigation.
- Provide a viable alternate.
- Inform the player through GPS/map and world cues.
- Allow meaningful detours.
- Ensure AI can recover.

Do not permanently softlock the player behind unavoidable destruction.

---

# 15. ESCAPE NAVIGATION AND GPS

Use the established Truck Sports/Truck Taxi navigation infrastructure where practical.

## 15.1 Objective marker

Clearly identify:

- Active evacuation destination.
- Approximate distance.
- Current route.
- Relevant alternate routes.

## 15.2 Dynamic rerouting

When dragons destroy a road or bridge:

- Update route availability.
- Find a valid detour.
- Update the GPS route line.
- Preserve destination identity.
- Avoid the previously observed disappearing-route regressions.

Do not repeatedly erase the player's navigation objective.

## 15.3 Full-screen map

Provide a readable map view showing:

- Player position.
- Extraction area.
- Routes.
- Known blocked paths.
- Relevant major dragon hazards.

## 15.4 Path correctness

Use a graph or equivalent authoritative route structure.

Validate generated scenarios.

Do not rely only on a random straight line to the destination.

---

# 16. SURVIVAL MECHANICS

## 16.1 Truck damage

Use or adapt existing per-vehicle damage systems.

Dragon attacks may damage:

- Body.
- Wheels.
- Engine.
- Other appropriate components.

Do not make every fireball an instant elimination.

## 16.2 Knockback

Wing gusts and impacts should physically push trucks.

Use controlled forces.

Preserve meaningful driving control.

## 16.3 Recovery

If a truck becomes overturned or stuck, allow a suitable in-place recovery mechanic.

**Do not teleport the truck back to the start simply because it is stuck.**

Use actual physical self-righting or configured recovery impulses.

## 16.4 Elimination

A player can be eliminated by:

- Severe damage.
- Destruction.
- Being caught in a decisive dragon attack.
- Failing a defined survival condition.

Eliminated participants should enter a suitable spectator state.

Do not reset eliminated players behind the attacking dragons and pretend the run continues.

## 16.5 Survivability

Balance difficulty around actual human testing.

Dragon attacks must be dangerous, but the challenge should feel skill-based.

Provide:

- Telegraphs.
- Avoidance opportunities.
- Recoverable mistakes.
- Meaningful alternate routes.

---

# 17. MULTIPLAYER ESCAPE

Support one to four human players and suitable AI trucks.

## 17.1 Competition rules

All participants start the same scenario.

The active evacuation point is shared.

Players race to escape alive.

Suggested winner rules:

- First successfully extracted surviving truck wins.
- Later survivors receive placement based on valid extraction time.
- Eliminated trucks rank below successful escapees.
- Ties follow actual match rules.

Allow configured AI competitor counts.

## 17.2 Shared human challenges

All human competitors should receive comparable opportunities to reach extraction.

Do not spawn a single player beside the boat while others start miles away unless deliberately supported by an asymmetrical variant.

## 17.3 Optional cooperative rules

Also support an optional cooperative evacuation configuration.

Players attempt to get everyone to safety.

Shared success can depend on:

- Number of survivors.
- Time.
- Total damage.
- Whether all human players escaped.

Do not replace the competitive default.

## 17.4 AI

AI must:

- Navigate dynamically.
- Respond to dragon attacks.
- Avoid hazards.
- Choose detours.
- Recover when stuck.
- Flee toward evacuation.
- Drive onto the boat if required.
- Enter the mountain shelter if required.
- Use valid safe-area triggers.

Do not assign AI success merely because a timer expires.

---

# 18. ESCAPE CAMERAS AND HUD

## HUD

Show:

- Player name/truck icon.
- Current extraction destination.
- Distance.
- Survival state.
- Damage/vehicle condition.
- Running position in competitive mode.
- Time.
- Contextual danger indicators.

Use the global NASCAR-style running-order presentation in single full-screen competitive race views where appropriate.

## Cameras

Provide:

- Third-person chase.
- First-person cockpit.
- Shared broadcast view where useful.
- Spectator mode.
- Dragon attack cinematic.
- Boat departure cinematic.
- Final escape cinematic.

Cinematic moments must not steal control during active danger unless specifically designed and safe.

## Dramatic chase presentation

Use:

- Dragon roars.
- Fire.
- Smoke.
- Collapsing structures.
- Dust.
- Villager reactions.
- Destruction audio.
- Music escalation.
- Visible dragon shadows overhead.

Avoid a flat driving experience with only a distant dragon model.

---

# 19. DRAGON ESCAPE ACCEPTANCE TESTS

Verify:

1. The same kingdom map supports multiple scenarios.
2. Starting region varies correctly.
3. Extraction destination varies.
4. Routes vary.
5. Scenario seeds are reproducible.
6. Fortress extraction works.
7. Mountain extraction works.
8. Boat extraction works.
9. Boat physically departs.
10. Trucks stay correctly positioned aboard the boat.
11. Dragons actively pursue.
12. Dragons use real animations.
13. Dragons attack trucks.
14. Structures visibly break.
15. Trees fall.
16. Fire effects function.
17. Roads can become blocked.
18. Alternate routes remain available.
19. GPS reroutes.
20. AI understands dynamic hazards.
21. In-place vehicle recovery works.
22. Elimination works.
23. Survival placements work.
24. Cooperative configuration works.
25. All local human layouts function.
26. Spectator mode works.
27. Rematches reset destruction.
28. Consecutive matches use meaningful variation.
29. Performance remains acceptable.
30. Existing Dragon Kingdom games remain functional.

---

# SYSTEM 5 — DRAGON MAYHEM VARIANTS
## EXISTING SPORTS UNDER DRAGON ATTACK

**This is a major shared feature, not simply decoration.**

The player should be able to take certain existing Truck Sports games and play a modified version in which dragons interact with the arena.

Examples:

- Dragons attack trucks.
- Dragons drop treasure.
- Dragons breathe fire across the field.
- Dragons swoop down and grab sheep.
- Dragons carry sheep into the sky.
- Dragons cartoonishly eat sheep.
- Dragons knock down scenery.
- Dragons create physical hazards.

**The original base game must remain fully playable without these additions.**

---

# 20. OPTIONAL VARIANT ARCHITECTURE

Add a clean configuration option to compatible sports.

Suggested label:

**Dragon Mayhem: OFF / ON**

A separate named ruleset is also acceptable if that matches existing sport-variant architecture.

## 20.1 When OFF

The sport plays normally.

No dragons.

No extra gold.

No dragon-related UI.

No dragon-specific scoring.

No unwanted changes to existing scenery, weather, or AI.

## 20.2 When ON

Enable the sport-specific Dragon Mayhem configuration.

This may include:

- Dragon flyovers.
- Dragon attacks.
- Gold drops.
- Sheep grabbing.
- Environmental destruction.
- Special dragon hazards.
- Additional sound/VFX.

## 20.3 Compatibility

Do not assume every existing sport is suitable.

The modifier should be data-driven, with sport-specific compatibility rules.

Begin with fully implemented, verified variants for the following sports, provided they exist in the latest registered project:

1. **Truck Keepaway — Dragon Raid**
2. **Truck Balloon Battle — Dragonstorm**
3. **Truck Cornhole — Dragon Gold Rush**
4. **Truck Cheese Rolling — Dragon Pursuit**

These four are the initial target implementations.

If an existing mode's actual architecture makes a proposed effect incompatible, adapt the variant while preserving its core game rules.

Do not simply add the toggle without implementing working hazards.

Additional sports can be made compatible later.

---

# 21. TRUCK KEEPAWAY — DRAGON RAID

Keep the established ball-possession rules.

Add dragons circling the arena.

## Dragon behavior

Dragons periodically:

- Fly over the field.
- Dive toward selected areas.
- Breathe fire.
- Generate wing gusts.
- Drop limited treasure.

## Gameplay interaction

Dragon gusts may move trucks or affect ball positioning in a controlled way.

Fire attacks create temporary danger areas.

Gold pickups can award small bonus points under the variant rules.

Do not allow gold to replace the possession-based victory objective.

## Camera

Preserve the established Keepaway shared Arena View framing and UI quality.

Do not reintroduce giant nameplates or oversized panels.

---

# 22. TRUCK BALLOON BATTLE — DRAGONSTORM

Preserve Balloon Battle's core balloon-popping mechanics.

## Dragon hazards

Dragons may:

- Dive over competitors.
- Produce wind gusts.
- Fire controlled attacks.
- Create temporary zones where balloon damage is more dangerous.

## Balance

Avoid unavoidable instant multi-balloon eliminations.

Use visible attack warnings.

Dragon activity should increase chaos without determining every winner randomly.

## UI

Preserve the updated single-row bottom leaderboard.

Do not reintroduce persistent help banners.

---

# 23. TRUCK CORNHOLE — DRAGON GOLD RUSH

Preserve Cornhole's distinctive miniature-truck scale.

The human spectators must remain enormous relative to the trucks.

## Dragon presentation

Use appropriately scaled fantasy dragons in the environment.

A dragon may fly above the board and drop valuable gold pieces.

## Gold mechanics

Gold should:

- Fall physically.
- Be collectible through actual contact or a legitimate defined scoring interaction.
- Award a modest configurable bonus.
- Not replace normal hole scoring.
- Not obstruct the scoring hole permanently.
- Not corrupt settled-truck placement.

## Attacks

Dragons may generate gusts or suitable temporary hazards.

Honor the sport's established midair noncollision rules and landing/settling behavior.

Do not break simultaneous launching.

---

# 24. TRUCK CHEESE ROLLING — DRAGON PURSUIT

Preserve the existing Cheese Rolling downhill race and race-specific environment.

## Dragon attacks

Dragons occasionally:

- Fly over the hill.
- Breathe fire across designated sections.
- Cause wing gusts.
- Drop small obstacles.
- Grab nearby sheep if present.

## Gold drops

Dragons may release gold pieces onto safe/reachable parts of the hill.

Players may collect them for limited variant bonus points.

Do not allow bonus collection to override the actual finish-line winner if the rules remain a race.

## Sheep interactions

Where appropriate, populate designated sections with existing sheep assets.

Dragons may swoop down, grab sheep, and carry them away.

Preserve all previously approved Cheese Rolling terrain, camera, countdown, and racing systems.

---

# 25. DRAGONS DROP GOLD

Build a reusable dragon gold-drop mechanic.

## 25.1 Spawn behavior

Dragons occasionally carry treasure or release gold during flight.

The dropped gold must be visibly released from the dragon.

## 25.2 Physics

Gold should:

- Fall physically.
- Bounce.
- Settle.
- Remain collectible for an appropriate period.
- Despawn safely when expired.

Use object pooling.

## 25.3 Scoring

Gold scoring must be sport-specific.

Examples:

- Small bonus points.
- Special temporary pickup score.
- Optional side objective.

Do not allow unlimited score farming.

Do not award points merely because an animated gold effect appeared.

## 25.4 Fairness

Do not drop gold exclusively onto one player's spawn repeatedly.

Use fair placement rules.

Gold should not spawn inside walls, beneath terrain, or outside playable boundaries.

---

# 26. DRAGONS GRAB AND EAT SHEEP

**THIS SHOULD BE A DISTINCTIVE COMEDIC WORLD INTERACTION.**

Use existing sheep models and animations where available.

## 26.1 Target selection

Dragons should identify valid sheep targets.

Do not grab:

- Score-critical gameplay objectives unless explicitly allowed.
- Human players accidentally through the sheep interaction.
- Invalid already-despawned sheep.
- Objects with no proper pickup representation.

## 26.2 Swoop sequence

A dragon should:

1. Notice a sheep.
2. Turn toward it.
3. Dive or descend.
4. Extend its claws.
5. Reach the sheep.
6. Attach the sheep to the appropriate claw/grip.
7. Lift the sheep into the air.
8. Fly away while carrying it.

The sheep should remain visibly attached during flight.

Use real animation and correct attachment offsets.

## 26.3 Eating sequence

The dragon may:

- Carry the sheep toward its mouth.
- Perform an appropriate bite/eating animation.
- Produce a comedic gulp or burp.
- Remove the sheep through a clean, non-graphic transition.

No gore is necessary.

Keep it outrageous and cartoonish.

## 26.4 Exploding sheep

Some Truck Sports modes already feature explosive sheep.

If a dragon grabs a designated explosive sheep, an optional special interaction may occur:

- Dragon grabs it.
- Sheep detonates.
- Dragon reacts in surprise.
- Smoke and cartoon impact effects play.
- Dragon temporarily loses its attack opportunity.

Preserve the established explosive sheep system.

Avoid double explosions or invalid physics callbacks.

## 26.5 Spawn and cleanup

If sheep are temporary variant actors:

- Spawn them through appropriate pools.
- Animate them.
- Give them sensible wandering behavior.
- Clean them up after the match.

Do not leave sheep permanently scattered throughout normal matches when Dragon Mayhem is disabled.

---

# 27. DRAGON MAYHEM FAIRNESS AND PERFORMANCE

## 27.1 Attack warnings

Players need opportunities to respond to dragon attacks.

Use:

- Dragon shadows.
- Roars.
- Visible incoming flight paths.
- Ground attack markers where appropriate.
- Sound cues.

## 27.2 Cooldowns

Dragons need reasonable attack cooldowns.

Avoid constant unavoidable fire.

## 27.3 Hazard limits

Limit concurrent active dragons according to sport scale and performance.

A small arena should not spawn dozens of enormous fully animated dragons.

## 27.4 Sport compatibility

Each sport defines:

- Valid dragon spawn points.
- Valid aerial routes.
- Hazard intensity.
- Allowed attack types.
- Gold rules.
- Sheep availability.
- Protected gameplay zones.
- Cleanup behavior.

## 27.5 No rules corruption

Dragon Mayhem must not:

- Duplicate existing scoring events.
- Eliminate a player unfairly before a match begins.
- Break the ball or scoring hole.
- Destroy essential objectives.
- Prevent an active round from ending.
- Corrupt saved profiles.
- Permanently change the base sport.

---

# 28. DRAGON MAYHEM ACCEPTANCE TESTS

For each of the four initial variants:

1. Base sport works with Dragon Mayhem OFF.
2. Variant toggle appears correctly.
3. Dragons spawn only when enabled.
4. Dragons animate.
5. Attacks function.
6. Appropriate warning cues appear.
7. Gold drops function where configured.
8. Gold uses actual physics.
9. Gold scores correctly.
10. Sheep animate where present.
11. Dragon sheep-grabbing works.
12. Eating animation works.
13. Explosive sheep special behavior works where supported.
14. Original sport mechanics remain valid.
15. AI continues playing the original objective.
16. Split-screen UI remains readable.
17. Match completion works.
18. Rematch resets all variant state.
19. Returning to the normal ruleset removes all Dragon Mayhem effects.
20. Existing sport performance remains acceptable.

---

# 29. SKYBOX, WEATHER, AND AESTHETIC REQUIREMENTS

This expansion must receive the complete visual polish standard already established across Truck Sports.

## 29.1 Beautiful weather-responsive skyboxes

Outdoor fantasy scenes should support appropriate:

- Morning.
- Day.
- Evening.
- Night.
- Stormy weather.
- Snow in mountain areas.
- Fog.
- Rain.
- Thunderstorms.
- Volcanic lighting.

## 29.2 Animated environmental details

Include:

- Moving vegetation.
- Wind.
- Waterfalls.
- Streams.
- Birds.
- Wildlife.
- Animated villagers.
- Ambient dragons.
- Fire.
- Smoke.
- Lava.
- Weather particles.

## 29.3 Materials

Use correct high-quality materials.

Avoid:

- Default gray surfaces.
- Missing textures.
- Pink materials.
- Flat unfinished terrain.
- Floating scenery.
- Z-fighting.
- Empty backgrounds.

## 29.4 Audio atmosphere

Use:

- Dragon roars.
- Wing flaps.
- Fire.
- Thunder.
- Fantasy ambience.
- Castle crowds.
- Cave echoes.
- Gold impacts.
- Sheep sounds.
- Environmental destruction.
- Battle music.
- Victory music.

Use existing owned audio where appropriate.

---

# 30. MANDATORY PARALLEL AGENT DEVELOPMENT

Use available Sol/Codex subagents strategically.

Suggested workstreams:

## Lead Integration Agent

Own:

- Architecture.
- Shared contracts.
- Canonical acceptance ledger.
- Shared registry.
- Repository integration.
- Versioning.
- Build stability.
- Cross-expansion protection.

## Dragon Asset and Animation Agent

Own:

- Asset discovery.
- Dragon species catalog.
- Rig configuration.
- Animation controllers.
- Flight/ground animations.
- Pickup/carry/eating animations.
- Baby dragon configuration.

## Fantasy Environment Agent

Own:

- Shared kingdom.
- Roads.
- Castle.
- Mountains.
- Forest.
- Volcano.
- Caverns.
- Nests.
- Harbor.
- Mountain shelter.
- Lighting and skyboxes.

## Dragon Riders Agent

Own:

- Mounting.
- Flight controls.
- Racing.
- Combat.
- Dislodging.
- Rescue.
- AI racers.

## Hoard Heist Agent

Own:

- Stealth.
- Treasure.
- Cargo.
- Dragon alertness.
- Wake-up.
- Chase.
- Extraction scoring.

## Egg Delivery Agent

Own:

- Egg carriers.
- Egg physics.
- Mother dragons.
- Hatching.
- Baby dragons.
- Delivery AI.

## Dragon Escape Agent

Own:

- Scenario generation.
- Extraction destinations.
- Route graph.
- GPS integration.
- Dragon chase behavior.
- Survival.
- Evacuation boat.
- Escape scoring.

## Destruction Agent

Own:

- Burnable props.
- Building destruction.
- Tree destruction.
- Bridge damage.
- Road hazards.
- World reset.
- Safe-route preservation.

## Dragon Mayhem Agent

Own:

- Variant architecture.
- Keepaway.
- Balloon Battle.
- Cornhole.
- Cheese Rolling.
- Gold drops.
- Sheep interactions.
- Variant-only scoring.

## UI / Audio / Cinematics Agent

Own:

- Logos.
- Menu cards.
- HUD.
- Results.
- Camera integration.
- Audio.
- Cinematic presentation.

## Testing Agent

Own:

- Automated tests.
- Runtime verification.
- Multiplayer tests.
- Performance.
- Regression checks.
- Evidence collection.

These are suggested responsibilities.

Use only the actual available concurrency.

Do not run multiple agents into conflicting edits.

---

# 31. IMPLEMENTATION SEQUENCE

## Phase 1 — Audit and Asset Discovery

- Inspect project.
- Discover owned dragon assets.
- Inspect rigging and animations.
- Identify shared-system dependencies.
- Protect previous expansion.
- Assign ownership.
- Populate acceptance ledger.

## Phase 2 — Shared Dragon Framework

- Species catalog.
- Ground movement.
- Flight.
- AI.
- Animation states.
- Attack system.
- Carrying/grabbing.
- Dragon Director.

## Phase 3 — Shared Kingdom

- Castle.
- Mountains.
- Forest.
- Volcano.
- Caverns.
- Nests.
- Village.
- Harbor.
- Mountain refuge.
- Roads.
- Skyboxes.
- Weather.
- Ambient dragons.

## Phase 4 — Playable Vertical Slices

- Riders: Mount, fly, checkpoint, finish.
- Hoard Heist: Steal, awaken, escape, score.
- Egg Delivery: Load, transport, hatch, deliver.
- Dragon Escape: Spawn, pursue, destroy, route, evacuate.

These are milestones, not permission to stop with unfinished prototypes.

## Phase 5 — Complete Gameplay

Finish all mode-specific mechanics.

## Phase 6 — Dragon Mayhem

Implement the optional variant system and the four initial supported sports.

## Phase 7 — Art and Presentation

- Complete assets.
- Animations.
- VFX.
- Audio.
- HUD.
- Logos.
- Cameras.
- Results.
- Weather.
- Time of day.

## Phase 8 — Runtime Validation

Test actual gameplay.

Verify all four new games and Dragon Mayhem variants.

## Phase 9 — Integration and Regression

Confirm existing sports and the twenty-one-game expansion remain intact.

## Phase 10 — Final Reporting

Reconcile acceptance requirements and produce patch notes.

---

# 32. PERMANENT ACCEPTANCE LEDGER

Use the existing Truck Sports requirements ledger.

Do not create a competing backlog.

Add a Dragon Kingdom section with stable IDs for:

- Shared dragon framework.
- Asset inventory.
- Animations.
- Kingdom environment.
- Destruction.
- Dragon Riders.
- Hoard Heist.
- Egg Delivery.
- Dragon Escape.
- Dragon Mayhem shared system.
- Each initial Dragon Mayhem sport variant.
- Multiplayer.
- UI.
- Cameras.
- Performance.
- Regression testing.

Use these statuses:

- MISSING.
- PARTIAL.
- IMPLEMENTED BUT UNVERIFIED.
- VERIFIED.
- BLOCKED.

Every item needs:

- Stable requirement ID.
- Exact requirement.
- Related scene.
- Relevant scripts.
- Prefabs.
- Assets.
- Automated tests.
- Runtime evidence.
- Visual evidence.
- Human playtest findings.
- Remaining work.
- Current status.

## Verification rules

Do not mark VERIFIED merely because:

- Code compiles.
- A prefab exists.
- An agent reports completion.
- The scene opens.
- An automated test passes without proving the visual/physical behavior.

Animated dragons need actual rendered verification.

Dragon pursuit needs actual gameplay verification.

Destructible scenery must be observed responding.

Boat extraction must be tested with a truck physically aboard.

Dynamic routes must be tested across multiple seeds.

Gold and sheep interactions must be observed.

Keep incomplete requirements open.

---

# 33. MANDATORY RUNTIME TESTS

Test each new game and variant.

At minimum verify:

1. Menu registration.
2. Setup configuration.
3. Player spawning.
4. Persistent profiles.
5. Customized vehicles.
6. Human input.
7. AI behavior.
8. Multiplayer.
9. Cameras.
10. Dragon animation.
11. Dragon attacks.
12. Physics.
13. Scoring.
14. Match completion.
15. Results.
16. Rematches.
17. Return to menu.
18. Weather/time.
19. Skybox.
20. Audio.
21. Assets.
22. Performance.
23. Unity console.
24. Save compatibility.
25. Previous expansion stability.

## Specific Dragon Escape tests

Test at least:

- Fortress extraction.
- Mountain extraction.
- Boat extraction.
- Multiple scenario seeds.
- Different route selections.
- Different active dragons.
- Road destruction.
- Dynamic rerouting.
- AI extraction.
- Human competitive escape.
- Cooperative escape.
- Boat departure.
- Full world reset.
- Consecutive rematches.

## Specific Dragon Mayhem tests

Test the base sport with modifier OFF and ON.

Verify OFF returns completely normal gameplay.

Test gold, attacks, sheep pickup/eating, and variant cleanup.

## Visual tests

Inspect actual rendered gameplay at:

- 1280×720.
- 1920×1080.

Test relevant split-screen arrangements.

Do not fabricate screenshots, FPS readings, or gameplay observations.

---

# 34. PERFORMANCE REQUIREMENTS

The shared kingdom, large animated dragons, fire, physics debris, and multiplayer cameras will be demanding.

Optimize:

- Dragon meshes.
- LODs.
- Animation update rates.
- AI frequency.
- Flying path calculations.
- Fire effects.
- Destruction debris.
- Vegetation.
- Weather particles.
- Physics gold.
- Sheep.
- Camera rendering.
- Streaming/loading.
- Audio concurrency.

Use pooling and bounded encounter budgets.

Preserve high-quality nearby animations.

Record measured performance where testing is available.

---

# 35. REQUIRED FINAL TECHNICAL REPORT

Produce a detailed final implementation report.

## Dragon assets

- Packs found.
- Prefabs imported.
- Species supported.
- Animation clips available.
- Missing animations.
- Retargeting/procedural solutions.

## Shared systems

- Dragon framework.
- AI.
- Flight.
- Combat.
- Carrying/eating.
- Destruction.
- Kingdom.
- Weather and lighting.
- Camera.
- UI.

## Games

For each of the four games:

- Implemented features.
- Scenes.
- Scripts.
- Assets.
- Tests.
- Observed behavior.
- Remaining issues.

## Dragon Mayhem

For each of the four initial variants:

- Integration completed.
- Original sport regression results.
- Gold rules.
- Dragon attacks.
- Sheep interactions.
- Scoring.
- Known issues.

## Existing project protection

Report whether the twenty-one-game expansion remains intact.

Document any necessary shared-system integration.

## Acceptance

Report totals for:

- VERIFIED.
- IMPLEMENTED BUT UNVERIFIED.
- PARTIAL.
- MISSING.
- BLOCKED.

Be accurate.

Do not invent successful tests.

---

# 36. MANDATORY CLASSIC WORLD OF WARCRAFT-STYLE ALPHA PATCH NOTES

At the end of this implementation pass, create a simple player-facing changelog in the style of classic World of Warcraft patch notes.

Use the actual project Alpha version.

Suggested:

# TRUCK SPORTS: PARTY!
## ALPHA 0.8.0 — WYRMS & WHEELS

### New Games
- Added Truck Dragon Riders.
- Added Truck Dragon Hoard Heist.
- Added Truck Dragon Egg Delivery.
- Added Truck Dragon Escape.

### Dragon Kingdom
- Added a fantasy world featuring castles, mountains, dragon caves, enchanted forests, volcanic regions, and lakes.
- Added multiple animated dragon species.
- Added dragon flight, combat, and environmental interactions.

### Truck Dragon Riders
- Trucks can now ride dragons in aerial races.
- Added elemental attacks.
- Added dramatic falls and midair rescues.

### Truck Dragon Hoard Heist
- Added a dragon treasure-stealing game.
- Added noise-based dragon alertness.
- Added physical treasure extraction.
- Added dragon wake-up and pursuit behavior.

### Truck Dragon Egg Delivery
- Added fragile dragon egg transport.
- Added protective mother dragons.
- Added premature hatching and baby-dragon interactions.

### Truck Dragon Escape
- Added survival racing against attacking dragons.
- Added randomized evacuation destinations and routes.
- Added castle, mountain, and boat extraction.
- Added dragon-driven environmental destruction.
- Added dynamic navigation around destroyed roads.

### Dragon Mayhem
- Added optional dragon-enhanced sport variants.
- Added dragon attacks during selected existing sports.
- Added collectible dragon-dropped gold.
- Added animated dragons that swoop down and capture sheep.
- Added cartoonish dragon-and-exploding-sheep interactions.

### General
- Added original logos and fantasy-themed menus.
- Added polished UI, cameras, lighting, weather, and audio.
- Integrated existing named profiles and customized trucks.

**These are formatting examples and planned features, not automatic claims of completed implementation.**

Only describe implemented features as shipped.

Distinguish unverified or unfinished work.

Keep these patch notes separate from the engineering report.

Retain this requirement for every future substantial Truck Sports master prompt.

---

# FINAL DIRECTIVE

Truck Sports: Party! should now have an entire fantastical kingdom filled with enormous living dragons.

Trucks should ride dragons through the skies.

They should race between castle towers, unleash elemental attacks, and sometimes fall off their mounts.

Trucks should sneak into treasure-filled caverns and try not to wake an ancient dragon.

They should deliver fragile dragon eggs while protective mothers patrol overhead and babies hatch unexpectedly.

**And most importantly, trucks should have to flee for their lives while dragons aggressively destroy an entire medieval kingdom.**

One match might require escaping into a fortified castle.

The next might require racing through forests into a mountain refuge.

Another might end with a frantic drive onto an evacuation boat while dragons burn down the harbor behind the player.

The destination, hazards, and viable routes must change between rounds.

The dragons must create REAL consequences in the world:

- Trees fall.
- Buildings burn.
- Bridges break.
- Roads become blocked.
- Debris scatters.
- Trucks get attacked.
- Escape routes change.

Existing sports should also gain optional Dragon Mayhem variants.

Dragons should fly overhead, attack competitors, scatter gold across arenas, grab sheep in their claws, and occasionally eat those sheep in ridiculous cartoon fashion.

**ALL OF THIS MUST USE THE DEVELOPER'S ACTUAL OWNED DRAGON ASSETS WHERE AVAILABLE.**

Moving dragons must be animated.

Gameplay physics must be genuine.

AI must actually participate.

Cameras must remain readable.

Skyboxes and environments must be beautiful.

UI must match the polished existing sports.

The original sports must remain unchanged when Dragon Mayhem is disabled.

The concurrent twenty-one-game expansion must remain intact.

Use parallel agents intelligently.

Maintain the permanent evidence-based acceptance ledger.

Verify actual gameplay.

Finish with a complete technical report and classic WoW-style Alpha patch notes.

**BUILD THE DRAGON KINGDOM. LET THE DRAGONS DESTROY IT. LET THE TRUCKS TRY TO SURVIVE.**