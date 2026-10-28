using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CampingWaterPresentationTests
{
    static Type Author => Type.GetType("LWS.TruckCamping.Editor.CampingWaterPresentationAuthoring, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] values)=>Author.GetMethod(method).Invoke(null,values);
    const string Timber="Assets/TruckCamping/Models/Camp Haul bound rounded timber.asset";
    [Serializable] sealed class Fact { public int vertices,triangles,topTriangles,degenerateTopUvTriangles,tangentCount;public float minimumUvArea; }
    [Test] public void ActualTimberCapHasEightCollapsedUvTrianglesAndWaterDerivativeFixesEveryTriangleWithoutChangingGeometry()
    {
        string before=(string)Call("Digest",Timber);var source=AssetDatabase.LoadAssetAtPath<Mesh>(Timber);Assert.That(source,Is.Not.Null);
        var original=JsonUtility.FromJson<Fact>((string)Call("MeshDiagnostics",source));Assert.That(original.topTriangles,Is.EqualTo(8));Assert.That(original.degenerateTopUvTriangles,Is.EqualTo(8));Assert.That(original.tangentCount,Is.Zero);
        var derived=(Mesh)Call("CreateTransientWaterMesh",source);
        try
        {
            Call("ValidateDerivedMesh",source,derived);var fixedMesh=JsonUtility.FromJson<Fact>((string)Call("MeshDiagnostics",derived));
            Assert.That(fixedMesh.triangles,Is.EqualTo(original.triangles));Assert.That(fixedMesh.vertices,Is.EqualTo(original.triangles*3));
            Assert.That(fixedMesh.topTriangles,Is.EqualTo(8));Assert.That(fixedMesh.degenerateTopUvTriangles,Is.Zero);Assert.That(fixedMesh.minimumUvArea,Is.GreaterThan(1e-9f));Assert.That(fixedMesh.tangentCount,Is.EqualTo(fixedMesh.vertices));
            Assert.That((string)Call("Digest",Timber),Is.EqualTo(before),"Original timber also owns ramps/scenery; its bytes must remain exact.");
        }
        finally { UnityEngine.Object.DestroyImmediate(derived); }
    }
    [TestCase("lake")] [TestCase("creek")] [TestCase("cascade")]
    public void ActualWorkingFishingAdapterSuppliesReviewedSurfaceAndVendorNormalWithoutVendorMutation(string kind)
    {
        const string sourcePath="Assets/TruckFishing/Materials/Clear fishing river.mat",riverPath="Assets/RiverModeler/ShaderGraph/Default River.mat",normalPath="Assets/RiverModeler/Textures/WaveNormal.png";
        string sourceBefore=(string)Call("Digest",sourcePath),riverBefore=(string)Call("Digest",riverPath),normalBefore=(string)Call("Digest",normalPath);
        var source=AssetDatabase.LoadAssetAtPath<Material>(sourcePath);var material=(Material)Call("CreateTransientWaterMaterial",kind);
        try
        {
            Assert.That(material.shader,Is.SameAs(source.shader));Assert.That(material.shader.isSupported,Is.True);Assert.That(material.GetTexture("_BumpMap"),Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)));
            Assert.That(material.GetTexture("_BaseMap"),Is.SameAs(kind=="cascade"?AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/WaterFoam.png"):null));Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(kind=="cascade"?new Color(.86f,.96f,1,.92f):source.GetColor("_BaseColor")));Assert.That(material.GetFloat("_Smoothness"),Is.EqualTo(kind=="cascade"?.36f:.72f));Assert.That(material.GetFloat("_BumpScale"),Is.EqualTo(kind=="cascade"?.1f:.2f));
            Assert.That(material.GetFloat("_ZWrite"),Is.Zero);Assert.That(material.GetFloat("_Surface"),Is.EqualTo(1));Assert.That(material.GetFloat("_Cull"),Is.EqualTo(kind=="cascade"?0:2));Assert.That(material.renderQueue,Is.EqualTo(3000));
            Assert.That((string)Call("Digest",sourcePath),Is.EqualTo(sourceBefore));Assert.That((string)Call("Digest",riverPath),Is.EqualTo(riverBefore));Assert.That((string)Call("Digest",normalPath),Is.EqualTo(normalBefore));
        }
        finally { UnityEngine.Object.DestroyImmediate(material); }
    }
    [TestCase("\n",true)] [TestCase("\r\n",true)] [TestCase("\n",false)] [TestCase("\r\n",false)]
    public void ExactNativeReferenceRewritePreservesEveryOtherFieldAndLineEnding(string newline,bool mesh)
    {
        string oldGuid=new string('a',32),nextGuid=new string('b',32);
        string field=mesh?"  m_Mesh: {fileID: 4300000, guid: "+oldGuid+", type: 2}":"  m_Materials:"+newline+"  - {fileID: 2100000, guid: "+oldGuid+", type: 2}";
        string source="--- !u!"+(mesh?33:23)+" &42"+newline+"Test:"+newline+field+newline+"  unrelated: {guid: "+oldGuid+"}"+newline;
        string result=(string)Call("RewriteWaterReference",source,mesh,oldGuid,nextGuid);
        Assert.That(result,Does.Contain(field.Replace(oldGuid,nextGuid)));Assert.That(result,Does.Contain("  unrelated: {guid: "+oldGuid+"}"+newline));
        Assert.That((string)Call("RewriteWaterReference",result,mesh,nextGuid,oldGuid),Is.EqualTo(source));
        Assert.That(Assert.Throws<TargetInvocationException>(()=>Call("RewriteWaterReference",source,mesh,new string('c',32),nextGuid)).InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test] public void UnsupportedMeshIsRefusedInsteadOfDiscardingUnknownStreams()
    {
        var mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward};mesh.normals=new[]{Vector3.up,Vector3.up,Vector3.up};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up};mesh.triangles=new[]{0,2,1};
        try { Assert.That(Assert.Throws<TargetInvocationException>(()=>Call("CreateTransientWaterMesh",mesh)).InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(mesh.vertexCount,Is.EqualTo(3)); }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }
}
public sealed class CampingWaterInstalledPresentationTests
{
    [Test] public void CurrentWaterIsExactAuthorizedNineteenRendererDeltaFromRetainedR52WithOriginalPhysicsAndRampHistory()
    {
        var author=Type.GetType("LWS.TruckCamping.Editor.CampingWaterPresentationAuthoring, Assembly-CSharp-Editor",true);
        string result=(string)author.GetMethod("VerifyWaterDescendsFrom").Invoke(null,new object[]{"a8448bc181521b1f01734f143c0bbf108893636e47324d44c65875b7574e0cdd"});
        Assert.That(result,Is.EqualTo("ONLY_19_WATER_MESH_AND_19_MATERIAL_REFERENCES_CHANGED_ALL_OTHER_WORLD_BYTES_EXACT"));
        var history=Type.GetType("LWS.TruckCamping.Editor.CampingHammockRampAuthoring, Assembly-CSharp-Editor",true);
        Assert.That((string)history.GetMethod("VerifyRetainedShorteningAndCurrentChain").Invoke(null,null),Does.Contain("STRICT_CURRENT_EXTENDED_TRANSITION_FROM_EXACT_R44_ARTIFACT"));
    }
}
