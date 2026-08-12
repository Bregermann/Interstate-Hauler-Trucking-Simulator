# Prompt 007 Truck Control Matrix

| Control | Semantic Action | Command Type | NWH Native | LWS Adapter | Keyboard | Controller | Wheel | Dashboard State | Production Ready | Notes |
|---|---|---:|---|---|---|---|---|---|---|---|
| Ignition toggle | ignitionToggle / ignition | Toggle | Partial | Yes | I | Deferred default | Bindable | ignitionState | Partial | NWH has engine ignition/running, not full electrical sim. |
| Engine start | engineStart | One-shot | Yes | Yes | E | Bindable | Bindable | engineRunning | Partial | Uses `EngineComponent.StartEngine()`. |
| Engine stop | engineStop | One-shot | Yes | Yes | Shift+E | Bindable | Bindable | engineRunning | Partial | Uses `EngineComponent.StopEngine()`. |
| Service brake | brake | Continuous | Yes | Existing | S / Down | LT | Pedal | serviceBrakeInput | Yes | Prompt 004/005 continuous input. |
| Parking brake | parkingBrakeToggle | Toggle | Yes | Yes | P | Button East | Bindable | parkingBrakeOn | Yes | NWH receives `Handbrake()`. |
| Trailer brake | trailerBrake | Held | Not found | State only | Space | Bindable | Bindable | trailerBrakeHeld | No | Independent NWH trailer brake not discovered. |
| Engine/Jake brake | engineBrake | Toggle/Multi-state | Not found | Capability only | M/PageUp/PageDown | Bindable | Bindable | engineBrakeLevel | No | Deferred until real truck/NWH API exists. |
| Retarder | retarderIncrease/decrease | Multi-state | Not found | Capability only | Home/End | Bindable | Bindable | retarderLevel | No | Deferred. |
| Headlights | lowBeamLights | Toggle | Yes | Yes | L | Button North | Bindable | headlightsOn | Yes | NWH light pulse. |
| High beams | highBeamLights | Toggle | Yes | Yes | K | Button West | Bindable | highBeamsOn | Yes | NWH light pulse. |
| Left signal | leftIndicator | Toggle | Yes | Yes | Z | Left shoulder | Bindable | turnSignal | Yes | Opposite signal cancels. |
| Right signal | rightIndicator | Toggle | Yes | Yes | X | Right shoulder | Bindable | turnSignal | Yes | Opposite signal cancels. |
| Hazards | hazardLights | Toggle | Yes | Yes | J | Left stick button | Bindable | hazardsOn | Yes | Clears directional signal state. |
| Wipers | wipers / wiperIncrease / wiperDecrease | Multi-state | Not found | State only | V | D-pad up | Bindable | wiperState | No | Visual animation deferred. |
| Horn | horn | Held | Yes | Yes | H | Button South | Bindable | hornActive | Partial | Uses NWH horn path. |
| Air horn | airHorn | Held | Partial | Yes | B | Right stick button | Bindable | airHornActive | Partial | Shares NWH horn path on selected semi. |
| Differential lock | differentialLock | Toggle | Partial | Yes | O | Bindable | Bindable | differentialLocked | Partial | Uses reversible differential slip-torque override. |
| Cruise toggle | cruiseControl | Toggle | Partial | Yes | C | D-pad right | Bindable | cruiseEnabled | Partial | Native if wrapper exists, otherwise LWS throttle/brake assist. |
| Cruise set | cruiseSet | One-shot | Partial | Yes | R | Bindable | Bindable | cruiseTargetSpeedMetersPerSecond | Partial | Captures current speed or minimum validation speed. |
| Cruise resume | cruiseResume | One-shot | Partial | Yes | Shift+R | Bindable | Bindable | cruiseEnabled | Partial | Resumes prior target. |
| Cruise cancel | cruiseCancel | One-shot | Partial | Yes | Backspace | D-pad left | Bindable | cruiseEnabled | Yes | Also cancels on brake/clutch. |
| Cruise increase | cruiseIncrease | One-shot | Partial | Yes | = | Bindable | Bindable | cruiseTargetSpeedMetersPerSecond | Partial | 1 mph step. |
| Cruise decrease | cruiseDecrease | One-shot | Partial | Yes | - | Bindable | Bindable | cruiseTargetSpeedMetersPerSecond | Partial | 1 mph step. |
| Trailer attach/detach | trailerAttachDetach | One-shot | Yes | Yes | T | D-pad down | Bindable | trailerAttached | Yes | Uses Prompt 004 coupling adapter. |
| Camera cycle | cameraCycle | One-shot | Yes | Yes | Tab | Select | Bindable | cameraCycleRequested | Partial | Calls `CameraChanger.NextCamera()`. |
| Look reset | lookReset | One-shot | Not found | Seam | Backquote | Bindable | Bindable | lookResetRequested | No | No stable selected-camera reset API found. |
| Flip off driver | flipOffDriver | One-shot | No | Yes | F | Chord default | Bindable | lastGesture | Partial | Event/target/animation hooks exist. |
| Interact | interact | One-shot | No | Input seam | Enter | Button South | Bindable | Event seam | Partial | Future interaction system. |
| Pause | pause | One-shot | No | Input seam | Esc | Start | Bindable | Input seam | Partial | Future UI. |
| Menu navigation | navigateUp/Down/Left/Right | One-shot | No | Input seam | Arrows | D-pad | D-pad | Input seam | Partial | Heat UI integration later. |
