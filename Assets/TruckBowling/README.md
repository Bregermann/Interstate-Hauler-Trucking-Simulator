# Truck Bowling

The third offline Truck Sports mode. Saved scene: `Assets/TruckBowling/Scenes/TruckBowling.unity`. Start the collection from `Assets/TruckSports/Scenes/TruckSports.unity`. The truck itself is the bowling ball; there is no ball projectile or throw button. No online services, networking packages, powerups or additional sports were added.

## Play and participants

Simultaneous: 1–4 participants, permanently assigned one lane each, with independent deliveries, pinsetters and full-screen / split-screen cab views. A completed lane leaves the other lanes running. Final standings appear only when every participant finishes.

Hot seat: 1–8 participants share one lane. Complete a whole frame before the next participant, including all earned tenth-frame deliveries. Shared human devices require a named handoff confirmation; AI turns advance automatically. Separately assigned devices drive only their active participant. Shared keyboard hot seat uses W/A/S/D. Final results remain until rematch, setup or collection return.

Every slot has an editable name, fixed distinct identity color, human/AI source and independent Easy/Normal/Hard difficulty. Setup is persisted as `TruckBowling.Setup.v1`; rematches preserve live device assignments, identities and settings. Scene re-entry preserves setup and controllers explicitly rejoin. North/Y/Triangle joins an unassigned controller source. Reassign is explicit. Whole controllers are exclusive in simultaneous mode; complementary left/right sticks may share one controller. Duplicate sticks and overlapping keyboard keys are rejected. Hot seat permits shared input sources because only one participant drives.

Keyboard clusters in slot order: W/A/S/D; arrows; T/F/G/H; I/J/K/L; Z/X/C/V; numpad 8/4/5/6; number-row 8/4/5/6; Home/Delete/End/PageDown. The first four are available simultaneously; later clusters serve separately controlled hot seat. Physical keyboard rollover is hardware dependent.

Whole controller: left stick steers, RT accelerates, LT brakes/reverses. Shared controller: each owned stick X steers and Y drives its own truck relative to its forward direction. Opposite throttle brakes local forward/backward motion before reversing. No shoot, throw, pass or boost input. Escape/assigned Start pauses. Tab opens the full scorecard. Inactive individually assigned pads cannot steer the active hot-seat truck or confirm its dialogs. Mouse can operate human menus. Modal/reconnect/handoff confirmation and return to driving require neutral/fresh input. Assigned disconnect pauses all offline lanes, lists the affected names, and retains scores, motion, delivery clocks and pinsetter state.

## Scoring and pinsetter

