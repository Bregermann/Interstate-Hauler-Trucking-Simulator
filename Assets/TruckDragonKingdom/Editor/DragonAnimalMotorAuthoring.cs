#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.Scriptables;
using MalbersAnimations.Utilities;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AnimalState = MalbersAnimations.Controller.State;

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit project-owned authoring. Commercial clips, models, IDs and Empty
    // State templates remain unchanged and referenced from the private intake.
    public static class DragonAnimalMotorAuthoring
    {
        public const string VendorRoot = "Assets/TruckDragonKingdom/PrivateLicensedAnimalController/Malbers Animations";
        const string EmptyRoot = VendorRoot + "/Animal Controller/Empty Controller/";
        const string LandingTag = DragonAnimalMotor.LandingAnimatorTag;

        // Explicit selected migration only: no Play/build regeneration and no
        // prefab/controller re-save for a correction confined to state masks.
        public static int UpgradeOwnedLandingCoreResetMasks()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run the explicit landing-state migration outside Play Mode.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");
            if(catalog==null)throw new InvalidOperationException("Existing authored species catalog required; preserve it rather than regenerating.");
            int changed=0;
            foreach(var species in catalog.species)
            {
                if(!species.Supports(DragonSpeciesRole.Ground|DragonSpeciesRole.Flying)||!species.HasAnimation(DragonAnimationAction.Land))continue;
                string folder="Assets/TruckDragonKingdom/Prefabs/Dragons/"+species.id;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+species.id+" Dragon Animal.prefab");
                if(prefab==null)throw new InvalidOperationException("Existing configured animal required: "+folder);
                var motor=prefab.GetComponentInChildren<DragonAnimalMotor>(true);
                if(motor==null)throw new InvalidOperationException("Configured motor required: "+folder);
                foreach(var state in motor.animal.states.Where(state=>state.ID==motor.idleState||state.ID==motor.groundState))
                {
                    string path=AssetDatabase.GetAssetPath(state);
                    if(!path.StartsWith(folder+"/",StringComparison.Ordinal))throw new InvalidOperationException("Never modify a raw/shared landing state: "+path);
                    if(!ConfigureLandingCoreResetMask(state))continue;
                    AssetDatabase.SaveAssetIfDirty(state);changed++;
                    Debug.Log("DRAGON_LANDING_CORE_MASK_UPGRADED "+path+" values retained; General.modify includes Persistent and LockMovement reset authority.");
                }
            }
            return changed;
        }

        public static GameObject BuildConfiguredPrefab(DragonSpeciesDefinition species, string outputFolder)
        {
            Guard(species, outputFolder);
            string prefabPath = outputFolder.TrimEnd('/') + "/" + species.id + " Dragon Animal.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing; // explicit repair/new folder for existing authored assets
            Directory.CreateDirectory(outputFolder); AssetDatabase.Refresh();
            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(species.visualPrefab);
                instance.name = species.displayName + " · Malbers Dragon";
                ConfigureOneInstance(instance, species, outputFolder);
                return PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally { if (instance != null) UnityEngine.Object.DestroyImmediate(instance); }
        }

        public static GameObject BuildActorPrefab(DragonSpeciesDefinition species, GameObject configuredPrefab, string outputFolder)
        {
            Guard(species, outputFolder);
            if (configuredPrefab == null || configuredPrefab.GetComponentInChildren<DragonAnimalMotor>(true) == null)
                throw new InvalidOperationException("A saved configured Generic Malbers motor prefab is required.");
            string path = outputFolder.TrimEnd('/') + "/" + species.id + " Dragon Actor.prefab";
            var prior = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prior != null) return prior;
            Directory.CreateDirectory(outputFolder); AssetDatabase.Refresh();
            var root = new GameObject(species.displayName + " Dragon Actor");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(configuredPrefab, root.transform);
                model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
                var character = root.AddComponent<DragonCharacter>(); character.motor = model.GetComponentInChildren<DragonAnimalMotor>(true);
                var mouthBone=Find(character.motor.transform,species.mouthBone);character.mouth=mouthBone!=null?mouthBone.Find(DragonCharacter.MouthSocketName):null;
                character.saddle = Find(character.motor.transform, species.saddleBone);
                character.leftGrip = Find(character.motor.transform, species.leftGripBone);
                character.rightGrip = Find(character.motor.transform, species.rightGripBone);
                if (character.mouth == null || species.Supports(DragonSpeciesRole.RiderMount) && character.saddle == null ||
                    species.Supports(DragonSpeciesRole.SheepCarrier) && (character.leftGrip == null || character.rightGrip == null))
                    throw new InvalidOperationException("Required actual mouth/saddle/grip bones are absent for the advertised species roles.");
                character.physicalColliders = model.GetComponentsInChildren<Collider>(true);
                character.voice = root.AddComponent<AudioSource>(); character.voice.playOnAwake = false; character.voice.spatialBlend = 1;
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static GameObject UpgradeActorMouthPrefab(DragonSpeciesDefinition species,GameObject prefab,string outputFolder)
        {
            Guard(species,outputFolder);string path=AssetDatabase.GetAssetPath(prefab);
            if(string.IsNullOrEmpty(path)||!path.StartsWith(outputFolder.TrimEnd('/')+"/",StringComparison.Ordinal))throw new InvalidOperationException("Upgrade only the selected owned Actor prefab mouth reference.");
            var contents=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var actor=contents.GetComponent<DragonCharacter>();if(actor==null||actor.motor==null)throw new InvalidOperationException("Actual configured Actor/motor required.");
                var bone=Find(actor.motor.transform,species.mouthBone);var socket=bone!=null?bone.Find(DragonCharacter.MouthSocketName):null;
                if(socket==null)throw new InvalidOperationException("Upgrade the configured animal mouth socket before its Actor reference.");
                if(actor.mouth==socket)return prefab;
                actor.mouth=socket;EditorUtility.SetDirty(actor);return PrefabUtility.SaveAsPrefabAsset(contents,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }

        // Explicit narrow migration of identity/materials, missing state caches
        // and inherited ground-takeoff exclusions/three-second flight expiry. Other state/controller tuning
        // and valid designer restrictions are retained.
        public static GameObject UpgradeConfiguredPrefab(DragonSpeciesDefinition species, GameObject prefab, string outputFolder)
        {
            Guard(species, outputFolder);
            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path) || !path.StartsWith(outputFolder.TrimEnd('/') + "/", StringComparison.Ordinal))
                throw new InvalidOperationException("Upgrade only the explicitly selected project-owned configured prefab.");
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var motor = contents.GetComponentInChildren<DragonAnimalMotor>(true);
                if (motor == null) throw new InvalidOperationException("Configured motor is absent; preserve the existing prefab.");
                bool changed = motor.configuredSpeciesId != species.id;
                if (changed) { motor.configuredSpeciesId = species.id; EditorUtility.SetDirty(motor); }
                changed|=ConfigureMouthSocket(motor.transform,species);
                // The vendor's Awake indexes this serialized cache before it
                // initializes cloned states. Editor-created state lists must
                // carry matching local active/priority entries into a build.
                if (motor.animal.states_C == null || motor.animal.states_C.Count != motor.animal.states.Count)
                {
                    motor.animal.states_C = motor.animal.states.Select(state => new MAnimal.StateCache { state = state, active = state.Active, priority = state.Priority }).ToList();
                    EditorUtility.SetDirty(motor.animal); changed = true;
                }
                var fly=motor.animal.State_Get<Fly>();
                if(fly!=null&&species.Supports(DragonSpeciesRole.Flying))
                {
                    string statePath=AssetDatabase.GetAssetPath(fly);
                    if(string.IsNullOrEmpty(statePath)||!statePath.StartsWith(outputFolder.TrimEnd('/')+"/",StringComparison.Ordinal))
                        throw new InvalidOperationException("Repair only the selected project-owned Fly state, never a vendor/shared designer state.");
                    if(species.Supports(DragonSpeciesRole.Ground)&&species.HasAnimation(DragonAnimationAction.Takeoff))changed|=ConfigureGroundTakeoff(fly,motor.idleState,motor.groundState);
                    changed|=ConfigureContinuousFlight(fly);
                    if(species.Supports(DragonSpeciesRole.Ground)&&species.HasAnimation(DragonAnimationAction.Land))
                    {
                        var idle=motor.animal.states.First(state=>state.ID==motor.idleState);var ground=motor.animal.states.First(state=>state.ID==motor.groundState);
                        foreach(var state in new[]{idle,ground})if(!AssetDatabase.GetAssetPath(state).StartsWith(outputFolder.TrimEnd('/')+"/",StringComparison.Ordinal))throw new InvalidOperationException("Upgrade only owned landing state assets.");
                        var controller=motor.animator.runtimeAnimatorController as AnimatorController;
                        if(controller==null||!AssetDatabase.GetAssetPath(controller).StartsWith(outputFolder.TrimEnd('/')+"/",StringComparison.Ordinal))throw new InvalidOperationException("Explicit compatible owned landing controller required.");
                        changed|=ConfigureGroundLanding(fly,idle,ground,motor.animal.height,controller,species);
                    }
                }
                changed |= ConfigureUrpMaterials(contents, species, outputFolder);
                if (!changed) return prefab; // no native save/write when already upgraded
                AssetDatabase.SaveAssets();
                return PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        public static GameObject BuildBreathEffectVariant(DragonSpeciesDefinition species, string outputFolder)
        {
            Guard(species, outputFolder);
            if (species.breathEffect == null) return null;
            string path = outputFolder.TrimEnd('/') + "/" + species.id + " Owned Breath URP.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            Directory.CreateDirectory(outputFolder); AssetDatabase.Refresh();
            var contents = (GameObject)PrefabUtility.InstantiatePrefab(species.breathEffect);
            try
            {
                ConfigureUrpMaterials(contents, species, outputFolder, true);
                var particles = contents.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var particle in particles)
                {
                    var main = particle.main; main.playOnAwake = false;
                    var emission = particle.emission; emission.rateOverTime = 0;
                }
                var fire = contents.GetComponentInChildren<FireBreath>(true);
                if (fire != null) { fire.onStart = false; fire.m_Particles = particles; }
                AssetDatabase.SaveAssets();
                return PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(contents); }
        }

        public static DragonAnimalMotor ConfigureOneInstance(GameObject originalModel, DragonSpeciesDefinition species, string outputFolder)
        {
            Guard(species, outputFolder);
            if (originalModel == null) throw new ArgumentNullException(nameof(originalModel));
            Directory.CreateDirectory(outputFolder); AssetDatabase.Refresh();
            var animator = originalModel.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || animator.avatar.isHuman)
                throw new InvalidOperationException("The actual owned dragon must retain its valid Generic Avatar.");
            if (animator.gameObject.GetComponent<DragonAnimalMotor>() != null)
                throw new InvalidOperationException("An existing authored dragon motor is retained; use an explicit repair.");
            var rig = Find(animator.transform, "Rig");
            if (rig == null || rig.parent != animator.transform)
                throw new InvalidOperationException("Expected the original Generic Rig directly below its Animator; preserve imported bone binding paths.");
            originalModel.transform.localScale *= species.visualScale;
            ConfigureUrpMaterials(originalModel, species, outputFolder);
            var animal = animator.GetComponent<MAnimal>();
            if (animal == null) animal = animator.gameObject.AddComponent<MAnimal>();
            var body = animator.GetComponent<Rigidbody>();
            if (body == null) body = animator.gameObject.AddComponent<Rigidbody>();
            var motor = animator.gameObject.AddComponent<DragonAnimalMotor>();
            motor.animal = animal; motor.animator = animator; motor.body = body;
            motor.configuredSpeciesId = species.id;
            animal.Anim = animator; animal.RB = body; animal.CloneStates = true;
            animal.UseCameraInput = false; animal.UseMainCameraDirection.Value = false;
            animal.isPlayer.Value = false; animal.CanStrafe = false;
            animal.InputSource = null;
            animator.updateMode = AnimatorUpdateMode.Fixed;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = true; animator.keepAnimatorStateOnDisable = true;
            body.useGravity = false; body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.mass = Mathf.Clamp(species.visualScale * species.visualScale * 150, 80, 6000);

            // Vendor Empty Controller/Wolf prefab pattern: Rotator and original
            // Rig are authored siblings. MAnimal reparents the already bound Rig
            // on Awake; never change Generic clip paths before Animator caches.
            var rotator = new GameObject("Dragon flight rotator").transform;
            rotator.SetParent(animator.transform, false);
            animal.Rotator = rotator; animal.RootBone = rig;
            FitBody(animal, body, rig, species);

            var idle = CloneState<Idle>("Idle", species, outputFolder);
            var locomotion = CloneState<Locomotion>("Locomotion", species, outputFolder);
            var fall = CloneState<Fall>("Fall", species, outputFolder);
            Fly fly = species.Supports(DragonSpeciesRole.Flying) ? CloneState<Fly>("Fly", species, outputFolder) : null;
            motor.idleState = idle.ID; motor.groundState = locomotion.ID; motor.fallState = fall.ID;
            motor.flightState = fly != null ? fly.ID : null;
            idle.Priority = 1; locomotion.Priority = 2; fall.Priority = 4;
            var groundSpeeds = SpeedSet("Dragon Ground", new[] { idle.ID, locomotion.ID }, species.groundSpeed, species.turnRate, animal.transform.lossyScale.y, 1.45f);
            idle.SpeedSets = new List<MSpeedSet> { groundSpeeds }; locomotion.SpeedSets = new List<MSpeedSet> { groundSpeeds };
            idle.General.RootMotion = false; idle.General.RootMotionRotation = false;
            locomotion.General.RootMotion = false; locomotion.General.RootMotionRotation = false; locomotion.RootRotation = false;
            locomotion.EnterTag.Value = string.Empty;
            animal.states = new List<AnimalState>(); animal.speedSets = new List<MSpeedSet> { groundSpeeds };
            if (fly != null)
            {
                if(species.Supports(DragonSpeciesRole.Ground)&&species.HasAnimation(DragonAnimationAction.Takeoff)) ConfigureGroundTakeoff(fly,idle.ID,locomotion.ID);
                ConfigureContinuousFlight(fly);
                fly.Priority = 6; fly.Bank = species.bankAngle; fly.PitchLimit = 75;
                fly.canLand.Value = false; fly.AutoGlide.Value = species.HasAnimation(DragonAnimationAction.Glide);
                fly.Impulse.Value = 3; fly.ImpulseTime.Value = .8f; fly.LandOn = animal.GroundLayer;
                fly.General.RootMotion = false; fly.General.RootMotionRotation = false;
                fly.EnterTag.Value = species.HasAnimation(DragonAnimationAction.Takeoff) ? "TakeOff" : string.Empty;
                fly.TagModifiers = new List<TagModifier>();
                if (species.HasAnimation(DragonAnimationAction.Takeoff))
                    fly.TagModifiers.Add(new TagModifier { AnimationTag = "TakeOff", modifier = fly.General, tagMessages = new List<MessageItem>() });
                var flightSpeeds = SpeedSet("Dragon Fly", new[] { fly.ID }, species.flightSpeed, species.turnRate,
                    animal.transform.lossyScale.y, 1.28f, species.flightAcceleration);
                fly.SpeedSets = new List<MSpeedSet> { flightSpeeds }; animal.speedSets.Add(flightSpeeds);
                animal.states.Add(fly);
            }
            animal.states.Add(fall); animal.states.Add(locomotion); animal.states.Add(idle);
            animal.states_C = animal.states.Select(state => new MAnimal.StateCache { state = state, active = state.Active, priority = state.Priority }).ToList();
            animal.OverrideStartState = species.Supports(DragonSpeciesRole.Ground) ? idle.ID : fly != null ? fly.ID : idle.ID;

            string controllerPath = outputFolder.TrimEnd('/') + "/" + species.id + " Dragon.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                throw new InvalidOperationException("Existing generated controller retained: " + controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            ConfigureParameters(controller);
            ConfigureLocomotion(controller, species, idle, locomotion, fall, fly);
            if(fly!=null&&species.Supports(DragonSpeciesRole.Ground)&&species.HasAnimation(DragonAnimationAction.Land))ConfigureGroundLanding(fly,idle,locomotion,animal.height,controller,species);
            ConfigureActions(controller, species, animal, motor);
            animator.runtimeAnimatorController = controller;
            ConfigureMouthSocket(motor.transform,species);
            foreach (var state in animal.states) EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(animal); EditorUtility.SetDirty(animator); EditorUtility.SetDirty(motor);
            AssetDatabase.SaveAssets();
            return motor;
        }

        static bool ConfigureMouthSocket(Transform animal,DragonSpeciesDefinition species)
        {
            var bone=Find(animal,species.mouthBone);
            if(bone==null)throw new InvalidOperationException("Actual mouth bone is absent on the owned configured rig: "+species.id+"/"+species.mouthBone);
            var existing=bone.Find(DragonCharacter.MouthSocketName);
            if(existing!=null)return false; // preserve an already authored/designer-tuned socket
            // Calibrate each actual imported rig's neutral bone basis separately.
            // RigJaw's +Z points sideways on these adults; it is not muzzle aim.
            // The socket retains zero offset and every animated head/jaw change,
            // without rotating/reparenting any original bone or motor/body root.
            Vector3 forward=bone.InverseTransformDirection(animal.forward),up=bone.InverseTransformDirection(animal.up);
            if(forward.sqrMagnitude<.9f||up.sqrMagnitude<.9f||Mathf.Abs(Vector3.Dot(forward.normalized,up.normalized))>.01f)throw new InvalidOperationException("Cannot derive an orthogonal actual model mouth basis for "+species.id);
            var socket=new GameObject(DragonCharacter.MouthSocketName).transform;socket.gameObject.layer=bone.gameObject.layer;socket.SetParent(bone,false);socket.localPosition=Vector3.zero;socket.localRotation=Quaternion.LookRotation(forward,up);socket.localScale=Vector3.one;EditorUtility.SetDirty(socket);return true;
        }

        static void Guard(DragonSpeciesDefinition species, string folder)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required for explicit dragon rig authoring.");
            if (species == null || species.visualPrefab == null || string.IsNullOrWhiteSpace(species.id) ||
                species.id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !float.IsFinite(species.visualScale) || species.visualScale <= 0)
                throw new InvalidOperationException("A discovered owned species and valid stable ID/scale are required.");
            if (EditorUtility.IsPersistent(species.visualPrefab) == false)
                throw new InvalidOperationException("The discovered visual must be a saved owned asset, not a scene object.");
            string normalized = (folder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith("Assets/TruckDragonKingdom/", StringComparison.Ordinal) ||
                normalized.Contains("..") || normalized.Contains("PrivateLicensed"))
                throw new InvalidOperationException("Generated dragon assets belong in a dedicated project-owned Dragon Kingdom folder.");
            if (!UnityEditorInternal.InternalEditorUtility.tags.Contains("Animal"))
                throw new InvalidOperationException("Register the vendor Animal tag before adding MAnimal; preserve shared tag/layer ownership.");
            if (!species.HasAnimation(DragonAnimationAction.GroundIdle) && !species.HasAnimation(DragonAnimationAction.FlyIdle))
                throw new InvalidOperationException("A real supported idle animation is required.");
        }

        static T CloneState<T>(string name, DragonSpeciesDefinition species, string folder) where T : AnimalState
        {
            string path = folder.TrimEnd('/') + "/" + species.id + " " + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) throw new InvalidOperationException("Existing authored state retained: " + path);
            var source = AssetDatabase.LoadAssetAtPath<T>(EmptyRoot + "Empty " + name + ".asset");
            if (source == null || source.ID == null) throw new InvalidOperationException("Imported actual vendor Empty State and StateID required: " + name);
            var copy = UnityEngine.Object.Instantiate(source); copy.name = species.id + " " + name;
            copy.animal = null; copy.Input = string.Empty;
            AssetDatabase.CreateAsset(copy, path); return copy;
        }

        static bool ConfigureGroundTakeoff(Fly fly,StateID idle,StateID locomotion)
        {
            // The official Empty Fly template sleeps while Idle/Locomotion,
            // appropriate for jumping into flight. Our real Takeoff animation
            // explicitly supports leaving the ground directly. Remove ONLY
            // these inherited exclusions; retain custom restrictions, modes,
            // stances, ResetFrom, controller/state tuning and original source.
            if(!fly.IncludeSleepState)return false; // a designer allow-list is retained
            if(fly.SleepFromState==null){fly.SleepFromState=new List<StateID>();EditorUtility.SetDirty(fly);return true;}
            int removed=fly.SleepFromState.RemoveAll(state=>state==idle||state==locomotion);
            if(removed>0)EditorUtility.SetDirty(fly);return removed>0;
        }
        static bool ConfigureContinuousFlight(Fly fly)
        {
            // Empty Fly's demonstration Duration=3 starts State.IDuration and
            // authorizes Fall after3seconds even with genuine Fly input held.
            // Flight duration belongs to the match's real stamina/landing owner.
            // Migrate ONLY that inherited constant; retain other designer values
            // and variable references for an explicit separate design decision.
            if(fly.Duration!=null&&fly.Duration.UseConstant&&Mathf.Approximately(fly.Duration.Value,3))
            {fly.Duration=new FloatReference(0);EditorUtility.SetDirty(fly);return true;}
            return false;
        }
        static bool ConfigureGroundLanding(Fly fly,AnimalState idle,AnimalState ground,float hip,AnimatorController controller,DragonSpeciesDefinition species)
        {
            bool changed=false;
            // Fly's first accepted surface must be at/below its real hip height;
            // a longer demonstration ray authorizes Fall while still airborne.
            if(fly.LandMultiplier.UseConstant&&Mathf.Approximately(fly.LandMultiplier.Value,1)&&!Mathf.Approximately(hip,1))
            {fly.LandMultiplier=new FloatReference(hip);EditorUtility.SetDirty(fly);changed=true;}
            foreach(var state in new[]{idle,ground})
            {
                changed|=ConfigureLandingCoreResetMask(state);
                if(state.TagModifiers==null)state.TagModifiers=new List<TagModifier>();
                var tag=state.TagModifiers.FirstOrDefault(value=>value.AnimationTag==LandingTag);
                if(tag==null)
                {
                    var setting=state.General;setting.Persistent=true;setting.LockMovement=true;setting.modify|=modifier.Persistent|modifier.LockMovement;
                    state.TagModifiers.Add(new TagModifier{AnimationTag=LandingTag,modifier=setting,tagMessages=new List<MessageItem>()});EditorUtility.SetDirty(state);changed=true;
                }
                else if(!tag.modifier.Persistent||!tag.modifier.LockMovement||(tag.modifier.modify&(modifier.Persistent|modifier.LockMovement))!=(modifier.Persistent|modifier.LockMovement))
                {
                    var setting=tag.modifier;setting.Persistent=true;setting.LockMovement=true;setting.modify|=modifier.Persistent|modifier.LockMovement;tag.modifier=setting;EditorUtility.SetDirty(state);changed=true;
                }
            }
            var clip=Clip(species,DragonAnimationAction.Land);
            foreach(string name in new[]{"Land to Idle","Land to Movement"})
            {
                var state=controller.layers[0].stateMachine.states.Select(value=>value.state).SingleOrDefault(value=>value.name==name);
                if(state==null||state.motion!=clip)throw new InvalidOperationException("Preserve custom controller; expected actual compatible Land state/mapped clip absent: "+name);
                if(state.tag!=LandingTag){state.tag=LandingTag;EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);changed=true;}
            }
            return changed;
        }

        static bool ConfigureLandingCoreResetMask(AnimalState state)
        {
            const modifier reset=modifier.Persistent|modifier.LockMovement;
            if((state.General.modify&reset)==reset)return false;
            var setting=state.General;setting.modify|=reset;state.General=setting;
            EditorUtility.SetDirty(state);return true;
        }

        static MSpeedSet SpeedSet(string name, StateID[] states, float worldSpeed, float turnRate, float scale, float sprintMultiplier, float acceleration = 8)
        {
            float normal = Mathf.Max(.1f, worldSpeed) / Mathf.Max(.001f, scale);
            float lerp = Mathf.Clamp(acceleration / Mathf.Max(1, worldSpeed) * 6, .5f, 12);
            var walk = new MSpeed(name, 1, lerp, 5) { position = new FloatReference(normal), rotation = new FloatReference(Mathf.Max(.1f, turnRate / 60f)), animator = new FloatReference(1) };
            var sprint = new MSpeed(name + " Sprint", 2, lerp, 5) { position = new FloatReference(normal * sprintMultiplier), rotation = new FloatReference(Mathf.Max(.1f, turnRate / 60f)), animator = new FloatReference(1.12f) };
            return new MSpeedSet { name = name, StartVerticalIndex = new IntReference(1), TopIndex = new IntReference(2),
                m_SprintIndex = new IntReference(2), states = states.ToList(), Speeds = new List<MSpeed> { walk, sprint },
                PitchLerpOn = new FloatReference(3), PitchLerpOff = new FloatReference(3), BankLerp = new FloatReference(4) };
        }

        static void FitBody(MAnimal animal, Rigidbody body, Transform rig, DragonSpeciesDefinition species)
        {
            var saddle = Find(rig, species.saddleBone) ?? rig;
            Vector3 local = animal.transform.InverseTransformPoint(saddle.position);
            float hip = Mathf.Max(.5f, local.y);
            var capsule = animal.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = animal.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = 2; capsule.radius = Mathf.Max(.25f, hip * .55f); capsule.height = Mathf.Max(capsule.radius * 2, hip * 2.2f);
            capsule.center = new Vector3(local.x, hip * .8f, local.z); animal.MainCollider = capsule;
            body.centerOfMass = capsule.center; animal.height = hip;
            animal.pivots = new List<MPivots> { new MPivots("Hip", new Vector3(0, hip, -hip * .4f), 1), new MPivots("Chest", new Vector3(0, hip, hip * .4f), 1) };
            animal.groundLayer.Value = 1; // default terrain/road layer; actor remains vendor layer 20
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        static bool ConfigureUrpMaterials(GameObject model, DragonSpeciesDefinition species, string folder, bool particles = false)
        {
            var shader = Shader.Find(particles ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Installed compatible URP shader required for the actual owned material variants.");
            var replacements = new Dictionary<Material, Material>();
            bool changed = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool rendererChanged = false;
                if (particles && renderer is ParticleSystemRenderer particleRenderer)
                {
                    var system = particleRenderer.GetComponent<ParticleSystem>();
                    int required = system != null && system.trails.enabled ? 2 : particleRenderer.renderMode != ParticleSystemRenderMode.None ? 1 : 0;
                    if (materials.Length < required) Array.Resize(ref materials, required);
                }
                if (materials.Length == 0 && MaterialSlotRenders(renderer, 0))
                    throw MissingMaterial(model.transform, renderer, 0, "active renderer has no material slots");
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null)
                    {
                        // ParticleSystemRenderer exposes a second material for
                        // trails even when its actual TrailModule is disabled.
                        // That slot is not a missing visible fire surface.
                        if (!MaterialSlotRenders(renderer, i)) continue;
                        if (particles && renderer is ParticleSystemRenderer)
                        {
                            source = AssetDatabase.LoadAssetAtPath<Material>(VendorRoot + "/Common/Particles/Materials/FireBreath.mat");
                            if (source == null || source.mainTexture == null)
                                throw MissingMaterial(model.transform, renderer, i, "the verified owned FireBreath material/Fire_Sheet texture is unavailable");
                        }
                        else throw MissingMaterial(model.transform, renderer, i, "active visual material is missing; no verified source material applies");
                    }
                    if (source.shader == shader) continue;
                    if (!replacements.TryGetValue(source, out var replacement))
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
                        if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("A saved owned source material is required before creating a URP variant.");
                        string key = guid + "-" + localId;
                        string path = folder.TrimEnd('/') + "/" + species.id + " Material " + key + (particles ? " URP Particle.mat" : " URP.mat");
                        replacement = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (replacement == null)
                        {
                            replacement = new Material(shader) { name = species.id + " · " + source.name + " · URP" };
                            var texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                            replacement.SetTexture("_BaseMap", texture);
                            replacement.SetColor("_BaseColor", SourceColor(source, particles));
                            replacement.SetTextureScale("_BaseMap", source.mainTextureScale); replacement.SetTextureOffset("_BaseMap", source.mainTextureOffset);
                            if (replacement.HasProperty("_Smoothness")) replacement.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : .35f);
                            if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
                            { replacement.SetTexture("_BumpMap", source.GetTexture("_BumpMap")); replacement.EnableKeyword("_NORMALMAP"); }
                            if (source.HasProperty("_EmissionMap") && source.GetTexture("_EmissionMap") != null)
                            {
                                replacement.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
                                replacement.SetColor("_EmissionColor", source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.white);
                                replacement.EnableKeyword("_EMISSION");
                            }
                            replacement.enableInstancing = true; AssetDatabase.CreateAsset(replacement, path);
                            if (particles)
                            {
                                bool additive = source.shader != null && source.shader.name.IndexOf("Additive", StringComparison.OrdinalIgnoreCase) >= 0;
                                replacement.SetFloat("_Surface", 1); replacement.SetFloat("_Blend", additive ? 2 : 0);
                                replacement.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                replacement.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                                replacement.SetFloat("_ZWrite", 0); replacement.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                                replacement.SetOverrideTag("RenderType", "Transparent"); replacement.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                                EditorUtility.SetDirty(replacement);
                            }
                        }
                        replacements.Add(source, replacement);
                    }
                    if (particles && replacement.HasProperty("_BaseColor") && replacement.GetColor("_BaseColor") != SourceColor(source, true))
                    { replacement.SetColor("_BaseColor", SourceColor(source, true)); EditorUtility.SetDirty(replacement); }
                    materials[i] = replacement;
                    rendererChanged = true;
                }
                if (rendererChanged) { renderer.sharedMaterials = materials; EditorUtility.SetDirty(renderer); changed = true; }
            }
            return changed;
        }

        static Color SourceColor(Material source, bool particles) => particles && source.HasProperty("_TintColor") ? source.GetColor("_TintColor") :
            source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;

        static bool MaterialSlotRenders(Renderer renderer, int index)
        {
            if (!renderer.enabled) return false;
            if (renderer is ParticleSystemRenderer particleRenderer)
            {
                if (index == 0) return particleRenderer.renderMode != ParticleSystemRenderMode.None;
                var system = particleRenderer.GetComponent<ParticleSystem>();
                return index == 1 && system != null && system.trails.enabled;
            }
            if (renderer is SkinnedMeshRenderer skin && (skin.sharedMesh == null || skin.sharedMesh.vertexCount == 0)) return false;
            if (renderer is MeshRenderer)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0) return false;
            }
            return true;
        }

        static InvalidOperationException MissingMaterial(Transform root, Renderer renderer, int index, string detail)
        {
            string path = renderer.transform == root ? root.name : root.name + "/" + AnimationUtility.CalculateTransformPath(renderer.transform, root);
            return new InvalidOperationException("Dragon material source error at " + path + " [" + renderer.GetType().Name + "] material slot " + index + ": " + detail + ".");
        }

        static void ConfigureParameters(AnimatorController controller)
        {
            foreach (string name in new[] { "Vertical", "VerticalRaw", "Horizontal", "UpDown", "DeltaUpDown", "StateFloat", "SpeedMultiplier", "StateTime", "ModePower", "DeltaAngle", "Slope" }) controller.AddParameter(name, AnimatorControllerParameterType.Float);
            foreach (string name in new[] { "Movement", "Grounded", "Sprint", "Strafe" }) controller.AddParameter(name, AnimatorControllerParameterType.Bool);
            foreach (string name in new[] { "State", "LastState", "StateEnterStatus", "StateExitStatus", "StateProfile", "Mode", "ModeStatus", "Stance", "LastStance", "Random", "Type" }) controller.AddParameter(name, AnimatorControllerParameterType.Int);
            controller.AddParameter("StateOn", AnimatorControllerParameterType.Trigger); controller.AddParameter("ModeOn", AnimatorControllerParameterType.Trigger);
        }

        static void ConfigureLocomotion(AnimatorController controller, DragonSpeciesDefinition species, Idle idle, Locomotion locomotion, Fall fall, Fly fly)
        {
            var machine = controller.layers[0].stateMachine;
            var idleClip = Clip(species, DragonAnimationAction.GroundIdle) ?? Clip(species, DragonAnimationAction.FlyIdle);
            var idleAnim = State(machine, "Ground Idle", idle.ID.name, idleClip);
            var ground = State(machine, "Ground Movement", locomotion.ID.name, GroundTree(controller, species, idleClip));
            var fallClip = Clip(species, DragonAnimationAction.Dive) ?? Clip(species, DragonAnimationAction.FlyIdle) ?? Clip(species, DragonAnimationAction.Damage) ?? idleClip;
            var falling = State(machine, "Unsupported Ground Fall", fall.ID.name, fallClip);
            Enter(machine, falling, fall.ID.ID);
            if (fly != null)
            {
                var flying = State(machine, "Flight", fly.ID.name, FlightTree(controller, species));
                if (species.TryGetAnimation(DragonAnimationAction.Takeoff, out var takeoff))
                {
                    var takingOff = State(machine, "Takeoff", "TakeOff", takeoff.clip); takingOff.speed = takeoff.playbackSpeed;
                    Enter(machine, takingOff, fly.ID.ID); Finish(takingOff, flying);
                }
                else Enter(machine, flying, fly.ID.ID);
                machine.defaultState = species.Supports(DragonSpeciesRole.Ground) ? idleAnim : flying;
            }
            else machine.defaultState = idleAnim;
            if (fly != null && species.TryGetAnimation(DragonAnimationAction.Land, out var land))
            {
                var landingIdle = State(machine, "Land to Idle", idle.ID.name, land.clip); landingIdle.speed = land.playbackSpeed;
                var landingWalk = State(machine, "Land to Movement", locomotion.ID.name, land.clip); landingWalk.speed = land.playbackSpeed;
                var a = Enter(machine, landingIdle, idle.ID.ID); a.AddCondition(AnimatorConditionMode.Equals, fly.ID.ID, "LastState");
                var b = Enter(machine, landingWalk, locomotion.ID.ID); b.AddCondition(AnimatorConditionMode.Equals, fly.ID.ID, "LastState");
                Finish(landingIdle, idleAnim); Finish(landingWalk, ground);
            }
            Enter(machine, idleAnim, idle.ID.ID); Enter(machine, ground, locomotion.ID.ID);
        }

        static BlendTree GroundTree(AnimatorController controller, DragonSpeciesDefinition species, AnimationClip idle)
        {
            var tree = Tree(controller, "Ground walk / run / turn", "Horizontal", "Vertical");
            tree.AddChild(idle, Vector2.zero);
            tree.AddChild(Clip(species, DragonAnimationAction.Walk) ?? idle, Vector2.up);
            tree.AddChild(Clip(species, DragonAnimationAction.Run) ?? Clip(species, DragonAnimationAction.Walk) ?? idle, Vector2.up * 2);
            if (species.TryGetAnimation(DragonAnimationAction.TurnLeft, out var left)) tree.AddChild(left.clip, Vector2.left);
            if (species.TryGetAnimation(DragonAnimationAction.TurnRight, out var right)) tree.AddChild(right.clip, Vector2.right);
            return tree;
        }

        static BlendTree FlightTree(AnimatorController controller, DragonSpeciesDefinition species)
        {
            var fly = Clip(species, DragonAnimationAction.Fly);
            if (fly == null) throw new InvalidOperationException("A flying role needs an actual flight clip.");
            var tree = Tree(controller, "Flight flap / glide / bank", "Horizontal", "Vertical");
            tree.AddChild(Clip(species, DragonAnimationAction.FlyIdle) ?? fly, Vector2.zero); tree.AddChild(fly, Vector2.up);
            tree.AddChild(Clip(species, DragonAnimationAction.Glide) ?? fly, Vector2.up * 2);
            if (species.TryGetAnimation(DragonAnimationAction.BankLeft, out var left)) tree.AddChild(left.clip, new Vector2(-1, 1));
            if (species.TryGetAnimation(DragonAnimationAction.BankRight, out var right)) tree.AddChild(right.clip, new Vector2(1, 1));
            return tree;
        }

        static BlendTree Tree(AnimatorController controller, string name, string horizontal, string vertical)
        {
            var tree = new BlendTree { name = name, blendType = BlendTreeType.FreeformCartesian2D, blendParameter = horizontal, blendParameterY = vertical, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller); return tree;
        }
        static AnimationClip Clip(DragonSpeciesDefinition species, DragonAnimationAction action) => species.TryGetAnimation(action, out var mapping) ? mapping.clip : null;
        static AnimatorState State(AnimatorStateMachine machine, string name, string tag, Motion motion)
        { var state = machine.AddState(name); state.tag = tag; state.motion = motion; state.writeDefaultValues = true; return state; }
        static AnimatorStateTransition Enter(AnimatorStateMachine machine, AnimatorState target, int id)
        {
            var transition = machine.AddAnyStateTransition(target); transition.duration = .16f; transition.hasExitTime = false; transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, id, "State"); transition.AddCondition(AnimatorConditionMode.If, 0, "StateOn"); return transition;
        }
        static void Finish(AnimatorState from, AnimatorState to)
        { var transition = from.AddTransition(to); transition.hasExitTime = true; transition.exitTime = .92f; transition.duration = .12f; }

        static void ConfigureActions(AnimatorController controller, DragonSpeciesDefinition species, MAnimal animal, DragonAnimalMotor motor)
        {
            var modeId = AssetDatabase.LoadAssetAtPath<ModeID>(VendorRoot + "/Common/Scriptable Assets/IDs/ModeID/Action.asset");
            if (modeId == null) throw new InvalidOperationException("The actual imported Malbers Action ModeID is required.");
            controller.AddLayer("Dragon Actions"); var layers = controller.layers; layers[1].defaultWeight = 1; controller.layers = layers;
            var machine = controller.layers[1].stateMachine; var empty = State(machine, "No Action", string.Empty, null); machine.defaultState = empty;
            var mode = new Mode { ID = modeId, AllowMovement = true, AllowRotation = true, Abilities = new List<Ability>() };
            var bindings = new List<DragonMotorActionBinding>();
            foreach (var mapping in species.animations ?? Array.Empty<DragonAnimationMapping>())
            {
                if (mapping == null || mapping.clip == null || !IsAbility(mapping.action)) continue;
                int ability = (int)mapping.action + 1;
                var limits = JsonUtility.FromJson<ModeProperties>("{}");
                limits.affectStates = new List<StateID>(); limits.Stances = new List<StanceID>(); limits.TransitionFrom = new List<int>();
                bool stationary = mapping.action == DragonAnimationAction.Sleep || mapping.action == DragonAnimationAction.Wake || mapping.action == DragonAnimationAction.Perch;
                mode.Abilities.Add(new Ability { Name = mapping.action.ToString(), Index = new IntReference(ability), Limits = limits,
                    Status = mapping.loop ? AbilityStatus.Forever : AbilityStatus.PlayOnce,
                    MultiplierPosition = new FloatReference(stationary ? 0 : 1), MultiplierRotation = new FloatReference(stationary ? 0 : 1),
                    Persistent = stationary, CoolDown = new FloatReference(.35f) });
                var state = State(machine, mapping.action.ToString(), "DragonAction_" + mapping.action, mapping.clip); state.speed = mapping.playbackSpeed;
                var callback = state.AddStateMachineBehaviour<ModeBehaviour>(); callback.ModeID = modeId; callback.EnterMode = callback.ExitMode = callback.OnModeUpdate = true;
                callback.NoExitTransitions = !mapping.loop; callback.ExitTime = .98f; callback.ExitAbility = -1;
                var entry = machine.AddAnyStateTransition(state); entry.hasExitTime = false; entry.duration = .12f; entry.canTransitionToSelf = false;
                entry.AddCondition(AnimatorConditionMode.Equals, Mathf.Abs(modeId.ID) * 1000 + ability, "Mode"); entry.AddCondition(AnimatorConditionMode.If, 0, "ModeOn");
                bindings.Add(new DragonMotorActionBinding { action = mapping.action, mode = modeId, abilityIndex = ability, continuous = mapping.loop });
            }
            var ended = machine.AddAnyStateTransition(empty); ended.hasExitTime = false; ended.duration = .12f; ended.canTransitionToSelf = false;
            ended.AddCondition(AnimatorConditionMode.Equals, 0, "Mode"); ended.AddCondition(AnimatorConditionMode.Equals, 0, "ModeStatus");
            var interrupted = machine.AddAnyStateTransition(empty); interrupted.hasExitTime = false; interrupted.duration = .12f; interrupted.canTransitionToSelf = false;
            interrupted.AddCondition(AnimatorConditionMode.Equals, -2, "ModeStatus");
            animal.modes = new List<Mode> { mode }; motor.actionBindings = bindings.ToArray();
        }
        static bool IsAbility(DragonAnimationAction action)
        {
            switch (action)
            {
                case DragonAnimationAction.GroundIdle: case DragonAnimationAction.Walk: case DragonAnimationAction.Run:
                case DragonAnimationAction.TurnLeft: case DragonAnimationAction.TurnRight: case DragonAnimationAction.Takeoff:
                case DragonAnimationAction.Fly: case DragonAnimationAction.FlyIdle: case DragonAnimationAction.Glide:
                case DragonAnimationAction.BankLeft: case DragonAnimationAction.BankRight: case DragonAnimationAction.Land: return false;
                default: return true;
            }
        }
    }
}
#endif
