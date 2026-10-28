# Tornado Rodeo — Game 6 source handoff

`LWS.TruckTornadoRodeo.Editor.TornadoAuthoring.Create` explicitly creates the missing `Assets/TruckTornadoRodeo/Scenes/TruckTornadoRodeo.unity`. It refuses an existing scene and never runs during normal startup/build. Current source is implemented; Unity authoring, native physics, rendered art and final integration remain unverified.

## Foundations and boundaries

Weather Maker retains sky, clouds, rain, lightning, global wind and time ownership through the existing Sports adapters. The installed Weather Maker vortex shader is fullscreen image distortion, not a local lifting force. `TornadoFieldModel` therefore owns only this particular spatial hazard: suction, velocity-relative tangential flow, lift, gust variation, drag and ceiling restoration. It reads existing `presentationWeather.Flow`; it never writes Weather Maker's global profile/wind, gravity, time step or collision matrix.

Full NWH tractors are direct instances of the existing Fare Fight prefab. The source prefab is never passed through `EnsurePrefab`, dirtied or rewritten. NWH retains powertrain, suspension, wheels, compound colliders and contacts. Existing `FareFightRoute` owns ground driving; `SportsLimitedAftertouch` owns finite airborne horizontal correction. Existing `SkyTowBodyGate.Sweep` supplies gate geometry before the mode's stricter whole-physical-envelope aperture check.

Lead-owned common integration remains explicit: collection registration, logos and actual menu-card screenshots, shared Cinemachine broadcast framing, weather/time/SRS and release metadata. After the first scene save, the author invokes the lead's `BindNewImpossibleScene(match, trucks)` over the exact four fixed owners. The scene has the original four driving/cab views and complete roster. `DescribeBroadcast` reports an actual high/current scorer, actual ring or actual flare/landing objective, never a fabricated camera subject.

The owned `TornadoStormOrder` LateUpdate adapter runs after `SportsEventSession.StartMatch` has configured the existing provider. It preserves every explicit forecast selection and only narrows Random to actual enabled Thunderstorm, Hurricane, Blizzard, Heavy Rain and Heavy Snow profile data. Distinct actual IDs receive equal discrete choices from a deterministic match-token seed; catalog order and display-label changes do not affect eligibility. It calls only the existing `ConfigurePreset` API once per new MatchId, never global Select/preferences/catalog mutation. Pause/resume and repeated starts with the same token do not reroll. Missing provider/resource/catalog bindings remain clearly pending; no fake storm or fallback label is created. The lead still installs the provider/time/SRS. `TornadoAuthoring.InstallOpenStormOrder` is an explicit idempotent upgrade for a saved V1 venue; it refuses unknown/duplicate bindings.

## Playable rules and physical behavior

- One-to-four simultaneous human/AI participants;90/150/210-second competition and three-second ready gate. Shared profiles, source routing, disconnect/pause, settings, Help, crew, results, rematch and collection return remain the original Session/Heat flow.
- The finite field extends68m horizontally and has a120m hazard height with a soft upper restoration band. Field acceleration is capped28m/s². ACTION gives seven seconds of physical flare/egress with an eight-second cooldown: reduced field response, outward drag, descent drag and bounded upright torque. Additional flare force is capped20m/s². No airborne velocity/position assignment is used. Slow overturned ground recovery is an in-place physical hop/torque with a five-second cooldown.
- Actual unsupported height of at least6m begins a flight. New10m tiers bank5 points. Signed actual quaternion rotation increments cancel rocking; every complete360° banks12. Impossible per-step rotation samples are rejected.
- Six real open torus hoops have physical rims. A swept crossing must fit every original physical truck-envelope corner within the actual9m aperture, using existing gate sweep geometry and preserving the selected truck's original physical dimensions. A ring banks30 once per flight.
- Sixteen real Rigidbody farm objects include twelve installed crate/barrel models and four genuine rigged SheepV5 animals. The same local force field lifts them from authored ground positions; they are not placed on animation paths. Sheep use the existing skeletal locomotion controller and `SportsSheepMotion`, driven by actual displacement. Animals are cartoonish and unharmed.
- Debris near-misses require an actual moving close encounter, closest approach, subsequent separation and no native contact in that encounter. They bank3 once per physical object/flight. Real native contacts suppress that avoidance reward and drive positional feedback.
- Controlled landing banks40 only after actual NWH wheel support for0.8s, up-dot≥0.7, contact/downward speed≤12m/s and planar speed≤16m/s. Native roof/body ground impacts latch uncontrolled contact, preventing a later self-right from disguising the crash as a clean landing. Unsupported or uncontrolled landings earn no bonus. Drive back in for a new flight. True world escape retires the actual rider in place; no active-run teleport or fake result is used.
- AI physically drives into the field, selects real remaining rings, uses the same finite correction/control and flares after an actual airborne interval or excessive height. Ground recovery and landing criteria are shared. Score receipts are confirmed only after actual rules bank; ties preserve equal places.

