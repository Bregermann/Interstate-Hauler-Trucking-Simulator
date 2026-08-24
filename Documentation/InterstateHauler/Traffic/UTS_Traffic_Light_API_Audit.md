# UTS Traffic Light API Audit

## Scope

This audit records installed Urban Traffic System traffic-light support for future prompts.

Traffic lights were not implemented in Stabilization 002.

## Installed Vendor Surface

Vendor root:

`Assets/UTS_FullPack`

Key scripts:

| Feature | Vendor Script | Notes |
| --- | --- | --- |
| Base semaphore behavior | `Scripts/Traffic Light/Semaphore Types/SemaphoreSystem.cs` | Abstract trigger/coroutine base for car and pedestrian light state. |
| One-way light cycle | `Scripts/Traffic Light/Semaphore Types/OneWaySemaphoreSystem.cs` | Single-flow semaphore cycle. |
| Standard intersection cycle | `Scripts/Traffic Light/Semaphore Types/StandardSemaphoreSystem.cs` | Standard crossroad cycle. |
| T-intersection cycle | `Scripts/Traffic Light/Semaphore Types/TSemaphoreSystem.cs` | T-crossroad cycle with car/pedestrian arrays. |
| Combined arrow cycle | `Scripts/Traffic Light/Semaphore Types/TogetherArrowSemaphoreSystem.cs` | Arrow/green coordinated cycle. |
| Vehicle light view | `Scripts/Traffic Light/View Semaphores/ViewCarSemaphore.cs` | Controls red/yellow/green/arrow visual states. |
| Pedestrian light view | `Scripts/Traffic Light/View Semaphores/ViewPeopleSemaphore.cs` | Controls pedestrian signals. |
| Movement-side trigger | `Scripts/Traffic Light/SemaphoreMovementSide.cs` | Sets allowed movement state for cars/bicycles/pedestrians near semaphores. |
| Scene light manager | `Scripts/Traffic Light/LightManager.cs` | Simple crossroad type flags. |

Key prefabs/assets:

| Asset | Path |
| --- | --- |
| Standard semaphore prefab | `Assets/UTS_FullPack/Models/Crossroad_prefabs/New Semaphore Prefabs/Standard Semaphore.prefab` |
| T semaphore prefab | `Assets/UTS_FullPack/Models/Crossroad_prefabs/New Semaphore Prefabs/T-Semaphore.prefab` |
| One-way semaphore prefab | `Assets/UTS_FullPack/Models/Crossroad_prefabs/New Semaphore Prefabs/One Way Semaphore.prefab` |
| Together-arrow standard semaphore prefab | `Assets/UTS_FullPack/Models/Crossroad_prefabs/New Semaphore Prefabs/Together Arrow Standard Semaphore.prefab` |
| Standard crossroad sample scene | `Assets/UTS_FullPack/Scenes/Standard_Crossroad1.unity` |
| T-semaphore sample scene | `Assets/UTS_FullPack/Scenes/T-Semaphore motion1.unity` |
| Vendor tutorial | `Assets/UTS_FullPack/PDF Tutorials/Tutorial 5 - Semaphore System.pdf` |

## Behavior Observed From Source

- `SemaphoreSystem` uses trigger enter/stay/exit around objects tagged `Car` and sets `CarAIController.INSIDE`.
- `CarAIController` and related movement scripts use semaphore distance/state fields such as `TO_SEMAPHORE`.
- `SemaphoreMovementSide` bridges visual semaphore state to movement permissions.
- View semaphore scripts expose visual state changes for car and pedestrian lights.
- Sample scenes include configured `distanceToSemaphore` fields on vendor path/movement components.

## LWS Integration Recommendation

Future traffic-light work should create an LWS adapter around vendor semaphore systems rather than editing UTS source.

Recommended project-owned concepts:

- `ILwsTrafficSignalService`
- `LwsTrafficSignalState`
- `LwsUtsTrafficSignalAdapter`
- `LwsIntersectionSignalDefinition`

The adapter should publish semantic signal state to traffic, dashboard/GPS warnings, and future world simulation without making UTS the global road authority.

## Deferred

- No traffic-light spawning.
- No intersection graph generation.
- No lane stop-line logic.
- No signal timing authoring.
- No traffic AI behavioral changes.
