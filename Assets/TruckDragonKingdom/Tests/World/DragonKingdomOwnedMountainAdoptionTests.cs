#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonKingdomOwnedMountainAdoptionTests
{
    static Type Plan=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAdoptionPlan, Assembly-CSharp-Editor",true);
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static MethodInfo Method(string name)=>Plan.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
    static object Get(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static void Set(object value,string name,object field)=>value.GetType().GetField(name).SetValue(value,field);
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    static string Arg(string key){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit actual passed bounded road source evidence required: "+key);return args[at+1];}
    static void Rejected(string method,params object[] arguments){var failure=Assert.Throws<TargetInvocationException>(()=>Method(method).Invoke(null,arguments));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());}
    [TestCase("status")][TestCase("error")][TestCase("sourceBytesPreserved")][TestCase("originalPhysicalGatesPassed")][TestCase("candidateRoadAuthorityAndCriticalPosesExact")]
    public void BoundedOrFailedWorldNeverQualifiesMountainAdoption(string failure)
    {
        var receipt=Activator.CreateInstance(Integration.GetNestedType("Receipt"));Set(receipt,"status","PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW");foreach(string field in new[]{"sourceBytesPreserved","originalPhysicalGatesPassed","candidateRoadAuthorityAndCriticalPosesExact"})Set(receipt,field,true);
        if(failure=="status")Set(receipt,failure,"SAVED_ROAD_ONLY_TERRAIN_WORLD_PENDING");else if(failure=="error")Set(receipt,failure,"retained genuine terrain failure");else Set(receipt,failure,false);
        Rejected("RequirePassedMountainWorld",receipt);
    }
    [TestCase("none")][TestCase("other-edge")][TestCase("node")][TestCase("target-width")][TestCase("target-station")][TestCase("target-cost")][TestCase("region-center")][TestCase("minimum-width")][TestCase("allowed-region-bounds")]
    public void ActualSaved646RoadAllowsOnlyTheDeclaredTargetDefinitionChange(string mutation)
    {
        string evidence=Arg("-tsDragonOwnedMountainAdoptionSource"),receiptPath=Path.Combine(evidence,"rendered-evidence","saved-owned-mountain-world-authority.json");Assert.That(File.Exists(receiptPath),Is.True);var report=JsonUtility.FromJson<SavedSource>(File.ReadAllText(receiptPath));Assert.That(report.savedReloaded&&report.originalWorldSourcesExact&&string.IsNullOrEmpty(report.error),Is.True);
        string originalPath="Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset",candidatePath=Path.GetDirectoryName(report.candidatePrefab).Replace('\\','/')+"/Dragon Kingdom.asset";var paths=new[]{originalPath,candidatePath,report.road.layout,receiptPath}.SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToDictionary(path=>path,Hash);
        var original=AssetDatabase.LoadAssetAtPath<ScriptableObject>(originalPath);var saved=AssetDatabase.LoadAssetAtPath<ScriptableObject>(candidatePath);var layout=AssetDatabase.LoadAssetAtPath<ScriptableObject>(report.road.layout);Assert.That(original&&saved&&layout,Is.True);var candidate=UnityEngine.Object.Instantiate(saved);
        try
        {
            var graph=Get(candidate,"graph");var edges=(IList)Get(graph,"edges");object target=edges.Cast<object>().Single(edge=>(string)Get(edge,"edgeId")=="caverns-volcano");
            switch(mutation)
            {
                case "other-edge":var other=edges.Cast<object>().First(edge=>edge!=target);Set(other,"speedLimitMph",(float)Get(other,"speedLimitMph")+1);break;
                case "node":var node=((IList)Get(graph,"nodes"))[0];Set(node,"position",(Vector3)Get(node,"position")+Vector3.up*.125f);break;
                case "target-width":Set(target,"laneWidthMeters",(float)Get(target,"laneWidthMeters")+.125f);break;
                case "target-station":var sample=((IList)Get(target,"samples"))[300];Set(sample,"position",(Vector3)Get(sample,"position")+Vector3.up*.125f);break;
                case "target-cost":Set(target,"travelCost",(float)Get(target,"travelCost")+1);break;
                case "region-center":var region=((Array)Get(candidate,"regions")).GetValue(0);Set(region,"center",(Vector3)Get(region,"center")+Vector3.right);break;
                case "minimum-width":Set(candidate,"minimumRoadWidth",(float)Get(candidate,"minimumRoadWidth")+1);break;
                case "allowed-region-bounds":var boundsRegion=((Array)Get(candidate,"regions")).GetValue(0);Set(boundsRegion,"bounds",new Bounds(new Vector3(10,20,30),new Vector3(500,500,500)));break;
            }
            object[] values={original,candidate,layout,false};if(mutation=="none"||mutation=="allowed-region-bounds")Assert.DoesNotThrow(()=>Method("ValidateMountainDefinitionDelta").Invoke(null,values));else Rejected("ValidateMountainDefinitionDelta",values);
            // Even the real saved646 route is not full-world acceptance.
            var incomplete=Activator.CreateInstance(Integration.GetNestedType("Receipt"));Set(incomplete,"ownedMountainRoad",JsonUtility.FromJson(JsonUtility.ToJson(report.road),Integration.GetNestedType("OwnedMountainRoadEvidence")));Rejected("RequirePassedMountainWorld",incomplete);
        }
        finally{UnityEngine.Object.DestroyImmediate(candidate);foreach(var pair in paths)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Read-only definition controls changed actual source: "+pair.Key);}
    }
    [Test]
    public void ActualRejectedFullWorldStillRejectsDespiteItsQualifiedRoad()
    {
        string path=Arg("-tsDragonRejectedWorldReceipt");Assert.That(File.Exists(path),Is.True);string before=Hash(path);var receipt=JsonUtility.FromJson(File.ReadAllText(path),Integration.GetNestedType("Receipt"));Assert.That(Get(receipt,"ownedMountainRoad"),Is.Not.Null,"Use actual failed integrated owned-road world evidence.");Assert.That((string)Get(receipt,"status"),Is.Not.EqualTo("PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"));Rejected("RequirePassedMountainWorld",receipt);Assert.That(Hash(path),Is.EqualTo(before));
    }
    [TestCase("none")][TestCase("root-pose")][TestCase("renderer")][TestCase("mesh")][TestCase("child")]
    public void RetiredRoadSubtreeKeepsAllOriginalSerializedPropertiesExceptRootActive(string mutation)
    {
        const string path="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";string before=Hash(path);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var source=prefab.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Kingdom caverns-volcano · EasyRoads generated course");var oldWorld=new GameObject("Validation only original road frame");var newWorld=new GameObject("Validation only candidate road frame");
        try
        {
            var a=UnityEngine.Object.Instantiate(source.gameObject,oldWorld.transform).transform;var b=UnityEngine.Object.Instantiate(source.gameObject,newWorld.transform).transform;a.name=b.name=source.name;b.gameObject.SetActive(false);
            if(mutation=="root-pose")b.localPosition+=Vector3.right*.01f;
            if(mutation=="renderer")b.GetComponentInChildren<Renderer>(true).enabled=false;
            if(mutation=="mesh")b.GetComponentInChildren<MeshFilter>(true).sharedMesh=null;
            if(mutation=="child")new GameObject("Unreviewed geometry").transform.SetParent(b,false);
            object[] values={a,b,oldWorld.transform,newWorld.transform,true};if(mutation=="none")Assert.DoesNotThrow(()=>Method("RequireMountainSubtreeExact").Invoke(null,values));else Rejected("RequireMountainSubtreeExact",values);
            if(mutation=="none")Rejected("RequireMountainSubtreeExact",a,b,oldWorld.transform,newWorld.transform,false);
        }
        finally{UnityEngine.Object.DestroyImmediate(oldWorld);UnityEngine.Object.DestroyImmediate(newWorld);Assert.That(Hash(path),Is.EqualTo(before));}
    }
    [Serializable]sealed class SavedSource{public string candidatePrefab,error;public bool savedReloaded,originalWorldSourcesExact;public RoadSource road;}
    [Serializable]sealed class RoadSource{public string layout;}
}
#endif
