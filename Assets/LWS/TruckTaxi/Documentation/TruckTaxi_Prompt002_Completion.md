# Truck Taxi Prompt 002 Completion

Validation date: 2026-09-25. This report distinguishes automated runtime evidence
from manual driving and final character-content approval.

## 1. Baseline / Preflight

Preserved `TruckTaxi_DemoCity`, its city, tractor, NWH input/physics, UTS population,
LWS routing and existing ride/session semantics. See `TruckTaxi_Prompt002_Preflight.md`.
Extended only Truck Taxi gameplay, passenger authoring/runtime, tooling and tests.
No core Interstate source or vendor source was edited by this pass. The pre-existing
unrelated working-tree changes were not reverted or included in the taxi commits.

## 2. Chatterbox Commit

`deab20ccd0101c8d85f4b702aebe1de57cca6de0`

`truck-taxi: working external chatterbox passenger voice pipeline`

78 files; adapter, queue, voice UI, import/cache, tests, voice/dialogue assets and
documentation. Python 11/11, EditMode 36/36, PlayMode 3/3 at that checkpoint.
Excluded external Python/Chatterbox installs, Jobs/Logs, machine settings, temporary
fixture WAVs, incomplete factory/gameplay work and unrelated project changes.
No push.

## 3. Ride Offer

`TruckTaxiOfferMap` uses the existing Compass render texture, not a fake map.
YOU, PICKUP and DESTINATION have labels/shapes and route geometry. The existing
LWS graph supplies `TruckTaxiRouteDistanceService`; straight-line is a labelled
fallback. Trip distance is pickup-to-destination. The immutable offer is the exact
passenger/pickup/destination accepted; tests assert identity and camera framing.
Coincident/nearby labels separate into vertical lanes while small colored pins
remain at exact coordinates. Center/corner collision cases have dedicated tests;
the final Windows offer capture was reviewed after this correction.

## 4. HUD

Primary speed/state/earnings/ride-count telemetry moved from upper windshield to
bottom; passenger information remains on the side. Existing Heat/TMP Canvas is
retained. Subtitles are below the physical GPS and above bottom telemetry.
Captures cover 1920x1080, 2560x1440 and 3440x1440 offer layouts. These are render
captures reviewed for layout, not human usability sign-off at every resolution.

## 5. Dashboard GPS

`TruckTaxiGPSAdapter` fits the existing live Compass world Canvas to the tractor
`interior` physical screen. Actual rect 640x340, lossy pixel scale 0.00023,
physical dimensions 0.1472x0.0782m. The redundant floating HUD Canvas is disabled;
its existing map camera still supplies the offer preview. No extra navigator.
Pickup/destination routes, exact offer, cockpit display and nonzero bounds are
covered in PlayMode. Destination changes use the same existing route request.

## 6. Arcade Handling

Reversible taxi-only `TruckTaxiVehicleHandlingOverride`: 65-degree low-speed lock
below 10 MPH, 40 at 20, 20 at 35, 8 at 70+; 300 degrees/second response and
0.045-0.15s smoothing. No yaw torque, traction rewrite, COM or upright lock.
Final controlled NWH keyboard test measured approximately 9.52m U-turn width,
175 degrees, 7.1 MPH peak, upright dot 1.00.
Settings restore on disable; no normal Interstate prefab/handling changes.
This is not exhaustive manual highway/rollover validation.

## 7. Unity Owned-Asset Search

Unity CLI/package skills audited first. FULL MY ASSETS LIBRARY NOT
PROGRAMMATICALLY ACCESSIBLE. No credential harvesting or purchases attempted.
Read-only local Asset Store archive metadata audit cataloged 53 downloaded
packages in `Tools/UnityAssetCatalog/owned_assets.json`; not claimed as the entire
owned library. See `TruckTaxi_OwnedAssetCandidates.md` for candidates and rationale.
Selected existing Compass/UTS/Heat/Pixel Crushers assets; a small radius-driven
ring was preferable to importing a broad unverified VFX/shader pack.

## 8. Pickup Presentation

`TruckTaxiRideLocation.detectionRadius`, default 11m, drives both gameplay and
`TruckTaxiPickupZoneVisualizer`. Ground ring, translucent area, beacon and label;
yellow approach, green inside, red too fast, pulsing boarding. Separate passenger
spawn and tractor stop points. Target-change-only ground queries. Session events
clean up immediately after boarding, cancellation, failure or the next ride.
Tests cover radius boundaries, excessive speed, leaving early, cancellation and
successive rides; arbitrary real driving approaches remain manual coverage.

