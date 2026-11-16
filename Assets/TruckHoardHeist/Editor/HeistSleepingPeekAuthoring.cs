#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckHoardHeist.Editor
{
    public static class HeistSleepingPeekAuthoring
    {
        public const string VigilPath="Assets/TruckHoardHeist/PrivateLicensedPotteryFragments/HeistMotion/Curled sleeping vigilance.anim";
        public const string ControllerPath="Assets/TruckHoardHeist/Controllers/Heist guardian sleeping vigilance.overrideController";
        [Serializable] sealed class Receipt
        {
            public string scope="Explicit scene eyelid-binding upgrade only; no clip/controller/mesh/world/cargo regeneration or visual/native gameplay acceptance.";
            public string scene,sceneBefore,sceneAfter,sceneMetadataBefore,sceneMetadataAfter;
            public bool changed,repeatByteExact,existingAssetBytesPreserved,objectIdentitiesPreserved;
            public string[] paths,before,after;
        }
        public static bool BindExistingVigil(HoardHeistMatch match)
        {
            if(match==null||match.dragons.Length<1||match.dragons[0]==null||match.dragons[0].motor==null)throw new InvalidOperationException("Actual saved Heist guardian required.");
            var lids=match.dragons[0].motor.GetComponent<DragonEyelidPresentation>();if(lids==null)throw new InvalidOperationException("Existing actual Inferno surface eyelids required.");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(VigilPath);var controller=match.dragons[0].motor.animator.runtimeAnimatorController as AnimatorOverrideController;
            if(clip==null||controller==null||AssetDatabase.GetAssetPath(controller)!=ControllerPath||!match.species.TryGet(match.dragonSpeciesIds[0],out var species)||!species.TryGetAnimation(DragonAnimationAction.LookAround,out var mapping)||controller[mapping.clip]!=clip)
                throw new InvalidOperationException("Preserve the existing actual derived curled-vigilance clip/controller GUIDs; reviewed override required before peek binding.");
            if(Array.IndexOf(lids.sleepingPeekClips,clip)>=0){lids.ValidateBindings();return false;}
            lids.RegisterSleepingPeekClip(clip);lids.ValidateBindings();EditorUtility.SetDirty(lids);
            if(PrefabUtility.IsPartOfPrefabInstance(lids))PrefabUtility.RecordPrefabInstancePropertyModifications(lids);return true;
        }
        public static void UpgradeSavedHeistPeekBinding()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Explicit stopped saved-scene binding migration only.");
            string scenePath=HoardHeistAuthoring.ScenePath;if(!File.Exists(scenePath)||!File.Exists(scenePath+".meta"))throw new InvalidOperationException("Existing saved Heist scene and GUID required; no rebuild.");
            for(int index=0;index<SceneManager.sceneCount;index++)if(SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Preserve dirty user scenes before explicit additive binding migration.");
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;bool opened=false;
            string sceneBefore=Hash(scenePath),metadata=Hash(scenePath+".meta");Dictionary<string,string> assets=null;bool changed=false,identities=false,repeat=false;
            try
            {
                scene=SceneManager.GetSceneByPath(scenePath);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);opened=true;}
                var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<HoardHeistMatch>(true)).Single();
                assets=AssetDatabase.GetDependencies(VigilPath,true).Concat(AssetDatabase.GetDependencies(ControllerPath,true)).Concat(AssetDatabase.GetDependencies("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset",true)).Where(File.Exists).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToDictionary(path=>path,Hash);
                var objects=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).Select(transform=>GlobalObjectId.GetGlobalObjectIdSlow(transform).ToString()).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
                changed=BindExistingVigil(match);if(changed){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Actual existing scene binding save failed.");}
                string first=Hash(scenePath);if(BindExistingVigil(match)||first!=Hash(scenePath))throw new InvalidOperationException("Repeated existing scene peek binding is not byte-idempotent.");repeat=true;
                identities=objects.SequenceEqual(scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).Select(transform=>GlobalObjectId.GetGlobalObjectIdSlow(transform).ToString()).OrderBy(value=>value,StringComparer.Ordinal));
                if(!identities||metadata!=Hash(scenePath+".meta")||assets.Any(pair=>Hash(pair.Key)!=pair.Value))throw new InvalidOperationException("Binding migration changed original object identities, scene GUID or existing asset bytes.");
            }
            finally{if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom","HeistSleepingPeek","binding-migration.json"));Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,JsonUtility.ToJson(new Receipt{scene=scenePath,sceneBefore=sceneBefore,sceneAfter=Hash(scenePath),sceneMetadataBefore=metadata,sceneMetadataAfter=Hash(scenePath+".meta"),changed=changed,repeatByteExact=repeat,existingAssetBytesPreserved=true,objectIdentitiesPreserved=identities,paths=assets.Keys.ToArray(),before=assets.Values.ToArray(),after=assets.Keys.Select(Hash).ToArray()},true)+"\n");Debug.Log("HEIST_SLEEPING_PEEK_BINDING_UPGRADED "+output+"; no scene rebuild or rendered acceptance inferred.");
        }
        static string Hash(string path){string full=Path.GetFullPath(path);if(Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal))full=@"\\?\"+full;using(var sha=SHA256.Create())using(var input=File.OpenRead(full))return string.Concat(sha.ComputeHash(input).Select(value=>value.ToString("x2")));}
    }
}
#endif
