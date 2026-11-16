# Dragon Kingdom owned asset and animation audit

Date: 2026-10-28. Discovery only. Read alongside `DragonAssetAudit.json`, which records exact model/prefab/controller/material paths, bone names and all 53 animation-FBX paths with available importer clip metadata.

## Result and scope

**Confirmed:** The owned Meshtint Monsters Ultimate Pack 02 Cute Series supplies three actual skinned, animated dragon models. Dragon Inferno is the strongest adult ground/flight candidate. Dragon Fire supports flight and attacks but has a smaller motion set. Dragon Spark is a small, wingless ground dragon and the best candidate for evaluating a baby/evolution role. These are usable asset candidates, not validated playable species.

Initial discovery was read-only. A subsequent explicit lead contract authorized the isolated intake tool, private ROOT copies, catalog schema and explicit rig-animation authoring source described below. No vendor source bytes changed, no Unity Editor launched by this agent, and no build/native/runtime tests ran in this workstream. The lead owns native import/compilation, architecture, ledger integration and the serialized Editor/build slot. Existing Alpha 0.7 assets and shared source remain outside this workstream's edit scope.

Read: project AGENTS.md; current intake/acceptance record at the top of TRUCK_SPORTS_ACCEPTANCE_LEDGER.md; Vendor Asset First rule; relevant full Wyrms & Wheels source requirements, including owned discovery, shared framework, each game, sheep interactions, parallel work, validation and reporting. The audit creates no competing requirements backlog.

## Exact owned sources

- Download cache: `C:/Users/Jeremy/AppData/Roaming/Unity/Asset Store-5.x/Meshtint Studio/3D ModelsCharactersCreatures/Monsters Ultimate Pack 02 Cute Series.unitypackage`.
- Cache size: 159,429,776 bytes. SHA256: `a15850bf32f3a39c76bb38fce934caa3d8208975ed350e0d4e8eae489ffaea19`.
- The package has 823 serialized asset pathnames. Its dragon model, animation FBX, prefab, material and controller pathnames were inspected directly in the archive without importing or saving payloads.
- Already imported source copy: `F:/Unity Projects/Spirit-Bloom/Assets/Monsters Ultimate Pack 02 Cute Series/`.
- Vendor documentation inspected: each dragon's `Read Me/General Introduction.pdf` and Inferno's `Read Me/Evolve your monsters - Cute Series.pdf`. The general guide documents demo dependencies on Post Processing Stack 2, Cinemachine and legacy Standard Assets characters. Do not import those legacy demo dependencies merely to use the models. The evolution guide describes the vendor's three-evolution design, which supports evaluating Spark/Fire/Inferno as related growth stages rather than inventing unsupported elemental species.
- No connected Unity asset-library/account/download connector was available in the parent discovery. A cached licensed asset and existing local project are evidence of accessible source; this report makes no claim of account ownership enumeration or remote download access.

## Model catalog

The common prefix below is `F:/Unity Projects/Spirit-Bloom/Assets/Monsters Ultimate Pack 02 Cute Series/`. Each model folder includes its FBX model, a skinned prefab, material, PSD textures, demo scene and controller. Package version is not established by the inspected metadata.

| Model | Exact prefab suffix | Rig and bones | Confirmed animation FBXs | Candidate roles and limitations |
| --- | --- | --- | --- | --- |
| Dragon Inferno | `Dragon Inferno Cute Series/Prefabs/Dragon Inferno.prefab` | Generic; RigJaw, left/right arm palms/fingers, 8 wing bones, 8 tail bones | 26 | Adult racer, ambient patrol, attacker, mother/guardian, heist guardian after missing-action authoring. Strongest coverage. No authored sleep/wake/eat/grab/landing/takeoff clips found. |
| Dragon Fire | `Dragon Fire Cute Series/Prefabs/Dragon Fire.prefab` | Generic; RigJaw, palms/fingers, 6 wing bones, 8 tail bones | 11 | Airborne smaller adult/juvenile, stationary/perched attacker. No ground idle/walk/run FBXs; do not assign ground locomotion until a compatible real animation solution is tested. |
| Dragon Spark | `Dragon Spark Cute Series/Prefabs/Dragon Spark.prefab` | Generic; RigJaw, palms, 3 tail bones; no wing bones | 16 | Ground baby/evolution candidate: idle, walk/run, turns, jump and attacks. No flight/flapping/crawl/hatch clips. Do not advertise a flying baby from this asset. |

