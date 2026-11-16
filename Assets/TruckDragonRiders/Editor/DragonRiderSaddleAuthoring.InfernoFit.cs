#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
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
        [Serializable]sealed class InfernoFitReport
        {
            public string scope="READ_ONLY_ALL699_ORIGINAL_SOURCE_POSES_FULL_SKIN_NWH_TIMBER_AND_LINE_ENVELOPE_CURRENT_START_COLLIDERS_AND_CONSERVATIVE_RING_RISK_NOT_COURSE_OR_RUNTIME_ACCEPTANCE",nativeEvidence,failure;
            public bool sceneBytesExact,sourceBytesExact,nwhExact,qualifiedNativeGeometry,conservativeEnvelopeAndStartupClear;
            public float factor;public int poses;public Bounds fullEnvelope,skinEnvelope,nwhEnvelope,harnessEnvelope;
            public List<RiderNativeSourceBaseline> actualNativeSourceBaselines=new List<RiderNativeSourceBaseline>();
            public List<InfernoFitContact> startupContacts=new List<InfernoFitContact>();
            public List<InfernoFitGate> gates=new List<InfernoFitGate>();
            public List<InfernoFitTriangle> limitingTriangles=new List<InfernoFitTriangle>();
            public List<InfernoFitCorridor> neutralStrictTorsoCorridors=new List<InfernoFitCorridor>();
            public string calibrationFailure;public float derivedRearward;public bool derivedStrictPatch;public SurfaceProof[] measuredPatch;
        }
        [Serializable]sealed class InfernoFitContact{public int start,otherStart=-1;public string own,other;public float depth;public Vector3 direction;}
        [Serializable]sealed class InfernoFitGate{public int gate;public float radius,maximumConservativeProjectedRadius;public bool fullEnvelopeFits;}
        [Serializable]sealed class InfernoFitCorridor{public float minimum,maximum,center;public string authority;}
        [Serializable]sealed class InfernoFitTriangle
        {
            public string pose,skin,sourceClip,incomingClip,sourceGuid,incomingGuid;public long sourceId,incomingId;
            public int triangle,originalTriangleBoxContacts;public float phase,incomingPhase,incomingWeight,unclampedPitch,unclampedBank,actualPitch,actualBank;
            public Vector3 anchor,actualNormal,physicalUp,worldA,worldB,worldC;public Matrix4x4 modelWorld;
            public Influence[] vertices;public RigControlBone[] bones;public DragonRiderSurfaceAnchor[] materialAnchors;
            public List<SavedTriangleGeometry> actualBoxes=new List<SavedTriangleGeometry>();
        }
        static MountedSizeProfile RequireActualInfernoSizeProof(string folder,List<RiderNativeSourceBaseline> baselines)
        {
            var xml=new XmlDocument();xml.Load(Path.Combine(folder,"tests.xml"));
            var cases=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(node=>node.Attributes["fullname"].Value=="DragonRiderProvenStartupSizeTests.ActualPassedPublicStartupSelectsRealInfernoDefaultWhileRetainingImportedStagingAndAll699NativePoseGates").ToArray();
            if(cases.Length!=1||cases[0].Attributes["result"].Value!="Passed")throw new InvalidOperationException("Actual passed all699 native Inferno geometry case required.");
            var report=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(folder,"rendered-evidence"),"precise-actual-public-startup-inferno-size-derivation.json",SearchOption.AllDirectories).Single()));
            var row=report.profiles.Single(value=>value.profile=="SAVED_MOUNTED_WITH_ACTUAL_PUBLIC_DEFAULT_ZERO");
            if(!report.sourceBytesExact||!report.sceneBytesExact||!report.nwhExact||!row.exactOriginalBoxAcceptance||!row.originalFootprintFits||row.actualPoses!=699||row.invalidAnatomicalPoses!=0||row.candidateSatPairs!=0||row.actualCandidateSkinScaleError>=.001f||row.maximumActualWheelGap>.03f)throw new InvalidOperationException("Complete positive original geometry and source authority required; no partial size report.");
            var manifest=JsonUtility.FromJson<FlightSourceRows>("{\"rows\":"+File.ReadAllText(Path.Combine(folder,"input-manifest.json"))+"}").rows;
            RequireRecordedStartupSourceHash(manifest,DragonRidersAuthoring.ScenePath,"Current complete Inferno whole-scene fit proof");var passed=report.profiles.Where(value=>value.species=="dragon-inferno"&&value.actualPoses==699&&value.invalidAnatomicalPoses==0&&value.sourceMotionUnchanged&&value.exactOriginalBoxAcceptance&&value.originalFootprintFits&&value.candidateSatPairs==0).ToArray();if(passed.Length!=2)throw new InvalidOperationException("Both actual passed complete native Inferno profiles required for exact native source baseline.");foreach(string path in row.sourceFiles.Select(value=>value.path).Distinct())RequireActualNativeCapturedDependency(manifest,passed.Select(value=>value.sourceFiles.ToArray()),path,RiderEvidenceHostRoot(folder),baselines,"Current complete Inferno actual measured source identity");
            return row;
        }
        static void FitInclude(ref Bounds bounds,ref bool found,Vector3 point){if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);}
        static IEnumerable<Vector3> FitCorners(Bounds bounds){for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)yield return bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));}
        static void FitSource(AnimationClip clip,out string path,out string guid,out long id){path=clip!=null?AssetDatabase.GetAssetPath(clip):null;guid=null;id=0;if(clip!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip,out guid,out id);}
        static void InspectInfernoLimitingTriangles(DragonCharacter actor,DragonRiderSaddle frame,SportsEventTruck truck,MountedPoseTrial trial,InfernoFitReport report)
        {
            if(trial.name!="Actual graph blend Turn Left -> Dive weight=0.25")return;
            var skin=frame.surfaceRenderer;var mesh=new Mesh();try
            {
                skin.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(skin.transform.TransformPoint).ToArray();var ids=skin.sharedMesh.triangles;var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;var model=actor.motor.transform;
                foreach(int triangle in new[]{2493,2495})
                {
                    int a=ids[triangle*3],b=ids[triangle*3+1],c=ids[triangle*3+2];var normal=frame.ActualPatchNormal;var actual=(Quaternion.Inverse(model.rotation)*Quaternion.LookRotation(Vector3.ProjectOnPlane(model.forward,normal).normalized,normal)).eulerAngles;
                    var row=new InfernoFitTriangle{pose=trial.name,skin=AssetDatabase.GetAssetPath(skin.sharedMesh),triangle=triangle,phase=trial.fraction,incomingPhase=trial.incomingFraction,incomingWeight=trial.incomingWeight,anchor=frame.ActualSurfacePoint,actualNormal=normal,physicalUp=frame.ReadPose().rotation*Vector3.up,unclampedPitch=Mathf.DeltaAngle(0,actual.x),unclampedBank=Mathf.DeltaAngle(0,actual.z),actualPitch=frame.CurrentPitch,actualBank=frame.CurrentBank,worldA=vertices[a],worldB=vertices[b],worldC=vertices[c],modelWorld=model.localToWorldMatrix,materialAnchors=frame.surfacePatchAnchors,vertices=new[]{a,b,c}.Select(vertex=>SkinInfluence(vertex,model.InverseTransformPoint(vertices[vertex]),weights[vertex],bones)).ToArray()};
                    FitSource(trial.clip,out row.sourceClip,out row.sourceGuid,out row.sourceId);FitSource(trial.incoming,out row.incomingClip,out row.incomingGuid,out row.incomingId);
                    row.bones=model.GetComponentsInChildren<Transform>(true).Where(bone=>new[]{"RigRibcage","RigSpine1","RigPelvis","RigLWing01","RigRWing01","RigNeck1"}.Contains(bone.name)).Select(bone=>new RigControlBone{name=bone.name,parent=bone.parent?.name,worldPosition=bone.position,worldRotation=bone.rotation,worldRight=bone.right,localPosition=bone.localPosition,localRotation=bone.localRotation}).ToArray();
                    foreach(var box in truck.GetComponentsInChildren<BoxCollider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy)){var la=box.transform.InverseTransformPoint(vertices[a])-box.center;var lb=box.transform.InverseTransformPoint(vertices[b])-box.center;var lc=box.transform.InverseTransformPoint(vertices[c])-box.center;if(!SeparatedByBoxAxes(la,lb,lc,box.size*.5f)&&ActualTriangleBoxSat(la,lb,lc,box.size*.5f,out _))row.originalTriangleBoxContacts++;row.actualBoxes.Add(new SavedTriangleGeometry{box=AnimationUtility.CalculateTransformPath(box.transform,truck.transform),skin=row.skin,triangle=triangle,worldA=vertices[a],worldB=vertices[b],worldC=vertices[c],boxLocalA=box.transform.InverseTransformPoint(vertices[a])-box.center,boxLocalB=box.transform.InverseTransformPoint(vertices[b])-box.center,boxLocalC=box.transform.InverseTransformPoint(vertices[c])-box.center,boxCenter=box.center,boxSize=box.size,actualBoxLocalToWorld=box.transform.localToWorldMatrix});}report.limitingTriangles.Add(row);
                }
            }finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        // Exact source-triangle / three longitudinal ray-line intersections.
        // These conservative corridors contain only upward triangles whose
        // THREE authored vertices are majority torso. No patch/angle grid.
        static void DeriveInfernoStrictTorsoCorridor(DragonCharacter actor,DragonSpeciesDefinition species,DragonRiderSaddle frame,SavedJoint[] rest,InfernoFitReport report)
        {
            if(!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral))throw new InvalidOperationException("Original neutral mapping required.");
            foreach(var joint in rest)joint.Restore();var clip=EffectiveRiderClip(actor,neutral.clip);clip.SampleAnimation(actor.motor.gameObject,clip.length*.25f);var model=actor.motor.transform;var bone=frame.sourceBone;var origin=model.InverseTransformPoint(bone.position);var skin=frame.surfaceRenderer;var mesh=new Mesh();
            try
            {
                skin.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(point=>model.InverseTransformPoint(skin.transform.TransformPoint(point))).ToArray();var ids=skin.sharedMesh.triangles;var weights=skin.sharedMesh.boneWeights;var lanes=new List<Vector2[]>();
                foreach(float lateral in new[]{0f,.15f,-.15f})
                {
                    var ranges=new List<Vector2>();float x=origin.x+lateral;
                    for(int i=0;i<ids.Length;i+=3)
                    {
                        var index=new[]{ids[i],ids[i+1],ids[i+2]};var p=index.Select(id=>vertices[id]).ToArray();if(Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized.y<.1f||index.Any(id=>TorsoWeight(weights[id],skin.bones)<=.5f)||p.Any(point=>Mathf.Abs(point.x-origin.x)>.8f||Mathf.Abs(point.z-origin.z)>1.1f||point.y<origin.y-.1f||point.y>origin.y+1.2f))continue;
                        var cuts=new List<float>();for(int edge=0;edge<3;edge++){var a=p[edge];var b=p[(edge+1)%3];if(Mathf.Abs(a.x-b.x)<1e-7f){if(Mathf.Abs(x-a.x)<1e-6f){cuts.Add(a.z-origin.z);cuts.Add(b.z-origin.z);}continue;}float t=(x-a.x)/(b.x-a.x);if(t>=0&&t<=1)cuts.Add(Mathf.Lerp(a.z,b.z,t)-origin.z);}
                        if(cuts.Count>=2)ranges.Add(new Vector2(cuts.Min(),cuts.Max()));
                    }
                    var merged=new List<Vector2>();foreach(var range in ranges.OrderBy(range=>range.x)){if(merged.Count==0||range.x>merged[merged.Count-1].y+1e-6f)merged.Add(range);else{var previous=merged[merged.Count-1];previous.y=Mathf.Max(previous.y,range.y);merged[merged.Count-1]=previous;}}lanes.Add(merged.ToArray());
                }
                var common=lanes[0].ToList();foreach(var lane in lanes.Skip(1))common=common.SelectMany(a=>lane.Select(b=>new Vector2(Mathf.Max(a.x,b.x),Mathf.Min(a.y,b.y)))).Where(range=>range.y>=range.x).ToList();
                foreach(var range in common.Where(range=>range.y-range.x>=.3f))report.neutralStrictTorsoCorridors.Add(new InfernoFitCorridor{minimum=range.x+.15f,maximum=range.y-.15f,center=(range.x+range.y)*.5f,authority="ORIGINAL_FLYIDLE_POINT25_UPWARD_ALL3VERTEX_MAJORITY_TORSO_AT_CENTER_AND_BOTH_POINT15_LATERAL_RAYS_WITH_ORIGINAL_POINT15_FORE_AFT_BASELINE"});
                var candidate=report.neutralStrictTorsoCorridors.Where(range=>range.center<0).OrderBy(range=>Mathf.Abs(range.center)).FirstOrDefault();if(candidate==null){report.calibrationFailure="No complete conservative bilateral majority-torso corridor supports the original0.30m-model longitudinal baseline. No point relocation or smaller radius invented.";return;}
                report.derivedRearward=candidate.center;foreach(var joint in rest)joint.Restore();var poses=rest.Select(joint=>new BonePose{bone=joint.joint,position=joint.position,rotation=joint.rotation}).ToArray();
                try{var measured=MeasureActualDorsalCandidate(actor,species,neutral,clip,poses,Quaternion.Inverse(bone.rotation)*model.rotation,bone.InverseTransformDirection(model.up).normalized,candidate.center,.15f,true);var patch=measured.proof.Where(point=>point.context==species.id+"-"+neutral.action+"-0.25-patch"+candidate.center.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).ToArray();VerifyCenteredMaterialPatch(model,bone,patch,candidate.center,.15f,true,out _,out _,out _);report.measuredPatch=patch;report.derivedStrictPatch=true;}catch(Exception error){report.calibrationFailure=error.ToString();}
            }finally{UnityEngine.Object.DestroyImmediate(mesh);foreach(var joint in rest)joint.Restore();}
        }
        public static string InspectQualifiedInfernoFullEnvelopeAndAnatomicalFitWithoutSavingAssets(string nativeEvidence,string startupEvidence)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes.");
            var report=new InfernoFitReport{nativeEvidence=nativeEvidence};var qualified=RequireActualInfernoSizeProof(nativeEvidence,report.actualNativeSourceBaselines);var startup=RequireProvenActualInfernoStartup(startupEvidence);report.factor=qualified.roundedCandidateFactor;report.qualifiedNativeGeometry=true;var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);string sceneHash=Hash(DragonRidersAuthoring.ScenePath);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonRidersMatch match=null;string physics=null;var preview=new PreviewRenderUtility();DragonRiderSaddle frame=null;var mesh=new Mesh();
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);var saved=match.racers.SelectMany(racer=>racer.choices).First(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId=="dragon-inferno");var copy=UnityEngine.Object.Instantiate(saved.gameObject);preview.AddSingleGO(copy);copy.SetActive(true);var actor=copy.GetComponent<DragonCharacter>();frame=copy.GetComponent<DragonRiderSaddle>();actor.motor.animator.enabled=false;foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                var truckObject=UnityEngine.Object.Instantiate(match.trucks[0].gameObject);preview.AddSingleGO(truckObject);truckObject.SetActive(true);var truck=truckObject.GetComponent<SportsEventTruck>();foreach(var body in truckObject.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}frame.rigging.truck=truck;
                var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");if(!catalog.TryGet("dragon-inferno",out var species))throw new InvalidOperationException("Actual source species required.");var rest=actor.motor.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();var trials=MountedAnimationTrials(actor,species);trials.AddRange(CompleteActualLayerBreathTrials(actor,species));InstallProvenConfiguredStartupTrial(actor,trials,startup,new MountedSizeProfile{profile="READ_ONLY_CURRENT_ENVELOPE_SOURCE_FIDELITY"});var footprint=OriginalFootprints(new[]{match.trucks[0]}).Single();
                foreach(var trial in trials.Where(value=>value.name=="Actual graph blend Turn Left -> Dive weight=0.25")){foreach(var joint in rest)joint.Restore();SampleMountedTrial(actor,trial);frame.CaptureEvaluatedAnimationPose();var pose=frame.ReadPose();truck.transform.SetPositionAndRotation(pose.position+pose.rotation*Vector3.up*footprint.support,pose.rotation);InspectInfernoLimitingTriangles(actor,frame,truck,trial,report);}
                DeriveInfernoStrictTorsoCorridor(actor,species,frame,rest,report);
                copy.transform.localScale*=report.factor;frame.supportPresentation.localScale/=report.factor;bool full=false,skinFound=false,nwhFound=false,harnessFound=false;var model=actor.motor.transform;
                foreach(var trial in trials)
                {
                    foreach(var joint in rest)joint.Restore();SampleMountedTrial(actor,trial);frame.CaptureEvaluatedAnimationPose();var pose=frame.ReadPose();truck.transform.SetPositionAndRotation(pose.position+pose.rotation*Vector3.up*footprint.support,pose.rotation);frame.rigging.RefreshAuthoredPresentation(true);report.poses++;
                    Vector3 Relative(Vector3 point)=>Quaternion.Inverse(model.rotation)*(point-model.position);
                    foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(value=>value.sharedMesh!=null)){skin.BakeMesh(mesh,true);foreach(var point in mesh.vertices){var p=Relative(skin.transform.TransformPoint(point));FitInclude(ref report.skinEnvelope,ref skinFound,p);FitInclude(ref report.fullEnvelope,ref full,p);}}
                    foreach(var point in FitCorners(ActualRenderedGeometryBounds(truckObject))){var p=Relative(point);FitInclude(ref report.nwhEnvelope,ref nwhFound,p);FitInclude(ref report.fullEnvelope,ref full,p);}
                    foreach(var point in FitCorners(ActualRenderedGeometryBounds(frame.supportPresentation.gameObject))){var p=Relative(point);FitInclude(ref report.harnessEnvelope,ref harnessFound,p);FitInclude(ref report.fullEnvelope,ref full,p);}
                    foreach(var line in copy.GetComponentsInChildren<LineRenderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy)){float radius=Mathf.Max(line.startWidth,line.endWidth)*.5f*Mathf.Max(line.transform.lossyScale.x,Mathf.Max(line.transform.lossyScale.y,line.transform.lossyScale.z));for(int i=0;i<line.positionCount;i++){var p=Relative(line.useWorldSpace?line.GetPosition(i):line.transform.TransformPoint(line.GetPosition(i)));foreach(var corner in FitCorners(new Bounds(p,Vector3.one*radius*2))){FitInclude(ref report.harnessEnvelope,ref harnessFound,corner);FitInclude(ref report.fullEnvelope,ref full,corner);}}}
                }
                foreach(var gate in match.gates.Select((value,index)=>(value,index))){float radius=FitCorners(report.fullEnvelope).Max(point=>new Vector2(point.x,point.y).magnitude);report.gates.Add(new InfernoFitGate{gate=gate.index,radius=gate.value.clearRadius,maximumConservativeProjectedRadius=radius,fullEnvelopeFits=radius<=gate.value.clearRadius});}
                foreach(var joint in rest)joint.Restore();SampleMountedTrial(actor,trials.Single(value=>value.actualConfiguredStartup));frame.CaptureEvaluatedAnimationPose();
                var solids=copy.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy).ToArray();var participantRoots=match.trucks.Select(value=>value.transform).Concat(match.racers.SelectMany(value=>value.choices).Select(value=>value.transform)).ToArray();var environment=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Collider>(true)).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy&&!participantRoots.Any(root=>value.transform.IsChildOf(root))).ToArray();
                (Vector3,Quaternion) At(Collider shape,int start){var target=match.world.riderStarts[start].position;var rotation=Quaternion.LookRotation((match.gates[0].center-target).normalized,Vector3.up)*Quaternion.Inverse(model.rotation);return(target+rotation*(shape.transform.position-model.position),rotation*shape.transform.rotation);}
                for(int i=0;i<4;i++)foreach(var own in solids){var a=At(own,i);foreach(var other in environment)if(Physics.ComputePenetration(own,a.Item1,a.Item2,other,other.transform.position,other.transform.rotation,out var direction,out float depth))report.startupContacts.Add(new InfernoFitContact{start=i,own=AnimationUtility.CalculateTransformPath(own.transform,copy.transform),other=other.name,depth=depth,direction=direction});for(int j=i+1;j<4;j++)foreach(var peer in solids){var b=At(peer,j);if(Physics.ComputePenetration(own,a.Item1,a.Item2,peer,b.Item1,b.Item2,out var direction,out float depth))report.startupContacts.Add(new InfernoFitContact{start=i,otherStart=j,own=own.name,other=peer.name,depth=depth,direction=direction});}}
                report.conservativeEnvelopeAndStartupClear=report.startupContacts.Count==0&&report.gates.All(value=>value.fullEnvelopeFits);
            }
            catch(Exception error){report.failure=error.ToString();}
            finally
            {
                if(frame!=null)frame.ReleaseGeometrySnapshot();UnityEngine.Object.DestroyImmediate(mesh);preview.Cleanup();if(match!=null)report.nwhExact=PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sceneBytesExact=Hash(DragonRidersAuthoring.ScenePath)==sceneHash;report.sourceBytesExact=watched.Count==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","InfernoFit");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"all699-envelope-and-original-anatomical-fit.json"),JsonUtility.ToJson(report,true));}
            }
            if(!string.IsNullOrEmpty(report.failure)||!report.sceneBytesExact||!report.sourceBytesExact||!report.nwhExact)throw new InvalidOperationException("Read-only actual Inferno fit diagnosis failed or changed source: "+report.failure);return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
