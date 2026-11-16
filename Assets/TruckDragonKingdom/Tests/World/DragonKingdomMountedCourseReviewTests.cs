#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomMountedCourseReviewTests
{
    [UnityTest,Timeout(900000)]
    public IEnumerator CalibratedActualAnimatedSkinAndCompleteOriginalTrucksClearCandidateOrderedCourseConservatively()
        =>Run("ReviewCandidate");
    [UnityTest,Timeout(900000)]
    public IEnumerator CoordinatedPhysicalCourseClearsActualEightAnimatedMountsAndOriginalTrucksWithoutChangingRules()
        =>Run("ReviewCoordinatedCourseCandidate");
    [UnityTest,Timeout(900000)]
    public IEnumerator QualifiedSavedProductionProfilesAndFixedPhysicalCourseClearTheActuallyPassedWorld()
    {
        Assert.That(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonMountedProductionEvidence"),Is.GreaterThanOrEqualTo(0),"Explicit actual production qualification required.");yield return Run("ReviewCoordinatedCourseCandidate");
    }
    [Test,Timeout(900000)]
    public void CurrentQualifiedProductionSceneReusesFullNativeBoxCenteredGeometryWithoutWorldAdoption()
    {
        var args=Environment.GetCommandLineArgs();string Required(string key){int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,key);return args[at+1];}
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string evidence=Required("-tsDragonMountedProductionEvidence"),course=Required("-tsRiderProductionCourseEvidence");string path=(string)author.GetMethod("QualifiedMountedCourseScenePath").Invoke(null,new object[]{evidence});var before=File.ReadAllBytes(path);
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();for(int i=0;i<SceneManager.sceneCount;i++){var incoming=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(incoming.path))Assert.That(EditorSceneManager.SaveScene(incoming,generated+"QualifiedCourseSourceRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);}
        var setup=EditorSceneManager.GetSceneManagerSetup();Scene loaded=default;
        for(int i=0;i<SceneManager.sceneCount;i++){var current=SceneManager.GetSceneAt(i);Assert.That(current.isDirty||current.path==path,Is.False,"Preserve user scenes; close the actual source before this read-only probe.");}
        var incomingBytes=setup.Where(row=>!string.IsNullOrEmpty(row.path)).ToDictionary(row=>row.path,row=>File.ReadAllBytes(row.path));var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolation,generated+"QualifiedCourseSourceIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            loaded=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);object[] invocation={loaded,evidence,course};object proof;try{proof=author.GetMethod("ValidateQualifiedMountedCourseSource").Invoke(null,invocation);}finally{loaded=(Scene)invocation[0];}object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
            foreach(string name in new[]{"currentConfigurationExact","currentProfilesExact","currentNwhExact","unitAncestors","noTestDependencies","candidateClosedForComparison","comparisonClosedBeforeCandidateReopen","candidateRestored","rootActivationExact","currentSourceBytesExact"})Assert.That((bool)Field(proof,name),Is.True,name);Assert.That((float)Field(proof,"straightExitDistance"),Is.EqualTo(55));var rigs=(System.Collections.IEnumerable)Field(proof,"rigs");int count=0;
            foreach(var rig in rigs){count++;string species=(string)Field(rig,"species");Assert.That((int)Field(rig,"recordedSourcePoses"),Is.EqualTo(species=="dragon-fire"?537:699));Assert.That((int)Field(rig,"recordedMixerLabelledPoses"),Is.GreaterThan(0));Assert.That((string)Field(rig,"envelopeAuthority"),Is.EqualTo("CURRENT_AUTHORED_BOX_CENTER_TRANSFORMED_BY_CURRENT_LOCAL_TO_WORLD_NOT_PHYSX_CACHE"));Assert.That(((string[])Field(rig,"poseLabels")).Length,Is.EqualTo((int)Field(rig,"recordedSourcePoses")));}
            Assert.That(count,Is.EqualTo(8));
        }
        finally{var owned=loaded.IsValid()&&loaded.isLoaded?loaded:SceneManager.GetSceneByPath(path);if(owned.IsValid()&&owned.isLoaded)EditorSceneManager.CloseScene(owned,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var pair in incomingBytes)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),"Incoming scene bytes changed during isolated qualification.");CollectionAssert.AreEqual(before,File.ReadAllBytes(path),"Read-only production course source probe changed the saved scene.");}
    }
    [Test]
    public void NativeQualifiedSphereSweepDetectsThinContactBetweenItsSampleCenters()
    {
        var obstacle=new GameObject("Validation only thin intermediate sphere-sweep blocker");
        try
        {
            var box=obstacle.AddComponent<BoxCollider>();box.size=Vector3.one*.001f;box.transform.position=new Vector3(.5f,1.999f,0);Physics.SyncTransforms();Assert.That(Array.IndexOf(Physics.OverlapSphere(Vector3.zero,2),box),Is.EqualTo(-1));Assert.That(Array.IndexOf(Physics.OverlapSphere(Vector3.right,2),box),Is.EqualTo(-1));Assert.That(Array.IndexOf(Physics.OverlapSphere(Vector3.right*.5f,2),box),Is.GreaterThanOrEqualTo(0),"Actual swept sphere contacts the thin obstacle between endpoint queries.");
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomMountedCourseReview, Assembly-CSharp-Editor",true);var receipt=Activator.CreateInstance(type.GetNestedType("Receipt"));receipt.GetType().GetField("fixedQualifiedCourse").SetValue(receipt,true);receipt.GetType().GetField("qualifiedSweepRadius").SetValue(receipt,2f);var contacts=Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(type.GetNestedType("Contact")));var blockers=new System.Collections.Generic.HashSet<Collider>{box};
            type.GetMethod("Probe",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{"validation-only-half-spacing",Vector3.zero,Vector3.right,new Bounds(Vector3.zero,Vector3.one),blockers,receipt,contacts});Assert.That((int)receipt.GetType().GetField("contactQueries").GetValue(receipt),Is.GreaterThan(0),"Half-spacing expansion must prevent an actual thin blocker from falling between samples.");
        }
        finally{UnityEngine.Object.DestroyImmediate(obstacle);}
    }
    [Test]
    public void NativeQualifiedSphereUsesTruckCrossingCenterRatherThanOffsetHeadingBounds()
    {
        var obstacle=new GameObject("Validation only current crossing-center blocker");
        try
        {
            var box=obstacle.AddComponent<BoxCollider>();box.size=Vector3.one*.1f;Vector3 crossing=new Vector3(20,0,0);box.transform.position=crossing+Vector3.up*1.9f;Physics.SyncTransforms();var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomMountedCourseReview, Assembly-CSharp-Editor",true);var receipt=Activator.CreateInstance(type.GetNestedType("Receipt"));receipt.GetType().GetField("fixedQualifiedCourse").SetValue(receipt,true);receipt.GetType().GetField("qualifiedSweepRadius").SetValue(receipt,2f);var contacts=Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(type.GetNestedType("Contact")));var blockers=new System.Collections.Generic.HashSet<Collider>{box};
            type.GetMethod("ProbePose",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{"validation-only-crossing-reference",crossing,Quaternion.identity,new Bounds(new Vector3(100,0,0),Vector3.one),0f,blockers,receipt,contacts});Assert.That((int)receipt.GetType().GetField("contactQueries").GetValue(receipt),Is.EqualTo(1),"An offset display/heading AABB must never replace the actual truck-box crossing center.");
        }
        finally{UnityEngine.Object.DestroyImmediate(obstacle);}
    }
    static IEnumerator Run(string method)
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";
        Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"MountedCourseRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve user edits before read-only mounted course review.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"MountedCourseIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomMountedCourseReview, Assembly-CSharp-Editor",true);
            type.GetMethod(method).Invoke(null,null);yield return null;
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((bool)Field(receipt,"sourceBytesPreserved"),Is.True);
            var rigs=(Array)Field(receipt,"rigs");Assert.That(rigs.Length,Is.GreaterThanOrEqualTo(8));
            foreach(var rig in rigs)
            {
                if((int)Field(rig,"recordedSourcePoses")>0){Assert.That((int)Field(rig,"recordedSourcePoses"),Is.EqualTo((string)Field(rig,"species")=="dragon-fire"?537:699));Assert.That((int)Field(rig,"recordedMixerLabelledPoses"),Is.GreaterThan(0));foreach(string count in new[]{"poses","baseFlightPoses","configuredGraphPoses","nativeMixerPoses"})Assert.That((int)Field(rig,count),Is.Zero,"A reused source receipt is not fresh pose/graph sampling.");}
                else{Assert.That((int)Field(rig,"poses"),Is.EqualTo((int)Field(rig,"baseFlightPoses")+(int)Field(rig,"configuredGraphPoses")));Assert.That((int)Field(rig,"baseFlightPoses"),Is.EqualTo(170));}Assert.That((int)Field(rig,"skinnedVertices"),Is.GreaterThan(0));
                Assert.That((int)Field(rig,"originalTruckColliders"),Is.GreaterThan(0));Assert.That((int)Field(rig,"originalTruckRenderers"),Is.GreaterThan(0));
            }
            Assert.That((int)Field(receipt,"gateCount"),Is.GreaterThanOrEqualTo(8));Assert.That((int)Field(receipt,"queries"),Is.GreaterThan(1000));
            Assert.That((int)Field(receipt,"physicalGateColliders"),Is.EqualTo((int)Field(receipt,"gateCount")));
            Assert.That((int)Field(receipt,"turnQueries"),Is.GreaterThan(0));
            Assert.That((int)Field(receipt,"contactQueries"),Is.Zero,"Conservative bounds contacts require exact geometry diagnosis or real course adjustment; inspect the complete receipt.");
            Assert.That((int)Field(receipt,"apertureFailures"),Is.Zero,"Do not widen/award a gate without adapting the actual physical course under explicit review.");
            Assert.That((string)Field(receipt,"status"),Is.EqualTo("SAMPLED_FULL_MOUNT_ENVELOPE_CLEAR_REQUIRES_ACTUAL_FLIGHT"));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif
