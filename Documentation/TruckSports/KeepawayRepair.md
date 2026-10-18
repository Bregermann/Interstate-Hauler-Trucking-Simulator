# Keepaway presentation repair — October 18

Status: accepted implementation/native/Windows milestone. This is one completed repair in the outstanding release queue, not a finished release of every queued feature.

## Actual findings

- Both saved scenes already use the enlarged 64 × 36 m rounded playable arena, with connected navigation and owned modeled ramps, crates, barrels, tires and sweepers. Enlarging it again would make the trucks smaller. Physical dimensions, traction, input, AI and possession rules are preserved.
- The previous camera fit reserved the top 12% for a header and top score cards, plus a bottom status strip. The baseline projected playing surface occupied approximately 83% of width and 70% of height in the native 16:9 view.
- Broadcast cards found the first standings line containing the displayed name. Two identically named drivers with authoritative totals 3.2 and 17.4 both displayed 3.2. Keepaway now supplies typed per-slot card values directly; names are presentation only.
- Sheep already use the actual SheepV5 rig, locomotion and native terrain raycasts. The baseline sampled 125 actual animated foot positions, −0.0022 to +0.1901 m from the contoured ground; seven sheep moved and eight animated. No speculative sheep physics rewrite was justified. Measurement uses `BakeMesh(..., true)` to account correctly for the imported skinned hierarchy scale.

## Changes

Compact rounded participant cards sit at the bottom, with identifying color, possession seconds, target, carrier/recovery/protection status and progress. The obsolete header and full-width status text/backplate are removed. Camera fitting reserves the card area, fits the existing rounded perimeter and expands for actual live trucks/ball during exceptional motion. Long names retain ellipsis rather than pushing adjacent scores.

First Person uses the installed Compass Navigator Pro POI system. A read-only proxy follows the single physical ball, whether loose, carried or airborne. Four existing vendor compass groups correspond to camera views. Only human-owned cab views activate guidance; Arena and AI-only broadcast remain unchanged. The fixed bottom-right gold ball/arrow uses the actual rendered camera, follows free look, fades over 0.18 seconds using DOTween, turns smoothly and displays feet/miles. Onscreen balls hide the pointer. Pause, invalid references, rematch and scene exit do not change input or ball authority.

The project-owned CNP subclass corrects the vendor overlay's second split-rectangle remap: Unity `WorldToViewportPoint` already returns coordinates local to the camera viewport. It retains vendor registration, state, projection, visibility and indicator creation. No vendor source is edited. The adapter initializes the vendor instance before calling settings setters that expect initialized internals.

The additive `KeepawayHudAuthoring.UpgradeBoth` installs the owned CNP-derived prefab into both existing scenes and refuses unsaved scenes. It does not rebuild the environment. Explicit environment/repair authoring defaults call the same installer; ordinary Windows builds do not regenerate scenes or overwrite hand edits.

## Validation record

Baseline: the previously accepted Windows build passed `-tsKeepawayRegression`: actual NWH/PhysX AI collection/theft, bounded wall recovery, complete normal-target AI matches, four cab steering/nameplates, pause and menu/rematch. Diagnostic baseline captures and measurements are under `Logs/TruckSports/Keepaway/Native/baseline`.

Replacement acceptance: **16 native saved-scene checks, 95 final-source Play Mode checks, and all four exact-build Windows suites pass**. The player presentation suite passes **117 checks** with actual screenshots at 1280×720, 1920×1080, 1440×900, 1024×768 and 2560×1080. The rounded physical boundary occupies about 96% of 16:9 width and remains visible; the cards are quantitatively below its near edge. Ultrawide uses the largest safe height fit rather than stretching or cropping the arena.

The physical regression verifies human/AI mixtures, intent-to-NWH-motor movement, an actual solid-wall recovery (1.12-second reverse, 2.43 m movement), real pickups/thefts/explosions and respawns, two full 60-second-target AI matches, pause preservation, four actual Taxi cab wheels/nameplate bounds, menu return and rematch. Full-match totals: **37.38/60/59.94/17.94** and **23.06/60/17.54/44.84**. These are actual NWH/PhysX outcomes at accelerated test time; no score injection or human playtest is claimed.

