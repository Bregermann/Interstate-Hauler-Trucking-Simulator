#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class DragonKingdomReadableCanopyTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method).Invoke(null,args);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name).SetValue(value,data);
    [TestCase("shrub-only")][TestCase("undersized-canopy")][TestCase("missing-understory")]
    public void UsefulTotalPopulationCannotSubstituteForActualCanopyHeightAndUnderstoryRequirements(string fault)
    {
        var type=Owner.GetNestedType("VergeDressingPart");int count=fault=="missing-understory"?15:18;var parts=Array.CreateInstance(type,count);var sources=(string[])Call("BoundedVergeDressingSources");for(int i=0;i<count;i++){var part=Activator.CreateInstance(type);Set(part,"site","test-grove");Set(part,"source",i<4&&fault!="shrub-only"?sources[i%3]:sources[3]);Set(part,"guid","fixture-source-identity");Set(part,"bounds",new Bounds(Vector3.zero,new Vector3(15,i<4?26:4,15)));parts.SetValue(part,i);}if(fault=="undersized-canopy")Set(parts.GetValue(0),"bounds",new Bounds(Vector3.zero,new Vector3(15,21.9f,15)));var error=Assert.Throws<TargetInvocationException>(()=>Call("MeasureReadableCanopyIsland","test-grove",parts));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("shrub-only total-count success is rejected"));
    }
    [Test]
    public void ActualVendorAllKnotsProducesTwelveProjectedFittedCanopyChoicesWithoutAnAreaErosionFailure()
    {
        var proof=Call("InspectActualCanopyOnKnotsFixture");Assert.That(Field(proof,"actualProjectedPoints"),Is.EqualTo(12));Assert.That((float)Field(proof,"minimumFittedHeight"),Is.GreaterThanOrEqualTo(22));Assert.That((float)Field(proof,"maximumFittedHeight"),Is.LessThanOrEqualTo(30));Assert.That((float)Field(proof,"maximumProjectionError"),Is.LessThan(.001f));Assert.That(Field(proof,"startupNone"),Is.True);Assert.That(Field(proof,"noRetainedGenerator"),Is.True);TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [Test]
    public void ActualEarlierPassingPopulationIsExplicitlyRejectedAsACanopyComposition()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonDressingBaseline");Assert.That(at,Is.GreaterThanOrEqualTo(0));string path=args[at+1];var baseline=JsonUtility.FromJson(File.ReadAllText(path),Owner.GetNestedType("SupportedVergeReview"));Assert.That(Field(baseline,"status"),Is.EqualTo("SUPPORTED_SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_ADOPTION"));var dressing=Field(baseline,"dressing");Assert.That(Field(dressing,"savedReloaded"),Is.True);var parts=(Array)Field(dressing,"parts");var sites=((Array)Field(dressing,"sites")).Cast<object>().Where(s=>(string)Field(s,"kind")=="ISLAND");foreach(var site in sites){var error=Assert.Throws<TargetInvocationException>(()=>Call("MeasureReadableCanopyIsland",Field(site,"id"),parts));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());}TestContext.WriteLine("Earlier population/native preservation success is retained; it does not imply new readable canopy acceptance.");
    }
    [TestCase("outside-region")][TestCase("under-water")][TestCase("earlier-composition")][TestCase("legal")]
    public void CandidateEligibilityRetainsOriginalBoundsAndEarlierCompositionGuardsBeforeCounting(string fault)
    {
        var region=new Bounds(Vector3.zero,new Vector3(100,200,100));var art=new Bounds(new Vector3(0,12,0),new Vector3(12,24,12));var prior=new[]{new Bounds(new Vector3(30,5,30),new Vector3(5,10,5))};if(fault=="outside-region")art.center=new Vector3(48,12,0);if(fault=="under-water")art.center=new Vector3(0,10,0);if(fault=="earlier-composition")prior[0]=art;string reason=(string)Call("ReadableCanopyBoundsRejection",region,art,prior);Assert.That(reason,fault=="legal"?Is.Null:Does.Contain(fault=="outside-region"?"OUTSIDE_ORIGINAL_REGION":fault=="under-water"?"UNDER_WATER":"EARLIER_COMPOSITION"));
    }
    [Test]
    public void ActualVendorEarlyRejectedTreeDoesNotConsumeQuotaAndLaterValidTreesFillIt()
    {
        var proof=Call("InspectActualCanopyEarlyInvalidLateValidQuotaFixture");Assert.That(Field(proof,"vendorPoints"),Is.EqualTo(12));Assert.That((int)Field(proof,"attempted"),Is.GreaterThan(3));Assert.That(Field(proof,"accepted"),Is.EqualTo(3));Assert.That((int)Field(proof,"rejected"),Is.GreaterThan(0));foreach(string flag in new[]{"firstRejected","lateValidCompletedQuota","rejectedObjectsRemoved","earlierUntouched","startupNone","noRetainedGenerator"})Assert.That(Field(proof,flag),Is.True,flag);TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [UnityTest,Timeout(1200000)]
    public IEnumerator ActualSixSavedGrovesContainEighteenToThirtyRealCanopiesAndKeepEarlierTerracesAndApproaches()
    {
        yield return (IEnumerator)Call("CaptureReadableCanopyDressingReview");var review=Owner.GetProperty("LastReadableCanopyReview").GetValue(null);Assert.That(Field(review,"status"),Is.EqualTo("SAVED_REAL_CANOPIES_CAPTURED_REQUIRES_DIRECT_VISUAL_REVIEW_NOT_ADOPTION"));Assert.That(Field(review,"error"),Is.Null.Or.Empty);Assert.That(Field(review,"earlierTerraceAndJunctionPartsPreserved"),Is.True);Assert.That((int)Field(review,"totalCanopies"),Is.InRange(18,30));var islands=(Array)Field(review,"islands");Assert.That(islands.Length,Is.EqualTo(6));foreach(var island in islands){Assert.That((int)Field(island,"trees"),Is.InRange(3,5));Assert.That((int)Field(island,"understory"),Is.InRange(12,20));Assert.That((float)Field(island,"minimumHeight"),Is.GreaterThanOrEqualTo(22));Assert.That((float)Field(island,"maximumHeight"),Is.LessThanOrEqualTo(30));Assert.That(((string[])Field(island,"sourceSpecies")).Length,Is.GreaterThan(0));Assert.That(((Vector3[])Field(island,"measuredCanopyDimensions")).Length,Is.EqualTo(Field(island,"trees")));}var proof=Field(review,"dressing");foreach(string flag in new[]{"originalWorldExact","originalTerrainBytesExact","originalSourcesExact","savedReloaded","noAddedPhysics","noRuntimeGenerator","incomingSceneContextRestored"})Assert.That(Field(proof,flag),Is.True,flag);Assert.That(((Array)Field(proof,"frames")).Length,Is.EqualTo(12));TestContext.WriteLine("REAL_CANOPY_COUNTS_AND_MEASURED_SAVED_DIMENSIONS_NOT_FINISHED_PRESENTATION_ACCEPTANCE_LEAD_IMAGE_REVIEW_REQUIRED");
    }
}
#endif
