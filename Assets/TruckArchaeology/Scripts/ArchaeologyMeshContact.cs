using System;
using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckArchaeology
{
 // Read-only query of the SAME PhysX-confirmed penetrating vendor collider.
 // No terrain/mesh generation, collision response or clearance authority.
 public sealed class ArchaeologyMeshContact
 {
  readonly List<Vector3> vertices=new List<Vector3>(4096);readonly List<int> triangles=new List<int>(8192);
  readonly Vector3[] first=new Vector3[16],second=new Vector3[16];
  static void Add(Vector3[] output,ref int count,Vector3 point){if(count>0&&(output[count-1]-point).sqrMagnitude<1e-12f)return;if(count>=output.Length)throw new InvalidOperationException("Bounded triangle-box query exceeded convex polygon capacity.");output[count++]=point;}
  public bool ClipTriangle(Bounds box,Vector3 a,Vector3 b,Vector3 c,out Vector3 point)
  {
   point=default;if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-12f)return false;var input=first;var output=second;input[0]=a;input[1]=b;input[2]=c;int count=3;
   for(int plane=0;plane<6;plane++)
   {
    int axis=plane/2;bool lower=(plane&1)==0;float limit=lower?box.min[axis]:box.max[axis];int written=0;Vector3 previous=input[count-1];float before=lower?previous[axis]-limit:limit-previous[axis];
    for(int i=0;i<count;i++)
    {
     Vector3 current=input[i];float after=lower?current[axis]-limit:limit-current[axis];if((before>=0)!=(after>=0))Add(output,ref written,Vector3.LerpUnclamped(previous,current,before/(before-after)));if(after>=0)Add(output,ref written,current);previous=current;before=after;
    }
    if(written>1&&(output[0]-output[written-1]).sqrMagnitude<1e-12f)written--;if(written==0)return false;count=written;var swap=input;input=output;output=swap;
   }
   for(int i=0;i<count;i++)point+=input[i];point/=count;return true;
  }
  public bool TryFindSurface(BoxCollider box,MeshCollider collider,out Vector3 surface,out Vector3 normal,out int submesh,out int triangle)
  {
   surface=normal=default;submesh=triangle=-1;var mesh=collider.sharedMesh;if(mesh==null||!mesh.isReadable)return false;mesh.GetVertices(vertices);var toBox=box.transform.worldToLocalMatrix*collider.transform.localToWorldMatrix;var bounds=new Bounds(box.center,box.size);
   for(int part=0;part<mesh.subMeshCount;part++)
   {
    mesh.GetTriangles(triangles,part);for(int i=0;i+2<triangles.Count;i+=3)
    {
     Vector3 a=toBox.MultiplyPoint3x4(vertices[triangles[i]]),b=toBox.MultiplyPoint3x4(vertices[triangles[i+1]]),c=toBox.MultiplyPoint3x4(vertices[triangles[i+2]]);var candidate=new Bounds(a,Vector3.zero);candidate.Encapsulate(b);candidate.Encapsulate(c);if(!candidate.Intersects(bounds)||!ClipTriangle(bounds,a,b,c,out Vector3 clipped))continue;
     Vector3 wa=box.transform.TransformPoint(a),wb=box.transform.TransformPoint(b),wc=box.transform.TransformPoint(c),outward=Vector3.Cross(wb-wa,wc-wa);if(outward.sqrMagnitude<1e-12f)continue;surface=box.transform.TransformPoint(clipped);normal=outward.normalized;submesh=part;triangle=i/3;return true;
    }
   }
   return false;
  }
 }
}
