# Pet Pals: source implementation and shared-world boundary

This source slice is implemented but not natively run. See `Assets/TruckCamping/IMPLEMENTATION.md` for the joint author sequence, vendor audit, persistence ownership and explicit validation limits. Both scene authors use the **same actual Camping wilderness prefab**.

Owner `PetPalsMatch` remains a noncompetitive Camping session. Four fixed stable owners each have one actual animated companion selected from a catalog-derived render-only miniature truck, fully clothed stylized adult UTS character and genuine animated CC0 dog. No second vehicle framework is used for pets: Unity Navigation owns the small kinematic companion actor, while the owner's unchanged full NWH truck remains fully physical. Walking/following/free-roam/call/stay, temperament, actual safe route replanning, obstacle-aware slack leash, needs, care, tricks, peeing/kicks, bath, growth, toys and sleeps operate in the real shared world.

The Heat adoption modal uses a detached draft, exact selected body/accessory preview, stable ID application and cancel. Its owned RT/camera/art are isolated below the world on a private layer and released on close/destruction; preview clones contain no physical/input/listener authority. Normal menus are gated only while open and regain their original focus. Keyboard/gamepad uGUI navigation and back use existing owned input semantics. Compatible saved IDs stay saved when catalog runtime fallback is needed.

All seven mandatory tricks require actual pose or travel plus a genuine timing cue before familiarity increases. ACTUAL care owns ACTION during its interaction, so the same press cannot also open the Camping activity menu. Sit/lie/roll/shake use actual bone/assembly/wheel routines; come requires actual travel; stay measures real root stability; dig requires real scraping at a designated soft-soil patch. Actual terrain clones receive holes and a real shallow bowl mesh/collider; original saved TerrainData references reset exactly. Digging is not a text-only counter and does not depend on archaeology's voxel system.

All three pet types use a recognizable gradual limb/wheel lift before a pooled non-graphic stream, then actual backward grass kicks and return behavior. Grass breaks also trigger occasionally during ordinary walks. Bathing requires water, soap, alternating actual scrub inputs, rinse and dry confirmations. It changes live paint/cloth/fur MPBs without altering source materials, glass, chrome or eyes. Physical toys are thrown with real forces, retrieved only by actual proximity/slow movement using finite joints, and returned physically. No invisible live pet attachment, immunity, score, body pose or AI completion teleport is added.

Contextual care and optional pet chase request shots through existing mode camera authority; private cab views remain owned by the original driver. Photo is shared. Every care panel is bound to the actual viewport owner, and there is no compulsory leaderboard or winner. Original sleepy/cute horn presentation does not imply audio-listening acceptance.

`PetProfileRepository` reserves slot1 on the **same actual configured Pixel Crushers storer** as strict workshop slot0. Records use profileID+petID, retain absent crew and prior adopted types, and preserve incompatible original files read-only. Pure/native test source covers state validation, real isolated vendor disk roundtrip and component reconstruction, all7 catalog miniature geometries, actual1–4 ownership/RT cancel, all3 real rigs/all7 tricks/pee/grass kicks/bath/terrain digging and ordinary physical AI toy/care flow. These tests have not been executed by this agent and may reveal integration/visual defects requiring repair.

VENDOR ASSETS AUDITED: shared Camping audit plus actual UTS Generic Biped, Quaternius34-bone dog and compatible Idle/Walking takes, original appearance catalog/mount APIs, Heat Button/InputField APIs, Unity Navigation and existing Pixel Crushers disk record/public APIs.

VENDOR ASSETS USED: shared Camping world/Vista/EasyRoads/foliage/water, unchanged full NWH owners, Heat/uGUI, existing view/input/profile/storage adapters, UTS adult rig, genuine CC0 dog clips, original catalog render-only bodies and explicit wheel/mount metadata.

RELEVANT ASSETS NOT USED: no suitable installed contextual trick pack was found, so missing routines are original animation over genuine explicit bones. Taxi on-foot is career-coupled, SceneStreamer is not duplicated, and designated shallow pet holes use the permitted lightweight approach instead of importing Digger's archaeology framework.

CUSTOM SYSTEMS CREATED: pet semantics/records/AI, original contextual animations, semantic miniature wheel gestures/accessories, care/adoption presentation, finite-joint toys, bounded terrain-hole adapter. Core vendor vehicle/save/UI/navigation/camera systems are reused.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
