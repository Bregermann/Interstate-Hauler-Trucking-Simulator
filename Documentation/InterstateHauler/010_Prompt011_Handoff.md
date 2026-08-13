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

Prompt 010A-E fixed the runtime hookup after normal Editor validation showed no traffic root or visible NPCs. The corridor builder now owns the lifecycle:

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

Normal Unity Editor manual validation:

- PASS confirmed by the user for traffic spawning and NPC traffic driving.
- Highway traffic is visibly functioning.
- The Prompt 009A main-thread performance regression remains fixed.
- `InterstateCorridorValidation` is now a valid live driving/traffic test environment.

Prompt 010A-E repair notes:

- runtime traffic hookup
- runtime traffic profile
- traffic graph initialization/handoff
- broken traffic prefab serialization
- incorrect nested wheel-object references
- persistent top-level UTS traffic prefab references
- correct UTS prefab support detection

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

Still-unverified separate regression checks:

- confirm traffic does not spawn inside the player/trailer
- confirm G29, 18-speed transmission, dashboard, mirrors, and truck controls still work
- confirm no repeating LWS Console errors occur during extended driving
