using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiPopulationAuthoring
    {
        public const string ProfilePath = "Assets/LWS/TruckTaxi/ScriptableObjects/Population/TruckTaxiCityPopulation.asset";

        [MenuItem("Truck Taxi/Population/Create Or Select Density Profile")]
        public static void SelectProfile() => Selection.activeObject = EnsureProfile();

        public static TruckTaxiPopulationProfile EnsureProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TruckTaxiPopulationProfile>(ProfilePath);
            if (profile != null) return profile;
            const string parent = "Assets/LWS/TruckTaxi/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(parent + "/Population")) AssetDatabase.CreateFolder(parent, "Population");
            profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            // Measured in the existing live DemoCity: 12 native pedestrians and 12 traffic, not the pedestrian cap of 24.
            profile.measuredPedestrianBaseline = 12;
            profile.measuredTrafficBaseline = 12;
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            return profile;
        }

        [MenuItem("Truck Taxi/Population/Apply Profile To Loaded Demo City")]
        public static void ApplyToLoadedDemoCity()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("Apply the Taxi density profile outside Play Mode."); return; }
            var profile = EnsureProfile();
            int assigned = 0;
            foreach (var population in Object.FindObjectsByType<TruckTaxiPedestrianPopulation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (population.gameObject.scene.name != "TruckTaxi_DemoCity" || population.densityProfile != null) continue;
                Undo.RecordObject(population, "Assign Taxi population density"); population.densityProfile = profile;
                EditorUtility.SetDirty(population); EditorSceneManager.MarkSceneDirty(population.gameObject.scene); assigned++;
            }
            foreach (var traffic in Object.FindObjectsByType<TruckTaxiTrafficAdapter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (traffic.gameObject.scene.name != "TruckTaxi_DemoCity" || traffic.densityProfile != null) continue;
                Undo.RecordObject(traffic, "Assign Taxi traffic density"); traffic.densityProfile = profile;
                EditorUtility.SetDirty(traffic); EditorSceneManager.MarkSceneDirty(traffic.gameObject.scene); assigned++;
            }
            Debug.Log("TAXI POPULATION: assigned " + assigned + " existing adapters. Existing custom profiles, baseline fields and all UTS path assets preserved. Save the scene through the normal Editor workflow.");
        }
    }
}
