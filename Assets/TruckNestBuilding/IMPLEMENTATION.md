# Truck Nest Building — source handoff

GAME17/common requirements; source implementation awaiting lead-native authoring and tests.

Explicit new-scene entry: `LWS.TruckNestBuilding.Editor.NestAuthoring.Create`.
Destination: `Assets/TruckNestBuilding/Scenes/TruckNestBuilding.unity`.
The author refuses an existing destination and dirty open scenes. Build/Play never
rebuilds the scene. It reuses the frozen Mechanical Bull `ImpossibleBalanceArt`
author helper and the lead's `BindNewImpossibleScene` appearance binder after the
first save. No old scenes, vendor files, shared source or registry were edited.

Four full NWH tractors physically gather 48 authored materials with a moving
kinematic gripper and breakable FixedJoint. The hand and decorative boom live
outside the original vehicle hierarchy. Human ACTION taps select/grasp, enter
placement and release; placement axes position the hand, held ACTION changes
height and yaw. AI uses the same hand motion, finite joints and real driving.
AI never assigns a piece pose, interlock, hatch flag or score.

Twelve semantic sockets form crossing branches, sticks, leaves, moss and optional
decorations. Fitting pose alone earns nothing: a released, slow piece must rest
against an already rooted compatible support for1.2seconds before a finite
interlock is created. Broken or unsupported links lose quality. Ten rooted core
pieces settle3seconds before a dynamic egg is introduced; actual low-motion
contact on the moss lining must last5seconds to hatch. Egg loss restarts that
objective. Baby trucks use the parent's selected render-only body, wiggle and
bounce, and play the installed horn at higher pitch. Six actual imported CC0 bird
rigs use their existing flight controllers; actor orbit and changing skin bones
are distinct test requirements.

Cooperation is an untimed shared workshop ending in a family or the explicit
Pause FINISH WORKSHOP action. The competitive format has separate nests and a
six-minute quality window. One to four players can mix human and AI owners.
Every rematch resets physical materials, structural claims, eggs and scores.

`NestMatch` implements the shared `ISportsWorkshopState`. The public
`CaptureWorkshopJson()` / `TryRestoreWorkshopJson(string,out string reason)`
operate only while paused/stopped, and reject held materials. Version1 includes
exact authored piece IDs, current profile IDs, original truck/cab poses, real
piece/egg poses, support IDs, socket IDs, clock and family state. Validation of
the entire payload precedes mutation. Unknown/future/corrupt data, incompatible
profiles/format, invalid poses, unsupported/cyclic structure and inconsistent
incubation reject clearly. Legitimate saved physical poses may be restored only
at this safe boundary. The lead owns Pixel Crushers Saver/storage integration;
there is no new backend or PlayerPrefs. `WorkshopSaveRequested` is the optional
binding point; its absence is stated in the Pause status.

Native filters: `NestRulesTests` (5 EditMode cases), `NestNativeTests` (2 PlayMode
fixtures). The native fixtures clone/restore InputSettings for unfocused batch
keyboard tests, preserve runtime focus policy, and use bounded real-time waits
with durable named telemetry under `Logs/TruckSports/Impossible07/NestLifecycle`.
The AI fixture allows240real seconds at scale4 and fails if ordinary gathering,
rooted construction, a real egg and hatch do not occur. This is a watchdog, not
an acceptance claim. Captures are explicitly controlled native images. No Unity,
player, Windows, hardware, listening or human validation was performed by this
author. Physical AI tuning, full visual review and save-adapter/native behaviour
remain pending actual evidence.

VENDOR ASSETS AUDITED: installed NWH Vehicle Physics2/Fare full-tractor prefab,
existing Mattress balance and Dance movement boundaries, Unity FixedJoint public
API, Heat shared UI authoring, Pixel Crushers workshop interface boundary,
Vista terrain layers, BaseSky installed tree/rock authoring, original NWH horn,
and already imported Quaternius bird controllers/license record.

VENDOR ASSETS USED: NWH through the existing Sports truck adapter and original
physics; Heat menus/HUD; Vista sample grass layer; existing purchased tree/rock
assets via BaseSky authoring; existing NWH horn; existing CC0 Eagle/bird rigs and
real flight clips (`Assets/TruckBaseJumping/ThirdParty/QuaterniusBirds`, unchanged
original bytes and license); existing Sports catalog/body and cab/view adapters.

RELEVANT ASSETS NOT USED: no installed nest-building/physical construction
gameplay was found. No complete decorative nest or baby-truck animation pack is
available. DOTween does not own physical material/vehicle poses; kinematic hands
and actual joint contact remain the single authority for those responsibilities.
Pixel Crushers storage is intentionally supplied by the lead adapter, rather
than duplicated in this mode. EasyRoads is unnecessary for an open clearing.

CUSTOM SYSTEMS CREATED: mode-owned gathering/gripper coordinator, semantic
support/interlock validation and quality rules, safe workshop snapshot,
original finished lathed wood/foliage/egg/timber/gripper geometry, forest layout,
render-only miniature family animation and explicit guarded new-scene author.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none. No new vehicle/input/UI/save backend.
