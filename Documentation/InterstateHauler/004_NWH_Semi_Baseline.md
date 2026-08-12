# Interstate Hauler - NWH Semi Baseline

Prompt: 004 - NWH Semi Baseline  
Date: 2026-08-11  
Unity: 6000.4.10f1  
URP: 17.4.0  
Color space: Linear  
Validation authority: normal Unity Editor

## 1. Selected NWH Tractor

Selected tractor:

`Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab`

Installed NWH version:

`NWH Vehicle Physics 2` package version `13.6`, confirmed from the selected prefab `.meta`.

Why selected:

- It is the installed semi tractor example, not a guessed sample.
- It already uses `NWH.VehiclePhysics2.VehicleController`.
- It includes the NWH heavy-vehicle drivetrain, wheel, suspension, brake, light, damage, trailer hitch, and camera modules.
- It has cab/interior assets and dashboard instruments suitable for cockpit-first validation.
- It has a matching installed semi trailer prefab using NWH trailer physics.
- It carries less gameplay-demo baggage than importing a full vendor scene.

Prompt 004 does not alter the vendor prefab. The project-owned player prefab is:

`Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`

This is a lightweight LWS prefab variant of the selected NWH tractor. Runtime LWS identity/adapters are added by the validation spawner.

## 2. Selected NWH Trailer

Selected trailer:

`Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab`

Project-owned validation trailer prefab:

`Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab`

This is the Prompt 004 dry-van stand-in. It should not be treated as final production 53 ft dry-van art.

Known trailer capabilities from installed prefab inspection:

- Rigidbody mass: `11000`
- NWH `TrailerModuleWrapper` with `TrailerModule`
- Attachment point: `AttachmentPoint`
- Attachment trigger radius: `0.4`
- Trailer stand / jacks reference exists
- Trailer brake lights, tail lights, left blinkers, and right blinkers exist through NWH lights data
- Trailer gear synchronization is serialized off

## 3. Important NWH Namespaces And Classes

Key installed namespaces/classes used by the LWS boundary:

- `NWH.VehiclePhysics2.VehicleController`
- `NWH.VehiclePhysics2.Input.VehicleInputHandler`
- `NWH.VehiclePhysics2.Input.VehicleInputStates`
- `NWH.VehiclePhysics2.Input.VehicleInputProviderBase`
- `NWH.VehiclePhysics2.Input.InputSystemVehicleInputProvider`
- `NWH.VehiclePhysics2.Powertrain.EngineComponent`
- `NWH.VehiclePhysics2.Powertrain.TransmissionComponent`
- `NWH.VehiclePhysics2.Powertrain.ClutchComponent`
- `NWH.VehiclePhysics2.Modules.Trailer.TrailerHitchModuleWrapper`
- `NWH.VehiclePhysics2.Modules.Trailer.TrailerHitchModule`
- `NWH.VehiclePhysics2.Modules.Trailer.TrailerModuleWrapper`
- `NWH.VehiclePhysics2.Modules.Trailer.TrailerModule`
- `NWH.Common.Cameras.CameraChanger`
- `NWH.Common.Cameras.VehicleCamera`
- `NWH.Common.Cameras.CameraMouseDrag`
- `NWH.Common.Cameras.CameraInsideVehicle`

## 4. LWS Adapter Architecture

Project-owned runtime additions:

- `LwsVehicleIdentity`
- `LwsTruckDefinition`
- `LwsPlayerTruck`
- `ILwsPlayerVehicleService`
- `LwsPlayerVehicleService`
- `LwsNwhVehicleAdapter`
- `ILwsTrailerCoupling`
- `LwsNwhTrailerCouplingAdapter`
- `LwsPlayerTruckSpawner`
- `LwsPlayerTruckDebugPanel`

NWH remains authoritative for:

- Physics
- Wheels
- Suspension
- Engine simulation
- Drivetrain
- Transmission mechanics available in NWH
- Trailer physics
- Trailer coupling physics

LWS now owns:

- Stable player truck identity
- Active player vehicle lifecycle
- Truck definition data
- Normalized telemetry
- Trailer attachment observation
- Development debug display
- Future save/input/gameplay boundaries

