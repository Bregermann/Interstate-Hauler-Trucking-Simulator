# Truck Target Launch

One to four simultaneous competitors drive down a real20m-wide EasyRoads runway, climb its curved lip and fly toward a60m-wide floating target. Five launches each; highest total wins. Only tied leaders receive equal-opportunity extras, repeating until resolved. Solo keeps a local personal best. Start positions rotate. The same NWH wheels, grip, power, wind and bounded flight controls apply to humans and AI.

Drive with the assigned movement source. After leaving the actual ramp end, small wings unfold. Lateral aftertouch remains available; release fore/aft once to arm range correction. The shared correction budget is1.6m/s, acceleration.65m/s², smooth onset and no vertical correction. ACTION commits one dive: wings fold, normal24% gravity compensation ends, and descent becomes18% heavier than ordinary gravity. There is no launch impulse or automatic trajectory correction.

The projected authoritative chassis center at first descending TOP contact scores10(outer30m),25(18m),50(8m precision),100(2.3m bullseye), or zero. Inclusive smaller boundaries take precedence. Side/underside/upward contacts do not score. Physics callbacks and a swept chassis-volume fallback use the same authoritative lock. A colored landing marker records that first result; rebounds, later collisions and held driving cannot change it. Misses retain physical motion and a pooled burst. Generous35s approach /18s flight /48s overall bounds recover genuine failed deliveries.

Target center is130m along the course at4m elevation, below the34m start and23.2m ramp lip. The runway ends at72m. Later rounds share mildly varied layouts, a slowly moving precision center and stronger visible wind, reset identically for everyone. AI varies its planning error/reaction/approach speed, uses projected planar heading and ordinary driving, finite correction and one predictive dive. It can jostle, miss and undershoot.

Human players get one/two/four independent chase panes; AI uses a shared broadcast. Cinemachine owns framing/dolly, with an initial camera offset kept outside the full group so near trucks cannot be excluded as behind-camera targets. The horizon stays stable during physical tumbles. After all resolve, actual recorded child poses, wings, target sections, score markings and landing markers are replayed through noncolliding ghosts, with three angles,0.5×/1×/2×, rewind and skip. Participant paint remains correct. Pause/reconnect freezes the delivery; deliberate fresh input after neutral remains required. Normal rematches preserve setup and reset correction, dives, scores and markers.

## Vendor boundary

VENDOR ASSETS AUDITED: EasyRoads3D Pro3.2.4f5 public ERRoadNetwork/ERRoad runtime examples/docs; NWH WheelController3D and VehiclePhysics2 AirSteerModule sources; Cinemachine2.10.7 GroupComposer/TargetGroup/LensSettings; WeatherMaker through the existing Sports adapter; DOTween1.2.825; installed Heat/uGUI, GR3D trucks, Toon Gas Station rock clusters and existing owned Sports modeling/replay/audio.

VENDOR ASSETS USED: Assets/EasyRoads3D public ERRoadNetwork(owner)/CreateRoad/SetMeshCollider/SetTerrainDeformation/SnapToTerrain/SetResolution/Refresh, baked by the shared editor-only SportsEasyRoadsAuthoring boundary; Assets/NWH wheel/engine foundation through SportsEventTruck; CinemachineTargetGroup/CinemachineGroupComposer/LensSettings; WeatherMaker/SportsChaosWeather with scene-owned pipeline restoration; DOTween wing presentation; existing shared Heat menus/input/settings; installed rock models and NWH/Weather audio. Scene-owned glyph meshes are baked from the existing TMP font; they remain surface mapped in replay. Existing modeled Sumo platform and Bungee lattice meshes are reused.

RELEVANT ASSETS NOT USED: NWH AirSteerModule provides general aircraft attitude torque through a full VehicleController, rather than a finite horizontal correction budget, so it is not a second flight authority. No replacement generic attitude controller was created. Private Taxi EasyRoads code was inspected for integration patterns but is neither referenced nor published. No supplied scoring/replay package implements this sport's first-top-contact rules.

CUSTOM SYSTEMS CREATED: Target Launch rules/progression, contact chronology, AI planning, native authoring and acceptance probes; a small public-API road bake/export boundary and finite horizontal correction helper. No road spline or vehicle physics framework was recreated.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
