# SOL 6.1 HIGH — INTERSTATE: HAULER
# TRUCK SPORTS: PARTY!
## MASTER HUMAN PLAYTEST REPAIR & RELEASE-QUALITY POLISH PASS
### Execute AFTER the Global Shared-Camera Standardization Pass

---

# 0. MISSION AND NON-NEGOTIABLE RULES

Implement all outstanding repairs, balancing changes, environment improvements, UI overhauls, and gameplay corrections described below.

These requirements come from extensive hands-on testing of Truck Sports: Party!

The original playtest notes have been updated with newer findings. **The latest instruction for each sport takes precedence over any contradictory earlier requirement.**

This document is the authoritative specification for this repair pass.

This is an **implementation task**, not a request for another development proposal or speculative backlog.

Inspect the existing Unity project, fix the problems, integrate the assets, test the actual gameplay, and record the results.

## 0.1 Important Execution Order

This task will run after a separate global shared-camera standardization pass.

That pass establishes **Truck Keepaway's existing Arena View camera as the reference for shared broadcast cameras throughout Truck Sports**.

Preserve that work.

Do not reintroduce excessive camera distances, automatically frame every distant AI competitor, or overwrite the new common camera settings.

Sport-specific camera fixes in this document must operate within that standardized framework.

## 0.2 These Notes Were Collected Before More Recent Development

Some reported issues may already have been resolved.

Before implementing each change:

1. Inspect the current scene, scripts, and prefabs.
2. Determine whether the requested feature already exists.
3. Verify whether it actually works.
4. Repair missing or defective behavior.
5. Preserve improvements already completed.
6. Record the result in the permanent acceptance ledger.

Do not blindly rebuild working features.

Do not restore old scene versions over newer successful development.

## 0.3 Preserve Confirmed Working Systems

The developer has explicitly approved the core gameplay of several modes.

Protect these systems:

**Truck Bowling:** Current gameplay and environment presentation are considered excellent. Only the explicitly requested text and UI improvements are authorized.

**Truck Poker:** The underlying poker game is excellent. Preserve rules, dealing, scoring, and other working gameplay.

**Cheese Rolling:** The core game is good. Focus on VFX, UI, and animated cat assets.

**Terrier Racing:** Current gameplay is good apart from weather wind causing excessive flight deviation. Do not perform another general speed or ramp overhaul.

**Trailer Plug:** The core mechanic is fun. Repair attachment, scaling, and target dimensions.

**Fare Fight:** Passenger pickups, deliveries, and stealing work. Preserve them.

**Minefield:** Preserve mine explosions and the fundamental distance challenge.

**Musical Parking:** Core gameplay is good. Focus on presentation and its specific split-screen HUD issues.

**Bungee Destruction:** Preserve its established swing controls.

## 0.4 Project-Wide Technical Requirements

Use the existing architecture for:

- NWH Vehicle Physics 2.
- Shared camera management.
- Human and AI input assignment.
- Local split-screen.
- Global participant names.
- Weather.
- HUD elements.
- Match lifecycle.
- Collision detection.
- Explosions.
- Scoring.
- Environmental assets.
- Object pooling.
- Save settings.

Avoid duplicate managers, unnecessary architectural rewrites, and incompatible mode-specific systems.

## 0.5 Use the Existing Asset Library

The project has a substantial collection of imported assets.

Before using primitive replacement meshes or basic placeholder effects, inspect available:

- Buildings.
- Explosion effects.
- Water systems.
- Crowd models.
- Animated character assets.
- Cat models.
- UI graphics.
- Environmental props.
- Materials.
- Terrain assets.
- Vehicle effects.
- Sounds.

Use high-quality existing assets where appropriate.

Do not leave obviously unfinished geometry or generic placeholder effects.

---

# 1. TRUCK FIELD GOAL

## 1.1 Fix the Broken Audio After Kicking

Human testing revealed an extremely unpleasant, broken sound after kicking the football.

Investigate:

- AudioSource configuration.
- Football impact sound.
- Repeated collision callbacks.
- Audio playback loops.
- Mixer routing.
- Volume clipping.
- Pitch modulation.
- Invalid audio state.
- Duplicate sound instances.
- Impact sounds being triggered repeatedly while the ball remains in contact.

Fix the root cause.

A valid kick should produce one clean, satisfying football impact sound, followed by appropriate flight and crowd audio.

No distorted looping, buzzing, repeated collision spam, or broken sound playback.

Test repeated kicks, missed kicks, successful field goals, and rematches.

## 1.2 Align the Holder Truck Correctly

The holder truck is currently positioned incorrectly relative to the football.

Correct its:

- Scale.
- Position.
- Rotation.
- Alignment with the football.
- Orientation toward the uprights.
- Distance from the kicking path.

The holder should be immediately beside the upright football, like a proper placekick holder.

The football must remain stable and upright until contact.

The holder must not obstruct the player's run-up.

Preserve the actual truck-as-foot kicking system.

## 1.3 Holder Truck Victory Dance

When a field goal succeeds, the holder truck should perform a cute celebration.

Possible moves:

- Happy cab bounces.
- Side-to-side wiggles.
- Suspension hops.
- Excited wheel movements.
- Proud small wheelie.
- Cheerful swaying.

Use the expressive truck animation infrastructure where available.

The celebration should begin after the score is confirmed.

Do not allow the animation to disturb the ball's flight or scoring detection.

## 1.4 Preserve Other Football Features

Keep the current:

- Truck-as-foot implementation.
- End zone.
- Uprights behind the end zone.
- Truck Sports: Party! branding.
- Wind effects.
- Weather selection.
- Skill-based kicking.
- Crowd reactions.
- Scoring.
- Cameras.

### Acceptance

The holder and football are correctly aligned, the kick audio works, and a successful field goal triggers a visible holder-truck dance.

---

# 2. TRUCK FISHING

**PRIORITY: CRITICAL GAMEPLAY REGRESSION.**

The fishing mode is currently broken because fish pass through the net without registering catches.

## 2.1 Restore Actual Fish Catching

Investigate:

- Fish colliders.
- Net colliders.
- Trigger configuration.
- Physics layers.
- Collision callbacks.
- Continuous collision detection.
- Rigidbody interpolation.
- Fish movement implementation.
- Net capture logic.
- Scoring events.
- Object pooling.
- Recently changed fish velocity systems.

A fish entering the actual valid catching region of the net must register correctly.

It must not pass directly through an active net.

It must award the proper score.

Make the capture logic reliable at all supported fish speeds.

Do not simply enlarge an invisible trigger far beyond the actual visible net.

