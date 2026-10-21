# SOL 6.1 HIGH — TRUCK SPORTS: PARTY!
# GLOBAL BROADCAST CAMERA OVERHAUL
## CRITICAL: TRUCKS ARE FAR TOO SMALL ON SCREEN

Human playtesting has identified a major presentation problem affecting numerous Truck Sports modes.

**The shared/broadcast cameras are zoomed WAY too far out.**

Trucks appear extremely tiny, especially in:

- Truck Kickball
- Truck Magnet Mayhem
- Other multiplayer games using an elevated camera that attempts to show every participant

This makes the games difficult to watch, difficult to play, and visually unimpressive.

**Fix this across the ENTIRE Truck Sports collection, including newly added modes.**

Do not simply move every camera closer by a fixed number of units. Implement proper dynamic framing with reasonable limits.

---

## 1. ESTABLISH MINIMUM VISIBLE TRUCK SIZE

The main rule:

**Player trucks must remain large enough onscreen to be clearly recognizable and readable.**

Implement a configurable minimum projected truck size for broadcast cameras.

Suggested initial targets for the primary action:

- Main controlled truck: approximately 8–12% of viewport height when appropriate.
- Other directly involved trucks: ideally 5–8% or larger.
- Avoid letting important trucks shrink below approximately 4% of viewport height during normal gameplay.

These are tuning targets, not rigid guarantees for every cinematic moment.

Measure the actual projected size of truck render bounds in screen space rather than relying entirely on world-space distance.

Support both perspective and orthographic cameras.

If an extreme multi-truck spread makes these size targets impossible while showing everyone, **prioritize the active action rather than zooming infinitely outward**.

Expose settings in the Unity Inspector.

---

## 2. ADD A MAXIMUM BROADCAST CAMERA DISTANCE / ZOOM LIMIT

Every shared camera needs an appropriate maximum zoom-out boundary.

For perspective cameras:
- Limit effective camera distance.
- Limit excessive field-of-view expansion.
- Account for actual truck scale and arena dimensions.

For orthographic cameras:
- Set sensible maximum orthographic size.
- Preserve truck readability.

Do not use one arbitrary value for every arena.

Create shared camera defaults with mode-specific overrides.

The maximum distance should be relative to the sport's gameplay scale and camera target.

**Do not allow a camera to zoom halfway into space just because one AI truck drove far away.**

---

## 3. STOP TRYING TO KEEP EVERY TRUCK VISIBLE AT ALL TIMES

This is likely a major cause of the problem.

Current broadcast framing appears to prioritize fitting every participant into one shot regardless of separation.

Change that behavior.

### Priority-based framing

Focus on:

1. The active human-controlled participant where appropriate.
2. The main gameplay objective.
3. Trucks currently interacting with that objective.
4. The immediate surrounding competitors.
5. Other competitors only when reasonably close.

Distant or irrelevant AI competitors should not force the entire camera to zoom out.

Examples:

**Kickball:** Focus on the batter, ball, active baserunners, and involved fielders. Do not zoom out to show every outfielder standing across the entire stadium.

**Magnet Mayhem:** Focus on the controlled truck, nearby magnetic objects, active opponents, and contested scoring zones. Do not zoom out because an unrelated AI is across the scrapyard.

**Keepaway:** Focus on the ball carrier, nearby challengers, and loose ball interactions.

**Hot Potato:** Prioritize the bomb carrier and the trucks currently chasing or fleeing from it.

**Volleyball:** Focus on the ball and current rally participants.

**King of the Hill:** Focus on the active scoring zone and trucks contesting it.

Each game should define meaningful camera targets.

---

## 4. IMPLEMENT INTELLIGENT DYNAMIC FRAMING

The broadcast camera should adjust according to actual gameplay.

### When action is concentrated

- Move closer.
- Make trucks larger.
- Use tighter framing.
- Keep the objective visible.
- Show meaningful vehicle detail.

### When action spreads out

- Zoom out moderately.
- Respect the maximum zoom-out limit.
- Keep the most important action in frame.
- Allow less relevant trucks to leave the screen.

### When action changes location

