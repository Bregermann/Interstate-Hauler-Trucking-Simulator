# Truck Taxi Mega Pass authoring

This is a source-based guide to the current prototype, not a validation report.
The persistent scene is `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity`.
Save open scene edits and leave Play Mode before running editor authoring.

## Editor sequence

1. Start from the existing regional world and Thunder Bowl. If either is absent,
   use `Truck Taxi/Regional/Convert Existing City To Streamed Region` and
   `Truck Taxi/Regional/Author Thunder Bowl Speedway` first; do not rerun them
   over partially authored content.
2. Run `Truck Taxi/Regional/Author Stadium, Concert and Mountain Pass`
   (`Editor/TruckTaxiMegaWorldAuthoring.cs`). This adds three EasyRoads-backed
   Scene Streamer chunks, persistent ride stops/POIs/road graph and UTS access
   lanes, venue walking loops, optional bus shuttles, and the venue catalog.
   It appends only its new scenes to Build Settings.
3. Run `Truck Taxi/Regional/Finish Mega World Integration`
   (`Editor/TruckTaxiMegaIntegrationAuthoring.cs`) to author persistent
   tornado, hurricane, earthquake, avalanche, and mudslide zones and ensure
   typed map icons.
4. Run `Truck Taxi/Regional/Rebuild Map Tiles`
   (`Editor/TruckTaxiMapTileBaker.cs`). This bakes fallback bitmap tiles from
   the `Taxi_*` scenes and persistent region bounds. Repeat after changing
   venue geometry or chunk extents; the world authoring step does not bake tiles.

New scenes: `Regional/Scenes/Taxi_SportsStadium.unity`,
`Regional/Scenes/Taxi_ConcertVenue.unity`, and
`Regional/Scenes/Taxi_MountainPass.unity`. Persistent chunk policy lives in
`Regional/Taxi_StreamingManifest.asset`. The existing
`Regional/Scenes/Taxi_ThunderBowlSpeedway.unity` is reused, not recreated.

## Tuning

| Subject | Edit here | Current source defaults / behavior |
| --- | --- | --- |
| Work Area and dispatch | `ScriptableObjects/TruckTaxi_DemoConfiguration.asset`; `Scripts/TruckTaxiRideWorkArea.cs`, `TruckTaxiSession.Dispatch.cs` | Default radius 500 m; map slider 100-4000 m. Anywhere Near Me, current district, current town, and custom modes restrict pickup only. Demand dispatch ranges in real seconds: very high 5-20, high 10-35, normal 25-75, low 60-150, very low 150-300. |
| Intercity | Same configuration; `Scripts/TruckTaxiRideWorkArea.cs` | Random eligible chance 10%, then three completed local rides after an intercity; after five completed locals without one, the next routable eligible offer is guaranteed intercity. Config fields: `intercityRideChance`, `localOffersAfterIntercity`, `intercityDryStreakThreshold`. Declines do not consume the guarantee. |
| Rivals | `Scripts/TruckTaxiRivalPopulation.cs` component | 12 logical rivals, maximum three live, 290 m materialization / 410 m release radii. Existing UTS traffic adapter owns physical NPC vehicles; rival riders and fares are presentation state, not player session requests. |
| Calendar and forecast | `ScriptableObjects/TruckTaxi_EnvironmentSettings.asset`; `Scripts/TruckTaxiCalendarWeatherService.cs` | Start 2026-06-01 at 16:00; 30 game seconds per real second; forecast seed 71923, seven in-game days, weather periods 2-5 in-game hours. Change date/time scale here, not from the PC clock. |
| Venues and events | `Resources/TruckTaxi/TruckTaxiVenueCatalog.asset`; `Scripts/TruckTaxiVenueDefinition.cs`, `TruckTaxiVenueRuntime.cs` | Union Field Saturday 15:00-18:00 at 1.6x fare, Neon Yard Friday 20:00-23:00 at 1.75x, Thunder Bowl Sunday 17:00-20:00 at 1.5x. Each has two-hour arrival/departure windows, 30-minute announcement lead, and 1.7x ride-frequency factor. Edit event dates, recurrence, windows, attendance, traffic/crowd, and fare multipliers in the catalog. |
| Hazards | Persistent `TruckTaxiHazardZone` components on the host; `Scripts/TruckTaxiHazardZone.cs`, `TruckTaxiExtremeEventDirector.cs` | Stable zone/region IDs, exposure type, radius and ordered world-space path are required. Slide paths need summit, slope crossing and runout. Pinecrest crossing is `TruckTaxiVenueLayout.MountainSlopeCrossing` at `(1180, 8, -1540)`. Director defaults cap nearby visuals at three and debris piles at two. |

These are source defaults; inspect the two existing ScriptableObject assets in
the Editor before tuning. Their current YAML does not list every newer
calendar, Work Area, or intercity dry-streak field.

For event recurrence and occurrence-scoped severe-weather decisions, see
[TruckTaxi_VenueEvents.md](TruckTaxi_VenueEvents.md). Arrival demand weights
destinations; departure demand weights pickups. The session captures event,
weather, time-of-day, and intercity fare modifiers at acceptance. Weather
warning/emergency transitions can delay/cancel an eligible event occurrence;
later weekly occurrences remain scheduled. The forecast requests actual
Weather Maker conditions through the existing LWS environment service.

The runtime integration is `Scripts/TruckTaxiWorldCoordinator.cs`: it connects
the LWS clock, venue demand, session, UTS traffic/pedestrian/bus budgets,
rival convergence, alerts, and map markers. `TruckTaxiPlanningPanel.cs`,
`TruckTaxiWorkAreaMapPanel.cs`, and `TruckTaxiMegaDebugPanel.cs` are
presentation/debug consumers, not separate authorities. The pass hazards
leave a navigable road center; this source does not claim a routed road
closure or GPS detour.

## Authority boundaries

LWS owns dates, recurrence, work-area rules, fare/demand semantics, route
graph, stream metadata, and hazard coordination. NWH owns player truck
physics; UTS owns NPC traffic/pedestrian/bus motion; EasyRoads authors road
meshes; Pixel Crushers Scene Streamer loads chunks; Compass Navigator Pro
presents navigation and POIs; Weather Maker and Weatherade present atmosphere
and road accumulation; Heat presents production UI. Pixel Crushers Save and
Dialogue remain the existing persistence and dialogue frameworks. Use project
adapters and configuration rather than editing vendor source or adding
competing systems.
