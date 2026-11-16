#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class RecalibrationReport
        {
            public string scope="READ_ONLY_CENTERED_ACTUAL_FIVE_POINT_SKIN_RECALIBRATION_OVER_UNCHANGED_SAFE_OWNED_CURVES_NO_TRUCK_OFFSETS_NO_SAVES_NOT_RUNTIME_OR_BINDING_MIGRATION_ACCEPTANCE";
            public bool assetsExact,nwhExact,sceneBytesExact;
            public List<RecalibrationSpecies> species=new List<RecalibrationSpecies>();
        }
        [Serializable]sealed class RecalibrationSpecies{public string species;public bool qualified;public float selectedModelRearward;public List<RecalibrationAttempt> attempts=new List<RecalibrationAttempt>();}
        [Serializable]sealed class RecalibrationAttempt
        {
            public bool originalBindingControl,parameterBoundaryControl,centeredSymmetricSpanned,qualified;
            public float modelRearward,longitudinalRadius=.15f,extraBreathNeck2,extraBreathDistal,worldLongitudinalShift,lateralSpanWorld,longitudinalSpanWorld,neutralMountRotationDelta;
            public Vector3 originalNeutralTruckRoot,candidateNeutralTruckRoot,actualWorldMountDelta,actualModelMountDelta;
            public DragonRiderSurfaceAnchor[] materialBindings;public SurfaceProof[] actualQueryProof;public float[] actualTailWeights,actualTorsoWeights;public bool strictMajorityTorsoRequired;
            public string failure;public DeckWheelSeating neutralActualDeckSeating;
            public SavedFlightValidation boundedScreen,completeEffectiveActions,completeRealLayersAndBreath;
        }
        public static string InspectCenteredMountedSkinRecalibrationWithoutSavingAssets()=>InspectCenteredMountedSkinRecalibration(false);
        public static string InspectMeasuredCombinedMountedRecalibrationWithoutSavingAssets()=>InspectCenteredMountedSkinRecalibration(true);
        public static string InspectConnectedMountedPostureRecalibrationWithoutSavingAssets()=>InspectCenteredMountedSkinRecalibration(true,true);
        static string InspectCenteredMountedSkinRecalibration(bool measuredCombination,bool connectedPosture=false)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scene edits first.");
            var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            string scenePath=DragonRidersAuthoring.ScenePath,sceneHash=Hash(scenePath);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
            var report=new RecalibrationReport();if(measuredCombination)report.scope="READ_ONLY_MEASURED_INFERNO_EXTRA_NECK2_30_WITH_SUPPORTED_CENTERED_REAR_PATCHES_AND_FIRE_STRICT_MAJORITY_TORSO_SYMMETRIC_POINT065_RADIUS_FIXED_LATERAL_POINT15_NO_SAVES_NOT_RUNTIME_ACCEPTANCE";if(connectedPosture)report.scope="READ_ONLY_ACTUAL_CONNECTED_POSTURE_INFERNO_SOURCE_WORLD_NECK1_NONFLIGHT_FIRE_MEASURED_TAIL1_MINUS10_BREATH10_FULL_NATIVE_SKIN_AND_ORIGINAL_CONTACT_NO_SAVES";var plans=new List<SavedFlightPlan>();DragonRidersMatch match=null;string physics=null;
            try
            {
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
                match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);
                var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");
                foreach(string id in new[]{"dragon-inferno","dragon-fire"})
                {
                    var speciesRow=new RecalibrationSpecies{species=id};report.species.Add(speciesRow);
                    if(!catalog.TryGet(id,out var species)||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral))throw new InvalidOperationException("Actual source species/neutral required.");
                    var saved=match.racers.SelectMany(racer=>racer.choices).First(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId==id);
                    var plan=BuildSavedFlightPlan(saved,id,true);plans.Add(plan);
                    if(measuredCombination&&id=="dragon-inferno")
                    {
                        var prior=saved.motor.animator.runtimeAnimatorController;var unchanged=plan.expected;UnityEngine.Object[] generated;
                        try{saved.motor.animator.runtimeAnimatorController=unchanged;generated=CreateTransientBreathFlexCandidate(saved,id,30,0);}finally{saved.motor.animator.runtimeAnimatorController=prior;}
                        plan.temporary.AddRange(generated);plan.expected=generated.OfType<UnityEngine.AnimatorOverrideController>().Single();species.TryGetAnimation(DragonAnimationAction.Breath,out var breath);AssertNonFlightMappings(unchanged,plan.expected,new[]{breath.clip});
                    }
                    if(connectedPosture)
                    {
                        var prior=saved.motor.animator.runtimeAnimatorController;
                        try
                        {
                            if(id=="dragon-fire")
                            {
                                saved.motor.animator.runtimeAnimatorController=plan.expected;
                                var flex=CreateTransientBreathFlexCandidate(saved,id,0,10);plan.temporary.AddRange(flex);plan.expected=flex.OfType<AnimatorOverrideController>().Single();
                            }
                            saved.motor.animator.runtimeAnimatorController=plan.expected;
                            var connected=CreateTransientConnectedPosture(saved,id);plan.temporary.AddRange(connected);plan.expected=connected.OfType<AnimatorOverrideController>().Single();
                        }
                        finally{saved.motor.animator.runtimeAnimatorController=prior;}
                    }
                    var preview=new PreviewRenderUtility();DragonRiderSaddle frame=null;
                    try
                    {
                        var copy=UnityEngine.Object.Instantiate(saved.gameObject);preview.AddSingleGO(copy);copy.SetActive(true);
                        var actor=copy.GetComponent<DragonCharacter>();frame=copy.GetComponent<DragonRiderSaddle>();actor.motor.animator.enabled=false;actor.motor.animator.runtimeAnimatorController=plan.expected;
                        foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}var truckCopy=UnityEngine.Object.Instantiate(match.trucks[0].gameObject);preview.AddSingleGO(truckCopy);truckCopy.SetActive(true);var neutralTruck=truckCopy.GetComponent<SportsEventTruck>();foreach(var body in truckCopy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}frame.rigging.truck=neutralTruck;
                        var original=CaptureFrameCalibration(frame);var model=actor.motor.transform;var bone=frame.sourceBone;
                        var poses=model.GetComponentsInChildren<Transform>(true).Select(value=>new BonePose{bone=value,position=value.localPosition,rotation=value.localRotation}).ToArray();
                        var neutralClip=EffectiveRiderClip(actor,neutral.clip);ResetNeutral();frame.CaptureEvaluatedAnimationPose();var originalPose=frame.ReadPose();
                        float originalSupport=OriginalFootprints(new[]{match.trucks[0]}).Single().support;
                        var originalTruckRoot=originalPose.position+originalPose.rotation*Vector3.up*originalSupport;
                        var candidates=measuredCombination?(id=="dragon-inferno"?new[]{(rear:(float?)null,radius:.15f),(rear:(float?)-.225f,radius:.15f),(rear:(float?)-.3f,radius:.15f)}:new[]{(rear:(float?)null,radius:.15f),(rear:(float?)-.15f,radius:.15f),(rear:(float?)-.15f,radius:.075f),(rear:(float?)-.15f,radius:.065f)}):new float?[]{null,0,-.075f,-.15f,-.225f,-.3f,-.375f,-.45f}.Select(rear=>(rear,radius:.15f)).ToArray();
                        foreach(var candidate in candidates)
                        {
                            var offset=candidate.rear;var row=new RecalibrationAttempt{originalBindingControl=!offset.HasValue,parameterBoundaryControl=measuredCombination&&id=="dragon-fire"&&offset.HasValue&&candidate.radius!=.065f,modelRearward=offset??0,longitudinalRadius=candidate.radius,extraBreathNeck2=measuredCombination&&id=="dragon-inferno"?30:0,extraBreathDistal=connectedPosture&&id=="dragon-fire"?10:0,strictMajorityTorsoRequired=measuredCombination,originalNeutralTruckRoot=originalTruckRoot};speciesRow.attempts.Add(row);
                            try
                            {
                                ApplyFrameCalibration(frame,original);ResetNeutral();
                                if(offset.HasValue)
                                {
                                    Quaternion rotation=Quaternion.Inverse(bone.rotation)*model.rotation;Vector3 localUp=bone.InverseTransformDirection(model.up).normalized;
                                    var measured=MeasureActualDorsalCandidate(actor,species,neutral,neutralClip,poses,rotation,localUp,offset.Value,candidate.radius,measuredCombination);
                                    string context=id+"-"+neutral.action+"-0.25-patch"+offset.Value.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
                                    var patch=measured.proof.Where(value=>value.context==context).ToArray();
                                    row.actualQueryProof=patch;row.materialBindings=measured.patchAnchors.Select(value=>new DragonRiderSurfaceAnchor{a=value.a,b=value.b,c=value.c,barycentric=value.barycentric}).ToArray();
                                    row.actualTorsoWeights=patch.Select(point=>point.torsoSkinWeight).ToArray();
                                    VerifyCenteredMaterialPatch(model,bone,patch,offset.Value,candidate.radius,measuredCombination,out row.lateralSpanWorld,out row.longitudinalSpanWorld,out row.actualTailWeights);
                                    row.centeredSymmetricSpanned=true;ApplyFrameCalibration(frame,measured);
                                }
                                ResetNeutral();frame.CaptureEvaluatedAnimationPose();var physical=frame.ReadPose();
                                row.candidateNeutralTruckRoot=physical.position+physical.rotation*Vector3.up*originalSupport;
                                row.actualWorldMountDelta=row.candidateNeutralTruckRoot-originalTruckRoot;row.actualModelMountDelta=model.InverseTransformVector(row.actualWorldMountDelta);
                                row.worldLongitudinalShift=Vector3.Dot(row.actualWorldMountDelta,model.forward);row.neutralMountRotationDelta=Quaternion.Angle(originalPose.rotation,physical.rotation);
                                neutralTruck.transform.SetPositionAndRotation(row.candidateNeutralTruckRoot,physical.rotation);frame.rigging.RefreshAuthoredPresentation(true);
                                row.neutralActualDeckSeating=MeasureActualDeckSeating(neutralTruck,frame.supportPresentation,physical.rotation*Vector3.up,"READ_ONLY_RECALIBRATED_ACTUAL_NEUTRAL_VENDOR_WHEEL_TIMBER_CONTACT_NO_LIVE_SUSPENSION_CLAIM");
                                if(row.neutralActualDeckSeating.wheels.Length!=4||row.neutralActualDeckSeating.wheels.Any(wheel=>!wheel.actualVisualFound||!wheel.deckFaceFound||Mathf.Abs(wheel.actualGap)>.03f))throw new InvalidOperationException("Actual four neutral original rendered wheels must meet existing vendor timber drive faces within3cm.");
                                plan.calibrationModelRearward=offset??0;
                                row.materialBindings=frame.surfacePatchAnchors.Select(value=>new DragonRiderSurfaceAnchor{a=value.a,b=value.b,c=value.c,barycentric=value.barycentric}).ToArray();
                                row.boundedScreen=NewValidation();plan.validation=row.boundedScreen;
                                ValidateCompleteSavedFlightCandidate(actor,match.trucks,plan,RecalibrationCriticalTrials,false);
                                // Original material bindings are an explicit control only;
                                // a query candidate must meet all complete existing gates.
                                if(row.originalBindingControl){WriteRecalibration(report);continue;}
                                if(row.parameterBoundaryControl)throw new InvalidOperationException("The original-radius or measured50/50-boundary parameter control is retained as diagnosis, not an approved body-supported acceptance candidate.");
                                row.completeEffectiveActions=NewValidation();plan.validation=row.completeEffectiveActions;
                                ValidateCompleteSavedFlightCandidate(actor,match.trucks,plan);
                                row.completeRealLayersAndBreath=NewValidation();plan.validation=row.completeRealLayersAndBreath;
                                ValidateCompleteSavedFlightCandidate(actor,match.trucks,plan,CompleteActualLayerBreathTrials,false);
                                row.qualified=true;speciesRow.qualified=true;speciesRow.selectedModelRearward=offset.Value;WriteRecalibration(report);break;
                            }
                            catch(Exception error){row.failure=error.ToString();WriteRecalibration(report);}
                        }
                        SavedFlightValidation NewValidation()=>new SavedFlightValidation{species=id,rootFlapFraction=plan.flap};
                        void ResetNeutral(){foreach(var pose in poses)if(pose.bone!=frame.socket)pose.Restore();neutralClip.SampleAnimation(model.gameObject,neutralClip.length*.25f);}
                    }
                    finally{if(frame!=null)frame.ReleaseGeometrySnapshot();preview.Cleanup();}
                }
                if(report.species.Any(value=>!value.qualified))throw new InvalidOperationException("No unchanged-gate genuine centered material-surface candidate qualifies for one or more species; no assets may be saved.");
            }
            finally
            {
                if(match!=null&&physics!=null)report.nwhExact=PhysicsSignature(match)==physics;
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);foreach(var plan in plans)plan.Dispose();if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
                report.sceneBytesExact=Hash(scenePath)==sceneHash;report.assetsExact=watched.Count==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);WriteRecalibration(report);
                if(!report.assetsExact||!report.nwhExact||!report.sceneBytesExact)throw new InvalidOperationException("Read-only material recalibration changed original source/NWH/scene.");
            }
            return JsonUtility.ToJson(report,true);
        }
        static Calibration CaptureFrameCalibration(DragonRiderSaddle frame)=>new Calibration{position=frame.socket.localPosition,rotation=frame.socket.localRotation,clip=frame.neutralClip,surfaces=frame.sampledSkinSurfaces,supportMotorLocalPosition=frame.supportMotorLocalPosition,dorsalMinimum=frame.measuredDorsalMinimum,dorsalMaximum=frame.measuredDorsalMaximum,anchorMesh=AssetDatabase.GetAssetPath(frame.surfaceRenderer.sharedMesh),anchorA=frame.surfaceVertexA,anchorB=frame.surfaceVertexB,anchorC=frame.surfaceVertexC,anchorBarycentric=frame.surfaceBarycentric,patchAnchors=frame.surfacePatchAnchors};
        static void ApplyFrameCalibration(DragonRiderSaddle frame,Calibration value)
        {
            frame.ReleaseGeometrySnapshot();frame.socket.SetLocalPositionAndRotation(value.position,value.rotation);frame.neutralClip=value.clip;frame.sampledSkinSurfaces=value.surfaces;frame.supportMotorLocalPosition=value.supportMotorLocalPosition;frame.measuredDorsalMinimum=value.dorsalMinimum;frame.measuredDorsalMaximum=value.dorsalMaximum;
            frame.surfaceRenderer=frame.actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(skin=>skin.sharedMesh!=null&&AssetDatabase.GetAssetPath(skin.sharedMesh)==value.anchorMesh);
            frame.surfaceVertexA=value.anchorA;frame.surfaceVertexB=value.anchorB;frame.surfaceVertexC=value.anchorC;frame.surfaceBarycentric=value.anchorBarycentric;frame.surfacePatchAnchors=value.patchAnchors;frame.Validate(frame.actor);
        }
        static void VerifyCenteredMaterialPatch(Transform model,Transform bone,SurfaceProof[] patch,float rearward,float longitudinalRadius,bool strictMajorityTorso,out float lateralSpan,out float longitudinalSpan,out float[] tailWeights)
        {
            if(patch.Length!=5||patch.Select(point=>point.mesh).Distinct().Count()!=1||patch.Any(point=>point.wingDominated||point.torsoSkinWeight<=0||point.upwardNormal<.1f||point.cpuSkinningRmse>=.0001f))throw new InvalidOperationException("Actual five upward torso material points on one verified source skin required; no wing/flank replacement.");
            tailWeights=patch.Select(point=>{float sum=0;for(int vertex=0;vertex<3;vertex++){var source=point.vertexInfluences[vertex];for(int slot=0;slot<source.weights.Length;slot++)if(source.bones[slot]!=null&&source.bones[slot].StartsWith("RigTail",StringComparison.Ordinal))sum+=source.weights[slot]*point.barycentric[vertex];}return sum;}).ToArray();
            if(strictMajorityTorso&&patch.Any(point=>point.barycentric.x<0||point.barycentric.y<0||point.barycentric.z<0))throw new InvalidOperationException("Every new material point must lie inside its actual triangle; no negative-weight or clamped fallback.");
            if(strictMajorityTorso&&patch.Any(point=>point.torsoSkinWeight<=.5f))throw new InvalidOperationException("Every new measured-combination material support must be strict majority spine/ribcage/pelvis; a50/50 tail boundary is a rejected control.");
            if(tailWeights.Any(weight=>weight>.5f))throw new InvalidOperationException("New centered source-material support cannot be a majority-tail surface.");
            var origin=model.InverseTransformPoint(bone.position);var expected=new[]{Vector2.zero,new Vector2(.15f,0),new Vector2(-.15f,0),new Vector2(0,longitudinalRadius),new Vector2(0,-longitudinalRadius)};
            for(int i=0;i<5;i++){var point=patch[i].actualModelPoint;if(Mathf.Abs(point.x-origin.x-expected[i].x)>.0001f||Mathf.Abs(point.z-origin.z-rearward-expected[i].y)>.0001f)throw new InvalidOperationException("Material patch moved to an asymmetric flank or skipped its declared centered ray.");}
            var world=patch.Select(point=>model.TransformPoint(point.actualModelPoint)).ToArray();lateralSpan=Vector3.Distance(world[1],world[2]);longitudinalSpan=Vector3.Distance(world[3],world[4]);
            if(lateralSpan<.3f*model.lossyScale.x-.001f||longitudinalSpan<2*longitudinalRadius*model.lossyScale.z-.001f||Vector3.Dot(Vector3.Cross(world[3]-world[4],world[1]-world[2]).normalized,model.up)<.1f)throw new InvalidOperationException("Actual centered material patch is not bilaterally/longitudinally spanned and upward.");
        }
        static List<MountedPoseTrial> RecalibrationCriticalTrials(DragonCharacter actor,DragonSpeciesDefinition species)
        {
            var result=CompleteActualLayerBreathTrials(actor,species).Where(trial=>trial.actualConfiguredBreathLayer&&trial.horizontal==0&&trial.vertical==1&&trial.fraction==.45f&&trial.transitionElapsed!=.09f).ToList();
            foreach(var action in new[]{DragonAnimationAction.FlyIdle,DragonAnimationAction.Fly,DragonAnimationAction.Glide,DragonAnimationAction.BankLeft,DragonAnimationAction.BankRight,DragonAnimationAction.Breath})
            {
                if(!species.TryGetAnimation(action,out var mapping))throw new InvalidOperationException("Actual mounted critical clip required.");var phases=action==DragonAnimationAction.Breath?new[]{0f,1f/36,2f/36,8f/36,11f/30,.45f,.75f,.98f}:new[]{0f,.45f,.98f};
                foreach(float phase in phases)result.Add(new MountedPoseTrial{name="Bounded recalibration actual "+action+" phase="+phase,clip=EffectiveRiderClip(actor,mapping.clip),fraction=phase});
            }
            return result;
        }
        static void WriteRecalibration(RecalibrationReport report)
        {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring",report.scope.Contains("ACTUAL_CONNECTED_POSTURE")?"ConnectedSkin":report.scope.Contains("MEASURED_INFERNO_EXTRA")?"CombinedSkin":"CenteredSkin");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"centered-skin-recalibration.json"),JsonUtility.ToJson(report,true));}
    }
}
#endif
