#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]public sealed class RiderTruckCrossingCenterSample
        {
            public string pose;public Vector3 authoredLocalCenter,currentMatrixCenter,physicsBoundsCenter,physicsBoundsSize;
            public Matrix4x4 currentLocalToWorld;public float centerDelta;public bool shapeEnabled,shapeActiveSelf,shapeActiveInHierarchy,physicsBoundsEmpty,hasAttachedBody,bodyDetectCollisions;
        }
        public static RiderTruckCrossingCenterSample MeasureCurrentRiderTruckCrossingCenter(BoxCollider shape,string pose)
        {
            if(shape==null)throw new InvalidOperationException("Actual existing truck BoxCollider required; no invented reference center.");
            var matrix=shape.transform.localToWorldMatrix;var current=matrix.MultiplyPoint3x4(shape.center);var bounds=shape.bounds;
            for(int i=0;i<16;i++)if(!float.IsFinite(matrix[i]))throw new InvalidOperationException("Finite current source BoxCollider transform required.");
            if(!float.IsFinite(current.x)||!float.IsFinite(current.y)||!float.IsFinite(current.z))throw new InvalidOperationException("Finite transformed authored source box center required.");
            return new RiderTruckCrossingCenterSample{pose=pose,authoredLocalCenter=shape.center,currentLocalToWorld=matrix,currentMatrixCenter=current,physicsBoundsCenter=bounds.center,physicsBoundsSize=bounds.size,centerDelta=Vector3.Distance(current,bounds.center),shapeEnabled=shape.enabled,shapeActiveSelf=shape.gameObject.activeSelf,shapeActiveInHierarchy=shape.gameObject.activeInHierarchy,physicsBoundsEmpty=bounds.size.Equals(Vector3.zero),hasAttachedBody=shape.attachedRigidbody!=null,bodyDetectCollisions=shape.attachedRigidbody!=null&&shape.attachedRigidbody.detectCollisions};
        }
        [Serializable]sealed class ActualMountedCourseEnvelope
        {
            public string scope="ALL699_ACTUAL_REFERENCED_SKIN_AND_ORIGINAL_NWH_HARNESS_POINTS_RELATIVE_CURRENT_AUTHORED_TRUCK_BOX_CENTER_NOT_AABB_CORNER_RADIUS_NOT_COURSE_OR_RUNTIME_ACCEPTANCE";
            public int poses,skinPoints,nwhMeshPoints,nwhBoxCorners,rigidMountPoints,harnessLineSamples;
            public string crossingCenterAuthority="CURRENT_AUTHORED_BOX_CENTER_TRANSFORMED_BY_CURRENT_LOCAL_TO_WORLD_NOT_PHYSX_CACHE";
            public int physicsBoundsNonmatchingSamples,physicsBoundsEmptySamples,disabledShapeSamples,inactiveShapeSamples;public float maximumPhysicsCenterDelta;
            public List<RiderTruckCrossingCenterSample> crossingCenters=new List<RiderTruckCrossingCenterSample>();
            public float maximumNominalGateRadialExtent,maximumTruckFrameRadialExtent,maximumAnyAttitudeVertexRadius,maximumForwardReach,maximumRearReach,maximumUpReach,maximumDownReach;
            public Bounds actorHeadingRelativeBounds;public bool hasBounds;public string radialLimitingPose,sphericalLimitingPose;
            public float sourceRingInnerAperture,sourceRingOuterRadius,sourceRingDepth,proposedClearRadiusNominal,proposedClearRadiusAnyAttitude;
            public string gateRadiusScope="MEASURED_DIMENSIONS_FOR_EXPLICIT_COURSE_DESIGN_NOT_FIXED_USER_REQUIREMENT_OR_PHYSICAL_FLIGHT_ACCEPTANCE";
        }
        static void ActualEnvelopePoint(ActualMountedCourseEnvelope envelope,Vector3 world,Vector3 crossingCenter,Transform model,Transform truck,string pose,float worldPadding=0)
        {
            Vector3 delta=world-crossingCenter,heading=Quaternion.Inverse(model.rotation)*delta,truckLocal=Quaternion.Inverse(truck.rotation)*delta;float radial=new Vector2(heading.x,heading.y).magnitude+worldPadding;float sphere=delta.magnitude+worldPadding;
            if(!envelope.hasBounds){envelope.actorHeadingRelativeBounds=new Bounds(heading,Vector3.one*(worldPadding*2));envelope.hasBounds=true;}else envelope.actorHeadingRelativeBounds.Encapsulate(new Bounds(heading,Vector3.one*(worldPadding*2)));
            if(radial>envelope.maximumNominalGateRadialExtent){envelope.maximumNominalGateRadialExtent=radial;envelope.radialLimitingPose=pose;}if(sphere>envelope.maximumAnyAttitudeVertexRadius){envelope.maximumAnyAttitudeVertexRadius=sphere;envelope.sphericalLimitingPose=pose;}
            envelope.maximumTruckFrameRadialExtent=Mathf.Max(envelope.maximumTruckFrameRadialExtent,new Vector2(truckLocal.x,truckLocal.y).magnitude+worldPadding);envelope.maximumForwardReach=Mathf.Max(envelope.maximumForwardReach,heading.z+worldPadding);envelope.maximumRearReach=Mathf.Max(envelope.maximumRearReach,-heading.z+worldPadding);envelope.maximumUpReach=Mathf.Max(envelope.maximumUpReach,heading.y+worldPadding);envelope.maximumDownReach=Mathf.Max(envelope.maximumDownReach,-heading.y+worldPadding);
        }
        static void ActualEnvelopeMesh(ActualMountedCourseEnvelope envelope,Mesh mesh,Matrix4x4 matrix,Vector3 crossing,Transform model,Transform truck,string pose,Action<int> count)
        {
            var vertices=mesh.vertices;var referenced=mesh.triangles.Distinct().ToArray();if(referenced.Length==0)throw new InvalidOperationException("Actual mounted render mesh has no referenced triangle geometry: "+mesh.name);foreach(int index in referenced)ActualEnvelopePoint(envelope,matrix.MultiplyPoint3x4(vertices[index]),crossing,model,truck,pose);count(referenced.Length);
        }
        static void CompleteActualCourseEnvelopeDimensions(ActualMountedCourseEnvelope envelope)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckSkyTow/Art/Arctic standard gate body.asset");if(mesh==null)throw new InvalidOperationException("Original authored physical ring mesh required.");var vertices=mesh.vertices;var triangles=mesh.triangles;float minimum=float.PositiveInfinity,maximum=0;
            float Edge(Vector2 a,Vector2 b){var delta=b-a;float t=delta.sqrMagnitude>1e-12f?Mathf.Clamp01(-Vector2.Dot(a,delta)/delta.sqrMagnitude):0;return (a+delta*t).magnitude;}
            foreach(var p in vertices)maximum=Mathf.Max(maximum,new Vector2(p.x,p.y).magnitude);
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=(Vector2)vertices[triangles[i]];var b=(Vector2)vertices[triangles[i+1]];var c=(Vector2)vertices[triangles[i+2]];float Cross(Vector2 x,Vector2 y)=>x.x*y.y-x.y*y.x;float area=Cross(b-a,c-a),x=Cross(b-a,-a),y=Cross(c-b,-b),z=Cross(a-c,-c);if(Mathf.Abs(area)>1e-10f&&(x>=0&&y>=0&&z>=0||x<=0&&y<=0&&z<=0))throw new InvalidOperationException("Source ring projection contains a solid central triangle; no invented aperture allowed.");minimum=Mathf.Min(minimum,Mathf.Min(Edge(a,b),Mathf.Min(Edge(b,c),Edge(c,a))));
            }
            if(!float.IsFinite(minimum)||minimum<=.01f)throw new InvalidOperationException("Source mesh has no actual measurable open radial aperture.");envelope.sourceRingInnerAperture=minimum;envelope.sourceRingOuterRadius=maximum;envelope.sourceRingDepth=mesh.bounds.size.z;
            // Explicit design suggestions, retaining BOTH exact nominal and
            // any-attitude actual-vertex measurements for review. Not a hidden
            // acceptance threshold or a required user-authored32m radius.
            envelope.proposedClearRadiusNominal=Mathf.Ceil((envelope.maximumNominalGateRadialExtent+5)/5)*5;envelope.proposedClearRadiusAnyAttitude=Mathf.Ceil((envelope.maximumAnyAttitudeVertexRadius+5)/5)*5;
        }
        static void CaptureActualTruckCenteredCourseEnvelope(ActualMountedCourseEnvelope envelope,DragonCharacter actor,DragonRiderSaddle frame,SportsEventTruck truck,string pose)
        {
            // Geometry authoring moves Transforms without advancing physics. Collider.bounds
            // may be empty or cached; use the actual authored box center at its current pose.
            // Retain BOTH native values so the saved proof exposes rather than hides any delta.
            envelope.scope=envelope.scope.Replace("RELATIVE_REAL_TRUCK_SHAPE_BOUNDS_CENTER","RELATIVE_CURRENT_AUTHORED_TRUCK_BOX_CENTER");
            var center=MeasureCurrentRiderTruckCrossingCenter(truck.shape,pose);envelope.crossingCenters.Add(center);
            if(!center.currentMatrixCenter.Equals(center.physicsBoundsCenter))envelope.physicsBoundsNonmatchingSamples++;
            if(center.physicsBoundsEmpty)envelope.physicsBoundsEmptySamples++;if(!center.shapeEnabled)envelope.disabledShapeSamples++;if(!center.shapeActiveInHierarchy)envelope.inactiveShapeSamples++;
            envelope.maximumPhysicsCenterDelta=Mathf.Max(envelope.maximumPhysicsCenterDelta,center.centerDelta);
            Vector3 crossing=center.currentMatrixCenter;var model=actor.motor.transform;envelope.poses++;var baked=new Mesh();
            try
            {
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(value=>value.sharedMesh!=null)){skin.BakeMesh(baked,true);ActualEnvelopeMesh(envelope,baked,skin.transform.localToWorldMatrix,crossing,model,truck.transform,pose,count=>envelope.skinPoints+=count);}
                foreach(var renderer in truck.GetComponentsInChildren<MeshRenderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy)){var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh!=null)ActualEnvelopeMesh(envelope,mesh,renderer.transform.localToWorldMatrix,crossing,model,truck.transform,pose,count=>envelope.nwhMeshPoints+=count);}
                foreach(var skin in truck.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy&&value.sharedMesh!=null)){skin.BakeMesh(baked,true);ActualEnvelopeMesh(envelope,baked,skin.transform.localToWorldMatrix,crossing,model,truck.transform,pose,count=>envelope.nwhMeshPoints+=count);}
                foreach(var shape in truck.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy))
                {
                    if(shape is not BoxCollider box)throw new InvalidOperationException("Measure every original full NWH solid shape; unsupported collider "+shape.GetType().Name);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){ActualEnvelopePoint(envelope,box.transform.TransformPoint(box.center+Vector3.Scale(box.size*.5f,new Vector3(x,y,z))),crossing,model,truck.transform,pose);envelope.nwhBoxCorners++;}
                }
                foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy)){var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh!=null)ActualEnvelopeMesh(envelope,mesh,renderer.transform.localToWorldMatrix,crossing,model,truck.transform,pose,count=>envelope.rigidMountPoints+=count);}
                foreach(var line in actor.GetComponentsInChildren<LineRenderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy))for(int index=0;index<line.positionCount;index++)
                {
                    Vector3 point=line.useWorldSpace?line.GetPosition(index):line.transform.TransformPoint(line.GetPosition(index));var keys=line.widthCurve.keys;if(keys.Length==0||keys.Any(key=>Mathf.Abs(key.value-keys[0].value)>.000001f||Mathf.Abs(key.inTangent)>.000001f||Mathf.Abs(key.outTangent)>.000001f))throw new InvalidOperationException("Original constant-width harness expected; variable native line width needs its own measured extrema.");float halfWidth=Mathf.Max(line.startWidth,line.endWidth)*.5f;
                    // Native line widths are world units. Add the full radial
                    // half-width analytically, rather than sampling six axes
                    // which would miss a diagonal width extremum.
                    ActualEnvelopePoint(envelope,point,crossing,model,truck.transform,pose,halfWidth);envelope.harnessLineSamples++;
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(baked);}
        }
    }
}
#endif
