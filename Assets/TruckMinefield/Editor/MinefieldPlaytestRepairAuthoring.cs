using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckMinefield.Editor
{
    public static class MinefieldPlaytestRepairAuthoring
    {
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
