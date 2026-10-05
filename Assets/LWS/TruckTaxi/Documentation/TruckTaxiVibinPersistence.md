# Vibin Persistence Handoff

## Ownership

Section 18 integration uses the existing registered `ILwsSaveService`, constructed as
`new LwsSaveService("truck-taxi", false)`. It never creates a second registry, file store,
serializer, clock provider, position provider or truck provider. A career-scoped facade
is rejected rather than replaced. Profiles use the shared directory's global index allocator.

The seven applicable existing world-resume, global-position, truck, clock, weather, road-condition
and navigation provider classes are registered once each. Freight's active-job provider is
excluded because it requires a career-only service and otherwise prevents sandbox initialization.

One participant, `lws.truck-taxi.vibin` version 1, carries the session snapshot, exact
needs/inventory snapshot and authored region ID. Its embedded `JsonUtility` payload follows
the existing LWS participant convention. The enclosing `lws.semantic-state` snapshot goes
through `LwsPixelCrushersSaveAdapter` and `LwsPixelCrushersSemanticSaver`; Pixel Crushers
owns serialization, persistence, slot existence, directory storage and backups. No gameplay
file IO, alternate career save, vendor source change or analytics storage is involved.

## Parent Wiring

Bootstrap and MainMenu integration is now implemented. HUD/save-panel remains parent/Aquinas-owned.
Bootstrap is frozen for the parent's final Bathroom/JugContext hooks; no further bootstrap edits
are part of this workstream. Its startup performs the following only after all services are ready:

```csharp
var persistence = GetComponent<TruckTaxiVibinPersistence>()
    ?? gameObject.AddComponent<TruckTaxiVibinPersistence>();
var initialized = persistence.Initialize(this);
// After host.Ready is true, on a fresh Taxi bootstrap:
if (initialized.Succeeded)
{
    if (TruckTaxiVibinPersistence.HasContinueRequest)
        persistence.ConsumeContinueRequest();
    else
        persistence.NewGame("VIBIN'");
}
```

- `Initialize(TruckTaxiBootstrap)` returns `LwsSaveOperationResult`; call after canonical
  player, clock, session, needs and regional services. Repeating with the same owner is safe.
- `NewGame(string displayName = "VIBIN'")` creates a scoped profile and calls the needs
  owner's one-time 10-per-ownable-item grant. Call before offers/gameplay on a fresh bootstrap.
  Initialization and loading never grant stock. Returning to New Game requires a fresh Taxi
  bootstrap, not repurposing an existing restored session. New-game autosave is queued.
  The method starts the fresh inactive session; load attempts and pending Continue requests
  cannot be converted into a new run on that same component.
- `Save(int slot = 1, bool overwrite = false)` is synchronous. Confirm overwrite in UI first.
  `RunProfileId` is bound only after successful creation or terminal load success. Manual saves
  target that owner, not mutable `ActiveProfileId`. The shared `AdditionalSaveBlockReason` rejects
  selected-profile/owner mismatch, including the facade's periodic and requested autosaves.
- `Load(string profileId, int slot = 1)`, `LoadAutosave(string profileId = null)`, `LoadLatest()`
  and `LoadRecoveryBackup()` return acceptance/failure immediately. Accepted loads continue
  asynchronously. Subscribe to `OperationCompleted` before invoking; its terminal callback
  sees `IsBusy == false`. `Status` and `LastResult` expose the final result.
- Loads deliberately leave the host and clock paused on success AND failure. Parent UI may
  offer Resume after success, but must not unpause failed loads. Failure blocks subsequent
  save/autosave overwrite until a load succeeds. No automatic backup application occurs.
- `RecoveryOffer` is the facade's existing offer. Display it and invoke `LoadRecoveryBackup`
  only after explicit player confirmation. Corrupt-primary preread runs the facade's own
  failure path to populate that offer. Ground/scene preparation failure does not fabricate one.
- `Profiles`, `GetManualSlots(profileId)`, `GetAutosaveSlot(profileId)`, `Latest`, and `HasSave`
  provide discovery. `Latest` chooses the newest existing manual/autosave across this scope.
  Empty discovery should disable Continue/Load and display `NO VIBIN' SAVE`.
- A menu using the same registry can call static `GetOrRegisterSaveService(registry, out saves)`
  and `FindLatest(saves)`. This registers/initializes only the scoped service once, and blocks
  gameplay saves while unbound. It cannot coexist with a different save scope in that registry.
- Static `SetContinueRequest(profileId, slot, autosave)` stores ONLY profile/slot metadata;
  `ConsumeContinueRequest()` is called after the new Taxi host is ready. `ClearContinueRequest()`
  explicitly clears that pending metadata (it does not interrupt an in-flight vendor load).
  Preparation does not consume the request. Only matching terminal success clears it;
  `PendingContinueRequest` and `RetryContinueRequest()` support a failed-startup retry.
  No snapshot crosses the menu via static state.

MainMenu creates a scene-local instance of the existing LWS sandbox bootstrap only when there
is no application bootstrap. Its serialized sandbox flags are configured before Awake; it does
not invent a parallel registry. Existing registries must have the matching save scope. Visible
mode text is now `VIBIN'`; the old scene-name constant remains an editor-tool compatibility alias.
New Game clears old Continue metadata. Continue and profile/manual/autosave entries use
`ExistsInVendorStore(saves, slot)` through the facade's adapter, not just occupied directory
metadata. Missing storage is reported as unavailable, while genuine empty storage reports
`NO VIBIN' SAVE`. No browser/file storage is involved.

