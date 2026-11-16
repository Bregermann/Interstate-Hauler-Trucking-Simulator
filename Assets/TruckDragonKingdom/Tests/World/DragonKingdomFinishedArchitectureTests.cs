#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NUnit.Framework;

public sealed class DragonKingdomFinishedArchitectureTests
{
    [Test,Timeout(300000)]
    public void ActualUnlinkedOriginalPreviewRetiresExactCastleBoundaryAndRejectsAuthoredCollisionChanges()
    {
        const string preview="Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity",prefab="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";var beforeScene=File.ReadAllBytes(preview);var beforePrefab=File.ReadAllBytes(prefab);Scene scene=default;
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonArchitectureGeometry");Assert.That(at>=0&&at+1<args.Length,Is.True);var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var worldType=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);
        try
        {
            scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren(worldType,true)).Single();var sanctuary=world.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Fortified castle sanctuary");Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(sanctuary),Is.Null,"Reproduce the original plain-object preview provenance.");Assert.That(sanctuary.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).Any(g=>g.Count()>1),Is.True,"Retain repeated original wall names; hierarchy correspondence must use sibling identity.");
            var collider=sanctuary.GetComponentsInChildren<Collider>(true).First(c=>c.enabled);var enabled=collider.enabled;try{collider.enabled=false;var failure=Assert.Throws<TargetInvocationException>(()=>type.GetMethod("RetireMeasuredCastlePrototypeShell").Invoke(null,new object[]{world,args[at+1]}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("authored property changed"));}finally{collider.enabled=enabled;}
            var proof=type.GetMethod("RetireMeasuredCastlePrototypeShell").Invoke(null,new object[]{world,args[at+1]});object Read(string name)=>proof.GetType().GetField(name).GetValue(proof);Assert.That((int)Read("disabledComponents"),Is.EqualTo(26));Assert.That((int)Read("retainedTowerComponents"),Is.EqualTo(8));Assert.That((int)Read("perches"),Is.EqualTo(4));foreach(string flag in new[]{"exactNativeComponentBoundary","allPerchReferencesAndWorldPosesExact","sourceBytesPreserved"})Assert.That((bool)Read(flag),Is.True,flag);
            var composition=Activator.CreateInstance(type.GetNestedType("FinishedArchitectureComposition"));void Set(string name,object value)=>composition.GetType().GetField(name).SetValue(composition,value);Set("geometryWitness",args[at+1]);using(var sha=SHA256.Create())Set("geometryWitnessSha256",BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(args[at+1]))).Replace("-","").ToLowerInvariant());Set("castlePrototypeRetirement",proof);Set("prototypeRetirementComplete",true);var originalWorld=AssetDatabase.LoadAssetAtPath<GameObject>(prefab).GetComponent(worldType);var bindings=(Array)type.GetMethod("ValidateSavedCastlePrototypeRetirementBindings").Invoke(null,new object[]{originalWorld,world,composition,false});Assert.That(bindings.Length,Is.EqualTo(26),"Read-only saved binding must preserve exact canonical component IDs across the actual plain preview.");
        }
        finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);CollectionAssert.AreEqual(beforeScene,File.ReadAllBytes(preview));CollectionAssert.AreEqual(beforePrefab,File.ReadAllBytes(prefab));}
    }
    [Test,Timeout(300000)]
    public void ExactMeasuredCastleRetirementDisablesOnly26ObsoleteComponentsAndPreserves4PhysicalPerchTowersAndReferencesInDisposablePreview()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var proof=type.GetMethod("CaptureMeasuredCastlePrototypeRetirementFromCommandLine").Invoke(null,null);
        object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        foreach(string flag in new[]{"exactNativeComponentBoundary","allPerchReferencesAndWorldPosesExact","sourceBytesPreserved"})Assert.That((bool)Field(proof,flag),Is.True,flag);
        Assert.That((int)Field(proof,"disabledComponents"),Is.EqualTo(26));Assert.That((int)Field(proof,"retainedTowerComponents"),Is.EqualTo(8));Assert.That((int)Field(proof,"perches"),Is.EqualTo(4));
        foreach(var row in ((Array)Field(proof,"retired")).Cast<object>()){Assert.That((bool)Field(row,"beforeEnabled"),Is.True);Assert.That((bool)Field(row,"afterEnabled"),Is.False);}
    }
    [Test,Timeout(600000)]
    public void ActualFourCompleteSourceFramesHaveRealPrimaryMeshCollisionAndIndividualPhysicalParcelsWithoutPretendingFinishedWorldAcceptance()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        var proof=type.GetMethod("CaptureMeasuredFinishedArchitectureStageFromCommandLine",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
        object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        foreach(string key in new[]{"strictGeometryPrerequisite","sourceBytesPreserved","completeSourceFramesAndInstances","roadStartObjectiveAndWingClear","crossEnsembleAndRetainedBuildingOverlapClear","prototypeRetirementComplete"})Assert.That((bool)Field(proof,key),Is.True,key);
        foreach(string key in new[]{"terrainSupportVerified","renderedAppearanceAccepted"})Assert.That((bool)Field(proof,key),Is.False,"Source-stage test cannot claim later physical/world/art acceptance.");
        Assert.That((int)Field(proof,"instances"),Is.EqualTo(678));Assert.That((int)Field(proof,"ownedMeshRepairs"),Is.EqualTo(1));var waterfront=Field(proof,"waterfront");Assert.That((bool)Field(waterfront,"actualSourceHullGeometryPassed"),Is.True);Assert.That((bool)Field(waterfront,"actualBedAndPhysicalSupportsPassed"),Is.False,"Source stage must not claim subsequently coordinated bed/pile/gangway acceptance.");Assert.That((bool)Field(waterfront,"noWaterfrontTerrainPads"),Is.True);Assert.That(((Array)Field(waterfront,"boats")).Length,Is.EqualTo(2));Assert.That((int)Field(proof,"addedColliders"),Is.GreaterThan(0));Assert.That((int)Field(proof,"footings"),Is.GreaterThan(0));Assert.That(((Array)Field(proof,"materials")).Length,Is.EqualTo(4));
        var groups=((Array)Field(proof,"groups")).Cast<object>().ToArray();Assert.That(groups.Select(g=>(int)Field(g,"instances")),Is.EqualTo(new[]{110,156,382,30}));
        var envelopes=((Array)Field(proof,"sourceEnvelopes")).Cast<object>().ToArray();Assert.That(envelopes.Length,Is.EqualTo(4));Assert.That(envelopes.Sum(e=>(int)Field(e,"instances")),Is.EqualTo(678));
        var ladder=((Array)Field(groups.Single(g=>(string)Field(g,"key")=="lumber-warehouse"),"parts")).Cast<object>().Single(p=>(string)Field(p,"instance")=="1007988382");var repair=Field(ladder,"ownedMeshRepair");Assert.That((bool)Field(repair,"rawNativeReferenceWasNull"),Is.True);Assert.That((string)Field(repair,"originalRequestedId"),Is.EqualTo("4300000"));Assert.That((string)Field(repair,"repairedId"),Is.EqualTo("4300002"));Assert.That((int)Field(repair,"repairedVertices"),Is.EqualTo(216));Assert.That((bool)Field(repair,"sourceBytesPreserved"),Is.True);
        var castle=groups.Single(g=>(string)Field(g,"key")=="fortified-quarter");Assert.That((string)Field(castle,"retainedTowerSeparationAxis"),Is.EqualTo("NEGATIVE_X_WEST"));Assert.That((float)Field(castle,"retainedTowerSeparation"),Is.GreaterThanOrEqualTo(5.999f));
        var plaza=(UnityEngine.Vector3)Field(castle,"originalFortressPlaza");var castleBounds=(UnityEngine.Bounds)Field(castle,"bounds");Assert.That(castleBounds.center.z,Is.EqualTo(plaza.z).Within(.001f));

        foreach(var group in groups)
        {
            Assert.That((bool)Field(group,"exactCompleteSourceComposition"),Is.True);Assert.That((bool)Field(group,"insideDeclaredCompositionParcel"),Is.True);
            foreach(var part in ((Array)Field(group,"parts")).Cast<object>())foreach(string flag in new[]{"sourcePoseExact","sourceGeometryExact"})Assert.That((bool)Field(part,flag),Is.True,flag);
        }
    }
}
#endif
