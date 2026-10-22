# October 22 — Precision camera layouts and Windows sky coverage

Two reproduced presentation defects are repaired. The shared HUD refresh had a 100 ms layout throttle: a rematch could leave newly selected viewport cards inactive until its next tick. `SportsEventMenus.Show` now resets that timer. A rendered-frame native regression reproduced the original failure and now passes; actual Windows tests verify the real card owner/root/name, including human slots later in the roster and AI views.

Minefield and Cornhole also showed uncovered sky regions in the actual Windows backbuffer. Native offscreen HDR captures did not reproduce them. Clear-color isolation turned those regions magenta; temporal reprojection, full-resolution clouds, post-processing, MSAA and actual D3D12 isolation did not remove them. This narrows the cause to background coverage in this vendor/URP split-screen rendering path; the exact low-level blit/matrix defect is not claimed proven.

The repair uses Weather Maker's documented alternative: Unity Skybox camera clearing and vendor clouds at AfterSkybox. A small owned shader calls the installed `ComputeSkySphereColorFullScreen` implementation, including its night-rotation path, rather than substituting a new sky algorithm. Each affected scene has one saved owned material. No ordinary runtime manager or per-frame override was added. Minefield changes 7 fields across its 16,700 original scene documents; Cornhole changes 6 fields, with all original documents retained. Both modes' gameplay scripts and all 925 unrelated pending files remain unchanged. Other sports keep their own protected cab, precision and strategic cameras.

Native final targeted validation: 9 EditMode projection/framing tests and 4 PlayMode saved-sky/weather/rematch/storm-camera/HUD tests passed. The earlier 8 PlayMode pass includes 6 shared compact-HUD regressions plus the Minefield/Target tests repeated in the final run; these counts are not presented as unique tests. Earlier failed harness runs and rejected render candidates remain in Logs/TruckSports/PlaytestRepair/History and their original run folders.

- PrecisionCameraFinalWindows: 843 checks, 0 errors; PID 66084, 50.27 real seconds, D3D11.
- PrecisionSkyFinalWindows: 295 checks, 0 errors; PID 65008, 43.46 real seconds, D3D11.
- MinefieldSkyFinalD3D12: 117 checks, 0 errors; PID 66892, 14.22 real seconds. The player log confirms Direct3D 12, level 12.1.

The exact Windows payload exercises all nine saved camera configurations with zero-to-four software-human inputs, AI views OFF/ON, spectator fallback, actual ownership/rectangles, rematches and menu return. Both sky repairs additionally exercise Clear, Heavy Rain, Snow, Blizzard and Hurricane. Actual 720p/1080p rendering was inspected. These are paused/short-live presentation fixtures, not complete gameplay matches, snow-accumulation acceptance, human approval or hardware/controller/listening/performance certification. Existing per-sport full-match evidence is retained separately. Validation uses deliberately long stress-test participant names.

The short Minefield sequence still shows snow particles during the subsequent Hurricane capture. Their transition/lifetime needs investigation during the queued weather directive; correct profile selection alone does not establish correct precipitation presentation. This repair certifies sky coverage, not that broader weather requirement.

Delivered Windows x64: `Builds/TruckSports/TruckSports.exe`, 60 scenes, 546 files. Full payload `1363d34f2710ddeecebf347361979da22780e1270a5a8d4d7c4b2dd9fa6b8327`, managed `fafb6d060823d8c8249b9244f26d7182e85c5e1768c8df3c3d8d5526ef25dac6`. The prior accepted build is preserved by the promotion receipt. No diagnostic-only candidate was promoted on logical assertions alone.

VENDOR ASSETS AUDITED: installed Weather Maker sky/cloud shaders, public profile/RenderQueue APIs and UserManual Skybox alternative; Unity6000.4.10f1/URP17.4 camera rendering; existing Heat/TMP viewport panels and NWH camera owners.

VENDOR ASSETS USED: Weather Maker `WeatherMakerSkyBoxShaderInclude.cginc`/`ComputeSkySphereColorFullScreen`, `WeatherMakerFullScreenCloudsScript.RenderQueue`, existing profile/precipitation bindings; Unity Skybox coverage; existing owned Heat/TMP panel layout. Original NWH handling remains intact.

RELEVANT ASSETS NOT USED: the generic built-in procedural skybox would discard Weather Maker's profile/night calculations; it was not substituted. No new Cinemachine, physics, weather, navigation or replay framework was needed.

CUSTOM SYSTEMS CREATED: thin skybox geometry shader calling the installed vendor implementation; explicit owned editor configuration helpers and dev-only targeted diagnostics/regressions. No new sky/weather simulation.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
