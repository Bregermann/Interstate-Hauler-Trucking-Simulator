# Prompt 013 - Road Condition Test Matrix

| Area | Test | Automated | Manual Editor | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| Compile | Runtime assembly builds | Yes | n/a | Pass | `dotnet build LWS.InterstateHauler.Runtime.csproj --no-restore` completed with 0 errors. |
| Compile | Editor validator assembly builds | Yes | n/a | Pass | `dotnet build LWS.InterstateHauler.Editor.csproj --no-restore` completed with 0 errors. |
| Compile | EditMode test assembly builds | Yes | n/a | Pass | `dotnet build LWS.InterstateHauler.Tests.EditMode.csproj --no-restore` completed with 0 errors. |
| Compile | PlayMode test assembly builds | Yes | n/a | Pass | `dotnet build LWS.InterstateHauler.Tests.PlayMode.csproj --no-restore` completed with 0 errors. |
| Simulation | Dry > Wet > Snow > Ice grip hierarchy | Yes | n/a | Pass | Covered by `LwsRoadConditionEditModeTests`. |
| Simulation | Rain accumulates gradually | Yes | n/a | Pass | Light and heavy rain are compared. |
| Simulation | Wetness dries when rain stops | Yes | n/a | Pass | Dry/warm weather reduces wetness. |
| Simulation | Snow accumulates and melts | Yes | n/a | Pass | Cold snow and warm melt path covered. |
| Simulation | Ice needs moisture/freezing temperature | Yes | n/a | Pass | Cold dry road does not form meaningful ice. |
| Simulation | Ice melts when warm | Yes | n/a | Pass | Warm weather reduces ice. |
| IDs/state | Stable road IDs and clamping | Yes | n/a | Pass | Snapshot `StateKey` and value clamping covered. |
| Authority | Core service avoids vendor type leakage | Yes | n/a | Pass | EditMode source guard checks core road-condition file. |
| Bootstrap | Road condition service registered once | Yes | n/a | Pass | PlayMode assembly includes service registration tests. |
| Adapters | Forced road condition reaches visual/physics adapters | Yes | n/a | Pass | Uses fake adapters so hardware/vendor visuals are not required. |
| Weather flow | Weather service rain drives road wetness | Yes | n/a | Pass | Uses semantic `ILwsWeatherService` snapshot. |
| Weatherade visuals | Wet/puddle/snow visual coverage | No | Required | Pending | Requires normal Editor visual check under URP. |
| NWH physics | Dry/wet/snow/ice truck handling | No | Required | Pending | Requires driving the NWH player truck in TruckValidation. |
| Trailer physics | Attached trailer grip/stability | No | Required | Pending | Requires normal Editor coupling and driving validation. |
| Debug | Force modes, temp overrides, accumulation speed | No | Required | Pending | Exposed in `LwsRoadConditionDebugPanel`. |
| Validator | `Interstate Hauler / Validate Project` Prompt 013 checks | Partly | Required | Pending | Validator code compiles; menu run remains normal Editor validation. |

## Known Validation Caveat

Generated `.csproj` builds are useful for Prompt 013 compile confidence, but normal Unity Editor validation remains authoritative for vendor runtime behavior and visual rendering.
