#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.TruckDragonKingdom.Editor;
using MalbersAnimations;
using MalbersAnimations.Controller;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    public static partial class DragonSpeciesAuthoring
    {
        const string GroundGrabVersion="LWS GROUNDED REACH V1";
        [Serializable]sealed class GroundGrabReceipt
        {
            public string scope="EXPLICIT OWNED GROUNDED GENERIC RIG DERIVATIVE; no root/target motion, no airborne Grab replacement, no native action/contact/render acceptance claimed",source,sourceHash,clip;
            public float palmHeightAtContact,lowestSkinAtContact,maximumRootDisplacement;
            public Vector3 gripMotorLocalAtContact;
            public bool sourceBytesUnchanged,airborneGrabUnchanged;
            public string sourcePreservationCheck="not executed until pose gate succeeds";
            public string lowestMesh;
            public int lowestVertex;
            public Vector3 lowestActualModelPoint;
            public string[] lowestBones;
            public float[] lowestWeights;
        }
        public static void UpgradeInfernoGroundGrab()=>BuildInfernoGroundGrab(false);
        public static void RegenerateInfernoGroundGrab()=>BuildInfernoGroundGrab(true);
        static void BuildInfernoGroundGrab(bool regenerate)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Run explicit selected GroundGrab authoring outside Play Mode.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(CatalogPath);
            if(catalog==null||!catalog.TryGet("dragon-inferno",out var species)||!species.TryGetAnimation(DragonAnimationAction.GroundIdle,out var idle)||!species.TryGetAnimation(DragonAnimationAction.Grab,out var aerial))throw new InvalidOperationException("Existing actual ground Idle and separate airborne Grab required.");
            string path=AuthoredRoot+"/dragon-inferno/GroundGrab.anim",raw=DragonRoot+"Dragon Inferno Cute Series/Prefabs/Dragon Inferno.prefab";
            string sourcePath=AssetDatabase.GetAssetPath(idle.clip),aerialPath=AssetDatabase.GetAssetPath(aerial.clip);string sourceHash=GroundGrabHash(sourcePath),airHash=GroundGrabHash(aerialPath),modelHash=GroundGrabHash(raw);
            var output=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool mapped=species.TryGetAnimation(DragonAnimationAction.GroundGrab,out var mapping);
            if(mapped&&(AssetDatabase.GetAssetPath(mapping.clip)!=path||mapping.sourceNote==null||!mapping.sourceNote.StartsWith(GroundGrabVersion,StringComparison.Ordinal)))throw new InvalidOperationException("Preserve alternate designer GroundGrab mapping; select a different owned action explicitly.");
            if(output==null||!mapped||regenerate)
            {
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(raw);if(original==null)throw new InvalidOperationException("Actual owned original Inferno Generic rig required.");
                var model=UnityEngine.Object.Instantiate(original);model.hideFlags=HideFlags.HideAndDontSave;model.SetActive(false);AnimationClip generated=null;
                try
                {
                    generated=CreateGroundGrabClip(model,idle.clip,species.visualScale,out var receipt);receipt.source=sourcePath;receipt.sourceHash=sourceHash;receipt.clip=path;
                    if(output==null){Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.CreateAsset(generated,path);output=generated;generated=null;}
                    else{generated.name=output.name;EditorUtility.CopySerialized(generated,output);EditorUtility.SetDirty(output);AssetDatabase.SaveAssetIfDirty(output);}
                    if(!mapped){mapping=new DragonAnimationMapping{action=DragonAnimationAction.GroundGrab};species.animations=species.animations.Concat(new[]{mapping}).ToArray();}
                    mapping.clip=output;mapping.loop=false;mapping.playbackSpeed=1;mapping.sourceNote=GroundGrabVersion+": actual Idle reference, selective torso crouch, real two-bone forelimb reach/planted rear feet and real neck targeting; airborne Grab retained byte-exact.";
                    EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
                    receipt.sourceBytesUnchanged=GroundGrabHash(sourcePath)==sourceHash&&GroundGrabHash(raw)==modelHash;receipt.airborneGrabUnchanged=GroundGrabHash(aerialPath)==airHash;
                    receipt.sourcePreservationCheck="executed after pose gate; exact raw Idle/model and airborne Grab hashes compared";
                    if(!receipt.sourceBytesUnchanged||!receipt.airborneGrabUnchanged)throw new InvalidOperationException("GroundGrab changed a real original source or airborne Grab.");
                    string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"GroundGrabAuthoring");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));}
                }
                finally{if(generated!=null)UnityEngine.Object.DestroyImmediate(generated);UnityEngine.Object.DestroyImmediate(model);}
            }
            InstallGroundGrabAbility(species,output);
            if(GroundGrabHash(sourcePath)!=sourceHash||GroundGrabHash(aerialPath)!=airHash||GroundGrabHash(raw)!=modelHash)throw new InvalidOperationException("Selected GroundGrab upgrade must preserve original sources and aerial Grab.");
        }
        static AnimationClip CreateGroundGrabClip(GameObject model,AnimationClip source,float worldScale,out GroundGrabReceipt receipt)
        {
            const float duration=2;var output=new AnimationClip{name="GroundGrab",frameRate=30,legacy=false,wrapMode=WrapMode.ClampForever};var bindings=CopyRestReferenceCurves(output,source,duration);
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(DragonRoot+"Dragon Inferno Cute Series/Prefabs/Dragon Inferno.prefab");var reference=NamedRig(original);var names=NamedRig(model);
            foreach(var pair in names)if(reference.TryGetValue(pair.Key,out var bone))pair.Value.SetLocalPositionAndRotation(bone.localPosition,bone.localRotation);
            var joints=RestJoints(model);source.SampleAnimation(model,0);var neutral=joints.ToDictionary(bone=>bone,bone=>new JointPose(bone));
            float floor=FootFloor(model,names),height=Height(model,names["RigHead"].position)-floor;Vector3 origin=model.transform.position,up=model.transform.up,forward=model.transform.forward,right=model.transform.right;
            var originalFeet=new Dictionary<string,Vector3>();var footRotations=new Dictionary<string,Quaternion>();var palmTargets=new Dictionary<string,Vector3>();
            foreach(string side in new[]{"L","R"})
            {
                originalFeet[side]=names["Rig"+side+"LegAnkle"].position;footRotations[side]=names["Rig"+side+"LegAnkle"].rotation;
                foreach(string part in new[]{"LegToes1","LegToes2"})if(names.TryGetValue("Rig"+side+part,out var toe))footRotations[side+part]=toe.rotation;
                var local=model.transform.InverseTransformPoint(names["Rig"+side+"ArmPalm"].position);local.y=floor+.65f/Mathf.Max(.001f,worldScale);local.z+=height*(side=="L"?.08f:-.025f);palmTargets[side]=model.transform.TransformPoint(local);
            }
            // Reuse the proven actual-rig articulated crouch foundation, then
            // remove its sleeping head/tail/wing pose. This is a reaching action,
            // with open eyes and planted legs, not a Sleep/Idle alias.
            PoseInfernoRest(model,names,floor,height);
            foreach(var bone in joints)if(bone.name.StartsWith("RigNeck",StringComparison.Ordinal)||bone.name=="RigHead"||bone.name=="RigJaw"||bone.name.StartsWith("RigTail",StringComparison.Ordinal)||bone.name.Contains("Wing"))neutral[bone].Apply(bone);
            var head=names["RigHead"];var headGoal=palmTargets["L"]+up*height*.30f+forward*height*.08f;
            foreach(int iteration in Enumerable.Range(0,14))foreach(var neck in new[]{names["RigNeck3"],names["RigNeck2"],names["RigNeck1"]})neck.rotation=Quaternion.FromToRotation(head.position-neck.position,headGoal-neck.position)*neck.rotation;
            // V163's lowest vertex115 is100%RigJaw. The arm reach does not
            // require lowering the face into the floor: retain a deliberate
            // forward/up head orientation after the real neck-chain solve.
            Aim(head,names["RigJaw"],forward+up*.12f);
            foreach(string side in new[]{"L","R"})
            {
                float sign=side=="L"?-1:1;SolveTwoBone(names["Rig"+side+"Leg1"],names["Rig"+side+"Leg2"],names["Rig"+side+"LegAnkle"],originalFeet[side],right*sign+forward*.2f);names["Rig"+side+"LegAnkle"].rotation=footRotations[side];
                foreach(string part in new[]{"LegToes1","LegToes2"})if(names.TryGetValue("Rig"+side+part,out var toe))toe.rotation=footRotations[side+part];
                SolveTwoBone(names["Rig"+side+"Arm1"],names["Rig"+side+"Arm2"],names["Rig"+side+"ArmPalm"],palmTargets[side],right*sign-forward*.2f);AimAxis(names["Rig"+side+"ArmPalm"],forward-up*.04f);
            }
            // Raise only articulated tail joints when the selective crouch
            // would otherwise drag their actual chain below the support plane.
            foreach(var tail in names.Values.Where(bone=>bone.name.StartsWith("RigTail",StringComparison.Ordinal)).OrderBy(Depth))
                if(tail.childCount>0){var child=tail.GetChild(0);var direction=child.position-tail.position;if(Height(model,child.position)<floor+height*.02f)Aim(tail,child,Vector3.ProjectOnPlane(direction,up)+up*direction.magnitude*.25f);}
            var reach=joints.ToDictionary(bone=>bone,bone=>new JointPose(bone));var rotations=joints.ToDictionary(bone=>bone,bone=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()});var positions=joints.ToDictionary(bone=>bone,bone=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()});
            receipt=new GroundGrabReceipt{source=AssetDatabase.GetAssetPath(source),sourceHash=GroundGrabHash(AssetDatabase.GetAssetPath(source))};Vector3 rootOrigin=model.transform.position;
            for(int sample=0;sample<=60;sample++)
            {
                float t=sample/60f,time=t*duration;foreach(var pair in neutral)pair.Value.Apply(pair.Key);source.SampleAnimation(model,t*source.length);
                var sourcePalms=new Dictionary<string,Vector3>();var sourceFeet=new Dictionary<string,Vector3>();var sourceFootRotations=new Dictionary<string,Quaternion>();foreach(string side in new[]{"L","R"}){sourcePalms[side]=names["Rig"+side+"ArmPalm"].position;sourceFeet[side]=names["Rig"+side+"LegAnkle"].position;sourceFootRotations[side]=names["Rig"+side+"LegAnkle"].rotation;foreach(string part in new[]{"LegToes1","LegToes2"})if(names.TryGetValue("Rig"+side+part,out var toe))sourceFootRotations[side+part]=toe.rotation;}
                float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.32f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.98f,t)));
                foreach(var bone in joints)bone.SetLocalPositionAndRotation(Vector3.Lerp(bone.localPosition,reach[bone].position,weight),Quaternion.Slerp(bone.localRotation,reach[bone].rotation,weight));
                foreach(string side in new[]{"L","R"})
                {
                    float sign=side=="L"?-1:1;SolveTwoBone(names["Rig"+side+"Leg1"],names["Rig"+side+"Leg2"],names["Rig"+side+"LegAnkle"],sourceFeet[side],right*sign+forward*.2f);names["Rig"+side+"LegAnkle"].rotation=sourceFootRotations[side];
                    // V162's sole vertex265 is100%RigRLegToes2. Preserve the
                    // real source foot-chain world orientation after hindlimb
                    // IK, rather than inheriting the crouch's toe rotation.
                    foreach(string part in new[]{"LegToes1","LegToes2"})if(names.TryGetValue("Rig"+side+part,out var toe))toe.rotation=sourceFootRotations[side+part];
                    SolveTwoBone(names["Rig"+side+"Arm1"],names["Rig"+side+"Arm2"],names["Rig"+side+"ArmPalm"],Vector3.Lerp(sourcePalms[side],palmTargets[side],weight),right*sign-forward*.2f);
                }
                // V165 vertex592 is shared by real Thumb2/Thumb3: generic
                // local-Z curl sent that finger below the floor. Articulate
                // the actual phalanx axes upward around the ground-level prey
                // instead; the palm target and planted limbs remain unchanged.
                PoseGroundGrabFingers(names,forward,right,up,weight);
                if(Mathf.Abs(t-.45f)<.001f)
                {
                    receipt.palmHeightAtContact=(Height(model,names["RigLArmPalm"].position)-floor)*worldScale;receipt.gripMotorLocalAtContact=model.transform.InverseTransformPoint(names["RigLArmPalm"].position);receipt.lowestSkinAtContact=(GroundGrabLowestSkin(model,receipt)-floor)*worldScale;
                    if(receipt.palmHeightAtContact<.2f||receipt.palmHeightAtContact>1.5f||receipt.lowestSkinAtContact<-.15f)
                    {
                        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"GroundGrabAuthoring");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"rejected-contact.json"),JsonUtility.ToJson(receipt,true));}
                        throw new InvalidOperationException("Actual grounded articulated contact gate failed before saving: palmHeight="+receipt.palmHeightAtContact+" lowestSkin="+receipt.lowestSkinAtContact+" root="+model.transform.position);
                    }
                }
                receipt.maximumRootDisplacement=Mathf.Max(receipt.maximumRootDisplacement,Vector3.Distance(rootOrigin,model.transform.position));
                foreach(var bone in joints){var q=bone.localRotation;var p=bone.localPosition;var r=rotations[bone];var v=positions[bone];r[0].AddKey(time,q.x);r[1].AddKey(time,q.y);r[2].AddKey(time,q.z);r[3].AddKey(time,q.w);v[0].AddKey(time,p.x);v[1].AddKey(time,p.y);v[2].AddKey(time,p.z);}
            }
            if(receipt.maximumRootDisplacement>.00001f)throw new InvalidOperationException("GroundGrab may articulate real bones but cannot move the motor/root.");
            foreach(var bone in joints)
            {
                string path=AnimationUtility.CalculateTransformPath(bone,model.transform);foreach(var binding in bindings.Where(binding=>binding.path==path&&binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0))AnimationUtility.SetEditorCurve(output,binding,null);
                string[] axes={"x","y","z","w"};for(int axis=0;axis<4;axis++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+axes[axis]),rotations[bone][axis]);for(int axis=0;axis<3;axis++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalPosition."+axes[axis]),positions[bone][axis]);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(output);settings.loopTime=false;settings.loopBlend=false;AnimationUtility.SetAnimationClipSettings(output,settings);output.EnsureQuaternionContinuity();return output;
        }
        static void PoseGroundGrabFingers(Dictionary<string,Transform> names,Vector3 forward,Vector3 right,Vector3 up,float weight)
        {
            foreach(string side in new[]{"L","R"})foreach(string finger in new[]{"Thumb","Index","Pinky"})
            {
                float inward=side=="L"?1:-1;
                for(int joint=1;joint<=3;joint++)
                {
                    if(!names.TryGetValue("Rig"+side+"Arm"+finger+joint,out var bone))continue;
                    Quaternion before=bone.rotation;
                    Vector3 direction=forward+right*inward*(finger=="Thumb"?.4f:.12f)+up*(.25f+joint*.12f);
                    if(joint<3&&names.TryGetValue("Rig"+side+"Arm"+finger+(joint+1),out var child))Aim(bone,child,direction);
                    else AimAxis(bone,direction);
                    bone.rotation=Quaternion.Slerp(before,bone.rotation,weight);
                }
            }
        }
        static float GroundGrabLowestSkin(GameObject model,GroundGrabReceipt receipt)
        {
            var baked=new Mesh();float lowest=float.PositiveInfinity;
            try
            {
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if(skin.sharedMesh==null)continue;baked.Clear();skin.BakeMesh(baked,true);var vertices=baked.vertices;
                    for(int index=0;index<vertices.Length;index++)
                    {
                        Vector3 point=skin.transform.TransformPoint(vertices[index]);float height=Height(model,point);if(height>=lowest)continue;
                        lowest=height;receipt.lowestMesh=AssetDatabase.GetAssetPath(skin.sharedMesh);receipt.lowestVertex=index;receipt.lowestActualModelPoint=model.transform.InverseTransformPoint(point);
                        var influence=skin.sharedMesh.boneWeights[index];int[] indices={influence.boneIndex0,influence.boneIndex1,influence.boneIndex2,influence.boneIndex3};receipt.lowestWeights=new[]{influence.weight0,influence.weight1,influence.weight2,influence.weight3};receipt.lowestBones=indices.Select(value=>value<skin.bones.Length&&skin.bones[value]!=null?skin.bones[value].name:"missing").ToArray();
                    }
                }
                return lowest;
            }
            finally{UnityEngine.Object.DestroyImmediate(baked);}
        }
        static void InstallGroundGrabAbility(DragonSpeciesDefinition species,AnimationClip clip)
        {
            string folder="Assets/TruckDragonKingdom/Prefabs/Dragons/"+species.id,path=folder+"/"+species.id+" Dragon Animal.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var motor=root.GetComponentInChildren<DragonAnimalMotor>(true);var aerial=motor.actionBindings.Single(value=>value.action==DragonAnimationAction.Grab);var mode=motor.animal.modes.Single(value=>value.ID==aerial.mode);int ability=(int)DragonAnimationAction.GroundGrab+1;
                var controller=motor.animator.runtimeAnimatorController as AnimatorController;if(controller==null||!AssetDatabase.GetAssetPath(controller).StartsWith(folder+"/",StringComparison.Ordinal))throw new InvalidOperationException("Preserve designer/override controller; explicit owned compatible controller required.");
                var machine=controller.layers.Single(value=>value.name=="Dragon Actions").stateMachine;var state=machine.states.Select(value=>value.state).SingleOrDefault(value=>value.tag=="DragonAction_GroundGrab");bool prefabChanged=false,controllerChanged=false;
                if(state!=null&&state.motion!=clip)throw new InvalidOperationException("Preserve alternate designer GroundGrab state.");
                if(!mode.Abilities.Any(value=>value.Index.Value==ability))
                {
                    var limits=JsonUtility.FromJson<ModeProperties>("{}");limits.affect=AffectStates.Include;limits.affectStates=new List<StateID>{motor.idleState,motor.groundState};limits.Stances=new List<StanceID>();limits.TransitionFrom=new List<int>();
                    mode.Abilities.Add(new Ability{Name="GroundGrab",Index=new MalbersAnimations.Scriptables.IntReference(ability),Limits=limits,Status=AbilityStatus.PlayOnce,MultiplierPosition=new MalbersAnimations.Scriptables.FloatReference(0),MultiplierRotation=new MalbersAnimations.Scriptables.FloatReference(1),CoolDown=new MalbersAnimations.Scriptables.FloatReference(.35f)});prefabChanged=true;
                }
                if(state==null)
                {
                    state=machine.AddState("GroundGrab");state.tag="DragonAction_GroundGrab";state.motion=clip;state.speed=1;state.writeDefaultValues=true;
                    var callback=state.AddStateMachineBehaviour<ModeBehaviour>();callback.ModeID=aerial.mode;callback.EnterMode=callback.ExitMode=callback.OnModeUpdate=true;callback.NoExitTransitions=true;callback.ExitTime=.98f;callback.ExitAbility=-1;
                    var entry=machine.AddAnyStateTransition(state);entry.hasExitTime=false;entry.duration=.12f;entry.canTransitionToSelf=false;entry.AddCondition(AnimatorConditionMode.Equals,Mathf.Abs(aerial.mode.ID)*1000+ability,"Mode");entry.AddCondition(AnimatorConditionMode.If,0,"ModeOn");controllerChanged=true;
                }
                if(!motor.actionBindings.Any(value=>value.action==DragonAnimationAction.GroundGrab)){motor.actionBindings=motor.actionBindings.Concat(new[]{new DragonMotorActionBinding{action=DragonAnimationAction.GroundGrab,mode=aerial.mode,abilityIndex=ability,contactNormalizedTime=.45f,continuous=false}}).ToArray();prefabChanged=true;}
                if(controllerChanged){EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);}
                if(prefabChanged){EditorUtility.SetDirty(motor);EditorUtility.SetDirty(motor.animal);PrefabUtility.SaveAsPrefabAsset(root,path);}
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static string GroundGrabHash(string path){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(@"\\?\"+Path.GetFullPath(path))).Select(value=>value.ToString("x2")));}
    }
}
#endif
