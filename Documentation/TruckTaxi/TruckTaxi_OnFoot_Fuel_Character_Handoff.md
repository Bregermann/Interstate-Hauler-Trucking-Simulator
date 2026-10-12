# Truck Taxi On Foot Fuel and Character Handoff

Date: 2026-10-09
Scope owner: this chat's on-foot, fuel, and character workstream
Integration owners: main for HUD, Bootstrap, MainMenu, central debug, and combined validation; Averroes for condition presentation and shared persistence coordination

## Status

Implementation and focused test authoring are present in the shared working tree. Main must perform the next Unity import, compilation, and test runs. This workstream has not run Unity, built the project, executed the added tests, made commits, or verified a Windows player. No live save/load or driving regression result is claimed.

The preservation baseline is commit `05871832`. No NWH driving authority, persistence framework, vendor source, main-owned HUD/Bootstrap/MainMenu, shared CareerPanel, scene, prefab, package, or project-settings file was edited by this workstream. Other workstreams have concurrent changes to some of those files; they must not be attributed to or reverted by this handoff.

## Condition Panel Ownership and Compile Blocker

**Averroes owns `TruckTaxiConditionPanel.cs` and `TruckTaxiConditionPresentation.cs`. This workstream owns their integration in `TruckTaxiWorldLifeController.cs`.**

Observed evidence:

- `Logs/TaxiCorrection-Compile1.log` reports CS0246 at `TruckTaxiWorldLifeController.cs(64,17)`: `TruckTaxiConditionPanel` could not be found.
- `Library/Bee/artifacts/1900b0aE.dag/LWS.TruckTaxi.Runtime.rsp` includes `TruckTaxiConditionPresentation.cs`, `TruckTaxiPlayerRagdoll.cs`, and `TruckTaxiWorldLifeController.cs`, but omits `TruckTaxiConditionPanel.cs`.
- At the review check, the panel source exists under `Assets/LWS/TruckTaxi/Scripts`, declares `LWS.TruckTaxi.TruckTaxiConditionPanel`, and has no preprocessor exclusion. Its `.meta` was absent.
- The current source exposes every panel API called by WorldLife. Both belong under the existing `LWS.TruckTaxi.Runtime.asmdef`; adding another assembly reference or a duplicate panel is not the appropriate repair.

Main/Averroes should settle/import the panel and its metadata, ensure it appears in the refreshed runtime compiler source list, then rerun Unity compilation. The existing response files and logs are evidence only: do not manually edit generated compiler inputs or create a competing condition class.

Current integration:

- WorldLife gets or adds one `TruckTaxiConditionPanel` on the HUD GameObject, calls `Initialize(host, hud)`, and subscribes to `VisibilityChanged`.
- The old WorldLife condition panel construction and `RefreshDamage()` renderer were removed.
- `OpenDamage()` opens the shared panel from pause without unpausing. Outside pause, the existing on-foot proximity and sight requirements remain.
- `DamageOpen`, `FocusRoot`, `CloseDamage()`, and `HandleHudInput(input)` forward to the shared panel.
- WorldLife unsubscribes from the visibility event on destruction.
- `InspectedPartId` captures the looked-at truck component. **The current shared panel's parameterless `Open()` does not consume it.** Averroes can read `host.WorldLife.InspectedPartId` when opening, or coordinate an optional selected-ID parameter. No private-field reflection or shared-file edit was introduced to fake this integration.
- Main should retain one WorldLife-owned creation route, or get/reuse the same HUD component if main later creates it. Do not independently add a second instance.

## Exact Owned File Inventory

All paths below are relative to `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator`.

