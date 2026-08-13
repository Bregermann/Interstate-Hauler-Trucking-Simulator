# Prompt 011 - Route Maneuver Matrix

| Maneuver | Classification | Icon | Voice Slot | Current Corridor Testable | Notes |
|---|---|---|---|---|---|
| StartRoute | First route step | Text/dev map | Yes | Yes | Announces route start. |
| ContinueStraight | Angle <= straight threshold | Text/dev map | Yes | Yes | Mainline continuation. |
| SlightLeft | Small left angle | Text/dev map | Yes | Possible | Covered by tests. |
| SlightRight | Small right angle | Text/dev map | Yes | Possible | Covered by tests. |
| TurnLeft | Medium left angle | Text/dev map | Yes | Possible | Covered by tests. |
| TurnRight | Medium right angle | Text/dev map | Yes | Possible | Covered by tests. |
| SharpLeft | Sharp left angle | Text/dev map | Yes | Possible | Covered by tests. |
| SharpRight | Sharp right angle | Text/dev map | Yes | Possible | Covered by tests. |
| KeepLeft | Topology future | Text/dev map | Yes | No | Slot exists for future interchanges. |
| KeepRight | Topology future | Text/dev map | Yes | No | Slot exists for future interchanges. |
| MergeLeft | Topology future | Text/dev map | Yes | No | Slot exists for future merge logic. |
| MergeRight | Topology future | Text/dev map | Yes | No | Slot exists for future merge logic. |
| TakeRampLeft | Ramp class plus left geometry | Text/dev map | Yes | Possible | Ramp class is prioritized. |
| TakeRampRight | Ramp class plus right geometry | Text/dev map | Yes | Yes | Entry-ramp route can exercise this. |
| TakeExitLeft | Exit topology future | Text/dev map | Yes | No | Slot exists. |
| TakeExitRight | Exit topology future | Text/dev map | Yes | No | Slot exists. |
| ForkLeft | Fork topology future | Text/dev map | Yes | No | Slot exists. |
| ForkRight | Fork topology future | Text/dev map | Yes | No | Slot exists. |
| MakeUTurn | Large ramp/turn geometry | Text/dev map | Yes | Possible | Current crossover route can validate conceptually. |
| EnterRoundabout | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit1 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit2 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit3 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit4 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit5 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit6 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit7 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| RoundaboutExit8 | Roundabout topology future | Text/dev map | Yes | No | Slot exists. |
| Arrive | Final route step | Text/dev map | Yes | Yes | Arrival threshold is 35 meters. |
| DestinationOnLeft | Destination side future | Text/dev map | Yes | No | Slot exists. |
| DestinationOnRight | Destination side future | Text/dev map | Yes | No | Slot exists. |
| RouteRecalculating | Off-route event | Text/dev map | Yes | Yes | Triggered by reroute request. |
| RouteRecalculated | Reroute success event | Text/dev map | Yes | Yes | Triggered after successful recalculation. |
| OffRoute | Map matching failure | Text/dev map | Yes | Yes | Triggered after leaving tolerance. |
