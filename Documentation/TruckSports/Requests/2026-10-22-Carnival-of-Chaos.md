# SOL 6.1 HIGH — INTERSTATE: HAULER
# TRUCK SPORTS: PARTY!
# ALPHA 0.6.0 — CARNIVAL OF CHAOS
## COMPLETE MASTER IMPLEMENTATION, GAMEPLAY REPAIR, CUSTOMIZATION & PRESENTATION PASS

**PROPOSED VERSION:** Alpha 0.6.0 — Carnival of Chaos

**IMPORTANT:** Inspect the project's authoritative version metadata before assigning this version number. If a newer Alpha version already exists, advance the actual version appropriately while retaining the Carnival of Chaos release name.

---

# 0. PRIMARY DIRECTIVE

Implement ALL requirements in this document directly within the existing Interstate: Hauler / Truck Sports: Party! Unity project.

These requirements come from several hours of direct human playtesting and represent the developer's current design decisions.

This development pass includes:

- A substantial global Truck Customizer and named profile system.
- Multiple gameplay and physics repairs.
- Scoreboard and split-screen presentation improvements.
- Major environment and asset enhancements.
- New scoring and combo mechanics.
- Improved camera behavior.
- AI improvements.
- Arcade celebration effects.
- Better player identification.
- Global UI cleanup.
- Scrap Heap reorganization.
- Permanent requirements tracking.
- Parallel-agent development.
- Mandatory World of Warcraft-style Alpha patch notes.

**Build the actual features. Do not substitute a development plan for implementation.**

Preserve systems that are already working.

Inspect the latest project state before changing code, because some older feedback may already have been addressed by previous development sessions.

The newest instructions in this document supersede contradictory older requirements.

## 0.1 IMPORTANT — TRUCK KICKBALL SCOPE

Kickball has ONE specific requirement restored to this implementation pass:

**Make the bases and baselines clearly visible during snowy weather by creating shoveled-out areas.**

All other previously discussed Kickball fixes remain assigned to earlier development work and must NOT be duplicated here.

Do not separately reimplement:

- AI-only turn simulation.
- Fast-forward until the next human turn.
- Camera-look controls while waiting to bat.
- Previous home-run resolution fixes.
- Previous broadcast-camera corrections.

Those were already assigned to an earlier prompt.

Inspect their current implementation if shared-system changes could affect them, but do not restart the work.

## 0.2 PROTECT APPROVED WORK

The developer has specifically approved several existing systems.

Do not unnecessarily redesign:

- Truck Hot Potato's shared Arena View HUD.
- The existing four-player BASE Jumping split-screen HUD.
- The existing four-player Mattress Racing split-screen HUD.
- Working truck controls and physics.
- Existing multiplayer input assignments.
- Global participant-name persistence.
- Working scoring systems.
- The established shared-camera standardization.
- Pedestrian Plowing's existing skybox and atmosphere.
- Cab Chaos's existing skybox and atmosphere.
- Previously implemented successful gameplay fixes.

Use targeted changes wherever possible.

---

# 1. MULTI-AGENT IMPLEMENTATION — MANDATORY

## 1.1 Use Parallel Sol/Codex Agents

This is a large task.

Use available subagents to accelerate implementation by working on independent requirements simultaneously.

Use bounded, efficient parallelism within the available execution and rate limits.

Suggested workstreams:

### Lead Integration Agent
- Own overall coordination.
- Inspect project architecture.
- Maintain authoritative shared-system contracts.
- Coordinate merges.
- Control the requirements ledger.
- Review cross-sport compatibility.

### Gameplay Agent A
- Ninja Warrior.
- Avalanche Escape.
- Red Light, Green Light.
- Sky Tow Rings.

### Gameplay Agent B
- Volleyball.
- Hot Potato.
- Tetherball.
- Hungry Hungry Haulers.

### Gameplay Agent C
- Cornhole.
- Billiards.
- Hockey.
- Pedestrian Plowing.

### Presentation/UI Agent
- Global HUD cleanup.
- Split-screen scoreboards.
- NASCAR-style racing leaderboard.
- Team scorebugs.
- Keepaway UI.
- Balloon Battle and Limbo UI.

### Environment/Asset Agent
- Fishing water and wildlife.
- Football stadium.
- Beer Pong tailgate.
- Animated lava.
- Retro Skee-Ball.
- Bird assets.
- Ring VFX.

### Customization/Profile Agent
- Named profiles.
- Truck body selection.
- Hat Shop.
- Faces.
- Mirrors.
- Wheels.
- Truck-ball accessories.
- Dashboard items.
- Persistent cosmetic saving.

### Testing/Validation Agent
- Automated tests.
- Runtime acceptance tests.
- Visual verification.
- Performance checks.
- Cross-sport regression testing.

Adapt these assignments to the actual available subagent capabilities.

## 1.2 Avoid Conflicting Edits

- Assign clear file and scene ownership.
- Do not allow agents to overwrite the same scene simultaneously.
- Establish shared interfaces before parallel implementation.
- Integrate work incrementally.
- Compile regularly.
- Run regression tests after shared-system changes.
- Resolve conflicts without silently discarding completed work.

## 1.3 Maximize Useful Development Throughput

Prioritize meaningful implementation.

Avoid repeatedly auditing the same backlog.

Use the permanent requirements ledger to determine what is missing and what has already been verified.

Spend available execution capacity on productive code, asset integration, gameplay testing, and fixes.

Do not increase agent count merely for appearances if coordination overhead outweighs the benefit.

---

# 2. GLOBAL HUD CLEANUP — ALL ACTIVE SPORTS

## 2.1 Remove Permanent Explanation Strips

Remove persistent, full-width explanatory and instruction banners from active gameplay HUDs throughout Truck Sports: Party!

Examples include:

- Controls explanations.
- Scoring-rule descriptions.
- Point-value legends.
- Repeated objective instructions.
- Debug-style status messages.
- Permanent bottom instruction bars.

