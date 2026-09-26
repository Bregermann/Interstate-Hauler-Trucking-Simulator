# Truck Taxi Objective Audit

This pass extends the local Truck Taxi session, not Interstate career/jobs/persistence. The preceding pedestrian patch was independently revalidated (EditMode, PlayMode and actual Windows player) and committed as `65956fb2`. The prior GPS repair was preserved in the separately approved checkpoint `28f6ae94`.

## Authorities and Installed Assets

- NWH Vehicle Physics 2: unchanged tractor/input/handling and recovery.
- UTS FullPack: existing traffic paths, pedestrian population and jointed ragdolls. No pursuit or vehicle-destruction authority exists.
- Compass Navigator Pro 4: existing physical GPS and optional HUD map, cameras, route graphics and offer preview. LWS road graph/service computes routes.
- Heat: existing runtime buttons, switches and sliders. Unity Input System/EventSystem supplies navigation. TMP supplies text.
- Pixel Crushers Dialogue: existing passenger bark/audio adapter. Chatterbox audio assignments remain untouched.
- EasyRoads: existing baked street geometry unchanged. New small optional-stop paved bays are project-owned validation content.

## Inventory

See [TruckTaxi_ObjectiveContentInventory.md](TruckTaxi_ObjectiveContentInventory.md) for every request asset, authored deadline, base target, capability/behavior tags and exact passenger references. This is generated from actual assets, not inferred from flavor text.

Stable IDs use `taxi.objective.<enum>`. There were **11 real request types** before this pass; now **12**. `ScenicRoute` retains its serialized enum value but means Scenic Stop. `IllicitStop` is appended, never inserted. Every type below has a runtime evaluator. Random eligibility additionally requires explicit passenger references, actual capabilities, an eligible stop where applicable, remaining world targets, and whole-ride compatibility.

| ID suffix / player name | Authoritative requirement / source | Failure / deadline | World evidence | Conflicts |
|---|---|---|---|---|
| FastDelivery / Arrive within N seconds | Arrive after boarding before generated integer Target seconds | Deadline or ride failure | Session timer and existing destination | Explicit authored forbidden tags only; clean/shortcut valid |
| Shortcut / Use N different shortcuts | N distinct `Shortcut` target IDs from authored trigger crossing above minimum speed | Request deadline | Enabled explicit `ShortcutTrigger` colliders | Explicit forbidden tags; clean driving valid |
| RamTraffic / Ram N traffic cars | N distinct actual traffic collision IDs above impact threshold | Request deadline | Existing active UTS cars + collision observer | Impact forbidden by clean/smooth |
| HitPedestrian / Hit N pedestrians | N distinct measured pedestrian strikes, current ragdoll/event guard | Request deadline | Existing UTS jointed pedestrians + hit observer | Impact forbidden by clean/smooth |
| PropertyDamage / Damage N roadside props | N distinct undamaged property targets knocked dynamic by collision | Request deadline | 24 authored movable construction barriers, not buildings | Impact forbidden by clean/smooth |
| Offroad / Drive offroad for N seconds | Accumulated seconds moving >1 m/s over an explicitly offroad surface | Request deadline | Downward surface sampling; paved roads/bays versus grass Ground | ForbidsOffroad metadata |
| SmoothRide / Arrive smoothly | No qualifying impacts and no acceleration/braking above authored limit since boarding | Impact, acceleration limit, deadline | Existing telemetry and collision observer | Impact requirements / explicit forbidden behavior |
| NoCollisions / Arrive without a qualifying collision | No qualifying impacts since boarding, not just since request assignment | Impact or deadline | Existing collision observer | Traffic/pedestrian/property impacts |
| MaximumChaos / Make N Chaos points | Current ride Chaos >= integer Target, rounded to 100-point steps | Request deadline | Existing Chaos scoring, at least one permitted scoring source | Reject if all available scoring sources prohibited |
| ScenicRoute / Scenic stop: enjoy view for Ns | Continuous stationary time at assigned scenic point; exact point duration is Target | Leaving/moving resets progress; deadline fails | Six routable authored scenic bays | Another simultaneously active timed stop |
| NearMiss / N close traffic passes without contact | Distinct traffic IDs at sufficient player AND relative speed, exit proximity without collision; target cooldown | Request deadline | Existing traffic + proximity/contact observer | Explicit forbidden high-speed tags; clean driving valid |
| IllicitStop / Sketchy pickup: wait Ns | Same shared timed-stop evaluator, abstract fictional handoff | Leaving/moving resets progress; deadline fails | Six routable authored sketchy bays | Another timed stop |

