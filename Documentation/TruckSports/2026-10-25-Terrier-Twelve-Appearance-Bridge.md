# Terrier twelve-owner selected body / original wreck replay — October25 source handoff

Status: IMPLEMENTED / UNVERIFIED. This handoff contains source only. Native authoring/test execution, saved scene integration, Windows rendering and human acceptance are pending. No Unity/Editor/player/Git/CLEAN use or scene mutations by this source owner.

On the explicitly opened original stopped Terrier scene, run:

`LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.InstallOpenTerrierAppearance()`

The caller owns scene save after success. The tool refuses partial/custom bindings, stale/missing private recipes, changed original mesh/wheel identities or non-normalized LOD content. It does not rebuild the venue, run FitPhysicalTruck, generate recipes, move camera masks, edit source prefabs or alter physics.

The saved source scene audit found12racers,48existing NWH controllers,12TerrierWheelPresentation components with four original wheel meshes each, four real private cabs, twelve intact ghost roots and144original physical/ghost wreck pairs. There were no appearance hosts/cab bindings. Each original model has34transforms (model root plus33mesh parts); each intact ghost has the matching34 below its existing physical-pose root. The new author stores those exact channel identities and keeps original wheel alignment/scale and per-racer exterior layers10–21. Existing private cab layers25–28 and original masks are retained.

The twelve Hosts intentionally stay OUT of SportsEventSession.appearanceHosts. Terrier's mode-owned adapter prepares them only AFTER NewHeat has reset all144wreck bodies and called all twelve original racer.Prepare operations. This avoids composing a changed body beneath a disabled or stale wrecked model. The adapter uses actual persistent slot IDs even when display names are identical. Existing Host fit, paint indices, NWH wheel following and dashboard APIs remain authoritative. Four existing cab adapters follow actual viewSlots, clear on eliminated-player spectating/replay and retain private accessory layers. There is no selected-art material-name/chrome tint scan.

Selected wreck rendering reuses the existing seven private recipes and their eight BodyRoles/four WheelRoles. Actual saved recipe part counts range28–117 before bounded catalog cosmetics. Render-only children are attached to the SAME144physical pieces and SAME144existing ghost roots; their original MeshRenderer references remain valid. Owned transformed mesh clones are shared between each live and ghost graphic and released on revision/teardown. Before original Emit, actual selected wheel/cosmetic pose and indexed source materials/property blocks are captured. Original Emit remains byte-for-byte unchanged in TerrierWreckPool.cs, including radial/upward/point velocities, angular formulas, masses, shapes, lifetime, collision exclusions and dessert debris ignores. After Emit only rendering enable states change. Original winner locking, loser blast forces, cream contact, lava, first-to-five and scoring are unchanged.

TerrierReplay remains the sole intact recording backend. Its original34channels per owner are preserved with four actual selected NWH wheel-pivot local-position/rotation channels appended. All selected body/accessory content is static normalized renderer-only content (the catalog prohibits Animator/gameplay components); it is snapshotted at a fresh stopped heat boundary. No hierarchy scan/rebuild occurs per recording frame. Each frame remains original12root poses plus fixed456part poses, at the original2200capacity, original timestamps/masks/20ms sampling and original jump/crash/angle/seek segments. The source/ghost mapping rebinds between heats while the pose/times/masks arrays retain identity. Part-pose payload changes from25,132,800 to28,089,600bytes: exactly2,956,800bytes (2.82MiB) additional. Original root payload739,200bytes and time/mask payload17,600bytes remain unchanged. The original144-piece debris pose recorder, including scale/active masks and its original allocation, is untouched; one emission per racer per heat lets selected debris use that event's fixed render bind without another time-series recorder.

A pending metadata-only refresh waits for the actual same-scene TerrierSnow.surface.EnvironmentBindingsReady signal once per heat. It never delays physics/countdown or resets recorded poses. Actual BeforeEmit and replay Begin also copy live indexed material/property blocks. Authored selected snapshots skip the legacy Cab-name recolor path, retaining source paint/chrome/snow metadata. Unauthored Terrier scenes retain their original replay path.

New native filter: `TerrierAppearanceIntegrationTests` (four cases):

- All seven bodies across12stable owners: same144body/shape/ghost/ghostRenderer IDs, original Emit velocity formula, actual selected surface coincidence/UV/material caps, original34plus exactly4wheel channels, fixed buffer identity/memory, no invented score and owned mesh cleanup. Explicit controlled Emit fixture; not claimed as natural race evidence.
- Genuine12AI Hard/Clear flow: actual legal winner, actual cream squish, loser wrecks, once score, actual recorded selected-wheel interpolation and144-piece ghost samples, four original replay angles/seek without physical/result changes, pause, next heat and rematch. No race poses/results injected.
- Clear/Snow controlled startup-record fixture: original start poses sampled once with countdown held; original TerrierSnow.Step processes real environment binding batches. Exact indexed metadata, one ready refresh, recorded-pose/time/count/duration/buffer retention. Separate from natural race acceptance.
- One to four real assigned native test Gamepads: original source validation/viewport layout, actual cab owner/dashboard and private exterior/interior masks, controlled original elimination callback/spectator cleanup. Software test devices and physical actors; no physical-human/device claim.

Keep original Terrier rules, finish, wind, dessert and full-match probes as regression requirements. New fixtures do not replace original complete first-to-five acceptance. Geometry/pose/camera/material tests have not run yet; the first native failures must be investigated rather than treated as acceptance.

Static Roslyn diagnostics after the author preflight repair:25source files,534cached references,0errors. No assembly emission or Unity invocation.

VENDOR ASSETS AUDITED: installed NWH GR3D original meshes/wheel containers and Terrier wheel adapter; existing seven private EzySlice-derived recipes/BodyRoles/WheelRoles; Sports appearance Host/catalog/cab binding; Taxi/Weatherade environment readiness API; original Terrier custom intact/debris recorders.

VENDOR ASSETS USED: original NWH physics/wheel poses, actual owned body/cosmetic prefabs/materials, existing private vendor-cut recipes, Host fit/indexed paint/dashboard, existing Weatherade binding and original Terrier recorders.

RELEVANT ASSETS NOT USED: generic SportsFragmentRig presentation requires a different rig/explicit SportsWheelMeshBinding and does not own Terrier's144-piece custom recorder. Terrier uses its existing pool/adapter instead; no generic rig or second recorder is introduced.

CUSTOM SYSTEMS CREATED: Terrier-only selected-render/wreck/snapshot adapter, additive preflight author, fixed four-channel map extension in the original recorder and focused native fixtures.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none. NWH remains physics/wheel authority; existing EzySlice recipes remain cut geometry; original Terrier recorders remain the only pose history.

Native30 author preflight follow-up: author failed before mutation on the original clone-root name. Read-only ROOT and CLEAN saved-scene inspection independently found34channels per racer, all33matching child mesh identities and no ghost physics. The sole mismatch in all twelve was channel0: `NWH GR3D visual geometry` versus its original `NWH GR3D visual geometry(Clone)`. The author now permits only that existing root alias (or exact original root name), verifies all33child names/shared Mesh identities/parent frames, and retains the ghost collider/Rigidbody rejection. New EditMode filter `TerrierReplayAuthorPreflightTests` verifies the genuine Instantiate alias and rejection of arbitrary root/child aliases, equal-but-different meshes, changed parent frames and count mismatch. Runtime, original34channels and recording buffers were not changed by this repair. Native rerun pending; prior failure evidence retained.
