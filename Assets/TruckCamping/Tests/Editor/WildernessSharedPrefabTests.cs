#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class WildernessSharedPrefabTests
{
    static string Digest(string p){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(p)).Select(x=>x.ToString("x2")));}
    [TestCase("TruckCamping")][TestCase("TruckPetPals")]
    public void ActualSavedScenesRetainOneInstanceOfTheSamePinnedWildernessPrefab(string mode)
    {
        const string prefab="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab",guid="2ec3264f79b522a49bcddfec51fc1cda";
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save interactive scene edits before this exact saved-reference proof.");
        string path="Assets/"+mode+"/Scenes/"+mode+".unity",before=Digest(path),meta=Digest(path+".meta");var prior=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var type=Type.GetType("LWS.TruckCamping.CampingWilderness, Assembly-CSharp",true);var worlds=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(type,true)).ToArray();Assert.That(worlds.Length,Is.EqualTo(1));
            Assert.That(AssetDatabase.AssetPathToGUID(prefab),Is.EqualTo(guid));Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(worlds[0].gameObject),Is.EqualTo(prefab));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(worlds[0]),Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(prefab).GetComponent(type)));
            Assert.That(Digest(path),Is.EqualTo(before));Assert.That(Digest(path+".meta"),Is.EqualTo(meta));LogAssert.NoUnexpectedReceived();
            TestContext.WriteLine("WILDERNESS_SHARED_EDITOR "+mode+" ONE actual saved prefab instance GUID="+guid+"; originalscene/meta exact; no reauthoring or duplicate forest.");
        }
        finally{if(prior.Length>0&&prior.All(x=>!string.IsNullOrEmpty(x.path)&&File.Exists(x.path))&&prior.Any(x=>x.isLoaded&&x.isActive))EditorSceneManager.RestoreSceneManagerSetup(prior);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
    }
}
#endif