| File | Work |
| --- | --- |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiUIInput.cs` | OnFootJump and CancelPickup semantic actions in existing map |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiWorldLifeController.cs` | Jump, traffic qualification/recovery, foot avatar integration, nozzle lifecycle, paused utilities, shared condition routing |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiPlayerRagdoll.cs` | New player lease on existing Taxi/UTS ragdoll; also contains runtime-added TruckTaxiPlayerTrafficContact relay |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiPlayerRagdoll.cs.meta` | New script metadata |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiFuelController.cs` | Per-nozzle dispensed gallons/cost, disabled-state guard and cleanup |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiFacilityInteractions.cs` | Reversible highlight on actual TruckTaxiFuelFiller renderers only |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiDriverAppearanceData.cs` | Additive chestShape field, validation, equality |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiDriverAppearancePanel.cs` | Existing Heat slider, draft preview, camera/render texture lifecycle |
| `Assets/LWS/TruckTaxi/Scripts/TruckTaxiDriverPresentation.cs` | Bounded existing adult chest-part adjustment; shared seated/on-foot appearance |
| `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiOnFootControlsTests.cs` | New semantic-input, impact-threshold and missing-provider tests |
| `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiOnFootControlsTests.cs.meta` | New test metadata |
| `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiDriverAppearanceTests.cs` | Shape persistence, legacy/default, validation, isolation, geometry tests |
| `Assets/LWS/TruckTaxi/Tests/EditMode/TruckTaxiFuelTests.cs` | Actual filler material restoration and idempotence |
| `Assets/LWS/TruckTaxi/Tests/PlayMode/TruckTaxiWorldLifePlayModeTests.cs` | New focused scene-level on-foot/fuel/appearance/recovery fixture |
| `Documentation/TruckTaxi/TruckTaxi_OnFoot_Fuel_Character_Handoff.md` | This handoff and review record |

`TruckTaxiConditionPanel.cs`, `TruckTaxiConditionPresentation.cs`, and their metadata are dependencies owned by Averroes, not files authored or edited by this workstream.

## On Foot Jump

### Root Cause

The existing CharacterController movement applied gravity and grounded downward velocity but had no jump input or positive launch velocity.

### Implementation and APIs

- `TruckTaxiUIInput.OnFootJump`: keyboard Space and gamepad West. South/E remains `WorldInteract`; no double jump/interact binding.
- `WorldLife.TryJump()`: returns false outside OnFoot, during modal input ownership, during ragdoll, or while ungrounded/already ascending.
- `WorldLife.IsGrounded`: existing controller's grounded state, with ascending velocity excluded.
- `jumpSpeed`: default 5.5 m/s; runtime clamped to 2-9 m/s with a finite-value fallback. This is launch velocity, not an added Rigidbody force/controller framework.
- Existing 20 m/s-squared gravity and 25 m/s terminal fall speed remain.
- Ceiling contact clears positive vertical velocity. Existing CharacterController slope limit and step behavior remain.
- `Jumped` event is an animation/presentation hook. No new jump animation asset was authored or claimed.
- Existing driving-input suppression remains acquired for the entire on-foot session and released on return.

Main can display `UIInput.Hint(UIInput.OnFootJump)` and expose `IsGrounded` through existing development diagnostics.

## Traffic Knockdown and Recovery

### Root Cause

The foot rig contained a CharacterController and camera but no player traffic-contact relay, jointed physics provider, or recovery coordination.

### Provider Reuse

`TruckTaxiPlayerRagdoll.Initialize(prefab, paths)` uses an explicitly assigned compatible prefab, otherwise searches the existing authored UTS paths' public `walkingPrefabs` field. It requires an existing `ITruckTaxiPedestrianRagdoll` provider, multiple rigidbodies, and joints. The intended existing assets are the project-owned `Taxi_Girl_11`/`Taxi_Girl_22` UTS variants.

The instance reuses `TruckTaxiPedestrian.TryStrike()`, `Reactivate()`, and `PrepareForPool()`. Its pedestrian update timer is disabled because WorldLife owns player recovery. It is not added to the population or scoring ledger.

UTS `NPCStats.EnablePhysics()` destroys root components. The existing reversible Taxi ragdoll implementation is deliberately reused instead of directly calling that destructive API or building another skeleton.

### Qualification and Bounds

