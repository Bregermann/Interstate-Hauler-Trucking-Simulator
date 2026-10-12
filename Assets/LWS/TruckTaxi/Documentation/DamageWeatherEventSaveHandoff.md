# Damage, Weather And Event Save Checkpoint

## Video Ownership Checkpoint

Main subsequently authorized TruckTaxiVideoSettings.cs, TruckTaxiEffectPresentation.cs and focused tests.
The earlier read-only video findings are now addressed in source:

- Clones are keyed by quality slot AND source pipeline. Gameplay explicitly uses the existing authored
  Taxi weather pipeline, retaining its renderer features; menu clones cannot replace that renderer.
- Every ApplyStartup rebinds the saved render properties after Environment initialization (without
  reapplying display dimensions after the first startup call).
- Original quality-slot pipeline bindings are captured BeforeSceneLoad, before Environment overrides.
  Owned bindings are restored before clones are destroyed, including inactive quality slots. Cleanup
  runs on gameplay-scene unload, application quit, subsystem reset and Editor play-mode exit. The
  retiring guard prevents effect teardown from recreating camera ownership after cleanup.
- ApplyScene includes inactive Game cameras. beginCameraRendering supplies the same current preference
  to cameras created later. Render-texture and non-Game cameras are excluded, including GPS capture.
- Applied, unconfirmed settings are the live preference authority until confirm/revert. Capture does
  not replace that preference with stale PlayerPrefs values.
- Effects no longer force post-processing on or restore an obsolete boolean. They respect the live
  preference and restore only their own volume-layer bit, preserving other mask edits. Camera data
  created for presentation is owned/cleaned by video settings, not destroyed by a completed effect.

Added Tests/EditMode/TruckTaxiVideoOwnershipTests.cs (six tests): authored renderer/source immutability,
menu-to-weather clone selection, multiple-slot cleanup/idempotence, inactive/late camera inheritance,
effect preference/mask ownership, and render-texture camera exclusion. Tests use installed URP types
via reflection to avoid changing the shared test assembly references. No PlayerPrefs writes.

Parent's WorldLife PlayMode run passed 4/5, with a manager lookup error during the second scene lifecycle.
Its log references the OLD VideoSettings implementation at line 97. That version left weather clones
bound after Environment's conditional pipeline restoration stopped matching. The new ownership cleanup
closes that concrete cross-scene failure path. This is NOT a claim that the reported runtime failure is
proven fixed: rerun the five-test suite. No vendor singleton resets, vendor source edits, ignored errors
or log-suppression assertions were introduced.

## EditMode2 Follow-Up

The remaining null-extension test exposed a declared null environment surviving root normalization;
normalizing its synthetic snow then made the default environment appear valid. Exact inline-null
normalization now applies irrespective of declaration flags. Flags remain intact, so payload
validation rejects declared-but-missing data. Added three explicit rejection tests for damage,
environment and extremes. No runtime/test execution here.

Read-only coordination: EffectPresentation.BindCamera forces renderPostProcessing=true each active
effect frame; RestoreCameras restores the cached pre-effect boolean (or destroys camera data it
created). Video preference changes during an effect therefore get overwritten both during and
after the effect. Main/Pauli should own the policy arbitration and current preference baseline;
this workstream did not edit video/effect source.

## EditMode1 Compatibility Follow-Up

Parent's TaxiCorrection-EditMode1.xml exposed seven legacy preflight failures because JsonUtility
materialized null inline extension classes as default records. Read now normalizes ONLY Unity's
exact null encoding, following Session's existing optional-data pattern. New writes include presence
flags so a declared-but-missing extension cannot become legacy data. Nondefault malformed records
still fail validation. Snow has the same explicit presence protection.

The component roundtrip test had no host/truck identity: null VehicleId became empty string through
JSON and correctly failed ownership validation. The fixture now binds an inactive LwsPlayerTruck
with a stable identity and additionally verifies rejection of another truck's snapshot. No production
ownership comparison was weakened. Added coverage for omitted/inline-null legacy extensions and
malformed/declared extensions. Follow-up tests have not been run by this workstream.

Source checkpoint only. No Unity/editor/build/Windows execution or commit by this workstream.
The existing live-driving save/load transaction and native engine/transmission restore were not replaced.

## Root Causes And Changes

- Component damage multiplied steering lock by as little as 0.55, and wrote native wheel damage.
  NWH DamageHandler was a second mechanical damage writer. Taxi now configures its public
  visualOnly flag while bound, retains vendor deformation, owns mechanical condition, and keeps
  wheel Damage at zero. Tire grip loss is bounded to 8%, rolling penalty to 15%.
- The roadside summary read NWH DamageHandler.Damage, which is not the persisted component ledger.
  SummaryDamage01 derives a repair-value-weighted loss from that ledger. Heat now persists too.
  Existing v1 records remain readable with absent heat defaulting to zero.
- Low-speed lock is 70 degrees (was 65), response 400 (was 300), smoothing .025 (was .045).
  At 35+ mph the prior angle envelope, smoothing and 300-degree/sec response are retained.
  NWH remains the only steering/physics authority. A measured turning-circle result is still needed.
- Taxi forecast overrides/manual mode/frozen policy and local snow depths were missing from saves.
  Optional environment data now travels inside the existing Vibin participant. Clock, weather
  intensity/precipitation/transition and general road conditions remain canonical LWS providers.
  Taxi rebind does not issue a second weather transition after those providers restore.
