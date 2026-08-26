# Interstate Hauler - Gameplay Vocabulary and Authority Glossary

Prompt: 019 - Gameplay State Machine + Canonical Gameplay Vocabulary

This document is the canonical language source for macro gameplay state and cross-system authority boundaries. Future prompts must extend this document when they introduce a real new gameplay mode or responsibility boundary.

## Core Rule

A gameplay state describes the broad phase of gameplay currently happening. It does not implement the feature.

Example: `HAUL_ACTIVE` means the player is hauling freight. The job system owns cargo, pay, route requirements, delivery validation, and results. The gameplay state service only says the broad phase is active.

Future prompts may extend this glossary when a real new gameplay system or macro state is implemented. Do not invent large numbers of speculative states in advance. New states should be added only when a real gameplay system needs them.

## Gameplay State Terms

| Term | Meaning | Authority |
|---|---|---|
| Gameplay State | Broad macro phase of current gameplay. | `ILwsGameplayStateService` |
| Gameplay State Service | The one project-owned macro gameplay flow authority. | `LwsGameplayStateService` |
| Current State | The current macro state. Read-only to external systems. | `ILwsGameplayStateService.CurrentState` |
| Previous State | The immediately previous macro state after a successful transition. | `ILwsGameplayStateService.PreviousState` |
| State Transition | A controlled request to move from one macro state to another. | `TryTransitionTo(...)`, `Pause(...)`, `Resume(...)` |
| Transition Reason | Concise diagnostic text explaining why a transition was requested. | `LastTransitionReason` |
| AllowsDrivingInput | Broad gameplay-level permission for continuous driving input. | `ILwsGameplayStateService.AllowsDrivingInput` |

## Current Runtime States

| Canonical Term | Code Name | Driving | Meaning |
|---|---|---|---|
| INITIALIZING | `Initializing` | No | Gameplay runtime is booting and required services/world/player are not yet ready for normal driving. |
| LOADING_WORLD | `LoadingWorld` | No | Major save/world restoration or future world-load operation is in progress. Detailed Prompt 017 phases map here. |
| FREE_DRIVE | `FreeDrive` | Yes | Normal unrestricted driving/free-roam. A job is not required. |
| PAUSED | `Paused` | No | Player pause menu is open. Resume returns to the remembered previous gameplay state when safe. |
| TRANSITIONING | `Transitioning` | No | Short-lived broad transition that is not specifically save/load. Do not use as a dumping ground. |
| RECOVERY_ERROR | `RecoveryError` | No | Critical gameplay/load transition failed and the game must remain in recovery/retry/cancel flow. |

## Future Reserved States

These are canonical future vocabulary only. Prompt 019 does not implement their gameplay systems.

| Canonical Term | Code Name | Expected Driving | Future Owner |
|---|---|---|---|
| AT_DEPOT | `AtDepot` | Context-dependent/no by default | Prompt 020 depot implementation |
| JOB_SELECTION | `JobSelection` | No | Prompt 020/023 job UI/data |
| TRAILER_PICKUP | `TrailerPickup` | Yes | Prompt 021 trailer assignment/pickup |
| HAUL_ACTIVE | `HaulActive` | Yes | Job/haul systems |
| DELIVERY | `Delivery` | Context-dependent | Prompt 022 delivery/parking |
| DELIVERY_RESULTS | `DeliveryResults` | No | Economy/results/statistics systems |

## Authority Boundaries

| Term | Definition | Authority |
|---|---|---|
| Pixel Crushers Save System | Production save framework, serialization lifecycle, `SavedGameData`, and storer abstraction. | Pixel Crushers |
| `ILwsSaveService` | Thin LWS facade for profiles, player-facing slots, UI state, and semantic coordination. | LWS facade over Pixel Crushers |
| `LwsSaveLoadCoordinator` | Detailed save/load ordering coordinator for world preparation and safe application. | LWS ordering only |
| LWS Save Participant / Semantic Provider | Game-specific payload participant collected by the Pixel Crushers semantic saver bridge. | LWS semantic payloads |
| Global Position | Double-precision world-space gameplay position that survives floating-origin shifts. | LWS world origin service |
| Local Position | Unity scene-space `Transform.position` after applying current origin offset. | Unity scene runtime / LWS conversion |
| Floating Origin | Runtime system that shifts local scene coordinates to preserve precision over large distances. | `ILwsWorldOriginService` |
| Origin Offset | Double-precision offset between global coordinates and local scene coordinates. | `ILwsWorldOriginService.CurrentOriginOffset` |
| NWH Vehicle Physics 2 | Player tractor/trailer physics, wheel/suspension/drivetrain/trailer physics. | NWH |
| Player Truck | Canonical player-controlled tractor entity. | `ILwsPlayerVehicleService` plus NWH vehicle components |
| LWS Transmission Authority | 18-speed/range/splitter gameplay semantics and mode state. | `Lws18SpeedTransmissionController` |
| LWS Navigation Authority | Route semantics and route computation. | `ILwsNavigationService`, `LwsRoutePlanner`, `ILwsRoadGraphService` |
| Compass Navigator Pro | GPS/minimap/route presentation. | Compass vendor presentation via LWS adapter |
| LWS Semantic Weather | Game-facing weather preset/state. | `ILwsWeatherService` |
| Weather Maker | Sky, atmosphere, precipitation, fog, sun/moon/time presentation. | Weather Maker vendor systems |
| LWS Road Condition | Game-facing wet/snow/ice/grip semantics. | `ILwsRoadConditionService` |
| Weatherade | Wet/snow surface accumulation visuals. | Weatherade vendor systems |
| NWH Road Condition Adapter | Applies LWS road condition grip semantics to NWH vehicle behavior where supported. | LWS adapter over NWH |
| UTS | Ambient/NPC traffic behavior and population. | UTS vendor package via LWS adapter |
| EasyRoads | Production road authoring and physical road geometry. | EasyRoads vendor package |
| LWS Road Graph | Semantic road network for navigation, traffic metadata, and save/load world context. | LWS road graph services |
| Escape Pause Menu | Player pause/save menu surface. | `ILwsPersistenceMenuService` / `LwsPersistencePauseMenu` |
| F2 Weather Panel | Development weather and road-condition testing panel. | Existing development UI, not gameplay state authority |
| F3 Save / Load Menu | Direct save/load UI entry point. | Existing persistence menu service; load transactions map through Prompt 017 |

## Interaction Maps

Gameplay state is a broad signal consumed by focused systems:

```text
GAMEPLAY STATE
      |
      +--> INPUT
      |
      +--> UI
      |
      +--> AUTOSAVE ELIGIBILITY
      |
      +--> FUTURE JOB SYSTEM
```

Road conditions stay semantic and fan out to presentation/physics adapters:

```text
LWS ROAD CONDITION
       |
       +--> Weatherade visuals
       |
       +--> NWH grip
```

Navigation remains semantic first, with Compass presenting the route:

```text
LWS ROAD GRAPH
       |
       v
LWS ROUTE
       |
       v
COMPASS NAVIGATOR PRO
```

Persistence remains Pixel Crushers-owned:

```text
GAME SEMANTICS
       |
       v
PIXEL CRUSHERS SAVE SYSTEM
       |
       v
SavedGameDataStorer
```

## Future Prompt Requirement

Future prompts that introduce macro gameplay modes must:

1. Read this glossary.
2. Determine whether an existing state already represents the concept.
3. Extend the existing gameplay state service only if a genuinely new broad state is required.
4. Update this glossary.
5. Update the transition diagram in `019_Gameplay_State_Machine.md`.
6. Not create a separate global state manager.