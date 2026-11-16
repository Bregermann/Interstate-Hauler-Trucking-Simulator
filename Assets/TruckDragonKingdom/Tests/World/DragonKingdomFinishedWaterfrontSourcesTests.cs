#if UNITY_EDITOR && VISTA
using System;
using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public sealed class DragonKingdomFinishedWaterfrontSourcesTests
{
    [Test]
    public void NativeGangwayJointUsesActualWalkableTrianglesWhenTheNorthernBoundsEdgeContainsNoMesh()
    {
        var root=new GameObject("Temporary actual open pad mesh control");var mesh=new Mesh();try
        {
            mesh.vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1),new Vector3(0,4,3)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>();var collider=root.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();Assert.That(collider.Raycast(new Ray(new Vector3(0,5,2.95f),Vector3.down),out _,8),Is.False,"Original bounds-derived point reproduces the empty edge");string before=UnityEditor.EditorJsonUtility.ToJson(collider);var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var point=(Vector3)type.GetMethod("ActualWalkableWaterfrontPad",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root.transform,mesh.bounds});Assert.That(point.y,Is.EqualTo(0).Within(.0001f));Assert.That(Mathf.Abs(point.x),Is.LessThanOrEqualTo(1));Assert.That(Mathf.Abs(point.z),Is.LessThanOrEqualTo(1));Assert.That(collider.Raycast(new Ray(point+Vector3.up,Vector3.down),out var hit,2),Is.True);Assert.That(hit.normal.y,Is.GreaterThan(.5f));Assert.That(UnityEditor.EditorJsonUtility.ToJson(collider),Is.EqualTo(before));Assert.That(root.transform.position.Equals(Vector3.zero),Is.True);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);Physics.SyncTransforms();}
    }
    [TestCase(0f,true),TestCase(.52f,true),TestCase(.8f,false)]
    public void NativeBearingKeepsOriginalContactsAndOnlyResolvesRealNearbyUndersidesInsideThePileFootprint(float x,bool expected)
    {
        var root=new GameObject("Temporary native underside boundary control");try
        {
            var collider=root.AddComponent<BoxCollider>();collider.size=Vector3.one;Physics.SyncTransforms();string before=UnityEditor.EditorJsonUtility.ToJson(collider);var station=new Vector3(x,0,0);object[] args={new Collider[]{collider},station,-2f,null,null};var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);bool found=(bool)type.GetMethod("TryWaterfrontNativeBearing",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);Assert.That(found,Is.EqualTo(expected));if(found){var actual=(Vector3)args[3];var hit=(RaycastHit)args[4];Assert.That(Vector3.Distance(actual,station),Is.LessThanOrEqualTo(.2501f));Assert.That(hit.collider,Is.SameAs(collider));Assert.That(hit.normal.y,Is.LessThan(-.35f));if(x==0)Assert.That(actual.Equals(station),Is.True);}Assert.That(UnityEditor.EditorJsonUtility.ToJson(collider),Is.EqualTo(before));Assert.That(root.transform.position.Equals(Vector3.zero),Is.True);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);Physics.SyncTransforms();}
    }
    [Test,Timeout(120000)]
    public void ActualMissingOptionalBeamPrefabResolvesToExistingProductionFbxAndProtectsItsRealDependencyClosure()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);string proof=(string)type.GetMethod("CaptureFinishedWaterfrontTimberSourceProbe").Invoke(null,null);
        Assert.That(proof,Does.Contain("\"requestedPrefabPresent\": false"),"This frozen source cohort exercises the actual absent-prefab/present-model branch.");Assert.That(proof,Does.Contain("/Models/SM_Prop_Beam_01.fbx"));Assert.That(proof,Does.Contain("\"resolverMatchesProductionPlacement\": true"));Assert.That(proof,Does.Contain("\"sourceAndMetadataBytesExact\": true"));Assert.That(proof,Does.Contain("\"temporarySceneClosed\": true"));TestContext.WriteLine(proof);
    }
}
#endif
