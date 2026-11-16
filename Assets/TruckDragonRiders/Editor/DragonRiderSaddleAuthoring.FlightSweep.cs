#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class CabFlightSweep
        {public string scope="READ_ONLY_REAL_TRANSIENT_SOURCE_CURVES_NATIVE_SOURCE_FRAME_MIXER_AND_THREE_LEAF_FULL_SKIN_SWEEP_ORIGINAL_BASELINE_NWH_POSE_NO_SAVED_ASSETS_NOT_LIVE_OR_PRODUCTION_ACCEPTANCE";public bool assetsExact,originalNwhExact,satControlsPassed,cachedBodyOracleControlsPassed=true;public int cachedBodyOracleComparisons;public List<CabSweepProfile> profiles=new List<CabSweepProfile>();}
        [Serializable]sealed class CabSweepProfile
        {public string species;public float neckPitch,headPitch,wingDihedral,sourceRootFlapFraction;public bool stableTorsoAnchorPrototype;public int sourceFrames,mixedPoses,nativeThreeLeafPoses;public bool fits;public List<CabSweepPose> poses=new List<CabSweepPose>();}
        sealed class SweepSkin{public SkinnedMeshRenderer skin;public Vector3[] world,model;public int[] triangles;public BoneWeight[] weights;public Transform[] bones;public float[] torso,wing;}
    [Serializable]sealed class CabSweepPose
        {public string pose,failure;public int legacyBodyOracleControls;public bool centerTorso,platformTorso,originalTruckPoseUnchanged,actualSkinOwnsTruckPose,actualCenteredSpannedPatch;public int actualCompleteTriangles,fullSkinBoxIntersections;public float maximumSupportDisplacement,minimumSkinGap=float.MaxValue,platformGap,savedToActualBodyMountDistance,savedToActualBodyMountAngle,actualLateralBodySupportSpan;public Vector3 originalTruckRoot,candidateSupportPlatform,savedOriginalTruckRoot,actualAuthoredSkinTruckRoot;public Vector3[] savedMixedWingSupport;public string[] intersectingBoneWeights;public Vector3[] baselineSupport,candidateSupport;}

        public static string InspectTransientCabFlightSourceFramesWithoutSavingAssets()
        {return InspectTransientCabFlightSourceFramesCore(false);}
        public static string InspectTransientSourceMotionEnvelopeWithoutSavingAssets()
        {return InspectTransientCabFlightSourceFramesCore(true);}
        public static string InspectTransientInfernoConnectedFamilyEnvelopeWithoutSavingAssets(){return InspectTransientCabFlightSourceFramesCore(true,true);}
        static string InspectTransientCabFlightSourceFramesCore(bool envelope,bool infernoOnly=false)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required for complete source-cycle sweep.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes before read-only source-cycle sweep.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit retained native output required.");string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring",envelope?"SourceMotionEnvelopeSweep":"FlightProfileSweep");Directory.CreateDirectory(folder);var report=new CabFlightSweep();if(envelope)report.scope="READ_ONLY_TRANSIENT_ORIGINAL_IDLE_HEAD_GLIDE_WING_ENVELOPE_ORIGINAL_CENTERED_ACTUAL_SKIN_SADDLE_AUTHORITY_COMPLETE_NATIVE_SOURCE_FRAME_MIXER_THREE_LEAF_NO_SAVED_ASSETS_NOT_LIVE_OR_PRODUCTION_ACCEPTANCE";var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
            var watched=AssetDatabase.GetDependencies(DragonRidersAuthoring.ScenePath,true).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Distinct().ToDictionary(path=>path,Hash);var owned=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            report.satControlsPassed=ActualTriangleBoxSat(new Vector3(-.2f,0,0),new Vector3(.2f,0,0),new Vector3(0,.2f,0),Vector3.one,out _)&&!ActualTriangleBoxSat(new Vector3(-.2f,0,2),new Vector3(.2f,0,2),new Vector3(0,.2f,2),Vector3.one,out _)&&ActualTriangleBoxSat(new Vector3(-3,-3,0),new Vector3(3,-3,0),new Vector3(0,3,0),Vector3.one,out _)&&!ActualTriangleBoxSat(new Vector3(2,0,0),new Vector3(0,2,0),new Vector3(2,2,0),Vector3.one*.5f,out _);if(!report.satControlsPassed)throw new InvalidOperationException("Full-skin sweep SAT controls failed.");
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();string originalPhysics=PhysicsSignature(match);var footprint=OriginalFootprints(new[]{match.trucks[0]}).Single();
                foreach(string speciesId in infernoOnly?new[]{"dragon-inferno"}:new[]{"dragon-inferno","dragon-fire"})
                {
                    if(!match.catalog.TryGet(speciesId,out var species))throw new InvalidOperationException("Actual accepted species required.");var saved=match.racers[0].choices.Single(actor=>actor.motor.configuredSpeciesId==speciesId);var preview=new PreviewRenderUtility();var transient=new List<UnityEngine.Object>();DragonRiderSaddle frame=null;Mesh baked=null;
                    try
                    {
                        var actorObject=UnityEngine.Object.Instantiate(saved.gameObject);preview.AddSingleGO(actorObject);actorObject.SetActive(true);var actor=actorObject.GetComponent<DragonCharacter>();var animator=actor.motor.animator;animator.enabled=false;frame=actorObject.GetComponent<DragonRiderSaddle>();if(frame==null||frame.calibrationVersion!=7)throw new InvalidOperationException("Actual accepted seven-version support required; this sweep never recalibrates it.");
                        var truckObject=UnityEngine.Object.Instantiate(match.trucks[0].gameObject);preview.AddSingleGO(truckObject);truckObject.SetActive(true);var truck=truckObject.GetComponent<SportsEventTruck>();frame.rigging.truck=truck;foreach(var body in actorObject.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}foreach(var body in truckObject.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                        if(truck.shape.center!=match.trucks[0].shape.center||truck.shape.size!=match.trucks[0].shape.size||truck.transform.lossyScale!=match.trucks[0].transform.lossyScale||truckObject.GetComponentsInChildren<Rigidbody>(true).Sum(body=>body.mass)!=match.trucks[0].GetComponentsInChildren<Rigidbody>(true).Sum(body=>body.mass))throw new InvalidOperationException("Preserve complete original NWH geometry/scale/mass.");
                        var baseline=animator.runtimeAnimatorController;var controller=baseline;while(controller is AnimatorOverrideController overlay)controller=overlay.runtimeAnimatorController;var flightKeys=MotionClips(ControllerStates(((AnimatorController)controller).layers[0].stateMachine).Single(state=>state.name=="Flight").motion).Distinct().ToArray();if(flightKeys.Length!=5)throw new InvalidOperationException("Actual five original Flight leaves required.");
                        var originalAnchors=frame.surfacePatchAnchors;var stableAnchors=originalAnchors;var originalPoses=actor.motor.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();var solids=truck.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy).ToArray();if(solids.Length==0||solids.Any(collider=>collider is not BoxCollider))throw new InvalidOperationException("Faithful complete original solid boxes required.");var boxes=solids.Cast<BoxCollider>().ToArray();var skins=actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(skin=>skin.sharedMesh!=null).ToArray();var triangleCache=skins.ToDictionary(skin=>skin,skin=>skin.sharedMesh.triangles);var weightCache=skins.ToDictionary(skin=>skin,skin=>skin.sharedMesh.boneWeights);var boneCache=skins.ToDictionary(skin=>skin,skin=>skin.bones);var torsoCache=skins.ToDictionary(skin=>skin,skin=>weightCache[skin].Select(weight=>TorsoWeight(weight,boneCache[skin])).ToArray());var wingCache=skins.ToDictionary(skin=>skin,skin=>weightCache[skin].Select(weight=>WingWeight(weight,boneCache[skin])).ToArray());baked=new Mesh();baked.MarkDynamic();
                        foreach(float wing in envelope?new[]{.25f,.5f}:new[]{0f,5f,10f})
                        {
                            animator.runtimeAnimatorController=baseline;var created=envelope?CreateTransientCabFlightEnvelope(actor,speciesId,wing):CreateTransientCabFlightControllerRecipe(actor,speciesId,speciesId=="dragon-inferno"?45:15,speciesId=="dragon-inferno"?0:30,wing);transient.AddRange(created);var candidate=created.OfType<AnimatorOverrideController>().Single();var profile=new CabSweepProfile{species=speciesId,neckPitch=speciesId=="dragon-inferno"?45:15,headPitch=speciesId=="dragon-inferno"?0:30,wingDihedral=envelope?0:wing,sourceRootFlapFraction=envelope?wing:1,stableTorsoAnchorPrototype=false};report.profiles.Add(profile);
                            foreach(var key in flightKeys){int frames=Mathf.Max(2,Mathf.CeilToInt(key.length*key.frameRate));for(int index=0;index<=frames;index++){Run(new MountedPoseTrial{name="Every original source frame "+key.name+" "+index+"/"+frames,clip=key,fraction=index/(float)frames});profile.sourceFrames++;}}
                            for(int a=0;a<flightKeys.Length;a++)for(int b=a+1;b<flightKeys.Length;b++)foreach(float weight in new[]{.015f,.25f,.5f,.75f,.985f})foreach(float phase in envelope?new[]{0f,.001f,.25f,.5f,.75f,.999f}:new[]{0f,.25f,.5f,.75f}){Run(new MountedPoseTrial{name="Native Flight pair "+flightKeys[a].name+" -> "+flightKeys[b].name+" weight="+weight+" phase="+phase,clip=flightKeys[a],incoming=flightKeys[b],fraction=phase,incomingFraction=phase,incomingWeight=weight});profile.mixedPoses++;}
                            if(envelope)for(int a=0;a<flightKeys.Length;a++)for(int b=a+1;b<flightKeys.Length;b++)foreach(float weight in new[]{.015f,.5f,.985f}){Run(new MountedPoseTrial{name="Native loop-boundary pair "+flightKeys[a].name+" -> "+flightKeys[b].name+" weight="+weight,clip=flightKeys[a],incoming=flightKeys[b],fraction=.999f,incomingFraction=.001f,incomingWeight=weight});profile.mixedPoses++;}
                            if(speciesId=="dragon-fire")foreach(float phase in new[]{0f,.25f,.45f,.535184741f,.75f,.98f}){var trial=ActualV203FireFlightTrial();trial.fraction=phase;Run(trial);profile.nativeThreeLeafPoses++;}
                            profile.fits=profile.poses.All(row=>string.IsNullOrEmpty(row.failure)&&row.fullSkinBoxIntersections==0&&(envelope?row.actualSkinOwnsTruckPose&&row.actualCenteredSpannedPatch:row.maximumSupportDisplacement<=.0002f&&row.originalTruckPoseUnchanged)&&row.centerTorso&&row.platformTorso&&row.minimumSkinGap>=-.15f&&row.platformGap>=.3f&&row.platformGap<=.9f);File.WriteAllText(Path.Combine(folder,"sweep-progress.json"),JsonUtility.ToJson(report,true));

                            void Run(MountedPoseTrial keyTrial)
                            {
                                var row=new CabSweepPose{pose=keyTrial.name};profile.poses.Add(row);try
                                {
                                    animator.runtimeAnimatorController=baseline;SetPatch(originalAnchors);Restore();SampleMountedTrial(actor,Resolved(keyTrial));frame.CaptureEvaluatedAnimationPose();var savedSupport=frame.ReadPose();row.savedOriginalTruckRoot=savedSupport.position+savedSupport.rotation*Vector3.up*footprint.support;row.savedMixedWingSupport=AnchorPoints();SetPatch(stableAnchors);frame.CaptureEvaluatedAnimationPose();var originalSupport=frame.ReadPose();var baselinePoints=AnchorPoints();truck.transform.SetPositionAndRotation(originalSupport.position+originalSupport.rotation*Vector3.up*footprint.support,originalSupport.rotation);var originalTruckPosition=truck.transform.position;var originalTruckRotation=truck.transform.rotation;row.originalTruckRoot=originalTruckPosition;row.baselineSupport=baselinePoints;row.savedToActualBodyMountDistance=Vector3.Distance(row.savedOriginalTruckRoot,originalTruckPosition);row.savedToActualBodyMountAngle=Quaternion.Angle(savedSupport.rotation,originalSupport.rotation);row.actualLateralBodySupportSpan=Vector3.Distance(baselinePoints[1],baselinePoints[2]);
                                    animator.runtimeAnimatorController=candidate;Restore();SampleMountedTrial(actor,Resolved(keyTrial));frame.CaptureEvaluatedAnimationPose();row.candidateSupportPlatform=frame.ReadPose().position;row.candidateSupport=AnchorPoints();for(int index=0;index<5;index++)row.maximumSupportDisplacement=Mathf.Max(row.maximumSupportDisplacement,Vector3.Distance(baselinePoints[index],row.candidateSupport[index]));
                                    var authoredSupport=frame.ReadPose();var expectedRoot=authoredSupport.position+authoredSupport.rotation*Vector3.up*footprint.support;row.actualAuthoredSkinTruckRoot=expectedRoot;var lateral=row.candidateSupport[1]-row.candidateSupport[2];row.actualLateralBodySupportSpan=lateral.magnitude;float centeredFraction=Vector3.Dot(row.candidateSupport[0]-row.candidateSupport[2],lateral)/lateral.sqrMagnitude;row.actualCenteredSpannedPatch=lateral.magnitude>=.5f&&Vector3.Distance(row.candidateSupport[3],row.candidateSupport[4])>=.5f&&centeredFraction>=.25f&&centeredFraction<=.75f;
                                    if(envelope){truck.transform.SetPositionAndRotation(expectedRoot,authoredSupport.rotation);row.savedToActualBodyMountDistance=Vector3.Distance(row.savedOriginalTruckRoot,expectedRoot);row.savedToActualBodyMountAngle=Quaternion.Angle(originalTruckRotation,authoredSupport.rotation);}row.actualSkinOwnsTruckPose=truck.transform.position==expectedRoot&&truck.transform.rotation==authoredSupport.rotation;

                                    // The envelope explicitly follows the genuinely authored skin through the
                                    // same single production saddle authority; original-pose deltas remain evidence.
                                    row.originalTruckPoseUnchanged=truck.transform.position==originalTruckPosition&&truck.transform.rotation==originalTruckRotation;if(envelope){originalTruckPosition=truck.transform.position;originalTruckRotation=truck.transform.rotation;}var actualPlatform=envelope?authoredSupport.position:originalSupport.position;var supportBone=frame.sourceBone;var cache=skins.Select(skin=>{skin.BakeMesh(baked,true);var vertices=baked.vertices;var matrix=actor.motor.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;return new SweepSkin{skin=skin,world=vertices.Select(skin.transform.TransformPoint).ToArray(),model=vertices.Select(matrix.MultiplyPoint3x4).ToArray(),triangles=triangleCache[skin],weights=weightCache[skin],bones=boneCache[skin],torso=torsoCache[skin],wing=wingCache[skin]};}).ToArray();
                                    bool validateLegacy=keyTrial.actualConfiguredFlight||keyTrial.incoming==null&&keyTrial.fraction==0||keyTrial.incoming!=null&&keyTrial.incomingWeight==.015f&&keyTrial.fraction==0;
                                    for(int index=0;index<footprint.bottom.Length;index++){var bottom=originalTruckPosition+originalTruckRotation*footprint.bottom[index];bool found=Measure(bottom,out var point);if(found)row.minimumSkinGap=Mathf.Min(row.minimumSkinGap,Vector3.Dot(bottom-point,actor.motor.transform.up));if(index==4)row.centerTorso=found;}row.platformTorso=Measure(actualPlatform,out var platformSkin);row.platformGap=row.platformTorso?Vector3.Dot(actualPlatform-platformSkin,actor.motor.transform.up):float.MaxValue;
                                    var influences=new List<string>();foreach(var skin in cache)
                                    {
                                        var points=skin.world;var triangles=skin.triangles;row.actualCompleteTriangles+=triangles.Length/3;foreach(var box in boxes)
                                        {
                                            var local=points.Select(point=>box.transform.InverseTransformPoint(point)-box.center).ToArray();var half=box.size*.5f;for(int index=0;index<triangles.Length;index+=3){int a=triangles[index],b=triangles[index+1],c=triangles[index+2];if(SeparatedByBoxAxes(local[a],local[b],local[c],half)||!ActualTriangleBoxSat(local[a],local[b],local[c],half,out _))continue;row.fullSkinBoxIntersections++;var names=new HashSet<string>();foreach(int vertex in new[]{a,b,c}){var w=skin.weights[vertex];Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);void Add(int bone,float value){if(value>0)names.Add(skin.bones[bone].name+"="+value.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}}influences.Add(box.name+"/triangle"+(index/3)+"/"+string.Join("+",names.OrderBy(name=>name,StringComparer.Ordinal)));}
                                        }
                                    }
                                    bool Measure(Vector3 query,out Vector3 point)
                                    {
                                        bool found=TrySweepCachedDorsal(actor.motor.transform,supportBone,query,cache,out point);if(validateLegacy){bool legacy=TryMeasureLiveDorsalSupport(actor.motor.transform,supportBone,query,out var actual);row.legacyBodyOracleControls++;report.cachedBodyOracleComparisons++;if(found!=legacy||found&&Vector3.Distance(point,actual)>.0001f){report.cachedBodyOracleControlsPassed=false;throw new InvalidOperationException("Cached native source-frame body ray disagrees with the unchanged existing actual-skin oracle.");}}return found;
                                    }
                                    row.intersectingBoneWeights=influences.ToArray();
                                }
                                catch(Exception error){row.failure=error.ToString();}
                            }
                            MountedPoseTrial Resolved(MountedPoseTrial source)=>new MountedPoseTrial{name=source.name,clip=source.clip!=null?EffectiveRiderClip(actor,source.clip):null,incoming=source.incoming!=null?EffectiveRiderClip(actor,source.incoming):null,fraction=source.fraction,incomingFraction=source.incomingFraction,incomingWeight=source.incomingWeight,actualConfiguredFlight=source.actualConfiguredFlight,horizontal=source.horizontal,vertical=source.vertical};
                            void Restore(){foreach(var pose in originalPoses)pose.Restore();}
                            void SetPatch(DragonRiderSurfaceAnchor[] patch){frame.surfacePatchAnchors=patch;frame.surfaceVertexA=patch[0].a;frame.surfaceVertexB=patch[0].b;frame.surfaceVertexC=patch[0].c;frame.surfaceBarycentric=patch[0].barycentric;}
                            Vector3[] AnchorPoints(){frame.surfaceRenderer.BakeMesh(baked,true);var vertices=baked.vertices;return frame.surfacePatchAnchors.Select(anchor=>frame.surfaceRenderer.transform.TransformPoint(vertices[anchor.a]*anchor.barycentric.x+vertices[anchor.b]*anchor.barycentric.y+vertices[anchor.c]*anchor.barycentric.z)).ToArray();}
                        }
                    }
                    finally{if(frame!=null)frame.ReleaseGeometrySnapshot();if(baked!=null)UnityEngine.Object.DestroyImmediate(baked);baked=null;preview.Cleanup();foreach(var item in transient)if(item!=null)UnityEngine.Object.DestroyImmediate(item);}
                }
                report.originalNwhExact=originalPhysics==PhysicsSignature(match);if(!report.originalNwhExact)throw new InvalidOperationException("Read-only sweep changed actual original NWH scene bindings.");
            }
            finally
            {
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);var after=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);report.assetsExact=owned.Keys.OrderBy(path=>path,StringComparer.Ordinal).SequenceEqual(after.OrderBy(path=>path,StringComparer.Ordinal))&&owned.All(pair=>Hash(pair.Key)==pair.Value)&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);File.WriteAllText(Path.Combine(folder,"actual-flight-profile-sweep.json"),JsonUtility.ToJson(report,true));if(!report.assetsExact)throw new InvalidOperationException("Source-cycle sweep changed source or saved assets.");
            }
            return JsonUtility.ToJson(report,true);
        }
        // Test-local reuse of one actual native bake for the ten unchanged
        // body rays. Every source clip start, pair start and native three-leaf
        // pose is checked against the original independent legacy oracle.
        static bool TrySweepCachedDorsal(Transform model,Transform bone,Vector3 worldRay,SweepSkin[] skins,out Vector3 worldPoint)
        {
            var query=model.InverseTransformPoint(worldRay);var anchor=model.InverseTransformPoint(bone.position);float best=float.NegativeInfinity;
            foreach(var skin in skins)
            {
                if(skin.skin.name.StartsWith("LWS ",StringComparison.Ordinal))continue;var points=skin.model;var triangles=skin.triangles;
                for(int index=0;index<triangles.Length;index+=3)
                {
                    int ia=triangles[index],ib=triangles[index+1],ic=triangles[index+2];var a=points[ia];var b=points[ib];var c=points[ic];if(Mathf.Abs(a.x-anchor.x)>.8f||Mathf.Abs(a.z-anchor.z)>1.1f||Mathf.Abs(b.x-anchor.x)>.8f||Mathf.Abs(b.z-anchor.z)>1.1f||Mathf.Abs(c.x-anchor.x)>.8f||Mathf.Abs(c.z-anchor.z)>1.1f)continue;var u=new Vector2(b.x-a.x,b.z-a.z);var v=new Vector2(c.x-a.x,c.z-a.z);var delta=new Vector2(query.x-a.x,query.z-a.z);float determinant=u.x*v.y-u.y*v.x;if(Mathf.Abs(determinant)<.0000001f)continue;float wb=(delta.x*v.y-delta.y*v.x)/determinant,wc=(u.x*delta.y-u.y*delta.x)/determinant,wa=1-wb-wc;if(wa<-.0001f||wb<-.0001f||wc<-.0001f)continue;float height=a.y*wa+b.y*wb+c.y*wc;if(height<anchor.y-.1f||height>anchor.y+1.2f||skin.torso[ia]*wa+skin.torso[ib]*wb+skin.torso[ic]*wc<=0||skin.wing[ia]*wa+skin.wing[ib]*wb+skin.wing[ic]*wc>.5f||Vector3.Cross(b-a,c-a).normalized.y<.1f)continue;best=Mathf.Max(best,height);
                }
            }
            worldPoint=float.IsNegativeInfinity(best)?Vector3.zero:model.TransformPoint(new Vector3(query.x,best,query.z));return !float.IsNegativeInfinity(best);
        }
        static bool SeparatedByBoxAxes(Vector3 a,Vector3 b,Vector3 c,Vector3 half)=>Mathf.Min(a.x,Mathf.Min(b.x,c.x))>half.x||Mathf.Max(a.x,Mathf.Max(b.x,c.x))< -half.x||Mathf.Min(a.y,Mathf.Min(b.y,c.y))>half.y||Mathf.Max(a.y,Mathf.Max(b.y,c.y))< -half.y||Mathf.Min(a.z,Mathf.Min(b.z,c.z))>half.z||Mathf.Max(a.z,Mathf.Max(b.z,c.z))< -half.z;
    }
}
#endif
