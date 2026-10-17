# Current combined Windows delivery — October16,2026

Production update: three achievable target layouts selected equally per round, equivalent wind/weather timeline for each attempt, bounded actual flight wind, readable power/release meter and MPH arrow. Full physical scoring, focused equal-condition/replay checks and final Windows layout acceptance passed. No extra scripted launch impulse was added. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Truck Giant Swing

One to eight competitors take turns on a real twenty-metre hinged seat. Three throws each; cumulative points win, with extra attempts for tied leaders. Solo keeps a local personal best. Sources can be reused between turns, including one whole controller; each turn has one active owner.

Time fore/aft movement to pump the physical seat. Weight shifts change the truck centre of mass and bounded torque adds energy only through the pendulum. Opposing movement wastes momentum. Preparation lasts at most35s; mapped ACTION releases, or the limit releases at the current pose. Release inherits the real seat point velocity at the truck centre of mass and actual angular velocity, with no added launch impulse or chosen landing position.

In flight, lateral input and fresh fore/aft input share a1.6s horizontal correction budget at0.85m/s². Held approach input cannot consume range correction immediately. There is no vertical lift or post-contact control. First ground contact locks the authoritative centre-of-mass X/Z zone once; later bounces cannot change it. Visible zones award10,25,50 or100; closed boundaries, smaller precision zone first where they overlap. The100 bullseye is nearer than the50 zone. Failed flights score zero.

Each throw has a bounded actual-pose replay, three camera angles, quarter/half/normal/2×/4× playback where available, rewind and skip. AI-only play advances automatically. Replay ghosts contain render transforms only and cannot affect physics, score or progression. Pause/reconnect preserve the pendulum and free flight. Rematches retain names, colors, sources and difficulty.

Saved scene: `Assets/TruckGiantSwing/Scenes/TruckGiantSwing.unity`; native regeneration: `LWS.TruckGiantSwing.Editor.SwingAuthoring.Create`. Uses tracked GR3D truck meshes, installed construction pallet seat/cable reel, NWH truck body foundation, Cinemachine, Heat/TMP, DOTween loading, installed grass/barriers/trees/rocks. No installed complete giant-swing asset was found; the owned bolted steel gantry, constrained seat semantics, scoring and replay coordination fill that gap. No vendor source modified.
