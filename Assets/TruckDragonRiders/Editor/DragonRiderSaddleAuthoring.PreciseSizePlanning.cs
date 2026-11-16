#if UNITY_EDITOR
using System;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        // Planning arithmetic only. The separate existing native float SAT,
        // actual BakeMesh, skin prediction, footprint and wheel gates are not
        // changed by this kernel and still decide whether a candidate fits.
        readonly struct SizePlanPoint
        {
            public readonly double x,y,z;
            public SizePlanPoint(double x,double y,double z){this.x=x;this.y=y;this.z=z;}
            public SizePlanPoint(Vector3 v):this(v.x,v.y,v.z){}
            public static SizePlanPoint operator +(SizePlanPoint a,SizePlanPoint b)=>new SizePlanPoint(a.x+b.x,a.y+b.y,a.z+b.z);
            public static SizePlanPoint operator -(SizePlanPoint a,SizePlanPoint b)=>new SizePlanPoint(a.x-b.x,a.y-b.y,a.z-b.z);
            public static SizePlanPoint operator *(SizePlanPoint a,double s)=>new SizePlanPoint(a.x*s,a.y*s,a.z*s);
            public static double Dot(SizePlanPoint a,SizePlanPoint b)=>a.x*b.x+a.y*b.y+a.z*b.z;
            public static SizePlanPoint Cross(SizePlanPoint a,SizePlanPoint b)=>new SizePlanPoint(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
            public Vector3 Float=>new Vector3((float)x,(float)y,(float)z);
        }
        static SizePlanPoint PreciseMaterialAnchor(Vector3 a,Vector3 b,Vector3 c,Vector3 barycentric)
        {
            // Preserve the authored affine material point. Do not normalize,
            // clamp, change its triangle, or edit the actual runtime binding.
            double sum=(double)barycentric.x+barycentric.y+barycentric.z;
            if(Math.Abs(sum-1)>1e-12)throw new InvalidOperationException("Exact affine authored barycentric sum required for this precision planning proof; actual runtime bindings remain unchanged.");
            var origin=new SizePlanPoint(a);return origin+(new SizePlanPoint(b)-origin)*barycentric.y+(new SizePlanPoint(c)-origin)*barycentric.z;
        }
        static SizePlanPoint PreciseBoxLocal(Matrix4x4 worldToLocal,SizePlanPoint world,Vector3 center)=>new SizePlanPoint(
            (double)worldToLocal.m00*world.x+(double)worldToLocal.m01*world.y+(double)worldToLocal.m02*world.z+worldToLocal.m03-center.x,
            (double)worldToLocal.m10*world.x+(double)worldToLocal.m11*world.y+(double)worldToLocal.m12*world.z+worldToLocal.m13-center.y,
            (double)worldToLocal.m20*world.x+(double)worldToLocal.m21*world.y+(double)worldToLocal.m22*world.z+worldToLocal.m23-center.z);
        static bool ForbiddenPreciseUniformSize(Vector3 worldA,Vector3 worldB,Vector3 worldC,SizePlanPoint materialAnchor,Matrix4x4 boxWorldToLocal,Vector3 center,Vector3 half,out double lower,out double upper)
        {
            var a=PreciseBoxLocal(boxWorldToLocal,new SizePlanPoint(worldA),center);var b=PreciseBoxLocal(boxWorldToLocal,new SizePlanPoint(worldB),center);var c=PreciseBoxLocal(boxWorldToLocal,new SizePlanPoint(worldC),center);var anchor=PreciseBoxLocal(boxWorldToLocal,materialAnchor,center);
            var edge0=b-a;var edge1=c-b;var edge2=a-c;a-=anchor;b-=anchor;c-=anchor;double low=1,high=double.PositiveInfinity;
            bool Axis(SizePlanPoint raw)
            {
                double square=SizePlanPoint.Dot(raw,raw);if(square<1e-14)return true;var axis=raw*(1/Math.Sqrt(square));
                double l=Math.Min(SizePlanPoint.Dot(a,axis),Math.Min(SizePlanPoint.Dot(b,axis),SizePlanPoint.Dot(c,axis)));
                double h=Math.Max(SizePlanPoint.Dot(a,axis),Math.Max(SizePlanPoint.Dot(b,axis),SizePlanPoint.Dot(c,axis)));
                double boxCenter=-SizePlanPoint.Dot(anchor,axis),radius=half.x*Math.Abs(axis.x)+half.y*Math.Abs(axis.y)+half.z*Math.Abs(axis.z);
                // The existing planning coefficient threshold is retained;
                // double differences/projections prevent cancellation from
                // manufacturing a nonzero slope on an anchored material face.
                if(l>1e-10)high=Math.Min(high,(boxCenter+radius)/l);else if(l< -1e-10)low=Math.Max(low,(boxCenter+radius)/l);else if(boxCenter+radius<0)return false;
                if(h>1e-10)low=Math.Max(low,(boxCenter-radius)/h);else if(h< -1e-10)high=Math.Min(high,(boxCenter-radius)/h);else if(boxCenter-radius>0)return false;
                return high>=low;
            }
            var right=new SizePlanPoint(1,0,0);var up=new SizePlanPoint(0,1,0);var forward=new SizePlanPoint(0,0,1);
            bool Edge(SizePlanPoint edge)=>Axis(SizePlanPoint.Cross(edge,right))&&Axis(SizePlanPoint.Cross(edge,up))&&Axis(SizePlanPoint.Cross(edge,forward));
            bool hit=Axis(right)&&Axis(up)&&Axis(forward)&&Axis(SizePlanPoint.Cross(edge0,edge1))&&Edge(edge0)&&Edge(edge1)&&Edge(edge2);
            lower=low;upper=high;return hit&&high>=low&&high>=1;
        }
        [Serializable]sealed class PreciseSizeControls
        {
            public string scope="DOUBLE_PRECISION_PLANNING_KERNEL_KNOWN_GEOMETRY_CONTROLS_NOT_NATIVE_SOURCE_PROFILE_OR_RUNTIME_ACCEPTANCE";
            public bool knownInitialInterval,knownLaterInterval,genuineUnboundedInterval,allTransformedAnchoredPlanesSeparated,nearCoplanarSourceWitnessSeparated;
            public bool legacyNearCoplanarInterval,legacyNearCoplanarUnbounded;public double legacyNearCoplanarLower,legacyNearCoplanarUpper;
            public int meaningfulWorldTransforms,independentNativeSatComparisons;
            public Vector3 sourceWitnessBoxCenter,sourceWitnessHalfExtents,sourceWitnessAuthoredBarycentric,sourceWitnessActualFloatAnchor,sourceWitnessPreciseAnchor;
            public Matrix4x4 sourceWitnessBoxMatrix;
            public float sourceWitnessActualToPreciseAnchorDistance;
        }
        public static string InspectPreciseMountedSizePlanningControls()
        {
            var result=new PreciseSizeControls();var a=new Vector3(-.5f,0,-.5f);var b=new Vector3(.5f,0,-.5f);var c=new Vector3(0,0,.5f);var anchor=new SizePlanPoint(0,-3,0);
            result.knownInitialInterval=ForbiddenPreciseUniformSize(a,b,c,anchor,Matrix4x4.identity,Vector3.zero,Vector3.one,out double low,out double high)&&Math.Abs(low-1)<1e-12&&Math.Abs(high-4d/3)<1e-12;
            var shift=Vector3.down*2;result.knownLaterInterval=ForbiddenPreciseUniformSize(a+shift,b+shift,c+shift,anchor,Matrix4x4.identity,Vector3.zero,Vector3.one,out low,out high)&&Math.Abs(low-2)<1e-12&&Math.Abs(high-4)<1e-12;
            result.genuineUnboundedInterval=ForbiddenPreciseUniformSize(a,b,c,new SizePlanPoint(0,0,0),Matrix4x4.identity,Vector3.zero,Vector3.one,out low,out high)&&double.IsPositiveInfinity(high);
            result.allTransformedAnchoredPlanesSeparated=true;
            foreach(var translation in new[]{Vector3.zero,new Vector3(10003,-4731,33017)})foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(17,43,-21)})foreach(var scale in new[]{Vector3.one,Vector3.one*1.87f,new Vector3(1.75f,.65f,2.25f)})
            {
                var matrix=Matrix4x4.TRS(translation,rotation,scale);var inverse=matrix.inverse;result.meaningfulWorldTransforms++;
                var pa=matrix.MultiplyPoint3x4(a+Vector3.up*2);var pb=matrix.MultiplyPoint3x4(b+Vector3.up*2);var pc=matrix.MultiplyPoint3x4(c+Vector3.up*2);var material=PreciseMaterialAnchor(pa,pb,pc,new Vector3(.25f,.25f,.5f));
                bool forbidden=ForbiddenPreciseUniformSize(pa,pb,pc,material,inverse,Vector3.zero,Vector3.one,out low,out high);result.allTransformedAnchoredPlanesSeparated&=!forbidden;
                foreach(float factor in new[]{1,1.1f,1.34f,2.5f,4.1f,8,16,32})
                {
                    bool predicted=forbidden&&factor>=low&&factor<=high;var la=inverse.MultiplyPoint3x4((material+(new SizePlanPoint(pa)-material)*factor).Float);var lb=inverse.MultiplyPoint3x4((material+(new SizePlanPoint(pb)-material)*factor).Float);var lc=inverse.MultiplyPoint3x4((material+(new SizePlanPoint(pc)-material)*factor).Float);
                    if(predicted!=ActualTriangleBoxSat(la,lb,lc,Vector3.one,out _))throw new InvalidOperationException("Precise transformed planning disagrees with the unchanged independent original native finite-box SAT control.");result.independentNativeSatComparisons++;
                }
            }
            // Exact retained V313 source-float triangle485/box matrix. Center
            // comes from unchanged V304 complete original NWH geometry; future
            // full derivation witnesses must retain it directly as well.
            a=new Vector3(-43.9825439453125f,229.96063232421875f,333.0661315917969f);b=new Vector3(-45,230.22813415527344f,331.0713195800781f);c=new Vector3(-44.99979019165039f,230.19456481933594f,333.0934753417969f);
            var box=new Matrix4x4(new Vector4(1,.0002233884879387915f,-3.638059038052477e-12f,0),new Vector4(-.00021989535889588296f,.9843629598617554f,.17615225911140442f,0),new Vector4(.00003935039057978429f,-.17615225911140442f,.9843629598617554f,0),new Vector4(-44.99946975708008f,230.8911590576172f,333.217041015625f,1));
            var center=new Vector3(-1.1342089859881486e-15f,.550000011920929f,.013257145881652832f);var half=new Vector3(1.0473871231079102f,.23000000417232513f,2.4166293144226074f)+Vector3.one*.02f;var bary=new Vector3(.00046883535105735064f,.0005426554707810283f,.9989885091781616f);var precise=PreciseMaterialAnchor(a,b,c,bary);var actual=new Vector3(-44.99931335449219f,230.19447326660156f,333.0923767089844f);
            result.sourceWitnessBoxCenter=center;result.sourceWitnessHalfExtents=half;result.sourceWitnessBoxMatrix=box;result.sourceWitnessAuthoredBarycentric=bary;result.sourceWitnessActualFloatAnchor=actual;result.sourceWitnessPreciseAnchor=precise.Float;result.sourceWitnessActualToPreciseAnchorDistance=Vector3.Distance(actual,precise.Float);
            result.nearCoplanarSourceWitnessSeparated=!ForbiddenPreciseUniformSize(a,b,c,precise,box.inverse,center,half,out low,out high);
            result.legacyNearCoplanarInterval=ForbiddenUniformSize(box.inverse.MultiplyPoint3x4(a)-center,box.inverse.MultiplyPoint3x4(b)-center,box.inverse.MultiplyPoint3x4(c)-center,box.inverse.MultiplyPoint3x4(actual)-center,half,out result.legacyNearCoplanarLower,out high);result.legacyNearCoplanarUnbounded=double.IsPositiveInfinity(high);result.legacyNearCoplanarUpper=result.legacyNearCoplanarUnbounded?0:high;
            if(!result.knownInitialInterval||!result.knownLaterInterval||!result.genuineUnboundedInterval||!result.allTransformedAnchoredPlanesSeparated||!result.nearCoplanarSourceWitnessSeparated||result.sourceWitnessActualToPreciseAnchorDistance>=.001f)throw new InvalidOperationException("Precise size planning failed known anchored, finite, later-contact, genuinely unbounded or exact retained near-coplanar source controls. "+JsonUtility.ToJson(result));
            return JsonUtility.ToJson(result,true);
        }
    }
}
#endif
