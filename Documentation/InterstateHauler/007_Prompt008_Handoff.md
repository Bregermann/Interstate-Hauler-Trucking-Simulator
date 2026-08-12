# Prompt 008 Handoff - Dashboard + Mirrors

Player truck prefab:

`Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`

Validation scene:

`Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`

Prompt 008 should consume truck-control state from:

- `ILwsTruckControlService.ActiveState`
- `LwsTruckControlController.CurrentState`
- `Lws18SpeedTransmissionController.DisplayState`

Dashboard state available:

- ignition state
- engine running/stalled
- parking brake
- service brake
- trailer brake intent
- headlights/high beams
- turn signal left/right/off
- hazards
- wiper state
- horn/air horn active
- engine brake level
- retarder level
- differential lock
- cruise enabled
- cruise target speed
- trailer attached/id
- transmission display label, shift state, range/splitter, rejection/abuse state

Selected NWH cab/interior objects observed in the source prefab include:

- `Cab`
- `interior`
- `steering wheel`
- `DashInstruments`
- `Dash Lights`
- `SpeedGaugeAnalog`
- `RPMGaugeAnalog`
- `GearGaugeDigital`
- `Left Blinker`
- `Right Blinker`
- `Check Engine`
- `High Beam`
- `TCS`
- mirror glass objects
- `RenderTextureMirrorCameraL`
- `RenderTextureMirrorCameraR`

Current mirror setup:

- left and right mirror render-texture cameras exist on the selected NWH semi
- Prompt 003 mirror quality semantics still need production integration
- Prompt 008 should inspect URP rendering, RenderTexture size, update rate, culling, and Steam Deck cost

Missing or deferred dashboard/cab work:

- wiper visual animation
- separate air horn indicator/audio
- engine/Jake brake visual/sound
- retarder visual/sound
- independent trailer brake indicator
- look-reset camera hook
- final production switch animations and indicator lamps

Prompt 008 should not read hardware APIs or NWH input provider state directly. Bind cab/dashboard behavior to LWS semantic state.
