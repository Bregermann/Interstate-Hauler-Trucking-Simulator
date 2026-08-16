# Prompt 013 - NWH Surface API Matrix

| Feature | NWH Class | NWH Property/Method | LWS Adapter | Support Level | Notes |
| --- | --- | --- | --- | --- | --- |
| Player vehicle root | `NWH.VehiclePhysics2.VehicleController` | `powertrain` | `LwsNwhRoadConditionAdapter` | Native | Active player truck is resolved through LWS player vehicle service. |
| Wheel list | `NWH.VehiclePhysics2.Powertrain.Powertrain` | `wheels` | `LwsNwhRoadConditionAdapter` | Native | Enumerated on tractor and attached trailer where available. |
| Wheel component | `NWH.VehiclePhysics2.Powertrain.WheelComponent` | `wheelUAPI` | `LwsNwhRoadConditionAdapter` | Native | Runtime wheel API entry point. |
| Longitudinal grip | `NWH.Common.Vehicles.WheelUAPI` | `LongitudinalFrictionGrip` | `LwsNwhRoadConditionAdapter` | Native | Multiplied by road condition and braking grip scalar. |
| Lateral grip | `NWH.Common.Vehicles.WheelUAPI` | `LateralFrictionGrip` | `LwsNwhRoadConditionAdapter` | Native | Multiplied by road condition lateral scalar. |
| Rolling resistance | `NWH.Common.Vehicles.WheelUAPI` | `RollingResistanceTorque` | `LwsNwhRoadConditionAdapter` | Native | Multiplied from dry baseline. |
| Slip telemetry | `NWH.Common.Vehicles.WheelUAPI` | `LongitudinalSlip`, `LateralSlip`, normalized slip values | Deferred | Native | Audited as available, not used in Prompt 013 tuning. |
| Rolling helper | `WheelComponent` | `ApplyRollingResistanceMultiplier(float)` | Deferred | Native | Direct baseline-safe torque assignment was chosen for restore clarity. |
| Player trailer detection | LWS trailer identity plus NWH controller | `VehicleController` on trailer identity | `LwsNwhRoadConditionAdapter` | Partial | Applies when attached trailer identity resolves to an NWH controller. |
| UTS traffic | n/a | n/a | none | Excluded | Prompt 013 must not alter UTS traffic physics. |
| Material/surface table | NWH surface config | none used | none | Not used | Prompt 013 uses LWS semantic road conditions instead of vendor surface assets. |
| Hydroplaning | n/a | none identified | LWS snapshot scalar | LWS Required | Represented through standing-water risk and grip reduction. |
| Dry restoration | `WheelUAPI` | same grip/rolling fields | `LwsNwhRoadConditionAdapter` | LWS Required | Dry baselines are cached per wheel and restored on disable/reset. |

## Integration Notes

The NWH adapter is the only Prompt 013 file that imports NWH namespaces. LWS gameplay consumes `LwsRoadConditionSnapshot`, not NWH tire implementation details.
