# SOL 6.1 HIGH — INTERSTATE: HAULER
# TRUCK SPORTS: PARTY!
## GLOBAL SKYBOX, LIGHTING, TIME-OF-DAY & SCORING PRESENTATION OVERHAUL

### Priority: Full Collection Visual Polish
### Execute after the current gameplay repair pass and before the code-quality stabilization pass.

---

# 0. MISSION

Implement a major visual and presentation improvement across **every applicable Truck Sports: Party! game**.

This pass has three primary objectives:

1. Replace basic or unattractive skyboxes with beautiful, atmosphere-rich skies that respond to weather.
2. Introduce selectable times of day with appropriate lighting, shadows, ambient effects, and environmental illumination.
3. Overhaul scoring feedback and match results so every meaningful scoring event has an attractive animation highlighting the participant who earned it.

**Implement these systems directly in Unity. Do not deliver only a design document.**

The result should make Truck Sports: Party! feel significantly more polished, cinematic, cohesive, and exciting.

Reuse existing project assets and shared systems wherever practical.

Preserve working gameplay, physics, cameras, multiplayer, weather behavior, and scoring.

---

# PART 1 — COMPLETE SKYBOX OVERHAUL

## 1. Create Beautiful Weather-Responsive Skies

The current skybox presentation across the collection needs a major aesthetic improvement.

Inspect the project's existing:

- Skybox assets.
- HDRI textures.
- Procedural sky shaders.
- Cloud materials.
- Atmospheric effects.
- Weather systems.
- Environmental lighting.
- Fog.
- Post-processing.
- Render pipeline settings.

**Prioritize the high-quality sky and weather assets already available in the project.**

Do not default to a basic blue gradient or generic flat sky if suitable production-quality assets exist.

## 2. Different Weather Must Produce Different Skies

Use the existing Truck Sports weather presets.

### Clear Weather

Create a beautiful, vibrant sky featuring:

- Rich blue atmospheric gradients.
- Natural clouds.
- Attractive sunlight.
- Clear horizon.
- Good environmental visibility.
- Convincing distance haze.

Avoid a featureless blue background.

### Cloudy / Overcast

- Large layered cloud formations.
- Soft sunlight.
- Muted sky colors.
- Diffused shadows.
- Convincing atmospheric depth.

### Rain

- Thick rain clouds.
- Darker sky.
- Reduced sunlight.
- Wetter-looking lighting.
- Environmental haze.
- Appropriate cloud movement.

### Heavy Rain / Thunderstorms

- Massive storm clouds.
- Dramatic darkened skies.
- Dynamic lightning.
- Lightning flashes illuminating the environment.
- Stronger atmospheric effects.
- Appropriate thunder synchronization.

Lightning should produce visible temporary illumination rather than simply playing a white particle effect.

### Snow

- Wintry sky.
- Soft cloud cover.
- Cooler environmental lighting.
- Appropriate atmospheric fog.
- Convincing snow visibility.

### Heavy Snow / Blizzard

- Dense cloud coverage.
- Reduced horizon visibility.
- Moving snow.
- Strong atmospheric depth.
- Appropriate cold lighting.
- Distant terrain obscured naturally.

Snow should not turn the entire arena into an overexposed white void.

### Hurricane / Extreme Storm

- Dramatic rapidly moving clouds.
- Heavy atmospheric effects.
- Darkened sky.
- Strong wind presentation.
- Rain and storm effects where appropriate.
- Occasional lightning if supported.

The sky should visually communicate the severity of the weather.

## 3. Support Every Existing Weather State

Inspect the authoritative weather registry.

Do not create a new disconnected weather system.

Use the actual available presets, with sensible visual mappings.

All applicable outdoor sports should respond consistently.

Indoor sports should preserve their intended indoor lighting and only expose appropriate exterior weather effects through windows, entrances, or other visible openings.

---

# PART 2 — TIME-OF-DAY SYSTEM

## 4. Add Time-of-Day Selection

Add a shared pre-match setting alongside Weather:

**TIME OF DAY**

- Random
- Morning
- Day
- Evening
- Night

The setting must work independently of weather.

Examples:

- Clear Morning
- Rainy Evening
- Snowy Night
- Blizzard Morning
- Hurricane Day
- Clear Night

Allow all valid weather and time combinations without creating physically impossible lighting artifacts.

Use the established Truck Sports match-setup UI framework.

## 5. Morning

Morning should feature:

- Low sunlight.
- Beautiful warm golden illumination.
- Long shadows.
- Slight atmospheric haze.
- Soft environmental color.
- Attractive sky gradients.

