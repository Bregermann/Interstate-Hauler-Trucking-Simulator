using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
using System.Linq;

namespace LWS.TruckMinefield.Editor
{
    public static class MinefieldPlaytestRepairAuthoring
    {
        [MenuItem("Truck Sports/Minefield/Repair viewport sky coverage")]
        public static void UpgradeViewportSky()
        {
            EditorSceneManager.OpenScene(MinefieldAuthoring.Scene);
            var match = Object.FindAnyObjectByType<MinefieldMatch>();
            if (match.views == null || match.views.views.Length != 4 || match.views.broadcast == null)
                throw new System.InvalidOperationException("Expected the existing four player views and shared Minefield weather binding.");
            LWS.TruckSports.Editor.SportsWeatherRenderingAuthoring.UseViewportSkybox(match.gameObject,match.views.views.Select(view=>view.view).Concat(new[]{match.views.broadcast.view}).ToArray(),"TruckMinefield");
            EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
            EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("MINEFIELD_SKY: installed Weather Maker calculations, Unity viewport skybox coverage, vendor AfterSkybox clouds. No gameplay, performance profile, other scene or vendor source changes.");
        }

        [MenuItem("Truck Sports/Minefield/Apply compact distance HUD")]
        public static void Upgrade()
        {
            EditorSceneManager.OpenScene(MinefieldAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<MinefieldMatch>();
            var hud=match.GetComponent<SportsExpansionViewHud>()??match.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=match;hud.rally=match.views;hud.minimalDistance=true;
            EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("MINEFIELD_HUD: existing shared rounded portrait widget, only participant color/truck portrait/live cumulative distance. Terrain, mines, force, controls and cameras unchanged.");
        }
    }
}
