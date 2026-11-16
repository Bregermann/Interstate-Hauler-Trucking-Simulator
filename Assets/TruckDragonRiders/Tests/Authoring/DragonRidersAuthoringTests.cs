#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRidersAuthoringTests
{
    [Test,Timeout(120000)]
    public void ExistingSavedRidersReferencesValidateWithoutRegenerationOrWrites()
    {
        const string scenePath="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
        Assert.That(File.Exists(scenePath),Is.True,"Retain and inspect the actual saved V87 scene; never recreate it for this diagnosis.");
        const string folder="Assets/TruckDragonRiders/Tests/Authoring/Generated";
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var s=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(s.path)){Directory.CreateDirectory(folder);AssetDatabase.Refresh();Assert.That(EditorSceneManager.SaveScene(s,folder+"/ValidationRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);}
            Assert.That(s.isDirty||string.IsNullOrEmpty(s.path),Is.False,"Preserve original user scenes before validation.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var bytes=File.ReadAllBytes(scenePath);
        try
        {
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            var type=Type.GetType("LWS.TruckDragonRiders.DragonRidersMatch, Assembly-CSharp",true);
            var match=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(type,true)).Single();
            var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRidersAuthoring, Assembly-CSharp-Editor",true);
            author.GetMethod("ValidateScene").Invoke(null,new[]{match});
            Assert.That(File.ReadAllBytes(scenePath),Is.EqualTo(bytes),"Read-only validation rewrote the saved scene.");
            Debug.Log("DRAGON_RIDERS_SAVED_BINDINGS_VALID actualWorld/catalog/weather/views fourNwhCrews twelveOrMoreOrderedGates sceneBytesExact=true");
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [Test,Timeout(300000)]
    public void SavedRidersUsesActualSharedKingdomConfiguredMountsAndFourNwhCrews()
    {
        const string folder="Assets/TruckDragonRiders/Tests/Authoring/Generated";
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))
            {
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                Assert.That(EditorSceneManager.SaveScene(scene,folder+"/RunnerScene-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            }
            Assert.That(scene.isDirty,Is.False,"Preserve unsaved user edits before explicit scene authoring.");
            Assert.That(!string.IsNullOrEmpty(scene.path)||scene.rootCount==0,Is.True);
        }
        var before=EditorSceneManager.GetSceneManagerSetup();
        var hashes=before.Where(s=>!string.IsNullOrEmpty(s.path)&&File.Exists(s.path)).ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        try
        {
            var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRidersAuthoring, Assembly-CSharp-Editor",true);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab"),Is.Not.Null,
                "Complete the original native world acceptance first; this fixture does not substitute a mock world.");
            author.GetMethod("Create").Invoke(null,null);
            const string scenePath="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath),Is.Not.Null);
            var type=Type.GetType("LWS.TruckDragonRiders.DragonRidersMatch, Assembly-CSharp",true);
            var match=UnityEngine.Object.FindAnyObjectByType(type);
            Assert.That(match,Is.Not.Null);author.GetMethod("ValidateScene").Invoke(null,new[]{match});
            var racers=(Array)type.GetField("racers").GetValue(match);Assert.That(racers.Length,Is.EqualTo(4));
            foreach(var racer in racers)
            {
                var truck=(Component)racer.GetType().GetField("truck").GetValue(racer);
                Assert.That(truck.GetType().GetField("tractorMotor").GetValue(truck),Is.Not.Null);
                Assert.That(((Array)racer.GetType().GetField("choices").GetValue(racer)).Length,Is.GreaterThanOrEqualTo(1));
            }
            foreach(var saved in hashes)Assert.That(File.ReadAllBytes(saved.Key),Is.EqualTo(saved.Value),saved.Key);
        }
        finally
        {
            if(before.Length==1&&string.IsNullOrEmpty(before[0].path))EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            else EditorSceneManager.RestoreSceneManagerSetup(before);
        }
    }
}
#endif
