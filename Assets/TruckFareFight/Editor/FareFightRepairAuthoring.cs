using System;
using CompassNavigatorPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckFareFight.Editor
{
    public static class FareFightRepairAuthoring
    {
        [MenuItem("Truck Sports/Fare Fight/Apply playtest objective indicators once")]
        public static void Upgrade()
        {
            EditorSceneManager.OpenScene(FareFightAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<FareFightMatch>();if(match==null)throw new InvalidOperationException("Saved Fare Fight scene missing.");
            var navigation=match.navigation;if(navigation.playtestRepairVersion>=2)return;
            if(navigation.playtestRepairVersion>=1){HideMetricDuplicate(navigation);Save(match);return;}
            navigation.objectiveMarkers=new CompassProPOI[4];
            for(int view=0;view<4;view++)
            {
                var old=navigation.gps[view];var root=old.gameObject;root.SetActive(false);
                var compass=root.AddComponent<SportsBroadcastCompass>();EditorUtility.CopySerializedManagedFieldsOnly(old,compass);UnityEngine.Object.DestroyImmediate(old);navigation.gps[view]=compass;
                compass.iconPixels=48;compass.labelFontSize=20;compass.labelWidth=180;
                // Editor configuration must not invoke runtime Refresh against
                // an inactive minimap whose runtime icon pool does not exist.
                var settings=new SerializedObject(compass);settings.FindProperty("_compassGroup").intValue=view+1;
                settings.FindProperty("_showOnScreenIndicators").boolValue=true;settings.FindProperty("_showOffScreenIndicators").boolValue=true;
                settings.FindProperty("_offScreenIndicatorScale").floatValue=1.2f;settings.FindProperty("_offScreenIndicatorMargin").floatValue=.1f;settings.FindProperty("_offScreenIndicatorShowDistance").boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();
                var marker=new GameObject("Independent Fare objective "+(view+1));marker.transform.SetParent(navigation.transform,false);marker.SetActive(false);var poi=marker.AddComponent<CompassProPOI>();
                poi.canBeVisited=false;poi.hideWhenVisited=false;poi.dontDestroyOnLoad=false;poi.showOnScreenIndicator=true;poi.showOffScreenIndicator=true;poi.ignoreAreaOfInterest=true;poi.playAudioClipWhenVisited=false;poi.radius=0;poi.compassGroupMask=1<<view;
                poi.iconNonVisited=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");compass.permittedTargets=new[]{poi};navigation.objectiveMarkers[view]=poi;EditorUtility.SetDirty(compass);
            }
            HideMetricDuplicate(navigation);Save(match);
            Debug.Log("FAREFIGHT_REPAIR: four isolated CNP objective indicators, valid vendor groups1-4; existing independent route/minimap settings preserved.");
        }
        static void HideMetricDuplicate(FareFightNavigation navigation){foreach(var compass in navigation.gps){var settings=new SerializedObject(compass);settings.FindProperty("_onScreenIndicatorShowDistance").boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();}}
        static void Save(FareFightMatch match){match.navigation.playtestRepairVersion=2;EditorUtility.SetDirty(match.navigation);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();}
    }
}
