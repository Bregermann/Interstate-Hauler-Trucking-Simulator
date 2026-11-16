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
        [Serializable] sealed class ConnectedPostureReceipt
        {
            public string scope="READ_ONLY_ACTUAL_CONNECTED_RIG_ROTATION_DERIVATIVE_NO_ROOT_TORSO_TRANSLATION_TRUCK_OR_SAVED_ASSET_CHANGE",species,recipe,reference;
            public float tailModelRightDegrees;
            public bool sourceBytesExact;
            public List<ConnectedPostureClip> clips=new List<ConnectedPostureClip>();
        }
        [Serializable] sealed class ConnectedPostureClip
        {
            public string key,source,selectedBone;
            public int authoredSamples,verificationSamples,preservedCurves;
            public float maximumSelectedRotationChange,maximumAuthoredRotationError;
            public bool translationsScalesAndUnselectedLocalsExact;
        }

        // V304 proves real finite Inferno neck triangles cross the original
        // chassis in ground actions; a projection exemption cannot fix them.
        // Align only the proximal connected joint to the phase-matched safe
        // source-flight orientation. The torso and every joint position keep
        // their original action. Fire's recorded63tail witnesses instead need
        // a downward root rotation: the fixed-box lower-plane solve requires
        // 8.1501degrees including2cm margin, so one bounded10degree candidate
        // is measured through the complete native skin/attachment oracle.
        static UnityEngine.Object[] CreateTransientConnectedPosture(DragonCharacter actor,string id)
        {
            if(actor.motor.animator.runtimeAnimatorController is not AnimatorOverrideController basis||!basis.name.EndsWith("_SAFE_ACTIONS",StringComparison.Ordinal))
                throw new InvalidOperationException("Actual previously authored safe mounted source controller required.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");
            if(catalog==null||!catalog.TryGet(id,out var species)||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral)||!species.TryGetAnimation(DragonAnimationAction.Breath,out var breath))
                throw new InvalidOperationException("Actual source species and mapped neutral/Breath required.");
            var selectedKeys=id=="dragon-inferno"
                ? MountedPostureActions(species).Where(action=>action!=DragonAnimationAction.Breath).Select(action=>{if(!species.TryGetAnimation(action,out var mapping))throw new InvalidOperationException("Missing original mapped action.");return mapping.clip;}).ToHashSet()
                : id=="dragon-fire"?new HashSet<AnimationClip>():throw new InvalidOperationException("Only the two measured original mounted species are supported.");
            var entries=new List<KeyValuePair<AnimationClip,AnimationClip>>();basis.GetOverrides(entries);
            if(id=="dragon-fire")foreach(var entry in entries)if(entry.Value!=null)selectedKeys.Add(entry.Key);
            var watched=AssetDatabase.GetDependencies(ActorPath(id),true).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToDictionary(path=>path,Hash);
            var report=new ConnectedPostureReceipt{species=id,recipe=id=="dragon-inferno"?"NON_FLIGHT_NON_BREATH_CONNECTED_NECK1_PHASE_MATCHED_ACTUAL_SAFE_FLYIDLE_MODEL_WORLD_ORIENTATION":"ALL_MAPPED_CONNECTED_TAIL1_MINUS10_MODEL_RIGHT_ROTATION_ORIGINAL_DISTAL_TAIL_MOTION",tailModelRightDegrees=id=="dragon-fire"?-10:0};
            GameObject visual=null;var created=new List<UnityEngine.Object>();
            try
            {
                visual=UnityEngine.Object.Instantiate(species.visualPrefab);visual.hideFlags=HideFlags.HideAndDontSave;visual.SetActive(false);visual.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                foreach(var animator in visual.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                var model=visual.transform;var joint=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==(id=="dragon-inferno"?"RigNeck1":"RigTail1"));
                var saved=model.GetComponentsInChildren<Transform>(true).Select(value=>new SavedJoint{joint=value,position=value.localPosition,rotation=value.localRotation,scale=value.localScale}).ToArray();
                string path=AnimationUtility.CalculateTransformPath(joint,model);var reference=EffectiveRiderClip(actor,neutral.clip);report.reference=AssetDatabase.GetAssetPath(reference)+"/"+reference.name;
                var next=new AnimatorOverrideController(basis.runtimeAnimatorController){name=basis.name+"_CONNECTED_SAFE_ACTIONS",hideFlags=HideFlags.HideAndDontSave};created.Add(next);
                foreach(var entry in entries)
                {
                    if(!selectedKeys.Contains(entry.Key)){next[entry.Key]=entry.Value;continue;}
                    var source=entry.Value??entry.Key;var output=UnityEngine.Object.Instantiate(source);output.hideFlags=HideFlags.HideAndDontSave;output.name="CONNECTED_"+source.name;created.Add(output);next[entry.Key]=output;
                    var row=new ConnectedPostureClip{key=entry.Key.name,source=AssetDatabase.GetAssetPath(source)+"/"+source.name,selectedBone=path,translationsScalesAndUnselectedLocalsExact=true};report.clips.Add(row);
                    int samples=Mathf.Max(2,Mathf.CeilToInt(source.length*120));row.authoredSamples=samples+1;var curves=Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();Quaternion previous=Quaternion.identity;
                    for(int step=0;step<=samples;step++)
                    {
                        float time=source.length*step/samples;Apply(time,out var originalRotation);
                        row.maximumSelectedRotationChange=Mathf.Max(row.maximumSelectedRotationChange,Quaternion.Angle(joint.localRotation,originalRotation));
                        var rotation=step==0?joint.localRotation:ContinuousRotation(joint.localRotation,previous);previous=rotation;
                        for(int channel=0;channel<4;channel++)curves[channel].AddKey(time,rotation[channel]);
                    }
                    foreach(var binding in AnimationUtility.GetCurveBindings(output).Where(binding=>binding.path==path&&RotationBinding(binding)))AnimationUtility.SetEditorCurve(output,binding,null);
                    for(int channel=0;channel<4;channel++)
                    {
                        var curve=curves[channel];for(int key=0;key<curve.length;key++){AnimationUtility.SetKeyLeftTangentMode(curve,key,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,key,AnimationUtility.TangentMode.Linear);}
                        AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[channel]),curve);
                    }
                    foreach(var binding in AnimationUtility.GetCurveBindings(source))
                    {
                        if(binding.path==path&&RotationBinding(binding))continue;
                        var original=AnimationUtility.GetEditorCurve(source,binding);var actual=AnimationUtility.GetEditorCurve(output,binding);
                        if(actual==null||!original.keys.SequenceEqual(actual.keys)||original.preWrapMode!=actual.preWrapMode||original.postWrapMode!=actual.postWrapMode)
                            throw new InvalidOperationException("Connected posture changed an unselected original curve: "+binding.path+"/"+binding.propertyName);
                        row.preservedCurves++;
                    }
                    foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))if(!AnimationUtility.GetObjectReferenceCurve(source,binding).SequenceEqual(AnimationUtility.GetObjectReferenceCurve(output,binding)))
                        throw new InvalidOperationException("Connected posture changed an original object-reference curve.");
                    row.verificationSamples=samples*2+1;
                    for(int step=0;step<=samples*2;step++)
                    {
                        float time=source.length*step/(samples*2);Apply(time,out _);var expected=joint.localRotation;
                        foreach(var pose in saved)pose.Restore();output.SampleAnimation(model.gameObject,time);
                        row.maximumAuthoredRotationError=Mathf.Max(row.maximumAuthoredRotationError,Quaternion.Angle(joint.localRotation,expected));
                    }
                    if(row.maximumAuthoredRotationError>.5f||!row.translationsScalesAndUnselectedLocalsExact)throw new InvalidOperationException("Connected rotation source derivative failed original curve/position/scale or midpoint rotation verification.");
                    void Apply(float time,out Quaternion originalRotation)
                    {
                        Quaternion referenceOrientation=Quaternion.identity;
                        if(id=="dragon-inferno")
                        {foreach(var pose in saved)pose.Restore();reference.SampleAnimation(model.gameObject,reference.length*time/source.length);referenceOrientation=Quaternion.Inverse(model.rotation)*joint.rotation;}
                        foreach(var pose in saved)pose.Restore();source.SampleAnimation(model.gameObject,time);
                        var before=saved.Select(pose=>new SavedJoint{joint=pose.joint,position=pose.joint.localPosition,rotation=pose.joint.localRotation,scale=pose.joint.localScale}).ToArray();originalRotation=joint.localRotation;
                        if(id=="dragon-inferno")joint.rotation=model.rotation*referenceOrientation;
                        else joint.rotation=Quaternion.AngleAxis(-10,model.right)*joint.rotation;
                        foreach(var pose in before)if(!pose.joint.localPosition.Equals(pose.position)||!pose.joint.localScale.Equals(pose.scale)||pose.joint!=joint&&!pose.joint.localRotation.Equals(pose.rotation))
                        {row.translationsScalesAndUnselectedLocalsExact=false;throw new InvalidOperationException("Connected posture must preserve every original joint position/scale and all unselected local rotations.");}
                    }
                }
                if(report.clips.Count==0)throw new InvalidOperationException("No actual mapped connected posture clips were authored.");
                report.sourceBytesExact=watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);if(!report.sourceBytesExact)throw new InvalidOperationException("Connected posture modified original asset bytes.");
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","ConnectedSkin");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,id+"-connected-posture-curves.json"),JsonUtility.ToJson(report,true));}
                return created.ToArray();
            }
            catch{foreach(var value in created)if(value!=null)UnityEngine.Object.DestroyImmediate(value);throw;}
            finally{if(visual!=null)UnityEngine.Object.DestroyImmediate(visual);}
        }
    }
}
#endif
