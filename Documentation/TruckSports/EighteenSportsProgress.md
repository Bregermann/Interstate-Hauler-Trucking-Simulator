# Eighteen-sport expansion — working implementation evidence

The original master request and UI addendum are preserved in `Requests/`. The authoritative permanent requirements remain in `TRUCK_SPORTS_ACCEPTANCE_LEDGER.md`; this note is a concise evidence index, not a replacement backlog.

## Current vertical slices

**Red Light, Green Light:** saved twelve-full-NWH-tractor stadium, whole-body swept finish detection, actual displacement-based red-light violations, one-second laugh warning without yellow, measured external-impact allowances, bounded actual-mesh destruction, placement results, rematches, shared/chase/full-cab views, global weather/crew/horn and optional AI cameras. The UTS giant master uses the original rig and animations. Existing posed stadium spectators react through the shared Field Goal crowd adapter. Source: `Assets/TruckRedLightGreenLight/`.

The stopping investigation reproduced NWH Auto D/N/R interpreting a held service brake at rest as reverse. The mode now holds stopped trucks using the handbrake without continuing the reverse request. Ordinary reverse remains available to humans; actual movement still causes violations. Earlier failed probes are retained. The keyboard rematch fixture also exposed the batch Editor's Game View input-focus filtering; the fixture temporarily selects the documented Input System editor behavior and restores it afterward. It never writes gameplay commands to manufacture a pass.

**Hot Potato:** saved eight-full-NWH-tractor arena, one atomic bomb owner, genuine collider-contact passing with no ram-speed requirement, compound/resting-contact suppression, separation and transfer cooldown, bounded random fuse cycles, carrier engine-power boost, elimination blasts and recorded actual debris, capped per-cycle pass rewards, sole-survivor results, safe recovery, and resettable reused Core-Collapse props. Carrier AI physically chases; other AI physically evade. Source: `Assets/TruckHotPotato/`.

Hot Potato reuses the existing Union Field venue and shared two-pose spectator assets. One thousand spectator seats were sampled against actual stand geometry and rendered with the existing shared posed meshes and instancing-enabled materials; temporary authoring colliders are removed before saving. Crowd reactions, weather, cabs, identity, controls, horn and camera options use existing adapters.

## Artwork and UI

The approved third Red Light logo establishes the requested scraped-paint, worn-ink PS2-era direction for the new sports. Hot Potato has distinct original artwork in that style. The existing41logos are unchanged. Prompts and generated-source provenance are retained in `Tools/TruckSports/ArtworkSources/`.

The new modes use actual gameplay previews, saved menu actions, original PNG setup/help artwork, compact rounded scorecards, viewport ownership and recorded match-end review. New shared adapters extend existing Heat/TMP/DOTween presentation and `SportsMotionReplay`; they do not replace those foundations.

**Balloon Battle:** eight full NWH tractors, exactly three giant colored balloons per truck, bounded damped kinematic tethers with two recorded cord segments, real capsule/spike contacts, quarter-second yellow telegraph,0.6-second attack and2.5-second cooldown. Each attack can pop at most one balloon on each victim. +1 per pop, +3 elimination, +5 sole-survivor bonus; simultaneous mutual final pops resolve a draw. Confetti uses pooled cut-paper particles; eliminated humans get spectator views. Shared/chase/full-cab cameras, global names/colors, weather, horn, results, rematches and recorded review are integrated. The stadium reuses the existing venue/crowd with teal/plum/marigold lanes, EasyRoads banks and authored festival pennants. Source: `Assets/TruckBalloonBattle/`.

Its first countdown test exposed diagonal spawn footprints intersecting ramp ends: maximum horizontal displacement was4.390m. Moving the full starting ring from32m to42m puts every tractor on the flat outer lane; the corrected native and Windows fixtures report approximately0.056–0.058m maximum planar settling while all eight trucks are grounded and upright. Vertical suspension settling is reported separately. Visual inspection also caught nonserialized authored MaterialPropertyBlocks producing white balloons; participant-derived colors now apply on every round/reset. Both causes were corrected rather than loosening the drift threshold or painting over captures.

