# Plowing presentation and recovery — accepted Windows milestone

The 95-file implementation is committed locally as `f85a2cd117848a24e84aae477f615034463c8d4d` and published as `82990a892dbc8cdb7511568986332d00c19986f8` on GitHub `main` and `codex/truck-sports-seven-events`. Both remote tips were read back after the successful normal atomic push. Previously published Sports work remains included. Unrelated Taxi/host/vendor changes and private Taxi ancestry were preserved and excluded.

Windows x64 Development: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the entire folder. TruckSports starts first; 35 explicit Sports-only scenes remain included. Build succeeded with zero errors and 31 warnings. All 25 protected ProjectSettings files match the pre-pass baseline; deliberate build-failure restoration passed. The original vendor moon material is back at Git HEAD; dynamic TMP fallback changes are preserved and excluded from Git.

Managed SHA256: `28cd5346297bb6d904972990440c58f0f13024576f56c05c38a0648a72039dd5`. Full 437-file data manifest SHA256: `997688c9fc0c148ce204755347c04a33058f488206ef5a575f6b049355fef60c`. [Machine-readable acceptance](PlowingPresentationEvidence/acceptance.json) contains all 24 completed, version-bound Windows outcomes. Three prior targeted cases are reused only because their assembly/data fingerprints exactly match this candidate.

## Changes and causes

- AI obstacle throttle `.3` was excluded by the old `> .3` stuck threshold; delayed reversing and changing avoidance sides prolonged wall stalls. The same physical native wall fixture improved from about 18.6 s to 7.6 s. The repair uses ordinary NWH steering/brake/reverse inputs, without added forces or score shortcuts.
- Both variants now use the installed Weather Maker night/cloud/moon renderer, an owned URP renderer/profile/material and practical festival lights. The old Unity skybox/extra daylight lamp and stale camera-eligibility cache prevented the requested sky. The actual Windows moon/cloud image verifies the correction.
- Role costumes replace persistent numeric world labels: bright clothes/blue costume noses, dark cloth masks/gloves/gold stars, and red STOP uniforms with the owned Poker dealer ponytail. Original character identity and skin do not determine points. Pitchfork props and bounded runner hops/brief prop perches add variety. The robot's owned meshes now match its humanoid rig; Taxi source assets remain untouched.
- Rounded player cards, compact viewport HUDs, readable names, short unscaled DOTween page/highlight transitions and a safe shared-camera region correct the supplied UI. The subtitle no longer overlaps setup controls; score text no longer overlaps portraits. Camera framing keeps starting trucks above score cards, with noncolliding pavement beyond the playable perimeter.
- Per-renderer clothing tint blocks increased SetPass cost. Eight Windows ablations isolated the tint comparison: baseline33.77ms/repeat36.11ms versus25.57ms without blocks, and average SetPass2279/repeat2481 versus 748. Shared runtime fabric material variants preserve the exact colors/textures/submaterials and restore batching. The palette is released on scene exit. Sky, geometry, shadows, lights, crowd count and physics were retained.
- Disabling old TMP world labels exposed a destroyed MeshFilter during settled-pose baking; the bake now checks Unity validity before accessing it.

## Observed validation

All 24 Windows suites passed: both wardrobe/night variants, real wall escape, physical scoring/impacts and retained-body resets, cab controls/GPS/jug/reconnect,384-target functionality, one-to-four-human view layouts, two sustained performance samples, six complete eight-AI matches/rematches, shared AI-camera preference persistence/ownership, swept Cornhole hole credit/guards/full autonomous match, repeated grounded Cheese starts/guards/cameras, Poker and Skee-Ball regressions. Native direct fixtures passed 16 rule checks, 1969 camera-selection checks and 100 overview projections. These are direct native/development fixtures and real Windows/PhysX checks, not a claim of a full vendor NUnit test run.

Layout captures verify actual 1280×720,1920×1080,1280×800,1024×768 and1920×800 windows across all 1–4-human shared/chase/cab layouts. Software-injected devices prove routing and exclusive whole/shared-stick ownership; physical hardware remains manual. Every AI contributed in the six full Plowing matches; spontaneous hops and occasional prop climbs were recorded. Exact fabric-color/no-block and palette-cleanup checks passed.

Two 60-real-second, four-view/eight-competitor/240-target samples measured chase mean 24.299ms/p9537.496ms and cab mean 26.574ms/p9537.526ms. Average SetPass fell to 678/670. These observed timings restore the earlier range; they are not a guaranteed frame rate.384-crowd sustained performance, audio balance, real controllers/wheels/haptics, human difficulty/readability and longer play sessions remain manual.

[Setup](PlowingPresentationEvidence/setup.png) · [Moon](PlowingPresentationEvidence/full-moon.png) · [Arena](PlowingPresentationEvidence/arena.png) · [Four cabs](PlowingPresentationEvidence/four-cabs.png) · [Standard](PlowingPresentationEvidence/standard.png) · [Bonus](PlowingPresentationEvidence/bonus.png) · [Penalty](PlowingPresentationEvidence/penalty.png).

[Customization](PedestrianPlowingCustomization.md) explains prefab clothes, palette fields, shared crowd/spawn settings, lights and explicit authoring tools. Normal builds preserve saved scenes/roster edits; regeneration tools are explicit. The full ordered queue remains active in [PartyCheckpoint.md](PartyCheckpoint.md); Fare Fight, global identities and later sports/repair requests are not marked complete by this milestone.

## Vendor accounting

VENDOR ASSETS AUDITED: NWH WheelController/SportsEventTruck, Taxi pedestrian/rig/reactions, UTS animations, owned Poker dealer hair, Weather Maker day/night/sky/URP, Heat uGUI, DOTween, Unity URP batching/profiler APIs and the existing native LOD/pooling adapters.

VENDOR ASSETS USED: Assets/NWH wheel controls; existing Taxi jointed ragdolls/reactions; UTS animation controller; Assets/TruckPoker/Art/DealerHair/Hair_Ponytail_01.fbx; Assets/WeatherMaker/Prefab (WeatherMakerURPRenderFeatureScript, DayNightCycleProfile, CelestialObject, camera cache API); Heat controls; DG.Tweening; Unity shared materials, LODGroup/Mesh combining and ProfilerRecorder.

RELEVANT ASSETS NOT USED: UTS road-traffic driving does not own free-roaming sport competitors; their existing planner still commands NWH. No installed pitchfork/ski-mask model was found in the asset audit; the small missing noncolliding costume meshes were authored over the existing models. Weatherade snow changes are outside this clear-night pass.

CUSTOM SYSTEMS CREATED: Small wardrobe/evasion/night/presentation adapters, a scene-lifetime fabric palette and targeted fixture/profiling tools over existing authorities.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
