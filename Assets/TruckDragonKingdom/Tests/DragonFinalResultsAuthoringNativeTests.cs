#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonFinalResultsAuthoringNativeTests
{
    [Test,Timeout(180000)]
    public void ExplicitFourModeBoardsPreserveOriginalDocumentsControlsAndRepeatedSceneBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        const string generated="Assets/TruckDragonKingdom/Tests/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"ResultsRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Save user edits before isolated authoring.");
        }
        string[] modes={"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonEscape","TruckDragonRiders"};
        var legacy=new[]{"TruckKeepaway","TruckCornhole","TruckCheeseRolling","TruckBalloonBattle"}.Select(mode=>"Assets/"+mode+"/Scenes/"+mode+".unity").Where(File.Exists).ToDictionary(path=>path,File.ReadAllBytes);
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonFinalResultsAuthoring, Assembly-CSharp-Editor",true);var upgrade=type.GetMethod("UpgradeSavedModes");
        upgrade.Invoke(null,null);
        var installed=modes.ToDictionary(mode=>"Assets/"+mode+"/Scenes/"+mode+".unity",mode=>File.ReadAllBytes("Assets/"+mode+"/Scenes/"+mode+".unity"));
        upgrade.Invoke(null,null);upgrade.Invoke(null,null);
        foreach(var item in installed)Assert.That(File.ReadAllBytes(item.Key),Is.EqualTo(item.Value),"Designer customizations and saved board identities survive repeats: "+item.Key);
        foreach(var item in legacy)Assert.That(File.ReadAllBytes(item.Key),Is.EqualTo(item.Value),"No unrelated original sport scene migration: "+item.Key);
    }
}
#endif
