#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DragonEscapeAuthoringTests
{
    const string Folder="Assets/TruckDragonEscape/";
    [Serializable] sealed class SourceReceipt { public string sourcePrefab,sourceSha256; public int sourceMeshes,fragments; }
    [Serializable] sealed class Receipt { public string id,intactPrefab,fragmentsPrefab; public float roadWidth; public string[] edges,fragmentMeshes; public SourceReceipt[] sources; }
    SceneSetup[] prior;
    static object Field(object item,string name)=>item.GetType().GetField(name).GetValue(item);
    [SetUp] public void OpenActualSavedScene()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Folder+"Scenes/TruckDragonEscape.unity"),Is.Not.Null,"Run explicit Create authoring first; this fixture must consume the saved actual world.");
        prior=EditorSceneManager.GetSceneManagerSetup();EditorSceneManager.OpenScene(Folder+"Scenes/TruckDragonEscape.unity",OpenSceneMode.Single);
    }
    [TearDown] public void RestoreScene() { if(prior!=null)EditorSceneManager.RestoreSceneManagerSetup(prior); }
    [Test] public void EveryAuthoredClosureHasTheExactRealSiteAndSameModelPrivateGeometry()
    {
        var type=Type.GetType("LWS.TruckDragonEscape.DragonEscapeMatch, Assembly-CSharp",true);
        var match=UnityEngine.Object.FindAnyObjectByType(type);Assert.That(match,Is.Not.Null);
        var definition=Field(Field(match,"world"),"definition");var plans=((IEnumerable)Field(definition,"closurePlans")).Cast<object>().ToArray();
        var hazards=((IEnumerable)Field(match,"routeHazards")).Cast<Component>().ToArray();Assert.That(hazards.Length,Is.EqualTo(4));Assert.That(plans.Length,Is.EqualTo(4));
        foreach(var hazard in hazards)
        {
            string id=(string)Field(hazard,"id");var plan=plans.Single(value=>(string)Field(value,"id")==id);
            Assert.That((string[])Field(hazard,"blockedEdges"),Is.EquivalentTo((string[])Field(plan,"edgeIds")));
            string manifest=Folder+"PrivateLicensedFragments/"+id+"/actual-source-and-fragments.json";Assert.That(File.Exists(manifest),Is.True);
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(manifest));Assert.That(receipt.id,Is.EqualTo(id));Assert.That(receipt.edges,Is.EquivalentTo((string[])Field(plan,"edgeIds")));
            var intact=(GameObject)Field(hazard,"intactPrefab");Assert.That(AssetDatabase.GetAssetPath(intact),Is.EqualTo(receipt.intactPrefab));
            var core=intact.GetComponents<Component>().Single(component=>component.GetType().Name=="CollapserPrefrag");
            var settings=new SerializedObject(core);Assert.That(settings.FindProperty("collapsedPrefab").objectReferenceValue,Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(receipt.fragmentsPrefab)));
            var support=intact.GetComponents<Component>().Single(component=>component.GetType().Name=="Support");var supportSettings=new SerializedObject(support);
            Assert.That(supportSettings.FindProperty("supportedBy").arraySize,Is.Zero);Assert.That(support.GetType().GetField("onCollapse",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(support),Is.Not.Null);
            var fragments=AssetDatabase.LoadAssetAtPath<GameObject>(receipt.fragmentsPrefab);var bodies=fragments.GetComponentsInChildren<Rigidbody>(true);
            Assert.That(bodies.Length,Is.InRange(2,32));Assert.That(bodies.All(body=>!body.isKinematic&&body.useGravity&&body.mass>0&&body.mass<=180),Is.True);
            Assert.That(receipt.sources.Sum(source=>source.fragments),Is.EqualTo(bodies.Length));
            foreach(var filter in fragments.GetComponentsInChildren<MeshFilter>(true))
            { Assert.That(AssetDatabase.GetAssetPath(filter.sharedMesh),Does.StartWith(Folder+"PrivateLicensedFragments/"+id+"/"));Assert.That(receipt.fragmentMeshes,Does.Contain(AssetDatabase.GetAssetPath(filter.sharedMesh))); }
            foreach(var source in receipt.sources)
            {
                Assert.That(source.sourcePrefab,Does.StartWith("Assets/TruckDragonKingdom/PrivateLicensedEnvironment/"));Assert.That(source.sourceMeshes,Is.GreaterThan(0));Assert.That(source.fragments,Is.GreaterThan(0));
                using(var hash=SHA256.Create())Assert.That(string.Concat(hash.ComputeHash(File.ReadAllBytes(source.sourcePrefab)).Select(value=>value.ToString("x2"))),Is.EqualTo(source.sourceSha256),"The actual source prefab must remain byte-identical.");
            }
            var preview=(GameObject)PrefabUtility.InstantiatePrefab(intact,hazard.transform);
            try
            {
                preview.transform.localPosition=Vector3.zero;preview.transform.localRotation=Quaternion.identity;Physics.SyncTransforms();
                var overlaps=Physics.OverlapBox(hazard.transform.position+Vector3.up*4.5f,new Vector3(receipt.roadWidth*.5f-.5f,3.5f,6),hazard.transform.rotation,~0,QueryTriggerInteraction.Ignore);
                Assert.That(overlaps.Any(shape=>shape.transform.IsChildOf(preview.transform)),Is.False,"Actual original NWH corridor must be clear before collapse: "+id);
                Assert.That(preview.GetComponentsInChildren<Renderer>(true).All(renderer=>renderer.sharedMaterials.All(material=>material!=null&&material.GetTag("RenderPipeline",false,"")=="UniversalPipeline")),Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(preview); }
        }
    }
}
#endif
