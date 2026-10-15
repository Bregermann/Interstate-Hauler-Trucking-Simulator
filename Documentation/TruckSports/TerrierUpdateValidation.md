# Truck Sports update — 2026-10-15

This report supersedes the earlier eleven-sport handoff for this update. The collection now has twelve sports and fourteen explicit saved scenes (including separate Keepaway Arena and First Person scenes). Final player/build and Git status are recorded below only after actual verification.

## Implementation and actual causes

Cheese runout: the old vehicle retained low hill grip (.48), motor24 and heavy .45/.8 damping through the bottom. Ground grade and grounded time now blend into motor42, grip.92, braking110 and steering42. Reduced runout damping preserves momentum. Bounded grounded lateral scrub, yaw damping and upright assistance restore useful bottom control; there is no airborne stabilization or orientation-relative velocity clamp. All sixteen racers share the same component.

Keepaway: 66×38 presentation, rounded 7m physical corners, one connected route grid, seeded crates/barrels, driveable timber ramps, movable tire stacks, rotating striped steel sweepers, organic traction patches and sixteen pooled real SheepV5 hazards. Fresh impact transfers ownership atomically before the former carrier explodes. Chains process at most two blasts per fixed step; safe delayed replenishment and respawn protection prevent recursive steals. Arena and First Person are separate menu choices. Human cab views reuse the exact Taxi/GR3D interior render meshes, with per-view exterior/interior layers and stable horizons; AI-only uses broadcast. Clear/rain/snow/blizzard/hurricane use the shared Weather Maker adapter and one coherent wind field.

Latest Keepaway presentation: an independent deterministic seed chooses rocky mud, snowfield, beach sand or meadow. Rocky mud directly reuses Cheese's Path Painter stdMudRocky albedo/normal textures; sand and grass use Vista terrain layers; snow uses installed Weather Maker/Weatherade textures. A UV-mapped rounded mesh has shallow real 3D contours (under .11m) and matching collision, with soft shadows and perimeter rocks/palms/grass. No arena resizing. Organic textured patches replace flat rectangular plates. Imported-model normalization now occurs before parenting; the previous world/local-coordinate mix displaced or undersized art relative to colliders. Actual installed crates, barrel clusters, tires, construction timber and road barriers replace visible obstacle cubes. Owned material copies retain vendor textures and use restrained wood gloss; vendor originals are untouched. Theme selection has a separate random stream and does not perturb obstacle/AI seeds. Existing physical surface-grip rules remain authoritative.

Curling: side-wall contact is now a legal physical bank; only the actual back boundary removes a stone. Side walls have .74 restitution with Maximum combine. No additional impulse is applied. Normal/Hard AI can choose a reflected bank target when blocked. The existing free-guard rollback remains authoritative.

Dock: the rim artifact was coplanar wall-top/surround geometry at y=0, not a broken material. An owned five-face wall render mesh removes the overlapping top face. Original BoxColliders, water and measurement rules remain intact in live play and replay.

Base: actual imported Transport Helicopter3911 with original Fly rotor pivots/clip, owned URP atlas material, derived rear opening, real interior/floor/ramp and four internal staging markers. Original model/material/texture files are preserved. Staggered wiggle/shuffle/hop exits share the same release altitude/velocity and individual flight clocks. AI uses persistent difficulty-dependent aiming/wind error and reaction delay; it no longer reads/cancels exact wind. Freefall tumbles transition progressively into canopy damping; camera looks down toward the destination with a stable horizon. Twenty-four pooled physical SheepV5/crates/projectiles, bounded blasts, low Weather Maker clouds, Vista terrain and installed trees/rocks give the fall scale and hazards. Landing scoring still locks on first contact.

Terrier: twelve physical NWH racers, at most four simultaneous humans, fair rotating stalls, same jump controls/strength for humans and AI, first to five. The mostly flat route has a25° diagonal heading and1.5° descent, a68m-wide start tapering to the finish. The actual physical opening is3.375m wide (25% wider than2.7),3.2m high and2.8m deep. All bounds, targets and complete-body ordered finish crossings use course coordinates. Missing a hurdle and remaining stranded causes one wreck; normal bumps/grazes do not. The genuine first finisher locks the winner, then remaining racers retain eight seconds before the heat ends. Finished trucks brake physically on a65m runout. Neutral no-finish heats rerun; repeated failures use a gentler layout.

Terrier human additions: actual Taxi interior; installed construction timber and striped barrier meshes; ten pooled animated UTS/Taxi running pedestrians with the existing jointed strike/ragdoll adapter; exact crowd/truck pose recording into collider-free replays; Weatherade accumulation and actual NWH wheel contact snow stamps via existing Taxi APIs. A Sports-owned rendering pipeline adds the installed SRS renderer; Taxi settings/pipeline are preserved. Heat radial UI, RobotoCondensed BoldItalic and DOTween show3,2,1 then GO GO GO!, with shared audio settings. Replay retains multiple angles, slow/fast playback, rewind and skip. It cannot score or change live bodies.

