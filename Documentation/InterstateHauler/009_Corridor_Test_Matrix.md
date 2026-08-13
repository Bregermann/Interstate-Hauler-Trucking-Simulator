# Prompt 009 Corridor Test Matrix

| Area | Test | Status | Notes |
|---|---|---|---|
| Spawn | Player truck appears in service area | Manual required | Uses existing `LwsPlayerTruckSpawner`. |
| Coupling | Dry-van trailer pickup | Manual required | Trailer spawn moved to service pad. |
| Merge | Entry ramp to northbound mainline | Manual required | Ramp exists in graph and builder. |
| Acceleration | Multi-gear acceleration | Manual required | Corridor length supports 18-speed testing. |
| Highway cruise | Reach and hold highway speed | Manual required | Mainline speed metadata is 65 MPH. |
| Lane change | Two lanes each direction | Manual required | Lane debug lines show approximate centers. |
| Braking | Highway-speed service brake test | Manual required | Road colliders must be verified in Editor. |
| Curve | Gentle sweeping curve | Manual required | Curvature intentionally truck-safe. |
| Grade | Mild grade | Manual required | About 17 m peak elevation variation. |
| Turnaround | Far apron/crossover | Manual required | Development turnaround, not final interchange art. |
| Road colliders | Tractor/trailer wheel contact | Manual required | EasyRoads mesh colliders requested, fallback colliders exist. |
| Mirrors | Left/right mirror road view | Manual required | Prompt 008 mirror stack preserved. |
| Dashboard | Speed/RPM/gear/lamp states | Manual required | Prompt 008 dashboard stack preserved. |
| G29 | Steering/pedals/shifter | Manual required | Prompt 005 objects retained in scene. |
| FFB | Road and curve feedback | Manual required | Requires physical G29. |
| 18-speed | Low/high range and split behavior | Manual required | Prompt 006 architecture untouched. |
| Controls | Signals, cruise, Jake, parking brake | Manual required | Prompt 007 architecture untouched. |
| Flip Off Driver | No-target gesture safe | Manual required | No traffic target yet by design. |
| Cab Life | Anchors and hula placeholder | Manual required | Prompt 008 cab anchor system untouched. |
| Aspect ratios | 16:9, 16:10, 21:9, 32:9 cockpit usability | Manual required | Prompt 008 owns cab scaling. |
| Automation | Runtime/EditMode/PlayMode C# builds | Passed | Dotnet project builds completed with warnings only. |