Arrival requests validate their full boarded interval. A request cannot be assigned retroactively after the ride has already violated its clean/smooth contract. All other requests use their authored deadline (typically 180 seconds); the HUD shows remaining seconds. Timed stops use 12 seconds scenic / 8 seconds sketchy, speed <=0.447 m/s, radius 8 m. Rewards remain definition-authored fare cents, score and satisfaction; sketchy stops add 50 Chaos once.

### Ideas Which Are Not Implemented Objectives

Follow Vehicle, Ram Target Vehicle, Lose Vehicle, Block Vehicle, Reach Location Before Vehicle, Destroy Vehicle and Collect Dropped Objects have **no request enum, evaluator, world target registry or authored request assets**. Related passenger flavor text is not gameplay support. Their capability bits stay absent and they are unavailable for random selection. A traffic ram is not vehicle destruction. No fake completion substitutes were added.

Passenger unique mechanics remain separate from requests: SportsCommentator, FeeCollector, PowerLevel, PropertyDestructionCat, StyleCombo, TimeObsessed, DesignAnalyst, NeverTips, DestinationChanger, BackseatDriver, Oversized and DashboardCompanion. They are not duplicated as mission types.

## Selection and Targets

`TruckTaxiObjectiveCapabilities` records runtime evidence; `TruckTaxiSession.CanAssign` and `ValidateCombination` validate the complete requested set. Stable ID duplicates are rejected, even when cloned assets differ. An explicit multi-instance definition must have distinct nonempty targets. Conflicting candidates are excluded without rerolling passenger, offer or destination. Resolved requests remain in the ride's semantic compatibility history, but a different timed stop may follow a completed stop; only one active stop owns GPS at a time. Count targets cannot exceed the available distinct world population. Ragdoll/already-hit pedestrians and damaged props are excluded from fresh support counts. Non-stop specific-target missions remain unavailable until a real identity resolver exists.

`TaxiRequestProgress.Target` is the one requirement used by description, progress denominator and completion. Counts and seconds are integers. Chaos difficulty is applied once and rounded to clean 100-point steps: the former 340.9 requirement becomes 300 and every surface says 300. At difficulty 1, 400 remains 400. No separate display target exists. Request-introduction fallback text also uses the generated description instead of repeating the unscaled authored base target. Existing authored voice clips remain intact.

## Optional Stops

`TruckTaxiStopObjectivePoint` supplies stable identity, category, display name, district, position, radius, duration, speed threshold, view direction, passenger-tag restrictions, dialogue and Chaos reward. `TruckTaxiSession.Tick` is the single evaluator. `TruckTaxiOptionalStops` only presents a distinct ground ring/label/progress, passenger look gesture/dialogue and existing GPS detour. Completion/failure restores the current final destination, including destination changes made during a stop. It does not create a new route solver, minimap or trigger framework.

The six scenic bays are West skyline, Western park, Northwest road, North skyline, North park horizon and Northeast cityscape. Six sketchy bays are Warehouse side, East service, Industrial exchange, South loading, Quiet package and Backstreet meeting. All are fictional city locations with truck-sized paved space, not real-world procurement locations. No time-of-day restrictions are currently authored. Future stop categories are vocabulary only, not working food/collection systems.

Every bay has an explicit endpoint/access leg in the existing LWS road graph, using the existing taxi-bay edge authoring helper. Roads are not regenerated. The targeted `Truck Taxi / Update Optional Stops And Objectives` command is idempotent and preserves existing authored stop transforms. It adds missing bays, updates semantic references and validates routes/ground. Use this targeted command after intentionally regenerating the demo city; the old full-city regeneration tool is not a content-preserving editor.

Regression checks also corrected the taxi distance adapter's handling of endpoints snapping to the same graph node: the planner returns no-edge failure, but those nearby endpoints still have a valid short local access leg. The taxi adapter recognizes this specific case; the shared Interstate route planner was not changed. A physical cab route depth check required 10 mm rather than 4 mm clearance from the existing screen face; `TruckTaxiGPSAdapter.dashboardScreenClearance` exposes that small taxi-only correction. Screen dimensions, cab anchors, vendor material and the two existing Compass instances remain unchanged.

## Optional Adult Choice

