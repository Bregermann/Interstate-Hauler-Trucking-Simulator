#if UNITY_EDITOR && VISTA
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class DragonKingdomWeldedNormalTests
{
    static Type Probe=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);
    static Mesh Triangle(Vector3[] positions)
    {
        var layoutType=Type.GetType("Digger.Modules.Core.Sources.VertexData, Digger.Core",true);var mesh=new Mesh();mesh.SetVertexBufferParams(3,(VertexAttributeDescriptor[])layoutType.GetField("Layout").GetValue(null));int stride=mesh.GetVertexBufferStride(0);var bytes=new byte[stride*3];void Write(int at,float value)=>Array.Copy(BitConverter.GetBytes(value),0,bytes,at,4);
        for(int i=0;i<3;i++){for(int j=0;j<3;j++){Write(i*stride+j*4,positions[i][j]);Write(i*stride+12+j*4,Vector3.up[j]);}Write(i*stride+24+i*4,1);Write(i*stride+40,.25f+i*.1f);Write(i*stride+44,.75f);}
        mesh.SetVertexBufferData(bytes,0,0,bytes.Length);mesh.SetIndexBufferParams(3,IndexFormat.UInt16);mesh.SetIndexBufferData(new ushort[]{0,1,2},0,0,3);mesh.subMeshCount=1;mesh.SetSubMesh(0,new SubMeshDescriptor(0,3));return mesh;
    }
    [TestCase("same-lod")]
    [TestCase("translated-chunk")]
    [TestCase("different-lod")]
    [TestCase("one-ulp-separated")]
    public void UnityProxyNormalsUseOnlyExactSameLodPositionsAndPreserveNativePayload(string mode)
    {
        float origin=mode=="one-ulp-separated"?1:0;var a=Triangle(new[]{new Vector3(origin,0,0),new Vector3(origin+1,0,0),new Vector3(origin,1,0)});float other=mode=="one-ulp-separated"?BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f)+1):origin;var b=Triangle(new[]{new Vector3(other,0,0),new Vector3(origin,1,0),new Vector3(origin,0,1)});var frames=new[]{Matrix4x4.identity,Matrix4x4.identity};object built=null;
        if(mode=="translated-chunk"){var vertices=b.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]-=new Vector3(100,50,200);b.vertices=vertices;frames[1]=Matrix4x4.Translate(new Vector3(100,50,200));}
        string originalA=EditorJsonUtility.ToJson(a),originalB=EditorJsonUtility.ToJson(b);try
        {
            built=Probe.GetMethod("BuildNativeWeldedNormalCopies").Invoke(null,new object[]{new[]{a,b},frames,new[]{0,mode=="different-lod"?1:0}});var copies=(Mesh[])built.GetType().GetField("copies").GetValue(built);var groups=(Array)built.GetType().GetField("groups").GetValue(built);Assert.That(groups.Length,Is.EqualTo(mode=="different-lod"?2:1));
            if(mode=="different-lod"||mode=="one-ulp-separated"){Assert.That(Vector3.Angle(copies[0].normals[0],Vector3.forward),Is.LessThan(.01));Assert.That(Vector3.Angle(copies[1].normals[0],Vector3.right),Is.LessThan(.01));}
            else{Assert.That(copies[0].normals[0],Is.EqualTo(copies[1].normals[0]),"Exact shared positions across original chunk copies must receive the same native proxy normal bits.");Assert.That(Vector3.Angle(copies[0].normals[0],new Vector3(1,0,1).normalized),Is.LessThan(.01));}
            if(mode=="one-ulp-separated")Assert.That((int)groups.GetValue(0).GetType().GetField("uniqueProxyVertices").GetValue(groups.GetValue(0)),Is.EqualTo(5),"One ULP must not become a fuzzy weld.");
            foreach(var group in groups)foreach(string name in new[]{"unityRecalculateNormalsCalled","proxyPositionsAndOrientedTrianglesExact","allCopyNonNormalAttributesExact","allCopiedNormalsMatchNativeProxy"})Assert.That((bool)group.GetType().GetField(name).GetValue(group),Is.True,name);Assert.That(EditorJsonUtility.ToJson(a),Is.EqualTo(originalA));Assert.That(EditorJsonUtility.ToJson(b),Is.EqualTo(originalB));
        }
        finally{if(built!=null)((IDisposable)built).Dispose();Assert.That(EditorJsonUtility.ToJson(a),Is.EqualTo(originalA));Assert.That(EditorJsonUtility.ToJson(b),Is.EqualTo(originalB));UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
    }
    [Test]
    public void OpposedFacesRejectNativeZeroNormalsAndPreserveBothOriginalMeshes()
    {
        var a=Triangle(new[]{Vector3.zero,Vector3.right,Vector3.up});var b=Triangle(new[]{Vector3.zero,Vector3.up,Vector3.right});string beforeA=EditorJsonUtility.ToJson(a),beforeB=EditorJsonUtility.ToJson(b);try{var failure=Assert.Throws<TargetInvocationException>(()=>Probe.GetMethod("BuildNativeWeldedNormalCopies").Invoke(null,new object[]{new[]{a,b},new[]{Matrix4x4.identity,Matrix4x4.identity},new[]{0,0}}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(EditorJsonUtility.ToJson(a),Is.EqualTo(beforeA));Assert.That(EditorJsonUtility.ToJson(b),Is.EqualTo(beforeB));}finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
    }
}
#endif
