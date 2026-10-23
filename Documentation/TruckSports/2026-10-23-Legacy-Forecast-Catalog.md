# Full legacy forecast catalog — October 23, 2026

Q22-SKY remains **PARTIAL**. Cheese Rolling and Base Jumping now offer all 33 installed Weather Maker profiles through the existing shared Heat selector, independently of time of day. Original scenes, rigs, terrain, NWH controls, scoring and course physics are retained. The shared-session integration count is 46; Cheese/Cornhole/Hockey/Wallball retain their original separate match authorities.

## Implementation and root causes

The two legacy adapters previously exposed only three visual families, with forecast selection coupled to round initialization or Base Jumping's wind-style option. They now delegate visual configuration/audio to the existing SportsChaosWeather adapter over their exact original Weather Maker world. Cheese implements the existing read-only lifecycle contract; Base retains SportsEventSession and its existing weather-field calls. Configure latches catalog selection against the real match GUID, so a new heat/attempt seed cannot reroll Random. Explicit ConfigurePreset remains available for deliberate diagnostic transitions. A new match resolves again and creates a fresh runtime clone without modifying catalog assets or Unity's gameplay RNG state.

Base's three original wind styles remain independently selectable, with their existing flow formula and canopy coupling. Cheese retains its original clear/snow/wet-storm grip and gust formulas; installed wet profiles map to its existing wet-storm family rather than introducing a replacement driving model. Snow accumulation now reads the selected visual forecast in both scenes instead of Base's old wind-option index. Weatherade/Taxi snow rendering remains the established implementation; this change does not certify every surface mask or track.

The first focused native run reproduced a real integration defect: Cheese's old authoring deliberately disabled the vendor cloud component when it controlled precipitation alone. Full profiles invoke the installed cloud manager. Explicit migration enables that existing component; no vendor source was changed and no error was suppressed. The original failed transcript remains locally archived. Existing Physics/Rigidbody/collider documents and world poses are unchanged; the two-scene review records every changed original document. Normal Play/builds do not invoke authoring or regenerate designer settings. 925 unrelated pending files remain byte-preserved.

Both saved setup pages have separate Weather and Time of Day Heat dropdowns. Base's original mode button is labelled as WIND to distinguish its retained physics styles. The original renderer/mushroom effects and existing time binding remain; Base gains the existing night-headlight coordinator. No new framework or vendor source dependencies are introduced.

## Actual validation

| Check | Observed result |
|---|---|
| legacy-forecast-focused-final-native | 4 PlayMode tests passed; 22.92s |
| legacy-forecast-46-regression-native | 1 PlayMode test passed; 95.91s |
| legacy-forecast-preference-editmode | 3 EditMode tests passed; 0.12s |
| legacy-forecast-direct-transition-native | 1 PlayMode test passed; 20.82s |
| LegacyForecastsWindows | 115 checks / 0 errors; 37.35s |
| LegacyForecastsCheeseGuardsWindows | 46 checks / 0 errors; 41.01s |
| LegacyForecastsCheeseMatchWindows | 66 checks / 0 errors; 307.38s |
| LegacyForecastsBaseMatchesWindows | 47 checks / 0 errors; 284.51s |

The focused native run covers all 33 profiles in each scene, three round refreshes per profile (198 total), matching snow state, one vendor world, retained profile instance/seed/time revision, fresh-match GUID/profile instances, existing three-sport clock regression, Cheese HUD pause/rematch and all four Base flare reset behavior. The 46-scene regression exercises saved maximum rosters with Day/Night, selectors, vendor sky uploads and existing headlights. Three EditMode checks verify independent preference persistence, invalid choices and deterministic Random without consuming gameplay RNG. A separate actual direct-profile transition test verifies snow emission stops and old flakes retire after switching to Hurricane; deliberate ConfigurePreset transitions remain functional. Runtime-only development probe changes followed the focused/46-scene tests; gameplay source is unchanged afterward. The preference/transition regressions follow the final compiled candidate.

Windows evidence belongs to the exact full payload below. The environment process produces 24 actual backbuffers: two setups, four forecast/time conditions and a two-software-human night configuration per scene, each at 720p/1080p. These captures are actual windows rather than resized source images. Existing Cheese guards exercise timed-start boosts, real truck/tree contact, explosions, input isolation and recovery. The focused four-AI Hurricane match retains its selected forecast across actual heats, reaches first-to-five, rematches and restores the host pipeline. The existing Base probe exercises actual deployment variants, landings, three complete four-AI matches with explicitly selected Clear/Rain/Snow and retained wind styles, pause/reassignment and software layouts. See verbatim receipts for actual observations and counts. The completed four-AI Cheese match finished 5–1–1–2 after 9 heats, 9 catches, 134 explosions and 455 damage applications with 0 neutral replays. Its 12,647 active-frame sample averaged 18.55 ms with 25.02 ms p95; the final Base four-view 90-second sample contains 6,428 frames averaging 14.00 ms with 18.77 ms p95 on the GTX 1660 Ti. These include host load, presentation and frame pacing; they are not isolated CPU/GPU or leak tests. No human playtest, physical-device, listening or isolated GPU acceptance is claimed.

## Build

Canonical Windows build: Build path `Builds/TruckSports/TruckSports.exe` plus all companion files. Full payload SHA-256 `ebacb6d68c60318e3742bbcbc3f93072af71a36e03983f6a1af10419a0ea34f5`; managed SHA-256 `eccd2e209cca6e921e9d587f4ac153227ddbffd18046b2228ede476ce4188742`; 549 files, 60 saved scenes. Exact 304 owned source inputs archived locally. Prior accepted build retained under Recovery.

## Remaining work

Full Q22-SKY environmental art, wintry morning colors/exposure, the currently flat lower-horizon background and silhouetted Base transport at night, venue/reflection/night-replay lighting, protected indoor/show exceptions, full snow-surface/track acceptance, score publishers beyond the Sumo pilot and results presentation remain unfinished. Carnival remains queued after Q22-SKY. This report does not reopen archived historical work or claim whole-backlog completion.

## VENDOR ASSETS AUDITED

Installed Weather Maker cloud/profile/day-night/precipitation/URP APIs; original Cheese/Base weather, snow, physics, cameras and setup; Heat dropdowns; existing NWH headlights and Weatherade/Taxi accumulation adapter.

## VENDOR ASSETS USED

Weather Maker registry of 33 profiles, sky/clouds/precipitation/time; Heat dropdowns; existing NWH/WheelController handling/headlight APIs and Weatherade/Taxi snow presentation. Original mushroom renderer is retained.

## RELEVANT ASSETS NOT USED

No new terrain, road, streaming, traffic or animation framework is needed for this forecast integration. Full Weatherade surface reauthoring is separate remaining work.

## CUSTOM SYSTEMS CREATED

No new framework. Existing LWS weather/lifecycle/authoring adapters and focused validation were extended.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
