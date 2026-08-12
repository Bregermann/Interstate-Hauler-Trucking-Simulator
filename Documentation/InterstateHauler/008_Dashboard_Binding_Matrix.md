# Prompt 008 Dashboard Binding Matrix

| Dashboard Element | Source Object | Gameplay State Source | Binding Component | Display Range/State | Working | Production Ready | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Speedometer | `SpeedGaugeAnalog` | `LwsNwhVehicleAdapter.ReadTelemetry().signedSpeedMetersPerSecond` | `LwsTruckDashboardController` -> NWH `AnalogGauge.Value` | 0-100 MPH | Yes, code-bound | Needs Editor visual pass | Uses MPH for contiguous-USA baseline. |
| Tachometer | `RPMGaugeAnalog` | `LwsNwhVehicleAdapter.ReadTelemetry().engineRpm` | `LwsTruckDashboardController` -> NWH `AnalogGauge.Value` | 0-3000 RPM | Yes, code-bound | Needs Editor visual pass | No fake RPM. |
| Gear display | `GearGaugeDigital` | `Lws18SpeedTransmissionController.DisplayState.displayLabel` | `LwsTruckDashboardController` -> NWH `DigitalGauge.stringValue` | `N`, `R`, `LO-L`, `LO-H`, `1L`..`8H` | Yes, code-bound | Needs legibility check | Does not distort Prompt 006 gear state. |
| Requested range | LWS auxiliary status/debug | `LwsTransmissionDisplayState.requestedRange` | `LwsTruckDashboardSnapshot` / debug panel | Low/High | State available | No | Physical art deferred. |
| Engaged range | LWS auxiliary status/debug | `LwsTransmissionDisplayState.engagedRange` | `LwsTruckDashboardSnapshot` / debug panel | Low/High | State available | No | Prevents preselect misrepresentation. |
| Requested splitter | LWS auxiliary status/debug | `LwsTransmissionDisplayState.requestedSplitter` | `LwsTruckDashboardSnapshot` / debug panel | Low/High | State available | No | Physical art deferred. |
| Engaged splitter | LWS auxiliary status/debug | `LwsTransmissionDisplayState.engagedSplitter` | `LwsTruckDashboardSnapshot` / debug panel | Low/High | State available | No | Physical art deferred. |
| Left signal | `Left Blinker` | `LwsTruckControlState.turnSignal/hazardsOn` | `LwsTruckDashboardController` -> NWH `DashLight.Active` | Blinking on/off | Yes, code-bound | Needs Editor visual pass | Hazards flash both lamps. |
| Right signal | `Right Blinker` | `LwsTruckControlState.turnSignal/hazardsOn` | `LwsTruckDashboardController` -> NWH `DashLight.Active` | Blinking on/off | Yes, code-bound | Needs Editor visual pass | Shared blink timing. |
| High beam | `High Beam` | `LwsTruckControlState.highBeamsOn` | `LwsTruckDashboardController` -> NWH `DashLight.Active` | Off/on | Yes, code-bound | Needs Editor visual pass | No raw input read. |
| Check engine | `Check Engine` | `engineStalled` / severe transmission abuse | `LwsTruckDashboardController` -> NWH `DashLight.Active` | Off/on warning | Partial | No | Future engine damage/fault system should own final semantics. |
| Ignition | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.ignitionState` | Runtime TextMesh indicator | Off/Electrical/EngineRunning | Yes, development-bound | No | Final physical switch/lamp art deferred. |
| Engine running | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.engineRunning` | Runtime TextMesh indicator | Off/run | Yes, development-bound | No | Placeholder physical status text. |
| Parking brake | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.parkingBrakeOn` | Runtime TextMesh indicator | Off/on | Yes, development-bound | No | No suitable native lamp confirmed. |
| Engine/Jake brake | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.engineBrakeLevel` | Runtime TextMesh indicator | Level 0+ | State available | No | Starter truck does not support Jake brake. |
| Retarder | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.retarderLevel` | Runtime TextMesh indicator | Level 0+ | State available | No | Starter truck does not support retarder. |
| Differential lock | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.differentialLocked` | Runtime TextMesh indicator | Off/on | Yes, development-bound | No | Prompt 007 uses reversible NWH slip-torque approximation. |
| Cruise | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.cruiseEnabled/target` | Runtime TextMesh indicator | Off/on + target MPH | Yes, development-bound | No | LWS fallback/native-if-present from Prompt 007. |
| Trailer state | `IH_DashboardAuxStatusPanel` | `LwsTruckControlState.trailerAttached` / telemetry | Runtime TextMesh indicator | Attached/detached | Yes, development-bound | No | Advanced trailer warnings deferred. |
| Wipers | None confirmed | `LwsTruckControlState.wiperState` | Snapshot/debug only | Off/intermittent/low/high | State only | No | Art/control hook deferred. |
| Horn/air horn | None visual | `LwsTruckControlState.hornActive/airHornActive` | Snapshot/debug only | Held/released | State only | No | Prompt 044 audio owns final behavior. |
| Steering wheel | `steering wheel` | NWH steering telemetry through `LwsNwhVehicleAdapter` | `LwsTruckDashboardController` transform rotation | +/- 450 degrees visual | Yes, code-bound | Needs Editor visual pass | Reflects gameplay steering, not raw G29 input. |