The Truck Balloon Battle bar explaining spike controls and scoring is an example of the unwanted presentation.

**Remove the panel and reclaim the screen space.**

Do not simply hide the text while retaining an empty black rectangle.

## 2.2 Preserve Necessary Information

Keep essential:

- Scores.
- Timers.
- Player names.
- Truck icons.
- Objective indicators.
- Current ball/bomb ownership.
- Relevant match progress.
- Short contextual warnings.
- Event-specific notifications.

Move general rules and controls into pre-match instructions or the pause/help menu.

## 2.3 Consistent Presentation Quality

Use the existing polished Truck Sports UI style.

- Compact panels.
- Player-color accents.
- Truck portraits.
- Attractive typography.
- Subtle transparency.
- Clean spacing.
- Readable text.
- Sensible animations.
- Responsive split-screen layouts.

Do not leave raw debug-like text blocks.

---

# 3. GLOBAL NASCAR-STYLE RACING LEADERBOARD

For EVERY racing sport that tracks finishing/running positions, implement a NASCAR-style horizontal running-order leaderboard when a SINGLE full-screen gameplay camera is active.

## Required Behavior

- One horizontal row.
- Competitors ordered left to right by live race position.
- Truck icon.
- Player name.
- Running position.
- Relevant race time where useful.

When positions change, panels should smoothly slide into their new order.

Avoid abrupt UI snapping.

Use authoritative race progression, including checkpoints and valid route distance, rather than unreliable straight-line positioning.

## Four-Player Split-Screen Protection

Do not modify the currently approved four-player BASE Jumping or Mattress Racing HUD layouts.

The new NASCAR leaderboard primarily applies to one full-screen gameplay view.

Create a reusable shared component rather than duplicating the system across every racing sport.

---

# 4. GLOBAL RACING FINISH-LINE PRESENTATION

## 4.1 Checkered Flags

For all sports with actual racing finish lines, reuse or adapt the existing Wife Carrying checkered-flag presentation.

Every applicable race should have an identifiable finish.

## 4.2 Winner Celebration

When the first legitimate competitor finishes:

- Trigger large victory VFX.
- Fireworks.
- Confetti.
- Dramatic lighting.
- Crowd cheering.
- Victory announcement.
- Appropriate cinematic presentation.

Ensure the effect fires once for the correct winner.

## 4.3 Losing Competitor Finish Reactions

When subsequent losing competitors cross:

- Animated pedestrians rush toward the finish area.
- They throw the existing arcade piss-jug projectiles toward the losing trucks.
- Use appropriate impact physics and comedy effects.

Preserve official results.

Post-finish impacts must not change already recorded finishing positions or times.

---

# 5. GLOBAL FREE-FOR-ALL PLAYER COLORS

In free-for-all games, every simultaneous competitor must have a visually distinct truck color.

Human testing revealed multiple players using identical blue and orange trucks in Truck Sumo.

## Requirements

- Detect duplicate or nearly identical preferred colors.
- Assign distinct match colors.
- Support larger 8–12-player fields.
- Use a high-contrast, colorblind-conscious palette.
- Add secondary identification if needed.
- Match all truck icons, HUD colors, nameplates, and results to the actual truck color.
- Preserve color consistency throughout the match.

Do not overwrite persistent profile color preferences merely because a temporary match needs different colors.

Do not impose free-for-all uniqueness rules on sports intentionally using team colors.

---

# 6. GLOBAL LAVA VISUAL OVERHAUL

Every game containing lava should use animated, visually interesting molten surfaces.

Affected games include Truck Target Launch, Terrier Racing, Floor Is Lava, and other registered lava environments.

## Visual Requirements

- Flowing molten currents.
- Slow churning.
- Bubbling.
- Occasional bursts.
- Pulsing emissive glow.
- Shifting orange/red/yellow patterns.
- Heat distortion.
- Smoke.
- Steam.
- Embers.
- Localized splash reactions.

Use existing owned lava shaders, materials, and VFX where appropriate.

When trucks or debris hit lava, trigger convincing molten splashes and eruption effects.

Preserve existing lava collision, scoring, elimination, and explosion rules.

Optimize for split-screen and large environments.

---

# 7. COMPLETE GLOBAL TRUCK CUSTOMIZATION & NAMED PROFILE SYSTEM

**THIS IS A MAJOR FEATURE OF THE UPDATE.**

The developer and Jeff have proposed expanding customization beyond decorative accessories to include completely different truck bodies.

The goal is a playful, expressive, persistent customization system inspired by the convenience of named profiles in Super Smash Bros. Melee.

Every named player should be able to have their own recognizable truck.

---

## 7.1 Named Player Profiles

Allow players to:

1. Create or select a named profile.
2. Open the Truck Customizer.
3. Choose a truck body.
4. Equip cosmetic accessories.
5. Save the profile.
6. Enter any compatible Truck Sports game.
7. Automatically spawn using that profile's customized truck.

Selections must persist:

- Between rounds.
- Between matches.
- Between sports.
- After returning to the main menu.
- After closing and reopening the game.

Use stable unique profile IDs internally.

Do not use editable display names as database keys.

Renaming a profile must not erase its customizations.

Two profiles with identical display names must remain independent.

Reuse the existing global participant registry and save architecture.

---

## 7.2 NEW CATEGORY — TRUCK BODY CUSTOMIZATION

Players must be able to change the truck's actual visible body/model, not just its paint or accessories.

Build a proper body-selection shop.

### Required Body Categories

**Standard Semi-Trucks**
- Existing semi-truck models.
- Appropriate compatible cab variations.

**Monster Trucks**
- Enormous lifted bodies.
- Huge wheels.
- Aggressive oversized appearance.
- Monster-truck-inspired proportions.

**Lowriders**
- Lowered truck bodies.
- Stylized suspension.
- Show-truck appearance.
- Suitable chrome and decorative variations.

**Redneck Lifted Trucks**
- Extremely lifted pickup-style trucks.
- Oversized tires.
- Exaggerated suspension.
- Off-road accessories.
- Comedic country-truck styling.

