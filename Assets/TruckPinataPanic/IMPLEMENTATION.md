# Truck Piñata Panic — source handoff

This module implements Game 4 of the Alpha 0.7 Impossible Games request. Native scene authoring, physics acceptance, actual rendered art review and human/device/listening/performance acceptance remain separate gates.

## Explicit authoring

Run `LWS.TruckPinataPanic.Editor.PinataAuthoring.Create` in the lead's stopped, compiled Editor. It refuses an existing `Assets/TruckPinataPanic/Scenes/TruckPinataPanic.unity`; normal play/build never generates the scene or art. It uses the installed Fare Fight full NWH tractor prefab directly, without calling `EnsurePrefab` or changing that prefab. The first save establishes the scene identity, then the lead-owned `SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match, match.trucks)` binds the exact four fixed owners and saves again.

The lead owns collection registration, logos/menu-card gameplay images, shared broadcast framing, weather/time/SRS and release metadata. `PinataMatch.DescribeBroadcast` supplies the actual shell/last damaging truck, released candy and last confirmed collector. `drivingViews.rally.trucks` is the complete four-tractor roster; all current participants remain active. Original chase/cab and input ownership are retained.

## Authority and lifecycle

- `PinataShell : CoreCollapse.Collapser` retains the vendor's `startingHp/currentHp`, public damage and structural collapse event. There is no second health model. Real powered truck contacts above 2.5 m/s normal closing speed, with a half-second per-truck contact gate, damage the shell. Stationary rubbing earns nothing. Actual lost health is banked once as a small 1–6-point reward.
- The 1,100 kg shell hangs on a real ten-metre ConfigurableJoint rope limit. Full-roster health is2400 (600 per active competitor), so one simultaneous maximum-damage impact per competitor cannot instantly skip progressive damage. Its original seven-point folded tissue-paper star has seven compound convex panel contacts, visible layered hand-cut fringe and progressive tears. A single latched collapse activates seven pooled physical paper panels and48 pooled physical sweets; it does not instantiate fracture debris during play.
- `PinataRules` owns only candy life, points, phase and time. Normal sweets are10 points, golden stars50, fused black sweets a loss of up to15 plus a physical collector shove. A candy's round/id can release and collect once. Unreleased/stale/duplicate/foreign-owner calls do not bank. The score journal includes round in each candy key and uses shared stable participant identities.
- Three/five/seven piñatas: three-second countdown,55 seconds to break an intact shell,24 seconds to collect after rupture, three-second intermission. An intact timeout fabricates no candy or rupture. Scores carry between piñatas; tied points share place. Results/rematch/setup use the original Session/Heat lifecycle.
- AI uses existing `FareFightRoute` driving through NWH inputs, actual run-ups, released-sweet targets and explosive detours. It cannot directly award damage or candy. Human ACTION is an in-place physical stuck/upright recovery with a four-second cooldown; the same recovery is available to AI. Active-run recovery never writes a saved pose.

## Presentation and asset foundations

Original editor-authored saved meshes provide the missing paper star, closed folded panels, cut fringe, tears, twist-wrapped sweets, embossed golden stars, fused warning sweets, carved towers, canopy scallops, bunting and rounded venue trim. Final meshes are saved assets, not runtime placeholder primitives. The continuous paved floor and original tractor colliders retain NWH ownership.

The finished festival author reuses Vista grass texture, installed festival paving and braided cable materials,24 actually rigged Taxi/UTS spectators with the existing Stadium reactions controller, original Parking Polka music and shared crowd clips. CoreCollapse dust/explosion and existing Balloon paper confetti are particle-only copies; vendor scripts are removed only on owned instances, never modified in their source. A silent particle container, when needed, only coordinates the actual existing child effects.

Heat setup/results/buttons, compact portrait/score cards, confirmed score notifications, TMP and DOTween remain presentation foundations. Shared status contains actual health/candy count and clock; permanent rules and control instructions remain in Help. The arena board reads typed state. Music, crowd, particles, lights, score feedback and trucks follow pause/reset/exit.

EzySlice's runtime shatter example was audited but not used: this closed lightweight paper prop needs bounded pooled prefragments and repeatable rematch, rather than repeated runtime slice/instantiate churn. The CoreCollapse prefragment approach is extended only to activate an authored pool.

## Targeted validation

Pure filter: `LWS.TruckPinataPanic.Tests.PinataRulesTests` (11 tests). These same actual NUnit methods passed in a managed harness outside Unity; this does not claim Unity EditMode validation.

Native filter: `LWS.TruckPinataPanic.Tests.PinataLifecycleTests` (four tests):

1. `FourRealAIDriveStrikeRuptureCollectFinishAndRematch` — original AI/full NWH physical swing, real rupture and moving candy, actual pickup, pause invariants, complete finite match, fresh rematch and actual GameView720/1080 captures.
2. `NativeNormalGoldenExplosiveContactBanksOnlyActualOwnerOnce` — real AI must rupture first; then an explicitly controlled physical-contact fixture checks each candy kind, actual owner, explosive shove and duplicate compound-collider suppression.
3. `OneThroughFourAIViewsKeepOriginalOwnersAndTypedCards` — original one-to-four owner/view layouts and typed points, followed by shared view.
4. `FourSyntheticLocalPadsDriveTheirOwnOriginalNwhTractors` — four software gamepads route to distinct original NWH actors, move through their real inputs/physics, keep stable appearance ownership and pause/resume. Software pads are not physical-device acceptance.

Cached current0.6 API compilation passes runtime and tests. Authoring compiles against those APIs plus a clearly isolated compile-only signature for the lead's newly added `BindNewImpossibleScene`; that is not native author execution or validation of the shared binder. The lead must author, compile and execute the native suite, inspect the actual art/physics/screenshots and integrate the common systems before claiming complete playable acceptance.

VENDOR ASSETS AUDITED: CoreCollapse damage/prefragment examples and EzySlice, full NWH/Fare Fight prefab and drive adapter, Heat/TMP/DOTween, Taxi/UTS audience, Vista, existing music/VFX.

VENDOR ASSETS USED: Those existing foundations except runtime EzySlice slicing.

RELEVANT ASSETS NOT USED: No installed/cached matching piñata or wrapped-candy art was found; original saved meshes fill that content gap. EzySlice runtime shattering is unsuitable for this bounded pooled paper prop.

CUSTOM SYSTEMS CREATED: Pinata-specific contact adapter, candy/round/scoring semantics, finite pool coordination, original modeled art and explicit scene author/tests.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; NWH, CoreCollapse health, original input/profile/menu/camera/audio/UI foundations remain owners.