- A small foot trigger calls `WorldLife.TryTrafficImpact(Collider)` on enter/stay.
- Requires `TruckTaxiImpactTarget.kind == Traffic`, an attached Rigidbody, a solid collider, approaching relative motion, and sufficient actual vehicle speed.
- The player's own truck, tiny bumps, stationary vehicles walked into, receding vehicles, modal ownership, and cooldown contacts are rejected.
- `trafficKnockdownSpeed`: default 2.5 m/s, bounded 1-8.
- Provider input velocity is capped at 30 m/s. Total player launch impulse is capped at 180 N s through existing pedestrian impulse calculation; multiplier 12, upward impulse 32, angular impulse 1.
- Existing provider body bounds also remain: max linear velocity 30 m/s, angular velocity 12 rad/s, and depenetration velocity 3 m/s.
- `ragdollRecoverySeconds`: default 3.5 seconds, runtime bounded 1-10. Uses scaled game time, so pause does not consume recovery time.
- Movement, jump and truck re-entry are disabled during ragdoll. The nozzle is returned.
- Camera follows the jointed body from a short third-person offset, checks obstruction while ignoring its own ragdoll, then restores the FPS local pose.
- Falling more than four meters below the last grounded position, non-finite position, or moving beyond a 40-meter radius forces recovery.
- Recovery checks a standing capsule on suitable static ground near the ragdoll, then at the last grounded position, then beside the truck. If no position is clear, it returns to the cab rather than leaving permanent ragdoll.
- A two-second post-recovery contact cooldown prevents immediate repeated knockdowns.
- Disable/return paths reset the provider and restore camera/listener/driving suppression ownership.

### Diagnostics

WorldLife exposes `IsRagdoll`, `RagdollAvailable`, `RagdollStatus`, `TryTrafficImpact(Collider)`, and the deterministic `QualifiesTrafficImpact(...)` predicate. The player adapter exposes `Ready`, `IsActive`, `Status`, `Position`, and `LastImpulse`.

If no compatible provider is configured, the adapter reports unavailable and refuses to fake a ragdoll. Current knockdown visuals are the existing UTS skinned model; they temporarily differ from the customized Wobble appearance. There is no death/health feature.

## Fuel and Filler Presentation

### Root Causes

The existing nozzle state supplied a text instruction but did not visually identify the actual filler. Fuel feedback showed cumulative lifetime fuel charges instead of the current nozzle session. Invalid carry/drive-away state lacked an explicit centralized nozzle return/highlight cleanup path.

The audited manual fueling path already had no parking-brake requirement. No nonexistent parking-brake check was claimed removed, and none was added.

### APIs and Behavior

- `WorldLife.ToggleNozzle(station)` picks up/returns the existing nozzle state; pickup requires a stopped truck in the station bay.
- `WorldLife.ActiveNozzleStation` remains the carry authority.
- `WorldLife.ReturnNozzle()` stops dispensing, clears that station, and restores highlighted filler materials.
- `WorldLife.CanPumpAt(station)` retains on-foot, modal, matching-station, hose-range, actual owned filler reach and sight checks.
- Default hose range remains 12 meters. `TruckTaxiGasStationPoint.CanRefuel` retains stationary speed, horizontal bay, vertical tolerance and active station checks.
- Moving/leaving the bay, excessive hose range, disabled station, ragdoll, re-entering the truck, WorldLife teardown, or FuelController disable ends the nozzle interaction.
- Pausing/opening a modal stops dispensing; merely pausing does not discard a still-valid carried nozzle.
- `TruckTaxiFuelFiller.SetHighlighted(bool)` and `Highlighted` operate on the actual filler subtree. Highlight materials are private clones; shared/authored materials are never mutated.
- Repeated enable is idempotent. Return/disable restores original material arrays and releases clones. No lights, new filler geometry, or constant material churn were added.
- `Fuel.BeginNozzleSession()` resets display-only `SessionGallonsDispensed` and `SessionFuelCharges`.
- Feedback reports added US gallons, session charge, tank gallons/capacity, and percent. Existing `TotalFuelCharges` remains intact.
- NWH `FuelModule.amount/capacity` still owns actual fuel. Existing Career/session wallet spending, fractional billing credit, consumption, and save paths remain.
- Existing hold-to-fuel interaction is retained. No physical hose simulation, new nozzle art, or separate attach animation was fabricated.

## Adult Character Appearance

### Root Cause

The existing adult driver was already generated with `clothedAdult = true` and separate `Left Clothed chest`/`Right Clothed chest` parts. Appearance data and UI exposed colors/cap but did not vary those supported parts.

### Persistence Contract for Averroes

`TruckTaxiDriverAppearanceData.chestShape` is an additive float in [-1, 1]. Zero is the old silhouette and the missing-field default. Appearance version remains 1. Non-finite/out-of-range values are rejected; `SameAs` includes the field and existing `Copy()` includes it.

