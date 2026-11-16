#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomNativeSettlementFootingsTests
{
    [TestCase("unchanged")][TestCase("duplicate-id")][TestCase("wrong-id")][TestCase("source")][TestCase("pose")][TestCase("missing")]
    public void CapturedRepeatedHarborNamesRequireDistinctNativeRootsAndExactSourcePose(string mutation)
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonRetainedInventoryControl");Assert.That(at>=0&&at+1<args.Length,Is.True,"Actual native fullworld inventory/seater receipt required.");string path=args[at+1];var original=File.ReadAllBytes(path);
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var receipt=JsonUtility.FromJson(File.ReadAllText(path),type.GetNestedType("Receipt"));object Read(object o,string f)=>o.GetType().GetField(f).GetValue(o);void Set(object o,string f,object value)=>o.GetType().GetField(f).SetValue(o,value);
        var integration=Read(receipt,"nativeArchitectureIntegration");var inventory=(Array)Read(integration,"retainedInventory");var retained=Read(receipt,"retainedNativeBuildings");var parts=(Array)Read(retained,"parts");Assert.That(parts.Length,Is.EqualTo(25));Assert.That(inventory.Length,Is.EqualTo(25));
        Assert.That(inventory.Cast<object>().GroupBy(p=>(string)Read(p,"region")+"/"+(string)Read(p,"name")).Any(g=>g.Count()>1),Is.True,"Reproduce actual repeated harbor names, not uniquely renamed placeholders.");
        // V414 captured native IDs on the independent inventory only. This
        // read-only control reconstructs the unique original-pose pairing; it
        // is not a world/terrain acceptance receipt or a modified asset.
        foreach(var part in parts){var matches=inventory.Cast<object>().Where(p=>(string)Read(p,"region")== (string)Read(part,"region")&&(string)Read(p,"name")== (string)Read(part,"name")&&(string)Read(p,"source")== (string)Read(part,"source")&&((Vector3)Read(p,"before")).Equals((Vector3)Read(part,"before"))).ToArray();Assert.That(matches.Length,Is.EqualTo(1));Set(part,"actualRootInstanceId",Read(matches[0],"actualRootInstanceId"));}
        var method=type.GetMethod("NativeRetainedFootingsMatch",BindingFlags.Static|BindingFlags.NonPublic);Assert.That((bool)method.Invoke(null,new object[]{inventory,retained}),Is.True,"Complete captured placements with repeated names are distinct roots.");var first=parts.GetValue(0);
        switch(mutation)
        {
            case "duplicate-id":Set(parts.GetValue(1),"actualRootInstanceId",Read(first,"actualRootInstanceId"));break;
            case "wrong-id":Set(first,"actualRootInstanceId",int.MaxValue);break;
            case "source":Set(first,"sourceGuid","unrelated-prefab");break;
            case "pose":Set(first,"before",(Vector3)Read(first,"before")+Vector3.right);break;
            case "missing":var shorter=Array.CreateInstance(parts.GetType().GetElementType(),24);Array.Copy(parts,shorter,24);Set(retained,"parts",shorter);break;
        }
        Assert.That((bool)method.Invoke(null,new object[]{inventory,retained}),Is.EqualTo(mutation=="unchanged"));CollectionAssert.AreEqual(original,File.ReadAllBytes(path));TestContext.WriteLine("CAPTURED_REPEATED_NAMES_NATIVE_ROOT_BINDING_CONTROL_"+mutation+"_NOT_TERRAIN_OR_SAVED_WORLD_ACCEPTANCE");
    }
    [Test,Timeout(300000)]
    public void ActualOriginalRetainedBuildingsUseIndividualLowerStructuralMeshBasesAndPreserveSourceGeometryBeforeTerrainIntegration()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var proof=type.GetMethod("CaptureRetainedNativeBuildingBaseStageFromCommandLine").Invoke(null,null);
        object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        Assert.That((bool)Field(proof,"sourceBytesPreserved"),Is.True);Assert.That((bool)Field(proof,"originalCriticalPosesAndGraphExact"),Is.True);Assert.That((bool)Field(proof,"allNativeTerrainContactsVerified"),Is.False,"This native base stage must not claim the subsequent coordinated terrain contact gate.");
        var parts=((Array)Field(proof,"parts")).Cast<object>().ToArray();var footings=((Array)Field(proof,"physicalFootings")).Cast<object>().ToArray();Assert.That(parts.Length,Is.GreaterThan(0));Assert.That(footings.Length,Is.EqualTo(parts.Length));Assert.That((string)Field(proof,"geometryWitnessSha256"),Has.Length.EqualTo(64));
        Assert.That(parts.Any(part=>(bool)Field(part,"usedCreekBankFallback")),Is.True,"The actual V314 original house inside the34m creek must use a documented dry bank parcel, never a water-guard waiver.");
        foreach(var part in parts)
        {
            if((bool)Field(part,"usedCreekBankFallback"))
            {
                var trials=((Array)Field(part,"trials")).Cast<object>().ToArray();Assert.That(trials.Length,Is.GreaterThan(192));Assert.That(trials.Take(192).All(t=>(string)Field(t,"phase")=="ORIGINAL_LOCAL192_0_TO_42M"&&(string)Field(t,"reason")!="ACCEPTED"),Is.True);
                int accepted=(int)Field(part,"trial");Assert.That(accepted,Is.InRange(192,269));Assert.That(((Array)Field(part,"fallbackParcels")).Length,Is.EqualTo(78));var last=trials[accepted];Assert.That((string)Field(last,"reason"),Is.EqualTo("ACCEPTED"));var parcel=Field(last,"creekParcel");Assert.That((float)Field(parcel,"existingRetainingBank"),Is.EqualTo(12));Assert.That(Mathf.Abs((float)Field(parcel,"alongOffset")),Is.LessThanOrEqualTo(48.001f));Assert.That((float)Field(parcel,"setbackMeters"),Is.InRange(0,16));Assert.That(((Vector3)Field(parcel,"normal")).magnitude,Is.EqualTo(1).Within(.0001));
                foreach(var water in ((Array)Field(last,"legacyWaterPoints")).Cast<object>()){Assert.That((bool)Field(water,"classifiedDry"),Is.True);Assert.That((float)Field(water,"creekBankDistance"),Is.GreaterThanOrEqualTo(12));}
            }
            Assert.That((bool)Field(part,"sourceGeometryAndScalePreserved"),Is.True);Assert.That((bool)Field(part,"originalRoadStartObjectiveAndWingClear"),Is.True);Assert.That((int)Field(part,"baseVertexCount"),Is.GreaterThanOrEqualTo(4));var bounds=(Bounds)Field(part,"measuredBaseFootprint");Assert.That(bounds.size.x,Is.InRange(.5f,65));Assert.That(bounds.size.z,Is.InRange(.5f,65));Assert.That(bounds.min.y,Is.EqualTo((float)Field(part,"groundSurface")).Within(.02f));}
    }
}
#endif
