#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class DragonKingdomSupportedVergeTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method).Invoke(null,args);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    [TestCase("too-many")][TestCase("outside-bound")][TestCase("vertical-offset")]
    public void SupportedParcelRevisionCannotTurnIntoUnboundedSearchOrChangeOriginalHeight(string fault)
    {
        var offsets=(Vector3[])Call("BoundedDressingParcelOffsets");if(fault=="too-many")offsets=offsets.Concat(new[]{new Vector3(3,0,3)}).ToArray();else if(fault=="outside-bound")offsets[1]=Vector3.right*85;else offsets[1]+=Vector3.up;var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>Call("ValidateDressingParcelOffsets",offsets));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void ActualOriginalSmallFixtureHasNoDistributedPointsWhileSupportedAreaProjectsAndRepeatsExactly()
    {
        var proof=Call("InspectActualDressingVendorProjectionFixture");Assert.That(Field(proof,"originalDistributed"),Is.EqualTo(0));Assert.That(Field(proof,"originalProjected"),Is.EqualTo(0));Assert.That((int)Field(proof,"supportedDistributed"),Is.GreaterThan(2));Assert.That((int)Field(proof,"supportedProjected"),Is.GreaterThan(2));Assert.That(Field(proof,"sameSeedExact"),Is.True);Assert.That((float)Field(proof,"maximumProjectionError"),Is.LessThan(.001f));Assert.That(Field(proof,"startupNone"),Is.True);Assert.That(Field(proof,"noRetainedGenerator"),Is.True);TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [UnityTest,Timeout(900000)]
    public IEnumerator ActualAllTwelveDressingSitesMeetOriginalUsefulPopulationThroughBoundedSameRegionParcels()
    {
        yield return (IEnumerator)Call("CaptureSupportedVendorVergeDressingReview");var review=Owner.GetProperty("LastSupportedVergeReview").GetValue(null);Assert.That(Field(review,"status"),Is.EqualTo("SUPPORTED_SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_ADOPTION"));Assert.That(Field(review,"error"),Is.Null.Or.Empty);var proof=Field(review,"dressing");foreach(string flag in new[]{"originalWorldExact","originalTerrainBytesExact","originalSourcesExact","savedReloaded","noAddedPhysics","noRuntimeGenerator","incomingSceneContextRestored"})Assert.That(Field(proof,flag),Is.True,flag);var groups=((Array)Field(proof,"groups")).Cast<object>().ToArray();Assert.That(groups.Length,Is.EqualTo(12));Assert.That(groups.Count(g=>(string)Field(g,"kind")=="ISLAND"),Is.EqualTo(6));Assert.That(groups.Count(g=>(string)Field(g,"kind")=="TERRACE"),Is.EqualTo(2));Assert.That(groups.Count(g=>(string)Field(g,"kind")=="JUNCTION"),Is.EqualTo(4));foreach(var group in groups)Assert.That((int)Field(group,"accepted"),Is.GreaterThanOrEqualTo((string)Field(group,"kind")=="ISLAND"?8:3));var attempts=((Array)Field(review,"parcelAttempts")).Cast<object>().ToArray();Assert.That(attempts.Count(a=>(bool)Field(a,"passed")),Is.EqualTo(12));Assert.That(attempts.All(a=>(int)Field(a,"trial")<13),Is.True);Assert.That(attempts.Where(a=>(bool)Field(a,"passed")).All(a=>(bool)Field(a,"originalRegionContained")),Is.True);Assert.That(((Array)Field(proof,"frames")).Length,Is.EqualTo(12));TestContext.WriteLine("BOUNDED_SAME_REGION_DRESSING_ONLY_ORIGINAL_BUFFER_AND_POPULATION_LIMITS_UNCHANGED_REQUIRES_DIRECT_VISUAL_REVIEW");
    }
}
#endif
