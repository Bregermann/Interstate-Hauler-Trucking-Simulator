# Original Generic villager animation correction

Native V78 passes all8 habitat searches, then fails the first ordinary villager at `Actual compatible owned humanoid avatar required`. Baseline: `Logs/TruckSports/PlaytestRepair/dragon08-NativeFourScenesV78.log`, around line1728. This is the first actual native execution of that incompatible assumption, not a rendering/art defect proven elsewhere.

Both `Assets/LWS/TruckTaxi/Prefabs/Pedestrians/Taxi_Girl_11.prefab` and `Taxi_Girl_22.prefab` inherit original UTS Girl/ragdoll art. Native V80 confirms their actual authored Avatar is shared from `Assets/UTS_FullPack/Models/People/Models/Girl/Girl_11.FBX`; a profile label is not proof of its Avatar filename. Original Girl11/Girl22 model import metadata are Generic (`animationType: 2`, `human: []`). Both original Taxi prefabs already reference the same vendor `Assets/UTS_FullPack/Models/People/Animations/Girl no Heel controller.controller`, GUID `5ce394841e66ad547a0da5048bfd9e64`. Its actual states are lowercase `walk` and `idle1`, referencing genuine looping Generic `Girl no Heel/walk_f.FBX` and `idle1_f.FBX`. No automatic transitions or competing controller parameters. Their real shared source bone family is Bip01. Neither a fake Human conversion nor an Avatar filename inferred from profile number is appropriate.

`SportsAudienceAuthoring.Spawn` preserves the original controller when passed null. It strips source interaction/traffic scripts, physics and NavMesh authority for decorative reuse while retaining original rig transforms/renderers. The owned ambience now uses that existing path and exact source states. No unrelated Yard humanoid controller, AvatarBuilder, retarget, invented bone mapping, source import change or art replacement.

New `DragonAmbientVillagerAuthoring.ValidateOriginalProfiles()` runs before habitat/NavMesh work and audits **both** profiles before throwing any aggregate error. Parent may execute it independently to avoid a complete scene composition. It requires a valid Generic Avatar and controller with exact object-reference equality to the actual loaded original Taxi prefab, preserves every original skin/LOD sharedMesh binding at the same relative path, verifies actual skin/Avatar model importers are Generic, and requires the original no-heel looping non-human idle/walk clips, at least15 Transform bindings and zero unresolved paths. On temporary real source-rig copies it samples24 points across each actual clip; genuine bilateral walking thigh excursion must reach5degrees on both legs. It records actual source/dependency hashes, preserved-reference proof, actual skin model paths and per-clip binding/motion evidence to `-tsDragonOutput/AmbienceArt/original-villager-rig-bindings.json`. Sampling/Editor inspection is not native live-world animation acceptance.

`SpawnOriginal` repeats exact identity/binding checks on each actual spawned villager. `DragonAmbientWalker` retains its existing valid-avatar/controller checks and ordinary navigation-driven animation crossfade, with state names configured to the vendor's `idle1`/`walk`. No check is removed without stronger same-rig evidence in its place.

`VillagerRig.SourceDependencies.json` records eleven existing candidate prefab/controller/model/animation byte identities and confirms all eleven already exist in the frozen native project; it does not claim every numbered model is used by its matching Taxi profile. Native preflight expands the actual AssetDatabase dependency closure. V80 proves Girl11 resolves all440clip bindings and actual walk leg excursion37.48°/38.93°; Girl22 then fails the earlier incorrect filename-equality check. That failure is retained, and the narrow exact-original-reference correction needs another native run. No raw asset copy/import/source edit. `VillagerRig.SourceCheck.json` records zero-diagnostic standalone Roslyn compilation. Native rerun, actual live articulation, region culling/pause/reset and human appearance review remain parent-owned and **unrun by this agent**.

VENDOR ASSETS AUDITED: both original Taxi and UTS prefabs, actual per-model Generic importer/Avatar references, original no-heel controller/states/clips, SportsAudienceAuthoring and shared passenger humanoid pipeline.

VENDOR ASSETS USED: exact original Taxi/UTS appearances, per-profile Generic avatars, same-rig idle/walk clips and original controller; existing audience spawn adapter and Unity Navigation.

RELEVANT ASSETS NOT USED: unrelated Yard humanoid controller and shared Human-conversion pipeline; neither is needed for original Generic source animation.

CUSTOM SYSTEMS CREATED: narrow explicit same-rig identity/binding/articulation preflight and correct original state configuration.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; existing source clips/controller own articulation, Unity Navigation owns motion.
