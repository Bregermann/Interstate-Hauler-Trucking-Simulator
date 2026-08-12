# Prompt 009 Handoff - EasyRoads Interstate Test Corridor

Active player truck prefab:

`Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`

Validation scene:

`Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`

Bootstrap:

`Assets/LWS/InterstateHauler/Bootstrap/Bootstrap.unity`

Active URP assets:

- `Assets/Settings/PC_RPAsset.asset`
- `Assets/Settings/PC_Renderer.asset`
- `Assets/LWS/InterstateHauler/Rendering/Data/IH_RenderingSettings.asset`

EasyRoads:

- Installed version from Prompt 001: EasyRoads3D Pro v3.2.4f5
- URP support package found through URP 17.2.0, no exact URP 17.4 package found
- Actual LWS adapter shell: `Assets/LWS/InterstateHauler/Roads/EasyRoads/LwsEasyRoadsExportBoundary.cs`

Authority rules:

- EasyRoads owns authored road geometry/building.
- LWS road graph owns routing IDs, truck navigation graph, GPS/jobs/traffic integration data.
- Vista owns terrain/biomes.
- UTS remains NPC traffic/reference authority only, not player vehicle/trailer authority.
- Compass remains route presentation, not route authority.

Recommended first corridor:

- Controlled straight interstate test corridor
- At least 2 miles / 3.2 km equivalent driveable length if practical
- 2 lanes each direction plus shoulder
- wide merge/turnaround/test area near spawn
- player truck spawn clear of road geometry
- trailer spawn/pickup area with enough reversing room

Vehicle requirements:

- Preserve Prompt 004/006/007/008 truck spawn stack.
- Do not bypass the LWS player truck spawner.
- Keep NWH player physics authority intact.

Streaming/floating-origin considerations:

- Prompt 009 can stay local and non-streamed, but should not hardcode future world origins into road graph IDs.
- Use stable LWS road IDs and corridor segment IDs from the beginning.

Known blockers:

- EasyRoads URP 17.4 visual/material compatibility still needs normal Editor validation.
- Runtime terrain mutation must be avoided or wrapped with restore/backup policy.
- No Vista terrain corridor has been generated yet.
- No production traffic lane export exists yet.

Prompt 008 note:

Cab Life accessory anchors now exist. Prompt 009 should preserve them but should not implement additional Cab Life content.
