using System;
using System.Collections.Generic;
using LWS.TruckDragonKingdom;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    [Serializable]
    public sealed class DragonRiderSurfaceAnchor
    {
        public int a,b,c;
        public Vector3 barycentric;
    }
    // Mode-local semantic attachment over the actual animated dorsal bone.
    // The authored socket removes the imported bone's intrinsic axes; no runtime
    // frame is captured from an unevaluated Animator or a previous match pose.
    [DefaultExecutionOrder(10)]
    public sealed class DragonRiderSaddle : MonoBehaviour
    {
        public const string SocketName="LWS Riders calibrated dorsal socket";
        public DragonCharacter actor;
        public Transform sourceBone, socket, supportPresentation;
        public AnimationClip neutralClip;
        public string speciesId;
        public int calibrationVersion, sampledSkinSurfaces;
        public float dorsalClearance=.45f;
        public Vector3 supportMotorLocalPosition;
        public float measuredDorsalMinimum,measuredDorsalMaximum;
        public SkinnedMeshRenderer surfaceRenderer;
        public DragonRiderSaddleRigging rigging;
        public int surfaceVertexA,surfaceVertexB,surfaceVertexC;
        public Vector3 surfaceBarycentric;
        // Center, right, left, forward, rear: the same five actual torso
        // material points verified by the explicit authoring ray patch.
        public DragonRiderSurfaceAnchor[] surfacePatchAnchors;
        [Range(0,35)]public float maximumBank=25;
        [Range(0,35)]public float maximumPitch=20;
        [Range(0,25)]public float maximumYaw=15;
        public float CurrentBank {get;private set;}
        public float CurrentPitch {get;private set;}
        Pose physicalPose;
        Mesh skinSnapshot;
        readonly List<Vector3> skinVertices=new List<Vector3>();
        public Vector3 ActualSurfacePoint {get;private set;}
        public Vector3 ActualPatchNormal {get;private set;}
        bool sampled;
        public void Validate(DragonCharacter selected)
        {
            if(actor!=selected||actor==null||actor.motor==null||sourceBone==null||socket==null||socket.parent!=sourceBone||
                supportPresentation==null||neutralClip==null||calibrationVersion!=7||sampledSkinSurfaces<3||dorsalClearance<.2f||
                surfaceRenderer==null||surfaceRenderer.sharedMesh==null||surfaceVertexA<0||surfaceVertexB<0||surfaceVertexC<0||
                Mathf.Abs(surfaceBarycentric.x+surfaceBarycentric.y+surfaceBarycentric.z-1)>.0001f||surfacePatchAnchors==null||surfacePatchAnchors.Length!=5)
                throw new InvalidOperationException("Explicit actual-rig Riders saddle calibration required before mounting the original NWH crew.");
            foreach(var point in surfacePatchAnchors)if(point==null||point.a<0||point.b<0||point.c<0||Mathf.Abs(point.barycentric.x+point.barycentric.y+point.barycentric.z-1)>.0001f)
                throw new InvalidOperationException("Actual five-point anatomical saddle patch is invalid.");
        }
        public void CaptureEvaluatedAnimationPose()
        {
            if(skinSnapshot==null){skinSnapshot=new Mesh{name="Riders current anatomical anchor snapshot"};skinSnapshot.MarkDynamic();}
            surfaceRenderer.BakeMesh(skinSnapshot,true);skinVertices.Clear();skinSnapshot.GetVertices(skinVertices);
            if(surfaceVertexA>=skinVertices.Count||surfaceVertexB>=skinVertices.Count||surfaceVertexC>=skinVertices.Count)throw new InvalidOperationException("Actual calibrated dorsal vertex identity changed.");
            var matrix=surfaceRenderer.transform.localToWorldMatrix;
            Vector3 a=matrix.MultiplyPoint3x4(skinVertices[surfaceVertexA]),b=matrix.MultiplyPoint3x4(skinVertices[surfaceVertexB]),c=matrix.MultiplyPoint3x4(skinVertices[surfaceVertexC]);
            ActualSurfacePoint=a*surfaceBarycentric.x+b*surfaceBarycentric.y+c*surfaceBarycentric.z;
            // A single one-sided low-poly face is the curved skin, not the
            // platform's whole anatomical plane. V166 showed its neutral
            // curvature bias clamping bank/pitch before any Animator callback.
            // Use the verified bilateral/longitudinal patch deformed this frame.
            Vector3 rightPoint=ReadAnchor(surfacePatchAnchors[1],matrix),leftPoint=ReadAnchor(surfacePatchAnchors[2],matrix);
            Vector3 frontPoint=ReadAnchor(surfacePatchAnchors[3],matrix),rearPoint=ReadAnchor(surfacePatchAnchors[4],matrix);
            Vector3 normal=Vector3.Cross(frontPoint-rearPoint,rightPoint-leftPoint).normalized;ActualPatchNormal=normal;
            if(Vector3.Dot(normal,actor.motor.transform.up)<.1f)
            {
                var clips=new List<string>();var animator=actor.motor.animator;
                for(int layer=0;layer<animator.layerCount;layer++)
                {
                    foreach(var info in animator.GetCurrentAnimatorClipInfo(layer))if(info.clip!=null&&info.weight>.01f)clips.Add("layer"+layer+" current="+info.clip.name+" weight="+info.weight+" normalized="+animator.GetCurrentAnimatorStateInfo(layer).normalizedTime);
                    foreach(var info in animator.GetNextAnimatorClipInfo(layer))if(info.clip!=null&&info.weight>.01f)clips.Add("layer"+layer+" next="+info.clip.name+" weight="+info.weight+" normalized="+animator.GetNextAnimatorStateInfo(layer).normalizedTime);
                }
                throw new InvalidOperationException("The actual anatomical saddle surface no longer faces upward. actor="+actor.name+" species="+speciesId+" frame="+Time.frameCount+" normal="+normal.ToString("F5")+" motorUp="+actor.motor.transform.up.ToString("F5")+" dot="+Vector3.Dot(normal,actor.motor.transform.up)+" motorEuler="+actor.motor.transform.eulerAngles.ToString("F3")+" action="+actor.motor.CurrentAction+" actionPlaying="+actor.motor.ActionPlaying+" actualClips=["+string.Join(";",clips)+"]");
            }
            Quaternion body=actor.motor.transform.rotation;
            Vector3 forward=Vector3.ProjectOnPlane(actor.motor.transform.forward,normal).normalized;
            if(forward.sqrMagnitude<.01f)throw new InvalidOperationException("Actual anatomical forward/support plane is invalid.");
            Vector3 actual=(Quaternion.Inverse(body)*Quaternion.LookRotation(forward,normal)).eulerAngles;
            CurrentPitch=Mathf.Clamp(Mathf.DeltaAngle(0,actual.x),-maximumPitch,maximumPitch);
            CurrentBank=Mathf.Clamp(Mathf.DeltaAngle(0,actual.z),-maximumBank,maximumBank);
            float yaw=Mathf.Clamp(Mathf.DeltaAngle(0,actual.y),-maximumYaw,maximumYaw);
            Quaternion orientation=body*Quaternion.Euler(CurrentPitch,yaw,CurrentBank);
            physicalPose=new Pose(ActualSurfacePoint+orientation*Vector3.up*dorsalClearance,orientation);sampled=true;
            // The racer evaluates this final animated surface after this
            // component's LateUpdate. Present the deck from that same pose,
            // rather than leaving it on the previous frame's support cache.
            if(supportPresentation!=null)supportPresentation.SetPositionAndRotation(physicalPose.position,physicalPose.rotation);
        }
        Vector3 ReadAnchor(DragonRiderSurfaceAnchor point,Matrix4x4 matrix)
        {
            if(point.a>=skinVertices.Count||point.b>=skinVertices.Count||point.c>=skinVertices.Count)throw new InvalidOperationException("Actual calibrated dorsal patch vertex identity changed.");
            return matrix.MultiplyPoint3x4(skinVertices[point.a]*point.barycentric.x+skinVertices[point.b]*point.barycentric.y+skinVertices[point.c]*point.barycentric.z);
        }
        public Vector3 ReadActualSkinVertexWorld(int index)
        {
            if(!sampled||index<0||index>=skinVertices.Count)throw new InvalidOperationException("Actual evaluated skin snapshot required for authored visual harness.");return surfaceRenderer.transform.TransformPoint(skinVertices[index]);
        }
        public Pose ReadPose()
        {
            if(!sampled)throw new InvalidOperationException("Prepare the physical saddle frame from an actual evaluated rig before mounting.");
            return physicalPose;
        }
        void LateUpdate()
        {
            if(actor==null||!actor.Prepared||actor.motor==null||!actor.motor.Prepared||socket==null||supportPresentation==null)return;
            var pose=ReadPose();supportPresentation.SetPositionAndRotation(pose.position,pose.rotation);
        }
        public void ReleaseGeometrySnapshot()
        {if(skinSnapshot!=null){if(Application.isPlaying)Destroy(skinSnapshot);else DestroyImmediate(skinSnapshot);}skinSnapshot=null;skinVertices.Clear();}
        void OnDisable(){ReleaseGeometrySnapshot();sampled=false;}
        void OnDestroy()=>ReleaseGeometrySnapshot();
    }
}
