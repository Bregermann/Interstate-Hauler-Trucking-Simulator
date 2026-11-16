#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRiderNativeEightReplacementTests
{
    [Serializable] sealed class Choice
    {
        public int slot,index;public string species,targetBefore,targetAfter,targetReloaded,actorBefore,actorAfter,actorReloaded;
        public bool otherSevenExact;
    }
    [Serializable] sealed class Receipt
    {
        public string status,failure;public int nativeCalls,replacedChoices,protectedExternalObjects,protectedRacers,protectedTrucks,protectedPhysicalRings;
        public bool allOriginalChoicesPreflighted,expectedLastChoiceEditRejected,otherSevenEveryStepExact,externalIdentitiesExact,externalConfigurationExact,worldExact,nwhExact,unitAncestors,qualifiedProfilesExact,noTestAssetDependencies,savedReloaded,originalSourcesExact;
        public float maximumNwhDimensionDrift;public Choice[] choices;
    }
    [TestCase(false)][TestCase(true)][Timeout(1800000)]
    public void ActualEightChoiceNativeReplacementPreservesOwnersAndRejectsLastChoiceEditBeforeStepZero(bool unknownLastEdit)
    {
        var args=Environment.GetCommandLineArgs();string Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit native authority required: "+key);return args[at+1];}
        string production=Required("-tsDragonMountedProductionEvidence"),course=Required("-tsRiderProductionCourseEvidence"),single=Required("-tsRiderNativeSingleEvidence"),tag=Required("-tsRiderNativeEightProbeTag");
        const string original="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity",generated="Assets/TruckDragonRiders/Tests/Generated/";var before=File.ReadAllBytes(original);var meta=File.ReadAllBytes(original+".meta");Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"NativeEightRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Save original user edits before disposable test.");}
        var setup=EditorSceneManager.GetSceneManagerSetup();var incoming=setup.Where(row=>!string.IsNullOrEmpty(row.path)).ToDictionary(row=>row.path,row=>File.ReadAllBytes(row.path));var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolation,generated+"NativeEightIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var json=(string)type.GetMethod("ProbeAllEightKnownActorNativePrefabReplacement").Invoke(null,new object[]{production,course,single,tag,unknownLastEdit});var proof=JsonUtility.FromJson<Receipt>(json);
            Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.originalSourcesExact&&proof.externalIdentitiesExact&&proof.externalConfigurationExact&&proof.worldExact&&proof.nwhExact,Is.True);Assert.That(proof.protectedExternalObjects,Is.GreaterThan(1000));Assert.That(proof.protectedRacers,Is.EqualTo(4));Assert.That(proof.protectedTrucks,Is.EqualTo(4));Assert.That(proof.protectedPhysicalRings,Is.EqualTo(12));Assert.That(proof.maximumNwhDimensionDrift,Is.LessThanOrEqualTo(.00001f));Assert.That(proof.choices.Length,Is.EqualTo(8));
            if(unknownLastEdit){Assert.That(proof.status,Is.EqualTo("LAST_CHOICE_CUSTOM_EDIT_REJECTED_BEFORE_ALL_REPLACEMENTS"));Assert.That(proof.expectedLastChoiceEditRejected,Is.True);Assert.That(proof.nativeCalls,Is.Zero,"All eight baselines must be checked before changing choice0.");Assert.That(proof.replacedChoices,Is.Zero);}
            else
            {
                Assert.That(proof.status,Is.EqualTo("DISPOSABLE_ALL_EIGHT_NATIVE_REPLACEMENTS_VALIDATED_NOT_CANONICAL"));Assert.That(proof.allOriginalChoicesPreflighted&&proof.otherSevenEveryStepExact&&proof.qualifiedProfilesExact&&proof.unitAncestors&&proof.noTestAssetDependencies&&proof.savedReloaded,Is.True);Assert.That(proof.nativeCalls,Is.EqualTo(8));Assert.That(proof.replacedChoices,Is.EqualTo(8));
                foreach(var row in proof.choices){Assert.That(row.otherSevenExact,Is.True);Assert.That(row.targetBefore,Is.Not.Empty);Assert.That(row.actorBefore,Is.Not.Empty);Assert.That(row.targetReloaded,Is.EqualTo(row.targetAfter));Assert.That(row.actorReloaded,Is.EqualTo(row.actorAfter));TestContext.WriteLine("Authorized actor subtree identity disposition crew"+row.slot+"/"+row.species+": "+row.actorBefore+" -> "+row.actorAfter+"; external owner identities preserved separately.");}
            }
        }
        finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var pair in incoming)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),"Incoming saved scene changed.");CollectionAssert.AreEqual(before,File.ReadAllBytes(original));CollectionAssert.AreEqual(meta,File.ReadAllBytes(original+".meta"));}
    }
}
#endif
