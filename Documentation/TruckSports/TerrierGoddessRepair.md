# Terrier Racing — Goddess repair evidence

## Implemented and saved

- The track retains its 68 m racing width through the last ordinary hurdle. A final funnel begins six metres beyond that hurdle (with bounded length safeguards). Floor mesh, collision mesh and six side boards use the same profile. Randomized hurdle/ramps, diagonal downhill orientation, twelve stalls and existing 25%-wider tunnel remain.
- Removed the staircase-based bridge towers. `TerrierVolcanoAuthoring.InstallSupports` no longer regenerates twenty repeated stair segments per pylon.
- Added installed Vista basalt albedo/normal detail and an original smooth, dimpled red cherry with curved stem. The cherry has no colliders and sits outside the winner's landing line.
- Added bounded angular-only upright damping and limited airborne yaw correction. Genuine strong obstacle/truck impacts temporarily suspend that assistance; large crash tumbles remain possible. No additional forward force, orientation-relative velocity clamp or velocity clearing was introduced.
- Existing oriented whole-body swept tunnel crossing, contact chronology, genuine solid-wall normal-impact policy and immutable winner lock are preserved.
- The scene's saved broadcast camera had HDR enabled but post-processing disabled; its renderer also lacked the Weather Maker render feature. A scene-owned renderer now reuses the existing Weather Maker integration and native URP grade. Host/shared render assets are untouched. The shared installed-profile weather selector is now bound in setup.
- Twelve broadcast scorecards now use two compact rows rather than covering the left side of the course.

## Actual validation

Baseline controlled actual NWH takeoffs: 8 checks / 0 errors. A normal timber ramp retained at least 17.72 m/s from 18.01 m/s, with maximum 8.86° tilt and 0.382 rad/s rotation. A dedicated grounded jump retained at least 17.75 m/s with 1.5° tilt. These straight fixtures did **not** reproduce the reported wild spinning, so no universal root-cause claim is made for that report.

After angular/funnel repairs: complete twelve-Hard-AI first-to-five match, **32 heats, 21 checks / 0 errors**. A natural winner reached five wins; real swept finishes, random layouts/weather, rematch score/lock reset, pause pose/clock preservation and collection return passed. The run used accelerated test presentation at 3×, then 6×, with the original 0.02 s physics timestep. This is native Editor NWH/PhysX evidence, not human, controller-hardware, Windows or GPU benchmarking.

Evidence: `Logs/TruckSports/EssentialRelease/Terrier/baseline/`, `Terrier/full/`, `Terrier/render-baseline.txt`. The first full-match screenshots exposed washed-out snow and the obstructive score column; they are retained as pre-rendering-fix evidence, not accepted final presentation.

## Pending acceptance

Rendered Clear/Blizzard and four-cab views at 720p,1080p,4:3 and ultrawide passed24/0; captures exposed a legacy through-track label and it was replaced with a depth-tested world TMP preset (final visual repeat pending). Expanded Unity PlayMode suite15/15 passed before that label assertion was added. Updated physical tunnel reproduction passes15 checks/0errors: centered, angled, real edge scrape, actual post-finish collision/delayed event, grounded guide redirection, genuine above-guide solid barrier slam at20.23m/s normal speed with physical fragments, and550m/s swept passage. First outdated grounded-wall fixture was rejected because the new guide produced a legitimate finish; retained underRejected/TerrierFinish-ground-guide-01. Winner-only dessert/debris native repeat passed13checks/0errors: true winner basin contact/squish once; nonwinner falls through ignored reward basin to actual lava;144 actual fragments from12trucks and their recorded nonphysical replay. Final world-label/aspect captures and updated assertions; updated combined Windows player and built-game checks; scoped publication. The wider backlog remains open.

## Main files

`Assets/TruckTerrierRacing/Scripts/TerrierCourse.cs`, `TerrierRacer.cs`, `TerrierMatch.cs`, `TerrierGoddessProbe.cs`, `TerrierFinishProbe.cs`; `Editor/TerrierGoddessAuthoring.cs`, `TerrierRouteAuthoring.cs`, `TerrierVolcanoAuthoring.cs`, `TerrierDessertAuthoring.cs`; saved `Scenes/TruckTerrierRacing.unity`; shared `SportsProductionAuthoring.cs`, `SportsBroadcastScores.cs`, and the Goddess PlayMode suite.

Vendor reuse: NWH WheelController, Weather Maker, Weatherade, Cinemachine, DOTween, Heat UI and installed Vista textures. Custom work is sport-specific track profile, angular limits, cherry geometry and acceptance fixtures. No vendor source edited.