- Smoothly follow the new important area.
- Use controlled camera transitions.
- Avoid abrupt snapping.
- Prevent rapid target switching.
- Avoid constant zooming in and out.

Use configurable hysteresis, damping, and minimum shot durations where appropriate.

The camera should feel like an intelligent sports broadcast operator.

---

## 5. ADD OFFSCREEN COMPETITOR INDICATORS WHERE USEFUL

When important competitors leave the camera view, use subtle directional indicators instead of zooming out excessively.

Possible indicators:

- Small player-colored arrows.
- Competitor name.
- Direction toward competitor.
- Ball or objective indicator.

Keep these minimal.

Do not clutter the edges of the screen with every AI participant.

Do not reveal hidden players in stealth-based games such as Hide & Seek.

Only show information the current participant is legitimately allowed to know.

---

## 6. TRUCK KICKBALL — SPECIFIC CAMERA REPAIR

Kickball is one of the worst offenders.

The broadcast camera should resemble an actual sports broadcast.

Use the previously requested elevated broadcast-booth-style location on the **first-base side between home plate and first base** as the primary default angle.

### During pitching

Frame:
- Pitcher.
- Football-sized or larger kickball.
- Batter.
- Immediate home-plate area.

The batter and pitcher should be clearly recognizable.

### After a kick

Transition to ball-focused broadcast tracking.

Frame:
- Moving ball.
- Relevant fielders.
- Relevant baserunners.
- Immediate play.

Do not insist on showing the entire baseball diamond in every frame.

### During baserunning

Focus on the base where the next meaningful play is occurring.

Allow the camera to track the active runner and defending truck.

### Home runs

Use a cinematic ball-tracking shot, followed by the already requested celebration sequence.

Do not zoom out to show all twelve trucks merely because a home run occurs.

### Acceptance

Human players must be able to recognize individual trucks, the ball, and the actual play from the normal broadcast view.

The broadcast should resemble a televised sporting event, not a satellite image of a baseball stadium.

---

## 7. TRUCK MAGNET MAYHEM — SPECIFIC CAMERA REPAIR

Magnet Mayhem also suffers from trucks appearing extremely small.

Prioritize:
- The human-controlled truck.
- Nearby opponents.
- Magnetically manipulated objects.
- Current target scrap.
- Active scoring-zone interactions.

When a magnetic object moves across the arena:

- Track its movement when relevant.
- Keep the controlling truck readable.
- Avoid excessive zoom-out.
- Smoothly transition between important magnetic interactions.

The player should be able to see the truck's actual magnet behavior, polarity effects, and nearby metal objects.

**The game must not look like a tiny collection of objects moving around a distant scrapyard.**

---

## 8. FIX ALL OTHER APPLICABLE SPORTS

Audit every registered Truck Sports mode that uses:

- Shared Arena View.
- Broadcast View.
- Elevated spectator camera.
- Automatically zooming multiplayer camera.
- Group-target framing.
- Cinematic overview cameras.

Do not limit this correction to Kickball and Magnet Mayhem.

Test established sports and all eighteen recently requested additions where currently implemented.

However, do not force unrelated cockpit, first-person, precision aiming, or fixed strategic cameras to use this behavior.

Preserve intentionally wide establishing shots as brief cinematic moments.

---

## 9. SUPPORT HUMAN-ONLY AND AI CAMERA OPTIONS

Respect the existing **Show AI Cameras** setting.

When AI cameras are disabled:
- Human viewports receive the available screen area.
- AI continues participating.
- Broadcast framing should not be distorted by unnecessary AI camera targets.

When AI cameras are enabled:
- Preserve appropriate additional camera views.
- Maintain correct viewport ownership.
- Recalculate framing relative to each actual viewport.

For AI-only spectator games, use sensible director-style camera behavior to track the active action.

Do not show blank or excessively distant camera views.

---

## 10. CAMERA SAFETY AND SMOOTHING

Preserve functional camera movement.

Requirements:

- No camera clipping through terrain.
- No underground camera positions.
- No wild rotation changes.
- No sudden excessive zoom.
- No constant camera oscillation.
- No jitter from physics updates.
- No obstructed truck views.
- No invalid target references.
- No camera spazzing after AI elimination.
- No incorrect viewport assignment.
- No unnecessary full-camera reset after every small event.