**Whack-a-Mole:**36pooled holes, three original animated mole prefabs, saved Emerge/Look/Dive/Whack clips, actual moving front-bumper contact and one hit per exposure. Standard/golden/decoy targets award10/50/−25points; decoys physically launch NWH's full tractor assembly. Timed2/3/4-minute competition gradually increases active targets and shortens exposure. The complete fairground uses existing fences, scenery, stalls/pennants and shared posed spectators, a live world scoreboard and its original grungy PNG. Shared/chase/full cab, crew/weather/horn, statistical/shared-placing results, actual recorded animated-mole review and rematches are integrated. Source: `Assets/TruckWhackAMole/`. The Windows AI result trace includes five genuine golden hits and one incidental decoy hit; AI receives no contact immunity.

**King of the Hill:** five collision-matched stone plateaus, eight EasyRoads bridges with visible edge rails, an actual connected67-node/70-edge LWS road graph and protected spectator overlook. Eight full NWH tractors compete for60cumulative uncontested control seconds;30/90options, physically present grounded/upright eligibility, opposing overturned bodies still contest, inaccessible proxy colliders/airborne trucks do not score. Every40seconds the hill relocates with a three-second announced preview. Actual AI uses the existing route/driver adapter, outward shoving, physical cliff checks and separated recovery. Results lock, recorded review, rematch and software-controlled cab/chase/shared views are integrated. Native and Windows probes each passed33checks. The tested Windows default match completed with Player5reaching60control seconds; this is automated gameplay, not a human pacing approval.

Its AI investigation found per-physics-step local retargeting cleared measured stall/reversing state. The new direct-goal adapter preserves that state. A separate route-equivalence test preserves Fare Fight's existing district API. Cliff support checks and continuous visible bridge rails reduced repeated recoveries; real combat retains exposed plateau edges.

The collection now has47saved build scenes including the front end,46sport scenes/variants including experimental Wallball. Five of the18new sports are integrated. The remaining13have not been declared finished. Seesaw source/artwork is being prepared separately; no unfinished menu entry is installed.

## Verification actually executed so far

- Latest native Unity6000.4.10f1 focused EditMode:69/69passed. Red Light13, Hot Potato11, Balloon13, Whack12, Hill18(including district/graph route-equivalence and five actual driving-surface metadata cases), registered artwork/viewport presentation2. Combined XML: `five-editmode-final.xml`. The first new metadata test used the wrong assembly-qualified Taxi type; that fixture was corrected and all five cases passed. No unrelated test suite is certified.
- Native Red Light actual NWH/PhysX: complete twelve-truck AI matches, grounded/separated countdown, pause, legal finishes, deliberate software-keyboard violation/destruction, rematches and recorded replay controls. Latest complete probe30checks/0errors.
- Native Hot Potato actual NWH/PhysX: complete eight-truck AI matches, actual passes, seven eliminations and one survivor, pause, results/replay, rematch and a controlled0.65m/s real-tractor contact. Resting contact does not repeatedly swap possession. Latest complete probe30checks/0errors.
- Each mode's probe also exercised one through four injected-controller participants plus AI, individual accelerator ownership, human-only cab rectangles, optional AI views, and AI-only broadcast/view allocation. Four-cab720p/1080p controlled Editor fixtures were captured.
- Native shared-camera, cab and replay fixtures were visually inspected. These are explicitly controlled Editor renders, not Windows framebuffer or human gameplay evidence.

Detailed native logs/XML are in `Logs/TruckSports/EighteenSports/`; native probe receipts/captures are under the isolated project's matching log folder. Windows receipts/captures are in the host log folder. No physical controller, steering-wheel, listening or human-playtest acceptance is claimed.

**Current Windows acceptance:** Hot Potato32checks/0errors; Red Light36/0; Balloon34/0; Whack33/0; Hill33/0; normal snow accumulation and actual NWH tire tracks in all five arenas15/0; menu304/0. Every process exited0 on the same managed build hash. Each gameplay probe ran full actual NWH/PhysX AI gameplay, real-contact scenarios, pause, rematch, replay and injected1–4human camera/input layouts, including fifth-human rejection and AI-only view policy. Corrected native Balloon/Whack PlayMode probes also passed34/0and33/0. Shared review hides live fragments during recorded playback and switches Weather Maker's allowed camera to the actual replay broadcast; ordinary camera lists restore on exit. The earlier Balloon title-art naming defect was repaired. Whack's preview must be installed before refreshing the shared menu: the pipeline ordering was corrected after that guard rejected an incomplete card. Final menu acceptance passed304checks, including actual setup launch/return for every registered sport.

