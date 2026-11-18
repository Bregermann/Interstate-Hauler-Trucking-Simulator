#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace LWS.TruckHorrorSports.Editor
{
    public static class ScarecrowLureAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active physical scarecrow headlight lure")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit upgrade.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.ScarecrowSeason))throw new InvalidOperationException("Open the saved Scarecrow Season scene.");
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);var actor=match.antagonist;
            if(actor==null||!actor.scarecrow||actor.body==null)throw new InvalidOperationException("Reuse the existing physical animated scarecrow.");
            if(actor.GetComponent<ScarecrowHeadlightLure>()!=null){Debug.Log("SCARECROW_PHYSICAL_LURE_PRESERVED");return;}
            var old=match.anomalies.Where(a=>a!=null&&a.kind==HorrorAnomalyKind.FalseHeadlights).ToArray();
            if(old.Length!=1||old[0].lights==null||old[0].lights.Length!=2||old[0].lights.Any(l=>l==null))throw new InvalidOperationException("The original single two-light false-headlight event is required; preserve customized encounters for manual review.");
            var idle=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NWH/Vehicle Physics 2/Sound/Engine/Running/EngineRunning - Truck4.wav");var crow=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_Crow.ogg");
            if(idle==null||crow==null)throw new InvalidOperationException("Reuse installed NWH idle and Weather Maker crow audio.");
            var positions=new[]{new Vector3(-24,1,5),new Vector3(24,1,47),new Vector3(-24,1,89),new Vector3(72,1,-16),new Vector3(-72,1,110),new Vector3(120,1,25)};
            foreach(var p in positions)if(!NavMesh.SamplePosition(p,out var projected,3,NavMesh.AllAreas))throw new InvalidOperationException("An authored lure station must lie on the existing farm navigation: "+p);
            var helper=actor.gameObject.AddComponent<ScarecrowHeadlightLure>();helper.match=match;helper.actor=actor;helper.retiredStaticEvent=old[0];helper.decoyLights=old[0].lights.ToArray();helper.lureStations=new Transform[positions.Length];
            var root=new GameObject("Authored physical false-headlight investigation stations").transform;
            for(int i=0;i<positions.Length;i++){var marker=new GameObject("Existing corridor headlight-lure station "+i).transform;marker.SetParent(root,false);NavMesh.SamplePosition(positions[i],out var projected,3,NavMesh.AllAreas);marker.position=projected.position;helper.lureStations[i]=marker;}
            // Adapt the owned pair rather than running a second static light event.
            for(int i=0;i<2;i++){var light=helper.decoyLights[i];Undo.RecordObject(light.transform,"Reuse original false headlight on physical stalker");light.transform.SetParent(actor.transform,false);light.transform.localPosition=new Vector3(i==0?-.7f:.7f,1.5f,1.2f);light.color=new Color(.86f,.84f,.62f);light.range=35;light.intensity=5;light.enabled=false;EditorUtility.SetDirty(light);}
            match.anomalies=match.anomalies.Where(a=>a!=old[0]).ToArray();old[0].enabled=false;EditorUtility.SetDirty(old[0]);
            helper.lureSound=Source(actor.transform,"Owned false diesel idle",idle,true);helper.revealSound=Source(actor.transform,"Owned scarecrow light revelation",crow,false);
            EditorUtility.SetDirty(match);EditorUtility.SetDirty(helper);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("SCARECROW_PHYSICAL_LURE_AUTHORED original light pair adapted to existing force-driven stalker; installed audio/animation/navigation retained, no actor teleport or objective changes. Root must integrate the small perception destination hooks, save and test.");
        }
        static AudioSource Source(Transform parent,string name,AudioClip clip,bool loop)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.up*2;var source=go.AddComponent<AudioSource>();source.clip=clip;source.playOnAwake=false;source.spatialBlend=1;source.minDistance=4;source.maxDistance=65;source.rolloffMode=AudioRolloffMode.Linear;source.loop=loop;return source;}
    }
}
#endif