## 2.2 Correct Fish Movement Speed

The previous requests for much faster fish have not produced the desired result.

Start fish at approximately **200% of their original initial movement speed**.

Then progressively increase the baseline throughout the round.

Suggested configurable baseline:

| Round Time | Original-Speed Multiplier |
|---|---:|
| Start | 2.0× |
| 30 seconds | 2.5× |
| 60 seconds | 3.0× |
| 90 seconds | 3.5× |
| 120 seconds | 4.0× |

Add independent random speed spikes and brief slowdowns.

The underlying speed progression must continue rising even though individual fish temporarily accelerate and decelerate.

Measure real movement speed, not just animation playback.

Do not let faster fish break net collision detection.

## 2.3 Redesign the Waterway

Reduce the horizontal fish-travel area to approximately **half its current width**.

Use the newly available space to create attractive land along both sides.

Build a complete winding river environment.

Include:

- Natural riverbanks.
- Grass and vegetation.
- Rocks.
- Trees.
- Terrain elevation.
- Appropriate water materials.
- Scenic details.
- At least one prominent bend in the channel.

Fish should emerge from around the bend rather than appearing abruptly in the player's view.

Maintain reasonable camera framing.

Do not place solid terrain inside the valid net movement area.

## 2.4 Increase Fish Size by 30%

Increase the actual fish models to approximately 130% of their current size.

Adjust relevant colliders and spawn clearances.

Preserve different fish sizes and species characteristics.

## 2.5 Preserve Other Fishing Features

Retain:

- Existing scoring.
- Fish values.
- Catching controls.
- Previously requested offscreen spawns.
- Debris and surprise catches.
- Net-clearing QTE where implemented.
- Polished fishing HUD.
- Weather.
- Rematches.

### Acceptance

Test at least ten consecutive legitimate net catches with varied fish sizes and movement speeds.

Every valid catch must register correctly.

The river is narrower, has scenic bends, and contains visibly faster and larger fish.

---

# 3. PEDESTRIAN PLOWING

## 3.1 Reduce Negative Targets

There are too many -100 targets.

Adjust the active target-category weights so negative targets are a minority.

Use approximately 15–20% as an initial configurable target, subject to gameplay balancing.

Preserve the existing per-point-category prefab arrays.

Do not assign every prefab to every scoring category.

## 3.2 Make Negative Targets Obvious

Add a highly visible radiant aura to -100 targets.

Use a distinctive combination of:

- Colored glow.
- Ground halo.
- Particle aura.
- Pulsing outline.
- Negative-score icon.

Make the visual readable in different weather and lighting.

The player should instantly know a negative target is undesirable.

## 3.3 Restore Truck Taxi Pedestrian Physics

Pedestrians currently slow down or overturn the player's truck far too much.

They behave like disproportionately heavy obstacles.

Reuse the established Truck Taxi pedestrian impact and ragdoll systems.

Requirements:

- Pedestrians react to truck impacts.
- Appropriate ragdoll forces apply.
- Pedestrians do not behave as immovable walls.
- The truck does not unrealistically flip merely from ordinary pedestrian contact.
- Reactions remain exaggerated and comedic.
- Impact scoring remains functional.

Do not modify the player's entire vehicle mass or handling to conceal the problem.

Fix pedestrian collision behavior.

## 3.4 Simplify the HUD

Replace the crowded current interface with a compact, polished point scoreboard.

Show:

- Player/truck identity.
- Current points.

Additional important notifications can appear contextually, but not as permanent blocks of text.

Use the existing polished Truck Sports UI language.

### Acceptance

Negative targets are less frequent, visibly marked, and pedestrians use appropriate Truck Taxi-style physics.

The HUD is compact and visually finished.

---

# 4. CAB CHAOS

## Clarify and Verify the Actual Game Identity

Human testing found Cab Chaos resembles Pedestrian Plowing, creating confusion about its purpose.

There is also uncertainty about whether it was intended to be a Simpsons Road Rage-style passenger-delivery mode.

Investigate the original project implementation, design documentation, scene history, and current mechanics.

Determine what the actual intended distinction is between:

- Cab Chaos.
- Pedestrian Plowing.
- Fare Fight.
- Truck Taxi.

Fare Fight currently provides the working passenger pickup, delivery, and theft gameplay.

Do not accidentally duplicate Fare Fight or remove existing working Cab Chaos mechanics.

### Required Work

1. Identify the original intended gameplay loop.
2. Compare it against the actual implemented mechanics.
3. Restore missing intended features where evidence supports them.
4. Improve the game-menu description and instructions.
5. Clearly communicate what makes Cab Chaos different.
6. Report any unresolved design ambiguity.

Do not unilaterally redesign Cab Chaos into an unrelated new sport.

If major design changes are genuinely needed, flag them for a later gameplay decision.

---

# 5. TRUCK KICKBALL

## 5.1 Broadcast Camera Position

Maintain the shared-camera distance standard established by Truck Keepaway.

However, use a sport-specific camera orientation resembling a real broadcast booth.

Position the main broadcast viewpoint approximately on the first-base side, between home plate and first base, elevated appropriately.

During pitching:
- Frame the pitcher and batter.

During fielding:
- Follow the ball and relevant fielders.

During baserunning:
- Follow the active runner and relevant base.

Do not zoom out to show the entire stadium at the expense of truck visibility.

Do not undo the separate global camera-standardization pass.

## 5.2 Fix Home-Run Resolution

Current bug:

The player hit a home run, received the run, but remained in a broken gameplay state while the defense continued attempting to tag them.

**Once a legal home run is confirmed, live defensive play must end.**

Immediately:

1. Resolve the home run.
2. Award all legally entitled runs.
3. End relevant tagging and defensive interactions.
4. Prevent additional outs from that completed play.
5. Transition into a celebration state.
6. Restore correct input and camera ownership for the next play.

## 5.3 Add Home-Run Celebration

Use:

- Large HOME RUN banner.
- Stadium crowd reaction.
- Fireworks or appropriate effects.
- Truck celebration.
- Cinematic camera.
- Score animation.

The celebration must not interrupt the authoritative game-state resolution.

## 5.4 Waiting-Line Camera Freedom

When a human player is waiting to bat:

- Allow free camera look.
- Allow them to observe the field.
- Allow them to watch other competitors.
- Preserve appropriate cockpit or spectator viewing controls.

Do not leave them trapped in a fixed camera.

## 5.5 Fast-Forward to Next At-Bat

Add an option to fast-forward AI-controlled play until the user's next batting opportunity.

Preserve valid scoring and inning progression.

