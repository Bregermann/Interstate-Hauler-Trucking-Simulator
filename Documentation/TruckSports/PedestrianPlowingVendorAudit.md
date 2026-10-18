# Pedestrian Plowing vendor audit

Implementation audit; final validation and publication receipt belong in the checkpoint/report.

| Installed foundation | Concrete reuse / decision |
| --- | --- |
| NWH WheelController3D (`Assets/NWH/WheelController`, existing installed version) | Existing SportsEventTruck authority, WheelController motor/brake/steer/grip API and SportsRallyAuthoring.FitTruck actual truck geometry/wheel bindings. No new vehicle physics. |
| UTS Full Pack (`Assets/UTS_FullPack`) | Existing Taxi_Girl_11 jointed humanoid rig, NPCStats-compatible Taxi adapter, original idle/talk/cheer/walk/run animation controller. The finite crowd uses Unity Navigation rather than UTS street population/MovePath, whose path assumptions and replenishment are inappropriate for a finite scored roster. Vendor ragdoll activation that destroys root bodies is bypassed through the existing reusable Taxi pooling API. |
| Taxi pedestrian/character systems (`Assets/LWS/TruckTaxi`) | TruckTaxiPedestrian.TryStrike/Reactivate/PrepareForPool/Expired and jointed ragdolls. Original funny passenger meshes/materials are retained in owned rig variants. Existing authored reactions are the intended dialogue source; recording coverage is limited. No TaxiBootstrap, ride session, dispatcher or replenishing population is instantiated. |
| Unity AI Navigation2.0.12 | NavMeshSurface, native NavMeshBuilder bake, NavMeshAgent movement/avoidance, complete NavMeshPath routes. Truck-clear connected navigation includes streets and plaza; spawn zones are authored in the scene. No replacement navigation framework. |
| EasyRoads3D | Existing isolated SportsEasyRoadsAuthoring.Bake over ERRoadNetwork/ERRoadType; native physical road meshes retained. Host Taxi road networks are untouched. |
| JC_LP_MegaCity, ScaffoldsPack, RoadPropsPack, UTS cars | Actual building, tree, scaffold/stage, crate and vehicle meshes/materials. Owned variants strip traffic/session behaviors. Missing festival canopies and bunting are small art meshes, not replacement world-authoring systems. |
| Core-Collapse (`Assets/CoreCollapse`) | Collapser/Support damage authority, existing prefractured Wall-Frag meshes and Dust prefab; thin PlowBreakable adapter adds bounded pooled reset and truck-only impact qualification. Fire uses its existing muzzle particle texture; no vendor source changes or new fracture algorithm. |
| NWH effects/audio | Existing smoke prefab and truck engine. Existing Sports sound bank retained; shared settings route volume/haptics. |
| Native Unity Mesh/LODGroup | Mesh.CombineMeshes preserves the original flat-colored character geometry for distant views; original detailed renderers and jointed rigs remain authoritative nearby and on impact. Textured/multi-material custom models retain full rendering. No replacement rendering framework. |
| Heat UI/TMP/DOTween | Existing SportsUIAuthoring/SportsEventMenus setup/help/pause/reconnect/results, graphical per-view cards, pooled popups and DOTween feedback. No new UI framework. |
| Cinemachine2.10.7 / existing Sports camera adapters | Stable chase views, explicit participant ownership and aspect fit; canonical Taxi cab and existing steering/free-look adapters. |
| Compass Navigator Pro4 / installed6.0.2 (`Assets/Plugins/Kronnect/CompassNavigatorPro`) | Real CompassPro dashboard world-space minimaps, independent group/follow/camera/route bindings and SetRoute over native NavMesh paths. Per-view cab layers excluded from GPS captures. No substitute minimap renderer. |
| Taxi driver needs/jug | TruckTaxiDriverNeedsState owns inventory/pressure/QTE success/spill, TruckTaxiJugLiquidLoop owns the existing liquid sound/fallback. Per-player sport adapter supplies owned inputs and pause/focus/reconnect policy; no duplicate jug rules or Taxi ride session. |
| SportsMotionReplay | Existing bounded actual-transform recording/seek/speed/angle playback. Ghost trees contain only transforms and render components. |
| Pixel Crushers, Weather Maker/Weatherade, Scene Streamer, Vista/Spline Spawner, wheel input packages | Audited existing project conventions. This finite offline city round needs no new save framework, dynamic weather, world streaming, terrain generator or steering-wheel backend. Existing collection settings/input remain in use. |

Custom systems created: finite role-independent crowd configuration and scene spawn zones; once-per-target contact attribution/score/round owner; finite crowd coordination/AI target selection; sport-specific lifecycle/presentation adapters; native authoring/configuration/validation tools. They supply missing sport semantics, rather than replacing vendor responsibilities.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

VENDOR ASSETS AUDITED: NWH, UTS, EasyRoads, Compass Navigator Pro, Heat/TMP/DOTween, Cinemachine, Unity Navigation, Core-Collapse, installed environment/character packs, Pixel Crushers, Weather Maker/Weatherade, Scene Streamer, Vista/Spline Spawner and existing input integrations.

VENDOR ASSETS USED: NWH wheels/vehicle adapters; UTS jointed rig/animations and Taxi pedestrian pooling; EasyRoads; Compass Navigator Pro; Heat/TMP/DOTween; Cinemachine; Unity Navigation/Mesh/LODGroup; Core-Collapse; original owned character/building/car/prop meshes and existing audio.

RELEVANT ASSETS NOT USED: UTS replenishing street population and MovePath do not fit a finite scored crowd; existing Taxi pooling/navigation APIs are reused. Pixel Crushers, weather, streaming, terrain generation and steering-wheel backend are not newly instantiated for this finite city sport; existing collection settings/input remain authoritative. See the table for precise reasons.

CUSTOM SYSTEMS CREATED: finite sport-role/configuration/spawn semantics, contact attribution and score/round authority, sport AI coordination, presentation/cab adapters, native authoring and explicit development validation. No duplicate vehicle, navigation, GPS, weather, save or UI framework.
