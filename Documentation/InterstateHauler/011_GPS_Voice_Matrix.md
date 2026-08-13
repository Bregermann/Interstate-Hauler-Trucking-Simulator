# Prompt 011 - GPS Voice Matrix

Default voice pack:

`Assets/LWS/InterstateHauler/Navigation/Data/IH_GpsVoicePack_Default.asset`

All AudioClip assignments are currently empty by design. Empty clips are allowed; the slot must exist.

| Instruction Type | Voice Pack Slot | AudioClip Assigned | Trigger | Can Repeat | Settings Controlled | Notes |
|---|---|---|---|---|---|---|
| StartRoute | StartRoute | No | Route start | Force/event only | Yes | Route-start cue. |
| ContinueStraight | ContinueStraight | No | Step approach | Once per step | Yes | Mainline continuation. |
| SlightLeft | SlightLeft | No | Step approach | Once per step | Yes | Small left maneuver. |
| SlightRight | SlightRight | No | Step approach | Once per step | Yes | Small right maneuver. |
| TurnLeft | TurnLeft | No | Step approach | Once per step | Yes | Normal left turn. |
| TurnRight | TurnRight | No | Step approach | Once per step | Yes | Normal right turn. |
| SharpLeft | SharpLeft | No | Step approach | Once per step | Yes | Sharp left. |
| SharpRight | SharpRight | No | Step approach | Once per step | Yes | Sharp right. |
| KeepLeft | KeepLeft | No | Step approach | Once per step | Yes | Future interchange support. |
| KeepRight | KeepRight | No | Step approach | Once per step | Yes | Future interchange support. |
| MergeLeft | MergeLeft | No | Step approach | Once per step | Yes | Future merge support. |
| MergeRight | MergeRight | No | Step approach | Once per step | Yes | Future merge support. |
| TakeRampLeft | TakeRampLeft | No | Step approach | Once per step | Yes | Ramp maneuver. |
| TakeRampRight | TakeRampRight | No | Step approach | Once per step | Yes | Ramp maneuver. |
| TakeExitLeft | TakeExitLeft | No | Step approach | Once per step | Yes | Future exit support. |
| TakeExitRight | TakeExitRight | No | Step approach | Once per step | Yes | Future exit support. |
| ForkLeft | ForkLeft | No | Step approach | Once per step | Yes | Future fork support. |
| ForkRight | ForkRight | No | Step approach | Once per step | Yes | Future fork support. |
| MakeUTurn | MakeUTurn | No | Step approach | Once per step | Yes | U-turn/crossover. |
| EnterRoundabout | EnterRoundabout | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit1 | RoundaboutExit1 | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit2 | RoundaboutExit2 | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit3 | RoundaboutExit3 | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit4 | RoundaboutExit4 | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit5 | RoundaboutExit5 | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit6 | RoundaboutExit6 | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit7 | RoundaboutExit7 | No | Step approach | Once per step | Yes | Future roundabouts. |
| RoundaboutExit8 | RoundaboutExit8 | No | Step approach | Once per step | Yes | Future roundabouts. |
| Arrive | Arrive | No | Arrival threshold | Force/event only | Yes | Destination reached. |
| DestinationOnLeft | DestinationOnLeft | No | Future destination-side event | Once | Yes | Slot exists. |
| DestinationOnRight | DestinationOnRight | No | Future destination-side event | Once | Yes | Slot exists. |
| RouteRecalculating | RouteRecalculating | No | Reroute request | Force/event only | Yes | Recalculation start. |
| RouteRecalculated | RouteRecalculated | No | Reroute success | Force/event only | Yes | Recalculation complete. |
| OffRoute | OffRoute | No | Off-route detection | Event/cooldown | Yes | Off-route warning. |
| InOneMile | InOneMile | No | Future fragment | N/A | Yes | Optional distance slot. |
| InHalfMile | InHalfMile | No | Future fragment | N/A | Yes | Optional distance slot. |
| InQuarterMile | InQuarterMile | No | Future fragment | N/A | Yes | Optional distance slot. |
| InOneThousandFeet | InOneThousandFeet | No | Future fragment | N/A | Yes | Optional distance slot. |
| InFiveHundredFeet | InFiveHundredFeet | No | Future fragment | N/A | Yes | Optional distance slot. |
| Now | Now | No | Future fragment | N/A | Yes | Optional context slot. |
| Then | Then | No | Future fragment | N/A | Yes | Optional context slot. |