In local multiplayer, do not skip another human player's active turn without appropriate agreement or confirmation.

### Acceptance

Legal home runs resolve instantly, celebrations work, waiting players can look around, and AI-only turns can be accelerated safely.

---

# 6. TRUCK SKY TOW RINGS

## 6.1 Repair Camera Jitter

The camera is extremely jerky.

Investigate:

- Spline update order.
- Tow vehicle physics.
- Camera smoothing.
- Rotation interpolation.
- Rigidbody interpolation.
- Camera parent hierarchy.
- Target positions.
- FixedUpdate/LateUpdate interactions.
- Cable oscillation.

Eliminate abrupt camera corrections and high-frequency jitter.

Maintain a smooth, readable aerial view.

## 6.2 Fix Ring Difficulty

Current ring alignment is excessively difficult.

Improve:

- Normal ring dimensions.
- Relative positioning.
- Spacing.
- Tow speed.
- Flight stability.
- Air correction authority.
- Spline corner behavior.
- Ring approach angles.

Ordinary rings should be challenging but regularly reachable through skillful play.

Reserve precision difficulty for clearly marked bonus rings.

## 6.3 Replace Ring Assets

Find high-quality existing ring models and suitable effects from the project asset collection.

Use:
- More impressive ring meshes.
- Better materials.
- Glowing accents.
- Particle trails.
- Ambient particles.
- Clear success effects.
- Attractive missed-ring feedback.

Do not leave the current low-quality placeholder rings.

## 6.4 Fix Boat Turns and Ring Alignment

The towing boat currently makes turns that throw the airborne truck far outside the intended ring path.

Correct the coupling between:

- Boat spline.
- Boat acceleration.
- Turn radius.
- Tow cable.
- Swing damping.
- Truck trajectory.
- Ring placement.

Make normal rings actually reachable during and after turns.

Preserve the scenic Arctic course and approximately two-minute run length.

### Acceptance

A complete run has smooth cameras, reachable rings, polished visuals, and stable towing around bends.

---

# 7. TRAILER PLUG

## 7.1 Correct Vehicle and Trailer Scale

The truck and trailer dimensions currently look inconsistent.

Use accurate relative scaling.

Inspect imported model scales and root transforms.

Do not arbitrarily resize only one component.

## 7.2 Use Proper Hitch Attachment

Reuse the existing trailer hookup and detachment infrastructure from Interstate: Hauler.

The trailer should:

- Align correctly with the truck.
- Follow naturally while attached.
- Remain stable during bungee motion.
- Disconnect cleanly when released.
- Preserve real momentum and rotation.
- Become an independent physical object after detachment.

No visual gap, floating hitch, or unrealistic scale mismatch.

## 7.3 Enlarge the Hole

The sinkhole is currently too small to receive or be sealed by the trailer.

Resize it to allow physically plausible:

- Partial plugging.
- Trailer entry.
- Bridging.
- Successful centered sealing.

Keep coverage detection consistent with actual trailer geometry.

## 7.4 Preserve Working Mechanics

Keep:

- Bungee controls.
- Trailer release gameplay.
- Existing Pedestrian Plowing pedestrian roster.
- Pedestrian impacts.
- Plug scoring.
- Crowd spawning.
- Cinematic trailer impact cameras.

### Acceptance

Truck and trailer have believable relative dimensions, attach and detach correctly, and the hole is physically possible to plug.

---

# 8. TRUCK FARE FIGHT

## 8.1 Objective Direction Arrow

Add a large, clearly readable directional arrow showing the player's current objective.

Arrow target changes automatically:

**Seeking Passenger:** Point toward the active passenger pickup.

**Another Player Has Passenger:** Point toward the passenger-carrying opponent when stealing is the current objective.

**Carrying Passenger:** Point toward the passenger destination.

Use the actual active objective position.

Update immediately after pickups, thefts, drop-offs, or objective reassignment.

## 8.2 Restore Truck Taxi Pedestrian Physics

Pedestrians currently behave like indestructible obstacles.

Reuse the existing Truck Taxi pedestrian collision/ragdoll logic.

They must react physically to truck contact without behaving like concrete barriers.

## 8.3 Fix Dashboard Clipping

Part of the dashboard visually escapes or glitches outside the truck during high-speed driving.

Investigate the cockpit mesh hierarchy, animation offsets, interpolation, near clipping, and vehicle motion.

Keep dashboard geometry properly inside the vehicle.

## 8.4 Completely Repair AI Objective Pursuit

AI currently drives aimlessly and collides with walls instead of pursuing the current goal.

Implement reliable objective-driven states:

- SEEK PASSENGER.
- STEAL PASSENGER.
- DELIVER PASSENGER.
- RECOVER FROM BLOCKED PATH.
- REPLAN AFTER OBJECTIVE CHANGE.

AI must always attempt the current valid objective.

Use real navigation, reversing, turning, obstacle avoidance, and route recovery.

Do not fake pickups or deliveries.

## 8.5 Fair Initial Passenger Distances

**NEW REQUIREMENT:** All competing trucks must begin at approximately equal driving distance from the first passenger.

Place competitors on different or opposing sides of the pickup where practical.

Requirements:

- Fair route distances, not merely equal straight-line distances.
- Different starting directions.
- No vehicle spawn overlaps.
- No unfair immediate pickup advantage.
- Accessible road connections.
- Working AI routing from all starting positions.

Test route lengths to ensure competitive fairness.

## 8.6 Preserve Existing Gameplay

Passenger pickup, delivery, and stealing already work and must remain intact.

### Acceptance

Human and AI competitors can pursue objectives from fair starting positions, pedestrians respond properly, and all pickup/steal/delivery actions still work.

---

# 9. TRUCK SNAKE

## 9.1 Complete HUD Modernization

Replace the existing poor interface with polished player information.

Display:

- Truck portrait.
- Player name.
- Score.
- Trailer count.
- Speed in MPH.
- Distance.
- Survival time.
- Relevant objective arrow.

Use clean layout and branded graphics.

## 9.2 Fix Eliminated AI Cameras

AI cameras behave wildly after their trucks are eliminated.

Immediately release or transition invalid camera targets.

Prevent cameras from following destroyed or removed vehicles.

Respect the Show AI Cameras preference.

## 9.3 Faster Acceleration and Better Turning

Apply mode-specific improvements to:

- Acceleration.
- Throttle responsiveness.
- Turning radius.
- Low-speed steering.
- Arcade maneuverability.

Do not change handling globally.

Preserve the trailer-snake following system.

## 9.4 Populate the Town

Add more:

