# Save / Load Drivability Regression

## Report And Causes

Human-observed: the saved truck reappeared but could not drive after loading.

- `LwsSaveLoadCoordinator` used `ILwsVehicleInputService.NeutralizeInput()` as temporary suppression. That API releases the registered source and owner. A live NWH provider referencing the service consequently kept receiving neutral input after load.
- `TruckTaxiVibinPersistence.LoadRoutine` unconditionally paused again in its completion `finally`, including successful loads. Bootstrap also unconditionally paused Continue, and the save/load panel remained open.
- `Lws18SpeedTransmissionController.RestoreState` copied the semantic state but did not restore the actual `mode` used by Update. Restoring an automatic save after switching to manual therefore left the live controller in manual mode.
- `LwsKeyboardGamepadTruckInputSource` registered on Start but did not release itself on destruction. Returning to the main menu could leave the persistent service holding a destroyed truck's source, preventing the next truck from registering its fallback input.

## Repair Boundaries

The load coordinator retains input device ownership. Existing `LoadingWorld` and `RecoveryError` gameplay-state gates suppress native driving axes, truck commands and transmission processing. Normal lifecycle release/shutdown semantics of `NeutralizeInput` are unchanged.

Legacy NWH fallback command reads and validation gear mapping now honor that same macro gate. Destroying a keyboard/gamepad source releases only its own registration through the existing service. A wheel or newer replacement source keeps its ownership.

Successful Taxi loads release their pause after world preparation and application finish. A load-specific completion event closes the existing save/load panel and clears its focus. Saves do not close the panel. Failed loads retain safe pause/recovery presentation. Continue does not re-pause an already completed load.

Transmission restoration installs the saved mode and selector, clears transient automatic shift timing/clutch state, and only requires physical shifter synchronization in manual mode. Manual synchronization safety remains intact.

No controller replacement, forced velocity, blanket suppression clearing, body constraint reset, new input provider, save format, storage backend, or vendor source modification is part of this repair. Pixel Crushers still owns save/load; NWH still owns the physical truck.

## Validation

Unity 6000.4.10f1 focused EditMode run: **165 passed, 0 failed, 0 skipped**. Evidence: `Logs/TaxiDriveRestore-EditMode6.xml` and matching `.log`. Coverage includes source ownership, all load phases and failure/retry gating, fallback native commands, destroyed-source replacement/non-owner safety, cross-mode transmission restoration, successful/failed load UI behavior, and isolated harness guards.

Windows build succeeded with Unity exit 0: `Logs/TaxiDriveRestore-WindowsBuild13.log`. Artifact: `Builds/TruckTaxiDemo/TruckTaxi.exe`. The separate external Build 18 installation was not updated.

Windows Continue acceptance passed in `Logs/TaxiDriveRestore-continue11.log` (process exit 0). Native driving distances were 11.75 m after fresh-process Continue, 10.75 m after reloading the actually driven save, 10.75 m after loading the other region, and 10.18 m after returning to the main menu and continuing with a newly spawned truck. Regions: `taxi.town01` and `taxi.town01outskirts`.

Fresh-process Manual Load acceptance passed in `Logs/TaxiDriveRestore-manual13.log` (process exit 0): 10.10 m after manual loading, 10.97 m after save/reload, 10.99 m after changing region, and 10.36 m after main-menu return and Continue. These Windows runs measured native throttle, service brake/stop, left/right steering, automatic forward gear, shift-command delivery, ignition/engine stop/start, a dynamic Rigidbody, clear gameplay/modal/recovery/travel gates, and exactly one native provider. Manual transmission hardware behavior is not inferred from automatic-mode command receipt.

An earlier `manual12` run passed its first three drives but timed out at the fourth drive's 20-second real-time deadline. Its input, mode, body and lock checks were valid. The rerun added timeout diagnostics only, not a production fix. This isolated timeout's cause is unconfirmed; the successful rerun does not erase it or establish performance reliability under loading stalls.

A second unchanged-build confirmation also passed: `Logs/TaxiDriveRestore-manual13-repeat.log`, exit 0. Its four driving distances were 10.50 m, 10.69 m, 10.66 m and 10.54 m, respectively. Both final manual runs completed all four drive/controls checks without debug recovery.

The existing opt-in development-player restart harness adds `-truck-taxi-test-drive` for isolated `ASTRA_SANDBOX_TEST` vendor storage. Loaded driving runs must not call Start Shift, resume menus, rebind input, reposition the truck, or force physics to make the assertions pass. Input System keyboard events flow through the existing input/controller stack; physical motion and native controls are measured.

The seed stage prepares stationary road saves in two distinct regions with an 80 m corridor remaining inside each seed region, initializes NWH through its first physics integration before using the starter, and clears initial ambient hazards only while creating the isolated fixture. Loaded runs never clear hazards or change input settings: they wait parked for legitimate save restrictions to expire. Focused Windows runs are required because the unchanged input focus policy rejects background keyboard events.

Regional seed slots 1/2 stay intact. Slot 3 records the actually driven position for in-session reload; the next load must explicitly change region. The sequence also returns through the actual main menu, verifies the old source was released, and drives the normally spawned replacement truck after Continue. Engine checks wait for full rundown and completed cranking, not merely a transient running flag during the starter.

The physical drive is bounded to a checked 25 m corridor and must move at least 8 m at >=2 m/s before braking. Validation rejects impossible speed/displacement, leaving the lane corridor and airborne motion; it does not reject an otherwise stationary truck merely because startup/streaming produced a >1 second frame. Earlier fixture failures (startup ordering, ambient hazard save restrictions, stale seed-region labels, clearance beyond the short drive, and the unrelated frame-time limit) are not counted as acceptance passes.

Physical Logitech hardware, human manual driving and other unexecuted scenarios must not be inferred from automated keyboard evidence.

All seven isolated fixture profiles were removed through the existing save facade, with every reserved profile slot checked absent. Evidence: `Logs/TaxiDriveRestore-cleanup13-*.log`, all exit 0. Real player storage was not used or changed; test logs remain available.

## Vendor Boundaries

- Vendor assets audited and used: existing Pixel Crushers Save System, NWH Vehicle Physics 2, and Unity Input System APIs.
- Relevant assets not used: GPS, weather, traffic and other unrelated vendor systems required no changes for this control restoration.
- Custom systems created: none in production; existing opt-in restart harness extended and focused regression tests added.
- Vendor source modified: no. Duplicate vendor functionality created: no.
