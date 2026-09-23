# Truck Taxi File Inventory

All new implementation files live under `Assets/LWS/TruckTaxi/` (paths below are
relative to that root). Unity-generated `.meta` files accompany assets and folders.

## Runtime

- Scripts/LWS.TruckTaxi.Runtime.asmdef
- Scripts/PassengerProfile.cs
- Scripts/PassengerRequestDefinition.cs
- Scripts/ShortcutTrigger.cs
- Scripts/TruckTaxiBootstrap.cs
- Scripts/TruckTaxiCollisionObserver.cs
- Scripts/TruckTaxiConfiguration.cs
- Scripts/TruckTaxiDebugPanel.cs
- Scripts/TruckTaxiGPSAdapter.cs
- Scripts/TruckTaxiHud.cs
- Scripts/TruckTaxiImpactTarget.cs
- Scripts/TruckTaxiPedestrian.cs
- Scripts/TruckTaxiPedestrianPopulation.cs
- Scripts/TruckTaxiPlayerSmokeTest.cs
- Scripts/TruckTaxiRideLocation.cs
- Scripts/TruckTaxiSession.cs
- Scripts/TruckTaxiTrafficAdapter.cs
- link.xml

## Editor And Tests

- Editor/LWS.TruckTaxi.Editor.asmdef
- Editor/TruckTaxiDemoBuilder.cs
- Tests/EditMode/LWS.TruckTaxi.Tests.EditMode.asmdef
- Tests/EditMode/TruckTaxiSessionTests.cs
- Tests/PlayMode/LWS.TruckTaxi.Tests.PlayMode.asmdef
- Tests/PlayMode/TruckTaxiDemoPlayModeTests.cs

## Authored And Generated Assets

- Scenes/TruckTaxi_DemoCity.unity
- Prefabs/TruckTaxi_InterstateTractor.prefab
- ScriptableObjects/TruckTaxi_DemoConfiguration.asset
- ScriptableObjects/Passengers/Al_Lee.asset
- ScriptableObjects/Passengers/Bill_Rush.asset
- ScriptableObjects/Passengers/Cam_Era.asset
- ScriptableObjects/Passengers/Dot_Matrix.asset
- ScriptableObjects/Passengers/Joy_Ride.asset
- ScriptableObjects/Passengers/Max_Volume.asset
- ScriptableObjects/Passengers/Nora_Brake.asset
- ScriptableObjects/Passengers/Pat_Pending.asset
- ScriptableObjects/Passengers/Ray_Rage.asset
- ScriptableObjects/Passengers/Robin_Roundabout.asset
- ScriptableObjects/Requests/FastDelivery.asset
- ScriptableObjects/Requests/HitPedestrian.asset
- ScriptableObjects/Requests/MaximumChaos.asset
- ScriptableObjects/Requests/NearMiss.asset
- ScriptableObjects/Requests/NoCollisions.asset
- ScriptableObjects/Requests/Offroad.asset
- ScriptableObjects/Requests/PropertyDamage.asset
- ScriptableObjects/Requests/RamTraffic.asset
- ScriptableObjects/Requests/ScenicRoute.asset
- ScriptableObjects/Requests/Shortcut.asset
- ScriptableObjects/Requests/SmoothRide.asset
- Materials/Asphalt.mat
- Materials/Brick red.mat
- Materials/Concrete.mat
- Materials/Harbor teal.mat
- Materials/Park green.mat
- Materials/Taxi yellow.mat
- DemoContent/Meshes/Road_0.asset through Road_9.asset (ten EasyRoads baked meshes)

## Documentation

- Documentation/TRUCK_TAXI_EXISTING_SYSTEM_AUDIT.md
- Documentation/TRUCK_TAXI_README.md
- Documentation/TRUCK_TAXI_VALIDATION.md
- Documentation/TRUCK_TAXI_FILES.md

## Intentional Changes Outside This Folder

- Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs: opt-in driving
  sandbox registration, default off. Career services are excluded only in this mode.
- ProjectSettings/EditorBuildSettings.asset: append the taxi scene; dedicated taxi
  builds use an explicit one-scene list without changing the main scene order.
- Assets/WeatherMaker/Prefab/Scripts/Clouds/WeatherMakerCloudProbeScript.cs:
  minimal UNITY_EDITOR guard for Editor-only gizmo texture cleanup, required by
  actual Windows compilation. See the existing-system audit for the diagnosis.

Other dirty project files existed before this task or were refreshed by Unity
imports/vendor initialization. They were not reverted or included in a commit.
Build output and validation evidence are under `Builds/TruckTaxiDemo/`.
