#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonKingdomRejectedFinishDiagnosticTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static Type Probe=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);
    static object Get(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static void Set(object value,string name,object field)=>value.GetType().GetField(name).SetValue(value,field);
    static object Invoke(string method,params object[] args)=>Integration.GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
    [Test]
    public void OmittedOptInNeverRequestsARejectedSnapshot()
    {Assert.That(Invoke("RejectedDiagnosticRequested",new object[]{Array.Empty<string>()}),Is.False);Assert.That(Invoke("RejectedDiagnosticRequested",new object[]{new[]{"-tsDragonFinishArt"}}),Is.False);Assert.That(Invoke("RejectedDiagnosticRequested",new object[]{new[]{"-tsDragonRetainRejectedWorld"}}),Is.True);}
    [TestCase("../escape","Assets/TruckDragonKingdom/VisualCandidates/../escape")]
    [TestCase("Good","Assets/TruckDragonKingdom/Prefabs")]
    [TestCase("Good","Assets/TruckDragonKingdom/VisualCandidates/Other")]
    [TestCase("Good/Bad","Assets/TruckDragonKingdom/VisualCandidates/Good/Bad")]
    public void CanonicalTraversalAndWrongCandidateFoldersAreRejectedBeforeWriting(string tag,string folder)
    {Assert.That(Assert.Throws<TargetInvocationException>(()=>Invoke("RejectedDiagnosticFolder",tag,folder)).InnerException,Is.TypeOf<InvalidOperationException>());}
    static object Metadata()
    {
        var p=Activator.CreateInstance(Integration.GetNestedType("RejectedWorldDiagnostic"));Set(p,"tag","MetadataOnly");Set(p,"folder","Assets/TruckDragonKingdom/VisualCandidates/MetadataOnly/RJ");Set(p,"prefab","Assets/TruckDragonKingdom/VisualCandidates/MetadataOnly/RJ/Shared Dragon Kingdom.prefab");Set(p,"definition","Assets/TruckDragonKingdom/VisualCandidates/MetadataOnly/RJ/Dragon Kingdom.asset");Set(p,"status","REJECTED_CURRENT_WORLD_SNAPSHOT_READABLE_ONLY_FOR_DIAGNOSIS_NOT_ADOPTION");Set(p,"criticalSha256","metadata-same-critical");Set(p,"criticalReloadedSha256","metadata-same-critical");
        foreach(string flag in new[]{"diagnosticReadable","savedReloaded","geometryExact","providersExact","rendererBindingsExact","worldConfigurationExact","liveStateRestored","diggerFilesExact","sourceBytesPreserved","mainSourceAuditRecorded"})Set(p,flag,true);
        var descriptor=Activator.CreateInstance(Probe.GetNestedType("RejectedWorldSignature"));Set(descriptor,"geometry","metadata-only");Set(descriptor,"providers","metadata-only");Set(descriptor,"rendererBindings","metadata-only");Set(descriptor,"worldConfiguration","metadata-only");Set(descriptor,"meshReferences",1);Set(descriptor,"colliders",1);Set(descriptor,"terrains",4);Set(p,"before",descriptor);Set(p,"reloaded",JsonUtility.FromJson(JsonUtility.ToJson(descriptor),descriptor.GetType()));
        var fileType=Integration.GetNestedType("RejectedDiagnosticFile");var file=Activator.CreateInstance(fileType);Set(file,"path","Logs/METADATA_ONLY_NOT_A_SOURCE_ASSET");Set(file,"sha256",new string('a',64));var files=Array.CreateInstance(fileType,1);files.SetValue(file,0);foreach(string key in new[]{"sourceFiles","snapshotFiles","diggerFiles"})Set(p,key,files);return p;
    }
    [Test]
    public void StructuralRejectedHeaderNeverAuthorizesAdoptionAndHasNoFileOrSceneSideEffects()
    {var proof=Metadata();Invoke("RequireRejectedDiagnosticHeader",proof);Assert.That(Get(proof,"authorizesAdoption"),Is.False,"This specimen proves only metadata shape; the real reader additionally requires exact rejected Main receipt, bytes, scene clone and native queries.");}
    [TestCase("accepted")][TestCase("adoption")][TestCase("unsealed")][TestCase("sources")][TestCase("geometry")][TestCase("providers")][TestCase("materials")][TestCase("live-state")][TestCase("digger")][TestCase("critical")][TestCase("owner")][TestCase("missing-descriptor")]
    public void RejectedHeaderRefusesIndependentStateOrProvenanceMutation(string fault)
    {
        var p=Metadata();switch(fault){case "accepted":Set(p,"status","PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW");break;case "adoption":Set(p,"authorizesAdoption",true);break;case "unsealed":Set(p,"mainSourceAuditRecorded",false);break;case "sources":Set(p,"sourceBytesPreserved",false);break;case "geometry":Set(p,"geometryExact",false);break;case "providers":Set(p,"providersExact",false);break;case "materials":Set(p,"rendererBindingsExact",false);break;case "live-state":Set(p,"liveStateRestored",false);break;case "digger":Set(p,"diggerFilesExact",false);break;case "critical":Set(p,"criticalReloadedSha256","changed");break;case "owner":Set(p,"prefab","Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab");break;case "missing-descriptor":Set(p,"reloaded",null);break;}
        Assert.That(Assert.Throws<TargetInvocationException>(()=>Invoke("RequireRejectedDiagnosticHeader",p)).InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void ExistingSavedRockPayloadAuthorityDetectsMeshAndColliderEditsAndExactRestoration()
    {
        var root=new GameObject("Disposable geometry identity control");Mesh mesh=null;
        try
        {
            var world=root.AddComponent(Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true));var child=new GameObject("Actual test triangle");child.transform.SetParent(root.transform,false);mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up};mesh.triangles=new[]{0,1,2};mesh.RecalculateBounds();child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>();var collider=child.AddComponent<BoxCollider>();var method=Probe.GetMethod("DescribeRejectedWorldWithoutMutation");object Capture()=>method.Invoke(null,new object[]{world});string before=JsonUtility.ToJson(Capture());var vertices=mesh.vertices;mesh.vertices=new[]{Vector3.forward*.25f,Vector3.right,Vector3.up};Assert.That(JsonUtility.ToJson(Capture()),Is.Not.EqualTo(before));mesh.vertices=vertices;Assert.That(JsonUtility.ToJson(Capture()),Is.EqualTo(before));collider.enabled=false;Assert.That(JsonUtility.ToJson(Capture()),Is.Not.EqualTo(before));collider.enabled=true;Assert.That(JsonUtility.ToJson(Capture()),Is.EqualTo(before));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);}
    }
    [Test,Explicit("Requires an actual opt-in retained rejected current-world snapshot; never builds or accepts one."),Timeout(900000)]
    public void ActualRetainedRejectedWorldRecordsOriginal61ArtistReasonsAndRestoresAllSourceState()
    {
        var proof=Integration.GetMethod("CaptureRetainedRejectedArtistPlacement").Invoke(null,null);foreach(string field in new[]{"originalGroupPosesRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);var placement=Get(proof,"placement");Assert.That(placement,Is.Not.Null);var trials=(Array)Get(placement,"placementTrials");Assert.That(trials.Length,Is.InRange(1,256));foreach(var row in trials){Assert.That(Get(row,"samples"),Is.EqualTo(49));Assert.That((string)Get(row,"rejectedReason"),Is.Not.Empty);Assert.That(((Array)Get(row,"floorPoints")).Length,Is.EqualTo(49));}Assert.That(Get(placement,"instances"),Is.EqualTo(61));Assert.That(Get(placement,"prefabs"),Is.EqualTo(28));TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [Test,Explicit("Requires an actual preserved rejected world; never regenerates or accepts it."),Timeout(900000)]
    public void ActualRetainedRejectedWorldMeasuresAll172RidgeRootsAndRestoresOriginalSourceState()
    {
        var proof=Integration.GetMethod("CaptureRetainedRejectedRidgePlacement").Invoke(null,null);foreach(string field in new[]{"original172PosesRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);var placement=Get(proof,"placement");Assert.That(placement,Is.Not.Null);Assert.That(((Array)Get(placement,"parts")).Length,Is.EqualTo(172));TestContext.WriteLine("ALL172_ACTUAL_RETAINED_RIDGE_DIAGNOSIS_NOT_FULL_WORLD_ACCEPTANCE "+JsonUtility.ToJson(proof,true));
    }
    [Test,Explicit("Requires the actual source-closed retained world; never regenerates or adopts it."),Timeout(900000)]
    public void ActualRetainedRejectedWorldMeasuresBothNaturalGrovesAndRestoresSourceAndSceneState()
    {
        var proof=Integration.GetMethod("CaptureRetainedRejectedNaturalGroves").Invoke(null,null);foreach(string field in new[]{"originalWorldRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);Assert.That(((Array)Get(proof,"groups")).Length,Is.EqualTo(2));TestContext.WriteLine("ACTUAL_BOTH_GROVES_MEASURED_NOT_FULL_WORLD_ACCEPTANCE "+JsonUtility.ToJson(proof,true));
    }

    [Test,Explicit("Uses actual original-grid nominal V463native mesh and complete source-closed restoration."),Timeout(900000)]
    public void ActualNominalRetainedCavernReproducesNativeGeometryInsideTheReservedDoorway()
    {
        var proof=Integration.GetMethod("CaptureRetainedRejectedNaturalGroves").Invoke(null,null);foreach(string field in new[]{"originalWorldRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);
        var clearance=Get(proof,"nominalDoorwayClearance");Assert.That(clearance,Is.Not.Null);Assert.That(Get(clearance,"nativeWallControlsPassed"),Is.True,"Native mesh wall rays must positively hit before an empty-space query counts");Assert.That(Get(clearance,"allJoinsClear"),Is.False,"The actual nominal rasterization defect must be reproduced, not inferred from a text label");TestContext.WriteLine("ACTUAL_NOMINAL_NATIVE_DOORWAY_NEGATIVE_CONTROL "+JsonUtility.ToJson(clearance,true));
    }

    [Test,Explicit("Read-only actual source-closed waterfront native bearing diagnosis; no support writes or adoption."),Timeout(900000)]
    public void ActualRetainedWaterfrontRecordsNativeUndersideFailuresWithoutChangingOriginalSourceGeometry()
    {
        Assert.That(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonWaterfrontBearingDiagnostic"),Is.GreaterThanOrEqualTo(0));var proof=Integration.GetMethod("CaptureRetainedRejectedNaturalGroves").Invoke(null,null);
        foreach(string field in new[]{"originalWorldRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);
        var bearings=(Array)Get(proof,"waterfrontBearings");Assert.That(bearings.Length,Is.GreaterThan(7));int failures=0;foreach(var row in bearings)if(!(bool)Get(row,"existingUndersideRayPassed"))failures++;Assert.That(failures,Is.GreaterThan(0),"Reproduce actual measured underside failure before changing placement");TestContext.WriteLine("ACTUAL_NATIVE_WATERFRONT_BEARINGS_NOT_SUPPORT_ACCEPTANCE "+JsonUtility.ToJson(proof,true));
    }
    [Test,Explicit("Actual retained source geometry and native contacts; does not install or accept complete waterfront supports."),Timeout(900000)]
    public void ActualRetainedWaterfrontResolvesEveryStationInsideItsExistingPileFootprintWithNativeUndersideContacts()
    {
        Assert.That(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonWaterfrontBearingDiagnostic"),Is.GreaterThanOrEqualTo(0));var proof=Integration.GetMethod("CaptureRetainedRejectedNaturalGroves").Invoke(null,null);foreach(string field in new[]{"originalWorldRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);
        var bearings=(Array)Get(proof,"waterfrontBearings");Assert.That(bearings.Length,Is.EqualTo(56));int shifted=0;foreach(var row in bearings)
        {
            Assert.That(Get(row,"boundedNativeBearingPassed"),Is.True);var requested=(Vector3)Get(row,"station");var resolved=(Vector3)Get(row,"resolvedStation");Assert.That(resolved.y,Is.EqualTo(requested.y));Assert.That(Vector3.Distance(requested,resolved),Is.LessThanOrEqualTo(.2501f));Assert.That(((Vector3)Get(row,"resolvedNormal")).y,Is.LessThan(-.35f));if((bool)Get(row,"existingUndersideRayPassed"))Assert.That(resolved.Equals(requested),Is.True,"Existing native contacts must stay exact");else shifted++;
        }
        Assert.That(shifted,Is.EqualTo(4));TestContext.WriteLine("ALL56_ACTUAL_NATIVE_BEARINGS_4_BOUNDED_EDGE_CORRECTIONS_NOT_COMPLETE_SUPPORT_ACCEPTANCE");
    }
    [Test,Explicit("Runs actual complete waterfront support on a temporary source-closed clone with no material/scene/prefab writes."),Timeout(900000)]
    public void ActualRetainedWaterfrontQualifiesNativePilesGangwayBoatBedAndFerryThenRestoresOriginalWorld()
    {
        Assert.That(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonWaterfrontSupportDiagnostic"),Is.GreaterThanOrEqualTo(0));var proof=Integration.GetMethod("CaptureRetainedRejectedWaterfrontSupport").Invoke(null,null);foreach(string field in new[]{"originalWorldRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);var support=Get(proof,"waterfrontSupports");Assert.That(support,Is.Not.Null);Assert.That(Get(support,"error"),Is.Null.Or.Empty);foreach(string field in new[]{"actualBedAndPhysicalSupportsPassed","sourceGeometryPreserved","originalRoadsAndFerryPreserved","sourceAndTerrainBytesPreserved","ferryDepartureClear"})Assert.That(Get(support,field),Is.True,field);Assert.That(Get(support,"supportedStructures"),Is.EqualTo(3));Assert.That(Get(support,"gangwayPlanks"),Is.GreaterThan(0));TestContext.WriteLine("ACTUAL_COMPLETE_WATERFRONT_NATIVE_SUPPORT_ON_DISPOSABLE_CLONE_NOT_FULL_WORLD_OR_RENDER_ACCEPTANCE");
    }
    [Test,Explicit("Uses the actual current failed-seam world and original measured Vista outputs; writes only twelve fresh owned data copies."),Timeout(900000)]
    public void ActualCurrentCoreCoastCopiesMeetOriginalSeamsAndNativeCollidersWithoutChangingCoreOrSourceAssets()
    {
        var proof=Integration.GetMethod("CaptureRetainedRejectedCoastCopies").Invoke(null,null);foreach(string field in new[]{"originalWorldRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);var copies=Get(proof,"coastCopies");Assert.That(copies,Is.Not.Null);Assert.That(Get(copies,"nativeHeightContactsPassed"),Is.True);Assert.That(Get(copies,"nativeHeightContacts"),Is.EqualTo(36));var coordination=Get(copies,"coordination");foreach(string field in new[]{"coreHeightmapsExact","originalSourcesExact","nonHeightPayloadExact","beyondCollarExact","passed"})Assert.That(Get(coordination,field),Is.True,field);Assert.That(((Array)Get(coordination,"tiles")).Length,Is.EqualTo(12));Assert.That((float)Get(Get(coordination,"before"),"innerMaximumError"),Is.GreaterThan(.04f),"Reproduce actual current-core mismatch first");Assert.That((string[])Get(Get(coordination,"after"),"issues"),Is.Empty);TestContext.WriteLine("ACTUAL_CURRENT_CORE_OUTER_COLLAR_AND36_NATIVE_CONTACTS_NOT_FULL_WORLD_OR_RENDER_ACCEPTANCE");
    }
    [Test,Explicit("Uses actual retained sixteen-terrain world to select all four original core tiles and measure every architecture parcel; no asset writes."),Timeout(900000)]
    public void ActualSixteenTerrainWorldRetainsExactlyFourOwnedCoreFieldsAndAll525NativeArchitectureContacts()
    {
        var proof=Integration.GetMethod("CaptureRetainedRejectedFinalArchitectureContacts").Invoke(null,null);foreach(string field in new[]{"originalWorldRestored","sourceFilesExact","sceneContextRestored"})Assert.That(Get(proof,field),Is.True,field);var actual=Get(proof,"finalArchitectureContacts");Assert.That(Get(actual,"totalTerrains"),Is.EqualTo(16));Assert.That(Get(actual,"selectedCoreTerrains"),Is.EqualTo(4));Assert.That(Get(actual,"excludedCoastTerrains"),Is.EqualTo(12));Assert.That(Get(actual,"cavernMaterialCoreTerrains"),Is.EqualTo(4));Assert.That(Get(actual,"cavernMaterialCoreSelectionExact"),Is.True);CollectionAssert.AreEqual((string[])Get(actual,"coreData"),(string[])Get(actual,"cavernMaterialCoreData"));var support=Get(actual,"support");Assert.That(Get(support,"passed"),Is.True);Assert.That(Get(support,"nativeColliderRaysPerformed"),Is.True);Assert.That(Get(support,"parcels"),Is.EqualTo(525));Assert.That((int)Get(support,"samples"),Is.GreaterThanOrEqualTo(4725));TestContext.WriteLine("ACTUAL16_TERRAINS_FOUR_OWNED_CORE_AND525_NATIVE_PARCELED_CONTACTS_NOT_FULL_WORLD_OR_RENDER_ACCEPTANCE");
    }

}
#endif
