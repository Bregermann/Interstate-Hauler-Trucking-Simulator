#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRiderCanonicalPhysicalCourseTests
{
    static Type Author=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
    static readonly string[] CourseCommand={"-tsRiderApplyCanonicalPhysicalCourse","-tsRiderCourseCanonicalMountReceipt","Logs/metadata-mount.json","-tsRiderProductionCourseEvidence","Logs/metadata-course","-tsRiderPhysicalCourseBridgeEvidence","Logs/metadata-bridge","-tsRiderCanonicalPhysicalCourseOutput","Logs/metadata-output"};
    static string[] ParseCourseCommand(string[] args)=>(string[])Author.GetMethod("CanonicalPhysicalCourseCommandArguments",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{args});
    [Test]public void PhysicalCourseCommandKeepsActualEvidenceRolesDistinctWithoutApplyingAnything()=>CollectionAssert.AreEqual(new[]{CourseCommand[2],CourseCommand[4],CourseCommand[6],CourseCommand[8]},ParseCourseCommand(CourseCommand));
    [TestCase(-1)][TestCase(0)][TestCase(1)][TestCase(3)][TestCase(5)][TestCase(7)]
    public void PhysicalCourseCommandRejectsMissingMarkerOrRoleBeforeMutation(int at)
    {var error=Assert.Throws<TargetInvocationException>(()=>ParseCourseCommand(at<0?null:CourseCommand.Where((_,i)=>i!=at).ToArray()));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    [TestCase(0)][TestCase(1)][TestCase(3)][TestCase(5)][TestCase(7)]
    public void PhysicalCourseCommandRejectsDuplicateMarkerOrRoleBeforeMutation(int at)
    {var error=Assert.Throws<TargetInvocationException>(()=>ParseCourseCommand(CourseCommand.Concat(new[]{CourseCommand[at],"Logs/ambiguous"}).ToArray()));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    [TestCase("")][TestCase(" ")][TestCase("-tsRiderPhysicalCourseBridgeEvidence")]
    public void PhysicalCourseCommandRejectsEmptyOrFlagAsEvidence(string value)
    {var args=(string[])CourseCommand.Clone();args[2]=value;var error=Assert.Throws<TargetInvocationException>(()=>ParseCourseCommand(args));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    [Test]public void PhysicalCourseCommandRejectsReusingOnePathForTwoAuthorityRoles()
    {var args=(string[])CourseCommand.Clone();args[6]=args[4];var error=Assert.Throws<TargetInvocationException>(()=>ParseCourseCommand(args));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    [TestCase("actual")][TestCase("duplicate-row")][TestCase("missing-row")][TestCase("extra-row")][TestCase("conflicting-alias-hash")]
    public void ActualV511SourceInventoryPreservesLogicalPackageAliasesAndRejectsShapeMutation(string fault)
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderCourseCanonicalMountReceipt");Assert.That(at>=0&&at+1<args.Length,Is.True,"The actual V511 canonical receipt is required, not a synthetic inventory.");string path=args[at+1];var before=File.ReadAllBytes(path);var applyType=Author.GetNestedType("CanonicalMountApply",BindingFlags.NonPublic);var apply=JsonUtility.FromJson(System.Text.Encoding.UTF8.GetString(before),applyType);object plan=applyType.GetField("plan").GetValue(apply);var source=(Array)plan.GetType().GetField("protectedSources").GetValue(plan);Assert.That(source.Length,Is.EqualTo(5894));var rowType=source.GetType().GetElementType();var rows=(Array)source.Clone();
        if(fault=="duplicate-row")rows.SetValue(rows.GetValue(0),1);
        if(fault=="missing-row"||fault=="extra-row"){rows=Array.CreateInstance(rowType,source.Length+(fault=="missing-row"?-1:1));Array.Copy(source,rows,Math.Min(source.Length,rows.Length));if(fault=="extra-row")rows.SetValue(source.GetValue(0),rows.Length-1);}
        if(fault=="conflicting-alias-hash"){int index=Enumerable.Range(0,rows.Length).Single(i=>(string)rowType.GetField("path").GetValue(rows.GetValue(i))=="Packages/com.unity.render-pipelines.universal/Editor/AssetVersion.cs");object clone=JsonUtility.FromJson(JsonUtility.ToJson(rows.GetValue(index)),rowType);rowType.GetField("sha256").SetValue(clone,new string('0',64));rows.SetValue(clone,index);}
        var method=Author.GetMethod("CourseRequireProtectedInventoryShape",BindingFlags.NonPublic|BindingFlags.Static);
        if(fault=="actual")Assert.That((int)method.Invoke(null,new object[]{rows}),Is.EqualTo(5888),"Actual5894 logical rows contain six SHA-identical captured URP logical/physical alias pairs.");else{var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{rows}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());}CollectionAssert.AreEqual(before,File.ReadAllBytes(path));
    }
    static string SyntheticDocumentControl(bool changed)
    {
        var text=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
        for(int i=1;i<=120;i++)text.Append("--- !u!4 &"+i+"\nTransform:\n  m_LocalPosition: {x: "+i+", y: 0, z: 0}\n  m_LocalScale: {x: "+(changed&&i<=12?"2":"1")+", y: 1, z: 1}\n  m_Father: {fileID: 0}\n");
        text.Append("--- !u!114 &1000\nMonoBehaviour:\n  gates:\n");for(int i=0;i<12;i++)text.Append("  - center: {x: "+i+", y: 220, z: 330}\n    rotation: {x: 0, y: 0, z: 0, w: 1}\n    clearRadius: "+(changed?60:32)+"\n");text.Append("  aiStraightGateExitDistance: "+(changed?55:20)+"\n  fixedSeed: 7\n");return text.ToString();
    }
    [TestCase("baseline",true)][TestCase("new-egress-field",true)][TestCase("unrelated-document",false)][TestCase("ring-position",false)][TestCase("match-center",false)][TestCase("match-option",false)][TestCase("gate-order",false)][TestCase("new-object",false)][TestCase("lost-object",false)][TestCase("header",false)]
    public void ThirteenDocumentAuthorityRejectsEveryUnrelatedFieldAndIdentity(string fault,bool expected)
    {
        var planType=Author.GetNestedType("CanonicalCoursePlan",BindingFlags.NonPublic);var plan=Activator.CreateInstance(planType,true);planType.GetField("matchDocument").SetValue(plan,"1000");planType.GetField("transformDocuments").SetValue(plan,Enumerable.Range(1,12).Select(i=>i.ToString()).ToArray());string before=SyntheticDocumentControl(false),after=SyntheticDocumentControl(true);
        if(fault=="new-egress-field")before=before.Replace("  aiStraightGateExitDistance: 20\n","");
        if(fault=="unrelated-document")after=after.Replace("--- !u!4 &50\nTransform:\n  m_LocalPosition: {x: 50", "--- !u!4 &50\nTransform:\n  m_LocalPosition: {x: 51");
        if(fault=="ring-position")after=after.Replace("m_LocalPosition: {x: 1,", "m_LocalPosition: {x: 9,");if(fault=="match-center")after=after.Replace("center: {x: 0,", "center: {x: 9,");if(fault=="match-option")after=after.Replace("fixedSeed: 7", "fixedSeed: 8");
        if(fault=="gate-order")after=after.Replace("center: {x: 0,", "center: {x: 99,").Replace("center: {x: 1,", "center: {x: 0,").Replace("center: {x: 99,", "center: {x: 1,");if(fault=="new-object")after+="--- !u!1 &9999\nGameObject:\n  m_Name: Unapproved\n";if(fault=="lost-object"){int start=after.IndexOf("--- !u!4 &50\n",StringComparison.Ordinal),end=after.IndexOf("--- !u!4 &51\n",StringComparison.Ordinal);after=after.Remove(start,end-start);}if(fault=="header")after=after.Replace("%YAML 1.1","%YAML 1.2");
        object[] args={before,after,plan,0};bool valid=(bool)Author.GetMethod("CourseDocumentDeltaExact",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);Assert.That(valid,Is.EqualTo(expected));if(expected)Assert.That(args[3],Is.EqualTo(13));
    }
    [Test]
    public void MissingOrDisposableMountReceiptCannotCreateCanonicalSourceAuthority()
    {
        var applyType=Author.GetNestedType("CanonicalMountApply",BindingFlags.NonPublic);var apply=Activator.CreateInstance(applyType,true);
        var method=Author.GetMethod("RequireCourseMountHeader",BindingFlags.NonPublic|BindingFlags.Static);
        foreach(bool testOnly in new[]{false,true}){var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new[]{apply,(object)testOnly}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());}
    }
    [Serializable]sealed class Proof
    {
        public string status,failure,backupFolder;public bool canonicalTarget,savedReloaded,allObjectIdsExact,only13DocumentsChanged,onlyAllowedFieldsChanged,worldExact,mountsExact,nwhExact,sourceClosureExact,physicalControlsPassed,rulesControlsPassed,rollbackExact,publicReaderRejectsTest;public int writes,changedDocuments,objects,gates;public float maximumNwhDimensionDrift;
    }
    [TestCase("baseline")][TestCase("unknown-unrelated")][TestCase("rollback-after-save")][TestCase("stale-plan")][Timeout(1800000)]
    public void SavedMountResultKeepsAllIdentitiesThroughPhysicalCourseOrExactRollback(string fault)
    {
        var args=Environment.GetCommandLineArgs();string Required(string key){int i=Array.IndexOf(args,key);Assert.That(i>=0&&i+1<args.Length,Is.True,key);return args[i+1];}bool actual=args.Contains("-tsRiderCourseCanonicalMountReceipt");Assert.That(actual&&args.Contains("-tsRiderCourseMountControlEvidence"),Is.False,"Select exactly one explicit upstream mount role.");string mount=Required(actual?"-tsRiderCourseCanonicalMountReceipt":"-tsRiderCourseMountControlEvidence"),course=Required("-tsRiderProductionCourseEvidence"),tag=Required("-tsRiderCoursePromotionProbeTag");
        const string original="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity",generated="Assets/TruckDragonRiders/Tests/Generated/";var before=File.ReadAllBytes(original);var meta=File.ReadAllBytes(original+".meta");Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,generated));AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(string.IsNullOrEmpty(scene.path)){if(!Application.isBatchMode||scene.isDirty)Assert.Ignore("Preserve interactive/dirty untitled scenes.");Assert.That(EditorSceneManager.SaveScene(scene,generated+"CoursePromotionRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);}Assert.That(scene.isDirty,Is.False,"Preserve user scene edits.");}
        var setup=EditorSceneManager.GetSceneManagerSetup();var incoming=setup.ToDictionary(row=>row.path,row=>File.ReadAllBytes(row.path));var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolation,generated+"CoursePromotionIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var result=(string)Author.GetMethod(actual?"ProbePhysicalCourseFromActualCanonicalMount":"ProbeCanonicalPhysicalCoursePromotion").Invoke(null,new object[]{mount,course,tag,fault});var proof=JsonUtility.FromJson<Proof>(result);Assert.That(proof.canonicalTarget,Is.False,"Never invoke actual canonical Apply from native disposable controls.");
            if(fault=="stale-plan"){Assert.That(proof.status,Is.EqualTo("STALE_COURSE_PLAN_REJECTED_BEFORE_WRITE"));Assert.That(proof.writes,Is.Zero);return;}
            Assert.That(proof.sourceClosureExact,Is.True);Assert.That(File.Exists(Path.Combine(proof.backupFolder,"before.unity")),Is.True);
            if(fault=="baseline")
            {
                Assert.That(proof.status,Is.EqualTo("TEST_ONLY_PHYSICAL_COURSE_PROMOTED_NO_CANONICAL_AUTHORITY"));Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.savedReloaded&&proof.allObjectIdsExact&&proof.only13DocumentsChanged&&proof.onlyAllowedFieldsChanged&&proof.worldExact&&proof.mountsExact&&proof.nwhExact&&proof.physicalControlsPassed&&proof.rulesControlsPassed&&proof.publicReaderRejectsTest,Is.True);Assert.That(proof.writes,Is.EqualTo(1));Assert.That(proof.changedDocuments,Is.EqualTo(13));Assert.That(proof.gates,Is.EqualTo(12));Assert.That(proof.objects,Is.GreaterThan(1000));Assert.That(proof.maximumNwhDimensionDrift,Is.LessThanOrEqualTo(.00001f));
            }
            else{Assert.That(proof.status,Is.EqualTo("REJECTED_PHYSICAL_COURSE_PROMOTION"));Assert.That(proof.rollbackExact,Is.True);Assert.That(proof.writes,Is.EqualTo(fault=="unknown-unrelated"?0:1));Assert.That(proof.failure,Does.Contain(fault=="unknown-unrelated"?"Unknown current scene":"INJECTED_AFTER_REAL_COURSE_SAVE"));}
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var row in incoming)CollectionAssert.AreEqual(row.Value,File.ReadAllBytes(row.Key));CollectionAssert.AreEqual(before,File.ReadAllBytes(original));CollectionAssert.AreEqual(meta,File.ReadAllBytes(original+".meta"));}
    }
}
#endif
