using System;
using System.IO;
using UnityEditor;

namespace LWS.TruckSports.Editor
{
    public static class SportsHitchesAuthoring
    {
        // Explicit authoring only. Normal play/build never regenerates venues.
        [MenuItem("Truck Sports/Alpha 0.10/Create missing expansion scenes")]
        public static void CreateMissingScenes()
        {
            RepairInterruptedDominoSnow();
            LWS.TruckWaterSports.Editor.WaterSportsAuthoring.CreateAll();
            LWS.TruckHorrorSports.Editor.HorrorAuthoring.CreateAll();
            LWS.TruckHitchSports.Editor.CoreHitchSportsAuthoring.CreateAll();
            LWS.TruckExtremeHauling.Editor.ExtremeHaulingAuthoring.CreateAll();
            int count = 0;
            foreach (var entry in SportsHitchesGames.All)
            {
                if (!File.Exists(entry.ScenePath)) throw new FileNotFoundException("New expansion venue not authored: " + entry.id, entry.ScenePath);
                count++;
            }
            if (count != 22) throw new InvalidOperationException("Alpha 0.10 requires exactly 22 distinct sports.");
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("HH10_SAVED_SCENES=22; gameplay, art and Windows acceptance remain separately required.");
        }
        [MenuItem("Truck Sports/Alpha 0.10/Install authored candidate scenery and equipment")]
        public static void InstallSavedCandidateContent()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before explicit candidate authoring.");
            foreach(string path in new[]{LWS.TruckWaterSports.Editor.WaterSportsAuthoring.PaddleScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.KayakScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.BlackwaterScene})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                LWS.TruckWaterSports.Editor.WaterSportsAuthoring.UpgradeActiveTruckScale();
                LWS.TruckWaterSports.Editor.WaterVenueLandscapeAuthoring.UpgradeActiveVenue();
                LWS.TruckWaterSports.Editor.WaterEffectsAuthoring.InstallActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            foreach(var entry in SportsHitchesGames.All)if(entry.ScenePath.StartsWith("Assets/TruckExtremeHauling/Scenes/",StringComparison.Ordinal))
            {
                var venue=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.ScenePath);
                LWS.TruckExtremeHauling.Editor.HaulingVenueLandscapeAuthoring.UpgradeActiveVenue();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(venue);
            }
            foreach(var entry in SportsHitchesGames.All)if(entry.ScenePath.StartsWith("Assets/TruckHitchSports/Scenes/",StringComparison.Ordinal))
            {
                var venue=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.ScenePath);
                LWS.TruckHitchSports.Editor.CoreHitchVenueLandscapeAuthoring.InstallActiveVenue();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(venue);
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity");
            LWS.TruckExtremeHauling.Editor.HaulingVenuePresentation.UpgradeActiveRingTossLaunchRamps();
            var cargo=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity");
            LWS.TruckExtremeHauling.Editor.CargoLandingGearAuthoring.UpgradeActiveCargoLandingGear();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(cargo);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity");
            LWS.TruckExtremeHauling.Editor.SkyhookSlalomAuthoring.UpgradeActiveSkyhookSlalom();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveHighwireReceivingSurfaces();
            var trebuchet=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveTrebuchetApparatus();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(trebuchet);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_CANDIDATE_CONTENT_INSTALLED explicit new-venue additions only.");
        }
        public static void InstallSavedTechnicalRiverForks()
        {
            foreach(string path in new[]{LWS.TruckWaterSports.Editor.WaterSportsAuthoring.PaddleScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.KayakScene})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                LWS.TruckWaterSports.Editor.RiverRouteAuthoring.InstallActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void RepairSavedHitchRunLandingGear()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/HitchAndRun.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveHitchRunLandingGear();AssetDatabase.SaveAssets();
        }
        public static void RepairSavedTrebuchetLoadingLane()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveTrebuchetApparatus();
            AssetDatabase.SaveAssets();
        }
        static void RepairInterruptedDominoSnow()
        {
            const string path = "Assets/TruckExtremeHauling/Scenes/DominoDerby.unity";
            if (!File.Exists(path)) return;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            if (UnityEngine.Object.FindAnyObjectByType<SportsSnowSurface>() != null) return;
            foreach (var trailer in UnityEngine.Object.FindObjectsByType<LWS.TruckHitchSports.TrailerBody>(UnityEngine.FindObjectsSortMode.None))
                if (trailer.GetComponent<SportsSnowTrackExclusion>() == null) trailer.gameObject.AddComponent<SportsSnowTrackExclusion>();
            SportsProductionAuthoring.InstallScene(); SportsFocusPolicy.InstallScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            UnityEngine.Debug.Log("HH10_DOMINO_SNOW_REPAIRED non-rolling loads excluded; physical trailer components retained.");
        }
    }
}
