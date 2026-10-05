# Needs / store / fatigue / sleep integration

## Parent and map hooks

- `TruckTaxiServiceCapability.Sleep = 8192`. Advertise only real enabled service points; ring uses `Position` and `InteractionRadius`.
- `State.Fatigue` is normalized 0..1 (zero rested, one exhausted).
- Coordinator exposes `TruckTaxiDriverNeedsSnapshot CapturePersistentState()` and `bool RestorePersistentState(TruckTaxiDriverNeedsSnapshot snapshot, out string error)`. Same methods are available on State. Stable string item IDs and exact counts, all four needs and starter-grant flag are included. Restore clock first, then coordinator payload; no starter grant on restore.
- NEW VIBIN creation only: call `CreateNewVibinInventory()` after needs initialization. Never on continue/load, scene initialization, region streaming or menu return. Idempotent and blocked after successful restore, including a legacy false grant flag.
- Parent owns LwsSaveService with profileScopeId truck-taxi and existing Pixel Crushers backend. No new serializer, backend or save authority here.
- Coordinator sleep hooks: `NearbySleep`, `RouteToSleep()`, `TrySleep(int hours)`, `SleepFailureReason(int hours)`. Durations 2, 4, 8. No adult/companion interaction.
- Parent owns Bootstrap, MainMenu, Session and future HUD Save UI hooks. The bounded HUD integration below preserves the parent's custom-content guards.
- `TruckTaxiSleepAuthoring.ConfigureHotels(scene)` / `ConfigureSleepLocation(location, costPerHourCents)` use Unity APIs on already-open scenes, require explicit truck-stop anchors, preserve IDs/capabilities, and never open/save/rebuild scenes. Parent invokes on generated data before its save. `BindExistingPoints(scene)` also binds existing typed hotels at runtime, making current hotel parking bays usable without a full regeneration. Bind after regional metadata activation if a future region owns its own service metadata.
- Map integration now uses the existing GPS intent API: sleep completes only its matching service ID; the explicit resume command clears only the player-service intent. Suspended passenger diversions are not cleared.
- No raw asset edits, separate Unity launch, build or commits in this workstream. Parent started its own Editor during implementation; this workstream does not control that process.

## Inventory and neutral custom-content hooks

`TruckTaxiNeedsItems.All` is the single 17-item registry. `Store` derives its 15 purchasable entries from ownership/purchase metadata. Filled containers remain ownable but not buyable. Every store sells every purchasable entry, including Road Pep; legacy `sellsRoadPep` metadata no longer restricts purchases. Decoration mounting limits do not prevent purchasing inventory.

Stable lookup: `FindByStableId(string)` (also `Find(string)`). State exposes atomic `TryConsumeItem(string stableItemId, int quantity)`: negative/unknown/insufficient stock or active jug action fail without mutation; zero validates the item. This generic debit does not apply drink/food effects or generate bottles.

Stable IDs: `water-bottle`, `soda`, `energy-drink`, `snack`, `burger`, `sandwich`, `hot-meal`, `empty-jug`, `empty-bottle`, `air-freshener`, `dashboard-dancer`, `novelty-duck`, `high-octane`, `mystery-mushrooms`, `road-pep`, `filled-bottle`, `filled-jug`.

New-game creation sets every ownable item to exactly 10, not 10 plus the legacy emergency jug. Explicit NonInventory, DebugOnly, NotPlayerOwnable flags exclude future entries. Snapshot restore validates completely before mutation, rejects unknown IDs/duplicates/invalid values instead of dropping data, and uses IDs rather than enum array positions. Missing registered entries restore as zero, never a starter grant. Capturing an active jug includes its reserved container but not transient QTE state. Snapshots are detached copies. Temporary stimulant effects are not persisted.

Current fare `Passenger.customItemInteractions` are available from ITEMS > PASSENGER ITEMS only when nonempty. Rows use the session block-reason authority; each interaction shows its authored optional prompt and Accept/Decline text. Accept revalidates current ride and membership, closes the panel, unpauses, then calls `host.CustomContent.TryExecuteItem`. Decline only updates panel feedback. Nothing runs or consumes inventory when opening the page, displaying a prompt, or beginning a separate custom/companion event. These are neutral data-driven hooks, not authored adult content.

## Store UI

Heat buttons remain authoritative for visual/button behavior. uGUI ScrollRect + RectMask2D + GridLayoutGroup + ContentSizeFitter own scrolling, masking and content layout. Header/status occupy 86%-99%, viewport 17%-85%, footer 3.5%-13%; rows cannot render behind the footer. Two responsive columns support arbitrary item counts. Focus changes from the existing keyboard/gamepad input owner scroll the selected cell fully into view, with padding. Mouse wheel/drag stays native ScrollRect behavior.

## Fatigue and sleep