- Buildings.
- Trees.
- Traffic.
- Pedestrians.
- Roadsides.
- Industrial objects.
- Landmarks.
- Environmental ambience.

Keep enough open routes for long trailer trains.

## 9.5 Respawning Trailer Pickups

Keep new pickups spawning throughout the round.

Place them at valid, reachable locations.

Do not allow all pickups to disappear permanently.

## 9.6 Nearest-Pickup Arrow

Add a clear directional indicator toward the nearest reachable trailer pickup.

Update the target as pickups are collected or replaced.

Do not point toward unreachable objects behind buildings.

### Acceptance

A long gameplay run continues receiving pickups, the town feels populated, AI cameras remain stable, and the truck has suitably responsive arcade handling.

---

# 10. MINEFIELD

## 10.1 Minimal Polished HUD

Normal gameplay should show only:

- Truck icon/portrait.
- Current cumulative scored distance.

Remove the overwhelming collection of text and status information.

Use an attractive compact panel.

## 10.2 Correct Cumulative Distance

**NEW CRITICAL CORRECTION: Distance currently appears to begin counting only after an explosion.**

That is wrong.

Distance must be counted **from the starting line**, including progress before any explosion.

Measure total valid forward course progression relative to the start.

An explosion may launch the truck farther, but it must not initialize or reset the distance calculation.

## 10.3 Out-of-Bounds Distance

If the truck falls beyond the map, finalize the score at the last valid grounded course position.

Do not count arbitrary falling distance.

Do not zero the score.

Allow legitimate airborne travel that eventually lands on the course.

## 10.4 Map Width and Side Mountains

Ensure the earlier physical map improvements exist:

- Approximately 2× original lateral width.
- Preserve the previously requested extended length.
- Tall mountains along both sides.
- Appropriate cliffs and forests.
- Real terrain collision.
- Minimal gaps in natural barriers.

Do not reduce mine explosion strength.

## 10.5 Preserve Gameplay

Minefield is otherwise satisfactory.

Do not rework controls, mines, or their powerful launch behavior.

### Acceptance

Score increases from the starting line, remains cumulative, and finalizes correctly if the truck leaves the terrain.

The extended map and mountains are physically present.

---

# 11. TRUCK BOWLING

## LATEST INSTRUCTION OVERRIDES EARLIER BOWLING REQUESTS

**The developer now considers Truck Bowling's gameplay and environment presentation perfect.**

DO NOT:

- Rebuild the bowling alley.
- Change gameplay.
- Retune physics.
- Change the camera.
- Rework the pin mechanics.
- Add crowds or new decorations.
- Modify gutters.
- Change the back wall.
- Replace existing art unnecessarily.

### Authorized Changes Only

**1. Remove plain text from the scene.**

Remove obsolete raw text and placeholder labels.

**2. Make the existing gameplay UI polished.**

Convert necessary gameplay information into clean, attractive Truck Sports HUD components.

Preserve the currently working information and scoring.

Do not redesign the environment or change the fundamental presentation.

### Acceptance

The actual bowling experience remains unchanged.

Only the unwanted plain text and unfinished HUD presentation are improved.

---

# 12. TRUCK BILLIARDS

Perform a full polished HUD pass.

Convert existing textual information into proper UI.

Include currently relevant:

- Player/truck identity.
- Score.
- Turn.
- Ball objective.
- Remaining balls.
- Relevant shot information.

Do not obscure the billiards arena.

Preserve gameplay and physics.

---

# 13. TRUCK HOCKEY

## 13.1 CRITICAL: FIX BROKEN GOAL DETECTION FIRST

**NEW HIGH-PRIORITY REGRESSION:** The puck can enter the goal without registering a goal.

Investigate:

- Goal trigger boundaries.
- Puck collider.
- Collision detection.
- Net volume.
- Trigger callbacks.
- Scoring ownership.
- Physics speed.
- State machine.
- Goal cooldowns.
- Round reset.
- Recent shared-system modifications.

A legal puck crossing the correct goal plane must register a goal.

Use robust crossing detection where necessary to handle fast pucks.

A goal must:

1. Award the correct team.
2. Update the score.
3. Trigger goal celebration.
4. Transition to the correct faceoff/reset state.

Do not award false goals when the puck merely touches the outside of the net.

## 13.2 Complete UI Modernization

After goal detection is repaired, overhaul:

- Main mode setup.
- Team selection.
- Player information.
- Scoreboard.
- Period/time display.
- Goal notifications.
- Relevant statistics.
- Pause menu.
- Results.
- Rematches.

Use polished existing Truck Sports styling.

### Acceptance

Repeated legitimate goals score reliably at different puck speeds, and all game UI meets release-quality standards.

---

# 14. TRUCK AIR HOCKEY

Perform a substantial release-quality menu and HUD redesign.

Include:

- Polished player setup.
- Proper scoreboard.
- Match time.
- Player colors.
- Goal feedback.
- Pause menu.
- Victory screen.
- Rematch presentation.

Preserve existing air hockey gameplay and physics.

Do not obstruct the playfield.

---

# 15. SCRAP HEAP

Move these two sports into the project's existing Scrap Heap category:

- Truck Log Rolling.
- Trampoline Dodgeball.

Do not delete them.

Do not destroy their assets.

Keep them accessible through the Scrap Heap section.

Remove them from the primary active-games lineup.

---

# 16. TRUCK POKER

## 16.1 Preserve the Excellent Core Game

The game itself is excellent.

Do not rewrite poker rules, hand evaluation, betting, card handling, or working gameplay.

## 16.2 PS2 World Series of Poker-Inspired UI

Research visual references from the PlayStation 2-era World Series of Poker games, particularly World Series of Poker and World Series of Poker: Tournament of Champions.

Use their table-oriented information hierarchy as inspiration.

Create an original modern Truck Sports interpretation with:

- Clear card presentation.
- Player chip information.
- Active-player highlighting.
- Pot values.
- Betting options.
- Dealer indicator.
- Readable table layout.
- Appropriate nameplates.
- Strong visual hierarchy.
- Attractive typography.
- Proper controller support.

Do not copy proprietary artwork.

Use the established Truck Sports palette and branding.

## 16.3 Animate AI Trucks

The AI trucks are completely stiff during poker.

Give them subtle expressive behaviors:

- Looking toward cards.
- Small steering/head-like movements.
- Cab wiggles.
- Nervous shaking.
- Turning toward other bettors.
- Looking toward the dealer.
- Celebrating wins.
- Reacting to losses.
- Subtle suspension movement.

Keep movements small and context-sensitive.

Use safe visual pivots or animation systems that do not physically disturb their parking positions.

