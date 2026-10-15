# Truck Dock Diving

1–8 competitors rotate through three attempts. Longest valid jump wins. Solo keeps a personal best. Tied leaders take extra jumps until resolved.

Drive the dock with your assigned keyboard cluster or controller. ACTION jumps (per-slot key shown in setup; whole pad South; shared-left LT / shared-right RT). A press near the edge is strongest; early/late is weaker and missing it gives a weak automatic leap. Release range input before bounded aftertouch.

Distance is forward from the edge to the Rigidbody center at first water-volume contact, rounded once to whole inches. That same integer is used for display, records and ties. Outside-pool center or solid landing is a foul. Cab/chassis separation is visual over one authoritative body. Every attempt records mesh-only replay poses: camera, rate0.25×–4×, rewind and skip; no physics or score rerun.

Pause and source disconnect preserve live physics, attempts and scores. Reconnect the same device or explicitly assign a replacement, release controls, then freshly confirm. Rematch retains setup. Shared audio/steering/vibration/display settings apply. No online services.

Build through LWS.TruckSports.Editor.SportsBuilder.Build with the explicit saved Sports scenes. Scene regeneration uses this sport’s owned Editor authoring utility and shared SportsEventAuthoring.

Vendor foundation: NWH/GR3D truck and NWH water-plane geometry; WheelController driving; Heat/uGUI/TMP; Cinemachine; Weather Maker splash audio/water-normal texture; UTS spectator meshes/applause animation. Crowd is scene-owned render/animation only; vendor NPC/ragdoll behavior is stripped from instances.

Validation and remaining human/device checks: see Assets/TruckSports/VALIDATION.md and Documentation/TruckSports/ExpansionCheckpoint.md. Development smoke runners execute only with their explicit validation flags and are inert in normal play. No physical-controller or human playtest acceptance is claimed.