The field stays within `TruckTaxiCareerProfileData.driverAppearance`. Existing `CaptureDriverAppearance()`, `TrySetDriverAppearance(...)`, Career equipment events, persistent session snapshots, and `RequestAutosave("Driver appearance")` remain the storage path. No PlayerPrefs or alternate save record was added. No shared Career/persistence file was edited by this workstream.

### Presentation Contract

- `DriverPresentation.SupportsChestShape` confirms the existing editable adult parts.
- `TryApplyAppearance(data, out reason)` preserves shirt/pants/trim/skin materials and cap/brim state while applying bounded shape changes.
- `ApplyVisualAppearance(Transform, data)` changes only supported existing parts. It uses absolute values, so repeated previews cannot accumulate scale.
- `CreateAppearanceVisual(parent)`, `SetOnFoot(parent, visible)`, `SetFootVisualVisible(visible)`, and `OnFootVisual` provide the standing player presentation without a second appearance authority.
- The foot visual shares the driver's owned materials. Head/eyes/cap/brim use the existing interior-hidden layer so the FPS camera does not clip through them.
- The appearance panel uses the configured Heat slider and a 256x384 render-texture preview with preserved 2:3 aspect. The preview stage is activated only for explicit renders; its camera, texture and stage are cleaned up on destruction.
- Slider range is [-1,1]. Missing Heat configuration reports an unavailable control rather than silently claiming it exists.
- Cancel restores saved appearance through the existing view logic; Apply commits the draft through Career.
- The adjustment is specific to the current clothed adult Wobble driver. It is not a generic skeletal breast morph, arbitrary clothing fitter, or a change to NPC/child models.

## WorldLife Pause Utilities and Service Ownership

- `OpenInventory()` supports an already-paused Driving/OnFoot caller without changing pause state. `UseSelectedInventoryItem()` permits that owned paused inventory route.
- `CloseInventory()` does not unpause. `FocusRoot` and `HandleHudInput()` keep its selection/close behavior accessible.
- Main's chosen Driver Inventory route is `EnvironmentNeeds.OpenInventory()`; the WorldLife API remains available and tested, not a request to redirect that menu.
- `OpenDamage()` routes to Averroes's single condition panel as described above. The panel itself preserves/restores its entry pause state.
- Existing `SetExternalModalFocus(bool)`, aggregate modal gating, cursor release, and the input-release-frame guard remain.
- Existing on-foot E/South interaction and nearby service paths remain. Main owns the new driving service E handling and service auto-open removal.
- No changes to the main-owned `TruckTaxiHud.cs`, `TruckTaxiBootstrap.cs`, `TruckTaxiMainMenu.cs`, central debug UI, or `TruckTaxiCareerPanel.cs` were made here.

## Pickup Cancel Follow Up

`TruckTaxiUIInput.CancelPickup` is now part of the existing action map:

```csharp
UIInput.CancelPickup.WasPressedThisFrame()
UIInput.Hint(UIInput.CancelPickup)
```

Default keyboard binding is **F6**, not Backspace. Backspace is already raw cruise cancellation; LB+East would also trigger the raw left indicator and parking brake. Input System composite consumption cannot suppress those direct hardware reads.

Gamepad pickup cancellation is intentionally unbound pending coordinated consumption in the driving input owner. Eject was not changed. Existing action-map enable/disable/disposal handles the new action. Main's current HUD source already consumes it for unpaused driving-to-pickup; no HUD edit was made here.

## Focused Tests Added or Extended

All are authored, not executed by this workstream. There are 13 added NUnit cases across these changes, including the pickup-cancel follow-up and filler lifecycle correction; existing tests were also extended.

### TruckTaxiOnFootControlsTests

- `PickupCancelUsesUnclaimedKeyAndExistingMapLifetime`: F6, map membership, hints, enable/disable, unchanged Cancel bindings, explicit absence of unsafe gamepad chord.
- `JumpBindingsDoNotReuseSouthInteraction`: Space/West jump, E/South interaction.
- `TrafficImpactRequiresMovingVehicleApproachingPlayer`: moving versus tiny/stationary/receding/matched-velocity/non-finite impacts.
- `MissingRagdollProviderDoesNotPretendToRagdoll`: unavailable provider refuses activation.

### TruckTaxiDriverAppearanceTests

- Existing snapshot round-trip and copy-isolation cases now include nonzero chestShape.
- Existing invalid-appearance parameterized test gains NaN, below-minimum and above-maximum shape cases.
- `LegacyMissingChestFieldPreservesOriginalShapeAndColors`.
- `ShapeOnlyEditUsesExistingCareerEventAndCopies`.
- `AdultClothedShapeIsVisibleBoundedAndDoesNotAccumulate`: actual supported Wobble geometry, idempotence, zero-shape restoration, cap/material preservation, unsupported-root refusal.
- Existing invalid-persisted and nullable-legacy tests remain.

### TruckTaxiFuelTests

- `ActualFillerHighlightRestoresMaterialsOnReturn`: material identity restoration, repeated return/idempotence, immediate clone disposal, and no authored material mutation.
- Existing stationary/same-level/in-bay checks remain.

### TruckTaxiWorldLifePlayModeTests

`ActualFillerHighlightRestoresMaterialsOnDisableAndDestruction`, category `TaxiOnFootFuelCharacter`, covers automatic component/GameObject disable, registry removal/re-enable, material restoration, deferred clone disposal, and component destruction while its renderer survives. This isolated fixture does not load DemoCity.

New `OnFootJumpPauseUtilitiesFuelLeaseAndTrafficRecovery`, category `TaxiOnFootFuelCharacter`:

- Loads DemoCity through the existing save-isolated introduction helper.
- Checks foot/seated shape agreement and cap preservation.
- Checks grounded jump, no immediate second jump and upward movement.
- Checks paused inventory/condition access without unpausing.
- Checks correct filler highlight and station-range loss cleanup.
- Calls the traffic impact API with a traffic-tagged Rigidbody, checks the real configured jointed provider, disabled controller, bounded impulse, recovery, camera restoration and driving suppression release.

This fixture invokes the impact API directly. It does **not** prove that a naturally moving UTS vehicle generated the trigger event. It checks nozzle carry/range cleanup, not a complete paid NWH dispensing cycle. The previous G-key exit/entry and facility lifecycle tests remain.

Suggested focused selection for main: `TruckTaxiOnFootControlsTests`, `TruckTaxiDriverAppearanceTests`, `TruckTaxiFuelTests`, then `TruckTaxiWorldLifePlayModeTests`. Main owns any expanded combined suite.

## Compile Readiness Review

### Integrated EditMode Failure Follow-up

The user's reported filler failure was found in `Logs/TaxiCorrection-EditMode1.xml` (the named `IntegratedEditMode1.xml` was not present). Its NUnit stack points to `TruckTaxiFuelTests.cs:55`, the `Highlighted == false` assertion immediately after `SetActive(false)`. All preceding explicit-return/material assertions were reached without a failure.

The supported cause is a fixture lifecycle mismatch: the EditMode test expected automatic `OnDisable` delivery from an ordinary MonoBehaviour without edit-mode execution. Production `TruckTaxiFuelFiller.OnDisable` already clears the highlight and calls the base unregister path. No production or vendor change was justified by this stack.

The EditMode test now covers explicit return plus repeated return, original material identity/color, and clone disposal. Automatic disable coverage was moved into a stronger PlayMode fixture described above, which also exercises component disable, re-enable, registry membership and destruction. No assertion was ignored, no lifecycle method was manually invoked, and no ExecuteAlways behavior was added to gameplay code. Main must rerun `TruckTaxiFuelTests` and the new PlayMode fixture; this workstream did not run Unity or claim a passing rerun.

Completed here:

1. Read current owned sources, public call sites, the runtime/test asmdefs, and current condition-panel dependency signatures.
2. Read main's existing Compile1 log and compiler source list. Confirmed the specific missing panel source-list entry.
3. Parsed 14 C# files using the installed Roslyn parser from SDK 9.0.314 with C# 9 language rules: 12 owned source/test files plus the two condition-panel dependencies. **Zero syntax errors.**
4. Scoped `git diff --check` passed before this handoff; final handoff delivery repeats the scoped check.

Not completed here:

- Semantic compilation, Unity import, assembly emission, EditMode/PlayMode execution, build, Windows acceptance, or fresh-process save/load validation.
- The reported Compile1 failure is still a failure; syntax parsing must not be described as fixing it or proving Unity compilation.
- No generated compiler input, Library asset, or shared condition-panel file was edited during this review.

Known integration gaps, in priority order:

