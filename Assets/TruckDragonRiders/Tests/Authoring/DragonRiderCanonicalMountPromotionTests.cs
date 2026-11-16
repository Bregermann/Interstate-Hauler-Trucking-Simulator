#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRiderCanonicalMountPromotionTests
{
    static readonly string[] CommandArgs={"-tsRiderApplyCanonicalMounts","-tsDragonMountedProductionEvidence","Logs/metadata-production","-tsRiderProductionCourseEvidence","Logs/metadata-course","-tsRiderNativeEightEvidence","Logs/metadata-eight","-tsRiderCanonicalPromotionOutput","Logs/metadata-output"};
    static string[] ParseCommand(string[] args)
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        return (string[])author.GetMethod("CanonicalPromotionCommandArguments",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{args});
    }
    [Test]
    public void ExplicitCommandKeepsAllThreeEvidenceRolesAndOutputDistinctWithoutAuthorizingAnyApply()
    {
        CollectionAssert.AreEqual(new[]{"Logs/metadata-production","Logs/metadata-course","Logs/metadata-eight","Logs/metadata-output"},ParseCommand(CommandArgs));
    }
    [TestCase(-1)][TestCase(0)][TestCase(1)][TestCase(3)][TestCase(5)][TestCase(7)]
    public void MissingExplicitCommandOrEvidenceRoleIsRejectedBeforeAnySceneMutation(int at)
    {
        string[] args=at<0?null:CommandArgs.Where((_,i)=>i!=at).ToArray();
        var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>ParseCommand(args));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());
    }
    [TestCase(0)][TestCase(1)][TestCase(3)][TestCase(5)][TestCase(7)]
    public void DuplicateExplicitCommandOrEvidenceRoleCannotSelectAnAmbiguousSource(int at)
    {
        var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>ParseCommand(CommandArgs.Concat(new[]{CommandArgs[at],"Logs/ambiguous"}).ToArray()));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());
    }
    [TestCase("")][TestCase(" ")][TestCase("-tsRiderNativeEightEvidence")]
    public void EmptyOrAnotherFlagCannotBeUsedAsAnEvidencePath(string value)
    {
        var args=(string[])CommandArgs.Clone();args[2]=value;var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>ParseCommand(args));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());
    }
    [Serializable] sealed class Receipt
    {
        public string status,failure,sourceSha256,resultSha256,backupFolder;
        public bool canonicalTarget,allOriginalChoicesPreflighted,otherSevenEveryStepExact,externalIdentitiesExact,configurationExact,worldExact,nwhExact,profilesExact,unitAncestors,noTestDependencies,savedReloaded,sourceClosureExact,rollbackExact;
        public int nativeCalls,protectedExternalObjects,protectedRacers,protectedTrucks,protectedRings;public float maximumNwhDimensionDrift;
    }
    [TestCase("baseline")][TestCase("unknown-last")][TestCase("rollback-after-first-save")][TestCase("stale-plan")][Timeout(1800000)]
    public void DisposableCanonicalPlanApplyPreservesOriginalsAndRejectsOrRollsBackFaults(string fault)
    {
        var args=Environment.GetCommandLineArgs();string Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit passed native authority required: "+key);return args[at+1];}
        string production=Required("-tsDragonMountedProductionEvidence"),course=Required("-tsRiderProductionCourseEvidence"),eight=Required("-tsRiderNativeEightEvidence"),tag=Required("-tsRiderCanonicalPromotionProbeTag");
        const string original="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity",generated="Assets/TruckDragonRiders/Tests/Generated/";var before=File.ReadAllBytes(original);var meta=File.ReadAllBytes(original+".meta");Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"PromotionRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Save incoming edits before disposable controls.");}
        var setup=EditorSceneManager.GetSceneManagerSetup();var incoming=setup.Where(row=>!string.IsNullOrEmpty(row.path)).ToDictionary(row=>row.path,row=>File.ReadAllBytes(row.path));var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolation,generated+"PromotionIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var json=(string)author.GetMethod("ProbeCanonicalMountPromotionPlanApply").Invoke(null,new object[]{production,course,eight,tag,fault});var proof=JsonUtility.FromJson<Receipt>(json);Assert.That(proof.canonicalTarget,Is.False,"These native controls must NEVER call the canonical Apply entry point.");
            if(fault=="stale-plan"){Assert.That(proof.status,Is.EqualTo("STALE_PLAN_REJECTED_BEFORE_WRITE"));Assert.That(proof.nativeCalls,Is.Zero);return;}
            Assert.That(proof.sourceClosureExact,Is.True);Assert.That(proof.protectedExternalObjects,Is.GreaterThan(1000));Assert.That(proof.protectedRacers,Is.EqualTo(4));Assert.That(proof.protectedTrucks,Is.EqualTo(4));Assert.That(proof.protectedRings,Is.EqualTo(12));Assert.That(File.Exists(Path.Combine(proof.backupFolder,"before.unity")),Is.True);CollectionAssert.AreEqual(before,File.ReadAllBytes(Path.Combine(proof.backupFolder,"before.unity")),"Backup must be the exact original scene, not a reconstructed candidate.");
            if(fault=="baseline")
            {
                Assert.That(proof.status,Is.EqualTo("DISPOSABLE_CANONICAL_PLAN_APPLY_VALIDATED_NOT_CANONICAL"));Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.allOriginalChoicesPreflighted&&proof.otherSevenEveryStepExact&&proof.externalIdentitiesExact&&proof.configurationExact&&proof.worldExact&&proof.nwhExact&&proof.profilesExact&&proof.unitAncestors&&proof.noTestDependencies&&proof.savedReloaded,Is.True);Assert.That(proof.nativeCalls,Is.EqualTo(8));Assert.That(proof.maximumNwhDimensionDrift,Is.LessThanOrEqualTo(.00001f));Assert.That(proof.resultSha256,Is.Not.EqualTo(proof.sourceSha256));
            }
            else
            {
                Assert.That(proof.status,Is.EqualTo("REJECTED_CANONICAL_MOUNT_PROMOTION"));Assert.That(proof.rollbackExact,Is.True,"Actual disposable scene/meta bytes must be restored after rejection, including a genuine saved first replacement.");Assert.That(proof.resultSha256,Is.EqualTo(proof.sourceSha256));Assert.That(proof.nativeCalls,Is.EqualTo(fault=="unknown-last"?0:1));Assert.That(proof.failure,Does.Contain(fault=="unknown-last"?"Unknown original choice edit":"INJECTED_AFTER_FIRST_NATIVE_SAVE"));
            }
        }
        finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var pair in incoming)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key));CollectionAssert.AreEqual(before,File.ReadAllBytes(original),"Canonical scene must remain exact throughout all disposable controls.");CollectionAssert.AreEqual(meta,File.ReadAllBytes(original+".meta"),"Original canonical GUID/meta must remain exact.");}
    }
}
#endif
