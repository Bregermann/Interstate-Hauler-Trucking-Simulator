# Urban road and industrial night lighting — October 24, 2026

Fare Fight and Truck Snake now have saved street fixtures controlled by the existing match clock. Hide and Seek's existing supported loading/staging lights now follow that clock without changing their night brightness or geometry.

| Venue | Lamps | Scope |
|---|---:|---|
| Fare Fight | 80 new | Original district graph and actual static supports |
| Truck Snake | 350 new | Original town, graded roads and supported elevated-road shoulders |
| Hide and Seek | 30 existing | 18 loading lights at 75 power and 12 staging lights at 110 power retained |

All 98,399 original scene documents remain. The road scenes change only the clock light array, optional dawn/covered gains and one parent child list each; Hide changes only its existing clock light array. New objects contain installed render geometry and non-shadow lights, without colliders, rigidbodies, joints or gameplay scripts. Static support raycasts and crossing-road/solid-obstacle exclusions determine mounts. Explicit additive authoring preserves populated arrays and existing fixture roots; Play Mode, normal builds and rematches do not regenerate fixtures.

## Causes and corrections

The scenes lacked bound road lighting. Hide's saved supported fixtures are distinct from 18 absent lights in old authoring code. The initial retained note incorrectly counted 28; actual native/serialized enumeration established 30, which the final tests verify with exact original powers.

The first native acceptance failed Snake's meaningful camera-illumination threshold. Fare passed numerically but visual inspection rejected per-mesh lighting cutoffs. Temporary unsaved cap 8/Forward+ comparisons isolated per-object light selection on long meshes: Fare562→1558→44589 and Snake5697→11459→28668 brighter pixels out of 57,600, within each frozen comparison. All four diagnostic images were inspected.

Both modes use local Forward+ copies of their original Weather renderers. Only owned names and rendering mode differ. Active pipelines change only their first renderer reference and additional-light limit 4→8. Original Weather Maker features, Weatherade depth renderers and source renderer assets remain intact.

A second native run caught that Snake actually references its original Weather pipeline, while an equivalent snow pipeline is unused. The active pipeline was corrected, the unused asset restored byte-for-byte, and the same acceptance threshold retained. Both failed native results and diagnostic evidence are preserved.

## Actual validation

Final three-venue PlayMode: 3 passed, Editor 40172 exit 0 in 100.98 seconds, test duration 61.1235793 seconds. Original support surfaces, zero added physics, exact Hide powers, five time transitions, pause, rematch object identity and actual camera light contribution passed. Fare 44,435, Snake 28,782 and Hide 10,026 brighter pixels out of 57,600. All 14 fresh native 720p/1080p captures were inspected. Four Clear dawn captures retain the original setup fade; those establish lighting visibility, not full dawn HUD acceptance. Existing snow coverage drives the configured gains. Warm orange snowy-morning ambient colors outside light pools remain a parent Q22-SKY-SKY-01 task.

Existing shared time-preference and confirmed-score EditMode: 8 passed, Editor 40772 exit 0 in 27.54 seconds, test duration 0.1734772 seconds.

The first Windows candidate passed 61 + 157 scripted checks with clean exits, but actual dawn images failed visual acceptance. That full build, exact source/config inputs and inspected failure frames are retained. Only 18 of its 36 weather images were inspected; no review credit for the other 18. The original clock now uses optional dawn 0.65 and covered 0.3 gains bound to each original snow component. No runtime framework or vendor change was needed.

Windows UrbanVenueDawnAfterWindows: 61 checks, zero errors; PID 38576, actual exit 0 in 32.64 seconds. All 12 actual 720p/1080p gameplay captures inspected; setup captures not credited.

Windows UrbanRoadDawnWeatherAfterWindows: 157 checks, zero errors; PID 41196, actual exit 0 in 93.61 seconds. All 36 actual 720p/1080p gameplay captures inspected; setup captures not credited.

Windows x64 candidate: `Builds/TruckSportsCamera/TruckSports.exe`, 60 scenes, 428 frozen owned C# inputs. Full content: `40fcf3dafe5bbb6d0fb6fbf83c31aa063d784e6836e3aa7f0da6b352cffc15ca`. Managed assembly: `09e5001290272210996c77b33d9ef17e7aeb8e75a9ed04aa8b7466211b4203f0`. Accepted `Builds/TruckSports/TruckSports.exe` remains 60d4716f; no promotion or public push. The separate known Dock shutdown blocker on the user-retained Editor remains open.

These are focused environment/lifecycle checks and AI-only starts, not complete sport matches, human driving, physical-controller, subjective listening, snow-track or isolated performance acceptance. All 925 protected unrelated files remain unchanged. The previous full candidate is preserved. Parent Q22-SKY, Carnival 0.6 and Impossible 0.7 remain open.

## VENDOR ASSETS AUDITED

Installed Toon Gas Station Streetlight_1C model and original shared URP material; original EasyRoads/LWS road geometry and graph; Weather Maker clock and renderer feature; Weatherade depth/track renderer; installed URP Forward+ configuration.

## VENDOR ASSETS USED

Streetlight_1C through existing owned presentation fitting; original SportsMatchTime over Weather Maker; existing road graphs and static road/terrain colliders for placement; supported URP spotlights and mode-local Forward+; original Weatherade renderer retained.

## RELEVANT ASSETS NOT USED

No alternate road, navigation, weather or lighting framework was needed. Existing saved roads supply placement data; no road regeneration or new runtime spawner was introduced.

## CUSTOM SYSTEMS CREATED

Explicit additive editor placement and targeted acceptance fixtures. No new runtime manager.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
