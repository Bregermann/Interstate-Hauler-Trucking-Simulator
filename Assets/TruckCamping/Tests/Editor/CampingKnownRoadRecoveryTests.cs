using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CampingKnownRoadRecoveryTests
{
    const string Original="Assets/TruckCamping/Models/Camp Haul scenic woodland loop EasyRoads mesh 0.asset";
    static Type Author=>Type.GetType("LWS.TruckCamping.Editor.CampingWildernessAuthoring, Assembly-CSharp-Editor",true);
    static object Invoke(string method,params object[] args)
    {
        try{return Author.GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
        catch(TargetInvocationException error){throw error.InnerException;}
    }
    [Test]public void ReviewedNativeVariationCopiesEveryActualVendorUvStreamWithoutChangingAssetGuidOrName()
    {
        var original=AssetDatabase.LoadAssetAtPath<Mesh>(Original);Assert.That(original,Is.Not.Null);
        var source=UnityEngine.Object.Instantiate(original);var target=UnityEngine.Object.Instantiate(original);
        string folder="Assets/TemporaryCampingKnownRoad_"+Guid.NewGuid().ToString("N");AssetDatabase.CreateFolder("Assets",Path.GetFileName(folder));string path=folder+"/retained.asset";
        try
        {
            target.name="Exact retained original road identity";AssetDatabase.CreateAsset(target,path);AssetDatabase.SaveAssets();var meta=File.ReadAllBytes(path+".meta");string guid=AssetDatabase.AssetPathToGUID(path);string persistedName=target.name;
            var points=source.vertices;points[0].y+=.03f;source.vertices=points;source.RecalculateBounds();
            var uv=new List<Vector4>();source.GetUVs(3,uv);Assert.That(uv.Count,Is.EqualTo(source.vertexCount),"Actual saved vendor road has UV3; copying only uv0/uv1 would lose it.");uv[0]+=new Vector4(.0001f,.0002f,0,0);source.SetUVs(3,uv);
            Assert.That((float)Invoke("ValidateReviewedRoadGeometry",source,target),Is.InRange(.0299f,.0301f));Invoke("CopyReviewedRoadGeometry",source,target);AssetDatabase.SaveAssets();
            Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(guid));CollectionAssert.AreEqual(meta,File.ReadAllBytes(path+".meta"));Assert.That(target.name,Is.EqualTo(persistedName),"Preserve the actual persisted identity after CreateAsset, which may adopt the asset filename.");
            CollectionAssert.AreEqual(source.vertices,target.vertices);CollectionAssert.AreEqual(source.normals,target.normals);CollectionAssert.AreEqual(source.tangents,target.tangents);CollectionAssert.AreEqual(source.GetIndices(0),target.GetIndices(0));
            var copied=new List<Vector4>();for(int i=0;i<8;i++){uv.Clear();copied.Clear();source.GetUVs(i,uv);target.GetUVs(i,copied);CollectionAssert.AreEqual(uv,copied,"Complete actual source UV channel"+i);}
        }
        finally{UnityEngine.Object.DestroyImmediate(source);AssetDatabase.DeleteAsset(folder);}
    }
    [TestCase("terrain8000-pass0")][TestCase("terrain1000-pass1")][TestCase("terrain1000-pass2")]
    public void ActualCompleteNativePositionBuffersStayWithinMeasuredEnvelopeAndPreserveOriginalInput(string pass)
    {
        var original=AssetDatabase.LoadAssetAtPath<Mesh>(Original);Assert.That(original,Is.Not.Null);
        string before=EditorJsonUtility.ToJson(original);var source=UnityEngine.Object.Instantiate(original);
        string path="Logs/TruckSports/Impossible07/CampingNavigationDiagnosis/axis-v2-"+pass+"-source-positions.float32le";
        try
        {
            Assert.That(File.Exists(path),Is.True,"Requires retained exact original/corrected/repeated native measurement, never fabricated positions.");
            var data=File.ReadAllBytes(path);Assert.That(data.Length,Is.EqualTo(original.vertexCount*12));var points=new Vector3[original.vertexCount];
            using(var stream=new MemoryStream(data))using(var reader=new BinaryReader(stream))for(int i=0;i<points.Length;i++)points[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
            source.vertices=points;source.RecalculateBounds();
            Assert.That((float)Invoke("ValidateReviewedRoadGeometry",source,original),Is.LessThanOrEqualTo(.05f),"Actual vendor positions must pass the narrowly measured, hash-gated recovery envelope.");
            Assert.That(EditorJsonUtility.ToJson(original),Is.EqualTo(before),"Replay validation never mutates the actual retained road.");
        }
        finally{UnityEngine.Object.DestroyImmediate(source);}
    }
    [TestCase("horizontal")][TestCase("vertical")][TestCase("indices")][TestCase("nonfiniteUv")][TestCase("colors")]
    public void AnythingOutsideReviewedRecipeRejectsBeforeCopyAndLeavesOriginalMeshExact(string mutation)
    {
        var original=AssetDatabase.LoadAssetAtPath<Mesh>(Original);Assert.That(original,Is.Not.Null);var source=UnityEngine.Object.Instantiate(original);
        var before=File.ReadAllBytes(Original);string json=EditorJsonUtility.ToJson(original);bool dirty=EditorUtility.IsDirty(original);
        try
        {
            if(mutation=="horizontal"||mutation=="vertical"){var points=source.vertices;if(mutation=="horizontal")points[0].x+=.003f;else points[0].y+=.051f;source.vertices=points;source.RecalculateBounds();}
            if(mutation=="indices"){var indices=source.GetIndices(0);int keep=indices[0];indices[0]=indices[1];indices[1]=keep;source.SetIndices(indices,source.GetTopology(0),0);}
            if(mutation=="nonfiniteUv"){var uv=new List<Vector4>();source.GetUVs(3,uv);uv[0]=new Vector4(float.NaN,0,0,0);source.SetUVs(3,uv);}
            if(mutation=="colors"){var colors=source.colors;colors[0]=Color.magenta;source.colors=colors;}
            Assert.Throws<InvalidOperationException>(()=>Invoke("CopyReviewedRoadGeometry",source,original));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(Original));Assert.That(EditorJsonUtility.ToJson(original),Is.EqualTo(json));Assert.That(EditorUtility.IsDirty(original),Is.EqualTo(dirty));
        }
        finally{UnityEngine.Object.DestroyImmediate(source);}
    }
    [Test]public void NewRecoveryCannotBeInvokedOutsideExactKnownPartialResume()
    {
        StringAssert.Contains("exclusive",Assert.Throws<InvalidOperationException>(()=>Invoke("RegenerateExactReviewedRoad",(object)null)).Message);
    }
}