**Firetrucks**
- Fire-engine body options.
- Emergency vehicle appearance.
- Appropriate lights and body details.

**Novelty Hot-Dog Truck**
- Create an original giant hot-dog-shaped novelty truck inspired by the visual idea of the Oscar Mayer Wienermobile shown in the developer's reference image.
- Use an original design, branding, and materials.
- Do not copy the Oscar Mayer logo or proprietary artwork.
- Make the sausage/hot-dog vehicle recognizable as a ridiculous novelty vehicle.

**Ice Cream Truck**
- Classic ice cream truck body.
- Bright colors.
- Ice cream-themed roof decorations.
- Optional musical/novelty presentation where appropriate.

### Additional Body Types

Search the existing asset collection for other suitable vehicle bodies.

If useful assets already exist, integrate them.

Provide a sensible expandable catalog so additional vehicle styles can be added later.

---

## 7.3 Physics and Compatibility for Different Bodies

The body category must not silently break competitive gameplay.

Different vehicles have different visual dimensions.

Implement a robust compatibility system with:

- Correct model scale.
- Appropriate mounting points.
- Correct cab orientation.
- Supported wheel placements.
- Valid camera positions.
- Correct cockpit references where available.
- Appropriate visual/collision representation.
- Safe accessory attachment offsets.

Use shared sport-appropriate performance balancing.

A monster truck should not automatically become unbeatable because its decorative tires are huge.

A lowrider should not become unusable because its body collides with every bump.

Where meaningful physical size differences are supported, ensure they are deliberate, balanced, and consistent with each sport's rules.

For sports requiring a standardized truck size or special equipment, provide mode-specific compatibility adapters or visually appropriate scaling without corrupting saved profile preferences.

Do not forcibly overwrite the player's chosen body just because a particular sport needs a temporary compatible configuration.

---

## 7.4 Hat Shop

Create a full Hat Shop.

Include a large selection such as:

- Cowboy hats.
- Top hats.
- Crowns.
- Wizard hats.
- Viking helmets.
- Party hats.
- Baseball caps.
- Construction helmets.
- Pirate hats.
- Novelty hats.

Use existing assets where possible.

Hats must attach appropriately to different truck bodies.

They must not block driving cameras or interfere with gameplay physics.

---

## 7.5 Truck Faces

Allow cosmetic facial features on the front of the truck.

Examples:

- Eyes.
- Eyebrows.
- Mouths.
- Smiles.
- Angry expressions.
- Goofy expressions.
- Mustaches.
- Eyelashes.

Support expressive combinations.

Ensure compatibility with different bodies.

Avoid blocking essential windows or first-person cameras.

---

## 7.6 Mirrors

Allow alternate mirror styles.

Examples:

- Standard mirrors.
- Chrome mirrors.
- Oversized novelty mirrors.
- Colored mirrors.
- Decorated mirrors.

Mount them correctly to each body.

Avoid clipping and physics interference.

---

## 7.7 Wheels

Provide cosmetic wheel and rim variations.

Examples:

- Chrome rims.
- Painted rims.
- Custom spokes.
- Decorative hubcaps.
- Monster-truck wheel appearances.
- Lowrider wheel appearances.

Cosmetic selections should not unintentionally alter traction, acceleration, or wheel physics in unrelated sports.

---

## 7.8 NEW CATEGORY — TRUCK BALL COSMETICS

Jeff specifically suggested different truck-ball cosmetics.

Add a playful category for rear-mounted truck-ball ornaments or novelty hitch accessories.

Examples:

- Different colors.
- Metallic finishes.
- Comedic oversized variants.
- Novelty decorative styles.
- Themed hanging accessories.

These should use appropriately positioned decorative attachments.

They may sway or bounce visually when suitable.

They must not interfere with trailer hitches, truck physics, other vehicles, or gameplay objectives.

Allow compatibility rules for modes requiring an unobstructed hitch.

---

## 7.9 Dashboard Accessories

Each profile should be able to equip an in-cab dashboard accessory.

Examples:

- Bobbleheads.
- Fuzzy dice.
- Miniature animals.
- Small figurines.
- Dashboard toys.
- Novelty ornaments.

These must be visible in first-person cockpit views.

Use proper mounting positions.

Keep them stable while driving.

Avoid obstructing the windshield or HUD.

---

## 7.10 Customization Shop Interface

Create a polished customization menu.

Required features:

- Rotating 3D truck preview.
- Selectable body category.
- Browseable cosmetic categories.
- Item thumbnails.
- Live preview.
- Equipped-item indication.
- Save/confirm behavior.
- Controller support.
- Mouse support.
- Clear profile identity.
- Proper Truck Sports: Party! branding.

The Hat Shop should feel like a real feature, not a debug list of prefab names.

Do not invent a required paid currency system if the project does not already have an appropriate cosmetic economy.

---

## 7.11 Persistent Customization Data

Store selections in versioned profile data.

Include:

- Unique participant/profile ID.
- Display name.
- Chosen truck body.
- Hat.
- Face decorations.
- Mirrors.
- Wheels.
- Truck-ball accessory.
- Dashboard accessory.
- Preferred color.
- Compatible vehicle customization metadata.

Support safe defaults for missing or unavailable assets.

Preserve saved customization through future content additions and version upgrades.

---

## 7.12 Named AI Profiles

Where the existing participant registry supports persistent named AI opponents, allow them to use saved customization too.

Their visual appearance should remain recognizable between sports.

Do not confuse a temporary AI match slot with a persistent AI profile.

---

## 7.13 Customizer Acceptance Tests

Verify:

1. Create a named profile.
2. Select a monster truck body.
3. Equip a hat.
4. Equip a face.
5. Change wheels and mirrors.
6. Add truck-ball accessories.
7. Add a dashboard item.
8. Save the configuration.
9. Enter multiple different Truck Sports modes.
10. Confirm the same customized truck appears.
11. Rename the profile.
12. Confirm the truck remains associated with that identity.
13. Close and relaunch the application.
14. Confirm customization persists.
15. Verify different truck bodies don't break cameras or physics.
16. Verify special modes retain correct scale.
17. Verify AI profiles remain distinct.
18. Verify missing assets use safe fallback behavior.

