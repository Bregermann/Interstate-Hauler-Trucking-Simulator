#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class BreathFlexReport{public string scope="READ_ONLY_ORDERED_BOUNDED_BREATH_POSITION_FLEXION_ORIGINAL_HEAD_WORLD_AIM_ALL_SOURCE_BREATH_FRAMES_AND_ENTRY_EXIT_PHASES_NO_SAVES_NOT_FULL_ACTION_OR_RUNTIME_ACCEPTANCE";public bool assetsExact,nwhExact,sceneBytesExact;public List<BreathFlexSpecies> species=new List<BreathFlexSpecies>();}
        [Serializable]sealed class BreathFlexSpecies{public string species;public Vector2[] orderedCandidates;public bool qualified;public float chosenExtraNeck2,chosenExtraDistal;public List<BreathFlexAttempt> attempts=new List<BreathFlexAttempt>();}
        [Serializable]sealed class BreathFlexAttempt{public float extraNeck2,extraDistal;public string failure;public SavedFlightValidation measured;}
        public static string InspectBoundedBreathFlexionCandidatesWithoutSavingAssets()=>InspectBreathFlexion(false);
        public static string InspectActualLayerBreathFlexionWithoutSavingAssets()=>InspectBreathFlexion(true);
        static string InspectBreathFlexion(bool actualLayer)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes first.");var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);string scenePath=DragonRidersAuthoring.ScenePath,sceneHash=Hash(scenePath);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;var plans=new List<SavedFlightPlan>();var report=new BreathFlexReport();if(actualLayer)report.scope="READ_ONLY_SAME_EXTRA30_BUDGET_INFERNO_PROXIMAL_FLEX_THREE_KNOWN_PURE_FAILURES_AND_REAL_CONFIGURED_TWO_LAYER_FIXED_POINT12_FLIGHT_ENTRY_EXIT_NOT_FULL_COVERAGE_OR_SAVED_ACCEPTANCE";DragonRidersMatch match=null;string physics=null;
            try
            {
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);foreach(string id in new[]{"dragon-inferno","dragon-fire"})
                {
                    var row=new BreathFlexSpecies{species=id,orderedCandidates=id=="dragon-inferno"?(actualLayer?new[]{new Vector2(30,0)}:new[]{new Vector2(0,15),new Vector2(0,30),new Vector2(15,15)}):new[]{new Vector2(0,10),new Vector2(0,20),new Vector2(0,30)}};report.species.Add(row);var actor=match.racers.SelectMany(racer=>racer.choices).First(value=>value.GetComponent<DragonRiderSaddle>().speciesId==id);var basis=BuildSavedFlightPlan(actor,id,true);plans.Add(basis);
                    foreach(var candidate in row.orderedCandidates)
                    {
                        var attempt=new BreathFlexAttempt{extraNeck2=candidate.x,extraDistal=candidate.y};row.attempts.Add(attempt);try
                        {
                            var prior=actor.motor.animator.runtimeAnimatorController;UnityEngine.Object[] created;try{actor.motor.animator.runtimeAnimatorController=basis.expected;created=CreateTransientBreathFlexCandidate(actor,id,candidate.x,candidate.y);}finally{actor.motor.animator.runtimeAnimatorController=prior;}var plan=new SavedFlightPlan{species=id,flap=basis.flap,basis=basis.expected,expected=created.OfType<AnimatorOverrideController>().Single(),temporary=created.ToList(),keys=basis.keys,validationKeys=basis.validationKeys,safeMounted=true,validation=new SavedFlightValidation{species=id,rootFlapFraction=basis.flap}};plans.Add(plan);attempt.measured=plan.validation;var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");catalog.TryGet(id,out var definition);definition.TryGetAnimation(DragonAnimationAction.Breath,out var breath);AssertNonFlightMappings(plan.basis,plan.expected,new[]{breath.clip});ValidateCompleteSavedFlightCandidate(actor,match.trucks,plan,actualLayer?ActualLayerBreathTrials:BreathFramesAndBoundaries,false);row.qualified=true;row.chosenExtraNeck2=candidate.x;row.chosenExtraDistal=candidate.y;WriteBreathFlexReport(report,actualLayer);break;
                        }
                        catch(Exception error){attempt.failure=error.ToString();WriteBreathFlexReport(report,actualLayer);}
                    }
                }
                if(report.species.Any(value=>!value.qualified))throw new InvalidOperationException("No declared bounded Breath flexion clears every actual source frame and entry/exit boundary for one or more species; inspect retained exact attempts.");
            }
            finally
            {
                if(match!=null&&physics!=null)report.nwhExact=PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);foreach(var plan in plans)plan.Dispose();if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sceneBytesExact=Hash(scenePath)==sceneHash;report.assetsExact=watched.Count==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);WriteBreathFlexReport(report,actualLayer);if(!report.assetsExact||!report.nwhExact||!report.sceneBytesExact)throw new InvalidOperationException("Read-only Breath screen changed original source/scene/NWH.");
            }
            return JsonUtility.ToJson(report,true);
        }
        static List<MountedPoseTrial> BreathFramesAndBoundaries(DragonCharacter actor,DragonSpeciesDefinition species)
        {
            if(!species.TryGetAnimation(DragonAnimationAction.Breath,out var breath))throw new InvalidOperationException("Actual calibrated Breath mapping required.");var runtime=actor.motor.animator.runtimeAnimatorController;while(runtime is AnimatorOverrideController layer)runtime=layer.runtimeAnimatorController;if(runtime is not AnimatorController controller)throw new InvalidOperationException("Actual source graph required.");var source=ControllerStates(controller.layers[0].stateMachine).SelectMany(state=>MotionClips(state.motion)).Distinct().Where(clip=>clip!=breath.clip).ToArray();var actual=EffectiveRiderClip(actor,breath.clip);var result=new List<MountedPoseTrial>();int frames=Mathf.Max(2,Mathf.CeilToInt(breath.clip.length*breath.clip.frameRate));for(int i=0;i<=frames;i++)result.Add(new MountedPoseTrial{name="Every original Breath frame "+i+"/"+frames,clip=actual,fraction=i/(float)frames});foreach(var key in source)foreach(float phase in new[]{0f,.25f,.45f,.75f,.98f})foreach(float weight in new[]{.25f,.5f,.75f})
            {var other=EffectiveRiderClip(actor,key);result.Add(new MountedPoseTrial{name="Breath exit -> "+key.name+" BreathPhase="+phase+" weight="+weight,clip=actual,incoming=other,fraction=phase,incomingFraction=.05f,incomingWeight=weight});result.Add(new MountedPoseTrial{name=key.name+" entry -> Breath BreathPhase="+phase+" weight="+weight,clip=other,incoming=actual,fraction=.45f,incomingFraction=phase,incomingWeight=weight});}return result;
        }
        static void WriteBreathFlexReport(BreathFlexReport report,bool actualLayer){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring",actualLayer?"BreathLayer":"BreathFlex");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"bounded-breath-flex.json"),JsonUtility.ToJson(report,true));}
    }
}
#endif