## 16.4 Preserve Previous Poker Presentation Features

Maintain:

- Accurate card ranks and suits.
- Dealer animations.
- Card reveals.
- Hand-ranking announcements.
- End-of-hand showdown.
- Winner celebrations.
- Correct pot resolution.

### Acceptance

The underlying game remains unchanged and working, while the UI and truck reactions feel like a polished poker game.

---

# 17. CHEESE ROLLING

## 17.1 Replace Placeholder Explosion VFX

Search existing owned assets for suitable explosion effects.

Replace low-quality placeholder explosions with proper:

- Fireballs.
- Smoke.
- Sparks.
- Debris.
- Impact effects.

Preserve explosion mechanics.

## 17.2 Complete HUD Overhaul

Modernize:

- Player/truck identity.
- Current race position.
- Cheese indicator.
- Timer.
- Countdown.
- Relevant race feedback.
- Results.

Keep the cheese marker legible against snow using shadow or outlining.

## 17.3 Use Owned Animated Cat Models

**NEW REQUIREMENT:** The developer owns animated cat models.

Find these assets in the project or imported packages.

Replace the stationary cat models in Cheese Rolling with appropriate animated versions.

Use available animation clips such as:

- Idle.
- Walking.
- Running.
- Turning.
- Other relevant behavior.

The cats must actually animate during gameplay.

Properly configure rigs, Animator components, movement transitions, and grounding.

Do not leave static cats after the replacement.

If the referenced assets are not present in the accessible project, identify the missing assets rather than inventing that they were found.

## 17.4 Preserve Racing Gameplay

The underlying Cheese Rolling gameplay is good.

Do not alter the mountain, speed, steering, physics, or course unless an existing regression specifically requires repair.

---

# 18. TRUCK DOCK DIVING

## 18.1 Denser, More Varied Crowds

Populate the stands substantially more densely.

Use varied spectator models and placements.

Include many attractive adult female spectators, varied clothing, hairstyles, poses, and animations.

Allow novelty spectators where appropriate, such as:

- Dogs.
- Trucks.
- Other suitable crowd props.

Avoid rigid, evenly spaced rows of identical spectators.

Use crowd optimization where practical.

## 18.2 Enclose and Complete the Pool

The pool has visible unfinished areas, particularly toward the back.

Complete the venue using real environmental assets.

Add appropriate:

- Pool walls.
- Spectator structures.
- Railings.
- Finished scenery.
- Poolside objects.
- Proper surrounding geometry.

Remove visible unfinished backfaces.

## 18.3 Replace Placeholder Run-Up Art

The approach section should look like a finished sporting venue.

Replace primitive geometry and generic materials.

## 18.4 Animated Water and Splashes

Find and reuse appropriate existing water assets.

The water must:

- Animate naturally.
- Respond to truck impacts.
- Produce visible splashes.
- Produce ripples and foam.
- Use appropriate reflections.
- Play suitable impact sounds.

Do not leave a static blue pool surface.

## 18.5 Stabilize Airborne Speech Bubbles

**NEW REQUIREMENT:** The speech/text bubbles shake excessively inside their small display area.

Fix their movement.

Use smooth interpolation, stabilized world anchoring, or appropriate screen-space positioning.

The bubble must:

- Track its truck smoothly.
- Avoid rapidly jittering.
- Remain readable.
- Fit within its display area.
- Avoid clipping.
- Fade naturally.

Preserve cute Canadian slang phrases.

### Acceptance

The audience is dense and varied, the pool and run-up are finished, water splashes properly, and speech bubbles do not shake excessively.

---

# 19. TRUCK KEEPAWAY

## LATEST INSTRUCTION OVERRIDES EARLIER ARENA EXPANSION REQUESTS

The earlier request for a 30% larger arena is superseded.

**The current request is approximately THREE TIMES the playable area and approximately ONE-THIRD the current obstacle density.**

## 19.1 Triple the Playable Arena Area

Increase the actual physical playable surface area to approximately **3× the current baseline**, not simply the camera zoom.

This is a footprint target, rather than a requirement to triple both dimensions and accidentally create nine times the area.

Extend:

- Floor.
- Collision surface.
- Arena boundaries.
- AI navigation.
- Truck spawns.
- Ball spawns.
- Hazard distribution.
- Camera framing where necessary.

Preserve the shared camera distance standard.

The camera should track important action rather than zooming dramatically outward to show the entire enlarged arena.

## 19.2 Reduce Obstacle Density to Approximately One-Third

**Three times less dense** means aim for approximately one-third the prior obstacle density per unit of playable area.

There should still be plenty of interesting objects throughout the expanded arena.

Do not make it completely empty.

Distribute props widely.

Prioritize:

- Open driving lanes.
- Large central chase spaces.
- Safe spawn locations.
- Fair ball access.
- Reasonable AI navigation.
- More room for high-speed collisions.

Avoid dense clusters.

## 19.3 MAKE ALL GAMEPLAY OBSTACLES DESTRUCTIBLE

**NEW REQUIREMENT:** Every obstacle placed within the active arena should be capable of reacting to and being destroyed by sufficient physical contact.

This includes applicable:

- Barrels.
- Crates.
- Lightweight barriers.
- Decorative obstacles.
- Destructible hazard props.
- Other physical arena objects.

Ordinary objects should also be movable when appropriate.

Heavy structures can require stronger impacts.

The main arena floor and necessary perimeter containment do not need to collapse; they are structural boundaries, not gameplay obstacles.

### Required Destruction

- Contact causes proportional physical response.
- Small impacts can push or damage appropriate objects.
- Larger impacts can break them.
- Destruction produces relevant debris and effects.
- Objects stop obstructing the course once broken.
- Scoring/possession rules remain unaffected unless explicitly designed otherwise.
- Rematches restore the arena.

Do not make barrels behave like concrete walls.

## 19.4 Preserve Existing Keepaway Mechanics

Maintain:

- Arena View.
- First Person.
- Offscreen ball arrow.
- Bottom scoreboard.
- Ball theft.
- Possession scoring.
- Explosive sheep.
- AI competition.
- Weather.
- Global participant names.

### Acceptance

The arena has approximately three times the physical playable footprint, obstacle density is substantially reduced, remaining obstacles are spread out, and actual impacts can destroy them.

---

# 20. TERRIER RACING

## LATEST MOVEMENT INSTRUCTION OVERRIDES PRIOR SPEED-TUNING REQUESTS

Earlier notes described excessive jumping speed and overshooting landing areas.

**The later playtest determined the main problem is the WEATHER WIND, not the basic truck movement.**

