# Passenger Bathroom Workstream Evidence

Date: 2026-09-30. Source-only bounded implementation of attachment section 10's ordinary passenger restroom need. Host/HUD wiring belongs to the parent. See `Passenger_Bathroom_Workstream_Contract.md` for exact hooks and required integration gates.

## Existing-HUD Integration Follow-Up

Added TruckTaxiPassengerBathroomPanel, TruckTaxiHudModalBridge and TruckTaxiModalPauseGuard; extended only the owned Need adapter to attach them during Initialize. The panel is a child of the existing Hud.Root and uses the existing Heat-backed button/text/panel factories. It supplies request acceptance/decline and brief break/reboard states, exclusive disposable host-pause claims, same-frame submit/cancel protection, known-foreign-modal guards, and conservative interruption cleanup. It creates no Canvas, EventSystem, input actions/bindings/listeners or navigation presentation.

Copernicus still wires ready initialization, Tick(mayOffer: CanOfferRequest), recovery cancellation and the patience predicate. Aquinas still adds the single bridge-dispatch call, bridge focus priority, ordinary-pause visibility guard and driving-controls blocked flag. Those owned files remain untouched here. Offers fail closed until the HUD dispatcher handshake exists; this is implemented integration support, not a claim of already-live host wiring.

`pwsh -NoProfile -File Tools/TruckTaxiValidation/Test-PassengerBathroomHud.ps1`: PASS, 30 production guard/source-boundary assertions, including exclusive ownership, wrong/stale release, interruption-sticky pause preservation and duplicate/open/close-frame dispatch. Added 9 NUnit cases, source-validated but not executed in Unity. No visual or live input evidence claimed.

## Validation

- `pwsh -NoProfile -File Tools/TruckTaxiValidation/Test-PassengerBathroom.ps1`: PASS, 31 focused assertions against production C# and 10,000 randomized clock/lifecycle mutations. Covers long-ride gating, intercity gain, authoring probability boundaries, pause/time-skip baselines, transitions, nonfatal declines, cooldown/quota, service-withdrawal retry, malformed inputs, higher-layer suspension, fare restoration and stale lease protection.
- `pwsh -NoProfile -File Tools/TruckTaxiValidation/Test-SourceDiagnostics.ps1`: the initial implementation and first panel/bridge pass had 0 errors. The last completed follow-up check (216 runtime / 48 editor / 88 EditMode source files) found 4 concurrent, out-of-scope runtime errors in `TruckTaxiSession.Objectives.cs` at lines 22, 26, 47 and 57: stale references to removed `TaxiRequestType.FillJug`. No bathroom/panel/bridge/editor/test errors were reported. A final retry after the ordinary feedback-text edit could NOT run because cached dependency `Library/Bee/artifacts/1900b0aE.dag/LWS.InterstateHauler.Runtime.ref.dll` was missing. The shared worktree/cache changed concurrently; current whole-worktree cleanliness is therefore unverified. This is in-memory Roslyn semantic diagnostics only: no Unity process, assembly emission or project build. Revalidate once the parent has a consistent cached reference set.
- 11 NUnit EditMode tests authored and source-validated, NOT executed in Unity. Additional cases validate real capability filtering, reachable route distance, actual bay anchor/radius/height/speed, and production navigation ownership.
- No PlayMode/presentation/frame evidence claimed. Physics/cab placement and shared modal/pause arbitration require the parent's integration plus live validation. Default tuning is not gameplay-balanced by these source tests.
- No Unity launch, build, commit, new Canvas, new navigation renderer, asset YAML, or world regeneration performed. The follow-up edits only its owned Need adapter/docs and new panel/bridge/test files; parent-owned runtime files and PassengerProfile were not edited.

## VENDOR ASSETS AUDITED

Compass Navigator Pro via TruckTaxiGPSAdapter and the current four-layer intent authority; NWH truck Rigidbody/host pause integration; Pixel Crushers via TruckTaxiDialoguePlayer; Heat's installed ModalWindowManager public API and controller-focus behavior, existing Heat button factories, current HUD/TruckTaxiUIInput focus/submit/cancel routing and related panel open/close patterns. Also inspected LWS GameClock, ServicePoint, route distances, PassengerRuntime, ActorWorldPlacement, PassengerActor, SeatProfile and CabOccupantRig.

## VENDOR ASSETS USED

Existing Compass presentation/route adapter, NWH-driven truck state, and Heat production buttons through existing TruckTaxiHud factories, all behind LWS APIs. Existing host.SetPaused, HUD Canvas/EventSystem and UIInput remain authorities. No replacement UI framework or vehicle behavior was created.

## RELEVANT ASSETS NOT USED

No direct Pixel Crushers conversation asset changes: the bounded request uses existing Session reactions and avoids unrelated generic mechanic dialogue. No standalone Heat ModalWindow prefab was introduced: its own controller-focus path would overlap TruckTaxiUIInput; the existing HUD already supplies the Heat-adapted factories and single dispatcher. No DOTween animation system, NavMesh setup or save adapter was needed. Existing checked actor placement and short Exit/Board abstractions remain authoritative.

## CUSTOM SYSTEMS CREATED

Passenger-specific authored game-clock state, a named temporary-restroom navigation/actor coordinator, a Heat-backed companion HUD panel, reusable LWS pause/input ownership bridge, focused tests and integration/evidence contract. These are Interstate gameplay semantics and coordination, not replacement vendor capabilities.

## VENDOR SOURCE MODIFIED

None.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None. No parallel GPS, minimap, clock, vehicle physics, dialogue renderer, Canvas, actor navigation or save authority.