All three model importers serialize `animationType: 2` (Generic), scale 1 and file-scale usage. All three prefabs include an Animator and SkinnedMeshRenderer, disable root-motion application, and serialize culling mode 1. They have **no Rigidbody or collider components**. LWS-owned variants need carefully sized compound body/grip/attack colliders, stable physics authority and explicit carried-object collision filtering. Avoid deforming-mesh colliders.

Serialized local SkinnedMeshRenderer AABB extents are Inferno `(2.0037699, 1.7198706, 3.0588918)`, Fire `(1.2021081, 1.225795, 1.9174582)`, Spark `(0.70611763, 0.51364255, 0.56233454)`. These are renderer-local stored bounds, **not measured world dimensions or proof of final truck-relative scale**. World bounds, all animated extrema, saddle support, camera clearance and ground contact require actual imported/rendered measurement.

All three materials reference Unity built-in shader fileID 46, with PSD source textures. Inferno has an additional emission PSD and emission color. ROOT is Unity 6000.4.10f1 with URP 17.4.0 in manifest and an assigned render-pipeline asset. Source material compatibility is unverified; create project-owned URP material variants preserving albedo/emission mappings, and inspect the rendered result. Do not modify source materials or assume the historical demo is compatible.

## Exact clip coverage

Every name below is the suffix of `<Dragon name>@<name>.FBX` under that model's `FBX/` directory. The full paths and available take/frame/loop settings are in the JSON. Existence and metadata are confirmed; actual deformation/playback quality remains unverified.

**Inferno (26):** Bite Attack; Cast Spell; Die; Fly Bite Attack; Fly Cast Spell; Fly Die; Fly Forward In Place; Fly Forward W Root; Fly Idle; Fly Projectile Attack; Fly Take Damage; Fly Turn Left; Fly Turn Right; Fly Wind Attack; Idle; Jump In Place; Jump W Root; Projectile Attack; Run Forward In Place; Run Forward W Root; Tail Attack; Take Damage; Turn Left; Turn Right; Walk Forward In Place; Walk Forward W Root.

**Fire (11):** Bite Attack; Cast Spell; Die; Fly Forward In Place; Fly Forward W Root; Fly Idle; Projectile Attack; Stomp Ground Attack; Take Damage; Turn Left; Turn Right.

**Spark (16):** Bite Attack; Cast Spell; Die; Head Attack In Place; Head Attack W Root; Idle; Jump In Place; Jump W Root; Projectile Attack; Run Forward In Place; Run Forward W Root; Take Damage; Turn Left; Turn Right; Walk Forward In Place; Walk Forward W Root.

Inferno's Idle/Fly Idle/Walk Forward In Place serialize looping 0-40 frames; Fly Forward In Place loops 0-30. Fire's Fly Idle loops 0-30 and flight-forward loops 0-25. Spark Idle/Walk loops 0-30. Some attack FBX importers use default imported takes rather than an explicit clip array; a missing clip array does not mean an empty animation.

Vendor controllers have empty parameter arrays and many repeated idle/flight states. They are demo-controller assets, not an established gameplay state machine. Keep source controllers intact; use supported AnimationClip references in a project-owned Animator/controller/override integration with the audited creature controller's required parameters and state tags. Do not leave demo automatic transitions in control of gameplay.

## Missing-action decisions required before integration