For the parent HUD/save-panel: use `host.Persistence`, `host.CanResume`, and
`host.ResumeBlockReason`. Persistence also exposes `HasActiveRun`, `ResumeBlocked`, and
`ResumeBlockReason`. The existing `host.SetPaused(false)` and `StartShift()` now enforce that
guard; session callbacks cannot unlock startup, in-flight loads or failed validated loads.
The wrapper stays paused after successful load until explicit Resume.

Use the wrapper for loads, not `SaveService.Load` from UI: readiness authorization belongs
to this component. On destruction it unregisters only its participant and leaves the registry
and facade lifetime with LWS, with saving/loading blocked until a new owner initializes.

## Safety And Timing

Manual active-fare failure is exactly `FINISH CURRENT RIDE BEFORE SAVING.` Saves also reject
capture, races, generic CustomContent playback, companion/private transitions, needs QTEs,
sleep, rescue/tow/fast travel, active local extreme physics, origin shifts, unavailable regions,
attached trailers and unsupported ground/clearance. No private-event payload is persisted.

The existing 300-second autosave driver remains the only timer. Session completion/purchase/
service counters, needs service/sleep events, fuel rescue and regional availability changes
queue boundary requests. Requests drain in LateUpdate so a charge cannot save before the
same-frame inventory/service mutation. Existing facade minimum interval and pending behavior
remain authoritative. Parent may call `RequestAutosave(reason)` at a menu/exit boundary; it
can defer, so it is not a guaranteed clean-quit flush and no extra quit writer was installed.

Loading uses read-only `InspectVendorSlot` first. Required canonical participants, versions,
duplicate IDs, malformed JSON, session, inventory, clock, world, region and truck pose are
validated without applying to live state. Needs validation uses a detached needs state.
The wrapper then awaits `PrepareInitialWorld`, requires the actual additive region scene,
and checks `TruckTaxiRecoverySpace` ground and clearance. It preserves the current origin,
rebases the saved work-area center, and authorizes facade Load only for the inspected snapshot.
The Taxi participant restores after the canonical clock, through the needs coordinator so
its game-clock delta is rebased. It acknowledges application before success is reported.
Work-area validation and restoration each rebase a detached session copy, never the supplied
payload. Only the existing truck provider/coordinator place the vehicle; Taxi restore performs
no Rigidbody move. Final pose/clearance verification must succeed before the run is unlocked.

## Bounded Gaps

- Fuel, damage, headlights and radio are not added to this semantic payload. Existing canonical
  providers retain their current responsibilities; this work does not claim extra vendor state.
- Attached trailers are blocked. Obsolete regions, blocked ground, and failed readiness block
  loading; no guessed road fallback or navigation changes were introduced.
- The parent repaired canonical quaternion validation; the temporary W-near-zero guard is
  removed. A valid 180-degree rotation is covered by a focused snapshot test.
- The shared save gate has no requested-target argument. Wrapper writes are owner-pinned and
  selected-profile mismatches are blocked, but direct calls to `SaveService.Save(otherProfile, ...)`
  while another profile remains selected need a parent target-aware facade guard. HUD must use
  the wrapper. This limitation is not presented as complete cross-target facade protection.
- The coordinator currently clears its application context when `LoadFromSlot` returns.
  A vendor listener that defers application can therefore lose the prepared position. Taxi
  now fails closed if final placement differs instead of introducing a second placement owner;
  asynchronous context retention/finalization remains a parent-foundation acceptance concern.
- A vendor failure after application begins is not transactionally rolled back. All known Taxi
  semantic preflight occurs before application, but this does not claim atomicity for unrelated
  vendor savers. A missing Taxi apply acknowledgement reports failure after 10 unscaled seconds.
- MainMenu/bootstrap code integration is complete at source level. HUD integration, real restart
  persistence, asynchronous vendor apply, region load/collision validation and UI backup
  confirmation still need parent Editor acceptance. No real-restart success is claimed.

## Validation

`TruckTaxiVibinPersistenceTests` covers required providers, corrupt/duplicate/versioned payloads,
invalid inventory preventing restore callbacks, exact inventory round trips, one-time starter
grant, clock/world/pose validation, fare policy, snapshot changes between preread/apply,
metadata-only continuation and career-scope isolation. Existing session/needs tests cover their
owners' field semantics.

Additional regression coverage exercises live-run profile ownership, shared autosave blocking,
retry/terminal Continue retention, detached repeated work-area rebasing, canonical provider
registration without freight jobs, occupied-metadata false availability, and bootstrap resume locks.

Source-only Roslyn semantic diagnostics cover the full Taxi runtime plus this fixture against
the project's current assemblies, with Editor symbols. No emitted assembly, Unity launch, build, test run,
commit, scene/prefab YAML edit or vendor edit was performed by this workstream. Parent should
run the `TaxiPersistence` EditMode category and a restart/load smoke test in its existing Editor.

## Vendor Audit

- VENDOR ASSETS AUDITED: Pixel Crushers SaveSystem load/serialize APIs and installed LWS
  semantic bridge; LWS Scene Streamer regional adapter; NWH-backed canonical truck/recovery APIs;
  installed Heat ButtonManager text and interactability APIs.
- VENDOR ASSETS USED: Pixel Crushers via the existing facade; Scene Streamer via regional
  preparation; existing NWH-owned truck and LWS recovery placement queries; Heat menu controls.
- RELEVANT ASSETS NOT USED: Pixel Crushers Dialogue state is outside idle Vibin snapshot scope;
  no dialogue replacement was created.
- CUSTOM SYSTEMS CREATED: Taxi scoped orchestration, one semantic participant/payload,
  metadata-only continue request, focused tests and this integration document.
- VENDOR SOURCE MODIFIED: None.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: None.
