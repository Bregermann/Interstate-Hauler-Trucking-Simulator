# Eight additional outdoor venues — October 23, 2026

Q22-SKY remains **PARTIAL**. This milestone integrates the existing shared weather/time presentation into eight audited outdoor venues that had no weather owner. It does not finish global scoring/results, every remaining lighting request, snow accumulation, hardware acceptance, or the queued Carnival directive.

## Delivered behavior

Dock Diving, Buzkashi, Bungee Demolition, Fishing, Sumo, Log Rolling, Trampoline Dodgeball and Demolition Derby now have independently saved Heat weather/time controls, the installed Weather Maker preset registry, frozen match time, the owned vendor sky/star profile, orthographic camera support, night vehicle lights and bounded venue lights. Rematches use a fresh match identity; pause/resume retains the selected weather instance and clock.

The optional `SportsEventSession.presentationWeather` field is assigned only in these eight scenes. Existing sports retain their original weather owners. The new hook configures after `BeginGame`, ticks the existing adapter, and pauses/resumes it; it does not change scoring, input ownership, vehicle forces or grip calculations, and adds no gameplay RNG seeding. Its weather seed comes from the private match identity. Weather selection changes the vendor atmosphere; this milestone does not add new grip/air-force rules to sports that did not have them.

Dock and Log Rolling retain their existing scene-scoped `BaseJumpRendering` owners. Their pipelines/renderers are cloned into each sport's Rendering folder with the original underwater feature retained and the vendor weather feature added. Derby retains its eight existing venue lights. Other fixtures use the tracked Toon Gas Station streetlight model; new lights cast no additional real-time shadows. Added vehicle lights remain outside recorded truck hierarchies.

## Root cause and safety evidence

These scenes had fixed sun lighting and no shared weather/time lifecycle. The first targeted native test also exposed an all-AI camera omission: Bungee replaces `session.cameras` with a broadcast view, while initial authoring registered only the setup array. Explicit authoring now registers every authored `SportsEventCamera.view`, including spectator, replay and driving views. The failing XML/log is retained in local History; the corrected test passed.

The first Windows candidate (`60828dd9...`) passed 161 automated checks but was rejected on visual review: Dock's night pool surround was too bright and Derby's perimeter too dim. Explicit authoring reduced Dock's new fixtures from 3800 to 1800 and widened the eight existing Derby spots from 80 to 115 degrees, with range 65→110 and intensity 1800→4500. No extra Derby lights or shadow casters were added. The final Windows receipts/captures below use the rebuilt payload; the first candidate's results/images remain in local `NewOutdoorsWindows` evidence.

The next candidate (`10cf721e...`) also passed 161 checks; setup review caught Buzkashi's existing Team Names button touching the control-help text. Its authored position moved from y=-268 to -225, preserving the button and its navigation. The native layout regression now checks control-help text against setup buttons for these eight venues. Intermediate captures remain in `NewOutdoorsFinalWindows`; final acceptance uses the fresh payload below.

Candidate `1590a8ae...` rendered the corrected layout, but one capture requested at 1280×720 still returned the prior 1920×1080 swap-chain buffer. The PNG header confirmed that mismatch and the acceptance run correctly failed. The development-only capture harness now waits/retries up to ten times, checking actual captured dimensions each time. It never rescales evidence or substitutes an offscreen camera render. The failing run remains in `NewOutdoorsAcceptedWindows`; final receipts below use the updated harness and rebuilt payload.

`new-outdoors-scene-review.json` records the additive change for all eight scenes. Every original scene document remains present; original Rigidbody/collider documents and original world poses are unchanged. Existing renderer features are verified natively. 925 unrelated pending files were rehashed and preserved. Normal builds and entering Play Mode do not invoke the migration or rewrite designer settings.

## Actual validation

| Check | Observed result |
|---|---|
| new-outdoors-final-native | 1 PlayMode test passed; 18.66s test time |
| outdoor-43-regression-native | 1 PlayMode test passed; 98.53s test time |
| outdoor-sumo-score-regression-native | 1 PlayMode test passed; 9.55s test time |
| NewOutdoorsStableCaptureWindows | 161 checks / 0 errors; 66.38s; PID 12040 |
| DockWeatherTimeMatrixStableCaptureWindows | 79 checks / 0 errors; 42.92s; PID 74264 |

