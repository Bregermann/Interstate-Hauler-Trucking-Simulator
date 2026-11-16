#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

public sealed class DragonRiderAuthoredCabMountTests
{
    const string Source="Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab";
    readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
    readonly Dictionary<string,string> sourceBytes=new Dictionary<string,string>();
    Scene incoming,scratch;
    string incomingContext;bool contextCaptured;
    static string SceneContext()=>string.Join("|",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var scene=SceneManager.GetSceneAt(i);return scene.handle+":"+scene.path+":"+scene.isLoaded+":"+scene.isDirty;}));
    Type Match=>Type.GetType("LWS.TruckDragonRiders.DragonRidersMatch, Assembly-CSharp",true);
    static string Hash(string path)
    {
        var authority=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true).GetMethod("Hash",BindingFlags.Static|BindingFlags.NonPublic);
        return (string)authority.Invoke(null,new object[]{Path.Combine(Path.GetDirectoryName(Application.dataPath),path)});
    }
    GameObject Object(string name){var value=new GameObject(name);value.SetActive(false);owned.Add(value);SceneManager.MoveGameObjectToScene(value,scratch);Assert.That(value.scene,Is.EqualTo(scratch),"Owned CAB control root must belong to the registered scratch scene.");return value;}
    [SetUp]public void CaptureActualSourceBytes()
    {
        incoming=default;scratch=default;incomingContext=null;contextCaptured=false;sourceBytes.Clear();owned.Clear();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Source);Assert.That(prefab,Is.Not.Null);var mesh=prefab.GetComponentsInChildren<MeshFilter>(true).Single(value=>value.name=="Cab").sharedMesh;
        foreach(var path in new[]{Source,Source+".meta",AssetDatabase.GetAssetPath(mesh),AssetDatabase.GetAssetPath(mesh)+".meta"}.Distinct())sourceBytes.Add(path,Hash(path));
        incoming=SceneManager.GetActiveScene();
        // Match the established batch-only registered additive-scene harness.
        // Never save dirty or interactive untitled user work.
        const string generated="Assets/TruckDragonRiders/Tests/Generated/AuthoredCabHarness/";
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var existing=SceneManager.GetSceneAt(i);if(!string.IsNullOrEmpty(existing.path))continue;
            if(!Application.isBatchMode||existing.isDirty)Assert.Ignore("Preserving interactive or dirty untitled scene; this native additive control requires a saved runner scene.");
            string physical=Path.Combine(Path.GetDirectoryName(Application.dataPath),generated);Directory.CreateDirectory(physical);Assert.That(Directory.Exists(physical),Is.True,"Actual project harness directory: "+physical);AssetDatabase.Refresh();string harness=generated+"AuthoredCabRunner-"+Guid.NewGuid().ToString("N")+".unity";
            Assert.That(EditorSceneManager.SaveScene(existing,harness),Is.True,"Save only the clean isolated batch runner, never user work.");TestContext.WriteLine("BATCH_ONLY_AUTHORED_CAB_RUNNER "+harness);
        }
        incomingContext=SceneContext();contextCaptured=true;
        scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        Assert.That(scratch.IsValid()&&scratch.isLoaded,Is.True,"Registered additive CAB control scene must be loaded.");
        TestContext.WriteLine("AUTHORED_CAB_SCENE_SETUP scratch="+scratch.handle+" activeBefore="+SceneManager.GetActiveScene().handle);
        if(SceneManager.GetActiveScene()!=scratch)EditorSceneManager.SetActiveScene(scratch);
        Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(scratch),"Editor must actually activate the registered CAB control scene.");
    }
    [TearDown]public void CleanupAndKeepActualSourceExact()
    {
        try{foreach(var value in owned.AsEnumerable().Reverse())if(value!=null)UnityEngine.Object.DestroyImmediate(value);owned.Clear();}
        finally
        {
            if(scratch.IsValid()&&scratch.isLoaded)Assert.That(EditorSceneManager.CloseScene(scratch,true),Is.True,"Close only the owned additive CAB control scene.");
            if(incoming.IsValid()&&incoming.isLoaded)
            {
                if(SceneManager.GetActiveScene()!=incoming)EditorSceneManager.SetActiveScene(incoming);
                Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(incoming),"Restore the actual incoming Editor scene, including after partial Setup.");
            }
        }
        if(contextCaptured)Assert.That(SceneContext(),Is.EqualTo(incomingContext),"Registered incoming scenes, dirty flags and paths must survive the additive control exactly.");
        foreach(var row in sourceBytes)Assert.That(Hash(row.Key),Is.EqualTo(row.Value),"Original NWH/cab/mesh source bytes must remain exact.");
    }
    MeshFilter ExistingAuthoredCockpit(out Camera eye,out MeshFilter actualSourceCab,out Camera actualSourceEye)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Source);actualSourceCab=prefab.GetComponentsInChildren<MeshFilter>(true).Single(value=>value.name=="Cab");actualSourceEye=prefab.GetComponentsInChildren<Camera>(true).Single(value=>value.name=="Vehicle Camera Driver");
        var go=Object("Actual existing Sports cockpit mapping test");eye=go.AddComponent<Camera>();eye.transform.SetPositionAndRotation(new Vector3(51,203,-79),Quaternion.Euler(17,54,-4));
        Type.GetType("LWS.TruckSports.Editor.SportsChaosAuthoring, Assembly-CSharp-Editor",true).GetMethod("Cab").Invoke(null,new object[]{eye,"Rider read-only cab test",0,26});
        return go.GetComponentsInChildren<MeshFilter>(true).Single(value=>value.name=="Taxi cockpit \u00b7 Cab");
    }
    bool Map(MeshFilter reference,Transform eye,MeshFilter live,out Vector3 local)
    {object[] args={reference,eye,live,Vector3.zero};bool result=(bool)Match.GetMethod("TryMapAuthoredCabEye").Invoke(null,args);local=(Vector3)args[3];return result;}
    [Test]public void ExistingActualNwhCockpitMapsItsAuthoredEyeThroughTheSameComposedMesh()
    {
        var reference=ExistingAuthoredCockpit(out var eye,out var original,out var driver);var live=Object("Actual shared Cab mesh at composed mount pose").AddComponent<MeshFilter>();live.sharedMesh=original.sharedMesh;live.transform.SetPositionAndRotation(new Vector3(33,233,-117),Quaternion.Euler(-16,128,4));live.transform.localScale=Vector3.one*1.15f;
        Assert.That(Map(reference,eye.transform,live,out var local),Is.True);var sourceLocal=original.transform.InverseTransformPoint(driver.transform.position);Assert.That(Vector3.Distance(local,sourceLocal),Is.LessThan(.0001f),"Derive the real original driver eye, never the old low generic fallback.");
        Assert.That(Vector3.Distance(live.transform.TransformPoint(local),live.transform.TransformPoint(sourceLocal)),Is.LessThan(.0002f));Assert.That(original.sharedMesh.bounds.Contains(local),Is.True);Assert.That(Vector3.Distance(local,new Vector3(-.17f,1.3f,.65f)),Is.GreaterThan(.5f));
    }
    [Test]public void IdenticalLookingButDifferentMeshIdentityCannotAcquireAnAuthoredMount()
    {
        var reference=ExistingAuthoredCockpit(out var eye,out var original,out _);var live=Object("Unqualified copy of Cab geometry").AddComponent<MeshFilter>();var duplicate=UnityEngine.Object.Instantiate(original.sharedMesh);owned.Add(duplicate);live.sharedMesh=duplicate;
        CollectionAssert.AreEqual(original.sharedMesh.vertices,duplicate.vertices);Assert.That(Map(reference,eye.transform,live,out _),Is.False,"Geometry resemblance cannot replace actual compatible shared source identity.");
    }
    [Test]public void AnEyeOutsideTheActualCabBoundsCannotBecomeASeatMapping()
    {
        var reference=ExistingAuthoredCockpit(out _,out var original,out _);var live=Object("Original matching Cab geometry").AddComponent<MeshFilter>();live.sharedMesh=original.sharedMesh;var wrong=Object("Unqualified outside camera").transform;wrong.position=reference.transform.TransformPoint(original.sharedMesh.bounds.max+Vector3.one*10);
        Assert.That(Map(reference,wrong,live,out _),Is.False,"Do not make an outside-cab eye into a comfort correction.");
    }
    [TestCase(false)][TestCase(true)]public void ExistingDesignerMountRemainsExactEvenForAnUnsupportedBody(bool candidateEnabled)
    {
        var go=Object("Existing designer Rider cab authority");var match=go.AddComponent(Match);var views=go.AddComponent(Type.GetType("LWS.TruckSports.SportsDrivingViews, Assembly-CSharp",true));var mount=Object("Designer mount with independent source").transform;mount.SetPositionAndRotation(new Vector3(-7,12,3),Quaternion.Euler(11,72,-4));mount.localScale=new Vector3(.8f,1.2f,.9f);var before=mount.localToWorldMatrix;
        var mounts=new[]{mount};views.GetType().GetField("cabMounts").SetValue(views,mounts);Match.GetField("drivingViews").SetValue(match,views);
        // No appearance host/body is compatible in this fixture. The explicit
        // designer mount nevertheless remains the first authority, unchanged.
        Match.GetMethod("ConfigureAuthoredSourceCabMountCandidate").Invoke(match,new object[]{0,0,candidateEnabled});Assert.That(ReferenceEquals(views.GetType().GetField("cabMounts").GetValue(views),mounts),Is.True);Assert.That(mounts[0],Is.SameAs(mount));Assert.That(mount.localToWorldMatrix,Is.EqualTo(before));Assert.That(go.GetComponentsInChildren<Transform>(true).Any(value=>value.name=="Riders authored compatible driver eye"),Is.False);
    }
}
#endif