## 5. Player Vehicle Lifecycle

Prompt 004 adds:

`Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`

The scene contains:

- LWS bootstrap
- Stock NWH temporary input provider
- Flat road-tagged driving pad
- Truck spawn point
- Trailer pickup point
- LWS truck spawner
- LWS scene marker
- URP-compatible camera, light, and default volume

At Play Mode start, `LwsPlayerTruckSpawner` instantiates the LWS player truck prefab and validation trailer prefab, then adds project-owned LWS components to the spawned truck/trailer instance. This keeps the vendor prefabs untouched while proving the lifecycle.

## 6. Available Runtime State

`LwsNwhVehicleAdapter` exposes normalized telemetry:

- Vehicle ID
- Definition ID
- Speed
- Signed speed
- Engine RPM
- Current NWH gear
- NWH gear name
- Neutral state
- Reverse state
- Throttle input
- Brake input
- Clutch input
- Steering input
- Parking brake input
- Service brake active
- Parking brake active
- Engine running
- Engine stalled
- Grounded
- Fully grounded
- Trailer attached
- Trailer ID
- World position
- World rotation

`LwsPlayerTruckState` defines the future save payload shape without serializing arbitrary NWH graphs.

## 7. Input Currently Used

Temporary Prompt 004 input:

- `NWH.VehiclePhysics2.Input.InputSystemVehicleInputProvider`
- `NWH.Common.Input.InputSystemSceneInputProvider`

NWH public hooks verified:

- `VehicleInputProviderBase.Throttle()`
- `VehicleInputProviderBase.Brakes()`
- `VehicleInputProviderBase.Steering()`
- `VehicleInputProviderBase.Clutch()`
- `VehicleInputProviderBase.Handbrake()`
- `VehicleInputProviderBase.ShiftUp()`
- `VehicleInputProviderBase.ShiftDown()`
- `VehicleInputProviderBase.ShiftInto()`
- `VehicleInputProviderBase.TrailerAttachDetach()`
- `VehicleInputHandler.Throttle`
- `VehicleInputHandler.Brakes`
- `VehicleInputHandler.Steering`
- `VehicleInputHandler.Clutch`
- `VehicleInputHandler.Handbrake`
- `VehicleInputHandler.ShiftInto`
- `VehicleInputHandler.TrailerAttachDetach`
- `TransmissionComponent.ShiftInto(int targetGear, bool instant = false)`

Prompt 005 should replace this validation input with the serious wheel/pedal/H-shifter/FFB integration. Prompt 006 should implement the trucking 18-speed range/splitter behavior.

## 8. Camera Setup

The selected NWH truck already includes:

- `Vehicle Camera Driver`
- `Vehicle Camera Mouse Drag`
- `Vehicle Camera Wheel`
- `Cameras` object with `CameraChanger`

Prompt 004 also includes a scene observer camera for loading the validation scene before the truck cameras activate.

Cab/driver view is expected to be the primary validation view through the NWH camera setup. Exterior chase/debug view is available through the NWH camera changer path.

## 9. Cab And Dashboard Inventory

Available:

- Cab/interior mesh
- Steering wheel object and NWH steering wheel animation reference
- Analog speed gauge
- Analog RPM gauge
- Digital gear gauge
- ABS dash light
- TCS dash light
- Check engine dash light
- Low beam dash light
- High beam dash light
- Left/right blinker dash lights
- Truck low/high/reverse/tail/brake/blinker lights data

Partial:

- Mirrors exist as render-texture cameras/materials but require URP Editor visual confirmation.
- Damage exists in NWH data but LWS gameplay damage/wear is not implemented.

Missing or deferred:

- Fuel display for LWS gameplay fuel
- Parking brake indicator
- Engine brake indicator
- Retarder indicator
- Wipers
- Final production dashboard controls
- Final no-HUD cockpit information pass

## 10. Mirror Inventory

Existing mirror assets:

- `MirrorGlassL`
- `MirrorGlassR`
- `RenderTextureMirrorCameraL`
- `RenderTextureMirrorCameraR`
- `EuroTruckMirrorTextureL.renderTexture`
- `EuroTruckMirrorTextureR.renderTexture`