Shared regressions also pass: **2,467 checks across all 20 applicable camera modes** and **769 checks across all 34 sport scenes** for persistent identities/menu layouts. No runtime exceptions were recorded. Final Windows animated sheep sampling found five moving/six animated, 123 valid ground samples, −0.0022 to +0.1906 m clearance. Existing sheep tuning was retained.

Actual Windows captures exposed and fixed a doubled vertical card anchor that the native preview did not identify. The earlier candidate also used an invalid ultrawide test assumption: a sideways fixture point was still inside the wider camera frustum. The corrected fixture places it fully offscreen. The native free-look fixture samples CNP's final late-frame projection rather than a camera already reset for the next frame. Rejected runs are retained under ignored Logs and are not counted as passes.

Final Windows x64 Development: `Builds/TruckSports/TruckSports.exe`, all companions, 35 explicit Sports-only scenes and TruckSports startup. Build 2026-10-18 13:22:15 UTC: **zero errors / 31 warnings**, 1,247,905,541 bytes, 35.42 seconds. Managed SHA256 `d016cd49c427f4acef886e468ca78da8c206748e21a1fbfc003059adc1988f11`; full 437-file data manifest `40e99ec79496f7471941225e1482eba85a3162173d612ee1ab16874965379001`. The supported SportsBuilder.Build workflow restored all 25 host-setting file hashes; the deliberate failure-restoration fixture also passed. Neither scene changes physical body/collider blocks.

[Curated acceptance](KeepawayEvidence/acceptance.json) includes exact-build receipts, native results and actual player screenshots. The “before” image is explicitly a native controlled preview, not a Windows before/after benchmark. Full diagnostics and rejected runs remain under ignored `Logs/TruckSports/Keepaway`.

Remaining checks: human UX and physical controller/free-look/keyboard-rollover/audio/haptics; sustained four-human-cab performance. This presentation pass does not claim a sustained new performance benchmark. Other queued modes/features remain open in PartyMasterLedger/GoddessPlaytestLedger/FareFightLedger.

## Files and assets

- Saved scenes: `Assets/TruckKeepaway/Scenes/TruckKeepaway.unity`, `TruckKeepawayFirstPerson.unity`.
- Read-only presentation: `KeepawayBallGuidance`, `KeepawayBallCompass`; owned prefab `Assets/TruckKeepaway/Prefabs/Keepaway ball compass.prefab`.
- Existing `KeepawayMatch`, shared `SportsEventSession/Menus`, `SportsBroadcastCanvas/Scores` receive narrowly scoped presentation hooks.
- Existing `KeepawayRepairAuthoring` and `KeepawayPresentationAuthoring` retain regeneration defaults.

## Vendor accounting

- **VENDOR ASSETS AUDITED:** Compass Navigator Pro indicator/split-screen APIs and prefabs/demo, Heat/uGUI theme, DOTween; existing NWH, Cinemachine, Weather Maker, Weatherade and SheepV5 integration.
- **VENDOR ASSETS USED:** CNP POIs/groups/indicator prefabs and arrow sprite; DOTween fades/card feedback; existing Heat theme, Cinemachine safe framing, NWH physical trucks, Weather Maker/Weatherade environment and SheepV5 animation.
- **RELEVANT ASSETS NOT USED:** No road, traffic, save-world or new terrain generator is needed for this bounded arena presentation repair. Existing world/scene/physics authorities are retained.
- **CUSTOM SYSTEMS CREATED:** Keepaway-specific read-only CNP adapter/coordinator, additive native authoring and explicit development acceptance probe; per-slot presentation contract extends the existing HUD.
- **VENDOR SOURCE MODIFIED:** None.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** None; the adapter supplies sport-specific placement, split-coordinate correction and visual behavior over CNP.
