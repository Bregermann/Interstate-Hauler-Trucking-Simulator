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

public sealed class DragonKingdomCosmeticGeometryTests
{
    [UnityTest,Timeout(1200000)]
    public IEnumerator ExplicitCandidatePreservesAcceptedAssetsAndPassesOriginalPhysicalGates()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"/CosmeticRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve current user scenes before explicit candidate authoring.");
        }
        var prior=EditorSceneManager.GetSceneManagerSetup();var original=prior.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"/CosmeticIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomCosmeticGeometryAuthoring, Assembly-CSharp-Editor",true);
            type.GetMethod("CreateReviewCandidate",Type.EmptyTypes).Invoke(null,null);yield return null;
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            foreach(string name in new[]{"acceptedAssetsExact","acceptedTerrainAssetsExact","roadMeshesExact","startsAndExtractionsExact","originalPhysicalGatesPassed"})Assert.That((bool)Field(receipt,name),Is.True,name);
            Assert.That((int)Field(receipt,"removedStretchedPeakRoots"),Is.EqualTo(18));Assert.That(((Array)Field(receipt,"placements")).Length,Is.EqualTo(22));
            var shoulders=Field(receipt,"candidateShoulders");Assert.That(Mathf.Abs((float)Field(shoulders,"escapeTreeTerrainAfter")-22.5f),Is.LessThan(3));
            foreach(var tile in (Array)Field(shoulders,"tiles")){Assert.That((int)Field(tile,"changedSamples"),Is.GreaterThan(0));Assert.That((bool)Field(tile,"protectedSamplesExact"),Is.True);Assert.That((bool)Field(tile,"originalHeightSamplesExact"),Is.True);}
            foreach(string name in new[]{"prefab","preview","definition"})Assert.That(File.Exists((string)Field(receipt,name)),Is.True,name);
            foreach(var file in original)Assert.That(File.ReadAllBytes(file.Key),Is.EqualTo(file.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(prior);}
    }
}
#endif