Consider subtle mist in appropriate outdoor environments.

## 6. Day

Day should feature:

- Bright natural sunlight.
- Clear environmental visibility.
- Proper shadows.
- Vibrant materials.
- Balanced exposure.
- Convincing sky and terrain illumination.

Avoid washed-out environments.

## 7. Evening

Evening should be especially attractive.

Use:

- Gorgeous orange, pink, purple, and red sunset gradients.
- Low-angle sunlight.
- Long dramatic shadows.
- Warm atmospheric haze.
- Beautiful cloud illumination.
- Appropriate reflected light.

Allow the environment to feel cinematic and colorful.

Buildings, mountains, trucks, and stadium structures should catch the warm evening light.

## 8. Night

Night must look genuinely different from daytime.

Create:

- Dark blue nighttime skies.
- Moonlight.
- Stars when cloud conditions permit.
- Beautiful moon illumination.
- Appropriate ambient lighting.
- Stadium floodlights.
- Streetlights.
- Building lights.
- Vehicle headlights.
- Illuminated signs.
- Visible glowing effects.

Clear nights should have attractive stars.

Stormy nights should feature dark clouds and dramatic lightning.

Snowy nights should have cold lighting and atmospheric snow effects.

## 9. Weather and Time Must Work Together

Do not implement weather and time of day as unrelated skybox swaps.

They must jointly control:

- Sky appearance.
- Sun direction.
- Moon visibility.
- Main directional light.
- Light color.
- Ambient illumination.
- Shadow intensity.
- Fog.
- Clouds.
- Atmospheric color.
- Reflection environment.
- Relevant post-processing.
- Artificial lights.

Examples:

**Clear Evening:** Warm orange sunset and long shadows.

**Thunderstorm Evening:** Dark storm clouds with residual warm light near the horizon.

**Clear Night:** Moonlight and stars.

**Blizzard Night:** Dark cloud coverage, blowing snow, reduced visibility, and atmospheric artificial lighting.

**Rainy Morning:** Soft early sunlight diffused through rain clouds.

The visual result should feel coherent.

---

# PART 3 — LIGHTING AND ATMOSPHERE POLISH

## 10. Improve Lighting Across Every Applicable Sport

Audit outdoor sports for:

- Flat lighting.
- Overexposure.
- Underexposure.
- Poor shadows.
- Incorrect ambient colors.
- Missing artificial lights.
- Poor material reflections.
- Unattractive fog.
- Incorrect light placement.
- Harsh transitions.

Use the existing render pipeline and appropriate project lighting features.

Do not switch rendering pipelines unnecessarily.

## 11. Make Each Environment Look Its Best

Respect the artistic identity of individual games.

Examples:

**Truck Keepaway**
- Attractive arena lighting.
- Readable trucks and hazards.
- Beautiful outdoor skies.

**Truck Kickball**
- Stadium lighting.
- Warm sunset conditions.
- Floodlit nighttime matches.

**Truck Field Goal**
- Proper evening football-stadium atmosphere.
- Large floodlights.
- Impressive nighttime presentation.

**Truck Avalanche Escape**
- Cold mountain lighting.
- Dramatic Arctic skies.
- Convincing snowstorms.

**Truck Sky Tow Rings**
- Spectacular Arctic skies.
- Beautiful atmospheric depth.
- Appropriate lighting on glaciers and mountains.

**Terrier Racing**
- Dramatic volcanic lighting.
- Glowing lava illuminating nearby surfaces.
- Weather and sky integration.

**Truck Floor Is Lava**
- Lava glow.
- Dark volcanic atmosphere.
- Proper environmental contrast.

**Truck Dock Diving**
- Beautiful daylight pool reflections.
- Evening poolside lighting.
- Proper night venue illumination.

**Truck Snake / Hide & Seek**
- City streetlights.
- Illuminated windows.
- Urban atmospheric lighting.
- Convincing night navigation.

**Truck Dance-Off**
- Preserve stage-specific concert lighting.
- Time of day may affect visible exterior scenery but must not override the synchronized show lighting.

Do not force every mode to use identical lighting values.

Use shared time/weather logic with scene-specific artistic adjustments.

## 12. Artificial Light Activation

At night, activate appropriate:

- Stadium floodlights.
- Streetlights.
- Parking lot lamps.
- Industrial lights.
- Building windows.
- Decorative lighting.
- Relevant vehicle lights.

Do not simply darken the sky and leave the level impossible to see.

