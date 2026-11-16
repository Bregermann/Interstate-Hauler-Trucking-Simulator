#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomCavernFoundationReviewTests
{
    [UnityTest,Timeout(1200000)]
    public IEnumerator ExistingVendorCavernBiomeProducesMeasuredOverheadMassWithoutSourceChanges()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"CavernMassRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve user scenes before genuine vendor terrain review.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"CavernMassIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomCavernFoundationReview, Assembly-CSharp-Editor",true);
            var iterator=(IEnumerator)type.GetMethod("CreateReviewCandidate").Invoke(null,null);while(iterator.MoveNext())yield return iterator.Current;
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(receipt,"status"),Is.EqualTo("MEASURED_MASSIF_READY_FOR_SEPARATE_DIGGER_EXCAVATION_REVIEW"));
            foreach(string n in new[]{"sourceBytesPreserved","outsideBiomePreserved","temporaryObjectsRemoved"})Assert.That((bool)Field(receipt,n),Is.True,n);
            Assert.That((int)Field(receipt,"populationCallbacks"),Is.EqualTo(4));Assert.That(((Array)Field(receipt,"terrains")).Length,Is.EqualTo(4));
            Assert.That(((Array)Field(receipt,"roof")).Length,Is.EqualTo(117));Assert.That((float)Field(receipt,"minimumRoofHeight"),Is.GreaterThanOrEqualTo(160));
            Assert.That(File.Exists((string)Field(receipt,"prefab")),Is.True);Assert.That(File.Exists((string)Field(receipt,"graph")),Is.True);
            Assert.That(Terrain.activeTerrains,Is.Empty,"Temporary vendor terrain must be removed after measured output saves.");
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif
