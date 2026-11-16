#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.TruckDragonKingdom;
using MalbersAnimations.Controller;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckHoardHeist.Editor
{
    public static class HeistSleepingVigilExitAuthoring
    {
        public const string NaturalControllerPath="Assets/TruckHoardHeist/Controllers/Heist guardian natural vigilance.controller";
        const string SharedControllerPath="Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon.controller";
        [Serializable] sealed class Receipt
        {
            public string scope="Heist-only LookAround PlayOnce vendor ModeBehaviour exit configuration; no motion, hearing, scoring, physics or shared controller edits.";
            public string scene,sceneBefore,sceneAfter,controller,controllerBefore,controllerAfter,overrideBefore,overrideAfter;
            public bool changed,repeatByteExact,originalAssetsExact,sceneGuidExact,objectIdentitiesExact;
            public string[] preservedPaths,preservedHashes;
        }
        static IEnumerable<AnimatorState> States(AnimatorStateMachine machine)
        {
            foreach(var child in machine.states)yield return child.state;
            foreach(var child in machine.stateMachines)foreach(var state in States(child.stateMachine))yield return state;
        }
        static ModeBehaviour VigilCallback(AnimatorController controller)
        {
            var state=controller.layers.SelectMany(layer=>States(layer.stateMachine)).Single(value=>value.tag=="DragonAction_LookAround");
            return state.behaviours.OfType<ModeBehaviour>().Single();
        }
        // CopyAsset legitimately clones embedded BlendTree subassets. Compare
        // their complete serialized structure recursively, retaining exact clip
        // leaves and one-to-one tree identity instead of requiring tree refs.
        static bool SameMotion(Motion source,Motion copy,Dictionary<BlendTree,BlendTree> trees)
        {
            if(source==null||copy==null)return source==copy;
            if(source is AnimationClip)return source==copy;
            if(!(source is BlendTree tree)||!(copy is BlendTree other))return false;
            if(trees.TryGetValue(tree,out var mapped))return mapped==other;
            if(trees.Values.Contains(other))return false;
            trees.Add(tree,other);
            using(var original=new SerializedObject(tree))using(var cloned=new SerializedObject(other))
            {
                var left=original.GetIterator();var right=cloned.GetIterator();
                while(true)
                {
                    bool hasLeft=left.Next(true),hasRight=right.Next(true);
                    if(hasLeft!=hasRight)return false;if(!hasLeft)return true;
                    if(left.propertyPath!=right.propertyPath||left.propertyType!=right.propertyType)return false;
                    if(left.propertyType==SerializedPropertyType.Generic)continue;
                    if(left.propertyType==SerializedPropertyType.ObjectReference&&(left.objectReferenceValue is Motion||right.objectReferenceValue is Motion))
                    {if(!SameMotion(left.objectReferenceValue as Motion,right.objectReferenceValue as Motion,trees))return false;}
                    else if(!SerializedProperty.DataEquals(left,right))return false;
                }
            }
        }
        static AnimatorController NaturalController()
        {
            var original=AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
            if(original==null||VigilCallback(original).NoExitTransitions)throw new InvalidOperationException("Preserve the original shared looping LookAround controller and inspect changed source configuration before migration.");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(NaturalControllerPath);
            if(controller==null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(NaturalControllerPath));AssetDatabase.Refresh();
                if(!AssetDatabase.CopyAsset(SharedControllerPath,NaturalControllerPath))throw new InvalidOperationException("Actual independent Heist controller copy failed.");
                controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(NaturalControllerPath);
            }
            var callback=VigilCallback(controller);
            if(callback.ModeID==null||!callback.EnterMode||!callback.ExitMode||!callback.OnModeUpdate||callback.ExitAbility!=-1)
                throw new InvalidOperationException("Actual Malbers enter/update/exit callback required; no synthetic completion replacement.");
            if(!callback.NoExitTransitions||!Mathf.Approximately(callback.ExitTime,.98f))
            {callback.NoExitTransitions=true;callback.ExitTime=.98f;EditorUtility.SetDirty(callback);AssetDatabase.SaveAssetIfDirty(callback);}
            var originalStates=original.layers.SelectMany(layer=>States(layer.stateMachine)).ToArray();var copiedStates=controller.layers.SelectMany(layer=>States(layer.stateMachine)).ToArray();
            var trees=new Dictionary<BlendTree,BlendTree>();
            if(originalStates.Length!=copiedStates.Length)throw new InvalidOperationException("Heist controller must retain every original state.");
            for(int i=0;i<originalStates.Length;i++)
            {
                var source=originalStates[i];var copy=copiedStates[i];
                if(source==copy||source.name!=copy.name||source.tag!=copy.tag||!SameMotion(source.motion,copy.motion,trees)||source.speed!=copy.speed||source.transitions.Length!=copy.transitions.Length||source.behaviours.Length!=copy.behaviours.Length)
                    throw new InvalidOperationException("Only the independent Heist vigilance callback may differ from the original controller.");
                for(int n=0;n<source.behaviours.Length;n++)
                {
                    string expected=EditorJsonUtility.ToJson(source.behaviours[n]);string actual=EditorJsonUtility.ToJson(copy.behaviours[n]);
                    if(source.tag=="DragonAction_LookAround"&&copy.behaviours[n] is ModeBehaviour)actual=actual.Replace("\"NoExitTransitions\":true","\"NoExitTransitions\":false");
                    if(source.behaviours[n]==copy.behaviours[n]||expected!=actual)throw new InvalidOperationException("Unrelated state callback changed during Heist controller copy.");
                }
            }
            return controller;
        }
        // Explicit stopped initial configuration only. The runtime vendor callback
        // observes its own real normalized clip clock and performs AnimationTagExit.
        public static bool ConfigureExistingVigilInstance(DragonAnimalMotor motor)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||motor==null||EditorUtility.IsPersistent(motor))throw new InvalidOperationException("Stopped actual Heist scene instance required.");
            return ConfigureInstance(motor,false);
        }
        // Native fixture uses the already authored asset/configuration before Prepare;
        // it never changes a running action, callback, phase or perception result.
        public static void ConfigureNativeFixtureBeforePrepare(DragonAnimalMotor motor)
        {
            if(motor==null||motor.Prepared||AssetDatabase.LoadAssetAtPath<AnimatorController>(NaturalControllerPath)==null)throw new InvalidOperationException("Existing explicit Heist controller and unprepared actual rig required.");
            ConfigureInstance(motor,true);
        }
        static bool ConfigureInstance(DragonAnimalMotor motor,bool fixture)
        {
            var overrides=motor.animator.runtimeAnimatorController as AnimatorOverrideController;
            if(overrides==null||AssetDatabase.GetAssetPath(overrides)!=HeistSleepingPeekAuthoring.ControllerPath)throw new InvalidOperationException("Existing Heist vigilance override required.");
            var controller=fixture?AssetDatabase.LoadAssetAtPath<AnimatorController>(NaturalControllerPath):NaturalController();
            if(!VigilCallback(controller).NoExitTransitions)throw new InvalidOperationException("Run the explicit natural vigilance exit migration before playback.");
            bool changed=false;
            if(overrides.runtimeAnimatorController!=controller)
            {
                if(fixture)throw new InvalidOperationException("Native fixture may not repair persistent configuration.");
                var retained=new List<KeyValuePair<AnimationClip,AnimationClip>>();overrides.GetOverrides(retained);overrides.runtimeAnimatorController=controller;overrides.ApplyOverrides(retained);
                var actual=new List<KeyValuePair<AnimationClip,AnimationClip>>();overrides.GetOverrides(actual);
                if(actual.Count!=retained.Count||actual.Any(pair=>!retained.Any(original=>original.Key==pair.Key&&original.Value==pair.Value)))throw new InvalidOperationException("Every original clip override must remain exact.");
                EditorUtility.SetDirty(overrides);AssetDatabase.SaveAssetIfDirty(overrides);changed=true;
            }
            var binding=motor.actionBindings.Single(value=>value.action==DragonAnimationAction.LookAround);
            var mode=motor.animal.modes.Single(value=>value.ID==binding.mode);var ability=mode.Abilities.Single(value=>value.Index.Value==binding.abilityIndex);
            if(ability.Status!=AbilityStatus.PlayOnce){ability.Status=AbilityStatus.PlayOnce;changed=true;}
            if(binding.continuous){binding.continuous=false;changed=true;}
            if(changed&&!fixture)
            {
                EditorUtility.SetDirty(motor);EditorUtility.SetDirty(motor.animal);
                if(PrefabUtility.IsPartOfPrefabInstance(motor))PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                if(PrefabUtility.IsPartOfPrefabInstance(motor.animal))PrefabUtility.RecordPrefabInstancePropertyModifications(motor.animal);
            }
            return changed;
        }
        public static void UpgradeSavedHeistVigilExit()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Explicit stopped Heist configuration migration only.");
            string path=HoardHeistAuthoring.ScenePath;if(!File.Exists(path))throw new InvalidOperationException("Existing saved Heist required; no scene rebuild.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty user scenes first.");
            var preserved=AssetDatabase.GetDependencies(SharedControllerPath,true).Concat(AssetDatabase.GetDependencies(HeistSleepingPeekAuthoring.VigilPath,true)).Append(HeistSleepingPeekAuthoring.ControllerPath+".meta").Append(path+".meta").Where(File.Exists).SelectMany(value=>value.EndsWith(".meta",StringComparison.Ordinal)?new[]{value}:new[]{value,value+".meta"}).Where(File.Exists).Distinct().ToDictionary(value=>value,Hash);
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;bool opened=false,changed=false,identities=false,repeat=false;
            string before=Hash(path),metadata=Hash(path+".meta"),controllerBefore=File.Exists(NaturalControllerPath)?Hash(NaturalControllerPath):null,overrideBefore=Hash(HeistSleepingPeekAuthoring.ControllerPath);
            try
            {
                scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened=true;}
                var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<HoardHeistMatch>(true)).Single();
                var ids=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).Select(value=>GlobalObjectId.GetGlobalObjectIdSlow(value).ToString()).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
                changed=ConfigureExistingVigilInstance(match.dragons[0].motor);
                if(changed){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Actual existing Heist save failed.");}
                var exact=new[]{path,NaturalControllerPath,NaturalControllerPath+".meta",HeistSleepingPeekAuthoring.ControllerPath}.ToDictionary(value=>value,Hash);
                if(ConfigureExistingVigilInstance(match.dragons[0].motor)||exact.Any(pair=>Hash(pair.Key)!=pair.Value))throw new InvalidOperationException("Repeated vigilance exit migration changed bytes.");repeat=true;
                identities=ids.SequenceEqual(scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).Select(value=>GlobalObjectId.GetGlobalObjectIdSlow(value).ToString()).OrderBy(value=>value,StringComparer.Ordinal));
                if(!identities||metadata!=Hash(path+".meta")||preserved.Any(pair=>Hash(pair.Key)!=pair.Value))throw new InvalidOperationException("Original shared assets, motion, GUID or saved object identity changed.");
            }
            finally{if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom","HeistSleepingPeek","natural-exit-migration.json"));Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,JsonUtility.ToJson(new Receipt{scene=path,sceneBefore=before,sceneAfter=Hash(path),controller=NaturalControllerPath,controllerBefore=controllerBefore,controllerAfter=Hash(NaturalControllerPath),overrideBefore=overrideBefore,overrideAfter=Hash(HeistSleepingPeekAuthoring.ControllerPath),changed=changed,repeatByteExact=repeat,originalAssetsExact=true,sceneGuidExact=metadata==Hash(path+".meta"),objectIdentitiesExact=identities,preservedPaths=preserved.Keys.ToArray(),preservedHashes=preserved.Values.ToArray()},true)+"\n");Debug.Log("HEIST_NATURAL_VIGIL_EXIT_UPGRADED "+output);
        }
        static string Hash(string path){string full=Path.GetFullPath(path);if(Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal))full=@"\\?\"+full;using(var sha=SHA256.Create())using(var input=File.OpenRead(full))return string.Concat(sha.ComputeHash(input).Select(value=>value.ToString("x2")));}
    }
}
#endif
