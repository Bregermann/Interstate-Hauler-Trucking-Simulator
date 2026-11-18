#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckHorrorSports.Editor
{
    public static class DeadEndPursuitAudioAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Dead End physical pursuit cues")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before the explicit saved-scene audio upgrade.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.DeadEnd))throw new InvalidOperationException("Open the saved Dead End venue.");
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);var actor=match.antagonist;
            if(actor==null||actor.semi==null||actor.body==null)throw new InvalidOperationException("Retain the existing physical driverless semi.");
            var existing=actor.GetComponent<DeadEndPursuitAudio>();if(existing!=null){if(existing.match!=match||existing.actor!=actor||existing.emitter==null)throw new InvalidOperationException("Preserve the edited cue component and repair its original references.");Debug.Log("DEAD_END_PURSUIT_CUES_PRESERVED");return;}
            var horn=Need("Assets/NWH/Vehicle Physics 2/Sound/Horn/Horn.wav");var brake=Need("Assets/NWH/Vehicle Physics 2/Sound/Brakes/Air Brake - Strong.wav");
            var helper=Undo.AddComponent<DeadEndPursuitAudio>(actor.gameObject);helper.match=match;helper.actor=actor;helper.horn=horn;helper.airBrake=brake;
            var go=new GameObject("Actual driverless semi spatial approach cues");Undo.RegisterCreatedObjectUndo(go,"Add owned spatial pursuit cues");go.transform.SetParent(actor.transform,false);go.transform.localPosition=Vector3.up*2;
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=1;source.minDistance=5;source.maxDistance=140;source.rolloffMode=AudioRolloffMode.Linear;source.pitch=.8f;source.dopplerLevel=.35f;
            source.outputAudioMixerGroup=actor.semi.engine!=null?actor.semi.engine.outputAudioMixerGroup:match.encounterAudio!=null?match.encounterAudio.outputAudioMixerGroup:null;helper.emitter=source;
            EditorUtility.SetDirty(helper);EditorSceneManager.MarkSceneDirty(scene);Debug.Log("DEAD_END_PURSUIT_CUES_AUTHORED option1/3 only; original physical controls, speeds, roads and outcomes untouched. Caller saves and validates actual approaches/listening.");
        }
        static AudioClip Need(string path){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null)throw new InvalidOperationException("Existing NWH audio asset missing: "+path);return clip;}
    }
}
#endif
