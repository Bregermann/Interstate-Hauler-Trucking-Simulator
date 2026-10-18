# Cheese Rolling — terrain and starting physics repair

Status: implementation, saved-scene authoring, expanded native regression and final Windows acceptance passed. Publication is recorded separately after the reviewed commit/push. This is one item in the full Party queue, not completion of all queued prompts.

## Reproduced cause

The old reset held a chassis at terrain+1.2m, although its NWH wheel anchors were+.02m, tires.30m radius and suspension travel.13m. Twelve neutral starts across1–4 participants plus12 background trucks measured.7934m unsupported wheel clearance, then approximately−4m/s vertical motion after GO and1.3–1.9kN damper peaks. No overlapping truck bodies, explosions or earned boost forces were recorded. Neutral flat-start spinning was not reproduced; do not attribute every reported spin to an explosion.

The targeted start-only fix removed the unsupported drop in12/12 repeated starts on the unchanged terrain. Every active truck had four supported NWH wheels, a level stationary body and no overlap before release. Example final-four-player release range−.0100 to+.0081m/s, tilt.103degrees, damper peak100.70N. The original controlled baseline and fixed receipts remain under ignored Logs/TruckSports/Party/Cheese.

## Implementation

Reset places the chassis near native tire support, synchronizes the authoritative pose and interpolation cache, clears old wheel/spring motion and uses NWH's public Initialize API. A separate preparation phase permits native gravity/suspension settling in Y while constraining horizontal movement/rotation. Each truck must have stable four-wheel support before being held through the countdown. All trucks must be ready; a bounded failure cancels the start rather than releasing an unsupported grid. GO restores normal constraints and dynamics. Fresh-input boost timing and ordinary race collisions, flips, hazards, tire forces and recovery remain intact. Preparation is excluded from solo elapsed time.

The hill's main grade is multiplied by.875. The enormous elevated platform, horizontal course extent, smooth transition, natural side banks and flat mud runout remain. Smooth deterministic broad noise, localized rounded humps/divots and a curved rut provide different navigable routes.3,520 native rule/gradient/seed/input fixtures passed after authoring. Native Terrain and TerrainCollider share the saved height data.

The owned truck's genuine NWH wheel meshes are bound to native rotating containers and sized to the existing physical tires. Duplicate static cylinder tires are removed. NWH owns visual suspension/steering/rotation; physics radii, mass, traction and normal race controls are unchanged. Regeneration defaults invoke the same narrow installation; the hand-edited scene is updated without rebuilding its UI, scenery or weather.

## Regression history and acceptance

The first expanded run failed: a.04m/s readiness cutoff rejected fully supported trucks whose position was stable but PhysX solver velocity alternated near±.058m/s. A two-second pose/velocity trace confirmed millimeter-scale settling, not unsupported motion. Readiness now also requires a2mm ride-height window over.3s, four-wheel support and bounded.12m/s velocity. The cancelled failed receipt is retained; it is not accepted. Repeated expanded12-start test on the new terrain; early/on-time/late held-accelerator and real earned boost; complete autonomous match/downhill progress; rematch/pause/reconnect and weather regressions; actual Windows720p/1080p visual inspection and sustained timings. No human or physical-controller playtest is claimed. The current Windows artifact and its accepted evidence are identified in the final milestone below.

VENDOR ASSETS AUDITED: installed NWH WheelController public reset/spring/ground detection/visual-container APIs; existing Unity Terrain/collider and Cheese authoring; Vista/Path Painter terrain layers, original owned truck/scenery, Weather Maker and shared UI/DOTween conventions.

VENDOR ASSETS USED: NWH suspension/tire physics and visual rotation, native Terrain/TerrainCollider, existing terrain layers/truck meshes, existing weather/scenery/UI/countdown.

RELEVANT ASSETS NOT USED: no replacement Vista terrain-generation graph or vehicle controller is needed for this targeted existing-course repair. Existing height authoring remains authoritative.

CUSTOM SYSTEMS CREATED: sport-specific start preparation and gated regression instrumentation; smooth features extend the existing course height function.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

Expanded native acceptance completed withERRORS=0:12 starts, all supported participant counts with12 background trucks, early/on-time/late held acceleration, real earned jump, and a complete autonomous match across14 real catches. Final scores5–4–3–2,235 actual explosion events,0 neutral replays; the hill still produces intentional racing chaos. This evidence does not substitute for Windows or human/controller acceptance.

Windows candidate569e872d passed all12starts/boost checks and a full14-catch physical match (4–5–3–2,211explosions,0neutral,ERRORS=0). Its older guard suite correctly caught a state-API regression: StartElapsed returned3 during the new preparation phase, causing the displayed-two input test to press early. StartElapsed now reports0 until countdown begins; the original assertion is retained. This failed candidate was superseded; the accepted rerun is identified below.

## Accepted Windows milestone

All 21 strict Windows suites passed for managed assembly `ef8a78590f6119af62340482d3b2c16fcc49be010f1e87e0cfa435fb19ea7b28`. See [version-bound acceptance and limitations](PartyCheeseImpactValidation.md). The 20-mode camera suite passed 2,467 checks plus separate preference-write/read fixtures. Applicable physical, rematch and full-match regressions passed; human/hardware validation remains manual. Earlier pending paragraphs record pre-acceptance history.
