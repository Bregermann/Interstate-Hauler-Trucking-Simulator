#if UNITY_EDITOR && VISTA
using System;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomSchema4AdoptionTests
{
    static Type Plan=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAdoptionPlan, Assembly-CSharp-Editor",true);
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Schema4Receipt()
    {
        var receipt=Activator.CreateInstance(Integration.GetNestedType("Receipt"));var finish=Activator.CreateInstance(Integration.GetNestedType("LandmarkFinish"));finish.GetType().GetField("compositionSchema").SetValue(finish,4);receipt.GetType().GetField("finish").SetValue(receipt,finish);return receipt;
    }
    [TestCase(false)][TestCase(true)]
    public void CapturedRepeatedHouseSourcesMapByOriginalPoseAndRejectAmbiguousFootingRecords(bool duplicatePose)=>RetainedPlacementChainControl(duplicatePose?"duplicate-native":null);
    [TestCase("placement-before")][TestCase("placement-after")][TestCase("placement-operation")][TestCase("placement-source")][TestCase("duplicate-placement")]
    public void ActualRetainedHouseChainRejectsChangedOrAmbiguousPlacementEvidence(string mutation)=>RetainedPlacementChainControl(mutation);
    [TestCase("valid")][TestCase("replacement-source")][TestCase("replacement-link")][TestCase("native-source")][TestCase("saved-pose")][TestCase("duplicate-replacement")]
    public void ActualWindmillReplacementRetainsElevenBuildingBaselineAndRejectsChangedChain(string mutation)=>RetainedPlacementChainControl("windmill-"+mutation);
    static void RetainedPlacementChainControl(string mutation)
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonRetainedInventoryControl");Assert.That(at>=0&&at+1<args.Length,Is.True);string path=args[at+1];var before=File.ReadAllBytes(path);var receipt=JsonUtility.FromJson(File.ReadAllText(path),Integration.GetNestedType("Receipt"));object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);void Set(object value,string field,object data)=>value.GetType().GetField(field).SetValue(value,data);var parts=((Array)Read(Read(receipt,"retainedNativeBuildings"),"parts")).Cast<object>().ToArray();var history=((Array)Read(receipt,"placements")).Cast<object>().ToArray();
        const string operation="Ground the retained actual building mesh on its dry measured foundation near the original streetscape hint; exclude but retain steam effects";const string prefab="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";var originalBytes=File.ReadAllBytes(prefab);var source=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);var worldType=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);var world=source.GetComponent(worldType);var scene=EditorSceneManager.NewPreviewScene();GameObject candidate=null;
        try
        {
            candidate=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);var current=candidate.GetComponent(worldType);Transform Region(object owner,string id){var record=((Array)Read(owner,"regions")).Cast<object>().Single(r=>(string)Read(r,"id")==id);return (Transform)Read(record,"root");}
            object[] Chain(object part)=>history.Where(p=>(string)Read(p,"region")== (string)Read(part,"region")&&(string)Read(p,"name")== (string)Read(part,"name")&&(string)Read(p,"operation")==operation&&string.IsNullOrEmpty((string)Read(p,"source"))&&((Vector3)Read(p,"after")).Equals((Vector3)Read(part,"before"))).ToArray();
            var originalParts=parts.Where(part=>{var links=Chain(part);return links.Length==1&&Region(world,(string)Read(part,"region")).Cast<Transform>().Count(t=>t.name==(string)Read(part,"name")&&t.position.Equals((Vector3)Read(links[0],"before")))==1;}).ToArray();var windmill=parts.Single(part=>(string)Read(part,"region")=="village"&&(string)Read(part,"name")=="Complete owned windmill house14");
            Assert.That(originalParts.Length,Is.EqualTo(10),"Actual canonical source has ten retained house identities; the standalone sail is an explicit replacement, never an eleventh original house.");Assert.That(originalParts.Length+1,Is.EqualTo(11),"Preserve the full eleven-building baseline: ten retained houses plus one source-validated windmill replacement; fourteen additional buildings remain separate.");
            var originalSail=Region(world,"village").Cast<Transform>().Single(t=>t.name=="P_BLD_windmill_sail");var windmillSource=AssetDatabase.LoadAssetAtPath<GameObject>((string)Read(windmill,"source"));Assert.That(windmillSource,Is.Not.Null);var complete=(GameObject)PrefabUtility.InstantiatePrefab(windmillSource,Region(current,"village"));complete.name="Complete owned windmill house14";complete.transform.position=(Vector3)Read(windmill,"after");complete.transform.rotation=(Quaternion)Read(windmill,"rotation");complete.transform.localScale=(Vector3)Read(windmill,"scale");
            var windmillMethod=Plan.GetMethod("Schema4ReplacedWindmillCounterpart",BindingFlags.Static|BindingFlags.NonPublic);object[] windmillParameters={originalSail,Region(current,"village"),receipt};Assert.That((Transform)windmillMethod.Invoke(null,windmillParameters),Is.EqualTo(complete.transform));
            if(mutation!=null&&mutation.StartsWith("windmill-",StringComparison.Ordinal))
            {
                var replace=history.Single(p=>(string)Read(p,"region")=="village"&&(string)Read(p,"name")==complete.name&&(string)Read(p,"operation")=="Replace unsupported sail with complete same-vendor animated building");string fault=mutation.Substring("windmill-".Length);
                if(fault=="replacement-source")Set(replace,"source","Assets/UnreviewedReplacement.prefab");
                if(fault=="replacement-link")Set(replace,"after",(Vector3)Read(replace,"after")+Vector3.right*.001f);
                if(fault=="native-source")Set(windmill,"source","Assets/UnreviewedBuilding.prefab");
                if(fault=="saved-pose")complete.transform.position+=Vector3.right*.003f;
                if(fault=="duplicate-replacement"){var values=(Array)Read(receipt,"placements");var duplicate=Array.CreateInstance(values.GetType().GetElementType(),values.Length+1);Array.Copy(values,duplicate,values.Length);duplicate.SetValue(replace,values.Length);Set(receipt,"placements",duplicate);}
                if(fault=="valid")Assert.That((Transform)windmillMethod.Invoke(null,windmillParameters),Is.EqualTo(complete.transform));else{var failure=Assert.Throws<TargetInvocationException>(()=>windmillMethod.Invoke(null,windmillParameters));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("Ambiguous"));}return;
            }
            foreach(var part in originalParts){var link=Chain(part).Single();var roots=Region(current,(string)Read(part,"region")).Cast<Transform>().Where(t=>t.name==(string)Read(part,"name")&&t.position.Equals((Vector3)Read(link,"before"))).ToArray();Assert.That(roots.Length,Is.EqualTo(1));roots[0].position=(Vector3)Read(part,"after");}
            var repeated=parts.GroupBy(p=>(string)Read(p,"region")+"/"+(string)Read(p,"name")).First(g=>g.Count()>1&&g.Any(p=>originalParts.Contains(p))).ToArray();var target=repeated.First(p=>originalParts.Contains(p));string region=(string)Read(target,"region"),name=(string)Read(target,"name");var placement=Chain(target).Single();var old=Region(world,region).Cast<Transform>().Single(t=>t.name==name&&t.position.Equals((Vector3)Read(placement,"before")));
            if(mutation=="duplicate-native"){var duplicate=repeated.First(p=>p!=target);foreach(string field in new[]{"before","rotation","scale"})Set(duplicate,field,Read(target,field));}
            if(mutation=="placement-before")Set(placement,"before",(Vector3)Read(placement,"before")+Vector3.right*.001f);
            if(mutation=="placement-after")Set(placement,"after",(Vector3)Read(placement,"after")+Vector3.right*.001f);
            if(mutation=="placement-operation")Set(placement,"operation","Unreviewed source relocation");
            if(mutation=="placement-source")Set(placement,"source","Assets/Unreviewed.prefab");
            if(mutation=="duplicate-placement"){var values=(Array)Read(receipt,"placements");var duplicate=Array.CreateInstance(values.GetType().GetElementType(),values.Length+1);Array.Copy(values,duplicate,values.Length);duplicate.SetValue(placement,values.Length);Set(receipt,"placements",duplicate);}
            var method=Plan.GetMethod("Schema4RetainedCounterpart",BindingFlags.Static|BindingFlags.NonPublic);object[] parameters={old,region,Region(current,region),receipt};if(mutation!=null){var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,parameters));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("Ambiguous"));}else{var mapped=(Transform)method.Invoke(null,parameters);Assert.That(mapped,Is.Not.Null,"The actual original identity and full placement chain must be recognized.");Assert.That(mapped.position.Equals((Vector3)Read(target,"after")),Is.True);Assert.That(mapped.name,Is.EqualTo(name));}
        }
        finally{if(candidate!=null)UnityEngine.Object.DestroyImmediate(candidate);EditorSceneManager.ClosePreviewScene(scene);CollectionAssert.AreEqual(before,File.ReadAllBytes(path));CollectionAssert.AreEqual(originalBytes,File.ReadAllBytes(prefab));}
    }
    [Test]
    public void Schema4CannotBypassActualNativeSavedArchitectureEvidence()
    {
        var method=Plan.GetMethod("ValidateSchema4SavedCandidate",BindingFlags.NonPublic|BindingFlags.Static);var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new[]{null,Schema4Receipt()}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("actual full native frame"));
    }
    [Test]
    public void Schema4RejectsColliderDisableOutsideTheExactComponentIdentityWhitelist()
    {
        var source=new GameObject("Unreviewed source collider");var destination=new GameObject("Preserved destination collider");
        try
        {
            var from=source.AddComponent<BoxCollider>();from.enabled=false;var to=destination.AddComponent<BoxCollider>();var mapping=Activator.CreateInstance(Plan.GetNestedType("Mapping",BindingFlags.NonPublic),true);
            var method=Plan.GetMethod("CopyReviewedExistingFields",BindingFlags.NonPublic|BindingFlags.Static);var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new[]{(object)from,to,mapping,Schema4Receipt()}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("Unreviewed existing collider"));Assert.That(to.enabled,Is.True,"A rejected state change must not modify even the disposable target.");
        }
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(destination);}
    }
    [Test,Timeout(120000)]
    public void ActualNativeCastleRetirementMapsExactly26OriginalComponentIdsAndRejectsUnreviewedChanges()
    {
        string proof=(string)Integration.GetMethod("CaptureSchema4CastleComponentMapProbe").Invoke(null,null);Assert.That(proof,Does.Contain("\"retainedDisabledComponentIds\": 26"));Assert.That(proof,Does.Contain("\"actualFaultsRejected\": 3"));Assert.That(proof,Does.Contain("\"noCanonicalAdoptionPerformed\": true"));TestContext.WriteLine(proof);
    }
}
#endif
