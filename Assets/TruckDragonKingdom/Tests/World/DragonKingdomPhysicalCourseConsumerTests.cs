#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomPhysicalCourseConsumerTests
{
    static Type Bridge=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomPhysicalCourseWorldBridge, Assembly-CSharp-Editor",true);
    static Type Review=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomMountedCourseReview, Assembly-CSharp-Editor",true);
    static Type Author=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
    const string H="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",Canonical="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity",Receipt="Logs/physical-course-metadata-only.json";
    static object New(Type owner,string nested)=>Activator.CreateInstance(owner.GetNestedType(nested,BindingFlags.Public|BindingFlags.NonPublic),true);
    [Serializable]sealed class HarnessRoot{public string name;public string[] components;public Vector3 position,scale;public Quaternion rotation;}
    [Serializable]sealed class HarnessScene{public string name,path;public bool dirty,active,ownedBatchBootstrap;public HarnessRoot[] roots;}
    [Serializable]sealed class HarnessEvidence{public string scope="ACTUAL_INCOMING_EDITOR_SCENE_OWNERSHIP_BEFORE_TEST_ISOLATION_NOT_WORLD_ACCEPTANCE";public HarnessScene[] scenes;}
    static bool OwnedBatchBootstrap(Scene scene)
    {
        var args=Environment.GetCommandLineArgs();int platform=Array.IndexOf(args,"-testPlatform");if(!Application.isBatchMode||!args.Contains("-runTests")||platform<0||platform+1>=args.Length||!string.Equals(args[platform+1],"EditMode",StringComparison.OrdinalIgnoreCase)||SceneManager.sceneCount!=1||!string.IsNullOrEmpty(scene.path))return false;
        var roots=scene.GetRootGameObjects();if(roots.Length==0)return true;if(roots.Length!=2||roots.Count(root=>root.name=="Main Camera")!=1||roots.Count(root=>root.name=="Directional Light")!=1)return false;
        foreach(var root in roots)
        {
            bool camera=root.name=="Main Camera";var allowed=camera?new[]{"UnityEngine.Transform","UnityEngine.Camera","UnityEngine.AudioListener","UnityEngine.Rendering.Universal.UniversalAdditionalCameraData"}:new[]{"UnityEngine.Transform","UnityEngine.Light","UnityEngine.Rendering.Universal.UniversalAdditionalLightData"};
            if(!root.activeSelf||root.transform.localScale!=Vector3.one||root.GetComponents<Component>().Any(component=>component==null||!allowed.Contains(component.GetType().FullName)))return false;
            if(camera&&(root.GetComponents<Camera>().Length!=1||root.GetComponents<AudioListener>().Length!=1||root.tag!="MainCamera"))return false;if(!camera&&(root.GetComponents<Light>().Length!=1||root.GetComponent<Light>().type!=LightType.Directional))return false;
        }return true;
    }
    static void RecordHarness(string name)
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;var proof=new HarnessEvidence{scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var scene=SceneManager.GetSceneAt(i);return new HarnessScene{name=scene.name,path=scene.path,dirty=scene.isDirty,active=scene==SceneManager.GetActiveScene(),ownedBatchBootstrap=OwnedBatchBootstrap(scene),roots=scene.GetRootGameObjects().Select(root=>new HarnessRoot{name=root.name,components=root.GetComponents<Component>().Select(component=>component==null?"MISSING_SCRIPT":component.GetType().AssemblyQualifiedName).ToArray(),position=root.transform.position,rotation=root.transform.rotation,scale=root.transform.localScale}).ToArray()};}).ToArray()};Directory.CreateDirectory(args[at+1]);File.WriteAllText(Path.Combine(args[at+1],name),JsonUtility.ToJson(proof,true));TestContext.WriteLine(JsonUtility.ToJson(proof));
    }
    static void Set(object o,string field,object value)=>o.GetType().GetField(field).SetValue(o,value);
    static object Get(object o,string field)=>o.GetType().GetField(field).GetValue(o);
    static void Fixture(out object side,out object review,out object certificate)
    {
        certificate=New(Bridge,"Certificate");foreach(string field in new[]{"productionEligible","currentProfilesExact","currentExternalConfigurationExact","currentWorldExact","currentNwhExact","unitAncestors","noTestDependencies","sourceClosureExact"})Set(certificate,field,true);
        Set(certificate,"scope","ONE_CANONICAL_SCENE_ROW_EXACT_PRODUCER_TO_NATIVE_MOUNTS_TO_NATIVE_PHYSICAL_COURSE_CHAIN_NOT_NEW_POSE_OR_WORLD_FLIGHT_ACCEPTANCE");Set(certificate,"status","CURRENT_CANONICAL_PHYSICAL_COURSE_SOURCE_CERTIFIED_RECORDED_NATIVE_POSES_NOT_NEW_REPLAY");Set(certificate,"receipt",Receipt);Set(certificate,"target",Canonical);foreach(string field in new[]{"receiptSha256","oldSceneSha256","currentSceneSha256","metaSha256"})Set(certificate,field,H);Set(certificate,"choices",8);Set(certificate,"originalTrucks",4);Set(certificate,"recordedInfernoPoses",699);Set(certificate,"recordedFirePoses",537);Set(certificate,"protectedFiles",5900);
        side=New(Bridge,"Receipt");Set(side,"candidate","MetadataOnly");Set(side,"coursePromotionReceipt",Receipt);Set(side,"coursePromotionReceiptSha256",H);Set(side,"certificate",JsonUtility.ToJson(certificate,true));foreach(string field in new[]{"canonicalSceneSha256","worldPrefabSha256","worldPreviewSha256","worldIntegrationSha256"})Set(side,field,H);
        var q=New(Author,"QualifiedCourseSource");Set(q,"scene",Canonical);Set(q,"sourceScene",Canonical);Set(q,"canonicalSourceReview",true);Set(q,"canonicalSourceCertificate",JsonUtility.ToJson(certificate,true));Set(q,"sceneSha256",H);Set(q,"status","CURRENT_CANONICAL_COURSE_SOURCE_CERTIFIED_WITH_RECORDED_GEOMETRY_NOT_NEW_REPLAY");Set(q,"straightExitDistance",55f);foreach(string field in new[]{"currentConfigurationExact","currentProfilesExact","currentNwhExact","unitAncestors","noTestDependencies","rootActivationExact","currentSourceBytesExact"})Set(q,field,true);
        var f=New(Author,"QualifiedCourseSourceFile");Set(f,"path","Assets/metadata-only.asset");Set(f,"sha256",H);var fa=Array.CreateInstance(f.GetType(),1);fa.SetValue(f,0);Set(q,"currentSourceSnapshot",fa);
        review=New(Review,"Receipt");Set(review,"candidate","MetadataOnly");Set(review,"status","SAMPLED_FULL_MOUNT_ENVELOPE_CLEAR_REQUIRES_ACTUAL_FLIGHT");Set(review,"ridersScene",Canonical);Set(review,"ridersSceneSha256",H);Set(review,"worldPrefabSha256",H);Set(review,"worldPreviewSha256",H);foreach(string field in new[]{"fixedQualifiedCourse","coordinatedCourse","sourceBytesPreserved"})Set(review,field,true);Set(review,"queries",100);Set(review,"turnQueries",20);Set(review,"gateCount",12);Set(review,"physicalGateColliders",12);Set(review,"straightExitDistance",55f);Set(review,"qualifiedSource",q);
        var gateType=review.GetType().GetField("courseLayout").FieldType.GetElementType();Assert.That(gateType.FullName,Is.EqualTo("LWS.TruckDragonRiders.DragonRiderGateDefinition"));Assert.That(gateType.Assembly.GetName().Name,Is.EqualTo("LWS.TruckDragonRiders.Rules"),"Actual installed Rules asmdef owns this type; never guess Assembly-CSharp.");var gates=Array.CreateInstance(gateType,12);for(int i=0;i<12;i++){var g=Activator.CreateInstance(gateType);Set(g,"center",new Vector3(i*150,220,330));Set(g,"rotation",Quaternion.identity);Set(g,"clearRadius",60f);gates.SetValue(g,i);}Set(review,"courseLayout",gates);Set(review,"originalCourseHash",Review.GetMethod("GateHash").Invoke(null,new object[]{gates}));
        var rr=Array.CreateInstance(Review.GetNestedType("Rig"),8);var sr=(IList)Get(q,"rigs");int at=0;for(int crew=0;crew<4;crew++)foreach(string species in new[]{"dragon-inferno","dragon-fire"})
        {
            int poses=species=="dragon-fire"?537:699;var r=New(Review,"Rig");var s=New(Author,"QualifiedCourseRigSource");foreach(var v in new[]{r,s}){Set(v,"crew",crew);Set(v,"species",species);Set(v,"recordedSourcePoses",poses);Set(v,"recordedMixerLabelledPoses",1);Set(v,"controller","Assets/metadata-"+species+".overrideController");Set(v,"controllerSha256",H);Set(v,"clips",new[]{"Assets/metadata-clip.anim"});Set(v,"clipSha256",new[]{H});}
            Set(r,"poseAuthority","PAIRED_FULL_NATIVE_GEOMETRY_CURRENT_PROFILE_EQUIVALENCE_NOT_NEW_POSE_REPLAY");Set(r,"skinnedVertices",100);Set(r,"originalTruckColliders",1);Set(r,"originalTruckRenderers",1);Set(r,"maximumGateRadialExtent",50f);Set(s,"skinPoints",100);Set(s,"anyAttitudeRadius",50f);Set(s,"envelopeAuthority","CURRENT_AUTHORED_BOX_CENTER_TRANSFORMED_BY_CURRENT_LOCAL_TO_WORLD_NOT_PHYSX_CACHE");var labels=Enumerable.Repeat("actual-source-pose",poses).ToArray();labels[0]="actual two-layer blend";Set(s,"poseLabels",labels);rr.SetValue(r,at++);sr.Add(s);
        }Set(review,"rigs",rr);
    }
    static void Metadata(object side,object review,object certificate)=>Bridge.GetMethod("RequireMetadata",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{side,review,JsonUtility.ToJson(certificate,true),"MetadataOnly",Receipt,H,H,H,H});
    [Test]public void ExactRecordedMetadataNeedsNoInventedFreshCountersOrComparisonLifecycle()
    {Fixture(out var side,out var review,out var certificate);Assert.DoesNotThrow(()=>Metadata(side,review,certificate));var q=Get(review,"qualifiedSource");foreach(string flag in new[]{"candidateClosedForComparison","comparisonClosedBeforeCandidateReopen","candidateRestored"})Assert.That(Get(q,flag),Is.EqualTo(false),"This explicit certificate path does not fabricate old comparison lifecycle.");}
    [TestCase("scope")][TestCase("test-only")][TestCase("old-sha")][TestCase("current-sha")][TestCase("fresh")][TestCase("world")][TestCase("radius")][TestCase("contacts")][TestCase("no-queries")][TestCase("missing-crew")][TestCase("duplicate-crew")][TestCase("recorded-count")][TestCase("source-state")][TestCase("empty-snapshot")][TestCase("mixer")][TestCase("clip")][TestCase("world-prefab")][TestCase("scene-path")][TestCase("receipt-path")][TestCase("rig-count")][TestCase("source-duplicate")][TestCase("source-invalid-sha")][TestCase("source-stale")][TestCase("root-activation")][TestCase("controller")][TestCase("clip-path")][TestCase("course-hash")][TestCase("unknown-world-certificate")][TestCase("review-recorded-count")]
    public void RecordedPhysicalAuthorityRejectsIndependentProvenanceOrGeometryMutation(string fault)
    {
        Fixture(out var side,out var review,out var certificate);var q=Get(review,"qualifiedSource");var r=((Array)Get(review,"rigs")).GetValue(0);var sources=(IList)Get(q,"rigs");var s=sources[0];
        if(fault=="scope")Set(certificate,"scope","TEST_ONLY");if(fault=="test-only")Set(certificate,"productionEligible",false);if(fault=="old-sha")Set(certificate,"oldSceneSha256",new string('b',64));if(fault=="current-sha")Set(certificate,"currentSceneSha256",new string('b',64));if(fault=="fresh")Set(r,"poses",170);if(fault=="world")Set(side,"worldIntegrationSha256",new string('b',64));if(fault=="radius")Set(((Array)Get(review,"courseLayout")).GetValue(0),"clearRadius",32f);if(fault=="contacts")Set(review,"contactQueries",1);if(fault=="no-queries")Set(review,"queries",0);if(fault=="missing-crew")sources.RemoveAt(0);if(fault=="duplicate-crew")Set(r,"crew",1);if(fault=="recorded-count")Set(s,"recordedSourcePoses",170);if(fault=="source-state")Set(q,"currentProfilesExact",false);if(fault=="empty-snapshot")Set(q,"currentSourceSnapshot",Array.CreateInstance(Author.GetNestedType("QualifiedCourseSourceFile"),0));if(fault=="mixer")Set(s,"recordedMixerLabelledPoses",2);if(fault=="clip")Set(r,"clipSha256",new[]{new string('b',64)});
        if(fault=="world-prefab")Set(review,"worldPrefabSha256",new string('b',64));if(fault=="scene-path")Set(review,"ridersScene","Assets/Wrong.unity");if(fault=="receipt-path")Set(side,"coursePromotionReceipt","Logs/Wrong.json");if(fault=="rig-count")Set(review,"rigs",Array.CreateInstance(r.GetType(),7));if(fault=="source-duplicate"){var files=(Array)Get(q,"currentSourceSnapshot");var duplicate=Array.CreateInstance(files.GetType().GetElementType(),2);duplicate.SetValue(files.GetValue(0),0);var alias=JsonUtility.FromJson(JsonUtility.ToJson(files.GetValue(0)),files.GetType().GetElementType());Set(alias,"path",Path.GetFullPath((string)Get(alias,"path")));duplicate.SetValue(alias,1);Set(q,"currentSourceSnapshot",duplicate);}if(fault=="source-invalid-sha")Set(((Array)Get(q,"currentSourceSnapshot")).GetValue(0),"sha256",new string('x',64));if(fault=="source-stale")Set(q,"currentSourceBytesExact",false);if(fault=="root-activation")Set(q,"rootActivationExact",false);if(fault=="controller")Set(r,"controllerSha256",new string('b',64));if(fault=="clip-path")Set(r,"clips",new[]{"Assets/Other.anim"});if(fault=="course-hash")Set(review,"originalCourseHash","CHANGED");if(fault=="unknown-world-certificate")Set(certificate,"currentWorldExact",false);if(fault=="review-recorded-count")Set(r,"recordedSourcePoses",698);
        var error=Assert.Throws<TargetInvocationException>(()=>Metadata(side,review,certificate));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]public void MetadataAloneCannotAuthorizeWorldAdoptionWithoutTheSpecificPassedNativeCase()
    {var before=EditorSceneManager.GetSceneManagerSetup();Assert.Throws<TargetInvocationException>(()=>Bridge.GetMethod("ValidateForWorldAdoption").Invoke(null,new object[]{"MetadataOnly","Logs/nonexistent-review.json","Logs/nonexistent-sidecar.json","Logs/nonexistent-specific-native-tests.xml",Receipt}));CollectionAssert.AreEqual(before.Select(s=>s.path),EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path));}
    [Test]public void MissingSpecificNativeWorldAdoptionCannotAuthorizeRecordedSourceMigration()
    {var before=EditorSceneManager.GetSceneManagerSetup();Assert.Throws<TargetInvocationException>(()=>Bridge.GetMethod("ValidateForCourseMigration").Invoke(null,new object[]{"MetadataOnly","Logs/nonexistent-review.json","Logs/nonexistent-adoption.json","Logs/nonexistent-sidecar.json","Logs/nonexistent-specific-native-tests.xml",Receipt,""}));CollectionAssert.AreEqual(before.Select(s=>s.path),EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path));}
    [Test]public void LegacyWorldAdoptionStillRejectsRecordedOnlyWithoutExplicitPhysicalSidecar()
    {
        Assert.That(Environment.GetCommandLineArgs().Contains("-tsDragonPhysicalCourseWorldReceipt"),Is.False,"This control explicitly exercises the original default branch.");Fixture(out var side,out var review,out var certificate);string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Temp"),path=Path.Combine(folder,"PhysicalCourseLegacy-"+Guid.NewGuid().ToString("N")+".json");Directory.CreateDirectory(folder);File.WriteAllText(path,JsonUtility.ToJson(review,true));
        try{var plan=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAdoptionPlan, Assembly-CSharp-Editor",true);var error=Assert.Throws<TargetInvocationException>(()=>plan.GetMethod("ValidateMountedCourseEvidence",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"MetadataOnly",path}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("Actual eight-rig coordinated mounted-course geometry"));}finally{File.Delete(path);}
    }
    [TestCase("absent")][TestCase("single")][TestCase("duplicate")][TestCase("missing")][TestCase("empty")][TestCase("another-flag")]
    public void ExplicitPhysicalCourseSelectionPreservesLegacyDefaultAndRejectsAmbiguity(string fault)
    {
        string key="-tsDragonCanonicalPhysicalCourseReceipt";string[] args=fault=="absent"?new[]{"-runTests"}:fault=="single"?new[]{key,Receipt}:fault=="duplicate"?new[]{key,Receipt,key,Receipt}:fault=="missing"?new[]{key}:new[]{key,fault=="empty"?"":"-runTests"};var method=Author.GetMethod("ExplicitPhysicalCourseReceipt",BindingFlags.NonPublic|BindingFlags.Static);if(fault=="absent")Assert.That(method.Invoke(null,new object[]{args}),Is.Null);else if(fault=="single")Assert.That(method.Invoke(null,new object[]{args}),Is.EqualTo(Path.GetFullPath(Receipt)));else{var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{args}));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    }
    [Test]public void CanonicalSnapshotUsesOnePhysicalKeyForRelativeAbsoluteAliasesWhileHistoricalPathsRemainUnchanged()
    {
        var method=Author.GetMethod("QualifiedCourseSnapshotPaths",BindingFlags.NonPublic|BindingFlags.Static);var paths=new[]{Canonical,Path.GetFullPath(Canonical)};var canonical=(string[])method.Invoke(null,new object[]{paths,true});var historical=(string[])method.Invoke(null,new object[]{paths,false});Assert.That(canonical.Length,Is.EqualTo(1));Assert.That(canonical[0],Is.EqualTo(Path.GetFullPath(Canonical)));CollectionAssert.AreEqual(paths,historical);
    }
    [Test]public void MissingActualPhysicalCourseCannotOpenAnySourceOrChangeIncomingSceneContext()
    {
        var before=EditorSceneManager.GetSceneManagerSetup();var bytes=before.Where(s=>!string.IsNullOrEmpty(s.path)).ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));object[] args={default(Scene),"Logs/no-production","Logs/no-course","Logs/no-real-physical-course-receipt.json"};Assert.Throws<TargetInvocationException>(()=>Author.GetMethod("ValidateQualifiedCanonicalPhysicalCourseSource").Invoke(null,args));Assert.That(((Scene)args[0]).IsValid(),Is.False);CollectionAssert.AreEqual(before.Select(s=>s.path),EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path));foreach(var row in bytes)CollectionAssert.AreEqual(row.Value,File.ReadAllBytes(row.Key));
    }
    [UnityTest,Timeout(1800000)]
    public IEnumerator ActualCertifiedPhysicalCanonicalCourseClearsPassedWorldWithoutAdoption()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,generated));AssetDatabase.Refresh();var canonical=File.ReadAllBytes(Canonical);var meta=File.ReadAllBytes(Canonical+".meta");
        RecordHarness("physical-course-harness-before.json");
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var incoming=SceneManager.GetSceneAt(i);if(string.IsNullOrEmpty(incoming.path))
            {
                Assert.That(OwnedBatchBootstrap(incoming),Is.True,"Preserve unknown/interactive untitled scenes. Only the actual batch EditMode single empty/default-camera-light bootstrap may be snapshotted; inspect physical-course-harness-before.json.");
                // Installed CreateBootstrapSceneTask creates the fresh default
                // camera/light scene for EditMode. Save its entire state before
                // closing it; never clear dirtiness or discard user objects.
                Assert.That(EditorSceneManager.SaveScene(incoming,generated+"PhysicalCourseRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True,"Preserve the actual owned bootstrap in a unique saved scene.");
            }
            Assert.That(incoming.isDirty||string.IsNullOrEmpty(incoming.path),Is.False,"Preserve dirty named/manual scene edits; only the identified batch bootstrap is owned.");
        }RecordHarness("physical-course-harness-after-snapshot.json");
        var setup=EditorSceneManager.GetSceneManagerSetup();var bytes=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"PhysicalCourseIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try{Bridge.GetMethod("ReviewActualCertifiedCourseAgainstPassedWorld").Invoke(null,null);yield return null;var result=Bridge.GetProperty("LastReceipt").GetValue(null);Assert.That(Get(result,"status"),Is.EqualTo("CANONICAL_PHYSICAL_COURSE_WORLD_CLEARANCE_CERTIFIED_RECORDED_SOURCE_NOT_FLIGHT_OR_ADOPTION"));Assert.That(Get(result,"failure"),Is.Null.Or.Empty);Assert.That(Get(result,"sourceBytesPreserved"),Is.True);Assert.That(Get(result,"freshPoseSamples"),Is.EqualTo(0));}
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var pair in bytes)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key));CollectionAssert.AreEqual(canonical,File.ReadAllBytes(Canonical));CollectionAssert.AreEqual(meta,File.ReadAllBytes(Canonical+".meta"));}
    }
    [Test,Timeout(1800000)]
    public void ActualSuccessfulMountToCourseChainRejectsIndependentOriginalHeaderMutation()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonCanonicalPhysicalCourseReceipt");Assert.That(at>=0&&at+1<args.Length,Is.True,"Actual successful canonical physical-course receipt required.");string path=args[at+1];var courseBytes=File.ReadAllBytes(path);var courseType=Author.GetNestedType("CanonicalCourseApply",BindingFlags.NonPublic);object actual=JsonUtility.FromJson(System.Text.Encoding.UTF8.GetString(courseBytes),courseType);var upstream=Author.GetMethod("RequireCourseUpstreamCanonicalMount",BindingFlags.NonPublic|BindingFlags.Static);object mount=upstream.Invoke(null,new[]{actual});Assert.That(Get(mount,"canonicalTarget"),Is.EqualTo(true));Assert.That(Get(mount,"nativeCalls"),Is.EqualTo(8));
        object plan=Get(actual,"plan");string mountPath=(string)Get(plan,"mountReceipt");var mountBytes=File.ReadAllBytes(mountPath);string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Temp");Directory.CreateDirectory(folder);string temporary=Path.Combine(folder,"PhysicalCourseHeader-"+Guid.NewGuid().ToString("N")+".json");int rejected=0;
        try
        {
            foreach(string fault in new[]{"old-sha","new-sha","metadata","project","target","source-closure","profiles","world","configuration","native-calls"})
            {
                var changed=JsonUtility.FromJson(System.Text.Encoding.UTF8.GetString(mountBytes),mount.GetType());if(fault=="old-sha")Set(changed,"sourceSha256","WRONG");if(fault=="new-sha")Set(changed,"resultSha256","WRONG");if(fault=="metadata")Set(changed,"metaSha256","WRONG");if(fault=="project")Set(changed,"projectRoot","F:/WrongProject");if(fault=="target")Set(changed,"target","Assets/Wrong.unity");if(fault=="source-closure")Set(changed,"sourceClosureExact",false);if(fault=="profiles")Set(changed,"profilesExact",false);if(fault=="world")Set(changed,"worldExact",false);if(fault=="configuration")Set(changed,"configurationExact",false);if(fault=="native-calls")Set(changed,"nativeCalls",7);File.WriteAllText(temporary,JsonUtility.ToJson(changed,true));var probe=JsonUtility.FromJson(System.Text.Encoding.UTF8.GetString(courseBytes),courseType);Set(Get(probe,"plan"),"mountReceipt",temporary);var error=Assert.Throws<TargetInvocationException>(()=>upstream.Invoke(null,new[]{probe}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message.StartsWith("Course certificate requires",StringComparison.Ordinal)||error.InnerException.Message.StartsWith("Exact producer",StringComparison.Ordinal),Is.True,"Reject the mutated authority header itself, not the temporary path's later inventory mismatch.");rejected++;
            }Assert.That(rejected,Is.EqualTo(10));
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);CollectionAssert.AreEqual(courseBytes,File.ReadAllBytes(path));CollectionAssert.AreEqual(mountBytes,File.ReadAllBytes(mountPath));}
    }
}
#endif
