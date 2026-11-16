#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class DragonKingdomVergeDressingReviewTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,args);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name).SetValue(value,data);
    [TestCase("missing-site")][TestCase("duplicate-id")][TestCase("global-radius")][TestCase("nonfinite-site")]
    public void DressingRejectsUnboundedOrAmbiguousAuthoringWithoutChangingOriginalWorld(string fault)
    {
        var sites=(Array)Call("BoundedVergeDressingSites");switch(fault){case "missing-site":var shortSites=Array.CreateInstance(sites.GetType().GetElementType(),11);Array.Copy(sites,shortSites,11);sites=shortSites;break;case "duplicate-id":Set(sites.GetValue(1),"id",Field(sites.GetValue(0),"id"));break;case "global-radius":Set(sites.GetValue(0),"radius",new Vector2(1000,1000));break;case "nonfinite-site":Set(sites.GetValue(0),"center",new Vector3(float.NaN,0,0));break;}
        var error=Assert.Throws<TargetInvocationException>(()=>Call("ValidateBoundedVergeDressingPlan",sites));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void DressingInputsAreActualDistinctOriginalVendorArtWithVisiblePrimaryMeshes()
    {
        var paths=(string[])Call("BoundedVergeDressingSources");Assert.That(paths.Length,Is.EqualTo(14));Assert.That(paths.Distinct().Count(),Is.EqualTo(paths.Length));foreach(string path in paths){Assert.That(path,Does.StartWith("Assets/TruckDragonKingdom/PrivateLicensedEnvironment/"));var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(prefab,Is.Not.Null,path);Assert.That(prefab.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null&&f.sharedMesh.vertexCount>0),Is.True,path);Assert.That(AssetDatabase.AssetPathToGUID(path),Has.Length.EqualTo(32));}
    }
    [Test]
    public void ActualVendorInsideAreaAndColliderProjectionAreDeterministicAndLeaveNoRuntimeGenerator()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
        try
        {
            scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var ground=new GameObject("Temporary bounded vendor projection control");SceneManager.MoveGameObjectToScene(ground,scene);ground.transform.position=new Vector3(9000,9.5f,9000);ground.AddComponent<BoxCollider>().size=new Vector3(100,1,100);Physics.SyncTransforms();
            var knots=new[]{new Vector3(8986,20,8986),new Vector3(9014,20,8986),new Vector3(9014,20,9014),new Vector3(8986,20,9014)};var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(((string[])Call("BoundedVergeDressingSources"))[3]);var records=new List<string[]>();
            for(int i=0;i<2;i++)
            {
                var group=Activator.CreateInstance(Owner.GetNestedType("VergeDressingGroup"));Set(group,"id","native-vendor-control");var points=(IEnumerable)Call("DressingVendorPoints",scene,knots,true,new[]{prefab},new[]{2f},7f,991,false,group);var rows=new List<string>();foreach(var point in points){object position=Field(point,"position");Assert.That((float)Field(position,"y"),Is.EqualTo(10).Within(.001f));rows.Add(JsonUtility.ToJson(point));}Assert.That(rows.Count,Is.GreaterThan(2));Assert.That(Field(group,"startupNone"),Is.True);Assert.That(Field(group,"noRetainedGenerator"),Is.True);records.Add(rows.ToArray());
            }
            CollectionAssert.AreEqual(records[0],records[1]);Assert.That(scene.GetRootGameObjects().Length,Is.EqualTo(1));
        }
        finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualSavedSixIslandsTwoTerracesAndFourApproachesPreservePhysicalWorldAndProduceBeforeAfterViews()
    {
        yield return (IEnumerator)Call("CaptureBoundedVendorVergeDressingReview");var proof=Owner.GetProperty("LastVergeDressingReview").GetValue(null);Assert.That(Field(proof,"status"),Is.EqualTo("SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_WORLD_ADOPTION"));Assert.That(Field(proof,"islands"),Is.EqualTo(6));Assert.That(Field(proof,"bands"),Is.EqualTo(2));Assert.That(Field(proof,"junctions"),Is.EqualTo(4));Assert.That((int)Field(proof,"instances"),Is.InRange(66,240));
        foreach(string flag in new[]{"originalWorldExact","originalTerrainBytesExact","originalSourcesExact","savedReloaded","noAddedPhysics","noRuntimeGenerator","incomingSceneContextRestored"})Assert.That(Field(proof,flag),Is.True,flag);var groups=(Array)Field(proof,"groups");Assert.That(groups.Length,Is.EqualTo(12));foreach(var group in groups){Assert.That(Field(group,"startupNone"),Is.True);Assert.That(Field(group,"noRetainedGenerator"),Is.True);Assert.That((int)Field(group,"spawnerCalls"),Is.GreaterThan(0));}var frames=(Array)Field(proof,"frames");Assert.That(frames.Length,Is.EqualTo(12));foreach(var frame in frames){Assert.That((string)Field(frame,"sha256"),Has.Length.EqualTo(64));Assert.That(((string[])Field(frame,"shaderErrors")).Length,Is.Zero);}TestContext.WriteLine("SAVED_VISUAL_DRESSING_TRIAL_ONLY_REQUIRES_REAL_IMAGE_REVIEW_AND_SEPARATE_GAMEPLAY_ADOPTION");
    }
}
#endif
