#if UNITY_EDITOR && VISTA
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class DragonKingdomNativeFaceNormalsTests
{
    static Type Geometry=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeFaceGeometry, Assembly-CSharp-Editor",true);
    static Mesh Control(bool faceNormals)
    {
        var layoutType=Type.GetType("Digger.Modules.Core.Sources.VertexData, Digger.Core",true);var layout=(VertexAttributeDescriptor[])layoutType.GetField("Layout").GetValue(null);var mesh=new Mesh();mesh.SetVertexBufferParams(6,layout);int stride=mesh.GetVertexBufferStride(0);var bytes=new byte[6*stride];
        var positions=new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.zero,Vector3.up,Vector3.forward};
        void Write(int at,float value)=>Array.Copy(BitConverter.GetBytes(value),0,bytes,at,4);
        for(int i=0;i<6;i++){var p=positions[i];var n=faceNormals?(i<3?Vector3.forward:Vector3.right):Vector3.up;for(int j=0;j<3;j++){Write(i*stride+j*4,p[j]);Write(i*stride+12+j*4,n[j]);}Write(i*stride+24+(i%3)*4,1);Write(i*stride+40,.25f);Write(i*stride+44,.75f);}
        mesh.SetVertexBufferData(bytes,0,0,bytes.Length);mesh.SetIndexBufferParams(6,IndexFormat.UInt16);mesh.SetIndexBufferData(new ushort[]{0,1,2,3,4,5},0,0,6);mesh.subMeshCount=1;mesh.SetSubMesh(0,new SubMeshDescriptor(0,6));return mesh;
    }
    [TestCase("face-normals-only",true,true)]
    [TestCase("face-order",true,true)]
    [TestCase("coordinate-bit",false,true)]
    [TestCase("reverse-winding",false,false)]
    [TestCase("uv-bit",false,true)]
    [TestCase("color-bit",false,true)]
    [TestCase("nonunit-normal",true,false)]
    [TestCase("wrong-face-normal",true,false)]
    public void FaceOnlyComparatorNeverWaivesGeometryOrOtherVertexAttributes(string change,bool geometry,bool faceNormals)
    {
        var before=Control(false);var after=Control(true);try
        {
            if(change=="face-order")after.triangles=new[]{3,4,5,0,1,2};
            else if(change=="coordinate-bit"){var values=after.vertices;values[1].x=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(values[1].x)+1);after.vertices=values;}
            else if(change=="reverse-winding")after.triangles=new[]{0,2,1,3,4,5};
            else if(change=="uv-bit"){var values=after.uv;values[0].x=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(values[0].x)+1);after.uv=values;}
            else if(change=="color-bit"){var values=after.colors;values[0].r=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(values[0].r)+1);after.colors=values;}
            else if(change=="nonunit-normal"){var values=after.normals;for(int i=0;i<3;i++)values[i]*=2;after.normals=values;}
            else if(change=="wrong-face-normal"){var values=after.normals;for(int i=0;i<3;i++)values[i]=Vector3.up;after.normals=values;}
            var proof=Geometry.GetMethod("Compare").Invoke(null,new object[]{before,after});object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);Assert.That((bool)Field("geometryExact"),Is.EqualTo(geometry),change+": "+JsonUtility.ToJson(proof));Assert.That((bool)Field("nativeFaceNormalsVerified"),Is.EqualTo(faceNormals),change);Assert.That((bool)Field("originalNormalAwareCanonicalExact"),Is.False,"The original paint-normal identity gate is never weakened by this separate intended-normal-change comparator.");TestContext.WriteLine(JsonUtility.ToJson(proof,true));
        }
        finally{UnityEngine.Object.DestroyImmediate(after);UnityEngine.Object.DestroyImmediate(before);}
    }
    [TestCase("Assets/TruckDragonKingdom/VisualCandidates/Face09/Terrain/one.asset","Assets/DiggerData/Scenes/DK-Face09/exact-guid", "Face09",true)]
    [TestCase("Assets/TruckDragonKingdom/Terrain/one.asset","Assets/DiggerData/Scenes/DK-Face09/exact-guid", "Face09",false)]
    [TestCase("Assets/TruckDragonKingdom/VisualCandidates/Face09/Terrain/one.asset","Assets/DiggerData/Scenes/DK-Other/exact-guid", "Face09",false)]
    [TestCase("Assets/TruckDragonKingdom/VisualCandidates/Face09/Terrain/one.asset","Assets/DiggerData/Scenes/DK-Face09/exact-guid", "../Face09",false)]
    public void ProviderAllowsOnlyExactOwnedCandidateIdentity(string terrain,string data,string tag,bool expected)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeFacePolygonizerProvider, Assembly-CSharp-Editor",true);Assert.That((bool)type.GetMethod("IsOwnedCandidateContext").Invoke(null,new object[]{terrain,data,tag}),Is.EqualTo(expected));
    }
    [Test]
    public void UnconfiguredProviderRejectsInsteadOfChangingAnExistingMaster()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeFacePolygonizerProvider, Assembly-CSharp-Editor",true);Assert.That(typeof(Component).IsAssignableFrom(type),Is.False,"The Editor diagnostic factory must not require an attachable or serialized behaviour.");var provider=Activator.CreateInstance(type);var error=Assert.Throws<TargetInvocationException>(()=>type.GetMethod("Init").Invoke(provider,null));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
}
#endif
