# Environment/time foundation — October 22, 2026

This milestone implements and validates the shared time controls and vendor lighting bridge in **five representative outdoor scenes**: Keepaway, Field Goal, Sky Tow, Terrier Racing and Truck Snake. Q22-SKY remains PARTIAL. Collection-wide artistic sky work, remaining venue integration, score events and results are still unfinished. Q22-CARNIVAL follows that work.

Players can choose Random, Morning, Day, Evening or Night independently of weather using the existing Heat setup controls. Day is the default for existing installations. A match holds its chosen time across attempts/heats; a new match resolves Random again. The preference persists through scene changes and a fresh Windows process. Original day/night profile assets remain unchanged.

The installed Weather Maker3D prefab had no orthographic sun entry, causing its camera-specific lookup to return null for the arena lens and the cycle update to return early. A project-owned, non-illuminating lookup entry mirrors the vendor astronomical sun; the3D venue uses the supported Perspective weather setting. The existing camera projection/ownership is retained. The fixed standalone noon fill is disabled so it cannot keep night scenes lit like daytime.

Sky Tow also retained a static procedural Arena sky at night. All five scenes now use Unity viewport coverage with the existing owned sky geometry shader calling Weather Maker's installed sky include, followed by vendor clouds. Four authored commercial-model lamps each illuminate Keepaway and Field Goal at evening/night, with no additional shadow maps. Field Goal lamps follow its existing field assembly. Other venue lighting remains open.

Keepaway uses a custom setup layout. Initial generic coordinates overlapped its title and left a misleading legacy weather control. The corrected four-column Chaos/Weather/Time/Seed header retains the roster and other options, and hides that obsolete control. Snake and Sky Tow use their spare right settings column with visible labels. The time list fits its five choices. Heat's disabled-scrollbar initialization attempts to remove a component required by SmoothScrollbar; the owned widget retains both components and uses uGUI AutoHide instead. Explicit versioned authoring preserves future designer positions; normal builds/Play Mode do not run migrations.

## Actual verification

- Three EditMode preference tests passed during implementation: independent persistence/defaults, deterministic Random without consuming gameplay Random state, and invalid stored/public values.
- Final native PlayMode suite: two tests passed, covering five saved scenes × all four explicit times plus Random, actual camera-specific sun updates, frozen/latching clocks, source-profile preservation, night lamp states, visible labels and no heading overlap across all five menus, no obsolete Keepaway control, and natural snow retirement.
- Final exact Windows payload:171 checks/0 errors, actual720p/1080p backbuffer captures, real Heat software-gamepad selection/keyboard navigation, nine Field Goal weather/time combinations, repeated starts, and two software-keyboard Sky Tow views.
- Fresh second Windows process:1 check/0 errors for saved time persistence. These172 checks share the exact payload below.
- Actual rendered captures were inspected. They demonstrate the corrected Keepaway setup, compact selector, repaired Arctic night and illuminated stadium. Files in Evidence distinguish original rejected visuals from corrected output.
- The initial snow concern was the authored particle lifetime: emission stopped within.35s of Hurricane selection; old flakes retired within8s. No vendor precipitation fix or abrupt particle clearing was applied.

Rejected histories are retained. Earlier763candidate passed164 logical Windows checks but kept a blue Arctic night sky. Candidate6d passed171+1 logical checks but further setup inspection found the Keepaway overlap. It was briefly promoted, then the previous accepted build was restored while repairing it; that rejected executable and both earlier accepted backups were preserved. The later be5 candidate passed logical checks but further five-menu inspection found Snake/Sky Tow layout problems, so it was never promoted. Overlaps were reproduced by failing native geometry tests. The final test measures rendered heading glyphs, avoiding false positives from empty title-container padding. Fixture-selection/compilation failures and the Heat initialization failure are retained separately.

Whole-player frame intervals are recorded in frame-intervals.json (60frames per combination); they include the full player workload and are not isolated CPU/GPU benchmarks. Human playtesting, physical controllers/wheels, listening, extended soaks and full collection performance acceptance were not performed. The two-human view was rendered; it has no separate frame-time sample block.

## Files and preserved work

Runtime: Assets/TruckSports/Scripts/SportsTimeOfDay.cs, SportsMatchTime.cs, SportsChaosWeather.cs, SportsWeatherPicker.cs, SportsEventSession.cs. Explicit tooling: SportsEnvironmentAuthoring.cs and SportsWeatherPickerAuthoring.cs. Development-only Windows probe: SportsEnvironmentProbe.cs. Native regressions: TimeOfDayPreferenceTests.cs, MatchTimeIntegrationTests.cs, WeatherTransitionTests.cs. Runner adds time-audit/time-author stages.

Five saved scene files receive additive bindings/UI/lamps and targeted sky/clear settings. Original scene document IDs are retained; no original physics/collider/game-rule objects were removed or rebuilt. Five owned sky materials and one Field Goal URP material variant are new. Original licensed prefabs/materials, Poker's indoor venue and both protected Plowing skies/rosters are unchanged. All925 unrelated pending files remain byte-identical.

Build: Builds/TruckSports/TruckSports.exe,60scenes/546files. Full content SHA256: 61311f68e3ee704402e19313f0cc9333fd370c51b721b60c7e2d28db3cd53a06. Managed SHA256: 12b332cf9f6af08e726e7b472ad8a980db3c2a612c20b328c0e6a8f12d4c54ab. Exact281owned source inputs are retained. Promotion receipt records the previous build backup.

## Remaining Q22-SKY work

Extend time/environment integration to the remaining applicable registered scenes and legacy weather adapters; retain indoor/concert exceptions. Finish individual registered weather sky variants, richer clear/sunset/cloud/stars/moon presentation and venue-specific artificial lights/headlights. The short Blizzard capture does not establish substantial turf snow coverage or tyre-track acceptance. Complete authoritative stable-ID/deduplicated score animation integration and actual tied results/statistics across sports, preserving Poker/Bowling/Kickball exceptions. Complete required rendered/performance acceptance at representative maximum rosters and split-screen counts. These are not closed by this foundation milestone.

TUG OF WAR: Brainstorm additional mechanics with the developer and ChatGPT. Current gameplay is too dependent on holding reverse. No changes were authorized during this pass.

## Vendor accounting

VENDOR ASSETS AUDITED: Weather Maker manual, cycle/sky/light/precipitation APIs,3D and2D prefab sun patterns; installed Heat dropdown/SmoothScrollbar and uGUI hierarchy; existing Toon Gas Station streetlight prefab; existing sport rendering/UI authoring.

VENDOR ASSETS USED: Weather Maker weather profiles, astronomical cycle, sky include, clouds/precipitation/light managers; Heat/TMP controls and existing DOTween menu presentation; Toon Gas Station Streetlight_1C via the existing owned model/material adapter. Existing vehicle/traffic/navigation systems remain in place.

RELEVANT ASSETS NOT USED: The complete Weather Maker2D world is not substituted for3D sport scenes; only its supported camera-specific celestial pattern is adapted. Weatherade surface accumulation, terrain tooling and reflection systems are not replaced or expanded in this milestone.

CUSTOM SYSTEMS CREATED: Shared time preference and match coordinator, lookup-only sun adapter, explicit versioned scene/Heat migrations, owned sky/material instances and focused acceptance probes/tests. No new weather or vehicle simulation framework.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None. Installed Weather Maker owns sky/celestial/weather calculations; Heat/DOTween own the existing presentation mechanisms.
