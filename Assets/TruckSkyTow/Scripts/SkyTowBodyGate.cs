using UnityEngine;

namespace LWS.TruckSkyTow
{
 // Exact translated-box sweep against the OPEN disk. Rotating steps use small
 // inscribed sweeps: every accepted point belongs to the interpolated rigid
 // body, rather than a loose projected radius or a rotational convex envelope.
 public sealed class SkyTowBodyGate
 {
  readonly Vector3[] vertices=new Vector3[16];
  static readonly int[] faces={0,2,3,0,3,1,4,5,7,4,7,6,0,1,5,0,5,4,2,6,7,2,7,3,0,4,6,0,6,2,1,3,7,1,7,5};
  static readonly int[] edges={0,1,0,2,0,4,1,3,1,5,2,3,2,6,3,7,4,5,4,6,5,7,6,7};
  float radiusSquared;
  static float SegmentDistance(Vector3 a,Vector3 b)
  {
   var d=b-a;float length=d.x*d.x+d.y*d.y;float t=length>.00000001f?Mathf.Clamp01(-(a.x*d.x+a.y*d.y)/length):0;float x=a.x+d.x*t,y=a.y+d.y*t;return x*x+y*y;
  }
  bool Triangle(int a,int b,int c)
  {
   if(Mathf.Abs(vertices[a].z)<.0000001f&&Mathf.Abs(vertices[b].z)<.0000001f&&Mathf.Abs(vertices[c].z)<.0000001f)return SegmentDistance(vertices[a],vertices[b])<radiusSquared||SegmentDistance(vertices[b],vertices[c])<radiusSquared||SegmentDistance(vertices[c],vertices[a])<radiusSquared;
   Vector3 p=default,q=default;int count=0;
   Cut(vertices[a],vertices[b],ref p,ref q,ref count);Cut(vertices[b],vertices[c],ref p,ref q,ref count);Cut(vertices[c],vertices[a],ref p,ref q,ref count);
   return count>0&&SegmentDistance(p,count>1?q:p)<radiusSquared;
  }
  static void Cut(Vector3 a,Vector3 b,ref Vector3 p,ref Vector3 q,ref int count)
  {
   if(a.z*b.z>0)return;float delta=b.z-a.z;
   if(Mathf.Abs(delta)<.0000001f){if(Mathf.Abs(a.z)>.0000001f)return;Point(a,ref p,ref q,ref count);Point(b,ref p,ref q,ref count);return;}
   float t=-a.z/delta;if(t<0||t>1)return;Point(Vector3.LerpUnclamped(a,b,t),ref p,ref q,ref count);
  }
  static void Point(Vector3 point,ref Vector3 p,ref Vector3 q,ref int count){if(count==0){p=point;count=1;}else if((point-p).sqrMagnitude>.000000000001f){q=point;count=2;}}
  static bool Contains(Vector3 start,Vector3 end,Quaternion rotation,Vector3 extents,Vector3 point)
  {
   var inverse=Quaternion.Inverse(rotation);var origin=inverse*(point-start);var velocity=inverse*(end-start);float enter=0,exit=1;
   for(int axis=0;axis<3;axis++){float o=origin[axis],v=velocity[axis],e=extents[axis];if(Mathf.Abs(v)<.0000001f){if(Mathf.Abs(o)>e)return false;}else{float a=(o-e)/v,b=(o+e)/v;if(a>b){float swap=a;a=b;b=swap;}enter=Mathf.Max(enter,a);exit=Mathf.Min(exit,b);if(enter>exit)return false;}}
   return true;
  }
  bool Translated(Vector3 start,Vector3 end,Quaternion rotation,Vector3 extents,Vector3 gate,Quaternion gateInverse)
  {
   if(extents.x<=0||extents.y<=0||extents.z<=0)return false;
   if(Contains(start,end,rotation,extents,gate))return true;
   for(int i=0;i<8;i++){var corner=rotation*Vector3.Scale(extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));vertices[i]=gateInverse*(start+corner-gate);vertices[i+8]=gateInverse*(end+corner-gate);}
   for(int i=0;i<faces.Length;i+=3)if(Triangle(faces[i],faces[i+1],faces[i+2])||Triangle(faces[i]+8,faces[i+1]+8,faces[i+2]+8))return true;
   for(int i=0;i<edges.Length;i+=2){int a=edges[i],b=edges[i+1];if(Triangle(a,b,b+8)||Triangle(a,b+8,a+8))return true;}
   return false;
  }
  public bool Sweep(Vector3 start,Quaternion before,Vector3 end,Quaternion after,Vector3 localCenter,Vector3 extents,Vector3 gate,Quaternion gateRotation,float clearRadius)
  {
   if(clearRadius<=0)return false;float reach=extents.magnitude+localCenter.magnitude;var normal=gateRotation*Vector3.forward;float a=Vector3.Dot(start-gate,normal),b=Vector3.Dot(end-gate,normal);if(Mathf.Min(a,b)>reach||Mathf.Max(a,b)<-reach)return false;
   var delta=end-start;float t=delta.sqrMagnitude>.0000001f?Mathf.Clamp01(Vector3.Dot(gate-start,delta)/delta.sqrMagnitude):0;if(Vector3.Distance(start+delta*t,gate)>reach+clearRadius)return false;
   radiusSquared=Mathf.Max(0,clearRadius-.00001f)*Mathf.Max(0,clearRadius-.00001f);var inverse=Quaternion.Inverse(gateRotation);float angle=Quaternion.Angle(before,after)*Mathf.Deg2Rad;
   var first=start+before*localCenter;var last=end+after*localCenter;
   if(angle<.000001f)return Translated(first,last,before,extents,gate,inverse);
   if(Translated(first,first,before,extents,gate,inverse)||Translated(last,last,after,extents,gate,inverse))return true;
   int steps=Mathf.Clamp(Mathf.CeilToInt(angle*reach/.004f),1,64);float shrink=2*extents.magnitude*Mathf.Sin(angle/(steps*4))+localCenter.magnitude*(1-Mathf.Cos(angle/(steps*2)));var inset=extents-Vector3.one*shrink;
   for(int i=0;i<steps;i++){float t0=i/(float)steps,t1=(i+1)/(float)steps;var p0=Vector3.Lerp(start,end,t0)+Quaternion.Slerp(before,after,t0)*localCenter;var p1=Vector3.Lerp(start,end,t1)+Quaternion.Slerp(before,after,t1)*localCenter;if(Translated(p0,p1,Quaternion.Slerp(before,after,(i+.5f)/steps),inset,gate,inverse))return true;}
   return false;
  }
 }
}
