#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomVaultMeshEquivalenceTests
{
    [TestCase("buffer-only",true)]
    [TestCase("vertex-order",true)]
    [TestCase("triangle-order",true)]
    [TestCase("vertex-triangle-cyclic-order",true)]
    [TestCase("texture-weights-only",true)]
    [TestCase("one-coordinate-bit",false)]
    [TestCase("one-normal-bit",false)]
    [TestCase("normal-association",false)]
    [TestCase("reverse-winding",false)]
    [TestCase("duplicate-face",false)]
    [TestCase("drop-face",false)]
    [TestCase("submesh-face-swap",false)]
    [TestCase("submesh-partition-same-raw",false)]
    public void ExactNativeGeometryAllowsOnlyOrderAndBufferPermutations(string change,bool expected)
    {
        var before=new Mesh{name="Exact native geometry before"};Mesh after=null;
        try
        {
            before.vertices=new[]{Vector3.zero,Vector3.right,new Vector3(1,1,0),Vector3.up};before.normals=new[]{Vector3.up,Vector3.forward,Vector3.forward,Vector3.forward};before.triangles=new[]{0,1,2,0,2,3};
            if(change=="submesh-face-swap"){before.subMeshCount=2;before.SetTriangles(new[]{0,1,2},0);before.SetTriangles(new[]{0,2,3},1);}
            after=UnityEngine.Object.Instantiate(before);after.name="Exact native geometry after";
            if(change=="vertex-order"||change=="vertex-triangle-cyclic-order")
            {
                int[] order={2,0,3,1};var inverse=new int[4];for(int i=0;i<4;i++)inverse[order[i]]=i;var positions=before.vertices;var normals=before.normals;after.vertices=order.Select(i=>positions[i]).ToArray();after.normals=order.Select(i=>normals[i]).ToArray();int[] faces=change=="vertex-order"?new[]{0,1,2,0,2,3}:new[]{2,3,0,1,2,0};after.triangles=faces.Select(i=>inverse[i]).ToArray();
            }
            else if(change=="triangle-order")after.triangles=new[]{0,2,3,0,1,2};
            else if(change=="texture-weights-only")after.SetUVs(2,new[]{new Vector4(0,0,0,1),new Vector4(0,0,0,1),new Vector4(0,0,0,1),new Vector4(0,0,0,1)});
            else if(change=="one-coordinate-bit"){var positions=after.vertices;positions[1].x=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(positions[1].x)+1);after.vertices=positions;}
            else if(change=="one-normal-bit"){var normals=after.normals;normals[1].z=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(normals[1].z)+1);after.normals=normals;}
            else if(change=="normal-association"){var normals=after.normals;var first=normals[0];normals[0]=normals[1];normals[1]=first;after.normals=normals;}
            else if(change=="reverse-winding")after.triangles=new[]{0,2,1,0,2,3};
            else if(change=="duplicate-face")after.triangles=new[]{0,1,2,0,1,2};
            else if(change=="drop-face")after.triangles=new[]{0,1,2};
            else if(change=="submesh-partition-same-raw"){after.subMeshCount=2;after.SetTriangles(new[]{0,1,2},0);after.SetTriangles(new[]{0,2,3},1);}
            else if(change=="submesh-face-swap"){after.SetTriangles(new[]{0,2,3},0);after.SetTriangles(new[]{0,1,2},1);}
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);var proof=type.GetMethod("CompareVaultMeshesExact").Invoke(null,new object[]{before,after});object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
            Assert.That((bool)Field("canonicalExact"),Is.EqualTo(expected),change+": "+Field("classification"));Assert.That((int)Field("beforeMeshInstanceId"),Is.Not.EqualTo((int)Field("afterMeshInstanceId")),"Control uses genuinely different native Mesh objects.");
            if(change=="vertex-order"||change=="triangle-order"||change=="vertex-triangle-cyclic-order")Assert.That((bool)Field("rawExact"),Is.False,"The original raw-order rejection must remain explicit even when canonical bits pass.");
            if(change=="one-coordinate-bit")Assert.That((string)Field("classification"),Is.EqualTo("VERTEX_COORDINATE_BITS_CHANGED"));if(change=="one-normal-bit"||change=="normal-association")Assert.That((string)Field("classification"),Is.EqualTo("NORMAL_BITS_OR_POSITION_NORMAL_ASSOCIATION_CHANGED"));if(change=="reverse-winding")Assert.That((string)Field("firstBeforeTriangle"),Is.Not.Empty);
            var bindingProof=type.GetMethod("CompareVaultMeshBindingControl").Invoke(null,new object[]{before,after});Assert.That((bool)bindingProof.GetType().GetField("canonicalExact").GetValue(bindingProof),Is.EqualTo(expected),"Actual binding dictionary fast path must enforce the same exact geometry/submesh rules.");
            if(change=="submesh-partition-same-raw")Assert.That((bool)Field("rawExact"),Is.True,"Combined raw triangles alone cannot prove unchanged submesh partitioning.");
            TestContext.WriteLine(change+" "+JsonUtility.ToJson(proof,true));
        }
        finally{if(after!=null)UnityEngine.Object.DestroyImmediate(after);UnityEngine.Object.DestroyImmediate(before);}
    }
}
#endif
