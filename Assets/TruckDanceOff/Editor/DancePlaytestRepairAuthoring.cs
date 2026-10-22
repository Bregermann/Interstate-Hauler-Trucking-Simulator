using System;
using System.IO;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDanceOff.Editor
{
    public static class DancePlaytestRepairAuthoring
    {
        [MenuItem("Truck Sports/Dance-Off/Repair saved concert presentation")]
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(DanceAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<DanceMatch>();
            if(match==null)throw new InvalidOperationException("Existing Dance-Off required.");
            if(match.GetComponent<DanceConcertCamera>()!=null){Debug.Log("DANCE_REPAIR_PRESERVED saved customization.");return;}
            var rig=match.stageCamera.GetComponent<SportsEventCamera>();if(rig==null||rig.rig==null||rig.broadcastFraming!=null)throw new InvalidOperationException("The existing dedicated Cinemachine concert rig must own this view.");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TruckSports/Art/Logos/TruckDanceOff.png");var sign=GameObject.Find("Original Truck Sports Party stage wordmark");if(texture==null||sign==null)throw new InvalidOperationException("Existing original Dance-Off PNG and saved stage sign are required.");
            const string path="Assets/TruckDanceOff/Materials/Dedicated Dance-Off stage logo.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(sign.GetComponent<Renderer>().sharedMaterial);material.name="Dedicated Dance-Off stage logo";material.mainTexture=texture;AssetDatabase.CreateAsset(material,path);}
            sign.GetComponent<Renderer>().sharedMaterial=material;sign.transform.position=new Vector3(0,8.6f,16.4f);sign.transform.localScale=new Vector3(18,18f*texture.height/texture.width,1);
            foreach(var dancer in match.dancers)dancer.transform.rotation=Quaternion.Euler(0,180,0);
            var camera=match.gameObject.AddComponent<DanceConcertCamera>();camera.match=match;camera.cameraRig=rig;
            match.greatTimingWindow=.12f;match.goodTimingWindow=.21f;
            EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/dance-playtest-repair-authoring.txt","Dedicated original Dance-Off PNG on existing stage sign; four authored truck rotations face the existing concert camera. Existing Cinemachine rig gains bounded musical shot selection, while DSP/rules/choreography/controls remain authoritative. Match GREAT120ms/GOOD210ms; PERFECT45ms and existing strict constructor remain.");
        }
    }
}