---

# 8. TRUCK BASE JUMPING

## 8.1 Single-Screen Leaderboard

When one full-screen gameplay camera is active, display the complete leaderboard in the bottom-right corner.

Include all participants:

- Name.
- Truck icon.
- Player color.
- Score.
- Correct ranking.

Replace the tiny local-player-only display.

**Do not alter the approved four-player split-screen HUD.**

## 8.2 Sweeping Wind Gusts

Add occasional randomized wind gusts.

Gusts should:

- Sweep across the environment.
- Vary in strength.
- Vary in direction.
- Affect airborne truck motion.
- Create interesting but manageable deviations.

Avoid making ordinary attempts unfairly impossible.

Expose gust settings for tuning.

## 8.3 Fully Animated Birds

Find the developer's existing owned bird assets.

Import/configure them properly.

Add animated birds flying around the course.

Birds should:

- Fly along believable paths.
- Animate their wings.
- Occasionally form small flocks.
- Sometimes cross the player's trajectory.
- Occasionally collide with the airborne truck.

Bird collisions should cause modest course deviations rather than automatic failure.

Preserve scoring and existing driving mechanics.

---

# 9. TRUCK MATTRESS RACING

Use the shared NASCAR-style horizontal leaderboard when one full-screen gameplay view is active.

Display:

- Running position.
- Competitor name.
- Truck icon.
- Relevant race time.

Animate position changes.

**Leave the existing four-player split-screen HUD unchanged.**

Preserve existing mattress physics, loose pillows, weather behavior, and racing mechanics.

---

# 10. TRUCK BILLIARDS

Add subtle, physically believable variance to the INITIAL BREAK SHOT ONLY.

Repeated clean head-on breaks currently produce overly symmetrical results, particularly among corner balls.

Introduce tiny, configurable variations through:

- Initial rack placement.
- Natural rack imperfections.
- Small legitimate contact differences.

Do not apply arbitrary forces after impact.

Do not alter normal shots after the break.

Preserve existing scoring and controls.

Test repeated head-on breaks to confirm varied but believable ball scatter.

---

# 11. TRUCK HOCKEY

Improve goal attribution.

The individual goal scorer should be the **last truck belonging to the scoring team to touch the puck**.

If an opposing defender accidentally puts the puck into their own net:

- Award the team point correctly.
- Announce **OWN GOAL**.
- Credit the last scoring-team truck that touched the puck, if eligible.

If no scoring-team participant touched the puck during the relevant play, award a team goal without inventing a player scorer.

Track puck contacts using stable participant IDs.

Avoid duplicate goal events.

Reset touch history between plays.

Preserve current goal detection and scoring logic.

---

# 12. TRUCK CORNHOLE

## 12.1 Simultaneous Launches

All human and AI trucks should launch simultaneously.

Every participant remains visible.

## 12.2 Midair Noncollision

During the initial launch and airborne travel:

- Trucks are visible.
- Truck-to-truck collisions are disabled.

Once trucks reach the opposing board:

- Enable appropriate truck-to-truck collision.
- Allow physical interactions and pileups.

Prevent unstable collision activation when vehicles overlap.

## 12.3 Preserve Landed Trucks

After a truck lands or finishes its attempt, leave it physically visible in its final position until the entire round ends.

Do not prematurely despawn or reset it.

## 12.4 Complete Art Pass

Build a significantly more attractive Cornhole venue.

Replace placeholder-looking surfaces and scenery.

Add appropriate environmental art, signage, spectators, materials, and lighting.

## 12.5 Giant Human Spectators

The trucks are intended to be approximately beanbag-sized.

NPC pedestrians should remain normal human scale.

They should therefore appear enormous compared with the miniature trucks.

Use properly animated pedestrian models.

Include walking, idling, cheering, and reaction behavior.

## 12.6 HUD Overhaul

Match the polished UI of the newer Truck Sports games.

Remove oversized debug panels and permanent explanations.

Preserve:

- Clean hole scoring.
- Wind/weather effects.
- Bottom-right wind indicator.
- The established 10 MPH cap on direct headwind.
- Actual physics-based scoring.

---

# 13. TRUCK KEEPAWAY

## 13.1 Complete HUD Redesign

Remove giant black HUD panels and ugly rectangular world-space player nameplates.

Use compact, polished truck icons for player identification.

## 13.2 One Full-Screen View

Display one dedicated horizontal scoreboard across the bottom.

Include:

- Names.
- Truck icons.
- Possession totals.
- Current ball carrier.
- Relevant protection status.

## 13.3 Four-Player Split-Screen

Place one compact shared scoreboard near the center junction of the four viewports, similar to Musical Parking.

Do not duplicate enormous scoreboards in every quadrant.

## 13.4 Shared Arena Composition

Correct any large unused gray/blank regions outside the playfield.

Keep the action properly framed and readable.

Maintain the previously established Keepaway camera viewing-distance standard.

Do not zoom excessively outward merely to show the entire arena.

Preserve ball mechanics, AI, sheep, cameras, and first-person offscreen ball indicators.

---

# 14. SCRAP HEAP

Move these sports into the existing Scrap Heap section:

- Truck Curling.
- Giant Swing.
- Log Rolling.
- Trampoline Dodgeball.

Preserve all scenes, scripts, prefabs, assets, and progress.

Do not delete or redesign them.

Keep them accessible for future development.

---

# 15. TRUCK BEER PONG

## 15.1 Repair Replay Cameras

The first replay camera currently begins underneath the table.

Fix camera placement and replay initialization.

Provide attractive views of the shot, trajectory, cup interaction, and result.

Never start inside or below table geometry.

## 15.2 Football Tailgate Environment

Build a lively football tailgate scene.