The current Terrier Racing gameplay is otherwise considered good.

## 20.1 Reduce Wind Intensity by Approximately 30%

Apply a mode-specific weather wind-force multiplier of approximately:

**TerrierRacingWindMultiplier = 0.70**

Use this as an initial value and expose it for tuning.

Reduce how much wind pushes airborne vehicles.

Keep weather visually active.

Preserve different weather conditions.

Do not globally reduce wind in unrelated sports.

## 20.2 Do Not Rework Racing Physics

Do not unnecessarily adjust:

- Vehicle acceleration.
- Ramp speed.
- Jump impulse.
- Course geometry.
- Normal airborne control.
- Existing propulsion.
- Landing distances.

Only make further movement changes if testing proves a separate genuine defect.

## 20.3 Replace the HUD with BASE Jumping-Style Compact Score Presentation

Remove the oversized repeated scoreboard/status layout.

Use the compact central split-screen arrangement described in Section 21.

For each human participant, show only the necessary racing information in a small, polished layout.

Avoid occupying large horizontal or vertical strips.

Adapt to different player counts.

### Acceptance

Weather no longer throws airborne trucks dramatically off course, existing racing feel is preserved, and the compact HUD looks polished.

---

# 21. TRUCK BASE JUMPING

## 21.1 Add Smoke to Flares

The existing flares need visible smoke.

Use appropriate particle systems.

Smoke should:

- Emerge from the actual flare position.
- Move convincingly.
- Respond to wind where appropriate.
- Remain visible at useful distances.
- Avoid excessive performance cost.

## 21.2 REMOVE THE MASSIVE TOP HUD BAR

The large top scoreboard/status bar must be removed completely.

Do not leave empty panel space behind it.

## 21.3 ONE CENTRAL SPLIT-SCREEN SCOREBOARD

For four-player split-screen:

Use the shared intersection of the four screen quadrants as the scoreboard anchor.

Each participant should have one compact information grouping near their own inner quadrant corner.

Layout concept:

- Player name at the top.
- Truck icon.
- Score next to the truck icon.

Only display each player's score ONCE.

Do not repeat all four scores inside every viewport.

Do not cover large sections of gameplay.

Use polished visual styling.

Support one-, two-, and three-human layouts with appropriate alternative anchoring.

Respect the shared Show AI Cameras option.

### Acceptance

No giant top bar, no duplicated player-score overlays, and one compact scoreboard group per relevant participant.

---

# 22. TRUCK MATTRESS RACING

## 22.1 NO AUTOMATIC FLIP RESET

**NEW REQUIREMENT:** When a player flips their truck, do not automatically reset it.

Instead, display a small contextual prompt:

**RESET TRUCK?**

Provide a button or controller action to confirm.

Until the player confirms:

- Preserve their current vehicle position.
- Preserve their physics state.
- Do not teleport them.
- Do not automatically flip them upright.
- Allow any legitimate self-recovery mechanics already available.

The popup should not obscure the entire screen.

AI competitors may use appropriate autonomous recovery logic, but human players must retain reset choice.

## 22.2 Use the BASE Jumping HUD Layout

Apply the same compact central split-screen presentation.

For Mattress Racing, show only:

- Running position.
- Current race time / completion time as appropriate.

Avoid additional large status panels.

Use a truck icon or name only where needed for clear participant identification.

Do not repeat the entire leaderboard in every quadrant.

## 22.3 Preserve Existing Mattress Systems

Keep:

- Physics-based mattresses.
- Loose player-colored pillows.
- Weather.
- Race progression.
- Free Cam after finishing.
- Existing obstacles.
- Existing controls.

Weather must continue until all relevant competitors have finished, as previously requested.

---

# 23. TRUCK WIFE CARRYING / WIFE RACING

## 23.1 Apply the Latest Minimal Racing HUD

The mode has been referred to as Wife Carrying and Wife Racing in the notes.

Check the actual project registration before treating these as distinct games.

If they are the same sport, apply the requirements once.

If there are genuinely separate modes, preserve both and apply the appropriate HUD changes.

Remove the enormous information block that currently occupies much of the screen.

During active gameplay show:

- Current running position.
- Race time.
- Directional arrow to the next checkpoint.

Nothing else needs permanent screen space.

## 23.2 BASE Jumping-Style Split-Screen Layout

Use the same compact central split-screen information arrangement as Mattress Racing.

Keep the directional checkpoint arrow appropriate to each player's camera.

Avoid repeating a huge leaderboard in each quadrant.

Preserve the finish, AI racing, checkpoints, weather, and passenger-carrying mechanics.

---

# 24. TRUCK MUSICAL PARKING

## 24.1 Preserve Working Gameplay

The developer likes the current game mechanics.

Do not redesign parking, AI decisions, or rules unnecessarily.

## 24.2 Improve Parking Bay Presentation

Replace default-looking markers and plain text with attractive:

- Parking bay numbers.
- Materials.
- Outlines.
- Status graphics.
- Secured/available indicators.
- Player-color accents.

## 24.3 Remove the Obstructive Top Bar

Remove the unnecessary shared-view top information bar.

Reorganize any genuinely essential information into the compact HUD.

## 24.4 Redesign the Bottom UI

The bottom strip should no longer consume the entire screen width.

Reduce it to appropriately sized elements.

Do not leave raw text in a huge empty black panel.

## 24.5 NEW: Shared-View Leaderboard Must Exist in Split-Screen

The polished shared-view leaderboard currently disappears or is not properly represented in split-screen.

Fix that.

For four-player split-screen, create a compact central leaderboard near the intersection of the four viewports.

Use:

- One truck icon for each competitor.
- Player color.
- Elimination status.

**Gray out a truck icon when that player is eliminated.**

Do not display large repeated player-score panels in every viewport.

For games with more competitors than viewports, use a compact arrangement that shows the actual relevant competitor states without covering gameplay.

Maintain proper naming associations and player identities.

### Acceptance

Shared and split-screen views both show useful, polished competition status without large bars or duplicated information.

---

# 25. TRUCK TARGET LAUNCH

## Use the BASE Jumping Compact Split-Screen HUD Standard

Remove the oversized or repeated HUD panels.

Use a single compact center-oriented scoreboard arrangement.

For each relevant player display:

- Player name.
- Truck icon.
- Current score.

Use the same clean layout behavior as BASE Jumping.

Preserve:

- Volcano.
- Lava explosions.
- Air-control wings.
- Existing launch physics.
- Final resting-position scoring.
- Settling detection.
- Rematches.

Do not revert to first-impact scoring.

---

# 26. TRUCK TUG OF WAR

