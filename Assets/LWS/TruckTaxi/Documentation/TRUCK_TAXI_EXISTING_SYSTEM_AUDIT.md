# Truck Taxi Existing System Audit

Truck Taxi is an optional driving sandbox, not a freight career or Dead Air scene.

| System | Existing implementation | Reuse / boundary |
|---|---|---|
| Tractor / spawn | Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs; Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab | Reuse spawner; configure fallback prefab without a truck definition's validation trailer. No physics fork. |
| Physics / transmission | NWH VehicleController, LwsNwhVehicleAdapter, Lws18SpeedTransmissionController | Unchanged; spawner installs stability, recovery and dashboard. |
| Input / wheel | LwsKeyboardGamepadTruckInputSource, LwsNwhVehicleInputProvider, Input/Wheels | Reuse driving controls; taxi-only UI actions use Unity Input System. |
| Trailer | LwsNwhTrailerCouplingAdapter / NWH trailer modules | No trailer spawned or required. No freight ActiveJob created. |
| Cameras / instruments | LwsNwhCameraPresentationMonitor, truck cameras, dashboard, mirrors | Reuse; do not rebuild cab. |
| GPS | Navigation/Compass/LwsCompassNavigatorProAdapter.cs; Assets/Plugins/Kronnect/CompassNavigatorPro | Existing Compass map/cameras/route renderer; TruckTaxiGPSAdapter only requests LWS navigation routes. |
| Road semantics | LwsRoadGraph, LwsNavigationService | Reuse graph and route calculation. Demo content supplies a compact graph. |
| Road geometry | Assets/EasyRoads3D/Scripts/runtimeScript.cs ERRoadNetwork/CreateRoad | Bake demo roads through installed EasyRoads public API. No custom replacement road renderer. |
| Traffic | Assets/UTS_FullPack; Traffic/UTS/LwsUtsTrafficApi.cs | Reuse CreatePath/SpawnVehicle. Highway controller filters out city roads; a taxi population adapter supplies closed city lanes to the same vendor bridge. |
| City art | NWH Euro Truck demo Building01/02 FBX | Reuse where practical with intentionally simple demo props/district dressing. |
| Pedestrians | Assets/UTS_FullPack/Scripts/Paths/PeopleWalkPath.cs, Passersby, adult people prefabs | UTS SpawnPeople/SpawnOnePeople own paths, animation and walking. TruckTaxiPedestrian only adds hit/respawn state. |
| Collision | Unity Rigidbody/Collision and vendor vehicle bodies | Taxi event observer only; no replacement physics. |
| Weather | LWS clock/weather/rendering services, Weather Maker adapters | Keep service authority; use simple daylight demo scene. One minimal player compilation compatibility fix, described below. |
| UI | Heat ButtonManager, uGUI, TextMeshPro | Heat button prefab plus focused uGUI taxi HUD; no new UI framework. |
| Audio | NWH truck audio; Heat audio optional | Preserve truck audio. Taxi notification clips optional. |
| Bootstrap | LwsApplicationBootstrap | Opt-in drivingSandbox skips career services before initialization; default registry unchanged. |
| Persistence | Pixel Crushers through LwsSaveService | NOT started in taxi sandbox. Taxi statistics are session-only; no player save files touched. |
| Freight events | Prompt 020/021/022 job/trailer/delivery services | Not initialized in taxi mode. Taxi owns only ride session/request semantics. |

Core modification risk is limited to an opt-in bootstrap flag. Default remains the full registry.
Existing dirty vendor/scene files predate this task and are not part of Truck Taxi.
Unity/vendor runtime initialization can also refresh imported materials/render textures;
existing unrelated changes were not reverted. The vendor source exception below is
the only intentional vendor source edit in this task.

The taxi scene references a Unity-reserialized copy of the Interstate tractor under
TruckTaxi/Prefabs, avoiding malformed source-variant file IDs. It is not a physics fork.
The taxi adapter defers UTS AI for one frame so CarMove.Start completes before Move.

## Windows Compilation Compatibility Exception

The first actual Windows build reproduced CS0103 at lines 48 and 50 in
`Assets/WeatherMaker/Prefab/Scripts/Clouds/WeatherMakerCloudProbeScript.cs`.
The blackTexture field and its gizmo usage are declared inside UNITY_EDITOR, but
OnDestroy previously referenced it in all builds. No wrapper or scene setting can
repair an undefined identifier while compiling the vendor assembly.

The minimal fix wraps only that texture cleanup in `#if UNITY_EDITOR` / `#endif`.
Editor behavior is unchanged; player builds omit cleanup for an Editor-only gizmo
texture. Cloud rendering, weather semantics and public APIs are untouched. This
fits the repository's reproducible compatibility-defect exception. Recheck this
guard when upgrading Weather Maker rather than carrying an unnecessary patch.
