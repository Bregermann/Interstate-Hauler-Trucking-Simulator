# Truck Field Goal — bumper kick

The truck is the kicking foot. Drive into the upright physical football beside its stationary holder truck. Approach speed, direction, contact height and lateral contact determine bounded power, elevation, line and spin. No launch button or airborne football control. NWH owns driving; PhysX owns gravity, flight and post collisions. The contact response applies once, then the shooter loses propulsion without applying another kick.

One to four sequential human/AI competitors use assigned sources or shared hot seat. Keyboard clusters, whole controllers and separate sticks follow established ownership rules. Only the current shooter drives. Easy/Normal/Hard estimate an approach using the same model, retain execution errors and physically drive into the ball. No homing, direct score awards or privileged launch power.

Competition gives five attempts at30/38/46/54/62m, displayed in yards. A forward authoritative-center crossing between physical uprights and above the crossbar scores once if it precedes airborne ground contact. Sweeps/contact chronology preserve post ricochets and reject backward/wide/low/ground-first shots. Later contacts cannot revoke GOOD. Tied leaders receive equal-opportunity extras; first shooter rotates. Solo practice selects a fixed distance and retries indefinitely.

The detached giant-foot/truck-projectile option is retired. Versioned setup migration keeps old competition choices as bumper competition and old practice choices as bumper practice, preserving saved names/identities. Established bumper personal-best/statistics keys remain in use.

## Weather and presentation

Choose Random or one of33 installed/configured Weather Maker forecasts through the Heat dropdown. The stable shared preference survives reloads. One forecast lasts the match, never reset per attempt. Explicit weather remains selected; Random rolls a fresh seed on rematch. Weighted selection uses Weather Maker's profile-group API. Other sports retain their current weather integration until their queued migration is completed.

The same actual vendor wind drives bounded relative-air drag/spin forces, windsock and compact MPH card. Its arrow is relative to the current camera. Head/tail/crosswinds affect flight continuously. Wet/snow presets reduce the same NWH grip for humans and AI. Existing Taxi/Weatherade accumulation/contact-track adapters remain the snow foundation.

The holder reuses the existing truck render model without adding another input/physics authority or obstructing the kicking lane. Goalposts stand at the back of a marked ten-yard end zone with the original Truck Sports: Party! wordmark. The football's visual and collision long axes are upright on the tee; actual angular motion rotates laces/stripes. Recorded noncolliding replay retains three angles,0.5/1/2x, rewind and skip. Pause/reconnect preserves the attempt; replacement input requires neutral before resuming.

## Editing and regeneration

Scene: Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity. Match-owned flightModel exposes shared contact power, air drag, spin lift and acceleration caps. Weather metadata: Assets/TruckSports/Settings/SportsWeatherPresets.asset (stable IDs, profile references, random weights, grip and explicit wind-unit conversion). Zero random weight excludes an entry from Random. Existing customized catalog entries are never automatically repopulated.

FieldGoalRepairAuthoring.Install upgrades inspected saved objects additively; explicit fresh-scene authoring invokes it too. Normal builds use saved scenes via LWS.TruckSports.Editor.SportsBuilder.Build. Do not invoke destructive scene rebuilding for delivery. Preserve original Taxi/vendor prefabs and host settings.

## Validation and delivery

Windows x64 Development;35 explicit Sports-only scenes; TruckSports startup;0 errors /20 warnings. Built 2026-10-18T17:53:15.697166+00:00. Managed SHA256 `6d67807a233ee2a2163b44b902c096e86ca0958b3b1b92ac2032c0405c2c6446`;438-file data manifest `d97805acd6b7cbc758219bd00ce3a7d643cc601adc0af59a0e0fa9df7d464633`.

All15 exact-build Windows suites pass, including real driven kicks, full AI difficulty/weather matches, input/replay/score regression and60-second snowy single-camera performance. See [repair, causes, evidence and limitations](../../Documentation/TruckSports/FieldGoalRepair.md). No human or physical-controller/audio acceptance claim. The whole queued release remains unfinished.

VENDOR ASSETS AUDITED: NWH truck/wheels; Heat dropdown/UI Manager public customization; Cinemachine/DOTween; Weather Maker8.0.9 profile/group/URP APIs; existing stadium and Taxi/Weatherade snow adapters.

VENDOR ASSETS USED: NWH through SportsEventTruck; Heat/uGUI; existing Cinemachine/DOTween; Weather Maker full profiles, weighted selection, wind/precipitation/cloud/fog/lightning and vendor Render Graph feature; existing snow, stadium/truck art, audio/replay/input/settings.

RELEVANT ASSETS NOT USED: No suitable installed windsock or football model was found in the relevant inventory. Owned tapered cloth mesh and established prolate football fill those shapes. The retired detached boot is disabled. Traffic/general flight controllers do not provide this sport's contact/scoring semantics.

CUSTOM SYSTEMS CREATED: Contact/aerodynamic semantics, equivalent AI approach estimation, stable forecast metadata/Heat binding, additive scene/default integration and explicit development probes. Existing chronological scoring/shared foundations are extended.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None.

### Stadium presentation — October19 working update
The field is a full120yards with10-yard end zones, one-yard hashes and traditional sideline yard numbers. The original Party logo sits at midfield; a new original Field Goal wordmark spans the goal end zone. The embedded stadium board stays behind/above the posts at every kick distance.

The stadium holds2304 existing-character spectators, with32 live animators. Occasional jumping, comic scuffles and sealed jug tosses stay cosmetic. A rare referee-truck/pitch-invader sequence can play after a result locks, using the existing Taxi ragdoll and a fixed pool; pause, rematch and menu return clean it up. Kick physics and scores remain authoritative and independent. For editable chance/timing, select the `FieldGoalStadiumAntics` component on the FieldGoalMatch object. Set Referee Chance to0 to disable the vignette.

See `Documentation/TruckSports/FieldGoalStadiumBoard.md` for exact validation and current build status. Native validation does not imply Windows/human/controller acceptance.

Latest follow-up: the actual wind sock mounts at the upper-right upright corner, 19m above turf. The stadium apron and end concourse use green textured grass and real collision; the authoritative football recognizes them as ground and resolves a miss once. The midfield Party art faces the right-hand stands. Native final field/board acceptance is 42/0, dense crowd 33/0 and collection PlayMode 28/28; Windows build/player verification is recorded separately in FieldGoalStadiumBoard.md.
