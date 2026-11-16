#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    public static partial class DragonSpeciesAuthoring
    {
        const string DirectedBreathVersion="LWS DIRECTED BREATH V1";
        [Serializable] sealed class BreathUpgradeReceipt
        {
            public string scope,species,source,sourceSha256,clip,clipGuid,neckPath,headPath;
            public string[] preservedSourcePaths;
            public int preservedCurves,correctedRotationCurves;
            public float duration,sourceContactAngle,authoredContactAngle,maximumContactWindowAngle;
            public bool originalSourcesUnchanged,unrelatedAuthoredClipsUnchanged;
        }

        // Explicit selected authoring only. Never called by import, play or build.
        public static DragonSpeciesCatalog RegenerateInfernoBreath()=>RegenerateDirectedBreath("dragon-inferno");
        public static DragonSpeciesCatalog RegenerateFireBreath()=>RegenerateDirectedBreath("dragon-fire");
        public static DragonSpeciesCatalog RegenerateDirectedBreath(string speciesId)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for explicit directed Breath generation.");
            string modelName=speciesId=="dragon-inferno"?"Dragon Inferno":speciesId=="dragon-fire"?"Dragon Fire":throw new InvalidOperationException("Only the two inspected actual adult rigs support selected Breath authoring.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(CatalogPath);
            if(catalog==null||!catalog.TryGet(speciesId,out var species)||!species.TryGetAnimation(DragonAnimationAction.Breath,out var mapping))throw new InvalidOperationException("Existing adult species and real Breath mapping required.");
            string rawFolder=DragonRoot+modelName+" Cute Series/";
            var rawModel=AssetDatabase.LoadAssetAtPath<GameObject>(rawFolder+"Prefabs/"+modelName+".prefab");
            string sourcePath=rawFolder+"FBX/"+modelName+"@Cast Spell.FBX";
            var source=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().SingleOrDefault(value=>!value.name.StartsWith("__",StringComparison.Ordinal));
            if(rawModel==null||source==null||source.length<=0)throw new InvalidOperationException("Actual raw adult model and Cast Spell subasset required: "+sourcePath);
            string folder=AuthoredRoot+"/"+speciesId,path=folder+"/Breath.anim";
            string previousPath=AssetDatabase.GetAssetPath(mapping.clip);
            if(previousPath!=sourcePath&&previousPath!=path)throw new InvalidOperationException("Refusing to replace a designer Breath mapping: "+previousPath);
            string configuredFolder="Assets/TruckDragonKingdom/Prefabs/Dragons/"+speciesId;
            var configured=AssetDatabase.LoadAssetAtPath<GameObject>(configuredFolder+"/"+speciesId+" Dragon Animal.prefab");
            if(configured==null)throw new InvalidOperationException("Explicitly configured actual animal and calibrated mouth socket required before directed Breath authoring.");
            var configuredJaw=configured.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.mouthBone);
            var socket=configuredJaw.Find(DragonCharacter.MouthSocketName);
            if(socket==null)throw new InvalidOperationException("Calibrated actual mouth socket required before directed Breath authoring.");
            Vector3 localMuzzleForward=socket.localRotation*Vector3.forward;
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(configuredFolder+"/"+speciesId+" Dragon.controller");
            var states=controller==null?Array.Empty<AnimatorState>():controller.layers.SelectMany(layer=>BreathStates(layer.stateMachine)).Where(state=>state.tag=="DragonAction_Breath").ToArray();
            if(states.Length!=1||states.Any(state=>state.motion!=mapping.clip&&state.motion!=source))throw new InvalidOperationException("One existing owned Breath state with the selected actual mapping is required; designer controller changes are retained.");
            var watched=Directory.GetFiles(AuthoredRoot,"*.anim",SearchOption.AllDirectories).Select(value=>value.Replace('\\','/')).Where(value=>value!=path).ToDictionary(value=>value,BreathHash,StringComparer.Ordinal);
            var rawPaths=new[]{sourcePath,AssetDatabase.GetAssetPath(rawModel)};
            var rawHashes=rawPaths.ToDictionary(value=>value,BreathHash,StringComparer.Ordinal);
            var model=UnityEngine.Object.Instantiate(rawModel);model.hideFlags=HideFlags.HideAndDontSave;model.SetActive(false);AnimationClip output=null;
            try
            {
                var joints=model.GetComponentsInChildren<Transform>(true);
                var jaw=joints.Single(value=>value.name==species.mouthBone);var head=joints.Single(value=>value.name=="RigHead");var neck=head.parent;
                if(neck==null||!neck.name.StartsWith("RigNeck",StringComparison.Ordinal)||!jaw.IsChildOf(head))throw new InvalidOperationException("Actual adult terminal neck/head/jaw hierarchy required.");
                if(Vector3.Angle(jaw.TransformDirection(localMuzzleForward),model.transform.forward)>.1f)throw new InvalidOperationException("Configured muzzle calibration no longer matches the unchanged actual raw rig.");
                var receipt=new BreathUpgradeReceipt{scope=DirectedBreathVersion+": exact original body/wing/jaw curves, selected terminal neck/head rotations only; full forward muzzle aim through normalized .36-.65, source windup/recovery outside .20-.88. Native playback/contact/rendering still requires verification.",species=speciesId,source=sourcePath,sourceSha256=rawHashes[sourcePath],clip=path,neckPath=AnimationUtility.CalculateTransformPath(neck,model.transform),headPath=AnimationUtility.CalculateTransformPath(head,model.transform),duration=source.length};
                output=UnityEngine.Object.Instantiate(source);output.hideFlags=HideFlags.None;output.name=Path.GetFileNameWithoutExtension(path);
                var selected=new[]{neck,head};var curves=selected.ToDictionary(value=>value,value=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()});
                var previous=new Dictionary<Transform,Quaternion>();int samples=Mathf.Max(2,Mathf.CeilToInt(source.length*60));
                for(int i=0;i<=samples;i++)
                {
                    float normalized=i/(float)samples,time=source.length*normalized;source.SampleAnimation(model,time);
                    float weight=BreathAimWeight(normalized);Vector3 desired=model.transform.forward;
                    var correction=Quaternion.FromToRotation(jaw.TransformDirection(localMuzzleForward),desired);
                    neck.rotation=Quaternion.Slerp(Quaternion.identity,correction,weight*.45f)*neck.rotation;
                    correction=Quaternion.FromToRotation(jaw.TransformDirection(localMuzzleForward),desired);
                    head.rotation=Quaternion.Slerp(Quaternion.identity,correction,weight)*head.rotation;
                    foreach(var bone in selected)
                    {
                        Quaternion value=bone.localRotation;
                        if(previous.TryGetValue(bone,out var last)&&Quaternion.Dot(last,value)<0)value=new Quaternion(-value.x,-value.y,-value.z,-value.w);
                        previous[bone]=value;var tracks=curves[bone];tracks[0].AddKey(time,value.x);tracks[1].AddKey(time,value.y);tracks[2].AddKey(time,value.z);tracks[3].AddKey(time,value.w);
                    }
                }
                string[] axes={"x","y","z","w"};var selectedPaths=new[]{receipt.neckPath,receipt.headPath};
                foreach(var bone in selected)
                {
                    string bonePath=AnimationUtility.CalculateTransformPath(bone,model.transform);
                    foreach(var binding in AnimationUtility.GetCurveBindings(output).Where(binding=>binding.path==bonePath&&BreathRotation(binding)))AnimationUtility.SetEditorCurve(output,binding,null);
                    for(int axis=0;axis<4;axis++)
                    {
                        var track=curves[bone][axis];for(int key=0;key<track.length;key++){AnimationUtility.SetKeyLeftTangentMode(track,key,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(track,key,AnimationUtility.TangentMode.Linear);}
                        AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalRotation."+axes[axis]),track);
                    }
                }
                receipt.preservedCurves=VerifyBreathPreservation(source,output,selectedPaths);receipt.correctedRotationCurves=8;
                source.SampleAnimation(model,source.length*.45f);receipt.sourceContactAngle=Vector3.Angle(jaw.TransformDirection(localMuzzleForward),model.transform.forward);
                output.SampleAnimation(model,output.length*.45f);receipt.authoredContactAngle=Vector3.Angle(jaw.TransformDirection(localMuzzleForward),model.transform.forward);
                for(int i=0;i<=28;i++){output.SampleAnimation(model,output.length*(.36f+i*.01f));receipt.maximumContactWindowAngle=Mathf.Max(receipt.maximumContactWindowAngle,Vector3.Angle(jaw.TransformDirection(localMuzzleForward),model.transform.forward));}
                if(receipt.maximumContactWindowAngle>1||Mathf.Abs(output.length-source.length)>.0001f)throw new InvalidOperationException("Actual authored muzzle/duration gate failed before saving: "+receipt.maximumContactWindowAngle);
                receipt.preservedSourcePaths=rawPaths;receipt.originalSourcesUnchanged=rawHashes.All(pair=>BreathHash(pair.Key)==pair.Value);receipt.unrelatedAuthoredClipsUnchanged=watched.All(pair=>BreathHash(pair.Key)==pair.Value);
                if(!receipt.originalSourcesUnchanged||!receipt.unrelatedAuthoredClipsUnchanged)throw new InvalidOperationException("Unrelated/source asset changed before selected Breath save.");
                EnsureFolder(folder);var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(saved==null){saved=output;AssetDatabase.CreateAsset(saved,path);output=null;}
                // An imported .anim has Unity's generated binding cache, while
                // this fresh clone does not. Comparing their entire native
                // serialization causes false changes and clears that cache.
                // Compare every editable animation value exactly instead; an
                // unchanged selected derivative must never be dirtied/saved.
                else if(saved.name!=output.name||!BreathClipsExactlyEqual(saved,output)){EditorUtility.CopySerialized(output,saved);EditorUtility.SetDirty(saved);}
                const string note=DirectedBreathVersion+": original Cast Spell body/wing/jaw retained; actual terminal neck/head aim the calibrated muzzle forward during .36-.65 strike, preserving original windup/recovery. Source hashes and unchanged curves checked; native actual-contact/render review required.";
                if(mapping.clip!=saved||mapping.loop||mapping.playbackSpeed!=1||mapping.sourceNote!=note){mapping.clip=saved;mapping.loop=false;mapping.playbackSpeed=1;mapping.sourceNote=note;EditorUtility.SetDirty(catalog);}
                foreach(var state in states)if(state.motion!=saved){state.motion=saved;EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);}
                AssetDatabase.SaveAssetIfDirty(saved);AssetDatabase.SaveAssetIfDirty(catalog);AssetDatabase.SaveAssetIfDirty(controller);
                receipt.clipGuid=AssetDatabase.AssetPathToGUID(path);string report="Documentation/TruckSports/DragonKingdom/"+speciesId+"-DirectedBreathReceipt.json",json=JsonUtility.ToJson(receipt,true);
                if(!File.Exists(report)||File.ReadAllText(report)!=json)File.WriteAllText(report,json);
                Debug.Log("DIRECTED_BREATH_SELECTED_UPGRADE "+speciesId+" sourceAngle="+receipt.sourceContactAngle+" authoredAngle="+receipt.authoredContactAngle+" maxWindow="+receipt.maximumContactWindowAngle+" exactPreservedCurves="+receipt.preservedCurves+" guid="+receipt.clipGuid+"; actual native playback/contact/render verification remains required.");
                return catalog;
            }
            finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(model);}
        }

        static IEnumerable<AnimatorState> BreathStates(AnimatorStateMachine machine)
        {foreach(var state in machine.states)yield return state.state;foreach(var child in machine.stateMachines)foreach(var state in BreathStates(child.stateMachine))yield return state;}
        static float BreathAimWeight(float time)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.36f,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,.88f,time)));
        static bool BreathRotation(EditorCurveBinding binding)=>binding.type==typeof(Transform)&&(binding.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0);
        static int VerifyBreathPreservation(AnimationClip source,AnimationClip output,string[] changed)
        {
            int count=0;foreach(var binding in AnimationUtility.GetCurveBindings(source))
            {
                if(changed.Contains(binding.path)&&BreathRotation(binding))continue;var a=AnimationUtility.GetEditorCurve(source,binding);var b=AnimationUtility.GetEditorCurve(output,binding);
                if(b==null||a.preWrapMode!=b.preWrapMode||a.postWrapMode!=b.postWrapMode||!a.keys.SequenceEqual(b.keys))throw new InvalidOperationException("Original body/wing/jaw curve changed: "+binding.path+"/"+binding.propertyName);count++;
            }
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))if(!AnimationUtility.GetObjectReferenceCurve(source,binding).SequenceEqual(AnimationUtility.GetObjectReferenceCurve(output,binding)))throw new InvalidOperationException("Source object-reference curve changed: "+binding.path);
            return count;
        }
        static bool BreathClipsExactlyEqual(AnimationClip a,AnimationClip b)
        {
            if(a.legacy!=b.legacy||a.frameRate!=b.frameRate||a.wrapMode!=b.wrapMode||a.localBounds!=b.localBounds)return false;
            var bindingsA=AnimationUtility.GetCurveBindings(a);var bindingsB=AnimationUtility.GetCurveBindings(b);
            if(bindingsA.Length!=bindingsB.Length)return false;
            foreach(var binding in bindingsA)
            {
                if(!bindingsB.Contains(binding))return false;var x=AnimationUtility.GetEditorCurve(a,binding);var y=AnimationUtility.GetEditorCurve(b,binding);
                if(x==null||y==null||x.preWrapMode!=y.preWrapMode||x.postWrapMode!=y.postWrapMode||!x.keys.SequenceEqual(y.keys))return false;
            }
            var objectsA=AnimationUtility.GetObjectReferenceCurveBindings(a);var objectsB=AnimationUtility.GetObjectReferenceCurveBindings(b);
            if(objectsA.Length!=objectsB.Length)return false;
            foreach(var binding in objectsA)if(!objectsB.Contains(binding)||!AnimationUtility.GetObjectReferenceCurve(a,binding).SequenceEqual(AnimationUtility.GetObjectReferenceCurve(b,binding)))return false;
            var eventsA=AnimationUtility.GetAnimationEvents(a);var eventsB=AnimationUtility.GetAnimationEvents(b);if(eventsA.Length!=eventsB.Length)return false;
            for(int i=0;i<eventsA.Length;i++)
            {
                var x=eventsA[i];var y=eventsB[i];if(x.time!=y.time||x.functionName!=y.functionName||x.stringParameter!=y.stringParameter||x.floatParameter!=y.floatParameter||x.intParameter!=y.intParameter||x.objectReferenceParameter!=y.objectReferenceParameter||x.messageOptions!=y.messageOptions)return false;
            }
            var settingsA=AnimationUtility.GetAnimationClipSettings(a);var settingsB=AnimationUtility.GetAnimationClipSettings(b);
            foreach(var property in settingsA.GetType().GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public))
                if(property.CanRead&&property.GetIndexParameters().Length==0&&!Equals(property.GetValue(settingsA),property.GetValue(settingsB)))return false;
            foreach(var field in settingsA.GetType().GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public))
                if(!Equals(field.GetValue(settingsA),field.GetValue(settingsB)))return false;
            return true;
        }
        static string BreathHash(string path){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
    }
}
#endif
