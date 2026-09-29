# Edenborough OSM editor pipeline (Section P)

This is an explicit authoring pipeline, not a runtime map service. It does not
download anything during gameplay. No vendor source, shared bootstrap, or
existing scene is modified by the importer.

## Source and reproducibility

- Prototype snapshot: OpenStreetMap API 0.6 map bbox
  `[-3.1970, 55.9460, -3.1830, 55.9540]` in Edinburgh, downloaded
  2026-09-28 UTC. The raw 14,486,229-byte `cache/old-town-east.osm`
  is ignored by Git and is outside Unity `Assets`.
- Normalized, tracked artifact: `prototype.json`. Its source SHA-256,
  UTC download time, bbox, ODbL license, attribution text, and pipeline
  version are embedded in `source`. The snapshot hash is
  `0015a0c99d8efd16e889dd79b93d81ad4414abb2f860f8cb0ba363902d6a550b`.
- OSM data is ODbL 1.0. Credit "OpenStreetMap contributors" with a link to
  https://www.openstreetmap.org/copyright in the shipped game/map/credits.
  The normalized geographic database may carry ODbL share-alike duties;
  review distribution and attribution before release. The generated scene
  stores provenance in `EdenboroughSourceMetadata`, but that component is
  not a substitute for visible player-facing credit.
- Full-city input path: an externally obtained Geofabrik Scotland PBF
  (`scotland-latest.osm.pbf`) may be passed with `--source`. This requires
  an already installed `osmium` executable for complete-ways bbox extraction
  and XML conversion into the ignored cache. Nothing installs or downloads
  that large PBF automatically. The configured small API snapshot is the
  working path on this machine.

