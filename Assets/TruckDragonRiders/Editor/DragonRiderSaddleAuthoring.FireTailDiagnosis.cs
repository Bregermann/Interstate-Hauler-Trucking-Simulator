#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class FireTailCandidate
        {
            public string scope="READ_ONLY_FIVE_PROVEN_FIRE_ACTION_TAIL1_ROTATION_CANDIDATES_EXISTING_PATCH_ALL_NATIVE_POSES_NOT_SAVED_AUTHORING_OR_MATCH_ACCEPTANCE";public bool assetsExact;public FootprintReceipt footprint;public MountedActionReceipt[] recipes;
        }
        public static string InspectFiveFireTailRepairsWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for five-action candidate diagnosis.");for(int index=0;index<SceneManager.sceneCount;index++)if(SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Preserve unsaved scenes before candidate diagnosis.");
            string scenePath=DragonRidersAuthoring.ScenePath,actorPath=ActorPath("dragon-fire"),catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";var watched=AssetDatabase.GetDependencies(actorPath,true).Concat(new[]{scenePath,catalogPath}).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)).Distinct().SelectMany(path=>new[]{path,path+".meta"}).ToDictionary(path=>path,Hash);var owned=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(catalogPath);if(catalog==null||!catalog.TryGet("dragon-fire",out var species)||!species.TryGetAnimation(DragonAnimationAction.Fly,out var forward)||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var hover))throw new InvalidOperationException("Actual original Fire source mappings required.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit retained candidate evidence required.");string output=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(output);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;GameObject root=null;var transient=new List<UnityEngine.Object>();var report=new FireTailCandidate();
            try
            {
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);var match=scene.GetRootGameObjects().SelectMany(value=>value.GetComponentsInChildren<DragonRidersMatch>(true)).Single();var saved=match.racers[0].choices.Select(value=>value.GetComponent<DragonRiderSaddle>()).Single(value=>value.speciesId==species.id);var calibration=new Calibration{modelRearwardOffset=0,anchorMesh=AssetDatabase.GetAssetPath(saved.surfaceRenderer.sharedMesh),anchorA=saved.surfaceVertexA,anchorB=saved.surfaceVertexB,anchorC=saved.surfaceVertexC,anchorBarycentric=saved.surfaceBarycentric,patchAnchors=saved.surfacePatchAnchors};
                root=PrefabUtility.LoadPrefabContents(actorPath);var actor=root.GetComponent<DragonCharacter>();actor.motor.animator.enabled=false;var controller=UnityEngine.Object.Instantiate((AnimatorOverrideController)saved.actor.motor.animator.runtimeAnimatorController);transient.Add(controller);actor.motor.animator.runtimeAnimatorController=controller;var recipes=new List<MountedActionReceipt>();var hoverClip=CreateCarrierHoverClip(species,hover.clip,forward.clip,out var hoverReceipt);transient.Add(hoverClip);controller[hover.clip]=hoverClip;recipes.Add(hoverReceipt);calibration.clip=hoverClip;
                foreach(var action in new[]{DragonAnimationAction.TurnLeft,DragonAnimationAction.TurnRight,DragonAnimationAction.Land,DragonAnimationAction.Breath,DragonAnimationAction.Takeoff})
                {
                    if(!species.TryGetAnimation(action,out var source))throw new InvalidOperationException("Actual original selected Fire action required: "+action);MountedActionReceipt receipt;AnimationClip clip;
                    if(action==DragonAnimationAction.Takeoff){clip=CreateCarrierTakeoffClip(source.clip,forward.clip);receipt=new MountedActionReceipt{species=species.id,action=action.ToString(),source=AssetDatabase.GetAssetPath(source.clip),reference=AssetDatabase.GetAssetPath(forward.clip),neckTranslationsUnchanged=true};}
                    else clip=CreateMountedSupportAction(species,source.clip,forward.clip,action,out receipt);
                    transient.Add(clip);ApplySelectedForwardTailRoot(species,source.clip,forward.clip,clip,receipt);controller[source.clip]=clip;recipes.Add(receipt);
                }
                hoverClip.SampleAnimation(actor.motor.gameObject,0);var original=actor.motor.GetComponentsInChildren<Transform>(true).Select(joint=>new BonePose{bone=joint,position=joint.localPosition,rotation=joint.localRotation}).ToArray();FitsOriginalFootprints(actor,species,calibration,original,match.trucks,out var footprint);footprint.scope="READ_ONLY_TRANSIENT_ALL_FIRE_SOURCE_AND_MIXER_POSES_ACTUAL_TAIL_OBB_AND_UNCHANGED_SKIN_GATES_NO_SAVED_ASSETS";report.footprint=footprint;report.recipes=recipes.ToArray();
            }
            finally
            {
                if(root!=null)PrefabUtility.UnloadPrefabContents(root);for(int index=transient.Count-1;index>=0;index--)if(transient[index]!=null)UnityEngine.Object.DestroyImmediate(transient[index]);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);var after=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);report.assetsExact=owned.Keys.OrderBy(path=>path,StringComparer.Ordinal).SequenceEqual(after.OrderBy(path=>path,StringComparer.Ordinal))&&owned.All(pair=>Hash(pair.Key)==pair.Value)&&watched.All(pair=>Hash(pair.Key)==pair.Value);try{File.WriteAllText(Path.Combine(output,"five-fire-tail-candidate-read-only.json"),JsonUtility.ToJson(report,true));}catch(Exception error){Debug.LogWarning("Could not retain Fire tail observation: "+error.Message);}if(!report.assetsExact)throw new InvalidOperationException("Read-only tail candidate diagnosis changed saved assets.");
            }
            return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