Bree Brightside (`p004`) is explicitly authored as a 21+ adult woman with flirtatious presentation and special-appreciation opt-in. Eligibility requires all those explicit fields plus human casting, excellent projected five-star performance, satisfaction >=4.5 and a configurable chance (default 15%). Other profiles default ineligible; Miss Mabel is not inferred eligible. No ambiguous or underage casting is inferred from appearance.

At destination, an eligible offer waits for Accept/Decline. Decline changes neither satisfaction nor reward rules. Accept shows only a black fade and a comedic time-skip caption, then the ordinary exit/result flow. No explicit visual/audio activity or camera sequence. Choice is never automatic.

## Ratings and UI

Satisfaction remains continuous. Final stars use thresholds 1.5, 2.5, 3.5 and 4.5 to award integers 1-5. `TotalStarsEarned / CompletedRides` is the cumulative average, displayed to one decimal with midpoint-away rounding. Session stats remain local; no persistence framework was introduced. Active UI says PROJECTED; offer UI says PASSENGER RATING; result UI distinguishes THIS RIDE and DRIVER AVERAGE.

`TruckTaxiUIInput` owns one Input System UI map. Standard EventSystem navigation handles arrows/WASD, stick/D-pad and slider adjustment. Enter/South submits; Escape/East cancels or declines; Start pauses. Hints use binding display strings for the last used device. Each modal has contained explicit navigation and initial focus. GPS settings and upright recovery are reachable from Pause. F/View hold ejection is preserved. F8 debug is developer-only; it is not required for normal play.

Offers suppress driving but retain the existing unscaled 30-second expiry. GPS slider instances disable unused hidden numeric-entry fields, so focus visits only visible switches, sliders, color swatches, Defaults and Close. The existing EventSystem bindings are restored when Truck Taxi's UI owner is destroyed. No vendor source edits are needed.

The existing debug panel adds an objective browser, capability/conflict/support-count diagnostics, force/validate compatible set, rebuild capabilities and stop gizmos. TESTED means successfully completed in the current runtime session, not a fabricated QA stamp.

## Validation Record

- Validated September 25, 2026 with Unity 6000.4.10f1. Pedestrian checkpoint: successful EditMode, PlayMode and Windows collision/ragdoll regression before commit.
- Final EditMode: **72 passed, 0 failed, 2 skipped**. Includes 10,000 compatibility bundles plus 10,000 actual generated rides with varying capability sets, rating/target/fallback-text regressions, count supply limits, sequential-stop validation and same-node access regression. The two pre-existing opt-in audio-generation/preparation tests were skipped; no new Chatterbox generation was performed.
- Final PlayMode: **5 passed, 0 failed**. Includes the objective/no-mouse flow, three-ride regression, NWH low-speed U-turn, physical pedestrian ragdolls and GPS presentation/settings. The U-turn measured approximately 176 degrees in 9.55 m width at 7.2 MPH. The cab route rendering test measured cyan route pixels present when enabled and absent when disabled.
- Windows build: **Succeeded, 0 build errors**, 327,578,390 bytes. Rebuilt game at `Builds/TruckTaxiDemo/TruckTaxi.exe`; current managed runtime assembly was produced at 23:05 local time. Unity's reused player executable timestamp is not the timestamp of the newly built gameplay assembly/content.
- Actual rebuilt Windows objective run: **exit 0 / TRUCK TAXI OBJECTIVE STANDALONE PASS**. Exercised all 12 evaluators, 1,000 additional runtime-generated bundles, actual scene support/ground/route endpoints, keyboard and simulated gamepad UI, scenic and sketchy timers/dialogue/markers, GPS detour/return, 400-point Chaos threshold, integer 5/3/4-star results and cumulative average, adult offer decline/accept fade, hold ejection and next-ride availability.
- Actual rebuilt Windows pedestrian run: **exit 0 / TRUCK TAXI PEDESTRIAN STANDALONE PASS**. Two real tractor contacts at 4 and 12 m/s activated 11-bone UTS ragdolls, objective progress, Chaos, reactions and cleanup.
- Actual rebuilt Windows existing smoke: **exit 0 / TRUCK TAXI PLAYER SMOKE PASS**. Injected keyboard ignition/throttle drove 22.9 m through the existing NWH/input path; generated greeting played through the existing dialogue output with subtitle; cab and HUD GPS rendered; hold ejection, cleanup and next-ride availability passed.

