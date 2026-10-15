# Truck Base Jumping

Four competitors always,0–4 humans plus AI. Three rounds; valid placements4/3/2/1, invalid0. Overall points, then lower total valid landing error, then lower total valid time. An exact overall tie adds another four-truck round.

Equal helicopter release altitude/time/range; starting positions rotate. Movement is compass-aligned air steering. ACTION deploys once; release, then hold ACTION to flare. Opening is progressive and wind drift is shared. Early deployment trades time for control; late/missed deployment risks a crash.

First ground contact locks body-center horizontal error in millimetres, time in milliseconds and pre-contact descent/attitude. Safe: canopy>=80%, descent<=8m/s, planar<=12m/s and up-dot>=.75. Safe inside target outranks safe outside; then one-metre accuracy band, time, exact error. Display uses feet/inches from those stored measurements. No competitor collisions or post-contact score improvement. Leaving the field or110active seconds without landing is invalid.

Pause and source disconnect preserve live physics, attempts and scores. Reconnect the same device or explicitly assign a replacement, release controls, then freshly confirm. Rematch retains setup. Shared audio/steering/vibration/display settings apply. No online services.

Build through LWS.TruckSports.Editor.SportsBuilder.Build with the explicit saved Sports scenes. Scene regeneration uses this sport’s owned Editor authoring utility and shared SportsEventAuthoring.

Vendor foundation: Weather Maker sky/precipitation/wind presentation and one shared gameplay wind field; NWH/GR3D payload meshes; owned helicopter/parachute visuals because no suitable installed asset was found; Heat/TMP; Cinemachine; scoped URP renderer restored on exit.

Validation and remaining human/device checks: see Assets/TruckSports/VALIDATION.md and Documentation/TruckSports/ExpansionCheckpoint.md. Development smoke runners execute only with their explicit validation flags and are inert in normal play. No physical-controller or human playtest acceptance is claimed.