## Finished content foundation

The explicit author reuses the saved collision-matched Vista farm surface, worn grass/dirt materials, original detailed timber barn, installed farmhouses/storage and Vista pines from the existing Herd venue. It copies only render content from that source scene, closes without saving it and adds owned static building bounds. Original source scenes/assets are not rewritten. New modeled corrugated silos, access ladders, fences, shelter board and physical torus beacon hoops complete the storm activity layout.

The local funnel is a bounded Unity particle spiral using the installed Weather Maker smoke texture and owned material over the existing NWH particle foundation. It visualizes the local force region; Weather Maker remains responsible for the atmosphere. A low skirt, existing CoreCollapse contact dust, Weather Maker wind audio, shared sports cues, TMP board and Heat/DOTween score feedback complete presentation. All physical objects are authored in finite arrays; normal play creates no new debris or scenery. Pause freezes field/scoring/physics, particles, animation time and feedback; rematch resets the exact existing actors.

## Validation gates

Pure filters: `LWS.TruckTornadoRodeo.Tests.TornadoRulesTests`, `LWS.TruckTornadoRodeo.Tests.TornadoFieldTests` and `LWS.TruckTornadoRodeo.Tests.TornadoStormPolicyTests` (28 cases). Exact NUnit cases passed in a managed harness outside Unity; this is not native/EditMode evidence. An additional read-only `TornadoStormCatalogTests` EditMode case checks the actual five installed severe entries and unchanged catalog/preference values; Unity execution is pending.

Native filter: `LWS.TruckTornadoRodeo.Tests.TornadoLifecycleTests` (four cases):

1. Genuine four-AI entry, lift, real stunts, flare, actually supported controlled landing, finite match, pause invariants and rematch. Captures actual GameView720/1080; checks original gravity/simulation mode and genuine sheep bone animation.
2. Explicitly controlled native full-NWH trajectories through the real ring and at its edge, checking the existing swept geometry/whole aperture while proving read-only geometry cannot itself bank points.
3. One-to-four actual AI view owners and typed score cards, then shared view.
4. Four software gamepads drive distinct original NWH actors and retain actual profile ownership. Software inputs do not establish physical-controller acceptance.

Cached current0.6 runtime/test API compilation passes. Author API compilation uses an explicit compile-only signature for the lead's new shared appearance hook. Actual author/native/visual/weather/Windows validation and human/device/listening/performance acceptance remain pending.

VENDOR ASSETS AUDITED: Weather Maker wind/vortex/public adapter APIs, existing airborne aftertouch and SkyTow gate geometry, NWH/Fare Fight driving, Vista/Herd farmland, IndieKit crates/barrels, SheepV5 animation, Heat/TMP/DOTween, CoreCollapse particle assets.

VENDOR ASSETS USED: Those existing foundations; local spatial force/game semantics fill the missing lifting-tornado behavior.

RELEVANT ASSETS NOT USED: Weather Maker's fullscreen vortex distortion is unsuitable as a physical3D hazard. Unrelated scene debris managers retain their original mode ownership. No compatible installed silo model was found; the owned corrugated model fills that art gap.

CUSTOM SYSTEMS CREATED: Mode-local spatial force model, actual stunt/flight/landing/avoidance rules, finite debris coordination, original silo/ring meshes, explicit author and targeted tests.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; global weather, NWH physics, route driving, input/profile/menu/camera and presentation foundations remain their existing owners.
