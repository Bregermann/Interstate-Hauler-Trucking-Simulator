#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class OwnedDefaultProof
        {
            public string scope="GENUINE_CONFIGURED_OWNED_DEFAULT0_EXISTING_SAFE_MOUNTED_ADAPTER_ORIGINAL_RAW_START_CONTROL_ALL59_POSITIONS_SCALES_UNSELECTED_ROTATIONS_EXACT_SELECTED_AUTHORED_ROTATIONS_SEPARATE_NO_BONE_WRITER",originalKeyGuid,underlyingDefaultKeyGuid,ownedClip,ownedController;
            public long originalKeyFileId,underlyingDefaultKeyFileId;public bool originalRaw59Passed,ownedNativeDefaultPassed;public int sourceBones,selectedBones;public List<OwnedDefaultBone> bones=new List<OwnedDefaultBone>();
        }
        [Serializable]sealed class OwnedDefaultBone{public string bone;public bool selected;public Vector3 originalPosition,ownedPosition,originalScale,ownedScale;public Quaternion originalRotation,ownedRotation,authoredReferenceRotation;public float positionError,scaleError,unselectedRotationError,authoredSelectedRotationError;}
        static void InstallActualExistingOwnedDefaultTrial(DragonCharacter actor,List<MountedPoseTrial> trials,ProvenRiderStartup startup,MountedSizeProfile row,RuntimeAnimatorController originalController,DragonSpeciesDefinition species)
        {
            var animator=actor.motor.animator;var owned=animator.runtimeAnimatorController;var rawRow=new MountedSizeProfile{profile="ORIGINAL_RAW_DEFAULT0_CONTROL_BEFORE_EXISTING_SAFE_MOUNTED"};var rawTrials=new List<MountedPoseTrial>{new MountedPoseTrial{name="Original raw configured default control"}};
            var skeleton=actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(skin=>skin.bones).Where(bone=>bone!=null).Distinct().ToArray();if(skeleton.Length!=59)throw new InvalidOperationException("All59original source skin bones required for owned default provenance.");Dictionary<Transform,SavedJoint> original=null;
            animator.runtimeAnimatorController=originalController;
            try{InstallProvenConfiguredStartupTrial(actor,rawTrials,startup,rawRow);original=skeleton.ToDictionary(bone=>bone,bone=>new SavedJoint{joint=bone,position=bone.localPosition,rotation=bone.localRotation,scale=bone.localScale});}finally{animator.runtimeAnimatorController=owned;}
            row.originalDefaultControlPassed=rawRow.actualConfiguredStartupTrial;var rawRuntime=originalController;while(rawRuntime is AnimatorOverrideController rawParent)rawRuntime=rawParent.runtimeAnimatorController;var rawKeys=MotionClips(((AnimatorController)rawRuntime).layers[0].stateMachine.defaultState.motion).ToArray();if(rawKeys.Length!=1)throw new InvalidOperationException("Original raw controller must retain its single original default key.");var runtime=owned;while(runtime is AnimatorOverrideController parent)runtime=parent.runtimeAnimatorController;var controller=runtime as AnimatorController;var keys=MotionClips(controller.layers[0].stateMachine.defaultState.motion).ToArray();if(keys.Length!=1||keys[0]!=rawKeys[0])throw new InvalidOperationException("Owned controller must retain the exact original configured default source key.");var clip=EffectiveRiderClip(actor,keys[0]);if(clip==startup.clip||AssetDatabase.Contains(clip)||!owned.name.EndsWith("_SAFE_ACTIONS",StringComparison.Ordinal))throw new InvalidOperationException("Only the existing unsaved all-action safe mounted adapter may use separate selected-rotation default proof.");
            var trial=new MountedPoseTrial{name="GENUINE_EXISTING_OWNED_SAFE_MOUNTED_CONFIGURED_DEFAULT0_WITH_ORIGINAL_RAW_CONTROL",clip=clip,fraction=0,actualConfiguredStartup=true};SampleMountedTrial(actor,trial);
            var allowed=new HashSet<string>{"RigNeck1","RigNeck2","RigNeck3","RigHead","RigLWing01","RigRWing01"};var proof=new OwnedDefaultProof{originalKeyGuid=startup.guid,originalKeyFileId=startup.fileId,ownedClip=clip.name,ownedController=owned.name,originalRaw59Passed=row.originalDefaultControlPassed};GameObject visual=null;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(keys[0],out proof.underlyingDefaultKeyGuid,out proof.underlyingDefaultKeyFileId);
            try
            {
                // Separate SCRIPT-FREE source visual evaluates the genuinely
                // authored clip. Never assign recorded/expected bones onto the
                // actual native actor to make a failed pose appear correct.
                visual=UnityEngine.Object.Instantiate(species.visualPrefab);visual.hideFlags=HideFlags.HideAndDontSave;visual.SetActive(false);foreach(var sourceAnimator in visual.GetComponentsInChildren<Animator>(true))sourceAnimator.enabled=false;clip.SampleAnimation(visual,0);var reference=visual.GetComponentsInChildren<Transform>(true).GroupBy(bone=>bone.name).ToDictionary(group=>group.Key,group=>group.ToArray());
                foreach(var bone in skeleton)
                {
                    bool selected=allowed.Contains(bone.name);var before=original[bone];var actual=new OwnedDefaultBone{bone=bone.name,selected=selected,originalPosition=before.position,ownedPosition=bone.localPosition,originalScale=before.scale,ownedScale=bone.localScale,originalRotation=before.rotation,ownedRotation=bone.localRotation,positionError=Vector3.Distance(before.position,bone.localPosition),scaleError=Vector3.Distance(before.scale,bone.localScale)};
                    if(selected){actual.authoredReferenceRotation=reference[bone.name].Single().localRotation;actual.authoredSelectedRotationError=(float)StartupRotationError(bone.localRotation,actual.authoredReferenceRotation);proof.selectedBones++;row.maximumOwnedDefaultSelectedRotationError=Mathf.Max(row.maximumOwnedDefaultSelectedRotationError,actual.authoredSelectedRotationError);}else{actual.unselectedRotationError=(float)StartupRotationError(before.rotation,bone.localRotation);row.maximumOwnedDefaultUnselectedRotationError=Mathf.Max(row.maximumOwnedDefaultUnselectedRotationError,actual.unselectedRotationError);}
                    row.maximumOwnedDefaultUnselectedPositionError=Mathf.Max(row.maximumOwnedDefaultUnselectedPositionError,actual.positionError);row.maximumOwnedDefaultScaleError=Mathf.Max(row.maximumOwnedDefaultScaleError,actual.scaleError);proof.bones.Add(actual);proof.sourceBones++;
                }
                if(proof.sourceBones!=59||proof.selectedBones!=6||!proof.originalRaw59Passed||row.maximumOwnedDefaultUnselectedPositionError>1e-5f||row.maximumOwnedDefaultScaleError>1e-6f||row.maximumOwnedDefaultUnselectedRotationError>.01f||row.maximumOwnedDefaultSelectedRotationError>.5f)throw new InvalidOperationException("Owned default must retain all59positions/scales and unselected rotations, and genuinely reproduce the existing authored selected rotations; no fidelity or source-default waiver.");
                proof.ownedNativeDefaultPassed=true;row.ownedDefaultValidated=true;row.actualConfiguredStartupTrial=true;row.startupEvidence=startup.folder;row.actualOwnedDefaultClip=clip.name;row.actualStartupOriginalKeyGuid=startup.guid;row.actualStartupOriginalKeyFileId=startup.fileId;var originalTrial=trials.Single(value=>value.clip==null&&!value.actualConfiguredFlight&&!value.actualConfiguredBreathLayer);originalTrial.name=trial.name;originalTrial.clip=trial.clip;originalTrial.fraction=0;originalTrial.actualConfiguredStartup=true;
            }
            finally
            {
                if(visual!=null)UnityEngine.Object.DestroyImmediate(visual);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","OwnedDefault");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"actual-existing-all-action-owned-default0.json"),JsonUtility.ToJson(proof,true));}
            }
        }
        static ProvenInfernoMaterial RequireProvenActualPointwiseInfernoMaterial(string folder)
        {
            var manifest=RequireRiderPassedManifest(folder,"DragonRiderPointwiseTorsoTests.ActualPointwiseFiveRayIntervalsAcrossAll12SourcePhasesDeriveOnePatchBeforeUnchanged699NativeGeometry");var points=JsonUtility.FromJson<PointwiseTorsoReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(folder,"rendered-evidence"),"actual-all12-pointwise-five-ray-material-intervals.json",SearchOption.AllDirectories).Single()));if(!points.derived||!points.sourceExact||!points.sceneExact||!points.nwhExact||points.sourcePoses!=12||points.points.Length!=5||points.center!= -.3136605322f)throw new InvalidOperationException("Exact positively proven all12pointwise source patch required.");
            var report=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(folder,"rendered-evidence"),"precise-native-material-inferno-size-derivation.json",SearchOption.AllDirectories).Single()));var passed=report.profiles.Where(row=>row.actualPoses==699&&row.invalidAnatomicalPoses==0&&row.sourceMotionUnchanged&&row.exactOriginalBoxAcceptance&&row.originalFootprintFits&&row.candidateSatPairs==0).ToArray();if(!report.sourceBytesExact||!report.nwhExact||!report.sceneBytesExact||passed.Length!=2)throw new InvalidOperationException("Both actual all699pointwise source controls must have passed native geometry before testing owned posture.");var baselines=new List<RiderNativeSourceBaseline>();foreach(var path in passed[0].sourceFiles.Select(row=>row.path))RequireActualNativeCapturedDependency(manifest,passed.Select(row=>row.sourceFiles.ToArray()),path,RiderEvidenceHostRoot(folder),baselines,"Original pointwise material source identity");RequireRecordedStartupSourceHash(manifest,DragonRidersAuthoring.ScenePath,"Original pointwise whole-scene identity");foreach(var path in new[]{"Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.PointwiseTorso.cs","Assets/TruckDragonRiders/Tests/Authoring/DragonRiderPointwiseTorsoTests.cs"})RequireRecordedStartupSourceHash(manifest,path,"Exact native pointwise material producer");return new ProvenInfernoMaterial{folder=folder,rearward=points.center,points=points.points};
        }
        public static string InspectExistingAllActionSafeMountedInfernoUsingProvenPointwiseMaterialWithoutSavingAssets(string pointwiseEvidence,string startupEvidence)=>InspectMountedSizeProfiles(true,true,false,RequireProvenActualInfernoStartup(startupEvidence),null,RequireProvenActualPointwiseInfernoMaterial(pointwiseEvidence),true);
    }
}
#endif
