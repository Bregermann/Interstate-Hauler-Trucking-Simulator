#if UNITY_EDITOR && VISTA
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonKingdomRidgeMeshSeatingTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static void Set(object value,string field,object data)=>value.GetType().GetField(field).SetValue(value,data);
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    [Test]
    public void LargestActualRefugeFootprintsAreAllocatedFirstWithoutReorderingOrReplacingOtherRegionItems()
    {
        var itemType=Integration.GetNestedType("RidgeItem",BindingFlags.NonPublic);var list=(System.Collections.IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(itemType));
        object Item(string region,string name,float x,float z,bool rock)
        {var value=Activator.CreateInstance(itemType,true);var record=Activator.CreateInstance(Integration.GetNestedType("Placement"));Set(record,"region",region);Set(record,"name",name);Set(value,"record",record);Set(value,"rock",rock);Set(value,"primaryBounds",new Bounds(Vector3.zero,new Vector3(x,10,z)));return value;}
        var small=Item("refuge","small",2,2,true);var forest=Item("forest","forest",100,100,false);var large=Item("refuge","large",24,24,true);var mountain=Item("mountains","mountain",200,200,true);var medium=Item("refuge","medium",10,10,true);
        foreach(var value in new[]{small,forest,large,mountain,medium})list.Add(value);
        var ordered=(Array)Integration.GetMethod("OrderRefugeRidgePacking",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{list});Assert.That(ordered.Length,Is.EqualTo(5));
        Assert.That(ordered.GetValue(0),Is.SameAs(large));Assert.That(ordered.GetValue(1),Is.SameAs(forest));Assert.That(ordered.GetValue(2),Is.SameAs(medium));Assert.That(ordered.GetValue(3),Is.SameAs(mountain));Assert.That(ordered.GetValue(4),Is.SameAs(small));
        Assert.That(list[0],Is.SameAs(small),"The original ordered inventory is preserved");
    }
    [TestCase("refuge",2048)][TestCase("mountains",1024)][TestCase("nests",256)]
    public void RefugeFallbackPreservesEveryPriorTrialAndRemainsInsideExplicitBounded128MetreShoulder(string region,int count)
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic;var limit=Integration.GetMethod("RidgeTrialLimit",flags);var radius=Integration.GetMethod("RidgeTrialRadius",flags);
        Assert.That((int)limit.Invoke(null,new object[]{region,true,false}),Is.EqualTo(count));
        for(int i=0;i<256;i++)Assert.That((float)radius.Invoke(null,new object[]{region,true,false,i}),Is.EqualTo(i==0?0:4*Mathf.Sqrt(i)));
        if(region=="refuge")for(int i=256;i<1024;i++)Assert.That((float)radius.Invoke(null,new object[]{region,true,false,i}),Is.GreaterThan(0).And.LessThanOrEqualTo(4*Mathf.Sqrt(255)));
        if(region=="refuge")for(int i=1024;i<2048;i++)Assert.That((float)radius.Invoke(null,new object[]{region,true,false,i}),Is.GreaterThanOrEqualTo(64).And.LessThanOrEqualTo(128));
        Assert.That((int)limit.Invoke(null,new object[]{"refuge",false,true}),Is.EqualTo(256),"Do not broaden unrelated tree searches");
    }
    [TestCase(1.5f,true)][TestCase(1.02f,false)][TestCase(.5f,false)]
    public void ActualPrimaryVerticesTouchNativeSlopedTerrainWithoutEditingTheHeightfieldOrChangingExistingGroundedModels(float initialY,bool moved)
    {
        var incoming=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();TerrainData data=null;GameObject ground=null,root=null;
        try
        {
            data=new TerrainData{heightmapResolution=33,size=new Vector3(100,10,100)};var heights=new float[33,33];for(int z=0;z<33;z++)for(int x=0;x<33;x++)heights[z,x]=x/32f*.2f;data.SetHeights(0,0,heights);
            var before=data.GetHeights(0,0,33,33);ground=Terrain.CreateTerrainGameObject(data);SceneManager.MoveGameObjectToScene(ground,scene);root=new GameObject("Actual sampled primary vertex envelope");SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(50,initialY,50);var position=root.transform.position;var rotation=root.transform.rotation;var scale=root.transform.localScale;
            var item=Activator.CreateInstance(Integration.GetNestedType("RidgeItem",BindingFlags.NonPublic),true);var vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,1),new Vector3(-1,3.8f,-1),new Vector3(1,3.8f,1)};Set(item,"root",root.transform);Set(item,"localSamples",vertices);
            var proof=Activator.CreateInstance(Integration.GetNestedType("RidgeCompositionPart"));Set(proof,"modelHeight",3.8f);var terrains=new[]{ground.GetComponent<Terrain>()};var flags=BindingFlags.Static|BindingFlags.NonPublic;
            Integration.GetMethod("MeasureRidgeMesh",flags).Invoke(null,new object[]{item,terrains,proof});float coarse=(float)Read(proof,"minimumMeshGroundGap");float correction=(float)Integration.GetMethod("SeatFloatingRidgePrimaryMesh",flags).Invoke(null,new object[]{item,terrains,proof});
            Assert.That(correction>0,Is.EqualTo(moved));
            if(moved){Assert.That(coarse,Is.GreaterThan(.25f));Assert.That((float)Read(proof,"minimumMeshGroundGap"),Is.EqualTo(0).Within(.00001f));Assert.That((bool)Read(proof,"visibleAndSupported"),Is.True);Assert.That(position.y-root.transform.position.y,Is.EqualTo(correction).Within(.00001f));}
            else Assert.That(root.transform.position,Is.EqualTo(position));
            Assert.That(root.transform.position.x,Is.EqualTo(position.x));Assert.That(root.transform.position.z,Is.EqualTo(position.z));Assert.That(root.transform.rotation,Is.EqualTo(rotation));Assert.That(root.transform.localScale,Is.EqualTo(scale));Assert.That((float)Read(proof,"primaryMeshSeatCorrection"),Is.EqualTo(correction));
            var after=data.GetHeights(0,0,33,33);for(int z=0;z<33;z++)for(int x=0;x<33;x++)Assert.That(after[z,x],Is.EqualTo(before[z,x]));Assert.That(vertices[0],Is.EqualTo(new Vector3(-1,0,-1)));
        }
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(ground!=null)UnityEngine.Object.DestroyImmediate(ground);if(data!=null)UnityEngine.Object.DestroyImmediate(data);EditorSceneManager.ClosePreviewScene(scene);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(incoming));}
    }
    [Test]
    public void ActualAuthoredCarrierSeatsFloatingPrimaryMeshWithoutChangingTheArtistsLocalTrs()
    {
        var incoming=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();TerrainData data=null;GameObject ground=null,carrier=null;
        try
        {
            data=new TerrainData{heightmapResolution=33,size=new Vector3(100,10,100)};var heights=new float[33,33];for(int z=0;z<33;z++)for(int x=0;x<33;x++)heights[z,x]=x/32f*.2f;data.SetHeights(0,0,heights);var originalHeights=data.GetHeights(0,0,33,33);ground=Terrain.CreateTerrainGameObject(data);SceneManager.MoveGameObjectToScene(ground,scene);
            carrier=new GameObject("Authored terrain support actual-local-TRS-control");SceneManager.MoveGameObjectToScene(carrier,scene);carrier.transform.position=new Vector3(50,.8f,50);var child=new GameObject("Original authored source-local object");child.transform.SetParent(carrier.transform,false);child.transform.localPosition=new Vector3(.15f,.7f,-.1f);var local=child.transform.localPosition;var rotation=child.transform.localRotation;var scale=child.transform.localScale;
            var item=Activator.CreateInstance(Integration.GetNestedType("RidgeItem",BindingFlags.NonPublic),true);Set(item,"root",child.transform);var vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,1),new Vector3(-1,4,-1),new Vector3(1,4,1)};Set(item,"localSamples",vertices);Set(item,"primaryRootPosition",child.transform.position);Set(item,"primaryBounds",new Bounds(child.transform.position+Vector3.up*2,new Vector3(2,4,2)));
            var proof=Activator.CreateInstance(Integration.GetNestedType("RidgeCompositionPart"));Set(proof,"modelHeight",4f);var terrains=new[]{ground.GetComponent<Terrain>()};var flags=BindingFlags.Static|BindingFlags.NonPublic;Integration.GetMethod("MeasureRidgeMesh",flags).Invoke(null,new object[]{item,terrains,proof});Assert.That((float)Read(proof,"minimumMeshGroundGap"),Is.GreaterThan(.25f));
            float moved=(float)Integration.GetMethod("SeatFloatingAuthoredNaturalCarrier",flags).Invoke(null,new object[]{item,terrains,proof});Assert.That(moved,Is.GreaterThan(0));Assert.That((float)Read(proof,"minimumMeshGroundGap"),Is.EqualTo(0).Within(.00001f));Assert.That(child.transform.localPosition.Equals(local),Is.True);Assert.That(child.transform.localRotation.Equals(rotation),Is.True);Assert.That(child.transform.localScale.Equals(scale),Is.True);Assert.That(carrier.transform.position.x,Is.EqualTo(50));Assert.That(carrier.transform.position.z,Is.EqualTo(50));
            var after=data.GetHeights(0,0,33,33);for(int z=0;z<33;z++)for(int x=0;x<33;x++)Assert.That(after[z,x],Is.EqualTo(originalHeights[z,x]));
        }
        finally{if(carrier!=null)UnityEngine.Object.DestroyImmediate(carrier);if(ground!=null)UnityEngine.Object.DestroyImmediate(ground);if(data!=null)UnityEngine.Object.DestroyImmediate(data);EditorSceneManager.ClosePreviewScene(scene);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(incoming));}
    }

    [Test]
    public void WholeParentQuarterTurnRefreshesActualWorldMeshBoundsWhilePreservingEverySourceLocalTransform()
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject parent=null;
        try
        {
            parent=new GameObject("Actual authored parent orientation control");SceneManager.MoveGameObjectToScene(parent,scene);parent.transform.SetPositionAndRotation(new Vector3(200,30,300),Quaternion.Euler(0,30,0));var child=GameObject.CreatePrimitive(PrimitiveType.Cube);child.transform.SetParent(parent.transform,false);child.transform.localPosition=new Vector3(1,2,3);child.transform.localScale=new Vector3(2,4,6);var local=child.transform.localPosition;var rotation=child.transform.localRotation;var scale=child.transform.localScale;var mesh=child.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
            var item=Activator.CreateInstance(Integration.GetNestedType("RidgeItem",BindingFlags.NonPublic),true);Set(item,"root",child.transform);var refresh=Integration.GetMethod("RefreshAuthoredNaturalGeometry",BindingFlags.Static|BindingFlags.NonPublic);refresh.Invoke(null,new[]{item});var before=(Bounds)Read(item,"primaryBounds");
            parent.transform.rotation=Quaternion.Euler(0,120,0);refresh.Invoke(null,new[]{item});var after=(Bounds)Read(item,"primaryBounds");Assert.That(after.size.x,Is.EqualTo(before.size.z).Within(.0001f));Assert.That(after.size.z,Is.EqualTo(before.size.x).Within(.0001f));Assert.That(after.size.y,Is.EqualTo(before.size.y).Within(.0001f));Assert.That(Vector3.Distance(after.center,child.transform.position),Is.LessThan(.0001f));
            Assert.That(child.transform.localPosition.Equals(local),Is.True);Assert.That(child.transform.localRotation.Equals(rotation),Is.True);Assert.That(child.transform.localScale.Equals(scale),Is.True);Assert.That(mesh.vertices,Is.EqualTo(vertices));
        }
        finally{if(parent!=null)UnityEngine.Object.DestroyImmediate(parent);EditorSceneManager.ClosePreviewScene(scene);}
    }

}
#endif
