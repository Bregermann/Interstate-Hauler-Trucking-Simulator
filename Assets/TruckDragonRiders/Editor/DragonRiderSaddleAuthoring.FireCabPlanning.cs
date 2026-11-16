#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable] sealed class FireCabInput { public FireCabView[] views; }
        [Serializable] sealed class FireCabView
        {
            public int owner; public bool exactOwnedStatePreserved; public string exactBeforeState,exactAfterState,actualBaseClips;
            public Vector3 truckEuler; public LeverSkinIdentity[] actualSkinSources; public FireCabFrame[] pureConnectedNeckCandidates;
            public FireCabRays[] sameFrameVirtualCabCandidates;
        }
        [Serializable] sealed class FireCabFrame
        {
            public string recipe; public Vector3 modelRight,modelUp,modelForward,originalMouthForward;
            public LeverJoint[] connectedJointMatrices; public Vector3[] actualSupportPoints;
        }
        [Serializable] sealed class FireCabRays { public string name; public Vector3 eye,forward,up; public LeverRay[] centralWindscreenRays; }
        [Serializable] sealed class FireCabPlanningReport
        {
            public string scope="SAME_FRAME_FULL_WEIGHTED_SKIN_RAYS_AND_PHYSICS_SNAPSHOT_ORIGINAL_BOX_PLANNING_NOT_RENDERED_BOX_OR_REAL_POST_CONTACT_FULL537_RUNTIME_OR_PROFILE_ACCEPTANCE",input,inputSha256,producerEvidence,failure;
            public bool sourcesExact,fixtureAssetsExact,originalSceneExact,baselineRaysExact,baselineAnchorsExact,originalNwhDependencyBytesExact;
            public string clearanceUnits="WORLD_METERS: ReadPose adds orientation*up*dorsalClearance; proposed height moves WHOLE unchanged truck/deck/cab, never just the camera";
            public string boxAuthority="ORIGINAL_SOURCE_BOX_LOCAL_SHAPES_AT_RECORDED_PHYSICS_BODY_POSES_NOT_ACTUAL_RENDERED_BOX_MATRICES";
            public float originalDorsalClearance; public int originalBoxes,fullSkinVertices,fullSkinTriangles;
            public Vector3 saddleUp; public List<FireCabPlanningRow> rows=new List<FireCabPlanningRow>(); public List<FireCabPlanningBox> boxes=new List<FireCabPlanningBox>();
        }
        [Serializable] sealed class FireCabPlanningRow
        {
            public string recipe,cameraBasis; public float extraHeadDegrees,extraPostWorldMeters,proposedDorsalClearance,maximumAnchorDisplacement,muzzleWorldAimChange;
            public Vector3 wholeTruckDeckCabTranslation; public int headHits,centerTriangle,fullSkinBoxPairs; public bool centerHead,meetsRayPlanningThreshold,sourceSkinUnchanged,fiveAnchorsInvariant;
            public Vector3[] candidateAnchors; public FireCabPlanningRay[] rays;
        }
        [Serializable] sealed class FireCabPlanningRay { public Vector2 viewport; public bool head; public int triangle; public float distance; }
        [Serializable] sealed class FireCabPlanningBox { public string path; public Vector3 center,size; public Matrix4x4 physicsLocalToWorld; public float physicsToNearestRecordedTransformPositionDelta,physicsToNearestRecordedTransformQuaternionComponentDelta; }
        sealed class FireCabSnapshotBody { public Vector3 position; public Quaternion rotation; public float mass,renderPositionDelta,renderQuaternionDelta; }

        // This diagnostic reads asset geometry and a frozen actual frame. It
        // never instantiates a motor, evaluates an Animator, or writes a pose.
        public static string InspectFireCabHeadAndSupportedPostPlanning(string inputPath,string producerEvidence)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for read-only Fire planning.");
            var report=new FireCabPlanningReport{input=inputPath,inputSha256=Hash(inputPath),producerEvidence=producerEvidence};
            var input=JsonUtility.FromJson<FireCabInput>(File.ReadAllText(inputPath));
            RequireRiderPassedManifest(producerEvidence,"DragonRiderMotorRootRepresentationTests.UnitAncestorsAndInactiveFireMotorRootRetainFull537GeometryAndExportPristineTestFixture");
            var producer=Directory.GetFiles(Path.Combine(producerEvidence,"rendered-evidence"),"precise-motor-root-fire-course-envelope.json",SearchOption.AllDirectories).Single();
            var proof=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(producer));var fixture=proof.testOnlyColdFixture;
            if(!proof.sourceBytesExact||!proof.nwhExact||!proof.sceneBytesExact||fixture==null||fixture.species!="dragon-fire"||fixture.expectedPoses!=537||!fixture.sameCandidatePassedAllPoses||proof.profiles.Single().candidateSatPairs!=0)throw new InvalidOperationException("Actual full537 Fire fixture/source proof required.");
            report.fixtureAssetsExact=fixture.fixtureAssets.All(row=>File.Exists(row.path)&&string.Equals(Hash(row.path),row.sha256,StringComparison.OrdinalIgnoreCase));if(!report.fixtureAssetsExact)throw new InvalidOperationException("Actual qualified TEST fixture assets changed.");
            report.originalNwhDependencyBytesExact=fixture.nwhAfter.Length>=1002&&fixture.nwhAfter.All(row=>string.Equals(Hash(CurrentRiderPhysicalDependency(row.path)),row.nativeSha256,StringComparison.OrdinalIgnoreCase));if(!report.originalNwhDependencyBytesExact)throw new InvalidOperationException("Original recorded complete NWH dependency bytes changed before snapshot planning.");
            var actor=AssetDatabase.LoadAssetAtPath<GameObject>(fixture.prefab);var frame=actor.GetComponent<DragonRiderSaddle>();var watched=AssetDatabase.GetDependencies(fixture.prefab,true).Where(File.Exists).SelectMany(path=>new[]{path,path+".meta"}).Concat(new[]{inputPath,producer,DragonRidersAuthoring.ScenePath,DragonRidersAuthoring.ScenePath+".meta"}).Where(File.Exists).Distinct().ToDictionary(path=>path,Hash);
            report.originalDorsalClearance=frame.dorsalClearance;if(frame.surfacePatchAnchors.Length!=5||frame.dorsalClearance!=.45f)throw new InvalidOperationException("Preserve actual qualified five-anchor/.45 WORLD-meter support control.");
            string originalScene=Hash(DragonRidersAuthoring.ScenePath);Scene scene=default;var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved incoming scenes.");
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);var match=CourseMatch(scene);
                foreach(var view in input.views)
                {
                    if(view.owner<0||view.owner>=4||!view.exactOwnedStatePreserved||view.exactBeforeState!=view.exactAfterState||!view.actualBaseClips.Contains("TRANSIENT_CAB_"))throw new InvalidOperationException("Immutable actual qualified Fire playback required.");
                    var identity=view.actualSkinSources.Single(value=>value.slot==view.owner);var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(value=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value.sharedMesh,out string guid,out long id);return guid==identity.sourceGuid&&id.ToString()==identity.sourceLocalId;});
                    if(skin!=frame.surfaceRenderer||identity.trueFullMatrixRmse>=.0001f)throw new InvalidOperationException("Actual source weighted-skin identity/control changed.");
                    var captured=view.pureConnectedNeckCandidates.Single(value=>value.recipe=="ZERO_OFFSET_NATIVE_TRUE_BAKE_CONTROL");var matrices=captured.connectedJointMatrices.ToDictionary(value=>value.bone,value=>value.originalWorld);var mesh=skin.sharedMesh;var local=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;var bones=skin.bones;var head=bones.Single(value=>value.name=="RigHead");var pivot=(Vector3)matrices[head.name].GetColumn(3);var indices=mesh.triangles;
                    if(!actor.GetComponent<LWS.TruckDragonKingdom.DragonCharacter>().mouth.IsChildOf(head))throw new InvalidOperationException("Actual calibrated muzzle must be in the selected connected head hierarchy.");
                    var geometry=new LeverPoseData{indices=indices,headWeights=new float[local.Length]};var baseline=Weighted(0);report.fullSkinVertices=local.Length;report.fullSkinTriangles=indices.Length/3;
                    var baselineAnchors=frame.surfacePatchAnchors.Select(point=>Anchor(baseline,point)).ToArray();if(captured.actualSupportPoints.Length!=5||baselineAnchors.Zip(captured.actualSupportPoints,Vector3.Distance).Max()>=.0001f)throw new InvalidOperationException("Full weighted source must reproduce all five actual skin anchors within existing independent-skin control.");report.baselineAnchorsExact=true;
                    // The mounted truck rotation is the already evaluated
                    // saddle ReadPose orientation, including its authored
                    // pitch/bank clamps. Euler round-trip is planning only.
                    report.saddleUp=Quaternion.Euler(view.truckEuler)*Vector3.up;
                    var boxes=FireCabSnapshotBoxes(view,match.trucks[view.owner]);report.boxes.AddRange(boxes);report.originalBoxes=boxes.Length;
                    var rayBases=view.sameFrameVirtualCabCandidates.Where(value=>value.name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL"||value.name=="BODY_FRAME_EYE_AND_ORIENTATION_SCALED_LOCAL_OFFSET").ToArray();if(rayBases.Length!=2)throw new InvalidOperationException("Both actual horizon and corrected full-body cab ray controls required.");
                    foreach(var rayBasis in rayBases)
                    {
                        if(rayBasis.centralWindscreenRays.Length!=25)throw new InvalidOperationException("Full existing25-ray windscreen grid required.");
                        foreach(var ray in rayBasis.centralWindscreenRays){var trace=TraceLeverHead(geometry,baseline,ray);if(trace.triangle!=ray.triangle||Mathf.Abs(trace.distance-ray.distance)>.001f||trace.head!=(ray.ownDragon&&ray.headNeckWeight>=.5f))throw new InvalidOperationException("Full original source reconstruction did not reproduce recorded ray triangle/distance/head membership.");}report.baselineRaysExact=true;
                        foreach(var recipe in new[]{(head:0f,height:0f),(head:30f,height:0f),(head:45f,height:0f),(head:0f,height:.5f),(head:0f,height:1f),(head:0f,height:2f),(head:0f,height:3f),(head:0f,height:4f),(head:0f,height:5f)})
                        {
                            var vertices=recipe.head==0?baseline:Weighted(recipe.head);var translation=report.saddleUp*recipe.height;var row=new FireCabPlanningRow{recipe=recipe.head>0?"ADDITIONAL_CONNECTED_HEAD_DOWN_FLEXION":"WHOLE_TRUCK_DECK_AND_CAB_SUPPORTED_POST_HEIGHT",cameraBasis=rayBasis.name,extraHeadDegrees=recipe.head,extraPostWorldMeters=recipe.height,proposedDorsalClearance=frame.dorsalClearance+recipe.height,wholeTruckDeckCabTranslation=translation,sourceSkinUnchanged=recipe.head==0,candidateAnchors=frame.surfacePatchAnchors.Select(point=>Anchor(vertices,point)).ToArray(),muzzleWorldAimChange=recipe.head==0?0:Vector3.Angle(captured.originalMouthForward,Quaternion.AngleAxis(recipe.head,captured.modelRight)*captured.originalMouthForward)};
                            row.maximumAnchorDisplacement=row.candidateAnchors.Zip(baselineAnchors,Vector3.Distance).Max();row.fiveAnchorsInvariant=row.maximumAnchorDisplacement<.0001f;
                            row.rays=rayBasis.centralWindscreenRays.Select(ray=>{var shifted=new LeverRay{origin=ray.origin+translation,direction=ray.direction,viewport=ray.viewport};var hit=TraceLeverHead(geometry,vertices,shifted);return new FireCabPlanningRay{viewport=ray.viewport,head=hit.head,triangle=hit.triangle,distance=hit.distance};}).ToArray();row.headHits=row.rays.Count(value=>value.head);var center=row.rays.Single(value=>(value.viewport-new Vector2(.5f,.5f)).sqrMagnitude<.000001f);row.centerHead=center.head;row.centerTriangle=center.triangle;row.meetsRayPlanningThreshold=!row.centerHead&&row.headHits<=5;
                            foreach(var box in boxes){var inverse=(Matrix4x4.Translate(translation)*box.physicsLocalToWorld).inverse;var points=vertices.Select(point=>inverse.MultiplyPoint3x4(point)-box.center).ToArray();for(int i=0;i<indices.Length;i+=3){var a=points[indices[i]];var b=points[indices[i+1]];var c=points[indices[i+2]];if(!SeparatedByBoxAxes(a,b,c,box.size*.5f)&&ActualTriangleBoxSat(a,b,c,box.size*.5f,out _))row.fullSkinBoxPairs++;}}
                            report.rows.Add(row);
                        }
                    }
                    Vector3[] Weighted(float degrees)
                    {
                        var result=new Vector3[local.Length];var turn=Quaternion.AngleAxis(degrees,captured.modelRight);var change=Matrix4x4.TRS(pivot,turn,Vector3.one)*Matrix4x4.Translate(-pivot);
                        for(int i=0;i<local.Length;i++){var w=weights[i];Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);void Add(int index,float amount){if(amount<=0)return;var bone=bones[index];var matrix=matrices[bone.name];if(degrees!=0&&(bone==head||bone.IsChildOf(head)))matrix=change*matrix;result[i]+=matrix.MultiplyPoint3x4(binds[index].MultiplyPoint3x4(local[i]))*amount;if(degrees==0&&(bone.name.IndexOf("head",StringComparison.OrdinalIgnoreCase)>=0||bone.name.IndexOf("neck",StringComparison.OrdinalIgnoreCase)>=0))geometry.headWeights[i]+=amount;}}return result;
                    }
                }
            }
            catch(Exception error){report.failure=error.ToString();throw;}
            finally
            {
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sourcesExact=watched.All(row=>File.Exists(row.Key)&&Hash(row.Key)==row.Value);report.originalSceneExact=Hash(DragonRidersAuthoring.ScenePath)==originalScene;report.originalNwhDependencyBytesExact&=fixture.nwhAfter.All(row=>string.Equals(Hash(CurrentRiderPhysicalDependency(row.path)),row.nativeSha256,StringComparison.OrdinalIgnoreCase));
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","FireCabPlanning");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"actual-frame-supported-post-and-head-planning.json"),JsonUtility.ToJson(report,true));}
            }
            if(!report.sourcesExact||!report.originalSceneExact||!report.originalNwhDependencyBytesExact)throw new InvalidOperationException("Read-only planning changed protected actual source/frame/NWH bytes.");return JsonUtility.ToJson(report,true);
        }
        static Vector3 Anchor(Vector3[] vertices,DragonRiderSurfaceAnchor point)=>vertices[point.a]*point.barycentric.x+vertices[point.b]*point.barycentric.y+vertices[point.c]*point.barycentric.z;
        static FireCabPlanningBox[] FireCabSnapshotBoxes(FireCabView view,SportsEventTruck truck)
        {
            var roots=view.exactBeforeState.Split(new[]{"root:"},StringSplitOptions.None).Skip(1).ToArray();if(roots.Length<4)throw new InvalidOperationException("Actual four-truck final snapshot groups required.");var group=roots[roots.Length-4+view.owner];
            var recorded=group.Split('\n').Where(line=>line.StartsWith("B:",StringComparison.Ordinal)).Select(line=>{var bits=line.Substring(line.LastIndexOf(':')+1).Split(',').Where(value=>!string.IsNullOrWhiteSpace(value)).Select(value=>BitConverter.Int32BitsToSingle(int.Parse(value))).ToArray();return new FireCabSnapshotBody{position=new Vector3(bits[0],bits[1],bits[2]),rotation=new Quaternion(bits[3],bits[4],bits[5],bits[6]),mass=bits[13]};}).ToArray();
            var bodies=truck.GetComponentsInChildren<Rigidbody>(true);if(recorded.Length!=bodies.Length||bodies.Length!=2||bodies.Where((body,index)=>body.mass!=recorded[index].mass).Any())throw new InvalidOperationException("Original actual root/cab body membership/order/mass changed; no guessed collider authority.");
            var transforms=group.Split('\n').Where(line=>line.StartsWith("T:",StringComparison.Ordinal)).Select(line=>line.Substring(line.LastIndexOf(':')+1).Split(',').Where(value=>!string.IsNullOrWhiteSpace(value)).Select(value=>BitConverter.Int32BitsToSingle(int.Parse(value))).ToArray()).ToArray();
            foreach(var body in recorded){var nearest=transforms.OrderBy(bits=>(new Vector3(bits[10],bits[11],bits[12])-body.position).sqrMagnitude).First();body.renderPositionDelta=Vector3.Distance(new Vector3(nearest[10],nearest[11],nearest[12]),body.position);body.renderQuaternionDelta=Enumerable.Range(0,4).Max(i=>Mathf.Abs(nearest[13+i]-body.rotation[i]));if(body.renderPositionDelta>=.001f||body.renderQuaternionDelta>=.00001f)throw new InvalidOperationException("Physics-snapshot planning cannot be presented as a nearby recorded render pose; retain explicit representation differences.");}
            var shapes=truck.GetComponentsInChildren<Collider>(true).Where(shape=>shape.enabled&&!shape.isTrigger&&shape.gameObject.activeInHierarchy).ToArray();if(shapes.Length!=2||shapes.Any(shape=>shape is not BoxCollider))throw new InvalidOperationException("Both complete original NWH solid boxes required.");
            return shapes.Cast<BoxCollider>().Select(box=>{int index=Array.IndexOf(bodies,box.attachedRigidbody);if(index<0||!bodies[index].transform.lossyScale.Equals(Vector3.one))throw new InvalidOperationException("Actual box lacks an original captured UNIT-scale body.");var relative=box.transform==bodies[index].transform?Matrix4x4.identity:bodies[index].transform.worldToLocalMatrix*box.transform.localToWorldMatrix;return new FireCabPlanningBox{path=AnimationUtility.CalculateTransformPath(box.transform,truck.transform),center=box.center,size=box.size,physicsLocalToWorld=Matrix4x4.TRS(recorded[index].position,recorded[index].rotation,Vector3.one)*relative,physicsToNearestRecordedTransformPositionDelta=recorded[index].renderPositionDelta,physicsToNearestRecordedTransformQuaternionComponentDelta=recorded[index].renderQuaternionDelta};}).ToArray();
        }
    }
}
#endif
