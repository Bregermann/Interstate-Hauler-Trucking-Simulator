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
        const string FireHoverFolder="Assets/TruckDragonRiders/Animations/dragon-fire";
        const string FireHoverPath=FireHoverFolder+"/Mounted Hover.anim";
        const string FireHoverControllerPath=FireHoverFolder+"/Mounted Hover.overrideController";
        const string FireTakeoffPath=FireHoverFolder+"/Mounted Takeoff.anim";
        const string FireHoverRecipe="Riders Fire carrier hover: original FlyIdle wings, original FlyForward torso V1";
        // Historical exact recipe is retained solely to recognize the authored
        // V1 derivative during an explicit migration. Never use this disconnected
        // numbered-neck/head selection to create the active mounted posture.
        static readonly HashSet<string> CarrierTorsoBones=new HashSet<string>(new[]{"Rig","RigPelvis","RigSpine1","RigSpine2","RigRibcage","RigNeck1","RigNeck2","RigNeck3","RigHead","RigJaw"},StringComparer.Ordinal);
        static readonly HashSet<string> TakeoffSupportBones=new HashSet<string>(new[]{"RigPelvis","RigSpine1","RigSpine2","RigRibcage"},StringComparer.Ordinal);
        public static AnimationClip EffectiveRiderClip(DragonCharacter actor,AnimationClip original)
        {
            if(actor.motor.animator.runtimeAnimatorController is not AnimatorOverrideController controller)return original;
            var entries=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(entries);
            foreach(var pair in entries)if(pair.Key==original)return pair.Value!=null?pair.Value:original;
            return original;
        }
        static AnimationClip CreateLegacyCarrierHoverClip(AnimationClip hover,AnimationClip forward)
        {
            var output=new AnimationClip{name=FireHoverRecipe,frameRate=hover.frameRate,legacy=false,wrapMode=WrapMode.Loop};
            foreach(var binding in AnimationUtility.GetCurveBindings(hover))AnimationUtility.SetEditorCurve(output,binding,AnimationUtility.GetEditorCurve(hover,binding));
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(hover))AnimationUtility.SetObjectReferenceCurve(output,binding,AnimationUtility.GetObjectReferenceCurve(hover,binding));
            foreach(var binding in AnimationUtility.GetCurveBindings(forward))
            {
                if(!CarrierTorsoBones.Contains(binding.path.Split('/').Last()))continue;
                var source=AnimationUtility.GetEditorCurve(forward,binding);if(source==null)continue;
                float timeScale=hover.length/Mathf.Max(.001f,forward.length);var keys=source.keys;
                for(int index=0;index<keys.Length;index++){var key=keys[index];key.time*=timeScale;key.inTangent/=timeScale;key.outTangent/=timeScale;keys[index]=key;}
                AnimationUtility.SetEditorCurve(output,binding,new AnimationCurve(keys){preWrapMode=source.preWrapMode,postWrapMode=source.postWrapMode});
            }
            var settings=AnimationUtility.GetAnimationClipSettings(hover);settings.loopTime=true;settings.loopBlend=true;AnimationUtility.SetAnimationClipSettings(output,settings);return output;
        }
        static AnimationClip CreateCarrierHoverClip(DragonSpeciesDefinition species,AnimationClip hover,AnimationClip forward,out MountedActionReceipt receipt)
        {
            var output=CreateMountedSupportAction(species,hover,forward,DragonAnimationAction.FlyIdle,out receipt);output.name="Mounted Hover";return output;
        }
        static void UpgradeKnownFireHoverForExplicitSaddleMigration(DragonSpeciesCatalog catalog)
        {
            var output=AssetDatabase.LoadAssetAtPath<AnimationClip>(FireHoverPath);if(output==null)return;
            if(!catalog.TryGet("dragon-fire",out var species)||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var hover)||!species.TryGetAnimation(DragonAnimationAction.Fly,out var forward))throw new InvalidOperationException("Actual Fire sources required for explicit known-hover migration.");
            AnimationClip expected=null,legacy=null;string guid=AssetDatabase.AssetPathToGUID(FireHoverPath),sourceHover=Hash(AssetDatabase.GetAssetPath(hover.clip)),sourceForward=Hash(AssetDatabase.GetAssetPath(forward.clip));
            try
            {
                expected=CreateCarrierHoverClip(species,hover.clip,forward.clip,out var receipt);
                if(CarrierClipMismatch(expected,output)==null)return;
                legacy=CreateLegacyCarrierHoverClip(hover.clip,forward.clip);
                string mismatch=CarrierClipMismatch(legacy,output);if(mismatch!=null)throw new InvalidOperationException("Preserve customized Fire hover: explicit V3 migration only accepts byte-equivalent V1 recipe channels/settings. "+mismatch);
                EditorUtility.CopySerialized(expected,output);EditorUtility.SetDirty(output);AssetDatabase.SaveAssetIfDirty(output);
                if(AssetDatabase.AssetPathToGUID(FireHoverPath)!=guid||CarrierClipMismatch(expected,output)!=null)throw new InvalidOperationException("Explicit known V1 hover migration lost its existing GUID or exact V3 recipe.");
                if(Hash(AssetDatabase.GetAssetPath(hover.clip))!=sourceHover||Hash(AssetDatabase.GetAssetPath(forward.clip))!=sourceForward)throw new InvalidOperationException("Explicit hover migration changed original source motion.");
                receipt.output=FireHoverPath;receipt.sourceBytesUnchanged=true;WriteFireHoverReceipt(receipt);
                Debug.Log("RIDERS_FIRE_HOVER_EXPLICIT_V1_TO_V3 originalSourcesExact=true existingGuidPreserved="+guid+" connectedOriginalHoverAimMaximum="+receipt.maximumWorldAimError);
            }
            finally{if(expected!=null)UnityEngine.Object.DestroyImmediate(expected);if(legacy!=null)UnityEngine.Object.DestroyImmediate(legacy);}
        }
        static void WriteFireHoverReceipt(MountedActionReceipt receipt)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;
            string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"mounted-fire-hover-v3.json"),JsonUtility.ToJson(receipt,true));
        }
        static AnimationClip CreateCarrierTakeoffClip(AnimationClip takeoff,AnimationClip forward)
        {
            var output=new AnimationClip{name="Mounted Takeoff",frameRate=takeoff.frameRate,legacy=false,wrapMode=takeoff.wrapMode};
            foreach(var binding in AnimationUtility.GetCurveBindings(takeoff))AnimationUtility.SetEditorCurve(output,binding,AnimationUtility.GetEditorCurve(takeoff,binding));
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(takeoff))AnimationUtility.SetObjectReferenceCurve(output,binding,AnimationUtility.GetObjectReferenceCurve(takeoff,binding));
            // Preserve actual root, head/muzzle, wings, tail and every limb.
            // Only the four named load-bearing torso joints use the proven
            // real forward-flight posture; the ordinary TakeOff state/tag,
            // duration, root motion and vendor transition remain authoritative.
            foreach(var binding in AnimationUtility.GetCurveBindings(forward))
            {
                if(!TakeoffSupportBones.Contains(binding.path.Split('/').Last()))continue;
                var source=AnimationUtility.GetEditorCurve(forward,binding);if(source==null)continue;
                float timeScale=takeoff.length/Mathf.Max(.001f,forward.length);var keys=source.keys;
                for(int index=0;index<keys.Length;index++){var key=keys[index];key.time*=timeScale;key.inTangent/=timeScale;key.outTangent/=timeScale;keys[index]=key;}
                AnimationUtility.SetEditorCurve(output,binding,new AnimationCurve(keys){preWrapMode=source.preWrapMode,postWrapMode=source.postWrapMode});
            }
            AnimationUtility.SetAnimationClipSettings(output,AnimationUtility.GetAnimationClipSettings(takeoff));AnimationUtility.SetAnimationEvents(output,AnimationUtility.GetAnimationEvents(takeoff));return output;
        }
        static AnimationClip RetainExactCarrierClip(string path,AnimationClip expected,AnimationClip knownLegacy=null)
        {
            try
            {
                var output=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(output!=null)
                {
                    string mismatch=CarrierClipMismatch(expected,output);
                    if(mismatch!=null)
                    {
                        if(knownLegacy==null||CarrierClipMismatch(knownLegacy,output)!=null)throw new InvalidOperationException("Preserve alternate saved carrier animation; only the exact known prior recipe may explicitly migrate: "+path+" / "+mismatch);
                        string guid=AssetDatabase.AssetPathToGUID(path);EditorUtility.CopySerialized(expected,output);EditorUtility.SetDirty(output);AssetDatabase.SaveAssetIfDirty(output);if(AssetDatabase.AssetPathToGUID(path)!=guid||CarrierClipMismatch(expected,output)!=null)throw new InvalidOperationException("Known carrier-tail migration changed GUID or final recipe.");
                    }
                    return output;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();AssetDatabase.CreateAsset(expected,path);var retained=expected;expected=null;return retained;
            }
            finally{if(expected!=null)UnityEngine.Object.DestroyImmediate(expected);if(knownLegacy!=null)UnityEngine.Object.DestroyImmediate(knownLegacy);}
        }
        static string CarrierClipMismatch(AnimationClip expected,AnimationClip actual)
        {
            // Unity can normalize a generated clip's Object.name to the file
            // name on import. Provenance is its complete recipe data instead.
            if(actual.legacy||actual.frameRate!=expected.frameRate||actual.length!=expected.length||actual.wrapMode!=expected.wrapMode)return "clip duration/rate/type/wrap";
            if(JsonUtility.ToJson(AnimationUtility.GetAnimationClipSettings(actual))!=JsonUtility.ToJson(AnimationUtility.GetAnimationClipSettings(expected)))return "actual animation settings";
            var expectedBindings=AnimationUtility.GetCurveBindings(expected);var actualBindings=AnimationUtility.GetCurveBindings(actual);
            if(!new HashSet<EditorCurveBinding>(expectedBindings).SetEquals(actualBindings))return "actual curve binding set";
            foreach(var binding in expectedBindings)
            {
                var a=AnimationUtility.GetEditorCurve(expected,binding);var b=AnimationUtility.GetEditorCurve(actual,binding);
                if(b==null||!a.keys.SequenceEqual(b.keys)||a.preWrapMode!=b.preWrapMode||a.postWrapMode!=b.postWrapMode)return "actual curve "+binding.path+"/"+binding.propertyName;
            }
            var expectedObjects=AnimationUtility.GetObjectReferenceCurveBindings(expected);var actualObjects=AnimationUtility.GetObjectReferenceCurveBindings(actual);
            if(!new HashSet<EditorCurveBinding>(expectedObjects).SetEquals(actualObjects))return "actual object binding set";
            foreach(var binding in expectedObjects)if(!AnimationUtility.GetObjectReferenceCurve(expected,binding).SequenceEqual(AnimationUtility.GetObjectReferenceCurve(actual,binding)))return "actual object curve "+binding.path;
            var actualEvents=AnimationUtility.GetAnimationEvents(actual);var expectedEvents=AnimationUtility.GetAnimationEvents(expected);if(actualEvents.Length!=expectedEvents.Length)return "actual events";
            for(int index=0;index<actualEvents.Length;index++)
            {var a=actualEvents[index];var b=expectedEvents[index];if(a.time!=b.time||a.functionName!=b.functionName||a.stringParameter!=b.stringParameter||a.floatParameter!=b.floatParameter||a.intParameter!=b.intParameter||a.objectReferenceParameter!=b.objectReferenceParameter||a.messageOptions!=b.messageOptions)return "actual event payload "+index;}
            return null;
        }
        static void ConfigureRiderHover(DragonCharacter actor,DragonSpeciesDefinition species)
        {
            if(species.id=="dragon-inferno"){ConfigureInfernoMountedActions(actor,species);return;}
            if(species.id!="dragon-fire")return;
            if(!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var hover)||!species.TryGetAnimation(DragonAnimationAction.Fly,out var forward))throw new InvalidOperationException("Actual owned Fire hover/forward clips required for mounted posture.");
            if(!species.TryGetAnimation(DragonAnimationAction.Takeoff,out var takeoff))throw new InvalidOperationException("Actual owned Fire Takeoff required for mounted launch.");
            var baseController=actor.motor.animator.runtimeAnimatorController;
            if(baseController is AnimatorOverrideController prior)
            {
                if(AssetDatabase.GetAssetPath(prior)!=FireHoverControllerPath)throw new InvalidOperationException("Preserve alternate designer Riders controller; explicitly select its mounted posture.");
                baseController=prior.runtimeAnimatorController;
            }
            string hoverSource=AssetDatabase.GetAssetPath(hover.clip),forwardSource=AssetDatabase.GetAssetPath(forward.clip);string hoverHash=Hash(hoverSource),forwardHash=Hash(forwardSource);
            var output=AssetDatabase.LoadAssetAtPath<AnimationClip>(FireHoverPath);
            var expected=CreateCarrierHoverClip(species,hover.clip,forward.clip,out var hoverReceipt);
            try
            {
                if(output==null)
                {
                    Directory.CreateDirectory(FireHoverFolder);AssetDatabase.Refresh();AssetDatabase.CreateAsset(expected,FireHoverPath);output=expected;expected=null;
                }
                else
                {
                    string mismatch=CarrierClipMismatch(expected,output);
                    if(mismatch!=null)throw new InvalidOperationException("Preserve alternate saved carrier animation; no implicit regeneration: "+mismatch);
                }
            }
            finally{if(expected!=null)UnityEngine.Object.DestroyImmediate(expected);}
            hoverReceipt.output=FireHoverPath;hoverReceipt.sourceBytesUnchanged=Hash(hoverSource)==hoverHash&&Hash(forwardSource)==forwardHash;WriteFireHoverReceipt(hoverReceipt);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(FireHoverControllerPath);
            if(controller==null)
            {
                controller=new AnimatorOverrideController(baseController){name="Fire Riders actual mounted hover"};var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(overrides);
                bool found=false;for(int index=0;index<overrides.Count;index++)if(overrides[index].Key==hover.clip){overrides[index]=new KeyValuePair<AnimationClip,AnimationClip>(hover.clip,output);found=true;}
                if(!found)throw new InvalidOperationException("Compatible actual Fire controller has no original hover clip binding.");controller.ApplyOverrides(overrides);AssetDatabase.CreateAsset(controller,FireHoverControllerPath);
            }
            if(controller.runtimeAnimatorController!=baseController||controller[hover.clip]!=output)throw new InvalidOperationException("Preserve alternate saved Riders override mapping; explicit compatible hover binding required.");
            string takeoffSource=AssetDatabase.GetAssetPath(takeoff.clip),takeoffHash=Hash(takeoffSource);
            var takeoffLegacy=CreateCarrierTakeoffClip(takeoff.clip,forward.clip);var takeoffExpected=UnityEngine.Object.Instantiate(takeoffLegacy);var takeoffReceipt=new MountedActionReceipt{species=species.id,action="Takeoff",source=takeoffSource,reference=forwardSource,neckTranslationsUnchanged=true};ApplySelectedForwardTailRoot(species,takeoff.clip,forward.clip,takeoffExpected,takeoffReceipt);var takeoffOutput=RetainExactCarrierClip(FireTakeoffPath,takeoffExpected,takeoffLegacy);WriteSelectedFireTailReceipt(takeoffReceipt,FireTakeoffPath);
            var oldTakeoff=controller[takeoff.clip];
            if(oldTakeoff!=null&&oldTakeoff!=takeoff.clip&&oldTakeoff!=takeoffOutput)throw new InvalidOperationException("Preserve alternate designer Fire Takeoff override; explicit selected carrier mapping required.");
            if(oldTakeoff!=takeoffOutput){controller[takeoff.clip]=takeoffOutput;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);}
            ConfigureFireMountedActions(actor,species,controller);
            actor.motor.animator.runtimeAnimatorController=controller;
            if(PrefabUtility.IsPartOfPrefabInstance(actor.motor.animator))PrefabUtility.RecordPrefabInstancePropertyModifications(actor.motor.animator);
            foreach(var wing in actor.GetComponentsInChildren<DragonWingAudio>(true))
                if(wing.flappingClips!=null)
                {var added=new[]{output,takeoffOutput}.Where(clip=>!wing.flappingClips.Contains(clip)).ToArray();if(added.Length>0){wing.flappingClips=wing.flappingClips.Concat(added).ToArray();if(PrefabUtility.IsPartOfPrefabInstance(wing))PrefabUtility.RecordPrefabInstancePropertyModifications(wing);}}
            // Explicit authored compatible default posture, not a runtime clip
            // command: a fresh paused countdown must not expose the imported
            // unevaluated bind pose. The actual graph uses this same override.
            output.SampleAnimation(actor.motor.gameObject,0);
            foreach(var bone in actor.motor.GetComponentsInChildren<Transform>(true))
                if(PrefabUtility.IsPartOfPrefabInstance(bone))PrefabUtility.RecordPrefabInstancePropertyModifications(bone);
            if(Hash(hoverSource)!=hoverHash||Hash(forwardSource)!=forwardHash||Hash(takeoffSource)!=takeoffHash)throw new InvalidOperationException("Mounted derivatives must preserve all actual owned original source clips.");
        }
    }
}
#endif