1. Main/Averroes must import the condition panel and verify the next compiler source list contains it.
2. Averroes must wire `InspectedPartId` into the shared panel if automatic looked-at-component selection is required. The panel opens now but currently chooses its own first/default selection.
3. Gamepad pickup cancellation needs coordinated raw-driving input ownership before a chord can be enabled.
4. Player ragdoll uses the UTS visual, not the customized Wobble model; final consistent ragdoll appearance is not implemented.
5. Preview rendering, runtime material variants, actual traffic trigger delivery, slopes/ceilings, repeated impacts, teardown, and cross-launch persistence remain runtime acceptance work.

## Main Acceptance Checklist

1. Import settled files, verify condition-panel inclusion, compile once through main's existing workflow.
2. Run the focused fixtures above before the combined suite.
3. Exit the truck; test Space and West, slope landing, ceiling contact and denied air-jump. Pause/resume around jumping; re-enter and drive.
4. Use a real moving UTS car for a slow bump and high-speed knockdown. Test repeated knockdowns, pause/save/menu during recovery, world edge/fall recovery and later driving.
5. Pick up a nozzle with parking brake off while stationary in the bay; verify correct filler glows. Fuel, check gallons/wallet/tank, pause/release hold, exceed hose range, move truck, return nozzle and unload. Verify highlight restoration throughout.
6. Open pause Truck Condition from driving and on foot. Back must retain the prior pause state. Confirm one panel and correct controller focus.
7. Change chest shape and all color/cap controls in Career preview; cancel and apply separately. Inspect seated/on-foot appearance. Save, quit, relaunch and load with the existing disposable-profile policy.
8. During driving-to-pickup, use F6 and verify exactly one cancellation with no cruise/parking-brake side effect.
9. Explicitly re-run the baseline live save/load driving scenario. No source review substitutes for that regression evidence.

## Vendor Report

### VENDOR ASSETS AUDITED

- UTS Full Pack 2.0 (version recorded in the existing project ragdoll documentation): `Assets/UTS_FullPack/Scripts/People/NPCStats.cs`, `Scripts/Paths/WalkPath.cs`, and `Models/People/RagDoll Prefabs/Girl_11.prefab`; project variants `Assets/LWS/TruckTaxi/Prefabs/Pedestrians/Taxi_Girl_11.prefab` and `Taxi_Girl_22.prefab`; existing `TruckTaxiUtsRagdollAdapter` and `TruckTaxiPedestrian`.
- NWH Vehicle Physics 2: `Assets/NWH/Vehicle Physics 2/Scripts/VehicleController/Modules/Fuel/FuelModule.cs` and existing FuelModuleWrapper integration. Exact installed package version was not re-established.
- Heat: `Assets/Heat - Complete Modern UI/Scripts/UI Elements/SliderManager.cs` and existing configured slider prefab usage. Exact version was not re-established.
- Existing Unity Input System map and LWS driving hardware-reader mappings; existing Career/session persistence interfaces. Pixel Crushers remains the underlying save framework, not a newly audited/replaced storage implementation.

### VENDOR ASSETS USED

- UTS authored joints/bodies through the project-owned variants and existing reversible `TruckTaxiPedestrian` activation/reset path.
- NWH `FuelModule.amount`, `capacity`, `FuelPercentage` and existing module/wallet purchase integration.
- Heat `SliderManager` configuration plus Unity UI Slider, with its independent PlayerPrefs saving disabled.
- Existing InputActionMap lifecycle and existing Career save authority.

### RELEVANT ASSETS NOT USED

- Direct `NPCStats.EnablePhysics()`: destructive root-body/collider removal is unsuitable for repeated player recovery; existing reversible Taxi implementation is reused.
- No replacement character controller, ragdoll builder, save framework, traffic controller, or UI framework was introduced.
- No external art/audio/voice generation was used; the current supported adult Wobble geometry and supplied UTS rig remain the content foundation.

### CUSTOM SYSTEMS CREATED

Player-specific traffic-contact relay and bounded coordination around the existing ragdoll provider. Existing WorldLife, fuel, appearance, and input owners were extended; targeted tests and this handoff were added.

### VENDOR SOURCE MODIFIED

NO by this workstream. Pre-existing and other-workstream changes remain untouched.

### DUPLICATE VENDOR FUNCTIONALITY CREATED

NO. Vendor physics, traffic assets, fuel accounting foundation, UI controls and existing save authority remain in place.