Include:

- Huge football stadium in the background.
- Parked cars.
- Pickup trucks.
- Semi-trucks.
- Tailgate props.
- Decorations.
- Party areas.
- Atmospheric audio.
- Numerous animated spectators.

## 15.3 Crowd Reactions

Spectators should:

- Watch the match.
- Animate naturally.
- Cheer successful shots.
- React to missed shots.
- Throw existing arcade piss-jug projectiles toward unsuccessful players.

Preserve Beer Pong rules, scoring, and turn progression.

---

# 16. TRUCK FIELD GOAL

## 16.1 Bowl-Shaped Stadium

Replace straight side stands extending far behind the scoreboard with an actual stadium bowl wrapping around the back of the end zone.

Add a second seating deck.

Leave a deliberate opening for the giant scoreboard.

Preserve scoreboard visibility and stadium sightlines.

## 16.2 Scoreboard Instant Replay

After every kick attempt, show a replay on the giant in-world stadium scoreboard.

Capture:

- Truck run-up.
- Contact with the football.
- Ball flight.
- Successful or unsuccessful result.

Use an appropriate replay capture/playback or RenderTexture system.

Do not rerun actual gameplay physics to generate a replay.

Preserve authoritative scoring.

Do not duplicate earlier Field Goal audio/holder repairs already assigned to previous development work unless verification reveals they remain broken.

---

# 17. TRUCK FISHING

## 17.1 Raise Water Level

Fish currently appear to float above the water.

Raise the water surface and correct fish swimming depth so fish visibly swim THROUGH the water.

## 17.2 Water Interaction

Add responsive water effects:

- Ripples.
- Wakes.
- Small splashes.
- Surface disturbance.
- Water separation.
- Foam where appropriate.

Effects should respond to fish speed and depth.

Use owned water assets where possible.

## 17.3 Animated Bear

Find the developer's existing owned bear model.

Import/configure it and use appropriate animation clips.

The bear should occasionally:

- Walk along the river.
- Idle.
- Turn.
- Wander.
- Enter the water.
- Wade or swim across.
- Create ripples and splashes.

Do not use a motionless sliding bear.

If the actual owned asset is unavailable, report that limitation rather than claiming it was found.

## 17.4 Other Wildlife

Add occasional appropriate wildlife using existing assets:

- Birds.
- Deer.
- Rabbits.
- Other suitable animals.

Make appearances intermittent and natural.

## 17.5 Preserve Fishing Mechanics

Do not regress:

- Fish catching.
- Net collisions.
- Scoring.
- Larger fish.
- Faster fish.
- Speed fluctuations.
- Winding river.
- Offscreen fish spawning.

---

# 18. TRUCK SKEE-BALL

## 18.1 Retro Arcade Makeover

Use the developer's vintage Skee-Ball reference image.

Create a worn 1980s/1990s arcade-machine aesthetic.

Include:

- Aged wood.
- Scratched metal.
- Worn paint.
- Faded graphics.
- Vintage signage.
- Arcade lighting.
- Proper recessed scoring geometry.

## 18.2 Circular Catch Basins

Replace the simplistic flat target surface with proper recessed circular scoring rings and catch areas.

## 18.3 Point Labels

Clearly display the actual value of every scoring target.

## 18.4 Zero-Point Catch Zone

If the truck misses the scoring holes, it should settle in an appropriate zero-point catch area rather than disappearing into empty space.

## 18.5 Physical Safety Net

Add a surrounding net that catches missed trucks.

Use correct physical collision.

## 18.6 Retro Pixelated Shader

Add a mode-specific retro low-resolution/pixelated presentation effect.

Keep the game readable.

Do not apply the shader globally to unrelated sports.

---

# 19. PEDESTRIAN PLOWING

## 19.1 Exact Spawn Weight Changes

Apply relative adjustments:

**-100 targets:** New weight = previous weight × 0.30.

**+10 targets:** New weight = previous weight × 1.20.

**+100 targets:** New weight = previous weight × 1.20.

Normalize spawn weights afterward.

Preserve the established prefab arrays for each scoring category.

## 19.2 Escalating Combo System

Create an arcade combo mechanic inspired by the Professor Genki-style diversion pacing of Saints Row: The Third.

When an eligible target is hit:

- Start/refill a combo timer.
- Display a shrinking combo bar.
- Increase hit streak.
- Increase the percentage scoring bonus.
- Refill the bar on continued successful hits.
- End the streak when time expires.

Suggested tunable values:

- Hit 1: 0% bonus.
- Hit 2: +10%.
- Hit 3: +20%.
- Hit 4: +30%.
- Hit 5: +40%.

Continue progression with an appropriate configurable maximum.

The bonus increases by percentage, not a single fixed multiplier.

Negative targets retain their penalties and cannot be exploited for bonus points.

## 19.3 Preserve Existing Physics and Indicators

Keep:

- Radiant -100 target aura.
- Correct spawn-category prefabs.
- Existing Truck Taxi-style pedestrian collision/ragdoll behavior.
- Authoritative scoring.

---

# 20. PEDESTRIAN PLOWING & CAB CHAOS HUD / SKYBOX EXCEPTIONS

## 20.1 Preserve Their Existing Skyboxes

Both games already have excellent atmosphere and visual tone.

**Do not replace their skyboxes or lighting with the generic global skybox pass.**

Preserve their established mood.

## 20.2 Minimal HUD

Display only:

- Player name.
- Truck icon/color.
- Current score.
- Remaining target count.
- One shared central timer.

Remove:

- Giant repeated player panels.
- Point-value legends.
- Repeated timers.
- Permanent instructions.

## 20.3 One Full-Screen View

Show the entire horizontal leaderboard along the bottom.

Include all relevant human and AI competitors.

Use one centered match timer.

## 20.4 Four-Player Split-Screen

Use compact shared UI near the center viewport junction.

Do not duplicate massive black HUD panels in every quadrant.

Preserve gameplay and scoring.

---

# 21. TRUCK HOT POTATO