## 9. Passenger Factory

Open **Truck Taxi > Passenger Factory**. UITK window extends the existing profile,
voice and dialogue path. Casting, appearance, animation, seat, voice, dialogue,
pair/companion and modifier data; search/filter; stable manifests; prefab building;
registration; validation and resumable eight-stage batch. Invalid passengers do
not terminate the batch. See `TruckTaxi_PassengerFactory.md` for exact workflow.

## 10. Model Pipeline

Manual Drop Folder, explicit Placeholder and Configured External Generator paths.
No AI service installed or claimed. Installed Blender 3.5.1 processing fixture
passed normalization/ground, material/LOD export and source-immutability checks.
Invalid replacements preserve the previous usable model/prefab. Cache signatures
include processing settings. Shared UTS male/female project-owned Humanoid copies
have valid avatars and eight installed clips mapped to 22 semantic states.
Humans use bone ragdolls; special fallback models use simple animation and a body.
85 runtime prefabs exist; all still require final casting-matched character art.
Texture atlasing, novel rigs and final animation performances require artist review.

## 11. Voice Pipeline

Existing external WorkHere installation reused; no clone/reinstall. Nine Al Lee
neutral audition clips generated/imported, covering greeting, chatter, arrival,
collision, shortcut, speed, boarding, ejection and pair response. Pixel Crushers
BarkController/IBarkUI handles runtime delivery, optional audio and subtitles.
Cooldown, one-shot, priority, interruption and missing-audio tests pass.
84 profiles remain uncast; missing-reference/model reports enumerate the gaps.
Hash/cache tests pass. Source neutral recording SHA256 remains
`BFC0D3DBE1373BAFC44B0FEA18037B5EDAC9335B3C54EC963F94562275A7F4DD`.
No external generator, reference recording, Python or Blender is required by player.

## 12. Passenger Ejection

Hold F, gamepad View/Select or existing HUD EJECT PASSENGER for 1.2s.
One shared request path cancels objectives, plays reaction, applies one fare/rating/
chaos consequence, restores temporary mass and ejects the representation.
Body ignores tractor colliders, caps exit velocity, cleans after 12s; next ride
becomes available after 3s. Bone/body physics fallback, not a bespoke final stunt.

## 13. Character Roster

85 profiles: original 10 plus 75 authored roster entries, all with stable IDs,
profiles, prefabs and manifests. Explicit race/ethnicity/species is casting metadata,
separate from gameplay traits. Original runtime names; inspirations are development
metadata. 85 need final models, 84 need reference casting. Twelve reusable modifier
kinds and five pair combinations are authored: commentators, dragon/companion,
tinkerer/robot, quiet hero/sidekick, marsupial/scientist. Shared objectives with
separate representations/voices; bespoke pair performances remain content work.

## 14. Testing

Python adapter: 11/11. Blender fixture: PASS. Final EditMode: 52/52, no skips,
including every prefab, Humanoid/ragdoll and idempotency, real cached voice import.
Final PlayMode: 3/3 (46.13s), including three successive human/oversized/robot rides,
pickup VFX, ejection, actual NWH keyboard drive and physical collision fixtures.
Cockpit capture found the eject button touching the GPS; it was moved into the
passenger panel footer for the final build. Subsequent capture verifies the repair.
Evidence in `Builds/TruckTaxiDemo/Validation`. Rides use controlled teleport/kinematic
staging; steering/input and collision fixtures use actual NWH/PhysX. No claim of
human driving, physical wheel-device operation or exhaustive production QA.

## 15. Final Source Control Commit

Requested source commit: `truck-taxi: complete passenger factory and arcade gameplay systems`.
Hash: `95084dff8330afb5693fa366cd4c551e289b297f` (1,480 files, including generated
profiles/prefabs/metadata and intentional game WAVs). Scoped to
`Assets/LWS/TruckTaxi`, `Tools/TruckTaxiPassengerFactory` and `Tools/UnityAssetCatalog`.
Temporary pipeline-validation WAV/manifests excluded. Unrelated Dead Air, freight,
Compass import, materials and project-settings changes remain uncommitted.
No push.
The subsequent Windows-validation commit contains only the offer-label separation,
three layout regression cases, opt-in player test-harness fixes and this report.

