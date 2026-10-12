# Truck Hockey — 3v3

Six saved trucks, three per team, inside Truck Sports. Scene: Assets/TruckHockey/Scenes/TruckHockey.unity. Start from Assets/TruckSports/Scenes/TruckSports.unity for the collection.

Choose 0–6 humans; AI fills the other slots. Names are editable for everyone, with independent AI difficulty, team color and B1–B3/O1–O3 markers. Keyboard Chaos, six whole controllers, three shared controllers and Custom presets are available. Rematches/menu returns preserve all slots. Rematches swap ends.

Keyboard clusters: W/A/S/D; T/F/G/H; I/J/K/L; arrows; Z/X/C/V; Numpad8/4/5/6. Keys are editable and active duplicates/Escape are rejected. Input Check shows all received sources; physical keyboard rollover still requires testing.

Whole pad: left stick steer, RT forward, LT brake/reverse, North recover. Shared sticks: each stick X steers and Y drives its own truck relative to its forward direction. Opposite throttle brakes before reversing; no dedicated brake/shoot/pass/boost is required. Start/Escape pauses. Pause offers six separate recover actions. Shared-pad disconnect preserves and identifies both owners; Resume requires fresh controls for both.

Retained rules score complete puck crossings for the attacking TEAM, including own goals; the live-only clock, deadline handling, fair resets and sudden death remain. Physical impacts have strength-scaled sound, short pooled sparks and restrained assigned-pad rumble. PhysX owns the impact response; no second impulse is added.

Coordinated AI switches challenger/support/defender roles with hysteresis. Recovery checks all five other trucks and clears stale contact history. Ordinary accessible blocks/crowds are left alone. Invalid geometry or sustained trapping without an escape announces a fair faceoff, preserving score/time.

Runtime: HockeyMatch, HockeyTruck, HockeyPuck, HockeyAI, HockeyMenus and HockeyContactFeedback. Saved-scene migration: SportsUpdateAuthoring. Final build: LWS.TruckSports.Editor.SportsBuilder.Build. Authoring defaults also produce the current six-participant implementation.

See ../TruckSports/README.md for full controls/vendor boundaries and ../TruckSports/UPDATE_3V3.md for actual tests, Git delivery and hardware limits.