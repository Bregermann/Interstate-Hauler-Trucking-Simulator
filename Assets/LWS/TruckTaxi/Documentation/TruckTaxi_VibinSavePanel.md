# Vibin pause save/load panel

`TruckTaxiHud.SaveLoad` owns a reusable `TruckTaxiVibinSavePanel` under the existing HUD root. The pause page has SAVE / LOAD. No Canvas, save service, serializer, file backend, profile, or save participant is created by this view. Runtime persistence is resolved lazily using `host.GetComponent<TruckTaxiVibinPersistence>()`, so Bootstrap initialization order remains with its owner.

The panel lists manual slots 1-3 plus autosave, and LOAD LATEST chooses the facade's latest save across its scoped profiles. Profile arrow controls change only the viewed profile: they never call SelectProfile or change the active run. Only the active profile enables Save. Slot occupancy includes the existing facade's physical-slot check, not just directory metadata. Manual saves to empty slots call Save(slot, false); occupied slots require a confirmation before Save(slot, true). All loads confirm unsaved-progress replacement. Autosave has no write or delete action.

REVIEW BACKUP appears only for the facade's available RecoveryOffer. It shows the profile, primary slot, backup timestamp and failure reason; only its explicit confirmation calls LoadRecoveryBackup(). Slot and recovery confirmations capture immutable IDs, timestamps and vendor-slot identities and revalidate before execution. A changed slot, active profile or backup offer invalidates the relevant confirmation. No recovery runs automatically after a failed load.

The existing Heat button factory and TruckTaxiUIInput own controls and focus. Cancel is the default confirmation selection. Busy facade/coordinator states disable buttons, remove focus and block Back/Escape/pause. The full-root raycast panel and HUD input branch suppress underlying controls and shortcuts. Back first cancels a pending confirmation; otherwise it returns to the existing pause page. Neither success nor failure unpauses. The persistence authority still validates unsafe fares, events and world readiness; refusal messages remain visible. Parent custom-content OwnsInputHold/UsesEventInputHold guards remain unchanged.

`InitializeView(TruckTaxiHud, Func<TruckTaxiVibinPersistence>, Action keepPaused)` supports reuse with an already-created view root; initialization is idempotent. The pause callback deliberately has no resume counterpart. Operation events are detached on destruction. No direct calls to Pixel Crushers are made by the panel: requests flow through the existing truck-taxi scoped LwsSaveService.

## Validation

The parent's initial full EditMode run (`Logs/TruckTaxi-LateSeptember-EditMode-Initial.json`) recorded 852 tests: 725 passed, 125 failed, 2 skipped. Two save-panel tests reached a MissingComponentException because `GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()` retained Unity's Editor fake-null wrapper. ViewButton now uses Unity-aware `== null` before adding the group; tests assert real CanvasGroups and matching disabled input/raycast state. The corrected tests still require the parent's rerun; no local Unity execution is claimed.

Other owned follow-up fixes from that report: the six store-scroll cases now inject their fixture EventSystem into `RefreshLayoutAndFocus(EventSystem)` instead of assigning the unregistered EditMode fixture to `EventSystem.current`. Runtime calls still use the no-argument method and current EventSystem. The regional HUD test now includes FATIGUE while retaining its distinct driving-status-bar assertion. The mushroom peak test covers both the current 360-second normal profile and explicit 18-second short-debug profile, following each full hold through half-fade and expiry. Temporary-effects runtime timing and TruckTaxiHud.cs were not edited in this follow-up.

`TruckTaxiVibinSavePanelTests` covers immutable slot/profile guards, autosave overwrite rejection, eight changed-backup identity cases, invalid/UTC timestamps, missing/late persistence, safe failure, repeated initialization without a Canvas, available-button focus, and busy closure suppression. Tests are authored for the parent's existing Unity Editor; this workstream does not launch Unity, compile/build, run Test Runner, or commit. Syntax-only parsing and scoped diff checks are reported separately.

Parent acceptance: compile the new panel and HUD, run its EditMode suite, then check Heat pointer/gamepad confirmation focus at 1920x1080 and scaled/wide layouts. Save each manual slot, cancel and approve overwrite, browse another profile without changing the active save target, load latest/autosave, exercise an offered backup with cancel/confirm, and confirm busy/failure paths leave driving paused. Existing new-game/load initialization remains with Bootstrap/MainMenu ownership. Bathroom modal hooks can be added after the HUD is released.

## Vendor report

- VENDOR ASSETS AUDITED: Heat ButtonManager public navigation/interactable APIs and existing HUD factory; Pixel Crushers Common SaveSystemTestMenu; existing LwsPersistencePauseMenu, LwsSaveService and Vibin persistence API.
- VENDOR ASSETS USED: Heat ButtonManager through TruckTaxiHud.Button; TMP/uGUI and existing TruckTaxiUIInput; existing LwsSaveService/Pixel Crushers orchestration through TruckTaxiVibinPersistence.
- RELEVANT ASSETS NOT USED: Pixel Crushers' generic test menu and Interstate's independent pause view are not instantiated because they would bypass the existing Taxi modal/profile preparation integration. Their facade/confirmation patterns are reused.
- CUSTOM SYSTEMS CREATED: One project-owned save-panel presenter with ephemeral confirmation identities and tests. No persistence or input framework.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