## 21.1 More Points for Bomb Passing

Increase successful bomb-transfer scoring.

Suggested value:

**+3 points per valid transfer.**

Make this tunable.

Prevent farming through continuous overlapping contact.

Preserve:
- Any legitimate truck contact transfer.
- Bomb ownership.
- Carrier speed boost.
- Explosion cycles.
- Survival scoring.

## 21.2 Split-Screen HUD Modernization

Improve all split-screen layouts.

Display compact:
- Player icons/names.
- Scores.
- Bomb carrier.
- Countdown.
- Elimination status.

Remove duplicated oversized panels and explanation text.

**The shared Arena View HUD is already excellent. Do not redesign it.**

## 21.3 Shared Arena Camera Human Visibility

All active human trucks must remain visible in shared Arena View.

Do not zoom so tightly onto the bomb that human players disappear offscreen.

Framing priority:

1. All active human players.
2. Bomb and carrier.
3. Relevant nearby AI.

Distant AI may leave the frame.

Stop requiring eliminated humans to remain visible.

Use smooth framing.

This is an intentional exception to the global Keepaway camera-distance standard when necessary.

---

# 22. TRUCK BALLOON BATTLE

Move the full eight-player leaderboard to the VERY BOTTOM of the screen.

Use ONE horizontal row.

Each participant should have compact:

- Truck icon.
- Name.
- Score.
- Remaining balloons.
- Spike status where practical.

Do not wrap into two rows.

Avoid covering the arena or game logo.

Remove the full-width bottom instruction banner.

Preserve gameplay and balloon physics.

---

# 23. TRUCK LIMBO

Perform the same major UI cleanup as other sports.

Remove:
- Permanent explanatory text.
- Oversized bars.
- Duplicate status panels.
- Raw debug-style UI.

Show only relevant:
- Player icons/names.
- Scores.
- Current round.
- Current bar height.
- Clearance status.
- Short success/failure feedback.

Preserve truck suspension, leaning, body animation, and gameplay.

---

# 24. TRUCK SKY TOW RINGS

## 24.1 Forgiving Ring Passes

A ring counts when any valid portion of the truck's body passes through the ring opening.

Do not require the truck's exact center point to pass through the center.

Use reliable geometry-based crossing detection.

Touching the outside ring frame without entering the opening must not count.

## 24.2 Animated Rings

Make rings significantly more interesting visually.

Use:

- Animated materials.
- Glowing effects.
- Orbiting particles.
- Pulsating circular energy.
- Particle trails.
- Strong successful-pass VFX.

A largely particle-constructed ring is acceptable if its shape remains readable and scoring detection works.

Preserve the existing Arctic spline environment and any successful camera/towing fixes.

---

# 25. TRUCK NINJA WARRIOR

## 25.1 Visible Water

Add actual visible animated water beneath the obstacle course.

Trucks that fall should visibly splash into water rather than empty space.

## 25.2 Stadium Crowds

Use the established Field Goal crowd system for a fuller, more lively stadium.

## 25.3 Starting Presentation

Add:

- Cheese Rolling-style 3–2–1–GO countdown.
- Proper start line.
- Two giant flaming entrance pillars.
- Correct initial truck positioning.

## 25.4 Falling Restarts the Entire Course

**LATEST AUTHORITATIVE RULE:**

If a truck falls or fails:

- End the current attempt.
- Restart at the beginning of the course.
- Reset timer.
- Reset obstacle states.
- Replay countdown.

**NO +5-SECOND TIME PENALTY.**

Do not respawn at intermediate checkpoints.

## 25.5 Hold-to-Charge Jump

**Do not implement a separate acceleration BOOST button.**

Use the existing Jump input.

- Tap: Short jump.
- Hold: Charge jump strength.
- Release: Execute higher/farther jump.
- Suspension visibly compresses while charging.
- Show a compact charge indicator.
- Cap power appropriately.

A fully charged jump should help clear early platforms even with limited forward momentum.

## 25.6 Mattress Surfing-Style Aftertouch

Reuse existing working airborne controls from Mattress Surfing/Mattress Racing.

While airborne allow:
- Pitch control.
- Roll control.
- Limited yaw.
- Landing alignment.
- Controlled balancing.

The intended sequence is:

**Hold Jump → Charge → Release → Fly → Tilt → Land.**

Preserve appropriate vehicle physics and AI participation.

---

# 26. HUNGRY HUNGRY HAULERS

## 26.1 Ball-Only Collection Holes

Replace each player's existing deposit destination with a recessed scoring hole.

Balls can physically enter.

Trucks must be unaffected by the hole.

Use appropriate ball-specific collision layers and collection triggers.

## 26.2 Manual Physical Dumping

The player must drive to their colored scoring zone, align the truck, and press ACTION.

The truck should:

1. Open its storage/tailgate/hopper.
2. Dump collected balls.
3. Allow the balls to fall and bounce physically.
4. Score only balls actually entering the hole.

Balls that miss must not magically score.

Positioning and alignment must matter.

AI must use the same physical unloading rules.

## 26.3 UI Overhaul

Use polished:
- Player names.
- Truck icons.
- Scores.
- Balls in storage.
- Clear scoring feedback.

Remove permanent instruction strips, duplicate panels, and clutter.

---

# 27. TRUCK RED LIGHT, GREEN LIGHT

## 27.1 Ground-Level Camera

Place the shared gameplay camera very low, just above the ground.

Angle it toward:

- The distant finish.
- Giant game master.
- All competing trucks.

Keep the scene dramatic but readable.

## 27.2 In-World Warning System

Remove the debug-like bottom text warning.

Instead:

1. Giant game master laughs.
2. Players hear and see the warning.
3. Approximately one second later the light turns RED.

**NO YELLOW LIGHT.**

Use enormous physical traffic lamps and recognizable in-world visual/audio transitions.

## 27.3 Fair Braking

**During RED, a truck is SAFE when its driver actively applies the brake without accelerating.**

