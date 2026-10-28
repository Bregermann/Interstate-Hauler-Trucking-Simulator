#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TeaLawnUvTests
{
    static Type Author=>Type.GetType("LWS.TruckTeaParty.Editor.TeaLawnUvAuthoring, Assembly-CSharp-Editor",true);
    static object Call(string name,params object[] args)=>Author.GetMethod(name).Invoke(null,args);
    [Test] public void ActualServingGeometryGetsValidCapUvsAndTangentFramesWithoutChangingOriginalAssetOrTriangleGeometry()
    {
        const string source="Assets/TruckTeaParty/Models/Original bevelled serving furniture.asset";
        string before=(string)Call("Hash",source);var mesh=(Mesh)Call("CreateTransient");
        try
        {
            var original=AssetDatabase.LoadAssetAtPath<Mesh>(source);Assert.That(mesh,Is.Not.SameAs(original));
            var vertices=original.vertices;var indices=original.triangles;Assert.That(mesh.vertices.Length,Is.EqualTo(indices.Length));Assert.That(mesh.tangents.Length,Is.EqualTo(indices.Length));
            for(int i=0;i<indices.Length;i++)Assert.That(mesh.vertices[i],Is.EqualTo(vertices[indices[i]]));
            Assert.That(mesh.bounds,Is.EqualTo(original.bounds));Assert.That((string)Call("Hash",source),Is.EqualTo(before));
            var uv=mesh.uv;for(int i=0;i<uv.Length;i+=3)Assert.That(Mathf.Abs((uv[i+1].x-uv[i].x)*(uv[i+2].y-uv[i].y)-(uv[i+1].y-uv[i].y)*(uv[i+2].x-uv[i].x)),Is.GreaterThan(1e-9f));
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }
    [TestCase("\n")][TestCase("\r\n")]
    public void OnlyExactLawnReferenceChangesWithEveryOtherDocumentAndLineEndingRetained(string newline)
    {
        string a=new string('a',32),b=new string('b',32);
        string doc="--- !u!33 &213497243"+newline+"MeshFilter:"+newline+"  m_GameObject: {fileID: 213497239}"+newline+"  m_Mesh: {fileID: 4300000, guid: "+a+", type: 2}"+newline;
        string prefix="unrelated: "+a+newline,suffix="--- !u!65 &123"+newline+"BoxCollider:"+newline+"  unchanged: "+a+newline;
        string text=prefix+doc+suffix;string changed=(string)Call("RewriteOnlyLawnMesh",text,a,b);
        Assert.That(changed,Does.StartWith(prefix));Assert.That(changed,Does.EndWith(suffix));Assert.That((string)Call("RewriteOnlyLawnMesh",changed,b,a),Is.EqualTo(text));
        Assert.That(Assert.Throws<TargetInvocationException>(()=>Call("RewriteOnlyLawnMesh",text,b,a)).InnerException,Is.TypeOf<InvalidOperationException>());
    }
}
public sealed class TeaLawnInstalledUvTests
{
    [Test] public void SavedLawnDerivativeLeavesOriginalFurnitureAndEveryOtherSavedSceneByteIntact()
    { Type.GetType("LWS.TruckTeaParty.Editor.TeaLawnUvAuthoring, Assembly-CSharp-Editor",true).GetMethod("VerifyInstalled").Invoke(null,null); }
}
#endif