During daytime, artificial lighting should behave appropriately for the venue.

## 13. Smooth Transitions

When the weather or sky changes during gameplay:

- Smoothly transition sky color.
- Blend cloud conditions.
- Adjust ambient lighting.
- Update fog.
- Update sun/moon illumination.
- Preserve camera exposure stability.

Avoid abrupt one-frame lighting jumps.

For short sports rounds, the selected time of day should generally remain stable throughout the match.

Do not introduce a rapid full-day cycle unless the mode already supports one.

Random time of day should be selected at match initialization.

## 14. Performance

The collection already contains demanding physics, spectators, multiple cameras, and large environments.

Optimize sky and lighting updates.

Avoid:

- Expensive per-frame reflection updates.
- Excessive real-time shadow-casting lights.
- Unnecessary material duplication.
- Rebuilding lighting resources repeatedly.
- Expensive weather transitions.
- Significant split-screen performance regressions.

Use sensible quality settings and scene-specific overrides.

Preserve attractive visuals without destroying performance.

---

# PART 4 — GLOBAL SCORING ANIMATION SYSTEM

## 15. Every Meaningful Scoring Event Needs Celebration

Right now, many sports award points without enough visual feedback.

Create a reusable, polished scoring presentation system.

**Whenever a participant earns a meaningful point or wins a scoring event, display an attractive animation identifying who earned it.**

The presentation should clearly communicate:

- Who scored.
- How many points were earned.
- Which team or participant received them.
- Why they were awarded, when relevant.
- How the score changed.

## 16. Animated Scoring Presentation

Use a visually satisfying sequence:

1. A scoring event is confirmed by authoritative gameplay logic.
2. The scoring participant is identified.
3. Their name and truck identity are highlighted.
4. A score animation appears.
5. Points visibly increase.
6. The updated leaderboard is shown.
7. Gameplay continues normally.

Example:

**BIG DADDY SCORES!**

**+3 POINTS**

Animated truck icon, colorful highlights, subtle particles, and satisfying audio.

Do not display unstyled text in a black rectangle.

## 17. Game-Specific Scoring Events

Adapt feedback to each sport.

Examples:

### Kickball
- RUN SCORED!
- HOME RUN!
- Grand celebration for a home run.

### Football
- FIELD GOAL!
- Dramatic scoreboard animation.
- Crowd cheering.
- Holder truck dancing.

### Keepaway
- Possession milestone.
- Successful steal.
- Winning possession threshold.

Do not interrupt gameplay every fraction of a second while continuous possession time accumulates.

### Truck Sumo
- RING OUT! +1
- LAST TRUCK STANDING! +3

### Bowling
- STRIKE!
- SPARE!
- GUTTER!

Preserve the bowling animations already functioning. Do not unnecessarily replace them.

### Bungee Destruction
- Structural damage score.
- Major demolition bonus.
- Full building collapse.

### Truck Fishing
- Successful catch.
- Fish weight awarded.
- High-value catch celebration.

### Hot Potato
- Successful bomb transfer where points are awarded.
- Explosion survival.
- Round victory.

### Floor Is Lava
- Successful opponent elimination.
- Survival bonus.
- Round victory.

### Magnet Mayhem
- Successful metal-object scoring.
- Team score update.

### Poker
- Preserve the existing showdown and winner animation system.
- Do not override poker-hand presentation with generic scoring graphics.

### Other Sports
- Use the actual scoring and victory mechanics for each game.

Do not fabricate scoring events or award points from visual animation code.

---

# PART 5 — REUSABLE SCORING PRESENTATION ARCHITECTURE

## 18. Build a Shared Scoring Presentation Manager

Implement or extend the existing score-presentation infrastructure.

The gameplay system remains authoritative.

The visual system consumes confirmed scoring events.

A scoring event should provide appropriate data such as:

- Sport ID.
- Match ID.
- Event ID.
- Participant ID.
- Participant display name.
- Player color.
- Team ID where relevant.
- Points awarded.
- Updated total.
- Scoring reason.
- Event severity or presentation tier.
- Whether a round or match has ended.

Use stable participant IDs, not display-name strings, for scoring attribution.

## 19. Avoid Duplicate Scoring

A scoring animation must never create additional gameplay points.

Prevent:

- Duplicate score awards.
- Repeated notifications from the same event.
- Incorrect winner identities.
- Stale score displays.
- Events surviving into new matches.

Use event IDs or other reliable deduplication.

## 20. Support Different Celebration Intensities

Use several animation tiers.

### Small Score

Quick, attractive popup.

