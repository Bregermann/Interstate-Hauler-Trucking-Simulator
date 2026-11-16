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
public sealed class DragonKingdomVisualCauseProbeTests
{
    [UnityTest,Timeout(180000)]public IEnumerator ActualRenderedViewsIdentifyRealWallColliderPathsWithoutSourceWrites()
    {
        const string folder="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(s.path))Assert.That(EditorSceneManager.SaveScene(s,folder+"WallProbeRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(s.isDirty||string.IsNullOrEmpty(s.path),Is.False);}
        var setup=EditorSceneManager.GetSceneManagerSetup();var empty=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(empty,folder+"WallProbeIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var t=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomVisualCauseProbe, Assembly-CSharp-Editor",true);t.GetMethod("ProbeSavedReviewViews").Invoke(null,null);yield return null;
            var r=t.GetProperty("LastReceipt").GetValue(null);Assert.That((bool)r.GetType().GetField("sourceBytesPreserved").GetValue(r),Is.True);Assert.That(((Array)r.GetType().GetField("rays").GetValue(r)).Length,Is.EqualTo(270));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif
