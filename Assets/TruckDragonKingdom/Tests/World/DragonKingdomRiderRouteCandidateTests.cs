#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomRiderRouteCandidateTests
{
    static Type Probe=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRiderRouteCandidateProbe, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Probe.GetMethod(method,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
    [TestCase(true)][TestCase(false)]
    public void NativeTerrainHeightGridIncludesNearPeakButExcludesDistantTilePeak(bool near)
    {
        var data=new TerrainData{heightmapResolution=33,size=new Vector3(320,100,320)};var heights=new float[33,33];heights[near?2:30,16]=1;data.SetHeights(0,0,heights);var go=Terrain.CreateTerrainGameObject(data);go.transform.position=new Vector3(-160,17,-160);var original=data.GetHeights(0,0,33,33);
        try{object[] args={go.GetComponent<Terrain>(),new Vector3(-100,200,-140),new Vector3(100,200,-140),5f,0};float top=(float)Call("LocalTerrainCeiling",args);Assert.That(top,Is.EqualTo(near?117:17).Within(.0001));Assert.That((int)args[4],Is.GreaterThan(0));CollectionAssert.AreEqual(original.Cast<float>(),data.GetHeights(0,0,33,33).Cast<float>());Assert.That(go.transform.position,Is.EqualTo(new Vector3(-160,17,-160)));}
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(data);}
    }
    [TestCase(true)][TestCase(false)]
    public void HorizontalCorridorRetainsVerticalHeightAndOnlyRejectsUnrelatedProjection(bool hit)
    {object[] args={new Vector3(-10,50,0),new Vector3(10,200,0),new Bounds(new Vector3(0,500,hit?2:20),new Vector3(2,1,2)),2f,0f,0f};Assert.That(Call("CorridorInterval",args),Is.EqualTo(hit));if(hit){Assert.That((float)args[4],Is.InRange(0,1));Assert.That((float)args[5],Is.GreaterThanOrEqualTo((float)args[4]));}}
    [Test]public void InvalidFootprintCannotReduceTheOriginalNativeRadius()
    {var error=Assert.Throws<TargetInvocationException>(()=>Call("CorridorInterval",new object[]{Vector3.zero,Vector3.one,new Bounds(),-1f,0f,0f}));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}

    [UnityTest,Timeout(1800000)]
    public IEnumerator ActualV518RouteUsesLocalTerrainAndUnchangedNativeSphereWithoutSavingSources()
    {
        var fixture=typeof(DragonKingdomPhysicalCourseConsumerTests);var bootstrap=fixture.GetMethod("OwnedBatchBootstrap",BindingFlags.NonPublic|BindingFlags.Static);const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";string root=Directory.GetParent(Application.dataPath).FullName;Directory.CreateDirectory(Path.Combine(root,generated));AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(string.IsNullOrEmpty(scene.path)){Assert.That((bool)bootstrap.Invoke(null,new object[]{scene}),Is.True,"Preserve arbitrary edited/user scenes; only established batch bootstrap is owned.");Assert.That(EditorSceneManager.SaveScene(scene,generated+"RouteProbeBootstrap-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);}Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);}
        var setup=EditorSceneManager.GetSceneManagerSetup();var bytes=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(Path.Combine(root,s.path)));var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"RouteProbeIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            Call("MeasureActualQualifiedRouteCandidate");yield return null;var evidence=Probe.GetProperty("LastEvidence").GetValue(null);var type=evidence.GetType();Assert.That(type.GetField("sourcesExact").GetValue(evidence),Is.True);Assert.That(type.GetField("allTwelvePhysicalRingsRetained").GetValue(evidence),Is.True);Assert.That(type.GetField("formationSeparated").GetValue(evidence),Is.True);Assert.That(type.GetField("equalFirstGateDistance").GetValue(evidence),Is.True);
            var measured=type.GetField("measured").GetValue(evidence);Assert.That(measured.GetType().GetField("qualifiedSweepRadius").GetValue(measured),Is.EqualTo(51.860015869140625f));Assert.That(measured.GetType().GetField("straightExitDistance").GetValue(measured),Is.EqualTo(55f));Assert.That(measured.GetType().GetField("contactQueries").GetValue(measured),Is.EqualTo(0),"A retained contact is a real candidate rejection; inspect uncapped blocker totals and explicit capped records.");Assert.That(type.GetField("status").GetValue(evidence),Is.EqualTo("DISPOSABLE_LOCAL_ROUTE_CLEAR_REQUIRES_EXPLICIT_POSE_TRANSITION_AND_REAL_FLIGHT"));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var pair in bytes)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(Path.Combine(root,pair.Key)));}
    }
}
#endif