Current status:

- Two mirror cameras are present in the truck prefab.
- Mirror materials/render textures are present.
- Prompt 004 did not implement quality scaling.
- Prompt 003 mirror quality categories remain the correct future control point.

Prompt 008 should connect these mirror cameras/render textures to the LWS rendering settings:

- Off
- Low
- Medium
- High
- Ultra

Performance concern: two live mirror cameras can be expensive, especially on Steam Deck. Update cadence and render texture size must be quality-controlled later.

## 11. Coupling Architecture

NWH coupling support:

- Tractor: `TrailerHitchModuleWrapper`
- Hitch module: `TrailerHitchModule`
- Trailer: `TrailerModuleWrapper`
- Trailer module: `TrailerModule`
- Attach event: `TrailerHitchModule.onTrailerAttach`
- Detach event: `TrailerHitchModule.onTrailerDetach`
- Input command: `VehicleInputHandler.TrailerAttachDetach`
- Public attach method: `TrailerHitchModule.AttachTrailer(TrailerModuleWrapper trailerWrapper)`
- Public detach method: `TrailerHitchModule.DetachTrailer(VehicleController vc)`

LWS coupling support:

- `ILwsTrailerCoupling`
- `LwsNwhTrailerCouplingAdapter`
- `LwsTrailerAttachmentState`

The LWS adapter subscribes to NWH events and also monitors state in `Update` in case a state change is missed.

## 12. URP And Linear Visual Findings

Inspection-only findings:

- Truck exterior materials are present.
- Cab/interior materials are present.
- Glass/window materials are present.
- Chrome/reflective materials are present.
- Tire/rim materials are present.
- Dashboard gauge and icon materials are present.
- Trailer body/graphics/brake light materials are present.
- Mirror render textures/materials are present.

Normal Editor visual validation is still required for:

- Pink materials
- URP shader conversion status
- Glass transparency
- Mirror render texture output
- Linear color brightness
- Light emissive intensity

No mass material conversion was performed.

## 13. Physics Baseline Results

Code/asset baseline established:

- NWH tractor selected
- NWH trailer selected
- LWS player truck prefab variant created
- LWS trailer prefab variant created
- Truck validation scene created
- Runtime LWS adapters created
- Temporary NWH input provider added
- Coupling adapter added
- Debug panel added
- Validator checks added
- EditMode/PlayMode tests added

Manual normal Editor driving validation is still required for:

- Idle
- Acceleration
- Braking
- Low-speed turning
- Moderate-speed stability
- Reverse
- Coupling
- Uncoupling
- Trailer stability
- Scene reload

The known broken batchmode compiler route was not used as a hard gate.

## 14. Known NWH Limitations

- The selected truck is an NWH sample asset, not final Interstate: Hauler production truck art.
- The selected trailer is a validation dry-van stand-in, not final production trailer art.
- The current NWH transmission is serialized as automatic; final trucking transmission behavior is deferred.
- The stock NWH input provider is temporary.
- Mirror quality scaling is not implemented.
- LWS save/load is only represented by payload shape, not full persistence.
- FFB availability/package status still needs Prompt 005 investigation.

## 15. Deferred Work

- Logitech wheel, pedals, clutch, H-shifter, and FFB
- Final 18-speed range/splitter mapping
- Final manual transmission behavior
- Production truck/trailer roster
- Garage ownership
- Damage/wear/fuel gameplay
- Air hose interaction
- Dashboard gameplay systems
- Mirror quality controller
- Route/job/economy systems

## 16. Prompt 005 Recommendations

Prompt 005 should start from:

- Player truck prefab: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`
- Trailer prefab: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab`
- Validation scene: `Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`
- LWS input seam: `ILwsVehicleInputSource`
- NWH bridge: `LwsNwhVehicleInputProvider`
- Direct NWH setters: `VehicleInputHandler.Throttle`, `Brakes`, `Steering`, `Clutch`, `Handbrake`, `ShiftInto`, `TrailerAttachDetach`

Prompt 005 should verify whether the optional NWH steering wheel / force feedback package is installed or requires manual import/licensing.