Minimal disruption.

### Major Score

Bigger animation and stronger effects.

Examples:
- Home run.
- Building collapse.
- Ring-out.
- Major target score.

### Round Winner

Show a short cinematic victory announcement and highlight the winner.

### Match Champion

Full-screen celebratory results presentation.

Use the appropriate tier based on the event.

Do not play a five-second full-screen animation for every minor continuous score update.

## 21. Keep Gameplay Moving

Some games award points frequently.

Do not make scoring feedback interrupt the game constantly.

Requirements:

- Ordinary scoring animations should be non-blocking.
- Multiple rapid scores may queue or combine.
- Avoid overlapping giant banners.
- Allow controlled pacing for turn-based events.
- Allow skipping longer animations where appropriate.
- Preserve input during active gameplay unless a legitimate round transition requires otherwise.

The presentation should make the game more exciting, not slower and frustrating.

---

# PART 6 — COMPLETE RESULTS SCREEN OVERHAUL

## 22. Every Sport Needs a Beautiful Results Screen

At the end of every round or match, show a polished results presentation.

It should match or exceed the best current Truck Sports interfaces.

Include relevant:

- Winner or winners.
- Player/truck names.
- Truck icons.
- Player colors.
- Final points.
- Placement.
- Sport-specific statistics.
- Match summary.
- Replay or continue options where supported.
- Rematch.
- Return to menu.

## 23. Highlight the Winners

The winner should be visually celebrated.

Use:

- Animated score cards.
- Truck portrait or live truck presentation.
- Gold highlights.
- Confetti.
- Animated lighting.
- Satisfying sound.
- Trophy or medal presentation.
- Winning truck celebration.

Use the actual globally saved participant name.

## 24. Show How Points Were Earned

Where appropriate, show a concise breakdown.

Examples:

**Truck Sumo**

Ring-outs: 3

Survival Bonus: 3

Total: 6

**Truck Target Launch**

Final Ring: Bullseye

Points: 500

**Truck Fishing**

Fish Caught: 12

Total Weight: 86 lb

Use only actual statistics generated by the game.

Do not invent irrelevant metrics.

## 25. Animated Scoreboard Ranking

At the end of a match:

1. Show participant results.
2. Animate the final totals.
3. Sort into correct ranking order.
4. Highlight the winners.
5. Play an appropriate celebration.

Handle ties properly.

Do not incorrectly choose a single winner when the gameplay rules produce a tie.

## 26. Participant Animation

Use available expressive truck-animation systems for winner celebrations.

Examples:

- Happy cab bounce.
- Proud wheelie.
- Side-to-side wiggle.
- Celebratory spin.
- Appropriate sport-specific animation.

Do not trigger physics explosions or destabilize trucks merely to make them dance.

## 27. Split-Screen Results

During local multiplayer:

- Present one clear shared results layout where appropriate.
- Do not duplicate the entire results screen in four quadrants.
- Preserve player names and identities.
- Correctly identify AI versus human results.
- Ensure rematch input ownership works.

## 28. Protect Special Existing Presentation

Do not overwrite already successful specialized presentation systems.

Examples:

- Poker showdown.
- Bowling strike/spare animations.
- Kickball home-run celebrations.
- Existing sport-specific victory sequences.

Integrate them into the shared presentation architecture or preserve them as overrides.

Do not replace something already excellent with a generic animation.

---

# PART 7 — INTEGRATE WITH ALL REGISTERED SPORTS

## 29. Audit Every Game

Inspect the actual Truck Sports mode registry.

Apply skybox/time-of-day improvements to every compatible environment.

Apply scoring feedback and results presentation to every active playable sport.

This includes older modes and the recent 18-game expansion where their implementations are present.

Do not omit a sport merely because it uses a different score format.

For indoor games, apply appropriate interior lighting and results UI without unnecessarily exposing an outdoor skybox.

For modes using continuous scoring, aggregate feedback sensibly.

For racing modes, emphasize checkpoints, finish placement, and round winners.

For survival modes, emphasize eliminations and final survival.

For team modes, clearly identify the scoring team.

---

# PART 8 — MANDATORY ACCEPTANCE TESTS

## 30. Skybox Tests

For representative outdoor sports, inspect:

- Clear Morning.
- Clear Day.
- Clear Evening.
- Clear Night.
- Rainy Evening.
- Thunderstorm Night.
- Snowy Morning.
- Blizzard Night.
- Hurricane Day.

Verify the sky, lighting, shadows, fog, and ambient atmosphere match the selected combination.