| Required action | Available foundation | Honest remaining work |
| --- | --- | --- |
| Ground idle/look/walk/run/turn | Inferno/Spark Idle, Walk, Run, Turn; generic bones | Configure blending, speeds, ground contact and look-at. Fire has no ground locomotion set. |
| Flapping flight/turns/air attacks | Inferno richest flight set; Fire flight idle/forward | Render each clip, verify actual flap cycle, map bank/pitch and synchronize real flight movement. |
| Takeoff/landing/perch/glide/dive | Fly clips, Inferno jump, wing and leg bones; Malbers Fly mechanics | No dedicated named clips found. Compose/author convincing compatible animation and IK/secondary pose transitions, then observe it. A mechanical landing or slowed idle alone is insufficient evidence. |
| Sleep/stir/wake/roar | Inferno ground idle, jaw/chest/wing/tail bones; attack poses | No dedicated named clips found. Real sleep breathing and animated rising/roaring are outstanding. Cannot claim Heist sleeping/waking acceptance from these assets alone. |
| Breath and elemental attack | Cast/Projectile attack and real jaw; vendor breath particles | Check pose timing; attach VFX at mouth, synchronize jaw/head and actual hit detection. No confirmed ice/lightning/forest model species; materials alone do not establish a distinct species. |
| Claw grab/carry/drop/eat sheep | Palms/fingers, jaw, Inferno Fly Bite/Bite; Malbers pickup APIs and generic IK | No dedicated grab/carry/eat clips. Author/retarget compatible grip and mouth sequence; test visible attachment and collision safety. Do not count simple parenting as animation. |
| Eggs/nest/baby hatch | Spark animated small ground model; Malbers DragonEgg logic | No Hatch/inspect/nest clips or dragon egg model found in this pack. Vendor DragonEgg assumes matching egg Animator and creature Hatch/out-of-egg states. Those assets/states are not supplied by these three dragons. |
| Damage/defeat/retreat | All have Take Damage/Die; Inferno airborne equivalents | Configure only roles with a convincing clip; retreat can use real walking/flight animation. |

These are the discovered original-asset gaps. The later explicit authoring source below prepares distinct project-owned bone animations for compatible rigs; it has not been executed or visually verified by this agent. Original missing-clip statements are retained, and original source assets remain unchanged.

## Vendor controller foundation: Malbers Animal Controller

Existing installed source: `F:/Unity Projects/House-Cat-Simulator/Assets/Malbers Animations/`.

Owned cached package: `C:/Users/Jeremy/AppData/Roaming/Unity/Asset Store-5.x/MalberS Animations/Editor ExtensionsAnimation/Animal Controller Malbers Character Controller.unitypackage` (71,338,399 bytes). Archive pathname inspection confirms Fly, MAnimalAIControl, MPickUp, Pickable, DragonEgg, FireBreath, effects and assembly definitions. No dragon model prefab was found in that controller package's dragon-named paths. Existing animal directories include Wolf Lite and Pets; they do not supply a dragon model.

Relevant public API/code evidence, relative to the Malbers root:

- `Common/Scripts/Animal Controller/States/Fly.cs`: `Fly` state; Bank, PitchLimit, InertiaLerp, takeoff Impulse/ImpulseTime/ImpulseCurve, canLand/LandOn/LandMultiplier, AvoidSurface/SurfaceDistance, FlapSpeed/GlideSpeed/AutoGlide, gravity drag/down acceleration. Public `SetAvoidSurface(bool)` and state lifecycle methods support configuration/adaptation. It expects the correct Animator tags and blend parameters.
- `Common/Scripts/Animal Controller/MAnimalCallBacks.cs`: `Move(Vector3)`, `State_TryActivate(int)`, `State_Activate(int/StateID)`, `Mode_TryActivate(...)`, `Mode_Activate(...)`, speed-set APIs.
- `Common/Scripts/Animal Controller/MAnimalAIControl.cs`: `SetTarget(Transform/GameObject)`, `SetDestination(Vector3)`, `ClearTarget()`, `Move()`, `Stop()`, free-movement air target support and ordinary ground NavMesh integration.
- **Important missing capability:** `MAnimalAIControl.FreeMovement()` explicitly performs no obstacle calculation; it steers straight at the destination. `Fly.AvoidSurface` is a surface/landing check, not a complete three-dimensional path planner. LWS must provide kingdom-specific aerial route/clearance/intercept decisions around towers, cliffs and moving trucks, through the vendor movement API. Do not claim the vendor already solves full aerial avoidance.
- `Common/Scripts/Interactions/MPickUp.cs`: Holder, extraHolders, TryPickUp(), TryDrop(), PickUpItem(), DropItem(), and pickup/drop events.
- `Common/Scripts/Interactions/Pickable.cs`: Pick(), Drop(), ForceDrop(), kinematicOnPick and reaction/events. Reuse where compatible with existing sheep authority; do not create competing ownership or double-trigger explosive sheep.
- `Common/Scripts/IK/IK Processors/IK Generic/IKGenericLookAt.cs` and existing IKSet/IKProfile/IKReaction provide a generic-rig look-at foundation. They do not automatically supply missing full-body action animations.
- `Common/Scripts/Animal Controller/DragonEgg.cs`: SetDragon(GameObject), CrackEgg(), OnEggCrack. Requires compatible egg State1/2 Animator states and creature Hatch/out-of-egg setup. It destroys the egg collider on crack; a pooling/reset adapter must restore required lifecycle state and should not assume this demo logic is immediately reusable unchanged.
- `Common/Scripts/Effects/FireBreath.cs`: Activate(bool), FireBreathColor(Color/ColorVar). Controls emission/color only; **no damage/hit detection** in this component. LWS combat must retain actual collision/cast-based interactions and bounded effects.

