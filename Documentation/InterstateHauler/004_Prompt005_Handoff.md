# Interstate Hauler - Prompt 005 Handoff

Expected Prompt 005: Logitech Wheel / Pedals / H-Shifter / FFB. Do not begin Prompt 005 from this document alone.

## Baseline Assets

- Player truck prefab: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`
- Trailer prefab: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab`
- Truck definition: `Assets/LWS/InterstateHauler/Vehicles/Data/IH_TruckDefinition_StarterNwhSemi.asset`
- Validation scene: `Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`
- Source NWH tractor: `Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab`
- Source NWH trailer: `Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab`

## Current Input Architecture

Temporary input source:

- `NWH.VehiclePhysics2.Input.InputSystemVehicleInputProvider`
- `NWH.Common.Input.InputSystemSceneInputProvider`

Existing LWS input seam:

- `ILwsVehicleInputSource`
- `LwsNwhVehicleInputProvider`
- `ILwsVehicleInputService`

Prompt 005 should decide whether to route wheel input through `LwsNwhVehicleInputProvider` immediately or keep NWH native input active for first hardware bring-up.

## Exact NWH Public Hooks

Read/write state:

- `VehicleController.input.Throttle`
- `VehicleController.input.Brakes`
- `VehicleController.input.Steering`
- `VehicleController.input.Clutch`
- `VehicleController.input.Handbrake`
- `VehicleController.input.ShiftInto`
- `VehicleController.input.ShiftUp`
- `VehicleController.input.ShiftDown`
- `VehicleController.input.TrailerAttachDetach`

Direct transmission:

- `VehicleController.powertrain.transmission.ShiftInto(int targetGear, bool instant = false)`
- `VehicleController.powertrain.transmission.Gear`
- `VehicleController.powertrain.transmission.GearName`

NWH stock H-shifter action names:

- `ShiftIntoR1`
- `ShiftInto0`
- `ShiftInto1`
- `ShiftInto2`
- `ShiftInto3`
- `ShiftInto4`
- `ShiftInto5`
- `ShiftInto6`
- `ShiftInto7`
- `ShiftInto8`

## Wheel / FFB Status

Optional NWH steering wheel / FFB package status was not confirmed in Prompt 004. Prompt 005 should inspect NWH optional packages and licensing/import requirements before implementing FFB.

## Control Conflicts

Current validation scene has stock NWH Input System provider active. Prompt 005 must avoid double-feeding NWH input from both the stock provider and the LWS wheel provider.

Camera/control focus currently relies on NWH `CameraChanger` and `InputSystemSceneInputProvider`.

## Blockers

Manual normal Unity Editor verification is still required for `TruckValidation.unity` driving, coupling, scene reload, and console health.
