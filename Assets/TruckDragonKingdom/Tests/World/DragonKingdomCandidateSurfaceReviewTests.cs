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

public sealed class DragonKingdomCandidateSurfaceReviewTests
{
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualVendorRoadTilingNormalAndTerrainTriplanarSaveSeparateCandidateWithExactPhysics()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {var s=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(s.path))Assert.That(EditorSceneManager.SaveScene(s,generated+"SurfaceRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(s.isDirty||string.IsNullOrEmpty(s.path),Is.False,"Preserve user edits before isolated material candidate.");}
        var setup=EditorSceneManager.GetSceneManagerSetup();var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"SurfaceIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            yield return null;var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomCandidateSurfaceReview, Assembly-CSharp-Editor",true);type.GetMethod("CreateReviewCandidate").Invoke(null,null);
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(string n)=>receipt.GetType().GetField(n).GetValue(receipt);
            Assert.That((string)Field("status"),Is.EqualTo("CAPTURED_VENDOR_SURFACE_REVIEW_REQUIRES_ACTUAL_IMAGE_APPROVAL"));
            foreach(string n in new[]{"sourceBytesPreserved","physicsHeightsAndWeightsExact","shaderErrorsAbsent"})Assert.That((bool)Field(n),Is.True,n);
            Assert.That((int)Field("roadRenderers"),Is.GreaterThanOrEqualTo(17));Assert.That(((Array)Field("frames")).Length,Is.EqualTo(6));
            foreach(string n in new[]{"prefab","preview","definition"})Assert.That(File.Exists((string)Field(n)),Is.True,n);
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif
