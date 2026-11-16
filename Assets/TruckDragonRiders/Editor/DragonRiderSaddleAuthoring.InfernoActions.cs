#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        const string InfernoMountedFolder="Assets/TruckDragonRiders/Animations/dragon-inferno";
        const string InfernoMountedControllerPath=InfernoMountedFolder+"/Mounted Actions.overrideController";
        struct SavedJoint
        {
            public Transform joint;public Vector3 position,scale;public Quaternion rotation;
            public void Restore(){joint.SetLocalPositionAndRotation(position,rotation);joint.localScale=scale;}
        }
        [Serializable]sealed class MountedActionReceipt
        {
            public string scope="EXPLICIT RIDERS-ONLY ACTUAL RIG TORSO RETARGET; source root/wings/limbs and nonselected tail channels preserved; where worldAimVerified=true, connected terminal neck/head rotations preserve original calibrated world muzzle AIM, not absolute muzzle origin. Native contact/render/fit acceptance remains separate.",species,action,source,reference,output;
            public string[] supportBones;
            public int preservedCurves,samples,authoredSamplesPerSecond,verificationSamples;
            public float maximumWorldAimError;
            public bool neckTranslationsUnchanged,sourceBytesUnchanged,worldAimVerified;
            public string recipe,tailRootRotationReference;public bool tailRootTranslationsUnchanged;public float maximumReferenceTailRootRotationError;
        }
        static int JointDepth(Transform value){int depth=0;for(;value!=null;value=value.parent)depth++;return depth;}
        static bool RotationBinding(EditorCurveBinding binding)=>binding.type==typeof(Transform)&&(binding.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0);
        static Quaternion ContinuousRotation(Quaternion value,Quaternion previous)
        {return Quaternion.Dot(value,previous)<0?new Quaternion(-value.x,-value.y,-value.z,-value.w):value;}
        static AnimationClip CreateMountedSupportAction(DragonSpeciesDefinition species,AnimationClip source,AnimationClip reference,DragonAnimationAction action,out MountedActionReceipt receipt)
        {
            bool preserveAim=action==DragonAnimationAction.Breath||action==DragonAnimationAction.FlyIdle;
            bool preserveFireHoverTail=species.id=="dragon-fire"&&action==DragonAnimationAction.FlyIdle;
            var root=PrefabUtility.LoadPrefabContents(ActorPath(species.id));
            AnimationClip output=null;
            try
            {
                root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var actor=root.GetComponent<DragonCharacter>();var model=actor.motor.transform;actor.motor.animator.enabled=false;
                var joints=model.GetComponentsInChildren<Transform>(true);
                var saved=joints.Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();
                var rig=joints.Single(value=>value.name=="Rig");
                var support=TakeoffSupportBones.Select(name=>joints.Single(value=>value.name==name)).OrderBy(JointDepth).ToArray();
                var head=joints.Single(value=>value.name=="RigHead");var neck=head.parent;
                var tailRoot=preserveFireHoverTail?joints.Single(value=>value.name=="RigTail1"):null;
                if(neck==null||!neck.name.StartsWith("RigNeck",StringComparison.Ordinal)||actor.mouth==null||!actor.mouth.IsChildOf(head))throw new InvalidOperationException("Actual connected adult neck/head/jaw and calibrated mouth required for mounted action retarget.");
                var corrected=(preserveAim?support.Concat(new[]{neck,head}):support).Concat(preserveFireHoverTail?new[]{tailRoot}:Array.Empty<Transform>()).Distinct().ToArray();
                var paths=corrected.ToDictionary(value=>value,value=>AnimationUtility.CalculateTransformPath(value,model));
                var rotations=corrected.ToDictionary(value=>value,value=>Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray());
                var positions=support.ToDictionary(value=>value,value=>Enumerable.Range(0,3).Select(_=>new AnimationCurve()).ToArray());
                var previous=new Dictionary<Transform,Quaternion>();
                output=UnityEngine.Object.Instantiate(source);output.hideFlags=HideFlags.None;output.name="Mounted "+action;
                receipt=new MountedActionReceipt{species=species.id,action=action.ToString(),source=AssetDatabase.GetAssetPath(source),reference=AssetDatabase.GetAssetPath(reference),supportBones=support.Select(value=>value.name).ToArray(),neckTranslationsUnchanged=true,worldAimVerified=preserveAim};
                if(preserveFireHoverTail){receipt.recipe="FIRE_HOVER_V3_FORWARD_SUPPORT_CONNECTED_ORIGINAL_HOVER_AIM_PHASE_MATCHED_FORWARD_TAIL1_ROTATION";receipt.tailRootRotationReference=AssetDatabase.GetAssetPath(reference)+"/RigTail1 rotation only";receipt.tailRootTranslationsUnchanged=true;}
                // At60Hz, zero-tangent quaternion component curves reproduce
                // each key but deviate between them while the original Rig
                // and jaw continue moving. The real V190 midpoint witness was
                // .563 degrees. Densify only this connected aim retarget and
                // use linear component interpolation; source channels and the
                // original clip frameRate/settings remain untouched.
                receipt.authoredSamplesPerSecond=preserveAim?120:60;
                int samples=Mathf.Max(2,Mathf.CeilToInt(source.length*receipt.authoredSamplesPerSecond));receipt.samples=samples+1;
                for(int step=0;step<=samples;step++)
                {
                    float phase=step/(float)samples,time=source.length*phase;
                    foreach(var pose in saved)pose.Restore();reference.SampleAnimation(model.gameObject,reference.length*phase);
                    Vector3 referenceOrigin=model.InverseTransformPoint(rig.position);
                    var offsets=support.ToDictionary(value=>value,value=>model.InverseTransformPoint(value.position)-referenceOrigin);
                    var orientations=support.ToDictionary(value=>value,value=>Quaternion.Inverse(model.rotation)*value.rotation);
                    Quaternion referenceTailRotation=preserveFireHoverTail?tailRoot.localRotation:Quaternion.identity;
                    foreach(var pose in saved)pose.Restore();source.SampleAnimation(model.gameObject,time);
                    Vector3 sourceOrigin=rig.position,sourceAim=model.InverseTransformDirection(actor.mouth.forward),neckPosition=neck.localPosition,headPosition=head.localPosition;
                    Vector3 originalTailTranslation=preserveFireHoverTail?tailRoot.localPosition:Vector3.zero;
                    // Keep the ORIGINAL animated Rig transform/movement. The
                    // articulated load-bearing joints counter its presentation
                    // twist using real forward-flight torso geometry. No root,
                    // limb, wing, unselected tail or mesh vertex is reassigned in runtime.
                    foreach(var joint in support)joint.SetPositionAndRotation(sourceOrigin+model.TransformVector(offsets[joint]),model.rotation*orientations[joint]);
                    if(preserveFireHoverTail){tailRoot.localRotation=referenceTailRotation;if(tailRoot.localPosition!=originalTailTranslation)throw new InvalidOperationException("Selected Fire hover tail root cannot stretch or translate.");}
                    if(preserveAim)
                    {
                        Vector3 desired=model.TransformDirection(sourceAim);
                        var correction=Quaternion.FromToRotation(actor.mouth.forward,desired);
                        neck.rotation=Quaternion.Slerp(Quaternion.identity,correction,.5f)*neck.rotation;
                        head.rotation=Quaternion.FromToRotation(actor.mouth.forward,desired)*head.rotation;
                        receipt.maximumWorldAimError=Mathf.Max(receipt.maximumWorldAimError,Vector3.Angle(actor.mouth.forward,desired));
                        if(!neck.localPosition.Equals(neckPosition)||!head.localPosition.Equals(headPosition))throw new InvalidOperationException("Mounted "+action+" may rotate connected neck/head but cannot stretch/reposition their joints.");
                    }
                    foreach(var joint in corrected)
                    {
                        Quaternion rotation=joint.localRotation;if(previous.TryGetValue(joint,out var before))rotation=ContinuousRotation(rotation,before);previous[joint]=rotation;
                        var curves=rotations[joint];curves[0].AddKey(time,rotation.x);curves[1].AddKey(time,rotation.y);curves[2].AddKey(time,rotation.z);curves[3].AddKey(time,rotation.w);
                    }
                    foreach(var joint in support)
                    {var p=joint.localPosition;var curves=positions[joint];curves[0].AddKey(time,p.x);curves[1].AddKey(time,p.y);curves[2].AddKey(time,p.z);}
                }
                if(preserveAim)
                    foreach(var curve in rotations.Values.Concat(positions.Values).SelectMany(value=>value))
                        for(int key=0;key<curve.length;key++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(curve,key,AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(curve,key,AnimationUtility.TangentMode.Linear);
                        }
                foreach(var binding in AnimationUtility.GetCurveBindings(output))
                    if(paths.Values.Contains(binding.path)&&RotationBinding(binding))AnimationUtility.SetEditorCurve(output,binding,null);
                foreach(var joint in corrected)for(int channel=0;channel<4;channel++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(paths[joint],typeof(Transform),"m_LocalRotation."+"xyzw"[channel]),rotations[joint][channel]);
                foreach(var joint in support)for(int channel=0;channel<3;channel++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(paths[joint],typeof(Transform),"m_LocalPosition."+"xyz"[channel]),positions[joint][channel]);
                var supportPaths=support.Select(value=>paths[value]).ToArray();
                foreach(var binding in AnimationUtility.GetCurveBindings(source))
                {
                    bool changed=supportPaths.Contains(binding.path)&&binding.type==typeof(Transform)&&(RotationBinding(binding)||binding.propertyName.StartsWith("m_LocalPosition.",StringComparison.Ordinal));
                    changed|=preserveAim&&(binding.path==paths[neck]||binding.path==paths[head])&&RotationBinding(binding);
                    changed|=preserveFireHoverTail&&binding.path==paths[tailRoot]&&RotationBinding(binding);
                    if(changed)continue;
                    var a=AnimationUtility.GetEditorCurve(source,binding);var b=AnimationUtility.GetEditorCurve(output,binding);
                    if(b==null||!a.keys.SequenceEqual(b.keys)||a.preWrapMode!=b.preWrapMode||a.postWrapMode!=b.postWrapMode)throw new InvalidOperationException("Mounted action changed unrelated original channel: "+binding.path+"/"+binding.propertyName);receipt.preservedCurves++;
                }
                foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))if(!AnimationUtility.GetObjectReferenceCurve(source,binding).SequenceEqual(AnimationUtility.GetObjectReferenceCurve(output,binding)))throw new InvalidOperationException("Mounted action changed source object curves.");
                // Independent resampling of the actual generated curves checks
                // interpolation, calibrated aim and joint continuity. It never
                // preserves a world mouth origin by moving/stretching bones.
                receipt.verificationSamples=preserveAim?samples*2+1:0;
                if(preserveAim)for(int step=0;step<=samples*2;step++)
                {
                    float time=source.length*step/(samples*2f);foreach(var pose in saved)pose.Restore();source.SampleAnimation(model.gameObject,time);
                    Vector3 desired=model.InverseTransformDirection(actor.mouth.forward),neckPosition=neck.localPosition,headPosition=head.localPosition;
                    Vector3 sourceTailTranslation=preserveFireHoverTail?tailRoot.localPosition:Vector3.zero;
                    foreach(var pose in saved)pose.Restore();output.SampleAnimation(model.gameObject,time);
                    if(preserveFireHoverTail)
                    {
                        Quaternion actual=tailRoot.localRotation;if(tailRoot.localPosition!=sourceTailTranslation)throw new InvalidOperationException("Generated Fire hover changed original Tail1 translation.");foreach(var pose in saved)pose.Restore();reference.SampleAnimation(model.gameObject,reference.length*(time/source.length));float tailError=Quaternion.Angle(actual,tailRoot.localRotation);receipt.maximumReferenceTailRootRotationError=Mathf.Max(receipt.maximumReferenceTailRootRotationError,tailError);if(tailError>.5f)throw new InvalidOperationException("Generated Fire hover lost phase-matched source Forward Tail1 rotation: "+tailError);foreach(var pose in saved)pose.Restore();output.SampleAnimation(model.gameObject,time);
                    }
                    float error=Vector3.Angle(model.InverseTransformDirection(actor.mouth.forward),desired);receipt.maximumWorldAimError=Mathf.Max(receipt.maximumWorldAimError,error);
                    if(error>.5f||!neck.localPosition.Equals(neckPosition)||!head.localPosition.Equals(headPosition))throw new InvalidOperationException("Actual generated mounted "+action+" lost calibrated source world aim or connected neck translations: species="+species.id+" phase="+(time/source.length)+" error="+error);
                }
                var retained=output;output=null;return retained;
            }
            finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);PrefabUtility.UnloadPrefabContents(root);}
        }
        static void ConfigureInfernoMountedActions(DragonCharacter actor,DragonSpeciesDefinition species)
        {
            if(!species.TryGetAnimation(DragonAnimationAction.Fly,out var forward)||!species.TryGetAnimation(DragonAnimationAction.TurnRight,out var turn)||!species.TryGetAnimation(DragonAnimationAction.Breath,out var breath))throw new InvalidOperationException("Actual Inferno forward/turn/directed Breath required for selected mounted action authoring.");
            var baseController=actor.motor.animator.runtimeAnimatorController;
            if(baseController is AnimatorOverrideController previous)
            {if(AssetDatabase.GetAssetPath(previous)!=InfernoMountedControllerPath)throw new InvalidOperationException("Preserve alternate designer Inferno Riders controller.");baseController=previous.runtimeAnimatorController;}
            var watched=new[]{AssetDatabase.GetAssetPath(forward.clip),AssetDatabase.GetAssetPath(turn.clip),AssetDatabase.GetAssetPath(breath.clip)}.Distinct().ToDictionary(path=>path,Hash);
            var turnOutput=RetainExactCarrierClip(InfernoMountedFolder+"/Mounted Turn Right.anim",CreateMountedSupportAction(species,turn.clip,forward.clip,DragonAnimationAction.TurnRight,out var turnReceipt));
            var breathOutput=RetainExactCarrierClip(InfernoMountedFolder+"/Mounted Breath.anim",CreateMountedSupportAction(species,breath.clip,forward.clip,DragonAnimationAction.Breath,out var breathReceipt));
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(InfernoMountedControllerPath);
            if(controller==null){controller=new AnimatorOverrideController(baseController){name="Inferno Riders actual load-bearing actions"};AssetDatabase.CreateAsset(controller,InfernoMountedControllerPath);}
            if(controller.runtimeAnimatorController!=baseController)throw new InvalidOperationException("Preserve alternate saved Inferno mounted base controller.");
            bool changed=false;
            foreach(var pair in new[]{new KeyValuePair<AnimationClip,AnimationClip>(turn.clip,turnOutput),new KeyValuePair<AnimationClip,AnimationClip>(breath.clip,breathOutput)})
            {var prior=controller[pair.Key];if(prior!=null&&prior!=pair.Key&&prior!=pair.Value)throw new InvalidOperationException("Preserve alternate Inferno designer action override.");if(prior!=pair.Value){controller[pair.Key]=pair.Value;changed=true;}RetainMappedWingObservation(actor,pair.Key,pair.Value);}
            if(changed){EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);}
            actor.motor.animator.runtimeAnimatorController=controller;if(PrefabUtility.IsPartOfPrefabInstance(actor.motor.animator))PrefabUtility.RecordPrefabInstancePropertyModifications(actor.motor.animator);
            bool exact=watched.All(pair=>Hash(pair.Key)==pair.Value);if(!exact)throw new InvalidOperationException("Inferno selected mounted actions modified their original sources.");
            turnReceipt.output=AssetDatabase.GetAssetPath(turnOutput);breathReceipt.output=AssetDatabase.GetAssetPath(breathOutput);turnReceipt.sourceBytesUnchanged=breathReceipt.sourceBytesUnchanged=true;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length)
            {string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"mounted-inferno-turn.json"),JsonUtility.ToJson(turnReceipt,true));File.WriteAllText(Path.Combine(folder,"mounted-inferno-breath.json"),JsonUtility.ToJson(breathReceipt,true));}
        }
        static void RetainMappedWingObservation(DragonCharacter actor,AnimationClip original,AnimationClip mapped)
        {
            foreach(var audio in actor.GetComponentsInChildren<DragonWingAudio>(true))
                if(audio.flappingClips!=null&&audio.flappingClips.Contains(original)&&!audio.flappingClips.Contains(mapped))
                {audio.flappingClips=audio.flappingClips.Concat(new[]{mapped}).ToArray();if(PrefabUtility.IsPartOfPrefabInstance(audio))PrefabUtility.RecordPrefabInstancePropertyModifications(audio);}
        }
    }
}
#endif
