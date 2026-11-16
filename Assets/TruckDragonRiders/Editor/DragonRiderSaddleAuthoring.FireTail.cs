#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        static void ApplySelectedForwardTailRoot(DragonSpeciesDefinition species,AnimationClip source,AnimationClip forward,AnimationClip output,MountedActionReceipt receipt)
        {
            if(species.id!="dragon-fire"||!new[]{"TurnLeft","TurnRight","Land","Breath","Takeoff"}.Contains(receipt.action))throw new InvalidOperationException("Only five native-SAT-proven Fire carrier actions select this tail-root rotation recipe.");
            var sourceHashes=new[]{AssetDatabase.GetAssetPath(source),AssetDatabase.GetAssetPath(forward)}.Distinct().ToDictionary(path=>path,Hash);var root=PrefabUtility.LoadPrefabContents(ActorPath(species.id));
            try
            {
                var actor=root.GetComponent<DragonCharacter>();var model=actor.motor.transform;actor.motor.animator.enabled=false;var tail=model.GetComponentsInChildren<Transform>(true).Single(joint=>joint.name=="RigTail1");string path=AnimationUtility.CalculateTransformPath(tail,model);var saved=model.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();
                var unchanged=AnimationUtility.GetCurveBindings(output).Where(binding=>binding.path!=path||!RotationBinding(binding)).ToDictionary(binding=>binding,binding=>AnimationUtility.GetEditorCurve(output,binding));var rotation=AnimationUtility.GetCurveBindings(forward).Where(binding=>binding.path==path&&RotationBinding(binding)).ToArray();if(rotation.Length==0)throw new InvalidOperationException("Actual original Forward Tail1 rotation channels required; no guessed tail angle.");
                foreach(var binding in AnimationUtility.GetCurveBindings(output).Where(binding=>binding.path==path&&RotationBinding(binding)))AnimationUtility.SetEditorCurve(output,binding,null);
                float timeScale=source.length/forward.length;
                foreach(var binding in rotation)
                {
                    var curve=AnimationUtility.GetEditorCurve(forward,binding);var keys=curve.keys;for(int index=0;index<keys.Length;index++){var key=keys[index];key.time=key.time>=forward.length-.000001f?source.length:key.time*timeScale;key.inTangent/=timeScale;key.outTangent/=timeScale;keys[index]=key;}AnimationUtility.SetEditorCurve(output,binding,new AnimationCurve(keys){preWrapMode=curve.preWrapMode,postWrapMode=curve.postWrapMode});
                }
                foreach(var pair in unchanged){var actual=AnimationUtility.GetEditorCurve(output,pair.Key);if(actual==null||!actual.keys.SequenceEqual(pair.Value.keys)||actual.preWrapMode!=pair.Value.preWrapMode||actual.postWrapMode!=pair.Value.postWrapMode)throw new InvalidOperationException("Selected Tail1 rotation changed a previously accepted torso/root/wing/head/distal-tail channel: "+pair.Key.path+"/"+pair.Key.propertyName);}
                receipt.recipe="FIRE_"+receipt.action.ToUpperInvariant()+"_V2_EXISTING_LOAD_BEARING_RECIPE_WITH_PHASE_MATCHED_FORWARD_TAIL1_ROTATION_ONLY";receipt.tailRootRotationReference=AssetDatabase.GetAssetPath(forward)+"/RigTail1 rotation only";receipt.tailRootTranslationsUnchanged=true;
                int samples=Mathf.Max(2,Mathf.CeilToInt(source.length*240));
                for(int step=0;step<=samples;step++)
                {
                    float phase=step/(float)samples;foreach(var pose in saved)pose.Restore();forward.SampleAnimation(model.gameObject,forward.length*phase);Quaternion expected=tail.localRotation;foreach(var pose in saved)pose.Restore();source.SampleAnimation(model.gameObject,source.length*phase);Vector3 translation=tail.localPosition,originalAim=model.InverseTransformDirection(actor.mouth.forward);foreach(var pose in saved)pose.Restore();output.SampleAnimation(model.gameObject,output.length*phase);
                    float error=Quaternion.Angle(tail.localRotation,expected);receipt.maximumReferenceTailRootRotationError=Mathf.Max(receipt.maximumReferenceTailRootRotationError,error);if(error>.5f||!tail.localPosition.Equals(translation))throw new InvalidOperationException("Selected mounted "+receipt.action+" lost actual phase-matched Forward Tail1 rotation or original translation: phase="+phase+" error="+error);
                    if(receipt.action=="Breath"){float aim=Vector3.Angle(model.InverseTransformDirection(actor.mouth.forward),originalAim);receipt.maximumWorldAimError=Mathf.Max(receipt.maximumWorldAimError,aim);if(aim>.5f)throw new InvalidOperationException("Tail-root-only adaptation altered independently calibrated original Breath aim.");}
                }
                receipt.sourceBytesUnchanged=sourceHashes.All(pair=>Hash(pair.Key)==pair.Value);if(!receipt.sourceBytesUnchanged)throw new InvalidOperationException("Selected tail-root derivative modified original source assets.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void WriteSelectedFireTailReceipt(MountedActionReceipt receipt,string output)
        {
            receipt.output=output;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"mounted-fire-"+receipt.action.ToLowerInvariant()+"-tail-v2.json"),JsonUtility.ToJson(receipt,true));
        }
    }
}
#endif
