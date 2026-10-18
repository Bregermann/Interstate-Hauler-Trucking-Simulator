# Plowing readability, night atmosphere and AI recovery

Status: accepted after all24 version-bound Windows suites passed and source publication was verified. See PlowingPresentationDelivery.md. The chronological entries below retain superseded candidate states and failed evidence.

## Current performance gate

Candidate `9d888e615326` / data manifest `eb48c784` passed wardrobe/night captures in both variants, physical impacts/scoring, retained-body rematches, Cab Chaos, dense crowd, all 60 one-to-four-view layout captures at five exact resolutions, the native 100-projection safe-framing fixture, and six full eight-AI matches. Every competitor contributed; arena runs recorded 216/218/220 accepted impacts and cab runs 218/221/218. Spontaneous jumps were observed in all six games, with occasional genuine prop climbs. Camera-preference persistence/ownership and Cornhole hole/guard checks also passed. The active Cheese start/guard players completed successfully after the old scheduler was stopped. This is partial candidate evidence, not final 24-suite acceptance.

Performance remains unresolved: 60-second four-view, eight-AI, 240-target samples measured chase mean39.10ms / p9562.54ms and cab mean37.43ms / p9568.77ms, versus earlier mean24.94/26.59ms samples. These separate-session figures are a regression signal, not yet a causal attribution. New development-only, seeded visual ablations and available Unity profiler counters will isolate sky/feature, costume geometry/shadows, clothing tint blocks and fill-light costs. Diagnostic stripped visuals are not delivery candidates. Preserve all physical gameplay/roster counts while optimizing presentation.

The eight instrumented Windows runs completed on `91e46fa8` / `f98b8995`: baseline33.77ms (repeat36.11), no tint blocks25.57, no cosmetic shadows36.46, no costumes33.82, no sky30.69, no Weather Maker feature28.56, no fill lights37.16. The decisive tint comparison reduced average SetPass calls from2279 (repeat2481) to748, GPU time20.97ms (repeat23.09) to15.02ms, and p95frame time56.29ms (repeat62.50) to37.52ms. Physics averaged about2ms; removing costumes alone did not resolve the cost. Seeded gameplay trajectories still differ with frame cadence, so these are measured comparative runs, not identical recorded replays. Render Thread and Draw Calls counters were unavailable; their values were not invented.

The targeted correction replaces the wardrobe's per-renderer tint blocks with shared runtime material variants keyed by original material and fabric color. It preserves shaders, textures, submaterial arrays, configured colors and original asset references; it changes no physics or scoring. The palette is released after the last scene-owned wardrobe is destroyed. New runtime checks require no clothing renderer blocks, exact configured colors and zero retained palette entries after scene exit. Windows appearance/performance/regression acceptance for this correction is pending. Sky, clothing geometry, shadows, crowd density and practical lights remain intact.

Optimized candidate `28cd5346297b` / `997688c9`: Windows wardrobe captures retain the same appearance; all three exact-color/no-block assertions and scene palette cleanup pass. Four-view chase measured mean24.299ms / p9537.496ms, GPU14.141ms and SetPass678; cab mean26.574ms / p9537.526ms and SetPass670. Both samples ran 60 real seconds with eight competitors, 240 targets, four focused rendered views and actual physical impacts. These restore the earlier performance range while retaining the requested visuals. The complete 24-case acceptance pass is running; its three identical-candidate cases are reused by exact assembly/data fingerprint rather than rerun unnecessarily.

## Requested behavior

Both Plowing modes share the original finite weighted character roster and contact-driven 10/100/−100 scoring. Role clothing is applied after the independently assigned role; authored skin materials and facial features are not scoring inputs.

Standard targets retain the blue costume nose, wear varied bright clothing and festival hair. Bonus runners wear a real charcoal fabric mask with eye openings, fabric gloves and dark clothing, with contrasting gold brows/star emblems. Penalties wear red uniforms, STOP crosses, fully clothed cartoon curves and the same owned ponytail mesh used by Poker's dealer. Numeric front/back/overhead labels are disabled. Brief contact feedback remains; the value legend lives in the HUD. Some characters hold a noncolliding festival pitchfork prop.

Weather Maker provides a fixed full-moon night through a sport-owned profile, with clouds and warm/cool festival fill lights. No replacement weather renderer or skin-recolor system was created. PlowNight updates the vendor's actual selected camera list.

Windows sky investigation: the first corrected renderer still produced a flat background, although a native offscreen render showed the actual moon/clouds. Refreshing the vendor's public camera-eligibility cache when the selected camera array changes restored the actual Windows sky. The controlled Windows capture on assembly `6b87da87` visibly contains the moon and clouds. No speculative renderer-feature or shader-source patch was necessary. The moon now has a sport-owned material; the original vendor moon material matches Git HEAD again. An early hidden-Editor probe reported a different night multiplier, so it is not used as the Windows visual acceptance result.

Close-up review also found that the half-meter robot model had been attached at source world size to a full-size humanoid animation/ragdoll rig. Its head sat around 0.4m while the human-sized vest sat at 0.73m, and its animated arms separated from its torso. Shrinking only the vest exposed the underlying rig mismatch. Explicit authoring now normalizes only this sport-owned default's existing meshes to the rig (3.21×), then refits its costume; original mesh shapes/materials and the Taxi source prefab are preserved. This migration runs once while that owned default still has the mismatched dimensions. Normal builds/Play Mode do not regenerate it. Final Windows review and full acceptance remain pending.