- Normalized Fatigue fills upward. Rates: parked .040/hour, normal driving .044/hour, high speed or severe weather .0485/hour. Midnight-5AM adds only 10%; elapsed intervals integrate night overlap rather than applying the ending hour to an entire skip. Pause and backward/invalid time do not add fatigue.
- Road Pep multiplies fatigue gain by .25 initially, with repeated use increasing this up to .85; its comedown multiplies gain by 1.2-1.5. Energy Drink multiplies gain by .8. None resets existing exhaustion. Repeated dosing still accumulates fatigue.
- Effects start at >=80%, escalating toward 100%. A DOTween smooth close/hold/open drives an isolated URP world Volume. Intervals range from 50-80 seconds at 80% to 8-12.8 seconds at 100%; full dark hold is <=.2 seconds, never rapid strobe. Reduced mode disables blur and limits dimming. Screen-space overlay Heat/TMP remains outside post-processing. Existing effect adapter jointly owns camera registration/restoration so mushroom and fatigue effects cannot restore over each other.
- Sleep accepts only 2/4/8 hours at a stopped real Sleep service. Available session only; pickup, onboard, boarding/exiting and all other fare phases reject with FINISH CURRENT RIDE BEFORE SLEEPING. Active jug, Road Pep challenge and custom presentations are blocked.
- Recovery subtracts .25/.55/1.0 fatigue; 8h fully rests. The authoritative LWS clock AddHours advances date/time even while the needs menu is paused. Its callback is suppressed for this one skip so sleep progression is applied exactly once. Hunger, thirst and bladder continue at minute granularity, including normal crisis consequences. Short temporary item effects expire across sleep.
- Optional `sleepCostPerHourCents` uses session wallet debit before the skip; free rest remains possible while in debt. `SleepCompleted(int hours)` is available to parent telemetry/save integration. No vehicle physics or companion sequence is invoked.

## Validation and remaining parent acceptance

Bounded HUD/input integration: keyboard H remains the normal horn; keyboard B is unbound from air horn. The shared controls entry used by companion pickup prompts now reports H only and explicitly labels keyboard air horn unbound. Gamepad South/right-stick and calibrated wheel horn/air-horn paths are unchanged. Bottom-left telemetry is speed-only with a centered single-line MPH region and more room for shift status; central StatusBars fuel is unchanged. The existing top needs band includes fatigue; existing services/context sleep buttons open the same 2/4/8-hour page, and controls catalog rows expose those menu actions. No extra HUD controls or Canvas were added. Mega Debug > EXTRAS displays the existing custom-content runner's Status; ANALYTICS and parent HUD fade, map-open, pause-cancel and input-hold guards remain intact. Parent may add Save UI after handoff; sleep authoring still requires the parent to invoke the Unity API helper before saving generated scenes.

Added `TruckTaxiHornBindingTests` for H press/hold/release, inert B including concurrent H, and independent gamepad horn/air-horn edges. Extended `TruckTaxiControlsAndCabLookTests` for the prompt-facing H label, sleep menu entries and retained wheel bindings. These are authored tests awaiting the parent's Editor run, not claimed passes.

Added EditMode suites `TruckTaxiNeedsSleepTests` and `TruckTaxiStoreScrollTests`; updated superseded grant/drink assertions in CorrectionTests and TemporaryEffectsTests. Coverage includes stable IDs, grant/restore idempotence, detached snapshots, invalid atomic restore, generic debits, exact drink relief, circadian timing, stimulant rebound, sleep recovery/clock boundaries/fare blocking, authoring idempotence and 43-row focused scrolling at 1920x1080, 2560x1440, 3440x1440 with 1.0/1.25 scale.

Syntax-only Roslyn parsing and scoped `git diff --check` are run locally. No Unity Test Runner or playtest was executed by this workstream; authored tests are not reported as passed. Parent must validate Editor compilation and run the focused suites. No binary/serialized scene/prefab/asset edits are included.

Manual parent acceptance: last store row at all requested sizes; gamepad focus across footer; buy Road Pep at every shop; start new VIBIN with 10 each, consume then save/quit/reload with no regrant; use a real hotel ring and verify 2/4/8h costs/date/needs; block sleep during a fare; inspect fatigue at 79/80/90/95/100 with full/reduced mode and mushroom overlap; verify GPS/alerts stay readable; authored custom-item accept consumes exactly once and restores driving, decline consumes nothing, empty arrays show nothing; confirm H-only horn/pickup prompts, speed-only lower telemetry with central fuel intact, and runner diagnostics at target resolutions. Parent supplies persistent profile orchestration, generated-scene authoring execution and final build.

## Vendor report

- VENDOR ASSETS AUDITED: Heat ButtonManager/ShopButtonManager and existing HUD adapter; Pixel Crushers Saver template and existing LwsSaveService; NWH CameraChanger via the existing camera adapter; Weather Maker day/night API and existing LWS clock ownership; installed DOTween; uGUI/URP; Unity Input System direct keyboard/gamepad adapter and Unity-DirectInput wheel calibration/control path through the existing NWH input provider.
- VENDOR ASSETS USED: Heat/TMP/uGUI controls; DOTween interpolation; URP Volume effects; existing NWH camera adapter and Weather Maker/LWS clock integration. Pixel Crushers persistence remains through the parent-owned LwsSaveService.
- RELEVANT ASSETS NOT USED: Heat ShopButtonManager not substituted because the existing Heat HUD button adapter already owns input and labels. No vendor inventory/fatigue/sleep rules provide these project-specific semantics. Compass Navigator map/rings stay with the map workstream. Weatherade/Scene Streamer need no edits for these rules.
- CUSTOM SYSTEMS CREATED: needs snapshot DTO and stable-ID metadata; fatigue/sleep rules and world presentation adapter; uGUI focused-scroll helper; explicit Unity sleep-authoring helper; neutral item UI wiring on the existing panel. No parallel clock/save/input/vehicle system.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none; adapters reuse the vendors for rendering, scrolling, animation, camera and save infrastructure.