Standard ten-pin scoring: ten frames, two deliveries except strikes, next two deliveries for strike bonuses and next one for spares. Tenth strike earns two fill deliveries; tenth spare earns one. After a tenth-frame strike and partial second delivery, the third delivery uses the remaining pins. After a second strike or an ordinary tenth spare, its next fill delivery receives a fresh rack. Maximum 300; completed games reject extra deliveries; tied highest totals are supported. `BowlingScore` is the sole score/rack authority. Reference: [USBC Keeping Score](https://bowl.com/keeping-score).

The truck crossing the foul line is expected, not a foot foul. X = strike, / = spare, − = miss. Cumulative totals remain … until their bonuses are resolved. Live unresolved totals are not ranked as final results. Full scorecard contains every delivery and cumulative frame total; final standings rank completed scores and retain tied ranks.

Pinfall is measured from actual Rigidbody pins after the truck reaches the end pit or a bounded genuine recovery, with motion retained until the result lock. A pin must remain down for at least 0.3 simulated seconds: up-vector dot world-up below 0.72 (about 44 degrees), root below -0.1, outside the deck sides, or beyond its rear. A touched pin or transient wobble is insufficient. Require at least 0.7 seconds settling plus 0.45 quiet seconds; an eight-second cap prevents indefinite resolution. Each pin is credited once per rack. Pinsetter action cannot add points after the result lock.

The visible sequence shows the result, lifts surviving pins, sweeps fallen pins and clears the truck, lowers preserved survivors or a fresh rack as required, then respawns on the approach. Preserved survivors keep their settled X/Z positions; the setter seats them upright at deck height before releasing physics. It explicitly synchronizes Rigidbody poses and clears machinery velocity. Each simultaneous lane runs its own sequence. Machinery uses DOTween; scaled delivery timers, physics and machinery pause together.

A committed gutter disables the truck's tire simulation/colliders and freezes its delivery. It cannot return or rebound into pins; already legitimate pin action may finish settling. Committed trucks get one pass: reversal behind furthest progress, overturned/outside trucks, low-speed creep and a bounded delivery deadline resolve the shot with any legal pinfall retained. A slow committed truck under 0.45 units/second resolves after 1.8 seconds. Approach deadline 40 seconds; rolling deadline 14; none advances while paused.

## Carry-through and AI polish — 2026-10-14

The abrupt stop was delivery policy: `EndDelivery` called `Clear`, zeroing velocities, making the chassis kinematic and disabling contacts on gutter/overturn/retreat/short timeout. Sideways tire scrub and opposite-input braking compounded it. There was no orientation-relative velocity cap. The settling clock was also counted in both FixedUpdate and Update.

A gutter now stays committed and physical, with pin contacts ignored after commitment and no inward rescue. Genuine pinfall before a late gutter retains the existing scoring rule. Ordinary delivery resolution starts after z > 67.4 in the pit; the truck is retired at result lock, after pin quiet checks. Sideways/backward control loss removes motor/brake/steer authority, reduces lateral scrub and keeps world momentum. Turning or temporarily overturning no longer ends a delivery. A genuine stationary body with little angular motion must stay below 0.45 for four seconds; escaped/invalid simulation and a 40-second limit remain bounded recovery paths. Settling uses one clock.

The same human/AI world-forward assist is at most 0.55 m/s², fades toward 13 m/s, requires two grounded wheels and positive down-lane motion, and stops three units before the head pin. A physical box query blocks it at obstacles; saturation falls back to a complete query. It adds no lateral aiming or pin impulse. Existing edge assistance stops on gutter commitment.

AI aims at a pocket-side full-rack line or a reachable remaining-pin cluster instead of the empty center of a split. Entry alignment is earlier and less abrupt; late corrections fade with oil and coast when the projected line is already useful. Easy/Normal/Hard aim errors are 0.34/0.07/0.03, reactions 0.28/0.09/0.06 seconds, and entry-speed targets 12.7/15.2/15.7 with small variation. Normal uses a safer spare line and less heading noise. All plans use ordinary controls and the same physics; no score/pin injection or live navigation teleport. Observed distributions and gutter/spare rates are in TruckSports/VALIDATION.md.

## Handling, cab and scale

NWH WheelController 3D owns tire, suspension, traction and physical response; bowling sets its public torque, brake, steer and friction APIs. All humans and AI use identical physics and edge assistance. NWH/GR3D geometry is retained for the tiny truck. No extra truck-to-pin or pin-to-pin impact impulse is applied. Contact diagnostics include NWH tire ground-contact reactions as well as chassis collisions.

Four lanes have grippy approaches, maple planks, aiming arrows, foul lines, lowered gutters, pin decks, pits, return equipment, overhead pinsetter structures, pickup plates and sweep bars. World units are enlarged for stable toy-scale physics: lane width 4.2, pin line 60, approach 20, pin height 1.42; chassis width 0.7, length 1.2, height 0.36. Rack spacing 1.08 means the truck cannot cover the entire rack.

Default perspective camera remains in the cab, with a clear windshield opening, dashboard, gauges, steering wheel and trim. Near clip 0.015, field of view 72. Only its own exterior is hidden during its camera pass; split-screen cameras do not hide another truck's exterior. Only one AudioListener is active. Reduced Camera Motion limits yaw follow to 180 rather than 540 degrees/second and reduces restrained impact shake to 12%; gravity-up rendering avoids violent roll/pitch.

Editable `BowlingHandling` tuning: approach grip 1.4 to end grip 0.10; oil exponent 1.25; motor torque 3.6, brake torque 9, speed governor 17; steering 26 degrees on approach to 7 at the end; lateral stiffness 0.8 and steering/speed-dependent rear slip 0.24. Furthest lane progress controls slipperiness, so backing up does not restore grip. After commitment propulsion/braking fade from 10% to 0.8% of normal authority. Entry speed and momentum dominate near the rack.

Separate edge assist starts within 0.6 of the edge, gently counters outward velocity and applies inward acceleration capped at 2.0. It reads only lane center/edges and truck motion, never remaining pins or desired score. It stops on gutter commitment and never applies force to pins. Poor approaches still gutter. AI selects a line from remaining pins, controls the actual truck, and uses yaw-rate damping for NWH steering. Difficulty changes speed, target precision, steering error and reaction. Approach recovery reverses through controls; live delivery navigation does not teleport, assign scores, add grip or apply hidden pin impulses.

Shared Master/SFX/vibration settings are reused. NWH truck-running/skid clips, Heat click audio, and owned pin/strike/spare/mechanical cues feed shared feedback. Haptics go only to the relevant assigned pad and stop on pause, disconnect, unload and focus loss. Audio listening/physical rumble remain human checks.

## Build and validation

Final build method: `LWS.TruckSports.Editor.SportsBuilder.Build`. Explicit scene list: TruckSports, TruckBilliards, TruckHockey, TruckBowling. Output: `Builds/TruckSports/TruckSports.exe`, all generated companion files required. Scoped host settings restoration is verified on success and deliberately thrown failure. `CreateValidateBuild` reauthors scenes and is not a validation-only command; use saved scenes for delivery. Bowling's `Create` authors its own scene and targets the front-end additions; regeneration defaults preserve Bowling and existing 3v3 integration.

Opt-in development probes: `-tsBowlingQuick`, `-tsBowlingUpdate`, `-tsBowlingFourAI`, `-tsBowlingEightAI`. Normal launch runs none. Rule/ownership fixtures run during editor validation. Physical matches and injected initial-layout/device checks are distinguished in `VALIDATION.md`. Generated results/captures are under Logs, excluded from Git and the packaged player payload.

## Vendor report

VENDOR ASSETS AUDITED: NWH Vehicle Physics 2 input/Arcade APIs and GR3D truck; NWH WheelController 3D WheelUAPI/Friction/Spring and CarController demo; installed asset names for bowling/pinsetter content; Heat controls/audio; shared Truck Sports input/settings/navigation; DOTween; Input System/TMP/uGUI/URP.
VENDOR ASSETS USED: `Assets/NWH/WheelController/Scripts/WheelController.cs` and `WheelController.UAPI.cs` (MotorTorque, BrakeTorque, SteerAngle, LateralFrictionGrip, LongitudinalFrictionGrip, LateralFrictionStiffness); vendor default friction preset; `Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab` geometry; NWH `EngineRunning - Truck4.wav`/`AsphaltSkidNoise.wav`; Heat ButtonManager/InputFieldManager/click; DOTween DOMoveY/DOMoveZ and result DOScale; Input System 1.19, TMP/uGUI/URP.
RELEVANT ASSETS NOT USED: Full road powertrain/Arcade drift assist is unnecessary for a single-delivery miniature truck using the same NWH tire solver directly. UTS road paths do not supply remaining-pin targeting. No bowling machinery/pin asset was found. Wheel hardware, road/world/weather authoring and online services are outside this offline sport.
CUSTOM SYSTEMS CREATED: Bowling score/rack semantics, lane/delivery coordination, remaining-pin AI input policy, pinfall/pinsetter sequencing, cab view, alley/pin geometry, participant/device adapters, presentation and focused validation.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Pin trampoline back wall — October16,2026

The existing pit bumper is now at67m instead of69m, with a visible spring bed, eight reused modeled tension springs per lane and a distinct existing Cornhole launch cue through the bounded shared feedback voices. Rear pin-area side containment remains physical and lane-specific. The truck receives ordinary PhysX wall contact; no enhanced response is applied to trucks and no handling/grip/motor changes were made.

A qualifying real pin/front-wall impact uses the cached incoming world velocity and actual contact normal. Apply only the difference between the post-PhysX velocity and one bounded desired return. Reverse the down-lane component strongly, retain94% useful lateral velocity(subject to5.5m/s lateral cap), limit vertical motion near pin height and allow a small bounded lift from the lower pit. Outgoing speed<=11.5m/s; outgoing squared speed<=1.35*incoming squared speed+4. Preserve natural tumbling without a second custom spin impulse. Per-pin0.55s cooldown and Enter-only handling prevent resting-contact boosts.

Pins registering down for0.3s remain down for that delivery even if they later stand upright. Reset/release clears that latch appropriately. Finalization requires at least0.9s and0.8s quiet, plus no wall return in the last0.9s; a12s settling maximum fairly locks the current actual/latching state for pathological persistent motion. The sweeper remains overhead during live ricochets. Existing first/second-ball, rack, strike/spare and tenth rules remain authoritative. Physical low pit arrival can hand off to settling without clearing momentum; no arbitrary early body freeze was added.