Both the House-Cat copy and the actual owned cached package contain `CustomPatch` comments in MAnimalAIControl. The audit did not author them. The cache is an exact owned-cache baseline, not proven pristine upstream vendor source; its package hash records provenance. Compare dependencies before selecting a scoped import. `Common/Scripts/MalbersAnimations.asmdef` has seven GUID-based references, with separate Editor/InputSystem/Cinemachine assemblies. Importing an isolated Fly.cs would omit required framework types and is unsuitable. Dependency and compilation validation remain the lead's serialized task.

## Owned VFX and audio

The Meshtint pack has **zero audio files, C# scripts or named VFX assets** in the imported pack tree. Species names do not establish a complete elemental-effects system.

Audited Malbers paths under House-Cat `Assets/Malbers Animations/Common/`:

- `Particles/Prefabs/FireBreath.prefab`, `FireBreath Hard.prefab`, `FireBreath 1.prefab`, `FireBall.prefab`, `FireBall Lite.prefab`, `Smoke.prefab`, `Hit Effect Fire.prefab`, `Big Dragon Dust.prefab`, `Big Dragon Track.prefab`.
- `Audio/Fire/Fire Explosion 1.wav`, `Fire Explosion 2.wav`, `Fire Shake.wav`, `Fire Throw.wav`.
- Existing `Prefabs/Weapons/Projectiles/FireBall.prefab` provides a further projectile candidate; exact damage/pooling compatibility was not audited in this phase.
- FireBreath's saved ParticleSystem is looping with a 1000-particle maximum and scaling mode 2. World scaling, URP shader compatibility, camera visibility and total encounter budget must be measured after integration.

Additional local owned sources:

- `F:/Unity Projects/UnityAssetPackages/Unity Asset Packages/Assets/Plugins/Epic Sounds and FX/Fantasy_Game_16bit/Fantasy_Game_16Bit/Attacks_and_Creatures/` contains Creature_Longer_Low_Growl, Creature_Growl_Loud_Gritty and many other creature growls; representative exact filenames are readily available. These are creature-audio candidates, not listened-to dragon roars.
- `F:/Unity Projects/Asset Playground/Assets/Fantasy_Game_16bit/Attacks_and_Creatures/` contains another imported copy of those fantasy creature sounds.
- `F:/Unity Projects/UnityAssetPackages/Unity Asset Packages/Assets/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/FX/P_FX_fire.prefab` provides environmental fire.
- ROOT already contains Weather Maker lightning presentation and licensed VFX under established sport-owned folders. This workstream did not modify them. Reuse should respect the existing owner/baseline and copy required licensed dependencies into the new private area under the lead's integration policy.

No listening, VFX playback, shader verification, damage or performance acceptance is claimed.

## Bounded discovery coverage

