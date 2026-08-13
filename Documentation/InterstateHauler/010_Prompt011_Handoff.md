# Prompt 011 Handoff - GPS / Routing

Use Prompt 010 only as traffic context.

Current validation scene:

`Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`

Road graph source:

- `LwsRoadGraphProvider`
- `ILwsRoadGraphService`
- Prompt 009 stable road IDs and lane metadata

Traffic runtime:

- `LwsUtsHighwayTrafficController`
- `LwsTrafficLaneBuilder`
- `LwsTrafficIdentity`
- `ILwsTrafficService`
- `Assets/LWS/InterstateHauler/Traffic/Data/IH_TrafficProfile_InterstateValidation.asset`

Prompt 010A fixed the runtime hookup after normal Editor validation showed no traffic root or visible NPCs. The corridor builder now owns the lifecycle:

`LwsInterstateCorridorRuntimeBuilder`
->
`ConfigureValidationProfile()`
->
`InitializeFromGraph()`
->
`IH UTS Highway Traffic Runtime/Lanes`
and
`IH UTS Highway Traffic Runtime/Vehicles`

NPC traffic identity:

- every spawned UTS NPC is configured as `LwsVehicleRole.AiVehicle`
- future systems should consume LWS identity/registry, not UTS concrete scripts

Do not use UTS for:

- player truck
- player trailer
- GPS
- route authority
- NWH drivetrain/coupling

Prompt 011 should:

- build GPS/navigation on the LWS road graph
- consume speed/road/lane metadata from `LwsRoadGraph`
- optionally query traffic through `ILwsTrafficService`
- keep routing independent of EasyRoads and UTS concrete classes

Manual checks still required before full pass:

- drive `InterstateCorridorValidation` in normal Unity Editor
- confirm traffic spawns and follows NB/SB/ramp lanes
- confirm `IH UTS Highway Traffic Runtime` appears with `Lanes` and `Vehicles`
- confirm the traffic status panel reports Initialized, UTS Available, Graph Available, and Spawnable Prefabs greater than zero
- confirm the `SPAWN TEST TRAFFIC NOW` button can spawn a visible NPC when safety rules allow it
- confirm G29, 18-speed transmission, dashboard, mirrors, and truck controls still work