## Evidence recorded before the final player

These are native Editor rule fixtures and actual PhysX/software-command reproductions, not human or physical-device playtests.

- Cheese41 and Curling20 pure fixtures; Terrier9 rule fixtures; saved-scene/reference checks.
- Corrected focused physical suite: Cheese starts/brakes/reverses/steers in Clear, Blizzard and Hurricane; sideways airborne world momentum survives; a fresh real explosion clears control loss and the truck drives again. Clear3.5s acceleration travelled45.04m, Hurricane37.31m. The explosion observation returned to ground without recovery teleportation.
- Curling actual repeated banks off both walls settle without removal or added speed; a real bank takeout of an outside-house protected guard restores the previous layout and removes the illegal delivery once. Earlier failed guard reproduction was inside the house and was corrected without retuning gameplay.
- Rounded Keepaway: four actual corner glances remain inside; a full four-AI30s possession match finished with natural steals and hazards, scores14.6/4.1/30/0,4steals,8blasts,14chains. This result predates the latest textured/contoured ground and is not its final physical acceptance.
- Terrier controlled physical suite: real truck contact activates one reused jointed ragdoll; missed .65m hurdle stalls and explodes; complete truck crosses the widened opening; race continues four seconds and ends at eight after first finish exactly once; finished truck brakes and remains on runout. All13 assertions passed,0errors.
- Genuine snow GPU readback: active trace pixels8161/1048576, indented7727, peakB255/indentdelta88. A later observation recorded72active/72indented. This verifies GPU accumulation/stamping, not merely CPU stamp counters.
- Actual camera captures inspected: Taxi cab/dashboard; diagonal snowy course; four Keepaway ground materials. The initial native render exposed patch z-fighting/model sizing, which were corrected. Headless Editor ScreenCapture failed once; camera RenderTexture captures succeeded. Editor GameView stayed2048×1152 despite Screen.SetResolution, so those captures do not prove actual window layouts.
- Latest Transport staging recheck:106poses per truck,0physical cargo intersections with the current sine exit animation. Earlier12landing/3round full-match result predates new sky hazards; final new-sky acceptance is listed separately.

Earlier rectangular/narrow-door Terrier long runs were stopped when the human changed requirements. They are preserved in ignored Logs and are not completed current-revision matches.

## Final Windows validation

The twelve-sport Windows x64 Development build completed successfully:0errors,387warnings,920010883bytes,249.102s,14explicit scenes and TruckSports startup. Host settings were restored. Shader warnings include installed foliage/Weatherade URP deprecations. This is a built payload, but full current-revision physical acceptance is still pending.

The first baseline integration run passed all preceding sport transitions through Base, then found a concrete Terrier twelve-player setup COLOR caption overflow at720p. Color/difficulty/pad captions now use20max/16min type in narrow rows; saved scene and authoring defaults are corrected. This source correction postdates that build and must be included in the next build. A full twelve-AI current widened-door Terrier physical match is running against the just-built player.

The human has now requested seven additional sports, sequentially, followed by a new combined build and the full test pass. Do not describe the overall task as complete. The final tests/delivery/Git push remain outstanding.

## Vendor report

VENDOR ASSETS AUDITED: NWH WheelController/GR3D vehicle and Taxi cab; Heat/uGUI/TMP; Cinemachine; DOTween; Weather Maker; Weatherade SRS and Taxi surface/trace adapter; UTS/Taxi people/ragdoll; Transport Helicopter3911/Fly animation; Path Painter terrain layers; Vista grass/sand/palms; ConstructionPropsBundle; RoadPropsPack; IndieKit crates/barrels; JC MegaCity and Toon Gas Station scenery.

VENDOR ASSETS USED: those vehicle/input/UI/camera/weather/accumulation/animation assets and APIs, actual imported helicopter, installed prop meshes/materials/terrain textures and existing project sport/Taxi adapters.

RELEVANT ASSETS NOT USED: EasyRoads, world traffic/GPS/streaming/career persistence and direct-wheel backend do not own these isolated arcade arena rules. Path Painter/Vista layers are reused rather than running their terrain generators for a fixed rounded sports floor. Full road powertrain is unnecessary for retained lightweight NWH-wheel event trucks. No alternative helicopter model is substituted. The supplied Terrier YouTube footage was inaccessible and was not viewed.

CUSTOM SYSTEMS CREATED: owned sport rules/progression, fair course layout/source coordination, model/scene authoring adapters, ground theme selection, bounded arcade hazard semantics, faithful replay coordination and opt-in validation probes.

VENDOR SOURCE MODIFIED: None. Original vendor art is not edited; derived meshes/materials/pipelines are owned Sports assets.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None. NWH owns tire/contact behavior; Weather Maker owns atmosphere; Weatherade/Taxi own snow surface/traces; DOTween owns presentation interpolation; UTS/Taxi assets own pedestrian animation/ragdoll support.
