# Ordinary Passenger Restroom Diversion

Scope: attachment `557bc09b-0a32-4499-b33f-b78e7a172da9/Pasted text.txt`, section 10, restricted to an ordinary restroom break. No container interactions, private content, companion pickup, new navigation authority, or new Canvas. Parent owns host and HUD wiring.

## New Sources

- `TruckTaxiPassengerBathroomState.cs`: serializable authored policy and pure game-clock state.
- `TruckTaxiPassengerBathroomNeed.cs`: existing service, navigation, actor and cab-placement adapter.
- `TruckTaxiPassengerBathroomPanel.cs`: ordinary request/break panel built with existing HUD/Heat factories, under Hud.Root.
- `TruckTaxiHudModalBridge.cs`: reusable existing-HUD pause lease and single-dispatch input bridge.
- `TruckTaxiModalPauseGuard.cs`: pure ownership/context/token/frame bookkeeping for that bridge.
- `TruckTaxiPassengerBathroomTests.cs`: 11 focused EditMode tests.
- `TruckTaxiModalPauseGuardTests.cs`: 9 additional EditMode cases for modal/input ownership.
- `Tools/TruckTaxiValidation/Test-PassengerBathroom.ps1`: executable pure-C# source checks without Unity.
- `Tools/TruckTaxiValidation/Test-PassengerBathroomHud.ps1`: executable ownership and source-boundary checks without Unity.

No changes to PassengerProfile, Session, Hud, Bootstrap, PassengerRuntime, CabRig, ServicePoint, or vendor sources. No new serialized PassengerProfile fields are required. Author `defaultPolicy` and optional `passengerOverrides` on the new component. Overrides explicitly reference existing profiles and do not infer characteristics from names or appearance.

## Parent Integration

Bootstrap remains Copernicus-owned; Hud remains Aquinas-owned. Neither file was edited by this workstream. The new component automatically attaches/reuses the panel and bridge on the existing Hud GameObject, subscribes the request/changed events, and supplies AcquirePresentationHold. Parents do NOT need to create a panel, subscribe another request modal, or build a pause wrapper.

### Copernicus: Initialization And Tick

Add the following to the existing ready driver-needs/environment initialization path, after Hud.Root/UIInput, Session, Player, Passengers, GPS and Environment.Clock exist. `PassengerBathroom` below is a parent-owned property/field; it is not assumed to exist before this integration.

```csharp
PassengerBathroom = GetComponent<TruckTaxiPassengerBathroomNeed>()
    ?? gameObject.AddComponent<TruckTaxiPassengerBathroomNeed>();
PassengerBathroom.Initialize(this);
```

Normal unpaused host tick, after Session.Tick:

```csharp
PassengerBathroom?.Tick(mayOffer: PassengerBathroom.CanOfferRequest);
```

The component's own Update only handles cleanup/presentation and never offers requests. The bridge blocks offers until the existing HUD calls its dispatcher hook, so bootstrap-only integration fails closed. `IsInitialized` reports successful clock/panel attachment; `CanOfferRequest` additionally requires the live HUD route, ready/unpaused host, and clear mutual-exclusion guards. Reinitialize after disabling/re-enabling the component.

At the top of the parent's existing IsLegitimateWait, BEFORE its speed-based early return:

```csharp
if (PassengerBathroom?.IsLegitimateServiceDiversion == true) return true;
```

This is essential: Session otherwise measures progress against the unchanged fare endpoint and can punish an agreed detour as wrong-way driving. Merge this into the existing predicate; do not install another delegate authority. Before recovery, teleport or save teardown call `PassengerBathroom.Cancel(reason)` and defer the operation when false: a checked return to the cab has not completed.

### Aquinas: Existing HUD Dispatcher

Resolve `GetComponent<TruckTaxiHudModalBridge>()` lazily, because it is attached after HUD initialization. In the existing Update, after mandatory rescue/fade handling but BEFORE FullMap/debug/services/pause/submit routing, call exactly once:

```csharp
if (GetComponent<TruckTaxiHudModalBridge>()?.HandleUiActions(UIInput) == true) return;
```

Call it every normal frame even when no added modal is open. That is the routing handshake. No InputAction creation, performed subscriptions, alternate keyboard/gamepad reads, EventSystem, or second SubmitSelected call is needed. This method consumes the existing Submit/Cancel/Pause bindings once and suppresses opening/closing-frame input fall-through.

In RefreshFocus, prioritize the bridge's FocusRoot only when bridge.IsOpen is true; otherwise retain the complete existing focus chain. Add `bridge?.IsOpen != true` to the ordinary pause-panel visibility condition, and include `bridge?.IsOpen == true` in the existing driving-controls `blocked` expression. These are the only view ownership hooks. Do not hide the Hud.Root or create a new Canvas. Optional shortcut panels that gain new open paths should also respect `bridge.HasLease`.

### Behavior And Pause Ownership