Inspected 79 cached Asset Store package filenames; examined Meshtint and Malbers package pathnames in depth. Targeted model/prefab searches covered ROOT Assets, UnityAssetPackages' Plugins collection, Asset Playground Assets, House-Cat Assets, Spirit-Bloom Assets, OmniSpire Assets, Clive the Cat Demo Assets and Jams. Inspected immediate Third Party Assets/Assets collection directories and Downloads package/archive names. The supplied Dog-Game path has no direct Assets child and was not recursively searched further. No unbounded all-F scan was run.

ROOT's named dragon prefab is the existing Taxi purple-dragon passenger; it is not a validated animated Kingdom dragon foundation. Other common findings were icon art, Malbers dust/track effects and generic monster audio. No additional suitable dragon model pack was found in the bounded sources. This is not an exhaustive claim about every asset on every drive.

## Integration recommendation

Start with original cached Meshtint Inferno plus Spark/Fire as real asset candidates, and scoped original Malbers runtime/controller dependencies behind an LWS adapter. Use Inferno for the first adult ground/flight validation and Spark for ground baby validation. Keep model-specific state maps, animation overrides, URP materials, colliders, grip/mouth/saddle anchors and species configuration in new Dragon Kingdom-owned assets. Keep source FBXs, rigs, textures and vendor code intact.

Before claiming a supported species/action: import under the agreed private licensed root; resolve assemblies; measure model/world bounds; inspect actual bone deformation and clip loops; validate grounded feet, flap/turn/attack timings, offscreen/onscreen culling, mounted truck and claws; author convincing missing actions; verify actual rendered 720p/1080p and split-screen behavior. Human playtest/listening remains separate.

## Required vendor report

- **VENDOR ASSETS AUDITED:** Meshtint Monsters Ultimate Pack 02 Cute Series; Malbers Animal Controller installed source and cached package; Epic Sounds and FX Fantasy Game audio; Tidal Flask fire; ROOT vendor lighting/VFX presence. Exact paths/classes are above.
- **VENDOR ASSETS USED:** Exact three-dragon model/53-motion closures, 515 unchanged Malbers runtime C# files plus official state IDs/Empty Controller configs, three complete FireBreath prefab closures, five vendor fire WAVs and three Epic Sounds creature WAVs copied to ignored ROOT folders. Runtime integration/native acceptance belongs to the lead and motor workstream.
- **RELEVANT ASSETS NOT USED:** Meshtint Fire for ground locomotion and Spark for flight lack the relevant named animations/rig support. Malbers optional Cinemachine 3/new-input integrations and Editor C# are omitted; existing Sports cameras/input remain authoritative. Broken optional projectile/track effects and conflicting smoke effects are excluded below. Malbers raw demo egg/controller and straight-line air AI require compatibility and missing behavior work. Existing Taxi purple-dragon is not established as an animated kingdom rig. Legacy demo dependencies are unnecessary for model-only import.
- **CUSTOM SYSTEMS CREATED:** Dependency-safe owned intake tool; species configuration/validation schema; explicit Editor catalog and real-rig animation authoring source. No custom vehicle physics, replacement creature locomotion, camera/input/weather framework or fabricated dragon model.
- **VENDOR SOURCE MODIFIED:** NO.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO.

## Authorized ROOT intake and byte verification

Tool: `Tools/TruckSports/ImportDragonKingdomAssets.py`. Dry-run is the default; `--apply` is required to copy. It does not touch CLEAN, Git refs, package manifests/settings or another sport. It validates archive path containment, preserves original GUIDs and payload bytes, computes actual dependency closures, checks ROOT/package GUIDs, enforces Git-ignored destinations, refuses differing existing files and verifies every copied hash. The project-owned assembly configuration is generated outside the retained vendor source tree.

Destinations:

- `Assets/TruckDragonKingdom/PrivateLicensedDragons/Monsters Ultimate Pack 02 Cute Series/<Dragon name> Cute Series/`.
- `Assets/TruckDragonKingdom/PrivateLicensedAnimalController/Malbers Animations/`.
- `Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/EpicSounds/`.

