# Dock source-plane PNG contact sheet — explicit Editor diagnostic

Source status: frozen; not executed by the source owner. This is separate from the original22-file Dock handoff; those hashes remain unchanged.

Root invocation on the explicitly opened, stopped original Dock scene:

`LWS.TruckDockDiving.Editor.DockSplitBatchPreview.CaptureOpenDockSourcePlanes()`

Menu: Truck Sports / Customizer / Dock / Capture Seven Source Bodies And Cyan Planes.

The method calls the existing explicit CreateDockSplitPlaneSettings (creates missing plane settings only), then renders all seven ACTUAL UNCUT source prefabs in their saved fitted vehicle frame, with cyan vehicle-local Y planes. It does not generate recipes, invoke the installer, save/rebuild scenes or change runtime gameplay. It creates a temporary preview scene containing clean renderer-only source copies, two lights, a normal CameraType.Game URP Base camera with a private640×480 RT, and captions using the installed broadcast font. It avoids PreviewRenderUtility/CameraType.Preview backbuffer rendering. No extra listener, pipeline asset, renderer asset, global weather camera registration or global rendering setting is added. Original source materials/meshes are referenced read-only; all temporary objects/RTs/materials/textures are cleaned in finally.

Output: `Logs/TruckSports/PlaytestRepair/DockSplitPlanePreview/<UTC timestamp>/seven-source-bodies-cyan-planes.png` (2560×960, four columns/two rows, seven labeled cells), plus `source-plane-preview.txt` recording each body ID, renderer count, plane, fitted bounds and dependency hash. Canonical output containment is checked. Root must inspect the real PNG before authorizing private cut generation; source compilation is not visual acceptance.

Static Roslyn diagnostics including this new file:21source files/534cached references/0errors; no assembly emission or Unity invocation.

VENDOR ASSETS AUDITED: existing seven catalog body prefabs/materials, original Dock fit settings, installed URP Game/Base camera APIs and broadcast font.

VENDOR ASSETS USED: actual catalog source meshes/materials, existing fit/plane settings, installed URP renderer and broadcast font.

RELEVANT ASSETS NOT USED: PreviewRenderUtility's URP preview/backbuffer path is intentionally bypassed for this batch diagnostic. No cut recipes are consumed/generated.

CUSTOM SYSTEMS CREATED: one explicit source-plane contact-sheet diagnostic.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