## 31. Time-of-Day Tests

Verify:

1. All four time presets work.
2. Random works.
3. Weather and time are independent selections.
4. Preferences persist appropriately.
5. Lighting matches the selection.
6. Nighttime artificial lighting activates.
7. Indoor sports retain appropriate venue lighting.
8. Short matches do not unexpectedly jump between times of day.
9. Rematches correctly retain or reroll settings according to configuration.

## 32. Score Animation Tests

Test:

- One player scores.
- Several players score in quick succession.
- AI scores.
- Team scores.
- Multiple points awarded at once.
- Round winner.
- Match winner.
- Ties.
- Rapid score changes.
- Skipped animation.
- Rematch reset.

Verify that scoring presentation never duplicates actual points.

## 33. Results Tests

Every active sport must have a functioning results presentation.

Confirm:

- Correct winners.
- Correct player names.
- Correct team attribution.
- Correct scores.
- Correct ranking.
- Relevant statistics.
- Proper rematch.
- Return to menu.
- No UI overlap.
- No stale scores from prior rounds.

## 34. Visual Evidence

Capture actual runtime screenshots demonstrating:

- Beautiful morning sky.
- Clear midday sky.
- Dramatic evening sunset.
- Nighttime stadium.
- Thunderstorm lighting.
- Snowy environment.
- Scoring animation.
- Round winner.
- Match results.

Do not claim visual verification without inspecting actual rendered output.

## 35. Performance

Test performance under:

- Full competitor counts.
- Heavy weather.
- Nighttime artificial lighting.
- Split-screen.
- Large stadium scenes.
- Recent 18-game expansion modes.

Record meaningful frame-time measurements.

Avoid severe performance regressions.

---

# PART 9 — PERMANENT ACCEPTANCE LEDGER

Update the existing permanent Truck Sports requirements ledger.

Do not create a second conflicting backlog.

Track separately:

- Skybox implementation.
- Each weather sky variant.
- Time-of-day selection.
- Time-of-day lighting.
- Scene-specific lighting integration.
- Artificial lighting.
- Score animation integration.
- Round winner presentation.
- Full results-screen integration.

Use the established statuses:

MISSING

PARTIAL

IMPLEMENTED BUT UNVERIFIED

VERIFIED

BLOCKED

Each requirement must include actual files, scenes, tests, and appropriate evidence.

Do not mark skybox or UI improvements VERIFIED merely because scripts compile.

---

# PART 10 — IMPLEMENTATION ORDER

## Phase 1 — Existing System Audit

Inspect:

- Weather architecture.
- Lighting configuration.
- Skybox assets.
- Render pipeline.
- UI architecture.
- Existing scoring events.
- Results screens.

## Phase 2 — Shared Sky and Time System

Build or extend the shared environment presentation controller.

Integrate weather-dependent skies.

Add time-of-day selection.

Implement appropriate lighting.

## Phase 3 — Representative Scene Verification

Before applying broadly, validate several contrasting environments:

- Outdoor arena.
- Stadium.
- Arctic scene.
- Volcanic scene.
- Urban environment.
- Indoor venue.

Correct common problems before rolling out across all sports.

## Phase 4 — Global Score Presentation

Implement the shared score event presentation system.

Add animated player-specific score feedback.

Preserve authoritative game scoring.

## Phase 5 — Complete Results Screens

Integrate the polished results presentation into all registered sports.

Preserve specialized celebrations.

## Phase 6 — Full Integration and Testing

Test weather/time combinations.

Test scoring events and victory screens.

Verify multiplayer layouts.

Perform regression testing.

Update the ledger.

---

# FINAL DIRECTIVE

Truck Sports: Party! should look dramatically better after this pass.

I want to see:

**Beautiful clear mornings with warm sunlight.**

**Rich blue skies during the day.**

**Gorgeous orange and purple sunsets.**

**Starry nighttime competitions under powerful stadium lights.**

**Massive storm clouds and lightning during thunderstorms.**

**Atmospheric blizzards and snowy Arctic skies.**

**Every meaningful point celebrated with attractive animation.**

**Every winner highlighted with a satisfying victory presentation.**

**Every sport ending with a polished results screen.**

Do not simply swap skybox textures and add a generic text popup.

Create a complete, coherent presentation system that connects the weather, sky, lighting, scores, player identities, and match results.

Reuse the existing asset collection.

Preserve current gameplay.

Test actual visual output.

**Make the entire Truck Sports: Party! collection feel more beautiful, lively, cinematic, and release-ready.**