Preserved source: Meshtint cache SHA256 `a15850bf32f3a39c76bb38fce934caa3d8208975ed350e0d4e8eae489ffaea19`; Malbers cache SHA256 `390f0b3a0ba495aa18ff9dd7ba6ad0ccaa063dbfd1d4f0e2d5e3e9b7e1505b8e`. The Malbers cache itself contains existing `CustomPatch` comments; no pristine-upstream claim is made and no patch was authored here.

Exact exclusions and reasons:

- `Common/Cinemachine/` and `Common/Input System/`: optional integrations use Cinemachine 3 while ROOT owns Cinemachine 2.10.7 and Sports input. They are not required by the retained movement API.
- `Common/Scripts/Editor/` C# is omitted. Referenced `Editor/Icons/` images are included to preserve MonoImporter icon dependencies without the optional Editor code.
- Original package `Packages/manifest.json` is never copied. Original `.asmdef` files are omitted. `PrivateLicensedAnimalController/LWS.DragonKingdom.MalbersRuntime.asmdef` is project-owned configuration named `MalbersAnimations`, referencing installed Mathematics, AI Navigation, TMP, Splines, Input System, Timeline and UnityEngine.UI by assembly name. The original unresolved GUID `7f7d1af65c2641843945d409d28f2e20` is not silently retained. Native compilation of this configuration remains the lead's validation step.
- `Smoke.prefab` and `Big Dragon Dust.prefab`: their Smoke_Black texture shares GUID `830884c18cf5e7a4895ee02c2dcd8e12` with existing Weather Maker SmokeTexture, but bytes and decoded pixels differ. Neither source nor Weather Maker is overwritten/remapped; the optional effects are excluded.
- `FireBall.prefab`, `FireBall Lite.prefab`, `Hit Effect Fire.prefab`: three referenced source textures are missing from the owned cache (`3bc01580ce4f1834eb3562397607f51d`, `f343d204ab60b284597aa1742824864c`, `10ff51d2d87fb7b46b70b55f8551c146`). `Big Dragon Track.prefab` references missing material GUID `738c85d96cbc784429fc0490b99c38a4`. These broken optional originals are excluded.
- Complete `FireBreath.prefab`, `FireBreath Hard.prefab`, `FireBreath 1.prefab` closures are retained. They still require URP/rendered validation and real LWS hit detection.

Successful final dry-run: **1,733 files / 102,608,502 bytes; zero unresolved GUIDs, zero omitted required references, zero preflight conflicts**. First `--apply` copied and verified the plan. Repeated `--apply` exited zero with `copyVerified: true`, `copiedFiles: 0`, `alreadyExactFiles: 1733`. A separate final rehash again checked all 1,733 files with zero mismatches. Counts include folder metadata and the LWS assembly config: 515 unchanged vendor C# files, 56 FBXs (three models plus 53 motions), eight WAVs and the remaining original dependency closures. No existing ROOT asset was reused through a colliding GUID.

Receipts: `DragonAssetAudit.IntakeDryRun.json`, `DragonAssetAudit.IntakeFirstApply.json`, final `DragonAssetAudit.Intake.json`. Earlier preflight failures (Windows output decoding, optional inspector-icon exclusions, real smoke collision and missing optional VFX dependencies) caused no asset writes; final successful evidence is separate from those rejected plans. The Python importer syntax was parsed successfully. No native/visual/human result is inferred from byte checks.

## Catalog and explicit animation authoring source

`Assets/TruckDragonKingdom/Scripts/DragonSpeciesCatalog.cs` defines stable IDs, real visual prefab, Ground/Flying/Baby/RiderMount/HoardGuardian/EggGuardian/SheepCarrier flags, scale/handling/element/stamina/temperament, jaw/grip/saddle bone names, breath/audio references, semantic action-to-AnimationClip mappings and role-required validation. Empty/duplicate IDs, invalid movement values, missing visual/required clips and duplicate mappings remain visible errors. It does not treat an unmapped action as a successful idle fallback.

