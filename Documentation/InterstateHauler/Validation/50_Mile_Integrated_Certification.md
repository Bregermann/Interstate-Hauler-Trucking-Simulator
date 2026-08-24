# 50-Mile Integrated Certification

## Scope

Primary scene:

`Assets/LWS/InterstateHauler/World/Origin/Validation/IH_50MileFloatingOriginValidation.unity`

The finite certification route is exactly 50.0 miles / 80,467.2 meters from Mile 0 to Mile 50. This is a validation and repair pass for the integrated highway stack, not a new gameplay-feature prompt.

## Systems Under Test

- NWH player truck and validation trailer
- Development automatic transmission with Drive/Neutral/Reverse selector
- Keyboard W/S automatic direction behavior
- 50-mile finite highway road, shoulders, ground, ditches, guardrails, median, semantic road graph, and traffic paths
- Scene streaming and additive 2-mile chunks
- Floating origin
- UTS traffic and time-based traffic demand
- LWS Game Clock
- Weather Maker visual weather and time presentation
- Weatherade road-condition presentation
- LWS road condition simulation
- GPS routing, cockpit GPS, HUD minimap, and full map
- Development Control Center

## Start State

Expected development start state:

- Start near Mile 0
- Game time starts at 8:00 AM through `LwsGameClockTuning.CreateValidationDefault`
- Weather starts in the first global mileage zone: Clear
- Automatic weather cycle is enabled
- GPS route to Mile 50 is requested on start
- Transmission development default is Automatic
- Automatic selector default is Drive
- UTS traffic seeds minimum local traffic presence after initialization

## Route And Chunk Layout

Automated model validation covers:

- Total distance: 80,467.2 m
- Chunk count: 25
- Chunk length: 3,218.688 m / 2 miles
- Chunk IDs: `IH_50MI_CHUNK_000` through `IH_50MI_CHUNK_024`
- Whole-mile markers: Mile 0 through Mile 50
- Exact chunk seam continuity at every boundary
- Bidirectional neighbor continuity at every boundary
- Final Mile 50 marker and turnaround pad are owned by chunk 024

## Teleport Certification Checkpoints

Use the Development Control Center or `LwsFiftyMileHighwayDebugPanel`:

| Checkpoint | Expected Chunk | Expected Weather | Expected Remaining |
| --- | --- | --- | --- |
| Mile 0 | `IH_50MI_CHUNK_000` | Clear | 50 mi |
| Mile 10 | `IH_50MI_CHUNK_005` | Overcast | 40 mi |
| Mile 25 | `IH_50MI_CHUNK_012` | Thunderstorm | 25 mi |
| Mile 40 | `IH_50MI_CHUNK_020` | Heavy Snow | 10 mi |
| Mile 49 | `IH_50MI_CHUNK_024` | Clear | 1 mi |

Each teleport uses global coordinates and asks the streaming service to reload the current neighborhood.

## Weather Cycle

Global mileage zones:

| Miles | Preset |
| --- | --- |
| 0-5 | Clear |
| 5-10 | Partly Cloudy |
| 10-15 | Overcast |
| 15-20 | Light Rain |
| 20-25 | Heavy Rain |
| 25-30 | Thunderstorm |
| 30-35 | Fog |
| 35-40 | Light Snow |
| 40-45 | Heavy Snow |
| 45-50 | Clear |

Weather Maker visual confirmation is still a normal Unity Editor manual requirement.

## Certification Report

When the controller reaches Mile 50, it logs:

`50-MILE CERTIFICATION COMPLETE`

Report fields:

- Distance
- Origin Shifts
- Origin Version
- Maximum Local Distance
- Largest Shift Duration
- Chunk Loads
- Chunk Unloads
- Meters Road Ahead
- Traffic Spawned
- Weather Transitions
- GPS Status
- Streaming Failures
- Streaming Last Error
- Runtime Errors

## Automated Validation Performed

Automated checks verify:

- Runtime, Editor, EditMode, and PlayMode C# assemblies compile.
- 50-mile constants are exact.
- 25 chunks are contiguous and unique.
- Every chunk seam is exact within tolerance.
- Whole-mile markers cover 0 through 50.
- Road graph solves an 80,467.2 m Mile 0 to Mile 50 route.
- Teleport checkpoints map to the expected chunks, weather zones, and remaining distances.
- Streaming policy keeps a bounded active neighborhood.
- Floating-origin validation offset keeps global mileage stable.
- Road graph provider accepts the 50-mile graph.
- Certification report and debug source expose required integrated metrics.

Automated validation does not prove that a human physically drove the route.

## Manual Full Drive Required

Run `IH_50MileFloatingOriginValidation.unity` in the normal Unity Editor and drive from Mile 0 to Mile 50.

During the drive, verify:

- No floating road or void under normal roadside.
- No wheel-catching road seams.
- Loaded road never ends under normal driving.
- Origin shifts occur repeatedly and truck/trailer/traffic/chunks remain aligned.
- Daytime traffic remains populated and lane-following.
- Weather Maker visual weather changes are obvious.
- Weatherade road-condition presentation remains active.
- GPS remaining distance is approximately 50, 40, 25, 10, and 0 miles at the expected points.
- Cockpit hides the HUD minimap and shows cab GPS.
- Exterior shows the HUD minimap.
- Full map opens/closes without changing the camera GPS policy.
- Development Control Center can open/close without corrupting driving input.
- Mile 50 reverse/drive W/S automatic selector behavior works with the trailer.
- Low-speed guardrail contact collides physically.
- Console does not accumulate repeating runtime errors.

Full pass status should be marked complete only after this manual drive is actually performed.