The first test exercises all eight maximum rosters, Day/Night, UI bounds, single weather/pipeline ownership, retained water features, all authored camera registrations, night fixtures and pause/resume. The broader test exercises all 43 compatible saved outdoor sessions. The Sumo test uses software keyboard events, actual NWH/PhysX bumper contact and survival departures, and verifies score receipts, Heat feedback, replay and rematch behavior. These are automated PlayMode fixtures, not human gameplay.

Windows acceptance uses the actual executable/backbuffer at 1280×720 and 1920×1080: Day/Night starts in all eight venues and nine weather/time combinations in Dock. Setup and night captures were inspected, including the corrected Buzkashi button and Dock/Derby fixtures, and all nine Dock matrix conditions were inspected at 1080p before promotion. The probe records 60 whole-player frame intervals per combination; these short starts are not sustained CPU/GPU/memory profiling, complete matches in all eight sports, physical-controller testing or listening acceptance. See the accompanying evidence images and receipts.

There are 25 recorded blocks / 1500 frame intervals: median range 6.25–6.26 ms, p95 range 6.27–12.52 ms. These include the application frame cap and complete player workload; they do not isolate GPU/CPU cost. Structured values are in `short-frame-samples.json`. Native gameplay/scene tests preceded the final development-only capture-wait refinement; the final capture code was compiled and exercised by both Windows runs.

## Build and source

`Builds/TruckSports/TruckSports.exe` plus all companion files. Full payload SHA-256: `a78af35303e2cf2ded9e223fabba6af256d1b301b44f152d9ae45ad8e3dc4dd5`. Managed assembly SHA-256: `76bae8fe8c6a64152be3f683e8c3c6ab30efb12be82a2bba994504aa188e9707`. 547 payload files, 60 saved build scenes including the front end. Exact compiled owned source inputs are archived in local build evidence.

Changed runtime: `Assets/TruckSports/Scripts/SportsEventSession.cs`; development-only matrix selection in `SportsEnvironmentProbe.cs`. Changed tooling: `SportsEnvironmentAuthoring`, `SportsChaosAuthoring`, `SportsWeatherRenderingAuthoring`. Changed native tests: `CollectionEnvironmentTests`. Each venue owns its saved scene and new Materials/Rendering configuration assets. The exact asset scope is in the evidence folder.

## Remaining work

- Remaining legacy outdoor owners/venue-specific artistic lighting, exposure/reflections, headlights during recorded replays, and complete snow/track acceptance remain open under Q22-SKY.
- Dock's morning foreground is still subdued and snowy morning retains the warm sunrise tint. Weatherade coverage for exposed surfaces in these eight venues has not been authored; the matrix demonstrates snowfall, not accumulated snow or tracks. These remain implementation/art acceptance work, not passed acceptance.
- New weather-driven traction/force behavior in these eight sports was not part of this additive presentation hook; existing driving/physics rules were preserved.
- Complete-match, multi-human device, listening and sustained performance acceptance are not established by these short-start captures.
- Shared confirmed scoring currently has the delivered Sumo integration; other sport publishers and global results presentation remain unfinished.
- The Carnival directive remains queued after Q22-SKY. No whole-backlog completion is claimed.

## VENDOR ASSETS AUDITED

Installed Weather Maker prefab, profile/day-night/sky/wind APIs and URP feature under `Assets/WeatherMaker/`; existing scoped pipeline/water feature under `Assets/TruckBogSnorkelling/`; NWH lighting components already used by the shared adapter; Heat setup widgets; `Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab`; existing Weatherade snow integration boundaries.

## VENDOR ASSETS USED

Weather Maker's existing prefab, preset registry profiles, precipitation/cloud/lightning/sun/moon/time and URP rendering; existing Heat dropdowns; NWH light manager through the existing project adapter; Toon Gas Station fixtures; the existing Bog underwater renderer feature. Dependencies were already tracked. No new licensed vendor source is published.

## RELEVANT ASSETS NOT USED

Weatherade surface accumulation was not newly authored here: exposed-surface/water masks require separate sport-specific acceptance. No substitute accumulation system was created. Existing input, camera, animation and physics systems were retained.

## CUSTOM SYSTEMS CREATED

No new weather or rendering framework. Existing LWS lifecycle, additive scene authoring, vendor adapters and targeted validation were extended.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
