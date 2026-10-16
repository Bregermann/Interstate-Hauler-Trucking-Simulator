# Trampoline Dodgeball

Offline physical truck dodgeball: one to four trucks per team, up to four local humans, named Easy/Normal/Hard AI, first team to three heat wins. Saved scene: `Assets/TruckTrampolineDodgeball/Scenes/TruckTrampolineDodgeball.unity`.

Drive and brake before reversing. Actual trampoline contact launches the truck; finite horizontal aftertouch nudges flight without hovering. Press the assigned action for a visible 0.24-second forward strike/return window, followed by a 1.05-second cooldown. Keyboard action keys are shown in setup; whole-pad South, shared left-stick LT, shared right-stick RT. Whole controllers have one owner, or their two sticks have separate owners. Pause, reconnect, explicit replacement and rematches use the existing collection lifecycle.

Cream balls are neutral. Striking changes a ball to the striker's team color. A live enemy ball eliminates a vulnerable truck once; friendly balls are safe and each striker has a short launch grace. Actual ground contact neutralizes the ball. A correctly timed return changes possession before elimination. One ball cannot eliminate an overlapping crowd in one flight. Contacts are ordered and both teams are evaluated together; an equal-step final wipe draws. Sudden death begins at 75 seconds with the same faster legal strikes for everyone; an unresolved 105-second round draws without awarding a point.

Shared three-quarter arena view is the default. Optional human chase split screen supports four views. Transparent upper safety walls retain containment while keeping trucks visible. Participant cards, team stripes, ball trails, strike crescents and cooldowns identify the action. Replays use recorded truck/ball poses and historical ball colors, with angle, slow/fast, rewind and skip controls.

NWH tires/suspension and PhysX own movement and collisions. Trampoline and strike actions apply a bounded desired-velocity change, once per accepted event. No extra collision knockback, homing, AI immunity or injected scores. AI drives and strikes through the same controls, stages behind neutral balls, estimates visible motion, makes fallible per-flight return decisions, and uses separate ground/air steering policy. Ordinary floor is grounded even without a trampoline component. Side recovery uses physical torque and input, never teleportation.

Authoring: `LWS.TruckDodgeball.Editor.DodgeAuthoring.Create`. Combined final build: `LWS.TruckSports.Editor.SportsBuilder.Build`. Do not use collection CreateValidateBuild for a delivery build because it reauthors older scenes.

## Vendor foundation

- Audited/used: installed NWH/GR3D truck geometry and tires, PhysX, existing Cornhole launch audio, Beer Pong seven-turn spring mesh, Field Goal rounded-frame helper, RoadProps arena dressing, Heat/TMP fonts/UI and DOTween compression/presentation, existing collection input/camera/replay systems.
- Relevant assets not used: NWH full road powertrain is unnecessary for this established lightweight direct-wheel arcade adapter; navigation/traffic, networking and wheel peripherals are outside this sport. No owned complete truck-dodgeball rules/AI solution was found. Existing vendor source is unchanged.
- Custom missing semantics: team elimination/contact ordering, finite strike/return arbitration, contact-driven trampoline response, fallible AI decisions and historical replay team colors. No duplicate vendor vehicle, UI, navigation or weather engine.

See VALIDATION.md for the actual evidence and its limits. Windows production acceptance remains pending until the final combined production build; a saved scene is not a completed Windows build.