## DO NOT IMPLEMENT GAMEPLAY CHANGES YET

The developer has identified a design problem:

**Both teams simply hold reverse, which makes the competition boring.**

This mode needs additional mechanics and strategic depth.

However, the explicit instruction is:

**Do not change Tug of War during this implementation pass.**

Instead:

1. Create or retain a design-discussion item in the project notes.
2. Label it as requiring future gameplay brainstorming.
3. In the final development report, remind the developer to brainstorm Tug of War mechanics with ChatGPT.
4. Do not retune controls.
5. Do not add arbitrary minigames.
6. Do not modify rope physics.
7. Do not declare the design problem solved.

This is a deferred design decision, not an implementation task.

---

# 27. TRUCK DEMOLITION DERBY

## 27.1 Repair Clipped Damage Percentages

The HUD currently clips the damage percentages.

Replace the long text labels:

- ENGINE
- BODY

with compact, recognizable icons.

Display the corresponding percentage beside each icon.

For example:

Engine icon + 74%

Body icon + 52%

Use actual existing damage values.

Make the percentages fully visible at supported resolutions and within split-screen layouts.

## 27.2 Remove Redundant Side Text

Remove the large:

"TRUCK SPORTS PARTY DEMOLITION DERBY"

text shown on either side of the screen.

It is unnecessary and visually cluttered.

Do not replace it with equally large permanent banners.

## 27.3 Add Prominent Arena Branding

Place a large, polished Demolition Derby / Truck Sports: Party! logo inside the actual arena.

Use appropriate placement such as:

- Center floor.
- Major stadium wall.
- Large banner.
- Central venue signage.

It should be visible and visually integrated, not obstruct gameplay.

Use a proper logo asset rather than plain default text.

## 27.4 Preserve Existing Derby Features

Do not undo:

- Collision damage settings.
- Five-second stopped/fire rule.
- Outward-facing starting arrangement.
- Reverse-ram gameplay.
- Stadium and spectators.
- Existing AI.
- Damage indicators.
- Arena collision fixes.

### Acceptance

Damage percentages are fully readable, side banners are gone, and the arena has prominent branding.

---

# 28. TRUCK DANCE-OFF

## 28.1 Correct Sport-Specific Logo

Replace the incorrect or generic displayed logo with the dedicated Truck Dance-Off logo.

Use the established game icon/logo assets.

Keep it readable and integrated into the dance venue and UI.

## 28.2 Make Rhythm Inputs More Forgiving

Current button timing is too strict.

Increase the accepted timing window.

Use a configurable, slightly generous timing tolerance.

Suggested approach:

- Preserve PERFECT timing as a narrow window.
- Expand GREAT and GOOD windows.
- Increase allowable late/early tolerance slightly.
- Preserve meaningful MISS conditions.
- Account for ordinary controller and display latency.

Do not display an unnecessarily complicated new timing interface.

This should feel forgiving in a fun party-game way.

Do not give perfect scores for blatantly incorrect input.

## 28.3 Make Trucks Face the Screen

The dancing trucks should be oriented toward the primary gameplay/broadcast camera.

Players should see the front and cab of the trucks.

Do not position them so their backs face the viewer during the main dance performance.

Keep enough room for choreography and collision-safe motion.

## 28.4 Improve Camera Work

Add more interesting cinematic presentation:

- Smooth camera pans.
- Controlled cuts.
- Close-ups.
- Wide shots.
- Side-to-side sweeps.
- Occasional low-angle views.
- Special move emphasis.
- Winner celebrations.

The camera should feel like an energetic concert or dance competition broadcast.

However:

- Never obscure timing prompts.
- Avoid rapid disorienting cuts.
- Maintain truck readability.
- Respect multiplayer HUD layouts.
- Do not interrupt rhythm scoring.

## 28.5 Preserve Cute Truck Animations

Keep:

- Bear Up.
- Bouncy Bounce.
- Happy Nod.
- Shy Sway.
- Butt Wiggle.
- Mini Donuts.
- Tippy-Taps.
- Roof spins.
- Other established moves.

Do not replace the cute choreography with generic truck movement.

### Acceptance

Trucks face the audience, logo is correct, rhythm timing is more forgiving, and cinematic camera behavior adds energy without hurting input readability.

---

# 29. SHARED HUD PRESENTATION SPECIFICATION

Several modes now request the same compact split-screen system.

Implement this as a reusable shared component rather than duplicating UI layouts.

Applicable modes include:

- BASE Jumping.
- Terrier Racing.
- Mattress Racing.
- Wife Carrying / Wife Racing.
- Target Launch.
- Musical Parking, with its icon-based elimination-specific variation.

## 29.1 Four-Player Layout

For a standard 2×2 split-screen:

- Create one shared score/status grouping near the center intersection.
- Place each player's compact component near the interior corner of their own viewport.
- Never display the entire player leaderboard redundantly in every viewport.
- Keep the grouping small.
- Leave surrounding gameplay visible.

## 29.2 Vary Contents by Sport

**BASE Jumping**
- Name.
- Truck icon.
- Score.

**Terrier Racing**
- Relevant racing score/status in the same compact presentation.

**Mattress Racing**
- Position.
- Time.

**Wife Racing**
- Position.
- Time.
- Personal checkpoint arrow elsewhere as appropriate.

**Target Launch**
- Name.
- Truck icon.
- Score.

**Musical Parking**
- Compact participant truck icons.
- Grayed-out eliminated competitors.

Do not create a single generic HUD that displays unnecessary information in every game.

## 29.3 Other Player Counts

Adapt layouts for:

- One human.
- Two humans.
- Three humans.
- Four humans.
- AI camera views where supported.

Use appropriate anchors when no four-way viewport intersection exists.

Respect the Show AI Cameras setting.

## 29.4 UI Visual Quality

Match polished existing Truck Sports games.

Use:

- Proper branded panels.
- Good typography.
- Icons.
- Player colors.
- Suitable contrast.
- Elegant spacing.
- Smooth feedback.
- Correct scaling.

Avoid raw debug-like text.

---

# 30. PERMANENT ACCEPTANCE LEDGER

This section is mandatory.

Use the existing project-side Truck Sports acceptance ledger.

Do not create another contradictory backlog.

## 30.1 Requirement Status

Every individual requirement must be recorded as:

- MISSING.
- PARTIAL.
- IMPLEMENTED BUT UNVERIFIED.
- VERIFIED.
- BLOCKED.

Do not use vague statuses such as "basically done."

## 30.2 Required Evidence

Every record must include:

