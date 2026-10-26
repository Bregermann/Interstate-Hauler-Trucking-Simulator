# Existing show and quarry lighting — Q22-SKY-LIGHT-SHOW-01

The saved Ninja Warrior show lights and Trailer Plug quarry lights were active but too weak to illuminate the gameplay. A controlled comparison measured zero meaningful changed pixels at their original powers. A temporary power-only trial remained visually incomplete: Ninja beams aimed below its elevated decks, and ground-level quarry lights could not illuminate the suspended rig.

The explicit, one-time `SportsEnvironmentAuthoring.CompleteExistingShowVenueLighting` migration re-aims the original24 colored Ninja fixtures at the actual12m decks, adjusts their cone/range/power, and retains their orange/cyan show colors. It retunes the six original quarry lights and mounts one upper fixture on a vertex of the existing modeled crane. That fixture reuses the installed Pixel Crushers lamp housing and material plus the established shared Sports LED mesh/material. No collider or physics component is added. Show lighting remains independent of ordinary night-only lamp switching; the original12-power green sinkhole light remains unchanged.

Both mode-local Forward+ renderer copies retain the original Weather Maker renderer feature and existing Weatherade depth renderer. The original source renderer assets remain unchanged. The per-object light budget increases from4 to8. No vendor source, gameplay camera, scoring, vehicle, navigation or original physics configuration changes.

Strict native scene review retains all15,911 original serialized records. Only30 original light photometry records,24 Ninja lamp rotations and one quarry start-fill rotation, one child reference per scene and17 render/light fixture records are authorized changes. Unrelated native null/default additions were excluded; raw native saves are retained. The method does not run on builds, scene load or Play Mode and refuses to overwrite independently customized initial fixtures; its authored root prevents repeated migration from replacing subsequent designer edits.

## Actual verification

- Initial controlled diagnosis: Editor26120 exited0/147.48s;2 diagnostic executions completed/36.3354844s. Original show lamps produced0 meaningful lit pixels; power-only trials produced1346/6397 pixels but remained visually incomplete. Four actual before/trial720p images inspected. These executions were diagnostic, not acceptance.
- Authoring: Editor36000 exited0/47.42s; only the two isolated saved scenes and local rendering assets were changed, then strictly reviewed/exported.
- First candidate was rejected after four Windows Clear Night frames showed an unlit initial trailer face and excessive crane glare. The two nominal upper mounts also selected the same crane vertex. The explicit `CorrectReviewedQuarryUpperFixture` repair removes the render-only duplicate, reduces the surviving upper lamp to700, and aims one original right-side quarry light at the suspended starting trailer with power18000. No physics/camera/scoring edits. Correction Editor43684 exited0/34.8s. Original failed first-candidate images and receipts remain retained.
- Final native integration: Editor43984 exited0/81.5s;2PlayMode tests passed/46.1327721s. Actual initial trailer-face region has169/169 positively affected pixels before GO; gameplay comparisons show47,934 Ninja and13,039 quarry lit pixels out of57,600. Original clock/pause/rematch identities,7 total quarry event fixtures and renderer contracts pass. All10 native720p/1080p images inspected. Quarry snow retains a localized bright hole-area patch; full-course exposure acceptance is open.
- Existing time/weather preference contracts: Editor38728 exited0/34.73s;3EditMode passed/.1228961s.
- Windows x64 Editor44420 exited0/306.82s; Unity summary279.9922270s,0errors/46warnings,60scenes,548files,437 frozen owned C# inputs. Full content a59c4c8a9975748c3b0986703429748238087af50d51a0b9a84c8d28e4156495, managed c49d7d11f8a422dd303d7b6ac560fbd0b7e0765f3e90938e75065b95c9d9609b.
- Final show Windows45160:157 scripted checks/0errors, then native exit3221225477 after cleanup. All36 actual720p/1080p frames reviewed through labelled overview sheets; initial quarry Clear Night also inspected as full source images. This is NOT a clean process pass.
- Final snow Windows45360:117 scripted checks/0errors, then native exit3221225477. All24 actual720p/1080p snow-startup frames reviewed through labelled overview sheets. Preceding8734 candidate43436 had117/0 and exit0, with all24 original frames inspected separately. Native shutdown remains blocked; no failed process is waived. Windows event records for45160/45360 show UnityPlayer+0x1d41c9 / c0000005, matching the previously symbolicated accessibility shutdown fault.

These are targeted presentation/integration checks, not human driving, physical controllers, listening, full-course visual traversal, complete competitive matches or isolated CPU/GPU profiling. ParentQ22 remains unfinished, followed by Carnival0.6 and Impossible0.7. No public publication or canonical build promotion is included in this checkpoint.

## Snow diagnostic interpretation

Seven read-only Snake road samples show that distant roads can accumulate snow outside the original800m depth volume. A local batch-rendered sample looked bare, so a second diagnostic used the original gameplay camera. Editor37664 exited0/75.63s; one diagnostic execution completed/33.5376301s. Four images inspected. Calling `UpdateDepth` and immediately comparing images did not establish a successful refresh: the installed vendor implementation waits for `WaitForEndOfFrame` before rendering/packing depth. Unity documents that this yield does not run in Editor batch mode. This supports a harness limitation, not a demonstrated production defect. [Unity WaitForEndOfFrame documentation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/WaitForEndOfFrame.html).

The development-only existing `SportsEnvironmentProbe` gains an explicit `snow-startup` case list to inspect the real Windows frame loop: direct Snow Day, Heavy Snow Day, Blizzard Night, Clear Day, Snow Day rematch and Snow Morning. It uses the original weather picker, match lifecycle, physics, vendor depth and GPU trace diagnostics. Distinct capture names preserve both Day starts. No production snow behavior was changed to compensate for batch-only images. Full-world, multiple widely separated human viewports and sheltered-surface acceptance remain separate unless actual evidence establishes them.

Candidate: `Builds/TruckSportsCamera/TruckSports.exe`, complete companion files retained. Previous25146954 road-snow candidate and preceding8734 first show candidate both preserved before replacement. Accepted `Builds/TruckSports/TruckSports.exe` remains60d4716f. Current6000.4.10f1 Dock native shutdown blocker remains separately recorded; no Editor upgrade or OS workaround.

## VENDOR ASSETS AUDITED

Existing Ninja/Trailer show-light configuration; URP renderer features/light budgets; Weather Maker; Weatherade CoverageBase depth refresh and SRS traces; installed Pixel Crushers model/material and original shared mounted-fixture integration. Existing NWH and Bungee crane presentation retained.

## VENDOR ASSETS USED

Weather Maker atmospheric/time authority; Weatherade original snow/depth/trace renderer; URP lighting. Housing: `Assets/Plugins/Pixel Crushers/Dialogue System/Demo/Art/Scifi Environment/Models/WallLamp.FBX`, original `Materials/WallLamp-16 - Default.mat`; existing `Assets/TruckSports/Models/Original mounted venue LED panel.asset` and shared material. Existing Sports lattice crane supplies actual mount geometry.

## RELEVANT ASSETS NOT USED

No new pole set: the saved show towers and quarry crane already supply the supports. No replacement weather, depth, snow, vehicle or UI system was needed. Existing UI remains unchanged.

## CUSTOM SYSTEMS CREATED

No new runtime system. An explicit migration extends existing authoring; focused read-only diagnostics/integration tests and a development-only existing acceptance-probe option were added.

## VENDOR SOURCE MODIFIED

None. Original Truck Taxi source and original renderer sources also remain unchanged.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None. Existing vendor and adapter responsibilities retained.