Build: Windows x64 Development/Mono/D3D11, Unity6000.4.10f1,47explicit scenes,0errors/57warnings,1,805,892,042reported bytes. Managed assembly SHA256 `d47679281b4d496f50d97639e7d61a1dc82298e7ad7787a3eabb0480acd5a3ff`. Canonical output remains `Builds/TruckSports/TruckSports.exe`, with all companion files. Actual composed Windows frames, focused XML and process receipts are copied into tracked `Evidence/2026-10-20-Expansion/`.

## Open work

These five vertical slices are not a declaration that the entire expansion is finished. Their remaining acceptance criteria are recorded against individual ledger entries. A severe-weather replay fixture produced insufficient visibility in one earlier run; later clear-weather recorded frames rendered correctly. Severe-weather replay visibility and subjective audio remain unverified rather than silently certified.

The other13sports retain their original requirements. Next: Seesaw, Floor Is Lava, Tetherball, Jousting, Magnet Mayhem; Volleyball, Freeze Tag, Hungry Hungry Haulers, Sheep Herding; Hide and Seek, Frogger, Avalanche Escape, Ninja Warrior. Existing source scenes, private Taxi changes and imported vendor work are preserved. The preservation check found zero changes across887protected files and25host settings. Original41logo hashes are unchanged.

Specific remaining acceptance limitations: no physical controllers/wheels, subjective listening, human matches or isolated CPU/GPU benchmark; severe-weather replay visibility remains unverified. Replay records truck/debris/balloon transforms, not the entire historic weather/light/particle/prop-damage timeline. Red Light's unrequested183-second absolute timeout was removed and has a new regression test: slower legal races continue until the rules resolve. Digital stadium standings, PA towers, pooled NWH-derived smoke/sparks and Cinemachine shake are integrated; actual violation-triggered particles/stat updates/impulse passed native and Windows probes. Hot Potato now tightens physical intercept/steering and escape commitment over the final six fuse seconds; complete Windows matches still pass the shared contact/scoring rules. These limitations are not a declaration that the entire18-sport expansion or every presentation detail has passed.

## Vendor accounting

**VENDOR ASSETS AUDITED:** NWH full tractor/input/transmission, UTS character/animation/traffic-light assets, RoadProps, EasyRoads, existing Core-Collapse integrations, Weather Maker/Weatherade, Heat, DOTween, Cinemachine and the existing Sports replay/crowd adapters. See `EighteenSportsVendorAudit.md`.

**VENDOR ASSETS USED:** NWH Vehicle Physics2 through `SportsNwhTractorMotor`/`FareFightVehicleAuthoring`; UTS `Man_11`, talk/cheer clips and Traffic Light_1; RoadProps barriers; EasyRoads baked ramps; existing `PlowBreakable` Core-Collapse props; Weather Maker/Weatherade through shared Sports adapters; Heat/TMP, DOTween and Cinemachine through existing presentation components. Original two-pose Taxi spectator meshes and sport-owned venue geometry are reused.

**RELEVANT ASSETS NOT USED:** no installed suitable giant cartoon bomb model was found; the bomb shell/fuse is original sport-owned geometry. A second destruction, replay, traffic, vehicle or weather framework was unnecessary.

**CUSTOM SYSTEMS CREATED:** sport-specific rule authorities, AI input coordination, scene authoring, contact adapters, evidence probes, original artwork and small read-only presentation adapters over existing shared systems.

**VENDOR SOURCE MODIFIED:** NO.

**DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO.

Explicit normal snow checks found unnamed competition floors missing existing Weatherade track metadata. Sport-owned tags now bind all five actual surfaces. Balloon's visible25mm overlay now has a matching MeshCollider. Native and Windows15-check probes passed actual accumulation and wheel-generated track stamps in all five saved scenes. The first multi-scene probe incorrectly stayed in its initial base session; explicit scene identity checks fixed that fixture, and the final probe genuinely loaded all five scenes.
