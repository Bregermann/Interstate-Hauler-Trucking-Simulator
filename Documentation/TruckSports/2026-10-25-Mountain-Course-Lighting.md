# Mountain and meadow venue lighting — Q22-SKY-LIGHT-COURSES-01

Six saved courses now have supported night lighting instead of relying mainly on truck headlights. Original physics, roads, terrain, AI routes, cameras and scoring remain intact. Strict native scene review retains all61,796 original serialized records; new scene records contain only transforms, models and lights, plus existing clock bindings. The explicit authoring methods never run during builds, loading or rematches and preserve subsequent designer edits.

| Venue | Fixtures | Placement |
|---|---:|---|
| Avalanche Escape |140| Real authored route sections, with shared sections deduplicated; terrain-supported footholds |
| Bog Snorkelling |34|12 Classic and22 Hard shore fixtures, owned by their original terrain activation |
| Mattress Racing |26| Along the downhill meadow; footings in the capped bank region with buried bases that accommodate bounded terrain noise |
| Minefield |186| Actual side meadows and extended landing runout through8,280m; no change to mines or scoring |
| Wife Carrying |20| Both sides of the existing loop circuit |
| Cheese Rolling |16| Original high platform, slope and runout; terrain heights use the established course formula |

The real saved pipeline owner is used for each course. Minefield and Wife Carrying retain their active Weather pipelines; their unused snow-pipeline copies are unchanged. Six mode-local Forward+ renderer copies preserve every original embedded feature and the existing Weatherade depth renderer. Wife's existing underwater feature and Cheese's mushroom feature remain alongside Weather Maker. Original renderer source assets are unchanged in the host; one isolated Bog copy has equivalent text with different line endings.

The existing clock supplies night/evening light, optional65% dawn gain, daytime zero contribution and snow-aware power reduction. No additional lighting/time authority was introduced. The initial bright-light trial was rejected after image inspection; the final authoring powers were reduced per venue to retain night atmosphere and avoid the neon Cheese/meadow exposure. The original high-brightness evidence and earlier failed test assumptions are retained.

## Actual verification

- Read-only native support audit15304 exited0/50.21s. Initial authoring39536 exited0/52.56s; Mattress support correction45868 exited0/35.2s; active-pipeline correction47008 exited0/37.55s. Native scene exports exclude unrelated Unity null/default serialization.
- Final retuned native46652 exited0/104.51s:6PlayMode tests passed/65.4695554s. Actual gameplay view light contribution, day zero contribution, Night rematch/pause identities, supported Mattress footing, extended Minefield placement, course activation and renderer features checked. Camera-only mean luminance was.186–.381; these are image samples, not a calibrated HDR or performance benchmark.
- Explicit Classic/Hard Bog40936 exited0/65.01s:1focused test passed/19.0487187s. All14 final native720p/1080p images individually inspected. These are controlled native camera captures, not player-window or human playtests.
- First exact Windows build4f5ca246 (548files,60scenes,438owned C# inputs) compiled successfully with0errors/46warnings. Its output-routing incident is recorded below. Corrected surface/probe build47236 was interrupted during the tool/process connection reset, with no success receipt. Its frozen440source list and partial log are preserved; no final artifact acceptance is claimed. Seven remaining venue integrations are included in the completed combined candidate below.
- Exact4f5 Windows weather matrix42112 exited2:391checks/1error, one Bog720p capture remained1080p. Legacy matrix50816 exited2:121checks/3errors, one Cheese720p capture and Base two-human start/view count. All89 available weather/time frames reviewed through10labelled640px overview sheets; no substitute for the missing frame. These failed runs are retained, not waived.

Avalanche road bands were isolated to its staging platform renderer sharing the exact430m road elevation. Renderer-only toggles distinguish it from the terrain/shortcut layers. Explicit native authoring disabled only that redundant renderer; strict export retained all11,307current scene records and changed only MeshRenderer1692076831 m_Enabled1→0. All colliders, meshes, transforms, roads and terrain are byte-unchanged by the repair. Its native collision-preservation regression passed/10.240695s; repaired720p/1080p native frames individually inspected. Exact248Windows Day/Night and nine weather/time case images confirm the renderer repair; no original collision geometry was changed.

The Base Jump fixture failure exposed valid key mappings with a software keyboard disabled by lost application focus. The explicit probe now uses a temporary IgnoreFocus settings clone, matching the project’s existing development probes, and restores it on cleanup. Native follow-up50404 exited0/152.9s:1test passed/1.9813239s with an enabled software keyboard and two actual human camera views. Production input validation is unchanged. Capture retries also reissue the real Windows resize request at bounded intervals while continuing to reject mismatched captures. Both earlier failed native diagnostic runs remain retained. Corrected actual Windows capture and software-human matrices now pass as recorded below. Complete-course traversal, human driving, physical controllers, listening and isolated CPU/GPU profiling remain unperformed. ParentQ22 remains unfinished; the remaining camera/feedback work, complete0.6 and0.7 work continues separately.

Candidate: `Builds/TruckSportsCamera/TruckSports.exe`. Previous completea59c4c8a candidate is preserved before replacement. The canonical `Builds/TruckSports/TruckSports.exe` currently contains preserveda59c4c8a; the old accepted60d runtime was not recovered after the output-routing mistake. No canonical promotion or public push in this milestone. The independently recorded Unity6000.4.10f1 native shutdown blocker is not waived.

## Combined Windows validation

Combined candidate248fd02f (548files,60registered scenes,442exact owned C# inputs) built in Editor42376, exit0/303.4s. Actual Windows Day/Night18600 exit0/90.62s:206checks/0errors; weather20756 exit0/88.93s:157/0; legacy52880 exit0/53.94s:124/0. Total487checks/0errors and three clean exits. All108gameplay frames reviewed through20overview sheets plus individually viewed two-human images;30setup captures excluded. Initial Minefield720p camera sky wedges remain tracked under Q22-SKY-CAMERA-MINEFIELD-START-01. Prior failed runs remain retained. Canonical full content before/after unchangeda59c4c8a9975748c3b0986703429748238087af50d51a0b9a84c8d28e4156495; this is the preserved candidate, not the unrecovered old60d accepted runtime. No canonical promotion/public push. Human/device/listening/full-course/performance acceptance remains unperformed.

## VENDOR ASSETS AUDITED

Installed Toon Gas Station fixture geometry; original EasyRoads course meshes and route metadata; actual NWH/PhysX vehicles; URP pipeline ownership/features; Weather Maker time/sky; Weatherade depth, accumulation and existing snow gain; existing course activation and terrain generation.

## VENDOR ASSETS USED

`Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab`, existing shared Sports fixture materials, URP Forward+ and original vendor renderer features. Weather Maker and Weatherade remain the weather/time/snow authorities. Actual EasyRoads, NWH and course data are retained.

## RELEVANT ASSETS NOT USED

No replacement traffic, vehicle, terrain generator, UI, road, weather or time system was needed. New poles reuse the installed model; no new custom lamp art or vendor substitute was created.

## CUSTOM SYSTEMS CREATED

No new runtime system. Existing authoring gains explicit course-specific placement, narrow reviewed exposure correction, and actual-pipeline light-budget setup. Focused native tests and local evidence tooling were added.

## VENDOR SOURCE MODIFIED

None. Original Truck Taxi source, original terrain/road assets and original renderer sources remain unchanged.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None. Native URP lighting and existing vendor adapters retain their responsibilities.