Residual sliding from snow, ice, or inertia must not cause elimination.

Do not punish involuntary movement or harmless physics jitter.

Intentional acceleration during RED causes a violation.

Prevent brake+throttle exploits.

## 27.4 Massive Explosions

Preserve enormous destruction effects for genuine red-light violations.

Do not weaken them.

---

# 28. TRUCK TETHERBALL

## 28.1 Faster Ball

Increase ball swing speed and impact responsiveness significantly.

## 28.2 Real Rope Wrapping

Use the existing rope asset.

As the ball circles the pole:

- The rope physically and visibly winds around it.
- Wrapped rope length increases.
- Free rope length decreases.
- Ball orbit responds to shortening tether length.

Opposite rotation must correctly unwind the rope.

No decorative-only fake wrapping.

## 28.3 Double Required Wraps

Require at least twice the current number of completed wraps before awarding a point.

Track correct directional winding.

## 28.4 Proper Ball and Sound

Replace the plain orange ball with a traditional tetherball-style model/material.

Use appropriate rubber-ball impact sounds from owned assets or create a suitable original sound.

Avoid clipping and repetitive audio spam.

---

# 29. TRUCK VOLLEYBALL

## 29.1 Faster Movement

Significantly increase mode-specific:

- Acceleration.
- Travel speed.
- Turning responsiveness.
- Maneuverability.

Do not change vehicle handling globally.

## 29.2 Jump Height

Increase jump height by approximately **15%**, not dramatically more.

This is the latest authoritative tuning.

## 29.3 Jump-to-Recover When Flipped

If a truck lands on its side, pressing Jump should:

- Apply a controlled upward impulse.
- Correct its rotation.
- Help flip it upright.
- Restore driving.

No automatic teleport/reset.

Support AI recovery.

## 29.4 CBS-Style Scorebug

Remove hits/touches counters from the active gameplay HUD.

Use a compact early-2000s CBS NFL-inspired broadcast scorebug.

Show only:

- Team name/identity.
- Team color.
- Actual points.

Support shared and split-screen presentations.

No huge duplicated HUD boxes.

---

# 30. TRUCK AVALANCHE ESCAPE

## 30.1 HUD Cleanup

Remove permanent instructions, debug panels, and clutter.

Use the NASCAR-style running-order display in one full-screen view.

Show only relevant position, timing, and avalanche danger.

## 30.2 Never Automatically Reset to Start

The current system sometimes teleports stuck trucks back to the beginning behind the avalanche.

That is unacceptable.

Never automatically reset a truck to the start during an active run.

## 30.3 Action Button Recovery Jump

Action should trigger a physical in-place hop.

Allow recovery from being stuck or overturned.

Preserve current course position and race progress.

No teleportation.

Use sensible force and cooldown.

AI should recover fairly.

## 30.4 Avalanche 20% Faster

Increase ACTUAL avalanche front/chase speed by 20%.

Suggested setting:

`AvalancheSpeedMultiplier = 1.20`

Synchronize:
- Snow front movement.
- Physical pushing.
- Catching.
- Burial.
- Snow particles.
- Debris.
- Audio.

Do not merely speed up decorative snow particles.

Preserve an achievable race.

---

# 31. TRUCK KICKBALL — SNOW VISIBILITY FIX ONLY

**THIS IS THE ONLY KICKBALL-SPECIFIC REQUIREMENT AUTHORIZED FOR THIS MASTER PROMPT.**

## 31.1 Shoveled Bases and Baselines

When snow is active, the baseball/kickball bases become nearly invisible.

Fix the presentation.

Create visibly shoveled-out areas around:

- Home plate.
- First base.
- Second base.
- Third base.

Clear snow from the actual base surfaces.

Add contrasting cleared borders around the bases.

Expose dirt or turf beneath the snow.

Use natural snowbank edges rather than artificial glowing rectangles.

## 31.2 Cleared Baselines/Basepaths

Provide visible cleared or partially shoveled paths connecting bases where appropriate.

Maintain readable gameplay during:

- Snow.
- Heavy snow.
- Blizzards.
- Daytime.
- Nighttime.

Ensure snow accumulation does not immediately cover these critical markings during a match.

## 31.3 Preserve Actual Gameplay

Do not change:
- Base positions.
- Trigger volumes.
- Baserunning.
- Scoring.
- Ball physics.
- Player input.
- Match rules.

Do not duplicate the earlier Kickball fast-forward or camera work.

### Acceptance

In a real running snowy match, all bases and relevant basepaths are clearly recognizable from both driving and broadcast perspectives.

---

# 32. PERMANENT ACCEPTANCE LEDGER

Use the existing project-side requirements ledger.

Do not create a competing backlog.

Each requirement must have one of these statuses:

- MISSING.
- PARTIAL.
- IMPLEMENTED BUT UNVERIFIED.
- VERIFIED.
- BLOCKED.

Record:

- Stable requirement ID.
- Game/system.
- Exact requirement.
- Files changed.
- Scenes/prefabs.
- Test procedure.
- Automated evidence.
- Runtime evidence.
- Screenshots where relevant.
- Human-playtest findings.
- Remaining work.

Do not mark a task VERIFIED merely because a script exists or a build succeeds.

A visual feature needs visual inspection.

A physics feature needs real simulated interactions.

An AI feature needs observed behavior.

A persistence feature needs save/reload testing.

When a previous implementation already satisfies the requirement, verify and record it rather than rebuilding it.

Carry incomplete requirements forward.

---

# 33. REQUIRED TESTING

## Automated

Run relevant:
- Unity compilation.
- EditMode tests.
- PlayMode tests.
- Scoring tests.
- Physics tests.
- Input tests.
- Profile persistence tests.
- AI behavior tests.

## Runtime

Verify modified sports through actual gameplay where the execution environment allows.

Test:

- One human.
- Mixed humans/AI.
- Relevant split-screen layouts.
- AI-only spectator matches.
- Scoring.
- Match completion.
- Rematches.
- Scene exit/reentry.
- Weather.
- Customization persistence.
- Camera ownership.
- Gamepad/keyboard controls.

