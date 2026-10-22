# Dock Diving venue and stable speech — October22 delivery

HPR-18 completes the existing Dock sport's crowd, pool, approach, water presentation and speech tracking. Original NWH driving, launch/timing, limited aftertouch, first-water scoring, recorded replay, tie-breaks, source ownership, audio and crew identity remain authoritative.

## Implemented

- 464 clothed spectators replace the visible 48 identical men. Eight actual owned UTS variants: business01_f_highpoly, casual03_f_highpoly, casual05_f_highpoly, casual07_f_highpoly, sportive02_f_highpoly, sportive05_f_highpoly, sportive03_m and sportive07_m. Six female/two male models retain their authored clothing, hair, rig and textures. Deterministic spacing/yaw/scale offsets and varied idle/dance phases avoid identical posing. Four render-only truck exhibits add novelty. Original spectator objects are retained inactive.
- 24 close spectators use actual compatible vendor animations. 440 distant spectators share 16 runtime-baked idle/applause meshes with instanced-capable owned materials; inactive pose-source rigs retire after the one-time bake. Existing `FieldGoalCrowd` handles applause/reset/pause; no duplicate scoring/NPC/navigation/ragdoll framework. Meshes are reused on rematch and destroyed on scene exit. No new raw vendor geometry asset is published.
- Owned Construction Props timber covers/clads the run-up while the original launch collider stays intact. MegaCity railings/fences, a completed far pool wall/coping, rear stands, enclosure, administration building and existing original Dock PNG logo finish the venue. The reused top-less wall mesh prevents overlapping coping faces. New physical structures stay outside the original launch/water envelopes.
- The prior pool had a static URP material and zero reflection probes, despite already having shared spray/ripple effects. Add the existing DOTween `FishingWaterFlow` adapter and a bounded 128px time-sliced reflection probe. Preserve the existing `SportsWaterInteraction` eight spray entries,48 ripple rings, sounds and replay events. Actual surface UV motion, pause and real physical entry were tested.
- The impact capture exposed the original splash renderer's empty BaseMap, opaque Surface0 and depth writing. Assign the existing shared textured River splash alpha material (transparent Surface1, no depth write) to that renderer, preserving every original particle simulation value. Decorative truck exhibits are grounded/sized from their actual renderer bounds. Saved reference tests enforce both corrections.
- `DockSlang` follows the displayed truck transform with a configurable .08s anchor filter after composition, reset for new owner/viewport/camera cuts and bounded margins that allow the existing scale animation. Original Canadian phrases, owner colors, fade cadence, one reaction per launch, pause and reset remain. Native real-flight raw caption jerk was5.060px RMS; filtered measurement0.010px RMS against a4.820px raw target. The preliminary camera-order mismatch hypothesis was not established; direct projection noise reaching the caption was measured.

## Actual validation

- Two EditMode tests passed: saved eight-variant/464-placement references, original launch bounds, actual water adapter/reflection/coping and repeat-authoring byte preservation.
- Two PlayMode tests passed: all eight vendor applause poses measurably deform real meshes;16 mesh instances reused,464 spectators, water UV motion/pause, genuine autonomous splash entry and caption jitter attenuation. No forced score or physics outcome.
- Exact Windows payload: 22/0 mixed-source venue checks and16/0 full AI checks, 38/0 total. A complete eight-participant match using keyboard, a whole pad and shared left/right sticks plus four AI produced 24 actual launches, valid scores for every human source, isolated inputs, preserved disconnect/reassignment, results, rematch and cleanup. The separate original probe completed the eight-AI three-attempt competition, checked bubble ownership/cadence/pause, positional cue/mute handling and menu cleanup.
- Actual composed images:720p,1080p,ultrawide and4:3 overviews; labelled close approach; real airborne bubble, impact spray/foam and mixed-match results; legacy AI flight images at720p/1080p/4:3. Overview/approach images are explicitly camera fixtures; gameplay and scored results are actual simulation.
- All 10672 original scene documents retained; 6898 owned additions. Original physical transforms/colliders and DockMatch/Contact/Replay/SplitVisual plus shared vehicle/session/water/crowd source bytes remain unchanged.925 unrelated pending files retain their SHA hashes. The original dock renderer and old crowd visibility are disabled, and the splash renderer references the existing shared alpha material; original source assets and particle simulation values are untouched.
- The first mixed Windows run completed its physical match and tie-break but failed on a test-harness event queued to its deliberately removed virtual pad. It was not promoted. The probe now checks device.added before neutral events; the fresh accepted run has zero errors. Its rejected receipt/images remain in local `DockFirstMixedVenueWindows-d3871a91`.
- A later diagnostic run used saturated virtual drivers that repeatedly tied until its watchdog; their actual input patterns were varied without changing the game's tie-break rules. A close approach image also exposed aspect-preserving fitting inside thin bounds: the owned art was too small. Explicit version3 corrects model extents/fence orientation, and native renderer-bounds checks now enforce the intended dimensions. These intermediate candidates were not promoted.

Human playtesting, physical hardware, audio listening and isolated CPU/GPU benchmarking were not performed. These automated results are not human acceptance. Dock remains the existing turn-based sport; no new simultaneous split-screen mode is claimed.

## Delivery and authoring

`Builds/TruckSports/TruckSports.exe`,60 scenes and all companion files. Full545-file SHA256 `5b12d4c0bab3bd7f2fe17a68e80304078fd1dfcefc257dc4bade9f4a8888e727`; managed SHA256 `f8654bd4e41dc1ac7da9dab5dc5b245542e930a7b2ea3449380fe2849972ff6d`. Prior accepted Cheese build is retained in Recovery. Exact235 scoped source inputs are archived locally with this build.

Edit the saved **Finished Dock competition venue · owned artwork** hierarchy, the **Owned audience** placement anchors/24 actors, `DockAudiencePresentation.variants`, and `Assets/TruckDockDiving/Presentation` material/controller assets. Distant meshes are intentionally generated from the actual owned rigs once in Play; placement/source references stay serialized and editable. **Truck Sports → Dock Diving → Finish saved pool and audience** is an explicit one-time tool that preserves versioned saved customizations on repeat. Normal builds do not rebuild the venue. `DockSlang.trackingTime` is Inspector-editable.

## Vendor accounting

VENDOR ASSETS AUDITED: Existing UTS clothed female/male prefabs, exact generic Bip01 rigs and original controller clips; Construction Props timber, MegaCity fences/building, installed Weather Maker normals and existing shared water/reflection/URP integration, Heat/TMP/DOTween, NWH, existing FieldGoalCrowd and SportsAudienceAuthoring.

VENDOR ASSETS USED: Eight owned UTS clothed models with their compatible idle/dance/applause clips and textures; Construction Props and MegaCity art; existing original logo, Heat/TMP/uGUI, DOTween water/fades, shared water spray/ripple/audio/replay, Unity ReflectionProbe/BakeMesh and original NWH driving.

RELEVANT ASSETS NOT USED: UTS traffic brains/ragdoll/nav components are unnecessary for protected render-only spectators. FieldGoal run-on antics are not introduced into Dock scoring. No replacement water/vehicle/weather/input engine or new logo is needed; the existing URP-compatible normal/flow and shared interaction systems are reused.

CUSTOM SYSTEMS CREATED: One bounded runtime pose-bake/binding adapter over existing crowd reactions, explicit saved venue authoring, focused bubble anchor filter and targeted diagnostics/tests. Pool/enclosure semantics remain LWS-owned.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
