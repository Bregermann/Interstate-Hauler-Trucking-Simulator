# Truck Taxi Custom Content

## Ownership

Designer assets are optional, content-neutral data. The existing `TruckTaxiRoadsideCompanionLoop` remains the event owner; `TruckTaxiCustomContentRunner` handles only its presentation/audio. NWH still owns physical vehicle state. Character clips run on temporary render-only copies, never on the vehicle Rigidbody, joints, controls or original occupant transforms.

No new navigation, save, companion or global state manager is introduced. Empty references are valid and retain the existing event fallback. No designer audio, animation or Timeline content is supplied in production configuration.

## Authoring

Open **Truck Taxi > Custom Content** (also available as **Private Event Content**).

1. Create an audio profile and assign imported AudioClips to Start, Loop, Accent and Completion arrays. Null entries are skipped. The Voices mixer is the default; a profile may override the mixer group.
2. Create a presentation profile. Assign a non-Legacy driver/passenger AnimationClip, an animation-only Timeline, a presentation prefab, or any supported combination. Empty values are allowed.
3. Create an interaction phase profile. Assign start/loop/completion presentations and audio profiles. Positive-weight variants optionally replace the loop presentation without immediate repetition.
4. Select the `TruckTaxiConfiguration` used by the scene and use **ASSIGN PROFILE TO PRIVATE EVENT**. Variant `-1` assigns the global fallback; `0` through `7` assign an optional roadside-companion visual-variant override. The runtime component can also supply a scene-authored variant override. These companions are separate from fare passengers, so the editor does not offer a disconnected fare-passenger assignment.
5. Enter Truck Taxi Play Mode and trigger the existing companion event normally. No extra trigger or controller reference is needed.

Playback path:

```text
Existing companion event
  -> companion variant override
     or TruckTaxiConfiguration.customCompanionInteraction
  -> runner.Begin / Beat / Complete / Cancel
  -> assigned audio and visual assets
  -> renderer, input, camera and event cleanup
```

Completion playback finishes before the companion's normal departure cleanup. Cancellation stops audio and Timeline graphs, destroys owned visual copies and restores each original renderer's previous enabled state. Starting another profile first cancels the previous one.

`optionalCustomInteractionProfile` supplies optional start audio/presentation fallback. Assigning it does **not** consume inventory or grant rewards. Item interactions require explicit player acceptance.

## Presentation Contract

- `driverAnchorId` and `passengerAnchorId` accept existing `TruckTaxiCabAnchorId` names. Blank keeps the current visual pose. An unavailable runtime mount logs a diagnostic and keeps the pose.
- Timeline animation output tracks must be named **Driver** or **Passenger**. They bind only to the corresponding visual proxy Animator. Other track types are rejected to prevent accidental changes to gameplay objects. Separate audio profiles handle sound.
- A presentation prefab may contain transforms, mesh filters, renderers and Animators. Collider, Rigidbody, custom behaviour and missing-script components are rejected. Remove a primitive cube's default collider before using it as presentation-only content.
- Timeline takes precedence over direct clips when both are assigned. Clips must match the source character rig/hierarchy. The runner retains its Animator placement/avatar and disables root motion on the visual copy.
- Duration and fade values are seconds. Fades reuse the existing HUD vignette. `LockInput` uses the existing event hold or Taxi pause for standalone previews/actions. Escape cancels playback and restores its prior state.
- `UseTruckRocking` requests the existing render-only vehicle presentation. It never applies forces or animates NWH physics.
- Audio uses at most two sources, optional overlap, finite pitch/volume limits, interval cooldowns and shuffle-without-immediate-repeat. Interior/exterior camera attenuation is configurable per profile.

## Item Interactions and Objectives

Create a `TruckTaxiCustomItemInteractionDefinition` and add it to the passenger's `customItemInteractions` list. Use an exact inventory stable ID, explicit optional age/gender/tags, a consume quantity and optional presentation/audio. The existing inventory UI presents valid authored actions; no action appears for an empty list.

The session validates eligibility before atomically consuming inventory and applying authored rewards. `oncePerRide` guards repeated acceptance. A presentation interrupted afterward does not refund an already accepted semantic action.

A `PassengerRequestDefinition` with type `CustomItemAction` listens for an exact `requiredCustomEventId` and optional exact `requiredItemStableId`. Its target is the number of distinct successful occurrences. The session's `RaiseCustomItemEvent` also supports other project-owned producers; duplicate occurrence IDs cannot advance a goal twice. Event IDs have no hardcoded content meaning.

## Preview and Validation

The editor page previews through the actual runtime runner, supports stop/cleanup, removes missing audio references and validates authored references. Item preview intentionally executes the real item action and therefore requires an onboard fare and inventory. Use disposable test state for it.

Automated fixtures create temporary beep, animation and cube content through these same fields. They do not assign anything to real passenger/configuration assets.

## Executed Validation (2026-09-30)

- Unity 6000.4.10f1 compilation: no script compilation errors.
- `TruckTaxiCustomContentTests`: 84/84 EditMode cases passed. Includes exact event IDs, optional item filters, inventory/currency validation, target feasibility, once-per-ride guards, reward atomicity and throwing/reentrant observers.
- `TruckTaxiCustomContentPlayModeTests`: 7/7 passed. Real runtime item acceptance completed an event-only objective; disabled runners, interrupted/reentrant playback, throwing cancellation observers and invalid duration fallback cleaned up correctly.
- `TruckTaxiCompanionLifecyclePlayModeTests`: 10/10 passed. Real companion `Interact`, `Tick`, completion and cancellation invoked profiles; a generated beep, non-Legacy wave clip, collider-free cube and actual Timeline `Passenger` AnimationTrack used the designer fields. Timeline rotation was measured on the proxy while tractor transform, collision and Rigidbody configuration remained unchanged. Variant override/global fallback and an unrelated auxiliary companion's input ownership were exercised.
- AssetDatabase inspection: one production Truck Taxi configuration, zero assigned custom companion profiles/variants, zero persisted custom-content profile assets. Test assets were temporary and destroyed by fixture teardown.
- Reports: `Logs/TruckTaxi-CustomContent-EditMode.json`, `Logs/TruckTaxi-CustomContent-ItemPlayMode.json`, `Logs/TruckTaxi-CustomContent-PlayMode.json`.

These are automated Unity PlayMode fixtures, not a manual full-route/cockpit visual inspection or a Windows player build. AudioSource playback was verified programmatically; human listening quality and future designer animation rig fit still require asset-specific preview. Earlier failing fixtures are retained in separate `Initial` reports; the passing reports above are the corrected runs.

Callbacks cannot prevent owned playback cleanup. Item debit/rewards commit before notifications; nested transactions are rejected. Standalone presentations acquire their own input hold even while an auxiliary companion is onboard. Escape cancels the owner of the current presentation, not an unrelated companion ride.

## Vendor Boundary

Audited/used: existing NWH player ownership, Unity Animation/PlayableDirector, AudioSource/mixers, existing Heat/uGUI HUD and the existing companion/cab-occupant adapters. Pixel Crushers, Compass and weather systems are not changed for this feature. No vendor source changes or duplicate vendor functionality.
