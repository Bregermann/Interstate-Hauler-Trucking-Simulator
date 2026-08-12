# Prompt 007 NWH Control API Matrix

| Feature | NWH Class | NWH Property/Method | Native Support Level | LWS Adapter | Known Limitation |
|---|---|---|---|---|---|
| Engine ignition | `EngineComponent` | `ignition` | Partial | `LwsNwhTruckControlAdapter` | No full electrical accessory sim. |
| Engine start | `EngineComponent` | `StartEngine()` | Native | `LwsNwhTruckControlAdapter` | Start-condition depth remains NWH-owned. |
| Engine stop | `EngineComponent` | `StopEngine()` | Native | `LwsNwhTruckControlAdapter` | None for baseline. |
| Engine running state | `EngineComponent` | `IsRunning` | Native | `LwsNwhTruckControlAdapter` | None for baseline. |
| Engine stalled state | `EngineComponent` | `IsStalled` | Native | `LwsNwhTruckControlAdapter` | Used as readback only. |
| Parking brake | `VehicleInputProviderBase` / `VehicleInputHandler` | `Handbrake()` / `input.Handbrake` | Native | `LwsNwhVehicleInputProvider` | LWS owns toggle state. |
| Low beams | `VehicleInputProviderBase`, `LightsMananger` | `LowBeamLights()`, `lowBeamLights` | Native | `LwsNwhVehicleInputProvider` | NWH consumes one-shot pulse. |
| High beams | `VehicleInputProviderBase`, `LightsMananger` | `HighBeamLights()`, `highBeamLights` | Native | `LwsNwhVehicleInputProvider` | NWH forces low beams on with high beams. |
| Left signal | `VehicleInputProviderBase`, `LightsMananger` | `LeftBlinker()`, `leftBlinkers` | Native | `LwsNwhVehicleInputProvider` | Automatic steering cancellation not integrated. |
| Right signal | `VehicleInputProviderBase`, `LightsMananger` | `RightBlinker()`, `rightBlinkers` | Native | `LwsNwhVehicleInputProvider` | Automatic steering cancellation not integrated. |
| Hazards | `VehicleInputProviderBase`, `LightsMananger` | `HazardLights()` | Native | `LwsNwhVehicleInputProvider` | LWS clears directional state when enabled. |
| Horn | `VehicleInputProviderBase`, `HornComponent` | `Horn()` | Native | `LwsNwhVehicleInputProvider` | One NWH horn path only. |
| Air horn | None confirmed | None confirmed | Partial | `LwsTruckControlController` | Shares NWH horn path for selected semi. |
| Wipers | None confirmed on selected semi | None confirmed | Not available | `LwsTruckControlState` | Semantic state only. |
| Engine/Jake brake | None confirmed | None confirmed | Not available | Capability model | Deferred. |
| Retarder | None confirmed | None confirmed | Not available | Capability model | Deferred. |
| Differential lock | `DifferentialComponent` | `slipTorque` | Partial | `LwsNwhTruckControlAdapter` | Reversible approximation, not dedicated API. |
| Cruise control | `CruiseControlModule` | `cruiseControlActive`, `targetSpeed` | Partial | `LwsNwhTruckControlAdapter` plus LWS fallback | Selected semi lacks confirmed wrapper. |
| Trailer attach/detach | `TrailerHitchModuleWrapper` | Existing hitch module | Native | `LwsNwhTrailerCouplingAdapter` | Requires physical positioning. |
| Trailer brake | Trailer module/input copy | None confirmed independent | Not available | Capability model | Manual trailer-only brake deferred. |
| Camera cycle | `CameraChanger` | `NextCamera()` | Native | `LwsNwhTruckControlAdapter` | Baseline only. |
| Look reset | None confirmed | None confirmed | Not available | Semantic seam | Prompt 008/camera polish should implement. |
| Transmission shifting | `TransmissionComponent` | `ShiftInto(int,bool)` | Native | Prompt 006 only | Prompt 007 intentionally does not call it. |
