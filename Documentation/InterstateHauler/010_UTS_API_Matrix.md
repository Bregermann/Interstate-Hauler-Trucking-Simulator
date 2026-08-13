| Feature | UTS Class | UTS Property/Method | Native Support Level | LWS Adapter | Notes |
|---|---|---|---|---|---|
| NPC vehicle physics | `CarMove` | `Move(float accel, float footbrake, float handbrake, float steering = 0)` | Native | `LwsUtsTrafficApi` | Used only by UTS NPCs. |
| NPC AI controller | `CarAIController` | `MOVE_SPEED`, `INCREASE`, `DECREASE`, `TO_CAR`, `TO_SEMAPHORE`, `MaxAngle`, `GetBoxSize()` | Native | `LwsUtsTrafficApi` | Configured after spawn. |
| Path component | `CarWalkPath` | `moveSpeed`, `speadIncrease`, `speadDecrease`, `distanceToCar`, `nextPointThreshold`, `SpawnPeople()`, `SpawnOnePeople()` | Native | `LwsUtsTrafficApi` | LWS creates path points from road graph lanes. |
| Path data | `WalkPath` | `pathPoint`, `pathPointTransform`, `numberOfWays`, `lineSpacing`, `loopPath`, `DrawCurved()` | Native | `LwsUtsTrafficApi` | Accessed by reflected public/private members. |
| Runtime path follower | `MovePath` | `walkPath`, `InitStartPosition(int, int, bool, bool)`, `SetLookPosition()` | Native | `LwsUtsTrafficApi` | Added to spawned NPC vehicles. |
| Wheel references | `CarWheels` | `WheelColliders`, `tireMeshes` | Native | Detection only | Prefabs must include this to be spawnable. |
| NPC trailer helper | `AddTrailer` | `Init()`, `trailerPrefab`, `trailer` | Native | Detection only | Not enabled for Prompt 010. |
| UTS traffic light response | `SemaphoreSystem`, `SemaphoreMovementSide` | raycast/tag based checks | Partial | Deferred | Corridor has no complete traffic-light network yet. |
| LWS traffic identity | `LwsTrafficIdentity` | `Configure()`, `TrafficId`, `LaneId` | LWS | Native LWS | Adds `LwsVehicleIdentity` with `AiVehicle` role. |
| LWS registry | `ILwsTrafficService` | `RegisterTrafficVehicle()`, `UnregisterTrafficVehicle()` | LWS | Native LWS | Future gameplay consumes this instead of UTS scripts. |