Primary source references:
[OSM API map bbox](https://wiki.openstreetmap.org/wiki/Api06),
[Geofabrik Scotland](https://download.geofabrik.de/europe/united-kingdom/scotland.html),
[osmium extract](https://docs.osmcode.org/osmium/latest/osmium-extract.html),
[OSM license](https://www.openstreetmap.org/copyright).

## Commands

From the repository root:

```powershell
python Tools/GeoImport/Edenborough/edenborough.py locate
python Tools/GeoImport/Edenborough/edenborough.py download
python Tools/GeoImport/Edenborough/edenborough.py extract
python Tools/GeoImport/Edenborough/edenborough.py validate
python Tools/GeoImport/Edenborough/edenborough.py build
python Tools/GeoImport/Edenborough/edenborough.py preview-classes
python Tools/GeoImport/Edenborough/edenborough.py preview-regions
python Tools/GeoImport/Edenborough/edenborough.py rebuild-map
python -m unittest discover -s Tools/GeoImport/Edenborough -p "test_*.py" -v
```

`download` refuses to replace a snapshot. Remove it explicitly to refresh,
then rebuild and review the new hash/counts. `extract`, `build`, and
`rebuild-map` normalize the configured bbox into the same artifact;
`rebuild-map` updates the `map` layer but does not alter the current game UI.
No raw input under `Assets` is accepted.

Unity editor menu `Tools > Edenborough` exposes the same locate/download,
extract, validate, graph, class, region, and map commands. `Build Selected
Region` opens a region picker. Choose `R_0_0` for the representative
prototype, then build. The exact public, dialog-free parent entry point is:

```csharp
string report = LWS.InterstateHauler.Editor.EdenboroughAuthoring.BuildRegion("R_0_0");
```

The zero-argument menu/execute-method helper is
`LWS.InterstateHauler.Editor.EdenboroughAuthoring.BuildPrototype()`.
Run in Edit Mode after compilation; close the previously generated region
scene before rebuilding. Other loaded scenes are not saved or replaced and
the previously active scene is restored. Failure closes the new unsaved scene.
Successful authoring leaves its scene open additively for inspection.

It generates a separate scene, real EasyRoads road objects, vendor-produced
mesh/collider assets, an LWS graph provider, `EdenboroughTrafficData.asset`,
one-region LWS streaming manifest/policy, a Scene Streamer-compatible chunk
root, source metadata, and a
`PrototypeTruckSpawn` transform under
`Assets/LWS/InterstateHauler/Edenborough/Generated/R_0_0/`. This is the
parent's Unity execution hook. Add the scene to the development build or load
it additively through the generated manifest; place the existing NWH tractor
at the marker. No new NWH rig or control authority is created here.

## Data and conventions

- Origin: WGS84 latitude 55.9500, longitude -3.1900. Local X is east,
  local Z is north, metres. Small-area equirectangular projection uses
  WGS84 Earth radius 6,378,137 m and a fixed origin latitude; no raw
  latitude/longitude enters Unity transforms. Rebase/floating-origin
  coordination remains with existing LWS streaming.
- Region grid: 500 m cells in local coordinates. The prototype bbox
  produces four cells. The normalized artifact has 273 candidate drivable OSM
  ways, 1,149 centerline segments, 2,259 directed LHT lane segments, 2,868
  turn transitions, 126 rail ways, one water feature, and four regions.
  `R_0_0` contains 63 unique source roads and 572 lanes (756 transitions
  terminate in this cell, including cross-cell links). Its bounded flat
  EasyRoads selection is 58 ways/runs, 552 lanes and 705 internal transitions.
  Five bridge/tunnel ways are explicitly excluded from scene/graph generation,
  not discarded from the source: `4887695`, `70955018`, `938128675`,
  `1188418525`, `1258152658`. These are input selection counts, not a claim
  that Unity authoring or the collision gate has been executed here.
- Stable IDs derive from OSM way/node IDs; display `name` is separate.
  Road attributes retain one-way, roundabout, raw lane counts, maxspeed,
  surface, bridge, tunnel, layer, access, hgv, service, width, and source
  way ID. Every original tag is also retained as a key/value array, including
  conditional access and turn-lane tags. Rail/water and non-drivable paths
  remain map references only.
  Named map lines are data, not automatically shipped real businesses/POIs.
- Truck paths exclude foot/cycle-only classes, explicitly private/no
  motor access, and service driveways/parking aisles. One-way and
  roundabout direction are honored. `no_*` and `only_*` OSM turn
  restrictions with a via-node are enforced; via-way restrictions still
  need a future graph pass. Lane offsets are on the left side of travel.
  No automatic U-turns are emitted. Conditional access, turn-lane indications,
  height/weight limits and truck turning radius still require route policy;
  candidate drivable does not mean unrestricted truck access.
- `EdenboroughTrafficData` exports existing `LwsTrafficLaneDefinition` objects
  and explicit legal turn mappings. Way bindings join OSM IDs/full tags to
  lane IDs and EasyRoads object names. Graph edge/road IDs intentionally use
  unique lane IDs, matching the existing LWS graph validation contract.
  Turn edge IDs map to `fromLaneId`, `toLaneId`, via-node ID and centerline.
  Imported short lanes have `spawnEnabled=false`; no spawner is introduced.
  An LWS adapter can choose an ordered lane route and call
  `TryComposeRoute(laneIds, closeLoop, out route, out message)`. It validates
  every legal transition and returns the existing lane type consumed by
  `LwsUtsTrafficApi.CreatePath`. This is data composition, not a route solver.
  The current UTS adapter creates looping paths: use `closeLoop:true` and
  require success before passing a route to it. Open routes need the existing
  adapter's endpoint lifecycle configured explicitly. No isolated segment is
  silently connected back to itself, and no UTS actors are instantiated here.
- EasyRoads is now the sole road geometry/collision authority. The former
  custom quad/pad mesher was removed, not retained as a production fallback.
  `EdenboroughEasyRoadsAdapter` uses the installed public `ERRoadNetwork`,
  `ERRoadType`, `ERModularBase` and `ERModularRoad` APIs behind one LWS wrapper.
  It instantiates the vendor network prefab in the new scene, explicitly binds
  its own modular base, and creates contiguous OSM marker runs with straight
  controls, lane-envelope widths, one-way metadata and enabled mesh colliders.
  Terrain deformation and snapping are disabled for this flat prototype.
  Mesh assets persist the vendor's output; LWS does not generate triangles.
  Bounded endpoint marker extensions provide prototype junction overlap,
  not a replacement junction editor. Production intersections still need
  EasyRoads connections/refinement, lane paint/signals and truck clearance QA.
- Authoring samples every lane and turn at intervals of at most 2 m, with
  at least three samples, against only enabled EasyRoads mesh colliders.
  Missing road support aborts export with the offending edge/sample. The
  simple prototype ground is never accepted as road support. A successful
  return reports that this collision gate and graph/manifest validation passed;
  it does not certify truck drivability. Bridge/tunnel grades are not flattened
  into fake navigable roads. Terrain and grade-separated authoring are deferred.
- The generated LWS graph and manifest are persistent assets in the
  selected scene folder. The manifest describes only the built region;
  the other planned regions are not claimed to be loadable. The graph provider
  has automatic global registration disabled, so loading a district cannot
  replace the live graph authority. The parent chooses graph/streaming activation.
  The `map`
  layer in JSON is a geography foundation, not a wired Truck Taxi map tile.

## Validation boundary

Automated outside Unity: six Python tests cover metric origin, LHT offsets,
one-way direction, excluded footway, turn restrictions, source hash, full tags
and real prototype provenance. Standalone Roslyn checks the Edenborough C#
against installed Unity/LWS references; this is not a Unity Editor compile.
Four focused `EdenboroughTrafficDataTests` are supplied for the parent's
EditMode run (legal route composition, missing loop/reverse transition,
disconnected transition/null ID, raw tag access).

Integration validation on 2026-09-29 used Unity 6000.4.10f1. All four EditMode
tests passed. `BuildRegion("R_0_0")` produced 58 EasyRoads objects, 552 lane
records and 705 turns; the vendor-collider coverage gate passed. The generated
region was reloaded additively for `EdenboroughNwhMovementPlayModeTests`.
Truck Taxi's existing NWH tractor drove 3.01 m using scripted public NWH input,
reaching 1.93 m/s and 2.74 degrees of wheel steering, with all wheels supported
by EasyRoads in 147/147 observed physics samples. No scene/prefab was changed
by this test. The older `IH_PlayerTruck_NWH.prefab` produced a pre-existing
PrefabInstance/PPtr import error; it was not repaired or used for the passing run.

This short automated drive is not human route acceptance. Narrow streets,
intersection turns, trailers, hardware input, live UTS/streaming activation and
prototype performance remain unverified. This district is not enabled in the
Truck Taxi Windows build. Visible OSM attribution is still required before a
player-facing release.

## Vendor audit

- VENDOR ASSETS AUDITED: installed EasyRoads public API metadata and network
  prefab; existing TruckTaxi EasyRoads wrapper; UTS CarWalkPath/WalkPath;
  LWS UTS adapter/lane contracts and Scene Streamer integration contracts.
- VENDOR ASSETS USED: EasyRoads road/mesh/collider authoring through public APIs;
  existing LWS graph, traffic lane and streaming contracts for vendor handoff.
- RELEVANT ASSETS NOT USED: no UTS actor spawning, NWH rig authoring/driving,
  Vista terrain generation or new Scene Streamer runtime lifecycle in this tool.
- CUSTOM SYSTEMS CREATED: editor-only OSM normalization, provenance/way bindings,
  EasyRoads adapter, legal-route data composition and focused tests.
- VENDOR SOURCE MODIFIED: none by this work.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none; custom road mesher removed.
