using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CompassNavigatorPro;
using LWS.TruckSports;

namespace LWS.TruckWifeCarrying.Editor
{
    public static class WifeHudRepairAuthoring
    {
        public static void UpgradeStartupCamera()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity");
            var match=UnityEngine.Object.FindAnyObjectByType<WifeMatch>();
            if(match==null||match.views==null)throw new InvalidOperationException("Saved Wife Carrying chase rig required");
            // The old offset placed the camera behind the render-only banner
            // even though the carried trucks started past it. Keep the native
            // Cinemachine rig and preserve any designer-authored alternative.
            if(match.views.groundOffset==new Vector3(0,6,-13)&&Mathf.Approximately(match.views.groundLookAhead,4))
            {
                match.views.groundOffset=new Vector3(0,6,-9);
                EditorUtility.SetDirty(match.views);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("WIFE_START_CAMERA saved mode-only chase offset="+match.views.groundOffset);
        }
        // Add references to the existing scene; never call its world generator.
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity");var match=UnityEngine.Object.FindAnyObjectByType<WifeMatch>();
            if(match==null)throw new InvalidOperationException("Saved Wife Carrying match required");
            var guide=match.GetComponent<WifeCheckpointGuidance>();
            if(guide!=null){if(guide.compasses?.Length!=4||guide.targets?.Length!=4)throw new InvalidOperationException("Preserve and inspect customized checkpoint guidance");return;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckSports/Resources/Broadcast objective compass.prefab");if(prefab==null)throw new InvalidOperationException("Owned CNP presentation prefab required");
            guide=match.gameObject.AddComponent<WifeCheckpointGuidance>();guide.match=match;guide.compasses=new SportsBroadcastCompass[4];guide.targets=new CompassProPOI[4];
            for(int view=0;view<4;view++)
            {
                var marker=new GameObject("Owned checkpoint target "+view);marker.transform.SetParent(match.transform,false);marker.SetActive(false);
                var target=marker.AddComponent<CompassProPOI>();target.compassGroupMask=1<<3;target.canBeVisited=false;target.hideWhenVisited=false;target.dontDestroyOnLoad=false;target.showOnScreenIndicator=true;target.showOffScreenIndicator=true;target.ignoreAreaOfInterest=true;target.playAudioClipWhenVisited=false;target.title="";target.iconNonVisited=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Sprites/icon-arrow-white.png");guide.targets[view]=target;
                var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);root.name="Owned checkpoint compass "+view;root.transform.SetParent(match.transform,false);root.SetActive(false);
                var compass=root.GetComponent<SportsBroadcastCompass>();compass.permittedTargets=new[]{target};compass.showOnScreenIndicators=true;compass.showOffScreenIndicators=true;compass.dontDestroyOnLoad=false;guide.compasses[view]=compass;
                PrefabUtility.RecordPrefabInstancePropertyModifications(compass);EditorUtility.SetDirty(compass);
            }
            EditorUtility.SetDirty(guide);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/wife-checkpoint-guidance.txt","Four existing-view CNP checkpoint references added without rebuilding the scene.");Debug.Log("WIFE_HUD_REPAIR four owned CNP checkpoint views; original physics/race geometry retained");
        }
    }
}
