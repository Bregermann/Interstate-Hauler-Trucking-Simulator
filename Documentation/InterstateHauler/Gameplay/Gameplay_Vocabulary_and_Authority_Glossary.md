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

## Gameplay Flow Terms

These are canonical gameplay-flow terms. Prompt 020 activates depot/job-board states; later states remain reserved vocabulary until their owning prompts.

| Canonical Term | Code Name | Expected Driving | Future Owner |
|---|---|---|---|
| AT_DEPOT | `AtDepot` | Yes | Active in Prompt 020 depot presence. |
| JOB_SELECTION | `JobSelection` | No | Active in Prompt 020 authored job board. |
| TRAILER_PICKUP | `TrailerPickup` | Yes | Activated after Prompt 020 job acceptance; Prompt 021 owns physical pickup. |
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
## Prompt 020 Depot and Authored Job Vocabulary

Prompt 020 activates depot/job-board gameplay on top of the Prompt 019 macro state service. Depot presence remains separate from gameplay state.

| Term | Canonical Meaning | Authority |
| --- | --- | --- |
| Depot | A named trucking gameplay location where activities may occur: job board, pickup, delivery, services, parking, or future company facilities. A depot does not own jobs, money, reputation, save files, or macro state. | LWS depot semantics |
| Depot Definition | Stable authored ScriptableObject data for a depot. It stores `StableDepotId`, display name, world identity, and whether the depot has a job board. | `LwsDepotDefinition` |
| Depot Runtime Instance | Scene-side depot representation with trigger bounds and interaction references. It is not the persistent depot identity. | `LwsDepotRuntime` |
| Depot Presence | Whether the canonical player tractor is currently inside a registered depot gameplay area. Trailer-only collider entry does not count. | `ILwsDepotService` |
| Destination | A stable semantic trucking destination referenced by authored jobs. It is not merely a Transform. | LWS destination data |
| Destination Definition | Stable authored ScriptableObject data for a destination. Prompt 023 expands authoring workflow. | `LwsDestinationDefinition` |
| Job Definition | Developer-authored ScriptableObject source content for a potential haul. It is not mutable player progress or a UI row. | `LwsJobDefinition` |
| Job Catalog | Runtime-safe collection of authored job/depot/destination assets. It does not use `AssetDatabase` at runtime. | `LwsJobCatalog`, `ILwsJobCatalogService` |
| Job Offer | Runtime representation of an authored job currently eligible for a job board. It is derived from a Job Definition. | `LwsJobOffer` |
| Job Offer Provider | Service that produces current offers from authored content. | `ILwsJobOfferProvider` |
| Authored Job Offer Provider | Production provider that filters `LwsJobDefinition` assets by current depot. It does not randomize jobs. | `LwsAuthoredJobOfferProvider` |
| Job Board | Player-facing UI for browsing and accepting authored job offers at an eligible depot. | `ILwsJobBoardService`, `LwsJobBoardPresenter` |
| Job Board Service | Coordinates current depot, authored offers, selection, acceptance, and state transitions. It does not generate jobs or write save files. | `LwsJobBoardService` |
| Active Job | Persistent player-specific snapshot created when the player accepts an offer. It stores stable semantic job values and source provenance. | `LwsActiveJob` |
| Active Job Service | Single authority for the current accepted job. Prompt 020 supports one active job maximum. | `ILwsActiveJobService`, `LwsActiveJobService` |
| Job Status | Persistent job lifecycle vocabulary such as `AwaitingTrailerPickup`, `HaulActive`, `Delivery`, `Completed`, `Failed`, and `Abandoned`. | `LwsJobStatus` |

Prompt 020 status updates:

- `AT_DEPOT`: ACTIVE / IMPLEMENTED IN PROMPT 020. `AllowsDrivingInput = YES`.
- `JOB_SELECTION`: ACTIVE / IMPLEMENTED IN PROMPT 020. `AllowsDrivingInput = NO`.
- `TRAILER_PICKUP`: ACTIVE STATE ENTERED AFTER JOB ACCEPTANCE. `AllowsDrivingInput = YES`. Physical pickup gameplay belongs to Prompt 021.

Depot/job-board flow:

```text
                 DEPOT PRESENCE
                       |
                       v
             LWS GAMEPLAY STATE
                       |
             +---------+---------+
             |                   |
             v                   v
         AT_DEPOT          JOB_SELECTION
                                  |
                                  | ACCEPT
                                  v
                          LwsActiveJobService
                                  |
                                  v
                          TRAILER_PICKUP
```

Authored content flow:

```text
LwsDepotDefinition

LwsDestinationDefinition

LwsJobDefinition
        |
        v
LwsJobCatalog
        |
        v
LwsAuthoredJobOfferProvider
        |
        v
LwsJobBoardService
        |
        v
PLAYER
        |
        v
LwsActiveJobService
```

Scriptable Sheets role: `LwsDepotDefinition`, `LwsDestinationDefinition`, `LwsJobDefinition`, and `LwsJobCatalog` are normal serialized ScriptableObjects intentionally shaped for Scriptable Sheets table editing. Prompt 023 owns the high-volume Scriptable Sheets job and destination authoring workflow.