Use appropriate interpolation and camera collision handling.

If targets become invalid, transition cleanly to a suitable active gameplay target.

---

## 11. CONFIGURABLE SHARED CAMERA SETTINGS

Create or update a reusable camera-framing configuration with parameters such as:

- Minimum projected truck size.
- Preferred projected truck size.
- Maximum zoom-out.
- Minimum zoom-in.
- Preferred camera elevation.
- Follow distance.
- Target-priority weighting.
- Objective-priority weighting.
- Camera damping.
- Zoom damping.
- Target-switch hysteresis.
- Offscreen-indicator behavior.

Use sensible global defaults.

Allow individual sports to override settings where necessary.

Do not introduce multiple contradictory camera managers.

Reuse and improve the existing shared framework.

---

## 12. MANDATORY VISUAL ACCEPTANCE TESTS

This issue is fundamentally visual.

Compilation and isolated camera tests are insufficient.

### Test A — Kickball

Launch with twelve trucks.

Confirm the camera can show the active play without the trucks becoming tiny.

Test pitching, batting, fielding, baserunning, and home runs.

### Test B — Magnet Mayhem

Launch a full competitive match.

Confirm the main truck and nearby action remain large and readable.

Move opponents far apart.

Verify that distant AI trucks do not force extreme zoom-out.

### Test C — Multiple Distant Trucks

Place competitors on opposite ends of a large arena.

Verify the camera selects the relevant action instead of zooming indefinitely outward.

### Test D — Concentrated Action

Bring several trucks together.

Verify the camera moves closer without excessive jitter or clipping.

### Test E — Dynamic Objective Changes

Change ball ownership, target locations, or scoring-zone activity.

Verify smooth camera transitions.

### Test F — Eliminations

Eliminate camera targets.

Verify the broadcast camera selects valid remaining gameplay targets without spasms.

### Test G — Split-Screen

Test supported one-, two-, three-, and four-human configurations.

Ensure per-viewport framing remains readable.

### Test H — Screen Resolutions

Visually inspect:

- 1280×720.
- 1920×1080.
- Other supported aspect ratios where practical.

### Test I — All Applicable Sports

Perform a camera audit of every currently implemented Truck Sports game with group/broadcast framing.

Identify and correct similar extreme-zoom issues.

---

## 13. BEFORE-AND-AFTER EVIDENCE

For Kickball, Magnet Mayhem, and at least three other affected sports, provide:

- Before/after screenshots where available.
- Actual projected truck screen-size measurements.
- Camera configuration changes.
- Evidence of correct action-based target selection.
- Confirmation that the objective remains visible.
- Runtime testing results.

Document any sport-specific exceptions.

Do not claim that camera readability improved without inspecting the rendered game.

---

## 14. UPDATE THE PERMANENT ACCEPTANCE LEDGER

Create or update the shared broadcast-camera requirements in the existing Truck Sports ledger.

Track each affected sport separately.

Record:
- Original camera issue.
- Root cause.
- Scene and scripts changed.
- Framing settings.
- Runtime screenshot evidence.
- Actual truck visibility.
- Test results.
- Remaining limitations.

Use the established statuses:

MISSING / PARTIAL / IMPLEMENTED BUT UNVERIFIED / VERIFIED / BLOCKED.

Do not mark this camera pass verified based solely on successful compilation.

---

# FINAL GOAL

The broadcast cameras should make Truck Sports: Party! look like a real, polished arcade sports game.

**Trucks must be big enough to see.**

The camera should prioritize the action, not obsessively frame every truck across a gigantic arena.

Kickball should resemble a televised sporting event.

Magnet Mayhem should clearly show the magnetic combat.

Keepaway should focus on possession battles.

Other sports should use equally intelligent, readable framing.

Preserve the ability to see more of the arena when appropriate, but **never sacrifice basic truck visibility just to keep irrelevant distant competitors onscreen**.

Fix this as a shared camera-system improvement and verify it across the collection.