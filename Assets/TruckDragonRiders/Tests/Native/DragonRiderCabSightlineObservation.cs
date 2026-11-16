#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

// Native test-only diagnosis of existing cameras at their real render callback.
// No camera, renderer, animation, physics, input or game state is changed.
public sealed class DragonRiderCabSightlineObservation : IDisposable
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly Component match;readonly string path;readonly Camera[] cameras;readonly List<View> frames=new List<View>();readonly HashSet<Camera> observed=new HashSet<Camera>();
    [Serializable]sealed class Receipt{public string scope="READ_ONLY_ACTUAL_RENDER_TRUE_BAKE_FULL_MATRIX_INDEPENDENT_WEIGHTED_SKIN_TWO_SIDED_RAYS_NOT_FULL_VISIBLE_PIXEL_OR_HUMAN_ACCEPTANCE";public List<View> views;}
    [Serializable]sealed class View
    {
        public int view,owner;public string source,actualAction,actualBaseClips,actualActionClips;public float actualActionNormalized;public bool actualActionPlaying;public Vector3 cameraEye,cameraForward,cameraUp,originalCabMountWorld,rigidCabEye,cameraMinusRigidEye,truckPosition,truckEuler;public float fieldOfView,aspect,nearClip,clock,actualLookYaw,actualLookPitch,cameraAngleToActualTruckForward,cameraAngleToRequestedFlightHeading;public string exactStateBeforeSha256,exactStateAfterSha256,exactBeforeState,exactAfterState,diagnosticError,actualWheelDeckSeating;public bool exactOwnedStatePreserved;public Rect viewport;public RayWitness[] centralWindscreenRays;public SkinSource[] actualSkinSources;public VirtualCabPose[] sameFrameVirtualCabCandidates;public CabMeshRegistration[] exactCanonicalCabMeshRegistrations;public NeckPosture[] pureConnectedNeckCandidates;
    }
    [Serializable]sealed class VirtualCabPose
    {
        public string scope="VIRTUAL_RAYS_ONLY_NO_ACTUAL_CAMERA_OR_GEOMETRY_CHANGE_NOT_RENDERED_ACCEPTANCE",name;public Vector3 eye,forward,up,localTruckEye;public float angleToActualTruckForward,maximumActualRayRoundTripOriginError,maximumActualRayRoundTripDirectionDegrees;public int anySkinHits,ownSkinHits,ownHeadNeckDominantHits;public RayWitness[] centralWindscreenRays;
    }
    [Serializable]sealed class CabMeshRegistration
    {
        public string scope="EXACT_SHARED_SOURCE_MESH_IDENTITY_REGISTRATION_NOT_A_DESIGNER_AUTHORED_NEW_SEAT",canonicalPrefab,sourceFilter,liveFilter,sourceMeshGuid,sourceMeshLocalId,appearanceBodyId;public bool exactSharedMeshIdentity;public Vector3 sourceDriverEyeRootLocal,sourceDriverEyeInMesh,mappedDriverEyeWorld,mappedDriverEyeTruckLocal,mappedDriverForward,mappedDriverUp,sourceMeshScale,liveMeshScale,actualEyeMinusRegisteredEye;public Matrix4x4 sourceMeshLocalToWorld,liveMeshLocalToWorld;public Bounds sourceMeshBounds,liveRendererWorldBounds;
    }
    [Serializable]sealed class NeckPosture
    {
        public string scope="PURE_WEIGHTED_GEOMETRY_PREDICTION_ONLY_NO_NATIVE_POSE_WRITER_NOT_ANIMATION_OR_RENDER_ACCEPTANCE",neckRoot,head,recipe;public bool worldAimPreservedByConstruction,originalHeadLocalPoseExpectedUnchanged;public WingDihedralProof[] connectedWingDihedral;public bool nonSelectedWorldMatricesExact;public float wingDihedralDegrees,maximumOriginalHeadLocalPoseError,requestedNeckPitch,requestedHeadPitch,headWorldRotationChange,downwardDegrees,zeroOffsetTrueBakeRmse,headWorldAimError,mouthWorldAimError,maximumChangedLocalTranslation,maximumUnchangedLocalMatrixError,maximumSupportPointDisplacement,headHeightChange,positivePitchModelForwardDotUp;public bool nonNeckWorldMatricesExact,satControlsPassed;public Vector3 modelRight,modelUp,modelForward,originalHeadPosition,candidateHeadPosition,originalHeadForward,candidateHeadForward,originalMouthForward,candidateMouthForward;public JointMatrix[] connectedJointMatrices;public Vector3[] actualSupportPoints,candidateSupportPoints;public int completeSkinTriangles,actualOriginalBoxes,triangleBoxIntersections,actualTrueBakeTriangleBoxIntersections;public string[] unsupportedOriginalSolidColliders;public TriangleBoxIntersection[] intersections,actualTrueBakeIntersections;public VirtualCabPose[] sameFrameCabRays;
    }
    sealed class NeckCase{public Transform bone;public float neckPitch,headPitch,wingDihedral;public bool preserveAim;public string recipe;}
    [Serializable]sealed class WingDihedralProof{public string root,actualDistalBone;public float signedDegrees,physicalSide,outboardUnitControlModelUpDelta,actualDistalModelUpDelta;public Vector3 originalRootWorld,originalDistalWorld,candidateDistalWorld;public Matrix4x4 originalRootWorldMatrix,candidateRootWorldMatrix;}
    [Serializable]sealed class JointMatrix{public string bone,parent;public bool neckHierarchy,headHierarchy,wingHierarchy;public Matrix4x4 originalWorld,candidateWorld,originalLocal,candidateLocal;}
    [Serializable]sealed class TriangleBoxIntersection{public string collider,originalVertexAWeights,originalVertexBWeights,originalVertexCWeights,averageOriginalBoneWeights;public float averageHeadNeckWeight,averageWingWeight,averageTailWeight;public int triangle,a,b,c;public Vector3 worldA,worldB,worldC,boxLocalA,boxLocalB,boxLocalC,boxSize,boxCenter;public Matrix4x4 actualOriginalBoxLocalToWorld;}
    [Serializable]sealed class RayWitness
    {
        public Vector2 viewport;public Vector3 origin,direction;public int hitSlot=-1;public string renderer,boneWeights;public float distance,headNeckWeight,normalDotRay,smoothNormalDotRay,geometricNormalDotSmooth;public Vector3 worldPoint;public int triangle=-1;public bool ownDragon,frontFacing;
    }
    [Serializable]sealed class SkinSource{public int slot,vertexCount,triangleCount,submeshCount,headNeckDominantVertices;public string renderer,sourceMesh,sourceGuid,sourceLocalId;public string[] materials,headNeckBones;public Bounds selectedTrueWorldBounds,falseWorldBounds,actualRendererWorldBounds;public float rendererMatrixDeterminant,trueFullMatrixRmse,falseFullMatrixRmse,actualTriangleForwardDistance,actualTriangleReverseDistance;public string independentSkinningError;}
    sealed class Skin
    {
        public int slot;public string renderer;public Vector3[] world;public int[] triangles;public BoneWeight[] weights;public Transform[] bones;public Bounds bounds;public Vector3[] normals;public SkinSource source;public SkinnedMeshRenderer actualRenderer;
    }
    static object Read(object source,string name)=>source.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(source):source.GetType().GetProperty(name,Any).GetValue(source);
    public DragonRiderCabSightlineObservation(Component session,string output)
    {VerifyRayControls();match=session;path=output;cameras=(Camera[])Read(match,"cameras");RenderPipelineManager.beginCameraRendering+=Rendering;}
    void Rendering(ScriptableRenderContext context,Camera camera)
    {
        int view=Array.IndexOf(cameras,camera);if(view<0||!observed.Add(camera))return;
        var driving=Read(match,"drivingViews");int owner=((int[])Read(Read(driving,"rally"),"owners"))[view];var truck=(Component)((Array)Read(match,"trucks")).GetValue(owner);var body=(Rigidbody)Read(truck,"body");var mounts=(Transform[])Read(driving,"cabMounts");var cab=mounts!=null&&owner<mounts.Length?mounts[owner]:null;Vector3 offset=cab!=null?truck.transform.InverseTransformPoint(cab.position):new Vector3(-.17f,1.3f,.65f);Vector3 rigidEye=body.position+body.rotation*Vector3.Scale(truck.transform.lossyScale,offset);
        var look=((Array)Read(driving,"looks")).GetValue(view);var racer=((Array)Read(match,"racers")).GetValue(owner);
        var motor=Read(Read(racer,"Dragon"),"motor");var animator=(Animator)Read(motor,"animator");string Clips(int layer)=>string.Join(";",animator.GetCurrentAnimatorClipInfo(layer).Select(value=>value.clip.name+"@"+value.weight.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        var row=new View{actualAction=Read(motor,"CurrentAction").ToString(),actualActionPlaying=(bool)Read(motor,"ActionPlaying"),actualActionNormalized=(float)Read(motor,"ActionNormalizedTime"),actualBaseClips=Clips(0),actualActionClips=animator.layerCount>1?Clips(1):"",actualLookYaw=(float)Read(look,"Yaw"),actualLookPitch=(float)Read(look,"Pitch"),cameraAngleToActualTruckForward=Vector3.Angle(camera.transform.forward,body.rotation*Vector3.forward),cameraAngleToRequestedFlightHeading=Vector3.Angle(camera.transform.forward,(Vector3)Read(racer,"FlightHeading")),view=view,owner=owner,source=Read(((Array)Read(Read(match,"setup"),"players")).GetValue(owner),"source").ToString(),cameraEye=camera.transform.position,cameraForward=camera.transform.forward,cameraUp=camera.transform.up,originalCabMountWorld=cab!=null?cab.position:Vector3.zero,rigidCabEye=rigidEye,cameraMinusRigidEye=camera.transform.position-rigidEye,truckPosition=body.position,truckEuler=body.rotation.eulerAngles,fieldOfView=camera.fieldOfView,aspect=camera.aspect,nearClip=camera.nearClipPlane,clock=(float)Read(match,"Clock"),viewport=camera.rect};
        row.exactBeforeState=SnapshotOwnedState();row.exactStateBeforeSha256=Hash(row.exactBeforeState);Exception diagnosticFailure=null;
        try
        {
        var skins=ReadActualSkins(camera);var rays=new List<RayWitness>();
        // A named central windscreen grid, not a claim about whole-frame area.
        for(int y=0;y<5;y++)for(int x=0;x<5;x++)
        {
            var uv=new Vector2(.3f+x*.1f,.3f+y*.1f);Ray ray=camera.ViewportPointToRay(new Vector3(uv.x,uv.y,0));rays.Add(TraceActualSkin(ray,uv,owner,skins));
        }
        row.centralWindscreenRays=rays.ToArray();row.actualSkinSources=skins.Select(skin=>skin.source).ToArray();
        row.exactCanonicalCabMeshRegistrations=RegisterCanonicalCabMesh(truck,camera);
        // Keep the exact observed frustum, near-plane offsets and look angles.
        // Only re-express its rays in hypothetical physical cab frames; never
        // move the real camera, sample animation, or alter the mounted truck.
        var fullRotation=body.rotation*Quaternion.Euler(row.actualLookPitch,row.actualLookYaw,0);
        var unscaledEye=body.position+body.rotation*offset;
        var candidates=new List<VirtualCabPose>{
            VirtualCandidate("ACTUAL_CAMERA_ROUND_TRIP_CONTROL",camera.transform.position,camera.transform.rotation),
            VirtualCandidate("BODY_FRAME_EYE_ONLY_ACTUAL_ORIENTATION",rigidEye,camera.transform.rotation),
            VirtualCandidate("ACTUAL_EYE_BODY_FRAME_ORIENTATION_ONLY",camera.transform.position,fullRotation),
            VirtualCandidate("BODY_FRAME_EYE_AND_ORIENTATION_SCALED_LOCAL_OFFSET",rigidEye,fullRotation),
            VirtualCandidate("BODY_FRAME_EYE_AND_ORIENTATION_UNSCALED_LOCAL_OFFSET",unscaledEye,fullRotation)};
        foreach(var registration in row.exactCanonicalCabMeshRegistrations)
        {
            // This candidate comes from the exact corresponding authored source
            // camera/mesh frame, never from a guessed scalar driver-eye height.
            var registeredRotation=Quaternion.LookRotation(registration.mappedDriverForward,registration.mappedDriverUp)*Quaternion.Euler(row.actualLookPitch,row.actualLookYaw,0);
            candidates.Add(VirtualCandidate("SOURCE_CAMERA_REGISTERED_THROUGH_EXACT_"+registration.sourceFilter+"_MESH",registration.mappedDriverEyeWorld,registeredRotation));
            candidates.Add(VirtualCandidate("SOURCE_EYE_ONLY_THROUGH_EXACT_"+registration.sourceFilter+"_MESH_ACTUAL_HORIZON_ORIENTATION",registration.mappedDriverEyeWorld,camera.transform.rotation));
            candidates.Add(VirtualCandidate("SOURCE_ORIENTATION_ONLY_THROUGH_EXACT_"+registration.sourceFilter+"_MESH_ACTUAL_EYE",camera.transform.position,registeredRotation));
        }
        row.sameFrameVirtualCabCandidates=candidates.ToArray();
        var control=row.sameFrameVirtualCabCandidates[0];
        if(control.maximumActualRayRoundTripOriginError>.0001f||control.maximumActualRayRoundTripDirectionDegrees>.05f||control.centralWindscreenRays.Where((ray,index)=>ray.hitSlot!=row.centralWindscreenRays[index].hitSlot||ray.triangle!=row.centralWindscreenRays[index].triangle||Mathf.Abs(ray.distance-row.centralWindscreenRays[index].distance)>.0001f).Any())throw new InvalidOperationException("Virtual cab same-pose ray reconstruction differs from the actual frustum/skin oracle; do not interpret hypothetical body-frame candidates.");
        row.pureConnectedNeckCandidates=ScreenConnectedNeckPostures(row,skins,truck,(Component)Read(racer,"Dragon"),(Component)motor);
        var actor=(Component)Read(racer,"Dragon");var frame=actor.GetComponents<Component>().Single(value=>value!=null&&value.GetType().FullName=="LWS.TruckDragonRiders.DragonRiderSaddle");var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);row.actualWheelDeckSeating=(string)author.GetMethod("ObserveActualTruckDeckSeating").Invoke(null,new object[]{frame,truck});
        VirtualCabPose VirtualCandidate(string name,Vector3 eye,Quaternion rotation)
        {
            var candidate=new VirtualCabPose{name=name,eye=eye,forward=rotation*Vector3.forward,up=rotation*Vector3.up,localTruckEye=Quaternion.Inverse(body.rotation)*(eye-body.position),angleToActualTruckForward=Vector3.Angle(rotation*Vector3.forward,body.rotation*Vector3.forward)};var virtualRays=new List<RayWitness>();
            foreach(var actual in row.centralWindscreenRays)
            {
                var localOrigin=Quaternion.Inverse(camera.transform.rotation)*(actual.origin-camera.transform.position);var localDirection=Quaternion.Inverse(camera.transform.rotation)*actual.direction;var origin=eye+rotation*localOrigin;var direction=rotation*localDirection;
                if(name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL"){candidate.maximumActualRayRoundTripOriginError=Mathf.Max(candidate.maximumActualRayRoundTripOriginError,Vector3.Distance(origin,actual.origin));candidate.maximumActualRayRoundTripDirectionDegrees=Mathf.Max(candidate.maximumActualRayRoundTripDirectionDegrees,Vector3.Angle(direction,actual.direction));}
                virtualRays.Add(TraceActualSkin(new Ray(origin,direction),actual.viewport,owner,skins));
            }
            candidate.centralWindscreenRays=virtualRays.ToArray();candidate.anySkinHits=virtualRays.Count(hit=>hit.hitSlot>=0);candidate.ownSkinHits=virtualRays.Count(hit=>hit.ownDragon);candidate.ownHeadNeckDominantHits=virtualRays.Count(hit=>hit.ownDragon&&hit.headNeckWeight>=.5f);return candidate;
        }
        }
        catch(Exception error){diagnosticFailure=error;row.diagnosticError=error.ToString();throw;}
        finally
        {
            row.exactAfterState=SnapshotOwnedState();row.exactStateAfterSha256=Hash(row.exactAfterState);row.exactOwnedStatePreserved=string.Equals(row.exactBeforeState,row.exactAfterState,StringComparison.Ordinal);frames.Add(row);Write();
            if(!row.exactOwnedStatePreserved&&diagnosticFailure==null)throw new InvalidOperationException("Read-only cab skin diagnosis changed actual owned transforms/bodies/Animator/native/global-physics state; inspect retained exact before/after receipt before making a gameplay claim.");
        }
    }
    static NeckPosture[] ScreenConnectedNeckPostures(View row,List<Skin> skins,Component truck,Component actor,Component motor)
    {
        var original=skins.Single(value=>value.slot==row.owner);var bones=original.bones;var head=bones.Single(value=>value!=null&&value.name=="RigHead");var necks=bones.Where(value=>value!=null&&value.name.StartsWith("RigNeck",StringComparison.Ordinal)).ToArray();var neckRoot=necks.Single(value=>!necks.Any(other=>other!=value&&value.IsChildOf(other)));
        if(!head.IsChildOf(neckRoot))throw new InvalidOperationException("Actual head is not connected to the selected original neck hierarchy; no synthetic posture may be invented.");
        var mouth=(Transform)Read(actor,"mouth");if(mouth==null||!mouth.IsChildOf(head))throw new InvalidOperationException("Actual calibrated muzzle must remain connected to original head/jaw.");
        Vector3[] sourcePoints;using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(original.actualRenderer.sharedMesh))using(var points=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp)){data[0].GetVertices(points);sourcePoints=points.ToArray();}var bindposes=original.actualRenderer.sharedMesh.bindposes;
        var solids=truck.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy).ToArray();var boxes=solids.OfType<BoxCollider>().ToArray();if(boxes.Length==0)throw new InvalidOperationException("Actual original NWH solid boxes are required for complete candidate geometry screening.");
        bool controls=TriangleBoxIntersects(new Vector3(-.2f,0,0),new Vector3(.2f,0,0),new Vector3(0,.2f,0),Vector3.one)&&!TriangleBoxIntersects(new Vector3(-.2f,0,2),new Vector3(.2f,0,2),new Vector3(0,.2f,2),Vector3.one)&&TriangleBoxIntersects(new Vector3(-3,-3,0),new Vector3(3,-3,0),new Vector3(0,3,0),Vector3.one)&&!TriangleBoxIntersects(new Vector3(2,0,0),new Vector3(0,2,0),new Vector3(2,2,0),Vector3.one*.5f);if(!controls)throw new InvalidOperationException("Candidate full triangle/OBB SAT failed contained/separated/crossing/diagonal controls.");
        var saddle=actor.GetComponents<Component>().Single(value=>value!=null&&value.GetType().FullName=="LWS.TruckDragonRiders.DragonRiderSaddle");if(!ReferenceEquals(Read(saddle,"surfaceRenderer"),original.actualRenderer))throw new InvalidOperationException("Candidate complete skin does not own the actual five-point saddle surface.");var anchors=((Array)Read(saddle,"surfacePatchAnchors")).Cast<object>().ToArray();if(anchors.Length!=5)throw new InvalidOperationException("Actual five original support anchors required.");
        var transforms=motor.GetComponentsInChildren<Transform>(true);var reports=new List<NeckPosture>();var actualTrueBakeIntersections=InspectCompleteSkin(original.world);
        var cases=new List<NeckCase>{new NeckCase{bone=neckRoot,preserveAim=true,recipe="ZERO_OFFSET_NATIVE_TRUE_BAKE_CONTROL"}};
        // V258 ruled out bounded aim-preserving distal rotations. These next
        // explicitly labelled normal-flight predictions allow head aim to change;
        // they do NOT alter, certify or substitute original Breath contact aim.
        // A real transient authored AOC has already applied its recipe. Screen
        // refinements only against the unchanged original baseline; the actual
        // authored pass retains its zero-offset native geometry proof alone.
        if(row.actualBaseClips.IndexOf("TRANSIENT_CAB_",StringComparison.Ordinal)<0)
        {
            var distal=necks.SingleOrDefault(value=>value.name=="RigNeck3");
            if(distal!=null)
            {
                cases.Add(new NeckCase{bone=distal,neckPitch=35,headPitch=10,recipe="ORIGINAL_BASELINE_INFERNO_NECK35_HEAD10_BUDGET45"});
                cases.Add(new NeckCase{bone=distal,neckPitch=45,headPitch=0,recipe="ORIGINAL_BASELINE_INFERNO_NECK45_ORIGINAL_HEAD_LOCALS_BUDGET45"});
                foreach(float wing in new[]{5f,10f})cases.Add(new NeckCase{bone=distal,neckPitch=45,headPitch=0,wingDihedral=wing,recipe="INFERNO_NECK45_ORIGINAL_HEAD_CONNECTED_WING_ROOT_DOWN_"+wing});
            }
            else
            {
                cases.Add(new NeckCase{bone=neckRoot,neckPitch=15,headPitch=30,recipe="FIRE_ORIGINAL_NECK15_PLUS_HEAD30_NORMAL_FLIGHT_NOT_BREATH_AIM"});
                foreach(float wing in new[]{5f,10f})cases.Add(new NeckCase{bone=neckRoot,neckPitch=15,headPitch=30,wingDihedral=wing,recipe="FIRE_NECK15_HEAD30_CONNECTED_WING_ROOT_DOWN_"+wing});
            }
        }
        foreach(var posture in cases)
        {
            var neck=posture.bone;float degrees=posture.neckPitch+posture.headPitch;if(degrees>45)throw new InvalidOperationException("Bounded diagnostic cannot exceed45degrees total requested joint pitch.");
            var rotation=Quaternion.AngleAxis(posture.neckPitch,motor.transform.right);var delta=posture.neckPitch==0?Matrix4x4.identity:Matrix4x4.Translate(neck.position)*Matrix4x4.Rotate(rotation)*Matrix4x4.Translate(-neck.position);var newHead=delta.MultiplyPoint3x4(head.position);var headRotation=posture.preserveAim?Quaternion.Inverse(rotation):Quaternion.AngleAxis(posture.headPitch,motor.transform.right);var counter=headRotation==Quaternion.identity?Matrix4x4.identity:Matrix4x4.Translate(newHead)*Matrix4x4.Rotate(headRotation)*Matrix4x4.Translate(-newHead);
            var wingDeltas=new Dictionary<Transform,Matrix4x4>();var wingProofs=new List<WingDihedralProof>();
            if(posture.wingDihedral>0)
            {
                if(posture.wingDihedral>10)throw new InvalidOperationException("Approved connected wing-root screen is bounded to10degrees.");
                var torso=bones.Single(value=>value!=null&&value.name=="RigRibcage");foreach(string name in new[]{"RigLWing01","RigRWing01"})
                {
                    var root=bones.Single(value=>value!=null&&value.name==name);float side=Mathf.Sign(Vector3.Dot(root.position-torso.position,motor.transform.right));if(Mathf.Abs(Vector3.Dot(root.position-torso.position,motor.transform.right))<.01f)throw new InvalidOperationException("Actual wing root side is ambiguous; do not guess its dihedral direction.");float signed=-side*posture.wingDihedral;var wingRotation=Quaternion.AngleAxis(signed,motor.transform.forward);var wingDelta=Matrix4x4.Translate(root.position)*Matrix4x4.Rotate(wingRotation)*Matrix4x4.Translate(-root.position);wingDeltas.Add(root,wingDelta);
                    var outboard=root.position+motor.transform.right*side;float down=Vector3.Dot(wingDelta.MultiplyPoint3x4(outboard)-outboard,motor.transform.up);if(down>=-.001f)throw new InvalidOperationException("Connected wing dihedral did not lower its explicitly labelled outboard unit control.");var distalBone=bones.Where(value=>value!=null&&(value==root||value.IsChildOf(root))).OrderByDescending(value=>Vector3.Dot(value.position-root.position,motor.transform.right)*side).First();
                    wingProofs.Add(new WingDihedralProof{root=root.name,actualDistalBone=distalBone.name,signedDegrees=signed,physicalSide=side,outboardUnitControlModelUpDelta=down,actualDistalModelUpDelta=Vector3.Dot(wingDelta.MultiplyPoint3x4(distalBone.position)-distalBone.position,motor.transform.up),originalRootWorld=root.position,originalDistalWorld=distalBone.position,candidateDistalWorld=wingDelta.MultiplyPoint3x4(distalBone.position),originalRootWorldMatrix=root.localToWorldMatrix,candidateRootWorldMatrix=wingDelta*root.localToWorldMatrix});
                }
                if(wingProofs.Count!=2||Mathf.Sign(wingProofs[0].signedDegrees)==Mathf.Sign(wingProofs[1].signedDegrees))throw new InvalidOperationException("Actual left/right wing roots require proven opposite dihedral signs.");
            }
            Matrix4x4 Candidate(Transform value){var source=value.localToWorldMatrix;if(value==head||value.IsChildOf(head))return counter*delta*source;if(value==neck||value.IsChildOf(neck))return delta*source;foreach(var pair in wingDeltas)if(value==pair.Key||value.IsChildOf(pair.Key))return pair.Value*source;return source;}
            var report=new NeckPosture{neckRoot=neck.name,head=head.name,recipe=posture.recipe,worldAimPreservedByConstruction=posture.preserveAim,requestedNeckPitch=posture.neckPitch,requestedHeadPitch=posture.headPitch,headWorldRotationChange=Quaternion.Angle(head.rotation,Candidate(head).rotation),downwardDegrees=degrees,modelRight=motor.transform.right,modelUp=motor.transform.up,modelForward=motor.transform.forward,positivePitchModelForwardDotUp=Vector3.Dot(Quaternion.AngleAxis(degrees,motor.transform.right)*motor.transform.forward,motor.transform.up),originalHeadPosition=head.position,candidateHeadPosition=newHead,headHeightChange=Vector3.Dot(newHead-head.position,motor.transform.up),originalHeadForward=head.forward,candidateHeadForward=Candidate(head).MultiplyVector(Vector3.forward).normalized,originalMouthForward=mouth.forward,candidateMouthForward=Candidate(mouth).MultiplyVector(Vector3.forward).normalized,nonNeckWorldMatricesExact=true,nonSelectedWorldMatricesExact=true,wingDihedralDegrees=posture.wingDihedral,connectedWingDihedral=wingProofs.ToArray(),satControlsPassed=controls,completeSkinTriangles=original.triangles.Length/3,actualOriginalBoxes=boxes.Length,unsupportedOriginalSolidColliders=solids.Where(value=>!(value is BoxCollider)).Select(value=>value.name+"/"+value.GetType().Name).ToArray(),actualTrueBakeTriangleBoxIntersections=actualTrueBakeIntersections.Count,actualTrueBakeIntersections=degrees==0?actualTrueBakeIntersections.ToArray():null};
            report.headWorldAimError=Vector3.Angle(report.originalHeadForward,report.candidateHeadForward);report.mouthWorldAimError=Vector3.Angle(report.originalMouthForward,report.candidateMouthForward);
            var joints=new List<JointMatrix>();foreach(var value in transforms)
            {
                var before=value.localToWorldMatrix;var after=Candidate(value);bool connected=value==neck||value.IsChildOf(neck),headChild=value==head||value.IsChildOf(head),wingChild=wingDeltas.Keys.Any(root=>value==root||value.IsChildOf(root));var localBefore=value.parent!=null?value.parent.worldToLocalMatrix*before:before;var localAfter=value.parent!=null?Candidate(value.parent).inverse*after:after;
                if(!connected)report.nonNeckWorldMatricesExact&=before.Equals(after);if(!connected&&!wingChild)report.nonSelectedWorldMatricesExact&=before.Equals(after);else report.maximumChangedLocalTranslation=Mathf.Max(report.maximumChangedLocalTranslation,Vector3.Distance(localBefore.GetColumn(3),localAfter.GetColumn(3)));
                if(value==head){report.originalHeadLocalPoseExpectedUnchanged=posture.headPitch==0&&!posture.preserveAim;for(int element=0;element<16;element++)report.maximumOriginalHeadLocalPoseError=Mathf.Max(report.maximumOriginalHeadLocalPoseError,Mathf.Abs(localBefore[element]-localAfter[element]));}
                if(value!=neck&&value!=head&&!wingDeltas.ContainsKey(value))for(int element=0;element<16;element++)report.maximumUnchangedLocalMatrixError=Mathf.Max(report.maximumUnchangedLocalMatrixError,Mathf.Abs(localBefore[element]-localAfter[element]));
                joints.Add(new JointMatrix{bone=value.name,parent=value.parent!=null?value.parent.name:"",neckHierarchy=connected,headHierarchy=headChild,wingHierarchy=wingChild,originalWorld=before,candidateWorld=after,originalLocal=localBefore,candidateLocal=localAfter});
            }
            report.connectedJointMatrices=joints.ToArray();var matrices=bones.Select(Candidate).ToArray();var candidatePoints=new Vector3[sourcePoints.Length];double error=0;for(int index=0;index<sourcePoints.Length;index++)
            {
                var weight=original.weights[index];var point=Vector3.zero;Add(weight.boneIndex0,weight.weight0);Add(weight.boneIndex1,weight.weight1);Add(weight.boneIndex2,weight.weight2);Add(weight.boneIndex3,weight.weight3);candidatePoints[index]=point;error+=(point-original.world[index]).sqrMagnitude;
                void Add(int bone,float amount){if(amount>0)point+=matrices[bone].MultiplyPoint3x4(bindposes[bone].MultiplyPoint3x4(sourcePoints[index]))*amount;}
            }
            if(degrees==0)report.zeroOffsetTrueBakeRmse=(float)Math.Sqrt(error/sourcePoints.Length);
            var bounds=new Bounds(candidatePoints[0],Vector3.zero);foreach(var point in candidatePoints)bounds.Encapsulate(point);var candidateSkin=new Skin{slot=original.slot,renderer=original.renderer,world=candidatePoints,triangles=original.triangles,weights=original.weights,bones=original.bones,bounds=bounds,normals=Array.Empty<Vector3>(),source=original.source,actualRenderer=original.actualRenderer};var candidateSkins=skins.Select(value=>ReferenceEquals(value,original)?candidateSkin:value).ToList();
            report.actualSupportPoints=anchors.Select(anchor=>SupportPoint(anchor,original.world)).ToArray();report.candidateSupportPoints=anchors.Select(anchor=>SupportPoint(anchor,candidatePoints)).ToArray();for(int index=0;index<5;index++)report.maximumSupportPointDisplacement=Mathf.Max(report.maximumSupportPointDisplacement,Vector3.Distance(report.actualSupportPoints[index],report.candidateSupportPoints[index]));
            var intersections=InspectCompleteSkin(candidatePoints);
            report.triangleBoxIntersections=intersections.Count;report.intersections=intersections.ToArray();report.sameFrameCabRays=row.sameFrameVirtualCabCandidates.Where(value=>value.name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL"||value.name.StartsWith("SOURCE_",StringComparison.Ordinal)).Select(value=>
            {
                var rays=value.centralWindscreenRays.Select(ray=>TraceActualSkin(new Ray(ray.origin,ray.direction),ray.viewport,row.owner,candidateSkins)).ToArray();return new VirtualCabPose{name=value.name,eye=value.eye,forward=value.forward,up=value.up,localTruckEye=value.localTruckEye,angleToActualTruckForward=value.angleToActualTruckForward,centralWindscreenRays=rays,anySkinHits=rays.Count(hit=>hit.hitSlot>=0),ownSkinHits=rays.Count(hit=>hit.ownDragon),ownHeadNeckDominantHits=rays.Count(hit=>hit.ownDragon&&hit.headNeckWeight>=.5f)};
            }).ToArray();
            reports.Add(report);row.pureConnectedNeckCandidates=reports.ToArray();
            if(!report.nonSelectedWorldMatricesExact||posture.preserveAim&&(report.headWorldAimError>.5f||report.mouthWorldAimError>.5f)||!posture.preserveAim&&(report.headWorldRotationChange>degrees+.5f||report.mouthWorldAimError>degrees+.5f)||report.originalHeadLocalPoseExpectedUnchanged&&report.maximumOriginalHeadLocalPoseError>.0002f||report.maximumChangedLocalTranslation>.0002f||report.maximumUnchangedLocalMatrixError>.0002f||degrees==0&&report.zeroOffsetTrueBakeRmse>=.0001f)throw new InvalidOperationException("Pure connected-neck prediction violated its original matrix/translation/world-aim/zero-bake control; do not select a posture.");
        }
        return reports.ToArray();
        List<TriangleBoxIntersection> InspectCompleteSkin(Vector3[] worldPoints)
        {
            var intersections=new List<TriangleBoxIntersection>();foreach(var box in boxes)
            {
                var local=worldPoints.Select(point=>box.transform.InverseTransformPoint(point)-box.center).ToArray();for(int index=0;index<original.triangles.Length;index+=3)
                {
                    int a=original.triangles[index],b=original.triangles[index+1],c=original.triangles[index+2];if(!TriangleBoxIntersects(local[a],local[b],local[c],box.size*.5f))continue;var average=new Dictionary<string,float>();Accumulate(original,a,1f/3,average);Accumulate(original,b,1f/3,average);Accumulate(original,c,1f/3,average);
                    intersections.Add(new TriangleBoxIntersection{collider=box.name,triangle=index/3,a=a,b=b,c=c,worldA=worldPoints[a],worldB=worldPoints[b],worldC=worldPoints[c],boxLocalA=local[a],boxLocalB=local[b],boxLocalC=local[c],boxSize=box.size,boxCenter=box.center,actualOriginalBoxLocalToWorld=box.transform.localToWorldMatrix,originalVertexAWeights=VertexWeights(a),originalVertexBWeights=VertexWeights(b),originalVertexCWeights=VertexWeights(c),averageOriginalBoneWeights=WeightsText(average),averageHeadNeckWeight=average.Where(pair=>HeadNeck(pair.Key)).Sum(pair=>pair.Value),averageWingWeight=average.Where(pair=>pair.Key.IndexOf("wing",StringComparison.OrdinalIgnoreCase)>=0).Sum(pair=>pair.Value),averageTailWeight=average.Where(pair=>pair.Key.IndexOf("tail",StringComparison.OrdinalIgnoreCase)>=0).Sum(pair=>pair.Value)});
                }
            }
            return intersections;
        }
        string VertexWeights(int vertex){var values=new Dictionary<string,float>();Accumulate(original,vertex,1,values);return WeightsText(values);}
        string WeightsText(Dictionary<string,float> values)=>string.Join(";",values.OrderByDescending(pair=>pair.Value).Select(pair=>pair.Key+"="+pair.Value.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        Vector3 SupportPoint(object anchor,Vector3[] points){var weights=(Vector3)Read(anchor,"barycentric");return points[(int)Read(anchor,"a")]*weights.x+points[(int)Read(anchor,"b")]*weights.y+points[(int)Read(anchor,"c")]*weights.z;}
    }
    // Same thirteen separating axes as the previously controlled complete-tail
    // native oracle; all candidate triangles are tested, with no bone filtering.
    static bool TriangleBoxIntersects(Vector3 a,Vector3 b,Vector3 c,Vector3 half)
    {
        Vector3[] boxAxes={Vector3.right,Vector3.up,Vector3.forward},edges={b-a,c-b,a-c};var axes=new List<Vector3>(boxAxes){Vector3.Cross(edges[0],edges[1])};foreach(var edge in edges)foreach(var axis in boxAxes)axes.Add(Vector3.Cross(edge,axis));foreach(var raw in axes){if(raw.sqrMagnitude<1e-14f)continue;var axis=raw.normalized;float pa=Vector3.Dot(a,axis),pb=Vector3.Dot(b,axis),pc=Vector3.Dot(c,axis),radius=half.x*Mathf.Abs(axis.x)+half.y*Mathf.Abs(axis.y)+half.z*Mathf.Abs(axis.z);if(Mathf.Min(pa,Mathf.Min(pb,pc))>radius||Mathf.Max(pa,Mathf.Max(pb,pc))< -radius)return false;}return true;
    }
    static CabMeshRegistration[] RegisterCanonicalCabMesh(Component truck,Camera actualCamera)
    {
        const string path="Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab";var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Actual canonical cab source unavailable; do not substitute an estimated eye anchor.");
        var driver=source.GetComponentsInChildren<Camera>(true).Single(value=>value.name=="Vehicle Camera Driver");var host=truck.GetComponents<Component>().FirstOrDefault(value=>value!=null&&value.GetType().FullName=="LWS.TruckSports.SportsTruckAppearanceHost");if(host==null)return Array.Empty<CabMeshRegistration>();
        var composed=((System.Collections.IEnumerable)Read(host,"ComposedBodyRenderers")).Cast<Renderer>().Where(value=>value!=null).ToArray();var results=new List<CabMeshRegistration>();
        foreach(var sourceFilter in source.GetComponentsInChildren<MeshFilter>(true).Where(value=>value.name=="Cab"||value.name=="interior"))
        foreach(var liveRenderer in composed)
        {
            var liveFilter=liveRenderer.GetComponent<MeshFilter>();if(liveFilter==null||sourceFilter.sharedMesh==null||!ReferenceEquals(sourceFilter.sharedMesh,liveFilter.sharedMesh))continue;
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceFilter.sharedMesh,out string guid,out long localId);var meshEye=sourceFilter.transform.InverseTransformPoint(driver.transform.position);var mappedEye=liveFilter.transform.TransformPoint(meshEye);
            var mappedForward=liveFilter.transform.TransformVector(sourceFilter.transform.InverseTransformVector(driver.transform.forward)).normalized;var mappedUp=liveFilter.transform.TransformVector(sourceFilter.transform.InverseTransformVector(driver.transform.up)).normalized;
            results.Add(new CabMeshRegistration{canonicalPrefab=path,sourceFilter=sourceFilter.name,liveFilter=liveFilter.name,sourceMeshGuid=guid,sourceMeshLocalId=localId.ToString(),appearanceBodyId=Read(host,"EffectiveBodyId").ToString(),exactSharedMeshIdentity=true,sourceDriverEyeRootLocal=source.transform.InverseTransformPoint(driver.transform.position),sourceDriverEyeInMesh=meshEye,mappedDriverEyeWorld=mappedEye,mappedDriverEyeTruckLocal=truck.transform.InverseTransformPoint(mappedEye),mappedDriverForward=mappedForward,mappedDriverUp=mappedUp,sourceMeshScale=sourceFilter.transform.lossyScale,liveMeshScale=liveFilter.transform.lossyScale,actualEyeMinusRegisteredEye=actualCamera.transform.position-mappedEye,sourceMeshLocalToWorld=sourceFilter.transform.localToWorldMatrix,liveMeshLocalToWorld=liveFilter.transform.localToWorldMatrix,sourceMeshBounds=sourceFilter.sharedMesh.bounds,liveRendererWorldBounds=liveRenderer.bounds});
        }
        return results.ToArray();
    }
    static RayWitness TraceActualSkin(Ray ray,Vector2 uv,int owner,List<Skin> skins)
    {
        var hit=new RayWitness{viewport=uv,origin=ray.origin,direction=ray.direction,distance=150};
        foreach(var skin in skins)
        {
            if(!skin.bounds.IntersectRay(ray))continue;
            for(int tri=0;tri<skin.triangles.Length;tri+=3)
            {
                int a=skin.triangles[tri],b=skin.triangles[tri+1],c=skin.triangles[tri+2];var normal=Vector3.Cross(skin.world[b]-skin.world[a],skin.world[c]-skin.world[a]).normalized;
                if(!TriangleHit(ray,skin.world[a],skin.world[b],skin.world[c],out float distance,out float u,out float v)||distance>=hit.distance)continue;
                hit.hitSlot=skin.slot;hit.ownDragon=skin.slot==owner;hit.renderer=skin.renderer;hit.triangle=tri/3;hit.distance=distance;hit.worldPoint=ray.GetPoint(distance);hit.normalDotRay=Vector3.Dot(normal,ray.direction);hit.frontFacing=hit.normalDotRay<0;
                if(skin.normals.Length==skin.world.Length){var smooth=(skin.normals[a]*(1-u-v)+skin.normals[b]*u+skin.normals[c]*v).normalized;hit.smoothNormalDotRay=Vector3.Dot(smooth,ray.direction);hit.geometricNormalDotSmooth=Vector3.Dot(normal,smooth);}
                var contributions=new Dictionary<string,float>();Accumulate(skin,a,1-u-v,contributions);Accumulate(skin,b,u,contributions);Accumulate(skin,c,v,contributions);hit.headNeckWeight=contributions.Where(pair=>HeadNeck(pair.Key)).Sum(pair=>pair.Value);hit.boneWeights=string.Join(";",contributions.OrderByDescending(pair=>pair.Value).Select(pair=>pair.Key+"="+pair.Value.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
            }
        }
        return hit;
    }
    static bool TriangleHit(Ray ray,Vector3 a,Vector3 b,Vector3 c,out float distance,out float u,out float v)
    {
        distance=u=v=0;var e1=b-a;var e2=c-a;var cross=Vector3.Cross(ray.direction,e2);float determinant=Vector3.Dot(e1,cross);if(Mathf.Abs(determinant)<1e-7f)return false;float inverse=1/determinant;var delta=ray.origin-a;u=Vector3.Dot(delta,cross)*inverse;if(u<0||u>1)return false;var q=Vector3.Cross(delta,e1);v=Vector3.Dot(ray.direction,q)*inverse;if(v<0||u+v>1)return false;distance=Vector3.Dot(e2,q)*inverse;return distance>=0;
    }
    static void VerifyRayControls()
    {
        var a=new Vector3(-1,-1,5);var b=new Vector3(0,1,5);var c=new Vector3(1,-1,5);
        if(!TriangleHit(new Ray(Vector3.zero,Vector3.forward),a,b,c,out float front,out _,out _)||Mathf.Abs(front-5)>.0001f||!TriangleHit(new Ray(Vector3.forward*10,Vector3.back),a,b,c,out float back,out _,out _)||Mathf.Abs(back-5)>.0001f||TriangleHit(new Ray(Vector3.right*4,Vector3.forward),a,b,c,out _,out _,out _)||TriangleHit(new Ray(Vector3.zero,Vector3.back),a,b,c,out _,out _,out _))throw new InvalidOperationException("Read-only cab two-sided ray sampler failed its known inside/outside/behind controls.");
    }
    static bool HeadNeck(string name)=>name.IndexOf("head",StringComparison.OrdinalIgnoreCase)>=0||name.IndexOf("neck",StringComparison.OrdinalIgnoreCase)>=0;
    static void Accumulate(Skin skin,int vertex,float factor,Dictionary<string,float> result)
    {
        if(skin.weights.Length!=skin.world.Length)return;var w=skin.weights[vertex];Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);
        void Add(int index,float value){if(value<=0||index<0||index>=skin.bones.Length||skin.bones[index]==null)return;string name=skin.bones[index].name;result.TryGetValue(name,out float old);result[name]=old+value*factor;}
    }
    static Tuple<float,float> PureWeightedVertexReference(Transform model,SkinnedMeshRenderer skin,Vector3[] selectedWorld,Vector3[] falseWorld)
    {
        Vector3[] original;using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(skin.sharedMesh))using(var points=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp)){data[0].GetVertices(points);original=points.ToArray();}
        var weights=skin.sharedMesh.boneWeights;var poses=skin.sharedMesh.bindposes;var bones=skin.bones;if(original.Length!=weights.Length||original.Length!=selectedWorld.Length||original.Length!=falseWorld.Length)throw new InvalidOperationException("Original cab skin vertex/weight/bake counts differ; do not invent a geometry proof.");
        var inverse=model.worldToLocalMatrix;double selected=0,unscaled=0;for(int index=0;index<original.Length;index++)
        {
            var w=weights[index];Vector3 expected=Vector3.zero;Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);var reference=inverse.MultiplyPoint3x4(expected);selected+=(reference-inverse.MultiplyPoint3x4(selectedWorld[index])).sqrMagnitude;unscaled+=(reference-inverse.MultiplyPoint3x4(falseWorld[index])).sqrMagnitude;
            void Add(int bone,float amount){if(amount<=0)return;if(bone<0||bone>=poses.Length||bone>=bones.Length||bones[bone]==null)throw new InvalidOperationException("Original cab weighted bone reference missing.");expected+=bones[bone].localToWorldMatrix.MultiplyPoint3x4(poses[bone].MultiplyPoint3x4(original[index]))*amount;}
        }
        return Tuple.Create((float)Math.Sqrt(selected/original.Length),(float)Math.Sqrt(unscaled/original.Length));
    }
    static string Hash(string text){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");}
    string SnapshotOwnedState()
    {
        var text=new StringBuilder();void F(float value)=>text.Append(BitConverter.SingleToInt32Bits(value)).Append(',');void V(Vector3 value){F(value.x);F(value.y);F(value.z);}void Q(Quaternion value){F(value.x);F(value.y);F(value.z);F(value.w);}
        text.Append("frame=").Append(Time.frameCount).Append(" time=");F(Time.time);F(Time.fixedTime);F(Time.timeScale);F(Time.fixedDeltaTime);text.Append(" physics=").Append(Physics.simulationMode).Append('/').Append(Physics.autoSyncTransforms).Append('/').Append(Physics.queriesHitTriggers).Append('/').Append(Physics.queriesHitBackfaces).Append('/').Append(Physics.reuseCollisionCallbacks).Append('/').Append(Physics.defaultSolverIterations).Append('/').Append(Physics.defaultSolverVelocityIterations);V(Physics.gravity);F(Physics.bounceThreshold);F(Physics.defaultContactOffset);F(Physics.sleepThreshold);for(int a=0;a<32;a++)for(int b=a;b<32;b++)text.Append(Physics.GetIgnoreLayerCollision(a,b)?'1':'0');text.AppendLine();
        var roots=new List<Component>();foreach(var racer in ((Array)Read(match,"racers")).Cast<object>())foreach(var actor in ((Array)Read(racer,"choices")).Cast<Component>())if(actor!=null&&!roots.Contains(actor))roots.Add(actor);foreach(var truck in ((Array)Read(match,"trucks")).Cast<Component>())if(truck!=null&&!roots.Contains(truck))roots.Add(truck);
        foreach(var root in roots)
        {
            text.Append("root:").Append(root.GetEntityId()).Append('/').Append(root.gameObject.activeSelf).Append('/').Append(root is Behaviour behaviour&&behaviour.enabled).AppendLine();foreach(var transform in root.GetComponentsInChildren<Transform>(true)){text.Append("T:").Append(transform.GetEntityId()).Append('/').Append(transform.gameObject.activeSelf).Append('/').Append(transform.hasChanged).Append(':');V(transform.localPosition);Q(transform.localRotation);V(transform.localScale);V(transform.position);Q(transform.rotation);text.AppendLine();}
            foreach(var body in root.GetComponentsInChildren<Rigidbody>(true)){text.Append("B:").Append(body.GetEntityId()).Append('/').Append(body.isKinematic).Append('/').Append(body.useGravity).Append('/').Append(body.detectCollisions).Append('/').Append(body.constraints).Append('/').Append(body.interpolation).Append('/').Append(body.collisionDetectionMode).Append('/').Append(body.IsSleeping()).Append(':');V(body.position);Q(body.rotation);V(body.linearVelocity);V(body.angularVelocity);F(body.mass);V(body.centerOfMass);V(body.inertiaTensor);Q(body.inertiaTensorRotation);text.AppendLine();}
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))text.Append("C:").Append(collider.GetEntityId()).Append('/').Append(collider.enabled).Append('/').Append(collider.isTrigger).Append('/').Append(collider.gameObject.layer).Append('/').Append(collider.contactOffset.ToString("R")).AppendLine();
            foreach(var animator in root.GetComponentsInChildren<Animator>(true))
            {
                text.Append("A:").Append(animator.GetEntityId()).Append('/').Append(animator.enabled).Append('/').Append(animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.GetEntityId().ToString():"none").Append('/').Append(animator.updateMode).Append('/').Append(animator.cullingMode).Append('/').Append(animator.applyRootMotion).Append('/').Append(animator.isInitialized).Append(':');F(animator.speed);if(animator.isInitialized){V(animator.rootPosition);Q(animator.rootRotation);for(int layer=0;layer<animator.layerCount;layer++){var current=animator.GetCurrentAnimatorStateInfo(layer);var next=animator.GetNextAnimatorStateInfo(layer);text.Append(current.fullPathHash).Append('/').Append(next.fullPathHash).Append('/').Append(animator.IsInTransition(layer)).Append(':');F(current.normalizedTime);F(next.normalizedTime);F(animator.GetLayerWeight(layer));foreach(var value in animator.GetCurrentAnimatorClipInfo(layer).Concat(animator.GetNextAnimatorClipInfo(layer))){text.Append(value.clip.GetEntityId()).Append(':');F(value.weight);}}foreach(var parameter in animator.parameters){text.Append(parameter.nameHash).Append(':');if(parameter.type==AnimatorControllerParameterType.Float)F(animator.GetFloat(parameter.nameHash));else if(parameter.type==AnimatorControllerParameterType.Int)text.Append(animator.GetInteger(parameter.nameHash));else if(parameter.type==AnimatorControllerParameterType.Bool)text.Append(animator.GetBool(parameter.nameHash));}}text.AppendLine();
            }
            if(root.GetType().Name=="DragonCharacter"){var motor=Read(root,"motor");var animal=Read(motor,"animal");text.Append("N:").Append(Read(root,"Paused")).Append('/').Append(Read(motor,"Paused"));foreach(string member in new[]{"Grounded","Sleep","LockMovement","LockInput","DisablePosition","DisableRotation","IsPlayingMode","IsPreparingMode"})text.Append('/').Append(member).Append('=').Append(Read(animal,member));text.Append('/').Append(Read(animal,"ActiveState")?.GetType().Name??"none").AppendLine();}
        }
        return text.ToString();
    }
    List<Skin> ReadActualSkins(Camera camera)
    {
        var result=new List<Skin>();var racers=(Array)Read(match,"racers");for(int slot=0;slot<racers.Length;slot++)
        {
            var actor=Read(racers.GetValue(slot),"Dragon") as Component;if(actor==null)continue;var motor=(Component)Read(actor,"motor");foreach(var renderer in motor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.forceRenderingOff||renderer.sharedMesh==null||(camera.cullingMask&(1<<renderer.gameObject.layer))==0)continue;var mesh=new Mesh();var falseMesh=new Mesh();try
                {
                    renderer.BakeMesh(falseMesh,false);renderer.BakeMesh(mesh,true);var world=mesh.vertices;var normalMatrix=renderer.transform.localToWorldMatrix.inverse.transpose;var normals=mesh.normals;for(int i=0;i<world.Length;i++){world[i]=renderer.transform.TransformPoint(world[i]);if(normals.Length==world.Length)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;}if(world.Length==0)continue;var bounds=new Bounds(world[0],Vector3.zero);foreach(var point in world)bounds.Encapsulate(point);var falseWorld=falseMesh.vertices.Select(renderer.transform.TransformPoint).ToArray();var falseBounds=new Bounds(falseWorld[0],Vector3.zero);foreach(var point in falseWorld)falseBounds.Encapsulate(point);
                    var contract=PureWeightedVertexReference(motor.transform,renderer,world,falseWorld);float trueRmse=contract.Item1,falseRmse=contract.Item2;string skinError=null;if(trueRmse>=.0001f)throw new InvalidOperationException("Actual cab skin source contract failed its pure original imported vertices/bindposes/bones check: trueFullRmse="+trueRmse);var weights=renderer.sharedMesh.boneWeights;var bones=renderer.bones;var marked=bones.Select(bone=>bone!=null&&HeadNeck(bone.name)).ToArray();bool Marked(int index)=>index>=0&&index<marked.Length&&marked[index];int dominant=0;foreach(var w in weights)if((Marked(w.boneIndex0)?w.weight0:0)+(Marked(w.boneIndex1)?w.weight1:0)+(Marked(w.boneIndex2)?w.weight2:0)+(Marked(w.boneIndex3)?w.weight3:0)>=.5f)dominant++;UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.sharedMesh,out string guid,out long local);
                    float actualForward=0,actualReverse=0;var triangles=mesh.triangles;for(int tri=0;tri<triangles.Length;tri+=3){var a=world[triangles[tri]];var b=world[triangles[tri+1]];var c=world[triangles[tri+2]];var n=Vector3.Cross(b-a,c-a);if(n.sqrMagnitude<.000001f)continue;n.Normalize();var center=(a+b+c)/3;if(!TriangleHit(new Ray(center+n,-n),a,b,c,out actualForward,out _,out _)||!TriangleHit(new Ray(center-n,n),a,b,c,out actualReverse,out _,out _)||Mathf.Abs(actualForward-1)>.001f||Mathf.Abs(actualReverse-1)>.001f)throw new InvalidOperationException("Actual posed cab skin triangle failed its independent two-sided center-ray controls.");break;}
                    var source=new SkinSource{selectedTrueWorldBounds=bounds,falseWorldBounds=falseBounds,actualRendererWorldBounds=renderer.bounds,rendererMatrixDeterminant=renderer.transform.localToWorldMatrix.determinant,trueFullMatrixRmse=trueRmse,falseFullMatrixRmse=falseRmse,independentSkinningError=skinError,actualTriangleForwardDistance=actualForward,actualTriangleReverseDistance=actualReverse,slot=slot,renderer=renderer.name,sourceMesh=UnityEditor.AssetDatabase.GetAssetPath(renderer.sharedMesh)+"/"+renderer.sharedMesh.name,sourceGuid=guid,sourceLocalId=local.ToString(),vertexCount=world.Length,triangleCount=mesh.triangles.Length/3,submeshCount=renderer.sharedMesh.subMeshCount,headNeckDominantVertices=dominant,headNeckBones=bones.Where(bone=>bone!=null&&HeadNeck(bone.name)).Select(bone=>bone.name).ToArray(),materials=renderer.sharedMaterials.Select(material=>material==null?"null":material.name+"/"+material.shader.name+"/Cull="+(material.HasProperty("_Cull")?material.GetFloat("_Cull").ToString():"unspecified")).ToArray()};result.Add(new Skin{slot=slot,renderer=renderer.name,world=world,triangles=mesh.triangles,weights=weights,bones=bones,bounds=bounds,normals=normals,source=source,actualRenderer=renderer});
                }
                finally{UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(falseMesh);}
            }
        }
        return result;
    }
    void Write(){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(new Receipt{views=frames},true)+"\n");}
    public void Dispose(){RenderPipelineManager.beginCameraRendering-=Rendering;Write();}
}
#endif
