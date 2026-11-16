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
        [Serializable]sealed class ActualFlightReplay
        {
            public string scope="READ_ONLY_EXACT_V203_CONFIGURED_THREE_LEAF_STATE_REPLAY_WITH_ORIGINAL_NWH_AND_ACTUAL_TAIL_PROOF_NOT_MOUNT_ACCEPTANCE";
            public bool assetsExact,weightsMatchObserved,colliderControlsPassed,historicalHoverReconstructed,candidateHoverV3;public MountedActionReceipt candidateRecipe;
            public float normalizedTime,stateLength,horizontal,vertical,minimumSkinGap,platformGap;
            public int stateHash;public string savedRuntimeController,actualFloatParameters;
            public int actualTailTrianglesTested,actualTailBoxIntersections,diagnosticWingTrianglesHidden;public bool tailSatControlsPassed;public TailBoxProof[] actualTailIntersectionProofs,actualNegativeRayTriangleSat;
            public string inputProvenance="Native configured-controller inverse solve matches all three observed evaluated weights; observed GetFloat was written after evaluated pose, not claimed as consumed input";public float observedHorizontal=-.0221373457f,observedVertical=.4991438f;
            public ReplayLeaf[] leaves;public ReplayRay[] rays;public string[] images;
        }
        [Serializable]sealed class ReplayLeaf{public string clip,guid;public long localId;public float weight;public bool exactHistoricalHover,exactOriginalForward,exactOriginalBankLeft;}
        [Serializable]sealed class ReplayRay{public int index;public bool found;public float gap;public Vector3 underside,skin;public SurfaceProof actualTriangle;public ColliderDistance[] originalColliderQueries;}
        [Serializable]sealed class TailBoxProof{public string collider;public int a,b,c;public Vector3 worldA,worldB,worldC,boxLocalA,boxLocalB,boxLocalC,boxCenter,boxSize;public Matrix4x4 boxLocalToWorld;public int testedAxes;public bool intersects;public Vector3 separatingAxis;public float separationTriangleMinimum,separationTriangleMaximum,separationBoxRadius;}
        public static string InspectActualV203FireStateWithoutSavingAssets()=>InspectActualV203FireState(false);
        public static string InspectCandidateFireHoverV3WithoutSavingAssets()=>InspectActualV203FireState(true);
        static string InspectActualV203FireState(bool candidate)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for actual-state replay.");
            for(int index=0;index<SceneManager.sceneCount;index++)if(SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Preserve unsaved scenes before read-only replay.");
            string actorPath=ActorPath("dragon-fire"),catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
            var watched=AssetDatabase.GetDependencies(actorPath,true).Concat(new[]{catalogPath,DragonRidersAuthoring.ScenePath}).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Distinct().ToDictionary(path=>path,Hash);
            var mode=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);var setup=EditorSceneManager.GetSceneManagerSetup();
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(catalogPath);if(catalog==null||!catalog.TryGet("dragon-fire",out var species))throw new InvalidOperationException("Actual Fire species required.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit native replay evidence folder required.");string output=Path.Combine(args[at+1],"RiderSaddleAuthoring",candidate?"CandidateFireHoverV3":"ActualV203FireReplay");Directory.CreateDirectory(output);
            Scene scene=default;GameObject loaded=null;DragonRiderSaddle probe=null;PreviewRenderUtility preview=null;AnimatorOverrideController historicalController=null;AnimationClip historicalHover=null;Mesh diagnosticCutaway=null;var report=new ActualFlightReplay();if(candidate){report.candidateHoverV3=true;report.scope="READ_ONLY_CANDIDATE_FIRE_HOVER_V3_ACTUAL_NATIVE_THREE_LEAF_STATE_WITH_ORIGINAL_NWH_SAT_NOT_SAVED_AUTHORING_OR_MATCH_ACCEPTANCE";}
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);var match=scene.GetRootGameObjects().SelectMany(value=>value.GetComponentsInChildren<DragonRidersMatch>(true)).Single();
                var saved=match.racers[3].choices.Select(value=>value.GetComponent<DragonRiderSaddle>()).Single(value=>value.speciesId==species.id);var calibration=new Calibration{anchorMesh=AssetDatabase.GetAssetPath(saved.surfaceRenderer.sharedMesh),anchorA=saved.surfaceVertexA,anchorB=saved.surfaceVertexB,anchorC=saved.surfaceVertexC,anchorBarycentric=saved.surfaceBarycentric,patchAnchors=saved.surfacePatchAnchors};
                loaded=PrefabUtility.LoadPrefabContents(actorPath);preview=new PreviewRenderUtility();var actorObject=UnityEngine.Object.Instantiate(loaded);preview.AddSingleGO(actorObject);actorObject.SetActive(true);var actor=actorObject.GetComponent<DragonCharacter>();
                // Durable historical control: after a legitimate V2 upgrade,
                // reconstruct only the exact original V1 hover in memory. No
                // current saved derivative or designer mapping is overwritten.
                if(!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var originalHover)||!species.TryGetAnimation(DragonAnimationAction.Fly,out var originalForward))throw new InvalidOperationException("Actual historical Fire hover/forward sources required.");
                historicalHover=candidate?CreateCarrierHoverClip(species,originalHover.clip,originalForward.clip,out report.candidateRecipe):CreateLegacyCarrierHoverClip(originalHover.clip,originalForward.clip);
                report.savedRuntimeController=AssetDatabase.GetAssetPath(saved.actor.motor.animator.runtimeAnimatorController);historicalController=UnityEngine.Object.Instantiate((AnimatorOverrideController)saved.actor.motor.animator.runtimeAnimatorController);historicalController[originalHover.clip]=historicalHover;report.historicalHoverReconstructed=!candidate;
                actor.motor.animator.runtimeAnimatorController=historicalController;actor.motor.animator.enabled=false;
                foreach(var body in actor.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                var truckObject=UnityEngine.Object.Instantiate(match.trucks[3].gameObject);preview.AddSingleGO(truckObject);truckObject.SetActive(true);var truck=truckObject.GetComponent<SportsEventTruck>();
                foreach(var body in truckObject.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;body.detectCollisions=true;}
                var trial=ActualV203FireFlightTrial();SampleActualConfiguredFlight(actor,trial);var animator=actor.motor.animator;var state=animator.GetCurrentAnimatorStateInfo(0);var leaves=animator.GetCurrentAnimatorClipInfo(0);
                if(!species.TryGetAnimation(DragonAnimationAction.BankLeft,out var originalBankLeft))throw new InvalidOperationException("Actual observed Fire BankLeft source required.");
                report.normalizedTime=state.normalizedTime;report.stateHash=state.fullPathHash;report.stateLength=state.length;report.horizontal=animator.GetFloat("Horizontal");report.vertical=animator.GetFloat("Vertical");report.actualFloatParameters=string.Join(" | ",animator.parameters.Where(value=>value.type==AnimatorControllerParameterType.Float).Select(value=>value.name+"="+animator.GetFloat(value.name).ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
                report.leaves=leaves.Where(value=>value.weight>.00001f).Select(value=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value.clip,out string guid,out long id);return new ReplayLeaf{clip=value.clip==historicalHover?FireHoverPath:AssetDatabase.GetAssetPath(value.clip),guid=guid,localId=id,weight=value.weight,exactHistoricalHover=value.clip==historicalHover,exactOriginalForward=value.clip==originalForward.clip,exactOriginalBankLeft=value.clip==originalBankLeft.clip};}).ToArray();
                float hoverWeight=leaves.Where(value=>value.clip==historicalHover).Sum(value=>value.weight),forwardWeight=leaves.Where(value=>value.clip==originalForward.clip).Sum(value=>value.weight),bankWeight=leaves.Where(value=>value.clip==originalBankLeft.clip).Sum(value=>value.weight);
                report.weightsMatchObserved=Mathf.Abs(hoverWeight-.47732f)<.0001f&&Mathf.Abs(forwardWeight-.48017f)<.0001f&&Mathf.Abs(bankWeight-.04251f)<.0001f;
                Debug.Log("RIDERS_ACTUAL_V203_REPLAY_LEAVES normalized="+report.normalizedTime+" stateLength="+report.stateLength+" horizontal="+report.horizontal+" vertical="+report.vertical+" leaves="+string.Join(" | ",report.leaves.Select(value=>value.clip+"@"+value.weight.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
                if(!report.weightsMatchObserved)throw new InvalidOperationException("Actual configured native replay leaf weights differ from observed V203; do not claim the guessed pose reproduced it.");
                probe=CreateOriginalFootprintProbe(actor,species,calibration);probe.CaptureEvaluatedAnimationPose();var platform=probe.ReadPose();var original=OriginalFootprints(new[]{match.trucks[3]}).Single();truck.transform.SetPositionAndRotation(platform.position+platform.rotation*Vector3.up*original.support,platform.rotation);Physics.SyncTransforms();
                var shapes=truckObject.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy).ToArray();Vector3 inside=truck.shape.transform.TransformPoint(truck.shape.center),outside=inside+Vector3.one.normalized*1000;
                report.colliderControlsPassed=Vector3.Distance(inside,truck.shape.ClosestPoint(inside))<.0001f&&Vector3.Distance(outside,truck.shape.ClosestPoint(outside))>900;if(!report.colliderControlsPassed)throw new InvalidOperationException("Original NWH native ClosestPoint controls failed.");
                var model=actor.motor.transform;var bone=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);var rays=new List<ReplayRay>();report.minimumSkinGap=float.MaxValue;
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                {
                    Vector3 bottom=truck.shape.transform.TransformPoint(truck.shape.center+Vector3.Scale(truck.shape.size*.5f,new Vector3(x,-1,z)));var ray=new ReplayRay{index=(x+1)*3+z+1,underside=bottom};ray.found=TryMeasureLiveDorsalSupport(model,bone,bottom,out ray.skin);ray.gap=ray.found?Vector3.Dot(bottom-ray.skin,model.up):float.MaxValue;
                    if(ray.found){report.minimumSkinGap=Mathf.Min(report.minimumSkinGap,ray.gap);if(ray.gap<-.15f){var query=model.InverseTransformPoint(bottom);var origin=model.InverseTransformPoint(bone.position);DorsalSurface(model,bone,new Vector2(query.x-origin.x,query.z-origin.z),"actual-V203-configured-three-leaf-negative-ray",out ray.actualTriangle,true);ray.originalColliderQueries=shapes.Select(shape=>{Vector3 closest=shape.ClosestPoint(ray.skin);float distance=Vector3.Distance(closest,ray.skin);return new ColliderDistance{path=AnimationUtility.CalculateTransformPath(shape.transform,truck.transform),type=shape.GetType().Name,enabled=shape.enabled,active=shape.gameObject.activeInHierarchy,closestPoint=closest,distance=distance,pointInsideOrOnBoundary=distance<.0001f};}).ToArray();}}rays.Add(ray);
                }
                report.rays=rays.ToArray();report.platformGap=TryMeasureLiveDorsalSupport(model,bone,platform.position,out var platformSkin)?Vector3.Dot(platform.position-platformSkin,model.up):float.MaxValue;
                InspectActualTailAgainstOriginalBoxes(probe.surfaceRenderer,shapes,truck.transform,report);
                preview.ambientColor=new Color(.35f,.38f,.43f,1);preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(40,35,0);preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(310,210,0);preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.09f,.12f,.16f,1);preview.camera.fieldOfView=38;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=500;
                var bounds=ActualRenderedGeometryBounds(actorObject);var truckBounds=ActualRenderedGeometryBounds(truckObject);bounds.Encapsulate(truckBounds);report.images=new[]{Path.Combine(output,"actual-three-leaf-full-mount.png"),Path.Combine(output,"actual-three-leaf-tail-chassis.png"),Path.Combine(output,"DIAGNOSTIC-TAIL-ONLY-FULL-TRUCK-actual-rear.png"),Path.Combine(output,"DIAGNOSTIC-TAIL-ONLY-FULL-TRUCK-actual-underside.png")};CaptureInterference(preview,bounds.center,new Vector3(-1,.55f,1),Mathf.Max(8,bounds.extents.magnitude)*2.8f,report.images[0]);CaptureInterference(preview,truckBounds.center,new Vector3(-1,-.15f,-.35f),Mathf.Max(4,truckBounds.extents.magnitude)*2.8f,report.images[1]);
                diagnosticCutaway=CreateReadOnlyTailCutaway(preview,probe.surfaceRenderer,out int hidden);report.diagnosticWingTrianglesHidden=hidden;preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(3,truckBounds.extents.magnitude)*1.15f;float outsideWholeMount=Mathf.Max(12,bounds.extents.magnitude*4);CaptureInterference(preview,truckBounds.center,new Vector3(-1,.15f,-1),outsideWholeMount,report.images[2]);CaptureInterference(preview,truckBounds.center,new Vector3(.8f,-.7f,-1),outsideWholeMount,report.images[3]);
            }
            finally
            {
                if(probe!=null){probe.ReleaseGeometrySnapshot();UnityEngine.Object.DestroyImmediate(probe.gameObject);}if(preview!=null)preview.Cleanup();if(diagnosticCutaway!=null)UnityEngine.Object.DestroyImmediate(diagnosticCutaway);if(historicalController!=null)UnityEngine.Object.DestroyImmediate(historicalController);if(historicalHover!=null)UnityEngine.Object.DestroyImmediate(historicalHover);if(loaded!=null)PrefabUtility.UnloadPrefabContents(loaded);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
                var after=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);report.assetsExact=mode.Keys.OrderBy(path=>path,StringComparer.Ordinal).SequenceEqual(after.OrderBy(path=>path,StringComparer.Ordinal))&&mode.All(pair=>Hash(pair.Key)==pair.Value)&&watched.All(pair=>Hash(pair.Key)==pair.Value);try{File.WriteAllText(Path.Combine(output,"actual-three-leaf-replay.json"),JsonUtility.ToJson(report,true));}catch(Exception error){Debug.LogWarning("Could not retain read-only native observation: "+error.Message);}if(!report.assetsExact)throw new InvalidOperationException("Read-only observed-state replay changed saved assets.");
            }
            string json=JsonUtility.ToJson(report,true);File.WriteAllText(Path.Combine(output,"actual-three-leaf-replay.json"),json);return json;
        }
        static float TailWeight(BoneWeight weight,Transform[] bones)
        {
            float result=0;var ids=new[]{weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};var values=new[]{weight.weight0,weight.weight1,weight.weight2,weight.weight3};for(int index=0;index<4;index++)if(values[index]>0&&bones[ids[index]].name.StartsWith("RigTail",StringComparison.Ordinal))result+=values[index];return result;
        }
        static void InspectActualTailAgainstOriginalBoxes(SkinnedMeshRenderer skin,Collider[] colliders,Transform truck,ActualFlightReplay report)
        {
            report.tailSatControlsPassed=ActualTriangleBoxSat(new Vector3(-.2f,0,0),new Vector3(.2f,0,0),new Vector3(0,.2f,0),Vector3.one,out _)
                &&!ActualTriangleBoxSat(new Vector3(-.2f,0,2),new Vector3(.2f,0,2),new Vector3(0,.2f,2),Vector3.one,out _)
                &&ActualTriangleBoxSat(new Vector3(-3,-3,0),new Vector3(3,-3,0),new Vector3(0,3,0),Vector3.one,out _)
                &&!ActualTriangleBoxSat(new Vector3(2,0,0),new Vector3(0,2,0),new Vector3(2,2,0),Vector3.one*.5f,out _);
            if(!report.tailSatControlsPassed)throw new InvalidOperationException("Actual triangle/OBB SAT failed contained, separated, no-vertex-inside crossing or diagonal-axis controls.");
            if(colliders.Length==0||colliders.Any(value=>value is not BoxCollider))throw new InvalidOperationException("Actual NWH requires enabled original solid boxes; add a faithful native query before interpreting missing/non-box clearance.");var mesh=new Mesh();var proofs=new List<TailBoxProof>();var negativeProofs=new List<TailBoxProof>();
            try
            {
                skin.BakeMesh(mesh,true);var points=mesh.vertices.Select(skin.transform.TransformPoint).ToArray();var triangles=skin.sharedMesh.triangles;var weights=skin.sharedMesh.boneWeights;var tail=weights.Select(value=>TailWeight(value,skin.bones)).ToArray();
                for(int index=0;index+2<triangles.Length;index+=3)
                {
                    int a=triangles[index],b=triangles[index+1],c=triangles[index+2];if(tail[a]<=0&&tail[b]<=0&&tail[c]<=0)continue;report.actualTailTrianglesTested++;
                    foreach(BoxCollider box in colliders)
                    {
                        var matrix=box.transform.localToWorldMatrix;var x=(Vector3)matrix.GetColumn(0);var y=(Vector3)matrix.GetColumn(1);var z=(Vector3)matrix.GetColumn(2);if(Mathf.Abs(Vector3.Dot(x.normalized,y.normalized))>.0001f||Mathf.Abs(Vector3.Dot(x.normalized,z.normalized))>.0001f||Mathf.Abs(Vector3.Dot(y.normalized,z.normalized))>.0001f)throw new InvalidOperationException("Sheared original collider cannot be substituted by a simple OBB query.");
                        var la=box.transform.InverseTransformPoint(points[a])-box.center;var lb=box.transform.InverseTransformPoint(points[b])-box.center;var lc=box.transform.InverseTransformPoint(points[c])-box.center;
                        bool intersects=ActualTriangleBoxSatDetailed(la,lb,lc,box.size*.5f,out int axes,out var separation);bool actualRayTriangle=(report.rays??Array.Empty<ReplayRay>()).Any(ray=>ray.actualTriangle!=null&&new HashSet<int>(ray.actualTriangle.vertexInfluences.Select(value=>value.vertex)).SetEquals(new[]{a,b,c}));
                        if(intersects||actualRayTriangle)
                        {
                            float pa=Vector3.Dot(la,separation),pb=Vector3.Dot(lb,separation),pc=Vector3.Dot(lc,separation);var half=box.size*.5f;var proof=new TailBoxProof{collider=AnimationUtility.CalculateTransformPath(box.transform,truck),a=a,b=b,c=c,worldA=points[a],worldB=points[b],worldC=points[c],boxLocalA=la,boxLocalB=lb,boxLocalC=lc,boxCenter=box.center,boxSize=box.size,boxLocalToWorld=matrix,testedAxes=axes,intersects=intersects,separatingAxis=separation,separationTriangleMinimum=Mathf.Min(pa,Mathf.Min(pb,pc)),separationTriangleMaximum=Mathf.Max(pa,Mathf.Max(pb,pc)),separationBoxRadius=half.x*Mathf.Abs(separation.x)+half.y*Mathf.Abs(separation.y)+half.z*Mathf.Abs(separation.z)};if(actualRayTriangle)negativeProofs.Add(proof);if(intersects){report.actualTailBoxIntersections++;proofs.Add(proof);}
                        }
                    }
                }
                report.actualTailIntersectionProofs=proofs.ToArray();report.actualNegativeRayTriangleSat=negativeProofs.ToArray();
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        static bool ActualTriangleBoxSat(Vector3 a,Vector3 b,Vector3 c,Vector3 half,out int tested)=>ActualTriangleBoxSatDetailed(a,b,c,half,out tested,out _);
        static bool ActualTriangleBoxSatDetailed(Vector3 a,Vector3 b,Vector3 c,Vector3 half,out int tested,out Vector3 separatingAxis)
        {
            tested=0;separatingAxis=Vector3.zero;Vector3[] boxAxes={Vector3.right,Vector3.up,Vector3.forward},edges={b-a,c-b,a-c};var axes=new List<Vector3>(boxAxes){Vector3.Cross(edges[0],edges[1])};foreach(var edge in edges)foreach(var axis in boxAxes)axes.Add(Vector3.Cross(edge,axis));
            foreach(var raw in axes){if(raw.sqrMagnitude<1e-14f)continue;var axis=raw.normalized;tested++;float pa=Vector3.Dot(a,axis),pb=Vector3.Dot(b,axis),pc=Vector3.Dot(c,axis),radius=half.x*Mathf.Abs(axis.x)+half.y*Mathf.Abs(axis.y)+half.z*Mathf.Abs(axis.z);if(Mathf.Min(pa,Mathf.Min(pb,pc))>radius||Mathf.Max(pa,Mathf.Max(pb,pc))< -radius){separatingAxis=axis;return false;}}return true;
        }
        static Mesh CreateReadOnlyTailCutaway(PreviewRenderUtility preview,SkinnedMeshRenderer skin,out int hidden)
        {
            var source=skin.sharedMesh;var mesh=new Mesh{name="READ ONLY actual baked TAIL triangles; every other dragon triangle hidden for rear-clearance diagnosis"};skin.BakeMesh(mesh,true);var weights=source.boneWeights;var wing=weights.Select(value=>WingWeight(value,skin.bones)).ToArray();var tail=weights.Select(value=>TailWeight(value,skin.bones)).ToArray();hidden=0;
            for(int part=0;part<source.subMeshCount;part++){var sourceTriangles=source.GetTriangles(part);var visible=new List<int>();for(int index=0;index+2<sourceTriangles.Length;index+=3){int a=sourceTriangles[index],b=sourceTriangles[index+1],c=sourceTriangles[index+2];if(tail[a]<=0&&tail[b]<=0&&tail[c]<=0){if(wing[a]>.5f||wing[b]>.5f||wing[c]>.5f)hidden++;continue;}visible.Add(a);visible.Add(b);visible.Add(c);}mesh.SetTriangles(visible,part);}mesh.RecalculateBounds();
            var transformMatrix=skin.transform.localToWorldMatrix;mesh.vertices=mesh.vertices.Select(transformMatrix.MultiplyPoint3x4).ToArray();var normalMatrix=transformMatrix.inverse.transpose;mesh.normals=mesh.normals.Select(value=>normalMatrix.MultiplyVector(value).normalized).ToArray();mesh.RecalculateBounds();
            var view=new GameObject("DIAGNOSTIC ONLY actual world-space tail triangles; full original truck; source SAT used unfiltered geometry");preview.AddSingleGO(view);view.AddComponent<MeshFilter>().sharedMesh=mesh;view.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.forceRenderingOff=true;return mesh;
        }
    }
}
#endif
