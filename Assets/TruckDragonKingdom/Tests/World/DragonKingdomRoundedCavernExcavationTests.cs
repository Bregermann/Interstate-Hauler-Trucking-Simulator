#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

public sealed class DragonKingdomRoundedCavernExcavationTests
{
    [UnityTest,Timeout(1500000)]
    public IEnumerator ActualRoundedDiggerExcavationUsesFreshFoundationAndPreservesOriginalSourcesWithoutPretendingWholeWorldAcceptance()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(UnityEngine.Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"RoundedCaveRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve incoming user scenes.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var incoming=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolation,generated+"RoundedCaveIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        try
        {
            yield return (IEnumerator)type.GetMethod("CreateIsolatedRoundedCavernProbe").Invoke(null,null);
            var proof=type.GetProperty("LastRoundedCavernProbe").GetValue(null);object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
            Assert.That((bool)Field("sourcesExact"),Is.True);Assert.That((bool)Field("criticalPosesExact"),Is.True);Assert.That((bool)Field("saved"),Is.True);
            Assert.That((string)Field("scope"),Does.Contain("NOT_WHOLE_WORLD"));Assert.That((string)Field("status"),Does.StartWith("ACTUAL_ROUNDED_CAVE_COLUMNS_SAVED"));
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernProbeFinish"))
            {
                Assert.That((bool)Field("authoredInnerPaletteBeforeDigger"),Is.True);
                Assert.That(((Array)Field("innerWeightPreparation")).Length,Is.EqualTo(4));
                var saved=AssetDatabase.LoadAssetAtPath<GameObject>((string)Field("prefab"));Assert.That(saved,Is.Not.Null);
                var transforms=saved.GetComponentsInChildren<Transform>(true);
                foreach(var pair in new[]{new{prefix="Owned underground crystal outcrop ",count=12},new{prefix="Owned working lantern caverns ",count=12},new{prefix="Owned supported cavern rock ",count=8}})
                    Assert.That(transforms.Count(t=>t.name.StartsWith(pair.prefix,StringComparison.Ordinal)),Is.EqualTo(pair.count),"Actual saved source-art membership, not just a receipt count.");
                foreach(var lamp in transforms.Where(t=>t.name.StartsWith("Owned working lantern caverns ",StringComparison.Ordinal)))Assert.That(lamp.GetComponentsInChildren<Light>(true).Length,Is.EqualTo(1));
                var terrain=saved.GetComponentsInChildren<Terrain>(true).First();var rock=terrain.terrainData.terrainLayers[3].diffuseTexture;Assert.That(rock,Is.Not.Null);
                var systems=saved.GetComponentsInChildren<Component>(true).Where(c=>c!=null&&c.GetType().FullName=="Digger.Modules.Core.Sources.DiggerSystem").ToArray();Assert.That(systems.Length,Is.EqualTo(2));
                foreach(var system in systems)
                {
                    var materials=(Material[])system.GetType().GetField("Materials").GetValue(system);Assert.That(materials.Length,Is.GreaterThan(0));Assert.That(materials[0].HasProperty("_Splat3"),Is.True);Assert.That(materials[0].GetTexture("_Splat3"),Is.EqualTo(rock),"Actual persisted Digger material must bind the authored rock diffuse source.");
                }
            }
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernAnalyticLobes"))
            {
                var cavern=Field("cavern");var rounded=cavern.GetType().GetField("roundedProfile").GetValue(cavern);object Lobe(string name)=>rounded.GetType().GetField(name).GetValue(rounded);
                Assert.That((int)cavern.GetType().GetField("nativePrimitiveBrushes").GetValue(cavern),Is.EqualTo(4));
                if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernDigPasses")){var args=Environment.GetCommandLineArgs();Assert.That((int)cavern.GetType().GetField("brushPasses").GetValue(cavern),Is.EqualTo(int.Parse(args[Array.IndexOf(args,"-tsDragonCavernDigPasses")+1],System.Globalization.CultureInfo.InvariantCulture)));}
                Assert.That((bool)Lobe("nativeAnalyticLobeInputs"),Is.True);Assert.That(((Array)Lobe("analyticLobeCenters")).Length,Is.EqualTo(4));Assert.That(((Array)Lobe("analyticLobeRadii")).Length,Is.EqualTo(4));Assert.That(((Array)Lobe("vendorInputVoxelCounts")).Length,Is.Zero);
            }
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernAuthoredBooleanUnion"))
            {
                var cavern=Field("cavern");var union=cavern.GetType().GetField("authoredUnion").GetValue(cavern);Assert.That(union,Is.Not.Null);object Union(string name)=>union.GetType().GetField(name).GetValue(union);
                Assert.That(cavern.GetType().GetField("authoredFloor").GetValue(cavern),Is.Not.Null);Assert.That((string)cavern.GetType().GetField("floorRestorationKind").GetValue(cavern),Is.EqualTo("AUTHORED_SIGNED_PLANE_INTERSECTION"));Assert.That((int)cavern.GetType().GetField("customBrushes").GetValue(cavern),Is.EqualTo(8));
                Assert.That((int)cavern.GetType().GetField("brushPasses").GetValue(cavern),Is.EqualTo(1));Assert.That((int)Union("sourceOperations"),Is.EqualTo(12));Assert.That((int)Union("analyticLobes"),Is.EqualTo(4));Assert.That((int)Union("vendorCustomCorridors"),Is.EqualTo(8));Assert.That((int)Union("appliedPasses"),Is.EqualTo(1));Assert.That((bool)Union("usesSignedExteriorBand"),Is.True);Assert.That((bool)Union("vendorCustomInputsExact"),Is.True);
                var operands=(Array)Union("operands");Assert.That(operands.Length,Is.EqualTo(12));int exteriorChanges=0;
                foreach(var operand in operands)
                {
                    object Operand(string name)=>operand.GetType().GetField(name).GetValue(operand);Assert.That((int)Operand("modifiedChunks"),Is.GreaterThan(0));exteriorChanges+=(int)Operand("changedExteriorSamples");
                    if((string)Operand("brush")=="Custom"){Assert.That((int)Operand("vendorInputVoxels"),Is.GreaterThan(0));Assert.That((string)Operand("inputBeforeSha256"),Is.Not.Empty);Assert.That((string)Operand("inputAfterSha256"),Is.EqualTo((string)Operand("inputBeforeSha256")));}
                }
                Assert.That(exteriorChanges,Is.GreaterThan(0),"The actual candidate must retain the signed exterior side for native interpolation; clipped positive-only edits are insufficient.");
            }
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernRasterClearance"))
            {
                var cavern=Field("cavern");var rounded=cavern.GetType().GetField("roundedProfile").GetValue(cavern);Assert.That((string)rounded.GetType().GetField("rasterClearancePolicy").GetValue(rounded),Is.EqualTo("OneMeasuredHorizontalVoxelDiagonalEachSide01"));Assert.That((float)rounded.GetType().GetField("measuredRoadSideGuardBand").GetValue(rounded),Is.GreaterThan(0));
                var active=SceneManager.GetActiveScene();var measurementScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);GameObject loaded=null;
                try
                {
                    loaded=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>((string)Field("prefab")),measurementScene);var world=loaded.GetComponent(Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true));Physics.SyncTransforms();var digger=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);var clearance=digger.GetMethod("MeasureNativeDoorwaySpace").Invoke(null,new object[]{world});
                    var args=Environment.GetCommandLineArgs();string output=args[Array.IndexOf(args,"-tsDragonOutput")+1];File.WriteAllText(Path.Combine(output,"actual-raster-native-doorway-space.json"),JsonUtility.ToJson(clearance,true));Assert.That((bool)clearance.GetType().GetField("nativeWallControlsPassed").GetValue(clearance),Is.True);Assert.That((bool)clearance.GetType().GetField("allJoinsClear").GetValue(clearance),Is.True,"Fresh native geometry must clear original reserved doorway space, not merely record a new policy label");
                }
                finally{if(loaded!=null)UnityEngine.Object.DestroyImmediate(loaded);EditorSceneManager.CloseScene(measurementScene,true);if(active.IsValid()&&active.isLoaded)EditorSceneManager.SetActiveScene(active);}
            }
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernNativeFaceNormalsDiagnostic"))
            {
                var face=Field("nativeFaceNormals");Assert.That(face,Is.Not.Null);object Detail(string name)=>face.GetType().GetField(name).GetValue(face);
                Assert.That((string)Detail("status"),Is.EqualTo("NATIVE_FACE_NORMAL_PAIR_RECORDED_REQUIRES_DIRECT_VISUAL_REVIEW"));Assert.That(((Array)Detail("frames")).Length,Is.EqualTo(2));Assert.That((int)Detail("repolygonizedMeshes"),Is.GreaterThan(0));Assert.That((int)Detail("meshesWithIntendedNormalChanges"),Is.GreaterThan(0));
                foreach(string name in new[]{"allNativeFaceNormalsVerified","allGeometryAndNonNormalAttributesExact","allVoxelBytesExact","physicsAndHeightsExact","sourceBytesExact","voxelMembershipExact","originalBindingsAndStateRestored","temporaryObjectsDestroyed"})Assert.That((bool)Detail(name),Is.True,name);
            }
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernCubicVoxelGrid"))
            {
                var cavern=Field("cavern");object Grid(string name)=>cavern.GetType().GetField(name).GetValue(cavern);
                Assert.That((bool)Grid("cubicGridRequested"),Is.True);Assert.That((int)Grid("configuredResolutionMultiplier"),Is.EqualTo(4));Assert.That((bool)Grid("configuredAutomaticVoxelHeight"),Is.True);
                Assert.That((Vector3[])Grid("actualVoxelScales"),Is.EqualTo(new[]{new Vector3(.78125f,.78125f,.78125f),new Vector3(.78125f,.78125f,.78125f)}));Assert.That(Grid("cubicInputProbe"),Is.Not.Null);
            }
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernSmoothUpperVault"))
            {
                var cavern=Field("cavern");var smoothing=cavern.GetType().GetField("vaultSmoothing").GetValue(cavern);Assert.That(smoothing,Is.Not.Null);Assert.That((int)smoothing.GetType().GetField("nativeOperations").GetValue(smoothing),Is.EqualTo(3));Assert.That(((Array)smoothing.GetType().GetField("samples").GetValue(smoothing)).Length,Is.EqualTo(3));
                if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernPaintSmoothedRock"))
                {
                    Assert.That((int)smoothing.GetType().GetField("rockPaintOperations").GetValue(smoothing),Is.EqualTo(3));foreach(string name in new[]{"paintSdfAndAlterationExact","paintMeshAndCollisionExact","paintHeightsAndHolesExact"})Assert.That((bool)smoothing.GetType().GetField(name).GetValue(smoothing),Is.True,name);
                }
            }
        }
        finally
        {
            var digger=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);
            if(!(bool)digger.GetProperty("IsBusy").GetValue(null))EditorSceneManager.RestoreSceneManagerSetup(setup);
            foreach(var item in incoming)Assert.That(File.ReadAllBytes(item.Key),Is.EqualTo(item.Value));
        }
    }
}
#endif
