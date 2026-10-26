# Seven field, road and beach venues — October 24, 2026

Freeze Tag, Frogger, Hungry Haulers, Jousting, Limbo, Red Light Green Light and Sheep Herding now have saved artificial illumination controlled by the existing match clock. Pre-fix Windows images showed dark play surfaces with isolated truck-headlight patches.

| Venue | Saved lamps | Placement |
|---|---:|---|
| Freeze Tag | 6 | Outside field boundaries and outer seating |
| Hungry Haulers | 6 | Outside collection arena and seating, on the actual surrounding ground |
| Jousting | 6 | Meadow beside the lanes, inside distant seating |
| Limbo | 4 | Flat sand outside the colored runways |
| Red Light Green Light | 8 | Outside barriers and outer bleachers |
| Sheep Herding | 8 | Bases raycast against the actual uneven saved terrain collider |
| Frogger | 78 across five courses | Roadside banks, excluding water and transverse traffic/return corridors |

All 82,275 original scene documents remain. Changed original records are the existing clock configuration and fixture-parent child lists. New objects contain render geometry and non-shadow lights, with no physics or gameplay scripts. Explicit additive authoring preserves populated light arrays and existing roots; entering Play Mode, normal builds and rematches do not regenerate fixtures. Frogger's original course activation owns its lights.

Hide and Seek remains separate. Eighteen lights in its older authoring code are absent from the current saved scene. This pass does not claim those fixtures exist or alter its stealth presentation.

## Causes and corrections

Long road meshes exhausted local-light selection. The initial native suite failed Frogger; Red Light's road was also visibly weak despite passing an image-difference check. A temporary cap increase fixed Red Light but not Frogger. A controlled Forward+ comparison fixed Frogger without changing geometry, camera or fixtures.

Red Light changes only its local additional-light cap from four to eight. Frogger uses eight and a local Forward+ copy of the existing owned Base Jumping renderer. The embedded Weather Maker feature and original Weatherade depth renderer remain intact; the source renderer is unchanged.

The first Windows candidate passed 141 + 79 state/rendering checks and exited cleanly, but visual inspection rejected its dark dawn and washed-out snowy night. That candidate, sources, scenes and captures are retained. Controlled Snow and Blizzard lamp-power comparisons confirmed the brightness response. The existing clock adapter now has optional editable dawn and covered-surface lamp gains, with defaults preserving other venues. Only Frogger binds its original snow component: dawn uses 0.65 of authored power; substantial snow eases it to 0.15. Cached gain updates allocate nothing and do not introduce another weather, exposure or light manager.

## Actual validation

- Seven final venue PlayMode tests passed: Editor 38956, exit 0, 126.06 seconds. Original mounts, absence of new physics, time selection, pause, rematch identities, real GPU lighting and all five Frogger course activation groups were exercised.
- The existing five-venue clock integration test passed: Editor 35884, exit 0, 55.55 seconds. Eight existing time/score EditMode tests passed: Editor 38924, exit 0, 28.09 seconds.
- All 18 final controlled 720p/1080p native images were inspected. Dawn captures include the existing setup fade and establish lighting visibility, not full dawn HUD acceptance. Snow coverage was 0.77578 with four road contacts and the original depth renderer retained.
- Final Windows Day/Night probe: 141 checks, zero errors; PID 40052, exit 0, 59.77 seconds. Original nine-case Frogger weather/time matrix: 79 checks, zero errors; PID 37400, exit 0, 42.87 seconds. All 46 actual gameplay captures at 720p/1080p were inspected; setup captures are not credited.
- Original failed native tests, temporary diagnostic results and the first failed visual candidate remain distinguishable. Passing scripted state checks were not treated as aesthetic acceptance.

These are focused environment and lifecycle checks. Full matches, human driving, physical controllers, subjective listening, complete snow-track acceptance and isolated performance measurements were not performed in this slice.

Windows x64 development candidate: `Builds/TruckSportsCamera/TruckSports.exe`, 60 saved scenes and 427 frozen owned C# inputs. Full content: `050c1676b1b825723da426e9a13841206388bd03d89293444ff226fd975e20b0`. Managed assembly: `09e5001290272210996c77b33d9ef17e7aeb8e75a9ed04aa8b7466211b4203f0`. The exact source manifest and build summary accompany the evidence.

The accepted `Builds/TruckSports/TruckSports.exe` remains unchanged at `60d4716f`. Previous full candidates are retained under `.sports-validation/Recovery/`. No promotion or public push was performed. The separate Dock native shutdown blocker remains on the user-retained Editor. All 925 protected unrelated pending files remain unchanged. Parent Q22-SKY, complete Carnival 0.6 and Impossible 0.7 remain open.

## VENDOR ASSETS AUDITED

Toon Gas Station Streetlight_1C; Weather Maker clock and renderer feature; Weatherade snow/depth rendering and cluster-light support; installed URP lighting configuration; original scene geometry and course activation.

## VENDOR ASSETS USED

`Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab` with the existing shared owned URP material; `Assets/WeatherMaker/Prefab/Scripts/Sky/WeatherMakerDayNightCycleManagerScript.cs` through the original `SportsMatchTime` binding; Weatherade coverage through `Assets/LWS/InterstateHauler/Roads/Conditions/LwsWeatheradeAdapter.cs` and the original snow depth renderer; supported URP spotlights and Forward+ configuration. Installed shader support was inspected in `Assets/NOT_Lonely/Weatherade SRS/Shaders/Includes/SRS_Lighting.hlsl` without modifying it.

## RELEVANT ASSETS NOT USED

No alternative weather, exposure, physics or navigation framework was needed. Existing scene authoring and course activation supply the integration.

## CUSTOM SYSTEMS CREATED

Explicit additive venue layouts, targeted native acceptance and optional lamp-gain semantics in the existing clock adapter. One local configured renderer copy. No new runtime manager.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
