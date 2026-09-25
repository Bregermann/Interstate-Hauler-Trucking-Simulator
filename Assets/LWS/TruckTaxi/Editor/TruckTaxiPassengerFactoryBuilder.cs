using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiPassengerFactoryBuilder
    {
        public const string Root="Assets/LWS/TruckTaxi/Passengers";
        public const string DatabasePath=Root+"/TruckTaxiPassengerDatabase.asset";
        public static readonly string[] Stages={"Data","Manifest","Model","Rig / Animation","Prefab","Validation","Registration","Audio"};
        [Serializable] public sealed class Manifest
        {
            public int schemaVersion=1;
            public string passengerId,developmentReference,runtimeName,species,raceEthnicity,sexPresentation,approximateAge,bodyDescription,faceDescription,hair,clothing,distinctiveFeatures;
            public string artStyle="Stylized, game-readable, exaggerated comedy; efficient PS2/PS3-era proportions, original visual identity";
            public string requiredPose="Neutral A-pose, feet grounded, no obstructive props";
            public string[] requiredViews={"Front","Three-quarter","Side","Rear"};
            public string referenceSheet="Plain background; consistent proportions and outfit across every view";
            public int targetPolygons,textureResolution;
            public string rigType,seatType,generationPrompt,avoidPrompt,expectedFilename,sourceFolder,modelStatus,rigStatus,animationStatus,prefabStatus,voiceStatus;
            public string[] uniqueMechanics,validation;
        }
        public static PassengerProfile CreatePassenger(string name)
        {
            TruckTaxiPassengerDialogueAuthoring.EnsureFolder(Root+"/Profiles");
            var p=ScriptableObject.CreateInstance<PassengerProfile>(); p.passengerId="passenger-"+Guid.NewGuid().ToString("N").Substring(0,12);
            p.passengerName=string.IsNullOrWhiteSpace(name) ? "New Passenger" : name;
            AssetDatabase.CreateAsset(p,Root+"/Profiles/"+p.passengerId+".asset"); BuildMissing(p); return p;
        }
        public static void BuildMissing(PassengerProfile p)
        {
            TruckTaxiPassengerDialogueAuthoring.BuildMissingDialogueAssets(p);
            p.casting=Ensure(p.casting,"Casting",p.passengerId);
            p.appearance=Ensure(p.appearance,"Appearance",p.passengerId);
            p.seatProfile=Ensure(p.seatProfile,"Seats",p.passengerId);
            if(p.animatorProfile==null) p.animatorProfile=SharedAnimations();
            if(p.possibleRequests==null || p.possibleRequests.Length==0)
                p.possibleRequests=AssetDatabase.FindAssets("t:PassengerRequestDefinition",new[]{"Assets/LWS/TruckTaxi"}).Select(g=>AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            AddLine(p,"boarding",TruckTaxiDialogueCategory.Boarding,"All aboard. Let's see what this tractor can do.");
            AddLine(p,"ejection",TruckTaxiDialogueCategory.EjectionReaction,p.ejectionReaction);
            AddLine(p,"pair",TruckTaxiDialogueCategory.PairPassengerReaction,"I heard that. We are discussing this driving later.");
            EditorUtility.SetDirty(p); EditorUtility.SetDirty(p.authoredDialogue); AssetDatabase.SaveAssets();
        }
        private static T Ensure<T>(T value,string folder,string id) where T:ScriptableObject
        {
            if(value!=null) return value;
            string root=Root+"/"+folder; TruckTaxiPassengerDialogueAuthoring.EnsureFolder(root);
            string path=root+"/"+id+".asset";
            value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(value==null) { value=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value,path); }
            return value;
        }
        public static void AddLine(PassengerProfile p,string id,TruckTaxiDialogueCategory category,string text)
        {
            var list=(p.authoredDialogue.lines ?? Array.Empty<TruckTaxiDialogueLine>()).ToList();
            if(list.Any(l=>l!=null && l.lineId==id)) return;
            list.Add(new TruckTaxiDialogueLine { lineId=id,passengerId=p.passengerId,category=category,text=text,subtitle=text }); p.authoredDialogue.lines=list.ToArray();
        }
        public static string WriteManifest(PassengerProfile p)
        {
            if(!TruckTaxiChatterboxQueue.IsSafeId(p.passengerId)) throw new InvalidOperationException("Invalid passenger ID.");
            string folder="Tools/TruckTaxiPassengerFactory/Manifests/Passengers"; Directory.CreateDirectory(folder);
            var m=new Manifest { passengerId=p.passengerId,developmentReference=p.developmentReference,runtimeName=p.passengerName,
                species=p.casting?.species,raceEthnicity=p.casting?.raceEthnicity,sexPresentation=p.casting?.sexPresentation,approximateAge=p.casting?.approximateAge,
                bodyDescription=p.appearance?.bodyDescription ?? p.casting?.build,faceDescription=p.appearance?.faceDescription,hair=p.casting?.hair,clothing=p.casting?.clothingStyle,distinctiveFeatures=p.casting?.distinctiveFeatures,
                targetPolygons=p.appearance?.targetPolygons ?? 12000,textureResolution=p.appearance?.textureResolution ?? 1024,
                rigType=p.appearance?.rigType.ToString(),seatType=p.seatProfile?.seatType.ToString(),
                generationPrompt=p.appearance?.generationPrompt,avoidPrompt=p.appearance?.avoidPrompt,
                expectedFilename=p.passengerId+".fbx",sourceFolder=TruckTaxiModelPipeline.SourceFolder(p),
                modelStatus=p.modelPrefab!=null ? "Supplied model" : "PLACEHOLDER / final model missing",
                rigStatus=p.appearance?.generationStatus,animationStatus=p.animatorProfile!=null ? "Shared animations / special fallback" : "MISSING",
                prefabStatus=p.runtimePrefab!=null ? "BUILT" : "MISSING",
                voiceStatus=File.Exists(p.voiceProfile?.neutralReference) ? "Reference assigned" : "MISSING reference; subtitles available",
                uniqueMechanics=(p.uniqueMechanics ?? Array.Empty<TruckTaxiMechanic>()).Select(x=>x.ToString()).ToArray(),validation=Validate(p).ToArray() };
            string path=folder+"/"+p.passengerId+".json"; WriteIfChanged(path,JsonUtility.ToJson(m,true)+"\n"); return path;
        }
        private static void WriteIfChanged(string path,string text) { if(!File.Exists(path) || File.ReadAllText(path)!=text) File.WriteAllText(path,text); }
        public static TruckTaxiAnimatorProfile SharedAnimations()
        {
            string folder=Root+"/Animations"; TruckTaxiPassengerDialogueAuthoring.EnsureFolder(folder);
            string path=folder+"/SharedPassenger.asset";
            var profile=AssetDatabase.LoadAssetAtPath<TruckTaxiAnimatorProfile>(path); if(profile!=null) return profile;
            profile=ScriptableObject.CreateInstance<TruckTaxiAnimatorProfile>();
            var controller=AnimatorController.CreateAnimatorControllerAtPath(folder+"/SharedPassenger.controller");
            var bindings=new List<TruckTaxiAnimationBinding>();
            foreach(TruckTaxiPassengerAnimation action in Enum.GetValues(typeof(TruckTaxiPassengerAnimation)))
            {
                string file=action==TruckTaxiPassengerAnimation.ApproachVehicle || action==TruckTaxiPassengerAnimation.Walk_Normal ? "walk" :
                    action==TruckTaxiPassengerAnimation.Run || action==TruckTaxiPassengerAnimation.Walk_Fast ? "run" :
                    action==TruckTaxiPassengerAnimation.BoardAbstraction ? "sitidle" :
                    action==TruckTaxiPassengerAnimation.Idle_Drunk ? "idledrunk" :
                    action==TruckTaxiPassengerAnimation.Cheer || action==TruckTaxiPassengerAnimation.Celebrate || action==TruckTaxiPassengerAnimation.Wave_Taxi ? "cheer" :
                    action==TruckTaxiPassengerAnimation.Complain || action==TruckTaxiPassengerAnimation.AngryGesture ? "shout" :
                    action==TruckTaxiPassengerAnimation.Point || action==TruckTaxiPassengerAnimation.Wait_Phone ? "talk1" : "idle1";
                string clipPath="Assets/UTS_FullPack/Models/People/Animations/man/"+file+".FBX";
                var clip=AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
                var state=controller.layers[0].stateMachine.AddState(action.ToString()); state.motion=clip;
                if(action==TruckTaxiPassengerAnimation.Idle_Normal) controller.layers[0].stateMachine.defaultState=state;
                bindings.Add(new TruckTaxiAnimationBinding { action=action,stateName=action.ToString(),clip=clip });
            }
            profile.controller=controller; profile.animations=bindings.ToArray(); AssetDatabase.CreateAsset(profile,path); return profile;
        }
        public static void BuildPrefab(PassengerProfile p)
        {
            if(p.appearance==null || p.seatProfile==null) BuildMissing(p);
            string folder=Root+"/Prefabs"; TruckTaxiPassengerDialogueAuthoring.EnsureFolder(folder);
            var root=new GameObject(p.passengerName); root.SetActive(false);
            try
            {
                var actor=root.AddComponent<TruckTaxiPassengerActor>();
                bool human=p.casting==null || p.casting.human;
                GameObject model=p.modelPrefab;
                actor.fallbackModel=model==null;
                if(model==null && human)
                {
                    bool female=p.casting?.sexPresentation?.IndexOf("woman",StringComparison.OrdinalIgnoreCase)>=0 || p.casting?.sexPresentation=="Female";
                    model=AssetDatabase.LoadAssetAtPath<GameObject>(female ? "Assets/UTS_FullPack/Models/People/Prefabs/Womans/business01_f_highpoly.prefab" : "Assets/UTS_FullPack/Models/People/Prefabs/Mans/business01_m.prefab");
                }
                GameObject visual;
                if(model!=null)
                {
                    visual=UnityEngine.Object.Instantiate(model,root.transform); visual.name="Model";
                    if(PrefabUtility.IsPartOfPrefabInstance(visual)) PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    foreach(var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                    foreach(var collider in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                    foreach(var body in visual.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
                    Normalize(visual,p.appearance.heightMeters,p.appearance.bodyScale);
                }
                else { visual=Fallback(root.transform,p); actor.specialAnimationFallback=true; }
                var animator=visual.GetComponentInChildren<Animator>(true);
                if(animator!=null)
                {
                    if(actor.fallbackModel && human && animator.avatar!=null)
                    {
                        string avatarPath=Root+"/Animations/Humanoid/"+Path.GetFileName(AssetDatabase.GetAssetPath(animator.avatar));
                        var imported=AssetDatabase.LoadAssetAtPath<GameObject>(avatarPath);
                        var mapped=imported!=null ? imported.GetComponent<Animator>() : null;
                        if(mapped!=null && mapped.avatar!=null && mapped.avatar.isValid && mapped.avatar.isHuman) animator.avatar=mapped.avatar;
                    }
                    animator.applyRootMotion=false; animator.runtimeAnimatorController=p.animatorProfile?.controller;
                    if(animator.isHuman)
                    {
                        var head=animator.GetBoneTransform(HumanBodyBones.Head); if(head!=null) head.localScale*=p.appearance.headScale;
                        foreach(var bone in new[]{HumanBodyBones.LeftShoulder,HumanBodyBones.RightShoulder})
                        { var shoulder=animator.GetBoneTransform(bone); if(shoulder!=null) shoulder.localPosition*=p.appearance.shoulderWidth; }
                    }
                }
                if(p.appearance.skinMaterials!=null && p.appearance.skinMaterials.Length>0)
                    foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if(renderer.sharedMaterials.Length==p.appearance.skinMaterials.Length) renderer.sharedMaterials=p.appearance.skinMaterials;
                AddAccessories(visual,p.appearance);
                AddProcessedLod(root,visual,p);
                if(p.appearance.buildRagdoll) BuildRagdoll(root);
                root.SetActive(true);
                p.runtimePrefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+p.passengerId+".prefab");
                EditorUtility.SetDirty(p); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void Normalize(GameObject visual,float height,Vector3 bodyScale)
        {
            visual.transform.localPosition=Vector3.zero; visual.transform.localRotation=Quaternion.identity;
            var renderers=visual.GetComponentsInChildren<Renderer>(true); if(renderers.Length==0) return;
            var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            if(bounds.size.y<.001f) throw new InvalidOperationException("Model has collapsed bounds.");
            float scale=height/bounds.size.y; visual.transform.localScale=Vector3.Scale(visual.transform.localScale*scale,bodyScale);
            bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            visual.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }
        private static void AddProcessedLod(GameObject root,GameObject visual,PassengerProfile p)
        {
            if(p.modelPrefab==null || string.IsNullOrEmpty(p.appearance.processedModelPath)) return;
            string path=Path.ChangeExtension(p.appearance.processedModelPath,null)+"_LOD1.fbx";
            if(!File.Exists(path)) return;
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(asset==null) return;
            var lod=UnityEngine.Object.Instantiate(asset,root.transform); lod.name="LOD1";
            Normalize(lod,p.appearance.heightMeters,p.appearance.bodyScale);
            var group=root.AddComponent<LODGroup>();
            group.SetLODs(new[]{new LOD(.15f,visual.GetComponentsInChildren<Renderer>(true)),new LOD(.015f,lod.GetComponentsInChildren<Renderer>(true))}); group.RecalculateBounds();
        }
        private static GameObject Fallback(Transform parent,PassengerProfile p)
        {
            var visual=new GameObject("Stylized placeholder - final model required"); visual.transform.SetParent(parent,false);
            string materials=Root+"/Materials"; TruckTaxiPassengerDialogueAuthoring.EnsureFolder(materials);
            string path=materials+"/"+p.passengerId+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color=p.appearance.fallbackColor; AssetDatabase.CreateAsset(mat,path); }
            float h=p.appearance.heightMeters;
            bool robot=p.appearance.characterType==TruckTaxiCharacterType.Robot || p.appearance.characterType==TruckTaxiCharacterType.DashboardPassenger;
            Primitive(visual.transform,"Body",robot ? PrimitiveType.Cube : PrimitiveType.Capsule,new Vector3(0,h*.48f,0),new Vector3(h*.4f,h*.4f,h*.32f),mat);
            Primitive(visual.transform,"Head",robot ? PrimitiveType.Cube : PrimitiveType.Sphere,new Vector3(0,h*.85f,0),Vector3.one*h*.34f,mat);
            Primitive(visual.transform,"Left foot",PrimitiveType.Cube,new Vector3(-h*.13f,h*.08f,.04f),new Vector3(h*.18f,h*.16f,h*.3f),mat);
            Primitive(visual.transform,"Right foot",PrimitiveType.Cube,new Vector3(h*.13f,h*.08f,.04f),new Vector3(h*.18f,h*.16f,h*.3f),mat);
            string species=(p.casting?.species ?? "").ToLowerInvariant();
            if(species.Contains("dragon") || species.Contains("cat") || species.Contains("creature"))
            { var tail=Primitive(visual.transform,"Tail",PrimitiveType.Capsule,new Vector3(0,h*.4f,-h*.35f),new Vector3(h*.15f,h*.35f,h*.15f),mat); tail.transform.localEulerAngles=new Vector3(65,0,0); }
            if(species.Contains("bird") || species.Contains("chicken") || species.Contains("dragon"))
            { Primitive(visual.transform,"Left wing",PrimitiveType.Cube,new Vector3(-h*.35f,h*.6f,0),new Vector3(h*.45f,h*.09f,h*.3f),mat); Primitive(visual.transform,"Right wing",PrimitiveType.Cube,new Vector3(h*.35f,h*.6f,0),new Vector3(h*.45f,h*.09f,h*.3f),mat); }
            return visual;
        }
        private static GameObject Primitive(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static void AddAccessories(GameObject visual,TruckTaxiAppearanceProfile a)
        {
            // Reusable authored parts/prefabs; no competing character-customizer UI.
            foreach(var item in new[]{a.head,a.hair,a.facialHair,a.eyes,a.glasses,a.hat,a.upperClothing,a.lowerClothing,a.shoes}.Concat(a.accessories ?? Array.Empty<GameObject>()))
                if(item!=null) UnityEngine.Object.Instantiate(item,visual.transform,false);
        }
        public static void BuildRagdoll(GameObject root)
        {
            var actor=root.GetComponent<TruckTaxiPassengerActor>();
            var animator=root.GetComponentInChildren<Animator>(true);
            var bodies=new List<Rigidbody>();
            if(animator!=null && animator.isHuman && animator.avatar!=null && animator.avatar.isValid)
            {
                var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,
                    HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg};
                var map=new Dictionary<Transform,Rigidbody>();
                foreach(var bone in bones)
                {
                    var t=animator.GetBoneTransform(bone); if(t==null) continue;
                    var rb=t.GetComponent<Rigidbody>(); if(rb==null) rb=t.gameObject.AddComponent<Rigidbody>();
                    rb.mass=bone==HumanBodyBones.Hips ? 12 : 5; rb.isKinematic=true;
                    var c=t.GetComponent<SphereCollider>(); if(c==null) c=t.gameObject.AddComponent<SphereCollider>();
                    c.radius=.1f/Mathf.Max(.01f,t.lossyScale.x); c.enabled=false;
                    map[t]=rb; bodies.Add(rb);
                }
                foreach(var pair in map)
                {
                    Transform parent=pair.Key.parent; while(parent!=null && !map.ContainsKey(parent)) parent=parent.parent;
                    if(parent==null) continue;
                    var joint=pair.Key.gameObject.AddComponent<CharacterJoint>(); joint.connectedBody=map[parent]; joint.enablePreprocessing=false;
                    joint.lowTwistLimit=new SoftJointLimit { limit=-30 }; joint.highTwistLimit=new SoftJointLimit { limit=30 };
                    joint.swing1Limit=joint.swing2Limit=new SoftJointLimit { limit=35 };
                }
            }
            if(bodies.Count==0)
            {
                var rb=root.GetComponent<Rigidbody>(); if(rb==null) rb=root.AddComponent<Rigidbody>();
                rb.mass=65; rb.isKinematic=true;
                var collider=root.GetComponent<CapsuleCollider>(); if(collider==null) collider=root.AddComponent<CapsuleCollider>();
                collider.height=1.6f; collider.radius=.3f; collider.center=Vector3.up*.8f; collider.enabled=false;
                bodies.Add(rb);
            }
            if(actor!=null) actor.ragdollBodies=bodies.ToArray();
        }
        public static List<string> Validate(PassengerProfile p)
        {
            var issues=new List<string>();
            if(p==null) { issues.Add("ERROR missing profile"); return issues; }
            if(!TruckTaxiChatterboxQueue.IsSafeId(p.passengerId)) issues.Add("ERROR stable ID invalid");
            if(string.IsNullOrWhiteSpace(p.passengerName)) issues.Add("ERROR runtime name missing");
            if(p.casting==null || p.appearance==null || p.seatProfile==null) issues.Add("ERROR casting/appearance/seat missing");
            if(p.voiceProfile==null || p.authoredDialogue==null) issues.Add("ERROR voice/dialogue asset missing");
            if(p.runtimePrefab==null) issues.Add("ERROR runtime prefab missing");
            else
            {
                var renderers=p.runtimePrefab.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length==0) issues.Add("ERROR no Renderer");
                if(renderers.Any(r=>r.sharedMaterials.Any(m=>m==null))) issues.Add("ERROR missing material");
                if(p.runtimePrefab.GetComponent<TruckTaxiPassengerActor>()==null) issues.Add("ERROR actor missing");
            }
            if(p.authoredDialogue!=null)
            {
                if(!p.authoredDialogue.lines.Any(l=>l!=null && l.category==TruckTaxiDialogueCategory.PickupGreeting)) issues.Add("ERROR greeting missing");
                if(!p.authoredDialogue.lines.Any(l=>l!=null && (l.category==TruckTaxiDialogueCategory.Arrival || l.category==TruckTaxiDialogueCategory.ArrivalGood))) issues.Add("ERROR arrival missing");
                if(p.authoredDialogue.lines.GroupBy(l=>l?.lineId).Any(g=>g.Count()>1)) issues.Add("ERROR duplicate dialogue IDs");
            }
            if(p.modelPrefab==null) issues.Add("WARNING final model missing; fallback is not final casting/art");
            if(!File.Exists(p.voiceProfile?.neutralReference)) issues.Add("WARNING reference voice missing; subtitles used");
            return issues;
        }
        public static void RegisterAll()
        {
            var all=TruckTaxiPassengerDialogueAuthoring.AllPassengers();
            var duplicate=all.GroupBy(p=>p.passengerId,StringComparer.OrdinalIgnoreCase).FirstOrDefault(g=>g.Count()>1);
            if(duplicate!=null) throw new InvalidOperationException("Duplicate passenger ID "+duplicate.Key);
            var db=AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(DatabasePath);
            if(db==null) { db=ScriptableObject.CreateInstance<TruckTaxiPassengerDatabase>(); AssetDatabase.CreateAsset(db,DatabasePath); }
            db.passengers=all; EditorUtility.SetDirty(db);
            foreach(string guid in AssetDatabase.FindAssets("t:TruckTaxiConfiguration",new[]{"Assets/LWS/TruckTaxi"}))
            { var config=AssetDatabase.LoadAssetAtPath<TruckTaxiConfiguration>(AssetDatabase.GUIDToAssetPath(guid)); config.passengerDatabase=db; EditorUtility.SetDirty(config); }
            AssetDatabase.SaveAssets();
        }
        public static void WriteReports()
        {
            var report=new StringBuilder("# Truck Taxi Missing Models\n\nFallback art is playable, not final casting or a completed generated model.\n\n");
            foreach(var group in TruckTaxiPassengerDialogueAuthoring.AllPassengers().GroupBy(p=>p.appearance?.characterType.ToString() ?? "Needs Manual Review"))
            { report.AppendLine("## "+group.Key); foreach(var p in group) { WriteManifest(p); report.AppendLine("- "+p.passengerId+" / "+p.passengerName+": "+(p.modelPrefab!=null ? "supplied model" : "FINAL MODEL MISSING / fallback prefab")+"; "+string.Join("; ",Validate(p))); } }
            File.WriteAllText("Assets/LWS/TruckTaxi/Documentation/TruckTaxi_MissingModels.md",report.ToString());
            TruckTaxiPassengerDialogueAuthoring.WriteMissingVoiceReport(); AssetDatabase.Refresh();
        }
        public static void BuildAllBatch()
        {
            foreach(var p in TruckTaxiPassengerDialogueAuthoring.AllPassengers())
                try { BuildMissing(p); if(p.runtimePrefab==null) BuildPrefab(p); WriteManifest(p); }
                catch(Exception ex) { Debug.LogError(p.passengerId+": "+ex.Message); }
            RegisterAll(); WriteReports(); AssetDatabase.SaveAssets();
        }
    }
}