- The panel's ordinary Heat ACCEPT/DECLINE buttons invoke the same Need.Accept/Decline methods as the bridge's existing UI submit/cancel route. It neither handles hover as confirmation nor creates independent input. A failed route leaves the request visible, with decline available. The backdrop blocks clicks into underlying driving controls.
- Offer and presentation have separate disposable claims in one exclusive bridge slot. `host.SetPaused(true)` freezes gameplay through the existing host authority. Disposal resumes only if this exact owner/token still holds the same ride/state, the host is still paused, and no known foreign modal/operation interrupted it. A changed ride, recovery/result/settings modal, external resume, missing routing/focus, or teardown invalidates automatic resume permanently for that claim. A conservative ordinary pause may remain after interruption; this is intentional, not an unconditional unpause.
- The legacy host exposes a pause boolean, not a global pause-generation lease. The bridge observes current public foreign-modal/operation flags and ride context; it does not claim to detect an unreported same-state `SetPaused(true)` from arbitrary future code. New callers must respect HasLease or add their public ownership flag to the bridge guard. No second global pause authority is introduced.
- Normal stop requests take precedence in the shared PassengerDiversion slot; ActiveStop/PendingDiversion withdraws bathroom without penalty. Higher PlayerService/Emergency intents suspend it; safe drop and ride termination cancel it. Session request-list and stop-presentation delegates remain untouched.
- Presentation uses only a temporary RESTROOM BREAK state, then reboards. Approximately 0.6 seconds after checked exit it advances the authored 1-10 game minutes, then attempts reboard at 1.2 seconds. A blocked reboard retains pause and offers RETRY BOARDING; cancel/pause cannot dismiss an outside passenger. Before recovery/teleport/save, honor a false Cancel return or terminate the ride through its existing lifecycle first.
- No persistent bladder bar. No standalone Canvas, new input actions, companion pickup/private interaction, vendor modification, or new route authority.

## Rules And Ownership

- Default eligibility: 50% of rides, initial bladder 0.1, gain 0.35 per game hour, 1.35x intercity gain, minimum 45 game minutes, request at 0.75, urgency at 0.95. These are authored tuning defaults, not claims about physiology. Longer/intercity trips cross the threshold more often. A single request per ride is the default; authored repeats have a minimum 15-minute cooldown and maximum three per ride.
- Bladder uses `ILwsGameClockService.ClockChanged`, not real-time delta. Pause, external paused skips, recovery holds and the break's own time jump refresh the baseline without accruing need. Backward/nonfinite clock updates cannot create negative need. Parent must pause recovery/travel clock jumps before applying them, as with existing host operations.
- Offers select the shortest reachable road-distance among loaded, enabled, identified Restroom-capable ServicePoints. No Store/Food/ride-location type assumption, invented roadside coordinates or fallback decorative POI. A truck already within an actual bay needs no graph leg. Stable-ID ties are deterministic.
- Accept claims `PassengerDiversion`, owner `passenger-bathroom`, with the actual service ID/position/radius and Bathroom marker. The slot must be free of other passenger/player/emergency intents when offered/accepted. Completion/cancellation releases only its owner/ID/token. It never calls ClearDestination or SetRideDestination, so the existing fare and higher intents remain intact.
- Presentation requires this active lease, actual ServicePoint.CanUse at <=0.44704 m/s, the bay's real horizontal radius and vertical tolerance, a seated primary actor, a current CabOccupantRig, and a checked off-road exit from ActorWorldPlacement. No companion or secondary occupant is moved.
- Exit uses PrepareForWorld/ShowInWorld and the existing non-graphic ExitAbstraction. This is a short abstract break at the checked service parking area, not new restroom-interior pathfinding. Reboard uses PrepareForSeat, SeatProfile.TryPlaceActor (CabOccupantRig), CompleteSeating. Bladder resets and the route pops only AFTER successful reboard; missing actors/terminal transitions are cancellation, not success.
- A blocked exit does not detach the actor or skip time. A blocked reboard retains the input/pause hold and the previous standing placement. If streamed support disappears during that exceptional failure, visibility is retained without inventing a replacement coordinate; parent recovery is required before resuming driving.
- Decline uses a small existing onboard-patience adjustment, capped so this action alone never takes patience below 0.15, plus ordinary Session.React text. It neither fails the ride nor adds a permanent penalty loop. Ordinary literal reactions intentionally do not invoke the generic UniqueMechanicReaction authoring bucket, which could contain unrelated passenger-specific material.
- This component's per-ride bladder is not persisted in this pass. Parent persistence/recovery must cancel temporary actions before its existing LWS save scope operation. No GPS persistence is introduced.

## Live Validation After Wiring

In an already-approved Editor session, run EditMode tests and verify: long/short game-clock trips; pause and sleep jumps; offer accept/decline; no real restroom; service unloading; wrong height/high speed/outside bay; blocked exit; compatible/blocked reboard; pause ownership when results/recovery interrupt; player-service and emergency suspension/pop; replaced passenger lease; final destination changed under the detour; save/reload cancellation. Verify existing external/cab Compass, unchanged vehicle heading, controller/keyboard/mouse focus, one submit per press, cancel/pause without leaking to driving, no underlying pause panel/click-through, narrow-screen text and panel teardown. These checks were not run because Unity launch/build was prohibited.
