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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class SizeAxisWitness
        {public string name,precision;public double x,y,z,triangleMinimum,triangleMaximum,boxMinimum,boxMaximum,squareMagnitude;public bool skipped,separatesAtOne;}
        [Serializable]sealed class SizeNumericRow
        {
            public string skin,box,sourceWeights;public int triangle,a,b,c;
            public bool nativeExpandedSat,preciseExpandedIntervalAtOne,legacyExpandedIntervalAtOne,nativeOriginalSat,preciseOriginalIntervalAtOne,preciseExpandedUnbounded;
            public double preciseExpandedLower,preciseExpandedUpper;
            public Vector3 worldA,worldB,worldC,nativeBoxLocalA,nativeBoxLocalB,nativeBoxLocalC,actualRuntimeAnchor,authoredBarycentric,boxCenter,originalHalfExtents,expandedHalfExtents;
            public string preciseAuthoredAnchor,preciseBoxLocalA,preciseBoxLocalB,preciseBoxLocalC;
            public Matrix4x4 boxLocalToWorld,boxWorldToLocal;
            public List<SizeAxisWitness> nativeFloatAxes=new List<SizeAxisWitness>(),preciseDoubleAxes=new List<SizeAxisWitness>();
        }
        [Serializable]sealed class SizeNumericReport
        {
            public string scope="ONE_ORIGINAL_RAW_INFERNO_REAL_TWO_LAYER_POSE_TRIANGLE178_NUMERIC_REPRODUCER_NO_ORACLE_OR_ANATOMY_WAIVER",actualNativeLayerProof;
            public bool sourceBytesExact,originalPhysicsExact,mismatchReproduced;
            public StartupPoseIdentity serializedStaging;
            public List<SizeNumericRow> rows=new List<SizeNumericRow>();
            public List<SizeNumericSkinInventory> allOriginalSkins=new List<SizeNumericSkinInventory>();
        }
        [Serializable]sealed class SizeNumericSkinInventory{public string path,source,guid;public long fileId;public int vertices,triangles;public bool explicitMainTriangleTarget;}
        [Serializable]sealed class StartupBoneIdentity{public string path;public Vector3 localPosition,localScale;public Quaternion localRotation;public bool sourceSkinBone;}
        [Serializable]sealed class StartupLeafIdentity{public int layer;public string path,guid;public long fileId;public float weight,normalized;}
        [Serializable]sealed class StartupChoiceIdentity
        {
            public int slot;public string species,phase,controller,controllerName;public bool active,prepared,animatorEnabled,motorPaused,physicallyMounted;
            public string motorPath,bodyPath,animatorPath,nativeState,animatorUpdateMode;public int nativePostStateCallbacks,nativeAnimationCycle,nativeInputSourceId;public string nativeInputSourceType;public bool inTimeline,animalEnabled,disablePosition,disableRotation,useCustomRotation,lockInput,lockMovement,bodyKinematic,applyRootMotion;public Vector3 actualBodyPosition,actualBodyVelocity,motorLocalPosition;public Quaternion actualBodyRotation,motorLocalRotation;public StartupParameterIdentity[] actualAnimatorParameters;
            public Vector3 motorPosition,motorScale;public Quaternion motorRotation;public int enabledActualSkinRenderers,actualSkinRenderersInObservedFrustum;
            public StartupBoneIdentity[] actualBoneLocals,allActualFrameTransforms;public List<StartupLeafIdentity> actualLeaves=new List<StartupLeafIdentity>();
        }
        [Serializable]sealed class StartupParameterIdentity{public string name,type,value;}
        [Serializable]sealed class StartupPoseIdentity
        {
            public string scope="READ_ONLY_ACTUAL_BODY_AND_SOURCE_CLIP_IDENTITY_NO_SAMPLING_OR_POSE_WRITER_NOT_STAGING_OR_GAMEPLAY_ACCEPTANCE",stage,observedCamera;
            public int frame;public float clock,countdown;public StartupChoiceIdentity[] choices;
            public bool batchMode;public string graphicsDevice,currentPipeline,participantContext;public StartupCameraIdentity[] allActualCameras;
            public StartupNwhBodyIdentity[] actualOriginalNwhBodies;
        }
        [Serializable]sealed class StartupNwhBodyIdentity{public int owner,entityId;public string path;public Vector3 position,localScale;public Quaternion rotation;public float mass;}
        [Serializable]sealed class StartupCameraIdentity{public string name,scene,type,targetTexture;public int entityId;public bool enabled,active,assignedOwnedCamera;public Rect rect,pixelRect;}
        static StartupPoseIdentity ReadStartupPoseIdentity(DragonRidersMatch match,string stage,Camera camera)
        {
            var rows=new List<StartupChoiceIdentity>();var planes=camera!=null?GeometryUtility.CalculateFrustumPlanes(camera):null;
            foreach(var racer in match.racers)foreach(var actor in racer.choices)
            {
                var frame=actor.GetComponent<DragonRiderSaddle>();var motor=actor.motor;var animator=motor.animator;var row=new StartupChoiceIdentity{slot=racer.slot,species=frame.speciesId,phase=racer.Phase.ToString(),controller=AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),controllerName=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.name:null,active=actor.gameObject.activeInHierarchy,prepared=actor.Prepared,animatorEnabled=animator.enabled,motorPaused=motor.Paused,physicallyMounted=racer.Dragon==actor&&racer.PhysicallyMounted,motorPosition=motor.transform.position,motorRotation=motor.transform.rotation,motorScale=motor.transform.lossyScale};
                row.motorPath=AnimationUtility.CalculateTransformPath(motor.transform,racer.match.transform);row.bodyPath=AnimationUtility.CalculateTransformPath(motor.body.transform,racer.match.transform);row.animatorPath=AnimationUtility.CalculateTransformPath(animator.transform,racer.match.transform);row.motorLocalPosition=motor.transform.localPosition;row.motorLocalRotation=motor.transform.localRotation;row.nativePostStateCallbacks=racer.PrepareGraphNativeCallbacks;row.inTimeline=motor.animal.InTimeline;row.nativeAnimationCycle=(int)motor.animal.GetType().GetProperty("CurrentCycle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(motor.animal);row.nativeInputSourceId=motor.animal.InputSource is UnityEngine.Object inputObject?inputObject.GetInstanceID():0;row.nativeInputSourceType=motor.animal.InputSource!=null?motor.animal.InputSource.GetType().FullName:null;row.animalEnabled=motor.animal.enabled;row.disablePosition=motor.animal.DisablePosition;row.disableRotation=motor.animal.DisableRotation;row.useCustomRotation=motor.animal.UseCustomRotation;row.lockInput=motor.animal.LockInput;row.lockMovement=motor.animal.LockMovement;row.bodyKinematic=motor.body.isKinematic;row.actualBodyPosition=motor.body.position;row.actualBodyRotation=motor.body.rotation;row.actualBodyVelocity=motor.body.linearVelocity;row.applyRootMotion=animator.applyRootMotion;row.animatorUpdateMode=animator.updateMode.ToString();row.nativeState=motor.animal.ActiveState!=null?motor.animal.ActiveState.GetType().FullName:null;
                row.actualAnimatorParameters=animator.parameters.Select(parameter=>new StartupParameterIdentity{name=parameter.name,type=parameter.type.ToString(),value=parameter.type==AnimatorControllerParameterType.Float?animator.GetFloat(parameter.nameHash).ToString("R"):parameter.type==AnimatorControllerParameterType.Int?animator.GetInteger(parameter.nameHash).ToString():parameter.type==AnimatorControllerParameterType.Bool?animator.GetBool(parameter.nameHash).ToString():"TRIGGER_NOT_READABLE"}).ToArray();
                var sourceBones=new HashSet<Transform>(motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(skin=>skin.bones).Where(bone=>bone!=null));row.allActualFrameTransforms=motor.GetComponentsInChildren<Transform>(true).Select(bone=>new StartupBoneIdentity{path=AnimationUtility.CalculateTransformPath(bone,motor.transform),localPosition=bone.localPosition,localRotation=bone.localRotation,localScale=bone.localScale,sourceSkinBone=sourceBones.Contains(bone)}).ToArray();row.actualBoneLocals=row.allActualFrameTransforms.Where(bone=>bone.path.Split('/').Last().StartsWith("Rig",StringComparison.Ordinal)).ToArray();
                foreach(var skin in motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(skin=>skin.enabled&&skin.gameObject.activeInHierarchy&&skin.sharedMesh!=null)){row.enabledActualSkinRenderers++;if(camera!=null&&(camera.cullingMask&(1<<skin.gameObject.layer))!=0&&GeometryUtility.TestPlanesAABB(planes,skin.bounds))row.actualSkinRenderersInObservedFrustum++;}
                if(actor.gameObject.activeInHierarchy&&animator.isInitialized)for(int layer=0;layer<animator.layerCount;layer++)foreach(var leaf in animator.GetCurrentAnimatorClipInfo(layer))
                    if(leaf.clip!=null&&leaf.weight>0){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(leaf.clip,out string guid,out long id);row.actualLeaves.Add(new StartupLeafIdentity{layer=layer,path=AssetDatabase.GetAssetPath(leaf.clip),guid=guid,fileId=id,weight=leaf.weight,normalized=animator.GetCurrentAnimatorStateInfo(layer).normalizedTime});}
                rows.Add(row);
            }
            var bodies=new List<StartupNwhBodyIdentity>();for(int owner=0;owner<match.trucks.Length;owner++)foreach(var body in match.trucks[owner].GetComponentsInChildren<Rigidbody>(true))bodies.Add(new StartupNwhBodyIdentity{owner=owner,entityId=body.GetEntityId(),path=AnimationUtility.CalculateTransformPath(body.transform,match.trucks[owner].transform),position=body.position,rotation=body.rotation,localScale=body.transform.localScale,mass=body.mass});
            var owned=match.cameras??Array.Empty<Camera>();return new StartupPoseIdentity{stage=stage,observedCamera=camera!=null?camera.name:null,frame=Time.frameCount,clock=match.Clock,countdown=match.Countdown,choices=rows.ToArray(),batchMode=Application.isBatchMode,graphicsDevice=SystemInfo.graphicsDeviceType.ToString(),currentPipeline=GraphicsSettings.currentRenderPipeline!=null?GraphicsSettings.currentRenderPipeline.GetType().FullName:"BUILT_IN",participantContext="Live="+match.Live+" Running="+match.Running+" Paused="+match.Paused+" count="+match.setup.count+" sources="+string.Join(",",match.setup.players.Take(match.setup.count).Select(player=>player.source.ToString())),actualOriginalNwhBodies=bodies.ToArray(),allActualCameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(view=>new StartupCameraIdentity{name=view.name,entityId=view.GetEntityId(),scene=view.gameObject.scene.path,type=view.cameraType.ToString(),targetTexture=view.targetTexture!=null?view.targetTexture.name:null,enabled=view.enabled,active=view.gameObject.activeInHierarchy,assignedOwnedCamera=owned.Contains(view),rect=view.rect,pixelRect=view.pixelRect}).ToArray()};
        }
        public static string ObserveActualRiderStartupPoseIdentity(DragonRidersMatch match,string stage,Camera camera)=>JsonUtility.ToJson(ReadStartupPoseIdentity(match,stage,camera),true);
        static string PreciseText(SizePlanPoint point)=>point.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+point.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+point.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        static List<SizeAxisWitness> FloatSizeAxes(Vector3 a,Vector3 b,Vector3 c,Vector3 half)
        {
            var result=new List<SizeAxisWitness>();var edges=new[]{b-a,c-b,a-c};var axes=new List<Vector3>{Vector3.right,Vector3.up,Vector3.forward,Vector3.Cross(edges[0],edges[1])};foreach(var edge in edges){axes.Add(Vector3.Cross(edge,Vector3.right));axes.Add(Vector3.Cross(edge,Vector3.up));axes.Add(Vector3.Cross(edge,Vector3.forward));}
            for(int i=0;i<axes.Count;i++){var raw=axes[i];bool skipped=raw.sqrMagnitude<1e-14f;var axis=skipped?Vector3.zero:raw.normalized;float low=Mathf.Min(Vector3.Dot(a,axis),Mathf.Min(Vector3.Dot(b,axis),Vector3.Dot(c,axis))),high=Mathf.Max(Vector3.Dot(a,axis),Mathf.Max(Vector3.Dot(b,axis),Vector3.Dot(c,axis))),radius=half.x*Mathf.Abs(axis.x)+half.y*Mathf.Abs(axis.y)+half.z*Mathf.Abs(axis.z);result.Add(new SizeAxisWitness{name=i<3?"box-axis-"+i:i==3?"triangle-plane":"edge-cross-box-"+(i-4),precision="EXISTING_NATIVE_FLOAT_SAT",x=axis.x,y=axis.y,z=axis.z,squareMagnitude=raw.sqrMagnitude,skipped=skipped,triangleMinimum=low,triangleMaximum=high,boxMinimum=-radius,boxMaximum=radius,separatesAtOne=!skipped&&(low>radius||high< -radius)});}
            return result;
        }
        static List<SizeAxisWitness> DoubleSizeAxes(SizePlanPoint a,SizePlanPoint b,SizePlanPoint c,SizePlanPoint anchor,Vector3 half)
        {
            var result=new List<SizeAxisWitness>();var right=new SizePlanPoint(1,0,0);var up=new SizePlanPoint(0,1,0);var forward=new SizePlanPoint(0,0,1);var edges=new[]{b-a,c-b,a-c};var axes=new List<SizePlanPoint>{right,up,forward,SizePlanPoint.Cross(edges[0],edges[1])};foreach(var edge in edges){axes.Add(SizePlanPoint.Cross(edge,right));axes.Add(SizePlanPoint.Cross(edge,up));axes.Add(SizePlanPoint.Cross(edge,forward));}a-=anchor;b-=anchor;c-=anchor;
            for(int i=0;i<axes.Count;i++){var raw=axes[i];double square=SizePlanPoint.Dot(raw,raw);bool skipped=square<1e-14;var axis=skipped?new SizePlanPoint(0,0,0):raw*(1/Math.Sqrt(square));double low=Math.Min(SizePlanPoint.Dot(a,axis),Math.Min(SizePlanPoint.Dot(b,axis),SizePlanPoint.Dot(c,axis))),high=Math.Max(SizePlanPoint.Dot(a,axis),Math.Max(SizePlanPoint.Dot(b,axis),SizePlanPoint.Dot(c,axis))),center=-SizePlanPoint.Dot(anchor,axis),radius=half.x*Math.Abs(axis.x)+half.y*Math.Abs(axis.y)+half.z*Math.Abs(axis.z);result.Add(new SizeAxisWitness{name=i<3?"box-axis-"+i:i==3?"triangle-plane":"edge-cross-box-"+(i-4),precision="PRECISE_PLANNING_DOUBLE",x=axis.x,y=axis.y,z=axis.z,squareMagnitude=square,skipped=skipped,triangleMinimum=low,triangleMaximum=high,boxMinimum=center-radius,boxMaximum=center+radius,separatesAtOne=!skipped&&(low>center+radius||high<center-radius)});}
            return result;
        }
        public static string InspectRawInfernoTriangle178NumericMismatchWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes first.");
            var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);var setup=EditorSceneManager.GetSceneManagerSetup();var report=new SizeNumericReport();Scene scene=default;DragonRidersMatch match=null;string physics=null;var preview=new PreviewRenderUtility();AnimatorOverrideController raw=null;DragonRiderSaddle frame=null;var mesh=new Mesh();
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);report.serializedStaging=ReadStartupPoseIdentity(match,"SERIALIZED_STOPPED_SCENE_BEFORE_ANY_PUBLIC_START_OR_SAMPLING",null);
                var saved=match.racers.SelectMany(racer=>racer.choices).First(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId=="dragon-inferno");var copy=UnityEngine.Object.Instantiate(saved.gameObject);preview.AddSingleGO(copy);copy.SetActive(true);var actor=copy.GetComponent<DragonCharacter>();frame=copy.GetComponent<DragonRiderSaddle>();actor.motor.animator.enabled=false;foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                var controller=actor.motor.animator.runtimeAnimatorController;while(controller is AnimatorOverrideController layer)controller=layer.runtimeAnimatorController;raw=new AnimatorOverrideController(controller){hideFlags=HideFlags.HideAndDontSave};var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();raw.GetOverrides(overrides);foreach(var entry in overrides)raw[entry.Key]=entry.Key;actor.motor.animator.runtimeAnimatorController=raw;
                var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");if(!catalog.TryGet("dragon-inferno",out var species))throw new InvalidOperationException("Actual original species required.");var trial=CompleteActualLayerBreathTrials(actor,species).Single(pose=>pose.actualConfiguredBreathLayer&&pose.horizontal==0&&pose.vertical==2&&pose.fraction==.45f&&pose.breathLayerEntry&&pose.transitionElapsed==.03f);SampleMountedTrial(actor,trial);report.actualNativeLayerProof=trial.actualNativeLayerProof;frame.CaptureEvaluatedAnimationPose();
                var anchor=frame.ActualSurfacePoint;var precise=PreciseMaterialAnchor(frame.ReadActualSkinVertexWorld(frame.surfaceVertexA),frame.ReadActualSkinVertexWorld(frame.surfaceVertexB),frame.ReadActualSkinVertexWorld(frame.surfaceVertexC),frame.surfaceBarycentric);var truckObject=UnityEngine.Object.Instantiate(match.trucks[0].gameObject);preview.AddSingleGO(truckObject);var truck=truckObject.GetComponent<SportsEventTruck>();truckObject.SetActive(true);foreach(var body in truckObject.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}var physical=frame.ReadPose();truck.transform.SetPositionAndRotation(physical.position+physical.rotation*Vector3.up*OriginalFootprints(new[]{match.trucks[0]}).Single().support,physical.rotation);
                foreach(var skin in actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(skin=>skin.sharedMesh!=null))
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(skin.sharedMesh,out string sourceGuid,out long sourceId);report.allOriginalSkins.Add(new SizeNumericSkinInventory{path=AnimationUtility.CalculateTransformPath(skin.transform,actor.motor.transform),source=AssetDatabase.GetAssetPath(skin.sharedMesh),guid=sourceGuid,fileId=sourceId,vertices=skin.sharedMesh.vertexCount,triangles=skin.sharedMesh.triangles.Length/3,explicitMainTriangleTarget=skin==frame.surfaceRenderer});
                    // This is the ONE reported triangle in the explicitly
                    // identified original main skin. Auxiliary eyelid skins
                    // have independent triangle namespaces; inventory them,
                    // but never reinterpret their triangle178 as this witness.
                    // Full source/profile SAT still measures every skin.
                    if(skin!=frame.surfaceRenderer)continue;
                    skin.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(skin.transform.TransformPoint).ToArray();var indices=skin.sharedMesh.triangles;int ia=indices[178*3],ib=indices[178*3+1],ic=indices[178*3+2];var a=vertices[ia];var b=vertices[ib];var c=vertices[ic];var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;string Influence(int vertex){var w=weights[vertex];return vertex+":"+bones[w.boneIndex0].name+"="+w.weight0.ToString("R")+";"+bones[w.boneIndex1].name+"="+w.weight1.ToString("R")+";"+bones[w.boneIndex2].name+"="+w.weight2.ToString("R")+";"+bones[w.boneIndex3].name+"="+w.weight3.ToString("R");}
                    foreach(var box in truck.GetComponentsInChildren<Collider>(true).Where(shape=>shape.enabled&&!shape.isTrigger&&shape.gameObject.activeInHierarchy).Cast<BoxCollider>())
                    {
                        var inverse=box.transform.worldToLocalMatrix;var la=box.transform.InverseTransformPoint(a)-box.center;var lb=box.transform.InverseTransformPoint(b)-box.center;var lc=box.transform.InverseTransformPoint(c)-box.center;var half=box.size*.5f;var expanded=half+Vector3.one*.02f;var da=PreciseBoxLocal(inverse,new SizePlanPoint(a),box.center);var db=PreciseBoxLocal(inverse,new SizePlanPoint(b),box.center);var dc=PreciseBoxLocal(inverse,new SizePlanPoint(c),box.center);var danchor=PreciseBoxLocal(inverse,precise,box.center);
                        bool interval=ForbiddenPreciseUniformSize(a,b,c,precise,inverse,box.center,expanded,out double low,out double high);bool legacy=ForbiddenUniformSize(la,lb,lc,box.transform.InverseTransformPoint(anchor)-box.center,expanded,out double legacyLow,out double legacyHigh);bool original=ForbiddenPreciseUniformSize(a,b,c,precise,inverse,box.center,half,out double originalLow,out double originalHigh);
                        var row=new SizeNumericRow{skin=AnimationUtility.CalculateTransformPath(skin.transform,actor.motor.transform),box=AnimationUtility.CalculateTransformPath(box.transform,truck.transform),triangle=178,a=ia,b=ib,c=ic,sourceWeights=Influence(ia)+" | "+Influence(ib)+" | "+Influence(ic),worldA=a,worldB=b,worldC=c,nativeBoxLocalA=la,nativeBoxLocalB=lb,nativeBoxLocalC=lc,actualRuntimeAnchor=anchor,authoredBarycentric=frame.surfaceBarycentric,preciseAuthoredAnchor=PreciseText(precise),preciseBoxLocalA=PreciseText(da),preciseBoxLocalB=PreciseText(db),preciseBoxLocalC=PreciseText(dc),boxCenter=box.center,originalHalfExtents=half,expandedHalfExtents=expanded,boxLocalToWorld=box.transform.localToWorldMatrix,boxWorldToLocal=inverse,nativeExpandedSat=!SeparatedByBoxAxes(la,lb,lc,expanded)&&ActualTriangleBoxSat(la,lb,lc,expanded,out _),preciseExpandedIntervalAtOne=interval&&low<=1&&high>=1,legacyExpandedIntervalAtOne=legacy&&legacyLow<=1&&legacyHigh>=1,nativeOriginalSat=!SeparatedByBoxAxes(la,lb,lc,half)&&ActualTriangleBoxSat(la,lb,lc,half,out _),preciseOriginalIntervalAtOne=original&&originalLow<=1&&originalHigh>=1,preciseExpandedLower=low,preciseExpandedUpper=double.IsPositiveInfinity(high)?0:high,preciseExpandedUnbounded=double.IsPositiveInfinity(high),nativeFloatAxes=FloatSizeAxes(la,lb,lc,expanded),preciseDoubleAxes=DoubleSizeAxes(da,db,dc,danchor,expanded)};report.rows.Add(row);report.mismatchReproduced|=row.nativeExpandedSat!=row.preciseExpandedIntervalAtOne;
                    }
                }
            }
            finally
            {
                if(frame!=null)frame.ReleaseGeometrySnapshot();if(raw!=null)UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(mesh);preview.Cleanup();if(match!=null)report.originalPhysicsExact=PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sourceBytesExact=watched.Count==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","MountedSize");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"raw-inferno-triangle178-numeric-witness.json"),JsonUtility.ToJson(report,true));}
            }
            if(!report.sourceBytesExact||!report.originalPhysicsExact)throw new InvalidOperationException("Numeric source-pose diagnosis changed original saved assets or physics.");return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
