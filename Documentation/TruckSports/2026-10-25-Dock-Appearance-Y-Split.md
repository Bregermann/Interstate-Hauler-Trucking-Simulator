# Dock selected-body split rendering — source handoff, October25

Status: IMPLEMENTED / UNVERIFIED. Static source diagnostics pass; no Unity import, native execution, scene save, Windows capture or human acceptance performed by this source owner.

The active dock actor now binds the actual current participant at the original attempt boundary, alongside eight fixed-owner staged trucks. Its selected body uses independent private horizontal-cut recipes. Original NWH controllers, Rigidbody/colliders, launch forces, local split formula, water scoring and the original1000-frame recorder remain authoritative. The upper presentation pivot is below the same legacy model as the original upper pieces, preserving the existing0.6-local-unit separation and that model's scale. Wheels keep the existing NWH/Host pose authority; rear equipment stays below, upper cosmetics follow the upper pivot.

Explicit stopped-Editor sequence (root owns scene open/save):

1. Open `Assets/TruckDockDiving/Scenes/TruckDockDiving.unity`.
2. `LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.CreateDockSplitPlaneSettings()`.
3. Inspect `Assets/TruckDockDiving/Customization/PrivateLicensedSplitBodies/Dock split planes.asset`. Its Inspector renders the actual selected body or saved halves, explicit vehicle-local Y plane, and original-scale separation preview. Seeded Y0.65 is subject to native/artist inspection. No preview or Inspector change regenerates assets.
4. `LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.CreateMissingDockSplitRecipes()`.
5. `LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring.InstallOpenDockAppearance()`.
6. Explicitly save only after successful validation. Existing stale/custom recipes or partial bindings are preserved and refused for review. No automatic regeneration.

Derived commercial geometry stays private under the independent Dock folder above. Existing X/Z fragments cannot preserve upper/lower separation and are not repurposed. Each material surface is cloned before the established vendor wrapper cuts it; caps retain the existing private cut-metal material. Each recipe is persisted with all Mesh subassets in an independent staging asset before publication to its final path. Interrupted staging remains inspectable.

Actual asset audit: original GR3D truck and MonsterTruck sources contain UV0, Fire_Truck UV0, and the owned selected icecream prefab uses MiniBus_Commercial_LOD0 with UV0+UV1. That LOD0 extra channel is active source content. No source vertex colors were found in those FBXs, but the added reconstruction is tested with synthetic colors. EzySlice interpolates UV0/normals/tangents. Extra UV1–7 dimensions2–4 and colors are reconstructed only for cut exterior triangles using the original material surface, barycentric position, UV0 and normal seam match. Conflicting matches refuse; output vertices are duplicated across seams. Caps use separate canonical X/Z extra coordinates and white color. Uncut source attributes retain exact indexed values. Weighted/blendshape or unsupported one-component UV data refuses instead of being dropped.

The original replay keeps its physical ghost root, frame array, times, interpolation, four angles, speed and rewind controls. One actual selected-render snapshot is built only at the original empty Begin boundary. Existing upper/lower replay pivots consume the recorded split value; material/property-block metadata refreshes at original Play. It never records another timeline or calls a score callback. At most one active selected split and one render snapshot exist, using shared private Mesh assets. Previous rendering objects are deactivated and detached before deferred destruction. The fixed1000×36-byte pose payload remains approximately35.2KiB; no new pose buffer is added.

Validation commands/filters for the root's exclusive Editor:

- EditMode `DockSplitRecipeTests`:3 synthetic cases plus7 actual body recipes. Checks vendor horizontal area/caps, exact uncut indexed attributes, extra UV/color interpolation, ambiguous-surface refusal, persistent private meshes/source hashes, original material-surface coverage, plane sides and actual selected icecream LOD0 UV1.
- PlayMode `DockAppearanceIntegrationTests`:2 cases. Controlled all7 bodies verifies9stable actor owners, single original physics configuration, original local separation/lower stationary/physical pose and velocity invariance, original1000buffer/ghost identity and no invented score. Separate genuine AI approach→flight→first-water distance→all4original recorded angles/rewind→next actual owner→rematch checks original outcomes and selected metadata. These are awaiting native execution.
- Existing original Dock lifecycle/rule/Slang tests remain required regression coverage; no original test was disabled or changed.

Static diagnostics:20source files,534cached references,0errors, no assembly emission. This is source-level evidence only. New geometry, cap surfaces, scene integration, original AI outcome, render correctness and subjective body split presentation still need native/Windows/human verification.

VENDOR ASSETS AUDITED: installed EzySlice cutter and interpolation implementation; NWH original GR3D/MonsterTruck body and real wheel/container APIs; UTS Fire_Truck/MiniBus authored LOD meshes; existing Sports Host/private fragment wrapper and Dock recorder.

VENDOR ASSETS USED: EzySlice through the frozen robust owned-mesh wrapper; actual NWH wheels/physics unchanged; actual owned seven catalog body prefabs/materials; existing Host fit/color/equipment and original Dock split/replay.

RELEVANT ASSETS NOT USED: existing X/Z fragment recipes cannot distinguish Dock upper/lower pieces. EzySlice has no extra-UV/color interpolation, requiring the narrow source-surface restoration around its cut. No alternate physics, camera or recorder is introduced.

CUSTOM SYSTEMS CREATED: Dock-only private Y recipe/settings/preview author, extra-channel restoration, selected rendering adapter, original-recorder rendering snapshot and focused fixtures.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none; EzySlice remains cutter, NWH remains physics/wheel authority.
