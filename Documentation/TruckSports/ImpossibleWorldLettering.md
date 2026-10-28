# Alpha 0.7 world lettering

The actual Jenga GPU diagnostic rendered **1,943 white glyph pixels both with
and without an opaque foreground occluder**. Its built-in `GUI/Text Shader`
uses overlay depth behavior. This also explains Moon base markings drawing
over the truck in the reviewed native captures.

`SportsImpossibleWorldTextAuthoring.InstallAuthoredWorldText` explicitly adds
the owned depth-material adapter only to saved Impossible07 venues. It preserves
their authored TextMesh strings, geometry, colors, transforms and font atlas.
Vehicle/cab hierarchies, custom font materials and all older sports are excluded.
No normal build or Play Mode entry regenerates scene configuration.

The adapter shares one runtime material per original font material, follows the
existing font atlas rebuild event, and restores/releases it on disable. The
shader uses the installed URP17.4 `3DText.shader` sample body unchanged except
for its project-owned Shader name. Its alpha-only font sampling is required:
the actual Direct3D11 default font atlas is Alpha8, with black RGB channels.
Multiplying atlas RGB by the text color incorrectly erased every glyph in the
first owned prototype. The original vendor file remains unchanged. Attribution
and Unity Companion License notice are retained in the owned shader.

Actual V13 original GPU acceptance passes1/1 in2.9277411s. The actual saved
container number is visible without the occluder and fully hidden behind it;
all18 original physics positions and velocities remain exact. Both generated
256px diagnostic images were directly inspected. Receipt:
`Logs/TruckSports/Impossible07/impossible07-v13-actual-world-font-visible-and-depth-occluded.xml`.
The earlier overlay and invisible-prototype failures remain retained.

V14 repeated saved-scene lifetime regression passes1/1 in17.9652738s: actual
Jenga labels share one atlas material, disabling a label restores its authored
font material, re-enabling reuses the shared instance, and two real menu exits
release/destroy the runtime material with an empty cache. The original font
asset remains unchanged. Receipt:
`Logs/TruckSports/Impossible07/impossible07-v14-font-shared-atlas-original-restore-and-scene-cleanup.xml`.

Windows scene appearance remains unverified; these targeted results are not a
complete gameplay screenshot or collection-wide rendering acceptance.

V25 venue-image review exposed a separate color defect: the unchanged fixed
function sample uses material `_Color` rather than TextMesh vertex colors, so
authored dark portrait captions appeared white. The adapter now supplies the
original TextMesh tint through a renderer MaterialPropertyBlock, multiplying
the original font-material tint. It updates on actual color changes, retains
the same shared atlas material, and restores the original renderer block when
disabled. Native resources are allocated on enable, never in deserialization.

V27 color/depth and existing lifetime cases pass2/2 in7.7521417s. Actual Alpha8
GPU frames show authored red, a runtime change to blue, and complete opaque
occlusion. All18 paused original container poses/velocities, the original font,
the preexisting renderer-block property and shared-material lifetime are
preserved. All three256px frames were directly inspected. Receipt:
`Logs/TruckSports/Impossible07/impossible07-v27-actual-colored-alpha8-font-gpu-and-atlas-lifetime.xml`.
This strengthens color evidence; it does not establish Windows acceptance.

- VENDOR ASSETS AUDITED: Unity TextMesh/default font, TextMeshPro, installed URP,
  existing per-camera label and UI adapters.
- VENDOR ASSETS USED: Original TextMesh geometry/atlas and installed URP17.4
  `Samples~/URPPackageSamples/SharedAssets/Shaders/3DText.shader` foundation.
- RELEVANT ASSETS NOT USED: Replacing these serialized TextMesh objects with TMP
  would disrupt their existing mode scripts, pools and authored text geometry.
- CUSTOM SYSTEMS CREATED: Explicit new-scene material binding/lifetime adapter;
  shader sample body is reused with an owned Shader name.
- VENDOR SOURCE MODIFIED: Original package files unchanged. The owned sample
  copy changes only its Shader name and adds attribution/provenance comments.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: No second text/UI renderer or atlas.

October28 final release follow-up: actual Windows Jenga gameplay images were inspected in the21-scene opening pass, with the existing owned font/URP adapter. This establishes rendered Windows opening presentation in the recorded camera, not every angle or human readability acceptance. See Alpha07ReleaseEvidence.md for exact candidate/test scope.