- Snow saves include background depth, individual plowed/compressed cell depths and a
  translation-independent authored-layout signature. Mismatched layouts fail before live apply.
- LivePhysicsActorCount incorrectly made ambient extreme events unsaveable. That gate is removed.
  Atomic capture/recovery/teleport gates remain with specific reasons. Existing fare/occupancy,
  race and interaction policy is left for the owning workstreams.
- Active extreme events save global footprint coordinates, stable IDs, stages via absolute clock
  deadlines, real-time quake progress, future authored schedules, daily cooldown/dry-day counters,
  procedural sequence and the bounded deterministic RNG call count. Restore reuses vendor visuals
  and existing event creation logic; no actor rigidbodies, Unity references or custom disk files.

## Integration Hooks

Pauli has integrated TruckTaxiConditionPanel as the SINGLE WorldLife.OpenDamage/CloseDamage view.
The exact class/file is Scripts/TruckTaxiConditionPanel.cs. Its API is Initialize(host, hud),
Open(), Close(), IsOpen, FocusRoot, VisibilityChanged and HandleInput(TruckTaxiUIInput).
Do not add another HUD entry or inspection panel. It uses these stateless presentation APIs:

- TruckTaxiConditionPresentation.Summary(host): authoritative damage/heat and actual brake torque,
  fuel capacity, plus the applied Career engine multiplier (not a claimed measured horsepower value).
- TruckTaxiConditionPresentation.Part(host.ComponentDamage, stablePartId): condition/state,
  consequence, price and service requirement.
- TruckTaxiConditionPresentation.Consequence(category) for a separate consequence column.
- ComponentDamage.RepairBlockReason(id) and existing TryRepair(id); payment still uses Session.TrySpend.
- Replace roadside/HUD percentage reads of DamageHandler.Damage with ComponentDamage.SummaryDamage01.
  Do not persist a second summary percent or edit vendor Damage's private setter.

Main: keep HUD's IsCapturingPlayer guard. Source proves this flag spans the four-second physical
capture sequence, not the ambient encounter. Display its atomic reason rather than silently ignoring pause.
Pauli: no Session/occupancy DTO fields were touched. New fields are damage.Heat01, environment and
extremes in the owned Vibin participant. If editing its gates, preserve the load transaction and these hooks.

## Exact Source Files

All paths below are relative to Assets/LWS/TruckTaxi/.

- Scripts/TruckTaxiComponentDamage.cs
- Scripts/TruckTaxiVehicleHandlingOverride.cs
- Scripts/TruckTaxiCalendarWeatherService.cs
- Scripts/TruckTaxiSnow.cs
- Scripts/TruckTaxiEnvironmentCoordinator.cs
- Scripts/TruckTaxiEnvironmentSnapshot.cs (new)
- Scripts/TruckTaxiExtremeEventDirector.cs
- Scripts/TruckTaxiExtremeFootprint.cs
- Scripts/TruckTaxiExtremeSchedule.cs
- Scripts/TruckTaxiExtremeSaveState.cs (new)
- Scripts/TruckTaxiVibinSaveParticipant.cs
- Scripts/TruckTaxiVibinPersistence.cs
- Scripts/TruckTaxiConditionPresentation.cs (new)
- Scripts/TruckTaxiConditionPanel.cs (new; integrated through WorldLife only)
- Tests/EditMode/TruckTaxiDamageEnvironmentPersistenceTests.cs (new)
- Documentation/DamageWeatherEventSaveHandoff.md (this file)

## Validation And Limits

Added focused tests for component/heat/summary roundtrip and isolation; malformed/legacy damage;
low/high-speed curves; translated snow layouts and plowed depth; fail-closed snow data; forecast
override restoration after clock rewind; no duplicate vendor request; invalid environment data;
global hazard rebasing; and natural-event cooldown/dry-day restoration. Not executed here.
Parent must compile and run these, existing save/driving tests, and isolated Windows restart checks.
No Windows proof is claimed. Scoped git diff --check passed before final handoff edits; rerun at checkpoint.

Visual mesh deformation and Weatherade wheel-track render textures are not serialized; persisted
component damage, snow coverage and local compressed/plowed depth are authoritative gameplay state.
Kaiju ambient actors are not included in the extreme-director snapshot; only its atomic capture is
blocked. A separate Kaiju resume policy remains a content/lifecycle integration item if required.
Per-zone ad-hoc TryStartNatural attempt history is not serialized (the normal daily scheduler is).
The snapshot rebases global hazard paths on load; ongoing live floating-origin shifts of existing
hazard paths remain the existing director's responsibility and need a dedicated runtime check.
Existing serialized handling overrides may retain earlier inspector values; Main should verify the
runtime-created adapter defaults or invoke its existing ResetDefaults through the approved debug path.

## Vendor Audit

- VENDOR ASSETS AUDITED: installed NWH DamageHandler/Steering, LWS canonical Pixel Crushers-backed
  save participants, Weather Maker/LWS weather coordinator, Weatherade snow presentation.
- VENDOR ASSETS USED: the same NWH public configuration/effects, canonical save/clock/weather/road
  providers and Weatherade snow surface. Existing HUD/WorldLife owns UI.
- RELEVANT ASSETS NOT USED: none replaced; no alternate damage, weather, physics or storage framework.
- CUSTOM SYSTEMS CREATED: optional semantic DTOs/validation and stateless condition presentation only.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
