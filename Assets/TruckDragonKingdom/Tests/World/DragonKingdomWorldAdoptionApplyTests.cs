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

public sealed class DragonKingdomWorldAdoptionApplyTests
{
    [UnityTest,Timeout(1800000)]
    public IEnumerator ExactReviewedCandidateAdoptsOriginalIdentitiesAndFourModesWithRealNavigationAndRepeatsByteExactly()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";
        Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"AdoptionRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve actual user edits before explicit saved-world adoption.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"AdoptionIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAdoptionPlan, Assembly-CSharp-Editor",true);
            // The production iterator checks exact physical, eighteen-view,
            // eight-mounted-rig, and retained-native-dependency evidence. This
            // fixture never supplies substitute approval or bypasses a gate.
            yield return (IEnumerator)type.GetMethod("ApplyAcceptedCandidate").Invoke(null,null);
            var proof=type.GetProperty("LastApplyReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(proof,"status"),Is.EqualTo("ADOPTED_ORIGINAL_IDENTITIES_AND_MODES_PRESERVED_REQUIRES_ACTUAL_PLAY_ACCEPTANCE"));
            foreach(string field in new[]{"prefabIdentitiesPreserved","previewIdentitiesPreserved","originalGuidsPreserved","originalSourceBytesPreserved","definitionRulesPreserved","physicalGatesPassed","allModeExternalStateAndSceneOwnedIdsPreserved","exactRetainedDiggerDependenciesPresent","exactRetainedOuterDependenciesPresent"})Assert.That((bool)Field(proof,field),Is.True,field);
            Assert.That((int)Field(proof,"retainedPrefabIdentities"),Is.GreaterThan(10000));
            Assert.That((int)Field(proof,"retainedPreviewIdentities"),Is.GreaterThan(10000));
            var dependencies=(Array)Field(proof,"requiredDiggerDependencies");Assert.That(dependencies.Length,Is.GreaterThan(500));
            foreach(var file in dependencies)Assert.That(((string)Field(file,"path")).StartsWith((string)Field(proof,"diggerDataFolder"),StringComparison.Ordinal),Is.True);
            var outer=(Array)Field(proof,"requiredOuterDependencies");Assert.That(outer,Is.Not.Null);if(outer.Length!=0)Assert.That(outer.Length,Is.GreaterThanOrEqualTo(26),"Real outer prefab/twelve TerrainData and their metadata must accompany fresh-build provenance.");
            // Current authored palette owns12 outer copies while the original
            // retained outer closure remains exact. Exercise the real adopted
            // Terrain/TerrainCollider bindings, not a synthetic approval.
            string candidate=(string)Field(proof,"candidate"),candidateFolder="Assets/TruckDragonKingdom/VisualCandidates/"+candidate;
            var receiptType=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration+Receipt, Assembly-CSharp-Editor",true);
            var physical=JsonUtility.FromJson(File.ReadAllText(candidateFolder+"/Integration Receipt.json"),receiptType);var palette=Field(physical,"palette");Assert.That((bool)Field(proof,"qualifiedOwnedMountainDeltaApplied"),Is.EqualTo(Field(physical,"ownedMountainRoad")!=null),"Only explicitly receipt-qualified owned mountain candidates may change the single canonical road target.");
            if(palette!=null)
            {
                var clones=((Array)Field(palette,"tiles")).Cast<object>().Where(tile=>(bool)Field(tile,"externalDataCloned")).ToArray();Assert.That(clones.Length,Is.EqualTo(12));
                var canonical=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab");var terrains=canonical.GetComponentsInChildren<Terrain>(true);
                Assert.That(terrains.Length,Is.EqualTo(16));var retainedOuter=outer.Cast<object>().ToDictionary(f=>(string)Field(f,"path"),f=>(string)Field(f,"sha256"));
                foreach(var tile in clones)
                {
                    string source=(string)Field(tile,"sourceData"),owned=(string)Field(tile,"candidate");
                    Assert.That(retainedOuter.ContainsKey(source)&&retainedOuter.ContainsKey(source+".meta"),Is.True,"Original outer provenance must remain retained, not replaced by clones.");
                    Assert.That(AssetDatabase.AssetPathToGUID(source),Is.EqualTo((string)Field(tile,"sourceDataGuid")));
                    Assert.That(owned.StartsWith(candidateFolder+"/Terrain/Palette/",StringComparison.Ordinal),Is.True);
                    var terrain=terrains.Single(t=>AssetDatabase.GetAssetPath(t.terrainData)==owned);Assert.That(terrain.GetComponent<TerrainCollider>().terrainData,Is.SameAs(terrain.terrainData));
                    Assert.That(AssetDatabase.AssetPathToGUID(owned),Is.EqualTo((string)Field(tile,"candidateGuid")));
                    Assert.That((string)Field(tile,"candidateGuid"),Is.Not.EqualTo((string)Field(tile,"sourceDataGuid")));
                    Assert.That(terrains.Any(t=>AssetDatabase.GetAssetPath(t.terrainData)==source),Is.False,"No stale direct source TerrainData binding may survive.");
                }
            }
            var modes=(Array)Field(proof,"modes");Assert.That(modes.Length,Is.EqualTo(4));
            foreach(var mode in modes)
            {
                Assert.That(Field(mode,"externalStateAfter"),Is.EqualTo(Field(mode,"externalStateBefore")));
                Assert.That((int)Field(mode,"trucks"),Is.EqualTo(4));Assert.That((bool)Field(mode,"sceneOwnedIdentitiesPreserved"),Is.True);Assert.That((bool)Field(mode,"originalWorldOverridesPreserved"),Is.True);
                var ambience=Field(mode,"ambience");Assert.That((bool)Field(ambience,"allExistingObjectsAndRouteReferencesPreserved"),Is.True);
                Assert.That((int)Field(ambience,"walkers"),Is.EqualTo(8));Assert.That((int)Field(ambience,"birds"),Is.EqualTo(7));Assert.That((int)Field(ambience,"sounds"),Is.EqualTo(9));
                var navigation=(string[])Field(ambience,"navigation");Assert.That(navigation.Length,Is.EqualTo(8));
                foreach(string path in navigation)Assert.That(AssetDatabase.LoadMainAssetAtPath(path),Is.Not.Null,"Actual native navigation output must persist: "+path);
            }
            var saved=((Array)Field(proof,"savedFiles")).Cast<object>().ToDictionary(f=>(string)Field(f,"path"),f=>File.ReadAllBytes((string)Field(f,"path")));
            Assert.That(saved.Count,Is.EqualTo(7));
            yield return (IEnumerator)type.GetMethod("ApplyAcceptedCandidate").Invoke(null,null);
            foreach(var pair in saved)Assert.That(File.ReadAllBytes(pair.Key),Is.EqualTo(pair.Value),"Explicit repeat changed accepted bytes: "+pair.Key);
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif
