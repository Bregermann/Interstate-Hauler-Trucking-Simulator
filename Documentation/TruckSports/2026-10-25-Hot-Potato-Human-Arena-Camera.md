# Hot Potato human-priority Arena camera — source handoff

Authority: Q22-CARNIVAL-REQ-21.3, exact request §21.3 in `Requests/2026-10-22-Carnival-of-Chaos.md`. Source implemented; exclusive native validation and Windows/human acceptance pending. The already approved shared Arena HUD is untouched.

## Evidence and correction

Original `HotPotatoMatch.Present` starts a100×100 bounds and includes every surviving AI. Its local Pose/Fit are bypassed while `SportsBroadcastFraming.OwnsShot` is true. The shared framing describes the carrier as primary, includes nearby subjects and imposes readability distance limits. Consequently the local bounds never guarantee distant human visibility.

The Hot-only `HumanArenaFraming` predicate requires a running, unfinished original shared Arena view, no recorded review, and at least one surviving configured non-AI participant. Only this predicate opts out through the existing `BroadcastFramingApplicable` virtual property. AI-only, original chase/cab and replay keep their prior paths. No shared source/profile is changed.

`HotPotatoHumanArenaCamera` is a bounds policy invoked by the existing Present coordinator, with no Unity update callback or independent camera authority. It requires actual current renderer bounds for every active human, the carrier and visible bomb. Existing `SportsBroadcastFraming.TruckBounds` supplies the appearance-aware truck bounds. Bomb placement/rotation occurs before fitting so a new carrier does not fit the previous carrier's bomb position. Eliminated humans drop from the mandatory group.

Nearby AI may be included within16units of the required bounds, only while the accumulated optional bounds stay within115% of the required dimensions plus4units. They cannot form a chain to distant AI. A3unit full bounds margin protects model edges. Center/size shrink exponentially at3/s; current target bounds are always encapsulated immediately to protect required subjects on the first expanded frame. Acquisition snaps the existing Cinemachine evaluation once; ordinary updates retain the vendor transposer. Existing `SportsCameraFit` handles the original orthographic projection and HUD insets. Its current explicit Apply caches the orientation/aspect, preventing a second LateUpdate fit for the same state.

The existing `SportsEventCamera.Pose` releases the shared group/occlusion ownership before evaluating. The shared framing's LateUpdate sees its predicate false and cannot also call PoseRaw. The mode policy holds acquired framing on pause and resets on split/review, stop and new match.

## Exact source and validation scope

Changed existing file: `Assets/TruckHotPotato/Scripts/HotPotatoMatch.cs` (presentation properties, Begin/Stop reset, Present only). Hot rules, input, AI, contacts, physics, scores, awards, journal, HUD and shared camera/session sources are unchanged.

New source: `Assets/TruckHotPotato/Scripts/HotPotatoHumanArenaCamera.cs` and its `.meta`.

New native fixture: `Assets/TruckSports/Tests/GoddessPlayMode/HotPotatoHumanArenaCameraPlayModeTests.cs` and its `.meta`. Filter `HotPotatoHumanArenaCameraPlayModeTests` has four cases:

- `OneToFourAssignedHumansAndCarrierHaveAllRendererCornersAt720And1080`: four genuinely distinct configured neutral test controllers through original source validation, original eight physical NWH actors dispersed inside the actual64m arena. All eight required world renderer-bounds corners, depth and image contents are checked at1280×720/1920×1080. Eight native camera RenderTexture PNGs go to a fresh timestamp directory under `Logs/TruckSports/PlaytestRepair/HotHumanArenaNative`. This is not Windows ScreenCapture or physical human/device acceptance.
- `DistantAiCannotEnlargeMandatoryHumanShotAndMovingHumanExpandsImmediately`: actual original actors, adjacent optional AI versus distant AI, and same-acquisition first-frame safety when an already assigned human moves across the arena.
- `OriginalEliminationDropsHumanSmoothlyPauseHoldsAndRematchReacquires`: deterministic seed selection through original Rules.Reset; original fuse Step eliminates the distant carrier. No alive/carrier/score property is injected. Shrink is gradual, surviving required subjects remain visible, presentation reads add no score receipts, pause holds the camera, and a new match restores participation.
- `SharedOwnershipReleasesForOriginalSplitCabAiOnlyAndRecordedReview`: original view selectors, original recorder Capture/Begin/End, rule-authoritative finish, new match, real global AI-only camera acquisition, then human reacquisition after the actual Cinemachine group composer exists. Checks original owner camera counts, projection mode, released occlusion and LookAt target, and at most one broadcast Cinemachine evaluation per frame.

These are explicitly controlled actor/camera regression fixtures. Match Update is disabled and physics held while the test invokes the original presentation flow with a stated frame delta. Controlled original recorder samples and fuse steps test presentation lifecycle; they do not establish natural match outcomes. No fixture yields WaitForEndOfFrame. Render targets/aspect, input settings/devices, isolated preferences and match state are restored in finally/Dispose paths.

Static semantic check:533 existing cached assembly references, zero C# errors for the changed Hot sources and new fixture. This does not substitute for Unity compilation, native execution, rendered-frame inspection or Windows acceptance. No Editor/player/CLEAN/Git/scene action was performed by this handoff.

## Vendor accounting

- VENDOR ASSETS AUDITED: installed Cinemachine GroupComposer/Transposer and existing SportsEventCamera, SportsBroadcastFraming, SportsCameraFit, SportsDrivingViews and SportsRecordedReview integration; original NWH actor/attached-body reset foundation.
- VENDOR ASSETS USED: existing Cinemachine rig, damping and evaluation; original NWH physical actors and existing renderer-bound/projection adapters.
- RELEVANT ASSETS NOT USED: DOTween is unnecessary for a per-frame subject-bounds policy; Cinemachine already supplies camera evaluation/damping. No new camera or animation framework is needed.
- CUSTOM SYSTEMS CREATED: one Hot-owned mandatory/optional subject-bounds policy and focused regression fixture.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none; LWS supplies mode-specific subject priorities, preserving existing camera evaluation and projection authorities.