## 16. Windows Build

Unity 6000.4.10f1, StandaloneWindows64, Development. Existing builder:
`LWS.TruckTaxi.Editor.TruckTaxiDemoBuilder.BuildWindows`.
Scene `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity`.
Output `Builds/TruckTaxiDemo/TruckTaxi.exe`.
Final result: SUCCEEDED, 328,843,526 bytes reported by BuildPipeline, zero errors.
The compile log contains 76 unique C# warnings: 54 deprecated APIs, 15 existing
vendor Scene Streamer type conflicts, seven unused fields. These were not hidden
or resolved by modifying vendors. Full log: `Validation/Prompt002WindowsBuild.log`.
Player runtime DLL SHA256:
`00FDEB0623FD5A36D7F673E3C4589BA01E6D168C495A02DF8A0E54E27427A76A`.
No Truck Taxi Editor assembly is present in the player Managed directory.

## 17. Actual EXE Test

Opt-in `-truck-taxi-smoke` harness exercises actual Windows player startup,
normal keyboard/NWH motion, offer, pickup route/VFX, generated greeting/subtitle,
boarding, destination route, cockpit capture, fare completion, next ride and
hold-to-eject. Complete ride is teleport-assisted, not claimed as manual driving.
Final execution: PASS, actual Windows process exit code 0. Keyboard E/W moved the
existing NWH tractor 22.9m. One complete assisted ride, authored request, fare,
second ride, generated greeting playback, subtitles, hold-ejection, external body
and post-ejection Available state passed. Ambient traffic/pedestrian population
was present; movement was separately asserted in PlayMode.

Reviewed real-player camera captures: Windows_Offer (actual roads, distinct labels,
route and both distances), Windows_Cockpit (live physical map, subtitle, no HUD
overlap), Windows_Ejection (external passenger body). All live under
`Builds/TruckTaxiDemo/Validation`; Windows_Fare and other states are also captured.
GPS logged 640x340, actual lossy scale (0.0002300,0.0002300,0.0002300),
0.1472x0.0782m, destination target set and route ready.

First hidden-window attempt completed ride/ejection but failed synthetic input and
returned black swap-chain captures. The opt-in harness now clones input settings
with IgnoreFocus and renders the actual gameplay camera/UI into a capture target.
Normal launches are unchanged. The rebuilt player passed twice, including the final
label fix. This is automated standalone validation, not human-driven playtesting.
No Error/Exception/Assert was reported in the final smoke transaction. The bark-only
Pixel Crushers controller warns that no full conversation database is assigned;
runtime bark audio/subtitles nevertheless passed. No conversation system is claimed.

## 18. Known Limitations

- Final character models/casting/performances are content gaps; fallbacks are explicit.
  The dashboard robot is partly off the right cockpit edge; final model/seat fitting
  remains necessary rather than claiming a polished companion presentation.
- Full online My Assets library was inaccessible; local cache is not ownership proof.
- No automatic creation/skinning of novel humanoid skeletons or texture atlases.
- Existing black mirror presentation is outside this taxi passenger pass.
- No standalone external wheel/controller hardware or long highway stress test claimed.
- Development smoke harness is opt-in only; ordinary launches do not auto-drive/eject.
- Existing unrelated working-tree vendor updates were used by the current build but
  are not attributed to or committed as this work.

## 19. Next Recommended Development Step

Review the playable build, then cast/replace a small representative final passenger
set (human, oversized, companion) before expanding all 85 characters. Use the same
factory/model/voice paths; no runtime rewrite is needed.

## Vendor-First Report

- VENDOR ASSETS AUDITED: NWH, Compass, UTS, Heat/TMP, Pixel Crushers, DOTween,
  local VFX/hologram/Character Customizer candidates, external Chatterbox.
- VENDOR ASSETS USED: NWH, Compass/LWS navigation, UTS models/animations/population,
  Heat/TMP, Pixel Crushers bark/CSV APIs, existing external Chatterbox; Blender tooling.
- RELEVANT ASSETS NOT USED: broad VFX/hologram packs and Character Customizer not
  imported; DOTween unnecessary for a small state-driven pickup indicator.
- CUSTOM SYSTEMS CREATED: taxi-specific authoring/manifest/model adapters,
  passenger semantics/runtime/modifiers/ejection, pickup-radius presentation, tests.
- VENDOR SOURCE MODIFIED: NO by this pass. Existing unrelated changes preserved.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
