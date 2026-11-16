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
        [Serializable] sealed class MountedSizeReport
        {
            public string scope="READ_ONLY_ORIGINAL_MAPPED_MOTION_VERSUS_EXISTING_SAVED_MOUNTED_CONTROL_EXACT_FORBIDDEN_UNIFORM_SIZE_INTERVALS_NOT_SAVED_OR_RUNTIME_ACCEPTANCE";
            public bool intervalControlsPassed,sourceBytesExact,nwhExact,sceneBytesExact;public ColdFixtureReceipt testOnlyColdFixture;
            public List<MountedSizeProfile> profiles=new List<MountedSizeProfile>();
            public List<RiderNativeSourceBaseline> inputActualSourceBaselines=new List<RiderNativeSourceBaseline>();
        }
        [Serializable] sealed class MountedSizeProfile
        {
            public string species,profile,controller,status,failure;public string supportedPostEvidence;public float supportedPostExtraWorldMeters,supportedPostOriginalClearance,supportedPostCandidateClearance;
            public Vector3 originalActorScale,originalMotorScale;
            public float planningMargin=.02f,derivedLowerBound,roundedCandidateFactor,actualCandidateSkinScaleError,maximumActualWheelGap;
            public bool sourceMotionUnchanged,hasFiniteUniformSize,exactOriginalBoxAcceptance,originalFootprintFits,precisePlanning;
            public float maximumPlanningAnchorDistance;
            public bool strictMaterialPatch;public string planningBoundMeaning;
            public float[] strictTorsoWeights;public SurfaceProof[] strictPatchProof;
            public int conservativeExpandedPlanningPairs,unsafeExpandedPlanningMisses;
            public List<MountedSizeIntervalWitness> unitPlanningDifferences=new List<MountedSizeIntervalWitness>();
            public int actualPoses,invalidAnatomicalPoses,triangles,boxes,originalSatPairs,intervalUnitControls,forbiddenIntervals,candidateSatPairs,candidateWheelContactPoses;
            public Bounds originalAllPoseBounds,candidateAllPoseBounds;
            public DragonRiderSurfaceAnchor[] originalMaterialAnchors;
            public List<string> invalidPoses=new List<string>();
            public List<MountedSizeIntervalWitness> limitingIntervals=new List<MountedSizeIntervalWitness>();
            public List<FlightSourceHash> sourceFiles=new List<FlightSourceHash>();
            public bool provenFireHeadLeverRecipe;public float derivedNeckPitch,derivedHeadPitch;public string headLeverEvidence;
            public bool motorRootConfiguredInactive,nativeMotorParentRetained,nativeUnitAncestors,nativeRuntimeAwakeObserved,nativeSpeedNormalizationRequired,nativeSpeedFieldsChanged;public string nativeScaleRepresentation;public Vector3 nativeMotorRootOriginalScale,nativeMotorRootCandidateScale,nativeActorCandidateScale,nativeDeckWorldDimensionsBefore,nativeDeckWorldDimensionsAfter;public float nativeScaleFactorBefore,nativeScaleFactorAfter,catalogFlightSpeed,nativeFlightPositionModifier,nativeUntunedFlightTarget;public ActualMountedCourseEnvelope actualCourseEnvelope;
            public bool existingAllActionSafeMounted,ownedDefaultValidated,originalDefaultControlPassed;public string actualOwnedDefaultClip,actualStartupOriginalKeyGuid;public long actualStartupOriginalKeyFileId;public float maximumOwnedDefaultUnselectedPositionError,maximumOwnedDefaultUnselectedRotationError,maximumOwnedDefaultScaleError,maximumOwnedDefaultSelectedRotationError;
            public bool nativeDerivedInfernoMaterialPatch;public string nativeMaterialEvidence;public float nativeMaterialRearward;
            public bool actualConfiguredStartupTrial;public string startupEvidence,actualStartupSourceGuid;public long actualStartupSourceFileId;public float startupBonePositionError,startupBoneRotationError;
            public string inactiveImportedStagingScope,inactiveImportedStagingFailure;public int inactiveImportedStagingSatPairs;
        }
        [Serializable] sealed class MountedSizeIntervalWitness
        {
            public string pose,skin,box;
            public int triangle;
            public int a,b,c;
            public string originalVertexAWeights,originalVertexBWeights,originalVertexCWeights;
            public bool actualCenterAnchorTriangle;
            public bool nativeExpandedUnitHit,planningExpandedUnitHit;
            public float lower,upper;
            public bool unbounded;
            public Vector3 actualMaterialAnchor,preciseAuthoredMaterialAnchor,authoredMaterialBarycentric,worldA,worldB,worldC,boxHalfExtents,actualBoxCenter;
            public Matrix4x4 actualBoxMatrix,actualBoxWorldToLocal;
        }
        struct UniformSizeInterval { public double lower,upper;public MountedSizeIntervalWitness witness; }
        sealed class SizePoseSnapshot { public MountedPoseTrial trial;public Vector3[][] skins;public Vector3 anchor; }

        // The original finite truck is fixed in the material-anchor frame.
        // Uniformly sizing the dragon scales each triangle projection to
        // [s*L,s*H], while the original box stays at[C-R,C+R]. Each of the
        // existing13SAT axes therefore gives two linear inequalities in s.
        // Intersecting them yields the EXACT forbidden scale interval for that
        // triangle, including contacts that first appear at a LARGER size.
        // No native size grid or selected-vertex collision substitute is used.
        static bool ForbiddenUniformSize(Vector3 a,Vector3 b,Vector3 c,Vector3 anchor,Vector3 half,out double lower,out double upper)
        {
            var edge0=b-a;var edge1=c-b;var edge2=a-c;a-=anchor;b-=anchor;c-=anchor;double low=1,high=double.PositiveInfinity;
            bool Axis(Vector3 axis)
            {
                if(axis.sqrMagnitude<1e-14f)return true;axis.Normalize();
                double l=Math.Min(Vector3.Dot(a,axis),Math.Min(Vector3.Dot(b,axis),Vector3.Dot(c,axis)));
                double h=Math.Max(Vector3.Dot(a,axis),Math.Max(Vector3.Dot(b,axis),Vector3.Dot(c,axis)));
                double center=-Vector3.Dot(anchor,axis),radius=half.x*Math.Abs(axis.x)+half.y*Math.Abs(axis.y)+half.z*Math.Abs(axis.z);
                if(l>1e-10)high=Math.Min(high,(center+radius)/l);else if(l< -1e-10)low=Math.Max(low,(center+radius)/l);else if(center+radius<0)return false;
                if(h>1e-10)low=Math.Max(low,(center-radius)/h);else if(h< -1e-10)high=Math.Min(high,(center-radius)/h);else if(center-radius>0)return false;
                return high>=low;
            }
            bool Edge(Vector3 edge)=>Axis(Vector3.Cross(edge,Vector3.right))&&Axis(Vector3.Cross(edge,Vector3.up))&&Axis(Vector3.Cross(edge,Vector3.forward));
            bool hit=Axis(Vector3.right)&&Axis(Vector3.up)&&Axis(Vector3.forward)&&Axis(Vector3.Cross(edge0,edge1))&&Edge(edge0)&&Edge(edge1)&&Edge(edge2);
            lower=low;upper=high;return hit&&high>=low&&high>=1;
        }
        static bool CheckUniformSizeIntervalControls()
        {
            var a=new Vector3(-.5f,0,-.5f);var b=new Vector3(.5f,0,-.5f);var c=new Vector3(0,0,.5f);var anchor=new Vector3(0,-3,0);
            if(!ForbiddenUniformSize(a,b,c,anchor,Vector3.one,out double low,out double high)||Math.Abs(low-1)>1e-6||Math.Abs(high-4d/3)>1e-6)
                throw new InvalidOperationException("Exact known initial-contact uniform-size interval failed.");
            var below=Vector3.down*2;
            if(!ForbiddenUniformSize(a+below,b+below,c+below,anchor,Vector3.one,out low,out high)||Math.Abs(low-2)>1e-6||Math.Abs(high-4)>1e-6)
                throw new InvalidOperationException("A currently separated triangle must retain its genuine later forbidden interval.");
            if(!ForbiddenUniformSize(a,b,c,Vector3.zero,Vector3.one,out low,out high)||!double.IsPositiveInfinity(high))
                throw new InvalidOperationException("An anchor-containing crossing cannot be cured by arbitrary size inflation.");
            foreach(var shift in new[]{Vector3.zero,below})foreach(double factor in new[]{1d,1.1d,1.34d,2.5d,4.1d,8d})
            {
                bool interval=ForbiddenUniformSize(a+shift,b+shift,c+shift,anchor,Vector3.one,out low,out high)&&factor>=low&&factor<=high;
                Vector3 A=anchor+(a+shift-anchor)*(float)factor,B=anchor+(b+shift-anchor)*(float)factor,C=anchor+(c+shift-anchor)*(float)factor;
                bool actual=ActualTriangleBoxSat(A,B,C,Vector3.one,out _);
                if(interval!=actual)throw new InvalidOperationException("Analytic uniform-size interval disagrees with independent finite triangle/box SAT.");
            }
            return true;
        }
        static float SmallestUniformSize(List<UniformSizeInterval> intervals,MountedSizeProfile report)
        {
            intervals.Sort((a,b)=>a.lower.CompareTo(b.lower));double candidate=1;
            foreach(var interval in intervals)
            {
                if(interval.lower>candidate+1e-7)continue;
                if(interval.upper<candidate)continue;
                if(report.limitingIntervals.Count<64)report.limitingIntervals.Add(interval.witness);
                if(double.IsPositiveInfinity(interval.upper)){report.hasFiniteUniformSize=false;return 0;}
                candidate=Math.Max(candidate,interval.upper+.0001);
            }
            report.derivedLowerBound=(float)candidate;
            // Report a deliberate hundredth-size authoring value. Rounding
            // must itself be checked against all LATER forbidden intervals.
            double rounded=Math.Ceiling(candidate*100)/100;
            bool moved;
            do
            {
                moved=false;
                foreach(var interval in intervals)
                    if(interval.lower<=rounded+1e-7&&interval.upper>=rounded)
                    {
                        if(report.limitingIntervals.Count<64)report.limitingIntervals.Add(interval.witness);
                        if(double.IsPositiveInfinity(interval.upper)){report.hasFiniteUniformSize=false;return 0;}
                        rounded=Math.Ceiling((interval.upper+.0001)*100)/100;moved=true;
                    }
            }while(moved);
            report.hasFiniteUniformSize=true;return (float)rounded;
        }
        public static string InspectOriginalMotionMountedSizeWithoutSavingAssets()=>InspectMountedSizeProfiles(false);
        public static string InspectOriginalCenteredConnectedMountedSizeWithoutSavingAssets()=>InspectMountedSizeProfiles(true);
        public static string InspectPreciseOriginalMotionMountedSizeWithoutSavingAssets()=>InspectMountedSizeProfiles(false,true);
        public static string InspectPreciseOriginalCenteredConnectedMountedSizeWithoutSavingAssets()=>InspectMountedSizeProfiles(true,true);
        public static string InspectPreciseFireNormalFamilyMountedSizeWithoutSavingAssets()=>InspectMountedSizeProfiles(true,true,true);
        static string InspectMountedSizeProfiles(bool connectedOriginalCentered,bool precisePlanning=false,bool fireNormalFamily=false,ProvenRiderStartup startup=null,ProvenFireHeadLever headLever=null,ProvenInfernoMaterial material=null,bool allActionMounted=false,bool motorRootRepresentation=false)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes first.");
            string scenePath=DragonRidersAuthoring.ScenePath,before=Hash(scenePath);var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            Exception firstMeasurementFailure=null;var report=new MountedSizeReport{intervalControlsPassed=CheckUniformSizeIntervalControls()};if(connectedOriginalCentered)report.scope="READ_ONLY_V279_CONNECTED_NORMAL_FLIGHT_ORIGINAL_CENTERED_MATERIAL_SUPPORT_FULL_ACTION_AND_LAYER_SIZE_INTERVALS_NO_ADDITIONAL_CURVE_CASCADE_NO_SAVED_ASSETS_NOT_RUNTIME_ACCEPTANCE";if(fireNormalFamily)report.scope="READ_ONLY_V279_FIRE_HALF_FLAP_NORMAL_FAMILY_ORIGINAL_AND_EXISTING_MEASURED_STRICT_TORSO_SUPPORT_ALL537_ACTION_LAYER_POSES_NOT_RUNTIME_ACCEPTANCE";if(precisePlanning){InspectPreciseMountedSizePlanningControls();report.scope="PRECISE_CONSERVATIVE_AUTHORED_MATERIAL_PLANNING_NOT_EXACT_NATIVE_FLOAT_MINIMUM_"+report.scope;}if(headLever!=null)report.scope+="_PROVEN_FIRE_HEAD_LEVER60_HEAD_MINUS15";if(startup!=null)report.scope+="_WITH_PROVEN_PUBLIC_CONFIGURED_STARTUP_ZERO_AND_SEPARATE_INACTIVE_IMPORTED_FRAME";if(material!=null)report.scope+="_NATIVE_DERIVED_STRICT_INFERNO_MATERIAL_PATCH";if(allActionMounted)report.scope+="_EXISTING_ALL_ACTION_SAFE_MOUNTED";if(motorRootRepresentation)report.scope+="_UNIT_ANCESTORS_MOTOR_ROOT_REPRESENTATION_WITH_REAL_TRUCK_CENTERED_ENVELOPE";if(coldFixtureExport!=null)report.scope+="_EXPLICIT_TEST_ONLY_COLD_FIXTURE_EXPORT_NOT_PRODUCTION";var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonRidersMatch match=null;string physics=null;
            try
            {
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);if(coldFixtureExport!=null){coldFixtureExport.Begin(match);report.testOnlyColdFixture=coldFixtureExport.receipt;}
                var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");
                foreach(string id in fireNormalFamily?new[]{"dragon-fire"}:connectedOriginalCentered?new[]{"dragon-inferno"}:new[]{"dragon-inferno","dragon-fire"})foreach(bool originalMapped in connectedOriginalCentered?new[]{false}:new[]{true,false})foreach(bool strictPatch in fireNormalFamily?(motorRootRepresentation?new[]{true}:new[]{false,true}):new[]{false})foreach(bool useConnected in startup!=null?(allActionMounted?new[]{false}:new[]{false,true}):new[]{connectedOriginalCentered})
                {
                    var row=new MountedSizeProfile{species=id,profile=headLever!=null?"PROVEN_FIRE_HEAD_LEVER60_HEAD_MINUS15_HALF_FLAP_"+(strictPatch?"MEASURED_STRICT_TORSO":"ORIGINAL_CENTERED_SUPPORT"):startup!=null?(useConnected?"V279_CONNECTED_ORIGINAL_CENTERED_WITH_ACTUAL_PUBLIC_DEFAULT_ZERO":"SAVED_MOUNTED_WITH_ACTUAL_PUBLIC_DEFAULT_ZERO"):fireNormalFamily?"V279_FIRE_NORMAL_FAMILY_HALF_FLAP_"+(strictPatch?"MEASURED_STRICT_TORSO_REAR_MINUS_POINT15_RADIUS_POINT065":"ORIGINAL_CENTERED_SUPPORT"):connectedOriginalCentered?"V279_CONNECTED_NORMAL_FLIGHT_ORIGINAL_CENTERED_MATERIAL_BINDINGS_FULL_ACTION_GATES":originalMapped?"ORIGINAL_NATIVE_GRAPH_KEYS_NO_NEW_CURVE_DERIVATIVES":"EXISTING_SAVED_MOUNTED_CONTROLLER_NO_NEW_CURVE_DERIVATIVES",status="IN_PROGRESS",precisePlanning=precisePlanning,strictMaterialPatch=strictPatch,planningBoundMeaning=precisePlanning?"BOUND_OUTSIDE_CONSERVATIVE_EXPANDED_PLANNING_UNION_NOT_EXACT_NATIVE_FLOAT_MINIMUM":"LEGACY_FLOAT_PLANNING_BOUND_REQUIRES_INDEPENDENT_NATIVE_ACCEPTANCE"};if(material!=null){row.profile="NATIVE_DERIVED_STRICT_TORSO_"+(useConnected?"V279_CONNECTED":"SAVED_MOUNTED")+"_WITH_ACTUAL_PUBLIC_DEFAULT_ZERO";row.strictMaterialPatch=true;row.nativeDerivedInfernoMaterialPatch=true;row.nativeMaterialEvidence=material.folder;row.nativeMaterialRearward=material.rearward;}if(allActionMounted){row.profile="EXISTING_ALL_ACTION_SAFE_MOUNTED_WITH_NATIVE_POINTWISE_PATCH_GENUINE_OWNED_DEFAULT0_AND_ORIGINAL_RAW_START_CONTROL";row.existingAllActionSafeMounted=true;}if(motorRootRepresentation)row.profile=fireNormalFamily?"UNIT_ANCESTORS_FIRE_MOTOR_ROOT11_POINT69_PROVEN_HEAD60_MINUS15_STRICT_TORSO_REAL_TRUCK_CENTERED_FULL537_ENVELOPE":"UNIT_ANCESTORS_MOTOR_ROOT17_EXISTING_ALL_ACTION_MOUNTED_AND_REAL_TRUCK_CENTERED_FULL699_ENVELOPE";report.profiles.Add(row);
                    try
                    {
                        if(!catalog.TryGet(id,out var species))throw new InvalidOperationException("Actual source species required.");
                        var saved=match.racers.SelectMany(racer=>racer.choices).First(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId==id);
                        MeasureOriginalMotionMountedSize(saved,match.trucks,species,originalMapped,row,useConnected,startup,headLever,material,allActionMounted,motorRootRepresentation);
                    }
                    catch(Exception error){if(firstMeasurementFailure==null)firstMeasurementFailure=error;row.failure=error.ToString();row.status="REJECTED_MEASUREMENT_INSPECT_ACTUAL_ERROR";}
                    WriteMountedSizeReport(report);
                }
                if(coldFixtureExport!=null){if(firstMeasurementFailure!=null){coldFixtureExport.receipt.failure=firstMeasurementFailure.ToString();System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(firstMeasurementFailure).Throw();}coldFixtureExport.Complete(match);}
            }
            finally
            {
                if(match!=null&&physics!=null)report.nwhExact=PhysicsSignature(match)==physics;
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
                report.sceneBytesExact=Hash(scenePath)==before;var currentModeFiles=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);bool exactModeMembership=coldFixtureExport==null?watched.Count==currentModeFiles.Length:new HashSet<string>(currentModeFiles.Where(path=>!watched.ContainsKey(path)),StringComparer.Ordinal).SetEquals(coldFixtureExport.createdPaths);report.sourceBytesExact=exactModeMembership&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);WriteMountedSizeReport(report);
            }
            if(!report.sourceBytesExact||!report.nwhExact||!report.sceneBytesExact)throw new InvalidOperationException("Read-only mounted size derivation changed original source/NWH/scene.");
            return JsonUtility.ToJson(report,true);
        }
        static void MeasureOriginalMotionMountedSize(DragonCharacter saved,SportsEventTruck[] originals,DragonSpeciesDefinition species,bool originalMapped,MountedSizeProfile row,bool connectedOriginalCentered=false,ProvenRiderStartup startup=null,ProvenFireHeadLever headLever=null,ProvenInfernoMaterial material=null,bool allActionMounted=false,bool motorRootRepresentation=false)
        {
            var preview=new PreviewRenderUtility();DragonRiderSaddle frame=null;AnimatorOverrideController raw=null;var mesh=new Mesh();Dictionary<string,string> sourcePaths=null;UnityEngine.Object[] connected=null;
            try
            {
                var copy=UnityEngine.Object.Instantiate(saved.gameObject);preview.AddSingleGO(copy);copy.SetActive(true);var actor=copy.GetComponent<DragonCharacter>();frame=copy.GetComponent<DragonRiderSaddle>();actor.motor.animator.enabled=false;
                foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                var truckObject=UnityEngine.Object.Instantiate(originals[0].gameObject);preview.AddSingleGO(truckObject);truckObject.SetActive(true);var truck=truckObject.GetComponent<SportsEventTruck>();foreach(var body in truckObject.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}frame.rigging.truck=truck;
                var originalStartupController=actor.motor.animator.runtimeAnimatorController;row.originalActorScale=copy.transform.localScale;row.originalMotorScale=actor.motor.transform.localScale;row.catalogFlightSpeed=species.flightSpeed;row.controller=AssetDatabase.GetAssetPath(actor.motor.animator.runtimeAnimatorController);
                row.originalMaterialAnchors=frame.surfacePatchAnchors.Select(point=>new DragonRiderSurfaceAnchor{a=point.a,b=point.b,c=point.c,barycentric=point.barycentric}).ToArray();
                if(connectedOriginalCentered||allActionMounted){connected=allActionMounted?CreateTransientSafeMountedController(actor,species.id):headLever!=null?CreateProvenFireHeadLeverEnvelope(actor,headLever):CreateTransientCabFlightEnvelope(actor,species.id,species.id=="dragon-fire"?.5f:.25f);if(headLever!=null){row.provenFireHeadLeverRecipe=true;row.derivedNeckPitch=headLever.neck;row.derivedHeadPitch=headLever.head;row.headLeverEvidence=headLever.folder;}actor.motor.animator.runtimeAnimatorController=connected.OfType<AnimatorOverrideController>().Single();}
                if(material!=null)
                {
                    if(species.id!="dragon-inferno")throw new InvalidOperationException("Actual native material fit is scoped to Inferno only.");ApplyNativeProvenInfernoMaterial(frame,material);row.strictPatchProof=material.points;row.strictTorsoWeights=material.points.Select(point=>point.torsoSkinWeight).ToArray();
                }
                else if(row.strictMaterialPatch)
                {
                    if(species.id!="dragon-fire"||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral))throw new InvalidOperationException("The existing measured strict Fire torso patch requires its actual source neutral mapping.");
                    var originalJoints=actor.motor.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();var originalPoses=actor.motor.GetComponentsInChildren<Transform>(true).Select(joint=>new BonePose{bone=joint,position=joint.localPosition,rotation=joint.localRotation}).ToArray();var model=actor.motor.transform;var bone=frame.sourceBone;
                    var measured=MeasureActualDorsalCandidate(actor,species,neutral,EffectiveRiderClip(actor,neutral.clip),originalPoses,Quaternion.Inverse(bone.rotation)*model.rotation,bone.InverseTransformDirection(model.up).normalized,-.15f,.065f,true);string context=species.id+"-"+neutral.action+"-0.25-patch-0.150";var proof=measured.proof.Where(point=>point.context==context).ToArray();VerifyCenteredMaterialPatch(model,bone,proof,-.15f,.065f,true,out _,out _,out _);row.strictPatchProof=proof;row.strictTorsoWeights=proof.Select(point=>point.torsoSkinWeight).ToArray();ApplyFrameCalibration(frame,measured);foreach(var joint in originalJoints)if(joint.joint!=frame.socket)joint.Restore();
                }
                if(originalMapped)
                {
                    var runtime=actor.motor.animator.runtimeAnimatorController;while(runtime is AnimatorOverrideController parent)runtime=parent.runtimeAnimatorController;
                    if(runtime is not AnimatorController controller)throw new InvalidOperationException("Actual original native mapped graph required.");raw=new AnimatorOverrideController(controller){hideFlags=HideFlags.HideAndDontSave};var entries=new List<KeyValuePair<AnimationClip,AnimationClip>>();raw.GetOverrides(entries);foreach(var entry in entries)raw[entry.Key]=entry.Key;actor.motor.animator.runtimeAnimatorController=raw;
                }
                sourcePaths=AssetDatabase.GetDependencies(ActorPath(species.id),true).Concat(AssetDatabase.GetDependencies(row.controller,true)).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToDictionary(path=>path,Hash);row.sourceFiles=sourcePaths.Select(pair=>new FlightSourceHash{path=pair.Key,sha256=pair.Value}).ToList();
                var trialList=MountedAnimationTrials(actor,species);trialList.AddRange(CompleteActualLayerBreathTrials(actor,species));
                var rest=actor.motor.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();
                var skins=actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(skin=>skin.sharedMesh!=null).ToArray();var indices=skins.Select(skin=>skin.sharedMesh.triangles).ToArray();var boxes=truck.GetComponentsInChildren<Collider>(true).Where(shape=>shape.enabled&&!shape.isTrigger&&shape.gameObject.activeInHierarchy).ToArray();if(boxes.Length==0||boxes.Any(shape=>shape is not BoxCollider))throw new InvalidOperationException("Every actual original enabled solid NWH box is required.");
                var influenceText=skins.Select(skin=>{var bones=skin.bones;return skin.sharedMesh.boneWeights.Select(w=>bones[w.boneIndex0].name+"="+w.weight0.ToString("R")+";"+bones[w.boneIndex1].name+"="+w.weight1.ToString("R")+";"+bones[w.boneIndex2].name+"="+w.weight2.ToString("R")+";"+bones[w.boneIndex3].name+"="+w.weight3.ToString("R")).ToArray();}).ToArray();
                var centerIds=new HashSet<int>{frame.surfaceVertexA,frame.surfaceVertexB,frame.surfaceVertexC};
                var support=OriginalFootprints(new[]{originals[0]}).Single();
                if(startup!=null)
                {
                    // Preserve the old imported frame as an explicit inactive
                    // diagnostic. Actual V341 public starts render a different,
                    // native configured-default source pose before mounting.
                    row.inactiveImportedStagingScope="IMPORTED_SERIALIZED_SOURCE_FRAME_RETAINED_NOT_RENDERED_BY_PROVEN_PUBLIC_PREPARE";
                    try
                    {
                        foreach(var joint in rest)joint.Restore();frame.CaptureEvaluatedAnimationPose();var staged=frame.ReadPose();truck.transform.SetPositionAndRotation(staged.position+staged.rotation*Vector3.up*support.support,staged.rotation);
                        for(int skinIndex=0;skinIndex<skins.Length;skinIndex++){skins[skinIndex].BakeMesh(mesh,true);var vertices=mesh.vertices.Select(skins[skinIndex].transform.TransformPoint).ToArray();foreach(var shape in boxes.Cast<BoxCollider>()){var local=vertices.Select(shape.transform.InverseTransformPoint).Select(point=>point-shape.center).ToArray();var ids=indices[skinIndex];for(int i=0;i<ids.Length;i+=3)if(!SeparatedByBoxAxes(local[ids[i]],local[ids[i+1]],local[ids[i+2]],shape.size*.5f)&&ActualTriangleBoxSat(local[ids[i]],local[ids[i+1]],local[ids[i+2]],shape.size*.5f,out _))row.inactiveImportedStagingSatPairs++;}}
                    }
                    catch(InvalidOperationException error){row.inactiveImportedStagingFailure=error.Message;}
                    foreach(var joint in rest)joint.Restore();if(allActionMounted)InstallActualExistingOwnedDefaultTrial(actor,trialList,startup,row,originalStartupController,species);else InstallProvenConfiguredStartupTrial(actor,trialList,startup,row);foreach(var joint in rest)joint.Restore();
                }
                var forbidden=new List<UniformSizeInterval>();var snapshots=new List<SizePoseSnapshot>();bool hasOriginalBounds=false;row.boxes=boxes.Length;
                foreach(var trial in trialList)
                {
                    foreach(var jointPose in rest)jointPose.Restore();SampleMountedTrial(actor,trial);
                    try{frame.CaptureEvaluatedAnimationPose();}
                    catch(InvalidOperationException error){row.invalidAnatomicalPoses++;if(row.invalidPoses.Count<32)row.invalidPoses.Add(trial.name+" | "+error.Message);continue;}
                    var pose=frame.ReadPose();truck.transform.SetPositionAndRotation(pose.position+pose.rotation*Vector3.up*support.support,pose.rotation);var anchor=frame.ActualSurfacePoint;
                    var preciseAnchor=row.precisePlanning?PreciseMaterialAnchor(frame.ReadActualSkinVertexWorld(frame.surfaceVertexA),frame.ReadActualSkinVertexWorld(frame.surfaceVertexB),frame.ReadActualSkinVertexWorld(frame.surfaceVertexC),frame.surfaceBarycentric):new SizePlanPoint(anchor);
                    row.maximumPlanningAnchorDistance=Mathf.Max(row.maximumPlanningAnchorDistance,Vector3.Distance(anchor,preciseAnchor.Float));if(row.maximumPlanningAnchorDistance>=.001f)throw new InvalidOperationException("Planning authored-barycentric point differs from actual native material anchor by1mm or more; no substitute support location may be used.");
                    var captured=new SizePoseSnapshot{trial=trial,anchor=anchor,skins=new Vector3[skins.Length][]};row.actualPoses++;
                    for(int skinIndex=0;skinIndex<skins.Length;skinIndex++)
                    {
                        var skin=skins[skinIndex];skin.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(skin.transform.TransformPoint).ToArray();captured.skins[skinIndex]=vertices;row.triangles+=indices[skinIndex].Length/3;
                        foreach(var vertex in vertices){if(!hasOriginalBounds){row.originalAllPoseBounds=new Bounds(vertex,Vector3.zero);hasOriginalBounds=true;}else row.originalAllPoseBounds.Encapsulate(vertex);}
                        foreach(var shape in boxes.Cast<BoxCollider>())
                        {
                            var localAnchor=shape.transform.InverseTransformPoint(anchor)-shape.center;var local=vertices.Select(vertex=>shape.transform.InverseTransformPoint(vertex)-shape.center).ToArray();var half=shape.size*.5f;var plannedHalf=half+Vector3.one*row.planningMargin;var triangleIndices=indices[skinIndex];
                            for(int index=0;index<triangleIndices.Length;index+=3)
                            {
                                var a=local[triangleIndices[index]];var b=local[triangleIndices[index+1]];var c=local[triangleIndices[index+2]];
                                double low,high;bool intersects=row.precisePlanning?ForbiddenPreciseUniformSize(vertices[triangleIndices[index]],vertices[triangleIndices[index+1]],vertices[triangleIndices[index+2]],preciseAnchor,shape.transform.worldToLocalMatrix,shape.center,plannedHalf,out low,out high):ForbiddenUniformSize(a,b,c,localAnchor,plannedHalf,out low,out high);
                                bool unit= !SeparatedByBoxAxes(a,b,c,plannedHalf)&&ActualTriangleBoxSat(a,b,c,plannedHalf,out _);
                                bool plannedAtOne=intersects&&low<=1&&high>=1;
                                if(plannedAtOne!=unit)
                                {
                                    if(unit)row.unsafeExpandedPlanningMisses++;else row.conservativeExpandedPlanningPairs++;
                                    if(row.unitPlanningDifferences.Count<8)row.unitPlanningDifferences.Add(new MountedSizeIntervalWitness{pose=trial.name,skin=AnimationUtility.CalculateTransformPath(skin.transform,actor.motor.transform),box=AnimationUtility.CalculateTransformPath(shape.transform,truck.transform),triangle=index/3,a=triangleIndices[index],b=triangleIndices[index+1],c=triangleIndices[index+2],actualMaterialAnchor=anchor,preciseAuthoredMaterialAnchor=preciseAnchor.Float,authoredMaterialBarycentric=frame.surfaceBarycentric,worldA=vertices[triangleIndices[index]],worldB=vertices[triangleIndices[index+1]],worldC=vertices[triangleIndices[index+2]],actualBoxCenter=shape.center,boxHalfExtents=half,actualBoxMatrix=shape.transform.localToWorldMatrix,actualBoxWorldToLocal=shape.transform.worldToLocalMatrix,nativeExpandedUnitHit=unit,planningExpandedUnitHit=plannedAtOne});
                                    if(unit||!row.precisePlanning)throw new InvalidOperationException("Planning missed actual native expanded SAT or legacy exact-unit control disagreed: "+trial.name+" triangle="+index/3+" native="+unit+" planning="+plannedAtOne);
                                }
                                row.intervalUnitControls++;
                                if(!SeparatedByBoxAxes(a,b,c,half)&&ActualTriangleBoxSat(a,b,c,half,out _))row.originalSatPairs++;
                                if(!intersects)continue;
                                int va=triangleIndices[index],vb=triangleIndices[index+1],vc=triangleIndices[index+2];
                                var witness=new MountedSizeIntervalWitness{pose=trial.name,skin=AnimationUtility.CalculateTransformPath(skin.transform,actor.motor.transform),box=AnimationUtility.CalculateTransformPath(shape.transform,truck.transform),triangle=index/3,a=va,b=vb,c=vc,originalVertexAWeights=influenceText[skinIndex][va],originalVertexBWeights=influenceText[skinIndex][vb],originalVertexCWeights=influenceText[skinIndex][vc],actualCenterAnchorTriangle=skin==frame.surfaceRenderer&&centerIds.Contains(va)&&centerIds.Contains(vb)&&centerIds.Contains(vc),lower=(float)low,upper=double.IsPositiveInfinity(high)?0:(float)high,unbounded=double.IsPositiveInfinity(high),actualMaterialAnchor=anchor,preciseAuthoredMaterialAnchor=preciseAnchor.Float,authoredMaterialBarycentric=frame.surfaceBarycentric,worldA=vertices[va],worldB=vertices[vb],worldC=vertices[vc],boxHalfExtents=half,actualBoxCenter=shape.center,actualBoxMatrix=shape.transform.localToWorldMatrix,actualBoxWorldToLocal=shape.transform.worldToLocalMatrix};
                                forbidden.Add(new UniformSizeInterval{lower=low,upper=high,witness=witness});
                            }
                        }
                    }
                    snapshots.Add(captured);
                }
                row.forbiddenIntervals=forbidden.Count;row.roundedCandidateFactor=SmallestUniformSize(forbidden,row);
                if(row.invalidAnatomicalPoses>0){row.status="REJECTED_ORIGINAL_MOTION_HAS_SCALE_INVARIANT_INVALID_ANATOMICAL_POSES";return;}
                if(!row.hasFiniteUniformSize){row.status="REJECTED_UNBOUNDED_CONTACT_NO_UNIFORM_SIZE_SOLUTION";return;}
                float factor=row.roundedCandidateFactor;var originalRoot=copy.transform.localScale;var originalDeckScale=frame.supportPresentation.localScale;
                if(motorRootRepresentation)
                {
                    var ownedController=actor.motor.animator.runtimeAnimatorController;PrepareInactiveMotorRootCandidate(preview,saved,truck,ownedController,material,factor,row,ref copy,ref actor,ref frame,ref rest);skins=actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(skin=>skin.sharedMesh!=null).ToArray();if(skins.Length!=indices.Length||!skins.Select(skin=>skin.sharedMesh.triangles.Length).SequenceEqual(indices.Select(index=>index.Length)))throw new InvalidOperationException("Fresh motor-root clone changed original source skin/topology membership.");row.actualCourseEnvelope=new ActualMountedCourseEnvelope();if(species.id=="dragon-fire")row.actualCourseEnvelope.scope="ALL537_ACTUAL_REFERENCED_FIRE_SKIN_AND_ORIGINAL_NWH_HARNESS_POINTS_RELATIVE_REAL_TRUCK_SHAPE_BOUNDS_CENTER_NOT_AABB_CORNER_RADIUS_NOT_COURSE_OR_RUNTIME_ACCEPTANCE";
                }
                else{copy.transform.localScale=originalRoot*factor;frame.supportPresentation.localScale=originalDeckScale/factor;}
                // Only an isolated owned-dragon clone changes size. Original
                // raw files/shared species and the full NWH/deck world size do
                // not change. Compare actual native skin to the exact predicted
                // uniform enlargement before counting any positive result.
                bool hasCandidateBounds=false;Vector3 origin=copy.transform.position;
                foreach(var snapshot in snapshots)
                {
                    foreach(var jointPose in rest)jointPose.Restore();SampleMountedTrial(actor,snapshot.trial);frame.CaptureEvaluatedAnimationPose();var pose=frame.ReadPose();truck.transform.SetPositionAndRotation(pose.position+pose.rotation*Vector3.up*support.support,pose.rotation);frame.rigging.RefreshAuthoredPresentation(true);
                    row.actualCandidateSkinScaleError=Mathf.Max(row.actualCandidateSkinScaleError,Vector3.Distance(frame.ActualSurfacePoint,origin+(snapshot.anchor-origin)*factor));
                    for(int skinIndex=0;skinIndex<skins.Length;skinIndex++)
                    {
                        skins[skinIndex].BakeMesh(mesh,true);var vertices=mesh.vertices.Select(skins[skinIndex].transform.TransformPoint).ToArray();if(vertices.Length!=snapshot.skins[skinIndex].Length)throw new InvalidOperationException("Uniform size changed source mesh identity.");
                        for(int vertex=0;vertex<vertices.Length;vertex++){row.actualCandidateSkinScaleError=Mathf.Max(row.actualCandidateSkinScaleError,Vector3.Distance(vertices[vertex],origin+(snapshot.skins[skinIndex][vertex]-origin)*factor));if(!hasCandidateBounds){row.candidateAllPoseBounds=new Bounds(vertices[vertex],Vector3.zero);hasCandidateBounds=true;}else row.candidateAllPoseBounds.Encapsulate(vertices[vertex]);}
                        foreach(var shape in boxes.Cast<BoxCollider>())
                        {
                            var local=vertices.Select(vertex=>shape.transform.InverseTransformPoint(vertex)-shape.center).ToArray();var half=shape.size*.5f;var triangleIndices=indices[skinIndex];for(int index=0;index<triangleIndices.Length;index+=3){var a=local[triangleIndices[index]];var b=local[triangleIndices[index+1]];var c=local[triangleIndices[index+2]];if(!SeparatedByBoxAxes(a,b,c,half)&&ActualTriangleBoxSat(a,b,c,half,out _))row.candidateSatPairs++;}
                        }
                    }
                    var seating=MeasureActualDeckSeating(truck,frame.supportPresentation,pose.rotation*Vector3.up,"READ_ONLY_INTENTIONALLY_SIZED_OWNED_MOUNT_ORIGINAL_NWH_AND_TIMBER_WORLD_SIZE");row.candidateWheelContactPoses++;if(seating.wheels.Length!=4||seating.wheels.Any(wheel=>!wheel.actualVisualFound||!wheel.deckFaceFound||Mathf.Abs(wheel.actualGap)>.03f))throw new InvalidOperationException("Owned mount size failed unchanged original four-wheel/timber3cm acceptance.");row.maximumActualWheelGap=Mathf.Max(row.maximumActualWheelGap,seating.wheels.Max(wheel=>Mathf.Abs(wheel.actualGap)));if(row.actualCourseEnvelope!=null)CaptureActualTruckCenteredCourseEnvelope(row.actualCourseEnvelope,actor,frame,truck,snapshot.trial.name);
                }
                if(row.actualCourseEnvelope!=null)CompleteActualCourseEnvelopeDimensions(row.actualCourseEnvelope);
                row.exactOriginalBoxAcceptance=row.candidateSatPairs==0&&row.actualCandidateSkinScaleError<.001f;
                var calibration=CaptureFrameCalibration(frame);var poses=actor.motor.GetComponentsInChildren<Transform>(true).Select(joint=>new BonePose{bone=joint,position=joint.localPosition,rotation=joint.localRotation}).ToArray();row.originalFootprintFits=FitsOriginalFootprints(actor,species,calibration,poses,originals,out var footprint);WriteFootprintReceipt(footprint);
                row.status=row.exactOriginalBoxAcceptance&&row.originalFootprintFits?"DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED_REQUIRES_CAB_RUNTIME_WORLD_AND_EXPLICIT_VARIANT":"REJECTED_DERIVED_SIZE_NATIVE_ORIGINAL_CONTACT_OR_FOOTPRINT";
                row.sourceMotionUnchanged=sourcePaths.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);
                if(!row.sourceMotionUnchanged)throw new InvalidOperationException("Read-only source-motion size comparison changed original assets.");
                if(motorRootRepresentation&&coldFixtureExport!=null)coldFixtureExport.Export(copy,actor,frame,rest,connected,row);
            }
            finally
            {
                if(frame!=null)frame.ReleaseGeometrySnapshot();if(raw!=null)UnityEngine.Object.DestroyImmediate(raw);if(connected!=null)foreach(var item in connected)if(item!=null&&!AssetDatabase.Contains(item))UnityEngine.Object.DestroyImmediate(item);UnityEngine.Object.DestroyImmediate(mesh);preview.Cleanup();
                if(sourcePaths!=null){row.sourceMotionUnchanged=sourcePaths.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);if(!row.sourceMotionUnchanged)throw new InvalidOperationException("Read-only mounted size derivation changed protected original source bytes.");}
            }
        }
        static void WriteMountedSizeReport(MountedSizeReport report)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","MountedSize");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,(report.scope.StartsWith("PRECISE_")?"precise-":"")+(report.scope.Contains("UNIT_ANCESTORS_MOTOR_ROOT_REPRESENTATION")?(report.profiles.Any(row=>row.species=="dragon-fire")?"motor-root-fire-course-envelope.json":"motor-root-inferno-course-envelope.json"):report.scope.Contains("EXISTING_ALL_ACTION_SAFE_MOUNTED")?"all-action-safe-inferno-size-derivation.json":report.scope.Contains("NATIVE_DERIVED_STRICT_INFERNO_MATERIAL_PATCH")?"native-material-inferno-size-derivation.json":report.scope.Contains("PROVEN_FIRE_HEAD_LEVER")?"proven-fire-head-lever-size-derivation.json":report.scope.Contains("PROVEN_PUBLIC_CONFIGURED_STARTUP")?"actual-public-startup-inferno-size-derivation.json":report.scope.Contains("V279_FIRE_HALF")?"fire-normal-family-size-derivation.json":report.scope.Contains("V279_CONNECTED")?"original-centered-connected-size-derivation.json":"original-motion-size-derivation.json")),JsonUtility.ToJson(report,true));
        }
    }
}
#endif
