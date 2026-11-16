#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // One explicit batch over the four existing sport-owned composers. Never
    // rebuild an existing hand-edited scene or mutate the accepted shared art.
    public static class DragonKingdomGameAuthoring
    {
        [Serializable] sealed class SceneReceipt {public string path,sha256,error;public bool created;public int regions,trucks,dragons;}
        [Serializable] sealed class Receipt {public string scope="EXPLICIT_SAVED_SCENE_COMPOSITION_NOT_PLAYABLE_MATCH_ACCEPTANCE";public string sharedWorldBefore,sharedWorldAfter;public SceneReceipt[] scenes;}
        static string Hash(string path){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
        public static void PrepareOriginalArtAndAudio()
        {
            LWS.TruckDragonKingdom.Ambience.DragonAmbientVillagerAuthoring.ValidateOriginalProfiles();
            DragonWingAudioAuthoring.InstallConfiguredWingAudio();
        }
        public static void CreateMissingModes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonReceipt");if(at<0||at+1>=args.Length)throw new InvalidOperationException("A unique explicit composition receipt is required.");
            string output=Path.GetFullPath(args[at+1]);if(File.Exists(output))throw new IOException("Preserve prior composition evidence.");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(DragonKingdomWorldAuthoring.PrefabPath)==null)throw new InvalidOperationException("The physically accepted shared kingdom must exist first.");
            string sharedHash=Hash(DragonKingdomWorldAuthoring.PrefabPath);var facts=new List<SceneReceipt>();var failures=new List<Exception>();
            string[] names={"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonEscape","TruckDragonRiders"};
            Action[] create={LWS.TruckHoardHeist.Editor.HoardHeistAuthoring.CreateNewScene,LWS.TruckDragonEggDelivery.Editor.DragonEggDeliveryAuthoring.Create,LWS.TruckDragonEscape.Editor.DragonEscapeAuthoring.Create,LWS.TruckDragonRiders.Editor.DragonRidersAuthoring.Create};
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            for(int i=0;i<names.Length;i++)
            {
                string path="Assets/"+names[i]+"/Scenes/"+names[i]+".unity";bool created=!File.Exists(path);string original=created?null:Hash(path);
                try
                {
                if(created)create[i]();else EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                if(!File.Exists(path))throw new IOException("Composer did not save its actual scene: "+path);
                var scene=EditorSceneManager.GetActiveScene();if(scene.path!=path)throw new InvalidOperationException("Composer left a different active scene: "+path);
                var roots=scene.GetRootGameObjects();var world=roots.SelectMany(root=>root.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();world.ValidateReferences();
                var session=roots.SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                if(session is LWS.TruckDragonRiders.DragonRidersMatch riders)LWS.TruckDragonRiders.Editor.DragonRidersAuthoring.ValidateScene(riders);
                int trucks=roots.SelectMany(root=>root.GetComponentsInChildren<SportsEventTruck>(true)).Count(truck=>truck.session==session);if(trucks!=4)throw new InvalidOperationException("Exactly four original participant tractors required: "+path);
                string savedHash=Hash(path);if(!created&&savedHash!=original)throw new InvalidOperationException("Existing designer scene changed while being inspected: "+path);
                if(Hash(DragonKingdomWorldAuthoring.PrefabPath)!=sharedHash)throw new InvalidOperationException("A mode composer changed the accepted shared kingdom asset.");
                facts.Add(new SceneReceipt{path=path,sha256=savedHash,created=created,regions=world.regions.Length,trucks=trucks,dragons=roots.SelectMany(root=>root.GetComponentsInChildren<DragonCharacter>(true)).Count()});
                }
                catch(Exception error)
                {
                    // Each new sport is independently reviewable. Preserve a
                    // failed composition and continue the other composers;
                    // the final batch still fails and records the exact cause.
                    failures.Add(new InvalidOperationException(path,error));
                    facts.Add(new SceneReceipt{path=path,created=created,error=error.ToString()});
                    Debug.LogWarning("DRAGON_MODE_COMPOSITION_FAILED "+path+"\n"+error);
                    if(Application.isBatchMode&&created)
                    {
                        // Preserve the failed owned scene rather than leaving
                        // dirty partial authoring to block the next sport.
                        var failed=EditorSceneManager.GetActiveScene();
                        if(failed.IsValid()&&failed.isLoaded&&failed.isDirty)
                        {
                            const string generated="Assets/TruckDragonKingdom/Tests/FailedComposition";
                            Directory.CreateDirectory(generated);AssetDatabase.Refresh();
                            string snapshot=generated+"/"+names[i]+"-"+Guid.NewGuid().ToString("N")+".unity";
                            if(!EditorSceneManager.SaveScene(failed,snapshot))throw new IOException("Failed owned composition could not be preserved: "+path,error);
                            string retained=Path.Combine(Path.GetDirectoryName(output),"failed-scenes",Path.GetFileName(snapshot));Directory.CreateDirectory(Path.GetDirectoryName(retained));File.Copy(snapshot,retained);
                        }
                    }
                }
                File.WriteAllText(output,JsonUtility.ToJson(new Receipt{sharedWorldBefore=sharedHash,sharedWorldAfter=Hash(DragonKingdomWorldAuthoring.PrefabPath),scenes=facts.ToArray()},true));
                if(Hash(DragonKingdomWorldAuthoring.PrefabPath)!=sharedHash)throw new InvalidOperationException("A mode composer changed the accepted shared kingdom asset.");
            }
            if(failures.Count>0)throw new AggregateException("One or more dragon modes failed composition; successful saved scenes and all failures are retained.",failures);
            Debug.Log("DRAGON_FOUR_MODES_COMPOSED "+facts.Count+" saved scenes; full native and Windows gameplay acceptance still required.");
        }
        public static void CreateAndBindMissingModes()
        {
            try{CreateMissingModes();}
            finally{DragonKingdomBroadcastAuthoring.UpgradeSavedModes();}
        }
    }
}
#endif
