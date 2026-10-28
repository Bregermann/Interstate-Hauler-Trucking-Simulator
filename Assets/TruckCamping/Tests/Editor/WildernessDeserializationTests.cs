#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class WildernessDeserializationTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static string Digest(string path){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b=>b.ToString("x2")));}
    static Type Runtime(string type)=>Type.GetType(type+", Assembly-CSharp",true);
    static Component[] Actors(Scene scene,string type)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Runtime(type),true)).Cast<Component>().ToArray();
    static void ProveLifecycle(Scene scene,string mode)
    {
        var explorers=Actors(scene,"LWS.TruckCamping.CampExplorer");Assert.That(explorers.Length,Is.EqualTo(4));
        foreach(var actor in explorers)
        {
            var field=actor.GetType().GetField("path",Any);Assert.That(field.GetValue(actor),Is.Null,"Saved Editor deserialization does not allocate native NavMeshPath.");
            var initialize=actor.GetType().GetMethod("EnsureNavigationPath",Any);initialize.Invoke(actor,null);var first=field.GetValue(actor);Assert.That(first,Is.Not.Null);
            initialize.Invoke(actor,null);Assert.That(field.GetValue(actor),Is.SameAs(first),"Safe main-thread lifecycle initialization reuses the actual path.");
        }
        var companions=Actors(scene,"LWS.TruckPetPals.PetCompanion");Assert.That(companions.Length,Is.EqualTo(mode=="TruckPetPals"?4:0));
        foreach(var actor in companions)
        {
            var tint=actor.GetType().GetField("tint",Any);var path=actor.GetType().GetField("safePath",Any);
            Assert.That(tint.GetValue(actor),Is.Null,"Inactive pet deserialization does not allocate native MPB.");Assert.That(path.GetValue(actor),Is.Null,"Inactive pet deserialization does not allocate native NavMeshPath.");
            bool active=actor.gameObject.activeSelf;var initialize=actor.GetType().GetMethod("EnsureNativeResources",Any);initialize.Invoke(actor,null);var firstTint=tint.GetValue(actor);var firstPath=path.GetValue(actor);
            Assert.That(firstTint,Is.Not.Null);Assert.That(firstPath,Is.Not.Null);initialize.Invoke(actor,null);
            Assert.That(tint.GetValue(actor),Is.SameAs(firstTint));Assert.That(path.GetValue(actor),Is.SameAs(firstPath));Assert.That(actor.gameObject.activeSelf,Is.EqualTo(active),"Initializing safe native resources cannot activate an unadopted pet or start navigation.");
        }
        Assert.That(scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).Sum(item=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)),Is.Zero);
        LogAssert.NoUnexpectedReceived();
    }
    [TestCase("TruckCamping")]
    [TestCase("TruckPetPals")]
    public void ActualSavedSceneCanOpenSaveFreshCopyAndDeserializeAgainWithoutNativeConstructorCalls(string mode)
    {
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save current interactive scene edits before this explicit saved-scene regression.");
        string source="Assets/"+mode+"/Scenes/"+mode+".unity";Assert.That(File.Exists(source),Is.True);string before=Digest(source),meta=Digest(source+".meta");
        var prior=EditorSceneManager.GetSceneManagerSetup();const string parent="Assets/TruckCamping/Tests/Editor";string name="TransientRoundtrip-"+Guid.NewGuid().ToString("N"),temporary=parent+"/"+name;
        Assert.That(Directory.Exists(temporary)||File.Exists(temporary+".meta"),Is.False);bool created=false;
        try
        {
            var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);ProveLifecycle(scene,mode);
            string guid=AssetDatabase.CreateFolder(parent,name);Assert.That(guid,Is.Not.Empty);created=true;
            string copy=temporary+"/"+mode+".unity";Assert.That(EditorSceneManager.SaveScene(scene,copy,true),Is.True);
            scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Single);ProveLifecycle(scene,mode);
            Assert.That(Digest(source),Is.EqualTo(before),"Original saved scene, including every original physics/configuration record, remains byte-exact.");Assert.That(Digest(source+".meta"),Is.EqualTo(meta),"Original scene GUID/importer remains byte-exact.");
            TestContext.WriteLine("WILDERNESS_DESERIALIZATION_PASS "+mode+" four original explorers; inactive pets="+(mode=="TruckPetPals"?4:0)+"; actual Editor open/save-copy/reload, idempotent native lifecycle, no log suppression, original scene/meta hashes exact.");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            if(created){Assert.That(temporary.StartsWith(parent+"/TransientRoundtrip-",StringComparison.Ordinal),Is.True);Assert.That(AssetDatabase.DeleteAsset(temporary),Is.True,"Only this fresh test-owned save-copy directory is removed.");}
            if(prior.Length>0&&prior.All(item=>!string.IsNullOrEmpty(item.path)&&File.Exists(item.path))&&prior.Any(item=>item.isLoaded&&item.isActive))EditorSceneManager.RestoreSceneManagerSetup(prior);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
}
#endif
