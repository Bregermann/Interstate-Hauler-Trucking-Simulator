# Prompt 009 - EasyRoads Interstate Test Corridor

## Summary

Prompt 009 establishes the first LWS-owned interstate proving ground scene:

`Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`

The scene keeps the existing Prompt 004-008 truck lifecycle intact: `IH_PlayerTruck_NWH`, `IH_TestTrailer_DryVan`, G29/DirectInput validation objects, 18-speed transmission, truck controls, dashboard, mirrors, and cab accessory anchors are carried forward from `TruckValidation`.

## EasyRoads Version

Installed package: EasyRoads3D Pro v3.2.4f5.

Inspected runtime/editor-facing APIs:

- `EasyRoads3Dv3.ERRoadNetwork`
- `EasyRoads3Dv3.ERRoadType`
- `ERRoadNetwork.CreateRoad(string, ERRoadType, Vector3[])`
- `ERRoadNetwork.BuildRoadNetwork()`
- `ERRoadNetwork.BuildRoadNetwork(bool splatmaps, bool trees, bool detail)`
- `ERRoadNetwork.RestoreRoadNetwork()`
- `ERRoadNetwork.HideWhiteSurfaces(bool)`
- `ERRoad.SetResolution(float)`
- `ERRoad.SetMeshCollider(bool)`
- `ERRoad.SetTerrainDeformation(bool)`
- `ERRoad.SnapToTerrain(bool)`
- `ERRoad.GetMarkerPositions()`
- `ERRoad.GetSplinePointsCenter()`

LWS accesses these through reflection so gameplay code does not reference EasyRoads concrete types directly.

## Editor/Runtime Boundary

EasyRoads owns road authoring, marker-based road geometry, mesh generation, road shaping, and any EasyRoads terrain integration explicitly enabled later.

LWS owns road IDs, segment IDs, graph topology, road class, direction, lane metadata, speed limits, surface identity, and nearest-road lookup.

The Prompt 009 validation scene uses a runtime builder only for this controlled proving ground. Production road authoring can later use EasyRoads editor-authored roads exported through `LwsEasyRoadsExportBoundary`.

## Selected Road Type

The validation builder creates LWS-safe EasyRoads road types at runtime:

- `IH Validation Interstate`
- `IH Validation Ramp`

Road material selection:

- Preferred: `Assets/EasyRoads3D/Resources/Materials/roads/road material.mat`
- Fallback: runtime URP Lit asphalt material

No EasyRoads vendor preset or global road type is overwritten.

## Road Dimensions

- Lane width: 3.7 m
- Lanes: 2 each direction
- Right shoulder: 3.0 m
- Left shoulder: 1.2 m
- Carriageway width: 11.6 m
- Median: 14.0 m
- Total divided paved/median envelope: about 37.2 m

These are development values sized for a full tractor-trailer at highway speeds.

## Corridor Layout

Approximate useful highway length: 3.4 km / 2.1 miles.

Layout:

- Start service/trailer pickup area near `z = -95`
- Entry/merge ramp into the northbound mainline
- Long acceleration/mainline section
- Gentle sweeping curves
- Mild grade variation, about 17 m peak over the route
- Turnaround apron and median crossover near the far end

The scene remains local and non-streamed. Prompt 014 owns streaming chunks, and Prompt 015 owns floating origin.

## Terrain Strategy

Prompt 009 does not generate Vista terrain. The validation scene uses dedicated generated road meshes and service-area pavement.

Terrain mutation policy:

- `SetTerrainDeformation(false)` is requested on EasyRoads roads.
- `BuildRoadNetwork(false, false, false)` is attempted to avoid splatmap/tree/detail mutation.
- `RestoreRoadNetwork()` is called on destroy when an EasyRoads network was created.
- No production terrain is mutated.

## Road Colliders

EasyRoads roads request mesh colliders through `SetMeshCollider(true)`.

If the EasyRoads runtime API is unavailable or fails, LWS fallback road ribbons and paved pads are generated with `MeshCollider` components so the scene can still be opened and driven for basic validation. A full Prompt 009 pass still requires normal Editor confirmation that the EasyRoads-generated road colliders are present and truck-safe.

## LWS Road IDs

Stable corridor IDs:

- `IH_TEST_I000_NB`
- `IH_TEST_I000_SB`
- `IH_TEST_I000_RAMP`
- `IH_TEST_I000_TURN`

Stable segment IDs:

- `IH_TEST_I000_NB_MAIN`
- `IH_TEST_I000_SB_MAIN`
- `IH_TEST_I000_NB_ENTRY_RAMP`
- `IH_TEST_I000_TURNAROUND_CROSSOVER`

These are validation/test IDs, not production real-world interstate IDs.

## Road Graph Structure

Graph ID: `IH_TEST_INTERSTATE_CORRIDOR_009`

Each logical segment has:

- start node
- end node
- edge ID
- road class
- direction
- dry asphalt surface identity
- speed limit
- lane count
- lane width
- lane-center offsets
- sampled centerline points

Current-road lookup is provided by:

- `ILwsRoadGraphService`
- `LwsRoadGraphService`
- `LwsRoadGraphQuery.TryFindNearestRoad`
- `LwsRoadGraphProvider`

## Road Class And Speed

Mainline road class: `LwsRoadClass.Interstate`

Ramp/crossover road class: `LwsRoadClass.Ramp`

Speed limits:

- Mainline: 65 MPH
- Entry ramp: 35 MPH
- Turnaround crossover: 25 MPH

## Centerline Export

`LwsEasyRoadsExportBoundary` now reflects EasyRoads-authored roads into LWS graph-friendly samples. It looks for public methods such as:

- `GetRoads`
- `GetRoadObjects`
- `GetMarkerPositions`
- `GetSplinePointsCenter`
- `GetSplinePoints`

Exported data is normalized into `LwsRoadGraph`, `LwsRoadEdge`, and `LwsRoadSample` without making GPS, traffic, jobs, or road-condition systems depend on EasyRoads classes.

## Future Seams

UTS traffic seam:

- Lane count, lane width, direction, and lane-center offsets are present.
- Prompt 010 should create traffic lanes from LWS graph samples, not from player truck physics or EasyRoads internals.
- No UTS traffic is configured in Prompt 009.

Compass/GPS seam:

- LWS graph topology and nearest-road lookup are ready.
- Compass remains presentation-only in Prompt 011.

Vista seam:

- No Vista terrain is generated or mutated.
- Future Vista integration should fit terrain under LWS road identity and EasyRoads geometry.

Streaming/floating-origin seam:

- IDs do not depend on instance IDs, hierarchy order, or absolute world origin.
- Scene remains local until later streaming/floating-origin prompts.

## Truck Regression Status

Implementation preserves the existing player truck scene stack:

- LWS bootstrap
- LWS truck spawner
- starter NWH semi definition
- `IH_PlayerTruck_NWH`
- `IH_TestTrailer_DryVan`
- 18-speed transmission definition
- truck controls
- dashboard and mirror runtime components
- G29/DirectInput validation objects

Manual road driving still required in normal Unity Editor.

## URP 17.4 Status

Prompt 003 established URP 17.4 and Linear color. EasyRoads includes older URP support assets through URP 17.2, with no exact URP 17.4 package confirmed. Prompt 009 did not import older URP support packages.

Visual result requires normal Editor verification:

- road material not pink
- asphalt brightness acceptable in Linear
- road mesh visible from cab/exterior cameras
- collider and material assignments correct

## Performance Baseline

No measured FPS/CPU/GPU baseline is recorded by Codex because the scene requires normal Editor play and visual/profiling verification.

Expected clean-scene cost drivers:

- generated road meshes/colliders
- lane debug line renderers
- truck mirrors from Prompt 008
- NWH truck/trailer physics

Prompt 010 should record a pre-traffic baseline before adding UTS vehicles.

## Known Limitations

- EasyRoads runtime generation must be verified in the normal Unity Editor.
- LWS fallback meshes are a safety path, not proof that EasyRoads output rendered correctly.
- No production lane markings/barriers/signage were authored.
- No UTS traffic, Compass route, Weather Maker weather, Weatherade road conditions, streaming, or floating origin were implemented.
- G29/FFB road feel requires physical hardware validation.

## Prompt 010 Recommendations

- Use `InterstateCorridorValidation.unity` as the first highway traffic test scene.
- Consume LWS road graph samples and lane metadata for traffic lanes.
- Keep UTS out of player truck/trailer control.
- Establish spawn/despawn zones at service area, ramp entry, far turnaround, and off-camera mainline ends.
- Verify mirror-visible traffic before broad traffic density tuning.
