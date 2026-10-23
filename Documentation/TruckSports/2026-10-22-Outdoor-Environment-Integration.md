# Outdoor environment and time integration — October22,2026

Q22-SKY remains **PARTIAL**. This milestone extends the shared weather/time controls from5 to35 compatible saved outdoor scenes, with actual vendor skies, stars, cloud lighting and night headlights. Legacy/no-weather venues, remaining venue-specific illumination, scoring celebrations and results remain open. Q22-CARNIVAL stays queued after Q22-SKY.

## Implemented

All35 scenes have independent Weather and Time of Day Heat selectors. Random/Morning/Day/Evening/Night persist across sports and application restarts; match time stays fixed across attempts and heats. Explicit versioned authoring preserves subsequent designer changes and never runs during ordinary builds or Play Mode. Original rigs, participant assignments, physics and rules remain their existing authorities.

The installed physically based sky profile had no star texture and zero night intensity. One owned sky profile reuses the installed Milky Way texture with restrained twinkle. The existing viewport sky shader still calls Weather Maker's atmosphere code. The vendor sphere skips orthographic camera uploads; SportsOrthographicSky uses its public profile API for allowed arena lenses and unregisters on disable. Perspective lenses retain the vendor callback.

The default cloud ambient gain was1 at every time. An inactive sun also returns the daylight-end gradient lookup. The match coordinator now scales a runtime copy of the authored cloud ambient gradient for the selected stable time, restoring its original reference on cleanup. Weather profiles and vendor assets remain unchanged. Clear, overcast, rain, storm, fog, snow, sleet and other registered states keep their own cloud/fog/precipitation/wind profiles.

Night headlights use NWH VehicleLight.SetState and its existing sources on complete tractors. Authored beams serve wheel-only arcade bodies. Existing NWH sources are retained. Newly added beams follow interpolated truck poses from a scene-owned presentation root, preserving the transform hierarchy required by motion replays. Hide & Seek retains its manual tactical light/engine owner. Only Keepaway/Field Goal have the previously authored four venue lamps each; this is not a claim that every venue's artistic night lighting is finished.

Setup corrections include separate team-option/team-count rows, Bog's course caption returned to its own button, removal of the redundant legacy weather control, and logo-safe headings beside the AI-camera selector. Original Bog authoring moved the caption by the button offset, producing a blank option and text at the screen edge. The old AI-camera heading shift could overlap a long heading with the real logo. Existing artwork is unchanged.

## Actual verification

Final native suite: **3 PlayMode tests passed**, exercising35 saved outdoor scenes at maximum rosters, repeated Clear Day/Night starts, real vendor render callbacks and shader state, night-headlight state, cloud ambient gain, callback disable/re-enable cleanup, stable match-time latching, source day/night preservation and menu geometry. The geometry checks use rendered glyphs, not padded text containers. They check selector accessibility, button separation, caption containment and title/logo separation. These short starts are not full gameplay matches.

Final Windows payload: **1,054 checks/0 errors** across7 sequential fresh processes. Every35-scene setup and Day/Night view was captured at720p/1080p. All33 installed weather presets were exercised at Night in Field Goal. The real Heat widget accepted software-gamepad selection and keyboard navigation. Two software-keyboard Sky Tow views shared one night weather world. A separate process verified preference persistence. No physical controllers/wheels, listening or human playtest acceptance is claimed.

There are298 actual backbuffer captures in the local run folders, indexed by SHA256 in all-rendered-captures.json. Tracked evidence includes every720p setup, representative1080p views and before/after defects. Frame data contains110 blocks of60 whole-player intervals at1080p; these are not isolated CPU/GPU profiles, long soaks or a separate four-player split-screen benchmark.

The earlier9c candidate passed1,054 logical checks but was rejected visually for bright headlights/clouds and setup issues. Its receipts/images remain. The intermediate754 payload separately passed278 cloud and81 setup/headlight checks; the final heading correction followed. During development, a real Limbo replay hierarchy regression was caught and fixed by relocating added lights outside the actor hierarchy. Failed fixtures that chose the wrong lens, used the host's restored menu-only Editor list, loaded an unrelated menu prefab diagnostic, or measured padded text containers are recorded separately from product defects. No errors were suppressed to obtain the final passes.

## Delivery and preservation

Build: Builds/TruckSports/TruckSports.exe,60 scenes/546 files. Full content SHA256: 40a10ec9fb986d26b66fbc54cb2f9485a3eab607cf071ceeaa1da66ac95945ca. Managed SHA256: 0005d17650bedaa251ba2e89135f9961e0abff9eb03448421fbb28c1182ed73d. Exact286 owned C# inputs are retained with the full build identity. The preceding accepted build is backed up by the promotion receipt.

All original scene document IDs, original Rigidbody/collider data and original world transform poses are retained in35 scene reviews. The925 unrelated pending files remain preserved. No Truck Taxi source, original vendor prefab, Plowing roster/mood, Poker venue or concert lighting was rewritten. Source changes are scoped to the time/sky/light/UI adapters, explicit authoring, probes, tests, owned profiles/materials and35 saved scenes.

The Time of Day control sits beside Weather in each listed setup. The shared star profile is Assets/TruckSports/Settings/Sports atmospheric sky.asset. Per-venue SportsMatchTime exposes the owned cloud ambient gain and authored venue lamps; SportsNightHeadlights exposes added beam offsets. Normal saves/builds do not regenerate these selections.

## Exact saved scenes

- Assets/TruckKeepaway/Scenes/TruckKeepaway.unity
- Assets/TruckKeepaway/Scenes/TruckKeepawayFirstPerson.unity
- Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity
- Assets/TruckMinefield/Scenes/TruckMinefield.unity
- Assets/TruckMattressRacing/Scenes/TruckMattressRacing.unity
- Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity
- Assets/TruckGiantSwing/Scenes/TruckGiantSwing.unity
- Assets/TruckBogSnorkelling/Scenes/TruckBogSnorkelling.unity
- Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity
- Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity
- Assets/TruckTugOfWar/Scenes/TruckTugOfWar.unity
- Assets/TruckKickball/Scenes/TruckKickball.unity
- Assets/TruckLimbo/Scenes/TruckLimbo.unity
- Assets/TruckSkyTow/Scenes/TruckSkyTow.unity
- Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity
- Assets/TruckFareFight/Scenes/TruckFareFight.unity
- Assets/TruckSnake/Scenes/TruckSnake.unity
- Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity
- Assets/TruckHotPotato/Scenes/TruckHotPotato.unity
- Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity
- Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity
- Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity
- Assets/TruckSeesaw/Scenes/TruckSeesaw.unity
- Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity
- Assets/TruckTetherball/Scenes/TruckTetherball.unity
- Assets/TruckJousting/Scenes/TruckJousting.unity
- Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity
- Assets/TruckVolleyball/Scenes/TruckVolleyball.unity
- Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity
- Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity
- Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity
- Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity
- Assets/TruckFrogger/Scenes/TruckFrogger.unity
- Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity
- Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity

## Remaining work

Q22-SKY still needs legacy Cornhole/Cheese/Base lifecycle integration and appropriate outdoor venues that currently have no shared weather world; indoor/show exceptions must retain their intended lighting. Finish scene-specific street/building/venue/lava illumination, reflection/exposure and night replay acceptance. Short Blizzard renders do not prove substantial turf snow coverage or tyre tracks. Complete confirmed stable-ID/deduplicated score events, non-blocking tiered celebrations and actual tied results/statistics without replacing specialized Poker/Bowling sequences. Complete remaining human/device/listening/full-match/split-screen/performance acceptance. None of these items is marked complete by this milestone.

TUG OF WAR: Brainstorm additional mechanics with the developer and ChatGPT. Current gameplay is too dependent on holding reverse. Only environment/setup presentation changed here; no mechanics changes were made.

## Vendor accounting

VENDOR ASSETS AUDITED: Weather Maker3D/2D celestial lookup, public sky/atmosphere/command-buffer/cloud APIs and installed sky textures/profiles; NWH LightsMananger, VehicleLight and LightSource; installed Heat/TMP and existing DOTween presentation; original saved venue/replay authoring.

VENDOR ASSETS USED: Weather Maker remains the sole weather, sky, sun/moon, clouds, fog and precipitation renderer. NWH owns complete-tractor light sources/emission. Heat/TMP and existing DOTween styling/navigation remain in use. Installed Milky Way artwork is referenced by an owned profile. Existing Toon Gas Station venue lamps remain in their two authored venues.

RELEVANT ASSETS NOT USED: A complete Weather Maker2D replacement is inappropriate for the3D worlds. Wheel-only arcade bodies lack a complete NWH VehicleController, so their authored Unity beams use the same match-time semantics without adding a second vehicle controller. Weatherade/terrain/navigation systems are retained; this milestone does not replace or complete their remaining accumulation/lighting work.

CUSTOM SYSTEMS CREATED: Narrow vendor sky-camera and match-time/light coordinators, replay-safe presentation followers, explicit saved-scene/Heat migrations and targeted validation tooling. No new weather simulation, vehicle physics or UI framework.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None.
