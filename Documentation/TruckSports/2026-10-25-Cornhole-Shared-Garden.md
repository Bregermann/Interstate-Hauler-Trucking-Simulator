# Cornhole shared board and miniature garden

Carnival §12.1–12.5 source prepared October 25, 2026. Native authoring, physical execution, image review and human verification remain pending. This implementation agent ran no Unity/player/Git, saved no scene and wrote nothing to CLEAN.

Explicit entry: `LWS.TruckCornhole.Editor.CornholeCarnivalAuthoring.InstallOpenScene()`.
The caller opens the existing Cornhole scene, invokes this entry, reviews and saves. It never opens/saves a scene or regenerates a collection/build. No load/build/rematch generator exists.

The first original open board/mesh collider/circular opening and catch become the only shared target. Superseded targets/grounds remain inactive. The original four ramp/trampoline assemblies use yaw -18/-6/+6/+18 around that board at the original target distance. Shared catch width is 96 units; its elevation/longitudinal bounds stay unchanged. Isolation walls deactivate. Original NWH torque/brake/suspension, mass, launch, air control and wind coefficients stay unchanged; horizontal coordinates rotate with each approach.

Shared target identity never moves bodies or scores. Existing overlap-safe pair gating stays authoritative. Original downward whole-body hole passage, native top support, stable time, ground invalidation, retained bodies, four attempts per owner, independent progress and whole-round banking remain. Cross-participant counters are read-only actual contact telemetry.

Layer28 is shared venue,29 live trucks;24–27 remain each owner's private cab/replay visuals. Collider layers/global collision matrix stay unchanged. Existing replay captures at most16original trucks,1400samples at the original.05s interval. Pose/time payload is722400bytes per owner,2889600bytes (~2.76MiB) for four owners, plus bounded managed arrays and at most64collider-free visual trucks across four pools. Each replay viewport hides all live trucks and shows its recorded opponents. Other viewports retain live presentation. Original pause/rates/seek/45-second review limit/next-attempt ownership remain.

Approved miniature art convention: original24-unit board represents a fictional1.2m garden board: **20art units per fictional metre**. A proportional1.8m person is36art units; the original1.2-unit truck is approximately a6cm beanbag equivalent. This is explicit in source/Inspector. It does not claim humans are literally1.8Unity units, shrink only truck colliders, change gravity/wind units or add render mapping. Yard furniture/fences/grass use the same convention.

Installed Taxi fences, UTS business male/female rigs, genuine Taxi Human idle/walk/cheer/shout, restaurant tables/chairs/lamp, RoadProps wood crate, NWH banner and owned textured wood/grass supply art. Source prefabs/importers/materials stay untouched. Clean render hierarchies avoid live NWH/UTS/passenger Awake. Cornhole-owned looped Human clips/controller persist required states before venue work; interrupted empty controllers repair in place while nonempty custom motions remain. Eight of32spectators walk bounded perimeter paths. Physical result state drives real clip reactions; no spectator collider/Rigidbody/traffic authority/score mutation exists.

Native filter: `CornholeSharedGardenPlayModeTests` (four cases):

- Natural four original AI ramps/trampolines, one physical target, genuine cross-owner contacts, retained16attempts and once-only original final banking. No supplied poses/velocities/points/launch flags.
- Original1–4viewports; real replay recording;16snapshots; four angles/three seek positions; private cabs, hidden live opponents, unchanged bodies, pause/cleanup and720p/1080p captures.
- Separately labeled controlled cross-owner native push. Real stable original board point must become an actual hole/ground outcome after native impact; temporary instability is insufficient. No supplied points/receipt.10MPH direct-headwind cap checked in four headings.
- Actual Human rigs/clips/bone movement, walking, documented scale, no presentation colliders/listeners/cameras, real result reactions, pause and reused rematch pool.

Retain original clean-hole, body-retention, bank/result/input/replay regressions. Only old controlled trampoline coordinates in CornholeSmoke and CornholeCarnivalLifecycleTests convert to the actual approach frame. Thresholds/strong retained-hole assertions/root1e-4spawn check remain unchanged. Help/setup copy describes the shared target; §12.6HUD redesign is separate.

VENDOR ASSETS AUDITED: NWH WheelController, UTS/Taxi Humanoid foundation/clips, DOTween, installed props/materials and existing weather rendering.

VENDOR ASSETS USED: original NWH/PhysX, DOTween perimeter paths, genuine UTS Human rigs/Taxi clips and installed yard props/textures. Existing Weather Maker/Weatherade remain authoritative.

RELEVANT ASSETS NOT USED: UTS traffic controllers; decorative spectators need no traffic authority. No replacement replay/input/render framework.

CUSTOM SYSTEMS CREATED: mode-only shared-target identity, approach coordinate adapter, bounded spectator presentation and explicit additive venue author.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
