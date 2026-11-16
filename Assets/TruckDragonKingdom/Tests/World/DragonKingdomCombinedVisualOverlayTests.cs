#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public sealed class DragonKingdomCombinedVisualOverlayTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Receipt(string type,string json)=>JsonUtility.FromJson(json,Owner.GetNestedType(type));
    const string Tower="{\"status\":\"WORLD_ALIGNED_ORIGINAL_MESH_COLOR_CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW\",\"graph\":{\"periodMeters\":10},\"materials\":[{}],\"originalRendererMeshesExact\":true,\"noRenderMeshSubstitution\":true,\"noUvRemapping\":true,\"originalColliders\":5957,\"originalTerrains\":16,\"originalPerches\":4,\"appearance\":{\"sourceBytesExact\":true,\"originalWorldStateRestored\":true}}";
    const string Tile="{\"interpolationPayloadExact\":true,\"sameVisualPhysicalProvider\":true,\"layersExact\":true}";
    const string Texture="{\"explicitStandaloneBc7\":true,\"nonReadable\":true,\"materialSemanticsExact\":true,\"notUpscaled\":true}";
    static string Land=>"{\"status\":\"SAVED_VENDOR_LANDSCAPE_SURFACES_CAPTURED_REQUIRES_DIRECT_VISUAL_REVIEW_NOT_ADOPTION\",\"savedReloaded\":true,\"originalWorldExact\":true,\"originalSourcesExact\":true,\"originalWorldBefore\":\"same\",\"tiles\":["+Tile+","+Tile+","+Tile+","+Tile+"]}";
    static string Budget=>"{\"status\":\"SAVED_2K_BC7_MEMORY_AND_QUALITY_CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW_NOT_ADOPTION\",\"savedReloaded\":true,\"originalWorldExact\":true,\"originalSourcesExact\":true,\"all166PlacementGeometryExact\":true,\"allLodAndPhysicsExact\":true,\"memoryBudgetPassed\":true,\"uncompressedReviewDependenciesExcluded\":true,\"instances\":166,\"canopies\":26,\"worldBefore\":\"same\",\"textures\":["+Texture+","+Texture+","+Texture+"]}";
    static void Validate(string tower,string land,string budget)
    {try{Owner.GetMethod("ValidateCombinedVisualSources").Invoke(null,new[]{Receipt("TowerWorldAlignedReview",tower),Receipt("LandscapeSurfaceReview",land),Receipt("CanopyBudgetReview",budget)});}catch(TargetInvocationException error){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();}}
    [Test]public void CoherentSourceReceiptShapeIsOnlyEligibilityNotNativeOrHumanAcceptance(){Assert.DoesNotThrow(()=>Validate(Tower,Land,Budget));}
    [Test]public void PendingCompressedCandidateCannotMasqueradeAsPassedComposition(){Assert.Throws<InvalidOperationException>(()=>Validate(Tower,Land,Budget.Replace("SAVED_2K_BC7_MEMORY_AND_QUALITY_CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW_NOT_ADOPTION","IN_PROGRESS")));}
    [Test]public void ChangedTerrainClassOrPhysicalProviderCannotEnterAppearanceOverlay(){Assert.Throws<InvalidOperationException>(()=>Validate(Tower,Land.Replace("\"layersExact\":true","\"nwhIndexMismatches\":1,\"layersExact\":true"),Budget));}
    [Test]public void DifferentPhysicalWorldCannotBeCombinedFromUnrelatedVisualTrials(){Assert.Throws<InvalidOperationException>(()=>Validate(Tower,Land,Budget.Replace("same","other")));}
    [Test]public void MeshRemappingCannotBeHiddenBySuccessfulColorReceipt(){Assert.Throws<InvalidOperationException>(()=>Validate(Tower.Replace("\"noUvRemapping\":true","\"noUvRemapping\":false"),Land,Budget));}
    [Test]public void RepeatedVendorPropNamesRetainEverySiblingAndNestedComponentInIdentityCapture()
    {
        var setup=(SceneSetup[])Owner.GetMethod("PrepareValidQuayProbeSceneSetup",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);Scene scene=default;GameObject source=null;string folder="Assets/TruckDragonKingdom/Tests/World/Generated/CombinedIdentity-"+Guid.NewGuid().ToString("N"),path=folder+"/identity-fixture.unity";Directory.CreateDirectory(folder);AssetDatabase.Refresh();var worldType=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);var capture=Owner.GetMethod("CombinedLocalIds",BindingFlags.NonPublic|BindingFlags.Static);
        try
        {
            scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);source=new GameObject("P_PROP_cart_01");source.AddComponent<BoxCollider>();new GameObject("Wheel").transform.SetParent(source.transform,false);var prefab=PrefabUtility.SaveAsPrefabAsset(source,folder+"/cart.prefab");Assert.That(prefab,Is.Not.Null);UnityEngine.Object.DestroyImmediate(source);source=null;var root=new GameObject("Repeated vendor names identity fixture");root.AddComponent(worldType);for(int i=0;i<2;i++){var prop=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);prop.name="P_PROP_cart_01";prop.transform.SetParent(root.transform,false);}new GameObject("Excluded owned dressing").transform.SetParent(root.transform,false);Assert.That(EditorSceneManager.SaveScene(scene,path),Is.True);
            var map=(System.Collections.IDictionary)capture.Invoke(null,new object[]{root.GetComponent(worldType),root.transform.Find("Excluded owned dressing")});var keys=map.Keys.Cast<string>().ToArray();Assert.That(keys.Length,Is.EqualTo(8));Assert.That(keys.Distinct().Count(),Is.EqualTo(8));Assert.That(map.Values.Cast<string>().Distinct().Count(),Is.EqualTo(8),"Each source-prefab instance/component must keep its full distinct GlobalObjectId including instance identity.");foreach(int i in new[]{0,1}){Assert.That(keys.Any(k=>k.StartsWith("@/"+i+":P_PROP_cart_01/0:Wheel|",StringComparison.Ordinal)),Is.True);Assert.That(keys.Count(k=>k.StartsWith("@/"+i+":P_PROP_cart_01",StringComparison.Ordinal)),Is.EqualTo(3));}Assert.That(keys.Any(k=>k.Contains("Excluded owned dressing")),Is.False);
            EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);root=scene.GetRootGameObjects().Single(g=>g.name=="Repeated vendor names identity fixture");var reloaded=(System.Collections.IDictionary)capture.Invoke(null,new object[]{root.GetComponent(worldType),root.transform.Find("Excluded owned dressing")});Assert.That(reloaded.Count,Is.EqualTo(map.Count));foreach(string key in keys)Assert.That(reloaded[key],Is.EqualTo(map[key]),key);
        }
        finally{if(source!=null)UnityEngine.Object.DestroyImmediate(source);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    static object Field(object obj,string name)=>obj.GetType().GetField(name).GetValue(obj);
    [UnityTest]public IEnumerator ActualSavedCombinedWorldPreservesGeometryIdentityClassificationAndAll166PartsAndRenders18Views()
    {
        yield return(IEnumerator)Owner.GetMethod("CaptureCombinedVisualOverlayReview").Invoke(null,null);var r=Owner.GetProperty("LastCombinedVisualReview").GetValue(null);Assert.That(Field(r,"status"),Is.EqualTo("SAVED_COMBINED_VISUAL_OVERLAY18VIEWS_REQUIRES_LEAD_REVIEW_NOT_CANONICAL_ADOPTION"));foreach(string name in new[]{"savedReloaded","allOriginalFieldsExact","oldSceneLocalIdsExact","dressingPlacementExact","nwhIndicesExact","allTerrainPhysicalPayloadsExact","sourceBytesExact","incomingSceneContextRestored","uncompressedDependenciesExcluded"})Assert.That(Field(r,name),Is.EqualTo(true),name);foreach(var pair in new[]{Tuple.Create("originalColliders",5957),Tuple.Create("originalTerrains",16),Tuple.Create("towerRenderers",4),Tuple.Create("terrainProviderPairs",4),Tuple.Create("instances",166),Tuple.Create("canopies",26)})Assert.That(Field(r,pair.Item1),Is.EqualTo(pair.Item2));Assert.That(((Array)Field(r,"frames")).Length,Is.EqualTo(18));var bindings=Field(r,"towerBindings");Assert.That(Field(bindings,"materialMatchCount"),Is.EqualTo(19));Assert.That(Field(bindings,"selectedSupportCount"),Is.EqualTo(4));Assert.That(Field(bindings,"unselectedMaterialMatchCount"),Is.EqualTo(15));
    }
}
#endif
