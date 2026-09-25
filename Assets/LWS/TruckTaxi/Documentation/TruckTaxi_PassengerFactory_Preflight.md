# Truck Taxi Passenger Factory Preflight

Baseline: `ff0ee0f6`, checkpoint tag `truck-taxi-before-passenger-factory`.
The existing three-ride PlayMode integration test passed again on 2026-09-23
before implementation (FactoryBaseline.xml, 30.53 seconds). Its rides are
teleport-assisted; this is not manual driving or cockpit readability proof.

## Known-Good Systems

| Responsibility | Existing owner |
| --- | --- |
| Scene | Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity |
| Mode bootstrap | TruckTaxiBootstrap; career-disabled LwsApplicationBootstrap |
| Tractor spawning | LwsPlayerTruckSpawner / TruckTaxi_InterstateTractor.prefab |
| Physics/input/recovery | NWH VehicleController, LWS input/transmission/stability/reset |
| Road routing | LwsRoadGraphProvider / LwsRoutePlanner / ILwsNavigationService |
| GPS rendering | LwsCompassNavigatorProAdapter, existing HUD and cab Compass instances |
| Cab screen mount | LwsCabGpsController / LwsCabAccessoryAnchorRegistry |
| HUD | TruckTaxiHud, Heat buttons, TMP, uGUI |
| Ambient world | TruckTaxiTrafficAdapter and TruckTaxiPedestrianPopulation / UTS |
| Rides/requests/fares | TruckTaxiSession, PassengerProfile, PassengerRequestDefinition |
| Debug | TruckTaxiDebugPanel, TruckTaxiPlayerSmokeTest |

Core mode files live under Assets/LWS/TruckTaxi. The Interstate services and
vendor assets are inherited dependencies, not replacement targets. The mode
already disables career save/job menus, omits trailers, uses its own authored
city/traffic lanes, and repositions the HUD Compass to bottom-left.

## Audit Findings And Boundaries

- Offer text currently measures straight lines in the UI. Reuse the existing
  LWS route planner for immutable offer distances without changing the active route.
- Existing cab Compass is already world-space, 640 x 400. Verify final world
  dimensions and housing alignment in the runtime, not merely local scale.
- Compass owns actual map cameras/textures/routes; no custom road renderer is needed.
- NWH steering already exposes maximumSteerAngle, speedSensitiveSteeringCurve,
  speedSensitiveSmoothingCurve and degreesPerSecondLimit. Its actual evaluation
  uses speed / 50 m/s (the old XML comment saying 100 m/s is stale).
- LWS stability already supplies COM/inertia/anti-roll. Preserve it and apply
  reversible steering-only settings to the spawned taxi instance.
- Keep the original ten passenger assets, scene layout, physics, transmission,
  wheel input, traffic and existing ride lifecycle. Do not regenerate the city.
- Phase B is gated on Phase A runtime/presentation validation. Audit the installed
  voice/model/dialogue tools before implementing that phase.
- Unrelated dirty Dead Air, freight trailer/delivery and vendor-import changes
  are outside this task and must remain untouched.

## Phase A Evidence

2026-09-23: `FactoryPhaseAPlayMode.xml` passed 3/3 tests (baseline three rides,
actual keyboard/NWH low-speed turn, presentation). Final `FactoryPresentation.xml`
passed after the rectangular map-mask and route CanvasRenderer corrections.
`FactoryPhaseAEditMode.xml` passed 23/23.

The observed cab screenshot shows the live Compass map aligned to the existing
interior-mesh GPS screen (640x340 at 0.00023 = 0.1472x0.0782m). No second map camera
or navigation framework was created. The offer screenshot shows YOU/PICKUP/DESTINATION
and distinct route legs. 1080p, 1440p and ultrawide captures were generated.
The feedback-controlled low-speed turn reached 175 degrees, about 7.26m swept width,
8.5 MPH peak, minimum upright dot 1.00. This is automated physics evidence, not a
claim of manual highway/reverse handling validation.

On the later voice-regression run, that coasting-only test driver overshot its speed
band (10.7 MPH, 13.17m width). The fixture now uses existing W/S service-brake input
and a fixed capture timestep; the 11m width assertion was not relaxed and vehicle
tuning was not changed. Two controlled reruns measured 9.49m / 9.54m, 175 / 176 degrees,
7.1 / 7.2 MPH peak, upright dot 1.00. Final `FactoryVoiceRegressionPlayMode.xml`
passed 3/3. Manual highway/reverse feel still needs human validation.

## Phase B Voice Adapter Checkpoint

The supplied external WorkHere installation was inspected and reused. See
`TruckTaxi_Chatterbox_WorkHere_Adapter.md` for exact interfaces and test evidence.
The current factory editor implements voice/dialogue authoring, queueing and import;
the broader model/rig/animation/ejection factory work remains outstanding.
