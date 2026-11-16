#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class CenteredDiagnostic
        {
            public string scope="READ_ONLY_EXISTING_CENTERED_PATCH_ACTUAL_482_POSE_SKIN_DIAGNOSIS_NOT_SAVED_CALIBRATION_OR_MOUNT_ACCEPTANCE",species="dragon-inferno";
            public int savedCalibrationVersion;
            public float measuredNeutralRearward,measuredNeutralLateral;
            public bool actualNeutralPatchCentered,sourceAndSavedAssetsExact;
            public string[] watchedFiles;
            public FootprintReceipt footprint;
            public SurfaceProof[] retainedNeutralAnchorProofs,retainedLowTorsoNeckProofs;
            public InterferencePose[] actualInterference;
        }

        // Deliberately consumes the existing saved geometric patch rather than
        // running seven known-rejected authoring candidates. All actual native
        // pose, original truck and penetrating-ray oracles remain identical.
        public static string InspectSavedCenteredInfernoFootprintWithoutSavingAssets()=>InspectSavedCenteredInferno(false);
        public static string InspectSavedCenteredInfernoInterferenceWithoutSavingAssets()=>InspectSavedCenteredInferno(true);
        static string InspectSavedCenteredInferno(bool renderInterference)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for read-only centered diagnosis.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            for(int index=0;index<SceneManager.sceneCount;index++)if(SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Preserve unsaved scene edits before native read-only diagnosis.");
            const string catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
            string scenePath=DragonRidersAuthoring.ScenePath,actorPath=ActorPath("dragon-inferno");
            var watched=AssetDatabase.GetDependencies(actorPath,true).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path))
                .Concat(new[]{scenePath,catalogPath}).SelectMany(path=>new[]{path,path+".meta"}).Distinct().ToDictionary(path=>path,Hash);
            var ownedBefore=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(catalogPath);
            if(catalog==null||!catalog.TryGet("dragon-inferno",out var species))throw new InvalidOperationException("Actual original configured Inferno species required.");
            Scene scene=default;GameObject root=null;var transient=new List<UnityEngine.Object>();
            var report=new CenteredDiagnostic();if(renderInterference)report.scope="READ_ONLY_FOUR_SAME_PHASE_FULL_ORIGINAL_NWH_AND_WING_INTERSECTION_RENDER_DIAGNOSIS_NOT_SAVED_CALIBRATION_OR_MOUNT_ACCEPTANCE";
            try
            {
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
                var match=scene.GetRootGameObjects().SelectMany(value=>value.GetComponentsInChildren<DragonRidersMatch>(true)).Single();
                var saved=match.racers.SelectMany(racer=>racer.choices).Select(value=>value.GetComponent<DragonRiderSaddle>()).First(value=>value!=null&&value.speciesId==species.id);
                if(saved.sourceBone==null||saved.sourceBone.name!=species.saddleBone||saved.surfaceRenderer==null||saved.surfacePatchAnchors==null||saved.surfacePatchAnchors.Length!=5)throw new InvalidOperationException("Actual saved five-point Inferno patch required; this diagnostic never creates its calibration.");
                report.savedCalibrationVersion=saved.calibrationVersion;
                var calibration=new Calibration{modelRearwardOffset=0,anchorMesh=AssetDatabase.GetAssetPath(saved.surfaceRenderer.sharedMesh),anchorA=saved.surfaceVertexA,anchorB=saved.surfaceVertexB,anchorC=saved.surfaceVertexC,anchorBarycentric=saved.surfaceBarycentric,patchAnchors=saved.surfacePatchAnchors};
                root=PrefabUtility.LoadPrefabContents(actorPath);var actor=root.GetComponent<DragonCharacter>();var model=actor.motor.transform;
                actor.motor.animator.enabled=false;
                if(!species.TryGetAnimation(DragonAnimationAction.Fly,out var forward)||!species.TryGetAnimation(DragonAnimationAction.TurnRight,out var turn)||!species.TryGetAnimation(DragonAnimationAction.Breath,out var breath)||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral))throw new InvalidOperationException("Actual original mounted flight/turn/Breath mappings required.");
                var turnClip=CreateMountedSupportAction(species,turn.clip,forward.clip,DragonAnimationAction.TurnRight,out _);transient.Add(turnClip);
                var breathClip=CreateMountedSupportAction(species,breath.clip,forward.clip,DragonAnimationAction.Breath,out _);transient.Add(breathClip);
                var baseController=actor.motor.animator.runtimeAnimatorController;while(baseController is AnimatorOverrideController layer)baseController=layer.runtimeAnimatorController;
                var controller=new AnimatorOverrideController(baseController){name="Read-only transient exact Inferno mounted recipe"};transient.Add(controller);
                controller[turn.clip]=turnClip;controller[breath.clip]=breathClip;actor.motor.animator.runtimeAnimatorController=controller;
                var originalPoses=model.GetComponentsInChildren<Transform>(true).Select(value=>new BonePose{bone=value,position=value.localPosition,rotation=value.localRotation}).ToArray();
                var bone=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);
                neutral.clip.SampleAnimation(model.gameObject,neutral.clip.length*.25f);
                var skin=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(value=>value.sharedMesh!=null&&AssetDatabase.GetAssetPath(value.sharedMesh)==calibration.anchorMesh);
                var mesh=new Mesh();
                try
                {
                    skin.BakeMesh(mesh,true);var vertices=mesh.vertices;var matrix=skin.transform.localToWorldMatrix;
                    Vector3 point=model.InverseTransformPoint(matrix.MultiplyPoint3x4(vertices[calibration.anchorA]*calibration.anchorBarycentric.x+vertices[calibration.anchorB]*calibration.anchorBarycentric.y+vertices[calibration.anchorC]*calibration.anchorBarycentric.z));
                    var origin=model.InverseTransformPoint(bone.position);report.measuredNeutralRearward=point.z-origin.z;report.measuredNeutralLateral=point.x-origin.x;
                    report.actualNeutralPatchCentered=Mathf.Abs(report.measuredNeutralRearward)<.0001f&&Mathf.Abs(report.measuredNeutralLateral)<.0001f;
                    if(!report.actualNeutralPatchCentered)throw new InvalidOperationException("Saved Inferno patch is not the actual centered candidate; do not substitute another patch for this diagnostic.");
                }
                finally{UnityEngine.Object.DestroyImmediate(mesh);foreach(var pose in originalPoses)pose.Restore();}
                report.retainedNeutralAnchorProofs=VerifySavedNeutralAnchorsAndNeckBlends(actor,species,calibration,originalPoses,out var lowTorso);
                report.retainedLowTorsoNeckProofs=lowTorso;
                if(renderInterference)
                    report.actualInterference=ReviewActualWingInterference(actor,species,calibration,match.trucks[0],saved.supportPresentation);
                else
                {
                    FitsOriginalFootprints(actor,species,calibration,originalPoses,match.trucks,out var footprint);report.footprint=footprint;
                    footprint.scope="READ_ONLY_ACTUAL_CENTERED_PATCH_NEGATIVE_RAY_EXPLANATION_NOT_AUTHORING_OR_FULL_MOUNT_ACCEPTANCE";
                }
            }
            finally
            {
                if(root!=null)PrefabUtility.UnloadPrefabContents(root);
                for(int index=transient.Count-1;index>=0;index--)if(transient[index]!=null)UnityEngine.Object.DestroyImmediate(transient[index]);
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
                if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
                var afterFiles=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);
                report.sourceAndSavedAssetsExact=ownedBefore.Keys.OrderBy(path=>path,StringComparer.Ordinal).SequenceEqual(afterFiles.OrderBy(path=>path,StringComparer.Ordinal))&&ownedBefore.All(pair=>Hash(pair.Key)==pair.Value)&&watched.All(pair=>Hash(pair.Key)==pair.Value);
                report.watchedFiles=watched.Keys.Concat(ownedBefore.Keys).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToArray();
                if(!report.sourceAndSavedAssetsExact)throw new InvalidOperationException("Read-only centered footprint diagnosis changed a saved source/scene/asset; never retain these changes.");
            }
            string json=JsonUtility.ToJson(report,true);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
            if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit retained native evidence folder required.");
            string output=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,renderInterference?"centered-inferno-real-wing-cab-interference.json":"centered-inferno-read-only-diagnosis.json"),json);return json;
        }
        static SurfaceProof[] VerifySavedNeutralAnchorsAndNeckBlends(DragonCharacter actor,DragonSpeciesDefinition species,Calibration calibration,BonePose[] originalPoses,out SurfaceProof[] lowTorso)
        {
            var model=actor.motor.transform;var bone=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);
            var skin=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(value=>value.sharedMesh!=null&&AssetDatabase.GetAssetPath(value.sharedMesh)==calibration.anchorMesh);
            var proofs=new List<SurfaceProof>();var mesh=new Mesh();
            try
            {
                foreach(var pose in originalPoses)pose.Restore();species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral);neutral.clip.SampleAnimation(model.gameObject,neutral.clip.length*.25f);skin.BakeMesh(mesh,true);var vertices=mesh.vertices;var matrix=skin.transform.localToWorldMatrix;var origin=model.InverseTransformPoint(bone.position);
                foreach(var anchor in calibration.patchAnchors)
                {
                    Vector3 expected=matrix.MultiplyPoint3x4(vertices[anchor.a]*anchor.barycentric.x+vertices[anchor.b]*anchor.barycentric.y+vertices[anchor.c]*anchor.barycentric.z),query=model.InverseTransformPoint(expected);
                    var measured=DorsalSurface(model,bone,new Vector2(query.x-origin.x,query.z-origin.z),"actual-retained-neutral-anchor",out var proof,true);
                    if(Vector3.Distance(expected,measured)>.0001f||proof.wingDominated)throw new InvalidOperationException("Anatomical classification changed a genuine saved neutral dorsal anchor.");proofs.Add(proof);
                }
                foreach(var pose in originalPoses)pose.Restore();species.TryGetAnimation(DragonAnimationAction.GroundIdle,out var ground);ground.clip.SampleAnimation(model.gameObject,0);
                var neckProofs=new List<SurfaceProof>();
                foreach(var offset in new[]{Vector2.zero,new Vector2(.15f,0),new Vector2(-.15f,0),new Vector2(0,.15f),new Vector2(0,-.15f)})
                {
                    DorsalSurface(model,bone,offset,"actual-retained-low-torso-neck-blend",out var proof,true);
                    if(proof.torsoSkinWeight<.2f&&proof.vertexInfluences.Any(value=>value.bones.Any(name=>name!=null&&name.StartsWith("RigNeck",StringComparison.Ordinal))))neckProofs.Add(proof);
                }
                if(neckProofs.Count==0)throw new InvalidOperationException("Actual low-torso neck-blended skin must remain eligible; a majority torso threshold is not anatomical truth.");lowTorso=neckProofs.ToArray();return proofs.ToArray();
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);foreach(var pose in originalPoses)pose.Restore();}
        }
    }
}
#endif
