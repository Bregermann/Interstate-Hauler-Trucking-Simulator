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
        const string CourseRingMesh="Assets/TruckSkyTow/Art/Arctic standard gate body.asset";
        const string CourseRingMaterial="Assets/TruckDragonRiders/Materials/Royal skyfire gate gold.mat";
        [Serializable]sealed class PhysicalCourseReceipt
        {
            public string scope="OWNED_SAVED_COURSE_CANDIDATE_REAL_RENDER_COLLIDER_SCORING_GEOMETRY_NOT_MOUNT_START_WORLD_CLEARANCE_FLIGHT_RACE_WINDOWS_OR_HUMAN_ACCEPTANCE",sourceScene,candidateScene,envelopeEvidence,candidateSceneSha256,failure;
            public float clearRadius,sourceInnerRadius,sourceOuterRadius,sourceDepth,uniformScale,physicalOuterRadius,physicalDepth,measuredRearReach,measuredAllAttitudeRadius,straightExitDistance,truckRadius;
            public bool sourceBytesExact,savedReloaded,orderedRouteExact,sourceArtExact,physicalControlsPassed,rulesControlsPassed;
            public FlightSourceHash[] protectedSources;public List<PhysicalCourseGate> gates=new List<PhysicalCourseGate>();
        }
        [Serializable]sealed class PhysicalCourseGate
        {
            public int index,solidRimHits,clearInnerRays,clearOuterRays;public Vector3 center;public Quaternion rotation;
            public float actualInnerRadius,actualOuterRadius,actualDepth,maximumMatrixDifference;public bool originalMeshAndMaterial,renderColliderMatrixExact,sourcePosePreserved,centerClearBothDirections;
        }
        public static string CreateMeasuredPhysicalGateCourseTestCandidate(string envelopeEvidence,string outputFolder)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for explicit candidate authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty||SceneManager.GetSceneAt(i).path==DragonRidersAuthoring.ScenePath)throw new InvalidOperationException("Preserve open scenes; close the original Riders scene before explicit isolated candidate authoring.");
            const string prefix="Assets/TruckDragonRiders/Tests/Generated/";outputFolder=outputFolder.Replace('\\','/').TrimEnd('/');if(!outputFolder.StartsWith(prefix,StringComparison.Ordinal)||outputFolder.Substring(prefix.Length).Contains('/')||outputFolder.Contains("..")||Directory.Exists(outputFolder)||File.Exists(outputFolder+".meta"))throw new InvalidOperationException("Unique absent direct Generated TEST child required; never overwrite a scene or prior candidate.");
            var manifest=RequireRiderPassedManifest(envelopeEvidence,"DragonRiderMotorRootRepresentationTests.UnitAncestorsAndInactiveConfiguredMotorRootRetainFull699GeometryAndMeasureActualTruckCenteredCourseEnvelope");RequireRecordedStartupSourceHash(manifest,DragonRidersAuthoring.ScenePath,"Original Rider scene for physical course candidate");
            var sourceProof=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(envelopeEvidence,"rendered-evidence"),"precise-motor-root-inferno-course-envelope.json",SearchOption.AllDirectories).Single()));var assembly=sourceProof.profiles.Single();var envelope=assembly.actualCourseEnvelope;
            if(!sourceProof.sourceBytesExact||!sourceProof.nwhExact||!sourceProof.sceneBytesExact||assembly.actualPoses!=699||assembly.invalidAnatomicalPoses!=0||assembly.candidateSatPairs!=0||!assembly.exactOriginalBoxAcceptance||!assembly.originalFootprintFits||envelope==null||envelope.poses!=699||!envelope.hasBounds)throw new InvalidOperationException("Actual full699 native assembly and truck-centered envelope required; no guessed body bounds.");
            if(envelope.crossingCenterAuthority!="CURRENT_AUTHORED_BOX_CENTER_TRANSFORMED_BY_CURRENT_LOCAL_TO_WORLD_NOT_PHYSX_CACHE"||envelope.crossingCenters==null||envelope.crossingCenters.Count!=699||envelope.crossingCenters.Any(center=>string.IsNullOrEmpty(center.pose)||Vector3.Distance(center.currentLocalToWorld.MultiplyPoint3x4(center.authoredLocalCenter),center.currentMatrixCenter)>.0001f))throw new InvalidOperationException("Fresh complete699 transformed authored box-center witnesses required; stale PhysX preview bounds cannot qualify course size.");
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(CourseRingMesh);var material=AssetDatabase.LoadAssetAtPath<Material>(CourseRingMaterial);if(mesh==null||material==null)throw new InvalidOperationException("Existing owned SkyTow gate mesh and Royal skyfire material required.");if(mesh.bounds.center.sqrMagnitude>1e-10f)throw new InvalidOperationException("The exact original centered ring mesh is required; never invent an aperture/score plane for offset replacement geometry.");
            var measured=new ActualMountedCourseEnvelope();CompleteActualCourseEnvelopeDimensions(measured);
            var report=new PhysicalCourseReceipt{sourceScene=DragonRidersAuthoring.ScenePath,candidateScene=outputFolder+"/Measured large physical Rider course TEST ONLY.unity",envelopeEvidence=envelopeEvidence,clearRadius=Mathf.Ceil((envelope.maximumAnyAttitudeVertexRadius+5)/5)*5,sourceInnerRadius=measured.sourceRingInnerAperture,sourceOuterRadius=measured.sourceRingOuterRadius,sourceDepth=measured.sourceRingDepth,measuredRearReach=envelope.maximumRearReach,measuredAllAttitudeRadius=envelope.maximumAnyAttitudeVertexRadius};
            report.uniformScale=report.clearRadius/report.sourceInnerRadius;report.physicalOuterRadius=report.sourceOuterRadius*report.uniformScale;report.physicalDepth=report.sourceDepth*report.uniformScale;report.straightExitDistance=Mathf.Ceil((report.measuredRearReach+report.physicalDepth*.5f+5)/5)*5;
            if(report.clearRadius<report.measuredAllAttitudeRadius+5||!float.IsFinite(report.straightExitDistance)||report.straightExitDistance<30||report.straightExitDistance>150)throw new InvalidOperationException("The fresh measured opening and finite exit must actually fit the qualified assembly envelope.");
            string[] protectedPaths={DragonRidersAuthoring.ScenePath,CourseRingMesh,CourseRingMaterial,"Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab","Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset"};report.protectedSources=protectedPaths.SelectMany(path=>new[]{path,path+".meta"}).Select(path=>{if(!File.Exists(path))throw new InvalidOperationException("Protected source missing: "+path);return new FlightSourceHash{path=path,sha256=Hash(path)};}).ToArray();
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonRiderGateDefinition[] original=null;Exception originalFailure=null;
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);var match=CourseMatch(scene);original=match.gates.Select(CopyCourseGate).ToArray();var physical=CoursePhysicalGates(scene);if(original.Length!=12||physical.Length!=12)throw new InvalidOperationException("Preserve the exact existing ordered12-ring loop.");
                for(int i=0;i<original.Length;i++)
                {
                    var gate=physical.Single(shape=>shape.name=="Aerial gate "+(i+1));var filter=gate.GetComponent<MeshFilter>();var renderer=gate.GetComponent<MeshRenderer>();
                    if(gate.sharedMesh!=mesh||filter==null||filter.sharedMesh!=mesh||renderer==null||renderer.sharedMaterial!=material||gate.convex||gate.isTrigger||!gate.enabled||gate.transform.parent==null||Vector3.Distance(gate.transform.parent.lossyScale,Vector3.one)>.000001f)throw new InvalidOperationException("Preserve unknown designer ring configurations; exact physical source art/unit-parent ring required.");
                    // Preserve every authored center/rotation/order. Only this
                    // scene copy's real ring size and matching aperture change.
                    gate.transform.localScale=Vector3.one*report.uniformScale;match.gates[i]=new DragonRiderGateDefinition{center=original[i].center,rotation=original[i].rotation,clearRadius=report.clearRadius};
                }
                match.aiStraightGateExitDistance=report.straightExitDistance;EditorUtility.SetDirty(match);Directory.CreateDirectory(outputFolder);AssetDatabase.Refresh();if(!EditorSceneManager.SaveScene(scene,report.candidateScene,true))throw new InvalidOperationException("Explicit separate TEST candidate scene did not save.");EditorSceneManager.CloseScene(scene,true);scene=default;
                scene=EditorSceneManager.OpenScene(report.candidateScene,OpenSceneMode.Additive);match=CourseMatch(scene);physical=CoursePhysicalGates(scene);Physics.SyncTransforms();report.savedReloaded=true;report.orderedRouteExact=match.gates.Length==original.Length&&match.gates.Select((gate,i)=>gate.center.Equals(original[i].center)&&gate.rotation.Equals(original[i].rotation)&&gate.clearRadius==report.clearRadius).All(value=>value);if(!report.orderedRouteExact||match.aiStraightGateExitDistance!=report.straightExitDistance)throw new InvalidOperationException("Saved candidate changed ordered source poses or failed to retain measured exit distance.");
                for(int i=0;i<original.Length;i++)report.gates.Add(InspectPhysicalCourseGate(physical.Single(shape=>shape.name=="Aerial gate "+(i+1)),match.gates[i],original[i],mesh,material,report,i));
                report.sourceArtExact=report.gates.All(gate=>gate.originalMeshAndMaterial);report.physicalControlsPassed=report.gates.All(gate=>gate.renderColliderMatrixExact&&gate.sourcePosePreserved&&gate.maximumMatrixDifference<=.001f&&Mathf.Abs(gate.actualInnerRadius-report.clearRadius)<=.001f&&gate.centerClearBothDirections&&gate.solidRimHits==16&&gate.clearInnerRays==8&&gate.clearOuterRays==8);
                report.truckRadius=CourseOriginalShapeRadius(match);report.rulesControlsPassed=InspectPhysicalCourseRules(match.gates,report.truckRadius);if(!report.sourceArtExact||!report.physicalControlsPassed||!report.rulesControlsPassed)throw new InvalidOperationException("Saved real mesh/collider/aperture or ordered scoring controls failed; inspect individual native witnesses.");
                report.candidateSceneSha256=Hash(report.candidateScene);
            }
            catch(Exception error){originalFailure=error;report.failure=error.ToString();}
            finally
            {
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sourceBytesExact=report.protectedSources.All(source=>File.Exists(source.path)&&Hash(source.path)==source.sha256);WritePhysicalCourseReceipt(report);
            }
            if(originalFailure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(originalFailure).Throw();if(!report.sourceBytesExact)throw new InvalidOperationException("Candidate authoring changed a protected original scene, shared world or art source.");return JsonUtility.ToJson(report,true);
        }
        static DragonRidersMatch CourseMatch(Scene scene)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();
        static MeshCollider[] CoursePhysicalGates(Scene scene)=>scene.GetRootGameObjects().Single(root=>root.name=="Ordered kingdom aerial gate loop").GetComponentsInChildren<MeshCollider>(true);
        static DragonRiderGateDefinition CopyCourseGate(DragonRiderGateDefinition gate)=>new DragonRiderGateDefinition{center=gate.center,rotation=gate.rotation,clearRadius=gate.clearRadius};
        static float CourseOriginalShapeRadius(DragonRidersMatch match)
        {
            if(match.trucks[0].shape is not BoxCollider box)throw new InvalidOperationException("Exact original NWH box shape required for scoring controls.");var matrix=box.transform.localToWorldMatrix;var half=box.size*.5f;var bounds=new Bounds(matrix.MultiplyPoint3x4(box.center-half),Vector3.zero);
            // Collider.bounds is empty for a legitimately inactive setup truck.
            // Measure its actual finite authored box without activating a racer.
            for(int i=0;i<8;i++)bounds.Encapsulate(matrix.MultiplyPoint3x4(box.center+new Vector3((i&1)==0?-half.x:half.x,(i&2)==0?-half.y:half.y,(i&4)==0?-half.z:half.z)));return Mathf.Max(bounds.extents.x,bounds.extents.y);
        }
        static PhysicalCourseGate InspectPhysicalCourseGate(MeshCollider collider,DragonRiderGateDefinition score,DragonRiderGateDefinition original,Mesh mesh,Material material,PhysicalCourseReceipt report,int index)
        {
            var row=new PhysicalCourseGate{index=index,center=score.center,rotation=score.rotation};var transform=collider.transform;var filter=collider.GetComponent<MeshFilter>();var renderer=collider.GetComponent<MeshRenderer>();row.originalMeshAndMaterial=filter!=null&&filter.sharedMesh==mesh&&collider.sharedMesh==mesh&&renderer!=null&&renderer.sharedMaterial==material&&!collider.convex&&!collider.isTrigger&&collider.enabled;row.renderColliderMatrixExact=filter!=null&&renderer!=null&&filter.transform.localToWorldMatrix.Equals(transform.localToWorldMatrix)&&renderer.transform.localToWorldMatrix.Equals(transform.localToWorldMatrix);
            var expected=Matrix4x4.TRS(score.center,score.rotation,Vector3.one*report.uniformScale);var actual=transform.localToWorldMatrix;for(int r=0;r<4;r++)for(int c=0;c<4;c++)row.maximumMatrixDifference=Mathf.Max(row.maximumMatrixDifference,Mathf.Abs(expected[r,c]-actual[r,c]));row.sourcePosePreserved=score.center.Equals(original.center)&&score.rotation.Equals(original.rotation);float scale=((Vector3)actual.GetColumn(0)).magnitude;row.actualInnerRadius=report.sourceInnerRadius*scale;row.actualOuterRadius=report.sourceOuterRadius*scale;row.actualDepth=report.sourceDepth*((Vector3)actual.GetColumn(2)).magnitude;
            Vector3 normal=score.rotation*Vector3.forward,right=score.rotation*Vector3.right,up=score.rotation*Vector3.up;float reach=report.physicalDepth+10;bool Hit(Vector3 point,bool reverse=false)=>collider.Raycast(new Ray(point+normal*(reverse?reach:-reach),normal*(reverse?-1:1)),out _,reach*2);
            row.centerClearBothDirections=!Hit(score.center)&&!Hit(score.center,true);float rim=(row.actualInnerRadius+row.actualOuterRadius)*.5f;
            for(int angle=0;angle<8;angle++){float radians=angle*Mathf.PI/4;var direction=right*Mathf.Cos(radians)+up*Mathf.Sin(radians);if(Hit(score.center+direction*rim))row.solidRimHits++;if(Hit(score.center+direction*rim,true))row.solidRimHits++;if(!Hit(score.center+direction*(report.clearRadius-.25f)))row.clearInnerRays++;if(!Hit(score.center+direction*(row.actualOuterRadius+.25f)))row.clearOuterRays++;}
            return row;
        }
        static bool InspectPhysicalCourseRules(DragonRiderGateDefinition[] gates,float radius)
        {
            if(!float.IsFinite(radius)||radius<=0||radius>=10)return false;var race=new DragonRiderRace();race.Begin(1,1,gates);
            bool Pass(int gate,bool mounted=true,float offset=0,bool reverse=false){var source=gates[gate];var n=source.rotation*Vector3.forward;var c=source.center+source.rotation*Vector3.right*offset;return race.ObservePassage(0,c+n*(reverse?10:-10),c+n*(reverse?-10:10),radius,mounted);}
            if(Pass(1)||Pass(0,false)||Pass(0,true,0,true)||Pass(0,true,gates[0].clearRadius-radius*.5f)||race.Passed[0]!=0)return false;
            if(!Pass(0)||race.Passed[0]!=1||Pass(0)||race.Passed[0]!=1)return false;race.Begin(1,1,gates);for(int i=0;i<gates.Length;i++){race.Tick(1);if(!Pass(i)||race.Passed[0]!=i+1)return false;}return race.Finished(0)&&race.FinishTime[0]==gates.Length;
        }
        static void WritePhysicalCourseReceipt(PhysicalCourseReceipt report)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","PhysicalCourse");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"saved-physical-large-ring-course-candidate.json"),JsonUtility.ToJson(report,true));
        }
    }
}
#endif