- Stable requirement ID.
- Sport.
- Exact requirement.
- Relevant scene.
- Scripts/prefabs.
- Implementation notes.
- Test procedure.
- Automated test results.
- Runtime evidence.
- Visual evidence where necessary.
- Current status.
- Remaining work.

## 30.3 Human Testing Overrides Incorrect Completion Claims

If the developer reports a feature is still broken, reopen its acceptance item.

Do not rely on earlier statements that it was completed.

A passing compilation does not prove a visual or gameplay requirement works.

## 30.4 Avoid Repeating Already Completed Work

If a requirement is already correctly implemented in the latest project version:

- Verify it.
- Record evidence.
- Do not rebuild it.
- Continue to the next outstanding requirement.

## 30.5 Preserve Open Work Across Sessions

Do not silently discard requirements.

When work is interrupted, leave exact continuation information.

Do not spend excessive execution time rewriting the ledger instead of implementing fixes.

---

# 31. IMPLEMENTATION PRIORITY

## P0 — Broken Core Functionality

Fix first:

1. Fishing fish passing through the net.
2. Hockey puck entering the goal without scoring.
3. Fare Fight AI ignoring objectives.
4. Kickball home-run state not ending properly.
5. Sky Rings camera instability and unreachable rings.
6. Trailer Plug attachment and size problems.
7. Minefield cumulative distance scoring.

These are actual gameplay failures.

## P1 — Major Environment and Physics Corrections

8. Keepaway 3× playable-area expansion, lower obstacle density, destructible props.
9. Pedestrian Plowing physics and negative-target balance.
10. Field Goal holder alignment and broken audio.
11. Trailer Plug sinkhole size.
12. Fishing river redesign and speed.
13. Terrier Racing wind reduction.
14. Bungee Destruction building replacements and responsiveness.
15. Fare Fight fair starting positions.
16. Snake handling, pickup spawning, and AI camera corrections.

## P2 — HUD and Presentation Overhauls

17. Shared BASE Jumping-style split-screen HUD component.
18. Target Launch HUD.
19. Terrier Racing HUD.
20. Mattress Racing HUD.
21. Wife Racing HUD.
22. Musical Parking HUD.
23. Minefield HUD.
24. Snake HUD.
25. Pedestrian Plowing HUD.
26. Bowling UI-only cleanup.
27. Hockey UI.
28. Air Hockey UI.
29. Billiards UI.
30. Poker UI.
31. Cheese Rolling UI.
32. Demolition Derby UI.
33. Dance-Off UI and camera polish.

## P3 — Additional Visual Improvements

34. Cheese Rolling animated cats and VFX.
35. Dock Diving crowd, pool, water, run-up, and speech-bubble fixes.
36. Bowling plain-text removal.
37. Demolition Derby logo.
38. Field Goal holder celebration.
39. Poker AI reactions.
40. Snake town detail.

## P4 — Organization and Deferred Design

41. Move Log Rolling into Scrap Heap.
42. Move Trampoline Dodgeball into Scrap Heap.
43. Preserve Tug of War for future gameplay brainstorming.
44. Report Cab Chaos design identity and any unresolved distinction.

Prioritize actual implementation over repeated planning.

---

# 32. MANDATORY REAL GAMEPLAY TESTS

For every changed mode, perform relevant testing.

## Technical

- Unity compilation.
- EditMode tests.
- PlayMode tests.
- Scene initialization.
- Prefab validation.
- Physics configuration.
- Correct input ownership.
- Correct player assignments.

## Gameplay

- Human player.
- Human with AI opponents.
- Multiple humans where supported.
- AI-only spectator scenarios where supported.
- Proper scoring.
- Round completion.
- Results.
- Rematch.
- Exit and reentry.

## Visual

Inspect actual output at:

- 1280×720.
- 1920×1080.
- Supported split-screen layouts.

Verify:

- No overlapping HUD.
- No excessive text.
- No broken camera target.
- No missing models.
- No placeholder effects.
- No incorrect asset scaling.
- No visible terrain gaps.
- No unnecessary debug overlays.

## Physics

Test actual gameplay contacts.

Especially:

- Fishing net collisions.
- Hockey goal crossings.
- Pedestrian ragdolls.
- Bungee building destruction.
- Trailer detachment.
- Sky Rings towing.
- Keepaway destructible props.
- Field Goal football collision.
- Minefield edge cases.

## Performance

Check the impact of:

- Larger environments.
- Crowds.
- Water simulation.
- Destruction.
- Larger Keepaway arena.
- Additional physics props.
- Animated characters.
- Split-screen.

Optimize without deleting requested features.

---

# 33. REQUIRED FINAL REPORT

For each sport, report:

### Root Cause

What was broken and why.

### Implementation

Which scripts, prefabs, scenes, and assets were changed.

### Testing

Actual test cases executed.

### Verification

What was genuinely observed.

### Remaining Work

Anything incomplete or unverified.

Also provide:

- Compilation results.
- EditMode results.
- PlayMode results.
- Player-build results where available.
- Known regressions.
- Acceptance ledger summary.
- Screenshots demonstrating important UI changes.
- Evidence of major environment changes.
- Known performance issues.

### Special Final Reminder

Include this explicit reminder in the completion report:

**TUG OF WAR: Brainstorm additional mechanics with the developer and ChatGPT. Current gameplay is too dependent on holding reverse. No changes were authorized during this pass.**

Do not claim Tug of War has been redesigned.

---

# FINAL DIRECTIVE

Truck Sports: Party! already has numerous promising and entertaining modes.

The problem is no longer a lack of ideas.

The priority now is making the existing games reliable, polished, and genuinely enjoyable.

Fishing must catch fish.

Hockey must score goals.

Fare Fight AI must actually pursue passengers.

Kickball must correctly finish home-run plays.

Sky Rings must have smooth cameras and achievable rings.

Trailer Plug must use correctly attached and scaled trailers.

Minefield must calculate cumulative distance properly.

Keepaway must be enormous, more open, and physically destructible.

Bungee Destruction must actually break buildings.

Poker must have a beautiful interface and expressive opponents.

The racing modes must have clean, compact HUDs rather than duplicated text covering every viewport.

Bowling must remain untouched apart from the expressly authorized text and UI cleanup.

Terrier Racing needs less wind interference, not another general movement overhaul.

Use the excellent assets already available.

Protect working systems.

Fix real gameplay defects before polishing decorations.

**Every requirement must be implemented, tested, and recorded accurately in the permanent acceptance ledger.**

Do not substitute another list of supposed accomplishments for demonstrably functioning gameplay.

Make this collection feel like a finished, cohesive, release-quality arcade party game.