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

public sealed class DragonKingdomRoundedFoundationReviewTests
{
    [UnityTest,Timeout(1500000)]
    public IEnumerator RealVendorRoundedBiomesRetainPeaksFloorsAndReduceMeasuredSevereSlopes()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(s.path))Assert.That(EditorSceneManager.SaveScene(s,generated+"RoundedRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(s.isDirty||string.IsNullOrEmpty(s.path),Is.False,"Preserve user scenes before isolated vendor generation.");}
        var setup=EditorSceneManager.GetSceneManagerSetup();var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"RoundedIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoundedFoundationReview, Assembly-CSharp-Editor",true);var iterator=(IEnumerator)type.GetMethod("CreateReviewCandidate").Invoke(null,null);while(iterator.MoveNext())yield return iterator.Current;
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(string n)=>receipt.GetType().GetField(n).GetValue(receipt);
            Assert.That((string)Field("status"),Is.EqualTo("MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW"));Assert.That((string[])Field("validationIssues"),Is.Empty);
            Assert.That((bool)Field("sourceBytesPreserved"),Is.True);Assert.That((bool)Field("temporaryObjectsRemoved"),Is.True);Assert.That((int)Field("heightCallbacks"),Is.EqualTo(4));
            Assert.That(((Array)Field("terrains")).Length,Is.EqualTo(4));Assert.That(((Array)Field("biomes")).Length,Is.EqualTo(13));Assert.That(((Array)Field("centers")).Length,Is.EqualTo(9));
            Assert.That(File.Exists((string)Field("prefab")),Is.True);Assert.That(Terrain.activeTerrains,Is.Empty);
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif
