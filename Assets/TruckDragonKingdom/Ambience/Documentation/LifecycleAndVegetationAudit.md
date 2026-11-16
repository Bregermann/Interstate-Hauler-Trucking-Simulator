# Ambience lifecycle and vegetation source audit

2026-10-28. Read-only inspection of the actual installed forest source and existing shared authoring, plus narrow fixes within the owned ambience runtime. No Unity/CLEAN execution, world/ground/road edit or vendor source modification occurred. Source inspection is not rendered or audible verification.

Two concrete lifecycle issues were corrected:

1. A source entering Weather Maker fade-out already had its logical `playing` flag cleared. The old pause loop checked that logical flag and could leave its DSP playback running during pause. Pause now records and pauses every actually playing source, including fade-outs, and resumes only sources that this lifecycle paused. Stop/new-match reset clears that bookkeeping. The existing Weather Maker fade wrapper remains the fade authority.
2. Existing `CampLivingArt.OnDisable()` restores bind pose. Toggling it for pause/culling therefore snapped the current pose instead of freezing articulation. The wrapper now caches the real current local bone/model pose around disabling it, then restores that pose. Buffers are retained and reused; no replacement gait, authored source curve or vendor animation code was introduced.

Standalone source and fixture compilation passes after these corrections. Actual native pause/fade/cull/resume tests remain unrun. The supplied native fixture samples current positions, local bone rotations and audio sample clocks after the ordinary pause boundary, then verifies same-population rematch reset. It is an early lifecycle test, not complete match/performance/listening acceptance.

Other inspected authorities remain intact: one existing Sports listener and SFX setting; no added mixer, listener, score or gameplay collision; eight bounded saved NavMesh datasets loaded once and removed by the ambience owner; paused/culling agents and bird tweens retain their current play position; ordinary world reset restores the same population only at new-match boundaries. Bird loop endpoints match; existing DOTween interpolation owns traversal. Existing Sports feedback does not expose a dedicated authored SFX mixer group; ambience follows its established AudioSource/shared volume convention rather than creating a competing mix graph.

`SportsSettings.ApplyAudio` already owns master gain through `AudioListener.volume`; the ambience wrapper applies the shared SFX gain once through the existing fade wrapper. No second master gain or mixer authority was added. This verifies routing intent from source, not loudness or audible quality.

Explicit ground/air/NavMesh installation failures now write bounded JSON witnesses below `-tsDragonOutput/AmbienceClearance` (or the existing project Logs fallback). Reports retain original candidate positions, nearest actual dense road edge/width and objective anchor, plus up to eight actual support/overlap collider hits for each of the first 24 rejected probes. Clearance margins, support tolerances and obstacle checks are unchanged. A failed temporary NavMesh registration is removed exactly once before its data is destroyed. No diagnostic operation moves obstacles, flattens terrain or fabricates a route. Reports and the stronger before/after pause-pose fixture compile, but their native execution remains unrun here.

## Actual forest material chain

The world authoring places this exact owned prefab:

`Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/Prefabs/Trees/P_ENV_tree_ancient_03.prefab`

The prefab/model importer resolves its leaf and wood material references to:

- `Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/Materials/M_ENV_tree_leaves_ancient.mat`
- `Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/Materials/M_ENV_wood_ancient_01.mat`

The leaf material references `Shaders/S_foliage_wind_URP_advanced_emission_U6-1.shader` in that same owned package. Its shader is `TidalFlask/Foliage Wind URP Advanced Emission U6-1`, with the `RenderPipeline=UniversalPipeline` subshader tag. The installed material has Wind Strength 0.196, Wind Scale 0.264 and Wind Speed (2,1). The actual shader computes noise from time-scaled wind speed and uses wind strength in vertex-position deformation. The vertex-color wind switch is off; the shader retains its non-vertex-color wind branch. Thus this is actual installed shader animation, not an invented vegetation sway component.

The wood material uses `Shaders/S_pbr_vertexcolorfade_URP_U6-1.shader`, also tagged `UniversalPipeline`. The examined tree prefab contains no independent animation-script reference; its leaf motion comes from the material shader.

`Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAuthoring.cs` calls its existing `RemapLegacyMaterials` adapter after placing the tree. That adapter explicitly retains materials whose RenderPipeline tag is UniversalPipeline, so both material references follow its preservation branch. No generated `Materials/Environment/<leaf-or-wood-sourceGUID> URP.mat` replacement existed for these two materials during this audit. Static source evidence therefore supports retained vendor wind rather than loss through generic URP Lit conversion. The native saved-instance material reference, shader compilation and actual visible leaf displacement still require rendered validation. Wind is not yet proven to respond dynamically to the selected weather; the retained material currently uses its authored constant wind parameters.

The existing world authoring also preserves and assigns owned controllers for its windmill sail/waterwheel props. That separate source configuration is not native motion acceptance.

- **VENDOR ASSETS AUDITED:** Tidal Flask forest prefab/material/shaders; Camping living-art disable behavior; Weather Maker audio fade wrapper; DOTween, Navigation and shared Sports audio conventions.
- **VENDOR ASSETS USED:** Existing wind shader, rig animation, tween/navigation and fade authorities.
- **RELEVANT ASSETS NOT USED:** No replacement foliage shader, procedural tree sway, mixer or navigation framework was created.
- **CUSTOM SYSTEMS CREATED:** Narrow pause bookkeeping and observed-pose preservation only.
- **VENDOR SOURCE MODIFIED:** NO.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO.
