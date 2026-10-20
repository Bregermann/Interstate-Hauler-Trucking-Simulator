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

The integrated collection now has46saved build scenes including the front end,45sport scenes/variants including experimental Wallball. These four playable modes are distinct from the remaining14requested sports. King of the Hill's original logo and source/control rules are in progress; there is no empty placeholder menu entry.

## Verification actually executed so far

- Native Unity6000.4.10f1 EditMode: Red Light12/12; Hot Potato11/11; Balloon13/13; Whack12/12; registered-artwork/viewport presentation2/2.50targeted tests passed for these four modes, not a claim about the entire discovered suite. Future Hill rule tests separately passed12/12; that does not make Hill playable yet.
- Native Red Light actual NWH/PhysX: complete twelve-truck AI matches, grounded/separated countdown, pause, legal finishes, deliberate software-keyboard violation/destruction, rematches and recorded replay controls. Latest complete probe30checks/0errors.
- Native Hot Potato actual NWH/PhysX: complete eight-truck AI matches, actual passes, seven eliminations and one survivor, pause, results/replay, rematch and a controlled0.65m/s real-tractor contact. Resting contact does not repeatedly swap possession. Latest complete probe30checks/0errors.
- Each mode's probe also exercised one through four injected-controller participants plus AI, individual accelerator ownership, human-only cab rectangles, optional AI views, and AI-only broadcast/view allocation. Four-cab720p/1080p controlled Editor fixtures were captured.
- Native shared-camera, cab and replay fixtures were visually inspected. These are explicitly controlled Editor renders, not Windows framebuffer or human gameplay evidence.

Detailed native logs/XML are in `Logs/TruckSports/EighteenSports/`; native probe receipts/captures are under the isolated project's matching log folder. Windows receipts/captures are in the host log folder. No physical controller, steering-wheel, listening or human-playtest acceptance is claimed.

**Current Windows acceptance:** Hot Potato32checks/0errors; Red Light34/0; Balloon34/0; Whack33/0; menu298/0. Every process exited0 on the same managed build hash. Each gameplay probe ran full actual NWH/PhysX AI gameplay, real-contact scenarios, pause, rematch, replay and injected1–4human camera/input layouts, including fifth-human rejection and AI-only view policy. Corrected native Balloon/Whack PlayMode probes also passed34/0and33/0. Shared review hides live fragments during recorded playback and switches Weather Maker's allowed camera to the actual replay broadcast; ordinary camera lists restore on exit. The earlier Balloon title-art naming defect was repaired. Whack's preview must be installed before refreshing the shared menu: the pipeline ordering was corrected after that guard rejected an incomplete card. Final menu acceptance passed298checks, including actual setup launch/return for every registered sport.

Build: Windows x64 Development/Mono/D3D11, Unity6000.4.10f1,46explicit scenes,0errors/57warnings. Managed assembly SHA256 `c04fb88e41cc57244f70814b33e126ff0371001cd8d3139b53e93fb03fe133a8`. Canonical output remains `Builds/TruckSports/TruckSports.exe`, with all companion files. Actual composed Windows frames, focused XML and process receipts are copied into tracked `Evidence/2026-10-20-Expansion/`.

## Open work

These four vertical slices are not a declaration that the entire expansion is finished. Their remaining acceptance criteria are recorded against individual ledger entries. A severe-weather replay fixture produced insufficient visibility in one earlier run; later clear-weather recorded frames rendered correctly. Severe-weather replay visibility and subjective audio remain unverified rather than silently certified.

The other14sports retain their original requirements; King of the Hill has original artwork and source/rule work in progress, rather than a playable scene claim. Next: King of the Hill; Seesaw, Floor Is Lava, Tetherball, Jousting, Magnet Mayhem; Volleyball, Freeze Tag, Hungry Hungry Haulers, Sheep Herding; Hide and Seek, Frogger, Avalanche Escape, Ninja Warrior. Existing source scenes, private Taxi changes and imported vendor work are preserved. The preservation check found zero changes across887protected files and25host settings. Original41logo hashes are unchanged.

Specific remaining acceptance limitations: no physical controllers/wheels, subjective listening, human matches or isolated CPU/GPU benchmark; severe-weather replay visibility remains unverified. Replay records truck/debris/balloon transforms, not the entire historic weather/light/particle/prop-damage timeline. Red Light's absolute183-second fallback needs severe-weather timing review. Its detailed digital stadium-stat boards/speaker dressing and dedicated camera-shake/smoke presentation are not all complete. Hot Potato's genuine pursuit/evasion works, but explicit fuse-urgency AI tuning remains. These limitations are not a declaration that the entire18-sport expansion or every presentation detail has passed.

## Vendor accounting

**VENDOR ASSETS AUDITED:** NWH full tractor/input/transmission, UTS character/animation/traffic-light assets, RoadProps, EasyRoads, existing Core-Collapse integrations, Weather Maker/Weatherade, Heat, DOTween, Cinemachine and the existing Sports replay/crowd adapters. See `EighteenSportsVendorAudit.md`.

**VENDOR ASSETS USED:** NWH Vehicle Physics2 through `SportsNwhTractorMotor`/`FareFightVehicleAuthoring`; UTS `Man_11`, talk/cheer clips and Traffic Light_1; RoadProps barriers; EasyRoads baked ramps; existing `PlowBreakable` Core-Collapse props; Weather Maker/Weatherade through shared Sports adapters; Heat/TMP, DOTween and Cinemachine through existing presentation components. Original two-pose Taxi spectator meshes and sport-owned venue geometry are reused.

**RELEVANT ASSETS NOT USED:** no installed suitable giant cartoon bomb model was found; the bomb shell/fuse is original sport-owned geometry. A second destruction, replay, traffic, vehicle or weather framework was unnecessary.

**CUSTOM SYSTEMS CREATED:** sport-specific rule authorities, AI input coordination, scene authoring, contact adapters, evidence probes, original artwork and small read-only presentation adapters over existing shared systems.

**VENDOR SOURCE MODIFIED:** NO.

**DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO.