Evidence is in `Builds/TruckTaxiDemo/Validation/`: `ObjectivesEditMode.xml`, `ObjectivesAllPlayMode.xml`, `ObjectivesWindowsBuild.log`, `ObjectivesWindowsSmoke.log`, `ObjectivesPedestrianWindowsSmoke.log`, and `ObjectivesRegressionWindowsSmoke.log`. Captured Windows images were inspected for GPS settings, scenic/sketchy rings and timers, Chaos `32/400`, integer ride/decimal average results, non-graphic fade and cockpit GPS. No runtime exception/check-failure entries were found in the three final player logs.

### Per-Objective Coverage and Limits

All 12 authored request types are enabled, capability-gated and have passing EditMode plus PlayMode/Windows runtime evaluator coverage. None was removed. Scenic Stop replaces the old drive-through behavior; Illicit Stop is the one new request type. Special Appreciation is a separate explicit post-ride choice, not a randomly assigned request.

| Objective | Additional integrated evidence | Still not claimed |
|---|---|---|
| FastDelivery | Runtime boarding/arrival timer evaluation | Every city journey hand-driven within deadline |
| Shortcut | Authored trigger support audited; semantic crossing event evaluated | Every shortcut physically driven this pass |
| RamTraffic | Real PhysX contact against a typed traffic fixture in PlayMode | Manual traffic-ram playthrough in EXE |
| HitPedestrian | Real NWH tractor impacts and UTS ragdolls in PlayMode and EXE | Physical controller hardware driving |
| PropertyDamage | Real PhysX contact against a typed property fixture in PlayMode | Building destruction or vehicle destruction |
| Offroad | Actual paved/grass raycast classification; accumulated evaluator timing | Manual offroad route traversal |
| SmoothRide | Arrival success and acceleration/impact rejection tests | Subjective handling acceptance |
| NoCollisions | Full-boarded-interval evaluation and contradiction tests | Every possible collision geometry |
| MaximumChaos | EXE screenshot `Make 400 Chaos points` / `32/400`, completion at 400 | Voice regeneration of existing authored clips |
| ScenicRoute | EXE zone/timer reset, dialogue, marker, completion and GPS return | Hand-driving all six bay approaches |
| NearMiss | Runtime semantic evaluator plus inspected speed/contact/cooldown observer | Real near-miss drive-by executed this pass |
| IllicitStop | EXE zone/timer reset, dialogue, marker, completion and GPS return | Hand-driving all six bay approaches |

Keyboard/gamepad events were injected through Unity Input System; no pointer clicks were used by the objective flow and no physical gamepad hardware test is claimed. Stop fixtures teleport and hold the tractor for deterministic timing. Developer-only debug tooling is not fully converted to gamepad navigation. New stop bays are functional demo content, not a final city-art pass. Target-vehicle missions, vehicle destruction and collectibles remain unsupported/unselectable. The full-city regeneration tool remains destructive by design; use the targeted updater for these additions.

No vendor source modifications. No new save system, navigator, vehicle controller, spreadsheet or voice pipeline.

## Authoring and Tuning

- Request assets: `Assets/LWS/TruckTaxi/ScriptableObjects/Requests/`. Stable ID, enabled flag, capability/behavior requirements, base target, deadline, reward and dialogue are Inspector fields. Player quantitative text is derived from the actual generated target.
- Scene stops: `Taxi optional stop points` in `TruckTaxi_DemoCity`. Edit `TruckTaxiStopObjectivePoint` for category, stable ID, radius, duration, speed, view, passenger tags and dialogue. Move the authored point, then rerun the targeted updater to rebuild only its access links.
- Adult event: `Passengers/Profiles/p004.asset` (Bree Brightside), explicit age/eligibility fields, chance and minimum satisfaction. All other profiles are explicitly ineligible.
- Near misses: `TruckTaxi_DemoConfiguration.asset`, minimum player/relative speed 8 m/s, radius 5 m and per-car cooldown 12 s. Any contact disqualifies the proximity pass, including contact too weak to score a collision.
- UI bindings: `TruckTaxiUIInput`, one Input System map. Input tests inject actual Keyboard/Gamepad device events; no physical controller hardware claim is made.
- Test fixtures: `-truck-taxi-objective-smoke`, `-truck-taxi-pedestrian-smoke` and `-truck-taxi-smoke` are opt-in development-player commands. Stop probes teleport/hold the tractor for deterministic timing; this is not a claim of hand-driving every approach.