Inspect UI at 1280×720 and 1920×1080.

Check performance after large effects, crowds, shaders, and asset additions.

If live gameplay testing is unavailable, keep appropriate items IMPLEMENTED BUT UNVERIFIED.

Never fabricate observations.

---

# 34. IMPLEMENTATION PRIORITIES

## P0 — Core Gameplay/Fairness

1. Ninja Warrior charged jumps and airborne balance.
2. Avalanche recovery and actual 20% faster snow front.
3. Red Light, Green Light fair braking.
4. Cornhole simultaneous launches and collision handling.
5. Tetherball physical rope winding.
6. Hungry Hungry Haulers physical dumping.
7. Volleyball mobility and recovery.
8. Hockey goal attribution.
9. Billiards break variation.
10. Kickball snow-cleared bases.

## P1 — Shared Systems

11. Named profile/customization architecture.
12. Full body shop and accessory systems.
13. Global free-for-all color uniqueness.
14. Universal HUD explanation-strip removal.
15. NASCAR racing leaderboards.
16. Racing finish-line celebrations.
17. Shared split-screen UI improvements.

## P2 — Art and Environment

18. Fishing responsive water and animated wildlife.
19. Field Goal stadium bowl and scoreboard replay.
20. Beer Pong tailgate environment.
21. Skee-Ball retro arcade overhaul.
22. Dynamic lava across all relevant sports.
23. Sky Tow Rings animated particle effects.
24. BASE Jumping birds and gusts.
25. Keepaway HUD/framing improvements.

## P3 — Integration and Polish

26. Balloon Battle HUD.
27. Limbo HUD.
28. Hot Potato split-screen and shared-camera framing.
29. Pedestrian Plowing combo and target balance.
30. Cab Chaos and Pedestrian Plowing HUD cleanup.
31. Scrap Heap reorganization.
32. Customization compatibility checks.
33. Full regression testing.
34. Acceptance ledger reconciliation.

Use parallel agents and efficient task sequencing.

---

# 35. REQUIRED FINAL ENGINEERING REPORT

Produce a technical implementation report with:

### Implementation
- Features completed.
- Scripts changed.
- Scenes modified.
- New assets integrated.
- Shared architecture changes.

### Tests
- Compilation.
- EditMode.
- PlayMode.
- Runtime.
- Visual verification.
- AI verification.
- Multiplayer.
- Performance.

### Ledger
- Verified.
- Implemented but unverified.
- Partial.
- Missing.
- Blocked.

### Outstanding Work
- Remaining problems.
- Known bugs.
- Unverified features.
- Technical risks.

Do not falsely claim full completion.

---

# 36. PERMANENT WORLD OF WARCRAFT-STYLE ALPHA PATCH NOTES REQUIREMENT

**THIS IS A PERMANENT REQUIREMENT FOR THIS AND ALL FUTURE SUBSTANTIAL MASTER IMPLEMENTATION PROMPTS.**

At the end of development, produce a separate PLAYER-FACING changelog in the style of classic World of Warcraft patch notes.

Use the project's authoritative Alpha version number.

Suggested format:

# TRUCK SPORTS: PARTY!
## ALPHA 0.6.0 — CARNIVAL OF CHAOS

### General
- Added ...
- Improved ...
- Fixed ...

### Truck Customization
- Added swappable truck bodies.
- Added a full Hat Shop.
- Added persistent named truck profiles.

### Truck Ninja Warrior
- Jump can now be charged by holding the Jump button.
- Added airborne tilt controls.
- Falling restarts the entire course without an additional time penalty.

### Truck Volleyball
- Increased acceleration.
- Increased jump height by 15%.
- Added a compact team scoreboard.

Continue for the other modified sports.

## Rules for Patch Notes

1. Use concise, player-friendly bullet points.
2. Group changes by sport or system.
3. Explain actual changes rather than technical implementation details.
4. Do not include unimplemented features as if shipped.
5. Distinguish verified changes from incomplete or unverified work.
6. Use a suitable Alpha version and release title.
7. Keep patch notes separate from the technical report.
8. Avoid vague entries such as "various improvements."
9. Carry this requirement forward into future substantial development prompts.
10. Produce a readable old-school WoW-style changelog without requiring the developer to ask again.

---

# FINAL DIRECTIVE

Truck Sports: Party! already contains an enormous collection of ridiculous physics-driven arcade games.

This update is about making them more polished, more expressive, more customizable, and more enjoyable.

Give players completely personalized trucks.

Let them choose monster trucks, lowriders, lifted trucks, firetrucks, ice cream trucks, and absurd hot-dog-shaped novelty vehicles.

Let them wear ridiculous hats.

Give trucks faces, custom mirrors, wheels, truck-ball decorations, and dashboard accessories.

Save those appearances to persistent named profiles.

Make racing leaderboards feel like televised motorsports.

Make every finish line a spectacular event.

Make lava move and bubble.

Bring animated wildlife to Fishing.

Turn Skee-Ball into an old arcade machine.

Make Cornhole a simultaneous miniature-truck competition.

Make Hot Potato reward risky passing.

Make Ninja Warrior's jumps powerful and controllable.

Make Tetherball genuinely wind its rope around the pole.

Make Hungry Hungry Haulers demand careful physics-based unloading.

Make Volleyball fast and responsive.

Make Red Light, Green Light fair and dramatic.

Make Avalanche Escape intense without resetting trucks unfairly.

Make snowy Kickball bases clearly visible.

Remove unnecessary permanent instructional text throughout the entire collection.

**Use parallel agents intelligently. Reuse existing owned assets. Preserve successful gameplay. Verify actual functionality. Maintain the permanent ledger.**

Finish with BOTH:

1. A complete engineering and verification report.
2. A classic World of Warcraft-style player-facing Alpha patch-notes changelog.

**Build the update. Test the update. Document the update. Make Truck Sports: Party! exceptional.**