`Assets/TruckDragonKingdom/Editor/DragonSpeciesAuthoring.cs` exposes explicit `DragonSpeciesAuthoring.CreateCatalog()` returning the catalog at `Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset`. A separate explicit menu command invokes it. There is no import callback, runtime generation or automatic scene rebuild. The lead has not yet executed this source in this workstream's evidence.

The source wires `dragon-inferno`, `dragon-fire`, `dragon-spark` to actual original prefabs and loads actual AnimationClip subassets from named FBX paths (not assumed clip-name equality). It links the retained FireBreath and real fire/creature audio. Original idle/walk/run/turn/flight/spell/tail/damage/defeat/retreat mappings remain references to owned clips. Render quality and correct contextual combat layering are not inferred.

Project-owned authored clips are prepared only by explicit native authoring under `Assets/TruckDragonKingdom/Animations/Authored/<species.id>/<Action>.anim`. Every such clip copies the actual source's motion curves, retimes them, then samples and overlays real quaternion bone curves at 30 Hz. Euler tracks for the same edited bone are removed before quaternion replacement, and quaternion continuity is enforced. No vendor source clip is modified. Required actual bones and source motions are checked; unsupported actions remain absent and role validation remains responsible for rejecting those configurations.

| Authored action | Compatible source/rig | Actual authored changes, pending native/rendered verification |
| --- | --- | --- |
| Sleep / Wake | Inferno and Spark ground Idle, actual pelvis/neck/head/spine/limbs/tail | Curled head/neck, bent legs/arms, tail curl, lowered pelvis and chest/wing breathing; Wake unfolds that pose and includes a jaw stretch. Sleep is not an Idle alias. |
| Takeoff / Land | Inferno/Fire flight; actual Inferno Idle or Fire Stomp grounded endpoint | Ground-to-flight and flight-to-ground sampled bone/pelvis blending with opposite asymmetric crouch/wing timing. Omitted if a real grounded reference is absent. |
| Glide | Winged Inferno/Fire real flapping clip | Samples 24 actual wing phases, holds the widest real wing pose with gentle flutter while retaining animated body/tail source motion. |
| BankLeft / BankRight / Dive | Winged Inferno/Fire flight | Asymmetric wings, neck/tail counterposes and wing tuck over actual flight; motor owns physical attitude/pathing. |
| Grab / Carry / Drop / ClawAttack | Winged Inferno/Fire, real arm/palm/finger bones | Reach, curl, held grip and release/slash curves over retained flight. Runtime owns target validation, attachment and hit timing. |
| Eat / Roar | Actual Bite/Cast source, jaw/head/neck and appropriate arms | Bite source plus hand-to-mouth/neck/jaw cycles; roar includes raised head, open jaw and chest/wing motion. No sheep disappearance/scoring is authored here. |
| InspectEgg / Nest / Perch | Grounded Inferno/Spark actual Idle | Head/neck dip and scanning, tail and arm secondary motion; egg/nest physics remain separate. |
| Hatch | Spark ground/baby rig and actual Jump In Place | Curled limbs/head/tail wiggle and progressive unfolding over real jump motion. Shell sequence/visibility/physical retention remain game integration responsibilities. |
| LookAround | Each model's actual Idle or Fly Idle | Real neck/head scanning and tail secondary animation. |

Fire remains unsupported for ordinary ground idle/walk/run, sleeping, nesting or hatching. Spark remains unsupported for flight/mount/aerial carrying. Only Inferno is eligible for the authored adult Hoard/Egg guardian roles; Spark stays a ground baby, and Fire remains an airborne adult/mount/carrier candidate. Authored advanced-role flags denote complete configured mappings, not verified gameplay or anatomy/comfort acceptance.

**Outstanding:** native compilation and execution of authoring; imported clip/bone binding; actual sleep silhouette, wake rise, ground contact and transition continuity; jaw axis/limb reach; convincing grip-to-mouth sequencing; authored hatching with an actual shell; URP materials; scaled world dimensions/colliders; actor/motor integration; actual rendered 720p/1080p/split-screen and human/listening acceptance. The lead owns these tests and acceptance updates. No test/visual verification was fabricated.
