#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRiderNativePrefabReplacementTests
{
    [Serializable]sealed class Receipt
    {
        public string status,failure,copyScene,targetBefore,targetAfter,targetReloaded,actorBefore,actorAfter,actorReloaded;
        public bool nativeReplacementCalled,expectedCustomEditRejected,targetRootReferenceRetained,actorReferenceRetained,externalIdentitiesExact,externalConfigurationExact,worldExact,nwhExact,qualifiedProfileExact,savedReloaded,originalSourcesExact;
        public int protectedExternalObjects,propertyOverrides,addedObjects,addedComponents,removedObjects,removedComponents;public float maximumNwhDimensionDrift;
    }
    [TestCase(false)][TestCase(true)][Timeout(1800000)]
    public void ActualSingleActorNativeApiRetainsQualifiedProfileAndRejectsUnknownEditsBeforeReplacement(bool unknownEdit)
    {
        var args=Environment.GetCommandLineArgs();string Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit bounded source/evidence required: "+key);return args[at+1];}
        string production=Required("-tsDragonMountedProductionEvidence"),course=Required("-tsRiderProductionCourseEvidence"),tag=Required("-tsRiderNativePrefabProbeTag");
        const string original="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";var before=File.ReadAllBytes(original);var meta=File.ReadAllBytes(original+".meta");const string generated="Assets/TruckDragonRiders/Tests/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"NativeReplaceRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve user edits before this disposable-scene native probe.");}
        var setup=EditorSceneManager.GetSceneManagerSetup();var incoming=setup.Where(row=>!string.IsNullOrEmpty(row.path)).ToDictionary(row=>row.path,row=>File.ReadAllBytes(row.path));var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolation,generated+"NativeReplaceIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var result=(string)author.GetMethod("ProbeSingleKnownActorNativePrefabReplacement").Invoke(null,new object[]{production,course,tag,unknownEdit});var proof=JsonUtility.FromJson<Receipt>(result);
            Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.originalSourcesExact&&proof.externalIdentitiesExact&&proof.externalConfigurationExact&&proof.worldExact&&proof.nwhExact,Is.True);Assert.That(proof.protectedExternalObjects,Is.GreaterThan(1000));
            if(unknownEdit){Assert.That(proof.status,Is.EqualTo("KNOWN_CUSTOM_EDIT_REJECTED_BEFORE_NATIVE_REPLACEMENT"));Assert.That(proof.expectedCustomEditRejected,Is.True);Assert.That(proof.nativeReplacementCalled,Is.False,"An unknown actor edit must be rejected before any override clearing/native replacement.");}
            else{Assert.That(proof.status,Is.EqualTo("DISPOSABLE_SINGLE_ACTOR_NATIVE_REPLACEMENT_VALIDATED_NOT_CANONICAL"));Assert.That(proof.nativeReplacementCalled&&proof.qualifiedProfileExact&&proof.savedReloaded,Is.True);Assert.That(proof.maximumNwhDimensionDrift,Is.LessThanOrEqualTo(.00001f));Assert.That(proof.targetBefore,Is.Not.Empty);Assert.That(proof.targetAfter,Is.Not.Empty);Assert.That(proof.targetReloaded,Is.Not.Empty);TestContext.WriteLine("Native target identities/reference retention are observations, not assumed guarantees: root="+proof.targetRootReferenceRetained+" actor="+proof.actorReferenceRetained+"; before="+proof.actorBefore+" after="+proof.actorAfter+" reloaded="+proof.actorReloaded);}
        }
        finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var pair in incoming)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),"Incoming saved scene changed during disposable probe.");CollectionAssert.AreEqual(before,File.ReadAllBytes(original),"Original canonical scene changed.");CollectionAssert.AreEqual(meta,File.ReadAllBytes(original+".meta"),"Original canonical scene GUID/meta changed.");}
    }
}
#endif
