#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        sealed class VaultMeshGeometryState
        {
            public bool hasMesh;public int meshInstanceId;public string rawShape,legacySignature,bindingState;
            public Vector3[] positions,normals;public int[][] indices;public MeshTopology[] topologies;
        }
        [Serializable]public sealed class VaultMeshDifference
        {
            public string binding,classification,beforeRawHash,afterRawHash,beforeBindingState,afterBindingState;
            public bool rawExact,canonicalExact,bindingStateExact,vertexBitsExact,triangleWindingExact;
            public int beforeMeshInstanceId,afterMeshInstanceId,beforeVertices,afterVertices,beforeNormals,afterNormals,beforeSubmeshes,afterSubmeshes;
            public string[] rawPositionBitDifferences,rawNormalBitDifferences,rawIndexDifferences;
            public string firstBeforeVertex,firstAfterVertex,firstBeforeTriangle,firstAfterTriangle;
        }
        [Serializable]public sealed class VaultMeshComparison
        {
            public string scope="EXACT_IEEE754_VERTEX_NORMAL_AND_ORIENTED_SUBMESH_TRIANGLE_MULTISET_ONLY_ORDER_AND_MESH_OBJECT_IDENTITY_PERMUTATIONS_ALLOWED";
            public int beforeBindings,afterBindings,rawExactBindings,permutationOnlyBindings,rejectedBindings,meshObjectIdentityChanges;
            public bool rawOrderExact,canonicalExact;public VaultMeshDifference[] differences;
        }
        struct VaultExactVertex:IComparable<VaultExactVertex>
        {
            public int x,y,z,nx,ny,nz;
            public int CompareTo(VaultExactVertex b)
            {int c=x.CompareTo(b.x);if(c!=0)return c;c=y.CompareTo(b.y);if(c!=0)return c;c=z.CompareTo(b.z);if(c!=0)return c;c=nx.CompareTo(b.nx);if(c!=0)return c;c=ny.CompareTo(b.ny);return c!=0?c:nz.CompareTo(b.nz);}
            public VaultExactVertex PositionOnly()=>new VaultExactVertex{x=x,y=y,z=z};
            public override string ToString()=>"P("+Hex(x)+","+Hex(y)+","+Hex(z)+")N("+Hex(nx)+","+Hex(ny)+","+Hex(nz)+")";
        }
        struct VaultExactTriangle:IComparable<VaultExactTriangle>
        {
            public int submesh;public VaultExactVertex a,b,c;
            public int CompareTo(VaultExactTriangle v)
            {int order=submesh.CompareTo(v.submesh);if(order!=0)return order;order=a.CompareTo(v.a);if(order!=0)return order;order=b.CompareTo(v.b);return order!=0?order:c.CompareTo(v.c);}
            public override string ToString()=>"S"+submesh+":"+a+" | "+b+" | "+c;
        }
        static string Hex(int bits)=>unchecked((uint)bits).ToString("X8",CultureInfo.InvariantCulture);
        static string VectorBits(Vector3 v)=>Hex(BitConverter.SingleToInt32Bits(v.x))+","+Hex(BitConverter.SingleToInt32Bits(v.y))+","+Hex(BitConverter.SingleToInt32Bits(v.z));
        static bool VaultFinite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        static VaultMeshGeometryState CaptureVaultMeshGeometry(Mesh mesh,string state,string legacyPrefix="")
        {
            if(mesh==null)return new VaultMeshGeometryState{hasMesh=false,bindingState=state,rawShape="NONE",legacySignature=legacyPrefix+"NONE",positions=Array.Empty<Vector3>(),normals=Array.Empty<Vector3>(),indices=Array.Empty<int[]>(),topologies=Array.Empty<MeshTopology>()};
            var positions=mesh.vertices;var normals=mesh.normals;var indices=Enumerable.Range(0,mesh.subMeshCount).Select(mesh.GetIndices).ToArray();var topology=Enumerable.Range(0,mesh.subMeshCount).Select(mesh.GetTopology).ToArray();
            // Preserve the ORIGINAL raw-order signature from V362, including
            // its combined triangle order and collider flag prefix.
            string raw=VaultHash(w=>{w.Write(mesh.vertexCount);foreach(var v in positions){w.Write(v.x);w.Write(v.y);w.Write(v.z);}foreach(var n in normals){w.Write(n.x);w.Write(n.y);w.Write(n.z);}foreach(int i in mesh.triangles)w.Write(i);});
            return new VaultMeshGeometryState{hasMesh=true,meshInstanceId=mesh.GetInstanceID(),bindingState=state,rawShape=raw,legacySignature=legacyPrefix+raw,positions=positions,normals=normals,indices=indices,topologies=topology};
        }
        static Dictionary<string,VaultMeshGeometryState> CaptureVaultMeshStates(IEnumerable<Digger.Modules.Core.Sources.DiggerSystem> systems)
        {
            var result=new Dictionary<string,VaultMeshGeometryState>();foreach(var system in systems)
            {
                string PathOf(Transform t){string path=t.name+"#"+t.GetSiblingIndex();while(t!=system.transform){t=t.parent;path=t.name+"/"+path;}return system.BasePathData+"/"+path;}
                string Pose(Transform t){var m=t.localToWorldMatrix;return string.Join(",",Enumerable.Range(0,16).Select(i=>Hex(BitConverter.SingleToInt32Bits(m[i]))))+"/active="+t.gameObject.activeSelf+"/layer="+t.gameObject.layer;}
                foreach(var filter in system.GetComponentsInChildren<MeshFilter>(true))result.Add("M/"+PathOf(filter.transform),CaptureVaultMeshGeometry(filter.sharedMesh,Pose(filter.transform)));
                foreach(var collider in system.GetComponentsInChildren<MeshCollider>(true))
                {string state=Pose(collider.transform)+"/enabled="+collider.enabled+"/trigger="+collider.isTrigger+"/convex="+collider.convex+"/cooking="+(int)collider.cookingOptions+"/material="+(collider.sharedMaterial==null?0:collider.sharedMaterial.GetInstanceID());result.Add("C/"+PathOf(collider.transform),CaptureVaultMeshGeometry(collider.sharedMesh,state,collider.enabled+"/"+collider.isTrigger+"/"));}
            }
            return result;
        }
        static VaultExactVertex[] VaultVertexBits(VaultMeshGeometryState state)
        {
            if(state.normals.Length!=0&&state.normals.Length!=state.positions.Length||state.positions.Any(v=>!VaultFinite(v))||state.normals.Any(v=>!VaultFinite(v)))throw new InvalidOperationException("Finite complete native mesh position/normal arrays required.");var vertices=new VaultExactVertex[state.positions.Length];
            for(int i=0;i<vertices.Length;i++){var p=state.positions[i];var n=state.normals.Length==0?Vector3.zero:state.normals[i];vertices[i]=new VaultExactVertex{x=BitConverter.SingleToInt32Bits(p.x),y=BitConverter.SingleToInt32Bits(p.y),z=BitConverter.SingleToInt32Bits(p.z),nx=BitConverter.SingleToInt32Bits(n.x),ny=BitConverter.SingleToInt32Bits(n.y),nz=BitConverter.SingleToInt32Bits(n.z)};}return vertices;
        }
        static VaultExactTriangle[] VaultTriangleBits(VaultMeshGeometryState state,VaultExactVertex[] vertices)
        {
            var faces=new List<VaultExactTriangle>();for(int submesh=0;submesh<state.indices.Length;submesh++)
            {
                var indices=state.indices[submesh];if(state.topologies[submesh]!=MeshTopology.Triangles||indices.Length%3!=0||indices.Any(i=>i<0||i>=vertices.Length))throw new InvalidOperationException("Only complete valid original native triangle submeshes are accepted.");
                for(int i=0;i<indices.Length;i+=3)
                {
                    var face=new VaultExactTriangle{submesh=submesh,a=vertices[indices[i]],b=vertices[indices[i+1]],c=vertices[indices[i+2]]};var second=new VaultExactTriangle{submesh=submesh,a=face.b,b=face.c,c=face.a};var third=new VaultExactTriangle{submesh=submesh,a=face.c,b=face.a,c=face.b};if(second.CompareTo(face)<0)face=second;if(third.CompareTo(face)<0)face=third;faces.Add(face);
                }
            }
            var sorted=faces.ToArray();Array.Sort(sorted);return sorted;
        }
        static string[] RawVectorDifferences(Vector3[] before,Vector3[] after)
        {return Enumerable.Range(0,Math.Min(before.Length,after.Length)).Where(i=>VectorBits(before[i])!=VectorBits(after[i])).Take(4).Select(i=>i+": "+VectorBits(before[i])+" -> "+VectorBits(after[i])).ToArray();}
        static bool VaultMeshStructureExact(VaultMeshGeometryState before,VaultMeshGeometryState after)
            =>before.hasMesh==after.hasMesh&&before.positions.Length==after.positions.Length&&before.normals.Length==after.normals.Length&&before.indices.Length==after.indices.Length&&before.topologies.SequenceEqual(after.topologies)&&!before.indices.Where((indices,i)=>indices.Length!=after.indices[i].Length).Any();
        static VaultMeshDifference CompareVaultMeshState(string key,VaultMeshGeometryState before,VaultMeshGeometryState after)
        {
            var row=new VaultMeshDifference{binding=key,beforeRawHash=before.legacySignature,afterRawHash=after.legacySignature,rawExact=before.legacySignature==after.legacySignature,beforeBindingState=before.bindingState,afterBindingState=after.bindingState,bindingStateExact=before.bindingState==after.bindingState,beforeMeshInstanceId=before.meshInstanceId,afterMeshInstanceId=after.meshInstanceId,beforeVertices=before.positions.Length,afterVertices=after.positions.Length,beforeNormals=before.normals.Length,afterNormals=after.normals.Length,beforeSubmeshes=before.indices.Length,afterSubmeshes=after.indices.Length,rawPositionBitDifferences=RawVectorDifferences(before.positions,after.positions),rawNormalBitDifferences=RawVectorDifferences(before.normals,after.normals)};
            row.rawIndexDifferences=Enumerable.Range(0,Math.Min(before.indices.Length,after.indices.Length)).SelectMany(s=>Enumerable.Range(0,Math.Min(before.indices[s].Length,after.indices[s].Length)).Where(i=>before.indices[s][i]!=after.indices[s][i]).Take(4).Select(i=>"S"+s+"/"+i+": "+before.indices[s][i]+" -> "+after.indices[s][i])).Take(4).ToArray();
            if(!row.bindingStateExact){row.classification="BINDING_TRANSFORM_OR_COLLIDER_STATE_CHANGED";return row;}
            if(!VaultMeshStructureExact(before,after)){row.classification="MESH_PRESENCE_VERTEX_NORMAL_SUBMESH_OR_INDEX_COUNT_CHANGED";return row;}
            if(!before.hasMesh){row.canonicalExact=row.vertexBitsExact=row.triangleWindingExact=true;row.classification="SAME_NULL_BINDING";return row;}
            try
            {
                var a=VaultVertexBits(before);var b=VaultVertexBits(after);var sortedA=(VaultExactVertex[])a.Clone();var sortedB=(VaultExactVertex[])b.Clone();Array.Sort(sortedA);Array.Sort(sortedB);int first=Enumerable.Range(0,a.Length).Where(i=>sortedA[i].CompareTo(sortedB[i])!=0).DefaultIfEmpty(-1).First();
                row.vertexBitsExact=first<0;if(first>=0)
                {
                    row.firstBeforeVertex=sortedA[first].ToString();row.firstAfterVertex=sortedB[first].ToString();var pa=a.Select(v=>v.PositionOnly()).ToArray();var pb=b.Select(v=>v.PositionOnly()).ToArray();Array.Sort(pa);Array.Sort(pb);row.classification=pa.Where((v,i)=>v.CompareTo(pb[i])!=0).Any()?"VERTEX_COORDINATE_BITS_CHANGED":"NORMAL_BITS_OR_POSITION_NORMAL_ASSOCIATION_CHANGED";return row;
                }
                var facesA=VaultTriangleBits(before,a);var facesB=VaultTriangleBits(after,b);int face=Enumerable.Range(0,facesA.Length).Where(i=>facesA[i].CompareTo(facesB[i])!=0).DefaultIfEmpty(-1).First();row.triangleWindingExact=face<0;if(face>=0){row.firstBeforeTriangle=facesA[face].ToString();row.firstAfterTriangle=facesB[face].ToString();row.classification="ORIENTED_TRIANGLE_OR_SUBMESH_MULTISET_CHANGED";return row;}
                row.canonicalExact=true;row.classification=row.rawExact?"RAW_AND_CANONICAL_EXACT":"PERMUTATION_ONLY_COORDINATE_NORMAL_WINDING_BITS_EXACT";return row;
            }
            catch(InvalidOperationException error){row.classification="INVALID_NATIVE_GEOMETRY: "+error.Message;return row;}
        }
        static VaultMeshComparison CompareVaultMeshStates(Dictionary<string,VaultMeshGeometryState> before,Dictionary<string,VaultMeshGeometryState> after)
        {
            var result=new VaultMeshComparison{beforeBindings=before.Count,afterBindings=after.Count};var differences=new List<VaultMeshDifference>();foreach(string key in before.Keys.Union(after.Keys).OrderBy(k=>k,StringComparer.Ordinal))
            {
                if(!before.TryGetValue(key,out var a)||!after.TryGetValue(key,out var b)){differences.Add(new VaultMeshDifference{binding=key,classification="ADDED_OR_REMOVED_NATIVE_MESH_BINDING"});result.rejectedBindings++;continue;}
                if(a.meshInstanceId!=b.meshInstanceId)result.meshObjectIdentityChanges++;
                // Raw equality remains independently recorded. Changed arrays
                // must pass canonical exact-bit multisets, never a tolerance.
                if(a.legacySignature==b.legacySignature&&a.bindingState==b.bindingState&&VaultMeshStructureExact(a,b)){result.rawExactBindings++;continue;}
                var row=CompareVaultMeshState(key,a,b);differences.Add(row);if(row.rawExact)result.rawExactBindings++;if(row.canonicalExact)result.permutationOnlyBindings++;else result.rejectedBindings++;
            }
            result.rawOrderExact=before.Count==after.Count&&result.rawExactBindings==before.Count;result.canonicalExact=before.Count==after.Count&&result.rejectedBindings==0;result.differences=differences.ToArray();return result;
        }
        public static VaultMeshComparison CompareVaultMeshBindingControl(Mesh before,Mesh after)=>CompareVaultMeshStates(new Dictionary<string,VaultMeshGeometryState>{{"CONTROL",CaptureVaultMeshGeometry(before,"SAME_CONTROL_BINDING")}},new Dictionary<string,VaultMeshGeometryState>{{"CONTROL",CaptureVaultMeshGeometry(after,"SAME_CONTROL_BINDING")}});
        public static VaultMeshDifference CompareVaultMeshesExact(Mesh before,Mesh after)=>CompareVaultMeshState("ACTUAL_NATIVE_MESH_CONTROL",CaptureVaultMeshGeometry(before,"SAME_CONTROL_BINDING"),CaptureVaultMeshGeometry(after,"SAME_CONTROL_BINDING"));
    }
}
#endif
