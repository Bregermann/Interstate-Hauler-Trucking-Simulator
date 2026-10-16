# Truck Minefield — final-rest scoring update

Eight competitors drive the same seeded mile course. Drive across the mile finish before exploding to win immediately; a launched truck crossing it only earns distance. First to five heat points, shared tied heat points and equal-leader tiebreaks remain unchanged.

After a mine/sheep launches the truck, physical bounces, skids, rolls and distinct later blasts continue. The first surface contact ends aftertouch, not motion or scoring. The authoritative final forward COM position is recorded only after supported linear speed<0.25m/s and angular speed<0.35rad/s remain quiet for1.5s. A stationary unsupported apex is never rest. Backward sliding lowers the final distance; path length and peak distance are not scored. Driving, traction, first blast power, existing bounded aftertouch and PhysX impacts retain their authority. Added secondary blast contribution is bounded by the existing95m/s speed ceiling and never replenishes aftertouch.

Every blast-origin and final-rest flag persists for the match, with participant color, heat and blast/attempt identification. Flags wave, have nonblocking geometry, standard LOD and bounded camera labels. Rematch clears previous-match history. Score precision remains the established0.1m rule, while player text displaysfeet. Replays record through physical resolution (300s capacity for this mode), with existing angle/speed/seek/skip controls.

The180s driving limit no longer truncates launched motion. A separate generous90s first-blast simulation failure locks its CURRENT position once and labels RECOVERY LOCK; leaving the authored world still fails. Ordinary full-match validation used none of these recovery locks. Once a genuinely settled attempt is finalized, its body is noncolliding and cannot acquire another score.

Native focused/17-heat full-match/source/lifecycle acceptance passed; see VALIDATION.md. This update is newer than the current25-sport Windows executable and awaits the final queued production-pass build. It is not yet Windows/human/controller-hardware acceptance.

## Earlier implementation notes (historical; first-contact policy superseded)

# Truck Minefield

Eight competitors, up to four local humans, first to five heat points. Mines and terrain use one reproducible layout throughout a match. Steer and accelerate through a broad mile-long rolling meadow; opposite throttle brakes then reverses. Read subtle real mine plates, winding clear gaps, explosive sheep and physical roadside props.

A real qualifying mine/sheep contact launches the truck once. Normal NWH propulsion ends. Left/right provides a small airborne correction. Release throttle once to enable forward/back range correction; the shared 2.2-second budget cannot hover or reverse flight. First subsequent terrain contact locks forward distance at the authoritative body center, rounded to 0.1m. Sideways travel, later bounces and repeat explosions cannot improve it. Failure or the generous 180-second timeout does not fabricate a landing score.

The farthest valid landing earns a point; tied heat leaders each score. First to five wins. Multiple five-point leaders run a tiebreak restricted to those leaders; a single next-heat leader resolves it. Cross the mile line within the actual course while still driving to win the whole match instantly. Mine-launched airborne crossings cannot do that. Simultaneous unresolved driven crossings require a tiebreak.

Tall actual waving flags mark blast and landing. A personal-best marker persists between heats; when the current landing is the best, the landing flag carries that label rather than drawing two flags on top of each other. Names have compact contrast backings and bounded per-camera apparent size. Resolved landing cameras pull back to show the marker and aftermath. One to four chase views retain a stable horizon through tumbles; AI-only uses broadcast. Replays record actual transforms and are nonphysical, with angle, speed, rewind, skip and automatic AI progression.

The shared collection setup supports names/colors, independent Easy/Normal/Hard, keyboard clusters, whole pads and exclusive shared-stick ownership. Up to four human slots; AI fills eight. AI sees nearby mine clues and remembers its own blasts, using the same tires, launch and aftertouch. Pause/disconnect preserve motion, scores and markers; replacement requires neutral then fresh input. Rematch keeps configuration. Offline only.

Saved scene: Assets/TruckMinefield/Scenes/TruckMinefield.unity. Author: LWS.TruckMinefield.Editor.MinefieldAuthoring.Create. Final combined Windows build: LWS.TruckSports.Editor.SportsBuilder.Build. Current validation and built-player limits: Documentation/TruckSports/SevenSportsCheckpoint.md.

## Assets

NWH WheelController owns approach tires/suspension; GR3D provides the actual trucks. Actual Hot Rod Oldtimers mine model, URP material and its three textures were selectively imported from the local owned asset collection. Actual DestroyIt demo flag mesh/pole/rig is reused without DestroyIt scripts. Existing SheepV5, ConstructionPropsBundle ramps/barrels/planks, RoadPropsPack tires, IndieKit crates, Mega City trees and existing rocks dress the course. Vista grass and Path Painter mud terrain layers, Weather Maker sky/clouds/precipitation/wind/thunder, Heat/TMP shared setup and Cinemachine-backed camera adapters are reused. Replay, phase/scoring rules, clue-limited AI and flag coordination are owned sport semantics. No vendor source changed. No assets purchased or services added.

Actual surrounding terrain and an extended landing runout keep flights over a real landscape. Four old truck wrecks and installed fence models complete roadside props. Bounded colored flight trails, ground-contact dust and existing applause accompany events. Replay flags reveal at the actual recorded blast/landing times. Latest Editor: complete nine-heat eight-AI match, nine rule fixtures and36 focused checks pass with zero errors. Final Windows validation remains pending.
