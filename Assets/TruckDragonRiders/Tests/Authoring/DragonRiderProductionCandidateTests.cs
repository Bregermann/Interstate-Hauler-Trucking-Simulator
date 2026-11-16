#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRiderProductionCandidateTests
{
    static Type Authoring=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
    const string Original="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
    static object Call(string scene,string[] producer,string[] runtime,string tag,string course=null)=>Authoring.GetMethod("CreateQualifiedMountedRidersSceneCandidate").Invoke(null,new object[]{scene,producer,runtime,tag,course});
    [TestCase("../escape")][TestCase("")][TestCase("bad/tag")]
    public void InvalidCandidateTagCannotWriteOrOverwriteProductionAssets(string tag)
    {
        var before=File.ReadAllBytes(Original);var error=Assert.Throws<TargetInvocationException>(()=>Call(Original,Array.Empty<string>(),Array.Empty<string>(),tag));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("candidate tag"));CollectionAssert.AreEqual(before,File.ReadAllBytes(Original));
    }
    [Test]
    public void MissingPairedEvidenceCannotFallBackToAnUnqualifiedSceneMount()
    {
        string tag="MissingEvidence-"+Guid.NewGuid().ToString("N");var before=File.ReadAllBytes(Original);var error=Assert.Throws<TargetInvocationException>(()=>Call(Original,Array.Empty<string>(),Array.Empty<string>(),tag));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("paired actual producer/runtime evidence"));Assert.That(Directory.Exists("Assets/TruckDragonRiders/Animations/Authored/QualifiedMounted-"+tag),Is.False);Assert.That(Directory.Exists("Assets/TruckDragonRiders/ReviewCandidates/QualifiedMounted-"+tag),Is.False);CollectionAssert.AreEqual(before,File.ReadAllBytes(Original));
    }
    [Serializable]sealed class Receipt
    {
        public string scope,candidateScene,failure;public string[] species,actorPrefabs,controllers;
        public bool savedReloaded,originalSourcesExact,sceneConfigurationExact,nwhFieldsExact,unitAncestors,profilesExact,noTestAssetDependencies;public int originalTrucks,replacedChoices;public float maximumNwhDimensionDrift;
    }
    [Test,Timeout(1800000)]
    public void SavedQualifiedRiderCandidateRetainsCrewNwhAndProductionOnlyMountReferences()
    {
        var args=Environment.GetCommandLineArgs();string Required(string name){int i=Array.IndexOf(args,name);Assert.That(i>=0&&i+1<args.Length,Is.True,name);return args[i+1];}
        string[] producers=Required("-tsRiderProductionProducerEvidence").Split(';');string[] runtimes=Required("-tsRiderProductionRuntimeEvidence").Split(';');string source=Required("-tsRiderProductionSourceScene"),tag=Required("-tsRiderProductionCandidateTag");int at=Array.IndexOf(args,"-tsRiderProductionCourseEvidence");string course=at>=0?args[at+1]:null;
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path)){const string folder="Assets/TruckDragonRiders/Tests/Generated/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();Assert.That(EditorSceneManager.SaveScene(scene,folder+"ProductionCandidateRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);}}
        var original=File.ReadAllBytes(source);var proof=JsonUtility.FromJson<Receipt>((string)Call(source,producers,runtimes,tag,course));
        Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.savedReloaded&&proof.originalSourcesExact&&proof.sceneConfigurationExact&&proof.nwhFieldsExact&&proof.unitAncestors&&proof.profilesExact&&proof.noTestAssetDependencies,Is.True);Assert.That(proof.originalTrucks,Is.EqualTo(4));Assert.That(proof.replacedChoices,Is.EqualTo(proof.species.Length*4));Assert.That(proof.maximumNwhDimensionDrift,Is.Zero);Assert.That(proof.actorPrefabs.Length,Is.EqualTo(proof.species.Length));Assert.That(proof.controllers.Length,Is.EqualTo(proof.species.Length));
        foreach(string path in proof.actorPrefabs.Concat(proof.controllers)){Assert.That(File.Exists(path),Is.True);Assert.That(path,Does.Not.Contain("/Tests/"));}Assert.That(File.Exists(proof.candidateScene),Is.True);Assert.That(AssetDatabase.GetDependencies(proof.candidateScene,true).Any(p=>p.Contains("/Tests/Generated/")),Is.False);CollectionAssert.AreEqual(original,File.ReadAllBytes(source));Assert.That(proof.scope,Does.Contain("NOT_CANONICAL"));
    }
}
#endif
