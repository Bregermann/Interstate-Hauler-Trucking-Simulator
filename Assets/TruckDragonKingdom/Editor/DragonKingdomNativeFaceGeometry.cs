#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDragonKingdom.Editor
{
    public static class DragonKingdomNativeFaceGeometry
    {
        [Serializable]public sealed class Evidence
        {
            public string scope="INTENDED_NATIVE_FACE_NORMAL_CHANGE_ONLY_NOT_PAINT_NORMAL_IDENTITY_WAIVER",error,firstAttributeDifference,firstFaceDifference;
            public bool geometryExact,nonNormalAttributesExact,originalNormalAwareCanonicalExact,nativeFaceNormalsVerified;public int vertices,triangles,normalCornersChecked;public float minimumFaceDot=1,maximumUnitSquaredError;
        }
        sealed class Bytes:IComparer<byte[]>
        {public int Compare(byte[] a,byte[] b){int n=Math.Min(a.Length,b.Length);for(int i=0;i<n;i++){int c=a[i].CompareTo(b[i]);if(c!=0)return c;}return a.Length.CompareTo(b.Length);}}
        static readonly Bytes Order=new Bytes();
        static string Hex(byte[] bytes)=>BitConverter.ToString(bytes).Replace("-","");
        static byte[][] Attributes(Mesh mesh)
        {
            var layout=mesh.GetVertexAttributes();var expected=Digger.Modules.Core.Sources.VertexData.Layout;
            if(mesh.vertexBufferCount!=1||layout.Length!=expected.Length||layout.Where((v,i)=>v.attribute!=expected[i].attribute||v.format!=expected[i].format||v.dimension!=expected[i].dimension||v.stream!=expected[i].stream).Any()||mesh.GetVertexAttributeFormat(VertexAttribute.Normal)!=VertexAttributeFormat.Float32||mesh.GetVertexAttributeDimension(VertexAttribute.Normal)!=3)throw new InvalidOperationException("Exact installed native vertex layout and complete float32 normals required.");
            int stride=mesh.GetVertexBufferStride(0),offset=mesh.GetVertexAttributeOffset(VertexAttribute.Normal);using(var access=Mesh.AcquireReadOnlyMeshData(mesh))
            {
                var bytes=access[0].GetVertexData<byte>(0).ToArray();if(bytes.Length!=mesh.vertexCount*stride||offset<0||offset+12>stride)throw new InvalidOperationException("Complete actual native vertex bytes required.");var rows=new byte[mesh.vertexCount][];
                for(int i=0;i<rows.Length;i++){rows[i]=new byte[stride-12];Array.Copy(bytes,i*stride,rows[i],0,offset);Array.Copy(bytes,i*stride+offset+12,rows[i],offset,stride-offset-12);}return rows;
            }
        }
        static byte[][] Faces(Mesh mesh,byte[][] attributes)
        {
            var rows=new List<byte[]>();for(int s=0;s<mesh.subMeshCount;s++)
            {
                var indices=mesh.GetIndices(s);if(mesh.GetTopology(s)!=MeshTopology.Triangles||indices.Length%3!=0||indices.Any(i=>i<0||i>=attributes.Length))throw new InvalidOperationException("Original valid oriented triangle submeshes required.");
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=attributes[indices[i]];var b=attributes[indices[i+1]];var c=attributes[indices[i+2]];
                    bool Less(byte[] x,byte[] y,byte[] z,byte[] p,byte[] q,byte[] r){int n=Order.Compare(x,p);if(n!=0)return n<0;n=Order.Compare(y,q);return n!=0?n<0:Order.Compare(z,r)<0;}
                    var original=new[]{a,b,c};int start=0;if(Less(original[1],original[2],original[0],original[0],original[1],original[2]))start=1;if(Less(original[2],original[0],original[1],original[start],original[(start+1)%3],original[(start+2)%3]))start=2;a=original[start];b=original[(start+1)%3];c=original[(start+2)%3];
                    var row=new byte[4+a.Length+b.Length+c.Length];Array.Copy(BitConverter.GetBytes(s),row,4);Array.Copy(a,0,row,4,a.Length);Array.Copy(b,0,row,4+a.Length,b.Length);Array.Copy(c,0,row,4+a.Length+b.Length,c.Length);rows.Add(row);
                }
            }
            var result=rows.ToArray();Array.Sort(result,Order);return result;
        }
        static bool EqualRows(byte[][] before,byte[][] after,out string first)
        {
            first=null;if(before.Length!=after.Length){first="COUNT "+before.Length+" -> "+after.Length;return false;}var a=(byte[][])before.Clone();var b=(byte[][])after.Clone();Array.Sort(a,Order);Array.Sort(b,Order);for(int i=0;i<a.Length;i++)if(Order.Compare(a[i],b[i])!=0){first=Hex(a[i])+" -> "+Hex(b[i]);return false;}return true;
        }
        static bool SameBits(Vector3 a,Vector3 b)=>BitConverter.SingleToInt32Bits(a.x)==BitConverter.SingleToInt32Bits(b.x)&&BitConverter.SingleToInt32Bits(a.y)==BitConverter.SingleToInt32Bits(b.y)&&BitConverter.SingleToInt32Bits(a.z)==BitConverter.SingleToInt32Bits(b.z);
        public static Evidence Compare(Mesh before,Mesh nativeFaceResult)
        {
            var proof=new Evidence();try
            {
                if(before==null||nativeFaceResult==null||before.vertexCount!=nativeFaceResult.vertexCount||before.subMeshCount!=nativeFaceResult.subMeshCount)throw new InvalidOperationException("Actual equally populated native before/after meshes required.");proof.vertices=before.vertexCount;
                var a=Attributes(before);var b=Attributes(nativeFaceResult);proof.nonNormalAttributesExact=EqualRows(a,b,out proof.firstAttributeDifference);proof.geometryExact=EqualRows(Faces(before,a),Faces(nativeFaceResult,b),out proof.firstFaceDifference)&&proof.nonNormalAttributesExact;
                proof.originalNormalAwareCanonicalExact=DragonKingdomDiggerCavernAuthoring.CompareVaultMeshesExact(before,nativeFaceResult).canonicalExact;
                var p=nativeFaceResult.vertices;var n=nativeFaceResult.normals;if(n.Length!=p.Length)throw new InvalidOperationException("Every native face vertex requires its actual normal.");proof.nativeFaceNormalsVerified=true;
                foreach(int s in Enumerable.Range(0,nativeFaceResult.subMeshCount))
                {
                    var indices=nativeFaceResult.GetIndices(s);for(int i=0;i<indices.Length;i+=3)
                    {
                        int x=indices[i],y=indices[i+1],z=indices[i+2];var face=Vector3.Cross(p[y]-p[x],p[z]-p[x]);if(!float.IsFinite(face.sqrMagnitude)||face.sqrMagnitude<=0)throw new InvalidOperationException("Native face-mode output contains a degenerate/nonfinite face.");face/=Mathf.Sqrt(face.sqrMagnitude);proof.triangles++;
                        if(!SameBits(n[x],n[y])||!SameBits(n[x],n[z]))proof.nativeFaceNormalsVerified=false;
                        foreach(int at in new[]{x,y,z}){float length=n[at].sqrMagnitude,dot=Vector3.Dot(n[at],face);if(!float.IsFinite(length)||!float.IsFinite(dot))throw new InvalidOperationException("Finite native unit face normals required.");proof.minimumFaceDot=Mathf.Min(proof.minimumFaceDot,dot);proof.maximumUnitSquaredError=Mathf.Max(proof.maximumUnitSquaredError,Mathf.Abs(length-1));proof.normalCornersChecked++;if(Mathf.Abs(length-1)>.00001f||dot<.99999f)proof.nativeFaceNormalsVerified=false;}
                    }
                }
            }
            catch(InvalidOperationException error){proof.error=error.Message;proof.geometryExact=proof.nativeFaceNormalsVerified=false;}return proof;
        }
    }
}
#endif
