# Interstate Hauler - Prompt 004 Truck Feature Matrix

| Feature | NWH Native | Selected Truck Supports | LWS Adapter Exists | Production Ready | Future Prompt | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Steering | Yes | Yes | Yes | No | 005 | NWH owns wheel/steering physics; LWS exposes normalized steering input/state. |
| Throttle | Yes | Yes | Yes | No | 005 | Temporary NWH Input System provider drives it. |
| Service brake | Yes | Yes | Yes | No | 005 | NWH `Brakes` and input states exposed through telemetry. |
| Parking brake | Yes | Yes | Yes | No | 005 | NWH `Handbrake`/`handbrakeValue` exposed. |
| Clutch | Yes | Yes | Yes | No | 005 | Exposed through `VehicleInputHandler.Clutch` and NWH clutch component. |
| Automatic | Yes | Yes | Partial | No | 006 | Selected truck serialized as NWH automatic baseline. |
| Sequential | Yes | Likely | Partial | No | 006 | NWH has shift up/down hooks; not tuned for trucking yet. |
| Manual | Yes | Likely | Partial | No | 006 | NWH `TransmissionShiftType.Manual` exists; not enabled as final behavior. |
| H-pattern | Yes | Input hooks exist | Partial | No | 005 | NWH `ShiftIntoR1`, `ShiftInto0`, and `ShiftInto1`-`ShiftInto8` actions exist. |
| Engine stall | Yes | Yes | Yes | No | 006 | `EngineComponent.IsStalled` exposed. |
| Engine brake | Unknown/partial | Not confirmed | Input intent only | No | 006 | LWS input command shape includes it, but Prompt 004 did not confirm NWH module. |
| Retarder | Unknown/partial | Not confirmed | Input intent only | No | 006 | LWS command shape includes it for future mapping. |
| Differential | Yes | NWH drivetrain has differential components | No | No | 006 | Do not duplicate NWH drivetrain. |
| Cruise control | Yes | Module/API present in NWH package | Input intent only | No | 006 | NWH input provider exposes `CruiseControl()`. |
| Lights | Yes | Yes | Partial | No | 007 | NWH truck and trailer light data present. |
| High beams | Yes | Yes | Input route exists | No | 007 | NWH input provider exposes high beams. |
| Signals | Yes | Yes | Input route exists | No | 007 | NWH blinkers and dash indicators present. |
| Hazards | Yes | Yes | Input route exists | No | 007 | NWH input provider exposes hazards. |
| Wipers | Not confirmed | Missing | Input intent only | No | 007 | Dashboard/wiper production behavior deferred. |
| Horn | Yes | Likely | Input route exists | No | 005 | NWH input provider exposes horn. |
| Mirrors | Render-texture setup | Yes, two mirrors | No quality controller | No | 008 | Two mirror cameras/render textures require URP visual and performance validation. |
| Trailer coupling | Yes | Yes | Yes | No | 004/005 | LWS observes NWH attach/detach events and state. |
| Landing gear | Yes/partial | Trailer jacks object exists | No | No | 007 | NWH trailer stand reference exists; no player interaction yet. |
| Trailer braking | Yes | Yes | Telemetry indirect | No | 007 | NWH trailer copies input states while attached. |
| Trailer lights | Yes | Yes | No gameplay adapter | No | 007 | NWH trailer light data includes brake/tail/blinkers. |
| Damage | Yes | Yes | No LWS gameplay layer | No | Later | NWH damage data exists; gameplay damage/wear deferred. |
| Tire friction | Yes | Yes | No tuning adapter | No | Later | NWH wheel physics owns tire behavior. |
| Saveable runtime state | Partial | Shape defined | Yes, shape only | No | Later | LWS state payload defined; no full save/load implementation. |
