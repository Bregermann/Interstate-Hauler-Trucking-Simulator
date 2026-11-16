#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.CoreCollapse.EzySlice;
using LWS.TruckDragonKingdom;
using LWS.TruckDragonKingdom.Editor;
using LWS.TruckJenga;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckHoardHeist.Editor
{
    public static class HoardHeistAuthoring
    {
        public const string Folder = "TruckHoardHeist";
        public const string ScenePath = "Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity";
        const string Derived = "Assets/TruckHoardHeist/PrivateLicensedPotteryFragments";
        const string HeistAudio = Derived + "/HeistAudio/";
        static string Village => DragonKingdomWorldAuthoring.Village;

        [MenuItem("Truck Sports/Alpha 0.8/Create NEW Dragon Hoard Heist")]
        public static void CreateNewScene()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Preserve the existing authored Heist scene.");
            var match = DragonKingdomModeAuthoring.Begin<HoardHeistMatch>(Folder, 1, new[] { "dragon-inferno" });
            Install(match); DragonKingdomModeAuthoring.Finish(match, Folder);
        }

        public static void Install(HoardHeistMatch match)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || match == null || match.world == null || match.trucks.Length != 4 || match.dragons.Length < 1 || match.transform.Find("Gilded Deep physical heist") != null)
                throw new InvalidOperationException("Stopped, compiled, new shared-kingdom Heist composition required.");
            foreach (string folder in new[] { "Materials", "Models", "Scenes" }) Directory.CreateDirectory("Assets/" + Folder + "/" + folder);
            Directory.CreateDirectory(Derived); AssetDatabase.Refresh();
            var root = new GameObject("Gilded Deep physical heist").transform; root.SetParent(match.transform, false);
            var region = match.world.Region("caverns");
            if (region.treasureCaches.Length == 0 || region.perches.Length == 0) throw new InvalidOperationException("Actual authored cavern hoard and sleeping perch required.");
            match.guardianHome = region.perches[0];
            match.dragons[0].transform.SetPositionAndRotation(match.guardianHome.position, Quaternion.Euler(0, 180, 0));
            var guardianBody = match.dragons[0].motor.body;
            guardianBody.position = match.dragons[0].motor.transform.position; guardianBody.rotation = match.dragons[0].motor.transform.rotation;
            ConfigureSleepingVigil(match);
            var gold = SportsEventAuthoring.Mat(Folder, "Gilded ancient treasure gold", new Color(.94f, .62f, .08f), .78f); gold.SetFloat("_Metallic", .92f);
            var bronze = SportsEventAuthoring.Mat(Folder, "Ancient bronze relief", new Color(.43f, .21f, .065f), .42f); bronze.SetFloat("_Metallic", .72f);
            var iron = SportsEventAuthoring.Mat(Folder, "Quiet cargo equipment steel", new Color(.12f, .15f, .17f), .45f); iron.SetFloat("_Metallic", .65f);
            var clay = SportsEventAuthoring.Mat(Folder, "Broken ceramic interior", new Color(.64f, .3f, .12f), .13f);
            var contact = SportsEventAuthoring.Contact(Folder, "Treasure and cargo contact", .7f, .06f);
            GoldMountain(root, match.guardianHome.position - Vector3.up * 2, gold, contact);
            var goods = new List<HoardTreasure>();
            Vector3 hoard = region.treasureCaches[0].position;
            for (int i = 0; i < 16; i++)
            {
                int kind = i % 7;
                Vector3 position = hoard + new Vector3((i % 4 - 1.5f) * 13, 0, (i / 4 - 1.5f) * 7);
                float floor = Ground(position, region.root.position.y);
                var itemRoot = new GameObject(new[] { "Royal coin stack", "Cast gold bars", "Banded treasure chest", "Valuable supply crate", "Enchanted gemstone", "Ancient jeweled crown", "Royal bronze amphora" }[kind]);
                itemRoot.transform.SetParent(root, false); itemRoot.transform.position = new Vector3(position.x, floor + 1, position.z);
                var visual = new GameObject("Actual treasure model").transform; visual.SetParent(itemRoot.transform, false);
                if (kind == 0) Coins(visual, gold);
                else if (kind == 1) Bars(visual, gold);
                else if (kind == 2 || kind == 3)
                {
                    Owned(visual, Village + "props/P_PROP_crate_01.prefab", 1.2f);
                    if (kind == 2)
                        for (int side = -1; side <= 1; side += 2) Part(visual, "Iron treasure band", PrimitiveType.Cube, new Vector3(side * .37f, 0, 0), new Vector3(.055f, 1.05f, 1.24f), gold);
                }
                else if (kind == 4) Owned(visual, DragonKingdomWorldAuthoring.Art + "Tarbo Studio/TARBO-TowerDefensePack/Prefabs/Crystals/TB_Str_Crystal_A.prefab", 1.1f);
                else if (kind == 5) Owned(visual, "Assets/TruckSports/Customization/Accessories/hat.crown.prefab", 1.1f);
                else PartMesh(visual, "Detailed ancient amphora", Lathe("Amphora", new[] { new Vector2(0,-.6f), new Vector2(.26f,-.58f), new Vector2(.34f,-.42f), new Vector2(.42f,-.05f), new Vector2(.4f,.18f), new Vector2(.23f,.38f), new Vector2(.16f,.48f), new Vector2(.24f,.52f), new Vector2(.24f,.6f), new Vector2(.16f,.6f), new Vector2(.14f,.49f), new Vector2(.2f,.38f), new Vector2(.34f,.12f), new Vector2(.3f,-.4f), new Vector2(0,-.45f) }), bronze);
                Bounds bounds = BoundsOf(visual.gameObject);
                visual.position += itemRoot.transform.position - bounds.center;
                bounds = BoundsOf(visual.gameObject);
                var shape = itemRoot.AddComponent<BoxCollider>(); shape.size = bounds.size + Vector3.one * .025f; shape.sharedMaterial = contact;
                var body = itemRoot.AddComponent<Rigidbody>(); body.mass = new[] { 32, 105, 85, 65, 28, 18, 45 }[kind]; body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.solverIterations = 16; body.maxDepenetrationVelocity = 3;
                var cargo = itemRoot.AddComponent<HoardTreasure>(); cargo.match = match; cargo.index = i; cargo.stableId = "gilded-deep.treasure." + i.ToString("00");
                cargo.value = new[] { 90, 260, 220, 120, 300, 280, 180 }[kind]; cargo.body = body; cargo.shape = shape;
                cargo.clink=itemRoot.AddComponent<AudioSource>();cargo.clink.playOnAwake=false;cargo.clink.spatialBlend=1;cargo.clink.maxDistance=85;
                cargo.clinkClip=RequiredAudio(HeistAudio+(i%2==0?"Fantasy_Game_Organic_Coin_Collect_A.wav":"Fantasy_Game_Organic_Coin_Collect_B.wav"));
                cargo.seedPosition = new Vector3(position.x, floor + shape.size.y * .5f + .08f, position.z); cargo.seedRotation = Quaternion.Euler(0, (i * 47) % 360, 0);
                body.position = cargo.seedPosition; body.rotation = cargo.seedRotation; goods.Add(cargo);
            }
            match.treasure = goods.ToArray();
            match.hoists = new JengaCrane[4]; match.cargoRacks = new BoxCollider[4]; match.hoistRigs = new HoardHoistRig[4];
            for (int i = 0; i < 4; i++) ConfigureCargo(match, i, iron, contact);

            // The actual eastern cavern opening exits onto its authored shared
            // road junction. Reception lies outside the rock ring, not in it.
            Vector3 exit = region.root.TransformPoint(new Vector3(185, 0, 0)); float exitFloor = Ground(exit, region.root.position.y);
            var reception = new GameObject("Outside cavern treasure reception"); reception.transform.SetParent(root, false); reception.transform.position = new Vector3(exit.x, exitFloor + 9, exit.z);
            match.extractionZone = reception.AddComponent<BoxCollider>(); match.extractionZone.isTrigger = true; match.extractionZone.size = new Vector3(52, 18, 56);
            var approach = new GameObject("Hoard extraction road goal").transform; approach.SetParent(root, false); approach.position = new Vector3(exit.x, exitFloor, exit.z); match.extractionApproach = approach;
            foreach (int side in new[] { -1, 1 })
            {
                var label = SportsEventAuthoring.Label("Received treasure depot", "ROYAL TREASURE RECEPTION", approach.position + new Vector3(0, 3, side * 29), .28f, new Color(1, .85f, .35f)); label.transform.SetParent(root, true);
            }
            var debris = new List<Rigidbody>();
            for (int i = 0; i < 6; i++)
            {
                var rock = new GameObject("Bounded real loose cavern rock " + i); rock.transform.SetParent(root, false);
                rock.transform.position = hoard + new Vector3((i % 3 - 1) * 16, 72 + i % 2 * 3, i < 3 ? -17 : 17);
                Owned(rock.transform, DragonKingdomWorldAuthoring.Cliff + "RockFormation_02.prefab", 2.3f);
                var bounds = BoundsOf(rock); var box = rock.AddComponent<BoxCollider>(); box.center = rock.transform.InverseTransformPoint(bounds.center); box.size = bounds.size; box.sharedMaterial = contact;
                var body = rock.AddComponent<Rigidbody>(); body.mass = 90; body.isKinematic = true; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; debris.Add(body);
            }
            match.caveDebris = debris.ToArray();
            var pots = new List<HoardPottery>();
            for (int i = 0; i < 8; i++) pots.Add(Pottery(match, root, hoard + new Vector3((i % 4 - 1.5f) * 13, 0, i < 4 ? -14 : 14), i, clay, contact));
            match.pottery = pots.ToArray();
            // Real opaque stone cover on either side of the navigable road;
            // its geometry participates in the shared actor's LOS raycasts.
            for (int i = 0; i < 4; i++)
            {
                var cover = new GameObject("Actual cave stealth column " + i); cover.transform.SetParent(root, false);
                var p = hoard + new Vector3((i - 1.5f) * 24, 0, 20); p.y = Ground(p, region.root.position.y) + 6; cover.transform.position = p;
                var model = Owned(cover.transform, DragonKingdomWorldAuthoring.Cliff + "RockFormation_01.prefab", 12);
                Bounds b = BoundsOf(model); model.transform.localScale = Vector3.Scale(model.transform.localScale, new Vector3(7 / Mathf.Max(.01f,b.size.x), 12 / Mathf.Max(.01f,b.size.y), 7 / Mathf.Max(.01f,b.size.z)));
                b = BoundsOf(model); model.transform.position += cover.transform.position - b.center;
                var collider = cover.AddComponent<BoxCollider>(); collider.size = new Vector3(7,12,7); collider.sharedMaterial = contact;
                var shelter=new GameObject("Actual quiet cover goal "+i).transform;shelter.SetParent(root,false);
                Vector3 shelterPosition=hoard+new Vector3((i-1.5f)*24,0,8);shelterPosition.y=Ground(shelterPosition,region.root.position.y);shelter.position=shelterPosition;match.quietCoverGoals[i]=shelter;
            }
            var rumble = new GameObject("Sleeping dragon low breathing rumble"); rumble.transform.SetParent(match.dragons[0].transform, false);
            match.sleepRumble = rumble.AddComponent<AudioSource>(); match.sleepRumble.playOnAwake = false; match.sleepRumble.spatialBlend = 1; match.sleepRumble.maxDistance = 130; match.sleepRumble.volume = .18f; match.sleepRumble.pitch = .5f;
            match.sleepRumble.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/EpicSounds/Fantasy_Game_Creature_Longer_Low_Growl.wav");
            match.awakeningRoar=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/EpicSounds/Fantasy_Game_Creature_Growl_Loud_Gritty.wav");
            EditorUtility.SetDirty(match);
        }

        public static void RefreshSleepingVigilAfterRestReview(HoardHeistMatch match)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||match==null||EditorUtility.IsPersistent(match))
                throw new InvalidOperationException("Explicit stopped scene vigilance refresh required after reviewing the updated base Sleep pose.");
            ConfigureSleepingVigil(match);
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<AnimationClip>(Derived+"/HeistMotion/Curled sleeping vigilance.anim"));
            AssetDatabase.SaveAssetIfDirty(match.dragons[0].motor.animator.runtimeAnimatorController);
            EditorUtility.SetDirty(match);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
        }

        static void ConfigureSleepingVigil(HoardHeistMatch match)
        {
            if(!match.species.TryGet(match.dragonSpeciesIds[0],out var species)||!species.TryGetAnimation(DragonAnimationAction.Sleep,out var sleep)||!species.TryGetAnimation(DragonAnimationAction.LookAround,out var look))
                throw new InvalidOperationException("Real authored sleep and look motions required for curled vigilance.");
            var eyelids=match.dragons[0].motor.GetComponent<DragonEyelidPresentation>();
            if(eyelids==null)throw new InvalidOperationException("Install the reviewed actual Inferno eyelids before authoring Heist sleeping vigilance.");
            eyelids.ValidateBindings();
            string folder=Derived+"/HeistMotion";Directory.CreateDirectory(folder);Directory.CreateDirectory("Assets/"+Folder+"/Controllers");AssetDatabase.Refresh();
            var model=UnityEngine.Object.Instantiate(species.visualPrefab);model.hideFlags=HideFlags.HideAndDontSave;model.SetActive(false);
            AnimationClip clip;
            try{clip=DragonSpeciesAuthoring.CreateRigActionClip(model,sleep.clip,DragonAnimationAction.LookAround,4,false);}
            finally{UnityEngine.Object.DestroyImmediate(model);}
            clip.name="Heist curled sleeping vigilance";string path=folder+"/Curled sleeping vigilance.anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing==null)AssetDatabase.CreateAsset(clip,path);else{EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(existing);}
            var animator=match.dragons[0].motor.animator;RuntimeAnimatorController baseController=animator.runtimeAnimatorController;
            var retained=new List<KeyValuePair<AnimationClip,AnimationClip>>();
            if(baseController is AnimatorOverrideController current){current.GetOverrides(retained);baseController=current.runtimeAnimatorController;}
            while(baseController is AnimatorOverrideController nested)baseController=nested.runtimeAnimatorController;
            var replacement=new AnimatorOverrideController(baseController);if(retained.Count>0)replacement.ApplyOverrides(retained);
            var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();replacement.GetOverrides(overrides);bool matched=false;
            for(int i=0;i<overrides.Count;i++)if(overrides[i].Key==look.clip){overrides[i]=new KeyValuePair<AnimationClip,AnimationClip>(overrides[i].Key,clip);matched=true;}
            if(!matched)throw new InvalidOperationException("Actual guardian controller has no configured LookAround motion to override.");
            replacement.ApplyOverrides(overrides);string controllerPath="Assets/"+Folder+"/Controllers/Heist guardian sleeping vigilance.overrideController";
            var saved=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
            if(saved==null){AssetDatabase.CreateAsset(replacement,controllerPath);saved=replacement;}else{EditorUtility.CopySerialized(replacement,saved);UnityEngine.Object.DestroyImmediate(replacement);EditorUtility.SetDirty(saved);}
            animator.runtimeAnimatorController=saved;EditorUtility.SetDirty(animator);
            // Only this actual derived sleeping-vigilance motion is added.
            // Ordinary catalog LookAround keeps its original open-eye meaning.
            eyelids.RegisterSleepingPeekClip(clip);EditorUtility.SetDirty(eyelids);
            if(PrefabUtility.IsPartOfPrefabInstance(animator))PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            if(PrefabUtility.IsPartOfPrefabInstance(eyelids))PrefabUtility.RecordPrefabInstancePropertyModifications(eyelids);
            HeistSleepingVigilExitAuthoring.ConfigureExistingVigilInstance(match.dragons[0].motor);
            match.hasSleepVigil=true;
        }

        static void ConfigureCargo(HoardHeistMatch match, int slot, Material steel, PhysicsMaterial contact)
        {
            var truck = match.trucks[slot]; var gear = new GameObject("Temporary Heist cargo rack and hoist").transform; gear.SetParent(truck.transform, false);
            gear.localPosition = truck.shape.center + new Vector3(0, truck.shape.size.y * .5f + .35f, -truck.shape.size.z * .22f);
            var deck = Part(gear, "Physical cargo rack deck", PrimitiveType.Cube, Vector3.zero, new Vector3(2.6f,.22f,4.8f), steel, contact);
            match.cargoRacks[slot] = deck.GetComponent<BoxCollider>();
            // The primitive's transform scale is baked into its collider so
            // rack slot coordinates remain world-metre authoring coordinates.
            match.cargoRacks[slot].size = new Vector3(2.6f,.22f,4.8f); deck.transform.localScale = Vector3.one;
            deck.GetComponent<MeshFilter>().sharedMesh = BoxMesh("Actual rack deck mesh", new Vector3(2.6f,.22f,4.8f));
            for (int side = -1; side <= 1; side += 2) Part(gear, "Physical cargo rail", PrimitiveType.Cube, new Vector3(side * 1.3f,.36f,0), new Vector3(.12f,.72f,4.8f), steel, contact);
            var machine = gear.gameObject.AddComponent<JengaCrane>(); machine.travelSpeed = 5; machine.liftSpeed = 3; machine.turnSpeed = 75;
            Part(gear,"Actual mounted crane mast",PrimitiveType.Cube,new Vector3(1.45f,2.2f,-2.25f),new Vector3(.24f,4.4f,.24f),steel,contact);
            var mastTip=new GameObject("Actual crane mast pivot").transform;mastTip.SetParent(gear,false);mastTip.localPosition=new Vector3(1.45f,4.4f,-2.25f);
            var boom=Part(gear,"Articulated telescopic crane boom",PrimitiveType.Cylinder,new Vector3(0,4.4f,-1),new Vector3(.13f,1,.13f),steel).transform;
            var spreader = Part(gear, "Physical hoist spreader", PrimitiveType.Cube, new Vector3(0,5,-1), new Vector3(.32f,.1f,.32f), steel, contact);
            machine.spreader = spreader.AddComponent<Rigidbody>(); machine.spreader.isKinematic = true; machine.spreader.useGravity = false; machine.spreader.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var line = new GameObject("Working hoist cable").AddComponent<LineRenderer>(); line.transform.SetParent(gear, false); line.positionCount = 2; line.startWidth = line.endWidth = .04f; line.sharedMaterial = steel; machine.hoist = line;
            match.hoists[slot] = machine;
            var presentation=gear.gameObject.AddComponent<HoardHoistRig>();presentation.crane=machine;presentation.mastTip=mastTip;presentation.telescopicBoom=boom;match.hoistRigs[slot]=presentation;
            var noise = truck.gameObject.AddComponent<HeistTruckNoise>(); noise.match = match; noise.truck = truck;
        }

        static HoardPottery Pottery(HoardHeistMatch match, Transform parent, Vector3 position, int index, Material clay, PhysicsMaterial contact)
        {
            var root = new GameObject("Actual breakable hoard pottery " + index); root.transform.SetParent(parent, false);
            position.y = Ground(position, match.world.Region("caverns").root.position.y) + .65f; root.transform.position = position;
            var visual = Owned(root.transform, Village + "props/P_PROP_PLANT_pot_v1_01.prefab", 1.2f);
            Bounds bounds = BoundsOf(visual); visual.transform.position += root.transform.position - bounds.center;
            var shape = root.AddComponent<BoxCollider>(); shape.size = new Vector3(1.2f,1.2f,1.2f); shape.sharedMaterial = contact;
            var body = root.AddComponent<Rigidbody>(); body.mass = 14; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            root.AddComponent<HeistPotterySupport>();
            var pot = root.AddComponent<HoardPottery>(); pot.match = match; pot.body = body; pot.intactCollider = shape; pot.intactVisual = visual; pot.seedPosition = position; pot.seedRotation = Quaternion.identity;
            pot.breakSound=root.AddComponent<AudioSource>();pot.breakSound.playOnAwake=false;pot.breakSound.spatialBlend=1;pot.breakSound.maxDistance=120;
            pot.breakSound.clip=RequiredAudio(HeistAudio+(index%2==0?"Fantasy_Game_Smash_Pot_A.wav":"Fantasy_Game_Smash_Pot_B.wav"));
            var filter = visual.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).OrderByDescending(f => f.sharedMesh.bounds.size.sqrMagnitude).First();
            pot.shards = CreateOwnedFragments(filter.gameObject, root.transform, Derived, "Pottery " + index, clay, contact, 3.5f, 2);
            return pot;
        }

        public static Rigidbody[] CreateOwnedFragments(GameObject sourceMeshObject, Transform parent, string outputFolder, string prefix, Material crossSection, PhysicsMaterial contact, float pieceMass = 7, int splits = 2)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || sourceMeshObject == null || EditorUtility.IsPersistent(sourceMeshObject) || sourceMeshObject.GetComponent<MeshFilter>()?.sharedMesh == null || sourceMeshObject.GetComponent<MeshRenderer>() == null || parent == null || !outputFolder.StartsWith("Assets/",StringComparison.Ordinal) || !outputFolder.Contains("/PrivateLicensed") || outputFolder.Contains("..") || splits < 1 || splits > 3 || !float.IsFinite(pieceMass) || pieceMass <= 0 || prefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Stopped explicit authoring, actual temporary source mesh and ignored private fragment destination required.");
            Directory.CreateDirectory(outputFolder); AssetDatabase.Refresh();
            var original = sourceMeshObject.GetComponent<MeshFilter>().sharedMesh;
            var materials = sourceMeshObject.GetComponent<MeshRenderer>().sharedMaterials;
            if (materials.Length != original.subMeshCount) throw new InvalidOperationException("Every actual original submesh needs its matching original material.");
            var bodies = new List<Rigidbody>();
            for (int cell = 0; cell < (1 << splits); cell++)
            {
                var surfaces = new List<Mesh>(); var surfaceMaterials = new List<Material>();
                for (int material = 0; material < original.subMeshCount; material++)
                {
                    Mesh working = ReadableOwnedClone(original);
                    var triangles = working.GetTriangles(material); working.subMeshCount = 1; working.SetTriangles(triangles,0);
                    string context = prefix + " original=" + AssetDatabase.GetAssetPath(original) + " material=" + material + " cell=" + cell;
                    try
                    {
                        for (int plane = 0; plane < splits && working != null; plane++)
                        {
                            Vector3 axis = plane == 0 ? Vector3.right : plane == 1 ? Vector3.forward : Vector3.up;
                            if ((cell & (1 << plane)) == 0) axis = -axis;
                            // Reuse the already validated project adapter over vendor
                            // EzySlice: preserves each original material surface and
                            // repairs only its known numerical slivers/tangent defect.
                            working = SportsTruckFragmentRecipeAuthoring.ClipOwnedWorkingMesh(working, sourceMeshObject.transform.localToWorldMatrix,
                                sourceMeshObject.GetComponent<Renderer>().bounds.center,axis,1,context);
                        }
                        if (working == null) continue;
                        if (working.vertexCount < 3 || working.GetTriangles(0).Length == 0) { UnityEngine.Object.DestroyImmediate(working); continue; }
                        surfaces.Add(working); surfaceMaterials.Add(materials[material]); working = null;
                    }
                    finally { if (working != null) UnityEngine.Object.DestroyImmediate(working); }
                }
                if (surfaces.Count == 0) continue;
                var chunk = new GameObject(prefix + " exact original cell " + cell);
                chunk.transform.SetPositionAndRotation(sourceMeshObject.transform.position,sourceMeshObject.transform.rotation);
                chunk.transform.localScale = sourceMeshObject.transform.lossyScale; chunk.transform.SetParent(parent,true);
                var collision = new List<CombineInstance>();
                for (int surface = 0; surface < surfaces.Count; surface++)
                {
                    Mesh mesh = SavePrivateMesh(surfaces[surface],outputFolder + "/" + prefix + " cell " + cell + " material " + surface + ".asset");
                    var part = new GameObject("Preserved original material surface " + surface); part.transform.SetParent(chunk.transform,false);
                    part.AddComponent<MeshFilter>().sharedMesh = mesh;
                    part.AddComponent<MeshRenderer>().sharedMaterials = Enumerable.Range(0,mesh.subMeshCount).Select(index => index == 0 ? surfaceMaterials[surface] : crossSection).ToArray();
                    for(int sub=0;sub<mesh.subMeshCount;sub++) collision.Add(new CombineInstance { mesh=mesh,subMeshIndex=sub,transform=Matrix4x4.identity });
                }
                var hull = new Mesh { name=prefix+" native collision cell "+cell,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
                hull.CombineMeshes(collision.ToArray(),true,true); hull.RecalculateBounds();
                hull = SavePrivateMesh(hull,outputFolder+"/"+prefix+" cell "+cell+" collision.asset");
                var collider=chunk.AddComponent<MeshCollider>();collider.sharedMesh=hull;collider.convex=true;collider.sharedMaterial=contact;
                var body=chunk.AddComponent<Rigidbody>();body.mass=pieceMass;body.isKinematic=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                bodies.Add(body);chunk.SetActive(false);
            }
            if (bodies.Count < 2) throw new InvalidOperationException("Actual same-model surfaces did not yield a real fracture; no mismatched rubble substitute.");
            return bodies.ToArray();
        }

        static Mesh SavePrivateMesh(Mesh mesh,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) { AssetDatabase.CreateAsset(mesh,path);return mesh; }
            // Explicit repeat authoring updates only this generated derived mesh;
            // its GUID remains stable and no original source mesh is touched.
            EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }

        static Mesh ReadableOwnedClone(Mesh source)
        {
            using (var snapshot = MeshUtility.AcquireReadOnlyMeshData(source))
            {
                var original=snapshot[0];var data=Mesh.AllocateWritableMeshData(1);var copy=data[0];
                try
                {
                    copy.SetVertexBufferParams(original.vertexCount,source.GetVertexAttributes());
                    for(int stream=0;stream<source.vertexBufferCount;stream++) copy.GetVertexData<byte>(stream).CopyFrom(original.GetVertexData<byte>(stream));
                    var indices=original.GetIndexData<byte>();copy.SetIndexBufferParams(indices.Length/(source.indexFormat==UnityEngine.Rendering.IndexFormat.UInt16?2:4),source.indexFormat);copy.GetIndexData<byte>().CopyFrom(indices);
                    copy.subMeshCount=original.subMeshCount;
                    for(int sub=0;sub<original.subMeshCount;sub++)copy.SetSubMesh(sub,original.GetSubMesh(sub),UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
                    var mesh=new Mesh{name=source.name+" exact owned readable working clone",indexFormat=source.indexFormat};
                    Mesh.ApplyAndDisposeWritableMeshData(data,mesh,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);mesh.bounds=source.bounds;return mesh;
                }
                catch { data.Dispose();throw; }
            }
        }

        static GameObject Owned(Transform parent, string path, float maximumSize)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Required actual owned prop: " + path);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(source); PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            obj.transform.SetParent(parent, false);
            foreach (var collider in obj.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var rigidbody in obj.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rigidbody);
            Bounds bounds = BoundsOf(obj); float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            obj.transform.localScale *= maximumSize / Mathf.Max(.001f, largest);
            bounds = BoundsOf(obj); obj.transform.position += parent.position - bounds.center;
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = renderer.sharedMaterials.Select(UrpMaterial).ToArray();
            return obj;
        }
        static Material UrpMaterial(Material source) => OwnedUrpMaterial(source,Folder);
        public static Material OwnedUrpMaterial(Material source,string modeFolder=Folder)
        {
            if (source == null) throw new InvalidOperationException("Preserve and repair missing actual source material explicitly.");
            if (source.GetTag("RenderPipeline",false,"")=="UniversalPipeline"||source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)) return source;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            if(!string.IsNullOrEmpty(guid))
            {
                var shared=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDragonKingdom/Materials/Environment/"+guid+" URP.mat");
                if(shared!=null)return shared;
            }
            string name = "Owned prop URP " + (guid.Length > 0 ? guid : source.name.Replace('/','_'));
            string destination="Assets/"+modeFolder+"/Materials";if(!Directory.Exists(destination)){Directory.CreateDirectory(destination);AssetDatabase.Refresh();}
            var material = SportsEventAuthoring.Mat(modeFolder, name, source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.color : Color.white, .25f);
            var texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.HasProperty("_MainTex") ? source.mainTexture : null;
            if(texture==null&&source.HasProperty("_Albedo"))texture=source.GetTexture("_Albedo");
            if (texture != null) material.SetTexture("_BaseMap", texture);
            if(source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")!=null){material.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));material.EnableKeyword("_NORMALMAP");}
            if(source.HasProperty("_Cutoff")&&(source.GetTag("RenderType",false,"")=="TransparentCutout"||source.IsKeywordEnabled("_ALPHATEST_ON"))){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",source.GetFloat("_Cutoff"));material.EnableKeyword("_ALPHATEST_ON");}
            return material;
        }
        static Bounds BoundsOf(GameObject obj)=>DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(obj.transform);
        static AudioClip RequiredAudio(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path) ?? throw new InvalidOperationException("Run the owned Heist audio intake before authoring: "+path);
        static float Ground(Vector3 p, float fallback)
        {
            Physics.SyncTransforms();
            var hits = Physics.RaycastAll(p + Vector3.up * 150, Vector3.down, 350, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits.OrderBy(h => Mathf.Abs(h.point.y - fallback)))
                if (hit.normal.y > .7f && Mathf.Abs(hit.point.y - fallback) < 12) return hit.point.y;
            throw new InvalidOperationException("Heist prop/goal needs actual supporting cavern road/terrain at " + p + "; no guessed ground fallback.");
        }
        static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, PhysicsMaterial contact = null)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material;
            var collider = obj.GetComponent<Collider>(); if (contact == null) UnityEngine.Object.DestroyImmediate(collider); else collider.sharedMaterial = contact;
            return obj;
        }
        static void PartMesh(Transform parent, string name, Mesh mesh, Material material)
        { var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material; }
        static void Coins(Transform parent, Material gold)
        {
            for (int i = 0; i < 7; i++)
            {
                var p = new Vector3(i < 4 ? -.23f : .24f, (i < 4 ? i : i - 4) * .095f - .15f, 0);
                Part(parent, "Minted royal coin", PrimitiveType.Cylinder, p, new Vector3(.55f,.038f,.55f), gold);
                Part(parent, "Raised royal coin seal", PrimitiveType.Cylinder, p + Vector3.up * .043f, new Vector3(.3f,.008f,.3f), gold);
            }
        }
        static void Bars(Transform parent, Material gold)
        {
            var mesh = BarMesh();
            for (int i = 0; i < 3; i++)
            {
                var bar = new GameObject("Tapered stamped gold ingot " + i); bar.transform.SetParent(parent, false); bar.transform.localPosition = new Vector3((i-1)*.37f,0,0); bar.AddComponent<MeshFilter>().sharedMesh = mesh; bar.AddComponent<MeshRenderer>().sharedMaterial = gold;
                Part(bar.transform, "Ingot royal assay stamp", PrimitiveType.Cylinder, new Vector3(0,.12f,0), new Vector3(.12f,.005f,.12f), gold);
            }
        }
        static void GoldMountain(Transform parent, Vector3 position, Material gold, PhysicsMaterial contact)
        {
            string path = "Assets/" + Folder + "/Models/Original stacked royal coin hoard.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Mesh coin = primitive.GetComponent<MeshFilter>().sharedMesh;
                var coins = new List<CombineInstance>();
                for (int tier = 0; tier < 12; tier++)
                    for (float radius = 0; radius <= 12 - tier * .8f + .001f; radius += 2.4f)
                        for (int i = 0, count = Mathf.Max(1,Mathf.CeilToInt(radius*Mathf.PI*2/2.4f)); i < count; i++)
                        {
                            float angle = i*Mathf.PI*2/count + tier*.19f;
                            var center = new Vector3(Mathf.Cos(angle)*radius,.12f+tier*.14f,Mathf.Sin(angle)*radius);
                            coins.Add(new CombineInstance { mesh = coin, transform = Matrix4x4.TRS(center,Quaternion.Euler(0,i*31,0),new Vector3(2.8f,.045f,2.8f)) });
                        }
                mesh = new Mesh { name = coins.Count + " individually modeled royal coins in ancient hoard", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(coins.ToArray(),true,true); mesh.RecalculateBounds();
                UnityEngine.Object.DestroyImmediate(primitive); AssetDatabase.CreateAsset(mesh,path);
            }
            var mountain = new GameObject("Actual ancient dragon gold hoard"); mountain.transform.SetParent(parent,false); mountain.transform.position = position;
            mountain.AddComponent<MeshFilter>().sharedMesh = mesh; mountain.AddComponent<MeshRenderer>().sharedMaterial = gold;
            var shape = mountain.AddComponent<MeshCollider>(); shape.sharedMesh = mesh; shape.sharedMaterial = contact;
        }
        static Mesh BarMesh()
        {
            string path = "Assets/" + Folder + "/Models/Tapered cast gold ingot.asset"; var found = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (found != null) return found;
            var mesh = new Mesh { name = "Actual tapered cast gold ingot" };
            mesh.vertices = new[] { new Vector3(-.18f,-.1f,-.48f),new Vector3(.18f,-.1f,-.48f),new Vector3(.18f,-.1f,.48f),new Vector3(-.18f,-.1f,.48f),new Vector3(-.13f,.1f,-.4f),new Vector3(.13f,.1f,-.4f),new Vector3(.13f,.1f,.4f),new Vector3(-.13f,.1f,.4f) };
            mesh.triangles = new[] { 0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static Mesh BoxMesh(string name, Vector3 size)
        {
            string path = "Assets/" + Folder + "/Models/" + name + ".asset"; var found = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (found != null) return found;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube); var mesh = UnityEngine.Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh); UnityEngine.Object.DestroyImmediate(temp);
            mesh.name = name; mesh.vertices = mesh.vertices.Select(v => Vector3.Scale(v,size)).ToArray(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static Mesh Lathe(string name, Vector2[] profile)
        {
            string path = "Assets/" + Folder + "/Models/" + name + ".asset"; var found = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (found != null) return found;
            const int sides = 40; var vertices = new Vector3[profile.Length*sides]; var triangles = new List<int>();
            for (int row=0;row<profile.Length;row++) for(int side=0;side<sides;side++) { float angle=side*Mathf.PI*2/sides; vertices[row*sides+side]=new Vector3(Mathf.Cos(angle)*profile[row].x,profile[row].y,Mathf.Sin(angle)*profile[row].x); if(row==0)continue;int a=(row-1)*sides+side,b=(row-1)*sides+(side+1)%sides,c=row*sides+side,d=row*sides+(side+1)%sides;triangles.AddRange(new[]{a,c,b,b,c,d}); }
            var mesh=new Mesh{name="Detailed royal amphora profile",vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
#endif