## AI investigation

Eight-AI full-match evidence in the previous acceptance had two Normal competitors with no impacts. Source inspection found obstacle throttle `.3` excluded by the old `Abs(command.y) > .3` stuck test, a two-second recovery delay and repeated reactive steering reversals. A physical wall fixture clarified that the old AI *could* reverse; the initial assumption of no reverse at all was disproved. The unchanged controller circled and reached the isolated target about 18.6 simulation seconds after fixture setup.

The repair includes low obstacle throttle in stalled-progress detection, starts a bounded reverse after .7 seconds without useful horizontal movement, holds an avoidance side briefly, uses ordinary NWH brake/reverse controls, clears stale paths and ignores inactive pooled targets. The repeated native same-wall test physically reached its target about 7.6 seconds after setup. Full-match and Windows repetitions remain required. Human driving and the contact-scoring authority are unchanged.

## Evasion and lifecycle

Bonus runners retain their existing faster panic speed. The new bounded NavMesh traversal permits short evasive hops and a roughly one-second perch on a nearby eligible prop/car, then returns to connected ground. A cooldown prevents continuous hopping. Actual truck strikes still interrupt into Taxi ragdolls and score only through the existing contact authority. No character score can be injected by evasion.

Native explicit fixtures have observed a bounded hop, a genuine solid-prop perch and return to reachable ground. They are controlled runtime fixtures, not human gameplay. Initial runtime testing found and fixed a MaterialPropertyBlock allocation in a MonoBehaviour field initializer. Removing world labels also exposed a destroyed TMP-generated MeshFilter in retained-pose baking; the bake now checks Unity object validity and ignores labels before accessing meshes. Failed evidence is preserved under Logs/TruckSports/Plowing.

## UI

The actual 720p/1080p one-to-four-view review found a subtitle/configuration-row overlap, long names crossing score-card portraits, and a shared overview exposing empty background behind the city perimeter. The subtitle now has its own gap; score-card text has a dedicated, bounded column with ellipsis for extreme names; the overview contains its ground footprint where the arena permits and centers oversized fits. A noncolliding pavement surround replaces the blue void without extending physical gameplay or navigation. Runtime view probes additionally require each human truck to remain inside its actual selected viewport. Final screenshots and sustained acceptance are still pending.

Validation correction: the first expanded camera assertion incorrectly required the owning truck's center to appear in its own cockpit camera. That assertion failed, as expected for a driver-seat view. The corrected fixture checks the forward driving focal point for cabs and the truck center for chase/shared views. It also checks exact rendered dimensions at 1280×720, 1920×1080, 1280×800, 1024×768 and 1920×800 before capturing. The prior failed assertion is retained in the candidate's evidence; it is not counted as a successful suite. Explicit scene authoring also rebinds the weather session when deriving Cab Chaos and normalizes the default robot through the same wardrobe initialization path.

Existing Heat controls/navigation remain authoritative. Shared Sports menu pages receive a short unscaled DOTween fade; button highlight colors tween, and the existing broadcast plate renderer now produces rounded corners. Plowing adds rounded participant cards and input surfaces, a compact per-view score/timer/target legend, readable cab needs information and screen-anchored shared score cards. Full movement instructions remain in Help/Pause. The first actual Windows candidate passed its controlled gameplay checks but failed visual acceptance: its inherited Unity skybox stayed blue, its arcade renderer had no Weather Maker render feature, and the old Sports daylight lamp remained enabled. The sky now uses a Plowing-owned URP renderer with the installed Weather Maker feature, a null Unity skybox (the existing Taxi integration pattern) and only the vendor celestial lighting plus festival lights. Zero-normal, double-sided star meshes now use a gold unlit material and sit on the vest rather than over the face. A substring match that treated a tool Handle as a Hand was corrected, and held pitchforks/masks were aligned in world space before attachment. Updated Windows visual acceptance is pending.

## Vendor accounting

VENDOR ASSETS AUDITED: NWH WheelController/SportsEventTruck, UTS traffic and animation controllers, Taxi pedestrian/rig/reaction systems, Poker owned dealer hair, Weather Maker day/night/moon/cloud APIs, Heat menu controls, DOTween and the existing native LOD/pooling adapters. Asset filename audit found no installed pitchfork or ski-mask model.

VENDOR ASSETS USED: NWH wheel controls, Taxi physical ragdolls and reactions, existing UTS character animation, Assets/TruckPoker/Art/DealerHair/Hair_Ponytail_01.fbx (lowest-detail mesh, baked for crowds), WeatherMakerPrefab and WeatherMakerDayNightCycleProfileScript/WeatherMakerCelestialObjectScript, Heat uGUI controls and DOTween.

RELEVANT ASSETS NOT USED: UTS road-traffic driving is unsuitable as the authority for the existing free-roaming sport competitors; the existing game-specific target planner still commands NWH. No replacement traffic AI is introduced. New masks/pitchforks are missing costume props, not replacements for owned character models.

CUSTOM SYSTEMS CREATED: small role-wardrobe, bounded pedestrian traversal, night/camera coordination and presentation adapters over the existing authorities, with targeted native/development probes.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
