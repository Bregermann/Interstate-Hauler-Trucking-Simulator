# Authored Trailer Morality Integration

## Parent Hook

After the existing HUD, UI input, Session and Player are initialized:

```csharp
var morality = GetComponent<TruckTaxiMoralityWorldController>()
    ?? gameObject.AddComponent<TruckTaxiMoralityWorldController>();
morality.authoredEvents = explicitlyAuthoredSceneEvents;
bool initialized = morality.Initialize(this, hud);
```

An empty array disables offers. There is no global scene search, resource fallback, generated trailer, generated stable identity, generated appearance or voice. Initialization refuses to replace another `MoralEventContextSource`. The optional `Open()` hook opens the same panel as the contextual HUD button; it cannot bypass the live checks.

The parent still owns any town-position mapping through `Session.TownIdAtPosition`. This controller does not infer town boundaries from destination radii. The Career file, Bootstrap, HUD and persistence facade are not modified by this integration.

The new `Session.MoralityChanged` event fires after town/accepted/completed state is installed, and during persistent restoration before the existing equipment/general notifications. The controller refreshes detached semantic snapshots on these events, not every frame. Restore invalidates arrival dwell and presentation even when loading the same profile.

## Regional Mapping And Optional Validation Entry Point

`LWS.TruckTaxi.Editor.TruckTaxiMoralityValidationAuthoring.Execute` is the narrow Editor `-executeMethod` entry point. It refuses dirty open scenes, opens the actual persistent Taxi scene and regional scenes for an identity audit, reports a bidirectionally road-graph-reachable pair of existing stops, and installs only `TruckTaxiMoralityTownBinding` on the existing host independently of trailer availability.

The town binding assigns `Session.TownIdAtPosition` to a guarded consumer of the existing `TruckTaxiRegionalWorld.ResolveRideTown` API, including its canonical outskirts/venue aliases. It does not create boundaries or infer a town from completion radii. Unknown positions remain neutral. It binds to the same-object Bootstrap Session after startup and refuses to replace another provider. No Bootstrap/HUD changes are needed.

The optional event authoring branch requires exactly one suitable existing persistent-scene trailer, its physical components, and two reachable existing same-town stops. It reuses their transforms and radii, appends an explicit controller reference, and never overwrites existing event assets. A streamed-only trailer is not persisted as an unsupported cross-scene object reference. If no suitable trailer exists, the entry point emits `MORALITY VALIDATION BLOCKED`, creates no event/choice assets and no physical content, but retains the independent town-mapping installation. Road graph success is not proof of articulated-trailer clearance.

## Narrow Authoring

1. Select an existing scene trailer with its existing `LwsTrailerGameplayIdentity`, NWH VehicleController and dynamic Rigidbody.
2. Invoke `Truck Taxi / Morality / Add Event To Selected Existing Trailer`. The Undo-supported command adds only the event component to that object, or selects the existing component. It does not create a trailer or save/regenerate the scene.
3. Assign a `TruckTaxiMoralChoiceDefinition` and `TruckTaxiTownDefinition`. Author a unique one-time event ID, the exact trailer ID, town ID and fictional abstract description.
4. The custom Inspector can fill an empty choice array with `turn-in` and `deliver-to-destination`, bounded small effects and zero payouts. Existing options are never overwritten. Review rewards explicitly; no real-world tactics or voice assets are supplied.
5. Fill empty destination bindings, then assign existing scene destination transforms. No positions are fabricated. Objective IDs must match the chosen definition exactly. Both truck and trailer must fit inside the same bounded arrival radius and height band.
6. Add the component reference to the controller's explicit event array. Run its validation button, then validate actual coupling, road routes and UI in Play Mode separately.

## Trusted Completion

The controller reads the active truck's existing coupling adapter and NWH hitch module. It requires two-sided attachment, the exact attached trailer object (not merely an equal ID), matching towing identity, active existing content, distinct dynamic collision-enabled rigidbodies and finite physics data.

Presentation and acceptance require stationary matching physical context. Completion additionally checks the saved choice's exact town/trailer/objective mapping, both bodies inside the authored horizontal radius and height band, low linear/angular speed, and a continuous 1-10 second authored dwell measured only in physics ticks. Moving, detaching, disabling content, changing profiles, loading, or losing valid context invalidates dwell. Pause time and wall-clock jumps cannot supply arrival credit. UI completion rechecks current evidence; it never passes reward values to Session.

Session remains the sole owner of accepted effects, completion records and wallet payout. Accepted effects are frozen, and completed event IDs prevent repeat payment. No UI display or route arrival automatically pays anything. The component does not spawn/despawn people, attach/detach trailers, move vehicles, or change physics settings.

## UI And Navigation Ownership

The controller uses existing HUD Panel/Text/Button factories (including configured Heat buttons), the existing modal bridge, and existing input dispatch. It creates no InputActions, EventSystem, independent pause authority or audio source. Walking and other modal owners are not interrupted. A contextual top-center button appears only when eligible, or while an accepted objective exists and the panel can open.

The GPS lease uses owner `taxi-morality` on the lowest existing `RideFinal` layer, admitted only when the entire stack has no target. Existing player-service, companion, ride and other targets are never replaced. Release uses owner, target and token. A higher-priority target may suspend the moral route; a ride can replace its lowest-layer slot. Rebinding retries only when navigation is free, with bounded retry frequency, and respects explicit Stop Navigation while its target remains present.

Accepted choices rebind from saved semantic IDs after resume. Missing, inactive, duplicate or changed authored bindings fail closed with diagnostics. Unavailable routes do not count as completion. No saved accepted choice creates a new identity or physical trailer.

## Persistence Limits

**The existing Vibin attached-trailer save/load block is unchanged.** Acceptance/completion requests autosave through the existing facade; a request is not proof that disk saving succeeded. While attached, saving remains deferred. Detach and satisfy the normal safe-save gate to persist accepted/completed semantic state.

Accepted morality choices can therefore be saved detached. After load, the same existing authored trailer must be available and physically recoupled before completion is eligible. Taxi persistence does not restore that trailer's previous world position or attachment. Trailer-world persistence, respawning, detached-trailer delivery completion, automatic detachment and return-to-menu save bypasses are not implemented.

## Verification Checkpoint

Authored EditMode coverage: exact trailer-instance/towing identity guards, dynamic body requirements, truck and trailer speed/spin, horizontal/vertical arrival bounds, continuous dwell/reset, saved binding mismatch/unavailability, invalid authoring, uninitialized controller, lease release isolation, and installed-state notification order.

No Unity, compilation or tests were run by this work. Parent validation still needs: actual NWH hitch attach/detach; keyboard/controller/mouse modal interaction; competing HUD panels; physical stop/dwell at both destinations; detached save/restart/resume; missing streamed trailer; route contention; and visual layout at supported resolutions. No scene or asset instances were authored automatically.
