# Edenborough implementation checkpoint

Status: authoring/runtime integration prepared; dedicated scene, owned-art imports and Windows acceptance are incomplete. Unity license/editor access is pending. Do not claim a playable world or runtime PASS from this source work.

## Prepared integration

- Fixed fictional nine-district footprint, 12 x 10 km; Gogar/Zoo west, Leith north, Musselburgh east, southern suburban/hill districts and bypass. Not GIS-grade geography. Local/arterial/highway/service lanes are 3.05/3.25/3.45/3.10 m; actual truck fit remains unverified.
- Six native URP terrains, installed Vista/NWH texture references, seamless regional heights, road cuts and explicit service-pad blending. Editor-only bake; no runtime terrain manager.
- Separate scene authoring, copied canonical host/spawner/configuration, actual EasyRoads meshes/colliders, existing LWS graph, UTS/Citizens circuits/walk areas, Spline Spawner dressing and existing facility interactions.
- Reviewed art palette must contain exact inspected prefab references, body paint materials and garage module alignment. Missing content fails closed rather than substituting a blockout or imported demo scene.
- Scene-specific baked static geometry tiles feed the existing Compass capture layer. Old catalogs and route authority remain intact.
- Main-menu New uses its configured primary scene; Continue uses the selected slot's whitelisted original scene. Old DemoCity saves do not enter Edenborough accidentally.
- Existing Heat/HUD/modal/input/Career garage integration, HSV live paint preview, Apply/Cancel/Default and per-owned-truck RGB persistence. Pixel Crushers remains storage authority.

## Garage limits

The current Career catalog contains one metadata-only starter. New-scene authoring copies its actual Taxi prefab into an authored definition and aligns copied catalog IDs without changing the source scene or catalog. Additional owned trucks are not fabricated. Physical hot-swapping to a different truck is not implemented; unsupported changes are rejected before committing Career state. This remains a completion limitation, not a working multi-truck switch claim.

## Asset evidence

The real cached NWH Vehicle Physics 2 package was read-only indexed: 1,345 entries, 65 prefabs, 474,741,993 bytes. The inspector supports Unity Asset Store gzip metadata and second-line pathname flags. It excludes frameworks/scripts and validates GUID closure and paths before staging. Six synthetic inspector tests passed; this is not visual prefab validation.

The 12 immediate shortlist art packages are absent from the default Asset Store cache. No new package has been imported. Installed UTS/Citizens, NWH, EasyRoads, Compass, Heat, Spline Spawner and Vista references are reused. Workbook status cells identify unavailable packages and unexecuted Windows checks; listing URP claims are not runtime verification.

## Validation and next step

Use Tools/TruckTaxiEdenborough/Checkpoint.md for all 14 original acceptance criteria, source counts, blockers and exact next steps. Normal Unity compile, EditMode execution, scene geometry checks, Play Mode driving, actual road/truck fit, visuals, performance and Windows save/quit/relaunch have not run. Planned output is Builds/TruckTaxiEdenborough/TruckTaxi.exe; no such task build is claimed to exist.

Vendor sources, the old demo scene and existing saves were not changed by this task. Existing unrelated dirty changes remain preserved. Local commits only; no push.
